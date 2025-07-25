// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense;

internal static class PresentationExtensions
{
    extension(ImmutableArray<TaggedText> taggedTexts)
    {
        internal ImmutableArray<object> ToInteractiveVsTextAdornments(
        INavigationActionFactory? navigationActionFactory)
        => taggedTexts.ToInteractiveTextElements(navigationActionFactory).SelectAsArray(ToVsElement);
    }

    extension(QuickInfoGlyphElement element)
    {
        public VisualStudio.Text.Adornments.ImageElement ToVsElement()
        => new(element.Glyph.GetImageId());
    }

    extension(QuickInfoClassifiedTextRun run)
    {
        public VisualStudio.Text.Adornments.ClassifiedTextRun ToVsRun()
        => run.NavigationAction is not null
            ? new(run.ClassificationTypeName, run.Text, run.NavigationAction, run.Tooltip, (VisualStudio.Text.Adornments.ClassifiedTextRunStyle)run.Style)
            : new(run.ClassificationTypeName, run.Text, (VisualStudio.Text.Adornments.ClassifiedTextRunStyle)run.Style);
    }

    extension(QuickInfoClassifiedTextElement element)
    {
        public VisualStudio.Text.Adornments.ClassifiedTextElement ToVsElement()
        => new(element.Runs.Select(ToVsRun));
    }

    extension(QuickInfoContainerElement element)
    {
        public VisualStudio.Text.Adornments.ContainerElement ToVsElement()
        => new((VisualStudio.Text.Adornments.ContainerElementStyle)element.Style, element.Elements.Select(ToVsElement));
    }

    extension(QuickInfoElement value)
    {
        public object ToVsElement()
        => value switch
        {
            QuickInfoGlyphElement element => element.ToVsElement(),
            QuickInfoContainerElement element => element.ToVsElement(),
            QuickInfoClassifiedTextElement element => element.ToVsElement(),

            _ => value
        };
    }
}
