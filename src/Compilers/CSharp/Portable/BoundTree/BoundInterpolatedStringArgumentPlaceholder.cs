// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundInterpolatedStringArgumentPlaceholder
    {
        public const int InstanceParameter = -1;
        public const int TrailingConstructorValidityParameter = -2;
        public const int UnspecifiedParameter = -3;

        public sealed override bool IsEquivalentToThisReference => throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
    }
}
