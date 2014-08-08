' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

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