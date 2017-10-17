// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SyntaxTokenListExtensions
    {
        /// <summary>
        /// Gets the concatenated value text for the token list.
        /// </summary>
        /// <returns>The concatenated value text, or an empty string if there are no tokens in the list.</returns>
        internal static string GetValueText(this SyntaxTokenList tokens)
        {
            switch (tokens.Count)
            {
                case 0:
                    return string.Empty;

                case 1:
                    return tokens[0].ValueText;

                default:
                    var pooledBuilder = PooledStringBuilder.GetInstance();
                    foreach (var token in tokens)
                    {
                        pooledBuilder.Builder.Append(token.ValueText);
                    }
                    return pooledBuilder.ToStringAndFree();
            }
        }
    }
}
