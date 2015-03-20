' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Text

Friend Class Range

    Public ReadOnly Property ClassifiedSpan As ClassifiedSpan

    Public ReadOnly Property Text As String

    Public Sub New(classification As String, span As TextSpan, text As SourceText)
        Me.New(classification, span, text.GetSubText(span).ToString())
    End Sub

    Public Sub New(classification As String, span As TextSpan, text As String)
        Me.New(New ClassifiedSpan(classification, span), text)
    End Sub

    Public Sub New(classifiedSpan As ClassifiedSpan, text As String)
        _ClassifiedSpan = classifiedSpan
        _Text = text
    End Sub

    Public ReadOnly Property ClassificationType As String
        Get
            Return ClassifiedSpan.ClassificationType
        End Get
    End Property

    Public ReadOnly Property TextSpan As TextSpan
        Get
            Return ClassifiedSpan.TextSpan
        End Get
    End Property
End Class
