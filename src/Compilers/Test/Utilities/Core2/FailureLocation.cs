// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class FailureLocation : Location
    {
        public FailureLocation(TextSpan span, SyntaxTree tree)
        {
            m_SourceSpan = span;
            m_SourceTree = tree;
        }

        private static Location m_Default;
        public static Location Default
        {
            get
            {
                if (m_Default == null)
                {
                    m_Default = new FailureLocation(new TextSpan(0, 0), null);
                }

                return m_Default;
            }
        }

        public override FileLinePositionSpan GetLineSpan()
        {
            return m_SourceTree.GetLineSpan(m_SourceSpan);
        }

        public override LocationKind Kind
        {
            get
            {
                return LocationKind.SourceFile;
            }
        }

        private TextSpan m_SourceSpan;
        public override TextSpan SourceSpan
        {
            get
            {
                return m_SourceSpan;
            }
        }

        private readonly SyntaxTree m_SourceTree;
        public override SyntaxTree SourceTree
        {
            get
            {
                return m_SourceTree;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals((obj as FailureLocation));
        }

        public bool Equals(FailureLocation other)
        {
            return m_SourceSpan == other.m_SourceSpan && ReferenceEquals(m_SourceTree, other.m_SourceTree);
        }

        public override int GetHashCode()
        {
            return m_SourceSpan.GetHashCode() ^ m_SourceTree.GetHashCode();
        }
    }
}