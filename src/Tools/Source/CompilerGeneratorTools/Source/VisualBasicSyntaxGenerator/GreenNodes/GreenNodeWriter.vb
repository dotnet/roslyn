' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------------------------------------
' This is the code that actually outputs the VB code that defines the tree. It is passed a read and validated
' ParseTree, and outputs the code to defined the node classes for that tree, and also additional data
' structures like the kinds, visitor, etc.
'-----------------------------------------------------------------------------------------------------------

Imports System.IO

' Class to write out the code for the code tree.
Friend Class GreenNodeWriter
    Inherits WriteUtils

    Private _writer As TextWriter    'output is sent here.
    Private ReadOnly _nonterminalsWithOneChild As List(Of String) = New List(Of String)
    Private ReadOnly _nonterminalsWithTwoChildren As List(Of String) = New List(Of String)

    ' Initialize the class with the parse tree to write.
    Public Sub New(parseTree As ParseTree)
        MyBase.New(parseTree)
    End Sub

    ' Write out the code defining the tree to the give file.
    Public Sub WriteTreeAsCode(writer As TextWriter)
        _writer = writer

        GenerateFile()
    End Sub

    Private Sub GenerateFile()
        GenerateNamespace()
    End Sub

    Private Sub GenerateNamespace()
        _writer.WriteLine()
        If Not String.IsNullOrEmpty(_parseTree.NamespaceName) Then
            _writer.WriteLine("Namespace {0}", Ident(_parseTree.NamespaceName) + ".Syntax.InternalSyntax")
            _writer.WriteLine()
        End If

        GenerateNodeStructures()

        If Not String.IsNullOrEmpty(_parseTree.VisitorName) Then
            GenerateVisitorClass()
        End If

        If Not String.IsNullOrEmpty(_parseTree.RewriteVisitorName) Then
            GenerateRewriteVisitorClass()
        End If

        If Not String.IsNullOrEmpty(_parseTree.NamespaceName) Then
            _writer.WriteLine("End Namespace")
        End If

        'DumpNames("Nodes with One Child", _nonterminalsWithOneChild)
        'DumpNames("Nodes with Two Children", _nonterminalsWithTwoChildren)
    End Sub

#Disable Warning IDE0051 ' Remove unused private members
    Private Sub DumpNames(title As String, names As List(Of String))
#Enable Warning IDE0051 ' Remove unused private members
        Console.WriteLine(title)
        Console.WriteLine("=======================================")
        Dim sortedNames = From n In names Order By n
        For Each name In sortedNames
            Console.WriteLine(name)
        Next
        Console.WriteLine()
    End Sub

    Private Sub GenerateNodeStructures()
        For Each nodeStructure In _parseTree.NodeStructures.Values
            If Not nodeStructure.NoFactory Then
                GenerateNodeStructureClass(nodeStructure)
            End If
        Next
    End Sub

    ' Generate a class declaration for a node structure.
    Private Sub GenerateNodeStructureClass(nodeStructure As ParseNodeStructure)
        ' XML comment
        GenerateXmlComment(_writer, nodeStructure, 4, includeRemarks:=False)

        ' Class name
        _writer.Write("    ")
        If (nodeStructure.PartialClass) Then
            _writer.Write("Partial ")
        End If
        Dim visibility As String = "Friend"

        If _parseTree.IsAbstract(nodeStructure) Then
            _writer.WriteLine("{0} MustInherit Class {1}", visibility, StructureTypeName(nodeStructure))
        ElseIf Not nodeStructure.HasDerivedStructure Then
            _writer.WriteLine("{0} NotInheritable Class {1}", visibility, StructureTypeName(nodeStructure))
        Else
            _writer.WriteLine("{0} Class {1}", visibility, StructureTypeName(nodeStructure))
        End If

        ' Base class
        If Not IsRoot(nodeStructure) Then
            _writer.WriteLine("        Inherits {0}", StructureTypeName(nodeStructure.ParentStructure))
        End If
        _writer.WriteLine()

        'Create members
        GenerateNodeStructureMembers(nodeStructure)

        ' Create the constructor.
        GenerateNodeStructureConstructor(nodeStructure, False, noExtra:=True)
        GenerateNodeStructureConstructor(nodeStructure, False, noExtra:=True, contextual:=True)
        GenerateNodeStructureConstructor(nodeStructure, False)

        GenerateCreateRed(nodeStructure)

        ' Create the property accessor for each of the fields
        Dim fields = nodeStructure.Fields
        For i = 0 To fields.Count - 1
            GenerateNodeFieldProperty(fields(i), i, fields(i).ContainingStructure IsNot nodeStructure)
        Next

        ' Create the property accessor for each of the children
        Dim children = nodeStructure.Children
        For i = 0 To children.Count - 1
            GenerateNodeChildProperty(nodeStructure, children(i), i)
            GenerateNodeWithChildProperty(children(i), i, nodeStructure)
        Next

        If Not (_parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken) Then
            If children.Count = 1 Then
                If Not children(0).IsList AndAlso Not KindTypeStructure(children(0).ChildKind).Name = "ExpressionSyntax" Then
                    _nonterminalsWithOneChild.Add(nodeStructure.Name)
                End If
            ElseIf children.Count = 2 Then
                If Not children(0).IsList AndAlso Not KindTypeStructure(children(0).ChildKind).Name = "ExpressionSyntax" AndAlso Not children(1).IsList AndAlso Not KindTypeStructure(children(1).ChildKind).Name = "ExpressionSyntax" Then
                    _nonterminalsWithTwoChildren.Add(nodeStructure.Name)
                End If
            End If
        End If
        'Create GetChild
        GenerateGetChild(nodeStructure)

        GenerateWithTrivia(nodeStructure)

        GenerateSetDiagnostics(nodeStructure)

        GenerateSetAnnotations(nodeStructure)

        ' Visitor accept method
        If Not String.IsNullOrEmpty(_parseTree.VisitorName) Then
            GenerateAccept(nodeStructure)
        End If

        ' Special methods for the root node.
        If IsRoot(nodeStructure) Then
            GenerateRootNodeSpecialMethods(nodeStructure)
        End If

        ' End the class
        _writer.WriteLine("    End Class")
        _writer.WriteLine()
    End Sub

    ' Generate CreateRed method 
    Private Sub GenerateCreateRed(nodeStructure As ParseNodeStructure)

        If _parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken OrElse nodeStructure.IsTrivia Then
            Return
        End If
        _writer.WriteLine("        Friend Overrides Function CreateRed(ByVal parent As SyntaxNode, ByVal startLocation As Integer) As SyntaxNode")
        _writer.WriteLine("            Return new {0}.Syntax.{1}(Me, parent, startLocation)", _parseTree.NamespaceName, StructureTypeName(nodeStructure))
        _writer.WriteLine("        End Function")
        _writer.WriteLine()

    End Sub

    ' Generate SetDiagnostics method 
    Private Sub GenerateSetDiagnostics(nodeStructure As ParseNodeStructure)

        If _parseTree.IsAbstract(nodeStructure) Then
            Return
        End If

        _writer.WriteLine("        Friend Overrides Function SetDiagnostics(ByVal newErrors As DiagnosticInfo()) As GreenNode")
        _writer.Write("            Return new {0}", StructureTypeName(nodeStructure))
        GenerateNodeStructureConstructorParameters(nodeStructure, "newErrors", "GetAnnotations", "GetLeadingTrivia", "GetTrailingTrivia")
        _writer.WriteLine("        End Function")
        _writer.WriteLine()

    End Sub

    ' Generate SetAnnotations method 
    Private Sub GenerateSetAnnotations(nodeStructure As ParseNodeStructure)

        If _parseTree.IsAbstract(nodeStructure) Then
            Return
        End If

        _writer.WriteLine("        Friend Overrides Function SetAnnotations(ByVal annotations As SyntaxAnnotation()) As GreenNode")
        _writer.Write("            Return new {0}", StructureTypeName(nodeStructure))
        GenerateNodeStructureConstructorParameters(nodeStructure, "GetDiagnostics", "annotations", "GetLeadingTrivia", "GetTrailingTrivia")
        _writer.WriteLine("        End Function")
        _writer.WriteLine()

    End Sub

    ' Generate WithTrivia method s
    Private Sub GenerateWithTrivia(nodeStructure As ParseNodeStructure)

        If _parseTree.IsAbstract(nodeStructure) OrElse Not nodeStructure.IsToken Then
            Return
        End If

        _writer.WriteLine("        Public Overrides Function WithLeadingTrivia(ByVal trivia As GreenNode) As GreenNode")
        _writer.Write("            Return new {0}", StructureTypeName(nodeStructure))
        GenerateNodeStructureConstructorParameters(nodeStructure, "GetDiagnostics", "GetAnnotations", "trivia", "GetTrailingTrivia")
        _writer.WriteLine("        End Function")
        _writer.WriteLine()

        _writer.WriteLine("        Public Overrides Function WithTrailingTrivia(ByVal trivia As GreenNode) As GreenNode")
        _writer.Write("            Return new {0}", StructureTypeName(nodeStructure))
        GenerateNodeStructureConstructorParameters(nodeStructure, "GetDiagnostics", "GetAnnotations", "GetLeadingTrivia", "trivia")
        _writer.WriteLine("        End Function")
        _writer.WriteLine()
    End Sub

    ' Generate GetChild, GetChildrenCount so members can be accessed by index
    Private Sub GenerateGetChild(nodeStructure As ParseNodeStructure)
        If _parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken Then
            Return
        End If

        Dim allChildren = GetAllChildrenOfStructure(nodeStructure)
        Dim childrenCount = allChildren.Count
        If childrenCount = 0 Then
            Return
        End If

        _writer.WriteLine("        Friend Overrides Function GetSlot(i as Integer) as GreenNode")

        ' Create the property accessor for each of the children
        Dim children = allChildren

        If childrenCount <> 1 Then
            _writer.WriteLine("            Select case i")

            For i = 0 To childrenCount - 1
                _writer.WriteLine("                Case {0}", i)
                _writer.WriteLine("                    Return Me.{0}", ChildVarName(children(i)))
            Next
            _writer.WriteLine("                Case Else")
            _writer.WriteLine("                    Debug.Assert(false, ""child index out of range"")")
            _writer.WriteLine("                    Return Nothing")
            _writer.WriteLine("            End Select")
        Else
            _writer.WriteLine("            If i = 0 Then")
            _writer.WriteLine("                Return Me.{0}", ChildVarName(children(0)))

            _writer.WriteLine("            Else")
            _writer.WriteLine("                Debug.Assert(false, ""child index out of range"")")
            _writer.WriteLine("                Return Nothing")
            _writer.WriteLine("            End If")
        End If

        _writer.WriteLine("        End Function")
        _writer.WriteLine()

        '_writer.WriteLine("        Friend Overrides ReadOnly Property SlotCount() As Integer")
        '_writer.WriteLine("            Get")
        '_writer.WriteLine("                Return {0}", childrenCount)
        '_writer.WriteLine("            End Get")
        '_writer.WriteLine("        End Property")

        _writer.WriteLine()

    End Sub

    Private Sub GenerateNodeStructureMembers(nodeStructure As ParseNodeStructure)
        Dim fields = nodeStructure.Fields
        For Each field In fields
            _writer.WriteLine("        Friend ReadOnly {0} as {1}", FieldVarName(field), FieldTypeRef(field))
        Next

        Dim children = nodeStructure.Children
        For Each child In children
            _writer.WriteLine("        Friend ReadOnly {0} as {1}", ChildVarName(child), ChildFieldTypeRef(child, True))
        Next

        _writer.WriteLine()
    End Sub

    ' Generate constructor for a node structure
    Private Sub GenerateNodeStructureConstructor(nodeStructure As ParseNodeStructure,
                                                 isRaw As Boolean,
                                                 Optional noExtra As Boolean = False,
                                                 Optional contextual As Boolean = False)

        ' these constructors are hardcoded
        If nodeStructure.IsTokenRoot OrElse nodeStructure.IsTriviaRoot OrElse nodeStructure.Name = "StructuredTriviaSyntax" Then
            Return
        End If

        If nodeStructure.ParentStructure Is Nothing Then
            Return
        End If

        Dim allFields = GetAllFieldsOfStructure(nodeStructure)

        _writer.Write("        Friend Sub New(")

        ' Generate each of the field parameters
        _writer.Write("ByVal kind As {0}", NodeKindType())
        If Not noExtra Then
            _writer.Write(", ByVal errors as DiagnosticInfo(), ByVal annotations as SyntaxAnnotation()", NodeKindType())
        End If

        If nodeStructure.IsTerminal Then
            ' terminals have a text
            _writer.Write(", text as String")
        End If

        If nodeStructure.IsToken Then
            ' tokens have trivia

            _writer.Write(", leadingTrivia As GreenNode, trailingTrivia As GreenNode", StructureTypeName(_parseTree.RootStructure))
        End If

        For Each field In allFields
            _writer.Write(", ")
            GenerateNodeStructureFieldParameter(field)
        Next

        For Each child In GetAllChildrenOfStructure(nodeStructure)
            _writer.Write(", ")
            GenerateNodeStructureChildParameter(child, Nothing, True)
        Next

        If contextual Then
            _writer.Write(", context As ISyntaxFactoryContext")
        End If

        _writer.WriteLine(")")

        ' Generate each of the field parameters
        _writer.Write("            MyBase.New(kind", NodeKindType())

        If Not noExtra Then
            _writer.Write(", errors, annotations")
        End If

        If nodeStructure.IsToken AndAlso Not nodeStructure.IsTokenRoot Then
            ' nonterminals have text.
            _writer.Write(", text")

            If Not nodeStructure.IsTrivia Then
                ' tokens have trivia, but only if they are not trivia.
                _writer.Write(", leadingTrivia, trailingTrivia")
            End If
        End If

        Dim baseClass = nodeStructure.ParentStructure
        If baseClass IsNot Nothing Then
            For Each child In GetAllChildrenOfStructure(baseClass)
                _writer.Write(", {0}", ChildParamName(child))
            Next
        End If

        _writer.WriteLine(")")

        If Not nodeStructure.Abstract Then
            Dim allChildren = GetAllChildrenOfStructure(nodeStructure)
            Dim childrenCount = allChildren.Count
            If childrenCount <> 0 Then
                _writer.WriteLine("            MyBase._slotCount = {0}", childrenCount)
            End If
        End If

        ' Generate code to initialize this class

        If contextual Then
            _writer.WriteLine("            Me.SetFactoryContext(context)")
        End If

        If allFields.Count > 0 Then
            For i = 0 To allFields.Count - 1
                _writer.WriteLine("            Me.{0} = {1}", FieldVarName(allFields(i)), FieldParamName(allFields(i)))
            Next
        End If

        If nodeStructure.Children.Count > 0 Then
            '_writer.WriteLine("            Dim fullWidth as integer")
            _writer.WriteLine()

            For Each child In nodeStructure.Children
                Dim indent = ""
                If child.IsOptional OrElse child.IsList Then
                    'If endKeyword IsNot Nothing Then
                    _writer.WriteLine("            If {0} IsNot Nothing Then", ChildParamName(child))
                    indent = "    "
                End If

                '_writer.WriteLine("{0}            fullWidth  += {1}.FullWidth", indent, ChildParamName(child))
                _writer.WriteLine("{0}            AdjustFlagsAndWidth({1})", indent, ChildParamName(child))
                _writer.WriteLine("{0}            Me.{1} = {2}", indent, ChildVarName(child), ChildParamName(child))

                If child.IsOptional OrElse child.IsList Then
                    'If endKeyword IsNot Nothing Then
                    _writer.WriteLine("            End If", ChildParamName(child))
                End If
            Next

            '_writer.WriteLine("            Me._fullWidth += fullWidth")
            _writer.WriteLine()
        End If

        'TODO: BLUE
        If StructureTypeName(nodeStructure) = "DirectiveTriviaSyntax" Then
            _writer.WriteLine("            SetFlags(NodeFlags.ContainsDirectives)")
        End If

        ' Generate End Sub
        _writer.WriteLine("        End Sub")
        _writer.WriteLine()

    End Sub

    Private Sub GenerateNodeStructureConstructorParameters(nodeStructure As ParseNodeStructure, errorParam As String, annotationParam As String, precedingTriviaParam As String, followingTriviaParam As String)
        ' Generate each of the field parameters
        _writer.Write("(Me.Kind")

        _writer.Write(", {0}", errorParam)
        _writer.Write(", {0}", annotationParam)

        If nodeStructure.IsToken Then
            ' nonterminals have text.
            _writer.Write(", text")

            If Not nodeStructure.IsTrivia Then
                ' tokens have trivia, but only if they are not trivia.
                _writer.Write(", {0}, {1}", precedingTriviaParam, followingTriviaParam)
            End If
        ElseIf nodeStructure.IsTrivia AndAlso nodeStructure.IsTriviaRoot Then
            _writer.Write(", Me.Text")
        End If

        For Each field In GetAllFieldsOfStructure(nodeStructure)
            _writer.Write(", {0}", FieldVarName(field))
        Next

        For Each child In GetAllChildrenOfStructure(nodeStructure)
            _writer.Write(", {0}", ChildVarName(child))
        Next

        _writer.WriteLine(")")
    End Sub

    ' Generate a parameter corresponding to a node structure field
    Private Sub GenerateNodeStructureFieldParameter(field As ParseNodeField, Optional conflictName As String = Nothing)
        _writer.Write("{0} As {1}", FieldParamName(field, conflictName), FieldTypeRef(field))
    End Sub

    ' Generate a parameter corresponding to a node structure child
    Private Sub GenerateNodeStructureChildParameter(child As ParseNodeChild, Optional conflictName As String = Nothing, Optional isGreen As Boolean = False)
        _writer.Write("{0} As {1}", ChildParamName(child, conflictName), ChildConstructorTypeRef(child, isGreen))
    End Sub

    ' Get modifiers
    Private Function GetModifiers(containingStructure As ParseNodeStructure, isOverride As Boolean, name As String) As String
        ' Is this overridable or an override?
        Dim modifiers = ""
        'If isOverride Then
        '    modifiers = "Overrides "
        'ElseIf containingStructure.HasDerivedStructure Then
        '    modifiers = "Overridable "
        'End If

        ' Put Shadows modifier on if useful.
        ' Object has Equals and GetType
        ' root name has members for every kind and structure (factory methods)
        If (name = "Equals" OrElse name = "GetType") Then 'OrElse _parseTree.NodeKinds.ContainsKey(name) OrElse _parseTree.NodeStructures.ContainsKey(name)) Then
            modifiers = "Shadows " + modifiers
        End If
        Return modifiers
    End Function

    ' Generate a public property for a node field
    Private Sub GenerateNodeFieldProperty(field As ParseNodeField, fieldIndex As Integer, isOverride As Boolean)
        ' XML comment
        GenerateXmlComment(_writer, field, 8)

        _writer.WriteLine("        Friend {2}ReadOnly Property {0} As {1}", FieldPropertyName(field), FieldTypeRef(field), GetModifiers(field.ContainingStructure, isOverride, field.Name))
        _writer.WriteLine("            Get")
        _writer.WriteLine("                Return Me.{0}", FieldVarName(field))
        _writer.WriteLine("            End Get")
        _writer.WriteLine("        End Property")
        _writer.WriteLine("")
    End Sub

    ' Generate a public property for a child
    Private Sub GenerateNodeChildProperty(node As ParseNodeStructure, child As ParseNodeChild, childIndex As Integer)
        ' XML comment
        GenerateXmlComment(_writer, child, 8)

        Dim isToken = KindTypeStructure(child.ChildKind).IsToken

        _writer.WriteLine("        Friend {2}ReadOnly Property {0} As {1}", ChildPropertyName(child), ChildPropertyTypeRef(node, child, True), GetModifiers(child.ContainingStructure, False, child.Name))
        _writer.WriteLine("            Get")
        If Not child.IsList Then
            _writer.WriteLine("                Return Me.{0}", ChildVarName(child))

        ElseIf child.IsSeparated Then
            _writer.WriteLine("                Return new {0}(New Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxList(of {1})(Me.{2}))", ChildPropertyTypeRef(node, child, True), BaseTypeReference(child), ChildVarName(child))

        ElseIf KindTypeStructure(child.ChildKind).IsToken Then
            _writer.WriteLine("                Return New Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxList(of GreenNode)(Me.{1})", BaseTypeReference(child), ChildVarName(child))

        Else
            _writer.WriteLine("                Return new {0}(Me.{1})", ChildPropertyTypeRef(node, child, True), ChildVarName(child))
        End If
        _writer.WriteLine("            End Get")
        _writer.WriteLine("        End Property")
        _writer.WriteLine("")

    End Sub

    ' Generate a public property for a child
    Private Sub GenerateNodeWithChildProperty(withChild As ParseNodeChild, childIndex As Integer, nodeStructure As ParseNodeStructure)
        Dim isOverride As Boolean = withChild.ContainingStructure IsNot nodeStructure
        If withChild.GenerateWith Then

            Dim isAbstract As Boolean = _parseTree.IsAbstract(nodeStructure)

            If Not isAbstract Then
                ' XML comment
                GenerateWithXmlComment(_writer, withChild, 8)
                _writer.WriteLine("        Friend {2}Function {0}({3} as {4}) As {1}", ChildWithFunctionName(withChild), StructureTypeName(withChild.ContainingStructure), GetModifiers(withChild.ContainingStructure, isOverride, withChild.Name), Ident(UpperFirstCharacter(withChild.Name)), ChildConstructorTypeRef(withChild))
                _writer.WriteLine("            Ensures(Result(Of {0}) IsNot Nothing)", StructureTypeName(withChild.ContainingStructure))
                _writer.Write("            return New {0}(", StructureTypeName(nodeStructure))

                _writer.Write("Kind, Green.Errors")

                Dim allFields = GetAllFieldsOfStructure(nodeStructure)
                If allFields.Count > 0 Then
                    For i = 0 To allFields.Count - 1
                        _writer.Write(", {0}", FieldParamName(allFields(i)))
                    Next
                End If

                For Each child In nodeStructure.Children
                    If child IsNot withChild Then
                        _writer.Write(", {0}", ChildParamName(child))
                    Else
                        _writer.Write(", {0}", Ident(UpperFirstCharacter(child.Name)))
                    End If
                Next

                _writer.WriteLine(")")
                _writer.WriteLine("        End Function")
            ElseIf nodeStructure.Children.Contains(withChild) Then
                ' XML comment
                GenerateWithXmlComment(_writer, withChild, 8)
                _writer.WriteLine("        Friend {2} Function {0}({3} as {4}) As {1}", ChildWithFunctionName(withChild), StructureTypeName(withChild.ContainingStructure), "MustOverride", Ident(UpperFirstCharacter(withChild.Name)), ChildConstructorTypeRef(withChild))
            End If
            _writer.WriteLine("")
        End If
    End Sub

    ' Generate public properties for a child that is a separated list

    Private Sub GenerateAccept(nodeStructure As ParseNodeStructure)
        If nodeStructure.ParentStructure IsNot Nothing AndAlso (_parseTree.IsAbstract(nodeStructure) OrElse nodeStructure.IsToken OrElse nodeStructure.IsTrivia) Then
            Return
        End If
        _writer.WriteLine("        Public {0} Function Accept(ByVal visitor As {1}) As VisualBasicSyntaxNode", If(IsRoot(nodeStructure), "Overridable", "Overrides"), _parseTree.VisitorName)
        _writer.WriteLine("            Return visitor.{0}(Me)", VisitorMethodName(nodeStructure))
        _writer.WriteLine("        End Function")
        _writer.WriteLine()
    End Sub

    ' Generate special methods and properties for the root node. These only appear in the root node.
    Private Sub GenerateRootNodeSpecialMethods(nodeStructure As ParseNodeStructure)

        _writer.WriteLine()
    End Sub

    ' Generate the Visitor class definition
    Private Sub GenerateVisitorClass()
        _writer.WriteLine("    Friend MustInherit Class {0}", Ident(_parseTree.VisitorName))

        ' Basic Visit method that dispatches.
        _writer.WriteLine("        Public Overridable Function Visit(ByVal node As {0}) As VisualBasicSyntaxNode", StructureTypeName(_parseTree.RootStructure))
        _writer.WriteLine("            If node IsNot Nothing")
        _writer.WriteLine("                Return node.Accept(Me)")
        _writer.WriteLine("            Else")
        _writer.WriteLine("                Return Nothing")
        _writer.WriteLine("            End If")
        _writer.WriteLine("        End Function")

        For Each nodeStructure In _parseTree.NodeStructures.Values

            GenerateVisitorMethod(nodeStructure)

        Next

        _writer.WriteLine("    End Class")
        _writer.WriteLine()
    End Sub

    ' Generate a method in the Visitor class
    Private Sub GenerateVisitorMethod(nodeStructure As ParseNodeStructure)
        If nodeStructure.IsToken OrElse nodeStructure.IsTrivia Then
            Return
        End If
        Dim methodName = VisitorMethodName(nodeStructure)
        Dim structureName = StructureTypeName(nodeStructure)

        _writer.WriteLine("        Public Overridable Function {0}(ByVal node As {1}) As VisualBasicSyntaxNode",
                          methodName,
                          structureName)

        _writer.WriteLine("            Debug.Assert(node IsNot Nothing)")

        If Not IsRoot(nodeStructure) Then
            _writer.WriteLine("            Return {0}(node)", VisitorMethodName(nodeStructure.ParentStructure))
        Else
            _writer.WriteLine("            Return node")
        End If
        _writer.WriteLine("        End Function")
    End Sub

    ' Generate the RewriteVisitor class definition
    Private Sub GenerateRewriteVisitorClass()
        _writer.WriteLine("    Friend MustInherit Class {0}", Ident(_parseTree.RewriteVisitorName))
        _writer.WriteLine("        Inherits {0}", Ident(_parseTree.VisitorName), StructureTypeName(_parseTree.RootStructure))
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

        _writer.WriteLine("        Public Overrides Function {0}(ByVal node As {1}) As {2}",
                  methodName,
                  structureName,
                  StructureTypeName(_parseTree.RootStructure))

        ' non-abstract non-terminals need to rewrite their children and recreate as needed.
        Dim allFields = GetAllFieldsOfStructure(nodeStructure)
        Dim allChildren = GetAllChildrenOfStructure(nodeStructure)

        ' create anyChanges variable
        _writer.WriteLine("            Dim anyChanges As Boolean = False")
        _writer.WriteLine()

        ' visit all children
        For i = 0 To allChildren.Count - 1
            If allChildren(i).IsList Then
                _writer.WriteLine("            Dim {0} = VisitList(node.{1})" + Environment.NewLine +
                                  "            If node.{2} IsNot {0}.Node Then anyChanges = True",
                                  ChildNewVarName(allChildren(i)), ChildPropertyName(allChildren(i)), ChildVarName(allChildren(i)))
            ElseIf KindTypeStructure(allChildren(i).ChildKind).IsToken Then
                _writer.WriteLine("            Dim {0} = DirectCast(Visit(node.{2}), {1})" + Environment.NewLine +
                                  "            If node.{3} IsNot {0} Then anyChanges = True",
                                  ChildNewVarName(allChildren(i)), BaseTypeReference(allChildren(i)), ChildPropertyName(allChildren(i)), ChildVarName(allChildren(i)))
            Else
                _writer.WriteLine("            Dim {0} = DirectCast(Visit(node.{2}), {1})" + Environment.NewLine +
                                  "            If node.{2} IsNot {0} Then anyChanges = True",
                                  ChildNewVarName(allChildren(i)), ChildPropertyTypeRef(nodeStructure, allChildren(i)), ChildVarName(allChildren(i)))
            End If
        Next
        _writer.WriteLine()

        ' check if any changes.
        _writer.WriteLine("            If anyChanges Then")

        _writer.Write("                Return New {0}(node.Kind", StructureTypeName(nodeStructure))

        _writer.Write(", node.GetDiagnostics, node.GetAnnotations")

        For Each field In allFields
            _writer.Write(", node.{0}", FieldPropertyName(field))
        Next
        For Each child In allChildren
            If child.IsList Then
                _writer.Write(", {0}.Node", ChildNewVarName(child))
            ElseIf KindTypeStructure(child.ChildKind).IsToken Then
                _writer.Write(", {0}", ChildNewVarName(child))
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

