// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal class ExtractInterfaceCodeAction : CodeActionWithOptions
    {
        private readonly ExtractInterfaceTypeAnalysisResult _typeAnalysisResult;
        private readonly AbstractExtractInterfaceService _extractInterfaceService;

        public ExtractInterfaceCodeAction(AbstractExtractInterfaceService extractInterfaceService, ExtractInterfaceTypeAnalysisResult typeAnalysisResult)
        {
            _extractInterfaceService = extractInterfaceService;
            _typeAnalysisResult = typeAnalysisResult;
        }

        public override object GetOptions(CancellationToken cancellationToken)
        {
            var containingNamespaceDisplay = _typeAnalysisResult.TypeToExtractFrom.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : _typeAnalysisResult.TypeToExtractFrom.ContainingNamespace.ToDisplayString();

            return AbstractExtractInterfaceService.GetExtractInterfaceOptionsAsync(
                _typeAnalysisResult.DocumentToExtractFrom,
                _typeAnalysisResult.TypeToExtractFrom,
                _typeAnalysisResult.ExtractableMembers,
                containingNamespaceDisplay,
                _typeAnalysisResult.FallbackOptions,
                cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            var operations = SpecializedCollections.EmptyEnumerable<CodeActionOperation>();

            if (options is ExtractInterfaceOptionsResult extractInterfaceOptions && !extractInterfaceOptions.IsCancelled)
            {
                var extractInterfaceResult = await _extractInterfaceService
                        .ExtractInterfaceFromAnalyzedTypeAsync(_typeAnalysisResult, extractInterfaceOptions, cancellationToken).ConfigureAwait(false);

                if (extractInterfaceResult.Succeeded)
                {
                    operations = new CodeActionOperation[]
                    {
                        new ApplyChangesOperation(extractInterfaceResult.UpdatedSolution),
                        new DocumentNavigationOperation(extractInterfaceResult.NavigationDocumentId, position: 0)
                    };
                }
            }

            return operations;
        }

        public override string Title => FeaturesResources.Extract_interface;
    }
}
