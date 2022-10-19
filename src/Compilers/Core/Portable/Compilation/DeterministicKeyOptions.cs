// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    [Flags]
    internal enum DeterministicKeyOptions
    {
        /// <summary>
        /// The default is to include all inputs to the compilation which impact the output of the 
        /// compilation: binaries or diagnostics.
        /// </summary>
        Default = 0b0,

        /// <summary>
        /// Ignore all file paths, but still include file names, in the deterministic key.
        /// </summary>
        /// <remarks>
        /// This is useful for scenarios where the consumer is interested in the content of the 
        /// <see cref="Compilation"/> being the same but aren't concerned precisely with the file
        /// path of the content. A typical example of this type of consumer is one that operates 
        /// in CI where the path changes frequently.
        /// </remarks>
        IgnorePaths = 0b0001,

        /// <summary>
        /// Ignore the versions of the tools contributing to the build (compiler and runtime)
        /// </summary>
        /// <remarks>
        /// Compiler output is not guaranteed to be deterministically equivalent between versions
        /// but very often is for wide ranges of versions. This option is useful for consumers 
        /// who are comfortable ignoring the versions when looking at compiler output. 
        /// </remarks>
        IgnoreToolVersions = 0b0010,
    }
}
