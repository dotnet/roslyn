// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal static class CodeAnalysisColors
    {
        private static object s_systemCaptionTextColorKey = "SystemCaptionTextColor";
        private static object s_checkBoxTextBrushKey = "CheckboxTextBrush";
        private static object s_systemCaptionTextBrushKey = "SystemCaptionTextBrush";
        private static object s_backgroundBrushKey = "BackgroundBrush";
        private static object s_buttonStyleKey = "ButtonStyle";
        private static object s_accentBarColorKey = "AccentBarBrush";

        public static object SystemCaptionTextColorKey
        {
            get
            {
                return s_systemCaptionTextColorKey;
            }

            set
            {
                s_systemCaptionTextColorKey = value;
            }
        }

        public static object SystemCaptionTextBrushKey
        {
            get
            {
                return s_systemCaptionTextBrushKey;
            }

            set
            {
                s_systemCaptionTextBrushKey = value;
            }
        }

        public static object CheckBoxTextBrushKey
        {
            get
            {
                return s_checkBoxTextBrushKey;
            }

            set
            {
                s_checkBoxTextBrushKey = value;
            }
        }

        public static object BackgroundBrushKey
        {
            get
            {
                return s_backgroundBrushKey;
            }

            set
            {
                s_backgroundBrushKey = value;
            }
        }

        public static object ButtonStyleKey
        {
            get
            {
                return s_buttonStyleKey;
            }

            set
            {
                s_buttonStyleKey = value;
            }
        }

        public static object AccentBarColorKey
        {
            get
            {
                return s_accentBarColorKey;
            }

            set
            {
                s_accentBarColorKey = value;
            }
        }
    }
}
