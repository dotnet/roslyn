// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal class SyntaxLastTokenReplacer : CSharpSyntaxRewriter
    {
        private readonly SyntaxToken oldToken;
        private readonly SyntaxToken newToken;
        private int count = 1;
        private bool found = false;

        private SyntaxLastTokenReplacer(SyntaxToken oldToken, SyntaxToken newToken)
        {
            this.oldToken = oldToken;
            this.newToken = newToken;
        }

        internal static TRoot Replace<TRoot>(TRoot root, SyntaxToken newToken)
            where TRoot : CSharpSyntaxNode
        {
            var oldToken = root.GetLastToken();
            var replacer = new SyntaxLastTokenReplacer(oldToken, newToken);
            var newRoot = (TRoot)replacer.Visit(root);
            Debug.Assert(replacer.found);
            return newRoot;
        }

        private static int CountNonNullSlots(CSharpSyntaxNode node)
        {
            return node.ChildNodesAndTokens().Count;
        }

        public override CSharpSyntaxNode Visit(CSharpSyntaxNode node)
        {
            if (node != null && !found)
            {
                count--;
                if (count == 0)
                {
                    var token = node as SyntaxToken;
                    if (token != null)
                    {
                        Debug.Assert(token == oldToken);
                        found = true;
                        return newToken;
                    }

                    count += CountNonNullSlots(node);
                    return base.Visit(node);
                }
            }

            return node;
        }
    }
}