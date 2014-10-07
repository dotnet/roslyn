using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Utilities;

namespace Roslyn.Services.Shared.RemoveUnnecessaryImports
{
    internal abstract class AbstractRemoveUnnecessaryImportsService : IRemoveUnnecessaryImportsService
    {
        protected abstract void RemoveUnnecessaryImportsWorker(IDocument document, CancellationToken cancellationToken, out CommonSyntaxNode rootNode, out IEnumerable<CommonSyntaxNode> unnecessaryImports, out IEnumerable<TextSpan> formattingSpans);
        protected abstract bool CodeMeaningChanged(IDocument oldDocument, IDocument newDocument, CancellationToken cancellationToken);

        public RemoveUnnecessaryImportsResult RemoveUnnecessaryImports(
            IDocument document,
            CancellationToken cancellationToken)
        {
            CommonSyntaxNode rootNode;
            IEnumerable<CommonSyntaxNode> unnecessaryImports;
            IEnumerable<TextSpan> formattingSpans;
            RemoveUnnecessaryImportsWorker(document, cancellationToken, out rootNode, out unnecessaryImports, out formattingSpans);

            if (rootNode != null)
            {
                rootNode = rootNode.Format(formattingSpans, document.GetFormattingOptions(), cancellationToken: cancellationToken).GetFormattedRoot(cancellationToken);
                var newDocument = document.UpdateSyntaxRoot(rootNode);

                var oldErrorCount = document.GetSemanticModel(cancellationToken).GetDiagnostics(cancellationToken).Count();
                var newErrorCount = newDocument.GetSemanticModel(cancellationToken).GetDiagnostics(cancellationToken).Count();
                if (newErrorCount <= oldErrorCount &&
                    !CodeMeaningChanged(document, newDocument, cancellationToken))
                {
                    return new RemoveUnnecessaryImportsResult(newDocument, unnecessaryImports);
                }
            }

            return new RemoveUnnecessaryImportsResult(document, SpecializedCollections.EmptyEnumerable<CommonSyntaxNode>());
        }
    }
}