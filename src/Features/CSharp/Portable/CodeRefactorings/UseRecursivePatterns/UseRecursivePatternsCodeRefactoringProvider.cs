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
            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var node = root.FindToken(textSpan.Start).Parent;
            var replacementFunc = GetReplacementFunc(node, model);
            if (replacementFunc is null)
                return;

            context.RegisterRefactoring(
                new MyCodeAction(
                    "Use recursive patterns",
                    _ => Task.FromResult(document.WithSyntaxRoot(replacementFunc(root)))));
        }

        private static Func<SyntaxNode, SyntaxNode>? GetReplacementFunc(SyntaxNode? node, SemanticModel model)
        {
            return node switch
            {
                BinaryExpressionSyntax(LogicalAndExpression) logicalAnd
                    => CombineLogicalAndOperands(logicalAnd, model),
                CasePatternSwitchLabelSyntax { WhenClause: { } whenClause } switchLabel
                    => CombineWhenClauseCondition(switchLabel.Pattern, whenClause.Condition, model),
                SwitchExpressionArmSyntax { WhenClause: { } whenClause } switchArm
                    => CombineWhenClauseCondition(switchArm.Pattern, whenClause.Condition, model),
                WhenClauseSyntax { Parent: CasePatternSwitchLabelSyntax switchLabel } whenClause
                    => CombineWhenClauseCondition(switchLabel.Pattern, whenClause.Condition, model),
                WhenClauseSyntax { Parent: SwitchExpressionArmSyntax switchArm } whenClause
                    => CombineWhenClauseCondition(switchArm.Pattern, whenClause.Condition, model),
                _ => null
            };
        }

        private static Func<SyntaxNode, SyntaxNode>? CombineLogicalAndOperands(BinaryExpressionSyntax logicalAnd, SemanticModel model)
        {
            if (TryDetermineReceiver(logicalAnd.Left, model, inWhenClause: false) is not var (leftReceiver, leftTarget, leftFlipped) ||
                TryDetermineReceiver(logicalAnd.Right, model, inWhenClause: false) is not var (rightReceiver, rightTarget, rightFlipped))
            {
                return null;
            }

            // If we have an is-expression on the left, first we check if there is a variable designation that's been used on the right-hand-side,
            // in which case, we'll convert and move the check inside the existing pattern, if possible.
            // For instance, `e is C c && c.p == 0` is converted to `e is C { p: 0 } c`
            if (leftTarget.Parent is IsPatternExpressionSyntax isPatternExpression &&
                TryFindVariableDesignation(isPatternExpression.Pattern, rightReceiver, model) is var (containingPattern, rightNamesOpt))
            {
                Debug.Assert(leftTarget == isPatternExpression.Pattern);
                Debug.Assert(leftReceiver == isPatternExpression.Expression);
                return root =>
                {
                    var rightPattern = CreatePattern(rightReceiver, rightTarget, rightFlipped);
                    var rewrittenPattern = RewriteContainingPattern(containingPattern, rightPattern, rightNamesOpt);
                    var replacement = isPatternExpression.ReplaceNode(containingPattern, rewrittenPattern);
                    return ReplaceAndAdjustBinaryExpressionOperands(root, logicalAnd, replacement);
                };
            }

            if (TryGetCommonReceiver(leftReceiver, rightReceiver, model) is not var (commonReceiver, leftNames, rightNames))
                return null;

            return root =>
            {
                var leftSubpattern = CreateSubpattern(leftNames, CreatePattern(leftReceiver, leftTarget, leftFlipped));
                var rightSubpattern = CreateSubpattern(rightNames, CreatePattern(rightReceiver, rightTarget, rightFlipped));
                ExpressionSyntax replacement = IsPatternExpression(commonReceiver, RecursivePattern(leftSubpattern, rightSubpattern));
                return ReplaceAndAdjustBinaryExpressionOperands(root, logicalAnd, replacement);
            };

            static SyntaxNode ReplaceAndAdjustBinaryExpressionOperands(SyntaxNode root, BinaryExpressionSyntax logicalAnd, ExpressionSyntax replacement)
            {
                if (logicalAnd.Left is BinaryExpressionSyntax(LogicalAndExpression) leftExpression)
                    replacement = leftExpression.WithRight(replacement);
                return root.ReplaceNode(logicalAnd, replacement.WithAdditionalAnnotations(Formatter.Annotation));
            }
        }

        private static Func<SyntaxNode, SyntaxNode>? CombineWhenClauseCondition(
            PatternSyntax switchPattern, ExpressionSyntax condition, SemanticModel model)
        {
            if (TryDetermineReceiver(condition, model, inWhenClause: true) is not var (receiver, target, flipped) ||
                TryFindVariableDesignation(switchPattern, receiver, model) is not var (containingPattern, namesOpt))
            {
                return null;
            }

            return root =>
            {
                var editor = new SyntaxEditor(root, CSharpSyntaxGenerator.Instance);
                switch (receiver.Parent!.Parent)
                {
                    // This is the leftmost `&&` operand in a when-clause. Remove the left-hand-side which we've just morphed in the switch pattern.
                    // For instance, `case { p: var v } when v.q == 1 && expr:` would be converted to `case { p: { q: 1 } } v when expr:`
                    case BinaryExpressionSyntax(LogicalAndExpression) logicalAnd:
                        editor.ReplaceNode(logicalAnd, logicalAnd.Right);
                        break;
                    // If we reach here, there's no other expression left in the when-clause. Remove.
                    // For instance, `case { p: var v } when v.q == 1:` would be converted to `case { p: { q: 1 } v }:`
                    case WhenClauseSyntax whenClause:
                        editor.RemoveNode(whenClause, SyntaxRemoveOptions.AddElasticMarker);
                        break;
                    case var v:
                        throw ExceptionUtilities.UnexpectedValue(v);
                }

                var generatedPattern = CreatePattern(receiver, target, flipped);
                var rewrittenPattern = RewriteContainingPattern(containingPattern, generatedPattern, namesOpt);
                editor.ReplaceNode(containingPattern, rewrittenPattern);
                return editor.GetChangedRoot();
            };
        }

        private static PatternSyntax RewriteContainingPattern(
            PatternSyntax containingPattern,
            PatternSyntax generatedPattern,
            ImmutableArray<IdentifierNameSyntax> namesOpt)
        {
            PatternSyntax result;
            if (namesOpt.IsDefault)
            {
                // If there's no name, this is a variable designation match.
                result = (containingPattern, generatedPattern) switch
                {
                    // We know we have a var-pattern, declaration-pattern or a recursive-pattern on the left as the containing node of the variable designation.
                    // Depending on the generated pattern off of the expression on the right, we can give a better result by morphing it into the existing match.
                    // Otherwise, we fallback to an `and` pattern.
                    (VarPatternSyntax var, RecursivePatternSyntax recurisve) => recurisve.WithDesignation(var.Designation),
                    (DeclarationPatternSyntax decl, RecursivePatternSyntax recurisve) => recurisve.WithType(decl.Type).WithDesignation(decl.Designation),
                    (RecursivePatternSyntax recurisve, DeclarationPatternSyntax decl) => recurisve.WithType(decl.Type),
                    _ => BinaryPattern(AndPattern, containingPattern, generatedPattern),
                };
            }
            else
            {
                // Otherwise, generate a subpattern per each name.
                var subpattern = CreateSubpattern(namesOpt, generatedPattern);
                result = containingPattern switch
                {
                    VarPatternSyntax p => RecursivePattern(subpattern, p.Designation),
                    DeclarationPatternSyntax p => RecursivePattern(subpattern, p.Designation, p.Type),
                    RecursivePatternSyntax p => p.AddPropertyPatternClauseSubpatterns(subpattern),
                    var p => throw ExceptionUtilities.UnexpectedValue(p)
                };
            }

            // We must have preserved the existing variable designation.
            Debug.Assert(containingPattern switch
            {
                VarPatternSyntax p => p.Designation,
                DeclarationPatternSyntax p => p.Designation,
                RecursivePatternSyntax p => p.Designation,
                var p => throw ExceptionUtilities.UnexpectedValue(p)
            } is var d && result.DescendantNodes().Any(node => AreEquivalent(node, d)));

            return result.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static PatternSyntax CreatePattern(ExpressionSyntax receiver, ExpressionOrPatternSyntax target, bool flipped)
        {
            return target switch
            {
                PatternSyntax pattern => pattern,
                ExpressionSyntax constant => CreatePattern(receiver, constant, flipped),
                var v => throw ExceptionUtilities.UnexpectedValue(v),
            };
        }

        private static (PatternSyntax ContainingPattern, ImmutableArray<IdentifierNameSyntax> NamesOpt)? TryFindVariableDesignation(
            PatternSyntax leftPattern,
            ExpressionSyntax rightReceiver,
            SemanticModel model)
        {
            if (GetInnermostReceiver(rightReceiver, model) is not IdentifierNameSyntax identifierName)
                return null;

            var designation = leftPattern.DescendantNodes()
                .OfType<SingleVariableDesignationSyntax>()
                .Where(d => AreEquivalent(d.Identifier, identifierName.Identifier))
                .FirstOrDefault();

            // For simplicity, we only support replacement when the designation is contained in one of the following patterns.
            // This excludes a parenthesized variable designation, for example, which would require rewriting the whole thing.
            var parent = designation.Parent;
            if (!parent.IsKind(SyntaxKind.VarPattern, SyntaxKind.DeclarationPattern, SyntaxKind.RecursivePattern))
                return null;

            // Since we're looking for a variable designation, we permit a replacement with no subpatterns.
            // For instance, `e is C v && v is { p: 1 }` would be converted to `e is C { p: 1 } v`
            _ = TryGetParentNames(identifierName, out var names);
            return ((PatternSyntax)parent, names);
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
                var v => throw ExceptionUtilities.UnexpectedValue(v),
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

        private static readonly PatternSyntax s_trueConstantPattern = ConstantPattern(LiteralExpression(TrueLiteralExpression));
        private static readonly PatternSyntax s_falseConstantPattern = ConstantPattern(LiteralExpression(FalseLiteralExpression));

        private static (ExpressionSyntax Receiver, ExpressionOrPatternSyntax Target, bool Flipped)? TryDetermineReceiver(
            ExpressionSyntax node,
            SemanticModel model,
            bool inWhenClause)
        {
            return node switch
            {
                // For comparison operators, after we have determined the
                // constant operand, we rewrite it as a constant or relational pattern.
                BinaryExpressionSyntax(EqualsExpression or
                                       NotEqualsExpression or
                                       GreaterThanExpression or
                                       GreaterThanOrEqualExpression or
                                       LessThanOrEqualExpression or
                                       LessThanExpression) expr
                    => TryDetermineConstant(expr, model),

                // If we found a `&&` here, there's two possibilities:
                //
                //  1) If we're in a when-clause, we look for the leftmost expression
                //     which we will try to combine with the switch arm/label pattern.
                //     For instance, we return `a` if we have `case <pat> when a && b && c`.
                //
                //  2) Otherwise, we will return the operand that *appears* to be on the left in the source.
                //     For instance, we return `a` if we have `x && a && b` with the cursor on the second operator.
                //     Since `&&` is left-associative, it's guaranteed to be the expression that we want.
                //     Note: For simplicity, we won't descend into any parenthesized expression here.
                //
                BinaryExpressionSyntax(LogicalAndExpression) expr
                    => TryDetermineReceiver(inWhenClause ? expr.Left : expr.Right, model, inWhenClause),

                // If we have an `is` operator, we'll try to combine the existing pattern with the other operand.
                IsPatternExpressionSyntax expr
                    => (expr.Expression, expr.Pattern, false),

                // We treat any other expression as if they were compared to true/false.
                // For instance, `a.b && !a.c` will be converted to `a is { b: true, c: false }`
                PrefixUnaryExpressionSyntax(LogicalNotExpression) expr
                    => (expr.Operand, s_falseConstantPattern, false),

                var expr => (expr, s_trueConstantPattern, false),
            };
        }

        private static (ExpressionSyntax Expression, ExpressionSyntax Constant, bool Flipped)? TryDetermineConstant(
            BinaryExpressionSyntax node,
            SemanticModel model)
        {
            return (node.Left, node.Right) switch
            {
                var (left, right) when model.GetConstantValue(left).HasValue => (right, left, true),
                var (left, right) when model.GetConstantValue(right).HasValue => (left, right, false),
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

        public static RecursivePatternSyntax RecursivePattern(params SubpatternSyntax[] subpatterns)
        {
            return SyntaxFactory.RecursivePattern(null, positionalPatternClause: null, PropertyPatternClause(SeparatedList(subpatterns)), null);
        }

        public static RecursivePatternSyntax RecursivePattern(SubpatternSyntax subpattern, VariableDesignationSyntax? designation = null, TypeSyntax? type = null)
        {
            return SyntaxFactory.RecursivePattern(type, positionalPatternClause: null, PropertyPatternClause(SingletonSeparatedList(subpattern)), designation);
        }

        /// <summary>
        /// Obtain the outermost common receiver between two expressions.
        /// </summary>
        private static (ExpressionSyntax CommonReceiver, ImmutableArray<IdentifierNameSyntax> LeftNames, ImmutableArray<IdentifierNameSyntax> RightNames)? TryGetCommonReceiver(
            ExpressionSyntax left,
            ExpressionSyntax right,
            SemanticModel model)
        {
            // First, we walk downwards to get the innermost receiver.
            var leftReceiver = GetInnermostReceiver(left, model);
            var rightReceiver = GetInnermostReceiver(right, model);

            // If we don't have a common starting point, bail - there's no common receiver.
            if (!AreEquivalent(leftReceiver, rightReceiver))
            {
                return null;
            }

            // Otherwise, walk upwards to capture the outermost common receiver.
            // We do this to decrease noise on superfluous subpatterns.
            while (leftReceiver.Parent is ExpressionSyntax leftParent && leftParent != left &&
                   rightReceiver.Parent is ExpressionSyntax rightParent && rightParent != right &&
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
        /// Walks up the receiver and collect any names on the right-hand-side. 
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
        /// Obtain the innermost receiver that is on the left of a member access which cannot be converted to a subpattern.
        /// </summary>
        private static ExpressionSyntax GetInnermostReceiver(ExpressionSyntax node, SemanticModel model)
        {
            return node switch
            {
                MemberAccessExpressionSyntax(SimpleMemberAccessExpression) { Name: IdentifierNameSyntax name } memberAccess
                    when RequiresInstanceReceiver(name, model) => GetInnermostReceiver(memberAccess.Expression, model),
                ConditionalAccessExpressionSyntax { WhenNotNull: IdentifierNameSyntax name } memberAccess
                    when RequiresInstanceReceiver(name, model) => GetInnermostReceiver(memberAccess.Expression, model),
                _ => node,
            };

            static bool RequiresInstanceReceiver(SyntaxNode node, SemanticModel model)
                => model.GetSymbolInfo(node).Symbol?.IsStatic == false;
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
