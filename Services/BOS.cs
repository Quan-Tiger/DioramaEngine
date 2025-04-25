using DioramaEngine.Models;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using System.IO;
using System.Text;

namespace DioramaEngine.Services
{
    internal class BOS
    {
        internal static void Write(ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, List<UpdatedReference> updated, string output)
        {
            List<DioramaRef> references = [];
            List<DioramaRef> transforms = [];
            PopulateLists(linkCache, updated, references, transforms);

            using StreamWriter sw = new(output);

            // Write out the list of references that are full base swaps
            if (references.Count > 0)
            {
                sw.WriteLine("[References]");
                foreach (var form in references)
                {
                    sw.WriteLine(GetLine(form, true));
                }
            }

            // Write out the list of references that are just transforms
            if (transforms.Count > 0)
            {
                if (references.Count > 0)
                {
                    sw.WriteLine("");
                }

                sw.WriteLine("[Transforms]");
                foreach (var transform in transforms)
                {
                    sw.WriteLine(GetLine(transform, false));
                }
            }

        }

        // Split the list of updated references between those that are base swaps ([References]) and those that are just [Transforms]
        private static void PopulateLists(ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, List<UpdatedReference> updated, List<DioramaRef> references, List<DioramaRef> transforms)
        {
            foreach (var update in updated)
            {
                DioramaRef reference = update.Reference;
                IFormLinkGetter<IPlacedObjectGetter> formLink = update.Link;

                string baseId = DioramaRef.GetFormattedFormID(reference.BaseFormId, false);
                if (FormKey.TryFactory($"{baseId}:{reference.BaseFormMod}", out var baseFormKey))
                {
                    IFormLinkGetter<IPlaceableObjectGetter> baseFormLink = baseFormKey.ToLinkGetter<IPlaceableObjectGetter>();
                    if (baseFormLink.TryResolveContext<ISkyrimMod, ISkyrimModGetter, IPlaceableObject, IPlaceableObjectGetter>(linkCache, out var baseContext))
                    {
                        if (formLink.TryResolveContext<ISkyrimMod, ISkyrimModGetter, IPlacedObject, IPlacedObjectGetter>(linkCache, out var context))
                        {
                            // Compare the base object IDs of the original and the updated
                            if (baseContext.Record.FormKey.IDString() != context.Record.Base.FormKey.IDString())
                            {
                                references.Add(reference);
                            }
                            else
                            {
                                transforms.Add(reference);
                            }
                        }
                    }
                }
            }
        }

        //0xXXXXXX~original.esm|0xYYYYYY~updated.esp|posA(6258.066,1744.7335,344.7038),rotR(0,-0,-1.3525497),scale(1)
        private static string GetLine(DioramaRef form, bool replaceBase)
        {
            StringBuilder sb = new StringBuilder();
            string formId = DioramaRef.GetFormattedFormID(form.FormID, form.IsFromESL);
            sb.Append($"0x{formId}~{form.Mod}");

            if (replaceBase)
            {
                string baseId = DioramaRef.GetFormattedFormID(form.BaseFormId, false);
                sb.Append($"|0x{baseId}~{form.BaseFormMod}");
            }

            string posY = form.IsDisabled ? "-30000.0" : form.PosY.ToString(); // Safe disable moves the reference deep down below the map
            string pos = $"posA({form.PosX},{posY},{form.PosZ})";
            string rot = $"rotR({form.RotX},{form.RotY},{form.RotZ})";
            string scale = $"scale({form.Scale})";
            sb.Append($"|{pos},{rot},{scale}");

            string flags = form.IsDisabled ? "flags(0x00000800)" : ""; // flag as initially disabled
            if (!string.IsNullOrEmpty(flags))
            {
                sb.Append($",{flags}");
            }

            return sb.ToString();
        }
    }
}
