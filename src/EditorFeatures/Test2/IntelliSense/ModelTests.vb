' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
    Public Class ModelTests
        Public Class Model
        End Class

        Private Class TestModelComputation
            Inherits ModelComputation(Of Model)

            Public Sub New(threadingContext As IThreadingContext, controller As IController(Of Model))
                MyBase.New(threadingContext, controller, TaskScheduler.Default)
            End Sub

            Friend Shared Function Create(threadingContext As IThreadingContext, Optional controller As IController(Of Model) = Nothing) As TestModelComputation
                If controller Is Nothing Then
                    Dim mock = New Mock(Of IController(Of Model))
                    controller = mock.Object
                End If

                Return New TestModelComputation(threadingContext, controller)
            End Function

            Friend Sub Wait()
                WaitForController()
            End Sub
        End Class

        <WpfFact>
        Public Sub ChainingTaskStartsAsyncOperation()
            Dim threadingContext = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExportedValue(Of IThreadingContext)
            Dim controller = New Mock(Of IController(Of Model))
            Dim modelComputation = TestModelComputation.Create(threadingContext, controller:=controller.Object)

            modelComputation.ChainTaskAndNotifyControllerWhenFinished(Function(m) m)

            controller.Verify(Sub(c) c.BeginAsyncOperation(
                                  It.IsAny(Of String),
                                  Nothing,
                                  It.IsAny(Of String),
                                  It.IsAny(Of Integer)))
        End Sub

        <WpfFact>
        Public Sub ChainingTaskThatCompletesNotifiesController()
            Dim threadingContext = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExportedValue(Of IThreadingContext)
            Dim controller = New Mock(Of IController(Of Model))
            Dim modelComputation = TestModelComputation.Create(threadingContext, controller:=controller.Object)
            Dim model = New Model()

            modelComputation.ChainTaskAndNotifyControllerWhenFinished(Function(m) model)
            modelComputation.Wait()

            controller.Verify(Sub(c) c.OnModelUpdated(model))
        End Sub

        <WpfFact>
        Public Sub ControllerIsOnlyUpdatedAfterLastTaskCompletes()
            Dim threadingContext = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExportedValue(Of IThreadingContext)
            Dim controller = New Mock(Of IController(Of Model))
            Dim modelComputation = TestModelComputation.Create(threadingContext, controller:=controller.Object)
            Dim model = New Model()
            Dim gate = New Object

            Monitor.Enter(gate)
            modelComputation.ChainTaskAndNotifyControllerWhenFinished(Function(m)
                                                                          SyncLock gate
                                                                              Return Nothing
                                                                          End SyncLock
                                                                      End Function)
            modelComputation.ChainTaskAndNotifyControllerWhenFinished(Function(m) model)
            Monitor.Exit(gate)
            modelComputation.Wait()

            controller.Verify(Sub(c) c.OnModelUpdated(model), Times.Once)
        End Sub

        <WpfFact>
        Public Async Function ControllerIsNotUpdatedIfComputationIsCancelled() As Task
            Dim threadingContext = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExportedValue(Of IThreadingContext)
            Dim controller = New Mock(Of IController(Of Model))
            Dim token = New Mock(Of IAsyncToken)
            controller.Setup(Function(c) c.BeginAsyncOperation(
                                 It.IsAny(Of String),
                                 Nothing,
                                 It.IsAny(Of String),
                                 It.IsAny(Of Integer))).Returns(token.Object)
            Dim modelComputation = TestModelComputation.Create(threadingContext, controller:=controller.Object)
            Dim model = New Model()
            Dim checkpoint1 = New Checkpoint
            Dim checkpoint2 = New Checkpoint
            Dim checkpoint3 = New Checkpoint

            token.Setup(Sub(t) t.Dispose()).Callback(Sub() checkpoint3.Release())

            modelComputation.ChainTaskAndNotifyControllerWhenFinished(Function(m, c)
                                                                          checkpoint1.Release()
                                                                          checkpoint2.Task.Wait()
                                                                          c.ThrowIfCancellationRequested()
                                                                          Return Task.FromResult(model)
                                                                      End Function)
            Await checkpoint1.Task
            modelComputation.Stop()
            checkpoint2.Release()
            Await checkpoint3.Task

            controller.Verify(Sub(c) c.OnModelUpdated(model), Times.Never)
        End Function

    End Class
End Namespace
