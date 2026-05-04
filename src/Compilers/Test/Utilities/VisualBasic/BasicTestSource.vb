' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports Microsoft.CodeAnalysis.Text

Public Structure BasicTestSource

    Public ReadOnly Property Value As Object

    Private Sub New(value As Object)
        Me.Value = value
    End Sub

    Public Shared Function Parse(text As String,
                                 Optional path As String = "",
                                 Optional options As VisualBasicParseOptions = Nothing,
                                 Optional encoding As Encoding = Nothing,
                                 Optional checksumAlgorithm As SourceHashAlgorithm = SourceHashAlgorithms.Default) As SyntaxTree

        Dim sourceTest = SourceText.From(text, If(encoding, Encoding.UTF8), checksumAlgorithm)
        Dim tree = SyntaxFactory.ParseSyntaxTree(sourceTest, If(options, TestOptions.RegularLatest), path)
        Return tree
    End Function

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
            Return New SyntaxTree() _
            {
                VisualBasicSyntaxTree.ParseText(
                    SourceText.From(source, encoding:=Nothing, SourceHashAlgorithms.Default),
                    If(parseOptions, TestOptions.RegularLatest))
            }
        End If

        Dim sources = TryCast(Value, String())
        If sources IsNot Nothing Then
            Return sources.Select(Function(s) VisualBasicSyntaxTree.ParseText(SourceText.From(s, encoding:=Nothing, SourceHashAlgorithms.Default), If(parseOptions, TestOptions.RegularLatest))).ToArray()
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
