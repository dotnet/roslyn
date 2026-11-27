// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class GenerateEventHandlerTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task NoCodeBlock()
    {
        var input = """
            <button @onclick="{|CS0103:Does[||]NotExist|}"></button>
            """;

        var expected = """
            <button @onclick="DoesNotExist"></button>
            @code {
                private void DoesNotExist(MouseEventArgs args)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, LanguageServerConstants.CodeActions.GenerateEventHandler);
    }

    [Fact]
    public async Task CodeBlock()
    {
        var input = """
            <button @onclick="{|CS0103:Does[||]NotExist|}"></button>

            @code
            {
            }
            """;

        var expected = """
            <button @onclick="DoesNotExist"></button>

            @code
            {
                private void DoesNotExist(MouseEventArgs args)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, LanguageServerConstants.CodeActions.GenerateEventHandler);
    }

    [Fact]
    public async Task CodeBlock_WithExistingCode()
    {
        var input = """
            <button @onclick="{|CS0103:Does[||]NotExist|}"></button>

            @code
            {
                public void M()
                {
                }
            }
            """;

        var expected = """
            <button @onclick="DoesNotExist"></button>

            @code
            {
                public void M()
                {
                }

                private void DoesNotExist(MouseEventArgs args)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, LanguageServerConstants.CodeActions.GenerateEventHandler);
    }

    [Fact]
    public async Task CodeBlock_Indented()
    {
        var input = """
            <button @onclick="{|CS0103:Does[||]NotExist|}"></button>

              @code {
              }
            """;

        var expected = """
            <button @onclick="DoesNotExist"></button>

            @code {
                private void DoesNotExist(MouseEventArgs args)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, LanguageServerConstants.CodeActions.GenerateEventHandler);
    }

    [Fact(Skip = "https://github.com/dotnet/razor/issues/11801")]
    public async Task BindSet()
    {
        var input = """
            <InputText @bind-Value="Text" @bind-Value:set="{|CS0103:Does[||]NotExist|}" />

            @code
            {
                private string Text { get; set; }
            }
            """;

        var expected = """
            <InputText @bind-Value="Text" @bind-Value:set="DoesNotExist" />

            @code
            {
                private string Text { get; set; }
                private void DoesNotExist(string args)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, LanguageServerConstants.CodeActions.GenerateEventHandler);
    }

    [Fact(Skip = "https://github.com/dotnet/razor/issues/11801")]
    public async Task BindAfter()
    {
        var input = """
            <InputText @bind-Value="Text" @bind-Value:after="{|CS0103:Does[||]NotExist|}" />

            @code
            {
                private string Text { get; set; }
            }
            """;

        var expected = """
            <InputText @bind-Value="Text" @bind-Value:after="DoesNotExist" />

            @code
            {
                private string Text { get; set; }
                private void DoesNotExist()
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, LanguageServerConstants.CodeActions.GenerateEventHandler);
    }

    [Fact]
    public async Task Callback()
    {
        var input = """
            <InputFile OnChange="{|CS0103:Does[||]NotExist|}" />

            @code
            {
            }
            """;

        var expected = """
            <InputFile OnChange="DoesNotExist" />

            @code
            {
                private void DoesNotExist(InputFileChangeEventArgs args)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, LanguageServerConstants.CodeActions.GenerateEventHandler);
    }

    [Fact]
    public async Task AsyncCallback()
    {
        var input = """
            <InputText ValueChanged="{|CS0103:Does[||]NotExistAsync|}" />

            @code
            {
            }
            """;

        var expected = """
            <InputText ValueChanged="DoesNotExistAsync" />

            @code
            {
                private Task DoesNotExistAsync(string args)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, LanguageServerConstants.CodeActions.GenerateAsyncEventHandler);
    }

    [Fact]
    public async Task BadCodeBehind()
    {
        await VerifyCodeActionAsync(
            input: """
                <button @onclick="{|CS0103:Does[||]NotExist|}"></button>
                """,
            expected: """
                <button @onclick="DoesNotExist"></button>
                @code {
                    private void DoesNotExist(MouseEventArgs args)
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            additionalFiles: [
                (FilePath("File1.razor.cs"), """
                    namespace Goo
                    {
                        public partial class NotAComponent
                        {
                        }
                    }
                    """)],
            codeActionName: LanguageServerConstants.CodeActions.GenerateEventHandler);
    }

    [Fact]
    public async Task CodeBehind()
    {
        await VerifyCodeActionAsync(
            input: """
                <button @onclick="{|CS0103:Does[||]NotExist|}"></button>
                """,
            expected: """
                <button @onclick="DoesNotExist"></button>
                """,
            additionalFiles: [
                (FilePath("File1.razor.cs"), """
                    namespace SomeProject;

                    public partial class File1
                    {
                        public void M()
                        {
                        }
                    }
                    """)],
            additionalExpectedFiles: [
                (FileUri("File1.razor.cs"), """
                    namespace SomeProject;
                    
                    public partial class File1
                    {
                        public void M()
                        {
                        }
                        private void DoesNotExist(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                    """)],
            codeActionName: LanguageServerConstants.CodeActions.GenerateEventHandler);
    }

    [Fact]
    public async Task CodeBehind_BlockBodiedNamespace()
    {
        await VerifyCodeActionAsync(
            input: """
                <button @onclick="{|CS0103:Does[||]NotExist|}"></button>
                """,
            expected: """
                <button @onclick="DoesNotExist"></button>
                """,
            additionalFiles: [
                (FilePath("File1.razor.cs"), """
                    namespace SomeProject
                    {
                        public partial class File1
                        {
                            public void M()
                            {
                            }
                        }
                    }
                    """)],
            additionalExpectedFiles: [
                (FileUri("File1.razor.cs"), """
                    namespace SomeProject
                    {
                        public partial class File1
                        {
                            public void M()
                            {
                            }
                            private void DoesNotExist(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
                            {
                                throw new System.NotImplementedException();
                            }
                        }
                    }
                    """)],
            codeActionName: LanguageServerConstants.CodeActions.GenerateEventHandler);
    }

    [Fact]
    public async Task EmptyCodeBehind()
    {
        await VerifyCodeActionAsync(
            input: """
                <button @onclick="{|CS0103:Does[||]NotExist|}"></button>
                """,
            expected: """
                <button @onclick="DoesNotExist"></button>
                """,
            additionalFiles: [
                (FilePath("File1.razor.cs"), """
                    namespace SomeProject;

                    public partial class File1
                    {
                    }
                    """)],
            additionalExpectedFiles: [
                (FileUri("File1.razor.cs"), """
                    namespace SomeProject;
                    
                    public partial class File1
                    {
                        private void DoesNotExist(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                    """)],
            codeActionName: LanguageServerConstants.CodeActions.GenerateEventHandler);
    }

    [Fact]
    public async Task GenerateAsyncEventHandler_NoCodeBlock()
    {
        var input = """
            <button @onclick="{|CS0103:Does[||]NotExist|}"></button>
            """;

        var expected = """
            <button @onclick="DoesNotExist"></button>
            @code {
                private Task DoesNotExist(MouseEventArgs args)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, LanguageServerConstants.CodeActions.GenerateAsyncEventHandler);
    }

    [Fact]
    public async Task GenerateAsyncEventHandler_CodeBlock()
    {
        var input = """
            <button @onclick="{|CS0103:Does[||]NotExist|}"></button>

            @code
            {
            }
            """;

        var expected = """
            <button @onclick="DoesNotExist"></button>

            @code
            {
                private Task DoesNotExist(MouseEventArgs args)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, LanguageServerConstants.CodeActions.GenerateAsyncEventHandler);
    }

    [Fact]
    public async Task NotInRefAttribute()
    {
        var input = """
            <button @ref="{|CS0103:Does[||]NotExist|}"></button>

            @code
            {
            }
            """;

        await VerifyCodeActionAsync(input, expected: null, LanguageServerConstants.CodeActions.GenerateAsyncEventHandler);
    }
}
