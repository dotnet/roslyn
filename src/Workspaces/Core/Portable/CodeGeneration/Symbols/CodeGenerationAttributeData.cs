﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationAttributeData : AttributeData
    {
        private readonly INamedTypeSymbol _attributeClass;
        private readonly ImmutableArray<TypedConstant> _constructorArguments;
        private readonly ImmutableArray<KeyValuePair<string, TypedConstant>> _namedArguments;

        protected override INamedTypeSymbol CommonAttributeClass { get { return _attributeClass; } }
        protected override IMethodSymbol CommonAttributeConstructor { get { return null; } }
        protected override ImmutableArray<TypedConstant> CommonConstructorArguments { get { return _constructorArguments; } }
        protected override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments { get { return _namedArguments; } }
        protected override SyntaxReference CommonApplicationSyntaxReference { get { return null; } }

        public CodeGenerationAttributeData(
            INamedTypeSymbol attributeClass,
            ImmutableArray<TypedConstant> constructorArguments,
            ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
        {
            _attributeClass = attributeClass;
            _constructorArguments = constructorArguments.NullToEmpty();
            _namedArguments = namedArguments.NullToEmpty();
        }
    }
}
