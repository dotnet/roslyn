// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Extensions
{
    internal static class LSPExtensions
    {
        public static Roslyn.Core.Imaging.ImageId ToLSPImageId(this VisualStudio.Core.Imaging.ImageId imageId)
            => new Roslyn.Core.Imaging.ImageId(imageId.Guid, imageId.Id);

        public static Roslyn.Text.Adornments.ImageElement ToLSPImageElement(this VisualStudio.Text.Adornments.ImageElement imageElement)
            => new Roslyn.Text.Adornments.ImageElement(imageElement.ImageId.ToLSPImageId(), imageElement.AutomationName);

        public static Roslyn.Text.Adornments.ClassifiedTextRun ToLSPRun(this VisualStudio.Text.Adornments.ClassifiedTextRun run)
            => new Roslyn.Text.Adornments.ClassifiedTextRun(run.ClassificationTypeName, run.Text, (Roslyn.Text.Adornments.ClassifiedTextRunStyle)run.Style, run.MarkerTagType, run.NavigationAction, run.Tooltip);

        public static Roslyn.Text.Adornments.ClassifiedTextElement ToLSPElement(this VisualStudio.Text.Adornments.ClassifiedTextElement element)
            => new Roslyn.Text.Adornments.ClassifiedTextElement(element.Runs.Select(r => r.ToLSPRun()));

        public static Roslyn.Text.Adornments.ContainerElement ToLSPElement(this VisualStudio.Text.Adornments.ContainerElement element)
            => new Roslyn.Text.Adornments.ContainerElement((Roslyn.Text.Adornments.ContainerElementStyle)element.Style, element.Elements.Select(e => ToLSPElement(e)));

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
}
