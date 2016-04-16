// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax>
    {
        internal abstract class AbstractIntroduceVariableCodeAction : CodeAction
        {
            private readonly bool _allOccurrences;
            private readonly bool _isConstant;
            private readonly bool _isLocal;
            private readonly bool _isQueryLocal;
            private readonly TExpressionSyntax _expression;
            private readonly SemanticDocument _document;
            private readonly TService _service;
            private readonly string _title;

            private static readonly Regex s_newlinePattern = new Regex(@"[\r\n]+");

            internal AbstractIntroduceVariableCodeAction(
                TService service,
                SemanticDocument document,
                TExpressionSyntax expression,
                bool allOccurrences,
                bool isConstant,
                bool isLocal,
                bool isQueryLocal)
            {
                _service = service;
                _document = document;
                _expression = expression;
                _allOccurrences = allOccurrences;
                _isConstant = isConstant;
                _isLocal = isLocal;
                _isQueryLocal = isQueryLocal;
                _title = CreateDisplayText(expression);
            }

            public override string Title
            {
                get { return _title; }
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var changedDocument = await GetChangedDocumentCoreAsync(cancellationToken).ConfigureAwait(false);
                return await Simplifier.ReduceAsync(changedDocument, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            private async Task<Document> GetChangedDocumentCoreAsync(CancellationToken cancellationToken)
            {
                if (_isQueryLocal)
                {
                    return await _service.IntroduceQueryLocalAsync(_document, _expression, _allOccurrences, cancellationToken).ConfigureAwait(false);
                }
                else if (_isLocal)
                {
                    return await _service.IntroduceLocalAsync(_document, _expression, _allOccurrences, _isConstant, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await IntroduceFieldAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task<Document> IntroduceFieldAsync(CancellationToken cancellationToken)
            {
                var result = await _service.IntroduceFieldAsync(_document, _expression, _allOccurrences, _isConstant, cancellationToken).ConfigureAwait(false);
                return result.Item1;
            }

            private string CreateDisplayText(TExpressionSyntax expression)
            {
                var singleLineExpression = _document.Project.LanguageServices.GetService<ISyntaxFactsService>().ConvertToSingleLine(expression);
                var nodeString = singleLineExpression.ToFullString().Trim();

                // prevent the display string from spanning multiple lines
                nodeString = s_newlinePattern.Replace(nodeString, " ");

                // prevent the display string from being too long
                const int MaxLength = 40;
                if (nodeString.Length > MaxLength)
                {
                    nodeString = nodeString.Substring(0, MaxLength) + "...";
                }

                return CreateDisplayText(nodeString);
            }

            private string CreateDisplayText(string nodeString)
            {
                // Indexed by: allOccurrences, isConstant, isLocal
                var formatStrings = new string[2, 2, 2]
                {
                  {
                    { FeaturesResources.IntroduceFieldFor, FeaturesResources.IntroduceLocalFor },
                    { FeaturesResources.IntroduceConstantFor, FeaturesResources.IntroduceLocalConstantFor }
                  },
                  {
                    { FeaturesResources.IntroduceFieldForAllOccurrences,  FeaturesResources.IntroduceLocalForAllOccurrences },
                    { FeaturesResources.IntroduceConstantForAllOccurrences, FeaturesResources.IntroduceLocalConstantForAll }
                  }
                };

                var formatString = _isQueryLocal
                    ? _allOccurrences
                        ? FeaturesResources.IntroduceQueryVariableForAll
                        : FeaturesResources.IntroduceQueryVariableFor
                    : formatStrings[_allOccurrences ? 1 : 0, _isConstant ? 1 : 0, _isLocal ? 1 : 0];
                return string.Format(formatString, nodeString);
            }

            protected ITypeSymbol GetExpressionType(
                CancellationToken cancellationToken)
            {
                var semanticModel = _document.SemanticModel;
                var typeInfo = semanticModel.GetTypeInfo(_expression, cancellationToken);

                return typeInfo.Type ?? typeInfo.ConvertedType ?? semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
            }
        }
    }
}
