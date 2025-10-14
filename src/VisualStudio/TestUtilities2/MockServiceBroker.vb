' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.ComponentModel.Composition
Imports System.IO.Pipelines
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.ServiceHub.Framework
Imports Microsoft.VisualStudio.Shell.ServiceBroker
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    <PartNotDiscoverable>
    <Export(GetType(SVsFullAccessServiceBroker))>
    <Export(GetType(MockServiceBroker))>
    Friend Class MockServiceBroker
        Implements IServiceBroker

        Private ReadOnly _services As New ConcurrentDictionary(Of Type, Object)()

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Sub RegisterService(Of T)(service As T)
            _services.Add(GetType(T), service)
        End Sub

        Public Event AvailabilityChanged As EventHandler(Of BrokeredServicesChangedEventArgs) Implements IServiceBroker.AvailabilityChanged

        Public Function GetProxyAsync(Of T As Class)(serviceDescriptor As ServiceRpcDescriptor, Optional options As ServiceActivationOptions = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ValueTask(Of T) Implements IServiceBroker.GetProxyAsync
            Dim service As Object = Nothing
            If _services.TryGetValue(GetType(T), service) Then
                Return New ValueTask(Of T)(CType(service, T))
            End If

            Throw New InvalidOperationException("The MockServiceBroker does not have a registered service for " + GetType(T).FullName)
        End Function

        Public Function GetPipeAsync(serviceMoniker As ServiceMoniker, Optional options As ServiceActivationOptions = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ValueTask(Of IDuplexPipe) Implements IServiceBroker.GetPipeAsync
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
