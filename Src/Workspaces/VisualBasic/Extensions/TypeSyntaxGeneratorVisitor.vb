' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Class TypeSyntaxGeneratorVisitor
        Inherits SymbolVisitor(Of TypeSyntax)

        Private ReadOnly addGlobal As Boolean

        Public Sub New(addGlobal As Boolean)
            Me.addGlobal = addGlobal
        End Sub

        Public Overrides Function DefaultVisit(node As ISymbol) As TypeSyntax
            Throw New NotImplementedException()
        End Function

        Private Function AddInformationTo(Of TTypeSyntax As TypeSyntax)(type As TTypeSyntax, symbol As ISymbol) As TTypeSyntax
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
            Select Case symbol.SpecialType
                Case SpecialType.System_Object
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))
                Case SpecialType.System_Boolean
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BooleanKeyword))
                Case SpecialType.System_Char
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.CharKeyword))
                Case SpecialType.System_SByte
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.SByteKeyword))
                Case SpecialType.System_Byte
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword))
                Case SpecialType.System_Int16
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ShortKeyword))
                Case SpecialType.System_UInt16
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UShortKeyword))
                Case SpecialType.System_Int32
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntegerKeyword))
                Case SpecialType.System_Int64
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword))
                Case SpecialType.System_UInt32
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntegerKeyword))
                Case SpecialType.System_UInt64
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword))
                Case SpecialType.System_Decimal
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword))
                Case SpecialType.System_Single
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.SingleKeyword))
                Case SpecialType.System_Double
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword))
                Case SpecialType.System_String
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))
                Case SpecialType.System_DateTime
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DateKeyword))
            End Select

            If symbol.Name = String.Empty OrElse symbol.IsAnonymousType Then
                Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))
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
                    If addGlobal AndAlso symbol.TypeKind <> TypeKind.[Error] Then
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
                If addGlobal Then
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