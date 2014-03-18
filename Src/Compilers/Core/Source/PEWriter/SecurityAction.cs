// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Cci
{
    internal enum SecurityAction
    {
        // this value is accepted by the compiler, but not part of the documented SecurityAction enum.
        Undocumented = 1,

        // Demand permission of all caller
        Demand = 2,

        // Assert permission so callers don't need
        Assert = 3,

        // Deny permissions so checks will fail
        Deny = 4,

        // Reduce permissions so check will fail
        PermitOnly = 5,

        // Demand permission of caller
        LinkDemand = 6,

        // Demand permission of a subclass
        InheritanceDemand = 7,

        // Request minimum permissions to run
        RequestMinimum = 8,

        // Request optional additional permissions
        RequestOptional = 9,

        // Refuse to be granted these permissions
        RequestRefuse = 10,
    }
}
