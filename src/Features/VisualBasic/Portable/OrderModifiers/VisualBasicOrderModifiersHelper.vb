' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.OrderModifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.OrderModifiers
    Friend Class VisualBasicOrderModifiersHelper
        Inherits AbstractOrderModifiersHelpers

        Public Shared ReadOnly Instance As New VisualBasicOrderModifiersHelper()

        Private Sub New()
        End Sub

        Protected Overrides Function GetKeywordKind(trimmed As String) As Integer
            Dim kind = SyntaxFacts.GetKeywordKind(trimmed)
            Return If(kind = SyntaxKind.None, SyntaxFacts.GetContextualKeywordKind(trimmed), kind)
        End Function

        'Protected Overrides Function TryParse(String value, out Dictionary<int, int> parsed) As Boolean
        '{
        '    If (!base.TryParse(value, out parsed))
        '    {
        '        Return False;
        '    }

        '    // 'partial' must always go at the end in C#.
        '    parsed[(int)SyntaxKind.PartialKeyword] = int.MaxValue;
        '    Return True;
        '}
    End Class
End Namespace
