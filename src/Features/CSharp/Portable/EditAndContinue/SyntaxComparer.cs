﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue
{
    internal sealed class SyntaxComparer : AbstractSyntaxComparer
    {
        internal static readonly SyntaxComparer TopLevel = new();
        internal static readonly SyntaxComparer Statement = new(compareStatementSyntax: true);

        private readonly SyntaxNode? _oldRoot;
        private readonly SyntaxNode? _newRoot;
        private readonly IEnumerable<SyntaxNode>? _oldRootChildren;
        private readonly IEnumerable<SyntaxNode>? _newRootChildren;

        // This comparer can operate in two modes: 
        // * Top level syntax, which looks at member declarations, but doesn't look inside method bodies etc.
        // * Statement syntax, which looks into member bodies and descends through all statements and expressions
        // This flag is used where there needs to be a disctinction made between how these are treated
        private readonly bool _compareStatementSyntax;

        private SyntaxComparer()
        {
        }

        public SyntaxComparer(bool compareStatementSyntax)
        {
            _compareStatementSyntax = compareStatementSyntax;
        }

        /// <summary>
        /// Creates a syntax comparer
        /// </summary>
        /// <param name="oldRoot">The root node to start comparisons from</param>
        /// <param name="newRoot">The new root node to compare against</param>
        /// <param name="oldRootChildren">Child nodes that should always be compared</param>
        /// <param name="newRootChildren">New child nodes to compare against</param>
        /// <param name="compareStatementSyntax">Whether this comparer is in "statement mode"</param>
        public SyntaxComparer(
            SyntaxNode oldRoot,
            SyntaxNode newRoot,
            IEnumerable<SyntaxNode> oldRootChildren,
            IEnumerable<SyntaxNode> newRootChildren,
            bool compareStatementSyntax = false)
        {
            // Set this first in case there are asserts, so they evaluate the right thing
            _compareStatementSyntax = compareStatementSyntax;

            if (!_compareStatementSyntax)
            {
                // for top syntax, explicitly listed roots and all their children must be labeled:
                Debug.Assert(HasLabel(oldRoot));
                Debug.Assert(HasLabel(newRoot));
                Debug.Assert(oldRootChildren.All(HasLabel));
                Debug.Assert(newRootChildren.All(HasLabel));
            }

            _oldRoot = oldRoot;
            _newRoot = newRoot;
            _oldRootChildren = oldRootChildren;
            _newRootChildren = newRootChildren;

            if (!_compareStatementSyntax)
            {
                // For top syntax the virtual parent of root children must be the respective root:
                Debug.Assert(!TryGetParent(oldRoot, out var _));
                Debug.Assert(!TryGetParent(newRoot, out var _));
                Debug.Assert(oldRootChildren.All(node => TryGetParent(node, out var parent) && parent == oldRoot));
                Debug.Assert(newRootChildren.All(node => TryGetParent(node, out var parent) && parent == newRoot));
            }
        }

        #region Tree Traversal

        protected internal override bool TryGetParent(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? parent)
        {
            if (node == _oldRoot || node == _newRoot)
            {
                parent = null;
                return false;
            }

            parent = node.Parent;
            while (parent != null && !HasLabel(parent))
            {
                parent = parent.Parent;
            }

            return parent != null;
        }

        protected internal override IEnumerable<SyntaxNode>? GetChildren(SyntaxNode node)
        {
            if (node == _oldRoot)
            {
                return _oldRootChildren;
            }

            if (node == _newRoot)
            {
                return _newRootChildren;
            }

            return HasChildren(node) ? EnumerateChildren(node) : null;
        }

        private IEnumerable<SyntaxNode> EnumerateChildren(SyntaxNode node)
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
                else if (_compareStatementSyntax)
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
        private bool DescendIntoChildren(SyntaxNode node)
            => !LambdaUtilities.IsLambdaBodyStatementOrExpression(node) && !HasLabel(node);

        protected internal sealed override IEnumerable<SyntaxNode> GetDescendants(SyntaxNode node)
        {
            var rootChildren = (node == _oldRoot) ? _oldRootChildren : (node == _newRoot) ? _newRootChildren : null;
            return (rootChildren != null) ? EnumerateDescendants(rootChildren) : EnumerateDescendants(node);
        }

        private IEnumerable<SyntaxNode> EnumerateDescendants(IEnumerable<SyntaxNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (HasLabel(node))
                {
                    yield return node;
                }

                foreach (var descendant in EnumerateDescendants(node))
                {
                    if (HasLabel(descendant))
                    {
                        yield return descendant;
                    }
                }
            }
        }

        private IEnumerable<SyntaxNode> EnumerateDescendants(SyntaxNode node)
        {
            foreach (var descendant in node.DescendantNodesAndTokens(
                descendIntoChildren: child => ShouldEnumerateChildren(child),
                descendIntoTrivia: false))
            {
                var descendantNode = descendant.AsNode();
                if (descendantNode != null && HasLabel(descendantNode))
                {
                    if (!LambdaUtilities.IsLambdaBodyStatementOrExpression(descendantNode))
                    {
                        yield return descendantNode;
                    }
                }
            }

            bool ShouldEnumerateChildren(SyntaxNode child)
            {
                // if we don't want to consider this nodes children, then don't
                if (!HasChildren(child))
                {
                    return false;
                }

                // Always descend into the children of the node we were asked about
                if (child == node)
                {
                    return true;
                }

                // otherwise, as long as we don't descend into lambdas
                return !LambdaUtilities.IsLambdaBodyStatementOrExpression(child);
            }
        }

        private bool HasChildren(SyntaxNode node)
        {
            // Leaves are labeled statements that don't have a labeled child.
            // We also return true for non-labeled statements.
            var label = Classify(node.Kind(), node, out var isLeaf);

            // ignored should always be reported as leaves for top syntax, but for statements
            // we want to look at all child nodes, because almost anything could have a lambda
            if (!_compareStatementSyntax)
            {
                Debug.Assert(label != Label.Ignored || isLeaf);
            }

            return !isLeaf;
        }

        #endregion

        #region Labels

        // Assumptions:
        // - Each listed label corresponds to one or more syntax kinds.
        // - Nodes with same labels might produce Update edits, nodes with different labels don't. 
        // - If IsTiedToParent(label) is true for a label then all its possible parent labels must precede the label.
        //   (i.e. both MethodDeclaration and TypeDeclaration must precede TypeParameter label).
        // - All descendants of a node whose kind is listed here will be ignored regardless of their labels
        internal enum Label
        {
            // Top level syntax kinds
            CompilationUnit,

            NamespaceDeclaration,
            ExternAliasDirective,              // tied to parent 
            UsingDirective,                    // tied to parent

            TypeDeclaration,
            EnumDeclaration,
            DelegateDeclaration,

            FieldDeclaration,                  // tied to parent
            FieldVariableDeclaration,          // tied to parent
            FieldVariableDeclarator,           // tied to parent

            MethodDeclaration,                 // tied to parent
            OperatorDeclaration,               // tied to parent
            ConversionOperatorDeclaration,     // tied to parent
            ConstructorDeclaration,            // tied to parent
            DestructorDeclaration,             // tied to parent
            PropertyDeclaration,               // tied to parent
            IndexerDeclaration,                // tied to parent
            EventDeclaration,                  // tied to parent
            EnumMemberDeclaration,             // tied to parent

            AccessorList,                      // tied to parent
            AccessorDeclaration,               // tied to parent

            // Statement syntax kinds
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
            SwitchExpression,
            SwitchExpressionArm,               // tied to parent
            WhenClause,                        // tied to parent

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

            // Syntax kinds that are common to both statement and top level
            TypeParameterList,                 // tied to parent
            TypeParameterConstraintClause,     // tied to parent
            TypeParameter,                     // tied to parent
            ParameterList,                     // tied to parent
            BracketedParameterList,            // tied to parent
            Parameter,                         // tied to parent
            AttributeList,                     // tied to parent
            Attribute,                         // tied to parent

            // helpers:
            Count,
            Ignored = IgnoredNode
        }

        /// <summary>
        /// Return 1 if it is desirable to report two edits (delete and insert) rather than a move edit
        /// when the node changes its parent.
        /// </summary>
        private static int TiedToAncestor(Label label)
        {
            switch (label)
            {
                // Top level syntax
                case Label.ExternAliasDirective:
                case Label.UsingDirective:
                case Label.FieldDeclaration:
                case Label.FieldVariableDeclaration:
                case Label.FieldVariableDeclarator:
                case Label.MethodDeclaration:
                case Label.OperatorDeclaration:
                case Label.ConversionOperatorDeclaration:
                case Label.ConstructorDeclaration:
                case Label.DestructorDeclaration:
                case Label.PropertyDeclaration:
                case Label.IndexerDeclaration:
                case Label.EventDeclaration:
                case Label.EnumMemberDeclaration:
                case Label.AccessorDeclaration:
                case Label.AccessorList:
                case Label.TypeParameterList:
                case Label.TypeParameter:
                case Label.TypeParameterConstraintClause:
                case Label.ParameterList:
                case Label.BracketedParameterList:
                case Label.Parameter:
                case Label.AttributeList:
                case Label.Attribute:
                    return 1;

                // Statement syntax
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
                case Label.WhenClause:
                case Label.SwitchExpressionArm:
                    return 1;

                default:
                    return 0;
            }
        }

        internal Label Classify(SyntaxKind kind, SyntaxNode? node, out bool isLeaf)
        {
            isLeaf = false;

            // If the node is a for loop Initializer, Condition, or Incrementor expression we label it as "ForStatementPart".
            // We need to capture it in the match since these expressions can be "active statements" and as such we need to map them.
            //
            // The parent is not available only when comparing nodes for value equality.
            if (node != null && node.Parent.IsKind(SyntaxKind.ForStatement) && node is ExpressionSyntax)
            {
                return Label.ForStatementPart;
            }

            switch (kind)
            {
                // ************************************
                // Top syntax
                // ************************************

                case SyntaxKind.CompilationUnit:
                    return Label.CompilationUnit;

                case SyntaxKind.GlobalStatement:
                    // TODO:
                    isLeaf = true;
                    return Label.Ignored;

                case SyntaxKind.ExternAliasDirective:
                    isLeaf = true;
                    return Label.ExternAliasDirective;

                case SyntaxKind.UsingDirective:
                    isLeaf = true;
                    return Label.UsingDirective;

                case SyntaxKind.NamespaceDeclaration:
                    return Label.NamespaceDeclaration;

                // Need to add support for records (tracked by https://github.com/dotnet/roslyn/issues/44877)
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                    return Label.TypeDeclaration;

                case SyntaxKind.EnumDeclaration:
                    return Label.EnumDeclaration;

                case SyntaxKind.DelegateDeclaration:
                    return Label.DelegateDeclaration;

                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                    return Label.FieldDeclaration;

                case SyntaxKind.ConversionOperatorDeclaration:
                    return Label.ConversionOperatorDeclaration;

                case SyntaxKind.OperatorDeclaration:
                    return Label.OperatorDeclaration;

                case SyntaxKind.DestructorDeclaration:
                    isLeaf = true;
                    return Label.DestructorDeclaration;

                case SyntaxKind.PropertyDeclaration:
                    return Label.PropertyDeclaration;

                case SyntaxKind.IndexerDeclaration:
                    return Label.IndexerDeclaration;

                case SyntaxKind.EventDeclaration:
                    return Label.EventDeclaration;

                case SyntaxKind.EnumMemberDeclaration:
                    // not a leaf because an attribute may be applied
                    return Label.EnumMemberDeclaration;

                case SyntaxKind.AccessorList:
                    return Label.AccessorList;

                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    isLeaf = true;
                    return Label.AccessorDeclaration;

                case SyntaxKind.TypeParameterList:
                    return Label.TypeParameterList;

                case SyntaxKind.TypeParameterConstraintClause:
                    return Label.TypeParameterConstraintClause;

                case SyntaxKind.TypeParameter:
                    // not a leaf because an attribute may be applied
                    return Label.TypeParameter;

                case SyntaxKind.BracketedParameterList:
                    return Label.BracketedParameterList;

                case SyntaxKind.ParameterList:
                    return Label.ParameterList;

                case SyntaxKind.Parameter:
                    return Label.Parameter;

                case SyntaxKind.AttributeList:
                    return Label.AttributeList;

                case SyntaxKind.Attribute:
                    isLeaf = true;
                    return Label.Attribute;

                // ************************************
                // Statement syntax
                // ************************************

                // Notes:
                // A descendant of a leaf node may be a labeled node that we don't want to visit if 
                // we are comparing its parent node (used for lambda bodies).
                // 
                // Expressions are ignored but they may contain nodes that should be matched by tree comparer.
                // (e.g. lambdas, declaration expressions). Descending to these nodes is handled in EnumerateChildren.

                case SyntaxKind.ConstructorDeclaration:
                    // Root when matching constructor bodies.
                    return Label.ConstructorDeclaration;

                case SyntaxKind.LocalDeclarationStatement:
                    return Label.LocalDeclarationStatement;

                case SyntaxKind.SingleVariableDesignation:
                    return Label.SingleVariableDesignation;

                case SyntaxKind.LabeledStatement:
                    return Label.LabeledStatement;

                case SyntaxKind.EmptyStatement:
                    isLeaf = true;
                    return Label.ExpressionStatement;

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

                case SyntaxKind.SwitchExpression:
                    return Label.SwitchExpression;

                case SyntaxKind.SwitchExpressionArm:
                    return Label.SwitchExpressionArm;

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
                case SyntaxKind.AnonymousMethodExpression:
                    // Note: Simple lambda expression is only labeled for statements, so it is below
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
            }

            // These nodes could be seen during top or statement processing, but need different results
            if (_compareStatementSyntax)
            {
                // Statement syntax
                switch (kind)
                {
                    case SyntaxKind.SimpleLambdaExpression:
                        return Label.NestedFunction;

                    case SyntaxKind.VariableDeclaration:
                        return Label.LocalVariableDeclaration;

                    case SyntaxKind.VariableDeclarator:
                        return Label.LocalVariableDeclarator;

                    case SyntaxKind.Block:
                        return Label.Block;
                }
            }
            else
            {
                // Top syntax
                switch (kind)
                {
                    case SyntaxKind.VariableDeclaration:
                        return Label.FieldVariableDeclaration;

                    case SyntaxKind.VariableDeclarator:
                        // For top syntax, a variable declarator is a leaf node
                        isLeaf = true;
                        return Label.FieldVariableDeclarator;

                    case SyntaxKind.MethodDeclaration:
                        return Label.MethodDeclaration;

                    case SyntaxKind.ConstructorDeclaration:
                        return Label.ConstructorDeclaration;
                }
            }

            // If we got this far, its an unlabelled node. Since just about any node can
            // contain a lambda, isLeaf must be true for statement syntax but for top
            // syntax, we don't need to descend into any ignored nodes
            isLeaf = !_compareStatementSyntax;
            return Label.Ignored;
        }

        protected internal override int GetLabel(SyntaxNode node)
            => (int)Classify(node.Kind(), node, out _);

        // internal for testing
        internal bool HasLabel(SyntaxKind kind)
            => Classify(kind, node: null, out _) != Label.Ignored;

        internal bool HasLabel(SyntaxNode node)
            => Classify(node.Kind(), node, out _) != Label.Ignored;

        protected internal override int LabelCount
            => (int)Label.Count;

        protected internal override int TiedToAncestor(int label)
            => TiedToAncestor((Label)label);

        #endregion

        #region Comparisons

        public override bool ValuesEqual(SyntaxNode left, SyntaxNode right)
        {
            Func<SyntaxKind, bool>? ignoreChildFunction;
            switch (left.Kind())
            {
                // all syntax kinds with a method body child:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    // When comparing method bodies we need to NOT ignore VariableDeclaration and VariableDeclarator children,
                    // but when comparing field definitions we should ignore VariableDeclarations children.

                    var leftBody = GetBody(left);
                    var rightBody = GetBody(right);

                    if (!SyntaxFactory.AreEquivalent(leftBody, rightBody, null))
                    {
                        return false;
                    }

                    ignoreChildFunction = childKind => childKind == SyntaxKind.Block || childKind == SyntaxKind.ArrowExpressionClause || HasLabel(childKind);
                    break;

                case SyntaxKind.SwitchSection:
                    return Equal((SwitchSectionSyntax)left, (SwitchSectionSyntax)right);

                case SyntaxKind.ForStatement:
                    // The only children of ForStatement are labeled nodes and punctuation.
                    return true;

                default:
                    if (HasChildren(left))
                    {
                        ignoreChildFunction = childKind => HasLabel(childKind);
                    }
                    else
                    {
                        ignoreChildFunction = null;
                    }

                    break;
            }

            return SyntaxFactory.AreEquivalent(left, right, ignoreChildFunction);
        }

        private bool Equal(SwitchSectionSyntax left, SwitchSectionSyntax right)
        {
            return SyntaxFactory.AreEquivalent(left.Labels, right.Labels, null)
                && SyntaxFactory.AreEquivalent(left.Statements, right.Statements, ignoreChildNode: HasLabel);
        }

        private static SyntaxNode? GetBody(SyntaxNode node)
        {
            switch (node)
            {
                case BaseMethodDeclarationSyntax baseMethodDeclarationSyntax: return baseMethodDeclarationSyntax.Body ?? (SyntaxNode?)baseMethodDeclarationSyntax.ExpressionBody?.Expression;
                case AccessorDeclarationSyntax accessorDeclarationSyntax: return accessorDeclarationSyntax.Body ?? (SyntaxNode?)accessorDeclarationSyntax.ExpressionBody?.Expression;
                default: throw ExceptionUtilities.UnexpectedValue(node);
            }
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

                case SyntaxKind.TypeParameterConstraintClause:
                    distance = ComputeDistance((TypeParameterConstraintClauseSyntax)leftNode, (TypeParameterConstraintClauseSyntax)rightNode);
                    return true;

                case SyntaxKind.TypeParameter:
                    distance = ComputeDistance((TypeParameterSyntax)leftNode, (TypeParameterSyntax)rightNode);
                    return true;

                case SyntaxKind.Parameter:
                    distance = ComputeDistance((ParameterSyntax)leftNode, (ParameterSyntax)rightNode);
                    return true;

                case SyntaxKind.AttributeList:
                    distance = ComputeDistance((AttributeListSyntax)leftNode, (AttributeListSyntax)rightNode);
                    return true;

                case SyntaxKind.Attribute:
                    distance = ComputeDistance((AttributeSyntax)leftNode, (AttributeSyntax)rightNode);
                    return true;

                default:
                    var leftName = TryGetName(leftNode);
                    var rightName = TryGetName(rightNode);
                    Contract.ThrowIfFalse(rightName.HasValue == leftName.HasValue);

                    if (leftName.HasValue)
                    {
                        distance = ComputeDistance(leftName.Value, rightName!.Value);
                        return true;
                    }
                    else
                    {
                        distance = 0;
                        return false;
                    }
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
                    returnType = null;
                    identifier = default;
                    typeParameters = null;
                    break;

                case SyntaxKind.ParenthesizedLambdaExpression:
                    var parenthesized = (ParenthesizedLambdaExpressionSyntax)nestedFunction;
                    parameters = GetDescendantTokensIgnoringSeparators(parenthesized.ParameterList.Parameters);
                    asyncKeyword = parenthesized.AsyncKeyword;
                    body = parenthesized.Body;
                    modifiers = default;
                    returnType = null;
                    identifier = default;
                    typeParameters = null;
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
                    returnType = null;
                    identifier = default;
                    typeParameters = null;
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
                case SyntaxKind.LocalFunctionStatement:
                    // value distance of the block body is included:
                    distance = GetDistance(leftBlock.Parent, rightBlock.Parent);
                    return true;

                case SyntaxKind.CatchClause:
                    var leftCatch = (CatchClauseSyntax)leftBlock.Parent;
                    var rightCatch = (CatchClauseSyntax)rightBlock.Parent;
                    if (leftCatch.Declaration == null && leftCatch.Filter == null &&
                        rightCatch.Declaration == null && rightCatch.Filter == null)
                    {
                        var leftTry = (TryStatementSyntax)leftCatch.Parent!;
                        var rightTry = (TryStatementSyntax)rightCatch.Parent!;

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
                if (child.IsKind(SyntaxKind.LocalDeclarationStatement, out LocalDeclarationStatementSyntax? localDecl))
                {
                    GetLocalNames(localDecl.Declaration, ref result);
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

        private static void GetLocalNames(SyntaxToken syntaxToken, [NotNull] ref List<SyntaxToken>? result)
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

        private static SyntaxNodeOrToken? TryGetName(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ExternAliasDirective:
                    return ((ExternAliasDirectiveSyntax)node).Identifier;

                case SyntaxKind.UsingDirective:
                    return ((UsingDirectiveSyntax)node).Name;

                case SyntaxKind.NamespaceDeclaration:
                    return ((NamespaceDeclarationSyntax)node).Name;

                // Need to add support for records (tracked by https://github.com/dotnet/roslyn/issues/44877)
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                    return ((TypeDeclarationSyntax)node).Identifier;

                case SyntaxKind.EnumDeclaration:
                    return ((EnumDeclarationSyntax)node).Identifier;

                case SyntaxKind.DelegateDeclaration:
                    return ((DelegateDeclarationSyntax)node).Identifier;

                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.VariableDeclaration:
                    return null;

                case SyntaxKind.VariableDeclarator:
                    return ((VariableDeclaratorSyntax)node).Identifier;

                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)node).Identifier;

                case SyntaxKind.ConversionOperatorDeclaration:
                    return ((ConversionOperatorDeclarationSyntax)node).Type;

                case SyntaxKind.OperatorDeclaration:
                    return ((OperatorDeclarationSyntax)node).OperatorToken;

                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)node).Identifier;

                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)node).Identifier;

                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)node).Identifier;

                case SyntaxKind.IndexerDeclaration:
                    return null;

                case SyntaxKind.EventDeclaration:
                    return ((EventDeclarationSyntax)node).Identifier;

                case SyntaxKind.EnumMemberDeclaration:
                    return ((EnumMemberDeclarationSyntax)node).Identifier;

                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                    return null;

                case SyntaxKind.TypeParameterConstraintClause:
                    return ((TypeParameterConstraintClauseSyntax)node).Name.Identifier;

                case SyntaxKind.TypeParameter:
                    return ((TypeParameterSyntax)node).Identifier;

                case SyntaxKind.TypeParameterList:
                case SyntaxKind.ParameterList:
                case SyntaxKind.BracketedParameterList:
                    return null;

                case SyntaxKind.Parameter:
                    return ((ParameterSyntax)node).Identifier;

                case SyntaxKind.AttributeList:
                    return ((AttributeListSyntax)node).Target;

                case SyntaxKind.Attribute:
                    return ((AttributeSyntax)node).Name;

                default:
                    return null;
            }
        }

        #endregion
    }
}
