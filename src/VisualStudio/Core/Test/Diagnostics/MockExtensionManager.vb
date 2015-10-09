' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Globalization
Imports System.Xml
Imports Microsoft.CodeAnalysis
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Friend Class MockExtensionManager

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

        Public Iterator Function GetEnabledExtensions(contentTypeName As String) As IEnumerable(Of MockInstalledExtension)
            Assert.Equal(_contentType, contentTypeName)

            For Each location In _locations
                Yield New MockInstalledExtension(_contentType, location)
            Next
        End Function

        Friend Class MockInstalledExtension

            Private ReadOnly _contentType As String
            Private ReadOnly _location As String

            Public Sub New(contentType As String, location As String)
                _contentType = contentType
                _location = location
            End Sub

            Public ReadOnly Property Content As IEnumerable(Of MockContent)
                Get
                    Return SpecializedCollections.SingletonEnumerable(Of MockContent)(New MockContent(_contentType, _location))
                End Get
            End Property

            Public ReadOnly Property Header As MockHeader
                Get
                    Return New MockHeader("Vsix")
                End Get
            End Property

            Public ReadOnly Property InstallPath As String
                Get
                    Return "\InstallPath"
                End Get
            End Property

            Friend Class MockContent

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

            Friend Class MockHeader

                Private ReadOnly _name As String

                Public Sub New(name As String)
                    _name = name
                End Sub

                Public ReadOnly Property LocalizedName As String
                    Get
                        Return _name
                    End Get
                End Property

            End Class

        End Class

    End Class
End Namespace
