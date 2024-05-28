// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

internal static class TextDocumentExtensions
{
    public static Uri CreateUri(this TextDocument document)
        => document.GetURI();

    public static async Task<ChecksumWrapper> GetChecksumAsync(this TextDocument document, CancellationToken cancellationToken)
        => new ChecksumWrapper(await document.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false));
}
