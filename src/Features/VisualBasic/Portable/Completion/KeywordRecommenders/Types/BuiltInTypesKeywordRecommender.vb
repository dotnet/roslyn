' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Types
    ''' <summary>
    ''' Recommends built-in types in various contexts.
    ''' </summary>
    Friend Class BuiltInTypesKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            ' Are we right after an As in an Enum declaration?
            Dim enumDeclaration = targetToken.GetAncestor(Of EnumStatementSyntax)()
            If enumDeclaration IsNot Nothing AndAlso
               enumDeclaration.UnderlyingType IsNot Nothing AndAlso
               targetToken = enumDeclaration.UnderlyingType.AsKeyword Then

                Dim keywordList = GetIntrinsicTypeKeywords(context)

                Return keywordList.Where(Function(k) k.Keyword.EndsWith("Byte", StringComparison.Ordinal) OrElse
                                                     k.Keyword.EndsWith("Short", StringComparison.Ordinal) OrElse
                                                     k.Keyword.EndsWith("Integer", StringComparison.Ordinal) OrElse
                                                     k.Keyword.EndsWith("Long", StringComparison.Ordinal))
            End If

            ' Are we inside a type constraint? Because these are never allowed there
            If targetToken.GetAncestor(Of TypeParameterSingleConstraintClauseSyntax)() IsNot Nothing OrElse
               targetToken.GetAncestor(Of TypeParameterMultipleConstraintClauseSyntax)() IsNot Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            ' Are we inside an attribute block? They're at least not allowed as the attribute itself
            If targetToken.Parent.IsKind(SyntaxKind.AttributeList) Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            ' Are we in an Imports statement? Type keywords aren't allowed there, just fully qualified type names
            If targetToken.GetAncestor(Of ImportsStatementSyntax)() IsNot Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            ' Are we after Inherits or Implements? Type keywords aren't allowed here.
            If targetToken.IsChildToken(Of InheritsStatementSyntax)(Function(n) n.InheritsKeyword) OrElse
               targetToken.IsChildToken(Of ImplementsStatementSyntax)(Function(n) n.ImplementsKeyword) Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            If context.IsTypeContext Then
                Return GetIntrinsicTypeKeywords(context)
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

        Private Shared ReadOnly s_intrinsicKeywordNames As String() = {
            "Boolean",
            "Byte",
            "Char",
            "Date",
            "Decimal",
            "Double",
            "Integer",
            "Long",
            "Object",
            "SByte",
            "Short",
            "Single",
            "String",
            "UInteger",
            "ULong",
            "UShort"}

        Private Shared ReadOnly s_intrinsicSpecialTypes As SByte() = {
            SpecialType.System_Boolean,
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_DateTime,
            SpecialType.System_Decimal,
            SpecialType.System_Double,
            SpecialType.System_Int32,
            SpecialType.System_Int64,
            SpecialType.System_Object,
            SpecialType.System_SByte,
            SpecialType.System_Int16,
            SpecialType.System_Single,
            SpecialType.System_String,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16}

        Private Function GetIntrinsicTypeKeywords(context As VisualBasicSyntaxContext) As IEnumerable(Of RecommendedKeyword)
            Debug.Assert(s_intrinsicKeywordNames.Length = s_intrinsicSpecialTypes.Length)

            Dim inferredSpecialTypes = context.InferredTypes.Select(Function(t) t.SpecialType).ToSet()

            Dim recommendedKeywords(s_intrinsicKeywordNames.Length - 1) As RecommendedKeyword
            For i = 0 To s_intrinsicKeywordNames.Length - 1
                Dim keyword As String = s_intrinsicKeywordNames(i)
                Dim specialType As SpecialType = DirectCast(s_intrinsicSpecialTypes(i), SpecialType)

                Dim priority = If(inferredSpecialTypes.Contains(specialType), SymbolMatchPriority.Keyword, MatchPriority.Default)

                recommendedKeywords(i) = New RecommendedKeyword(s_intrinsicKeywordNames(i), Glyph.Keyword,
                                                                Function(cancellationToken)
                                                                    Dim tooltip = GetDocumentationCommentText(context, specialType, cancellationToken)
                                                                    Return RecommendedKeyword.CreateDisplayParts(keyword, tooltip)
                                                                End Function, isIntrinsic:=True, matchPriority:=priority)
            Next

            Return recommendedKeywords
        End Function

        Private Function GetDocumentationCommentText(context As VisualBasicSyntaxContext, type As SpecialType, cancellationToken As CancellationToken) As String
            Dim symbol = context.SemanticModel.Compilation.GetSpecialType(type)
            Return symbol.GetDocumentationComment(context.SemanticModel.Compilation, Globalization.CultureInfo.CurrentUICulture, expandIncludes:=True, expandInheritdoc:=True, cancellationToken:=cancellationToken).SummaryText
        End Function
    End Class
End Namespace
