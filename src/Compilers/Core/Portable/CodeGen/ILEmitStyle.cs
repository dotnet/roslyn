// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
