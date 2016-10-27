' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
"Imports System
Imports System.Collections.Generic

Module Module1
    Function M() As IEnumerable(Of Integer)
        [|Yield|] 1 
 End Function
End Module",
"Imports System
Imports System.Collections.Generic

Module Module1
    Iterator Function M() As IEnumerable(Of Integer)
        Yield 1
    End Function
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
        Public Async Function TestConvertToIteratorSub() As Task
            Await TestMissingAsync(
"Module Module1
    Sub M() As 
 [|Yield|] 1 
 End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
        Public Async Function TestConvertToIteratorFunctionLambda() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic

Module Module1
    Sub M()
        Dim a As Func(Of IEnumerable(Of Integer)) = Function()
                                                        [|Yield|] 0 
 End Function
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic

Module Module1
    Sub M()
        Dim a As Func(Of IEnumerable(Of Integer)) = Iterator Function()
                                                        Yield 0
                                                    End Function
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
        Public Async Function TestConvertToIteratorSubLambda() As Task
            Await TestMissingAsync(
"Imports System
Imports System.Collections.Generic

Module Module1
    Sub M()
        Dim a As Func(Of IEnumerable(Of Integer)) = Sub()
                                                        [|Yield|] 0 
 End Sub
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
        Public Async Function TestConvertToIteratorSingleLineFunctionLambda() As Task
            Await TestMissingAsync(
"Imports System
Imports System.Collections.Generic

Module Module1
    Sub M()
        Dim a As Func(Of IEnumerable(Of Integer)) = Function() [|Yield|] 0 
 End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
        Public Async Function TestConvertToIteratorSingleLineSubLambda() As Task
            Await TestMissingAsync(
"Imports System
Imports System.Collections.Generic

Module Module1
    Sub M()
        Dim a As Func(Of IEnumerable(Of Integer)) = Sub() [|Yield|] 0 
 End Sub
End Module")
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
"Module Module1
    Iterator Function M() As IEnumerable(Of Integer)
        [|Return|] 1
    End Function
End Module",
"Module Module1
    Iterator Function M() As IEnumerable(Of Integer)
        Yield 1
    End Function
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
        Public Async Function TestChangeToYieldCodeFixProviderSub() As Task
            Await TestAsync(
"Module Module1
    Iterator Sub M()
        [|Return|] 1
    End Sub
End Module",
"Module Module1
    Iterator Sub M()
        Yield 1
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
        Public Async Function TestChangeToYieldCodeFixProviderFunctionLambda() As Task
            Await TestAsync(
"Module Module1
    Sub M()
        Dim a = Iterator Function()
                    [|Return|] 0
                End Function
    End Sub
End Module",
"Module Module1
    Sub M()
        Dim a = Iterator Function()
                    Yield 0
                End Function
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
        Public Async Function TestChangeToYieldCodeFixProviderSubLambda() As Task
            Await TestAsync(
"Module Module1
    Sub M()
        Dim a = Iterator Sub()
                    [|Return|] 0
                End Sub
    End Sub
End Module",
"Module Module1
    Sub M()
        Dim a = Iterator Sub()
                    Yield 0
                End Sub
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
        Public Async Function TestChangeToYieldCodeFixProviderSingleLineFunctionLambda() As Task
            Await TestMissingAsync("Module Module1
    Sub M()
        Dim a = Iterator Function() [|Return|] 0 
 End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
        Public Async Function TestChangeToYieldCodeFixProviderSingleLineSubLambda() As Task
            Await TestAsync(
"Module Module1
    Sub M()
        Dim a = Iterator Sub() [|Return|] 0
    End Sub
End Module",
"Module Module1
    Sub M()
        Dim a = Iterator Sub() Yield 0
    End Sub
End Module")
        End Function

    End Class
End Namespace

