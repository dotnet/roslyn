' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.MoveType

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.MoveType
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.MoveTypeToFile), [Shared]>
    Friend Class MoveTypeCodeRefactoringProvider
        Inherits AbstractMoveTypeCodeRefactoringProvider
    End Class
End Namespace
