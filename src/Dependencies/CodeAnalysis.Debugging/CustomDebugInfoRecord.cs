// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal struct CustomDebugInfoRecord
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
