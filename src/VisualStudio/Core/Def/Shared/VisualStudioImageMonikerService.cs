// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tags;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Shared
{
    internal struct CompositeImage
    {
        public readonly ImmutableArray<ImageCompositionLayer> Layers;
        public readonly IImageHandle ImageHandle;

        public CompositeImage(ImmutableArray<ImageCompositionLayer> layers, IImageHandle imageHandle)
        {
            this.Layers = layers;
            this.ImageHandle = imageHandle;
        }
    }

    [ExportImageMonikerService(Name = Name)]
    [Order(Before = DefaultImageMonikerService.Name)]
    internal class VisualStudioImageMonikerService : ForegroundThreadAffinitizedObject, IImageMonikerService
    {
        public const string Name = nameof(VisualStudioImageMonikerService);

        private readonly IVsImageService2 _imageService;

        // We have to keep the image handles around to keep the compound glyph alive.
        private readonly List<CompositeImage> _compositeImages = new List<CompositeImage>();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioImageMonikerService(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _imageService = (IVsImageService2)serviceProvider.GetService(typeof(SVsImageService));
        }

        public bool TryGetImageMoniker(ImmutableArray<string> tags, out ImageMoniker imageMoniker)
        {
            this.AssertIsForeground();

            imageMoniker = GetImageMoniker(tags);
            return !imageMoniker.IsNullImage();
        }

        private ImageMoniker GetImageMoniker(ImmutableArray<string> tags)
        {
            var glyph = tags.GetFirstGlyph();
            switch (glyph)
            {
                case Glyph.AddReference:
                    return GetCompositedImageMoniker(
                        CreateLayer(Glyph.Reference.GetImageMoniker(), virtualXOffset: 1, virtualYOffset: 2),
                        CreateLayer(KnownMonikers.PendingAddNode, virtualWidth: 7, virtualXOffset: -1, virtualYOffset: -2));
            }

            return glyph.GetImageMoniker();
        }

        private ImageCompositionLayer CreateLayer(
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

        private ImageMoniker GetCompositedImageMoniker(params ImageCompositionLayer[] layers)
        {
            this.AssertIsForeground();

            foreach (var compositeImage in _compositeImages)
            {
                if (compositeImage.Layers.SequenceEqual(layers))
                {
                    return compositeImage.ImageHandle.Moniker;
                }
            }

            var imageHandle = _imageService.AddCustomCompositeImage(
                    virtualWidth: 16, virtualHeight: 16,
                    layerCount: layers.Length, layers: layers);

            _compositeImages.Add(new CompositeImage(layers.AsImmutableOrEmpty(), imageHandle));

            var moniker = imageHandle.Moniker;
            return moniker;
        }
    }
}
