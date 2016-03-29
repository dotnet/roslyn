using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.PopulateSwitch
{
    internal partial class PopulateSwitchCodeFixProvider : CodeFixProvider
    {
        private class PopulateSwitchFixAllProvider : BatchSimplificationFixAllProvider
        {
            internal static new readonly PopulateSwitchFixAllProvider Instance = new PopulateSwitchFixAllProvider();

            protected override SyntaxNode GetNodeToSimplify(SyntaxNode root, SemanticModel model, Diagnostic diagnostic, Workspace workspace, out string codeActionId, CancellationToken cancellationToken)
            {
                codeActionId = null;
                return GetSwitchStatementNode(root, diagnostic.Location.SourceSpan);
            }

            protected override bool NeedsParentFixup
            {
                get
                {
                    return true;
                }
            }

            protected override async Task<Document> AddSimplifyAnnotationsAsync(Document document, SyntaxNode nodeToSimplify, CancellationToken cancellationToken)
            {
                var switchBlock = nodeToSimplify as SwitchStatementSyntax;
                if (switchBlock == null)
                {
                    return null;
                }

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                return await AddMissingSwitchLabelsAsync(model, document, root, switchBlock).ConfigureAwait(false);
            }
        }
    }
}
