' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class OverloadResolutionPriorityTests
        Inherits BasicTestBase

        Private Const OverloadResolutionPriorityAttributeDefinitionCS As String = "
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
    {
        public int Priority => priority;
    }
}
"

        Private Const OverloadResolutionPriorityAttributeDefinitionVB As String = "
namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Constructor Or AttributeTargets.Property, AllowMultiple:= false, Inherited:= false)>
    public class OverloadResolutionPriorityAttribute
        Inherits Attribute

        Public Sub New(priority As Integer)
            Me.Priority = priority
        End Sub

        public Readonly Property Priority As Integer
    End Class
End Namespace
"

        Private Const OverloadResolutionPriorityAttributeILDefinition As String = "
.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 e0 00 00 00 02 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
        72 69 74 65 64 00
    )
    .field private int32 '<priority>P'
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 priority
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::'<priority>P'
        ldarg.0
        call instance void [mscorlib]System.Attribute::.ctor()
        ret
    }
    .method public hidebysig specialname 
        instance int32 get_Priority () cil managed 
    {
        ldarg.0
        ldfld int32 System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::'<priority>P'
        ret
    }
    .property instance int32 Priority()
    {
        .get instance int32 System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::get_Priority()
    }
}
"

        <Theory, CombinatorialData>
        Public Sub IncreasedPriorityWins_01_CS(i1First As Boolean)

            Dim i1Source = "
[OverloadResolutionPriority(1)]
public static void M(I1 x) => System.Console.WriteLine(1);
"

            Dim i2Source = "
public static void M(I2 x) => throw null;
"

            Dim reference = CreateCSharpCompilation("
using System.Runtime.CompilerServices;

public interface I1 {}
public interface I2 {}
public interface I3 : I1, I2 {}

public class C
{" +
    If(i1First, i1Source, i2Source) +
    If(i1First, i2Source, i1Source) + "
}
" + OverloadResolutionPriorityAttributeDefinitionCS, parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        C.M(i3)
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe)

            Dim c = compilation.GetTypeByMetadataName("C")
            Dim ms = c.GetMembers("M").Cast(Of MethodSymbol)()
            For Each m In ms
                Assert.Equal(If(m.Parameters(0).Type.Name = "I1", 1, 0), m.OverloadResolutionPriority)
            Next

            CompileAndVerify(compilation, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Theory, CombinatorialData>
        Public Sub IncreasedPriorityWins_01(i1First As Boolean)

            Dim i1Source = "
<OverloadResolutionPriority(1)>
public Shared Sub M(x As I1)
    System.Console.WriteLine(1)
End Sub
"

            Dim i2Source = "
public Shared Sub M(x As I2)
    throw DirectCast(Nothing, System.Exception)
End Sub
"

            Dim reference = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public class C" +
    If(i1First, i1Source, i2Source) +
    If(i1First, i2Source, i1Source) + "
End Class
"

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        C.M(i3)
    End Sub
End Class
"

            Dim comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            Dim validate = Sub([module] As ModuleSymbol)
                               Dim c = [module].ContainingAssembly.GetTypeByMetadataName("C")
                               Dim ms = c.GetMembers("M").Cast(Of MethodSymbol)()
                               For Each m In ms
                                   Assert.Equal(If(m.Parameters(0).Type.Name = "I1", 1, 0), m.OverloadResolutionPriority)
                               Next
                           End Sub

            CompileAndVerify(comp1, expectedOutput:="1", sourceSymbolValidator:=validate, symbolValidator:=validate).VerifyDiagnostics()

            Dim compilationReference As CompilationReference = comp1.ToMetadataReference()
            Dim comp2 = CreateCompilation(source, references:={compilationReference}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="1").VerifyDiagnostics()

            Dim metadataReference As MetadataReference = comp1.EmitToImageReference()
            Dim comp3 = CreateCompilation(source, references:={metadataReference}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="1").VerifyDiagnostics()

            comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular17_13)
            CompileAndVerify(comp1, expectedOutput:="1", sourceSymbolValidator:=validate, symbolValidator:=validate).VerifyDiagnostics()

            comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            comp1.AssertTheseDiagnostics(If(i1First,
<expected><![CDATA[
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared Sub M(x As I1)': Not most specific.
    'Public Shared Sub M(x As I2)': Not most specific.
        C.M(i3)
          ~
BC36716: Visual Basic 16.9 does not support overload resolution priority.
<OverloadResolutionPriority(1)>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>,
<expected><![CDATA[
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared Sub M(x As I2)': Not most specific.
    'Public Shared Sub M(x As I1)': Not most specific.
        C.M(i3)
          ~
BC36716: Visual Basic 16.9 does not support overload resolution priority.
<OverloadResolutionPriority(1)>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>))

            comp2 = CreateCompilation(source, references:={compilationReference}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            Dim expected = If(i1First,
<expected>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared Sub M(x As I1)': Not most specific.
    'Public Shared Sub M(x As I2)': Not most specific.
        C.M(i3)
          ~
</expected>,
<expected>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared Sub M(x As I2)': Not most specific.
    'Public Shared Sub M(x As I1)': Not most specific.
        C.M(i3)
          ~
</expected>)

            comp2.AssertTheseDiagnostics(expected)

            comp3 = CreateCompilation(source, references:={metadataReference}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            comp3.AssertTheseDiagnostics(expected)
        End Sub

        <Theory, CombinatorialData>
        Public Sub Accessibility_01(i1First As Boolean)

            Dim i1Source = "
<OverloadResolutionPriority(1)>
protected Shared Sub M(x As I1)
    System.Console.WriteLine(1)
End Sub
"

            Dim i2Source = "
public Shared Sub M(x As I2)
    System.Console.WriteLine(2)
End Sub
"

            Dim reference = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public class C" +
    If(i1First, i1Source, i2Source) +
    If(i1First, i2Source, i1Source) + "
End Class
"

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        C.M(i3)
    End Sub
End Class
"

            Dim comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            CompileAndVerify(comp1, expectedOutput:="2").VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="2").VerifyDiagnostics()

            Dim comp3 = CreateCompilation(source, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="2").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub EarlyFilteringByParamArray()

            Dim source = "
Imports System

Module Program
    Sub Main()
        Dim t As new Test1
        t.M1(1)
        t.M2(1)
    End Sub
End Module
    
Class Test1
    Sub M1(s As Integer)
        Console.Write(1)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(ParamArray s As Integer())
        Console.Write(2)
    End Sub

    Sub M2(s As Integer)
        Console.Write(3)
    End Sub

    Sub M2(ParamArray s As Integer())
        Console.Write(4)
    End Sub
End Class
"
            Dim compilation = CompilationUtils.CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            CompileAndVerify(compilation, expectedOutput:="23")
        End Sub

        <Fact>
        Public Sub EarlyFilteringOnExtensionMethodTargetTypeGenericity()

            Dim source = "
Module Program
    Sub Main()
        Dim t As new Integer?(1)
        t.M1()
        t.M2()
    End Sub
End Module
"
            Dim reference = "
Imports System
Imports System.Runtime.CompilerServices

Public Module Test1
    <Extension>
    Sub M1(s As Integer?)
        Console.Write(1)
    End Sub

    <OverloadResolutionPriority(1)>
    <Extension>
    Sub M1(Of T As Structure)(s As T?)
        Console.Write(2)
    End Sub

    <Extension>
    Sub M2(s As Integer?)
        Console.Write(3)
    End Sub

    <Extension>
    Sub M2(Of T As Structure)(s As T?)
        Console.Write(4)
    End Sub
End Module
"
            Dim compilation = CompilationUtils.CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:="23")

            compilation = CompilationUtils.CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular17_13)
            CompileAndVerify(compilation, expectedOutput:="23")

            compilation = CompilationUtils.CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16.9 does not support overload resolution priority.
    <OverloadResolutionPriority(1)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

            Dim ref = CompilationUtils.CreateCompilation({reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)

            compilation = CompilationUtils.CreateCompilation(source, references:={ref.ToMetadataReference()}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            CompileAndVerify(compilation, expectedOutput:="13")

            compilation = CompilationUtils.CreateCompilation(source, references:={ref.EmitToImageReference()}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            CompileAndVerify(compilation, expectedOutput:="13")
        End Sub

        <Fact>
        Public Sub EarlyFilteringOnReceiverType_01()

            Dim source = "
Imports System
Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
        Dim t = 1.ToString()
        t.M1()
        t.M2()
    End Sub
End Module
    
Module Test1
    <Extension>
    Sub M1(s As String)
        Console.Write(1)
    End Sub

    <OverloadResolutionPriority(1)>
    <Extension>
    Sub M1(s As Object)
        Console.Write(2)
    End Sub

    <Extension>
    Sub M2(s As String)
        Console.Write(3)
    End Sub

    <Extension>
    Sub M2(s As Object)
        Console.Write(4)
    End Sub
End Module
"
            Dim compilation = CompilationUtils.CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            CompileAndVerify(compilation, expectedOutput:="23")
        End Sub

        <Fact>
        Public Sub EarlyFilteringOnReceiverType_02()

            Dim source = "
Imports System

Module Program
    Sub Main()
        Dim t As new Test2
        t.M1(1.ToString())
        t.M2(1.ToString())
    End Sub
End Module
    
Class Test1
    overridable Sub M1(s As String)
        Console.Write(1)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(s As Object)
        Console.Write(2)
    End Sub

    overridable Sub M2(s As String)
        Console.Write(4)
    End Sub

    Sub M2(s As Object)
        Console.Write(5)
    End Sub
End Class

Class Test2
    Inherits Test1

    overrides Sub M1(s As String)
        Console.Write(3)
    End Sub

    overrides Sub M2(s As String)
        Console.Write(6)
    End Sub
End Class
"
            Dim compilation = CompilationUtils.CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            CompileAndVerify(compilation, expectedOutput:="26")
        End Sub

        <Fact>
        Public Sub EarlyFiltering_OnReceiverType_03()

            Dim source = "
Imports System

Module Program
    Sub Main()
        Dim t As new Test2
        t.M1(1.ToString())
        t.M2(1.ToString())
    End Sub
End Module
    
Class Test1
    overridable Sub M1(s As String)
        Console.Write(1)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(s As String, Optional x as Integer = 0)
        Console.Write(2)
    End Sub

    overridable Sub M2(s As String)
        Console.Write(4)
    End Sub

    Sub M2(s As String, Optional x as Integer = 0)
        Console.Write(5)
    End Sub
End Class

Class Test2
    Inherits Test1

    overrides Sub M1(s As String)
        Console.Write(3)
    End Sub

    overrides Sub M2(s As String)
        Console.Write(6)
    End Sub
End Class
"
            Dim compilation = CompilationUtils.CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            CompileAndVerify(compilation, expectedOutput:="26")
        End Sub

        <Fact>
        Public Sub TestResolutionBasedOnInferenceKind2()
            Dim compilationDef = "
Option Strict Off

Module Module1

    Sub Main()
        Dim val As Integer = 0

        M1(1, Function(x As Integer) As Integer
                  Return 2
              End Function, 1, val)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, Integer), z As U, ParamArray v() As Long)
        System.Console.Write(1)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(Of T)(x As Integer, y As System.Func(Of Integer, T), z As Integer, v As Integer)
        System.Console.Write(2)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, T), z As U, v As Long)
        System.Console.Write(3)
    End Sub

End Module
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="2")
        End Sub

        <Fact>
        Public Sub TestResolutionBasedOnInferenceKind4()
            Dim compilationDef = "
Option Strict Off

Module Module1

    Sub Main()
        Dim val As Integer = 0

        M1(1, Function(x As Integer) As Integer
                  Return 2
              End Function, 1, v:=val)
        M1(1, Function(x As Integer) As Integer
                  Return 2
              End Function, 1, val)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, Integer), z As U, v As Long, ParamArray vv() As Long)
        System.Console.Write(1)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(Of T)(x As Integer, y As System.Func(Of Integer, T), z As Integer, v As Integer)
        System.Console.Write(2)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, T), z As U, v As Long)
        System.Console.Write(3)
    End Sub
End Module
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="22")
        End Sub

        <Fact>
        Public Sub NarrowingConversions_01()
            Dim compilationDef = "
Option Strict Off

Module Module1

    Sub Main()
        M1(New C1())
        M1(DirectCast(New C2(), C0))
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(x As I1)
        System.Console.Write(1)
    End Sub

    Sub M1(x As I2)
        System.Console.Write(2)
    End Sub
End Module

Interface I1
End Interface

Interface I2
End Interface

Class C0
End Class

Class C1
    Inherits C0
    Implements I1, I2
End Class

Class C2
    Inherits C0
    Implements I2
End Class
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)

            ' If the priority filtering for 'M1(DirectCast(New C2(), C0))' was applied - System.InvalidCastException: Unable to cast object of type 'C2' to type 'I1'.
            compilation.AssertTheseDiagnostics(
<expected>
BC30519: Overload resolution failed because no accessible 'M1' can be called without a narrowing conversion:
    'Public Sub M1(x As I1)': Argument matching parameter 'x' narrows from 'C0' to 'I1'.
    'Public Sub M1(x As I2)': Argument matching parameter 'x' narrows from 'C0' to 'I2'.
        M1(DirectCast(New C2(), C0))
        ~~
</expected>)
        End Sub

        <Fact>
        Public Sub NarrowingConversions_02()
            Dim compilationDef = "
Option Strict Off

Module Module1

    Sub Main()
        M1(CObj(New C2()))
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(x As I1)
        System.Console.Write(1)
    End Sub

    Sub M1(x As I2)
        System.Console.Write(2)
    End Sub
End Module

Interface I1
End Interface

Interface I2
End Interface

Class C0
End Class

Class C1
    Inherits C0
    Implements I1, I2
End Class

Class C2
    Inherits C0
    Implements I2
End Class
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)

            ' If the priority filtering was applied - System.InvalidCastException: Unable to cast object of type 'C2' to type 'I1'.
            CompileAndVerify(compilation, expectedOutput:="2")
        End Sub

        <Fact>
        Public Sub NarrowingConversions_03()
            Dim compilationDef = "
Module Module1

    Sub Main()
        Try
            M1(New C0())
        Catch ex As System.InvalidCastException
            System.Console.Write(ex)    
        End Try
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(x As C1)
    End Sub

    Sub M1(x As C2)
        System.Console.Write(2)
    End Sub
End Module

Class C0
    public shared narrowing operator CType(x As C0) As C1
        throw new System.InvalidCastException()
    End Operator
    public shared widening operator CType(x As C0) As C2
        return New C2()
    End Operator
End Class

Class C1
End Class

Class C2
End Class
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            CompileAndVerify(compilation, expectedOutput:="2")

            compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off))
            ' If the priority filtering was applied - System.InvalidCastException : Specified cast is not valid.
            CompileAndVerify(compilation, expectedOutput:="2")
        End Sub

        <Fact>
        Public Sub DelegateRelaxationLevelNarrowing_01()
            Dim compilationDef = "
Option Strict Off

Module Module1

    Sub Main()
        M1(Function() New C1())
        M1(Function() DirectCast(New C2(), C0))
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(x As System.Func(Of I1))
        x()
        System.Console.Write(1)
    End Sub

    Sub M1(x As System.Func(Of I2))
        x()
        System.Console.Write(2)
    End Sub
End Module

Interface I1
End Interface

Interface I2
End Interface

Class C0
End Class

Class C1
    Inherits C0
    Implements I1, I2
End Class

Class C2
    Inherits C0
    Implements I2
End Class
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)

            ' If the priority filtering for 'M1(Function() DirectCast(New C2(), C0))' was applied - System.InvalidCastException: Unable to cast object of type 'C2' to type 'I1'.
            compilation.AssertTheseDiagnostics(
<expected>
BC30521: Overload resolution failed because no accessible 'M1' is most specific for these arguments:
    'Public Sub M1(x As Func(Of I1))': Not most specific.
    'Public Sub M1(x As Func(Of I2))': Not most specific.
        M1(Function() DirectCast(New C2(), C0))
        ~~
</expected>)
        End Sub

        <Fact>
        Public Sub NarrowingFromNumericConstant_01()
            Dim compilationDef = "
Option Strict Off

Module Module1

    Sub Main()
        M1(CDbl(0))
    End Sub

    Sub M1(x as Decimal)
        System.Console.Write(1)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(x As Single)
        System.Console.Write(2)
    End Sub
End Module
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)

            compilation.AssertTheseDiagnostics(
<expected>
BC30519: Overload resolution failed because no accessible 'M1' can be called without a narrowing conversion:
    'Public Sub M1(x As Decimal)': Argument matching parameter 'x' narrows from 'Double' to 'Decimal'.
    'Public Sub M1(x As Single)': Argument matching parameter 'x' narrows from 'Double' to 'Single'.
        M1(CDbl(0))
        ~~
</expected>)
        End Sub

        <Fact>
        Public Sub NarrowingFromNumericConstant_02()
            Dim compilationDef = "
Module Module1

    Sub Main()
        M1(CLng(0))
        M2(CLng(0))
    End Sub

    Sub M1(x as Integer)
        System.Console.Write(1)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(x As UInteger)
        System.Console.Write(2)
    End Sub

    Sub M2(x as Integer)
        System.Console.Write(3)
    End Sub

    Sub M2(x As UInteger)
        System.Console.Write(4)
    End Sub
End Module
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="13")
        End Sub

        <Fact>
        Public Sub NarrowingFromNumericConstant_03()
            Dim compilationDef = "
Module Module1

    Sub Main()
        M1(CLng(0))
    End Sub

    Sub M1(x as Integer)
        System.Console.Write(1)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(x As UInteger)
        System.Console.Write(2)
    End Sub

    Sub M1(x As Long)
        System.Console.Write(3)
    End Sub
End Module
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="3")
        End Sub

        <Fact>
        Public Sub NarrowingFromNumericConstant_04()
            Dim compilationDef = "
Module Module1

    Sub Main()
        M1(Function() CLng(0))
        M2(Function() CLng(0))
    End Sub

    Sub M1(x as System.Func(Of Integer))
        System.Console.Write(1)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(x As System.Func(Of UInteger))
        System.Console.Write(2)
    End Sub

    Sub M2(x as System.Func(Of Integer))
        System.Console.Write(3)
    End Sub

    Sub M2(x As System.Func(Of UInteger))
        System.Console.Write(4)
    End Sub
End Module
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="13")
        End Sub

        <Fact>
        Public Sub NarrowingFromNumericConstant_05()
            Dim compilationDef = "
Module Module1

    Sub Main()
        M1(Long.MaxValue)
        M2(Long.MaxValue)
    End Sub

    Sub M1(x as Integer)
        System.Console.Write(1)
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Sub M1(x As UInteger)
        System.Console.Write(2)
    End Sub

    Sub M2(x as Integer)
        System.Console.Write(3)
    End Sub

    Sub M2(x As UInteger)
        System.Console.Write(4)
    End Sub
End Module
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))

            CompileAndVerify(compilation, expectedOutput:="13")

            compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))

            compilation.AssertTheseDiagnostics(
<expected>
BC30518: Overload resolution failed because no accessible 'M1' can be called with these arguments:
    'Public Sub M1(x As Integer)': Constant expression not representable in type 'Integer'.
    'Public Sub M1(x As UInteger)': Constant expression not representable in type 'UInteger'.
        M1(Long.MaxValue)
        ~~
BC30518: Overload resolution failed because no accessible 'M2' can be called with these arguments:
    'Public Sub M2(x As Integer)': Constant expression not representable in type 'Integer'.
    'Public Sub M2(x As UInteger)': Constant expression not representable in type 'UInteger'.
        M2(Long.MaxValue)
        ~~
</expected>)
        End Sub

        <Theory, CombinatorialData>
        Public Sub IncreasedPriorityWins_01_CS_Property(i1First As Boolean)

            Dim i1Source = "
[OverloadResolutionPriority(1)]
public int this[I1 x] { set { System.Console.WriteLine(1); } }
"

            Dim i2Source = "
public int this[I2 x] { set { throw null; } }
"

            Dim reference = CreateCSharpCompilation("
using System.Runtime.CompilerServices;

public interface I1 {}
public interface I2 {}
public interface I3 : I1, I2 {}

public class C
{" +
    If(i1First, i1Source, i2Source) +
    If(i1First, i2Source, i1Source) + "
}
" + OverloadResolutionPriorityAttributeDefinitionCS, parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim c As New C()
        Dim i3 As I3 = Nothing
        c(i3) = 0
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe)

            Dim c = compilation.GetTypeByMetadataName("C")
            Dim ms = c.GetMembers("Item").Cast(Of PropertySymbol)()
            For Each m In ms
                Assert.Equal(If(m.Parameters(0).Type.Name = "I1", 1, 0), m.OverloadResolutionPriority)
            Next

            CompileAndVerify(compilation, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Theory, CombinatorialData>
        Public Sub IncreasedPriorityWins_01_Property(i1First As Boolean)

            Dim i1Source = "
<OverloadResolutionPriority(1)>
public Shared WriteOnly Property M(x As I1) As Integer
    Set
        System.Console.WriteLine(1)
    End Set
End Property
"

            Dim i2Source = "
public Shared WriteOnly Property M(x As I2) As Integer
    Set
        throw DirectCast(Nothing, System.Exception)
    End Set
End Property
"

            Dim reference = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public class C" +
    If(i1First, i1Source, i2Source) +
    If(i1First, i2Source, i1Source) + "
End Class
"

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        C.M(i3) = 0
    End Sub
End Class
"

            Dim comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            Dim validate = Sub([module] As ModuleSymbol)
                               Dim c = [module].ContainingAssembly.GetTypeByMetadataName("C")
                               Dim ms = c.GetMembers("M").Cast(Of PropertySymbol)()
                               For Each m In ms
                                   Assert.Equal(If(m.Parameters(0).Type.Name = "I1", 1, 0), m.OverloadResolutionPriority)
                               Next
                           End Sub

            CompileAndVerify(comp1, expectedOutput:="1", sourceSymbolValidator:=validate, symbolValidator:=validate).VerifyDiagnostics()

            Dim compilationReference As CompilationReference = comp1.ToMetadataReference()
            Dim comp2 = CreateCompilation(source, references:={compilationReference}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="1").VerifyDiagnostics()

            Dim metadataReference As MetadataReference = comp1.EmitToImageReference()
            Dim comp3 = CreateCompilation(source, references:={metadataReference}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="1").VerifyDiagnostics()

            comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular17_13)
            CompileAndVerify(comp1, expectedOutput:="1", sourceSymbolValidator:=validate, symbolValidator:=validate).VerifyDiagnostics()

            comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            comp1.AssertTheseDiagnostics(If(i1First,
<expected><![CDATA[
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared WriteOnly Property M(x As I1) As Integer': Not most specific.
    'Public Shared WriteOnly Property M(x As I2) As Integer': Not most specific.
        C.M(i3) = 0
          ~
BC36716: Visual Basic 16.9 does not support overload resolution priority.
<OverloadResolutionPriority(1)>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>,
<expected><![CDATA[
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared WriteOnly Property M(x As I2) As Integer': Not most specific.
    'Public Shared WriteOnly Property M(x As I1) As Integer': Not most specific.
        C.M(i3) = 0
          ~
BC36716: Visual Basic 16.9 does not support overload resolution priority.
<OverloadResolutionPriority(1)>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>))

            comp2 = CreateCompilation(source, references:={compilationReference}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            Dim expected = If(i1First,
<expected>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared WriteOnly Property M(x As I1) As Integer': Not most specific.
    'Public Shared WriteOnly Property M(x As I2) As Integer': Not most specific.
        C.M(i3) = 0
          ~
</expected>,
<expected>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared WriteOnly Property M(x As I2) As Integer': Not most specific.
    'Public Shared WriteOnly Property M(x As I1) As Integer': Not most specific.
        C.M(i3) = 0
          ~
</expected>)

            comp2.AssertTheseDiagnostics(expected)

            comp3 = CreateCompilation(source, references:={metadataReference}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            comp3.AssertTheseDiagnostics(expected)
        End Sub

        <Fact>
        Public Sub ParameterlessProperty_01()
            Dim compilationDef = "
Module Module1

    Sub Main()
        M1 = 0
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(-1)>
    WriteOnly Property M1 As Integer
        Set
            System.Console.Write(1)
        End Set
    End Property

    WriteOnly Property M1(Optional x As Integer = 0) As Integer
        Set
            System.Console.Write(2)
        End Set
    End Property
End Module
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:="2")
        End Sub

        <Fact>
        Public Sub ParameterlessProperty_02()
            Dim compilationDef = "
Module Module1

    Sub Main()
        M1 = 0
    End Sub

    WriteOnly Property M1 As Integer
        Set
            System.Console.Write(1)
        End Set
    End Property

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    WriteOnly Property M1(Optional x As Integer = 0) As Integer
        Set
            System.Console.Write(2)
        End Set
    End Property
End Module
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:="2")
        End Sub

        <Fact>
        Public Sub TestObsoleteAttributeCycles()
            Dim source = "
Imports System

Public Class Test
    <Obsolete(""F1 is obsolete"")>
    <SomeAttr(F1)>
    Public Const F1 As Integer = 10

    <Obsolete(""F2 is obsolete"", True)>
    <SomeAttr(F3)>
    Public Const F2 As Integer = 10

    <Obsolete(""F3 is obsolete"")>
    <SomeAttr(F2)>
    Public Const F3 As Integer = 10

    <Obsolete(F4, True)>
    Public Const F4 As String = ""blah""

    <Obsolete(F5)>
    Public F5 As String = ""blah""

    <Obsolete(P1, True)>
    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Public ReadOnly Property P1 As String
        Get
            Return ""blah""
        End Get
    End Property

    <Obsolete>
    <SomeAttr(P2, True)>
    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Public ReadOnly Property P2 As String
        Get
            Return ""blah""
        End Get
    End Property

    <Obsolete(Method1)>
    Public Sub Method1()
    End Sub

    <Obsolete()>
    <SomeAttr1(Method2)>
    Public Sub Method2()
    End Sub

    <Obsolete(F6)>
    <SomeAttr(F6)>
    <SomeAttr(F7)>
    Public Const F6 As String = ""F6 is obsolete""

    <Obsolete(F7, True)>
    <SomeAttr(F6)>
    <SomeAttr(F7)>
    Public Const F7 As String = ""F7 is obsolete""
End Class

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Public Class SomeAttr
    Inherits Attribute
    Public Sub New(x As Integer)
    End Sub
    Public Sub New(x As String)
    End Sub
End Class

Public Class SomeAttr1
    Inherits Attribute
    Public Sub New(x As Action)
    End Sub
End Class
"
            Dim compilation = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB})
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30059: Constant expression is required.
    <Obsolete(F5)>
              ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    <Obsolete(F5)>
              ~~
BC30059: Constant expression is required.
    <Obsolete(P1, True)>
              ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    <Obsolete(P1, True)>
              ~~
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
    <SomeAttr(P2, True)>
     ~~~~~~~~
BC30059: Constant expression is required.
    <SomeAttr(P2, True)>
              ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    <SomeAttr(P2, True)>
              ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    <Obsolete(Method1)>
              ~~~~~~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    <SomeAttr1(Method2)>
               ~~~~~~~
]]></expected>
            )
        End Sub

        <Fact>
        Public Sub PropertyInAttribute_01()
            Dim source = "
Imports System

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Public Class SomeAttr
    Inherits Attribute

    Public Property P1 As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    Public Property P1(ParamArray x As Integer()) As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property

End Class

<SomeAttr(P1:=1)>
Public Class SomeAttr1
End Class
"
            Dim compilation = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB})
            compilation.AssertNoDiagnostics()

            Dim attr = compilation.GetTypeByMetadataName("SomeAttr1").GetAttributes().Single()
            Assert.Equal("SomeAttr(P1:=1)", attr.ToString())
        End Sub

        <Fact>
        Public Sub PropertyInAttribute_02()
            Dim source = "
Imports System

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Public Class SomeAttr
    Inherits Attribute

    Public Property P1(Optional x As Integer = 0) As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property

    Public Property P2(ParamArray x As Integer()) As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property

End Class

<SomeAttr(P1:=1)>
<SomeAttr(P2:=2)>
Public Class SomeAttr1
End Class
"
            Dim compilation = CreateCompilation(source)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30658: Property 'P1' with no parameters cannot be found.
<SomeAttr(P1:=1)>
          ~~
BC30658: Property 'P2' with no parameters cannot be found.
<SomeAttr(P2:=2)>
          ~~
]]></expected>
            )
        End Sub

        <Fact>
        Public Sub DefaultProperty_01()
            Dim compilationDef = "
Class Module1

    Shared Sub Main()
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(-1)>
    Default WriteOnly Property M1 As Integer
        Set
            System.Console.Write(1)
        End Set
    End Property

    Default WriteOnly Property M1(x As Integer) As Integer
        Set
            System.Console.Write(2)
        End Set
    End Property
End Class
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(
<expected>
BC31048: Properties with no required parameters cannot be declared 'Default'.
    Default WriteOnly Property M1 As Integer
                               ~~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultProperty_02()
            Dim compilationDef = "
Class Module1

    Shared Sub Main()
    End Sub

    <System.Runtime.CompilerServices.OverloadResolutionPriority(-1)>
    WriteOnly Property M1 As Integer
        Set
            System.Console.Write(1)
        End Set
    End Property

    Default WriteOnly Property M1(x As Integer) As Integer
        Set
            System.Console.Write(2)
        End Set
    End Property
End Class
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(
<expected>
BC30361: 'Public WriteOnly Default Property M1(x As Integer) As Integer' and 'Public WriteOnly Property M1 As Integer' cannot overload each other because only one is declared 'Default'.
    WriteOnly Property M1 As Integer
                       ~~
</expected>)
        End Sub

        <Theory, CombinatorialData>
        Public Sub DefaultProperty_03(i1First As Boolean)

            Dim i1Source = "
<OverloadResolutionPriority(1)>
public Default WriteOnly Property M(x As I1) As Integer
    Set
        System.Console.Write(1)
    End Set
End Property
"

            Dim i2Source = "
public Default WriteOnly Property M(x As I2) As Integer
    Set
        throw DirectCast(Nothing, System.Exception)
    End Set
End Property
"

            Dim reference = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public class C" +
    If(i1First, i1Source, i2Source) +
    If(i1First, i2Source, i1Source) + "
End Class
"

            Dim source = "
public class Program 
    Shared Sub Main
        Dim c as New C()
        Dim i3 As I3 = Nothing
        c.M(i3) = 0
        c(i3) = 0
    End Sub
End Class
"

            Dim comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            Dim validate = Sub([module] As ModuleSymbol)
                               Dim c = [module].ContainingAssembly.GetTypeByMetadataName("C")
                               Dim ms = c.GetMembers("M").Cast(Of PropertySymbol)()
                               For Each m In ms
                                   Assert.Equal(If(m.Parameters(0).Type.Name = "I1", 1, 0), m.OverloadResolutionPriority)
                               Next
                           End Sub

            CompileAndVerify(comp1, expectedOutput:="11", sourceSymbolValidator:=validate, symbolValidator:=validate).VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="11").VerifyDiagnostics()

            Dim comp3 = CreateCompilation(source, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="11").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub WriteOnlyVsReadOnlyProperty_01()

            Dim compilationDef = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public class C
    <OverloadResolutionPriority(1)>
    public Shared WriteOnly Property M(x As I1) As Integer
        Set
            System.Console.WriteLine(1)
        End Set
    End Property
    public Shared ReadOnly Property M(x As I2) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
    End Property
End Class

public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        Dim x = C.M(i3)
    End Sub
End Class
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(
<expected>
BC30524: Property 'M' is 'WriteOnly'.
        Dim x = C.M(i3)
                ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub WriteOnlyVsReadOnlyProperty_02()

            Dim compilationDef = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public class C
    public Shared WriteOnly Property M(x As I1) As Integer
        Set
            System.Console.WriteLine(1)
        End Set
    End Property
    <OverloadResolutionPriority(1)>
    public Shared ReadOnly Property M(x As I2) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
    End Property
End Class

public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        C.M(i3) = 0
    End Sub
End Class
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(
<expected>
BC30526: Property 'M' is 'ReadOnly'.
        C.M(i3) = 0
        ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub LiftedOperator_01()
            Dim reference = "
public Structure S

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    public shared operator-(x As S) As S
        System.Console.Write(1)
        return Nothing
    End Operator

    public shared operator-(x As S?) As S?
        System.Console.Write(2)
        return Nothing
    End Operator
End Structure
"
            Dim source = "
Module Module1
    Sub Main()
        Dim s As New S?(Nothing)
        s = -s
    End Sub
End Module
"
            Dim compilation = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:="1")

            compilation = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular17_13)
            CompileAndVerify(compilation, expectedOutput:="1")

            compilation = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular16_9)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16.9 does not support overload resolution priority.
    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

            Dim ref = CreateCompilation({reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseDll)

            compilation = CreateCompilation(source, references:={ref.ToMetadataReference()}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular16_9)
            CompileAndVerify(compilation, expectedOutput:="2")

            compilation = CreateCompilation(source, references:={ref.EmitToImageReference()}, options:=TestOptions.ReleaseExe, parseOptions:=TestOptions.Regular16_9)
            CompileAndVerify(compilation, expectedOutput:="2")
        End Sub

        <Fact>
        Public Sub Plus2()
            Dim compilationDef = "
Option Strict Off

Imports System

Module Module1

    Structure S1

        <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
        Public Shared Operator +(x As S1) As S1
            System.Console.WriteLine(""+(x As S1) As S1"")
            Return x
        End Operator

        <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
        Public Shared Operator +(x As S1?) As Integer
            System.Console.WriteLine(""+(x As S1?) As Integer"")
            Return 0
        End Operator

    End Structure

    Sub Main()
        Dim y1 = +New S1()
        System.Console.WriteLine(""-----"")
        Dim y2 = +New S1?()
        System.Console.WriteLine(""-----"")
        Dim y3 = +New S1?(New S1())
    End Sub
End Module
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
+(x As S1) As S1
-----
+(x As S1?) As Integer
-----
+(x As S1?) As Integer
]]>)
        End Sub

        <Fact>
        Public Sub ConversionOperator_01()
            Dim compilationDef = "
Structure S

    public shared widening operator CType(x As Integer) As S
        System.Console.Write(1)
        return Nothing
    End Operator

    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
    public shared widening operator CType(x As Long) As S
        System.Console.Write(2)
        return Nothing
    End Operator
End Structure

Module Module1
    Sub Main()
        Dim val as Integer = 0
        Dim s As S = val
    End Sub
End Module
"
            Dim compilation = CreateCompilation({compilationDef, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.ReleaseExe)

            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC37334: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
    <System.Runtime.CompilerServices.OverloadResolutionPriority(1)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Theory, CombinatorialData>
        Public Sub IncreasedPriorityWins_02_CS(i1First As Boolean)

            Dim reference = CreateCSharpCompilation("
using System.Runtime.CompilerServices;

public interface I1 {}
public interface I2 {}
public interface I3 : I1, I2 {}

public class C
{
    [OverloadResolutionPriority(2)]
    public static void M(object o) => System.Console.WriteLine(1);

    [OverloadResolutionPriority(1)]
    public static void M(I1 x) => throw null;

    public static void M(I2 x) => throw null;
}
" + OverloadResolutionPriorityAttributeDefinitionCS, parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        C.M(i3)
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub IncreasedPriorityWins_02()

            Dim reference = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public class C
    <OverloadResolutionPriority(2)>
    public Shared Sub M(x As Object)
        System.Console.WriteLine(1)
    End Sub
    <OverloadResolutionPriority(1)>
    public Shared Sub M(x As I1)
        throw DirectCast(Nothing, System.Exception)
    End Sub
    public Shared Sub M(x As I2)
        throw DirectCast(Nothing, System.Exception)
    End Sub
End Class
"

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        C.M(i3)
    End Sub
End Class
"

            Dim comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            CompileAndVerify(comp1, expectedOutput:="1").VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="1").VerifyDiagnostics()

            Dim comp3 = CreateCompilation(source, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Theory, CombinatorialData>
        Public Sub DecreasedPriorityLoses(i1First As Boolean)

            Dim i1Source = "
public Shared Sub M(x As I1)
    System.Console.WriteLine(1)
End Sub
"

            Dim i2Source = "
<OverloadResolutionPriority(-1)>
public Shared Sub M(x As I2)
    throw DirectCast(Nothing, System.Exception)
End Sub
"

            Dim reference = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public class C" +
    If(i1First, i1Source, i2Source) +
    If(i1First, i2Source, i1Source) + "
End Class
"

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        C.M(i3)
    End Sub
End Class
"

            Dim comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            CompileAndVerify(comp1, expectedOutput:="1").VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="1").VerifyDiagnostics()

            Dim comp3 = CreateCompilation(source, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub ZeroIsTreatedAsDefault()

            Dim reference = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public class C
    public Shared Sub M(x As I1)
        System.Console.WriteLine(1)
    End Sub
    <OverloadResolutionPriority(0)>
    public Shared Sub M(x As I2)
        throw DirectCast(Nothing, System.Exception)
    End Sub
End Class
"

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        C.M(i3)
    End Sub
End Class
"

            Dim comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            Dim expected =
<expected>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared Sub M(x As I1)': Not most specific.
    'Public Shared Sub M(x As I2)': Not most specific.
        C.M(i3)
          ~
</expected>

            comp1.AssertTheseDiagnostics(expected)

            comp1 = CreateCompilation({reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)
            Dim validate = Sub([module] As ModuleSymbol)
                               Dim c = [module].ContainingAssembly.GetTypeByMetadataName("C")
                               Dim ms = c.GetMembers("M").Cast(Of MethodSymbol)()
                               For Each m In ms
                                   Assert.Equal(0, m.OverloadResolutionPriority)
                               Next
                           End Sub
            CompileAndVerify(comp1, sourceSymbolValidator:=validate, symbolValidator:=validate).VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            comp2.AssertTheseDiagnostics(expected)

            Dim comp3 = CreateCompilation(source, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            comp3.AssertTheseDiagnostics(expected)
        End Sub

        <Theory>
        <InlineData(-1)>
        <InlineData(1)>
        <InlineData(Integer.MaxValue)>
        <InlineData(Integer.MinValue)>
        Public Sub AmbiguityWithinPriority(priority As Integer)

            Dim reference = $"
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public class C
    <OverloadResolutionPriority({priority})>
    public Shared Sub M(x As I1)
        System.Console.WriteLine(1)
    End Sub
    <OverloadResolutionPriority({priority})>
    public Shared Sub M(x As I2)
        throw DirectCast(Nothing, System.Exception)
    End Sub
End Class
"

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        C.M(i3)
    End Sub
End Class
"

            Dim comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            Dim expected =
<expected>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared Sub M(x As I1)': Not most specific.
    'Public Shared Sub M(x As I2)': Not most specific.
        C.M(i3)
          ~
</expected>

            comp1.AssertTheseDiagnostics(expected)
        End Sub

        <Fact>
        Public Sub MethodDiscoveryStopsAtFirstApplicableMethod_CS()

            Dim reference = CreateCSharpCompilation("
using System.Runtime.CompilerServices;

public interface I1 {}
public interface I2 {}
public interface I3 : I1, I2 {}

public class Base
{
    [OverloadResolutionPriority(1)]
    public static void M(I2 x) => throw null;
}
public class Derived : Base
{
    public static void M(I1 x) => System.Console.WriteLine(1);
}
" + OverloadResolutionPriorityAttributeDefinitionCS, parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        Derived.M(i3)
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe)

            ' Unlike C#, VB doesn't stop collecting candidates at Derived
            compilation.AssertTheseDiagnostics(
<expected>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared Overloads Sub Derived.M(x As I1)': Not most specific.
    'Public Shared Overloads Sub Base.M(x As I2)': Not most specific.
        Derived.M(i3)
                ~
</expected>)
        End Sub

        <Fact>
        Public Sub MethodDiscoveryStopsAtFirstApplicableIndexer_CS()

            Dim reference = CreateCSharpCompilation("
using System.Runtime.CompilerServices;

public interface I1 {}
public interface I2 {}
public interface I3 : I1, I2 {}

public class Base
{
    [OverloadResolutionPriority(1)]
    public int this[I2 x]
    {
        get => throw null;
        set => throw null;
    }
}
public class Derived : Base
{
    public int this[I1 x]
    {
        get { System.Console.Write(1); return 1; }
        set => System.Console.Write(2);
    }
}
" + OverloadResolutionPriorityAttributeDefinitionCS, parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        Dim d = new Derived()
        Dim x = d(i3)
        d(i3) = 0
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe)

            ' Unlike C#, VB doesn't stop collecting candidates at Derived
            compilation.AssertTheseDiagnostics(
<expected>
BC30521: Overload resolution failed because no accessible 'Item' is most specific for these arguments:
    'Public Overloads Default Property Derived.Item(x As I1) As Integer': Not most specific.
    'Public Overloads Default Property Base.Item(x As I2) As Integer': Not most specific.
        Dim x = d(i3)
                ~
BC30521: Overload resolution failed because no accessible 'Item' is most specific for these arguments:
    'Public Overloads Default Property Derived.Item(x As I1) As Integer': Not most specific.
    'Public Overloads Default Property Base.Item(x As I2) As Integer': Not most specific.
        d(i3) = 0
        ~
</expected>)
        End Sub

        <Fact>
        Public Sub OrderingWithAnExtensionMethodContainingClass_CS()

            Dim reference = CreateCSharpCompilation("
using System.Runtime.CompilerServices;

public interface I1 {}
public interface I2 {}
public interface I3 : I1, I2 {}

public static class C
{
    [OverloadResolutionPriority(1)]
    public static void M(this I1 x) => System.Console.WriteLine(1);

    public static void M(this I2 x) => throw null;
}
" + OverloadResolutionPriorityAttributeDefinitionCS, parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        i3.M()
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub DoesNotOrderBetweenExtensionMethodContainingClasses_CS()

            Dim reference = CreateCSharpCompilation("
using System.Runtime.CompilerServices;

public interface I1 {}
public interface I2 {}
public interface I3 : I1, I2 {}

public static class C1
{
    [OverloadResolutionPriority(1)]
    public static void M(this I1 x) => System.Console.WriteLine(1);
}

public static class C2
{
    public static void M(this I2 x) => throw null;
}
" + OverloadResolutionPriorityAttributeDefinitionCS, parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim i3 As I3 = Nothing
        i3.M()
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe)
            compilation.AssertTheseDiagnostics(
<expected>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    Extension method 'Public Sub M()' defined in 'C1': Not most specific.
    Extension method 'Public Sub M()' defined in 'C2': Not most specific.
        i3.M()
           ~
</expected>)
        End Sub

        <Fact>
        Public Sub Overrides_NoPriorityChangeFromBase_Methods_CS()

            Dim reference = CreateCSharpCompilation("
using System.Runtime.CompilerServices;

public class Base
{
    [OverloadResolutionPriority(1)]
    public virtual void M(object o) => throw null;
    public virtual void M(string s) => throw null;
}

public class Derived : Base
{
    public override void M(object o) => System.Console.WriteLine(""1"");
    public override void M(string s) => throw null;
}
" + OverloadResolutionPriorityAttributeDefinitionCS, parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source = "
public class Program 
    Shared Sub Main
        Dim d = new Derived()
        d.M(""test"")
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Overrides_ChangePriorityInSource_Methods()

            Dim source = "
Imports System.Runtime.CompilerServices

public class Base
    <OverloadResolutionPriority(1)>
    public Overridable Sub M(o As Object)
        throw DirectCast(Nothing, System.Exception)
    End Sub
    public Overridable Sub M(s As String)
        throw DirectCast(Nothing, System.Exception)
    End Sub
    <OverloadResolutionPriority(3)>
    public Overridable Sub M2(o As Object)
        throw DirectCast(Nothing, System.Exception)
    End Sub
End Class

public class Derived
    Inherits Base

    <OverloadResolutionPriority(0)>
    public Overrides Sub M(o As Object)
        System.Console.WriteLine(""1"")
    End Sub
    <OverloadResolutionPriority(2)>
    public Overrides Sub M(s As String)
        throw DirectCast(Nothing, System.Exception)
    End Sub
    <OverloadResolutionPriority(3)> ' Priority matches
    public Overrides Sub M2(o As Object)
        System.Console.WriteLine(""1"")
    End Sub
End Class

public class Program 
    Shared Sub Main
        Dim d = new Derived()
        d.M(""test"")
    End Sub
End Class
"

            Dim compilation = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC37333: Cannot use 'OverloadResolutionPriorityAttribute' on an overriding member.
    <OverloadResolutionPriority(0)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37333: Cannot use 'OverloadResolutionPriorityAttribute' on an overriding member.
    <OverloadResolutionPriority(2)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37333: Cannot use 'OverloadResolutionPriorityAttribute' on an overriding member.
    <OverloadResolutionPriority(3)> ' Priority matches
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub Overrides_ChangePriorityInMetadata_Methods()

            Dim source1 = "
Imports System.Runtime.CompilerServices

public class Base
    <OverloadResolutionPriority(1)>
    public Overridable Sub M(o As Object)
        throw DirectCast(Nothing, System.Exception)
    End Sub
    public Overridable Sub M(s As String)
        throw DirectCast(Nothing, System.Exception)
    End Sub
End Class
"

            Dim comp1 = CreateCompilation({source1, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll, assemblyName:="assembly1")
            Dim assembly1 = comp1.EmitToImageReference()

            ' Equivalent to:
            '
            ' public class Derived : Base
            ' {
            '     [OverloadResolutionPriority(0)]
            '     public override void M(object o) => System.Console.WriteLine("1");
            '     [OverloadResolutionPriority(2)]
            '     public override void M(string s) => throw null;
            ' }
            Dim il2 = "
.assembly extern assembly1 {}

.class public auto ansi beforefieldinit Derived extends [assembly1]Base
{
    .method public hidebysig virtual 
        instance void M (
            object o
        ) cil managed 
    {
        .custom instance void [assembly1]System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
            01 00 00 00 00 00 00 00
        )
        ldstr ""1""
        call void [mscorlib]System.Console::WriteLine(string)
        ret
    } // end of method Derived::M

    .method public hidebysig virtual 
        instance void M (
            string s
        ) cil managed 
    {
        .custom instance void [assembly1]System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
            01 00 02 00 00 00 00 00
        )
        ldnull
        throw
    } // end of method Derived::M

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [assembly1]Base::.ctor()
        ret
    } // end of method Derived::.ctor

}
"

            Dim source = "
public class Program 
    Shared Sub Main
        Dim d = new Derived()
        d.M(""test"")
    End Sub
End Class
"
            Dim assembly2 = CompileIL(il2)

            Dim compilation = CreateCompilation(source, references:={assembly1, assembly2}, options:=TestOptions.DebugExe)

            CompileAndVerify(compilation, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Overrides_ChangePriorityInMetadata_Indexers()

            Dim source1 = "
Imports System.Runtime.CompilerServices

public class Base
    <OverloadResolutionPriority(1)>
    public Overridable Default Property Item(o As Object) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property
    public Overridable Default Property Item(s As String) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property
End Class
"

            Dim comp1 = CreateCompilation({source1, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll, assemblyName:="assembly1")
            Dim assembly1 = comp1.EmitToImageReference()

            ' Equivalent to:
            '
            ' public class Derived: Base
            ' {
            '     public override int this[object o]
            '     {
            '         get { System.Console.Write(1); return 1; }
            '         set => System.Console.Write(2);
            '     }
            '     [OverloadResolutionPriority(2)]
            '     public override int this[string s]
            '     {
            '         get => throw null;
            '         set => throw null;
            '     }
            ' }
            Dim il2 = "
.assembly extern assembly1 {}

.class public auto ansi beforefieldinit Derived extends [assembly1]Base
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )
    // Methods
    .method public hidebysig specialname virtual 
        instance int32 get_Item (
            object o
        ) cil managed 
    {
        ldc.i4.1
        call void [mscorlib]System.Console::Write(int32)
        ldc.i4.1
        ret
    } // end of method Derived::get_Item

    .method public hidebysig specialname virtual 
        instance void set_Item (
            object o,
            int32 'value'
        ) cil managed 
    {
        ldc.i4.2
        call void [mscorlib]System.Console::Write(int32)
        ret
    } // end of method Derived::set_Item

    .method public hidebysig specialname virtual 
        instance int32 get_Item (
            string s
        ) cil managed 
    {
        ldnull
        throw
    } // end of method Derived::get_Item

    .method public hidebysig specialname virtual 
        instance void set_Item (
            string s,
            int32 'value'
        ) cil managed 
    {
        ldnull
        throw
    } // end of method Derived::set_Item

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [assembly1]Base::.ctor()
        ret
    } // end of method Derived::.ctor

    // Properties
    .property instance int32 Item(
        object o
    )
    {
        .get instance int32 Derived::get_Item(object)
        .set instance void Derived::set_Item(object, int32)
    }
    .property instance int32 Item(
        string s
    )
    {
        .custom instance void [assembly1]System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
            01 00 02 00 00 00 00 00
        )
        .get instance int32 Derived::get_Item(string)
        .set instance void Derived::set_Item(string, int32)
    }
}
"

            Dim source = "
public class Program 
    Shared Sub Main
        Dim d = new Derived()
        Dim x = d(""test"")
        d(""test"") = 0
        x = d.Item(""test"")
        d.Item(""test"") = 0
    End Sub
End Class
"
            Dim assembly2 = CompileIL(il2)

            Dim compilation = CreateCompilation(source, references:={assembly1, assembly2}, options:=TestOptions.DebugExe)

            CompileAndVerify(compilation, expectedOutput:="1212").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Overrides_ChangePriorityInSource_Indexers()

            Dim source = "
Imports System.Runtime.CompilerServices

public class Base
    <OverloadResolutionPriority(1)>
    public Overridable Default Property Item(o As Object) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property
    public Overridable Default Property Item(s As String) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property
    <OverloadResolutionPriority(3)>
    public Overridable Default Property Item(o As Integer) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property
End Class

public class Derived
    Inherits Base

    public Overrides Default Property Item(o As Object) As Integer
        Get
             System.Console.Write(1)
             return 1
        End Get
        Set
            System.Console.Write(2)
        End Set
    End Property

    <OverloadResolutionPriority(2)>
    public Overrides Default Property Item(s As String) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property

    <OverloadResolutionPriority(3)> ' Priority matches
    public Overrides Default Property Item(o As Integer) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property
End Class

public class Program 
    Shared Sub Main
        Dim d = new Derived()
        Dim x = d(""test"")
        d(""test"") = 0
        x = d.Item(""test"")
        d.Item(""test"") = 0
    End Sub
End Class
"
            Dim compilation = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC37333: Cannot use 'OverloadResolutionPriorityAttribute' on an overriding member.
    <OverloadResolutionPriority(2)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37333: Cannot use 'OverloadResolutionPriorityAttribute' on an overriding member.
    <OverloadResolutionPriority(3)> ' Priority matches
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ThroughRetargeting_Methods()

            Dim source1 = "
public class RetValue
End Class
"

            Dim comp1_1 = CreateCompilation(source1, options:=TestOptions.DebugDll, assemblyName:="Ret")
            DirectCast(comp1_1.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Ret", New Version(1, 0, 0, 0), isRetargetable:=True)
            Dim comp1_2 = CreateCompilation(source1, options:=TestOptions.DebugDll, assemblyName:="Ret")
            DirectCast(comp1_2.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Ret", New Version(2, 0, 0, 0), isRetargetable:=True)

            Dim source2 = "
Imports System.Runtime.CompilerServices

public class C
    <OverloadResolutionPriority(1)>
    public Function M(o As Object) As RetValue
        System.Console.WriteLine(""1"")
        return new RetValue()
    End Function
    public Function M(s As String)
        throw DirectCast(Nothing, System.Exception)
    End Function
End Class
"

            Dim comp2 = CreateCompilation({source2, OverloadResolutionPriorityAttributeDefinitionVB}, references:={comp1_1.ToMetadataReference()}, options:=TestOptions.DebugDll)
            comp2.VerifyDiagnostics()

            Dim source3 = "
public class Program 
    Shared Sub Main
        Dim c = new C()
        c.M(""test"")
    End Sub
End Class
"

            Dim comp3 = CreateCompilation(source3, references:={comp2.ToMetadataReference(), comp1_2.ToMetadataReference()}, options:=TestOptions.DebugExe)
            Dim c = comp3.GetTypeByMetadataName("C")
            Dim ms = c.GetMembers("M").Cast(Of MethodSymbol)().ToArray()
            Assert.Equal(2, ms.Length)
            Assert.All(ms, Function(m) Assert.IsType(Of RetargetingMethodSymbol)(m))
            AssertEx.Equal("Function C.M(o As System.Object) As RetValue", ms(0).ToTestDisplayString())
            Assert.Equal(1, ms(0).OverloadResolutionPriority)
            AssertEx.Equal("Function C.M(s As System.String) As System.Object", ms(1).ToTestDisplayString())
            Assert.Equal(0, ms(1).OverloadResolutionPriority)

            comp3.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub ThroughRetargeting_Indexers()

            Dim source1 = "
public class RetValue
End Class
"

            Dim comp1_1 = CreateCompilation(source1, options:=TestOptions.DebugDll, assemblyName:="Ret")
            DirectCast(comp1_1.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Ret", New Version(1, 0, 0, 0), isRetargetable:=True)
            Dim comp1_2 = CreateCompilation(source1, options:=TestOptions.DebugDll, assemblyName:="Ret")
            DirectCast(comp1_2.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Ret", New Version(2, 0, 0, 0), isRetargetable:=True)

            Dim source2 = "
Imports System.Runtime.CompilerServices

public class C
    <OverloadResolutionPriority(1)>
    public Default Property M(o As Object) As RetValue
        Get
            System.Console.WriteLine(""1"")
            return new RetValue()
        End Get
        Set
            System.Console.WriteLine(""2"")
        End Set
    End Property
    public Default Property M(s As String)
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property
End Class
"

            Dim comp2 = CreateCompilation({source2, OverloadResolutionPriorityAttributeDefinitionVB}, references:={comp1_1.ToMetadataReference()}, options:=TestOptions.DebugDll)
            comp2.VerifyDiagnostics()

            Dim source3 = "
public class Program 
    Shared Sub Main
        Dim c = new C()
        Dim x = c(""test"")
        c(""test"") = new RetValue()
    End Sub
End Class
"

            Dim comp3 = CreateCompilation(source3, references:={comp2.ToMetadataReference(), comp1_2.ToMetadataReference()}, options:=TestOptions.DebugExe)
            Dim c = comp3.GetTypeByMetadataName("C")
            Dim ms = c.GetMembers("M").Cast(Of PropertySymbol)().ToArray()
            Assert.Equal(2, ms.Length)
            Assert.All(ms, Function(m) Assert.IsType(Of RetargetingPropertySymbol)(m))
            AssertEx.Equal("Property C.M(o As System.Object) As RetValue", ms(0).ToTestDisplayString())
            Assert.Equal(1, ms(0).OverloadResolutionPriority)
            AssertEx.Equal("Property C.M(s As System.String) As System.Object", ms(1).ToTestDisplayString())
            Assert.Equal(0, ms(1).OverloadResolutionPriority)

            comp3.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AppliedToAttributeConstructors()

            Dim source = "
Imports System.Runtime.CompilerServices

<C(""test"")>
public class C
    Inherits System.Attribute

    <OverloadResolutionPriority(1)>
    public Sub New(o As object)
    End Sub

    public Sub New(s As string)
    End Sub
End Class
"

            Dim compilation = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)
            CompileAndVerify(compilation).VerifyDiagnostics()

            Dim c = compilation.GetTypeByMetadataName("C")

            Dim attr = c.GetAttributes().Single()
            AssertEx.Equal("Sub C..ctor(o As System.Object)", attr.AttributeConstructor.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub CycleOnOverloadResolutionPriorityConstructor_01()

            Dim source = "
namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Constructor Or AttributeTargets.Property, AllowMultiple:= false, Inherited:= false)>
    public class OverloadResolutionPriorityAttribute
        Inherits Attribute

        <OverloadResolutionPriority(1)>
        Public Sub New(priority As Integer)
            Me.Priority = priority
        End Sub

        public Readonly Property Priority As Integer
    End Class
End Namespace
"

            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll)
            CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Theory>
        <InlineData(0)>
        <InlineData(1)>
        Public Sub CycleOnOverloadResolutionPriorityConstructor_02(ctorToForce As Integer)

            Dim source = "
Imports System.Runtime.CompilerServices

namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Constructor Or AttributeTargets.Property, AllowMultiple:= false, Inherited:= false)>
    public class OverloadResolutionPriorityAttribute
        Inherits Attribute

        Public Sub New(priority As Integer)
            Me.Priority = priority
        End Sub

        ' Attribute is intentionally ignored, as this will cause a cycle
        <OverloadResolutionPriority(1)>
        Public Sub New(priority As Object)
            Me.Priority = priority
        End Sub

        public Readonly Property Priority As Integer
    End Class
End Namespace

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public Module Program 

    <OverloadResolutionPriority(1)>
    <Extension>
    Sub M(this As I1)
        System.Console.WriteLine(1)
    End Sub

    <Extension>
    Sub M(this As I2)
        throw DirectCast(Nothing, System.Exception)
    End Sub

    Sub Main
        Dim i3 As I3 = Nothing
        i3.M()
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugExe)
            compilation.AssertTheseEmitDiagnostics(
<expected>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    Extension method 'Public Sub M()' defined in 'Program': Not most specific.
    Extension method 'Public Sub M()' defined in 'Program': Not most specific.
        i3.M()
           ~
</expected>
            )

            compilation = CreateCompilation(source, options:=TestOptions.DebugExe)
            Dim ctors = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute").Constructors
            Dim ctor = ctors(1)
            AssertEx.Equal("Sub System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(priority As System.Object)", ctor.ToTestDisplayString())

            ctors(ctorToForce).GetAttributes()
            compilation.AssertTheseEmitDiagnostics(
<expected>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    Extension method 'Public Sub M()' defined in 'Program': Not most specific.
    Extension method 'Public Sub M()' defined in 'Program': Not most specific.
        i3.M()
           ~
</expected>
            )

            Assert.Equal(1, ctor.OverloadResolutionPriority)
            AssertEx.Equal("Sub System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(priority As System.Int32)", ctor.GetAttributes().Single().AttributeConstructor.ToTestDisplayString())

            Dim m = compilation.GetTypeByMetadataName("Program").GetMembers("M").OfType(Of MethodSymbol)().First()
            Assert.Equal(0, m.OverloadResolutionPriority)
            AssertEx.Equal("Sub System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(priority As System.Object)", m.GetAttributes().First().AttributeConstructor.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub CycleOnOverloadResolutionPriorityConstructor_04()

            Dim source = "
Imports System.Runtime.CompilerServices

namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Constructor Or AttributeTargets.Property, AllowMultiple:= false, Inherited:= false)>
    public class OverloadResolutionPriorityAttribute
        Inherits Attribute

        <OtherAttribute()>
        Public Sub New(priority As Integer)
            Me.Priority = priority
        End Sub

        public Readonly Property Priority As Integer
    End Class
End Namespace

public class OtherAttribute
    Inherits System.Attribute

    <OverloadResolutionPriority(1)>
    Public Sub New()
    End Sub
End Class
"

            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll)
            CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub CycleOnOverloadResolutionPriorityConstructor_05()

            Dim source = "
Imports System.Runtime.CompilerServices

namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Constructor Or AttributeTargets.Property, AllowMultiple:= false, Inherited:= false)>
    public class OverloadResolutionPriorityAttribute
        Inherits Attribute

        <ObsoleteAttribute()>
        Public Sub New(priority As Integer)
            Me.Priority = priority
        End Sub

        public Readonly Property Priority As Integer
    End Class
End Namespace

Namespace System
    public class ObsoleteAttribute
        Inherits System.Attribute

        <OverloadResolutionPriority(1)>
        Public Sub New()
        End Sub
    End Class
End Namespace
"

            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll)
            CompileAndVerify(compilation).Diagnostics.AssertTheseDiagnostics(
<expected><![CDATA[
BC40008: 'Public Sub New(priority As Integer)' is obsolete.
        <OverloadResolutionPriority(1)>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>
            )
        End Sub

        <Theory>
        <InlineData(0)>
        <InlineData(1)>
        Public Sub CycleOnOverloadResolutionPriorityConstructor_06(ctorToForce As Integer)

            Dim source = "
Imports System.Runtime.CompilerServices

namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Constructor Or AttributeTargets.Property, AllowMultiple:= false, Inherited:= false)>
    public class OverloadResolutionPriorityAttribute
        Inherits Attribute

        Public Sub New(priority As Integer)
            Me.Priority = priority
        End Sub

        ' Attribute is intentionally ignored, as this will cause a cycle
        <OverloadResolutionPriority(CType(-1, SByte))>
        Public Sub New(priority As SByte)
            Me.Priority = priority
        End Sub

        public Readonly Property Priority As Integer
    End Class
End Namespace

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public Module Program 

    <OverloadResolutionPriority(1)>
    <Extension>
    Sub M(this As I1)
        System.Console.WriteLine(1)
    End Sub

    <Extension>
    Sub M(this As I2)
        throw DirectCast(Nothing, System.Exception)
    End Sub

    Sub Main
        Dim i3 As I3 = Nothing
        i3.M()
    End Sub
End Module
"

            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:="1").VerifyDiagnostics()

            compilation = CreateCompilation(source, options:=TestOptions.DebugExe)
            Dim ctors = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute").Constructors
            Dim ctor = ctors(1)
            AssertEx.Equal("Sub System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(priority As System.SByte)", ctor.ToTestDisplayString())

            ctors(ctorToForce).GetAttributes()
            CompileAndVerify(compilation, expectedOutput:="1").VerifyDiagnostics()

            Assert.Equal(0, ctor.OverloadResolutionPriority)
            AssertEx.Equal("Sub System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(priority As System.SByte)", ctor.GetAttributes().Single().AttributeConstructor.ToTestDisplayString())

            Dim m = compilation.GetTypeByMetadataName("Program").GetMembers("M").OfType(Of MethodSymbol)().First()
            Assert.Equal(1, m.OverloadResolutionPriority)
            AssertEx.Equal("Sub System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(priority As System.Int32)", m.GetAttributes().First().AttributeConstructor.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub CycleOnOverloadResolutionPriorityConstructor_07()

            Dim source = "
Imports System.Runtime.CompilerServices

Namespace System
    public class ObsoleteAttribute
        Inherits System.Attribute

        Public Sub New(x As String)
        End Sub

        <OverloadResolutionPriority(1)>
        Public Sub New(x As String, Optional y As Boolean = False)
        End Sub
        
    End Class
End Namespace

<System.Obsolete(""Test"")>
Class C
End Class

Class D
    Dim x As C
End Class
"

            Dim compilation = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)
            Dim verifier = CompileAndVerify(
                    compilation,
                    symbolValidator:=Sub(m)
                                         AssertEx.Equal("Sub System.ObsoleteAttribute..ctor(x As System.String, [y As System.Boolean = False])",
                                                        m.ContainingAssembly.GetTypeByMetadataName("C").GetAttributes.Single().AttributeConstructor.ToTestDisplayString())
                                     End Sub)

            verifier.Diagnostics.AssertTheseDiagnostics(
<expected><![CDATA[
BC40000: 'C' is obsolete: 'Test'.
    Dim x As C
             ~
]]></expected>
            )
        End Sub

        <Fact>
        Public Sub CycleOnOverloadResolutionPriorityConstructor_08()

            Dim source = "
Imports System.Runtime.CompilerServices

Namespace System
    public class ObsoleteAttribute
        Inherits System.Attribute

        Public Sub New(x As String)
        End Sub

        <OverloadResolutionPriority(1)>
        Public Sub New(x As String, Optional y As Boolean = True)
        End Sub
        
    End Class
End Namespace

<System.Obsolete(""Test"")>
Class C
End Class

Class D
    Dim x As C
End Class
"

            Dim compilation = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)
            AssertEx.Equal("Sub System.ObsoleteAttribute..ctor(x As System.String, [y As System.Boolean = True])",
                           compilation.GetTypeByMetadataName("C").GetAttributes.Single().AttributeConstructor.ToTestDisplayString())

            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30668: 'C' is obsolete: 'Test'.
    Dim x As C
             ~
]]></expected>
            )
        End Sub

        <Theory, CombinatorialData>
        Public Sub OverloadResolutionAppliedToIndexers(i1First As Boolean)

            Dim i1Source = "
<OverloadResolutionPriority(1)>
public Default Property Item(x As I1) As Integer
    Get
        System.Console.Write(1)
        Return 0
    End Get
    Set
        System.Console.Write(2)
    End Set
End Property
"

            Dim i2Source = "
public Default Property Item(x As I2) As Integer
    Get
        throw DirectCast(Nothing, System.Exception)
    End Get
    Set
        throw DirectCast(Nothing, System.Exception)
    End Set
End Property
"

            Dim reference = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

public class C" +
    If(i1First, i1Source, i2Source) +
    If(i1First, i2Source, i1Source) + "
End Class
"

            Dim source = "
public class Program 
    Shared Sub Main
        Dim c = new C()
        Dim i3 As I3 = Nothing
        Dim x = c(i3)
        c(i3) = 0
    End Sub
End Class
"

            Dim comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            Dim validate = Sub([module] As ModuleSymbol)
                               Dim c = [module].ContainingAssembly.GetTypeByMetadataName("C")
                               Dim ms = c.GetMembers("Item").Cast(Of PropertySymbol)()
                               For Each m In ms
                                   Assert.Equal(If(m.Parameters(0).Type.Name = "I1", 1, 0), m.OverloadResolutionPriority)
                               Next
                           End Sub

            CompileAndVerify(comp1, expectedOutput:="12", sourceSymbolValidator:=validate, symbolValidator:=validate).VerifyDiagnostics()

            Dim compilationReference As CompilationReference = comp1.ToMetadataReference()
            Dim comp2 = CreateCompilation(source, references:={compilationReference}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="12").VerifyDiagnostics()

            Dim metadataReference As MetadataReference = comp1.EmitToImageReference()
            Dim comp3 = CreateCompilation(source, references:={metadataReference}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="12").VerifyDiagnostics()

            comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular17_13)
            CompileAndVerify(comp1, expectedOutput:="12", sourceSymbolValidator:=validate, symbolValidator:=validate).VerifyDiagnostics()

            comp1 = CreateCompilation({source, reference, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            comp1.AssertTheseDiagnostics(If(i1First,
<expected><![CDATA[
BC30521: Overload resolution failed because no accessible 'Item' is most specific for these arguments:
    'Public Default Property Item(x As I1) As Integer': Not most specific.
    'Public Default Property Item(x As I2) As Integer': Not most specific.
        Dim x = c(i3)
                ~
BC30521: Overload resolution failed because no accessible 'Item' is most specific for these arguments:
    'Public Default Property Item(x As I1) As Integer': Not most specific.
    'Public Default Property Item(x As I2) As Integer': Not most specific.
        c(i3) = 0
        ~
BC36716: Visual Basic 16.9 does not support overload resolution priority.
<OverloadResolutionPriority(1)>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>,
<expected><![CDATA[
BC30521: Overload resolution failed because no accessible 'Item' is most specific for these arguments:
    'Public Default Property Item(x As I2) As Integer': Not most specific.
    'Public Default Property Item(x As I1) As Integer': Not most specific.
        Dim x = c(i3)
                ~
BC30521: Overload resolution failed because no accessible 'Item' is most specific for these arguments:
    'Public Default Property Item(x As I2) As Integer': Not most specific.
    'Public Default Property Item(x As I1) As Integer': Not most specific.
        c(i3) = 0
        ~
BC36716: Visual Basic 16.9 does not support overload resolution priority.
<OverloadResolutionPriority(1)>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>))

            comp2 = CreateCompilation(source, references:={compilationReference}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            Dim expected = If(i1First,
<expected>
BC30521: Overload resolution failed because no accessible 'Item' is most specific for these arguments:
    'Public Default Property Item(x As I1) As Integer': Not most specific.
    'Public Default Property Item(x As I2) As Integer': Not most specific.
        Dim x = c(i3)
                ~
BC30521: Overload resolution failed because no accessible 'Item' is most specific for these arguments:
    'Public Default Property Item(x As I1) As Integer': Not most specific.
    'Public Default Property Item(x As I2) As Integer': Not most specific.
        c(i3) = 0
        ~
</expected>,
<expected>
BC30521: Overload resolution failed because no accessible 'Item' is most specific for these arguments:
    'Public Default Property Item(x As I2) As Integer': Not most specific.
    'Public Default Property Item(x As I1) As Integer': Not most specific.
        Dim x = c(i3)
                ~
BC30521: Overload resolution failed because no accessible 'Item' is most specific for these arguments:
    'Public Default Property Item(x As I2) As Integer': Not most specific.
    'Public Default Property Item(x As I1) As Integer': Not most specific.
        c(i3) = 0
        ~
</expected>)

            comp2.AssertTheseDiagnostics(expected)

            comp3 = CreateCompilation(source, references:={metadataReference}, options:=TestOptions.DebugExe, parseOptions:=TestOptions.Regular16_9)
            comp3.AssertTheseDiagnostics(expected)
        End Sub

        <Fact>
        Public Sub NoImpactToFunctionType()

            Dim source = "
Option Infer On 

Imports System.Runtime.CompilerServices

Class C

    Shared Sub Main()
        Dim x = AddressOf M1
    End Sub

    <OverloadResolutionPriority(1)>
    Shared Sub M1(x As Integer)
    End Sub

    Shared Sub M1(x As String)
    End Sub
End Class
"

            Dim compilation = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30581: 'AddressOf' expression cannot be converted to 'Object' because 'Object' is not a delegate type.
        Dim x = AddressOf M1
                ~~~~~~~~~~~~
]]></expected>
            )
        End Sub

        <Fact>
        Public Sub DelegateConversion()

            Dim source1 = "
Imports System.Runtime.CompilerServices

Public Class C

    <OverloadResolutionPriority(1)>
    Shared Sub M(x As Object)
        System.Console.Write(1)
    End Sub

    Shared Sub M(x As String)
        throw DirectCast(Nothing, System.Exception)
    End Sub
End Class
"
            Dim source2 = "
Module Program
    Sub Main()
        Dim a As System.Action(Of string) = AddressOf C.M
        a(Nothing)
    End Sub
End Module
"

            Dim comp1 = CreateCompilation({source1, source2, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp1, expectedOutput:="1").VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source2, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="1").VerifyDiagnostics()

            Dim comp3 = CreateCompilation(source2, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Interface_DifferentPriorities_Methods()

            Dim source1 = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

Public Interface I
    <OverloadResolutionPriority(1)>
    Sub M(x As I1)
    Sub M(x As I2)
End Interface

Public Class C
    Implements I

    public Sub M(x As I1) Implements I.M
        System.Console.Write(1)
    End Sub

    <OverloadResolutionPriority(2)>
    public Sub M(x As I2) Implements I.M
        System.Console.Write(2)
    End Sub
End Class
"
            Dim source2 = "
Module Program
    Sub Main()
        Dim c = new C()
        Dim i3 As I3 = Nothing
        c.M(i3)
        DirectCast(c, I).M(i3)
    End Sub
End Module
"

            Dim comp1 = CreateCompilation({source1, source2, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp1, expectedOutput:="21").VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source2, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="21").VerifyDiagnostics()

            Dim comp3 = CreateCompilation(source2, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="21").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Interface_DifferentPriorities_Indexers()

            Dim source1 = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

Public Interface I
    <OverloadResolutionPriority(1)>
    Default Property Item(x As I1) As Integer
    Default Property Item(x As I2) As Integer
End Interface

Public Class C
    Implements I

    Default Property Item(x As I1) As Integer Implements I.Item
        Get
            System.Console.Write(1)
            return 0
        End Get
        Set
            System.Console.Write(2)
        End Set
    End Property
    <OverloadResolutionPriority(2)>
    Default Property Item(x As I2) As Integer Implements I.Item
        Get
            System.Console.Write(3)
            return 0
        End Get
        Set
            System.Console.Write(4)
        End Set
    End Property
End Class
"
            Dim source2 = "
Module Program
    Sub Main()
        Dim c = new C()
        Dim i3 As I3 = Nothing

        c(I3) = 1
        Dim x = c(i3)
        c.Item(I3) = 1
        x = c.Item(i3)
        
        Dim i As I = DirectCast(c, I)
        i(I3) = 1
        x = i(i3)
        i.Item(I3) = 1
        x = i.Item(i3)
    End Sub
End Module
"

            Dim comp1 = CreateCompilation({source1, source2, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp1, expectedOutput:="43432121").VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source2, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="43432121").VerifyDiagnostics()

            Dim comp3 = CreateCompilation(source2, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="43432121").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AppliedToIndexerGetterSetter_Source()

            Dim source = "
Imports System.Runtime.CompilerServices

Public Class C

    Default Property Item(x As Integer) As Integer
        <OverloadResolutionPriority(1)>
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        <OverloadResolutionPriority(2)>
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property
End Class
"

            Dim comp = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)

            Dim c = comp.GetTypeByMetadataName("C")
            Dim indexer = c.GetMember(Of PropertySymbol)("Item")

            Assert.Equal(0, indexer.OverloadResolutionPriority)
            Assert.Equal(0, indexer.GetMethod.OverloadResolutionPriority)
            Assert.Equal(0, indexer.SetMethod.OverloadResolutionPriority)

            comp.AssertTheseDiagnostics(
<expected><![CDATA[
BC37334: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
        <OverloadResolutionPriority(1)>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37334: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
        <OverloadResolutionPriority(2)>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub AppliedToIndexerGetterSetter_Metadata()

            ' Equivalent to:
            ' public class C
            ' {
            '     public int this[object x]
            '     {
            '         [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
            '         get => throw null;
            '         [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
            '         set => throw null;
            '     }
            '     public int this[string x]
            '     {
            '         get { System.Console.Write(1); return 1; }
            '         set => System.Console.Write(2);
            '     }
            ' }
            Dim il = "
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )
    // Methods
    .method public hidebysig specialname 
        instance int32 get_Item (
            object x
        ) cil managed 
    {
        .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
            01 00 01 00 00 00 00 00
        )
        ldnull
        throw
    } // end of method C::get_Item

    .method public hidebysig specialname 
        instance void set_Item (
            object x,
            int32 'value'
        ) cil managed 
    {
        .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
            01 00 01 00 00 00 00 00
        )
        ldnull
        throw
    } // end of method C::set_Item

    .method public hidebysig specialname 
        instance int32 get_Item (
            string x
        ) cil managed 
    {
        ldc.i4.1
        call void [mscorlib]System.Console::Write(int32)
        ldc.i4.1
        ret
    } // end of method C::get_Item

    .method public hidebysig specialname 
        instance void set_Item (
            string x,
            int32 'value'
        ) cil managed 
    {
        ldc.i4.2
        call void [mscorlib]System.Console::Write(int32)
        ret
    } // end of method C::set_Item

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    } // end of method C::.ctor

    // Properties
    .property instance int32 Item(
        object x
    )
    {
        .get instance int32 C::get_Item(object)
        .set instance void C::set_Item(object, int32)
    }
    .property instance int32 Item(
        string x
    )
    {
        .get instance int32 C::get_Item(string)
        .set instance void C::set_Item(string, int32)
    }
}
"

            Dim ilRef = CompileIL(il + OverloadResolutionPriorityAttributeILDefinition)

            Dim source = "
public class Program 
    Shared Sub Main
        Dim c = new C()
        Dim x = c(""test"")
        c(""test"") = 0
    End Sub
End Class
"
            Dim comp = CreateCompilation(source, references:={ilRef}, options:=TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:="12").VerifyDiagnostics()

            Dim c = comp.GetTypeByMetadataName("C")
            Dim indexers = c.GetMembers("Item").OfType(Of PropertySymbol)().ToArray()

            Assert.Equal(2, indexers.Length)

            Dim indexer = indexers(0)
            AssertEx.Equal("Property C.Item(x As System.Object) As System.Int32", indexer.ToTestDisplayString())
            Assert.Equal(0, indexer.OverloadResolutionPriority)
            Assert.Equal(0, indexer.GetMethod.OverloadResolutionPriority)
            Assert.Equal(0, indexer.SetMethod.OverloadResolutionPriority)

            indexer = indexers(1)
            AssertEx.Equal("Property C.Item(x As System.String) As System.Int32", indexer.ToTestDisplayString())
            Assert.Equal(0, indexer.OverloadResolutionPriority)
            Assert.Equal(0, indexer.GetMethod.OverloadResolutionPriority)
            Assert.Equal(0, indexer.SetMethod.OverloadResolutionPriority)
        End Sub

        <Fact>
        Public Sub AppliedToPropertyGetterSetter()

            Dim source = "
Imports System.Runtime.CompilerServices

Public Class C

    Property Prop As Integer
        <OverloadResolutionPriority(1)>
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        <OverloadResolutionPriority(2)>
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property
End Class
"

            Dim comp = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)

            Dim c = comp.GetTypeByMetadataName("C")
            Dim indexer = c.GetMember(Of PropertySymbol)("Prop")

            Assert.Equal(0, indexer.OverloadResolutionPriority)
            Assert.Equal(0, indexer.GetMethod.OverloadResolutionPriority)
            Assert.Equal(0, indexer.SetMethod.OverloadResolutionPriority)

            comp.AssertTheseDiagnostics(
<expected><![CDATA[
BC37334: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
        <OverloadResolutionPriority(1)>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37334: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
        <OverloadResolutionPriority(2)>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub AppliedToEventGetterSetter()

            Dim source = "
Imports System.Runtime.CompilerServices

Public Class C

    Custom Event Prop As System.Action
        <OverloadResolutionPriority(1)>
        AddHandler(x as System.Action)
        End AddHandler
        <OverloadResolutionPriority(2)>
        RemoveHandler(x as System.Action)
        End RemoveHandler
        <OverloadResolutionPriority(3)>
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
"

            Dim comp = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Dim c = comp.GetTypeByMetadataName("C")
            Dim indexer = c.GetMember(Of EventSymbol)("Prop")

            Assert.Equal(0, indexer.AddMethod.OverloadResolutionPriority)
            Assert.Equal(0, indexer.RemoveMethod.OverloadResolutionPriority)
            Assert.Equal(0, indexer.RaiseMethod.OverloadResolutionPriority)

            comp.AssertTheseDiagnostics(
<expected><![CDATA[
BC37334: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
        <OverloadResolutionPriority(1)>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37334: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
        <OverloadResolutionPriority(2)>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37334: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
        <OverloadResolutionPriority(3)>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub Dynamic()

            Dim source1 = "
Imports System.Runtime.CompilerServices

Public Class C

    <OverloadResolutionPriority(1)>
    Sub M(o As Long)
        throw DirectCast(Nothing, System.Exception)
    End Sub

    Sub M(s As String)
        System.Console.Write(2)
    End Sub

    <OverloadResolutionPriority(1)>
    Default Property Item(x As Long) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property

    Default Property Item(x As String) As Integer
        Get
            System.Console.Write(3)
            Return 0
        End Get
        Set
            System.Console.Write(4)
        End Set
    End Property
End Class
"

            Dim source2 = "
Option Strict Off

public class Program 
    Shared Sub Main
        Dim arg As Object = ""test""
        Dim c = new C()
        c.M(arg)
        Dim x = c(arg)
        c(arg) = 0
    End Sub
End Class
"

            Dim comp = CreateCompilation({source2, source1, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp, expectedOutput:="234").VerifyDiagnostics()

            Dim comp1 = CreateCompilation({source1, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)

            Dim comp2 = CreateCompilation(source2, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="234").VerifyDiagnostics()

            Dim comp3 = CreateCompilation(source2, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="234").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Destructor()

            Dim source = "
Imports System.Runtime.CompilerServices

Public Class C
    <OverloadResolutionPriority(1)>
    Protected Overrides Sub Finalize()
    End Sub
End Class
"

            Dim comp = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)

            comp.AssertTheseDiagnostics(
<expected><![CDATA[
BC37333: Cannot use 'OverloadResolutionPriorityAttribute' on an overriding member.
    <OverloadResolutionPriority(1)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub BinaryOperators_SameType()

            Dim source1 = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

Public Class C

    <OverloadResolutionPriority(1)>
    Shared Operator + (c As C, i As I1) As C
        System.Console.Write(1)
        return c
    End Operator

    Shared Operator + (c As C, i As I2) As C
        throw DirectCast(Nothing, System.Exception)
    End Operator
End Class
"

            Dim source2 = "
Option Strict Off

public class Program 
    Shared Sub Main
        Dim c = new C()
        Dim i3 As I3 = Nothing
        Dim x = c + i3
    End Sub
End Class
"

            Dim comp = CreateCompilation({source2, source1, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp, expectedOutput:="1").VerifyDiagnostics()

            Dim comp1 = CreateCompilation({source1, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)

            Dim comp2 = CreateCompilation(source2, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="1").VerifyDiagnostics()

            Dim comp3 = CreateCompilation(source2, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub BinaryOperators_DifferentType()

            Dim source1 = "
Imports System.Runtime.CompilerServices

public interface I1
End Interface
public interface I2
End Interface
public interface I3
    Inherits I1, I2
End Interface

Public Class C1
    <OverloadResolutionPriority(1)>
    Shared Operator + (c1 As C1, c2 As C2) As C1
        throw DirectCast(Nothing, System.Exception)
    End Operator
End Class

Public Class C2
    Shared Operator + (c1 As C1, c2 As C2) As C2
        throw DirectCast(Nothing, System.Exception)
    End Operator
End Class
"

            Dim source2 = "
Option Strict Off

public class Program 
    Shared Sub Main
        Dim c1 = new C1()
        Dim c2 = new C2()
        Dim x = c1 + c2
    End Sub
End Class
"

            Dim comp = CreateCompilation({source2, source1, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            Dim errs As XElement =
<expected>
BC30521: Overload resolution failed because no accessible '+' is most specific for these arguments:
    'Public Shared Operator C1.+(c1 As C1, c2 As C2) As C1': Not most specific.
    'Public Shared Operator C2.+(c1 As C1, c2 As C2) As C2': Not most specific.
        Dim x = c1 + c2
                ~~~~~~~
</expected>
            comp.AssertTheseDiagnostics(errs)

            Dim comp1 = CreateCompilation({source1, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)

            Dim comp2 = CreateCompilation(source2, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            comp2.AssertTheseDiagnostics(errs)

            Dim comp3 = CreateCompilation(source2, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            comp3.AssertTheseDiagnostics(errs)
        End Sub

        <Fact>
        Public Sub DisallowedOnStaticCtors()

            Dim source = "
Imports System.Runtime.CompilerServices

Public Class C
    <OverloadResolutionPriority(1)>
    Shared Sub New()
    End Sub
End Class
"

            Dim comp = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)

            comp.AssertTheseDiagnostics(
<expected><![CDATA[
BC37334: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
    <OverloadResolutionPriority(1)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Theory>
        <InlineData("<System.Runtime.CompilerServices.OverloadResolutionPriority(1)>", "")>
        <InlineData("", "<System.Runtime.CompilerServices.OverloadResolutionPriority(1)>")>
        Public Sub PartialMethod(definitionPriority As String, implementationPriority As String)

            Dim definition = "
Public Partial Class C

    " + definitionPriority + "
    Private Partial Sub M(x As Object)
    End Sub
End Class
"

            Dim implementation = "
Public Partial Class C

    " + implementationPriority + "
    Private Sub M(x As Object)
        System.Console.Write(1)
    End Sub

    Sub M(x As String)
        throw DirectCast(Nothing, System.Exception)
    End Sub
End Class
"

            Dim source2 = "
Partial Class C
    Shared Sub Main()
        Dim c = new C()
        c.M("""")
    End Sub
End Class
"

            Dim comp1 = CreateCompilation({source2, definition, implementation, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp1, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeAppliedTwiceMethod_Source()

            Dim source = "
Imports System.Runtime.CompilerServices

Class C
    <OverloadResolutionPriority(1)>
    <OverloadResolutionPriority(2)>
    Shared Sub M(x As Object)
    End Sub

    Shared Sub M(x As String)
    End Sub
End Class
"

            Dim compilation = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30663: Attribute 'OverloadResolutionPriorityAttribute' cannot be applied multiple times.
    <OverloadResolutionPriority(2)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>
            )

            Dim m = compilation.GetTypeByMetadataName("C").GetMembers("M").OfType(Of MethodSymbol)().First()
            Assert.Equal("Sub C.M(x As System.Object)", m.ToTestDisplayString())
            Assert.Equal(2, m.OverloadResolutionPriority)
        End Sub

        <Fact>
        Public Sub AttributeAppliedTwiceMethod_Metadata()

            ' Equivalent to:
            ' public class C
            ' {
            '     [OverloadResolutionPriority(1)]
            '     [OverloadResolutionPriority(2)]
            '     public void M(object o) => System.Console.Write(1);
            '     public void M(string s) => System.Console.Write(2);
            ' }
            Dim il = "
.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{
    .method public hidebysig 
        instance void M (
            object o
        ) cil managed 
    {
        .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
            01 00 01 00 00 00 00 00
        )
        .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
            01 00 02 00 00 00 00 00
        )
        ldc.i4.1
        call void [mscorlib]System.Console::Write(int32)
        ret
    } // end of method C::M

    .method public hidebysig 
        instance void M (
            string s
        ) cil managed 
    {
        ldc.i4.2
        call void [mscorlib]System.Console::Write(int32)
        ret
    } // end of method C::M

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    } // end of method C::.ctor
} // end of class C
"

            Dim ilRef = CompileIL(il + OverloadResolutionPriorityAttributeILDefinition)

            Dim source = "
public class Program 
    Shared Sub Main
        Dim c = new C()
        c.M(""test"")
    End Sub
End Class
"
            Dim comp = CreateCompilation(source, references:={ilRef}, options:=TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:="1").VerifyDiagnostics()

            Dim m = comp.GetTypeByMetadataName("C").GetMembers("M").OfType(Of MethodSymbol)().First()
            Assert.Equal("Sub C.M(o As System.Object)", m.ToTestDisplayString())
            Assert.Equal(2, m.OverloadResolutionPriority)
        End Sub

        <Fact>
        Public Sub AttributeAppliedTwiceConstructor_Source()

            Dim source = "
Imports System.Runtime.CompilerServices

Class C
    <OverloadResolutionPriority(1)>
    <OverloadResolutionPriority(2)>
    Sub New(x As Object)
    End Sub

    Sub New(x As String)
    End Sub
End Class
"

            Dim compilation = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30663: Attribute 'OverloadResolutionPriorityAttribute' cannot be applied multiple times.
    <OverloadResolutionPriority(2)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>
            )

            Dim m = compilation.GetTypeByMetadataName("C").GetMembers(".ctor").OfType(Of MethodSymbol)().First()
            Assert.Equal("Sub C..ctor(x As System.Object)", m.ToTestDisplayString())
            Assert.Equal(2, m.OverloadResolutionPriority)
        End Sub

        <Fact>
        Public Sub AttributeAppliedTwiceConstructor_Metadata()

            ' Equivalent to:
            ' public class C
            ' {
            '     [OverloadResolutionPriority(1)]
            '     [OverloadResolutionPriority(2)]
            '     public C(object o) {}
            '     public C(string s) {}
            ' }
            Dim il = "
.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            object o
        ) cil managed 
    {
        .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
            01 00 01 00 00 00 00 00
        )
        .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
            01 00 02 00 00 00 00 00
        )
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ldc.i4.1
        call void [mscorlib]System.Console::Write(int32)
        ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            string s
        ) cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ldc.i4.2
        call void [mscorlib]System.Console::Write(int32)
        ret
    } // end of method C::.ctor
} // end of class C
"

            Dim ilRef = CompileIL(il + OverloadResolutionPriorityAttributeILDefinition)

            Dim source = "
public class Program 
    Shared Sub Main
        Dim c = new C(""test"")
    End Sub
End Class
"
            Dim comp = CreateCompilation(source, references:={ilRef}, options:=TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:="1").VerifyDiagnostics()

            Dim m = comp.GetTypeByMetadataName("C").GetMembers(".ctor").OfType(Of MethodSymbol)().First()
            Assert.Equal("Sub C..ctor(o As System.Object)", m.ToTestDisplayString())
            Assert.Equal(2, m.OverloadResolutionPriority)
        End Sub

        <Fact>
        Public Sub AttributeAppliedTwiceIndexer_Source()

            Dim source = "
Imports System.Runtime.CompilerServices

Class C
    <OverloadResolutionPriority(1)>
    <OverloadResolutionPriority(2)>
    Default Property Item(x As Object) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property

    Default Property Item(x As String) As Integer
        Get
            throw DirectCast(Nothing, System.Exception)
        End Get
        Set
            throw DirectCast(Nothing, System.Exception)
        End Set
    End Property
End Class
"

            Dim compilation = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugDll)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30663: Attribute 'OverloadResolutionPriorityAttribute' cannot be applied multiple times.
    <OverloadResolutionPriority(2)>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>
            )

            Dim m = compilation.GetTypeByMetadataName("C").GetMembers("Item").OfType(Of PropertySymbol)().First()
            AssertEx.Equal("Property C.Item(x As System.Object) As System.Int32", m.ToTestDisplayString())
            Assert.Equal(2, m.OverloadResolutionPriority)
        End Sub

        <Fact>
        Public Sub AttributeAppliedTwiceIndexer_Metadata()

            ' Equivalent to:
            ' public class C
            ' {
            '     [OverloadResolutionPriority(1)]
            '     [OverloadResolutionPriority(2)]
            '     public int this[object o]
            '     {
            '         get => throw null;
            '         set => throw null;
            '     }
            '     public int this[string o]
            '     {
            '         get => throw null;
            '         set => throw null;
            '     }
            ' }
            Dim il = "
.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )
    // Methods
    .method public hidebysig specialname 
        instance int32 get_Item (
            object o
        ) cil managed 
    {
        ldc.i4.1
        call void [mscorlib]System.Console::Write(int32)
        ldc.i4.1
        ret
    } // end of method C::get_Item

    .method public hidebysig specialname 
        instance void set_Item (
            object o,
            int32 'value'
        ) cil managed 
    {
        ldc.i4.2
        call void [mscorlib]System.Console::Write(int32)
        ret
    } // end of method C::set_Item

    .method public hidebysig specialname 
        instance int32 get_Item (
            string o
        ) cil managed 
    {
        ldnull
        throw
    } // end of method C::get_Item

    .method public hidebysig specialname 
        instance void set_Item (
            string o,
            int32 'value'
        ) cil managed 
    {
        ldnull
        throw
    } // end of method C::set_Item

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    } // end of method C::.ctor

    // Properties
    .property instance int32 Item(
        object o
    )
    {
        .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
            01 00 01 00 00 00 00 00
        )
        .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
            01 00 02 00 00 00 00 00
        )
        .get instance int32 C::get_Item(object)
        .set instance void C::set_Item(object, int32)
    }
    .property instance int32 Item(
        string o
    )
    {
        .get instance int32 C::get_Item(string)
        .set instance void C::set_Item(string, int32)
    }

} // end of class C
"

            Dim ilRef = CompileIL(il + OverloadResolutionPriorityAttributeILDefinition)

            Dim source = "
public class Program 
    Shared Sub Main
        Dim c = new C()
        Dim x = c(""test"")
        c(""test"") = 0
    End Sub
End Class
"
            Dim comp = CreateCompilation(source, references:={ilRef}, options:=TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:="12").VerifyDiagnostics()

            Dim m = comp.GetTypeByMetadataName("C").GetMembers("Item").OfType(Of PropertySymbol)().First()
            AssertEx.Equal("Property C.Item(o As System.Object) As System.Int32", m.ToTestDisplayString())
            Assert.Equal(2, m.OverloadResolutionPriority)
        End Sub

        <Fact>
        Public Sub HonoredInsideExpressionTree()

            Dim source = "
Imports System.Runtime.CompilerServices

Class C
    <OverloadResolutionPriority(1)>
    Shared Sub M(x As Object)
        System.Console.Write(1)
    End Sub

    Shared Sub M(x As String)
        throw DirectCast(Nothing, System.Exception)
    End Sub
End Class

public class Program 
    Shared Sub Main
        Dim e As System.Linq.Expressions.Expression(Of System.Action) = Sub() C.M(""test"")
        e.Compile()()
    End Sub
End Class
"

            Dim comp = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub QuerySyntax()

            Dim source = "
Imports System
Imports System.Runtime.CompilerServices

Class C
    <OverloadResolutionPriority(1)>
    Function [Select](Of T)(selector As Func(Of Integer, T)) As C
        System.Console.Write(1)
        Return Me
    End Function

    Function [Select](selector As Func(Of Integer, Integer)) As C
        throw DirectCast(Nothing, System.Exception)
    End Function
End Class

public class Program 
    Shared Sub Main
        Dim c As New C()
        Dim y = From x in c Select x
    End Sub
End Class
"

            Dim comp = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp, expectedOutput:="1").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub ObjectInitializers()

            Dim source = "
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

class C 
    Implements IEnumerable(Of Integer)

    private _list As New List(Of Integer)

    Sub Add(x As Integer)
        throw DirectCast(Nothing, System.Exception)
    End Sub

    <OverloadResolutionPriority(1)>
    Sub Add(x As Integer, Optional y As Integer = 0)
        _list.Add(x)
    End Sub

    Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Return _list.GetEnumerator()
    End Function

    Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class

class Program
    Shared Sub Main()
        Dim c As new C() from { 2 }
        for each i in c
            Console.Write(i)
        Next
    End Sub
End Class
"

            Dim comp = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp, expectedOutput:="2").VerifyDiagnostics()
        End Sub

        <Theory>
        <InlineData(New Integer() {1, 2, 3})>
        <InlineData(New Integer() {1, 3, 2})>
        <InlineData(New Integer() {2, 1, 3})>
        <InlineData(New Integer() {2, 3, 1})>
        <InlineData(New Integer() {3, 1, 2})>
        <InlineData(New Integer() {3, 2, 1})>
        Public Sub ExtensionsOnlyFilteredByApplicability_01(methodOrder As Integer())

            Dim e2Methods = ""

            For Each method In methodOrder
                Select Case method
                    Case 1
                        e2Methods += "
<OverloadResolutionPriority(-1)>
<Extension>
Sub R(x As Integer)
    Console.WriteLine(""E2.R(int)"")
End Sub
"
                    Case 2
                        e2Methods += "
<Extension>
Sub R(x As String)
    Console.WriteLine(""E2.R(string)"")
End Sub
"
                    Case 3
                        e2Methods += "
<Extension>
Sub R(x As Boolean)
    Console.WriteLine(""E2.R(bool)"")
End Sub
"
                    Case Else
                        Throw ExceptionUtilities.Unreachable()
                End Select
            Next

            Dim source = $"
Imports System
Imports System.Runtime.CompilerServices

class Program
    Shared Sub Main()
        Dim x As Integer = 5
        x.R() ' E1.R(int)
    End Sub
End Class

Module E1
    <Extension>
    Sub R(x As Integer)
        Console.WriteLine(""E1.R(int)"")
    End Sub
End Module

Module E2
    {e2Methods}
End Module
"

            Dim comp = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            comp.AssertTheseDiagnostics(
<expected>
BC30521: Overload resolution failed because no accessible 'R' is most specific for these arguments:
    Extension method 'Public Sub R()' defined in 'E1': Not most specific.
    Extension method 'Public Sub R()' defined in 'E2': Not most specific.
        x.R() ' E1.R(int)
          ~
</expected>)
        End Sub

        <Theory>
        <InlineData(New Integer() {1, 2, 3})>
        <InlineData(New Integer() {1, 3, 2})>
        <InlineData(New Integer() {2, 1, 3})>
        <InlineData(New Integer() {2, 3, 1})>
        <InlineData(New Integer() {3, 1, 2})>
        <InlineData(New Integer() {3, 2, 1})>
        Public Sub ExtensionsOnlyFilteredByApplicability_02(methodOrder As Integer())

            Dim e2Methods = ""

            For Each method In methodOrder
                Select Case method
                    Case 1
                        e2Methods += "
<OverloadResolutionPriority(-1)>
<Extension>
Sub R(x As Integer)
    Console.WriteLine(""E2.R(int)"")
End Sub
"
                    Case 2
                        e2Methods += "
<Extension>
Sub R(x As String)
    Console.WriteLine(""E2.R(string)"")
End Sub
"
                    Case 3
                        e2Methods += "
<OverloadResolutionPriority(-1)>
<Extension>
Sub R(x As Boolean)
    Console.WriteLine(""E2.R(bool)"")
End Sub
"
                    Case Else
                        Throw ExceptionUtilities.Unreachable()
                End Select
            Next

            Dim source = $"
Imports System
Imports System.Runtime.CompilerServices

class Program
    Shared Sub Main()
        Dim x As Integer = 5
        x.R() ' E1.R(int)
    End Sub
End Class

Module E1
    <Extension>
    Sub R(x As Integer)
        Console.WriteLine(""E1.R(int)"")
    End Sub
End Module

Module E2
    {e2Methods}
End Module
"

            Dim comp = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            comp.AssertTheseDiagnostics(
<expected>
BC30521: Overload resolution failed because no accessible 'R' is most specific for these arguments:
    Extension method 'Public Sub R()' defined in 'E1': Not most specific.
    Extension method 'Public Sub R()' defined in 'E2': Not most specific.
        x.R() ' E1.R(int)
          ~
</expected>)
        End Sub

        <Theory>
        <InlineData(New Integer() {1, 2, 3})>
        <InlineData(New Integer() {1, 3, 2})>
        <InlineData(New Integer() {2, 1, 3})>
        <InlineData(New Integer() {2, 3, 1})>
        <InlineData(New Integer() {3, 1, 2})>
        <InlineData(New Integer() {3, 2, 1})>
        Public Sub ExtensionsOnlyFilteredByApplicability_03(methodOrder As Integer())

            Dim e2Methods = ""

            For Each method In methodOrder
                Select Case method
                    Case 1
                        e2Methods += "
<OverloadResolutionPriority(-1)>
<Extension>
Sub R(x As Integer)
    Console.WriteLine(""E2.R(int)"")
End Sub
"
                    Case 2
                        e2Methods += "
<Extension>
Sub R(x As String)
    Console.WriteLine(""E2.R(string)"")
End Sub
"
                    Case 3
                        e2Methods += "
<Extension>
Sub R(x As Object)
    Console.WriteLine(""E2.R(object)"")
End Sub
"
                    Case Else
                        Throw ExceptionUtilities.Unreachable()
                End Select
            Next

            Dim source = $"
Imports System
Imports System.Runtime.CompilerServices

class Program
    Shared Sub Main()
        Dim x As Integer = 5
        x.R() ' E1.R(int)
    End Sub
End Class

Module E1
    <Extension>
    Sub R(x As Integer)
        Console.WriteLine(""E1.R(int)"")
    End Sub
End Module

Module E2
    {e2Methods}
End Module
"

            Dim comp = CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp, expectedOutput:="E1.R(int)").VerifyDiagnostics()
        End Sub

    End Class
End Namespace
