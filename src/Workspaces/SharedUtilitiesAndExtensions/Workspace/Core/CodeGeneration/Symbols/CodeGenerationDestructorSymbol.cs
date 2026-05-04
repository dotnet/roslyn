// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal sealed class CodeGenerationDestructorSymbol(
    INamedTypeSymbol? containingType,
    ImmutableArray<AttributeData> attributes) : CodeGenerationMethodSymbol(containingType,
         attributes,
         Accessibility.NotApplicable,
         default,
         returnType: null,
         refKind: RefKind.None,
         explicitInterfaceImplementations: default,
         name: string.Empty,
         typeParameters: [],
         parameters: [],
         returnTypeAttributes: [])
{
    public override MethodKind MethodKind => MethodKind.Destructor;

    protected override CodeGenerationSymbol Clone()
    {
        var result = new CodeGenerationDestructorSymbol(this.ContainingType, this.GetAttributes());

        CodeGenerationDestructorInfo.Attach(result,
            CodeGenerationDestructorInfo.GetTypeName(this),
            CodeGenerationDestructorInfo.GetStatements(this));

        return result;
    }
}
