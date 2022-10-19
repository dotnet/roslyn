// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Threading;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [DataContract]
    internal readonly struct DebuggingSessionId : IEquatable<DebuggingSessionId>
    {
        [DataMember(Order = 0)]
        private readonly int _id;

        public DebuggingSessionId(int id)
            => _id = id;

        public override bool Equals(object? obj)
            => obj is DebuggingSessionId id && Equals(id);

        public bool Equals(DebuggingSessionId other)
            => _id == other._id;

        public override int GetHashCode()
            => _id;

        public static bool operator ==(DebuggingSessionId left, DebuggingSessionId right)
            => left.Equals(right);

        public static bool operator !=(DebuggingSessionId left, DebuggingSessionId right)
            => !(left == right);

        public override string ToString()
            => _id.ToString();
    }
}
