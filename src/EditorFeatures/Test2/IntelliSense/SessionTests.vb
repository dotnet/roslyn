' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim threadingContext = exportProvider.GetExportedValue(Of IThreadingContext)
            Dim presenter = New Mock(Of IIntelliSensePresenterSession)(MockBehavior.Strict)
            Dim controller = New Mock(Of IController(Of Model))(MockBehavior.Strict)
            controller.Setup(Sub(c) c.StopModelComputation())
            Dim session = New Session(Of IController(Of Model), Model, IIntelliSensePresenterSession)(
                controller.Object,
                New ModelComputation(Of Model)(threadingContext, controller.Object, TaskScheduler.Default),
                presenter.Object)

            presenter.Raise(Sub(p) AddHandler p.Dismissed, Nothing, New EventArgs())

            controller.Verify(Sub(c) c.StopModelComputation())
        End Sub

        <WpfFact>
        Public Sub PresenterIsDismissedWhenSessionIsStopped()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim threadingContext = exportProvider.GetExportedValue(Of IThreadingContext)
            Dim presenter = New Mock(Of IIntelliSensePresenterSession)(MockBehavior.Strict)
            presenter.Setup(Sub(p) p.Dismiss())
            Dim controller = New Mock(Of IController(Of Model))(MockBehavior.Strict)
            Dim session = New Session(Of IController(Of Model), Model, IIntelliSensePresenterSession)(
                controller.Object,
                New ModelComputation(Of Model)(threadingContext, controller.Object, TaskScheduler.Default),
                presenter.Object)

            session.Stop()

            presenter.Verify(Sub(p) p.Dismiss())
        End Sub
    End Class
End Namespace
