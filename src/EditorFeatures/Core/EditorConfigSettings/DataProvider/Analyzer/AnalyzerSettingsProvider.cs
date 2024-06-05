// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.EditorConfig;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using RoslynEnumerableExtensions = Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Extensions.EnumerableExtensions;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.Analyzer;

internal sealed class AnalyzerSettingsProvider : SettingsProviderBase<AnalyzerSetting, AnalyzerSettingsUpdater, AnalyzerSetting, ReportDiagnostic>
{
    private readonly IDiagnosticAnalyzerService _analyzerService;

    public AnalyzerSettingsProvider(string fileName, AnalyzerSettingsUpdater settingsUpdater, Workspace workspace, IDiagnosticAnalyzerService analyzerService, IGlobalOptionService globalOptions)
        : base(fileName, settingsUpdater, workspace, globalOptions)
    {
        _analyzerService = analyzerService;
        Update();
    }

    protected override void UpdateOptions(TieredAnalyzerConfigOptions options, ImmutableArray<Project> projectsInScope)
    {
        var analyzerReferences = RoslynEnumerableExtensions.DistinctBy(projectsInScope.SelectMany(p => p.AnalyzerReferences), a => a.Id).ToImmutableArray();
        foreach (var analyzerReference in analyzerReferences)
        {
            var configSettings = GetSettings(analyzerReference, options.EditorConfigOptions);
            AddRange(configSettings);
        }
    }

    private IEnumerable<AnalyzerSetting> GetSettings(AnalyzerReference analyzerReference, AnalyzerConfigOptions editorConfigOptions)
    {
        IEnumerable<DiagnosticAnalyzer> csharpAnalyzers = analyzerReference.GetAnalyzers(LanguageNames.CSharp);
        IEnumerable<DiagnosticAnalyzer> visualBasicAnalyzers = analyzerReference.GetAnalyzers(LanguageNames.VisualBasic);
        var dotnetAnalyzers = csharpAnalyzers.Intersect(visualBasicAnalyzers, DiagnosticAnalyzerComparer.Instance);
        csharpAnalyzers = csharpAnalyzers.Except(dotnetAnalyzers, DiagnosticAnalyzerComparer.Instance);
        visualBasicAnalyzers = visualBasicAnalyzers.Except(dotnetAnalyzers, DiagnosticAnalyzerComparer.Instance);

        var csharpSettings = ToAnalyzerSetting(csharpAnalyzers, Language.CSharp);
        var csharpAndVisualBasicSettings = csharpSettings.Concat(ToAnalyzerSetting(visualBasicAnalyzers, Language.VisualBasic));
        return csharpAndVisualBasicSettings.Concat(ToAnalyzerSetting(dotnetAnalyzers, Language.CSharp | Language.VisualBasic));

        IEnumerable<AnalyzerSetting> ToAnalyzerSetting(IEnumerable<DiagnosticAnalyzer> analyzers,
                                                               Language language)
        {
            return analyzers
                .SelectMany(a => _analyzerService.AnalyzerInfoCache.GetDiagnosticDescriptors(a))
                .GroupBy(d => d.Id)
                .OrderBy(g => g.Key, StringComparer.CurrentCulture)
                .Select(g =>
                {
                    var selectedDiagnostic = g.First();
                    var isEditorconfig = selectedDiagnostic.IsDefinedInEditorConfig(editorConfigOptions);
                    var settingLocation = new SettingLocation(isEditorconfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, FileName);
                    var severity = selectedDiagnostic.GetEffectiveSeverity(editorConfigOptions);
                    return new AnalyzerSetting(selectedDiagnostic, severity, SettingsUpdater, language, settingLocation);
                });
        }
    }

    private class DiagnosticAnalyzerComparer : IEqualityComparer<DiagnosticAnalyzer>
    {
        public static readonly DiagnosticAnalyzerComparer Instance = new();

        public bool Equals(DiagnosticAnalyzer? x, DiagnosticAnalyzer? y)
        {
            if (x is null && y is null)
                return true;

            if (x is null || y is null)
                return false;

            return x.GetAnalyzerIdAndVersion().GetHashCode() == y.GetAnalyzerIdAndVersion().GetHashCode();
        }

        public int GetHashCode(DiagnosticAnalyzer obj) => obj.GetAnalyzerIdAndVersion().GetHashCode();
    }
}
