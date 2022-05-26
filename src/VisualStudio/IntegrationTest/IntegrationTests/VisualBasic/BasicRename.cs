// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
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

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // reset relevant global options to default values:
            VisualStudio.Workspace.SetGlobalOption(WellKnownGlobalOption.InlineRenameSessionOptions_RenameInComments, language: null, value: false);
            VisualStudio.Workspace.SetGlobalOption(WellKnownGlobalOption.InlineRenameSessionOptions_RenameInStrings, language: null, value: false);
            VisualStudio.Workspace.SetGlobalOption(WellKnownGlobalOption.InlineRenameSessionOptions_RenameOverloads, language: null, value: false);
            VisualStudio.Workspace.SetGlobalOption(WellKnownGlobalOption.InlineRenameSessionOptions_RenameFile, language: null, value: true);
            VisualStudio.Workspace.SetGlobalOption(WellKnownGlobalOption.InlineRenameSessionOptions_PreviewChanges, language: null, value: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        [WorkItem(21657, "https://github.com/dotnet/roslyn/issues/21657")]
        public void VerifyAttributeRename()
        {
            var markup = @"
Import System;

Public Class [|$$ustom|]Attribute 
        Inherits Attribute
End Class";
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys("Custom", VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
Import System;

Public Class CustomAttribute
    Inherits Attribute
End Class");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        [WorkItem(21657, "https://github.com/dotnet/roslyn/issues/21657")]
        public void VerifyAttributeRenameWhileRenameClasss()
        {
            var markup = @"
Import System;

Public Class [|$$ustom|]Attribute 
        Inherits Attribute
End Class";

            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys("Custom");
            VisualStudio.Editor.Verify.TextContains(@"
Import System;

Public Class Custom$$Attribute 
        Inherits Attribute
End Class", true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        [WorkItem(21657, "https://github.com/dotnet/roslyn/issues/21657")]
        public void VerifyAttributeRenameWhileRenameAttribute()
        {
            var markup = @"
Import System;

<[|$$ustom|]>
Class Bar
End Class

Public Class ustomAttribute 
        Inherits Attribute
End Class";
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out _, out ImmutableArray<TextSpan> _);
            _ = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);

            VisualStudio.Editor.SendKeys("Custom");
            VisualStudio.Editor.Verify.TextContains(@"
Import System;

<Custom$$>
Class Bar
End Class

Public Class CustomAttribute 
        Inherits Attribute
End Class", true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        [WorkItem(21657, "https://github.com/dotnet/roslyn/issues/21657")]
        public void VerifyAttributeRenameWhileRenameAttributeClass()
        {
            var markup = @"
Import System;

<ustom>
Class Bar
End Class

Public Class [|$$ustom|]Attribute 
        Inherits Attribute
End Class";
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out _, out ImmutableArray<TextSpan> _);
            _ = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);

            VisualStudio.Editor.SendKeys("Custom");
            VisualStudio.Editor.Verify.TextContains(@"
Import System;

<Custom>
Class Bar
End Class

Public Class Custom$$Attribute 
        Inherits Attribute
End Class", true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        [WorkItem(21657, "https://github.com/dotnet/roslyn/issues/21657")]
        public void VerifyAttributeCapitalizedRename()
        {
            var markup = @"
Import System;

Public Class [|$$ustom|]ATTRIBUTE
        Inherits Attribute
End Class";
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys("Custom", VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
Import System;

Public Class CustomAttribute
    Inherits Attribute
End Class");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        [WorkItem(21657, "https://github.com/dotnet/roslyn/issues/21657")]
        public void VerifyAttributeNotCapitalizedRename()
        {
            var markup = @"
Import System;

Public Class [|$$ustom|]attribute
        Inherits Attribute
End Class";
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys("Custom", VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
Import System;

Public Class CustomAttribute
    Inherits Attribute
End Class");
        }
    }
}
