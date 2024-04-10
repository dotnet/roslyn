// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
}
