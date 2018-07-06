// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpExtractMethod : AbstractIdeEditorTest
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

        public CSharpExtractMethod()
            : base(nameof(CSharpExtractMethod))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SimpleExtractMethodAsync()
        {
            await VisualStudio.Editor.SetTextAsync(TestSource);
            await VisualStudio.Editor.PlaceCaretAsync("Console", charsOffset: -1);
            await VisualStudio.Editor.PlaceCaretAsync("World", charsOffset: 4, extendSelection: true);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Refactor_ExtractMethod);

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
            await VisualStudio.Editor.Verify.TextContainsAsync(expectedText);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Rename);
            AssertEx.SetEqual(spans, await VisualStudio.Editor.GetTagSpansAsync(VisualStudio.InlineRenameDialog.ValidRenameTag));

            await VisualStudio.Editor.SendKeysAsync("SayHello", VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"private static void SayHello()
    {
        Console.WriteLine(""Hello World"");
    }");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task ExtractViaCodeActionAsync()
        {
            await VisualStudio.Editor.SetTextAsync(TestSource);
            await VisualStudio.Editor.PlaceCaretAsync("a = 5", charsOffset: -1);
            await VisualStudio.Editor.PlaceCaretAsync("a * b", charsOffset: 1, extendSelection: true);
            await VisualStudio.Editor.Verify.CodeActionAsync("Extract Method", applyFix: true, willBlockUntilComplete: true);

            var expectedMarkup = @"
using System;
public class Program
{
    public int Method()
    {
        Console.WriteLine(""Hello World"");
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
}";

            MarkupTestFile.GetSpans(expectedMarkup, out var expectedText, out ImmutableArray<TextSpan> spans);
            Assert.Equal(expectedText, await VisualStudio.Editor.GetTextAsync());
            AssertEx.SetEqual(spans, await VisualStudio.Editor.GetTagSpansAsync(VisualStudio.InlineRenameDialog.ValidRenameTag));
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task ExtractViaCodeActionWithMoveLocalAsync()
        {
            await VisualStudio.Editor.SetTextAsync(TestSource);
            await VisualStudio.Editor.PlaceCaretAsync("a = 5", charsOffset: -1);
            await VisualStudio.Editor.PlaceCaretAsync("a * b", charsOffset: 1, extendSelection: true);
            try
            {
                await VisualStudio.Workspace.SetFeatureOptionAsync(ExtractMethodOptions.AllowMovingDeclaration, LanguageNames.CSharp, true);
                await VisualStudio.Editor.Verify.CodeActionAsync("Extract Method + Local", applyFix: true, willBlockUntilComplete: true);

                var expectedMarkup = @"
using System;
public class Program
{
    public int Method()
    {
        Console.WriteLine(""Hello World"");
        int result = [|NewMethod|]();
        return result;
    }

    private static int [|NewMethod|]()
    {
        int a, b;
        a = 5;
        b = 10;
        int result = a * b;
        return result;
    }
}";

                MarkupTestFile.GetSpans(expectedMarkup, out var expectedText, out ImmutableArray<TextSpan> spans);
                Assert.Equal(expectedText, await VisualStudio.Editor.GetTextAsync());
                AssertEx.SetEqual(spans, await VisualStudio.Editor.GetTagSpansAsync(VisualStudio.InlineRenameDialog.ValidRenameTag));
            }
            finally
            {
                await VisualStudio.Workspace.SetFeatureOptionAsync(ExtractMethodOptions.AllowMovingDeclaration, LanguageNames.CSharp, false);
            }
        }
    }
}
