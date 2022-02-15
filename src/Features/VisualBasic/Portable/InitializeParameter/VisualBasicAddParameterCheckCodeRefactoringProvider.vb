' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.InitializeParameter
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
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
            BinaryExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides Function SupportsRecords(options As ParseOptions) As Boolean
            Return False
        End Function

        Protected Overrides Function IsFunctionDeclaration(node As SyntaxNode) As Boolean
            Return InitializeParameterHelpers.IsFunctionDeclaration(node)
        End Function

        Protected Overrides Function GetBody(functionDeclaration As SyntaxNode) As SyntaxNode
            Return InitializeParameterHelpers.GetBody(functionDeclaration)
        End Function

        Protected Overrides Sub InsertStatement(editor As SyntaxEditor, functionDeclaration As SyntaxNode, returnsVoid As Boolean, statementToAddAfterOpt As SyntaxNode, statement As StatementSyntax)
            InitializeParameterHelpers.InsertStatement(editor, functionDeclaration, statementToAddAfterOpt, statement)
        End Sub

        Protected Overrides Function IsImplicitConversion(compilation As Compilation, source As ITypeSymbol, destination As ITypeSymbol) As Boolean
            Return InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination)
        End Function

        Protected Overrides Function CanOffer(body As SyntaxNode) As Boolean
            Return True
        End Function

        Protected Overrides Function PrefersThrowExpression(options As DocumentOptionSet) As Boolean
            ' No throw expression preference option is defined for VB because it doesn't support throw expressions.
            Return False
        End Function

        Protected Overrides Function EscapeResourceString(input As String) As String
            Return input.Replace("""", """""")
        End Function

        Protected Overrides Function CreateParameterCheckIfStatement(options As DocumentOptionSet, condition As ExpressionSyntax, ifTrueStatement As StatementSyntax) As StatementSyntax
            Return SyntaxFactory.MultiLineIfBlock(
                ifStatement:=SyntaxFactory.IfStatement(SyntaxFactory.Token(SyntaxKind.IfKeyword), condition, SyntaxFactory.Token(SyntaxKind.ThenKeyword)),
                statements:=New SyntaxList(Of StatementSyntax)(ifTrueStatement),
                elseIfBlocks:=Nothing,
                elseBlock:=Nothing)
        End Function

        Protected Overrides Function TryAddNullCheckToParameterDeclarationAsync(document As Document, parameterSyntax As ParameterSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Return Task.FromResult(Of Document)(Nothing)
        End Function
    End Class
End Namespace
