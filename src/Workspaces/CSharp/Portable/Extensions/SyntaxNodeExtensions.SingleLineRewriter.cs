// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal partial class SyntaxNodeExtensions
    {
        internal class SingleLineRewriter : CSharpSyntaxRewriter
        {
            private bool lastTokenEndedInWhitespace;

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (lastTokenEndedInWhitespace)
                {
                    token = token.WithLeadingTrivia(Enumerable.Empty<SyntaxTrivia>());
                }
                else if (token.LeadingTrivia.Count > 0)
                {
                    token = token.WithLeadingTrivia(SyntaxFactory.Space);
                }

                if (token.TrailingTrivia.Count > 0)
                {
                    token = token.WithTrailingTrivia(SyntaxFactory.Space);
                    lastTokenEndedInWhitespace = true;
                }
                else
                {
                    lastTokenEndedInWhitespace = false;
                }

                return token;
            }
        }
    }
}