// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IInfoBarService), layer: ServiceLayer.Host), Shared]
    internal class VisualStudioInfoBarService : ForegroundThreadAffinitizedObject, IInfoBarService
    {
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IForegroundNotificationService _foregroundNotificationService;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioInfoBarService(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            IForegroundNotificationService foregroundNotificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext)
        {
            _serviceProvider = serviceProvider;
            _foregroundNotificationService = foregroundNotificationService;
            _listener = listenerProvider.GetListener(FeatureAttribute.InfoBar);
        }

        public void ShowInfoBar(string message, params InfoBarUI[] items)
        {
            ThisCanBeCalledOnAnyThread();

            // We can be called from any thread since errors can occur anywhere, however we can only construct and InfoBar from the UI thread.
            _foregroundNotificationService.RegisterNotification(() =>
            {
                if (TryGetInfoBarData(out var infoBarHost))
                {
                    CreateInfoBar(infoBarHost, message, items);
                }
            }, _listener.BeginAsyncOperation(nameof(ShowInfoBar)));
        }

        private bool TryGetInfoBarData(out IVsInfoBarHost infoBarHost)
        {
            AssertIsForeground();

            infoBarHost = null;

            // global error info, show it on main window info bar
            if (_serviceProvider.GetService(typeof(SVsShell)) is not IVsShell shell ||
                ErrorHandler.Failed(shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var globalInfoBar)))
            {
                return false;
            }

            infoBarHost = globalInfoBar as IVsInfoBarHost;
            return infoBarHost != null;
        }

        private void CreateInfoBar(IVsInfoBarHost infoBarHost, string message, InfoBarUI[] items)
        {
            if (!(_serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) is IVsInfoBarUIFactory factory))
            {
                // no info bar factory, don't do anything
                return;
            }

            var textSpans = new List<IVsInfoBarTextSpan>()
            {
                new InfoBarTextSpan(message)
            };

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
                textSpans,
                actionItems.ToArray(),
                KnownMonikers.StatusInformation,
                isCloseButtonVisible: true);

            if (!TryCreateInfoBarUI(factory, infoBarModel, out var infoBarUI))
            {
                return;
            }

            uint? infoBarCookie = null;
            var eventSink = new InfoBarEvents(items, () =>
            {
                // run given onClose action if there is one.
                items.FirstOrDefault(i => i.Kind == InfoBarUI.UIKind.Close).Action?.Invoke();

                if (infoBarCookie.HasValue)
                {
                    infoBarUI.Unadvise(infoBarCookie.Value);
                }
            });

            infoBarUI.Advise(eventSink, out var cookie);
            infoBarCookie = cookie;

            infoBarHost.AddInfoBar(infoBarUI);
        }

        private class InfoBarEvents : IVsInfoBarUIEvents
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

        private static bool TryCreateInfoBarUI(IVsInfoBarUIFactory infoBarUIFactory, IVsInfoBar infoBar, out IVsInfoBarUIElement uiElement)
        {
            uiElement = infoBarUIFactory.CreateInfoBar(infoBar);
            return uiElement != null;
        }
    }
}
