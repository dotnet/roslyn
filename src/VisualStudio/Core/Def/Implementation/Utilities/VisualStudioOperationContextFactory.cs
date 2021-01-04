// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    [ExportWorkspaceService(typeof(IOperationContextFactory), ServiceLayer.Host), Shared]
    internal partial class VisualStudioOperationContextFactory : IOperationContextFactory
    {
        private readonly SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioOperationContextFactory(
            SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IOperationContext CreateOperationContext(string title, string description, bool allowCancellation, bool showProgress)
        {
            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            Contract.ThrowIfNull(workspace);

            var notificationService = workspace.Services.GetService<IGlobalOperationNotificationService>();
            Contract.ThrowIfNull(notificationService);

            var dialogFactory = (IVsThreadedWaitDialogFactory)_serviceProvider.GetService(typeof(SVsThreadedWaitDialogFactory));
            Contract.ThrowIfNull(dialogFactory);

            return new VisualStudioOperationContext(notificationService, dialogFactory, title, description, allowCancellation, showProgress);
        }

        private partial class VisualStudioOperationContext : AbstractOperationContext
        {
            private const int DelayToShowDialogSecs = 2;

            private readonly IVsThreadedWaitDialog3 _dialog;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly GlobalOperationRegistration _registration;

            private readonly string _title;
            private readonly bool _allowCancel;

            public VisualStudioOperationContext(
                IGlobalOperationNotificationService notificationService,
                IVsThreadedWaitDialogFactory dialogFactory,
                string title,
                string description,
                bool allowCancel,
                bool showProgress)
                : base(description)
            {
                _title = title;
                _allowCancel = allowCancel;
                _cancellationTokenSource = new CancellationTokenSource();

                _dialog = CreateDialog(dialogFactory, showProgress);
                _registration = notificationService.Start(title);

                this.AddScope(description);
            }

            private IVsThreadedWaitDialog3 CreateDialog(
                IVsThreadedWaitDialogFactory dialogFactory, bool showProgress)
            {
                Marshal.ThrowExceptionForHR(dialogFactory.CreateInstance(out var dialog2));
                Contract.ThrowIfNull(dialog2);

                var dialog3 = (IVsThreadedWaitDialog3)dialog2;

                var callback = new Callback(this);

                dialog3.StartWaitDialogWithCallback(
                    szWaitCaption: _title,
                    szWaitMessage: this.Description,
                    szProgressText: null,
                    varStatusBmpAnim: null,
                    szStatusBarText: null,
                    fIsCancelable: _allowCancel,
                    iDelayToShowDialog: DelayToShowDialogSecs,
                    fShowProgress: showProgress,
                    iTotalSteps: this.TotalItems,
                    iCurrentStep: this.CompletedItems,
                    pCallback: callback);

                return dialog3;
            }

            public override CancellationToken CancellationToken
            {
                get
                {
                    return _allowCancel
                        ? _cancellationTokenSource.Token
                        : CancellationToken.None;
                }
            }

            protected override void OnScopeInformationChanged()
            {
                ((IVsThreadedWaitDialog2)_dialog).UpdateProgress(
                    this.Description,
                    szProgressText: null,
                    szStatusBarText: null,
                    iCurrentStep: this.CompletedItems,
                    iTotalSteps: this.TotalItems,
                    fDisableCancel: !_allowCancel,
                    pfCanceled: out _);
            }

            public void Dispose()
            {
                _dialog.EndWaitDialog(out var canceled);

                if (canceled == 0)
                {
                    _registration.Done();
                }

                _registration.Dispose();
            }

            private void OnCanceled()
            {
                if (_allowCancel)
                    _cancellationTokenSource.Cancel();
            }

            private class Callback : IVsThreadedWaitDialogCallback
            {
                private readonly VisualStudioOperationContext _context;

                public Callback(VisualStudioOperationContext context)
                    => _context = context;

                public void OnCanceled()
                    => _context.OnCanceled();
            }
        }
    }
}
