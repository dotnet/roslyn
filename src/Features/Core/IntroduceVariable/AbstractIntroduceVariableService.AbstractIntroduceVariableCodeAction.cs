// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
            private readonly bool allOccurrences;
            private readonly bool isConstant;
            private readonly bool isLocal;
            private readonly bool isQueryLocal;
            private readonly TExpressionSyntax expression;
            private readonly SemanticDocument document;
            private readonly TService service;
            private readonly string title;

            private static Regex newlinePattern = new Regex(@"[\r\n]+", RegexOptions.Compiled);

            internal AbstractIntroduceVariableCodeAction(
                TService service,
                SemanticDocument document,
                TExpressionSyntax expression,
                bool allOccurrences,
                bool isConstant,
                bool isLocal,
                bool isQueryLocal)
            {
                this.service = service;
                this.document = document;
                this.expression = expression;
                this.allOccurrences = allOccurrences;
                this.isConstant = isConstant;
                this.isLocal = isLocal;
                this.isQueryLocal = isQueryLocal;
                this.title = CreateDisplayText(expression);
            }

            public override string Title
            {
                get { return this.title; }
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var changedDocument = await GetChangedDocumentCoreAsync(cancellationToken).ConfigureAwait(false);
                return await Simplifier.ReduceAsync(changedDocument, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            private async Task<Document> GetChangedDocumentCoreAsync(CancellationToken cancellationToken)
            {
                if (isQueryLocal)
                {
                    return await service.IntroduceQueryLocalAsync(this.document, this.expression, this.allOccurrences, cancellationToken).ConfigureAwait(false);
                }
                else if (isLocal)
                {
                    return await service.IntroduceLocalAsync(this.document, this.expression, this.allOccurrences, this.isConstant, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await IntroduceFieldAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task<Document> IntroduceFieldAsync(CancellationToken cancellationToken)
            {
                var result = await service.IntroduceFieldAsync(this.document, this.expression, this.allOccurrences, this.isConstant, cancellationToken).ConfigureAwait(false);
                return result.Item1;
            }

            private string CreateDisplayText(TExpressionSyntax expression)
            {
                var singleLineExpression = this.document.Project.LanguageServices.GetService<ISyntaxFactsService>().ConvertToSingleLine(expression);
                var nodeString = singleLineExpression.ToFullString().Trim();

                // prevent the display string from spanning multiple lines
                nodeString = newlinePattern.Replace(nodeString, " ");

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

                var formatString = isQueryLocal
                    ? allOccurrences
                        ? FeaturesResources.IntroduceQueryVariableForAll
                        : FeaturesResources.IntroduceQueryVariableFor
                    : formatStrings[allOccurrences ? 1 : 0, isConstant ? 1 : 0, isLocal ? 1 : 0];
                return string.Format(formatString, nodeString);
            }

            protected ITypeSymbol GetExpressionType(
                CancellationToken cancellationToken)
            {
                var semanticModel = document.SemanticModel;
                var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

                return typeInfo.Type ?? typeInfo.ConvertedType ?? semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
            }
        }
    }
}
