using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VS = Microsoft.VisualStudio.Progression;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal class VisualStudioErrorReportingService : IErrorReportingService
    {
        private VisualStudioWorkspaceImpl workspace;
        private static InfoBarButton EnableItem = new InfoBarButton(ServicesVSResources.Enable);
        private static InfoBarButton EnableAndIgnoreItem = new InfoBarButton(ServicesVSResources.EnableAndIgnore);

        public VisualStudioErrorReportingService(VisualStudioWorkspaceImpl workspace)
        {
            this.workspace = workspace;
        }

        public async void ShowErrorInfoForCodeFix(DocumentId documentId, string codefixName, Action OnEnableClicked, Action OnEnableAndIgnoreClicked)
        {
            // We can be called from any thread since errors can occur anywhere, however we can only construct and InfoBar from the UI thread.
            // We try and get the current UI dispatcher and invoke our code on that thread. We return immediately so we don't block waiting on the UI thread.
            var dispatcher = VS.UIThread.GetUIDispatcher();
            await dispatcher?.InvokeAsync(() =>
            {
                IVsWindowFrame frame;
                IVsInfoBarUIFactory factory;
                if (workspace.TryGetInfoBarData(documentId, out frame, out factory))
                {
                    CreateInfoBar(codefixName, OnEnableClicked, OnEnableAndIgnoreClicked, frame, factory);
                }
            });
        }

        private void CreateInfoBar(string codefixName, Action onEnableClicked, Action onEnableAndIgnoreClicked, IVsWindowFrame frame, IVsInfoBarUIFactory factory)
        {
            object unknown;
            if (frame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out unknown) == VSConstants.S_OK)
            {
                var textSpans = new List<IVsInfoBarTextSpan>()
                {
                    new InfoBarTextSpan(string.Format(ServicesVSResources.CodefixEncounteredError, codefixName)),
                };

                var infoBarModel = new InfoBarModel(
                    textSpans,
                    new IVsInfoBarActionItem[]
                        {
                            EnableItem,
                            EnableAndIgnoreItem
                        },
                    KnownMonikers.StatusInformation,
                    isCloseButtonVisible: true);

                IVsInfoBarUIElement infoBarUI;
                if (TryCreateInfoBarUI(infoBarModel, factory, out infoBarUI))
                {
                    InfoBarEvents eventSink = new InfoBarEvents(onEnableClicked, onEnableAndIgnoreClicked);
                    uint cookie;
                    infoBarUI.Advise(eventSink, out cookie);

                    IVsInfoBarHost host = (IVsInfoBarHost)unknown;
                    host.AddInfoBar(infoBarUI);
                }
            }
        }

        private static bool TryCreateInfoBarUI(IVsInfoBar infoBar, IVsInfoBarUIFactory infoBarUIFactory, out IVsInfoBarUIElement uiElement)
        {
            uiElement = infoBarUIFactory.CreateInfoBar(infoBar);
            return uiElement != null;
        }

        private class InfoBarEvents : IVsInfoBarUIEvents
        {
            private Action onEnableAndIgnoreClicked;
            private Action onEnableClicked;

            public InfoBarEvents(Action onEnableClicked, Action onEnableAndIgnoreClicked)
            {
                this.onEnableClicked = onEnableClicked;
                this.onEnableAndIgnoreClicked = onEnableAndIgnoreClicked;
            }

            public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
            {
                if (actionItem.Equals(EnableItem))
                {
                    onEnableClicked();
                }

                if (actionItem.Equals(EnableAndIgnoreItem))
                {
                    onEnableAndIgnoreClicked();
                }

                infoBarUIElement.Close();
            }

            public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
            {
            }
        }
    }
}
