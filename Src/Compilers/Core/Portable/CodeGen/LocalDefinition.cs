// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
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
        private readonly ILocalSymbol symbolOpt;

        private readonly string nameOpt; // null if it is a temp.

        //data type associated with the local signature slot.
        private readonly Cci.ITypeReference type;

        // specifies whether local slot has a byref constraint and whether
        // the type of the local has the "pinned modifier" (7.1.2).
        // CLI spec Part I, paragraph 8.6.1.3:
        //   The byref constraint states that the content of the corresponding location is a managed pointer. A managed
        //   pointer can point to a local variable, parameter, field of a compound type, or element of an array.
        private readonly LocalSlotConstraints constraints;

        //ordinal position of the slot in the local signature.
        private readonly int slot;

        //Says if the local variable is Dynamic
        private readonly bool isDynamic;

        //True if the variable was not declared in source.
        private readonly CommonSynthesizedLocalKind synthesizedKind;

        /// <see cref="Cci.ILocalDefinition.PdbAttributes"/>.
        private readonly uint pdbAttributes;

        //Gives the synthesized dynamic attributes of the local definition
        private readonly ImmutableArray<TypedConstant> dynamicTransformFlags;

        /// <summary>
        /// Creates a new LocalDefinition.
        /// </summary>
        /// <param name="symbolOpt">Local symbol, used by edit and continue only, null otherwise.</param>
        /// <param name="nameOpt">Name associated with the slot.</param>
        /// <param name="type">Type associated with the slot.</param>
        /// <param name="slot">Slot position in the signature.</param>
        /// <param name="dynamicTransformFlags">Contains the synthesized dynamic attributes of the local</param>
        /// <param name="synthesizedKind">Synthesized local kind.</param>
        /// <param name="pdbAttributes">Value to emit in the attributes field in the PDB.</param>
        /// <param name="constraints">Specifies whether slot type should have pinned modifier and whether slot should have byref constraint.</param>
        /// <param name="isDynamic">Specifies if the type is Dynamic.</param>
        public LocalDefinition(
            ILocalSymbol symbolOpt,
            string nameOpt,
            Cci.ITypeReference type,
            int slot,
            CommonSynthesizedLocalKind synthesizedKind,
            uint pdbAttributes,
            LocalSlotConstraints constraints,
            bool isDynamic,
            ImmutableArray<TypedConstant> dynamicTransformFlags)
        {
            this.symbolOpt = symbolOpt;
            this.nameOpt = nameOpt;
            this.type = type;
            this.slot = slot;
            this.synthesizedKind = synthesizedKind;
            this.pdbAttributes = pdbAttributes;
            this.dynamicTransformFlags = dynamicTransformFlags;
            this.constraints = constraints;
            this.isDynamic = isDynamic;
        }

        internal string GetDebuggerDisplay()
        {
            return string.Format("{0}: {1} ({2})", slot, nameOpt ?? "<unnamed>", type);
        }

        public ILocalSymbol SymbolOpt
        {
            get { return this.symbolOpt; }
        }

        public Location Location
        {
            get
            {
                ISymbol symbol = this.symbolOpt as ISymbol;
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

        public int SlotIndex
        {
            get { return slot; }
        }

        public Cci.IMetadataConstant CompileTimeValue
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public ImmutableArray<Cci.ICustomModifier> CustomModifiers
        {
            get { return ImmutableArray<Cci.ICustomModifier>.Empty; }
        }

        public bool IsConstant
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public bool IsModified
        {
            get { return false; }
        }

        internal LocalSlotConstraints Constraints
        {
            get { return this.constraints; }
        }

        public bool IsPinned
        {
            get { return (this.constraints & LocalSlotConstraints.Pinned) != 0; }
        }

        public bool IsReference
        {
            get { return (this.constraints & LocalSlotConstraints.ByRef) != 0; }
        }

        public bool IsDynamic
        {
            get { return this.isDynamic; }
        }

        public uint PdbAttributes
        {
            get { return this.pdbAttributes; }
        }

        public CommonSynthesizedLocalKind SynthesizedLocalKind
        {
            get { return this.synthesizedKind; }
        }

        public ImmutableArray<TypedConstant> DynamicTransformFlags
        {
            get { return this.dynamicTransformFlags; }
        }

        public Cci.ITypeReference Type
        {
            get { return this.type; }
        }

        public string Name
        {
            get { return nameOpt; }
        }

        public byte[] Signature
        {
            get { return null; }
        }
    }
}