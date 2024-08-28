// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.CodeAnalysis.Extensions;

internal static class VSEditorLSPExtensions
{
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

    private static object? ToLSPElement(object? value)
        => value switch
        {
            VisualStudio.Core.Imaging.ImageId imageId => ToLSPImageId(imageId),
            VisualStudio.Text.Adornments.ImageElement element => ToLSPImageElement(element),
            VisualStudio.Text.Adornments.ContainerElement element => ToLSPElement(element),
            VisualStudio.Text.Adornments.ClassifiedTextElement element => ToLSPElement(element),
            VisualStudio.Text.Adornments.ClassifiedTextRun run => ToLSPRun(run),
            _ => value,
        };
}
