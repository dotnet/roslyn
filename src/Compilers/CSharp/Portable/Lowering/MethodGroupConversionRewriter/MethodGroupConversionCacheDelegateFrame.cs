// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class MethodGroupConversionCacheDelegateFrame : SynthesizedContainer
    {
        private readonly Symbol _ContainingSymbol;
        public override Symbol ContainingSymbol => _ContainingSymbol;

        public override TypeKind TypeKind => TypeKind.Class;

        public override bool IsStatic => true;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public readonly SynthesizedFieldSymbol DelegateField;

        private MethodGroupConversionCacheDelegateFrame(
                MethodGroupConversionCacheTargetFrame containerFrame,
                NamedTypeSymbol delegateType,
                string name,
                int typeParametersCount
            )
            : base(name, typeParametersCount, true)
        {
            Debug.Assert(delegateType.Arity == typeParametersCount);

            _ContainingSymbol = containerFrame;

            var constructedDelegateType = typeParametersCount > 0 ? delegateType.Construct(TypeParameters) : delegateType;
            var fieldName = GeneratedNames.MakeMethodGroupConversionCacheDelegateFieldName();

            DelegateField = new SynthesizedFieldSymbol(this, constructedDelegateType, fieldName, isPublic: true, isStatic: true);
        }

        public static MethodGroupConversionCacheDelegateFrame Create(MethodGroupConversionCacheTargetFrame containerFrame, NamedTypeSymbol delegateType)
        {
            var frameName = GeneratedNames.MakeMethodGroupConversionCacheDelegateFrameName(delegateType);
            return new MethodGroupConversionCacheDelegateFrame(containerFrame, delegateType.OriginalDefinition, frameName, delegateType.Arity);
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            var members = base.GetMembers();
            if (DelegateField != null)
            {
                members = ImmutableArray.Create<Symbol>(DelegateField).AddRange(members);
            }
            return members;
        }
    }
}
