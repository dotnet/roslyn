// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Media;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal static class HighContrastChecker
    {
        private static readonly Color s_white = Color.FromRgb(0xFF, 0xFF, 0xFF);
        private static readonly Color s_black = Color.FromRgb(0x00, 0x00, 0x00);
        private static readonly Color s_green = Color.FromRgb(0x00, 0xFF, 0x00);

        public static bool IsHighContrast
        {
            get
            {
                // copied from http://index/#Microsoft.VisualStudio.Platform.VSEditor/Helpers.cs,3db7255be2c777f1

                // If WPF tells us that we're in high-contrast, Go ahead and believe it.
                if (SystemParameters.HighContrast)
                {
                    return true;
                }

                // If WPF doesn't tell us that we're in high-contrast, we still could be.  We'll have to check some colors for
                // ourselves.
                var controlColor = SystemColors.ControlColor;
                var controlTextColor = SystemColors.ControlTextColor;

                // Are we running under High Contrast #1 or High Contrast Black?
                if ((controlColor == s_black) && (controlTextColor == s_white))
                {
                    return true;
                }

                // Are we running under High Contrast White?
                if ((controlColor == s_white) && (controlTextColor == s_black))
                {
                    return true;
                }

                // Are we running under High Contrast #2?
                if ((controlColor == s_black) && (controlTextColor == s_green))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
