using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.FxCopDiagnosticFixers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
{
    /// <summary>
    /// CA1821: Remove empty finalizers
    /// </summary>
    [ExportCodeFixProvider(CA1821DiagnosticAnalyzer.RuleId, LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class CA1821CodeFixProvider : CodeFixProviderBase
    {
        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return SpecializedCollections.SingletonEnumerable(CA1821DiagnosticAnalyzer.RuleId);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.RemoveEmptyFinalizers;
        }

        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, string diagnosticId, CancellationToken cancellationToken)
        {
            return Task.FromResult(document.WithSyntaxRoot(root.RemoveNode(nodeToFix, SyntaxRemoveOptions.KeepNoTrivia)));
        }
    }
}