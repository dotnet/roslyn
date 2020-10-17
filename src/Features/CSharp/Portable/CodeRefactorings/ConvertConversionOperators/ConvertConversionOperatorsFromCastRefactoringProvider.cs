// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ConvertConversionOperators;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertConversionOperators
{
    /// <summary>
    /// Refactor:
    ///     var o = (object)1;
    ///
    /// Into:
    ///     var o = 1 as object;
    ///
    /// Or:
    ///     visa versa
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertConversionOperators), Shared]
    internal partial class CSharpConvertConversionOperatorsFromCastRefactoringProvider
        : AbstractConvertConversionOperatorsRefactoringProvider<CastExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertConversionOperatorsFromCastRefactoringProvider()
        {
        }

        protected override async Task<ImmutableArray<CastExpressionSyntax>> FilterFromExpressionCandidatesAsync(ImmutableArray<CastExpressionSyntax> castExpressions, Document document, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            castExpressions = (from node in castExpressions
                               let type = semanticModel.GetTypeInfo(node, cancellationToken).Type
                               where type != null && !type.IsValueType
                               select node)
                               .ToImmutableArray();

            return castExpressions;
        }

        protected override string GetTitle()
            => "TODO";

        protected override SyntaxNode ConvertExpression(CastExpressionSyntax castExpression)
        {
            var typeNode = castExpression.Type;
            var expression = castExpression.Expression;

            var asExpression = BinaryExpression(SyntaxKind.AsExpression, expression, typeNode);

            return asExpression;
        }
    }
}
