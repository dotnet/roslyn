// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    [Export(typeof(ICodeRefactoringService)), Shared]
    internal class CodeRefactoringService : ICodeRefactoringService
    {
        private readonly Lazy<ImmutableDictionary<string, Lazy<IEnumerable<CodeRefactoringProvider>>>> _lazyLanguageToProvidersMap;

        [ImportingConstructor]
        public CodeRefactoringService(
            [ImportMany] IEnumerable<Lazy<CodeRefactoringProvider, CodeChangeProviderMetadata>> providers)
        {
            // convert set of all code refactoring providers into a map from language to a lazy initialized list of ordered providers.
            _lazyLanguageToProvidersMap = new Lazy<ImmutableDictionary<string, Lazy<IEnumerable<CodeRefactoringProvider>>>>(
                () =>
                    ImmutableDictionary.CreateRange(
                        DistributeLanguages(providers)
                            .GroupBy(lz => lz.Metadata.Language)
                            .Select(grp => new KeyValuePair<string, Lazy<IEnumerable<CodeRefactoringProvider>>>(
                                grp.Key,
                                new Lazy<IEnumerable<CodeRefactoringProvider>>(() => ExtensionOrderer.Order(grp).Select(lz => lz.Value))))));
        }

        private IEnumerable<Lazy<CodeRefactoringProvider, OrderableLanguageMetadata>> DistributeLanguages(IEnumerable<Lazy<CodeRefactoringProvider, CodeChangeProviderMetadata>> providers)
        {
            foreach (Lazy<CodeRefactoringProvider, CodeChangeProviderMetadata> provider in providers)
            {
                foreach (string language in provider.Metadata.Languages)
                {
                    OrderableLanguageMetadata orderable = new OrderableLanguageMetadata(null, language);
                    yield return new Lazy<CodeRefactoringProvider, OrderableLanguageMetadata>(() => provider.Value, orderable);
                }
            }
        }

        private ImmutableDictionary<string, Lazy<IEnumerable<CodeRefactoringProvider>>> LanguageToProvidersMap
        {
            get
            {
                return _lazyLanguageToProvidersMap.Value;
            }
        }

        private IEnumerable<CodeRefactoringProvider> GetProviders(Document document)
        {
            Lazy<IEnumerable<CodeRefactoringProvider>> lazyProviders;
            if (this.LanguageToProvidersMap.TryGetValue(document.Project.Language, out lazyProviders))
            {
                return lazyProviders.Value;
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<CodeRefactoringProvider>();
            }
        }

        public async Task<bool> HasRefactoringsAsync(
            Document document,
            TextSpan state,
            CancellationToken cancellationToken)
        {
            var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>();

            foreach (var provider in this.GetProviders(document))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var refactoring = await GetRefactoringFromProviderAsync(
                    document, state, provider, extensionManager, cancellationToken).ConfigureAwait(false);

                if (refactoring != null)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<IEnumerable<CodeRefactoring>> GetRefactoringsAsync(
            Document document,
            TextSpan state,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_CodeRefactoringService_GetRefactoringsAsync, cancellationToken))
            {
                var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>();
                var tasks = new List<Task<CodeRefactoring>>();

                foreach (var provider in this.GetProviders(document))
                {
                    tasks.Add(Task.Run(
                        async () => await GetRefactoringFromProviderAsync(document, state, provider, extensionManager, cancellationToken).ConfigureAwait(false), cancellationToken));
                }

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                return results.WhereNotNull();
            }
        }

        private async Task<CodeRefactoring> GetRefactoringFromProviderAsync(
            Document document,
            TextSpan state,
            CodeRefactoringProvider provider,
            IExtensionManager extensionManager,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (extensionManager.IsDisabled(provider))
            {
                return null;
            }

            try
            {
                var actions = new List<CodeAction>();
                var context = new CodeRefactoringContext(document, state,

                    // TODO: Can we share code between similar lambdas that we pass to this API in BatchFixAllProvider.cs, CodeFixService.cs and CodeRefactoringService.cs?
                    (a) =>
                    {
                        // Serialize access for thread safety - we don't know what thread the refactoring provider will call this delegate from.
                        lock (actions)
                        {
                            actions.Add(a);
                        }
                    },
                    cancellationToken);

                var task = provider.ComputeRefactoringsAsync(context) ?? SpecializedTasks.EmptyTask;
                await task.ConfigureAwait(false);
                if (actions.Count > 0)
                {
                    return new CodeRefactoring(provider, actions);
                }
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
    }
}
