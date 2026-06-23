// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class GenerateConstructorTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    private const int FieldActionIndex = 0;
    private const int PropertyActionIndex = 1;
    private const int NoFieldActionIndex = 2;

    [Fact]
    public async Task GenerateConstructor_FromCodeBlock_ExistingCodeBlock()
    {
        var input = """
            @code
            {
                private File1 Create(int value)
                {
                    return new [||]File1(value);
                }
            }
            """;

        var expected = """
            @code
            {
                private File1 Create(int value)
                {
                    return new File1(value);
                }

                private int value;

                public File1(int value)
                {
                    this.value = value;
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            PredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_ForClassInCodeBlock_WithoutParameter()
    {
        var input = """
            @code
            {
                private Goo Create()
                {
                    return new [||]Goo();
                }

                private class Goo
                {
                    public Goo(int value)
                    {
                    }
                }
            }
            """;

        var expected = """
            @code
            {
                private Goo Create()
                {
                    return new Goo();
                }

                private class Goo
                {
                    public Goo()
                    {
                    }

                    public Goo(int value)
                    {
                    }
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            PredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_WithoutCodeBlock()
    {
        var input = """
            @{
                var value = 1;
                var item = new [||]File1(value);
            }
            """;

        var expected = """
            @{
                var value = 1;
                var item = new File1(value);
            }
            @code {
                private int value;

                public File1(int value)
                {
                    this.value = value;
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            PredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_WithoutCodeBlock_WithoutParameter()
    {
        await VerifyCodeActionAsync(
            input: """
                @{
                    var item = new [||]File1();
                }
                """,
            expected: """
                @{
                    var item = new File1();
                }
                @code {
                    public File1()
                    {
                    }
                }
                """,
            additionalFiles: [
                (FilePath("File1.razor.cs"), """
                    namespace SomeProject;

                    public partial class File1
                    {
                        public File1(int value)
                        {
                        }
                    }
                    """)],
            codeActionName: PredefinedCodeFixProviderNames.GenerateConstructor,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_FromRazor_InOtherFile()
    {
        await VerifyCodeActionAsync(
            input: """
                @code {
                    private Helper M(int value)
                    {
                        return new [||]Helper(value);
                    }
                }
                """,
            expected: """
                @code {
                    private Helper M(int value)
                    {
                        return new Helper(value);
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("Helper.cs"), """
                    namespace SomeProject;

                    public class Helper
                    {
                    }
                    """)
            ],
            additionalExpectedFiles:
            [
                (FileUri("Helper.cs"), """
                    namespace SomeProject;

                    public class Helper
                    {
                        private int value;

                        public Helper(int value)
                        {
                            this.value = value;
                        }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: FieldActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_FromRazor_InOtherFile_Property()
    {
        await VerifyCodeActionAsync(
            input: """
                @code {
                    private Helper M(int value)
                    {
                        return new [||]Helper(value);
                    }
                }
                """,
            expected: """
                @code {
                    private Helper M(int value)
                    {
                        return new Helper(value);
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("Helper.cs"), """
                    namespace SomeProject;

                    public class Helper
                    {
                    }
                    """)
            ],
            additionalExpectedFiles:
            [
                (FileUri("Helper.cs"), """
                    namespace SomeProject;

                    public class Helper
                    {
                        public Helper(int value)
                        {
                            Value = value;
                        }

                        public int Value { get; }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: PropertyActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_FromRazor_InOtherFile_NoField()
    {
        await VerifyCodeActionAsync(
            input: """
                @code {
                    private Helper M(int value)
                    {
                        return new [||]Helper(value);
                    }
                }
                """,
            expected: """
                @code {
                    private Helper M(int value)
                    {
                        return new Helper(value);
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("Helper.cs"), """
                    namespace SomeProject;

                    public class Helper
                    {
                    }
                    """)
            ],
            additionalExpectedFiles:
            [
                (FileUri("Helper.cs"), """
                    namespace SomeProject;

                    public class Helper
                    {
                        public Helper(int value)
                        {
                        }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: NoFieldActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_FromRazor_InOtherRazorFile()
    {
        await VerifyCodeActionAsync(
            input: """
                @code {
                    private OtherComponent M(int value)
                    {
                        return new [||]OtherComponent(value);
                    }
                }
                """,
            expected: """
                @code {
                    private OtherComponent M(int value)
                    {
                        return new OtherComponent(value);
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("OtherComponent.razor"), """
                    <div>Hi</div>
                    """)
            ],
            additionalExpectedFiles:
            [
                (FileUri("OtherComponent.razor"), """
                    <div>Hi</div>
                    @code {
                        private int value;

                        public OtherComponent(int value)
                        {
                            this.value = value;
                        }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: FieldActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_FromRazor_InOtherRazorFile_Property()
    {
        await VerifyCodeActionAsync(
            input: """
                @code {
                    private OtherComponent M(int value)
                    {
                        return new [||]OtherComponent(value);
                    }
                }
                """,
            expected: """
                @code {
                    private OtherComponent M(int value)
                    {
                        return new OtherComponent(value);
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("OtherComponent.razor"), """
                    <div>Hi</div>
                    """)
            ],
            additionalExpectedFiles:
            [
                (FileUri("OtherComponent.razor"), """
                    <div>Hi</div>
                    @code {
                        public OtherComponent(int value)
                        {
                            this.Value = value;
                        }

                        public int Value { get; }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: PropertyActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_FromRazor_InOtherRazorFile_NoField()
    {
        await VerifyCodeActionAsync(
            input: """
                @code {
                    private OtherComponent M(int value)
                    {
                        return new [||]OtherComponent(value);
                    }
                }
                """,
            expected: """
                @code {
                    private OtherComponent M(int value)
                    {
                        return new OtherComponent(value);
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("OtherComponent.razor"), """
                    <div>Hi</div>
                    """)
            ],
            additionalExpectedFiles:
            [
                (FileUri("OtherComponent.razor"), """
                    <div>Hi</div>
                    @code {
                        public OtherComponent(int value)
                        {
                        }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: NoFieldActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_ForClassInCodeBlock()
    {
        var input = """
            @code
            {
                private Goo Create(int value)
                {
                    return new [||]Goo(value);
                }

                private class Goo
                {
                }
            }
            """;

        var expected = """
            @code
            {
                private Goo Create(int value)
                {
                    return new Goo(value);
                }

                private class Goo
                {
                    private int value;

                    public Goo(int value)
                    {
                        this.value = value;
                    }
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            PredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: FieldActionIndex,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task GenerateConstructor_Legacy_WithoutFunctionsBlock()
    {
        var input = """
            @{
                var value = 1;
                var item = new [||]File1(value);
            }
            """;

        var expected = """
            @{
                var value = 1;
                var item = new File1(value);
            }
            @functions {
                private int value;

                public File1(int value)
                {
                    this.value = value;
                }
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            PredefinedCodeFixProviderNames.GenerateConstructor,
            codeActionIndex: FieldActionIndex,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }
}
