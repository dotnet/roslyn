// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Imaging.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;

internal abstract class MenuItemViewModel
{
    /// <summary>
    /// Display content for the target.
    /// </summary>
    public string DisplayContent { get; }

    /// <summary>
    /// ImageMoniker shown in the menu.
    /// </summary>
    public ImageMoniker ImageMoniker { get; }

    /// <summary>
    /// AutomationName for the MenuItem.
    /// </summary>
    public string AutomationName { get; }

    public double ScaleFactor { get; }

    protected MenuItemViewModel(string displayContent, ImageMoniker imageMoniker, double scaleFactor)
    {
        ImageMoniker = imageMoniker;
        DisplayContent = displayContent;
        AutomationName = displayContent;
        ScaleFactor = scaleFactor;
    }
}
