// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundIndexerAccess
    {
        internal BoundIndexerAccess WithReceiver(BoundExpression receiver)
        {
            return this.Update(receiver, this.Indexer, this.Arguments, this.ArgumentNamesOpt,
                this.ArgumentRefKindsOpt, this.Expanded, this.ArgsToParamsOpt, this.DefaultArguments, this.Type);
        }
    }
}
