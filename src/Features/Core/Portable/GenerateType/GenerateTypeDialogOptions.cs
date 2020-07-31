// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.GenerateType
{
    internal class GenerateTypeDialogOptions
    {
        public bool IsPublicOnlyAccessibility { get; }
        public TypeKindOptions TypeKindOptions { get; }
        public bool IsAttribute { get; }

        public GenerateTypeDialogOptions(
            bool isPublicOnlyAccessibility = false,
            TypeKindOptions typeKindOptions = TypeKindOptions.AllOptions,
            bool isAttribute = false)
        {
            IsPublicOnlyAccessibility = isPublicOnlyAccessibility;
            TypeKindOptions = typeKindOptions;
            IsAttribute = isAttribute;
        }
    }
}
