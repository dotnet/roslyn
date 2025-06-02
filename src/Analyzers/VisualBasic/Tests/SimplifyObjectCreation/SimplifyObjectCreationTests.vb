' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(
    Of Microsoft.CodeAnalysis.VisualBasic.SimplifyObjectCreation.VisualBasicSimplifyObjectCreationDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.SimplifyObjectCreation.VisualBasicSimplifyObjectCreationCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SimplifyObjectCreation
    Public Class SimplifyObjectCreationTests
        <Fact>
        Public Async Function SimplifyObjectCreation() As Task
            Await VerifyVB.VerifyCodeFixAsync("
Public Class S
    Public Shared Function Create() As S
        Dim [|result As S = New S()|]
        return result
    End Function
End Class
", "
Public Class S
    Public Shared Function Create() As S
        Dim result As New S()
        return result
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function SimplifyObjectCreation_CodeStyleOptionTurnedOn() As Task
            Dim code = "
Public Class S
    Public Shared Function Create() As S
        Dim [|result As S = New S()|]
        return result
    End Function
End Class
"
            Dim fixedCode = "
Public Class S
    Public Shared Function Create() As S
        Dim result As New S()
        return result
    End Function
End Class
"
            Dim test = New VerifyVB.Test With
            {
                .TestCode = code,
                .FixedCode = fixedCode
            }
            test.Options.Add(VisualBasicCodeStyleOptions.PreferSimplifiedObjectCreation, True)
            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function SimplifyObjectCreation_CodeStyleOptionTurnedOff() As Task
            Dim code = "
Public Class S
    Public Shared Function Create() As S
        Dim result As S = New S()
        return result
    End Function
End Class
"
            Dim test = New VerifyVB.Test With
            {
                .TestCode = code,
                .FixedCode = code
            }
            test.Options.Add(VisualBasicCodeStyleOptions.PreferSimplifiedObjectCreation, False)
            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function SimplifyObjectCreation_CallCtorWithoutParenthesis() As Task
            Await VerifyVB.VerifyCodeFixAsync("
Public Class S
    Public Shared Function Create() As S
        Dim [|result As S = New S|]
        return result
    End Function
End Class
", "
Public Class S
    Public Shared Function Create() As S
        Dim result As New S
        return result
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function SimplifyObjectCreation_PreserveAsAndNewCasing() As Task
            Await VerifyVB.VerifyCodeFixAsync("
Public Class S
    Public Shared Function Create() As S
        Dim [|result as S = NEW S()|]
        return result
    End Function
End Class
", "
Public Class S
    Public Shared Function Create() As S
        Dim result as NEW S()
        return result
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function SimplifyObjectCreation_MultipleDeclarators() As Task
            Await VerifyVB.VerifyCodeFixAsync("
Public Class S
    Public Shared Function Create() As S
        Dim [|result as S = NEW S()|], [|result2 As S = New S|]
        return result
    End Function
End Class
", "
Public Class S
    Public Shared Function Create() As S
        Dim result as NEW S(), result2 As New S
        return result
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function SimplifyObjectCreation_WithInitializer() As Task
            Await VerifyVB.VerifyCodeFixAsync("
Public Class S
    Public X As Integer

    Public Shared Function Create() As S
        Dim [|result As S = New S() With { .X = 0 }|]
        return result
    End Function
End Class
", "
Public Class S
    Public X As Integer

    Public Shared Function Create() As S
        Dim result As New S() With { .X = 0 }
        return result
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function SimplifyObjectCreation_FromCollectionInitializer() As Task
            Await VerifyVB.VerifyCodeFixAsync("
Imports System.Collections.Generic

Public Class S
    Public Shared Function Create() As List(Of Integer)
        Dim [|result As List(Of Integer) = New List(Of Integer)() From { 0, 1, 2, 3 }|]
        return result
    End Function
End Class
", "
Imports System.Collections.Generic

Public Class S
    Public Shared Function Create() As List(Of Integer)
        Dim result As New List(Of Integer)() From { 0, 1, 2, 3 }
        return result
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function TypeIsConverted_NoDiagnostic() As Task
            Await VerifyVB.VerifyAnalyzerAsync("
Public Interface IInterface
End Interface

Public Class S : Implements IInterface
    Public Shared Function Create() As S
        Dim result as IInterface = NEW S()
        return result
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function ArrayCreation_NoDiagnostic() As Task
            Await VerifyVB.VerifyAnalyzerAsync("
Public Class C
    Public Sub M()
        Dim x As String() = New String() { }
    End Sub
End Class
")
        End Function
    End Class
End Namespace
