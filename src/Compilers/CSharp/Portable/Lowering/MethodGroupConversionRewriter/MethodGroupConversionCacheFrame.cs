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
    internal sealed class MethodGroupConversionCacheFrame : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly Symbol FrameContainer;
        private readonly MethodSymbol TargetMethod;
        private readonly SynthesizedFieldSymbol CacheBackingField;

        public MethodGroupConversionCacheFrame(
                Symbol frameContainer,
                string name,
                MethodSymbol targetMethod,
                ImmutableArray<TypeParameterSymbol> typeParameters,
                TypeMap typeMap
            )
            : base(name, typeParameters, typeMap)
        {
            TargetMethod = targetMethod;
        }

        public MethodGroupConversionCacheFrame(
                Symbol frameContainer,
                string name,
                MethodSymbol targetMethod
            )
            : base(name, 0, false)
        {
            TargetMethod = targetMethod;
        }

        public override Symbol ContainingSymbol => FrameContainer;

        public override TypeKind TypeKind => TypeKind.Class;

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => true;

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method => TargetMethod;
    }
}
