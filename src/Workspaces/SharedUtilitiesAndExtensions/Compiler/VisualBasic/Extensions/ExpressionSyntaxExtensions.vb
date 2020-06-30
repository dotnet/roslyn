' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module ExpressionSyntaxExtensions
        <Extension()>
        Public Function WalkUpParentheses(expression As ExpressionSyntax) As ExpressionSyntax
            While expression.IsParentKind(SyntaxKind.ParenthesizedExpression)
                expression = DirectCast(expression.Parent, ExpressionSyntax)
            End While

            Return expression
        End Function

        <Extension()>
        Public Function WalkDownParentheses(expression As ExpressionSyntax) As ExpressionSyntax
            While expression.IsKind(SyntaxKind.ParenthesizedExpression)
                expression = DirectCast(expression, ParenthesizedExpressionSyntax).Expression
            End While

            Return expression
        End Function

        <Extension()>
        Public Function IsLeftSideOfDot(expression As ExpressionSyntax) As Boolean
            If expression Is Nothing Then
                Return False
            End If

            Return _
                (expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso DirectCast(expression.Parent, QualifiedNameSyntax).Left Is expression) OrElse
                (expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso DirectCast(expression.Parent, MemberAccessExpressionSyntax).Expression Is expression)
        End Function

        <Extension()>
        Public Function IsNewOnRightSideOfDotOrBang(expression As ExpressionSyntax) As Boolean
            Dim identifierName = TryCast(expression, IdentifierNameSyntax)
            If identifierName Is Nothing Then
                Return False
            End If

            If String.Compare(identifierName.Identifier.ToString(), "New", StringComparison.OrdinalIgnoreCase) <> 0 Then
                Return False
            End If

            Return identifierName.IsRightSideOfDotOrBang()
        End Function

        <Extension()>
        Public Function IsMemberAccessExpressionName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso
                   DirectCast(expression.Parent, MemberAccessExpressionSyntax).Name Is expression
        End Function

        <Extension()>
        Public Function IsAnyMemberAccessExpressionName(expression As ExpressionSyntax) As Boolean
            Return expression IsNot Nothing AndAlso
                   TypeOf expression.Parent Is MemberAccessExpressionSyntax AndAlso
                   DirectCast(expression.Parent, MemberAccessExpressionSyntax).Name Is expression
        End Function

        <Extension()>
        Public Function IsRightSideOfDotOrBang(expression As ExpressionSyntax) As Boolean
            Return expression.IsAnyMemberAccessExpressionName() OrElse expression.IsRightSideOfQualifiedName()
        End Function

        <Extension()>
        Public Function IsRightSideOfDot(expression As ExpressionSyntax) As Boolean
            Return expression.IsMemberAccessExpressionName() OrElse expression.IsRightSideOfQualifiedName()
        End Function

        <Extension()>
        Public Function IsRightSideOfQualifiedName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso
                   DirectCast(expression.Parent, QualifiedNameSyntax).Right Is expression
        End Function

        <Extension()>
        Public Function IsLeftSideOfQualifiedName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso
                   DirectCast(expression.Parent, QualifiedNameSyntax).Left Is expression
        End Function

        <Extension()>
        Public Function IsAnyLiteralExpression(expression As ExpressionSyntax) As Boolean
            Return expression.IsKind(SyntaxKind.CharacterLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.DateLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.FalseLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.NothingLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.NumericLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.StringLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.TrueLiteralExpression)
        End Function

        <Extension()>
        Public Function DetermineType(expression As ExpressionSyntax,
                                      semanticModel As SemanticModel,
                                      cancellationToken As CancellationToken) As ITypeSymbol
            ' If a parameter appears to have a void return type, then just use 'object' instead.
            If expression IsNot Nothing Then
                Dim typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken)
                Dim symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken)
                If typeInfo.Type IsNot Nothing AndAlso typeInfo.Type.SpecialType = SpecialType.System_Void Then
                    Return semanticModel.Compilation.ObjectType
                End If

                Dim symbol = If(typeInfo.Type, symbolInfo.GetAnySymbol())
                If symbol IsNot Nothing Then
                    Return symbol.ConvertToType(semanticModel.Compilation)
                End If

                If TypeOf expression Is CollectionInitializerSyntax Then
                    Dim collectionInitializer = DirectCast(expression, CollectionInitializerSyntax)
                    Return DetermineType(collectionInitializer, semanticModel, cancellationToken)
                End If
            End If

            Return semanticModel.Compilation.ObjectType
        End Function

        <Extension()>
        Private Function DetermineType(collectionInitializer As CollectionInitializerSyntax,
                                      semanticModel As SemanticModel,
                                      cancellationToken As CancellationToken) As ITypeSymbol
            Dim rank = 1
            While collectionInitializer.Initializers.Count > 0 AndAlso
                  collectionInitializer.Initializers(0).Kind = SyntaxKind.CollectionInitializer
                rank += 1
                collectionInitializer = DirectCast(collectionInitializer.Initializers(0), CollectionInitializerSyntax)
            End While

            Dim type = collectionInitializer.Initializers.FirstOrDefault().DetermineType(semanticModel, cancellationToken)
            Return semanticModel.Compilation.CreateArrayTypeSymbol(type, rank)
        End Function

        Private Function IsUnnecessaryCast(
            castNode As ExpressionSyntax,
            castExpressionNode As ExpressionSyntax,
            semanticModel As SemanticModel,
            assumeCallKeyword As Boolean,
            cancellationToken As CancellationToken
        ) As Boolean

            Return CastAnalyzer.IsUnnecessary(castNode, castExpressionNode, semanticModel, assumeCallKeyword, cancellationToken)
        End Function

        <Extension>
        Public Function IsUnnecessaryCast(
            node As CastExpressionSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken,
            Optional assumeCallKeyword As Boolean = False
        ) As Boolean

            Return IsUnnecessaryCast(node, node.Expression, semanticModel, assumeCallKeyword, cancellationToken)
        End Function

        <Extension>
        Public Function IsUnnecessaryCast(
            node As PredefinedCastExpressionSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken,
            Optional assumeCallKeyword As Boolean = False
        ) As Boolean

            Return IsUnnecessaryCast(node, node.Expression, semanticModel, assumeCallKeyword, cancellationToken)
        End Function

        <Extension>
        Public Function GetOperatorPrecedence(expression As ExpressionSyntax) As OperatorPrecedence
            Select Case expression.Kind
                Case SyntaxKind.ExponentiateExpression
                    Return OperatorPrecedence.PrecedenceExponentiate
                Case SyntaxKind.UnaryMinusExpression,
                     SyntaxKind.UnaryPlusExpression
                    Return OperatorPrecedence.PrecedenceNegate
                Case SyntaxKind.MultiplyExpression,
                     SyntaxKind.DivideExpression
                    Return OperatorPrecedence.PrecedenceMultiply
                Case SyntaxKind.IntegerDivideExpression
                    Return OperatorPrecedence.PrecedenceIntegerDivide
                Case SyntaxKind.ModuloExpression
                    Return OperatorPrecedence.PrecedenceModulus
                Case SyntaxKind.AddExpression,
                     SyntaxKind.SubtractExpression
                    Return OperatorPrecedence.PrecedenceAdd
                Case SyntaxKind.ConcatenateExpression
                    Return OperatorPrecedence.PrecedenceConcatenate
                Case SyntaxKind.LeftShiftExpression,
                     SyntaxKind.RightShiftExpression
                    Return OperatorPrecedence.PrecedenceShift
                Case SyntaxKind.EqualsExpression,
                     SyntaxKind.NotEqualsExpression,
                     SyntaxKind.LessThanExpression,
                     SyntaxKind.GreaterThanExpression,
                     SyntaxKind.LessThanOrEqualExpression,
                     SyntaxKind.GreaterThanOrEqualExpression,
                     SyntaxKind.LikeExpression,
                     SyntaxKind.IsExpression,
                     SyntaxKind.IsNotExpression
                    Return OperatorPrecedence.PrecedenceRelational
                Case SyntaxKind.NotExpression
                    Return OperatorPrecedence.PrecedenceNot
                Case SyntaxKind.AndExpression,
                     SyntaxKind.AndAlsoExpression
                    Return OperatorPrecedence.PrecedenceAnd
                Case SyntaxKind.OrExpression,
                     SyntaxKind.OrElseExpression
                    Return OperatorPrecedence.PrecedenceOr
                Case SyntaxKind.ExclusiveOrExpression
                    Return OperatorPrecedence.PrecedenceXor
                Case Else
                    Return OperatorPrecedence.PrecedenceNone
            End Select
        End Function

#Disable Warning IDE0060 ' Remove unused parameter
        <Extension()>
        Public Function IsInOutContext(expression As ExpressionSyntax) As Boolean
#Enable Warning IDE0060 ' Remove unused parameter
            ' NOTE(cyrusn): VB has no concept of an out context.  Even when a parameter has an
            ' '<Out>' attribute on it, it's still treated as ref by VB.  So we always return false
            ' here.
            Return False
        End Function

        <Extension()>
        Public Function IsInRefContext(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Dim simpleArgument = TryCast(expression?.Parent, SimpleArgumentSyntax)

            If simpleArgument Is Nothing Then
                Return False
            ElseIf simpleArgument.IsNamed Then
                Dim info = semanticModel.GetSymbolInfo(simpleArgument.NameColonEquals.Name, cancellationToken)

                Dim parameter = TryCast(info.GetAnySymbol(), IParameterSymbol)
                Return parameter IsNot Nothing AndAlso parameter.RefKind <> RefKind.None

            Else
                Dim argumentList = TryCast(simpleArgument.Parent, ArgumentListSyntax)

                If argumentList IsNot Nothing Then
                    Dim parent = argumentList.Parent
                    Dim index = argumentList.Arguments.IndexOf(simpleArgument)

                    Dim info = semanticModel.GetSymbolInfo(parent, cancellationToken)
                    Dim symbol = info.GetAnySymbol()

                    If TypeOf symbol Is IMethodSymbol Then
                        Dim method = DirectCast(symbol, IMethodSymbol)
                        If index < method.Parameters.Length Then
                            Return method.Parameters(index).RefKind <> RefKind.None
                        End If
                    ElseIf TypeOf symbol Is IPropertySymbol Then
                        Dim prop = DirectCast(symbol, IPropertySymbol)
                        If index < prop.Parameters.Length Then
                            Return prop.Parameters(index).RefKind <> RefKind.None
                        End If
                    End If
                End If

            End If

            Return False
        End Function

#Disable Warning IDE0060 ' Remove unused parameter
        <Extension()>
        Public Function IsInInContext(expression As ExpressionSyntax) As Boolean
#Enable Warning IDE0060 ' Remove unused parameter
            ' NOTE: VB does not support in parameters. Always return False here.
            Return False
        End Function

        <Extension()>
        Public Function IsOnlyWrittenTo(expression As ExpressionSyntax) As Boolean
            If expression.IsRightSideOfDot() Then
                expression = TryCast(expression.Parent, ExpressionSyntax)
            End If

            If expression IsNot Nothing Then
                If expression.IsInOutContext() Then
                    Return True
                End If

                If expression.IsParentKind(SyntaxKind.SimpleAssignmentStatement) Then
                    Dim assignmentStatement = DirectCast(expression.Parent, AssignmentStatementSyntax)
                    If expression Is assignmentStatement.Left Then
                        Return True
                    End If
                End If

                If expression.IsParentKind(SyntaxKind.NameColonEquals) AndAlso
                   expression.Parent.IsParentKind(SyntaxKind.SimpleArgument) Then

                    ' <C(Prop:=1)>
                    ' this is only a write to Prop
                    Return True
                End If

                If expression.IsChildNode(Of NamedFieldInitializerSyntax)(Function(n) n.Name) Then
                    Return True
                End If

                Return False
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsWrittenTo(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            If IsOnlyWrittenTo(expression) Then
                Return True
            End If

            If expression.IsRightSideOfDot() Then
                expression = TryCast(expression.Parent, ExpressionSyntax)
            End If

            If expression IsNot Nothing Then
                If expression.IsInRefContext(semanticModel, cancellationToken) Then
                    Return True
                End If

                If TypeOf expression.Parent Is AssignmentStatementSyntax Then
                    Dim assignmentStatement = DirectCast(expression.Parent, AssignmentStatementSyntax)
                    If expression Is assignmentStatement.Left Then
                        Return True
                    End If
                End If

                If expression.IsChildNode(Of NamedFieldInitializerSyntax)(Function(n) n.Name) Then
                    Return True
                End If

                Return False
            End If

            Return False
        End Function
    End Module
End Namespace
