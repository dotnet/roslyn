// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationAttributeData : AttributeData
    {
        private readonly INamedTypeSymbol attributeClass;
        private readonly ImmutableArray<TypedConstant> constructorArguments;
        private readonly ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments;

        protected override INamedTypeSymbol CommonAttributeClass { get { return attributeClass; } }
        protected override IMethodSymbol CommonAttributeConstructor { get { return null; } }
        protected override ImmutableArray<TypedConstant> CommonConstructorArguments { get { return constructorArguments; } }
        protected override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments { get { return namedArguments; } }
        protected override SyntaxReference CommonApplicationSyntaxReference { get { return null; } }

        public CodeGenerationAttributeData(
            INamedTypeSymbol attributeClass,
            ImmutableArray<TypedConstant> constructorArguments,
            ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
        {
            this.attributeClass = attributeClass;
            this.constructorArguments = constructorArguments.NullToEmpty();
            this.namedArguments = namedArguments.NullToEmpty();
        }
    }
}