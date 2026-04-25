// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.LiveShare.Guest;

[Export(typeof(ILiveShareProjectPathProvider))]
[method: ImportingConstructor]
internal class GuestProjectPathProvider(
    JoinableTaskContext joinableTaskContext,
    ITextDocumentFactoryService textDocumentFactory,
    IProxyAccessor proxyAccessor,
    ILiveShareSessionAccessor liveShareSessionAccessor) : ILiveShareProjectPathProvider
{
    private readonly JoinableTaskFactory _joinableTaskFactory = joinableTaskContext.Factory;
    private readonly ITextDocumentFactoryService _textDocumentFactory = textDocumentFactory;
    private readonly IProxyAccessor _proxyAccessor = proxyAccessor;
    private readonly ILiveShareSessionAccessor _liveShareSessionAccessor = liveShareSessionAccessor;

    public bool TryGetProjectPath(ITextBuffer textBuffer, [NotNullWhen(returnValue: true)] out string? filePath)
    {
        if (!_liveShareSessionAccessor.IsGuestSessionActive)
        {
            filePath = null;
            return false;
        }

        if (!_textDocumentFactory.TryGetTextDocument(textBuffer, out var textDocument))
        {
            filePath = null;
            return false;
        }

        var hostProjectPath = GetHostProjectPath(textDocument);
        if (hostProjectPath is null)
        {
            filePath = null;
            return false;
        }

        // Host always responds with a host-based path, convert back to a guest one.
        filePath = ResolveGuestPath(hostProjectPath);
        return true;
    }

    private Uri? GetHostProjectPath(ITextDocument textDocument)
    {
        Assumes.NotNull(_liveShareSessionAccessor.Session);

        // The path we're given is from the guest so following other patterns we always ask the host information in its own form (aka convert on guest instead of on host).
        var ownerPath = _liveShareSessionAccessor.Session.ConvertLocalPathToSharedUri(textDocument.FilePath);

        var hostProjectPath = _joinableTaskFactory.Run(() =>
        {
            var projectHierarchyProxy = _proxyAccessor.GetProjectHierarchyProxy();

            // We need to block the UI thread to get a proper project path. However, this is only done once on opening the document.
            return projectHierarchyProxy.GetProjectPathAsync(ownerPath, CancellationToken.None);
        });

        return hostProjectPath;
    }

    // We do not want this inlined because the work done in this method requires the VisualStudio.LiveShare assembly.
    // We do not want to load that assembly outside of a LiveShare session.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private string ResolveGuestPath(Uri hostProjectPath)
    {
        Assumes.NotNull(_liveShareSessionAccessor.Session);

        return _liveShareSessionAccessor.Session.ConvertSharedUriToLocalPath(hostProjectPath);
    }
}
