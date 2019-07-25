// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract partial class AbstractImportsAdder
    {
        protected readonly Document Document;

        protected AbstractImportsAdder(Document document)
        {
            this.Document = document;
        }

        protected abstract IList<INamespaceSymbol> GetExistingNamespaces(SemanticModel semanticModel, SyntaxNode namespaceScope, CancellationToken cancellationToken);

        // protected abstract SyntaxNode GetContextNode(SyntaxNodeOrToken node);
        protected abstract SyntaxNode GetImportsContainer(SyntaxNode node);
        protected abstract SyntaxNode GetInnermostNamespaceScope(SyntaxNodeOrToken node);

        public abstract Task<Document> AddAsync(bool placeSystemNamespaceFirst, CodeGenerationOptions options, CancellationToken cancellationToken);

        protected async Task<IDictionary<SyntaxNode, ISet<INamedTypeSymbol>>> GetAllReferencedDefinitionsAsync(
            Compilation compilation, CancellationToken cancellationToken)
        {
            var namespaceScopeToReferencedDefinitions = new Dictionary<SyntaxNode, ISet<INamedTypeSymbol>>();
            var root = await Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            ISet<INamedTypeSymbol> createSet(SyntaxNode _) => new HashSet<INamedTypeSymbol>();

            var annotatedNodes = root.GetAnnotatedNodesAndTokens(SymbolAnnotation.Kind);
            foreach (var annotatedNode in annotatedNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (annotatedNode.GetAnnotations(DoNotAddImportsAnnotation.Kind).Any())
                {
                    continue;
                }

                SyntaxNode namespaceScope = null;
                var annotations = annotatedNode.GetAnnotations(SymbolAnnotation.Kind);
                foreach (var annotation in annotations)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    foreach (var namedType in SymbolAnnotation.GetSymbols(annotation, compilation).OfType<INamedTypeSymbol>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!IsBuiltIn(namedType))
                        {
                            namespaceScope ??= this.GetInnermostNamespaceScope(annotatedNode);
                            var referencedDefinitions = namespaceScopeToReferencedDefinitions.GetOrAdd(
                                namespaceScope, createSet);
                            referencedDefinitions.Add(namedType);
                        }
                    }
                }
            }

            return namespaceScopeToReferencedDefinitions;
        }

        private bool IsBuiltIn(INamedTypeSymbol type)
        {
            switch (type.OriginalDefinition.SpecialType)
            {
                case Microsoft.CodeAnalysis.SpecialType.System_Object:
                case Microsoft.CodeAnalysis.SpecialType.System_Void:
                case Microsoft.CodeAnalysis.SpecialType.System_Boolean:
                case Microsoft.CodeAnalysis.SpecialType.System_Char:
                case Microsoft.CodeAnalysis.SpecialType.System_SByte:
                case Microsoft.CodeAnalysis.SpecialType.System_Byte:
                case Microsoft.CodeAnalysis.SpecialType.System_Int16:
                case Microsoft.CodeAnalysis.SpecialType.System_UInt16:
                case Microsoft.CodeAnalysis.SpecialType.System_Int32:
                case Microsoft.CodeAnalysis.SpecialType.System_UInt32:
                case Microsoft.CodeAnalysis.SpecialType.System_Int64:
                case Microsoft.CodeAnalysis.SpecialType.System_UInt64:
                case Microsoft.CodeAnalysis.SpecialType.System_Decimal:
                case Microsoft.CodeAnalysis.SpecialType.System_Single:
                case Microsoft.CodeAnalysis.SpecialType.System_Double:
                case Microsoft.CodeAnalysis.SpecialType.System_String:
                case Microsoft.CodeAnalysis.SpecialType.System_Nullable_T:
                    return true;
            }

            return false;
        }

        protected async Task<IDictionary<SyntaxNode, IList<INamespaceSymbol>>> DetermineNamespaceToImportAsync(
            CodeGenerationOptions options, CancellationToken cancellationToken)
        {
            var semanticModel = await this.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;

            // First, find all the named types referenced by code that we are trying to generated.  
            var namespaceScopeToReferencedDefinitions = await GetAllReferencedDefinitionsAsync(
                compilation, cancellationToken).ConfigureAwait(false);

            var importsContainerToMissingImports = new Dictionary<SyntaxNode, IList<INamespaceSymbol>>();

            // Next determine the namespaces we'd have to have imported in order to reference them
            // in a non-qualified manner.
            foreach (var kvp in namespaceScopeToReferencedDefinitions)
            {
                var namespaceScope = kvp.Key;
                var referencedDefinitions = kvp.Value;

                var referencedNamespaces =
                    referencedDefinitions.Select(t => t.ContainingNamespace)
                                         .WhereNotNull()
                                         .Where(n => !n.IsGlobalNamespace)
                                         .Distinct()
                                         .ToList();

                AddMissingNamespaces(
                    semanticModel, importsContainerToMissingImports, namespaceScope, referencedNamespaces, cancellationToken);
            }

            if (options.AdditionalImports.Any())
            {
                var root = await this.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                AddMissingNamespaces(
                    semanticModel, importsContainerToMissingImports, root, options.AdditionalImports, cancellationToken);
            }

            return importsContainerToMissingImports;
        }

        private void AddMissingNamespaces(
            SemanticModel semanticModel,
            Dictionary<SyntaxNode, IList<INamespaceSymbol>> importsContainerToMissingImports,
            SyntaxNode namespaceScope,
            IEnumerable<INamespaceSymbol> referencedNamespaces,
            CancellationToken cancellationToken)
        {
            var existingNamespaces = this.GetExistingNamespaces(semanticModel, namespaceScope, cancellationToken);
            var missingInThisScope = referencedNamespaces.Except(existingNamespaces, INamespaceSymbolExtensions.EqualityComparer);

            var importsContainer = this.GetImportsContainer(namespaceScope);
            var missingImports = importsContainerToMissingImports.GetOrAdd(importsContainer, _ => new List<INamespaceSymbol>());

            var updatedResult = missingImports.Concat(missingInThisScope)
                                              .Distinct(INamespaceSymbolExtensions.EqualityComparer)
                                              .OrderBy(INamespaceSymbolExtensions.CompareNamespaces)
                                              .ToList();

            missingImports.Clear();
            missingImports.AddRange(updatedResult);
        }

        // TODO(cyrusn): Implement this.
        //
        // General algorithm.  See the set of types+arity imported by the current set of imports.
        // Then see the types+arity of the namespace that's being added.  If there are any
        // intersections then we may cause an ambiguity in the code.  To check if there will actually
        // be an ambiguity, Look for SimpleNameNodes in the code that match the name+arity.  If we
        // run into any, then just return that an ambiguity is possible. (Note: if this creates too
        // many false positive, then we may want to do some binding of the node to see if there's
        // actually an ambiguity).
        protected virtual bool CouldCauseAmbiguity(ISet<INamespaceSymbol> currentImportedNamespaces, INamespaceSymbol namespaceToImport)
        {
            return false;
        }

        protected static IEnumerable<INamespaceSymbol> GetContainingNamespacesAndThis(INamespaceSymbol namespaceSymbol)
        {
            while (namespaceSymbol != null && !namespaceSymbol.IsGlobalNamespace)
            {
                yield return namespaceSymbol;
                namespaceSymbol = namespaceSymbol.ContainingNamespace;
            }
        }
    }
}
