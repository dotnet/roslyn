// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal abstract partial class AbstractMoveToNamespaceCodeAction : CodeActionWithOptions
    {
        private readonly IMoveToNamespaceService _moveToNamespaceService;
        private readonly MoveToNamespaceAnalysisResult _moveToNamespaceAnalysisResult;

        public AbstractMoveToNamespaceCodeAction(IMoveToNamespaceService moveToNamespaceService, MoveToNamespaceAnalysisResult analysisResult)
        {
            _moveToNamespaceService = moveToNamespaceService;
            _moveToNamespaceAnalysisResult = analysisResult;
        }

        public override object GetOptions(CancellationToken cancellationToken)
        {
            return _moveToNamespaceService.GetChangeNamespaceOptions(
                _moveToNamespaceAnalysisResult.Document,
                _moveToNamespaceAnalysisResult.OriginalNamespace,
                _moveToNamespaceAnalysisResult.Namespaces);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
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

            return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
        }

        private static ImmutableArray<CodeActionOperation> CreateRenameOperations(MoveToNamespaceResult moveToNamespaceResult)
        {
            Debug.Assert(moveToNamespaceResult.Succeeded);

            var operations = PooledObjects.ArrayBuilder<CodeActionOperation>.GetInstance();
            operations.Add(new ApplyChangesOperation(moveToNamespaceResult.UpdatedSolution));

            var symbolRenameCodeActionOperationFactory = moveToNamespaceResult.UpdatedSolution.Workspace.Services.GetService<ISymbolRenamedCodeActionOperationFactoryWorkspaceService>();

            // It's possible we're not in a host context providing this service, in which case
            // just provide a code action that won't notify of the symbol rename.
            // Without the symbol rename operation, code generators (like WPF) may not
            // know to regenerate code correctly.
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

            return operations.ToImmutableAndFree();
        }

        public static AbstractMoveToNamespaceCodeAction Generate(IMoveToNamespaceService changeNamespaceService, MoveToNamespaceAnalysisResult analysisResult)
            => analysisResult.Container switch
            {
                MoveToNamespaceAnalysisResult.ContainerType.NamedType => new MoveTypeToNamespaceCodeAction(changeNamespaceService, analysisResult),
                MoveToNamespaceAnalysisResult.ContainerType.Namespace => new MoveItemsToNamespaceCodeAction(changeNamespaceService, analysisResult),
                _ => throw ExceptionUtilities.UnexpectedValue(analysisResult.Container)
            };
    }
}
