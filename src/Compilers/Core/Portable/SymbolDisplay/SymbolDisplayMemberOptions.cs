// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the options for how members are displayed in the description of a symbol.
    /// </summary>
    [Flags]
    public enum SymbolDisplayMemberOptions
    {
        /// <summary>
        /// Includes only the name of the member.
        /// </summary>
        None = 0,

        /// <summary>
        /// Includes the (return) type of the method/field/property/event.
        /// </summary>
        IncludeType = 1 << 0,

        /// <summary>
        /// Includes the modifiers of the member. For example, "static readonly" in C# or "Shared ReadOnly" in Visual Basic.
        /// <para>
        /// Accessibility modifiers are controlled separately by <see cref="IncludeAccessibility"/>.
        /// </para>
        /// </summary>
        IncludeModifiers = 1 << 1,

        /// <summary>
        /// Includes the accessibility modifiers of the member. For example, "public" in C# or "Public" in Visual Basic.
        /// </summary>
        IncludeAccessibility = 1 << 2,

        /// <summary>
        /// Includes the name of corresponding interface on members that explicitly implement interface members. For example, "IGoo.Bar { get; }".        
        /// <para>
        /// This option has no effect in Visual Basic.
        /// </para>
        /// </summary>
        IncludeExplicitInterface = 1 << 3,

        /// <summary>
        /// Includes the parameters of methods and properties/indexers.        
        /// <para>
        /// See <see cref="SymbolDisplayParameterOptions"/> for finer-grained settings.
        /// </para>
        /// </summary>
        IncludeParameters = 1 << 4,

        /// <summary>
        /// Includes the name of the type containing the member.        
        /// <para>
        /// The format of the containing type is determined by <see cref="SymbolDisplayTypeQualificationStyle"/>.
        /// </para>
        /// </summary>
        IncludeContainingType = 1 << 5,

        /// <summary>
        /// Includes the value of the member if is a constant.
        /// </summary>
        IncludeConstantValue = 1 << 6,

        /// <summary>
        /// Includes the <c>ref</c>, <c>ref readonly</c>, <c>ByRef</c> keywords for ref-returning methods and properties/indexers.
        /// Also includes the <c>readonly</c> keyword on methods, properties/indexers, and events due to the keyword
        /// changing the <c>this</c> parameter's ref kind from <c>ref</c> to <c>ref readonly</c>.
        /// </summary>
        IncludeRef = 1 << 7,
    }
}
