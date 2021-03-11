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
    internal readonly struct ManagedMethodId : IEquatable<ManagedMethodId>
    {
        [DataMember(Order = 0)]
        public readonly Guid Module;

        [DataMember(Order = 1)]
        public readonly ManagedModuleMethodId Method;

        public ManagedMethodId(Guid module, int token, int version)
            : this(module, new ManagedModuleMethodId(token, version))
        {
        }

        public ManagedMethodId(Guid module, ManagedModuleMethodId method)
        {
            Module = module;
            Method = method;
        }

        public int Token => Method.Token;
        public int Version => Method.Version;

        public override bool Equals(object? obj)
            => obj is ManagedMethodId id && Equals(id);

        public bool Equals(ManagedMethodId other)
            => Module == other.Module &&
               Method == other.Method;

        public override int GetHashCode()
            => Hash.Combine(Module.GetHashCode(), Method.GetHashCode());

        public static bool operator ==(ManagedMethodId left, ManagedMethodId right) => left.Equals(right);
        public static bool operator !=(ManagedMethodId left, ManagedMethodId right) => !left.Equals(right);

        internal string GetDebuggerDisplay()
            => $"mvid={Module} {Method.GetDebuggerDisplay()}";
    }
}
