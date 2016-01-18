using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MakeMethodSynchronous
{
    internal abstract class AbstractMakeMethodSynchronousCodeFixProvider : CodeFixProvider
    {
        protected abstract bool IsMethodOrAnonymousFunction(SyntaxNode node);
        protected abstract SyntaxNode RemoveAsyncTokenAndFixReturnType(IMethodSymbol methodSymbolOpt, SyntaxNode node, ITypeSymbol taskType, ITypeSymbol taskOfTType);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();

            var token = diagnostic.Location.FindToken(cancellationToken);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            for (var node = token.Parent; node != null; node = node.Parent)
            {
                if (IsMethodOrAnonymousFunction(node))
                {
                    context.RegisterCodeFix(new MyCodeAction(FeaturesResources.Make_method_synchronous,
                        c => FixNodeAsync(context.Document, node, c)), diagnostic);
                    return;
                }
            }
        }

        private const string AsyncSuffix = "Async";

        private async Task<Solution> FixNodeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            // See if we're on an actual method declaration (otherwise we're on a lambda declaration).
            // If we're on a method declaration, we'll get an IMethodSymbol back.  In that case, check
            // if it has the 'Async' suffix, and remove that suffix if so.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbolOpt = semanticModel.GetDeclaredSymbol(node) as IMethodSymbol;

            if (methodSymbolOpt?.MethodKind == MethodKind.Ordinary && 
                methodSymbolOpt.Name.Length > AsyncSuffix.Length && 
                methodSymbolOpt.Name.EndsWith(AsyncSuffix))
            {
                return await RenameThenRemoveAsyncTokenAsync(document, node, methodSymbolOpt, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await RemoveAsyncTokenAsync(document, methodSymbolOpt, node, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<Solution> RenameThenRemoveAsyncTokenAsync(Document document, SyntaxNode node, IMethodSymbol methodSymbol, CancellationToken cancellationToken)
        {
            var name = methodSymbol.Name;
            var newName = name.Substring(0, name.Length - AsyncSuffix.Length);
            var solution = document.Project.Solution;
            var options = solution.Workspace.Options;

            // Store the path to this node.  That way we can find it post rename.
            var syntaxPath = new SyntaxPath(node);

            // Rename the method to remove the 'Async' suffix, then remove the 'async' keyword.
            var newSolution = await Renamer.RenameSymbolAsync(solution, methodSymbol, newName, options, cancellationToken).ConfigureAwait(false);
            var newDocument = newSolution.GetDocument(document.Id);
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            SyntaxNode newNode;
            if (syntaxPath.TryResolve<SyntaxNode>(newRoot, out newNode))
            {
                var semanticModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var newMethod = (IMethodSymbol)semanticModel.GetDeclaredSymbol(newNode, cancellationToken);
                return await RemoveAsyncTokenAsync(newDocument, newMethod, newNode, cancellationToken).ConfigureAwait(false);
            }

            return newSolution;
        }

        private async Task<Solution> RemoveAsyncTokenAsync(Document document, IMethodSymbol methodSymbolOpt, SyntaxNode node, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

            var newNode = RemoveAsyncTokenAndFixReturnType(methodSymbolOpt, node, taskType, taskOfTType)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(node, newNode);

            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument.Project.Solution;
        }

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(
                string title,
                Func<CancellationToken, Task<Solution>> createChangedSolution,
                string equivalenceKey = null) : base(title, createChangedSolution, equivalenceKey)
            {
            }
        }
    }
}
