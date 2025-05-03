// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal static class InlineRenameColors
{
    // In the Dashboard XAML, we bind to these keys to provide the correct color resources.
    // The default values of these fields match the names of the keys in DashboardColors.xaml,
    // but in Visual Studio we will change these keys (since they are settable) to the keys for
    // the actual Visual Studio resource keys.
    //
    // Each entry here should have a corresponding entry in InlineRenameColors.xaml to specify the
    // default color.

    public static object SystemCaptionTextColorKey { get; set; } = "SystemCaptionTextColor";
    public static object SystemCaptionTextBrushKey { get; set; } = "SystemCaptionTextBrush";
    public static object CheckBoxTextBrushKey { get; set; } = "CheckBoxTextBrush";
    public static object BackgroundBrushKey { get; set; } = "BackgroundBrush";
    public static object AccentBarColorKey { get; set; } = "AccentBarBrush";
    public static object ButtonStyleKey { get; set; } = "ButtonStyle";
    public static object ButtonBorderBrush { get; set; } = "ButtonBorderBrush";
    public static object GrayTextKey { get; set; } = "GrayTextBrush";
    public static object TextBoxTextBrushKey { get; set; } = "TextBoxTextBrushKey";
    public static object TextBoxBackgroundBrushKey { get; set; } = "TextBoxBackgroundBrushKey";
    public static object TextBoxBorderBrushKey { get; set; } = "TextBoxBackgroundBrushKey";
}
