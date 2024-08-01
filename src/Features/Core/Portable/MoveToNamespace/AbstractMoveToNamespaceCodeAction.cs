// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.CodeCleanup;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MoveToNamespace;

internal abstract partial class AbstractMoveToNamespaceCodeAction(
    IMoveToNamespaceService moveToNamespaceService,
    MoveToNamespaceAnalysisResult analysisResult) : CodeActionWithOptions
{
    private readonly IMoveToNamespaceService _moveToNamespaceService = moveToNamespaceService;
    private readonly MoveToNamespaceAnalysisResult _moveToNamespaceAnalysisResult = analysisResult;

    /// <summary>
    /// This code action does notify clients about the rename it performs.  However, this is an optional part of
    /// this work, that happens after the move has happened.  As such, this does not require non document changes
    /// and can run in all our hosts.
    /// </summary>
    public sealed override ImmutableArray<string> Tags => [];

    public sealed override object GetOptions(CancellationToken cancellationToken)
    {
        return _moveToNamespaceService.GetChangeNamespaceOptions(
            _moveToNamespaceAnalysisResult.Document,
            _moveToNamespaceAnalysisResult.OriginalNamespace,
            _moveToNamespaceAnalysisResult.Namespaces);
    }

    protected sealed override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
        object options, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        // We won't get an empty target namespace from VS, but still should handle it w/o crashing.
        if (options is MoveToNamespaceOptionsResult moveToNamespaceOptions &&
            !moveToNamespaceOptions.IsCancelled &&
            !string.IsNullOrEmpty(moveToNamespaceOptions.Namespace))
        {
            var moveToNamespaceResult = await _moveToNamespaceService.MoveToNamespaceAsync(
                _moveToNamespaceAnalysisResult,
                moveToNamespaceOptions.Namespace,
                cancellationToken).ConfigureAwait(false);

            if (moveToNamespaceResult.Succeeded)
            {
                return CreateRenameOperations(moveToNamespaceResult);
            }
        }

        return [];
    }

    private static ImmutableArray<CodeActionOperation> CreateRenameOperations(MoveToNamespaceResult moveToNamespaceResult)
    {
        Debug.Assert(moveToNamespaceResult.Succeeded);

        using var _ = PooledObjects.ArrayBuilder<CodeActionOperation>.GetInstance(out var operations);
        operations.Add(new ApplyChangesOperation(moveToNamespaceResult.UpdatedSolution));

        var symbolRenameCodeActionOperationFactory = moveToNamespaceResult.UpdatedSolution.Services.GetService<ISymbolRenamedCodeActionOperationFactoryWorkspaceService>();

        // It's possible we're not in a host context providing this service, in which case just provide a code
        // action that won't notify of the symbol rename. Without the symbol rename operation, code generators (like
        // WPF) may not know to regenerate code correctly.
        if (symbolRenameCodeActionOperationFactory != null)
        {
            foreach (var (newName, symbol) in moveToNamespaceResult.NewNameOriginalSymbolMapping)
            {
                operations.Add(symbolRenameCodeActionOperationFactory.CreateSymbolRenamedOperation(
                    symbol,
                    newName,
                    moveToNamespaceResult.OriginalSolution,
                    moveToNamespaceResult.UpdatedSolution));
            }
        }

        return operations.ToImmutableAndClear();
    }

    public static AbstractMoveToNamespaceCodeAction Generate(IMoveToNamespaceService changeNamespaceService, MoveToNamespaceAnalysisResult analysisResult)
        => analysisResult.Container switch
        {
            MoveToNamespaceAnalysisResult.ContainerType.NamedType => new MoveTypeToNamespaceCodeAction(changeNamespaceService, analysisResult),
            MoveToNamespaceAnalysisResult.ContainerType.Namespace => new MoveItemsToNamespaceCodeAction(changeNamespaceService, analysisResult),
            _ => throw ExceptionUtilities.UnexpectedValue(analysisResult.Container)
        };
}
