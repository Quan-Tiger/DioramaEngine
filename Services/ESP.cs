using DioramaEngine.Models;
using Mutagen.Bethesda.Plugins.Cache.Internals.Implementations;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Mutagen.Bethesda;
using System.IO;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Environments;

namespace DioramaEngine.Services
{
    internal class ESP
    {

        // Convert our reference into a Mutagen form link
        internal static IFormLinkGetter<IPlacedObjectGetter>? CollectFormLink(DioramaRef reference, IncrementProgress progress)
        {
            string baseId = DioramaRef.GetFormattedFormID(reference.BaseFormId, false);
            if (FormKey.TryFactory($"{baseId}:{reference.BaseFormMod}", out var baseFormKey))
            {
                progress.Increment();
                string formID = DioramaRef.GetFormattedFormID(reference.FormID, reference.IsFromESL);
                if (FormKey.TryFactory($"{formID}:{reference.Mod}", out var formKey))
                {
                    return formKey.ToLinkGetter<IPlacedObjectGetter>();
                }
            }

            return null;
        }

        internal static void UpdateReference(ISkyrimMod outputMod, ImmutableLoadOrderLinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, UpdatedReference updated, IncrementProgress progress)
        {
            DioramaRef reference = updated.Reference;
            IFormLinkGetter<IPlacedObjectGetter> formLink = updated.Link;

            // First try and find the base object's form key
            string baseId = DioramaRef.GetFormattedFormID(reference.BaseFormId, false);
            if (FormKey.TryFactory($"{baseId}:{reference.BaseFormMod}", out var baseFormKey))
            {
                progress.Increment();

                // Try and get the mod context of the reference. This gives us the most recent mod that updated it.
                if (formLink.TryResolveContext<ISkyrimMod, ISkyrimModGetter, IPlacedObject, IPlacedObjectGetter>(linkCache, out var context))
                {
                    progress.Increment();

                    // Finally try and get the mod context for the base object
                    IFormLinkGetter<IPlaceableObjectGetter> baseFormLink = baseFormKey.ToLinkGetter<IPlaceableObjectGetter>();
                    if (baseFormLink.TryResolveContext<ISkyrimMod, ISkyrimModGetter, IPlaceableObject, IPlaceableObjectGetter>(linkCache, out _))
                    {
                        IPlacedObject dupe = context.GetOrAddAsOverride(outputMod); // Duplicate the reference as an override into our new mod
                        UpdateObject(reference, baseFormLink, dupe); // Update the duplicated ref with our new properties
                    }
                }
            }
        }

        // Add an new reference to our mod
        internal static bool AddNewReference(ISkyrimMod outputMod, ImmutableLoadOrderLinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, HashSet<string> knownMasters, DioramaRef reference, IncrementProgress progress)
        {
            bool newReferenceAdded = false; // default return value

            // First try and get the cell's form key
            string cellId = DioramaRef.GetFormattedFormID(reference.Cell, false);
            if (FormKey.TryFactory($"{cellId}:{reference.CellMod}", out var cellFormKey))
            {
                // Get the mod context of the reference. This gives us the most recent mod that updated it.
                var cellContext = linkCache.ResolveContext<ICell, ICellGetter>(cellFormKey);

                knownMasters.Add(Path.GetFileNameWithoutExtension(cellContext.ModKey.Name)); // Add the cell's most recent mod to the list of known masters
                ICell dupe = cellContext.GetOrAddAsOverride(outputMod); // Duplicate the cell's reference as an override into our new mod

                // First try and find the base object's form key
                string baseId = DioramaRef.GetFormattedFormID(reference.BaseFormId, false);
                if (FormKey.TryFactory($"{baseId}:{reference.BaseFormMod}", out var baseFormKey))
                {
                    progress.Increment();

                    // try and get the mod context of the base object. This gives us the most recent mod that updated it.
                    IFormLinkGetter<IPlaceableObjectGetter> baseFormLink = baseFormKey.ToLinkGetter<IPlaceableObjectGetter>();
                    if (baseFormLink.TryResolveContext<ISkyrimMod, ISkyrimModGetter, IPlaceableObject, IPlaceableObjectGetter>(linkCache, out var baseContext))
                    {
                        progress.Increment();
                        knownMasters.Add(baseContext.ModKey.Name);  // Add the cell's most recent mod to the list of known masters

                        // Create a new reference and update it's properties
                        IPlacedObject placedObject = new PlacedObject(outputMod.GetNextFormKey(), SkyrimRelease.SkyrimSE);
                        UpdateObject(reference, baseFormLink, placedObject);

                        // Copy the new reference into our overridden cell
                        Cell cell = new(outputMod) { Temporary = [placedObject] };
                        var onlyTemporaryMask = new Cell.TranslationMask(defaultOn: false)
                        {
                            Temporary = true
                        };
                        dupe.DeepCopyIn(cell, onlyTemporaryMask);
                        newReferenceAdded = true; // New reference successfully added
                    }
                }
            }

            return newReferenceAdded;
        }

        // Update the properties of an object based on the reference we got from the JSON
        internal static void UpdateObject(DioramaRef reference, IFormLinkGetter<IPlaceableObjectGetter> baseFormLink, IPlacedObject placedObject)
        {
            placedObject.Base = baseFormLink.AsNullable();
            placedObject.Placement = new Placement()
            {
                Position = new P3Float(reference.PosX, reference.PosY, reference.PosZ),
                Rotation = new P3Float(reference.RotX, reference.RotY, reference.RotZ)
            };
            placedObject.Scale = reference.Scale;

            if (reference.IsDisabled)
                placedObject.Disable(IPlaced.DisableType.SafeDisable);
        }

        // Write the ESP file to disk
        internal static bool WriteESP(ISkyrimMod outputMod, List<UpdatedReference> updatedReferences, List<Master> masters, string author, string description, string saveFile, IncrementProgress progress)
        {
            string outputFile = Path.GetFileName(saveFile);
            string outputName = Path.GetFileNameWithoutExtension(saveFile);

            // When creating the link cache, only take into account enabled mods and any mod with an identical name (so we don't add ourselves as master)
            using (var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE))
            using (var linkCache = env.LoadOrder.ListedOrder.Where(lo => lo.Enabled && lo.ModKey.Name != outputName).ToImmutableLinkCache())
            {
                foreach (var updated in updatedReferences)
                {
                    UpdateReference(outputMod, linkCache, updated, progress);
                }
            }

            // Either create a new mod or get the existing mod
            SkyrimMod mod = File.Exists(saveFile) && ModKey.TryFromFileName(outputFile, out var modKey)
                ? new SkyrimMod(modKey, SkyrimRelease.SkyrimSE)
                : new SkyrimMod(ModKey.FromFileName(outputFile), SkyrimRelease.SkyrimSE);

            // Copy all of our new and updated references into the mod
            mod.DeepCopyIn(outputMod);

            TryFlagAsESL(mod); // If the mod passes the checks, automatically flag it as ESL

            mod.ModHeader.Author = author;
            mod.ModHeader.Description = description;


            // TODO: Throw error if mod already exists and set to master or figure something else out

            mod.BeginWrite
                .ToPath(saveFile)
                .WithNoLoadOrder()
                .NoNextFormIDProcessing()
                .WithExtraIncludedMasters(masters.Where(m => m.IsChecked && m.Key.Name != outputName).Select(m => m.Key))
                .Write();

            return true;
        }


        internal static void TryFlagAsESL(ISkyrimMod mod)
        {
            uint uintMin = 0x800;
            uint uintMax = 0xFFF;

            // If any record has a form key beyond the valid ESL bounds then break out of the function
            foreach (var rec in mod.EnumerateMajorRecords())
            {
                if (!rec.FormKey.ModKey.Equals(mod.ModKey)) continue;

                if (rec.FormKey.ID < uintMin || rec.FormKey.ID > uintMax)
                    return;
            }
            
            // If we've gotten this far then the mod has passed all the checks
            mod.ModHeader.Flags |= SkyrimModHeader.HeaderFlag.Small;
        }
    }
}
