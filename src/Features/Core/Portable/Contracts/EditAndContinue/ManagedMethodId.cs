// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue
{
    /// <summary>
    /// ManagedMethodId is a module/method pair which is used to uniquely identify the
    /// symbol store's understanding of a particular CLR method.
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal readonly struct ManagedMethodId : IEquatable<ManagedMethodId>
    {
        /// <summary>
        /// Creates a ManagedMethodId.
        /// </summary>
        /// <param name="module">Module version ID in which the method exists.</param>
        /// <param name="method">Method ID.</param>
        public ManagedMethodId(
            Guid module,
            ManagedModuleMethodId method)
        {
            Module = module;
            Method = method;
        }

        public ManagedMethodId(Guid module, int token, int version)
            : this(module, new(token, version))
        {
        }

        /// <summary>
        /// The module version ID in which the method exists.
        /// </summary>
        [DataMember(Name = "module")]
        public Guid Module { get; }

        /// <summary>
        /// The unique identifier for the method within <see cref="Module"/>.
        /// </summary>
        [DataMember(Name = "method")]
        public ManagedModuleMethodId Method { get; }

        public int Token => Method.Token;

        public int Version => Method.Version;

        public bool Equals(ManagedMethodId other)
        {
            return Module == other.Module && Method.Equals(other.Method);
        }

        public override bool Equals(object? obj) => obj is ManagedMethodId method && Equals(method);

        public override int GetHashCode()
        {
            return Module.GetHashCode() ^ Method.GetHashCode();
        }

        public static bool operator ==(ManagedMethodId left, ManagedMethodId right) => left.Equals(right);

        public static bool operator !=(ManagedMethodId left, ManagedMethodId right) => !(left == right);

        internal string GetDebuggerDisplay() => $"mvid={Module} {Method.GetDebuggerDisplay()}";
    }
}
