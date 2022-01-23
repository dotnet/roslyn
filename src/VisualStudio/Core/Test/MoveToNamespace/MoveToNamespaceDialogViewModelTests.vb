' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CSharp.LanguageServices
Imports Microsoft.CodeAnalysis.Test.Utilities
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

            viewModel.NamespaceName = viewModel.AvailableNamespaces.First().Namespace

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

            viewModel.NamespaceName = viewModel.AvailableNamespaces.Last().Namespace & ".NewNamespace"

            monitor.VerifyExpectations()
            monitor.Detach()

            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(viewModel.Icon, KnownMonikers.StatusInformation)

            viewModel.NamespaceName = viewModel.AvailableNamespaces.First().Namespace

            Assert.True(viewModel.CanSubmit)
            Assert.False(viewModel.ShowMessage)
        End Sub

        Private Shared Function CreateViewModel(Optional defaultNamespace As String = Nothing, Optional availableNamespaces As ImmutableArray(Of String) = Nothing) As MoveToNamespaceDialogViewModel
            If (defaultNamespace Is Nothing) Then
                defaultNamespace = "Default.Namespace"
            End If

            If (availableNamespaces = Nothing) Then
                availableNamespaces = ImmutableArray.Create({
                                                            defaultNamespace,
                                                            defaultNamespace & "1",
                                                            defaultNamespace & "2"})
            End If

            Return New MoveToNamespaceDialogViewModel(defaultNamespace, availableNamespaces, CSharpSyntaxFacts.Instance, ImmutableArray(Of String).Empty)
        End Function
    End Class
End Namespace

