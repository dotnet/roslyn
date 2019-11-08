// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct EncLocalInfo : IEquatable<EncLocalInfo>
    {
        public readonly LocalSlotDebugInfo SlotInfo;
        public readonly Cci.ITypeReference Type;
        public readonly LocalSlotConstraints Constraints;
        public readonly byte[] Signature;
        public readonly bool isUnused;

        public EncLocalInfo(byte[] signature)
        {
            Debug.Assert(signature != null);
            Debug.Assert(signature.Length > 0);

            this.SlotInfo = new LocalSlotDebugInfo(SynthesizedLocalKind.EmitterTemp, LocalDebugId.None);
            this.Type = null;
            this.Constraints = default(LocalSlotConstraints);
            this.Signature = signature;
            this.isUnused = true;
        }

        public EncLocalInfo(LocalSlotDebugInfo slotInfo, Cci.ITypeReference type, LocalSlotConstraints constraints, byte[] signature)
        {
            Debug.Assert(type != null);

            this.SlotInfo = slotInfo;
            this.Type = type;
            this.Constraints = constraints;
            this.Signature = signature;
            this.isUnused = false;
        }

        public bool IsDefault
        {
            get { return this is { Type: null, Signature: null }; }
        }

        public bool IsUnused
        {
            get { return isUnused; }
        }

        public bool Equals(EncLocalInfo other)
        {
            Debug.Assert(this.Type != null);
            Debug.Assert(other.Type != null);

            return this.SlotInfo.Equals(other.SlotInfo) &&
                   this.Type.Equals(other.Type) &&
                   this.Constraints == other.Constraints &&
                   this.isUnused == other.isUnused;
        }

        public override bool Equals(object obj)
        {
            return obj is EncLocalInfo && Equals((EncLocalInfo)obj);
        }

        public override int GetHashCode()
        {
            Debug.Assert(this.Type != null);

            return Hash.Combine(this.SlotInfo.GetHashCode(),
                   Hash.Combine(this.Type.GetHashCode(),
                   Hash.Combine((int)this.Constraints,
                   Hash.Combine(isUnused, 0))));
        }

        private string GetDebuggerDisplay()
        {
            if (this.IsDefault)
            {
                return "[default]";
            }

            if (this.isUnused)
            {
                return "[invalid]";
            }

            return string.Format("[Id={0}, SynthesizedKind={1}, Type={2}, Constraints={3}, Sig={4}]",
                this.SlotInfo.Id,
                this.SlotInfo.SynthesizedKind,
                this.Type,
                this.Constraints,
                (this.Signature != null) ? BitConverter.ToString(this.Signature) : "null");
        }
    }
}
