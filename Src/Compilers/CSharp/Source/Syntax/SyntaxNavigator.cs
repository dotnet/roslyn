// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxNavigator : AbstractSyntaxNavigator
    {
        public static readonly AbstractSyntaxNavigator Instance = new SyntaxNavigator();

        private static readonly Func<SyntaxTrivia, bool> CommonSyntaxTriviaSkipped =
            t => t.RawKind == (int)SyntaxKind.SkippedTokensTrivia;

        [Flags]
        private enum SyntaxKinds
        {
            DocComments = 1,
            Directives = 2,
            SkippedTokens = 4,
        }

        private static readonly Func<SyntaxTrivia, bool>[] StepIntoFunctions = new Func<SyntaxTrivia, bool>[]
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
            var index = (skipped     ? SyntaxKinds.SkippedTokens : 0) |
                        (directives  ? SyntaxKinds.Directives    : 0) |
                        (docComments ? SyntaxKinds.DocComments   : 0);
            return StepIntoFunctions[(int)index];
        }

        public static Func<SyntaxTrivia, bool> ToCommon(Func<SyntaxTrivia, bool> func)
        {
            if (ReferenceEquals(func, SyntaxTriviaFunctions.Any))
            {
                return SyntaxTrivia.Any;
            }

            if (ReferenceEquals(func, SyntaxTriviaFunctions.Skipped))
            {
                return CommonSyntaxTriviaSkipped;
            }

            if (ReferenceEquals(func, null))
            {
                return null;
            }

            return t => func((SyntaxTrivia)t);
        }

        public static Func<SyntaxToken, bool> ToCommon(Func<SyntaxToken, bool> func)
        {
            if (ReferenceEquals(func, SyntaxToken.Any))
            {
                return SyntaxToken.Any;
            }

            if (ReferenceEquals(func, SyntaxToken.NonZeroWidth))
            {
                return SyntaxToken.NonZeroWidth;
            }

            if (ReferenceEquals(func, null))
            {
                return null;
            }

            return t => func((SyntaxToken)t);
        }
    }
}