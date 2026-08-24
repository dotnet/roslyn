// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.Diagnostics;

internal static class UnusedDirectiveCache
{
    private static readonly ConditionalWeakTable<RazorCodeDocument, TextSpan[]> s_cache = new();

    public static void Set(RazorCodeDocument codeDocument, TextSpan[] spans)
    {
#if NET
        s_cache.AddOrUpdate(codeDocument, spans);
#else
        lock (s_cache)
        {
            s_cache.Remove(codeDocument);
            s_cache.Add(codeDocument, spans);
        }
#endif
    }

    public static bool TryGet(RazorCodeDocument codeDocument, out TextSpan[] spans)
    {
        return s_cache.TryGetValue(codeDocument, out spans!);
    }
}
