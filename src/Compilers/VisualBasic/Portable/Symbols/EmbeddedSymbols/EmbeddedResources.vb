' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Reflection

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Class EmbeddedResources

        Private Shared s_embedded As String
        Public Shared ReadOnly Property Embedded As String
            Get
                If s_embedded Is Nothing Then
                    s_embedded = GetManifestResourceString("Embedded.vb")
                End If

                Return s_embedded
            End Get
        End Property

        Private Shared s_internalXmlHelper As String
        Public Shared ReadOnly Property InternalXmlHelper As String
            Get
                If s_internalXmlHelper Is Nothing Then
                    s_internalXmlHelper = GetManifestResourceString("InternalXmlHelper.vb")
                End If

                Return s_internalXmlHelper
            End Get
        End Property

        Private Shared s_vbCoreSourceText As String
        Public Shared ReadOnly Property VbCoreSourceText As String
            Get
                If s_vbCoreSourceText Is Nothing Then
                    s_vbCoreSourceText = GetManifestResourceString("VbCoreSourceText.vb")
                End If

                Return s_vbCoreSourceText
            End Get
        End Property

        Private Shared s_vbMyTemplateText As String
        Public Shared ReadOnly Property VbMyTemplateText As String
            Get
                If s_vbMyTemplateText Is Nothing Then
                    s_vbMyTemplateText = GetManifestResourceString("VbMyTemplateText.vb")
                End If

                Return s_vbMyTemplateText
            End Get
        End Property

        Private Shared Function GetManifestResourceString(name As String) As String
            Using reader As New StreamReader(GetType(EmbeddedResources).GetTypeInfo().Assembly.GetManifestResourceStream(name))
                Return reader.ReadToEnd()
            End Using
        End Function

    End Class

End Namespace
