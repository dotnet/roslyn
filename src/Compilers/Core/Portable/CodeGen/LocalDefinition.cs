// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class LocalDefinition : ILocalDefinition
    {
        //TODO: locals are really just typed slots. They do not have names.
        // name only matters for pdb generation where it is a scope-specific mapping to a slot.
        // it may be better if local does not have a name as will restrict reuse of locals when we do it.

        //Local symbol, currently used by edit and continue and for the location.
        private readonly ILocalSymbol _symbolOpt;

        private readonly string _nameOpt;

        //data type associated with the local signature slot.
        private readonly ITypeReference _type;

        // specifies whether local slot has a byref constraint and whether
        // the type of the local has the "pinned modifier" (7.1.2).
        // CLI spec Part I, paragraph 8.6.1.3:
        //   The byref constraint states that the content of the corresponding location is a managed pointer. A managed
        //   pointer can point to a local variable, parameter, field of a compound type, or element of an array.
        private readonly LocalSlotConstraints _constraints;

        //ordinal position of the slot in the local signature.
        private readonly int _slot;

        /// <summary>
        /// Creates a new LocalDefinition.
        /// </summary>
        /// <param name="symbolOpt">Local symbol, used by edit and continue only, null otherwise.</param>
        /// <param name="nameOpt">Name associated with the slot.</param>
        /// <param name="type">Type associated with the slot.</param>
        /// <param name="slot">Slot position in the signature.</param>
        /// <param name="dynamicTransformFlags">Contains the synthesized dynamic attributes of the local</param>
        /// <param name="synthesizedKind">Local kind.</param>
        /// <param name="id">Local id.</param>
        /// <param name="pdbAttributes">Value to emit in the attributes field in the PDB.</param>
        /// <param name="constraints">Specifies whether slot type should have pinned modifier and whether slot should have byref constraint.</param>
        /// <param name="isDynamic">Specifies if the type is Dynamic.</param>
        public LocalDefinition(
            ILocalSymbol symbolOpt,
            string nameOpt,
            ITypeReference type,
            int slot,
            SynthesizedLocalKind synthesizedKind,
            LocalDebugId id,
            uint pdbAttributes,
            LocalSlotConstraints constraints,
            bool isDynamic,
            ImmutableArray<TypedConstant> dynamicTransformFlags)
        {
            _symbolOpt = symbolOpt;
            _nameOpt = nameOpt;
            _type = type;
            _slot = slot;
            SlotInfo = new LocalSlotDebugInfo(synthesizedKind, id);
            PdbAttributes = pdbAttributes;
            DynamicTransformFlags = dynamicTransformFlags;
            _constraints = constraints;
            IsDynamic = isDynamic;
        }

        internal string GetDebuggerDisplay()
            => $"{_slot}: {_nameOpt ?? "<unnamed>"} ({_type})";

        public ILocalSymbol SymbolOpt => _symbolOpt;

        public Location Location
        {
            get
            {
                ISymbol symbol = _symbolOpt;
                if (symbol != null)
                {
                    ImmutableArray<Location> locations = symbol.Locations;
                    if (!locations.IsDefaultOrEmpty)
                    {
                        return locations[0];
                    }
                }
                return Location.None;
            }
        }

        public int SlotIndex => _slot;

        public IMetadataConstant CompileTimeValue
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public ImmutableArray<ICustomModifier> CustomModifiers
            => ImmutableArray<ICustomModifier>.Empty;

        public bool IsConstant
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public bool IsModified => false;

        public LocalSlotConstraints Constraints => _constraints;

        public bool IsPinned
            => (_constraints & LocalSlotConstraints.Pinned) != 0;

        public bool IsReference
            => (_constraints & LocalSlotConstraints.ByRef) != 0;

        //Says if the local variable is Dynamic
        public bool IsDynamic { get; }

        /// <see cref="ILocalDefinition.PdbAttributes"/>.
        public uint PdbAttributes { get; }

        //Gives the synthesized dynamic attributes of the local definition
        public ImmutableArray<TypedConstant> DynamicTransformFlags { get; }

        public ITypeReference Type => _type;

        public string Name => _nameOpt;

        public byte[] Signature => null;

        public LocalSlotDebugInfo SlotInfo { get; }
    }
}
