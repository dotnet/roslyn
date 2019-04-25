// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
    internal static class VisualStudioWorkspaceImplExtensions
    {
        // We're mucking around creating native objects.  They need to live around as long as the 
        // hierarchy we're getting them for.  To do this, we attach them to the hierarchy with a
        // conditional weak table.
        private static readonly ConditionalWeakTable<IVsHierarchy, Dictionary<uint, IImageHandle>> s_hierarchyToItemIdToImageHandle =
            new ConditionalWeakTable<IVsHierarchy, Dictionary<uint, IImageHandle>>();

        private static readonly ConditionalWeakTable<IVsHierarchy, Dictionary<uint, IImageHandle>>.CreateValueCallback s_createValue =
            _ => new Dictionary<uint, IImageHandle>();

        private static bool TryGetImageListAndIndex(this IVsHierarchy hierarchy, IVsImageService2 imageService, uint itemId, out IntPtr imageList, out ushort index)
        {
            var itemIdToImageHandle = s_hierarchyToItemIdToImageHandle.GetValue(hierarchy, s_createValue);

            // Get the actual image moniker that the vs hierarchy is using to in solution explorer.
            var imageMoniker = imageService.GetImageMonikerForHierarchyItem(hierarchy, itemId, (int)__VSHIERARCHYIMAGEASPECT.HIA_Icon);
            var monikerImageList = new VsImageMonikerImageList(imageMoniker);

            // Get an image handle to this image moniker, and keep it around for the lifetime of the
            // hierarchy itself.
            var imageHandle = imageService.AddCustomImageList(monikerImageList);
            itemIdToImageHandle[itemId] = imageHandle;

            // Now, we want to get an HIMAGELIST ptr for that image.  
            var uiObject = imageService.GetImage(imageHandle.Moniker, new ImageAttributes
            {
                StructSize = Marshal.SizeOf(typeof(ImageAttributes)),
                Dpi = 96,
                LogicalWidth = 16,
                LogicalHeight = 16,
                ImageType = (uint)_UIImageType.IT_ImageList,
                Format = (uint)_UIDataFormat.DF_Win32,
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
            });

            if (uiObject != null)
            {
                if (Microsoft.Internal.VisualStudio.PlatformUI.Utilities.GetObjectData(uiObject) is IVsUIWin32ImageList imageListData)
                {
                    if (ErrorHandler.Succeeded(imageListData.GetHIMAGELIST(out var imageListInt)))
                    {
                        imageList = (IntPtr)imageListInt;
                        index = 0;
                        return true;
                    }
                }
            }

            imageList = default;
            index = 0;
            return false;
        }

        public static bool TryGetImageListAndIndex(this VisualStudioWorkspaceImpl workspace, IVsImageService2 imageService, DocumentId id, out IntPtr imageList, out ushort index)
        {
            var hierarchy = workspace.GetHierarchy(id.ProjectId);
            var document = workspace.CurrentSolution.GetDocument(id);
            if (hierarchy != null)
            {
                var itemId = hierarchy.TryGetItemId(document.FilePath);
                return TryGetImageListAndIndex(hierarchy, imageService, itemId, out imageList, out index);
            }

            imageList = default;
            index = 0;
            return false;
        }

        private class VsImageMonikerImageList : IVsImageMonikerImageList
        {
            private readonly ImageMoniker _imageMoniker;

            public VsImageMonikerImageList(ImageMoniker imageMoniker)
            {
                _imageMoniker = imageMoniker;
            }

            public int ImageCount
            {
                get
                {
                    return 1;
                }
            }

            public void GetImageMonikers(int firstImageIndex, int imageMonikerCount, ImageMoniker[] imageMonikers)
            {
                if (firstImageIndex == 0 && imageMonikerCount == 1)
                {
                    imageMonikers[0] = _imageMoniker;
                }
            }
        }
    }
}
