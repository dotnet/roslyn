// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using CSharpToVisualBasicConverter.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace CSharpToVisualBasicConverter
{
    public static partial class Converter
    {
        public static SyntaxNode Convert(
            SyntaxTree syntaxTree,
            IDictionary<string, string> identifierMap = null,
            bool convertStrings = false)
        {
            SourceText text = syntaxTree.GetText();
            SyntaxNode node = syntaxTree.GetRoot();

            string vbText = Convert(text, node, identifierMap, convertStrings);

            return VB.SyntaxFactory.ParseSyntaxTree(vbText).GetRoot();
        }

        public static string Convert(
            string text,
            IDictionary<string, string> identifierMap = null,
            bool convertStrings = false)
        {
            List<Func<string, SyntaxNode>> parseFunctions = new List<Func<string, SyntaxNode>>()
            {
                s => CS.SyntaxFactory.ParseExpression(s),
                s => CS.SyntaxFactory.ParseStatement(s),
            };

            foreach (Func<string, SyntaxNode> parse in parseFunctions)
            {
                SyntaxNode node = parse(text);
                SourceText stringText = SourceText.From(text);

                if (!node.ContainsDiagnostics && node.FullSpan.Length == text.Length)
                {
                    return Convert(stringText, node, identifierMap, convertStrings);
                }
            }

            return Convert(CS.SyntaxFactory.ParseSyntaxTree(text), identifierMap, convertStrings).ToFullString();
        }

        private static string Convert(
            SourceText text,
            SyntaxNode node,
            IDictionary<string, string> identifierMap,
            bool convertStrings)
        {
            if (node is CS.Syntax.StatementSyntax)
            {
                NodeVisitor nodeVisitor = new NodeVisitor(text, identifierMap, convertStrings);
                StatementVisitor statementVisitor = new StatementVisitor(nodeVisitor, text);
                SyntaxList<VB.Syntax.StatementSyntax> vbStatements = statementVisitor.Visit(node);

                return string.Join(Environment.NewLine, vbStatements.Select(s => s.NormalizeWhitespace()));
            }
            else
            {
                NodeVisitor visitor = new NodeVisitor(text, identifierMap, convertStrings);
                SyntaxNode vbNode = visitor.Visit(node);

                return vbNode.NormalizeWhitespace().ToFullString();
            }
        }

        private static SeparatedSyntaxList<T> SeparatedList<T>(T value)
            where T : SyntaxNode => VB.SyntaxFactory.SingletonSeparatedList(value);

        private static SyntaxTriviaList TriviaList(IEnumerable<SyntaxTrivia> list)
            => VB.SyntaxFactory.TriviaList(list);

        private static SyntaxList<T> List<T>(params T[] nodes)
            where T : SyntaxNode => List(nodes.Where(n => n != null));

        private static SyntaxList<T> List<T>(IEnumerable<T> nodes)
            where T : SyntaxNode => VB.SyntaxFactory.List<T>(nodes);

        private static SeparatedSyntaxList<T> SeparatedCommaList<T>(IEnumerable<T> nodes)
            where T : SyntaxNode
        {
            IList<T> nodesList = nodes as IList<T> ?? nodes.ToList();
            List<SyntaxNodeOrToken> builder = new List<SyntaxNodeOrToken>();
            SyntaxToken token = VB.SyntaxFactory.Token(VB.SyntaxKind.CommaToken);

            bool first = true;
            foreach (T node in nodes)
            {
                if (!first)
                {
                    builder.Add(token);
                }

                first = false;
                builder.Add(node);
            }

            return VB.SyntaxFactory.SeparatedList<T>(builder);
        }

        private static string RemoveNewLines(string text) =>
            text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");

        private static string CreateCouldNotBeConvertedText(string text, Type type)
            => "'" + RemoveNewLines(text) + "' could not be converted to a " + type.Name;

        private static string CreateCouldNotBeConvertedComment(string text, Type type)
            => "' " + CreateCouldNotBeConvertedText(text, type);

        private static string CreateCouldNotBeConvertedString(string text, Type type)
            => "\"" + CreateCouldNotBeConvertedText(text, type) + "\"";

        private static VB.Syntax.StatementSyntax CreateBadStatement(string text, Type type)
        {
            string comment = CreateCouldNotBeConvertedComment(text, type);
            SyntaxTrivia trivia = VB.SyntaxFactory.CommentTrivia(comment);

            SyntaxToken token = VB.SyntaxFactory.Token(SyntaxTriviaList.Create(trivia), VB.SyntaxKind.EmptyToken);
            return VB.SyntaxFactory.EmptyStatement(token);
        }

        private static VB.Syntax.StatementSyntax CreateBadStatement(SyntaxNode node, NodeVisitor visitor)
        {
            IEnumerable<SyntaxTrivia> leadingTrivia = node.GetFirstToken(includeSkipped: true).LeadingTrivia.SelectMany(visitor.VisitTrivia);
            IEnumerable<SyntaxTrivia> trailingTrivia = node.GetLastToken(includeSkipped: true).TrailingTrivia.SelectMany(visitor.VisitTrivia);

            string comment = CreateCouldNotBeConvertedComment(node.ToString(), typeof(VB.Syntax.StatementSyntax));
            leadingTrivia = leadingTrivia.Concat(
                VB.SyntaxFactory.CommentTrivia(comment));

            SyntaxToken token = VB.SyntaxFactory.Token(TriviaList(leadingTrivia), VB.SyntaxKind.EmptyToken, trailing: TriviaList(trailingTrivia));
            return VB.SyntaxFactory.EmptyStatement(token);
        }

        private static VB.Syntax.StructuredTriviaSyntax CreateBadDirective(SyntaxNode node, NodeVisitor visitor)
        {
            IEnumerable<SyntaxTrivia> leadingTrivia = node.GetFirstToken(includeSkipped: true).LeadingTrivia.SelectMany(visitor.VisitTrivia).Where(t => !t.IsKind(VB.SyntaxKind.EndOfLineTrivia));
            IEnumerable<SyntaxTrivia> trailingTrivia = node.GetLastToken(includeSkipped: true).TrailingTrivia.SelectMany(visitor.VisitTrivia).Where(t => !t.IsKind(VB.SyntaxKind.EndOfLineTrivia));

            string comment = CreateCouldNotBeConvertedComment(node.ToString(), typeof(VB.Syntax.StatementSyntax));
            leadingTrivia = leadingTrivia.Concat(
                VB.SyntaxFactory.CommentTrivia(comment));

            SyntaxToken token = VB.SyntaxFactory.Token(TriviaList(leadingTrivia), VB.SyntaxKind.HashToken, trailing: TriviaList(trailingTrivia), text: "");
            return VB.SyntaxFactory.BadDirectiveTrivia(token);
        }
    }
}
