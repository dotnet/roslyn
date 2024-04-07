// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.LanguageService;

internal static class ArityUtilities
{
    private const string GenericTypeNameManglingString = "`";
    private static readonly ImmutableArray<string> s_aritySuffixesOneToNine = ["`1", "`2", "`3", "`4", "`5", "`6", "`7", "`8", "`9"];

    public static string GetMetadataAritySuffix(int arity)
    {
        Debug.Assert(arity > 0);
        return (arity <= s_aritySuffixesOneToNine.Length)
            ? s_aritySuffixesOneToNine[arity - 1]
            : string.Concat(GenericTypeNameManglingString, arity.ToString(CultureInfo.InvariantCulture));
    }
}
