' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
    Public Class SessionTests

        Public Class Model
        End Class

        <WpfFact>
        Public Sub SessionStopsWhenPresenterIsDismissed()
            Dim threadingContext = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExportedValue(Of IThreadingContext)
            Dim presenter = New Mock(Of IIntelliSensePresenterSession)
            Dim controller = New Mock(Of IController(Of Model))
            Dim session = New Session(Of IController(Of Model), Model, IIntelliSensePresenterSession)(
                controller.Object,
                New ModelComputation(Of Model)(threadingContext, controller.Object, TaskScheduler.Default),
                presenter.Object)

            presenter.Raise(Sub(p) AddHandler p.Dismissed, Nothing, New EventArgs())

            controller.Verify(Sub(c) c.StopModelComputation())
        End Sub

        <WpfFact>
        Public Sub PresenterIsDismissedWhenSessionIsStopped()
            Dim threadingContext = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExportedValue(Of IThreadingContext)
            Dim presenter = New Mock(Of IIntelliSensePresenterSession)
            Dim controller = New Mock(Of IController(Of Model))
            Dim session = New Session(Of IController(Of Model), Model, IIntelliSensePresenterSession)(
                controller.Object,
                New ModelComputation(Of Model)(threadingContext, controller.Object, TaskScheduler.Default),
                presenter.Object)

            session.Stop()

            presenter.Verify(Sub(p) p.Dismiss())
        End Sub
    End Class
End Namespace
