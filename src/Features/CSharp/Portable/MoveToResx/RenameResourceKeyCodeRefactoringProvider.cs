using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.MoveToResx
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(RenameResourceKeyCodeRefactoringProvider)), Shared]
    internal class RenameResourceKeyCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var span = context.Span;

            // Find the identifier at the caret
            var token = root.FindToken(span.Start);
            if (token.Parent is not IdentifierNameSyntax identifierName)
                return;

            // Check if the identifier is the Name of a MemberAccessExpressionSyntax
            if (identifierName.Parent is not MemberAccessExpressionSyntax memberAccess)
                return;

            // Only offer if this is the rightmost identifier in a chain (the resource key)
            if (memberAccess.Name != identifierName)
                return;

            context.RegisterRefactoring(
                CodeAction.Create(
                    "Rename resource key...",
                    c => TriggerRenameAsync(context.Document, identifierName, c),
                    nameof(RenameResourceKeyCodeRefactoringProvider)));
        }

        private static bool IsResourceKeyMemberAccess(IdentifierNameSyntax identifier)
        {
            var parent = identifier.Parent;
            while (parent is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Name == identifier)
                    return true;
                parent = parent.Parent;
            }
            return false;
        }

        private async Task<Solution> TriggerRenameAsync(Document document, IdentifierNameSyntax identifier, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol;
            if (symbol == null)
                return document.Project.Solution;

            // Create RenameOptions to use with the updated RenameSymbolAsync method
            var renameOptions = new SymbolRenameOptions(
                RenameOverloads: true, // Example option, adjust based on your requirements
                RenameInStrings: false,
                RenameInComments: false
            );

            // Use the updated overload of RenameSymbolAsync
            var solution = document.Project.Solution;
            var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, renameOptions, symbol.Name, cancellationToken).ConfigureAwait(false);

            return newSolution;
        }
    }
}
