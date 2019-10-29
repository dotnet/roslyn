' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Friend Class VisualBasicSelectionResult
        Inherits SelectionResult

        Public Shared Async Function CreateResultAsync(
            status As OperationStatus,
            originalSpan As TextSpan,
            finalSpan As TextSpan,
            options As OptionSet,
            selectionInExpression As Boolean,
            document As SemanticDocument,
            firstToken As SyntaxToken,
            lastToken As SyntaxToken,
            cancellationToken As CancellationToken) As Task(Of VisualBasicSelectionResult)

            Contract.ThrowIfNull(document)

            Dim firstAnnotation = New SyntaxAnnotation()
            Dim lastAnnotation = New SyntaxAnnotation()

            Dim root = document.Root
            Dim newDocument = Await SemanticDocument.CreateAsync(document.Document.WithSyntaxRoot(root.AddAnnotations(
                        {Tuple.Create(Of SyntaxToken, SyntaxAnnotation)(firstToken, firstAnnotation),
                         Tuple.Create(Of SyntaxToken, SyntaxAnnotation)(lastToken, lastAnnotation)})), cancellationToken).ConfigureAwait(False)

            Return New VisualBasicSelectionResult(
                status,
                originalSpan,
                finalSpan,
                options,
                selectionInExpression,
                newDocument,
                firstAnnotation,
                lastAnnotation)
        End Function

        Private Sub New(
            status As OperationStatus,
            originalSpan As TextSpan,
            finalSpan As TextSpan,
            options As OptionSet,
            selectionInExpression As Boolean,
            document As SemanticDocument,
            firstTokenAnnotation As SyntaxAnnotation,
            lastTokenAnnotation As SyntaxAnnotation)

            MyBase.New(
                status,
                originalSpan,
                finalSpan,
                options,
                selectionInExpression,
                document,
                firstTokenAnnotation,
                lastTokenAnnotation)
        End Sub

        Protected Overrides Function UnderAsyncAnonymousMethod(token As SyntaxToken, firstToken As SyntaxToken, lastToken As SyntaxToken) As Boolean
            Dim current = token.Parent

            While current IsNot Nothing
                If TypeOf current Is DeclarationStatementSyntax OrElse
                   TypeOf current Is LambdaExpressionSyntax Then
                    Exit While
                End If

                current = current.Parent
            End While

            If current Is Nothing OrElse TypeOf current Is DeclarationStatementSyntax Then
                Return False
            End If

            ' make sure selection contains the lambda
            Return firstToken.SpanStart <= current.GetFirstToken().SpanStart AndAlso
                   current.GetLastToken().Span.End <= lastToken.Span.End
        End Function

        Public Overrides Function ContainingScopeHasAsyncKeyword() As Boolean
            If SelectionInExpression Then
                Return False
            End If

            Dim node = CType(Me.GetContainingScope(), SyntaxNode)
            If TypeOf node Is MethodBlockBaseSyntax Then
                Dim methodBlock = DirectCast(node, MethodBlockBaseSyntax)
                If methodBlock.BlockStatement IsNot Nothing Then
                    Return methodBlock.BlockStatement.Modifiers.Any(SyntaxKind.AsyncKeyword)
                End If

                Return False
            ElseIf TypeOf node Is LambdaExpressionSyntax Then
                Dim lambda = DirectCast(node, LambdaExpressionSyntax)
                If lambda.SubOrFunctionHeader IsNot Nothing Then
                    Return lambda.SubOrFunctionHeader.Modifiers.Any(SyntaxKind.AsyncKeyword)
                End If
            End If

            Return False
        End Function

        Public Overrides Function GetContainingScope() As SyntaxNode
            Contract.ThrowIfNull(Me.SemanticDocument)

            Dim first = GetFirstTokenInSelection()

            If SelectionInExpression Then
                Dim last = GetLastTokenInSelection()

                Return first.GetCommonRoot(last).GetAncestorOrThis(Of ExpressionSyntax)()
            End If

            ' it contains statements
            Return first.GetAncestors(Of SyntaxNode).FirstOrDefault(Function(n) TypeOf n Is MethodBlockBaseSyntax OrElse TypeOf n Is LambdaExpressionSyntax)
        End Function

        Public Overrides Function GetContainingScopeType() As ITypeSymbol
            Dim node = CType(Me.GetContainingScope(), SyntaxNode)
            Dim semanticModel = Me.SemanticDocument.SemanticModel

            ' special case for collection initializer and explicit cast
            If node.IsExpressionInCast() Then
                Dim castExpression = TryCast(node.Parent, CastExpressionSyntax)
                If castExpression IsNot Nothing Then
                    Return semanticModel.GetTypeInfo(castExpression.Type).Type
                End If
            End If

            Dim expression As ExpressionSyntax = Nothing
            If TypeOf node Is CollectionInitializerSyntax Then
                expression = node.GetUnparenthesizedExpression()
                Return semanticModel.GetTypeInfo(expression).ConvertedType
            End If

            Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
            If methodBlock IsNot Nothing Then
                Dim symbol = semanticModel.GetDeclaredSymbol(methodBlock.BlockStatement)
                Dim propertySymbol = TryCast(symbol, IPropertySymbol)
                If propertySymbol IsNot Nothing Then
                    Return propertySymbol.Type
                Else
                    Return DirectCast(symbol, IMethodSymbol).ReturnType
                End If
            End If

            Dim info As TypeInfo
            Dim lambda = TryCast(node, LambdaExpressionSyntax)
            If lambda IsNot Nothing Then
                If SelectionInExpression Then
                    info = semanticModel.GetTypeInfo(lambda)
                    Return If(info.Type.IsObjectType(), info.ConvertedType, info.Type)
                Else
                    Return semanticModel.GetLambdaOrAnonymousMethodReturnType(lambda)
                End If
            End If

            expression = DirectCast(node, ExpressionSyntax)
            ' regular case. always use ConvertedType to get implicit conversion right.
            expression = expression.GetUnparenthesizedExpression()

            info = semanticModel.GetTypeInfo(expression)
            If info.ConvertedType IsNot Nothing AndAlso
                    Not info.ConvertedType.IsErrorType() Then
                If expression.Kind = SyntaxKind.AddressOfExpression Then
                    Return info.ConvertedType
                End If

                Dim conversion = semanticModel.ClassifyConversion(expression, info.ConvertedType)
                If conversion.IsNumeric AndAlso conversion.IsWidening Then
                    Return info.ConvertedType
                End If

                Dim conv = semanticModel.GetConversion(expression)
                If IsCoClassImplicitConversion(info, conv, semanticModel.Compilation.CoClassType()) Then
                    Return info.ConvertedType
                End If
            End If

            ' use FormattableString if conversion between String And FormattableString
            If (info.Type?.SpecialType = SpecialType.System_String).GetValueOrDefault() AndAlso
               info.ConvertedType?.IsFormattableString() Then

                Return info.ConvertedType
            End If

            ' get type without considering implicit conversion
            Return If(info.Type.IsObjectType(), info.ConvertedType, info.Type)
        End Function

        Private Shared Function IsCoClassImplicitConversion(info As TypeInfo, conversion As Conversion, coclassSymbol As ISymbol) As Boolean
            If Not conversion.IsWidening OrElse
                 info.ConvertedType Is Nothing OrElse
                 info.ConvertedType.TypeKind <> TypeKind.Interface Then
                Return False
            End If

            ' let's see whether this interface has coclass attribute
            Return info.ConvertedType.GetAttributes().Any(Function(c) c.AttributeClass.Equals(coclassSymbol))
        End Function

        Public Overloads Function GetFirstStatement() As ExecutableStatementSyntax
            Return GetFirstStatement(Of ExecutableStatementSyntax)()
        End Function

        Public Overloads Function GetLastStatement() As ExecutableStatementSyntax
            Return GetLastStatement(Of ExecutableStatementSyntax)()
        End Function

        Public Function GetFirstStatementUnderContainer() As ExecutableStatementSyntax
            Contract.ThrowIfTrue(SelectionInExpression)

            Dim firstToken = GetFirstTokenInSelection()
            Dim lastToken = GetLastTokenInSelection()
            Dim commonRoot = firstToken.GetCommonRoot(lastToken)

            Dim statement As ExecutableStatementSyntax
            If commonRoot.IsStatementContainerNode() Then
                Dim firstStatement = GetFirstStatement()
                statement = firstStatement.GetAncestorsOrThis(Of ExecutableStatementSyntax) _
                                          .SkipWhile(Function(s) s.Parent IsNot commonRoot) _
                                          .First()
                If statement.Parent.ContainStatement(statement) Then
                    Return statement
                End If
            End If

            statement = commonRoot.GetStatementUnderContainer()
            Contract.ThrowIfNull(statement)

            Return statement
        End Function

        Public Function GetLastStatementUnderContainer() As ExecutableStatementSyntax
            Contract.ThrowIfTrue(SelectionInExpression)

            Dim firstStatement = GetFirstStatementUnderContainer()
            Dim container = firstStatement.GetStatementContainer()

            Dim lastStatement = Me.GetLastStatement().GetAncestorsOrThis(Of ExecutableStatementSyntax) _
                                                .SkipWhile(Function(s) s.Parent IsNot container) _
                                                .First()

            Contract.ThrowIfNull(lastStatement)
            Contract.ThrowIfFalse(lastStatement.Parent Is (GetFirstStatementUnderContainer()).Parent)

            Return lastStatement
        End Function

        Public Function InnermostStatementContainer() As SyntaxNode
            Contract.ThrowIfFalse(SelectionInExpression)

            Dim containingScope = GetContainingScope()
            Dim statementContainer =
                containingScope.Parent _
                               .GetAncestorsOrThis(Of SyntaxNode)() _
                               .FirstOrDefault(Function(n) n.IsStatementContainerNode)

            If statementContainer IsNot Nothing Then
                Return statementContainer
            End If

            Dim field = containingScope.GetAncestor(Of FieldDeclarationSyntax)()
            If field IsNot Nothing Then
                Return field.Parent
            End If

            Dim [property] = containingScope.GetAncestor(Of PropertyStatementSyntax)()
            If [property] IsNot Nothing Then
                Return [property].Parent
            End If

            ' no repl yet
            ' Contract.ThrowIfFalse(last.IsParentKind(SyntaxKind.GlobalStatement))
            ' Contract.ThrowIfFalse(last.Parent.IsParentKind(SyntaxKind.CompilationUnit))
            ' Return last.Parent.Parent
            Return Contract.FailWithReturn(Of SyntaxNode)("No REPL yet")
        End Function

        Public Function IsUnderModuleBlock() As Boolean
            Dim currentScope = CType(GetContainingScope(), SyntaxNode)
            Dim types = currentScope.GetAncestors(Of TypeBlockSyntax)()

            Return types.Any(Function(t) t.BlockStatement.Kind = SyntaxKind.ModuleStatement)
        End Function

        Public Function ContainsInstanceExpression() As Boolean
            Dim first = GetFirstTokenInSelection()
            Dim last = GetLastTokenInSelection()
            Dim node = first.GetCommonRoot(last)

            Return node.DescendantNodesAndSelf(
                TextSpan.FromBounds(first.SpanStart, last.Span.End)) _
                                       .Any(Function(n) TypeOf n Is InstanceExpressionSyntax)
        End Function
    End Class
End Namespace
