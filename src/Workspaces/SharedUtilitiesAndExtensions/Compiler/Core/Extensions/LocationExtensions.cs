// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class LocationExtensions
{
    extension(Location location)
    {
        public SyntaxTree GetSourceTreeOrThrow()
        {
            Contract.ThrowIfNull(location.SourceTree);
            return location.SourceTree;
        }

        public SyntaxToken FindToken(CancellationToken cancellationToken)
            => location.GetSourceTreeOrThrow().GetRoot(cancellationToken).FindToken(location.SourceSpan.Start);

        public SyntaxNode FindNode(CancellationToken cancellationToken)
            => location.GetSourceTreeOrThrow().GetRoot(cancellationToken).FindNode(location.SourceSpan);

        public SyntaxNode FindNode(bool getInnermostNodeForTie, CancellationToken cancellationToken)
            => location.GetSourceTreeOrThrow().GetRoot(cancellationToken).FindNode(location.SourceSpan, getInnermostNodeForTie: getInnermostNodeForTie);

        public SyntaxNode FindNode(bool findInsideTrivia, bool getInnermostNodeForTie, CancellationToken cancellationToken)
            => location.GetSourceTreeOrThrow().GetRoot(cancellationToken).FindNode(location.SourceSpan, findInsideTrivia, getInnermostNodeForTie);
    }

    extension(Location loc)
    {
        public bool IsVisibleSourceLocation()
        {
            if (!loc.IsInSource)
            {
                return false;
            }

            var tree = loc.SourceTree;
            return !(tree == null || tree.IsHiddenPosition(loc.SourceSpan.Start));
        }
    }

    extension(Location loc1)
    {
        public bool IntersectsWith(Location loc2)
        {
            Debug.Assert(loc1.IsInSource && loc2.IsInSource);
            return loc1.SourceTree == loc2.SourceTree && loc1.SourceSpan.IntersectsWith(loc2.SourceSpan);
        }
    }
}
