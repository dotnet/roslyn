// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Makes the Microsoft.CodeAnalysis.EmbeddedAttribute available in every compilation.
    /// </summary>
    internal sealed class InjectedEmbeddedAttributeSymbol : InjectedAttributeSymbol
    {
        private InjectedEmbeddedAttributeSymbol(
            AttributeDescription description,
            NamespaceSymbol containingNamespace,
            CSharpCompilation compilation,
            Func<CSharpCompilation, NamedTypeSymbol, DiagnosticBag, ImmutableArray<MethodSymbol>> getConstructors,
            DiagnosticBag diagnostics)
            : base(description, containingNamespace, compilation, getConstructors, diagnostics)
        {
        }

        public static InjectedEmbeddedAttributeSymbol Create(NamespaceSymbol containingNamespace)
        {
            return new InjectedEmbeddedAttributeSymbol(AttributeDescription.CodeAnalysisEmbeddedAttribute, containingNamespace, containingNamespace.DeclaringCompilation, makeNonNullTypesAttributeConstructor, new DiagnosticBag());

            ImmutableArray<MethodSymbol> makeNonNullTypesAttributeConstructor(CSharpCompilation compilation, NamedTypeSymbol containingType, DiagnosticBag diagnostics)
            {
                return ImmutableArray.Create<MethodSymbol>(new EmbeddedAttributeConstructorSymbol(containingType));
            }
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
            => new AttributeUsageInfo(validTargets: AttributeTargets.All, allowMultiple: false, inherited: false);

        private sealed class EmbeddedAttributeConstructorSymbol : SynthesizedInstanceConstructor
        {
            internal EmbeddedAttributeConstructorSymbol(NamedTypeSymbol containingType)
                : base(containingType)
            {
                Debug.Assert(containingType is InjectedAttributeSymbol);
            }

            public override ImmutableArray<ParameterSymbol> Parameters
                => ImmutableArray<ParameterSymbol>.Empty;

            internal override bool SynthesizesLoweredBoundBody
                => true;

            /// <summary>
            /// Note: this method captures diagnostics into the containing type (an injected attribute symbol) instead,
            /// as we don't yet know if the containing type will be emitted.
            /// </summary>
            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                var containingType = (InjectedAttributeSymbol)ContainingType;
                GenerateMethodBodyCore(compilationState, containingType.Diagnostics);
            }
        }
    }
}
