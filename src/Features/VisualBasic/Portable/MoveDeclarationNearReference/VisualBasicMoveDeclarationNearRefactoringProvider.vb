' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.MoveDeclarationNearReference
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MoveDeclarationNearReference
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.MoveDeclarationNearReference), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.InlineTemporary)>
    Class VisualBasicMoveDeclarationNearReferenceCodeRefactoringProvider
        Inherits AbstractMoveDeclarationNearReferenceCodeRefactoringProvider(Of LocalDeclarationStatementSyntax)
    End Class
End Namespace
