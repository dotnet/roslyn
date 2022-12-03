' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertCast
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertCast
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertDirectCastToTryCast), [Shared]>
    Friend Class VisualBasicConvertDirectCastToTryCastCodeRefactoringProvider
        Inherits AbstractConvertCastCodeRefactoringProvider(Of TypeSyntax, DirectCastExpressionSyntax, TryCastExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Change_to_TryCast
        End Function

        Protected Overrides ReadOnly Property FromKind As Integer = CInt(SyntaxKind.DirectCastExpression)

        Protected Overrides Function GetTypeNode(from As DirectCastExpressionSyntax) As TypeSyntax
            Return from.Type
        End Function

        Protected Overrides Function ConvertExpression(fromExpression As DirectCastExpressionSyntax, nullableContext As NullableContext, isReferenceType As Boolean) As TryCastExpressionSyntax
            Return SyntaxFactory.TryCastExpression(
                SyntaxFactory.Token(SyntaxKind.TryCastKeyword),
                fromExpression.OpenParenToken,
                fromExpression.Expression,
                fromExpression.CommaToken,
                fromExpression.Type,
                fromExpression.CloseParenToken)
        End Function
    End Class
End Namespace
