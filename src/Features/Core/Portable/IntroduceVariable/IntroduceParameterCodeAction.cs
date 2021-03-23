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
    internal partial class AbstractIntroduceParameterService<TExpressionSyntax, TInvocationExpressionSyntax, TIdentifierNameSyntax>
    {
        internal class IntroduceParameterCodeAction : CodeAction
        {
            private readonly bool _allOccurrences;
            private readonly bool _trampoline;
            private readonly bool _overload;
            private readonly AbstractIntroduceParameterService<TExpressionSyntax, TInvocationExpressionSyntax, TIdentifierNameSyntax> _service;
            private readonly TExpressionSyntax _expression;
            private readonly SemanticDocument _semanticDocument;

            internal IntroduceParameterCodeAction(
                SemanticDocument document,
                AbstractIntroduceParameterService<TExpressionSyntax, TInvocationExpressionSyntax, TIdentifierNameSyntax> service,
                TExpressionSyntax expression,
                bool allOccurrences,
                bool trampoline,
                bool overload)
            {
                _semanticDocument = document;
                _service = service;
                _expression = expression;
                _allOccurrences = allOccurrences;
                _trampoline = trampoline;
                _overload = overload;
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
                var solution = await _service.IntroduceParameterAsync(_semanticDocument, _expression, _allOccurrences, _trampoline, _overload, cancellationToken).ConfigureAwait(false);
                return solution.GetDocument(_semanticDocument.Document.Id);
            }

            private string CreateDisplayText(TExpressionSyntax expression)
            {
                var singleLineExpression = _semanticDocument.Document.GetLanguageService<ISyntaxFactsService>().ConvertToSingleLine(expression);
                var nodeString = singleLineExpression.ToString();

                return string.Format(CreateDisplayText(), nodeString);
            }

            private string CreateDisplayText()
                => (_allOccurrences, _trampoline, _overload) switch
                {
                    (true, true, false) => FeaturesResources.Introduce_parameter_and_extract_method_for_all_occurrences_of_0,
                    (true, false, false) => FeaturesResources.Introduce_parameter_for_all_occurrences_of_0,
                    (true, false, true) => FeaturesResources.Introduce_new_parameter_overload_for_all_occurrences_of_0,
                    (false, true, false) => FeaturesResources.Introduce_parameter_and_extract_method_for_0,
                    (false, false, true) => FeaturesResources.Introduce_new_parameter_overload_for_0,
                    (false, false, false) => FeaturesResources.Introduce_parameter_for_0,
                    _ => throw new System.NotImplementedException()
                };
        }
    }
}
