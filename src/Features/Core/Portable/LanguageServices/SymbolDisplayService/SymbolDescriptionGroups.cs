// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    [Flags]
    internal enum SymbolDescriptionGroups
    {
        None = 0,
        MainDescription = 1 << 0,
        AwaitableUsageText = 1 << 1,
        Documentation = 1 << 2,
        TypeParameterMap = 1 << 3,
        AnonymousTypes = 1 << 4,
        Exceptions = 1 << 5,
        All = MainDescription | AwaitableUsageText | Documentation | TypeParameterMap | AnonymousTypes | Exceptions
    }
}
