' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Reflection

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Class EmbeddedResources

        Private Shared _embedded As String
        Public Shared ReadOnly Property Embedded As String
            Get
                If _embedded Is Nothing Then
                    _embedded = GetManifestResourceString("Embedded.vb")
                End If

                Return _embedded
            End Get
        End Property

        Private Shared _internalXmlHelper As String
        Public Shared ReadOnly Property InternalXmlHelper As String
            Get
                If _internalXmlHelper Is Nothing Then
                    _internalXmlHelper = GetManifestResourceString("InternalXmlHelper.vb")
                End If

                Return _internalXmlHelper
            End Get
        End Property

        Private Shared _vbCoreSourceText As String
        Public Shared ReadOnly Property VbCoreSourceText As String
            Get
                If _vbCoreSourceText Is Nothing Then
                    _vbCoreSourceText = GetManifestResourceString("VbCoreSourceText.vb")
                End If

                Return _vbCoreSourceText
            End Get
        End Property

        Private Shared _vbMyTemplateText As String
        Public Shared ReadOnly Property VbMyTemplateText As String
            Get
                If _vbMyTemplateText Is Nothing Then
                    _vbMyTemplateText = GetManifestResourceString("VbMyTemplateText.vb")
                End If

                Return _vbMyTemplateText
            End Get
        End Property

        Private Shared Function GetManifestResourceString(name As String) As String
            Using reader As New StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                Return reader.ReadToEnd()
            End Using
        End Function

    End Class

End Namespace
