// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class StateMachineTypeSymbol : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        public readonly MethodSymbol KickoffMethod;

        public StateMachineTypeSymbol(string name, MethodSymbol kickoffMethod)
            : base(name, kickoffMethod)
        {
            Debug.Assert(kickoffMethod != null);
            this.KickoffMethod = kickoffMethod;
        }

        public override Symbol ContainingSymbol
        {
            get { return KickoffMethod.ContainingType; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get
            {
                // MoveNext method contains user code from the async/iterator method:
                return true;
            }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return KickoffMethod; }
        }
    }
}
