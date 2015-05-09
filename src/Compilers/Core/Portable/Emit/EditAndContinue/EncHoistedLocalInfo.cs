// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct EncHoistedLocalInfo : IEquatable<EncHoistedLocalInfo>
    {
        public readonly LocalSlotDebugInfo SlotInfo;
        public readonly Cci.ITypeReference Type;

        public EncHoistedLocalInfo(bool ignored)
        {
            SlotInfo = new LocalSlotDebugInfo(SynthesizedLocalKind.EmitterTemp, LocalDebugId.None);
            Type = null;
        }

        public EncHoistedLocalInfo(LocalSlotDebugInfo slotInfo, Cci.ITypeReference type)
        {
            Debug.Assert(type != null);
            this.SlotInfo = slotInfo;
            this.Type = type;
        }

        public bool IsUnused
        {
            get { return this.Type == null; }
        }

        public bool Equals(EncHoistedLocalInfo other)
        {
            Debug.Assert(this.Type != null);
            Debug.Assert(other.Type != null);

            return this.SlotInfo.Equals(other.SlotInfo) &&
                   this.Type.Equals(other.Type);
        }

        public override bool Equals(object obj)
        {
            return obj is EncHoistedLocalInfo && Equals((EncHoistedLocalInfo)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Type, this.SlotInfo.GetHashCode());
        }

        private string GetDebuggerDisplay()
        {
            if (this.IsUnused)
            {
                return "[invalid]";
            }

            return string.Format("[Id={0}, SynthesizedKind={1}, Type={2}]",
                this.SlotInfo.Id,
                this.SlotInfo.SynthesizedKind,
                this.Type);
        }
    }
}
