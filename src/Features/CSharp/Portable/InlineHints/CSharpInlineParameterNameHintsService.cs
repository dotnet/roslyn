// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;

namespace Microsoft.CodeAnalysis.CSharp.InlineHints
{
    /// <summary>
    /// The service to locate the positions in which the adornments should appear
    /// as well as associate the adornments back to the parameter name
    /// </summary>
    [ExportLanguageService(typeof(IInlineParameterNameHintsService), LanguageNames.CSharp), Shared]
    internal class CSharpInlineParameterNameHintsService : AbstractInlineParameterNameHintsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineParameterNameHintsService()
        {
        }

        protected override void AddAllParameterNameHintLocations(
             SemanticModel semanticModel,
             SyntaxNode node,
             Action<InlineParameterHint> addHint,
             bool hideForParametersThatDifferBySuffix,
             bool hideForParametersThatMatchMethodIntent,
             CancellationToken cancellationToken)
        {
            if (node is ArgumentSyntax argument)
            {
                if (argument.NameColon != null)
                    return;

                var parameter = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                if (string.IsNullOrEmpty(parameter?.Name))
                    return;

                if (hideForParametersThatMatchMethodIntent && MatchesMethodIntent(argument, parameter))
                    return;

                addHint(new InlineParameterHint(parameter.GetSymbolKey(cancellationToken), parameter.Name, argument.Span.Start, GetKind(argument.Expression)));
            }
            else if (node is AttributeArgumentSyntax attribute)
            {
                if (attribute.NameEquals != null || attribute.NameColon != null)
                    return;

                var parameter = attribute.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                if (string.IsNullOrEmpty(parameter?.Name))
                    return;

                addHint(new InlineParameterHint(parameter.GetSymbolKey(cancellationToken), parameter.Name, attribute.SpanStart, GetKind(attribute.Expression)));
            }
        }

        private static bool MatchesMethodIntent(ArgumentSyntax argument, IParameterSymbol parameter)
        {
            // Methods like `SetColor(color: "y")` `FromResult(result: "x")` `Enable/DisablePolling(bool)` don't need
            // parameter names to improve clarity.  The parameter is clear from the context of the method name.
            if (argument.Parent is not ArgumentListSyntax argumentList)
                return false;

            if (argumentList.Arguments[0] != argument)
                return false;

            if (argumentList.Parent is not InvocationExpressionSyntax invocationExpression)
                return false;

            var invokedExpression = invocationExpression.Expression;
            var rightMostName = invokedExpression.GetRightmostName();
            if (rightMostName == null)
                return false;

            return MatchesMethodIntent(rightMostName.Identifier.ValueText, parameter);
        }

        private static InlineParameterHintKind GetKind(ExpressionSyntax arg)
            => arg switch
            {
                LiteralExpressionSyntax or InterpolatedStringExpressionSyntax => InlineParameterHintKind.Literal,
                ObjectCreationExpressionSyntax => InlineParameterHintKind.ObjectCreation,
                CastExpressionSyntax cast => GetKind(cast.Expression),
                PrefixUnaryExpressionSyntax prefix => GetKind(prefix.Operand),
                _ => InlineParameterHintKind.Other,
            };
    }
}
