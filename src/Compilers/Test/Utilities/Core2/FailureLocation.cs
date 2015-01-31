// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class FailureLocation : Location
    {
        public FailureLocation(TextSpan span, SyntaxTree tree)
        {
            _sourceSpan = span;
            _sourceTree = tree;
        }

        private static Location s_default;
        public static Location Default
        {
            get
            {
                if (s_default == null)
                {
                    s_default = new FailureLocation(new TextSpan(0, 0), null);
                }

                return s_default;
            }
        }

        public override FileLinePositionSpan GetLineSpan()
        {
            return _sourceTree.GetLineSpan(_sourceSpan);
        }

        public override LocationKind Kind
        {
            get
            {
                return LocationKind.SourceFile;
            }
        }

        private TextSpan _sourceSpan;
        public override TextSpan SourceSpan
        {
            get
            {
                return _sourceSpan;
            }
        }

        private readonly SyntaxTree _sourceTree;
        public override SyntaxTree SourceTree
        {
            get
            {
                return _sourceTree;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals((obj as FailureLocation));
        }

        public bool Equals(FailureLocation other)
        {
            return _sourceSpan == other._sourceSpan && ReferenceEquals(_sourceTree, other._sourceTree);
        }

        public override int GetHashCode()
        {
            return _sourceSpan.GetHashCode() ^ _sourceTree.GetHashCode();
        }
    }
}
