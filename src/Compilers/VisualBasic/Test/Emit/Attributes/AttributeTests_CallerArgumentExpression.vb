' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.TestMetadata

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class AttributeTests_CallerArgumentExpression
        Inherits BasicTestBase

#Region "CallerArgumentExpression - Invocations"
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
            CompileAndVerify(compilation, expectedOutput:="123").VerifyDiagnostics()

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
            CompileAndVerify(compilation, expectedOutput:="123  + _
               5").VerifyDiagnostics()
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
            CompileAndVerify(compilation, expectedOutput:="124, 123, 124").VerifyDiagnostics()
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
        myIntegerExpression.M()
    End Sub

    Private Const p As String = NameOf(p)

    <Extension>
    Public Sub M(p As Integer, <CallerArgumentExpression(p)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="myIntegerExpression").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestGoodCallerArgumentExpressionAttribute_ExtensionMethod_NotThisParameter()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Dim myIntegerExpression As Integer = 5
        myIntegerExpression.M(myIntegerExpression * 2)
    End Sub

    Private Const q As String = NameOf(q)

    <Extension>
    Public Sub M(p As Integer, q As Integer, <CallerArgumentExpression(q)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="myIntegerExpression * 2").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestIncorrectParameterNameInCallerArgumentExpressionAttribute()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log()
    End Sub

    Private Const pp As String = NameOf(pp)

    Sub Log(<CallerArgumentExpression(pp)> Optional arg As String = ""<default>"")
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular16_9)
            ' PROTOTYPE(caller-expr): This should have diagnostics.
            CompileAndVerify(compilation, expectedOutput:="<default>").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestCallerArgumentWithMemberNameAttributes()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(0+ 0)
    End Sub

    Private Const p As String = NameOf(p)

    Sub Log(p As Integer, <CallerArgumentExpression(p)> <CallerMemberName> Optional arg As String = ""<default>"")
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular16_9)
            CompileAndVerify(compilation, expectedOutput:="Main").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestCallerArgumentExpressionWithOptionalTargetParameter()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Dim callerTargetExp = ""caller target value""
        Log(0)
        Log(0, callerTargetExp)
    End Sub

    Private Const target As String = NameOf(target)

    Sub Log(p As Integer, Optional target As String = ""target default value"", <CallerArgumentExpression(target)> Optional arg As String = ""arg default value"")
        Console.WriteLine(target)
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="target default value
arg default value
caller target value
callerTargetExp").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestCallerArgumentExpressionWithMultipleOptionalAttribute()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Dim callerTargetExp = ""caller target value""
        Log(0)
        Log(0, callerTargetExp)
        Log(0, target:=callerTargetExp)
        Log(0, notTarget:=""Not target value"")
        Log(0, notTarget:=""Not target value"", target:=callerTargetExp)
    End Sub

    Private Const target As String = NameOf(target)

    Sub Log(p As Integer, Optional target As String = ""target default value"", Optional notTarget As String = ""not target default value"", <CallerArgumentExpression(target)> Optional arg As String = ""arg default value"")
        Console.WriteLine(target)
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="target default value
arg default value
caller target value
callerTargetExp
caller target value
callerTargetExp
target default value
arg default value
caller target value
callerTargetExp").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestCallerArgumentExpressionWithDifferentParametersReferringToEachOther()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        M()
        M(""param1_value"")
        M(param1:=""param1_value"")
        M(param2:=""param2_value"")
        M(param1:=""param1_value"", param2:=""param2_value"")
        M(param2:=""param2_value"", param1:=""param1_value"")
    End Sub

    Sub M(<CallerArgumentExpression(""param2"")> Optional param1 As String = ""param1_default"", <CallerArgumentExpression(""param1"")> Optional param2 As String = ""param2_default"")
        Console.WriteLine($""param1: {param1}, param2: {param2}"")
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="param1: param1_default, param2: param2_default
param1: param1_value, param2: ""param1_value""
param1: param1_value, param2: ""param1_value""
param1: ""param2_value"", param2: param2_value
param1: param1_value, param2: param2_value
param1: param1_value, param2: param2_value").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestArgumentExpressionIsCallerMember()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        M()
    End Sub

    Sub M(<CallerMemberName> Optional callerName As String = ""<default-caller-name>"", <CallerArgumentExpression(""callerName"")> Optional argumentExp As String = ""<default-arg-expression>"")
        Console.WriteLine(callerName)
        Console.WriteLine(argumentExp)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="Main
<default-arg-expression>").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestArgumentExpressionIsSelfReferential()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        M()
        M(""value"")
    End Sub

    Sub M(<CallerArgumentExpression(""p"")> Optional p As String = ""<default>"")
        Console.WriteLine(p)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            ' PROTOTYPE(caller-expr): Warning for self-referential
            CompileAndVerify(compilation, expectedOutput:="<default>
value").VerifyDiagnostics()
        End Sub
#End Region

#Region "CallerArgumentExpression - Attributes"
        <Fact>
        Public Sub TestGoodCallerArgumentExpressionAttribute_Attribute()
            Dim source = "
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Public Class MyAttribute : Inherits Attribute
    Private Const p As String = ""p""
    Sub New(p As Integer, <CallerArgumentExpression(p)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Class

<My(123)>
Public Module Program
    Sub Main()
        GetType(Program).GetCustomAttribute(GetType(MyAttribute))
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="123").VerifyDiagnostics()
        End Sub

        ' PROTOTYPE(caller-expr): TODO - More tests.
#End Region

#Region "CallerArgumentExpression - Test various symbols"
        <Fact>
        Public Sub TestIndexers()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices

Class Program
    Const i As String = NameOf(i)

    Default Public Property Item(i As Integer, <CallerArgumentExpression(i)> Optional s As String = ""<default-arg>"") As Integer
        Get
            Return i
        End Get
        Set(value As Integer)
            Console.WriteLine($""{i}, {s}"")
        End Set
    End Property

    Public Shared Sub Main()
        Dim p As New Program()
        p(1+  1) = 5
        p(2+  2, ""explicit-value"") = 5
    End Sub
End Class
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="2, 1+  1
4, explicit-value").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub ComClass()
            Dim source As String = "
Imports Microsoft.VisualBasic

<ComClass(ComClass1.ClassId, ComClass1.InterfaceId, ComClass1.EventsId)>
Public Class ComClass1
    ' Use the Region directive to define a section named COM Guids.
#Region ""COM GUIDs""
    ' These  GUIDs provide the COM identity for this class
    ' and its COM interfaces. You can generate
    ' these guids using guidgen.exe
    Public Const ClassId As String = ""7666AC25-855F-4534-BC55-27BF09D49D46""
    Public Const InterfaceId As String = ""54388137-8A76-491e-AA3A-853E23AC1217""
    Public Const EventsId As String = ""EA329A13-16A0-478d-B41F-47583A761FF2""
#End Region

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub M(x As Integer, Optional y As String = """")
    End Sub
End Class

Module Program
    Sub Main()
        Dim comObj As New ComClass1()
        comObj.M(1+  0)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="").VerifyDiagnostics() ' PROTOTYPE(caller-expr): Figure out how to fix these:
            ' (5) : error BC35000: Requested operation is not available because the runtime library function 'System.Runtime.InteropServices.GuidAttribute..ctor' is not defined.
            ' (5) : error BC35000: Requested operation is not available because the runtime library function 'System.Runtime.InteropServices.ClassInterfaceAttribute..ctor' is not defined.
            ' (5) : error BC35000: Requested operation is not available because the runtime library function 'System.Runtime.InteropServices.DispIdAttribute..ctor' is not defined.
        End Sub
#End Region
    End Class
End Namespace
