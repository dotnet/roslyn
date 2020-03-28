// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationAttributeData : AttributeData
    {
        private readonly INamedTypeSymbol _attributeClass;
        private readonly ImmutableArray<TypedConstant> _constructorArguments;
        private readonly ImmutableArray<KeyValuePair<string, TypedConstant>> _namedArguments;

        protected override INamedTypeSymbol CommonAttributeClass => _attributeClass;
        protected override IMethodSymbol CommonAttributeConstructor => null;
        protected override ImmutableArray<TypedConstant> CommonConstructorArguments => _constructorArguments;
        protected override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments => _namedArguments;
        protected override SyntaxReference CommonApplicationSyntaxReference => null;

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
