// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.VisualStudio.Core.Imaging;

namespace Microsoft.CodeAnalysis.Editor.Tags
{
    /// <summary>
    /// Extensibility point for hosts to display <see cref="ImageId"/>s for items with Tags.
    /// </summary>
    internal interface IImageIdService
    {
        bool TryGetImageId(ImmutableArray<string> tags, out ImageId imageId);
    }
}
