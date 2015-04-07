using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VS = Microsoft.VisualStudio.Progression;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal class VisualStudioErrorReportingService : IErrorReportingService
    {
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IForegroundNotificationService _foregroundNotificationService;
        private readonly IDocumentTrackingService _documentTrackingService;
        private readonly static InfoBarButton EnableItem = new InfoBarButton(ServicesVSResources.Enable);
        private readonly static InfoBarButton EnableAndIgnoreItem = new InfoBarButton(ServicesVSResources.EnableAndIgnore);

        public VisualStudioErrorReportingService(VisualStudioWorkspaceImpl workspace, IForegroundNotificationService foregroundNotificationService)
        {
            _workspace = workspace;
            _foregroundNotificationService = foregroundNotificationService;
            _documentTrackingService = workspace.Services.GetService<IDocumentTrackingService>();

        }

        public void ShowErrorInfoForCodeFix(string codefixName, Action OnEnableClicked, Action OnEnableAndIgnoreClicked)
        {
            var documentId = _documentTrackingService.GetActiveDocument();

            // We can be called from any thread since errors can occur anywhere, however we can only construct and InfoBar from the UI thread.
            var waiter = new InfoBarWaiter();
            _foregroundNotificationService.RegisterNotification(() =>
            {
                IVsWindowFrame frame;
                IVsInfoBarUIFactory factory;
                if (_workspace.TryGetInfoBarData(documentId, out frame, out factory))
                {
                    CreateInfoBar(codefixName, OnEnableClicked, OnEnableAndIgnoreClicked, frame, factory);
                }
            }, waiter.BeginAsyncOperation("Show InfoBar"));
        }

        private void CreateInfoBar(string name, Action onEnableClicked, Action onEnableAndIgnoreClicked, IVsWindowFrame frame, IVsInfoBarUIFactory factory)
        {
            object unknown;
            if (frame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out unknown) == VSConstants.S_OK)
            {
                var textSpans = new List<IVsInfoBarTextSpan>()
                {
                    new InfoBarTextSpan(string.Format(ServicesVSResources.CodefixOrRefactoringEncounteredError, name)),
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
                    uint? infoBarCookie = null;
                    InfoBarEvents eventSink = new InfoBarEvents(onEnableClicked, onEnableAndIgnoreClicked, () =>
                    {
                        if (infoBarCookie.HasValue)
                        {
                            infoBarUI.Unadvise(infoBarCookie.Value);
                        }
                    });
                    uint cookie;
                    infoBarUI.Advise(eventSink, out cookie);
                    infoBarCookie = cookie;
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
            private readonly Action onClosed;
            private readonly Action onEnableAndIgnoreClicked;
            private readonly Action onEnableClicked;

            public InfoBarEvents(Action onEnableClicked, Action onEnableAndIgnoreClicked, Action onClose)
            {
                this.onEnableClicked = onEnableClicked;
                this.onEnableAndIgnoreClicked = onEnableAndIgnoreClicked;
                this.onClosed = onClose;
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
                onClosed();
            }
        }

        internal class InfoBarWaiter : AsynchronousOperationListener { }
    }
}
