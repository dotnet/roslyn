' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.Internal.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Moq
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests

    <PartNotDiscoverable>
    <Export>
    <Export(GetType(SVsServiceProvider))>
    <Export(GetType(SAsyncServiceProvider))>
    Friend Class MockServiceProvider
        Implements IServiceProvider
        Implements SVsServiceProvider ' The shell service provider actually implements this too for people using that type directly
        Implements IAsyncServiceProvider
        Implements IAsyncServiceProvider2

        Private ReadOnly _exportProvider As Composition.ExportProvider
        Private ReadOnly _fileChangeEx As New MockVsFileChangeEx

        Public MockRunningDocumentTable As New MockVsRunningDocumentTable

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(exportProvider As Composition.ExportProvider)
            _exportProvider = exportProvider
        End Sub

        Public Function GetService(serviceType As Type) As Object Implements IServiceProvider.GetService
            Select Case serviceType
                Case GetType(SVsSolution), GetType(SVsShell)
                    ' Return a loose mock that just is a big no-op
                    Dim solutionMock As New Mock(Of IVsSolution2)(MockBehavior.Loose)
                    Return solutionMock.Object

                Case GetType(SVsBackgroundSolution)
                    Return GetVsBackgroundSolutionMock()

                Case GetType(SComponentModel)
                    Return GetComponentModelMock()

                Case GetType(SVsXMLMemberIndexService)
                    Return New MockXmlMemberIndexService

                Case GetType(SVsSmartOpenScope)
                    Return New MockVsSmartOpenScope

                Case GetType(SVsFileChangeEx)
                    Return _fileChangeEx

                Case GetType(SVsRunningDocumentTable)
                    Return MockRunningDocumentTable

                Case Else
                    Throw New Exception($"{NameOf(MockServiceProvider)} does not implement {serviceType.FullName}.")
            End Select
        End Function

        Public Function GetServiceAsync(serviceType As Type) As Task(Of Object) Implements IAsyncServiceProvider.GetServiceAsync
            Return GetServiceAsync(serviceType, False)
        End Function

        Public Function GetServiceAsync(serviceType As Type, swallowExceptions As Boolean) As Task(Of Object) Implements IAsyncServiceProvider2.GetServiceAsync
            Try
                Return Task.FromResult(GetService(serviceType))
            Catch ex As Exception When swallowExceptions
                Return SpecializedTasks.Null(Of Object)()
            End Try
        End Function

        Friend Function GetComponentModelMock() As IComponentModel
            Return New MockComponentModel(_exportProvider)
        End Function

        Private Shared Function GetVsBackgroundSolutionMock() As IVsBackgroundSolution
            ' Return a simple mock that lets callers subscribe to events
            Dim mock = New Mock(Of IVsBackgroundSolution)(MockBehavior.Strict)

            mock.Setup(Function(s) s.SubscribeListener(It.IsAny(Of Object))).Returns(
                Function()
                    Return New Mock(Of IVsBackgroundDisposable)().Object
                End Function)
            mock.SetupGet(Function(s) s.IsSolutionOpening).Returns(False)

            Return mock.Object
        End Function
    End Class

End Namespace
