﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
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

        private ImmutableDictionary<string, Lazy<IEnumerable<CodeRefactoringProvider>>> LanguageToProvidersMap
            => _lazyLanguageToProvidersMap.Value;

        private IEnumerable<CodeRefactoringProvider> GetProviders(Document document)
        {
            if (LanguageToProvidersMap.TryGetValue(document.Project.Language, out var lazyProviders))
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
            var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();

            foreach (var provider in GetProviders(document))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var refactoring = await GetRefactoringFromProviderAsync(
                    document, state, provider, extensionManager, isBlocking: false, cancellationToken).ConfigureAwait(false);

                if (refactoring != null)
                {
                    return true;
                }
            }

            return false;
        }

        public Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(
            Document document,
            TextSpan state,
            CancellationToken cancellationToken)
            => GetRefactoringsAsync(document, state, isBlocking: false, cancellationToken);

        public Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(
            Document document,
            TextSpan state,
            bool isBlocking,
            CancellationToken cancellationToken)
            => GetRefactoringsAsync(document, state, isBlocking, addOperationScope: _ => null, cancellationToken);

        public async Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(
            Document document,
            TextSpan state,
            bool isBlocking,
            Func<string, IDisposable?> addOperationScope,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_CodeRefactoringService_GetRefactoringsAsync, cancellationToken))
            {
                var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();
                var tasks = new List<Task<CodeRefactoring?>>();

                foreach (var provider in GetProviders(document))
                {
                    tasks.Add(Task.Run(
                        () =>
                        {
                            var providerName = provider.GetType().Name;
                            using (addOperationScope(providerName))
                            using (RoslynEventSource.LogInformationalBlock(FunctionId.Refactoring_CodeRefactoringService_GetRefactoringsAsync, providerName, cancellationToken))
                            {
                                return GetRefactoringFromProviderAsync(document, state, provider, extensionManager, isBlocking, cancellationToken);
                            }
                        },
                        cancellationToken));
                }

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                return results.WhereNotNull().ToImmutableArray();
            }
        }

        private async Task<CodeRefactoring?> GetRefactoringFromProviderAsync(
            Document document,
            TextSpan state,
            CodeRefactoringProvider provider,
            IExtensionManager extensionManager,
            bool isBlocking,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (extensionManager.IsDisabled(provider))
            {
                return null;
            }

            try
            {
                var actions = ArrayBuilder<(CodeAction action, TextSpan? applicableToSpan)>.GetInstance();
                var context = new CodeRefactoringContext(document, state,

                    // TODO: Can we share code between similar lambdas that we pass to this API in BatchFixAllProvider.cs, CodeFixService.cs and CodeRefactoringService.cs?
                    (action, applicableToSpan) =>
                    {
                        // Serialize access for thread safety - we don't know what thread the refactoring provider will call this delegate from.
                        lock (actions)
                        {
                            actions.Add((action, applicableToSpan));
                        }
                    },
                    isBlocking,
                    cancellationToken);

                var task = provider.ComputeRefactoringsAsync(context) ?? Task.CompletedTask;
                await task.ConfigureAwait(false);

                var result = actions.Count > 0
                    ? new CodeRefactoring(provider, actions.ToImmutable())
                    : null;

                actions.Free();

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
    }
}
