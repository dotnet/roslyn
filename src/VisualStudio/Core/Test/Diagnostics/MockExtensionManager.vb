' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Xml
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
Imports Moq
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Public Class MockExtensionManager

        Private ReadOnly _contentType As String
        Private ReadOnly _locations() As String

        Public Sub New(contentType As String, ParamArray locations() As String)
            _contentType = contentType
            _locations = locations
        End Sub

        Public Function GetEnabledExtensionContentLocations(contentTypeName As String) As IEnumerable(Of String)
            Assert.Equal(_contentType, contentTypeName)
            Return _locations
        End Function

        Public Iterator Function GetEnabledExtensions(contentTypeName As String) As IEnumerable(Of Object)
            Assert.Equal(_contentType, contentTypeName)

            For Each location In _locations
                Dim installedExtensionMock As New Mock(Of IMockInstalledExtension)(MockBehavior.Strict)

                installedExtensionMock.SetupGet(Function(m) m.Content).Returns(
                    New MockContent() {
                        New MockContent(_contentType, location),
                        New MockContent(_contentType, VisualStudioWorkspaceDiagnosticAnalyzerProviderService.MicrosoftCodeAnalysisCSharp),
                        New MockContent(_contentType, VisualStudioWorkspaceDiagnosticAnalyzerProviderService.MicrosoftCodeAnalysisVisualBasic)
                    })

                installedExtensionMock.Setup(Function(m) m.GetContentLocation(It.IsAny(Of MockContent))).Returns(Function(content As MockContent)
                                                                                                                     If content.RelativePath.IndexOf("$RootFolder$") >= 0 Then
                                                                                                                         Return content.RelativePath.Replace("$RootFolder$", "ResolvedRootFolder")
                                                                                                                     ElseIf content.RelativePath.IndexOf("$ShellFolder$") >= 0 Then
                                                                                                                         Return content.RelativePath.Replace("$ShellFolder$", "ResolvedShellFolder")
                                                                                                                     Else
                                                                                                                         Return Path.Combine("\InstallPath", content.RelativePath)
                                                                                                                     End If
                                                                                                                 End Function)

                Dim headerMock As New Mock(Of IMockHeader)(MockBehavior.Strict)
                headerMock.SetupGet(Function(h) h.LocalizedName).Returns("Vsix")

                installedExtensionMock.SetupGet(Function(m) m.Header).Returns(headerMock.Object)

                Yield installedExtensionMock.Object
            Next
        End Function

        Public Interface IMockInstalledExtension
            ReadOnly Property Content As IEnumerable(Of MockContent)
            Function GetContentLocation(content As MockContent) As String
            ReadOnly Property Header As IMockHeader
        End Interface

        Public Interface IMockHeader
            ReadOnly Property LocalizedName As String
        End Interface

        Public Class MockContent
            Private ReadOnly _contentType As String
            Private ReadOnly _location As String

            Public Sub New(contentType As String, location As String)
                _contentType = contentType
                _location = location
            End Sub

            Public ReadOnly Property ContentTypeName As String
                Get
                    Return _contentType
                End Get
            End Property

            Public ReadOnly Property RelativePath As String
                Get
                    Return _location
                End Get
            End Property
        End Class
    End Class
End Namespace
