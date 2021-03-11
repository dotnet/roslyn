// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    public enum Platform
    {
        /// <summary>
        /// AnyCPU (default) compiles the assembly to run on any platform.
        /// </summary>
        AnyCpu = 0,

        /// <summary>
        /// x86 compiles the assembly to be run by the 32-bit, x86-compatible common language runtime.
        /// </summary>
        X86 = 1,

        /// <summary>
        /// x64 compiles the assembly to be run by the 64-bit common language runtime on a computer that supports the AMD64 or EM64T instruction set.
        /// </summary>
        X64 = 2,

        /// <summary>
        /// Itanium compiles the assembly to be run by the 64-bit common language runtime on a computer with an Itanium processor.
        /// </summary>
        Itanium = 3,

        /// <summary>
        /// Compiles your assembly to run on any platform. Your application runs in 32-bit mode on systems that support both 64-bit and 32-bit applications.
        /// </summary>
        AnyCpu32BitPreferred = 4,

        /// <summary>
        /// Compiles your assembly to run on a computer that has an Advanced RISC Machine (ARM) processor.
        /// </summary>
        Arm = 5,

        /// <summary>
        /// Compiles your assembly to run on a computer that has an Advanced RISC Machine 64 bit (ARM64) processor.
        /// </summary>
        Arm64 = 6,
    };

    internal static partial class EnumBounds
    {
        internal static bool IsValid(this Platform value)
        {
            return value >= Platform.AnyCpu && value <= Platform.Arm64;
        }

        internal static bool Requires64Bit(this Platform value)
        {
            return value == Platform.X64 || value == Platform.Itanium || value == Platform.Arm64;
        }

        internal static bool Requires32Bit(this Platform value)
        {
            return value == Platform.X86;
        }
    }
}
