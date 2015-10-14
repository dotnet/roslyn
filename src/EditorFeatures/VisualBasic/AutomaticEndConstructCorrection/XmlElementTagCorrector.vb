' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    Friend Class XmlElementTagCorrector
        Inherits AbstractCorrector

        Public Sub New(subjectBuffer As ITextBuffer, _waitIndicator As IWaitIndicator)
            MyBase.New(subjectBuffer, _waitIndicator)
        End Sub

        Protected Overrides Function IsAllowableTextUnderPosition(textUnderPosition As String) As Boolean
            Return False
        End Function

        Protected Overrides Function TryGetValidToken(e As TextContentChangedEventArgs, ByRef token As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Return False
        End Function

        Protected Overrides Function GetLinkedEditSpans(snapshot As ITextSnapshot, token As SyntaxToken) As IEnumerable(Of ITrackingSpan)
            Return SpecializedCollections.EmptyEnumerable(Of ITrackingSpan)
        End Function
    End Class
End Namespace
