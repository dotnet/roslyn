// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private bool useElasticTrivia;
            private bool _lastTokenEndedInWhitespace;

            public SingleLineRewriter(bool useElasticTrivia)
            {
                this.useElasticTrivia = useElasticTrivia;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_lastTokenEndedInWhitespace)
                {
                    token = token.WithLeadingTrivia(Enumerable.Empty<SyntaxTrivia>());
                }
                else if (token.LeadingTrivia.Count > 0)
                {
                    if (useElasticTrivia)
                    {
                        token = token.WithLeadingTrivia(SyntaxFactory.ElasticSpace);
                    }
                    else
                    {
                        token = token.WithLeadingTrivia(SyntaxFactory.Space);
                    }
                }

                if (token.TrailingTrivia.Count > 0)
                {
                    if (useElasticTrivia)
                    {
                        token = token.WithTrailingTrivia(SyntaxFactory.ElasticSpace);
                    }
                    else
                    {
                        token = token.WithTrailingTrivia(SyntaxFactory.Space);
                    }
                    _lastTokenEndedInWhitespace = true;
                }
                else
                {
                    _lastTokenEndedInWhitespace = false;
                }

                return token;
            }
        }
    }
}
