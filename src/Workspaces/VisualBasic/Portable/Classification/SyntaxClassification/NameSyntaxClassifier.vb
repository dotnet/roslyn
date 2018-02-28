' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class NameSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxNodeTypes As ImmutableArray(Of Type) = ImmutableArray.Create(
            GetType(NameSyntax),
            GetType(ModifiedIdentifierSyntax),
            GetType(MethodStatementSyntax))

        Public Overrides Sub AddClassifications(
                syntax As SyntaxNode,
                semanticModel As SemanticModel,
                result As ArrayBuilder(Of ClassifiedSpan),
                cancellationToken As CancellationToken)

            Dim nameSyntax = TryCast(syntax, NameSyntax)
            If nameSyntax IsNot Nothing Then
                ClassifyNameSyntax(nameSyntax, semanticModel, result, cancellationToken)
                Return
            End If

            Dim modifiedIdentifier = TryCast(syntax, ModifiedIdentifierSyntax)
            If modifiedIdentifier IsNot Nothing Then
                ClassifyModifiedIdentifier(modifiedIdentifier, semanticModel, result, cancellationToken)
                Return
            End If

            Dim methodStatement = TryCast(syntax, MethodStatementSyntax)
            If methodStatement IsNot Nothing Then
                ClassifyMethodStatement(methodStatement, semanticModel, result, cancellationToken)
                Return
            End If
        End Sub

        Private Sub ClassifyNameSyntax(
                node As NameSyntax,
                semanticModel As SemanticModel,
                result As ArrayBuilder(Of ClassifiedSpan),
                cancellationToken As CancellationToken)

            Dim symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken)
            Dim symbol = symbolInfo.Symbol

            If symbol Is Nothing AndAlso symbolInfo.CandidateSymbols.Length > 0 Then
                Dim firstSymbol = symbolInfo.CandidateSymbols(0)
                Select Case symbolInfo.CandidateReason
                    Case CandidateReason.NotCreatable
                        ' Not creatable types are still classified as types.
                        If firstSymbol.IsConstructor() OrElse TypeOf firstSymbol Is ITypeSymbol Then
                            symbol = firstSymbol
                        End If

                    Case CandidateReason.OverloadResolutionFailure
                        ' If we couldn't bind to a constructor, still classify the type.
                        If firstSymbol.IsConstructor() Then
                            symbol = firstSymbol
                        End If

                    Case CandidateReason.Inaccessible
                        ' If we couldn't bind to a constructor, still classify the type if its accessible
                        If firstSymbol.IsConstructor() AndAlso semanticModel.IsAccessible(node.SpanStart, firstSymbol.ContainingType) Then
                            symbol = firstSymbol
                        End If
                End Select
            End If

            ' Classify a reference to an attribute constructor in an attribute location
            ' as if we were classifying the attribute type itself.
            If symbol.IsConstructor() AndAlso node.IsParentKind(SyntaxKind.Attribute) Then
                symbol = symbol.ContainingType
            End If

            If symbol IsNot Nothing Then
                Select Case symbol.Kind
                    Case SymbolKind.Method
                        Dim classification = GetClassificationForMethod(node, DirectCast(symbol, IMethodSymbol))
                        If classification IsNot Nothing Then
                            result.Add(New ClassifiedSpan(GetNameToken(node).Span, classification))
                            Return
                        End If
                    Case SymbolKind.Event
                        result.Add(New ClassifiedSpan(GetNameToken(node).Span, ClassificationTypeNames.EventName))
                        Return
                    Case SymbolKind.Property
                        result.Add(New ClassifiedSpan(GetNameToken(node).Span, ClassificationTypeNames.PropertyName))
                        Return
                    Case SymbolKind.Field
                        Dim classification = GetClassificationForField(DirectCast(symbol, IFieldSymbol))
                        If classification IsNot Nothing Then
                            result.Add(New ClassifiedSpan(GetNameToken(node).Span, classification))
                            Return
                        End If
                    Case SymbolKind.Parameter
                        result.Add(New ClassifiedSpan(GetNameToken(node).Span, ClassificationTypeNames.ParameterName))
                        Return
                    Case SymbolKind.Local
                        Dim classification = GetClassificationForLocal(DirectCast(symbol, ILocalSymbol))
                        If classification IsNot Nothing Then
                            result.Add(New ClassifiedSpan(GetNameToken(node).Span, classification))
                            Return
                        End If
                End Select

                Dim type = TryCast(symbol, ITypeSymbol)
                If type IsNot Nothing Then
                    Dim classification = GetClassificationForType(type)
                    If classification IsNot Nothing Then
                        Dim token = GetNameToken(node)
                        result.Add(New ClassifiedSpan(token.Span, classification))
                        Return
                    End If
                End If

                If symbol.IsMyNamespace(semanticModel.Compilation) Then
                    result.Add(New ClassifiedSpan(GetNameToken(node).Span, ClassificationTypeNames.Keyword))
                    Return
                End If
            Else
                ' Okay, it doesn't bind to anything.
                Dim identifierName = TryCast(node, IdentifierNameSyntax)
                If identifierName IsNot Nothing Then
                    Dim token = identifierName.Identifier

                    If token.HasMatchingText(SyntaxKind.FromKeyword) AndAlso
                       semanticModel.SyntaxTree.IsExpressionContext(token.SpanStart, cancellationToken, semanticModel) Then

                        ' Optimistically classify "From" as a keyword in expression contexts
                        result.Add(New ClassifiedSpan(token.Span, ClassificationTypeNames.Keyword))
                        Return
                    ElseIf token.HasMatchingText(SyntaxKind.AsyncKeyword) OrElse
                           token.HasMatchingText(SyntaxKind.IteratorKeyword) Then

                        ' Optimistically classify "Async" or "Iterator" as a keyword in expression contexts
                        If semanticModel.SyntaxTree.IsExpressionContext(token.SpanStart, cancellationToken, semanticModel) Then
                            result.Add(New ClassifiedSpan(token.Span, ClassificationTypeNames.Keyword))
                            Return
                        End If
                    End If
                End If
            End If
        End Sub

        Private Function GetClassificationForField(fieldSymbol As IFieldSymbol) As String
            If fieldSymbol.IsConst Then
                Return If(fieldSymbol.ContainingType.IsEnumType(), ClassificationTypeNames.EnumMemberName, ClassificationTypeNames.ConstantName)
            End If

            Return ClassificationTypeNames.FieldName
        End Function

        Private Function GetClassificationForLocal(localSymbol As ILocalSymbol) As String
            Return If(localSymbol.IsConst, ClassificationTypeNames.ConstantName, ClassificationTypeNames.LocalName)
        End Function

        Private Function GetClassificationForMethod(node As NameSyntax, methodSymbol As IMethodSymbol) As String
            If methodSymbol.MethodKind = MethodKind.Constructor Then
                ' If node is member access or qualified name with explicit New on the right side, we should classify New as a keyword.
                If node.IsNewOnRightSideOfDotOrBang() Then
                    Return ClassificationTypeNames.Keyword
                Else
                    ' We bound to a constructor, but we weren't something like the 'New' in 'X.New'.
                    ' This can happen when we're actually just binding the full node 'X.New'.  In this
                    ' case, don't return anything for this full node.  We'll end up hitting the 
                    ' 'New' node as the worker walks down, and we'll classify it then.
                    Return Nothing
                End If
            Else
                Return If(methodSymbol.IsExtensionMethod(), ClassificationTypeNames.ExtensionMethodName, ClassificationTypeNames.MethodName)
            End If
        End Function

        Private Sub ClassifyModifiedIdentifier(
                modifiedIdentifier As ModifiedIdentifierSyntax,
                semanticModel As SemanticModel,
                result As ArrayBuilder(Of ClassifiedSpan),
                cancellationToken As CancellationToken)

            If modifiedIdentifier.ArrayBounds IsNot Nothing OrElse
               modifiedIdentifier.ArrayRankSpecifiers.Count > 0 OrElse
               modifiedIdentifier.Nullable.Kind <> SyntaxKind.None Then

                Return
            End If

            If modifiedIdentifier.IsParentKind(SyntaxKind.VariableDeclarator) AndAlso
               modifiedIdentifier.Parent.IsParentKind(SyntaxKind.FieldDeclaration) Then

                If DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax).AsClause Is Nothing AndAlso
                   DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax).Initializer Is Nothing Then

                    Dim token = modifiedIdentifier.Identifier
                    If token.HasMatchingText(SyntaxKind.AsyncKeyword) OrElse
                       token.HasMatchingText(SyntaxKind.IteratorKeyword) Then

                        ' Optimistically classify "Async" or "Iterator" as a keyword
                        result.Add(New ClassifiedSpan(token.Span, ClassificationTypeNames.Keyword))
                        Return
                    End If
                End If
            End If
        End Sub

        Private Function GetNameToken(node As NameSyntax) As SyntaxToken
            Select Case node.Kind
                Case SyntaxKind.IdentifierName
                    Return DirectCast(node, IdentifierNameSyntax).Identifier
                Case SyntaxKind.GenericName
                    Return DirectCast(node, GenericNameSyntax).Identifier
                Case SyntaxKind.QualifiedName
                    Return DirectCast(node, QualifiedNameSyntax).Right.Identifier
                Case Else
                    Throw New NotSupportedException()
            End Select
        End Function

        Private Sub ClassifyMethodStatement(methodStatement As MethodStatementSyntax, semanticModel As SemanticModel, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            ' Ensure that extension method declarations are classified properly.
            ' Note that the method statement name is likely already classified as a method name
            ' by the syntactic classifier. However, there isn't away to determine whether a VB
            ' method declaration is an extension method syntactically.
            Dim methodSymbol = semanticModel.GetDeclaredSymbol(methodStatement)
            If methodSymbol.IsExtensionMethod Then
                result.Add(New ClassifiedSpan(methodStatement.Identifier.Span, ClassificationTypeNames.ExtensionMethodName))
            End If
        End Sub

    End Class
End Namespace
