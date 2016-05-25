using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ReplacePropertyWithMethods
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
       Name = nameof(ReplacePropertyWithMethodsCodeRefactoringProvider)), Shared]
    internal class ReplacePropertyWithMethodsCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var service = document.GetLanguageService<IReplacePropertyWithMethodsService>();
            if (service == null)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var position = context.Span.Start;
            var token = root.FindToken(position);

            if (!token.Span.Contains(context.Span))
            {
                return;
            }

            var propertyDeclaration = service.GetPropertyDeclaration(token);
            if (propertyDeclaration == null)
            {
                return;
            }

            // var propertyName = service.GetPropertyName(propertyDeclaration);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration) as IPropertySymbol;
            var propertyName = propertySymbol.Name;

            var accessorCount =
                (propertySymbol.GetMethod == null ? 0 : 1) +
                (propertySymbol.SetMethod == null ? 0 : 1);

            var resourceString = accessorCount == 1
                ? FeaturesResources.Replace_0_with_method
                : FeaturesResources.Replace_0_with_methods;

            // Looks good!
            context.RegisterRefactoring(new ReplacePropertyWithMethodsCodeAction(
                string.Format(resourceString, propertyName),
                c => ReplacePropertyWithMethods(context.Document, /*propertyName, */ propertySymbol, c),
                propertyName));
        }

        private async Task<Solution> ReplacePropertyWithMethods(
           Document document,
           IPropertySymbol propertySymbol,
           CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var originalSolution = document.Project.Solution;
            var propertyReferences = await SymbolFinder.FindReferencesAsync(propertySymbol, originalSolution, cancellationToken).ConfigureAwait(false);

            // Get the warnings we'd like to put at the definition site.
            var definitionWarning = GetDefinitionIssues(propertyReferences);

            var referencesByDocument = propertyReferences.SelectMany(r => r.Locations)
                                                        .ToLookup(loc => loc.Document);

            // References and definitions can overlap (for example, references to one property
            // inside the definition of another).  So we do a multi phase rewrite.  We first
            // rewrite all references to point at the new methods instead.  Then we remove all
            // the actual property definitions and replace them with the new methods.
            var updatedSolution = originalSolution;

            updatedSolution = await UpdateReferencesAsync(updatedSolution, referencesByDocument, cancellationToken).ConfigureAwait(false);
            updatedSolution = await ReplaceDefinitionsWithMethodsAsync(originalSolution, updatedSolution, propertyReferences, cancellationToken).ConfigureAwait(false);

            return updatedSolution;
        }

        private string GetDefinitionIssues(IEnumerable<ReferencedSymbol> getMethodReferences)
        {
            // TODO: add things to be concerned about here.  For example:
            // 1. If any of the referenced symbols are from metadata.
            // 2. If a symbol is referenced implicitly.
            // 3. if the property has attributes.
            return null;
        }

        private async Task<Solution> UpdateReferencesAsync(
            Solution updatedSolution, ILookup<Document, ReferenceLocation> referencesByDocument, CancellationToken cancellationToken)
        {
            foreach (var group in referencesByDocument)
            {
                cancellationToken.ThrowIfCancellationRequested();

                updatedSolution = await UpdateReferencesInDocumentAsync(
                    updatedSolution, group.Key, group,
                    cancellationToken).ConfigureAwait(false);
            }

            return updatedSolution;
        }
        private async Task<Solution> UpdateReferencesInDocumentAsync(
           Solution updatedSolution,
           Document originalDocument,
           IEnumerable<ReferenceLocation> references,
           CancellationToken cancellationToken)
        {
            var root = await originalDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, originalDocument.Project.Solution.Workspace);
            var service = originalDocument.GetLanguageService<IReplacePropertyWithMethodsService>();

            ReplaceReferences(references, root, editor, service, cancellationToken);

            updatedSolution = updatedSolution.WithDocumentSyntaxRoot(originalDocument.Id, editor.GetChangedRoot());

            return updatedSolution;
        }

        private static void ReplaceReferences(
            IEnumerable<ReferenceLocation> references,
            SyntaxNode root, SyntaxEditor editor,
            IReplacePropertyWithMethodsService service,
            CancellationToken cancellationToken)
        {
            if (references != null)
            {
                foreach (var referenceLocation in references)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var location = referenceLocation.Location;
                    var nameToken = root.FindToken(location.SourceSpan.Start);

                    if (referenceLocation.IsImplicit)
                    {
                        // Warn the user that we can't properly replace this property with a method.
                        editor.ReplaceNode(nameToken.Parent, nameToken.Parent.WithAdditionalAnnotations(
                            ConflictAnnotation.Create(FeaturesResources.Property_referenced_implicitly)));
                    }
                    else
                    {
                        service.ReplaceReference(editor, nameToken);
                    }
                }
            }
        }
        private async Task<Solution> ReplaceDefinitionsWithMethodsAsync(
           Solution originalSolution,
           Solution updatedSolution,
           IEnumerable<ReferencedSymbol> references,
           CancellationToken cancellationToken)
        {
            var definitionsByDocumentId = await GetDefinitionsByDocumentIdAsync(originalSolution, references, cancellationToken).ConfigureAwait(false);

            foreach (var kvp in definitionsByDocumentId)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var documentId = kvp.Key;
                var definitions = kvp.Value;

                updatedSolution = await ReplaceDefinitionsWithMethodsAsync(
                    updatedSolution, documentId, definitions, cancellationToken).ConfigureAwait(false);
            }

            return updatedSolution;
        }

        private async Task<MultiDictionary<DocumentId, IPropertySymbol>> GetDefinitionsByDocumentIdAsync(
           Solution originalSolution,
           IEnumerable<ReferencedSymbol> referencedSymbols,
           CancellationToken cancellationToken)
        {
            var result = new MultiDictionary<DocumentId, IPropertySymbol>();
            foreach (var referencedSymbol in referencedSymbols)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var definition = referencedSymbol.Definition as IPropertySymbol;
                if (definition?.DeclaringSyntaxReferences.Length > 0)
                {
                    var syntax = await definition.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                    if (syntax != null)
                    {
                        var document = originalSolution.GetDocument(syntax.SyntaxTree);
                        if (document != null)
                        {
                            result.Add(document.Id, definition);
                        }
                    }
                }
            }

            return result;
        }

        private async Task<Solution> ReplaceDefinitionsWithMethodsAsync(
            Solution updatedSolution,
            DocumentId documentId,
            MultiDictionary<DocumentId, IPropertySymbol>.ValueSet originalDefinitions,
            CancellationToken cancellationToken)
        {
            var updatedDocument = updatedSolution.GetDocument(documentId);
            var compilation = await updatedDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // We've already gone and updated all references.  So now re-resolve all the definitions
            // in the current compilation to find their updated location.
            var currentDefinitions = await GetCurrentPropertiesAsync(
                updatedSolution, compilation, documentId, originalDefinitions, cancellationToken).ConfigureAwait(false);

            var service = updatedDocument.GetLanguageService<IReplacePropertyWithMethodsService>();

            var semanticModel = await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, updatedSolution.Workspace);

            // First replace all the get methods with properties.
            foreach (var definition in currentDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                service.ReplacePropertyWithMethod(editor, semanticModel, definition.Item1, definition.Item2);
            }

            return updatedSolution.WithDocumentSyntaxRoot(documentId, editor.GetChangedRoot());
        }

        private async Task<List<ValueTuple<IPropertySymbol, SyntaxNode>>> GetCurrentPropertiesAsync(
            Solution updatedSolution,
            Compilation compilation,
            DocumentId documentId,
            MultiDictionary<DocumentId, IPropertySymbol>.ValueSet originalDefinitions,
            CancellationToken cancellationToken)
        {
            var result = new List<ValueTuple<IPropertySymbol, SyntaxNode>>();
            foreach (var originalDefinition in originalDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var property = GetSymbolInCurrentCompilation(compilation, originalDefinition, cancellationToken);
                var declaration = await GetPropertyDeclarationAsync(property, cancellationToken).ConfigureAwait(false);

                if (declaration != null && updatedSolution.GetDocument(declaration.SyntaxTree)?.Id == documentId)
                {
                    result.Add(ValueTuple.Create(property, declaration));
                }
            }

            return result;
        }

        private async Task<SyntaxNode> GetPropertyDeclarationAsync(
            IPropertySymbol property, CancellationToken cancellationToken)
        {
            if (property == null)
            {
                return null;
            }

            Debug.Assert(property.DeclaringSyntaxReferences.Length == 1);
            var reference = property.DeclaringSyntaxReferences[0];
            return await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
        }

        private static TSymbol GetSymbolInCurrentCompilation<TSymbol>(Compilation compilation, TSymbol originalDefinition, CancellationToken cancellationToken)
            where TSymbol : class, ISymbol
        {
            return originalDefinition.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken).GetAnySymbol() as TSymbol;
        }


        private class ReplacePropertyWithMethodsCodeAction : CodeAction.SolutionChangeAction
        {
            public ReplacePropertyWithMethodsCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey)
                : base(title, createChangedSolution, equivalenceKey)
            {
            }
        }
    }
}
