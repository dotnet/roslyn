// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class LocationExtensions
{
    public static SyntaxTree GetSourceTreeOrThrow(this Location location)
    {
        Contract.ThrowIfNull(location.SourceTree);
        return location.SourceTree;
    }

    public static SyntaxToken FindToken(this Location location, CancellationToken cancellationToken)
        => location.GetSourceTreeOrThrow().GetRoot(cancellationToken).FindToken(location.SourceSpan.Start);

    public static SyntaxNode FindNode(this Location location, CancellationToken cancellationToken)
        => location.GetSourceTreeOrThrow().GetRoot(cancellationToken).FindNode(location.SourceSpan);

    public static SyntaxNode FindNode(this Location location, bool getInnermostNodeForTie, CancellationToken cancellationToken)
        => location.GetSourceTreeOrThrow().GetRoot(cancellationToken).FindNode(location.SourceSpan, getInnermostNodeForTie: getInnermostNodeForTie);

    public static SyntaxNode FindNode(this Location location, bool findInsideTrivia, bool getInnermostNodeForTie, CancellationToken cancellationToken)
        => location.GetSourceTreeOrThrow().GetRoot(cancellationToken).FindNode(location.SourceSpan, findInsideTrivia, getInnermostNodeForTie);

    public static bool IsVisibleSourceLocation(this Location loc)
    {
        if (!loc.IsInSource)
        {
            return false;
        }

        var tree = loc.SourceTree;
        return !(tree == null || tree.IsHiddenPosition(loc.SourceSpan.Start));
    }

    public static bool IntersectsWith(this Location loc1, Location loc2)
    {
        Debug.Assert(loc1.IsInSource && loc2.IsInSource);
        return loc1.SourceTree == loc2.SourceTree && loc1.SourceSpan.IntersectsWith(loc2.SourceSpan);
    }
}
