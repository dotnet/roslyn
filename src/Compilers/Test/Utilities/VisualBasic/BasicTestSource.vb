' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Text

Public Structure BasicTestSource

    Public ReadOnly Property Value As Object

    Private Sub New(value As Object)
        Me.Value = value
    End Sub

    Public Function GetSyntaxTrees(
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional ByRef assemblyName As String = Nothing,
        Optional ByRef spans As IEnumerable(Of IEnumerable(Of TextSpan)) = Nothing) As SyntaxTree()

        If Value Is Nothing Then
            Debug.Assert(parseOptions Is Nothing)
            Return Array.Empty(Of SyntaxTree)
        End If

        Dim xmlSource = TryCast(Value, XElement)
        If xmlSource IsNot Nothing Then
            Return ParseSourceXml(xmlSource, parseOptions, assemblyName, spans).ToArray()
        End If

        Dim source = TryCast(Value, String)
        If source IsNot Nothing Then
            Return New SyntaxTree() {VisualBasicSyntaxTree.ParseText(source, parseOptions)}
        End If

        Dim sources = TryCast(Value, String())
        If sources IsNot Nothing Then
            Return sources.Select(Function(s) VisualBasicSyntaxTree.ParseText(s, parseOptions)).ToArray()
        End If

        Dim tree = TryCast(Value, SyntaxTree)
        If tree IsNot Nothing Then
            Debug.Assert(parseOptions Is Nothing)
            Return New SyntaxTree() {tree}
        End If

        Dim trees = TryCast(Value, SyntaxTree())
        If trees IsNot Nothing Then
            Debug.Assert(parseOptions Is Nothing)
            Return trees
        End If

        Throw New Exception($"Unexpected value: {Value}")
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="source">The sources compile according to the following schema        
    ''' &lt;compilation name="assemblyname[optional]"&gt;
    ''' &lt;file name="file1.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' &lt;/compilation&gt;
    ''' </param>
    Public Shared Widening Operator CType(source As XElement) As BasicTestSource
        Return New BasicTestSource(source)
    End Operator

    Public Shared Widening Operator CType(source As String) As BasicTestSource
        Return New BasicTestSource(source)
    End Operator

    Public Shared Widening Operator CType(source As String()) As BasicTestSource
        Return New BasicTestSource(source)
    End Operator

    Public Shared Widening Operator CType(source As SyntaxTree) As BasicTestSource
        Return New BasicTestSource(source)
    End Operator

    Public Shared Widening Operator CType(source As SyntaxTree()) As BasicTestSource
        Return New BasicTestSource(source)
    End Operator
End Structure
