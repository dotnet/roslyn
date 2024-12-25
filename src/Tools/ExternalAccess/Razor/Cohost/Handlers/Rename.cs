// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class Rename
{
    public static Task<Range?> GetRenameRangeAsync(Document document, LinePosition linePosition, CancellationToken cancellationToken)
        => PrepareRenameHandler.GetRenameRangeAsync(document, linePosition, cancellationToken);

    public static Task<WorkspaceEdit?> GetRenameEditAsync(Document document, LinePosition linePosition, string newName, CancellationToken cancellationToken)
        => RenameHandler.GetRenameEditAsync(document, linePosition, newName, cancellationToken);
}
