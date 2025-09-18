' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.InvertIf.VisualBasicInvertSingleLineIfCodeRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InvertIf
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
    Public NotInheritable Class InvertIfDirectiveTests
        Private Shared Async Function TestAsync(testCode As String, fixedCode As String) As Task
            Await New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
                }.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")>
        Public Async Function TestIfDirective1() As Task
            Await TestAsync("
            [||]#if true
            #else
            #end if
            ", "
            #if False
            #else
            #end if
            ")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")>
        Public Async Function TestIfDirective2() As Task
            Await TestAsync("
            [||]#if true
            #else

            #end if
            ", "
            #if False

            #else
            #end if
            ")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")>
        Public Async Function TestIfDirective3() As Task
            Await TestAsync("
            [||]#if true

            #else
            #end if
            ", "
            #if False
            #else

            #end if
            ")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")>
        Public Async Function TestIfDirective4() As Task
            Await TestAsync("
            [||]#if true
            class C
            end class
            #else
            interface I

            end interface
            #end if
            ", "
            #if False
            interface I

            end interface
            #else
            class C
            end class
            #end if
            ")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")>
        Public Async Function TestIfDirective5() As Task
            Await TestAsync("
            [||]#if Not true
            class C
            end class
            #else
            interface I

            end interface
            #end if
            ", "
            #if true
            interface I

            end interface
            #else
            class C
            end class
            #end if
            ")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")>
        Public Async Function TestIfDirective6() As Task
            Await TestAsync("
            [||]#if NAME
            class C
            end class
            #else
            interface I

            end interface
            #end if
            ", "
            #if Not NAME
            interface I

            end interface
            #else
            class C
            end class
            #end if
            ")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")>
        Public Async Function TestIfDirective7() As Task
            Await TestAsync("
            [||]#if A andalso B
            class C
            end class
            #else
            interface I

            end interface
            #end if
            ", "
            #if Not (A andalso B)
            interface I

            end interface
            #else
            class C
            end class
            #end if
            ")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")>
        Public Async Function TestIfDirective8() As Task
            Await TestAsync("
            [||]#if (true)
            class C
            end class
            #else
            interface I

            end interface
            #end if
            ", "
            #if (False)
            interface I

            end interface
            #else
            class C
            end class
            #end if
            ")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")>
        Public Async Function TestIfDirective9() As Task
            Await TestAsync("
                [||]#if (true)
                class C
                end class
                #else
                interface I

                end interface
                #end if
            ", "
                #if (False)
                interface I

                end interface
                #else
                class C
                end class
                #end if
            ")
        End Function
    End Class
End Namespace
