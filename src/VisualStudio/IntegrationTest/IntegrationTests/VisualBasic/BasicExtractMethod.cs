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

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [TestClass]
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

        public BasicExtractMethod( )
            : base( nameof(BasicExtractMethod))
        {
        }

        [TestMethod, TestCategory(Traits.Features.ExtractMethod)]
        public void SimpleExtractMethod()
        {
            VisualStudioInstance.Editor.SetText(TestSource);
            VisualStudioInstance.Editor.PlaceCaret("Console", charsOffset: -1);
            VisualStudioInstance.Editor.PlaceCaret("Hello VB!", charsOffset: 3, extendSelection: true);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Refactor_ExtractMethod);

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
            VisualStudioInstance.Editor.Verify.TextContains(expectedText);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Rename);
            AssertEx.SetEqual(spans, VisualStudioInstance.Editor.GetTagSpans(VisualStudioInstance.InlineRenameDialog.ValidRenameTag));

            VisualStudioInstance.Editor.SendKeys("SayHello", VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.TextContains(@"    Private Sub SayHello()
        Console.WriteLine(""Hello VB!"")
    End Sub");
        }

        [TestMethod, TestCategory(Traits.Features.ExtractMethod)]
        public void ExtractViaCodeAction()
        {
            VisualStudioInstance.Editor.SetText(TestSource);
            VisualStudioInstance.Editor.PlaceCaret("a = 5", charsOffset: -1);
            VisualStudioInstance.Editor.PlaceCaret("a * b", charsOffset: 1, extendSelection: true);
            VisualStudioInstance.Editor.Verify.CodeAction("Extract Method", applyFix: true, blockUntilComplete: true);

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
                VisualStudioInstance.Workspace.SetFeatureOption("ExtractMethodOptions", "AllowMovingDeclaration", LanguageNames.VisualBasic, "true");
                VisualStudioInstance.Editor.Verify.CodeAction("Extract Method + Local", applyFix: true, blockUntilComplete: true);

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
                Assert.AreEqual(expectedText, VisualStudioInstance.Editor.GetText());
                AssertEx.SetEqual(spans, VisualStudioInstance.Editor.GetTagSpans(VisualStudioInstance.InlineRenameDialog.ValidRenameTag));
            }
            finally
            {
                VisualStudioInstance.Workspace.SetFeatureOption("ExtractMethodOptions", "AllowMovingDeclaration", LanguageNames.VisualBasic, "false");
            }
        }
    }
}
