// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedLambdaCacheFieldSymbol : SynthesizedFieldSymbolBase, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly TypeWithAnnotations _type;
        private readonly MethodSymbol _topLevelMethod;

        public SynthesizedLambdaCacheFieldSymbol(NamedTypeSymbol containingType, TypeSymbol type, string name, MethodSymbol topLevelMethod, bool isReadOnly, bool isStatic)
            : base(containingType, name, isPublic: true, isReadOnly: isReadOnly, isStatic: isStatic)
        {
            Debug.Assert((object)type != null);
            Debug.Assert((object)topLevelMethod != null);
            _type = TypeWithAnnotations.Create(type);
            _topLevelMethod = topLevelMethod;
        }

        internal override bool SuppressDynamicAttribute => true;

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method => _topLevelMethod;

        // When the containing top-level method body is updated we don't need to attempt to update the cache field
        // since a field update is a no-op.
        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => false;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _type;
        }
    }
}
