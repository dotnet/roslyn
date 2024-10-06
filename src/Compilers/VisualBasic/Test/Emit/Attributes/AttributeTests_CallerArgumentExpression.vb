' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class AttributeTests_CallerArgumentExpression
        Inherits BasicTestBase

#Region "CallerArgumentExpression - Invocations"
        <ConditionalFact(GetType(CoreClrOnly))>
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

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="123").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestGoodCallerArgumentExpressionAttribute_OldVersion()
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

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular16)
            CompileAndVerify(compilation, expectedOutput:="123").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestGoodCallerArgumentExpressionAttribute_MultipleAttributes()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices

Namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Parameter, AllowMultiple:=True, Inherited:=False)>
    Public NotInheritable Class CallerArgumentExpressionAttribute
        Inherits Attribute

        Public Sub New(parameterName As String)
            ParameterName = parameterName
        End Sub

        Public ReadOnly Property ParameterName As String
    End Class
End Namespace

Class Program
    Public Shared Sub Main()
        Log(123, 456)
    End Sub

    Const p1 As String = NameOf(p1)
    Const p2 As String = NameOf(p2)

    Private Shared Sub Log(p1 As Integer, p2 As Integer, <CallerArgumentExpression(p1), CallerArgumentExpression(p2)> ByVal Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="456").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestGoodCallerArgumentExpressionAttribute_MultipleAttributes_IncorrectCtor()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices

Namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Parameter, AllowMultiple:=True, Inherited:=False)>
    Public NotInheritable Class CallerArgumentExpressionAttribute
        Inherits Attribute

        Public Sub New(parameterName As String, extraParam As Integer)
            ParameterName = parameterName
        End Sub

        Public ReadOnly Property ParameterName As String
    End Class
End Namespace

Class Program
    Public Shared Sub Main()
        Log(123, 456)
    End Sub

    Const p1 As String = NameOf(p1)
    Const p2 As String = NameOf(p2)

    Private Shared Sub Log(p1 As Integer, p2 As Integer, <CallerArgumentExpression(p1, 0), CallerArgumentExpression(p2, 1)> ByVal Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="<default-arg>").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestGoodCallerArgumentExpressionAttribute_CaseInsensitivity()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices
Public Module Program2
    Sub Main()
        Log(123)
    End Sub

    Private Const P As String = NameOf(P)
    Public Sub Log(p As Integer, <CallerArgumentExpression(P)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="123").VerifyDiagnostics().VerifyTypeIL("Program2", "
.class public auto ansi sealed Program2
	extends [System.Runtime]System.Object
{
	.custom instance void [Microsoft.VisualBasic.Core]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = (
		01 00 00 00
	)
	// Fields
	.field private static literal string P = ""P""
	// Methods
	.method public static 
		void Main () cil managed 
	{
		.custom instance void [System.Runtime]System.STAThreadAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2050
		// Code size 13 (0xd)
		.maxstack 8
		.entrypoint
		IL_0000: ldc.i4.s 123
		IL_0002: ldstr ""123""
		IL_0007: call void Program2::Log(int32, string)
		IL_000c: ret
	} // end of method Program2::Main
	.method public static 
		void Log (
			int32 p,
			[opt] string arg
		) cil managed 
	{
		.param [2] = ""<default-arg>""
			.custom instance void [System.Runtime]System.Runtime.CompilerServices.CallerArgumentExpressionAttribute::.ctor(string) = (
				01 00 01 70 00 00
			)
		// Method begins at RVA 0x205e
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.1
		IL_0001: call void [System.Console]System.Console::WriteLine(string)
		IL_0006: ret
	} // end of method Program2::Log
} // end of class Program2
")
            Dim csCompilation = CreateCSharpCompilation("Program2.Log(5 + 2);", referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Net50, {compilation.EmitToImageReference()}), compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.ConsoleApplication))
            CompileAndVerify(csCompilation, expectedOutput:="5 + 2").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestGoodCallerArgumentExpressionAttribute_CaseInsensitivity_Metadata()
            Dim il = "
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi C
    extends [mscorlib]System.Object
{
    // Methods
    .method public specialname rtspecialname
        instance void .ctor () cil managed
    {
        // Method begins at RVA 0x2050
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    } // end of method C::.ctor

    .method public static
        void M (
            int32 i,
            [opt] string s
        ) cil managed
    {
        .param [2] = ""default""
            .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerArgumentExpressionAttribute::.ctor(string) = (
                01 00 01 49 00 00 // I
            )
        // Method begins at RVA 0x2058
        // Code size 9 (0x9)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldarg.1
        IL_0002: call void [mscorlib]System.Console::WriteLine(string)
        IL_0007: nop
        IL_0008: ret
    } // end of method C::M

} // end of class C
"

            Dim source =
    <compilation>
        <file name="c.vb"><![CDATA[
Module Program
    Sub Main()
        C.M(0 + 1)
    End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, il, options:=TestOptions.ReleaseExe, includeVbRuntime:=True, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="0 + 1").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="123  + _
               5").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="124, 123, 124").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim comp1 = CreateCompilation(source, targetFramework:=TargetFramework.Net50, parseOptions:=TestOptions.Regular16_9)
            comp1.VerifyDiagnostics()
            Dim ref1 = comp1.EmitToImageReference()

            Dim source2 = "
Module Program
    Public Sub Main()
        FromFirstAssembly.Log(2 + 2, 3 + 1)
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source2, references:={ref1}, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="2 + 2").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="myIntegerExpression").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="myIntegerExpression * 2").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentExpressionAttribute_ExtensionMethod_IncorrectParameter()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Dim myIntegerExpression As Integer = 5
        myIntegerExpression.M(myIntegerExpression * 2)
    End Sub

    Private Const qq As String = NameOf(qq)

    <Extension>
    Public Sub M(p As Integer, q As Integer, <CallerArgumentExpression(qq)> Optional arg As String = ""<default-arg>"")
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="<default-arg>")
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC42505: The CallerArgumentExpressionAttribute applied to parameter 'arg' will have no effect. It is applied with an invalid parameter name.
    Public Sub M(p As Integer, q As Integer, <CallerArgumentExpression(qq)> Optional arg As String = "<default-arg>")
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular16_9)
            CompileAndVerify(compilation, expectedOutput:="<default>")
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC42505: The CallerArgumentExpressionAttribute applied to parameter 'arg' will have no effect. It is applied with an invalid parameter name.
    Sub Log(<CallerArgumentExpression(pp)> Optional arg As String = "<default>")
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular16_9)
            CompileAndVerify(compilation, expectedOutput:="Main").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentWithMemberNameAttributes2()
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="Main").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentWithFilePathAttributes()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(0+ 0)
    End Sub

    Private Const p As String = NameOf(p)

    Sub Log(p As Integer, <CallerArgumentExpression(p)> <CallerFilePath> Optional arg As String = ""<default>"")
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(Parse(source, "C:\\Program.cs", options:=TestOptions.Regular16_9), targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:="C:\\Program.cs").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentWithFilePathAttributes2()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(0+ 0)
    End Sub

    Private Const p As String = NameOf(p)

    Sub Log(p As Integer, <CallerArgumentExpression(p)> <CallerFilePath> Optional arg As String = ""<default>"")
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(Parse(source, "C:\\Program.cs", options:=TestOptions.RegularLatest), targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:="C:\\Program.cs").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentWithLineNumberAttributes()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(0+ 0)
    End Sub

    Private Const p As String = NameOf(p)

    Sub Log(p As Integer, <CallerArgumentExpression(p)> <CallerLineNumber> Optional arg As String = ""<default>"")
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular16_9)
            CompileAndVerify(compilation, expectedOutput:="6").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentWithLineNumberAttributes2()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(0+ 0)
    End Sub

    Private Const p As String = NameOf(p)

    Sub Log(p As Integer, <CallerArgumentExpression(p)> <CallerLineNumber> Optional arg As String = ""<default>"")
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="6").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentWithLineNumberAttributes3()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(0+ 0)
    End Sub

    Private Const p As String = NameOf(p)

    Sub Log(p As Integer, <CallerArgumentExpression(p)> <CallerLineNumber> Optional arg As Integer = 0)
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular16_9)
            CompileAndVerify(compilation, expectedOutput:="6").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentWithLineNumberAttributes4()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(0+ 0)
    End Sub

    Private Const p As String = NameOf(p)

    Sub Log(p As Integer, <CallerArgumentExpression(p)> <CallerLineNumber> Optional arg As Integer = 0)
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="6").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentNonOptionalParameter()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(0+ 0)
    End Sub

    Private Const p As String = NameOf(p)

    Sub Log(p As Integer, <CallerArgumentExpression(p)> arg As String)
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30455: Argument not specified for parameter 'arg' of 'Public Sub Log(p As Integer, arg As String)'.
        Log(0+ 0)
        ~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentNonOptionalParameter2()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(0+ 0)
    End Sub

    Private Const p As String = NameOf(p)

    Sub Log(p As Integer, <CallerArgumentExpression(p)> arg As String)
        Console.WriteLine(arg)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular16_9)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30455: Argument not specified for parameter 'arg' of 'Public Sub Log(p As Integer, arg As String)'.
        Log(0+ 0)
        ~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentWithOverride()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices

MustInherit Class Base
    Const p As String = NameOf(p)
    Public MustOverride Sub Log_RemoveAttributeInOverride(p As Integer, <CallerArgumentExpression(p)> Optional arg As String = ""default"")
    Public MustOverride Sub Log_AddAttributeInOverride(p As Integer, Optional arg As String = ""default"")
End Class

Class Derived : Inherits Base

    Const p As String = NameOf(p)
    Public Overrides Sub Log_AddAttributeInOverride(p As Integer, <CallerArgumentExpression(p)> Optional arg As String = ""default"")
        Console.WriteLine(arg)
    End Sub

    Public Overrides Sub Log_RemoveAttributeInOverride(p As Integer, Optional arg As String = ""default"")
        Console.WriteLine(arg)
    End Sub
End Class

Class Program
    Public Shared Sub Main()
        Dim derived = New Derived()
        derived.Log_AddAttributeInOverride(5 + 4)
        derived.Log_RemoveAttributeInOverride(5 + 5)

        DirectCast(derived, Base).Log_AddAttributeInOverride(5 + 4)
        DirectCast(derived, Base).Log_RemoveAttributeInOverride(5 + 5)
    End Sub
End Class
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="5 + 4
default
default
5 + 5").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentWithUserDefinedConversionFromString()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices

Class C
    Public Sub New(s As String)
        Prop = s
    End Sub

    Public ReadOnly Property Prop As String

    Public Shared Widening Operator CType(s As String) As C
        Return New C(s)
    End Operator

End Class

Class Program
    Public Shared Sub Main()
        Log(0)
    End Sub
    Const p As String = NameOf(p)
    Shared Sub Log(p As Integer, <CallerArgumentExpression(p)> Optional arg As C = Nothing)
        Console.WriteLine(arg Is Nothing)
    End Sub
End Class
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="True").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="target default value
arg default value
caller target value
callerTargetExp").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
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

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="param1: param1_default, param2: param2_default
param1: param1_value, param2: ""param1_value""
param1: param1_value, param2: ""param1_value""
param1: ""param2_value"", param2: param2_value
param1: param1_value, param2: param2_value
param1: param1_value, param2: param2_value").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="Main
<default-arg-expression>").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="<default>
value")
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC42504: The CallerArgumentExpressionAttribute applied to parameter 'p' will have no effect because it's self-referential.
    Sub M(<CallerArgumentExpression("p")> Optional p As String = "<default>")
           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            CompileAndVerify(compilation, expectedOutput:="<default>
value").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentExpression_OnByRefParameter01()
            Dim source As String = "
Imports System.Runtime.CompilerServices
Module Program
    Private Const p As String = NameOf(p)
    Sub Log(p As Integer, <CallerArgumentExpression(p)> ByRef arg As String)
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseDll, parseOptions:=TestOptions.RegularLatest)
            compilation.AssertTheseDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestCallerArgumentExpression_OnByRefParameter02()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Log(1 + 1)
    End Sub

    Private Const p As String = NameOf(p)
    Sub Log(p As Integer, <CallerArgumentExpression(p)> Optional ByRef arg As String = ""<default-value>"")
        Console.WriteLine(arg)
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="1 + 1").VerifyDiagnostics()
        End Sub
#End Region

#Region "CallerArgumentExpression - Attributes"
        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="123").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="123 _ ' comment
    + 5").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim comp1 = CreateCompilation(source, targetFramework:=TargetFramework.Net50)
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
            Dim compilation = CreateCompilation(source2, references:={ref1}, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="2 + 2").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="<default-arg>")
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC42505: The CallerArgumentExpressionAttribute applied to parameter 'arg' will have no effect. It is applied with an invalid parameter name.
    Sub New(<CallerArgumentExpression(p)> Optional arg As String = "<default-arg>")
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="target default value
arg default value
caller target value
""caller target value""").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestArgumentExpressionIsReferringToItself_AttributeConstructor()
            Dim source = "
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices

<AttributeUsage(AttributeTargets.Class, AllowMultiple:=True)>
Public Class MyAttribute : Inherits Attribute
    Private Const p As String = NameOf(p)
    Sub New(<CallerArgumentExpression(p)> Optional p As String = ""default"")
        Console.WriteLine(p)
    End Sub
End Class

<My>
<My(""value"")>
Public Module Program
    Sub Main()
        GetType(Program).GetCustomAttributes(GetType(MyAttribute))
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="default
value")
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC42504: The CallerArgumentExpressionAttribute applied to parameter 'p' will have no effect because it's self-referential.
    Sub New(<CallerArgumentExpression(p)> Optional p As String = "default")
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestArgumentExpressionInAttributeConstructor_OptionalAndFieldInitializer()
            Dim source = "
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices

Public Class MyAttribute : Inherits Attribute
    Private Const a As String = NameOf(a)
    Sub New(<CallerArgumentExpression(a)> Optional expr_a As String = ""<default0>"", Optional a As String = ""<default1>"")
        Console.WriteLine($""'{a}', '{expr_a}'"")
    End Sub

    Public I1 As Integer
    Public I2 As Integer
    Public I3 As Integer
End Class

<My(I1:=0, I2:=1, I3:=2)>
Public Module Program
    Sub Main()
        GetType(Program).GetCustomAttribute(GetType(MyAttribute))
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="'<default1>', '<default0>'").VerifyDiagnostics()
        End Sub
#End Region

#Region "CallerArgumentExpression - Test various symbols"
        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="2, 1+  1
4, explicit-value").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation).VerifyDiagnostics().VerifyIL("Program.Main", "
{
  // Code size       27 (0x1b)
  .maxstack  3
  .locals init (String V_0)
  IL_0000:  ldnull
  IL_0001:  ldftn      ""Sub Program.MImpl(String, ByRef String)""
  IL_0007:  newobj     ""Sub Program.M..ctor(Object, System.IntPtr)""
  IL_000c:  ldstr      """"
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  ldnull
  IL_0015:  callvirt   ""Sub Program.M.EndInvoke(ByRef String, System.IAsyncResult)""
  IL_001a:  ret
}
")
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestDelegate2()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices

Class Program
    Delegate Sub M(s1 As String, <CallerArgumentExpression(""s1"")> Optional ByRef s2 as String = """")

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
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC33010: 'Delegate' parameters cannot be declared 'Optional'.
    Delegate Sub M(s1 As String, <CallerArgumentExpression("s1")> Optional ByRef s2 as String = "")
                                                                  ~~~~~~~~
]]></expected>)

        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
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
            Dim comp1 = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseDll, parseOptions:=TestOptions.RegularLatest)
            comp1.VerifyDiagnostics()

            Dim source2 = "
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

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub Tuple()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices


Namespace System.Runtime.CompilerServices
    Public Interface ITuple
        ReadOnly Property Length As Integer
        Default ReadOnly Property Item(index As Integer) As Object
    End Interface
End Namespace

Namespace System
    Public Structure ValueTuple(Of T1, T2) : Implements ITuple
        Public Item1 As T1
        Public Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
            item1 = item1
            item2 = item2
        End Sub

        Default Public ReadOnly Property Item(index As Integer) As Object Implements ITuple.Item
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public ReadOnly Property Length As Integer Implements ITuple.Length
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Sub M(s1 As String, <CallerArgumentExpression(""s1"")> Optional s2 As String = ""<default>"")
            Console.WriteLine(s2)
        End Sub
    End Structure
End Namespace


Module Program
    Sub Main()
        Dim x = New ValueTuple(Of Integer, Integer)(0, 0)
        x.M(1 + 2)
    End Sub
End Module
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="1 + 2").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestOperator()
            Dim il = ".class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi C
    extends [mscorlib]System.Object
{
    // Methods
    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    } // end of method C::.ctor

    .method public specialname static 
        class C op_Addition (
            class C left,
            [opt] int32 right
        ) cil managed 
    {
        .param [2] = int32(0)
            .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerArgumentExpressionAttribute::.ctor(string) = (
                01 00 04 6c 65 66 74 00 00
            )
        // Method begins at RVA 0x2058
        // Code size 7 (0x7)
        .maxstack 1
        .locals init (
            [0] class C
        )

        IL_0000: nop
        IL_0001: ldnull
        IL_0002: stloc.0
        IL_0003: br.s IL_0005

        IL_0005: ldloc.0
        IL_0006: ret
    } // end of method C::op_Addition

} // end of class C
"

            Dim source =
    <compilation>
        <file name="c.vb"><![CDATA[
Module Program
    Sub Main()
        Dim obj As New C()
        obj = obj + 0
    End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, il, options:=TestOptions.ReleaseExe, includeVbRuntime:=True, parseOptions:=TestOptions.RegularLatest)
            compilation.VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestSetter1()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices

Class Program
    Public Property P As String
        Get
            Return ""Return getter""
        End Get
        Set(<CallerArgumentExpression("""")> value As String)
            Console.WriteLine(value)
        End Set
    End Property

    Public Shared Sub Main()
        Dim prog As New Program()
        prog.P = ""New value""
    End Sub
End Class
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="New value")
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC42505: The CallerArgumentExpressionAttribute applied to parameter 'value' will have no effect. It is applied with an invalid parameter name.
        Set(<CallerArgumentExpression("")> value As String)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestSetter2()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices

Class Program
    Public Property P As String
        Get
            Return ""Return getter""
        End Get
        Set(<CallerArgumentExpression(""value"")> value As String)
            Console.WriteLine(value)
        End Set
    End Property

    Public Shared Sub Main()
        Dim prog As New Program()
        prog.P = ""New value""
    End Sub
End Class
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="New value")
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC42504: The CallerArgumentExpressionAttribute applied to parameter 'value' will have no effect because it's self-referential.
        Set(<CallerArgumentExpression("value")> value As String)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestSetter3()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices

Class Program
    Public Property P As String
        Get
            Return ""Return getter""
        End Get
        Set(<CallerArgumentExpression("""")> Optional value As String = ""default"")
            Console.WriteLine(value)
        End Set
    End Property

    Public Shared Sub Main()
        Dim prog As New Program()
        prog.P = ""New value""
    End Sub
End Class
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC42505: The CallerArgumentExpressionAttribute applied to parameter 'value' will have no effect. It is applied with an invalid parameter name.
        Set(<CallerArgumentExpression("")> Optional value As String = "default")
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31065: 'Set' parameter cannot be declared 'Optional'.
        Set(<CallerArgumentExpression("")> Optional value As String = "default")
                                           ~~~~~~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestSetter4()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices

Class Program
    Public Property P As String
        Get
            Return ""Return getter""
        End Get
        Set(<CallerArgumentExpression(""value"")> Optional value As String = ""default"")
            Console.WriteLine(value)
        End Set
    End Property

    Public Shared Sub Main()
        Dim prog As New Program()
        prog.P = ""New value""
    End Sub
End Class
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC42504: The CallerArgumentExpressionAttribute applied to parameter 'value' will have no effect because it's self-referential.
        Set(<CallerArgumentExpression("value")> Optional value As String = "default")
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31065: 'Set' parameter cannot be declared 'Optional'.
        Set(<CallerArgumentExpression("value")> Optional value As String = "default")
                                                ~~~~~~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestSetter5()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices

Class Program
    Public Property P(x As String) As String
        Get
            Return ""Return getter""
        End Get
        Set(<CallerArgumentExpression(""x"")> Optional value As String = ""default"")
            Console.WriteLine(value)
        End Set
    End Property

    Public Shared Sub Main()
        Dim prog As New Program()
        prog.P(""xvalue"") = ""New value""
    End Sub
End Class
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC31065: 'Set' parameter cannot be declared 'Optional'.
        Set(<CallerArgumentExpression("x")> Optional value As String = "default")
                                            ~~~~~~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        Public Sub TestSetter6()
            Dim source As String = "
Imports System
Imports System.Runtime.CompilerServices

Class Program
    Public Property P(x As String) As String
        Get
            Return ""Return getter""
        End Get
        Set(<CallerArgumentExpression(""x"")> value As String)
            Console.WriteLine(value)
        End Set
    End Property

    Public Shared Sub Main()
        Dim prog As New Program()
        prog.P(""xvalue"") = ""New value""
    End Sub
End Class
"
            Dim compilation = CreateCompilation(source, targetFramework:=TargetFramework.Net50, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.RegularLatest)
            CompileAndVerify(compilation, expectedOutput:="New value").VerifyDiagnostics()
        End Sub
#End Region
    End Class
End Namespace
