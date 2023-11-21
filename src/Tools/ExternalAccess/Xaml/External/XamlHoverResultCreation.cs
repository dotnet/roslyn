// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.QuickInfo;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal static class XamlHoverResultCreation
{
    public static Task<LSP.Hover> CreateHoverAsync(TextDocument document, QuickInfoItem info, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        => DefaultLspHoverResultCreationService.CreateDefaultHoverAsync(document, info, clientCapabilities, cancellationToken);
}
