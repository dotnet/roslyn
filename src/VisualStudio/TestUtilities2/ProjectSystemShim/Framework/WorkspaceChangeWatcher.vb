' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.TestHooks

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
    Friend Class WorkspaceChangeWatcher
        Implements IDisposable

        Private ReadOnly _asynchronousOperationWaiter As IAsynchronousOperationWaiter
        Private ReadOnly _workspaceChangedDisposer As WorkspaceEventRegistration
        Private _changeEvents As New List(Of WorkspaceChangeEventArgs)

        Public Sub New(environment As TestEnvironment)
            Dim listenerProvider = environment.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)()
            _asynchronousOperationWaiter = listenerProvider.GetWaiter(FeatureAttribute.Workspace)

            _workspaceChangedDisposer = environment.Workspace.RegisterWorkspaceChangedHandler(AddressOf OnWorkspaceChanged)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            _workspaceChangedDisposer.Dispose()
        End Sub

        Private Sub OnWorkspaceChanged(e As WorkspaceChangeEventArgs)
            _changeEvents.Add(e)
        End Sub

        Friend Async Function GetNewChangeEventsAsync() As Task(Of IEnumerable(Of WorkspaceChangeEventArgs))
            Await _asynchronousOperationWaiter.ExpeditedWaitAsync()

            ' Return the events so far, clearing the list if somebody wants to ask for further events
            Dim changeEvents = _changeEvents
            _changeEvents = New List(Of WorkspaceChangeEventArgs)()
            Return changeEvents
        End Function
    End Class
End Namespace
