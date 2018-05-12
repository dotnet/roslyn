' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Partial Friend NotInheritable Class VisualBasicInvertIfCodeRefactoringProvider
        Inherits AbstractInvertIfCodeRefactoringProvider

        Protected Overrides Function GetIfStatement(token As SyntaxToken) As SyntaxNode
            Return If(DirectCast(token.GetAncestor(Of SingleLineIfStatementSyntax), SyntaxNode),
                      DirectCast(token.GetAncestor(Of MultiLineIfBlockSyntax), SyntaxNode))
        End Function

        Protected Overrides Function GetAnalyzer(ifStatement As SyntaxNode) As IAnalyzer
            Return If(TypeOf ifStatement Is SingleLineIfStatementSyntax,
                      DirectCast(New SingleLineIfStatementAnalyzer, IAnalyzer),
                      DirectCast(New MultiLineIfStatementAnalyzer, IAnalyzer))
        End Function
    End Class
End Namespace
