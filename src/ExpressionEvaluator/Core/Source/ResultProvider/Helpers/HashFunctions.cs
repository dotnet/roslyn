// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
    /// <summary>
    /// Required by <see cref="CaseInsensitiveComparison"/>
    /// </summary>
    internal static class Hash
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    {
        internal const int FnvOffsetBias = unchecked((int)2166136261);

        internal const int FnvPrime = 16777619;

        internal static int CombineFNVHash(int hashCode, char ch)
        {
            return unchecked((hashCode ^ ch) * Hash.FnvPrime);
        }
    }
}
