// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxTokenExtensions
    {
        public static bool TryParseGenericName(this SyntaxToken genericIdentifier, CancellationToken cancellationToken, out GenericNameSyntax genericName)
        {
            if (genericIdentifier.GetNextToken(includeSkipped: true).Kind() == SyntaxKind.LessThanToken)
            {
                var lastToken = genericIdentifier.FindLastTokenOfPartialGenericName();

                var syntaxTree = genericIdentifier.SyntaxTree;
                var name = SyntaxFactory.ParseName(syntaxTree.GetText(cancellationToken).ToString(TextSpan.FromBounds(genericIdentifier.SpanStart, lastToken.Span.End)));

                genericName = name as GenericNameSyntax;
                return genericName != null;
            }

            genericName = null;
            return false;
        }

        /// <summary>
        /// Lexically, find the last token that looks like it's part of this generic name.
        /// </summary>
        /// <param name="genericIdentifier">The "name" of the generic identifier, last token before
        /// the "&amp;"</param>
        /// <returns>The last token in the name</returns>
        /// <remarks>This is related to the code in <see cref="SyntaxTreeExtensions.IsInPartiallyWrittenGeneric(SyntaxTree, int, CancellationToken)"/></remarks>
        public static SyntaxToken FindLastTokenOfPartialGenericName(this SyntaxToken genericIdentifier)
        {
            Contract.ThrowIfFalse(genericIdentifier.Kind() == SyntaxKind.IdentifierToken);

            // advance to the "<" token
            var token = genericIdentifier.GetNextToken(includeSkipped: true);
            Contract.ThrowIfFalse(token.Kind() == SyntaxKind.LessThanToken);

            var stack = 0;

            do
            {
                // look forward one token
                {
                    var next = token.GetNextToken(includeSkipped: true);
                    if (next.Kind() == SyntaxKind.None)
                    {
                        return token;
                    }

                    token = next;
                }

                if (token.Kind() == SyntaxKind.GreaterThanToken)
                {
                    if (stack == 0)
                    {
                        return token;
                    }
                    else
                    {
                        stack--;
                        continue;
                    }
                }

                switch (token.Kind())
                {
                    case SyntaxKind.LessThanLessThanToken:
                        stack++;
                        goto case SyntaxKind.LessThanToken;

                    // fall through
                    case SyntaxKind.LessThanToken:
                        stack++;
                        break;

                    case SyntaxKind.AsteriskToken:      // for int*
                    case SyntaxKind.QuestionToken:      // for int?
                    case SyntaxKind.ColonToken:         // for global::  (so we don't dismiss help as you type the first :)
                    case SyntaxKind.ColonColonToken:    // for global::
                    case SyntaxKind.CloseBracketToken:
                    case SyntaxKind.OpenBracketToken:
                    case SyntaxKind.DotToken:
                    case SyntaxKind.IdentifierToken:
                    case SyntaxKind.CommaToken:
                        break;

                    // If we see a member declaration keyword, we know we've gone too far
                    case SyntaxKind.ClassKeyword:
                    case SyntaxKind.StructKeyword:
                    case SyntaxKind.InterfaceKeyword:
                    case SyntaxKind.DelegateKeyword:
                    case SyntaxKind.EnumKeyword:
                    case SyntaxKind.PrivateKeyword:
                    case SyntaxKind.PublicKeyword:
                    case SyntaxKind.InternalKeyword:
                    case SyntaxKind.ProtectedKeyword:
                    case SyntaxKind.VoidKeyword:
                        return token.GetPreviousToken(includeSkipped: true);

                    default:
                        // user might have typed "in" on the way to typing "int"
                        // don't want to disregard this genericname because of that
                        if (SyntaxFacts.IsKeywordKind(token.Kind()))
                        {
                            break;
                        }

                        // anything else and we're sunk. Go back to the token before.
                        return token.GetPreviousToken(includeSkipped: true);
                }
            }
            while (true);
        }
    }
}
