' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.EncapsulateField

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.EncapsulateField
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.EncapsulateField), [Shared]>
    Friend Class EncapsulateFieldRefactoringProvider
        Inherits AbstractEncapsulateFieldRefactoringProvider
    End Class
End Namespace
