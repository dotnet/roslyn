using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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

    [ExportWorkspaceService(typeof(IImageMonikerService), layer: ServiceLayer.Host), Shared]
    internal class VisualStudioImageMonikerService : ForegroundThreadAffinitizedObject, IImageMonikerService
    {
        private readonly IVsImageService2 _imageService;

        // We have to keep the image handles around to keep the compound glyph alive.
        private readonly List<CompositeImage> _compositeImages = new List<CompositeImage>();

        [ImportingConstructor]
        public VisualStudioImageMonikerService(SVsServiceProvider serviceProvider)
        {
            _imageService = (IVsImageService2)serviceProvider.GetService(typeof(SVsImageService));
        }

        public ImageMoniker GetImageMoniker(Glyph glyph)
        {
            this.AssertIsForeground();

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
