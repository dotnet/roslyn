// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.ReplaceMethodWithProperty), Shared]
    internal class ReplaceMethodWithPropertyCodeRefactoringProvider :
        CodeRefactoringProvider,
        IEqualityComparer<ReferenceLocation>
    {
        private const string GetPrefix = "Get";

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
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

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol methodSymbol ||
                !IsValidGetMethod(methodSymbol))
            {
                return;
            }

            var hasGetPrefix = HasGetPrefix(methodName);
            var propertyName = hasGetPrefix
                ? NameGenerator.GenerateUniqueName(
                    methodName[GetPrefix.Length..],
                    n => !methodSymbol.ContainingType.GetMembers(n).Any())
                : methodName;
            var nameChanged = hasGetPrefix;

            // Looks good!
            context.RegisterRefactoring(CodeAction.Create(
                string.Format(FeaturesResources.Replace_0_with_property, methodName),
                c => ReplaceMethodsWithPropertyAsync(document, propertyName, nameChanged, methodSymbol, setMethod: null, cancellationToken: c),
                methodName),
                methodDeclaration.Span);

            // If this method starts with 'Get' see if there's an associated 'Set' method we could 
            // replace as well.
            if (hasGetPrefix)
            {
                var setMethod = FindSetMethod(methodSymbol);
                if (setMethod != null)
                {
                    context.RegisterRefactoring(CodeAction.Create(
                        string.Format(FeaturesResources.Replace_0_and_1_with_property, methodName, setMethod.Name),
                        c => ReplaceMethodsWithPropertyAsync(document, propertyName, nameChanged, methodSymbol, setMethod, cancellationToken: c),
                        methodName + "-get/set"),
                        methodDeclaration.Span);
                }
            }
        }

        private static bool HasGetPrefix(string text)
            => HasPrefix(text, GetPrefix);
        private static bool HasPrefix(string text, string prefix)
            => text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && text.Length > prefix.Length && !char.IsLower(text[prefix.Length]);

        private static IMethodSymbol? FindSetMethod(IMethodSymbol getMethod)
        {
            var containingType = getMethod.ContainingType;
            var setMethodName = "Set" + getMethod.Name[GetPrefix.Length..];
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
                SymbolEqualityComparer.IncludeNullability.Equals(setMethod.Parameters[0].Type, getMethod.ReturnType) &&
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

        private async Task<Solution> ReplaceMethodsWithPropertyAsync(
            Document document,
            string propertyName,
            bool nameChanged,
            IMethodSymbol getMethod,
            IMethodSymbol? setMethod,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var project = document.Project;
            var originalSolution = project.Solution;
            var getMethodReferences = await SymbolFinder.FindReferencesAsync(
                getMethod, originalSolution, cancellationToken).ConfigureAwait(false);
            var setMethodReferences = setMethod == null
                ? SpecializedCollections.EmptyEnumerable<ReferencedSymbol>()
                : await SymbolFinder.FindReferencesAsync(
                    setMethod, originalSolution, cancellationToken).ConfigureAwait(false);

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
            var root = await originalDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, originalDocument.Project.Solution.Workspace.Services);
            var service = originalDocument.GetRequiredLanguageService<IReplaceMethodWithPropertyService>();

            ReplaceGetReferences(propertyName, nameChanged, getReferences, root, editor, service, cancellationToken);
            ReplaceSetReferences(propertyName, nameChanged, setReferences, root, editor, service, cancellationToken);

            updatedSolution = updatedSolution.WithDocumentSyntaxRoot(originalDocument.Id, editor.GetChangedRoot());

            return updatedSolution;
        }

        private void ReplaceGetReferences(
            string propertyName, bool nameChanged,
            IEnumerable<ReferenceLocation> getReferences,
            SyntaxNode root, SyntaxEditor editor,
            IReplaceMethodWithPropertyService service,
            CancellationToken cancellationToken)
        {
            if (getReferences != null)
            {
                // We may hit a location multiple times due to how we do FAR for linked symbols, but each linked symbol
                // is allowed to report the entire set of references it think it is compatible with.  So ensure we're 
                // hitting each location only once.
                // 
                // Note Use DistinctBy (.Net6) once available.
                foreach (var referenceLocation in getReferences.Distinct(this))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var location = referenceLocation.Location;
                    var nameToken = root.FindToken(location.SourceSpan.Start);
                    if (nameToken.Parent == null)
                    {
                        Debug.Fail($"Parent node of {nameToken} is null.");
                        continue;
                    }

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

        private void ReplaceSetReferences(
            string propertyName, bool nameChanged,
            IEnumerable<ReferenceLocation> setReferences,
            SyntaxNode root, SyntaxEditor editor,
            IReplaceMethodWithPropertyService service,
            CancellationToken cancellationToken)
        {
            if (setReferences != null)
            {
                // We may hit a location multiple times due to how we do FAR for linked symbols, but each linked symbol
                // is allowed to report the entire set of references it think it is compatible with.  So ensure we're 
                // hitting each location only once.
                // 
                // Note Use DistinctBy (.Net6) once available.
                foreach (var referenceLocation in setReferences.Distinct(this))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var location = referenceLocation.Location;
                    var nameToken = root.FindToken(location.SourceSpan.Start);
                    if (nameToken.Parent == null)
                    {
                        Debug.Fail($"Parent node of {nameToken} is null.");
                        continue;
                    }

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

        private static async Task<Solution> ReplaceGetMethodsAndRemoveSetMethodsAsync(
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

        private static async Task<Solution> ReplaceGetMethodsAndRemoveSetMethodsAsync(
            string propertyName,
            bool nameChanged,
            Solution updatedSolution,
            DocumentId documentId,
            MultiDictionary<DocumentId, IMethodSymbol>.ValueSet originalGetDefinitions,
            MultiDictionary<DocumentId, IMethodSymbol>.ValueSet originalSetDefinitions,
            bool updateSetMethod,
            CancellationToken cancellationToken)
        {
            var updatedDocument = updatedSolution.GetRequiredDocument(documentId);
            var compilation = await updatedDocument.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            // We've already gone and updated all references.  So now re-resolve all the definitions
            // in the current compilation to find their updated location.
            var getSetPairs = await GetGetSetPairsAsync(
                updatedSolution, compilation, documentId, originalGetDefinitions, updateSetMethod, cancellationToken).ConfigureAwait(false);

            var service = updatedDocument.GetRequiredLanguageService<IReplaceMethodWithPropertyService>();

            var semanticModel = await updatedDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxTree = await updatedDocument.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await updatedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, updatedSolution.Workspace.Services);

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

        private static async Task<ImmutableArray<GetAndSetMethods>> GetGetSetPairsAsync(
            Solution updatedSolution,
            Compilation compilation,
            DocumentId documentId,
            MultiDictionary<DocumentId, IMethodSymbol>.ValueSet originalDefinitions,
            bool updateSetMethod,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<GetAndSetMethods>.GetInstance(out var result);
            foreach (var originalDefinition in originalDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var getMethod = GetSymbolInCurrentCompilation(compilation, originalDefinition, cancellationToken);
                if (getMethod != null && IsValidGetMethod(getMethod))
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

            return result.ToImmutable();
        }

        private static TSymbol? GetSymbolInCurrentCompilation<TSymbol>(Compilation compilation, TSymbol originalDefinition, CancellationToken cancellationToken)
            where TSymbol : class, ISymbol
        {
            return originalDefinition.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).GetAnySymbol() as TSymbol;
        }

        private static async Task<SyntaxNode?> GetMethodDeclarationAsync(IMethodSymbol? method, CancellationToken cancellationToken)
        {
            if (method == null)
            {
                return null;
            }

            Debug.Assert(method.DeclaringSyntaxReferences.Length == 1);
            var reference = method.DeclaringSyntaxReferences[0];
            return await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<MultiDictionary<DocumentId, IMethodSymbol>> GetDefinitionsByDocumentIdAsync(
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

#pragma warning disable IDE0060 // Remove unused parameter - Method not completely implemented.
        private static string? GetDefinitionIssues(IEnumerable<ReferencedSymbol> getMethodReferences)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            // TODO: add things to be concerned about here.  For example:
            // 1. If any of the referenced symbols are from metadata.
            // 2. If a symbol is referenced implicitly.
            // 3. if the methods have attributes on them.
            return null;
        }

        public bool Equals([AllowNull] ReferenceLocation x, [AllowNull] ReferenceLocation y)
        {
            Contract.ThrowIfFalse(x.Document == y.Document);
            return x.Location.SourceSpan == y.Location.SourceSpan;
        }

        public int GetHashCode([DisallowNull] ReferenceLocation obj)
            => obj.Location.SourceSpan.GetHashCode();
    }
}
