// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.Tags
{
    /// <summary>
    /// Extensibility point for hosts to display <see cref="ImageMoniker"/>s for items with Tags.
    /// </summary>
    public interface IImageMonikerService
    {
        bool TryGetImageMoniker(ImmutableArray<string> tags, out ImageMoniker imageMoniker);
    }
}
