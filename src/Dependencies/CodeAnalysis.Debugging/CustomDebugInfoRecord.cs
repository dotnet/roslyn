// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal readonly struct CustomDebugInfoRecord
    {
        public readonly CustomDebugInfoKind Kind;
        public readonly byte Version;
        public readonly ImmutableArray<byte> Data;

        public CustomDebugInfoRecord(CustomDebugInfoKind kind, byte version, ImmutableArray<byte> data)
        {
            Kind = kind;
            Version = version;
            Data = data;
        }
    }
}
