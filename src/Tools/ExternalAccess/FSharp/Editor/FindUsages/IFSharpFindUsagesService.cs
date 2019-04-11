using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.FindUsages
{
    public interface IFSharpFindUsagesService
    {
        /// <summary>
        /// Finds the references for the symbol at the specific position in the document,
        /// pushing the results into the context instance.
        /// </summary>
        Task FindReferencesAsync(Document document, int position, IFSharpFindUsagesContext context);

        /// <summary>
        /// Finds the implementations for the symbol at the specific position in the document,
        /// pushing the results into the context instance.
        /// </summary>
        Task FindImplementationsAsync(Document document, int position, IFSharpFindUsagesContext context);
    }
}
