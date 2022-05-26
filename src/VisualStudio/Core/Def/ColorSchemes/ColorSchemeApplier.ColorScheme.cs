// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        /// <summary>
        /// A ColorSchemeName represents a style to be applied to VS themes in 
        /// order to emphasize some aspect of the source code. For instance
        /// C++ has an 'Enhanced (Globals vs. Members)' scheme to emphasize
        /// a distinction between where identifiers are declared.
        /// </summary>
        private class ColorScheme
        {
            public ImmutableArray<ColorTheme> Themes { get; }

            public ColorScheme(ImmutableArray<ColorTheme> themes)
                => Themes = themes;
        }

        /// <summary>
        /// A ColorTheme contains a scheme's colors for a particular VS theme.
        /// </summary>
        private class ColorTheme
        {
            public string Name { get; }
            public Guid Guid { get; }
            public ColorCategory Category { get; }

            public ColorTheme(string name, Guid guid, ColorCategory category)
            {
                Name = name;
                Guid = guid;
                Category = category;
            }
        }

        private class ColorCategory
        {
            public string Name { get; }
            public Guid Guid { get; }
            public ImmutableArray<ColorItem> Colors { get; }

            public ColorCategory(string name, Guid guid, ImmutableArray<ColorItem> colors)
            {
                Name = name;
                Guid = guid;
                Colors = colors;
            }
        }

        private class ColorItem
        {
            public string Name { get; }
            public __VSCOLORTYPE BackgroundType { get; }
            public uint? Background { get; }
            public __VSCOLORTYPE ForegroundType { get; }
            public uint? Foreground { get; }

            public ColorItem(string name, __VSCOLORTYPE backgroundType, uint? background, __VSCOLORTYPE foregroundType, uint? foreground)
            {
                Name = name;

                Debug.Assert(backgroundType == __VSCOLORTYPE.CT_INVALID || background.HasValue);
                BackgroundType = backgroundType;
                Background = background;

                Debug.Assert(foregroundType == __VSCOLORTYPE.CT_INVALID || foreground.HasValue);
                ForegroundType = foregroundType;
                Foreground = foreground;
            }
        }
    }
}
