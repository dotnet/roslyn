// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingImmutableArrayExtensions
    {
        public static ImmutableArray<byte> UnitTesting_ToImmutable(this MemoryStream stream)
            => stream.ToImmutable();
    }
}
