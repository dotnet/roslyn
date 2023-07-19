' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.NewLines.ConsecutiveStatementPlacement.VisualBasicConsecutiveStatementPlacementDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.NewLines.ConsecutiveStatementPlacement.ConsecutiveStatementPlacementCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.NewLines.ConsecutiveStatementPlacement
    Public Class ConsecutiveStatementPlacementTests
        Private Shared Async Function TestWithOptionOn(testCode As String, fixedCode As String) As Task
            Dim test = New VerifyVB.Test() With {
                .TestCode = testCode,
                .FixedCode = fixedCode
                }

            test.Options.Add(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement)
            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestNotAfterPropertyBlock() As Task
            Dim code =
"
class C
    readonly property X as integer
        get
            return 0
        end get
    end property
    readonly property Y as integer
        get
            return 0
        end get
    end property
end class"
            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestNotAfterMethodBlock() As Task
            Dim code =
"
class C
    sub X()
    end sub
    sub Y()
    end sub
end class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestNotAfterStatementsOnSingleLine() As Task
            Dim code =
"
class C
    sub M()
        if (true) : end if : return
    end sub
end class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestNotAfterStatementsOnMultipleLinesWithCommentBetween1() As Task
            Dim code =
"
class C
    sub M()
        if (true)
        end if
        ' x
        return
    end sub
end class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestNotAfterStatementsWithSingleBlankLines() As Task
            Dim code =
"
class C
    sub M()
        if (true)
        end if

        return
    end sub
end class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestNotAfterStatementsWithSingleBlankLinesWithSpaces() As Task
            Dim code =
"
class C
    sub M()
        if (true)
        end if
        
        return
    end sub
end class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestNotAfterStatementsWithMultipleBlankLines() As Task
            Dim code =
"
class C
    sub M()
        if (true)
        end if

        return
    end sub
end class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestNotAfterStatementsOnMultipleLinesWithPPDirectiveBetween1() As Task
            Dim code =
"
class C
    sub M()
        if (true)
        end if
#Region """"
        return
    end sub
#End Region
end class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestNotBetweenBlockAndOuterBlocker() As Task
            Dim code =
"
class C
    sub M()
        if (true)
            if (false)
            end if
        end if
    end sub
end class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestBetweenBlockAndStatement1() As Task
            Await TestWithOptionOn("
class C
    sub M()
        if (true)
        [|end if|]
        return
    end sub
end class", "
class C
    sub M()
        if (true)
        end if

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestNotBetweenBlockAndStatement1_WhenOptionOff() As Task
            Dim code = "
class C
    sub M()
        if (true)
        end if
        return
    end sub
end class"

            Await New VerifyVB.Test() With {
                .TestCode = code,
                .FixedCode = code
                }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestBetweenForEachAndStatement1() As Task
            For Each x In {0}
            Next

            Await TestWithOptionOn("
class C
    sub M()
        For Each x In {0}
        [|Next|]
        return
    end sub
end class", "
class C
    sub M()
        For Each x In {0}
        Next

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestBetweenBlockAndStatement2() As Task
            Await TestWithOptionOn("
class C
    sub M()
        if (true)
        [|end if|] ' trailing comment
        return
    end sub
end class", "
class C
    sub M()
        if (true)
        end if ' trailing comment

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestBetweenBlockAndStatement3() As Task
            Await TestWithOptionOn("
class C
    sub M()
        if (true) : [|end if|]
        return
    end sub
end class", "
class C
    sub M()
        if (true) : end if

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestFixAll1() As Task
            Await TestWithOptionOn("
class C
    sub M()
        if (true)
        [|end if|]
        return
        if (true)
        [|end if|]
        return
    end sub
end class", "
class C
    sub M()
        if (true)
        end if

        return
        if (true)
        end if

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestAfterEndSelect() As Task
            Await TestWithOptionOn("
class C
    sub M()
        select (0)
        [|end select|]
        return
    end sub
end class", "
class C
    sub M()
        select (0)
        end select

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestAfterEndTry() As Task
            Await TestWithOptionOn("
class C
    sub M()
        Try
        Finally
        [|End Try|]
        return
    end sub
end class", "
class C
    sub M()
        Try
        Finally
        End Try

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestAfterUsing() As Task
            Await TestWithOptionOn("
class C
    sub M(d as System.IDisposable)
        using (d)
        [|end using|]
        return
    end sub
end class", "
class C
    sub M(d as System.IDisposable)
        using (d)
        [|end using|]

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestAfterDo1() As Task
            Await TestWithOptionOn("
class C
    sub M()
        do
        [|loop|]
        return
    end sub
end class", "
class C
    sub M()
        do
        loop

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestAfterDo2() As Task
            Await TestWithOptionOn("
class C
    sub M()
        do
        [|loop while true|]
        return
    end sub
end class", "
class C
    sub M()
        do
        loop while true

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestAfterDo3() As Task
            Await TestWithOptionOn("
class C
    sub M()
        do
        [|loop until true|]
        return
    end sub
end class", "
class C
    sub M()
        do
        loop until true

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestIfFollowedByIf() As Task
            Await TestWithOptionOn("
class C
    sub M()
        if (true)
        [|end if|]
        if (true)
        end if
    end sub
end class", "
class C
    sub M()
        if (true)
        end if

        if (true)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestClassWithEndOfLine() As Task
            Dim code = "
class C
    sub M()
    end sub
end class
"

            Await TestWithOptionOn(code, code)
        End Function
    End Class
End Namespace
