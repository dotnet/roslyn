﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicRename : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        private InlineRenameDialog_OutOfProc InlineRenameDialog => VisualStudio.InlineRenameDialog;

        public BasicRename(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicRename))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyLocalVariableRename()
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
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
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

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyLocalVariableRenameWithCommentsUpdated()
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
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();
            InlineRenameDialog.ToggleIncludeComments();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
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

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyLocalVariableRenameWithStringsUpdated()
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
            SetUpEditor(markup);

            InlineRenameDialog.Invoke();
            InlineRenameDialog.ToggleIncludeStrings();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
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

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyOverloadsUpdated()
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
            SetUpEditor(markup);

            InlineRenameDialog.Invoke();
            InlineRenameDialog.ToggleIncludeOverloads();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
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
