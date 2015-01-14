// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal static partial class UsingsAndExternAliasesOrganizer
    {
        public static void Organize(
            SyntaxList<ExternAliasDirectiveSyntax> externAliasList,
            SyntaxList<UsingDirectiveSyntax> usingList,
            bool placeSystemNamespaceFirst,
            out SyntaxList<ExternAliasDirectiveSyntax> organizedExternAliasList,
            out SyntaxList<UsingDirectiveSyntax> organizedUsingList)
        {
            if (externAliasList.Count > 0 || usingList.Count > 0)
            {
                // Merge the list of usings and externs into one list.  
                // order them in the order that they were originally in the
                // file.
                var initialList = usingList.Cast<SyntaxNode>()
                                           .Concat(externAliasList)
                                           .OrderBy(n => n.SpanStart).ToList();

                if (!initialList.SpansPreprocessorDirective())
                {
                    // If there is a banner comment that precedes the nodes,
                    // then remove it and store it for later.
                    IEnumerable<SyntaxTrivia> leadingTrivia;
                    initialList[0] = initialList[0].GetNodeWithoutLeadingBannerAndPreprocessorDirectives(out leadingTrivia);

                    var comparer = placeSystemNamespaceFirst
                        ? UsingsAndExternAliasesDirectiveComparer.SystemFirstInstance
                        : UsingsAndExternAliasesDirectiveComparer.NormalInstance;

                    var finalList = initialList.OrderBy(comparer).ToList();

                    // Check if sorting the list actually changed anything.  If not, then we don't
                    // need to make any changes to the file.
                    if (!finalList.SequenceEqual(initialList))
                    {
                        // Make sure newlines are correct between nodes.
                        EnsureNewLines(finalList);

                        // Reattach the banner.
                        finalList[0] = finalList[0].WithPrependedLeadingTrivia(leadingTrivia);

                        // Now split out the externs and usings back into two separate lists.
                        organizedExternAliasList = finalList.Where(t => t is ExternAliasDirectiveSyntax)
                                                            .Cast<ExternAliasDirectiveSyntax>()
                                                            .ToSyntaxList();
                        organizedUsingList = finalList.Where(t => t is UsingDirectiveSyntax)
                                                      .Cast<UsingDirectiveSyntax>()
                                                      .ToSyntaxList();
                        return;
                    }
                }
            }

            organizedExternAliasList = externAliasList;
            organizedUsingList = usingList;
        }

        private static void EnsureNewLines(IList<SyntaxNode> list)
        {
            // First, make sure that every node (except the last one) ends with
            // a newline.
            for (int i = 0; i < list.Count - 1; i++)
            {
                var node = list[i];
                var trailingTrivia = node.GetTrailingTrivia();

                if (!trailingTrivia.Any() || trailingTrivia.Last().Kind() != SyntaxKind.EndOfLineTrivia)
                {
                    // TODO(cyrusn): Don't use CRLF.  Use the appropriate 
                    // newline for this file.
                    list[i] = node.WithTrailingTrivia(trailingTrivia.Concat(SyntaxFactory.CarriageReturnLineFeed));
                }
            }

            // Now, make sure that every node (except the first one) does *not*
            // start with newlines.
            for (int i = 1; i < list.Count; i++)
            {
                var node = list[i];
                list[i] = node.WithLeadingTrivia(node.GetLeadingTrivia().SkipWhile(t => t.Kind() == SyntaxKind.EndOfLineTrivia));
            }

            list[0] = list[0].WithLeadingTrivia(list[0].GetLeadingTrivia().SkipWhile(t => t.Kind() == SyntaxKind.EndOfLineTrivia));
        }
    }
}
