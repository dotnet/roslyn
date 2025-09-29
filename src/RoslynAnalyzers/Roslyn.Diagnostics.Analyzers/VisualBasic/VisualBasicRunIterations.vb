' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic)>
    <[Shared]>
    Public Class VisualBasicRunIterations
        Inherits AbstractRunIterations(Of MethodStatementSyntax)

        <ImportingConstructor>
        <Obsolete("This exported object must be obtained through the MEF export provider.", True)>
        Public Sub New()
        End Sub

        Private Protected Overrides ReadOnly Property RefactoringHelpers As IRefactoringHelpers
            Get
                Return VisualBasicRefactoringHelpers.Instance
            End Get
        End Property
    End Class
End Namespace
