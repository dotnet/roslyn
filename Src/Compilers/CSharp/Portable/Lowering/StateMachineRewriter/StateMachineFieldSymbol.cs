// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a synthesized state machine helper field 
    /// </summary>
    internal sealed class StateMachineFieldSymbol : SynthesizedFieldSymbolBase, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly TypeSymbol type;

        public StateMachineFieldSymbol(
            NamedTypeSymbol stateMachineType,
            TypeSymbol type,
            string name,
            bool isPublic)
            : base(stateMachineType, name, isPublic, isReadOnly: false, isStatic: false)
        {
            Debug.Assert((object)type != null);
            this.type = type;
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return this.type;
        }

        internal override int IteratorLocalIndex
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            // TODO: hoisted temps?
            get { return false; }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return ((ISynthesizedMethodBodyImplementationSymbol)ContainingSymbol).Method; }
        }
    }
}