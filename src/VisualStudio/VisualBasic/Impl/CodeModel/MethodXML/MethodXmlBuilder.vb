' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.MethodXml

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.MethodXml
    Friend Class MethodXmlBuilder
        Inherits AbstractMethodXmlBuilder

        Private Shared ReadOnly s_fullNameFormat As New SymbolDisplayFormat(
            typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            memberOptions:=SymbolDisplayMemberOptions.None)

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
                Case SyntaxKind.AddHandlerStatement,
                     SyntaxKind.RemoveHandlerStatement
                    success = TryGenerateAddOrRemoveHandlerStatement(DirectCast(statement, AddRemoveHandlerStatementSyntax))
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

        Private Function TryGenerateAddOrRemoveHandlerStatement(addHandlerStatement As AddRemoveHandlerStatementSyntax) As Boolean
            ' AddHandler statements are represented as invocations of an event's add_* method.
            ' RemoveHandler statements are represented as invocations of an event's remove_* method.

            Dim eventExpression = addHandlerStatement.EventExpression
            Dim eventSymbol = TryCast(SemanticModel.GetSymbolInfo(eventExpression).Symbol, IEventSymbol)

            Dim eventAccessor As IMethodSymbol
            If addHandlerStatement.Kind() = SyntaxKind.AddHandlerStatement Then
                eventAccessor = eventSymbol?.AddMethod
            ElseIf addHandlerStatement.Kind() = SyntaxKind.RemoveHandlerStatement
                eventAccessor = eventSymbol?.RemoveMethod
            Else
                eventAccessor = Nothing
            End If

            If eventAccessor Is Nothing Then
                Return False
            End If

            Using ExpressionStatementTag(GetLineNumber(addHandlerStatement))
                Using ExpressionTag()
                    Using MethodCallTag()
                        If Not TryGenerateExpression(eventExpression, eventAccessor, generateAttributes:=True) Then
                            Return False
                        End If

                        GenerateType(eventSymbol.ContainingType, implicit:=True, assemblyQualify:=True)

                        Using ArgumentTag()
                            If Not TryGenerateExpression(addHandlerStatement.DelegateExpression, generateAttributes:=True) Then
                                Return False
                            End If
                        End Using

                        Return True
                    End Using
                End Using
            End Using
        End Function

        Private Function TryGenerateExpression(expression As ExpressionSyntax, Optional symbolOpt As ISymbol = Nothing, Optional generateAttributes As Boolean = False) As Boolean
            Using ExpressionTag()
                Return TryGenerateExpressionSansTag(expression, symbolOpt, generateAttributes)
            End Using
        End Function

        Private Function TryGenerateExpressionSansTag(expression As ExpressionSyntax, Optional symbolOpt As ISymbol = Nothing, Optional generateAttributes As Boolean = False) As Boolean
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
                    Return TryGenerateNameRef(DirectCast(expression, MemberAccessExpressionSyntax), symbolOpt, generateAttributes)

                Case SyntaxKind.IdentifierName
                    Return TryGenerateNameRef(DirectCast(expression, IdentifierNameSyntax), symbolOpt, generateAttributes)

                Case SyntaxKind.InvocationExpression
                    Return TryGenerateMethodCall(DirectCast(expression, InvocationExpressionSyntax))

                Case SyntaxKind.GetTypeExpression
                    Return TryGenerateGetTypeExpression(DirectCast(expression, GetTypeExpressionSyntax))

                Case SyntaxKind.CTypeExpression,
                     SyntaxKind.DirectCastExpression,
                     SyntaxKind.TryCastExpression
                    Return TryGenerateCastExpression(DirectCast(expression, CastExpressionSyntax))

                Case SyntaxKind.PredefinedCastExpression
                    Return TryGeneratePredefinedCastExpression(DirectCast(expression, PredefinedCastExpressionSyntax))

                Case SyntaxKind.MeExpression
                    GenerateThisReference()
                    Return True

                Case SyntaxKind.AddressOfExpression
                    Return TryGenerateAddressOfExpression(DirectCast(expression, UnaryExpressionSyntax))
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

        Private Shared Function GetFullNameText(symbol As ISymbol) As String
            If symbol Is Nothing Then
                Return Nothing
            End If

            If symbol.IsAccessor() Then
                Return Nothing
            End If

            Return symbol.ContainingSymbol.ToDisplayString(s_fullNameFormat) & "." & symbol.Name
        End Function

        Private Function TryGenerateNameRef(
            memberAccess As MemberAccessExpressionSyntax,
            Optional symbolOpt As ISymbol = Nothing,
            Optional generateAttributes As Boolean = False
        ) As Boolean

            symbolOpt = If(symbolOpt, SemanticModel.GetSymbolInfo(memberAccess).Symbol)

            ' Note: There's no null check for 'symbolOpt' here. If 'symbolOpt' is Nothing, we'll generate an "unknown" name ref.
            Dim varKind As VariableKind = GetVariableKind(symbolOpt)

            Dim name = If(symbolOpt IsNot Nothing, symbolOpt.Name, memberAccess.Name.Identifier.ValueText)
            Dim nameAttribute = If(generateAttributes, name, Nothing)
            Dim fullNameAttribute = If(generateAttributes, GetFullNameText(symbolOpt), Nothing)

            Using NameRefTag(varKind, nameAttribute, fullNameAttribute)

                Dim leftHandSymbol = SemanticModel.GetSymbolInfo(memberAccess.GetExpressionOfMemberAccessExpression()).Symbol
                If leftHandSymbol IsNot Nothing Then
                    If leftHandSymbol.Kind = SymbolKind.Alias Then
                        leftHandSymbol = DirectCast(leftHandSymbol, IAliasSymbol).Target
                    End If

                    ' This can occur if a module member is referenced without the module name. In that case,
                    ' we'll go ahead and try to use the module namespace name.
                    If leftHandSymbol.Kind = SymbolKind.Namespace AndAlso
                       symbolOpt?.ContainingType?.TypeKind = TypeKind.Module Then

                        leftHandSymbol = symbolOpt.ContainingType
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
                ElseIf Not TryGenerateExpression(memberAccess.GetExpressionOfMemberAccessExpression(), generateAttributes:=generateAttributes) Then
                    Return False
                End If

                If Not generateAttributes Then
                    GenerateName(name)
                End If
            End Using

            Return True
        End Function

        Private Function TryGenerateNameRef(
            identifierName As IdentifierNameSyntax,
            Optional symbolOpt As ISymbol = Nothing,
            Optional generateAttributes As Boolean = False
        ) As Boolean

            symbolOpt = If(symbolOpt, SemanticModel.GetSymbolInfo(identifierName).Symbol)

            ' Note: There's no null check for 'symbolOpt' here. If 'symbolOpt' is Nothing, we'll generate an "unknown" name ref.
            Dim varKind = GetVariableKind(symbolOpt)

            Dim name = If(symbolOpt IsNot Nothing, symbolOpt.Name, identifierName.Identifier.ValueText)
            Dim nameAttribute = If(generateAttributes, name, Nothing)
            Dim fullNameAttribute = If(generateAttributes, GetFullNameText(symbolOpt), Nothing)

            Using NameRefTag(varKind, nameAttribute, fullNameAttribute)
                If symbolOpt IsNot Nothing AndAlso varKind <> VariableKind.Local Then
                    GenerateLastNameRef(symbolOpt)
                End If

                If Not generateAttributes Then
                    GenerateName(name)
                End If
            End Using

            Return True
        End Function

        Private Sub GenerateLastNameRef(symbol As ISymbol)
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
        End Sub

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

        Private Function TryGenerateCast(type As ITypeSymbol, expression As ExpressionSyntax, Optional specialCastKind As SpecialCastKind? = Nothing) As Boolean
            Using (CastTag(specialCastKind))
                GenerateType(type)

                Return TryGenerateExpression(expression)
            End Using
        End Function

        Private Function TryGenerateCastExpression(castExpression As CastExpressionSyntax) As Boolean
            If castExpression.Type Is Nothing Then
                Return False
            End If

            Dim type = SemanticModel.GetTypeInfo(castExpression.Type).Type
            If type Is Nothing Then
                Return False
            End If

            If Not TryGenerateCast(type, castExpression.Expression, GetSpecialCastKind(castExpression)) Then
                Return False
            End If

            Return True
        End Function

        Private Shared Function GetSpecialCastKind(castExpression As CastExpressionSyntax) As SpecialCastKind?
            Select Case castExpression.Kind()
                Case SyntaxKind.DirectCastExpression
                    Return SpecialCastKind.DirectCast
                Case SyntaxKind.TryCastExpression
                    Return SpecialCastKind.TryCast
                Case Else
                    Return Nothing
            End Select
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

                    If Not TryGenerateArrayBounds(arrayBounds) Then
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
                                    If Not TryGenerateArrayInitializer(DirectCast(initializer, CollectionInitializerSyntax)) Then
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

        Private Function TryGenerateArrayBounds(argumentList As ArgumentListSyntax) As Boolean
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

        Private Function TryGenerateArrayInitializer(collectionInitializer As CollectionInitializerSyntax) As Boolean
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

        Private Function TryGenerateAddressOfExpression(expression As UnaryExpressionSyntax) As Boolean
            Debug.Assert(expression.Kind() = SyntaxKind.AddressOfExpression)

            Dim delegateExpression = expression.Operand

            Dim delegateSymbol = TryCast(SemanticModel.GetSymbolInfo(delegateExpression).Symbol, IMethodSymbol)
            If delegateSymbol Is Nothing Then
                Return False
            End If

            Dim eventType = SemanticModel.GetTypeInfo(expression).Type
            If eventType Is Nothing Then
                Return False
            End If

            Using NewDelegateTag(delegateSymbol.Name)
                GenerateType(eventType, implicit:=True, assemblyQualify:=True)

                If delegateExpression.Kind() = SyntaxKind.IdentifierName Then
                    GenerateLastNameRef(delegateSymbol)
                ElseIf delegateExpression.Kind() = SyntaxKind.SimpleMemberAccessExpression
                    Dim memberAccess = DirectCast(delegateExpression, MemberAccessExpressionSyntax)
                    If Not TryGenerateExpression(memberAccess.GetExpressionOfMemberAccessExpression(), generateAttributes:=True) Then
                        Return False
                    End If
                End If

                GenerateType(delegateSymbol.ContainingType, implicit:=True, assemblyQualify:=True)

                Return True
            End Using
        End Function

        Public Shared Function Generate(methodBlock As MethodBlockBaseSyntax, semanticModel As SemanticModel) As String
            Dim symbol = semanticModel.GetDeclaredSymbol(methodBlock)
            Dim builder = New MethodXmlBuilder(symbol, semanticModel)

            builder.GenerateStatementBlock(methodBlock.Statements)

            Return builder.ToString()
        End Function

    End Class
End Namespace
