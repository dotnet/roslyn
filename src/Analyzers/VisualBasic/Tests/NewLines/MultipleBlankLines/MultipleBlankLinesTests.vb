' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.NewLines.MultipleBlankLines.VisualBasicMultipleBlankLinesDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.NewLines.MultipleBlankLines.MultipleBlankLinesCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.NewLines.MultipleBlankLines
    Public Class MultipleBlankLinesTests
        Private Shared Async Function TestWithOptionOn(testCode As String, fixedCode As String) As Task
            Dim test = New VerifyVB.Test() With
            {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }
            test.Options.Add(CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSilentEnforcement)

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestOneBlankLineAtTopOfFile() As Task
            Dim code =
"
' comment"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestTwoBlankLineAtTopOfFile() As Task
            Dim code =
"[||]

' comment"
            Dim fixedCode =
"
' comment"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestTwoBlankLineAtTopOfFile_NotWithOptionOff() As Task
            Dim code =
"

' comment"

            Await New VerifyVB.Test() With
            {
                .TestCode = code,
                .FixedCode = code
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestThreeBlankLineAtTopOfFile() As Task
            Dim code =
"[||]


' comment"
            Dim fixedCode =
"
' comment"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestFourBlankLineAtTopOfFile() As Task
            Dim code =
"[||]



' comment"
            Dim fixedCode =
"
' comment"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestOneBlankLineAtTopOfEmptyFile() As Task
            Dim code =
"
"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestTwoBlankLinesAtTopOfEmptyFile() As Task
            Dim code =
"[||]

"
            Dim fixedCode =
"
"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestThreeBlankLinesAtTopOfEmptyFile() As Task
            Dim code =
"[||]


"
            Dim fixedCode =
"
"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestFourBlankLinesAtTopOfEmptyFile() As Task
            Dim code =
"[||]



"
            Dim fixedCode =
"
"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestNoBlankLineAtEndOfFile_1() As Task
            Dim code =
"Class C
End Class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestNoBlankLineAtEndOfFile_2() As Task
            Dim code =
"Class C
End Class
"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestOneBlankLineAtEndOfFile() As Task
            Dim code =
"Class C
End Class

"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestTwoBlankLineAtEndOfFile() As Task
            Dim code =
"Class C
End Class
[||]

"
            Dim fixedCode =
"Class C
End Class

"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestThreeBlankLineAtEndOfFile() As Task
            Dim code =
"Class C
End Class
[||]


"
            Dim fixedCode =
"Class C
End Class

"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestFourBlankLineAtEndOfFile() As Task
            Dim code =
"Class C
End Class
[||]



"
            Dim fixedCode =
"Class C
End Class

"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestNoBlankLineBetweenTokens() As Task
            Dim code =
"Class C
End Class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestOneBlankLineBetweenTokens() As Task
            Dim code =
"Class C

End Class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestTwoBlankLineBetweenTokens() As Task
            Dim code =
"Class C
[||]

End Class"
            Dim fixedCode =
"Class C

End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestThreeBlankLineBetweenTokens() As Task
            Dim code =
"Class C
[||]


End Class"
            Dim fixedCode =
"Class C

End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestFourBlankLineBetweenTokens() As Task
            Dim code =
"Class C
[||]



End Class"
            Dim fixedCode =
"Class C

End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestNoBlankLineAfterComment() As Task
            Dim code =
"Class C
    ' comment
End Class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestOneBlankLineAfterComment() As Task
            Dim code =
"Class C
    ' comment

End Class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestTwoBlankLineAfterComment() As Task
            Dim code =
"Class C
    ' comment
[||]

End Class"
            Dim fixedCode =
"Class C
    ' comment

End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestThreeBlankLineAfterComment() As Task
            Dim code =
"Class C
    ' comment
[||]


End Class"
            Dim fixedCode =
"Class C
    ' comment

End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestFourBlankLineAfterComment() As Task
            Dim code =
"Class C
    ' comment
[||]


End Class"
            Dim fixedCode =
"Class C
    ' comment

End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestNoBlankLineAfterDirective() As Task
            Dim code =
"Class C
    #Const X = 0
End Class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestOneBlankLineAfterDirective() As Task
            Dim code =
"Class C
    #Const X = 0

End Class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestTwoBlankLineAfterDirective() As Task
            Dim code =
"Class C
    #Const X = 0
[||]

End Class"
            Dim fixedCode =
"Class C
    #Const X = 0

End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestThreeBlankLineAfterDirective() As Task
            Dim code =
"Class C
    #Const X = 0
[||]


End Class"
            Dim fixedCode =
"Class C
    #Const X = 0

End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestFourBlankLineAfterDirective() As Task
            Dim code =
"Class C
    #Const X = 0
[||]


End Class"
            Dim fixedCode =
"Class C
    #Const X = 0

End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestNoBlankLineAfterDocComment() As Task
            Dim code =
"
''' <summary/>
Class C
End Class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestOneBlankLineAfterDocComment() As Task
            Dim code =
"
''' <summary/>

Class C
End Class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestTwoBlankLineAfterDocComment() As Task
            Dim code =
"
''' <summary/>
[||]

Class C
End Class"
            Dim fixedCode =
"
''' <summary/>

Class C
End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestThreeBlankLineAfterDocComment() As Task
            Dim code =
"
''' <summary/>
[||]


Class C
End Class"
            Dim fixedCode =
"
''' <summary/>

Class C
End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestFourBlankLineAfterDocComment() As Task
            Dim code =
"
''' <summary/>
[||]



Class C
End Class"
            Dim fixedCode =
"
''' <summary/>

Class C
End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestNoBlankLineAllConstructs() As Task
            Dim code =
"''' <summary/>
'
#Const X = 0
Class C
End Class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestOneBlankLineAllConstructs() As Task
            Dim code =
"
''' <summary/>

'

#Const X = 0

Class C
End Class"

            Await TestWithOptionOn(code, code)
        End Function

        <Fact>
        Public Async Function TestTwoBlankLineAllConstructs() As Task
            Dim code =
"[||]

''' <summary/>


'


#Const X = 0


Class C
End Class"
            Dim fixedCode =
"
''' <summary/>

'

#Const X = 0

Class C
End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestThreeBlankLineAllConstructs() As Task
            Dim code =
"[||]


''' <summary/>



'



#Const X = 0



Class C
End Class"
            Dim fixedCode =
"
''' <summary/>

'

#Const X = 0

Class C
End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestFourBlankLineAllConstructs() As Task
            Dim code =
"[||]



''' <summary/>




'




#Const X = 0




Class C
End Class"
            Dim fixedCode =
"
''' <summary/>

'

#Const X = 0

Class C
End Class"

            Await TestWithOptionOn(code, fixedCode)
        End Function
    End Class
End Namespace
