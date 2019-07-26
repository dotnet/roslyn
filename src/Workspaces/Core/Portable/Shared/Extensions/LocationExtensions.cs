// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class LocationExtensions
    {
        public static SyntaxToken FindToken(this Location location, CancellationToken cancellationToken)
            => location.SourceTree.GetRoot(cancellationToken).FindToken(location.SourceSpan.Start);

        public static SyntaxNode FindNode(this Location location, CancellationToken cancellationToken)
            => location.SourceTree.GetRoot(cancellationToken).FindNode(location.SourceSpan);

        public static SyntaxNode FindNode(this Location location, bool getInnermostNodeForTie, CancellationToken cancellationToken)
            => location.SourceTree.GetRoot(cancellationToken).FindNode(location.SourceSpan, getInnermostNodeForTie: getInnermostNodeForTie);

        public static SyntaxNode FindNode(this Location location, bool findInsideTrivia, bool getInnermostNodeForTie, CancellationToken cancellationToken)
            => location.SourceTree.GetRoot(cancellationToken).FindNode(location.SourceSpan, findInsideTrivia, getInnermostNodeForTie);

        public static bool IsVisibleSourceLocation(this Location loc)
        {
            if (!loc.IsInSource)
            {
                return false;
            }

            var tree = loc.SourceTree;
            return !(tree == null || tree.IsHiddenPosition(loc.SourceSpan.Start));
        }
    }
}
