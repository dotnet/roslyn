' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
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
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices

Public Module FromFirstAssembly
    Private Const p As String = NameOf(p)
    Public Sub Log(p As Integer, q As Integer, <CallerArgumentExpression(p)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim comp1 = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, parseOptions:=TestOptions.Regular16_9)
            comp1.VerifyDiagnostics()
            Dim ref1 = comp1.EmitToImageReference()

            Dim source2 = "
Module Program
    Public Sub Main()
        FromFirstAssembly.Log(2 + 2, 3 + 1)
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source2, references:={ref1, Net451.MicrosoftVisualBasic}, targetFramework:=TargetFramework.NetCoreApp, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="2 + 2").VerifyDiagnostics()
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

        <Fact>
        Public Sub TestArgumentExpressionIsSelfReferential_Metadata()
            Dim il = ".class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi abstract sealed beforefieldinit C
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        void M (
            [opt] string p
        ) cil managed 
    {
        .param [1] = ""<default>""
            .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerArgumentExpressionAttribute::.ctor(string) = (
                01 00 01 70 00 00
            )
        // Method begins at RVA 0x2050
        // Code size 9 (0x9)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldarg.0
        IL_0002: call void [mscorlib]System.Console::WriteLine(string)
        IL_0007: nop
        IL_0008: ret
    } // end of method C::M

} // end of class C"

            Dim source =
    <compilation>
        <file name="c.vb"><![CDATA[
Module Program
    Sub Main()
        C.M()
        C.M("value")
    End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, il, options:=TestOptions.ReleaseExe, includeVbRuntime:=True, parseOptions:=TestOptions.RegularLatest)
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

        <Fact>
        Public Sub TestGoodCallerArgumentExpressionAttribute_ExpressionHasTrivia_Attribute()
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

<My(123 _ ' comment
    + 5 ' comment
    )>
Public Module Program
    Sub Main()
        GetType(Program).GetCustomAttribute(GetType(MyAttribute))
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="123 _ ' comment
    + 5").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestGoodCallerArgumentExpressionAttribute_DifferentAssembly_AttributeConstructor()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Public Class MyAttribute : Inherits Attribute
    Private Const p As String = ""p""
    Sub New(p As Integer, q As Integer, <CallerArgumentExpression(p)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Class
"
            Dim comp1 = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp)
            comp1.VerifyDiagnostics()
            Dim ref1 = comp1.EmitToImageReference()

            Dim source2 = "
Imports System.Reflection

<My(2 + 2, 3 + 1)>
Public Module Program
    Sub Main()
        GetType(Program).GetCustomAttribute(GetType(MyAttribute))
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source2, references:={ref1, Net451.MicrosoftVisualBasic}, targetFramework:=TargetFramework.NetCoreApp, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(Compilation, expectedOutput:="2 + 2").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestIncorrectParameterNameInCallerArgumentExpressionAttribute_AttributeConstructor()
            Dim source = "
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Public Class MyAttribute : Inherits Attribute
    Private Const p As String = ""p""
    Sub New(<CallerArgumentExpression(p)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Class

<My>
Public Module Program
    Sub Main()
        GetType(Program).GetCustomAttribute(GetType(MyAttribute))
    End Sub
End Module
"
            ' PROTOTYPE(caller-expr): Warning.
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="<default-arg>").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestCallerArgumentExpressionWithOptionalTargetParameter_AttributeConstructor()
            Dim source = "
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices

<AttributeUsage(AttributeTargets.Class, AllowMultiple:=True)>
Public Class MyAttribute : Inherits Attribute
    Private Const target As String = NameOf(target)
    Sub New(p As Integer, Optional target As String = ""target default value"", <CallerArgumentExpression(target)> Optional arg As String = ""arg default value"")
        Console.WriteLine(target)
        Console.WriteLine(arg)
    End Sub
End Class

<My(0)>
<My(0, ""caller target value"")>
Public Module Program
    Sub Main()
        GetType(Program).GetCustomAttributes(GetType(MyAttribute))
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="target default value
arg default value
caller target value
""caller target value""").VerifyDiagnostics()
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
        Public Sub TestDelegate1()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices

Class Program
    Delegate Sub M(s1 As String, <CallerArgumentExpression(""s1"")> ByRef s2 as String)

    Shared Sub MImpl(s1 As String, ByRef s2 As String)
        Console.WriteLine(s1)
        Console.WriteLine(s2)
    End Sub

    Public Shared Sub Main()
        Dim x As M = AddressOf MImpl
        x.EndInvoke("""", Nothing)
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
Imports System
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic

Namespace System.Runtime.InteropServices
    <AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.[Interface] Or AttributeTargets.[Class] Or AttributeTargets.[Enum] Or AttributeTargets.Struct Or AttributeTargets.[Delegate], Inherited:=False)>
    Public NotInheritable Class GuidAttribute
        Inherits Attribute

        Public Sub New(guid As String)
            Value = guid
        End Sub

        Public ReadOnly Property Value As String
    End Class

    <AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.[Class], Inherited:=False)>
    Public NotInheritable Class ClassInterfaceAttribute
        Inherits Attribute

        Public Sub New(classInterfaceType As ClassInterfaceType)
            Value = classInterfaceType
        End Sub

        Public Sub New(classInterfaceType As Short)
            Value = CType(classInterfaceType, ClassInterfaceType)
        End Sub

        Public ReadOnly Property Value As ClassInterfaceType
    End Class

    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Field Or AttributeTargets.[Property] Or AttributeTargets.[Event], Inherited:=False)>
    Public NotInheritable Class DispIdAttribute
        Inherits Attribute

        Public Sub New(dispId As Integer)
            Value = dispId
        End Sub

        Public ReadOnly Property Value As Integer
    End Class

    Public Enum ClassInterfaceType
        None = 0
        AutoDispatch = 1
        AutoDual = 2
    End Enum
End Namespace

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

    Public Sub M(x As Integer, <CallerArgumentExpression(""x"")> Optional y As String = ""<default>"")
        Console.WriteLine(y)
    End Sub
End Class
"
            Dim comp1 = CreateCompilation(source, targetFramework:=TargetFramework.NetCoreApp, references:={Net451.MicrosoftVisualBasic}, options:=TestOptions.ReleaseDll, parseOptions:=TestOptions.RegularLatest)
            comp1.VerifyDiagnostics()

            Dim source2 = "
Imports System

Module Program
    Sub Main()
        Dim x As ComClass1._ComClass1 = New ComClass1()
        x.M(1 + 2)
    End Sub
End Module
"
            Dim comp2 = CreateCompilation(source2, references:={comp1.EmitToImageReference()}, TestOptions.ReleaseExe, TestOptions.RegularLatest)
            CompileAndVerify(comp2, expectedOutput:="1 + 2").VerifyDiagnostics()
        End Sub
#End Region
    End Class
End Namespace
