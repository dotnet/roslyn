// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [Flags]
    internal enum StateMachineKinds
    {
        None = 0,
        Async = 1,
        Iterator = 1 << 1,
    }
}
