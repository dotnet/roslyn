// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense;

internal static class PresentationExtensions
{
    public static VisualStudio.Text.Adornments.ImageElement ToVsElement(this QuickInfoGlyphElement element)
        => new(element.Glyph.GetImageId());

    public static VisualStudio.Text.Adornments.ClassifiedTextRun ToVsRun(this QuickInfoClassifiedTextRun run)
        => new(run.ClassificationTypeName, run.Text, run.NavigationAction, run.Tooltip, (VisualStudio.Text.Adornments.ClassifiedTextRunStyle)run.Style);

    public static VisualStudio.Text.Adornments.ClassifiedTextElement ToVsElement(this QuickInfoClassifiedTextElement element)
        => new(element.Runs.Select(ToVsRun));

    public static VisualStudio.Text.Adornments.ContainerElement ToVsElement(this QuickInfoContainerElement element)
        => new((VisualStudio.Text.Adornments.ContainerElementStyle)element.Style, element.Elements.Select(ToVsElement));

    private static object? ToVsElement(QuickInfoElement value)
        => value switch
        {
            QuickInfoGlyphElement element => element.ToVsElement(),
            QuickInfoContainerElement element => element.ToVsElement(),
            QuickInfoClassifiedTextElement element => element.ToVsElement(),

            _ => throw ExceptionUtilities.UnexpectedValue(value)
        };
}
