// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

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
                CodeGenerationConstructorInfo.GetTypeName(this),
                CodeGenerationConstructorInfo.GetStatements(this),
                CodeGenerationConstructorInfo.GetBaseConstructorArgumentsOpt(this),
                CodeGenerationConstructorInfo.GetThisConstructorArgumentsOpt(this));

            return result;
        }
    }
}
