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

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Copilot
{
    [ExportWorkspaceService(typeof(ICopilotConfigService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioCopilotConfigService : ICopilotConfigService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioCopilotConfigService(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        private const string CopilotConfigFileName = ".copilotconfig";

        private readonly string _defaultDescription = @"I am interested in all the categories that are mentioned in the 'Categories' list below.";
        private static readonly ImmutableArray<string> s_defaultCcodeAnalysisSuggestionCategories = ImmutableArray.Create(
            "Security", "Performance", "Design", "Reliability", "Maintenance", "Style");

        private readonly ConcurrentDictionary<string, (Checksum checksum, Task<ImmutableDictionary<string, string>> readConfigTask)> _copilotConfigCache = new();
        private readonly ConcurrentDictionary<IReadOnlyList<AnalyzerReference>, Task<ImmutableArray<DiagnosticDescriptor>>> _diagnosticDescriptorCache = new();
        private readonly IThreadingContext _threadingContext;

        public async Task<ImmutableArray<string>?> TryGetCopilotConfigPromptAsync(string feature, Project project, CancellationToken cancellationToken)
        {
            var prompt = await ReadCopilotConfigFileAndGetPromptAsync(feature, project, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(prompt))
                return null;

            return ImmutableArray.Create(prompt!);
        }

        private async Task<string?> ReadCopilotConfigFileAndGetPromptAsync(string feature, Project project, CancellationToken cancellationToken)
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
                    var config = await GetCopilotConfigAsync(project, cancellationToken).ConfigureAwait(false);

                    var description = config.TryGetValue("Description", out var value) ? value : _defaultDescription;
                    builder.Add(description);

                    // TODO: Provide the list of diagnostics in the form of (Id, category, description) for LLM to pick from.
                    var availableDiagnostics = await GetAvailableDiagnosticDescriptorAsync(project).ConfigureAwait(false);
                    IEnumerable<string> categories = availableDiagnostics.IsEmpty ? s_defaultCcodeAnalysisSuggestionCategories : availableDiagnostics.Select(d => d.Category).ToHashSet();
                    builder.Add(string.Join(",", categories));
                    break;

                default:
                    return null;
            }

            var prompt = CopilotConfigFeatures.GetPrompt(feature, builder.ToArray());
            return prompt;
        }

        public Task<ImmutableArray<(string, ImmutableArray<string>)>> ParsePromptResponseAsync(ImmutableArray<string> response, string feature, Project project, CancellationToken cancellationToken)
        {
            using var _1 = ArrayBuilder<(string, ImmutableArray<string>)>.GetInstance(out var builder);

            if (feature == CopilotConfigFeatures.CodeAnalysisSuggestions)
            {
                using var _2 = ArrayBuilder<string>.GetInstance(out var idsBuilder);
                foreach (var responsePart in response.Order())
                {
                    var trimmedResponsePart = responsePart;
                    var index = responsePart.IndexOf("Step 3:");
                    if (index > 0)
                    {
                        trimmedResponsePart = responsePart[index..];
                    }

                    var parts = trimmedResponsePart.Split('\n');
                    foreach (var item in parts)
                    {
                        index = item.IndexOf(':');
                        if (index <= 0 || index >= item.Length - 1)
                            continue;

                        var prefix = item[..index].Trim();
                        if (prefix.All(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch)))
                        {
                            var ids = item[(index + 1)..].Split(',');
                            foreach (var id in ids)
                            {
                                var trimmedId = id.Trim();
                                if (!string.IsNullOrEmpty(trimmedId) && trimmedId.All(char.IsLetterOrDigit))
                                {
                                    idsBuilder.Add(trimmedId);
                                }
                            }

                            if (idsBuilder.Count > 0)
                            {
                                builder.Add((prefix, idsBuilder.ToImmutableAndClear()));
                            }
                        }
                    }
                }
            }

            return Task.FromResult(builder.ToImmutable());
        }

        public async Task<ImmutableDictionary<string, string>> GetCopilotConfigAsync(Project project, CancellationToken cancellationToken)
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

                    if (task.IsCompleted)
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

        private async Task<ImmutableArray<DiagnosticDescriptor>> GetAvailableDiagnosticDescriptorAsync(Project project)
        {
            if (!_diagnosticDescriptorCache.TryGetValue(project.AnalyzerReferences, out var task))
            {
                task = new Task<ImmutableArray<DiagnosticDescriptor>>(() => GetAllAvailableAnalyzerDescriptors(project), _threadingContext.DisposalToken);

                var returnedTask = _diagnosticDescriptorCache.GetOrAdd(project.AnalyzerReferences, task);
                if (returnedTask == task)
                    task.Start();

                task = returnedTask;
            }

            if (task.IsCompleted)
                return await task.ConfigureAwait(false);

            return ImmutableArray<DiagnosticDescriptor>.Empty;


            static ImmutableArray<DiagnosticDescriptor> GetAllAvailableAnalyzerDescriptors(Project project)
            {
                var analyzers = project.AnalyzerReferences.SelectMany(reference => reference.GetAnalyzers(project.Language));
                return analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).ToImmutableArray();
            }
        }
    }
}
