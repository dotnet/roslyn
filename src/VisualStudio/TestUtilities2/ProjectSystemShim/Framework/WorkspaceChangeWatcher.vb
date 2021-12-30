' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
    Friend Class WorkspaceChangeWatcher
        Implements IDisposable

        Private ReadOnly _environment As TestEnvironment
        Private ReadOnly _asynchronousOperationWaiter As IAsynchronousOperationWaiter
        Private _changeEvents As New List(Of WorkspaceChangeEventArgs)

        Public Sub New(environment As TestEnvironment)
            _environment = environment

            Dim listenerProvider = environment.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)()
            _asynchronousOperationWaiter = listenerProvider.GetWaiter(FeatureAttribute.Workspace)

            AddHandler environment.Workspace.WorkspaceChanged, AddressOf OnWorkspaceChanged
        End Sub

        Private Sub OnWorkspaceChanged(sender As Object, e As WorkspaceChangeEventArgs)
            _changeEvents.Add(e)
        End Sub

        Friend Async Function GetNewChangeEventsAsync() As Task(Of IEnumerable(Of WorkspaceChangeEventArgs))
            Await _asynchronousOperationWaiter.ExpeditedWaitAsync()

            ' Return the events so far, clearing the list if somebody wants to ask for further events
            Dim changeEvents = _changeEvents
            _changeEvents = New List(Of WorkspaceChangeEventArgs)()
            Return changeEvents
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            RemoveHandler _environment.Workspace.WorkspaceChanged, AddressOf OnWorkspaceChanged
        End Sub
    End Class
End Namespace
