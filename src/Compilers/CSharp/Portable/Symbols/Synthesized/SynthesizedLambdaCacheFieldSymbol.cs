// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedLambdaCacheFieldSymbol : SynthesizedFieldSymbol, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly MethodSymbol _topLevelMethod;

        public SynthesizedLambdaCacheFieldSymbol(NamedTypeSymbol containingType, TypeSymbol type, string name, MethodSymbol topLevelMethod, bool isReadOnly, bool isStatic)
            : base(containingType, type, name, isPublic: true, isReadOnly: isReadOnly, isStatic: isStatic)
        {
            Debug.Assert(topLevelMethod != null);
            _topLevelMethod = topLevelMethod;
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method => _topLevelMethod;

        // When the containing top-level method body is updated we don't need to attempt to update the cache field
        // since a field update is a no-op.
        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => false;
    }
}
