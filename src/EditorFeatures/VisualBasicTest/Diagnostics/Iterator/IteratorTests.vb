' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.Iterator
    Public Class ConvertToIteratorTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New VisualBasicConvertToIteratorCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
        Public Async Function TestConvertToIteratorFunction() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n \n Module Module1 \n Function M() As IEnumerable(Of Integer) \n [|Yield|] 1 \n End Function \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n \n Module Module1 \n Iterator Function M() As IEnumerable(Of Integer) \n Yield 1 \n End Function \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
        Public Async Function TestConvertToIteratorSub() As Task
            Await TestMissingAsync(
NewLines("Module Module1 \n Sub M() As \n [|Yield|] 1 \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
        Public Async Function TestConvertToIteratorFunctionLambda() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n \n Module Module1 \n Sub M() \n Dim a As Func(Of IEnumerable(Of Integer)) = Function() \n [|Yield|] 0 \n End Function \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n \n Module Module1 \n Sub M() \n Dim a As Func(Of IEnumerable(Of Integer)) = Iterator Function() \n Yield 0 \n End Function \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
        Public Async Function TestConvertToIteratorSubLambda() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n \n Module Module1 \n Sub M() \n Dim a As Func(Of IEnumerable(Of Integer)) = Sub() \n [|Yield|] 0 \n End Sub \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
        Public Async Function TestConvertToIteratorSingleLineFunctionLambda() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n \n Module Module1 \n Sub M() \n Dim a As Func(Of IEnumerable(Of Integer)) = Function() [|Yield|] 0 \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
        Public Async Function TestConvertToIteratorSingleLineSubLambda() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n \n Module Module1 \n Sub M() \n Dim a As Func(Of IEnumerable(Of Integer)) = Sub() [|Yield|] 0 \n End Sub \n End Module"))
        End Function
    End Class

    Public Class ChangeToYieldTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New VisualBasicChangeToYieldCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
        Public Async Function TestChangeToYieldCodeFixProviderFunction() As Task
            Await TestAsync(
NewLines("Module Module1 \n Iterator Function M() As IEnumerable(Of Integer) \n [|Return|] 1 \n End Function \n End Module"),
NewLines("Module Module1 \n Iterator Function M() As IEnumerable(Of Integer) \n Yield 1 \n End Function \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
        Public Async Function TestChangeToYieldCodeFixProviderSub() As Task
            Await TestAsync(
NewLines("Module Module1 \n Iterator Sub M() \n [|Return|] 1 \n End Sub \n End Module"),
NewLines("Module Module1 \n Iterator Sub M() \n Yield 1 \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
        Public Async Function TestChangeToYieldCodeFixProviderFunctionLambda() As Task
            Await TestAsync(
NewLines("Module Module1 \n Sub M() \n Dim a = Iterator Function() \n [|Return|] 0 \n End Function \n End Sub \n End Module"),
NewLines("Module Module1 \n Sub M() \n Dim a = Iterator Function() \n Yield 0 \n End Function \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
        Public Async Function TestChangeToYieldCodeFixProviderSubLambda() As Task
            Await TestAsync(
NewLines("Module Module1 \n Sub M() \n Dim a = Iterator Sub() \n [|Return|] 0 \n End Sub \n End Sub \n End Module"),
NewLines("Module Module1 \n Sub M() \n Dim a = Iterator Sub() \n Yield 0 \n End Sub \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
        Public Async Function TestChangeToYieldCodeFixProviderSingleLineFunctionLambda() As Task
            Await TestMissingAsync(NewLines("Module Module1 \n Sub M() \n Dim a = Iterator Function() [|Return|] 0 \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
        Public Async Function TestChangeToYieldCodeFixProviderSingleLineSubLambda() As Task
            Await TestAsync(
NewLines("Module Module1 \n Sub M() \n Dim a = Iterator Sub() [|Return|] 0 \n End Sub \n End Module"),
NewLines("Module Module1 \n Sub M() \n Dim a = Iterator Sub() Yield 0 \n End Sub \n End Module"))
        End Function

    End Class
End Namespace

