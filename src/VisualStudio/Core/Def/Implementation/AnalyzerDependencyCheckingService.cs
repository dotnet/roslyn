// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [Export(typeof(AnalyzerDependencyCheckingService))]
    internal sealed class AnalyzerDependencyCheckingService
    {
        private static readonly object s_dependencyConflictErrorId = new object();

        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly HostDiagnosticUpdateSource _updateSource;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task<ImmutableArray<AnalyzerDependencyConflict>> _task = Task.FromResult(ImmutableArray<AnalyzerDependencyConflict>.Empty);
        private ImmutableHashSet<string> _analyzerPaths = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);

        [ImportingConstructor]
        public AnalyzerDependencyCheckingService(
            VisualStudioWorkspaceImpl workspace,
            HostDiagnosticUpdateSource updateSource)
        {
            _workspace = workspace;
            _updateSource = updateSource;
        }

        public async void CheckForConflictsAsync()
        {
            try
            {
                ImmutableArray<AnalyzerDependencyConflict> conflicts = await GetConflictsAsync().ConfigureAwait(continueOnCapturedContext: true);

                var builder = ImmutableArray.CreateBuilder<DiagnosticData>();

                foreach (var project in _workspace.ProjectTracker.Projects)
                {
                    builder.Clear();

                    foreach (var conflict in conflicts)
                    {
                        if (project.CurrentProjectAnalyzersContains(conflict.AnalyzerFilePath1) ||
                            project.CurrentProjectAnalyzersContains(conflict.AnalyzerFilePath2))
                        {
                            builder.Add(CreateDiagnostic(project.Id, conflict));
                        }
                    }

                    _updateSource.UpdateDiagnosticsForProject(project.Id, s_dependencyConflictErrorId, builder.ToImmutable());
                }

                foreach (var conflict in conflicts)
                {
                    LogConflict(conflict);
                }
            }
            catch (OperationCanceledException) { }
        }

        private void LogConflict(AnalyzerDependencyConflict conflict)
        {
            Logger.Log(
                FunctionId.AnalyzerDependencyCheckingService_CheckForConflictsAsync,
                KeyValueLogMessage.Create(m =>
                {
                    m["Dependency1"] = Path.GetFileName(conflict.DependencyFilePath1);
                    m["Dependency2"] = Path.GetFileName(conflict.DependencyFilePath2);
                    m["Analyzer1"] = Path.GetFileName(conflict.AnalyzerFilePath1);
                    m["Analyzer2"] = Path.GetFileName(conflict.AnalyzerFilePath2);
                }));
        }

        private DiagnosticData CreateDiagnostic(ProjectId projectId, AnalyzerDependencyConflict conflict)
        {
            string id = ServicesVSResources.WRN_AnalyzerDependencyConflictId;
            string category = ServicesVSResources.ErrorCategory;
            string message = string.Format(
                ServicesVSResources.WRN_AnalyzerDependencyConflictMessage,
                conflict.DependencyFilePath1,
                Path.GetFileNameWithoutExtension(conflict.AnalyzerFilePath1),
                conflict.DependencyFilePath2,
                Path.GetFileNameWithoutExtension(conflict.AnalyzerFilePath2));

            DiagnosticData data = new DiagnosticData(
                id,
                category,
                message,
                ServicesVSResources.WRN_AnalyzerDependencyConflictMessage,
                severity: DiagnosticSeverity.Warning,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                warningLevel: 0,
                customTags: ImmutableArray<string>.Empty,
                properties: ImmutableDictionary<string, string>.Empty,
                workspace: _workspace,
                projectId: projectId);

            return data;
        }

        private Task<ImmutableArray<AnalyzerDependencyConflict>> GetConflictsAsync()
        {
            ImmutableHashSet<string> currentAnalyzerPaths = _workspace.CurrentSolution
                .Projects
                .SelectMany(p => p.AnalyzerReferences)
                .OfType<AnalyzerFileReference>()
                .Select(a => a.FullPath)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

            if (currentAnalyzerPaths.SetEquals(_analyzerPaths))
            {
                return _task;
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            _analyzerPaths = currentAnalyzerPaths;

            _task = _task.SafeContinueWith(_ =>
            {
                return new AnalyzerDependencyChecker(currentAnalyzerPaths).Run(_cancellationTokenSource.Token);
            },
            TaskScheduler.Default);

            return _task;
        }
    }
}
