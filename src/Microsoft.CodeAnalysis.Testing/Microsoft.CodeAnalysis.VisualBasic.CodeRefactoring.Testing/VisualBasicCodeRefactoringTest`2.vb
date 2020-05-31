' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicCodeRefactoringTest(Of TCodeRefactoring As {CodeRefactoringProvider, New}, TVerifier As {IVerifier, New})
    Inherits CodeRefactoringTest(Of TVerifier)

    Private Shared ReadOnly DefaultLanguageVersion As LanguageVersion =
        If([Enum].TryParse("Default", DefaultLanguageVersion), DefaultLanguageVersion, LanguageVersion.VisualBasic14)

    Public Overrides ReadOnly Property Language As String
        Get
            Return LanguageNames.VisualBasic
        End Get
    End Property

    Public Overrides ReadOnly Property SyntaxKindType As Type
        Get
            Return GetType(SyntaxKind)
        End Get
    End Property

    Protected Overrides ReadOnly Property DefaultFileExt As String
        Get
            Return "vb"
        End Get
    End Property

    Protected Overrides Function CreateCompilationOptions() As CompilationOptions
        Return New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    End Function

    Protected Overrides Function CreateParseOptions() As ParseOptions
        Return New VisualBasicParseOptions(DefaultLanguageVersion, DocumentationMode.Diagnose)
    End Function

    Protected Overrides Function GetCodeRefactoringProviders() As IEnumerable(Of CodeRefactoringProvider)
        Return New TCodeRefactoring() {New TCodeRefactoring()}
    End Function
End Class
