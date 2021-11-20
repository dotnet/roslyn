// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundImplicitIndexerAccess
    {
        internal BoundImplicitIndexerAccess WithLengthOrCountAccess(BoundExpression lengthOrCountAccess)
        {
            return this.Update(this.Argument, lengthOrCountAccess, this.ReceiverPlaceholder,
                this.IndexerOrSliceAccess, this.ArgumentPlaceholders, this.Type);
        }

        // The receiver expression is the receiver of IndexerAccess.
        // The LengthOrCountAccess uses a placeholder as receiver.
        internal BoundExpression GetReceiver()
        {
            var receiver = this.IndexerOrSliceAccess switch
            {
                BoundIndexerAccess { ReceiverOpt: var r } => r,
                BoundCall { ReceiverOpt: var r } => r,
                _ => throw ExceptionUtilities.UnexpectedValue(this.IndexerOrSliceAccess.Kind)
            };

            Debug.Assert(receiver is not null);
            return receiver;
        }
    }
}
