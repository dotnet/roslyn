// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// Manages access to Visual Studio code window information in a thread-safe way.
    /// </summary>
    internal class VisualStudioCodeWindowInfoService
    {
        private readonly IVsCodeWindow _codeWindow;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IThreadingContext _threadingContext;

        public VisualStudioCodeWindowInfoService(
            IVsCodeWindow codeWindow,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IThreadingContext threadingContext)
        {
            threadingContext.ThrowIfNotOnUIThread();
            _codeWindow = codeWindow;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _threadingContext = threadingContext;
        }

        /// <summary>
        /// Get <see cref="VisualStudioCodeWindowInfo"/>, switching to the UI thread if necessary.
        /// </summary>
        public async ValueTask<VisualStudioCodeWindowInfo?> GetVisualStudioCodeWindowInfoAsync(CancellationToken token)
        {
            if (!_threadingContext.JoinableTaskContext.IsOnMainThread)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(token);
            }

            return GetVisualStudioCodeWindowInfo();
        }

        /// <summary>
        /// Returns a version of this service whose apis can only be called on the UI thread.
        /// </summary>
        public VisualStudioCodeWindowInfoService_OnlyCallOnUIThread GetServiceAndThrowIfNotOnUIThread()
        {
            _threadingContext.ThrowIfNotOnUIThread();
            return new VisualStudioCodeWindowInfoService_OnlyCallOnUIThread(this);
        }

        private VisualStudioCodeWindowInfo? GetVisualStudioCodeWindowInfo()
        {
            var wpfTextView = GetLastActiveIWpfTextView();
            if (wpfTextView is null)
            {
                return null;
            }
            var textBuffer = wpfTextView.TextBuffer;

            var caretPoint = ITextViewExtensions.GetCaretPoint(wpfTextView, textBuffer);

            if (_editorAdaptersFactoryService.GetBufferAdapter(textBuffer) is IPersistFileFormat persistFileFormat &&
                ErrorHandler.Succeeded(persistFileFormat.GetCurFile(out var filePath, out var _)))
            {
                return new VisualStudioCodeWindowInfo(textBuffer, filePath, caretPoint);
            }

            return null;
        }

        private SnapshotPoint? GetCurrentCaretSnapshotPoint()
        {
            var activeTextView = GetLastActiveIWpfTextView();
            if (activeTextView is null)
            {
                return null;
            }

            var textBuffer = activeTextView.TextBuffer;
            return ITextViewExtensions.GetCaretPoint(activeTextView, textBuffer);
        }

        private SnapshotPoint? GetSnapshotPointFromCaretPosition(CaretPosition newPosition)
        {
            var activeTextView = GetLastActiveIWpfTextView();
            if (activeTextView is null)
            {
                return null;
            }

            var textBuffer = activeTextView.TextBuffer;
            return newPosition.Point.GetPoint(textBuffer, PositionAffinity.Predecessor);
        }

        private IWpfTextView? GetLastActiveIWpfTextView()
        {
            if (ErrorHandler.Failed(_codeWindow.GetLastActiveView(out var textView)))
                return null;

            return _editorAdaptersFactoryService.GetWpfTextView(textView);
        }

        /// <summary>
        /// The apis is this class can only be called from the UI thread.
        /// </summary>
        public class VisualStudioCodeWindowInfoService_OnlyCallOnUIThread
        {
            private readonly VisualStudioCodeWindowInfoService _service;

            protected internal VisualStudioCodeWindowInfoService_OnlyCallOnUIThread(VisualStudioCodeWindowInfoService service)
            {
                _service = service;
            }

            public SnapshotPoint? GetCurrentCaretSnapshotPoint()
            {
                return _service.GetCurrentCaretSnapshotPoint();
            }

            public SnapshotPoint? GetSnapshotPointFromCaretPosition(CaretPosition newPosition)
            {
                return _service.GetSnapshotPointFromCaretPosition(newPosition);
            }

            public IWpfTextView? GetLastActiveIWpfTextView()
            {
                return _service.GetLastActiveIWpfTextView();
            }
        }
    }
}
