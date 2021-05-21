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
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            Func<SyntaxNode> replacementFunc;
            var node = root.FindToken(textSpan.Start).Parent;
            switch (node)
            {
                case WhenClauseSyntax whenClause:
                    {
                        if (!TryDetermineReceiver(whenClause.Condition, semanticModel, out var receiver, out var target))
                            return;

                        receiver = GetInnermostReceiver(receiver, semanticModel);
                        if (receiver is not IdentifierNameSyntax name)
                            return;

                        var parent = whenClause.Parent;
                        var pattern = parent switch
                        {
                            CasePatternSwitchLabelSyntax label => label.Pattern,
                            SwitchExpressionArmSyntax arm => arm.Pattern,
                            _ => null
                        };

                        if (pattern is null)
                            return;

                        Debug.Assert(parent is not null);

                        var designation = pattern.DescendantNodes()
                            .OfType<SingleVariableDesignationSyntax>()
                            .Where(d => AreEquivalent(d.Identifier, name.Identifier))
                            .FirstOrDefault();

                        if (designation is null)
                            return;

                        if (designation.Parent is not VarPatternSyntax replacementNode)
                            return;

                        var names = GetNames(receiver);
                        if (names.IsDefaultOrEmpty)
                            return;

                        if (target is not ExpressionSyntax constant)
                            return;

                        node = parent;
                        replacementFunc = parent switch
                        {
                            SwitchExpressionArmSyntax switchArm => () =>
                            {
                                var pattern = GetPattern(whenClause.Condition, constant);
                                var subpattern = GetSubpattern(names, pattern);
                                return switchArm
                                    .WithWhenClause(null)
                                    .WithPattern(switchArm.Pattern
                                        .ReplaceNode(replacementNode, RecursivePattern(subpattern).WithDesignation(designation)));
                            }
                            ,
                            CasePatternSwitchLabelSyntax label => () =>
                            {
                                var pattern = GetPattern(whenClause.Condition, constant);
                                var subpattern = GetSubpattern(names, pattern);
                                return label
                                    .WithWhenClause(null)
                                    .WithPattern(label.Pattern
                                        .ReplaceNode(replacementNode, RecursivePattern(subpattern).WithDesignation(designation)));
                            }
                            ,
                            var v => throw ExceptionUtilities.UnexpectedValue(v.Kind())
                        };
                        break;
                    }
                case BinaryExpressionSyntax(LogicalAndExpression) logicalAnd:
                    {
                        if (!TryDetermineReceiver(logicalAnd.Left, semanticModel, out var leftReceiver, out var leftTarget) ||
                            !TryDetermineReceiver(logicalAnd.Right, semanticModel, out var rightReceiver, out var rightTarget))
                        {
                            return;
                        }

                        var receiver = GetCommonReceiver(leftReceiver, rightReceiver, semanticModel, out var leftNames, out var rightNames);
                        if (receiver is null)
                        {
                            return;
                        }

                        if (leftNames.IsDefaultOrEmpty ||
                            rightNames.IsDefaultOrEmpty)
                        {
                            return;
                        }

                        switch (leftTarget, rightTarget)
                        {
                            case (ExpressionSyntax leftConstant, ExpressionSyntax rightConstant):
                                replacementFunc = () =>
                                {
                                    var leftPattern = GetPattern(logicalAnd.Left, leftConstant);
                                    var rightPattern = GetPattern(logicalAnd.Right, rightConstant);
                                    var leftSubpattern = GetSubpattern(leftNames, leftPattern);
                                    var rightSubpattern = GetSubpattern(rightNames, rightPattern);
                                    var recursive = RecursivePattern(leftSubpattern, rightSubpattern);
                                    return IsPatternExpression(receiver, recursive);
                                };
                                break;
                            default:
                                return;
                        }

                        break;
                    }
#if false
                case BinaryExpressionSyntax(LogicalOrExpression) logicalOr:
                    return;
#endif
                default:
                    return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    "Use recursive patterns",
                    c =>
                    {
                        var newRoot = root.ReplaceNode(node, replacementFunc());
                        var newDocument = document.WithSyntaxRoot(newRoot);
                        return Task.FromResult(newDocument);
                    }));
        }

        private static PatternSyntax GetPattern(SyntaxNode node, ExpressionSyntax constant)
        {
            return node switch
            {
                BinaryExpressionSyntax(EqualsExpression) => ConstantPattern(constant),
                BinaryExpressionSyntax(NotEqualsExpression) => UnaryPattern(ConstantPattern(constant)),
                BinaryExpressionSyntax(GreaterThanExpression or
                                       GreaterThanOrEqualExpression or
                                       LessThanExpression or
                                       LessThanOrEqualExpression) e => RelationalPattern(e.OperatorToken, constant),
                var v => throw ExceptionUtilities.UnexpectedValue(v.Kind()),
            };
        }

        private static bool TryDetermineReceiver(
            ExpressionSyntax operand,
            SemanticModel semanticModel,
            [NotNullWhen(true)] out ExpressionSyntax? receiver,
            [NotNullWhen(true)] out ExpressionOrPatternSyntax? target)
        {
            switch (operand)
            {
                case BinaryExpressionSyntax(EqualsExpression or
                                            NotEqualsExpression or
                                            GreaterThanExpression or
                                            GreaterThanOrEqualExpression or
                                            LessThanExpression or
                                            LessThanOrEqualExpression) node:
                    if (!TryDetermineConstant(node, semanticModel, out var constant, out var other))
                        break;
                    receiver = other;
                    target = constant;
                    return true;
#if false
                case BinaryExpressionSyntax(IsExpression) node:
                    receiver = node.Left;
                    target = node.Right;
                    return true;
                case IsPatternExpressionSyntax node:
                    receiver = node.Expression;
                    target = node.Pattern;
                    return true;
#endif
            }
            receiver = null;
            target = null;
            return false;
        }

        private static bool TryDetermineConstant(
            BinaryExpressionSyntax node,
            SemanticModel semanticModel,
            [NotNullWhen(true)] out ExpressionSyntax? constant,
            [NotNullWhen(true)] out ExpressionSyntax? other)
        {
            if (semanticModel.GetConstantValue(node.Left).HasValue)
            {
                (constant, other) = (node.Left, node.Right);
                return true;
            }
            if (semanticModel.GetConstantValue(node.Right).HasValue)
            {
                (constant, other) = (node.Right, node.Left);
                return true;
            }
            constant = other = null;
            return false;
        }

        private static SubpatternSyntax GetSubpattern(ImmutableArray<IdentifierNameSyntax> names, PatternSyntax pattern)
        {
            return names.Aggregate(
                pattern,
                (pattern, name) => Subpattern(name, pattern),
                (subpattern, name) => Subpattern(name, RecursivePattern(subpattern)));
        }

        private static RecursivePatternSyntax RecursivePattern(SubpatternSyntax subpattern)
        {
            return RecursivePattern(properties: PropertyPatternClause(SingletonSeparatedList(subpattern)));
        }

        private static RecursivePatternSyntax RecursivePattern(params SubpatternSyntax[] subpatterns)
        {
            return RecursivePattern(properties: PropertyPatternClause(SeparatedList(subpatterns)));
        }

        private static SubpatternSyntax Subpattern(IdentifierNameSyntax name, PatternSyntax pattern)
        {
            return SyntaxFactory.Subpattern(NameColon(name), pattern);
        }

        public static RecursivePatternSyntax RecursivePattern(
            TypeSyntax? type = null,
            PositionalPatternClauseSyntax? positional = null,
            PropertyPatternClauseSyntax? properties = null,
            VariableDesignationSyntax? designation = null)
        {
            return SyntaxFactory.RecursivePattern(type, positional, properties, designation);
        }

        /// <summary>
        /// Obtain the outermost common receiver between two expressions.
        /// </summary>
        private static ExpressionSyntax? GetCommonReceiver(
            ExpressionSyntax left,
            ExpressionSyntax right,
            SemanticModel semanticModel,
            out ImmutableArray<IdentifierNameSyntax> leftNames,
            out ImmutableArray<IdentifierNameSyntax> rightNames)
        {
            // First, we walk downwards to get the innermost receiver.
            var leftReceiver = GetInnermostReceiver(left, semanticModel);
            var rightReceiver = GetInnermostReceiver(right, semanticModel);

            // If we don't have a common starting point, bail - there's no common receiver.
            if (!AreEquivalent(leftReceiver, rightReceiver))
            {
                leftNames = rightNames = default;
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
            leftNames = GetNames(leftReceiver);
            rightNames = GetNames(rightReceiver);
            return leftReceiver;
        }

        /// <summary>
        /// Walks up the recever and collect any names on the right-hand-side. 
        /// These are already verified in <see cref="GetInnermostReceiver(ExpressionSyntax, SemanticModel)"/> below to have a proper name.
        /// Note: we collect names inside out, so that the outermost name ends up in the innermost subpattern.
        /// </summary>
        private static ImmutableArray<IdentifierNameSyntax> GetNames(ExpressionSyntax receiver)
        {
            using var _ = ArrayBuilder<IdentifierNameSyntax>.GetInstance(out var names);
            CollectNames(receiver.Parent, names);
            return names.ToImmutableOrNull();

            static void CollectNames(SyntaxNode? node, ArrayBuilder<IdentifierNameSyntax> names)
            {
                switch (node)
                {
                    case MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } memberAccess:
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
