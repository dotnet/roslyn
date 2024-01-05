// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedEmbeddedScopedRefAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
    {
        private readonly ImmutableArray<MethodSymbol> _constructors;

        public SynthesizedEmbeddedScopedRefAttributeSymbol(
            string name,
            NamespaceSymbol containingNamespace,
            ModuleSymbol containingModule,
            NamedTypeSymbol systemAttributeType)
            : base(name, containingNamespace, containingModule, baseType: systemAttributeType)
        {
            _constructors = ImmutableArray.Create<MethodSymbol>(
                new SynthesizedEmbeddedAttributeConstructorWithBodySymbol(
                    this,
                    getParameters: m => ImmutableArray<ParameterSymbol>.Empty,
                    getConstructorBody: (_, _, _) => { }));
        }

        public override ImmutableArray<MethodSymbol> Constructors => _constructors;

        internal override AttributeUsageInfo GetAttributeUsageInfo() =>
            new AttributeUsageInfo(AttributeTargets.Parameter, allowMultiple: false, inherited: false);
    }
}
