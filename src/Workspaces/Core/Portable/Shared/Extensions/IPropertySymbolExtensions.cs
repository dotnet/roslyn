// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class IPropertySymbolExtensions
{
    public static bool IsWritableInConstructor(this IPropertySymbol property)
        => property.SetMethod != null || ContainsBackingField(property);

    private static bool ContainsBackingField(IPropertySymbol property)
        => property.GetBackingFieldIfAny() != null;
}
