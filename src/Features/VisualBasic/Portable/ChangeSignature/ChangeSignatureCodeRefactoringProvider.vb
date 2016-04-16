' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.CodeRefactorings

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ChangeSignature
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ChangeSignature), [Shared]>
    Friend Class ChangeSignatureCodeRefactoringProvider
        Inherits AbstractChangeSignatureCodeRefactoringProvider

    End Class
End Namespace
