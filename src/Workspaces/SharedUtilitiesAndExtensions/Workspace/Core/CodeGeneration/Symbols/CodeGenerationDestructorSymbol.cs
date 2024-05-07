// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal abstract class CodeGenerationDestructorSymbol(
    INamedTypeSymbol containingType,
    ImmutableArray<AttributeData> attributes) : CodeGenerationMethodSymbol(containingType,
         attributes,
         Accessibility.NotApplicable,
         default,
         returnType: null,
         refKind: RefKind.None,
         explicitInterfaceImplementations: default,
         name: string.Empty,
         typeParameters: ImmutableArray<ITypeParameterSymbol>.Empty,
         parameters: ImmutableArray<IParameterSymbol>.Empty,
         returnTypeAttributes: ImmutableArray<AttributeData>.Empty)
{
    public override MethodKind MethodKind => MethodKind.Destructor;

    protected override CodeGenerationSymbol Clone()
    {
        var result = CodeGenerationSymbolMappingFactory.Instance.CreateDestructorSymbol(this.ContainingType, this.GetAttributes());

        CodeGenerationDestructorInfo.Attach(result,
            CodeGenerationDestructorInfo.GetTypeName((IMethodSymbol)this),
            CodeGenerationDestructorInfo.GetStatements((IMethodSymbol)this));

        return (CodeGenerationSymbol)result;
    }
}
