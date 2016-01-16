' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Roslyn.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ChangeSignature
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
            monitor.AddExpectation(Function() viewModel.AllParameters)
            monitor.AddExpectation(Function() viewModel.CanMoveUp)
            monitor.AddExpectation(Function() viewModel.CanMoveDown)

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
            monitor.AddExpectation(Function() viewModel.AllParameters)
            monitor.AddExpectation(Function() viewModel.CanMoveUp)
            monitor.AddExpectation(Function() viewModel.CanMoveDown)

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
                AssertPermuted({1, 0}, viewModel.AllParameters, viewModelTestState.OriginalParameterList)
            End If

            If signatureDisplay IsNot Nothing Then
                Assert.Equal(signatureDisplay, viewModel.TEST_GetSignatureDisplayText())
            End If

        End Sub

        Private Sub AssertPermuted(permutation As Integer(), actualParameterList As List(Of ChangeSignatureDialogViewModel.ParameterViewModel), originalParameterList As ImmutableArray(Of IParameterSymbol))
            For index = 0 To permutation.Length - 1
                Dim expected = originalParameterList(permutation(index))
                Assert.Equal(expected, actualParameterList(index).ParameterSymbol)
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

        Private Async Function GetViewModelTestStateAsync(markup As XElement, languageName As String) As Tasks.Task(Of ChangeSignatureViewModelTestState)

            Dim workspaceXml =
            <Workspace>
                <Project Language=<%= languageName %> CommonReferences="true">
                    <Document><%= markup.NormalizedValue.Replace(vbCrLf, vbLf) %></Document>
                </Project>
            </Workspace>

            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If (Not doc.CursorPosition.HasValue) Then
                    Assert.True(False, "Missing caret location in document.")
                End If

                Dim tree = Await workspaceDoc.GetSyntaxTreeAsync()
                Dim token = Await tree.GetTouchingWordAsync(doc.CursorPosition.Value, workspaceDoc.Project.LanguageServices.GetService(Of ISyntaxFactsService)(), CancellationToken.None)
                Dim symbol = (Await workspaceDoc.GetSemanticModelAsync()).GetDeclaredSymbol(token.Parent)

                Dim viewModel = New ChangeSignatureDialogViewModel(New TestNotificationService(), ParameterConfiguration.Create(symbol.GetParameters().ToList(), symbol.IsExtensionMethod()), symbol, workspace.ExportProvider.GetExport(Of ClassificationTypeMap)().Value)
                Return New ChangeSignatureViewModelTestState(viewModel, symbol.GetParameters())
            End Using
        End Function
    End Class
End Namespace
