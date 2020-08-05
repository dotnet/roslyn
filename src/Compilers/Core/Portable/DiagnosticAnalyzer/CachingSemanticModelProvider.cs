// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Provider that caches semantic models for requested trees, with a strong reference to the model.
    /// Clients using this provider are responsible for maintaining the lifetime of the entries in this cache,
    /// and should invoke <see cref="RemoveCachedSemanticModel(SyntaxTree, Compilation)"/> to clear entries when appropriate.
    /// For example, <see cref="CompilationWithAnalyzers"/> uses this provider to ensure that semantic model instances
    /// are shared between the compiler and analyzers for improved analyzer execution performance. The underlying
    /// <see cref="AnalyzerDriver"/> executing analyzers clears entries in the cache whenever a <see cref="CompilationUnitCompletedEvent"/>
    /// has been processed, indicating all relevant analyzers have executed on the corresponding syntax tree for the event.
    /// </summary>
    internal sealed class CachingSemanticModelProvider : SemanticModelProvider
    {
        private readonly ConcurrentDictionary<(SyntaxTree, Compilation), SemanticModel> _semanticModelsMap;

        public CachingSemanticModelProvider()
        {
            _semanticModelsMap = new ConcurrentDictionary<(SyntaxTree, Compilation), SemanticModel>();
        }

        public override SemanticModel GetSemanticModel(SyntaxTree tree, Compilation compilation)
        {
            if (_semanticModelsMap.TryGetValue((tree, compilation), out var model))
            {
                return model;
            }

            // Avoid infinite recursion by passing 'useSemanticModelProviderIfNonNull: false'
            model = compilation.GetSemanticModelCore(tree, ignoreAccessibility: false, useSemanticModelProviderIfNonNull: false);
            return _semanticModelsMap.GetOrAdd((tree, compilation), model);
        }

        internal void RemoveCachedSemanticModel(SyntaxTree tree, Compilation compilation)
        {
            _semanticModelsMap.TryRemove((tree, compilation), out _);
        }
    }
}
