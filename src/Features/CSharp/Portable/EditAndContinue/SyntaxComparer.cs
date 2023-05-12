// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Differencing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue
{
    internal sealed class SyntaxComparer : AbstractSyntaxComparer
    {
        internal static readonly SyntaxComparer TopLevel = new(null, null, null, null, compareStatementSyntax: false);
        internal static readonly SyntaxComparer Statement = new(null, null, null, null, compareStatementSyntax: true);

        /// <summary>
        /// Creates a syntax comparer
        /// </summary>
        /// <param name="oldRoot">The root node to start comparisons from</param>
        /// <param name="newRoot">The new root node to compare against</param>
        /// <param name="oldRootChildren">Child nodes that should always be compared</param>
        /// <param name="newRootChildren">New child nodes to compare against</param>
        /// <param name="compareStatementSyntax">Whether this comparer is in "statement mode"</param>
        public SyntaxComparer(
            SyntaxNode? oldRoot,
            SyntaxNode? newRoot,
            IEnumerable<SyntaxNode>? oldRootChildren,
            IEnumerable<SyntaxNode>? newRootChildren,
            bool compareStatementSyntax)
            : base(oldRoot, newRoot, oldRootChildren, newRootChildren, compareStatementSyntax)
        {
        }

        protected override bool IsLambdaBodyStatementOrExpression(SyntaxNode node)
            => LambdaUtilities.IsLambdaBodyStatementOrExpression(node);

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

            GlobalStatement,

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
            ArrowExpressionClause,             // tied to parent

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
            UsingStatementWithExpression,
            UsingStatementWithDeclarations,
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

            YieldReturnStatement,              // tied to parent
            YieldBreakStatement,               // tied to parent
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
                case Label.ArrowExpressionClause:
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
                case Label.YieldReturnStatement:
                case Label.YieldBreakStatement:
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

        internal override int Classify(int kind, SyntaxNode? node, out bool isLeaf)
            => (int)Classify((SyntaxKind)kind, node, out isLeaf);

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

            // ************************************
            // Top and statement syntax
            // ************************************

            // These nodes can appear during top level and statement processing, so we put them in this first
            // switch for simplicity. Statement specific, and top level specific cases are handled below.
            switch (kind)
            {
                case SyntaxKind.CompilationUnit:
                    return Label.CompilationUnit;

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

                case SyntaxKind.ConstructorDeclaration:
                    // Root when matching constructor bodies.
                    return Label.ConstructorDeclaration;
            }

            if (_compareStatementSyntax)
            {
                return ClassifyStatementSyntax(kind, node, out isLeaf);
            }

            return ClassifyTopSyntax(kind, node, out isLeaf);
        }

        private static Label ClassifyStatementSyntax(SyntaxKind kind, SyntaxNode? node, out bool isLeaf)
        {
            isLeaf = false;

            // ************************************
            // Statement syntax
            // ************************************

            // These nodes could potentially be seen as top level syntax, but we only want them labelled
            // during statement syntax so they have to be kept separate.
            //
            // For example when top level sees something like this:
            //
            //      private int X => new Func(() => { return 1 })();
            //
            // It needs to go through the entire lambda to know if that property def has changed
            // but if we start labelling things, like ReturnStatement in the above example, then
            // it will stop. Given that a block bodied lambda can have any statements a user likes
            // the whole set has to be dealt with separately.
            switch (kind)
            {
                // Notes:
                // A descendant of a leaf node may be a labeled node that we don't want to visit if 
                // we are comparing its parent node (used for lambda bodies).
                // 
                // Expressions are ignored but they may contain nodes that should be matched by tree comparer.
                // (e.g. lambdas, declaration expressions). Descending to these nodes is handled in EnumerateChildren.

                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
                    // These declarations can come after global statements so we want to stop statement matching
                    // because no global statements can come after them
                    isLeaf = true;
                    return Label.Ignored;

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
                    // yield break is distinct from yield return as it does not suspend the state machine in a resumable state
                    return Label.YieldBreakStatement;

                case SyntaxKind.YieldReturnStatement:
                    return Label.YieldReturnStatement;

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
                    // We need to distinguish using statements with expression or single variable declaration from ones with multiple variable declarations. 
                    // The former generate a single try-finally block, the latter one for each variable. The finally blocks need to match since they
                    // affect state machine state matching. For simplicity we do not match single-declaration to expression, we just treat usings
                    // with declarations entirely separately from usings with expressions.
                    //
                    // The parent is not available only when comparing nodes for value equality.
                    // In that case it doesn't matter what label the node has as long as it has some.
                    return node is UsingStatementSyntax { Declaration: not null } ? Label.UsingStatementWithDeclarations : Label.UsingStatementWithExpression;

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

                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                case SyntaxKind.LocalFunctionStatement:
                    return Label.NestedFunction;

                case SyntaxKind.VariableDeclaration:
                    return Label.LocalVariableDeclaration;

                case SyntaxKind.VariableDeclarator:
                    return Label.LocalVariableDeclarator;

                case SyntaxKind.Block:
                    return Label.Block;
            }

            // If we got this far, its an unlabelled node. Since just about any node can
            // contain a lambda, isLeaf must be false for statement syntax.
            return Label.Ignored;
        }

        private static Label ClassifyTopSyntax(SyntaxKind kind, SyntaxNode? node, out bool isLeaf)
        {
            isLeaf = false;

            // ************************************
            // Top syntax
            // ************************************

            // More the most part these nodes will only appear in top syntax but its easier to
            // keep them separate so we can more easily discern was is shared, above.
            switch (kind)
            {
                case SyntaxKind.GlobalStatement:
                    isLeaf = true;
                    return Label.GlobalStatement;

                case SyntaxKind.ExternAliasDirective:
                    isLeaf = true;
                    return Label.ExternAliasDirective;

                case SyntaxKind.UsingDirective:
                    isLeaf = true;
                    return Label.UsingDirective;

                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.FileScopedNamespaceDeclaration:
                    return Label.NamespaceDeclaration;

                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
                    return Label.TypeDeclaration;

                case SyntaxKind.MethodDeclaration:
                    return Label.MethodDeclaration;

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

                case SyntaxKind.ArrowExpressionClause:
                    if (node?.Parent is (kind: SyntaxKind.PropertyDeclaration or SyntaxKind.IndexerDeclaration))
                        return Label.ArrowExpressionClause;

                    break;

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

                // Note: These last two do actually appear as statement syntax, but mean something
                // different and hence have a different label
                case SyntaxKind.VariableDeclaration:
                    return Label.FieldVariableDeclaration;

                case SyntaxKind.VariableDeclarator:
                    // For top syntax, a variable declarator is a leaf node
                    isLeaf = true;
                    return Label.FieldVariableDeclarator;

                case SyntaxKind.AttributeList:
                    // Only module/assembly attributes are labelled
                    if (node is not null && node.IsParentKind(SyntaxKind.CompilationUnit))
                    {
                        return Label.AttributeList;
                    }

                    break;

                case SyntaxKind.Attribute:
                    // Only module/assembly attributes are labelled
                    if (node is { Parent: { } parent } && parent.IsParentKind(SyntaxKind.CompilationUnit))
                    {
                        isLeaf = true;
                        return Label.Attribute;
                    }

                    break;
            }

            // If we got this far, its an unlabelled node. For top
            // syntax, we don't need to descend into any ignored nodes
            isLeaf = true;
            return Label.Ignored;
        }

        // internal for testing
        internal bool HasLabel(SyntaxKind kind)
            => Classify(kind, node: null, out _) != Label.Ignored;

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
                    {
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
                    }

                case SyntaxKind.UsingDirective:
                    {
                        var leftUsing = (UsingDirectiveSyntax)leftNode;
                        var rightUsing = (UsingDirectiveSyntax)rightNode;

                        // For now, just compute the distances of both the alias and name and combine their weights
                        // 50/50. We could consider weighting the alias more heavily.  i.e. if you have `using X = ...`
                        // and `using X = ...` it's more likely that this is the same alias, and just the name portion
                        // changed versus thinking that some other using became this alias.
                        distance =
                            ComputeDistance(leftUsing.Alias, rightUsing.Alias) +
                            ComputeDistance(leftUsing.NamespaceOrType, rightUsing.NamespaceOrType);

                        // Consider two usings that only differ by presence/absence of 'global' to be a near match.
                        if (leftUsing.GlobalKeyword.IsKind(SyntaxKind.None) != rightUsing.GlobalKeyword.IsKind(SyntaxKind.None))
                            distance += EpsilonDist;

                        // Consider two usings that only differ by presence/absence of 'unsafe' to be a near match.
                        if (leftUsing.UnsafeKeyword.IsKind(SyntaxKind.None) != rightUsing.UnsafeKeyword.IsKind(SyntaxKind.None))
                            distance += EpsilonDist;

                        return true;
                    }

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
                case SyntaxKind.GlobalStatement:
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
                if (child is LocalDeclarationStatementSyntax localDecl)
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
                    return ((UsingDirectiveSyntax)node).NamespaceOrType;

                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.FileScopedNamespaceDeclaration:
                    return ((BaseNamespaceDeclarationSyntax)node).Name;

                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
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

                case SyntaxKind.ArrowExpressionClause:
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

        public sealed override double GetDistance(SyntaxNode oldNode, SyntaxNode newNode)
        {
            Debug.Assert(GetLabel(oldNode) == GetLabel(newNode) && GetLabel(oldNode) != IgnoredNode);

            if (oldNode == newNode)
            {
                return ExactMatchDist;
            }

            if (TryComputeWeightedDistance(oldNode, newNode, out var weightedDistance))
            {
                if (weightedDistance == ExactMatchDist && !SyntaxFactory.AreEquivalent(oldNode, newNode))
                {
                    weightedDistance = EpsilonDist;
                }

                return weightedDistance;
            }

            return ComputeValueDistance(oldNode, newNode);
        }

        internal static double ComputeValueDistance(SyntaxNode? oldNode, SyntaxNode? newNode)
        {
            if (SyntaxFactory.AreEquivalent(oldNode, newNode))
            {
                return ExactMatchDist;
            }

            var distance = ComputeDistance(oldNode, newNode);

            // We don't want to return an exact match, because there
            // must be something different, since we got here 
            return (distance == ExactMatchDist) ? EpsilonDist : distance;
        }

        internal static double ComputeDistance(SyntaxNodeOrToken oldNodeOrToken, SyntaxNodeOrToken newNodeOrToken)
        {
            Debug.Assert(newNodeOrToken.IsToken == oldNodeOrToken.IsToken);

            double distance;
            if (oldNodeOrToken.IsToken)
            {
                var leftToken = oldNodeOrToken.AsToken();
                var rightToken = newNodeOrToken.AsToken();

                distance = ComputeDistance(leftToken, rightToken);
                Debug.Assert(!SyntaxFactory.AreEquivalent(leftToken, rightToken) || distance == ExactMatchDist);
            }
            else
            {
                var leftNode = oldNodeOrToken.AsNode();
                var rightNode = newNodeOrToken.AsNode();

                distance = ComputeDistance(leftNode, rightNode);
                Debug.Assert(!SyntaxFactory.AreEquivalent(leftNode, rightNode) || distance == ExactMatchDist);
            }

            return distance;
        }

        /// <summary>
        /// Enumerates tokens of all nodes in the list. Doesn't include separators.
        /// </summary>
        internal static IEnumerable<SyntaxToken> GetDescendantTokensIgnoringSeparators<TSyntaxNode>(SeparatedSyntaxList<TSyntaxNode> list)
            where TSyntaxNode : SyntaxNode
        {
            foreach (var node in list)
            {
                foreach (var token in node.DescendantTokens())
                {
                    yield return token;
                }
            }
        }

        /// <summary>
        /// Calculates the distance between two syntax nodes, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the nodes are. 
        /// </remarks>
        public static double ComputeDistance(SyntaxNode? oldNode, SyntaxNode? newNode)
        {
            if (oldNode == null || newNode == null)
            {
                return (oldNode == newNode) ? 0.0 : 1.0;
            }

            return ComputeDistance(oldNode.DescendantTokens(), newNode.DescendantTokens());
        }

        /// <summary>
        /// Calculates the distance between two syntax tokens, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the tokens are. 
        /// </remarks>
        public static double ComputeDistance(SyntaxToken oldToken, SyntaxToken newToken)
            => LongestCommonSubstring.ComputePrefixDistance(
                oldToken.Text, Math.Min(oldToken.Text.Length, LongestCommonSubsequence.MaxSequenceLengthForDistanceCalculation),
                newToken.Text, Math.Min(newToken.Text.Length, LongestCommonSubsequence.MaxSequenceLengthForDistanceCalculation));

        private static ImmutableArray<T> CreateArrayForDistanceCalculation<T>(IEnumerable<T>? enumerable)
            => enumerable is null ? ImmutableArray<T>.Empty : enumerable.Take(LongestCommonSubsequence.MaxSequenceLengthForDistanceCalculation).ToImmutableArray();

        /// <summary>
        /// Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        /// </remarks>
        public static double ComputeDistance(IEnumerable<SyntaxToken>? oldTokens, IEnumerable<SyntaxToken>? newTokens)
            => LcsTokens.Instance.ComputeDistance(CreateArrayForDistanceCalculation(oldTokens), CreateArrayForDistanceCalculation(newTokens));

        /// <summary>
        /// Calculates the distance between two sequences of syntax nodes, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        /// </remarks>
        public static double ComputeDistance(IEnumerable<SyntaxNode>? oldNodes, IEnumerable<SyntaxNode>? newNodes)
            => LcsNodes.Instance.ComputeDistance(CreateArrayForDistanceCalculation(oldNodes), CreateArrayForDistanceCalculation(newNodes));

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(IEnumerable<SyntaxNode>? oldNodes, IEnumerable<SyntaxNode>? newNodes)
            => LcsNodes.Instance.GetEdits(oldNodes.AsImmutableOrEmpty(), newNodes.AsImmutableOrEmpty());

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(ImmutableArray<SyntaxNode> oldNodes, ImmutableArray<SyntaxNode> newNodes)
            => LcsNodes.Instance.GetEdits(oldNodes.NullToEmpty(), newNodes.NullToEmpty());

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(IEnumerable<SyntaxToken>? oldTokens, IEnumerable<SyntaxToken>? newTokens)
            => LcsTokens.Instance.GetEdits(oldTokens.AsImmutableOrEmpty(), newTokens.AsImmutableOrEmpty());

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(ImmutableArray<SyntaxToken> oldTokens, ImmutableArray<SyntaxToken> newTokens)
            => LcsTokens.Instance.GetEdits(oldTokens.NullToEmpty(), newTokens.NullToEmpty());

        private sealed class LcsTokens : LongestCommonImmutableArraySubsequence<SyntaxToken>
        {
            internal static readonly LcsTokens Instance = new LcsTokens();

            protected override bool Equals(SyntaxToken oldElement, SyntaxToken newElement)
                => SyntaxFactory.AreEquivalent(oldElement, newElement);
        }

        private sealed class LcsNodes : LongestCommonImmutableArraySubsequence<SyntaxNode>
        {
            internal static readonly LcsNodes Instance = new LcsNodes();

            protected override bool Equals(SyntaxNode oldElement, SyntaxNode newElement)
                => SyntaxFactory.AreEquivalent(oldElement, newElement);
        }

        #endregion
    }
}
