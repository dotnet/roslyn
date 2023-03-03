// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class LocalDefinition : Cci.ILocalDefinition
    {
        //TODO: locals are really just typed slots. They do not have names.
        // name only matters for pdb generation where it is a scope-specific mapping to a slot.
        // it may be better if local does not have a name as will restrict reuse of locals when we do it.

        //Local symbol, currently used by edit and continue and for the location.
        private readonly ILocalSymbolInternal? _symbolOpt;

        private readonly string? _nameOpt;

        //data type associated with the local signature slot.
        private readonly Cci.ITypeReference _type;

        // specifies whether local slot has a byref constraint and whether
        // the type of the local has the "pinned modifier" (7.1.2).
        // CLI spec Part I, paragraph 8.6.1.3:
        //   The byref constraint states that the content of the corresponding location is a managed pointer. A managed
        //   pointer can point to a local variable, parameter, field of a compound type, or element of an array.
        private readonly LocalSlotConstraints _constraints;

        //ordinal position of the slot in the local signature.
        private readonly int _slot;

        private readonly LocalSlotDebugInfo _slotInfo;

        /// <see cref="Cci.ILocalDefinition.PdbAttributes"/>.
        private readonly LocalVariableAttributes _pdbAttributes;

        //Gives the synthesized dynamic attributes of the local definition
        private readonly ImmutableArray<bool> _dynamicTransformFlags;

        private readonly ImmutableArray<string> _tupleElementNames;

        /// <summary>
        /// Creates a new LocalDefinition.
        /// </summary>
        /// <param name="symbolOpt">Local symbol, used by edit and continue only, null otherwise.</param>
        /// <param name="nameOpt">Name associated with the slot.</param>
        /// <param name="type">Type associated with the slot.</param>
        /// <param name="slot">Slot position in the signature.</param>
        /// <param name="synthesizedKind">Local kind.</param>
        /// <param name="id">Local id.</param>
        /// <param name="pdbAttributes">Value to emit in the attributes field in the PDB.</param>
        /// <param name="constraints">Specifies whether slot type should have pinned modifier and whether slot should have byref constraint.</param>
        /// <param name="dynamicTransformFlags">The synthesized dynamic attributes of the local.</param>
        /// <param name="tupleElementNames">Tuple element names of the local.</param>
        public LocalDefinition(
            ILocalSymbolInternal? symbolOpt,
            string? nameOpt,
            Cci.ITypeReference type,
            int slot,
            SynthesizedLocalKind synthesizedKind,
            LocalDebugId id,
            LocalVariableAttributes pdbAttributes,
            LocalSlotConstraints constraints,
            ImmutableArray<bool> dynamicTransformFlags,
            ImmutableArray<string> tupleElementNames)
        {
            _symbolOpt = symbolOpt;
            _nameOpt = nameOpt;
            _type = type;
            _slot = slot;
            _slotInfo = new LocalSlotDebugInfo(synthesizedKind, id);
            _pdbAttributes = pdbAttributes;
            _dynamicTransformFlags = dynamicTransformFlags.NullToEmpty();
            _tupleElementNames = tupleElementNames.NullToEmpty();
            _constraints = constraints;
        }

        internal string GetDebuggerDisplay()
            => $"{_slot}: {_nameOpt ?? "<unnamed>"} ({_type})";

        public ILocalSymbolInternal? SymbolOpt => _symbolOpt;

        public Location Location
        {
            get
            {
                if (_symbolOpt != null)
                {
                    ImmutableArray<Location> locations = _symbolOpt.Locations;
                    if (!locations.IsDefaultOrEmpty)
                    {
                        return locations[0];
                    }
                }
                return Location.None;
            }
        }

        public int SlotIndex => _slot;

        public MetadataConstant CompileTimeValue
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public ImmutableArray<Cci.ICustomModifier> CustomModifiers
            => ImmutableArray<Cci.ICustomModifier>.Empty;

        public bool IsConstant
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public bool IsModified => false;

        public LocalSlotConstraints Constraints => _constraints;

        public bool IsPinned
            => (_constraints & LocalSlotConstraints.Pinned) != 0;

        public bool IsReference
            => (_constraints & LocalSlotConstraints.ByRef) != 0;

        public LocalVariableAttributes PdbAttributes => _pdbAttributes;

        public ImmutableArray<bool> DynamicTransformFlags => _dynamicTransformFlags;

        public ImmutableArray<string> TupleElementNames => _tupleElementNames;

        public Cci.ITypeReference Type => _type;

        public string? Name => _nameOpt;

        public byte[]? Signature => null;

        public LocalSlotDebugInfo SlotInfo => _slotInfo;
    }
}
