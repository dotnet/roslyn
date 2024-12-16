﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundListPattern
    {
        internal BoundListPattern WithSubpatterns(ImmutableArray<BoundPattern> subpatterns)
        {
            return Update(subpatterns, this.HasSlice, this.LengthAccess, this.IndexerAccess, this.ReceiverPlaceholder, this.ArgumentPlaceholder, this.Variable, this.VariableAccess, this.InputType, this.NarrowedType);
        }

        private partial void Validate()
        {
            Debug.Assert(LengthAccess is null or BoundPropertyAccess or BoundBadExpression);
            Debug.Assert(IndexerAccess is null or BoundIndexerAccess or BoundImplicitIndexerAccess or BoundArrayAccess or BoundBadExpression or BoundDynamicIndexerAccess);
            Debug.Assert(Binder.GetIndexerOrImplicitIndexerSymbol(IndexerAccess) is var _);
            Debug.Assert(NarrowedType.Equals(InputType.StrippedType(), TypeCompareKind.AllIgnoreOptions));
        }
    }
}
