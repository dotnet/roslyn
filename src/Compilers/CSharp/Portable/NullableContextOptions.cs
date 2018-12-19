// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Specifies the nullable context.
    /// </summary>
    public enum NullableContextOptions : byte
    {
        /// <summary>
        /// Nullable annotation and warning contexts are disabled.
        /// </summary>
        Disable,

        /// <summary>
        /// Nullable annotation and warning contexts are enabled.
        /// </summary>
        Enable,

        /// <summary>
        /// Nullable annotation context is enabled and the nullable warning context is safeonly.
        /// </summary>
        SafeOnly,
    }
}
