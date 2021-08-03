// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Completion
{
    public abstract partial class CompletionServiceWithProviders
    {

        private readonly ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, StrongBox<ImmutableArray<CompletionProvider>>> _projectCompletionProvidersMap
             = new();

        private readonly ConditionalWeakTable<AnalyzerReference, ProjectCompletionProvider> _analyzerReferenceToCompletionProvidersMap
            = new();
        private readonly ConditionalWeakTable<AnalyzerReference, ProjectCompletionProvider>.CreateValueCallback _createProjectCompletionProvidersProvider
            = new(r => new ProjectCompletionProvider(r));

        private ImmutableArray<CompletionProvider> GetProjectCompletionProviders(Project project)
        {
            if (project is null)
            {
                return ImmutableArray<CompletionProvider>.Empty;
            }

            if (project.Solution.Workspace.Kind == WorkspaceKind.Interactive)
            {
                // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict completions in Interactive
                return ImmutableArray<CompletionProvider>.Empty;
            }

            if (_projectCompletionProvidersMap.TryGetValue(project.AnalyzerReferences, out var completionProviders))
            {
                return completionProviders.Value;
            }

            return GetProjectCompletionProvidersSlow(project);

            // Local functions
            ImmutableArray<CompletionProvider> GetProjectCompletionProvidersSlow(Project project)
            {
                return _projectCompletionProvidersMap.GetValue(project.AnalyzerReferences, pId => new StrongBox<ImmutableArray<CompletionProvider>>(ComputeProjectCompletionProviders(project))).Value;
            }

            ImmutableArray<CompletionProvider> ComputeProjectCompletionProviders(Project project)
            {
                using var _ = ArrayBuilder<CompletionProvider>.GetInstance(out var builder);
                foreach (var reference in project.AnalyzerReferences)
                {
                    var projectCompletionProvider = _analyzerReferenceToCompletionProvidersMap.GetValue(reference, _createProjectCompletionProvidersProvider);
                    foreach (var completionProvider in projectCompletionProvider.GetExtensions(project.Language))
                    {
                        builder.Add(completionProvider);
                    }
                }

                return builder.ToImmutable();
            }
        }

        private class ProjectCompletionProvider
            : AbstractProjectExtensionProvider<CompletionProvider, ExportCompletionProviderAttribute>
        {
            public ProjectCompletionProvider(AnalyzerReference reference)
                : base(reference)
            {
            }

            protected override bool SupportsLanguage(ExportCompletionProviderAttribute exportAttribute, string language)
            {
                return exportAttribute.Language == null
                    || exportAttribute.Language.Length == 0
                    || exportAttribute.Language.Contains(language);
            }

            protected override bool TryGetExtensionsFromReference(AnalyzerReference reference, out ImmutableArray<CompletionProvider> extensions)
            {
                // check whether the analyzer reference knows how to return completion providers directly.
                if (reference is ICompletionProviderFactory completionProviderFactory)
                {
                    extensions = completionProviderFactory.GetCompletionProviders();
                    return true;
                }

                extensions = default;
                return false;
            }
        }
    }
}
