using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor
{
    [ExportContentTypeLanguageService(LanguageNames.FSharp, LanguageNames.FSharp), Shared]
    internal class FSharpContentTypeLanguageService : IContentTypeLanguageService
    {
        private readonly IContentTypeRegistryService _contentTypeRegistry;

        [ImportingConstructor]
        public FSharpContentTypeLanguageService(IContentTypeRegistryService contentTypeRegistry)
        {
            _contentTypeRegistry = contentTypeRegistry;
        }

        public IContentType GetDefaultContentType()
        {
            return _contentTypeRegistry.GetContentType(LanguageNames.FSharp);
        }
    }
}
