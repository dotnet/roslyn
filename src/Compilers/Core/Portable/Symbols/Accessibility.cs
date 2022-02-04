// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Enumeration for common accessibility combinations.
    /// </summary>
    public enum Accessibility
    {
        /// <summary>
        /// No accessibility specified.
        /// </summary>
        NotApplicable = 0,

        // DO NOT CHANGE ORDER OF THESE ENUM VALUES
        Private = 1,

        /// <summary>
        /// Only accessible where both protected and internal members are accessible
        /// (more restrictive than <see cref="Protected"/>, <see cref="Internal"/> and <see cref="ProtectedOrInternal"/>).
        /// </summary>
        ProtectedAndInternal = 2,

        /// <summary>
        /// Only accessible where both protected and friend members are accessible
        /// (more restrictive than <see cref="Protected"/>, <see cref="Friend"/> and <see cref="ProtectedOrFriend"/>).
        /// </summary>
        ProtectedAndFriend = ProtectedAndInternal,

        Protected = 3,

        Internal = 4,
        Friend = Internal,

        /// <summary>
        /// Accessible wherever either protected or internal members are accessible
        /// (less restrictive than <see cref="Protected"/>, <see cref="Internal"/> and <see cref="ProtectedAndInternal"/>).
        /// </summary>
        ProtectedOrInternal = 5,

        /// <summary>
        /// Accessible wherever either protected or friend members are accessible
        /// (less restrictive than <see cref="Protected"/>, <see cref="Friend"/> and <see cref="ProtectedAndFriend"/>).
        /// </summary>
        ProtectedOrFriend = ProtectedOrInternal,

        Public = 6
    }
}
