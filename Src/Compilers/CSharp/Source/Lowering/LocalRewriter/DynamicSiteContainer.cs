// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class DynamicSiteContainer : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly MethodSymbol topLevelMethod;

        internal DynamicSiteContainer(string name, MethodSymbol topLevelMethod)
            : base(name, topLevelMethod)
        {
            Debug.Assert(topLevelMethod != null);
            this.topLevelMethod = topLevelMethod;
        }

        public override Symbol ContainingSymbol
        {
            get { return topLevelMethod.ContainingSymbol; }
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Class; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get { return true; }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return topLevelMethod; }
        }
    }
}
