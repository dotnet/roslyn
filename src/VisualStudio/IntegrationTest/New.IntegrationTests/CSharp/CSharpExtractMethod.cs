// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.TextFormatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.PlatformUI;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
    public class CSharpExtractMethod : AbstractEditorTest
    {
        private const string TestSource = @"
using System;
public class Program
{
    public int Method()
    {
        Console.WriteLine(""Hello World"");
        int a;
        int b;
        a = 5;
        b = 10;
        int result = a * b;
        return result;
    }
}";
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
            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Extract method", applyFix: true, cancellationToken: HangMitigatingCancellationToken);
            var expectedMarkup = @"
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
        Console.WriteLine(""Hello World"");
    }
}";
            MarkupTestFile.GetSpans(expectedMarkup, out var expectedText, out ImmutableArray<TextSpan> spans);
            await TestServices.EditorVerifier.TextContainsAsync(expectedText, cancellationToken: HangMitigatingCancellationToken);
            AssertEx.SetEqual(spans, await TestServices.Editor.GetTagSpansAsync(RenameFieldBackgroundAndBorderTag.TagId, HangMitigatingCancellationToken));
            await TestServices.Input.SendAsync(new InputKey[] { "SayHello", VirtualKeyCode.RETURN }, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"private static void SayHello()
    {
        Console.WriteLine(""Hello World"");
    }");
        }

        [IdeFact, WorkItem(61369, "https://github.com/dotnet/roslyn/pull/61369")]
        public async Task ExtractMethodWithTriviaSelected()
        {
            await TestServices.Editor.SetTextAsync(TestSource, HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("int result", charsOffset: -8, HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("result;", charsOffset: 4, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Extract method", applyFix: true, cancellationToken: HangMitigatingCancellationToken);

            var expectedMarkup = @"
using System;
public class Program
{
    public int Method()
    {
        Console.WriteLine(""Hello World"");
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
}";

            MarkupTestFile.GetSpans(expectedMarkup, out var expectedText, out ImmutableArray<TextSpan> spans);
            await TestServices.EditorVerifier.TextContainsAsync(expectedText, cancellationToken: HangMitigatingCancellationToken);
            AssertEx.SetEqual(spans, await TestServices.Editor.GetTagSpansAsync(RenameFieldBackgroundAndBorderTag.TagId, HangMitigatingCancellationToken));
            await TestServices.Input.SendAsync(new InputKey[] { "SayHello", VirtualKeyCode.RETURN }, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"private static int SayHello(int a, int b)
    {
        return a * b;
    }");
        }
    }
}
