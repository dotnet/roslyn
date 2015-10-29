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
        private readonly Symbol Container;
        public override Symbol ContainingSymbol => Container;

        private readonly MethodSymbol TargetMethod;
        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method => TargetMethod;
        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => true;

        private readonly MethodGroupConversionCacheFrameConstructor _Constructor;
        internal override MethodSymbol Constructor => _Constructor;

        internal SynthesizedFieldSymbol FieldForCachedDelegate { get; private set; }

        public override TypeKind TypeKind => TypeKind.Class;

        private MethodGroupConversionCacheFrame(
                Symbol container,
                string name,
                int typeParametersCount,
                MethodSymbol targetMethod
            )
            : base(name, typeParametersCount, true)
        {
            Container = container;
            TargetMethod = targetMethod;
            _Constructor = new MethodGroupConversionCacheFrameConstructor(this);
        }

        internal static MethodGroupConversionCacheFrame Create(Symbol container, int typeParametersCount, TypeSymbol delegateType, MethodSymbol targetMethod)
        {
            var frameName = "<>S_"; //GeneratedNames.
            var fieldName = "<>F_"; //GeneratedNames.

            var frameSymbol = new MethodGroupConversionCacheFrame(container, frameName, typeParametersCount, targetMethod);
            var fieldSymbol = new SynthesizedFieldSymbol(frameSymbol, delegateType, fieldName, isPublic: true, isStatic: true);
            frameSymbol.FieldForCachedDelegate = fieldSymbol;

            return frameSymbol;
        }

        internal void Sythesize(TypeCompilationState compilationState)
        {
            throw new NotImplementedException();
        }
    }
}
