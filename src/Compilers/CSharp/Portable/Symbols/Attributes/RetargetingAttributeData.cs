// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a retargeting custom attribute
    /// </summary>
    internal sealed class RetargetingAttributeData : CSharpAttributeData
    {
        private readonly CSharpAttributeData _underlying;
        private readonly NamedTypeSymbol _attributeClass;
        private readonly MethodSymbol? _attributeConstructor;
        private readonly ImmutableArray<TypedConstant> _constructorArguments;
        private readonly ImmutableArray<KeyValuePair<string, TypedConstant>> _namedArguments;

        internal RetargetingAttributeData(
            CSharpAttributeData underlying,
            NamedTypeSymbol attributeClass,
            MethodSymbol? attributeConstructor,
            ImmutableArray<TypedConstant> constructorArguments,
            ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
        {
            Debug.Assert(underlying is SourceAttributeData or SynthesizedAttributeData);

            _underlying = underlying;
            _attributeClass = attributeClass;
            _attributeConstructor = attributeConstructor;
            _constructorArguments = constructorArguments;
            _namedArguments = namedArguments;
        }

        public override NamedTypeSymbol AttributeClass => _attributeClass;
        public override MethodSymbol? AttributeConstructor => _attributeConstructor;
        protected internal override ImmutableArray<TypedConstant> CommonConstructorArguments => _constructorArguments;
        protected internal override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments => _namedArguments;

        public override SyntaxReference? ApplicationSyntaxReference => null;

        internal override bool HasErrors => _underlying.HasErrors || _attributeConstructor is null;

        internal override bool IsConditionallyOmitted => _underlying.IsConditionallyOmitted;

        internal override Location GetAttributeArgumentLocation(int parameterIndex) => _underlying.GetAttributeArgumentLocation(parameterIndex);
        internal override int GetTargetAttributeSignatureIndex(AttributeDescription description) => _underlying.GetTargetAttributeSignatureIndex(description);
        internal override bool IsTargetAttribute(string namespaceName, string typeName) => _underlying.IsTargetAttribute(namespaceName, typeName);
    }
}
