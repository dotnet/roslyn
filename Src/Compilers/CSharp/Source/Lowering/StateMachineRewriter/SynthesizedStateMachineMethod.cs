// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// State machine interface method implementation.
    /// </summary>
    internal sealed class SynthesizedStateMachineMethod : SynthesizedImplementationMethod, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly bool hasMethodBodyDependency;

        public SynthesizedStateMachineMethod(
            string name,            
            MethodSymbol interfaceMethod,
            NamedTypeSymbol implementingType,
            MethodSymbol asyncKickoffMethod,
            PropertySymbol associatedProperty,
            bool debuggerHidden,
            bool hasMethodBodyDependency)
            : base(interfaceMethod, implementingType,  name, debuggerHidden, associatedProperty, asyncKickoffMethod)
        {
            this.hasMethodBodyDependency = hasMethodBodyDependency;
        }

        public bool HasMethodBodyDependency
        {
            get { return hasMethodBodyDependency; }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return ((ISynthesizedMethodBodyImplementationSymbol)ContainingSymbol).Method; }
        }
    }
}
