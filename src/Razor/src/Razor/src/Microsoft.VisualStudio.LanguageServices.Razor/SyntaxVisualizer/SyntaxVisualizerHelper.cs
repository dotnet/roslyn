// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Protocol.DevTools;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.SyntaxVisualizer;

/// <summary>
/// Provides helper methods for the Syntax Visualizer, that itself has no IVT access to anything much.
/// </summary>
internal static class SyntaxVisualizerHelper
{
    internal static async Task<RazorSyntaxNode?> GetSyntaxRootAsync(IRemoteServiceInvoker remoteServiceInvoker, Uri hostDocumentUri, Solution solution, CancellationToken cancellationToken)
    {
        if (!solution.TryGetRazorDocument(hostDocumentUri, out var razorDocument))
        {
            return null;
        }

        var tree = await remoteServiceInvoker.TryInvokeAsync<IRemoteDevToolsService, SyntaxVisualizerTree?>(
            solution,
            (service, solutionInfo, cancellationToken) => service.GetRazorSyntaxTreeAsync(solutionInfo, razorDocument.Id, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (tree is null)
        {
            return null;
        }

        return new RazorSyntaxNode(tree.Root);
    }

    internal static ValueTask<string?> GetTagHelperDescriptorsAsync(IRemoteServiceInvoker remoteServiceInvoker, Uri hostDocumentUri, TagHelpersKind tagHelpersKind, Solution solution, CancellationToken cancellationToken)
    {
        if (!solution.TryGetRazorDocument(hostDocumentUri, out var razorDocument))
        {
            return new ValueTask<string?>();
        }

        return remoteServiceInvoker.TryInvokeAsync<IRemoteDevToolsService, string>(
            solution,
            (service, solutionInfo, cancellationToken) => service.GetTagHelpersJsonAsync(solutionInfo, razorDocument.Id, tagHelpersKind, cancellationToken),
            cancellationToken);
    }
}
