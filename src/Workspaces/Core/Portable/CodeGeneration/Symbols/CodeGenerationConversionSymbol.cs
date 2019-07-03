// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

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
