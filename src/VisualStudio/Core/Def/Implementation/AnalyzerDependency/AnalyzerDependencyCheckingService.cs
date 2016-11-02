// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
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
        private static readonly IIgnorableAssemblyList s_systemPrefixList = new IgnorableAssemblyNamePrefixList("System");
        private static readonly IIgnorableAssemblyList s_codeAnalysisPrefixList = new IgnorableAssemblyNamePrefixList("Microsoft.CodeAnalysis");
        private static readonly IIgnorableAssemblyList s_explicitlyIgnoredAssemblyList = new IgnorableAssemblyIdentityList(GetExplicitlyIgnoredAssemblyIdentities());
        private static readonly IIgnorableAssemblyList s_assembliesIgnoredByNameList = new IgnorableAssemblyNameList(ImmutableHashSet.Create("mscorlib"));

        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly HostDiagnosticUpdateSource _updateSource;
        private readonly BindingRedirectionService _bindingRedirectionService;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task<AnalyzerDependencyResults> _task = Task.FromResult(AnalyzerDependencyResults.Empty);
        private ImmutableHashSet<string> _analyzerPaths = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);

        private readonly DiagnosticDescriptor _missingAnalyzerReferenceRule = new DiagnosticDescriptor(
            id: IDEDiagnosticIds.MissingAnalyzerReferenceId,
            title: ServicesVSResources.MissingAnalyzerReference,
            messageFormat: ServicesVSResources.Analyzer_assembly_0_depends_on_1_but_it_was_not_found_Analyzers_may_not_run_correctly_unless_the_missing_assembly_is_added_as_an_analyzer_reference_as_well,
            category: FeaturesResources.Roslyn_HostError,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private readonly DiagnosticDescriptor _analyzerDependencyConflictRule = new DiagnosticDescriptor(
            id: IDEDiagnosticIds.AnalyzerDependencyConflictId,
            title: ServicesVSResources.AnalyzerDependencyConflict,
            messageFormat: ServicesVSResources.Analyzer_assemblies_0_and_1_both_have_identity_2_but_different_contents_Only_one_will_be_loaded_and_analyzers_using_these_assemblies_may_not_run_correctly,
            category: FeaturesResources.Roslyn_HostError,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        [ImportingConstructor]
        public AnalyzerDependencyCheckingService(
            VisualStudioWorkspaceImpl workspace,
            HostDiagnosticUpdateSource updateSource)
        {
            _workspace = workspace;
            _updateSource = updateSource;
            _bindingRedirectionService = new BindingRedirectionService();
        }

        public async void CheckForConflictsAsync()
        {
            AnalyzerDependencyResults results = null;
            try
            {
                results = await GetConflictsAsync().ConfigureAwait(continueOnCapturedContext: true);
            }
            catch
            {
                return;
            }

            if (results == null)
            {
                return;
            }

            var builder = ImmutableArray.CreateBuilder<DiagnosticData>();

            var conflicts = results.Conflicts;
            var missingDependencies = results.MissingDependencies;

            foreach (var project in _workspace.ProjectTracker.ImmutableProjects)
            {
                builder.Clear();

                foreach (var conflict in conflicts)
                {
                    if (project.CurrentProjectAnalyzersContains(conflict.AnalyzerFilePath1) ||
                        project.CurrentProjectAnalyzersContains(conflict.AnalyzerFilePath2))
                    {
                        var messageArguments = new string[] { conflict.AnalyzerFilePath1, conflict.AnalyzerFilePath2, conflict.Identity.ToString() };
                        DiagnosticData diagnostic;
                        if (DiagnosticData.TryCreate(_analyzerDependencyConflictRule, messageArguments, project.Id, _workspace, out diagnostic))
                        {
                            builder.Add(diagnostic);
                        }
                    }
                }

                foreach (var missingDependency in missingDependencies)
                {
                    if (project.CurrentProjectAnalyzersContains(missingDependency.AnalyzerPath))
                    {
                        var messageArguments = new string[] { missingDependency.AnalyzerPath, missingDependency.DependencyIdentity.ToString() };
                        DiagnosticData diagnostic;
                        if (DiagnosticData.TryCreate(_missingAnalyzerReferenceRule, messageArguments, project.Id, _workspace, out diagnostic))
                        {
                            builder.Add(diagnostic);
                        }
                    }
                }

                _updateSource.UpdateDiagnosticsForProject(project.Id, s_dependencyConflictErrorId, builder.ToImmutable());
            }

            foreach (var conflict in conflicts)
            {
                LogConflict(conflict);
            }

            foreach (var missingDependency in missingDependencies)
            {
                LogMissingDependency(missingDependency);
            }
        }

        private void LogConflict(AnalyzerDependencyConflict conflict)
        {
            Logger.Log(
                FunctionId.AnalyzerDependencyCheckingService_LogConflict,
                KeyValueLogMessage.Create(m =>
                {
                    m["Identity"] = conflict.Identity.ToString();
                    m["Analyzer1"] = conflict.AnalyzerFilePath1;
                    m["Analyzer2"] = conflict.AnalyzerFilePath2;
                }));
        }

        private void LogMissingDependency(MissingAnalyzerDependency missingDependency)
        {
            Logger.Log(
                FunctionId.AnalyzerDependencyCheckingService_LogMissingDependency,
                KeyValueLogMessage.Create(m =>
                {
                    m["Analyzer"] = missingDependency.AnalyzerPath;
                    m["Identity"] = missingDependency.DependencyIdentity;
                }));
        }

        private Task<AnalyzerDependencyResults> GetConflictsAsync()
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
                IEnumerable<AssemblyIdentity> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => AssemblyIdentity.FromAssemblyDefinition(assembly));
                IgnorableAssemblyIdentityList loadedAssembliesList = new IgnorableAssemblyIdentityList(loadedAssemblies);

                IIgnorableAssemblyList[] ignorableAssemblyLists = new[] { s_systemPrefixList, s_codeAnalysisPrefixList, s_explicitlyIgnoredAssemblyList, s_assembliesIgnoredByNameList, loadedAssembliesList };
                return new AnalyzerDependencyChecker(currentAnalyzerPaths, ignorableAssemblyLists, _bindingRedirectionService).Run(_cancellationTokenSource.Token);
            },
            TaskScheduler.Default);

            return _task;
        }

        private static IEnumerable<AssemblyIdentity> GetExplicitlyIgnoredAssemblyIdentities()
        {
            // Microsoft.VisualBasic.dll
            var list = new List<AssemblyIdentity>();
            AddAssemblyIdentity(list, "Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            AddAssemblyIdentity(list, "Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            return list;
        }

        private static void AddAssemblyIdentity(List<AssemblyIdentity> list, string dllName)
        {
            AssemblyIdentity identity;
            if (!AssemblyIdentity.TryParseDisplayName(dllName, out identity))
            {
                return;
            }

            list.Add(identity);
        }

        private class BindingRedirectionService : IBindingRedirectionService
        {
            public AssemblyIdentity ApplyBindingRedirects(AssemblyIdentity originalIdentity)
            {
                string redirectedAssemblyName = AppDomain.CurrentDomain.ApplyPolicy(originalIdentity.ToString());

                AssemblyIdentity redirectedAssemblyIdentity;
                if (AssemblyIdentity.TryParseDisplayName(redirectedAssemblyName, out redirectedAssemblyIdentity))
                {
                    return redirectedAssemblyIdentity;
                }

                return originalIdentity;
            }
        }
    }
}
