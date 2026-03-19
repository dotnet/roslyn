' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports <xmlns="http://schemas.microsoft.com/VisualStudio/Roslyn/Compiler">

Public Class XmlRenamer
    Private ReadOnly _xDoc As XDocument

    Public Sub New(xDoc As XDocument)
        _xDoc = xDoc
    End Sub

    Public Sub Rename(renamingFile As String)
        Dim namesToUpdate As List(Of NameToUpdate)

        namesToUpdate = ParseUpdateList(renamingFile)

        For Each name In (From n In namesToUpdate Where n.kind = UpdateKind.Enumerator)
            UpdateEnumerator(name)
        Next
        For Each name In (From n In namesToUpdate Where n.kind = UpdateKind.Child)
            UpdateChild(name)
        Next
        For Each name In (From n In namesToUpdate Where n.kind = UpdateKind.Field)
            UpdateField(name)
        Next
        For Each name In (From n In namesToUpdate Where n.kind = UpdateKind.Kind)
            UpdateNodeKind(name)
        Next
        For Each name In (From n In namesToUpdate Where n.kind = UpdateKind.Class)
            UpdateNodeClass(name)
        Next
        For Each name In (From n In namesToUpdate Where n.kind = UpdateKind.Enum)
            UpdateEnum(name)
        Next
    End Sub

    Private Function ParseUpdateList(renamingFile As String) As List(Of NameToUpdate)
        Dim namesToUpdate As New List(Of NameToUpdate)

        For Each line In File.ReadLines(renamingFile)
            Dim fields = line.Split({","c})
            If fields.Length >= 4 AndAlso fields(0).Trim <> "" AndAlso Cleanup(fields(3)) <> "" Then
                namesToUpdate.Add(New NameToUpdate With {.kind = DirectCast([Enum].Parse(GetType(UpdateKind), fields(0), True), UpdateKind),
                                                         .typeName = Cleanup(fields(1)),
                                                         .memberName = Cleanup(fields(2)),
                                                         .newName = Cleanup(fields(3))})
            End If
        Next

        Return namesToUpdate
    End Function

    Private Function Cleanup(s As String) As String
        s = s.Trim()
        If s.StartsWith("[", StringComparison.Ordinal) AndAlso s.EndsWith("]", StringComparison.Ordinal) Then
            s = s.Substring(1, s.Length - 2)
        End If
        If s.StartsWith("Optional", StringComparison.Ordinal) Then
            s = s.Substring("Optional".Length)
        End If

        Return s
    End Function

    Private Sub UpdateNodeKind(update As NameToUpdate)
        For Each node In (From n In _xDoc...<node-kind> Where n.@name = update.memberName)
            node.@name = update.newName
        Next

        UpdateKindString(update.memberName, update.newName)
    End Sub

    Private Sub UpdateNodeClass(update As NameToUpdate)
        For Each node In (From n In _xDoc...<node-structure> Where n.@name = update.typeName)
            node.@name = update.newName
        Next

        For Each node In (From n In _xDoc...<node-structure> Where n.@parent = update.typeName)
            node.@parent = update.newName
        Next

        Dim oldKindName = "@" + update.typeName
        Dim newKindName = "@" + update.newName

        UpdateKindString(oldKindName, newKindName)
    End Sub

    Private Sub UpdateEnum(update As NameToUpdate)
        For Each node In (From n In _xDoc...<enumeration> Where n.@name = update.typeName)
            node.@name = update.newName
        Next
    End Sub

    Private Sub UpdateEnumerator(update As NameToUpdate)
        For Each enumNode In (From n In _xDoc...<enumeration> Where n.@name = update.typeName)
            For Each node In (From n In enumNode.<enumerators>.<enumerator> Where n.@name = update.memberName)
                node.@name = update.newName
            Next
        Next
    End Sub

    Private Sub UpdateChild(update As NameToUpdate)
        For Each structNode In (From n In _xDoc...<node-structure> Where n.@name = update.typeName)
            For Each node In (From n In structNode.<child> Where n.@name = update.memberName)
                node.@name = update.newName
            Next
        Next
    End Sub

    Private Sub UpdateField(update As NameToUpdate)
        For Each structNode In (From n In _xDoc...<node-structure> Where n.@name = update.typeName)
            For Each node In (From n In structNode.<field> Where n.@name = update.memberName)
                node.@name = update.newName
            Next
        Next
    End Sub

    Private Function IndexOfNodeKind(attrValue As String, kind As String) As Integer
        If String.IsNullOrEmpty(attrValue) Then
            Return -1
        End If

        Dim index As Integer = attrValue.IndexOf(kind, StringComparison.Ordinal)

        If (index > 0 AndAlso attrValue(index - 1) <> "|"c) Then
            Return -1    ' must be preceded by vert bar or nothing.
        End If

        Dim endIndex = index + kind.Length
        If (endIndex < attrValue.Length AndAlso attrValue(endIndex) <> "|"c) Then
            Return -1    ' must be followed by vert bar or nothing.
        End If

        Return index
    End Function

    Private Function ContainsNodeKind(attrValue As String, kind As String) As Boolean
        Return IndexOfNodeKind(attrValue, kind) >= 0
    End Function

    Private Sub UpdateKindString(oldKind As String, newKind As String)
        For Each node In (From n In _xDoc...<child> Where ContainsNodeKind(n.@kind, oldKind))
            UpdateKindAttribute(node.Attribute("kind"), oldKind, newKind)
        Next

        For Each node In (From n In _xDoc...<child> Where ContainsNodeKind(n.@<separator-kind>, oldKind))
            UpdateKindAttribute(node.Attribute("separator-kind"), oldKind, newKind)
        Next

        For Each node In (From n In _xDoc...<node-kind-alias> Where ContainsNodeKind(n.@alias, oldKind))
            UpdateKindAttribute(node.Attribute("alias"), oldKind, newKind)
        Next
    End Sub

    Private Sub UpdateKindAttribute(attr As XAttribute, oldKind As String, newKind As String)
        Dim attrValue = attr.Value
        Dim startIndex = IndexOfNodeKind(attrValue, oldKind)
        Dim newValue = attrValue.Substring(0, startIndex) + newKind + attrValue.Substring(startIndex + oldKind.Length)
        attr.Value = newValue
    End Sub
End Class

Friend Class NameToUpdate
    Public kind As updateKind
    Public typeName As String
    Public memberName As String
    Public newName As String
End Class

Friend Enum UpdateKind
    [Enum]
    Enumerator
    [Class]
    Kind
    Field
    Child
End Enum
