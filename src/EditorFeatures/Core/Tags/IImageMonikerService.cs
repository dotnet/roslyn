// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
