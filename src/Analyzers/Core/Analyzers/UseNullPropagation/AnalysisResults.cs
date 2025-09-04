// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.UseNullPropagation;

internal abstract partial class AbstractUseNullPropagationDiagnosticAnalyzer<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TConditionalExpressionSyntax,
    TBinaryExpressionSyntax,
    TInvocationExpressionSyntax,
    TConditionalAccessExpressionSyntax,
    TElementAccessExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TIfStatementSyntax,
    TExpressionStatementSyntax> where TSyntaxKind : struct
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TConditionalExpressionSyntax : TExpressionSyntax
    where TBinaryExpressionSyntax : TExpressionSyntax
    where TInvocationExpressionSyntax : TExpressionSyntax
    where TConditionalAccessExpressionSyntax : TExpressionSyntax
    where TElementAccessExpressionSyntax : TExpressionSyntax
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TIfStatementSyntax : TStatementSyntax
    where TExpressionStatementSyntax : TStatementSyntax
{
    public readonly struct ConditionalExpressionAnalysisResult(TExpressionSyntax conditionPartToCheck, TExpressionSyntax whenPartToCheck, ImmutableDictionary<string, string?> properties)
    {
        public TExpressionSyntax ConditionPartToCheck { get; } = conditionPartToCheck;
        public TExpressionSyntax WhenPartToCheck { get; } = whenPartToCheck;
        public ImmutableDictionary<string, string?> Properties { get; } = properties;
    }

    public readonly struct IfStatementAnalysisResult(TStatementSyntax trueStatement, TExpressionSyntax whenPartMatch, TStatementSyntax? nullAssignmentOpt, ImmutableDictionary<string, string?> properties)
    {
        public TStatementSyntax TrueStatement { get; } = trueStatement;
        public TExpressionSyntax WhenPartMatch { get; } = whenPartMatch;
        public TStatementSyntax? NullAssignmentOpt { get; } = nullAssignmentOpt;
        public ImmutableDictionary<string, string?> Properties { get; } = properties;
    }
}
