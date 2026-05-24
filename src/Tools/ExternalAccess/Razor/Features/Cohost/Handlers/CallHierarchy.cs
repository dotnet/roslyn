// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;
using RoslynCallHierarchy = Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class CallHierarchy
{
    public static Task<LSP.CallHierarchyItem[]?> PrepareCallHierarchyAsync(
        Document document,
        LinePosition linePosition,
        CancellationToken cancellationToken)
        => RoslynCallHierarchy.PrepareCallHierarchyHandler.PrepareCallHierarchyAsync(document, linePosition, cancellationToken);

    public static Task<LSP.CallHierarchyIncomingCall[]?> GetIncomingCallsAsync(
        Document document,
        LSP.CallHierarchyItem item,
        CancellationToken cancellationToken)
        => RoslynCallHierarchy.CallHierarchyIncomingCallsHandler.GetIncomingCallsAsync(document, item, allowRazorSourceGeneratedDocuments: true, cancellationToken);

    public static Task<LSP.CallHierarchyOutgoingCall[]?> GetOutgoingCallsAsync(
        Document document,
        LSP.CallHierarchyItem item,
        CancellationToken cancellationToken)
        => RoslynCallHierarchy.CallHierarchyOutgoingCallsHandler.GetOutgoingCallsAsync(document, item, cancellationToken);
}
