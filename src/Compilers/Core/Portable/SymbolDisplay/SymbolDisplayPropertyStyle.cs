// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the options for how properties are displayed in symbol descriptions.
    /// </summary>
    public enum SymbolDisplayPropertyStyle
    {
        /// <summary>
        /// Shows only the names of properties.
        /// <see cref="SymbolDisplayMemberOptions"/>
        /// </summary>
        NameOnly = 0,

        /// <summary>
        /// Indicates whether the property is readable and/or writable.
        /// In C#, this is accomplished by including accessors.
        /// In Visual Basic, this is accomplished by including the <c>ReadOnly</c> or <c>WriteOnly</c>
        /// keyword, as appropriate.
        /// </summary>
        ShowReadWriteDescriptor = 1,
    }
}
