' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.MoveToNamespace
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.MoveToNamespace

    <[UseExportProvider]>
    Public Class MoveToNamespaceDialogViewModelTests
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToNamespace)>
        Public Sub TestMoveToNamespace_ErrorState()
            Dim viewModel = CreateViewModel()

            Assert.True(viewModel.CanSubmit)
            Assert.False(viewModel.ShowMessage)

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.CanSubmit)
            monitor.AddExpectation(Function() viewModel.NamespaceName)
            monitor.AddExpectation(Function() viewModel.ShowMessage)
            monitor.AddExpectation(Function() viewModel.Message)
            monitor.AddExpectation(Function() viewModel.Icon)

            viewModel.NamespaceName = "2InvalidNamespace"

            monitor.VerifyExpectations()
            monitor.Detach()

            Assert.False(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(viewModel.Icon, KnownMonikers.StatusInvalid)

            viewModel.NamespaceName = viewModel.AvailableNamespaces.First()

            Assert.True(viewModel.CanSubmit)
            Assert.False(viewModel.ShowMessage)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToNamespace)>
        Public Sub TestMoveToNamespace_NewNamespaceState()
            Dim viewModel = CreateViewModel()

            Assert.True(viewModel.CanSubmit)
            Assert.False(viewModel.ShowMessage)

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.NamespaceName)
            monitor.AddExpectation(Function() viewModel.ShowMessage)
            monitor.AddExpectation(Function() viewModel.Message)
            monitor.AddExpectation(Function() viewModel.Icon)

            viewModel.NamespaceName = viewModel.AvailableNamespaces.Last() & ".NewNamespace"

            monitor.VerifyExpectations()
            monitor.Detach()

            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(viewModel.Icon, KnownMonikers.StatusInformation)

            viewModel.NamespaceName = viewModel.AvailableNamespaces.First()

            Assert.True(viewModel.CanSubmit)
            Assert.False(viewModel.ShowMessage)
        End Sub

        Private Function CreateViewModel(Optional defaultNamespace As String = Nothing, Optional availableNamespaces As ImmutableArray(Of String) = Nothing) As MoveToNamespaceDialogViewModel
            If (defaultNamespace Is Nothing) Then
                defaultNamespace = "Default.Namespace"
            End If

            If (availableNamespaces = Nothing) Then
                availableNamespaces = ImmutableArray.Create({
                                                            defaultNamespace,
                                                            defaultNamespace & "1",
                                                            defaultNamespace & "2"})
            End If

            Return New MoveToNamespaceDialogViewModel(defaultNamespace, availableNamespaces, CSharpSyntaxFactsService.Instance)
        End Function
    End Class
End Namespace

