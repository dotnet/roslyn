' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
    End Class
End Namespace
