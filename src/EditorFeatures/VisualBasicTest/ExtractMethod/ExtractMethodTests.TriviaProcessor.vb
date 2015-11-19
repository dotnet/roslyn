' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ExtractMethod
    Partial Public Class ExtractMethodTests
        Public Class TriviaProcessor

            <WorkItem(539281)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestCommentBeforeCode()
                Dim code = <text>Class C
    Sub M()
        [|'comment
        Console.Write(10)|]
    End Sub
End Class</text>

                Dim expected = <text>Class C
    Sub M()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        'comment
        Console.Write(10)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(545173)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub LineContinuation()
                Dim code = <text>Module Program
    Sub Main
        Dim x = [|1. _
            ToString|]
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main
        Dim x = GetX()
    End Sub

    Private Function GetX() As String
        Return 1. _
                    ToString
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(544568)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub LineContinuation2()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim x1 = Function(num _
                          As _
                     Integer
                          )
                     Return [|num _
                     +
                     1|]
                 End Function
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim x1 = Function(num _
                          As _
                     Integer
                          )
                     Return NewMethod(num)
                 End Function
    End Sub

    Private Function NewMethod(num As Integer) As Integer
        Return num _
                             +
                             1
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(529797)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ImplicitLineContinuation()
                Dim code = <text>Imports System.Linq
Module A
    Sub Main()
        Dim q = [|From x In "" Distinct|] ' Extract Method
        .ToString()
    End Sub
End Module</text>

                Dim expected = <text>Imports System.Linq
Module A
    Sub Main()
        Dim q = NewMethod() ' Extract Method
        .ToString()
    End Sub

    Private Function NewMethod() As System.Collections.Generic.IEnumerable(Of Char)
        Return From x In "" Distinct
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(529797)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ImplicitLineContinuation2()
                Dim code = <text>Imports System.Linq
Module A
    Sub Main()
        Dim q = [|From x In "" Distinct|]
        .ToString()
    End Sub
End Module</text>

                Dim expected = <text>Imports System.Linq
Module A
    Sub Main()
        Dim q = NewMethod()
        .ToString()
    End Sub

    Private Function NewMethod() As System.Collections.Generic.IEnumerable(Of Char)
        Return From x In "" Distinct
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub
        End Class
    End Class
End Namespace
