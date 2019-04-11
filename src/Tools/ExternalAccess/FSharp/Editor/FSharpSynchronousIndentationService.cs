using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor
{
    [Shared]
    [ExportLanguageService(typeof(ISynchronousIndentationService), LanguageNames.FSharp)]
    internal class FSharpSynchronousIndentationService : ISynchronousIndentationService
    {
        private readonly IFSharpSynchronousIndentationService _service;

        [ImportingConstructor]
        public FSharpSynchronousIndentationService(IFSharpSynchronousIndentationService service)
        {
            _service = service;
        }

        public CodeAnalysis.Editor.IndentationResult? GetDesiredIndentation(Document document, int lineNumber, CancellationToken cancellationToken)
        {
            var result = _service.GetDesiredIndentation(document, lineNumber, cancellationToken);
            if (result.HasValue)
            {
                return new CodeAnalysis.Editor.IndentationResult(result.Value.BasePosition, result.Value.Offset);
            }
            else
            {
                return null;
            }
        }
    }
}
