// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseRecursivePatterns
{
    using static SyntaxKind;
    using static SyntaxFactory;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseRecursivePatterns), Shared]
    internal sealed class UseRecursivePatternsCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UseRecursivePatternsCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            if (textSpan.Length > 0)
            {
                return;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var node = root.FindToken(textSpan.Start).Parent;
            var replacementFunc = GetReplacementFunc(node, semanticModel);
            if (replacementFunc is null)
                return;

            context.RegisterRefactoring(
                new MyCodeAction(
                    "Use recursive patterns",
                    _ => Task.FromResult(document.WithSyntaxRoot(replacementFunc(root)))));
        }

        private static Func<SyntaxNode, SyntaxNode>? GetReplacementFunc(SyntaxNode? node, SemanticModel semanticModel)
        {
            return node switch
            {
                BinaryExpressionSyntax(LogicalAndExpression) logicalAnd
                    => CombineLogicalAndOperands(logicalAnd, semanticModel),
                CasePatternSwitchLabelSyntax { WhenClause: { } whenClause } switchLabel
                    => Combine(switchLabel, switchLabel.Pattern, whenClause.Condition, semanticModel),
                SwitchExpressionArmSyntax { WhenClause: { } whenClause } switchArm
                    => Combine(switchArm, switchArm.Pattern, whenClause.Condition, semanticModel),
                WhenClauseSyntax { Parent: CasePatternSwitchLabelSyntax switchLabel } whenClause
                    => Combine(switchLabel, switchLabel.Pattern, whenClause.Condition, semanticModel),
                WhenClauseSyntax { Parent: SwitchExpressionArmSyntax switchArm } whenClause
                    => Combine(switchArm, switchArm.Pattern, whenClause.Condition, semanticModel),
                _ => null
            };
        }

        private static Func<SyntaxNode, SyntaxNode>? CombineLogicalAndOperands(BinaryExpressionSyntax logicalAnd, SemanticModel semanticModel)
        {
            if (logicalAnd.Left is IsPatternExpressionSyntax left)
            {
                return Combine(logicalAnd, left.Pattern, logicalAnd.Right, semanticModel);
            }

            if (TryDetermineReceiver(logicalAnd.Left, semanticModel) is not var (leftReceiver, leftTarget, leftFlipped) ||
                TryDetermineReceiver(logicalAnd.Right, semanticModel) is not var (rightReceiver, rightTarget, rightFlipped) ||
                TryGetCommonReceiver(leftReceiver, rightReceiver, semanticModel) is not var (commonReceiver, leftNames, rightNames) ||
                leftNames.IsDefault ||
                rightNames.IsDefault)
            {
                return null;
            }

            return root => root.ReplaceNode(logicalAnd, IsPatternExpression(commonReceiver, RecursivePattern(
                CreateSubpattern(leftNames, CreatePattern(logicalAnd.Left, leftTarget, leftFlipped)),
                CreateSubpattern(rightNames, CreatePattern(logicalAnd.Right, rightTarget, rightFlipped)))));
        }

        private static Func<SyntaxNode, SyntaxNode>? Combine(SyntaxNode nodeToReplace, PatternSyntax pattern, ExpressionSyntax expression, SemanticModel semanticModel)
        {
            Debug.Assert(nodeToReplace is
                SwitchExpressionArmSyntax or
                CasePatternSwitchLabelSyntax or
                BinaryExpressionSyntax(LogicalAndExpression) { Left: IsPatternExpressionSyntax });

            if (TryDetermineReceiver(expression, semanticModel) is not var (receiver, target, flipped) ||
                GetInnermostReceiver(receiver, semanticModel) is not IdentifierNameSyntax identifierName)
            {
                return null;
            }

            var designation = pattern.DescendantNodes()
                .OfType<SingleVariableDesignationSyntax>()
                .Where(d => AreEquivalent(d.Identifier, identifierName.Identifier))
                .FirstOrDefault();

            if (!designation.IsParentKind(SyntaxKind.VarPattern, SyntaxKind.DeclarationPattern, SyntaxKind.RecursivePattern))
                return null;

            RoslynDebug.AssertNotNull(designation.Parent);

            var names = GetParentNames(identifierName);
            if (names.IsDefault)
                return null;

            return root =>
            {
                var newParent = CreateDesignationParent(designation, CreateSubpattern(names, CreatePattern(expression, target, flipped)));
                return root.ReplaceNode(nodeToReplace, nodeToReplace.ReplaceNode(designation.Parent, newParent) switch
                {
                    SwitchExpressionArmSyntax switchArm => switchArm.WithWhenClause(null).WithAdditionalAnnotations(Formatter.Annotation),
                    CasePatternSwitchLabelSyntax switchLabel => switchLabel.WithWhenClause(null).WithAdditionalAnnotations(Formatter.Annotation),
                    var node => node
                });
            };

            static PatternSyntax CreateDesignationParent(SingleVariableDesignationSyntax designation, SubpatternSyntax subpattern)
            {
                return designation.Parent switch
                {
                    VarPatternSyntax p => RecursivePattern(properties: PropertyPatternClause(SingletonSeparatedList(subpattern)), designation: designation),
                    DeclarationPatternSyntax p => RecursivePattern(p.Type, PropertyPatternClause(SingletonSeparatedList(subpattern)), designation: designation),
                    RecursivePatternSyntax p => p.AddPropertyPatternClauseSubpatterns(subpattern),
                    var p => throw ExceptionUtilities.UnexpectedValue(p?.Kind())
                };
            }
        }

        private static PatternSyntax CreatePattern(SyntaxNode node, ExpressionSyntax constant, bool flipped)
        {
            return node switch
            {
                BinaryExpressionSyntax(EqualsExpression) => ConstantPattern(constant),
                BinaryExpressionSyntax(NotEqualsExpression) => UnaryPattern(ConstantPattern(constant)),
                BinaryExpressionSyntax(GreaterThanExpression or
                                       GreaterThanOrEqualExpression or
                                       LessThanOrEqualExpression or
                                       LessThanExpression) e
                    => RelationalPattern(flipped ? Flip(e.OperatorToken) : e.OperatorToken, constant),
                var v => throw ExceptionUtilities.UnexpectedValue(v.Kind()),
            };

            static SyntaxToken Flip(SyntaxToken token)
            {
                var kind = token.Kind() switch
                {
                    LessThanToken => GreaterThanToken,
                    LessThanEqualsToken => GreaterThanEqualsToken,
                    GreaterThanEqualsToken => LessThanEqualsToken,
                    GreaterThanToken => LessThanToken,
                    var v => throw ExceptionUtilities.UnexpectedValue(v)
                };
                return Token(token.LeadingTrivia, kind, token.TrailingTrivia);
            }
        }

        private static (ExpressionSyntax Receiver, ExpressionSyntax Target, bool Flipped)? TryDetermineReceiver(
            ExpressionSyntax node,
            SemanticModel semanticModel)
        {
            return node switch
            {
                BinaryExpressionSyntax(EqualsExpression or
                                       NotEqualsExpression or
                                       GreaterThanExpression or
                                       GreaterThanOrEqualExpression or
                                       LessThanOrEqualExpression or
                                       LessThanExpression) e
                    => TryDetermineConstant(e, semanticModel),
                _ => null,
            };
        }

        private static (ExpressionSyntax Other, ExpressionSyntax Constant, bool Flipped)? TryDetermineConstant(
            BinaryExpressionSyntax node,
            SemanticModel semanticModel)
        {
            return (node.Left, node.Right) switch
            {
                var (left, right) when semanticModel.GetConstantValue(left).HasValue => (right, left, true),
                var (left, right) when semanticModel.GetConstantValue(right).HasValue => (left, right, false),
                _ => null
            };
        }

        private static SubpatternSyntax CreateSubpattern(ImmutableArray<IdentifierNameSyntax> names, PatternSyntax pattern)
        {
            return names.Aggregate(
                pattern,
                (pattern, name) => Subpattern(NameColon(name), pattern),
                subpattern => RecursivePattern(subpattern));
        }

        public static RecursivePatternSyntax RecursivePattern(SubpatternSyntax subpattern)
        {
            return RecursivePattern(properties: PropertyPatternClause(SingletonSeparatedList(subpattern)));
        }

        public static RecursivePatternSyntax RecursivePattern(params SubpatternSyntax[] subpatterns)
        {
            return RecursivePattern(properties: PropertyPatternClause(SeparatedList(subpatterns)));
        }

        public static RecursivePatternSyntax RecursivePattern(
            TypeSyntax? type = null,
            PropertyPatternClauseSyntax? properties = null,
            VariableDesignationSyntax? designation = null)
        {
            return SyntaxFactory.RecursivePattern(type, positionalPatternClause: null, properties, designation);
        }

        /// <summary>
        /// Obtain the outermost common receiver between two expressions.
        /// </summary>
        private static (ExpressionSyntax CommonReceiver,
                        ImmutableArray<IdentifierNameSyntax> LeftNames,
                        ImmutableArray<IdentifierNameSyntax> RightNames)? TryGetCommonReceiver(
            ExpressionSyntax left,
            ExpressionSyntax right,
            SemanticModel semanticModel)
        {
            // First, we walk downwards to get the innermost receiver.
            var leftReceiver = GetInnermostReceiver(left, semanticModel);
            var rightReceiver = GetInnermostReceiver(right, semanticModel);

            // If we don't have a common starting point, bail - there's no common receiver.
            if (!AreEquivalent(leftReceiver, rightReceiver))
            {
                return null;
            }

            // Otherwise, walk upwards to capture the outermost common receiver.
            // We do this to decrease noise on superfluous subpatterns.
            while (leftReceiver.Parent is ExpressionSyntax leftParent &&
                   rightReceiver.Parent is ExpressionSyntax rightParent &&
                   AreEquivalent(leftParent, rightParent))
            {
                leftReceiver = leftParent;
                rightReceiver = rightParent;
            }

            // Collect any names on the right-hand-side of the final receiver that we want to convert to subpatterns.
            return (leftReceiver, GetParentNames(leftReceiver), GetParentNames(rightReceiver));
        }

        /// <summary>
        /// Walks up the recever and collect any names on the right-hand-side. 
        /// These are already verified in <see cref="GetInnermostReceiver(ExpressionSyntax, SemanticModel)"/> below to have a proper name.
        /// </summary>
        private static ImmutableArray<IdentifierNameSyntax> GetParentNames(ExpressionSyntax node)
        {
            using var _ = ArrayBuilder<IdentifierNameSyntax>.GetInstance(out var names);
            CollectNames(node.Parent, names);
            return names.ToImmutableOrNull();

            // We collect names inside out, so that the outermost name ends up in the innermost subpattern.
            static void CollectNames(SyntaxNode? node, ArrayBuilder<IdentifierNameSyntax> names)
            {
                switch (node)
                {
                    case MemberAccessExpressionSyntax(SimpleMemberAccessExpression) { Name: IdentifierNameSyntax name } memberAccess:
                        CollectNames(memberAccess.Parent, names);
                        names.Add(name);
                        break;
                    case ConditionalAccessExpressionSyntax { WhenNotNull: IdentifierNameSyntax name } memberAccess:
                        CollectNames(memberAccess.Parent, names);
                        names.Add(name);
                        break;
                }
            }
        }

        /// <summary>
        /// Obtain the innermost receiver that is on the left of a member access, but cannot be coverted to a property pattern.
        /// </summary>
        private static ExpressionSyntax GetInnermostReceiver(ExpressionSyntax node, SemanticModel semanticModel)
        {
            switch (node)
            {
                case MemberAccessExpressionSyntax(SimpleMemberAccessExpression) { Name: IdentifierNameSyntax name } memberAccess
                    when semanticModel.GetSymbolInfo(name).Symbol?.IsStatic == false:
                    return GetInnermostReceiver(memberAccess.Expression, semanticModel);
                case ConditionalAccessExpressionSyntax { WhenNotNull: IdentifierNameSyntax name } memberAccess
                    when semanticModel.GetSymbolInfo(name).Symbol?.IsStatic == false:
                    return GetInnermostReceiver(memberAccess.Expression, semanticModel);
                default:
                    return node;
            }
        }

        private sealed class MyCodeAction : CodeActions.CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
