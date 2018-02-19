' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectFunctionReturnType

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.FullyQualify
    Public Class FixIncorrectFunctionReturnTypeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New IncorrectFunctionReturnTypeCodeFixProvider())
        End Function

        <WorkItem(718494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718494")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestAsyncFunction1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module Program
    [|Async Function F()|]
        Return Nothing
    End Function
End Module",
"Imports System.Threading.Tasks
Module Program
    Async Function F() As Task
        Return Nothing
    End Function
End Module")
        End Function

        <WorkItem(718494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718494")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestAsyncFunction2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module Program
    [|Async Function F() As Integer|]
        Return Nothing
    End Function
End Module",
"Imports System.Threading.Tasks
Module Program
    Async Function F() As Task(Of Integer)
        Return Nothing
    End Function
End Module")
        End Function

        <WorkItem(718494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718494")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestAsyncFunction3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module Program
    Async Function F() As Task
        Dim a = [|Async Function() As Integer|]
                    Return Nothing
                End Function
        Return Nothing
    End Function
End Module",
"Imports System.Threading.Tasks
Module Program
    Async Function F() As Task
        Dim a = Async Function() As Task(Of Integer)
                    Return Nothing
                End Function
        Return Nothing
    End Function
End Module")
        End Function

        <WorkItem(718494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718494")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestIteratorFunction1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Collections
Imports System.Collections.Generic
Module Program
    [|Iterator Function F()|]
        Return Nothing
    End Function
End Module",
"Imports System.Collections
Imports System.Collections.Generic
Module Program
    Iterator Function F() As IEnumerable
        Return Nothing
    End Function
End Module")
        End Function

        <WorkItem(718494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718494")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestIteratorFunction2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Collections
Imports System.Collections.Generic
Module Program
    [|Iterator Function F() As Integer|]
        Return Nothing
    End Function
End Module",
"Imports System.Collections
Imports System.Collections.Generic
Module Program
    Iterator Function F() As IEnumerable(Of Integer)
        Return Nothing
    End Function
End Module")
        End Function

        <WorkItem(718494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718494")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectFunctionReturnType)>
        Public Async Function TestIteratorFunction3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Collections
Imports System.Collections.Generic
Module Program
    Async Function F() As Task
        Dim a = [|Iterator Function() As Integer|]
                    Return Nothing
                End Function
        Return Nothing
    End Function
End Module",
"Imports System.Collections
Imports System.Collections.Generic
Module Program
    Async Function F() As Task
        Dim a = Iterator Function() As IEnumerable(Of Integer)
                    Return Nothing
                End Function
        Return Nothing
    End Function
End Module")
        End Function
    End Class
End Namespace
