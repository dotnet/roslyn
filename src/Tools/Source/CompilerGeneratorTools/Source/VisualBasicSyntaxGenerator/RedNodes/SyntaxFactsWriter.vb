' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------------------------------------
' This is the code that actually outputs the VB code that defines the tree. It is passed a read and validated
' ParseTree, and outputs the code to defined the node classes for that tree, and also additional data
' structures like the kinds, visitor, etc.
'-----------------------------------------------------------------------------------------------------------

Imports System.IO

' Class to write out the code for the code tree.
Public Class SyntaxFactsWriter
    Inherits WriteUtils

    Private _writer As TextWriter    'output is sent here.

    ' Initialize the class with the parse tree to write.
    Public Sub New(parseTree As ParseTree)
        MyBase.New(parseTree)
    End Sub

    ' Write out the factory class to the given file.
    Public Sub GenerateFile(writer As TextWriter)

        _writer = writer
        With _writer
            .WriteLine($"

Namespace {Ident(_parseTree.NamespaceName)}

    Partial Public Class SyntaxFacts")
            GenerateAllFactoryMethods()
            _writer.WriteLine("    End Class

     Public Module GeneratedExtensionSyntaxFacts")
            GenerateAllExtensionFactoryMethods()
            _writer.WriteLine("    End Module

End Namespace")
        End With
    End Sub

    Public Sub GenerateGetText(writer As TextWriter)
        _writer = writer
        _writer.WriteLine("
Namespace {Ident(_parseTree.NamespaceName)}
    Partial Public Class SyntaxFacts")
        GenerateGetText()
        _writer.WriteLine("    End Class
End Namespace")
    End Sub

    ' Generate all factory methods for all node structures.
    Private Sub GenerateAllFactoryMethods()
        For Each nodeStructure In _parseTree.NodeStructures.Values
            GenerateFactoryMethodsForStructure(nodeStructure)
        Next

        GenerateGetText()
    End Sub

    Private Sub GenerateAllExtensionFactoryMethods()
        GenerateExtensionGetText()
    End Sub

    Private Sub GenerateGetText()

        _writer.WriteLine(
"        ''' <summary>
        ''' Return keyword or punctuation text based on SyntaxKind
        ''' </summary>
        Public Shared Function GetText(kind As SyntaxKind) As String
            Select Case kind")

        For Each nodeStructure In _parseTree.NodeStructures.Values
            For Each kind In nodeStructure.NodeKinds
                Dim tokenText = kind.TokenText

                If tokenText IsNot Nothing AndAlso tokenText.Contains("""") Then
                    tokenText = tokenText.Replace("""", """""")
                End If

                If tokenText <> Nothing Then
                    _writer.WriteLine($"        Case SyntaxKind.{kind.Name}")

                    If tokenText.Contains("vbCrLf") Then
                        _writer.WriteLine($"            Return {tokenText}")
                    Else
                        _writer.WriteLine($"            Return ""{tokenText}""")
                    End If
                End If
            Next
        Next

        _writer.WriteLine(
 "            Case Else
                  Return String.Empty
             End Select
         End Function")

    End Sub

    Private Sub GenerateExtensionGetText()

        _writer.WriteLine(
"        ''' <summary>
        ''' Return keyword or punctuation text based on SyntaxKind
        ''' </summary>
        < Extension() >
        Public Function GetText(kind As SyntaxKind) As String
            Return SyntaxFacts.GetText(kind)
        End Function")

    End Sub

    ' Generate all factory methods for a node structure.
    ' If a nodeStructure has 0 kinds, it is abstract and no factory method is generator
    ' If a nodeStructure has 1 kind, a factory method for that kind is generator
    ' If a nodestructure has >=2 kinds, a factory method for each kind is generated, plus one for the structure as a whole, unless name would conflict.
    Private Sub GenerateFactoryMethodsForStructure(nodeStructure As ParseNodeStructure)
        If _parseTree.IsAbstract(nodeStructure) Then Return ' abstract structures don't have factory methods

        GenerateSyntaxFacts(nodeStructure)

    End Sub

    ' Generate the factory method for a node structure, possibly customized to a particular kind.
    ' If kind is Nothing, generate a factory method that takes a Kind parameter, and can create any kind.
    ' If kind is not Nothing, generator a factory method customized to that particular kind.
    Private Sub GenerateSyntaxFacts(nodeStructure As ParseNodeStructure)
        'GenerateSyntaxFact(nodeStructure.Name, nodeStructure.NodeKinds)
        GenerateSyntaxFact(SyntaxFactName(nodeStructure), nodeStructure.NodeKinds, Not nodeStructure.InternalSyntaxFacts)

        For Each child In GetAllChildrenOfStructure(nodeStructure)
            Dim kinds = child.ChildKind(nodeStructure.NodeKinds)
            Dim childKindStruct = KindTypeStructure(child.ChildKind)
            If kinds IsNot Nothing AndAlso childKindStruct.IsToken AndAlso Not child.IsList Then
                GenerateSyntaxFact(FactoryName(nodeStructure) + child.Name, kinds, Not child.InternalSyntaxFacts)
            End If
        Next
    End Sub

    ' Generate the factory method for a node structure, possibly customized to a particular kind.
    ' If kind is Nothing, generate a factory method that takes a Kind parameter, and can create any kind.
    ' If kind is not Nothing, generator a factory method customized to that particular kind.
    ' The simplified form is:
    '   Defaults the text for any token with token-text defined
    '   Defaults the trivia to a single trailing space for any token
    Private Sub GenerateSyntaxFact(name As String, nodeKinds As IList(Of ParseNodeKind), Optional publicAccessibility As Boolean = True)

        'Dim factoryFunctionName As String       ' name of the factory method.
        Dim needComma = False
        If nodeKinds.Count >= 2 Then

            _writer.WriteLine($"        {If(publicAccessibility, "Public", "Friend")} Shared Function Is{name}(kind As SyntaxKind) As Boolean")
            _writer.WriteLine("            Select Case kind")

            _writer.WriteLine("                Case _")
            For Each kind In nodeKinds
                If needComma Then
                    _writer.WriteLine(",")
                End If
                _writer.Write($"                SyntaxKind.{kind.Name}")
                needComma = True
            Next
            _writer.WriteLine(
"
                Return True
            End Select
            Return False
        End Function
")
        End If

    End Sub

End Class
