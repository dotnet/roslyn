// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [Export(typeof(AnalyzerDependencyCheckingService))]
    internal sealed class AnalyzerDependencyCheckingService
    {
        /// <summary>
        /// Object given as key for <see cref="HostDiagnosticUpdateSource.UpdateAndAddDiagnosticsArgsForProject"/>.
        /// </summary>
        private static readonly object s_dependencyConflictErrorId = new();
        private static readonly IIgnorableAssemblyList s_systemPrefixList = new IgnorableAssemblyNamePrefixList("System");
        private static readonly IIgnorableAssemblyList s_codeAnalysisPrefixList = new IgnorableAssemblyNamePrefixList("Microsoft.CodeAnalysis");
        private static readonly IIgnorableAssemblyList s_explicitlyIgnoredAssemblyList = new IgnorableAssemblyIdentityList(GetExplicitlyIgnoredAssemblyIdentities());
        private static readonly IIgnorableAssemblyList s_assembliesIgnoredByNameList = new IgnorableAssemblyNameList(ImmutableHashSet.Create("mscorlib"));
        private static readonly IBindingRedirectionService s_bindingRedirectionService = new BindingRedirectionService();

        private readonly VisualStudioWorkspace _workspace;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;

        /// <summary>
        /// Object given to synchronize access to the mutable fields in this class.
        /// </summary>
        private readonly object _gate = new();
        private CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// The most recently started analysis task; if we start a new analysis we will cancel the previous one and start the next one
        /// as a continuation of this task to ensure any notification to <see cref="_hostDiagnosticUpdateSource"/> was done first.
        /// </summary>
        private Task _task = Task.CompletedTask;
        private ImmutableHashSet<string> _previousAnalyzerPaths = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly DiagnosticDescriptor s_missingAnalyzerReferenceRule = new(
            id: IDEDiagnosticIds.MissingAnalyzerReferenceId,
            title: ServicesVSResources.MissingAnalyzerReference,
            messageFormat: ServicesVSResources.Analyzer_assembly_0_depends_on_1_but_it_was_not_found_Analyzers_may_not_run_correctly_unless_the_missing_assembly_is_added_as_an_analyzer_reference_as_well,
            category: FeaturesResources.Roslyn_HostError,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor s_analyzerDependencyConflictRule = new(
            id: IDEDiagnosticIds.AnalyzerDependencyConflictId,
            title: ServicesVSResources.AnalyzerDependencyConflict,
            messageFormat: ServicesVSResources.Analyzer_assemblies_0_and_1_both_have_identity_2_but_different_contents_Only_one_will_be_loaded_and_analyzers_using_these_assemblies_may_not_run_correctly,
            category: FeaturesResources.Roslyn_HostError,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerDependencyCheckingService(
            VisualStudioWorkspace workspace,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            _workspace = workspace;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
        }

        public void ReanalyzeSolutionForConflicts()
        {
            var solution = _workspace.CurrentSolution;
            var currentAnalyzerPaths = solution
                .Projects
                .SelectMany(p => p.AnalyzerReferences)
                .OfType<AnalyzerFileReference>()
                .Select(a => a.FullPath)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

            lock (_gate)
            {
                // If we've already started analysis for this set of analyzers, no reason to start over
                if (currentAnalyzerPaths.SetEquals(_previousAnalyzerPaths))
                {
                    return;
                }

                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                _previousAnalyzerPaths = currentAnalyzerPaths;

                // Capturing cancellationToken here so the right instance is passed into the delegates below
                var cancellationToken = _cancellationTokenSource.Token;

                // We are explicitly relying on SafeContinueWith including LazyCancellation as a continuation option here
                _task = _task.SafeContinueWith(_ =>
                {
                    AnalyzeAndReportConflictsInSolution(solution, currentAnalyzerPaths, _hostDiagnosticUpdateSource, cancellationToken);
                },
                cancellationToken, TaskScheduler.Default);
            }
        }

        // Method is static to prevent accidental use of mutable state in this class
        private static void AnalyzeAndReportConflictsInSolution(
            Solution solution,
            ImmutableHashSet<string> currentAnalyzerPaths,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            CancellationToken cancellationToken)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => AssemblyIdentity.FromAssemblyDefinition(assembly));
            var loadedAssembliesList = new IgnorableAssemblyIdentityList(loadedAssemblies);

            var ignorableAssemblyLists = new[] { s_systemPrefixList, s_codeAnalysisPrefixList, s_explicitlyIgnoredAssemblyList, s_assembliesIgnoredByNameList, loadedAssembliesList };

            cancellationToken.ThrowIfCancellationRequested();

            var results = AnalyzerDependencyChecker.ComputeDependencyConflicts(currentAnalyzerPaths, ignorableAssemblyLists, s_bindingRedirectionService, cancellationToken);

            var builder = ImmutableArray.CreateBuilder<DiagnosticData>();

            var conflicts = results.Conflicts;
            var missingDependencies = results.MissingDependencies;

            using var argsBuilder = TemporaryArray<DiagnosticsUpdatedArgs>.Empty;
            foreach (var project in solution.Projects)
            {
                builder.Clear();

                // If our analysis has been cancelled, it means another request has been queued behind us; thus it's OK to stop
                // doing the analysis now and let that other one fix up any stale results.
                cancellationToken.ThrowIfCancellationRequested();

                var analyzerFilePaths = new HashSet<string>(
                    project.AnalyzerReferences
                           .OfType<AnalyzerFileReference>()
                           .Select(f => f.FullPath),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var conflict in conflicts)
                {
                    if (analyzerFilePaths.Contains(conflict.AnalyzerFilePath1) ||
                        analyzerFilePaths.Contains(conflict.AnalyzerFilePath2))
                    {
                        var messageArguments = new string[] { conflict.AnalyzerFilePath1, conflict.AnalyzerFilePath2, conflict.Identity.ToString() };
                        if (DiagnosticData.TryCreate(s_analyzerDependencyConflictRule, messageArguments, project, out var diagnostic))
                        {
                            builder.Add(diagnostic);
                        }
                    }
                }

                foreach (var missingDependency in missingDependencies)
                {
                    if (analyzerFilePaths.Contains(missingDependency.AnalyzerPath))
                    {
                        var messageArguments = new string[] { missingDependency.AnalyzerPath, missingDependency.DependencyIdentity.ToString() };
                        if (DiagnosticData.TryCreate(s_missingAnalyzerReferenceRule, messageArguments, project, out var diagnostic))
                        {
                            builder.Add(diagnostic);
                        }
                    }
                }

                hostDiagnosticUpdateSource.UpdateAndAddDiagnosticsArgsForProject(ref argsBuilder.AsRef(), project.Id, s_dependencyConflictErrorId, builder.ToImmutable());
            }

            hostDiagnosticUpdateSource.RaiseDiagnosticsUpdated(argsBuilder.ToImmutableAndClear());

            foreach (var conflict in conflicts)
            {
                LogConflict(conflict);
            }

            foreach (var missingDependency in missingDependencies)
            {
                LogMissingDependency(missingDependency);
            }
        }

        private static void LogConflict(AnalyzerDependencyConflict conflict)
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

        private static void LogMissingDependency(MissingAnalyzerDependency missingDependency)
        {
            Logger.Log(
                FunctionId.AnalyzerDependencyCheckingService_LogMissingDependency,
                KeyValueLogMessage.Create(m =>
                {
                    m["Analyzer"] = missingDependency.AnalyzerPath;
                    m["Identity"] = missingDependency.DependencyIdentity;
                }));
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
            if (!AssemblyIdentity.TryParseDisplayName(dllName, out var identity))
            {
                return;
            }

            list.Add(identity);
        }

        private sealed class BindingRedirectionService : IBindingRedirectionService
        {
            public AssemblyIdentity ApplyBindingRedirects(AssemblyIdentity originalIdentity)
            {
                var redirectedAssemblyName = AppDomain.CurrentDomain.ApplyPolicy(originalIdentity.ToString());
                if (AssemblyIdentity.TryParseDisplayName(redirectedAssemblyName, out var redirectedAssemblyIdentity))
                {
                    return redirectedAssemblyIdentity;
                }

                return originalIdentity;
            }
        }
    }
}
