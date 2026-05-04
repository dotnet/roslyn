// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.EditorConfig;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using RoslynEnumerableExtensions = Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Extensions.EnumerableExtensions;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.Analyzer;

internal sealed class AnalyzerSettingsProvider
    : SettingsProviderBase<AnalyzerSetting, AnalyzerSettingsUpdater, AnalyzerSetting, ReportDiagnostic>,
    // So we can unify descriptors across VB/C# to create single settings that apply to both languages.
    IEqualityComparer<DiagnosticDescriptor>
{
    public AnalyzerSettingsProvider(
        IThreadingContext threadingContext,
        string fileName,
        AnalyzerSettingsUpdater settingsUpdater,
        Workspace workspace,
        IGlobalOptionService optionService)
        : base(threadingContext, fileName, settingsUpdater, workspace, optionService)
    {
        Update();
    }

    protected override async Task UpdateOptionsAsync(
        TieredAnalyzerConfigOptions options, ImmutableArray<Project> projectsInScope, CancellationToken cancellationToken)
    {
        var analyzerReferences = RoslynEnumerableExtensions.DistinctBy(projectsInScope.SelectMany(p => p.AnalyzerReferences), a => a.Id).ToImmutableArray();
        using var _ = PooledDictionary<AnalyzerReference, Project>.GetInstance(out var analyzerReferenceToSomeReferencingProject);

        foreach (var project in projectsInScope)
        {
            foreach (var analyzerReference in project.AnalyzerReferences)
                analyzerReferenceToSomeReferencingProject[analyzerReference] = project;
        }

        foreach (var analyzerReference in analyzerReferences)
        {
            var someReferencingProject = analyzerReferenceToSomeReferencingProject[analyzerReference];
            var configSettings = await GetSettingsAsync(
                someReferencingProject, analyzerReference, options.EditorConfigOptions, cancellationToken).ConfigureAwait(false);
            AddRange(configSettings);
        }
    }

    private async Task<ImmutableArray<AnalyzerSetting>> GetSettingsAsync(
        Project someReferencingProject, AnalyzerReference analyzerReference, AnalyzerConfigOptions editorConfigOptions, CancellationToken cancellationToken)
    {
        var solution = someReferencingProject.Solution;
        var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();

        var csharpDescriptors = await service.GetDiagnosticDescriptorsAsync(solution, someReferencingProject.Id, analyzerReference, LanguageNames.CSharp, cancellationToken).ConfigureAwait(false);
        var vbDescriptors = await service.GetDiagnosticDescriptorsAsync(solution, someReferencingProject.Id, analyzerReference, LanguageNames.VisualBasic, cancellationToken).ConfigureAwait(false);

        var dotnetDescriptors = csharpDescriptors.Intersect(vbDescriptors, this).ToImmutableArray();

        return [
            .. ToAnalyzerSettings(csharpDescriptors.Except(dotnetDescriptors), Language.CSharp),
            .. ToAnalyzerSettings(vbDescriptors.Except(dotnetDescriptors), Language.VisualBasic),
            .. ToAnalyzerSettings(dotnetDescriptors, Language.CSharp | Language.VisualBasic)];

        IEnumerable<AnalyzerSetting> ToAnalyzerSettings(
            IEnumerable<DiagnosticDescriptor> descriptors, Language language)
        {
            return descriptors
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

    bool IEqualityComparer<DiagnosticDescriptor>.Equals(DiagnosticDescriptor x, DiagnosticDescriptor y)
        => x.Id == y.Id;

    int IEqualityComparer<DiagnosticDescriptor>.GetHashCode(DiagnosticDescriptor obj)
        => obj.Id.GetHashCode();
}
