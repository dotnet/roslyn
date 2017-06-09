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
            _writer.WriteLine(
$"
Namespace {Ident(_parseTree.NamespaceName)}
")
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
            _writer.WriteLine(
$"
Namespace {Ident(_parseTree.NamespaceName & ".Syntax")}
")
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

        _writer.Write($"        {Ident(kind.Name)}")

        For i = 1 To 35 - Ident(kind.Name).Length
            _writer.Write(" ")
        Next

        _writer.Write($"' {StructureTypeName(kind.NodeStructure)}")

        ' Write the list of parent classes also.
        Dim struct = kind.NodeStructure.ParentStructure
        Do While struct IsNot Nothing AndAlso Not IsRoot(struct)
            _writer.Write(" : {0}", StructureTypeName(struct))
            struct = struct.ParentStructure
        Loop

        _writer.WriteLine()
    End Sub


    Private Sub GenerateEnumTypes()
        For Each enumeration In _parseTree.Enumerations.Values
            GenerateEnumerationType(enumeration)
        Next
    End Sub


    Private Sub GenerateRedFactory()
        'Private Shared Function CreateRed(ByVal green As GreenNode, ByVal parent As SyntaxNode, ByVal startLocation As Integer) As SyntaxNode
        '    Requires(green IsNot Nothing)
        '    Requires(startLocation >= 0)
        '    
        '       Select Case green.Kind
        Dim name = StructureTypeName(_parseTree.RootStructure)
        _writer.WriteLine(
 $"Partial Public MustInherit Class {name}
    Friend Shared Function CreateRed(ByVal green As Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{name}, ByVal parent As {name}, ByVal startLocation As Integer) As {name}
        Debug.Assert(green IsNot Nothing)
        Debug.Assert(startLocation >= 0)

        Select Case green.Kind")

        For Each nodeStructure In _parseTree.NodeStructures.Values
            GenerateFactoryCase(nodeStructure)
        Next

        _writer.WriteLine(
"            Case Else
        Throw New InvalidOperationException(""unexpected node kind"")
        End Select
    End Function
End Class")

        '       Case Else
        '        Throw New InvalidOperationException("unexpected node kind")
        '            End Select
        '    End Sub
        'End Class
    End Sub

    Private Sub GenerateFactoryCase(nodeStructure As ParseNodeStructure)
        If nodeStructure.Abstract Then Return

        Dim kinds = nodeStructure.NodeKinds
        Dim first As Boolean = True
        For Each kind In kinds
            If first Then
                _writer.Write(
$"            Case SyntaxKind.{kind.Name}")
                first = False
            Else
                _writer.Write(
$",
                 SyntaxKind.{kind.Name}")

            End If
        Next
        _writer.WriteLine(
$"
                Return New {StructureTypeName(nodeStructure)}(green, parent, startLocation)
")
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

        If enumeration.IsFlags Then _writer.WriteLine("    <Flags()>")
        _writer.WriteLine($"    Public Enum {EnumerationTypeName(enumeration)}")

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

        _writer.Write($"        {Ident(enumerator.Name)}")
        If generateValue Then _writer.Write($" = {GetConstantValue(enumerator.Value)}")
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
        With _writer
            ' Class name
            .Write("    ")
            If (nodeStructure.PartialClass) Then .Write("Partial ")

            Dim stName = StructureTypeName(nodeStructure)

            If _parseTree.IsAbstract(nodeStructure) Then
                .WriteLine($"Public MustInherit Class {stName}")
            ElseIf Not nodeStructure.HasDerivedStructure Then
                .WriteLine($"Public NotInheritable Class {stName}")
            Else
                .WriteLine($"Public Class {stName}")
            End If

            ' Base class
            If Not IsRoot(nodeStructure) Then .WriteLine($"        Inherits {StructureTypeName(nodeStructure.ParentStructure)}")
            _writer.WriteLine()

            'Create members
            GenerateNodeStructureMembers(nodeStructure)

            ' Create the constructor.
            GenerateNodeStructureConstructor(nodeStructure)

            ' Create the IsTerminal property
            If nodeStructure.ParentStructure IsNot Nothing AndAlso nodeStructure.IsTerminal <> nodeStructure.ParentStructure.IsTerminal Then
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
            If Not String.IsNullOrEmpty(_parseTree.VisitorName) Then GenerateAccept(nodeStructure)

            GenerateUpdate(nodeStructure)

            ' Special methods for the root node.
            If IsRoot(nodeStructure) Then GenerateRootNodeSpecialMethods(nodeStructure)

            ' End the class
            .WriteLine(
"    End Class
")
        End With
    End Sub

    Private Sub GenerateChildListAccessor(nodeStructure As ParseNodeStructure, child As ParseNodeChild)
        If Not _parseTree.IsAbstract(nodeStructure) Then
            Dim kindType = KindTypeStructure(child.ChildKind)
            Dim itemType = If(kindType.IsToken, "SyntaxToken", kindType.Name)
            Dim Name = child.Name
            _writer.WriteLine(
 $"        Public Shadows Function Add{Name}(ParamArray items As {itemType}()) As {nodeStructure.Name}
            Return Me.With{Name}(Me.{Name}.AddRange(items))
        End Function
")
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
        If Not _parseTree.IsAbstract(nodeStructure) Then
            Dim childStructure = KindTypeStructure(child.ChildKind)
            If Not _parseTree.IsAbstract(childStructure) Then

                Dim nestedList = GetNestedList(nodeStructure, child)
                Dim nestedListStructure = KindTypeStructure(nestedList.ChildKind)
                Dim itemType = If(nestedListStructure.IsToken, "SyntaxToken", nestedListStructure.Name)
                Dim Name = child.Name
                Dim ListName = nestedList.Name
                _writer.WriteLine(
$"        Public Shadows Function Add{Name}{ListName}(ParamArray items As {itemType}()) As {nodeStructure.Name}
            Dim _child = If (Me.{Name} IsNot Nothing, Me.{Name}, SyntaxFactory.{FactoryName(childStructure)}())
            Return Me.With{Name}(_child.Add{ListName}(items))
        End Function
")
            End If
        End If
    End Sub

    ' Generate IsTerminal property.
    Private Sub GenerateIsTerminal(nodeStructure As ParseNodeStructure)
        _writer.WriteLine(
$"        Public Overrides ReadOnly Property IsTerminal As Boolean
            Get
                Return {If(nodeStructure.IsTerminal, "True", "False")}
            End Get
        End Property
")
    End Sub

    Private Sub GenerateNodeStructureMembers(nodeStructure As ParseNodeStructure)
        Dim children = nodeStructure.Children
        For Each child In children
            If Not KindTypeStructure(child.ChildKind).IsToken Then
                _writer.WriteLine($"        Friend {ChildVarName(child)} as {ChildPrivateFieldTypeRef(child)}")
            End If
        Next

        _writer.WriteLine()
    End Sub

    ' Generate constructor for a node structure
    Private Sub GenerateNodeStructureConstructor(nodeStructure As ParseNodeStructure)
        With _writer
            If Not IsRoot(nodeStructure) AndAlso
            nodeStructure.Name <> "StructuredTriviaSyntax" Then

                _writer.WriteLine(
"        Friend Sub New(ByVal green As GreenNode, ByVal parent as SyntaxNode, ByVal startLocation As Integer)
            MyBase.New(green, parent, startLocation)
            Debug.Assert(green IsNot Nothing)
            Debug.Assert(startLocation >= 0)
        End Sub
")
            End If

            Dim allFields = GetAllFieldsOfStructure(nodeStructure)
            If Not _parseTree.IsAbstract(nodeStructure) AndAlso StructureTypeName(nodeStructure) <> "IdentifierSyntax" Then
                .Write("        Friend Sub New(")

                ' Generate each of the field parameters
                .Write($"ByVal kind As { NodeKindType()}, ByVal errors as DiagnosticInfo(), ByVal annotations as SyntaxAnnotation()")

                If nodeStructure.IsTerminal Then .Write(", text as String") ' terminals have a text


                If nodeStructure.IsToken Then .Write(", precedingTrivia As SyntaxTriviaList, followingTrivia As SyntaxTriviaList", StructureTypeName(_parseTree.RootTrivia))                    ' tokens have trivia


                For Each field In allFields
                    .Write(", ")
                    GenerateNodeStructureFieldParameter(field)
                Next

                For Each child In GetAllChildrenOfStructure(nodeStructure)
                    .Write(", ")
                    GenerateNodeStructureChildParameter(child, Nothing)
                Next
                .WriteLine(")")

                ' Generate call to create new builder, and pass result to my private constructor.

                .Write($"            Me.New(New Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{StructureTypeName(nodeStructure)}(")

                ' Generate each of the field parameters
                .Write("kind", NodeKindType())

                .Write(", errors, annotations")

                If nodeStructure.IsTerminal Then .Write(", text")                ' nonterminals have text.

                If nodeStructure.IsToken AndAlso Not nodeStructure.IsTrivia Then .Write(", precedingTrivia.Node, followingTrivia.Node") ' tokens havetrivia, but only if they are not trivia.

                If allFields.Count > 0 Then
                    For i = 0 To allFields.Count - 1
                        _writer.Write(", {0}", FieldParamName(allFields(i)))
                    Next
                End If


                For Each child In GetAllChildrenOfStructure(nodeStructure)

                    Dim ParamName = ChildParamName(child)
                    If KindTypeStructure(child.ChildKind).IsToken Then
                        .Write($", {ParamName}")

                    ElseIf child.IsList Then
                        .Write($", if({ParamName} IsNot Nothing, {ParamName}.Green, Nothing)")

                    Else
                        If child.IsOptional Then .Write($", if({ParamName} IsNot Nothing ") ' optional normal child.


                        ' non-optional normal child.
                        .Write($", DirectCast({ParamName}.Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{BaseTypeReference(child)})")

                        If child.IsOptional Then .Write(", Nothing) ")     ' optional normal child.
                    End If
                Next

                .WriteLine("), Nothing, 0)")

                ' Generate contracts
                If nodeStructure.IsTerminal Then .WriteLine("            Debug.Assert(text IsNot Nothing)") ' tokens and trivia have a text

                ' Generate End Sub
                .WriteLine(
"        End Sub
")

            End If
        End With
    End Sub

    ' Generate a parameter corresponding to a node structure field
    Private Sub GenerateNodeStructureFieldParameter(field As ParseNodeField, Optional conflictName As String = Nothing)
        _writer.Write($"{FieldParamName(field, conflictName)} As {FieldTypeRef(field)}")
    End Sub

    ' Generate a parameter corresponding to a node structure child
    Private Sub GenerateNodeStructureChildParameter(child As ParseNodeChild, Optional conflictName As String = Nothing)
        _writer.Write($"{ChildParamName(child, conflictName)} As {ChildConstructorTypeRef(child)}")
    End Sub

    ' Generate a parameter corresponding to a node structure child
    Private Sub GenerateFactoryChildParameter(node As ParseNodeStructure, child As ParseNodeChild, Optional conflictName As String = Nothing)
        _writer.Write($"{ChildParamName(child, conflictName)} As {ChildFactoryTypeRef(node, child, False, False)}")
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

        _writer.WriteLine(
 $"        Public {GetModifiers(field.ContainingStructure, False, field.Name)} ReadOnly Property {FieldPropertyName(field)} As {FieldTypeRef(field)}
             Get
                Return DirectCast(Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{TypeName}).{FieldPropertyName(field)}
            End Get
        End Property
")
    End Sub

    ' Generate a public property for a child

    Private Sub GenerateNodeChildProperty(nodeStructure As ParseNodeStructure, child As ParseNodeChild, childIndex As Integer, isOverride As Boolean)

        ' XML comment
        GenerateXmlComment(_writer, child, 8)

        If nodeStructure.HasDerivedStructure Then
            WriteOut_HasDerivedStructure(nodeStructure, child, childIndex, isOverride)
        ElseIf isOverride Then
            WriteOut_Overrides(nodeStructure, child, childIndex, isOverride)
        Else
            WriteOut_ChildProperty(nodeStructure, child, childIndex, isOverride)
        End If
    End Sub

    Private Sub WriteOut_ChildProperty(nodeStructure As ParseNodeStructure, child As ParseNodeChild, childIndex As Integer, isOverride As Boolean)

        _writer.WriteLine("        Public {0} ReadOnly Property {1} As {2}", GetModifiers(child.ContainingStructure, isOverride, child.Name), ChildPropertyName(child), ChildPropertyTypeRef(nodeStructure, child))
        _writer.WriteLine("            Get")
        Me.GenerateNodeChildPropertyRedAccessLogic(nodeStructure, child, childIndex, isOverride)
        _writer.WriteLine("            End Get")
        _writer.WriteLine("        End Property")
        _writer.WriteLine("")
    End Sub

    Private Sub WriteOut_Overrides(nodeStructure As ParseNodeStructure, child As ParseNodeChild, childIndex As Integer, isOverride As Boolean)
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
    End Sub

    Private Sub WriteOut_HasDerivedStructure(nodeStructure As ParseNodeStructure, child As ParseNodeChild, childIndex As Integer, isOverride As Boolean)
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
    End Sub

    Private Sub GenerateNodeChildPropertyRedAccessLogic(nodeStructure As ParseNodeStructure, child As ParseNodeChild, childIndex As Integer, isOverride As Boolean)
        If KindTypeStructure(child.ChildKind).IsToken Then
            If child.IsList Then
                WriteOut_IsList(nodeStructure, child, childIndex)
            Else
                If child.IsOptional Then
                    WriteOut_IsList(nodeStructure, child, childIndex)
                Else
                    _writer.WriteLine("                return New SyntaxToken(Me, DirectCast(Me.Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}).{1}, {2}, {3})", StructureTypeName(nodeStructure), ChildVarName(child), Me.GetChildPosition(childIndex), Me.GetChildIndex(childIndex))
                End If
            End If
        ElseIf IsListStructureType(child) Then
            If childIndex = 0 Then
                _writer.WriteLine("                Dim listNode = GetRedAtZero({0})", ChildVarName(child))
            Else
                _writer.WriteLine("                Dim listNode = GetRed({0}, {1})", ChildVarName(child), childIndex)
            End If
            _writer.WriteLine("                Return New {0}(listNode)", ChildPropertyTypeRef(nodeStructure, child, False))
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

    Private Sub WriteOut_IsList(nodeStructure As ParseNodeStructure, child As ParseNodeChild, childIndex As Integer)
        With _writer
            .WriteLine("                Dim slot = DirectCast(Me.Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}).{1}", StructureTypeName(nodeStructure), ChildVarName(child))
            .WriteLine("                If slot IsNot Nothing")
            .WriteLine("                    return New SyntaxTokenList(Me, slot, {0}, {1})", Me.GetChildPosition(childIndex), Me.GetChildIndex(childIndex))
            .WriteLine("                End If")
            .WriteLine("                Return Nothing")
        End With
    End Sub

    Private Function GetChildPosition(i As Integer) As String
        If (i = 0) Then Return "Me.Position"
        Return "Me.GetChildPosition(" & i & ")"
    End Function

    Private Function GetChildIndex(i As Integer) As String
        If (i = 0) Then Return "0"
        Return "Me.GetChildIndex(" & i & ")"
    End Function

    ' Generate a public property for a child
    Private Sub GenerateNodeWithChildProperty(withChild As ParseNodeChild, childIndex As Integer, nodeStructure As ParseNodeStructure)
        'Dim isOverride As Boolean = withChild.ContainingStructure IsNot nodeStructure
        Dim hasPrevious As Boolean

        With _writer
            If True Then ' withChild.GenerateWith Then

                Dim isAbstract As Boolean = _parseTree.IsAbstract(nodeStructure)

                If Not isAbstract Then
                    ' XML comment
                    GenerateWithXmlComment(_writer, withChild, 8)
                    .WriteLine("        Public Shadows Function {0}({1} as {2}) As {3}", ChildWithFunctionName(withChild), ChildParamName(withChild), ChildPropertyTypeRef(nodeStructure, withChild), StructureTypeName(nodeStructure))
                    .Write("            return Update(")

                    If nodeStructure.NodeKinds.Count >= 2 Then
                        .Write("Me.Kind")
                        hasPrevious = True
                    End If

                    Dim allFields = GetAllFieldsOfStructure(nodeStructure)
                    If allFields.Count > 0 Then
                        For i = 0 To allFields.Count - 1
                            If hasPrevious Then .Write(", ")

                            .Write(FieldParamName(allFields(i)))
                            hasPrevious = True
                        Next
                    End If

                    For Each child In GetAllChildrenOfStructure(nodeStructure)
                        If hasPrevious Then .Write(", ")
                        If child Is withChild Then
                            .Write(ChildParamName(child))
                        Else
                            .Write($"Me.{UpperFirstCharacter(child.Name)}")
                        End If
                        hasPrevious = True
                    Next

                    .WriteLine(")")
                    .WriteLine("        End Function")
                End If

                .WriteLine()
            End If
        End With
    End Sub

    ' Generate two public properties for a child that is a separated list
    Private Sub GenerateNodeSeparatedListChildProperty(node As ParseNodeStructure, child As ParseNodeChild, childIndex As Integer, isOverride As Boolean)
        ' XML comment
        GenerateXmlComment(_writer, child, 8)
        Dim part0 = ChildPropertyName(child)
        Dim part1 = ChildPropertyTypeRef(node, child)
        Dim part2 = GetModifiers(child.ContainingStructure, isOverride, child.Name)
        With _writer
            .WriteLine(
$"        Public {part2} ReadOnly Property {part0} As {part1}
            Get
                Dim listNode = ")

            If childIndex = 0 Then
                .WriteLine($"GetRedAtZero({ChildVarName(child)})")
            Else
                .WriteLine($"GetRed({ChildVarName(child)}, {childIndex})")
            End If

            .WriteLine(
$"                If listNode IsNot Nothing
                    Return New {ChildPropertyTypeRef(node, child)}(listNode, {GetChildIndex(childIndex)})
                End If
                Return Nothing
            End Get
        End Property
")
        End With
    End Sub

    Private Sub GenerateAccept(nodeStructure As ParseNodeStructure)
        If _parseTree.IsAbstract(nodeStructure) Then Return

        Dim part0 = If(IsRoot(nodeStructure), "Overridable", "Overrides")
        Dim part1 = _parseTree.VisitorName
        Dim part2 = VisitorMethodName(nodeStructure)
        With _writer
            .WriteLine(
$"        Public {part0} Function Accept(Of TResult)(ByVal visitor As {part1}(Of TResult)) As TResult
            Return visitor.{part2}(Me)
        End Function

        Public {part0} Sub Accept(ByVal visitor As {part1})
            visitor.{part2}(Me)
        End Sub
")
        End With
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
            GenerateParameterXmlComment(_writer, "kind", String.Format("The New kind.", nodeStructure.Name))
        End If

        For Each child In GetAllChildrenOfStructure(nodeStructure)
            GenerateParameterXmlComment(_writer, LowerFirstCharacter(OptionalChildName(child)), String.Format("The value for the {0} property.", child.Name))
        Next

        With _writer

            .Write("        Public ")
            If Not nodeStructure.ParentStructure?.Abstract Then .Write("Shadows ")

            _writer.Write("Function Update(")

            If isMultiKind Then .Write("kind As SyntaxKind") : needComma = True

            For Each child In GetAllChildrenOfStructure(nodeStructure)
                If needComma Then .Write(", ")
                GenerateFactoryChildParameter(nodeStructure, child, Nothing)
                needComma = True
            Next

            .WriteLine($") As {structureName}")

            needComma = False
            _writer.Write("            If ")

            If isMultiKind Then .Write("kind <> Me.Kind") : needComma = True

            For Each child In GetAllChildrenOfStructure(nodeStructure)
                If needComma Then .Write(" OrElse ")

                If child.IsList Then
                    .Write($"{ChildParamName(child)} <> Me.{ChildPropertyName(child)}")
                ElseIf KindTypeStructure(child.ChildKind).IsToken Then
                    .Write($"{ChildParamName(child)} <> Me.{ChildPropertyName(child)}")
                Else
                    .Write($"{ChildParamName(child)} IsNot Me.{ChildPropertyName(child)}")
                End If
                needComma = True
            Next
            .WriteLine(" Then")

            needComma = False

            _writer.Write($"                Dim newNode = SyntaxFactory.{factory}(")
            If isMultiKind Then .Write("kind, ")

            For Each child In GetAllChildrenOfStructure(nodeStructure)
                If needComma Then .Write(", ")
                .Write("{0}", ChildParamName(child))
                needComma = True
            Next
            .WriteLine(
")
                Dim annotations = Me.GetAnnotations()
                If annotations IsNot Nothing AndAlso annotations.Length > 0
                    Return newNode.WithAnnotations(annotations)
                End If
                Return newNode
            End If
            Return Me
        End Function
")
        End With
    End Sub

    ' Generate special methods and properties for the root node. These only appear in the root node.
    Private Sub GenerateRootNodeSpecialMethods(nodeStructure As ParseNodeStructure)
        _writer.WriteLine()
    End Sub

    ' Generate GetChild, GetChildrenCount so members can be accessed by index
    Private Sub GenerateGetSlot(nodeStructure As ParseNodeStructure, allChildren As List(Of ParseNodeChild))

        If _parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken Then Return

        Dim childrenCount = allChildren.Count
        If childrenCount = 0 Then Return

        With _writer
            .WriteLine("        Friend Overrides Function GetSlot(i as Integer) as IBaseSyntaxNodeExt")

            ' Create the property accessor for each of the children
            Dim children = allChildren

            If childrenCount <> 1 Then
                .WriteLine("            Select case i")

                For i = 0 To childrenCount - 1
                    Dim child = children(i)
                    .WriteLine("                Case {0}", i)
                    If KindTypeStructure(child.ChildKind).IsToken Then
                        .WriteLine("                    Return DirectCast(Me.Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}).{1}", StructureTypeName(nodeStructure), ChildVarName(child))
                    ElseIf child.IsList Then
                        If (i = 0) Then
                            .WriteLine("                    Return GetRedAtZero({0})", ChildVarName(child))
                        Else
                            .WriteLine("                    Return GetRed({0}, {1})", ChildVarName(child), i)
                        End If
                    Else
                        _writer.WriteLine("                    Return Me.{0}", ChildPropertyName(child))
                    End If
                Next
                .WriteLine("                Case Else")
                .WriteLine("                     Debug.Assert(false, ""child index out of range"")")
                .WriteLine("                     Return Nothing")
                .WriteLine("            End Select")
            Else
                .WriteLine("            If i = 0 Then")
                Dim child = children(0)
                If KindTypeStructure(child.ChildKind).IsToken Then
                    .WriteLine("                Return DirectCast(Me.Green, Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.{0}).{1}", StructureTypeName(nodeStructure), ChildVarName(child))
                ElseIf child.IsList Then
                    .WriteLine("                Return GetRedAtZero({0})", ChildVarName(child))
                Else
                    .WriteLine("                Return Me.{0}", ChildPropertyName(child))
                End If

                .WriteLine("            Else")
                .WriteLine("                Debug.Assert(false, ""child index out of range"")")
                .WriteLine("                Return Nothing")
                .WriteLine("            End If")
            End If

            _writer.WriteLine("        End Function")
            _writer.WriteLine()
        End With
    End Sub

    ' Generate GetChild, GetChildrenCount so members can be accessed by index
    Private Sub GenerateGetNodeSlot(nodeStructure As ParseNodeStructure, allChildren As List(Of ParseNodeChild))
        Dim childrenCount = IsAbstractOrTokenOrZeroChildren(nodeStructure, allChildren)
        If childrenCount.HasValue = False Then Return

        With _writer

            .WriteLine("        Friend Overrides Function GetNodeSlot(i as Integer) as SyntaxNode")

            ' Create the property accessor for each of the children
            Dim children = allChildren
            If childrenCount <> 1 Then
                .WriteLine("            Select case i")

                For i = 0 To childrenCount - 1
                    Dim child = children(i)
                    Dim VarName = ChildVarName(child)
                    If KindTypeStructure(child.ChildKind).IsToken Then Continue For

                    .WriteLine("                Case {0}", i)
                    If child.IsList Then
                        If i = 0 Then
                            .WriteLine($"                    Return GetRedAtZero({VarName})")
                        Else
                            .WriteLine($"                    Return GetRed({VarName}, {i})")
                        End If
                    Else
                        .WriteLine("                    Return Me.{0}", ChildPropertyName(child))
                    End If

                Next
                .WriteLine("                Case Else")
                .WriteLine("                     Return Nothing")
                .WriteLine("            End Select")

            ElseIf Not KindTypeStructure(children(0).ChildKind).IsToken Then
                .WriteLine("            If i = 0 Then")
                Dim child = children(0)
                If child.IsList Then
                    .WriteLine($"               Return GetRedAtZero({ChildVarName(child)})")
                Else
                    .WriteLine($"                Return Me.{ChildPropertyName(child)}")
                End If

                .WriteLine("            Else")
                .WriteLine("                Return Nothing")
                .WriteLine("            End If")

            Else
                .WriteLine("                Return Nothing")
            End If

            .WriteLine("        End Function")
            .WriteLine()
        End With
    End Sub

    Private Function IsAbstractOrTokenOrZeroChildren(nodeStructure As ParseNodeStructure, allChildren As List(Of ParseNodeChild)) As Integer?
        If _parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken Then Return Nothing

        Dim childrenCount = allChildren.Count
        If childrenCount = 0 Then Return Nothing
        Return childrenCount
    End Function

    ' Generate GetChild, GetChildrenCount so members can be accessed by index
    Private Sub GenerateGetCachedSlot(nodeStructure As ParseNodeStructure, allChildren As List(Of ParseNodeChild))
        Dim childrenCount = IsAbstractOrTokenOrZeroChildren(nodeStructure, allChildren)
        If childrenCount.HasValue = False Then Return

        With _writer
            .WriteLine("        Friend Overrides Function GetCachedSlot(i as Integer) as SyntaxNode")

            ' Create the property accessor for each of the children
            Dim children = allChildren

            If childrenCount <> 1 Then
                .WriteLine("            Select case i")

                For i = 0 To childrenCount - 1
                    Dim child = children(i)
                    If Not KindTypeStructure(child.ChildKind).IsToken Then
                        .WriteLine(
$"                Case {i}
                    Return Me.{ChildVarName(child)}")
                    End If
                Next
                .WriteLine(
"                Case Else
                     Return Nothing
            End Select")
            Else
                .WriteLine("            If i = 0 Then")
                Dim child = children(0)
                If KindTypeStructure(child.ChildKind).IsToken Then
                    .WriteLine("                Return Nothing")
                Else
                    .WriteLine($"                Return Me.{ChildVarName(child)}")
                End If

                .WriteLine(
"            Else
                Return Nothing
            End If")
            End If
            .WriteLine(
"        End Function
")
        End With
    End Sub


    ' Generate GetChild, GetChildrenCount so members can be accessed by index
    Private Sub GenerateGetSlotCount(nodeStructure As ParseNodeStructure, allChildren As List(Of ParseNodeChild))

        If _parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken Then Return

        Dim childrenCount = allChildren.Count
        If childrenCount = 0 Then Return
        _writer.WriteLine(
$"        Friend Overrides ReadOnly Property SlotCount() As Integer = {childrenCount}
")
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
        If nodeStructure.IsToken OrElse nodeStructure.IsTrivia Then Return

        Dim MethodName = VisitorMethodName(nodeStructure)
        Dim TypeName = StructureTypeName(nodeStructure)

        If withResult Then
            _writer.WriteLine(
$"        Public Overridable Function {MethodName}(ByVal node As {TypeName}) As TResult
            Return Me.DefaultVisit(node)
        End Function")
        Else
            _writer.WriteLine(
$"        Public Overridable Sub {MethodName}(ByVal node As {TypeName}
            Me.DefaultVisit(node)
        End Sub")
        End If
    End Sub



    ' Generate the RewriteVisitor class definition
    Private Sub GenerateRewriteVisitorClass()
        _writer.WriteLine(
$"    Public MustInherit Class {Ident(_parseTree.RewriteVisitorName)}
        Inherits {Ident(_parseTree.VisitorName)}(Of SyntaxNode)
")

        For Each nodeStructure In _parseTree.NodeStructures.Values
            GenerateRewriteVisitorMethod(nodeStructure)
        Next

        _writer.WriteLine(
"    End Class
")
    End Sub

    ' Generate a method in the RewriteVisitor class
    Private Sub GenerateRewriteVisitorMethod(nodeStructure As ParseNodeStructure)
        If nodeStructure.IsToken OrElse nodeStructure.IsTrivia Then Return
        If nodeStructure.Abstract Then Return            ' do nothing for abstract nodes

        Dim methodName = VisitorMethodName(nodeStructure)
        Dim structureName = StructureTypeName(nodeStructure)

        _writer.WriteLine($"        Public Overrides Function {methodName}(ByVal node As {structureName}) As SyntaxNode")

        ' non-abstract non-terminals need to rewrite their children and recreate as needed.
        Dim allFields = GetAllFieldsOfStructure(nodeStructure)
        Dim allChildren = GetAllChildrenOfStructure(nodeStructure)
        With _writer

            ' create anyChanges variable
            .WriteLine("            Dim anyChanges As Boolean = False")
            .WriteLine()

            ' visit all children
            For i = 0 To allChildren.Count - 1
                Dim child = allChildren(i)
                Dim VarName = ChildNewVarName(child)
                Dim PropName = ChildPropertyName(child)
                If child.IsList Then
                    .WriteLine($"            Dim {VarName} = VisitList(node.{PropName})")
                    If KindTypeStructure(child.ChildKind).IsToken Then
                        .WriteLine($"            If node.{PropName}.Node IsNot {VarName}.Node Then anyChanges = True")
                    Else
                        .WriteLine($"            If node.{PropName} IsNot {VarName}.Node Then anyChanges = True")
                    End If

                ElseIf KindTypeStructure(child.ChildKind).IsToken Then
                    .WriteLine(
$"            Dim {VarName} = DirectCast(VisitToken(node.{PropName}).Node, {ChildConstructorTypeRef(child)})
            If node.{PropName}.Node IsNot {VarName} Then anyChanges = True")

                Else
                    .WriteLine(
$"            Dim {VarName} = DirectCast(Visit(node.{PropName}), {ChildPropertyName(child)})
            If node.{PropName} IsNot {VarName} Then anyChanges = True")
                End If
            Next
            _writer.WriteLine()

            ' check if any changes.
            .WriteLine("            If anyChanges Then")
            .Write($"                Return New {StructureTypeName(nodeStructure)}(node.Kind, node.Green.GetDiagnostics, node.Green.GetAnnotations")

            For Each field In allFields
                .Write($", node.{FieldPropertyName(field)}")
            Next

            For Each child In allChildren
                .Write($", {ChildNewVarName(child)}")
                If child.IsList Then
                    'If KindTypeStructure(child.ChildKind).IsToken Then
                    '  
                    'Else
                    '    _writer.Write(", {0}.Node", ChildNewVarName(child))
                    'End If  
                    .Write(".Node")
                ElseIf KindTypeStructure(child.ChildKind).IsToken Then
                    '  .Write(", {0}", ChildNewVarName(child), ChildFieldTypeRef(child))
                Else
                    ' .Write(", {0}", ChildNewVarName(child))
                End If
            Next
            .WriteLine(
")
             Else
                Return node
             End If
         End Function
 ")

        End With
    End Sub


End Class
