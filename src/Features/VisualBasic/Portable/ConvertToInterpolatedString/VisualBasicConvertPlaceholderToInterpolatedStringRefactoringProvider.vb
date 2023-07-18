' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertToInterpolatedString
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertPlaceholderToInterpolatedString), [Shared]>
    Partial Friend Class VisualBasicConvertPlaceholderToInterpolatedStringRefactoringProvider
        Inherits AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider(Of
            ExpressionSyntax,
            LiteralExpressionSyntax,
            InvocationExpressionSyntax,
            InterpolatedStringExpressionSyntax,
            ArgumentSyntax,
            ArgumentListSyntax,
            InterpolationSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function ParseExpression(text As String) As ExpressionSyntax
            Return SyntaxFactory.ParseExpression(text)
        End Function
    End Class
End Namespace
