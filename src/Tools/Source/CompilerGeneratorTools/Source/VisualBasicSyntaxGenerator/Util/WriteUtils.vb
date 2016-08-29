' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------------------------------------
' Defines a base class that is inherited by the writing classes that defines a bunch of utility
' functions for building up names of constructs. Basically anything that we want to shared between different
' output classes is defined here.
'-----------------------------------------------------------------------------------------------------------

Imports System.IO
Imports System.Text

' Utility functions for writing out the parse tree. Basically name generation and various simply utility functions
' that are shared by different writing classes. This is typically inherited by a writing class.
Public MustInherit Class WriteUtils
    Protected _parseTree As ParseTree  ' the tree to output

#If OLDSTYLE Then
    Protected Const NodeKindString = "NodeKind"
    Protected Const NodeListString = "NodeList"
    Protected Const SeparatedNodeListString = "SeparatedNodeList"
    Protected Const NodeFactoryListString = "NodeFactory"
#Else
    Protected Const NodeKindString = "SyntaxKind"
    Protected Const NodeListString = "SyntaxList"
    Protected Const SeparatedNodeListString = "SeparatedSyntaxList"
    Protected Const NodeFactoryListString = "Syntax"
#End If
    Public Sub New(parseTree As ParseTree)
        _parseTree = parseTree
    End Sub

    ' Name generation

    ' Get the base type name (no namespace) for a node structure.
    Protected Function StructureTypeName(nodeStructure As ParseNodeStructure) As String
        Return Ident(nodeStructure.Name)
    End Function

    ' Get the base type name (no namespace) for an enumeration.
    Protected Function EnumerationName(enumeration As ParseEnumeration) As String
        Return Ident(enumeration.Name)
    End Function

    'Get a factory name from a kind
    Protected Function FactoryName(nodeKind As ParseNodeKind) As String
        Return nodeKind.Name
    End Function

    'Get a factory name from a structure
    Protected Function FactoryName(nodeStructure As ParseNodeStructure) As String
#If OLDSTYLE Then
        Return nodeStructure.Name
#Else
        If nodeStructure.Name.EndsWith("Syntax", StringComparison.Ordinal) Then
            Return nodeStructure.Name.Substring(0, nodeStructure.Name.Length - 6)
        Else
            Return nodeStructure.Name
        End If
#End If
    End Function

    Protected Function SyntaxFactName(nodeStructure As ParseNodeStructure) As String
        Dim name = FactoryName(nodeStructure)
        If name = "Keyword" Then
            name = "KeywordKind"
        End If
        Return name
    End Function

    ' Get the name for a field private variable
    Protected Function FieldVarName(nodeField As ParseNodeField) As String
        Dim name As String = nodeField.Name
        Return "_" + LowerFirstCharacter(name)
    End Function

    ' Get the name for a child private variable
    Protected Function ChildVarName(nodeChild As ParseNodeChild) As String
        Dim name As String = nodeChild.Name
        Return "_" + LowerFirstCharacter(name)
    End Function

    ' Get the name for a child private variable cache (not in the builder, but in the actual node)
    Protected Function ChildCacheVarName(nodeChild As ParseNodeChild) As String
        Dim name As String = nodeChild.Name
        Return "_" + LowerFirstCharacter(name) + "Cache"
    End Function

    ' Get the name for a new child variable is the visitor
    Protected Function ChildNewVarName(nodeChild As ParseNodeChild) As String
        Dim name As String = nodeChild.Name
        Return Ident("new" + UpperFirstCharacter(name))
    End Function

#If False Then
    ' Get the name for a new child variable is the visitor, but for the separators
    Protected Function ChildNewVarSeparatorsName(ByVal nodeChild As ParseNodeChild) As String
        Dim name As String = ChildSeparatorsName(nodeChild)
        Return Ident(LowerFirstCharacter(name) + "New")
    End Function
#End If

    ' Get the name for a field parameter.
    ' If conflictName is not nothing, make sure the name doesn't conflict with that name.
    Protected Function FieldParamName(nodeField As ParseNodeField, Optional conflictName As String = Nothing) As String
        Dim name As String = nodeField.Name
        If String.Equals(name, conflictName, StringComparison.OrdinalIgnoreCase) Then
            name += "Parameter"
        End If
        Return Ident(LowerFirstCharacter(name))
    End Function

    ' Get the name for a child parameter
    ' If conflictName is not nothing, make sure the name doesn't conflict with that name.
    Protected Function ChildParamName(nodeChild As ParseNodeChild, Optional conflictName As String = Nothing) As String
        Dim name As String = OptionalChildName(nodeChild)
        If String.Equals(name, conflictName, StringComparison.OrdinalIgnoreCase) Then
            name += "Parameter"
        End If
        Return Ident(LowerFirstCharacter(name))
    End Function

#If False Then
    ' Get the name for a child separator list parameter
    Protected Function ChildSeparatorsParamName(ByVal nodeChild As ParseNodeChild) As String
        Return Ident(LowerFirstCharacter(ChildSeparatorsName(nodeChild)))
    End Function
#End If

    ' Get the name for a field property
    Protected Function FieldPropertyName(nodeField As ParseNodeField) As String
        Return Ident(UnescapedFieldPropertyName(nodeField))
    End Function

    Protected Function UnescapedFieldPropertyName(nodeField As ParseNodeField) As String
        Dim name As String = nodeField.Name
        Return UpperFirstCharacter(name)
    End Function

    ' Get the name for a child property
    Protected Function ChildWithFunctionName(nodeChild As ParseNodeChild) As String
        Return Ident("With" + UpperFirstCharacter(nodeChild.Name))
    End Function

    ' Get the name for a child property
    Protected Function ChildPropertyName(nodeChild As ParseNodeChild) As String
        Return Ident(UnescapedChildPropertyName(nodeChild))
    End Function

    Protected Function UnescapedChildPropertyName(nodeChild As ParseNodeChild) As String
        Return UpperFirstCharacter(OptionalChildName(nodeChild))
    End Function

    ' Get the name for a child separators property
    Protected Function ChildSeparatorsPropertyName(nodeChild As ParseNodeChild) As String
        Return Ident(UpperFirstCharacter(ChildSeparatorsName(nodeChild)))
    End Function

    ' If a child is optional and isn't a list, add "Optional" to the name.
    Protected Function OptionalChildName(nodeChild As ParseNodeChild) As String
        Dim name As String = nodeChild.Name
        If nodeChild.IsOptional AndAlso Not nodeChild.IsList Then
#If OLDSTYLE Then
            Return "Optional" + UpperFirstCharacter(name)
#Else
            Return UpperFirstCharacter(name)
#End If
        Else
            Return UpperFirstCharacter(name)
        End If
    End Function

    ' If a child is a separated list, return the name of the separators.
    Protected Function ChildSeparatorsName(nodeChild As ParseNodeChild) As String
        If nodeChild.IsList AndAlso nodeChild.IsSeparated Then
            If String.IsNullOrEmpty(nodeChild.SeparatorsName) Then
                _parseTree.ReportError(nodeChild.Element, "separator-name was not found, but is required for separated lists")
            End If
            Return UpperFirstCharacter(nodeChild.SeparatorsName)
        Else
            Throw New InvalidOperationException("Shouldn't get here")
        End If
    End Function

    ' Get the type reference for a field property
    Protected Function FieldTypeRef(nodeField As ParseNodeField) As String
        Dim fieldType As Object = nodeField.FieldType
        If TypeOf fieldType Is SimpleType Then
            Return SimpleTypeName(CType(fieldType, SimpleType))
        ElseIf TypeOf fieldType Is ParseEnumeration Then
            Return EnumerationTypeName(CType(fieldType, ParseEnumeration))
        End If

        nodeField.ParseTree.ReportError(nodeField.Element, "Bad type for field")
        Return "UNKNOWNTYPE"
    End Function

    ' Get the type reference for a child property
    Protected Function ChildPropertyTypeRef(nodeStructure As ParseNodeStructure, nodeChild As ParseNodeChild, Optional isGreen As Boolean = False, Optional denyOverride As Boolean = False) As String
        Dim isOverride = nodeChild.ContainingStructure IsNot nodeStructure AndAlso Not denyOverride

        Dim result As String
        If nodeChild.IsList Then
            If nodeChild.IsSeparated Then
                result = String.Format(If(isGreen, "SeparatedSyntaxList(Of {0})", "SeparatedSyntaxList(Of {0})"), BaseTypeReference(nodeChild))
            ElseIf KindTypeStructure(nodeChild.ChildKind).IsToken Then
                If isGreen Then
                    result = String.Format("SyntaxList(Of {0})", BaseTypeReference(nodeChild))
                Else
                    result = "SyntaxTokenList"
                End If

            Else
                result = String.Format("SyntaxList(Of {0})", BaseTypeReference(nodeChild))
            End If
        Else
            If Not isGreen AndAlso KindTypeStructure(nodeChild.ChildKind).IsToken Then
                Return String.Format("SyntaxToken")
            End If

            If Not isGreen AndAlso isOverride Then
                Dim childKindStructure As ParseNodeStructure = Nothing

                If nodeStructure.NodeKinds.Count = 1 Then
                    Dim childNodeKind = GetChildNodeKind(nodeStructure.NodeKinds(0), nodeChild)
                    childKindStructure = KindTypeStructure(childNodeKind)
                Else
                    Dim childKinds As New HashSet(Of ParseNodeKind)()

                    For Each kind In nodeStructure.NodeKinds
                        Dim childNodeKind = GetChildNodeKind(kind, nodeChild)
                        If childNodeKind IsNot Nothing Then
                            childKinds.Add(GetChildNodeKind(kind, nodeChild))
                        Else
                            childKinds = Nothing
                            Exit For
                        End If
                    Next

                    If childKinds IsNot Nothing Then
                        If childKinds.Count = 1 Then
                            childKindStructure = KindTypeStructure(childKinds.First)
                        Else
                            childKindStructure = GetCommonStructure(childKinds.ToList())
                        End If
                    End If
                End If

                If childKindStructure IsNot Nothing Then
                    Return childKindStructure.Name
                End If
            End If

            result = BaseTypeReference(nodeChild)
        End If

        If isGreen Then
            If nodeChild.IsList Then
                Return String.Format("Global.Microsoft.CodeAnalysis.Syntax.InternalSyntax.{0}", result)
            Else
                Return String.Format("InternalSyntax.{0}", result)
            End If
        End If

        Return result
    End Function

    Protected Function ChildFieldTypeRef(nodeChild As ParseNodeChild, Optional isGreen As Boolean = False) As String
        If nodeChild.IsList Then
            If nodeChild.IsSeparated Then
                Return String.Format(If(isGreen, "GreenNode", "SeparatedSyntaxList(Of {0})"), BaseTypeReference(nodeChild))
            Else
                Return String.Format(If(isGreen, "GreenNode", "SyntaxList(Of {0})"), BaseTypeReference(nodeChild))
            End If
        Else
            Return BaseTypeReference(nodeChild)
        End If
    End Function

    Protected Function ChildPrivateFieldTypeRef(nodeChild As ParseNodeChild) As String
        If nodeChild.IsList Then
            Return "SyntaxNode"
        Else
            Return BaseTypeReference(nodeChild)
        End If
    End Function

    ' Get the type reference for a child separators property
    Protected Function ChildSeparatorsTypeRef(nodeChild As ParseNodeChild) As String
        Return String.Format("SyntaxList(Of {0})", SeparatorsBaseTypeReference(nodeChild))
    End Function

    ' Get the type reference for a child constructor
    Protected Function ChildConstructorTypeRef(nodeChild As ParseNodeChild, Optional isGreen As Boolean = False) As String
        If nodeChild.IsList Then
            If isGreen OrElse KindTypeStructure(nodeChild.ChildKind).IsToken Then
                Return "GreenNode"
            Else
                Return "SyntaxNode"
            End If
        Else
            Dim name = BaseTypeReference(nodeChild)
            If KindTypeStructure(nodeChild.ChildKind).IsToken Then
                Return "InternalSyntax." + name
            End If
            Return name
        End If
    End Function

    ' Get the type reference for a child constructor
    Protected Function ChildFactoryTypeRef(nodeStructure As ParseNodeStructure, nodeChild As ParseNodeChild, Optional isGreen As Boolean = False, Optional internalForm As Boolean = False) As String
        If nodeChild.IsList Then
            If nodeChild.IsSeparated Then
                If isGreen Then
                    Return String.Format("Global.Microsoft.CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(of GreenNode)", BaseTypeReference(nodeChild))
                Else
                    Return String.Format("SeparatedSyntaxList(Of {0})", BaseTypeReference(nodeChild))
                End If
            Else
                If isGreen Then
                    Return String.Format("Global.Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxList(of GreenNode)", StructureTypeName(_parseTree.RootStructure))
                Else
                    If KindTypeStructure(nodeChild.ChildKind).IsToken Then
                        Return String.Format("SyntaxTokenList")
                    Else
                        Return String.Format("SyntaxList(of {0})", BaseTypeReference(nodeChild))
                    End If
                End If

            End If
        Else
            If Not internalForm AndAlso KindTypeStructure(nodeChild.ChildKind).IsToken Then
                Return String.Format("SyntaxToken", BaseTypeReference(nodeChild))
            End If
            If Not isGreen Then
                Return ChildPropertyTypeRef(nodeStructure, nodeChild, isGreen)
            Else
                Return BaseTypeReference(nodeChild)
            End If
        End If
    End Function

#If False Then
    ' Get the type reference for a child separators constructor
    Protected Function ChildSeparatorConstructorTypeRef(ByVal nodeChild As ParseNodeChild) As String
        If nodeChild.IsList AndAlso nodeChild.IsSeparated Then
            Return String.Format("SeparatedNodeList(Of {0})", SeparatorsBaseTypeReference(nodeChild))
        Else
            Throw New ApplicationException("shouldn't get here")
        End If
    End Function
#End If

    ' Is this type reference a list structure kind?
    Protected Function IsListStructureType(nodeField As ParseNodeChild) As Boolean
        Return nodeField.IsList AndAlso (TypeOf nodeField.ChildKind Is ParseNodeKind OrElse TypeOf nodeField.ChildKind Is List(Of ParseNodeKind))
    End Function

    ' Is this type reference a non-list structure kind?
    Protected Function IsNodeStructureType(nodeField As ParseNodeChild) As Boolean
        Return Not nodeField.IsList AndAlso (TypeOf nodeField.ChildKind Is ParseNodeKind OrElse TypeOf nodeField.ChildKind Is List(Of ParseNodeKind))
    End Function

    ' Get the type reference for a child private variable, ignoring lists
    Protected Function BaseTypeReference(nodeChild As ParseNodeChild) As String
        Return KindTypeReference(nodeChild.ChildKind, nodeChild.Element)
    End Function

    ' Get the type reference for separators, ignoring lists
    Protected Function SeparatorsBaseTypeReference(nodeChild As ParseNodeChild) As String
        Return KindTypeReference(nodeChild.SeparatorsKind, nodeChild.Element)
    End Function

    ' Get the type reference for a kind
    Private Function KindTypeReference(kind As Object, element As XNode) As String
        If TypeOf kind Is ParseNodeKind Then
            Return StructureTypeName(CType(kind, ParseNodeKind).NodeStructure)
        ElseIf TypeOf kind Is List(Of ParseNodeKind) Then
            Dim commonStructure = GetCommonStructure(CType(kind, List(Of ParseNodeKind)))
            If commonStructure Is Nothing Then
                Return "Object"
            Else
                Return StructureTypeName(commonStructure)
            End If
        End If

        _parseTree.ReportError(element, "Invalid kind specified")
        Return "UNKNOWNTYPE"
    End Function

    ' Get the type reference for a kind
    Protected Function KindTypeStructure(kind As Object) As ParseNodeStructure
        If TypeOf kind Is ParseNodeKind Then
            Return CType(kind, ParseNodeKind).NodeStructure
        ElseIf TypeOf kind Is List(Of ParseNodeKind) Then
            Return GetCommonStructure(CType(kind, List(Of ParseNodeKind)))
        End If

        Return Nothing
    End Function

    ' Get the type name of a simple type
    Protected Function SimpleTypeName(simpleType As SimpleType) As String

        Select Case simpleType
            Case SimpleType.Bool
                Return "Boolean"
            Case SimpleType.Text
                Return "String"
            Case SimpleType.Character
                Return "Char"
            Case SimpleType.Int32
                Return "Integer"
            Case SimpleType.UInt32
                Return "UInteger"
            Case SimpleType.Int64
                Return "Long"
            Case SimpleType.UInt64
                Return "ULong"
            Case SimpleType.Float32
                Return "Single"
            Case SimpleType.Float64
                Return "Double"
            Case SimpleType.Decimal
                Return "System.Decimal"
            Case SimpleType.DateTime
                Return "DateTime"
            Case SimpleType.TextSpan
                Return "TextSpan"
            Case SimpleType.NodeKind
                Return NodeKindString
            Case Else
                Throw New InvalidOperationException("Unexpected simple type")
        End Select

    End Function

    ' Get the type name of an enumeration type
    Protected Function EnumerationTypeName(enumType As ParseEnumeration) As String
        Return Ident(enumType.Name)
    End Function

    ' The name of the node kind enumeration
    Protected Function NodeKindType() As String
        Return NodeKindString
    End Function

    ' The name of the visitor method for a structure type
    Protected Function VisitorMethodName(nodeStructure As ParseNodeStructure) As String
        Dim nodeName = nodeStructure.Name
        If nodeName.EndsWith("Syntax", StringComparison.Ordinal) Then nodeName = nodeName.Substring(0, nodeName.Length - 6)

        Return "Visit" + nodeName
    End Function

    ' Is this structure the root?
    Protected Function IsRoot(nodeStructure As ParseNodeStructure) As Boolean
        Return String.IsNullOrEmpty(nodeStructure.ParentStructureId)
    End Function

    ' Given a list of ParseNodeKinds, find the common structure that encapsulates all
    ' of them, or else return Nothing if there is no common structure.
    Protected Function GetCommonStructure(kindList As List(Of ParseNodeKind)) As ParseNodeStructure
        Dim structList = kindList.Select(Function(kind) kind.NodeStructure).ToList() ' list of the structures.

        ' Any candidate ancestor is an ancestor (or same) of the first element
        Dim candidate As ParseNodeStructure = structList(0)

        Do
            If IsAncestorOfAll(candidate, structList) Then
                Return candidate
            End If

            candidate = candidate.ParentStructure
        Loop While (candidate IsNot Nothing)

        Return Nothing ' no ancestor
    End Function

    ' Is this structure an ancestorOrSame of all
    Protected Function IsAncestorOfAll(parent As ParseNodeStructure, children As List(Of ParseNodeStructure)) As Boolean
        Return children.TrueForAll(Function(child) _parseTree.IsAncestorOrSame(parent, child))
    End Function

    ' Get all of the fields of a structure, including inherited fields, in the right order.
    ' TODO: need way to get the ordering right.
    Protected Function GetAllFieldsOfStructure(struct As ParseNodeStructure) As List(Of ParseNodeField)
        Dim fullList As New List(Of ParseNodeField)

        ' For now, just put inherited stuff at the beginning, until we design a real ordering solution
        Do While struct IsNot Nothing
            fullList.InsertRange(0, struct.Fields)
            struct = struct.ParentStructure
        Loop

        Return fullList
    End Function

    ' Get all of the children of a structure, including inherited children, in the right order.
    ' The ordering is defined first by order attribute, then by declared order (base before derived)
    Protected Function GetAllChildrenOfStructure(struct As ParseNodeStructure) As List(Of ParseNodeChild)
        Dim fullList As New List(Of Tuple(Of ParseNodeChild, Integer))

        ' For now, just put inherited stuff at the beginning, until we design a real ordering solution
        Dim baseLevel = 0  ' 0 for this structure, 1 for base, 2 for grandbase, ...

        Do While struct IsNot Nothing
            For i = 0 To struct.Children.Count - 1
                ' Add each child with an integer giving default sort order.
                fullList.Add(New Tuple(Of ParseNodeChild, Integer)(struct.Children(i), i - baseLevel * 100))
            Next
            struct = struct.ParentStructure
            baseLevel += 1
        Loop

        ' Return a new list in order.
        Return (From n In fullList Order By n.Item1.Order, n.Item2 Select n.Item1).ToList()
    End Function

    ' Get all of the children of a structure, including inherited children, in the right order.
    ' that can be passed to the factory method.
    ' The ordering is defined first by order attribute, then by declared order (base before derived)
    Protected Function GetAllFactoryChildrenOfStructure(struct As ParseNodeStructure) As IEnumerable(Of ParseNodeChild)
        Return From child In GetAllChildrenOfStructure(struct) Where Not child.NotInFactory Select child
    End Function

    ' String utility functions

    ' Lowercase the first character o a string
    Protected Function LowerFirstCharacter(s As String) As String
        If s Is Nothing OrElse s.Length = 0 Then
            Return s
        Else
            Return Char.ToLowerInvariant(s(0)) + s.Substring(1)
        End If
    End Function

    ' Uppercase the first character o a string
    Protected Function UpperFirstCharacter(s As String) As String
        If s Is Nothing OrElse s.Length = 0 Then
            Return s
        Else
            Return Char.ToUpperInvariant(s(0)) + s.Substring(1)
        End If
    End Function

    ' Word wrap a string into lines
    Protected Function WordWrap(text As String) As List(Of String)
        Const LineLength As Integer = 80
        Dim lines As New List(Of String)

        ' Remove newlines, consecutive spaces.
        text = text.Replace(vbCr, " ")
        text = text.Replace(vbLf, " ")
        Do
            Dim newText = text.Replace("  ", " ")
            If newText = text Then Exit Do
            text = newText
        Loop
        text = text.Trim()

        While text.Length >= LineLength
            Dim split As Integer = text.Substring(0, LineLength).LastIndexOf(" "c)
            If split < 0 Then split = LineLength

            Dim line As String = text.Substring(0, split).Trim()
            lines.Add(line)

            text = text.Substring(split).Trim()
        End While

        If text.Length > 0 Then
            lines.Add(text)
        End If

        Return lines
    End Function



    ' Create a description XML comment with the given tag, indented the given number of characters
    Private Sub GenerateXmlCommentPart(writer As TextWriter, text As String, xmlTag As String, indent As Integer)
        If String.IsNullOrWhiteSpace(text) Then Return

        text = XmlEscape(text)

        Dim lines = WordWrap(text)

        lines.Insert(0, "<" & xmlTag & ">")
        lines.Add("</" & xmlTag & ">")

        Dim prefix = New String(" "c, indent) & "''' "
        For Each line In lines
            writer.WriteLine(prefix & line)
        Next
    End Sub

    Protected Shared Function XmlEscape(value As String) As String
        Return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
    End Function

    Protected Shadows Function XmlEscapeAndWrap(value As String) As List(Of String)
        Return WordWrap(XmlEscape(value))
    End Function

    Public Sub GenerateSummaryXmlComment(writer As TextWriter, text As String, Optional indent As Integer = 8)
        GenerateXmlCommentPart(writer, text, "summary", indent)
    End Sub

    Public Sub GenerateParameterXmlComment(writer As TextWriter, parameterName As String, text As String, Optional escapeText As Boolean = False, Optional indent As Integer = 8)
        If String.IsNullOrWhiteSpace(text) Then Return

        If escapeText Then
            text = XmlEscape(text)
        Else
            ' Ensure the text does not require escaping.
            Dim filtered = text.Replace("<cref ", "").Replace("/>", "").Replace("&amp;", "").Replace("&lt;", "").Replace("&gt;", "")
            Debug.Assert(filtered = XmlEscape(filtered))
        End If

        Dim prefix = New String(" "c, indent) & "''' "

        writer.WriteLine(prefix & "<param name=""{0}"">", parameterName)

        For Each line In WordWrap(text)
            writer.WriteLine(prefix & line)
        Next

        writer.WriteLine(prefix & "</param>")
    End Sub

    Public Sub GenerateTypeParameterXmlComment(writer As TextWriter, typeParameterName As String, text As String, Optional indent As Integer = 4)
        If String.IsNullOrWhiteSpace(text) Then Return

        text = XmlEscape(text)

        Dim prefix = New String(" "c, indent) & "''' "

        writer.WriteLine(prefix & "<typeparam name=""{0}"">", typeParameterName)

        For Each line In WordWrap(text)
            writer.WriteLine(prefix & line)
        Next

        writer.WriteLine(prefix & "</typeparam>")
    End Sub



    ' Generate and XML comment with the given description and remarks sections. If empty, the sections are omitted.
    Protected Sub GenerateXmlComment(writer As TextWriter, descriptionText As String, remarksText As String, indent As Integer)
        GenerateXmlCommentPart(writer, descriptionText, "summary", indent)
        GenerateXmlCommentPart(writer, remarksText, "remarks", indent)
    End Sub

    ' Generate XML comment for a structure
    Protected Sub GenerateXmlComment(writer As TextWriter, struct As ParseNodeStructure, indent As Integer)
        Dim descriptionText As String = struct.Description
        Dim remarksText As String = Nothing
        GenerateXmlComment(writer, descriptionText, remarksText, indent)
    End Sub

    ' Generate XML comment for an child
    Protected Sub GenerateXmlComment(writer As TextWriter, child As ParseNodeChild, indent As Integer)
        Dim descriptionText As String = child.Description
        Dim remarksText As String = Nothing
        If child.IsOptional Then
            If child.IsList Then
                remarksText = "If nothing is present, an empty list is returned."
            Else
                remarksText = "This child is optional. If it is not present, then Nothing is returned."
            End If
        End If
        GenerateXmlComment(writer, descriptionText, remarksText, indent)
    End Sub

    ' Generate XML comment for an child
    Protected Sub GenerateWithXmlComment(writer As TextWriter, child As ParseNodeChild, indent As Integer)
        Dim descriptionText As String = "Returns a copy of this with the " + ChildPropertyName(child) + " property changed to the specified value. Returns this instance if the specified value is the same as the current value."
        Dim remarksText As String = Nothing
        GenerateXmlComment(writer, descriptionText, remarksText, indent)
    End Sub

    ' Generate XML comment for an field
    Protected Sub GenerateXmlComment(writer As TextWriter, field As ParseNodeField, indent As Integer)
        Dim descriptionText As String = field.Description
        Dim remarksText As String = Nothing
        GenerateXmlComment(writer, descriptionText, remarksText, indent)
    End Sub

    ' Generate XML comment for an kind
    Protected Sub GenerateXmlComment(writer As TextWriter, kind As ParseNodeKind, indent As Integer)
        Dim descriptionText As String = kind.Description
        Dim remarksText As String = Nothing
        GenerateXmlComment(writer, descriptionText, remarksText, indent)
    End Sub

    ' Generate XML comment for an enumeration
    Protected Sub GenerateXmlComment(writer As TextWriter, enumerator As ParseEnumeration, indent As Integer)
        Dim descriptionText As String = enumerator.Description
        Dim remarksText As String = Nothing
        GenerateXmlComment(writer, descriptionText, remarksText, indent)
    End Sub

    ' Generate XML comment for an enumerator
    Protected Sub GenerateXmlComment(writer As TextWriter, enumerator As ParseEnumerator, indent As Integer)
        Dim descriptionText As String = enumerator.Description
        Dim remarksText As String = Nothing
        GenerateXmlComment(writer, descriptionText, remarksText, indent)
    End Sub

    Private ReadOnly _VBKeywords As String() = {
        "ADDHANDLER",
        "ADDRESSOF",
        "ALIAS",
        "AND",
        "ANDALSO",
        "AS",
        "BOOLEAN",
        "BYREF",
        "BYTE",
        "BYVAL",
        "CALL",
        "CASE",
        "CATCH",
        "CBOOL",
        "CBYTE",
        "CCHAR",
        "CDATE",
        "CDBL",
        "CDEC",
        "CHAR",
        "CINT",
        "CLASS",
        "CLNG",
        "COBJ",
        "CONST",
        "CONTINUE",
        "CSBYTE",
        "CSHORT",
        "CSNG",
        "CSTR",
        "CTYPE",
        "CUINT",
        "CULNG",
        "CUSHORT",
        "DATE",
        "DECIMAL",
        "DECLARE",
        "DEFAULT",
        "DELEGATE",
        "DIM",
        "DIRECTCAST",
        "DO",
        "DOUBLE",
        "EACH",
        "ELSE",
        "ELSEIF",
        "END",
        "ENDIF",
        "ENUM",
        "ERASE",
        "ERROR",
        "EVENT",
        "EXIT",
        "FALSE",
        "FINALLY",
        "FOR",
        "FRIEND",
        "FUNCTION",
        "GET",
        "GETTYPE",
        "GETXMLNAMESPACE",
        "GLOBAL",
        "GOSUB",
        "GOTO",
        "HANDLES",
        "IF",
        "IMPLEMENTS",
        "IMPORTS",
        "IN",
        "INHERITS",
        "INTEGER",
        "INTERFACE",
        "IS",
        "ISNOT",
        "LET",
        "LIB",
        "LIKE",
        "LONG",
        "LOOP",
        "ME",
        "MOD",
        "MODULE",
        "MUSTINHERIT",
        "MUSTOVERRIDE",
        "MYBASE",
        "MYCLASS",
        "NAMESPACE",
        "NARROWING",
        "NEW",
        "NEXT",
        "NOT",
        "NOTHING",
        "NOTINHERITABLE",
        "NOTOVERRIDABLE",
        "OBJECT",
        "OF",
        "ON",
        "OPERATOR",
        "OPTION",
        "OPTIONAL",
        "OR",
        "ORELSE",
        "OVERLOADS",
        "OVERRIDABLE",
        "OVERRIDES",
        "PARAMARRAY",
        "PARTIAL",
        "PRIVATE",
        "PROPERTY",
        "PROTECTED",
        "PUBLIC",
        "RAISEEVENT",
        "READONLY",
        "REDIM",
        "REM",
        "REMOVEHANDLER",
        "RESUME",
        "RETURN",
        "SBYTE",
        "SELECT",
        "SET",
        "SHADOWS",
        "SHARED",
        "SHORT",
        "SINGLE",
        "STATIC",
        "STEP",
        "STOP",
        "STRING",
        "STRUCTURE",
        "SUB",
        "SYNCLOCK",
        "THEN",
        "THROW",
        "TO",
        "TRUE",
        "TRY",
        "TRYCAST",
        "TYPEOF",
        "UINTEGER",
        "ULONG",
        "USHORT",
        "USING",
        "VARIANT",
        "WEND",
        "WHEN",
        "WHILE",
        "WIDENING",
        "WITH",
        "WITHEVENTS",
        "WRITEONLY",
        "XOR"}

    ' If the string is a keyword, escape it. Otherwise just return it.
    Protected Function Ident(id As String) As String
        If _VBKeywords.Contains(id.ToUpperInvariant()) Then
            Return "[" + id + "]"
        Else
            Return id
        End If
    End Function

    Public Function EscapeQuotes(s As String) As String
        If s.IndexOf(""""c) <> -1 Then
            Dim sb As New StringBuilder
            Dim parts = s.Split(""""c)
            Dim last = parts.Length - 1
            For i = 0 To last - 1
                sb.Append(parts(i))
                sb.Append("""""")
            Next
            sb.Append(parts(last))
            s = sb.ToString
        End If
        Return s
    End Function


    Public Function GetChildNodeKind(nodeKind As ParseNodeKind, child As ParseNodeChild) As ParseNodeKind
        Dim childNodeKind = TryCast(child.ChildKind, ParseNodeKind)
        Dim childNodeKinds = TryCast(child.ChildKind, List(Of ParseNodeKind))

        If childNodeKinds IsNot Nothing AndAlso nodeKind IsNot Nothing Then
            childNodeKind = child.ChildKind(nodeKind.Name)
        End If

        If childNodeKind Is Nothing AndAlso child.DefaultChildKind IsNot Nothing Then
            childNodeKind = child.DefaultChildKind
        End If

        Return childNodeKind
    End Function

    Public Function IsAutoCreatableToken(node As ParseNodeStructure, nodeKind As ParseNodeKind, child As ParseNodeChild) As Boolean
        Dim childNodeKind = GetChildNodeKind(nodeKind, child)

        If childNodeKind IsNot Nothing Then
            Dim childNodeStructure = KindTypeStructure(childNodeKind)
            If childNodeStructure.IsToken AndAlso childNodeKind.Name <> "IdentifierToken" Then
                Return True
            End If
        End If

        Return False
    End Function

    Public Function IsAutoCreatableNodeOfAutoCreatableTokens(node As ParseNodeStructure, nodeKind As ParseNodeKind, child As ParseNodeChild) As Boolean
        Dim childNodeKind = GetChildNodeKind(nodeKind, child)

        ' Node contains only auto-creatable tokens
        If childNodeKind IsNot Nothing Then
            Dim childNodeStructure = KindTypeStructure(childNodeKind)
            If Not childNodeStructure.IsToken Then
                Dim allChildren = GetAllChildrenOfStructure(childNodeStructure)
                For Each childNodeChild In allChildren
                    If Not IsAutoCreatableToken(childNodeStructure, childNodeKind, childNodeChild) Then
                        Return False
                    End If
                Next

                Return True
            End If
        End If

        Return False
    End Function

    Public Function IsAutoCreatableChild(node As ParseNodeStructure, nodeKind As ParseNodeKind, child As ParseNodeChild) As Boolean
        Return IsAutoCreatableToken(node, nodeKind, child) OrElse IsAutoCreatableNodeOfAutoCreatableTokens(node, nodeKind, child)
    End Function

End Class
