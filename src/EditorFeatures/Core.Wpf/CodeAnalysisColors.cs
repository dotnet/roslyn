// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal static class CodeAnalysisColors
    {
        // Use VS color keys in order to support theming.
        public static object SystemCaptionTextColorKey => EnvironmentColors.SystemWindowTextColorKey;
        public static object CheckBoxTextBrushKey => EnvironmentColors.SystemWindowTextBrushKey;
        public static object SystemCaptionTextBrushKey => EnvironmentColors.SystemWindowTextBrushKey;
        public static object BackgroundBrushKey => VsBrushes.CommandBarGradientBeginKey;
        public static object ButtonStyleKey => VsResourceKeys.ButtonStyleKey;
        public static object AccentBarColorKey => EnvironmentColors.FileTabInactiveDocumentBorderEdgeBrushKey;
    }
}
