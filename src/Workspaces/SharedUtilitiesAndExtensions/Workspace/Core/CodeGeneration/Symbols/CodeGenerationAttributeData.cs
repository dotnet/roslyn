// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationAttributeData(
        INamedTypeSymbol attributeClass,
        ImmutableArray<TypedConstant> constructorArguments,
        ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments) : AttributeData
    {
        private readonly ImmutableArray<TypedConstant> _constructorArguments = constructorArguments.NullToEmpty();
        private readonly ImmutableArray<KeyValuePair<string, TypedConstant>> _namedArguments = namedArguments.NullToEmpty();

        protected override INamedTypeSymbol CommonAttributeClass => attributeClass;
        protected override IMethodSymbol CommonAttributeConstructor => null;
        protected override ImmutableArray<TypedConstant> CommonConstructorArguments => _constructorArguments;
        protected override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments => _namedArguments;
        protected override SyntaxReference CommonApplicationSyntaxReference => null;
    }
}
