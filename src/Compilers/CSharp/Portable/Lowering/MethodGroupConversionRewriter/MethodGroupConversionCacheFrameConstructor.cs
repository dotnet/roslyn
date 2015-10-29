// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class MethodGroupConversionCacheFrameConstructor : SynthesizedInstanceConstructor, ISynthesizedMethodBodyImplementationSymbol
    {
        internal MethodGroupConversionCacheFrameConstructor(MethodGroupConversionCacheFrame frame) : base(frame) { }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => false;

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method => ((ISynthesizedMethodBodyImplementationSymbol)ContainingSymbol).Method;
    }
}
