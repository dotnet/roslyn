// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal abstract partial class AbstractReducer
    {
        internal interface IReductionRewriter : IDisposable
        {
            void Initialize(ParseOptions parseOptions, SimplifierOptions options, CancellationToken cancellationToken);

            SyntaxNodeOrToken VisitNodeOrToken(SyntaxNodeOrToken nodeOrTokenToReduce, SemanticModel semanticModel, bool simplifyAllDescendants);

            bool HasMoreWork { get; }
        }
    }
}
