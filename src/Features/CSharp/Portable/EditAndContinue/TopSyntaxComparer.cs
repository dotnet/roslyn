// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue
{
    internal sealed class TopSyntaxComparer : SyntaxComparer
    {
        internal static readonly TopSyntaxComparer Instance = new TopSyntaxComparer();

        private TopSyntaxComparer()
        {
        }

        #region Tree Traversal

        protected internal override bool TryGetParent(SyntaxNode node, out SyntaxNode parent)
        {
            var parentNode = node.Parent;
            parent = parentNode;
            return parentNode != null;
        }

        protected internal override IEnumerable<SyntaxNode> GetChildren(SyntaxNode node)
        {
            Debug.Assert(GetLabel(node) != IgnoredNode);
            return HasChildren(node) ? EnumerateChildren(node) : null;
        }

        private IEnumerable<SyntaxNode> EnumerateChildren(SyntaxNode node)
        {
            foreach (var child in node.ChildNodesAndTokens())
            {
                var childNode = child.AsNode();
                if (childNode != null && GetLabel(childNode) != IgnoredNode)
                {
                    yield return childNode;
                }
            }
        }

        protected internal override IEnumerable<SyntaxNode> GetDescendants(SyntaxNode node)
        {
            foreach (var descendant in node.DescendantNodesAndTokens(
                descendIntoChildren: HasChildren,
                descendIntoTrivia: false))
            {
                var descendantNode = descendant.AsNode();
                if (descendantNode != null && GetLabel(descendantNode) != IgnoredNode)
                {
                    yield return descendantNode;
                }
            }
        }

        private static bool HasChildren(SyntaxNode node)
        {
            // Leaves are labeled statements that don't have a labeled child.
            // We also return true for non-labeled statements.
            bool isLeaf;
            Label label = Classify(node.Kind(), out isLeaf, ignoreVariableDeclarations: false);

            // ignored should always be reported as leaves
            Debug.Assert(label != Label.Ignored || isLeaf);

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

                default:
                    return 0;
            }
        }

        // internal for testing
        internal static Label Classify(SyntaxKind kind, out bool isLeaf, bool ignoreVariableDeclarations)
        {
            switch (kind)
            {
                case SyntaxKind.CompilationUnit:
                    isLeaf = false;
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
                    isLeaf = false;
                    return Label.NamespaceDeclaration;

                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                    isLeaf = false;
                    return Label.TypeDeclaration;

                case SyntaxKind.EnumDeclaration:
                    isLeaf = false;
                    return Label.EnumDeclaration;

                case SyntaxKind.DelegateDeclaration:
                    isLeaf = false;
                    return Label.DelegateDeclaration;

                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                    isLeaf = false;
                    return Label.FieldDeclaration;

                case SyntaxKind.VariableDeclaration:
                    isLeaf = ignoreVariableDeclarations;
                    return ignoreVariableDeclarations ? Label.Ignored : Label.FieldVariableDeclaration;

                case SyntaxKind.VariableDeclarator:
                    isLeaf = true;
                    return ignoreVariableDeclarations ? Label.Ignored : Label.FieldVariableDeclarator;

                case SyntaxKind.MethodDeclaration:
                    isLeaf = false;
                    return Label.MethodDeclaration;

                case SyntaxKind.ConversionOperatorDeclaration:
                    isLeaf = false;
                    return Label.ConversionOperatorDeclaration;

                case SyntaxKind.OperatorDeclaration:
                    isLeaf = false;
                    return Label.OperatorDeclaration;

                case SyntaxKind.ConstructorDeclaration:
                    isLeaf = false;
                    return Label.ConstructorDeclaration;

                case SyntaxKind.DestructorDeclaration:
                    isLeaf = true;
                    return Label.DestructorDeclaration;

                case SyntaxKind.PropertyDeclaration:
                    isLeaf = false;
                    return Label.PropertyDeclaration;

                case SyntaxKind.IndexerDeclaration:
                    isLeaf = false;
                    return Label.IndexerDeclaration;

                case SyntaxKind.EventDeclaration:
                    isLeaf = false;
                    return Label.EventDeclaration;

                case SyntaxKind.EnumMemberDeclaration:
                    isLeaf = false; // attribute may be applied
                    return Label.EnumMemberDeclaration;

                case SyntaxKind.AccessorList:
                    isLeaf = false;
                    return Label.AccessorList;

                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    isLeaf = true;
                    return Label.AccessorDeclaration;

                case SyntaxKind.TypeParameterList:
                    isLeaf = false;
                    return Label.TypeParameterList;

                case SyntaxKind.TypeParameterConstraintClause:
                    isLeaf = false;
                    return Label.TypeParameterConstraintClause;

                case SyntaxKind.TypeParameter:
                    isLeaf = false; // children: attributes
                    return Label.TypeParameter;

                case SyntaxKind.ParameterList:
                    isLeaf = false;
                    return Label.ParameterList;

                case SyntaxKind.BracketedParameterList:
                    isLeaf = false;
                    return Label.BracketedParameterList;

                case SyntaxKind.Parameter:
                    // We ignore anonymous methods and lambdas, 
                    // we only care about parameters of member declarations.
                    isLeaf = false; // children: attributes
                    return Label.Parameter;

                case SyntaxKind.AttributeList:
                    isLeaf = false;
                    return Label.AttributeList;

                case SyntaxKind.Attribute:
                    isLeaf = true;
                    return Label.Attribute;

                default:
                    isLeaf = true;
                    return Label.Ignored;
            }
        }

        protected internal override int GetLabel(SyntaxNode node)
        {
            return (int)GetLabel(node.Kind());
        }

        internal static Label GetLabel(SyntaxKind kind)
        {
            bool isLeaf;
            return Classify(kind, out isLeaf, ignoreVariableDeclarations: false);
        }

        // internal for testing
        internal static bool HasLabel(SyntaxKind kind, bool ignoreVariableDeclarations)
        {
            bool isLeaf;
            return Classify(kind, out isLeaf, ignoreVariableDeclarations) != Label.Ignored;
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

        public override bool ValuesEqual(SyntaxNode left, SyntaxNode right)
        {
            Func<SyntaxKind, bool> ignoreChildFunction;
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
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    // When comparing method bodies we need to NOT ignore VariableDeclaration and VariableDeclarator children,
                    // but when comparing field definitions we should ignore VariableDeclarations children.
                    ignoreChildFunction = childKind => HasLabel(childKind, ignoreVariableDeclarations: true);
                    break;

                default:
                    if (HasChildren(left))
                    {
                        ignoreChildFunction = childKind => HasLabel(childKind, ignoreVariableDeclarations: false);
                    }
                    else
                    {
                        ignoreChildFunction = null;
                    }

                    break;
            }

            return SyntaxFactory.AreEquivalent(left, right, ignoreChildFunction);
        }

        protected override bool TryComputeWeightedDistance(SyntaxNode leftNode, SyntaxNode rightNode, out double distance)
        {
            SyntaxNodeOrToken? leftName = TryGetName(leftNode);
            SyntaxNodeOrToken? rightName = TryGetName(rightNode);
            Debug.Assert(rightName.HasValue == leftName.HasValue);

            if (leftName.HasValue)
            {
                distance = ComputeDistance(leftName.Value, rightName.Value);
                return true;
            }
            else
            {
                distance = 0;
                return false;
            }
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
