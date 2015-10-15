' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ExtractInterface

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ExtractInterface
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ExtractInterface), [Shared]>
    Friend Class ExtractInterfaceCodeRefactoringProvider
        Inherits AbstractExtractInterfaceCodeRefactoringProvider

    End Class
End Namespace
