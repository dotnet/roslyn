// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
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

        public CSharpExtractMethod(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpExtractMethod))
        {
        }

        [WpfFact]
        public void SimpleExtractMethod()
        {
            VisualStudio.Editor.SetText(TestSource);
            VisualStudio.Editor.PlaceCaret("Console", charsOffset: -1);
            VisualStudio.Editor.PlaceCaret("World", charsOffset: 4, extendSelection: true);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Refactor_ExtractMethod);
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ExtractMethod);

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

            MarkupTestFile.GetSpans(expectedMarkup, out var expectedText, out var spans);
            VisualStudio.Editor.Verify.TextContains(expectedText);
            AssertEx.SetEqual(spans, VisualStudio.Editor.GetTagSpans(InlineRenameDialog_OutOfProc.ValidRenameTag));

            VisualStudio.Editor.SendKeys("SayHello", VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"private static void SayHello()
    {
        Console.WriteLine(""Hello World"");
    }");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/61369")]
        public void ExtractMethodWithTriviaSelected()
        {
            VisualStudio.Editor.SetText(TestSource);
            VisualStudio.Editor.PlaceCaret("int result", charsOffset: -8);
            VisualStudio.Editor.PlaceCaret("result;", charsOffset: 4, extendSelection: true);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Refactor_ExtractMethod);
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ExtractMethod);

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

            MarkupTestFile.GetSpans(expectedMarkup, out var expectedText, out var spans);
            Assert.Equal(expectedText, VisualStudio.Editor.GetText());
            AssertEx.SetEqual(spans, VisualStudio.Editor.GetTagSpans(InlineRenameDialog_OutOfProc.ValidRenameTag));

            VisualStudio.Editor.SendKeys("SayHello", VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"private static int SayHello(int a, int b)
    {
        return a * b;
    }");
        }

        [WpfFact]
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

            MarkupTestFile.GetSpans(expectedMarkup, out var expectedText, out var spans);
            Assert.Equal(expectedText, VisualStudio.Editor.GetText());
            AssertEx.SetEqual(spans, VisualStudio.Editor.GetTagSpans(InlineRenameDialog_OutOfProc.ValidRenameTag));
        }
    }
}
