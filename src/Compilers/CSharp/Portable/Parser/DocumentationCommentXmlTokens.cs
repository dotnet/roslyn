// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    /// <summary>
    /// Set of well-known SyntaxTokens commonly found within XML doc comments.
    /// </summary>
    internal static class DocumentationCommentXmlTokens
    {
        // Well-known tags that typically have no leading trivia
        private static readonly SyntaxToken seeToken = Identifier(DocumentationCommentXmlNames.SeeElementName);
        private static readonly SyntaxToken codeToken = Identifier(DocumentationCommentXmlNames.CodeElementName);
        private static readonly SyntaxToken listToken = Identifier(DocumentationCommentXmlNames.ListElementName);
        private static readonly SyntaxToken paramToken = Identifier(DocumentationCommentXmlNames.ParameterElementName);
        private static readonly SyntaxToken valueToken = Identifier(DocumentationCommentXmlNames.ValueElementName);
        private static readonly SyntaxToken exampleToken = Identifier(DocumentationCommentXmlNames.ExampleElementName);
        private static readonly SyntaxToken includeToken = Identifier(DocumentationCommentXmlNames.IncludeElementName);
        private static readonly SyntaxToken remarksToken = Identifier(DocumentationCommentXmlNames.RemarksElementName);
        private static readonly SyntaxToken seealsoToken = Identifier(DocumentationCommentXmlNames.SeeAlsoElementName);
        private static readonly SyntaxToken summaryToken = Identifier(DocumentationCommentXmlNames.SummaryElementName);
        private static readonly SyntaxToken exceptionToken = Identifier(DocumentationCommentXmlNames.ExceptionElementName);
        private static readonly SyntaxToken typeparamToken = Identifier(DocumentationCommentXmlNames.TypeParameterElementName);
        private static readonly SyntaxToken permissionToken = Identifier(DocumentationCommentXmlNames.PermissionElementName);
        private static readonly SyntaxToken typeparamrefToken = Identifier(DocumentationCommentXmlNames.TypeParameterReferenceElementName);

        // Well-known tags that typically have a single space in leading trivia
        private static readonly SyntaxToken crefToken = IdentifierWithLeadingSpace(DocumentationCommentXmlNames.CrefAttributeName);
        private static readonly SyntaxToken fileToken = IdentifierWithLeadingSpace(DocumentationCommentXmlNames.FileAttributeName);
        private static readonly SyntaxToken nameToken = IdentifierWithLeadingSpace(DocumentationCommentXmlNames.NameAttributeName);
        private static readonly SyntaxToken pathToken = IdentifierWithLeadingSpace(DocumentationCommentXmlNames.PathAttributeName);
        private static readonly SyntaxToken typeToken = IdentifierWithLeadingSpace(DocumentationCommentXmlNames.TypeAttributeName);

        private static SyntaxToken Identifier(string text)
        {
            return SyntaxFactory.Identifier(SyntaxKind.None, null, text, text, trailing: null);
        }

        private static SyntaxToken IdentifierWithLeadingSpace(string text)
        {
            return SyntaxFactory.Identifier(SyntaxKind.None, SyntaxFactory.Space, text, text, trailing: null);
        }

        private static bool IsSingleSpaceTrivia(SyntaxListBuilder syntax)
        {
            return syntax.Count == 1 && SyntaxFactory.Space.IsEquivalentTo(syntax[0]);
        }

        /// <summary>
        /// Look up a well known SyntaxToken for a given XML element tag or attribute.
        /// This is a performance optimization to avoid creating duplicate tokens for the same content.
        /// </summary>
        /// <param name="text">The text of the tag or attribute.</param>
        /// <param name="leading">The leading trivia of the token.</param>
        /// <returns>The SyntaxToken representing the well-known tag or attribute or null if it's not well-known.</returns>
        public static SyntaxToken LookupToken(string text, SyntaxListBuilder leading)
        {
            if (leading == null)
            {
                return LookupXmlElementTag(text);
            }

            if (IsSingleSpaceTrivia(leading))
            {
                return LookupXmlAttribute(text);
            }

            return null;
        }

        private static SyntaxToken LookupXmlElementTag(string text)
        {
            switch (text.Length)
            {
                case 3:
                    if (text == DocumentationCommentXmlNames.SeeElementName)
                    {
                        return seeToken;
                    }
                    break;

                case 4:
                    switch (text)
                    {
                        case DocumentationCommentXmlNames.CodeElementName:
                            return codeToken;

                        case DocumentationCommentXmlNames.ListElementName:
                            return listToken;
                    }
                    break;

                case 5:
                    switch (text)
                    {
                        case DocumentationCommentXmlNames.ParameterElementName:
                            return paramToken;

                        case DocumentationCommentXmlNames.ValueElementName:
                            return valueToken;
                    }
                    break;

                case 7:
                    switch (text)
                    {
                        case DocumentationCommentXmlNames.ExampleElementName:
                            return exampleToken;

                        case DocumentationCommentXmlNames.IncludeElementName:
                            return includeToken;

                        case DocumentationCommentXmlNames.RemarksElementName:
                            return remarksToken;

                        case DocumentationCommentXmlNames.SeeAlsoElementName:
                            return seealsoToken;

                        case DocumentationCommentXmlNames.SummaryElementName:
                            return summaryToken;
                    }
                    break;

                case 9:
                    switch (text)
                    {
                        case DocumentationCommentXmlNames.ExceptionElementName:
                            return exceptionToken;

                        case DocumentationCommentXmlNames.TypeParameterElementName:
                            return typeparamToken;
                    }
                    break;

                case 10:
                    if (text == DocumentationCommentXmlNames.PermissionElementName)
                    {
                        return permissionToken;
                    }
                    break;

                case 12:
                    if (text == DocumentationCommentXmlNames.TypeParameterElementName)
                    {
                        return typeparamrefToken;
                    }
                    break;
            }

            return null;
        }

        private static SyntaxToken LookupXmlAttribute(string text)
        {
            // It happens that all tokens have text of length 4
            if (text.Length != 4)
            {
                return null;
            }

            // The compiler may choose to turn this switch statement into a dictionary lookup
            switch (text)
            {
                case DocumentationCommentXmlNames.CrefAttributeName:
                    return crefToken;

                case DocumentationCommentXmlNames.FileAttributeName:
                    return fileToken;

                case DocumentationCommentXmlNames.NameAttributeName:
                    return nameToken;

                case DocumentationCommentXmlNames.PathAttributeName:
                    return pathToken;

                case DocumentationCommentXmlNames.TypeAttributeName:
                    return typeToken;
            }

            return null;
        }
    }
}
