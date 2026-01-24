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

internal sealed class RemoteExtensionMemberImportCompletionService(
    in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteExtensionMemberImportCompletionService
{
    internal sealed class Factory : FactoryBase<IRemoteExtensionMemberImportCompletionService>
    {
        protected override IRemoteExtensionMemberImportCompletionService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteExtensionMemberImportCompletionService(arguments);
    }

    public ValueTask<ImmutableArray<SerializableImportCompletionItem>> GetUnimportedExtensionMembersAsync(
        Checksum solutionChecksum,
        DocumentId documentId,
        int position,
        string receiverTypeSymbolKeyData,
        bool isStatic,
        ImmutableArray<string> namespaceInScope,
        ImmutableArray<string> targetTypesSymbolKeyData,
        bool forceCacheCreation,
        bool hideAdvancedMembers,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
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

                return await ExtensionMemberImportCompletionHelper.GetUnimportedExtensionMembersInCurrentProcessAsync(
                    document, semanticModel: null, position, receiverTypeSymbol, isStatic, namespaceInScopeSet, targetTypes, forceCacheCreation, hideAdvancedMembers, cancellationToken).ConfigureAwait(false);
            }

            return default;
        }, cancellationToken);
    }

    public ValueTask WarmUpCacheAsync(Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);
            ExtensionMemberImportCompletionHelper.WarmUpCacheInCurrentProcess(project);
        }, cancellationToken);
    }
}
