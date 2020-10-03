// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace IOperationGenerator
{
    // PROTOTYPE(iop): Delete this after migration
    internal sealed partial class IOperationClassWriter
    {
        private static readonly HashSet<string> PortedTypes = new HashSet<string>()
        {
            "IEmptyOperation",
            "IBranchOperation",
            "IEndOperation",
            "IStopOperation",
            "ILiteralOperation",
            "ILocalReferenceOperation",
            "IParameterReferenceOperation",
            "IInstanceReferenceOperation",
            "IConditionalAccessInstanceOperation",
            "IDefaultValueOperation",
            "ITypeOfOperation",
            "ISizeOfOperation",
            "IOmittedArgumentOperation",
            "IDiscardOperation",
            "IPlaceholderOperation",
            "IBlockOperation",
            "ISwitchOperation"
        };
    }
}
