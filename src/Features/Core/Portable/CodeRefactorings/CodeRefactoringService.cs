// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    [Export(typeof(ICodeRefactoringService)), Shared]
    internal sealed class CodeRefactoringService : ICodeRefactoringService
    {
        private readonly Lazy<ImmutableDictionary<string, Lazy<ImmutableArray<CodeRefactoringProvider>>>> _lazyLanguageToProvidersMap;
        private readonly Lazy<ImmutableDictionary<CodeRefactoringProvider, CodeChangeProviderMetadata>> _lazyRefactoringToMetadataMap;
        private readonly ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, StrongBox<ImmutableArray<CodeRefactoringProvider>>> _projectRefactoringsMap = new();

        private readonly ConditionalWeakTable<AnalyzerReference, ProjectCodeRefactoringProvider> _analyzerReferenceToRefactoringsMap = new();
        private readonly ConditionalWeakTable<AnalyzerReference, ProjectCodeRefactoringProvider>.CreateValueCallback _createProjectCodeRefactoringsProvider
            = new(r => new ProjectCodeRefactoringProvider(r));

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeRefactoringService(
            [ImportMany] IEnumerable<Lazy<CodeRefactoringProvider, CodeChangeProviderMetadata>> providers)
        {
            // convert set of all code refactoring providers into a map from language to a lazy initialized list of ordered providers.
            _lazyLanguageToProvidersMap = new Lazy<ImmutableDictionary<string, Lazy<ImmutableArray<CodeRefactoringProvider>>>>(
                () =>
                    ImmutableDictionary.CreateRange(
                        DistributeLanguages(providers)
                            .GroupBy(lz => lz.Metadata.Language)
                            .Select(grp => new KeyValuePair<string, Lazy<ImmutableArray<CodeRefactoringProvider>>>(
                                grp.Key,
                                new Lazy<ImmutableArray<CodeRefactoringProvider>>(() => ExtensionOrderer.Order(grp).Select(lz => lz.Value).ToImmutableArray())))));
            _lazyRefactoringToMetadataMap = new(() => providers.Where(provider => provider.IsValueCreated).ToImmutableDictionary(provider => provider.Value, provider => provider.Metadata));
        }

        private static IEnumerable<Lazy<CodeRefactoringProvider, OrderableLanguageMetadata>> DistributeLanguages(IEnumerable<Lazy<CodeRefactoringProvider, CodeChangeProviderMetadata>> providers)
        {
            foreach (var provider in providers)
            {
                foreach (var language in provider.Metadata.Languages)
                {
                    var orderable = new OrderableLanguageMetadata(
                        provider.Metadata.Name, language, provider.Metadata.AfterTyped, provider.Metadata.BeforeTyped);
                    yield return new Lazy<CodeRefactoringProvider, OrderableLanguageMetadata>(() => provider.Value, orderable);
                }
            }
        }

        private ImmutableDictionary<string, Lazy<ImmutableArray<CodeRefactoringProvider>>> LanguageToProvidersMap
            => _lazyLanguageToProvidersMap.Value;

        private ImmutableDictionary<CodeRefactoringProvider, CodeChangeProviderMetadata> RefactoringToMetadataMap
            => _lazyRefactoringToMetadataMap.Value;

        private ConcatImmutableArray<CodeRefactoringProvider> GetProviders(Document document)
        {
            var allRefactorings = ImmutableArray<CodeRefactoringProvider>.Empty;
            if (LanguageToProvidersMap.TryGetValue(document.Project.Language, out var lazyProviders))
            {
                allRefactorings = lazyProviders.Value;
            }

            return allRefactorings.ConcatFast(GetProjectRefactorings(document.Project));
        }

        public async Task<bool> HasRefactoringsAsync(
            Document document,
            TextSpan state,
            CodeActionOptions options,
            CancellationToken cancellationToken)
        {
            var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();

            foreach (var provider in GetProviders(document))
            {
                cancellationToken.ThrowIfCancellationRequested();
                RefactoringToMetadataMap.TryGetValue(provider, out var providerMetadata);

                var refactoring = await GetRefactoringFromProviderAsync(
                    document, state, provider, providerMetadata, extensionManager, options, cancellationToken).ConfigureAwait(false);

                if (refactoring != null)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(
            Document document,
            TextSpan state,
            CodeActionRequestPriority priority,
            CodeActionOptions options,
            Func<string, IDisposable?> addOperationScope,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_CodeRefactoringService_GetRefactoringsAsync, cancellationToken))
            {
                var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();
                using var _ = ArrayBuilder<Task<CodeRefactoring?>>.GetInstance(out var tasks);

                foreach (var provider in GetProviders(document))
                {
                    if (priority != CodeActionRequestPriority.None && priority != provider.RequestPriority)
                        continue;

                    tasks.Add(Task.Run(() =>
                        {
                            var providerName = provider.GetType().Name;
                            RefactoringToMetadataMap.TryGetValue(provider, out var providerMetadata);

                            using (addOperationScope(providerName))
                            using (RoslynEventSource.LogInformationalBlock(FunctionId.Refactoring_CodeRefactoringService_GetRefactoringsAsync, providerName, cancellationToken))
                            {
                                return GetRefactoringFromProviderAsync(document, state, provider, providerMetadata, extensionManager, options, cancellationToken);
                            }
                        },
                        cancellationToken));
                }

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                return results.WhereNotNull().ToImmutableArray();
            }
        }

        private static async Task<CodeRefactoring?> GetRefactoringFromProviderAsync(
            Document document,
            TextSpan state,
            CodeRefactoringProvider provider,
            CodeChangeProviderMetadata? providerMetadata,
            IExtensionManager extensionManager,
            CodeActionOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (extensionManager.IsDisabled(provider))
            {
                return null;
            }

            try
            {
                using var _ = ArrayBuilder<(CodeAction action, TextSpan? applicableToSpan)>.GetInstance(out var actions);
                var context = new CodeRefactoringContext(document, state,

                    // TODO: Can we share code between similar lambdas that we pass to this API in BatchFixAllProvider.cs, CodeFixService.cs and CodeRefactoringService.cs?
                    (action, applicableToSpan) =>
                    {
                        // Serialize access for thread safety - we don't know what thread the refactoring provider will call this delegate from.
                        lock (actions)
                        {
                            // Add the Refactoring Provider Name to the parent CodeAction's CustomTags.
                            // Always add a name even in cases of 3rd party refactorings that do not export
                            // name metadata.
                            action.AddCustomTag(providerMetadata?.Name ?? provider.GetTypeDisplayName());

                            actions.Add((action, applicableToSpan));
                        }
                    },
                    options,
                    cancellationToken);

                var task = provider.ComputeRefactoringsAsync(context) ?? Task.CompletedTask;
                await task.ConfigureAwait(false);

                var result = actions.Count > 0
                    ? new CodeRefactoring(provider, actions.ToImmutable())
                    : null;

                return result;
            }
            catch (OperationCanceledException)
            {
                // We don't want to catch operation canceled exceptions in the catch block 
                // below. So catch is here and rethrow it.
                throw;
            }
            catch (Exception e)
            {
                extensionManager.HandleException(provider, e);
            }

            return null;
        }

        private ImmutableArray<CodeRefactoringProvider> GetProjectRefactorings(Project project)
        {
            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict refactorings in Interactive
            if (project.Solution.Workspace.Kind == WorkspaceKind.Interactive)
            {
                return ImmutableArray<CodeRefactoringProvider>.Empty;
            }

            if (_projectRefactoringsMap.TryGetValue(project.AnalyzerReferences, out var refactorings))
            {
                return refactorings.Value;
            }

            return GetProjectRefactoringsSlow(project);

            // Local functions
            ImmutableArray<CodeRefactoringProvider> GetProjectRefactoringsSlow(Project project)
            {
                return _projectRefactoringsMap.GetValue(project.AnalyzerReferences, pId => new StrongBox<ImmutableArray<CodeRefactoringProvider>>(ComputeProjectRefactorings(project))).Value;
            }

            ImmutableArray<CodeRefactoringProvider> ComputeProjectRefactorings(Project project)
            {
                using var _ = ArrayBuilder<CodeRefactoringProvider>.GetInstance(out var builder);
                foreach (var reference in project.AnalyzerReferences)
                {
                    var projectCodeRefactoringProvider = _analyzerReferenceToRefactoringsMap.GetValue(reference, _createProjectCodeRefactoringsProvider);
                    foreach (var refactoring in projectCodeRefactoringProvider.GetExtensions(project.Language))
                        builder.Add(refactoring);
                }

                return builder.ToImmutable();
            }
        }

        private class ProjectCodeRefactoringProvider
            : AbstractProjectExtensionProvider<CodeRefactoringProvider, ExportCodeRefactoringProviderAttribute>
        {
            public ProjectCodeRefactoringProvider(AnalyzerReference reference)
                : base(reference)
            {
            }

            protected override bool SupportsLanguage(ExportCodeRefactoringProviderAttribute exportAttribute, string language)
            {
                return exportAttribute.Languages == null
                    || exportAttribute.Languages.Length == 0
                    || exportAttribute.Languages.Contains(language);
            }

            protected override bool TryGetExtensionsFromReference(AnalyzerReference reference, out ImmutableArray<CodeRefactoringProvider> extensions)
            {
                // check whether the analyzer reference knows how to return fixers directly.
                if (reference is ICodeRefactoringProviderFactory codeRefactoringProviderFactory)
                {
                    extensions = codeRefactoringProviderFactory.GetRefactorings();
                    return true;
                }

                extensions = default;
                return false;
            }
        }
    }
}
