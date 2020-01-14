// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ILDelta
    {
        public readonly ImmutableArray<byte> Value;

        public ILDelta(ImmutableArray<byte> value)
        {
            Value = value;
        }
    }
}
