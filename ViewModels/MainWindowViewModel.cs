using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DioramaEngine.Models;
using DioramaEngine.Services;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda;
using Noggog;
using System.IO;
using System.Windows;
using System.Diagnostics;
using Mutagen.Bethesda.Installs;

namespace DioramaEngine.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<string> profiles = [];

        [ObservableProperty]
        private string? selectedProfile;

        [ObservableProperty]
        private List<DioramaRef> references;

        [ObservableProperty]
        private DioramaRef selectedReference;

        [ObservableProperty]
        private Visibility createVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private int refCount;

        [ObservableProperty]
        private double progressValue;

        [ObservableProperty]
        private IncrementProgress progress;

        [ObservableProperty]
        private Visibility progressVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private List<Master> masters;

        [ObservableProperty]
        private string author;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private bool isESP = true;

        [ObservableProperty]
        private bool isBOS = false;


        private string _defaultDirectory;
        private ISkyrimMod _outputMod;
        private string _profileDir;
        private List<UpdatedReference> _updatedReferences = [];
        private bool _newReferenceFound;

        // Tasks that need to be performed when the application first opens
        [RelayCommand]
        private async Task Initialise()
        {

            // If the SKSE path is set in config then launch SKSE and wait for it to exit
            if (!string.IsNullOrEmpty(Settings.SksePath))
            {
                ProcessStartInfo processStartInfo = new()
                {
                    WorkingDirectory = GameLocations.GetGameFolder(GameRelease.SkyrimSE),
                    FileName = Settings.SksePath
                };
                Process? p = Process.Start(processStartInfo);
                await p.WaitForExitAsync();
            }

            // Find all the profile files, if none found display error and shut down
            using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
            _profileDir = $@"{env.DataFolderPath}\Diorama\profiles\";
            if (Directory.Exists(_profileDir))
            {
                var profilePaths = Directory.EnumerateFiles(_profileDir, "*.json");

                if (!profilePaths.Any())
                {
                    MessageBox.Show($"No profiles found", "Diorama", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }

                Profiles = profilePaths.Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
                SelectedProfile = Profiles.FirstOrDefault();
            }
            else
            {
                MessageBox.Show($"Profile directory not found", "Diorama", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            _defaultDirectory = Path.Combine(Environment.CurrentDirectory, "output");
            if (!Directory.Exists(_defaultDirectory))
                Directory.CreateDirectory(_defaultDirectory);
        }

        // Read the references in from the selected profile
        [RelayCommand]
        private async Task Read()
        {
            ProgressVisibility = Visibility.Visible;
            try
            {
                _newReferenceFound = false;
                References = JsonHandler.Read($@"{_profileDir}\{SelectedProfile}.json");
                SelectedReference = References.FirstOrDefault();
                _outputMod = (ISkyrimMod)ModInstantiator.Activator(ModKey.FromName(SelectedProfile, ModType.Plugin), GameRelease.SkyrimSE);
                _updatedReferences.Clear();

                // Initialise the progress bar
                Progress = new(s =>
                {
                    ProgressValue = s;
                }, References.Count * 3);

                HashSet<string> knownMasters = [];

                // Run in the background as it can be CPU intensive
                await Task.Run(() =>
                {
                    using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
                    Progress.Increment();
                    using var linkCache = env.LoadOrder.ListedOrder.Where(lo => lo.Enabled).ToImmutableLinkCache();
                    Progress.Increment();

                    foreach (DioramaRef reference in References)
                    {
                        // For each reference either add it as a new reference or override an existing one
                        if (reference.IsNew)
                        {
                            _newReferenceFound = ESP.AddNewReference(_outputMod, linkCache, knownMasters, reference, Progress);
                        }
                        else
                        {
                            var formLink = ESP.CollectFormLink(reference, Progress);
                            if (formLink != null)
                            {
                                // Add reference to the list, we'll perform the update operation during the write operation later
                                // This is due to needing to know the ESP name in order to handle master conflicts
                                _updatedReferences.Add(new UpdatedReference (reference, formLink));
                            }
                        }
                        Progress.Increment();
                    }
                    // Initialise the potential master list based the load order. Any known masters are disabled in the UI to prevent deselection
                    // Due to delaying the updating of references until the write operation, we won't have a full list of known masters unfortunately
                    Masters = env.LoadOrder.ListedOrder.Select(m => new Master(knownMasters.Contains(m.ModKey.Name)) { Key = m.ModKey }).ToList();
                });

                CreateVisibility = Visibility.Visible;
            } 
            catch  (Exception ex)
            {
                MessageBox.Show($"Unhandled error occurred: {ex.Message}", "Diorama", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            ProgressVisibility = Visibility.Collapsed;
        }

        [RelayCommand]
        private void CreateNewESP()
        {
            try
            {
                bool save = SaveNewESP();

                if (!save)
                    return;

                MessageBox.Show("Plugin created", "Diorama", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.Yes);
                CleanUp();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unhandled error occurred: {ex.Message}", "Diorama", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void UpdateESP()
        {
            try
            {
                bool found = UpdateExistingESP();

                if (!found)
                    return;

                MessageBox.Show("Plugin updated", "Diorama", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.Yes);
                CleanUp();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unhandled error occurred: {ex.Message}", "Diorama", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void CreateNewBOS()
        {
            try
            {
                bool save = false;
                if (_newReferenceFound)
                {
                    bool keepGoing = MessageBox.Show($"New references found. The BOS ini will only contain updated existing references. Continue?", "Diorama", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No) == MessageBoxResult.Yes;
                    if (!keepGoing)
                        return;
                }
                save = WriteBOS();

                if (!save)
                    return;

                MessageBox.Show("BOS ini created", "Diorama", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.Yes);
                CleanUp();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unhandled error occurred: {ex.Message}", "Diorama", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Check if the profile should be deleted. If yes, delete, clear all references and remove the profile from the list
        private void CleanUp()
        {
            bool deleteProfile = MessageBox.Show($"Delete [{SelectedProfile}] profile?", "Diorama", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No) == MessageBoxResult.Yes;
            if (deleteProfile)
            {
                string toDelete = $@"{_profileDir}\{SelectedProfile}.json";
                References.Clear();
                Profiles.Remove(SelectedProfile);
                SelectedProfile = Profiles.FirstOrDefault();
                File.Delete(toDelete);
            }
        }

        private bool SaveNewESP()
        {
            bool save = FileIO.TrySaveFileDialog("Elder Scrolls Plugin (*.esp)|*.esp",
                                            "Choose open location",
                                            SelectedProfile + ".esp",
                                            _defaultDirectory,
                                            out string saveFile);

            if (!save)
                return false;
            return ESP.WriteESP(_outputMod, _updatedReferences, Masters, Author, Description, saveFile, Progress);
        }

        private bool UpdateExistingESP()
        {
            bool open = FileIO.TryOpenFileDialog("Elder Scrolls Plugin (*.esp)|*.esp",
                                            "Choose open location",
                                            SelectedProfile + ".esp",
                                            _defaultDirectory,
                                            out string saveFile);

            if (!open)
                return false;
            return ESP.WriteESP(_outputMod, _updatedReferences, Masters, Author, Description, saveFile, Progress);
        }

        private bool WriteBOS()
        {
            bool save = FileIO.TrySaveFileDialog("INI file (*.ini)|*.ini",
                                            "Choose open location",
                                            SelectedProfile + "_SWAP.ini",
                                            _defaultDirectory,
                                            out string saveFile);

            if (!save)
                return false;

            // If user has added _SWAP themselves, remove it as we are handling that ourselves down the line
            string outputName = Path.GetFileNameWithoutExtension(saveFile.Replace("_SWAP", ""));

            using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
            using var linkCache = env.LoadOrder.ListedOrder.Where(lo => lo.Enabled && lo.ModKey.Name != outputName).ToImmutableLinkCache();
            foreach (var updated in _updatedReferences)
            {
                ESP.UpdateReference(_outputMod, linkCache, updated, Progress);
            }

            BOS.Write(linkCache, _updatedReferences, saveFile);

            return true;
        }
    }
}
