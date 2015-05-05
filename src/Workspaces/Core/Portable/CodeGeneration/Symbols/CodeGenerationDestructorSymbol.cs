// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationDestructorSymbol : CodeGenerationMethodSymbol
    {
        public CodeGenerationDestructorSymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes) :
            base(containingType,
                 attributes,
                 Accessibility.NotApplicable,
                 default(DeclarationModifiers),
                 returnType: null,
                 returnsByRef: false,
                 explicitInterfaceSymbolOpt: null,
                 name: string.Empty,
                 typeParameters: SpecializedCollections.EmptyList<ITypeParameterSymbol>(),
                 parameters: SpecializedCollections.EmptyList<IParameterSymbol>(),
                 returnTypeAttributes: SpecializedCollections.EmptyList<AttributeData>())
        {
        }

        public override MethodKind MethodKind
        {
            get
            {
                return MethodKind.Destructor;
            }
        }

        protected override CodeGenerationSymbol Clone()
        {
            var result = new CodeGenerationDestructorSymbol(this.ContainingType, this.GetAttributes());

            CodeGenerationDestructorInfo.Attach(result,
                CodeGenerationDestructorInfo.GetTypeName(this),
                CodeGenerationDestructorInfo.GetStatements(this));

            return result;
        }
    }
}
