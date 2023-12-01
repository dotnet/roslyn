// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Caches text document identifiers in between calls to Handler and
/// ResolveHandler. Used to minimize passing around request resolve data.
/// </summary>
internal sealed class DocumentCache : ResolveCache<TextDocumentIdentifier>
{
    public DocumentCache() : base(maxCacheSize: 3)
    {
    }
}
