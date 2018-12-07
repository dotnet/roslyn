// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal class MoveToNamespaceCodeAction : CodeActionWithOptions
    {
        private readonly AbstractMoveToNamespaceService _changeNamespaceService;
        private readonly MoveToNamespaceAnalysisResult _moveToNamespaceAnalysisResult;

        public override string Title => FeaturesResources.Move_to_namespace;

        public MoveToNamespaceCodeAction(AbstractMoveToNamespaceService changeNamespaceService, MoveToNamespaceAnalysisResult analysisResult)
        {
            _changeNamespaceService = changeNamespaceService;
            _moveToNamespaceAnalysisResult = analysisResult;
        }

        public override object GetOptions(CancellationToken cancellationToken)
        {
            return _changeNamespaceService.GetOptions(
                _moveToNamespaceAnalysisResult.Document,
                _moveToNamespaceAnalysisResult.OriginalNamespace,
                cancellationToken);
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
    }
}
