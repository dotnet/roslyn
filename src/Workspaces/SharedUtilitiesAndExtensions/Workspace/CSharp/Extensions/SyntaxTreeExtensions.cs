﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxTreeExtensions
    {
        public static bool IsPrimaryFunctionExpressionContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
        {
            return
                syntaxTree.IsTypeOfExpressionContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsDefaultExpressionContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsSizeOfExpressionContext(position, tokenOnLeftOfPosition);
        }

        public static bool IsInNonUserCode(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return
                syntaxTree.IsEntirelyWithinNonUserCodeComment(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinConflictMarker(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinStringOrCharLiteral(position, cancellationToken) ||
                syntaxTree.IsInInactiveRegion(position, cancellationToken);
        }

        public static bool IsInInactiveRegion(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(syntaxTree);

            // cases:
            // $ is EOF

            // #if false
            //    |

            // #if false
            //    |$

            // #if false
            // |

            // #if false
            // |$

            if (syntaxTree.IsPreProcessorKeywordContext(position, cancellationToken))
            {
                return false;
            }

            // The latter two are the hard cases we don't actually have an 
            // DisabledTextTrivia yet. 
            var trivia = syntaxTree.GetRoot(cancellationToken).FindTrivia(position, findInsideTrivia: false);
            if (trivia.Kind() == SyntaxKind.DisabledTextTrivia)
            {
                return true;
            }

            var token = syntaxTree.FindTokenOrEndToken(position, cancellationToken);
            if (token.Kind() == SyntaxKind.EndOfFileToken)
            {
                var triviaList = token.LeadingTrivia;
                foreach (var triviaTok in triviaList.Reverse())
                {
                    if (triviaTok.Span.Contains(position))
                    {
                        return false;
                    }

                    if (triviaTok.Span.End < position)
                    {
                        if (!triviaTok.HasStructure)
                        {
                            return false;
                        }

                        var structure = triviaTok.GetStructure();
                        if (structure is BranchingDirectiveTriviaSyntax branch)
                        {
                            return !branch.IsActive || !branch.BranchTaken;
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsInPartiallyWrittenGeneric(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return syntaxTree.IsInPartiallyWrittenGeneric(position, cancellationToken, out _, out _);
        }

        public static bool IsInPartiallyWrittenGeneric(
            this SyntaxTree syntaxTree,
            int position,
            CancellationToken cancellationToken,
            out SyntaxToken genericIdentifier)
        {
            return syntaxTree.IsInPartiallyWrittenGeneric(position, cancellationToken, out genericIdentifier, out _);
        }

        public static bool IsInPartiallyWrittenGeneric(
            this SyntaxTree syntaxTree,
            int position,
            CancellationToken cancellationToken,
            out SyntaxToken genericIdentifier,
            out SyntaxToken lessThanToken)
        {
            genericIdentifier = default;
            lessThanToken = default;
            var index = 0;

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            if (token.Kind() == SyntaxKind.None)
            {
                return false;
            }

            // check whether we are under type or member decl
            if (token.GetAncestor<TypeParameterListSyntax>() != null)
            {
                return false;
            }

            var stack = 0;
            while (true)
            {
                switch (token.Kind())
                {
                    case SyntaxKind.LessThanToken:
                        if (stack == 0)
                        {
                            // got here so we read successfully up to a < now we have to read the
                            // name before that and we're done!
                            lessThanToken = token;
                            token = token.GetPreviousToken(includeSkipped: true);
                            if (token.Kind() == SyntaxKind.None)
                            {
                                return false;
                            }

                            // ok
                            // so we've read something like:
                            // ~~~~~~~~~<a,b,...
                            // but we need to know the simple name that precedes the <
                            // it could be
                            // ~~~~~~goo<a,b,...
                            if (token.Kind() == SyntaxKind.IdentifierToken)
                            {
                                // okay now check whether it is actually partially written
                                if (IsFullyWrittenGeneric(token, lessThanToken))
                                {
                                    return false;
                                }

                                genericIdentifier = token;
                                return true;
                            }

                            return false;
                        }
                        else
                        {
                            stack--;
                            break;
                        }

                    case SyntaxKind.GreaterThanGreaterThanToken:
                        stack++;
                        goto case SyntaxKind.GreaterThanToken;

                    // fall through
                    case SyntaxKind.GreaterThanToken:
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
                        break;

                    case SyntaxKind.CommaToken:
                        if (stack == 0)
                        {
                            index++;
                        }

                        break;

                    default:
                        // user might have typed "in" on the way to typing "int"
                        // don't want to disregard this genericname because of that
                        if (SyntaxFacts.IsKeywordKind(token.Kind()))
                        {
                            break;
                        }

                        // anything else and we're sunk.
                        return false;
                }

                // look backward one token, include skipped tokens, because the parser frequently
                // does skip them in cases like: "Func<A, B", which get parsed as: expression
                // statement "Func<A" with missing semicolon, expression statement "B" with missing
                // semicolon, and the "," is skipped.
                token = token.GetPreviousToken(includeSkipped: true);
                if (token.Kind() == SyntaxKind.None)
                {
                    return false;
                }
            }
        }

        private static bool IsFullyWrittenGeneric(SyntaxToken token, SyntaxToken lessThanToken)
        {
            return token.Parent is GenericNameSyntax genericName && genericName.TypeArgumentList != null &&
                   genericName.TypeArgumentList.LessThanToken == lessThanToken && !genericName.TypeArgumentList.GreaterThanToken.IsMissing;
        }
    }
}
