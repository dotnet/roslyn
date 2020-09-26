// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
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

        public CSharpExtractMethod(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpExtractMethod))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SimpleExtractMethod()
        {
            VisualStudio.Editor.SetText(TestSource);
            VisualStudio.Editor.PlaceCaret("Console", charsOffset: -1);
            VisualStudio.Editor.PlaceCaret("World", charsOffset: 4, extendSelection: true);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Refactor_ExtractMethod);

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
            VisualStudio.Editor.Verify.TextContains(expectedText);
            AssertEx.SetEqual(spans, VisualStudio.Editor.GetTagSpans(VisualStudio.InlineRenameDialog.ValidRenameTag));

            VisualStudio.Editor.SendKeys("SayHello", VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"private static void SayHello()
    {
        Console.WriteLine(""Hello World"");
    }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractViaCodeAction()
        {
            VisualStudio.Editor.SetText(TestSource);
            VisualStudio.Editor.PlaceCaret("a = 5", charsOffset: -1);
            VisualStudio.Editor.PlaceCaret("a * b", charsOffset: 1, extendSelection: true);
            VisualStudio.Editor.Verify.CodeAction("Extract method", applyFix: true, blockUntilComplete: true);

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
            Assert.Equal(expectedText, VisualStudio.Editor.GetText());
            AssertEx.SetEqual(spans, VisualStudio.Editor.GetTagSpans(VisualStudio.InlineRenameDialog.ValidRenameTag));
        }
    }
}
