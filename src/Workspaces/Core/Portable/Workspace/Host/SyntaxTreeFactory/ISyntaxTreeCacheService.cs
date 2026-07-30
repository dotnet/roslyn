// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host;

internal readonly record struct SyntaxTreeCacheKey(string Language, Checksum TextChecksum, ParseOptions Options);

internal interface ISyntaxTreeCacheService : IWorkspaceService
{
    SyntaxTreeCacheKey CreateKey(string language, SourceText text, ParseOptions options);

    bool TryGetRoot(SyntaxTreeCacheKey key, out SyntaxNode? root);

    SyntaxNode GetOrAddRoot(SyntaxTreeCacheKey key, SyntaxNode root);

    void RefreshRoot(SyntaxTreeCacheKey key, SyntaxNode root);
}
