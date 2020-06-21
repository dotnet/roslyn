' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------------------------------------
' This is the code that actually outputs the VB code that defines the tree. It is passed a read and validated
' ParseTree, and outputs the code to defined the node classes for that tree, and also additional data
' structures like the kinds, visitor, etc.
'-----------------------------------------------------------------------------------------------------------

Imports System.IO

' Class to write out the names in the tree as a CSV file for Excel.

Friend Class WriteCsvNames
    Inherits WriteUtils

    Private _writer As TextWriter    'output is sent here.

    ' Initialize the class with the parse tree to write.
    Public Sub New(parseTree As ParseTree)
        MyBase.New(parseTree)
    End Sub

    ' Write out the CSV with the names.
    Public Sub WriteCsv(filename As String)
        _writer = New StreamWriter(New FileStream(filename, FileMode.Create, FileAccess.Write))

        Using _writer
            WriteEnums()
            WriteNodeStructures()
            WriteFactories()
        End Using
    End Sub

    Private Sub WriteCsvLine(val1 As String, val2 As String, val3 As String)
        _writer.WriteLine("{0},{1},{2}", val1, val2, val3)
    End Sub

    Private Sub WriteBlankLine()
        WriteCsvLine("", "", "")
    End Sub

    Private Sub WriteEnums()
        For Each enumerationType In _parseTree.Enumerations.Values
            WriteEnum(enumerationType)
        Next
    End Sub

    Private Sub WriteNodeStructures()
        For Each nodeStructure In _parseTree.NodeStructures.Values
            WriteNodeStructure(nodeStructure)
        Next
    End Sub

    ' Generate an enumeration type
    Private Sub WriteEnum(enumeration As ParseEnumeration)
        WriteCsvLine("enum", EnumerationTypeName(enumeration), "")

        For Each enumerator In enumeration.Enumerators
            WriteEnumeratorVariable(enumerator, enumeration)
        Next

        WriteBlankLine()
    End Sub

    Private Sub WriteEnumeratorVariable(enumerator As ParseEnumerator, enumeration As ParseEnumeration)
        WriteCsvLine(NameOf(enumerator), EnumerationTypeName(enumeration), enumerator.Name)
    End Sub

    Private Sub WriteNodeStructure(nodeStructure As ParseNodeStructure)
        WriteCsvLine("class", StructureTypeName(nodeStructure), "")

        For Each kind In nodeStructure.NodeKinds
            WriteKind(kind)
        Next

        Dim allFields = GetAllFieldsOfStructure(nodeStructure)
        For i = 0 To allFields.Count - 1
            WriteField(allFields(i))
        Next

        Dim allChildren = GetAllChildrenOfStructure(nodeStructure)
        For i = 0 To allChildren.Count - 1
            WriteChild(allChildren(i))
        Next

        WriteBlankLine()
    End Sub

    Private Sub WriteKind(kind As ParseNodeKind)
        WriteCsvLine(NameOf(kind), StructureTypeName(kind.NodeStructure), Ident(kind.Name))
    End Sub

    Private Sub WriteField(field As ParseNodeField)
        WriteCsvLine(NameOf(field), StructureTypeName(field.ContainingStructure), FieldPropertyName(field))
    End Sub

    Private Sub WriteChild(child As ParseNodeChild)
        WriteCsvLine(NameOf(child), StructureTypeName(child.ContainingStructure), ChildPropertyName(child))
    End Sub

    Private Sub WriteFactories()
        WriteCsvLine("class", Ident(_parseTree.FactoryClassName), "")
        For Each nodeStructure In _parseTree.NodeStructures.Values
            GenerateFactoryMethodsForStructure(nodeStructure)
        Next
    End Sub

    Private Sub GenerateFactoryMethodsForStructure(nodeStructure As ParseNodeStructure)
        If _parseTree.IsAbstract(nodeStructure) Then Return ' abstract structures don't have factory methods

        For Each nodeKind In nodeStructure.NodeKinds
            GenerateFactoryMethods(nodeStructure, nodeKind)
        Next

        ' Only generate one a structure-level factory method if >= 2 kinds, and the nodeStructure name doesn't conflict with a kind name.
        If nodeStructure.NodeKinds.Count >= 2 And Not _parseTree.NodeKinds.ContainsKey(FactoryName(nodeStructure)) Then
            GenerateFactoryMethods(nodeStructure, Nothing)
        End If
    End Sub

    Private Sub GenerateFactoryMethods(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind)
        GenerateFactoryMethod(nodeStructure, nodeKind, False)
        If HasSimplifiedFactory(nodeStructure, nodeKind) Then
            GenerateFactoryMethod(nodeStructure, nodeKind, True)
        End If
    End Sub

    ' Should we generator a simplified factory method also?
    Private Function HasSimplifiedFactory(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind) As Boolean
        ' Currently, all tokens have simplified factories.
        Return nodeStructure.IsToken
    End Function

    ' Generate the factory method for a node structure, possibly customized to a particular kind.
    ' If kind is Nothing, generate a factory method that takes a Kind parameter, and can create any kind.
    ' If kind is not Nothing, generator a factory method customized to that particular kind.
    ' The simplified form is:
    '   Defaults the text for any token with token-text defined
    '   Defaults the trivia to a single trailing space for any token
    Private Sub GenerateFactoryMethod(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind, simplifiedForm As Boolean)
        Dim factoryFunctionName As String       ' name of the factory method.

        If nodeKind IsNot Nothing Then
            If nodeKind.NoFactory Then Return

            factoryFunctionName = FactoryName(nodeKind)
        Else
            If nodeStructure.NoFactory Then Return

            factoryFunctionName = FactoryName(nodeStructure)
        End If

        WriteCsvLine("factory", Ident(_parseTree.FactoryClassName), Ident(factoryFunctionName))

    End Sub

End Class
