// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Specifies a hash algorithms used for hashing source files.
    /// </summary>
    public enum SourceHashAlgorithm
    {
        /// <summary>
        /// No algorithm specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Secure Hash Algorithm 1.
        /// </summary>
        Sha1 = 1,

        /// <summary>
        /// Secure Hash Algorithm 2 with a hash size of 256 bits.
        /// </summary>
        Sha256 = 2,
    }

    internal static class SourceHashAlgorithmUtils
    {
#pragma warning disable CA1802 // Use literals where appropriate
        public static readonly SourceHashAlgorithm DefaultHashAlgorithm = SourceHashAlgorithm.Sha256;
#pragma warning restore CA1802 // Use literals where appropriate
    }
}
