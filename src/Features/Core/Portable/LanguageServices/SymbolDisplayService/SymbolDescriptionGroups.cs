// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.LanguageService
{
    [Flags]
    internal enum SymbolDescriptionGroups
    {
        None = 0,
        MainDescription = 1 << 0,
        AwaitableUsageText = 1 << 1,
        Documentation = 1 << 2,
        TypeParameterMap = 1 << 3,
        StructuralTypes = 1 << 4,
        Exceptions = 1 << 5,
        Captures = 1 << 6,
        ReturnsDocumentation = 1 << 7,
        ValueDocumentation = 1 << 8,
        RemarksDocumentation = 1 << 9,
        All = MainDescription | AwaitableUsageText | Documentation | TypeParameterMap | StructuralTypes | Exceptions | Captures | ReturnsDocumentation | ValueDocumentation | RemarksDocumentation,
    }
}
