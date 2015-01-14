// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a compiler-generated field for a hoisted iterator or async local.
    /// </summary>
    internal sealed class StateMachineHoistedLocalSymbol : SynthesizedFieldSymbolBase, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly TypeSymbol type;
        private readonly int index;

        public StateMachineHoistedLocalSymbol(
            NamedTypeSymbol stateMachineType,
            TypeSymbol type,
            string name,
            int index)
            : base(stateMachineType, name, isPublic: true, isReadOnly: false, isStatic: false)
        {
            Debug.Assert(index >= 1);
            Debug.Assert((object)type != null);
            this.type = type;
            this.index = index;
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return this.type;
        }

        internal override int IteratorLocalIndex
        {
            get { return index; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get { return true; }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return ((ISynthesizedMethodBodyImplementationSymbol)ContainingSymbol).Method; }
        }
    }
}
