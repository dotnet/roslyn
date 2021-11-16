﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater
{
    internal class AnalyzerSettingsUpdater : SettingsUpdaterBase<AnalyzerSetting, DiagnosticSeverity>
    {
        public AnalyzerSettingsUpdater(Workspace workspace, string editorconfigPath) : base(workspace, editorconfigPath)
        {
        }

        protected override SourceText? GetNewText(SourceText sourceText,
                                                  IReadOnlyList<(AnalyzerSetting option, DiagnosticSeverity value)> settingsToUpdate,
                                                  CancellationToken token)
            => SettingsUpdateHelper.TryUpdateAnalyzerConfigDocument(sourceText, EditorconfigPath, settingsToUpdate);
    }
}
