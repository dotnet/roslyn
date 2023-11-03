// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundImplicitIndexerAccess
    {
        internal BoundImplicitIndexerAccess WithLengthOrCountAccess(BoundExpression lengthOrCountAccess)
        {
            return this.Update(this.Receiver, this.Argument, lengthOrCountAccess, this.ReceiverPlaceholder,
                this.IndexerOrSliceAccess, this.ArgumentPlaceholders, this.Type);
        }

        internal BoundImplicitIndexerAccess WithArgument(BoundExpression argument)
        {
            Debug.Assert(TypeSymbol.Equals(this.Argument.Type, argument.Type, TypeCompareKind.ConsiderEverything));

            return this.Update(this.Receiver, argument, this.LengthOrCountAccess, this.ReceiverPlaceholder,
                this.IndexerOrSliceAccess, this.ArgumentPlaceholders, this.Type);
        }

        private partial void Validate()
        {
            Debug.Assert(LengthOrCountAccess is BoundPropertyAccess or BoundArrayLength or BoundLocal or BoundBadExpression);
            Debug.Assert(IndexerOrSliceAccess is BoundIndexerAccess or BoundCall or BoundArrayAccess);
        }
    }
}
