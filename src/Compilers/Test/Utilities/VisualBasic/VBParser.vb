' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text

Public Class VBParser
    Private ReadOnly _options As VisualBasicParseOptions

    Public Sub New(Optional options As VisualBasicParseOptions = Nothing)
        _options = options
    End Sub

    Public Function Parse(code As String) As SyntaxTree
        Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.From(code, Encoding.UTF8, SourceHashAlgorithms.Default), _options, path:="")
        Return tree
    End Function
End Class

'TODO: We need this only temporarily until 893565 is fixed.
Public Class VBKindProvider : Implements ISyntaxNodeKindProvider
    Public Function Kind(node As Object) As String Implements ISyntaxNodeKindProvider.Kind
        Return node.GetType().GetTypeInfo().GetDeclaredProperty("Kind").GetValue(node, Nothing).ToString()
    End Function
End Class
