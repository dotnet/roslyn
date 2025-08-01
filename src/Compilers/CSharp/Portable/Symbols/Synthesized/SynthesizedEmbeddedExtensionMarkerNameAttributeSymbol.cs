// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // Tracked by https://github.com/dotnet/roslyn/issues/78963 : We are not declaring and not initializing the "Name" property yet.
    internal sealed class SynthesizedEmbeddedExtensionMarkerNameAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
    {
        private readonly ImmutableArray<MethodSymbol> _constructors;

        public SynthesizedEmbeddedExtensionMarkerNameAttributeSymbol(
            string name,
            NamespaceSymbol containingNamespace,
            ModuleSymbol containingModule,
            NamedTypeSymbol systemAttributeType,
            TypeSymbol systemStringType)
            : base(name, containingNamespace, containingModule, baseType: systemAttributeType)
        {
            _constructors = ImmutableArray.Create<MethodSymbol>(
                new SynthesizedEmbeddedAttributeConstructorSymbol(
                    this,
                    m => ImmutableArray.Create(SynthesizedParameterSymbol.Create(m, TypeWithAnnotations.Create(systemStringType), 0, RefKind.None, name: "name"))));

            // Ensure we never get out of sync with the description
            Debug.Assert(_constructors.Length == AttributeDescription.ExtensionMarkerNameAttribute.Signatures.Length);
        }

        public override ImmutableArray<MethodSymbol> Constructors => _constructors;

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return new AttributeUsageInfo(
                AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property |
                AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate,
                allowMultiple: false, inherited: false);
        }
    }
}
