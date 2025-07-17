' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module ParenthesizedExpressionSyntaxExtensions

        Private Function EndsQuery(token As SyntaxToken, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Dim query = token.Parent.FirstAncestorOrSelf(Of QueryExpressionSyntax)()
            If query IsNot Nothing Then
                Return query.GetLastToken() = token
            Else
                Dim invocationAtLast = token.Parent.FirstAncestorOrSelf(Of InvocationExpressionSyntax)()
                Return invocationAtLast IsNot Nothing AndAlso
                   invocationAtLast.GetLastToken() = token AndAlso
                   invocationAtLast.CanRemoveEmptyArgumentList(semanticModel) AndAlso
                   EndsQuery(invocationAtLast.Expression.GetLastToken(), semanticModel, cancellationToken)
            End If

            Return False
        End Function

        Private Function EndsVariableDeclarator(token As SyntaxToken) As Boolean
            Dim variableDeclarator = token.Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            Return variableDeclarator IsNot Nothing AndAlso
                   variableDeclarator.GetLastToken() = token
        End Function

        Private Function EndsLambda(token As SyntaxToken) As Boolean
            Dim lambda = token.Parent.FirstAncestorOrSelf(Of SingleLineLambdaExpressionSyntax)()
            Return lambda IsNot Nothing AndAlso
                   lambda.GetLastToken() = token
        End Function

        <Extension>
        Public Function CanRemoveParentheses(
            node As ParenthesizedExpressionSyntax,
            semanticModel As SemanticModel,
            Optional cancellationToken As CancellationToken = Nothing
        ) As Boolean

            If node.OpenParenToken.IsMissing OrElse node.CloseParenToken.IsMissing Then
                ' Cases:
                '   (3
                Return False
            End If

            Dim expression = node.Expression

            ' Cases:
            '   ((Goo))
            If expression.IsKind(SyntaxKind.ParenthesizedExpression) Then
                Return True
            End If

            '   ((Goo, Bar))
            If expression.IsKind(SyntaxKind.TupleExpression) Then
                Return True
            End If

            ' Cases:
            '   ("x"c)
            '   (#1/1/2001#)
            '   (False)
            '   (Nothing)
            '   (1)
            '   ("")
            '   (True)
            If expression.IsKind(SyntaxKind.CharacterLiteralExpression) OrElse
               expression.IsKind(SyntaxKind.DateLiteralExpression) OrElse
               expression.IsKind(SyntaxKind.FalseLiteralExpression) OrElse
               expression.IsKind(SyntaxKind.NothingLiteralExpression) OrElse
               expression.IsKind(SyntaxKind.NumericLiteralExpression) OrElse
               expression.IsKind(SyntaxKind.StringLiteralExpression) OrElse
               expression.IsKind(SyntaxKind.TrueLiteralExpression) Then

                Return True
            End If

            ' Case:
            '   ($"")
            If expression.IsKind(SyntaxKind.InterpolatedStringExpression) Then
                Return True
            End If

            ' Cases:
            '   (Me)
            '   (MyBase)
            '   (MyClass)
            If expression.IsKind(SyntaxKind.MeExpression) OrElse
               expression.IsKind(SyntaxKind.MyBaseExpression) OrElse
               expression.IsKind(SyntaxKind.MyClassExpression) Then

                Return True
            End If

            ' Cases:
            '   (DirectCast(Goo))
            '   (TryCast(Goo))
            '   (CType(Goo, Bar))
            '   (CInt(Goo))
            If expression.IsKind(SyntaxKind.DirectCastExpression) OrElse
               expression.IsKind(SyntaxKind.TryCastExpression) OrElse
               expression.IsKind(SyntaxKind.CTypeExpression) OrElse
               TypeOf expression Is PredefinedCastExpressionSyntax Then

                Return True
            End If

            ' Cases:
            '   (AddressOf Goo)
            '   (New With {.Goo = ""})
            '   (If(True, 1, 2))
            '   (If(Nothing, 1))
            '   (NameOf(Goo))
            If expression.IsKind(SyntaxKind.AddressOfExpression) OrElse
               expression.IsKind(SyntaxKind.AnonymousObjectCreationExpression) OrElse
               expression.IsKind(SyntaxKind.TernaryConditionalExpression) OrElse
               expression.IsKind(SyntaxKind.BinaryConditionalExpression) OrElse
               expression.IsKind(SyntaxKind.NameOfExpression) Then

                Return True
            End If

            ' Cases:
            '   List(Of Integer()) From {({1})} -to- {({1})}
            '   List(Of Integer()) From {{({1})}} -to- {{{1}}}
            '   $"{ ({1}) } - to- $"{ {1} }"
            '   {({1})} -to- {({1})}
            '   ({1}) -to- {1}
            If expression.IsKind(SyntaxKind.CollectionInitializer) Then

                If node.IsParentKind(SyntaxKind.Interpolation) Then
                    Dim interpolation = DirectCast(node.Parent, InterpolationSyntax)

                    If interpolation.OpenBraceToken.Span.End = node.OpenParenToken.Span.Start AndAlso
                       node.OpenParenToken.Span.End = expression.Span.Start Then

                        ' In an interpolation, we need to be careful not to remove a parenthesis if it touches a curly brace
                        ' on the left and the right. Otherwise, code will parse differently.

                        Return False
                    End If
                End If

                If Not node.IsParentKind(SyntaxKind.CollectionInitializer) Then
                    ' Standalone parenthesized array literal.
                    ' Parentheses are insignificant.
                    ' Ex. x = ({1})
                    Return True
                End If

                If node.Parent.IsParentKind(SyntaxKind.ObjectCollectionInitializer) AndAlso
                   DirectCast(node.Parent.Parent, ObjectCollectionInitializerSyntax).Initializer Is node.Parent Then
                    ' This is a parenthesized array literal as a collection item within ObjectCollectionInitializer.
                    ' Parentheses are significant in this case and should not be removed.
                    ' Ex. List(Of Integer()) From {({1})}
                    Return False
                End If

                If node.Parent.IsParentKind(SyntaxKind.CollectionInitializer) AndAlso
                   node.Parent.Parent.IsParentKind(SyntaxKind.ObjectCollectionInitializer) AndAlso
                   DirectCast(node.Parent.Parent.Parent, ObjectCollectionInitializerSyntax).Initializer Is node.Parent.Parent Then
                    ' This is a parenthesized array literal within first level sub-collection initializer within ObjectCollectionInitializer.
                    ' Parentheses are insignificant in this case.
                    ' Ex. List(Of Integer()) From {{({1})}}
                    Return True
                End If

                ' This is a parenthesized array literal as an item expression within another array literal.
                ' Parentheses are significant in this case and should not be removed.
                Return False
            End If

            Dim firstToken = expression.GetFirstToken()
            Dim previousToken = node.OpenParenToken.GetPreviousToken()

            ' Case:
            '   0 > <x/>.Value
            '   0 < <x/>.Value
            If firstToken.IsKind(SyntaxKind.LessThanToken) AndAlso
               previousToken.IsKind(SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken) Then

                Return False
            End If

            ' Cases:
            '   (<xml/>)
            '   (<xml></xml>)
            '   (<x/>.@a)
            '   (<x/>...<b>)
            '   (<x/>.<a>)
            If expression.IsKind(SyntaxKind.XmlEmptyElement) OrElse
               expression.IsKind(SyntaxKind.XmlElement) OrElse
               expression.IsKind(SyntaxKind.XmlAttributeAccessExpression) OrElse
               expression.IsKind(SyntaxKind.XmlDescendantAccessExpression) OrElse
               expression.IsKind(SyntaxKind.XmlElementAccessExpression) Then
                Return True
            End If

            Dim lastToken = expression.GetLastToken()
            Dim nextToken = node.CloseParenToken.GetNextToken()

            ' Cases:
            '   Dim x = (Goo)
            If node.IsParentKind(SyntaxKind.EqualsValue) AndAlso
               Not EndsQuery(lastToken, semanticModel, cancellationToken) AndAlso
               Not EndsLambda(lastToken) AndAlso
               Not nextToken.IsKindOrHasMatchingText(SyntaxKind.CommaToken) Then
                Return True
            End If

            ' Cases:
            '   (New Goo)
            '   (New Goo())
            If expression.IsKind(SyntaxKind.ObjectCreationExpression) Then
                Dim objectCreation = DirectCast(expression, ObjectCreationExpressionSyntax)

                If nextToken.IsKindOrHasMatchingText(SyntaxKind.DotToken) Then
                    If objectCreation.ArgumentList Is Nothing Then
                        ' Note we can remove the parentheses when the next token is dot only
                        ' if the type of the ObjectCreationExpression is a predefined type.
                        ' So, we can remove parentheses in this case...
                        '
                        '     Call (New Integer).ToString
                        '
                        ' But not this one...
                        '
                        '     Call (New Int32).ToString

                        Return TypeOf objectCreation.Type Is PredefinedTypeSyntax
                    End If
                End If

                If nextToken.IsKindOrHasMatchingText(SyntaxKind.OpenParenToken) Then
                    Return False
                End If

                Return True
            End If

            ' Cases:
            ' 1.   (Goo)
            ' 2.   (Goo())
            ' 3.   <x/>.GetHashCode()
            ' 4.   1 < (<x/>.GetHashCode()) Or 1 > (<x/>.GetHashCode())
            If expression.IsKind(SyntaxKind.InvocationExpression) Then
                Dim invocationExpression = DirectCast(expression, InvocationExpressionSyntax)

                If invocationExpression.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                    Dim memberAccess = DirectCast(invocationExpression.Expression, MemberAccessExpressionSyntax)
                    If (TypeOf memberAccess.Expression Is XmlNodeSyntax AndAlso
                        (previousToken.IsKindOrHasMatchingText(SyntaxKind.LessThanToken) OrElse
                        previousToken.IsKindOrHasMatchingText(SyntaxKind.GreaterThanToken))) Then

                        Return False
                    End If
                End If

                If invocationExpression.ArgumentList Is Nothing Then
                    Return Not nextToken.IsKindOrHasMatchingText(SyntaxKind.OpenParenToken)
                End If

                Return True
            End If

            ' Cases:
            '   (Goo.Bar)
            '   (Goo)
            If expression.IsKind(SyntaxKind.IdentifierName) OrElse
               expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then

                ' If this is a local, field or property is passed to a ByRef parameter, we should
                ' keep the parentheses to ensure that we don't change copy-back semantics.
                If TypeOf node.Parent Is ArgumentSyntax Then
                    Dim symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol
                    If symbol IsNot Nothing Then
                        If symbol.MatchesKind(SymbolKind.Local, SymbolKind.Field, SymbolKind.Property) Then
                            Dim argument = DirectCast(node.Parent, ArgumentSyntax)
                            Dim parameter = argument.DetermineParameter(semanticModel, cancellationToken:=cancellationToken)

                            If parameter IsNot Nothing AndAlso
                               parameter.RefKind <> RefKind.None Then

                                Return False
                            End If
                        End If
                    End If
                End If

                ' If the next token is an open paren, we need to be careful to ensure
                ' that it is the opening of the argument list of a parenting invocation
                ' for which this is the expression.
                If nextToken.IsKindOrHasMatchingText(SyntaxKind.OpenParenToken) Then
                    If node.IsParentKind(SyntaxKind.InvocationExpression) Then
                        Dim parentInvocation = DirectCast(node.Parent, InvocationExpressionSyntax)
                        If parentInvocation.Expression Is node AndAlso
                           parentInvocation.ArgumentList IsNot Nothing AndAlso
                           parentInvocation.ArgumentList.OpenParenToken = nextToken Then

                            Return True
                        End If
                    End If

                    Return False
                End If

                Return True
            End If

            ' Case:
            '   (Goo(Of Bar))
            If expression.IsKind(SyntaxKind.GenericName) Then
                If Not nextToken.IsKindOrHasMatchingText(SyntaxKind.OpenParenToken) Then
                    Return True
                End If
            End If

            Dim isNodeCloseParenLastTokenOfStatement = node.CloseParenToken.IsLastTokenOfStatement(checkColonTrivia:=True)
            Dim nextNextToken = nextToken.GetNextToken()

            ' Dim z = Function() (From x In "") ' OK
            ' Select 1
            ' End Select
            ' Select is the only keyword in LINQ which has dual usage 1. Case selection 2. Query Select Clause
            If isNodeCloseParenLastTokenOfStatement AndAlso
                EndsQuery(lastToken, semanticModel, cancellationToken) AndAlso
                nextToken.Kind = SyntaxKind.SelectKeyword AndAlso
                nextNextToken.Kind <> SyntaxKind.CaseKeyword Then
                Return False
            End If

            ' (Await Task.Run(Function() i)) <EOL>
            ' (Await Task.Run(Function() i)),
            If node.Expression.IsKind(SyntaxKind.AwaitExpression) AndAlso
                (isNodeCloseParenLastTokenOfStatement OrElse
                nextToken.Kind = SyntaxKind.CommaToken) Then
                Return True
            End If

            ' Cases:
            '   (1 + 1) * 8
            '   (1 + 1).ToString
            '   (1 + 1)?.ToString
            '   (1 + 1)()
            If TypeOf expression Is BinaryExpressionSyntax OrElse
               TypeOf expression Is UnaryExpressionSyntax Then

                Dim parentExpression = TryCast(node.Parent, ExpressionSyntax)
                If parentExpression IsNot Nothing Then
                    If parentExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression) OrElse
                       parentExpression.IsKind(SyntaxKind.ConditionalAccessExpression) OrElse
                       parentExpression.IsKind(SyntaxKind.InvocationExpression) Then
                        Return False
                    End If

                    Dim precedence = expression.GetOperatorPrecedence()
                    Dim parentPrecedence = parentExpression.GetOperatorPrecedence()

                    ' Only remove if the expression's precedence is higher than its parent.
                    If parentPrecedence <> OperatorPrecedence.PrecedenceNone AndAlso
                       precedence < parentPrecedence Then

                        Return False
                    End If

                    ' If the expression's precedence is the same as its parent, and both are binary expressions,
                    ' check for associativity and commutability.
                    If precedence <> OperatorPrecedence.PrecedenceNone AndAlso precedence = parentPrecedence Then
                        Dim binaryExpression = TryCast(expression, BinaryExpressionSyntax)
                        Dim parentBinaryExpression = TryCast(parentExpression, BinaryExpressionSyntax)

                        If binaryExpression IsNot Nothing AndAlso parentBinaryExpression IsNot Nothing Then
                            ' All binary expressions are left associative, so if the expression
                            ' is on the left side of a binary expression the parentheses can be removed.
                            If parentBinaryExpression.Left Is node Then
                                Return True
                            End If

                            ' If both the expression and its parent are binary expressions and their kinds
                            ' are the same, and the parenthesized expression is on the right and the 
                            ' operation is associative, it can sometimes be safe to remove these parens.
                            '
                            ' i.e. if you have "a AndAlso (b AndAlso c)" it can be converted to "a AndAlso b AndAlso c" 
                            ' as that New interpretation "(a AndAlso b) AndAlso c" operates the exact same way at 
                            ' runtime.
                            '
                            ' Specifically: 
                            '  1) the operands are still executed in the same order a, b, then c.
                            '     So even if they have side effects, it will Not matter.
                            '  2) the same shortcircuiting happens.
                            '  3) the result will always be the same (for logical operators, there are 
                            '     additional conditions that are checked for non-logical operators).
                            If IsAssociative(parentBinaryExpression.Kind) AndAlso
                               expression.Kind = parentExpression.Kind Then

                                Return VisualBasicSemanticFacts.Instance.IsSafeToChangeAssociativity(
                                    binaryExpression, parentBinaryExpression, semanticModel)
                            End If
                        End If

                        Return False
                    End If
                End If

                Return True
            End If

            ' Cases:
            '   (Sub() From x in y), Goo
            '   Dim a = (Sub() If True Then Dim x), b = a
            '   Dim y = (Function() Console.ReadLine)()
            '   Call (Sub() Exit Sub)
            '   Dim x = <x <%= (Sub() If True Then Else) %>/>
            If TypeOf expression Is SingleLineLambdaExpressionSyntax Then
                If node.CloseParenToken.IsLastTokenOfStatementWithEndOfLine() AndAlso
                    lastToken.Kind = SyntaxKind.ThenKeyword Then
                    Return False
                End If

                If nextToken.IsKindOrHasMatchingText(SyntaxKind.CommaToken) Then
                    Dim lastStatement = lastToken.Parent.GetFirstEnclosingStatement()
                    If EndsQuery(lastToken, semanticModel, cancellationToken) OrElse EndsVariableDeclarator(lastToken) OrElse
                            (EndsLambda(lastToken) AndAlso
                            Not previousToken.IsKindOrHasMatchingText(SyntaxKind.OpenParenToken) AndAlso
                            lastStatement IsNot Nothing AndAlso lastStatement.Kind = SyntaxKind.ReDimStatement) Then
                        Return False
                    End If

                    Return True
                End If

                ' case:
                ' (Sub() If True Then Dim y = Sub(z As Integer)
                '                             End Sub).Invoke()
                If nextToken.IsKindOrHasMatchingText(SyntaxKind.DotToken) AndAlso
                       nextToken.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                    Return False
                End If

                ' case:
                ' 1. Call (Sub() If True Then Dim y = Sub(z As Integer)
                '                             End Sub)
                ' 2. (Sub() If True Then Dim y = Sub()
                '                                End Sub) Is Nothing
                ' 3. TypeOf (Sub() If True Then Dim y = Sub()
                '                                End Sub) Is Object
                ' 4. TypeOf (Sub() If True Then Dim y = Sub()
                '                                End Sub) IsNot Object
                If (node.Parent.Kind = SyntaxKind.InvocationExpression OrElse
                        node.Parent.Kind = SyntaxKind.IsExpression OrElse
                        node.Parent.Kind = SyntaxKind.TypeOfIsExpression OrElse
                        node.Parent.Kind = SyntaxKind.TypeOfIsNotExpression) Then
                    Return False
                End If

                ' case:
                ' 1. (Sub() If True Then) Implements I.A
                ' 2. If True Then : Dim x As Action = (Sub() If True Then) : Else : Return : End If
                If nextToken.IsKindOrHasMatchingText(SyntaxKind.CloseParenToken) OrElse
                       nextToken.IsKindOrHasMatchingText(SyntaxKind.CloseBraceToken) OrElse
                       lastToken.IsLastTokenOfStatement(checkColonTrivia:=True) OrElse
                       node.Parent.Kind = SyntaxKind.XmlEmbeddedExpression Then
                    Return True
                End If

                ' case:
                ' 1 .
                ' Dim a = Sub() If False Then Console.WriteLine() Else Dim q = From x In ""
                ' [Take]()
                If isNodeCloseParenLastTokenOfStatement AndAlso
                    EndsQuery(lastToken, semanticModel, cancellationToken) AndAlso
                  nextToken.IsKeyword Then
                    Return True
                End If

                If isNodeCloseParenLastTokenOfStatement AndAlso
                    Not EndsQuery(lastToken, semanticModel, cancellationToken) Then
                    Return True
                End If

                Return False
            End If

            If TypeOf expression Is MultiLineLambdaExpressionSyntax Then
                Return True
            End If

            ' Cases:
            '   {(From x in y), From x in y}
            '
            '   Dim q = (From x in "")
            '   Select 1
            '   End Select
            '
            '   With New StringBuilder
            '       Dim q = (From x in "")
            '       .Length = 0
            '   End With
            '
            ' Dim y = (From c In "" Distinct)
            '    !A = !B
            If EndsQuery(lastToken, semanticModel, cancellationToken) Then
                If nextToken.IsKindOrHasMatchingText(SyntaxKind.CloseParenToken) OrElse
                   nextToken.IsKindOrHasMatchingText(SyntaxKind.CloseBraceToken) OrElse
                   node.CloseParenToken.IsLastTokenOfStatement() Then

                    Dim nextTokenTextKind = SyntaxFacts.GetContextualKeywordKind(nextToken.Text)
                    Select Case nextTokenTextKind
                        Case SyntaxKind.AscendingKeyword,
                             SyntaxKind.DescendingKeyword,
                             SyntaxKind.DistinctKeyword,
                             SyntaxKind.GroupKeyword,
                             SyntaxKind.IntoKeyword,
                             SyntaxKind.OrderKeyword,
                             SyntaxKind.SkipKeyword,
                             SyntaxKind.TakeKeyword,
                             SyntaxKind.WhereKeyword,
                             SyntaxKind.JoinKeyword,
                             SyntaxKind.InKeyword,
                             SyntaxKind.LetKeyword,
                             SyntaxKind.OnKeyword,
                             SyntaxKind.SelectKeyword,
                             SyntaxKind.AggregateKeyword,
                             SyntaxKind.FromKeyword
                            Return False
                    End Select

                    If Not (nextToken.IsKindOrHasMatchingText(SyntaxKind.DotToken) AndAlso
                            nextToken.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression)) AndAlso
                       Not (nextToken.IsKindOrHasMatchingText(SyntaxKind.SelectKeyword) AndAlso
                            nextToken.Parent.IsKind(SyntaxKind.SelectStatement)) AndAlso
                       Not (nextToken.IsKindOrHasMatchingText(SyntaxKind.ExclamationToken) AndAlso
                            lastToken.IsKeyword AndAlso
                            nextToken.Parent.IsKind(SyntaxKind.DictionaryAccessExpression)) Then

                        Return True
                    End If
                End If

                Return False
            End If

            ' case:
            ' (GetType(String)) => GetType(String)
            If expression.IsKind(SyntaxKind.GetTypeExpression) Then
                Return True
            End If

            ' case:
            ' 1. (!b) => !b
            If expression.Kind = SyntaxKind.DictionaryAccessExpression AndAlso
                node.CloseParenToken.IsLastTokenOfStatement() Then
                Return True
            End If

            Return False
        End Function

        Private Function IsAssociative(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.AddExpression,
                     SyntaxKind.MultiplyExpression,
                     SyntaxKind.AndExpression,
                     SyntaxKind.AndAlsoExpression,
                     SyntaxKind.OrExpression,
                     SyntaxKind.ExclusiveOrExpression,
                     SyntaxKind.OrElseExpression
                    Return True
            End Select

            Return False
        End Function
    End Module
End Namespace
