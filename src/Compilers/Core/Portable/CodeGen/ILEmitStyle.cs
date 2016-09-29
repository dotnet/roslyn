// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal enum ILEmitStyle : byte
    {
        // no optimizations
        // add additional debug specific emit 
        // like nops for sequence points mapping to no IL
        Debug = 0,

        // do optimizations that do not diminish debug experience
        DebugFriendlyRelease = 1,

        // do all optimizations
        Release = 2,
    }
}
