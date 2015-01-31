// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal sealed class SyntaxNavigator : AbstractSyntaxNavigator
    {
        public static readonly AbstractSyntaxNavigator Instance = new SyntaxNavigator();

        [Flags]
        private enum SyntaxKinds
        {
            DocComments = 1,
            Directives = 2,
            SkippedTokens = 4,
        }

        private static readonly Func<SyntaxTrivia, bool>[] s_stepIntoFunctions = new Func<SyntaxTrivia, bool>[]
        {
            /* 000 */ null,
            /* 001 */ t =>                                                                   SyntaxFacts.IsDocumentationCommentTrivia((SyntaxKind)t.RawKind),
            /* 010 */ t =>                                                  t.IsDirective                                                                ,
            /* 011 */ t =>                                                  t.IsDirective || SyntaxFacts.IsDocumentationCommentTrivia((SyntaxKind)t.RawKind),
            /* 100 */ t => t.RawKind == (int)SyntaxKind.SkippedTokensTrivia                                                                                 ,
            /* 101 */ t => t.RawKind == (int)SyntaxKind.SkippedTokensTrivia                  || SyntaxFacts.IsDocumentationCommentTrivia((SyntaxKind)t.RawKind),
            /* 110 */ t => t.RawKind == (int)SyntaxKind.SkippedTokensTrivia || t.IsDirective                                                                ,
            /* 111 */ t => t.RawKind == (int)SyntaxKind.SkippedTokensTrivia || t.IsDirective || SyntaxFacts.IsDocumentationCommentTrivia((SyntaxKind)t.RawKind),
        };

        protected override Func<SyntaxTrivia, bool> GetStepIntoFunction(bool skipped, bool directives, bool docComments)
        {
            var index = (skipped ? SyntaxKinds.SkippedTokens : 0) |
                        (directives ? SyntaxKinds.Directives : 0) |
                        (docComments ? SyntaxKinds.DocComments : 0);
            return s_stepIntoFunctions[(int)index];
        }
    }
}
