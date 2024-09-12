// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tags;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Shared;

internal readonly struct CompositeImage
{
    public readonly ImmutableArray<ImageCompositionLayer> Layers;
    public readonly IImageHandle ImageHandle;

    public CompositeImage(ImmutableArray<ImageCompositionLayer> layers, IImageHandle imageHandle)
    {
        this.Layers = layers;
        this.ImageHandle = imageHandle;
    }
}

[ExportImageIdService(Name = Name)]
[Order(Before = DefaultImageIdService.Name)]
internal class VisualStudioImageIdService : ForegroundThreadAffinitizedObject, IImageIdService
{
    public const string Name = nameof(VisualStudioImageIdService);

    private readonly IVsImageService2 _imageService;

    // We have to keep the image handles around to keep the compound glyph alive.
    private readonly List<CompositeImage> _compositeImages = [];

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioImageIdService(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
        : base(threadingContext)
    {
        _imageService = (IVsImageService2)serviceProvider.GetService(typeof(SVsImageService));
    }

    public bool TryGetImageId(ImmutableArray<string> tags, out ImageId imageId)
    {
        this.AssertIsForeground();

        imageId = GetImageId(tags);
        return imageId != default;
    }

    private ImageId GetImageId(ImmutableArray<string> tags)
    {
        var glyph = tags.GetFirstGlyph();
        switch (glyph)
        {
            case Glyph.AddReference:
                return GetCompositedImageId(
                    CreateLayer(Glyph.Reference.GetImageMoniker(), virtualXOffset: 1, virtualYOffset: 2),
                    CreateLayer(KnownMonikers.PendingAddNode, virtualWidth: 7, virtualXOffset: -1, virtualYOffset: -2));
        }

        return glyph.GetImageId();
    }

    private static ImageCompositionLayer CreateLayer(
        ImageMoniker imageMoniker,
        int virtualWidth = 16,
        int virtualYOffset = 0,
        int virtualXOffset = 0)
    {
        return new ImageCompositionLayer
        {
            VirtualWidth = virtualWidth,
            VirtualHeight = 16,
            ImageMoniker = imageMoniker,
            HorizontalAlignment = (uint)_UIImageHorizontalAlignment.IHA_Left,
            VerticalAlignment = (uint)_UIImageVerticalAlignment.IVA_Top,
            VirtualXOffset = virtualXOffset,
            VirtualYOffset = virtualYOffset,
        };
    }

    private ImageId GetCompositedImageId(params ImageCompositionLayer[] layers)
    {
        this.AssertIsForeground();

        foreach (var compositeImage in _compositeImages)
        {
            if (compositeImage.Layers.SequenceEqual(layers))
            {
                return compositeImage.ImageHandle.Moniker.ToImageId();
            }
        }

        var imageHandle = _imageService.AddCustomCompositeImage(
                virtualWidth: 16, virtualHeight: 16,
                layerCount: layers.Length, layers: layers);

        _compositeImages.Add(new CompositeImage(layers.AsImmutableOrEmpty(), imageHandle));

        var moniker = imageHandle.Moniker;
        return moniker.ToImageId();
    }
}
