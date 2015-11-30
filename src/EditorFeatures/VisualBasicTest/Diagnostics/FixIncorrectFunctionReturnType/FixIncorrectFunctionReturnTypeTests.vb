' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectFunctionReturnType

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.FullyQualify
    Public Class FixIncorrectFunctionReturnTypeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New IncorrectFunctionReturnTypeCodeFixProvider())
        End Function

        <WorkItem(718494)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestAsyncFunction1() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module Program \n [|Async Function F()|] \n Return Nothing \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function F() As Task \n Return Nothing \n End Function \n End Module"))
        End Function

        <WorkItem(718494)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestAsyncFunction2() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module Program \n [|Async Function F() As   Integer|]   \n Return Nothing \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function F() As   Task(Of Integer)   \n Return Nothing \n End Function \n End Module"))
        End Function

        <WorkItem(718494)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestAsyncFunction3() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function F() As Task \n Dim a = [|Async Function() As Integer|] \n Return Nothing \n End Function\n Return Nothing \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function F() As Task \n Dim a = Async Function() As Task(Of Integer) \n Return Nothing \n End Function\n Return Nothing \n End Function \n End Module"))
        End Function

        <WorkItem(718494)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestIteratorFunction1() As Task
            Await TestAsync(
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Module Program \n [|Iterator Function F()|] \n Return Nothing \n End Function \n End Module"),
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Module Program \n Iterator Function F() As IEnumerable \n Return Nothing \n End Function \n End Module"))
        End Function

        <WorkItem(718494)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestIteratorFunction2() As Task
            Await TestAsync(
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Module Program \n [|Iterator Function F() As   Integer|]   \n Return Nothing \n End Function \n End Module"),
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Module Program \n Iterator Function F() As   IEnumerable(Of Integer)   \n Return Nothing \n End Function \n End Module"))
        End Function

        <WorkItem(718494)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestIteratorFunction3() As Task
            Await TestAsync(
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Module Program \n Async Function F() As Task \n Dim a = [|Iterator Function() As Integer|] \n Return Nothing \n End Function\n Return Nothing \n End Function \n End Module"),
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Module Program \n Async Function F() As Task \n Dim a = Iterator Function() As IEnumerable(Of Integer) \n Return Nothing \n End Function\n Return Nothing \n End Function \n End Module"))
        End Function
    End Class
End Namespace
