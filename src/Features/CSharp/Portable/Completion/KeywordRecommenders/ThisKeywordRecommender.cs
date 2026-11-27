// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class ThisKeywordRecommender() : AbstractSyntacticSingleKeywordRecommender(SyntaxKind.ThisKeyword)
{
    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        => IsInstanceExpressionOrStatement(context) ||
           IsThisParameterModifierContext(context) ||
           IsConstructorInitializerContext(context);

    private static bool IsInstanceExpressionOrStatement(CSharpSyntaxContext context)
        => context.IsInstanceContext && (context.IsNonAttributeExpressionContext || context.IsStatementContext);

    private static bool IsConstructorInitializerContext(CSharpSyntaxContext context)
    {
        // cases:
        //   Goo() : |

        var token = context.TargetToken;

        return
            token.Kind() == SyntaxKind.ColonToken &&
            token.Parent is ConstructorInitializerSyntax { Parent: ConstructorDeclarationSyntax constructor } &&
            !constructor.Modifiers.Any(SyntaxKind.StaticKeyword);
    }

    private static bool IsThisParameterModifierContext(CSharpSyntaxContext context)
    {
        if (context.SyntaxTree.IsParameterModifierContext(
                context.Position, context.LeftToken, includeOperators: false, out var parameterIndex, out var previousModifier))
        {
            if (previousModifier
                    is SyntaxKind.None
                    or SyntaxKind.RefKeyword
                    or SyntaxKind.InKeyword
                    or SyntaxKind.ReadOnlyKeyword)
            {
                if (parameterIndex == 0 &&
                    context.SyntaxTree.IsPossibleExtensionMethodContext(context.LeftToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    protected override bool ShouldPreselect(CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        var outerType = context.SemanticModel.GetEnclosingNamedType(context.Position, cancellationToken);
        return context.InferredTypes.Any(static (t, outerType) => Equals(t, outerType), outerType);
    }
}
