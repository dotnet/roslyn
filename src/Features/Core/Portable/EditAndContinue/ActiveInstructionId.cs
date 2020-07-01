// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [DataContract]
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal readonly struct ActiveInstructionId : IEquatable<ActiveInstructionId>
    {
        [DataMember(Order = 0)]
        public readonly ActiveMethodId MethodId;

        [DataMember(Order = 1)]
        public readonly int ILOffset;

        public ActiveInstructionId(ActiveMethodId methodId, int ilOffset)
        {
            MethodId = methodId;
            ILOffset = ilOffset;
        }

        public override bool Equals(object? obj)
            => obj is ActiveInstructionId id && Equals(id);

        public bool Equals(ActiveInstructionId other)
            => ILOffset == other.ILOffset &&
               MethodId == other.MethodId;

        public override int GetHashCode()
            => Hash.Combine(MethodId.GetHashCode(), ILOffset);

        public static bool operator ==(ActiveInstructionId left, ActiveInstructionId right) => left.Equals(right);
        public static bool operator !=(ActiveInstructionId left, ActiveInstructionId right) => !left.Equals(right);

        internal string GetDebuggerDisplay()
            => $"{MethodId.GetDebuggerDisplay()} IL_{ILOffset:X4}";
    }
}
