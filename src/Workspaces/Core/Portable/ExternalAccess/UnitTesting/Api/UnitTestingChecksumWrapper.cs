// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingChecksumWrapper
    {
        private Checksum UnderlyingObject { get; }

        public UnitTestingChecksumWrapper(Checksum underlyingObject)
            => UnderlyingObject = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));

        public bool IsEqualTo(UnitTestingChecksumWrapper other)
            => other.UnderlyingObject == UnderlyingObject;
    }
}
