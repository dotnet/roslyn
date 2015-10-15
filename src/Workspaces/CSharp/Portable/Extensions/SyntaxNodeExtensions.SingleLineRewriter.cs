// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal partial class SyntaxNodeExtensions
    {
        internal class SingleLineRewriter : CSharpSyntaxRewriter
        {
            private bool _lastTokenEndedInWhitespace;

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_lastTokenEndedInWhitespace)
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
