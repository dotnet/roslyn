// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseRecursivePatterns;

using static SyntaxFactory;
using static SyntaxKind;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseRecursivePatterns), Shared]
internal sealed class UseRecursivePatternsCodeRefactoringProvider : SyntaxEditorBasedCodeRefactoringProvider
{
    private static readonly PatternSyntax s_trueConstantPattern = ConstantPattern(LiteralExpression(TrueLiteralExpression));
    private static readonly PatternSyntax s_falseConstantPattern = ConstantPattern(LiteralExpression(FalseLiteralExpression));

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public UseRecursivePatternsCodeRefactoringProvider()
    {
    }

    protected override ImmutableArray<FixAllScope> SupportedFixAllScopes => AllFixAllScopes;

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;
        if (document.Project.Solution.WorkspaceKind == WorkspaceKind.MiscellaneousFiles)
            return;

        if (textSpan.Length > 0)
            return;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root.SyntaxTree.Options.LanguageVersion() < LanguageVersion.CSharp9)
            return;

        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var node = root.FindToken(textSpan.Start).Parent;
        var replacementFunc = GetReplacementFunc(node, model);
        if (replacementFunc is null)
            return;

        context.RegisterRefactoring(
            CodeAction.Create(
                CSharpFeaturesResources.Use_recursive_patterns,
                _ => Task.FromResult(document.WithSyntaxRoot(replacementFunc(root))),
                nameof(CSharpFeaturesResources.Use_recursive_patterns)));
    }

    private static Func<SyntaxNode, SyntaxNode>? GetReplacementFunc(SyntaxNode? node, SemanticModel model)
        => node switch
        {
            BinaryExpressionSyntax(LogicalAndExpression) logicalAnd => CombineLogicalAndOperands(logicalAnd, model),
            CasePatternSwitchLabelSyntax { WhenClause: { } whenClause } switchLabel => CombineWhenClauseCondition(switchLabel.Pattern, whenClause.Condition, model),
            SwitchExpressionArmSyntax { WhenClause: { } whenClause } switchArm => CombineWhenClauseCondition(switchArm.Pattern, whenClause.Condition, model),
            WhenClauseSyntax { Parent: CasePatternSwitchLabelSyntax switchLabel } whenClause => CombineWhenClauseCondition(switchLabel.Pattern, whenClause.Condition, model),
            WhenClauseSyntax { Parent: SwitchExpressionArmSyntax switchArm } whenClause => CombineWhenClauseCondition(switchArm.Pattern, whenClause.Condition, model),
            _ => null
        };

    private static bool IsFixableNode(SyntaxNode node)
        => node switch
        {
            BinaryExpressionSyntax(LogicalAndExpression) => true,
            CasePatternSwitchLabelSyntax { WhenClause: { } } => true,
            SwitchExpressionArmSyntax { WhenClause: { } } => true,
            WhenClauseSyntax { Parent: CasePatternSwitchLabelSyntax } => true,
            WhenClauseSyntax { Parent: SwitchExpressionArmSyntax } => true,
            _ => false
        };

    private static Func<SyntaxNode, SyntaxNode>? CombineLogicalAndOperands(BinaryExpressionSyntax logicalAnd, SemanticModel model)
    {
        if (TryDetermineReceiver(logicalAnd.Left, model) is not var (leftReceiver, leftTarget, leftFlipped) ||
            TryDetermineReceiver(logicalAnd.Right, model) is not var (rightReceiver, rightTarget, rightFlipped))
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
                return root.ReplaceNode(logicalAnd, AdjustBinaryExpressionOperands(logicalAnd, replacement));
            };
        }

        if (TryGetCommonReceiver(leftReceiver, rightReceiver, leftTarget, rightTarget, model) is var (commonReceiver, leftNames, rightNames))
        {
            return root =>
            {
                // It's possible we decided to discard a pattern due to it being redundant (such as a null check
                // combined with a property check belonging to the same field we confirmed not being null).
                // For instance 'cf != null && cf.C != 0', the left null check doesn't add more information than the 
                // right expression because the is pattern `cf is { C: not 0 }` already checks for null implicitly
                if (leftNames.Length == 0)
                {
                    var rightSubpattern = CreateSubpattern(rightNames, CreatePattern(rightReceiver, rightTarget, rightFlipped));
                    var replacement = IsPatternExpression(commonReceiver, RecursivePattern(rightSubpattern));
                    return root.ReplaceNode(logicalAnd, AdjustBinaryExpressionOperands(logicalAnd, replacement));
                }
                else if (rightNames.Length == 0)
                {
                    var leftSubpattern = CreateSubpattern(leftNames, CreatePattern(leftReceiver, leftTarget, leftFlipped));
                    var replacement = IsPatternExpression(commonReceiver, RecursivePattern(leftSubpattern));
                    return root.ReplaceNode(logicalAnd, AdjustBinaryExpressionOperands(logicalAnd, replacement));
                }
                else
                {
                    var leftSubpattern = CreateSubpattern(leftNames, CreatePattern(leftReceiver, leftTarget, leftFlipped));
                    var rightSubpattern = CreateSubpattern(rightNames, CreatePattern(rightReceiver, rightTarget, rightFlipped));
                    var replacement = IsPatternExpression(commonReceiver, RecursivePattern(leftSubpattern, rightSubpattern));
                    return root.ReplaceNode(logicalAnd, AdjustBinaryExpressionOperands(logicalAnd, replacement));
                }
            };
        }

        return null;

        static SyntaxNode AdjustBinaryExpressionOperands(BinaryExpressionSyntax logicalAnd, ExpressionSyntax replacement)
        {
            // If there's a `&&` on the left, we have picked the right-hand-side for the combination.
            // In which case, we should replace that instead of the whole `&&` operator in a chain.
            // For instance, `expr && a.b == 1 && a.c == 2` is converted to `expr && a is { b: 1, c: 2 }`
            if (logicalAnd.Left is BinaryExpressionSyntax(LogicalAndExpression) leftExpression)
                replacement = leftExpression.WithRight(replacement);
            return replacement.ConvertToSingleLine().WithAdditionalAnnotations(Formatter.Annotation);
        }
    }

    private static Func<SyntaxNode, SyntaxNode>? CombineWhenClauseCondition(PatternSyntax switchPattern, ExpressionSyntax condition, SemanticModel model)
    {
        if (TryDetermineReceiver(condition, model, inWhenClause: true) is not var (receiver, target, flipped) ||
            TryFindVariableDesignation(switchPattern, receiver, model) is not var (containingPattern, namesOpt))
        {
            return null;
        }

        return root =>
        {
            var editor = new SyntaxEditor(root, CSharpSyntaxGenerator.Instance);
            switch (receiver.GetRequiredParent().Parent)
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
        // This is a variable designation match. We'll try to combine the generated
        // pattern from the right-hand-side into the containing pattern of this designation.
        var rewrittenPattern = namesOpt.IsDefault
            // If there's no name, we will combine the pattern itself.
            ? Combine(containingPattern, generatedPattern)
            // Otherwise, we generate a subpattern per each name and rewrite as a recursive pattern.
            : AddSubpattern(containingPattern, CreateSubpattern(namesOpt, generatedPattern));

        return rewrittenPattern.ConvertToSingleLine().WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);

        static PatternSyntax Combine(PatternSyntax containingPattern, PatternSyntax generatedPattern)
        {
            // We know we have a var-pattern, declaration-pattern or a recursive-pattern on the left as the containing node of the variable designation.
            // Depending on the generated pattern off of the expression on the right, we can give a better result by morphing it into the existing match.
            return (containingPattern, generatedPattern) switch
            {
                // e.g. `e is var x && x is { p: 1 }` => `e is { p: 1 } x`
                (VarPatternSyntax var, RecursivePatternSyntax { Designation: null } recursive)
                    => recursive.WithDesignation(var.Designation),

                // e.g. `e is C x && x is { p: 1 }` => `is C { p: 1 } x`
                (DeclarationPatternSyntax decl, RecursivePatternSyntax { Type: null, Designation: null } recursive)
                    => recursive.WithType(decl.Type).WithDesignation(decl.Designation),

                // e.g. `e is { p: 1 } x && x is C` => `is C { p: 1 } x`
                (RecursivePatternSyntax { Type: null } recursive, TypePatternSyntax type)
                    => recursive.WithType(type.Type),

                // e.g. `e is { p: 1 } x && x is { q: 2 }` => `e is { p: 1, q: 2 } x`
                (RecursivePatternSyntax recursive, RecursivePatternSyntax { Type: null, Designation: null } other)
                    when recursive.PositionalPatternClause is null || other.PositionalPatternClause is null
                    => recursive
                        .WithPositionalPatternClause(recursive.PositionalPatternClause ?? other.PositionalPatternClause)
                        .WithPropertyPatternClause(Concat(recursive.PropertyPatternClause, other.PropertyPatternClause)),

                // In any other case, we fallback to an `and` pattern.
                // UNDONE: This may result in a few unused variables which should be removed in a later pass.
                _ => BinaryPattern(AndPattern, containingPattern.Parenthesize(), generatedPattern.Parenthesize()),
            };
        }

        static PatternSyntax AddSubpattern(PatternSyntax containingPattern, SubpatternSyntax subpattern)
        {
            return containingPattern switch
            {
                // e.g. `case var x when x.p is 1` => `case { p: 1 } x`
                VarPatternSyntax p => RecursivePattern(type: null, subpattern, p.Designation),

                // e.g. `case Type x when x.p is 1` => `case Type { p: 1 } x`
                DeclarationPatternSyntax p => RecursivePattern(p.Type, subpattern, p.Designation),

                // e.g. `case { p: 1 } x when x.q is 2` => `case { p: 1, q: 2 } x`
                RecursivePatternSyntax p => p.AddPropertyPatternClauseSubpatterns(subpattern),

                // We've already checked that the designation is contained in any of the above pattern forms.
                var p => throw ExceptionUtilities.UnexpectedValue(p)
            };
        }

        static PropertyPatternClauseSyntax? Concat(PropertyPatternClauseSyntax? left, PropertyPatternClauseSyntax? right)
        {
            if (left is null || right is null)
                return left ?? right;
            return left.WithSubpatterns(left.Subpatterns.AddRange(right.Subpatterns));
        }
    }

    private static PatternSyntax CreatePattern(ExpressionSyntax originalReceiver, ExpressionOrPatternSyntax target, bool flipped)
    {
        return target switch
        {
            // A pattern come from an `is` expression on either side of `&&`
            PatternSyntax pattern => pattern,
            TypeSyntax type when originalReceiver.IsParentKind(IsExpression) => TypePattern(type),
            // Otherwise, this is a constant. Depending on the original receiver, we create an appropriate pattern.
            ExpressionSyntax constant => originalReceiver.Parent switch
            {
                BinaryExpressionSyntax(EqualsExpression) => ConstantPattern(constant),
                BinaryExpressionSyntax(NotEqualsExpression) => UnaryPattern(ConstantPattern(constant)),
                BinaryExpressionSyntax(GreaterThanExpression or
                                       GreaterThanOrEqualExpression or
                                       LessThanOrEqualExpression or
                                       LessThanExpression) e
                    => RelationalPattern(flipped ? Flip(e.OperatorToken) : e.OperatorToken, constant),
                var v => throw ExceptionUtilities.UnexpectedValue(v),
            },
            var v => throw ExceptionUtilities.UnexpectedValue(v),
        };

        static SyntaxToken Flip(SyntaxToken token)
        {
            return Token(token.Kind() switch
            {
                LessThanToken => GreaterThanToken,
                LessThanEqualsToken => GreaterThanEqualsToken,
                GreaterThanEqualsToken => LessThanEqualsToken,
                GreaterThanToken => LessThanToken,
                var v => throw ExceptionUtilities.UnexpectedValue(v)
            });
        }
    }

    private static (PatternSyntax ContainingPattern, ImmutableArray<IdentifierNameSyntax> NamesOpt)? TryFindVariableDesignation(
        PatternSyntax leftPattern,
        ExpressionSyntax rightReceiver,
        SemanticModel model)
    {
        using var _ = ArrayBuilder<IdentifierNameSyntax>.GetInstance(out var names);
        if (GetInnermostReceiver(rightReceiver, names, model) is not IdentifierNameSyntax identifierName)
            return null;

        var designation = leftPattern.DescendantNodes()
            .OfType<SingleVariableDesignationSyntax>()
            .Where(d => d.Identifier.ValueText == identifierName.Identifier.ValueText)
            .FirstOrDefault();

        // Excluding list patterns because those cannot be combined with a recursive pattern.
        if (designation is not { Parent: PatternSyntax(not SyntaxKind.ListPattern) containingPattern })
            return null;

        // Only the following patterns can directly contain a variable designation.
        // Note: While a parenthesized designation can also contain other variables,
        // it is not a pattern, so it would not get past the PatternSyntax test above.
        Debug.Assert(containingPattern.Kind() is SyntaxKind.VarPattern or SyntaxKind.DeclarationPattern or SyntaxKind.RecursivePattern);
        return (containingPattern, names.ToImmutableOrNull());
    }

    private static (ExpressionSyntax Receiver, ExpressionOrPatternSyntax Target, bool Flipped)? TryDetermineReceiver(
        ExpressionSyntax node,
        SemanticModel model,
        bool inWhenClause = false)
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
            //     For simplicity, we won't descend into any parenthesized expression here.
            //
            BinaryExpressionSyntax(LogicalAndExpression) expr => TryDetermineReceiver(inWhenClause ? expr.Left : expr.Right, model, inWhenClause),

            // If we have an `is` operator, we'll try to combine the existing pattern/type with the other operand.
            BinaryExpressionSyntax(IsExpression) { Right: NullableTypeSyntax type } expr => (expr.Left, type.ElementType, Flipped: false),
            BinaryExpressionSyntax(IsExpression) { Right: TypeSyntax type } expr => (expr.Left, type, Flipped: false),
            IsPatternExpressionSyntax expr => (expr.Expression, expr.Pattern, Flipped: false),

            // We treat any other expression as if they were compared to true/false.
            // For instance, `a.b && !a.c` will be converted to `a is { b: true, c: false }`
            PrefixUnaryExpressionSyntax(LogicalNotExpression) expr => (expr.Operand, s_falseConstantPattern, Flipped: false),
            var expr => (expr, s_trueConstantPattern, Flipped: false),
        };

        static (ExpressionSyntax Expression, ExpressionSyntax Constant, bool Flipped)? TryDetermineConstant(BinaryExpressionSyntax node, SemanticModel model)
        {
            return (node.Left, node.Right) switch
            {
                var (left, right) when model.GetConstantValue(left).HasValue => (right, left, Flipped: true),
                var (left, right) when model.GetConstantValue(right).HasValue => (left, right, Flipped: false),
                _ => null
            };
        }
    }

    private static SubpatternSyntax CreateSubpattern(ImmutableArray<IdentifierNameSyntax> names, PatternSyntax pattern)
    {
        Debug.Assert(!names.IsDefaultOrEmpty);

        if (names.Length > 1 && names[0].SyntaxTree.Options.LanguageVersion() >= LanguageVersion.CSharp10)
        {
            ExpressionSyntax expression = names[^1];
            for (var i = names.Length - 2; i >= 0; i--)
                expression = MemberAccessExpression(SimpleMemberAccessExpression, expression, names[i]);
            return SyntaxFactory.Subpattern(ExpressionColon(expression, Token(ColonToken)), pattern);
        }
        else
        {
            var subpattern = Subpattern(names[0], pattern);
            for (var i = 1; i < names.Length; i++)
                subpattern = Subpattern(names[i], RecursivePattern(subpattern));
            return subpattern;
        }
    }

    private static SubpatternSyntax Subpattern(IdentifierNameSyntax name, PatternSyntax pattern)
        => SyntaxFactory.Subpattern(NameColon(name), pattern);

    private static RecursivePatternSyntax RecursivePattern(params SubpatternSyntax[] subpatterns)
        => SyntaxFactory.RecursivePattern(type: null, positionalPatternClause: null, PropertyPatternClause([.. subpatterns]), designation: null);

    private static RecursivePatternSyntax RecursivePattern(TypeSyntax? type, SubpatternSyntax subpattern, VariableDesignationSyntax? designation)
        => SyntaxFactory.RecursivePattern(type, positionalPatternClause: null, PropertyPatternClause([subpattern]), designation);

    private static RecursivePatternSyntax RecursivePattern(SubpatternSyntax subpattern)
        => RecursivePattern(type: null, subpattern, designation: null);

    /// <summary>
    /// Obtain the outermost common receiver between two expressions.  This can succeed with a null 'CommonReceiver'
    /// in the case that the common receiver is the 'implicit this'.
    /// </summary>
    private static (ExpressionSyntax CommonReceiver, ImmutableArray<IdentifierNameSyntax> LeftNames, ImmutableArray<IdentifierNameSyntax> RightNames)? TryGetCommonReceiver(
        ExpressionSyntax left,
        ExpressionSyntax right,
        ExpressionOrPatternSyntax leftTarget,
        ExpressionOrPatternSyntax rightTarget,
        SemanticModel model)
    {
        using var _1 = ArrayBuilder<IdentifierNameSyntax>.GetInstance(out var leftNames);
        using var _2 = ArrayBuilder<IdentifierNameSyntax>.GetInstance(out var rightNames);

        if (!TryGetInnermostReceiver(left, leftNames, out var leftReceiver, model) ||
            !TryGetInnermostReceiver(right, rightNames, out var rightReceiver, model) ||
            !AreEquivalent(leftReceiver, rightReceiver)) // We must have a common starting point to proceed.
        {
            return null;
        }

        var commonReceiver = leftReceiver;

        // To reduce noise on superfluous subpatterns and avoid duplicates, skip any common name in the path.
        var lastName = SkipCommonNames(leftNames, rightNames);
        if (lastName is not null)
        {
            // If there were some common names in the path, we rewrite the receiver to include those.
            // For instance, in `a.b.c && a.b.d`, we have `b` as the last common name in the path,
            // So we want `a.b` as the receiver so that we convert it to `a.b is { c: true, d: true }`.
            commonReceiver = GetInnermostReceiver(left, lastName, static (identifierName, lastName) => identifierName != lastName);
        }

        // If the common receiver is null, there might still be one in cases like these:
        // `MyClassField != null && MyClassField.prop != 0`. In this case, the left expression doesn't say
        // anything new to the second one so it should be discarded, but MyClassField should still act as the
        // receiver instead of the implicit this so we get
        // `MyClassField is { prop: not 0 }` instead of `this is { MyClassField: not null, MyClassField.prop: not 0 }`
        // We need to cover this case for either side of the expression by detecting a null check on either side
        if (AreEquivalent(leftNames[^1], rightNames[^1]))
        {
            var leftIsNullCheck = IsNullCheck(leftTarget.Parent);
            var rightIsNullCheck = IsNullCheck(rightTarget.Parent);

            if (leftIsNullCheck)
            {
                lastName = rightNames[^1];
                commonReceiver = GetInnermostReceiver(right, lastName, static (identifierName, lastName) => identifierName != lastName);
                rightNames.Clip(rightNames.Count - 1);
                return (commonReceiver ?? ThisExpression(), ImmutableArray<IdentifierNameSyntax>.Empty, rightNames.ToImmutable());
            }

            if (rightIsNullCheck)
            {
                lastName = leftNames[^1];
                commonReceiver = GetInnermostReceiver(left, lastName, static (identifierName, lastName) => identifierName != lastName);
                leftNames.Clip(leftNames.Count - 1);
                return (commonReceiver ?? ThisExpression(), leftNames.ToImmutable(), ImmutableArray<IdentifierNameSyntax>.Empty);
            }
        }

        // If the common receiver is null and we can't find a redundant pattern in the case above,
        // it's an implicit `this` reference in source.
        // For instance, `prop == 1 && field == 2` would be converted to `this is { prop: 1, field: 2 }`
        return (commonReceiver ?? ThisExpression(), leftNames.ToImmutable(), rightNames.ToImmutable());

        static bool TryGetInnermostReceiver(ExpressionSyntax node, ArrayBuilder<IdentifierNameSyntax> builder, [NotNullWhen(true)] out ExpressionSyntax? receiver, SemanticModel model)
        {
            receiver = GetInnermostReceiver(node, builder, model);
            return builder.Any();
        }

        static IdentifierNameSyntax? SkipCommonNames(ArrayBuilder<IdentifierNameSyntax> leftNames, ArrayBuilder<IdentifierNameSyntax> rightNames)
        {
            IdentifierNameSyntax? lastName = null;
            int leftIndex, rightIndex;
            // Note: we don't want to skip the first name to still be able to convert to a subpattern, hence checking `> 0` below.
            for (leftIndex = leftNames.Count - 1, rightIndex = rightNames.Count - 1; leftIndex > 0 && rightIndex > 0; leftIndex--, rightIndex--)
            {
                var leftName = leftNames[leftIndex];
                var rightName = rightNames[rightIndex];
                if (!AreEquivalent(leftName, rightName))
                    break;
                lastName = leftName;
            }

            leftNames.Clip(leftIndex + 1);
            rightNames.Clip(rightIndex + 1);
            return lastName;
        }

        static bool IsNullCheck(SyntaxNode? exp)
        {
            if (exp is BinaryExpressionSyntax(NotEqualsExpression) binaryExpression)
            {
                if (binaryExpression.Left.Kind() == NullLiteralExpression || binaryExpression.Right.Kind() == NullLiteralExpression)
                    return true;
            }

            return false;
        }
    }

    private static ExpressionSyntax? GetInnermostReceiver(ExpressionSyntax node, ArrayBuilder<IdentifierNameSyntax> builder, SemanticModel model)
    {
        return GetInnermostReceiver(node, model, CanConvertToSubpattern, builder);

        static bool CanConvertToSubpattern(IdentifierNameSyntax name, SemanticModel model)
        {
            return model.GetSymbolInfo(name).Symbol is
            {
                IsStatic: false,
                Kind: SymbolKind.Property or SymbolKind.Field,
                ContainingType: not { SpecialType: SpecialType.System_Nullable_T }
            };
        }
    }

    private static ExpressionSyntax? GetInnermostReceiver<TArg>(
        ExpressionSyntax node, TArg arg,
        Func<IdentifierNameSyntax, TArg, bool> canConvertToSubpattern,
        ArrayBuilder<IdentifierNameSyntax>? builder = null)
    {
        return GetInnermostReceiver(node);

        ExpressionSyntax? GetInnermostReceiver(ExpressionSyntax node)
        {
            switch (node)
            {

                case IdentifierNameSyntax name
                        when canConvertToSubpattern(name, arg):
                    builder?.Add(name);
                    // This is a member reference with an implicit `this` receiver.
                    // We know this is true because we already checked canConvertToSubpattern.
                    // Any other name outside the receiver position is captured in the cases below.
                    return null;

                case MemberBindingExpressionSyntax { Name: IdentifierNameSyntax name }
                        when canConvertToSubpattern(name, arg):
                    builder?.Add(name);
                    // We only reach here from a parent conditional-access.
                    // Returning null here means that all the names on the right were convertible to a property pattern.
                    return null;

                case MemberAccessExpressionSyntax(SimpleMemberAccessExpression) { Name: IdentifierNameSyntax name } memberAccess
                        when canConvertToSubpattern(name, arg) && !memberAccess.Expression.IsKind(SyntaxKind.BaseExpression):
                    builder?.Add(name);
                    // For a simple member access we simply record the name and descend into the expression on the left-hand-side.
                    return GetInnermostReceiver(memberAccess.Expression);

                case ConditionalAccessExpressionSyntax conditionalAccess:
                    // For a conditional access, first we need to verify the right-hand-side is convertible to a property pattern.
                    var right = GetInnermostReceiver(conditionalAccess.WhenNotNull);
                    if (right is not null)
                    {
                        // If it has it's own receiver other than a member-binding expression, we return this node as the receiver.
                        // For instance, if we had `a?.M().b`, the name `b` is already captured, so we need to return `a?.M()` as the innermost receiver.
                        // If there was no name, this call returns itself, e.g. in `a?.M()` the receiver is the entire existing conditional access.
                        return conditionalAccess.WithWhenNotNull(right);
                    }
                    // Otherwise, descend into the the expression on the left-hand-side.
                    return GetInnermostReceiver(conditionalAccess.Expression);

                default:
                    return node;
            }
        }
    }

    protected override async Task FixAllAsync(
        Document document,
        ImmutableArray<TextSpan> fixAllSpans,
        SyntaxEditor editor,
        CodeActionOptionsProvider optionsProvider,
        string? equivalenceKey,
        CancellationToken cancellationToken)
    {
        // Get all the descendant nodes to refactor.
        // NOTE: We need to realize the nodes with 'ToArray' call here
        // to ensure we strongly hold onto the nodes so that 'TrackNodes'
        // invoked below, which does tracking based off a ConditionalWeakTable,
        // tracks the nodes for the entire duration of this method.
        var nodes = editor.OriginalRoot.DescendantNodes().Where(IsFixableNode).ToArray();

        // We're going to be continually editing this tree. Track all the nodes we
        // care about so we can find them across each edit.
        document = document.WithSyntaxRoot(editor.OriginalRoot.TrackNodes(nodes));

        // Process all nodes to refactor in reverse to ensure nested nodes
        // are processed before the outer nodes to refactor.
        foreach (var originalNode in nodes.Reverse())
        {
            // Only process nodes fully within a fixAllSpan
            if (!fixAllSpans.Any(fixAllSpan => fixAllSpan.Contains(originalNode.Span)))
                continue;

            // Get current root, current node to refactor and semantic model.
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var currentNode = root.GetCurrentNodes(originalNode).SingleOrDefault();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var replacementFunc = GetReplacementFunc(currentNode, semanticModel);
            if (replacementFunc == null)
                continue;

            document = document.WithSyntaxRoot(replacementFunc(root));
        }

        var updatedRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(editor.OriginalRoot, updatedRoot);
    }
}
