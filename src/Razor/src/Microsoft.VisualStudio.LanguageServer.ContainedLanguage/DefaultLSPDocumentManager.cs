// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

[Shared]
[Export(typeof(LSPDocumentManager))]
internal class DefaultLSPDocumentManager : TrackingLSPDocumentManager
{
    /// <summary>
    /// Represents the key used to store the old content type of a document that's being removed. This is needed as part of the
    /// logic to determine which listeners to notify when a document is removed since the buffer may have already had it's
    /// content type changed to something else.
    /// </summary>
    internal static object LSPDocumentRemovalOldContentTypeKey = new();

    private readonly JoinableTaskContext _joinableTaskContext;
    private readonly FileUriProvider _fileUriProvider;
    private readonly LSPDocumentFactory _documentFactory;
    private readonly ConcurrentDictionary<Uri, LSPDocument> _documents;
    private readonly IEnumerable<Lazy<LSPDocumentChangeListener, IContentTypeMetadata>> _documentManagerChangeListeners;

    [ImportingConstructor]
    public DefaultLSPDocumentManager(
        JoinableTaskContext joinableTaskContext,
        FileUriProvider fileUriProvider,
        LSPDocumentFactory documentFactory,
        [ImportMany] IEnumerable<Lazy<LSPDocumentChangeListener, IContentTypeMetadata>> documentManagerChangeListeners)
    {
        if (joinableTaskContext is null)
        {
            throw new ArgumentNullException(nameof(joinableTaskContext));
        }

        if (fileUriProvider is null)
        {
            throw new ArgumentNullException(nameof(fileUriProvider));
        }

        if (documentFactory is null)
        {
            throw new ArgumentNullException(nameof(documentFactory));
        }

        if (documentManagerChangeListeners is null)
        {
            throw new ArgumentNullException(nameof(documentManagerChangeListeners));
        }

        _joinableTaskContext = joinableTaskContext;
        _fileUriProvider = fileUriProvider;
        _documentFactory = documentFactory;
        _documentManagerChangeListeners = documentManagerChangeListeners;
        _documents = new ConcurrentDictionary<Uri, LSPDocument>();
    }

    public override void RefreshVirtualDocuments()
    {
        var documents = _documents.Values.ToArray();

        foreach (var document in documents)
        {
            var oldSnapshot = document.CurrentSnapshot;
            if (_documentFactory.TryRefreshVirtualDocuments(document))
            {
                var newSnapshot = document.CurrentSnapshot;
                NotifyDocumentManagerChangeListeners(old: oldSnapshot, @new: null, virtualOld: null, virtualNew: null, LSPDocumentChangeKind.Removed);
                NotifyDocumentManagerChangeListeners(old: null, @new: newSnapshot, virtualOld: null, virtualNew: null, LSPDocumentChangeKind.Added);
            }
        }
    }

    public override void TrackDocument(ITextBuffer buffer)
    {
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        Debug.Assert(_joinableTaskContext.IsOnMainThread);

        var uri = _fileUriProvider.GetOrCreate(buffer);
        if (_documents.TryGetValue(uri, out _))
        {
            throw new InvalidOperationException($"Can not track document that's already being tracked {uri}");
        }

        var lspDocument = _documentFactory.Create(buffer);
        _documents[uri] = lspDocument;

        NotifyDocumentManagerChangeListeners(
            old: null,
            @new: lspDocument.CurrentSnapshot,
            virtualOld: null,
            virtualNew: null,
            LSPDocumentChangeKind.Added);
    }

    public override void UntrackDocument(ITextBuffer buffer)
    {
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        Debug.Assert(_joinableTaskContext.IsOnMainThread);

        var uri = _fileUriProvider.GetOrCreate(buffer);
        if (!_documents.TryGetValue(uri, out var lspDocument))
        {
            // We don't know about this document, noop.
            return;
        }

        // Given we're no longer tracking the document we don't want to pin the Uri to the current state of the buffer (could have been renamed to another Uri).
        _fileUriProvider.Remove(buffer);

        if (_documents.TryRemove(uri, out _))
        {
            NotifyDocumentManagerChangeListeners(
                lspDocument.CurrentSnapshot,
                @new: null,
                virtualOld: null,
                virtualNew: null,
                LSPDocumentChangeKind.Removed);
        }
        else
        {
            Debug.Fail($"Couldn't remove {uri.AbsolutePath}. This should never ever happen.");
        }

        lspDocument.Dispose();
    }

    public override void UpdateVirtualDocument<TVirtualDocument>(
        Uri hostDocumentUri,
        IReadOnlyList<ITextChange> changes,
        int hostDocumentVersion,
        object? state)
    {
        if (hostDocumentUri is null)
        {
            throw new ArgumentNullException(nameof(hostDocumentUri));
        }

        if (changes is null)
        {
            throw new ArgumentNullException(nameof(changes));
        }

        Debug.Assert(_joinableTaskContext.IsOnMainThread);

        if (!_documents.TryGetValue(hostDocumentUri, out var lspDocument))
        {
            // Don't know about document, noop.
            return;
        }

        if (!lspDocument.TryGetVirtualDocument<TVirtualDocument>(out var virtualDocument))
        {
            // Unable to locate virtual document of typeof(TVirtualDocument)
            // Ex. Microsoft.WebTools.Languages.LanguageServer.Delegation.ContainedLanguage.Css.CssVirtualDocument
            return;
        }

        if (changes.Count == 0 &&
            virtualDocument.HostDocumentVersion == hostDocumentVersion)
        {
            // The current virtual document already knows about this update.
            // Ignore it so we don't prematurely invoke a change event.
            return;
        }

        var old = lspDocument.CurrentSnapshot;
        var oldVirtual = virtualDocument.CurrentSnapshot;
        var @new = lspDocument.UpdateVirtualDocument<TVirtualDocument>(changes, hostDocumentVersion, state);

        if (old == @new)
        {
            return;
        }

        if (!lspDocument.TryGetVirtualDocument<TVirtualDocument>(out var newVirtualDocument))
        {
            throw new InvalidOperationException("This should never ever happen.");
        }

        var newVirtual = newVirtualDocument.CurrentSnapshot;
        NotifyDocumentManagerChangeListeners(old, @new, oldVirtual, newVirtual, LSPDocumentChangeKind.VirtualDocumentChanged);
    }

    public override void UpdateVirtualDocument<TVirtualDocument>(
        Uri hostDocumentUri,
        Uri virtualDocumentUri,
        IReadOnlyList<ITextChange> changes,
        int hostDocumentVersion,
        object? state)
    {
        if (hostDocumentUri is null)
        {
            throw new ArgumentNullException(nameof(hostDocumentUri));
        }

        if (changes is null)
        {
            throw new ArgumentNullException(nameof(changes));
        }

        Debug.Assert(_joinableTaskContext.IsOnMainThread);

        if (!_documents.TryGetValue(hostDocumentUri, out var lspDocument))
        {
            // Don't know about document, noop.
            return;
        }

        if (!lspDocument.TryGetVirtualDocument<TVirtualDocument>(virtualDocumentUri, out var virtualDocument))
        {
            // Unable to locate virtual document of typeof(TVirtualDocument)
            // Ex. Microsoft.WebTools.Languages.LanguageServer.Delegation.ContainedLanguage.Css.CssVirtualDocument
            return;
        }

        if (changes.Count == 0 &&
            virtualDocument.HostDocumentVersion == hostDocumentVersion)
        {
            // The current virtual document already knows about this update.
            // Ignore it so we don't prematurely invoke a change event.
            return;
        }

        var old = lspDocument.CurrentSnapshot;
        var oldVirtual = virtualDocument.CurrentSnapshot;
        var @new = lspDocument.UpdateVirtualDocument<TVirtualDocument>(virtualDocument, changes, hostDocumentVersion, state);

        if (old == @new)
        {
            return;
        }

        if (!lspDocument.TryGetVirtualDocument<TVirtualDocument>(virtualDocumentUri, out var newVirtualDocument))
        {
            throw new InvalidOperationException("This should never ever happen.");
        }

        var newVirtual = newVirtualDocument.CurrentSnapshot;
        NotifyDocumentManagerChangeListeners(old, @new, oldVirtual, newVirtual, LSPDocumentChangeKind.VirtualDocumentChanged);
    }

    public override bool TryGetDocument(Uri uri, [NotNullWhen(returnValue: true)] out LSPDocumentSnapshot? lspDocumentSnapshot)
    {
        if (!_documents.TryGetValue(uri, out var lspDocument))
        {
            // This should never happen in practice but return `null` so our tests can validate
            lspDocumentSnapshot = null;
            return false;
        }

        lspDocumentSnapshot = lspDocument.CurrentSnapshot;
        return true;
    }

    private void NotifyDocumentManagerChangeListeners(
        LSPDocumentSnapshot? old,
        LSPDocumentSnapshot? @new,
        VirtualDocumentSnapshot? virtualOld,
        VirtualDocumentSnapshot? virtualNew,
        LSPDocumentChangeKind kind)
    {
        IContentType? oldContentType = null;
        if (old is not null)
        {
            oldContentType = old.Snapshot.ContentType;

            // During removal, prefer to use the content type from the buffer properties if it exists since the buffer may have already
            // had it's content type changed at this point but we still want to notify the correct listeners based on the old content type.
            if (kind == LSPDocumentChangeKind.Removed
                && old.Snapshot.TextBuffer.Properties.TryGetProperty<IContentType>(LSPDocumentRemovalOldContentTypeKey, out var contentType))
            {
                oldContentType = contentType;
            }
        }

        foreach (var listener in _documentManagerChangeListeners)
        {
            var notifyListener = false;

            if (oldContentType != null &&
                listener.Metadata.ContentTypes.Any(oldContentType.IsOfType))
            {
                notifyListener = true;
            }
            else if (@new is not null &&
                listener.Metadata.ContentTypes.Any(@new.Snapshot.ContentType.IsOfType))
            {
                notifyListener = true;
            }

            if (notifyListener)
            {
                listener.Value.Changed(old, @new, virtualOld, virtualNew, kind);
            }
        }
    }
}
