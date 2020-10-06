// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
             SemanticModel semanticModel, IEnumerable<SyntaxNode> nodes,
             ArrayBuilder<InlineParameterHint> result, CancellationToken cancellationToken)
        {
            foreach (var node in nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (node is ArgumentSyntax argument)
                {
                    if (argument.NameColon == null)
                    {
                        var param = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                        if (!string.IsNullOrEmpty(param?.Name))
                            result.Add(new InlineParameterHint(param.GetSymbolKey(cancellationToken), param.Name, argument.Span.Start, GetKind(argument.Expression)));
                    }
                }
                else if (node is AttributeArgumentSyntax attribute)
                {
                    if (attribute.NameEquals == null && attribute.NameColon == null)
                    {
                        var param = attribute.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                        if (!string.IsNullOrEmpty(param?.Name))
                            result.Add(new InlineParameterHint(param.GetSymbolKey(cancellationToken), param.Name, attribute.SpanStart, GetKind(attribute.Expression)));
                    }
                }
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
