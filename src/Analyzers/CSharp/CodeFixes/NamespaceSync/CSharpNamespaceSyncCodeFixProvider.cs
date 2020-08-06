using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.NamespaceSync
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddBraces), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddAwait)]
    internal class CSharpNamespaceSyncCodeFixProvider: SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpNamespaceSyncCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => throw new NotImplementedException();

        internal override CodeFixCategory CodeFixCategory => throw new NotImplementedException();

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
           return TryChangeTopLevelNamespacesAsync(document, _analysis.TargetNamespace, cancellationToken);
        }

        private sealed class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Add_braces, createChangedDocument, CSharpAnalyzersResources.Add_braces)
            {
            }
        }

        public async Task<Solution> TryChangeTopLevelNamespacesAsync(
            Document document,
            string targetNamespace,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Don't descend into anything other than top level declarations from the root.
            // ChangeNamespaceService only controls top level declarations right now.
            // Don't use namespaces that already match the target namespace
            var originalNamespaceDeclarations = await GetTopLevelNamespacesAsync(document, cancellationToken).ConfigureAwait(false);

            if (originalNamespaceDeclarations.Length == 0)
            {
                return null;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var originalNamespaceName = semanticModel.GetDeclaredSymbol(originalNamespaceDeclarations.First(), cancellationToken).ToDisplayString();
            var solution = document.Project.Solution;

            // Only loop as many top level namespace declarations as we originally had. 
            // Change namespace doesn't change this number, so this helps limit us and
            // rule out namespaces that didn't need to be changed
            for (var i = 0; i < originalNamespaceDeclarations.Length; i++)
            {
                var namespaceName = semanticModel.GetDeclaredSymbol(originalNamespaceDeclarations[i], cancellationToken).ToDisplayString();
                if (namespaceName != originalNamespaceName)
                {
                    // Skip all namespaces that didn't match the original namespace name that 
                    // we were syncing. 
                    continue;
                }
                syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // Since the original namespaces were retrieved before the document was modified
                // get the current top level namespaces. Since we're only renaming namespaces, the 
                // number and index of each is the same.
                var namespaces = await GetTopLevelNamespacesAsync(document, cancellationToken).ConfigureAwait(false);

                var namespaceToRename = namespaces[i];
                solution = await ChangeNamespaceAsync(document, namespaceToRename, targetNamespace, cancellationToken).ConfigureAwait(false);
                document = solution.GetRequiredDocument(document.Id);
            }

            return solution;
            static async Task<ImmutableArray<SyntaxNode>> GetTopLevelNamespacesAsync(Document document, CancellationToken cancellationToken)
            {
                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

                return syntaxRoot
                    .DescendantNodes(n => !syntaxFacts.IsDeclaration(n))
                    .Where(n => syntaxFacts.IsNamespaceDeclaration(n))
                    .ToImmutableArray();
            }
        }

        private async Task<Solution> ChangeNamespaceAsync(
           Document document,
           SyntaxNode container,
           string targetNamespace,
           CancellationToken cancellationToken)
        {
            // Make sure given namespace name is valid, "" means global namespace.
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (targetNamespace == null
                || (targetNamespace.Length > 0 && !targetNamespace.Split(s_dotSeparator).All(syntaxFacts.IsValidIdentifier)))
            {
                throw new ArgumentException(nameof(targetNamespace));
            }

            if (!IsValidContainer(container))
            {
                throw new ArgumentException(nameof(container));
            }

            var solution = document.Project.Solution;

            var containersFromAllDocuments = await GetValidContainersFromAllLinkedDocumentsAsync(document, container, cancellationToken).ConfigureAwait(false);
            if (containersFromAllDocuments.IsDefault)
            {
                return solution;
            }

            // No action required if declared namespace already matches target.
            var declaredNamespace = GetDeclaredNamespace(container);
            if (syntaxFacts.StringComparer.Equals(targetNamespace, declaredNamespace))
            {
                return solution;
            }

            // Annotate the container nodes so we can still find and modify them after syntax tree has changed.
            var annotatedSolution = await AnnotateContainersAsync(solution, containersFromAllDocuments, cancellationToken).ConfigureAwait(false);

            // Here's the entire process for changing namespace:
            // 1. Change the namespace declaration, fix references and add imports that might be necessary.
            // 2. Explicitly merge the diff to get a new solution.
            // 3. Remove added imports that are unnecessary.
            // 4. Do another explicit diff merge based on last merged solution.
            //
            // The reason for doing explicit diff merge twice is so merging after remove unnecessary imports can be correctly handled.

            var documentIds = containersFromAllDocuments.SelectAsArray(pair => pair.id);
            var solutionAfterNamespaceChange = annotatedSolution;
            using var _ = PooledHashSet<DocumentId>.GetInstance(out var referenceDocuments);

            foreach (var documentId in documentIds)
            {
                var (newSolution, refDocumentIds) =
                    await ChangeNamespaceInSingleDocumentAsync(solutionAfterNamespaceChange, documentId, declaredNamespace, targetNamespace, cancellationToken)
                        .ConfigureAwait(false);
                solutionAfterNamespaceChange = newSolution;
                referenceDocuments.AddRange(refDocumentIds);
            }

            var solutionAfterFirstMerge = await MergeDiffAsync(solution, solutionAfterNamespaceChange, cancellationToken).ConfigureAwait(false);

            // After changing documents, we still need to remove unnecessary imports related to our change.
            // We don't try to remove all imports that might become unnecessary/invalid after the namespace change, 
            // just ones that fully match the old/new namespace. Because it's hard to get it right and will almost 
            // certainly cause perf issue.
            // For example, if we are changing namespace `Foo.Bar` (which is the only namespace declaration with such name)
            // to `A.B`, the using of name `Bar` in a different file below would remain untouched, even it's no longer valid:
            //
            //      namespace Foo
            //      {
            //          using Bar;
            //          ~~~~~~~~~
            //      }
            //
            // Also, because we may have added different imports to document that triggered the refactoring
            // and the documents that reference affected types declared in changed namespace, we try to remove
            // unnecessary imports separately.

            var solutionAfterImportsRemoved = await RemoveUnnecessaryImportsAsync(
                solutionAfterFirstMerge,
                documentIds,
                GetAllNamespaceImportsForDeclaringDocument(declaredNamespace, targetNamespace),
                cancellationToken).ConfigureAwait(false);

            solutionAfterImportsRemoved = await RemoveUnnecessaryImportsAsync(
                solutionAfterImportsRemoved,
                referenceDocuments.ToImmutableArray(),
                ImmutableArray.Create(declaredNamespace, targetNamespace),
                cancellationToken).ConfigureAwait(false);

            return await MergeDiffAsync(solutionAfterFirstMerge, solutionAfterImportsRemoved, cancellationToken).ConfigureAwait(false);
        }

        protected async Task<ImmutableArray<(DocumentId, SyntaxNode)>> GetValidContainersFromAllLinkedDocumentsAsync(
            Document document,
            SyntaxNode container,
            CancellationToken cancellationToken)
        {
            if (document.Project.FilePath == null
                || document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles
                || await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
            {
                return default;
            }

            TextSpan containerSpan;
            if (container is NamespaceDeclarationSyntax)
            {
                containerSpan = container.Span;
            }
            else if (container is CompilationUnitSyntax)
            {
                // A compilation unit as container means user want to move all its members from global to some namespace.
                // We use an empty span to indicate this case.
                containerSpan = default;
            }
            else
            {
                throw ExceptionUtilities.Unreachable;
            }

            if (!IsSupportedLinkedDocument(document, out var allDocumentIds))
            {
                return default;
            }

            return await TryGetApplicableContainersFromAllDocumentsAsync(document.Project.Solution, allDocumentIds, containerSpan, cancellationToken)
                    .ConfigureAwait(false);
        }

        private static bool IsValidContainer(SyntaxNode container)
            => container is NamespaceDeclarationSyntax;
    }
}
