' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.CodeCleanup
    Friend Module AsyncOrIteratorFunctionReturnTypeFixer

        Public Function RewriteMethodStatement(func As MethodStatementSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As MethodStatementSyntax
            Return RewriteMethodStatement(func, semanticModel, oldFunc:=func, cancellationToken:=cancellationToken)
        End Function

        Public Function RewriteMethodStatement(func As MethodStatementSyntax, semanticModel As SemanticModel, oldFunc As MethodStatementSyntax, cancellationToken As CancellationToken) As MethodStatementSyntax
            If func.DeclarationKeyword.Kind = SyntaxKind.FunctionKeyword Then

                Dim modifiers = func.Modifiers
                Dim parameterListOpt = func.ParameterList
                Dim asClauseOpt = func.AsClause
                Dim oldAsClauseOpt = oldFunc.AsClause
                Dim position = If(oldFunc.ParameterList IsNot Nothing, oldFunc.ParameterList.SpanStart, oldFunc.Identifier.SpanStart)

                If RewriteFunctionStatement(modifiers, oldAsClauseOpt, parameterListOpt, asClauseOpt, semanticModel, position, cancellationToken) Then
                    Return func.Update(func.Kind, func.AttributeLists, func.Modifiers, func.DeclarationKeyword, func.Identifier,
                                   func.TypeParameterList, parameterListOpt, asClauseOpt, func.HandlesClause, func.ImplementsClause)
                End If
            End If

            Return func
        End Function

        Public Function RewriteLambdaHeader(lambdaHeader As LambdaHeaderSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As LambdaHeaderSyntax
            Return RewriteLambdaHeader(lambdaHeader, semanticModel, oldLambdaHeader:=lambdaHeader, cancellationToken:=cancellationToken)
        End Function

        Public Function RewriteLambdaHeader(lambdaHeader As LambdaHeaderSyntax, semanticModel As SemanticModel, oldLambdaHeader As LambdaHeaderSyntax, cancellationToken As CancellationToken) As LambdaHeaderSyntax
            If lambdaHeader.DeclarationKeyword.Kind = SyntaxKind.FunctionKeyword AndAlso
               lambdaHeader.AsClause IsNot Nothing AndAlso
               lambdaHeader.ParameterList IsNot Nothing Then

                Dim parameterList = lambdaHeader.ParameterList
                Dim asClause = lambdaHeader.AsClause
                If RewriteFunctionStatement(lambdaHeader.Modifiers, oldLambdaHeader.AsClause, parameterList, asClause, semanticModel, oldLambdaHeader.AsClause.SpanStart, cancellationToken) Then
                    Return lambdaHeader.Update(lambdaHeader.Kind, lambdaHeader.AttributeLists, lambdaHeader.Modifiers, lambdaHeader.DeclarationKeyword, parameterList, asClause)
                End If
            End If

            Return lambdaHeader
        End Function

        Private Function RewriteFunctionStatement(modifiers As SyntaxTokenList,
                                                  oldAsClauseOpt As AsClauseSyntax,
                                                  ByRef parameterListOpt As ParameterListSyntax,
                                                  ByRef asClauseOpt As SimpleAsClauseSyntax,
                                                  semanticModel As SemanticModel,
                                                  position As Integer,
                                                  cancellationToken As CancellationToken) As Boolean

            ' Pretty list Async and Iterator functions without an AsClause or an incorrect return type as follows:
            '   1) Without an AsClause: Async functions are pretty listed to return Task and Iterator functions to return IEnumerable.
            '   2) With an AsClause: If the return type R is a valid non-error type, pretty list as follows:
            '       (a) Async functions: If R is not Task or Task(Of T) or an instantiation of Task(Of T), pretty list return type to Task(Of R).
            '       (b) Iterator functions: If R is not IEnumerable/IEnumerator or IEnumerable(Of T)/IEnumerator(Of T) or an instantiation
            '           of these generic types, pretty list return type to IEnumerable(Of R).

            If modifiers.Any(SyntaxKind.AsyncKeyword) Then
                Return RewriteAsyncFunction(oldAsClauseOpt, parameterListOpt, asClauseOpt, semanticModel, position, cancellationToken)
            ElseIf modifiers.Any(SyntaxKind.IteratorKeyword) Then
                Return RewriteIteratorFunction(oldAsClauseOpt, parameterListOpt, asClauseOpt, semanticModel, position, cancellationToken)
            Else
                Return False
            End If
        End Function

        Private Function RewriteAsyncFunction(oldAsClauseOpt As AsClauseSyntax,
                                                     ByRef parameterListOpt As ParameterListSyntax,
                                                     ByRef asClauseOpt As SimpleAsClauseSyntax,
                                              semanticModel As SemanticModel,
                                              position As Integer,
                                                     cancellationToken As CancellationToken) As Boolean
            If semanticModel Is Nothing Then
                Return False
            End If

            Dim taskType = semanticModel.Compilation.GetTypeByMetadataName(GetType(Task).FullName)
            If asClauseOpt Is Nothing Then
                ' Without an AsClause: Async functions are pretty listed to return Task.
                If taskType IsNot Nothing AndAlso parameterListOpt IsNot Nothing Then
                    GenerateFunctionAsClause(taskType, parameterListOpt, asClauseOpt, semanticModel, position)
                    Return True
                End If
            Else
                '   2) With an AsClause: If the return type R is a valid non-error type, pretty list as follows:
                '       (a) Async functions: If R is not Task or Task(Of T) or an instantiation of Task(Of T), pretty list return type to Task(Of R).
                Dim taskOfT = semanticModel.Compilation.GetTypeByMetadataName(GetType(Task(Of)).FullName)
                Dim returnType = semanticModel.GetTypeInfo(oldAsClauseOpt.Type, cancellationToken).Type
                If returnType IsNot Nothing AndAlso Not returnType.IsErrorType() AndAlso
                    taskType IsNot Nothing AndAlso taskOfT IsNot Nothing AndAlso
                    Not returnType.Equals(taskType) AndAlso Not returnType.OriginalDefinition.Equals(taskOfT) Then
                    RewriteFunctionAsClause(taskOfT.Construct(returnType), asClauseOpt, semanticModel, position)
                    Return True
                End If
            End If
            Return False
        End Function

        Private Function RewriteIteratorFunction(oldAsClauseOpt As AsClauseSyntax,
                                                 ByRef parameterListOpt As ParameterListSyntax,
                                                 ByRef asClauseOpt As SimpleAsClauseSyntax,
                                                 semanticModel As SemanticModel,
                                                 position As Integer,
                                                 cancellationToken As CancellationToken) As Boolean
            If semanticModel Is Nothing Then
                Return False
            End If

            If asClauseOpt Is Nothing Then
                ' Without an AsClause: Iterator functions are pretty listed to return IEnumerable.
                Dim iEnumerableType = semanticModel.Compilation.GetTypeByMetadataName(GetType(IEnumerable).FullName)
                If iEnumerableType IsNot Nothing Then
                    GenerateFunctionAsClause(iEnumerableType, parameterListOpt, asClauseOpt, semanticModel, position)
                    Return True
                End If
            Else
                '   2) With an AsClause: If the return type R is a valid non-error type, pretty list as follows:
                '       (b) Iterator functions: If R is not IEnumerable/IEnumerator or IEnumerable(Of T)/IEnumerator(Of T) or an instantiation
                '           of these generic types, pretty list return type to IEnumerable(Of R).
                Dim returnType = semanticModel.GetTypeInfo(oldAsClauseOpt.Type, cancellationToken).Type
                If returnType IsNot Nothing AndAlso Not returnType.IsErrorType() Then
                    Select Case returnType.OriginalDefinition.SpecialType
                        Case SpecialType.System_Collections_IEnumerable, SpecialType.System_Collections_IEnumerator,
                            SpecialType.System_Collections_Generic_IEnumerable_T, SpecialType.System_Collections_Generic_IEnumerator_T
                            Return False
                        Case Else
                            Dim iEnumerableOfT = semanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
                            If iEnumerableOfT IsNot Nothing Then
                                RewriteFunctionAsClause(iEnumerableOfT.Construct(returnType), asClauseOpt, semanticModel, position)
                                Return True
                            End If
                    End Select
                End If
            End If
            Return False
        End Function

        ' Pretty list the function without an AsClause to return type "T"
        Private Sub GenerateFunctionAsClause(type As ITypeSymbol,
                                             ByRef parameterListOpt As ParameterListSyntax,
                                             ByRef asClauseOpt As SimpleAsClauseSyntax,
                                             semanticModel As SemanticModel,
                                             position As Integer)
            Debug.Assert(type IsNot Nothing)
            Debug.Assert(parameterListOpt IsNot Nothing)
            Debug.Assert(asClauseOpt Is Nothing)
            Debug.Assert(semanticModel IsNot Nothing)

            Dim typeSyntax = SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(semanticModel, position))
            asClauseOpt = SyntaxFactory.SimpleAsClause(typeSyntax).NormalizeWhitespace()

            Dim closeParenToken = parameterListOpt.CloseParenToken
            If Not closeParenToken.HasTrailingTrivia Then
                ' Add trailing whitespace trivia to close paren token.
                closeParenToken = closeParenToken.WithTrailingTrivia(SyntaxFactory.WhitespaceTrivia(" "))
            Else
                ' Add trailing whitespace trivia to close paren token and move it's current trailing trivia to asClause.
                Dim closeParenTrailingTrivia = closeParenToken.TrailingTrivia
                asClauseOpt = asClauseOpt.WithTrailingTrivia(closeParenTrailingTrivia)
                closeParenToken = closeParenToken.WithTrailingTrivia(SyntaxFactory.WhitespaceTrivia(" "))
            End If

            parameterListOpt = parameterListOpt.WithCloseParenToken(closeParenToken)
        End Sub

        ' Pretty list the function with return type "T" to return "genericTypeName(Of T)"
        Private Sub RewriteFunctionAsClause(genericType As INamedTypeSymbol, ByRef asClauseOpt As SimpleAsClauseSyntax, semanticModel As SemanticModel, position As Integer)
            Debug.Assert(genericType.IsGenericType)
            Debug.Assert(asClauseOpt IsNot Nothing AndAlso Not asClauseOpt.IsMissing)
            Debug.Assert(semanticModel IsNot Nothing)

            ' Move the leading and trailing trivia from the existing typeSyntax node to the new AsClause.
            Dim typeSyntax = asClauseOpt.Type
            Dim leadingTrivia = typeSyntax.GetLeadingTrivia()
            Dim trailingTrivia = typeSyntax.GetTrailingTrivia()
            Dim newTypeSyntax = SyntaxFactory.ParseTypeName(genericType.ToMinimalDisplayString(semanticModel, position))

            ' Replace the generic type argument with the original type name syntax. We need this for couple of scenarios:
            '   (a) Original type symbol binds to an alias symbol. Generic type argument would be the alias symbol's target type, but we want to retain the alias name.
            '   (b) Original type syntax is a non-simplified name, we don't want to replace it with a simplified name.
            Dim genericName As GenericNameSyntax
            If newTypeSyntax.Kind = SyntaxKind.QualifiedName Then
                genericName = DirectCast(DirectCast(newTypeSyntax, QualifiedNameSyntax).Right, GenericNameSyntax)
            Else
                genericName = DirectCast(newTypeSyntax, GenericNameSyntax)
            End If

            Dim currentTypeArgument = genericName.TypeArgumentList.Arguments.First
            Dim newTypeArgument = typeSyntax _
                                  .WithoutLeadingTrivia() _
                                  .WithoutTrailingTrivia()

            newTypeSyntax = newTypeSyntax.ReplaceNode(currentTypeArgument, newTypeArgument) _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia)

            asClauseOpt = asClauseOpt.WithType(newTypeSyntax)
        End Sub
    End Module
End Namespace
