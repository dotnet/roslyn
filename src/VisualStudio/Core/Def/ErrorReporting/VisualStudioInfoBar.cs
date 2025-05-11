// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ErrorReporting;

internal sealed class VisualStudioInfoBar(
    IThreadingContext threadingContext,
    IVsService<IVsInfoBarUIFactory> vsInfoBarUIFactory,
    IVsService<IVsShell> vsShell,
    IAsynchronousOperationListenerProvider listenerProvider,
    IVsWindowFrame? windowFrame)
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IVsService<IVsInfoBarUIFactory> _vsInfoBarUIFactory = vsInfoBarUIFactory;
    private readonly IVsService<IVsShell> _vsShell = vsShell;
    private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.InfoBar);
    private readonly IVsWindowFrame? _windowFrame = windowFrame;

    /// <summary>
    /// Keep track of the messages that are currently being shown to the user.  If we would show the same message again,
    /// block that from happening so we don't spam the user with the same message.  When the info bar item is dismissed
    /// though, we then may show the same message in the future.  This is important for user clarity as it's possible
    /// for a feature to fail for some reason, then work fine for a while, then fail again.  We want the second failure
    /// message to be reported to ensure the user is not confused.
    /// 
    /// Accessed on UI thread only.
    /// </summary>
    private readonly HashSet<string> _currentlyShowingMessages = [];

    public void ShowInfoBarMessageFromAnyThread(string message, params InfoBarUI[] items)
        => ShowInfoBarMessageFromAnyThread(message, isCloseButtonVisible: true, KnownMonikers.StatusInformation, items);

    public void ShowInfoBarMessageFromAnyThread(
        string message,
        bool isCloseButtonVisible,
        ImageMoniker imageMoniker,
        params InfoBarUI[] items)
    {
        // We can be called from any thread since errors can occur anywhere, however we can only construct and InfoBar from the UI thread.
        _threadingContext.JoinableTaskFactory.RunAsync(async () =>
        {
            using var _ = _listener.BeginAsyncOperation(nameof(ShowInfoBarMessageFromAnyThread));

            await ShowInfoBarMessageAsync(message, isCloseButtonVisible, imageMoniker, items).ConfigureAwait(false);
        });
    }

    public async Task<InfoBarMessage?> ShowInfoBarMessageAsync(
        string message,
        bool isCloseButtonVisible,
        ImageMoniker imageMoniker,
        params InfoBarUI[] items)
    {
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(_threadingContext.DisposalToken);

        // If we're already shown this same message to the user, then do not bother showing it
        // to them again.  It will just be noisy.
        if (await GetInfoBarHostObjectAsync().ConfigureAwait(true) is not IVsInfoBarHost infoBarHost)
            return null;

        if (_currentlyShowingMessages.Contains(message))
            return null;

        // create action item list
        var actionItems = new List<IVsInfoBarActionItem>();

        foreach (var item in items)
        {
            switch (item.Kind)
            {
                case InfoBarUI.UIKind.Button:
                    actionItems.Add(new InfoBarButton(item.Title));
                    break;
                case InfoBarUI.UIKind.HyperLink:
                    actionItems.Add(new InfoBarHyperlink(item.Title));
                    break;
                case InfoBarUI.UIKind.Close:
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(item.Kind);
            }
        }

        var infoBarModel = new InfoBarModel(
            [new InfoBarTextSpan(message)],
            actionItems,
            imageMoniker,
            isCloseButtonVisible);

        var factory = await _vsInfoBarUIFactory.GetValueAsync().ConfigureAwait(true);
        var infoBarUI = factory.CreateInfoBar(infoBarModel);
        if (infoBarUI == null)
            return null;

        uint? infoBarCookie = null;
        var eventSink = new InfoBarEvents(items, onClose: () =>
        {
            Contract.ThrowIfFalse(_threadingContext.JoinableTaskContext.IsOnMainThread);

            // Remove the message from the list that we're keeping track of.  Future identical
            // messages can now be shown.
            _currentlyShowingMessages.Remove(message);

            // Run given onClose action if there is one.
            items.FirstOrDefault(i => i.Kind == InfoBarUI.UIKind.Close).Action?.Invoke();

            if (infoBarCookie.HasValue)
            {
                infoBarUI.Unadvise(infoBarCookie.Value);
            }
        });

        if (ErrorHandler.Succeeded(infoBarUI.Advise(eventSink, out var cookie)))
            infoBarCookie = cookie;

        infoBarHost.AddInfoBar(infoBarUI);

        _currentlyShowingMessages.Add(message);
        return new InfoBarMessage(this, infoBarHost, message, imageMoniker, infoBarUI, infoBarCookie);
    }

    private async Task<object?> GetInfoBarHostObjectAsync()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        if (_windowFrame != null)
            return ErrorHandler.Succeeded(_windowFrame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out var windowInfoBarHostObject)) ? windowInfoBarHostObject : null;

        var shell = await _vsShell.GetValueAsync().ConfigureAwait(true);
        return ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var globalInfoBarHostObject))
            ? globalInfoBarHostObject
            : null;
    }

    public sealed class InfoBarMessage(
        VisualStudioInfoBar visualStudioInfoBar,
        IVsInfoBarHost infoBarHost,
        string message,
        ImageMoniker imageMoniker,
        IVsInfoBarUIElement infoBarUI,
        uint? infoBarCookie)
    {
        public readonly string Message = message;
        public readonly ImageMoniker ImageMoniker = imageMoniker;

        private bool _removed;

        public void Remove()
        {
            visualStudioInfoBar._threadingContext.ThrowIfNotOnUIThread();
            if (_removed)
                return;

            _removed = true;
            if (infoBarCookie.HasValue)
                infoBarUI.Unadvise(infoBarCookie.Value);

            infoBarHost.RemoveInfoBar(infoBarUI);
            visualStudioInfoBar._currentlyShowingMessages.Remove(this.Message);
        }
    }

    private sealed class InfoBarEvents : IVsInfoBarUIEvents
    {
        private readonly InfoBarUI[] _items;
        private readonly Action _onClose;

        public InfoBarEvents(InfoBarUI[] items, Action onClose)
        {
            Contract.ThrowIfNull(onClose);

            _items = items;
            _onClose = onClose;
        }

        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            var item = _items.FirstOrDefault(i => i.Title == actionItem.Text);
            if (item.IsDefault)
            {
                return;
            }

            item.Action?.Invoke();

            if (!item.CloseAfterAction)
            {
                return;
            }

            infoBarUIElement.Close();
        }

        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
            => _onClose();
    }
}
