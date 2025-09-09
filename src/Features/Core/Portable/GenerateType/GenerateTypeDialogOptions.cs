// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.GenerateType;

internal sealed class GenerateTypeDialogOptions(
    bool isPublicOnlyAccessibility = false,
    TypeKindOptions typeKindOptions = TypeKindOptions.AllOptions,
    bool isAttribute = false)
{
    public bool IsPublicOnlyAccessibility { get; } = isPublicOnlyAccessibility;
    public TypeKindOptions TypeKindOptions { get; } = typeKindOptions;
    public bool IsAttribute { get; } = isAttribute;
}
