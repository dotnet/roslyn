' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryCall.VisualBasicRemoveUnnecessaryCallDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryCall.VisualBasicRemoveUnnecessaryCallCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnnecessaryCall
    <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveCall)>
    Public Class RemoveUnnecessaryCallTests
        Private Shared Function VerifyCodeFixAsync(source As String, fixedSource As String) As Task
            Return New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource
            }.RunAsync()
        End Function

        <Fact>
        Public Function TestRemoveCall() As Task
            Return VerifyCodeFixAsync(
"Imports System
Public Class Program
    Public Sub MySub()
        [|Call|] Console.WriteLine()
    End Sub
End Class
",
"Imports System
Public Class Program
    Public Sub MySub()
        Console.WriteLine()
    End Sub
End Class
")
        End Function

        <Fact>
        Public Function TestRemoveCallLowerCase() As Task
            Return VerifyCodeFixAsync(
"Imports System
Public Class Program
    Public Sub MySub()
        [|call|] Console.WriteLine()
    End Sub
End Class
",
"Imports System
Public Class Program
    Public Sub MySub()
        Console.WriteLine()
    End Sub
End Class
")
        End Function
        <Fact>
        Public Function TestRemoveCallTrailingComment() As Task
            Return VerifyCodeFixAsync(
"Imports System
Public Class Program
    Public Sub MySub()
        [|Call|] Console.WriteLine() ' comment
    End Sub
End Class
",
"Imports System
Public Class Program
    Public Sub MySub()
        Console.WriteLine() ' comment
    End Sub
End Class
")
        End Function

        <Fact>
        Public Function TestRemoveCallFullQualifiedName() As Task
            Return VerifyCodeFixAsync(
"Public Class Program
    Public Sub MySub()
        [|Call|] System.Console.WriteLine()
        [|Call|] Global.System.Console.WriteLine()
    End Sub
End Class
",
"Public Class Program
    Public Sub MySub()
        System.Console.WriteLine()
        Global.System.Console.WriteLine()
    End Sub
End Class
")
        End Function

        <Fact>
        Public Function TestRemoveCallClassKeywords() As Task
            Return VerifyCodeFixAsync(
"Imports System
Public Class Program
    Public Sub MySub()
        [|Call|] MyClass.A()
        [|Call|] MyClass.A
        [|Call|] C(Nothing)
    End Sub
    Public Shared Sub A()
    End Sub
    Public Overridable Sub B()
    End Sub
    Private Sub C(b As Action)
        If b Is Nothing Then
            [|Call|] Me.B()
            [|Call|] Me.B
        End If
    End Sub
End Class
Public Class Program2
    Inherits Program
    Public Overrides Sub B()
        [|Call|] MyBase.B()
        [|Call|] MyBase.B
    End Sub
End Class
",
"Imports System
Public Class Program
    Public Sub MySub()
        MyClass.A()
        MyClass.A
        C(Nothing)
    End Sub
    Public Shared Sub A()
    End Sub
    Public Overridable Sub B()
    End Sub
    Private Sub C(b As Action)
        If b Is Nothing Then
            Me.B()
            Me.B
        End If
    End Sub
End Class
Public Class Program2
    Inherits Program
    Public Overrides Sub B()
        MyBase.B()
        MyBase.B
    End Sub
End Class
")
        End Function

        <Fact>
        Public Function TestRemoveCallTypeShared() As Task
            Return VerifyCodeFixAsync(
"Public Class Program
    Public Sub MySub()
        [|Call|] Decimal.Round(1.5D).A()
        [|Call|] Decimal.Round(0.5D).A
    End Sub
End Class
Module DecimalExt
    <System.Runtime.CompilerServices.Extension>
    Public Sub A(x As Decimal)
    End Sub
End Module
",
"Public Class Program
    Public Sub MySub()
        Decimal.Round(1.5D).A()
        Decimal.Round(0.5D).A
    End Sub
End Class
Module DecimalExt
    <System.Runtime.CompilerServices.Extension>
    Public Sub A(x As Decimal)
    End Sub
End Module
")
        End Function

        <Fact>
        Public Function TestRemoveCallTypeOperations() As Task
            Return VerifyCodeFixAsync(
"Public Class Program
    Public Sub MySub()
        Dim o As Object = """"
        [|Call|] CStr(o).A
        [|Call|] CType(o, String).A
        [|Call|] DirectCast(o, String).A()
        [|Call|] TryCast(o, String)?.A()
        [|Call|] GetType(String).ToString().A()
    End Sub
End Class
Module StringExt
    <System.Runtime.CompilerServices.Extension>
    Public Sub A(x As String)
    End Sub
End Module
",
"Public Class Program
    Public Sub MySub()
        Dim o As Object = """"
        CStr(o).A
        CType(o, String).A
        DirectCast(o, String).A()
        TryCast(o, String)?.A()
        GetType(String).ToString().A()
    End Sub
End Class
Module StringExt
    <System.Runtime.CompilerServices.Extension>
    Public Sub A(x As String)
    End Sub
End Module
")
        End Function

        <Fact>
        Public Function TestRemoveCallGetXmlNamespace() As Task
            Return VerifyCodeFixAsync(
"Imports System.Xml.Linq
Imports <xmlns:ns=""http://example.com"">
Public Class Program
    Public Sub MySub()
        [|Call|] GetXmlNamespace(ns).A
        [|Call|] GetXmlNamespace(ns).A()
    End Sub
End Class
Module StringExt
    <System.Runtime.CompilerServices.Extension>
    Public Sub A(x As XNamespace)
    End Sub
End Module
",
"Imports System.Xml.Linq
Imports <xmlns:ns=""http://example.com"">
Public Class Program
    Public Sub MySub()
        GetXmlNamespace(ns).A
        GetXmlNamespace(ns).A()
    End Sub
End Class
Module StringExt
    <System.Runtime.CompilerServices.Extension>
    Public Sub A(x As XNamespace)
    End Sub
End Module
")
        End Function

        <Fact>
        Public Function TestRemoveCallMethodChain() As Task
            Return VerifyCodeFixAsync(
"Imports System.Runtime.CompilerServices
Public Class Program
    Public Sub MySub()
        Dim o As Object = """"
        [|Call|] CStr(o).C.C().C.C().C()?.C.A
        [|Call|] GetType(Integer).ToString().ToString.C.C?.C.C().C()?.C.A
    End Sub
End Class
Module StringExt
    <Extension>
    Public Sub A(x As String)
    End Sub
    <Extension>
    Public Function C(x As String) As String
        Return x
    End Function
End Module
",
"Imports System.Runtime.CompilerServices
Public Class Program
    Public Sub MySub()
        Dim o As Object = """"
        CStr(o).C.C().C.C().C()?.C.A
        GetType(Integer).ToString().ToString.C.C?.C.C().C()?.C.A
    End Sub
End Class
Module StringExt
    <Extension>
    Public Sub A(x As String)
    End Sub
    <Extension>
    Public Function C(x As String) As String
        Return x
    End Function
End Module
")
        End Function

        <Theory>
        <InlineData(
"' New Operator
Imports System
Public Class Program
    Public Sub MySub()
        Call New A().B
        Call New A().B()
    End SUb
End Class
Class A
    Public Sub B()
    End Sub
End Class
")>
        <InlineData(
"' Parens
Imports System
Public Class Program
    Public Sub MySub()
        Call (1 + 2).A()
        Call (1 + 2).A
    End SUb
End Class
Module IntExt
    <System.Runtime.CompilerServices.Extension>
    Public Sub A(n As Integer)
    End Sub
End Module
")>
        <InlineData(
"' If Operator
Imports System
Imports System.Runtime.CompilerServices
Public Class Program
    Public Sub MySub()
        Call If(String.Empty.Length = 0, 1, 0).A()
        Call If(String.Empty, ""null"").A
    End SUb
End Class
Module IntExt
    <Extension>
    Public Sub A(n As Integer)
    End Sub
    <Extension>
    Public Sub A(s As String)
    End Sub
End Module
")>
        <InlineData(
"' Literal
Imports System.Runtime.CompilerServices
Public Class Program
    Public Sub MySub()
        Call 1.A
        Call 2.A()
        Call 1.5.A
        Call 2.5.A()
        Call """".A
        Call """".A()
        Call True.A
        Call False.A()
        Call #2024-01-01#.A
        Call #01/01/2024#.A()
        Call Nothing?.ToString()
    End Sub
End Class
Module StringExt
    <Extension>
    Public Sub A(x As String)
    End Sub
    <Extension>
    Public Sub A(x As Integer)
    End Sub
    <Extension>
    Public Sub A(x As Double)
    End Sub
    <Extension>
    Public Sub A(x As Boolean)
    End Sub
    <Extension>
    Public Sub A(x As Date)
    End Sub
End Module
"
        )>
        <InlineData(
"'XML
Imports System.Xml.Linq
Public Class Program
    Public Sub MySub()
        Call <xml:Root/>.A
        Call <xml:Root/>.A()
    End Sub
End Class
Module XmlExt
    <System.Runtime.CompilerServices.Extension>
    Public Sub A(x As XElement)
    End Sub
End Module
")>
        Public Function TestRemoveCallKeep(code As String) As Task
            Return VerifyCodeFixAsync(code, code)
        End Function
    End Class
End Namespace
