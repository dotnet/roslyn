// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal partial class AbstractIntroduceParameterService<TService, TExpressionSyntax>
    {
        internal abstract class AbstractIntroduceParameterCodeAction : CodeAction
        {
            private readonly bool _allOccurrences;
            private readonly TService _service;
            private readonly TExpressionSyntax _expression;
            private readonly SemanticDocument _semanticDocument;

            internal AbstractIntroduceParameterCodeAction(
                SemanticDocument document,
                TService service,
                TExpressionSyntax expression,
                bool allOccurrences)
            {
                _semanticDocument = document;
                _service = service;
                _expression = expression;
                _allOccurrences = allOccurrences;
                Title = CreateDisplayText(expression);
            }

            public override string Title { get; }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var changedDocument = await GetChangedDocumentCoreAsync(cancellationToken).ConfigureAwait(false);
                return await Simplifier.ReduceAsync(changedDocument, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            private async Task<Document> GetChangedDocumentCoreAsync(CancellationToken cancellationToken)
            {
                return await _service.IntroduceParameterAsync(_semanticDocument, _expression, _allOccurrences, cancellationToken).ConfigureAwait(false);
            }

            private string CreateDisplayText(TExpressionSyntax expression)
            {
                var singleLineExpression = _semanticDocument.Document.GetLanguageService<ISyntaxFactsService>().ConvertToSingleLine(expression);
                var nodeString = singleLineExpression.ToString();

                return CreateDisplayText(nodeString);
            }

            private string CreateDisplayText(string nodeString)
            {
                var formatString = _allOccurrences
                        ? FeaturesResources.Introduce_parameter_for_all_occurrences_of_0
                        : FeaturesResources.Introduce_parameter_for_0;
                return string.Format(formatString, nodeString);
            }

            protected ITypeSymbol GetExpressionType(
                CancellationToken cancellationToken)
            {
                var semanticModel = _semanticDocument.SemanticModel;
                var typeInfo = semanticModel.GetTypeInfo(_expression, cancellationToken);

                return typeInfo.Type ?? typeInfo.ConvertedType ?? semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
            }
        }
    }
}
