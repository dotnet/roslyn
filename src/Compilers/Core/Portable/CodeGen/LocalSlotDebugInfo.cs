// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal struct LocalSlotDebugInfo : IEquatable<LocalSlotDebugInfo>
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
