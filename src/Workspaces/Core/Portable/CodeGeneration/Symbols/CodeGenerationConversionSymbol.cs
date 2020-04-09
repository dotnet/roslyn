// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationConversionSymbol : CodeGenerationMethodSymbol
    {
        public CodeGenerationConversionSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol toType,
            IParameterSymbol fromType,
            bool isImplicit,
            ImmutableArray<AttributeData> toTypeAttributes)
            : base(containingType,
                  attributes,
                  declaredAccessibility,
                  modifiers,
                  returnType: toType,
                  refKind: RefKind.None,
                  explicitInterfaceImplementations: default,
                  name: isImplicit ?
                      WellKnownMemberNames.ImplicitConversionName :
                      WellKnownMemberNames.ExplicitConversionName,
                  typeParameters: ImmutableArray<ITypeParameterSymbol>.Empty,
                  parameters: ImmutableArray.Create(fromType),
                  returnTypeAttributes: toTypeAttributes)
        {
        }

        public override MethodKind MethodKind => MethodKind.Conversion;
    }
}
