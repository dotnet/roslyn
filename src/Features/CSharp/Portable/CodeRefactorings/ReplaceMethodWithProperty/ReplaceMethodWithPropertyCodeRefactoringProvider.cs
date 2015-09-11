using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ReplaceMethodWithProperty
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ReplaceMethodWithPropertyCodeRefactoringProvider)), Shared]
    internal class ReplaceMethodWithPropertyCodeRefactoringProvider : CodeRefactoringProvider
    {
        private const string GetPrefix = "Get";

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var position = context.Span.Start;
            var token = root.FindToken(position);

            if (!token.Span.Contains(context.Span))
            {
                return;
            }

            var containingMethod = token.Parent.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod == null)
            {
                return;
            }

            var start = containingMethod.AttributeLists.Count > 0
                ? containingMethod.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
                : containingMethod.SpanStart;

            // Offer this refactoring anywhere in the signature of the method.
            if (position < start || position > containingMethod.ParameterList.Span.End)
            {
                return;
            }

            // Ok, we're in the signature of the method.  Now see if the method is viable to be 
            // replaced with a property.
            if (containingMethod.TypeParameterList != null)
            {
                return;
            }

            if (containingMethod.ParameterList.Parameters.Count > 0)
            {
                return;
            }

            if (containingMethod.ReturnType.Kind() == SyntaxKind.PredefinedType &&
                ((PredefinedTypeSyntax)containingMethod.ReturnType).Keyword.Kind() == SyntaxKind.VoidKeyword)
            {
                return;
            }

            var hasGetPrefix = HasGetPrefix(containingMethod.Identifier);
            var propertyName = hasGetPrefix
                ? containingMethod.Identifier.ValueText.Substring(GetPrefix.Length)
                : containingMethod.Identifier.ValueText;
            var nameChanged = hasGetPrefix;

            // Looks good!
            context.RegisterRefactoring(new ReplaceMethodWithPropertyCodeAction(
                string.Format(CSharpFeaturesResources.Replace0WithProperty, containingMethod.Identifier.ValueText),
                c => ReplaceMethodsWithProperty(context.Document, propertyName, nameChanged, containingMethod, setMethod: null, cancellationToken: c),
                containingMethod.Identifier.ValueText));


            // If this method starts with 'Get' see if there's an associated 'Set' method we could 
            // replace as well.
            if (hasGetPrefix)
            {
                var setMethod = await FindSetMethodAsync(document, containingMethod, cancellationToken).ConfigureAwait(false);
                if (setMethod != null)
                {
                    context.RegisterRefactoring(new ReplaceMethodWithPropertyCodeAction(
                        string.Format(CSharpFeaturesResources.Replace0and1WithProperty, containingMethod.Identifier.ValueText, setMethod.Identifier.ValueText),
                        c => ReplaceMethodsWithProperty(context.Document, propertyName, nameChanged, containingMethod, setMethod, cancellationToken: c),
                        containingMethod.Identifier.ValueText + "get/set"));
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

        private async Task<MethodDeclarationSyntax> FindSetMethodAsync(Document document, MethodDeclarationSyntax getMethod, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var getMethodSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(getMethod, cancellationToken);
            var containingType = getMethodSymbol.ContainingType;
            if (containingType == null)
            {
                return null;
            }

            var setMethod = containingType.GetMembers("Set" + getMethod.Identifier.ValueText.Substring(GetPrefix.Length))
                                          .OfType<IMethodSymbol>()
                                          .Where(m => !m.IsGenericMethod)
                                          .Where(m => m.ReturnsVoid)
                                          .Where(m => m.Parameters.Length == 1 && Equals(m.Parameters[0].Type, getMethodSymbol.ReturnType))
                                          .Where(m => m.IsAbstract == getMethodSymbol.IsAbstract)
                                          .Where(m => m.DeclaringSyntaxReferences.Length == 1)
                                          .FirstOrDefault();

            if (setMethod == null)
            {
                return null;
            }

            var syntax = await setMethod.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            return syntax as MethodDeclarationSyntax;
        }

        private async Task<Solution> ReplaceMethodsWithProperty(
            Document document,
            string propertyName,
            bool nameChanged,
            MethodDeclarationSyntax getMethod,
            MethodDeclarationSyntax setMethod,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var getMethodSymbol = semanticModel.GetDeclaredSymbol(getMethod, cancellationToken);
            var setMethodSymbol = setMethod == null ? null : semanticModel.GetDeclaredSymbol(setMethod, cancellationToken);

            var originalSolution = document.Project.Solution;
            var getMethodReferences = await SymbolFinder.FindReferencesAsync(getMethodSymbol, originalSolution, cancellationToken).ConfigureAwait(false);

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
                var currentDocument = group.Key;

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

            foreach(var referenceLocation in group)
            {
                var location = referenceLocation.Location;
                var nameToken = root.FindToken(location.SourceSpan.Start);

                if (referenceLocation.IsImplicit)
                {
                    // Warn the user that we can't properly replace this method with a property.
                    editor.ReplaceNode(nameToken.Parent, nameToken.Parent.WithAdditionalAnnotations(
                        ConflictAnnotation.Create(CSharpFeaturesResources.MethodReferencedImplicitly)));
                }
                else
                {
                    if (nameToken.Kind() == SyntaxKind.IdentifierToken)
                    {
                        var nameNode = nameToken.Parent as IdentifierNameSyntax;
                        var newName = nameChanged
                            ? SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(propertyName).WithTriviaFrom(nameToken))
                            : nameNode;

                        var invocation = nameNode?.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        var invocationExpression = invocation?.Expression;
                        if (IsInvocationName(nameNode, invocationExpression))
                        {
                            // It was invoked.  Remove the invocation, and also change the name if necessary.
                            editor.ReplaceNode(invocation, invocation.Expression.ReplaceNode(nameNode, newName));
                        }
                        else
                        {
                            // Wasn't invoked.  Change the name, but report a conflict.
                            var annotation = ConflictAnnotation.Create(CSharpFeaturesResources.NonInvokedMethodCannotBeReplacedWithProperty);
                            editor.ReplaceNode(nameNode, newName.WithIdentifier(newName.Identifier.WithAdditionalAnnotations(annotation)));
                        }
                    }
                }
            }

            updatedSolution = updatedSolution.WithDocumentSyntaxRoot(originalDocument.Id, editor.GetChangedRoot());

            return updatedSolution;
        }

        private static bool IsInvocationName(IdentifierNameSyntax nameNode, ExpressionSyntax invocationExpression)
        {
            if (invocationExpression == nameNode)
            {
                return true;
            }

            if (nameNode.IsAnyMemberAccessExpressionName() && nameNode.Parent == invocationExpression)
            {
                return true;
            }

            return false;
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
            var currentMethodDeclarations =  await GetCurrentMethodDeclarationsAsync(
                updatedSolution, compilation, documentId, originalDefinitions, cancellationToken).ConfigureAwait(false);

            return await ReplaceMethodsWithPropertiesAsync(propertyName, updatedDocument, currentMethodDeclarations, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Solution> ReplaceMethodsWithPropertiesAsync(
            string propertyName,
            Document document,
            List<MethodDeclarationSyntax> currentMethodDeclarations,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var generator = new SyntaxEditor(root, document.Project.Solution.Workspace);
            foreach (var method in currentMethodDeclarations)
            {
                generator.ReplaceNode(method, ConvertMethodToProperty(method, propertyName));
            }

            return document.Project.Solution.WithDocumentSyntaxRoot(
                document.Id, generator.GetChangedRoot());
        }

        private PropertyDeclarationSyntax ConvertMethodToProperty(MethodDeclarationSyntax method, string propertyName)
        {
            var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
            accessor = method.Body == null
                ? accessor.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                : accessor.WithBody(method.Body);

            var property = SyntaxFactory.PropertyDeclaration(method.AttributeLists, method.Modifiers, method.ReturnType,
                method.ExplicitInterfaceSpecifier, identifier: SyntaxFactory.Identifier(propertyName),
                accessorList: SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(accessor)));

            if (method.ExpressionBody != null)
            {
                property = property.WithExpressionBody(method.ExpressionBody);
                property = property.WithSemicolonToken(method.SemicolonToken);
            }

            return property;
        }

        private async Task<List<MethodDeclarationSyntax>> GetCurrentMethodDeclarationsAsync(
            Solution updatedSolution, Compilation compilation, DocumentId documentId, MultiDictionary<DocumentId, ISymbol>.ValueSet originalDefinitions, CancellationToken cancellationToken)
        {
            var result = new List<MethodDeclarationSyntax>();
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

        private async Task<List<MethodDeclarationSyntax>> GetMethodDeclarationsAsync(ISymbol currentDefinition, CancellationToken cancellationToken)
        {
            var result = new List<MethodDeclarationSyntax>();
            foreach (var reference in currentDefinition.DeclaringSyntaxReferences)
            {
                var syntax = await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                var methodDeclaration = syntax as MethodDeclarationSyntax;
                if (methodDeclaration != null)
                {
                    result.Add(methodDeclaration);
                }
            }

            return result;
        }

        private async Task<MultiDictionary<DocumentId,ISymbol>> GetDefinitionsByDocumentIdAsync(Solution originalSolution, IEnumerable<ReferencedSymbol> referencedSymbols, CancellationToken cancellationToken)
        {
            var result = new MultiDictionary<DocumentId, ISymbol>();
            foreach (var referencedSymbol in referencedSymbols)
            {
                var definition = referencedSymbol.Definition;
                if (definition.DeclaringSyntaxReferences.Length > 0)
                {
                    var syntax = await definition.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                    if (syntax != null )
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

        private class ReplaceMethodWithPropertyCodeAction: CodeAction.SolutionChangeAction
        {
            public ReplaceMethodWithPropertyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey)
                : base(title, createChangedSolution, equivalenceKey)
            {
            }
        }
    }
}
