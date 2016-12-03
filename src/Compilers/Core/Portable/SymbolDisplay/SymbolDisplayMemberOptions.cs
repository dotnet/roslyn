﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Includes the modifiers of the member.
        /// For example, "static readonly" in C# or "Shared ReadOnly" in Visual Basic.
        /// </summary>
        /// <remarks>
        /// Accessibility modifiers are controlled separately by <see cref="IncludeAccessibility"/>.
        /// </remarks>
        IncludeModifiers = 1 << 1,

        /// <summary>
        /// Includes the accessibility modifiers of the member.
        /// For example, "public" in C# or "Public" in Visual Basic.
        /// </summary>
        IncludeAccessibility = 1 << 2,

        /// <summary>
        /// Includes the name of corresponding interface on members that explicitly implement
        /// interface members.
        /// For example, "IFoo.Bar { get; }".
        /// </summary>
        /// <remarks>
        /// This option has no effect in Visual Basic.
        /// </remarks>
        IncludeExplicitInterface = 1 << 3,

        /// <summary>
        /// Includes the parameters of methods and properties/indexers.
        /// </summary>
        /// <remarks>
        /// See <see cref="SymbolDisplayParameterOptions"/> for finer-grained settings.
        /// </remarks>
        IncludeParameters = 1 << 4,

        /// <summary>
        /// Includes the name of the type containing the member.
        /// </summary>
        /// <remarks>
        /// The format of the containing type is determined by <see cref="SymbolDisplayTypeQualificationStyle"/>.
        /// </remarks>
        IncludeContainingType = 1 << 5,

        /// <summary>
        /// Includes the value of the member if is a constant.
        /// </summary>
        IncludeConstantValue = 1 << 6,

        /// <summary>
        /// Includes the <c>ref</c>, <c>ByRef</c> keyword for ref-returning methods and properties/indexers.
        /// </summary>
        IncludeRef = 1 << 7,
    }
}
