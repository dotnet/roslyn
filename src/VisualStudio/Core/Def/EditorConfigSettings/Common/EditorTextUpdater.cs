// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common
{
    internal static class EditorTextUpdater
    {
        public static void UpdateText(IThreadingContext threadingContext,
                                      IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
                                      IVsTextLines textLines,
                                      IWpfSettingsEditorViewModel viewModel)
            => threadingContext.JoinableTaskFactory.Run(async () =>
            {
                var buffer = editorAdaptersFactoryService.GetDocumentBuffer(textLines);
                if (buffer is null)
                {
                    return;
                }

                var changes = await viewModel.GetChangesAsync().ConfigureAwait(true);
                if (changes is null)
                {
                    return;
                }

                TextEditApplication.UpdateText(changes.ToImmutableArray(), buffer, EditOptions.DefaultMinimalChange);
            });
    }
}
