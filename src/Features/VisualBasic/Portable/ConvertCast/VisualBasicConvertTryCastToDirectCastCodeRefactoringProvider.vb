' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertCast
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertConversionOperators
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertTryCastToDirectCast), [Shared]>
    Friend Class VisualBasicConvertTryCastToDirectCastCodeRefactoringProvider
        Inherits AbstractConvertCastCodeRefactoringProvider(Of TypeSyntax, TryCastExpressionSyntax, DirectCastExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Change_to_DirectCast
        End Function

        Protected Overrides ReadOnly Property FromKind As Integer = SyntaxKind.TryCastExpression

        Protected Overrides Function GetTypeNode(from As TryCastExpressionSyntax) As TypeSyntax
            Return from.Type
        End Function

        Protected Overrides Function ConvertExpression(fromExpression As TryCastExpressionSyntax, nullableContext As NullableContext, isReferenceType As Boolean) As DirectCastExpressionSyntax
            Return SyntaxFactory.DirectCastExpression(
                SyntaxFactory.Token(SyntaxKind.DirectCastKeyword),
                fromExpression.OpenParenToken,
                fromExpression.Expression,
                fromExpression.CommaToken,
                fromExpression.Type,
                fromExpression.CloseParenToken)
        End Function
    End Class
End Namespace
