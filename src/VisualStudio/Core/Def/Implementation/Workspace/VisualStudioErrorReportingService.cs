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
    internal class VisualStudioErrorReportingService : IErrorReportingService
    {
        private readonly static InfoBarButton s_enableItem = new InfoBarButton(ServicesVSResources.Enable);
        private readonly static InfoBarButton s_enableAndIgnoreItem = new InfoBarButton(ServicesVSResources.Enable_and_ignore_future_errors);

        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IForegroundNotificationService _foregroundNotificationService;
        private readonly IAsynchronousOperationListener _listener;

        public VisualStudioErrorReportingService(
            VisualStudioWorkspaceImpl workspace, IForegroundNotificationService foregroundNotificationService, IAsynchronousOperationListener listener)
        {
            _workspace = workspace;
            _foregroundNotificationService = foregroundNotificationService;
            _listener = listener;
        }

        public void ShowErrorInfoForCodeFix(string codefixName, Action OnEnable, Action OnEnableAndIgnore, Action OnClose)
        {
            // We can be called from any thread since errors can occur anywhere, however we can only construct and InfoBar from the UI thread.
            _foregroundNotificationService.RegisterNotification(() =>
            {
                IVsWindowFrame frame;
                IVsInfoBarUIFactory factory;
                if (_workspace.TryGetInfoBarData(out frame, out factory))
                {
                    CreateInfoBarForCodeFix(factory, frame, string.Format(ServicesVSResources._0_encountered_an_error_and_has_been_disabled, codefixName), OnClose, OnEnable, OnEnableAndIgnore);
                }
            }, _listener.BeginAsyncOperation("Show InfoBar"));
        }

        public void ShowErrorInfo(string message, params ErrorReportingUI[] items)
        {
            // We can be called from any thread since errors can occur anywhere, however we can only construct and InfoBar from the UI thread.
            _foregroundNotificationService.RegisterNotification(() =>
            {
                IVsWindowFrame frame;
                IVsInfoBarUIFactory factory;
                if (_workspace.TryGetInfoBarData(out frame, out factory))
                {
                    CreateInfoBar(factory, frame, message, items);
                }
            }, _listener.BeginAsyncOperation("Show InfoBar"));
        }

        private void CreateInfoBar(IVsInfoBarUIFactory factory, IVsWindowFrame frame, string message, ErrorReportingUI[] items)
        {
            object unknown;
            if (ErrorHandler.Failed(frame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out unknown)))
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

            IVsInfoBarUIElement infoBarUI;
            if (!TryCreateInfoBarUI(factory, infoBarModel, out infoBarUI))
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

            uint cookie;
            infoBarUI.Advise(eventSink, out cookie);
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

        private void CreateInfoBarForCodeFix(IVsInfoBarUIFactory factory, IVsWindowFrame frame, string message, Action onClose, Action onEnable = null, Action onEnableAndIgnore = null)
        {
            object unknown;
            if (ErrorHandler.Failed(frame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out unknown)))
            {
                return;
            }

            var textSpans = new List<IVsInfoBarTextSpan>()
            {
                new InfoBarTextSpan(message)
            };

            // create action item list
            var actionItems = new List<IVsInfoBarActionItem>();
            if (onEnable != null)
            {
                actionItems.Add(s_enableItem);
            }

            if (onEnableAndIgnore != null)
            {
                actionItems.Add(s_enableAndIgnoreItem);
            }

            var infoBarModel = new InfoBarModel(
                textSpans,
                actionItems.ToArray(),
                KnownMonikers.StatusInformation,
                isCloseButtonVisible: true);

            IVsInfoBarUIElement infoBarUI;
            if (!TryCreateInfoBarUI(factory, infoBarModel, out infoBarUI))
            {
                return;
            }

            uint? infoBarCookie = null;
            var eventSink = new CodeFixInfoBarEvents(() =>
            {
                onClose();

                if (infoBarCookie.HasValue)
                {
                    infoBarUI.Unadvise(infoBarCookie.Value);
                }
            }, onEnable, onEnableAndIgnore);

            uint cookie;
            infoBarUI.Advise(eventSink, out cookie);
            infoBarCookie = cookie;

            IVsInfoBarHost host = (IVsInfoBarHost)unknown;
            host.AddInfoBar(infoBarUI);
        }

        private class CodeFixInfoBarEvents : IVsInfoBarUIEvents
        {
            private readonly Action _onClose;
            private readonly Action _onEnable;
            private readonly Action _onEnableAndIgnore;

            public CodeFixInfoBarEvents(Action onClose, Action onEnable = null, Action onEnableAndIgnore = null)
            {
                Contract.ThrowIfNull(onClose);

                _onClose = onClose;
                _onEnable = onEnable;
                _onEnableAndIgnore = onEnableAndIgnore;
            }

            public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
            {
                if (actionItem.Equals(s_enableItem))
                {
                    _onEnable?.Invoke();
                }

                if (actionItem.Equals(s_enableAndIgnoreItem))
                {
                    _onEnableAndIgnore?.Invoke();
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
    }
}
