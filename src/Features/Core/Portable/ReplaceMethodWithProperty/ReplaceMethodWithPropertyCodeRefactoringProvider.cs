// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = nameof(ReplaceMethodWithPropertyCodeRefactoringProvider)), Shared]
    internal class ReplaceMethodWithPropertyCodeRefactoringProvider : CodeRefactoringProvider
    {
        private const string GetPrefix = "Get";

        [ImportingConstructor]
        public ReplaceMethodWithPropertyCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var service = document.GetLanguageService<IReplaceMethodWithPropertyService>();
            if (service == null)
            {
                return;
            }

            var methodDeclaration = await service.GetMethodDeclarationAsync(context).ConfigureAwait(false);
            if (methodDeclaration == null)
            {
                return;
            }

            // Ok, we're in the signature of the method.  Now see if the method is viable to be 
            // replaced with a property.
            var generator = SyntaxGenerator.GetGenerator(document);
            var methodName = generator.GetName(methodDeclaration);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
            if (!IsValidGetMethod(methodSymbol))
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
                string.Format(FeaturesResources.Replace_0_with_property, methodName),
                c => ReplaceMethodsWithProperty(document, propertyName, nameChanged, methodSymbol, setMethod: null, cancellationToken: c),
                methodName),
                methodDeclaration.Span);

            // If this method starts with 'Get' see if there's an associated 'Set' method we could 
            // replace as well.
            if (hasGetPrefix)
            {
                var setMethod = FindSetMethod(methodSymbol);
                if (setMethod != null)
                {
                    context.RegisterRefactoring(new ReplaceMethodWithPropertyCodeAction(
                        string.Format(FeaturesResources.Replace_0_and_1_with_property, methodName, setMethod.Name),
                        c => ReplaceMethodsWithProperty(document, propertyName, nameChanged, methodSymbol, setMethod, cancellationToken: c),
                        methodName + "-get/set"),
                        methodDeclaration.Span);
                }
            }
        }

        private static bool HasGetPrefix(SyntaxToken identifier)
        {
            return HasGetPrefix(identifier.ValueText);
        }

        private static bool HasGetPrefix(string text)
        {
            return HasPrefix(text, GetPrefix);
        }
        private static bool HasPrefix(string text, string prefix)
        {
            return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && text.Length > prefix.Length && !char.IsLower(text[prefix.Length]);
        }

        private IMethodSymbol FindSetMethod(IMethodSymbol getMethod)
        {
            var containingType = getMethod.ContainingType;
            var setMethodName = "Set" + getMethod.Name.Substring(GetPrefix.Length);
            var setMethod = containingType.GetMembers()
                                          .OfType<IMethodSymbol>()
                                          .Where(m => setMethodName.Equals(m.Name, StringComparison.OrdinalIgnoreCase))
                                          .Where(m => IsValidSetMethod(m, getMethod))
                                          .FirstOrDefault();

            return setMethod;
        }

        private static bool IsValidGetMethod(IMethodSymbol getMethod)
        {
            return getMethod != null &&
                getMethod.ContainingType != null &&
                !getMethod.IsGenericMethod &&
                !getMethod.IsAsync &&
                getMethod.Parameters.Length == 0 &&
                !getMethod.ReturnsVoid &&
                getMethod.DeclaringSyntaxReferences.Length == 1 &&
                !OverridesMethodFromSystemObject(getMethod);
        }

        private static bool OverridesMethodFromSystemObject(IMethodSymbol method)
        {
            for (var current = method; current != null; current = current.OverriddenMethod)
            {
                if (current.ContainingType.SpecialType == SpecialType.System_Object)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidSetMethod(IMethodSymbol setMethod, IMethodSymbol getMethod)
        {
            return IsValidSetMethod(setMethod) &&
                setMethod.Parameters.Length == 1 &&
                setMethod.Parameters[0].RefKind == RefKind.None &&
                Equals(setMethod.Parameters[0].GetTypeWithAnnotatedNullability(), getMethod.GetReturnTypeWithAnnotatedNullability()) &&
                setMethod.IsAbstract == getMethod.IsAbstract;
        }

        private static bool IsValidSetMethod(IMethodSymbol setMethod)
        {
            return setMethod != null &&
                !setMethod.IsGenericMethod &&
                !setMethod.IsAsync &&
                setMethod.ReturnsVoid &&
                setMethod.DeclaringSyntaxReferences.Length == 1;
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
            var setMethodReferences = setMethod == null
                ? SpecializedCollections.EmptyEnumerable<ReferencedSymbol>()
                : await SymbolFinder.FindReferencesAsync(setMethod, originalSolution, cancellationToken).ConfigureAwait(false);

            // Get the warnings we'd like to put at the definition site.
            var definitionWarning = GetDefinitionIssues(getMethodReferences);

            var getReferencesByDocument = getMethodReferences.SelectMany(r => r.Locations).ToLookup(loc => loc.Document);
            var setReferencesByDocument = setMethodReferences.SelectMany(r => r.Locations).ToLookup(loc => loc.Document);

            // References and definitions can overlap (for example, references to one method
            // inside the definition of another).  So we do a multi phase rewrite.  We first
            // rewrite all references to point at the property instead.  Then we remove all
            // the actual method definitions and replace them with the new properties.
            var updatedSolution = originalSolution;

            updatedSolution = await UpdateReferencesAsync(updatedSolution, propertyName, nameChanged, getReferencesByDocument, setReferencesByDocument, cancellationToken).ConfigureAwait(false);
            updatedSolution = await ReplaceGetMethodsAndRemoveSetMethodsAsync(originalSolution, updatedSolution, propertyName, nameChanged, getMethodReferences, setMethodReferences, updateSetMethod: setMethod != null, cancellationToken: cancellationToken).ConfigureAwait(false);

            return updatedSolution;
        }

        private async Task<Solution> UpdateReferencesAsync(Solution updatedSolution, string propertyName, bool nameChanged, ILookup<Document, ReferenceLocation> getReferencesByDocument, ILookup<Document, ReferenceLocation> setReferencesByDocument, CancellationToken cancellationToken)
        {
            var allReferenceDocuments = getReferencesByDocument.Concat(setReferencesByDocument).Select(g => g.Key).Distinct();
            foreach (var referenceDocument in allReferenceDocuments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                updatedSolution = await UpdateReferencesInDocumentAsync(
                    propertyName, nameChanged, updatedSolution, referenceDocument,
                    getReferencesByDocument[referenceDocument],
                    setReferencesByDocument[referenceDocument],
                    cancellationToken).ConfigureAwait(false);
            }

            return updatedSolution;
        }

        private async Task<Solution> UpdateReferencesInDocumentAsync(
            string propertyName,
            bool nameChanged,
            Solution updatedSolution,
            Document originalDocument,
            IEnumerable<ReferenceLocation> getReferences,
            IEnumerable<ReferenceLocation> setReferences,
            CancellationToken cancellationToken)
        {
            var root = await originalDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, originalDocument.Project.Solution.Workspace);
            var service = originalDocument.GetLanguageService<IReplaceMethodWithPropertyService>();

            ReplaceGetReferences(propertyName, nameChanged, getReferences, root, editor, service, cancellationToken);
            ReplaceSetReferences(propertyName, nameChanged, setReferences, root, editor, service, cancellationToken);

            updatedSolution = updatedSolution.WithDocumentSyntaxRoot(originalDocument.Id, editor.GetChangedRoot());

            return updatedSolution;
        }

        private static void ReplaceGetReferences(
            string propertyName, bool nameChanged,
            IEnumerable<ReferenceLocation> getReferences,
            SyntaxNode root, SyntaxEditor editor,
            IReplaceMethodWithPropertyService service,
            CancellationToken cancellationToken)
        {
            if (getReferences != null)
            {
                foreach (var referenceLocation in getReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var location = referenceLocation.Location;
                    var nameToken = root.FindToken(location.SourceSpan.Start);

                    if (referenceLocation.IsImplicit)
                    {
                        // Warn the user that we can't properly replace this method with a property.
                        editor.ReplaceNode(nameToken.Parent, nameToken.Parent.WithAdditionalAnnotations(
                            ConflictAnnotation.Create(FeaturesResources.Method_referenced_implicitly)));
                    }
                    else
                    {
                        service.ReplaceGetReference(editor, nameToken, propertyName, nameChanged);
                    }
                }
            }
        }

        private static void ReplaceSetReferences(
            string propertyName, bool nameChanged,
            IEnumerable<ReferenceLocation> setReferences,
            SyntaxNode root, SyntaxEditor editor,
            IReplaceMethodWithPropertyService service,
            CancellationToken cancellationToken)
        {
            if (setReferences != null)
            {
                foreach (var referenceLocation in setReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var location = referenceLocation.Location;
                    var nameToken = root.FindToken(location.SourceSpan.Start);

                    if (referenceLocation.IsImplicit)
                    {
                        // Warn the user that we can't properly replace this method with a property.
                        editor.ReplaceNode(nameToken.Parent, nameToken.Parent.WithAdditionalAnnotations(
                            ConflictAnnotation.Create(FeaturesResources.Method_referenced_implicitly)));
                    }
                    else
                    {
                        service.ReplaceSetReference(editor, nameToken, propertyName, nameChanged);
                    }
                }
            }
        }

        private async Task<Solution> ReplaceGetMethodsAndRemoveSetMethodsAsync(
            Solution originalSolution,
            Solution updatedSolution,
            string propertyName,
            bool nameChanged,
            IEnumerable<ReferencedSymbol> getMethodReferences,
            IEnumerable<ReferencedSymbol> setMethodReferences,
            bool updateSetMethod,
            CancellationToken cancellationToken)
        {
            var getDefinitionsByDocumentId = await GetDefinitionsByDocumentIdAsync(originalSolution, getMethodReferences, cancellationToken).ConfigureAwait(false);
            var setDefinitionsByDocumentId = await GetDefinitionsByDocumentIdAsync(originalSolution, setMethodReferences, cancellationToken).ConfigureAwait(false);

            var documentIds = getDefinitionsByDocumentId.Keys.Concat(setDefinitionsByDocumentId.Keys).Distinct();
            foreach (var documentId in documentIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var getDefinitions = getDefinitionsByDocumentId[documentId];
                var setDefinitions = setDefinitionsByDocumentId[documentId];

                updatedSolution = await ReplaceGetMethodsAndRemoveSetMethodsAsync(
                    propertyName, nameChanged, updatedSolution, documentId, getDefinitions, setDefinitions, updateSetMethod, cancellationToken).ConfigureAwait(false);
            }

            return updatedSolution;
        }

        private async Task<Solution> ReplaceGetMethodsAndRemoveSetMethodsAsync(
            string propertyName,
            bool nameChanged,
            Solution updatedSolution,
            DocumentId documentId,
            MultiDictionary<DocumentId, IMethodSymbol>.ValueSet originalGetDefinitions,
            MultiDictionary<DocumentId, IMethodSymbol>.ValueSet originalSetDefinitions,
            bool updateSetMethod,
            CancellationToken cancellationToken)
        {
            var updatedDocument = updatedSolution.GetDocument(documentId);
            var compilation = await updatedDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // We've already gone and updated all references.  So now re-resolve all the definitions
            // in the current compilation to find their updated location.
            var getSetPairs = await GetGetSetPairsAsync(
                updatedSolution, compilation, documentId, originalGetDefinitions, updateSetMethod, cancellationToken).ConfigureAwait(false);

            var service = updatedDocument.GetLanguageService<IReplaceMethodWithPropertyService>();

            var semanticModel = await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxTree = await updatedDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, updatedSolution.Workspace);

            var documentOptions = await updatedDocument.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var parseOptions = syntaxTree.Options;

            // First replace all the get methods with properties.
            foreach (var getSetPair in getSetPairs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                service.ReplaceGetMethodWithProperty(
                    documentOptions, parseOptions, editor, semanticModel,
                    getSetPair, propertyName, nameChanged);
            }

            // Then remove all the set methods.
            foreach (var originalSetMethod in originalSetDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var setMethod = GetSymbolInCurrentCompilation(compilation, originalSetMethod, cancellationToken);
                var setMethodDeclaration = await GetMethodDeclarationAsync(setMethod, cancellationToken).ConfigureAwait(false);

                var setMethodDocument = updatedSolution.GetDocument(setMethodDeclaration?.SyntaxTree);
                if (setMethodDocument?.Id == documentId)
                {
                    service.RemoveSetMethod(editor, setMethodDeclaration);
                }
            }

            return updatedSolution.WithDocumentSyntaxRoot(documentId, editor.GetChangedRoot());
        }

        private async Task<List<GetAndSetMethods>> GetGetSetPairsAsync(
            Solution updatedSolution,
            Compilation compilation,
            DocumentId documentId,
            MultiDictionary<DocumentId, IMethodSymbol>.ValueSet originalDefinitions,
            bool updateSetMethod,
            CancellationToken cancellationToken)
        {
            var result = new List<GetAndSetMethods>();
            foreach (var originalDefinition in originalDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var getMethod = GetSymbolInCurrentCompilation(compilation, originalDefinition, cancellationToken);
                if (IsValidGetMethod(getMethod))
                {
                    var setMethod = updateSetMethod ? FindSetMethod(getMethod) : null;
                    var getMethodDeclaration = await GetMethodDeclarationAsync(getMethod, cancellationToken).ConfigureAwait(false);
                    var setMethodDeclaration = await GetMethodDeclarationAsync(setMethod, cancellationToken).ConfigureAwait(false);

                    if (getMethodDeclaration != null && updatedSolution.GetDocument(getMethodDeclaration.SyntaxTree)?.Id == documentId)
                    {
                        result.Add(new GetAndSetMethods(getMethod, setMethod, getMethodDeclaration, setMethodDeclaration));
                    }
                }
            }

            return result;
        }

        private static TSymbol GetSymbolInCurrentCompilation<TSymbol>(Compilation compilation, TSymbol originalDefinition, CancellationToken cancellationToken)
            where TSymbol : class, ISymbol
        {
            return originalDefinition.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken).GetAnySymbol() as TSymbol;
        }

        private async Task<SyntaxNode> GetMethodDeclarationAsync(IMethodSymbol method, CancellationToken cancellationToken)
        {
            if (method == null)
            {
                return null;
            }

            Debug.Assert(method.DeclaringSyntaxReferences.Length == 1);
            var reference = method.DeclaringSyntaxReferences[0];
            return await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<MultiDictionary<DocumentId, IMethodSymbol>> GetDefinitionsByDocumentIdAsync(
            Solution originalSolution,
            IEnumerable<ReferencedSymbol> referencedSymbols,
            CancellationToken cancellationToken)
        {
            var result = new MultiDictionary<DocumentId, IMethodSymbol>();
            foreach (var referencedSymbol in referencedSymbols)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var definition = referencedSymbol.Definition as IMethodSymbol;
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

        private string GetDefinitionIssues(IEnumerable<ReferencedSymbol> getMethodReferences)
        {
            // TODO: add things to be concerned about here.  For example:
            // 1. If any of the referenced symbols are from metadata.
            // 2. If a symbol is referenced implicitly.
            // 3. if the methods have attributes on them.
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
