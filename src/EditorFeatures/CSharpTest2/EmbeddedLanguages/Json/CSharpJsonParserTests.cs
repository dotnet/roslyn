// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.EmbeddedLanguages.Json;

using JsonSeparatedList = EmbeddedSeparatedSyntaxNodeList<JsonKind, JsonNode, JsonValueNode>;
using JsonToken = EmbeddedSyntaxToken<JsonKind>;
using JsonTrivia = EmbeddedSyntaxTrivia<JsonKind>;

public partial class CSharpJsonParserTests
{
    private const string SupportedLanguage = "en-US";

    private readonly IVirtualCharService _service = CSharpVirtualCharService.Instance;
    private const string StatementPrefix = "var v = ";

    private static SyntaxToken GetStringToken(string text)
    {
        var statement = StatementPrefix + text;
        var parsedStatement = SyntaxFactory.ParseStatement(statement);
        var token = parsedStatement.DescendantTokens().ToArray()[3];
        Assert.True(token.Kind() == SyntaxKind.StringLiteralToken);

        return token;
    }

    protected void Test(
        string stringText,
        string? expected,
        string looseDiagnostics,
        string strictDiagnostics,
        bool runLooseSubTreeCheck = true)
    {
        Test(stringText, JsonOptions.Loose, expected, looseDiagnostics, runLooseSubTreeCheck);
        Test(stringText, JsonOptions.Strict, expected, strictDiagnostics, runSubTreeChecks: true);
    }

    private void Test(
        string stringText, JsonOptions options,
        string? expectedTree, string expectedDiagnostics,
        bool runSubTreeChecks)
    {
        var tree = TryParseTree(stringText, options, conversionFailureOk: false);
        if (tree == null)
        {
            Assert.Null(expectedTree);
            return;
        }

        Assert.NotNull(expectedTree);

        // Tests are allowed to not run the subtree tests.  This is because some
        // subtrees can cause the native regex parser to exhibit very bad behavior
        // (like not ever actually finishing compiling).
        if (runSubTreeChecks)
            TryParseSubTrees(stringText, options);

        var actualTree = TreeToText(tree).Replace("""
            "
            """, """
            ""
            """);
        AssertEx.Equal(expectedTree.Replace("""
            "
            """, """
            ""
            """), actualTree);

        ValidateDiagnostics(expectedDiagnostics, tree);
    }

    private protected static void ValidateDiagnostics(string expectedDiagnostics, JsonTree tree)
    {
        var actualDiagnostics = DiagnosticsToText(tree.Diagnostics).Replace("""
            "
            """, """
            ""
            """);
        AssertEx.Equal(RemoveMessagesInNonSupportedLanguage(expectedDiagnostics).Replace("""
            "
            """, """
            ""
            """), actualDiagnostics);
    }

    private static string RemoveMessagesInNonSupportedLanguage(string value)
    {
        if (value == "")
            return value;

        if (Thread.CurrentThread.CurrentCulture.Name == SupportedLanguage)
            return value;

        var diagnosticsElement = XElement.Parse(value);
        foreach (var diagnosticElement in diagnosticsElement.Elements("Diagnostic"))
            diagnosticElement.Attribute("Message")!.Remove();

        return diagnosticsElement.ToString();
    }

    private void TryParseSubTrees(string stringText, JsonOptions options)
    {
        // Trim the input from the right and make sure tree invariants hold
        var current = stringText;
        while (current != """
            @""
            """ && current != """
            ""
            """)
        {
            current = current[0..^2] + """
                "
                """;
            TryParseTree(current, options, conversionFailureOk: true);
        }

        // Trim the input from the left and make sure tree invariants hold
        current = stringText;
        while (current != """
            @""
            """ && current != """
            ""
            """)
        {
            current = current[0] == '@'
                ? """
                @"
                """ + current[3..]
                : """
                "
                """ + current[2..];

            TryParseTree(current, options, conversionFailureOk: true);
        }

        for (var start = stringText[0] == '@' ? 2 : 1; start < stringText.Length - 1; start++)
        {
            TryParseTree(
                stringText[..start] + stringText[(start + 1)..],
                options, conversionFailureOk: true);
        }
    }

    private protected (SyntaxToken, JsonTree?, VirtualCharSequence) JustParseTree(
        string stringText, JsonOptions options, bool conversionFailureOk)
    {
        var token = GetStringToken(stringText);
        if (token.ValueText == "")
            return default;

        var allChars = _service.TryConvertToVirtualChars(token);
        if (allChars.IsDefault)
        {
            Assert.True(conversionFailureOk, "Failed to convert text to token.");
            return (token, null, allChars);
        }

        var tree = JsonParser.TryParse(allChars, options);
        return (token, tree, allChars);
    }

    private JsonTree? TryParseTree(
        string stringText, JsonOptions options, bool conversionFailureOk)
    {
        var (token, tree, allChars) = JustParseTree(stringText, options, conversionFailureOk);
        if (tree == null)
        {
            Assert.True(allChars.IsDefault);
            return null;
        }

        CheckInvariants(tree, allChars);

        if (options == JsonOptions.Loose)
        {
            try
            {
                JToken.Parse(token.ValueText);
            }
            catch (Exception)
            {
                Assert.NotEmpty(tree.Diagnostics);
                return tree;
            }
        }
        else
        {
            try
            {
                JsonDocument.Parse(token.ValueText, new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
            }
            catch (Exception)
            {
                Assert.NotEmpty(tree.Diagnostics);
                return tree;
            }
        }

        Assert.Empty(tree.Diagnostics);
        return tree;
    }

    private protected static string TreeToText(JsonTree tree)
        => new XElement("Tree",
            NodeToElement(tree.Root)).ToString();

    private protected static string DiagnosticsToText(ImmutableArray<EmbeddedDiagnostic> diagnostics)
    {
        if (diagnostics.IsEmpty)
            return "";

        return new XElement("Diagnostics",
            diagnostics.Select(d =>
            {
                var element = new XElement("Diagnostic");
                // Ensure the diagnostic we emit is the same as the .NET one. Note: we can only
                // do this in en-US as that's the only culture where we control the text exactly
                // and can ensure it exactly matches Regex.  We depend on localization to do a 
                // good enough job here for other languages.
                if (Thread.CurrentThread.CurrentCulture.Name == SupportedLanguage)
                    element.Add(new XAttribute("Message", d.Message));

                element.Add(new XAttribute("Start", d.Span.Start));
                element.Add(new XAttribute("Length", d.Span.Length));

                return element;
            })).ToString();
    }

    private static XElement NodeToElement(JsonNode node)
        => node switch
        {
            JsonArrayNode arrayNode => ArrayNodeToElement(arrayNode),
            JsonCompilationUnit compilationUnit => CompilationUnitToElement(compilationUnit),
            JsonObjectNode objectNode => ObjectNodeToElement(objectNode),
            JsonConstructorNode constructorNode => ConstructorNodeToElement(constructorNode),
            _ => NodeToElementWorker(node),
        };

    private static XElement NodeToElementWorker(JsonNode node)
    {
        var element = new XElement(node.Kind.ToString());
        foreach (var child in node)
            element.Add(NodeOrTokenToElement(child));

        return element;
    }

    private static XElement NodeOrTokenToElement(EmbeddedSyntaxNodeOrToken<JsonKind, JsonNode> child)
        => child.IsNode ? NodeToElement(child.Node) : TokenToElement(child.Token);

    private static XElement ConstructorNodeToElement(JsonConstructorNode node)
        => new(
            node.Kind.ToString(),
            TokenToElement(node.NewKeyword),
            TokenToElement(node.NameToken),
            TokenToElement(node.OpenParenToken),
            CreateSequenceNode(node.Sequence),
            TokenToElement(node.CloseParenToken));

    private static XElement ObjectNodeToElement(JsonObjectNode node)
        => new(
            node.Kind.ToString(),
            TokenToElement(node.OpenBraceToken),
            CreateSequenceNode(node.Sequence),
            TokenToElement(node.CloseBraceToken));

    private static XElement CompilationUnitToElement(JsonCompilationUnit node)
        => new(
            node.Kind.ToString(),
            CreateSequenceNode(node.Sequence),
            TokenToElement(node.EndOfFileToken));

    private static XElement ArrayNodeToElement(JsonArrayNode node)
        => new(
            node.Kind.ToString(),
            TokenToElement(node.OpenBracketToken),
            CreateSequenceNode(node.Sequence),
            TokenToElement(node.CloseBracketToken));

    private static XElement CreateSequenceNode(ImmutableArray<JsonValueNode> sequence)
        => new("Sequence", sequence.Select(NodeToElement));

    private static XElement CreateSequenceNode(JsonSeparatedList sequence)
        => new("Sequence", sequence.NodesAndTokens.Select(NodeOrTokenToElement));

    private static XElement TokenToElement(JsonToken token)
    {
        var element = new XElement(token.Kind.ToString());

        if (token.Value != null)
            element.Add(new XAttribute("value", token.Value));

        if (token.LeadingTrivia.Length > 0)
            element.Add(new XElement("Trivia", token.LeadingTrivia.Select(t => TriviaToElement(t))));

        if (token.VirtualChars.Length > 0)
            element.Add(token.VirtualChars.CreateString());

        if (token.TrailingTrivia.Length > 0)
            element.Add(new XElement("Trivia", token.TrailingTrivia.Select(t => TriviaToElement(t))));

        return element;
    }

    private static XElement TriviaToElement(JsonTrivia trivia)
        => new(
            trivia.Kind.ToString(),
            trivia.VirtualChars.CreateString().Replace("\f", "\\f"));

    private protected static void CheckInvariants(JsonTree tree, VirtualCharSequence allChars)
    {
        var root = tree.Root;
        var position = 0;
        CheckInvariants(root, ref position, allChars);
        Assert.Equal(allChars.Length, position);
    }

    private static void CheckInvariants(JsonNode node, ref int position, VirtualCharSequence allChars)
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

    private static void CheckInvariants(JsonToken token, ref int position, VirtualCharSequence allChars)
    {
        CheckInvariants(token.LeadingTrivia, ref position, allChars);
        CheckCharacters(token.VirtualChars, ref position, allChars);
        CheckInvariants(token.TrailingTrivia, ref position, allChars);
    }

    private static void CheckInvariants(ImmutableArray<JsonTrivia> leadingTrivia, ref int position, VirtualCharSequence allChars)
    {
        foreach (var trivia in leadingTrivia)
            CheckInvariants(trivia, ref position, allChars);
    }

    private static void CheckInvariants(JsonTrivia trivia, ref int position, VirtualCharSequence allChars)
    {
        switch (trivia.Kind)
        {
            case JsonKind.SingleLineCommentTrivia:
            case JsonKind.MultiLineCommentTrivia:
            case JsonKind.WhitespaceTrivia:
            case JsonKind.EndOfLineTrivia:
                break;
            default:
                Assert.False(true, "Incorrect trivia kind");
                return;
        }

        CheckCharacters(trivia.VirtualChars, ref position, allChars);
    }

    private static void CheckCharacters(VirtualCharSequence virtualChars, ref int position, VirtualCharSequence allChars)
    {
        for (var i = 0; i < virtualChars.Length; i++)
            Assert.Equal(allChars[position + i], virtualChars[i]);

        position += virtualChars.Length;
    }

    private object RemoveSequenceNode(XNode node)
    {
        if (node is not XElement element)
            return node;

        var children = element.Nodes().Select(RemoveSequenceNode);

        if (element.Name == "Sequence")
            return children;
        return new XElement(element.Name, children);
    }

    [Fact]
    public void TestDeepRecursion1()
    {
        var (token, tree, chars) =
            JustParseTree(
                """
                @"[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
                [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[["
                """,
                JsonOptions.Loose, conversionFailureOk: false);
        Assert.False(token.IsMissing);
        Assert.False(chars.IsDefaultOrEmpty());
        Assert.Null(tree);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_queries/edit/1691963")]
    public void TestDeepRecursion2()
    {
        var (token, tree, chars) =
            JustParseTree(
                """
                @"::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::"
                """,
                JsonOptions.Loose, conversionFailureOk: false);
        Assert.False(token.IsMissing);
        Assert.False(chars.IsDefaultOrEmpty());
        Assert.Null(tree);
    }
}
