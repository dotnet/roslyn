' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------------------------------------
' Defines the in-memory format of the parse tree description. Many of the structures also 
' contain some of the code for reading themselves from the input file.
'-----------------------------------------------------------------------------------------------------------

Imports System.Globalization
Imports System.Text
Imports System.Xml
Imports <xmlns="http://schemas.microsoft.com/VisualStudio/Roslyn/Compiler">

' Root class of the parse tree. All information about the parse tree is in
' stuff accessible from here. In particular, contains dictionaries of all 
' the various parts (structures, node kinds, enumerations, etc.)
Public Class ParseTree
    Public FileName As String           ' my file name

    Public NamespaceName As String      ' name of the namespace to put stuff in

    Public VisitorName As String        ' name of the visitor class

    Public RewriteVisitorName As String    ' name of the rewriting visitor class

    Public FactoryClassName As String   ' name of the factory class

    Public ContextualFactoryClassName As String   ' name of the contextual factory class

    ' Dictionary of all node-structure's, indexed by name
    Public NodeStructures As New Dictionary(Of String, ParseNodeStructure)

    ' Dictionary of all node-kinds, indexed by name
    Public NodeKinds As New Dictionary(Of String, ParseNodeKind)

    ' Dictionary of all enumerations, indexed by name
    Public Enumerations As New Dictionary(Of String, ParseEnumeration)

    ' Dictionary of all node-kind-alias's, indexed by name
    Public Aliases As New Dictionary(Of String, ParseNodeKindAlias)

    ' Determines which structures are abstract
    Public IsAbstract As New Dictionary(Of ParseNodeStructure, Boolean)

    ' Get the root structure.
    Public RootStructure As ParseNodeStructure

    ' Get the root structure for Tokens, Trivia
    Public RootToken, RootTrivia As ParseNodeStructure

    ' Remember nodes with errors so we only report one error per node.
    Private ReadOnly _elementsWithErrors As New Dictionary(Of XNode, Boolean)

    ' Report an error.
    Public Sub ReportError(referencingNode As XNode, message As String, ParamArray args As Object())
        Dim fullMessage As String = FileName

        If referencingNode IsNot Nothing Then
            If _elementsWithErrors.ContainsKey(referencingNode) Then
                ' We already reported an error on this node.
                Exit Sub
            End If

            _elementsWithErrors(referencingNode) = True      ' remember this so we only report errors on this node once.
            Dim lineInfo = CType(referencingNode, IXmlLineInfo)
            fullMessage += String.Format("({0})", lineInfo.LineNumber)
        End If

        fullMessage += String.Format(": " + message, args)

        Console.WriteLine(fullMessage)
    End Sub

    ' Does this struct have any children (including parents?
    Public Function HasAnyChildren(struct As ParseNodeStructure) As Boolean
        Return struct.Children.Count > 0 OrElse (struct.ParentStructure IsNot Nothing AndAlso HasAnyChildren(struct.ParentStructure))
    End Function

    ' We finished reading the tree. Do a bit of pre-processing.
    Public Sub FinishedReading()
        ' Go through each node-structure and check for the root. 
        RootStructure = Nothing
        For Each struct In NodeStructures.Values
            If struct.IsRoot Then
                If RootStructure IsNot Nothing Then
                    ReportError(RootStructure.Element, "More than one root node specified.")
                    ReportError(struct.Element, "More than one root node specified.")
                End If
                RootStructure = struct
            Else
                ' this node is derived from something else. Remember that.
                struct.ParentStructure.HasDerivedStructure = Not String.IsNullOrEmpty(struct.ParentStructureId)
            End If

            ' Check for token root.
            If struct.IsTokenRoot Then
                If RootToken IsNot Nothing Then ReportError(struct.Element, "More than one token root specified.")
                RootToken = struct
            End If

            ' Check for trivia root.
            If struct.IsTriviaRoot Then
                If RootTrivia IsNot Nothing Then ReportError(struct.Element, "More than one trivia root specified.")
                RootTrivia = struct
            End If

            IsAbstract(struct) = True

            ' Determine "tokens" and trivia by walking the hierarchy
            SetIsTokenAndIsTrivia(struct)
        Next

        ' Figure out abstract nodes - they have no kinds associated with them.
        For Each kind In NodeKinds.Values
            IsAbstract(kind.NodeStructure) = False
        Next
    End Sub

    ' Set the IsToken and IsTrivia flags on a struct
    Private Sub SetIsTokenAndIsTrivia(struct As ParseNodeStructure)
        ' Walk the hierarchy.
        Dim parent = struct

        While parent IsNot Nothing
            If parent.IsTokenRoot Then
                struct.IsToken = True
                Return
            End If
            If parent.IsTriviaRoot Then
                struct.IsTrivia = True
            End If

            parent = parent.ParentStructure
        End While
    End Sub

    Public Function ParseEnumType(enumString As String, referencingElement As XNode) As Object
        If (Enumerations.ContainsKey(enumString)) Then
            Return Enumerations(enumString)
        End If

        ReportError(referencingElement, "{0} is not a valid field type. You should add a node-kind entry in the syntax.xml.", enumString)
        Return Nothing
    End Function


    Public Function ParseOneNodeKind(typeString As String, referencingNode As XNode) As ParseNodeKind
        If (NodeKinds.ContainsKey(typeString)) Then
            Return NodeKinds(typeString)
        End If

        ReportError(referencingNode, "{0} is not a valid node kind", typeString)
        Return Nothing
    End Function

    Public Function ParseNodeKind(typeParts As IList(Of String), referencingNode As XNode) As Object
        Dim typeList As New List(Of ParseNodeKind)

        For Each typePart In typeParts
            Dim foundType = ParseNodeKind(typePart, referencingNode)
            If (TypeOf foundType Is List(Of ParseNodeKind)) Then
                typeList.AddRange(CType(foundType, List(Of ParseNodeKind)))
            Else
                If Not TypeOf foundType Is ParseNodeKind Then
                    ReportError(referencingNode, "{0} cannot be used in a alternation of types; it is not a node kind", typePart)
                Else
                    typeList.Add(CType(foundType, ParseNodeKind))
                End If
            End If
        Next

        If typeList.Count = 1 Then
            Return typeList(0)
        End If
        Return typeList
    End Function

    Public Function ParseNodeKind(typeString As String, referencingNode As XNode) As Object
        If (typeString.Contains("|")) Then
            Dim typeList As New List(Of ParseNodeKind)
            For Each typePart As String In typeString.Split("|"c)
                Dim foundType = ParseNodeKind(typePart, referencingNode)
                If (TypeOf foundType Is List(Of ParseNodeKind)) Then
                    typeList.AddRange(CType(foundType, List(Of ParseNodeKind)))
                Else
                    If Not TypeOf foundType Is ParseNodeKind Then
                        ReportError(referencingNode, "{0} cannot be used in a alternation of types; it is not a node kind", typePart)
                    Else
                        typeList.Add(CType(foundType, ParseNodeKind))
                    End If
                End If
            Next

            Return typeList
        End If

        If typeString.StartsWith("@", StringComparison.Ordinal) Then
            Dim nodeTypeString = typeString.Substring(1)
            If Not NodeStructures.ContainsKey(nodeTypeString) Then
                ReportError(referencingNode, "Unknown structure '@{0}'", nodeTypeString)
                Return Nothing
            End If

            Return NodeStructures(nodeTypeString).GetAllKinds()
        End If

        If Aliases.ContainsKey(typeString) Then
            Return ParseNodeKind(Aliases(typeString).AliasKinds, referencingNode)
        End If

        Return ParseOneNodeKind(typeString, referencingNode)
    End Function

    ' Is this structure some base structure of another, or the same
    Public Function IsAncestorOrSame(parent As ParseNodeStructure, child As ParseNodeStructure) As Boolean
        Do
            If (parent Is child) Then
                Return True
            End If

            child = child.ParentStructure
        Loop While (child IsNot Nothing)

        Return False
    End Function

End Class

' Base class for things in the parse trees. Each one has a reference back 
' to the containing parse tree, and also the XML element it was loaded from.
Public MustInherit Class ParseTreeDefinition
    Private _parseTree As ParseTree

    Public Overridable Property ParseTree() As ParseTree
        Get
            Return Me._parseTree
        End Get
        Set(value As ParseTree)
            Me._parseTree = value
        End Set
    End Property

    ' The element this was loaded from. Primarily useful for getting line/col info for errors.
    Public Element As XElement
End Class

' Information defined in a node-structure element. Defines a node class in the parse tree.
Public Class ParseNodeStructure
    Inherits ParseTreeDefinition

    ' name of the structure.
    Public Name As String

    ' parent as a string.
    Public ParentStructureId As String

    Public ReadOnly Property ParentStructure() As ParseNodeStructure
        Get
            If String.IsNullOrEmpty(ParentStructureId) Then
                Return Nothing
            Else
                If Not ParseTree.NodeStructures.ContainsKey(ParentStructureId) Then
                    ParseTree.ReportError(Element, "Unknown parent structure '{0}' for node-structure '{1}'", ParentStructureId, Name)
                    Return Nothing
                End If
                Return ParseTree.NodeStructures(ParentStructureId)
            End If
        End Get
    End Property

    ' Information about the structure.

    Public Description As String

    Public Abstract As Boolean

    Public PartialClass As Boolean

    Public IsPredefined As Boolean

    Public IsRoot As Boolean

    Public IsTokenRoot, IsTriviaRoot As Boolean ' is this the root of tokens, trivia?

    Public NoFactory As Boolean ' if true, don't create factory method

    Public NodeKinds As List(Of ParseNodeKind)

    Public Fields As List(Of ParseNodeField)

    Public Children As List(Of ParseNodeChild)

    Public HasDerivedStructure As Boolean ' does this node have any nodes derived from it?

    Public ReadOnly InternalSyntaxFacts As Boolean

    Public ReadOnly HasDefaultFactory As Boolean

    Public IsToken As Boolean ' true if node derives from token root
    Public IsTrivia As Boolean ' true if node derives from trivia root

    Public ReadOnly Property IsTerminal As Boolean
        Get
            Return IsToken OrElse (Not ParseTree.HasAnyChildren(Me) AndAlso Not Me.Abstract)
        End Get
    End Property

    Public DefaultTrailingTrivia As String ' default trailing trivia for the simplified factory.

    ' Create a new structure in the give tree and load it from the give XElement.
    Public Sub New(el As XElement, tree As ParseTree)
        Me.ParseTree = tree
        Me.Element = el

        Name = el.@name
        ParentStructureId = el.@parent
        Description = el.<description>.Value
        DefaultTrailingTrivia = el.@<default-trailing-trivia>

        Abstract = If(CType(el.Attribute("abstract"), Boolean?), False)
        PartialClass = If(CType(el.Attribute("partial"), Boolean?), False)
        IsPredefined = If(CType(el.Attribute("predefined"), Boolean?), False)
        IsRoot = If(CType(el.Attribute("root"), Boolean?), False)
        IsTokenRoot = If(CType(el.Attribute("token-root"), Boolean?), False)
        IsTriviaRoot = If(CType(el.Attribute("trivia-root"), Boolean?), False)
        NoFactory = If(CType(el.Attribute("no-factory"), Boolean?), False)
        HasDefaultFactory = If(CType(el.Attribute("has-default-factory"), Boolean?), False)
        InternalSyntaxFacts = If(CType(el.Attribute("syntax-facts-internal"), Boolean?), False)

        NodeKinds = (From nk In el.<node-kind> Select New ParseNodeKind(nk, Me)).ToList()
        For Each nk In NodeKinds
            If tree.NodeKinds.ContainsKey(nk.Name) Then
                tree.ReportError(Element, "node-kind ""{0}"" already defined.", nk.Name)
            Else
                tree.NodeKinds.Add(nk.Name, nk)
            End If
        Next
        Fields = (From f In el.<field> Select New ParseNodeField(f, Me)).ToList()
        Children = (From c In el.<child> Select New ParseNodeChild(c, Me)).ToList()
    End Sub

    Public Function GetAllKinds() As List(Of ParseNodeKind)
        Return New List(Of ParseNodeKind)(From kvPair In ParseTree.NodeKinds Where ParseTree.IsAncestorOrSame(Me, kvPair.Value.NodeStructure) Select kvPair.Value)
    End Function

    Public Function DerivesFrom(baseClassName As String) As Boolean
        Dim nodeStructure As ParseNodeStructure = Me
        While nodeStructure IsNot Nothing
            If String.Compare(nodeStructure.Name, baseClassName, True) = 0 Then
                Return True
            End If
            nodeStructure = nodeStructure.ParentStructure
        End While
        Return False
    End Function

    Public Overrides Function ToString() As String
        Return Name
    End Function
End Class

' Defines a single node kind in the tree. Every node kind is associated with a structure, but
' a structure can have multiple node kinds. Kinds must be unique in the whole tree.
Public Class ParseNodeKind
    Inherits ParseTreeDefinition

    Public Name As String

    Public StructureId As String

    Public TokenText As String

    Public NoFactory As Boolean ' if true, don't create factory method for this kind.

    Public ReadOnly Property NodeStructure() As ParseNodeStructure
        Get
            If String.IsNullOrEmpty(StructureId) Then
                Return Nothing
            Else
                If Not ParseTree.NodeStructures.ContainsKey(StructureId) Then
                    ParseTree.ReportError(Element, "Unknown structure '{0}' for node-kind '{1}'", StructureId, Name)
                    Return Nothing
                End If
                Return ParseTree.NodeStructures(StructureId)
            End If
        End Get
    End Property

    Public Description As String

    Public Sub New(el As XElement, struct As ParseNodeStructure)
        Me.ParseTree = struct.ParseTree
        Me.Element = el

        Name = el.@name
        TokenText = el.@<token-text>
        NoFactory = If(CType(el.Attribute("no-factory"), Boolean?), False)
        StructureId = struct.Name
        Description = If(el.<description>.Value, struct.Description)
    End Sub
End Class

' Defines an alias for one or more node kinds.
Public Class ParseNodeKindAlias
    Inherits ParseTreeDefinition

    Public Name As String

    Public AliasKinds As String

    Public Description As String

    Public Sub New(el As XElement, tree As ParseTree)
        ParseTree = tree
        Me.Element = el

        Name = el.@name
        AliasKinds = el.@alias
        Description = el.<description>.Value
    End Sub
End Class

' A field in a node structure. A field is a property that stores data like integer,
' text, etc, not a child node. Its type can be a simple type or an enumeration type.
Public Class ParseNodeField
    Inherits ParseTreeDefinition

    Public ReadOnly Name As String

    Public ReadOnly ContainingStructure As ParseNodeStructure

    Public ReadOnly IsOptional As Boolean

    Public ReadOnly FieldTypeId As String

    Public ReadOnly Description As String

    Public Sub New(el As XElement, struct As ParseNodeStructure)
        Me.ParseTree = struct.ParseTree
        Me.Element = el
        Me.ContainingStructure = struct

        Name = el.@name
        IsOptional = If(CType(el.Attribute("optional"), Boolean?), False)
        FieldTypeId = el.@type
        Description = el.<description>.Value
    End Sub

    ' Gets the field type. Could return a SimpleType, Enumeration 
    Public ReadOnly Property FieldType() As Object
        Get
            Select Case FieldTypeId.ToLowerInvariant()
                Case "boolean"
                    Return SimpleType.Bool
                Case "text"
                    Return SimpleType.Text
                Case "character"
                    Return SimpleType.Character
                Case "int32"
                    Return SimpleType.Int32
                Case "uint32"
                    Return SimpleType.UInt32
                Case "int64"
                    Return SimpleType.Int64
                Case "uint64"
                    Return SimpleType.UInt64
                Case "float32"
                    Return SimpleType.Float32
                Case "float64"
                    Return SimpleType.Float64
                Case "decimal"
                    Return SimpleType.Decimal
                Case "datetime"
                    Return SimpleType.DateTime
                Case "textspan"
                    Return SimpleType.TextSpan
                Case "nodekind"
                    Return SimpleType.NodeKind
                Case Else
                    Return ParseTree.ParseEnumType(FieldTypeId, Element)
            End Select
        End Get
    End Property


End Class

' Defines a child node with a node structure. A child can be a single child
' node of a list of child nodes.
Public Class ParseNodeChild
    Inherits ParseTreeDefinition

    Public ReadOnly Name As String

    Public ReadOnly ContainingStructure As ParseNodeStructure

    Public ReadOnly IsOptional As Boolean

    Public ReadOnly IsList As Boolean

    Public ReadOnly IsSeparated As Boolean

    Public ReadOnly SeparatorsName As String

    Private ReadOnly _childKindNames As New Dictionary(Of String, List(Of String))
    Private _childKind As Object

    Public ReadOnly SeparatorsTypeId As String

    Public ReadOnly Description As String

    Public ReadOnly Order As Single

    Public ReadOnly NotInFactory As Boolean

    Public ReadOnly GenerateWith As Boolean

    Public ReadOnly InternalSyntaxFacts As Boolean

    Public KindForNodeKind As Dictionary(Of String, ParseNodeKind)

    Private ReadOnly _defaultChildKindName As String
    Private _defaultChildKind As ParseNodeKind

    Public Sub New(el As XElement, struct As ParseNodeStructure)
        Me.ParseTree = struct.ParseTree
        Me.Element = el
        Me.ContainingStructure = struct
        Single.TryParse(el.@order, NumberStyles.Any, CultureInfo.InvariantCulture, Me.Order)
        Name = el.@name
        SeparatorsName = el.@<separator-name>
        SeparatorsTypeId = el.@<separator-kind>
        IsList = If(CType(el.Attribute("list"), Boolean?), False)
        IsSeparated = el.@<separator-kind> <> ""
        IsOptional = If(CType(el.Attribute("optional"), Boolean?), False)
        Description = el.<description>.Value
        NotInFactory = If(CType(el.Attribute("not-in-factory"), Boolean?), False)
        GenerateWith = If(CType(el.Attribute("generate-with"), Boolean?), False)
        InternalSyntaxFacts = If(CType(el.Attribute("syntax-facts-internal"), Boolean?), False)
        _defaultChildKindName = CType(el.Attribute("default-kind"), String)

        For Each kind In el.<kind>
            ' The kind may be duplicated
            Dim nodeKinds As List(Of String) = Nothing
            If _childKindNames.TryGetValue(kind.@name, nodeKinds) Then
                nodeKinds.Add(kind.@<node-kind>)
            Else
                nodeKinds = New List(Of String)
                nodeKinds.Add(kind.@<node-kind>)
                _childKindNames.Add(kind.@name, nodeKinds)
            End If
        Next
        If _childKindNames.Count = 0 Then
            Dim kindsString = el.@kind
            For Each kind As String In kindsString.Split("|"c)
                _childKindNames.Add(kind, Nothing) 'New List(Of String)(From nodeKind In struct.NodeKinds Select nodeKind.Name))
            Next
        ElseIf el.@kind IsNot Nothing Then
            ParseTree.ReportError(el, "Cannot have both kind attribute on child and also have kind sub element in child.")
        End If
    End Sub

    Public ReadOnly Property ChildKind(kind As String) As ParseNodeKind
        Get
            If KindForNodeKind Is Nothing Then
                KindForNodeKind = New Dictionary(Of String, ParseNodeKind)
                For Each key In _childKindNames.Keys
                    Dim nodeNames = _childKindNames(key)
                    If nodeNames IsNot Nothing Then
                        Dim child = ParseTree.ParseOneNodeKind(key, Element)
                        For Each nodeName In nodeNames
                            Dim node = ParseTree.ParseOneNodeKind(nodeName, Element)
                            KindForNodeKind.Add(node.Name, child)
                        Next
                    End If
                Next
            End If
            Dim result As ParseNodeKind = Nothing
            KindForNodeKind.TryGetValue(kind, result)
            Return result
        End Get
    End Property

    Public ReadOnly Property ChildKind(containerKinds As IList(Of ParseNodeKind)) As List(Of ParseNodeKind)
        Get
            Dim allChildkinds As List(Of ParseNodeKind) = TryCast(ChildKind(), List(Of ParseNodeKind))
            If allChildkinds Is Nothing Then
                Return Nothing
            End If

            Dim childkindsForContainer = New List(Of ParseNodeKind)
            For Each containerKind In containerKinds
                Dim propertyKind = ChildKind(containerKind.Name)
                If propertyKind IsNot Nothing Then
                    childkindsForContainer.Add(propertyKind)
                End If
            Next
            If childkindsForContainer.Count > 0 Then
                Return childkindsForContainer
            End If

            Return allChildkinds
        End Get
    End Property


    ' Gets the child type. Could return a NodeKind, List(NodeKind) containing the allowable node kinds of the child.
    Public ReadOnly Property ChildKind() As Object
        Get
            If _childKind Is Nothing Then
                Dim names(_childKindNames.Count - 1) As String
                _childKindNames.Keys.CopyTo(names, 0)
                _childKind = ParseTree.ParseNodeKind(names, Element)
            End If
            Return _childKind
        End Get
    End Property

    Public Function WithChildKind(childKind As Object) As ParseNodeChild
        Dim copy = New ParseNodeChild(Me.Element, Me.ContainingStructure)
        copy._childKind = childKind
        Return copy
    End Function

    Public ReadOnly Property DefaultChildKind() As ParseNodeKind
        Get
            If _defaultChildKind Is Nothing AndAlso _defaultChildKindName IsNot Nothing Then
                _defaultChildKind = CType(ParseTree.ParseNodeKind(_defaultChildKindName, Element), ParseNodeKind)
            End If
            Return _defaultChildKind
        End Get
    End Property

    ' Gets the separators type. Could return a NodeKind, List(NodeKind) containing the allowable node kinds of the child.
    Public ReadOnly Property SeparatorsKind() As Object
        Get
            Return ParseTree.ParseNodeKind(SeparatorsTypeId, Element)
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return Name
    End Function

End Class

' Defines an enumeration type, so that fields of this type can be defined.
Public Class ParseEnumeration
    Inherits ParseTreeDefinition

    Public Name As String

    Public IsFlags As Boolean

    Public Description As String

    Public Enumerators As List(Of ParseEnumerator)

    Public Sub New(el As XElement, tree As ParseTree)
        ParseTree = tree
        Me.Element = el

        Name = el.@name
        IsFlags = If(CType(el.Attribute("flags"), Boolean?), False)
        Description = el.<description>.Value

        Enumerators = (From en In el.<enumerators>.<enumerator> Select New ParseEnumerator(en, Me)).ToList()
    End Sub
End Class

' Defines a single enumerator inside an enumeration type.
Public Class ParseEnumerator
    Public Name As String

    Public ValueString As String

    Public Description As String

    Public Sub New(el As XElement, enumeration As ParseEnumeration)
        Name = el.@name
        ValueString = el.@hexvalue
        Description = el.<description>.Value
    End Sub

    Public ReadOnly Property Value() As Long
        Get
            Return Convert.ToInt64(ValueString, 16)
        End Get
    End Property


End Class

' THe kinds of simple types for fields.
Public Enum SimpleType
    Bool
    Text
    Character
    Int32
    UInt32
    Int64
    UInt64
    Float32
    Float64
    [Decimal]
    DateTime
    TextSpan
    NodeKind
End Enum
