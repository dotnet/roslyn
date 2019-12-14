// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
