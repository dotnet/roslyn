' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.InitializeParameter
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InitializeParameter
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicInitializeParameterCodeRefactoringProvider)), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.ChangeSignature)>
    Friend Class VisualBasicInitializeParameterCodeRefactoringProvider
        Inherits AbstractInitializeParameterCodeRefactoringProvider(Of
            ParameterSyntax,
            MethodBlockBaseSyntax,
            StatementSyntax,
            ExpressionSyntax,
            BinaryExpressionSyntax)

        Protected Overrides Function GetBody(containingMember As MethodBlockBaseSyntax) As SyntaxNode
            Return containingMember
        End Function

        Protected Overrides Function IsImplicitConversion(compilation As Compilation, source As ITypeSymbol, destination As ITypeSymbol) As Boolean
            Return compilation.ClassifyConversion(source:=source, destination:=destination).IsWidening
        End Function

        Protected Overrides Sub InsertStatement(
                editor As SyntaxEditor,
                body As SyntaxNode,
                statementToAddAfterOpt As IOperation,
                statement As StatementSyntax)
            Dim methodBlock = DirectCast(body, MethodBlockBaseSyntax)
            Dim statements = methodBlock.Statements

            If statementToAddAfterOpt IsNot Nothing Then
                editor.InsertAfter(statementToAddAfterOpt.Syntax, statement)
            Else
                Dim newStatements = statements.Insert(0, statement)
                editor.SetStatements(body, newStatements)
            End If
        End Sub
    End Class
End Namespace