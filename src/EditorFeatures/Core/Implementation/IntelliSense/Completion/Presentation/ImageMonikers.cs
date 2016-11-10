// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    internal static class ImageMonikers
    {
        public static ImageMoniker GetImageMoniker(ImmutableArray<string> tags)
        {
            return tags.GetGlyph().GetImageMoniker();
        }
    }
}