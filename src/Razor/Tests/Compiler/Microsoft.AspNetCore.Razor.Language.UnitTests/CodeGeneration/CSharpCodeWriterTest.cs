// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class CSharpCodeWriterTest
{
    // The length of the newline string written by writer.WriteLine.
    private static readonly int WriterNewLineLength = Environment.NewLine.Length;

    public static IEnumerable<object[]> NewLines
    {
        get
        {
            return new object[][]
            {
                new object[] { "\r" },
                new object[] { "\n" },
                new object[] { "\r\n" },
            };
        }
    }

    [Fact]
    public void CSharpCodeWriter_TracksPosition_WithWrite()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.Write("1234");

        // Assert
        var location = writer.Location;
        var expected = new SourceLocation(absoluteIndex: 4, lineIndex: 0, characterIndex: 4);

        Assert.Equal(expected, location);
    }

    [Fact]
    public void CSharpCodeWriter_TracksPosition_WithIndent()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteLine();
        writer.Indent(size: 3);

        // Assert
        var location = writer.Location;
        var expected = new SourceLocation(absoluteIndex: 3 + WriterNewLineLength, lineIndex: 1, characterIndex: 3);

        Assert.Equal(expected, location);
    }

    [Fact]
    public void CSharpCodeWriter_TracksPosition_WithWriteLine()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteLine("1234");

        // Assert
        var location = writer.Location;

        var expected = new SourceLocation(absoluteIndex: 4 + WriterNewLineLength, lineIndex: 1, characterIndex: 0);

        Assert.Equal(expected, location);
    }

    [Theory]
    [MemberData(nameof(NewLines))]
    public void CSharpCodeWriter_TracksPosition_WithWriteLine_WithNewLineInContent(string newLine)
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteLine("1234" + newLine + "12");

        // Assert
        var location = writer.Location;

        var expected = new SourceLocation(
            absoluteIndex: 6 + newLine.Length + WriterNewLineLength,
            lineIndex: 2,
            characterIndex: 0);

        Assert.Equal(expected, location);
    }

    [Theory]
    [MemberData(nameof(NewLines))]
    public void CSharpCodeWriter_TracksPosition_WithWrite_WithNewlineInContent(string newLine)
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.Write("1234" + newLine + "123" + newLine + "12");

        // Assert
        var location = writer.Location;

        var expected = new SourceLocation(
            absoluteIndex: 9 + newLine.Length + newLine.Length,
            lineIndex: 2,
            characterIndex: 2);

        Assert.Equal(expected, location);
    }

    [Fact]
    public void CSharpCodeWriter_TracksPosition_WithWrite_WithNewlineInContent_RepeatedN()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.Write("1234\n\n123");

        // Assert
        var location = writer.Location;

        var expected = new SourceLocation(
            absoluteIndex: 9,
            lineIndex: 2,
            characterIndex: 3);

        Assert.Equal(expected, location);
    }

    [Fact]
    public void CSharpCodeWriter_TracksPosition_WithWrite_WithMixedNewlineInContent()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.Write("1234\r123\r\n12\n1");

        // Assert
        var location = writer.Location;

        var expected = new SourceLocation(
            absoluteIndex: 14,
            lineIndex: 3,
            characterIndex: 1);

        Assert.Equal(expected, location);
    }

    [Fact]
    public void CSharpCodeWriter_TracksPosition_WithNewline_SplitAcrossWrites()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.Write("1234\r");
        var location1 = writer.Location;

        writer.Write("\n");
        var location2 = writer.Location;

        // Assert
        var expected1 = new SourceLocation(absoluteIndex: 5, lineIndex: 1, characterIndex: 0);
        Assert.Equal(expected1, location1);

        var expected2 = new SourceLocation(absoluteIndex: 6, lineIndex: 1, characterIndex: 0);
        Assert.Equal(expected2, location2);
    }

    [Fact]
    public void CSharpCodeWriter_TracksPosition_WithTwoNewline_SplitAcrossWrites_R()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.Write("1234\r");
        var location1 = writer.Location;

        writer.Write("\r");
        var location2 = writer.Location;

        // Assert
        var expected1 = new SourceLocation(absoluteIndex: 5, lineIndex: 1, characterIndex: 0);
        Assert.Equal(expected1, location1);

        var expected2 = new SourceLocation(absoluteIndex: 6, lineIndex: 2, characterIndex: 0);
        Assert.Equal(expected2, location2);
    }

    [Fact]
    public void CSharpCodeWriter_TracksPosition_WithTwoNewline_SplitAcrossWrites_N()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.Write("1234\n");
        var location1 = writer.Location;

        writer.Write("\n");
        var location2 = writer.Location;

        // Assert
        var expected1 = new SourceLocation(absoluteIndex: 5, lineIndex: 1, characterIndex: 0);
        Assert.Equal(expected1, location1);

        var expected2 = new SourceLocation(absoluteIndex: 6, lineIndex: 2, characterIndex: 0);
        Assert.Equal(expected2, location2);
    }

    [Fact]
    public void CSharpCodeWriter_TracksPosition_WithTwoNewline_SplitAcrossWrites_Reversed()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.Write("1234\n");
        var location1 = writer.Location;

        writer.Write("\r");
        var location2 = writer.Location;

        // Assert
        var expected1 = new SourceLocation(absoluteIndex: 5, lineIndex: 1, characterIndex: 0);
        Assert.Equal(expected1, location1);

        var expected2 = new SourceLocation(absoluteIndex: 6, lineIndex: 2, characterIndex: 0);
        Assert.Equal(expected2, location2);
    }

    [Fact]
    public void CSharpCodeWriter_TracksPosition_WithNewline_SplitAcrossWrites_AtBeginning()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.Write("\r");
        var location1 = writer.Location;

        writer.Write("\n");
        var location2 = writer.Location;

        // Assert
        var expected1 = new SourceLocation(absoluteIndex: 1, lineIndex: 1, characterIndex: 0);
        Assert.Equal(expected1, location1);

        var expected2 = new SourceLocation(absoluteIndex: 2, lineIndex: 1, characterIndex: 0);
        Assert.Equal(expected2, location2);
    }

    [Fact]
    public void CSharpCodeWriter_LinesBreaksOutsideOfContentAreNotCounted()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.Write("\r\nHello\r\nWorld\r\n", startIndex: 2, count: 12);
        var location = writer.Location;

        // Assert
        var expected = new SourceLocation(absoluteIndex: 12, lineIndex: 1, characterIndex: 5);
        Assert.Equal(expected, location);
    }

    [Fact]
    public void WriteLineNumberDirective_UsesFilePath_FromSourceLocation()
    {
        // Arrange
        var filePath = "some-path";
        var mappingLocation = new SourceSpan(filePath, 10, 4, 3, 9);

        using var writer = new CodeWriter();
        var expected = $"#line 5 \"{filePath}\"" + writer.NewLine;

        // Act
        writer.WriteLineNumberDirective(mappingLocation, ensurePathBackslashes: false);
        var code = writer.GetText().ToString();

        // Assert
        Assert.Equal(expected, code);
    }

    [Fact]
    public void WriteField_WritesFieldDeclaration()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteField(
            suppressWarnings: [],
            modifiers: ["private"],
            type: "global::System.String",
            name: "_myString");

        // Assert
        var output = writer.GetText().ToString();
        Assert.Equal("""
            private global::System.String _myString;

            """, output);
    }

    [Fact]
    public void WriteField_WithModifiers_WritesFieldDeclaration()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteField(
            suppressWarnings: [],
            modifiers: ["private", "readonly", "static"],
            type: "global::System.String",
            name: "_myString");

        // Assert
        var output = writer.GetText().ToString();
        Assert.Equal("""
            private readonly static global::System.String _myString;

            """, output);
    }

    [Fact]
    public void WriteField_WithModifiersAndSuppressions_WritesFieldDeclaration()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteField(
            suppressWarnings: ["0001", "0002"],
            modifiers: ["private", "readonly", "static"],
            type: "global::System.String",
            name: "_myString");

        // Assert
        var output = writer.GetText().ToString();
        Assert.Equal("""
            #pragma warning disable 0001
            #pragma warning disable 0002
            private readonly static global::System.String _myString;
            #pragma warning restore 0002
            #pragma warning restore 0001

            """,
            output);
    }

    [Fact]
    public void WriteAutoPropertyDeclaration_WritesPropertyDeclaration()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteAutoPropertyDeclaration(modifiers: ["public" ], type: "global::System.String", name: "MyString");

        // Assert
        var output = writer.GetText().ToString();
        Assert.Equal("""
            public global::System.String MyString { get; set; }

            """, output);
    }

    [Fact]
    public void WriteAutoPropertyDeclaration_WithModifiers_WritesPropertyDeclaration()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteAutoPropertyDeclaration(modifiers: ["public", "static"], type: "global::System.String", name: "MyString");

        // Assert
        var output = writer.GetText().ToString();
        Assert.Equal("""
            public static global::System.String MyString { get; set; }

            """, output);
    }

    [Fact]
    public void CSharpCodeWriter_RespectTabSetting()
    {
        // Arrange
        var options = RazorCodeGenerationOptions.Default
            .WithIndentSize(4)
            .WithFlags(indentWithTabs: true);

        using var writer = new CodeWriter(options);

        // Act
        writer.BuildClassDeclaration(modifiers: [], name: "C", baseType: null, interfaces: [], typeParameters: [], context: null);
        writer.WriteField(suppressWarnings: [], modifiers: [], type: "int", name: "f");

        // Assert
        var output = writer.GetText().ToString();
        Assert.Equal("""
            class C
            {
            	int f;

            """, output);
    }

    [Fact]
    public void CSharpCodeWriter_RespectSpaceSetting()
    {
        // Arrange
        var options = RazorCodeGenerationOptions.Default
            .WithIndentSize(4)
            .WithFlags(indentWithTabs: false);

        using var writer = new CodeWriter(options);

        // Act
        writer.BuildClassDeclaration(modifiers: [], name: "C", baseType: null, interfaces: [], typeParameters: [], context: null);
        writer.WriteField(suppressWarnings: [], modifiers: [], type: "int", name: "f");

        // Assert
        var output = writer.GetText().ToString();
        Assert.Equal("""
            class C
            {
                int f;

            """, output);
    }

    [Fact, WorkItem("https://github.com/dotnet/core/issues/9885")]
    public void AlignedPages_WritesCorrectlyWhenPageAndBufferAreAligned()
    {
        var pages = new LinkedList<ReadOnlyMemory<char>[]>();

        const string FirstLine = "First Line";
        pages.AddLast([(FirstLine + FirstLine).AsMemory(), "Second".AsMemory()]);

        var testReader = CodeWriter.GetTestTextReader(pages);
        var output = new char[FirstLine.Length];

        testReader.Read(output, 0, output.Length);
        Assert.Equal(FirstLine, string.Join("", output));
        Array.Clear(output, 0, output.Length);

        testReader.Read(output, 0, output.Length);
        Assert.Equal(FirstLine, string.Join("", output));
        Array.Clear(output, 0, output.Length);

        testReader.Read(output, 0, output.Length);
        Assert.Equal("Second\0\0\0\0", string.Join("", output));
    }

    [Fact]
    public void ReaderOnlyReadsAsMuchAsRequested()
    {
        var pages = new LinkedList<ReadOnlyMemory<char>[]>();

        const string FirstLine = "First Line";
        pages.AddLast([FirstLine.AsMemory()]);

        var testReader = CodeWriter.GetTestTextReader(pages);
        var output = new char[FirstLine.Length];

        testReader.Read(output, 0, 2);
        Assert.Equal("Fi\0\0\0\0\0\0\0\0", string.Join("", output));
        Array.Clear(output, 0, output.Length);

        testReader.Read(output, 0, output.Length);
        Assert.Equal("rst Line\0\0", string.Join("", output));
    }

    [Fact]
    public void WriteIntegerLiteral_Zero_WritesZero()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteIntegerLiteral(0);

        // Assert
        var output = writer.GetText().ToString();
        Assert.Equal("0", output);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(42)]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(999)]
    public void WriteIntegerLiteral_SmallPositiveNumbers_WritesCorrectly(int value)
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteIntegerLiteral(value);

        // Assert
        var output = writer.GetText().ToString();
        var expected = value.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(1234)]
    [InlineData(12345)]
    [InlineData(123456)]
    [InlineData(1000000)]
    public void WriteIntegerLiteral_LargePositiveNumbers_WritesCorrectly(int value)
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteIntegerLiteral(value);

        // Assert
        var output = writer.GetText().ToString();
        var expected = value.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-5)]
    [InlineData(-42)]
    [InlineData(-99)]
    [InlineData(-100)]
    [InlineData(-999)]
    public void WriteIntegerLiteral_SmallNegativeNumbers_WritesCorrectly(int value)
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteIntegerLiteral(value);

        // Assert
        var output = writer.GetText().ToString();
        var expected = value.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData(-1000)]
    [InlineData(-1234)]
    [InlineData(-12345)]
    [InlineData(-123456)]
    [InlineData(-1000000)]
    public void WriteIntegerLiteral_LargeNegativeNumbers_WritesCorrectly(int value)
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteIntegerLiteral(value);

        // Assert
        var output = writer.GetText().ToString();
        var expected = value.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData(999999)]
    [InlineData(1000001)]
    [InlineData(999000)]
    [InlineData(1001000)]
    public void WriteIntegerLiteral_BoundaryValues_WritesCorrectly(int value)
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteIntegerLiteral(value);

        // Assert
        var output = writer.GetText().ToString();
        var expected = value.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData(10000)]      // Exactly 5 digits
    [InlineData(100000)]     // Exactly 6 digits  
    [InlineData(1000000000)] // Close to int.MaxValue
    [InlineData(2000000000)] // Larger value
    [InlineData(10001)]      // Leading zeros in middle group
    [InlineData(1000010)]    // Multiple leading zeros
    [InlineData(1020000)]    // Trailing zeros after non-zero
    [InlineData(102030)]     // Mixed digits
    [InlineData(1000000001)] // Maximum digits with leading zeros
    public void WriteIntegerLiteral_AdditionalEdgeCases_WritesCorrectly(int value)
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteIntegerLiteral(value);

        // Assert
        var output = writer.GetText().ToString();
        var expected = value.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData(int.MinValue + 1)] // Just above MinValue
    [InlineData(-10001)]           // Negative with leading zeros
    [InlineData(-1000010)]         // Negative with multiple groups
    public void WriteIntegerLiteral_NegativeEdgeCases_WritesCorrectly(int value)
    {
        // Arrange  
        using var writer = new CodeWriter();

        // Act
        writer.WriteIntegerLiteral(value);

        // Assert
        var output = writer.GetText().ToString();
        var expected = value.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(expected, output);
    }

    [Fact]
    public void WriteIntegerLiteral_MaxValue_WritesCorrectly()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteIntegerLiteral(int.MaxValue);

        // Assert
        var output = writer.GetText().ToString();
        Assert.Equal("2147483647", output);
    }

    [Fact]
    public void WriteIntegerLiteral_MinValue_WritesCorrectly()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteIntegerLiteral(int.MinValue);

        // Assert
        var output = writer.GetText().ToString();
        Assert.Equal("-2147483648", output);
    }

    [Fact]
    public void WriteIntegerLiteral_MultipleValues_WritesCorrectly()
    {
        // Arrange
        using var writer = new CodeWriter();

        // Act
        writer.WriteIntegerLiteral(123);
        writer.Write(", ");
        writer.WriteIntegerLiteral(-456);
        writer.Write(", ");
        writer.WriteIntegerLiteral(0);

        // Assert
        var output = writer.GetText().ToString();
        Assert.Equal("123, -456, 0", output);
    }
}
