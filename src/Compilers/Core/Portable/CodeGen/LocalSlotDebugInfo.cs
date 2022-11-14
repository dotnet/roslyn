// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal readonly struct LocalSlotDebugInfo : IEquatable<LocalSlotDebugInfo>
    {
        public readonly SynthesizedLocalKind SynthesizedKind;
        public readonly LocalDebugId Id;

        public LocalSlotDebugInfo(SynthesizedLocalKind synthesizedKind, LocalDebugId id)
        {
            this.SynthesizedKind = synthesizedKind;
            this.Id = id;
        }

        public bool Equals(LocalSlotDebugInfo other)
        {
            return this.SynthesizedKind == other.SynthesizedKind
                && this.Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            return obj is LocalSlotDebugInfo && Equals((LocalSlotDebugInfo)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine((int)SynthesizedKind, Id.GetHashCode());
        }

        public override string ToString()
        {
            return SynthesizedKind.ToString() + " " + Id.ToString();
        }
    }
}
