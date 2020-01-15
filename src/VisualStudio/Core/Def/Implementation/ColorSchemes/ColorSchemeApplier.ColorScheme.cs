// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private class ColorScheme
        {
            public ImmutableArray<ColorTheme> Themes { get; }

            public ColorScheme(ImmutableArray<ColorTheme> themes)
            {
                Themes = themes;
            }
        }

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
            public int BackgroundType { get; }
            public uint? Background { get; }
            public int ForegroundType { get; }
            public uint? Foreground { get; }

            public ColorItem(string name, int backgroundType, uint? background, int foregroundType, uint? foreground)
            {
                Name = name;

                Debug.Assert(backgroundType == (int)__VSCOLORTYPE.CT_INVALID || background.HasValue);
                BackgroundType = backgroundType;
                Background = background;

                Debug.Assert(foregroundType == (int)__VSCOLORTYPE.CT_INVALID || foreground.HasValue);
                ForegroundType = foregroundType;
                Foreground = foreground;
            }
        }
    }
}
