// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
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
        private readonly byte[] _signature;
        private readonly int _slot;

        internal SignatureOnlyLocalDefinition(byte[] signature, int slot)
        {
            _signature = signature;
            _slot = slot;
        }

        public MetadataConstant CompileTimeValue
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public ImmutableArray<Cci.ICustomModifier> CustomModifiers
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public ImmutableArray<bool> DynamicTransformFlags
        {
            get { return ImmutableArray<bool>.Empty; }
        }

        public ImmutableArray<string> TupleElementNames
        {
            get { return ImmutableArray<string>.Empty; }
        }

        /// <remarks>
        /// This temp is not interesting to the expression compiler.  However, it 
        /// may be replaced by an interesting local in a later stage.
        /// </remarks>
        public LocalVariableAttributes PdbAttributes => LocalVariableAttributes.DebuggerHidden;

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

        public Location Location => Location.None;

        public string? Name => null;

        public int SlotIndex => _slot;

        public Cci.ITypeReference Type
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public byte[] Signature => _signature;

        public LocalSlotDebugInfo SlotInfo
            => new LocalSlotDebugInfo(SynthesizedLocalKind.EmitterTemp, LocalDebugId.None);
    }
}
