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
        private readonly ImmutableArray<ISyntaxTransformBuilder> _builders;

        private readonly ImmutableArray<ArrayBuilder<SyntaxNode>> _resultArray;

        internal IncrementalGeneratorSyntaxWalker(ImmutableArray<ISyntaxTransformBuilder> builders, ImmutableArray<ArrayBuilder<SyntaxNode>> resultArray)
        {
            _builders = builders;
            _resultArray = resultArray;
        }

        public static ImmutableArray<ImmutableArray<SyntaxNode>> Run(SyntaxNode root, ImmutableArray<ISyntaxTransformBuilder> builders)
        {
            var results = builders.SelectAsArray(s => ArrayBuilder<SyntaxNode>.GetInstance());
            IncrementalGeneratorSyntaxWalker walker = new IncrementalGeneratorSyntaxWalker(builders, results);
            walker.Visit(root);
            return results.SelectAsArray(r => r.ToImmutableAndFree());
        }

        public override void Visit(SyntaxNode node)
        {
            for (int i = 0; i < _builders.Length; i++)
            {
                if (_builders[i].Filter(node))
                {
                    _resultArray[i].Add(node);
                }
            }
            base.Visit(node);
        }
    }
}
