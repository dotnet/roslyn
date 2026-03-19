' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class CompoundAssignment
        Inherits BasicTestBase

        <Fact()>
        Public Sub CompoundAssignTest1()

            Dim compilationDef =
<compilation name="CompoundAssignTest1">
    <file name="a.vb">
Module Program
    Sub Main()
        Test1(1)     

        Dim x As Integer = 2
        Test2(x)
        System.Console.WriteLine(x)      

        Test3(New TestC())

        Test4(New TestC())
    End Sub

    Sub Test1(x as Integer)
        x+=1
        System.Console.WriteLine(x)      
    End Sub

    Sub Test2(ByRef x as Integer)
        x+=2
    End Sub

    Sub Test3(x as TestC)
        x.P1+=3
        System.Console.WriteLine(x.P1)      
    End Sub

    Sub Test4(ByRef x As TestC)
        x.P1 += 4
        System.Console.WriteLine(x.P1)
    End Sub

End Module

Class TestC
    Public Property P1 As Integer = 3
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="2" & Environment.NewLine & "4" & Environment.NewLine & "6" & Environment.NewLine & "7")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Test1",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Program.Test2",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i4
  IL_0003:  ldc.i4.2
  IL_0004:  add.ovf
  IL_0005:  stind.i4
  IL_0006:  ret
}
]]>)

            verifier.VerifyIL("Program.Test3",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (TestC V_0)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  callvirt   "Function TestC.get_P1() As Integer"
  IL_0009:  ldc.i4.3
  IL_000a:  add.ovf
  IL_000b:  callvirt   "Sub TestC.set_P1(Integer)"
  IL_0010:  ldarg.0
  IL_0011:  callvirt   "Function TestC.get_P1() As Integer"
  IL_0016:  call       "Sub System.Console.WriteLine(Integer)"
  IL_001b:  ret
}
]]>)

            verifier.VerifyIL("Program.Test4",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  3
  .locals init (TestC V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  callvirt   "Function TestC.get_P1() As Integer"
  IL_000a:  ldc.i4.4
  IL_000b:  add.ovf
  IL_000c:  callvirt   "Sub TestC.set_P1(Integer)"
  IL_0011:  ldarg.0
  IL_0012:  ldind.ref
  IL_0013:  callvirt   "Function TestC.get_P1() As Integer"
  IL_0018:  call       "Sub System.Console.WriteLine(Integer)"
  IL_001d:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub CompoundAssignTest2Error()

            Dim compilationDef =
<compilation name="CompoundAssignTest2">
    <file name="a.vb">
Module Program
    Sub Main()
        Dim x as String
        x+="1"
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        x+="1"
        ~
</expected>)
        End Sub

        <Fact()>
        Public Sub CompoundAssignTest3Error()

            Dim compilationDef =
<compilation name="CompoundAssignTest3">
    <file name="a.vb">
Module Program

    Sub Main()
        Dim x As String = "0"
        x += 1

        Dim y As System.Guid = Nothing
        y += 1
        y -= 1
        y *= 1
        y /= 1
        y \= 1
        y ^= 1
        y &lt;&lt;= 1
        y &gt;&gt;= 1
        y &amp;= 1

        Dim o As Object = Nothing
        x &amp;= o
        o &amp;= x

        System += 1

        P1 += 1
    End Sub

    ReadOnly Property P1 As Integer
        Get
            Return 0
        End Get
    End Property

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42016: Implicit conversion from 'String' to 'Double'.
        x += 1
        ~
BC42016: Implicit conversion from 'Double' to 'String'.
        x += 1
        ~~~~~~
BC30452: Operator '+' is not defined for types 'Guid' and 'Integer'.
        y += 1
        ~~~~~~
BC30452: Operator '-' is not defined for types 'Guid' and 'Integer'.
        y -= 1
        ~~~~~~
BC30452: Operator '*' is not defined for types 'Guid' and 'Integer'.
        y *= 1
        ~~~~~~
BC30452: Operator '/' is not defined for types 'Guid' and 'Integer'.
        y /= 1
        ~~~~~~
BC30452: Operator '\' is not defined for types 'Guid' and 'Integer'.
        y \= 1
        ~~~~~~
BC30452: Operator '^' is not defined for types 'Guid' and 'Integer'.
        y ^= 1
        ~~~~~~
BC30452: Operator '<<' is not defined for types 'Guid' and 'Integer'.
        y <<= 1
        ~~~~~~~
BC30452: Operator '>>' is not defined for types 'Guid' and 'Integer'.
        y >>= 1
        ~~~~~~~
BC30452: Operator '&' is not defined for types 'Guid' and 'Integer'.
        y &= 1
        ~~~~~~
BC42016: Implicit conversion from 'Object' to 'String'.
        x &= o
        ~~~~~~
BC42019: Operands of type Object used for operator '&'; runtime errors could occur.
        x &= o
             ~
BC42019: Operands of type Object used for operator '&'; runtime errors could occur.
        o &= x
        ~
BC30112: 'System' is a namespace and cannot be used as an expression.
        System += 1
        ~~~~~~
BC30526: Property 'P1' is 'ReadOnly'.
        P1 += 1
        ~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub CompoundAssignTest4()

            Dim compilationDef =
<compilation name="CompoundAssignTest4">
    <file name="a.vb">
Module Program
    Sub Main()
        Dim x As String = "0"
        Dim y As Integer = 1
        x +=  'BIND1:"x"
             y 'BIND2:"y"
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            If True Then
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Double", typeInfo.ConvertedType.ToTestDisplayString())
                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("NarrowingString", conv.Kind.ToString())
                Assert.True(conv.Exists)

                Dim symbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node1)
                Assert.Equal("x As System.String", symbolInfo.Symbol.ToTestDisplayString())
            End If
            If True Then
                Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node2)

                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Double", typeInfo.ConvertedType.ToTestDisplayString())
                Dim conv = semanticModel.GetConversion(node2)
                Assert.Equal("WideningNumeric", conv.Kind.ToString())
                Assert.True(conv.Exists)

                Dim symbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node2)
                Assert.Equal("y As System.Int32", symbolInfo.Symbol.ToTestDisplayString())
            End If
        End Sub

        <Fact()>
        Public Sub CompoundAssignTest5()

            Dim compilationDef =
<compilation name="CompoundAssignTest5">
    <file name="a.vb"><![CDATA[ 
Module Program

    Function GetIndex(ByRef i As Integer) As Integer
        Dim result As Integer = i
        i += 1
        System.Console.WriteLine("GetIndex")
        Return result
    End Function

    Sub Print(x() As Integer, i As Integer)
        System.Console.Write("{0}: ", i)

        For Each number In x
            System.Console.Write("{0} ", number)
        Next

        System.Console.WriteLine("|")
    End Sub

    Sub Main()
        Dim i As Integer = 0
        Dim x() As Integer = New Integer() {-2, -3, -4, -5, -6, -7, -8, -9, -10}

        Print(x, i)
        x(GetIndex(i)) += 1
        Print(x, i)
        x(GetIndex(i)) -= 2
        Print(x, i)
        x(GetIndex(i)) /= 3
        Print(x, i)
        x(GetIndex(i)) \= 4
        Print(x, i)
        x(GetIndex(i)) *= 5
        Print(x, i)
        x(GetIndex(i)) ^= 6
        Print(x, i)
        x(GetIndex(i)) <<= 7
        Print(x, i)
        x(GetIndex(i)) >>= 8
        Print(x, i)
        x(GetIndex(i)) &= 9
        Print(x, i)
    End Sub

    Sub ILTest()
        Dim i As Integer = 0
        Dim x() As Integer = Nothing

        x(GetIndex(i)) += 1
    End Sub

    Sub ILTest2(x(,,) As Integer)
        x(GetIndex(), GetIndex(), GetIndex()) += 1
    End Sub

    Function GetIndex() As Integer
        Return 0
    End Function
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
0: -2 -3 -4 -5 -6 -7 -8 -9 -10 |
GetIndex
1: -1 -3 -4 -5 -6 -7 -8 -9 -10 |
GetIndex
2: -1 -5 -4 -5 -6 -7 -8 -9 -10 |
GetIndex
3: -1 -5 -1 -5 -6 -7 -8 -9 -10 |
GetIndex
4: -1 -5 -1 -1 -6 -7 -8 -9 -10 |
GetIndex
5: -1 -5 -1 -1 -30 -7 -8 -9 -10 |
GetIndex
6: -1 -5 -1 -1 -30 117649 -8 -9 -10 |
GetIndex
7: -1 -5 -1 -1 -30 117649 -1024 -9 -10 |
GetIndex
8: -1 -5 -1 -1 -30 117649 -1024 -1 -10 |
GetIndex
9: -1 -5 -1 -1 -30 117649 -1024 -1 -109 |
]]>)

            verifier.VerifyIL("Program.ILTest",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  3
  .locals init (Integer V_0, //i
  Integer& V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       "Function Program.GetIndex(ByRef Integer) As Integer"
  IL_000a:  ldelema    "Integer"
  IL_000f:  dup
  IL_0010:  stloc.1
  IL_0011:  ldloc.1
  IL_0012:  ldind.i4
  IL_0013:  ldc.i4.1
  IL_0014:  add.ovf
  IL_0015:  stind.i4
  IL_0016:  ret
}
]]>)

            verifier.VerifyIL("Program.ILTest2",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  4
  .locals init (Integer& V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Program.GetIndex() As Integer"
  IL_0006:  call       "Function Program.GetIndex() As Integer"
  IL_000b:  call       "Function Program.GetIndex() As Integer"
  IL_0010:  call       "Integer(*,*,*).Address"
  IL_0015:  dup
  IL_0016:  stloc.0
  IL_0017:  ldloc.0
  IL_0018:  ldind.i4
  IL_0019:  ldc.i4.1
  IL_001a:  add.ovf
  IL_001b:  stind.i4
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CompoundAssignTest6()

            Dim compilationDef =
<compilation name="CompoundAssignTest6">
    <file name="a.vb"><![CDATA[ 
Imports System

Module Program
    Sub Main()
        Dim obj As New C(Of String)()
        obj(obj.RP) += obj.RP & obj.RP
        Console.Write(obj.V)
    End Sub
End Module

Class C(Of T)
    Default Property AP(s As T) As String
        Get
            Return "D" & s.ToString()
        End Get
        Set(value As String)
            WP = value.ToString()
        End Set
    End Property

    ReadOnly Property RP As String
        Get
            Return "R"
        End Get
    End Property

    Public V As String
    WriteOnly Property WP As String
        Set(ByVal value As String)
            V &= value
        End Set
    End Property
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:="DRRR")

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  5
  .locals init (C(Of String) V_0, //obj
  C(Of String) V_1,
  String V_2)
  IL_0000:  newobj     "Sub C(Of String)..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  dup
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  callvirt   "Function C(Of String).get_RP() As String"
  IL_000f:  dup
  IL_0010:  stloc.2
  IL_0011:  ldloc.1
  IL_0012:  ldloc.2
  IL_0013:  callvirt   "Function C(Of String).get_AP(String) As String"
  IL_0018:  ldloc.0
  IL_0019:  callvirt   "Function C(Of String).get_RP() As String"
  IL_001e:  ldloc.0
  IL_001f:  callvirt   "Function C(Of String).get_RP() As String"
  IL_0024:  call       "Function String.Concat(String, String, String) As String"
  IL_0029:  callvirt   "Sub C(Of String).set_AP(String, String)"
  IL_002e:  ldloc.0
  IL_002f:  ldfld      "C(Of String).V As String"
  IL_0034:  call       "Sub System.Console.Write(String)"
  IL_0039:  ret
}

]]>)
        End Sub

        <WorkItem(543613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543613")>
        <Fact()>
        Public Sub IntegerObjectConcatAssignWithNothing()

            Dim compilationDef =
<compilation name="IntegerObjectConcatAssignWithNothing">
    <file name="a.vb"><![CDATA[ 
Module Program
    Sub Main()
        Dim x As Object
        x = 123
        x = x & Nothing
        System.Console.WriteLine(x)
        x &= Nothing
        System.Console.WriteLine(x)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="123" & Environment.NewLine & "123")

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  2
  IL_0000:  ldc.i4.s   123
  IL_0002:  box        "Integer"
  IL_0007:  ldnull
  IL_0008:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.ConcatenateObject(Object, Object) As Object"
  IL_000d:  dup
  IL_000e:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0013:  call       "Sub System.Console.WriteLine(Object)"
  IL_0018:  ldnull
  IL_0019:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.ConcatenateObject(Object, Object) As Object"
  IL_001e:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0023:  call       "Sub System.Console.WriteLine(Object)"
  IL_0028:  ret
}
]]>)

        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub MidAssignment1()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        

Module Module1

    Sub Main()
        System.Console.WriteLine(Test1("1234567890"))
        System.Console.WriteLine(Test2("1234567890"))
        System.Console.WriteLine(Test3("1234567890"))

        P1 = "1234567890"
        Test4()
        System.Console.WriteLine(P1)

        P2 = "1234567890"
        Test5()
        System.Console.WriteLine(P2)

        System.Console.WriteLine(Test6("1234567890"))
        System.Console.WriteLine(Test7("1234567890"))
        System.Console.WriteLine(Test8("1234567890"))

        Dim test As New Test
        test.P3 = "1234567890"
        Test9(test)
        System.Console.WriteLine(test.P3)

        Dim x = <x y="abc"/>
        Mid$(x.@y, 1, 2) = "d"
        System.Console.WriteLine(x)
    End Sub

    Function Test1(target As String) As String
        Mid$(target, 3) = "a"
        Return target
    End Function

    Function Test2(target As String) As String
        Mid(target, 5, 3) = "bcdef"
        Return target
    End Function

    Function Test3(target As Integer) As Integer
        Mid(target, 5) = "00"
        Return target
    End Function

    Sub Test4()
        Mid(P1, 5, 3) = "gh"
    End Sub

    Sub Test5()
        Mid(P2, 5, 3) = "999"
    End Sub

    Function Test6(target As Object) As Object
        Mid(target, 5) = "--"
        Return target
    End Function

    Function Test7(target As IntPtr) As IntPtr
        Mid(target, 5) = "88"
        Return target
    End Function

    Function Test8(target As IntPtr) As IntPtr
        Mid(target, "5", "2") = 789
        Return target
    End Function

    Sub Test9(target As Object)
        Mid(target.P3, 2) = "111"
    End Sub

    Property P1 As String
    Property P2 As Integer

End Module

Class Test
    Property P3 As Integer
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemCoreRef, SystemXmlRef, SystemXmlLinqRef}, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
12a4567890
1234bcd890
1234007890
1234gh7890
1234999890
1234--7890
1234887890
1234787890
1111567890
<x y="dbc" />
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  4
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  ldc.i4     0x7fffffff
  IL_0008:  ldstr      "a"
  IL_000d:  call       "Sub Microsoft.VisualBasic.CompilerServices.StringType.MidStmtStr(ByRef String, Integer, Integer, String)"
  IL_0012:  ldarg.0
  IL_0013:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  4
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.5
  IL_0003:  ldc.i4.3
  IL_0004:  ldstr      "bcdef"
  IL_0009:  call       "Sub Microsoft.VisualBasic.CompilerServices.StringType.MidStmtStr(ByRef String, Integer, Integer, String)"
  IL_000e:  ldarg.0
  IL_000f:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  4
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.5
  IL_000a:  ldc.i4     0x7fffffff
  IL_000f:  ldstr      "00"
  IL_0014:  call       "Sub Microsoft.VisualBasic.CompilerServices.StringType.MidStmtStr(ByRef String, Integer, Integer, String)"
  IL_0019:  ldloc.0
  IL_001a:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(String) As Integer"
  IL_001f:  starg.s    V_0
  IL_0021:  ldarg.0
  IL_0022:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  4
  .locals init (String V_0)
  IL_0000:  call       "Function Module1.get_P1() As String"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldc.i4.5
  IL_0009:  ldc.i4.3
  IL_000a:  ldstr      "gh"
  IL_000f:  call       "Sub Microsoft.VisualBasic.CompilerServices.StringType.MidStmtStr(ByRef String, Integer, Integer, String)"
  IL_0014:  ldloc.0
  IL_0015:  call       "Sub Module1.set_P1(String)"
  IL_001a:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test5",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  4
  .locals init (String V_0)
  IL_0000:  call       "Function Module1.get_P2() As Integer"
  IL_0005:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String"
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_0
  IL_000d:  ldc.i4.5
  IL_000e:  ldc.i4.3
  IL_000f:  ldstr      "999"
  IL_0014:  call       "Sub Microsoft.VisualBasic.CompilerServices.StringType.MidStmtStr(ByRef String, Integer, Integer, String)"
  IL_0019:  ldloc.0
  IL_001a:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(String) As Integer"
  IL_001f:  call       "Sub Module1.set_P2(Integer)"
  IL_0024:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test6",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  4
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Object) As String"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.5
  IL_000a:  ldc.i4     0x7fffffff
  IL_000f:  ldstr      "--"
  IL_0014:  call       "Sub Microsoft.VisualBasic.CompilerServices.StringType.MidStmtStr(ByRef String, Integer, Integer, String)"
  IL_0019:  ldloc.0
  IL_001a:  starg.s    V_0
  IL_001c:  ldarg.0
  IL_001d:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test7",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.IntPtr.op_Explicit(System.IntPtr) As Integer"
  IL_0006:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String"
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.5
  IL_000f:  ldc.i4     0x7fffffff
  IL_0014:  ldstr      "88"
  IL_0019:  call       "Sub Microsoft.VisualBasic.CompilerServices.StringType.MidStmtStr(ByRef String, Integer, Integer, String)"
  IL_001e:  ldloc.0
  IL_001f:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToLong(String) As Long"
  IL_0024:  call       "Function System.IntPtr.op_Explicit(Long) As System.IntPtr"
  IL_0029:  starg.s    V_0
  IL_002b:  ldarg.0
  IL_002c:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test9",
            <![CDATA[
{
  // Code size       72 (0x48)
  .maxstack  13
  .locals init (Object V_0,
  String V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldnull
  IL_0004:  ldstr      "P3"
  IL_0009:  ldc.i4.1
  IL_000a:  newarr     "Object"
  IL_000f:  dup
  IL_0010:  ldc.i4.0
  IL_0011:  ldloc.0
  IL_0012:  ldnull
  IL_0013:  ldstr      "P3"
  IL_0018:  ldc.i4.0
  IL_0019:  newarr     "Object"
  IL_001e:  ldnull
  IL_001f:  ldnull
  IL_0020:  ldnull
  IL_0021:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_0026:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Object) As String"
  IL_002b:  stloc.1
  IL_002c:  ldloca.s   V_1
  IL_002e:  ldc.i4.2
  IL_002f:  ldc.i4     0x7fffffff
  IL_0034:  ldstr      "111"
  IL_0039:  call       "Sub Microsoft.VisualBasic.CompilerServices.StringType.MidStmtStr(ByRef String, Integer, Integer, String)"
  IL_003e:  ldloc.1
  IL_003f:  stelem.ref
  IL_0040:  ldnull
  IL_0041:  ldnull
  IL_0042:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSet(Object, System.Type, String, Object(), String(), System.Type())"
  IL_0047:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MidAssignment2()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        

Module Module1

    Sub Main()
        Dim x As String = "ddd"
        Mid#(x, 2) = "a"
    End Sub

    Sub Test1()
        Mid(Main(), 3) = "a"
    End Sub

    Sub Test2()
        Mid(4.ToString(), 5, 3) = "bcdef"
    End Sub

    Sub Test3()
        Dim target As String
        Mid(target, 5) = "00"
    End Sub

    Sub Test4()
        Mid(P1, 5, 3) = "gh"
    End Sub

    Sub Test5()
        Mid(P2, 5, 3) = "999"
    End Sub

    Sub Test6()
        Dim x = <x y="abc"/>
        Mid$(x.<y>, 1, 2) = "d"
        Mid$(<x/>.<y>, 1, 2) = "d"
    End Sub

    ReadOnly Property P1 As String
        Get
            Return ""
        End Get
    End Property

    WriteOnly Property P2 As Integer
        Set(value As Integer)
        End Set
    End Property

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemCoreRef, SystemXmlRef, SystemXmlLinqRef}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30277: Type character '#' does not match declared data type 'String'.
        Mid#(x, 2) = "a"
        ~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        Mid(Main(), 3) = "a"
            ~~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        Mid(4.ToString(), 5, 3) = "bcdef"
            ~~~~~~~~~~~~
BC42104: Variable 'target' is used before it has been assigned a value. A null reference exception could result at runtime.
        Mid(target, 5) = "00"
            ~~~~~~
BC30526: Property 'P1' is 'ReadOnly'.
        Mid(P1, 5, 3) = "gh"
            ~~
BC30524: Property 'P2' is 'WriteOnly'.
        Mid(P2, 5, 3) = "999"
            ~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        Mid$(x.<y>, 1, 2) = "d"
             ~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        Mid$(<x/>.<y>, 1, 2) = "d"
             ~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub MidAssignment3()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Class Module1

    Sub Main()
        Dim x As String = "ddd"
        Mid(x, 2) = "a"
    End Sub

End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef, options:=TestOptions.ReleaseDll)

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StringType.MidStmtStr' is not defined.
        Mid(x, 2) = "a"
        ~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub MidAssignment4()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System        

Class Module1

    Sub Main()
        Dim x As Integer = 222222
        Dim y As Integer = 3

        Mid(x, 'BIND1:"x"
            2) = y 'BIND2:"y"
    End Sub

End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef, options:=TestOptions.ReleaseDll)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim midExpression As MidExpressionSyntax = Nothing

            If True Then
                Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)

                midExpression = DirectCast(node1.Parent.Parent.Parent, MidExpressionSyntax)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())
                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("NarrowingString", conv.Kind.ToString())
                Assert.True(conv.Exists)

                Dim symbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node1)
                Assert.Equal("x As System.Int32", symbolInfo.Symbol.ToTestDisplayString())
            End If

            If True Then
                Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 2)
                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())
                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("NarrowingString", conv.ToString())
                Assert.True(conv.Exists)

                Dim symbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node1)
                Assert.Equal("y As System.Int32", symbolInfo.Symbol.ToTestDisplayString())
            End If

            If True Then
                Dim node1 As ExpressionSyntax = midExpression
                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())
                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Identity", conv.Kind.ToString())
                Assert.True(conv.Exists)

                Dim symbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node1)
                Assert.Null(symbolInfo.Symbol)
                Assert.Empty(symbolInfo.CandidateSymbols)
            End If
        End Sub

        <WorkItem(642269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/642269")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Bug642269()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System        

Class Module1

    Shared Sub Main()
        Dim s() As Object = {Nothing, Nothing, "12345"}
        Mid(s(2), 1, 1) = "-"
        System.Console.WriteLine(s(2))
    End Sub

End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
            <![CDATA[
-2345
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {Class, IMoveable})(item As T)
        item.Position += GetOffset(item)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000f:  ldarga.s   V_0
  IL_0011:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0016:  add.ovf
  IL_0017:  constrained. "T"
  IL_001d:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0022:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldarga.s   V_0
  IL_0003:  ldarga.s   V_0
  IL_0005:  constrained. "T"
  IL_000b:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0010:  ldarga.s   V_0
  IL_0012:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0017:  add.ovf
  IL_0018:  constrained. "T"
  IL_001e:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0023:  nop
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {Class, IMoveable})(ByRef item As T)
        item.Position += GetOffset(item)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  constrained. "T"
  IL_0008:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000d:  ldarg.0
  IL_000e:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0013:  add.ovf
  IL_0014:  constrained. "T"
  IL_001a:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_001f:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.0
  IL_0003:  constrained. "T"
  IL_0009:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000e:  ldarg.0
  IL_000f:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0014:  add.ovf
  IL_0015:  constrained. "T"
  IL_001b:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0020:  nop
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {Class, IMoveable})(item As T)
        Dim lItem = item
        lItem.Position += GetOffset(lItem)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (T V_0) //lItem
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldloca.s   V_0
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  add.ovf
  IL_0019:  constrained. "T"
  IL_001f:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0024:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  3
  .locals init (T V_0) //lItem
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  ldloca.s   V_0
  IL_0007:  constrained. "T"
  IL_000d:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0019:  add.ovf
  IL_001a:  constrained. "T"
  IL_0020:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0025:  nop
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_04()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = { New Item With {.Name = "Goo"} }
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {Class, IMoveable})(item As T())
        item(0).Position += GetOffset(item(0))
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  4
  .locals init (T() V_0)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  readonly.
  IL_0006:  ldelema    "T"
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  readonly.
  IL_000f:  ldelema    "T"
  IL_0014:  constrained. "T"
  IL_001a:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldelema    "T"
  IL_0026:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_002b:  add.ovf
  IL_002c:  constrained. "T"
  IL_0032:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0037:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  4
  .locals init (T() V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  readonly.
  IL_0007:  ldelema    "T"
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  readonly.
  IL_0010:  ldelema    "T"
  IL_0015:  constrained. "T"
  IL_001b:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0020:  ldarg.0
  IL_0021:  ldc.i4.0
  IL_0022:  ldelema    "T"
  IL_0027:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_002c:  add.ovf
  IL_002d:  constrained. "T"
  IL_0033:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0038:  nop
  IL_0039:  ret
}
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_05()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Structure Test(Of T)
    Public F As T
End Structure

Class Program
    Shared Sub Main()
        Dim item = New Test(Of Item) With { .F = New Item With {.Name = "Goo"} } 
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {Class, IMoveable})(item As Test(Of T))
        item.F.Position += GetOffset(item.F)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  3
  .locals init (T& V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldflda     "Test(Of T).F As T"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  constrained. "T"
  IL_0010:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0015:  ldarga.s   V_0
  IL_0017:  ldflda     "Test(Of T).F As T"
  IL_001c:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0021:  add.ovf
  IL_0022:  constrained. "T"
  IL_0028:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_002d:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (T& V_0)
  IL_0000:  nop
  IL_0001:  ldarga.s   V_0
  IL_0003:  ldflda     "Test(Of T).F As T"
  IL_0008:  dup
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0016:  ldarga.s   V_0
  IL_0018:  ldflda     "Test(Of T).F As T"
  IL_001d:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0022:  add.ovf
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_002e:  nop
  IL_002f:  ret
}
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_06()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {Class, IMoveable})(item As T)
        With item
            .Position += GetOffset(item)
        End With
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000f:  ldarga.s   V_0
  IL_0011:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0016:  add.ovf
  IL_0017:  constrained. "T"
  IL_001d:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0022:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarga.s   V_0
  IL_0004:  ldarga.s   V_0
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  add.ovf
  IL_0019:  constrained. "T"
  IL_001f:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0024:  nop
  IL_0025:  nop
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_07()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {IMoveable})(item As T)
        item.Position += GetOffset(item)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000f:  ldarga.s   V_0
  IL_0011:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0016:  add.ovf
  IL_0017:  constrained. "T"
  IL_001d:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0022:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldarga.s   V_0
  IL_0003:  ldarga.s   V_0
  IL_0005:  constrained. "T"
  IL_000b:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0010:  ldarga.s   V_0
  IL_0012:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0017:  add.ovf
  IL_0018:  constrained. "T"
  IL_001e:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0023:  nop
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_08()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {IMoveable})(ByRef item As T)
        item.Position += GetOffset(item)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  constrained. "T"
  IL_0008:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000d:  ldarg.0
  IL_000e:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0013:  add.ovf
  IL_0014:  constrained. "T"
  IL_001a:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_001f:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.0
  IL_0003:  constrained. "T"
  IL_0009:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000e:  ldarg.0
  IL_000f:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0014:  add.ovf
  IL_0015:  constrained. "T"
  IL_001b:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0020:  nop
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_09()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {IMoveable})(item As T)
        Dim lItem = item
        lItem.Position += GetOffset(lItem)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (T V_0) //lItem
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldloca.s   V_0
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  add.ovf
  IL_0019:  constrained. "T"
  IL_001f:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0024:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  3
  .locals init (T V_0) //lItem
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  ldloca.s   V_0
  IL_0007:  constrained. "T"
  IL_000d:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0019:  add.ovf
  IL_001a:  constrained. "T"
  IL_0020:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0025:  nop
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_10()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = { New Item With {.Name = "Goo"} }
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {IMoveable})(item As T())
        item(0).Position += GetOffset(item(0))
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  4
  .locals init (T() V_0)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  readonly.
  IL_0006:  ldelema    "T"
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  readonly.
  IL_000f:  ldelema    "T"
  IL_0014:  constrained. "T"
  IL_001a:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldelema    "T"
  IL_0026:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_002b:  add.ovf
  IL_002c:  constrained. "T"
  IL_0032:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0037:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  4
  .locals init (T() V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  readonly.
  IL_0007:  ldelema    "T"
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  readonly.
  IL_0010:  ldelema    "T"
  IL_0015:  constrained. "T"
  IL_001b:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0020:  ldarg.0
  IL_0021:  ldc.i4.0
  IL_0022:  ldelema    "T"
  IL_0027:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_002c:  add.ovf
  IL_002d:  constrained. "T"
  IL_0033:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0038:  nop
  IL_0039:  ret
}
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_11()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Structure Test(Of T)
    Public F As T
End Structure

Class Program
    Shared Sub Main()
        Dim item = New Test(Of Item) With { .F = New Item With {.Name = "Goo"} } 
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {IMoveable})(item As Test(Of T))
        item.F.Position += GetOffset(item.F)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  3
  .locals init (T& V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldflda     "Test(Of T).F As T"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  constrained. "T"
  IL_0010:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0015:  ldarga.s   V_0
  IL_0017:  ldflda     "Test(Of T).F As T"
  IL_001c:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0021:  add.ovf
  IL_0022:  constrained. "T"
  IL_0028:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_002d:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (T& V_0)
  IL_0000:  nop
  IL_0001:  ldarga.s   V_0
  IL_0003:  ldflda     "Test(Of T).F As T"
  IL_0008:  dup
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0016:  ldarga.s   V_0
  IL_0018:  ldflda     "Test(Of T).F As T"
  IL_001d:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0022:  add.ovf
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_002e:  nop
  IL_002f:  ret
}
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/63221"), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_12()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item)
    End Sub

    Shared Sub Shift(Of T As {IMoveable})(item As T)
        With item
            .Position += GetOffset(item)
        End With
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000f:  ldarga.s   V_0
  IL_0011:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0016:  add.ovf
  IL_0017:  constrained. "T"
  IL_001d:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0022:  ret
}
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            verifier = CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
]]>)

            ' Verify presence of constrained calls in order to enforce compatibility with Dev12
            verifier.VerifyIL("Program.Shift",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarga.s   V_0
  IL_0004:  ldarga.s   V_0
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  add.ovf
  IL_0019:  constrained. "T"
  IL_001f:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0024:  nop
  IL_0025:  nop
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_13()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position(x as Integer) As Integer
    Property Position(x as String) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x as Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property

    Public Property Position(x as String) As Integer Implements IMoveable.Position
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item, 0)
    End Sub

    Shared Sub Shift(Of T As {Class, IMoveable})(item As T, index as Object)
        item.Position(index) += GetOffset(item)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)
        End Sub

        <Fact(), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_14()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position(x as Integer) As Integer
    Property Position(x as String) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x as Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property

    Public Property Position(x as String) As Integer Implements IMoveable.Position
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item, 0)
    End Sub

    Shared Sub Shift(Of T As {IMoveable})(item As T, index as Object)
        item.Position(index) += GetOffset(item)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
]]>)
        End Sub

        <Fact(), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_15()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String
    Public B1 As Boolean
    Public B2 As Boolean

    Public Property Position As Integer Implements IMoveable.Position
        Get
            B1 = True
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            B2 = True
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item)
        Console.WriteLine(item.B1)
        Console.WriteLine(item.B2)
    End Sub

    Shared Sub Shift(Of T As {IMoveable})(ByRef item As T)
        item.Position += GetOffset(item)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
False
True
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Bar'
False
True
]]>)
        End Sub

        <Fact(), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941_16()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
    Property Position(x As String) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String
    Public B1 As Boolean
    Public B2 As Boolean

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            B1 = True
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            B2 = True
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property

    Public Property Position(x As String) As Integer Implements IMoveable.Position
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Shift(item, 0)
        Console.WriteLine(item.B1)
        Console.WriteLine(item.B2)
    End Sub

    Shared Sub Shift(Of T As {IMoveable})(item As T, index As Object)
        item.Position(index) += GetOffset(item)
    End Sub

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
False
False
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(compilation,
            <![CDATA[
Position get for item 'Goo'
Position set for item 'Goo'
False
False
]]>)
        End Sub

        <Fact, WorkItem(4132, "https://github.com/dotnet/roslyn/issues/4132")>
        Public Sub Issue4132()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Namespace NullableMathRepro
    Module Program
        Sub Main()
            Dim x As Integer? = 0
            x += 5
            Console.WriteLine("'x' is {0}", x)

            Dim y As IntHolder? = 0
            y += 5
            Console.WriteLine("'y' is {0}", y)
        End Sub
    End Module

    Structure IntHolder
        Private x As Integer

        Public Shared Widening Operator CType(ih As IntHolder) As Integer
            Console.WriteLine("operator int (IntHolder ih)")
            Return ih.x
        End Operator

        Public Shared Widening Operator CType(i As Integer) As IntHolder
            Console.WriteLine("operator IntHolder(int i)")
            Return New IntHolder With {.x = i}
        End Operator

        Public Shared Operator +(x As IntHolder, y As Integer) As Integer
            Return CInt(x) + y
        End Operator
        Public Overrides Function ToString() As String
            Return x.ToString()
        End Function

    End Structure
End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
            <![CDATA[
'x' is 5
operator IntHolder(int i)
operator int (IntHolder ih)
operator IntHolder(int i)
'y' is 5
]]>)
        End Sub

    End Class

End Namespace

