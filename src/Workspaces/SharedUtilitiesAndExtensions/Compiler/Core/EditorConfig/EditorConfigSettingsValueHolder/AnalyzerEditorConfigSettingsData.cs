// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    internal partial class EditorConfigSettingsData
    {
        private static readonly BidirectionalMap<string, DiagnosticSeverity> AnalyzerSettingMap =
            new(new[]
            {
                KeyValuePairUtil.Create("suggestion", DiagnosticSeverity.Info),
                KeyValuePairUtil.Create("warning", DiagnosticSeverity.Warning),
                KeyValuePairUtil.Create("silent", DiagnosticSeverity.Hidden),
                KeyValuePairUtil.Create("error", DiagnosticSeverity.Error),
            });
        public static EditorConfigData<DiagnosticSeverity> AnalyzerSetting = new AnalyzerEditorConfigData("dotnet_diagnostic.Id.severity",
                                                                                                          AnalyzerSettingMap);
    }
}
