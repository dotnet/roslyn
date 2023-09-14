// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerInfoCache;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Copilot
{
    [ExportWorkspaceService(typeof(ICopilotConfigService), ServiceLayer.Host), Shared]
    [Export(typeof(VisualStudioCopilotConfigService))]
    internal sealed class VisualStudioCopilotConfigService : ICopilotConfigService, IDisposable
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioCopilotConfigService(
            VisualStudioWorkspace workspace,
            IThreadingContext threadingContext,
            SharedGlobalCache sharedGlobalCache)
        {
            _workspace = workspace;
            _threadingContext = threadingContext;
            _copilotServiceProvider = workspace.Services.GetRequiredService<ICopilotServiceProvider>();
            _diagnosticAnalyzerInfoCache = sharedGlobalCache.AnalyzerInfoCache;
            workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
        }

        private const string CopilotConfigFileName = ".copilotconfig";

        private readonly string _defaultDescription = @"I am interested in all the categories that are mentioned in the 'Categories' list below.";
        private static readonly ImmutableArray<string> s_defaultCcodeAnalysisSuggestionCategories = ImmutableArray.Create(
            "Security", "Performance", "Design", "Reliability", "Maintenance", "Style");

        private readonly ConcurrentDictionary<string, (Checksum checksum, Task<ImmutableDictionary<string, string>> readConfigTask)> _copilotConfigCache = new();
        private readonly ConcurrentDictionary<string, ImmutableArray<string>?> _copilotConfigResponseCache = new();
        private readonly ConcurrentDictionary<IReadOnlyList<AnalyzerReference>, Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>>> _diagnosticDescriptorCache = new();
        private readonly VisualStudioWorkspace _workspace;
        private readonly IThreadingContext _threadingContext;
        private readonly ICopilotServiceProvider _copilotServiceProvider;
        private readonly DiagnosticAnalyzerInfoCache _diagnosticAnalyzerInfoCache;

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            foreach (var project in _workspace.CurrentSolution.Projects)
                KickOffBackgrundComputationOfCopilotConfigData(project, cancellationToken);

            return Task.CompletedTask;
        }

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionReloaded:
                    foreach (var project in e.NewSolution.Projects)
                        KickOffBackgrundComputationOfCopilotConfigData(project, CancellationToken.None);
                    break;

                case WorkspaceChangeKind.AdditionalDocumentAdded:
                case WorkspaceChangeKind.AdditionalDocumentReloaded:
                case WorkspaceChangeKind.AdditionalDocumentChanged:
                    KickOffBackgrundComputationOfCopilotConfigData(e.NewSolution.GetProject(e.ProjectId), CancellationToken.None);
                    break;
            }
        }

        private void KickOffBackgrundComputationOfCopilotConfigData(Project? project, CancellationToken cancellationToken)
        {
            if (project == null)
                return;

            Task.Run(async () =>
            {
                // Add tasks for copilot config data computation for all features here.
                await GetCodeAnalysisSuggestionsConfigDataAsync(project, forceCompute: true, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public Task<ImmutableArray<(string, ImmutableArray<string>)>> TryGetCodeAnalysisSuggestionsConfigDataAsync(Project project, CancellationToken cancellationToken)
            => GetCodeAnalysisSuggestionsConfigDataAsync(project, forceCompute: false, cancellationToken);

        private async Task<ImmutableArray<(string, ImmutableArray<string>)>> GetCodeAnalysisSuggestionsConfigDataAsync(Project project, bool forceCompute, CancellationToken cancellationToken)
        {
            var response = await GetCopilotConfigResponseAsync(CopilotConfigFeatures.CodeAnalysisSuggestions, project, forceCompute, cancellationToken).ConfigureAwait(false);
            if (!response.HasValue)
                return ImmutableArray<(string, ImmutableArray<string>)>.Empty;

            var descriptorsByCategory = await GetAvailableDiagnosticDescriptorsByCategoryAsync(project, forceCompute).ConfigureAwait(false);
            if (descriptorsByCategory.IsEmpty)
                return ImmutableArray<(string, ImmutableArray<string>)>.Empty;

            return ParseCodeAnalysisSuggestionsResponse(response.Value, descriptorsByCategory);
        }

        private async Task<ImmutableArray<string>?> GetCopilotConfigResponseAsync(string feature, Project project, bool forceCompute, CancellationToken cancellationToken)
        {
            var prompt = await ReadCopilotConfigFileAndGetPromptAsync(feature, project, forceCompute, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(prompt))
                return null;

            return await GetCopilotConfigResponseAsync(prompt!, feature, project, forceCompute, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<string>?> GetCopilotConfigResponseAsync(string prompt, string feature, Project project, bool forceCompute, CancellationToken cancellationToken)
        {
            var requestKey = $"{feature}:{prompt}";
            if (_copilotConfigResponseCache.TryGetValue(requestKey, out var response))
                return response;

            if (!forceCompute)
            {
                KickOffBackgrundComputationOfCopilotConfigData(project, cancellationToken);
                return null;
            }

            response = await _copilotServiceProvider.SendOneOffRequestAsync(ImmutableArray.Create(prompt), cancellationToken).ConfigureAwait(false);
            return _copilotConfigResponseCache.GetOrAdd(requestKey, response);
        }

        private async Task<string?> ReadCopilotConfigFileAndGetPromptAsync(string feature, Project project, bool forceCompute, CancellationToken cancellationToken)
        {
            // TODO: Implement the following pieces:
            //       1. Parse ".copilotconfig" additional files from the project as validated content matches schema.
            //       2. Find relevant sections in these files for the given 'feature'
            //          For example, for "CodeAnalysisSuggestions" feature, we expect a "Description" section
            //          containing natural language description of the categories of desired code analysis suggestions
            //          The final prompt is created by substituting the "Description" section in the prompt and a
            //          "Categories" list for available categories of code analysis suggestions.
            //

            using var _ = ArrayBuilder<string>.GetInstance(out var builder);
            switch (feature)
            {
                case CopilotConfigFeatures.CodeAnalysisSuggestions:
                    var config = await GetCopilotConfigAsync(project, forceCompute, cancellationToken).ConfigureAwait(false);

                    var description = config.TryGetValue("Description", out var value) ? value : _defaultDescription;
                    builder.Add(description);

                    // TODO: Provide the list of diagnostics in the form of (Id, category, description) for LLM to pick from.
                    var availableDiagnostics = await GetAvailableDiagnosticDescriptorsByCategoryAsync(project, forceCompute).ConfigureAwait(false);
                    var categories = availableDiagnostics.IsEmpty ? s_defaultCcodeAnalysisSuggestionCategories : availableDiagnostics.Keys;
                    builder.Add(string.Join(",", categories));
                    break;

                default:
                    return null;
            }

            var prompt = CopilotConfigFeatures.GetPrompt(feature, builder.ToArray());
            return prompt;
        }

        private static ImmutableArray<(string, ImmutableArray<string>)> ParseCodeAnalysisSuggestionsResponse(
            ImmutableArray<string> response,
            ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> descriptorsByCategory)
        {
            using var _1 = ArrayBuilder<(string, ImmutableArray<string>)>.GetInstance(out var builder);
            using var _2 = ArrayBuilder<string>.GetInstance(out var idsBuilder);
            using var _3 = PooledHashSet<string>.GetInstance(out var uniqueIds);
            foreach (var responsePart in response.Order())
            {
                var trimmedResponsePart = responsePart;
                var index = responsePart.IndexOf("Answer:");
                if (index > 0)
                {
                    trimmedResponsePart = responsePart[index..];
                }

                var parts = trimmedResponsePart.Split(',');
                foreach (var item in parts)
                {
                    var trimmedItem = item.Trim();
                    if (descriptorsByCategory.TryGetValue(trimmedItem, out var descriptors))
                    {
                        foreach (var descriptor in descriptors)
                        {
                            if (uniqueIds.Add(descriptor.Id))
                                idsBuilder.Add(descriptor.Id);
                        }

                        if (idsBuilder.Count > 0)
                            builder.Add((trimmedItem, idsBuilder.ToImmutableAndClear()));
                    }
                }
            }

            return builder.ToImmutable();
        }

        private async Task<ImmutableDictionary<string, string>> GetCopilotConfigAsync(Project project, bool forceCompute, CancellationToken cancellationToken)
        {
            if (project.AdditionalDocuments.FirstOrDefault(d => d.Name == CopilotConfigFileName && d.FilePath != null) is TextDocument configFile)
            {
                Contract.ThrowIfNull(configFile.FilePath);

                var checksum = await configFile.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
                if (_copilotConfigCache.TryGetValue(configFile.FilePath!, out var value))
                {
                    var (cachedChecksum, task) = value;
                    if (!checksum.Equals(cachedChecksum))
                    {
                        task = ReadConfigAsync(configFile.FilePath, _threadingContext.DisposalToken);
                        _copilotConfigCache.AddOrUpdate(configFile.FilePath, (checksum, task), (_, _) => (checksum, task));
                    }

                    if (task.IsCompleted || forceCompute)
                        return await value.readConfigTask.ConfigureAwait(false);
                }
            }

            return ImmutableDictionary<string, string>.Empty;

            static async Task<ImmutableDictionary<string, string>> ReadConfigAsync(string path, CancellationToken cancellationToken)
            {
                try
                {
                    var builder = ImmutableDictionary.CreateBuilder<string, string>();
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    var jsonDocument = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                    foreach (var obj in jsonDocument.RootElement.EnumerateObject())
                        builder.Add(obj.Name, obj.Value.ToString());

                    return builder.ToImmutable();
                }
                catch (JsonException)
                {
                    return ImmutableDictionary<string, string>.Empty;
                }
            }
        }

        private async Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetAvailableDiagnosticDescriptorsByCategoryAsync(Project project, bool forceCompute)
        {
            if (!_diagnosticDescriptorCache.TryGetValue(project.AnalyzerReferences, out var task))
            {
                task = new Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>>(() => GetAllAvailableAnalyzerDescriptors(project), _threadingContext.DisposalToken);

                var returnedTask = _diagnosticDescriptorCache.GetOrAdd(project.AnalyzerReferences, task);
                if (returnedTask == task)
                    task.Start();

                task = returnedTask;
            }

            if (task.IsCompleted || forceCompute)
                return await task.ConfigureAwait(false);

            return ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>.Empty;

            ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> GetAllAvailableAnalyzerDescriptors(Project project)
            {
                var analyzers = project.AnalyzerReferences.SelectMany(reference => reference.GetAnalyzers(project.Language));
                var descriptors = analyzers.SelectMany(analyzer => analyzer.IsCompilerAnalyzer()
                    ? ImmutableArray<DiagnosticDescriptor>.Empty
                    : _diagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(analyzer));

                var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticDescriptor>>(StringComparer.OrdinalIgnoreCase);
                foreach (var descriptorGroup in descriptors.GroupBy(d => d.Category))
                {
                    builder.Add(descriptorGroup.Key, descriptorGroup.OrderByDescending(d => d.Id).ToImmutableArray());
                }

                return builder.ToImmutable();
                ;

            }
        }

        public void Dispose()
        {
            _workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
        }
    }
}
