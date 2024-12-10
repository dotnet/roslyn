' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.InitializeParameter
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InitializeParameter
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.AddParameterCheck), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.ChangeSignature)>
    Friend Class VisualBasicAddParameterCheckCodeRefactoringProvider
        Inherits AbstractAddParameterCheckCodeRefactoringProvider(Of
            TypeBlockSyntax,
            ParameterSyntax,
            StatementSyntax,
            ExpressionSyntax,
            BinaryExpressionSyntax,
            VisualBasicSimplifierOptions)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function IsFunctionDeclaration(node As SyntaxNode) As Boolean
            Return InitializeParameterHelpers.IsFunctionDeclaration(node)
        End Function

        Protected Overrides Function GetBody(functionDeclaration As SyntaxNode) As SyntaxNode
            Return InitializeParameterHelpers.GetBody(functionDeclaration)
        End Function

        Protected Overrides Function IsImplicitConversion(compilation As Compilation, source As ITypeSymbol, destination As ITypeSymbol) As Boolean
            Return InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination)
        End Function

        Protected Overrides Function CanOffer(body As SyntaxNode) As Boolean
            Return True
        End Function

        Protected Overrides Function PrefersThrowExpression(options As VisualBasicSimplifierOptions) As Boolean
            ' No throw expression preference option is defined for VB because it doesn't support throw expressions.
            Return False
        End Function

        Protected Overrides Function EscapeResourceString(input As String) As String
            Return input.Replace("""", """""")
        End Function

        Protected Overrides Function CreateParameterCheckIfStatement(condition As ExpressionSyntax, ifTrueStatement As StatementSyntax, options As VisualBasicSimplifierOptions) As StatementSyntax
            Return SyntaxFactory.MultiLineIfBlock(
                ifStatement:=SyntaxFactory.IfStatement(SyntaxFactory.Token(SyntaxKind.IfKeyword), condition, SyntaxFactory.Token(SyntaxKind.ThenKeyword)),
                statements:=New SyntaxList(Of StatementSyntax)(ifTrueStatement),
                elseIfBlocks:=Nothing,
                elseBlock:=Nothing)
        End Function
    End Class
End Namespace
