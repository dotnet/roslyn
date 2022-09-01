// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundSlicePattern
    {
        private partial void Validate()
        {
            Debug.Assert(IndexerAccess is null or BoundIndexerAccess or BoundImplicitIndexerAccess or BoundArrayAccess or BoundBadExpression or BoundDynamicIndexerAccess);
            Debug.Assert(Binder.GetIndexerOrImplicitIndexerSymbol(IndexerAccess) is var _);
        }
    }
}
