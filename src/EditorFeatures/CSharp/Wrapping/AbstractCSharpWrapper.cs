//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.CodeActions;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.Editor.Wrapping;

//namespace Microsoft.CodeAnalysis.Editor.CSharp.Wrapping
//{
//    internal abstract class AbstractCSharpWrapper : AbstractWrapper
//    {
//        protected override SyntaxNode Rewrite(SyntaxNode root, ImmutableArray<Edit> edits)
//        {
//            var rewriter = new Rewriter(edits);
//            return rewriter.Visit(root);
//        }

//        private class Rewriter : CSharpSyntaxRewriter
//        {
//            private readonly Dictionary<SyntaxNodeOrToken, string> _leftNodeOrTokenToNewTrivia =
//                new Dictionary<SyntaxNodeOrToken, string>();
//            private readonly HashSet<SyntaxNodeOrToken> _rightNodeToStripLeadingTriviaFrom =
//                new HashSet<SyntaxNodeOrToken>();

//            public Rewriter(ImmutableArray<Edit> edits)
//            {
//                foreach(var edit in edits)
//                {
//                    _leftNodeOrTokenToNewTrivia.Add(edit.Left, edit.NewTrivia);
//                    _rightNodeToStripLeadingTriviaFrom.Add(edit.Right);
//                }
//            }

//            public override SyntaxToken VisitToken(SyntaxToken token)
//            {
//                if ()
//            }
//        }
//    }
//}
