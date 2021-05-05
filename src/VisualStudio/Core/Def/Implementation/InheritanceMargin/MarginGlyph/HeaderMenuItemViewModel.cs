// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    /// <summary>
    /// The view model used for the header of TargetMenuItemViewModel.
    /// It is used when the context menu contains targets having multiple inheritance relationship.
    /// In such case, this would be shown as a header for a group of targets.
    /// e.g.
    /// 'I↓ Implemented members'
    ///       Method 'Bar'
    /// 'I↑ Implementing members'
    ///       Method 'Foo'
    /// </summary>
    internal class HeaderMenuItemViewModel : InheritanceMenuItemViewModel
    {
        public HeaderMenuItemViewModel(string displayContent, ImageMoniker imageMoniker, string automationName)
            : base(displayContent, imageMoniker, automationName)
        {
        }
    }
}
