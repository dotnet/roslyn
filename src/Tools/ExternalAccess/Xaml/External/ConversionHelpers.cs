// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.QuickInfo;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal static class ConversionHelpers
{
    public static Uri CreateAbsoluteUri(string absolutePath)
        => ProtocolConversions.CreateAbsoluteUri(absolutePath);

    /// <summary>
    /// Helper to create <see cref="LSP.Hover"/> from <see cref="QuickInfoItem"/>.
    /// </summary>
    public static Task<LSP.Hover> CreateHoverResultAsync(TextDocument document, QuickInfoItem info, XamlRequestContext context, CancellationToken cancellationToken)
        => DefaultLspHoverResultCreationService.CreateDefaultHoverAsync(document, info, context.ClientCapabilities, cancellationToken);
}
