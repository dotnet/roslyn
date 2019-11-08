// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue
{
    internal sealed class StatementSyntaxComparer : SyntaxComparer
    {
        internal static readonly StatementSyntaxComparer Default = new StatementSyntaxComparer();

        private readonly SyntaxNode? _oldRootChild;
        private readonly SyntaxNode? _newRootChild;
        private readonly SyntaxNode? _oldRoot;
        private readonly SyntaxNode? _newRoot;

        private StatementSyntaxComparer()
        {
        }

        internal StatementSyntaxComparer(SyntaxNode oldRootChild, SyntaxNode newRootChild)
        {
            _oldRootChild = oldRootChild;
            _newRootChild = newRootChild;
            _oldRoot = oldRootChild.Parent;
            _newRoot = newRootChild.Parent;
        }

        #region Tree Traversal

        protected internal override bool TryGetParent(SyntaxNode node, out SyntaxNode parent)
        {
            parent = node.Parent;
            while (parent != null && !HasLabel(parent))
            {
                parent = parent.Parent;
            }

            return parent != null;
        }

        protected internal override IEnumerable<SyntaxNode>? GetChildren(SyntaxNode node)
        {
            Debug.Assert(HasLabel(node));

            if (node == _oldRoot || node == _newRoot)
            {
                return EnumerateRootChildren(node);
            }

            return IsLeaf(node) ? null : EnumerateNonRootChildren(node);
        }

        private IEnumerable<SyntaxNode> EnumerateNonRootChildren(SyntaxNode node)
        {
            foreach (var child in node.ChildNodes())
            {
                if (LambdaUtilities.IsLambdaBodyStatementOrExpression(child))
                {
                    continue;
                }

                if (HasLabel(child))
                {
                    yield return child;
                }
                else
                {
                    foreach (var descendant in child.DescendantNodes(DescendIntoChildren))
                    {
                        if (HasLabel(descendant))
                        {
                            yield return descendant;
                        }
                    }
                }
            }
        }

        private IEnumerable<SyntaxNode> EnumerateRootChildren(SyntaxNode root)
        {
            RoslynDebug.Assert(_oldRoot != null);
            RoslynDebug.Assert(_newRoot != null);
            RoslynDebug.Assert(_oldRootChild != null);
            RoslynDebug.Assert(_newRootChild != null);

            var child = (root == _oldRoot) ? _oldRootChild : _newRootChild;

            if (GetLabelImpl(child) != Label.Ignored)
            {
                yield return child;
            }
            else
            {
                foreach (var descendant in child.DescendantNodes(DescendIntoChildren))
                {
                    if (HasLabel(descendant))
                    {
                        yield return descendant;
                    }
                }
            }
        }

        private bool DescendIntoChildren(SyntaxNode node)
        {
            return !LambdaUtilities.IsLambdaBodyStatementOrExpression(node) && !HasLabel(node);
        }

        protected internal sealed override IEnumerable<SyntaxNode> GetDescendants(SyntaxNode node)
        {
            if (node == _oldRoot || node == _newRoot)
            {
                RoslynDebug.Assert(_oldRoot != null);
                RoslynDebug.Assert(_newRoot != null);
                RoslynDebug.Assert(_oldRootChild != null);
                RoslynDebug.Assert(_newRootChild != null);

                var rootChild = (node == _oldRoot) ? _oldRootChild : _newRootChild;

                if (HasLabel(rootChild))
                {
                    yield return rootChild;
                }

                node = rootChild;
            }

            // TODO: avoid allocation of closure
            foreach (var descendant in node.DescendantNodes(descendIntoChildren: c => !IsLeaf(c) && (c == node || !LambdaUtilities.IsLambdaBodyStatementOrExpression(c))))
            {
                if (!LambdaUtilities.IsLambdaBodyStatementOrExpression(descendant) && HasLabel(descendant))
                {
                    yield return descendant;
                }
            }
        }

        private static bool IsLeaf(SyntaxNode node)
        {
            Classify(node.Kind(), node, out var isLeaf);
            return isLeaf;
        }

        #endregion

        #region Labels

        // Assumptions:
        // - Each listed label corresponds to one or more syntax kinds.
        // - Nodes with same labels might produce Update edits, nodes with different labels don't. 
        // - If IsTiedToParent(label) is true for a label then all its possible parent labels must precede the label.
        //   (i.e. both MethodDeclaration and TypeDeclaration must precede TypeParameter label).
        internal enum Label
        {
            ConstructorDeclaration,
            Block,
            CheckedStatement,
            UnsafeStatement,

            TryStatement,
            CatchClause,                      // tied to parent
            CatchDeclaration,                 // tied to parent
            CatchFilterClause,                // tied to parent
            FinallyClause,                    // tied to parent
            ForStatement,
            ForStatementPart,                 // tied to parent
            ForEachStatement,
            UsingStatement,
            FixedStatement,
            LockStatement,
            WhileStatement,
            DoStatement,
            IfStatement,
            ElseClause,                        // tied to parent 

            SwitchStatement,
            SwitchSection,
            CasePatternSwitchLabel,            // tied to parent
            WhenClause,

            YieldStatement,                    // tied to parent
            GotoStatement,
            GotoCaseStatement,
            BreakContinueStatement,
            ReturnThrowStatement,
            ExpressionStatement,

            LabeledStatement,

            // TODO: 
            // Ideally we could declare LocalVariableDeclarator tied to the first enclosing node that defines local scope (block, foreach, etc.)
            // Also consider handling LocalDeclarationStatement as just a bag of variable declarators,
            // so that variable declarators contained in one can be matched with variable declarators contained in the other.
            LocalDeclarationStatement,         // tied to parent
            LocalVariableDeclaration,          // tied to parent
            LocalVariableDeclarator,           // tied to parent

            SingleVariableDesignation,
            AwaitExpression,
            NestedFunction,

            FromClause,
            QueryBody,
            FromClauseLambda,                 // tied to parent
            LetClauseLambda,                  // tied to parent
            WhereClauseLambda,                // tied to parent
            OrderByClause,                    // tied to parent
            OrderingLambda,                   // tied to parent
            SelectClauseLambda,               // tied to parent
            JoinClauseLambda,                 // tied to parent
            JoinIntoClause,                   // tied to parent
            GroupClauseLambda,                // tied to parent
            QueryContinuation,                // tied to parent

            // helpers:
            Count,
            Ignored = IgnoredNode
        }

        private static int TiedToAncestor(Label label)
        {
            switch (label)
            {
                case Label.LocalDeclarationStatement:
                case Label.LocalVariableDeclaration:
                case Label.LocalVariableDeclarator:
                case Label.GotoCaseStatement:
                case Label.BreakContinueStatement:
                case Label.ElseClause:
                case Label.CatchClause:
                case Label.CatchDeclaration:
                case Label.CatchFilterClause:
                case Label.FinallyClause:
                case Label.ForStatementPart:
                case Label.YieldStatement:
                case Label.FromClauseLambda:
                case Label.LetClauseLambda:
                case Label.WhereClauseLambda:
                case Label.OrderByClause:
                case Label.OrderingLambda:
                case Label.SelectClauseLambda:
                case Label.JoinClauseLambda:
                case Label.JoinIntoClause:
                case Label.GroupClauseLambda:
                case Label.QueryContinuation:
                case Label.CasePatternSwitchLabel:
                    return 1;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// <paramref name="node"/> is null only when comparing value equality of a tree node.
        /// </summary>
        internal static Label Classify(SyntaxKind kind, SyntaxNode? node, out bool isLeaf)
        {
            // Notes:
            // A descendant of a leaf node may be a labeled node that we don't want to visit if 
            // we are comparing its parent node (used for lambda bodies).
            // 
            // Expressions are ignored but they may contain nodes that should be matched by tree comparer.
            // (e.g. lambdas, declaration expressions). Descending to these nodes is handled in EnumerateChildren.

            isLeaf = false;

            // If the node is a for loop Initializer, Condition, or Incrementor expression we label it as "ForStatementPart".
            // We need to capture it in the match since these expressions can be "active statements" and as such we need to map them.
            //
            // The parent is not available only when comparing nodes for value equality.
            if (node is ExpressionSyntax _ && node.Parent.IsKind(SyntaxKind.ForStatement))
            {
                return Label.ForStatementPart;
            }

            switch (kind)
            {
                case SyntaxKind.ConstructorDeclaration:
                    // Root when matching constructor bodies.
                    return Label.ConstructorDeclaration;

                case SyntaxKind.Block:
                    return Label.Block;

                case SyntaxKind.LocalDeclarationStatement:
                    return Label.LocalDeclarationStatement;

                case SyntaxKind.VariableDeclaration:
                    return Label.LocalVariableDeclaration;

                case SyntaxKind.VariableDeclarator:
                    return Label.LocalVariableDeclarator;

                case SyntaxKind.SingleVariableDesignation:
                    return Label.SingleVariableDesignation;

                case SyntaxKind.LabeledStatement:
                    return Label.LabeledStatement;

                case SyntaxKind.EmptyStatement:
                    isLeaf = true;
                    return Label.Ignored;

                case SyntaxKind.GotoStatement:
                    isLeaf = true;
                    return Label.GotoStatement;

                case SyntaxKind.GotoCaseStatement:
                case SyntaxKind.GotoDefaultStatement:
                    isLeaf = true;
                    return Label.GotoCaseStatement;

                case SyntaxKind.BreakStatement:
                case SyntaxKind.ContinueStatement:
                    isLeaf = true;
                    return Label.BreakContinueStatement;

                case SyntaxKind.ReturnStatement:
                case SyntaxKind.ThrowStatement:
                    return Label.ReturnThrowStatement;

                case SyntaxKind.ExpressionStatement:
                    return Label.ExpressionStatement;

                case SyntaxKind.YieldBreakStatement:
                case SyntaxKind.YieldReturnStatement:
                    return Label.YieldStatement;

                case SyntaxKind.DoStatement:
                    return Label.DoStatement;

                case SyntaxKind.WhileStatement:
                    return Label.WhileStatement;

                case SyntaxKind.ForStatement:
                    return Label.ForStatement;

                case SyntaxKind.ForEachVariableStatement:
                case SyntaxKind.ForEachStatement:
                    return Label.ForEachStatement;

                case SyntaxKind.UsingStatement:
                    return Label.UsingStatement;

                case SyntaxKind.FixedStatement:
                    return Label.FixedStatement;

                case SyntaxKind.CheckedStatement:
                case SyntaxKind.UncheckedStatement:
                    return Label.CheckedStatement;

                case SyntaxKind.UnsafeStatement:
                    return Label.UnsafeStatement;

                case SyntaxKind.LockStatement:
                    return Label.LockStatement;

                case SyntaxKind.IfStatement:
                    return Label.IfStatement;

                case SyntaxKind.ElseClause:
                    return Label.ElseClause;

                case SyntaxKind.SwitchStatement:
                    return Label.SwitchStatement;

                case SyntaxKind.SwitchSection:
                    return Label.SwitchSection;

                case SyntaxKind.CaseSwitchLabel:
                case SyntaxKind.DefaultSwitchLabel:
                    // Switch labels are included in the "value" of the containing switch section.
                    // We don't need to analyze case expressions.
                    isLeaf = true;
                    return Label.Ignored;

                case SyntaxKind.WhenClause:
                    return Label.WhenClause;

                case SyntaxKind.CasePatternSwitchLabel:
                    return Label.CasePatternSwitchLabel;

                case SyntaxKind.TryStatement:
                    return Label.TryStatement;

                case SyntaxKind.CatchClause:
                    return Label.CatchClause;

                case SyntaxKind.CatchDeclaration:
                    // the declarator of the exception variable
                    return Label.CatchDeclaration;

                case SyntaxKind.CatchFilterClause:
                    return Label.CatchFilterClause;

                case SyntaxKind.FinallyClause:
                    return Label.FinallyClause;

                case SyntaxKind.LocalFunctionStatement:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    return Label.NestedFunction;

                case SyntaxKind.FromClause:
                    // The first from clause of a query is not a lambda.
                    // We have to assign it a label different from "FromClauseLambda"
                    // so that we won't match lambda-from to non-lambda-from.
                    // 
                    // Since FromClause declares range variables we need to include it in the map,
                    // so that we are able to map range variable declarations.
                    // Therefore we assign it a dedicated label.
                    // 
                    // The parent is not available only when comparing nodes for value equality.
                    // In that case it doesn't matter what label the node has as long as it has some.
                    if (node == null || node.Parent.IsKind(SyntaxKind.QueryExpression))
                    {
                        return Label.FromClause;
                    }

                    return Label.FromClauseLambda;

                case SyntaxKind.QueryBody:
                    return Label.QueryBody;

                case SyntaxKind.QueryContinuation:
                    return Label.QueryContinuation;

                case SyntaxKind.LetClause:
                    return Label.LetClauseLambda;

                case SyntaxKind.WhereClause:
                    return Label.WhereClauseLambda;

                case SyntaxKind.OrderByClause:
                    return Label.OrderByClause;

                case SyntaxKind.AscendingOrdering:
                case SyntaxKind.DescendingOrdering:
                    return Label.OrderingLambda;

                case SyntaxKind.SelectClause:
                    return Label.SelectClauseLambda;

                case SyntaxKind.JoinClause:
                    return Label.JoinClauseLambda;

                case SyntaxKind.JoinIntoClause:
                    return Label.JoinIntoClause;

                case SyntaxKind.GroupClause:
                    return Label.GroupClauseLambda;

                case SyntaxKind.IdentifierName:
                case SyntaxKind.QualifiedName:
                case SyntaxKind.GenericName:
                case SyntaxKind.TypeArgumentList:
                case SyntaxKind.AliasQualifiedName:
                case SyntaxKind.PredefinedType:
                case SyntaxKind.PointerType:
                case SyntaxKind.NullableType:
                case SyntaxKind.TupleType:
                case SyntaxKind.RefType:
                case SyntaxKind.OmittedTypeArgument:
                case SyntaxKind.NameColon:
                case SyntaxKind.OmittedArraySizeExpression:
                case SyntaxKind.ThisExpression:
                case SyntaxKind.BaseExpression:
                case SyntaxKind.ArgListExpression:
                case SyntaxKind.NumericLiteralExpression:
                case SyntaxKind.StringLiteralExpression:
                case SyntaxKind.CharacterLiteralExpression:
                case SyntaxKind.TrueLiteralExpression:
                case SyntaxKind.FalseLiteralExpression:
                case SyntaxKind.NullLiteralExpression:
                case SyntaxKind.TypeOfExpression:
                case SyntaxKind.SizeOfExpression:
                case SyntaxKind.DefaultExpression:
                case SyntaxKind.ConstantPattern:
                case SyntaxKind.DiscardDesignation:
                    // can't contain a lambda/await/anonymous type:
                    isLeaf = true;
                    return Label.Ignored;

                case SyntaxKind.AwaitExpression:
                    return Label.AwaitExpression;

                default:
                    // any other node may contain a lambda:
                    return Label.Ignored;
            }
        }

        protected internal override int GetLabel(SyntaxNode node)
        {
            return (int)GetLabelImpl(node);
        }

        internal static Label GetLabelImpl(SyntaxNode node)
        {
            return Classify(node.Kind(), node, out _);
        }

        internal static bool HasLabel(SyntaxNode node)
        {
            return GetLabelImpl(node) != Label.Ignored;
        }

        protected internal override int LabelCount
        {
            get { return (int)Label.Count; }
        }

        protected internal override int TiedToAncestor(int label)
        {
            return TiedToAncestor((Label)label);
        }

        #endregion

        #region Comparisons

        internal static bool IgnoreLabeledChild(SyntaxKind kind)
        {
            // In most cases we can determine Label based on child kind.
            // The only cases when we can't are
            // - for Initializer, Condition and Incrementor expressions in ForStatement.
            // - first from clause of a query expression.
            return Classify(kind, node: null, out _) != Label.Ignored;
        }

        public override bool ValuesEqual(SyntaxNode left, SyntaxNode right)
        {
            // only called from the tree matching alg, which only operates on nodes that are labeled.
            Debug.Assert(HasLabel(left));
            Debug.Assert(HasLabel(right));

            Func<SyntaxKind, bool>? ignoreChildNode;
            switch (left.Kind())
            {
                case SyntaxKind.SwitchSection:
                    return Equal((SwitchSectionSyntax)left, (SwitchSectionSyntax)right);

                case SyntaxKind.ForStatement:
                    // The only children of ForStatement are labeled nodes and punctuation.
                    return true;

                default:
                    // When comparing the value of a node with its partner we are deciding whether to add an Update edit for the pair.
                    // If the actual change is under a descendant labeled node we don't want to attribute it to the node being compared,
                    // so we skip all labeled children when recursively checking for equivalence.
                    if (IsLeaf(left))
                    {
                        ignoreChildNode = null;
                    }
                    else
                    {
                        ignoreChildNode = IgnoreLabeledChild;
                    }

                    break;
            }

            return SyntaxFactory.AreEquivalent(left, right, ignoreChildNode);
        }

        private bool Equal(SwitchSectionSyntax left, SwitchSectionSyntax right)
        {
            return SyntaxFactory.AreEquivalent(left.Labels, right.Labels, null)
                && SyntaxFactory.AreEquivalent(left.Statements, right.Statements, ignoreChildNode: IgnoreLabeledChild);
        }

        protected override bool TryComputeWeightedDistance(SyntaxNode leftNode, SyntaxNode rightNode, out double distance)
        {
            switch (leftNode.Kind())
            {
                case SyntaxKind.VariableDeclarator:
                    distance = ComputeDistance(
                        ((VariableDeclaratorSyntax)leftNode).Identifier,
                        ((VariableDeclaratorSyntax)rightNode).Identifier);
                    return true;

                case SyntaxKind.ForStatement:
                    var leftFor = (ForStatementSyntax)leftNode;
                    var rightFor = (ForStatementSyntax)rightNode;
                    distance = ComputeWeightedDistance(leftFor, rightFor);
                    return true;

                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    {

                        var leftForEach = (CommonForEachStatementSyntax)leftNode;
                        var rightForEach = (CommonForEachStatementSyntax)rightNode;
                        distance = ComputeWeightedDistance(leftForEach, rightForEach);
                        return true;
                    }

                case SyntaxKind.UsingStatement:
                    var leftUsing = (UsingStatementSyntax)leftNode;
                    var rightUsing = (UsingStatementSyntax)rightNode;

                    if (leftUsing.Declaration != null && rightUsing.Declaration != null)
                    {
                        distance = ComputeWeightedDistance(
                            leftUsing.Declaration,
                            leftUsing.Statement,
                            rightUsing.Declaration,
                            rightUsing.Statement);
                    }
                    else
                    {
                        distance = ComputeWeightedDistance(
                            (SyntaxNode?)leftUsing.Expression ?? leftUsing.Declaration!,
                            leftUsing.Statement,
                            (SyntaxNode?)rightUsing.Expression ?? rightUsing.Declaration!,
                            rightUsing.Statement);
                    }

                    return true;

                case SyntaxKind.LockStatement:
                    var leftLock = (LockStatementSyntax)leftNode;
                    var rightLock = (LockStatementSyntax)rightNode;
                    distance = ComputeWeightedDistance(leftLock.Expression, leftLock.Statement, rightLock.Expression, rightLock.Statement);
                    return true;

                case SyntaxKind.FixedStatement:
                    var leftFixed = (FixedStatementSyntax)leftNode;
                    var rightFixed = (FixedStatementSyntax)rightNode;
                    distance = ComputeWeightedDistance(leftFixed.Declaration, leftFixed.Statement, rightFixed.Declaration, rightFixed.Statement);
                    return true;

                case SyntaxKind.WhileStatement:
                    var leftWhile = (WhileStatementSyntax)leftNode;
                    var rightWhile = (WhileStatementSyntax)rightNode;
                    distance = ComputeWeightedDistance(leftWhile.Condition, leftWhile.Statement, rightWhile.Condition, rightWhile.Statement);
                    return true;

                case SyntaxKind.DoStatement:
                    var leftDo = (DoStatementSyntax)leftNode;
                    var rightDo = (DoStatementSyntax)rightNode;
                    distance = ComputeWeightedDistance(leftDo.Condition, leftDo.Statement, rightDo.Condition, rightDo.Statement);
                    return true;

                case SyntaxKind.IfStatement:
                    var leftIf = (IfStatementSyntax)leftNode;
                    var rightIf = (IfStatementSyntax)rightNode;
                    distance = ComputeWeightedDistance(leftIf.Condition, leftIf.Statement, rightIf.Condition, rightIf.Statement);
                    return true;

                case SyntaxKind.Block:
                    var leftBlock = (BlockSyntax)leftNode;
                    var rightBlock = (BlockSyntax)rightNode;
                    return TryComputeWeightedDistance(leftBlock, rightBlock, out distance);

                case SyntaxKind.CatchClause:
                    distance = ComputeWeightedDistance((CatchClauseSyntax)leftNode, (CatchClauseSyntax)rightNode);
                    return true;

                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                case SyntaxKind.LocalFunctionStatement:
                    distance = ComputeWeightedDistanceOfNestedFunctions(leftNode, rightNode);
                    return true;

                case SyntaxKind.YieldBreakStatement:
                case SyntaxKind.YieldReturnStatement:
                    // Ignore the expression of yield return. The structure of the state machine is more important than the yielded values.
                    distance = (leftNode.RawKind == rightNode.RawKind) ? 0.0 : 0.1;
                    return true;

                case SyntaxKind.SingleVariableDesignation:
                    distance = ComputeWeightedDistance((SingleVariableDesignationSyntax)leftNode, (SingleVariableDesignationSyntax)rightNode);
                    return true;

                default:
                    distance = 0;
                    return false;
            }
        }

        private static double ComputeWeightedDistanceOfNestedFunctions(SyntaxNode leftNode, SyntaxNode rightNode)
        {
            GetNestedFunctionsParts(leftNode, out var leftParameters, out var leftAsync, out var leftBody, out var leftModifiers, out var leftReturnType, out var leftIdentifier, out var leftTypeParameters);
            GetNestedFunctionsParts(rightNode, out var rightParameters, out var rightAsync, out var rightBody, out var rightModifiers, out var rightReturnType, out var rightIdentifier, out var rightTypeParameters);

            if ((leftAsync.Kind() == SyntaxKind.AsyncKeyword) != (rightAsync.Kind() == SyntaxKind.AsyncKeyword))
            {
                return 1.0;
            }

            var modifierDistance = ComputeDistance(leftModifiers, rightModifiers);
            var returnTypeDistance = ComputeDistance(leftReturnType, rightReturnType);
            var identifierDistance = ComputeDistance(leftIdentifier, rightIdentifier);
            var typeParameterDistance = ComputeDistance(leftTypeParameters, rightTypeParameters);
            var parameterDistance = ComputeDistance(leftParameters, rightParameters);
            var bodyDistance = ComputeDistance(leftBody, rightBody);

            return
                modifierDistance * 0.1 +
                returnTypeDistance * 0.1 +
                identifierDistance * 0.2 +
                typeParameterDistance * 0.2 +
                parameterDistance * 0.2 +
                bodyDistance * 0.2;
        }

        private static void GetNestedFunctionsParts(
            SyntaxNode nestedFunction,
            out IEnumerable<SyntaxToken> parameters,
            out SyntaxToken asyncKeyword,
            out SyntaxNode body,
            out SyntaxTokenList modifiers,
            out TypeSyntax? returnType,
            out SyntaxToken identifier,
            out TypeParameterListSyntax? typeParameters)
        {
            switch (nestedFunction.Kind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                    var simple = (SimpleLambdaExpressionSyntax)nestedFunction;
                    parameters = simple.Parameter.DescendantTokens();
                    asyncKeyword = simple.AsyncKeyword;
                    body = simple.Body;
                    modifiers = default;
                    returnType = default;
                    identifier = default;
                    typeParameters = default;
                    break;

                case SyntaxKind.ParenthesizedLambdaExpression:
                    var parenthesized = (ParenthesizedLambdaExpressionSyntax)nestedFunction;
                    parameters = GetDescendantTokensIgnoringSeparators(parenthesized.ParameterList.Parameters);
                    asyncKeyword = parenthesized.AsyncKeyword;
                    body = parenthesized.Body;
                    modifiers = default;
                    returnType = default;
                    identifier = default;
                    typeParameters = default;
                    break;

                case SyntaxKind.AnonymousMethodExpression:
                    var anonymous = (AnonymousMethodExpressionSyntax)nestedFunction;
                    if (anonymous.ParameterList != null)
                    {
                        parameters = GetDescendantTokensIgnoringSeparators(anonymous.ParameterList.Parameters);
                    }
                    else
                    {
                        parameters = SpecializedCollections.EmptyEnumerable<SyntaxToken>();
                    }

                    asyncKeyword = anonymous.AsyncKeyword;
                    body = anonymous.Block;
                    modifiers = default;
                    returnType = default;
                    identifier = default;
                    typeParameters = default;
                    break;

                case SyntaxKind.LocalFunctionStatement:
                    var localFunction = (LocalFunctionStatementSyntax)nestedFunction;
                    parameters = GetDescendantTokensIgnoringSeparators(localFunction.ParameterList.Parameters);
                    asyncKeyword = default;
                    body = (SyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody!;
                    modifiers = localFunction.Modifiers;
                    returnType = localFunction.ReturnType;
                    identifier = localFunction.Identifier;
                    typeParameters = localFunction.TypeParameterList;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(nestedFunction.Kind());
            }
        }

        private bool TryComputeWeightedDistance(BlockSyntax leftBlock, BlockSyntax rightBlock, out double distance)
        {
            // No block can be matched with the root block.
            // Note that in constructors the root is the constructor declaration, since we need to include 
            // the constructor initializer in the match.
            if (leftBlock.Parent == null ||
                rightBlock.Parent == null ||
                leftBlock.Parent.IsKind(SyntaxKind.ConstructorDeclaration) ||
                rightBlock.Parent.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                distance = 0.0;
                return true;
            }

            if (GetLabel(leftBlock.Parent) != GetLabel(rightBlock.Parent))
            {
                distance = 0.2 + 0.8 * ComputeWeightedBlockDistance(leftBlock, rightBlock);
                return true;
            }

            switch (leftBlock.Parent.Kind())
            {
                case SyntaxKind.IfStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.DoStatement:
                case SyntaxKind.FixedStatement:
                case SyntaxKind.LockStatement:
                case SyntaxKind.UsingStatement:
                case SyntaxKind.SwitchSection:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    // value distance of the block body is included:
                    distance = GetDistance(leftBlock.Parent, rightBlock.Parent);
                    return true;

                case SyntaxKind.CatchClause:
                    var leftCatch = (CatchClauseSyntax)leftBlock.Parent;
                    var rightCatch = (CatchClauseSyntax)rightBlock.Parent;
                    if (leftCatch.Declaration == null && leftCatch.Filter == null &&
                        rightCatch.Declaration == null && rightCatch.Filter == null)
                    {
                        var leftTry = (TryStatementSyntax)leftCatch.Parent;
                        var rightTry = (TryStatementSyntax)rightCatch.Parent;

                        distance = 0.5 * ComputeValueDistance(leftTry.Block, rightTry.Block) +
                                   0.5 * ComputeValueDistance(leftBlock, rightBlock);
                    }
                    else
                    {
                        // value distance of the block body is included:
                        distance = GetDistance(leftBlock.Parent, rightBlock.Parent);
                    }

                    return true;

                case SyntaxKind.Block:
                case SyntaxKind.LabeledStatement:
                    distance = ComputeWeightedBlockDistance(leftBlock, rightBlock);
                    return true;

                case SyntaxKind.UnsafeStatement:
                case SyntaxKind.CheckedStatement:
                case SyntaxKind.UncheckedStatement:
                case SyntaxKind.ElseClause:
                case SyntaxKind.FinallyClause:
                case SyntaxKind.TryStatement:
                    distance = 0.2 * ComputeValueDistance(leftBlock, rightBlock);
                    return true;

                default:
                    throw ExceptionUtilities.UnexpectedValue(leftBlock.Parent.Kind());
            }
        }

        private double ComputeWeightedDistance(SingleVariableDesignationSyntax leftNode, SingleVariableDesignationSyntax rightNode)
        {
            var distance = ComputeDistance(leftNode, rightNode);
            double parentDistance;

            if (leftNode.Parent != null &&
                rightNode.Parent != null &&
                GetLabel(leftNode.Parent) == GetLabel(rightNode.Parent))
            {
                parentDistance = ComputeDistance(leftNode.Parent, rightNode.Parent);
            }
            else
            {
                parentDistance = 1;
            }

            return 0.5 * parentDistance + 0.5 * distance;
        }

        private static double ComputeWeightedBlockDistance(BlockSyntax leftBlock, BlockSyntax rightBlock)
        {
            if (TryComputeLocalsDistance(leftBlock, rightBlock, out var distance))
            {
                return distance;
            }

            return ComputeValueDistance(leftBlock, rightBlock);
        }

        private static double ComputeWeightedDistance(CatchClauseSyntax left, CatchClauseSyntax right)
        {
            var blockDistance = ComputeDistance(left.Block, right.Block);
            var distance = CombineOptional(blockDistance, left.Declaration, right.Declaration, left.Filter, right.Filter);
            return AdjustForLocalsInBlock(distance, left.Block, right.Block, localsWeight: 0.3);
        }

        private static double ComputeWeightedDistance(
            CommonForEachStatementSyntax leftCommonForEach,
            CommonForEachStatementSyntax rightCommonForEach)
        {
            var statementDistance = ComputeDistance(leftCommonForEach.Statement, rightCommonForEach.Statement);
            var expressionDistance = ComputeDistance(leftCommonForEach.Expression, rightCommonForEach.Expression);

            List<SyntaxToken>? leftLocals = null;
            List<SyntaxToken>? rightLocals = null;
            GetLocalNames(leftCommonForEach, ref leftLocals);
            GetLocalNames(rightCommonForEach, ref rightLocals);

            var localNamesDistance = ComputeDistance(leftLocals, rightLocals);

            var distance = localNamesDistance * 0.6 + expressionDistance * 0.2 + statementDistance * 0.2;
            return AdjustForLocalsInBlock(distance, leftCommonForEach.Statement, rightCommonForEach.Statement, localsWeight: 0.6);
        }

        private static double ComputeWeightedDistance(ForStatementSyntax left, ForStatementSyntax right)
        {
            var statementDistance = ComputeDistance(left.Statement, right.Statement);
            var conditionDistance = ComputeDistance(left.Condition, right.Condition);

            var incDistance = ComputeDistance(
                GetDescendantTokensIgnoringSeparators(left.Incrementors), GetDescendantTokensIgnoringSeparators(right.Incrementors));

            var distance = conditionDistance * 0.3 + incDistance * 0.3 + statementDistance * 0.4;
            if (TryComputeLocalsDistance(left.Declaration, right.Declaration, out var localsDistance))
            {
                distance = distance * 0.4 + localsDistance * 0.6;
            }

            return distance;
        }

        private static double ComputeWeightedDistance(
            VariableDeclarationSyntax leftVariables,
            StatementSyntax leftStatement,
            VariableDeclarationSyntax rightVariables,
            StatementSyntax rightStatement)
        {
            var distance = ComputeDistance(leftStatement, rightStatement);
            // Put maximum weight behind the variables declared in the header of the statement.
            if (TryComputeLocalsDistance(leftVariables, rightVariables, out var localsDistance))
            {
                distance = distance * 0.4 + localsDistance * 0.6;
            }

            // If the statement is a block that declares local variables, 
            // weight them more than the rest of the statement.
            return AdjustForLocalsInBlock(distance, leftStatement, rightStatement, localsWeight: 0.2);
        }

        private static double ComputeWeightedDistance(
            SyntaxNode? leftHeader,
            StatementSyntax leftStatement,
            SyntaxNode? rightHeader,
            StatementSyntax rightStatement)
        {
            var headerDistance = ComputeDistance(leftHeader, rightHeader);
            var statementDistance = ComputeDistance(leftStatement, rightStatement);
            var distance = headerDistance * 0.6 + statementDistance * 0.4;

            return AdjustForLocalsInBlock(distance, leftStatement, rightStatement, localsWeight: 0.5);
        }

        private static double AdjustForLocalsInBlock(
            double distance,
            StatementSyntax leftStatement,
            StatementSyntax rightStatement,
            double localsWeight)
        {
            // If the statement is a block that declares local variables, 
            // weight them more than the rest of the statement.
            if (leftStatement.Kind() == SyntaxKind.Block && rightStatement.Kind() == SyntaxKind.Block)
            {
                if (TryComputeLocalsDistance((BlockSyntax)leftStatement, (BlockSyntax)rightStatement, out var localsDistance))
                {
                    return localsDistance * localsWeight + distance * (1 - localsWeight);
                }
            }

            return distance;
        }

        private static bool TryComputeLocalsDistance(VariableDeclarationSyntax? left, VariableDeclarationSyntax? right, out double distance)
        {
            List<SyntaxToken>? leftLocals = null;
            List<SyntaxToken>? rightLocals = null;

            if (left != null)
            {
                GetLocalNames(left, ref leftLocals);
            }

            if (right != null)
            {
                GetLocalNames(right, ref rightLocals);
            }

            if (leftLocals == null || rightLocals == null)
            {
                distance = 0;
                return false;
            }

            distance = ComputeDistance(leftLocals, rightLocals);
            return true;
        }

        private static bool TryComputeLocalsDistance(BlockSyntax left, BlockSyntax right, out double distance)
        {
            List<SyntaxToken>? leftLocals = null;
            List<SyntaxToken>? rightLocals = null;

            GetLocalNames(left, ref leftLocals);
            GetLocalNames(right, ref rightLocals);

            if (leftLocals == null || rightLocals == null)
            {
                distance = 0;
                return false;
            }

            distance = ComputeDistance(leftLocals, rightLocals);
            return true;
        }

        // Doesn't include variables declared in declaration expressions
        // Consider including them (https://github.com/dotnet/roslyn/issues/37460).
        private static void GetLocalNames(BlockSyntax block, ref List<SyntaxToken>? result)
        {
            foreach (var child in block.ChildNodes())
            {
                if (child.IsKind(SyntaxKind.LocalDeclarationStatement))
                {
                    GetLocalNames(((LocalDeclarationStatementSyntax)child).Declaration, ref result);
                }
            }
        }

        // Doesn't include variables declared in declaration expressions
        // Consider including them (https://github.com/dotnet/roslyn/issues/37460).
        private static void GetLocalNames(VariableDeclarationSyntax localDeclaration, ref List<SyntaxToken>? result)
        {
            foreach (var local in localDeclaration.Variables)
            {
                GetLocalNames(local.Identifier, ref result);
            }
        }

        internal static void GetLocalNames(CommonForEachStatementSyntax commonForEach, ref List<SyntaxToken>? result)
        {
            switch (commonForEach.Kind())
            {
                case SyntaxKind.ForEachStatement:
                    GetLocalNames(((ForEachStatementSyntax)commonForEach).Identifier, ref result);
                    return;

                case SyntaxKind.ForEachVariableStatement:
                    var forEachVariable = (ForEachVariableStatementSyntax)commonForEach;
                    GetLocalNames(forEachVariable.Variable, ref result);
                    return;

                default:
                    throw ExceptionUtilities.UnexpectedValue(commonForEach.Kind());
            }
        }

        private static void GetLocalNames(ExpressionSyntax expression, ref List<SyntaxToken>? result)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.DeclarationExpression:
                    var declarationExpression = (DeclarationExpressionSyntax)expression;
                    var localDeclaration = declarationExpression.Designation;
                    GetLocalNames(localDeclaration, ref result);
                    return;

                case SyntaxKind.TupleExpression:
                    var tupleExpression = (TupleExpressionSyntax)expression;
                    foreach (var argument in tupleExpression.Arguments)
                    {
                        GetLocalNames(argument.Expression, ref result);
                    }
                    return;

                default:
                    // Do nothing for node that cannot have variable declarations inside.
                    return;
            }
        }

        private static void GetLocalNames(VariableDesignationSyntax designation, ref List<SyntaxToken>? result)
        {
            switch (designation.Kind())
            {
                case SyntaxKind.SingleVariableDesignation:
                    GetLocalNames(((SingleVariableDesignationSyntax)designation).Identifier, ref result);
                    return;

                case SyntaxKind.ParenthesizedVariableDesignation:
                    var parenthesizedVariableDesignation = (ParenthesizedVariableDesignationSyntax)designation;
                    foreach (var variableDesignation in parenthesizedVariableDesignation.Variables)
                    {
                        GetLocalNames(variableDesignation, ref result);
                    }
                    return;

                case SyntaxKind.DiscardDesignation:
                    return;

                default:
                    throw ExceptionUtilities.UnexpectedValue(designation.Kind());
            }
        }

        private static void GetLocalNames(SyntaxToken syntaxToken, [NotNull]ref List<SyntaxToken>? result)
        {
            result ??= new List<SyntaxToken>();
            result.Add(syntaxToken);
        }

        private static double CombineOptional(
            double distance0,
            SyntaxNode? left1,
            SyntaxNode? right1,
            SyntaxNode? left2,
            SyntaxNode? right2,
            double weight0 = 0.8,
            double weight1 = 0.5)
        {
            var one = left1 != null || right1 != null;
            var two = left2 != null || right2 != null;

            if (!one && !two)
            {
                return distance0;
            }

            var distance1 = ComputeDistance(left1, right1);
            var distance2 = ComputeDistance(left2, right2);

            double d;
            if (one && two)
            {
                d = distance1 * weight1 + distance2 * (1 - weight1);
            }
            else if (one)
            {
                d = distance1;
            }
            else
            {
                d = distance2;
            }

            return distance0 * weight0 + d * (1 - weight0);
        }

        #endregion
    }
}
