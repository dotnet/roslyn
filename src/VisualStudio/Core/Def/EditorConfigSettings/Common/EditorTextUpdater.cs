// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common
{
    internal class EditorTextUpdater
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IVsTextLines _textLines;

        public EditorTextUpdater(IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
                                 IVsTextLines textLines)
        {
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _textLines = textLines;
        }

        public void UpdateText(IReadOnlyList<TextChange> changes)
        {
            var buffer = _editorAdaptersFactoryService.GetDocumentBuffer(_textLines);
            if (buffer is null)
            {
                return;
            }

            TextEditApplication.UpdateText(changes.ToImmutableArray(), buffer, EditOptions.DefaultMinimalChange);
        }
    }
}
