' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
