// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationDestructorSymbol : CodeGenerationMethodSymbol
    {
        public CodeGenerationDestructorSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes)
            : base(containingType,
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
        }

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
}
