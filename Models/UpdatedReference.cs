using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace DioramaEngine.Models
{
    internal class UpdatedReference
    {
        internal DioramaRef Reference { get; set; }
        internal IFormLinkGetter<IPlacedObjectGetter> Link { get; set; }

        internal UpdatedReference(DioramaRef reference, IFormLinkGetter<IPlacedObjectGetter> link)
        {
            Reference = reference;
            Link = link;
        }
    }
}
