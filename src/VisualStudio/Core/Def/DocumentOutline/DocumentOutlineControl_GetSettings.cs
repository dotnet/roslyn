// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineControl
    {
        private void EnqueuGetSettingsTask()
        {
            if (TryGetSettings_OnlyCallOnUIThread(out var settings))
            {
                EnqueuGetDocumentSymbolTask(settings);
            }
        }

        public bool TryGetSettings_OnlyCallOnUIThread([NotNullWhen(true)] out DocumentOutlineSettings? settings)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            settings = null;
            var activeTextView = GetLastActiveIWpfTextView();
            if (activeTextView is null)
                return false;

            var textBuffer = activeTextView.TextBuffer;
            var filePath = GetFilePath(textBuffer);
            if (filePath is null)
                return false;

            var caretPoint = activeTextView.GetCaretPoint(textBuffer);
            var searchQuery = SearchBox.Text;
            var sortOption = SortOption;

            settings = new DocumentOutlineSettings(textBuffer, filePath, searchQuery, sortOption, caretPoint);
            return true;

            string? GetFilePath(ITextBuffer textBuffer)
            {
                _threadingContext.ThrowIfNotOnUIThread();
                if (_editorAdaptersFactoryService.GetBufferAdapter(textBuffer) is IPersistFileFormat persistFileFormat &&
                    ErrorHandler.Succeeded(persistFileFormat.GetCurFile(out var filePath, out var _)))
                {
                    return filePath;
                }

                return null;
            }
        }

        internal record DocumentOutlineSettings(ITextBuffer TextBuffer, string FilePath, string? SearchQuery, SortOption SortOption, SnapshotPoint? CaretPoint);
    }
}
