' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.PasteTracking

Namespace Microsoft.CodeAnalysis.AddMissingImports

    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.AddMissingImports)>
    Public Class VisualBasicAddMissingImportsRefactoringProviderTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Dim testWorkspace = DirectCast(workspace, TestWorkspace)
            Dim pasteTrackingService = testWorkspace.ExportProvider.GetExportedValue(Of PasteTrackingService)()
            Return New VisualBasicAddMissingImportsRefactoringProvider(pasteTrackingService)
        End Function

        Protected Overrides Function CreateWorkspaceFromFile(initialMarkup As String, parameters As TestParameters) As TestWorkspace
            Dim Workspace = TestWorkspace.CreateVisualBasic(initialMarkup)

            ' Treat the span being tested as the pasted span
            Dim hostDocument = Workspace.Documents.First()
            Dim pastedTextSpan = hostDocument.SelectedSpans.FirstOrDefault()

            If Not pastedTextSpan.IsEmpty Then
                Dim PasteTrackingService = Workspace.ExportProvider.GetExportedValue(Of PasteTrackingService)()
                PasteTrackingService.RegisterPastedTextSpan(hostDocument.TextBuffer, pastedTextSpan)
            End If

            Return Workspace
        End Function

        <WpfFact>
        Public Async Function AddMissingImports_NoAction_PasteIsNotMissingImports() As Task
            Dim code = "
Class [|C|]
    Dim foo As D
End Class

Namespace A
    Public Class D
    End Class
End Namespace
"

            Await TestMissingInRegularAndScriptAsync(code)
        End Function

        <WpfFact>
        Public Async Function AddMissingImports_AddImport_PasteContainsSingleMissingImport() As Task
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

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <WpfFact>
        Public Async Function AddMissingImports_AddImports_PasteContainsMultipleMissingImports() As Task
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

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <WpfFact>
        Public Async Function AddMissingImports_NoAction_NoPastedSpan() As Task
            Dim code = "
Class C
    Dim foo As D[||]
End Class

Namespace A
    Public Class D
    End Class
End Namespace
"

            Await TestMissingInRegularAndScriptAsync(code)
        End Function

        <WpfFact>
        Public Async Function AddMissingImports_NoAction_PasteContainsAmibiguousMissingImport() As Task
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

            Await TestMissingInRegularAndScriptAsync(code)
        End Function

        <WpfFact>
        Public Async Function AddMissingImports_PartialFix_PasteContainsFixableAndAmbiguousMissingImports() As Task
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

            Await TestInRegularAndScriptAsync(code, expected)
        End Function
    End Class
End Namespace
