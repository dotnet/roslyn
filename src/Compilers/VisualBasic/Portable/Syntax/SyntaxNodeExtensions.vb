' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Scanner = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.Scanner

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Module SyntaxNodeExtensions

        <Extension()>
        Public Function WithAnnotations(Of TNode As VisualBasicSyntaxNode)(node As TNode, ParamArray annotations As SyntaxAnnotation()) As TNode
            Return DirectCast(node.Green.SetAnnotations(annotations).CreateRed(), TNode)
        End Function

        ''' <summary>
        ''' Find enclosing WithStatement if it exists.
        ''' </summary>
        ''' <param name="node"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <Extension()> _
        Public Function ContainingWithStatement(node As VisualBasicSyntaxNode) As WithStatementSyntax
            Debug.Assert(node IsNot Nothing)

            If node Is Nothing Then
                Return Nothing
            End If

            node = node.Parent
            While node IsNot Nothing
                Select Case node.Kind
                    Case SyntaxKind.WithBlock
                        Return DirectCast(node, WithBlockSyntax).WithStatement

                    Case SyntaxKind.SubBlock,
                        SyntaxKind.FunctionBlock,
                        SyntaxKind.PropertyBlock,
                        SyntaxKind.ConstructorBlock,
                        SyntaxKind.OperatorBlock,
                        SyntaxKind.EventBlock
                        ' Don't look outside the current method/property/operator/event
                        Exit While

                End Select

                node = node.Parent
            End While

            Return Nothing
        End Function

        <Extension()> _
        Public Sub GetAncestors(Of T As VisualBasicSyntaxNode, C As VisualBasicSyntaxNode)(node As VisualBasicSyntaxNode, result As ArrayBuilder(Of T))

            Dim current = node.Parent
            Do While current IsNot Nothing AndAlso Not (TypeOf current Is C)
                If TypeOf current Is T Then
                    result.Add(DirectCast(current, T))
                End If
                current = current.Parent
            Loop

            result.ReverseContents()
        End Sub

        <Extension()> _
        Public Function GetAncestorOrSelf(Of T As VisualBasicSyntaxNode)(node As VisualBasicSyntaxNode) As T

            Do While node IsNot Nothing
                Dim result = TryCast(node, T)
                If result IsNot Nothing Then
                    Return result
                End If
                node = node.Parent
            Loop

            Return Nothing
        End Function

        <Extension()>
        Public Function IsLambdaExpressionSyntax(this As SyntaxNode) As Boolean
            Select Case this.Kind
                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return True
            End Select

            Return False
        End Function

        ''' <summary>
        ''' Simplified version of ExtractAnonymousTypeMemberName implemented on inner tokens.
        ''' </summary>
        <Extension()>
        Friend Function ExtractAnonymousTypeMemberName(input As ExpressionSyntax, <Out()> ByRef failedToInferFromXmlName As XmlNameSyntax) As SyntaxToken
            ' TODO: revise and remove code duplication
            failedToInferFromXmlName = Nothing

TryAgain:
            Select Case input.Kind
                Case SyntaxKind.IdentifierName
                    Return DirectCast(input, IdentifierNameSyntax).Identifier

                Case SyntaxKind.XmlName
                    Dim xmlNameInferredFrom = DirectCast(input, XmlNameSyntax)
                    If Not Scanner.IsIdentifier(xmlNameInferredFrom.LocalName.ToString) Then
                        failedToInferFromXmlName = xmlNameInferredFrom
                        Return Nothing
                    End If

                    Return xmlNameInferredFrom.LocalName

                Case SyntaxKind.XmlBracketedName
                    ' handles something like <a-a>
                    Dim xmlNameInferredFrom = DirectCast(input, XmlBracketedNameSyntax)
                    input = xmlNameInferredFrom.Name
                    GoTo TryAgain

                Case SyntaxKind.SimpleMemberAccessExpression,
                     SyntaxKind.DictionaryAccessExpression

                    Dim memberAccess = DirectCast(input, MemberAccessExpressionSyntax)

                    If input.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                        ' See if this is an identifier qualified with XmlElementAccessExpression or XmlDescendantAccessExpression
                        Dim receiver As ExpressionSyntax = If(memberAccess.Expression, GetCorrespondingConditionalAccessReceiver(memberAccess))

                        If receiver IsNot Nothing Then
                            Select Case receiver.Kind
                                Case SyntaxKind.XmlElementAccessExpression,
                                    SyntaxKind.XmlDescendantAccessExpression

                                    input = receiver
                                    GoTo TryAgain
                            End Select
                        End If
                    End If

                    input = memberAccess.Name
                    GoTo TryAgain

                Case SyntaxKind.XmlElementAccessExpression,
                     SyntaxKind.XmlAttributeAccessExpression,
                     SyntaxKind.XmlDescendantAccessExpression

                    Dim xmlAccess = DirectCast(input, XmlMemberAccessExpressionSyntax)

                    input = xmlAccess.Name
                    GoTo TryAgain

                Case SyntaxKind.InvocationExpression
                    Dim invocation = DirectCast(input, InvocationExpressionSyntax)

                    Dim target As ExpressionSyntax = If(invocation.Expression, GetCorrespondingConditionalAccessReceiver(invocation))

                    If target Is Nothing Then
                        Exit Select
                    End If

                    If invocation.ArgumentList Is Nothing OrElse invocation.ArgumentList.Arguments.Count = 0 Then
                        input = target
                        GoTo TryAgain
                    End If

                    Debug.Assert(invocation.ArgumentList IsNot Nothing)

                    If invocation.ArgumentList.Arguments.Count = 1 Then
                        ' See if this is an indexed XmlElementAccessExpression or XmlDescendantAccessExpression
                        Select Case target.Kind
                            Case SyntaxKind.XmlElementAccessExpression,
                                SyntaxKind.XmlDescendantAccessExpression
                                input = target
                                GoTo TryAgain
                        End Select
                    End If

                Case SyntaxKind.ConditionalAccessExpression
                    input = DirectCast(input, ConditionalAccessExpressionSyntax).WhenNotNull
                    GoTo TryAgain
            End Select

            Return Nothing
        End Function

        Private Function GetCorrespondingConditionalAccessReceiver(node As ExpressionSyntax) As ExpressionSyntax
            Dim access As ConditionalAccessExpressionSyntax = GetCorrespondingConditionalAccessExpression(node)

            If access IsNot Nothing Then
                Return access.Expression
            End If

            Return Nothing
        End Function

        <Extension>
        Friend Function GetCorrespondingConditionalAccessExpression(node As ExpressionSyntax) As ConditionalAccessExpressionSyntax
            Dim access As VisualBasicSyntaxNode = node
            Dim parent As VisualBasicSyntaxNode = access.Parent

            While parent IsNot Nothing
                Select Case parent.Kind
                    Case SyntaxKind.DictionaryAccessExpression,
                         SyntaxKind.SimpleMemberAccessExpression

                        If DirectCast(parent, MemberAccessExpressionSyntax).Expression IsNot access Then
                            Return Nothing
                        End If

                    Case SyntaxKind.XmlElementAccessExpression,
                         SyntaxKind.XmlDescendantAccessExpression,
                         SyntaxKind.XmlAttributeAccessExpression

                        If DirectCast(parent, XmlMemberAccessExpressionSyntax).Base IsNot access Then
                            Return Nothing
                        End If

                    Case SyntaxKind.InvocationExpression

                        If DirectCast(parent, InvocationExpressionSyntax).Expression IsNot access Then
                            Return Nothing
                        End If

                    Case SyntaxKind.ConditionalAccessExpression

                        Dim conditional = DirectCast(parent, ConditionalAccessExpressionSyntax)
                        If conditional.WhenNotNull Is access Then
                            Return conditional
                        ElseIf conditional.Expression IsNot access Then
                            Return Nothing
                        End If

                    Case Else
                        Return Nothing
                End Select

                access = parent
                parent = access.Parent
            End While

            Return Nothing
        End Function

        <Extension>
        Friend Function GetLeafAccess(conditionalAccess As ConditionalAccessExpressionSyntax) As ExpressionSyntax
            Dim access As ExpressionSyntax = conditionalAccess.WhenNotNull

            Do
                Select Case access.Kind
                    Case SyntaxKind.DictionaryAccessExpression,
                         SyntaxKind.SimpleMemberAccessExpression

                        Dim memberAccess = DirectCast(access, MemberAccessExpressionSyntax)
                        If memberAccess.Expression Is Nothing Then
                            Return memberAccess
                        Else
                            access = memberAccess.Expression
                        End If

                    Case SyntaxKind.XmlElementAccessExpression,
                         SyntaxKind.XmlDescendantAccessExpression,
                         SyntaxKind.XmlAttributeAccessExpression

                        Dim memberAccess = DirectCast(access, XmlMemberAccessExpressionSyntax)
                        If memberAccess.Base Is Nothing Then
                            Return memberAccess
                        Else
                            access = memberAccess.Base
                        End If

                    Case SyntaxKind.InvocationExpression

                        Dim invocation = DirectCast(access, InvocationExpressionSyntax)
                        If invocation.Expression Is Nothing Then
                            Return invocation
                        Else
                            access = invocation.Expression
                        End If

                    Case SyntaxKind.ConditionalAccessExpression

                        access = DirectCast(access, ConditionalAccessExpressionSyntax).Expression

                        If access Is Nothing Then
                            ' Must be a syntax error
                            Return Nothing
                        End If

                    Case Else
                        Return Nothing
                End Select
            Loop
        End Function

        ''' <summary>
        ''' Returns true if all arguments are of the specified kind and they are also missing.
        ''' </summary>
        <Extension()>
        Public Function AllAreMissing(arguments As IEnumerable(Of VisualBasicSyntaxNode), kind As SyntaxKind) As Boolean
            Return Not arguments.Any(Function(arg) Not (arg.Kind = kind AndAlso DirectCast(arg, IdentifierNameSyntax).IsMissing))
        End Function

        ''' <summary>
        ''' Returns true if all arguments are missing.
        ''' </summary>
        ''' <param name="arguments"></param>
        <Extension()>
        Public Function AllAreMissingIdentifierName(arguments As IEnumerable(Of VisualBasicSyntaxNode)) As Boolean
            Return arguments.AllAreMissing(SyntaxKind.IdentifierName)
        End Function

        ''' <summary>
        ''' Given a syntax node of query clause returns its leading keyword
        ''' </summary>
        <Extension()>
        Public Function QueryClauseKeywordOrRangeVariableIdentifier(syntax As SyntaxNode) As SyntaxToken
            Select Case syntax.Kind

                Case SyntaxKind.CollectionRangeVariable
                    Return DirectCast(syntax, CollectionRangeVariableSyntax).Identifier.Identifier

                Case SyntaxKind.ExpressionRangeVariable
                    Return DirectCast(syntax, ExpressionRangeVariableSyntax).NameEquals.Identifier.Identifier

                Case SyntaxKind.FromClause
                    Return DirectCast(syntax, FromClauseSyntax).FromKeyword

                Case SyntaxKind.FromClause
                    Return DirectCast(syntax, FromClauseSyntax).FromKeyword

                Case SyntaxKind.LetClause
                    Return DirectCast(syntax, LetClauseSyntax).LetKeyword

                Case SyntaxKind.AggregateClause
                    Return DirectCast(syntax, AggregateClauseSyntax).AggregateKeyword

                Case SyntaxKind.DistinctClause
                    Return DirectCast(syntax, DistinctClauseSyntax).DistinctKeyword

                Case SyntaxKind.WhereClause
                    Return DirectCast(syntax, WhereClauseSyntax).WhereKeyword

                Case SyntaxKind.SkipWhileClause, SyntaxKind.TakeWhileClause
                    Return DirectCast(syntax, PartitionWhileClauseSyntax).SkipOrTakeKeyword

                Case SyntaxKind.SkipClause, SyntaxKind.TakeClause
                    Return DirectCast(syntax, PartitionClauseSyntax).SkipOrTakeKeyword

                Case SyntaxKind.GroupByClause
                    Return DirectCast(syntax, GroupByClauseSyntax).GroupKeyword

                Case SyntaxKind.GroupJoinClause
                    Return DirectCast(syntax, GroupJoinClauseSyntax).GroupKeyword

                Case SyntaxKind.SimpleJoinClause
                    Return DirectCast(syntax, SimpleJoinClauseSyntax).JoinKeyword

                Case SyntaxKind.OrderByClause
                    Return DirectCast(syntax, OrderByClauseSyntax).OrderKeyword

                Case SyntaxKind.SelectClause
                    Return DirectCast(syntax, SelectClauseSyntax).SelectKeyword

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(syntax.Kind)
            End Select
        End Function

        <Extension>
        Friend Function EnclosingStructuredTrivia(node As VisualBasicSyntaxNode) As StructuredTriviaSyntax
            While node IsNot Nothing
                If node.IsStructuredTrivia Then
                    Return DirectCast(node, StructuredTriviaSyntax)
                Else
                    node = node.Parent
                End If
            End While
            Return Nothing
        End Function

    End Module
End Namespace
