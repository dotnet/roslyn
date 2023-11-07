' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    Friend NotInheritable Class SyntaxUtilities
        Public Shared Function CreateLambdaBody(node As SyntaxNode) As LambdaBody
            Return New VisualBasicLambdaBody(node)
        End Function

        Public Shared Function TryGetDeclarationBody(node As SyntaxNode) As MemberBody
            Select Case node.Kind
                Case SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.OperatorBlock,
                    SyntaxKind.GetAccessorBlock,
                    SyntaxKind.SetAccessorBlock,
                    SyntaxKind.AddHandlerAccessorBlock,
                    SyntaxKind.RemoveHandlerAccessorBlock,
                    SyntaxKind.RaiseEventAccessorBlock
                    Return New MethodBody(DirectCast(node, MethodBlockBaseSyntax))

                Case SyntaxKind.PropertyStatement
                    Dim propertyStatement = DirectCast(node, PropertyStatementSyntax)
                    If propertyStatement.Initializer IsNot Nothing Then
                        Return New PropertyWithInitializerDeclarationBody(propertyStatement)
                    End If

                    If HasAsNewClause(propertyStatement) Then
                        Return New PropertyWithNewClauseDeclarationBody(propertyStatement)
                    End If

                    Return Nothing

                Case SyntaxKind.ModifiedIdentifier
                    Dim modifiedIdentifier = DirectCast(node, ModifiedIdentifierSyntax)

                    If Not node.IsParentKind(SyntaxKind.VariableDeclarator) Then
                        ' parameter
                        Return Nothing
                    End If

                    Dim variableDeclarator = DirectCast(node.Parent, VariableDeclaratorSyntax)
                    Dim fieldDeclaration = DirectCast(variableDeclarator.Parent, FieldDeclarationSyntax)

                    If fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword) Then
                        Return Nothing
                    End If

                    Dim body As MemberBody = Nothing

                    If IsFieldDeclaration(modifiedIdentifier) Then
                        ' Dim a, b As New C()
                        If HasAsNewClause(variableDeclarator) Then
                            body = New FieldWithMultipleAsNewClauseDeclarationBody(modifiedIdentifier)
                        End If

                        ' Dim a(n), b(n) As Integer
                        If modifiedIdentifier.ArrayBounds IsNot Nothing Then
                            ' AsNew clause can be syntactically specified at the same time as array bounds can be  (it's a semantic error).
                            ' Guard against such case to maintain consistency and set body to Nothing in that case.
                            body = If(body Is Nothing, New FieldWithMultipleArrayBoundsDeclarationBody(modifiedIdentifier), Nothing)
                        End If
                    Else
                        If variableDeclarator.Initializer IsNot Nothing Then
                            ' Dim a = initializer
                            body = New FieldWithInitializerDeclarationBody(variableDeclarator)
                        ElseIf HasAsNewClause(variableDeclarator) Then
                            ' Dim a As New T
                            body = New FieldWithSingleAsNewClauseDeclarationBody(variableDeclarator)
                        End If

                        ' Dim a(n) As T
                        Dim name = variableDeclarator.Names(0)

                        If name.ArrayBounds IsNot Nothing Then
                            ' Initializer and AsNew clause can't be syntactically specified at the same time, but array bounds can be (it's a semantic error).
                            ' Guard against such case to maintain consistency and set body to Nothing in that case.
                            body = If(body Is Nothing, New FieldWithSingleArrayBoundsDeclarationBody(variableDeclarator), Nothing)
                        End If
                    End If

                    Return body

                Case Else
                    Return Nothing
            End Select
        End Function

        Public Shared Function HasAsNewClause(variableDeclarator As VariableDeclaratorSyntax) As Boolean
            Return variableDeclarator.AsClause IsNot Nothing AndAlso variableDeclarator.AsClause.IsKind(SyntaxKind.AsNewClause)
        End Function

        Public Shared Function HasAsNewClause(propertyStatement As PropertyStatementSyntax) As Boolean
            Return propertyStatement.AsClause IsNot Nothing AndAlso propertyStatement.AsClause.IsKind(SyntaxKind.AsNewClause)
        End Function

        ''' <summary>
        ''' Returns true if the <see cref="ModifiedIdentifierSyntax"/> node represents a field declaration.
        ''' </summary>
        Friend Shared Function IsFieldDeclaration(node As ModifiedIdentifierSyntax) As Boolean
            Return node.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration) AndAlso DirectCast(node.Parent, VariableDeclaratorSyntax).Names.Count > 1
        End Function

        ''' <summary>
        ''' Returns true if the <see cref="VariableDeclaratorSyntax"/> node represents a field declaration.
        ''' </summary>
        Friend Shared Function IsFieldDeclaration(node As VariableDeclaratorSyntax) As Boolean
            Return node.Parent.IsKind(SyntaxKind.FieldDeclaration) AndAlso node.Names.Count = 1
        End Function

        Friend Shared Function GetArrayBoundsCapturedVariables(model As SemanticModel, arrayBounds As ArgumentListSyntax) As ImmutableArray(Of ISymbol)
            ' Edge case, no need to be efficient, currently there can either be no captured variables or just "Me".
            ' Dim a((Function(n) n + 1).Invoke(1), (Function(n) n + 2).Invoke(2)) As Integer
            Return ImmutableArray.CreateRange(
                    arrayBounds.Arguments.
                        SelectMany(AddressOf GetArgumentExpressions).
                        SelectMany(Function(expr) model.AnalyzeDataFlow(expr).Captured).
                        Distinct())
        End Function

        Private Shared Iterator Function GetArgumentExpressions(argument As ArgumentSyntax) As IEnumerable(Of ExpressionSyntax)
            Select Case argument.Kind
                Case SyntaxKind.SimpleArgument
                    Yield DirectCast(argument, SimpleArgumentSyntax).Expression

                Case SyntaxKind.RangeArgument
                    Dim range = DirectCast(argument, RangeArgumentSyntax)
                    Yield range.LowerBound
                    Yield range.UpperBound

                Case SyntaxKind.OmittedArgument

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(argument.Kind)
            End Select
        End Function

        <Conditional("DEBUG")>
        Public Shared Sub AssertIsBody(syntax As SyntaxNode, allowLambda As Boolean)
            ' lambda/query
            If LambdaUtilities.IsLambdaBody(syntax) Then
                Debug.Assert(allowLambda)
                Debug.Assert(TypeOf syntax Is ExpressionSyntax OrElse TypeOf syntax Is LambdaHeaderSyntax)
                Return
            End If

            ' sub/function/ctor/operator/accessor
            If TypeOf syntax Is MethodBlockBaseSyntax Then
                Return
            End If

            ' field/property initializer
            If TypeOf syntax Is ExpressionSyntax Then
                If syntax.Parent.Kind = SyntaxKind.EqualsValue Then
                    If syntax.Parent.Parent.IsKind(SyntaxKind.PropertyStatement) Then
                        Return
                    End If

                    Debug.Assert(syntax.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
                    Debug.Assert(syntax.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
                    Return
                ElseIf syntax.Parent.Kind = SyntaxKind.AsNewClause Then
                    If syntax.Parent.Parent.IsKind(SyntaxKind.PropertyStatement) Then
                        Return
                    End If

                    Debug.Assert(syntax.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
                    Debug.Assert(syntax.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
                    Return
                End If
            End If

            ' field array initializer
            If TypeOf syntax Is ArgumentListSyntax Then
                Debug.Assert(syntax.Parent.IsKind(SyntaxKind.ModifiedIdentifier))
                Debug.Assert(syntax.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
                Debug.Assert(syntax.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
                Return
            End If

            Throw ExceptionUtilities.Unreachable
        End Sub

        Public Shared Function GetBody(node As LambdaExpressionSyntax) As SyntaxList(Of SyntaxNode)
            Select Case node.Kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression, SyntaxKind.MultiLineSubLambdaExpression
                    Return DirectCast(node, MultiLineLambdaExpressionSyntax).Statements
                Case SyntaxKind.SingleLineFunctionLambdaExpression, SyntaxKind.SingleLineSubLambdaExpression
                    Return SyntaxFactory.SingletonList(DirectCast(node, SingleLineLambdaExpressionSyntax).Body)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

        Public Shared Function IsParameterlessConstructor(declaration As SyntaxNode) As Boolean
            If Not declaration.IsKind(SyntaxKind.ConstructorBlock) Then
                Return False
            End If

            Dim ctor = DirectCast(declaration, ConstructorBlockSyntax)
            Return ctor.BlockStatement.ParameterList.Parameters.Count = 0
        End Function

        Public Shared Function HasBackingField(propertyDeclaration As SyntaxNode) As Boolean
            Return propertyDeclaration.IsKind(SyntaxKind.PropertyStatement) AndAlso
                   Not propertyDeclaration.Parent.IsKind(SyntaxKind.PropertyBlock) AndAlso
                   Not DirectCast(propertyDeclaration, PropertyStatementSyntax).Modifiers.Any(SyntaxKind.MustOverrideKeyword)
        End Function

        Public Shared Function IsAsyncMethodOrLambda(declarationOrBody As SyntaxNode) As Boolean
            Return GetModifiers(declarationOrBody).Any(SyntaxKind.AsyncKeyword)
        End Function

        Public Shared Function IsIteratorMethodOrLambda(declaration As SyntaxNode) As Boolean
            Return GetModifiers(declaration).Any(SyntaxKind.IteratorKeyword)
        End Function

        Public Shared Function GetAwaitExpressions(body As SyntaxNode) As ImmutableArray(Of SyntaxNode)
            ' skip lambda bodies
            Return ImmutableArray.CreateRange(body.DescendantNodes(AddressOf LambdaUtilities.IsNotLambda).
                Where(Function(n) n.IsKind(SyntaxKind.AwaitExpression)))
        End Function

        Public Shared Function GetYieldStatements(body As SyntaxNode) As ImmutableArray(Of SyntaxNode)
            ' enumerate statements:
            Return ImmutableArray.CreateRange(body.DescendantNodes(Function(n) TypeOf n IsNot ExpressionSyntax).
                Where(Function(n) n.IsKind(SyntaxKind.YieldStatement)))

        End Function

        Public Shared Function GetModifiers(declarationOrBody As SyntaxNode) As SyntaxTokenList
            Select Case declarationOrBody.Kind
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock
                    Return DirectCast(declarationOrBody, MethodBlockBaseSyntax).BlockStatement.Modifiers

                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return DirectCast(declarationOrBody, LambdaExpressionSyntax).SubOrFunctionHeader.Modifiers

                Case SyntaxKind.FunctionLambdaHeader,
                     SyntaxKind.SubLambdaHeader
                    Return DirectCast(declarationOrBody, LambdaHeaderSyntax).Modifiers
            End Select

            Return Nothing
        End Function
    End Class
End Namespace
