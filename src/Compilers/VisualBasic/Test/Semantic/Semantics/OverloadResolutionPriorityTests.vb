' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

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

            Dim comp2 = CreateCompilation(source, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="1").VerifyDiagnostics()

            Dim comp3 = CreateCompilation(source, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp3, expectedOutput:="1").VerifyDiagnostics()
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
Imports System
Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
        Dim t As new Integer?(1)
        t.M1()
        t.M2()
    End Sub
End Module
    
Module Test1
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
            Dim compilation = CompilationUtils.CreateCompilation({source, OverloadResolutionPriorityAttributeDefinitionVB}, options:=TestOptions.DebugExe)

            CompileAndVerify(compilation, expectedOutput:="23")
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

    End Class
End Namespace
