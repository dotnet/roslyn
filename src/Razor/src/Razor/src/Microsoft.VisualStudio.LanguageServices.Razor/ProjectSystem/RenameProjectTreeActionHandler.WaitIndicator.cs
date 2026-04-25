// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Razor.ProjectSystem;

internal sealed partial class RenameProjectTreeHandler
{
    // Copied, and simplified to our needs, from https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Waiting/VisualStudioWaitContext.cs

    internal sealed class WaitIndicator : IDisposable
    {
        // Using a slightly shorter delay than you might expect, because this indicator happens _after_ the user
        // has already potentially seen a dialog for the actual file rename.
        private const int DelayToShowDialogSecs = 1;

        private readonly string _title;
        private readonly IVsThreadedWaitDialog3 _dialog;

        private string _message;

        public WaitIndicator(IVsThreadedWaitDialogFactory waitDialogFactory, string title, string message)
        {
            _title = title;
            _message = message;
            _dialog = CreateDialog(waitDialogFactory);
        }

        private IVsThreadedWaitDialog3 CreateDialog(IVsThreadedWaitDialogFactory dialogFactory)
        {
            Marshal.ThrowExceptionForHR(dialogFactory.CreateInstance(out var dialog2));

            Assumes.NotNull(dialog2);

            var dialog3 = (IVsThreadedWaitDialog3)dialog2;

            dialog3.StartWaitDialog(
                szWaitCaption: _title,
                szWaitMessage: _message,
                szProgressText: null,
                varStatusBmpAnim: null,
                szStatusBarText: null,
                fIsCancelable: false,
                iDelayToShowDialog: DelayToShowDialogSecs,
                fShowMarqueeProgress: true);

            return dialog3;
        }

        public void Dispose()
        {
            _dialog.EndWaitDialog(out _);
        }
    }
}
