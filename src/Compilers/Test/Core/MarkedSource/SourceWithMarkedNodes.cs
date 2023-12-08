// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Roslyn.Test.Utilities
{
    internal sealed partial class SourceWithMarkedNodes
    {
        /// <summary>
        /// The source with markers stripped out, that was used to produce the tree
        /// </summary>
        public readonly string Source;

        /// <summary>
        /// The original input source with markers intact.
        /// </summary>
        public readonly string Input;

        public readonly ImmutableArray<MarkedSpan> MarkedSpans;
        public readonly SyntaxNode Root;

        /// <summary>
        /// Parses source code with markers for further processing
        /// </summary>
        /// <param name="markedSource">The marked source</param>
        /// <param name="parser">Delegate to turn source code into a syntax tree</param>
        /// <param name="getSyntaxKind">Delegate to turn a marker into a syntax kind</param>
        /// <param name="removeTags">Whether to remove tags from the source, as distinct from replacing them with whitespace. Note that if this
        /// value is true then any marked node other than the first, will have an incorrect offset.</param>
        public SourceWithMarkedNodes(string markedSource, Func<string, SyntaxTree> parser, Func<string, int> getSyntaxKind, bool removeTags = false)
        {
            Source = removeTags ? RemoveTags(markedSource) : ClearTags(markedSource);
            Input = markedSource;
            Root = parser(Source).GetRoot();

            MarkedSpans = ImmutableArray.CreateRange(GetSpansRecursive(markedSource, 0, getSyntaxKind));
        }

        public SyntaxTree Tree => Root.SyntaxTree;

        private static IEnumerable<MarkedSpan> GetSpansRecursive(string markedSource, int offset, Func<string, int> getSyntaxKind)
        {
            foreach (var match in s_markerPattern.Matches(markedSource).ToEnumerable())
            {
                var tagName = match.Groups["TagName"];
                var markedSyntax = match.Groups["MarkedSyntax"];
                var syntaxKindOpt = match.Groups["SyntaxKind"].Value;
                var idOpt = match.Groups["Id"].Value;
                var id = string.IsNullOrEmpty(idOpt) ? 0 : int.Parse(idOpt);
                var parentIdOpt = match.Groups["ParentId"].Value;
                var parentId = string.IsNullOrEmpty(parentIdOpt) ? 0 : int.Parse(parentIdOpt);
                var parsedKind = string.IsNullOrEmpty(syntaxKindOpt) ? 0 : getSyntaxKind(syntaxKindOpt);
                int absoluteOffset = offset + markedSyntax.Index;

                yield return new MarkedSpan(new TextSpan(absoluteOffset, markedSyntax.Length), new TextSpan(match.Index, match.Length), tagName.Value, parsedKind, id, parentId);

                foreach (var nestedSpan in GetSpansRecursive(markedSyntax.Value, absoluteOffset, getSyntaxKind))
                {
                    yield return nestedSpan;
                }
            }
        }

        internal static string RemoveTags(string source)
        {
            return s_tags.Replace(source, "");
        }

        internal static string ClearTags(string source)
        {
            return s_tags.Replace(source, m => new string(' ', m.Length));
        }

        private static readonly Regex s_tags = new Regex(
            @"[<][/]?[NMCL][:][:\.A-Za-z0-9]*[>]",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        private static readonly Regex s_markerPattern = new Regex(
            @"[<]                                # Open tag
              (?<TagName>[NMCL])                 # The actual tag name can be any of these letters

              (                                  # Start a group so that everything after the tag can be optional
                [:]                              # A colon
                (?<Id>[0-9]+)                    # The first number after the colon is the Id
                ([.](?<ParentId>[0-9]+))?        # Digits after a decimal point are the parent Id
                ([:](?<SyntaxKind>[A-Za-z]+))?   # A second colon separates the syntax kind
              )                                  # Close the group for the things after the tag name
              [>]                                # Close tag

              (                                  # Start a group so that the closing tag is optional
                (?<MarkedSyntax>.*)              # This matches the source within the tags
                [<][/][NMCL][:]?(\k<Id>)* [>]    # The closing tag with its optional Id
              )?                                 # End of the group for the closing tag",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        public ImmutableDictionary<SyntaxNode, int> MapSyntaxNodesToMarks()
            => MarkedSpans.ToImmutableDictionary(
                keySelector: marker => GetNode(Root, marker),
                elementSelector: marker => marker.Id);

        public ImmutableDictionary<int, SyntaxNode> MapMarksToSyntaxNodes()
            => MarkedSpans.ToImmutableDictionary(
                keySelector: marker => marker.Id,
                elementSelector: marker => GetNode(Root, marker));

        public SyntaxNode GetNode(string tag, int id)
            => GetNode(Root, MarkedSpans.Single(s => s.TagName == tag && s.Id == id));

        private static SyntaxNode GetNode(SyntaxNode root, MarkedSpan marker)
        {
            var node = root.FindNode(marker.MarkedSyntax, getInnermostNodeForTie: true);
            if (marker.SyntaxKind == 0)
            {
                return node;
            }

            var nodeOfKind = node.FirstAncestorOrSelf<SyntaxNode>(n => n.RawKind == marker.SyntaxKind);
            Assert.NotNull(nodeOfKind);
            return nodeOfKind;
        }

        public static Func<SyntaxNode, SyntaxNode> GetSyntaxMap(SourceWithMarkedNodes source0, SourceWithMarkedNodes source1, List<SyntaxNode> unmappedNodes = null)
        {
            var map0 = source0.MapMarksToSyntaxNodes();
            var map1 = source1.MapSyntaxNodesToMarks();

            return new Func<SyntaxNode, SyntaxNode>(node1 =>
            {
                if (map1.TryGetValue(node1, out var mark))
                {
                    if (map0.TryGetValue(mark, out var result))
                    {
                        return result;
                    }

                    unmappedNodes?.Add(node1);
                }

                return null;
            });
        }
    }
}
