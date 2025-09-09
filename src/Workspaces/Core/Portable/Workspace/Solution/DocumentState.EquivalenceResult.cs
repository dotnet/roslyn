// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal partial class DocumentState
{
    internal sealed class EquivalenceResult(bool topLevelEquivalent, bool interiorEquivalent)
    {
        public readonly bool TopLevelEquivalent = topLevelEquivalent;
        public readonly bool InteriorEquivalent = interiorEquivalent;
    }
}
