// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;

namespace Microsoft.CodeAnalysis.Extensions;

internal static class VSEditorLSPExtensions
{
    public static Roslyn.Core.Imaging.ImageId ToLSPImageId(this Glyph glyph)
    {
        var (guid, id) = glyph.GetVsImageData();

        return new(guid, id);
    }

    public static Roslyn.Core.Imaging.ImageId ToLSPImageId(this VisualStudio.Core.Imaging.ImageId imageId)
        => new(imageId.Guid, imageId.Id);

    public static Roslyn.Text.Adornments.ImageElement ToLSPImageElement(this VisualStudio.Text.Adornments.ImageElement imageElement)
        => new(imageElement.ImageId.ToLSPImageId(), imageElement.AutomationName);

    public static Roslyn.Text.Adornments.ClassifiedTextRun ToLSPRun(this VisualStudio.Text.Adornments.ClassifiedTextRun run)
        => new(run.ClassificationTypeName, run.Text, (Roslyn.Text.Adornments.ClassifiedTextRunStyle)run.Style, run.MarkerTagType, run.NavigationAction, run.Tooltip);

    public static Roslyn.Text.Adornments.ClassifiedTextElement ToLSPElement(this VisualStudio.Text.Adornments.ClassifiedTextElement element)
        => new(element.Runs.Select(r => r.ToLSPRun()));

    public static Roslyn.Text.Adornments.ContainerElement ToLSPElement(this VisualStudio.Text.Adornments.ContainerElement element)
        => new((Roslyn.Text.Adornments.ContainerElementStyle)element.Style, element.Elements.Select(ToLSPElement));

    public static Roslyn.Text.Adornments.ImageElement ToLSPElement(this QuickInfoGlyphElement element)
        => new(element.Glyph.ToLSPImageId());

    public static Roslyn.Text.Adornments.ClassifiedTextRun ToLSPRun(this QuickInfoClassifiedTextRun run)
        => new(run.ClassificationTypeName, run.Text, (Roslyn.Text.Adornments.ClassifiedTextRunStyle)run.Style, markerTagType: null, run.NavigationAction, run.Tooltip);

    public static Roslyn.Text.Adornments.ClassifiedTextElement ToLSPElement(this QuickInfoClassifiedTextElement element)
        => new(element.Runs.Select(ToLSPRun));

    public static Roslyn.Text.Adornments.ContainerElement ToLSPElement(this QuickInfoContainerElement element)
        => new((Roslyn.Text.Adornments.ContainerElementStyle)element.Style, element.Elements.Select(ToLSPElement));

    private static object? ToLSPElement(object? value)
        => value switch
        {
            VisualStudio.Core.Imaging.ImageId imageId => imageId.ToLSPImageId(),
            VisualStudio.Text.Adornments.ImageElement element => element.ToLSPImageElement(),
            VisualStudio.Text.Adornments.ContainerElement element => element.ToLSPElement(),
            VisualStudio.Text.Adornments.ClassifiedTextElement element => element.ToLSPElement(),
            VisualStudio.Text.Adornments.ClassifiedTextRun run => run.ToLSPRun(),

            _ => value,
        };

    private static object? ToLSPElement(QuickInfoElement value)
    {
        return value switch
        {
            QuickInfoGlyphElement element => element.ToLSPElement(),
            QuickInfoContainerElement element => element.ToLSPElement(),
            QuickInfoClassifiedTextElement element => element.ToLSPElement(),

            _ => value
        };
    }
}
