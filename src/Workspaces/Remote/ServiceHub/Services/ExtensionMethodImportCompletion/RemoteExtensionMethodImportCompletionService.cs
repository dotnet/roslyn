// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteExtensionMethodImportCompletionService : BrokeredServiceBase, IRemoteExtensionMethodImportCompletionService
{
    internal sealed class Factory : FactoryBase<IRemoteExtensionMethodImportCompletionService>
    {
        protected override IRemoteExtensionMethodImportCompletionService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteExtensionMethodImportCompletionService(arguments);
    }

    public RemoteExtensionMethodImportCompletionService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
    }

    public ValueTask<SerializableUnimportedExtensionMethods?> GetUnimportedExtensionMethodsAsync(
        Checksum solutionChecksum,
        DocumentId documentId,
        int position,
        string receiverTypeSymbolKeyData,
        ImmutableArray<string> namespaceInScope,
        ImmutableArray<string> targetTypesSymbolKeyData,
        bool forceCacheCreation,
        bool hideAdvancedMembers,
        CancellationToken cancellationToken)
    {
        var stopwatch = SharedStopwatch.StartNew();
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var assetSyncTime = stopwatch.Elapsed;

            // Completion always uses frozen-partial semantic in-proc, which is not automatically passed to OOP, so enable it explicitly
            var document = solution.GetRequiredDocument(documentId).WithFrozenPartialSemantics(cancellationToken);
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var symbol = SymbolKey.ResolveString(receiverTypeSymbolKeyData, compilation, cancellationToken: cancellationToken).GetAnySymbol();

            if (symbol is ITypeSymbol receiverTypeSymbol)
            {
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var namespaceInScopeSet = new HashSet<string>(namespaceInScope, syntaxFacts.StringComparer);
                var targetTypes = targetTypesSymbolKeyData
                        .Select(symbolKey => SymbolKey.ResolveString(symbolKey, compilation, cancellationToken: cancellationToken).GetAnySymbol() as ITypeSymbol)
                        .WhereNotNull().ToImmutableArray();

                var intialGetSymbolsTime = stopwatch.Elapsed - assetSyncTime;

                var result = await ExtensionMethodImportCompletionHelper.GetUnimportedExtensionMethodsInCurrentProcessAsync(
                    document, semanticModel: null, position, receiverTypeSymbol, namespaceInScopeSet, targetTypes, forceCacheCreation, hideAdvancedMembers, assetSyncTime, cancellationToken).ConfigureAwait(false);

                result.GetSymbolsTime += intialGetSymbolsTime;
                return result;
            }

            return null;
        }, cancellationToken);
    }

    public ValueTask WarmUpCacheAsync(Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, solution =>
        {
            var project = solution.GetRequiredProject(projectId);
            ExtensionMethodImportCompletionHelper.WarmUpCacheInCurrentProcess(project);
            return ValueTaskFactory.CompletedTask;
        }, cancellationToken);
    }
}
