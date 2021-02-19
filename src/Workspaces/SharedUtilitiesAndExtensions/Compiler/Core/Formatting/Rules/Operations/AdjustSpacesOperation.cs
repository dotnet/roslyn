// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// indicate how many spaces are needed between two spaces
    /// </summary>
    internal readonly struct AdjustSpacesOperation
    {
        public static readonly AdjustSpacesOperation None = default;

        public readonly int Space;
        public readonly AdjustSpacesOption Option;

        public AdjustSpacesOperation(int space, AdjustSpacesOption option)
        {
            Contract.ThrowIfFalse(space >= 0);
            Space = space;
            Option = option;
        }
    }
}
