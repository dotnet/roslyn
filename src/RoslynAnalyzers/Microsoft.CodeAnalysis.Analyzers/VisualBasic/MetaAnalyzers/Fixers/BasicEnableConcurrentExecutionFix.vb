' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(BasicEnableConcurrentExecutionFix)), [Shared]>
    Public Class BasicEnableConcurrentExecutionFix
        Inherits EnableConcurrentExecutionFix

        <ImportingConstructor>
        <Obsolete("This exported object must be obtained through the MEF export provider.", True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetStatements(methodDeclaration As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Dim method = TryCast(methodDeclaration, MethodBlockSyntax)
            Return method.Statements
        End Function
    End Class
End Namespace
