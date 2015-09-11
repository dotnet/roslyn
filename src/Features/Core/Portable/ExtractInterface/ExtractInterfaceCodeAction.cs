// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly Task<IEnumerable<CodeActionOperation>> _taskReturningNoCodeActionOperations = SpecializedTasks.EmptyEnumerable<CodeActionOperation>();

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

            return _extractInterfaceService.GetExtractInterfaceOptions(
                _typeAnalysisResult.DocumentToExtractFrom,
                _typeAnalysisResult.TypeToExtractFrom,
                _typeAnalysisResult.ExtractableMembers,
                containingNamespaceDisplay,
                cancellationToken);
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            IEnumerable<CodeActionOperation> operations = null;

            var extractInterfaceOptions = options as ExtractInterfaceOptionsResult;
            if (extractInterfaceOptions != null && !extractInterfaceOptions.IsCancelled)
            {
                var extractInterfaceResult = _extractInterfaceService.ExtractInterfaceFromAnalyzedType(_typeAnalysisResult, extractInterfaceOptions, cancellationToken);

                if (extractInterfaceResult.Succeeded)
                {
                    operations = new CodeActionOperation[]
                        {
                        new ApplyChangesOperation(extractInterfaceResult.UpdatedSolution),
                        new NavigationOperation(extractInterfaceResult.NavigationDocumentId, position: 0)
                        };
                }
            }

            return Task.FromResult(operations);
        }

        public override string Title
        {
            get { return FeaturesResources.ExtractInterface; }
        }
    }
}
