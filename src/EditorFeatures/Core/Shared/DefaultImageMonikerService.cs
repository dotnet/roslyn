// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.Shared
{
    [ExportWorkspaceService(typeof(IImageMonikerService)), Shared]
    internal class DefaultImageMonikerService : IImageMonikerService
    {
        public ImageMoniker GetImageMoniker(Glyph glyph)
        {
            // We can't do the compositing of these glyphs at the editor layer.  So just map them
            // to the non-add versions.
            switch (glyph)
            {
                case Glyph.AddReference:
                    glyph = Glyph.Reference;
                    break;
            }

            return glyph.GetImageMoniker();
        }
    }
}
