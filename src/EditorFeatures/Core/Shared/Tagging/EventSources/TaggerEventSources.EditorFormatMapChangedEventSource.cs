// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class EditorFormatMapChangedEventSource : AbstractTaggerEventSource
        {
            private readonly IEditorFormatMap _editorFormatMap;

            public EditorFormatMapChangedEventSource(IEditorFormatMap editorFormatMap, TaggerDelay delay)
                : base(delay)
            {
                _editorFormatMap = editorFormatMap;
            }

            public override void Connect()
            {
                _editorFormatMap.FormatMappingChanged += OnEditorFormatMapChanged;
            }

            public override void Disconnect()
            {
                _editorFormatMap.FormatMappingChanged -= OnEditorFormatMapChanged;
            }

            private void OnEditorFormatMapChanged(object sender, FormatItemsEventArgs e)
            {
                this.RaiseChanged();
            }
        }
    }
}