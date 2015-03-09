// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// A local whose type is represented by a metadata signature instead of a type symbol.
    /// </summary>
    /// <remarks>
    /// Used when emitting a new version of a method during EnC for variables that are no longer used.
    /// </remarks>
    internal sealed class SignatureOnlyLocalDefinition : ILocalDefinition
    {
        internal SignatureOnlyLocalDefinition(byte[] signature, int slot)
        {
            Signature = signature;
            SlotIndex = slot;
        }

        public IMetadataConstant CompileTimeValue
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public ImmutableArray<ICustomModifier> CustomModifiers
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
        public uint PdbAttributes => PdbWriter.HiddenLocalAttributesValue;

        public bool IsDynamic => false;

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

        public string Name => null;

        public int SlotIndex { get; }

        public ITypeReference Type
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public byte[] Signature { get; }

        public LocalSlotDebugInfo SlotInfo
            => new LocalSlotDebugInfo(SynthesizedLocalKind.EmitterTemp, LocalDebugId.None);
    }
}
