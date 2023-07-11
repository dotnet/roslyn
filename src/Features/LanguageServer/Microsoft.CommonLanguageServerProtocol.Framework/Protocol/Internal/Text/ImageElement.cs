// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Core.Imaging;

namespace Roslyn.Text.Adornments
{
    public class ImageElement
    {
        public static readonly ImageElement Empty = new ImageElement(default(ImageId), string.Empty);

        public ImageId ImageId { get; }

        public string AutomationName { get; }

        public ImageElement(ImageId imageId)
        {
            ImageId = imageId;
        }

        public ImageElement(ImageId imageId, string automationName)
            : this(imageId)
        {
            AutomationName = automationName ?? throw new ArgumentNullException("automationName");
        }
    }
}