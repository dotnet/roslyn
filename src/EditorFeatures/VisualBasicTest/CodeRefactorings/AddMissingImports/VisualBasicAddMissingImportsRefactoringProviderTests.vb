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
                PasteTrackingService.RegisterPastedTextSpan(hostDocument.GetTextBuffer(), Nothing)
                PasteTrackingService.RegisterPastedTextSpan(hostDocument.GetTextBuffer(), pastedTextSpan)
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

        <WorkItem(39155, "https://github.com/dotnet/roslyn/issues/39155")>
        <WpfFact>
        Public Async Function AddMissingImports_Extension() As Task
            Dim code = "
Imports System.Runtime.CompilerServices

Class Foo
    Sub M(f As Foo)
        [|f.M1()|]
    End Sub
End Class

Namespace N
    Public Module M
        <Extension>
        Public Sub M1(f As Foo)
        End Sub
    End Module
End Namespace
"
            Dim expected = "
Imports System.Runtime.CompilerServices
Imports N

Class Foo
    Sub M(f As Foo)
        f.M1()
    End Sub
End Class

Namespace N
    Public Module M
        <Extension>
        Public Sub M1(f As Foo)
        End Sub
    End Module
End Namespace
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <WorkItem(39155, "https://github.com/dotnet/roslyn/issues/39155")>
        <WpfFact>
        Public Async Function AddMissingImports_Extension_Overload() As Task
            Dim code = "
Imports System.Runtime.CompilerServices

Class Foo
    Sub M(f As Foo)
        [|f.M1()|]
    End Sub
End Class

Public Module M2
    <Extension>
    Public Sub M1(f As String)
    End Sub
End Module

Namespace N
    Public Module M
        <Extension>
        Public Sub M1(f As Foo)
        End Sub
    End Module
End Namespace
"
            Dim expected = "
Imports System.Runtime.CompilerServices
Imports N

Class Foo
    Sub M(f As Foo)
        f.M1()
    End Sub
End Class

Public Module M2
    <Extension>
    Public Sub M1(f As String)
    End Sub
End Module

Namespace N
    Public Module M
        <Extension>
        Public Sub M1(f As Foo)
        End Sub
    End Module
End Namespace
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <WorkItem(39155, "https://github.com/dotnet/roslyn/issues/39155")>
        <WpfFact>
        Public Async Function AddMissingImports_Extension_Await() As Task
            Dim code = "
Imports System.Runtime.CompilerServices

Public Class Foo
    Async Sub M(f As Foo)
        [|Await f|]
    End Sub
End Class

Namespace N
    Public Module FooExtensions
        <Extension>
        Public Function GetAwaiter(f As Foo) As FooAwaiter
            Return New FooAwaiter
        End Function
    End Module

    Public Structure FooAwaiter
        Implements INotifyCompletion
        Public ReadOnly Property IsCompleted As Boolean

        Public Sub OnCompleted(continuation As Action) Implements INotifyCompletion.OnCompleted
        End Sub

        Public Sub GetResult()
        End Sub
    End Structure
End Namespace
"
            Dim expected = "
Imports System.Runtime.CompilerServices
Imports N

Public Class Foo
    Async Sub M(f As Foo)
        Await f
    End Sub
End Class

Namespace N
    Public Module FooExtensions
        <Extension>
        Public Function GetAwaiter(f As Foo) As FooAwaiter
            Return New FooAwaiter
        End Function
    End Module

    Public Structure FooAwaiter
        Implements INotifyCompletion
        Public ReadOnly Property IsCompleted As Boolean

        Public Sub OnCompleted(continuation As Action) Implements INotifyCompletion.OnCompleted
        End Sub

        Public Sub GetResult()
        End Sub
    End Structure
End Namespace
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <WorkItem(39155, "https://github.com/dotnet/roslyn/issues/39155")>
        <WpfFact>
        Public Async Function AddMissingImports_Extension_Await_Overload() As Task
            Dim code = "
Imports System.Runtime.CompilerServices

Public Class Foo
    Async Sub M(f As Foo)
        [|Await f|]
    End Sub
End Class

Public Module BarExtensions
    <Extension()>
    Public Function GetAwaiter(f As String) As N.FooAwaiter
        Return New N.FooAwaiter
    End Function
End Module

Namespace N
    Public Module FooExtensions
        <Extension>
        Public Function GetAwaiter(f As Foo) As FooAwaiter
            Return New FooAwaiter
        End Function
    End Module

    Public Structure FooAwaiter
        Implements INotifyCompletion
        Public ReadOnly Property IsCompleted As Boolean

        Public Sub OnCompleted(continuation As Action) Implements INotifyCompletion.OnCompleted
        End Sub

        Public Sub GetResult()
        End Sub
    End Structure
End Namespace
"
            Dim expected = "
Imports System.Runtime.CompilerServices
Imports N

Public Class Foo
    Async Sub M(f As Foo)
        Await f
    End Sub
End Class

Public Module BarExtensions
    <Extension()>
    Public Function GetAwaiter(f As String) As N.FooAwaiter
        Return New N.FooAwaiter
    End Function
End Module

Namespace N
    Public Module FooExtensions
        <Extension>
        Public Function GetAwaiter(f As Foo) As FooAwaiter
            Return New FooAwaiter
        End Function
    End Module

    Public Structure FooAwaiter
        Implements INotifyCompletion
        Public ReadOnly Property IsCompleted As Boolean

        Public Sub OnCompleted(continuation As Action) Implements INotifyCompletion.OnCompleted
        End Sub

        Public Sub GetResult()
        End Sub
    End Structure
End Namespace
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <WorkItem(39155, "https://github.com/dotnet/roslyn/issues/39155")>
        <WpfFact>
        Public Async Function AddMissingImports_Extension_Select() As Task
            Dim code = "
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Public Class Foo
    Sub M(f As Foo)
        Dim u = [|From x In f|] Select x
    End Sub
End Class

Namespace N
    Public Module FooExtensions
        <Extension>
        Public Function [Select](f As Foo, func As Func(Of Integer, Integer)) As IEnumerable(Of Integer)
            Return Nothing
        End Function
    End Module
End Namespace
"
            Dim expected = "
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports N

Public Class Foo
    Sub M(f As Foo)
        Dim u = From x In f Select x
    End Sub
End Class

Namespace N
    Public Module FooExtensions
        <Extension>
        Public Function [Select](f As Foo, func As Func(Of Integer, Integer)) As IEnumerable(Of Integer)
            Return Nothing
        End Function
    End Module
End Namespace
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <WorkItem(39155, "https://github.com/dotnet/roslyn/issues/39155")>
        <WpfFact>
        Public Async Function AddMissingImports_Extension_Select_Overload() As Task
            Dim code = "
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Public Class Foo
    Sub M(f As Foo)
        Dim u = [|From x In f|] Select x
    End Sub
End Class

Public Module BarExtensions
    <Extension>
    Public Function [Select](f As String, func As Func(Of Integer, Integer)) As IEnumerable(Of Integer)
        Return Nothing
    End Function
End Module

Namespace N
    Public Module FooExtensions
        <Extension>
        Public Function [Select](f As Foo, func As Func(Of Integer, Integer)) As IEnumerable(Of Integer)
            Return Nothing
        End Function
    End Module
End Namespace
"
            Dim expected = "
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports N

Public Class Foo
    Sub M(f As Foo)
        Dim u = From x In f Select x
    End Sub
End Class

Public Module BarExtensions
    <Extension>
    Public Function [Select](f As String, func As Func(Of Integer, Integer)) As IEnumerable(Of Integer)
        Return Nothing
    End Function
End Module

Namespace N
    Public Module FooExtensions
        <Extension>
        Public Function [Select](f As Foo, func As Func(Of Integer, Integer)) As IEnumerable(Of Integer)
            Return Nothing
        End Function
    End Module
End Namespace
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function
    End Class
End Namespace
