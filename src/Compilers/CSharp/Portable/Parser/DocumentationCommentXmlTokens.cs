// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Syntax.InternalSyntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    /// <summary>
    /// Set of well-known SyntaxTokens commonly found within XML doc comments.
    /// </summary>
    internal static class DocumentationCommentXmlTokens
    {
        // Well-known tags that typically have no leading trivia
        private static readonly SyntaxToken s_seeToken = Identifier(DocumentationCommentXmlNames.SeeElementName);
        private static readonly SyntaxToken s_codeToken = Identifier(DocumentationCommentXmlNames.CodeElementName);
        private static readonly SyntaxToken s_listToken = Identifier(DocumentationCommentXmlNames.ListElementName);
        private static readonly SyntaxToken s_paramToken = Identifier(DocumentationCommentXmlNames.ParameterElementName);
        private static readonly SyntaxToken s_valueToken = Identifier(DocumentationCommentXmlNames.ValueElementName);
        private static readonly SyntaxToken s_exampleToken = Identifier(DocumentationCommentXmlNames.ExampleElementName);
        private static readonly SyntaxToken s_includeToken = Identifier(DocumentationCommentXmlNames.IncludeElementName);
        private static readonly SyntaxToken s_remarksToken = Identifier(DocumentationCommentXmlNames.RemarksElementName);
        private static readonly SyntaxToken s_seealsoToken = Identifier(DocumentationCommentXmlNames.SeeAlsoElementName);
        private static readonly SyntaxToken s_summaryToken = Identifier(DocumentationCommentXmlNames.SummaryElementName);
        private static readonly SyntaxToken s_exceptionToken = Identifier(DocumentationCommentXmlNames.ExceptionElementName);
        private static readonly SyntaxToken s_typeparamToken = Identifier(DocumentationCommentXmlNames.TypeParameterElementName);
        private static readonly SyntaxToken s_permissionToken = Identifier(DocumentationCommentXmlNames.PermissionElementName);
        private static readonly SyntaxToken s_typeparamrefToken = Identifier(DocumentationCommentXmlNames.TypeParameterReferenceElementName);

        // Well-known tags that typically have a single space in leading trivia
        private static readonly SyntaxToken s_crefToken = IdentifierWithLeadingSpace(DocumentationCommentXmlNames.CrefAttributeName);
        private static readonly SyntaxToken s_fileToken = IdentifierWithLeadingSpace(DocumentationCommentXmlNames.FileAttributeName);
        private static readonly SyntaxToken s_nameToken = IdentifierWithLeadingSpace(DocumentationCommentXmlNames.NameAttributeName);
        private static readonly SyntaxToken s_pathToken = IdentifierWithLeadingSpace(DocumentationCommentXmlNames.PathAttributeName);
        private static readonly SyntaxToken s_typeToken = IdentifierWithLeadingSpace(DocumentationCommentXmlNames.TypeAttributeName);

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
        public static SyntaxToken? LookupToken(string text, SyntaxListBuilder? leading)
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

        private static SyntaxToken? LookupXmlElementTag(string text)
        {
            switch (text.Length)
            {
                case 3:
                    if (text == DocumentationCommentXmlNames.SeeElementName)
                    {
                        return s_seeToken;
                    }
                    break;

                case 4:
                    switch (text)
                    {
                        case DocumentationCommentXmlNames.CodeElementName:
                            return s_codeToken;

                        case DocumentationCommentXmlNames.ListElementName:
                            return s_listToken;
                    }
                    break;

                case 5:
                    switch (text)
                    {
                        case DocumentationCommentXmlNames.ParameterElementName:
                            return s_paramToken;

                        case DocumentationCommentXmlNames.ValueElementName:
                            return s_valueToken;
                    }
                    break;

                case 7:
                    switch (text)
                    {
                        case DocumentationCommentXmlNames.ExampleElementName:
                            return s_exampleToken;

                        case DocumentationCommentXmlNames.IncludeElementName:
                            return s_includeToken;

                        case DocumentationCommentXmlNames.RemarksElementName:
                            return s_remarksToken;

                        case DocumentationCommentXmlNames.SeeAlsoElementName:
                            return s_seealsoToken;

                        case DocumentationCommentXmlNames.SummaryElementName:
                            return s_summaryToken;
                    }
                    break;

                case 9:
                    switch (text)
                    {
                        case DocumentationCommentXmlNames.ExceptionElementName:
                            return s_exceptionToken;

                        case DocumentationCommentXmlNames.TypeParameterElementName:
                            return s_typeparamToken;
                    }
                    break;

                case 10:
                    if (text == DocumentationCommentXmlNames.PermissionElementName)
                    {
                        return s_permissionToken;
                    }
                    break;

                case 12:
                    if (text == DocumentationCommentXmlNames.TypeParameterElementName)
                    {
                        return s_typeparamrefToken;
                    }
                    break;
            }

            return null;
        }

        private static SyntaxToken? LookupXmlAttribute(string text)
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
                    return s_crefToken;

                case DocumentationCommentXmlNames.FileAttributeName:
                    return s_fileToken;

                case DocumentationCommentXmlNames.NameAttributeName:
                    return s_nameToken;

                case DocumentationCommentXmlNames.PathAttributeName:
                    return s_pathToken;

                case DocumentationCommentXmlNames.TypeAttributeName:
                    return s_typeToken;
            }

            return null;
        }
    }
}
