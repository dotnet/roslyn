// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies how to display delegates (just the name or the name with the signature).
    /// </summary>
    public enum SymbolDisplayDelegateStyle
    {
        /// <summary>
        /// Shows only the name of the delegate (e.g. "SomeDelegate").
        /// </summary>
        NameOnly = 0,

        /// <summary>
        /// Shows the name and the parameters of the delegate (e.g. "SomeDelegate(int x)").  
        /// <para>
        /// This option determines whether the parameter list (the <c>(...)</c> part) is shown.
        /// The content of the parameter list is controlled by <see cref="SymbolDisplayParameterOptions"/>.
        /// </para>
        /// </summary>
        NameAndParameters = 1,

        /// <summary>
        /// Shows the name and the signature of the delegate (e.g. "void SomeDelegate(int x)").
        /// <para>
        /// This option determines whether the full signature (return type and parameter list) is shown.
        /// The content of the parameter list is controlled by <see cref="SymbolDisplayParameterOptions"/>.
        /// </para>
        /// </summary>
        NameAndSignature = 2,
    }
}
