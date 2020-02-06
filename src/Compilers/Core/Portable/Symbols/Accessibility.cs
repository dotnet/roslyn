// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

        ProtectedAndInternal = 2,
        ProtectedAndFriend = ProtectedAndInternal,

        Protected = 3,

        Internal = 4,
        Friend = Internal,

        ProtectedOrInternal = 5,
        ProtectedOrFriend = ProtectedOrInternal,

        Public = 6
    }
}
