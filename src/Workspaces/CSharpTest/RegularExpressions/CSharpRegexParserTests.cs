// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.RegularExpressions;
using Microsoft.CodeAnalysis.RegularExpressions;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.RegularExpressions
{
    internal class LogicalStringComparer : IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        public static readonly IComparer<string> Instance = new LogicalStringComparer();

        private LogicalStringComparer()
        {
        }

        public int Compare(string x, string y)
        {
            return StrCmpLogicalW(x, y);
        }
    }

    public class Fixture : IDisposable
    {
        public void Dispose()
        {
            var tests = CSharpRegexParserTests.nameToTest;
            var other = new Dictionary<string, string>();

            var referenceTests =
                tests.Where(kvp => kvp.Key.StartsWith("ReferenceTest"))
                     .OrderBy(kvp => kvp.Key, LogicalStringComparer.Instance)
                     .Select(kvp => kvp.Value);

            var val = string.Join("\r\n", referenceTests);
        }
    }

    [CollectionDefinition(nameof(MyCollection))]
    public class MyCollection : ICollectionFixture<Fixture>
    {
    }

    [Collection(nameof(MyCollection))]
    public partial class CSharpRegexParserTests
    {
        private readonly IVirtualCharService _service = new CSharpVirtualCharService();
        private const string _statmentPrefix = "var v = ";

        public CSharpRegexParserTests(Fixture fixture)
        {
            _fixture = fixture;
        }

        private SyntaxToken GetStringToken(string text)
        {
            var statement = _statmentPrefix + text;
            var parsedStatement = SyntaxFactory.ParseStatement(statement);
            var token = parsedStatement.DescendantTokens().ToArray()[3];
            Assert.True(token.Kind() == SyntaxKind.StringLiteralToken);

            return token;
        }

        private readonly Fixture _fixture;

        public static Dictionary<string, string> nameToTest = new Dictionary<string, string>();

        private void Test(string stringText, string expected, RegexOptions options, [CallerMemberName]string name = "")
        {
            var test = GenerateTests(stringText, options, name);
            nameToTest.Add(name, test);

#if false
            var tree = TryParseTree(stringText, options, conversionFailureOk: false);

            // TryParseSubTrees(stringText, options);

            var actual = TreeToText(tree).Replace("\"", "\"\"");
            Assert.Equal(expected.Replace("\"", "\"\""), actual);
#endif
        }

        public string GenerateTests(string val, RegexOptions options, string testName)
        {
            var builder = new StringBuilder();
            builder.AppendLine("[Fact]");
            builder.AppendLine("public void " + testName + "()");
            builder.AppendLine("{");
            builder.Append(@"    Test(");

            var escaped = val.Replace("\"", "\"\"");
            var quoted = "" + '@' + '"' + escaped + '"';
            builder.Append(quoted);

            var token = GetStringToken(val);
            var allChars = _service.TryConvertToVirtualChars(token);
            var tree = RegexParser.TryParse(allChars, options);

            var actual = TreeToText(tree).Replace("\"", "\"\"");
            builder.Append(", " + '@' + '"');
            builder.Append(actual);

            builder.AppendLine("" + '"' + ", RegexOptions." + options.ToString() + ");");
            builder.AppendLine("}");

            return builder.ToString();
        }


        private void TryParseSubTrees(string stringText, RegexOptions options)
        {
            var current = stringText;
            while (current != "@\"\"" && current != "\"\"")
            {
                current = current.Substring(0, current.Length - 2) + "\"";
                TryParseTree(current, options, conversionFailureOk: true);
            }

            current = stringText;
            while (current != "@\"\"" && current != "\"\"")
            {
                if (current[0] == '@')
                {
                    current = "@\"" + current.Substring(3);
                }
                else
                {
                    current = "\"" + current.Substring(2);
                }

                TryParseTree(current, options, conversionFailureOk: true);
            }
        }

        private (SyntaxToken, RegexTree, ImmutableArray<VirtualChar>) JustParseTree(
            string stringText, RegexOptions options, bool conversionFailureOk)
        {
            var token = GetStringToken(stringText);
            var allChars = _service.TryConvertToVirtualChars(token);
            if (allChars.IsDefault)
            {
                Assert.True(conversionFailureOk, "Failed to convert text to token.");
                return (token, null, allChars);
            }

            var tree = RegexParser.TryParse(allChars, options);
            return (token, tree, allChars);
        }

        private RegexTree TryParseTree(string stringText, RegexOptions options, bool conversionFailureOk)
        {
            var (token, tree, allChars) = JustParseTree(stringText, options, conversionFailureOk);
            if (tree == null)
            {
                Assert.True(allChars.IsDefault);
                return null;
            }

            CheckInvariants(tree, allChars);

            try
            {
                var regex = new Regex(token.ValueText, options);
                Assert.Empty(tree.Diagnostics);
            }
            catch (IndexOutOfRangeException)
            {
                // bug with .net regex parser.  Can happen with patterns like: (?<-0
                Assert.NotEmpty(tree.Diagnostics);
            }
            catch (NullReferenceException)
            {
                // bug with .net regex parser.  can happen with patterns like: (?(?S))
            }
            catch (OutOfMemoryException)
            {
                // bug with .net regex parser.  can happen with patterns like: a{2147483647,}
            }
            catch (Exception e)
            {
                Assert.NotEmpty(tree.Diagnostics);
                Assert.True(tree.Diagnostics.Any(d => e.Message.Contains(d.Message)));
            }

            return tree;
        }

        private string TreeToText(RegexTree tree)
        {
            var element = new XElement("Tree",
                NodeToElement(tree.Root));

            if (tree.Diagnostics.Length > 0) {
                element.Add(new XElement("Diagnostics",
                    tree.Diagnostics.Select(d =>
                        new XElement("Diagnostic",
                            new XAttribute("Message", d.Message),
                            new XAttribute("Start", d.Span.Start),
                            new XAttribute("Length", d.Span.Length)))));
            }

            element.Add(new XElement("Captures",
                tree.CaptureNumbersToSpan.OrderBy(kvp => kvp.Key).Select(kvp =>
                    new XElement("Capture", new XAttribute("Name", kvp.Key), new XAttribute("Span", kvp.Value))),
                tree.CaptureNamesToSpan.OrderBy(kvp => kvp.Key).Select(kvp =>
                    new XElement("Capture", new XAttribute("Name", kvp.Key), new XAttribute("Span", kvp.Value)))));

            return element.ToString();
        }

        private XElement NodeToElement(RegexNode node)
        {
            var element = new XElement(node.Kind.ToString());
            foreach (var child in node)
            {
                element.Add(child.IsNode ? NodeToElement(child.Node) : TokenToElement(child.Token));
            }

            return element;
        }

        private XElement TokenToElement(RegexToken token)
        {
            var element = new XElement(token.Kind.ToString());

            if (token.Value != null)
            {
                element.Add(new XAttribute("value", token.Value));
            }

            if (token.LeadingTrivia.Length > 0)
            {
                element.Add(new XElement("Trivia", token.LeadingTrivia.Select(t => TriviaToElement(t))));
            }

            if (token.VirtualChars.Length > 0)
            {
                element.Add(new string(token.VirtualChars.Select(vc => vc.Char).ToArray()));
            }

            return element;
        }

        private XElement TriviaToElement(RegexTrivia trivia)
            => new XElement(
                trivia.Kind.ToString(),
                new string(trivia.VirtualChars.Select(vc => vc.Char).ToArray()));

        private void CheckInvariants(RegexTree tree, ImmutableArray<VirtualChar> allChars)
        {
            var root = tree.Root;
            var position = 0;
            CheckInvariants(root, ref position, allChars);
            Assert.Equal(allChars.Length, position);
        }

        private void CheckInvariants(RegexNode node, ref int position, ImmutableArray<VirtualChar> allChars)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    CheckInvariants(child.Node, ref position, allChars);
                }
                else
                {
                    CheckInvariants(child.Token, ref position, allChars);
                }
            }
        }

        private void CheckInvariants(RegexToken token, ref int position, ImmutableArray<VirtualChar> allChars)
        {
            CheckInvariants(token.LeadingTrivia, ref position, allChars);
            CheckCharacters(token.VirtualChars, ref position, allChars);
        }

        private void CheckInvariants(ImmutableArray<RegexTrivia> leadingTrivia, ref int position, ImmutableArray<VirtualChar> allChars)
        {
            foreach (var trivia in leadingTrivia)
            {
                CheckInvariants(trivia, ref position, allChars);
            }
        }

        private void CheckInvariants(RegexTrivia trivia, ref int position, ImmutableArray<VirtualChar> allChars)
        {
            switch (trivia.Kind)
            {
            case RegexKind.CommentTrivia:
            case RegexKind.WhitespaceTrivia:
                break;
            default:
                Assert.False(true, "Incorrect trivia kind");
                return;
            }

            CheckCharacters(trivia.VirtualChars, ref position, allChars);
        }

        private static void CheckCharacters(ImmutableArray<VirtualChar> virtualChars, ref int position, ImmutableArray<VirtualChar> allChars)
        {
            for (var i = 0; i < virtualChars.Length; i++)
            {
                Assert.Equal(allChars[position + i], virtualChars[i]);
            }

            position += virtualChars.Length;
        }

        [Fact]
        public void TestEmpty()
        {
            Test("\"\"", @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOneWhitespace_IgnorePatternWhitespace()
        {
            Test("\" \"", @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestTwoWhitespace_IgnorePatternWhitespace()
        {
            Test("\"  \"", @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia>  </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestEmptyParenComment()
        {
            Test("\"(?#)\"", @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <CommentTrivia>(?#)</CommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestSimpleParenComment()
        {
            Test("\"(?# )\"", @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <CommentTrivia>(?# )</CommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnterminatedParenComment1()
        {
            Test("\"(?#\"", @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <CommentTrivia>(?#</CommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated (?#...) comment"" Start=""9"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnterminatedParenComment2()
        {
            Test("\"(?# \"", @"<Tree>
  <CompilationUnit>
    <Sequence />
    <EndOfFile>
      <Trivia>
        <CommentTrivia>(?# </CommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated (?#...) comment"" Start=""9"" Length=""4"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }
        
        [Fact]
        public void TestOpenQuestion1()
        {
            Test(@"""(?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken />
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""11"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOpenQuestion2()
        {
            Test(@"""(?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken />
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""11"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestOpenQuestion3()
        {
            Test(@"""(? """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>?</TextToken>
          </Text>
          <Text>
            <TextToken> </TextToken>
          </Text>
        </Sequence>
        <CloseParenToken />
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""12"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOpenQuestion4()
        {
            Test(@"""(? """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken />
      </SimpleGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""12"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestSimpleOptionsNode1()
        {
            Test("\"(?i)\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>i</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestSimpleOptionsNode2()
        {
            Test("\"(?im)\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>im</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestSimpleOptionsNode3()
        {
            Test("\"(?im-x)\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>im-x</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestSimpleOptionsNode4()
        {
            Test("\"(?im-x+n)\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>im-x+n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOptionThatDoesNotChangeWhitespaceScanning()
        {
            Test("\"(?i) \"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>i</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOptionThatDoesChangeWhitespaceScanning()
        {
            Test("\"(?x) \"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>x</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOptionThatDoesChangeWhitespaceScanning2()
        {
            Test("\" (?x) \"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>x</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOptionThatDoesChangeWhitespaceScanning3()
        {
            Test("\" (?-x) \"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-x</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestOptionRestoredWhenGroupPops()
        {
            Test("\" ( (?-x) ) \"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>(</OpenParenToken>
        <Sequence>
          <SimpleOptionsGrouping>
            <OpenParenToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>(</OpenParenToken>
            <QuestionToken>?</QuestionToken>
            <OptionsToken>-x</OptionsToken>
            <CloseParenToken>)</CloseParenToken>
          </SimpleOptionsGrouping>
          <Text>
            <TextToken> </TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNestedOptionGroup1()
        {
            Test("\" (?-x:) \"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NestedOptionsGrouping>
        <OpenParenToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-x</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNestedOptionGroup2()
        {
            Test("\" (?-x: ) \"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NestedOptionsGrouping>
        <OpenParenToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-x</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence>
          <Text>
            <TextToken> </TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNestedOptionGroup3()
        {
            Test("\" (?-x: (?+x: ) ) \"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NestedOptionsGrouping>
        <OpenParenToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-x</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence>
          <Text>
            <TextToken> </TextToken>
          </Text>
          <NestedOptionsGrouping>
            <OpenParenToken>(</OpenParenToken>
            <QuestionToken>?</QuestionToken>
            <OptionsToken>+x</OptionsToken>
            <ColonToken>:</ColonToken>
            <Sequence />
            <CloseParenToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>)</CloseParenToken>
          </NestedOptionsGrouping>
          <Text>
            <TextToken> </TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestIncompleteOptionsGroup1()
        {
            Test("\"(?-x\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-x</OptionsToken>
        <CloseParenToken />
      </SimpleOptionsGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestIncompleteOptionsGroup2()
        {
            Test("\"(?-x \"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-x</OptionsToken>
        <CloseParenToken />
      </SimpleOptionsGrouping>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestIncorrectOptionsGroup3()
        {
            Test("\"(?-x :\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-x</OptionsToken>
        <CloseParenToken />
      </SimpleOptionsGrouping>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <Text>
        <TextToken>:</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestIncorrectOptionsGroup4()
        {
            Test("\"(?-x )\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-x</OptionsToken>
        <CloseParenToken />
      </SimpleOptionsGrouping>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <Text>
        <TextToken>)</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Too many )'s"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestIncorrectOptionsGroup5()
        {
            Test("\"(?-x :)\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-x</OptionsToken>
        <CloseParenToken />
      </SimpleOptionsGrouping>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <Text>
        <TextToken>:</TextToken>
      </Text>
      <Text>
        <TextToken>)</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Too many )'s"" Start=""15"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestCloseParen()
        {
            Test("\")\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>)</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Too many )'s"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestSingleChar()
        {
            Test("\"a\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestTwoCharsChar()
        {
            Test("\"ab\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>b</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestAsteriskQuantifier()
        {
            Test("\"a*\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrMoreQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestAsteriskQuestionQuantifier()
        {
            Test("\"a*?\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <LazyQuantifier>
        <ZeroOrMoreQuantifier>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <AsteriskToken>*</AsteriskToken>
        </ZeroOrMoreQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPlusQuantifier()
        {
            Test("\"a+\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OneOrMoreQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <PlusToken>+</PlusToken>
      </OneOrMoreQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPlusQuestionQuantifier()
        {
            Test("\"a+?\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <LazyQuantifier>
        <OneOrMoreQuantifier>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <PlusToken>+</PlusToken>
        </OneOrMoreQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestQuestionQuantifier()
        {
            Test("\"a?\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrOneQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestQuestionQuestionQuantifier()
        {
            Test("\"a??\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <LazyQuantifier>
        <ZeroOrOneQuantifier>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <QuestionToken>?</QuestionToken>
        </ZeroOrOneQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestEmptySimpleGroup()
        {
            Test("\"()\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestGroupWithSingleElement()
        {
            Test("\"(a)\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestGroupWithMissingCloseParen()
        {
            Test("\"(\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken />
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""10"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestGroupWithElementWithMissingCloseParen()
        {
            Test("\"(a\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken />
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""11"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void JustBar()
        {
            Test("\"|\"", @"<Tree>
  <CompilationUnit>
    <Alternation>
      <Sequence />
      <BarToken>|</BarToken>
      <Sequence />
    </Alternation>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void SpaceBar()
        {
            Test("\" |\"", @"<Tree>
  <CompilationUnit>
    <Alternation>
      <Sequence>
        <Text>
          <TextToken> </TextToken>
        </Text>
      </Sequence>
      <BarToken>|</BarToken>
      <Sequence />
    </Alternation>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void BarSpace()
        {
            Test("\"| \"", @"<Tree>
  <CompilationUnit>
    <Alternation>
      <Sequence />
      <BarToken>|</BarToken>
      <Sequence>
        <Text>
          <TextToken> </TextToken>
        </Text>
      </Sequence>
    </Alternation>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void SpaceBarSpace()
        {
            Test("\" | \"", @"<Tree>
  <CompilationUnit>
    <Alternation>
      <Sequence>
        <Text>
          <TextToken> </TextToken>
        </Text>
      </Sequence>
      <BarToken>|</BarToken>
      <Sequence>
        <Text>
          <TextToken> </TextToken>
        </Text>
      </Sequence>
    </Alternation>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void JustBar_IgnoreWhitespace()
        {
            Test("\"|\"", @"<Tree>
  <CompilationUnit>
    <Alternation>
      <Sequence />
      <BarToken>|</BarToken>
      <Sequence />
    </Alternation>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void SpaceBar_IgnoreWhitespace()
        {
            Test("\" |\"", @"<Tree>
  <CompilationUnit>
    <Alternation>
      <Sequence />
      <BarToken>
        <Trivia>
          <WhitespaceTrivia> </WhitespaceTrivia>
        </Trivia>|</BarToken>
      <Sequence />
    </Alternation>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void BarSpace_IgnoreWhitespace()
        {
            Test("\"| \"", @"<Tree>
  <CompilationUnit>
    <Alternation>
      <Sequence />
      <BarToken>|</BarToken>
      <Sequence />
    </Alternation>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void SpaceBarSpace_IgnoreWhitespace()
        {
            Test("\" | \"", @"<Tree>
  <CompilationUnit>
    <Alternation>
      <Sequence />
      <BarToken>
        <Trivia>
          <WhitespaceTrivia> </WhitespaceTrivia>
        </Trivia>|</BarToken>
      <Sequence />
    </Alternation>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void DoubleBar()
        {
            Test("\"||\"", @"<Tree>
  <CompilationUnit>
    <Alternation>
      <Alternation>
        <Sequence />
        <BarToken>|</BarToken>
        <Sequence />
      </Alternation>
      <BarToken>|</BarToken>
      <Sequence />
    </Alternation>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void BarInGroup()
        {
            Test("\"(|)\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Alternation>
          <Sequence />
          <BarToken>|</BarToken>
          <Sequence />
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestExactNumericQuantifier()
        {
            Test("\"a{0}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ExactNumericQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""0"">0</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ExactNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOpenRangeNumericQuantifier()
        {
            Test("\"a{0,}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OpenRangeNumericQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""0"">0</NumberToken>
        <CommaToken>,</CommaToken>
        <CloseBraceToken>}</CloseBraceToken>
      </OpenRangeNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestClosedRangeNumericQuantifier()
        {
            Test("\"a{0,1}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ClosedRangeNumericQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""0"">0</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""1"">1</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestLargeExactRangeNumericQuantifier1()
        {
            Test("\"a{2147483647}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ExactNumericQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""2147483647"">2147483647</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ExactNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestLargeExactRangeNumericQuantifier2()
        {
            Test("\"a{2147483648}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ExactNumericQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""-2147483648"">2147483648</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ExactNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Capture group numbers must be less than or equal to Int32.MaxValue"" Start=""11"" Length=""10"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestLargeOpenRangeNumericQuantifier1()
        {
            Test("\"a{2147483647,}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OpenRangeNumericQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""2147483647"">2147483647</NumberToken>
        <CommaToken>,</CommaToken>
        <CloseBraceToken>}</CloseBraceToken>
      </OpenRangeNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestLargeOpenRangeNumericQuantifier2()
        {
            Test("\"a{2147483648,}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OpenRangeNumericQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""-2147483648"">2147483648</NumberToken>
        <CommaToken>,</CommaToken>
        <CloseBraceToken>}</CloseBraceToken>
      </OpenRangeNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Capture group numbers must be less than or equal to Int32.MaxValue"" Start=""11"" Length=""10"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestLargeClosedRangeNumericQuantifier1()
        {
            Test("\"a{0,2147483647}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ClosedRangeNumericQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""0"">0</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""2147483647"">2147483647</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestLargeClosedRangeNumericQuantifier2()
        {
            Test("\"a{0,2147483648}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ClosedRangeNumericQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""0"">0</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""-2147483648"">2147483648</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Capture group numbers must be less than or equal to Int32.MaxValue"" Start=""13"" Length=""10"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestBadMinMaxClosedRangeNumericQuantifier()
        {
            Test("\"a{1,0}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ClosedRangeNumericQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <OpenBraceToken>{</OpenBraceToken>
        <NumberToken value=""1"">1</NumberToken>
        <CommaToken>,</CommaToken>
        <NumberToken value=""0"">0</NumberToken>
        <CloseBraceToken>}</CloseBraceToken>
      </ClosedRangeNumericQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Illegal {x,y} with x &gt; y"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestLazyExactNumericQuantifier()
        {
            Test("\"a{0}?\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <LazyQuantifier>
        <ExactNumericQuantifier>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <OpenBraceToken>{</OpenBraceToken>
          <NumberToken value=""0"">0</NumberToken>
          <CloseBraceToken>}</CloseBraceToken>
        </ExactNumericQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestLazyOpenNumericQuantifier()
        {
            Test("\"a{0,}?\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <LazyQuantifier>
        <OpenRangeNumericQuantifier>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <OpenBraceToken>{</OpenBraceToken>
          <NumberToken value=""0"">0</NumberToken>
          <CommaToken>,</CommaToken>
          <CloseBraceToken>}</CloseBraceToken>
        </OpenRangeNumericQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestLazyClosedNumericQuantifier()
        {
            Test("\"a{0,1}?\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <LazyQuantifier>
        <ClosedRangeNumericQuantifier>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <OpenBraceToken>{</OpenBraceToken>
          <NumberToken value=""0"">0</NumberToken>
          <CommaToken>,</CommaToken>
          <NumberToken value=""1"">1</NumberToken>
          <CloseBraceToken>}</CloseBraceToken>
        </ClosedRangeNumericQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestIncompleteNumericQuantifier1()
        {
            Test("\"a{\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestIncompleteNumericQuantifier2()
        {
            Test("\"a{0\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestIncompleteNumericQuantifier3()
        {
            Test("\"a{0,\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>,</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestIncompleteNumericQuantifier4()
        {
            Test("\"a{0,1\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>,</TextToken>
      </Text>
      <Text>
        <TextToken>1</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNotNumericQuantifier1()
        {
            Test("\"a{0 }\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNotNumericQuantifier2()
        {
            Test("\"a{0, }\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>,</TextToken>
      </Text>
      <Text>
        <TextToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNotNumericQuantifier3()
        {
            Test("\"a{0 ,}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>,</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNotNumericQuantifier4()
        {
            Test("\"a{0 ,1}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>,</TextToken>
      </Text>
      <Text>
        <TextToken>1</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNotNumericQuantifier5()
        {
            Test("\"a{0, 1}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>,</TextToken>
      </Text>
      <Text>
        <TextToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>1</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNotNumericQuantifier6()
        {
            Test("\"a{0,1 }\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>a</TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>,</TextToken>
      </Text>
      <Text>
        <TextToken>1</TextToken>
      </Text>
      <Text>
        <TextToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestLazyQuantifierDueToIgnoredWhitespace()
        {
            Test("\"a* ?\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <LazyQuantifier>
        <ZeroOrMoreQuantifier>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <AsteriskToken>*</AsteriskToken>
        </ZeroOrMoreQuantifier>
        <QuestionToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>?</QuestionToken>
      </LazyQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNonLazyQuantifierDueToNonIgnoredWhitespace()
        {
            Test("\"a* ?\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrMoreQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <ZeroOrOneQuantifier>
        <Text>
          <TextToken> </TextToken>
        </Text>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestAsteriskQuantifierAtStart()
        {
            Test("\"*\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>*</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestAsteriskQuantifierAtStartOfGroup()
        {
            Test("\"(*)\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>*</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestAsteriskQuantifierAfterQuantifier()
        {
            Test("\"a**\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrMoreQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <Text>
        <TextToken>*</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Nested quantifier *"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPlusQuantifierAtStart()
        {
            Test("\"+\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>+</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPlusQuantifierAtStartOfGroup()
        {
            Test("\"(+)\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>+</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPlusQuantifierAfterQuantifier()
        {
            Test("\"a*+\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrMoreQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <Text>
        <TextToken>+</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Nested quantifier +"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestQuestionQuantifierAtStart()
        {
            Test("\"?\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>?</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestQuestionQuantifierAtStartOfGroup()
        {
            Test("\"(?)\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestQuestionQuantifierAfterQuantifier()
        {
            Test("\"a*??\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <LazyQuantifier>
        <ZeroOrMoreQuantifier>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <AsteriskToken>*</AsteriskToken>
        </ZeroOrMoreQuantifier>
        <QuestionToken>?</QuestionToken>
      </LazyQuantifier>
      <Text>
        <TextToken>?</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Nested quantifier ?"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNumericQuantifierAtStart()
        {
            Test("\"{0}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNumericQuantifierAtStartOfGroup()
        {
            Test("\"({0})\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>{</TextToken>
          </Text>
          <Text>
            <TextToken>0</TextToken>
          </Text>
          <Text>
            <TextToken>}</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""10"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNumericQuantifierAfterQuantifier()
        {
            Test("\"a*{0}\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrMoreQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Nested quantifier {"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNonNumericQuantifierAtStart()
        {
            Test("\"{0\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNonNumericQuantifierAtStartOfGroup()
        {
            Test("\"({0)\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>{</TextToken>
          </Text>
          <Text>
            <TextToken>0</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNonNumericQuantifierAfterQuantifier()
        {
            Test("\"a*{0\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrMoreQuantifier>
        <Text>
          <TextToken>a</TextToken>
        </Text>
        <AsteriskToken>*</AsteriskToken>
      </ZeroOrMoreQuantifier>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestEscapeAtEnd1()
        {
            Test("@\"\\\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken />
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Illegal \ at end of pattern"" Start=""10"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestEscapeAtEnd2()
        {
            Test(@"""\\""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken />
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Illegal \ at end of pattern"" Start=""9"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestSimpleEscape()
        {
            Test("@\"\\w\"", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClassEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>w</TextToken>
      </CharacterClassEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPrimaryEscapes1()
        {
            Test(@"@""\b\B\A\G\Z\z\w\W\s\W\s\S\d\D""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <AnchorEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>b</TextToken>
      </AnchorEscape>
      <AnchorEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>B</TextToken>
      </AnchorEscape>
      <AnchorEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>A</TextToken>
      </AnchorEscape>
      <AnchorEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>G</TextToken>
      </AnchorEscape>
      <AnchorEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>Z</TextToken>
      </AnchorEscape>
      <AnchorEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>z</TextToken>
      </AnchorEscape>
      <CharacterClassEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>w</TextToken>
      </CharacterClassEscape>
      <CharacterClassEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>W</TextToken>
      </CharacterClassEscape>
      <CharacterClassEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </CharacterClassEscape>
      <CharacterClassEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>W</TextToken>
      </CharacterClassEscape>
      <CharacterClassEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>s</TextToken>
      </CharacterClassEscape>
      <CharacterClassEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>S</TextToken>
      </CharacterClassEscape>
      <CharacterClassEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>d</TextToken>
      </CharacterClassEscape>
      <CharacterClassEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>D</TextToken>
      </CharacterClassEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape1()
        {
            Test(@"@""\c""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken />
      </ControlEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Missing control character"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape2()
        {
            Test(@"@""\c<""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken />
      </ControlEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape3()
        {
            Test(@"@""\ca""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken>a</TextToken>
      </ControlEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape4()
        {
            Test(@"@""\cA""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken>A</TextToken>
      </ControlEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape5()
        {
            Test(@"@""\c A""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken />
      </ControlEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <Text>
        <TextToken>A</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape6()
        {
            Test(@"@""\c(a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken />
      </ControlEscape>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape7()
        {
            Test(@"@""\c>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken />
      </ControlEscape>
      <Text>
        <TextToken>&gt;</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape8()
        {
            Test(@"@""\c?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ZeroOrOneQuantifier>
        <ControlEscape>
          <BackslashToken>\</BackslashToken>
          <TextToken>c</TextToken>
          <TextToken />
        </ControlEscape>
        <QuestionToken>?</QuestionToken>
      </ZeroOrOneQuantifier>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape9()
        {
            Test(@"@""\c@""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken>@</TextToken>
      </ControlEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape10()
        {
            Test(@"@""\c^""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken>^</TextToken>
      </ControlEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape11()
        {
            Test(@"@""\c_""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken>_</TextToken>
      </ControlEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape12()
        {
            Test(@"@""\c`""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken />
      </ControlEscape>
      <Text>
        <TextToken>`</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape13()
        {
            Test(@"@""\c{""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken />
      </ControlEscape>
      <Text>
        <TextToken>{</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape14()
        {
            Test(@"@""\ca""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken>a</TextToken>
      </ControlEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape15()
        {
            Test(@"@""\cA""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken>A</TextToken>
      </ControlEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape16()
        {
            Test(@"@""\cz""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken>z</TextToken>
      </ControlEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestControlEscape17()
        {
            Test(@"@""\cZ""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ControlEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>c</TextToken>
        <TextToken>Z</TextToken>
      </ControlEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnknownEscape1()
        {
            Test(@"@""\m""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>m</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \m"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape1()
        {
            Test(@"@""\x""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken />
      </HexEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape2()
        {
            Test(@"@""\x """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken />
      </HexEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape3()
        {
            Test(@"@""\x0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken>0</TextToken>
      </HexEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape4()
        {
            Test(@"@""\x0 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken>0</TextToken>
      </HexEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape5()
        {
            Test(@"@""\x00""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken>00</TextToken>
      </HexEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape6()
        {
            Test(@"@""\x00 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken>00</TextToken>
      </HexEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape7()
        {
            Test(@"@""\x000""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken>00</TextToken>
      </HexEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape8()
        {
            Test(@"@""\xff""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken>ff</TextToken>
      </HexEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape9()
        {
            Test(@"@""\xFF""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken>FF</TextToken>
      </HexEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape10()
        {
            Test(@"@""\xfF""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken>fF</TextToken>
      </HexEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape11()
        {
            Test(@"@""\xfff""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken>ff</TextToken>
      </HexEscape>
      <Text>
        <TextToken>f</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestHexEscape12()
        {
            Test(@"@""\xgg""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <HexEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>x</TextToken>
        <TextToken />
      </HexEscape>
      <Text>
        <TextToken>g</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnknownEscape2()
        {
            Test(@"@""\m """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>m</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \m"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnicodeEscape1()
        {
            Test(@"@""\u""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <UnicodeEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>u</TextToken>
        <TextToken />
      </UnicodeEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnicodeEscape2()
        {
            Test(@"@""\u0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <UnicodeEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>u</TextToken>
        <TextToken>0</TextToken>
      </UnicodeEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnicodeEscape3()
        {
            Test(@"@""\u00""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <UnicodeEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>u</TextToken>
        <TextToken>00</TextToken>
      </UnicodeEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""4"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnicodeEscape4()
        {
            Test(@"@""\u000""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <UnicodeEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>u</TextToken>
        <TextToken>000</TextToken>
      </UnicodeEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""5"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnicodeEscape5()
        {
            Test(@"@""\u0000""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <UnicodeEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>u</TextToken>
        <TextToken>0000</TextToken>
      </UnicodeEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnicodeEscape6()
        {
            Test(@"@""\u0000 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <UnicodeEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>u</TextToken>
        <TextToken>0000</TextToken>
      </UnicodeEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnicodeEscape7()
        {
            Test(@"@""\u """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <UnicodeEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>u</TextToken>
        <TextToken />
      </UnicodeEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnicodeEscape8()
        {
            Test(@"@""\u0 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <UnicodeEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>u</TextToken>
        <TextToken>0</TextToken>
      </UnicodeEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestUnicodeEscape9()
        {
            Test(@"@""\ugggg""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <UnicodeEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>u</TextToken>
        <TextToken />
      </UnicodeEscape>
      <Text>
        <TextToken>g</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
      <Text>
        <TextToken>g</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOctalEscape1()
        {
            Test(@"@""\0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>0</TextToken>
      </OctalEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOctalEscape2()
        {
            Test(@"@""\0 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>0</TextToken>
      </OctalEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOctalEscape3()
        {
            Test(@"@""\00""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>00</TextToken>
      </OctalEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOctalEscape4()
        {
            Test(@"@""\00 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>00</TextToken>
      </OctalEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOctalEscape5()
        {
            Test(@"@""\000""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>000</TextToken>
      </OctalEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOctalEscape6()
        {
            Test(@"@""\000 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>000</TextToken>
      </OctalEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOctalEscape7()
        {
            Test(@"@""\0000""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>000</TextToken>
      </OctalEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOctalEscape8()
        {
            Test(@"@""\0000 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>000</TextToken>
      </OctalEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }
        
        [Fact]
        public void TestOctalEscape9()
        {
            Test(@"@""\7""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""7"">7</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 7"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOctalEscape10()
        {
            Test(@"@""\78""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>7</TextToken>
      </OctalEscape>
      <Text>
        <TextToken>8</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOctalEscape11()
        {
            Test(@"@""\8""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""8"">8</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 8"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestOctalEscapeEcmascript1()
        {
            Test(@"@""\40""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>40</TextToken>
      </OctalEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestOctalEscapeEcmascript2()
        {
            Test(@"@""\401""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>40</TextToken>
      </OctalEscape>
      <Text>
        <TextToken>1</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestOctalEscapeEcmascript3()
        {
            Test(@"@""\37""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>37</TextToken>
      </OctalEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestOctalEscapeEcmascript4()
        {
            Test(@"@""\371""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>371</TextToken>
      </OctalEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestOctalEscapeEcmascript5()
        {
            Test(@"@""\0000""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>000</TextToken>
      </OctalEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

#region KCapture

        [Fact]
        public void TestKCaptureEscape1()
        {
            Test(@"@""\k""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureEscape2()
        {
            Test(@"@""\k """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureEscape3()
        {
            Test(@"@""\k<""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureEscape4()
        {
            Test(@"@""\k< """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \k"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureEscape5()
        {
            Test(@"@""\k<0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \k"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureEscape6()
        {
            Test(@"@""\k<0 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \k"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureEscape7()
        {
            Test(@"@""\k<0>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureEscape8()
        {
            Test(@"@""\k<0> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureEscape9()
        {
            Test(@"@""\k<00> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">00</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureEscape10()
        {
            Test(@"@""\k<a> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group name a"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureEscape11()
        {
            Test(@"@""(?<a>)\k<a> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureEcmaEscape1()
        {
            Test(@"@""\k""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestKCaptureEcmaEscape2()
        {
            Test(@"@""\k """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestKCaptureEcmaEscape3()
        {
            Test(@"@""\k<""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestKCaptureEcmaEscape4()
        {
            Test(@"@""\k< """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestKCaptureEcmaEscape5()
        {
            Test(@"@""\k<0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestKCaptureEcmaEscape6()
        {
            Test(@"@""\k<0 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestKCaptureEcmaEscape7()
        {
            Test(@"@""\k<0>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestKCaptureEcmaEscape8()
        {
            Test(@"@""\k<0> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </KCaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestKCaptureQuoteEscape3()
        {
            Test(@"@""\k'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>'</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Malformed \k&lt;...&gt; named back reference"" Start=""10"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureQuoteEscape4()
        {
            Test(@"@""\k' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>'</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \k"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureQuoteEscape5()
        {
            Test(@"@""\k'0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>'</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \k"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureQuoteEscape6()
        {
            Test(@"@""\k'0 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>'</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \k"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureQuoteEscape7()
        {
            Test(@"@""\k'0'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <QuoteToken>'</QuoteToken>
        <NumberToken value=""0"">0</NumberToken>
        <QuoteToken>'</QuoteToken>
      </KCaptureEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureQuoteEscape8()
        {
            Test(@"@""\k'0' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <QuoteToken>'</QuoteToken>
        <NumberToken value=""0"">0</NumberToken>
        <QuoteToken>'</QuoteToken>
      </KCaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureQuoteEscape9()
        {
            Test(@"@""\k'00' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <QuoteToken>'</QuoteToken>
        <NumberToken value=""0"">00</NumberToken>
        <QuoteToken>'</QuoteToken>
      </KCaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureQuoteEscape10()
        {
            Test(@"@""\k'a' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <QuoteToken>'</QuoteToken>
      </KCaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group name a"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureQuoteEscape11()
        {
            Test(@"@""(?<a>)\k'a' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <KCaptureEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <QuoteToken>'</QuoteToken>
      </KCaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureWrongQuote1()
        {
            Test(@"@""\k<0' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>&lt;</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>'</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \k"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestKCaptureWrongQuote2()
        {
            Test(@"@""\k'0> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>k</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>'</TextToken>
      </Text>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>&gt;</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized escape sequence \k"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

#endregion

#region CaptureEscape

        [Fact]
        public void TestCaptureEscape1()
        {
            Test(@"@""\""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken />
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Illegal \ at end of pattern"" Start=""10"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureEscape2()
        {
            Test(@"@""\ """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken> </TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureEscape3()
        {
            Test(@"@""\<""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>&lt;</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureEscape4()
        {
            Test(@"@""\< """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>&lt;</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureEscape5()
        {
            Test(@"@""\<0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>&lt;</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureEscape6()
        {
            Test(@"@""\<0 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>&lt;</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureEscape7()
        {
            Test(@"@""\<0>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </CaptureEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureEscape8()
        {
            Test(@"@""\<0> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </CaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureEscape9()
        {
            Test(@"@""\<00> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">00</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </CaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureEscape10()
        {
            Test(@"@""\<a> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </CaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group name a"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureEscape11()
        {
            Test(@"@""(?<a>)\<a> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </CaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureEcmaEscape1()
        {
            Test(@"@""\""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken />
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Illegal \ at end of pattern"" Start=""10"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestCaptureEcmaEscape2()
        {
            Test(@"@""\ """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken> </TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestCaptureEcmaEscape3()
        {
            Test(@"@""\<""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>&lt;</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestCaptureEcmaEscape4()
        {
            Test(@"@""\< """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>&lt;</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestCaptureEcmaEscape5()
        {
            Test(@"@""\<0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>&lt;</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestCaptureEcmaEscape6()
        {
            Test(@"@""\<0 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>&lt;</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestCaptureEcmaEscape7()
        {
            Test(@"@""\<0>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </CaptureEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestCaptureEcmaEscape8()
        {
            Test(@"@""\<0> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
      </CaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestCaptureQuoteEscape3()
        {
            Test(@"@""\'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>'</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureQuoteEscape4()
        {
            Test(@"@""\' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>'</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureQuoteEscape5()
        {
            Test(@"@""\'0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>'</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureQuoteEscape6()
        {
            Test(@"@""\'0 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>'</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureQuoteEscape7()
        {
            Test(@"@""\'0'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <QuoteToken>'</QuoteToken>
        <NumberToken value=""0"">0</NumberToken>
        <QuoteToken>'</QuoteToken>
      </CaptureEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureQuoteEscape8()
        {
            Test(@"@""\'0' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <QuoteToken>'</QuoteToken>
        <NumberToken value=""0"">0</NumberToken>
        <QuoteToken>'</QuoteToken>
      </CaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureQuoteEscape9()
        {
            Test(@"@""\'00' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <QuoteToken>'</QuoteToken>
        <NumberToken value=""0"">00</NumberToken>
        <QuoteToken>'</QuoteToken>
      </CaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureQuoteEscape10()
        {
            Test(@"@""\'a' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <QuoteToken>'</QuoteToken>
      </CaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group name a"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureQuoteEscape11()
        {
            Test(@"@""(?<a>)\'a' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <CaptureEscape>
        <BackslashToken>\</BackslashToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <QuoteToken>'</QuoteToken>
      </CaptureEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureWrongQuote1()
        {
            Test(@"@""\<0' """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>&lt;</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>'</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptureWrongQuote2()
        {
            Test(@"@""\'0> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>'</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken>&gt;</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

#endregion

        [Fact]
        public void TestDefinedCategoryEscape()
        {
            Test(@"""\\p{Cc}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CategoryEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
        <OpenBraceToken>{</OpenBraceToken>
        <EscapeCategoryToken>Cc</EscapeCategoryToken>
        <CloseBraceToken>}</CloseBraceToken>
      </CategoryEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestDefinedCategoryEscapeWithSpaces1()
        {
            Test(@"""\\p{ Cc }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <Text>
        <TextToken>C</TextToken>
      </Text>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""9"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestDefinedCategoryEscapeWithSpaces2()
        {
            Test(@"""\\p{ Cc }""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>C</TextToken>
      </Text>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""9"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestDefinedCategoryEscapeWithSpaces3()
        {
            Test(@"""\\p {Cc}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>{</TextToken>
      </Text>
      <Text>
        <TextToken>C</TextToken>
      </Text>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Malformed \p{X} character escape"" Start=""9"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestUndefinedCategoryEscape()
        {
            Test(@"""\\p{xxx}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CategoryEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
        <OpenBraceToken>{</OpenBraceToken>
        <EscapeCategoryToken>xxx</EscapeCategoryToken>
        <CloseBraceToken>}</CloseBraceToken>
      </CategoryEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unknown property 'xxx'"" Start=""13"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestTooShortCategoryEscape1()
        {
            Test(@"""\\p""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
      </SimpleEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""9"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestTooShortCategoryEscape2()
        {
            Test(@"""\\p{""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>{</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""9"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestTooShortCategoryEscape3()
        {
            Test(@"""\\p{}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""9"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestTooShortCategoryEscape4()
        {
            Test(@"""\\p{} """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unknown property"" Start=""9"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestTooShortCategoryEscape5()
        {
            Test(@"""\\p {} """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>}</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Malformed \p{X} character escape"" Start=""9"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestTooShortCategoryEscape6()
        {
            Test(@"""\\p{Cc """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
      </SimpleEscape>
      <Text>
        <TextToken>{</TextToken>
      </Text>
      <Text>
        <TextToken>C</TextToken>
      </Text>
      <Text>
        <TextToken>c</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Incomplete \p{X} character escape"" Start=""9"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCategoryNameWithDash()
        {
            Test(@"""\\p{IsArabicPresentationForms-A}""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CategoryEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>p</TextToken>
        <OpenBraceToken>{</OpenBraceToken>
        <EscapeCategoryToken>IsArabicPresentationForms-A</EscapeCategoryToken>
        <CloseBraceToken>}</CloseBraceToken>
      </CategoryEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNonCapturingGrouping1()
        {
            Test(@"""(?:)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonCapturingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <ColonToken>:</ColonToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </NonCapturingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNonCapturingGrouping2()
        {
            Test(@"""(?:a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonCapturingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <ColonToken>:</ColonToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NonCapturingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNonCapturingGrouping3()
        {
            Test(@"""(?:""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonCapturingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <ColonToken>:</ColonToken>
        <Sequence />
        <CloseParenToken />
      </NonCapturingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""12"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNonCapturingGrouping4()
        {
            Test(@"""(?: """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonCapturingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <ColonToken>:</ColonToken>
        <Sequence />
        <CloseParenToken />
      </NonCapturingGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestPositiveLookaheadGrouping1()
        {
            Test(@"""(?=)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <PositiveLookaheadGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <EqualsToken>=</EqualsToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </PositiveLookaheadGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPositiveLookaheadGrouping2()
        {
            Test(@"""(?=a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <PositiveLookaheadGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <EqualsToken>=</EqualsToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </PositiveLookaheadGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPositiveLookaheadGrouping3()
        {
            Test(@"""(?=""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <PositiveLookaheadGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <EqualsToken>=</EqualsToken>
        <Sequence />
        <CloseParenToken />
      </PositiveLookaheadGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""12"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPositiveLookaheadGrouping4()
        {
            Test(@"""(?= """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <PositiveLookaheadGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <EqualsToken>=</EqualsToken>
        <Sequence />
        <CloseParenToken />
      </PositiveLookaheadGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNegativeLookaheadGrouping1()
        {
            Test(@"""(?!)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegativeLookaheadGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <ExclamationToken>!</ExclamationToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </NegativeLookaheadGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNegativeLookaheadGrouping2()
        {
            Test(@"""(?!a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegativeLookaheadGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <ExclamationToken>!</ExclamationToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NegativeLookaheadGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNegativeLookaheadGrouping3()
        {
            Test(@"""(?!""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegativeLookaheadGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <ExclamationToken>!</ExclamationToken>
        <Sequence />
        <CloseParenToken />
      </NegativeLookaheadGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""12"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNegativeLookaheadGrouping4()
        {
            Test(@"""(?! """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegativeLookaheadGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <ExclamationToken>!</ExclamationToken>
        <Sequence />
        <CloseParenToken />
      </NegativeLookaheadGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNonBacktrackingGrouping1()
        {
            Test(@"""(?>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonBacktrackingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </NonBacktrackingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNonBacktrackingGrouping2()
        {
            Test(@"""(?>a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonBacktrackingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NonBacktrackingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNonBacktrackingGrouping3()
        {
            Test(@"""(?>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonBacktrackingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken />
      </NonBacktrackingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""12"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNonBacktrackingGrouping4()
        {
            Test(@"""(?> """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NonBacktrackingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken />
      </NonBacktrackingGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestPositiveLookbehindGrouping1()
        {
            Test(@"""(?<=)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <PositiveLookbehindGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <EqualsToken>=</EqualsToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </PositiveLookbehindGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPositiveLookbehindGrouping2()
        {
            Test(@"""(?<=a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <PositiveLookbehindGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <EqualsToken>=</EqualsToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </PositiveLookbehindGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPositiveLookbehindGrouping3()
        {
            Test(@"""(?<=""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <PositiveLookbehindGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <EqualsToken>=</EqualsToken>
        <Sequence />
        <CloseParenToken />
      </PositiveLookbehindGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestPositiveLookbehindGrouping4()
        {
            Test(@"""(?<= """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <PositiveLookbehindGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <EqualsToken>=</EqualsToken>
        <Sequence />
        <CloseParenToken />
      </PositiveLookbehindGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNegativeLookbehindGrouping1()
        {
            Test(@"""(?<!)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegativeLookbehindGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <ExclamationToken>!</ExclamationToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </NegativeLookbehindGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNegativeLookbehindGrouping2()
        {
            Test(@"""(?<!a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegativeLookbehindGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <ExclamationToken>!</ExclamationToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NegativeLookbehindGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNegativeLookbehindGrouping3()
        {
            Test(@"""(?<!""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegativeLookbehindGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <ExclamationToken>!</ExclamationToken>
        <Sequence />
        <CloseParenToken />
      </NegativeLookbehindGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNegativeLookbehindGrouping4()
        {
            Test(@"""(?<! """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NegativeLookbehindGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <ExclamationToken>!</ExclamationToken>
        <Sequence />
        <CloseParenToken />
      </NegativeLookbehindGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <WhitespaceTrivia> </WhitespaceTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedCapture1()
        {
            Test(@"""(?<""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <GreaterThanToken />
        <Sequence />
        <CloseParenToken />
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""3"" />
    <Diagnostic Message=""Not enough )'s"" Start=""12"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedCapture2()
        {
            Test(@"""(?<>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken />
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""12"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedCapture3()
        {
            Test(@"""(?<a""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken />
        <Sequence />
        <CloseParenToken />
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""3"" />
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedCapture4()
        {
            Test(@"""(?<a>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken />
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedCapture5()
        {
            Test(@"""(?<a>a""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken />
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""15"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedCapture6()
        {
            Test(@"""(?<a>a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedCapture7()
        {
            Test(@"""(?<a >a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken> </TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNamedCapture8()
        {
            Test(@"""(?<a >a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>&gt;</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedCapture9()
        {
            Test(@"""(?< a>a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken> </TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNamedCapture10()
        {
            Test(@"""(?< a>a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>a</TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedCapture11()
        {
            Test(@"""(?< a >a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken> </TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken> </TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNamedCapture12()
        {
            Test(@"""(?< a >a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>a</TextToken>
          </Text>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>&gt;</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedCapture13()
        {
            Test(@"""(?<ab>a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""ab"">ab</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestZeroNumberCapture()
        {
            Test(@"""(?<0>a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Capture number cannot be zero"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNumericNumberCapture1()
        {
            Test(@"""(?<1>a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNumericNumberCapture2()
        {
            Test(@"""(?<10>a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""10"">10</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNumericNumberCapture3()
        {
            Test(@"""(?<1>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNumericNumberCapture4()
        {
            Test(@"""(?<1> )""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence>
          <Text>
            <TextToken> </TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNumericNumberCapture6()
        {
            Test(@"""(?<1> )""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping1()
        {
            Test(@"""(?<-""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <CaptureNameToken />
        <GreaterThanToken />
        <Sequence />
        <CloseParenToken />
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""13"" Length=""0"" />
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping2()
        {
            Test(@"""(?<-0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken />
        <Sequence />
        <CloseParenToken />
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""3"" />
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping3()
        {
            Test(@"""(?<-0)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken />
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping4()
        {
            Test(@"""(?<-0>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken />
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""15"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping5()
        {
            Test(@"""(?<-0>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping6()
        {
            Test(@"""(?<-0 >)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>&gt;</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping7()
        {
            Test(@"""(?<- 0 >)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <CaptureNameToken />
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>0</TextToken>
          </Text>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>&gt;</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping8()
        {
            Test(@"""(?<- 0>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <CaptureNameToken />
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>0</TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping9()
        {
            Test(@"""(?<-00>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">00</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping10()
        {
            Test(@"""(?<a-""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <CaptureNameToken />
        <GreaterThanToken />
        <Sequence />
        <CloseParenToken />
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""14"" Length=""0"" />
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping11()
        {
            Test(@"""(?<a-0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken />
        <Sequence />
        <CloseParenToken />
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""9"" Length=""3"" />
    <Diagnostic Message=""Not enough )'s"" Start=""15"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping12()
        {
            Test(@"""(?<a-0)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken />
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""15"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping13()
        {
            Test(@"""(?<a-0>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken />
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""16"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping14()
        {
            Test(@"""(?<a-0>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping15()
        {
            Test(@"""(?<a-0 >)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>&gt;</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""15"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping16()
        {
            Test(@"""(?<a- 0 >)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <CaptureNameToken />
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>0</TextToken>
          </Text>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>&gt;</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping17()
        {
            Test(@"""(?<a- 0>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <CaptureNameToken />
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>
              <Trivia>
                <WhitespaceTrivia> </WhitespaceTrivia>
              </Trivia>0</TextToken>
          </Text>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGrouping18()
        {
            Test(@"""(?<a-00>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">00</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingUndefinedReference1()
        {
            Test(@"""(?<-1>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingDefinedReferenceBehind()
        {
            Test(@"""()(?<-1>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingDefinedReferenceAhead()
        {
            Test(@"""(?<-1>)()""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingNamedReferenceBehind()
        {
            Test(@"""(?<a>)(?<-a>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingNamedReferenceAhead()
        {
            Test(@"""(?<-a>)(?<a>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingNumberedReferenceBehind()
        {
            Test(@"""(?<4>)(?<-4>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""4"">4</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""4"">4</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingNumberedReferenceAhead()
        {
            Test(@"""(?<-4>)(?<4>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""4"">4</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <NumberToken value=""4"">4</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingAutoNumberedExists()
        {
            Test(@"""(?<a>)(?<b>)(?<-1>)(?<-2>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""b"">b</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingAutoNumbers()
        {
            Test(@"""()()(?<-0>)(?<-1>)(?<-2>)(?<-3>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""3"">3</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 3"" Start=""38"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingAutoNumbers1()
        {
            Test(@"""()(?<a>)(?<-0>)(?<-1>)(?<-2>)(?<-3>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""3"">3</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 3"" Start=""42"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingAutoNumbers2()
        {
            Test(@"""(?<a>)()(?<-0>)(?<-1>)(?<-2>)(?<-3>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""3"">3</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 3"" Start=""42"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingAutoNumbers3()
        {
            Test(@"""(?<a>)(?<b>)(?<-0>)(?<-1>)(?<-2>)(?<-3>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""b"">b</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""3"">3</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 3"" Start=""46"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingAutoNumbers4()
        {
            Test(@"""(?<-0>)(?<-1>)(?<-2>)(?<-3>)()()""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""3"">3</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 3"" Start=""34"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingAutoNumbers5_1()
        {
            Test(@"""(?<-0>)(?<-1>)(?<-2>)(?<-3>)()(?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""3"">3</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken />
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""27"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 3"" Start=""34"" Length=""1"" />
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""39"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""40"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""41"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        // "(?<-0>)(?<-1>)(?<-2>)(?<-3>)()(?"

        [Fact]
        public void TestBalancingGroupingAutoNumbers5()
        {
            Test(@"""(?<-0>)(?<-1>)(?<-2>)(?<-3>)()(?<a>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""3"">3</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 3"" Start=""34"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingAutoNumbers6()
        {
            Test(@"""(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)()""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""3"">3</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 3"" Start=""34"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingAutoNumbers7_1()
        {
            Test(@"""(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)(?""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""3"">3</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence>
          <Text>
            <TextToken>?</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken />
      </SimpleGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""27"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 3"" Start=""34"" Length=""1"" />
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""43"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""44"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""45"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBalancingGroupingAutoNumbers7()
        {
            Test(@"""(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)(?<b>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""1"">1</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""2"">2</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""3"">3</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""b"">b</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 3"" Start=""34"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestReferenceToBalancingGroupCaptureName1()
        {
            Test(@"""(?<a-0>)(?<b-a>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""b"">b</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestReferenceToBalancingGroupCaptureName2()
        {
            Test(@"""(?<a-0>)(?<-a>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestReferenceToSameBalancingGroup()
        {
            Test(@"""(?<a-a>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestQuoteNamedCapture()
        {
            Test(@"""(?'a')""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <QuoteToken>'</QuoteToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestQuoteBalancingCapture1()
        {
            Test(@"""(?'-0')""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken />
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <QuoteToken>'</QuoteToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestQuoteBalancingCapture2()
        {
            Test(@"""(?'a-0')""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <QuoteToken>'</QuoteToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestMismatchedOpenCloseCapture1()
        {
            Test(@"""(?<a-0')""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <GreaterThanToken />
        <Sequence>
          <Text>
            <TextToken>'</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""15"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestMismatchedOpenCloseCapture2()
        {
            Test(@"""(?'a-0>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BalancingGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <QuoteToken>'</QuoteToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <MinusToken>-</MinusToken>
        <NumberToken value=""0"">0</NumberToken>
        <QuoteToken />
        <Sequence>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </BalancingGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Invalid group name: Group names must begin with a word character"" Start=""15"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture1()
        {
            Test(@"""(?(""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence />
          <CloseParenToken />
        </SimpleGrouping>
        <Sequence />
        <CloseParenToken />
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""12"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture2()
        {
            Test(@"""(?(0""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""0"">0</NumberToken>
        <CloseParenToken />
        <Sequence />
        <CloseParenToken />
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""malformed"" Start=""12"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture3()
        {
            Test(@"""(?(0)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""0"">0</NumberToken>
        <CloseParenToken>)</CloseParenToken>
        <Sequence />
        <CloseParenToken />
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture4()
        {
            Test(@"""(?(0))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""0"">0</NumberToken>
        <CloseParenToken>)</CloseParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture5()
        {
            Test(@"""(?(0)a)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""0"">0</NumberToken>
        <CloseParenToken>)</CloseParenToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture6()
        {
            Test(@"""(?(0)a|)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""0"">0</NumberToken>
        <CloseParenToken>)</CloseParenToken>
        <Alternation>
          <Sequence>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </Sequence>
          <BarToken>|</BarToken>
          <Sequence />
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture7()
        {
            Test(@"""(?(0)a|b)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""0"">0</NumberToken>
        <CloseParenToken>)</CloseParenToken>
        <Alternation>
          <Sequence>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </Sequence>
          <BarToken>|</BarToken>
          <Sequence>
            <Text>
              <TextToken>b</TextToken>
            </Text>
          </Sequence>
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture8()
        {
            Test(@"""(?(0)a|b|)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""0"">0</NumberToken>
        <CloseParenToken>)</CloseParenToken>
        <Alternation>
          <Alternation>
            <Sequence>
              <Text>
                <TextToken>a</TextToken>
              </Text>
            </Sequence>
            <BarToken>|</BarToken>
            <Sequence>
              <Text>
                <TextToken>b</TextToken>
              </Text>
            </Sequence>
          </Alternation>
          <BarToken>|</BarToken>
          <Sequence />
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Too many | in (?()|)"" Start=""17"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture9()
        {
            Test(@"""(?(0)a|b|c)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""0"">0</NumberToken>
        <CloseParenToken>)</CloseParenToken>
        <Alternation>
          <Alternation>
            <Sequence>
              <Text>
                <TextToken>a</TextToken>
              </Text>
            </Sequence>
            <BarToken>|</BarToken>
            <Sequence>
              <Text>
                <TextToken>b</TextToken>
              </Text>
            </Sequence>
          </Alternation>
          <BarToken>|</BarToken>
          <Sequence>
            <Text>
              <TextToken>c</TextToken>
            </Text>
          </Sequence>
        </Alternation>
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Too many | in (?()|)"" Start=""17"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture10()
        {
            Test(@"""(?(0 )""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""0"">0</NumberToken>
        <CloseParenToken />
        <Sequence />
        <CloseParenToken>
          <Trivia>
            <WhitespaceTrivia> </WhitespaceTrivia>
          </Trivia>)</CloseParenToken>
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""malformed"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture11()
        {
            Test(@"""(?(1))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""1"">1</NumberToken>
        <CloseParenToken>)</CloseParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""reference to undefined group"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestConditionalCapture12()
        {
            Test(@"""(?(00))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <NumberToken value=""0"">00</NumberToken>
        <CloseParenToken>)</CloseParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedConditionalCapture1()
        {
            Test(@"""(?(a))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedConditionalCapture2()
        {
            Test(@"""(?<a>)(?(a))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <ConditionalCaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OpenParenToken>(</OpenParenToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <CloseParenToken>)</CloseParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalCaptureGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedConditionalCapture3()
        {
            Test(@"""(?<a>)(?(a ))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken>
            <Trivia>
              <WhitespaceTrivia> </WhitespaceTrivia>
            </Trivia>)</CloseParenToken>
        </SimpleGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestNamedConditionalCapture4()
        {
            Test(@"""(?<a>)(?( a))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CaptureGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <LessThanToken>&lt;</LessThanToken>
        <CaptureNameToken value=""a"">a</CaptureNameToken>
        <GreaterThanToken>&gt;</GreaterThanToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </CaptureGrouping>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <Text>
              <TextToken>
                <Trivia>
                  <WhitespaceTrivia> </WhitespaceTrivia>
                </Trivia>a</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken>)</CloseParenToken>
        </SimpleGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestCaptureInConditionalGrouping1()
        {
            Test(@"""(?(?'""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <QuoteToken>'</QuoteToken>
          <CaptureNameToken />
          <QuoteToken />
          <Sequence />
          <CloseParenToken />
        </CaptureGrouping>
        <Sequence />
        <CloseParenToken />
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions do not capture and cannot be named"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""11"" Length=""3"" />
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestCaptureInConditionalGrouping2()
        {
            Test(@"""(?(?'x'))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <QuoteToken>'</QuoteToken>
          <CaptureNameToken value=""x"">x</CaptureNameToken>
          <QuoteToken>'</QuoteToken>
          <Sequence />
          <CloseParenToken>)</CloseParenToken>
        </CaptureGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions do not capture and cannot be named"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestCommentInConditionalGrouping1()
        {
            Test(@"""(?(?#""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <Text>
              <TextToken>?</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken />
        </SimpleGrouping>
        <Sequence />
        <CloseParenToken />
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <CommentTrivia>#</CommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated (?#...) comment"" Start=""11"" Length=""3"" />
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""11"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""12"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestCommentInConditionalGrouping2()
        {
            Test(@"""(?(?#)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <Text>
              <TextToken>?</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken />
        </SimpleGrouping>
        <Sequence />
        <CloseParenToken />
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <CommentTrivia>#)</CommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions cannot be comments"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""11"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""12"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""15"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestCommentInConditionalGrouping3()
        {
            Test(@"""(?(?#))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <SimpleGrouping>
          <OpenParenToken>(</OpenParenToken>
          <Sequence>
            <Text>
              <TextToken>?</TextToken>
            </Text>
          </Sequence>
          <CloseParenToken />
        </SimpleGrouping>
        <Sequence />
        <CloseParenToken />
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile>
      <Trivia>
        <CommentTrivia>#))</CommentTrivia>
      </Trivia>
    </EndOfFile>
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions cannot be comments"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""11"" Length=""1"" />
    <Diagnostic Message=""Quantifier {x,y} following nothing"" Start=""12"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""16"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestAngleCaptureInConditionalGrouping1()
        {
            Test(@"""(?(?<""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <LessThanToken>&lt;</LessThanToken>
          <CaptureNameToken />
          <GreaterThanToken />
          <Sequence />
          <CloseParenToken />
        </CaptureGrouping>
        <Sequence />
        <CloseParenToken />
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions do not capture and cannot be named"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""11"" Length=""3"" />
    <Diagnostic Message=""Not enough )'s"" Start=""14"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestAngleCaptureInConditionalGrouping2()
        {
            Test(@"""(?(?<a""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <LessThanToken>&lt;</LessThanToken>
          <CaptureNameToken value=""a"">a</CaptureNameToken>
          <GreaterThanToken />
          <Sequence />
          <CloseParenToken />
        </CaptureGrouping>
        <Sequence />
        <CloseParenToken />
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions do not capture and cannot be named"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Unrecognized grouping construct"" Start=""11"" Length=""3"" />
    <Diagnostic Message=""Not enough )'s"" Start=""15"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestAngleCaptureInConditionalGrouping3()
        {
            Test(@"""(?(?<a>""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <LessThanToken>&lt;</LessThanToken>
          <CaptureNameToken value=""a"">a</CaptureNameToken>
          <GreaterThanToken>&gt;</GreaterThanToken>
          <Sequence />
          <CloseParenToken />
        </CaptureGrouping>
        <Sequence />
        <CloseParenToken />
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions do not capture and cannot be named"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""16"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestAngleCaptureInConditionalGrouping4()
        {
            Test(@"""(?(?<a>)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <LessThanToken>&lt;</LessThanToken>
          <CaptureNameToken value=""a"">a</CaptureNameToken>
          <GreaterThanToken>&gt;</GreaterThanToken>
          <Sequence />
          <CloseParenToken>)</CloseParenToken>
        </CaptureGrouping>
        <Sequence />
        <CloseParenToken />
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions do not capture and cannot be named"" Start=""9"" Length=""1"" />
    <Diagnostic Message=""Not enough )'s"" Start=""17"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestAngleCaptureInConditionalGrouping5()
        {
            Test(@"""(?(?<a>))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <CaptureGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <LessThanToken>&lt;</LessThanToken>
          <CaptureNameToken value=""a"">a</CaptureNameToken>
          <GreaterThanToken>&gt;</GreaterThanToken>
          <Sequence />
          <CloseParenToken>)</CloseParenToken>
        </CaptureGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Alternation conditions do not capture and cannot be named"" Start=""9"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestLookbehindAssertionInConditionalGrouping1()
        {
            Test(@"""(?(?<=))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <PositiveLookbehindGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <LessThanToken>&lt;</LessThanToken>
          <EqualsToken>=</EqualsToken>
          <Sequence />
          <CloseParenToken>)</CloseParenToken>
        </PositiveLookbehindGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestLookbehindAssertionInConditionalGrouping2()
        {
            Test(@"""(?(?<!))""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <ConditionalExpressionGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <NegativeLookbehindGrouping>
          <OpenParenToken>(</OpenParenToken>
          <QuestionToken>?</QuestionToken>
          <LessThanToken>&lt;</LessThanToken>
          <ExclamationToken>!</ExclamationToken>
          <Sequence />
          <CloseParenToken>)</CloseParenToken>
        </NegativeLookbehindGrouping>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </ConditionalExpressionGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnorePatternWhitespace);
        }

        [Fact]
        public void TestBackreference1()
        {
            Test(@"@""\1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestBackreference2()
        {
            Test(@"@""\1 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""11"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestBackreference3()
        {
            Test(@"@""()\1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestBackreference4()
        {
            Test(@"@""()\1 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestBackreference5()
        {
            Test(@"@""()\10 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>10</TextToken>
      </OctalEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestEcmascriptBackreference1()
        {
            Test(@"@""\1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>1</TextToken>
      </OctalEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestEcmascriptBackreference2()
        {
            Test(@"@""\1 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <OctalEscape>
        <BackslashToken>\</BackslashToken>
        <TextToken>1</TextToken>
      </OctalEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestEcmascriptBackreference3()
        {
            Test(@"@""()\1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken>1</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }
         
        [Fact]
        public void TestEcmaBackreference4()
        {
            Test(@"@""()\1 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken>1</NumberToken>
      </BackreferenceEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestEcmascriptBackreference5()
        {
            Test(@"@""()\10 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken>1</NumberToken>
      </BackreferenceEscape>
      <Text>
        <TextToken>0</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestEcmascriptBackreference6()
        {
            Test(@"@""()()()()()()()()()()\10 """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken>10</NumberToken>
      </BackreferenceEscape>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ECMAScript);
        }

#region Character Classes

        [Fact]
        public void TestCharacterClass1()
        {
            Test(@"@""[""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence />
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""11"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass2()
        {
            Test(@"@""[ """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken> </TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""12"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass3()
        {
            Test(@"@""[]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>]</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""12"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass4()
        {
            Test(@"@""[] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>]</TextToken>
          </Text>
          <Text>
            <TextToken> </TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass5()
        {
            Test(@"@""[a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass6()
        {
            Test(@"@""[a] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass7()
        {
            Test(@"@""[a-""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken />
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""13"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass8()
        {
            Test(@"@""[a- """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken> </TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""12"" Length=""1"" />
    <Diagnostic Message=""Unterminated [] set"" Start=""14"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass9()
        {
            Test(@"@""[a-]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>-</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass10()
        {
            Test(@"@""[a-] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <Text>
            <TextToken>-</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass11()
        {
            Test(@"@""[a-b]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>b</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass12()
        {
            Test(@"@""[a-b] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>b</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass13()
        {
            Test(@"@""[a-[b]] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>b</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass14()
        {
            Test(@"@""[a-b-[c]] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>b</TextToken>
            </Text>
          </CharacterClassRange>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>c</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass15()
        {
            Test(@"@""[a-[b]-c] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>b</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
          </CharacterClassSubtraction>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>c</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""A subtraction must be the last element in a character class"" Start=""12"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass16()
        {
            Test(@"@""[[a]-b] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>[</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken>-</TextToken>
      </Text>
      <Text>
        <TextToken>b</TextToken>
      </Text>
      <Text>
        <TextToken>]</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass17()
        {
            Test(@"@""[[a]-[b]] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>[</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken>-</TextToken>
      </Text>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>b</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken>]</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass18()
        {
            Test(@"@""[\w-a] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>w</TextToken>
          </CharacterClassEscape>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass19()
        {
            Test(@"@""[a-\w] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <CharacterClassEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>w</TextToken>
            </CharacterClassEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Cannot include class \w in character range"" Start=""13"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass20()
        {
            Test(@"@""[\p{llll}-a] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>llll</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unknown property 'llll'"" Start=""14"" Length=""4"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass21()
        {
            Test(@"@""[\p{Lu}-a] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>Lu</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass22()
        {
            Test(@"@""[a-\p{Lu}] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <CategoryEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>p</TextToken>
              <OpenBraceToken>{</OpenBraceToken>
              <EscapeCategoryToken>Lu</EscapeCategoryToken>
              <CloseBraceToken>}</CloseBraceToken>
            </CategoryEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Cannot include class \p in character range"" Start=""13"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass23()
        {
            Test(@"@""[a-[:Ll:]] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>:</TextToken>
                </Text>
                <Text>
                  <TextToken>L</TextToken>
                </Text>
                <Text>
                  <TextToken>l</TextToken>
                </Text>
                <Text>
                  <TextToken>:</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass24()
        {
            Test(@"@""[a-[:Ll]] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>:</TextToken>
                </Text>
                <Text>
                  <TextToken>L</TextToken>
                </Text>
                <Text>
                  <TextToken>l</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass25()
        {
            Test(@"@""[a-[:""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>:</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken />
            </CharacterClass>
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""15"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass26()
        {
            Test(@"@""[a-[:L""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>:</TextToken>
                </Text>
                <Text>
                  <TextToken>L</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken />
            </CharacterClass>
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""16"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass27()
        {
            Test(@"@""[a-[:L:""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>:</TextToken>
                </Text>
                <Text>
                  <TextToken>L</TextToken>
                </Text>
                <Text>
                  <TextToken>:</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken />
            </CharacterClass>
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""17"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass28()
        {
            Test(@"@""[a-[:L:]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>:</TextToken>
                </Text>
                <Text>
                  <TextToken>L</TextToken>
                </Text>
                <Text>
                  <TextToken>:</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""18"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass29()
        {
            Test(@"@""[\-]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>-</TextToken>
          </SimpleEscape>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass30()
        {
            Test(@"@""[a-b-c] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>b</TextToken>
            </Text>
          </CharacterClassRange>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>c</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass31()
        {
            Test(@"@""[-b-c] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <CharacterClassRange>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>c</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass32()
        {
            Test(@"@""[-[b] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>[</TextToken>
          </Text>
          <Text>
            <TextToken>b</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass33()
        {
            Test(@"@""[-[b]] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>[</TextToken>
          </Text>
          <Text>
            <TextToken>b</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken>]</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass34()
        {
            Test(@"@""[--b """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>-</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>b</TextToken>
            </Text>
          </CharacterClassRange>
          <Text>
            <TextToken> </TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""15"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass35()
        {
            Test(@"@""[--b] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>-</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>b</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass36()
        {
            Test(@"@""[--[b """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>b</TextToken>
                </Text>
                <Text>
                  <TextToken> </TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken />
            </CharacterClass>
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unterminated [] set"" Start=""16"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass37()
        {
            Test(@"@""[--[b] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>b</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
          </CharacterClassSubtraction>
          <Text>
            <TextToken> </TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""A subtraction must be the last element in a character class"" Start=""12"" Length=""0"" />
    <Diagnostic Message=""Unterminated [] set"" Start=""17"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass38()
        {
            Test(@"@""[--[b]] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>b</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass39()
        {
            Test(@"@""[a--[b """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>-</TextToken>
            </Text>
          </CharacterClassRange>
          <Text>
            <TextToken>[</TextToken>
          </Text>
          <Text>
            <TextToken>b</TextToken>
          </Text>
          <Text>
            <TextToken> </TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken />
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""12"" Length=""1"" />
    <Diagnostic Message=""Unterminated [] set"" Start=""17"" Length=""0"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass40()
        {
            Test(@"@""[,--[a] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>,</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>-</TextToken>
            </Text>
          </CharacterClassRange>
          <Text>
            <TextToken>[</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass41()
        {
            Test(@"@""[,--[a]] """, @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>,</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>-</TextToken>
            </Text>
          </CharacterClassRange>
          <Text>
            <TextToken>[</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
      <Text>
        <TextToken>]</TextToken>
      </Text>
      <Text>
        <TextToken> </TextToken>
      </Text>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass42()
        {
            Test(@"@""[\s-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>s</TextToken>
          </CharacterClassEscape>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClass43()
        {
            Test(@"@""[\p{Lu}-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CategoryEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>p</TextToken>
            <OpenBraceToken>{</OpenBraceToken>
            <EscapeCategoryToken>Lu</EscapeCategoryToken>
            <CloseBraceToken>}</CloseBraceToken>
          </CategoryEscape>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestNegatedCharacterClass1()
        {
            Test(@"@""[a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange1()
        {
            Test(@"@""[\c<-\c>]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <ControlEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>c</TextToken>
            <TextToken />
          </ControlEscape>
          <CharacterClassRange>
            <Text>
              <TextToken>&lt;</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken />
            </ControlEscape>
          </CharacterClassRange>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""13"" Length=""1"" />
    <Diagnostic Message=""Unrecognized control character"" Start=""17"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange2()
        {
            Test(@"@""[\c>-\c<]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <ControlEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>c</TextToken>
            <TextToken />
          </ControlEscape>
          <CharacterClassRange>
            <Text>
              <TextToken>&gt;</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken />
            </ControlEscape>
          </CharacterClassRange>
          <Text>
            <TextToken>&lt;</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""13"" Length=""1"" />
    <Diagnostic Message=""Unrecognized control character"" Start=""17"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange3()
        {
            Test(@"@""[\c>-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <ControlEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>c</TextToken>
            <TextToken />
          </ControlEscape>
          <CharacterClassRange>
            <Text>
              <TextToken>&gt;</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange4()
        {
            Test(@"@""[a-\c>]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken />
            </ControlEscape>
          </CharacterClassRange>
          <Text>
            <TextToken>&gt;</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Unrecognized control character"" Start=""15"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange5()
        {
            Test(@"@""[a--]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>-</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange6()
        {
            Test(@"@""[--a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>-</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange7()
        {
            Test(@"@""[a-\-]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Sequence>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>-</TextToken>
              </SimpleEscape>
            </Sequence>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange8()
        {
            Test(@"@""[\--a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>-</TextToken>
          </SimpleEscape>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange9()
        {
            Test(@"@""[\0-\1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>0</TextToken>
            </OctalEscape>
            <MinusToken>-</MinusToken>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>1</TextToken>
            </OctalEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange10()
        {
            Test(@"@""[\1-\0]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>1</TextToken>
            </OctalEscape>
            <MinusToken>-</MinusToken>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>0</TextToken>
            </OctalEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange11()
        {
            Test(@"@""[\0-\01]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>0</TextToken>
            </OctalEscape>
            <MinusToken>-</MinusToken>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>01</TextToken>
            </OctalEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange12()
        {
            Test(@"@""[\01-\0]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>01</TextToken>
            </OctalEscape>
            <MinusToken>-</MinusToken>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>0</TextToken>
            </OctalEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange13()
        {
            Test(@"@""[[:x:]-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <PosixProperty>
              <TextToken>[:x:]</TextToken>
            </PosixProperty>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange14()
        {
            Test(@"@""[a-[:x:]]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <Text>
            <TextToken>a</TextToken>
          </Text>
          <CharacterClassSubtraction>
            <MinusToken>-</MinusToken>
            <CharacterClass>
              <OpenBracketToken>[</OpenBracketToken>
              <Sequence>
                <Text>
                  <TextToken>:</TextToken>
                </Text>
                <Text>
                  <TextToken>x</TextToken>
                </Text>
                <Text>
                  <TextToken>:</TextToken>
                </Text>
              </Sequence>
              <CloseBracketToken>]</CloseBracketToken>
            </CharacterClass>
          </CharacterClassSubtraction>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange15()
        {
            Test(@"@""[\0-\ca]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>0</TextToken>
            </OctalEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>a</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange16()
        {
            Test(@"@""[\ca-\0]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>a</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>0</TextToken>
            </OctalEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange17()
        {
            Test(@"@""[\ca-\cA]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>a</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>A</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange18()
        {
            Test(@"@""[\cA-\ca]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>A</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>a</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange19()
        {
            Test(@"@""[\u0-\u1]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>0</TextToken>
            </UnicodeEscape>
            <MinusToken>-</MinusToken>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>1</TextToken>
            </UnicodeEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""11"" Length=""3"" />
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""15"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange20()
        {
            Test(@"@""[\u1-\u0]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>1</TextToken>
            </UnicodeEscape>
            <MinusToken>-</MinusToken>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>0</TextToken>
            </UnicodeEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""11"" Length=""3"" />
    <Diagnostic Message=""Insufficient hexadecimal digits"" Start=""15"" Length=""3"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange21()
        {
            Test(@"@""[\u0000-\u0000]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>0000</TextToken>
            </UnicodeEscape>
            <MinusToken>-</MinusToken>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>0000</TextToken>
            </UnicodeEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange22()
        {
            Test(@"@""[\u0000-\u0001]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>0000</TextToken>
            </UnicodeEscape>
            <MinusToken>-</MinusToken>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>0001</TextToken>
            </UnicodeEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange23()
        {
            Test(@"@""[\u0001-\u0000]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>0001</TextToken>
            </UnicodeEscape>
            <MinusToken>-</MinusToken>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>0000</TextToken>
            </UnicodeEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""17"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange24()
        {
            Test(@"@""[\u0001-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>0001</TextToken>
            </UnicodeEscape>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange25()
        {
            Test(@"@""[a-\u0001]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <UnicodeEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>u</TextToken>
              <TextToken>0001</TextToken>
            </UnicodeEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange26()
        {
            Test(@"@""[a-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange27()
        {
            Test(@"@""[a-A]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>A</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange28()
        {
            Test(@"@""[A-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>A</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange29()
        {
            Test(@"@""[a-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnoreCase);
        }

        [Fact]
        public void TestCharacterClassRange30()
        {
            Test(@"@""[a-A]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>A</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.IgnoreCase);
        }

        [Fact]
        public void TestCharacterClassRange31()
        {
            Test(@"@""[A-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>A</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.IgnoreCase);
        }

        [Fact]
        public void TestCharacterClassRange32()
        {
            Test(@"@""[a-\x61]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <HexEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>x</TextToken>
              <TextToken>61</TextToken>
            </HexEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange33()
        {
            Test(@"@""[\x61-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <HexEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>x</TextToken>
              <TextToken>61</TextToken>
            </HexEscape>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange34()
        {
            Test(@"@""[a-\x60]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <HexEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>x</TextToken>
              <TextToken>60</TextToken>
            </HexEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange35()
        {
            Test(@"@""[\x62-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <HexEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>x</TextToken>
              <TextToken>62</TextToken>
            </HexEscape>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""15"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange36()
        {
            Test(@"@""[a-\x62]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <HexEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>x</TextToken>
              <TextToken>62</TextToken>
            </HexEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange37()
        {
            Test(@"@""[\x62-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <HexEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>x</TextToken>
              <TextToken>62</TextToken>
            </HexEscape>
            <MinusToken>-</MinusToken>
            <Text>
              <TextToken>a</TextToken>
            </Text>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""15"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange38()
        {
            Test(@"@""[\3-\cc]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>3</TextToken>
            </OctalEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>c</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange39()
        {
            Test(@"@""[\cc-\3]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>c</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>3</TextToken>
            </OctalEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange40()
        {
            Test(@"@""[\2-\cc]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>2</TextToken>
            </OctalEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>c</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange41()
        {
            Test(@"@""[\cc-\2]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>c</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>2</TextToken>
            </OctalEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange42()
        {
            Test(@"@""[\4-\cc]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>4</TextToken>
            </OctalEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>c</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange43()
        {
            Test(@"@""[\cc-\4]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>c</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <OctalEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>4</TextToken>
            </OctalEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange44()
        {
            Test(@"@""[\ca-\cb]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>a</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>b</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange45()
        {
            Test(@"@""[\ca-\cB]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>a</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>B</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange46()
        {
            Test(@"@""[\cA-\cb]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>A</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>b</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange47()
        {
            Test(@"@""[\cA-\cB]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>A</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>B</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange48()
        {
            Test(@"@""[\cb-\ca]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>b</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>a</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange49()
        {
            Test(@"@""[\cb-\cA]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>b</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>A</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange50()
        {
            Test(@"@""[\cB-\ca]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>B</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>a</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange51()
        {
            Test(@"@""[\cB-\cA]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>B</TextToken>
            </ControlEscape>
            <MinusToken>-</MinusToken>
            <ControlEscape>
              <BackslashToken>\</BackslashToken>
              <TextToken>c</TextToken>
              <TextToken>A</TextToken>
            </ControlEscape>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""14"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange52()
        {
            Test(@"@""[\--a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>-</TextToken>
          </SimpleEscape>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>a</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange53()
        {
            Test(@"@""[\--#]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <SimpleEscape>
            <BackslashToken>\</BackslashToken>
            <TextToken>-</TextToken>
          </SimpleEscape>
          <Text>
            <TextToken>-</TextToken>
          </Text>
          <Text>
            <TextToken>#</TextToken>
          </Text>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange54()
        {
            Test(@"@""[a-\-]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Sequence>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>-</TextToken>
              </SimpleEscape>
            </Sequence>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange55()
        {
            Test(@"@""[a-\-b]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Sequence>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>-</TextToken>
              </SimpleEscape>
              <Text>
                <TextToken>b</TextToken>
              </Text>
            </Sequence>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange56()
        {
            Test(@"@""[a-\-\-b]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Sequence>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>-</TextToken>
              </SimpleEscape>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>-</TextToken>
              </SimpleEscape>
              <Text>
                <TextToken>b</TextToken>
              </Text>
            </Sequence>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange57()
        {
            Test(@"@""[b-\-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Sequence>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>-</TextToken>
              </SimpleEscape>
              <Text>
                <TextToken>a</TextToken>
              </Text>
            </Sequence>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange58()
        {
            Test(@"@""[b-\-\-a]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>b</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Sequence>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>-</TextToken>
              </SimpleEscape>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>-</TextToken>
              </SimpleEscape>
              <Text>
                <TextToken>a</TextToken>
              </Text>
            </Sequence>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""[x-y] range in reverse order"" Start=""12"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange59()
        {
            Test(@"@""[a-\-\D]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Sequence>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>-</TextToken>
              </SimpleEscape>
              <CharacterClassEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>D</TextToken>
              </CharacterClassEscape>
            </Sequence>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Cannot include class \D in character range"" Start=""15"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCharacterClassRange60()
        {
            Test(@"@""[a-\-\-\D]""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <CharacterClass>
        <OpenBracketToken>[</OpenBracketToken>
        <Sequence>
          <CharacterClassRange>
            <Text>
              <TextToken>a</TextToken>
            </Text>
            <MinusToken>-</MinusToken>
            <Sequence>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>-</TextToken>
              </SimpleEscape>
              <SimpleEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>-</TextToken>
              </SimpleEscape>
              <CharacterClassEscape>
                <BackslashToken>\</BackslashToken>
                <TextToken>D</TextToken>
              </CharacterClassEscape>
            </Sequence>
          </CharacterClassRange>
        </Sequence>
        <CloseBracketToken>]</CloseBracketToken>
      </CharacterClass>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Cannot include class \D in character range"" Start=""17"" Length=""2"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

#endregion

        [Fact]
        public void TestCaptures1()
        {
            Test(@"@""()\1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures2()
        {
            Test(@"@""()\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures3()
        {
            Test(@"@""()()\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures4()
        {
            Test(@"@""()\1""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures5()
        {
            Test(@"@""()\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""13"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures6()
        {
            Test(@"@""()()\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""15"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures7()
        {
            Test(@"@""()()(?n)\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures8()
        {
            Test(@"@""()(?n)()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""21"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures9()
        {
            Test(@"@""(?n)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""19"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""21"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures10()
        {
            Test(@"@""()()(?n)\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""19"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""21"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures11()
        {
            Test(@"@""()(?n)()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""19"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""21"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures12()
        {
            Test(@"@""(?n)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""19"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""21"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures13()
        {
            Test(@"@""()()(?-n)\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures14()
        {
            Test(@"@""()(?-n)()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures15()
        {
            Test(@"@""(?-n)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures16()
        {
            Test(@"@""()()(?-n)\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""20"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""22"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures17()
        {
            Test(@"@""()(?-n)()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""22"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures18()
        {
            Test(@"@""(?-n)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures19()
        {
            Test(@"@""()()(?n:\1\2)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <NestedOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence>
          <BackreferenceEscape>
            <BackslashToken>\</BackslashToken>
            <NumberToken value=""1"">1</NumberToken>
          </BackreferenceEscape>
          <BackreferenceEscape>
            <BackslashToken>\</BackslashToken>
            <NumberToken value=""2"">2</NumberToken>
          </BackreferenceEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures20()
        {
            Test(@"@""()()(?n:\1\2)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <NestedOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence>
          <BackreferenceEscape>
            <BackslashToken>\</BackslashToken>
            <NumberToken value=""1"">1</NumberToken>
          </BackreferenceEscape>
          <BackreferenceEscape>
            <BackslashToken>\</BackslashToken>
            <NumberToken value=""2"">2</NumberToken>
          </BackreferenceEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""19"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""21"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures21()
        {
            Test(@"@""()()(?-n:\1\2)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <NestedOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence>
          <BackreferenceEscape>
            <BackslashToken>\</BackslashToken>
            <NumberToken value=""1"">1</NumberToken>
          </BackreferenceEscape>
          <BackreferenceEscape>
            <BackslashToken>\</BackslashToken>
            <NumberToken value=""2"">2</NumberToken>
          </BackreferenceEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures22()
        {
            Test(@"@""()()(?-n:\1\2)""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <NestedOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence>
          <BackreferenceEscape>
            <BackslashToken>\</BackslashToken>
            <NumberToken value=""1"">1</NumberToken>
          </BackreferenceEscape>
          <BackreferenceEscape>
            <BackslashToken>\</BackslashToken>
            <NumberToken value=""2"">2</NumberToken>
          </BackreferenceEscape>
        </Sequence>
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""20"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""22"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures23()
        {
            Test(@"@""(?n:)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NestedOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures24()
        {
            Test(@"@""(?n:)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NestedOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""20"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""22"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures25()
        {
            Test(@"@""(?-n:)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NestedOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures26()
        {
            Test(@"@""(?-n:)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <NestedOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <ColonToken>:</ColonToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </NestedOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""21"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""23"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures27()
        {
            Test(@"@""(?n)(?-n)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures28()
        {
            Test(@"@""(?n)(?-n)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestCaptures29()
        {
            Test(@"@""(?-n)(?n)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""24"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""26"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.None);
        }

        [Fact]
        public void TestCaptures30()
        {
            Test(@"@""(?-n)(?n)()()\1\2""", @"<Tree>
  <CompilationUnit>
    <Sequence>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>-n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleOptionsGrouping>
        <OpenParenToken>(</OpenParenToken>
        <QuestionToken>?</QuestionToken>
        <OptionsToken>n</OptionsToken>
        <CloseParenToken>)</CloseParenToken>
      </SimpleOptionsGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <SimpleGrouping>
        <OpenParenToken>(</OpenParenToken>
        <Sequence />
        <CloseParenToken>)</CloseParenToken>
      </SimpleGrouping>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""1"">1</NumberToken>
      </BackreferenceEscape>
      <BackreferenceEscape>
        <BackslashToken>\</BackslashToken>
        <NumberToken value=""2"">2</NumberToken>
      </BackreferenceEscape>
    </Sequence>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Reference to undefined group number 1"" Start=""24"" Length=""1"" />
    <Diagnostic Message=""Reference to undefined group number 2"" Start=""26"" Length=""1"" />
  </Diagnostics>
</Tree>", RegexOptions.ExplicitCapture);
        }

        [Fact]
        public void TestDeepRecursion()
        {
            var (token, tree, chars) =
                JustParseTree(
@"@""((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((""", RegexOptions.None, conversionFailureOk: false);
            Assert.False(token.IsMissing);
            Assert.False(chars.IsDefaultOrEmpty);
            Assert.Null(tree);
        }
    }
}
