// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Core.Imaging;

namespace Microsoft.CodeAnalysis.Editor.Tags;

[ExportImageIdService(Name = Name)]
internal sealed class DefaultImageIdService : IImageIdService
{
    public const string Name = nameof(DefaultImageIdService);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultImageIdService()
    {
    }

    public bool TryGetImageId(ImmutableArray<string> tags, out ImageId imageId)
    {
        var glyph = tags.GetFirstGlyph();

        // We can't do the compositing of these glyphs at the editor layer.  So just map them
        // to the non-add versions.
        switch (glyph)
        {
            case Glyph.AddReference:
                glyph = Glyph.Reference;
                break;
        }

        imageId = glyph.GetImageId();
        return imageId != default;
    }
}
