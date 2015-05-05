// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationConversionSymbol : CodeGenerationMethodSymbol
    {
        public CodeGenerationConversionSymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol toType,
            IParameterSymbol fromType,
            bool isImplicit,
            IList<AttributeData> toTypeAttributes) :
            base(containingType,
                attributes,
                declaredAccessibility,
                modifiers,
                returnType: toType,
                returnsByRef: false,
                explicitInterfaceSymbolOpt: null,
                name: isImplicit ?
                    WellKnownMemberNames.ImplicitConversionName :
                    WellKnownMemberNames.ExplicitConversionName,
                typeParameters: SpecializedCollections.EmptyList<ITypeParameterSymbol>(),
                parameters: new List<IParameterSymbol>(SpecializedCollections.SingletonEnumerable(fromType)),
                returnTypeAttributes: toTypeAttributes)
        {
        }

        public override MethodKind MethodKind
        {
            get
            {
                return MethodKind.Conversion;
            }
        }
    }
}
