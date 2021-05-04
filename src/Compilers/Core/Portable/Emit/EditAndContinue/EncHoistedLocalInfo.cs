// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct EncHoistedLocalInfo : IEquatable<EncHoistedLocalInfo>
    {
        public readonly LocalSlotDebugInfo SlotInfo;
        public readonly Cci.ITypeReference? Type;

        public EncHoistedLocalInfo(bool _)
        {
            SlotInfo = new LocalSlotDebugInfo(SynthesizedLocalKind.EmitterTemp, LocalDebugId.None);
            Type = null;
        }

        public EncHoistedLocalInfo(LocalSlotDebugInfo slotInfo, Cci.ITypeReference type)
        {
            SlotInfo = slotInfo;
            Type = type;
        }

        public bool IsUnused
            => Type is null;

        public bool Equals(EncHoistedLocalInfo other)
            => SlotInfo.Equals(other.SlotInfo) &&
               Cci.SymbolEquivalentEqualityComparer.Instance.Equals(Type, other.Type);

        public override bool Equals(object? obj)
            => obj is EncHoistedLocalInfo info && Equals(info);

        public override int GetHashCode()
            => Hash.Combine(Cci.SymbolEquivalentEqualityComparer.Instance.GetHashCode(Type), SlotInfo.GetHashCode());

        private string GetDebuggerDisplay()
        {
            if (IsUnused)
            {
                return "[invalid]";
            }

            return string.Format("[Id={0}, SynthesizedKind={1}, Type={2}]",
                SlotInfo.Id,
                SlotInfo.SynthesizedKind,
                Type);
        }
    }
}
