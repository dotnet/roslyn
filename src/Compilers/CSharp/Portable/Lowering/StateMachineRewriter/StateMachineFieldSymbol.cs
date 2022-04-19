// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a synthesized state machine field.
    /// </summary>
    internal sealed class StateMachineFieldSymbol : SynthesizedFieldSymbolBase, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly TypeWithAnnotations _type;
        private readonly bool _isThis;

        // -1 if the field doesn't represent a long-lived local or an awaiter.
        internal readonly int SlotIndex;

        internal readonly LocalSlotDebugInfo SlotDebugInfo;

        // Some fields need to be public since they are initialized directly by the kickoff method.
        public StateMachineFieldSymbol(NamedTypeSymbol stateMachineType, TypeWithAnnotations type, string name, bool isPublic, bool isThis)
            : this(stateMachineType, type, name, new LocalSlotDebugInfo(SynthesizedLocalKind.LoweringTemp, LocalDebugId.None), slotIndex: -1, isPublic: isPublic)
        {
            _isThis = isThis;
        }

        public StateMachineFieldSymbol(NamedTypeSymbol stateMachineType, TypeSymbol type, string name, SynthesizedLocalKind synthesizedKind, int slotIndex, bool isPublic)
            : this(stateMachineType, type, name, new LocalSlotDebugInfo(synthesizedKind, LocalDebugId.None), slotIndex, isPublic: isPublic)
        {
        }

        public StateMachineFieldSymbol(NamedTypeSymbol stateMachineType, TypeSymbol type, string name, LocalSlotDebugInfo slotDebugInfo, int slotIndex, bool isPublic) :
            this(stateMachineType, TypeWithAnnotations.Create(type), name, slotDebugInfo, slotIndex, isPublic)
        {
        }

        public StateMachineFieldSymbol(NamedTypeSymbol stateMachineType, TypeWithAnnotations type, string name, LocalSlotDebugInfo slotDebugInfo, int slotIndex, bool isPublic)
            : base(stateMachineType, name, isPublic: isPublic, isReadOnly: false, isStatic: false)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(slotDebugInfo.SynthesizedKind.IsLongLived() == (slotIndex >= 0));

            _type = type;
            this.SlotIndex = slotIndex;
            this.SlotDebugInfo = slotDebugInfo;
        }

        internal override bool SuppressDynamicAttribute
        {
            get { return true; }
        }

        public override RefKind RefKind => RefKind.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _type;
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get { return true; }
        }

        IMethodSymbolInternal ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return ((ISynthesizedMethodBodyImplementationSymbol)ContainingSymbol).Method; }
        }

        internal override bool IsCapturedFrame
        {
            get { return _isThis; }
        }
    }
}
