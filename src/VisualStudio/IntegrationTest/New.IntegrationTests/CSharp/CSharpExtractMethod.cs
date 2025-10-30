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

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.ExtractMethod)]
public class CSharpExtractMethod : AbstractEditorTest
{
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

    public CSharpExtractMethod()
        : base(nameof(CSharpExtractMethod))
    {
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
        await TestServices.Editor.SetTextAsync(TestSource, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("int result", charsOffset: -8, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("result;", charsOffset: 4, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Refactor.ExtractMethod, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ExtractMethod, HangMitigatingCancellationToken);
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
        Assert.Equal(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        var tags = (await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken)).SelectAsArray(tag => tag.Span.Span.ToTextSpan());
        AssertEx.SetEqual(spans, tags);

        await TestServices.Input.SendAsync(["SayHello", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
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
        await TestServices.Editor.SetTextAsync(TestSource, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("a = 5", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("a * b", charsOffset: 1, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Extract method", applyFix: true, blockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
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
        Assert.Equal(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        var tags = (await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken)).SelectAsArray(tag => tag.Span.Span.ToTextSpan());
        AssertEx.SetEqual(spans, tags);
    }
}
