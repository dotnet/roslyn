// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test.Legacy;

public class CSharpCodeParserTest
{
    public static TheoryData InvalidTagHelperPrefixData
    {
        get
        {
            var directiveLocation = new SourceLocation(1, 2, 3);

            RazorDiagnostic InvalidPrefixError(int length, char character, string prefix)
            {
                return RazorDiagnosticFactory.CreateParsing_InvalidTagHelperPrefixValue(
                    new SourceSpan(directiveLocation, length), SyntaxConstants.CSharp.TagHelperPrefixKeyword, character, prefix);
            }

            return new TheoryData<string, SourceLocation, IEnumerable<RazorDiagnostic>>
                {
                    {
                        "th ",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(3, ' ', "th "),
                        }
                    },
                    {
                        "th\t",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(3, '\t', "th\t"),
                        }
                    },
                    {
                        """
                        th

                        """,
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(2 + Environment.NewLine.Length, Environment.NewLine[0], """
                            th

                            """),
                        }
                    },
                    {
                        " th ",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(4, ' ', " th "),
                        }
                    },
                    {
                        "@",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(1, '@', "@"),
                        }
                    },
                    {
                        "t@h",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(3, '@', "t@h"),
                        }
                    },
                    {
                        "!",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(1, '!', "!"),
                        }
                    },
                    {
                        "!th",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(3, '!', "!th"),
                        }
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidTagHelperPrefixData))]
    public void ValidateTagHelperPrefix_ValidatesPrefix(
        string directiveText,
        SourceLocation directiveLocation,
        object expectedErrors)
    {
        // Arrange
        var expectedDiagnostics = (IEnumerable<RazorDiagnostic>)expectedErrors;
        var diagnostics = new List<RazorDiagnostic>();

        // Act
        CSharpCodeParser.ValidateTagHelperPrefix(directiveText, directiveLocation, diagnostics);

        // Assert
        Assert.Equal(expectedDiagnostics, diagnostics);
    }

    [Theory]
    [InlineData("foo,assemblyName")]
    [InlineData("foo, assemblyName")]
    [InlineData("   foo, assemblyName")]
    [InlineData("   foo   , assemblyName")]
    [InlineData("foo,    assemblyName")]
    [InlineData("   foo   ,    assemblyName   ")]
    public void ParseAddOrRemoveDirective_CalculatesAssemblyLocationInLookupText(string text)
    {
        // Arrange
        var directive = new CSharpCodeParser.ParsedDirective()
        {
            DirectiveText = text,
        };

        var diagnostics = new List<RazorDiagnostic>();

        // Act
        var result = CSharpCodeParser.ParseAddOrRemoveDirective(directive, SourceLocation.Zero, diagnostics);

        // Assert
        Assert.Empty(diagnostics);
        Assert.Equal("foo", result.TypePattern);
        Assert.Equal("assemblyName", result.AssemblyName);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("*,", 2)]
    [InlineData("?,", 2)]
    [InlineData(",", 1)]
    [InlineData(",,,", 3)]
    [InlineData("First, ", 7)]
    [InlineData("First , ", 8)]
    [InlineData(" ,Second", 8)]
    [InlineData(" , Second", 9)]
    [InlineData("SomeType,", 9)]
    [InlineData("SomeAssembly", 12)]
    [InlineData("First,Second,Third", 18)]
    public void ParseAddOrRemoveDirective_CreatesErrorIfInvalidLookupText_DoesNotThrow(string directiveText, int errorLength)
    {
        // Arrange
        var directive = new CSharpCodeParser.ParsedDirective()
        {
            DirectiveText = directiveText
        };

        var diagnostics = new List<RazorDiagnostic>();
        var expectedError = RazorDiagnosticFactory.CreateParsing_InvalidTagHelperLookupText(
            new SourceSpan(new SourceLocation(1, 2, 3), errorLength), directiveText);

        // Act
        var result = CSharpCodeParser.ParseAddOrRemoveDirective(directive, new SourceLocation(1, 2, 3), diagnostics);

        // Assert
        Assert.Same(directive, result);

        var error = Assert.Single(diagnostics);
        Assert.Equal(expectedError, error);
    }

    [Fact]
    public void TagHelperPrefixDirective_DuplicatesCauseError()
    {
        // Arrange
        var expectedDiagnostic = RazorDiagnosticFactory.CreateParsing_DuplicateDirective(
            new SourceSpan(
                filePath: null,
                absoluteIndex: 22 + Environment.NewLine.Length,
                lineIndex: 1,
                characterIndex: 0,
                length: 16,
                lineCount: 1,
                endCharacterIndex: 0),
            "tagHelperPrefix");
        var source = TestRazorSourceDocument.Create(
            @"@tagHelperPrefix ""th:""
@tagHelperPrefix ""th""",
            filePath: null);

        // Act
        var document = RazorSyntaxTree.Parse(source);

        // Assert
        var erroredNode = document.Root.DescendantNodes().Last(n => n.GetChunkGenerator() is TagHelperPrefixDirectiveChunkGenerator);
        var chunkGenerator = Assert.IsType<TagHelperPrefixDirectiveChunkGenerator>(erroredNode.GetChunkGenerator());
        var diagnostic = Assert.Single(chunkGenerator.Diagnostics);
        Assert.Equal(expectedDiagnostic, diagnostic);
    }

    [Fact]
    public void MapDirectives_HandlesDuplicates()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create();
        var options = RazorParserOptions.Default;
        using var context = new ParserContext(source, options);

        // Act & Assert (Does not throw)
        ImmutableArray<DirectiveDescriptor> directiveDescriptors = [
            DirectiveDescriptor.CreateDirective("test", DirectiveKind.SingleLine),
            DirectiveDescriptor.CreateDirective("test", DirectiveKind.SingleLine)
        ];

        _ = new CSharpCodeParser(directiveDescriptors, context);
    }
}
