' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.MethodXml

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.MethodXml
    Friend Class MethodXmlBuilder
        Inherits AbstractMethodXmlBuilder

        Private Sub New(symbol As IMethodSymbol, semanticModel As SemanticModel)
            MyBase.New(symbol, semanticModel)
        End Sub

        Private Sub GenerateStatementBlock(statementList As SyntaxList(Of StatementSyntax))
            Using BlockTag()
                For Each statement In statementList
                    GenerateStatement(statement)
                Next
            End Using
        End Sub

        Private Sub GenerateStatement(statement As StatementSyntax)
            Dim success = False
            Dim mark = GetMark()

            Select Case statement.Kind
                Case SyntaxKind.LocalDeclarationStatement
                    success = TryGenerateLocal(DirectCast(statement, LocalDeclarationStatementSyntax))
                Case SyntaxKind.SimpleAssignmentStatement
                    success = TryGenerateAssignment(DirectCast(statement, AssignmentStatementSyntax))
                Case SyntaxKind.CallStatement
                    success = TryGenerateCall(DirectCast(statement, CallStatementSyntax))
                Case SyntaxKind.ExpressionStatement
                    success = TryGenerateExpressionStatement(DirectCast(statement, ExpressionStatementSyntax))
            End Select

            If Not success Then
                Rewind(mark)
                GenerateUnknown(statement)
            End If

            ' Just for readability
            LineBreak()
        End Sub

        Private Function TryGenerateLocal(localDeclaration As LocalDeclarationStatementSyntax) As Boolean
            For Each declarator In localDeclaration.Declarators
                Using LocalTag(GetLineNumber(localDeclaration))
                    ' Note: this is a difference from Dev10 where VB always generates
                    ' a full assembly-qualified name. Because this extra information is
                    ' generally wasteful, and C# never generates, we'll just generate
                    ' the non-assembly-qualified name instead.
                    Dim type = declarator.Type(SemanticModel)
                    If type Is Nothing Then
                        Return False
                    End If

                    GenerateType(type)

                    For Each name In declarator.Names
                        Using NameTag()
                            EncodedText(name.Identifier.ToString())
                        End Using

                        Dim initializer = declarator.GetInitializer()

                        If type.TypeKind = TypeKind.Array AndAlso
                          (name.ArrayBounds IsNot Nothing OrElse initializer IsNot Nothing) Then
                            Using ExpressionTag()
                                If Not TryGenerateNewArray(name.ArrayBounds, initializer, type) Then
                                    Return False
                                End If
                            End Using
                        ElseIf initializer IsNot Nothing AndAlso Not TryGenerateExpression(initializer) Then
                            Return False
                        End If
                    Next
                End Using
            Next

            Return True
        End Function

        Private Function TryGenerateAssignment(assignmentStatement As AssignmentStatementSyntax) As Boolean
            Using ExpressionStatementTag(GetLineNumber(assignmentStatement))
                Using ExpressionTag()
                    Using AssignmentTag()
                        Return TryGenerateExpression(assignmentStatement.Left) AndAlso
                               TryGenerateExpression(assignmentStatement.Right)
                    End Using
                End Using
            End Using
        End Function

        Private Function TryGenerateCall(callStatement As CallStatementSyntax) As Boolean
            Using ExpressionStatementTag(GetLineNumber(callStatement))
                Return TryGenerateExpression(callStatement.Invocation)
            End Using
        End Function

        Private Function TryGenerateExpressionStatement(expressionStatement As ExpressionStatementSyntax) As Boolean
            Using ExpressionStatementTag(GetLineNumber(expressionStatement))
                Return TryGenerateExpression(expressionStatement.Expression)
            End Using
        End Function

        Private Function TryGenerateExpression(expression As ExpressionSyntax) As Boolean
            Using ExpressionTag()
                Return TryGenerateExpressionSansTag(expression)
            End Using
        End Function

        Private Function TryGenerateExpressionSansTag(expression As ExpressionSyntax) As Boolean
            Select Case expression.Kind
                Case SyntaxKind.CharacterLiteralExpression,
                     SyntaxKind.UnaryMinusExpression,
                     SyntaxKind.NumericLiteralExpression,
                     SyntaxKind.StringLiteralExpression,
                     SyntaxKind.TrueLiteralExpression,
                     SyntaxKind.FalseLiteralExpression
                    Return TryGenerateLiteral(expression)

                Case SyntaxKind.NothingLiteralExpression
                    GenerateNullLiteral()
                    Return True

                Case SyntaxKind.ParenthesizedExpression
                    Return TryGenerateParenthesizedExpression(DirectCast(expression, ParenthesizedExpressionSyntax))

                Case SyntaxKind.AddExpression,
                     SyntaxKind.OrExpression,
                     SyntaxKind.AndExpression,
                     SyntaxKind.ConcatenateExpression
                    Return TryGenerateBinaryOperation(DirectCast(expression, BinaryExpressionSyntax))

                Case SyntaxKind.CollectionInitializer
                    Return TryGenerateCollectionInitializer(DirectCast(expression, CollectionInitializerSyntax))

                Case SyntaxKind.ArrayCreationExpression
                    Return TryGenerateArrayCreation(DirectCast(expression, ArrayCreationExpressionSyntax))

                Case SyntaxKind.ObjectCreationExpression
                    Return TryGenerateNewClass(DirectCast(expression, ObjectCreationExpressionSyntax))

                Case SyntaxKind.SimpleMemberAccessExpression
                    Return TryGenerateNameRef(DirectCast(expression, MemberAccessExpressionSyntax))

                Case SyntaxKind.IdentifierName
                    Return TryGenerateNameRef(DirectCast(expression, IdentifierNameSyntax))

                Case SyntaxKind.InvocationExpression
                    Return TryGenerateMethodCall(DirectCast(expression, InvocationExpressionSyntax))

                Case SyntaxKind.GetTypeExpression
                    Return TryGenerateGetTypeExpression(DirectCast(expression, GetTypeExpressionSyntax))

                Case SyntaxKind.CTypeExpression
                    Return TryGenerateCTypeExpression(DirectCast(expression, CTypeExpressionSyntax))

                Case SyntaxKind.PredefinedCastExpression
                    Return TryGeneratePredefinedCastExpression(DirectCast(expression, PredefinedCastExpressionSyntax))

                Case SyntaxKind.MeExpression
                    GenerateThisReference()
                    Return True
            End Select

            Return False
        End Function

        Private Function TryGenerateLiteral(expression As ExpressionSyntax) As Boolean
            Using LiteralTag()
                Dim constantValue = SemanticModel.GetConstantValue(expression)
                If Not constantValue.HasValue Then
                    Return False
                End If

                Dim type = SemanticModel.GetTypeInfo(expression).Type
                If type Is Nothing Then
                    Return False
                End If

                Select Case expression.Kind
                    Case SyntaxKind.UnaryMinusExpression,
                         SyntaxKind.NumericLiteralExpression
                        GenerateNumber(constantValue.Value, type)
                        Return True

                    Case SyntaxKind.CharacterLiteralExpression
                        GenerateChar(CChar(constantValue.Value))
                        Return True

                    Case SyntaxKind.StringLiteralExpression
                        GenerateString(CStr(constantValue.Value))
                        Return True

                    Case SyntaxKind.TrueLiteralExpression,
                         SyntaxKind.FalseLiteralExpression
                        GenerateBoolean(CBool(constantValue.Value))
                        Return True
                End Select
            End Using

            Return False
        End Function

        Private Function TryGenerateParenthesizedExpression(parenthesizedExpression As ParenthesizedExpressionSyntax) As Boolean
            Using ParenthesesTag()
                Return TryGenerateExpression(parenthesizedExpression.Expression)
            End Using
        End Function

        Private Function TryGenerateBinaryOperation(binaryExpression As BinaryExpressionSyntax) As Boolean
            Dim kind As BinaryOperatorKind
            Select Case binaryExpression.Kind
                Case SyntaxKind.AddExpression
                    kind = BinaryOperatorKind.Plus
                Case SyntaxKind.OrExpression
                    kind = BinaryOperatorKind.BitwiseOr
                Case SyntaxKind.AndExpression
                    kind = BinaryOperatorKind.BitwiseAnd
                Case SyntaxKind.ConcatenateExpression
                    kind = BinaryOperatorKind.Concatenate
                Case Else
                    Return False
            End Select

            Using BinaryOperationTag(kind)
                Return TryGenerateExpression(binaryExpression.Left) AndAlso
                       TryGenerateExpression(binaryExpression.Right)
            End Using
        End Function

        Private Function TryGenerateNewClass(objectCreationExpression As ObjectCreationExpressionSyntax) As Boolean
            Dim type = TryCast(SemanticModel.GetSymbolInfo(objectCreationExpression.Type).Symbol, ITypeSymbol)
            If type Is Nothing Then
                Return False
            End If

            Using NewClassTag()
                GenerateType(type)

                If objectCreationExpression.ArgumentList IsNot Nothing Then
                    For Each argument In objectCreationExpression.ArgumentList.Arguments
                        If Not TryGenerateArgument(argument) Then
                            Return False
                        End If
                    Next
                End If
            End Using

            Return True
        End Function

        Private Function TryGenerateArgument(argument As ArgumentSyntax) As Boolean
            Using ArgumentTag()
                Return TryGenerateExpression(argument.GetArgumentExpression())
            End Using
        End Function

        Protected Overrides Function GetVariableKind(symbol As ISymbol) As VariableKind
            Dim kind = MyBase.GetVariableKind(symbol)

            ' need to special case WithEvent properties. CodeModel wants them as fields.
            If symbol?.Kind = SymbolKind.Property Then
                Dim propertySymbol = TryCast(symbol, IPropertySymbol)
                If propertySymbol IsNot Nothing AndAlso propertySymbol.IsWithEvents Then
                    kind = VariableKind.Field
                End If
            End If

            Return kind
        End Function

        Private Function TryGenerateNameRef(memberAccessExpression As MemberAccessExpressionSyntax) As Boolean
            Dim symbol = SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol

            ' No null check for 'symbol' here. If 'symbol' unknown, we'll
            ' generate an "unknown" name ref.

            Dim kind As VariableKind = GetVariableKind(symbol)

            Using NameRefTag(kind)
                Dim leftHandSymbol = SemanticModel.GetSymbolInfo(memberAccessExpression.GetExpressionOfMemberAccessExpression()).Symbol
                If leftHandSymbol IsNot Nothing Then
                    If leftHandSymbol.Kind = SymbolKind.Alias Then
                        leftHandSymbol = DirectCast(leftHandSymbol, IAliasSymbol).Target
                    End If

                    ' This can occur if a module member is referenced without the module name. In that case,
                    ' we'll go ahead and try to use the module name name.
                    If leftHandSymbol.Kind = SymbolKind.Namespace AndAlso
                       symbol.ContainingType IsNot Nothing AndAlso
                       symbol.ContainingType.TypeKind = TypeKind.Module Then

                        leftHandSymbol = symbol.ContainingType
                    End If
                End If

                ' If the left-hand side is a named type, we generate a literal expression
                ' with the type name. Otherwise, we generate the expression normally.
                If leftHandSymbol IsNot Nothing AndAlso leftHandSymbol.Kind = SymbolKind.NamedType Then
                    Using ExpressionTag()
                        Using LiteralTag()
                            GenerateType(DirectCast(leftHandSymbol, ITypeSymbol))
                        End Using
                    End Using
                ElseIf Not TryGenerateExpression(memberAccessExpression.GetExpressionOfMemberAccessExpression()) Then
                    Return False
                End If

                GenerateName(memberAccessExpression.Name.Identifier.ValueText)
            End Using

            Return True
        End Function

        Private Function TryGenerateNameRef(identifierName As IdentifierNameSyntax) As Boolean
            Dim symbol = SemanticModel.GetSymbolInfo(identifierName).Symbol

            ' No null check for 'symbol' here. If 'symbol' unknown, we'll
            ' generate an "unknown" name ref.

            Dim varKind = GetVariableKind(symbol)

            Using NameRefTag(varKind)
                If symbol IsNot Nothing AndAlso varKind <> VariableKind.Local Then
                    ' This is a little tricky -- if our method symbol's containing type inherits from
                    ' or is the same as the identifier symbol's containing type, we'll go ahead and
                    ' generate a <ThisReference />. Otherwise, because this is an identifier, we assume
                    ' that it is a Shared member or an imported type (for example, a Module) and generate
                    ' an <Expression><Literal><Type/></Literal></Expression>
                    If Me.Symbol.ContainingType.InheritsFromOrEquals(symbol.ContainingType) Then
                        Using ExpressionTag()
                            GenerateThisReference()
                        End Using
                    Else
                        Using ExpressionTag()
                            Using LiteralTag()
                                GenerateType(symbol.ContainingType)
                            End Using
                        End Using
                    End If
                End If

                GenerateName(identifierName.ToString())
            End Using

            Return True
        End Function

        Private Function TryGenerateMethodCall(invocationExpression As InvocationExpressionSyntax) As Boolean
            Using MethodCallTag()
                If Not TryGenerateExpression(invocationExpression.Expression) Then
                    Return False
                End If

                If invocationExpression.ArgumentList IsNot Nothing Then
                    For Each argument In invocationExpression.ArgumentList.Arguments
                        If Not TryGenerateArgument(argument) Then
                            Return False
                        End If
                    Next
                End If
            End Using

            Return True
        End Function

        Private Function TryGenerateGetTypeExpression(getTypeExpression As GetTypeExpressionSyntax) As Boolean
            If getTypeExpression.Type Is Nothing Then
                Return False
            End If

            Dim type = SemanticModel.GetTypeInfo(getTypeExpression.Type).Type
            If type Is Nothing Then
                Return False
            End If

            GenerateType(type)

            Return True
        End Function

        Private Function TryGenerateCast(type As ITypeSymbol, expression As ExpressionSyntax) As Boolean
            Using (CastTag())
                GenerateType(type)

                Return TryGenerateExpression(expression)
            End Using
        End Function

        Private Function TryGenerateCTypeExpression(ctypeExpression As CTypeExpressionSyntax) As Boolean
            If ctypeExpression.Type Is Nothing Then
                Return False
            End If

            Dim type = SemanticModel.GetTypeInfo(ctypeExpression.Type).Type
            If type Is Nothing Then
                Return False
            End If

            If Not TryGenerateCast(type, ctypeExpression.Expression) Then
                Return False
            End If

            Return True
        End Function

        Private Function TryGeneratePredefinedCastExpression(predefinedCastExpression As PredefinedCastExpressionSyntax) As Boolean
            Dim type = GetTypeFromPredefinedCastKeyword(SemanticModel.Compilation, predefinedCastExpression.Keyword.Kind)
            If type Is Nothing Then
                Return False
            End If

            If Not TryGenerateCast(type, predefinedCastExpression.Expression) Then
                Return False
            End If

            Return True
        End Function

        Private Function TryGenerateConstantArrayBound(expression As ExpressionSyntax) As Boolean
            Dim constantValue = SemanticModel.GetConstantValue(expression)

            If Not constantValue.HasValue Then
                Return False
            End If

            If Not TypeOf constantValue.Value Is Integer Then
                Return False
            End If

            Dim upperBound = CInt(constantValue.Value) + 1

            Using ExpressionTag()
                Using LiteralTag()
                    GenerateNumber(upperBound, SemanticModel.Compilation.GetSpecialType(SpecialType.System_Int32))
                End Using
            End Using

            Return True
        End Function

        Private Function TryGenerateSimpleArrayBound(argument As ArgumentSyntax) As Boolean
            If Not TypeOf argument Is SimpleArgumentSyntax Then
                Return False
            End If

            Return TryGenerateConstantArrayBound(DirectCast(argument, SimpleArgumentSyntax).Expression)
        End Function

        Private Function TryGenerateRangeArrayBound(argument As ArgumentSyntax) As Boolean
            If Not TypeOf argument Is RangeArgumentSyntax Then
                Return False
            End If

            Return TryGenerateConstantArrayBound(DirectCast(argument, RangeArgumentSyntax).UpperBound)
        End Function

        Private Function TryGenerateCollectionInitializer(collectionInitializer As CollectionInitializerSyntax) As Boolean
            For Each initializer In collectionInitializer.Initializers
                If Not TryGenerateExpression(initializer) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Function TryGenerateNewArray(arrayBounds As ArgumentListSyntax, initializer As ExpressionSyntax, type As ITypeSymbol) As Boolean
            Using NewArrayTag()
                GenerateType(type)

                If arrayBounds IsNot Nothing Then

                    If Not TryGenerateArrayBounds(arrayBounds, type) Then
                        Return False
                    End If

                    If initializer IsNot Nothing AndAlso initializer.Kind = SyntaxKind.CollectionInitializer Then
                        If Not TryGenerateCollectionInitializer(DirectCast(initializer, CollectionInitializerSyntax)) Then
                            Return False
                        End If
                    End If

                Else
                    ' No array bounds...

                    If initializer IsNot Nothing Then
                        If type.TypeKind = TypeKind.Array Then
                            Select Case initializer.Kind
                                Case SyntaxKind.ArrayCreationExpression
                                    If Not TryGenerateArrayCreation(DirectCast(initializer, ArrayCreationExpressionSyntax)) Then
                                        Return False
                                    End If
                                Case SyntaxKind.CollectionInitializer
                                    If Not TryGenerateArrayInitializer(DirectCast(initializer, CollectionInitializerSyntax), type) Then
                                        Return False
                                    End If
                                Case Else
                                    Return False
                            End Select
                        ElseIf Not TryGenerateExpression(initializer) Then
                            Return False
                        End If
                    End If

                End If

                Return True
            End Using
        End Function

        Private Function TryGenerateArrayBounds(argumentList As ArgumentListSyntax, type As ITypeSymbol) As Boolean
            For Each argument In argumentList.Arguments
                Using BoundTag()
                    If Not TryGenerateSimpleArrayBound(argument) AndAlso
                       Not TryGenerateRangeArrayBound(argument) Then
                        Return False
                    End If
                End Using
            Next

            Return True
        End Function

        Private Function TryGenerateArrayInitializer(collectionInitializer As CollectionInitializerSyntax, type As ITypeSymbol) As Boolean
            Using BoundTag()
                Using ExpressionTag()
                    Using LiteralTag()
                        Using NumberTag()
                            EncodedText(collectionInitializer.Initializers.Count.ToString())
                        End Using
                    End Using
                End Using
            End Using

            Return TryGenerateCollectionInitializer(collectionInitializer)
        End Function

        Private Function TryGenerateArrayCreation(arrayCreationExpression As ArrayCreationExpressionSyntax) As Boolean
            Dim type = SemanticModel.GetTypeInfo(arrayCreationExpression).Type
            If type Is Nothing Then
                Return False
            End If

            If Not TryGenerateNewArray(arrayCreationExpression.ArrayBounds, arrayCreationExpression.Initializer, type) Then
                Return False
            End If

            Return True
        End Function

        Public Shared Function Generate(methodBlock As MethodBlockBaseSyntax, semanticModel As SemanticModel) As String
            Dim symbol = DirectCast(semanticModel.GetDeclaredSymbol(methodBlock), IMethodSymbol)
            Dim builder = New MethodXmlBuilder(symbol, semanticModel)

            builder.GenerateStatementBlock(methodBlock.Statements)

            Return builder.ToString()
        End Function

    End Class
End Namespace
