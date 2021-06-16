' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.TestMetadata

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class AttributeTests_CallerArgumentExpression
        Inherits BasicTestBase

        <Fact>
        Public Sub TestGoodCallerArgumentExpressionAttribute()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(123)
    End Sub

    Private Const p As String = NameOf(p)
    Sub Log(p As Integer, <CallerArgumentExpression(p)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            compilation.VerifyDiagnostics()
            CompileAndVerify(compilation, expectedOutput:="123")

        End Sub

        <Fact>
        Public Sub TestGoodCallerArgumentExpressionAttribute_ExpressionHasTrivia()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(' comment _
               123  + _
               5 ' comment
        )
    End Sub

    Private Const p As String = NameOf(p)
    Sub Log(p As Integer, <CallerArgumentExpression(p)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            compilation.VerifyDiagnostics()
            CompileAndVerify(compilation, expectedOutput:="") ' TODO
        End Sub

        <Fact>
        Public Sub TestGoodCallerArgumentExpressionAttribute_SwapArguments()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(q:=123, p:=124)
    End Sub

    Private Const p As String = NameOf(p)
    Sub Log(p As Integer, q As Integer, <CallerArgumentExpression(p)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine($""{p}, {q}, {arg}"")
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            compilation.VerifyDiagnostics()
            CompileAndVerify(compilation, expectedOutput:="124, 123, 124")
        End Sub

        <Fact>
        Public Sub TestGoodCallerArgumentExpressionAttribute_DifferentAssembly()

        End Sub

        <Fact>
        Public Sub TestGoodCallerArgumentExpressionAttribute_ExtensionMethod_ThisParameter()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Dim myIntegerExpression As Integer = 5
        myIntegerExpression.M(""s"")
    End Sub

    Private Const p As String = NameOf(p)

    <Extension>
    Public Sub M(p As Integer, s As String, <CallerArgumentExpression(p)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            compilation.VerifyDiagnostics()
            CompileAndVerify(compilation, expectedOutput:="myIntegerExpression")
        End Sub
    End Class
End Namespace
