' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.PreferFrameworkType
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicPreferFrameworkTypeDiagnosticAnalyzer
        Inherits PreferFrameworkTypeDiagnosticAnalyzerBase(Of
            SyntaxKind,
            ExpressionSyntax,
        TypeSyntax,
        IdentifierNameSyntax,
        PredefinedTypeSyntax)

        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) =
            ImmutableArray.Create(SyntaxKind.PredefinedType)

        Protected Overrides Function IsInMemberAccessOrCrefReferenceContext(node As ExpressionSyntax) As Boolean
            Return node.IsDirectChildOfMemberAccessExpression() OrElse node.InsideCrefReference()
        End Function

        Protected Overrides Function IsIdentifierNameReplaceableWithFrameworkType(semanticModel As SemanticModel, node As IdentifierNameSyntax) As Boolean
            Return False
        End Function

        Protected Overrides Function IsPredefinedTypeReplaceableWithFrameworkType(node As PredefinedTypeSyntax) As Boolean
            ' There is nothing to replace if keyword matches type name. For e.g: we don't want to replace `Object` 
            ' Or `String` because we'd essentially be replacing it with the same thing.
            Return Not KeywordMatchesTypeName(node.Keyword.Kind())
        End Function

        ''' <summary>
        ''' Returns true, if the VB language keyword for predefined type matches its
        ''' actual framework type name.
        ''' </summary>
        Private Shared Function KeywordMatchesTypeName(kind As SyntaxKind) As Boolean
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
