' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Simplification.Simplifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification.Simplifiers
    Friend MustInherit Class AbstractVisualBasicSimplifier(Of TSyntax As SyntaxNode, TSimplifiedSyntax As SyntaxNode)
        Inherits AbstractSimplifier(Of TSyntax, TSimplifiedSyntax, VisualBasicSimplifierOptions)

        ''' <summary>
        ''' Returns the predefined keyword kind for a given special type.
        ''' </summary>
        ''' <param name="type">The specialtype of this type.</param>
        ''' <returns>The keyword kind for a given special type, or SyntaxKind.None if the type name is not a predefined type.</returns>
        Protected Shared Function GetPredefinedKeywordKind(type As SpecialType) As SyntaxKind
            Select Case type
                Case SpecialType.System_Boolean
                    Return SyntaxKind.BooleanKeyword
                Case SpecialType.System_Byte
                    Return SyntaxKind.ByteKeyword
                Case SpecialType.System_SByte
                    Return SyntaxKind.SByteKeyword
                Case SpecialType.System_Int32
                    Return SyntaxKind.IntegerKeyword
                Case SpecialType.System_UInt32
                    Return SyntaxKind.UIntegerKeyword
                Case SpecialType.System_Int16
                    Return SyntaxKind.ShortKeyword
                Case SpecialType.System_UInt16
                    Return SyntaxKind.UShortKeyword
                Case SpecialType.System_Int64
                    Return SyntaxKind.LongKeyword
                Case SpecialType.System_UInt64
                    Return SyntaxKind.ULongKeyword
                Case SpecialType.System_Single
                    Return SyntaxKind.SingleKeyword
                Case SpecialType.System_Double
                    Return SyntaxKind.DoubleKeyword
                Case SpecialType.System_Decimal
                    Return SyntaxKind.DecimalKeyword
                Case SpecialType.System_String
                    Return SyntaxKind.StringKeyword
                Case SpecialType.System_Char
                    Return SyntaxKind.CharKeyword
                Case SpecialType.System_Object
                    Return SyntaxKind.ObjectKeyword
                Case SpecialType.System_DateTime
                    Return SyntaxKind.DateKeyword
                Case Else
                    Return SyntaxKind.None
            End Select
        End Function

        Protected Shared Function TryReplaceWithAlias(
            node As ExpressionSyntax,
            semanticModel As SemanticModel,
            <Out> ByRef aliasReplacement As IAliasSymbol) As Boolean
            aliasReplacement = Nothing

            If Not IsAliasReplaceableExpression(node) Then
                Return False
            End If

            Dim symbol = semanticModel.GetSymbolInfo(node).Symbol

            If (symbol.IsConstructor()) Then
                symbol = symbol.ContainingType
            End If

            ' The following condition checks if the user has used alias in the original code and
            ' if so the expression is replaced with the Alias
            If TypeOf node Is QualifiedNameSyntax Then
                Dim qualifiedNameNode = DirectCast(node, QualifiedNameSyntax)
                If qualifiedNameNode.Right.Identifier.HasAnnotations(AliasAnnotation.Kind) Then
                    Dim aliasAnnotationInfo = qualifiedNameNode.Right.Identifier.GetAnnotations(AliasAnnotation.Kind).Single()

                    Dim aliasName = AliasAnnotation.GetAliasName(aliasAnnotationInfo)
                    Dim aliasIdentifier = SyntaxFactory.IdentifierName(aliasName)

                    Dim aliasTypeInfo = semanticModel.GetSpeculativeAliasInfo(node.SpanStart, aliasIdentifier, SpeculativeBindingOption.BindAsTypeOrNamespace)

                    If Not aliasTypeInfo Is Nothing Then
                        aliasReplacement = aliasTypeInfo
                        Return ValidateAliasForTarget(aliasReplacement, semanticModel, node, symbol)
                    End If
                End If
            End If

            If node.Kind = SyntaxKind.IdentifierName AndAlso semanticModel.GetAliasInfo(DirectCast(node, IdentifierNameSyntax)) IsNot Nothing Then
                Return False
            End If

            ' an alias can only replace a type Or namespace
            If symbol Is Nothing OrElse
                (symbol.Kind <> SymbolKind.Namespace AndAlso symbol.Kind <> SymbolKind.NamedType) Then

                Return False
            End If

            If symbol Is Nothing OrElse Not TypeOf (symbol) Is INamespaceOrTypeSymbol Then
                Return False
            End If

            Dim preferAliasToQualifiedName = True

            If TypeOf node Is QualifiedNameSyntax Then
                Dim qualifiedName = DirectCast(node, QualifiedNameSyntax)
                If Not qualifiedName.Right.HasAnnotation(Simplifier.SpecialTypeAnnotation) Then
                    Dim type = semanticModel.GetTypeInfo(node).Type
                    If Not type Is Nothing Then
                        Dim keywordKind = GetPredefinedKeywordKind(type.SpecialType)
                        If keywordKind <> SyntaxKind.None Then
                            preferAliasToQualifiedName = False
                        End If
                    End If
                End If
            End If

            aliasReplacement = DirectCast(symbol, INamespaceOrTypeSymbol).GetAliasForSymbol(node, semanticModel)
            If aliasReplacement IsNot Nothing And preferAliasToQualifiedName Then
                Return ValidateAliasForTarget(aliasReplacement, semanticModel, node, symbol)
            End If

            Return False
        End Function

        Private Shared Function IsAliasReplaceableExpression(expression As ExpressionSyntax) As Boolean
            If expression.Kind = SyntaxKind.IdentifierName OrElse
               expression.Kind = SyntaxKind.QualifiedName Then
                Return True
            End If

            If expression.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                Dim memberAccess = DirectCast(expression, MemberAccessExpressionSyntax)

                Return memberAccess.Expression IsNot Nothing AndAlso IsAliasReplaceableExpression(memberAccess.Expression)
            End If

            Return False
        End Function

        ' We must verify that the alias actually binds back to the thing it's aliasing.
        ' It's possible there is another symbol with the same name as the alias that binds first
        Private Shared Function ValidateAliasForTarget(aliasReplacement As IAliasSymbol, semanticModel As SemanticModel, node As ExpressionSyntax, symbol As ISymbol) As Boolean
            Dim aliasName = aliasReplacement.Name

            ' If we're the argument of a NameOf(X.Y) call, then we can't simplify to an
            ' alias unless the alias has the same name as us (i.e. 'Y').
            If node.IsNameOfArgumentExpression() Then
                Dim nameofValueOpt = semanticModel.GetConstantValue(node.Parent.Parent.Parent)
                If Not nameofValueOpt.HasValue Then
                    Return False
                End If

                Dim existingValue = TryCast(nameofValueOpt.Value, String)
                If existingValue Is Nothing OrElse existingValue <> aliasName Then
                    Return False
                End If
            End If

            Dim boundSymbols = semanticModel.LookupNamespacesAndTypes(node.SpanStart, name:=aliasName)
            If boundSymbols.Length = 1 Then
                Dim boundAlias = TryCast(boundSymbols(0), IAliasSymbol)
                If boundAlias IsNot Nothing And aliasReplacement.Target.Equals(symbol) Then
                    If symbol.IsAttribute Then
                        boundSymbols = semanticModel.LookupNamespacesAndTypes(node.Span.Start, name:=aliasName + "Attribute")
                        Return boundSymbols.IsEmpty
                    End If

                    Return True
                End If
            End If

            Return False
        End Function

        Protected Shared Function PreferPredefinedTypeKeywordInMemberAccess(expression As ExpressionSyntax, options As VisualBasicSimplifierOptions) As Boolean
            Return (IsDirectChildOfMemberAccessExpression(expression) OrElse IsInCrefReferenceForPredefinedTypeInMemberAccessContext(expression)) AndAlso
                   (Not InsideNameOfExpression(expression)) AndAlso
                   options.PreferPredefinedTypeKeywordInMemberAccess.Value
        End Function

        Protected Shared Function InsideNameOfExpression(expr As ExpressionSyntax) As Boolean
            Dim nameOfExpression = expr.FirstAncestorOrSelf(Of NameOfExpressionSyntax)()
            Return nameOfExpression IsNot Nothing
        End Function

        ''' <Remarks>
        ''' Note: This helper exists solely to work around Bug 1012713. Once it is fixed, this helper must be
        ''' deleted in favor of <see cref="InsideCrefReference(ExpressionSyntax)"/>.
        ''' Context: Bug 1012713 makes it so that the compiler doesn't support <c>PredefinedType.Member</c> inside crefs 
        ''' (i.e. System.Int32.MaxValue is supported but Integer.MaxValue isn't). Until this bug is fixed, we don't 
        ''' support simplifying types names Like System.Int32.MaxValue to Integer.MaxValue.
        ''' </Remarks>
        Private Shared Function IsInCrefReferenceForPredefinedTypeInMemberAccessContext(expression As ExpressionSyntax) As Boolean
            Return (InsideCrefReference(expression) AndAlso Not expression.IsLeftSideOfQualifiedName)
        End Function
    End Class
End Namespace
