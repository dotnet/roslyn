// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicExtractMethod : AbstractIdeEditorTest
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

        public BasicExtractMethod()
            : base(nameof(BasicExtractMethod))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SimpleExtractMethodAsync()
        {
            await VisualStudio.Editor.SetTextAsync(TestSource);
            await VisualStudio.Editor.PlaceCaretAsync("Console", charsOffset: -1);
            await VisualStudio.Editor.PlaceCaretAsync("Hello VB!", charsOffset: 3, extendSelection: true);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Refactor_ExtractMethod);

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
            await VisualStudio.Editor.Verify.TextContainsAsync(expectedText);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Rename);
            AssertEx.SetEqual(spans, await VisualStudio.Editor.GetTagSpansAsync(RenameFieldBackgroundAndBorderTag.TagId));

            await VisualStudio.Editor.SendKeysAsync("SayHello", VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"    Private Sub SayHello()
        Console.WriteLine(""Hello VB!"")
    End Sub");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task ExtractViaCodeActionAsync()
        {
            await VisualStudio.Editor.SetTextAsync(TestSource);
            await VisualStudio.Editor.PlaceCaretAsync("a = 5", charsOffset: -1);
            await VisualStudio.Editor.PlaceCaretAsync("a * b", charsOffset: 1, extendSelection: true);
            await VisualStudio.Editor.Verify.CodeActionAsync("Extract Method", applyFix: true, willBlockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);

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
            Assert.Equal(expectedText, await VisualStudio.Editor.GetTextAsync());
            AssertEx.SetEqual(spans, await VisualStudio.Editor.GetTagSpansAsync(RenameFieldBackgroundAndBorderTag.TagId));
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task ExtractViaCodeActionWithMoveLocalAsync()
        {
            await VisualStudio.Editor.SetTextAsync(TestSource);
            await VisualStudio.Editor.PlaceCaretAsync("a = 5", charsOffset: -1);
            await VisualStudio.Editor.PlaceCaretAsync("a * b", charsOffset: 1, extendSelection: true);
            try
            {
                await VisualStudio.Workspace.SetFeatureOptionAsync(ExtractMethodOptions.AllowMovingDeclaration, LanguageNames.VisualBasic, true);
                await VisualStudio.Editor.Verify.CodeActionAsync("Extract Method + Local", applyFix: true, willBlockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);

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
                Assert.Equal(expectedText, await VisualStudio.Editor.GetTextAsync());
                AssertEx.SetEqual(spans, await VisualStudio.Editor.GetTagSpansAsync(RenameFieldBackgroundAndBorderTag.TagId));
            }
            finally
            {
                await VisualStudio.Workspace.SetFeatureOptionAsync(ExtractMethodOptions.AllowMovingDeclaration, LanguageNames.VisualBasic, false);
            }
        }
    }
}
