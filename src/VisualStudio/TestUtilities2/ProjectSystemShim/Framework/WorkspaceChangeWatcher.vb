' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.TestHooks

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
    Friend Class WorkspaceChangeWatcher
        Implements IDisposable

        Private ReadOnly _environment As TestEnvironment
        Private ReadOnly _asynchronousOperationWaiter As IAsynchronousOperationWaiter
        Private _changeEvents As New List(Of WorkspaceChangeEventArgs)
        Private ReadOnly _workspaceChangedDisposer As IDisposable

        Public Sub New(environment As TestEnvironment)
            _environment = environment

            Dim listenerProvider = environment.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)()
            _asynchronousOperationWaiter = listenerProvider.GetWaiter(FeatureAttribute.Workspace)

            _workspaceChangedDisposer = environment.Workspace.RegisterWorkspaceChangedHandler(AddressOf OnWorkspaceChangedAsync)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            _workspaceChangedDisposer.Dispose()
        End Sub

        Private Function OnWorkspaceChangedAsync(e As WorkspaceChangeEventArgs, cancellationToken As CancellationToken) As Task
            _changeEvents.Add(e)

            Return Task.CompletedTask
        End Function

        Friend Async Function GetNewChangeEventsAsync() As Task(Of IEnumerable(Of WorkspaceChangeEventArgs))
            Await _asynchronousOperationWaiter.ExpeditedWaitAsync()

            ' Return the events so far, clearing the list if somebody wants to ask for further events
            Dim changeEvents = _changeEvents
            _changeEvents = New List(Of WorkspaceChangeEventArgs)()
            Return changeEvents
        End Function
    End Class
End Namespace
