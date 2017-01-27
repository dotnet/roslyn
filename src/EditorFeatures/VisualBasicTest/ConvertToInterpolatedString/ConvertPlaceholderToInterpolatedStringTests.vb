﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertToInterpolatedString
    Public Class ConvertPlaceholderToInterpolatedStringTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As CodeRefactoringProvider
            Return New VisualBasicConvertPlaceholderToInterpolatedStringRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestSingleItemSubstitution() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim a = [|String.Format("{0}", 1)|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim a = $"{1}"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestItemOrdering() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim a = [|String.Format("{0}{1}{2}", 1, 2, 3)|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim a = $"{1}{2}{3}"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestItemOrdering2() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim a = [|String.Format("{0}{2}{1}", 1, 2, 3)|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim a = $"{1}{3}{2}"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestItemOrdering3() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim a = [|String.Format("{0}{0}{0}", 1, 2, 3)|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim a = $"{1}{1}{1}"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestItemOutsideRange() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim a = [|String.Format("{4}{5}{6}", 1, 2, 3)|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim a = $"{4}{5}{6}"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestItemDoNotHaveCast() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim a = [|String.Format("{0}{1}{2}", 0.5, "Hello", 3)|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim a = $"{0.5}{"Hello"}{3}"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestItemWithoutSyntaxErrorDoesNotHaveCast() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim a = [|String.Format("{0}{1}{2}", 0.5, "Hello", 3)|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim a = $"{0.5}{"Hello"}{3}"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestPreserveParenthesis() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim a = [|String.Format("{0}", (New Object))|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim a = $"{(New Object)}"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestMultiLineExpression() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim a = [|String.Format("{0}", If(True,
                              "Yes",
                              TryCast(False, Object)))|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim a = $"{If(True,
                              "Yes",
                              TryCast(False, Object))}"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestFormatSpecifiers() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim pricePerOunce As Decimal = 17.36
        Dim s = [|String.Format("The current price Is {0:C2} per ounce.",
                                 pricePerOunce)|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim pricePerOunce As Decimal = 17.36
        Dim s = $"The current price Is {pricePerOunce:C2} per ounce."
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestFormatSpecifiers2() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim s = [|String.Format("It Is now {0:d} at {0:T}", DateTime.Now)|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim s = $"It Is now {DateTime.Now:d} at {DateTime.Now:T}"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestFormatSpecifiers3() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim years As Integer() = {2013, 2014, 2015}
        Dim population As Integer() = {1025632, 1105967, 1148203}
        Dim s = String.Format("{0,6} {1,15}\n\n", "Year", "Population")
        For index = 0 To years.Length - 1
            s += [|String.Format("{0, 6} {1, 15: N0}\n",
                               years(index), population(index))|]
        Next
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim years As Integer() = {2013, 2014, 2015}
        Dim population As Integer() = {1025632, 1105967, 1148203}
        Dim s = String.Format("{0,6} {1,15}\n\n", "Year", "Population")
        For index = 0 To years.Length - 1
            s += $"{years(index),6} {population(index),15: N0}\n"
        Next
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestFormatSpecifiers4() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim s = [|String.Format("{0,-10:C}", 126347.89)|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim s = $"{126347.89,-10:C}"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestFormatSpecifiers5() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim cities As Tuple(Of String, DateTime, Integer, DateTime, Integer)() =
            {Tuple.Create("Los Angeles", New DateTime(1940, 1, 1), 1504277,
                         New DateTime(1950, 1, 1), 1970358),
             Tuple.Create("New York", New DateTime(1940, 1, 1), 7454995,
                         New DateTime(1950, 1, 1), 7891957),
            Tuple.Create("Chicago", New DateTime(1940, 1, 1), 3396808,
                         New DateTime(1950, 1, 1), 3620962),
            Tuple.Create("Detroit", New DateTime(1940, 1, 1), 1623452,
                         New DateTime(1950, 1, 1), 1849568)}
        Dim output As String
        For Each city In cities
            output = [|String.Format("{0,-12}{1,8:yyyy}{2,12:N0}{3,8:yyyy}{4,12:N0}{5,14:P1}",
                                   city.Item1, city.Item2, city.Item3, city.Item4, city.Item5,
                                   (city.Item5 - city.Item3) / CType(city.Item3, Double))|]
        Next
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim cities As Tuple(Of String, DateTime, Integer, DateTime, Integer)() =
            {Tuple.Create("Los Angeles", New DateTime(1940, 1, 1), 1504277,
                         New DateTime(1950, 1, 1), 1970358),
             Tuple.Create("New York", New DateTime(1940, 1, 1), 7454995,
                         New DateTime(1950, 1, 1), 7891957),
            Tuple.Create("Chicago", New DateTime(1940, 1, 1), 3396808,
                         New DateTime(1950, 1, 1), 3620962),
            Tuple.Create("Detroit", New DateTime(1940, 1, 1), 1623452,
                         New DateTime(1950, 1, 1), 1849568)}
        Dim output As String
        For Each city In cities
            output = $"{city.Item1,-12}{city.Item2,8:yyyy}{city.Item3,12:N0}{city.Item4,8:yyyy}{city.Item5,12:N0}{(city.Item5 - city.Item3) / CType(city.Item3, Double),14:P1}"
        Next
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestFormatSpecifiers6() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim values As Short() = {Int16.MaxValue, -27, 0, 1042, Int16.MaxValue}
        For Each value In values
            Dim s = [|String.Format("{0,10:G}: {0,10:X}", value)|]
        Next
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim values As Short() = {Int16.MaxValue, -27, 0, 1042, Int16.MaxValue}
        For Each value In values
            Dim s = $"{value,10:G}: {value,10:X}"
        Next
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestMultilineStringLiteral2() As Task
            Dim text = <File>
Imports System
Module T
    Sub M()
        Dim value1 = 16932
        Dim value2 = 15421
        Dim result = [|String.Format("
    {0,10} ({0,8:X8})
And {1,10} ({1,8:X8})
  = {2,10} ({2,8:X8})",
                                      value1, value2, value1 And value2)|]
    End Sub
End Module</File>.ConvertTestSourceTag()

            Dim expected = <File>
Imports System
Module T
    Sub M()
        Dim value1 = 16932
        Dim value2 = 15421
        Dim result = $"
    {value1,10} ({value1,8:X8})
And {value2,10} ({value2,8:X8})
  = {value1 And value2,10} ({value1 And value2,8:X8})"
    End Sub
End Module</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestParamsArray() As Task
            Dim text = <File>
Imports System
Module T
    Sub M(args As String())
        Dim s = [|String.Format("{0}", args)|]
    End Sub
End Module</File>.ConvertTestSourceTag()
            Await TestMissingAsync(text)
        End Function

        <WorkItem(13605, "https://github.com/dotnet/roslyn/issues/13605")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestInvocationWithNullArguments() As Task
            Dim text =
"Module Module1
    Sub Main()
        [|TaskAwaiter|]
    End Sub
End Module"
            Await TestMissingAsync(text)
        End Function
    End Class
End Namespace