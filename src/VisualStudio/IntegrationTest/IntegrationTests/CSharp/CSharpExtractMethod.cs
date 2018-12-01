// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
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

        public CSharpExtractMethod() : base(nameof(CSharpExtractMethod)) { }

        [TestMethod, TestCategory(Traits.Features.ExtractMethod)]
        public void SimpleExtractMethod()
        {
            VisualStudioInstance.Editor.SetText(TestSource);
            VisualStudioInstance.Editor.PlaceCaret("Console", charsOffset: -1);
            VisualStudioInstance.Editor.PlaceCaret("World", charsOffset: 4, extendSelection: true);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Refactor_ExtractMethod);

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
            VisualStudioInstance.Editor.Verify.TextContains(expectedText);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Rename);
            AssertEx.SetEqual(spans, VisualStudioInstance.Editor.GetTagSpans(VisualStudioInstance.InlineRenameDialog.ValidRenameTag));

            VisualStudioInstance.Editor.SendKeys("SayHello", VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.TextContains(@"private static void SayHello()
    {
        Console.WriteLine(""Hello World"");
    }");
        }

        [TestMethod, TestCategory(Traits.Features.ExtractMethod)]
        public void ExtractViaCodeAction()
        {
            VisualStudioInstance.Editor.SetText(TestSource);
            VisualStudioInstance.Editor.PlaceCaret("a = 5", charsOffset: -1);
            VisualStudioInstance.Editor.PlaceCaret("a * b", charsOffset: 1, extendSelection: true);
            VisualStudioInstance.Editor.Verify.CodeAction("Extract Method", applyFix: true, blockUntilComplete: true);

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
            Assert.AreEqual(expectedText, VisualStudioInstance.Editor.GetText());
            AssertEx.SetEqual(spans, VisualStudioInstance.Editor.GetTagSpans(VisualStudioInstance.InlineRenameDialog.ValidRenameTag));
        }

        [TestMethod, TestCategory(Traits.Features.ExtractMethod)]
        public void ExtractViaCodeActionWithMoveLocal()
        {
            VisualStudioInstance.Editor.SetText(TestSource);
            VisualStudioInstance.Editor.PlaceCaret("a = 5", charsOffset: -1);
            VisualStudioInstance.Editor.PlaceCaret("a * b", charsOffset: 1, extendSelection: true);
            try
            {
                VisualStudioInstance.Workspace.SetFeatureOption("ExtractMethodOptions", "AllowMovingDeclaration", LanguageNames.CSharp, "true");
                VisualStudioInstance.Editor.Verify.CodeAction("Extract Method + Local", applyFix: true, blockUntilComplete: true);

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
                Assert.AreEqual(expectedText, VisualStudioInstance.Editor.GetText());
                AssertEx.SetEqual(spans, VisualStudioInstance.Editor.GetTagSpans(VisualStudioInstance.InlineRenameDialog.ValidRenameTag));
            }
            finally
            {
                VisualStudioInstance.Workspace.SetFeatureOption("ExtractMethodOptions", "AllowMovingDeclaration", LanguageNames.CSharp, "false");
            }
        }
    }
}
