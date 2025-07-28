// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.CodeAnalysis.Extensions;

internal static class VSEditorLSPExtensions
{
    extension(VisualStudio.Core.Imaging.ImageId imageId)
    {
        public Roslyn.Core.Imaging.ImageId ToLSPImageId()
        => new(imageId.Guid, imageId.Id);
    }

    extension(VisualStudio.Text.Adornments.ImageElement imageElement)
    {
        public Roslyn.Text.Adornments.ImageElement ToLSPImageElement()
        => new(imageElement.ImageId.ToLSPImageId(), imageElement.AutomationName);
    }

    extension(VisualStudio.Text.Adornments.ClassifiedTextRun run)
    {
        public Roslyn.Text.Adornments.ClassifiedTextRun ToLSPRun()
        => new(run.ClassificationTypeName, run.Text, (Roslyn.Text.Adornments.ClassifiedTextRunStyle)run.Style, run.MarkerTagType, run.NavigationAction, run.Tooltip);
    }

    extension(VisualStudio.Text.Adornments.ClassifiedTextElement element)
    {
        public Roslyn.Text.Adornments.ClassifiedTextElement ToLSPElement()
        => new(element.Runs.Select(r => r.ToLSPRun()));
    }

    extension(VisualStudio.Text.Adornments.ContainerElement element)
    {
        public Roslyn.Text.Adornments.ContainerElement ToLSPElement()
        => new((Roslyn.Text.Adornments.ContainerElementStyle)element.Style, element.Elements.Select(ToLSPElement));
    }

    private static object ToLSPElement(object value)
        => value switch
        {
            VisualStudio.Core.Imaging.ImageId imageId => imageId.ToLSPImageId(),
            VisualStudio.Text.Adornments.ImageElement element => element.ToLSPImageElement(),
            VisualStudio.Text.Adornments.ContainerElement element => element.ToLSPElement(),
            VisualStudio.Text.Adornments.ClassifiedTextElement element => element.ToLSPElement(),
            VisualStudio.Text.Adornments.ClassifiedTextRun run => run.ToLSPRun(),
            _ => value,
        };
}
