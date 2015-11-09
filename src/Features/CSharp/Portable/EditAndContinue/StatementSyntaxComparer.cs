// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue
{
    internal sealed class StatementSyntaxComparer : SyntaxComparer
    {
        internal static readonly StatementSyntaxComparer Default = new StatementSyntaxComparer();

        private readonly SyntaxNode _oldRootChild;
        private readonly SyntaxNode _newRootChild;
        private readonly SyntaxNode _oldRoot;
        private readonly SyntaxNode _newRoot;

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

        protected internal override IEnumerable<SyntaxNode> GetChildren(SyntaxNode node)
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
            Debug.Assert(_oldRoot != null && _newRoot != null);

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
                Debug.Assert(_oldRoot != null && _newRoot != null);

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
            bool isLeaf;
            Classify(node.Kind(), node, out isLeaf);
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

            AwaitExpression,

            LocalFunction,

            Lambda,

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
                case Label.LocalFunction:
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
                    return 1;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// <paramref name="nodeOpt"/> is null only when comparing value equality of a tree node.
        /// </summary>
        internal static Label Classify(SyntaxKind kind, SyntaxNode nodeOpt, out bool isLeaf)
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
            if (nodeOpt != null && nodeOpt.Parent.IsKind(SyntaxKind.ForStatement) && nodeOpt is ExpressionSyntax)
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
                    return Label.LocalFunction;

                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    return Label.Lambda;

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
                    if (nodeOpt == null || nodeOpt.Parent.IsKind(SyntaxKind.QueryExpression))
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
                case SyntaxKind.ArrayType:
                case SyntaxKind.ArrayRankSpecifier:
                case SyntaxKind.PointerType:
                case SyntaxKind.NullableType:
                case SyntaxKind.OmittedTypeArgument:
                case SyntaxKind.NameColon:
                case SyntaxKind.StackAllocArrayCreationExpression:
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
            bool isLeaf;
            return Classify(node.Kind(), node, out isLeaf);
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

            bool isLeaf;
            return Classify(kind, null, out isLeaf) != Label.Ignored;
        }

        public override bool ValuesEqual(SyntaxNode left, SyntaxNode right)
        {
            // only called from the tree matching alg, which only operates on nodes that are labeled.
            Debug.Assert(HasLabel(left));
            Debug.Assert(HasLabel(right));

            Func<SyntaxKind, bool> ignoreChildNode;
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
                    var leftForEach = (ForEachStatementSyntax)leftNode;
                    var rightForEach = (ForEachStatementSyntax)rightNode;
                    distance = ComputeWeightedDistance(leftForEach, rightForEach);
                    return true;

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
                            (SyntaxNode)leftUsing.Expression ?? leftUsing.Declaration,
                            leftUsing.Statement,
                            (SyntaxNode)rightUsing.Expression ?? rightUsing.Declaration,
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
                    BlockSyntax leftBlock = (BlockSyntax)leftNode;
                    BlockSyntax rightBlock = (BlockSyntax)rightNode;
                    return TryComputeWeightedDistance(leftBlock, rightBlock, out distance);

                case SyntaxKind.CatchClause:
                    distance = ComputeWeightedDistance((CatchClauseSyntax)leftNode, (CatchClauseSyntax)rightNode);
                    return true;

                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    distance = ComputeWeightedDistanceOfLambdas(leftNode, rightNode);
                    return true;

                case SyntaxKind.LocalFunctionStatement:
                    distance = ComputeWeightedDistanceOfLocalFunctions((LocalFunctionStatementSyntax)leftNode, (LocalFunctionStatementSyntax)rightNode);
                    return true;

                case SyntaxKind.YieldBreakStatement:
                case SyntaxKind.YieldReturnStatement:
                    // Ignore the expression of yield return. The structure of the state machine is more important than the yielded values.
                    distance = (leftNode.RawKind == rightNode.RawKind) ? 0.0 : 0.1;
                    return true;

                default:
                    distance = 0;
                    return false;
            }
        }

        private static double ComputeWeightedDistanceOfLocalFunctions(LocalFunctionStatementSyntax leftNode, LocalFunctionStatementSyntax rightNode)
        {
            double modifierDistance = ComputeDistance(leftNode.Modifiers, rightNode.Modifiers);
            double returnTypeDistance = ComputeDistance(leftNode.ReturnType, rightNode.ReturnType);
            double identifierDistance = ComputeDistance(leftNode.Identifier, rightNode.Identifier);
            double typeParameterDistance = ComputeDistance(leftNode.TypeParameterList, rightNode.TypeParameterList);
            double parameterDistance = ComputeDistance(leftNode.ParameterList.Parameters, rightNode.ParameterList.Parameters);
            double bodyDistance = ComputeDistance((SyntaxNode)leftNode.Body ?? leftNode.ExpressionBody, (SyntaxNode)rightNode.Body ?? rightNode.ExpressionBody);

            return
                modifierDistance * 0.1 +
                returnTypeDistance * 0.1 +
                identifierDistance * 0.2 +
                typeParameterDistance * 0.2 +
                parameterDistance * 0.2 +
                bodyDistance * 0.2;
        }

        private static double ComputeWeightedDistanceOfLambdas(SyntaxNode leftNode, SyntaxNode rightNode)
        {
            IEnumerable<SyntaxToken> leftParameters, rightParameters;
            SyntaxToken leftAsync, rightAsync;
            SyntaxNode leftBody, rightBody;

            GetLambdaParts(leftNode, out leftParameters, out leftAsync, out leftBody);
            GetLambdaParts(rightNode, out rightParameters, out rightAsync, out rightBody);

            if ((leftAsync.Kind() == SyntaxKind.AsyncKeyword) != (rightAsync.Kind() == SyntaxKind.AsyncKeyword))
            {
                return 1.0;
            }

            double parameterDistance = ComputeDistance(leftParameters, rightParameters);
            double bodyDistance = ComputeDistance(leftBody, rightBody);

            return parameterDistance * 0.6 + bodyDistance * 0.4;
        }

        private static void GetLambdaParts(SyntaxNode lambda, out IEnumerable<SyntaxToken> parameters, out SyntaxToken asyncKeyword, out SyntaxNode body)
        {
            switch (lambda.Kind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                    var simple = (SimpleLambdaExpressionSyntax)lambda;
                    parameters = simple.Parameter.DescendantTokens();
                    asyncKeyword = simple.AsyncKeyword;
                    body = simple.Body;
                    break;

                case SyntaxKind.ParenthesizedLambdaExpression:
                    var parenthesized = (ParenthesizedLambdaExpressionSyntax)lambda;
                    parameters = GetDescendantTokensIgnoringSeparators(parenthesized.ParameterList.Parameters);
                    asyncKeyword = parenthesized.AsyncKeyword;
                    body = parenthesized.Body;
                    break;

                case SyntaxKind.AnonymousMethodExpression:
                    var anonymous = (AnonymousMethodExpressionSyntax)lambda;
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
                    break;

                default:
                    throw ExceptionUtilities.Unreachable;
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

            if (leftBlock.Parent.Kind() != rightBlock.Parent.Kind())
            {
                distance = 0.2 + 0.8 * ComputeWeightedBlockDistance(leftBlock, rightBlock);
                return true;
            }

            switch (leftBlock.Parent.Kind())
            {
                case SyntaxKind.IfStatement:
                case SyntaxKind.ForEachStatement:
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
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private static double ComputeWeightedBlockDistance(BlockSyntax leftBlock, BlockSyntax rightBlock)
        {
            double distance;
            if (TryComputeLocalsDistance(leftBlock, rightBlock, out distance))
            {
                return distance;
            }

            return ComputeValueDistance(leftBlock, rightBlock);
        }

        private static double ComputeWeightedDistance(CatchClauseSyntax left, CatchClauseSyntax right)
        {
            double blockDistance = ComputeDistance(left.Block, right.Block);
            double distance = CombineOptional(blockDistance, left.Declaration, right.Declaration, left.Filter, right.Filter);
            return AdjustForLocalsInBlock(distance, left.Block, right.Block, localsWeight: 0.3);
        }

        private static double ComputeWeightedDistance(ForEachStatementSyntax left, ForEachStatementSyntax right)
        {
            double statementDistance = ComputeDistance(left.Statement, right.Statement);
            double expressionDistance = ComputeDistance(left.Expression, right.Expression);
            double identifierDistance = ComputeDistance(left.Identifier, right.Identifier);

            double distance = identifierDistance * 0.7 + expressionDistance * 0.2 + statementDistance * 0.1;
            return AdjustForLocalsInBlock(distance, left.Statement, right.Statement, localsWeight: 0.6);
        }

        private static double ComputeWeightedDistance(ForStatementSyntax left, ForStatementSyntax right)
        {
            double statementDistance = ComputeDistance(left.Statement, right.Statement);
            double conditionDistance = ComputeDistance(left.Condition, right.Condition);

            double incDistance = ComputeDistance(
                GetDescendantTokensIgnoringSeparators(left.Incrementors), GetDescendantTokensIgnoringSeparators(right.Incrementors));

            double distance = conditionDistance * 0.3 + incDistance * 0.3 + statementDistance * 0.4;

            double localsDistance;
            if (TryComputeLocalsDistance(left.Declaration, right.Declaration, out localsDistance))
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
            double distance = ComputeDistance(leftStatement, rightStatement);

            // Put maximum weight behind the variables declared in the header of the statement.
            double localsDistance;
            if (TryComputeLocalsDistance(leftVariables, rightVariables, out localsDistance))
            {
                distance = distance * 0.4 + localsDistance * 0.6;
            }

            // If the statement is a block that declares local variables, 
            // weight them more than the rest of the statement.
            return AdjustForLocalsInBlock(distance, leftStatement, rightStatement, localsWeight: 0.2);
        }

        private static double ComputeWeightedDistance(
            SyntaxNode leftHeaderOpt,
            StatementSyntax leftStatement,
            SyntaxNode rightHeaderOpt,
            StatementSyntax rightStatement)
        {
            Debug.Assert(leftStatement != null);
            Debug.Assert(rightStatement != null);

            double headerDistance = ComputeDistance(leftHeaderOpt, rightHeaderOpt);
            double statementDistance = ComputeDistance(leftStatement, rightStatement);
            double distance = headerDistance * 0.6 + statementDistance * 0.4;

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
                double localsDistance;
                if (TryComputeLocalsDistance((BlockSyntax)leftStatement, (BlockSyntax)rightStatement, out localsDistance))
                {
                    return localsDistance * localsWeight + distance * (1 - localsWeight);
                }
            }

            return distance;
        }

        private static bool TryComputeLocalsDistance(VariableDeclarationSyntax leftOpt, VariableDeclarationSyntax rightOpt, out double distance)
        {
            List<SyntaxToken> leftLocals = null;
            List<SyntaxToken> rightLocals = null;

            if (leftOpt != null)
            {
                GetLocalNames(leftOpt, ref leftLocals);
            }

            if (rightOpt != null)
            {
                GetLocalNames(rightOpt, ref rightLocals);
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
            List<SyntaxToken> leftLocals = null;
            List<SyntaxToken> rightLocals = null;

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

        // doesn't include variables declared in declaration expressions
        private static void GetLocalNames(BlockSyntax block, ref List<SyntaxToken> result)
        {
            foreach (var child in block.ChildNodes())
            {
                if (child.IsKind(SyntaxKind.LocalDeclarationStatement))
                {
                    GetLocalNames(((LocalDeclarationStatementSyntax)child).Declaration, ref result);
                }
            }
        }

        // doesn't include variables declared in declaration expressions
        private static void GetLocalNames(VariableDeclarationSyntax localDeclaration, ref List<SyntaxToken> result)
        {
            foreach (var local in localDeclaration.Variables)
            {
                if (result == null)
                {
                    result = new List<SyntaxToken>();
                }

                result.Add(local.Identifier);
            }
        }

        private static double CombineOptional(
            double distance0,
            SyntaxNode leftOpt1,
            SyntaxNode rightOpt1,
            SyntaxNode leftOpt2,
            SyntaxNode rightOpt2,
            double weight0 = 0.8,
            double weight1 = 0.5)
        {
            bool one = leftOpt1 != null || rightOpt1 != null;
            bool two = leftOpt2 != null || rightOpt2 != null;

            if (!one && !two)
            {
                return distance0;
            }

            double distance1 = ComputeDistance(leftOpt1, rightOpt1);
            double distance2 = ComputeDistance(leftOpt2, rightOpt2);

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
