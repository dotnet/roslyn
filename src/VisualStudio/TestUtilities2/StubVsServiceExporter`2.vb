' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Threading

' Import Roslyn.Utilities with an alias to avoid conflicts with AsyncLazy(Of T). This implementation relies on
' AsyncLazy(Of T) from vs-threading, and not the one from Roslyn.
Imports RoslynUtilities = Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests
    <Export(GetType(IVsService(Of ,)))>
    <PartCreationPolicy(CreationPolicy.NonShared)>
    <PartNotDiscoverable>
    Friend Class StubVsServiceExporter(Of TService As Class, TInterface As Class)
        Implements IVsService(Of TService, TInterface)

        Private ReadOnly _serviceGetter As AsyncLazy(Of TInterface)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
            <Import(GetType(SVsServiceProvider))> asyncServiceProvider As IAsyncServiceProvider2, joinableTaskContext As JoinableTaskContext)

            _serviceGetter = New AsyncLazy(Of TInterface)(
                Function() asyncServiceProvider.GetServiceAsync(Of TService, TInterface)(True, CancellationToken.None),
                joinableTaskContext.Factory)
        End Sub

        ''' <inheritdoc />
        Public Function GetValueAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of TInterface) Implements IVsService(Of TInterface).GetValueAsync
            Return _serviceGetter.GetValueAsync(cancellationToken)
        End Function

        ''' <inheritdoc />
        Public Function GetValueOrNullAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of TInterface) Implements IVsService(Of TService, TInterface).GetValueOrNullAsync
            Dim value = GetValueAsync(cancellationToken)

            If value.IsCompleted Then
                Return TransformResult(value)
            End If

            Return value.ContinueWith(
                Function(t) TransformResult(t),
                CancellationToken.None, ' token is already passed to antecedent, and this is a tiny sync continuation, so no need to make it also cancelable.
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default).Unwrap()
        End Function

        Private Shared Function TransformResult(task As Task(Of TInterface)) As Task(Of TInterface)
            Debug.Assert(task.IsCompleted)

            If task.Status = TaskStatus.Faulted Then
                ' Our caller never wants exceptions, so return a cached null value
                Return RoslynUtilities.SpecializedTasks.Null(Of TInterface)()
            Else
                ' Whether this is cancelled or ran to completion, we return the value as-is
                Return RoslynUtilities.SpecializedTasks.AsNullable(task)
            End If
        End Function
    End Class
End Namespace
