// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Core.Imaging;

namespace Roslyn.Text.Adornments;

internal sealed class ImageElement
{
    public static readonly ImageElement Empty = new(default, string.Empty);

    public ImageId ImageId { get; }
    public string? AutomationName { get; }

    public ImageElement(ImageId imageId) : this(imageId, null)
    {
    }

    public ImageElement(ImageId imageId, string? automationName)
    {
        ImageId = imageId;
        AutomationName = automationName;
    }
}
