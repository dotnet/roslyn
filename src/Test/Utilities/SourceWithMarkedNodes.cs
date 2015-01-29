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
        public readonly ImmutableArray<ValueTuple<TextSpan, int>> SpansAndKinds;

        public SourceWithMarkedNodes(string markedSource, Func<string, SyntaxTree> parser, Func<string, int> getSyntaxKind)
        {
            Source = ClearTags(markedSource);
            Tree = parser(Source);

            SpansAndKinds = ImmutableArray.CreateRange(
                from match in MarkerPattern.Matches(markedSource).ToEnumerable()
                let markedSyntax = match.Groups["MarkedSyntax"]
                let syntaxKindOpt = match.Groups["SyntaxKind"].Value
                let parsedKind = string.IsNullOrEmpty(syntaxKindOpt) ? 0 : getSyntaxKind(syntaxKindOpt)
                select ValueTuple.Create(new TextSpan(markedSyntax.Index, markedSyntax.Length), parsedKind));
        }

        internal static string ClearTags(string source)
        {
            return Tags.Replace(source, m => new string(' ', m.Length));
        }

        private static readonly Regex Tags = new Regex(
            @"[<][/]?N[:][:A-Za-z0-9]+[>]",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        private static readonly Regex MarkerPattern = new Regex(
            @"[<]N[:] (?<Id>[0-9]+) ([:](?<SyntaxKind>[A-Za-z]+))? [>]
              (?<MarkedSyntax>.*)
              [<][/]N[:](\k<Id>) [>]",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        public ImmutableDictionary<SyntaxNode, int> MapSyntaxNodesToMarks()
        {
            var root = Tree.GetRoot();
            var builder = ImmutableDictionary.CreateBuilder<SyntaxNode, int>();
            for (int i = 0; i < SpansAndKinds.Length; i++)
            {
                builder.Add(GetNode(root, SpansAndKinds[i]), i);
            }

            return builder.ToImmutableDictionary();
        }

        private SyntaxNode GetNode(SyntaxNode root, ValueTuple<TextSpan, int> spanAndKind)
        {
            var node = root.FindNode(spanAndKind.Item1, getInnermostNodeForTie: true);
            if (spanAndKind.Item2 == 0)
            {
                return node;
            }

            var nodeOfKind = node.FirstAncestorOrSelf<SyntaxNode>(n => n.RawKind == spanAndKind.Item2);
            Assert.NotNull(nodeOfKind);
            return nodeOfKind;
        }

        public ImmutableDictionary<int, SyntaxNode> MapMarksToSyntaxNodes()
        {
            var root = Tree.GetRoot();
            var builder = ImmutableDictionary.CreateBuilder<int, SyntaxNode>();
            for (int i = 0; i < SpansAndKinds.Length; i++)
            {
                builder.Add(i, GetNode(root, SpansAndKinds[i]));
            }

            return builder.ToImmutableDictionary();
        }

        public static Func<SyntaxNode, SyntaxNode> GetSyntaxMap(SourceWithMarkedNodes source0, SourceWithMarkedNodes source1)
        {
            var map0 = source0.MapMarksToSyntaxNodes();
            var map1 = source1.MapSyntaxNodesToMarks();

            return new Func<SyntaxNode, SyntaxNode>(node1 =>
            {
                int mark;
                var result = map1.TryGetValue(node1, out mark) ? map0[mark] : null;
                return result;
            });
        }
    }
}
