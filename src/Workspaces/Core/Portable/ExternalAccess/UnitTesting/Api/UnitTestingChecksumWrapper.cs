// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
