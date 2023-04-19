// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InlineRename
{
    [Export(typeof(IDashboardColorUpdater))]
    internal class DashboardColorUpdater : IDashboardColorUpdater
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DashboardColorUpdater()
        {
        }

        public void UpdateColors()
        {
            DashboardColors.SystemCaptionTextColorKey = EnvironmentColors.SystemWindowTextColorKey;
            DashboardColors.SystemCaptionTextBrushKey = EnvironmentColors.SystemWindowTextBrushKey;
            DashboardColors.CheckBoxTextBrushKey = EnvironmentColors.SystemWindowTextBrushKey;
            DashboardColors.BackgroundBrushKey = VsBrushes.CommandBarGradientBeginKey;
            DashboardColors.AccentBarColorKey = EnvironmentColors.FileTabInactiveDocumentBorderEdgeBrushKey;
            DashboardColors.ButtonStyleKey = VsResourceKeys.ButtonStyleKey;
        }
    }
}
