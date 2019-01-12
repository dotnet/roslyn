' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.PreferFrameworkType
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicPreferFrameworkTypeDiagnosticAnalyzer
        Inherits PreferFrameworkTypeDiagnosticAnalyzerBase(Of SyntaxKind, ExpressionSyntax, PredefinedTypeSyntax)

        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) =
            ImmutableArray.Create(SyntaxKind.PredefinedType)

        Protected Overrides Function IsInMemberAccessOrCrefReferenceContext(node As ExpressionSyntax) As Boolean
            Return node.IsInMemberAccessContext() OrElse node.InsideCrefReference()
        End Function

        Protected Overrides Function IsPredefinedTypeReplaceableWithFrameworkType(node As PredefinedTypeSyntax) As Boolean
            ' There is nothing to replace if keyword matches type name. For e.g: we don't want to replace `Object` 
            ' Or `String` because we'd essentially be replacing it with the same thing.
            Return Not KeywordMatchesTypeName(node.Keyword.Kind())
        End Function

        Protected Overrides Function GetLanguageName() As String
            Return LanguageNames.VisualBasic
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
