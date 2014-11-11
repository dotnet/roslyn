// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// A local whose type is represented by a metadata signature instead of a type symbol.
    /// </summary>
    /// <remarks>
    /// Used when emitting a new version of a method during EnC for variables that are no longer used.
    /// </remarks>
    internal sealed class SignatureOnlyLocalDefinition : Cci.ILocalDefinition
    {
        private readonly byte[] signature;
        private readonly int slot;

        internal SignatureOnlyLocalDefinition(byte[] signature, int slot)
        {
            this.signature = signature;
            this.slot = slot;
        }

        public Cci.IMetadataConstant CompileTimeValue
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public ImmutableArray<Cci.ICustomModifier> CustomModifiers
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public ImmutableArray<TypedConstant> DynamicTransformFlags
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        /// <remarks>
        /// This temp is not interesting to the expression compiler.  However, it 
        /// may be replaced by an interesting local in a later stage.
        /// </remarks>
        public uint PdbAttributes
        {
            get { return Cci.PdbWriter.HiddenLocalAttributesValue; }
        }

        public bool IsDynamic
        {
            get { return false; }
        }

        public bool IsPinned
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public bool IsReference
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public LocalSlotConstraints Constraints
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public Location Location
        {
            get { return Location.None; }
        }

        public string Name
        {
            get { return null; }
        }

        public int SlotIndex
        {
            get { return this.slot; }
        }

        public Cci.ITypeReference Type
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public byte[] Signature
        {
            get { return this.signature; }
        }

        public LocalSlotDebugInfo SlotInfo
        {
            get { return new LocalSlotDebugInfo(SynthesizedLocalKind.EmitterTemp, LocalDebugId.None); }
        }
    }
}
