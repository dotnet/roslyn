﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Windows
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.CSharp.ChangeSignature
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ChangeSignature
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ChangeSignature
    <[UseExportProvider]>
    Public Class AddParameterViewModelTests

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub AddParameter_SubmittingRequiresTypeAndNameAndCallsiteValue()
            Dim markup = <Text><![CDATA[
class MyClass
{
    public void M($$) { }
}"]]></Text>

            Dim viewModelTestState = GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel

            VerifyOpeningState(viewModel)

            viewModel.VerbatimTypeName = "int"
            Assert.False(viewModel.TrySubmit())

            viewModel.VerbatimTypeName = ""
            viewModel.ParameterName = "x"
            Assert.False(viewModel.TrySubmit())

            viewModel.VerbatimTypeName = "int"
            Assert.False(viewModel.TrySubmit())

            viewModel.CallSiteValue = "7"
            Assert.True(viewModel.TrySubmit())
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub AddParameter_TypeNameTextBoxInteractions()
            Dim markup = <Text><![CDATA[
class MyClass<T>
{
    public void M($$) { }
}"]]></Text>

            Dim viewModelTestState = GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel

            VerifyOpeningState(viewModel)

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.TypeBindsDynamicStatus)
            monitor.AddExpectation(Function() viewModel.TypeIsEmptyImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotParseImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotBindImage)
            monitor.AddExpectation(Function() viewModel.TypeBindsImage)

            viewModel.VerbatimTypeName = "M"

            monitor.VerifyExpectations()
            monitor.Detach()

            AssertTypeBindingIconAndTextIs(viewModel, NameOf(viewModel.TypeDoesNotBindImage), ServicesVSResources.Type_name_is_not_recognized)

            monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.TypeBindsDynamicStatus)
            monitor.AddExpectation(Function() viewModel.TypeIsEmptyImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotParseImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotBindImage)
            monitor.AddExpectation(Function() viewModel.TypeBindsImage)

            viewModel.VerbatimTypeName = "MyClass"

            monitor.VerifyExpectations()
            monitor.Detach()

            AssertTypeBindingIconAndTextIs(viewModel, NameOf(viewModel.TypeDoesNotBindImage), ServicesVSResources.Type_name_is_not_recognized)

            monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.TypeBindsDynamicStatus)
            monitor.AddExpectation(Function() viewModel.TypeIsEmptyImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotParseImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotBindImage)
            monitor.AddExpectation(Function() viewModel.TypeBindsImage)

            viewModel.VerbatimTypeName = "MyClass<i"

            monitor.VerifyExpectations()
            monitor.Detach()

            AssertTypeBindingIconAndTextIs(viewModel, NameOf(viewModel.TypeDoesNotParseImage), ServicesVSResources.Type_name_has_a_syntax_error)

            monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.TypeBindsDynamicStatus)
            monitor.AddExpectation(Function() viewModel.TypeIsEmptyImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotParseImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotBindImage)
            monitor.AddExpectation(Function() viewModel.TypeBindsImage)

            viewModel.VerbatimTypeName = "MyClass<int>"

            monitor.VerifyExpectations()
            monitor.Detach()

            AssertTypeBindingIconAndTextIs(viewModel, NameOf(viewModel.TypeBindsImage), ServicesVSResources.Type_name_is_recognized)

            monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.TypeBindsDynamicStatus)
            monitor.AddExpectation(Function() viewModel.TypeIsEmptyImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotParseImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotBindImage)
            monitor.AddExpectation(Function() viewModel.TypeBindsImage)

            viewModel.VerbatimTypeName = ""

            monitor.VerifyExpectations()
            monitor.Detach()

            AssertTypeBindingIconAndTextIs(viewModel, NameOf(viewModel.TypeIsEmptyImage), ServicesVSResources.Please_enter_a_type_name)
        End Sub

        <Theory, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        <InlineData("int")>
        <InlineData("MyClass")>
        <InlineData("NS1.NS2.DifferentClass")>
        Public Sub AddParameter_NoExistingParameters_TypeBinds(typeName As String)
            Dim markup = <Text><![CDATA[
namespace NS1
{
    namespace NS2
    {
        class DifferentClass { }
    }
}

class MyClass
{
    public void M($$)
    {
        M();
    }
}"]]></Text>

            Dim viewModelTestState = GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel

            VerifyOpeningState(viewModel)

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.TypeBindsDynamicStatus)
            monitor.AddExpectation(Function() viewModel.TypeIsEmptyImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotParseImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotBindImage)
            monitor.AddExpectation(Function() viewModel.TypeBindsImage)

            viewModel.VerbatimTypeName = typeName

            monitor.VerifyExpectations()
            monitor.Detach()

            AssertTypeBindingIconAndTextIs(viewModel, NameOf(viewModel.TypeBindsImage), ServicesVSResources.Type_name_is_recognized)

            viewModel.ParameterName = "x"
            viewModel.CallSiteValue = "0"

            Assert.True(viewModel.TrySubmit())
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub AddParameter_CannotBeBothRequiredAndOmit()
            Dim markup = <Text><![CDATA[
class MyClass<T>
{
    public void M($$) { }
}"]]></Text>

            Dim viewModelTestState = GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel

            VerifyOpeningState(viewModel)

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.IsOptional)
            monitor.AddExpectation(Function() viewModel.IsRequired)
            monitor.AddExpectation(Function() viewModel.IsCallsiteRegularValue)
            monitor.AddExpectation(Function() viewModel.IsCallsiteOmitted)

            viewModel.IsOptional = True
            viewModel.IsCallsiteOmitted = True
            viewModel.IsRequired = True

            monitor.VerifyExpectations()
            monitor.Detach()

            Assert.True(viewModel.IsCallsiteRegularValue)
            Assert.False(viewModel.IsCallsiteOmitted)
        End Sub

        <Theory, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        <InlineData("int")>
        <InlineData("MyClass")>
        <InlineData("NS1.NS2.DifferentClass")>
        Public Sub AddParameter_ExistingParameters_TypeBinds(typeName As String)
            Dim markup = <Text><![CDATA[
namespace NS1
{
    namespace NS2
    {
        class DifferentClass { }
    }
}

class MyClass
{
    public void M(int x$$)
    {
        M(3);
    }
}"]]></Text>

            Dim viewModelTestState = GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel

            VerifyOpeningState(viewModel)

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.TypeBindsDynamicStatus)
            monitor.AddExpectation(Function() viewModel.TypeIsEmptyImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotParseImage)
            monitor.AddExpectation(Function() viewModel.TypeDoesNotBindImage)
            monitor.AddExpectation(Function() viewModel.TypeBindsImage)

            viewModel.VerbatimTypeName = typeName

            monitor.VerifyExpectations()
            monitor.Detach()

            AssertTypeBindingIconAndTextIs(viewModel, NameOf(viewModel.TypeBindsImage), ServicesVSResources.Type_name_is_recognized)

            viewModel.ParameterName = "x"
            viewModel.CallSiteValue = "0"

            Assert.True(viewModel.TrySubmit())
        End Sub

        Private Sub AssertTypeBindingIconAndTextIs(viewModel As AddParameterDialogViewModel, currentIcon As String, expectedMessage As String)
            Assert.True(viewModel.TypeIsEmptyImage = If(NameOf(viewModel.TypeIsEmptyImage) = currentIcon, Visibility.Visible, Visibility.Collapsed))
            Assert.True(viewModel.TypeDoesNotParseImage = If(NameOf(viewModel.TypeDoesNotParseImage) = currentIcon, Visibility.Visible, Visibility.Collapsed))
            Assert.True(viewModel.TypeDoesNotBindImage = If(NameOf(viewModel.TypeDoesNotBindImage) = currentIcon, Visibility.Visible, Visibility.Collapsed))
            Assert.True(viewModel.TypeBindsImage = If(NameOf(viewModel.TypeBindsImage) = currentIcon, Visibility.Visible, Visibility.Collapsed))

            Assert.Equal(expectedMessage, viewModel.TypeBindsDynamicStatus)
        End Sub

        Private Sub VerifyOpeningState(viewModel As AddParameterDialogViewModel)
            Assert.True(viewModel.TypeBindsDynamicStatus = ServicesVSResources.Please_enter_a_type_name)

            Assert.True(viewModel.TypeIsEmptyImage = Visibility.Visible)
            Assert.True(viewModel.TypeDoesNotParseImage = Visibility.Collapsed)
            Assert.True(viewModel.TypeDoesNotBindImage = Visibility.Collapsed)
            Assert.True(viewModel.TypeBindsImage = Visibility.Collapsed)

            Assert.True(viewModel.IsRequired)
            Assert.False(viewModel.IsOptional)
            Assert.Equal(String.Empty, viewModel.DefaultValue)

            Assert.True(viewModel.IsCallsiteRegularValue)
            Assert.Equal(String.Empty, viewModel.CallSiteValue)
            Assert.False(viewModel.UseNamedArguments)
            Assert.False(viewModel.IsCallsiteTodo)
            Assert.False(viewModel.IsCallsiteOmitted)

            Assert.False(viewModel.TrySubmit)
        End Sub

        Private Function GetViewModelTestStateAsync(
            markup As XElement,
            languageName As String) As AddParameterViewModelTestState

            Dim workspaceXml =
            <Workspace>
                <Project Language=<%= languageName %> CommonReferences="true">
                    <Document><%= markup.NormalizedValue.Replace(vbCrLf, vbLf) %></Document>
                </Project>
            </Workspace>

            Dim exportProvider = ExportProviderCache _
                .GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic() _
                    .WithParts(GetType(CSharpChangeSignatureViewModelFactoryService), GetType(VisualBasicChangeSignatureViewModelFactoryService))) _
                .CreateExportProvider()

            Using workspace = TestWorkspace.Create(workspaceXml, exportProvider:=exportProvider)
                Dim doc = workspace.Documents.Single()
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If Not doc.CursorPosition.HasValue Then
                    Assert.True(False, "Missing caret location in document.")
                End If

                Dim viewModel = New AddParameterDialogViewModel(workspaceDoc, doc.CursorPosition.Value)
                Return New AddParameterViewModelTestState(viewModel)
            End Using
        End Function
    End Class
End Namespace
