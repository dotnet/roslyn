// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationConstructorSymbol : CodeGenerationMethodSymbol
    {
        public CodeGenerationConstructorSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            ImmutableArray<IParameterSymbol> parameters)
            : base(containingType,
                   attributes,
                   accessibility,
                   modifiers,
                   returnType: null,
                   refKind: RefKind.None,
                   explicitInterfaceImplementations: default,
                   name: string.Empty,
                   typeParameters: ImmutableArray<ITypeParameterSymbol>.Empty,
                   parameters: parameters,
                   returnTypeAttributes: ImmutableArray<AttributeData>.Empty)
        {
        }

        public override MethodKind MethodKind => MethodKind.Constructor;

        protected override CodeGenerationSymbol Clone()
        {
            var result = new CodeGenerationConstructorSymbol(this.ContainingType, this.GetAttributes(), this.DeclaredAccessibility, this.Modifiers, this.Parameters);

            CodeGenerationConstructorInfo.Attach(result,
                CodeGenerationConstructorInfo.GetIsUnsafe(this),
                CodeGenerationConstructorInfo.GetTypeName(this),
                CodeGenerationConstructorInfo.GetStatements(this),
                CodeGenerationConstructorInfo.GetBaseConstructorArgumentsOpt(this),
                CodeGenerationConstructorInfo.GetThisConstructorArgumentsOpt(this));

            return result;
        }
    }
}
