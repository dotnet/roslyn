' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature.ChangeSignatureDialogViewModel
Imports Microsoft.VisualStudio.Text.Classification
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ChangeSignature
    <[UseExportProvider]>
    Public Class ChangeSignatureViewModelTests

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
                canCommit:=False,
                canMoveUp:=False,
                canMoveDown:=False)

            monitor.Detach()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function ReorderParameters_MethodWithTwoNormalParameters_CannotCommitAfterPermutationsResultingInOriginalOrdering() As Tasks.Task
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
            Dim message As String = Nothing
            Assert.False(viewModel.CanSubmit(message))
            Assert.Equal(ServicesVSResources.You_must_change_the_signature, message)

            VerifyAlteredState(
                viewModelTestState,
                canCommit:=False,
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
                canCommit:=True,
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
                canCommit:=True,
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

            Dim selectionChangedMonitor = New PropertyChangedTestMonitor(viewModel, strict:=True)
            selectionChangedMonitor.AddExpectation(Function() viewModel.CanMoveUp)
            selectionChangedMonitor.AddExpectation(Function() viewModel.MoveUpAutomationText)
            selectionChangedMonitor.AddExpectation(Function() viewModel.CanMoveDown)
            selectionChangedMonitor.AddExpectation(Function() viewModel.MoveDownAutomationText)
            selectionChangedMonitor.AddExpectation(Function() viewModel.CanRemove)
            selectionChangedMonitor.AddExpectation(Function() viewModel.RemoveAutomationText)
            selectionChangedMonitor.AddExpectation(Function() viewModel.CanRestore)
            selectionChangedMonitor.AddExpectation(Function() viewModel.RestoreAutomationText)

            viewModel.SelectedIndex = 1

            selectionChangedMonitor.VerifyExpectations()
            selectionChangedMonitor.Detach()

            VerifyAlteredState(
                viewModelTestState,
                canMoveUp:=True,
                canMoveDown:=False)

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
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
                canCommit:=True,
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

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        <WorkItem(30315, "https://github.com/dotnet/roslyn/issues/30315")>
        Public Async Function ChangeSignature_ParameterDisplay_Nullable() As Tasks.Task
            Dim markup = <Text><![CDATA[
#nullable enable
class MyClass
{
    public string? $$M(string? x)
    {
    }
}"]]></Text>

            Dim viewModelTestState = Await GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel
            VerifyOpeningState(viewModel, "public string? M(string? x)")
            VerifyParameterInfo(
                viewModel,
                parameterIndex:=0,
                type:="string?")
        End Function

        <WorkItem(8437, "https://github.com/dotnet/roslyn/issues/8437")>
        Public Async Function ChangeSignature_VerifyParamsArrayFunctionality() As Tasks.Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    public ref int $$M(int x, params int[] y)
      {
    }
}"]]></Text>

            Dim viewModelTestState = Await GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel
            VerifyOpeningState(viewModel, "public ref int M(int x, params int[] y)")

            viewModel.SelectedIndex = 1

            VerifyAlteredState(viewModelTestState,
                canMoveUp:=False,
                canMoveDown:=False,
                canRemove:=True)
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

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        <WorkItem(30315, "https://github.com/dotnet/roslyn/issues/30315")>
        Public Async Function ChangeSignature_ParameterDisplay_DefaultStruct() As Tasks.Task
            Dim markup = <Text><![CDATA[
struct MyStruct
{

}

class Goo
{
    void $$Bar(MyStruct s = default(MyStruct))
    {

    }
}]]></Text>

            Dim viewModelTestState = Await GetViewModelTestStateAsync(markup, LanguageNames.CSharp)
            Dim viewModel = viewModelTestState.ViewModel
            VerifyOpeningState(viewModel, "private void Bar(MyStruct s = default(MyStruct))")
            VerifyParameterInfo(
                viewModel,
                parameterIndex:=0,
                type:="MyStruct",
                defaultValue:="default")
        End Function

        Private Shared Sub VerifyAlteredState(
           viewModelTestState As ChangeSignatureViewModelTestState,
           Optional monitor As PropertyChangedTestMonitor = Nothing,
           Optional canCommit As Boolean? = Nothing,
           Optional canMoveUp As Boolean? = Nothing,
           Optional canMoveDown As Boolean? = Nothing,
           Optional canRemove As Boolean? = Nothing,
           Optional canRestore As Boolean? = Nothing,
           Optional permutation As Integer() = Nothing,
           Optional signatureDisplay As String = Nothing)

            Dim viewModel = viewModelTestState.ViewModel

            If monitor IsNot Nothing Then
                monitor.VerifyExpectations()
            End If

            If canCommit IsNot Nothing Then
                Dim message As String = Nothing
                Assert.Equal(canCommit, viewModel.CanSubmit(message))

                If canCommit.Value Then
                    Assert.True(viewModel.TrySubmit())
                End If
            End If

            If canMoveUp IsNot Nothing Then
                Assert.Equal(canMoveUp, viewModel.CanMoveUp)
            End If

            If canMoveDown IsNot Nothing Then
                Assert.Equal(canMoveDown, viewModel.CanMoveDown)
            End If

            If canRemove IsNot Nothing Then
                Assert.Equal(canRemove, viewModel.CanRemove)
            End If

            If canRestore IsNot Nothing Then
                Assert.Equal(canRestore, viewModel.CanRestore)
            End If

            If permutation IsNot Nothing Then
                AssertPermuted(permutation, viewModel.AllParameters, viewModelTestState.OriginalParameterList)
            End If

            If signatureDisplay IsNot Nothing Then
                Assert.Equal(signatureDisplay, viewModel.TEST_GetSignatureDisplayText())
            End If

        End Sub

        Private Shared Sub AssertPermuted(permutation As Integer(), actualParameterList As List(Of ChangeSignatureDialogViewModel.ParameterViewModel), originalParameterList As ImmutableArray(Of IParameterSymbol))
            Dim finalParameterList = actualParameterList.Where(Function(p) Not p.IsRemoved)
            For index = 0 To permutation.Length - 1
                Dim expected = originalParameterList(permutation(index))
                Assert.Equal(expected, DirectCast(finalParameterList(index), ExistingParameterViewModel).ParameterSymbol)
            Next
        End Sub

        Private Shared Sub VerifyOpeningState(viewModel As ChangeSignatureDialogViewModel, openingSignatureDisplay As String)
            Assert.Equal(openingSignatureDisplay, viewModel.TEST_GetSignatureDisplayText())
            Dim message As String = Nothing
            Assert.False(viewModel.CanSubmit(message))
            Assert.Equal(ServicesVSResources.You_must_change_the_signature, message)
            Assert.False(viewModel.CanMoveUp)
        End Sub

        Private Shared Sub VerifyParameterInfo(
            viewModel As ChangeSignatureDialogViewModel,
                parameterIndex As Integer,
                Optional modifier As String = Nothing,
                Optional type As String = Nothing,
                Optional parameterName As String = Nothing,
                Optional defaultValue As String = Nothing,
                Optional isDisabled As Boolean? = Nothing,
                Optional isRemoved As Boolean? = Nothing)

            Dim parameter = viewModel.AllParameters(parameterIndex)

            If modifier IsNot Nothing Then
                Assert.Equal(modifier, parameter.Modifier)
            End If

            If type IsNot Nothing Then
                Assert.Equal(type, parameter.Type)
            End If

            If parameterName IsNot Nothing Then
                Assert.Equal(parameterName, parameter.ParameterName)
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
        End Sub

        Private Shared Async Function GetViewModelTestStateAsync(
            markup As XElement,
            languageName As String) As Tasks.Task(Of ChangeSignatureViewModelTestState)

            Dim workspaceXml =
            <Workspace>
                <Project Language=<%= languageName %> CommonReferences="true">
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
                    ParameterConfiguration.Create(symbol.GetParameters().Select(Function(p) DirectCast(New ExistingParameter(p), Parameter)).ToImmutableArray(), symbol.IsExtensionMethod(), selectedIndex:=0),
                    symbol,
                    workspaceDoc,
                    positionForTypeBinding:=0,
                    workspace.ExportProvider.GetExportedValue(Of IClassificationFormatMapService)().GetClassificationFormatMap("text"),
                    workspace.ExportProvider.GetExportedValue(Of ClassificationTypeMap)())
                Return New ChangeSignatureViewModelTestState(viewModel, symbol.GetParameters())
            End Using
        End Function
    End Class
End Namespace
