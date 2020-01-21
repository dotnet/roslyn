' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Test.Utilities

Public Class VBParser
    Private ReadOnly _options As VisualBasicParseOptions

    Public Sub New(Optional options As VisualBasicParseOptions = Nothing)
        _options = options
    End Sub

    Public Function Parse(code As String) As SyntaxTree
        Dim tree = VisualBasicSyntaxTree.ParseText(code, _options, "", Encoding.UTF8)
        Return tree
    End Function
End Class

'TODO: We need this only temporarily until 893565 is fixed.
Public Class VBKindProvider : Implements ISyntaxNodeKindProvider
    Public Function Kind(node As Object) As String Implements ISyntaxNodeKindProvider.Kind
        Return node.GetType().GetTypeInfo().GetDeclaredProperty("Kind").GetValue(node, Nothing).ToString()
    End Function
End Class
