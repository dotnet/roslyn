// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis;

public partial class Project
{
    private class EquivalenceResult(bool publiclyEquivalent, bool privatelyEquivalent)
    {
        public readonly bool PubliclyEquivalent = publiclyEquivalent;
        public readonly bool PrivatelyEquivalent = privatelyEquivalent;
    }
}
