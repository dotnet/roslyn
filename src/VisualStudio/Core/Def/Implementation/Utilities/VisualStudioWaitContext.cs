// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal sealed partial class VisualStudioWaitContext : IWaitContext
    {
        private const int DelayToShowDialogSecs = 2;

        private readonly IVsThreadedWaitDialog3 _dialog;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly GlobalOperationRegistration _registration;

        private readonly string _title;
        private string _message;
        private bool _allowCancel;

        public VisualStudioWaitContext(
            IGlobalOperationNotificationService notificationService,
            IVsThreadedWaitDialogFactory dialogFactory,
            string title,
            string message,
            bool allowCancel)
        {
            _title = title;
            _message = message;
            _allowCancel = allowCancel;
            _cancellationTokenSource = new CancellationTokenSource();

            _dialog = CreateDialog(dialogFactory);
            _registration = notificationService.Start(title);
        }

        private IVsThreadedWaitDialog3 CreateDialog(IVsThreadedWaitDialogFactory dialogFactory)
        {
            IVsThreadedWaitDialog2 dialog2;
            Marshal.ThrowExceptionForHR(dialogFactory.CreateInstance(out dialog2));
            Contract.ThrowIfNull(dialog2);

            var dialog3 = (IVsThreadedWaitDialog3)dialog2;

            var callback = new Callback(this);

            dialog3.StartWaitDialogWithCallback(
                szWaitCaption: _title,
                szWaitMessage: _message,
                szProgressText: null,
                varStatusBmpAnim: null,
                szStatusBarText: null,
                fIsCancelable: _allowCancel,
                iDelayToShowDialog: DelayToShowDialogSecs,
                fShowProgress: false,
                iTotalSteps: 0,
                iCurrentStep: 0,
                pCallback: callback);

            return dialog3;
        }

        public CancellationToken CancellationToken
        {
            get
            {
                return _allowCancel
                    ? _cancellationTokenSource.Token
                    : CancellationToken.None;
            }
        }

        public string Message
        {
            get
            {
                return _message;
            }

            set
            {
                _message = value;
                UpdateDialog();
            }
        }

        public bool AllowCancel
        {
            get
            {
                return _allowCancel;
            }

            set
            {
                _allowCancel = value;
                UpdateDialog();
            }
        }

        private void UpdateDialog()
        {
            bool hasCancelled;
            _dialog.UpdateProgress(
                _message,
                szProgressText: null,
                szStatusBarText: null,
                iCurrentStep: 0,
                iTotalSteps: 0,
                fDisableCancel: !_allowCancel,
                pfCanceled: out hasCancelled);
        }

        public void UpdateProgress()
        {
        }

        public void Dispose()
        {
            int canceled;
            _dialog.EndWaitDialog(out canceled);

            if (canceled == 0)
            {
                _registration.Done();
            }

            _registration.Dispose();
        }

        private void OnCanceled()
        {
            if (_allowCancel)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }
}
