' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    <Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
    Public Class SessionTests

        Public Sub New()
            TestWorkspace.ResetThreadAffinity()
        End Sub

        Public Class Model
        End Class

        <WpfFact>
        Public Sub SessionStopsWhenPresenterIsDismissed()
            Dim presenter = New Mock(Of IIntelliSensePresenterSession)
            Dim controller = New Mock(Of IController(Of Model))
            Dim session = New Session(Of IController(Of Model), Model, IIntelliSensePresenterSession)(
                controller.Object,
                New ModelComputation(Of Model)(controller.Object, TaskScheduler.Default),
                presenter.Object)

            presenter.Raise(Sub(p) AddHandler p.Dismissed, Nothing, New EventArgs())

            controller.Verify(Sub(c) c.StopComputationAndDismissPresentation())
        End Sub

        <WpfFact>
        Public Sub PresenterIsNotDismissedWhenSessionIsStopped()
            Dim presenter = New Mock(Of IIntelliSensePresenterSession)
            Dim controller = New Mock(Of IController(Of Model))
            Dim session = New Session(Of IController(Of Model), Model, IIntelliSensePresenterSession)(
                controller.Object,
                New ModelComputation(Of Model)(controller.Object, TaskScheduler.Default),
                presenter.Object)

            session.StopComputation()

            presenter.Verify(Sub(p) p.Dismiss(), Times.Never)
        End Sub
    End Class
End Namespace
