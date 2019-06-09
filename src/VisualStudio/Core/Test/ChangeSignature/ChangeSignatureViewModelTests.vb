' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
Imports Microsoft.VisualStudio.Text.Classification
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ChangeSignature
    <[UseExportProvider]>
    Public Class ReorderParametersViewModelTests

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function ReorderParameters_MethodWithTwoNormalParameters_UpDownArrowsNotOfferedWhenNoSelection() As Tasks.Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    public void $$M(int x, string y)
    {
    }
}"]]></Text>

            Dim viewModelTestState = Await GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel
            VerifyOpeningState(viewModel, "public void M(int x, string y)")

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.CanMoveDown)

            viewModel.SelectedIndex = -1

            VerifyAlteredState(
                viewModelTestState,
                monitor,
                isOkButtonEnabled:=False,
                canMoveUp:=False,
                canMoveDown:=False)

            monitor.Detach()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function ReorderParameters_MethodWithTwoNormalParameters_OkButtonNotOfferedAfterPermutationsResultingInOriginalOrdering() As Tasks.Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    public void $$M(int x, string y)
    {
    }
}"]]></Text>

            Dim viewModelTestState = Await GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel
            VerifyOpeningState(viewModel, "public void M(int x, string y)")

            viewModel.MoveDown()
            Assert.True(viewModel.TrySubmit)

            viewModel.MoveUp()
            Assert.False(viewModel.TrySubmit)

            VerifyAlteredState(
                viewModelTestState,
                isOkButtonEnabled:=False,
                canMoveUp:=False,
                canMoveDown:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function ReorderParameters_MethodWithTwoNormalParameters_MoveFirstParameterDown() As Tasks.Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    public void $$M(int x, string y)
    {
    }
}"]]></Text>

            Dim viewModelTestState = Await GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel
            VerifyOpeningState(viewModel, "public void M(int x, string y)")

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.IsOkButtonEnabled)
            monitor.AddExpectation(Function() viewModel.SignatureDisplay)
            monitor.AddExpectation(Function() viewModel.SignaturePreviewAutomationText)
            monitor.AddExpectation(Function() viewModel.AllParameters)
            monitor.AddExpectation(Function() viewModel.CanMoveUp)
            monitor.AddExpectation(Function() viewModel.MoveUpAutomationText)
            monitor.AddExpectation(Function() viewModel.CanMoveDown)
            monitor.AddExpectation(Function() viewModel.MoveDownAutomationText)

            viewModel.MoveDown()

            VerifyAlteredState(
                viewModelTestState,
                monitor,
                isOkButtonEnabled:=True,
                canMoveUp:=True,
                canMoveDown:=False,
                permutation:={1, 0},
                signatureDisplay:="public void M(string y, int x)")

            monitor.Detach()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function ReorderParameters_MethodWithTwoNormalParameters_RemoveFirstParameter() As Tasks.Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    public void $$M(int x, string y)
    {
    }
}"]]></Text>

            Dim viewModelTestState = Await GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel
            VerifyOpeningState(viewModel, "public void M(int x, string y)")

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.IsOkButtonEnabled)
            monitor.AddExpectation(Function() viewModel.SignatureDisplay)
            monitor.AddExpectation(Function() viewModel.SignaturePreviewAutomationText)
            monitor.AddExpectation(Function() viewModel.AllParameters)
            monitor.AddExpectation(Function() viewModel.CanRemove)
            monitor.AddExpectation(Function() viewModel.RemoveAutomationText)
            monitor.AddExpectation(Function() viewModel.CanRestore)
            monitor.AddExpectation(Function() viewModel.RestoreAutomationText)

            viewModel.Remove()

            VerifyAlteredState(
                viewModelTestState,
                monitor,
                isOkButtonEnabled:=True,
                canMoveUp:=False,
                canMoveDown:=True,
                permutation:={1},
                signatureDisplay:="public void M(string y)")

            monitor.Detach()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function ReorderParameters_MethodWithTwoNormalParameters_MoveSecondParameterUp() As Tasks.Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    public void $$M(int x, string y)
    {
    }
}"]]></Text>

            Dim viewModelTestState = Await GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel
            VerifyOpeningState(viewModel, "public void M(int x, string y)")

            viewModel.SelectedIndex = 1

            VerifyAlteredState(
                viewModelTestState,
                canMoveUp:=True,
                canMoveDown:=False)

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.IsOkButtonEnabled)
            monitor.AddExpectation(Function() viewModel.SignatureDisplay)
            monitor.AddExpectation(Function() viewModel.SignaturePreviewAutomationText)
            monitor.AddExpectation(Function() viewModel.AllParameters)
            monitor.AddExpectation(Function() viewModel.CanMoveUp)
            monitor.AddExpectation(Function() viewModel.MoveUpAutomationText)
            monitor.AddExpectation(Function() viewModel.CanMoveDown)
            monitor.AddExpectation(Function() viewModel.MoveDownAutomationText)

            viewModel.MoveUp()

            VerifyAlteredState(
                viewModelTestState,
                monitor,
                isOkButtonEnabled:=True,
                canMoveUp:=False,
                canMoveDown:=True,
                permutation:={1, 0},
                signatureDisplay:="public void M(string y, int x)")

            monitor.VerifyExpectations()
            monitor.Detach()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function ChangeSignature_ParameterDisplay_MultidimensionalArray() As Tasks.Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    public void $$M(int[,] x)
    {
    }
}"]]></Text>

            Dim viewModelTestState = Await GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel
            VerifyOpeningState(viewModel, "public void M(int[,] x)")
            VerifyParameterInfo(
                viewModel,
                parameterIndex:=0,
                type:="int[,]")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        <WorkItem(8437, "https://github.com/dotnet/roslyn/issues/8437")>
        Public Async Function ChangeSignature_ParameterDisplay_RefReturns() As Tasks.Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    public ref int $$M(int[,] x)
    {
    }
}"]]></Text>

            Dim viewModelTestState = Await GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel
            VerifyOpeningState(viewModel, "public ref int M(int[,] x)")
            VerifyParameterInfo(
                viewModel,
                parameterIndex:=0,
                type:="int[,]")
        End Function

        Private Sub VerifyAlteredState(
           viewModelTestState As ChangeSignatureViewModelTestState,
           Optional monitor As PropertyChangedTestMonitor = Nothing,
           Optional isOkButtonEnabled As Boolean? = Nothing,
           Optional canMoveUp As Boolean? = Nothing,
           Optional canMoveDown As Boolean? = Nothing,
           Optional permutation As Integer() = Nothing,
           Optional signatureDisplay As String = Nothing)

            Dim viewModel = viewModelTestState.ViewModel

            If monitor IsNot Nothing Then
                monitor.VerifyExpectations()
            End If

            If isOkButtonEnabled IsNot Nothing Then
                Assert.Equal(isOkButtonEnabled, viewModel.IsOkButtonEnabled)
            End If

            If canMoveUp IsNot Nothing Then
                Assert.Equal(canMoveUp, viewModel.CanMoveUp)
            End If

            If canMoveDown IsNot Nothing Then
                Assert.Equal(canMoveDown, viewModel.CanMoveDown)
            End If

            If permutation IsNot Nothing Then
                AssertPermuted(permutation, viewModel.AllParameters, viewModelTestState.OriginalParameterList)
            End If

            If signatureDisplay IsNot Nothing Then
                Assert.Equal(signatureDisplay, viewModel.TEST_GetSignatureDisplayText())
            End If

        End Sub

        Private Sub AssertPermuted(permutation As Integer(), actualParameterList As List(Of ChangeSignatureDialogViewModel.ParameterViewModel), originalParameterList As ImmutableArray(Of IParameterSymbol))
            Dim finalParameterList = actualParameterList.Where(Function(p) Not p.IsRemoved)
            For index = 0 To permutation.Length - 1
                Dim expected = originalParameterList(permutation(index))
                Assert.Equal(expected, finalParameterList(index).ParameterSymbol)
            Next
        End Sub

        Private Sub VerifyOpeningState(viewModel As ChangeSignatureDialogViewModel, openingSignatureDisplay As String)
            Assert.Equal(openingSignatureDisplay, viewModel.TEST_GetSignatureDisplayText())
            Assert.False(viewModel.IsOkButtonEnabled)
            Assert.False(viewModel.TrySubmit)
            Assert.False(viewModel.CanMoveUp)
        End Sub

        Private Sub VerifyParameterInfo(
            viewModel As ChangeSignatureDialogViewModel,
            parameterIndex As Integer,
            Optional modifier As String = Nothing,
            Optional type As String = Nothing,
            Optional parameterName As String = Nothing,
            Optional defaultValue As String = Nothing,
            Optional isDisabled As Boolean? = Nothing,
            Optional isRemoved As Boolean? = Nothing,
            Optional needsBottomBorder As Boolean? = Nothing)

            Dim parameter = viewModel.AllParameters(parameterIndex)

            If modifier IsNot Nothing Then
                Assert.Equal(modifier, parameter.Modifier)
            End If

            If type IsNot Nothing Then
                Assert.Equal(type, parameter.Type)
            End If

            If parameterName IsNot Nothing Then
                Assert.Equal(parameterName, parameter.Parameter)
            End If

            If defaultValue IsNot Nothing Then
                Assert.Equal(defaultValue, parameter.Default)
            End If

            If isDisabled.HasValue Then
                Assert.Equal(isDisabled.Value, parameter.IsDisabled)
            End If

            If isRemoved.HasValue Then
                Assert.Equal(isRemoved.Value, parameter.IsRemoved)
            End If

            If needsBottomBorder.HasValue Then
                Assert.Equal(needsBottomBorder.Value, parameter.NeedsBottomBorder)
            End If


        End Sub

        Private Async Function GetViewModelTestStateAsync(
            markup As XElement,
            languageName As String) As Tasks.Task(Of ChangeSignatureViewModelTestState)

            Dim workspaceXml =
            <Workspace>
                <Project Language=<%= languageName %> CommonReferences="true" Features="refLocalsAndReturns">
                    <Document><%= markup.NormalizedValue.Replace(vbCrLf, vbLf) %></Document>
                </Project>
            </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If (Not doc.CursorPosition.HasValue) Then
                    Assert.True(False, "Missing caret location in document.")
                End If

                Dim tree = Await workspaceDoc.GetSyntaxTreeAsync()
                Dim token = Await tree.GetTouchingWordAsync(doc.CursorPosition.Value, workspaceDoc.Project.LanguageServices.GetService(Of ISyntaxFactsService)(), CancellationToken.None)
                Dim symbol = (Await workspaceDoc.GetSemanticModelAsync()).GetDeclaredSymbol(token.Parent)

                Dim viewModel = New ChangeSignatureDialogViewModel(
                    New TestNotificationService(),
                    ParameterConfiguration.Create(symbol.GetParameters().ToList(), symbol.IsExtensionMethod(), selectedIndex:=0),
                    symbol,
                    workspace.ExportProvider.GetExportedValue(Of IClassificationFormatMapService)().GetClassificationFormatMap("text"),
                    workspace.ExportProvider.GetExportedValue(Of ClassificationTypeMap)())
                Return New ChangeSignatureViewModelTestState(viewModel, symbol.GetParameters())
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestRefKindsDisplayedCorrectly() As Tasks.Task
            Dim includedInTest = {RefKind.None, RefKind.Ref, RefKind.Out, RefKind.In, RefKind.RefReadOnly}
            Assert.Equal(includedInTest, EnumUtilities.GetValues(Of RefKind)())

            Dim markup = <Text><![CDATA[
class Test
{
    private void Method$$(int p1, ref int p2, in int p3, out int p4) { }
}"]]></Text>

            Dim state = Await GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            VerifyOpeningState(state.ViewModel, "private void Method(int p1, ref int p2, in int p3, out int p4)")

            Assert.Equal("", state.ViewModel.AllParameters(0).Modifier)
            Assert.Equal("ref", state.ViewModel.AllParameters(1).Modifier)
            Assert.Equal("in", state.ViewModel.AllParameters(2).Modifier)
            Assert.Equal("out", state.ViewModel.AllParameters(3).Modifier)
        End Function
    End Class
End Namespace
