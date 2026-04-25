// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

// Simplified version of Roslyn's SyntaxNodeCache
internal sealed class SyntaxTokenCache
{
    private const int CacheSizeBits = 16;
    private const int CacheSize = 1 << CacheSizeBits;
    private const int CacheMask = CacheSize - 1;
    public static readonly SyntaxTokenCache Instance = new();

    private readonly SyntaxToken[] _cache = new SyntaxToken[CacheSize];

    internal SyntaxTokenCache() { }

    public bool CanBeCached(SyntaxKind kind, params RazorDiagnostic[] diagnostics)
        => diagnostics.Length == 0;

    public SyntaxToken GetCachedToken(SyntaxKind kind, string content)
    {
        var hash = (kind, content).GetHashCode();

        // Allow the upper 16 bits to contribute to the index
        var indexableHash = hash ^ (hash >> 16);

        var idx = indexableHash & CacheMask;
        var token = _cache[idx];

        if (token != null && token.Kind == kind && token.Content == content)
        {
            return token;
        }

        token = new SyntaxToken(kind, content, []);
        _cache[idx] = token;

        return token;
    }
}
