' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
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

                ' This tests the paste tracking service's resiliancy to failing when multiple pasted spans are
                ' registered consecutively And that the last registered span wins.
                PasteTrackingService.RegisterPastedTextSpan(hostDocument.TextBuffer, Nothing)
                PasteTrackingService.RegisterPastedTextSpan(hostDocument.TextBuffer, pastedTextSpan)
            End If

            Return Workspace
        End Function

        Private Overloads Function TestInRegularAndScriptAsync(
            initialMarkup As String, expectedMarkup As String,
            placeSystemNamespaceFirst As Boolean, separateImportDirectiveGroups As Boolean) As Task

            Dim options = OptionsSet(
                SingleOption(GenerationOptions.PlaceSystemNamespaceFirst, placeSystemNamespaceFirst),
                SingleOption(GenerationOptions.SeparateImportDirectiveGroups, separateImportDirectiveGroups))

            Return TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options:=options)
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
        Public Async Function AddMissingImports_AddImportsBelowSystem_PlaceSystemFirstPasteContainsMultipleMissingImports() As Task
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

            Await TestInRegularAndScriptAsync(code, expected, placeSystemNamespaceFirst:=True, separateImportDirectiveGroups:=False)
        End Function

        <WpfFact>
        Public Async Function AddMissingImports_AddImportsAboveSystem_DontPlaceSystemFirstPasteContainsMultipleMissingImports() As Task
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
Imports A
Imports B
Imports System

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

            Await TestInRegularAndScriptAsync(code, expected, placeSystemNamespaceFirst:=False, separateImportDirectiveGroups:=False)
        End Function

        <WpfFact>
        Public Async Function AddMissingImports_AddImportsUngrouped_SeparateImportGroupsPasteContainsMultipleMissingImports() As Task '
            ' The current fixes for AddImport diagnostics do not consider whether imports should be grouped.
            ' This test documents this behavior and is a reminder that when the behavior changes 
            ' AddMissingImports is also affected and should be considered.

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
Imports A
Imports B
Imports System

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

            Await TestInRegularAndScriptAsync(code, expected, placeSystemNamespaceFirst:=False, separateImportDirectiveGroups:=True)
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

        <WorkItem(31768, "https://github.com/dotnet/roslyn/issues/31768")>
        <WpfFact>
        Public Async Function AddMissingImports_AddMultipleImports_NoPreviousImports() As Task
            Dim code = "
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

            Await TestInRegularAndScriptAsync(code, expected, placeSystemNamespaceFirst:=False, separateImportDirectiveGroups:=False)
        End Function
    End Class
End Namespace
