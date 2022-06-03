// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class CodeDefinitionWindow_InProc : InProcComponent
    {
        private CodeDefinitionWindow_InProc()
        {
        }

        public static CodeDefinitionWindow_InProc Create()
            => new CodeDefinitionWindow_InProc();

        private static IWpfTextView GetCodeDefinitionWpfTextView()
        {
            var shell = GetGlobalService<SVsUIShell, IVsUIShell>();
            var windowGuid = Guid.Parse(ToolWindowGuids80.CodedefinitionWindow);

            Marshal.ThrowExceptionForHR(shell.FindToolWindow(0, ref windowGuid, out var windowFrame));

            var view = VsShellUtilities.GetTextView(windowFrame);
            var editorAdaptersService = GetComponentModelService<IVsEditorAdaptersFactoryService>();

            var wpfView = editorAdaptersService.GetWpfTextView(view);

            Contract.ThrowIfNull(wpfView, "We were unable to get the Code Definition Window view.");

            return wpfView;
        }

        public void Show()
        {
            InvokeOnUIThread(cancellationToken =>
            {
                var codeDefinitionWindow = GetGlobalService<SVsCodeDefView, IVsCodeDefView>();
                codeDefinitionWindow.ShowWindow();
            });
        }

        /// <summary>
        /// Waits for all async processing to complete, including the async processing in the
        /// code definition window itself.
        /// </summary>
        private static void WaitUntilProcessingComplete()
        {
            GetWaitingService().WaitForAsyncOperations(FeatureAttribute.CodeDefinitionWindow);

            InvokeOnUIThread(cancellationToken =>
            {
                var codeDefinitionWindow = GetGlobalService<SVsCodeDefView, IVsCodeDefView>();

                // The code definition window does some processing on idle, which we can force after we've completed our
                // processing.
                codeDefinitionWindow.ForceIdleProcessing();
            });
        }

        /// <summary>
        /// Returns the current line of text in the code definition window itself.
        /// </summary>
        public string GetCurrentLineText()
        {
            WaitUntilProcessingComplete();

            return InvokeOnUIThread(cancellationToken =>
            {
                var view = GetCodeDefinitionWpfTextView();
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();

                return line.GetText();
            });
        }

        /// <summary>
        /// Returns the entire text of the code definition window itself.
        /// </summary>
        public string GetText()
        {
            WaitUntilProcessingComplete();

            return InvokeOnUIThread(cancellationToken =>
            {
                var view = GetCodeDefinitionWpfTextView();
                return view.TextSnapshot.GetText();
            });
        }
    }
}
