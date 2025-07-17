// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.FileHeaders;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.FileHeaders;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpFileHeaderDiagnosticAnalyzer,
    CSharpFileHeaderCodeFixProvider>;

public sealed class FileHeaderTests
{
    private const string TestSettings = """
        [*.cs]
        file_header_template = Copyright (c) SomeCorp. All rights reserved.\nLicensed under the ??? license. See LICENSE file in the project root for full license information.
        """;

    private const string TestSettingsWithEmptyLines = """
        [*.cs]
        file_header_template = \nCopyright (c) SomeCorp. All rights reserved.\n\nLicensed under the ??? license. See LICENSE file in the project root for full license information.\n
        """;

    /// <summary>
    /// Verifies that the analyzer will not report a diagnostic when the file header is not configured.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("")]
    [InlineData("file_header_template =")]
    [InlineData("file_header_template = unset")]
    public Task TestFileHeaderNotConfiguredAsync(string fileHeaderTemplate)
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
            }
            """,
            EditorConfig = $"""
            [*]
            {fileHeaderTemplate}
            """,
        }.RunAsync();

    /// <summary>
    /// Verifies that the analyzer will report a diagnostic when the file is completely missing a header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1414432")]
    public async Task TestNoFileHeaderAsync(string lineEnding)
    {
        var testCode = """
            [||]namespace N
            {
            }
            """;
        var fixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace N
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode.ReplaceLineEndings(lineEnding),
            FixedCode = fixedCode.ReplaceLineEndings(lineEnding),
            EditorConfig = TestSettings,
            Options =
            {
                { FormattingOptions2.NewLine, lineEnding },
            },
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that the analyzer will report a diagnostic when the file is completely missing a header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task TestNoFileHeaderWithUsingDirectiveAsync()
        => new VerifyCS.Test
        {
            TestCode = """
            [||]using System;

            namespace N
            {
            }
            """,
            FixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            using System;

            namespace N
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    /// <summary>
    /// Verifies that the analyzer will report a diagnostic when the file is completely missing a header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task TestNoFileHeaderWithBlankLineAndUsingDirectiveAsync()
        => new VerifyCS.Test
        {
            TestCode = """
            [||]
            using System;

            namespace N
            {
            }
            """,
            FixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            using System;

            namespace N
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    /// <summary>
    /// Verifies that the analyzer will report a diagnostic when the file is completely missing a header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestNoFileHeaderWithWhitespaceLineAsync()
    {
        var testCode = "[||]    " + """

            using System;

            namespace N
            {
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            using System;

            namespace N
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that the built-in variable <c>fileName</c> works as expected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task TestFileNameBuiltInVariableAsync()
        => new VerifyCS.Test
        {
            TestCode = """
            [||]namespace N
            {
            }
            """,
            FixedCode = """
            // Test0.cs Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace N
            {
            }
            """,
            EditorConfig = """
            [*.cs]
            file_header_template = {fileName} Copyright (c) SomeCorp. All rights reserved.\nLicensed under the ??? license. See LICENSE file in the project root for full license information.
            """,
        }.RunAsync();

    /// <summary>
    /// Verifies that a valid file header built using single line comments will not produce a diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task TestValidFileHeaderWithSingleLineCommentsAsync()
        => new VerifyCS.Test
        {
            TestCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    /// <summary>
    /// Verifies that a valid file header built using multi-line comments will not produce a diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task TestValidFileHeaderWithMultiLineComments1Async()
        => new VerifyCS.Test
        {
            TestCode = """
            /* Copyright (c) SomeCorp. All rights reserved.
             * Licensed under the ??? license. See LICENSE file in the project root for full license information.
             */

            namespace Bar
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    /// <summary>
    /// Verifies that a valid file header built using multi-line comments will not produce a diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task TestValidFileHeaderWithMultiLineComments2Async()
        => new VerifyCS.Test
        {
            TestCode = """
            /* Copyright (c) SomeCorp. All rights reserved.
               Licensed under the ??? license. See LICENSE file in the project root for full license information. */

            namespace Bar
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    /// <summary>
    /// Verifies that a valid file header built using unterminated multi-line comments will not produce a diagnostic
    /// message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestValidFileHeaderWithMultiLineComments3Async()
    {
        var testCode = """
            /* Copyright (c) SomeCorp. All rights reserved.
               Licensed under the ??? license. See LICENSE file in the project root for full license information.
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(1,1): error CS1035: End-of-file found, '*/' expected
                DiagnosticResult.CompilerError("CS1035").WithSpan(1, 1, 1, 1),
            },
            FixedCode = testCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that a file header without text / only whitespace will produce the expected diagnostic message.
    /// </summary>
    /// <param name="comment">The comment text.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("[|//|]")]
    [InlineData("[|//|]    ")]
    public Task TestInvalidFileHeaderWithoutTextAsync(string comment)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            {{comment}}

            namespace Bar
            {
            }
            """,
            FixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task TestInvalidFileHeaderWithWrongTextAsync()
        => new VerifyCS.Test
        {
            TestCode = """
            [|//|] Copyright (c) OtherCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """,
            FixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task TestInvalidFileHeaderWithWrongText2Async()
        => new VerifyCS.Test
        {
            TestCode = """
            [|/*|] Copyright (c) OtherCorp. All rights reserved.
             * Licensed under the ??? license. See LICENSE file in the project root for full license information.
             */

            namespace Bar
            {
            }
            """,
            FixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            /* Copyright (c) OtherCorp. All rights reserved.
             * Licensed under the ??? license. See LICENSE file in the project root for full license information.
             */

            namespace Bar
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    [Theory]
    [InlineData("", "")]
    [InlineData(" Header", "")]
    [InlineData(" Header", " Header")]
    public Task TestValidFileHeaderInRegionAsync(string startLabel, string endLabel)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            #region{{startLabel}}
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.
            #endregion{{endLabel}}

            namespace Bar
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    [Theory]
    [InlineData("", "")]
    [InlineData(" Header", "")]
    [InlineData(" Header", " Header")]
    public Task TestInvalidFileHeaderWithWrongTextInRegionAsync(string startLabel, string endLabel)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            #region{{startLabel}}
            [|//|] Copyright (c) OtherCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.
            #endregion{{endLabel}}

            namespace Bar
            {
            }
            """,
            FixedCode = $$"""
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            #region{{startLabel}}
            // Copyright (c) OtherCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.
            #endregion{{endLabel}}

            namespace Bar
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task TestInvalidFileHeaderWithWrongTextInUnterminatedMultiLineComment1Async()
        => new VerifyCS.Test
        {
            TestCode = """
            {|CS1035:|}[|/*|] Copyright (c) OtherCorp. All rights reserved.
             * Licensed under the ??? license. See LICENSE file in the project root for full license information.
            """,
            FixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            {|CS1035:|}/* Copyright (c) OtherCorp. All rights reserved.
             * Licensed under the ??? license. See LICENSE file in the project root for full license information.
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task TestInvalidFileHeaderWithWrongTextInUnterminatedMultiLineComment2Async()
        => new VerifyCS.Test
        {
            TestCode = """
            {|CS1035:|}[|/*|]/
            """,
            FixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            {|CS1035:|}/*/
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public Task TestInvalidFileHeaderWithWrongTextAfterBlankLineAsync(string firstLine)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            {{firstLine}}
            [|//|] Copyright (c) OtherCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """,
            FixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task TestInvalidFileHeaderWithWrongTextFollowedByCommentAsync()
        => new VerifyCS.Test
        {
            TestCode = """
            [|//|] Copyright (c) OtherCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            //using System;

            namespace Bar
            {
            }
            """,
            FixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            //using System;

            namespace Bar
            {
            }
            """,
            EditorConfig = TestSettings,
        }.RunAsync();

    [Fact]
    public Task TestHeaderMissingRequiredNewLinesAsync()
        => new VerifyCS.Test
        {
            TestCode = """
            [|//|] Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """,
            FixedCode = """
            //
            // Copyright (c) SomeCorp. All rights reserved.
            //
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.
            //

            namespace Bar
            {
            }
            """,
            EditorConfig = TestSettingsWithEmptyLines,
        }.RunAsync();
}
