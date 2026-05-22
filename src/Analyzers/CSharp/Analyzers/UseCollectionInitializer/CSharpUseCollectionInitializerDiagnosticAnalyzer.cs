// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionExpression;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

using static SyntaxFactory;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseCollectionInitializerDiagnosticAnalyzer :
    AbstractUseCollectionInitializerDiagnosticAnalyzer<
        SyntaxKind,
        ExpressionSyntax,
        StatementSyntax,
        BaseObjectCreationExpressionSyntax,
        MemberAccessExpressionSyntax,
        InvocationExpressionSyntax,
        ExpressionStatementSyntax,
        // For C#, member assignments and `Add` invocations are both wrapped in
        // `ExpressionStatementSyntax`. Pass 3 of the IDE0017+IDE0028 unification re-uses this
        // slot for the unified walk's member-init detection — see the analyzer for details.
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        CSharpUseCollectionInitializerAnalyzer>
{
    protected override ISyntaxFacts SyntaxFacts
        => CSharpSyntaxFacts.Instance;

    // C# member-init fade includes the `.` operator token (`c.` is faded). VB's analogous
    // shape historically faded only the receiver (`c`). See the legacy
    // `CSharpUseObjectInitializerDiagnosticAnalyzer.FadeOutOperatorToken` for the equivalent.
    protected override bool FadeOutOperatorToken => true;

    protected override CSharpUseCollectionInitializerAnalyzer GetAnalyzer()
        => CSharpUseCollectionInitializerAnalyzer.Allocate();

    protected override bool HasExistingInvalidInitializerForCollectionExpression(BaseObjectCreationExpressionSyntax objectCreationExpression)
    {
        // Mirrors `CSharpUseCollectionInitializerAnalyzer.HasExistingInvalidInitializerForCollection`
        // but takes the object creation as a parameter so the diagnostic analyzer can call it
        // without going through the pooled walk's `_objectCreationExpression` field.
        // Pre-Pass-3 the walk-level method was reached implicitly via `ShouldAnalyze`'s
        // short-circuit; now the diagnostic analyzer's collection-expression branch
        // consults this check directly.
        if (objectCreationExpression.Initializer is InitializerExpressionSyntax(SyntaxKind.ObjectInitializerExpression)
            {
                Expressions: [var firstExpression, ..],
            })
        {
            if (firstExpression is AssignmentExpressionSyntax
                {
                    Left: ImplicitElementAccessSyntax { ArgumentList.Arguments.Count: 1 }
                } &&
                this.SyntaxFacts.SupportsKeyValuePairElement(objectCreationExpression.SyntaxTree.Options))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    protected override bool AreCollectionInitializersSupported(Compilation compilation)
        => compilation.LanguageVersion() >= LanguageVersion.CSharp3;

    protected override bool AreCollectionExpressionsSupported(Compilation compilation)
        => compilation.LanguageVersion().SupportsCollectionExpressions();

    protected override bool CanUseCollectionExpression(
        SemanticModel semanticModel,
        BaseObjectCreationExpressionSyntax objectCreationExpression,
        INamedTypeSymbol? expressionType,
        ImmutableArray<InitializerMatch<SyntaxNode>> preMatches,
        bool allowSemanticsChange,
        CancellationToken cancellationToken,
        out bool changesSemantics)
    {
        // Synthesize the final collection expression we would replace this object-creation with.  That will allow us to
        // determine if we end up calling the right overload in cases of overloaded methods.
        var replacement = CollectionExpression(SeparatedList(
            GetMatchElements(preMatches).Concat(GetInitializerElements(objectCreationExpression.Initializer))));

        return UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
            semanticModel, objectCreationExpression, replacement, expressionType, isSingletonInstance: false, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken, out changesSemantics);

        static IEnumerable<CollectionElementSyntax> GetMatchElements(ImmutableArray<InitializerMatch<SyntaxNode>> preMatches)
        {
            foreach (var match in preMatches)
            {
                if (match.Node is ExpressionSyntax expression)
                {
                    yield return match.UseSpread ? SpreadElement(expression) : ExpressionElement(expression);
                }
                else if (match.Node is ArgumentListSyntax argumentList)
                {
                    yield return WithElement(argumentList.WithoutTrivia());
                }
            }
        }

        static IEnumerable<CollectionElementSyntax> GetInitializerElements(InitializerExpressionSyntax? initializer)
        {
            if (initializer != null)
            {
                foreach (var expression in initializer.Expressions)
                {
                    // Enable when dictionary-expressions come online.
#if false
                    if (expression is InitializerExpressionSyntax { Expressions: [var keyExpression, var valueExpression1] })
                    {
                        // { k, v } -> [k: v]
                        yield return KeyValuePairElement(keyExpression, valueExpression1);
                    }
                    else if (expression is AssignmentExpressionSyntax
                    {
                        Left: ImplicitElementAccessSyntax { ArgumentList.Arguments: [var argument] },
                        Right: var valueExpression2,
                    })
                    {
                        // [k] = v -> [k: v]
                        yield return KeyValuePairElement(argument.Expression, valueExpression2);
                    }
                    else
                    {
                        yield return ExpressionElement(expression);
                    }
#else
                    yield return ExpressionElement(expression);
#endif
                }
            }
        }
    }

    protected override bool IsValidContainingStatement(StatementSyntax node)
    {
        // We don't want to offer this for using declarations because the way they are lifted means all
        // initialization is done before entering try block. For example
        // 
        // using var c = new List<int>() { 1 };
        //
        // is lowered to:
        //
        // var __c = new List<int>();
        // __c.Add(1);
        // var c = __c;
        // try
        // {
        // }
        // finally
        // {
        //     if (c != null)
        //     {
        //         ((IDisposable)c).Dispose();
        //     }
        // }
        //
        // As can be seen, if initializing throws any kind of exception, the newly created instance will not
        // be disposed properly.
        return node is not LocalDeclarationStatementSyntax localDecl ||
            localDecl.UsingKeyword == default;
    }
}
