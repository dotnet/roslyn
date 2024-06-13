// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.FileHeaders.CSharpFileHeaderDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.FileHeaders.CSharpFileHeaderCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.FileHeaders;

public class FileHeaderTests
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
    public async Task TestFileHeaderNotConfiguredAsync(string fileHeaderTemplate)
    {
        var testCode = """
            namespace N
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = testCode,
            EditorConfig = $@"
[*]
{fileHeaderTemplate}
",
        }.RunAsync();
    }

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
    public async Task TestNoFileHeaderWithUsingDirectiveAsync()
    {
        var testCode = """
            [||]using System;

            namespace N
            {
            }
            """;
        var fixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            using System;

            namespace N
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that the analyzer will report a diagnostic when the file is completely missing a header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestNoFileHeaderWithBlankLineAndUsingDirectiveAsync()
    {
        var testCode = """
            [||]
            using System;

            namespace N
            {
            }
            """;
        var fixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            using System;

            namespace N
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

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
        var fixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            using System;

            namespace N
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that the built-in variable <c>fileName</c> works as expected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestFileNameBuiltInVariableAsync()
    {
        var editorConfig = """
            [*.cs]
            file_header_template = {fileName} Copyright (c) SomeCorp. All rights reserved.\nLicensed under the ??? license. See LICENSE file in the project root for full license information.
            """;

        var testCode = """
            [||]namespace N
            {
            }
            """;
        var fixedCode = """
            // Test0.cs Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace N
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = editorConfig,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that a valid file header built using single line comments will not produce a diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestValidFileHeaderWithSingleLineCommentsAsync()
    {
        var testCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = testCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that a valid file header built using multi-line comments will not produce a diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestValidFileHeaderWithMultiLineComments1Async()
    {
        var testCode = """
            /* Copyright (c) SomeCorp. All rights reserved.
             * Licensed under the ??? license. See LICENSE file in the project root for full license information.
             */

            namespace Bar
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = testCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that a valid file header built using multi-line comments will not produce a diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestValidFileHeaderWithMultiLineComments2Async()
    {
        var testCode = """
            /* Copyright (c) SomeCorp. All rights reserved.
               Licensed under the ??? license. See LICENSE file in the project root for full license information. */

            namespace Bar
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = testCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

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
    public async Task TestInvalidFileHeaderWithoutTextAsync(string comment)
    {
        var testCode = $@"{comment}

namespace Bar
{{
}}";
        var fixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestInvalidFileHeaderWithWrongTextAsync()
    {
        var testCode = """
            [|//|] Copyright (c) OtherCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """;
        var fixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestInvalidFileHeaderWithWrongText2Async()
    {
        var testCode = """
            [|/*|] Copyright (c) OtherCorp. All rights reserved.
             * Licensed under the ??? license. See LICENSE file in the project root for full license information.
             */

            namespace Bar
            {
            }
            """;
        var fixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            /* Copyright (c) OtherCorp. All rights reserved.
             * Licensed under the ??? license. See LICENSE file in the project root for full license information.
             */

            namespace Bar
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(" Header", "")]
    [InlineData(" Header", " Header")]
    public async Task TestValidFileHeaderInRegionAsync(string startLabel, string endLabel)
    {
        var testCode = $@"#region{startLabel}
// Copyright (c) SomeCorp. All rights reserved.
// Licensed under the ??? license. See LICENSE file in the project root for full license information.
#endregion{endLabel}

namespace Bar
{{
}}
";

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = testCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(" Header", "")]
    [InlineData(" Header", " Header")]
    public async Task TestInvalidFileHeaderWithWrongTextInRegionAsync(string startLabel, string endLabel)
    {
        var testCode = $@"#region{startLabel}
[|//|] Copyright (c) OtherCorp. All rights reserved.
// Licensed under the ??? license. See LICENSE file in the project root for full license information.
#endregion{endLabel}

namespace Bar
{{
}}
";
        var fixedCode = $@"// Copyright (c) SomeCorp. All rights reserved.
// Licensed under the ??? license. See LICENSE file in the project root for full license information.

#region{startLabel}
// Copyright (c) OtherCorp. All rights reserved.
// Licensed under the ??? license. See LICENSE file in the project root for full license information.
#endregion{endLabel}

namespace Bar
{{
}}
";

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestInvalidFileHeaderWithWrongTextInUnterminatedMultiLineComment1Async()
    {
        var testCode = """
            {|CS1035:|}[|/*|] Copyright (c) OtherCorp. All rights reserved.
             * Licensed under the ??? license. See LICENSE file in the project root for full license information.
            """;
        var fixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            {|CS1035:|}/* Copyright (c) OtherCorp. All rights reserved.
             * Licensed under the ??? license. See LICENSE file in the project root for full license information.
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestInvalidFileHeaderWithWrongTextInUnterminatedMultiLineComment2Async()
    {
        var testCode = """
            {|CS1035:|}[|/*|]/
            """;
        var fixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            {|CS1035:|}/*/
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public async Task TestInvalidFileHeaderWithWrongTextAfterBlankLineAsync(string firstLine)
    {
        var testCode = $@"{firstLine}
[|//|] Copyright (c) OtherCorp. All rights reserved.
// Licensed under the ??? license. See LICENSE file in the project root for full license information.

namespace Bar
{{
}}";
        var fixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    /// <summary>
    /// Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestInvalidFileHeaderWithWrongTextFollowedByCommentAsync()
    {
        var testCode = """
            [|//|] Copyright (c) OtherCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            //using System;

            namespace Bar
            {
            }
            """;
        var fixedCode = """
            // Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            //using System;

            namespace Bar
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettings,
        }.RunAsync();
    }

    [Fact]
    public async Task TestHeaderMissingRequiredNewLinesAsync()
    {
        var testCode = """
            [|//|] Copyright (c) SomeCorp. All rights reserved.
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.

            namespace Bar
            {
            }
            """;
        var fixedCode = """
            //
            // Copyright (c) SomeCorp. All rights reserved.
            //
            // Licensed under the ??? license. See LICENSE file in the project root for full license information.
            //

            namespace Bar
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = TestSettingsWithEmptyLines,
        }.RunAsync();
    }
}
