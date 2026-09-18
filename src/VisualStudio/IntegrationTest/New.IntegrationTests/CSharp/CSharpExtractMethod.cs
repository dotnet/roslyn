// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.ExtractMethod)]
public class CSharpExtractMethod : AbstractEditorTest
{
    private readonly ITestOutputHelper _output;

    private const string TestSource = """

        using System;
        public class Program
        {
            public int Method()
            {
                Console.WriteLine("Hello World");
                int a;
                int b;
                a = 5;
                b = 10;
                int result = a * b;
                return result;
            }
        }
        """;

    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpExtractMethod(ITestOutputHelper output)
        : base(nameof(CSharpExtractMethod))
    {
        _output = output;
    }

    [IdeFact]
    public async Task SimpleExtractMethod()
    {
        await TestServices.Editor.SetTextAsync(TestSource, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Console", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("World", charsOffset: 4, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Refactor.ExtractMethod, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ExtractMethod, HangMitigatingCancellationToken);
        MarkupTestFile.GetSpans("""

            using System;
            public class Program
            {
                public int Method()
                {
                    [|NewMethod|]();
                    int a;
                    int b;
                    a = 5;
                    b = 10;
                    int result = a * b;
                    return result;
                }

                private static void [|NewMethod|]()
                {
                    Console.WriteLine("Hello World");
                }
            }
            """, out var expectedText, out var spans);
        await TestServices.EditorVerifier.TextContainsAsync(expectedText, cancellationToken: HangMitigatingCancellationToken);
        var tags = (await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken)).SelectAsArray(tag => tag.Span.Span.ToTextSpan());
        AssertEx.SetEqual(spans, tags);

        await TestServices.Input.SendAsync(["SayHello", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""
            private static void SayHello()
                {
                    Console.WriteLine("Hello World");
                }
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/pull/61369")]
    public async Task ExtractMethodWithTriviaSelected()
    {
        _output.WriteLine("CSharpExtractMethod.ExtractMethodWithTriviaSelected: action 1");
        await TestServices.Editor.SetTextAsync(TestSource, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpExtractMethod.ExtractMethodWithTriviaSelected: action 2");
        await TestServices.Editor.PlaceCaretAsync("int result", charsOffset: -8, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpExtractMethod.ExtractMethodWithTriviaSelected: action 3");
        await TestServices.Editor.PlaceCaretAsync("result;", charsOffset: 4, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpExtractMethod.ExtractMethodWithTriviaSelected: action 4");
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Refactor.ExtractMethod, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpExtractMethod.ExtractMethodWithTriviaSelected: action 5");
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ExtractMethod, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpExtractMethod.ExtractMethodWithTriviaSelected: action 6");
        MarkupTestFile.GetSpans("""

            using System;
            public class Program
            {
                public int Method()
                {
                    Console.WriteLine("Hello World");
                    int a;
                    int b;
                    a = 5;
                    b = 10;
                    return [|NewMethod|](a, b);
                }

                private static int [|NewMethod|](int a, int b)
                {
                    return a * b;
                }
            }
            """, out var expectedText, out var spans);
        _output.WriteLine("CSharpExtractMethod.ExtractMethodWithTriviaSelected: action 7");
        Assert.Equal(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        _output.WriteLine("CSharpExtractMethod.ExtractMethodWithTriviaSelected: action 8");
        var tags = (await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken)).SelectAsArray(tag => tag.Span.Span.ToTextSpan());
        _output.WriteLine("CSharpExtractMethod.ExtractMethodWithTriviaSelected: action 9");
        AssertEx.SetEqual(spans, tags);

        _output.WriteLine("CSharpExtractMethod.ExtractMethodWithTriviaSelected: action 10");
        await TestServices.Input.SendAsync(["SayHello", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        _output.WriteLine("CSharpExtractMethod.ExtractMethodWithTriviaSelected: action 11");
        await TestServices.EditorVerifier.TextContainsAsync("""
            private static int SayHello(int a, int b)
                {
                    return a * b;
                }
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task ExtractViaCodeAction()
    {
        _output.WriteLine("CSharpExtractMethod.ExtractViaCodeAction: action 1");
        await TestServices.Editor.SetTextAsync(TestSource, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpExtractMethod.ExtractViaCodeAction: action 2");
        await TestServices.Editor.PlaceCaretAsync("a = 5", charsOffset: -1, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpExtractMethod.ExtractViaCodeAction: action 3");
        await TestServices.Editor.PlaceCaretAsync("a * b", charsOffset: 1, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpExtractMethod.ExtractViaCodeAction: action 4");
        await TestServices.EditorVerifier.CodeActionAsync("Extract method", applyFix: true, blockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine("CSharpExtractMethod.ExtractViaCodeAction: action 5");
        MarkupTestFile.GetSpans("""

            using System;
            public class Program
            {
                public int Method()
                {
                    Console.WriteLine("Hello World");
                    int a;
                    int b;
                    int result;
                    [|NewMethod|](out a, out b, out result);
                    return result;
                }

                private static void [|NewMethod|](out int a, out int b, out int result)
                {
                    a = 5;
                    b = 10;
                    result = a * b;
                }
            }
            """, out var expectedText, out var spans);
        _output.WriteLine("CSharpExtractMethod.ExtractViaCodeAction: action 6");
        Assert.Equal(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        _output.WriteLine("CSharpExtractMethod.ExtractViaCodeAction: action 7");
        var tags = (await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken)).SelectAsArray(tag => tag.Span.Span.ToTextSpan());
        _output.WriteLine("CSharpExtractMethod.ExtractViaCodeAction: action 8");
        AssertEx.SetEqual(spans, tags);
    }
}
