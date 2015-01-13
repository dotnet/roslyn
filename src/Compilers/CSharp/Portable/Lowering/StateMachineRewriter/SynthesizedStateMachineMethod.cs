// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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
            StateMachineTypeSymbol stateMachineType,
            PropertySymbol associatedProperty,
            bool debuggerHidden,
            bool generateDebugInfo,
            bool hasMethodBodyDependency)
            : base(interfaceMethod, stateMachineType, name, debuggerHidden, generateDebugInfo, associatedProperty)
        {
            this.hasMethodBodyDependency = hasMethodBodyDependency;
        }

        public StateMachineTypeSymbol StateMachineType
        {
            get { return (StateMachineTypeSymbol)ContainingSymbol; }
        }

        public bool HasMethodBodyDependency
        {
            get { return hasMethodBodyDependency; }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return StateMachineType.KickoffMethod; }
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return this.StateMachineType.KickoffMethod.CalculateLocalSyntaxOffset(localPosition, localTree);
        }
    }
}
