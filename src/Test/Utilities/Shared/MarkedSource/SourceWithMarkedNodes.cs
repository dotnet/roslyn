// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.Test.Utilities
{
    internal sealed class SourceWithMarkedNodes
    {
        public readonly string Source;
        public readonly SyntaxTree Tree;
        public readonly ImmutableArray<ValueTuple<TextSpan, int, int>> SpansAndKindsAndIds;

        public SourceWithMarkedNodes(string markedSource, Func<string, SyntaxTree> parser, Func<string, int> getSyntaxKind)
        {
            Source = ClearTags(markedSource);
            Tree = parser(Source);

            SpansAndKindsAndIds = ImmutableArray.CreateRange(GetSpansRecursive(markedSource, 0, getSyntaxKind));
        }

        private static IEnumerable<ValueTuple<TextSpan, int, int>> GetSpansRecursive(string markedSource, int offset, Func<string, int> getSyntaxKind)
        {
            foreach (var match in s_markerPattern.Matches(markedSource).ToEnumerable())
            {
                var markedSyntax = match.Groups["MarkedSyntax"];
                var syntaxKindOpt = match.Groups["SyntaxKind"].Value;
                var id = int.Parse(match.Groups["Id"].Value);
                var parsedKind = string.IsNullOrEmpty(syntaxKindOpt) ? 0 : getSyntaxKind(syntaxKindOpt);
                int absoluteOffset = offset + markedSyntax.Index;

                yield return ValueTuple.Create(new TextSpan(absoluteOffset, markedSyntax.Length), parsedKind, id);

                foreach (var nestedSpan in GetSpansRecursive(markedSyntax.Value, absoluteOffset, getSyntaxKind))
                {
                    yield return nestedSpan;
                }
            }
        }

        internal static string ClearTags(string source)
        {
            return s_tags.Replace(source, m => new string(' ', m.Length));
        }

        private static readonly Regex s_tags = new Regex(
            @"[<][/]?N[:][:A-Za-z0-9]+[>]",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        private static readonly Regex s_markerPattern = new Regex(
            @"[<]N[:] (?<Id>[0-9]+) ([:](?<SyntaxKind>[A-Za-z]+))? [>]
              (?<MarkedSyntax>.*)
              [<][/]N[:](\k<Id>) [>]",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        public ImmutableDictionary<SyntaxNode, int> MapSyntaxNodesToMarks()
        {
            var root = Tree.GetRoot();
            var builder = ImmutableDictionary.CreateBuilder<SyntaxNode, int>();
            for (int i = 0; i < SpansAndKindsAndIds.Length; i++)
            {
                var node = GetNode(root, SpansAndKindsAndIds[i]);
                builder.Add(node, SpansAndKindsAndIds[i].Item3);
            }

            return builder.ToImmutableDictionary();
        }

        private SyntaxNode GetNode(SyntaxNode root, ValueTuple<TextSpan, int, int> spanAndKindAndId)
        {
            var node = root.FindNode(spanAndKindAndId.Item1, getInnermostNodeForTie: true);
            if (spanAndKindAndId.Item2 == 0)
            {
                return node;
            }

            var nodeOfKind = node.FirstAncestorOrSelf<SyntaxNode>(n => n.RawKind == spanAndKindAndId.Item2);
            Assert.NotNull(nodeOfKind);
            return nodeOfKind;
        }

        public ImmutableDictionary<int, SyntaxNode> MapMarksToSyntaxNodes()
        {
            var root = Tree.GetRoot();
            var builder = ImmutableDictionary.CreateBuilder<int, SyntaxNode>();
            for (int i = 0; i < SpansAndKindsAndIds.Length; i++)
            {
                builder.Add(SpansAndKindsAndIds[i].Item3, GetNode(root, SpansAndKindsAndIds[i]));
            }

            return builder.ToImmutableDictionary();
        }

        public static Func<SyntaxNode, SyntaxNode> GetSyntaxMap(SourceWithMarkedNodes source0, SourceWithMarkedNodes source1)
        {
            var map0 = source0.MapMarksToSyntaxNodes();
            var map1 = source1.MapSyntaxNodesToMarks();
#if DUMP
            Console.WriteLine("========");
#endif
            return new Func<SyntaxNode, SyntaxNode>(node1 =>
            {
                int mark;
                SyntaxNode result;
                if (map1.TryGetValue(node1, out mark) && map0.TryGetValue(mark, out result))
                {
                    return result;
                }

#if DUMP
                Console.WriteLine($"? {node1.RawKind} [[{node1}]]");
#endif
                return null;
            });
        }
    }
}
