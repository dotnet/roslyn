// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal enum ProjectReadOnlyReason
    {
        /// <summary>
        /// Project module was successfully loaded into the debuggee process when edit session started and is editable.
        /// </summary>
        None,

        /// <summary>
        /// Project hasn't been built at the time debugging session started (or the binary is unreadable).
        /// </summary>
        MetadataNotAvailable,

        /// <summary>
        /// One of the following reasons
        /// - Module wasn't loaded into the debuggee process when edit session started.
        /// - The debugger has been attached to an already running process.
        /// - The code being debugged was optimized at build or runtime.
        /// - The assembly being debugged is loaded as domain-neutral.
        /// - The assembly being debugged is runtime-generated (Reflection.Emit).
        /// - IntelliTrace is enabled.
        /// </summary>
        NotLoaded,
    }
}
