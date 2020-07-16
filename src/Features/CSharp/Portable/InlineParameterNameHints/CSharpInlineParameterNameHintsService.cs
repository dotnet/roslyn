// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineParameterNameHints;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InlineParameterNameHints
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

        protected override IEnumerable<InlineParameterHint> AddAllParameterNameHintLocations(
             SemanticModel semanticModel, IEnumerable<SyntaxNode> nodes, CancellationToken cancellationToken)
        {
            var spans = new List<InlineParameterHint>();
            foreach (var node in nodes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return SpecializedCollections.EmptyEnumerable<InlineParameterHint>();
                }

                if (node is ArgumentSyntax argument)
                {
                    if (argument.NameColon == null && IsExpressionWithNoName(argument.Expression))
                    {
                        var param = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                        if (param != null && param.Name != "")
                        {
                            spans.Add(new InlineParameterHint(param.Name, argument.Span.Start));
                        }
                    }
                }
                else if (node is AttributeArgumentSyntax attribute)
                {
                    if (attribute.NameEquals == null && attribute.NameColon == null && IsExpressionWithNoName(attribute.Expression))
                    {
                        var param = attribute.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                        if (param != null && param.Name != "")
                        {
                            spans.Add(new InlineParameterHint(param.Name, attribute.SpanStart));
                        }
                    }
                }
            }
            return spans;
        }

        /// <summary>
        /// Determines if the argument is of a type that should have an adornment appended
        /// </summary>
        /// <param name="arg">The argument that is being looked at</param>
        /// <returns>true when the adornment should be added</returns>
        private static bool IsExpressionWithNoName(ExpressionSyntax arg)
        {
            if (arg is LiteralExpressionSyntax)
            {
                // We want to adorn literals no matter what
                return true;
            }
            if (arg is ObjectCreationExpressionSyntax)
            {
                // We want to adorn object invocations that exist as arguments because they are not declared anywhere
                // else in the file
                // Example: testMethod(^new Object()); should show the adornment at the caret  
                return true;
            }
            if (arg is CastExpressionSyntax cast)
            {
                // Recurse until we find a literal
                // If so, then we should add the adornment
                return IsExpressionWithNoName(cast.Expression);
            }
            if (arg is PrefixUnaryExpressionSyntax negation)
            {
                // Recurse until we find a literal
                // If so, then we should add the adornment
                return IsExpressionWithNoName(negation.Operand);
            }

            return false;
        }
    }
}
