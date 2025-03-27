// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Class to represent a synthesized attribute
    /// </summary>
    internal abstract class SynthesizedAttributeData : CSharpAttributeData
    {
        public static SynthesizedAttributeData Create(CSharpCompilation compilation, MethodSymbol wellKnownMember, ImmutableArray<TypedConstant> arguments, ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
        {
            return new FromMethodAndArguments(compilation, wellKnownMember, arguments, namedArguments);
        }

        public static SynthesizedAttributeData Create(SourceAttributeData original)
        {
            return new FromSourceAttributeData(original);
        }

        private sealed class FromMethodAndArguments : SynthesizedAttributeData
        {
            private readonly CSharpCompilation _compilation;
            private readonly MethodSymbol _wellKnownMember;
            private readonly ImmutableArray<TypedConstant> _arguments;
            private readonly ImmutableArray<KeyValuePair<string, TypedConstant>> _namedArguments;

            internal FromMethodAndArguments(CSharpCompilation compilation, MethodSymbol wellKnownMember, ImmutableArray<TypedConstant> arguments, ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
            {
                Debug.Assert((object)wellKnownMember != null);
                Debug.Assert(!arguments.IsDefault);
                Debug.Assert(!namedArguments.IsDefault); // Frequently empty though.

                _compilation = compilation;
                _wellKnownMember = wellKnownMember;
                _arguments = arguments;
                _namedArguments = namedArguments;
            }

            public override SyntaxReference? ApplicationSyntaxReference => null;
            public override NamedTypeSymbol AttributeClass => _wellKnownMember.ContainingType;
            public override MethodSymbol AttributeConstructor => _wellKnownMember;
            protected internal override ImmutableArray<TypedConstant> CommonConstructorArguments => _arguments;
            protected internal override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments => _namedArguments;
            internal override bool HasErrors => false;
            internal override DiagnosticInfo? ErrorInfo => null;
            internal override bool IsConditionallyOmitted => false;
            internal override Location GetAttributeArgumentLocation(int parameterIndex) => NoLocation.Singleton;

            internal override int GetTargetAttributeSignatureIndex(AttributeDescription description)
            {
                return SourceAttributeData.GetTargetAttributeSignatureIndex(_compilation, AttributeClass, AttributeConstructor, description);
            }

            internal override bool IsTargetAttribute(string namespaceName, string typeName)
            {
                return SourceAttributeData.IsTargetAttribute(AttributeClass, namespaceName, typeName);
            }
        }

        private sealed class FromSourceAttributeData : SynthesizedAttributeData
        {
            private readonly SourceAttributeData _original;

            internal FromSourceAttributeData(SourceAttributeData original)
            {
                _original = original;
            }

            public override SyntaxReference? ApplicationSyntaxReference => _original.ApplicationSyntaxReference;
            public override NamedTypeSymbol AttributeClass => _original.AttributeClass;
            public override MethodSymbol? AttributeConstructor => _original.AttributeConstructor;
            protected internal override ImmutableArray<TypedConstant> CommonConstructorArguments => _original.CommonConstructorArguments;
            protected internal override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments => _original.CommonNamedArguments;

            [MemberNotNullWhen(false, nameof(AttributeConstructor))]
            internal override bool HasErrors => _original.HasErrors;
            internal override DiagnosticInfo? ErrorInfo => _original.ErrorInfo;
            internal override bool IsConditionallyOmitted => _original.IsConditionallyOmitted;

            internal override Location GetAttributeArgumentLocation(int parameterIndex) => _original.GetAttributeArgumentLocation(parameterIndex);
            internal override int GetTargetAttributeSignatureIndex(AttributeDescription description) => _original.GetTargetAttributeSignatureIndex(description);
            internal override bool IsTargetAttribute(string namespaceName, string typeName) => _original.IsTargetAttribute(namespaceName, typeName);
        }
    }
}
