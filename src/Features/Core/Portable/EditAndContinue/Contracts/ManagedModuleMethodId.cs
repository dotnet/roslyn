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
    internal readonly struct ManagedModuleMethodId : IEquatable<ManagedModuleMethodId>
    {
        [DataMember(Order = 0)]
        public readonly int Token;

        [DataMember(Order = 1)]
        public readonly int Version;

        public ManagedModuleMethodId(int token, int version)
        {
            Token = token;
            Version = version;
        }

        public override bool Equals(object? obj)
            => obj is ManagedModuleMethodId id && Equals(id);

        public bool Equals(ManagedModuleMethodId other)
            => Token == other.Token &&
               Version == other.Version;

        public override int GetHashCode()
            => Hash.Combine(Token, Version);

        public static bool operator ==(ManagedModuleMethodId left, ManagedModuleMethodId right) => left.Equals(right);
        public static bool operator !=(ManagedModuleMethodId left, ManagedModuleMethodId right) => !left.Equals(right);

        internal string GetDebuggerDisplay()
            => $"0x{Token:X8} v{Version}";
    }
}
