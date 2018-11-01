' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO

' Class to write out the code for the code tree.
Friend Class RedNodeWriter
    Inherits WriteUtils

    Private _writer As TextWriter    'output is sent here.

    ' Initialize the class with the parse tree to write.
    Public Sub New(parseTree As ParseTree)
        MyBase.New(parseTree)
    End Sub

    ' Write out the code defining the tree to the give file.
    Public Sub WriteMainTreeAsCode(writer As TextWriter)
        _writer = writer
        GenerateMainNamespace()
    End Sub

    Public Sub WriteSyntaxTreeAsCode(writer As TextWriter)
        _writer = writer
        GenerateSyntaxNamespace()
    End Sub

    Private Sub GenerateMainNamespace()
        If Not String.IsNullOrEmpty(_parseTree.NamespaceName) Then
            _writer.WriteLine()
            _writer.WriteLine("Namespace {0}", Ident(_parseTree.NamespaceName))
            _writer.WriteLine()
        End If

        ' We no longer generate SyntaxKind. Instead we write it by hand to ensure we do
        ' so while maintaining compatibility (i.e. never change the numbering)
        ''GenerateKindEnum()
        ''_writer.WriteLine()

        If Not String.IsNullOrEmpty(_parseTree.VisitorName) Then
            GenerateVisitorClass(True)
            GenerateVisitorClass(False)
        End If

        If Not String.IsNullOrEmpty(_parseTree.RewriteVisitorName) Then
            GenerateRewriteVisitorClass()
        End If

        If Not String.IsNullOrEmpty(_parseTree.NamespaceName) Then
            _writer.WriteLine("End Namespace")
        End If
    End Sub

    Private Sub GenerateSyntaxNamespace()
        If Not String.IsNullOrEmpty(_parseTree.NamespaceName) Then
            _writer.WriteLine()
            _writer.WriteLine("Namespace {0}", Ident(_parseTree.NamespaceName & ".Syntax"))
            _writer.WriteLine()
        End If

        GenerateEnumTypes()
        _writer.WriteLine()

        'GenerateRedFactory()
        _writer.WriteLine()

        GenerateNodeStructures()

        If Not String.IsNullOrEmpty(_parseTree.NamespaceName) Then
            _writer.WriteLine("End Namespace")
        End If
    End Sub

    Private Sub GenerateKindEnum()
        ' XML comment
        GenerateXmlComment(_writer, "Enumeration with all Visual Basic syntax node kinds.", Nothing, 4)

#If ListFlags Then
        _writer.WriteLine("    <Flags()>", NodeKindType())
#End If
        _writer.WriteLine("    Public Enum {0} As UShort", NodeKindType())

        _writer.WriteLine("        None")
        _writer.WriteLine("        List = GreenNode.ListKind")

        Dim count = 0
        For Each kind In _parseTree.NodeKinds.Values
            GenerateKindEnumerator(kind)
            count += 1
        Next

#If ListFlags Then
        Dim syntaxListKind = (2 * count) And MaxSyntaxKinds
        _writer.WriteLine("        List = &h{0:x}", syntaxListKind)
        _writer.WriteLine("        SeparatedList = &h{0:x}", syntaxListKind * 2 + syntaxListKind)
#End If
        _writer.WriteLine("    End Enum")
    End Sub

    Private Sub GenerateKindEnumerator(kind As ParseNodeKind)
        ' XML comment
        GenerateXmlComment(_writer, kind, 8)

        _writer.Write("        {0}", Ident(kind.Name))

        For i = 1 To 35 - Ident(kind.Name).Length
            _writer.Write(" ")
        Next

        _writer.Write("' {0}", StructureTypeName(kind.NodeStructure))

        ' Write the list of parent classes also.
        Dim struct = kind.NodeStructure.ParentStructure
        Do While struct IsNot Nothing AndAlso Not IsRoot(struct)
            _writer.Write(" : {0}", StructureTypeName(struct))
            struct = struct.ParentStructure
        Loop

        _writer.WriteLine()
    End Sub

    Private Sub GenerateEnumTypes()
        For Each enumerationType In _parseTree.Enumerations.Values
            GenerateEnumerationType(enumerationType)
        Next
    End Sub


    Private Sub GenerateRedFactory()
        'Private Shared Function CreateRed(ByVal green As GreenNode, ByVal parent As SyntaxNode, ByVal startLocation As Integer) As SyntaxNode
        '    Requires(green IsNot Nothing)
        '    Requires(startLocation >= 0)
        '    
        '       Select Case green.Kind
        _writer.WriteLine("Partial Public MustInherit Class {0}", StructureTypeName(_parseTree.RootStructure))
        _writer.WriteLine("    Friend Shared Function CreateRed(ByVal green As Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}, ByVal parent As {0}, ByVal startLocation As Integer) As {0}", StructureTypeName(_parseTree.RootStructure))
        _writer.WriteLine("        Debug.Assert(green IsNot Nothing)")
        _writer.WriteLine("        Debug.Assert(startLocation >= 0)")
        _writer.WriteLine("")
        _writer.WriteLine("        Select Case green.Kind")

        For Each nodeStructure In _parseTree.NodeStructures.Values
            GenerateFactoryCase(nodeStructure)
        Next

        _writer.WriteLine("            Case Else")
        _writer.WriteLine("                Throw New InvalidOperationException(""unexpected node kind"")")
        _writer.WriteLine("        End Select")
        _writer.WriteLine("    End Function")
        _writer.WriteLine("End Class")

        '       Case Else
        '        Throw New InvalidOperationException("unexpected node kind")
        '            End Select
        '    End Sub
        'End Class
    End Sub

    Private Sub GenerateFactoryCase(nodeStructure As ParseNodeStructure)
        If nodeStructure.Abstract Then
            Return
        End If

        Dim kinds = nodeStructure.NodeKinds
        Dim first As Boolean = True
        For Each kind In kinds
            If first Then
                _writer.Write("            Case SyntaxKind." & kind.Name)
                first = False
            Else
                _writer.Write("," & vbCrLf)
                _writer.Write("                 SyntaxKind." & kind.Name)

            End If
        Next
        _writer.WriteLine("")
        _writer.WriteLine("                Return New " & StructureTypeName(nodeStructure) & "(green, parent, startLocation)")
        _writer.WriteLine("")
    End Sub

    Private Sub GenerateNodeStructures()
        For Each nodeStructure In _parseTree.NodeStructures.Values
            If Not (nodeStructure.IsToken OrElse nodeStructure.IsTrivia) Then
                GenerateNodeStructureClass(nodeStructure)
            End If
        Next
    End Sub

    ' Generate an enumeration type
    Private Sub GenerateEnumerationType(enumeration As ParseEnumeration)
        ' XML comment
        GenerateXmlComment(_writer, enumeration, 4)

        If enumeration.IsFlags Then
            _writer.WriteLine("    <Flags()> _")
        End If
        _writer.WriteLine("    Public Enum {0}", EnumerationTypeName(enumeration))

        For Each enumerator In enumeration.Enumerators
            GenerateEnumeratorVariable(enumerator, enumeration.IsFlags)
        Next

        _writer.WriteLine("    End Enum")
        _writer.WriteLine()
    End Sub

    ' Generate a public enumerator declaration for a enumerator. 
    ' If generateValue is true, generate the value also
    Private Sub GenerateEnumeratorVariable(enumerator As ParseEnumerator, generateValue As Boolean)
        _writer.WriteLine()

        ' XML comment
        GenerateXmlComment(_writer, enumerator, 8)

        _writer.Write("        {0}", Ident(enumerator.Name))
        If generateValue Then
            _writer.Write(" = {0}", GetConstantValue(enumerator.Value))
        End If
        _writer.WriteLine()
    End Sub

    ' Generate an constant value
    Private Function GetConstantValue(val As Long) As String
        Return val.ToString()
    End Function

    ' Generate a class declaration for a node structure.
    Private Sub GenerateNodeStructureClass(nodeStructure As ParseNodeStructure)
        ' SyntaxNode lives in the root namespace now, unlike the other unwashed node types.
        If nodeStructure.IsPredefined Then Return

        ' XML comment
        GenerateXmlComment(_writer, nodeStructure, 4)

        ' Class name
        _writer.Write("    ")
        If (nodeStructure.PartialClass) Then
            _writer.Write("Partial ")
        End If
        If _parseTree.IsAbstract(nodeStructure) Then
            _writer.WriteLine("Public MustInherit Class {0}", StructureTypeName(nodeStructure))
        ElseIf Not nodeStructure.HasDerivedStructure Then
            _writer.WriteLine("Public NotInheritable Class {0}", StructureTypeName(nodeStructure))
        Else
            _writer.WriteLine("Public Class {0}", StructureTypeName(nodeStructure))
        End If

        ' Base class
        If Not IsRoot(nodeStructure) Then
            _writer.WriteLine("        Inherits {0}", StructureTypeName(nodeStructure.ParentStructure))
        End If
        _writer.WriteLine()

        'Create members
        GenerateNodeStructureMembers(nodeStructure)

        ' Create the constructor.
        GenerateNodeStructureConstructor(nodeStructure)

        ' Create the IsTerminal property
        Dim parentStructure = nodeStructure.ParentStructure
        If parentStructure IsNot Nothing AndAlso nodeStructure.IsTerminal <> parentStructure.IsTerminal Then
            GenerateIsTerminal(nodeStructure)
        End If

        ' Create the property accessor for each of the fields
        Dim allFields = GetAllFieldsOfStructure(nodeStructure)
        For i = 0 To allFields.Count - 1
            GenerateNodeFieldProperty(allFields(i), StructureTypeName(nodeStructure), allFields(i).ContainingStructure IsNot nodeStructure)
        Next

        ' Create the property accessor for each of the children

        Dim allChildren = GetAllChildrenOfStructure(nodeStructure)
        For i = 0 To allChildren.Count - 1
            Dim child = allChildren(i)
            If child.IsList AndAlso child.IsSeparated Then
                GenerateNodeSeparatedListChildProperty(nodeStructure, child, i, child.ContainingStructure IsNot nodeStructure)
            Else
                GenerateNodeChildProperty(nodeStructure, child, i, child.ContainingStructure IsNot nodeStructure)
            End If
            GenerateNodeWithChildProperty(child, i, nodeStructure)

            If child.IsList Then
                GenerateChildListAccessor(nodeStructure, child)
            ElseIf HasNestedList(nodeStructure, child) Then
                GeneratedNestedChildListAccessor(nodeStructure, child)
            End If
        Next

        If allChildren.Count > 0 Then
            'GenerateGetSlot(nodeStructure, allChildren)
            GenerateGetCachedSlot(nodeStructure, allChildren)
            GenerateGetNodeSlot(nodeStructure, allChildren)
            'GenerateGetSlotCount(nodeStructure, allChildren)
        End If

        ' Visitor accept method
        If Not String.IsNullOrEmpty(_parseTree.VisitorName) Then
            GenerateAccept(nodeStructure)
        End If

        GenerateUpdate(nodeStructure)

        ' Special methods for the root node.
        If IsRoot(nodeStructure) Then
            GenerateRootNodeSpecialMethods(nodeStructure)
        End If

        ' End the class
        _writer.WriteLine("    End Class")
        _writer.WriteLine()
    End Sub

    Private Sub GenerateChildListAccessor(nodeStructure As ParseNodeStructure, child As ParseNodeChild)
        Dim kindType = KindTypeStructure(child.ChildKind)
        Dim itemType = If(kindType.IsToken, "SyntaxToken", kindType.Name)

        If _parseTree.IsAbstract(nodeStructure) Then

            If nodeStructure.Children.Contains(child) Then
                _writer.WriteLine("        Public Shadows Function Add{0}(ParamArray items As {1}()) As {2}", child.Name, itemType, nodeStructure.Name)
                _writer.WriteLine("            Return Add{0}Core(items)", child.Name)
                _writer.WriteLine("        End Function")
                _writer.WriteLine("        Friend MustOverride Function Add{0}Core(ParamArray items As {1}()) As {2}", child.Name, itemType, nodeStructure.Name)
            End If

        Else
            _writer.WriteLine("        Public Shadows Function Add{0}(ParamArray items As {1}()) As {2}", child.Name, itemType, nodeStructure.Name)
            _writer.WriteLine("            Return Me.With{0}(Me.{0}.AddRange(items))", child.Name)
            _writer.WriteLine("        End Function")
            _writer.WriteLine()

            If Not nodeStructure.Children.Contains(child) AndAlso
                   nodeStructure.ParentStructure.Children.Contains(child) Then

                _writer.WriteLine("        Friend Overrides Function Add{0}Core(ParamArray items As {1}()) As {2}", child.Name, itemType, nodeStructure.ParentStructure.Name)
                _writer.WriteLine("            Return Add{0}(items)", child.Name)
                _writer.WriteLine("        End Function")
                _writer.WriteLine()
            End If
        End If
    End Sub

    Private Function HasNestedList(nodeStructure As ParseNodeStructure, child As ParseNodeChild) As Boolean
        Dim nestedList = GetNestedList(nodeStructure, child)
        Return nestedList IsNot Nothing
    End Function

    ' The nested list of a structure is the one and only list child
    ' The structure's other children must be auto-creatable.
    Private Function GetNestedList(nodeStructure As ParseNodeStructure, child As ParseNodeChild) As ParseNodeChild
        Dim childStructure = KindTypeStructure(child.ChildKind)
        If childStructure.IsToken Then Return Nothing
        Dim children = GetAllChildrenOfStructure(childStructure).ToList()
        Dim listChild As ParseNodeChild = Nothing
        For Each childNodeChild In children
            If childNodeChild.IsList Then
                If listChild Is Nothing Then
                    listChild = childNodeChild
                Else
                    ' More than one list!
                    Return Nothing
                End If
            ElseIf Not IsAutoCreatableChild(childStructure, Nothing, childNodeChild) Then
                Return Nothing
            End If
        Next
        Return listChild
    End Function

    Private Sub GeneratedNestedChildListAccessor(nodeStructure As ParseNodeStructure, child As ParseNodeChild)
        Dim childStructure = KindTypeStructure(child.ChildKind)
        If _parseTree.IsAbstract(childStructure) Then
            Return
        End If

        Dim nestedList = GetNestedList(nodeStructure, child)
        Dim nestedListStructure = KindTypeStructure(nestedList.ChildKind)
        Dim itemType = If(nestedListStructure.IsToken, "SyntaxToken", nestedListStructure.Name)

        If _parseTree.IsAbstract(nodeStructure) Then
            If nodeStructure.Children.Contains(child) Then

                _writer.WriteLine("        Public Shadows Function Add{0}{1}(ParamArray items As {2}()) As {3}", child.Name, nestedList.Name, itemType, nodeStructure.Name)
                _writer.WriteLine("            Return Add{0}{1}Core(items)", child.Name, nestedList.Name)
                _writer.WriteLine("        End Function")
                _writer.WriteLine("        Friend MustOverride Function Add{0}{1}Core(ParamArray items As {2}()) As {3}", child.Name, nestedList.Name, itemType, nodeStructure.Name)
                _writer.WriteLine()
            End If
        Else

            _writer.WriteLine("        Public Shadows Function Add{0}{1}(ParamArray items As {2}()) As {3}", child.Name, nestedList.Name, itemType, nodeStructure.Name)
            _writer.WriteLine("            Dim _child = If (Me.{0} IsNot Nothing, Me.{0}, SyntaxFactory.{1}())", child.Name, FactoryName(childStructure))
            _writer.WriteLine("            Return Me.With{0}(_child.Add{1}(items))", child.Name, nestedList.Name)
            _writer.WriteLine("        End Function")
            _writer.WriteLine()

            If Not nodeStructure.Children.Contains(child) AndAlso
                   nodeStructure.ParentStructure.Children.Contains(child) Then

                _writer.WriteLine("        Friend Overrides Function Add{0}{1}Core(ParamArray items As {2}()) As {3}", child.Name, nestedList.Name, itemType, nodeStructure.ParentStructure.Name)
                _writer.WriteLine("            Return Add{0}{1}(items)", child.Name, nestedList.Name)
                _writer.WriteLine("        End Function")
                _writer.WriteLine()
            End If
        End If
    End Sub

    ' Generate IsTerminal property.
    Private Sub GenerateIsTerminal(nodeStructure As ParseNodeStructure)
        _writer.WriteLine("        Public Overrides ReadOnly Property IsTerminal As Boolean")
        _writer.WriteLine("            Get")
        _writer.WriteLine("                Return {0}", If(nodeStructure.IsTerminal, "True", "False"))
        _writer.WriteLine("            End Get")
        _writer.WriteLine("        End Property")
        _writer.WriteLine()
    End Sub

    Private Sub GenerateNodeStructureMembers(nodeStructure As ParseNodeStructure)
        Dim children = nodeStructure.Children
        For Each child In children
            If Not KindTypeStructure(child.ChildKind).IsToken Then
                _writer.WriteLine("        Friend {0} as {1}", ChildVarName(child), ChildPrivateFieldTypeRef(child))
            End If
        Next

        _writer.WriteLine()
    End Sub

    ' Generate constructor for a node structure
    Private Sub GenerateNodeStructureConstructor(nodeStructure As ParseNodeStructure)
        If Not IsRoot(nodeStructure) AndAlso
            nodeStructure.Name <> "StructuredTriviaSyntax" Then

            _writer.WriteLine("        Friend Sub New(ByVal green As GreenNode, ByVal parent as SyntaxNode, ByVal startLocation As Integer)")
            _writer.WriteLine("            MyBase.New(green, parent, startLocation)")
            _writer.WriteLine("            Debug.Assert(green IsNot Nothing)")
            _writer.WriteLine("            Debug.Assert(startLocation >= 0)")
            _writer.WriteLine("        End Sub")
            _writer.WriteLine()
        End If

        Dim allFields = GetAllFieldsOfStructure(nodeStructure)
        If Not _parseTree.IsAbstract(nodeStructure) AndAlso StructureTypeName(nodeStructure) <> "IdentifierSyntax" Then
            _writer.Write("        Friend Sub New(")

            ' Generate each of the field parameters
            _writer.Write("ByVal kind As {0}, ByVal errors as DiagnosticInfo(), ByVal annotations as SyntaxAnnotation()", NodeKindType())

            If nodeStructure.IsTerminal Then
                ' terminals have a text
                _writer.Write(", text as String")
            End If

            If nodeStructure.IsToken Then
                ' tokens have trivia
                _writer.Write(", precedingTrivia As SyntaxTriviaList, followingTrivia As SyntaxTriviaList", StructureTypeName(_parseTree.RootTrivia))
            End If

            For Each field In allFields
                _writer.Write(", ")
                GenerateNodeStructureFieldParameter(field)
            Next

            For Each child In GetAllChildrenOfStructure(nodeStructure)
                _writer.Write(", ")
                GenerateNodeStructureChildParameter(child, Nothing)
            Next
            _writer.WriteLine(")")

            ' Generate call to create new builder, and pass result to my private constructor.

            _writer.Write("            Me.New(New Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}(", StructureTypeName(nodeStructure))

            ' Generate each of the field parameters
            _writer.Write("kind", NodeKindType())

            _writer.Write(", errors, annotations")

            If nodeStructure.IsTerminal Then
                ' nonterminals have text.
                _writer.Write(", text")
            End If

            If nodeStructure.IsToken AndAlso Not nodeStructure.IsTrivia Then
                ' tokens havetrivia, but only if they are not trivia.
                _writer.Write(", precedingTrivia.Node, followingTrivia.Node")
            End If

            If allFields.Count > 0 Then
                For i = 0 To allFields.Count - 1
                    _writer.Write(", {0}", FieldParamName(allFields(i)))
                Next
            End If


            For Each child In GetAllChildrenOfStructure(nodeStructure)
                If KindTypeStructure(child.ChildKind).IsToken Then
                    _writer.Write(", {0}", ChildParamName(child))

                ElseIf child.IsList Then
                    _writer.Write(", if({0} IsNot Nothing, {0}.Green, Nothing)", ChildParamName(child))

                Else
                    If child.IsOptional Then
                        ' optional normal child.
                        _writer.Write(", if({0} IsNot Nothing ", ChildParamName(child))
                    End If

                    ' non-optional normal child.
                    _writer.Write(", DirectCast({0}.Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{1})", ChildParamName(child), BaseTypeReference(child))

                    If child.IsOptional Then
                        ' optional normal child.
                        _writer.Write(", Nothing) ")
                    End If
                End If
            Next

            _writer.WriteLine("), Nothing, 0)")

            ' Generate contracts
            If nodeStructure.IsTerminal Then
                ' tokens and trivia have a text
                _writer.WriteLine("            Debug.Assert(text IsNot Nothing)")
            End If

            ' Generate End Sub
            _writer.WriteLine("        End Sub")
            _writer.WriteLine()

        End If
    End Sub

    ' Generate a parameter corresponding to a node structure field
    Private Sub GenerateNodeStructureFieldParameter(field As ParseNodeField, Optional conflictName As String = Nothing)
        _writer.Write("{0} As {1}", FieldParamName(field, conflictName), FieldTypeRef(field))
    End Sub

    ' Generate a parameter corresponding to a node structure child
    Private Sub GenerateNodeStructureChildParameter(child As ParseNodeChild, Optional conflictName As String = Nothing)
        _writer.Write("{0} As {1}", ChildParamName(child, conflictName), ChildConstructorTypeRef(child))
    End Sub

    ' Generate a parameter corresponding to a node structure child
    Private Sub GenerateFactoryChildParameter(node As ParseNodeStructure, child As ParseNodeChild, Optional conflictName As String = Nothing)
        _writer.Write("{0} As {1}", ChildParamName(child, conflictName), ChildFactoryTypeRef(node, child, False, False))
    End Sub

    ' Get modifiers
    Private Function GetModifiers(containingStructure As ParseNodeStructure, isOverride As Boolean, name As String) As String
        ' Is this overridable or an override?
        Dim modifiers = ""

        If isOverride Then
            modifiers = "Overrides"
        ElseIf containingStructure.HasDerivedStructure Then
            modifiers = "Overridable"
        End If

        ' Put Shadows modifier on if useful.
        ' Object has Equals and GetType
        ' root name has members for every kind and structure (factory methods)
        If (name = "Equals" OrElse name = "GetType") Then ' OrElse _parseTree.NodeKinds.ContainsKey(name) OrElse _parseTree.NodeStructures.ContainsKey(name)) Then
            modifiers = "Shadows " + modifiers
        End If
        Return modifiers
    End Function

    ' Generate a public property for a node field
    Private Sub GenerateNodeFieldProperty(field As ParseNodeField, TypeName As String, isOverride As Boolean)
        ' XML comment
        GenerateXmlComment(_writer, field, 8)

        _writer.WriteLine("        Public {2} ReadOnly Property {0} As {1}", FieldPropertyName(field), FieldTypeRef(field), GetModifiers(field.ContainingStructure, False, field.Name))
        _writer.WriteLine("            Get")
        _writer.WriteLine("                Return DirectCast(Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}).{1}", TypeName, FieldPropertyName(field))
        _writer.WriteLine("            End Get")
        _writer.WriteLine("        End Property")
        _writer.WriteLine("")
    End Sub

    ' Generate a public property for a child

    Private Sub GenerateNodeChildProperty(nodeStructure As ParseNodeStructure, child As ParseNodeChild, childIndex As Integer, isOverride As Boolean)

        ' XML comment
        GenerateXmlComment(_writer, child, 8)

        If nodeStructure.HasDerivedStructure Then

            _writer.WriteLine("        Public ReadOnly Property {0} As {1}", ChildPropertyName(child), ChildPropertyTypeRef(nodeStructure, child))
            _writer.WriteLine("            Get")
            _writer.WriteLine("                Return Me.Get{0}Core()", child.Name, ChildPropertyTypeRef(nodeStructure, child))
            _writer.WriteLine("            End Get")
            _writer.WriteLine("        End Property")
            _writer.WriteLine("")

            _writer.WriteLine("        Friend {0} Function Get{1}Core() As {2}", GetModifiers(child.ContainingStructure, isOverride, child.Name), child.Name, ChildPropertyTypeRef(nodeStructure, child, denyOverride:=True))
            Me.GenerateNodeChildPropertyRedAccessLogic(nodeStructure, child, childIndex, isOverride)
            _writer.WriteLine("        End Function")
            _writer.WriteLine()

        ElseIf isOverride Then

            _writer.WriteLine("        Public {0} ReadOnly Property {1} As {2}", If(isOverride, "Shadows", ""), ChildPropertyName(child), ChildPropertyTypeRef(nodeStructure, child))
            _writer.WriteLine("            Get")
            Me.GenerateNodeChildPropertyRedAccessLogic(nodeStructure, child, childIndex, isOverride)
            _writer.WriteLine("            End Get")
            _writer.WriteLine("        End Property")
            _writer.WriteLine("")

            _writer.WriteLine("        Friend {0} Function Get{1}Core() As {2}", GetModifiers(child.ContainingStructure, isOverride, child.Name), child.Name, ChildPropertyTypeRef(nodeStructure, child, denyOverride:=True))
            _writer.WriteLine("            Return Me.{0}", ChildPropertyName(child), ChildPropertyTypeRef(nodeStructure, child))
            _writer.WriteLine("        End Function")
            _writer.WriteLine()

        Else

            _writer.WriteLine("        Public {0} ReadOnly Property {1} As {2}", GetModifiers(child.ContainingStructure, isOverride, child.Name), ChildPropertyName(child), ChildPropertyTypeRef(nodeStructure, child))
            _writer.WriteLine("            Get")
            Me.GenerateNodeChildPropertyRedAccessLogic(nodeStructure, child, childIndex, isOverride)
            _writer.WriteLine("            End Get")
            _writer.WriteLine("        End Property")
            _writer.WriteLine("")

        End If
    End Sub

    Private Sub GenerateNodeChildPropertyRedAccessLogic(nodeStructure As ParseNodeStructure, child As ParseNodeChild, childIndex As Integer, isOverride As Boolean)
        If KindTypeStructure(child.ChildKind).IsToken Then
            If child.IsList Then
                _writer.WriteLine("                Dim slot = DirectCast(Me.Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}).{1}", StructureTypeName(nodeStructure), ChildVarName(child))
                _writer.WriteLine("                If slot IsNot Nothing")
                _writer.WriteLine("                    return new SyntaxTokenList(Me, slot, {0}, {1})", Me.GetChildPosition(childIndex), Me.GetChildIndex(childIndex))
                _writer.WriteLine("                End If")
                _writer.WriteLine("                Return Nothing")
            Else
                If child.IsOptional Then
                    _writer.WriteLine("                Dim slot = DirectCast(Me.Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}).{1}", StructureTypeName(nodeStructure), ChildVarName(child))
                    _writer.WriteLine("                If slot IsNot Nothing")
                    _writer.WriteLine("                    return new SyntaxToken(Me, slot, {0}, {1})", Me.GetChildPosition(childIndex), Me.GetChildIndex(childIndex))
                    _writer.WriteLine("                End If")
                    _writer.WriteLine("                Return Nothing")
                Else
                    _writer.WriteLine("                return new SyntaxToken(Me, DirectCast(Me.Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}).{1}, {2}, {3})", StructureTypeName(nodeStructure), ChildVarName(child), Me.GetChildPosition(childIndex), Me.GetChildIndex(childIndex))
                End If
            End If
        ElseIf IsListStructureType(child) Then
            If childIndex = 0 Then
                _writer.WriteLine("                Dim listNode = GetRedAtZero({0})", ChildVarName(child))
            Else
                _writer.WriteLine("                Dim listNode = GetRed({0}, {1})", ChildVarName(child), childIndex)
            End If
            _writer.WriteLine("                Return new {0}(listNode)", ChildPropertyTypeRef(nodeStructure, child, False))
        Else
            Dim type = ChildPropertyTypeRef(nodeStructure, child)
            Dim baseType = ChildPropertyTypeRef(nodeStructure, child, denyOverride:=True)

            If childIndex = 0 Then
                If type = baseType Then
                    _writer.WriteLine("                Return GetRedAtZero({0})", ChildVarName(child))
                Else
                    _writer.WriteLine("                Return DirectCast(GetRedAtZero({0}), {1})", ChildVarName(child), type)
                End If
            Else
                If type = baseType Then
                    _writer.WriteLine("                Return GetRed({0}, {1})", ChildVarName(child), childIndex)
                Else
                    _writer.WriteLine("                Return DirectCast(GetRed({0}, {1}), {2})", ChildVarName(child), childIndex, type)
                End If
            End If

        End If
    End Sub

    Private Function GetChildPosition(i As Integer) As String
        If (i = 0) Then
            Return "Me.Position"
        Else
            Return "Me.GetChildPosition(" & i & ")"
        End If
    End Function

    Private Function GetChildIndex(i As Integer) As String
        If (i = 0) Then
            Return "0"
        Else
            Return "Me.GetChildIndex(" & i & ")"
        End If
    End Function

    ' Generate a public property for a child
    Private Sub GenerateNodeWithChildProperty(withChild As ParseNodeChild, childIndex As Integer, nodeStructure As ParseNodeStructure)
        'Dim isOverride As Boolean = withChild.ContainingStructure IsNot nodeStructure
        Dim hasPrevious As Boolean

        If True Then ' withChild.GenerateWith Then

            Dim isAbstract As Boolean = _parseTree.IsAbstract(nodeStructure)

            If isAbstract Then
                If nodeStructure.Children.Contains(withChild) Then
                    ' +WriteLine($"    public {node.Name} With{field.Name}({fieldType} {CamelCase(field.Name)}) => With{field.Name}Core({CamelCase(field.Name)});");
                    '+ WriteLine($"    internal abstract {node.Name} With{field.Name}Core({fieldType} {CamelCase(field.Name)});");
                    GenerateWithXmlComment(_writer, withChild, 8)

                    _writer.WriteLine($"        Public Function {ChildWithFunctionName(withChild)}({ChildParamName(withChild)} As {ChildPropertyTypeRef(nodeStructure, withChild)}) As {StructureTypeName(nodeStructure)}")
                    _writer.WriteLine($"            Return {ChildWithFunctionName(withChild)}Core({ChildParamName(withChild)})")
                    _writer.WriteLine($"        End Function")

                    _writer.WriteLine($"        Friend MustOverride Function {ChildWithFunctionName(withChild)}Core({ChildParamName(withChild)} As {ChildPropertyTypeRef(nodeStructure, withChild)}) As {StructureTypeName(nodeStructure)}")
                End If
            Else
                If Not nodeStructure.Children.Contains(withChild) AndAlso
                       nodeStructure.ParentStructure.Children.Contains(withChild) Then

                    _writer.WriteLine($"        Friend Overrides Function {ChildWithFunctionName(withChild)}Core({ChildParamName(withChild)} As {ChildPropertyTypeRef(nodeStructure, withChild)}) As {StructureTypeName(nodeStructure.ParentStructure)}")
                    _writer.WriteLine($"            Return {ChildWithFunctionName(withChild)}({ChildParamName(withChild)})")
                    _writer.WriteLine($"        End Function")
                    _writer.WriteLine()
                End If

                ' XML comment
                GenerateWithXmlComment(_writer, withChild, 8)
                _writer.WriteLine("        Public Shadows Function {0}({1} as {2}) As {3}", ChildWithFunctionName(withChild), ChildParamName(withChild), ChildPropertyTypeRef(nodeStructure, withChild), StructureTypeName(nodeStructure))
                _writer.Write("            return Update(")

                If nodeStructure.NodeKinds.Count >= 2 Then
                    _writer.Write("Me.Kind")
                    hasPrevious = True
                End If

                Dim allFields = GetAllFieldsOfStructure(nodeStructure)
                If allFields.Count > 0 Then
                    For i = 0 To allFields.Count - 1
                        If hasPrevious Then
                            _writer.Write(", ")
                        End If
                        _writer.Write("{0}", FieldParamName(allFields(i)))
                        hasPrevious = True
                    Next
                End If

                For Each child In GetAllChildrenOfStructure(nodeStructure)
                    If hasPrevious Then
                        _writer.Write(", ")
                    End If
                    If child Is withChild Then
                        _writer.Write("{0}", ChildParamName(child))
                    Else
                        _writer.Write("Me.{0}", UpperFirstCharacter(child.Name))
                    End If
                    hasPrevious = True
                Next

                _writer.WriteLine(")")
                _writer.WriteLine("        End Function")
            End If

            _writer.WriteLine("")
        End If
    End Sub

    ' Generate two public properties for a child that is a separated list
    Private Sub GenerateNodeSeparatedListChildProperty(node As ParseNodeStructure, child As ParseNodeChild, childIndex As Integer, isOverride As Boolean)
        ' XML comment
        GenerateXmlComment(_writer, child, 8)

        _writer.WriteLine("        Public {2} ReadOnly Property {0} As {1}", ChildPropertyName(child), ChildPropertyTypeRef(node, child), GetModifiers(child.ContainingStructure, isOverride, child.Name))
        _writer.WriteLine("            Get")
        If childIndex = 0 Then
            _writer.WriteLine("                Dim listNode = GetRedAtZero({0})", ChildVarName(child), childIndex)
        Else
            _writer.WriteLine("                Dim listNode = GetRed({0}, {1})", ChildVarName(child), childIndex)
        End If
        _writer.WriteLine("                If listNode IsNot Nothing")
        _writer.WriteLine("                    Return new {0}(listNode, {1})", ChildPropertyTypeRef(node, child), GetChildIndex(childIndex))
        _writer.WriteLine("                End If")
        _writer.WriteLine("                Return Nothing")
        _writer.WriteLine("            End Get")
        _writer.WriteLine("        End Property")
        _writer.WriteLine("")
    End Sub

    Private Sub GenerateAccept(nodeStructure As ParseNodeStructure)
        If _parseTree.IsAbstract(nodeStructure) Then
            Return
        End If

        _writer.WriteLine("        Public {0} Function Accept(Of TResult)(ByVal visitor As {1}(Of TResult)) As TResult", If(IsRoot(nodeStructure), "Overridable", "Overrides"), _parseTree.VisitorName)
        _writer.WriteLine("            Return visitor.{0}(Me)", VisitorMethodName(nodeStructure))
        _writer.WriteLine("        End Function")
        _writer.WriteLine()

        _writer.WriteLine("        Public {0} Sub Accept(ByVal visitor As {1})", If(IsRoot(nodeStructure), "Overridable", "Overrides"), _parseTree.VisitorName)
        _writer.WriteLine("            visitor.{0}(Me)", VisitorMethodName(nodeStructure))
        _writer.WriteLine("        End Sub")
        _writer.WriteLine()
    End Sub

    ' Generate Update method . But only for non terminals
    Private Sub GenerateUpdate(nodeStructure As ParseNodeStructure)

        If _parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken OrElse nodeStructure.IsTrivia Then
            Return
        End If

        Dim structureName = StructureTypeName(nodeStructure)
        Dim factory = FactoryName(nodeStructure)
        Dim needComma = False
        Dim isMultiKind As Boolean = (nodeStructure.NodeKinds.Count >= 2)

        _writer.WriteLine()

        GenerateSummaryXmlComment(_writer, String.Format("Returns a copy of this with the specified changes. Returns this instance if there are no actual changes.", nodeStructure.Name))

        If isMultiKind Then
            GenerateParameterXmlComment(_writer, "kind", String.Format("The new kind.", nodeStructure.Name))
        End If

        For Each child In GetAllChildrenOfStructure(nodeStructure)
            GenerateParameterXmlComment(_writer, LowerFirstCharacter(OptionalChildName(child)), String.Format("The value for the {0} property.", child.Name))
        Next

        _writer.Write("        Public ")
        If nodeStructure.ParentStructure IsNot Nothing AndAlso Not nodeStructure.ParentStructure.Abstract Then
            _writer.Write("Shadows ")
        End If

        _writer.Write("Function Update(")

        If isMultiKind Then
            _writer.Write("kind As SyntaxKind")
            needComma = True
        End If

        For Each child In GetAllChildrenOfStructure(nodeStructure)
            If needComma Then
                _writer.Write(", ")
            End If
            GenerateFactoryChildParameter(nodeStructure, child, Nothing)
            needComma = True
        Next

        _writer.WriteLine(") As {0}", structureName)

        needComma = False
        _writer.Write("            If ")

        If isMultiKind Then
            _writer.Write("kind <> Me.Kind")
            needComma = True
        End If

        For Each child In GetAllChildrenOfStructure(nodeStructure)
            If needComma Then
                _writer.Write(" OrElse ")
            End If

            If child.IsList Then
                _writer.Write("{0} <> Me.{1}", ChildParamName(child), ChildPropertyName(child))
            ElseIf KindTypeStructure(child.ChildKind).IsToken Then
                _writer.Write("{0} <> Me.{1}", ChildParamName(child), ChildPropertyName(child))
            Else
                _writer.Write("{0} IsNot Me.{1}", ChildParamName(child), ChildPropertyName(child))
            End If
            needComma = True
        Next
        _writer.WriteLine(" Then")

        needComma = False

        _writer.Write("                Dim newNode = SyntaxFactory.{0}(", factory)
        If isMultiKind Then
            _writer.Write("kind, ")
        End If
        For Each child In GetAllChildrenOfStructure(nodeStructure)
            If needComma Then
                _writer.Write(", ")
            End If
            _writer.Write("{0}", ChildParamName(child))
            needComma = True
        Next
        _writer.WriteLine(")")

        _writer.WriteLine("                Dim annotations = Me.GetAnnotations()")
        _writer.WriteLine("                If annotations IsNot Nothing AndAlso annotations.Length > 0")
        _writer.WriteLine("                    return newNode.WithAnnotations(annotations)")
        _writer.WriteLine("                End If")
        _writer.WriteLine("                Return newNode")
        _writer.WriteLine("            End If")
        _writer.WriteLine("            Return Me")
        _writer.WriteLine("        End Function")
        _writer.WriteLine()
    End Sub

    ' Generate special methods and properties for the root node. These only appear in the root node.
    Private Sub GenerateRootNodeSpecialMethods(nodeStructure As ParseNodeStructure)
        _writer.WriteLine()
    End Sub

    ' Generate GetChild, GetChildrenCount so members can be accessed by index
    Private Sub GenerateGetSlot(nodeStructure As ParseNodeStructure, allChildren As List(Of ParseNodeChild))
        If _parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken Then
            Return
        End If

        Dim childrenCount = allChildren.Count
        If childrenCount = 0 Then
            Return
        End If

        _writer.WriteLine("        Friend Overrides Function GetSlot(i as Integer) as IBaseSyntaxNodeExt")

        ' Create the property accessor for each of the children
        Dim children = allChildren

        If childrenCount <> 1 Then
            _writer.WriteLine("            Select case i")

            For i = 0 To childrenCount - 1
                Dim child = children(i)
                _writer.WriteLine("                Case {0}", i)
                If KindTypeStructure(child.ChildKind).IsToken Then
                    _writer.WriteLine("                    Return DirectCast(Me.Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}).{1}", StructureTypeName(nodeStructure), ChildVarName(child))
                ElseIf child.IsList Then
                    If (i = 0) Then
                        _writer.WriteLine("                    Return GetRedAtZero({0})", ChildVarName(child))
                    Else
                        _writer.WriteLine("                    Return GetRed({0}, {1})", ChildVarName(child), i)
                    End If
                Else
                    _writer.WriteLine("                    Return Me.{0}", ChildPropertyName(child))
                End If
            Next
            _writer.WriteLine("                Case Else")
            _writer.WriteLine("                     Debug.Assert(false, ""child index out of range"")")
            _writer.WriteLine("                     Return Nothing")
            _writer.WriteLine("            End Select")
        Else
            _writer.WriteLine("            If i = 0 Then")
            Dim child = children(0)
            If KindTypeStructure(child.ChildKind).IsToken Then
                _writer.WriteLine("                Return DirectCast(Me.Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}).{1}", StructureTypeName(nodeStructure), ChildVarName(child))
            ElseIf child.IsList Then
                _writer.WriteLine("                Return GetRedAtZero({0})", ChildVarName(child))
            Else
                _writer.WriteLine("                Return Me.{0}", ChildPropertyName(child))
            End If

            _writer.WriteLine("            Else")
            _writer.WriteLine("                Debug.Assert(false, ""child index out of range"")")
            _writer.WriteLine("                Return Nothing")
            _writer.WriteLine("            End If")
        End If

        _writer.WriteLine("        End Function")
        _writer.WriteLine()
    End Sub

    ' Generate GetChild, GetChildrenCount so members can be accessed by index
    Private Sub GenerateGetNodeSlot(nodeStructure As ParseNodeStructure, allChildren As List(Of ParseNodeChild))
        If _parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken Then
            Return
        End If

        Dim childrenCount = allChildren.Count
        If childrenCount = 0 Then
            Return
        End If

        _writer.WriteLine("        Friend Overrides Function GetNodeSlot(i as Integer) as SyntaxNode")

        ' Create the property accessor for each of the children
        Dim children = allChildren

        If childrenCount <> 1 Then
            _writer.WriteLine("            Select case i")

            For i = 0 To childrenCount - 1
                Dim child = children(i)
                If KindTypeStructure(child.ChildKind).IsToken Then
                    Continue For
                End If

                _writer.WriteLine("                Case {0}", i)
                If child.IsList Then
                    If i = 0 Then
                        _writer.WriteLine("                    Return GetRedAtZero({0})", ChildVarName(child))
                    Else
                        _writer.WriteLine("                    Return GetRed({0}, {1})", ChildVarName(child), i)
                    End If
                Else
                    _writer.WriteLine("                    Return Me.{0}", ChildPropertyName(child))
                End If

            Next
            _writer.WriteLine("                Case Else")
            _writer.WriteLine("                     Return Nothing")
            _writer.WriteLine("            End Select")

        ElseIf Not KindTypeStructure(children(0).ChildKind).IsToken Then
            _writer.WriteLine("            If i = 0 Then")
            Dim child = children(0)
            If child.IsList Then
                _writer.WriteLine("               Return GetRedAtZero({0})", ChildVarName(child))
            Else
                _writer.WriteLine("                Return Me.{0}", ChildPropertyName(child))
            End If

            _writer.WriteLine("            Else")
            _writer.WriteLine("                Return Nothing")
            _writer.WriteLine("            End If")

        Else
            _writer.WriteLine("                Return Nothing")
        End If

        _writer.WriteLine("        End Function")
        _writer.WriteLine()
    End Sub

    ' Generate GetChild, GetChildrenCount so members can be accessed by index
    Private Sub GenerateGetCachedSlot(nodeStructure As ParseNodeStructure, allChildren As List(Of ParseNodeChild))
        If _parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken Then
            Return
        End If

        Dim childrenCount = allChildren.Count
        If childrenCount = 0 Then
            Return
        End If

        _writer.WriteLine("        Friend Overrides Function GetCachedSlot(i as Integer) as SyntaxNode")

        ' Create the property accessor for each of the children
        Dim children = allChildren

        If childrenCount <> 1 Then
            _writer.WriteLine("            Select case i")

            For i = 0 To childrenCount - 1
                Dim child = children(i)
                If Not KindTypeStructure(child.ChildKind).IsToken Then
                    _writer.WriteLine("                Case {0}", i)
                    _writer.WriteLine("                    Return Me.{0}", ChildVarName(child), i)
                End If
            Next
            _writer.WriteLine("                Case Else")
            _writer.WriteLine("                     Return Nothing")
            _writer.WriteLine("            End Select")
        Else
            _writer.WriteLine("            If i = 0 Then")
            Dim child = children(0)
            If KindTypeStructure(child.ChildKind).IsToken Then
                _writer.WriteLine("                Return Nothing")
            Else
                _writer.WriteLine("                Return Me.{0}", ChildVarName(child))
            End If

            _writer.WriteLine("            Else")
            _writer.WriteLine("                Return Nothing")
            _writer.WriteLine("            End If")
        End If

        _writer.WriteLine("        End Function")
        _writer.WriteLine()
    End Sub


    ' Generate GetChild, GetChildrenCount so members can be accessed by index
    Private Sub GenerateGetSlotCount(nodeStructure As ParseNodeStructure, allChildren As List(Of ParseNodeChild))
        If _parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken Then
            Return
        End If

        Dim childrenCount = allChildren.Count
        If childrenCount = 0 Then
            Return
        End If

        _writer.WriteLine("        Friend Overrides ReadOnly Property SlotCount() As Integer")
        _writer.WriteLine("            Get")
        _writer.WriteLine("                Return {0}", childrenCount)
        _writer.WriteLine("            End Get")
        _writer.WriteLine("        End Property")

        _writer.WriteLine()
    End Sub

    ' Generate the Visitor class definition
    Private Sub GenerateVisitorClass(withResult As Boolean)
        _writer.WriteLine("    Public MustInherit Class {0}{1}", Ident(_parseTree.VisitorName), If(withResult, "(Of TResult)", ""))

        For Each nodeStructure In _parseTree.NodeStructures.Values
            If Not nodeStructure.Abstract Then
                GenerateVisitorMethod(nodeStructure, withResult)
            End If
        Next

        _writer.WriteLine("    End Class")
        _writer.WriteLine()
    End Sub

    ' Generate a method in the Visitor class
    Private Sub GenerateVisitorMethod(nodeStructure As ParseNodeStructure, withResult As Boolean)
        If nodeStructure.IsToken OrElse nodeStructure.IsTrivia Then
            Return
        End If
        _writer.WriteLine("        Public Overridable {2} {0}(ByVal node As {1}){3}",
                          VisitorMethodName(nodeStructure),
                          StructureTypeName(nodeStructure),
                          If(withResult, "Function", "Sub"),
                          If(withResult, " As TResult", ""))

        _writer.WriteLine("            {0}Me.DefaultVisit(node){1}", If(withResult, "Return ", ""), If(withResult, "", ": Return"))
        _writer.WriteLine("        End {0}", If(withResult, "Function", "Sub"))
    End Sub



    ' Generate the RewriteVisitor class definition
    Private Sub GenerateRewriteVisitorClass()
        _writer.WriteLine("    Public MustInherit Class {0}", Ident(_parseTree.RewriteVisitorName))
        _writer.WriteLine("        Inherits {0}(Of SyntaxNode)", Ident(_parseTree.VisitorName))
        _writer.WriteLine()

        For Each nodeStructure In _parseTree.NodeStructures.Values
            GenerateRewriteVisitorMethod(nodeStructure)
        Next

        _writer.WriteLine("    End Class")
        _writer.WriteLine()
    End Sub

    ' Generate a method in the RewriteVisitor class
    Private Sub GenerateRewriteVisitorMethod(nodeStructure As ParseNodeStructure)
        If nodeStructure.IsToken OrElse nodeStructure.IsTrivia Then
            Return
        End If

        If nodeStructure.Abstract Then
            ' do nothing for abstract nodes
            Return
        End If

        Dim methodName = VisitorMethodName(nodeStructure)
        Dim structureName = StructureTypeName(nodeStructure)

        _writer.WriteLine("        Public Overrides Function {0}(ByVal node As {1}) As SyntaxNode",
                  methodName,
                  structureName)

        ' non-abstract non-terminals need to rewrite their children and recreate as needed.
        Dim allFields = GetAllFieldsOfStructure(nodeStructure)
        Dim allChildren = GetAllChildrenOfStructure(nodeStructure)

        ' create anyChanges variable
        _writer.WriteLine("            Dim anyChanges As Boolean = False")
        _writer.WriteLine()

        ' visit all children
        For i = 0 To allChildren.Count - 1
            Dim child = allChildren(i)
            If child.IsList Then
                _writer.WriteLine("            Dim {0} = VisitList(node.{1})", ChildNewVarName(child), ChildPropertyName(child))
                If KindTypeStructure(child.ChildKind).IsToken Then
                    _writer.WriteLine("            If node.{0}.Node IsNot {1}.Node Then anyChanges = True", ChildPropertyName(child), ChildNewVarName(child))
                Else
                    _writer.WriteLine("            If node.{0} IsNot {1}.Node Then anyChanges = True", ChildVarName(child), ChildNewVarName(child))
                End If

            ElseIf KindTypeStructure(child.ChildKind).IsToken Then
                _writer.WriteLine("            Dim {0} = DirectCast(VisitToken(node.{2}).Node, {3})" + vbCrLf +
                                  "            If node.{2}.Node IsNot {0} Then anyChanges = True",
                                  ChildNewVarName(child), BaseTypeReference(child), ChildPropertyName(child), ChildConstructorTypeRef(child))
            Else
                _writer.WriteLine("            Dim {0} = DirectCast(Visit(node.{2}), {1})" + vbCrLf +
                                  "            If node.{2} IsNot {0} Then anyChanges = True",
                                  ChildNewVarName(child), ChildPropertyTypeRef(nodeStructure, child), ChildPropertyName(child))
            End If
        Next
        _writer.WriteLine()

        ' check if any changes.
        _writer.WriteLine("            If anyChanges Then")

        _writer.Write("                Return New {0}(node.Kind", StructureTypeName(nodeStructure))

        _writer.Write(", node.Green.GetDiagnostics, node.Green.GetAnnotations")

        For Each field In allFields
            _writer.Write(", node.{0}", FieldPropertyName(field))
        Next
        For Each child In allChildren
            If child.IsList Then
                If KindTypeStructure(child.ChildKind).IsToken Then
                    _writer.Write(", {0}.Node", ChildNewVarName(child))
                Else
                    _writer.Write(", {0}.Node", ChildNewVarName(child))
                End If
            ElseIf KindTypeStructure(child.ChildKind).IsToken Then
                _writer.Write(", {0}", ChildNewVarName(child), ChildFieldTypeRef(child))
            Else
                _writer.Write(", {0}", ChildNewVarName(child))
            End If
        Next
        _writer.WriteLine(")")

        _writer.WriteLine("            Else")
        _writer.WriteLine("                Return node")
        _writer.WriteLine("            End If")

        _writer.WriteLine("        End Function")
        _writer.WriteLine()
    End Sub


End Class
