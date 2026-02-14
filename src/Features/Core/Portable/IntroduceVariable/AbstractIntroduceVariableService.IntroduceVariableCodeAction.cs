// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.IntroduceVariable;

internal abstract partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
{
    private sealed class IntroduceVariableCodeAction : CodeAction
    {
        private readonly bool _allOccurrences;
        private readonly bool _isConstant;
        private readonly bool _isLocal;
        private readonly bool _isQueryLocal;
        private readonly TExpressionSyntax _expression;
        private readonly SemanticDocument _semanticDocument;
        private readonly TService _service;

        public readonly CodeCleanupOptions Options;

        public IntroduceVariableCodeAction(
            TService service,
            SemanticDocument document,
            CodeCleanupOptions options,
            TExpressionSyntax expression,
            bool allOccurrences,
            bool isConstant,
            bool isLocal,
            bool isQueryLocal)
        {
            _service = service;
            _semanticDocument = document;
            Options = options;
            _expression = expression;
            _allOccurrences = allOccurrences;
            _isConstant = isConstant;
            _isLocal = isLocal;
            _isQueryLocal = isQueryLocal;
            Title = CreateDisplayText(expression);
        }

        public override string Title { get; }

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var changedDocument = await GetChangedDocumentCoreAsync(cancellationToken).ConfigureAwait(false);
            return await Simplifier.ReduceAsync(changedDocument, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Document> GetChangedDocumentCoreAsync(CancellationToken cancellationToken)
        {
            if (_isQueryLocal)
            {
                return _service.IntroduceQueryLocal(_semanticDocument, _expression, _allOccurrences, cancellationToken);
            }
            else if (_isLocal)
            {
                return _service.IntroduceLocal(_semanticDocument, Options, _expression, _allOccurrences, _isConstant, cancellationToken);
            }
            else
            {
                return await _service.IntroduceFieldAsync(_semanticDocument, _expression, _allOccurrences, _isConstant, cancellationToken).ConfigureAwait(false);
            }
        }

        private string CreateDisplayText(TExpressionSyntax expression)
        {
            var singleLineExpression = _semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>().ConvertToSingleLine(expression);
            var nodeString = singleLineExpression.ToString();

            return CreateDisplayText(nodeString);
        }

        // Indexed by: allOccurrences, isConstant, isLocal
        private static readonly string[,,] formatStrings = new string[2, 2, 2]
            {
              {
                { FeaturesResources.Introduce_field_for_0, FeaturesResources.Introduce_local_for_0 },
                { FeaturesResources.Introduce_constant_for_0, FeaturesResources.Introduce_local_constant_for_0 }
              },
              {
                { FeaturesResources.Introduce_field_for_all_occurrences_of_0,  FeaturesResources.Introduce_local_for_all_occurrences_of_0 },
                { FeaturesResources.Introduce_constant_for_all_occurrences_of_0, FeaturesResources.Introduce_local_constant_for_all_occurrences_of_0 }
              }
            };

        private string CreateDisplayText(string nodeString)
        {
            var formatString = _isQueryLocal
                ? _allOccurrences
                    ? FeaturesResources.Introduce_query_variable_for_all_occurrences_of_0
                    : FeaturesResources.Introduce_query_variable_for_0
                : formatStrings[_allOccurrences ? 1 : 0, _isConstant ? 1 : 0, _isLocal ? 1 : 0];
            return string.Format(formatString, nodeString);
        }
    }
}
