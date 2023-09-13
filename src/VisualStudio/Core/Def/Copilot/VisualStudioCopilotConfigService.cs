// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Copilot;
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
        public VisualStudioCopilotConfigService()
        {
        }

        // TODO: Either complete the below hard-coded categories list OR populate it from all analyzers loaded for current project.
        private static readonly ImmutableArray<string> s_codeAnalysisSuggestionCategories = ImmutableArray.Create(
            "Security", "Performance", "Design", "Reliability", "Maintenance", "Style");

        public async Task<ImmutableArray<string>?> TryGetCopilotConfigPromptAsync(string feature, Project project, CancellationToken cancellationToken)
        {
            var prompt = await ReadCopilotConfigFileAndGetPromptAsync(feature, project, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(prompt))
                return null;

            return ImmutableArray.Create(prompt!);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private static Task<string?> ReadCopilotConfigFileAndGetPromptAsync(string feature, Project project, CancellationToken cancellationToken)
#pragma warning restore IDE0060 // Remove unused parameter
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
                    // TODO: Replace the below dummy "Description" with description read from .copilotconfig file
                    var description = @"I am interested in all the categories that are mentioned in the 'Categories' list below.";
                    builder.Add(description);

                    var categories = string.Join(",", s_codeAnalysisSuggestionCategories);
                    builder.Add(categories);
                    break;

                default:
                    return Task.FromResult<string?>(null);
            }

            var prompt = CopilotConfigFeatures.GetPrompt(feature, builder.ToArray());
            return Task.FromResult(prompt);
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
    }
}
