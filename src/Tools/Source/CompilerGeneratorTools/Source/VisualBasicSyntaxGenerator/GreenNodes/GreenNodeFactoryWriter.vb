' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------------------------------------
' This is the code that actually outputs the VB code that defines the node factories. It is passed a read and validated
' ParseTree, and outputs the code to for the node factories.
'-----------------------------------------------------------------------------------------------------------

Imports System.IO

' Class to write out the code for the code tree.
Friend Class GreenNodeFactoryWriter
    Inherits WriteUtils

    Private _writer As TextWriter    'output is sent here.

    ' Initialize the class with the parse tree to write.
    Public Sub New(parseTree As ParseTree)
        MyBase.New(parseTree)
    End Sub

    ' Write out the factory class to the given file.
    Public Sub WriteFactories(writer As TextWriter)
        _writer = writer

        GenerateFile()
    End Sub

    Private Sub GenerateFile()
        GenerateFactoryClass(contextual:=False)
        GenerateFactoryClass(contextual:=True)
    End Sub

    ' Generate the factory class
    Private Sub GenerateFactoryClass(contextual As Boolean)
        _writer.WriteLine()
        If Not String.IsNullOrEmpty(_parseTree.NamespaceName) Then
            _writer.WriteLine("Namespace {0}", Ident(_parseTree.NamespaceName) + ".Syntax.InternalSyntax")
            _writer.WriteLine()
        End If

        If contextual Then
            _writer.WriteLine("    Friend Class {0}", Ident(_parseTree.ContextualFactoryClassName))
            GenerateConstructor()
        Else
            _writer.WriteLine("    Friend Partial Class {0}", Ident(_parseTree.FactoryClassName))
        End If

        _writer.WriteLine()
        GenerateAllFactoryMethods(contextual)

        _writer.WriteLine("    End Class")

        If Not String.IsNullOrEmpty(_parseTree.NamespaceName) Then
            _writer.WriteLine("End Namespace")
        End If
    End Sub

    ' Generator all factory methods for all node structures.
    Private Sub GenerateAllFactoryMethods(contextual As Boolean)
        For Each nodeStructure In _parseTree.NodeStructures.Values
            If Not nodeStructure.NoFactory Then
                GenerateFactoryMethodsForStructure(nodeStructure, contextual)
            End If
        Next
    End Sub

    ' Generator all factory methods for a node structure.
    ' If a nodeStructure has 0 kinds, it is abstract and no factory method is generator
    ' If a nodeStructure has 1 kind, a factory method for that kind is generator
    ' If a nodestructure has >=2 kinds, a factory method for each kind is generated, plus one for the structure as a whole, unless name would conflict.
    Private Sub GenerateFactoryMethodsForStructure(nodeStructure As ParseNodeStructure, contextual As Boolean)
        If _parseTree.IsAbstract(nodeStructure) Then Return ' abstract structures don't have factory methods

        If nodeStructure.Name = "PunctuationSyntax" OrElse nodeStructure.Name = "KeywordSyntax" Then
            Return ' use SyntaxFactory.Token() instead, that one is manually implemented
        End If

        For Each nodeKind In nodeStructure.NodeKinds
            GenerateFactoryMethods(nodeStructure, nodeKind, contextual)
        Next

        ' Only generate one a structure-level factory method if >= 2 kinds, and the nodeStructure name doesn't conflict with a kind name.
        If nodeStructure.NodeKinds.Count >= 2 And Not _parseTree.NodeKinds.ContainsKey(FactoryName(nodeStructure)) Then
            GenerateFactoryMethods(nodeStructure, Nothing, contextual)
        End If
    End Sub

    ' Generate the factory method for a node structure, possibly customized to a particular kind.
    ' If kind is Nothing, generate a factory method that takes a Kind parameter, and can create any kind.
    ' If kind is not Nothing, generator a factory method customized to that particular kind.
    Private Sub GenerateFactoryMethods(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind, contextual As Boolean)
        GenerateFactoryMethod(nodeStructure, nodeKind, internalForm:=True, contextual:=contextual)

        'Dim tokenChildren = AllFactoryChildrenOfStructure(nodeStructure).Where(
        '       Function(c)
        '           Return KindTypeStructure(c.ChildKind).IsToken
        '       End Function)

        'If nodeStructure.IsToken OrElse nodeStructure.IsTrivia OrElse tokenChildren.Any Then
        '    GenerateFactoryMethod(nodeStructure, nodeKind, True)
        'End If

    End Sub

    Private Sub CheckKind(structureName As String)
        _writer.WriteLine("            Debug.Assert(SyntaxFacts.Is{0}(kind))", structureName)
    End Sub

    Private Sub CheckParam(name As String)
        _writer.WriteLine("            Debug.Assert({0} IsNot Nothing)", name)
    End Sub

    Private Sub CheckStructureParam(parent As ParseNodeStructure, nodeKind As ParseNodeKind, child As ParseNodeChild, factoryFunctionName As String)
        Dim name = ChildParamName(child, factoryFunctionName)
        _writer.Write("            Debug.Assert({0} IsNot Nothing", name)

        Dim childNodeKind As ParseNodeKind = TryCast(child.ChildKind, ParseNodeKind)

        If childNodeKind IsNot Nothing Then
            _writer.WriteLine(" AndAlso {0}.Kind = SyntaxKind.{1})", name, childNodeKind.Name)

        ElseIf TypeOf child.ChildKind Is List(Of ParseNodeKind) Then
            If nodeKind IsNot Nothing Then
                childNodeKind = child.ChildKind(nodeKind.Name)
                If childNodeKind IsNot Nothing Then
                    _writer.WriteLine(" AndAlso {0}.Kind = SyntaxKind.{1})", name, childNodeKind.Name)
                Else
                    _writer.WriteLine(" AndAlso SyntaxFacts.Is{1}({0}.Kind))", name, FactoryName(parent) + child.Name)
                End If
            Else
                _writer.WriteLine(" AndAlso SyntaxFacts.Is{1}({0}.Kind))", name, FactoryName(parent) + child.Name)
            End If
        Else
            _writer.WriteLine(")")
        End If
    End Sub

    ' Generate the factory method for a node structure, possibly customized to a particular kind.
    ' If kind is Nothing, generate a factory method that takes a Kind parameter, and can create any kind.
    ' If kind is not Nothing, generator a factory method customized to that particular kind.
    ' The simplified form is:
    '   Defaults the text for any token with token-text defined
    '   Defaults the trivia to a single trailing space for any token
    Private Sub GenerateFactoryMethod(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind, internalForm As Boolean, contextual As Boolean)

        Dim factoryFunctionName As String       ' name of the factory method.
        Dim allFields = GetAllFieldsOfStructure(nodeStructure)
        Dim allChildren = GetAllChildrenOfStructure(nodeStructure)
        Dim allFactoryChildren = GetAllFactoryChildrenOfStructure(nodeStructure)
        Dim tokenText As String = Nothing  ' If not nothing, the default text for a token.

        If nodeKind IsNot Nothing Then
            If nodeKind.NoFactory Then Return

            factoryFunctionName = FactoryName(nodeKind)
            If nodeStructure.IsToken AndAlso nodeKind.TokenText <> "" Then
                tokenText = nodeKind.TokenText
            End If
        Else
            If nodeStructure.NoFactory Then Return

            factoryFunctionName = FactoryName(nodeStructure)
        End If

        ' 1. Generate the Function line
        '------------------------------
        Dim needComma = False  ' do we need a comma before the next parameter?

        _writer.WriteLine()
        GenerateSummaryXmlComment(_writer, nodeStructure.Description)

        If nodeKind Is Nothing Then

            Dim kindsList = String.Join(", ", From kind In nodeStructure.NodeKinds Select kind.Name)

            GenerateParameterXmlComment(_writer, "kind", String.Format("A <see cref=""SyntaxKind""/> representing the specific kind of {0}. One of {1}.", nodeStructure.Name, kindsList))
        End If

        If nodeStructure.IsTerminal Then
            GenerateParameterXmlComment(_writer, "text", "The actual text of this token.")
        End If

        For Each child In allChildren
            GenerateParameterXmlComment(_writer, LowerFirstCharacter(OptionalChildName(child)), child.Description, escapeText:=True)
        Next

        _writer.Write(
                    "        {0} {1}{2}Function {3}(",
                    "Friend",
                    If(factoryFunctionName = "GetType" OrElse factoryFunctionName = "Equals",
                       "Shadows ",
                       ""
                    ),
                    If(contextual, "", "Shared "),
                    Ident(factoryFunctionName)
                )

        If nodeKind Is Nothing Then
            _writer.Write("kind As {0}", NodeKindType())
            needComma = True
        End If

        If nodeStructure.IsTerminal Then
            ' terminals have text, except in simplified form 
            If needComma Then _writer.Write(", ")
            _writer.Write("text as String")
            needComma = True
        End If

        ' Generate parameters for each field and child
        For Each field In allFields
            If needComma Then _writer.Write(", ")
            GenerateNodeStructureFieldParameter(field, factoryFunctionName)
            needComma = True
        Next
        For Each child In allFactoryChildren
            If needComma Then _writer.Write(", ")
            GenerateNodeStructureChildParameter(nodeStructure, child, factoryFunctionName)
            needComma = True
        Next

        If nodeStructure.IsToken Then
            ' tokens have trivia also.
            If needComma Then _writer.Write(", ") : needComma = False
            _writer.Write("leadingTrivia As GreenNode, trailingTrivia As GreenNode", StructureTypeName(_parseTree.RootStructure))

        End If

        _writer.WriteLine(") As {0}", StructureTypeName(nodeStructure))

        '2. Generate the contracts.
        '----------------------------------------
        If nodeStructure.IsTerminal Then
            ' terminals have text, except in simplified form 
            CheckParam("text")
        End If

        If nodeKind Is Nothing Then
            CheckKind(SyntaxFactName(nodeStructure))
        End If

        For Each child In allFactoryChildren
            If Not child.IsOptional Then
                ' No need to check lists they are value types.  It is OK for child.Node to be nothing
                If child.IsList Then
                    'CheckListParam(ChildParamName(child, factoryFunctionName), internalForm)
                Else
                    If KindTypeStructure(child.ChildKind).IsToken Then
                        CheckStructureParam(nodeStructure, nodeKind, child, factoryFunctionName)
                    Else
                        CheckParam(ChildParamName(child, factoryFunctionName))
                    End If
                End If
            End If
        Next

        '3. Generate the call to the constructor
        '----------------------------------------

        ' the non-simplified form calls the constructor
        If (nodeStructure.IsTerminal OrElse
            nodeStructure.Name = "SkippedTokensTriviaSyntax" OrElse
            nodeStructure.Name = "DocumentationCommentTriviaSyntax" OrElse
            nodeStructure.Name.EndsWith("DirectiveTriviaSyntax", StringComparison.Ordinal) OrElse
            nodeStructure.Name = "AttributeSyntax" OrElse
            allFields.Count + allChildren.Count > 3) Then

            _writer.Write("            Return New {0}(", StructureTypeName(nodeStructure))
            GenerateCtorArgs(nodeStructure, nodeKind, contextual, factoryFunctionName)
            _writer.WriteLine(")")

        Else
            _writer.WriteLine("")
            'Dim hash As Integer
            _writer.WriteLine("            Dim hash As Integer")

            'Dim cached = SyntaxNodeCache.TryGetNode(SyntaxKind.ReturnStatement, returnKeyword, expression, hash)

            If contextual Then
                _writer.Write("            Dim cached = VisualBasicSyntaxNodeCache.TryGetNode(")
            Else
                _writer.Write("            Dim cached = SyntaxNodeCache.TryGetNode(")
            End If

            GenerateCtorArgs(nodeStructure, nodeKind, contextual, factoryFunctionName)
            _writer.WriteLine(", hash)")

            'If cached IsNot Nothing Then
            _writer.WriteLine("            If cached IsNot Nothing Then")
            '    Return DirectCast(cached, ReturnStatementSyntax)
            _writer.WriteLine("                Return DirectCast(cached, {0})", StructureTypeName(nodeStructure))
            'End If
            _writer.WriteLine("            End If")
            _writer.WriteLine("")

            'Dim result = New ReturnStatementSyntax(SyntaxKind.ReturnStatement, returnKeyword, expression)
            _writer.Write("            Dim result = New {0}(", StructureTypeName(nodeStructure))
            GenerateCtorArgs(nodeStructure, nodeKind, contextual, factoryFunctionName)
            _writer.WriteLine(")")
            'If hash >= 0 Then
            _writer.WriteLine("            If hash >= 0 Then")
            '    SyntaxNodeCache.AddNode(result, hash)
            _writer.WriteLine("                SyntaxNodeCache.AddNode(result, hash)")
            'End If
            _writer.WriteLine("            End If")
            _writer.WriteLine("")

            'Return result
            _writer.WriteLine("            Return result")

        End If

        '4. Generate the End Function
        '----------------------------
        _writer.WriteLine("        End Function")
        _writer.WriteLine()
    End Sub

    Private Sub GenerateCtorArgs(nodeStructure As ParseNodeStructure,
                                 nodeKind As ParseNodeKind,
                                 contextual As Boolean,
                                 factoryFunctionName As String)
        Dim allFields = GetAllFieldsOfStructure(nodeStructure)
        Dim allChildren = GetAllChildrenOfStructure(nodeStructure)

        If nodeKind Is Nothing Then
            _writer.Write("kind")
        Else
            _writer.Write("{0}.{1}", NodeKindType(), Ident(nodeKind.Name))
        End If

        If nodeStructure.IsTerminal Then
            ' terminals have text
            _writer.Write(", text")
        End If

        If nodeStructure.IsToken Then
            ' tokens have trivia
            _writer.Write(", leadingTrivia, trailingTrivia")
        End If

        ' Generate parameters for each field and child
        For Each field In allFields
            _writer.Write(", {0}", FieldParamName(field, factoryFunctionName))
        Next

        For Each child In allChildren
            If child.NotInFactory Then
                _writer.Write(", Nothing")
            Else
                If child.IsList Then
                    _writer.Write(", {0}.Node", ChildParamName(child, factoryFunctionName))
                ElseIf Not True AndAlso KindTypeStructure(child.ChildKind).IsToken Then
                    _writer.Write(", {0}.Node)", ChildParamName(child, factoryFunctionName), ChildConstructorTypeRef(child, True))
                Else
                    _writer.Write(", {0}", ChildParamName(child, factoryFunctionName))
                End If
            End If
        Next

        If contextual Then
            _writer.Write(", _factoryContext")
        End If
    End Sub

    ' Generate a parameter corresponding to a node structure field
    Private Sub GenerateNodeStructureFieldParameter(field As ParseNodeField, Optional conflictName As String = Nothing)
        _writer.Write("{0} As {1}", FieldParamName(field, conflictName), FieldTypeRef(field))
    End Sub

    ' Generate a parameter corresponding to a node structure child
    Private Sub GenerateNodeStructureChildParameter(node As ParseNodeStructure, child As ParseNodeChild, Optional conflictName As String = Nothing)
        _writer.Write("{0} As {1}", ChildParamName(child, conflictName), ChildFactoryTypeRef(node, child, True, True))
    End Sub

    Private Sub GenerateConstructor()
        _writer.WriteLine()
        _writer.WriteLine("        Private ReadOnly _factoryContext As ISyntaxFactoryContext")
        _writer.WriteLine()
        _writer.WriteLine("        Sub New(factoryContext As ISyntaxFactoryContext)")
        _writer.WriteLine("            _factoryContext = factoryContext")
        _writer.WriteLine("        End Sub")
    End Sub

End Class

