' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.InvertLogical
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InvertLogical
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertLogical), [Shared]>
    Friend Class VisualBasicInvertLogicalCodeRefactoringProvider
        Inherits AbstractInvertLogicalCodeRefactoringProvider(Of SyntaxKind, ExpressionSyntax, BinaryExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetOperatorText(binaryExprKind As SyntaxKind) As String
            Return If(binaryExprKind = SyntaxKind.AndAlsoExpression,
                      SyntaxFacts.GetText(SyntaxKind.AndAlsoKeyword),
                      SyntaxFacts.GetText(SyntaxKind.OrElseKeyword))
        End Function
    End Class
End Namespace
