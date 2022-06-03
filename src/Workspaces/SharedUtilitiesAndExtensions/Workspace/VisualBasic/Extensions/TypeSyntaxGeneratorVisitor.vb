' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Class TypeSyntaxGeneratorVisitor
        Inherits SymbolVisitor(Of TypeSyntax)

        Private ReadOnly _addGlobal As Boolean

        Private Shared ReadOnly AddGlobalInstance As TypeSyntaxGeneratorVisitor = New TypeSyntaxGeneratorVisitor(addGlobal:=True)
        Private Shared ReadOnly NotAddGlobalInstance As TypeSyntaxGeneratorVisitor = New TypeSyntaxGeneratorVisitor(addGlobal:=False)

        Private Sub New(addGlobal As Boolean)
            Me._addGlobal = addGlobal
        End Sub

        Public Shared Function Create(addGlobal As Boolean) As TypeSyntaxGeneratorVisitor
            Return If(addGlobal, AddGlobalInstance, NotAddGlobalInstance)
        End Function

        Public Overrides Function DefaultVisit(node As ISymbol) As TypeSyntax
            Throw New NotImplementedException()
        End Function

        Private Shared Function AddInformationTo(Of TTypeSyntax As TypeSyntax)(type As TTypeSyntax, symbol As ISymbol) As TTypeSyntax
            type = type.WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker)
            type = type.WithAdditionalAnnotations(SymbolAnnotation.Create(symbol))
            Return type
        End Function

        Public Overrides Function VisitAlias(symbol As IAliasSymbol) As TypeSyntax
            Return AddInformationTo(symbol.Name.ToIdentifierName, symbol)
        End Function

        Public Overrides Function VisitArrayType(symbol As IArrayTypeSymbol) As TypeSyntax
            Dim underlyingNonArrayType = symbol.ElementType
            While underlyingNonArrayType.Kind = SymbolKind.ArrayType
                underlyingNonArrayType = (DirectCast(underlyingNonArrayType, IArrayTypeSymbol)).ElementType
            End While

            Dim elementTypeSyntax = underlyingNonArrayType.Accept(Me)
            Dim ranks = New List(Of ArrayRankSpecifierSyntax)()
            Dim arrayType = symbol
            While arrayType IsNot Nothing
                Dim commaCount = Math.Max(0, arrayType.Rank - 1)
                Dim commas = SyntaxFactory.TokenList(Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), commaCount))
                ranks.Add(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.Token(SyntaxKind.OpenParenToken), commas, SyntaxFactory.Token(SyntaxKind.CloseParenToken)))
                arrayType = TryCast(arrayType.ElementType, IArrayTypeSymbol)
            End While

            Dim arrayTypeSyntax = SyntaxFactory.ArrayType(elementTypeSyntax, SyntaxFactory.List(ranks))
            Return AddInformationTo(arrayTypeSyntax, symbol)
        End Function

        Public Overrides Function VisitDynamicType(symbol As IDynamicTypeSymbol) As TypeSyntax
            Return AddInformationTo(SyntaxFactory.IdentifierName("dynamic"), symbol)
        End Function

        Public Function CreateSimpleTypeSyntax(symbol As INamedTypeSymbol) As TypeSyntax
            Dim syntax = TryCreateSpecializedNamedTypeSyntax(symbol)
            If syntax IsNot Nothing Then
                Return syntax
            End If

            If symbol.IsTupleType AndAlso
               symbol.TupleUnderlyingType IsNot Nothing AndAlso
               Not symbol.Equals(symbol.TupleUnderlyingType) Then
                Return CreateSimpleTypeSyntax(symbol.TupleUnderlyingType)
            End If

            If symbol.Name = String.Empty OrElse symbol.IsAnonymousType Then
                Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Object"))
            End If

            If symbol.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T Then
                Return AddInformationTo(SyntaxFactory.NullableType(symbol.TypeArguments.First().Accept(Me)), symbol)
            End If

            If symbol.TypeParameters.Length = 0 Then
                Return symbol.Name.ToIdentifierName
            End If

            Return SyntaxFactory.GenericName(
                symbol.Name.ToIdentifierToken,
                SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(symbol.TypeArguments.[Select](Function(t) t.Accept(Me)))))
        End Function

        Private Shared Function TryCreateSpecializedNamedTypeSyntax(symbol As INamedTypeSymbol) As TypeSyntax
            Select Case symbol.SpecialType
                Case SpecialType.System_Object
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Object"))
                Case SpecialType.System_Boolean
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Boolean"))
                Case SpecialType.System_Char
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Char"))
                Case SpecialType.System_SByte
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("SByte"))
                Case SpecialType.System_Byte
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Byte"))
                Case SpecialType.System_Int16
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Int16"))
                Case SpecialType.System_UInt16
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("UInt16"))
                Case SpecialType.System_Int32
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Int32"))
                Case SpecialType.System_Int64
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Int64"))
                Case SpecialType.System_UInt32
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("UInt32"))
                Case SpecialType.System_UInt64
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("UInt64"))
                Case SpecialType.System_Decimal
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Decimal"))
                Case SpecialType.System_Single
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Single"))
                Case SpecialType.System_Double
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Double"))
                Case SpecialType.System_String
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("String"))
                Case SpecialType.System_DateTime
                    Return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("DateTime"))
            End Select

            If symbol.IsTupleType AndAlso symbol.TupleElements.Length >= 2 Then
                Return CreateTupleTypeSyntax(symbol)
            End If

            Return Nothing
        End Function

        Private Shared Function CreateTupleTypeSyntax(symbol As INamedTypeSymbol) As TypeSyntax
            Dim elements = symbol.TupleElements

            Return SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(
                elements.Select(Function(element) If(Not element.IsImplicitlyDeclared,
                                                        SyntaxFactory.NamedTupleElement(
                                                                        SyntaxFactory.Identifier(element.Name),
                                                                        SyntaxFactory.SimpleAsClause(
                                                                                    SyntaxFactory.Token(SyntaxKind.AsKeyword),
                                                                                    Nothing,
                                                                                    element.Type.GenerateTypeSyntax())),
                                                        DirectCast(SyntaxFactory.TypedTupleElement(
                                                                        element.Type.GenerateTypeSyntax()), TupleElementSyntax)))))
        End Function

        Public Overrides Function VisitNamedType(symbol As INamedTypeSymbol) As TypeSyntax
            Dim typeSyntax = CreateSimpleTypeSyntax(symbol)
            If Not (TypeOf typeSyntax Is SimpleNameSyntax) Then
                Return typeSyntax
            End If

            Dim simpleNameSyntax = DirectCast(typeSyntax, SimpleNameSyntax)
            If symbol.ContainingType IsNot Nothing Then
                If symbol.ContainingType.TypeKind = TypeKind.Submission Then
                    Return typeSyntax
                Else
                    Return AddInformationTo(SyntaxFactory.QualifiedName(DirectCast(symbol.ContainingType.Accept(Me), NameSyntax), simpleNameSyntax), symbol)
                End If
            ElseIf symbol.ContainingNamespace IsNot Nothing Then
                If symbol.ContainingNamespace.IsGlobalNamespace Then
                    If _addGlobal AndAlso symbol.TypeKind <> TypeKind.[Error] Then
                        Return AddInformationTo(SyntaxFactory.QualifiedName(SyntaxFactory.GlobalName(), simpleNameSyntax), symbol)
                    End If
                Else
                    Dim container = symbol.ContainingNamespace.Accept(Me)
                    Return AddInformationTo(SyntaxFactory.QualifiedName(DirectCast(container, NameSyntax), simpleNameSyntax), symbol)
                End If
            End If

            Return simpleNameSyntax
        End Function

        Public Overrides Function VisitNamespace(symbol As INamespaceSymbol) As TypeSyntax
            Dim result = AddInformationTo(symbol.Name.ToIdentifierName, symbol)
            If symbol.ContainingNamespace Is Nothing Then
                Return result
            End If

            If symbol.ContainingNamespace.IsGlobalNamespace Then
                If _addGlobal Then
                    Return AddInformationTo(SyntaxFactory.QualifiedName(SyntaxFactory.GlobalName(), result), symbol)
                Else
                    Return result
                End If
            Else
                Dim container = symbol.ContainingNamespace.Accept(Me)
                Return AddInformationTo(SyntaxFactory.QualifiedName(DirectCast(container, NameSyntax), result), symbol)
            End If
        End Function

        Public Overrides Function VisitPointerType(symbol As IPointerTypeSymbol) As TypeSyntax
            ' TODO(cyrusn): What to do here?  Maybe object would be better instead?
            Return symbol.PointedAtType.Accept(Me)
        End Function

        Public Overrides Function VisitTypeParameter(symbol As ITypeParameterSymbol) As TypeSyntax
            Return AddInformationTo(symbol.Name.ToIdentifierName, symbol)
        End Function
    End Class
End Namespace
