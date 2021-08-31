' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
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
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.InheritanceMargin

    <Trait(Traits.Feature, Traits.Features.InheritanceMargin)>
    <UseExportProvider>
    Public Class InheritanceMarginViewModelTests

        Private Shared s_defaultMargin As Thickness = New Thickness(4, 1, 4, 1)

        Private Shared s_indentMargin As Thickness = New Thickness(22, 1, 4, 1)

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
                VerifyMenuItem(expectedMenuItem, actualMenuItem)
            Next

        End Sub

        Private Shared Sub VerifyMenuItem(expected As InheritanceMenuItemViewModel, actual As InheritanceMenuItemViewModel)
            Assert.Equal(expected.AutomationName, actual.AutomationName)
            Assert.Equal(expected.DisplayContent, actual.DisplayContent)
            Assert.Equal(expected.ImageMoniker, actual.ImageMoniker)

            Dim expectedTargetMenuItem = TryCast(expected, TargetMenuItemViewModel)
            Dim acutalTargetMenuItem = TryCast(actual, TargetMenuItemViewModel)
            If expectedTargetMenuItem IsNot Nothing AndAlso acutalTargetMenuItem IsNot Nothing Then
                Return
            End If

            Dim expectedMemberMenuItem = TryCast(expected, MemberMenuItemViewModel)
            Dim acutalMemberMenuItem = TryCast(actual, MemberMenuItemViewModel)
            If expectedMemberMenuItem IsNot Nothing AndAlso acutalMemberMenuItem IsNot Nothing Then
                Assert.Equal(expectedMemberMenuItem.Targets.Length, acutalMemberMenuItem.Targets.Length)
                For i = 0 To expectedMemberMenuItem.Targets.Length - 1
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
    void Sub();
}
public class Bar : IBar
{
    public void Sub() { };
}"
            Dim tooltipTextForIBar = String.Format(ServicesVSResources._0_is_implemented_by_1, "IBar", "Bar")
            Dim targetForIBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implementing_types, KnownMonikers.Implemented, ServicesVSResources.Implementing_types)).
                Add(New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing))

            Dim tooltipTextForSubOfIBar = String.Format(ServicesVSResources._0_is_implemented_by_1, "IBar.Sub", "Bar.Sub")
            Dim targetForIBarSub = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implementing_members, KnownMonikers.Implemented, ServicesVSResources.Implementing_members)).
                Add(New TargetMenuItemViewModel("Bar.Sub", KnownMonikers.MethodPublic, "Bar.Sub", Nothing))

            Dim tooltipTextForSubOfBar = String.Format(ServicesVSResources._0_implements_1, "Bar.Sub", "IBar.Sub")
            Dim targetForSubOfBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_members, KnownMonikers.Implementing, ServicesVSResources.Implemented_members)).
                Add(New TargetMenuItemViewModel("IBar.Sub", KnownMonikers.MethodPublic, "IBar.Sub", Nothing))

            Dim tooltipTextForBar = String.Format(ServicesVSResources._0_implements_1, "Bar", "IBar")
            Dim targetForBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, ServicesVSResources.Implemented_interfaces)).
                Add(New TargetMenuItemViewModel("IBar", KnownMonikers.InterfacePublic, "IBar", Nothing))

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {2, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForIBar),
                    tooltipTextForIBar,
                    1,
                    targetForIBar)},
                {4, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForSubOfIBar),
                    tooltipTextForSubOfIBar,
                    1,
                    targetForIBarSub)},
                {6, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForBar),
                    tooltipTextForBar,
                    1,
                    targetForBar)},
                {8, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForSubOfBar),
                    tooltipTextForSubOfBar,
                    1,
                    targetForSubOfBar)}})
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

            Dim tooltipTextForAbsBar = String.Format(ServicesVSResources._0_derives_1, "AbsBar", "Bar")
            Dim targetForAbsBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Derived_types, KnownMonikers.Overridden, ServicesVSResources.Derived_types)).
                Add(New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing))

            Dim tooltipTextForAbstractFoo = String.Format(ServicesVSResources._0_is_overridden_by_1, "AbsBar.Foo", "Bar.Foo")
            Dim targetForAbsFoo = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Overriding_members, KnownMonikers.Overridden, ServicesVSResources.Overriding_members)).
                Add(New TargetMenuItemViewModel("Bar.Foo", KnownMonikers.MethodPublic, "Bar.Foo", Nothing))

            Dim tooltipTextForBar = String.Format(ServicesVSResources._0_is_derived_from_1, "Bar", "AbsBar")
            Dim targetForBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Base_Types, KnownMonikers.Overriding, ServicesVSResources.Base_Types)).
                Add(New TargetMenuItemViewModel("AbsBar", KnownMonikers.ClassPublic, "AbsBar", Nothing))

            Dim tooltipTextForOverrideFoo = String.Format(ServicesVSResources._0_overrides_1, "Bar.Foo", "AbsBar.Foo")
            Dim targetForOverrideFoo = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Overridden_members, KnownMonikers.Overriding, ServicesVSResources.Overridden_members)).
                Add(New TargetMenuItemViewModel("AbsBar.Foo", KnownMonikers.MethodPublic, "AbsBar.Foo", Nothing))

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {2, New InheritanceMarginViewModel(
                    KnownMonikers.Overridden,
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
                    KnownMonikers.Overriding,
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
            Dim tooltipTextForIBar1 = String.Format(ServicesVSResources._0_has_multiple_implementing_types, "IBar1")
            Dim targetForIBar1 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New HeaderMenuItemViewModel(ServicesVSResources.Implementing_types, KnownMonikers.Implemented, ServicesVSResources.Implementing_types),
                New TargetMenuItemViewModel("IBar2", KnownMonikers.InterfacePublic, "IBar2", Nothing),
                New TargetMenuItemViewModel("IBar3", KnownMonikers.InterfacePublic, "IBar3", Nothing))

            Dim tooltipTextForIBar2 = String.Format(ServicesVSResources._0_has_inherited_interfaces_and_implementing_types, "IBar2")
            Dim targetForIBar2 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New HeaderMenuItemViewModel(ServicesVSResources.Inherited_interfaces, KnownMonikers.Implementing, ServicesVSResources.Inherited_interfaces),
                New TargetMenuItemViewModel("IBar1", KnownMonikers.InterfacePublic, "IBar1", Nothing),
                New HeaderMenuItemViewModel(ServicesVSResources.Implementing_types, KnownMonikers.Implemented, ServicesVSResources.Implementing_types),
                New TargetMenuItemViewModel("IBar3", KnownMonikers.InterfacePublic, "IBar3", Nothing))

            Dim tooltipTextForIBar3 = String.Format(ServicesVSResources._0_has_multiple_inherited_interfaces, "IBar3")
            Dim targetForIBar3 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New HeaderMenuItemViewModel(ServicesVSResources.Inherited_interfaces, KnownMonikers.Implementing, ServicesVSResources.Inherited_interfaces),
                New TargetMenuItemViewModel("IBar1", KnownMonikers.InterfacePublic, "IBar1", Nothing),
                New TargetMenuItemViewModel("IBar2", KnownMonikers.InterfacePublic, "IBar2", Nothing)).CastArray(Of InheritanceMenuItemViewModel)

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {2, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForIBar1),
                    tooltipTextForIBar1,
                    1,
                    targetForIBar1)},
                {3, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
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

            Dim tooltipTextForIBar1 = String.Format(ServicesVSResources._0_is_implemented_by_1, "IBar1", "BarSample")
            Dim targetForIBar1 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New HeaderMenuItemViewModel(ServicesVSResources.Implementing_types, KnownMonikers.Implemented, ServicesVSResources.Implementing_types),
                New TargetMenuItemViewModel("BarSample", KnownMonikers.ClassPublic, "BarSample", Nothing))

            Dim tooltipTextForE1AndE2InInterface = ServicesVSResources.Multiple_members_are_inherited
            Dim targetForE1AndE2InInterface = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New MemberMenuItemViewModel("IBar1.e1", KnownMonikers.EventPublic, "IBar1.e1", ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                    New HeaderMenuItemViewModel(ServicesVSResources.Implementing_members, KnownMonikers.Implemented, ServicesVSResources.Implementing_members),
                    New TargetMenuItemViewModel("BarSample.e1", KnownMonikers.EventPublic, "BarSample.e1", Nothing))),
                New MemberMenuItemViewModel("IBar1.e2", KnownMonikers.EventPublic, "IBar1.e2", ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                    New HeaderMenuItemViewModel(ServicesVSResources.Implementing_members, KnownMonikers.Implemented, ServicesVSResources.Implementing_members),
                    New TargetMenuItemViewModel("BarSample.e2", KnownMonikers.EventPublic, "BarSample.e2", Nothing))))

            Dim tooltipTextForBarSample = String.Format(ServicesVSResources._0_implements_1, "BarSample", "IBar1")
            Dim targetForBarSample = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New HeaderMenuItemViewModel(ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, ServicesVSResources.Implemented_interfaces),
                New TargetMenuItemViewModel("IBar1", KnownMonikers.InterfaceInternal, "IBar1", Nothing))

            Dim tooltipTextForE1AndE2InBarSample = ServicesVSResources.Multiple_members_are_inherited
            Dim targetForE1AndE2InInBarSample = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New MemberMenuItemViewModel("BarSample.e1", KnownMonikers.EventPublic, "BarSample.e1", ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                    New HeaderMenuItemViewModel(ServicesVSResources.Implemented_members, KnownMonikers.Implementing, ServicesVSResources.Implemented_members),
                    New TargetMenuItemViewModel("IBar1.e1", KnownMonikers.EventPublic, "IBar1.e1", Nothing)).CastArray(Of InheritanceMenuItemViewModel)),
                New MemberMenuItemViewModel("BarSample.e2", KnownMonikers.EventPublic, "BarSample.e2", ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                    New HeaderMenuItemViewModel(ServicesVSResources.Implemented_members, KnownMonikers.Implementing, ServicesVSResources.Implemented_members),
                    New TargetMenuItemViewModel("IBar1.e2", KnownMonikers.EventPublic, "IBar1.e2", Nothing)).CastArray(Of InheritanceMenuItemViewModel))) _
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

        <WpfFact>
        Public Function TestSingleInterfaceImplementsSingleInterface() As Task
            Dim markup = "
interface IBar1
{
}

interface IBar2 : IBar1
{
}"

            Dim tooltipTextForIBar1 = String.Format(ServicesVSResources._0_is_implemented_by_1, "IBar1", "IBar2")
            Dim targetForIBar1 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New HeaderMenuItemViewModel(ServicesVSResources.Implementing_types, KnownMonikers.Implemented, ServicesVSResources.Implementing_types),
                New TargetMenuItemViewModel("IBar2", KnownMonikers.InterfaceInternal, "IBar2", Nothing))

            Dim tooltipTextForIBar2 = String.Format(ServicesVSResources._0_is_inherited_from_1, "IBar2", "IBar1")
            Dim targetForIBar2 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(
                New HeaderMenuItemViewModel(ServicesVSResources.Inherited_interfaces, KnownMonikers.Implementing, ServicesVSResources.Inherited_interfaces),
                New TargetMenuItemViewModel("IBar1", KnownMonikers.InterfaceInternal, "IBar1", Nothing))

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {2, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForIBar1),
                    tooltipTextForIBar1,
                    1,
                    targetForIBar1)},
                {6, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForIBar2),
                    tooltipTextForIBar2,
                    1,
                    targetForIBar2)}})
        End Function

        <WpfFact>
        Public Function TestClassImplementsMultipleInterfaces() As Task
            Dim markup = "
public interface IBar1 { }
public interface IBar2 : IBar1 { }
public class Bar : IBar2
{
}"
            Dim tooltipTextForIBar1 = String.Format(ServicesVSResources._0_has_multiple_implementing_types, "IBar1")
            Dim targetForIBar1 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implementing_types, KnownMonikers.Implemented, ServicesVSResources.Implementing_types)).
                Add(New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing)).
                Add(New TargetMenuItemViewModel("IBar2", KnownMonikers.InterfacePublic, "IBar2", Nothing))

            Dim tooltipTextForIBar2 = String.Format(ServicesVSResources._0_has_inherited_interfaces_and_implementing_types, "IBar2")
            Dim targetForIBar2 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Inherited_interfaces, KnownMonikers.Implementing, ServicesVSResources.Inherited_interfaces)).
                Add(New TargetMenuItemViewModel("IBar1", KnownMonikers.InterfacePublic, "IBar1", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Implementing_types, KnownMonikers.Implemented, ServicesVSResources.Implementing_types)).
                Add(New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing))

            Dim tooltipTextForBar = String.Format(ServicesVSResources._0_has_multiple_implemented_interfaces, "Bar")
            Dim targetForBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, ServicesVSResources.Implemented_interfaces)).
                Add(New TargetMenuItemViewModel("IBar1", KnownMonikers.InterfacePublic, "IBar1", Nothing)).
                Add(New TargetMenuItemViewModel("IBar2", KnownMonikers.InterfacePublic, "IBar2", Nothing))

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {2, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForIBar1),
                    tooltipTextForIBar1,
                    1,
                    targetForIBar1)},
                {3, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForIBar2),
                    tooltipTextForIBar2,
                    1,
                    targetForIBar2)},
                {4, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForBar),
                    tooltipTextForBar,
                    1,
                    targetForBar)}})
        End Function

        <WpfFact>
        Public Function TestClassOverridesMultipleAbstractClasses() As Task
            Dim markup = "
public abstract class AbsBar
{
    public abstract void Goo();
}

public class Bar2 : AbsBar
{
    public override void Goo() { }
}

public class Bar : Bar2
{
    public override void Goo() { }
}"

            Dim tooltipTextForAbsBar = String.Format(ServicesVSResources._0_has_multiple_derived_types, "AbsBar")
            Dim targetForAbsBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Derived_types, KnownMonikers.Overridden, ServicesVSResources.Derived_types)).
                Add(New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing)).
                Add(New TargetMenuItemViewModel("Bar2", KnownMonikers.ClassPublic, "Bar2", Nothing))

            Dim tooltipTextForAbstractGoo = String.Format(ServicesVSResources._0_is_overridden_by_members_from_multiple_classes, "AbsBar.Goo")
            Dim targetForAbsGoo = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Overriding_members, KnownMonikers.Overridden, ServicesVSResources.Overriding_members)).
                Add(New TargetMenuItemViewModel("Bar.Goo", KnownMonikers.MethodPublic, "Bar.Goo", Nothing)).
                Add(New TargetMenuItemViewModel("Bar2.Goo", KnownMonikers.MethodPublic, "Bar2.Goo", Nothing))

            Dim tooltipTextForBar2 = String.Format(ServicesVSResources._0_has_base_types_and_derived_types, "Bar2")
            Dim targetForBar2 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Base_Types, KnownMonikers.Overriding, ServicesVSResources.Base_Types)).
                Add(New TargetMenuItemViewModel("AbsBar", KnownMonikers.ClassPublic, "AbsBar", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Derived_types, KnownMonikers.Overridden, ServicesVSResources.Derived_types)).
                Add(New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing))

            Dim tooltipTextForOverrideGoo = String.Format(ServicesVSResources._0_has_overridden_members_and_overriding_members, "Bar2.Goo")
            Dim targetForOverrideFoo = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Overridden_members, KnownMonikers.Overriding, ServicesVSResources.Overridden_members)).
                Add(New TargetMenuItemViewModel("AbsBar.Goo", KnownMonikers.MethodPublic, "AbsBar.Goo", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Overriding_members, KnownMonikers.Overridden, ServicesVSResources.Overriding_members)).
                Add(New TargetMenuItemViewModel("Bar.Goo", KnownMonikers.MethodPublic, "Bar.Goo", Nothing))

            Dim tooltipTextForBar = String.Format(ServicesVSResources._0_has_multiple_base_types, "Bar")
            Dim targetForBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Base_Types, KnownMonikers.Overriding, ServicesVSResources.Base_Types)).
                Add(New TargetMenuItemViewModel("AbsBar", KnownMonikers.ClassPublic, "AbsBar", Nothing)).
                Add(New TargetMenuItemViewModel("Bar2", KnownMonikers.ClassPublic, "Bar2", Nothing))

            Dim tooltipTextForGooInBar = String.Format(ServicesVSResources._0_overrides_members_from_multiple_classes, "Bar.Goo")
            Dim targetForAbsGooInBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Overridden_members, KnownMonikers.Overriding, ServicesVSResources.Overridden_members)).
                Add(New TargetMenuItemViewModel("AbsBar.Goo", KnownMonikers.MethodPublic, "AbsBar.Goo", Nothing)).
                Add(New TargetMenuItemViewModel("Bar2.Goo", KnownMonikers.MethodPublic, "Bar2.Goo", Nothing))

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {2, New InheritanceMarginViewModel(
                    KnownMonikers.Overridden,
                    CreateTextBlock(tooltipTextForAbsBar),
                    tooltipTextForAbsBar,
                    1,
                    targetForAbsBar)},
                {4, New InheritanceMarginViewModel(
                    KnownMonikers.Overridden,
                    CreateTextBlock(tooltipTextForAbstractGoo),
                    tooltipTextForAbstractGoo,
                    1,
                    targetForAbsGoo)},
                {7, New InheritanceMarginViewModel(
                    KnownMonikers.Overriding,
                    CreateTextBlock(tooltipTextForBar2),
                    tooltipTextForBar2,
                    1,
                    targetForBar2)},
                {9, New InheritanceMarginViewModel(
                    KnownMonikers.Overriding,
                    CreateTextBlock(tooltipTextForOverrideGoo),
                    tooltipTextForOverrideGoo,
                    1,
                    targetForOverrideFoo)},
                {12, New InheritanceMarginViewModel(
                    KnownMonikers.Overriding,
                    CreateTextBlock(tooltipTextForBar),
                    tooltipTextForBar,
                    1,
                    targetForBar)},
                {14, New InheritanceMarginViewModel(
                    KnownMonikers.Overriding,
                    CreateTextBlock(tooltipTextForGooInBar),
                    tooltipTextForGooInBar,
                    1,
                    targetForAbsGooInBar)}})
        End Function

        <WpfFact>
        Public Function TestClassImplementsMultipleInterfaceMembers() As Task
            Dim markup = "
public interface IBar1
{
    void Goo();
}
public interface IBar2
{
    void Goo();
}
public class Bar : IBar1, IBar2
{
    public void Goo() { }
}"
            Dim tooltipTextForIBar1 = String.Format(ServicesVSResources._0_is_implemented_by_1, "IBar1", "Bar")
            Dim targetForIBar1 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implementing_types, KnownMonikers.Implemented, ServicesVSResources.Implementing_types)).
                Add(New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing))

            Dim tooltipTextForGooOfIBar1 = String.Format(ServicesVSResources._0_is_implemented_by_1, "IBar1.Goo", "Bar.Goo")
            Dim targetForGooOfIBar1 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implementing_members, KnownMonikers.Implemented, ServicesVSResources.Implementing_members)).
                Add(New TargetMenuItemViewModel("Bar.Goo", KnownMonikers.MethodPublic, "Bar.Goo", Nothing))

            Dim tooltipTextForIBar2 = String.Format(ServicesVSResources._0_is_implemented_by_1, "IBar2", "Bar")
            Dim targetForIBar2 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implementing_types, KnownMonikers.Implemented, ServicesVSResources.Implementing_types)).
                Add(New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing))

            Dim tooltipTextForGooOfIBar2 = String.Format(ServicesVSResources._0_is_implemented_by_1, "IBar2.Goo", "Bar.Goo")
            Dim targetForGooOfIBar2 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implementing_members, KnownMonikers.Implemented, ServicesVSResources.Implementing_members)).
                Add(New TargetMenuItemViewModel("Bar.Goo", KnownMonikers.MethodPublic, "Bar.Goo", Nothing))

            Dim tooltipTextForBar = String.Format(ServicesVSResources._0_has_multiple_implemented_interfaces, "Bar")
            Dim targetForBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, ServicesVSResources.Implemented_interfaces)).
                Add(New TargetMenuItemViewModel("IBar1", KnownMonikers.InterfacePublic, "IBar1", Nothing)).
                Add(New TargetMenuItemViewModel("IBar2", KnownMonikers.InterfacePublic, "IBar2", Nothing))

            Dim tooltipTextForGooOfIBar = String.Format(ServicesVSResources._0_implements_members_from_multiple_interfaces, "Bar.Goo")
            Dim targetForGooOfIBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_members, KnownMonikers.Implementing, ServicesVSResources.Implemented_members)).
                Add(New TargetMenuItemViewModel("IBar1.Goo", KnownMonikers.MethodPublic, "IBar1.Goo", Nothing)).
                Add(New TargetMenuItemViewModel("IBar2.Goo", KnownMonikers.MethodPublic, "IBar2.Goo", Nothing))

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {2, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForIBar1),
                    tooltipTextForIBar1,
                    1,
                    targetForIBar1)},
                {4, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForGooOfIBar1),
                    tooltipTextForGooOfIBar1,
                    1,
                    targetForGooOfIBar1)},
                {6, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForIBar2),
                    tooltipTextForIBar2,
                    1,
                    targetForIBar2)},
                {8, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForGooOfIBar2),
                    tooltipTextForGooOfIBar2,
                    1,
                    targetForGooOfIBar2)},
                {10, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForBar),
                    tooltipTextForBar,
                    1,
                    targetForBar)},
                {12, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForGooOfIBar),
                    tooltipTextForGooOfIBar,
                    1,
                    targetForGooOfIBar)}})
        End Function

        <WpfFact>
        Public Function TestImplementedInterfaceAndDerivedType() As Task
            Dim markup = "
using System.Collections;
public class Bar: IEnumerable { }
public class SubBar : Bar { }"
            Dim tooltipTextForBar = String.Format(ServicesVSResources._0_has_implemented_interfaces_and_derived_types, "Bar")
            Dim targetForBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, ServicesVSResources.Implemented_interfaces)).
                Add(New TargetMenuItemViewModel("IEnumerable", KnownMonikers.InterfacePublic, "IEnumerable", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Derived_types, KnownMonikers.Overridden, ServicesVSResources.Derived_types)).
                Add(New TargetMenuItemViewModel("SubBar", KnownMonikers.ClassPublic, "SubBar", Nothing))

            Dim tooltipTextForSubBar = String.Format(ServicesVSResources._0_has_implemented_interfaces_and_base_types, "SubBar")
            Dim targetForBaseBar1 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, ServicesVSResources.Implemented_interfaces)).
                Add(New TargetMenuItemViewModel("IEnumerable", KnownMonikers.InterfacePublic, "IEnumerable", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Base_Types, KnownMonikers.Overriding, ServicesVSResources.Base_Types)).
                Add(New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing))

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {3, New InheritanceMarginViewModel(
                    KnownMonikers.ImplementingOverridden,
                    CreateTextBlock(tooltipTextForBar),
                    tooltipTextForBar,
                    1,
                    targetForBar)},
                {4, New InheritanceMarginViewModel(
                    KnownMonikers.ImplementingOverriding,
                    CreateTextBlock(tooltipTextForSubBar),
                    tooltipTextForSubBar,
                    1,
                    targetForBaseBar1)}})
        End Function

        <WpfFact>
        Public Function TestImplementedInterfaceBaseTypeAndDerivedType() As Task
            Dim markup = "
using System.Collections;
public class BaseBar : IEnumerable
{
    public virtual IEnumerator GetEnumerator() => throw new NotImplementedException();
}
public class Bar: BaseBar, IEnumerable
{
    public override IEnumerator GetEnumerator() => throw new NotImplementedException();
}
public class SubBar : Bar
{
    public override IEnumerator GetEnumerator() => throw new NotImplementedException();
}"

            Dim tooltipTextForBaseBar = String.Format(ServicesVSResources._0_has_implemented_interfaces_and_derived_types, "BaseBar")
            Dim targetForBaseBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, ServicesVSResources.Implemented_interfaces)).
                Add(New TargetMenuItemViewModel("IEnumerable", KnownMonikers.InterfacePublic, "IEnumerable", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Derived_types, KnownMonikers.Overridden, ServicesVSResources.Derived_types)).
                Add(New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing)).
                Add(New TargetMenuItemViewModel("SubBar", KnownMonikers.ClassPublic, "SubBar", Nothing))

            Dim tooltipTextForGetEnumeratorOfBaseBar = String.Format(ServicesVSResources._0_has_implemented_members_and_overriding_members, "BaseBar.GetEnumerator")
            Dim targetForGetEnumeratorOfBaseBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_members, KnownMonikers.Implementing, ServicesVSResources.Implemented_members)).
                Add(New TargetMenuItemViewModel("IEnumerable.GetEnumerator", KnownMonikers.MethodPublic, "IEnumerable.GetEnumerator", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Overriding_members, KnownMonikers.Overridden, ServicesVSResources.Overriding_members)).
                Add(New TargetMenuItemViewModel("Bar.GetEnumerator", KnownMonikers.MethodPublic, "Bar.GetEnumerator", Nothing)).
                Add(New TargetMenuItemViewModel("SubBar.GetEnumerator", KnownMonikers.MethodPublic, "SubBar.GetEnumerator", Nothing))

            Dim tooltipTextForBar = String.Format(ServicesVSResources._0_has_implemented_interfaces_base_types_and_derived_types, "Bar")
            Dim targetForBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, ServicesVSResources.Implemented_interfaces)).
                Add(New TargetMenuItemViewModel("IEnumerable", KnownMonikers.InterfacePublic, "IEnumerable", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Base_Types, KnownMonikers.Overriding, ServicesVSResources.Base_Types)).
                Add(New TargetMenuItemViewModel("BaseBar", KnownMonikers.ClassPublic, "BaseBar", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Derived_types, KnownMonikers.Overridden, ServicesVSResources.Derived_types)).
                Add(New TargetMenuItemViewModel("SubBar", KnownMonikers.ClassPublic, "SubBar", Nothing))

            Dim tooltipTextForGetEnumeratorOfBar = String.Format(ServicesVSResources._0_has_implemented_members_overridden_members_and_overriding_members, "Bar.GetEnumerator")
            Dim targetForGetEnumeratorOfBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_members, KnownMonikers.Implementing, ServicesVSResources.Implemented_members)).
                Add(New TargetMenuItemViewModel("IEnumerable.GetEnumerator", KnownMonikers.MethodPublic, "IEnumerable.GetEnumerator", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Overridden_members, KnownMonikers.Overriding, ServicesVSResources.Overridden_members)).
                Add(New TargetMenuItemViewModel("BaseBar.GetEnumerator", KnownMonikers.MethodPublic, "BaseBar.GetEnumerator", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Overriding_members, KnownMonikers.Overridden, ServicesVSResources.Overriding_members)).
                Add(New TargetMenuItemViewModel("SubBar.GetEnumerator", KnownMonikers.MethodPublic, "SubBar.GetEnumerator", Nothing))

            Dim tooltipTextForSubBar = String.Format(ServicesVSResources._0_has_implemented_interfaces_and_base_types, "SubBar")
            Dim targetForBaseBar1 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, ServicesVSResources.Implemented_interfaces)).
                Add(New TargetMenuItemViewModel("IEnumerable", KnownMonikers.InterfacePublic, "IEnumerable", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Base_Types, KnownMonikers.Overriding, ServicesVSResources.Base_Types)).
                Add(New TargetMenuItemViewModel("Bar", KnownMonikers.ClassPublic, "Bar", Nothing)).
                Add(New TargetMenuItemViewModel("BaseBar", KnownMonikers.ClassPublic, "BaseBar", Nothing))

            Dim tooltipTextForGetEnumeratorOfSubBar = String.Format(ServicesVSResources._0_has_implemented_members_and_overridden_members, "SubBar.GetEnumerator")
            Dim targetForGetEnumeratorOfSubBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_members, KnownMonikers.Implementing, ServicesVSResources.Implemented_members)).
                Add(New TargetMenuItemViewModel("IEnumerable.GetEnumerator", KnownMonikers.MethodPublic, "IEnumerable.GetEnumerator", Nothing)).
                Add(New HeaderMenuItemViewModel(ServicesVSResources.Overridden_members, KnownMonikers.Overriding, ServicesVSResources.Overridden_members)).
                Add(New TargetMenuItemViewModel("Bar.GetEnumerator", KnownMonikers.MethodPublic, "Bar.GetEnumerator", Nothing)).
                Add(New TargetMenuItemViewModel("BaseBar.GetEnumerator", KnownMonikers.MethodPublic, "BaseBar.GetEnumerator", Nothing))

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {3, New InheritanceMarginViewModel(
                    KnownMonikers.ImplementingOverridden,
                    CreateTextBlock(tooltipTextForBaseBar),
                    tooltipTextForBaseBar,
                    1,
                    targetForBaseBar)},
                {5, New InheritanceMarginViewModel(
                    KnownMonikers.ImplementingOverridden,
                    CreateTextBlock(tooltipTextForGetEnumeratorOfBaseBar),
                    tooltipTextForGetEnumeratorOfBaseBar,
                    1,
                    targetForGetEnumeratorOfBaseBar)},
                {7, New InheritanceMarginViewModel(
                    KnownMonikers.ImplementingOverridden,
                    CreateTextBlock(tooltipTextForBar),
                    tooltipTextForBar,
                    1,
                    targetForBar)},
                {9, New InheritanceMarginViewModel(
                    KnownMonikers.ImplementingOverridden,
                    CreateTextBlock(tooltipTextForGetEnumeratorOfBar),
                    tooltipTextForGetEnumeratorOfBar,
                    1,
                    targetForGetEnumeratorOfBar)},
                {11, New InheritanceMarginViewModel(
                    KnownMonikers.ImplementingOverriding,
                    CreateTextBlock(tooltipTextForSubBar),
                    tooltipTextForSubBar,
                    1,
                    targetForBaseBar1)},
                {13, New InheritanceMarginViewModel(
                    KnownMonikers.ImplementingOverriding,
                    CreateTextBlock(tooltipTextForGetEnumeratorOfSubBar),
                    tooltipTextForGetEnumeratorOfSubBar,
                    1,
                    targetForGetEnumeratorOfSubBar)}})
        End Function

        <WpfFact>
        Public Function TestImplementedByMembersFromMultipleType() As Task
            Dim markup = "
public interface IBar
{
    void Sub();
}
public class Bar1: IBar
{
    public void Sub() { }
}
public class Bar2 : IBar
{
    public void Sub() { }
}"
            Dim tooltipTextForIBar = String.Format(ServicesVSResources._0_has_multiple_implementing_types, "IBar")
            Dim targetForIBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implementing_types, KnownMonikers.Implemented, ServicesVSResources.Implementing_types)).
                Add(New TargetMenuItemViewModel("Bar1", KnownMonikers.ClassPublic, "Bar1", Nothing)).
                Add(New TargetMenuItemViewModel("Bar2", KnownMonikers.ClassPublic, "Bar2", Nothing))

            Dim tooltipTextForSubInIBar = String.Format(ServicesVSResources._0_is_implemented_by_members_from_multiple_types, "IBar.Sub")
            Dim targetForBar = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implementing_members, KnownMonikers.Implemented, ServicesVSResources.Implementing_members)).
                Add(New TargetMenuItemViewModel("Bar1.Sub", KnownMonikers.MethodPublic, "Bar1.Sub", Nothing)).
                Add(New TargetMenuItemViewModel("Bar2.Sub", KnownMonikers.MethodPublic, "Bar2.Sub", Nothing))

            Dim tooltipTextForBar1 = String.Format(ServicesVSResources._0_implements_1, "Bar1", "IBar")
            Dim targetForBaseBar1 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, ServicesVSResources.Implemented_interfaces)).
                Add(New TargetMenuItemViewModel("IBar", KnownMonikers.InterfacePublic, "IBar", Nothing))

            Dim tooltipTextForSubInBar1 = String.Format(ServicesVSResources._0_implements_1, "Bar1.Sub", "IBar.Sub")
            Dim targetForSubInBar1 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_members, KnownMonikers.Implementing, ServicesVSResources.Implemented_members)).
                Add(New TargetMenuItemViewModel("IBar.Sub", KnownMonikers.MethodPublic, "IBar.Sub", Nothing))

            Dim tooltipTextForBar2 = String.Format(ServicesVSResources._0_implements_1, "Bar2", "IBar")
            Dim targetForBaseBar2 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, ServicesVSResources.Implemented_interfaces)).
                Add(New TargetMenuItemViewModel("IBar", KnownMonikers.InterfacePublic, "IBar", Nothing))

            Dim tooltipTextForSubInBar2 = String.Format(ServicesVSResources._0_implements_1, "Bar2.Sub", "IBar.Sub")
            Dim targetForSubInBar2 = ImmutableArray.Create(Of InheritanceMenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Implemented_members, KnownMonikers.Implementing, ServicesVSResources.Implemented_members)).
                Add(New TargetMenuItemViewModel("IBar.Sub", KnownMonikers.MethodPublic, "IBar.Sub", Nothing))

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, InheritanceMarginViewModel) From {
                {2, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForIBar),
                    tooltipTextForIBar,
                    1,
                    targetForIBar)},
                {4, New InheritanceMarginViewModel(
                    KnownMonikers.Implemented,
                    CreateTextBlock(tooltipTextForSubInIBar),
                    tooltipTextForSubInIBar,
                    1,
                    targetForBar)},
                {6, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForBar1),
                    tooltipTextForBar1,
                    1,
                    targetForBaseBar1)},
                {8, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForSubInBar1),
                    tooltipTextForSubInBar1,
                    1,
                    targetForSubInBar1)},
                {10, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForBar2),
                    tooltipTextForBar2,
                    1,
                    targetForBaseBar2)},
                {12, New InheritanceMarginViewModel(
                    KnownMonikers.Implementing,
                    CreateTextBlock(tooltipTextForSubInBar2),
                    tooltipTextForSubInBar2,
                    1,
                    targetForSubInBar1)}})
        End Function
    End Class
End Namespace
