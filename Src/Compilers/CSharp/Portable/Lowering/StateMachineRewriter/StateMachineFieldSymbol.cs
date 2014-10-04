// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a synthesized state machine field.
    /// </summary>
    internal sealed class StateMachineFieldSymbol : SynthesizedFieldSymbolBase, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly TypeSymbol type;

        // 0 if the corresponding captured local is synthesized, 
        // or the field doesn't correspond to a hoisted local.
        // > 0 if it corresponds to the field name
        private readonly int userDefinedHoistedLocalId;

        public StateMachineFieldSymbol(NamedTypeSymbol stateMachineType, TypeSymbol type, string fieldName, bool isPublic)
            : base(stateMachineType, fieldName, isPublic: isPublic, isReadOnly: false, isStatic: false)
        {
            Debug.Assert((object)type != null);
            this.type = type;
        }

        public StateMachineFieldSymbol(NamedTypeSymbol stateMachineType, TypeSymbol type, string localName, int userDefinedHoistedLocalId)
            : base(stateMachineType, localName, isPublic: true, isReadOnly: false, isStatic: false)
        {
            Debug.Assert(userDefinedHoistedLocalId >= 1);
            Debug.Assert((object)type != null);

            this.type = type;
            this.userDefinedHoistedLocalId = userDefinedHoistedLocalId;
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return this.type;
        }

        internal override int UserDefinedHoistedLocalId
        {
            get { return userDefinedHoistedLocalId; }
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