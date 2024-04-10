// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal class CodeGenerationConversionSymbol(
    INamedTypeSymbol containingType,
    ImmutableArray<AttributeData> attributes,
    Accessibility declaredAccessibility,
    DeclarationModifiers modifiers,
    ITypeSymbol toType,
    IParameterSymbol fromType,
    bool isImplicit,
    ImmutableArray<AttributeData> toTypeAttributes,
    string documentationCommentXml) : CodeGenerationMethodSymbol(containingType,
          attributes,
          declaredAccessibility,
          modifiers,
          returnType: toType,
          refKind: RefKind.None,
          explicitInterfaceImplementations: default,
          name: isImplicit
                  ? WellKnownMemberNames.ImplicitConversionName
                  : WellKnownMemberNames.ExplicitConversionName,
          typeParameters: ImmutableArray<ITypeParameterSymbol>.Empty,
          parameters: ImmutableArray.Create(fromType),
          returnTypeAttributes: toTypeAttributes,
          documentationCommentXml)
{
    public override MethodKind MethodKind => MethodKind.Conversion;
}
