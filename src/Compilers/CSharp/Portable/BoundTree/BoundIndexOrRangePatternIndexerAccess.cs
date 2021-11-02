// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundIndexOrRangePatternIndexerAccess
    {
        internal BoundIndexOrRangePatternIndexerAccess WithLengthOrCountAccess(BoundExpression lengthOrCountAccess)
        {
            return new BoundIndexOrRangePatternIndexerAccess(this.Syntax, this.Receiver, this.Argument, lengthOrCountAccess, 
                this.IndexerAccess, this.ReceiverPlaceholder, this.ArgumentPlaceholders, this.Type, this.HasErrors);
        }
    }
}
