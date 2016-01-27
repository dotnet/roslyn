// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        private readonly static InfoBarButton s_enableAndIgnoreItem = new InfoBarButton(ServicesVSResources.EnableAndIgnore);

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

        public void ShowErrorInfoForCodeFix(string codefixName, Action OnEnableClicked, Action OnEnableAndIgnoreClicked, Action OnClose)
        {
            // We can be called from any thread since errors can occur anywhere, however we can only construct and InfoBar from the UI thread.
            _foregroundNotificationService.RegisterNotification(() =>
            {
                IVsWindowFrame frame;
                IVsInfoBarUIFactory factory;
                if (_workspace.TryGetInfoBarData(out frame, out factory))
                {
                    CreateInfoBar(factory, frame, string.Format(ServicesVSResources.CodefixOrRefactoringEncounteredError, codefixName), OnClose, OnEnableClicked, OnEnableAndIgnoreClicked);
                }
            }, _listener.BeginAsyncOperation("Show InfoBar"));
        }

        public void ShowErrorInfo(string title, Action OnClose)
        {
            // We can be called from any thread since errors can occur anywhere, however we can only construct and InfoBar from the UI thread.
            _foregroundNotificationService.RegisterNotification(() =>
            {
                IVsWindowFrame frame;
                IVsInfoBarUIFactory factory;
                if (_workspace.TryGetInfoBarData(out frame, out factory))
                {
                    CreateInfoBar(factory, frame, title, OnClose);
                }
            }, _listener.BeginAsyncOperation("Show InfoBar"));
        }

        private void CreateInfoBar(IVsInfoBarUIFactory factory, IVsWindowFrame frame, string title, Action onClose, Action onEnableClicked = null, Action onEnableAndIgnoreClicked = null)
        {
            object unknown;
            if (frame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out unknown) == VSConstants.S_OK)
            {
                var textSpans = new List<IVsInfoBarTextSpan>()
                {
                    new InfoBarTextSpan(title)
                };

                // create action item list
                var actionItems = new List<IVsInfoBarActionItem>();
                if (onEnableClicked != null)
                {
                    actionItems.Add(s_enableItem);
                }

                if (onEnableAndIgnoreClicked != null)
                {
                    actionItems.Add(s_enableAndIgnoreItem);
                }

                var infoBarModel = new InfoBarModel(
                    textSpans,
                    actionItems.ToArray(),
                    KnownMonikers.StatusInformation,
                    isCloseButtonVisible: true);

                IVsInfoBarUIElement infoBarUI;
                if (TryCreateInfoBarUI(factory, infoBarModel, out infoBarUI))
                {
                    uint? infoBarCookie = null;
                    InfoBarEvents eventSink = new InfoBarEvents(() =>
                    {
                        onClose();
                        if (infoBarCookie.HasValue)
                        {
                            infoBarUI.Unadvise(infoBarCookie.Value);
                        }
                    }, onEnableClicked, onEnableAndIgnoreClicked);

                    uint cookie;
                    infoBarUI.Advise(eventSink, out cookie);
                    infoBarCookie = cookie;

                    IVsInfoBarHost host = (IVsInfoBarHost)unknown;
                    host.AddInfoBar(infoBarUI);
                }
            }
        }

        private static bool TryCreateInfoBarUI(IVsInfoBarUIFactory infoBarUIFactory, IVsInfoBar infoBar, out IVsInfoBarUIElement uiElement)
        {
            uiElement = infoBarUIFactory.CreateInfoBar(infoBar);
            return uiElement != null;
        }

        private class InfoBarEvents : IVsInfoBarUIEvents
        {
            private readonly Action _onClosed;
            private readonly Action _onEnableAndIgnoreClicked;
            private readonly Action _onEnableClicked;

            public InfoBarEvents(Action onClose, Action onEnableClicked = null, Action onEnableAndIgnoreClicked = null)
            {
                Contract.ThrowIfNull(onClose);

                _onClosed = onClose;
                _onEnableClicked = onEnableClicked;
                _onEnableAndIgnoreClicked = onEnableAndIgnoreClicked;
            }

            public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
            {
                if (actionItem.Equals(s_enableItem))
                {
                    _onEnableClicked?.Invoke();
                }

                if (actionItem.Equals(s_enableAndIgnoreItem))
                {
                    _onEnableAndIgnoreClicked?.Invoke();
                }

                infoBarUIElement.Close();
            }

            public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
            {
                _onClosed();
            }
        }
    }
}
