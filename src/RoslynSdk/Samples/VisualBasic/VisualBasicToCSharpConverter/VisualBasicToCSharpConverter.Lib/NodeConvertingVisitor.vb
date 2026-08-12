' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CSharp.Symbols
Imports Microsoft.CodeAnalysis.CSharp.Syntax
Imports Microsoft.CodeAnalysis.CSharp.SyntaxExtensions
Imports Microsoft.CodeAnalysis.CSharp.SyntaxFactory
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports CS = Microsoft.CodeAnalysis.CSharp
Imports Extension = System.Runtime.CompilerServices.ExtensionAttribute
Imports VB = Microsoft.CodeAnalysis.VisualBasic

Namespace VisualBasicToCSharpConverter

    Partial Public Class Converter

        Private Class NodeConvertingVisitor
            Inherits VisualBasicSyntaxVisitor(Of SyntaxNode)

            Private Shared ReadOnly VoidKeyword As SyntaxToken = Token(CS.SyntaxKind.VoidKeyword)
            Private Shared ReadOnly SemicolonToken As SyntaxToken = Token(CS.SyntaxKind.SemicolonToken)
            Private Shared ReadOnly MissingSemicolonToken As SyntaxToken = MissingToken(CS.SyntaxKind.SemicolonToken)
            ' This is a hack. But because this will be written out to text it'll work.
            Private Shared ReadOnly SystemRuntimeInteropServicesCharSetName As CS.Syntax.NameSyntax = ParseName("global::System.Runtime.InteropServices.CharSet")
            Private Shared ReadOnly SystemRuntimeInteropServicesCharSetAnsiExpression As CS.Syntax.MemberAccessExpressionSyntax = MemberAccessExpression(CS.SyntaxKind.SimpleMemberAccessExpression, SystemRuntimeInteropServicesCharSetName, IdentifierName("Ansi"))
            Private Shared ReadOnly SystemRuntimeInteropServicesCharSetUnicodeExpression As CS.Syntax.MemberAccessExpressionSyntax = MemberAccessExpression(CS.SyntaxKind.SimpleMemberAccessExpression, SystemRuntimeInteropServicesCharSetName, IdentifierName("Unicode"))
            Private Shared ReadOnly SystemRuntimeInteropServicesCharSetAutoExpression As CS.Syntax.MemberAccessExpressionSyntax = MemberAccessExpression(CS.SyntaxKind.SimpleMemberAccessExpression, SystemRuntimeInteropServicesCharSetName, IdentifierName("Auto"))

            ' This can change after visiting an OptionStatement.
            Private IsOptionExplicitOn As Boolean = True
            Private IsOptionCompareBinary As Boolean = True
            Private IsOptionStrictOn As Boolean = False
            Private IsOptionInferOn As Boolean = True

            Private ReadOnly RootNamespace As String = ""
            Private ReadOnly RootNamespaceName As CS.Syntax.NameSyntax = If(String.IsNullOrEmpty(RootNamespace),
                                                                     Nothing,
                                                                     ParseName(RootNamespace)
                                                                  )

            Protected Function DeriveName(expression As VB.Syntax.ExpressionSyntax) As String
                Do While TypeOf expression Is VB.Syntax.InvocationExpressionSyntax
                    expression = CType(expression, VB.Syntax.InvocationExpressionSyntax).Expression
                Loop

                Select Case expression.Kind
                    Case VB.SyntaxKind.SimpleMemberAccessExpression

                        Return CType(expression, VB.Syntax.MemberAccessExpressionSyntax).Name.Identifier.ValueText

                    Case VB.SyntaxKind.IdentifierName

                        Return CType(expression, VB.Syntax.IdentifierNameSyntax).Identifier.ValueText

                    Case VB.SyntaxKind.GenericName

                        Return CType(expression, VB.Syntax.GenericNameSyntax).Identifier.ValueText

                    Case Else
                        Return Nothing
                End Select
            End Function

            Protected Function DeriveRankSpecifiers(
                                   boundsOpt As VB.Syntax.ArgumentListSyntax,
                                   specifiersOpt As IEnumerable(Of VB.Syntax.ArrayRankSpecifierSyntax),
                                   Optional includeSizes As Boolean = False
                               ) As IEnumerable(Of CS.Syntax.ArrayRankSpecifierSyntax)

                Dim result As New List(Of CS.Syntax.ArrayRankSpecifierSyntax)()

                If boundsOpt IsNot Nothing Then
                    If includeSizes Then
                        result.Add(ArrayRankSpecifier(SeparatedList((From arg In boundsOpt.Arguments Select VisitArrayRankSpecifierSize(arg)).Cast(Of CS.Syntax.ExpressionSyntax))))
                    Else
                        result.Add(ArrayRankSpecifier(OmittedArraySizeExpressionList(Of CS.Syntax.ExpressionSyntax)(boundsOpt.Arguments.Count)))
                    End If
                End If

                If specifiersOpt IsNot Nothing Then
                    For Each ars In specifiersOpt
                        result.Add(ArrayRankSpecifier(OmittedArraySizeExpressionList(Of CS.Syntax.ExpressionSyntax)(ars.Rank)))
                    Next
                End If

                Return result

            End Function

            Protected Function DeriveInitializer(
                                   identifier As VB.Syntax.ModifiedIdentifierSyntax,
                                   asClauseOpt As VB.Syntax.AsClauseSyntax,
                                   Optional initializerOpt As VB.Syntax.EqualsValueSyntax = Nothing
                               ) As CS.Syntax.EqualsValueClauseSyntax

                If initializerOpt IsNot Nothing Then
                    Return Visit(initializerOpt)
                End If

                If asClauseOpt IsNot Nothing AndAlso asClauseOpt.IsKind(VB.SyntaxKind.AsNewClause) Then
                    Dim newExpression = DirectCast(asClauseOpt, VB.Syntax.AsNewClauseSyntax).NewExpression
                    Select Case newExpression.Kind
                        Case VB.SyntaxKind.ObjectCreationExpression
                            Return EqualsValueClause(VisitObjectCreationExpression(newExpression))
                        Case VB.SyntaxKind.ArrayCreationExpression
                            Return EqualsValueClause(VisitArrayCreationExpression(newExpression))
                        Case VB.SyntaxKind.AnonymousObjectCreationExpression
                            Return EqualsValueClause(VisitAnonymousObjectCreationExpression(newExpression))
                    End Select
                End If

                If identifier.ArrayBounds IsNot Nothing Then
                    Return EqualsValueClause(ArrayCreationExpression(DeriveType(identifier, asClauseOpt, initializerOpt, includeSizes:=True)))
                End If

                Return Nothing

            End Function

            Protected Function DeriveType(
                                   identifier As VB.Syntax.ModifiedIdentifierSyntax,
                                   asClause As VB.Syntax.AsClauseSyntax,
                                   initializer As VB.Syntax.EqualsValueSyntax,
                                   Optional includeSizes As Boolean = False,
                                   Optional isRangeVariable As Boolean = False
                               ) As CS.Syntax.TypeSyntax

                Dim type = DeriveType(identifier.Identifier, asClause, , initializer, isRangeVariable)

                ' TODO: Implement check for nullable var.
                If Not identifier.Nullable.IsKind(VB.SyntaxKind.None) Then
                    type = NullableType(type)
                End If

                If identifier.ArrayBounds IsNot Nothing OrElse
                   identifier.ArrayRankSpecifiers.Count > 0 Then

                    Return ArrayType(type, List(DeriveRankSpecifiers(identifier.ArrayBounds, identifier.ArrayRankSpecifiers, includeSizes)))
                End If

                Return type
            End Function

            Protected Function DeriveType(
                                   identifier As SyntaxToken,
                                   asClause As VB.Syntax.AsClauseSyntax,
                                   Optional methodKeyword As SyntaxToken = Nothing,
                                   Optional initializerOpt As VB.Syntax.EqualsValueSyntax = Nothing,
                                   Optional isRangeVariable As Boolean = False
                               ) As CS.Syntax.TypeSyntax

                If asClause IsNot Nothing Then

                    If asClause.IsKind(VB.SyntaxKind.AsNewClause) Then
                        Return IdentifierName("var")
                    Else
                        Return Visit(asClause)
                    End If

                ElseIf methodKeyword.IsKind(VB.SyntaxKind.SubKeyword) Then

                    Return PredefinedType(Token(CS.SyntaxKind.VoidKeyword))

                ElseIf initializerOpt IsNot Nothing AndAlso
                       IsOptionInferOn AndAlso
                       (identifier.Parent.Parent.Parent.IsKind(VB.SyntaxKind.LocalDeclarationStatement) OrElse
                        identifier.Parent.Parent.Parent.IsKind(VB.SyntaxKind.UsingStatement)) Then

                    Return IdentifierName("var")

                ElseIf isRangeVariable Then

                    ' C# collection range variables omit their type.
                    Return Nothing

                Else
                    Dim text = identifier.ToString()

                    If Not Char.IsLetterOrDigit(text(text.Length - 1)) Then

                        Select Case text(text.Length - 1)
                            Case "!"c
                                Return PredefinedType(Token(CS.SyntaxKind.FloatKeyword))
                            Case "@"c
                                Return PredefinedType(Token(CS.SyntaxKind.DecimalKeyword))
                            Case "#"c
                                Return PredefinedType(Token(CS.SyntaxKind.DoubleKeyword))
                            Case "$"c
                                Return PredefinedType(Token(CS.SyntaxKind.StringKeyword))
                            Case "%"c
                                Return PredefinedType(Token(CS.SyntaxKind.IntKeyword))
                            Case "&"c
                                Return PredefinedType(Token(CS.SyntaxKind.LongKeyword))
                        End Select
                    End If
                End If

                ' If no AsClause is provided and no type characters are present and this isn't a Sub declaration pick Object or Dynamic based on Option Strict setting.
                If IsOptionStrictOn Then
                    Return PredefinedType(Token(CS.SyntaxKind.ObjectKeyword))
                Else
                    Return IdentifierName("dynamic")
                End If

            End Function

            Protected Function DeriveType(declarator As VB.Syntax.CollectionRangeVariableSyntax) As CS.Syntax.TypeSyntax

                Return DeriveType(declarator.Identifier, declarator.AsClause, initializer:=Nothing, isRangeVariable:=True)

            End Function

            Protected Function TransferTrivia(source As SyntaxNode, target As SyntaxNode) As SyntaxNode

                Return target.WithTrivia(VisitTrivia(source.GetLeadingTrivia()), VisitTrivia(source.GetTrailingTrivia()))

            End Function

            Public Overloads Function Visit(nodes As IEnumerable(Of SyntaxNode)) As IEnumerable(Of SyntaxNode)

                Return From node In nodes Select Visit(node)

            End Function

            Public Overloads Function Visit(statements As IEnumerable(Of VB.Syntax.StatementSyntax)) As IEnumerable(Of SyntaxNode)

                ' VB variable declarations allow multiple types and variables. In order to translate to proper C# code
                ' we have to flatten the list here by raising each declarator to the level of its parent.
                Return Aggregate
                           node In statements
                       Into
                           SelectMany(Flatten(node))


            End Function

            ' TODO: This suppression should be removed once we have rulesets in place for Roslyn.sln
            <SuppressMessage("", "RS0002")>
            Function Flatten(statement As SyntaxNode) As IEnumerable(Of SyntaxNode)

                Select Case statement.Kind
                    Case VB.SyntaxKind.FieldDeclaration
                        Return Aggregate node In CType(statement, VB.Syntax.FieldDeclarationSyntax).Declarators Into SelectMany(VisitVariableDeclaratorVariables(node))
                    Case VB.SyntaxKind.LocalDeclarationStatement
                        Return Aggregate node In CType(statement, VB.Syntax.LocalDeclarationStatementSyntax).Declarators Into SelectMany(VisitVariableDeclaratorVariables(node))
                    Case Else
                        Return {Visit(statement)}
                End Select

            End Function

            Public Overrides Function VisitAccessorStatement(node As VB.Syntax.AccessorStatementSyntax) As SyntaxNode

                Dim accessorBlock As VB.Syntax.AccessorBlockSyntax = node.Parent

                Dim kind As CS.SyntaxKind
                Select Case node.Kind
                    Case VB.SyntaxKind.GetAccessorStatement
                        kind = CS.SyntaxKind.GetAccessorDeclaration
                    Case VB.SyntaxKind.SetAccessorStatement
                        kind = CS.SyntaxKind.SetAccessorDeclaration
                    Case VB.SyntaxKind.AddHandlerAccessorStatement
                        kind = CS.SyntaxKind.AddAccessorDeclaration
                    Case VB.SyntaxKind.RemoveHandlerAccessorStatement
                        kind = CS.SyntaxKind.RemoveAccessorDeclaration
                    Case VB.SyntaxKind.RaiseEventAccessorStatement

                        ' TODO: Transform RaiseEvent accessor into a method.
                        Throw New NotImplementedException()

                    Case Else
                        Throw New NotSupportedException(node.Kind.ToString())
                End Select

                Return TransferTrivia(accessorBlock, AccessorDeclaration(kind).WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))).WithModifiers(TokenList(VisitModifiers(node.Modifiers))).WithBody(Block(List(Visit(accessorBlock.Statements)))))

            End Function

            Public Overrides Function VisitAddRemoveHandlerStatement(node As VB.Syntax.AddRemoveHandlerStatementSyntax) As SyntaxNode

                If node.IsKind(VB.SyntaxKind.AddHandlerStatement) Then
                    Return TransferTrivia(node, ExpressionStatement(AssignmentExpression(CS.SyntaxKind.AddAssignmentExpression, Visit(node.EventExpression), Visit(node.DelegateExpression))))
                Else
                    Return TransferTrivia(node, ExpressionStatement(AssignmentExpression(CS.SyntaxKind.SubtractAssignmentExpression, Visit(node.EventExpression), Visit(node.DelegateExpression))))
                End If

            End Function

            Public Overrides Function VisitAnonymousObjectCreationExpression(node As VB.Syntax.AnonymousObjectCreationExpressionSyntax) As SyntaxNode

                Return AnonymousObjectCreationExpression(SeparatedList(Visit(node.Initializer.Initializers).Cast(Of CS.Syntax.AnonymousObjectMemberDeclaratorSyntax)))

            End Function

            Public Overrides Function VisitArgumentList(node As VB.Syntax.ArgumentListSyntax) As SyntaxNode

                If node Is Nothing Then Return ArgumentList()

                Return ArgumentList(SeparatedList(Visit(node.Arguments).Cast(Of CS.Syntax.ArgumentSyntax)))

            End Function

            Public Overrides Function VisitArrayCreationExpression(node As VB.Syntax.ArrayCreationExpressionSyntax) As SyntaxNode

                Return ArrayCreationExpression(ArrayType(Visit(node.Type)) _
                        .WithRankSpecifiers(List(DeriveRankSpecifiers(node.ArrayBounds, node.RankSpecifiers, includeSizes:=True)))) _
                        .WithInitializer(If(node.ArrayBounds IsNot Nothing AndAlso node.Initializer.Initializers.Count = 0, Nothing, VisitCollectionInitializer(node.Initializer)))

            End Function

            Public Overrides Function VisitArrayRankSpecifier(node As VB.Syntax.ArrayRankSpecifierSyntax) As SyntaxNode

                Return ArrayRankSpecifier(OmittedArraySizeExpressionList(Of CS.Syntax.ExpressionSyntax)(node.Rank))

            End Function

            Protected Overloads Function VisitArrayRankSpecifierSize(node As VB.Syntax.ArgumentSyntax) As SyntaxNode

                If node Is Nothing Then Return Nothing

                If TypeOf node Is VB.Syntax.RangeArgumentSyntax Then
                    Dim arg As VB.Syntax.RangeArgumentSyntax = node

                    Return VisitArrayBound(arg.UpperBound)
                Else
                    Return VisitArrayBound(CType(node, VB.Syntax.SimpleArgumentSyntax).Expression)
                End If

            End Function

            Protected Function VisitArrayBound(expression As SyntaxNode) As SyntaxNode

                If expression.Kind = VB.SyntaxKind.SubtractExpression Then
                    Dim be As VB.Syntax.BinaryExpressionSyntax = expression

                    If be.Right.Kind = VB.SyntaxKind.NumericLiteralExpression Then
                        If CInt(CType(be.Right, VB.Syntax.LiteralExpressionSyntax).Token.Value) = 1 Then
                            Return Visit(be.Left)
                        End If
                    End If

                ElseIf expression.Kind = VB.SyntaxKind.NumericLiteralExpression Then

                    ' Practically speaking this can only legally be -1.
                    Dim length = CInt(CType(expression, VB.Syntax.LiteralExpressionSyntax).Token.Value) + 1

                    Return LiteralExpression(CS.SyntaxKind.NumericLiteralExpression, Literal(CStr(length), length))

                ElseIf expression.IsKind(VB.SyntaxKind.UnaryMinusExpression) Then

                    Dim negate As VB.Syntax.UnaryExpressionSyntax = expression
                    If negate.Operand.IsKind(VB.SyntaxKind.NumericLiteralExpression) Then
                        Dim length = -CInt(CType(negate.Operand, VB.Syntax.LiteralExpressionSyntax).Token.Value) + 1

                        Return LiteralExpression(CS.SyntaxKind.NumericLiteralExpression, Literal(CStr(length), length))
                    End If

                End If

                Return BinaryExpression(
                           CS.SyntaxKind.AddExpression,
                           ParenthesizedExpression(Visit(expression)),
                           LiteralExpression(CS.SyntaxKind.NumericLiteralExpression, Literal("1", 1))
                       )

            End Function

            Public Overrides Function VisitArrayType(node As VB.Syntax.ArrayTypeSyntax) As SyntaxNode

                Return ArrayType(Visit(node.ElementType), List(DeriveRankSpecifiers(Nothing, node.RankSpecifiers.ToList())))

            End Function

            Public Overrides Function VisitAssignmentStatement(node As VB.Syntax.AssignmentStatementSyntax) As SyntaxNode

                Return TransferTrivia(node, ExpressionStatement(AssignmentExpression(CS.SyntaxKind.SimpleAssignmentExpression, Visit(node.Left), Visit(node.Right))))

            End Function

            Public Overrides Function VisitAttribute(node As VB.Syntax.AttributeSyntax) As SyntaxNode

                Return TransferTrivia(node.Parent, AttributeList(SingletonSeparatedList(Attribute(Visit(node.Name), VisitAttributeArgumentList(node.ArgumentList)))).WithTarget(VisitAttributeTarget(node.Target)))

            End Function

            Protected Function VisitAttributeArgumentList(node As VB.Syntax.ArgumentListSyntax) As SyntaxNode

                If node Is Nothing Then Return Nothing

                Return AttributeArgumentList(SeparatedList(Visit(node.Arguments).Cast(Of CS.Syntax.AttributeArgumentSyntax)))

            End Function

            Public Overrides Function VisitAttributeList(node As VB.Syntax.AttributeListSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Protected Function VisitAttributeLists(nodes As IEnumerable(Of VB.Syntax.AttributeListSyntax)) As IEnumerable(Of SyntaxNode)

                Return Visit((From list In nodes, attribute In list.Attributes Select attribute))

            End Function

            Public Overrides Function VisitAttributesStatement(node As VB.Syntax.AttributesStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Protected Function VisitAttributeStatements(statements As IEnumerable(Of VB.Syntax.AttributesStatementSyntax)) As IEnumerable(Of SyntaxNode)

                ' TOOD: AttributeStatement contains a list of blocks but there is only ever one block in the list.
                Return Visit((From statement In statements, list In statement.AttributeLists, attribute In list.Attributes Select attribute))

            End Function

            Public Overrides Function VisitAttributeTarget(node As VB.Syntax.AttributeTargetSyntax) As SyntaxNode

                If node Is Nothing Then Return Nothing

                If node.AttributeModifier.IsKind(VB.SyntaxKind.AssemblyKeyword) Then
                    Return AttributeTargetSpecifier(Token(CS.SyntaxKind.AssemblyKeyword))
                Else
                    Return AttributeTargetSpecifier(Token(CS.SyntaxKind.ModuleKeyword))
                End If

            End Function

            Public Overrides Function VisitAwaitExpression(node As VB.Syntax.AwaitExpressionSyntax) As SyntaxNode

                Return AwaitExpression(Visit(node.Expression))

            End Function

            Public Overrides Function VisitBadDirectiveTrivia(node As VB.Syntax.BadDirectiveTriviaSyntax) As SyntaxNode

                Throw New NotImplementedException(node.ToString())

            End Function

            Public Overrides Function VisitBinaryConditionalExpression(node As VB.Syntax.BinaryConditionalExpressionSyntax) As SyntaxNode

                Return BinaryExpression(CS.SyntaxKind.CoalesceExpression, Visit(node.FirstExpression), Visit(node.SecondExpression))

            End Function

            Public Overrides Function VisitBinaryExpression(node As VB.Syntax.BinaryExpressionSyntax) As SyntaxNode

                Dim kind As CS.SyntaxKind

                Select Case node.Kind
                    Case VB.SyntaxKind.AddExpression
                        kind = CS.SyntaxKind.AddExpression
                    Case VB.SyntaxKind.SubtractExpression
                        kind = CS.SyntaxKind.SubtractExpression
                    Case VB.SyntaxKind.MultiplyExpression
                        kind = CS.SyntaxKind.MultiplyExpression
                    Case VB.SyntaxKind.DivideExpression

                        kind = CS.SyntaxKind.DivideExpression
                    ' TODO: Transform into cast with division if needed.

                    Case VB.SyntaxKind.ModuloExpression
                        kind = CS.SyntaxKind.ModuloExpression

                    Case VB.SyntaxKind.IntegerDivideExpression

                        kind = CS.SyntaxKind.DivideExpression
                    ' TODO: Transform into user-defined operator method call if needed.

                    Case VB.SyntaxKind.ExponentiateExpression

                        'TODO: Transform into call to Math.Pow.
                        Return ExpressionStatement(
                                   InvocationExpression(
                                       ParseName("global::System.Math.Pow"),
                                       ArgumentList(
                                           SeparatedList({
                                                CS.SyntaxFactory.Argument(Visit(node.Left)),
                                                CS.SyntaxFactory.Argument(Visit(node.Right))}
                                           )
                                       )
                                   )
                               )

                        Return NotImplementedExpression(node)

                        Throw New NotImplementedException(node.ToString())

                    Case VB.SyntaxKind.EqualsExpression
                        kind = CS.SyntaxKind.EqualsExpression
                    Case VB.SyntaxKind.NotEqualsExpression
                        kind = CS.SyntaxKind.NotEqualsExpression
                    Case VB.SyntaxKind.LessThanExpression
                        kind = CS.SyntaxKind.LessThanExpression
                    Case VB.SyntaxKind.LessThanOrEqualExpression
                        kind = CS.SyntaxKind.LessThanOrEqualExpression
                    Case VB.SyntaxKind.GreaterThanExpression
                        kind = CS.SyntaxKind.GreaterThanExpression
                    Case VB.SyntaxKind.GreaterThanOrEqualExpression
                        kind = CS.SyntaxKind.GreaterThanOrEqualExpression

                    Case VB.SyntaxKind.IsExpression

                        ' TODO: Transform into call to Object.ReferenceEquals as necessary.
                        kind = CS.SyntaxKind.EqualsExpression

                    Case VB.SyntaxKind.IsNotExpression

                        ' TODO: Transform into NotExpression of call to Object.ReferenceEquals as necessary.
                        kind = CS.SyntaxKind.NotEqualsExpression

                    Case VB.SyntaxKind.LeftShiftExpression
                        kind = CS.SyntaxKind.LeftShiftExpression
                    Case VB.SyntaxKind.RightShiftExpression
                        kind = CS.SyntaxKind.RightShiftExpression
                    Case VB.SyntaxKind.AndExpression
                        kind = CS.SyntaxKind.BitwiseAndExpression
                    Case VB.SyntaxKind.AndAlsoExpression
                        kind = CS.SyntaxKind.LogicalAndExpression
                    Case VB.SyntaxKind.OrExpression
                        kind = CS.SyntaxKind.BitwiseOrExpression
                    Case VB.SyntaxKind.OrElseExpression
                        kind = CS.SyntaxKind.LogicalOrExpression
                    Case VB.SyntaxKind.ExclusiveOrExpression
                        kind = CS.SyntaxKind.ExclusiveOrExpression

                    Case VB.SyntaxKind.ConcatenateExpression

                        kind = CS.SyntaxKind.AddExpression

                    ' TODO: Transform into call to user-defined operator if needed (e.g. for user-defined operator).

                    Case VB.SyntaxKind.LikeExpression

                        Return NotImplementedExpression(node)

                        Throw New NotSupportedException(node.Kind.ToString())

                    Case Else

                        Return NotImplementedExpression(node)

                        Throw New NotSupportedException(node.Kind.ToString())
                End Select

                Return BinaryExpression(kind, Visit(node.Left), Visit(node.Right))
            End Function

            Public Overrides Function VisitCallStatement(node As VB.Syntax.CallStatementSyntax) As SyntaxNode

                Return TransferTrivia(node, ExpressionStatement(VisitInvocationExpression(node.Invocation)))

            End Function

            Public Overrides Function VisitExpressionStatement(node As VB.Syntax.ExpressionStatementSyntax) As SyntaxNode

                Return TransferTrivia(node, ExpressionStatement(Visit(node.Expression)))

            End Function

            Public Overrides Function VisitCaseBlock(node As VB.Syntax.CaseBlockSyntax) As SyntaxNode

                Dim statements = Visit(node.Statements)
                If Not node.IsKind(VB.SyntaxKind.CaseElseBlock) Then
                    statements = statements.Union({BreakStatement()})
                End If

                Return TransferTrivia(node, SwitchSection(
                                                List(Visit(node.CaseStatement.Cases)),
                                                List(statements)
                                            )
                       )

            End Function

            Protected Function VisitCaseBlocks(blocks As IEnumerable(Of VB.Syntax.CaseBlockSyntax)) As IEnumerable(Of SyntaxNode)

                Return From b In blocks Select VisitCaseBlock(b)

            End Function

            Public Overrides Function VisitElseCaseClause(node As VB.Syntax.ElseCaseClauseSyntax) As SyntaxNode

                Return DefaultSwitchLabel()

            End Function

            Public Overrides Function VisitRangeCaseClause(node As VB.Syntax.RangeCaseClauseSyntax) As SyntaxNode

                Return CaseSwitchLabel(NotImplementedExpression(node))

                ' TODO: Rewrite this to an if statement.
                Throw New NotImplementedException(node.ToString())

            End Function

            Public Overrides Function VisitRelationalCaseClause(node As VB.Syntax.RelationalCaseClauseSyntax) As SyntaxNode

                Return CaseSwitchLabel(NotImplementedExpression(node))

                ' TODO: Rewrite this to an if statement.
                Throw New NotImplementedException(node.ToString())

            End Function

            Public Overrides Function VisitCaseStatement(node As VB.Syntax.CaseStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitSimpleCaseClause(node As VB.Syntax.SimpleCaseClauseSyntax) As SyntaxNode

                Return CaseSwitchLabel(Visit(node.Value))

            End Function

            Public Overrides Function VisitDirectCastExpression(node As VB.Syntax.DirectCastExpressionSyntax) As SyntaxNode
                Return ParenthesizedExpression(CastExpression(Visit(node.Type), Visit(node.Expression)))
            End Function

            Public Overrides Function VisitCTypeExpression(node As VB.Syntax.CTypeExpressionSyntax) As SyntaxNode
                Return ParenthesizedExpression(CastExpression(Visit(node.Type), Visit(node.Expression)))
            End Function

            Public Overrides Function VisitTryCastExpression(node As VB.Syntax.TryCastExpressionSyntax) As SyntaxNode
                Return ParenthesizedExpression(BinaryExpression(CS.SyntaxKind.AsExpression, Visit(node.Expression), Visit(node.Type)))
            End Function

            Public Overrides Function VisitCatchFilterClause(node As VB.Syntax.CatchFilterClauseSyntax) As SyntaxNode

                ' We could in theory translate this into a switch inside a catch.
                ' It's not really at all the same thing as a filter though so for now
                ' we'll just throw.
                Throw New NotSupportedException(node.Kind.ToString())

            End Function

            Public Overrides Function VisitCatchBlock(node As VB.Syntax.CatchBlockSyntax) As SyntaxNode

                Return CatchClause().WithDeclaration(VisitCatchStatement(node.CatchStatement)).WithBlock(Block(List(Visit(node.Statements))))

            End Function

            Public Overrides Function VisitCatchStatement(node As VB.Syntax.CatchStatementSyntax) As SyntaxNode


                If node.IdentifierName Is Nothing Then Return Nothing

                Dim result = CatchDeclaration(VisitSimpleAsClause(node.AsClause)).WithIdentifier(VisitIdentifier(node.IdentifierName.Identifier))

                If node.WhenClause IsNot Nothing Then result = result.WithTrailingTrivia({Comment("/* " & node.WhenClause.ToString() & " */")})

                Return result

            End Function

            Protected Function VisitCatchBlocks(parts As IEnumerable(Of VB.Syntax.CatchBlockSyntax)) As IEnumerable(Of SyntaxNode)

                Return From part In parts Select VisitCatchBlock(part)

            End Function

            Public Overrides Function VisitCollectionInitializer(node As VB.Syntax.CollectionInitializerSyntax) As SyntaxNode

                If node Is Nothing Then Return Nothing

                Select Case node.Parent.Kind
                    Case VB.SyntaxKind.ObjectCollectionInitializer,
                            VB.SyntaxKind.AsNewClause
                        Return InitializerExpression(CS.SyntaxKind.CollectionInitializerExpression, SeparatedList(Visit(node.Initializers).Cast(Of CS.Syntax.ExpressionSyntax)))

                    Case VB.SyntaxKind.ArrayCreationExpression
                        Return InitializerExpression(CS.SyntaxKind.ArrayInitializerExpression, SeparatedList(Visit(node.Initializers).Cast(Of CS.Syntax.ExpressionSyntax)))

                    Case Else

                        ' This covers array initializers in a variable declaration.
                        If node.Parent.IsKind(VB.SyntaxKind.EqualsValue) AndAlso
                           node.Parent.Parent.IsKind(VB.SyntaxKind.VariableDeclarator) AndAlso
                           CType(node.Parent.Parent, VB.Syntax.VariableDeclaratorSyntax).AsClause IsNot Nothing Then

                            Return InitializerExpression(CS.SyntaxKind.ArrayInitializerExpression, SeparatedList(Visit(node.Initializers).Cast(Of CS.Syntax.ExpressionSyntax)))
                        End If

                        ' This is an array literal.
                        ' TODO: Calculate the rank of this array initializer, right now it assumes rank 1.
                        Return ImplicitArrayCreationExpression(
                                   InitializerExpression(CS.SyntaxKind.ArrayInitializerExpression, SeparatedList(Visit(node.Initializers).Cast(Of CS.Syntax.ExpressionSyntax)))
                               )
                End Select

            End Function

            Public Overrides Function VisitCollectionRangeVariable(node As VB.Syntax.CollectionRangeVariableSyntax) As SyntaxNode
                Return MyBase.VisitCollectionRangeVariable(node)
            End Function

            Public Overrides Function VisitCompilationUnit(node As VB.Syntax.CompilationUnitSyntax) As SyntaxNode
                Dim usings = List(VisitImportsStatements(node.Imports))
                Dim attributes = List(VisitAttributeStatements(node.Attributes))
                Dim members = List(VisitMembers(node.Members))
                Dim root = CompilationUnit().WithUsings(usings).WithAttributeLists(attributes).WithMembers(members)
                Return root.NormalizeWhitespace()
            End Function

            Public Overrides Function VisitConstDirectiveTrivia(node As VB.Syntax.ConstDirectiveTriviaSyntax) As SyntaxNode

                If node.Value.IsKind(VB.SyntaxKind.TrueLiteralExpression) OrElse
                   node.Value.IsKind(VB.SyntaxKind.FalseLiteralExpression) Then

                    Return DefineDirectiveTrivia(VisitIdentifier(node.Name), isActive:=True)
                Else
                    Return BadDirectiveTrivia(MissingToken(CS.SyntaxKind.HashToken).WithTrailingTrivia(TriviaList(Comment("/* " & node.ToString() & " */"))), isActive:=True)

                    Throw New NotSupportedException("Non-boolean directive constants.")
                End If

            End Function

            Public Overrides Function VisitSubNewStatement(node As VB.Syntax.SubNewStatementSyntax) As SyntaxNode

                Dim typeName = CType(node.Parent.Parent, VB.Syntax.TypeBlockSyntax).BlockStatement.Identifier

                Dim subNewBlock As VB.Syntax.ConstructorBlockSyntax = node.Parent

                Dim initializer As CS.Syntax.ConstructorInitializerSyntax = Nothing

                ' Check for chained constructor call.
                If subNewBlock.Statements.Count >= 1 Then
                    Dim firstStatement = subNewBlock.Statements(0)
                    Dim invocationExpression As VB.Syntax.InvocationExpressionSyntax

                    Select Case firstStatement.Kind
                        Case VB.SyntaxKind.CallStatement
                            invocationExpression = TryCast(DirectCast(firstStatement, VB.Syntax.CallStatementSyntax).Invocation, VB.Syntax.InvocationExpressionSyntax)
                        Case VB.SyntaxKind.ExpressionStatement
                            invocationExpression = TryCast(DirectCast(firstStatement, VB.Syntax.ExpressionStatementSyntax).Expression, VB.Syntax.InvocationExpressionSyntax)
                        Case Else
                            invocationExpression = Nothing
                    End Select

                    If invocationExpression IsNot Nothing Then
                        Dim memberAccess = TryCast(invocationExpression.Expression, VB.Syntax.MemberAccessExpressionSyntax)
                        If memberAccess IsNot Nothing Then

                            If TypeOf memberAccess.Expression Is VB.Syntax.InstanceExpressionSyntax AndAlso
                                memberAccess.Name.Identifier.ToString().Equals("New", StringComparison.OrdinalIgnoreCase) Then

                                Select Case memberAccess.Expression.Kind
                                    Case VB.SyntaxKind.MeExpression, VB.SyntaxKind.MyClassExpression
                                        initializer = ConstructorInitializer(CS.SyntaxKind.ThisConstructorInitializer, VisitArgumentList(invocationExpression.ArgumentList))
                                    Case VB.SyntaxKind.MyBaseExpression
                                        initializer = ConstructorInitializer(CS.SyntaxKind.BaseConstructorInitializer, VisitArgumentList(invocationExpression.ArgumentList))
                                End Select
                            End If
                        End If
                    End If
                End If

                ' TODO: Fix trivia transfer so that trailing trivia on this node doesn't end up on the close curly.
                ' TODO: Implement trivia transfer so that trivia on the Me.New or MyBase.New call is not lost.
                Return TransferTrivia(node, ConstructorDeclaration(VisitIdentifier(typeName)) _
                                                .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                                                .WithParameterList(VisitParameterList(node.ParameterList)) _
                                                .WithInitializer(initializer) _
                                                .WithBody(Block(List(Visit(If(initializer Is Nothing, subNewBlock.Statements, subNewBlock.Statements.Skip(1))))))
                                                )

            End Function

            Public Overrides Function VisitContinueStatement(node As VB.Syntax.ContinueStatementSyntax) As SyntaxNode

                ' So long as this continue statement binds to its immediately enclosing loop this is simple.
                ' Otherwise it would require rewriting with goto statements.
                ' TODO: Consider implementing this using binding instead.
                Dim parent = node.Parent
                Do Until VB.SyntaxFacts.IsDoLoopBlock(parent.Kind) OrElse
                         parent.IsKind(VB.SyntaxKind.ForBlock) OrElse
                         parent.IsKind(VB.SyntaxKind.ForEachBlock) OrElse
                         parent.IsKind(VB.SyntaxKind.WhileBlock)

                    parent = parent.Parent
                Loop

                If (node.IsKind(VB.SyntaxKind.ContinueDoStatement) AndAlso VB.SyntaxFacts.IsDoLoopBlock(parent.Kind)) OrElse
                   (node.IsKind(VB.SyntaxKind.ContinueForStatement) AndAlso (parent.IsKind(VB.SyntaxKind.ForBlock) OrElse parent.IsKind(VB.SyntaxKind.ForEachBlock))) OrElse
                   (node.IsKind(VB.SyntaxKind.ContinueWhileStatement) AndAlso parent.IsKind(VB.SyntaxKind.WhileBlock)) Then

                    Return ContinueStatement()
                Else

                    Return NotImplementedStatement(node)

                    Throw New NotImplementedException("Rewriting Continue statements which branch out of their immediately containing loop block into gotos.")
                End If

            End Function

            Public Overrides Function VisitDeclareStatement(node As VB.Syntax.DeclareStatementSyntax) As SyntaxNode
                ' Declare Ansi|Unicode|Auto Sub|Function Name Lib "LibName" Alias "AliasName"(ParameterList)[As ReturnType]
                ' [DllImport("LibName", CharSet: CharSet.Ansi|Unicode|Auto, EntryPoint: AliasName|Name)]
                ' extern ReturnType|void Name(ParameterList);

                Dim charSet As CS.Syntax.ExpressionSyntax = Nothing
                If node.CharsetKeyword.IsKind(VB.SyntaxKind.None) Then
                    charSet = SystemRuntimeInteropServicesCharSetAutoExpression
                Else
                    Select Case node.CharsetKeyword.Kind
                        Case VB.SyntaxKind.AnsiKeyword
                            charSet = SystemRuntimeInteropServicesCharSetAnsiExpression
                        Case VB.SyntaxKind.UnicodeKeyword
                            charSet = SystemRuntimeInteropServicesCharSetUnicodeExpression
                        Case VB.SyntaxKind.AutoKeyword
                            charSet = SystemRuntimeInteropServicesCharSetAutoExpression
                    End Select
                End If

                Dim aliasString As String
                If node.AliasKeyword.IsKind(VB.SyntaxKind.None) Then
                    aliasString = node.Identifier.ValueText
                Else
                    aliasString = node.AliasName.Token.ValueText
                End If


                Dim dllImportAttribute = Attribute(
                                             ParseName("global::System.Runtime.InteropServices.DllImport"),
                                             AttributeArgumentList(SeparatedList({
                                                                       AttributeArgument(LiteralExpression(CS.SyntaxKind.StringLiteralExpression, Literal(node.LibraryName.Token.ToString(), node.LibraryName.Token.ValueText))),
                                                                       AttributeArgument(charSet).WithNameColon(NameColon(IdentifierName("CharSet"))),
                                                                       AttributeArgument(
                                                                           LiteralExpression(CS.SyntaxKind.StringLiteralExpression, Literal("""" & aliasString & """", aliasString))).WithNameColon(NameColon(IdentifierName("EntryPoint")))}
                                                                   )
                                             )
                                         )

                ' TODO: Transfer attributes on the return type to the statement.
                Return MethodDeclaration(DeriveType(node.Identifier, node.AsClause, node.SubOrFunctionKeyword), VisitIdentifier(node.Identifier)) _
                            .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists).Union({AttributeList(SingletonSeparatedList(dllImportAttribute))}))) _
                            .WithModifiers(TokenList(VisitModifiers(node.Modifiers).Union({Token(CS.SyntaxKind.ExternKeyword)}))) _
                            .WithParameterList(VisitParameterList(node.ParameterList))

            End Function

            Public Overrides Function VisitDelegateStatement(node As VB.Syntax.DelegateStatementSyntax) As SyntaxNode

                Return DelegateDeclaration(
                           DeriveType(node.Identifier, node.AsClause, node.SubOrFunctionKeyword),
                           VisitIdentifier(node.Identifier)) _
                       .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                       .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                       .WithTypeParameterList(VisitTypeParameterList(node.TypeParameterList)) _
                       .WithParameterList(VisitParameterList(node.ParameterList)) _
                       .WithConstraintClauses(List(VisitTypeParameterConstraintClauses(node.TypeParameterList)))

            End Function

            Public Overrides Function VisitDocumentationCommentTrivia(node As VB.Syntax.DocumentationCommentTriviaSyntax) As SyntaxNode
                Return DocumentationCommentTrivia(CS.SyntaxKind.SingleLineDocumentationCommentTrivia).WithEndOfComment(MissingToken(CS.SyntaxKind.EndOfDocumentationCommentToken).WithLeadingTrivia(TriviaList(Comment("/* " & node.ToString() & " */"))))
            End Function

            Public Overrides Function VisitDoLoopBlock(node As VB.Syntax.DoLoopBlockSyntax) As SyntaxNode

                Select Case node.Kind
                    Case VB.SyntaxKind.DoWhileLoopBlock, VB.SyntaxKind.DoUntilLoopBlock

                        Return WhileStatement(VisitWhileOrUntilClause(node.DoStatement.WhileOrUntilClause), Block(List(Visit(node.Statements))))

                    Case VB.SyntaxKind.DoLoopWhileBlock, VB.SyntaxKind.DoLoopUntilBlock

                        Return DoStatement(Block(List(Visit(node.Statements))), VisitWhileOrUntilClause(node.LoopStatement.WhileOrUntilClause))

                    Case VB.SyntaxKind.SimpleDoLoopBlock

                        Return WhileStatement(LiteralExpression(CS.SyntaxKind.TrueLiteralExpression), Block(List(Visit(node.Statements))))

                    Case Else
                        Throw New NotSupportedException(node.Kind.ToString())
                End Select

            End Function

            Public Overrides Function VisitDoStatement(node As VB.Syntax.DoStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitElseDirectiveTrivia(node As VB.Syntax.ElseDirectiveTriviaSyntax) As SyntaxNode

                Return ElseDirectiveTrivia(isActive:=True, branchTaken:=False)

            End Function

            Public Overrides Function VisitElseBlock(node As VB.Syntax.ElseBlockSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitElseStatement(node As VB.Syntax.ElseStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitEmptyStatement(node As VB.Syntax.EmptyStatementSyntax) As SyntaxNode

                Return TransferTrivia(node, EmptyStatement())

            End Function

            Public Overrides Function VisitEndBlockStatement(node As VB.Syntax.EndBlockStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitEndExternalSourceDirectiveTrivia(node As VB.Syntax.EndExternalSourceDirectiveTriviaSyntax) As SyntaxNode

                Return LineDirectiveTrivia(MissingToken(CS.SyntaxKind.NumericLiteralToken), isActive:=False)

            End Function

            Public Overrides Function VisitEndIfDirectiveTrivia(node As VB.Syntax.EndIfDirectiveTriviaSyntax) As SyntaxNode

                Return EndIfDirectiveTrivia(isActive:=False)

            End Function

            Public Overrides Function VisitEndRegionDirectiveTrivia(node As VB.Syntax.EndRegionDirectiveTriviaSyntax) As SyntaxNode

                Return EndRegionDirectiveTrivia(isActive:=False)

            End Function

            Public Overrides Function VisitEnumBlock(node As VB.Syntax.EnumBlockSyntax) As SyntaxNode

                Return VisitEnumStatement(node.EnumStatement)

            End Function

            Public Overrides Function VisitEnumMemberDeclaration(node As VB.Syntax.EnumMemberDeclarationSyntax) As SyntaxNode

                Return TransferTrivia(node, EnumMemberDeclaration(List(VisitAttributeLists(node.AttributeLists)), VisitIdentifier(node.Identifier), VisitEqualsValue(node.Initializer)))

            End Function

            Public Overrides Function VisitEnumStatement(node As VB.Syntax.EnumStatementSyntax) As SyntaxNode

                Dim enumBlock As VB.Syntax.EnumBlockSyntax = node.Parent

                Dim base As CS.Syntax.BaseListSyntax = Nothing
                If node.UnderlyingType IsNot Nothing Then
                    base = BaseList(SingletonSeparatedList(Of BaseTypeSyntax)(SimpleBaseType(CType(VisitSimpleAsClause(node.UnderlyingType), CS.Syntax.TypeSyntax))))
                End If

                Return TransferTrivia(enumBlock, EnumDeclaration(VisitIdentifier(node.Identifier)) _
                                                     .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                     .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                                                     .WithBaseList(base) _
                                                     .WithMembers(SeparatedList(Visit(enumBlock.Members).Cast(Of CS.Syntax.EnumMemberDeclarationSyntax)))
                       )

            End Function

            Public Overrides Function VisitEqualsValue(node As VB.Syntax.EqualsValueSyntax) As SyntaxNode

                If node Is Nothing Then Return Nothing

                Return EqualsValueClause(Visit(node.Value))

            End Function

            Public Overrides Function VisitEraseStatement(node As VB.Syntax.EraseStatementSyntax) As SyntaxNode

                Return NotImplementedStatement(node)

                ' TODO: Implement rewrite to call Array.Clear.
                Throw New NotImplementedException(node.ToString())

            End Function

            Public Overrides Function VisitErrorStatement(node As VB.Syntax.ErrorStatementSyntax) As SyntaxNode

                Return NotImplementedStatement(node)
                Throw New NotSupportedException(node.Kind.ToString())

            End Function

            Public Overrides Function VisitEventBlock(node As VB.Syntax.EventBlockSyntax) As SyntaxNode

                Return VisitEventStatement(node.EventStatement)

            End Function

            Public Overrides Function VisitEventStatement(node As VB.Syntax.EventStatementSyntax) As SyntaxNode

                Dim eventBlock = TryCast(node.Parent, VB.Syntax.EventBlockSyntax)

                Dim accessors = If(eventBlock Is Nothing,
                                   Nothing,
                                   eventBlock.Accessors
                                )

                If node.AsClause IsNot Nothing Then
                    If accessors.Count = 0 Then
                        ' TODO: Synthesize an explicit interface implementation if this event's name differs from the name of the method in its Implements clause.
                        Return TransferTrivia(node, EventFieldDeclaration(
                                                        VariableDeclaration(
                                                            VisitSimpleAsClause(node.AsClause),
                                                            SingletonSeparatedList(VariableDeclarator(VisitIdentifier(node.Identifier)))
                                                        )).WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                          .WithModifiers(TokenList(VisitModifiers(node.Modifiers)))
                                                    )

                    Else
                        Return NotImplementedMember(node)

                        Return TransferTrivia(eventBlock, EventDeclaration(
                                                              VisitSimpleAsClause(node.AsClause),
                                                              VisitIdentifier(node.Identifier)) _
                                                            .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                            .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                                                            .WithAccessorList(AccessorList(List(Visit(eventBlock.Accessors))))
                                                        )
                    End If
                Else
                    Return NotImplementedMember(node)

                    ' TODO: Implement rewrite to add implicit delegate declaration.
                    Throw New NotSupportedException("Events with inline parameter lists.")
                End If

            End Function

            Public Overrides Function VisitExitStatement(node As VB.Syntax.ExitStatementSyntax) As SyntaxNode

                Select Case node.Kind
                    Case VB.SyntaxKind.ExitSubStatement,
                         VB.SyntaxKind.ExitOperatorStatement

                        Return ReturnStatement()

                    Case VB.SyntaxKind.ExitTryStatement

                        Return NotImplementedStatement(node)
                        ' TODO: Implement a rewrite of this to a goto statement.
                        Throw New NotSupportedException(node.Kind.ToString())

                    Case VB.SyntaxKind.ExitSelectStatement

                        ' TODO: Implement rewrite to goto statement here if there are intermediate loops between this statement and the Select block.
                        Return BreakStatement()

                    Case VB.SyntaxKind.ExitPropertyStatement

                        Dim parent = node.Parent
                        Do Until TypeOf parent Is VB.Syntax.MethodBaseSyntax
                            parent = parent.Parent
                        Loop

                        If parent.IsKind(VB.SyntaxKind.SetAccessorBlock) Then
                            Return ReturnStatement()
                        Else
                            Return NotImplementedStatement(node)
                            ' TODO: Implement rewrite of Exit Property statements to return the implicit return variable.
                            Throw New NotSupportedException("Exit Property in a Property Get block.")
                        End If

                    Case VB.SyntaxKind.ExitFunctionStatement
                        Return NotImplementedStatement(node)

                        ' TODO: Implement rewrite of Exit Function statements to return the implicit return variable.
                        Throw New NotSupportedException("Exit Function statements.")

                    Case VB.SyntaxKind.ExitDoStatement,
                         VB.SyntaxKind.ExitForStatement,
                         VB.SyntaxKind.ExitWhileStatement

                        ' So long as this exit statement binds to its immediately enclosing block this is simple.
                        ' Otherwise it would require rewriting with goto statements.
                        ' TODO: Consider implementing this using binding instead.
                        Dim parent = node.Parent
                        Do Until VB.SyntaxFacts.IsDoLoopBlock(parent.Kind) OrElse
                                 (parent.IsKind(VB.SyntaxKind.ForBlock) OrElse parent.IsKind(VB.SyntaxKind.ForEachBlock)) OrElse
                                 parent.IsKind(VB.SyntaxKind.WhileBlock)

                            parent = parent.Parent
                        Loop

                        If (node.IsKind(VB.SyntaxKind.ExitDoStatement) AndAlso VB.SyntaxFacts.IsDoLoopBlock(parent.Kind)) OrElse
                           (node.IsKind(VB.SyntaxKind.ExitForStatement) AndAlso (parent.IsKind(VB.SyntaxKind.ForBlock) OrElse parent.IsKind(VB.SyntaxKind.ForEachBlock))) OrElse
                           (node.IsKind(VB.SyntaxKind.ExitWhileStatement) AndAlso parent.IsKind(VB.SyntaxKind.WhileBlock)) Then

                            Return ContinueStatement()
                        Else

                            Return NotImplementedStatement(node)

                            Throw New NotImplementedException("Rewriting Exit statements which branch out of their immediately containing loop block into gotos.")
                        End If

                    Case Else
                        Throw New NotSupportedException(node.Kind.ToString())
                End Select

            End Function

            Public Overrides Function VisitExternalChecksumDirectiveTrivia(node As VB.Syntax.ExternalChecksumDirectiveTriviaSyntax) As SyntaxNode

                Throw New NotSupportedException(node.Kind.ToString())

            End Function

            Public Overrides Function VisitExternalSourceDirectiveTrivia(node As VB.Syntax.ExternalSourceDirectiveTriviaSyntax) As SyntaxNode

                Return LineDirectiveTrivia(Literal(node.LineStart.ToString(), CInt(node.LineStart.Value)), isActive:=True) _
                            .WithFile(Literal(node.ExternalSource.ToString(), node.ExternalSource.ValueText))

            End Function

            Public Overrides Function VisitFieldDeclaration(node As VB.Syntax.FieldDeclarationSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitFinallyBlock(node As VB.Syntax.FinallyBlockSyntax) As SyntaxNode

                If node Is Nothing Then Return Nothing

                Return FinallyClause(Block(List(Visit(node.Statements))))

            End Function

            Public Overrides Function VisitFinallyStatement(node As VB.Syntax.FinallyStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitForBlock(node As VB.Syntax.ForBlockSyntax) As SyntaxNode

                Return Visit(node.ForStatement)

            End Function

            Public Overrides Function VisitForEachBlock(node As VB.Syntax.ForEachBlockSyntax) As SyntaxNode

                Return Visit(node.ForEachStatement)

            End Function

            Public Overrides Function VisitForEachStatement(node As VB.Syntax.ForEachStatementSyntax) As SyntaxNode

                Dim forBlock As VB.Syntax.ForEachBlockSyntax = node.Parent

                Dim type As CS.Syntax.TypeSyntax
                Dim identifier As SyntaxToken

                Select Case node.ControlVariable.Kind
                    Case VB.SyntaxKind.IdentifierName

                        type = IdentifierName("var")
                        identifier = VisitIdentifier(CType(node.ControlVariable, VB.Syntax.IdentifierNameSyntax).Identifier)

                    Case VB.SyntaxKind.VariableDeclarator

                        Dim declarator As VB.Syntax.VariableDeclaratorSyntax = node.ControlVariable

                        type = DeriveType(declarator.Names(0), declarator.AsClause, declarator.Initializer)
                        identifier = VisitIdentifier(declarator.Names(0).Identifier)

                    Case Else

                        Return NotImplementedStatement(node)

                        Throw New NotSupportedException(node.ControlVariable.Kind.ToString())
                End Select

                Return TransferTrivia(forBlock, ForEachStatement(
                                                    type,
                                                    identifier,
                                                    Visit(node.Expression),
                                                    Block(List(Visit(forBlock.Statements)))
                                                )
                       )

            End Function

            Public Overrides Function VisitForStatement(node As VB.Syntax.ForStatementSyntax) As SyntaxNode

                Dim forBlock As VB.Syntax.ForBlockSyntax = node.Parent

                Dim type As CS.Syntax.TypeSyntax
                Dim identifier As SyntaxToken

                Select Case node.ControlVariable.Kind
                    Case VB.SyntaxKind.IdentifierName

                        ' TODO: Bind to make sure this name isn't referencing an existing variable.
                        '       If it is then we shouldn't create a var declarator but instead an
                        '       initialization expression.
                        type = IdentifierName("var")
                        identifier = VisitIdentifier(CType(node.ControlVariable, VB.Syntax.IdentifierNameSyntax).Identifier)

                    Case VB.SyntaxKind.VariableDeclarator

                        Dim declarator As VB.Syntax.VariableDeclaratorSyntax = node.ControlVariable

                        type = DeriveType(declarator.Names(0), declarator.AsClause, declarator.Initializer)
                        identifier = VisitIdentifier(declarator.Names(0).Identifier)

                    Case Else

                        Return NotImplementedStatement(node)

                        Throw New NotSupportedException(node.ControlVariable.Kind.ToString())
                End Select

                Dim toValue = node.ToValue
                If toValue.IsKind(VB.SyntaxKind.ParenthesizedExpression) Then
                    toValue = CType(toValue, VB.Syntax.ParenthesizedExpressionSyntax).Expression
                End If

                Dim declarationOpt = VariableDeclaration(type, SingletonSeparatedList(VariableDeclarator(identifier).WithInitializer(EqualsValueClause(Visit(node.FromValue)))))

                Dim conditionOpt As CS.Syntax.ExpressionSyntax = BinaryExpression(CS.SyntaxKind.LessThanOrEqualExpression, IdentifierName(identifier), Visit(toValue))

                Dim incrementor As CS.Syntax.ExpressionSyntax = PostfixUnaryExpression(CS.SyntaxKind.PostIncrementExpression, IdentifierName(identifier))

                ' Rewrite ... To Count - 1 to < Count.
                If node.StepClause Is Nothing Then
                    If toValue.IsKind(VB.SyntaxKind.SubtractExpression) Then
                        Dim subtract As VB.Syntax.BinaryExpressionSyntax = toValue

                        If subtract.Right.IsKind(VB.SyntaxKind.NumericLiteralExpression) AndAlso
                           CInt(CType(subtract.Right, VB.Syntax.LiteralExpressionSyntax).Token.Value) = 1 Then

                            conditionOpt = BinaryExpression(CS.SyntaxKind.LessThanExpression, IdentifierName(identifier), Visit(subtract.Left))

                        End If
                    End If
                Else

                    Dim stepValue = node.StepClause.StepValue
                    If stepValue.IsKind(VB.SyntaxKind.ParenthesizedExpression) Then
                        stepValue = CType(stepValue, VB.Syntax.ParenthesizedExpressionSyntax).Expression
                    End If

                    incrementor = AssignmentExpression(CS.SyntaxKind.AddAssignmentExpression, IdentifierName(identifier), Visit(stepValue))

                    If stepValue.IsKind(VB.SyntaxKind.UnaryMinusExpression) Then
                        Dim negate As VB.Syntax.UnaryExpressionSyntax = stepValue

                        conditionOpt = BinaryExpression(CS.SyntaxKind.GreaterThanOrEqualExpression, IdentifierName(identifier), Visit(toValue))

                        If negate.Operand.IsKind(VB.SyntaxKind.NumericLiteralExpression) AndAlso
                           CInt(CType(negate.Operand, VB.Syntax.LiteralExpressionSyntax).Token.Value) = 1 Then

                            incrementor = PostfixUnaryExpression(CS.SyntaxKind.PostDecrementExpression, IdentifierName(identifier))
                        Else
                            incrementor = AssignmentExpression(CS.SyntaxKind.SubtractAssignmentExpression, IdentifierName(identifier), Visit(negate.Operand))
                        End If
                    End If
                End If

                Return TransferTrivia(forBlock, ForStatement(Block(List(Visit(forBlock.Statements)))).WithDeclaration(declarationOpt).WithCondition(conditionOpt).WithIncrementors(SingletonSeparatedList(incrementor)))

            End Function

            Public Overrides Function VisitForStepClause(node As VB.Syntax.ForStepClauseSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitGenericName(node As VB.Syntax.GenericNameSyntax) As SyntaxNode

                Return GenericName(VisitIdentifier(node.Identifier), VisitTypeArgumentList(node.TypeArgumentList))

            End Function

            Public Overrides Function VisitGetTypeExpression(node As VB.Syntax.GetTypeExpressionSyntax) As SyntaxNode

                Return TypeOfExpression(Visit(node.Type))

            End Function

            Public Overrides Function VisitGetXmlNamespaceExpression(node As VB.Syntax.GetXmlNamespaceExpressionSyntax) As SyntaxNode
                Return NotImplementedExpression(node)
            End Function

            Public Overrides Function VisitGlobalName(node As VB.Syntax.GlobalNameSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitGoToStatement(node As VB.Syntax.GoToStatementSyntax) As SyntaxNode

                If node.Label.IsKind(VB.SyntaxKind.IdentifierLabel) Then
                    Return TransferTrivia(node, GotoStatement(CS.SyntaxKind.GotoStatement, IdentifierName(VisitIdentifier(node.Label.LabelToken))))
                Else
                    Return NotImplementedStatement(node)
                    ' Rewrite this label with an alpha prefix.
                    Throw New NotSupportedException("Goto statements with numeric label names.")
                End If

            End Function

            Public Overrides Function VisitGroupAggregation(node As VB.Syntax.GroupAggregationSyntax) As SyntaxNode
                Return MyBase.VisitGroupAggregation(node)
            End Function

            Public Overrides Function VisitGroupByClause(node As VB.Syntax.GroupByClauseSyntax) As SyntaxNode
                Return MyBase.VisitGroupByClause(node)
            End Function

            Public Overrides Function VisitGroupJoinClause(node As VB.Syntax.GroupJoinClauseSyntax) As SyntaxNode
                Return MyBase.VisitGroupJoinClause(node)
            End Function

            Public Overrides Function VisitHandlesClause(node As VB.Syntax.HandlesClauseSyntax) As SyntaxNode
                Return MyBase.VisitHandlesClause(node)
            End Function

            Public Overrides Function VisitHandlesClauseItem(node As VB.Syntax.HandlesClauseItemSyntax) As SyntaxNode
                Return MyBase.VisitHandlesClauseItem(node)
            End Function

            Public Overrides Function VisitIdentifierName(node As VB.Syntax.IdentifierNameSyntax) As SyntaxNode

                Return IdentifierName(VisitIdentifier(node.Identifier))

            End Function

            Protected Function VisitIdentifier(token As SyntaxToken) As SyntaxToken

                If token.IsKind(VB.SyntaxKind.None) Then Return Nothing

                Dim text = token.ValueText

                ' Strip out type characters.
                If Not Char.IsLetterOrDigit(text(text.Length - 1)) OrElse text.EndsWith("_") Then
                    text = text.Substring(0, text.Length - 1)
                End If

                If text = "_" Then
                    Return Identifier("_" & text)
                Else
                    Return Identifier(text)
                End If

            End Function

            Public Overrides Function VisitIfDirectiveTrivia(node As VB.Syntax.IfDirectiveTriviaSyntax) As SyntaxNode

                Return IfDirectiveTrivia(Visit(node.Condition), isActive:=False, branchTaken:=False, conditionValue:=False)

            End Function

            Public Overrides Function VisitIfStatement(node As VB.Syntax.IfStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitImplementsClause(node As VB.Syntax.ImplementsClauseSyntax) As SyntaxNode
                Return MyBase.VisitImplementsClause(node)
            End Function

            Public Overrides Function VisitImplementsStatement(node As VB.Syntax.ImplementsStatementSyntax) As SyntaxNode

                If node.Types.Count = 1 Then
                    Return Visit(node.Types(0))
                Else
                    Throw New InvalidOperationException()
                End If

            End Function

            Protected Function VisitImplementsStatements(statements As IEnumerable(Of VB.Syntax.ImplementsStatementSyntax)) As IEnumerable(Of SyntaxNode)

                Return Visit((Aggregate statement In statements Into SelectMany(statement.Types)))

            End Function

            Public Overrides Function VisitImportsStatement(node As VB.Syntax.ImportsStatementSyntax) As SyntaxNode

                If node.ImportsClauses.Count > 1 Then
                    Throw New InvalidOperationException()
                End If

                Return Visit(node.ImportsClauses(0))

            End Function

            Protected Function VisitImportsStatements(statements As IEnumerable(Of VB.Syntax.ImportsStatementSyntax)) As IEnumerable(Of SyntaxNode)

                Return Visit((Aggregate statement In statements Into SelectMany(statement.ImportsClauses)))

            End Function

            Public Overrides Function VisitInferredFieldInitializer(node As VB.Syntax.InferredFieldInitializerSyntax) As SyntaxNode

                Return Visit(node.Expression)

            End Function

            Public Overrides Function VisitInheritsStatement(node As VB.Syntax.InheritsStatementSyntax) As SyntaxNode

                If node.Types.Count = 1 Then
                    Return Visit(node.Types(0))
                Else
                    Throw New InvalidOperationException()
                End If

            End Function

            Protected Function VisitInheritsStatements(statements As IEnumerable(Of VB.Syntax.InheritsStatementSyntax)) As IEnumerable(Of SyntaxNode)

                Return Visit((Aggregate statement In statements Into SelectMany(statement.Types)))

            End Function

            Protected Overridable Function VisitInstanceExpression(node As VB.Syntax.InstanceExpressionSyntax) As SyntaxNode

                Select Case node.Kind
                    Case VB.SyntaxKind.MeExpression
                        Return ThisExpression()
                    Case VB.SyntaxKind.MyBaseExpression
                        Return BaseExpression()
                    Case VB.SyntaxKind.MyClassExpression
                        Return NotImplementedExpression(node)
                        Throw New NotSupportedException("C# doesn't have a MyClass equivalent")
                    Case Else
                        Throw New NotSupportedException(node.Kind.ToString())
                End Select

            End Function

            Public Overrides Function VisitMeExpression(node As VB.Syntax.MeExpressionSyntax) As SyntaxNode
                Return VisitInstanceExpression(node)
            End Function

            Public Overrides Function VisitMyBaseExpression(node As VB.Syntax.MyBaseExpressionSyntax) As SyntaxNode
                Return VisitInstanceExpression(node)
            End Function

            Public Overrides Function VisitMyClassExpression(node As VB.Syntax.MyClassExpressionSyntax) As SyntaxNode
                Return VisitInstanceExpression(node)
            End Function

            Public Overrides Function VisitInvocationExpression(node As VB.Syntax.InvocationExpressionSyntax) As SyntaxNode

                ' TODO: Use binding to detect whether this is an invocation or an index, 
                '       and if an index whether off a property or the result of an implicit method invocation.
                Return InvocationExpression(Visit(node.Expression), VisitArgumentList(node.ArgumentList))

            End Function

            Public Overrides Function VisitJoinCondition(node As VB.Syntax.JoinConditionSyntax) As SyntaxNode
                Return MyBase.VisitJoinCondition(node)
            End Function

            Public Overrides Function VisitSimpleJoinClause(node As VB.Syntax.SimpleJoinClauseSyntax) As SyntaxNode
                Return MyBase.VisitSimpleJoinClause(node)
            End Function

            Public Overrides Function VisitLabelStatement(node As VB.Syntax.LabelStatementSyntax) As SyntaxNode

                Return LabeledStatement(VisitIdentifier(node.LabelToken), EmptyStatement())

            End Function

            Public Overrides Function VisitLambdaHeader(node As VB.Syntax.LambdaHeaderSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitLiteralExpression(node As VB.Syntax.LiteralExpressionSyntax) As SyntaxNode

                Select Case node.Kind
                    Case VB.SyntaxKind.StringLiteralExpression

                        ' VB String literals are effectively implicitly escaped (a.k.a verbatim).
                        Dim valueText = If(node.Token.ValueText.Contains("\"),
                                           "@" & """" & node.Token.ValueText & """",
                                           """" & node.Token.ValueText & """"
                                        )

                        Return LiteralExpression(CS.SyntaxKind.StringLiteralExpression, Literal(valueText, CStr(node.Token.Value)))

                    Case VB.SyntaxKind.CharacterLiteralExpression

                        Return LiteralExpression(CS.SyntaxKind.StringLiteralExpression, Literal("'" & node.Token.ValueText & "'", CChar(node.Token.Value)))

                    Case VB.SyntaxKind.TrueLiteralExpression

                        Return LiteralExpression(CS.SyntaxKind.TrueLiteralExpression)

                    Case VB.SyntaxKind.FalseLiteralExpression

                        Return LiteralExpression(CS.SyntaxKind.FalseLiteralExpression)

                    Case VB.SyntaxKind.DateLiteralExpression

                        Return NotImplementedExpression(node)

                        ' TODO: Rewrite to new global::System.DateTime.Parse("yyyy-MM-dd HH:mm:ss")
                        Throw New NotImplementedException(node.ToString())

                    Case VB.SyntaxKind.NumericLiteralExpression

                        Select Case node.Token.Kind

                            Case VB.SyntaxKind.DecimalLiteralToken

                                Return LiteralExpression(CS.SyntaxKind.NumericLiteralExpression, Literal(node.Token.ValueText, CDec(node.Token.Value)))

                            Case VB.SyntaxKind.FloatingLiteralToken

                                Return LiteralExpression(CS.SyntaxKind.NumericLiteralExpression, Literal(node.Token.ValueText, CDbl(node.Token.Value)))

                            Case VB.SyntaxKind.IntegerLiteralToken

                                Dim literalText As String = Nothing

                                Select Case node.Token.GetBase()
                                    Case VB.Syntax.LiteralBase.Decimal
                                        literalText = node.Token.ValueText

                                    Case VB.Syntax.LiteralBase.Hexadecimal,
                                         VB.Syntax.LiteralBase.Octal

                                        literalText = "0x" & CType(node.Token.Value, IFormattable).ToString("X02", formatProvider:=Nothing)

                                End Select

                                Dim literalToken As SyntaxToken

                                Select Case node.Token.GetTypeCharacter()
                                    Case VB.Syntax.TypeCharacter.ShortLiteral

                                        literalToken = Literal(literalText, CShort(node.Token.Value))

                                    Case VB.Syntax.TypeCharacter.IntegerLiteral

                                        literalToken = Literal(literalText, CInt(node.Token.Value))

                                    Case VB.Syntax.TypeCharacter.LongLiteral

                                        literalToken = Literal(literalText, CLng(node.Token.Value))

                                    Case VB.Syntax.TypeCharacter.UShortLiteral

                                        literalToken = Literal(literalText, CUShort(node.Token.Value))

                                    Case VB.Syntax.TypeCharacter.UIntegerLiteral

                                        literalToken = Literal(literalText, CUInt(node.Token.Value))

                                    Case VB.Syntax.TypeCharacter.ULongLiteral

                                        literalToken = Literal(literalText, CULng(node.Token.Value))

                                    Case Else ' Default to Integer type

                                        literalToken = Literal(literalText, CInt(node.Token.Value))

                                End Select

                                Return LiteralExpression(CS.SyntaxKind.NumericLiteralExpression, literalToken)

                            Case Else
                                Return NotImplementedExpression(node)

                                Throw New NotSupportedException(node.Token.Kind.ToString())
                        End Select

                    Case VB.SyntaxKind.NothingLiteralExpression
                        ' TODO: Bind this expression in context to determine whether this translates to null or default(T).
                        Return LiteralExpression(CS.SyntaxKind.NullLiteralExpression)

                    Case Else
                        Return NotImplementedExpression(node)

                        Throw New NotSupportedException(node.Kind.ToString())
                End Select

            End Function

            Public Overrides Function VisitLocalDeclarationStatement(node As VB.Syntax.LocalDeclarationStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitLoopStatement(node As VB.Syntax.LoopStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitMemberAccessExpression(node As VB.Syntax.MemberAccessExpressionSyntax) As SyntaxNode

                If node.Expression Is Nothing Then

                    Return NotImplementedExpression(node)
                    ' TODO: Rewrite WithBlock member access.
                    Throw New NotImplementedException(node.ToString())
                End If

                Select Case node.Kind

                    Case VB.SyntaxKind.SimpleMemberAccessExpression

                        Return MemberAccessExpression(CS.SyntaxKind.SimpleMemberAccessExpression, Visit(node.Expression), Visit(node.Name))

                    Case VB.SyntaxKind.DictionaryAccessExpression

                        Return NotImplementedExpression(node)
                        ' TODO: Rewrite to Invocation.
                        Throw New NotImplementedException(node.ToString())

                    Case Else
                        Throw New NotSupportedException(node.Kind.ToString())
                End Select

            End Function

            Public Overrides Function VisitSimpleImportsClause(node As VB.Syntax.SimpleImportsClauseSyntax) As SyntaxNode

                If node.Alias Is Nothing Then
                    Return TransferTrivia(node.Parent, UsingDirective(Visit(node.Name)))
                Else
                    Return TransferTrivia(node.Parent, UsingDirective(Visit(node.Name)).WithAlias(NameEquals(CS.SyntaxFactory.IdentifierName(VisitIdentifier(node.Alias.Identifier)))))
                End If

            End Function

            Protected Function VisitMembers(statements As IEnumerable(Of VB.Syntax.StatementSyntax)) As IEnumerable(Of CS.Syntax.MemberDeclarationSyntax)
                Dim members As New List(Of CS.Syntax.MemberDeclarationSyntax)

                For Each statement In statements
                    Dim converted = Visit(statement)

                    If TypeOf converted Is CS.Syntax.MemberDeclarationSyntax Then
                        members.Add(converted)
                    ElseIf TypeOf converted Is CS.Syntax.StatementSyntax Then
                        members.Add(GlobalStatement(converted))
                    Else
                        Throw New NotSupportedException(converted.Kind.ToString())
                    End If
                Next

                Return members
            End Function

            Public Overrides Function VisitMethodBlock(node As VB.Syntax.MethodBlockSyntax) As SyntaxNode
                Return Visit(node.SubOrFunctionStatement)
            End Function

            Public Overrides Function VisitConstructorBlock(node As ConstructorBlockSyntax) As SyntaxNode
                Return Visit(node.SubNewStatement)
            End Function

            Public Overrides Function VisitOperatorBlock(node As OperatorBlockSyntax) As SyntaxNode
                Return Visit(node.OperatorStatement)
            End Function

            Public Overrides Function VisitAccessorBlock(node As AccessorBlockSyntax) As SyntaxNode
                Return Visit(node.AccessorStatement)
            End Function

            Public Overrides Function VisitMethodStatement(node As VB.Syntax.MethodStatementSyntax) As SyntaxNode

                ' A MustInherit method, or a method inside an Interface definition will be directly parented by the TypeBlock.
                Dim methodBlock = TryCast(node.Parent, VB.Syntax.MethodBlockSyntax)

                Dim triviaSource As SyntaxNode = node
                If methodBlock IsNot Nothing Then
                    triviaSource = methodBlock
                End If

                Return TransferTrivia(triviaSource, MethodDeclaration(
                                                        DeriveType(node.Identifier, node.AsClause, node.SubOrFunctionKeyword),
                                                        VisitIdentifier(node.Identifier)) _
                                                    .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                    .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                                                    .WithTypeParameterList(VisitTypeParameterList(node.TypeParameterList)) _
                                                    .WithParameterList(VisitParameterList(node.ParameterList)) _
                                                    .WithConstraintClauses(List(VisitTypeParameterConstraintClauses(node.TypeParameterList))) _
                                                    .WithBody(If(methodBlock Is Nothing, Nothing, Block(List(Visit(methodBlock.Statements))))) _
                                                    .WithSemicolonToken(If(methodBlock Is Nothing, Token(CS.SyntaxKind.SemicolonToken), Nothing))
                                        )

            End Function

            Protected Function VisitModifier(token As SyntaxToken) As SyntaxToken

                Dim kind As CS.SyntaxKind

                Select Case token.Kind
                    Case VB.SyntaxKind.PublicKeyword
                        kind = CS.SyntaxKind.PublicKeyword
                    Case VB.SyntaxKind.PrivateKeyword
                        kind = CS.SyntaxKind.PrivateKeyword
                    Case VB.SyntaxKind.ProtectedKeyword
                        kind = CS.SyntaxKind.ProtectedKeyword
                    Case VB.SyntaxKind.FriendKeyword
                        kind = CS.SyntaxKind.InternalKeyword
                    Case VB.SyntaxKind.SharedKeyword
                        kind = CS.SyntaxKind.StaticKeyword
                    Case VB.SyntaxKind.OverridesKeyword
                        kind = CS.SyntaxKind.OverrideKeyword
                    Case VB.SyntaxKind.OverridableKeyword
                        kind = CS.SyntaxKind.VirtualKeyword
                    Case VB.SyntaxKind.MustOverrideKeyword
                        kind = CS.SyntaxKind.AbstractKeyword
                    Case VB.SyntaxKind.NotOverridableKeyword
                        kind = CS.SyntaxKind.SealedKeyword
                    Case VB.SyntaxKind.OverloadsKeyword
                        kind = CS.SyntaxKind.NewKeyword
                    Case VB.SyntaxKind.MustInheritKeyword
                        kind = CS.SyntaxKind.AbstractKeyword
                    Case VB.SyntaxKind.NotInheritableKeyword
                        kind = CS.SyntaxKind.SealedKeyword
                    Case VB.SyntaxKind.PartialKeyword
                        kind = CS.SyntaxKind.PartialKeyword
                    Case VB.SyntaxKind.ByRefKeyword
                        kind = CS.SyntaxKind.RefKeyword
                    Case VB.SyntaxKind.ParamArrayKeyword
                        kind = CS.SyntaxKind.ParamsKeyword
                    Case VB.SyntaxKind.NarrowingKeyword
                        kind = CS.SyntaxKind.ExplicitKeyword
                    Case VB.SyntaxKind.WideningKeyword
                        kind = CS.SyntaxKind.ImplicitKeyword
                    Case VB.SyntaxKind.ConstKeyword
                        kind = CS.SyntaxKind.ConstKeyword
                    Case VB.SyntaxKind.ReadOnlyKeyword

                        If TypeOf token.Parent Is VB.Syntax.PropertyStatementSyntax Then
                            kind = CS.SyntaxKind.None
                        Else
                            kind = CS.SyntaxKind.ReadOnlyKeyword
                        End If

                    Case VB.SyntaxKind.DimKeyword
                        kind = CS.SyntaxKind.None

                    Case VB.SyntaxKind.AsyncKeyword
                        kind = CS.SyntaxKind.AsyncKeyword
                    Case Else
                        Return NotImplementedModifier(token)

                        Throw New NotSupportedException(token.Kind.ToString())
                End Select

                Return CS.SyntaxFactory.Token(kind)

            End Function

            Protected Function VisitModifiers(tokens As IEnumerable(Of SyntaxToken)) As IEnumerable(Of SyntaxToken)

                Return From
                           token In tokens
                       Where
                           Not token.IsKind(VB.SyntaxKind.ByValKeyword) AndAlso
                           Not token.IsKind(VB.SyntaxKind.OptionalKeyword)
                       Select
                           translation = VisitModifier(token)
                       Where
                           Not translation.IsKind(CS.SyntaxKind.None)

            End Function

            Public Overrides Function VisitModifiedIdentifier(node As VB.Syntax.ModifiedIdentifierSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitMultiLineIfBlock(node As VB.Syntax.MultiLineIfBlockSyntax) As SyntaxNode

                Dim elseOpt As CS.Syntax.ElseClauseSyntax = Nothing

                ' TODO: Transfer trivia for each elseif/else block.
                If node.ElseBlock IsNot Nothing Then
                    elseOpt = ElseClause(Block(List(Visit(node.ElseBlock.Statements))))
                End If

                For i = node.ElseIfBlocks.Count - 1 To 0 Step -1
                    elseOpt = ElseClause(
                                  IfStatement(
                                      Visit(node.ElseIfBlocks(i).ElseIfStatement.Condition),
                                      Block(List(Visit(node.ElseIfBlocks(i).Statements)))) _
                                    .WithElse(elseOpt)
                                  )
                Next

                Return TransferTrivia(node, IfStatement(
                                                Visit(node.IfStatement.Condition),
                                                Block(List(Visit(node.Statements)))) _
                                                .WithElse(elseOpt)
                                            )

            End Function

            Public Overrides Function VisitMultiLineLambdaExpression(node As VB.Syntax.MultiLineLambdaExpressionSyntax) As SyntaxNode

                Dim asyncKeyword = If(node.SubOrFunctionHeader.Modifiers.Any(VB.SyntaxKind.AsyncKeyword),
                      Token(CS.SyntaxKind.AsyncKeyword),
                      Nothing)

                Dim parameterList = VisitParameterList(node.SubOrFunctionHeader.ParameterList)

                Dim arrowToken = Token(CS.SyntaxKind.EqualsGreaterThanToken)

                Dim body = Block(List(Visit(node.Statements)))

                Return ParenthesizedLambdaExpression(asyncKeyword, parameterList, arrowToken, body)

            End Function

            Public Overrides Function VisitNamedFieldInitializer(node As VB.Syntax.NamedFieldInitializerSyntax) As SyntaxNode

                Return If(node.Parent.Parent.IsKind(VB.SyntaxKind.AnonymousObjectCreationExpression),
                          CType(AnonymousObjectMemberDeclarator(NameEquals(VisitIdentifierName(node.Name)), Visit(node.Expression)), SyntaxNode),
                          AssignmentExpression(CS.SyntaxKind.SimpleAssignmentExpression, VisitIdentifierName(node.Name), Visit(node.Expression)))

            End Function

            Public Overrides Function VisitNamespaceBlock(node As VB.Syntax.NamespaceBlockSyntax) As SyntaxNode

                Return VisitNamespaceStatement(node.NamespaceStatement)

            End Function

            Public Overrides Function VisitNamespaceStatement(node As VB.Syntax.NamespaceStatementSyntax) As SyntaxNode

                Dim namespaceBlock As VB.Syntax.NamespaceBlockSyntax = node.Parent

                If node.Name.IsKind(VB.SyntaxKind.GlobalName) Then

                    ' TODO: Split all members to declare in global namespace.
                    Throw New NotImplementedException(node.ToString())

                Else
                    Dim baseName = node.Name
                    Do While TypeOf baseName Is VB.Syntax.QualifiedNameSyntax
                        baseName = CType(baseName, VB.Syntax.QualifiedNameSyntax).Left
                    Loop

                    Dim remainingNames = TryCast(baseName.Parent, VB.Syntax.QualifiedNameSyntax)

                    Dim finalName As CS.Syntax.NameSyntax

                    ' Strip out the Global name.
                    If baseName.IsKind(VB.SyntaxKind.GlobalName) Then
                        finalName = Visit(remainingNames.Right)
                        remainingNames = TryCast(remainingNames.Parent, VB.Syntax.QualifiedNameSyntax)
                    ElseIf RootNamespaceName IsNot Nothing Then
                        finalName = QualifiedName(RootNamespaceName, Visit(baseName))
                    Else
                        finalName = Visit(baseName)
                    End If

                    Do Until remainingNames Is Nothing
                        finalName = QualifiedName(finalName, Visit(remainingNames.Right))
                        remainingNames = TryCast(remainingNames.Parent, VB.Syntax.QualifiedNameSyntax)
                    Loop

                    Return TransferTrivia(node, NamespaceDeclaration(finalName).WithMembers(List(Visit(namespaceBlock.Members))))

                End If

            End Function

            Public Overrides Function VisitNextStatement(node As VB.Syntax.NextStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitNullableType(node As VB.Syntax.NullableTypeSyntax) As SyntaxNode

                Return NullableType(Visit(node.ElementType))

            End Function

            Public Overrides Function VisitObjectCollectionInitializer(node As VB.Syntax.ObjectCollectionInitializerSyntax) As SyntaxNode

                ' TODO: Figure out what to do if the initializers contain nested initializers that invoke extension methods.
                Return VisitCollectionInitializer(node.Initializer)

            End Function

            Public Overrides Function VisitObjectCreationExpression(node As VB.Syntax.ObjectCreationExpressionSyntax) As SyntaxNode

                Return ObjectCreationExpression(Visit(node.Type)) _
                            .WithArgumentList(VisitArgumentList(node.ArgumentList)) _
                            .WithInitializer(Visit(node.Initializer))

            End Function

            Public Overrides Function VisitObjectMemberInitializer(node As VB.Syntax.ObjectMemberInitializerSyntax) As SyntaxNode

                Return InitializerExpression(CS.SyntaxKind.ObjectInitializerExpression, SeparatedList(Visit(node.Initializers).Cast(Of CS.Syntax.ExpressionSyntax)))

            End Function

            Public Overrides Function VisitOmittedArgument(node As VB.Syntax.OmittedArgumentSyntax) As SyntaxNode

                Return CS.SyntaxFactory.Argument(NotImplementedExpression(node))

                ' TODO: Bind to discover default values.
                Throw New NotImplementedException(node.ToString())
            End Function

            Public Overrides Function VisitOnErrorGoToStatement(node As VB.Syntax.OnErrorGoToStatementSyntax) As SyntaxNode

                Return NotImplementedStatement(node)

            End Function

            Public Overrides Function VisitOnErrorResumeNextStatement(node As VB.Syntax.OnErrorResumeNextStatementSyntax) As SyntaxNode

                Return NotImplementedStatement(node)

            End Function

            Public Overrides Function VisitOperatorStatement(node As VB.Syntax.OperatorStatementSyntax) As SyntaxNode

                Dim operatorBlock As VB.Syntax.OperatorBlockSyntax = node.Parent

                Dim kind As CS.SyntaxKind
                Select Case node.OperatorToken.Kind

                    Case VB.SyntaxKind.CTypeKeyword

                        Dim otherModifiers As New List(Of SyntaxToken)(node.Modifiers.Count)
                        Dim implicitOrExplicitKeyword As SyntaxToken

                        For Each modifier In node.Modifiers
                            Select Case modifier.Kind
                                Case VB.SyntaxKind.NarrowingKeyword
                                    implicitOrExplicitKeyword = Token(CS.SyntaxKind.ExplicitKeyword)
                                Case VB.SyntaxKind.WideningKeyword
                                    implicitOrExplicitKeyword = Token(CS.SyntaxKind.ImplicitKeyword)
                                Case Else
                                    otherModifiers.Add(modifier)
                            End Select
                        Next

                        Return TransferTrivia(operatorBlock, ConversionOperatorDeclaration(
                                                                implicitOrExplicitKeyword,
                                                                VisitSimpleAsClause(node.AsClause)) _
                                                                .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                                .WithModifiers(TokenList(VisitModifiers(otherModifiers))) _
                                                                .WithParameterList(VisitParameterList(node.ParameterList)) _
                                                                .WithBody(Block(List(Visit(operatorBlock.Statements))))
                                                             )

                    Case VB.SyntaxKind.IsTrueKeyword
                        kind = CS.SyntaxKind.TrueKeyword
                    Case VB.SyntaxKind.IsFalseKeyword
                        kind = CS.SyntaxKind.FalseKeyword
                    Case VB.SyntaxKind.NotKeyword
                        kind = CS.SyntaxKind.BitwiseNotExpression
                    Case VB.SyntaxKind.PlusToken
                        kind = CS.SyntaxKind.PlusToken
                    Case VB.SyntaxKind.MinusToken
                        kind = CS.SyntaxKind.MinusMinusToken
                    Case VB.SyntaxKind.AsteriskToken
                        kind = CS.SyntaxKind.AsteriskToken
                    Case VB.SyntaxKind.SlashToken
                        kind = CS.SyntaxKind.SlashToken
                    Case VB.SyntaxKind.LessThanLessThanToken
                        kind = CS.SyntaxKind.LessThanLessThanToken
                    Case VB.SyntaxKind.GreaterThanGreaterThanToken
                        kind = CS.SyntaxKind.GreaterThanGreaterThanToken
                    Case VB.SyntaxKind.ModKeyword
                        kind = CS.SyntaxKind.PercentToken
                    Case VB.SyntaxKind.OrKeyword
                        kind = CS.SyntaxKind.BarToken
                    Case VB.SyntaxKind.XorKeyword
                        kind = CS.SyntaxKind.CaretToken
                    Case VB.SyntaxKind.AndKeyword
                        kind = CS.SyntaxKind.AmpersandToken
                    Case VB.SyntaxKind.EqualsToken
                        kind = CS.SyntaxKind.EqualsEqualsToken
                    Case VB.SyntaxKind.LessThanGreaterThanToken
                        kind = CS.SyntaxKind.ExclamationEqualsToken
                    Case VB.SyntaxKind.LessThanToken
                        kind = CS.SyntaxKind.LessThanToken
                    Case VB.SyntaxKind.LessThanEqualsToken
                        kind = CS.SyntaxKind.LessThanEqualsToken
                    Case VB.SyntaxKind.GreaterThanEqualsToken
                        kind = CS.SyntaxKind.GreaterThanEqualsToken
                    Case VB.SyntaxKind.GreaterThanToken
                        kind = CS.SyntaxKind.GreaterThanToken

                    Case VB.SyntaxKind.AmpersandToken,
                         VB.SyntaxKind.BackslashToken,
                         VB.SyntaxKind.LikeKeyword,
                         VB.SyntaxKind.CaretToken

                        Return NotImplementedMember(node)
                        ' TODO: Rewrite this as a normal method with the System.Runtime.CompilerServices.SpecialName attribute.
                        Throw New NotImplementedException(node.ToString())

                End Select

                Return TransferTrivia(operatorBlock, OperatorDeclaration(
                                                         DeriveType(node.OperatorToken, node.AsClause, node.OperatorKeyword),
                                                         Token(kind)) _
                                                     .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                     .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                                                     .WithParameterList(VisitParameterList(node.ParameterList)) _
                                                     .WithBody(Block(List(Visit(operatorBlock.Statements))))
                                        )

            End Function

            Public Overrides Function VisitOptionStatement(node As VB.Syntax.OptionStatementSyntax) As SyntaxNode

                Select Case node.NameKeyword.Kind
                    Case VB.SyntaxKind.ExplicitKeyword
                        If node.ValueKeyword.IsKind(VB.SyntaxKind.OffKeyword) Then
                            IsOptionExplicitOn = False

                            ' TODO: Log this.
                            ''Throw New NotSupportedException("Option Explicit Off")
                        End If
                    Case VB.SyntaxKind.CompareKeyword
                        If node.ValueKeyword.IsKind(VB.SyntaxKind.TextKeyword) Then
                            IsOptionCompareBinary = False

                            ' TODO: Log this.
                            ''Throw New NotImplementedException("Option Compare Text")
                        End If
                    Case VB.SyntaxKind.StrictKeyword

                        IsOptionStrictOn = Not node.ValueKeyword.IsKind(VB.SyntaxKind.OffKeyword)

                    Case VB.SyntaxKind.InferKeyword

                        IsOptionInferOn = Not node.ValueKeyword.IsKind(VB.SyntaxKind.OffKeyword)

                End Select

                Return Nothing
            End Function

            Public Overrides Function VisitOrderByClause(node As VB.Syntax.OrderByClauseSyntax) As SyntaxNode
                Return MyBase.VisitOrderByClause(node)
            End Function

            Public Overrides Function VisitOrdering(node As VB.Syntax.OrderingSyntax) As SyntaxNode
                Return MyBase.VisitOrdering(node)
            End Function

            Public Overrides Function VisitParameter(node As VB.Syntax.ParameterSyntax) As SyntaxNode

                Return Parameter(
                           List(VisitAttributeLists(node.AttributeLists)),
                           TokenList(VisitModifiers(node.Modifiers)),
                           DeriveType(node.Identifier, node.AsClause, initializer:=Nothing),
                           VisitIdentifier(node.Identifier.Identifier),
                           VisitEqualsValue(node.Default)
                       )

            End Function

            Public Overrides Function VisitParameterList(node As VB.Syntax.ParameterListSyntax) As SyntaxNode

                If node Is Nothing Then Return ParameterList()

                Return ParameterList(SeparatedList(Visit(node.Parameters).Cast(Of CS.Syntax.ParameterSyntax)))

            End Function

            Public Overrides Function VisitParenthesizedExpression(node As VB.Syntax.ParenthesizedExpressionSyntax) As SyntaxNode

                Return ParenthesizedExpression(Visit(node.Expression))

            End Function

            Public Overrides Function VisitPartitionClause(node As VB.Syntax.PartitionClauseSyntax) As SyntaxNode
                Return MyBase.VisitPartitionClause(node)
            End Function

            Public Overrides Function VisitPartitionWhileClause(node As VB.Syntax.PartitionWhileClauseSyntax) As SyntaxNode
                Return MyBase.VisitPartitionWhileClause(node)
            End Function

            Public Overrides Function VisitPredefinedCastExpression(node As VB.Syntax.PredefinedCastExpressionSyntax) As SyntaxNode

                ' NOTE: For conversions between intrinsic types this is an over-simplification.
                '       Depending on the source and target types this may be a C# cast, a VB runtime call, a BCL call, or a simple IL instruction.
                Dim kind As CS.SyntaxKind

                Select Case node.Keyword.Kind
                    Case VB.SyntaxKind.CByteKeyword
                        kind = CS.SyntaxKind.ByteKeyword
                    Case VB.SyntaxKind.CUShortKeyword
                        kind = CS.SyntaxKind.UShortKeyword
                    Case VB.SyntaxKind.CUIntKeyword
                        kind = CS.SyntaxKind.UIntKeyword
                    Case VB.SyntaxKind.CULngKeyword
                        kind = CS.SyntaxKind.ULongKeyword
                    Case VB.SyntaxKind.CSByteKeyword
                        kind = CS.SyntaxKind.SByteKeyword
                    Case VB.SyntaxKind.CShortKeyword
                        kind = CS.SyntaxKind.ShortKeyword
                    Case VB.SyntaxKind.CIntKeyword
                        kind = CS.SyntaxKind.IntKeyword
                    Case VB.SyntaxKind.CLngKeyword
                        kind = CS.SyntaxKind.LongKeyword
                    Case VB.SyntaxKind.CSngKeyword
                        kind = CS.SyntaxKind.FloatKeyword
                    Case VB.SyntaxKind.CDblKeyword
                        kind = CS.SyntaxKind.DoubleKeyword
                    Case VB.SyntaxKind.CDecKeyword
                        kind = CS.SyntaxKind.DecimalKeyword
                    Case VB.SyntaxKind.CStrKeyword
                        kind = CS.SyntaxKind.StringKeyword
                    Case VB.SyntaxKind.CCharKeyword
                        kind = CS.SyntaxKind.CharKeyword
                    Case VB.SyntaxKind.CDateKeyword
                        Return ParenthesizedExpression(CastExpression(ParseTypeName("global::System.DateTime"), Visit(node.Expression)))
                    Case VB.SyntaxKind.CBoolKeyword
                        kind = CS.SyntaxKind.BoolKeyword
                    Case VB.SyntaxKind.CObjKeyword
                        kind = CS.SyntaxKind.ObjectKeyword
                    Case Else
                        Throw New NotSupportedException(node.Keyword.Kind.ToString())
                End Select

                Return ParenthesizedExpression(CastExpression(PredefinedType(Token(kind)), Visit(node.Expression)))

            End Function

            Public Overrides Function VisitPredefinedType(node As VB.Syntax.PredefinedTypeSyntax) As SyntaxNode

                Dim kind As CS.SyntaxKind

                Select Case node.Keyword.Kind
                    Case VB.SyntaxKind.ByteKeyword
                        kind = CS.SyntaxKind.ByteKeyword
                    Case VB.SyntaxKind.UShortKeyword
                        kind = CS.SyntaxKind.UShortKeyword
                    Case VB.SyntaxKind.UIntegerKeyword
                        kind = CS.SyntaxKind.UIntKeyword
                    Case VB.SyntaxKind.ULongKeyword
                        kind = CS.SyntaxKind.ULongKeyword
                    Case VB.SyntaxKind.SByteKeyword
                        kind = CS.SyntaxKind.SByteKeyword
                    Case VB.SyntaxKind.ShortKeyword
                        kind = CS.SyntaxKind.ShortKeyword
                    Case VB.SyntaxKind.IntegerKeyword
                        kind = CS.SyntaxKind.IntKeyword
                    Case VB.SyntaxKind.LongKeyword
                        kind = CS.SyntaxKind.LongKeyword
                    Case VB.SyntaxKind.SingleKeyword
                        kind = CS.SyntaxKind.FloatKeyword
                    Case VB.SyntaxKind.DoubleKeyword
                        kind = CS.SyntaxKind.DoubleKeyword
                    Case VB.SyntaxKind.DecimalKeyword
                        kind = CS.SyntaxKind.DecimalKeyword
                    Case VB.SyntaxKind.StringKeyword
                        kind = CS.SyntaxKind.StringKeyword
                    Case VB.SyntaxKind.CharKeyword
                        kind = CS.SyntaxKind.CharKeyword
                    Case VB.SyntaxKind.DateKeyword
                        Return ParseTypeName("global::System.DateTime")
                    Case VB.SyntaxKind.BooleanKeyword
                        kind = CS.SyntaxKind.BoolKeyword
                    Case VB.SyntaxKind.ObjectKeyword
                        kind = CS.SyntaxKind.ObjectKeyword
                    Case Else
                        Throw New NotSupportedException(node.Keyword.Kind.ToString())
                End Select

                Return PredefinedType(Token(kind))

            End Function

            Public Overrides Function VisitPropertyBlock(node As VB.Syntax.PropertyBlockSyntax) As SyntaxNode

                Return VisitPropertyStatement(node.PropertyStatement)

            End Function

            Public Overrides Function VisitPropertyStatement(node As VB.Syntax.PropertyStatementSyntax) As SyntaxNode

                Dim propertyBlockOpt = TryCast(node.Parent, VB.Syntax.PropertyBlockSyntax)

                If propertyBlockOpt IsNot Nothing Then
                    Return TransferTrivia(propertyBlockOpt, PropertyDeclaration(
                                                                DeriveType(node.Identifier, node.AsClause),
                                                                VisitIdentifier(node.Identifier)) _
                                                            .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                            .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                                                            .WithAccessorList(AccessorList(List(Visit(propertyBlockOpt.Accessors))))
                                                    )
                Else

                    Dim accessors = New List(Of CS.Syntax.AccessorDeclarationSyntax)() From {
                                            AccessorDeclaration(CS.SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SemicolonToken),
                                            AccessorDeclaration(CS.SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SemicolonToken)
                                        }

                    ' For MustOverride properties and properties in interfaces we have to check the modifiers
                    ' to determine whether get or set accessors should be generated.
                    For Each modifier In node.Modifiers
                        Select Case modifier.Kind
                            Case VB.SyntaxKind.ReadOnlyKeyword
                                accessors.RemoveAt(1)
                            Case (VB.SyntaxKind.WriteOnlyKeyword)
                                accessors.RemoveAt(0)
                        End Select
                    Next

                    ' TODO: Transfer initializers on the auto-prop to the constructor.
                    Return TransferTrivia(node, PropertyDeclaration(
                                                    DeriveType(node.Identifier, node.AsClause),
                                                    VisitIdentifier(node.Identifier)) _
                                                .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                                                .WithAccessorList(AccessorList(List(accessors)))
                                    )
                End If

            End Function

            Public Overrides Function VisitQualifiedName(node As VB.Syntax.QualifiedNameSyntax) As SyntaxNode

                If TypeOf node.Left Is VB.Syntax.GlobalNameSyntax Then
                    Return AliasQualifiedName(IdentifierName("global"), Visit(node.Right))
                Else
                    Return QualifiedName(Visit(node.Left), Visit(node.Right))
                End If

            End Function

            Public Overrides Function VisitQueryExpression(node As VB.Syntax.QueryExpressionSyntax) As SyntaxNode

                Return (New QueryClauseConvertingVisitor(parent:=Me)).Visit(node)

            End Function

            Public Overrides Function VisitRaiseEventStatement(node As VB.Syntax.RaiseEventStatementSyntax) As SyntaxNode

                ' TODO: Rewrite to a conditional invocation based on a thread-safe null check.
                Return TransferTrivia(node, ExpressionStatement(InvocationExpression(VisitIdentifierName(node.Name), VisitArgumentList(node.ArgumentList))))

            End Function

            Public Overrides Function VisitRangeArgument(node As VB.Syntax.RangeArgumentSyntax) As SyntaxNode
                Return MyBase.VisitRangeArgument(node)
            End Function

            Public Overrides Function VisitReDimStatement(node As VB.Syntax.ReDimStatementSyntax) As SyntaxNode

                Return NotImplementedStatement(node)
                ' TODO: Implement rewrite to Array.CopyTo with new array creation.
                Throw New NotImplementedException(node.ToString())

            End Function

            Public Overrides Function VisitRegionDirectiveTrivia(node As VB.Syntax.RegionDirectiveTriviaSyntax) As SyntaxNode

                Return RegionDirectiveTrivia(isActive:=False).WithRegionKeyword(Token(SyntaxTriviaList.Create(CS.SyntaxFactory.ElasticMarker), CS.SyntaxKind.RegionKeyword, TriviaList(PreprocessingMessage(node.Name.ValueText))))

            End Function

            Public Overrides Function VisitResumeStatement(node As VB.Syntax.ResumeStatementSyntax) As SyntaxNode

                Return NotImplementedStatement(node)
                Throw New NotSupportedException("Resume statements.")

            End Function

            Public Overrides Function VisitReturnStatement(node As VB.Syntax.ReturnStatementSyntax) As SyntaxNode

                Return ReturnStatement(Visit(node.Expression))

            End Function

            Public Overrides Function VisitSelectBlock(node As VB.Syntax.SelectBlockSyntax) As SyntaxNode

                ' TODO: Bind to expression to ensure it's of a type C# can switch on.
                Return TransferTrivia(node, SwitchStatement(Visit(node.SelectStatement.Expression)).WithSections(List(VisitCaseBlocks(node.CaseBlocks))))

            End Function

            Public Overrides Function VisitSelectClause(node As VB.Syntax.SelectClauseSyntax) As SyntaxNode
                Return MyBase.VisitSelectClause(node)
            End Function

            Public Overrides Function VisitSelectStatement(node As VB.Syntax.SelectStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitSimpleArgument(node As VB.Syntax.SimpleArgumentSyntax) As SyntaxNode

                If node.IsNamed Then
                    If TypeOf node.Parent.Parent Is VB.Syntax.AttributeSyntax Then
                        Return AttributeArgument(Visit(node.Expression)).WithNameColon(NameColon(IdentifierName(VisitIdentifier(node.NameColonEquals.Name.Identifier))))
                    Else
                        ' TODO: Bind to discover ByRef arguments.
                        Return CS.SyntaxFactory.Argument(Visit(node.Expression)).WithNameColon(NameColon(IdentifierName(VisitIdentifier(node.NameColonEquals.Name.Identifier))))
                    End If
                Else
                    If TypeOf node.Parent.Parent Is VB.Syntax.AttributeSyntax Then
                        Return AttributeArgument(Visit(node.Expression))
                    Else
                        ' TODO: Bind to discover ByRef arguments.
                        Return CS.SyntaxFactory.Argument(Visit(node.Expression))
                    End If
                End If

            End Function

            Public Overrides Function VisitSimpleAsClause(node As VB.Syntax.SimpleAsClauseSyntax) As SyntaxNode

                If node Is Nothing Then Return Nothing

                Return Visit(node.Type)

            End Function

            Public Overrides Function VisitSingleLineIfStatement(node As VB.Syntax.SingleLineIfStatementSyntax) As SyntaxNode

                Dim elseOpt As CS.Syntax.ElseClauseSyntax = Nothing

                If node.ElseClause IsNot Nothing Then
                    elseOpt = ElseClause(Block(List(Visit(node.ElseClause.Statements))))
                End If

                Return TransferTrivia(node, IfStatement(
                                                Visit(node.Condition),
                                                Block(List(Visit(node.Statements)))) _
                                              .WithElse(elseOpt)
                                        )


            End Function

            Public Overrides Function VisitSingleLineLambdaExpression(node As VB.Syntax.SingleLineLambdaExpressionSyntax) As SyntaxNode

                Dim asyncKeyword = If(node.SubOrFunctionHeader.Modifiers.Any(VB.SyntaxKind.AsyncKeyword),
                                      Token(CS.SyntaxKind.AsyncKeyword),
                                      Nothing)

                Dim parameterList = VisitParameterList(node.SubOrFunctionHeader.ParameterList)

                Dim arrowToken = Token(CS.SyntaxKind.EqualsGreaterThanToken)

                Dim body = If(node.IsKind(VB.SyntaxKind.SingleLineFunctionLambdaExpression),
                              Visit(node.Body),
                              Block(SingletonList(Visit(node.Body))))

                Return ParenthesizedLambdaExpression(asyncKeyword, parameterList, arrowToken, body)

            End Function

            Public Overrides Function VisitSkippedTokensTrivia(node As VB.Syntax.SkippedTokensTriviaSyntax) As SyntaxNode

                Return SkippedTokensTrivia(TokenList(MissingToken(CS.SyntaxKind.SemicolonToken).WithTrailingTrivia(TriviaList(Comment("/* " & node.ToString() & " */")))))

            End Function

            Public Overrides Function VisitSpecialConstraint(node As VB.Syntax.SpecialConstraintSyntax) As SyntaxNode

                Select Case node.Kind
                    Case VB.SyntaxKind.NewConstraint
                        Return ConstructorConstraint()
                    Case VB.SyntaxKind.ClassConstraint
                        Return ClassOrStructConstraint(CS.SyntaxKind.ClassConstraint)
                    Case VB.SyntaxKind.StructureConstraint
                        Return ClassOrStructConstraint(CS.SyntaxKind.StructConstraint)
                    Case Else
                        Throw New NotSupportedException(node.Kind.ToString())
                End Select

            End Function

            Public Overrides Function VisitStopOrEndStatement(node As VB.Syntax.StopOrEndStatementSyntax) As SyntaxNode

                Return NotImplementedStatement(node)
                ' TODO: Rewrite Stop to System.Diagnostics.Debug.Break and End to System.Environment.Exit.
                Throw New NotImplementedException(node.ToString())

            End Function

            Public Overrides Function VisitSyncLockBlock(node As VB.Syntax.SyncLockBlockSyntax) As SyntaxNode

                Return VisitSyncLockStatement(node.SyncLockStatement)

            End Function

            Public Overrides Function VisitSyncLockStatement(node As VB.Syntax.SyncLockStatementSyntax) As SyntaxNode

                Dim syncLockBlock As VB.Syntax.SyncLockBlockSyntax = node.Parent

                Return LockStatement(Visit(node.Expression), Block(List(Visit(syncLockBlock.Statements))))

            End Function

            Public Overrides Function VisitTernaryConditionalExpression(node As VB.Syntax.TernaryConditionalExpressionSyntax) As SyntaxNode

                Return ConditionalExpression(Visit(node.Condition), Visit(node.WhenTrue), Visit(node.WhenFalse))

            End Function

            Public Overrides Function VisitThrowStatement(node As VB.Syntax.ThrowStatementSyntax) As SyntaxNode

                If node.Expression Is Nothing Then
                    Return ThrowStatement()
                Else
                    Return ThrowStatement(Visit(node.Expression))
                End If

            End Function

            Protected Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia

                Dim text = trivia.ToFullString()

                Select Case trivia.Kind
                    Case VB.SyntaxKind.CommentTrivia

                        If text.StartsWith("'") AndAlso text.Length > 1 Then
                            Return Comment("//" & text.Substring(1))
                        ElseIf text.StartsWith("REM", StringComparison.OrdinalIgnoreCase) AndAlso text.Length > 3 Then
                            Return Comment("//" & text.Substring(3))
                        Else
                            Return Comment("//")
                        End If

                    Case VB.SyntaxKind.DisabledTextTrivia

                        Return Comment("/* Disabled: " & text & " */")

                    Case VB.SyntaxKind.EndOfLineTrivia

                        Return EndOfLine(text)

                    Case VB.SyntaxKind.DocumentationCommentTrivia

                        Return CS.SyntaxFactory.Trivia(VisitDocumentationCommentTrivia(trivia.GetStructure()))

                    Case VB.SyntaxKind.WhitespaceTrivia

                        Return Whitespace(text)

                    Case Else

                        Return Comment("/* " & text & " */")

                End Select
            End Function

            Protected Function VisitTrivia(trivia As IEnumerable(Of SyntaxTrivia)) As SyntaxTriviaList

                Return TriviaList(From t In trivia Select VisitTrivia(t))

            End Function

            Public Overrides Function VisitTryBlock(node As VB.Syntax.TryBlockSyntax) As SyntaxNode

                Return TransferTrivia(node, TryStatement(List(VisitCatchBlocks(node.CatchBlocks))) _
                                                .WithBlock(Block(List(Visit(node.Statements)))) _
                                                .WithFinally(VisitFinallyBlock(node.FinallyBlock))
                                            )

            End Function

            Public Overrides Function VisitTryStatement(node As VB.Syntax.TryStatementSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Public Overrides Function VisitTypeArgumentList(node As VB.Syntax.TypeArgumentListSyntax) As SyntaxNode

                Return TypeArgumentList(SeparatedList(Visit(node.Arguments).Cast(Of CS.Syntax.TypeSyntax)))

            End Function

            Public Overrides Function VisitModuleBlock(ByVal node As VB.Syntax.ModuleBlockSyntax) As SyntaxNode

                Return VisitModuleStatement(node.ModuleStatement)

            End Function

            Public Overrides Function VisitClassBlock(ByVal node As VB.Syntax.ClassBlockSyntax) As SyntaxNode

                Return VisitClassStatement(node.ClassStatement)

            End Function

            Public Overrides Function VisitStructureBlock(ByVal node As VB.Syntax.StructureBlockSyntax) As SyntaxNode

                Return VisitStructureStatement(node.StructureStatement)

            End Function

            Public Overrides Function VisitInterfaceBlock(ByVal node As VB.Syntax.InterfaceBlockSyntax) As SyntaxNode

                Return VisitInterfaceStatement(node.InterfaceStatement)

            End Function

            Public Overrides Function VisitTypeConstraint(ByVal node As VB.Syntax.TypeConstraintSyntax) As SyntaxNode

                Return TypeConstraint(Visit(node.Type))

            End Function

            Public Overrides Function VisitTypeOfExpression(node As VB.Syntax.TypeOfExpressionSyntax) As SyntaxNode

                Dim isExpression = BinaryExpression(CS.SyntaxKind.IsExpression, Visit(node.Expression), Visit(node.Type))

                If node.IsKind(VB.SyntaxKind.TypeOfIsNotExpression) Then
                    Return PrefixUnaryExpression(CS.SyntaxKind.LogicalNotExpression,
                                                 ParenthesizedExpression(isExpression)
                           )
                Else
                    Return isExpression
                End If

            End Function

            Public Overrides Function VisitTypeParameter(node As VB.Syntax.TypeParameterSyntax) As SyntaxNode

                Dim varianceKeyword As SyntaxToken
                Select Case node.VarianceKeyword.Kind
                    Case VB.SyntaxKind.InKeyword
                        varianceKeyword = Token(CS.SyntaxKind.InKeyword)

                    Case VB.SyntaxKind.OutKeyword
                        varianceKeyword = Token(CS.SyntaxKind.OutKeyword)

                    Case Else
                        varianceKeyword = Token(CS.SyntaxKind.None)
                End Select

                Return TypeParameter(VisitIdentifier(node.Identifier)).WithVarianceKeyword(varianceKeyword)

            End Function

            Public Overrides Function VisitTypeParameterList(node As VB.Syntax.TypeParameterListSyntax) As SyntaxNode

                If node Is Nothing Then Return Nothing

                Return TypeParameterList(SeparatedList(Visit(node.Parameters).Cast(Of CS.Syntax.TypeParameterSyntax)))

            End Function

            Protected Function VisitTypeParameterConstraintClauses(typeParameterListOpt As VB.Syntax.TypeParameterListSyntax) As IEnumerable(Of SyntaxNode)

                If typeParameterListOpt Is Nothing Then Return Nothing

                Return Visit((From parameter In typeParameterListOpt.Parameters Where parameter.TypeParameterConstraintClause IsNot Nothing Select parameter.TypeParameterConstraintClause))

            End Function

            Public Overrides Function VisitTypeParameterMultipleConstraintClause(node As VB.Syntax.TypeParameterMultipleConstraintClauseSyntax) As SyntaxNode

                If node Is Nothing Then Return Nothing

                ' In C# the new() constraint must be specified last.
                Return TypeParameterConstraintClause(IdentifierName(VisitIdentifier(CType(node.Parent, VB.Syntax.TypeParameterSyntax).Identifier))).WithConstraints(SeparatedList(Visit(From c In node.Constraints Order By c.IsKind(VB.SyntaxKind.NewConstraint)).Cast(Of CS.Syntax.TypeParameterConstraintSyntax)))

            End Function

            Public Overrides Function VisitTypeParameterSingleConstraintClause(node As VB.Syntax.TypeParameterSingleConstraintClauseSyntax) As SyntaxNode

                If node Is Nothing Then Return Nothing

                Return TypeParameterConstraintClause(IdentifierName(VisitIdentifier(CType(node.Parent, VB.Syntax.TypeParameterSyntax).Identifier))).WithConstraints(SingletonSeparatedList(CType(Visit(node.Constraint), CS.Syntax.TypeParameterConstraintSyntax)))

            End Function

            Public Overrides Function VisitModuleStatement(ByVal node As VB.Syntax.ModuleStatementSyntax) As SyntaxNode
                Dim block As VB.Syntax.ModuleBlockSyntax = node.Parent

                ' TODO: Rewrite all members in a module to be static.
                Return TransferTrivia(block, ClassDeclaration(VisitIdentifier(node.Identifier)) _
                                                .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                                                .WithTypeParameterList(VisitTypeParameterList(node.TypeParameterList)) _
                                                .WithConstraintClauses(List(VisitTypeParameterConstraintClauses(node.TypeParameterList))) _
                                                .WithMembers(List(Visit(block.Members)))
                                             )

            End Function

            Public Overrides Function VisitClassStatement(ByVal node As VB.Syntax.ClassStatementSyntax) As SyntaxNode

                Dim block As VB.Syntax.ClassBlockSyntax = node.Parent

                Dim bases As CS.Syntax.BaseListSyntax = Nothing
                If block.Inherits.Count > 0 OrElse block.Implements.Count > 0 Then
                    bases = BaseList(SeparatedList(Of BaseTypeSyntax)(VisitInheritsStatements(block.Inherits).Union(VisitImplementsStatements(block.Implements)).
                                                                      Cast(Of CS.Syntax.TypeSyntax).Select(Function(t) SimpleBaseType(t))))
                End If

                Return TransferTrivia(block, ClassDeclaration(VisitIdentifier(node.Identifier)) _
                                                .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                                                .WithTypeParameterList(VisitTypeParameterList(node.TypeParameterList)) _
                                                .WithBaseList(bases) _
                                                .WithConstraintClauses(List(VisitTypeParameterConstraintClauses(node.TypeParameterList))) _
                                                .WithMembers(List(Visit(block.Members)))
                                             )

            End Function

            Public Overrides Function VisitStructureStatement(ByVal node As VB.Syntax.StructureStatementSyntax) As SyntaxNode

                Dim block As VB.Syntax.StructureBlockSyntax = node.Parent

                Dim bases As CS.Syntax.BaseListSyntax = Nothing
                If block.Inherits.Count > 0 OrElse block.Implements.Count > 0 Then
                    bases = BaseList(SeparatedList(Of BaseTypeSyntax)(VisitInheritsStatements(block.Inherits).Union(VisitImplementsStatements(block.Implements)).
                                                                      Cast(Of CS.Syntax.TypeSyntax).Select(Function(t) SimpleBaseType(t))))
                End If

                Return TransferTrivia(block, StructDeclaration(VisitIdentifier(node.Identifier)) _
                                                .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                                                .WithTypeParameterList(VisitTypeParameterList(node.TypeParameterList)) _
                                                .WithBaseList(bases) _
                                                .WithConstraintClauses(List(VisitTypeParameterConstraintClauses(node.TypeParameterList))) _
                                                .WithMembers(List(Visit(block.Members)))
                                        )

            End Function

            Public Overrides Function VisitInterfaceStatement(ByVal node As VB.Syntax.InterfaceStatementSyntax) As SyntaxNode

                Dim block As VB.Syntax.InterfaceBlockSyntax = node.Parent

                Dim bases As CS.Syntax.BaseListSyntax = Nothing
                If block.Inherits.Count > 0 Then
                    bases = BaseList(SeparatedList(Of BaseTypeSyntax)(VisitInheritsStatements(block.Inherits).
                                                                      Cast(Of CS.Syntax.TypeSyntax).Select(Function(t) SimpleBaseType(t))))
                End If

                ' VB allows Interfaces to have nested types, C# does not. 
                ' But this is rare enough that we'll assume the members are non-types for now.
                Return TransferTrivia(block, InterfaceDeclaration(VisitIdentifier(node.Identifier)) _
                                                .WithAttributeLists(List(VisitAttributeLists(node.AttributeLists))) _
                                                .WithModifiers(TokenList(VisitModifiers(node.Modifiers))) _
                                                .WithTypeParameterList(VisitTypeParameterList(node.TypeParameterList)) _
                                                .WithBaseList(bases) _
                                                .WithConstraintClauses(List(VisitTypeParameterConstraintClauses(node.TypeParameterList))) _
                                                .WithMembers(List(Visit(block.Members)))
                                             )
            End Function

            Public Overrides Function VisitUnaryExpression(ByVal node As VB.Syntax.UnaryExpressionSyntax) As SyntaxNode

                Select Case node.Kind
                    Case VB.SyntaxKind.UnaryMinusExpression

                        Return PrefixUnaryExpression(CS.SyntaxKind.UnaryMinusExpression, Visit(node.Operand))

                    Case VB.SyntaxKind.UnaryPlusExpression

                        Return PrefixUnaryExpression(CS.SyntaxKind.UnaryPlusExpression, Visit(node.Operand))

                    Case VB.SyntaxKind.NotExpression

                        ' TODO: Bind expression to determine whether this is a logical or bitwise not expression.
                        Return PrefixUnaryExpression(CS.SyntaxKind.LogicalNotExpression, Visit(node.Operand))

                    Case VB.SyntaxKind.AddressOfExpression

                        Return Visit(node.Operand)

                    Case Else
                        Throw New NotSupportedException(node.Kind.ToString())
                End Select

            End Function

            Public Overrides Function VisitUsingBlock(node As VB.Syntax.UsingBlockSyntax) As SyntaxNode

                Return VisitUsingStatement(node.UsingStatement)

            End Function

            Public Overrides Function VisitUsingStatement(node As VB.Syntax.UsingStatementSyntax) As SyntaxNode

                Dim usingBlock As VB.Syntax.UsingBlockSyntax = node.Parent

                Dim body As CS.Syntax.StatementSyntax = Block(List(Visit(usingBlock.Statements)))

                If node.Expression IsNot Nothing Then

                    Return TransferTrivia(usingBlock, UsingStatement(body).WithExpression(Visit(node.Expression)))

                Else

                    For i = node.Variables.Count - 1 To 0 Step -1

                        Dim declarator = node.Variables(i)

                        ' TODO: Refactor so that visiting a VB declarator returns a C# declarator.
                        body = UsingStatement(body).WithDeclaration(
                                   VariableDeclaration(
                                       DeriveType(declarator.Names(0), declarator.AsClause, declarator.Initializer),
                                       SingletonSeparatedList(VariableDeclarator(
                                                         VisitIdentifier(declarator.Names(0).Identifier)) _
                                                              .WithInitializer(DeriveInitializer(declarator.Names(0), declarator.AsClause, declarator.Initializer))
                                       )
                                   )
                               )

                    Next

                    Return TransferTrivia(node, body)
                End If

            End Function

            Public Overrides Function VisitVariableDeclarator(node As VB.Syntax.VariableDeclaratorSyntax) As SyntaxNode

                Throw New InvalidOperationException()

            End Function

            Protected Function VisitVariableDeclaratorVariables(declarator As VB.Syntax.VariableDeclaratorSyntax) As IEnumerable(Of SyntaxNode)

                ' TODO: Derive an initializer based on VB's As New syntax or default variable
                ' initialization.
                Select Case declarator.Parent.Kind
                    Case VB.SyntaxKind.FieldDeclaration
                        Dim field As VB.Syntax.FieldDeclarationSyntax = declarator.Parent

                        Return From v In declarator.Names Select FieldDeclaration(
                                                                     VariableDeclaration(
                                                                         DeriveType(v, declarator.AsClause, declarator.Initializer),
                                                                         SingletonSeparatedList(VariableDeclarator(
                                                                                           VisitIdentifier(v.Identifier)).WithInitializer(
                                                                                           DeriveInitializer(v, declarator.AsClause, declarator.Initializer)
                                                                                       )
                                                                         )
                                                                     )
                                                                 ).WithAttributeLists(List(VisitAttributeLists(field.AttributeLists))) _
                                                                  .WithModifiers(TokenList(VisitModifiers(field.Modifiers)))
                    Case VB.SyntaxKind.LocalDeclarationStatement
                        Dim local As VB.Syntax.LocalDeclarationStatementSyntax = declarator.Parent

                        Return From v In declarator.Names Select LocalDeclarationStatement(
                                                                     VariableDeclaration(
                                                                         DeriveType(v, declarator.AsClause, declarator.Initializer),
                                                                         SingletonSeparatedList(
                                                                             VariableDeclarator(
                                                                                 VisitIdentifier(v.Identifier)).WithInitializer(
                                                                                     DeriveInitializer(v, declarator.AsClause, declarator.Initializer)
                                                                             )
                                                                         )
                                                                     )
                                                                 ).WithModifiers(TokenList(VisitModifiers(local.Modifiers)))

                    Case Else
                        Throw New NotSupportedException(declarator.Parent.Kind.ToString())
                End Select
            End Function

            Public Overrides Function VisitVariableNameEquals(node As VB.Syntax.VariableNameEqualsSyntax) As SyntaxNode
                Return MyBase.VisitVariableNameEquals(node)
            End Function

            Public Overrides Function VisitWhereClause(node As VB.Syntax.WhereClauseSyntax) As SyntaxNode

                Return WhereClause(Visit(node.Condition))

            End Function

            Public Overrides Function VisitWhileBlock(node As VB.Syntax.WhileBlockSyntax) As SyntaxNode

                Return VisitWhileStatement(node.WhileStatement)

            End Function

            Public Overrides Function VisitWhileStatement(node As VB.Syntax.WhileStatementSyntax) As SyntaxNode

                Dim whileBlock As VB.Syntax.WhileBlockSyntax = node.Parent

                Return TransferTrivia(node, WhileStatement(Visit(node.Condition), Block(List(Visit(whileBlock.Statements)))))

            End Function

            Public Overrides Function VisitWhileOrUntilClause(node As VB.Syntax.WhileOrUntilClauseSyntax) As SyntaxNode

                If node Is Nothing Then Return Nothing

                If node.IsKind(VB.SyntaxKind.WhileClause) Then
                    Return Visit(node.Condition)
                Else
                    ' TODO: Invert conditionals if possible on comparison expressions to avoid wrapping this in a !expression.
                    Return PrefixUnaryExpression(CS.SyntaxKind.LogicalNotExpression, ParenthesizedExpression(Visit(node.Condition)))
                End If

            End Function

            Public Overrides Function VisitWithBlock(node As VB.Syntax.WithBlockSyntax) As SyntaxNode

                Return VisitWithStatement(node.WithStatement)

            End Function

            Public Overrides Function VisitWithStatement(node As VB.Syntax.WithStatementSyntax) As SyntaxNode

                Return NotImplementedStatement(node)
                ' TODO: Rewrite to block with temp variable name instead of omitted LeftOpt member access expressions.
                Throw New NotImplementedException(node.ToString())

            End Function

            Public Overrides Function VisitXmlAttribute(node As VB.Syntax.XmlAttributeSyntax) As SyntaxNode
                Return MyBase.VisitXmlAttribute(node)
            End Function

            Public Overrides Function VisitXmlBracketedName(node As VB.Syntax.XmlBracketedNameSyntax) As SyntaxNode
                Return MyBase.VisitXmlBracketedName(node)
            End Function

            Public Overrides Function VisitXmlCDataSection(node As VB.Syntax.XmlCDataSectionSyntax) As SyntaxNode
                Return VisitXmlNode(node)
            End Function

            Public Overrides Function VisitXmlComment(node As VB.Syntax.XmlCommentSyntax) As SyntaxNode
                Return VisitXmlNode(node)
            End Function

            Public Overrides Function VisitXmlDeclaration(node As VB.Syntax.XmlDeclarationSyntax) As SyntaxNode
                Return MyBase.VisitXmlDeclaration(node)
            End Function

            Public Overrides Function VisitXmlDeclarationOption(node As VB.Syntax.XmlDeclarationOptionSyntax) As SyntaxNode
                Return MyBase.VisitXmlDeclarationOption(node)
            End Function

            Public Overrides Function VisitXmlDocument(node As VB.Syntax.XmlDocumentSyntax) As SyntaxNode
                Return VisitXmlNode(node)
            End Function

            Public Overrides Function VisitXmlElement(node As VB.Syntax.XmlElementSyntax) As SyntaxNode
                Return VisitXmlNode(node)
            End Function

            Public Overrides Function VisitXmlElementEndTag(node As VB.Syntax.XmlElementEndTagSyntax) As SyntaxNode
                Return MyBase.VisitXmlElementEndTag(node)
            End Function

            Public Overrides Function VisitXmlElementStartTag(node As VB.Syntax.XmlElementStartTagSyntax) As SyntaxNode
                Return MyBase.VisitXmlElementStartTag(node)
            End Function

            Public Overrides Function VisitXmlEmbeddedExpression(node As VB.Syntax.XmlEmbeddedExpressionSyntax) As SyntaxNode
                Return MyBase.VisitXmlEmbeddedExpression(node)
            End Function

            Public Overrides Function VisitXmlEmptyElement(node As VB.Syntax.XmlEmptyElementSyntax) As SyntaxNode
                Return VisitXmlNode(node)
            End Function

            Public Overrides Function VisitXmlMemberAccessExpression(node As VB.Syntax.XmlMemberAccessExpressionSyntax) As SyntaxNode
                Return NotImplementedExpression(node)
            End Function

            Public Overrides Function VisitXmlName(node As VB.Syntax.XmlNameSyntax) As SyntaxNode
                Return MyBase.VisitXmlName(node)
            End Function

            Public Overrides Function VisitXmlNamespaceImportsClause(node As VB.Syntax.XmlNamespaceImportsClauseSyntax) As SyntaxNode

                Return UsingDirective(IdentifierName(MissingToken(CS.SyntaxKind.IdentifierToken))) _
                    .WithUsingKeyword(MissingToken(CS.SyntaxKind.UsingKeyword)) _
                    .WithSemicolonToken(MissingSemicolonToken.WithTrailingTrivia(TriviaList(Comment("/* " & node.ToString() & " */"))))

            End Function

            Protected Overridable Function VisitXmlNode(node As VB.Syntax.XmlNodeSyntax) As SyntaxNode
                ' Just spit this out as a string literal for now.
                Dim text = node.ToString().Replace("""", """""")

                Return LiteralExpression(CS.SyntaxKind.StringLiteralExpression, Literal("@""" & text & """", text))
            End Function

            Public Overrides Function VisitXmlPrefix(node As VB.Syntax.XmlPrefixSyntax) As SyntaxNode
                Return MyBase.VisitXmlPrefix(node)
            End Function

            Public Overrides Function VisitXmlProcessingInstruction(node As VB.Syntax.XmlProcessingInstructionSyntax) As SyntaxNode
                Return VisitXmlNode(node)
            End Function

            Public Overrides Function VisitXmlString(node As VB.Syntax.XmlStringSyntax) As SyntaxNode
                Return MyBase.VisitXmlString(node)
            End Function

            Public Overrides Function VisitXmlText(node As VB.Syntax.XmlTextSyntax) As SyntaxNode
                Return MyBase.VisitXmlText(node)
            End Function

            Protected Function NotImplementedStatement(node As SyntaxNode) As CS.Syntax.StatementSyntax
                Return EmptyStatement(MissingSemicolonToken.WithTrailingTrivia(TriviaList(Comment("/* Not Implemented: " & node.ToString() & " */"))))
            End Function

            Protected Function NotImplementedMember(node As SyntaxNode) As CS.Syntax.MemberDeclarationSyntax
                Return IncompleteMember().WithModifiers(TokenList(MissingToken(CS.SyntaxKind.PublicKeyword).WithTrailingTrivia(TriviaList(Comment("/* Not Implemented: " & node.ToString() & " */")))))
            End Function

            Protected Function NotImplementedExpression(node As SyntaxNode) As CS.Syntax.ExpressionSyntax
                Return IdentifierName(MissingToken(CS.SyntaxKind.IdentifierToken).WithTrailingTrivia(TriviaList(Comment("/* Not Implemented: " & node.ToString() & " */"))))
            End Function

            Protected Function NotImplementedModifier(token As SyntaxToken) As SyntaxToken
                Return MissingToken(CS.SyntaxKind.PublicKeyword).WithTrailingTrivia(TriviaList(Comment("/* Not Implemented: " & token.ToString() & " */")))
            End Function

        End Class

    End Class

    Friend Module SyntaxUtils

        ReadOnly CommaToken As SyntaxToken = Token(CS.SyntaxKind.CommaToken)
        ReadOnly OmittedArraySizeExpression As SyntaxNode = CS.SyntaxFactory.OmittedArraySizeExpression(Token(CS.SyntaxKind.OmittedArraySizeExpressionToken))

        Public Function OmittedArraySizeExpressionList(Of TNode As SyntaxNode)(rank As Integer) As SeparatedSyntaxList(Of TNode)

            Dim tokens = New SyntaxNodeOrToken(0 To 2 * rank - 2) {}
            For i = 0 To rank - 2
                tokens(2 * i) = OmittedArraySizeExpression
                tokens(2 * i + 1) = CommaToken
            Next

            tokens(2 * rank - 2) = OmittedArraySizeExpression

            Return SeparatedList(Of TNode)(tokens)

        End Function

        <Extension()>
        Public Function WithLeadingTrivia(Of TNode As SyntaxNode)(node As TNode, trivia As IEnumerable(Of SyntaxTrivia)) As TNode
            Dim firstToken = node.GetFirstToken()

            Return node.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(TriviaList(trivia)))
        End Function

        <Extension()>
        Public Function WithTrailingTrivia(Of TNode As SyntaxNode)(node As TNode, trivia As IEnumerable(Of SyntaxTrivia)) As TNode
            Dim lastToken = node.GetLastToken()

            Return node.ReplaceToken(lastToken, lastToken.WithTrailingTrivia(TriviaList(trivia)))
        End Function

        <Extension()>
        Public Function WithTrivia(Of TNode As SyntaxNode)(node As TNode, leadingTrivia As IEnumerable(Of SyntaxTrivia), trailingTrivia As IEnumerable(Of SyntaxTrivia)) As TNode
            Return node.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia)
        End Function

    End Module

End Namespace
