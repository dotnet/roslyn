// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;

/// <summary>
/// The view model used for the header of TargetMenuItemViewModel.
/// e.g.
/// 'I↓ Implementing members'
///       Method 'Bar'
/// </summary>
internal class HeaderMenuItemViewModel : MenuItemViewModel
{
    public HeaderMenuItemViewModel(string displayContent, ImageMoniker imageMoniker)
        : base(displayContent, imageMoniker)
    {
    }
}
