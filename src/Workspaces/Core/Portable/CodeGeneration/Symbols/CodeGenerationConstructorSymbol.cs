// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationConstructorSymbol : CodeGenerationMethodSymbol
    {
        public CodeGenerationConstructorSymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            IList<IParameterSymbol> parameters) :
            base(containingType,
                 attributes,
                 accessibility,
                 modifiers,
                 returnType: null,
                 returnsByRef: false,
                 explicitInterfaceSymbolOpt: null,
                 name: string.Empty,
                 typeParameters: SpecializedCollections.EmptyList<ITypeParameterSymbol>(),
                 parameters: parameters,
                 returnTypeAttributes: SpecializedCollections.EmptyList<AttributeData>())
        {
        }

        public override MethodKind MethodKind
        {
            get
            {
                return MethodKind.Constructor;
            }
        }

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
