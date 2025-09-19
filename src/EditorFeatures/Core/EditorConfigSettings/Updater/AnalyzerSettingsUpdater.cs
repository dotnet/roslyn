// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

internal sealed class AnalyzerSettingsUpdater(Workspace workspace, string editorconfigPath) : SettingsUpdaterBase<AnalyzerSetting, ReportDiagnostic>(workspace, editorconfigPath)
{
    protected override SourceText? GetNewText(SourceText sourceText,
                                              IReadOnlyList<(AnalyzerSetting option, ReportDiagnostic value)> settingsToUpdate,
                                              CancellationToken token)
        => SettingsUpdateHelper.TryUpdateAnalyzerConfigDocument(sourceText, EditorconfigPath, settingsToUpdate);
}
