// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
