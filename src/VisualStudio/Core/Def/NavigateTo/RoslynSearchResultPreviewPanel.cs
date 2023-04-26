// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Search.Data;
using Microsoft.VisualStudio.Search.UI.PreviewPanel.Models;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal sealed partial class RoslynSearchItemsSourceProvider
{
    /// <summary>
    /// Roslyn preview for our nav-to result.  We just provide a code-editor with the caret positioned in the
    /// correct location.
    /// </summary>
    private sealed class RoslynSearchResultPreviewPanel : SearchResultPreviewPanelBase
    {
        public override UIBaseModel UserInterface { get; }

        public RoslynSearchResultPreviewPanel(
            RoslynSearchItemsSourceProvider provider,
            Uri uri,
            Guid projectGuid,
            Span span,
            string title,
            ImageId icon)
            : base(title, icon)
        {
            UserInterface = new CodeEditorModel(
                nameof(RoslynSearchResultPreviewPanel),
                new VisualStudio.Threading.AsyncLazy<TextDocumentLocation>(() =>
                    Task.FromResult(new TextDocumentLocation(uri, projectGuid, span)),
                    provider._threadingContext.JoinableTaskFactory),
                isEditable: true);
        }
    }
}
