using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.Dialog
{
    internal class InterfacePullerWithDialog
    {
        internal async Task<Solution> ComputeChangedSolution(
            PullTargetsResult result,
            SemanticModel semanticModel,
            Document contextDocument,
            ICodeGenerationService codeGenerationService,
            CancellationToken cancellationToken)
        {
            var targetDeclaringSyntax = await result.Target.DeclaringSyntaxReferences.First().GetSyntaxAsync(cancellationToken);

            var nodesToPullUp = new List<SyntaxNode>();

            var solution = contextDocument.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var targetDocumentEditor = await solutionEditor.GetDocumentEditorAsync(solution.GetDocument(targetDeclaringSyntax.SyntaxTree).Id, cancellationToken).ConfigureAwait(false);

            foreach((var memberSymbol, var _) in result.SelectedMembers)
            {
                if (memberSymbol.DeclaredAccessibility != Accessibility.Public)
                {
                    await UpdateAccessibilityToPublic(memberSymbol, codeGenerationService, targetDocumentEditor, cancellationToken);
                }

                if (memberSymbol is IMethodSymbol methodSymbol)
                {
                    nodesToPullUp.Add(codeGenerationService.AddMethod(targetDeclaringSyntax, methodSymbol));
                }
                else if (memberSymbol is IEventSymbol eventSymbol)
                {
                    var option = new CodeGenerationOptions(generateMethodBodies : false);
                    nodesToPullUp.Add(codeGenerationService.AddEvent(targetDeclaringSyntax, eventSymbol));
                }
                else if (memberSymbol is IPropertySymbol propertySymbol)
                {
                    nodesToPullUp.Add(codeGenerationService.AddProperty(targetDeclaringSyntax, propertySymbol));
                }
                else
                {
                    throw new ArgumentException($"nameof {memberSymbol} should be method, event or property");
                }
            }

            foreach (var node in nodesToPullUp)
            {
                targetDocumentEditor.AddMember(targetDeclaringSyntax, node);
            }

            return solutionEditor.GetChangedSolution();
        }

        private async Task UpdateAccessibilityToPublic(
            ISymbol memberSymbol,
            ICodeGenerationService codeGenerationService,
            DocumentEditor editor,
            CancellationToken cancellationToken)
        {
            var memberSyntax = await memberSymbol.DeclaringSyntaxReferences.First().GetSyntaxAsync(cancellationToken);
            var publicMemberSyntax = codeGenerationService.UpdateDeclarationAccessibility(memberSyntax, Accessibility.Public);
            editor.ReplaceNode(memberSyntax, publicMemberSyntax);
        }
    }
}
