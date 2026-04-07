// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Utilities;

internal static partial class UsingsAndExternAliasesOrganizer
{
    public static void Organize(
        SyntaxList<ExternAliasDirectiveSyntax> externAliasList,
        SyntaxList<UsingDirectiveSyntax> usingList,
        bool placeSystemNamespaceFirst,
        bool separateGroups,
        SyntaxTrivia fallbackTrivia,
        out SyntaxList<ExternAliasDirectiveSyntax> organizedExternAliasList,
        out SyntaxList<UsingDirectiveSyntax> organizedUsingList)
    {
        // Attempt to use an existing newline trivia from the existing usings/externs.  If we can't find any use what
        // the caller passed in.
        var newLineTrivia = ((IEnumerable<SyntaxNode>)externAliasList)
            .Concat(usingList)
            .Select(n => n.GetTrailingTrivia().FirstOrNull(t => t.Kind() == SyntaxKind.EndOfLineTrivia))
            .Where(t => t != null)
            .FirstOrDefault() ?? fallbackTrivia;

        OrganizeWorker(
            externAliasList, usingList, placeSystemNamespaceFirst,
            newLineTrivia,
            out organizedExternAliasList, out organizedUsingList);

        if (separateGroups)
        {
            if (organizedExternAliasList.Count > 0 && organizedUsingList.Count > 0)
            {
                var firstUsing = organizedUsingList[0];

                if (!firstUsing.GetLeadingTrivia().Any(t => t.IsEndOfLine()))
                {
                    var newFirstUsing = firstUsing.WithPrependedLeadingTrivia(newLineTrivia);
                    organizedUsingList = organizedUsingList.Replace(firstUsing, newFirstUsing);
                }
            }

            for (var i = 1; i < organizedUsingList.Count; i++)
            {
                var lastUsing = organizedUsingList[i - 1];
                var currentUsing = organizedUsingList[i];

                if (NeedsGrouping(lastUsing, currentUsing) &&
                    !currentUsing.GetLeadingTrivia().Any(t => t.IsEndOfLine()))
                {
                    var newCurrentUsing = currentUsing.WithPrependedLeadingTrivia(newLineTrivia);
                    organizedUsingList = organizedUsingList.Replace(currentUsing, newCurrentUsing);
                }
            }
        }
    }

    // NOTE: Stay in sync with TokenBasedFormattingRule.GetGroupIdentifier
    public static bool NeedsGrouping(
        UsingDirectiveSyntax using1,
        UsingDirectiveSyntax using2)
    {
        var directive1IsUsingStatic = using1.StaticKeyword.IsKind(SyntaxKind.StaticKeyword);
        var directive2IsUsingStatic = using2.StaticKeyword.IsKind(SyntaxKind.StaticKeyword);

        var directive1IsAlias = using1.Alias != null;
        var directive2IsAlias = using2.Alias != null;

        var directive1IsNamespace = !directive1IsUsingStatic && !directive1IsAlias;
        var directive2IsNamespace = !directive2IsUsingStatic && !directive2IsAlias;

        if (directive1IsAlias && directive2IsAlias)
        {
            return false;
        }

        if (directive1IsUsingStatic && directive2IsUsingStatic)
        {
            return false;
        }

        if (directive1IsNamespace && directive2IsNamespace)
        {
            // Both normal usings.  Place them in groups if their first namespace
            // component differs.
            // LanguageParser.ParseUsingDirective guarantees that if there is no alias, Name is always present
            Contract.ThrowIfNull(using1.Name);
            Contract.ThrowIfNull(using2.Name);
            var name1 = using1.Name.GetFirstToken().ValueText;
            var name2 = using2.Name.GetFirstToken().ValueText;
            return name1 != name2;
        }

        // They have different types, definitely put them into new groups.
        return true;
    }

    private static void OrganizeWorker(
        SyntaxList<ExternAliasDirectiveSyntax> externAliasList,
        SyntaxList<UsingDirectiveSyntax> usingList,
        bool placeSystemNamespaceFirst,
        SyntaxTrivia newLineTrivia,
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
                initialList[0] = initialList[0].GetNodeWithoutLeadingBannerAndPreprocessorDirectives(out var leadingTrivia);

                var comparer = placeSystemNamespaceFirst
                    ? UsingsAndExternAliasesDirectiveComparer.SystemFirstInstance
                    : UsingsAndExternAliasesDirectiveComparer.NormalInstance;

                var finalList = initialList.OrderBy(comparer).ToList();

                // Check if sorting the list actually changed anything.  If not, then we don't
                // need to make any changes to the file.
                if (!finalList.SequenceEqual(initialList))
                {
                    // Make sure newlines are correct between nodes.
                    EnsureNewLines(finalList, newLineTrivia);

                    // Reattach the banner.
                    finalList[0] = finalList[0].WithPrependedLeadingTrivia(leadingTrivia);

                    // Now split out the externs and usings back into two separate lists.
                    organizedExternAliasList = [.. finalList
                        .Where(t => t is ExternAliasDirectiveSyntax)
                        .Cast<ExternAliasDirectiveSyntax>()];
                    organizedUsingList = [.. finalList
                        .Where(t => t is UsingDirectiveSyntax)
                        .Cast<UsingDirectiveSyntax>()];
                    return;
                }
            }
        }

        organizedExternAliasList = externAliasList;
        organizedUsingList = usingList;
    }

    private static void EnsureNewLines(IList<SyntaxNode> list, SyntaxTrivia newLineTrivia)
    {
        // First, make sure that every node (except the last one) ends with
        // a newline.
        for (var i = 0; i < list.Count - 1; i++)
        {
            var node = list[i];
            var trailingTrivia = node.GetTrailingTrivia();

            if (!trailingTrivia.Any() || trailingTrivia.Last().Kind() != SyntaxKind.EndOfLineTrivia)
            {
                list[i] = node.WithTrailingTrivia(trailingTrivia.Concat(newLineTrivia));
            }
        }

        // Now, make sure that every node (except the first one) does *not*
        // start with newlines.
        for (var i = 1; i < list.Count; i++)
        {
            var node = list[i];
            list[i] = TrimLeadingNewLines(node);
        }

        list[0] = TrimLeadingNewLines(list[0]);
    }

    private static SyntaxNode TrimLeadingNewLines(SyntaxNode node)
        => node.WithLeadingTrivia(node.GetLeadingTrivia().SkipWhile(t => t.Kind() == SyntaxKind.EndOfLineTrivia));
}
