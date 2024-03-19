' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports System.IO
Imports System.Xml
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Moq
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Public Class MockExtensionManager
        Private ReadOnly _contentType As String
        Private ReadOnly _extensions As (Paths As String(), Id As String)()

        Public Sub New(Optional extensions As (Paths As String(), Id As String)() = Nothing,
                       Optional contentType As String = "Microsoft.VisualStudio.Analyzer")
            _contentType = contentType
            _extensions = If(extensions, Array.Empty(Of (Paths As String(), Id As String)))
        End Sub

        Public Iterator Function GetEnabledExtensions(contentTypeName As String) As IEnumerable(Of Object)
            Assert.Equal(_contentType, contentTypeName)

            For Each extension In _extensions
                For Each extensionPath In extension.Paths
                    Dim installedExtensionMock As New Mock(Of IMockInstalledExtension)(MockBehavior.Strict)

                    installedExtensionMock.SetupGet(Function(m) m.Content).Returns(
                    New MockContent() {
                        New MockContent(_contentType, extensionPath)
                    })

                    installedExtensionMock.Setup(Function(m) m.GetContentLocation(It.IsAny(Of MockContent))).Returns(
                    Function(content As MockContent)
                        If content.RelativePath.IndexOf("$RootFolder$") >= 0 Then
                            Return content.RelativePath.Replace("$RootFolder$", Path.Combine(TempRoot.Root, "ResolvedRootFolder"))
                        ElseIf content.RelativePath.IndexOf("$ShellFolder$") >= 0 Then
                            Return content.RelativePath.Replace("$ShellFolder$", Path.Combine(TempRoot.Root, "ResolvedShellFolder"))
                        Else
                            Return Path.Combine(TempRoot.Root, "InstallPath", content.RelativePath)
                        End If
                    End Function)

                    Dim headerMock As New Mock(Of IMockHeader)(MockBehavior.Strict)
                    headerMock.SetupGet(Function(h) h.Identifier).Returns(extension.Id)

                    installedExtensionMock.SetupGet(Function(m) m.Header).Returns(headerMock.Object)

                    Yield installedExtensionMock.Object
                Next
            Next
        End Function

        Public Interface IMockInstalledExtension
            ReadOnly Property Content As IEnumerable(Of MockContent)
            Function GetContentLocation(content As MockContent) As String
            ReadOnly Property Header As IMockHeader
        End Interface

        Public Interface IMockHeader
            ReadOnly Property Identifier As String
        End Interface

        Public Class MockContent
            Private ReadOnly _contentType As String
            Private ReadOnly _path As String

            Public Sub New(contentType As String, path As String)
                _contentType = contentType
                _path = path
            End Sub

            Public ReadOnly Property ContentTypeName As String
                Get
                    Return _contentType
                End Get
            End Property

            Public ReadOnly Property RelativePath As String
                Get
                    Return _path
                End Get
            End Property
        End Class
    End Class
End Namespace
