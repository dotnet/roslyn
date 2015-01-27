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
        private readonly ExtractInterfaceTypeAnalysisResult typeAnalysisResult;
        private readonly AbstractExtractInterfaceService extractInterfaceService;
        private readonly Task<IEnumerable<CodeActionOperation>> taskReturningNoCodeActionOperations = Task.FromResult(SpecializedCollections.EmptyEnumerable<CodeActionOperation>());

        public ExtractInterfaceCodeAction(AbstractExtractInterfaceService extractInterfaceService, ExtractInterfaceTypeAnalysisResult typeAnalysisResult)
        {
            this.extractInterfaceService = extractInterfaceService;
            this.typeAnalysisResult = typeAnalysisResult;
        }

        public override object GetOptions(CancellationToken cancellationToken)
        {
            var containingNamespaceDisplay = typeAnalysisResult.TypeToExtractFrom.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : typeAnalysisResult.TypeToExtractFrom.ContainingNamespace.ToDisplayString();

            return extractInterfaceService.GetExtractInterfaceOptions(
                typeAnalysisResult.DocumentToExtractFrom,
                typeAnalysisResult.TypeToExtractFrom,
                typeAnalysisResult.ExtractableMembers,
                containingNamespaceDisplay,
                cancellationToken);
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            IEnumerable<CodeActionOperation> operations = null;

            var extractInterfaceOptions = options as ExtractInterfaceOptionsResult;
            if (extractInterfaceOptions != null && !extractInterfaceOptions.IsCancelled)
            {
                var extractInterfaceResult = extractInterfaceService.ExtractInterfaceFromAnalyzedType(typeAnalysisResult, extractInterfaceOptions, cancellationToken);

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
