// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal readonly struct FinderLocation(SyntaxNode node, ReferenceLocation location)
{
    /// <summary>
    /// The actual node that we found the reference on.  Normally the 'Name' portion
    /// of any piece of syntax.  Might also be something like a 'foreach' statement node
    /// when finding results for something like GetEnumerator.
    /// </summary>
    public readonly SyntaxNode Node = node;

    /// <summary>
    /// The location we want want to return through the FindRefs API.  The location contains
    /// additional information (like if this was a Write, or if it was Implicit).  This value
    /// also has a <see cref="ReferenceLocation.Location"/> property.  Importantly, this value
    /// is not necessarily the same location you would get by calling <see cref="Node"/>.<see
    /// cref="SyntaxNode.GetLocation"/>.  Instead, this location is where we want to navigate
    /// the user to.  A case where this can be different is with an indexer reference.  The <see
    /// cref="Node"/> will be the node for the full 'ElementAccessExpression', whereas the 
    /// location we will take the user to will be the zero-length position immediately preceding
    /// the `[` character.
    /// </summary>
    public readonly ReferenceLocation Location = location;

    public void Deconstruct(out SyntaxNode node, out ReferenceLocation location)
    {
        node = Node;
        location = Location;
    }
}
