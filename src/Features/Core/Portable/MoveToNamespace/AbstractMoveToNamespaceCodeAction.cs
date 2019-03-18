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
        private readonly AbstractMoveToNamespaceService _changeNamespaceService;
        private readonly MoveToNamespaceAnalysisResult _moveToNamespaceAnalysisResult;

        public AbstractMoveToNamespaceCodeAction(AbstractMoveToNamespaceService changeNamespaceService, MoveToNamespaceAnalysisResult analysisResult)
        {
            _changeNamespaceService = changeNamespaceService;
            _moveToNamespaceAnalysisResult = analysisResult;
        }

        public override object GetOptions(CancellationToken cancellationToken)
        {
            return _changeNamespaceService.GetOptionsAsync(
                _moveToNamespaceAnalysisResult.Document,
                _moveToNamespaceAnalysisResult.OriginalNamespace,
                cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            IEnumerable<CodeActionOperation> operations = null;

            if (options is MoveToNamespaceOptionsResult moveToNamespaceOptions && !moveToNamespaceOptions.IsCancelled)
            {
                var moveToNamespaceResult = await _changeNamespaceService.MoveToNamespaceAsync(
                    _moveToNamespaceAnalysisResult,
                    moveToNamespaceOptions.Namespace,
                    cancellationToken).ConfigureAwait(false);

                if (moveToNamespaceResult.Succeeded)
                {
                    operations = new CodeActionOperation[]
                    {
                        new ApplyChangesOperation(moveToNamespaceResult.UpdatedSolution)
                    };
                }
            }

            return operations;
        }

        public static AbstractMoveToNamespaceCodeAction Generate(AbstractMoveToNamespaceService changeNamespaceService, MoveToNamespaceAnalysisResult analysisResult)
            => analysisResult.Type switch
        {
            MoveToNamespaceAnalysisResult.ContainerType.NamedType => (AbstractMoveToNamespaceCodeAction)new MoveTypeToNamespaceCodeAction(changeNamespaceService, analysisResult),
            MoveToNamespaceAnalysisResult.ContainerType.Namespace => new MoveItemsToNamespaceCodeAction(changeNamespaceService, analysisResult),
            _ => throw new InvalidOperationException($"Unexpected type {analysisResult.Type}")
        };
    }
}
