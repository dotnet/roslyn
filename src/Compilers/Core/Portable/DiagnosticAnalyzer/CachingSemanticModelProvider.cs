// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Provider that caches semantic models for requested trees, with a strong reference to the model.
    /// Clients using this provider are responsible for maintaining the lifetime of the entries in this cache,
    /// and should invoke <see cref="EnsureCachedSemanticModelRemoved(SyntaxTree, Compilation)"/> to clear entries when appropriate.
    /// For example, <see cref="CompilationWithAnalyzers"/> uses this provider to ensure that semantic model instances
    /// are shared between the compiler and analyzers for improved analyzer execution performance. The underlying
    /// <see cref="AnalyzerDriver"/> executing analyzers clears entries in the cache whenever a <see cref="CompilationUnitCompletedEvent"/>
    /// has been processed, indicating all relevant analyzers have executed on the corresponding syntax tree for the event.
    /// </summary>
    internal sealed class CachingSemanticModelProvider : SemanticModelProvider
    {
        private static readonly Func<(SyntaxTree, Compilation), SemanticModel> s_createSemanticModel =
            ((SyntaxTree tree, Compilation compilation) tuple) => tuple.compilation.CreateSemanticModel(tuple.tree, ignoreAccessibility: false);

        private readonly ConcurrentDictionary<(SyntaxTree, Compilation), SemanticModel> _semanticModelsMap;

        public CachingSemanticModelProvider()
        {
            _semanticModelsMap = new ConcurrentDictionary<(SyntaxTree, Compilation), SemanticModel>();
        }

        public override SemanticModel GetSemanticModel(SyntaxTree tree, Compilation compilation, bool ignoreAccessibility = false)
        {
            // We only care about caching semantic models for internal callers, which use the default 'ignoreAccessibility = false'.
            if (!ignoreAccessibility)
            {
                return _semanticModelsMap.GetOrAdd((tree, compilation), s_createSemanticModel);
            }

            return s_createSemanticModel((tree, compilation));
        }

        internal void EnsureCachedSemanticModelRemoved(SyntaxTree tree, Compilation compilation)
        {
            _semanticModelsMap.TryRemove((tree, compilation), out _);
        }
    }
}
