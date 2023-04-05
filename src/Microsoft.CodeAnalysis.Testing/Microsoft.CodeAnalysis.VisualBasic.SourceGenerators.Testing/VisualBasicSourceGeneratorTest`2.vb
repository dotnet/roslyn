' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicSourceGeneratorTest(Of TSourceGenerator As New, TVerifier As {IVerifier, New})
    Inherits SourceGeneratorTest(Of TVerifier)

    Private Shared ReadOnly DefaultLanguageVersion As LanguageVersion =
        If([Enum].TryParse("Default", DefaultLanguageVersion), DefaultLanguageVersion, LanguageVersion.VisualBasic14)

    Public Overrides ReadOnly Property Language As String
        Get
            Return LanguageNames.VisualBasic
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

    Protected Overrides Function GetSourceGenerators() As IEnumerable(Of Type)
        Return New Type() {GetType(TSourceGenerator)}
    End Function
End Class
