' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ExtractMethod
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ExtractMethod), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    Friend Class ExtractMethodCodeRefactoringProvider
        Inherits AbstractExtractMethodCodeRefactoringProvider
    End Class
End Namespace
