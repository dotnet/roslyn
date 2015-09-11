using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, 
        Name = nameof(ReplaceMethodWithPropertyCodeRefactoringProvider)), Shared]
    internal class ReplaceMethodWithPropertyCodeRefactoringProvider : CodeRefactoringProvider
    {
        private const string GetPrefix = "Get";

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var service = document.GetLanguageService<IReplaceMethodWithPropertyService>();
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

            var methodDeclaration = service.GetMethodDeclaration(token);
            if (methodDeclaration == null)
            {
                return;
            }

            // Ok, we're in the signature of the method.  Now see if the method is viable to be 
            // replaced with a property.
            var methodName = service.GetMethodName(methodDeclaration);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
            if (methodSymbol == null ||
                methodSymbol.IsGenericMethod ||
                methodSymbol.Parameters.Length > 0 ||
                methodSymbol.ReturnsVoid)
            {
                return;
            }

            var hasGetPrefix = HasGetPrefix(methodName);
            var propertyName = hasGetPrefix
                ? methodName.Substring(GetPrefix.Length)
                : methodName;
            var nameChanged = hasGetPrefix;

            // Looks good!
            context.RegisterRefactoring(new ReplaceMethodWithPropertyCodeAction(
                string.Format(FeaturesResources.Replace0WithProperty, methodName),
                c => ReplaceMethodsWithProperty(context.Document, propertyName, nameChanged, methodSymbol, setMethod: null, cancellationToken: c),
                methodName));


            // If this method starts with 'Get' see if there's an associated 'Set' method we could 
            // replace as well.
            if (hasGetPrefix)
            {
                var setMethod = FindSetMethod(methodSymbol);
                if (setMethod != null)
                {
                    context.RegisterRefactoring(new ReplaceMethodWithPropertyCodeAction(
                        string.Format(FeaturesResources.Replace0and1WithProperty, methodName, setMethod.Name),
                        c => ReplaceMethodsWithProperty(context.Document, propertyName, nameChanged, methodSymbol, setMethod, cancellationToken: c),
                        methodName + "-get/set"));
                }
            }
        }

        private static bool HasGetPrefix(SyntaxToken identifier)
        {
            return HasGetPrefix(identifier.ValueText);
        }

        private static bool HasGetPrefix(string text)
        {
            return text.StartsWith(GetPrefix) && text.Length > GetPrefix.Length;
        }

        private IMethodSymbol FindSetMethod(IMethodSymbol getMethod)
        {
            var containingType = getMethod.ContainingType;
            if (containingType == null)
            {
                return null;
            }

            var setMethod = containingType.GetMembers("Set" + getMethod.Name.Substring(GetPrefix.Length))
                                          .OfType<IMethodSymbol>()
                                          .Where(m => !m.IsGenericMethod)
                                          .Where(m => m.ReturnsVoid)
                                          .Where(m => m.Parameters.Length == 1 && Equals(m.Parameters[0].Type, getMethod.ReturnType))
                                          .Where(m => m.IsAbstract == getMethod.IsAbstract)
                                          .Where(m => m.DeclaringSyntaxReferences.Length == 1)
                                          .FirstOrDefault();

            return setMethod;
        }

        private async Task<Solution> ReplaceMethodsWithProperty(
            Document document,
            string propertyName,
            bool nameChanged,
            IMethodSymbol getMethod,
            IMethodSymbol setMethod,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var originalSolution = document.Project.Solution;
            var getMethodReferences = await SymbolFinder.FindReferencesAsync(getMethod, originalSolution, cancellationToken).ConfigureAwait(false);

            // Get the warnings we'd like to put at the definition site.
            var definitionWarning = GetDefinitionIssues(getMethodReferences);

            var referencesByDocument = getMethodReferences.SelectMany(r => r.Locations).GroupBy(loc => loc.Document);

            // References and definitions can overlap (for example, references to one method
            // inside the definition of another).  So we do a two phase rewrite.  We first
            // rewrite all references to point at the property instead.  Then we remove all
            // the actual method definitions and replace them with the new properties.
            var updatedSolution = originalSolution;
            foreach (var group in referencesByDocument)
            {
                updatedSolution = await UpdateReferencesInDocumentAsync(
                    propertyName, nameChanged, updatedSolution, group, cancellationToken).ConfigureAwait(false);
            }

            var definitionsByDocumentId = await GetDefinitionsByDocumentIdAsync(
                originalSolution, getMethodReferences, cancellationToken).ConfigureAwait(false);

            foreach (var group in definitionsByDocumentId)
            {
                var documentId = group.Key;
                //if (currentDocument.Project.Language == LanguageNames.CSharp)
                {
                    updatedSolution = await UpdateDefinitionsInDocumentAsync(
                        propertyName, updatedSolution, documentId, group.Value, cancellationToken).ConfigureAwait(false);
                }
            }

            return updatedSolution;

            //// Now go and replace all definitions with their new properties.
            //foreach (var referenceLocation in getMethodReferences)
            //{
            //    var definition = referenceLocation.Definition;
            //    if (definition.DeclaringSyntaxReferences.Length > 0)
            //    {
            //        var definitionLocation = await definition.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            //        var originalDefinitionDocument = originalSolution.GetDocument(definitionLocation.SyntaxTree);
            //        if (originalDefinitionDocument != null && originalDefinitionDocument.Project.Language == LanguageNames.CSharp)
            //        {

            //            var definitionInCurrentSolution = definition.GetSymbolKey().Resolve();
            //        }
            //    }

            //}
        }

        private async Task<Solution> UpdateReferencesInDocumentAsync(
            string propertyName,
            bool nameChanged,
            Solution updatedSolution,
            IGrouping<Document, ReferenceLocation> group,
            CancellationToken cancellationToken)
        {
            var originalDocument = group.Key;
            var root = await originalDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, originalDocument.Project.Solution.Workspace);
            var service = originalDocument.GetLanguageService<IReplaceMethodWithPropertyService>();

            foreach (var referenceLocation in group)
            {
                var location = referenceLocation.Location;
                var nameToken = root.FindToken(location.SourceSpan.Start);

                if (referenceLocation.IsImplicit)
                {
                    // Warn the user that we can't properly replace this method with a property.
                    editor.ReplaceNode(nameToken.Parent, nameToken.Parent.WithAdditionalAnnotations(
                        ConflictAnnotation.Create(FeaturesResources.MethodReferencedImplicitly)));
                }
                else
                {
                    service.ReplaceReference(editor, nameToken, propertyName, nameChanged);
                }
            }

            updatedSolution = updatedSolution.WithDocumentSyntaxRoot(originalDocument.Id, editor.GetChangedRoot());

            return updatedSolution;
        }

        private async Task<Solution> UpdateDefinitionsInDocumentAsync(
            string propertyName,
            Solution updatedSolution,
            DocumentId documentId,
            MultiDictionary<DocumentId, ISymbol>.ValueSet originalDefinitions,
            CancellationToken cancellationToken)
        {
            var updatedDocument = updatedSolution.GetDocument(documentId);
            var compilation = await updatedDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // We've already gone and updated all references.  So now re-resolve all the definitions
            // in the current compilation to find their updated location.
            var currentMethodDeclarations = await GetCurrentMethodDeclarationsAsync(
                updatedSolution, compilation, documentId, originalDefinitions, cancellationToken).ConfigureAwait(false);

            return await ReplaceMethodsWithPropertiesAsync(
                propertyName, updatedDocument, currentMethodDeclarations, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Solution> ReplaceMethodsWithPropertiesAsync(
            string propertyName,
            Document document,
            List<SyntaxNode> currentMethodDeclarations,
            CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IReplaceMethodWithPropertyService>();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            foreach (var method in currentMethodDeclarations)
            {
                editor.ReplaceNode(method, service.ConvertMethodToProperty(method, propertyName));
            }

            return document.Project.Solution.WithDocumentSyntaxRoot(
                document.Id, editor.GetChangedRoot());
        }

        private async Task<List<SyntaxNode>> GetCurrentMethodDeclarationsAsync(
            Solution updatedSolution, Compilation compilation, DocumentId documentId, MultiDictionary<DocumentId, ISymbol>.ValueSet originalDefinitions, CancellationToken cancellationToken)
        {
            var result = new List<SyntaxNode>();
            foreach (var originalDefinition in originalDefinitions)
            {
                var resolved = originalDefinition.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken);
                foreach (var currentDefinition in resolved.GetAllSymbols())
                {
                    var methodDeclarations = await GetMethodDeclarationsAsync(currentDefinition, cancellationToken).ConfigureAwait(false);
                    result.AddRange(methodDeclarations.Where(
                        m => updatedSolution.GetDocument(m.SyntaxTree)?.Id == documentId));
                }
            }

            return result;
        }

        private async Task<List<SyntaxNode>> GetMethodDeclarationsAsync(ISymbol currentDefinition, CancellationToken cancellationToken)
        {
            var result = new List<SyntaxNode>();
            foreach (var reference in currentDefinition.DeclaringSyntaxReferences)
            {
                var declaration = await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                if (declaration != null)
                {
                    result.Add(declaration);
                }
            }

            return result;
        }

        private async Task<MultiDictionary<DocumentId, ISymbol>> GetDefinitionsByDocumentIdAsync(Solution originalSolution, IEnumerable<ReferencedSymbol> referencedSymbols, CancellationToken cancellationToken)
        {
            var result = new MultiDictionary<DocumentId, ISymbol>();
            foreach (var referencedSymbol in referencedSymbols)
            {
                var definition = referencedSymbol.Definition;
                if (definition.DeclaringSyntaxReferences.Length > 0)
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

        private string GetDefinitionIssues(IEnumerable<ReferencedSymbol> getMethodReferences)
        {
            // TODO: add things to be concerned about here.  For example:
            // 1. If any of the referenced symbols are from metadata.
            // 2. If a symbol is referenced implicitly.
            return null;
        }

        private class ReplaceMethodWithPropertyCodeAction : CodeAction.SolutionChangeAction
        {
            public ReplaceMethodWithPropertyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey)
                : base(title, createChangedSolution, equivalenceKey)
            {
            }
        }
    }
}