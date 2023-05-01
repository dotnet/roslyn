' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Class VisualBasicDeclarationComparer
        Implements IComparer(Of SyntaxNode)

        Private Shared ReadOnly s_kindPrecedenceMap As Dictionary(Of SyntaxKind, Integer) = New Dictionary(Of SyntaxKind, Integer)(SyntaxFacts.EqualityComparer) From
            {
                {SyntaxKind.FieldDeclaration, 0},
                {SyntaxKind.ConstructorBlock, 1},
                {SyntaxKind.SubNewStatement, 1},
                {SyntaxKind.PropertyBlock, 2},
                {SyntaxKind.PropertyStatement, 2},
                {SyntaxKind.EventBlock, 3},
                {SyntaxKind.EventStatement, 3},
                {SyntaxKind.SubBlock, 4},
                {SyntaxKind.SubStatement, 4},
                {SyntaxKind.FunctionBlock, 5},
                {SyntaxKind.FunctionStatement, 5},
                {SyntaxKind.OperatorBlock, 6},
                {SyntaxKind.OperatorStatement, 6},
                {SyntaxKind.EnumBlock, 7},
                {SyntaxKind.InterfaceBlock, 8},
                {SyntaxKind.StructureBlock, 9},
                {SyntaxKind.ClassBlock, 10},
                {SyntaxKind.ModuleBlock, 11},
                {SyntaxKind.DelegateSubStatement, 12},
                {SyntaxKind.DelegateFunctionStatement, 12}
            }

        Private Shared ReadOnly s_operatorPrecedenceMap As Dictionary(Of SyntaxKind, Integer) = New Dictionary(Of SyntaxKind, Integer)(SyntaxFacts.EqualityComparer) From
            {
                {SyntaxKind.PlusToken, 0},
                {SyntaxKind.MinusToken, 1},
                {SyntaxKind.AsteriskToken, 3},
                {SyntaxKind.SlashToken, 4},
                {SyntaxKind.BackslashToken, 5},
                {SyntaxKind.CaretToken, 6},
                {SyntaxKind.AmpersandToken, 7},
                {SyntaxKind.NotKeyword, 8},
                {SyntaxKind.LikeKeyword, 9},
                {SyntaxKind.ModKeyword, 10},
                {SyntaxKind.AndKeyword, 11},
                {SyntaxKind.OrKeyword, 12},
                {SyntaxKind.XorKeyword, 13},
                {SyntaxKind.LessThanLessThanToken, 14},
                {SyntaxKind.GreaterThanGreaterThanToken, 15},
                {SyntaxKind.EqualsToken, 16},
                {SyntaxKind.LessThanGreaterThanToken, 17},
                {SyntaxKind.GreaterThanToken, 18},
                {SyntaxKind.LessThanToken, 19},
                {SyntaxKind.GreaterThanEqualsToken, 20},
                {SyntaxKind.LessThanEqualsToken, 21},
                {SyntaxKind.IsTrueKeyword, 22},
                {SyntaxKind.IsFalseKeyword, 23},
                {SyntaxKind.CTypeKeyword, 24}
            }

        Public Shared ReadOnly WithNamesInstance As New VisualBasicDeclarationComparer(includeName:=True)
        Public Shared ReadOnly WithoutNamesInstance As New VisualBasicDeclarationComparer(includeName:=False)

        Private ReadOnly _includeName As Boolean

        Private Sub New(includeName As Boolean)
            _includeName = includeName
        End Sub

        Public Function Compare(x As SyntaxNode, y As SyntaxNode) As Integer Implements IComparer(Of SyntaxNode).Compare
            Dim xPrecedence As Integer
            Dim yPrecedence As Integer
            If Not s_kindPrecedenceMap.TryGetValue(x.Kind(), xPrecedence) OrElse
               Not s_kindPrecedenceMap.TryGetValue(y.Kind(), yPrecedence) Then
                ' The containing definition is malformed and contains a node kind we didn't expect.
                ' Ignore comparisons with those unexpected nodes and sort them to the end of the declaration.
                Return 1
            End If

            If xPrecedence <> yPrecedence Then
                Return If(xPrecedence < yPrecedence, -1, 1)
            End If

            x = ConvertBlockToStatement(x)
            y = ConvertBlockToStatement(y)

            Select Case x.Kind
                Case SyntaxKind.DelegateSubStatement,
                     SyntaxKind.DelegateFunctionStatement
                    Return Compare(DirectCast(x, DelegateStatementSyntax), DirectCast(y, DelegateStatementSyntax))

                Case SyntaxKind.FieldDeclaration
                    Return Compare(DirectCast(x, FieldDeclarationSyntax), DirectCast(y, FieldDeclarationSyntax))

                Case SyntaxKind.SubNewStatement
                    Return Compare(DirectCast(x, SubNewStatementSyntax), DirectCast(y, SubNewStatementSyntax))

                Case SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement
                    Return Compare(DirectCast(x, MethodStatementSyntax), DirectCast(y, MethodStatementSyntax))

                Case SyntaxKind.EventStatement
                    Return Compare(DirectCast(x, EventStatementSyntax), DirectCast(y, EventStatementSyntax))

                Case SyntaxKind.PropertyStatement
                    Return Compare(DirectCast(x, PropertyStatementSyntax), DirectCast(y, PropertyStatementSyntax))

                Case SyntaxKind.OperatorStatement
                    Return Compare(DirectCast(x, OperatorStatementSyntax), DirectCast(y, OperatorStatementSyntax))

                Case SyntaxKind.EnumStatement
                    Return Compare(DirectCast(x, EnumStatementSyntax), DirectCast(y, EnumStatementSyntax))

                Case SyntaxKind.InterfaceStatement,
                     SyntaxKind.StructureStatement,
                     SyntaxKind.ClassStatement,
                     SyntaxKind.ModuleStatement
                    Return Compare(DirectCast(x, TypeStatementSyntax), DirectCast(y, TypeStatementSyntax))
            End Select

            throw ExceptionUtilities.UnexpectedValue(x.Kind)
        End Function

        Private Shared Function ConvertBlockToStatement(node As SyntaxNode) As SyntaxNode
            Select Case node.Kind
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(node, PropertyBlockSyntax).PropertyStatement

                Case SyntaxKind.EventBlock
                    Return DirectCast(node, EventBlockSyntax).EventStatement

                Case SyntaxKind.ConstructorBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.OperatorBlock
                    Return DirectCast(node, MethodBlockBaseSyntax).BlockStatement

                Case SyntaxKind.EnumBlock
                    Return DirectCast(node, EnumBlockSyntax).EnumStatement

                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).BlockStatement

                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).BlockStatement

                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).BlockStatement

                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).BlockStatement
            End Select

            Return node
        End Function

        Private Function Compare(x As DelegateStatementSyntax, y As DelegateStatementSyntax) As Integer
            Dim result = 0
            If EqualAccessibility(x, x.Modifiers, y, y.Modifiers, result) Then

                If _includeName Then
                    EqualIdentifierName(x.Identifier, y.Identifier, result)
                End If
            End If

            Return result
        End Function

        Private Function Compare(x As FieldDeclarationSyntax, y As FieldDeclarationSyntax) As Integer
            Dim result = 0
            If EqualConstness(x.Modifiers, y.Modifiers, result) AndAlso
               EqualSharedness(x.Modifiers, y.Modifiers, result) AndAlso
               EqualReadOnlyNess(x.Modifiers, y.Modifiers, result) AndAlso
               EqualAccessibility(x, x.Modifiers, y, y.Modifiers, result) Then

                If _includeName Then
                    EqualIdentifierName(
                    x.Declarators.FirstOrDefault().Names.FirstOrDefault().Identifier,
                    y.Declarators.FirstOrDefault().Names.FirstOrDefault().Identifier,
                    result)
                End If
            End If

            Return result
        End Function

        Private Shared Function Compare(x As SubNewStatementSyntax, y As SubNewStatementSyntax) As Integer
            Dim result = 0
            If EqualSharedness(x.Modifiers, y.Modifiers, result) AndAlso
               EqualAccessibility(x, x.Modifiers, y, y.Modifiers, result) Then

                EqualParameterLists(x.ParameterList, y.ParameterList, result)
            End If

            Return result
        End Function

        Private Function Compare(x As MethodStatementSyntax, y As MethodStatementSyntax) As Integer
            Dim result = 0
            If EqualSharedness(x.Modifiers, y.Modifiers, result) AndAlso
               EqualAccessibility(x, x.Modifiers, y, y.Modifiers, result) Then

                If _includeName Then
                    EqualIdentifierName(x.Identifier, y.Identifier, result)
                End If
            End If

            Return result
        End Function

        Private Function Compare(x As EventStatementSyntax, y As EventStatementSyntax) As Integer
            Dim result = 0
            If EqualSharedness(x.Modifiers, y.Modifiers, result) AndAlso
               EqualAccessibility(x, x.Modifiers, y, y.Modifiers, result) Then

                If _includeName Then
                    EqualIdentifierName(x.Identifier, y.Identifier, result)
                End If
            End If

            Return result
        End Function

        Private Function Compare(x As PropertyStatementSyntax, y As PropertyStatementSyntax) As Integer
            Dim result = 0
            If EqualSharedness(x.Modifiers, y.Modifiers, result) AndAlso
               EqualAccessibility(x, x.Modifiers, y, y.Modifiers, result) Then

                If _includeName Then
                    EqualIdentifierName(x.Identifier, y.Identifier, result)
                End If
            End If

            Return result
        End Function

        Private Shared Function Compare(x As OperatorStatementSyntax, y As OperatorStatementSyntax) As Integer
            Dim result = 0
            If EqualOperatorPrecedence(x.OperatorToken, y.OperatorToken, result) Then
                EqualParameterLists(x.ParameterList, y.ParameterList, result)
            End If

            Return result
        End Function

        Private Function Compare(x As EnumStatementSyntax, y As EnumStatementSyntax) As Integer
            Dim result = 0
            If EqualAccessibility(x, x.Modifiers, y, y.Modifiers, result) Then
                If _includeName Then
                    EqualIdentifierName(x.Identifier, y.Identifier, result)
                End If
            End If

            Return result
        End Function

        Private Function Compare(x As TypeStatementSyntax, y As TypeStatementSyntax) As Integer
            Dim result = 0
            If EqualSharedness(x.Modifiers, y.Modifiers, result) AndAlso
               EqualAccessibility(x, x.Modifiers, y, y.Modifiers, result) Then

                If _includeName Then
                    EqualIdentifierName(x.Identifier, y.Identifier, result)
                End If
            End If

            Return result
        End Function

        Private Shared Function NeitherNull(x As Object, y As Object, ByRef comparisonResult As Integer) As Boolean
            If x Is Nothing AndAlso y Is Nothing Then
                comparisonResult = 0
                Return False
            ElseIf x Is Nothing Then
                comparisonResult = -1
                Return False
            ElseIf y Is Nothing Then
                comparisonResult = 1
                Return False
            Else
                comparisonResult = 0
                Return True
            End If
        End Function

        Private Shared Function ContainsToken(list As IEnumerable(Of SyntaxToken), kind As SyntaxKind) As Boolean
            Return list.Contains(Function(token As SyntaxToken)
                                     Return token.Kind = kind
                                 End Function)
        End Function

        Private Enum Accessibility
            [Public]
            [Protected]
            [ProtectedFriend]
            [Friend]
            [Private]
        End Enum

        Private Shared Function GetAccessibilityPrecedence(declaration As SyntaxNode, modifiers As IEnumerable(Of SyntaxToken)) As Integer
            If ContainsToken(modifiers, SyntaxKind.PublicKeyword) Then
                Return Accessibility.Public
            ElseIf ContainsToken(modifiers, SyntaxKind.ProtectedKeyword) Then
                If ContainsToken(modifiers, SyntaxKind.FriendKeyword) Then
                    Return Accessibility.ProtectedFriend
                End If

                Return Accessibility.Protected
            ElseIf ContainsToken(modifiers, SyntaxKind.FriendKeyword) Then
                Return Accessibility.Friend
            ElseIf ContainsToken(modifiers, SyntaxKind.PrivateKeyword) Then
                Return Accessibility.Private
            End If

            ' Determine default accessibility
            Select Case declaration.Kind
                Case SyntaxKind.InterfaceStatement,
                     SyntaxKind.ModuleStatement,
                     SyntaxKind.ClassStatement,
                     SyntaxKind.StructureStatement

                    ' Convert type definition statements to their corresponding blocks so we can traverse up the tree
                    declaration = declaration.Parent
            End Select

            Dim node = declaration.Parent
            While node IsNot Nothing
                Select Case node.Kind
                    Case SyntaxKind.InterfaceBlock
                        ' Interface members are all Public
                        Return Accessibility.Public

                    Case SyntaxKind.ModuleBlock,
                         SyntaxKind.ClassBlock
                        ' Standard module and class members default to Public unless they are variable declarations
                        Return If(declaration.Kind = SyntaxKind.FieldDeclaration, Accessibility.Private, Accessibility.Public)

                    Case SyntaxKind.StructureBlock
                        ' Structure member declarations always default to Public
                        Return Accessibility.Public
                End Select

                node = node.Parent
            End While

            ' Namespace members default to Friend
            Return Accessibility.Friend
        End Function

        Private Shared Function BothHaveModifier(x As SyntaxTokenList, y As SyntaxTokenList, modifierKind As SyntaxKind, ByRef comparisonResult As Integer) As Boolean
            Dim xHasModifier = ContainsToken(x, modifierKind)
            Dim yHasModifier = ContainsToken(y, modifierKind)

            If xHasModifier = yHasModifier Then
                comparisonResult = 0
                Return True
            End If

            comparisonResult = If(xHasModifier, -1, 1)
            Return False
        End Function

        Private Shared Function EqualSharedness(x As SyntaxTokenList, y As SyntaxTokenList, ByRef comparisonResult As Integer) As Boolean
            Return BothHaveModifier(x, y, SyntaxKind.SharedKeyword, comparisonResult)
        End Function

        Private Shared Function EqualReadOnlyness(x As SyntaxTokenList, y As SyntaxTokenList, ByRef comparisonResult As Integer) As Boolean
            Return BothHaveModifier(x, y, SyntaxKind.ReadOnlyKeyword, comparisonResult)
        End Function

        Private Shared Function EqualConstness(x As SyntaxTokenList, y As SyntaxTokenList, ByRef comparisonResult As Integer) As Boolean
            Return BothHaveModifier(x, y, SyntaxKind.ConstKeyword, comparisonResult)
        End Function

        Private Shared Function EqualAccessibility(x As SyntaxNode, xModifiers As SyntaxTokenList, y As SyntaxNode, yModifiers As SyntaxTokenList, ByRef comparisonResult As Integer) As Boolean
            Dim xAccessibility = GetAccessibilityPrecedence(x, xModifiers)
            Dim yAccessibility = GetAccessibilityPrecedence(y, yModifiers)

            If xAccessibility = yAccessibility Then
                comparisonResult = 0
                Return True
            End If

            comparisonResult = If(xAccessibility < yAccessibility, -1, 1)
            Return False
        End Function

        Private Shared Function EqualIdentifierName(x As SyntaxToken, y As SyntaxToken, ByRef comparisonResult As Integer) As Boolean
            comparisonResult = CaseInsensitiveComparison.Compare(x.ValueText, y.ValueText)
            Return comparisonResult = 0
        End Function

        Private Shared Function EqualOperatorPrecedence(x As SyntaxToken, y As SyntaxToken, ByRef comparisonResult As Integer) As Boolean
            Dim xPrecedence = 0
            Dim yPrecedence = 0

            s_operatorPrecedenceMap.TryGetValue(x.Kind, xPrecedence)
            s_operatorPrecedenceMap.TryGetValue(y.Kind, yPrecedence)

            comparisonResult = If(xPrecedence = yPrecedence, 0, If(xPrecedence < yPrecedence, -1, 1))
            Return comparisonResult = 0
        End Function

        Private Shared Function EqualParameterLists(x As ParameterListSyntax, y As ParameterListSyntax, ByRef comparisonResult As Integer) As Boolean
            Dim result = 0
            If NeitherNull(x, y, result) Then
                Dim xParameterCount = x.Parameters.Count
                Dim yParameterCount = y.Parameters.Count

                comparisonResult = If(xParameterCount = yParameterCount, 0, If(x.Parameters.Count < y.Parameters.Count, -1, 1))
            End If

            Return comparisonResult = 0
        End Function
    End Class
End Namespace
