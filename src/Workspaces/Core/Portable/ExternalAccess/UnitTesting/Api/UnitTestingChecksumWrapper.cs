// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal readonly struct UnitTestingChecksumWrapper(Checksum underlyingObject)
{
    private Checksum UnderlyingObject { get; } = underlyingObject;

    public bool IsEqualTo(UnitTestingChecksumWrapper other)
        => other.UnderlyingObject == UnderlyingObject;
}
