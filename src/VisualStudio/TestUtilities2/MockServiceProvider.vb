' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.Internal.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Settings
Imports Microsoft.VisualStudio.Settings.Internal
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
        Private ReadOnly _localRegistry As New StubLocalRegistry
        Private _settingsManager As ISettingsManager

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

                Case GetType(SLocalRegistry)
                    Return _localRegistry

                Case GetType(SVsSettingsPersistenceManager)
                    If _settingsManager Is Nothing Then
                        LoggerFactory.Reset()
                        _settingsManager = SettingsManagerFactory.CreateInstance(New StubSettingsManagerHost())
                    End If

                    Return _settingsManager

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
    End Class

End Namespace
