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
    internal readonly struct ActiveMethodId : IEquatable<ActiveMethodId>
    {
        [DataMember(Order = 0)]
        public readonly Guid ModuleId;

        [DataMember(Order = 1)]
        public readonly int Token;

        [DataMember(Order = 2)]
        public readonly int Version;

        public ActiveMethodId(Guid moduleId, int token, int version)
        {
            ModuleId = moduleId;
            Token = token;
            Version = version;
        }

        public override bool Equals(object? obj)
            => obj is ActiveMethodId id && Equals(id);

        public bool Equals(ActiveMethodId other)
            => Token == other.Token &&
               Version == other.Version &&
               ModuleId.Equals(other.ModuleId);

        public override int GetHashCode()
            => Hash.Combine(ModuleId.GetHashCode(), Hash.Combine(Token, Version));

        public static bool operator ==(ActiveMethodId left, ActiveMethodId right) => left.Equals(right);
        public static bool operator !=(ActiveMethodId left, ActiveMethodId right) => !left.Equals(right);

        internal string GetDebuggerDisplay()
            => $"mvid={ModuleId} 0x{Token:X8} v{Version}";
    }
}
