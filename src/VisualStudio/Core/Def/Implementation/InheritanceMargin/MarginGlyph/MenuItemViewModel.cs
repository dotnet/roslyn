// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    internal abstract class MenuItemViewModel
    {
        /// <summary>
        /// ImageMoniker shown in the menu.
        /// </summary>
        public ImageMoniker ImageMoniker { get; }

        protected MenuItemViewModel(ImageMoniker imageMoniker)
        {
            ImageMoniker = imageMoniker;
        }
    }
}
