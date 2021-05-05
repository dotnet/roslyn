// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class IncrementalGeneratorSyntaxWalker : SyntaxWalker
    {
        private readonly ArrayBuilder<ISyntaxTransformBuilder> _builders;

        private readonly ArrayBuilder<ArrayBuilder<SyntaxNode>?> _results;

        internal IncrementalGeneratorSyntaxWalker(ArrayBuilder<ISyntaxTransformBuilder> builders, ArrayBuilder<ArrayBuilder<SyntaxNode>?> results)
        {
            _builders = builders;
            _results = results;
        }

        public static void VisitNodeForBuilders(SyntaxNode root, SemanticModel semanticModel, ArrayBuilder<ISyntaxTransformBuilder> builders)
        {
            var results = ArrayBuilder<ArrayBuilder<SyntaxNode>?>.GetInstance(builders.Count, fillWithValue: null);

            IncrementalGeneratorSyntaxWalker walker = new IncrementalGeneratorSyntaxWalker(builders, results);
            walker.Visit(root);

            for (int i = 0; i < builders.Count; i++)
            {
                var result = results[i];
                if (result is object)
                {
                    builders[i].AddFilterEntries(result.ToImmutableAndFree(), semanticModel);
                }
            }
            results.Free();
        }

        public override void Visit(SyntaxNode node)
        {
            for (int i = 0; i < _builders.Count; i++)
            {
                if (_builders[i].Filter(node))
                {
                    var result = _results[i];
                    if (result is null)
                    {
                        _results[i] = result = ArrayBuilder<SyntaxNode>.GetInstance();
                    }
                    result.Add(node);
                }
            }
            base.Visit(node);
        }
    }
}
