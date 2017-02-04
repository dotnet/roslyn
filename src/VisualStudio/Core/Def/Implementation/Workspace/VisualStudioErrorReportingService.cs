// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class VisualStudioErrorReportingService : IErrorReportingService
    {
        private readonly static InfoBarButton s_enableItem = new InfoBarButton(ServicesVSResources.Enable);
        private readonly static InfoBarButton s_enableAndIgnoreItem = new InfoBarButton(ServicesVSResources.Enable_and_ignore_future_errors);

        private readonly IServiceProvider _serviceProvider;
        private readonly IForegroundNotificationService _foregroundNotificationService;
        private readonly IAsynchronousOperationListener _listener;

        public VisualStudioErrorReportingService(
            SVsServiceProvider serviceProvider, IForegroundNotificationService foregroundNotificationService, IAsynchronousOperationListener listener)
        {
            _serviceProvider = serviceProvider;
            _foregroundNotificationService = foregroundNotificationService;
            _listener = listener;
        }

        public void ShowErrorInfo(string message, params ErrorReportingUI[] items)
        {
            // We can be called from any thread since errors can occur anywhere, however we can only construct and InfoBar from the UI thread.
            _foregroundNotificationService.RegisterNotification(() =>
            {
                if (TryGetInfoBarData(out var frame, out var factory))
                {
                    CreateInfoBar(factory, frame, message, items);
                }
            }, _listener.BeginAsyncOperation("Show InfoBar"));
        }

        private bool TryGetInfoBarData(out IVsWindowFrame frame, out IVsInfoBarUIFactory factory)
        {
            frame = null;
            factory = null;
            var monitorSelectionService = _serviceProvider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

            // We want to get whichever window is currently in focus (including toolbars) as we could have had an exception thrown from the error list or interactive window
            if (monitorSelectionService != null &&
               ErrorHandler.Succeeded(monitorSelectionService.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out object value)))
            {
                frame = value as IVsWindowFrame;
            }
            else
            {
                return false;
            }

            factory = _serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
            return frame != null && factory != null;
        }

        private void CreateInfoBar(IVsInfoBarUIFactory factory, IVsWindowFrame frame, string message, ErrorReportingUI[] items)
        {
            if (ErrorHandler.Failed(frame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out var unknown)))
            {
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
                    case ErrorReportingUI.UIKind.Button:
                        actionItems.Add(new InfoBarButton(item.Title));
                        break;
                    case ErrorReportingUI.UIKind.HyperLink:
                        actionItems.Add(new InfoBarHyperlink(item.Title));
                        break;
                    case ErrorReportingUI.UIKind.Close:
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
                items.FirstOrDefault(i => i.Kind == ErrorReportingUI.UIKind.Close).Action?.Invoke();

                if (infoBarCookie.HasValue)
                {
                    infoBarUI.Unadvise(infoBarCookie.Value);
                }
            });
            infoBarUI.Advise(eventSink, out var cookie);
            infoBarCookie = cookie;

            var host = (IVsInfoBarHost)unknown;
            host.AddInfoBar(infoBarUI);
        }

        private class InfoBarEvents : IVsInfoBarUIEvents
        {
            private readonly ErrorReportingUI[] _items;
            private readonly Action _onClose;

            public InfoBarEvents(ErrorReportingUI[] items, Action onClose)
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
            {
                _onClose();
            }
        }

        private static bool TryCreateInfoBarUI(IVsInfoBarUIFactory infoBarUIFactory, IVsInfoBar infoBar, out IVsInfoBarUIElement uiElement)
        {
            uiElement = infoBarUIFactory.CreateInfoBar(infoBar);
            return uiElement != null;
        }

        public void ShowDetailedErrorInfo(Exception exception)
        {
            string errorInfo = GetFormattedExceptionStack(exception);
            (new DetailedErrorInfoDialog(exception.Message, errorInfo)).ShowModal();
        }
    }
}
