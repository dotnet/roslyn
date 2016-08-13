// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Debugging
{
    /// <summary>
    /// The kinds of custom debug info that we know how to interpret.
    /// The values correspond to possible values of the "kind" byte
    /// in the record header.
    /// </summary>
    internal enum CustomDebugInfoKind : byte
    {
        UsingInfo = 0,
        ForwardInfo = 1,
        ForwardToModuleInfo = 2,
        StateMachineHoistedLocalScopes = 3,
        ForwardIterator = 4,
        DynamicLocals = 5,
        EditAndContinueLocalSlotMap = 6,
        EditAndContinueLambdaMap = 7,
    }
}
