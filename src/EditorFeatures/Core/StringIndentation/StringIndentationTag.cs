// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.StringIndentation;

/// <summary>
/// Tag that specifies how a string's content is indented.
/// </summary>
internal sealed class StringIndentationTag : BrushTag, IEquatable<StringIndentationTag>
{
    private readonly StringIndentationTaggerProvider _provider;

    public readonly ImmutableArray<SnapshotSpan> OrderedHoleSpans;

    public StringIndentationTag(
        StringIndentationTaggerProvider provider,
        IEditorFormatMap editorFormatMap,
        ImmutableArray<SnapshotSpan> orderedHoleSpans)
        : base(editorFormatMap)
    {
        _provider = provider;
        OrderedHoleSpans = orderedHoleSpans;
    }

    protected override Color? GetColor(IWpfTextView view, IEditorFormatMap editorFormatMap)
    {
        var brush = view.VisualElement.TryFindResource("outlining.verticalrule.foreground") as SolidColorBrush;
        return brush?.Color;
    }

    // Intentionally throwing, we have never supported this facility, and there is no contract around placing
    // these tags in sets or maps.
    public override int GetHashCode()
        => throw new NotImplementedException();

    public override bool Equals(object? obj)
        => Equals(obj as StringIndentationTag);

    public bool Equals(StringIndentationTag? other)
    {
        if (other is null)
            return false;

        if (this.OrderedHoleSpans.Length != other.OrderedHoleSpans.Length)
            return false;

        for (int i = 0, n = this.OrderedHoleSpans.Length; i < n; i++)
        {
            if (!_provider.SpanEquals(this.OrderedHoleSpans[i], other.OrderedHoleSpans[i]))
                return false;
        }

        return true;
    }
}
