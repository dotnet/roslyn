// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal static class RoslynExtensions
{
    public static bool TryGetTypeByMetadataName(
        this Compilation compilation,
        string fullyQualifiedMetadataName,
        [NotNullWhen(true)] out INamedTypeSymbol? result)
    {
        result = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
        return result is not null;
    }
}
