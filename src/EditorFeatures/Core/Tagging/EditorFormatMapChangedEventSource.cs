// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Tagging;

internal sealed class EditorFormatMapChangedEventSource : AbstractTaggerEventSource
{
    private readonly IEditorFormatMap _editorFormatMap;

    public EditorFormatMapChangedEventSource(IEditorFormatMap editorFormatMap)
        => _editorFormatMap = editorFormatMap;

    public override void Connect()
        => _editorFormatMap.FormatMappingChanged += OnEditorFormatMapChanged;

    public override void Disconnect()
        => _editorFormatMap.FormatMappingChanged -= OnEditorFormatMapChanged;

    private void OnEditorFormatMapChanged(object sender, FormatItemsEventArgs e)
        => this.RaiseChanged();
}
