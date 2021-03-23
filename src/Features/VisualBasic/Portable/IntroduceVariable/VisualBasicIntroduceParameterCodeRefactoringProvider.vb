' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.IntroduceVariable
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicIntroduceParameterCodeRefactoringProvider
        Inherits AbstractIntroduceParameterService(Of ExpressionSyntax, InvocationExpressionSyntax, IdentifierNameSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GenerateExpressionFromOptionalParameter(parameterSymbol As IParameterSymbol) As SyntaxNode
            Return GenerateExpression(parameterSymbol.Type, parameterSymbol.ExplicitDefaultValue, canUseFieldReference:=True)
        End Function

        Protected Overrides Function GetContainingParameterizedDeclaration(node As SyntaxNode) As Boolean
            Select Case node.Kind()
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.FunctionLambdaHeader,
                     SyntaxKind.SubLambdaHeader
                    Return True
            End Select

            Return False
        End Function
    End Class
End Namespace
