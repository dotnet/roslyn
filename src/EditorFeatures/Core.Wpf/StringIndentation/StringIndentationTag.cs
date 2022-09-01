// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.StringIndentation
{
    /// <summary>
    /// Tag that specifies how a string's content is indented.
    /// </summary>
    internal class StringIndentationTag : BrushTag
    {
        public readonly ImmutableArray<SnapshotSpan> OrderedHoleSpans;

        public StringIndentationTag(
            IEditorFormatMap editorFormatMap,
            ImmutableArray<SnapshotSpan> orderedHoleSpans)
            : base(editorFormatMap)
        {
            OrderedHoleSpans = orderedHoleSpans;
        }

        protected override Color? GetColor(IWpfTextView view, IEditorFormatMap editorFormatMap)
        {
            var brush = view.VisualElement.TryFindResource("outlining.verticalrule.foreground") as SolidColorBrush;
            return brush?.Color;
        }
    }
}
