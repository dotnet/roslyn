// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    return SpecializedCollections.SingletonEnumerable(new ApplyChangesOperation(moveToNamespaceResult.UpdatedSolution));
                }
            }

            return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
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
