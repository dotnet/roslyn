// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal abstract partial class AbstractMoveToNamespaceCodeAction : CodeActionWithOptions
    {
        private readonly IMoveToNamespaceService _moveToNamespaceService;
        private readonly MoveToNamespaceAnalysisResult _moveToNamespaceAnalysisResult;

        public AbstractMoveToNamespaceCodeAction(IMoveToNamespaceService changeNamespaceService, MoveToNamespaceAnalysisResult analysisResult)
        {
            _moveToNamespaceService = changeNamespaceService;
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
            IEnumerable<CodeActionOperation> operations = null;

            if (options is MoveToNamespaceOptionsResult moveToNamespaceOptions && !moveToNamespaceOptions.IsCancelled)
            {
                var moveToNamespaceResult = await _moveToNamespaceService.MoveToNamespaceAsync(
                    _moveToNamespaceAnalysisResult,
                    moveToNamespaceOptions.Namespace,
                    cancellationToken).ConfigureAwait(false);

                if (moveToNamespaceResult.Succeeded)
                {
                    operations = SpecializedCollections.SingletonEnumerable(new ApplyChangesOperation(moveToNamespaceResult.UpdatedSolution));
                }
            }

            return operations;
        }

        public static AbstractMoveToNamespaceCodeAction Generate(IMoveToNamespaceService changeNamespaceService, MoveToNamespaceAnalysisResult analysisResult)
            => analysisResult.Container switch
        {
            MoveToNamespaceAnalysisResult.ContainerType.NamedType => (AbstractMoveToNamespaceCodeAction)new MoveTypeToNamespaceCodeAction(changeNamespaceService, analysisResult),
            MoveToNamespaceAnalysisResult.ContainerType.Namespace => new MoveItemsToNamespaceCodeAction(changeNamespaceService, analysisResult),
            _ => throw new InvalidOperationException($"Unexpected type {analysisResult.Container}")
        };
    }
}
