using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.Dialog
{
    internal class ClassPullerWithDialog
    {
        internal async Task<Solution> ComputeChangedSolution(
            PullTargetsResult result,
            SemanticModel semanticModel,
            Document contextDocument,
            ICodeGenerationService codeGenerationService,
            CancellationToken cancellationToken)
        {
            var targetDeclaringSyntax = await result.Target.DeclaringSyntaxReferences.First().GetSyntaxAsync(cancellationToken);

            var solution = contextDocument.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var targetDocumentEditor = await solutionEditor.GetDocumentEditorAsync(solution.GetDocument(targetDeclaringSyntax.SyntaxTree).Id, cancellationToken).ConfigureAwait(false);

            var symbolsToPullUp = new List<ISymbol>();
            var changeTargetAbstract = false;

            // Remove node
            targetDeclaringSyntax = RemoveMembers(result.SelectedMembers.
                Where(memberSelectionPair => !memberSelectionPair.makeAbstract).
                Select(memberSelectionPair => memberSelectionPair.member), targetDeclaringSyntax);
            
            // Add members
            if (result.Target.IsAbstract)
            {
                foreach ((var symbol, var makeAbstract) in result.SelectedMembers)
                {
                    if (makeAbstract && !symbol.IsAbstract)
                    {
                        symbolsToPullUp.Add(GetAbstractMemberSymbol(symbol, codeGenerationService));
                    }
                    else
                    {
                        symbolsToPullUp.Add(symbol);
                    }
                }
            }
            else if (result.Target.IsStatic)
            {
                foreach ((var symbol, var makeAbstract) in result.SelectedMembers)
                {
                    // TODO: What to pull Up if the member is abstract?
                    symbolsToPullUp.Add(symbol);
                }
            }
            else
            {
                foreach ((var symbol, var makeAbstract) in result.SelectedMembers)
                {
                    if (symbol.IsAbstract || makeAbstract)
                    {
                        changeTargetAbstract = true;
                    }

                    if (!symbol.IsAbstract && makeAbstract)
                    {
                        symbolsToPullUp.Add(GetAbstractMemberSymbol(symbol, codeGenerationService));
                    }
                }
            }
            var changedTarget = targetDeclaringSyntax;
            if (changeTargetAbstract)
            {
                changedTarget = codeGenerationService.UpdateDeclarationModifiers(targetDeclaringSyntax,
                    new SyntaxToken[] { SyntaxFactory.Token(SyntaxKind.AbstractKeyword) });
            }

            changedTarget = codeGenerationService.AddMembers(targetDeclaringSyntax, symbolsToPullUp);
            targetDocumentEditor.ReplaceNode(targetDeclaringSyntax, changedTarget);
            return solutionEditor.GetChangedSolution();
        }

        private SyntaxNode RemoveMembers(IEnumerable<ISymbol> symbolsToBeRemoved, SyntaxNode targetSyntaxNode)
        {
            // TODO: How to locate the right position of the syntax from symbol?
            // Also the remove should behave right for multiple declaration on same line
            // If public int i, j, k are all need 
            var syntaxTobeRemoved = symbolsToBeRemoved.Select(symbol => symbol.DeclaringSyntaxReferences).
                Where(references => references.Length > 0).Select(references => references.First().GetSyntax());

            foreach (var node in syntaxTobeRemoved)
            {
                targetSyntaxNode = targetSyntaxNode.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
            }
            return targetSyntaxNode;
        }

        private ISymbol GetAbstractMemberSymbol(ISymbol memberSymbol, ICodeGenerationService codeGenerationService)
        {
            var modifier = new DeclarationModifiers(isAbstract:true);
            if (memberSymbol is IMethodSymbol methodSymbol)
            {
                return CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, modifiers: modifier);
            }
            else if (memberSymbol is IPropertySymbol propertySymbol)
            {
                return CodeGenerationSymbolFactory.CreatePropertySymbol(propertySymbol, modifiers: modifier);
            }
            else if (memberSymbol is IEventSymbol eventSymbol)
            {
                return CodeGenerationSymbolFactory.CreateEventSymbol(eventSymbol, modifiers: modifier);
            }
            else if (memberSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeKind == TypeKind.Class)
            {
                return CodeGenerationSymbolFactory.
                    CreateNamedTypeSymbol(namedTypeSymbol.GetAttributes(), namedTypeSymbol.DeclaredAccessibility, modifier, namedTypeSymbol.TypeKind, namedTypeSymbol.Name, namedTypeSymbol.TypeParameters, namedTypeSymbol.BaseType, namedTypeSymbol.Interfaces, namedTypeSymbol.SpecialType, namedTypeSymbol.GetMembers());
            }
            else
            {
                throw new ArgumentException($"{nameof(memberSymbol)} should be method, property, event, indexer or class");
            }
        }
    }
}
