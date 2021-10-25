// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class CompletionUtilities
    {
        public static bool IsTypeImplicitlyConvertible(Compilation compilation, ITypeSymbol sourceType, ImmutableArray<ITypeSymbol> targetTypes)
        {
            foreach (var targetType in targetTypes)
            {
                if (compilation.ClassifyCommonConversion(sourceType, targetType).IsImplicit)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
