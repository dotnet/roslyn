' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Test.Utilities.AddMissingImports
Imports Microsoft.VisualStudio.Composition

Namespace Microsoft.CodeAnalysis.AddMissingImports

    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.AddMissingImports)>
    Public Class VisualBasicAddMissingImportsFeatureServiceTests
        Inherits AbstractAddMissingImportsFeatureServiceTest

        Private Shared ReadOnly _exportProviderFactory As Lazy(Of IExportProviderFactory) = New Lazy(Of IExportProviderFactory)(
            Function()
                ' When running tests we need to get compiler diagnostics so we can find the missing imports
                Dim catalog As ComposableCatalog = TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.
                    WithoutPartsOfType(GetType(IWorkspaceDiagnosticAnalyzerProviderService)).
                    WithPart(GetType(VisualBasicCompilerDiagnosticAnalyzerProviderService))

                Return ExportProviderCache.GetOrCreateExportProviderFactory(catalog)
            End Function)

        Public Sub New()
            MyBase.New(LanguageNames.VisualBasic)
        End Sub

        Protected Overrides Function CreateExportProvider() As ExportProvider
            Return _exportProviderFactory.Value.CreateExportProvider()
        End Function

        <Fact>
        Public Async Function AddMissingImports_NoChange_SpanIsNotMissingImports() As Task
            Dim code = "
Class [|C|]
    Dim foo As D
End Class

Namespace A
    Public Class D
    End Class
End Namespace
"

            Await AssertDocumentUnchangedAsync(code)
        End Function

        <Fact>
        Public Async Function AddMissingImports_AddedImport_SpanContainsMissingImport() As Task
            Dim code = "
Class C
    Dim foo As [|D|]
End Class

Namespace A
    Public Class D
    End Class
End Namespace
"

            Dim expected = "
Imports A

Class C
    Dim foo As D
End Class

Namespace A
    Public Class D
    End Class
End Namespace
"

            Await AssertDocumentChangedAsync(code, expected)
        End Function

        <Fact>
        Public Async Function AddMissingImports_AddedMultipleImports_SpanContainsMissingImports() As Task
            Dim code = "
Imports System

Class C
    [|Dim foo As D
    Dim bar As E|]
End Class

Namespace A
    Public Class D
    End Class
End Namespace

Namespace B
    Public Class E
    End Class
End Namespace
"

            Dim expected = "
Imports System
Imports A
Imports B

Class C
    Dim foo As D
    Dim bar As E
End Class

Namespace A
    Public Class D
    End Class
End Namespace

Namespace B
    Public Class E
    End Class
End Namespace
"

            Await AssertDocumentChangedAsync(code, expected)
        End Function

        <Fact>
        Public Async Function AddMissingImports_NoChange_SpanContainsAmbiguousMissingImport() As Task
            Dim code = "
Class C
    Dim foo As [|D|]
End Class

Namespace A
    Public Class D
    End Class
End Namespace

Namespace B
    Public Class D
    End Class
End Namespace
"

            Await AssertDocumentUnchangedAsync(code)
        End Function

        <Fact>
        Public Async Function AddMissingImports_PartialFix_SpanContainsFixableAndAmbiguousMissingImports() As Task
            Dim code = "
Imports System

Class C
    [|Dim foo As D
    Dim bar As E|]
End Class

Namespace A
    Public Class D
    End Class
End Namespace

Namespace B
    Public Class D
    End Class

    Public Class E
    End Class
End Namespace
"

            Dim expected = "
Imports System
Imports B

Class C
    Dim foo As D
    Dim bar As E
End Class

Namespace A
    Public Class D
    End Class
End Namespace

Namespace B
    Public Class D
    End Class

    Public Class E
    End Class
End Namespace
"

            Await AssertDocumentChangedAsync(code, expected)
        End Function
    End Class
End Namespace
