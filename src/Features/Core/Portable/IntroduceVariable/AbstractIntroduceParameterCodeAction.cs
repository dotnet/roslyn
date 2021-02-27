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
    internal partial class AbstractIntroduceParameterService<TService, TExpressionSyntax, TInvocationExpressionSyntax, TIdentifierNameSyntax>
    {
        internal abstract class AbstractIntroduceParameterCodeAction : CodeAction
        {
            private readonly bool _allOccurrences;
            private readonly bool _trampoline;
            private readonly TService _service;
            private readonly TExpressionSyntax _expression;
            private readonly SemanticDocument _semanticDocument;

            internal AbstractIntroduceParameterCodeAction(
                SemanticDocument document,
                TService service,
                TExpressionSyntax expression,
                bool allOccurrences,
                bool trampoline)
            {
                _semanticDocument = document;
                _service = service;
                _expression = expression;
                _allOccurrences = allOccurrences;
                _trampoline = trampoline;
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
                var solution = await _service.IntroduceParameterAsync(_semanticDocument, _expression, _allOccurrences, _trampoline, cancellationToken).ConfigureAwait(false);
                return solution.GetDocument(_semanticDocument.Document.Id);
            }

            private string CreateDisplayText(TExpressionSyntax expression)
            {
                var singleLineExpression = _semanticDocument.Document.GetLanguageService<ISyntaxFactsService>().ConvertToSingleLine(expression);
                var nodeString = singleLineExpression.ToString();

                return string.Format(CreateDisplayText(), nodeString);
            }

            private string CreateDisplayText()
                => (_allOccurrences, _trampoline) switch
                {
                    (true, true) => FeaturesResources.Introduce_new_parameter_overload_for_all_occurrences_of_0,
                    (true, false) => FeaturesResources.Introduce_parameter_for_all_occurrences_of_0,
                    (false, true) => FeaturesResources.Introduce_new_parameter_overload_for_0,
                    (false, false) => FeaturesResources.Introduce_parameter_for_0
                };
        }
    }
}
