' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class NameSyntaxClassifier
        Inherits AbstractNameSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxNodeTypes As ImmutableArray(Of Type) = ImmutableArray.Create(
            GetType(NameSyntax),
            GetType(ModifiedIdentifierSyntax),
            GetType(MethodStatementSyntax),
            GetType(LabelSyntax))

        Public Overrides Sub AddClassifications(
                syntax As SyntaxNode,
                textSpan As TextSpan,
                semanticModel As SemanticModel,
                options As ClassificationOptions,
                result As SegmentedList(Of ClassifiedSpan),
                cancellationToken As CancellationToken)

            Dim nameSyntax = TryCast(syntax, NameSyntax)
            If nameSyntax IsNot Nothing Then
                ClassifyNameSyntax(nameSyntax, semanticModel, result, cancellationToken)
                Return
            End If

            Dim modifiedIdentifier = TryCast(syntax, ModifiedIdentifierSyntax)
            If modifiedIdentifier IsNot Nothing Then
                ClassifyModifiedIdentifier(modifiedIdentifier, result)
                Return
            End If

            Dim methodStatement = TryCast(syntax, MethodStatementSyntax)
            If methodStatement IsNot Nothing Then
                ClassifyMethodStatement(methodStatement, semanticModel, result)
                Return
            End If

            Dim labelSyntax = TryCast(syntax, LabelSyntax)
            If labelSyntax IsNot Nothing Then
                ClassifyLabelSyntax(labelSyntax, result)
                Return
            End If
        End Sub

        Protected Overrides Function GetRightmostNameArity(node As SyntaxNode) As Integer?
            If TypeOf (node) Is ExpressionSyntax Then
                Return DirectCast(node, ExpressionSyntax).GetRightmostName()?.Arity
            End If

            Return Nothing
        End Function

        Protected Overrides Function IsParentAnAttribute(node As SyntaxNode) As Boolean
            Return node.IsParentKind(SyntaxKind.Attribute)
        End Function

        Private Sub ClassifyNameSyntax(
                node As NameSyntax,
                semanticModel As SemanticModel,
                result As SegmentedList(Of ClassifiedSpan),
                cancellationToken As CancellationToken)

            Dim classifiedSpan As ClassifiedSpan

            Dim symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken)
            Dim symbol = TryGetSymbol(node, symbolInfo)

            If symbol Is Nothing Then
                If TryClassifyIdentifier(node, semanticModel, cancellationToken, classifiedSpan) Then
                    result.Add(classifiedSpan)
                End If

                Return
            End If

            If TryClassifyMyNamespace(node, symbol, semanticModel, classifiedSpan) Then
                result.Add(classifiedSpan)
            ElseIf TryClassifySymbol(node, symbol, classifiedSpan) Then
                result.Add(classifiedSpan)

                ' Additionally classify static symbols
                TryClassifyStaticSymbol(symbol, classifiedSpan.TextSpan, result)
            End If
        End Sub

        Private Shared Function TryClassifySymbol(
            node As NameSyntax,
            symbol As ISymbol,
            ByRef classifiedSpan As ClassifiedSpan) As Boolean

            Select Case symbol.Kind
                Case SymbolKind.Namespace
                    ' Do not classify the Global namespace. It is already syntactically classified as a keyword.
                    ' Also, we ignore QualifiedNameSyntax nodes since we want to classify individual name parts.
                    If Not node.IsKind(SyntaxKind.GlobalName) AndAlso TypeOf node Is IdentifierNameSyntax Then
                        classifiedSpan = New ClassifiedSpan(GetNameToken(node).Span, ClassificationTypeNames.NamespaceName)
                        Return True
                    End If
                Case SymbolKind.Method
                    Dim classification = GetClassificationForMethod(node, DirectCast(symbol, IMethodSymbol))
                    If classification IsNot Nothing Then
                        classifiedSpan = New ClassifiedSpan(GetNameToken(node).Span, classification)
                        Return True
                    End If
                Case SymbolKind.Event
                    classifiedSpan = New ClassifiedSpan(GetNameToken(node).Span, ClassificationTypeNames.EventName)
                    Return True
                Case SymbolKind.Property
                    classifiedSpan = New ClassifiedSpan(GetNameToken(node).Span, ClassificationTypeNames.PropertyName)
                    Return True
                Case SymbolKind.Field
                    Dim classification = GetClassificationForField(DirectCast(symbol, IFieldSymbol))
                    If classification IsNot Nothing Then
                        classifiedSpan = New ClassifiedSpan(GetNameToken(node).Span, classification)
                        Return True
                    End If
                Case SymbolKind.Parameter
                    classifiedSpan = New ClassifiedSpan(GetNameToken(node).Span, ClassificationTypeNames.ParameterName)
                    Return True
                Case SymbolKind.Local
                    Dim classification = GetClassificationForLocal(DirectCast(symbol, ILocalSymbol))
                    If classification IsNot Nothing Then
                        classifiedSpan = New ClassifiedSpan(GetNameToken(node).Span, classification)
                        Return True
                    End If
            End Select

            Dim type = TryCast(symbol, ITypeSymbol)
            If type IsNot Nothing Then
                Dim classification = GetClassificationForType(type)
                If classification IsNot Nothing Then
                    Dim token = GetNameToken(node)
                    classifiedSpan = New ClassifiedSpan(token.Span, classification)
                    Return True
                End If
            End If

            Return False
        End Function

        Private Shared Function TryClassifyMyNamespace(
            node As NameSyntax,
            symbol As ISymbol,
            semanticModel As SemanticModel,
            ByRef classifiedSpan As ClassifiedSpan) As Boolean

            If symbol.IsMyNamespace(semanticModel.Compilation) Then
                classifiedSpan = New ClassifiedSpan(GetNameToken(node).Span, ClassificationTypeNames.Keyword)
                Return True
            End If

            Return False
        End Function

        Private Shared Function TryClassifyIdentifier(
            node As NameSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken,
            ByRef classifiedSpan As ClassifiedSpan) As Boolean

            ' Okay, it doesn't bind to anything.
            Dim identifierName = TryCast(node, IdentifierNameSyntax)
            If identifierName IsNot Nothing Then
                Dim token = identifierName.Identifier

                If token.HasMatchingText(SyntaxKind.FromKeyword) AndAlso
                    semanticModel.SyntaxTree.IsExpressionContext(token.SpanStart, cancellationToken, semanticModel) Then

                    ' Optimistically classify "From" as a keyword in expression contexts
                    classifiedSpan = New ClassifiedSpan(token.Span, ClassificationTypeNames.Keyword)
                    Return True
                ElseIf token.HasMatchingText(SyntaxKind.AsyncKeyword) OrElse
                    token.HasMatchingText(SyntaxKind.IteratorKeyword) Then

                    ' Optimistically classify "Async" or "Iterator" as a keyword in expression contexts
                    If semanticModel.SyntaxTree.IsExpressionContext(token.SpanStart, cancellationToken, semanticModel) Then
                        classifiedSpan = New ClassifiedSpan(token.Span, ClassificationTypeNames.Keyword)
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        Private Shared Function GetClassificationForField(fieldSymbol As IFieldSymbol) As String
            If fieldSymbol.IsConst Then
                Return If(fieldSymbol.ContainingType.IsEnumType(), ClassificationTypeNames.EnumMemberName, ClassificationTypeNames.ConstantName)
            End If

            Return ClassificationTypeNames.FieldName
        End Function

        Private Shared Function GetClassificationForLocal(localSymbol As ILocalSymbol) As String
            Return If(localSymbol.IsConst,
                      ClassificationTypeNames.ConstantName,
                      ClassificationTypeNames.LocalName)
        End Function

        Private Shared Function GetClassificationForMethod(node As NameSyntax, methodSymbol As IMethodSymbol) As String
            Select Case methodSymbol.MethodKind
                Case MethodKind.Constructor
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

                Case MethodKind.BuiltinOperator,
                     MethodKind.UserDefinedOperator
                    ' Operators are already classified syntactically.
                    Return Nothing
            End Select

            Return If(methodSymbol.IsReducedExtension(),
                      ClassificationTypeNames.ExtensionMethodName,
                      ClassificationTypeNames.MethodName)
        End Function

        Private Shared Sub ClassifyModifiedIdentifier(
                modifiedIdentifier As ModifiedIdentifierSyntax,
                result As SegmentedList(Of ClassifiedSpan))

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

        Private Shared Function GetNameToken(node As NameSyntax) As SyntaxToken
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

        Private Shared Sub ClassifyMethodStatement(methodStatement As MethodStatementSyntax, semanticModel As SemanticModel, result As SegmentedList(Of ClassifiedSpan))
            ' Ensure that extension method declarations are classified properly.
            ' Note that the method statement name is likely already classified as a method name
            ' by the syntactic classifier. However, there isn't away to determine whether a VB
            ' method declaration is an extension method syntactically.
            Dim methodSymbol = semanticModel.GetDeclaredSymbol(methodStatement)
            If methodSymbol IsNot Nothing AndAlso methodSymbol.IsExtensionMethod Then
                result.Add(New ClassifiedSpan(methodStatement.Identifier.Span, ClassificationTypeNames.ExtensionMethodName))
            End If
        End Sub

        Private Shared Sub ClassifyLabelSyntax(
            node As LabelSyntax,
            result As SegmentedList(Of ClassifiedSpan))

            result.Add(New ClassifiedSpan(node.LabelToken.Span, ClassificationTypeNames.LabelName))
        End Sub
    End Class
End Namespace
