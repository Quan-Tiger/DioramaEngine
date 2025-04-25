using Microsoft.Win32;

namespace DioramaEngine.Services
{
    internal class FileIO
    {
        internal static bool TrySaveFileDialog(string filter, string title, string filename, string defaultDirectory, out string filepath)
        {
            SaveFileDialog sfd = new()
            {
                Title = title,
                Filter = filter,
                FileName = filename,
                DefaultDirectory = defaultDirectory
            };

            bool result = sfd.ShowDialog() == true;
            filepath = sfd.FileName;
            return result;
        }

        internal static bool TryOpenFileDialog(string filter, string title, string filename, string defaultDirectory, out string filepath)
        {
            OpenFileDialog ofd = new()
            {
                Title = title,
                Filter = filter,
                FileName = filename,
                DefaultDirectory = defaultDirectory
            };

            bool result = ofd.ShowDialog() == true;
            filepath = ofd.FileName;
            return result;
        }
    }
}
