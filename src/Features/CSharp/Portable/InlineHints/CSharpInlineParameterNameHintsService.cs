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
using Microsoft.CodeAnalysis.PooledObjects;

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
             ArrayBuilder<InlineParameterHint> buffer,
             CancellationToken cancellationToken)
        {
            if (node is BaseArgumentListSyntax argumentList)
            {
                AddArguments(semanticModel, buffer, argumentList, cancellationToken);
            }
            else if (node is AttributeArgumentListSyntax attributeArgumentList)
            {
                AddArguments(semanticModel, buffer, attributeArgumentList, cancellationToken);
            }
        }

        private static void AddArguments(SemanticModel semanticModel, ArrayBuilder<InlineParameterHint> buffer, AttributeArgumentListSyntax argumentList, CancellationToken cancellationToken)
        {
            foreach (var argument in argumentList.Arguments)
            {
                if (argument.NameEquals != null || argument.NameColon != null)
                    continue;

                var parameter = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                buffer.Add(new InlineParameterHint(
                    parameter,
                    argument.Span.Start,
                    GetKind(argument.Expression)));
            }
        }

        private static void AddArguments(SemanticModel semanticModel, ArrayBuilder<InlineParameterHint> buffer, BaseArgumentListSyntax argumentList, CancellationToken cancellationToken)
        {
            foreach (var argument in argumentList.Arguments)
            {
                if (argument.NameColon != null)
                    continue;

                var parameter = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                buffer.Add(new InlineParameterHint(
                    parameter,
                    argument.Span.Start,
                    GetKind(argument.Expression)));
            }
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
