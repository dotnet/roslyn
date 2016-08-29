' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    <Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
    Public Class ModelTests
        Public Sub New()
            TestWorkspace.ResetThreadAffinity()
        End Sub

        Public Class Model
        End Class

        Private Class TestModelComputation
            Inherits ModelComputation(Of Model)

            Public Sub New(controller As IController(Of Model))
                MyBase.New(controller, TaskScheduler.Default)
            End Sub

            Friend Shared Function Create(Optional controller As IController(Of Model) = Nothing) As TestModelComputation
                If controller Is Nothing Then
                    Dim mock = New Mock(Of IController(Of Model))
                    controller = mock.Object
                End If

                Return New TestModelComputation(controller)
            End Function

            Friend Sub Wait()
                WaitForController()
            End Sub
        End Class

        <WpfFact>
        Public Sub ChainingTaskStartsAsyncOperation()
            Dim controller = New Mock(Of IController(Of Model))
            Dim modelComputation = TestModelComputation.Create(controller:=controller.Object)

            modelComputation.ChainTaskAndNotifyControllerWhenFinished(Function(m) m)

            controller.Verify(Sub(c) c.BeginAsyncOperation(
                                  It.IsAny(Of String),
                                  Nothing,
                                  It.IsAny(Of String),
                                  It.IsAny(Of Integer)))
        End Sub

        <WpfFact>
        Public Sub ChainingTaskThatCompletesNotifiesController()
            Dim controller = New Mock(Of IController(Of Model))
            Dim modelComputation = TestModelComputation.Create(controller:=controller.Object)
            Dim model = New Model()

            modelComputation.ChainTaskAndNotifyControllerWhenFinished(Function(m) model)
            modelComputation.Wait()

            controller.Verify(Sub(c) c.OnModelUpdated(model))
        End Sub

        <WpfFact>
        Public Sub ControllerIsOnlyUpdatedAfterLastTaskCompletes()
            Dim controller = New Mock(Of IController(Of Model))
            Dim modelComputation = TestModelComputation.Create(controller:=controller.Object)
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
            Dim controller = New Mock(Of IController(Of Model))
            Dim token = New Mock(Of IAsyncToken)
            controller.Setup(Function(c) c.BeginAsyncOperation(
                                 It.IsAny(Of String),
                                 Nothing,
                                 It.IsAny(Of String),
                                 It.IsAny(Of Integer))).Returns(token.Object)
            Dim modelComputation = TestModelComputation.Create(controller:=controller.Object)
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
