// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InlineRename;

[Export(typeof(IInlineRenameColorUpdater))]
internal class DashboardColorUpdater : IInlineRenameColorUpdater
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DashboardColorUpdater()
    {
    }

    public void UpdateColors()
    {
        InlineRenameColors.SystemCaptionTextColorKey = EnvironmentColors.SystemWindowTextColorKey;
        InlineRenameColors.SystemCaptionTextBrushKey = EnvironmentColors.SystemWindowTextBrushKey;
        InlineRenameColors.CheckBoxTextBrushKey = EnvironmentColors.SystemWindowTextBrushKey;
        InlineRenameColors.BackgroundBrushKey = VsBrushes.CommandBarGradientBeginKey;
        InlineRenameColors.AccentBarColorKey = EnvironmentColors.FileTabInactiveDocumentBorderEdgeBrushKey;
        InlineRenameColors.ButtonStyleKey = VsResourceKeys.ButtonStyleKey;
        InlineRenameColors.GrayTextKey = VsBrushes.GrayTextKey;
        InlineRenameColors.TextBoxBackgroundBrushKey = EnvironmentColors.SearchBoxBackgroundBrushKey;
        InlineRenameColors.TextBoxTextBrushKey = EnvironmentColors.SystemWindowTextBrushKey;
        InlineRenameColors.TextBoxBorderBrushKey = EnvironmentColors.SearchBoxBorderBrushKey;
    }
}
