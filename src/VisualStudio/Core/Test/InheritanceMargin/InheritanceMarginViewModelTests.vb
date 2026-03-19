' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Windows
Imports System.Windows.Documents
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.InheritanceMargin
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Imaging.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
Imports Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
Imports Microsoft.VisualStudio.Text.Classification
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.InheritanceMargin

    <Trait(Traits.Feature, Traits.Features.InheritanceMargin)>
    <UseExportProvider>
    Public Class InheritanceMarginViewModelTests

        Private Shared ReadOnly s_defaultMargin As Thickness = New Thickness(4, 1, 4, 1)

        Private Shared ReadOnly s_indentMargin As Thickness = New Thickness(22, 1, 4, 1)

        Private Structure GlyphViewModelData
            Public ReadOnly Property ImageMoniker As ImageMoniker
            Public ReadOnly Property ToolTipText As String
            Public ReadOnly Property AutomationName As String
            Public ReadOnly Property ScaleFactor As Double
            Public ReadOnly Property MenuItems As MenuItemViewModelData()

            Public Sub New(imageMoniker As ImageMoniker, toolTipText As String, automationName As String, scaleFactor As Double, ParamArray menuItems() As MenuItemViewModelData)
                Me.ImageMoniker = imageMoniker
                Me.ToolTipText = toolTipText
                Me.AutomationName = automationName
                Me.ScaleFactor = scaleFactor
                Me.MenuItems = menuItems
            End Sub
        End Structure

        Private Structure MenuItemViewModelData
            Public ReadOnly Property AutomationName As String
            Public ReadOnly Property DisplayContent As String
            Public ReadOnly Property ImageMoniker As ImageMoniker
            Public ReadOnly Property ViewModelType As Type
            Public ReadOnly Property MenuItems As MenuItemViewModelData()

            Public Sub New(automationName As String, displayContent As String, imageMoniker As ImageMoniker, viewModelType As Type, ParamArray menuItems() As MenuItemViewModelData)
                Me.AutomationName = automationName
                Me.DisplayContent = displayContent
                Me.ImageMoniker = imageMoniker
                Me.ViewModelType = viewModelType
                Me.MenuItems = menuItems
            End Sub
        End Structure

        Private Shared Async Function VerifyAsync(markup As String, languageName As String, expectedViewModels As Dictionary(Of Integer, GlyphViewModelData)) As Task
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
            Using workspace = EditorTestWorkspace.Create(workspaceFile)
                Dim testDocument = workspace.Documents.Single()
                Dim document = workspace.CurrentSolution.GetDocument(testDocument.Id)
                Dim service = document.GetRequiredLanguageService(Of IInheritanceMarginService)

                Dim classificationTypeMap = workspace.ExportProvider.GetExportedValue(Of ClassificationTypeMap)
                Dim classificationFormatMap = workspace.ExportProvider.GetExportedValue(Of IClassificationFormatMapService)

                ' For these tests, we need to be on UI thread, so don't call ConfigureAwait(False)
                Dim root = Await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(True)
                Dim inheritanceItems = Await service.GetInheritanceMemberItemsAsync(
                    document, root.FullSpan, includeGlobalImports:=True, frozenPartialSemantics:=True, cancellationToken).ConfigureAwait(True)

                Dim acutalLineToTagDictionary = inheritanceItems.GroupBy(Function(item) item.LineNumber) _
                    .ToDictionary(Function(grouping) grouping.Key,
                                  Function(grouping)
                                      Dim lineNumber = grouping.Key
                                      Dim items = grouping.Select(Function(g) g).ToImmutableArray()
                                      Return New InheritanceMarginTag(lineNumber, items)
                                  End Function)
                Assert.Equal(expectedViewModels.Count, acutalLineToTagDictionary.Count)

                For Each kvp In expectedViewModels
                    Dim lineNumber = kvp.Key
                    Dim expectedViewModel = kvp.Value
                    Assert.True(acutalLineToTagDictionary.ContainsKey(lineNumber))

                    Dim acutalTag = acutalLineToTagDictionary(lineNumber)
                    ' Editor TestView zoom level is 100 based.
                    Dim actualViewModel = InheritanceMarginGlyphViewModel.Create(
                        classificationTypeMap, classificationFormatMap.GetClassificationFormatMap("tooltip"), acutalTag, 100)

                    VerifyTwoViewModelAreSame(expectedViewModel, actualViewModel)
                Next

            End Using
        End Function

        Private Shared Sub VerifyTwoViewModelAreSame(expected As GlyphViewModelData, actual As InheritanceMarginGlyphViewModel)
            Assert.Equal(expected.ImageMoniker, actual.ImageMoniker)
            Dim actualTextGetFromTextBlock = actual.ToolTipTextBlock.Inlines _
                .OfType(Of Run).Select(Function(run) run.Text) _
                .Aggregate(Function(text1, text2) text1 + text2)
            ' When the text block is created, a unicode 'left to right' would be inserted between the space.
            ' Make sure it is removed.
            Dim leftToRightMarker = Char.ConvertFromUtf32(&H200E)
            Dim actualText = actualTextGetFromTextBlock.Replace(leftToRightMarker, String.Empty)
            Assert.Equal(expected.ToolTipText, actualText)
            Assert.Equal(expected.AutomationName, actual.AutomationName)
            Assert.Equal(expected.MenuItems.Length, actual.MenuItemViewModels.Length)
            Assert.Equal(expected.ScaleFactor, actual.ScaleFactor)

            For i = 0 To expected.MenuItems.Length - 1
                Dim expectedMenuItem = expected.MenuItems(i)
                Dim actualMenuItem = actual.MenuItemViewModels(i)
                VerifyMenuItem(expectedMenuItem, actualMenuItem)
            Next

        End Sub

        Private Shared Sub VerifyMenuItem(expected As MenuItemViewModelData, actual As MenuItemViewModel)
            Assert.Equal(expected.AutomationName, actual.AutomationName)
            Assert.Equal(expected.DisplayContent, actual.DisplayContent)
            Assert.Equal(expected.ImageMoniker, actual.ImageMoniker)

            Assert.IsType(expected.ViewModelType, actual)

            If expected.ViewModelType = GetType(MemberMenuItemViewModel) Then
                Dim acutalMemberMenuItem = CType(actual, MemberMenuItemViewModel)
                Assert.Equal(expected.MenuItems.Length, acutalMemberMenuItem.Targets.Length)
                For i = 0 To expected.MenuItems.Length - 1
                    VerifyMenuItem(expected.MenuItems(i), acutalMemberMenuItem.Targets(i))
                Next
            End If
        End Sub

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
            Dim tooltipTextForBar = String.Format(ServicesVSResources._0_is_inherited, "class Bar")
            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, GlyphViewModelData) From {
                {2, New GlyphViewModelData(
                    KnownMonikers.Implemented,
                    tooltipTextForIBar,
                    tooltipTextForIBar,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Implementing_types, ServicesVSResources.Implementing_types, KnownMonikers.Implemented, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("Bar", "Bar", KnownMonikers.ClassPublic, GetType(TargetMenuItemViewModel)))},
                {5, New GlyphViewModelData(
                    KnownMonikers.Implementing,
                    tooltipTextForBar,
                    tooltipTextForBar,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Implemented_interfaces, ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("IBar", "IBar", KnownMonikers.InterfacePublic, GetType(TargetMenuItemViewModel)))}})
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
            Dim tooltipTextForAbstractFoo = String.Format(ServicesVSResources._0_is_inherited, "abstract void AbsBar.Foo()")
            Dim tooltipTextForBar = String.Format(ServicesVSResources._0_is_inherited, "class Bar")
            Dim tooltipTextForOverrideFoo = String.Format(ServicesVSResources._0_is_inherited, "override void Bar.Foo()")
            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, GlyphViewModelData) From {
                {2, New GlyphViewModelData(
                    KnownMonikers.Overridden,
                    tooltipTextForAbsBar,
                    tooltipTextForAbsBar,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Derived_types, ServicesVSResources.Derived_types, KnownMonikers.Overridden, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("Bar", "Bar", KnownMonikers.ClassPublic, GetType(TargetMenuItemViewModel)))},
                {4, New GlyphViewModelData(
                    KnownMonikers.Overridden,
                    tooltipTextForAbstractFoo,
                    tooltipTextForAbstractFoo,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Overriding_members, ServicesVSResources.Overriding_members, KnownMonikers.Overridden, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("Bar.Foo", "Bar.Foo", KnownMonikers.MethodPublic, GetType(TargetMenuItemViewModel)))},
                {7, New GlyphViewModelData(
                    KnownMonikers.Overriding,
                    tooltipTextForBar,
                    tooltipTextForBar,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Base_Types, ServicesVSResources.Base_Types, KnownMonikers.Overriding, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("AbsBar", "AbsBar", KnownMonikers.ClassPublic, GetType(TargetMenuItemViewModel)))},
                {9, New GlyphViewModelData(
                    KnownMonikers.Overriding,
                    tooltipTextForOverrideFoo,
                    tooltipTextForOverrideFoo,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Overridden_members, ServicesVSResources.Overridden_members, KnownMonikers.Overriding, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("AbsBar.Foo", "AbsBar.Foo", KnownMonikers.MethodPublic, GetType(TargetMenuItemViewModel)))}})
        End Function

        <WpfFact>
        Public Function TestInterfaceImplementsInterfaceRelationship() As Task
            Dim markup = "
public interface IBar1 { }
public interface IBar2 : IBar1 { }
public interface IBar3 : IBar2 { }
"
            Dim tooltipTextForIBar1 = String.Format(ServicesVSResources._0_is_inherited, "interface IBar1")
            Dim tooltipTextForIBar2 = String.Format(ServicesVSResources._0_is_inherited, "interface IBar2")
            Dim tooltipTextForIBar3 = String.Format(ServicesVSResources._0_is_inherited, "interface IBar3")
            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, GlyphViewModelData) From {
                {2, New GlyphViewModelData(
                    KnownMonikers.Implemented,
                    tooltipTextForIBar1,
                    tooltipTextForIBar1,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Implementing_types, ServicesVSResources.Implementing_types, KnownMonikers.Implemented, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("IBar2", "IBar2", KnownMonikers.InterfacePublic, GetType(TargetMenuItemViewModel)),
                    New MenuItemViewModelData("IBar3", "IBar3", KnownMonikers.InterfacePublic, GetType(TargetMenuItemViewModel)))},
                {3, New GlyphViewModelData(
                    KnownMonikers.ImplementingImplemented,
                    tooltipTextForIBar2,
                    tooltipTextForIBar2,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Inherited_interfaces, ServicesVSResources.Inherited_interfaces, KnownMonikers.Implementing, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("IBar1", "IBar1", KnownMonikers.InterfacePublic, GetType(TargetMenuItemViewModel)),
                    New MenuItemViewModelData(ServicesVSResources.Implementing_types, ServicesVSResources.Implementing_types, KnownMonikers.Implemented, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("IBar3", "IBar3", KnownMonikers.InterfacePublic, GetType(TargetMenuItemViewModel)))},
                {4, New GlyphViewModelData(
                    KnownMonikers.Implementing,
                    tooltipTextForIBar3,
                    tooltipTextForIBar3,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Inherited_interfaces, ServicesVSResources.Inherited_interfaces, KnownMonikers.Implementing, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("IBar1", "IBar1", KnownMonikers.InterfacePublic, GetType(TargetMenuItemViewModel)),
                    New MenuItemViewModelData("IBar2", "IBar2", KnownMonikers.InterfacePublic, GetType(TargetMenuItemViewModel)))}})
        End Function

        <WpfFact>
        Public Function TestClassDerivesClass() As Task
            Dim markup = "
public class Bar1 {}
public class Bar2 : Bar1 {}
public class Bar3 : Bar2 {}"

            Dim tooltipTextForBar1 = String.Format(ServicesVSResources._0_is_inherited, "class Bar1")
            Dim targetForBar1 = ImmutableArray.Create(Of MenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Derived_types, KnownMonikers.Overridden)).
                Add(New TargetMenuItemViewModel("Bar2", KnownMonikers.ClassPublic, Nothing)).Add(New TargetMenuItemViewModel("Bar3", KnownMonikers.ClassPublic, Nothing))

            Dim tooltipTextForBar2 = String.Format(ServicesVSResources._0_is_inherited, "class Bar2")

            Dim targetForBar2 = ImmutableArray.Create(Of MenuItemViewModel)(
                New HeaderMenuItemViewModel(ServicesVSResources.Base_Types, KnownMonikers.Overriding)).
                    Add(New TargetMenuItemViewModel("Bar1", KnownMonikers.ClassPublic, Nothing)).
                        Add(New HeaderMenuItemViewModel(ServicesVSResources.Derived_types, KnownMonikers.Overridden)).
                    Add(New TargetMenuItemViewModel("Bar3", KnownMonikers.ClassPublic, Nothing))

            Dim tooltipTextForBar3 = String.Format(ServicesVSResources._0_is_inherited, "class Bar3")
            Dim targetForBar3 = ImmutableArray.Create(Of MenuItemViewModel)(New HeaderMenuItemViewModel(ServicesVSResources.Base_Types, KnownMonikers.Overriding)).
                Add(New TargetMenuItemViewModel("Bar1", KnownMonikers.ClassPublic, Nothing)).
                Add(New TargetMenuItemViewModel("Bar2", KnownMonikers.ClassPublic, Nothing))

            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, GlyphViewModelData) From {
                {2, New GlyphViewModelData(
                    KnownMonikers.Overridden,
                    tooltipTextForBar1,
                    tooltipTextForBar1,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Derived_types, ServicesVSResources.Derived_types, KnownMonikers.Overridden, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("Bar2", "Bar2", KnownMonikers.ClassPublic, GetType(TargetMenuItemViewModel)),
                    New MenuItemViewModelData("Bar3", "Bar3", KnownMonikers.ClassPublic, GetType(TargetMenuItemViewModel)))},
                {3, New GlyphViewModelData(
                    KnownMonikers.OverridingOverridden,
                    tooltipTextForBar2,
                    tooltipTextForBar2,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Base_Types, ServicesVSResources.Base_Types, KnownMonikers.Overriding, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("Bar1", "Bar1", KnownMonikers.ClassPublic, GetType(TargetMenuItemViewModel)),
                    New MenuItemViewModelData(ServicesVSResources.Derived_types, ServicesVSResources.Derived_types, KnownMonikers.Overridden, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("Bar3", "Bar3", KnownMonikers.ClassPublic, GetType(TargetMenuItemViewModel)))},
                {4, New GlyphViewModelData(
                    KnownMonikers.Overriding,
                    tooltipTextForBar3,
                    tooltipTextForBar3,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Base_Types, ServicesVSResources.Base_Types, KnownMonikers.Overriding, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("Bar1", "Bar1", KnownMonikers.ClassPublic, GetType(TargetMenuItemViewModel)),
                    New MenuItemViewModelData("Bar2", "Bar2", KnownMonikers.ClassPublic, GetType(TargetMenuItemViewModel)))}})

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
            Dim tooltipTextForE1AndE2InInterface = ServicesVSResources.Multiple_members_are_inherited
            Dim tooltipTextForBarSample = String.Format(ServicesVSResources._0_is_inherited, "class BarSample")
            Dim tooltipTextForE1AndE2InBarSample = ServicesVSResources.Multiple_members_are_inherited
            Return VerifyAsync(markup, LanguageNames.CSharp, New Dictionary(Of Integer, GlyphViewModelData) From {
                {3, New GlyphViewModelData(
                    KnownMonikers.Implemented,
                    tooltipTextForIBar1,
                    tooltipTextForIBar1,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Implementing_types, ServicesVSResources.Implementing_types, KnownMonikers.Implemented, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("BarSample", "BarSample", KnownMonikers.ClassPublic, GetType(TargetMenuItemViewModel)))},
                {5, New GlyphViewModelData(
                    KnownMonikers.Implemented,
                    tooltipTextForE1AndE2InInterface,
                    String.Format(ServicesVSResources.Multiple_members_are_inherited_on_line_0, 5),
                    1,
                    New MenuItemViewModelData("event EventHandler IBar1.e1", "event EventHandler IBar1.e1", KnownMonikers.EventPublic, GetType(MemberMenuItemViewModel),
                        New MenuItemViewModelData(ServicesVSResources.Implementing_members, ServicesVSResources.Implementing_members, KnownMonikers.Implemented, GetType(HeaderMenuItemViewModel)),
                        New MenuItemViewModelData("BarSample.e1", "BarSample.e1", KnownMonikers.EventPublic, GetType(TargetMenuItemViewModel))),
                    New MenuItemViewModelData("event EventHandler IBar1.e2", "event EventHandler IBar1.e2", KnownMonikers.EventPublic, GetType(MemberMenuItemViewModel),
                        New MenuItemViewModelData(ServicesVSResources.Implementing_members, ServicesVSResources.Implementing_members, KnownMonikers.Implemented, GetType(HeaderMenuItemViewModel)),
                        New MenuItemViewModelData("BarSample.e2", "BarSample.e2", KnownMonikers.EventPublic, GetType(TargetMenuItemViewModel))))},
                {8, New GlyphViewModelData(
                    KnownMonikers.Implementing,
                    tooltipTextForBarSample,
                    tooltipTextForBarSample,
                    1,
                    New MenuItemViewModelData(ServicesVSResources.Implemented_interfaces, ServicesVSResources.Implemented_interfaces, KnownMonikers.Implementing, GetType(HeaderMenuItemViewModel)),
                    New MenuItemViewModelData("IBar1", "IBar1", KnownMonikers.InterfaceInternal, GetType(TargetMenuItemViewModel)))},
                {10, New GlyphViewModelData(
                    KnownMonikers.Implementing,
                    tooltipTextForE1AndE2InBarSample,
                    String.Format(ServicesVSResources.Multiple_members_are_inherited_on_line_0, 10),
                    1,
                    New MenuItemViewModelData("virtual event EventHandler BarSample.e1", "virtual event EventHandler BarSample.e1", KnownMonikers.EventPublic, GetType(MemberMenuItemViewModel),
                        New MenuItemViewModelData(ServicesVSResources.Implemented_members, ServicesVSResources.Implemented_members, KnownMonikers.Implementing, GetType(HeaderMenuItemViewModel)),
                        New MenuItemViewModelData("IBar1.e1", "IBar1.e1", KnownMonikers.EventPublic, GetType(TargetMenuItemViewModel))),
                    New MenuItemViewModelData("virtual event EventHandler BarSample.e2", "virtual event EventHandler BarSample.e2", KnownMonikers.EventPublic, GetType(MemberMenuItemViewModel),
                        New MenuItemViewModelData(ServicesVSResources.Implemented_members, ServicesVSResources.Implemented_members, KnownMonikers.Implementing, GetType(HeaderMenuItemViewModel)),
                        New MenuItemViewModelData("IBar1.e2", "IBar1.e2", KnownMonikers.EventPublic, GetType(TargetMenuItemViewModel))))}})
        End Function
    End Class
End Namespace
