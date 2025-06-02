' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Testing
Imports VerifyConvertToIterator = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer, Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator.VisualBasicConvertToIteratorCodeFixProvider)
Imports VerifyConvertToYield = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer, Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator.VisualBasicChangeToYieldCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.Iterator
    <Trait(Traits.Feature, Traits.Features.CodeActionsConvertToIterator)>
    Public Class ConvertToIteratorTests
        Private Shared Async Function VerifyCodeFixAsync(code As String, fixedCode As String) As Task
            Await New VerifyConvertToIterator.Test With
            {
                .TestCode = code,
                .FixedCode = fixedCode,
                .CodeActionValidationMode = CodeActionValidationMode.None
            }.RunAsync()
        End Function

        Private Shared Async Function VerifyNoCodeFixAsync(code As String) As Task
            Await New VerifyConvertToIterator.Test With
            {
                .TestCode = code,
                .FixedCode = code
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestConvertToIteratorFunction() As Task
            Await VerifyCodeFixAsync(
"Imports System
Imports System.Collections.Generic

Module Module1
    Function M() As IEnumerable(Of Integer)
        {|BC30451:Yield|} {|BC30800:1 |}
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

        <Fact>
        Public Async Function TestConvertToIteratorSub() As Task
            Await VerifyNoCodeFixAsync(
"Module Module1
    Sub M() {|BC30205:As|} 
 {|BC30451:Yield|} {|BC30800:1 |}
 End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestConvertToIteratorFunctionLambda() As Task
            Await VerifyCodeFixAsync(
"Imports System
Imports System.Collections.Generic

Module Module1
    Sub M()
        Dim a As Func(Of IEnumerable(Of Integer)) = Function()
                                                        {|BC30451:Yield|} {|BC30800:0 |}
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

        <Fact>
        Public Async Function TestConvertToIteratorSubLambda() As Task
            Await VerifyNoCodeFixAsync(
"Imports System
Imports System.Collections.Generic

Module Module1
    Sub M()
        Dim a As Func(Of IEnumerable(Of Integer)) = Sub()
                                                        {|BC30451:Yield|} {|BC30800:1 |}
 End Sub
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestConvertToIteratorSingleLineFunctionLambda() As Task
            Await VerifyNoCodeFixAsync(
"Imports System
Imports System.Collections.Generic

Module Module1
    Sub M()
        Dim a As Func(Of IEnumerable(Of Integer)) = Function() {|BC30451:Yield|} {|BC30205:1|} 
 End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestConvertToIteratorSingleLineSubLambda() As Task
            Await VerifyNoCodeFixAsync(
"Imports System
Imports System.Collections.Generic

Module Module1
    Sub M()
        Dim a As Func(Of IEnumerable(Of Integer)) = Sub() {|BC30451:Yield|} {|BC30800:1 |}
 End Sub
End Module")
        End Function
    End Class

    <Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)>
    Public Class ChangeToYieldTests
        Private Shared Async Function VerifyCodeFixAsync(code As String, fixedCode As String) As Task
            Await New VerifyConvertToYield.Test With
            {
                .TestCode = code,
                .FixedCode = fixedCode
            }.RunAsync()
        End Function

        Private Shared Async Function VerifyNoCodeFixAsync(code As String) As Task
            Await New VerifyConvertToYield.Test With
            {
                .TestCode = code,
                .FixedCode = code
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestChangeToYieldCodeFixProviderFunction() As Task
            Await VerifyCodeFixAsync(
"Module Module1
    Iterator Function M() As {|BC30002:IEnumerable(Of Integer)|}
        {|BC36942:Return 1|}
    End Function
End Module",
"Module Module1
    Iterator Function M() As {|BC30002:IEnumerable(Of Integer)|}
        Yield 1
    End Function
End Module")
        End Function

        <Fact>
        Public Async Function TestChangeToYieldCodeFixProviderSub() As Task
            Await VerifyCodeFixAsync(
"Module Module1
    Iterator {|BC36938:Sub|} M()
        {|BC36942:Return 1|}
    End Sub
End Module",
"Module Module1
    Iterator {|BC36938:Sub|} M()
        Yield 1
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestChangeToYieldCodeFixProviderFunctionLambda() As Task
            Await VerifyCodeFixAsync(
"Module Module1
    Sub M()
        Dim a = Iterator Function()
                    {|BC36942:Return 0|}
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

        <Fact>
        Public Async Function TestChangeToYieldCodeFixProviderSubLambda() As Task
            Await VerifyCodeFixAsync(
"Module Module1
    Sub M()
        Dim a = Iterator {|BC36938:Sub|}()
                    {|BC36942:Return 0|}
                End Sub
    End Sub
End Module",
"Module Module1
    Sub M()
        Dim a = Iterator {|BC36938:Sub|}()
                    Yield 0
                End Sub
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestChangeToYieldCodeFixProviderSingleLineFunctionLambda() As Task
            Await VerifyNoCodeFixAsync("Module Module1
    Sub M()
        Dim a = {|BC36947:Iterator Function() |}{|BC30201:|}Return 0 
 End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestChangeToYieldCodeFixProviderSingleLineSubLambda() As Task
            Await VerifyCodeFixAsync(
"Module Module1
    Sub M()
        Dim a = Iterator {|BC36938:Sub|}() {|BC36942:Return 0|}
    End Sub
End Module",
"Module Module1
    Sub M()
        Dim a = Iterator {|BC36938:Sub|}() Yield 0
    End Sub
End Module")
        End Function
    End Class
End Namespace
