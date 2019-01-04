// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Text.RegularExpressions;
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
            private static readonly Regex s_newlinePattern = new Regex(@"[\r\n]+");

            private readonly bool _useElasticTrivia;
            private bool _lastTokenEndedInWhitespace;

            public SingleLineRewriter(bool useElasticTrivia)
            {
                this._useElasticTrivia = useElasticTrivia;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_lastTokenEndedInWhitespace)
                {
                    token = token.WithLeadingTrivia(Enumerable.Empty<SyntaxTrivia>());
                }
                else if (token.LeadingTrivia.Count > 0)
                {
                    if (_useElasticTrivia)
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
                    if (_useElasticTrivia)
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

                if (token.Kind() == SyntaxKind.StringLiteralToken ||
                    token.Kind() == SyntaxKind.InterpolatedStringTextToken)
                {
                    if (s_newlinePattern.IsMatch(token.Text))
                    {
                        var newText = s_newlinePattern.Replace(token.Text, " ");
                        token = SyntaxFactory.Token(
                            token.LeadingTrivia,
                            token.Kind(),
                            newText, newText,
                            token.TrailingTrivia);
                    }
                }

                return token;
            }
        }
    }
}
