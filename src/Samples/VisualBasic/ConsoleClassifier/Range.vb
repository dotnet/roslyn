' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Text

Class Range

    ReadOnly Property ClassifiedSpan As ClassifiedSpan

    ReadOnly Property Text As String

    Sub New(classification As String, span As TextSpan, text As SourceText)
        Me.New(classification, span, text.GetSubText(span).ToString())
    End Sub

    Sub New(classification As String, span As TextSpan, text As String)
        Me.New(New ClassifiedSpan(classification, span), text)
    End Sub

    Sub New(classifiedSpan As ClassifiedSpan, text As String)
        _ClassifiedSpan = classifiedSpan
        _Text = text
    End Sub

    ReadOnly Property ClassificationType As String
        Get
            Return ClassifiedSpan.ClassificationType
        End Get
    End Property

    ReadOnly Property TextSpan As TextSpan
        Get
            Return ClassifiedSpan.TextSpan
        End Get
    End Property
End Class