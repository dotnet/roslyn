' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.PreferFrameworkType
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicPreferFrameworkTypeDiagnosticAnalyzer
        Inherits PreferFrameworkTypeDiagnosticAnalyzerBase(Of SyntaxKind)

        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind)
            Get
                Return ImmutableArray.Create(SyntaxKind.PredefinedType)
            End Get
        End Property

        Protected Overrides Function IsInMemberAccessContext(node As SyntaxNode, semanticModel As SemanticModel) As Boolean
            Dim expression = TryCast(node, ExpressionSyntax)
            If expression Is Nothing Then
                Return False
            End If

            Return expression.IsInMemberAccessContext() OrElse expression.InsideCrefReference()
        End Function

        Protected Overrides Function IsPredefinedTypeAndReplaceableWithFrameworkType(node As SyntaxNode) As Boolean
            Dim keywordKind = TryCast(node, PredefinedTypeSyntax)?.Keyword.Kind()

            ' There is nothing to replace if keyword matches type name.
            Return Not (keywordKind Is Nothing) AndAlso
                   SyntaxFacts.IsPredefinedType(keywordKind.Value) AndAlso
                   Not KeywordMatchesTypeName(keywordKind.Value)
        End Function

        ''' <summary>
        ''' Returns true, if the VB language keyword for predefined type matches its
        ''' actual framework type name.
        ''' </summary>
        Private Function KeywordMatchesTypeName(kind As SyntaxKind) As Boolean
            Select Case kind
                Case _
                SyntaxKind.BooleanKeyword,
                SyntaxKind.ByteKeyword,
                SyntaxKind.CharKeyword,
                SyntaxKind.ObjectKeyword,
                SyntaxKind.SByteKeyword,
                SyntaxKind.StringKeyword,
                SyntaxKind.SingleKeyword,
                SyntaxKind.DecimalKeyword,
                SyntaxKind.DoubleKeyword
                    Return True
            End Select

            Return False
        End Function
    End Class

End Namespace