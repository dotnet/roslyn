' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(BasicEnableConcurrentExecutionFix)), [Shared]>
    Public Class BasicEnableConcurrentExecutionFix
        Inherits EnableConcurrentExecutionFix

        Protected Overrides Function GetStatements(methodDeclaration As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Dim method = TryCast(methodDeclaration, MethodBlockSyntax)
            Return method.Statements
        End Function
    End Class
End Namespace