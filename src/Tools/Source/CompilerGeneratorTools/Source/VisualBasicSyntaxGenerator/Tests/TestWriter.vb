﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------------------------------------
' This is the code that actually outputs the VB code that defines the tree. It is passed a read and validated
' ParseTree, and outputs the code to defined the node classes for that tree, and also additional data
' structures like the kinds, visitor, etc.
'-----------------------------------------------------------------------------------------------------------

Imports System.IO

Public Class TestWriter
    Inherits WriteUtils

    Private ReadOnly _checksum As String
    Private _writer As TextWriter    'output is sent here.
    Private Const s_externalSourceDirectiveString As String = "ExternalSourceDirective"

    ' Initialize the class with the parse tree to write.
    Public Sub New(parseTree As ParseTree, checksum As String)
        MyBase.New(parseTree)
        _checksum = checksum
    End Sub

    ' Write out the code defining the tree to the give file.
    Public Sub WriteTestCode(writer As TextWriter)
        _writer = writer

        GenerateFile()
    End Sub

    Private Sub GenerateFile()
        _writer.WriteLine("' Tests for parse trees.")
        _writer.WriteLine("' DO NOT HAND EDIT")
        _writer.WriteLine()

        GenerateNamespace()
    End Sub

    Private Sub GenerateNamespace()
        _writer.WriteLine()
        _writer.WriteLine("Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests")
        _writer.WriteLine()

        _writer.WriteLine("Partial Public Class GeneratedTests")
        _writer.WriteLine()

        _writer.WriteLine("#region ""Green Factory Calls""")
        GenerateFactoryCalls(True)
        _writer.WriteLine("#end region")
        _writer.WriteLine()

        _writer.WriteLine("#region ""Green Factory Tests""")
        GenerateFactoryCallTests(True)
        _writer.WriteLine("#end region")
        _writer.WriteLine()

        _writer.WriteLine("#region ""Green Rewriter Tests""")
        GenerateRewriterTests(True)
        _writer.WriteLine("#end region")
        _writer.WriteLine()

        _writer.WriteLine("#region ""Green Visitor Tests""")
        GenerateVisitorTests(True)
        _writer.WriteLine("#end region")
        _writer.WriteLine()

        _writer.WriteLine("#region ""Red Factory Calls""")
        GenerateFactoryCalls(False)
        _writer.WriteLine("#end region")
        _writer.WriteLine()

        _writer.WriteLine("#region ""Red Factory Tests""")
        GenerateFactoryCallTests(False)
        _writer.WriteLine("#end region")
        _writer.WriteLine()

        _writer.WriteLine("#region ""Red Rewriter Tests""")
        GenerateRewriterTests(False)
        _writer.WriteLine("#end region")
        _writer.WriteLine()

        _writer.WriteLine("End Class")
        _writer.WriteLine("End Namespace")
    End Sub

    Private Sub GenerateFactoryCalls(isGreen As Boolean)
        For Each nodeStructure In _parseTree.NodeStructures.Values
            If Not nodeStructure.Abstract AndAlso Not nodeStructure.NoFactory Then

                If Not nodeStructure.Name = "KeywordSyntax" AndAlso Not nodeStructure.Name = "PunctuationSyntax" Then
                    GenerateFactoryCall(isGreen, nodeStructure)
                End If
            End If
        Next
    End Sub

    Private Sub GenerateFactoryCall(isGreen As Boolean, nodeStructure As ParseNodeStructure)
        For Each kind In nodeStructure.NodeKinds
            GenerateFactoryCall(isGreen, nodeStructure, kind)
        Next

        'If nodeStructure.NodeKinds.Count > 1 Then
        '    GenerateFactoryCall(isGreen, nodeStructure, Nothing)
        'End If

    End Sub

    Private Sub GenerateFactoryCall(isGreen As Boolean, nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind)

        If nodeKind.Name = "AttributeTarget" AndAlso Not isGreen Then
            Dim x = 0
        End If

        If nodeKind.Name.Contains(s_externalSourceDirectiveString) Then
            Return ' check for fix
        End If

        Dim namespacePrefix As String = If(isGreen, "InternalSyntax.", String.Empty)

        _writer.Write("        Private Shared Function ")

        Dim functionName As String = If(nodeKind Is Nothing, FactoryName(nodeStructure), FactoryName(nodeKind))


        If isGreen Then
            _writer.Write("GenerateGreen" + functionName)
        Else
            _writer.Write("GenerateRed" + functionName)
        End If

        If isGreen Then
            _writer.WriteLine("() As " + namespacePrefix + nodeStructure.Name)
        Else

            If nodeStructure.IsToken Then
                _writer.WriteLine("() As SyntaxToken")
            ElseIf nodeStructure.IsTrivia Then
                _writer.WriteLine("() As SyntaxTrivia")
            Else
                _writer.WriteLine("() As {0}", StructureTypeName(nodeStructure))
            End If
        End If

        Dim first As Boolean = True

        Dim callTokens As List(Of String) = New List(Of String)()
        Dim anePositions As List(Of Integer) = New List(Of Integer)()
        Dim aePositions As List(Of Integer) = New List(Of Integer)()
        Dim KindNonePositions As List(Of Integer) = New List(Of Integer)()

        Dim currentLine = 1

        If nodeKind Is Nothing Then
            callTokens.Add(namespacePrefix + "SyntaxFactory." + nodeStructure.Name + "(")
        Else
            callTokens.Add(namespacePrefix + "SyntaxFactory." + nodeKind.Name + "(")
        End If

        If nodeStructure.IsToken Then

            If isGreen Then
                If nodeStructure.IsTerminal Then
                    callTokens.Add("String.Empty")
                    first = False
                End If
            Else
                If Not first Then callTokens.Add(", ")
                first = False

                callTokens.Add(namespacePrefix + "SyntaxFactory.TriviaList(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "" ""))")

                'If nodeStructure.Name.Contains("Xml") Then
                '    callTokens.Add("String.Empty")
                '    first = False
                'End If

                If nodeKind.Name.EndsWith("LiteralToken", StringComparison.Ordinal) OrElse
                   nodeKind.Name.EndsWith("XmlNameToken", StringComparison.Ordinal) OrElse
                   nodeKind.Name.EndsWith("DocumentationCommentLineBreakToken", StringComparison.Ordinal) OrElse
                   nodeKind.Name = "InterpolatedStringTextToken" _
                Then
                    If Not first Then callTokens.Add(", ")
                    callTokens.Add("String.Empty")
                    first = False
                End If

            End If

            Dim fields = GetAllFieldsOfStructure(nodeStructure)

            For Each field In fields

                If Not first Then callTokens.Add(", ")
                first = False

                Dim fieldType = FieldTypeRef(field)
                callTokens.Add(GetInitValueForType(fieldType))

            Next

            If Not first Then callTokens.Add(", ")
            first = False

            If isGreen Then
                callTokens.Add(namespacePrefix + "SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "" ""), ")
                callTokens.Add(namespacePrefix + "SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "" ""))")
            Else
                callTokens.Add(namespacePrefix + "SyntaxFactory.TriviaList(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "" "")))")
            End If

            If isGreen Then
                _writer.Write("            return ")
                callTokens.ForEach(AddressOf _writer.Write)
                _writer.WriteLine()
            Else
                _writer.WriteLine("            Dim exceptionTest as boolean = false")

                For exceptionChecks = 0 To anePositions.Count - 1

                    _writer.WriteLine("            Try")

                    For i = 0 To callTokens.Count - 1
                        If (i <> anePositions(exceptionChecks)) Then
                            _writer.Write(callTokens(i))
                        Else
                            _writer.Write("Nothing")
                        End If
                    Next

                    _writer.WriteLine("            catch e as ArgumentNullException")
                    _writer.WriteLine("            exceptionTest = true")
                    _writer.WriteLine("            End Try")
                    _writer.WriteLine("            Debug.Assert(exceptionTest)")
                    _writer.WriteLine("            exceptionTest = false")
                    _writer.WriteLine()
                Next

                ' quick hack to cover more code in keyword factories ...
                If nodeStructure.IsTerminal AndAlso Not nodeStructure.IsTrivia AndAlso nodeStructure.Name = "KeywordSyntax" Then

                    _writer.Write("            Dim node1 = ")
                    For i = 0 To callTokens.Count - 1
                        _writer.Write(callTokens(i))
                        If i = 0 Then _writer.Write("String.Empty, ")
                    Next i
                    _writer.WriteLine()

                    _writer.Write("            dim node2 = ")
                    callTokens.ForEach(AddressOf _writer.Write)
                    _writer.WriteLine()

                    _writer.WriteLine("            Debug.Assert(node1.GetText() = String.Empty)")
                    _writer.WriteLine("            Debug.Assert(node2.GetText() <> String.Empty)")
                    _writer.WriteLine()

                    ' make parameter = nothing to cause exceptions
                    _writer.WriteLine("            Try")
                    _writer.WriteLine("            exceptionTest = false")

                    For i = 0 To callTokens.Count - 1
                        _writer.Write(callTokens(i))
                        If i = 0 Then _writer.Write("Nothing, ")
                    Next i
                    _writer.WriteLine()

                    _writer.WriteLine("            catch e as ArgumentNullException")
                    _writer.WriteLine("            exceptionTest = true")
                    _writer.WriteLine("            End Try")
                    _writer.WriteLine("            Debug.Assert(exceptionTest)")
                    _writer.WriteLine()

                    _writer.WriteLine("            return node2")
                Else
                    _writer.Write("            return ")
                    callTokens.ForEach(AddressOf _writer.Write)
                    _writer.WriteLine()

                End If

            End If

        Else

            Dim children = GetAllFactoryChildrenOfStructure(nodeStructure)

            If nodeStructure.IsTrivia Then
                callTokens.Add("String.Empty")
                anePositions.Add(callTokens.Count - 1)
                first = False
            End If

            For Each child In children
                If Not first Then
                    callTokens.Add(", ")
                End If

                If child.IsOptional Then
                    ' Hack: remove when the factory methods have been fixed to not contain overloads.
                    If nodeStructure.Name = "MemberAccessExpressionSyntax" Then
                        If first Then
                            callTokens.Add(String.Format("CType(Nothing, {0}{1})", namespacePrefix, ChildFieldTypeRef(child)))
                        End If
                    Else
                        callTokens.Add("Nothing")
                    End If

                    ' TODO: remove
                    first = False

                    Continue For
                End If

                ' TODO: move up.
                first = False

                Dim childNodeKind As ParseNodeKind = If(Not child Is Nothing, TryCast(child.ChildKind, ParseNodeKind), Nothing)

                If TypeOf child.ChildKind Is List(Of ParseNodeKind) Then
                    childNodeKind = child.ChildKind(nodeKind.Name)
                End If

                If childNodeKind Is Nothing Then
                    childNodeKind = DirectCast(child.ChildKind, List(Of ParseNodeKind)).Item(0)
                End If

                If child.IsList AndAlso child.IsSeparated Then
                    Dim childKindStructure = child.ParseTree.NodeStructures(childNodeKind.StructureId)
                    childKindStructure = If(Not childKindStructure.ParentStructure Is Nothing, childKindStructure.ParentStructure, childKindStructure)
                    callTokens.Add("New " + ChildFactoryTypeRef(nodeStructure, child, isGreen, True) + "()")
                Else
                    Dim structureOfchild = child.ParseTree.NodeStructures(childNodeKind.StructureId)
                    If structureOfchild.Name = "PunctuationSyntax" OrElse structureOfchild.Name = "KeywordSyntax" Then

                        If isGreen Then
                            callTokens.Add("new InternalSyntax." + structureOfchild.Name + "(")
                            callTokens.Add("SyntaxKind." + childNodeKind.Name + ", String.Empty, Nothing, Nothing)")
                        Else
                            Dim token = "SyntaxFactory.Token(SyntaxKind." + childNodeKind.Name + ")"
                            If child.IsList Then
                                token = "SyntaxTokenList.Create(" & token & ")"
                            End If
                            callTokens.Add(token)

                            ' add none kind here
                            If Not TypeOf child.ChildKind Is List(Of ParseNodeKind) Then
                                If child.IsOptional Then
                                    KindNonePositions.Add(callTokens.Count - 1)
                                End If
                            Else
                                If Not child.IsList Then
                                    aePositions.Add(callTokens.Count - 1)
                                End If
                            End If
                        End If
                    Else
                        If isGreen Then
                            callTokens.Add("GenerateGreen" + FactoryName(childNodeKind) + "()")
                        Else
                            Dim result = "GenerateRed" + FactoryName(childNodeKind) + "()"
                            If structureOfchild.IsToken AndAlso child.IsList Then
                                result = "SyntaxTokenList.Create(" & result & ")"
                            ElseIf child.IsSeparated Then
                                result = String.Format("SyntaxFactory.SingletonSeparatedList(Of {0}({1})", BaseTypeReference(child), result)
                            ElseIf child.IsList Then
                                result = String.Format("SyntaxFactory.SingletonList(Of {0})({1})", BaseTypeReference(child), result)
                            End If
                            callTokens.Add(result)
                        End If
                    End If

                    If Not KindTypeStructure(child.ChildKind).IsToken AndAlso Not child.IsList Then
                        anePositions.Add(callTokens.Count - 1)
                    ElseIf KindTypeStructure(child.ChildKind).IsToken And Not child.IsList Then
                        aePositions.Add(callTokens.Count - 1)
                    End If
                End If
            Next

            callTokens.Add(")")

            ' TODO: remove extra conditions
            If isGreen OrElse nodeStructure.Name = "CaseBlockSyntax" OrElse nodeStructure.Name = "IfPartSyntax" OrElse nodeStructure.Name = "MultiLineIfBlockSyntax" Then
                _writer.Write("            return ")
                callTokens.ForEach(AddressOf _writer.Write)
                _writer.WriteLine()
            Else

                _writer.WriteLine("            Dim exceptionTest as boolean = false")

                For exceptionChecks = 0 To anePositions.Count - 1

                    _writer.WriteLine("            Try")

                    _writer.Write("            ")
                    For i = 0 To callTokens.Count - 1
                        If (i <> anePositions(exceptionChecks)) Then
                            _writer.Write(callTokens(i))
                        Else
                            _writer.Write("Nothing")
                        End If
                    Next
                    _writer.WriteLine()

                    _writer.WriteLine("            catch e as ArgumentNullException")
                    _writer.WriteLine("            exceptionTest = true")
                    _writer.WriteLine("            End Try")
                    _writer.WriteLine("            Debug.Assert(exceptionTest)")
                    _writer.WriteLine("            exceptionTest = false")
                    _writer.WriteLine()
                Next

                For exceptionChecks = 0 To aePositions.Count - 1

                    _writer.WriteLine("            Try")
                    _writer.Write("            ")
                    For i = 0 To callTokens.Count - 1
                        If (i <> aePositions(exceptionChecks)) Then
                            _writer.Write(callTokens(i))
                        Else
                            _writer.Write("SyntaxFactory.Token(SyntaxKind.ExternalSourceKeyword)") ' this syntaxtoken should not be legal anywhere in the tests
                        End If
                    Next
                    _writer.WriteLine()

                    _writer.WriteLine("            catch e as ArgumentException")
                    _writer.WriteLine("            exceptionTest = true")
                    _writer.WriteLine("            End Try")
                    _writer.WriteLine("            Debug.Assert(exceptionTest)")
                    _writer.WriteLine("            exceptionTest = false")
                    _writer.WriteLine()
                Next

                For exceptionChecks = 0 To KindNonePositions.Count - 1

                    _writer.Write("            ")
                    For i = 0 To callTokens.Count - 1
                        If (i <> KindNonePositions(exceptionChecks)) Then
                            _writer.Write(callTokens(i))
                        Else
                            _writer.Write("New SyntaxToken(Nothing, New InternalSyntax.KeywordSyntax(SyntaxKind.None, Nothing, Nothing, """", Nothing, Nothing), 0, 0)")
                        End If
                    Next
                    _writer.WriteLine()
                    _writer.WriteLine()
                Next


                _writer.Write("            return ")
                callTokens.ForEach(AddressOf _writer.Write)
                _writer.WriteLine()
            End If

        End If

        _writer.WriteLine("        End Function")
        _writer.WriteLine()
    End Sub

    Private Sub GenerateFactoryCallTests(isGreen As Boolean)
        For Each nodeStructure In _parseTree.NodeStructures.Values
            If Not nodeStructure.Abstract AndAlso Not nodeStructure.NoFactory Then
                If Not nodeStructure.Name = "KeywordSyntax" AndAlso Not nodeStructure.Name = "PunctuationSyntax" Then
                    GenerateFactoryCallTest(isGreen, nodeStructure)
                End If
            End If
        Next
    End Sub

    Private Sub GenerateFactoryCallTest(isGreen As Boolean, nodeStructure As ParseNodeStructure)
        For Each kind In nodeStructure.NodeKinds
            GenerateFactoryCallTest(isGreen, nodeStructure, kind)
        Next
    End Sub

    Private Sub GenerateFactoryCallTest(isGreen As Boolean, nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind)

        If nodeKind.Name.Contains(s_externalSourceDirectiveString) Then
            Return ' check for fix
        End If

        Dim funcNamePart = If(isGreen, "Green", "Red")

        _writer.WriteLine("        <Fact>")
        _writer.Write("        Public Sub ")

        _writer.Write("Test{0}{1}", funcNamePart, FactoryName(nodeKind))
        _writer.WriteLine("()")

        _writer.WriteLine("            dim objectUnderTest = Generate{0}{1}()", funcNamePart, FactoryName(nodeKind))

        'Dim children = GetAllChildrenOfStructure(nodeStructure)

        If isGreen Then
            _writer.WriteLine("            AttachAndCheckDiagnostics(objectUnderTest)")
        Else
            Dim withStat As String = Nothing
            For Each child In GetAllChildrenOfStructure(nodeStructure)
                If Not child.IsOptional Then
                    _writer.WriteLine("            Assert.NotNull(objectUnderTest.{0})", LowerFirstCharacter(child.Name))
                End If
                withStat += String.Format(".With{0}(objectUnderTest.{0})", child.Name)
            Next
            If (withStat IsNot Nothing) Then
                _writer.WriteLine("            Dim withObj = objectUnderTest{0}", withStat)
                _writer.WriteLine("            Assert.Equal(withobj, objectUnderTest)")
            End If
        End If

        _writer.WriteLine("        End Sub")
        _writer.WriteLine()
    End Sub


    Private Sub GenerateRewriterTests(isGreen As Boolean)
        For Each nodeStructure In _parseTree.NodeStructures.Values
            If Not nodeStructure.Abstract AndAlso Not nodeStructure.NoFactory AndAlso Not nodeStructure.IsToken AndAlso Not nodeStructure.IsTrivia Then
                GenerateRewriterTest(isGreen, nodeStructure)
            End If
        Next
    End Sub

    Private Sub GenerateRewriterTest(isGreen As Boolean, nodeStructure As ParseNodeStructure)
        For Each kind In nodeStructure.NodeKinds
            GenerateRewriterTest(isGreen, nodeStructure, kind)
        Next
    End Sub

    Private Sub GenerateRewriterTest(isGreen As Boolean, nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind)

        If nodeKind.Name.Contains(s_externalSourceDirectiveString) Then
            Return ' check for fix
        End If

        Dim funcNamePart = If(isGreen, "Green", "Red")

        _writer.WriteLine("        <Fact>")
        _writer.Write("        Public Sub ")

        _writer.Write("Test{0}{1}Rewriter", funcNamePart, FactoryName(nodeKind))
        _writer.WriteLine("()")

        _writer.WriteLine("            dim oldNode = Generate{0}{1}()", funcNamePart, FactoryName(nodeKind))
        _writer.WriteLine("            Dim rewriter = New {0}IdentityRewriter()", funcNamePart)
        _writer.WriteLine("            Dim newNode = rewriter.Visit(oldNode)")
        _writer.WriteLine("            Assert.Equal(oldNode, newNode)")
        _writer.WriteLine("        End Sub")
        _writer.WriteLine()

    End Sub

    Private Sub GenerateVisitorTests(isGreen As Boolean)
        For Each nodeStructure In _parseTree.NodeStructures.Values
            If Not nodeStructure.Abstract AndAlso Not nodeStructure.NoFactory AndAlso Not nodeStructure.IsToken AndAlso Not nodeStructure.IsTrivia Then
                GenerateVisitorTest(isGreen, nodeStructure)
            End If
        Next
    End Sub

    Private Sub GenerateVisitorTest(isGreen As Boolean, nodeStructure As ParseNodeStructure)
        For Each kind In nodeStructure.NodeKinds
            GenerateVisitorTest(isGreen, nodeStructure, kind)
        Next
    End Sub

    Private Sub GenerateVisitorTest(isGreen As Boolean, nodeStructure As ParseNodeStructure, nodeKind As ParseNodeKind)

        If nodeKind.Name.Contains(s_externalSourceDirectiveString) Then
            Return ' check for fix
        End If

        Dim funcNamePart = If(isGreen, "Green", "Red")

        _writer.WriteLine("        <Fact>")
        _writer.Write("        Public Sub ")

        _writer.Write("Test" + funcNamePart + FactoryName(nodeKind) + "Visitor")
        _writer.WriteLine("()")

        _writer.WriteLine("            Dim oldNode = Generate" + funcNamePart + FactoryName(nodeKind) + "()")
        _writer.WriteLine("            Dim visitor = New " + funcNamePart + "NodeVisitor()")
        _writer.WriteLine("            visitor.Visit(oldNode)")
        _writer.WriteLine("        End Sub")
        _writer.WriteLine()
    End Sub

    Public Function GetInitValueForType(fieldType As String) As String
        Select Case fieldType
            Case "Integer"
                Return "23"
            Case "String"
                Return """Bar"""
            Case "Char"
                Return """E""C"
            Case "DateTime"
                Return "New DateTime(2008,11,04)"
            Case "System.Decimal"
                Return "42"
            Case "TypeCharacter"
                Return "TypeCharacter.DecimalLiteral"
            Case "SyntaxKind"
                Return "SyntaxKind.IdentifierName"
            Case Else
                Return "Unknown Type"
        End Select
    End Function

    Public Sub AddHandwrittenFactoryCall(baseType As String)

        Select Case baseType
            Case "IdentifierTokenSyntax"
                _writer.Write("new InternalSyntax.SimpleIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, ""text"",")
                _writer.Write("InternalSyntax.SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "" ""), ")
                _writer.Write("InternalSyntax.SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "" ""))")

            Case "IntegerLiteralTokenSyntax"
                _writer.Write("new InternalSyntax.IntegerLiteralToken(""42"", LiteralBase.Decimal, TypeCharacter.None, 42,")
                _writer.Write("InternalSyntax.SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "" ""), ")
                _writer.Write("InternalSyntax.SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "" ""))")

        End Select
    End Sub

End Class
