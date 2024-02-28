// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.IntroduceVariable;

internal partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
{
    internal abstract class AbstractIntroduceVariableCodeAction : CodeAction
    {
        private readonly bool _allOccurrences;
        private readonly bool _isConstant;
        private readonly bool _isLocal;
        private readonly bool _isQueryLocal;
        private readonly TExpressionSyntax _expression;
        private readonly SemanticDocument _semanticDocument;
        private readonly TService _service;

        public readonly CodeCleanupOptions Options;

        internal AbstractIntroduceVariableCodeAction(
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
            var simplifierOptions = await changedDocument.GetSimplifierOptionsAsync(Options.SimplifierOptions, cancellationToken).ConfigureAwait(false);
            return await Simplifier.ReduceAsync(changedDocument, simplifierOptions, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Document> GetChangedDocumentCoreAsync(CancellationToken cancellationToken)
        {
            if (_isQueryLocal)
            {
                return await _service.IntroduceQueryLocalAsync(_semanticDocument, _expression, _allOccurrences, cancellationToken).ConfigureAwait(false);
            }
            else if (_isLocal)
            {
                return await _service.IntroduceLocalAsync(_semanticDocument, _expression, _allOccurrences, _isConstant, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await _service.IntroduceFieldAsync(_semanticDocument, _expression, _allOccurrences, _isConstant, cancellationToken).ConfigureAwait(false);
            }
        }

        private string CreateDisplayText(TExpressionSyntax expression)
        {
            var singleLineExpression = _semanticDocument.Document.GetLanguageService<ISyntaxFactsService>().ConvertToSingleLine(expression);
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

        protected ITypeSymbol GetExpressionType(
            CancellationToken cancellationToken)
        {
            var semanticModel = _semanticDocument.SemanticModel;
            var typeInfo = semanticModel.GetTypeInfo(_expression, cancellationToken);

            return typeInfo.Type ?? typeInfo.ConvertedType ?? semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
        }
    }
}
