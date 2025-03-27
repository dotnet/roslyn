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
    internal readonly struct EncLocalInfo : IEquatable<EncLocalInfo>
    {
        public readonly LocalSlotDebugInfo SlotInfo;
        public readonly Cci.ITypeReference? Type;
        public readonly LocalSlotConstraints Constraints;
        public readonly byte[]? Signature;
        public readonly bool IsUnused;

        public EncLocalInfo(byte[] signature)
        {
            Debug.Assert(signature.Length > 0);

            SlotInfo = new LocalSlotDebugInfo(SynthesizedLocalKind.EmitterTemp, LocalDebugId.None);
            Type = null;
            Constraints = LocalSlotConstraints.None;
            Signature = signature;
            IsUnused = true;
        }

        public EncLocalInfo(LocalSlotDebugInfo slotInfo, Cci.ITypeReference type, LocalSlotConstraints constraints, byte[]? signature)
        {
            SlotInfo = slotInfo;
            Type = type;
            Constraints = constraints;
            Signature = signature;
            IsUnused = false;
        }

        public bool IsDefault
            => Type is null && Signature is null;

        public bool Equals(EncLocalInfo other)
        {
            return SlotInfo.Equals(other.SlotInfo) &&
                   Cci.SymbolEquivalentEqualityComparer.Instance.Equals(Type, other.Type) &&
                   Constraints == other.Constraints &&
                   IsUnused == other.IsUnused;
        }

        public override bool Equals(object? obj)
            => obj is EncLocalInfo info && Equals(info);

        public override int GetHashCode()
        {
            Debug.Assert(Type != null);

            return Hash.Combine(SlotInfo.GetHashCode(),
                   Hash.Combine(Cci.SymbolEquivalentEqualityComparer.Instance.GetHashCode(Type),
                   Hash.Combine((int)Constraints,
                   Hash.Combine(IsUnused, 0))));
        }

        private string GetDebuggerDisplay()
        {
            if (IsDefault)
            {
                return "[default]";
            }

            if (IsUnused)
            {
                return "[invalid]";
            }

            return string.Format("[Id={0}, SynthesizedKind={1}, Type={2}, Constraints={3}, Sig={4}]",
                SlotInfo.Id,
                SlotInfo.SynthesizedKind,
                Type,
                Constraints,
                (Signature != null) ? BitConverter.ToString(Signature) : "null");
        }
    }
}
