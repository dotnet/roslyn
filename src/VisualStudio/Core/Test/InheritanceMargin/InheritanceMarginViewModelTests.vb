' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Text
Imports System.Threading
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Documents
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.InheritanceMargin
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
Imports Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
Imports Microsoft.VisualStudio.Text.Classification
Imports Microsoft.VisualStudio.Threading
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.InheritanceMargin

    <Trait(Traits.Feature, Traits.Features.InheritanceMargin)>
    <UseExportProvider>
    Public Class InheritanceMarginViewModelTests

        Private Shared s_defaultMargin As Thickness = New Thickness(4, 1, 4, 1)

        Private Shared s_indentMargin As Thickness = New Thickness(20, 1, 4, 1)

        Private Shared Async Function VerifyAsync(markup As String, languageName As String, expectedViewModels As Dictionary(Of Integer, InheritanceMarginViewModel)) As Task
            ' Add an lf before the document so that the line number starts
            ' with 1, which meets the line number in the editor (but in fact all things start from 0)
            Dim workspaceFile =
            <Workspace>
                <Project Language=<%= languageName %> CommonReferences="true">
                    <Document><%= vbLf %>
                        <%= markup.Replace(vbCrLf, vbLf) %>
                    </Document>
                </Project>
            </Workspace>

            Dim cancellationToken As CancellationToken = CancellationToken.None
            Using workspace = TestWorkspace.Create(workspaceFile)
                Dim testDocument = workspace.Documents.Single()
                Dim document = workspace.CurrentSolution.GetDocument(testDocument.Id)
                Dim service = document.GetRequiredLanguageService(Of IInheritanceMarginService)

                Dim classificationTypeMap = workspace.ExportProvider.GetExportedValue(Of ClassificationTypeMap)
                Dim classificationFormatMap = workspace.ExportProvider.GetExportedValue(Of IClassificationFormatMapService)

                ' For these tests, we need to be on UI thread, so don't call ConfigureAwait(False)
                Dim root = Await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(True)
                Dim inheritanceItems = Await service.GetInheritanceMemberItemsAsync(document, root.FullSpan, cancellationToken).ConfigureAwait(True)

                Dim acutalLineToTagDictionary = inheritanceItems.GroupBy(Function(item) item.LineNumber) _
                    .ToDictionary(Function(grouping) grouping.Key,
                                  Function(grouping)
                                      Dim lineNumber = grouping.Key
                                      Dim items = grouping.Select(Function(g) g).ToImmutableArray()
                                      Return New InheritanceMarginTag(workspace, lineNumber, items)
                                  End Function)
                Assert.Equal(expectedViewModels.Count, acutalLineToTagDictionary.Count)

                For Each kvp In expectedViewModels
                    Dim lineNumber = kvp.Key
                    Dim expectedViewModel = kvp.Value
                    Assert.True(acutalLineToTagDictionary.ContainsKey(lineNumber))

                    Dim acutalTag = acutalLineToTagDictionary(lineNumber)
                    ' Editor TestView zoom level is 100 based.
                    Dim actualViewModel = InheritanceMarginViewModel.Create(
                        classificationTypeMap, classificationFormatMap.GetClassificationFormatMap("tooltip"), acutalTag, 100)

                    VerifyTwoViewModelAreSame(expectedViewModel, actualViewModel)
                Next

            End Using
        End Function

        Private Shared Sub VerifyTwoViewModelAreSame(expected As InheritanceMarginViewModel, actual As InheritanceMarginViewModel)
            Assert.Equal(expected.ImageMoniker, actual.ImageMoniker)
            Dim actualTextGetFromTextBlock = actual.ToolTipTextBlock.Inlines _
                .OfType(Of Run).Select(Function(run) run.Text) _
                .Aggregate(Function(text1, text2) text1 + text2)
            ' When the text block is created, a unicode 'left to right' would be inserted between the space.
            ' Make sure it is removed.
            Dim leftToRightMarker = Char.ConvertFromUtf32(&H200E)
            Dim actualText = actualTextGetFromTextBlock.Replace(leftToRightMarker, String.Empty)
            Assert.Equal(expected.ToolTipTextBlock.Text, actualText)
            Assert.Equal(expected.AutomationName, actual.AutomationName)
            Assert.Equal(expected.MenuItemViewModels.Length, actual.MenuItemViewModels.Length)
            Assert.Equal(expected.ScaleFactor, actual.ScaleFactor)

            For i = 0 To expected.MenuItemViewModels.Length - 1
                Dim expectedMenuItem = expected.MenuItemViewModels(i)
                Dim actualMenuItem = actual.MenuItemViewModels(i)
            Next

        End Sub

        Private Shared Sub VerifyMenuItem(expected As InheritanceMenuItemViewModel, actual As InheritanceMenuItemViewModel)
            Assert.Equal(expected.AutomationName, actual.AutomationName)
            Assert.Equal(expected.DisplayContent, actual.DisplayContent)
            Assert.Equal(expected.ImageMoniker, actual.ImageMoniker)

            Dim expectedTargetMenuItem = TryCast(expected, TargetMenuItemViewModel)
            Dim acutalTargetMenuItem = TryCast(actual, TargetMenuItemViewModel)
            If expectedTargetMenuItem IsNot Nothing AndAlso acutalTargetMenuItem IsNot Nothing Then
                Assert.Equal(expectedTargetMenuItem.Margin, acutalTargetMenuItem.Margin)
                Return
            End If

            Dim expectedMemberMenuItem = TryCast(expected, MemberMenuItemViewModel)
            Dim acutalMemberMenuItem = TryCast(actual, MemberMenuItemViewModel)
            If expectedTargetMenuItem IsNot Nothing AndAlso acutalTargetMenuItem IsNot Nothing Then
                Assert.Equal(expectedMemberMenuItem.Targets.Length, acutalMemberMenuItem.Targets.Length)
                For i = 0 To expectedMemberMenuItem.Targets.Length
                    VerifyMenuItem(expectedMemberMenuItem.Targets(i), acutalMemberMenuItem.Targets(i))
                Next
                Return
            End If

            ' At this stage, both of the items should be header
            Assert.True(TypeOf expected Is HeaderMenuItemViewModel)
            Assert.True(TypeOf actual Is HeaderMenuItemViewModel)
        End Sub

        Private Shared Function CreateTextBlock(text As String) As TextBlock
            Return New TextBlock With {
                .Text = text
            }
        End Function

        <WpfFact>
        Public Function TestClassImplementsInterfaceRelationship() As Task
            Dim markup = "
public interface IBar
{
}
public class Bar : IBar
{
}"
            Dim tooltipTextForIBar = String.Format(ServicesVSResources._0_is_inherited, "interface IBar")
            Dim targetForIBar = ImmutableArray.Create(
                New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)

            Dim tooltipTextForBar = String.Format(ServicesVSResources._0_is_inherited, "class Bar")
            Dim targetForBar = ImmutableArray.Create(
                New TargetMenuItemViewModel("IBar", KnownMonikers.InterfacePublic, "IBar", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {2, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForIBar),
                    tooltipTextForIBar,
                    1,
                    targetForIBar)},
                {5, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForBar),
                    tooltipTextForBar,
                    1,
                    targetForBar)}})
        End Function

        <WpfFact>
        Public Function TestClassOverridesAbstractClassRelationship() As Task
            Dim markup = "
public abstract class AbsBar
{
    public abstract void Foo();
}

public class Bar : AbsBar
{
    public override void Foo();
}"

            Dim tooltipTextForAbsBar = String.Format(ServicesVSResources._0_is_inherited, "class AbsBar")
            Dim targetForAbsBar = ImmutableArray.Create(
                New TargetMenuItemViewModel("Bar", KnownMonikers.Class, "Bar", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)

            Dim tooltipTextForAbstractFoo = String.Format(ServicesVSResources._0_is_inherited, "abstract void AbsBar.Foo()")
            Dim targetForAbsFoo = ImmutableArray.Create(
                New TargetMenuItemViewModel("AbsBar.Foo()", KnownMonikers.MethodPublic, "AbsBar.Foo()", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)

            Dim tooltipTextForBar = String.Format(ServicesVSResources._0_is_inherited, "class Bar")
            Dim targetForBar = ImmutableArray.Create(
                New TargetMenuItemViewModel("AbsBar", KnownMonikers.Class, "AbsBar", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)

            Dim tooltipTextForOverrideFoo = String.Format(ServicesVSResources._0_is_inherited, "override void Bar.Foo()")
            Dim targetForOverrideFoo = ImmutableArray.Create(
                New TargetMenuItemViewModel("Bar.Foo()", KnownMonikers.MethodPublic, "Bar.Foo()", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {2, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForAbsBar),
                    tooltipTextForAbsBar,
                    1,
                    targetForAbsBar)},
                {4, New InheritanceMarginViewModel(
                    KnownMonikers.Overridden,
                    CreateTextBlock(tooltipTextForAbstractFoo),
                    tooltipTextForAbstractFoo,
                    1,
                    targetForAbsFoo)},
                {7, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForBar),
                    tooltipTextForBar,
                    1,
                    targetForBar)},
                {9, New InheritanceMarginViewModel(
                    KnownMonikers.Overriding,
                    CreateTextBlock(tooltipTextForOverrideFoo),
                    tooltipTextForOverrideFoo,
                    1,
                    targetForOverrideFoo)}})
        End Function

        <WpfFact>
        Public Function TestInterfaceImplementsInterfaceRelationship() As Task
            Dim markup = "
public interface IBar1 { }
public interface IBar2 : IBar1 { }
public interface IBar3 : IBar2 { }
"
            Dim tooltipTextForIBar1 = String.Format(ServicesVSResources._0_is_inherited, "interface IBar1")
            Dim targetForIBar1 = ImmutableArray.Create(
                New TargetMenuItemViewModel("IBar2", KnownMonikers.InterfacePublic, "IBar2", Nothing, s_defaultMargin),
                New TargetMenuItemViewModel("IBar3", KnownMonikers.InterfacePublic, "IBar3", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)

            Dim tooltipTextForIBar2 = String.Format(ServicesVSResources._0_is_inherited, "interface IBar2")
            Dim targetForIBar2 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New HeaderMenuItemViewModel(ServicesVSResources.Implementing_members, KnownMonikers.Implementing, ServicesVSResources.Implementing_members),
                New TargetMenuItemViewModel("IBar1", KnownMonikers.InterfacePublic, "IBar1", Nothing, s_indentMargin),
                New HeaderMenuItemViewModel(ServicesVSResources.Implemented_members, KnownMonikers.Implemented, ServicesVSResources.Implemented_members),
                New TargetMenuItemViewModel("IBar3", KnownMonikers.InterfacePublic, "IBar3", Nothing, s_indentMargin))

            Dim tooltipTextForIBar3 = String.Format(ServicesVSResources._0_is_inherited, "interface IBar3")
            Dim targetForIBar3 = ImmutableArray.Create(
                New TargetMenuItemViewModel("IBar1", KnownMonikers.InterfacePublic, "IBar1", Nothing, s_defaultMargin),
                New TargetMenuItemViewModel("IBar2", KnownMonikers.InterfacePublic, "IBar2", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {2, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForIBar1),
                    tooltipTextForIBar1,
                    1,
                    targetForIBar1)},
                {3, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForIBar2),
                    tooltipTextForIBar2,
                    1,
                    targetForIBar2)},
                {4, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForIBar3),
                    tooltipTextForIBar3,
                    1,
                    targetForIBar3)}})
        End Function

        <WpfFact>
        Public Function TestMutipleMemberOnSameline() As Task
            Dim markup = "
using System;
interface IBar1
{
    public event EventHandler e1, e2;
}

public class BarSample : IBar1
{
    public virtual event EventHandler e1, e2;
}"

            Dim tooltipTextForIBar1 = String.Format(ServicesVSResources._0_is_inherited, "interface IBar1")
            Dim targetForIBar1 = ImmutableArray.Create(
                New TargetMenuItemViewModel("BarSample", KnownMonikers.ClassPublic, "BarSample", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)

            Dim tooltipTextForE1AndE2InInterface = ServicesVSResources.Multiple_members_are_inherited
            Dim targetForE1AndE2InInterface = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New MemberMenuItemViewModel("event EventHandler e1", KnownMonikers.EventPublic, "event EventHandler e1", ImmutableArray.Create(
                    New TargetMenuItemViewModel("BarSample.e1", KnownMonikers.EventPublic, "BarSample.e1", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)),
                New MemberMenuItemViewModel("event EventHandler e2", KnownMonikers.EventPublic, "event EventHandler e2", ImmutableArray.Create(
                    New TargetMenuItemViewModel("BarSample.e2", KnownMonikers.EventPublic, "BarSample.e2", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel))) _
            .CastArray(Of InheritanceMenuItemViewModel)

            Dim tooltipTextForBarSample = String.Format(ServicesVSResources._0_is_inherited, "class BarSample")
            Dim targetForBarSample = ImmutableArray.Create(
                New TargetMenuItemViewModel("IBar1", KnownMonikers.InterfacePublic, "IBar1", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)

            Dim tooltipTextForE1AndE2InBarSample = ServicesVSResources.Multiple_members_are_inherited
            Dim targetForE1AndE2InInBarSample = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New MemberMenuItemViewModel("event EventHandler e1", KnownMonikers.EventPublic, "event EventHandler e1", ImmutableArray.Create(
                    New TargetMenuItemViewModel("IBar1.BarSample.e1", KnownMonikers.EventPublic, "IBar1.BarSample.e1", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel)),
                New MemberMenuItemViewModel("event EventHandler e2", KnownMonikers.EventPublic, "event EventHandler e2", ImmutableArray.Create(
                    New TargetMenuItemViewModel("IBar1.BarSample.e2", KnownMonikers.EventPublic, "IBar1.BarSample.e2", Nothing, s_defaultMargin)).CastArray(Of InheritanceMenuItemViewModel))) _
            .CastArray(Of InheritanceMenuItemViewModel)

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {3, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForIBar1),
                    tooltipTextForIBar1,
                    1,
                    targetForIBar1)},
                {5, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForE1AndE2InInterface),
                    String.Format(ServicesVSResources.Multiple_members_are_inherited_on_line_0, 5),
                    1,
                    targetForE1AndE2InInterface)},
                {8, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForBarSample),
                    tooltipTextForBarSample,
                    1,
                    targetForBarSample)},
                {10, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForE1AndE2InBarSample),
                    String.Format(ServicesVSResources.Multiple_members_are_inherited_on_line_0, 10),
                    1,
                    targetForE1AndE2InInBarSample)}})
        End Function
    End Class
End Namespace
