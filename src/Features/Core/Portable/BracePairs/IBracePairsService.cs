// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.BracePairs
{
    internal readonly record struct BracePairs(
        TextSpan Start,
        TextSpan End);

    internal interface IBracePairsService : ILanguageService
    {
        Task AddBracePairsAsync(Document document, ArrayBuilder<BracePairs> bracePairs, CancellationToken cancellationToken);
    }

    internal abstract class AbstractBracePairsService : IBracePairsService
    {
        private readonly Dictionary<int, int> _bracePairKinds = new();

        protected AbstractBracePairsService(params (int startBraceKind, int endBraceKind)[] bracePairs)
        {
            foreach (var pair in bracePairs)
                _bracePairKinds[pair.startBraceKind] = pair.endBraceKind;
        }

        public async Task AddBracePairsAsync(Document document, ArrayBuilder<BracePairs> bracePairs, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var stack);

            stack.Add(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current.IsNode)
                {
                    foreach (var child in current.ChildNodesAndTokens().Reverse())
                        stack.Push(child);
                }
                else if (current.IsToken)
                {
                    if (_bracePairKinds.TryGetValue(current.AsToken().RawKind, out var closeKind))
                    {
                        // hit an open token.  Try to find the corresponding close token in the parent.
                        if (current.Parent != null)
                        {
                            foreach (var sibling in current.Parent.ChildNodesAndTokens())
                            {
                                if (sibling.IsToken && sibling.RawKind == closeKind)
                                    bracePairs.Add(new BracePairs(current.Span, sibling.Span));
                            }
                        }
                    }
                }
            }
        }
    }
}
