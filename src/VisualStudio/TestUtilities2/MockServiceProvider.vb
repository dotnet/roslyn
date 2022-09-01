' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Moq

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests

    <PartNotDiscoverable>
    <Export>
    <Export(GetType(SVsServiceProvider))>
    Friend Class MockServiceProvider
        Implements IServiceProvider
        Implements SVsServiceProvider ' The shell service provider actually implements this too for people using that type directly
        Implements IAsyncServiceProvider

        Private ReadOnly _exportProvider As Composition.ExportProvider
        Private ReadOnly _fileChangeEx As MockVsFileChangeEx = New MockVsFileChangeEx

        Public MockMonitorSelection As IVsMonitorSelection

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

                Case GetType(SComponentModel)
                    Return GetComponentModelMock()

                Case GetType(SVsShellMonitorSelection)
                    Return MockMonitorSelection

                Case GetType(SVsXMLMemberIndexService)
                    Return New MockXmlMemberIndexService

                Case GetType(SVsSmartOpenScope)
                    Return New MockVsSmartOpenScope

                Case GetType(SVsFileChangeEx)
                    Return _fileChangeEx

                Case Else
                    Throw New Exception($"{NameOf(MockServiceProvider)} does not implement {serviceType.FullName}.")
            End Select
        End Function

        Public Function GetServiceAsync(serviceType As Type) As Task(Of Object) Implements IAsyncServiceProvider.GetServiceAsync
            Return System.Threading.Tasks.Task.FromResult(GetService(serviceType))
        End Function

        Friend Function GetComponentModelMock() As IComponentModel
            Return New MockComponentModel(_exportProvider)
        End Function
    End Class

End Namespace
