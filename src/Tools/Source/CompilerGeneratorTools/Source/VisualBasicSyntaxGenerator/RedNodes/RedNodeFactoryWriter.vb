' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO

' Class to write out the code for the code tree.
Friend Class RedNodeFactoryWriter
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
        GenerateFactoryClass()
    End Sub

    ' Generate the factory class
    Private Sub GenerateFactoryClass()
        _writer.WriteLine()
        If Not String.IsNullOrEmpty(_parseTree.NamespaceName) Then
            _writer.WriteLine("Namespace {0}", Ident(_parseTree.NamespaceName))
            _writer.WriteLine()
        End If

        _writer.WriteLine("    Public Partial Class {0}", Ident(_parseTree.FactoryClassName))
        GenerateSpecialMembers()
        _writer.WriteLine()
        GenerateAllFactoryMethods()
        _writer.WriteLine("    End Class")

        If Not String.IsNullOrEmpty(_parseTree.NamespaceName) Then
            _writer.WriteLine("End Namespace")
        End If
    End Sub

    ' Generate special members, that aren't the factories, but are used by the factories
    Private Sub GenerateSpecialMembers()
    End Sub

    ' Generator all factory methods for all node structures.
    Private Sub GenerateAllFactoryMethods()
        For Each nodeStructure In _parseTree.NodeStructures.Values
            If Not nodeStructure.NoFactory Then
                GenerateFactoryMethodsForStructure(nodeStructure)
            End If
        Next
    End Sub

    ' Generator all factory methods for a node structure.
    ' If a nodeStructure has 0 kinds, it is abstract and no factory method is generator
    ' If a nodeStructure has 1 kind, a factory method for that kind is generator
    ' If a nodestructure has >=2 kinds, a factory method for each kind is generated, plus one for the structure as a whole, unless name would conflict.
    Private Sub GenerateFactoryMethodsForStructure(nodeStructure As ParseNodeStructure)
        If _parseTree.IsAbstract(nodeStructure) Then
            Return ' abstract structures don't have factory methods
        End If

        If nodeStructure.Name = "PunctuationSyntax" OrElse nodeStructure.Name = "KeywordSyntax" Then
            'No specialized factories for tokens
            'use SyntaxFactory.Token() instead, that one is manually implemented
        Else
            For Each nodeKind In nodeStructure.NodeKinds
                GenerateFactoryMethods(nodeStructure, nodeKind)
            Next

            ' Only generate one a structure-level factory method if >= 2 kinds
            '
            ' In case of a kind has the same name as the type itself, we will create two overloads
            ' TODO: consider renaming the nodes or the general factory method to be unambiguous for
            ' public API users.
            If nodeStructure.NodeKinds.Count >= 2 Then
                GenerateFactoryMethods(nodeStructure, Nothing)
            End If
        End If
    End Sub

    Private Sub GenerateFactoryMethods(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind)
        GenerateFullFactoryMethod(nodeStructure, nodeKind)

        If nodeKind Is Nothing Then
            GenerateChildTokenKindsForNodeKinds(nodeStructure)
        End If

        ' Generate secondary factory w/o auto-creatable tokens
        Dim allFullFactoryChildren = GetAllFactoryChildrenOfStructure(nodeStructure).ToList()
        Dim allFields = GetAllFieldsOfStructure(nodeStructure)
        Dim allFactoryChildrenWithoutAutoCreatableTokens = GetAllFactoryChildrenWithoutAutoCreatableTokens(nodeStructure, nodeKind)
        Dim leastSignature = allFullFactoryChildren
        If Not Enumerable.SequenceEqual(allFullFactoryChildren, allFactoryChildrenWithoutAutoCreatableTokens) Then
            GenerateSecondaryFactoryMethod(nodeStructure, nodeKind, allFullFactoryChildren, allFields, allFactoryChildrenWithoutAutoCreatableTokens, AddressOf GetDefaultedFactoryParameterExpression)
            leastSignature = allFactoryChildrenWithoutAutoCreatableTokens
        End If

        ' Generate secondary factory with only required children 
        ' TODO: (if there is only one child and it is an optional list, then allow it to be optional unless there is already a zero-parameter factory)
        Dim allRequiredFactoryChildren = GetAllRequiredFactoryChildren(nodeStructure, nodeKind)
        If Not (Enumerable.SequenceEqual(allFullFactoryChildren, allRequiredFactoryChildren) OrElse Enumerable.SequenceEqual(allFactoryChildrenWithoutAutoCreatableTokens, allRequiredFactoryChildren)) Then
            GenerateSecondaryFactoryMethod(nodeStructure, nodeKind, allFullFactoryChildren, allFields, allRequiredFactoryChildren, AddressOf GetDefaultedFactoryParameterExpression)
            leastSignature = allRequiredFactoryChildren
        End If

        If leastSignature.Any(Function(c) CanBeIdentifierToken(c)) Then
            ' create additional factory with identifier tokens replaced by string parameter
            GenerateSecondaryFactoryMethod(nodeStructure, nodeKind, allFullFactoryChildren, allFields, leastSignature, AddressOf GetDefaultedFactoryParameterExpression, substituteString:=True)
        End If

        If leastSignature.Count = 1 AndAlso leastSignature(0).IsList Then
            ' create additional factory with list as params array
            GenerateSecondaryFactoryMethod(nodeStructure, nodeKind, allFullFactoryChildren, allFields, leastSignature, AddressOf GetDefaultedFactoryParameterExpression, substituteParamArray:=True)
        End If

    End Sub

    Private Sub GenerateChildTokenKindsForNodeKinds(nodeStructure As ParseNodeStructure)
        For Each child In GetAllFactoryChildrenOfStructure(nodeStructure)
            Dim childStructure = KindTypeStructure(child.ChildKind)
            If nodeStructure.NodeKinds.Count > 1 AndAlso child.KindForNodeKind IsNot Nothing AndAlso child.KindForNodeKind.Count > 1 Then
                GenerateChildTokenKindsForNodeKinds(nodeStructure, child)
            End If
        Next
    End Sub

    Private Sub GenerateChildTokenKindsForNodeKinds(nodeStructure As ParseNodeStructure, child As ParseNodeChild)

        Dim name = "Get" + FactoryName(nodeStructure) + child.Name + "Kind"
        _writer.WriteLine("        Private Shared Function {0}(kind As SyntaxKind) As SyntaxKind", name)
        _writer.WriteLine("            Select Case kind")

        For Each nodeKind In nodeStructure.NodeKinds
            Dim childNodeKind As ParseNodeKind = Nothing
            If child.KindForNodeKind.TryGetValue(nodeKind.Name, childNodeKind) Then
                _writer.WriteLine("                Case SyntaxKind.{0}", nodeKind.Name)
                _writer.WriteLine("                    Return SyntaxKind.{0}", childNodeKind.Name)
            End If
        Next
        _writer.WriteLine("                Case Else")
        _writer.WriteLine("                    Throw New ArgumentException(""{0}"")", child.Name)

        _writer.WriteLine("            End Select")
        _writer.WriteLine("        End Function")
    End Sub

    ' Generate the factory method for a node structure, possibly customized to a particular kind.
    ' If kind is Nothing, generate a factory method that takes a Kind parameter, and can create any kind.
    ' If kind is not Nothing, generator a factory method customized to that particular kind.
    ' The simplified form is:
    '   Defaults the text for any token with token-text defined
    '   Defaults the trivia to a single trailing space for any token
    Private Sub GenerateFullFactoryMethod(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind)

        Dim factoryFunctionName As String       ' name of the factory method.
        Dim allFields = GetAllFieldsOfStructure(nodeStructure)
        Dim allChildren = GetAllChildrenOfStructure(nodeStructure)
        Dim allFactoryChildren = GetAllFactoryChildrenOfStructure(nodeStructure)
        Dim tokenText As String = Nothing  ' If not nothing, the default text for a token.

        If nodeKind IsNot Nothing Then
            If nodeKind.NoFactory Then
                Return
            End If

            factoryFunctionName = FactoryName(nodeKind)
            If nodeStructure.IsToken AndAlso nodeKind.TokenText <> "" Then
                tokenText = nodeKind.TokenText
            End If
        Else
            If nodeStructure.NoFactory Then
                Return
            End If

            factoryFunctionName = FactoryName(nodeStructure)
        End If

        Dim isPunctuation = nodeStructure.Name = "PunctuationSyntax" AndAlso Not (nodeKind IsNot Nothing AndAlso nodeKind.Name = "StatementTerminatorToken")
        If HasDefaultToken(nodeStructure, nodeKind) AndAlso isPunctuation Then
            Return
        End If

        ' 1. Generate the Function line
        '------------------------------
        Dim isTextNecessary As Boolean = GenerateFullFactoryMethodFunctionLine(nodeStructure, nodeKind, factoryFunctionName, allFields, allChildren, allFactoryChildren, isPunctuation, includeTriviaForTokens:=True)

        '2. Generate the contracts.
        '----------------------------------------
        If isTextNecessary Then
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
                    Dim paramName = ChildParamName(child, factoryFunctionName)

                    If KindTypeStructure(child.ChildKind).IsToken Then
                        CheckChildToken(nodeStructure, nodeKind, child, paramName, factoryFunctionName)
                    Else
                        CheckChildNode(nodeStructure, nodeKind, child, paramName, factoryFunctionName)
                    End If
                End If
            End If
        Next

        '3. Generate the call to the constructor or other factory method.
        '----------------------------------------

        ' the non-simplified form calls the constructor
        If nodeStructure.IsToken Then
            _writer.Write("            Return New SyntaxToken(Nothing, New InternalSyntax.{0}(", StructureTypeName(nodeStructure))
        ElseIf nodeStructure.IsTrivia Then
            _writer.Write("            Return New SyntaxTrivia(Nothing, New InternalSyntax.{0}(", StructureTypeName(nodeStructure))
        Else
            _writer.Write("            Return New {0}(", StructureTypeName(nodeStructure))
        End If

        If nodeKind Is Nothing Then
            _writer.Write("kind")
        Else
            _writer.Write("{0}.{1}", NodeKindType(), Ident(nodeKind.Name))
        End If

        ' Pass nothing for errors and annotations
        _writer.Write(", Nothing, Nothing")

        If nodeStructure.IsTerminal Then
            ' terminals have text
            If isPunctuation Then
                If nodeKind IsNot Nothing AndAlso nodeKind.Name = "StatementTerminatorToken" Then
                    _writer.Write(", VbCrLf")
                Else
                    _writer.Write(", ""{0}""", If(tokenText <> """", tokenText, tokenText + tokenText))
                End If

            Else
                _writer.Write(", text")
            End If
        End If

        If nodeStructure.IsToken Then
            ' tokens have trivia
            _writer.Write(", leadingTrivia.Node, trailingTrivia.Node")
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
                    If KindTypeStructure(child.ChildKind).IsToken Then
                        _writer.Write(", {0}.Node", ChildParamName(child, factoryFunctionName))
                    Else
                        _writer.Write(", {0}.Node", ChildParamName(child, factoryFunctionName))
                    End If
                ElseIf KindTypeStructure(child.ChildKind).IsToken Then
                    _writer.Write(", DirectCast({0}.Node, {1})", ChildParamName(child, factoryFunctionName), ChildConstructorTypeRef(child, True))
                Else
                    _writer.Write(", {0}", ChildParamName(child, factoryFunctionName))
                End If

            End If
        Next

        If nodeStructure.IsToken OrElse nodeStructure.IsTrivia Then
            _writer.WriteLine("), 0, 0)")
        Else
            _writer.WriteLine(")")
        End If

        '4. Generate the End Function
        '----------------------------
        _writer.WriteLine("        End Function")
        _writer.WriteLine()

        ' For tokens, generate factory method that doesn't take trivia
        If nodeStructure.IsToken Then
            ' 5. Generate the Function line
            '------------------------------
            GenerateFullFactoryMethodFunctionLine(nodeStructure, nodeKind, factoryFunctionName, allFields, allChildren, allFactoryChildren, isPunctuation, includeTriviaForTokens:=False)

            ' 6. Generate the call to the factory method generated above, passing Nothing for the trivia.
            '----------------------------------------
            _writer.Write("            Return {0}(Nothing", Ident(factoryFunctionName)) ' leading trivia

            If nodeKind Is Nothing Then
                _writer.Write(", kind")
            End If

            If isTextNecessary Then
                ' terminals have text, except in simplified form 
                _writer.Write(", text")
            End If

            ' Generate parameters for each field and child
            For Each field In allFields
                _writer.Write(", {0}", FieldParamName(field, factoryFunctionName))
            Next

            For Each child In allFactoryChildren
                _writer.Write(", {0}", ChildParamName(child, factoryFunctionName))
            Next

            _writer.WriteLine(", Nothing)") ' trailing trivia

            ' 7. Generate the End Function
            '----------------------------
            _writer.WriteLine("        End Function")
            _writer.WriteLine()
        End If

    End Sub

    Private Function GenerateFullFactoryMethodFunctionLine(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind, factoryFunctionName As String, allFields As List(Of ParseNodeField), allChildren As List(Of ParseNodeChild), allFactoryChildren As IEnumerable(Of ParseNodeChild), isPunctuation As Boolean, includeTriviaForTokens As Boolean) As Boolean
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

        _writer.Write("        Public {0}Shared Function {1}(",
                      If(factoryFunctionName = "GetType" OrElse factoryFunctionName = "Equals", "Shadows ", ""),
                      Ident(factoryFunctionName)
                )

        If nodeStructure.IsToken AndAlso includeTriviaForTokens Then
            ' tokens have trivia also.
            If needComma Then
                _writer.Write(", ")
            End If

            _writer.Write("leadingTrivia As SyntaxTriviaList")

            needComma = True
        End If

        If nodeKind Is Nothing Then
            If needComma Then
                _writer.Write(", ")
            End If

            _writer.Write("ByVal kind As {0}", NodeKindType())

            needComma = True
        End If

        Dim isTextNecessary = nodeStructure.IsTerminal AndAlso Not isPunctuation

        If isTextNecessary Then
            ' terminals have text, except in simplified form 
            If needComma Then
                _writer.Write(", ")
            End If

            _writer.Write("text as String")

            needComma = True
        End If

        ' Generate parameters for each field and child
        For Each field In allFields
            If needComma Then
                _writer.Write(", ")
            End If

            GenerateNodeStructureFieldParameter(field, factoryFunctionName)
            needComma = True
        Next

        For Each child In allFactoryChildren
            If needComma Then _writer.Write(", ")
            GenerateNodeStructureChildParameter(nodeStructure, child, factoryFunctionName, makeOptional:=False)
            needComma = True
        Next

        If nodeStructure.IsToken AndAlso includeTriviaForTokens Then
            ' tokens have trivia also.
            If needComma Then
                _writer.Write(", ")
            End If

            _writer.Write("trailingTrivia As SyntaxTriviaList")

            needComma = True
        End If

        If nodeStructure.IsToken Then
            _writer.WriteLine(") As SyntaxToken")
        ElseIf nodeStructure.IsTrivia Then
            _writer.WriteLine(") As SyntaxTrivia")
        Else
            _writer.WriteLine(") As {0}", StructureTypeName(nodeStructure))
        End If

        Return isTextNecessary
    End Function

    Private Sub CheckKind(structureName As String)
        _writer.WriteLine("            If Not SyntaxFacts.Is{0}(kind) Then", structureName)
        _writer.WriteLine("                Throw New ArgumentException(""kind"")")
        _writer.WriteLine("            End If")
    End Sub

    Private Sub CheckParam(name As String)
        _writer.WriteLine("            if {0} Is Nothing Then", name)
        _writer.WriteLine("                Throw New ArgumentNullException(NameOf({0}))", name)
        _writer.WriteLine("            End If")
    End Sub

    Private Sub CheckChildNode(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind, child As ParseNodeChild, paramName As String, factoryFunctionName As String)

        If Not child.IsOptional Then
            CheckParam(paramName)
        End If

        Dim childNodeKind = TryCast(child.ChildKind, ParseNodeKind)
        Dim childNodeKinds = TryCast(child.ChildKind, List(Of ParseNodeKind))

        If nodeKind IsNot Nothing AndAlso child.KindForNodeKind IsNot Nothing Then
            child.KindForNodeKind.TryGetValue(nodeKind.Name, childNodeKind)
        End If

        If childNodeKind IsNot Nothing Then
            ' child can only ever be of one kind (and possibly None)
            _writer.WriteLine("            Select Case {0}.Kind()", paramName)
            _writer.Write("                Case SyntaxKind.{0}", childNodeKind.Name)

            _writer.WriteLine()
            _writer.WriteLine("                Case Else")
            _writer.WriteLine("                    Throw new ArgumentException(""{0}"")", paramName)
            _writer.WriteLine("            End Select")

        ElseIf childNodeKinds IsNot Nothing Then
            If nodeKind Is Nothing AndAlso child.KindForNodeKind IsNot Nothing AndAlso child.KindForNodeKind.Count > 1 Then
                ' child kind must correspond to specific node kind
                Dim getterName = "Get" + FactoryName(nodeStructure) + child.Name + "Kind"

                _writer.Write("            If ")
                If child.IsOptional Then
                    _writer.Write("(Not {0}.IsKind(SyntaxKind.None)) AndAlso ", paramName)
                End If

                _writer.WriteLine("(Not {0}.IsKind({1}(kind))) Then", paramName, getterName)

                _writer.WriteLine("                Throw new ArgumentException(""{0}"")", paramName)
                _writer.WriteLine("            End If")

            Else

                ' otherwise child must be one of a specific set of kinds
                _writer.WriteLine("            Select Case {0}.Kind()", paramName)

                Dim first = True
                For Each childNodeKind In childNodeKinds
                    If first Then
                        first = False
                        _writer.Write("                Case SyntaxKind.{0}", childNodeKind.Name)
                        Continue For
                    End If

                    _writer.WriteLine(",")
                    _writer.Write("                     SyntaxKind.{0}", childNodeKind.Name)
                Next

                _writer.WriteLine()

                _writer.WriteLine("                Case Else")
                _writer.WriteLine("                    Throw new ArgumentException(""{0}"")", paramName)
                _writer.WriteLine("            End Select")

            End If
        End If
    End Sub

    Private Sub CheckChildToken(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind, child As ParseNodeChild, paramName As String, factoryFunctionName As String)
        Dim childNodeKind = TryCast(child.ChildKind, ParseNodeKind)
        Dim childNodeKinds = TryCast(child.ChildKind, List(Of ParseNodeKind))

        If nodeKind IsNot Nothing AndAlso child.KindForNodeKind IsNot Nothing Then
            child.KindForNodeKind.TryGetValue(nodeKind.Name, childNodeKind)
        End If

        If childNodeKind IsNot Nothing Then
            ' child can only ever be of one kind (and possibly None)
            _writer.WriteLine("            Select Case {0}.Kind()", paramName)
            _writer.Write("                Case SyntaxKind.{0}", childNodeKind.Name)

            If child.IsOptional Then
                _writer.WriteLine(" :")
                _writer.Write("                Case SyntaxKind.None")
            End If

            _writer.WriteLine()
            _writer.WriteLine("                Case Else")
            _writer.WriteLine("                    Throw new ArgumentException(""{0}"")", paramName)
            _writer.WriteLine("            End Select")

        ElseIf childNodeKinds IsNot Nothing Then

            If nodeKind Is Nothing AndAlso child.KindForNodeKind IsNot Nothing AndAlso child.KindForNodeKind.Count > 1 Then
                ' child kind must correspond to specific node kind
                Dim getterName = "Get" + FactoryName(nodeStructure) + child.Name + "Kind"

                _writer.Write("            If ")
                If child.IsOptional Then
                    _writer.Write("(Not ({0}.IsKind(SyntaxKind.None)) AndAlso ", paramName)
                End If

                _writer.WriteLine("(Not {0}.IsKind({1}(kind))) Then", paramName, getterName)

                _writer.WriteLine("                Throw new ArgumentException(""{0}"")", paramName)
                _writer.WriteLine("            End If")

            Else
                ' otherwise child must be one of a specific set of kinds
                _writer.WriteLine("            Select Case {0}.Kind()", paramName)

                Dim needsComma = False
                For Each childNodeKind In childNodeKinds

                    If needsComma Then
                        _writer.WriteLine(" :")
                    End If
                    _writer.Write("                Case SyntaxKind.{0}", childNodeKind.Name)
                    needsComma = True
                Next

                If child.IsOptional Then
                    _writer.WriteLine(" :")
                    _writer.WriteLine("                Case SyntaxKind.None")
                End If

                _writer.WriteLine()

                _writer.WriteLine("                Case Else")
                _writer.WriteLine("                    Throw new ArgumentException(""{0}"")", paramName)
                _writer.WriteLine("            End Select")

            End If
        End If
    End Sub

    Private Function HasDefaultToken(nodeStructure As ParseNodeStructure,
                             nodeKind As ParseNodeKind) As Boolean

        If Not nodeStructure.IsToken Then
            Return False
        End If

        If nodeKind Is Nothing Then
            Return False
        End If

        If nodeKind.TokenText Is Nothing Then
            Return False
        End If

        If nodeKind.NoFactory Then
            Return False
        End If

        Return True
    End Function

    Private Function GetAllFactoryChildrenWithoutAutoCreatableTokens(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind) As List(Of ParseNodeChild)
        Return GetAllFactoryChildrenOfStructure(nodeStructure).Where(Function(child) Not IsAutoCreatableChild(nodeStructure, nodeKind, child)).ToList()
    End Function

    Private Function GetDefaultedFactoryParameterExpression(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind, child As ParseNodeChild) As String
        If child.IsOptional Then
            Return "Nothing"
        End If

        If IsAutoCreatableToken(nodeStructure, nodeKind, child) Then
            Dim childNodeKind = GetChildNodeKind(nodeKind, child)
            Dim result = String.Format("SyntaxFactory.Token(SyntaxKind.{0})", childNodeKind.Name)
            If child.IsList Then
                result = "SyntaxTokenList.Create(" & result & ")"
            End If
            Return result
        ElseIf IsAutoCreatableNodeOfAutoCreatableTokens(nodeStructure, nodeKind, child) Then
            Dim childNodeKind = GetChildNodeKind(nodeKind, child)
            If child.IsSeparated Then
                Return String.Format("SyntaxFactory.SingletonSeparatedList(SyntaxFactory.{0}())", FactoryName(childNodeKind))
            ElseIf child.IsList Then
                Return String.Format("SyntaxFactory.SingletonList(SyntaxFactory.{0}())", FactoryName(childNodeKind))
            Else
                Return String.Format("SyntaxFactory.{0}()", FactoryName(childNodeKind))
            End If
        End If
        Throw New InvalidOperationException("Badness")
    End Function

    Private Function IsRequiredFactoryChild(node As ParseNodeStructure, nodeKind As ParseNodeKind, child As ParseNodeChild) As Boolean
        Return Not (child.IsOptional Or IsAutoCreatableChild(node, nodeKind, child))
    End Function

    Private Function GetAllRequiredFactoryChildren(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind) As List(Of ParseNodeChild)
        Return GetAllFactoryChildrenOfStructure(nodeStructure).Where(Function(child) IsRequiredFactoryChild(nodeStructure, nodeKind, child)).ToList()
    End Function

    Private Sub GenerateSecondaryFactoryMethod(nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind, allFullFactoryChildren As List(Of ParseNodeChild), allFields As List(Of ParseNodeField), allFactoryChildren As List(Of ParseNodeChild), getDefaultedParameter As Func(Of ParseNodeStructure, ParseNodeKind, ParseNodeChild, String), Optional substituteString As Boolean = False, Optional substituteParamArray As Boolean = False)
        Dim factoryFunctionName As String       ' name of the factory method.
        Dim allFactoryChildrenSet = New HashSet(Of ParseNodeChild)(allFactoryChildren)

        If nodeKind IsNot Nothing Then
            If nodeKind.NoFactory Then
                Return
            End If

            factoryFunctionName = FactoryName(nodeKind)
        Else
            If nodeStructure.NoFactory Then
                Return
            End If

            factoryFunctionName = FactoryName(nodeStructure)
        End If

        Dim isPunctuation = nodeStructure.Name = "PunctuationSyntax" AndAlso Not (nodeKind IsNot Nothing AndAlso nodeKind.Name = "StatementTerminatorToken")
        If HasDefaultToken(nodeStructure, nodeKind) AndAlso isPunctuation Then
            Return
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

        For Each child In allFactoryChildren
            GenerateParameterXmlComment(_writer, LowerFirstCharacter(OptionalChildName(child)), child.Description, escapeText:=True)
        Next

        _writer.Write("        Public {0}Shared Function {1}(",
                      If(factoryFunctionName = "GetType" OrElse factoryFunctionName = "Equals", "Shadows ", ""),
                      Ident(factoryFunctionName)
                )

        If nodeKind Is Nothing Then
            _writer.Write("ByVal kind As {0}", NodeKindType())
            needComma = True
        End If

        Dim isTextNecessary = nodeStructure.IsTerminal AndAlso Not isPunctuation

        If isTextNecessary Then
            ' terminals have text, except in simplified form 
            If needComma Then
                _writer.Write(", ")
            End If

            _writer.Write("text as String")

            needComma = True
        End If

        ' Generate parameters for each field and child
        For Each field In allFields
            If needComma Then
                _writer.Write(", ")
            End If

            GenerateNodeStructureFieldParameter(field, factoryFunctionName)
            needComma = True
        Next

        For Each child In allFactoryChildren
            If needComma Then _writer.Write(", ")
            GenerateNodeStructureChildParameter(nodeStructure, child, factoryFunctionName, makeOptional:=False, substituteString:=substituteString, substituteParamArray:=substituteParamArray)
            needComma = True
        Next

        If nodeStructure.IsToken Then
            ' tokens have trivia also.
            If needComma Then
                _writer.Write(", ")
            End If

            needComma = False
            _writer.Write("Optional leadingTrivia As SyntaxTriviaList = Nothing, Optional trailingTrivia As SyntaxTriviaList = Nothing")
        End If

        If nodeStructure.IsToken Then
            _writer.WriteLine(") As SyntaxToken")
        ElseIf nodeStructure.IsTrivia Then
            _writer.WriteLine(") As SyntaxTrivia")
        Else
            _writer.WriteLine(") As {0}", StructureTypeName(nodeStructure))
        End If

        '2. Generate the call to the constructor or other factory method.
        '----------------------------------------

        ' the non-simplified form calls the constructor
        _writer.Write("            Return SyntaxFactory.{0}(", Ident(factoryFunctionName))

        needComma = False
        If nodeKind Is Nothing Then
            _writer.Write("kind")
            needComma = True
        ElseIf nodeStructure.NodeKinds.Count >= 2 AndAlso FactoryName(nodeStructure) = FactoryName(nodeKind) Then
            ' if this is a factory method for a type which has a kind that has the same name,
            ' make sure the factory method overload that takes a kind is called, otherwise the kind get's dropped and may change to the default (which is bad)
            _writer.Write("SyntaxKind." + nodeKind.Name)
            needComma = True
        End If

        ' Generate parameters for each field and child
        For Each field In allFields
            If needComma Then
                _writer.Write(", ")
            End If
            _writer.Write("{0}", FieldParamName(field, factoryFunctionName))
            needComma = True
        Next

        For Each child In allFullFactoryChildren
            If needComma Then
                _writer.Write(", ")
            End If

            If Not allFactoryChildrenSet.Contains(child) Then
                Dim defaultedParameterExpression As String = getDefaultedParameter(nodeStructure, nodeKind, child)
                _writer.Write("{0}", defaultedParameterExpression)
            ElseIf substituteString AndAlso CanBeIdentifierToken(child) Then
                _writer.Write("SyntaxFactory.Identifier({0})", ChildParamName(child, factoryFunctionName))
            ElseIf substituteParamArray AndAlso child.IsList Then
                If child.IsSeparated Then
                    _writer.Write("SyntaxFactory.SeparatedList(Of {0})().AddRange({1})", BaseTypeReference(child), ChildParamName(child, factoryFunctionName))
                Else
                    _writer.Write("SyntaxFactory.List({0})", ChildParamName(child, factoryFunctionName))
                End If
            Else
                ' everything else is pass-through to full factory
                _writer.Write("{0}", ChildParamName(child, factoryFunctionName))
            End If
            needComma = True
        Next

        _writer.WriteLine(")")

        '3. Generate the End Function
        '----------------------------
        _writer.WriteLine("        End Function")
        _writer.WriteLine()

    End Sub

    ' Generate a parameter corresponding to a node structure field
    Private Sub GenerateNodeStructureFieldParameter(field As ParseNodeField, Optional conflictName As String = Nothing)
        _writer.Write("{0} As {1}", FieldParamName(field, conflictName), FieldTypeRef(field))
    End Sub

    Private Function CanBeIdentifierToken(child As ParseNodeChild) As Boolean
        Dim childKind = TryCast(child.ChildKind, ParseNodeKind)
        If childKind IsNot Nothing Then
            Return childKind.Name = "IdentifierToken"
        End If
        Dim childKinds = TryCast(child.ChildKind, List(Of ParseNodeKind))
        Return childKinds IsNot Nothing AndAlso childKinds.Any(Function(k) k.Name = "IdentifierToken")
    End Function

    ' Generate a parameter corresponding to a node structure child
    Private Sub GenerateNodeStructureChildParameter(node As ParseNodeStructure, child As ParseNodeChild, Optional conflictName As String = Nothing, Optional makeOptional As Boolean = False, Optional substituteString As Boolean = False, Optional substituteParamArray As Boolean = False)
        Dim type = ChildFactoryTypeRef(node, child, False, False)

        If substituteString And CanBeIdentifierToken(child) Then
            type = "String"
        End If

        If Not makeOptional Then
            If substituteParamArray AndAlso child.IsList Then
                _writer.Write("ParamArray {0} As {1}()", ChildParamName(child, conflictName), BaseTypeReference(child))
            Else
                _writer.Write("{0} As {1}", ChildParamName(child, conflictName), type)
            End If
        Else
            _writer.Write("Optional {0} As {1} = Nothing", ChildParamName(child, conflictName), type)
        End If
    End Sub
End Class
