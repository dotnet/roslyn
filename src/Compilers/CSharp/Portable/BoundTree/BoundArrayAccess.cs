// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp;

internal partial class BoundArrayAccess
{
    public BoundArrayAccess(SyntaxNode syntax, BoundExpression expression, ImmutableArray<BoundExpression> indices, TypeSymbol type, bool hasErrors = false)
        : this(syntax, expression, indices, inCompoundAssignmentReceiver: false, type, hasErrors)
    {
    }

    public BoundArrayAccess Update(BoundExpression expression, ImmutableArray<BoundExpression> indices, TypeSymbol type)
    {
        return this.Update(expression, indices, inCompoundAssignmentReceiver: false, type);
    }
}
