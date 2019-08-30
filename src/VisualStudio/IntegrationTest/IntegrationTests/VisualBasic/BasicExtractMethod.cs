// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicExtractMethod : AbstractEditorTest
    {
        private const string TestSource = @"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Console.WriteLine(""Hello VB!"")
    End Sub

    Function F() As Integer
        Dim a As Integer
        Dim b As Integer
        a = 5
        b = 5
        Dim result = a * b
        Return result
    End Function
End Module";

        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicExtractMethod(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(BasicExtractMethod))
        {
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SimpleExtractMethod()
        {
            VisualStudio.Editor.SetText(TestSource);
            VisualStudio.Editor.PlaceCaret("Console", charsOffset: -1);
            VisualStudio.Editor.PlaceCaret("Hello VB!", charsOffset: 3, extendSelection: true);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Refactor_ExtractMethod);

            var expectedMarkup = @"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        [|NewMethod|]()
    End Sub

    Private Sub [|NewMethod|]()
        Console.WriteLine(""Hello VB!"")
    End Sub

    Function F() As Integer
        Dim a As Integer
        Dim b As Integer
        a = 5
        b = 5
        Dim result = a * b
        Return result
    End Function
End Module";

            MarkupTestFile.GetSpans(expectedMarkup, out var expectedText, out ImmutableArray<TextSpan> spans);
            VisualStudio.Editor.Verify.TextContains(expectedText);
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Rename);
            AssertEx.SetEqual(spans, VisualStudio.Editor.GetTagSpans(VisualStudio.InlineRenameDialog.ValidRenameTag));

            VisualStudio.Editor.SendKeys("SayHello", VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"    Private Sub SayHello()
        Console.WriteLine(""Hello VB!"")
    End Sub");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractViaCodeAction()
        {
            VisualStudio.Editor.SetText(TestSource);
            VisualStudio.Editor.PlaceCaret("a = 5", charsOffset: -1);
            VisualStudio.Editor.PlaceCaret("a * b", charsOffset: 1, extendSelection: true);
            VisualStudio.Editor.Verify.CodeAction("Extract Method", applyFix: true, blockUntilComplete: true);

            var expectedMarkup = @"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Console.WriteLine(""Hello VB!"")
    End Sub

    Function F() As Integer
        Dim a As Integer
        Dim b As Integer
        Dim result As Integer = Nothing
        [|NewMethod|](a, b, result)
        Return result
    End Function

    Private Sub [|NewMethod|](ByRef a As Integer, ByRef b As Integer, ByRef result As Integer)
        a = 5
        b = 5
        result = a * b
    End Sub
End Module";

            MarkupTestFile.GetSpans(expectedMarkup, out var expectedText, out ImmutableArray<TextSpan> spans);
            Assert.Equal(expectedText, VisualStudio.Editor.GetText());
            AssertEx.SetEqual(spans, VisualStudio.Editor.GetTagSpans(VisualStudio.InlineRenameDialog.ValidRenameTag));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractViaCodeActionWithMoveLocal()
        {
            VisualStudio.Editor.SetText(TestSource);
            VisualStudio.Editor.PlaceCaret("a = 5", charsOffset: -1);
            VisualStudio.Editor.PlaceCaret("a * b", charsOffset: 1, extendSelection: true);
            try
            {
                VisualStudio.Workspace.SetFeatureOption("ExtractMethodOptions", "AllowMovingDeclaration", LanguageNames.VisualBasic, "true");
                VisualStudio.Editor.Verify.CodeAction("Extract Method + Local", applyFix: true, blockUntilComplete: true);

                var expectedMarkup = @"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Console.WriteLine(""Hello VB!"")
    End Sub

    Function F() As Integer
        Dim result As Integer = [|NewMethod|]()
        Return result
    End Function

    Private Function [|NewMethod|]() As Integer
        Dim a, b As Integer
        a = 5
        b = 5
        Dim result = a * b
        Return result
    End Function
End Module";

                MarkupTestFile.GetSpans(expectedMarkup, out var expectedText, out ImmutableArray<TextSpan> spans);
                Assert.Equal(expectedText, VisualStudio.Editor.GetText());
                AssertEx.SetEqual(spans, VisualStudio.Editor.GetTagSpans(VisualStudio.InlineRenameDialog.ValidRenameTag));
            }
            finally
            {
                VisualStudio.Workspace.SetFeatureOption("ExtractMethodOptions", "AllowMovingDeclaration", LanguageNames.VisualBasic, "false");
            }
        }
    }
}
