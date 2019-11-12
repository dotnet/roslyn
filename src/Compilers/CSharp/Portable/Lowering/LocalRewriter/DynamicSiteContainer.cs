// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class DynamicSiteContainer : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly MethodSymbol _topLevelMethod;

        internal DynamicSiteContainer(string name, MethodSymbol topLevelMethod)
            : base(name, topLevelMethod)
        {
            Debug.Assert(topLevelMethod != null);
            _topLevelMethod = topLevelMethod;
        }

        public override Symbol ContainingSymbol
        {
            get { return _topLevelMethod.ContainingSymbol; }
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Class; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get { return true; }
        }

        IMethodSymbolInternal ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return _topLevelMethod; }
        }
    }
}
