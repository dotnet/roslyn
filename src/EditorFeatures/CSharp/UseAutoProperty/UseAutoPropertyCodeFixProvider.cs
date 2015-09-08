using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UseAutoProperty
{
    [Shared]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "UseAutoProperty")]
    internal class UseAutoPropertyCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UseAutoPropertyAnalyzer.UseAutoProperty);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var equivalenceKey = diagnostic.Properties[nameof(AnalysisResult.SymbolEquivalenceKey)];

                context.RegisterCodeFix(
                    new UseAutoPropertyCodeAction(
                        CSharpEditorResources.UseAutoProperty,
                        c => ProcessResult(context, diagnostic, c),
                        equivalenceKey),
                    diagnostic);
            }

            return Task.FromResult(false);
        }

        private async Task<Solution> ProcessResult(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var locations = diagnostic.AdditionalLocations;
            var propertyLocation = locations[0];
            var declaratorLocation = locations[1];

            var declaratorTree = declaratorLocation.SourceTree;
            var declaratorDocument = context.Document.Project.GetDocument(declaratorTree);
            var declaratorTreeRoot = await declaratorDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var propertyTree = propertyLocation.SourceTree;
            var propertyDocument = context.Document.Project.GetDocument(propertyTree);
            var propertyTreeRoot = await propertyDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var solution = declaratorDocument.Project.Solution;

            var declarator = declaratorTreeRoot.FindNode(declaratorLocation.SourceSpan);
            var fieldDeclaration = (FieldDeclarationSyntax)declarator.Parent.Parent;

            var nodeToRemove = fieldDeclaration.Declaration.Variables.Count > 1 ? declarator : (SyntaxNode)fieldDeclaration;

            var propertyDeclaration = propertyTreeRoot.FindNode(propertyLocation.SourceSpan).FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            var updatedProperty = UpdateProperty(propertyDeclaration);

            const SyntaxRemoveOptions options = SyntaxRemoveOptions.KeepUnbalancedDirectives | SyntaxRemoveOptions.AddElasticMarker;
            if (declaratorDocument == propertyDocument)
            {
                // Same file.  Have to do this in a slightly complicated fashion.
                var editor = new SyntaxEditor(declaratorTreeRoot, declaratorDocument.Project.Solution.Workspace);
                editor.RemoveNode(nodeToRemove, options);
                editor.ReplaceNode(propertyDeclaration, updatedProperty);

                return solution.WithDocumentSyntaxRoot(
                    declaratorDocument.Id, editor.GetChangedRoot());
            }
            else
            {
                // In different files.  Just update both files.
                var newFieldTreeRoot = declaratorTreeRoot.RemoveNode(nodeToRemove, options);
                var newPropertyTreeRoot = propertyTreeRoot.ReplaceNode(propertyDeclaration, updatedProperty);

                var updatedSolution = solution.WithDocumentSyntaxRoot(declaratorDocument.Id, newFieldTreeRoot);
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(propertyDocument.Id, newPropertyTreeRoot);

                return updatedSolution;
            }
        }

        private PropertyDeclarationSyntax UpdateProperty(PropertyDeclarationSyntax propertyDeclaration)
        {
            return propertyDeclaration.WithAccessorList(UpdateAccessorList(propertyDeclaration.AccessorList));
        }

        private AccessorListSyntax UpdateAccessorList(AccessorListSyntax accessorList)
        {
            return accessorList.WithAccessors(SyntaxFactory.List(GetAccessors(accessorList.Accessors)));
        }

        private IEnumerable<AccessorDeclarationSyntax> GetAccessors(SyntaxList<AccessorDeclarationSyntax> accessors)
        {
            foreach (var accessor in accessors)
            {
                yield return accessor.WithBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }
        }

        private class UseAutoPropertyCodeAction : CodeAction.SolutionChangeAction
        {
            public UseAutoPropertyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey)
                : base(title, createChangedSolution, equivalenceKey)
            {
            }
        }
    }
}