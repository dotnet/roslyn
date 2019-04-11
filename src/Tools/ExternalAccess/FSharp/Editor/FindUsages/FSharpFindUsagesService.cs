using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.FindUsages
{
    [Shared]
    [ExportLanguageService(typeof(IFindUsagesService), LanguageNames.FSharp)]
    internal class FSharpFindUsagesService : IFindUsagesService
    {
        private readonly IFSharpFindUsagesService _service;

        [ImportingConstructor]
        public FSharpFindUsagesService(IFSharpFindUsagesService service)
        {
            _service = service;
        }

        public Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context)
        {
            return _service.FindImplementationsAsync(document, position, new FSharpFindUsagesContext(context));
        }

        public Task FindReferencesAsync(Document document, int position, IFindUsagesContext context)
        {
            return _service.FindReferencesAsync(document, position, new FSharpFindUsagesContext(context));
        }
    }
}
