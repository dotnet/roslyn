// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class CachingSemanticModelProvider : SemanticModelProvider
    {
        private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModelsMap;

        public CachingSemanticModelProvider()
        {
            _semanticModelsMap = new ConcurrentDictionary<SyntaxTree, SemanticModel>();
        }

        public override SemanticModel GetSemanticModel(SyntaxTree tree, Compilation compilation)
        {
            if (_semanticModelsMap.TryGetValue(tree, out var model) &&
                model.Compilation == compilation)
            {
                return model;
            }

            // Avoid infinite recursion by passing 'useSemanticModelProviderIfNonNull: false'
            model = compilation.GetSemanticModelCore(tree, ignoreAccessibility: false, useSemanticModelProviderIfNonNull: false);
            return _semanticModelsMap.AddOrUpdate(tree,
                addValue: model,
                updateValueFactory: (_, currentModel) => currentModel.Compilation == compilation ? currentModel : model);
        }

        internal void RemoveCachedSemanticModel(SyntaxTree tree)
        {
            _semanticModelsMap.TryRemove(tree, out _);
        }
    }
}
