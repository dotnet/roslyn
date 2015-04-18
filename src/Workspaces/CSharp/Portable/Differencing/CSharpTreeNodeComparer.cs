// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Differencing;

namespace Microsoft.CodeAnalysis.CSharp.Differencing
{
    internal class CSharpTreeNodeComparer : TreeNodeComparer
    {
        public readonly static CSharpTreeNodeComparer Instance = new CSharpTreeNodeComparer();

        private static readonly int Count = Enum.GetValues(typeof(SyntaxKind)).Cast<SyntaxKind>().Select(k => (int)k).Max() - (int)SyntaxKind.TildeToken + 2;

        private CSharpTreeNodeComparer() : base(new CSharpTreeNodeEquivalentChecker())
        {
        }

        protected internal override int LabelCount
        {
            get
            {
                return Count;
            }
        }

        protected internal override int GetLabel(TreeNode node)
        {
            if (node.Kind == (int)SyntaxKind.None)
            {
                return 0;
            }

            if (node.Kind == (int)SyntaxKind.List)
            {
                return 1;
            }

            return node.Kind - (int)SyntaxKind.TildeToken + 2;
        }

        private class CSharpTreeNodeEquivalentChecker : ITreeNodeComparer
        {
            private static readonly Func<TreeNode, TreeNode, bool> checker = (left, right) =>
            {
                if (left.IsNode && right.IsNode)
                {
                    return SyntaxFactory.AreEquivalent(left.AsNode(), right.AsNode());
                }

                if (left.IsToken && right.IsToken)
                {
                    return SyntaxFactory.AreEquivalent(left.AsToken(), right.AsToken());
                }

                if (left.IsTrivia && right.IsTrivia)
                {
                    var leftTrivia = left.AsTrivia();
                    var rightTrivia = right.AsTrivia();

                    if (leftTrivia.HasStructure && rightTrivia.HasStructure)
                    {
                        return SyntaxFactory.AreEquivalent(leftTrivia.GetStructure(), rightTrivia.GetStructure());
                    }

                    return leftTrivia.ToString() == rightTrivia.ToString();
                }

                return false;
            };

            public LcsTreeNodes LcsTreeNodes { get; } = new LcsTreeNodes(checker);

            public bool AreEquivalent(TreeNode left, TreeNode right)
            {
                return checker(left, right);
            }
        }
    }
}
