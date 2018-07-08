// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicRename : AbstractIdeEditorTest
    {
        public BasicRename()
            : base(nameof(BasicRename))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        private InlineRenameDialog_InProc2 InlineRenameDialog => VisualStudio.InlineRenameDialog;

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyLocalVariableRenameAsync()
        {
            var markup = @"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim [|x|]$$ As Integer = 0
        [|x|] = 5
        TestMethod([|x|])
    End Sub
    Sub TestMethod(y As Integer)

    End Sub
End Module";
            await SetUpEditorAsync(markup);
            await InlineRenameDialog.InvokeAsync();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = await VisualStudio.Editor.GetTagSpansAsync(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y, VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim y As Integer = 0
        y = 5
        TestMethod(y)
    End Sub
    Sub TestMethod(y As Integer)

    End Sub
End Module");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyLocalVariableRenameWithCommentsUpdatedAsync()
        {
            // "variable" is intentionally misspelled as "varixable" and "this" is misspelled as
            // "thix" below to ensure we don't change instances of "x" in comments that are part of
            // larger words
            var markup = @"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    ''' <summary>
    ''' creates a varixable named [|x|] xx
    ''' </summary>
    ''' <param name=""args""></param>
    Sub Main(args As String())
        ' thix varixable is named [|x|] xx
        Dim [|x|]$$ As Integer = 0
        [|x|] = 5
        TestMethod([|x|])
End Module";
            await SetUpEditorAsync(markup);
            await InlineRenameDialog.InvokeAsync();
            await InlineRenameDialog.ToggleIncludeCommentsAsync();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = await VisualStudio.Editor.GetTagSpansAsync(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y, VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    ''' <summary>
    ''' creates a varixable named y xx
    ''' </summary>
    ''' <param name=""args""></param>
    Sub Main(args As String())
        ' thix varixable is named y xx
        Dim y As Integer = 0
        y = 5
        TestMethod(y)
End Module");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyLocalVariableRenameWithStringsUpdatedAsync()
        {
            var markup = @"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim [|x|]$$ As Integer = 0
        [|x|] = 5
        Dim s = ""[|x|] xx [|x|]""
    End Sub
End Module";
            await SetUpEditorAsync(markup);

            await InlineRenameDialog.InvokeAsync();
            await InlineRenameDialog.ToggleIncludeStringsAsync();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = await VisualStudio.Editor.GetTagSpansAsync(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y, VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim y As Integer = 0
        y = 5
        Dim s = ""y xx y""
    End Sub
End Module");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyOverloadsUpdatedAsync()
        {
            var markup = @"
Interface I
    Sub [|TestMethod|]$$(y As Integer)
    Sub [|TestMethod|](y As String)
End Interface

Public MustInherit Class A
    Implements I
    Public MustOverride Sub [|TestMethod|](y As Integer) Implements I.[|TestMethod|]
    Public MustOverride Sub [|TestMethod|](y As String) Implements I.[|TestMethod|]
End Class";
            await SetUpEditorAsync(markup);

            await InlineRenameDialog.InvokeAsync();
            await InlineRenameDialog.ToggleIncludeOverloadsAsync();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = await VisualStudio.Editor.GetTagSpansAsync(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y, VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Interface I
    Sub y(y As Integer)
    Sub y(y As String)
End Interface

Public MustInherit Class A
    Implements I
    Public MustOverride Sub y(y As Integer) Implements I.y
    Public MustOverride Sub y(y As String) Implements I.y
End Class");
        }
    }
}
