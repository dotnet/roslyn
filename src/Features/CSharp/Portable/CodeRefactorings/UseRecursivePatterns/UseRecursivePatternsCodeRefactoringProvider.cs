// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
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
                    => CombineWhenClauseCondition(switchLabel.Pattern, whenClause.Condition, semanticModel),
                SwitchExpressionArmSyntax { WhenClause: { } whenClause } switchArm
                    => CombineWhenClauseCondition(switchArm.Pattern, whenClause.Condition, semanticModel),
                WhenClauseSyntax { Parent: CasePatternSwitchLabelSyntax switchLabel } whenClause
                    => CombineWhenClauseCondition(switchLabel.Pattern, whenClause.Condition, semanticModel),
                WhenClauseSyntax { Parent: SwitchExpressionArmSyntax switchArm } whenClause
                    => CombineWhenClauseCondition(switchArm.Pattern, whenClause.Condition, semanticModel),
                _ => null
            };
        }

        private static Func<SyntaxNode, SyntaxNode>? CombineLogicalAndOperands(BinaryExpressionSyntax logicalAnd, SemanticModel semanticModel)
        {
            if (TryDetermineReceiver(logicalAnd.Left, semanticModel, inWhenClause: false) is not var (leftReceiver, leftTarget, leftFlipped) ||
                TryDetermineReceiver(logicalAnd.Right, semanticModel, inWhenClause: false) is not var (rightReceiver, rightTarget, rightFlipped))
            {
                return null;
            }

            switch (leftTarget, rightTarget)
            {
                case (ExpressionSyntax leftConstant, ExpressionSyntax rightConstant):
                    if (TryGetCommonReceiver(leftReceiver, rightReceiver, semanticModel) is not (var commonReceiver, var leftNames, var rightNames))
                        return null;

                    return root =>
                    {
                        ExpressionSyntax replacement =
                            IsPatternExpression(commonReceiver, RecursivePattern(
                                CreateSubpattern(leftNames, CreatePattern(leftReceiver, leftConstant, leftFlipped)),
                                CreateSubpattern(rightNames, CreatePattern(rightReceiver, rightConstant, rightFlipped))));
                        return root.ReplaceNode(logicalAnd, AdjustBinaryExpressionOperands(logicalAnd, replacement));
                    };

                case (PatternSyntax leftPattern, _):
                    if (TryFindDesignation(leftPattern, rightReceiver, semanticModel) is not var (designation, names))
                        return null;

                    RoslynDebug.AssertNotNull(leftPattern.Parent);
                    RoslynDebug.AssertNotNull(designation.Parent);

                    return root =>
                    {
                        var containingPattern = RewriteContainingPattern(rightReceiver, rightTarget, rightFlipped, designation, names);
                        var replacement = ((IsPatternExpressionSyntax)leftPattern.Parent).ReplaceNode(designation.Parent, containingPattern);
                        return root.ReplaceNode(logicalAnd, AdjustBinaryExpressionOperands(logicalAnd, replacement));
                    };

                default:
                    return null;
            }
        }

        private static ExpressionSyntax AdjustBinaryExpressionOperands(BinaryExpressionSyntax logicalAnd, ExpressionSyntax replacement)
        {
            if (logicalAnd.Left is BinaryExpressionSyntax(LogicalAndExpression) leftExpression)
                replacement = leftExpression.WithRight(replacement);
            return replacement.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static Func<SyntaxNode, SyntaxNode>? CombineWhenClauseCondition(PatternSyntax pattern, ExpressionSyntax condition, SemanticModel semanticModel)
        {
            if (TryDetermineReceiver(condition, semanticModel, inWhenClause: true) is not var (receiver, target, flipped) ||
                TryFindDesignation(pattern, receiver, semanticModel) is not var (designation, names))
            {
                return null;
            }

            RoslynDebug.AssertNotNull(designation.Parent);
            RoslynDebug.AssertNotNull(receiver.Parent?.Parent);

            return root =>
            {
                var editor = new SyntaxEditor(root, CSharpSyntaxGenerator.Instance);
                switch (receiver.Parent.Parent)
                {
                    case BinaryExpressionSyntax(LogicalAndExpression) logicalAnd:
                        editor.ReplaceNode(logicalAnd, logicalAnd.Right);
                        break;
                    case WhenClauseSyntax whenClause:
                        editor.RemoveNode(whenClause, SyntaxRemoveOptions.AddElasticMarker);
                        break;
                    case var v:
                        throw ExceptionUtilities.UnexpectedValue(v.Kind());
                }

                var containingPattern = RewriteContainingPattern(receiver, target, flipped, designation, names);
                editor.ReplaceNode(designation.Parent, containingPattern);
                return editor.GetChangedRoot();
            };
        }

        private static PatternSyntax RewriteContainingPattern(
            ExpressionSyntax receiver, ExpressionOrPatternSyntax target, bool flipped,
            SingleVariableDesignationSyntax designation, ImmutableArray<IdentifierNameSyntax> names)
        {
            var subpattern = CreateSubpattern(names, target switch
            {
                ExpressionSyntax constant => CreatePattern(receiver, constant, flipped),
                PatternSyntax pattern => pattern,
                var v => throw ExceptionUtilities.UnexpectedValue(v.Kind()),
            });

            RoslynDebug.AssertNotNull(designation.Parent);

            return designation.Parent switch
            {
                VarPatternSyntax => RecursivePattern(properties: PropertyPatternClause(SingletonSeparatedList(subpattern)), designation: designation),
                DeclarationPatternSyntax p => RecursivePattern(p.Type, PropertyPatternClause(SingletonSeparatedList(subpattern)), designation: designation),
                RecursivePatternSyntax p => p.AddPropertyPatternClauseSubpatterns(subpattern),
                var p => throw ExceptionUtilities.UnexpectedValue(p.Kind())
            };
        }

        private static (SingleVariableDesignationSyntax Designation, ImmutableArray<IdentifierNameSyntax> Names)?
            TryFindDesignation(PatternSyntax leftPattern, ExpressionSyntax rightReceiver, SemanticModel semanticModel)
        {
            if (GetInnermostReceiver(rightReceiver, semanticModel) is not IdentifierNameSyntax identifierName)
                return null;

            var designation = leftPattern.DescendantNodes()
                .OfType<SingleVariableDesignationSyntax>()
                .Where(d => AreEquivalent(d.Identifier, identifierName.Identifier))
                .FirstOrDefault();

            if (!designation.IsParentKind(SyntaxKind.VarPattern, SyntaxKind.DeclarationPattern, SyntaxKind.RecursivePattern))
                return null;

            if (!TryGetParentNames(identifierName, out var names))
                return null;

            return (designation, names);
        }

        private static PatternSyntax CreatePattern(ExpressionSyntax receiver, ExpressionSyntax target, bool flipped)
        {
            return receiver.Parent switch
            {
                BinaryExpressionSyntax(EqualsExpression) => ConstantPattern(target),
                BinaryExpressionSyntax(NotEqualsExpression) => UnaryPattern(ConstantPattern(target)),
                BinaryExpressionSyntax(GreaterThanExpression or
                                       GreaterThanOrEqualExpression or
                                       LessThanOrEqualExpression or
                                       LessThanExpression) e
                    => RelationalPattern(flipped ? Flip(e.OperatorToken) : e.OperatorToken, target),
                var v => throw ExceptionUtilities.UnexpectedValue(v?.Kind()),
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

        private static (ExpressionSyntax Receiver, ExpressionOrPatternSyntax Target, bool Flipped)? TryDetermineReceiver(
            ExpressionSyntax node,
            SemanticModel semanticModel,
            bool inWhenClause)
        {
            return node switch
            {
                BinaryExpressionSyntax(LogicalAndExpression) expr
                    => TryDetermineReceiver(inWhenClause ? expr.Left : expr.Right, semanticModel, inWhenClause),
                BinaryExpressionSyntax(EqualsExpression or
                                       NotEqualsExpression or
                                       GreaterThanExpression or
                                       GreaterThanOrEqualExpression or
                                       LessThanOrEqualExpression or
                                       LessThanExpression) expr
                    => TryDetermineConstant(expr, semanticModel),
                IsPatternExpressionSyntax expr
                    => (expr.Expression, expr.Pattern, false),
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
            if (!TryGetParentNames(leftReceiver, out var leftNames) ||
                !TryGetParentNames(rightReceiver, out var rightNames))
            {
                return null;
            }

            return (leftReceiver, leftNames, rightNames);
        }

        /// <summary>
        /// Walks up the recever and collect any names on the right-hand-side. 
        /// These are already verified in <see cref="GetInnermostReceiver(ExpressionSyntax, SemanticModel)"/> below to have a proper name.
        /// </summary>
        private static bool TryGetParentNames(ExpressionSyntax node, out ImmutableArray<IdentifierNameSyntax> names)
        {
            using var _ = ArrayBuilder<IdentifierNameSyntax>.GetInstance(out var builder);
            CollectNames(node.Parent, builder);
            names = builder.ToImmutableOrNull();
            return !names.IsDefault;

            // We collect names inside out, so that the outermost name ends up in the innermost subpattern.
            static void CollectNames(SyntaxNode? node, ArrayBuilder<IdentifierNameSyntax> builder)
            {
                switch (node)
                {
                    case MemberAccessExpressionSyntax(SimpleMemberAccessExpression) { Name: IdentifierNameSyntax name } memberAccess:
                        CollectNames(memberAccess.Parent, builder);
                        builder.Add(name);
                        break;
                    case ConditionalAccessExpressionSyntax { WhenNotNull: IdentifierNameSyntax name } memberAccess:
                        CollectNames(memberAccess.Parent, builder);
                        builder.Add(name);
                        break;
                }
            }
        }

        /// <summary>
        /// Obtain the innermost receiver that is on the left of a member access, but cannot be coverted to a property pattern.
        /// </summary>
        private static ExpressionSyntax GetInnermostReceiver(ExpressionSyntax node, SemanticModel semanticModel)
        {
            return node switch
            {
                MemberAccessExpressionSyntax(SimpleMemberAccessExpression) { Name: IdentifierNameSyntax name } memberAccess
                    when semanticModel.GetSymbolInfo(name).Symbol?.IsStatic == false
                    => GetInnermostReceiver(memberAccess.Expression, semanticModel),
                ConditionalAccessExpressionSyntax { WhenNotNull: IdentifierNameSyntax name } memberAccess
                    when semanticModel.GetSymbolInfo(name).Symbol?.IsStatic == false
                    => GetInnermostReceiver(memberAccess.Expression, semanticModel),
                _ => node,
            };
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
