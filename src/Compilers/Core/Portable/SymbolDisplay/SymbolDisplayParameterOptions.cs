// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies how parameters are displayed in the description of a (member, property/indexer, or delegate) symbol.
    /// </summary>
    [Flags]
    public enum SymbolDisplayParameterOptions
    {
        /// <summary>
        /// Omits parameters from symbol descriptions.    
        /// <para>
        /// If this option is combined with <see cref="SymbolDisplayMemberOptions.IncludeParameters"/>, then only
        /// the parentheses will be shown (e.g. M()).
        /// </para>
        /// </summary>
        None = 0,

        /// <summary>
        /// Includes the <c>this</c> keyword before the first parameter of an extension method in C#. 
        /// <para>
        /// This option has no effect in Visual Basic.
        /// </para>
        /// </summary>
        IncludeExtensionThis = 1 << 0,

        /// <summary>
        /// Includes the <c>params</c>, <c>scoped</c>, <c>ref</c>, <c>in</c>, <c>out</c>, <c>ByRef</c>, <c>ByVal</c> keywords before parameters.
        /// Replaced by <see cref="IncludeModifiers"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        IncludeParamsRefOut = IncludeModifiers,

        /// <summary>
        /// Includes the <c>params</c>, <c>scoped</c>, <c>ref</c>, <c>in</c>, <c>out</c>, <c>ByRef</c>, <c>ByVal</c> keywords before parameters.
        /// </summary>
        IncludeModifiers = 1 << 1,

        /// <summary>
        /// Includes parameter types in symbol descriptions.
        /// </summary>
        IncludeType = 1 << 2,

        /// <summary>
        /// Includes parameter names in symbol descriptions.
        /// </summary>
        IncludeName = 1 << 3,

        /// <summary>
        /// Includes parameter default values in symbol descriptions.
        /// <para>Ignored if <see cref="IncludeName"/> is not set.
        /// </para>
        /// </summary>
        IncludeDefaultValue = 1 << 4,

        /// <summary>
        /// Includes square brackets around optional parameters.
        /// </summary>
        IncludeOptionalBrackets = 1 << 5,
    }
}
