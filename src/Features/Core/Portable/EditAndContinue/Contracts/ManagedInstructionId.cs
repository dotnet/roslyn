// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    [DataContract]
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal readonly struct ManagedInstructionId : IEquatable<ManagedInstructionId>
    {
        [DataMember(Order = 0)]
        public readonly ManagedMethodId Method;

        [DataMember(Order = 1)]
        public readonly int ILOffset;

        public ManagedInstructionId(ManagedMethodId method, int ilOffset)
        {
            Method = method;
            ILOffset = ilOffset;
        }

        public override bool Equals(object? obj)
            => obj is ManagedInstructionId id && Equals(id);

        public bool Equals(ManagedInstructionId other)
            => ILOffset == other.ILOffset &&
               Method == other.Method;

        public override int GetHashCode()
            => Hash.Combine(Method.GetHashCode(), ILOffset);

        public static bool operator ==(ManagedInstructionId left, ManagedInstructionId right) => left.Equals(right);
        public static bool operator !=(ManagedInstructionId left, ManagedInstructionId right) => !left.Equals(right);

        internal string GetDebuggerDisplay()
            => $"{Method.GetDebuggerDisplay()} IL_{ILOffset:X4}";
    }
}
