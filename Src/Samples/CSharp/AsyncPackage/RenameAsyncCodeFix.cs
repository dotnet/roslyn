using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace AsyncPackage
{
    /// <summary>
    /// This codefix adds "Async" to the end of the Method Identifier and does a basic spellcheck in case the user had already tried to type Async
    /// </summary>
    [ExportCodeFixProvider(RenameAsyncAnalyzer.RenameAsyncId, LanguageNames.CSharp)]
    public class RenameAsyncCodeFix : CodeFixProvider
    {
        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return new[] { RenameAsyncAnalyzer.RenameAsyncId };
        }

        public sealed override async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var diagnosticSpan = diagnostics.First().Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            // Return a code action that will invoke the fix. (The name is intentional; that's my sense of humor)
            return new[] { new RenameAsyncCodeAction("Add Async to the end of the method name", c => RenameMethodAsync(document, methodDeclaration, c)) };
        }

        private async Task<Solution> RenameMethodAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync().ConfigureAwait(false);
            var oldSolution = document.Project.Solution;

            var symbol = model.GetDeclaredSymbol(methodDeclaration);

            var oldName = methodDeclaration.Identifier.ToString();
            var newName = string.Empty;

            // Check to see if name already contains Async at the end
            if (HasAsyncSuffix(oldName))
            {
                newName = oldName.Substring(0, oldName.Length - 5) + "Async";
            }
            else
            {
                newName = oldName + "Async";
            }

            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, symbol, newName, document.Project.Solution.Workspace.Options).ConfigureAwait(false);

            // Gets all identifiers to check for renaming conflicts
            var syntaxTree = await document.GetSyntaxTreeAsync().ConfigureAwait(false);
            var descendentTokens = syntaxTree.GetRoot().DescendantTokens();

            var usedNames = descendentTokens.Where(token => token.IsKind(SyntaxKind.IdentifierToken)).Select(token => token.Value);

            while (usedNames.Contains(newName))
            {
                // No codefix should be offered if the name conflicts with another method.
                return oldSolution;
            }

            return newSolution;
        }

        /// <summary>
        /// This spellchecker obviously has limitations, but it may be helpful to some.
        /// </summary>
        /// <param name="oldName"></param>
        /// <returns>Returns a boolean of whether or not "Async" may have been in the Method name already but was mispelled.</returns>
        public bool HasAsyncSuffix(string oldName)
        {
            if (oldName.Length >= 5)
            {
                var last5letters = oldName.Substring(oldName.Length - 5);

                // Check case. The A in Async must be capitalized
                if (last5letters.Contains("async") || last5letters.Contains("asinc"))
                {
                    return true;
                }
                else if (((last5letters.Contains("A") || last5letters.Contains("a")) && last5letters.Contains("s") && last5letters.Contains("y")
                    && last5letters.Contains("n") && last5letters.Contains("c")) && !last5letters.ToLower().Equals("scany"))
                {
                    return true; // Basic spellchecker. This is obviously not conclusive, but it may catch a small error if the letters are simply switched around.
                }
            }

            return false;
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        private class RenameAsyncCodeAction : CodeAction
        {
            private Func<CancellationToken, Task<Solution>> generateSolution;
            private string title;

            public RenameAsyncCodeAction(string title, Func<CancellationToken, Task<Solution>> generateSolution)
            {
                this.title = title;
                this.generateSolution = generateSolution;
            }

            public override string Title { get { return title; } }

            protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return base.GetChangedSolutionAsync(cancellationToken);
            }
        }
    }
}