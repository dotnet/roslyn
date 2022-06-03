﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.EditAndContinue.Contracts
{
    /// <summary>
    /// Active instruction identifier.
    /// It has the information necessary to track an active instruction within the debug session.
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal readonly struct ManagedInstructionId : IEquatable<ManagedInstructionId>
    {
        /// <summary>
        /// Creates an ActiveInstructionId.
        /// </summary>
        /// <param name="method">Method which the instruction is scoped to.</param>
        /// <param name="ilOffset">IL offset for the instruction.</param>
        public ManagedInstructionId(
            ManagedMethodId method,
            int ilOffset)
        {
            Method = method;
            ILOffset = ilOffset;
        }

        /// <summary>
        /// Method which the instruction is scoped to.
        /// </summary>
        [DataMember(Name = "method")]
        public ManagedMethodId Method { get; }

        /// <summary>
        /// The IL offset for the instruction.
        /// </summary>
        [DataMember(Name = "ilOffset")]
        public int ILOffset { get; }

        public bool Equals(ManagedInstructionId other)
        {
            return Method.Equals(other.Method) && ILOffset == other.ILOffset;
        }

        public override bool Equals(object? obj) => obj is ManagedInstructionId instr && Equals(instr);

        public override int GetHashCode()
        {
            return Method.GetHashCode() ^ ILOffset;
        }

        public static bool operator ==(ManagedInstructionId left, ManagedInstructionId right) => left.Equals(right);

        public static bool operator !=(ManagedInstructionId left, ManagedInstructionId right) => !(left == right);

        internal string GetDebuggerDisplay() => $"{Method.GetDebuggerDisplay()} IL_{ILOffset:X4}";
    }
}
