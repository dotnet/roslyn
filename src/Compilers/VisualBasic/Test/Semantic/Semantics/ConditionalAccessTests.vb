' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class ConditionalAccessTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub Simple1()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Test1(New S1())
        Test1(Nothing)
    	System.Console.WriteLine("---------")
        Test2(New S1())
        Test2(Nothing)
	    System.Console.WriteLine("---------")
        Test3(New C1())
        Test3(Nothing)
	    System.Console.WriteLine("---------")
        Test4(New C1())
        Test4(Nothing)
	    System.Console.WriteLine("---------")

        Test5(Of S1)(Nothing)
	    System.Console.WriteLine("---------")
        Test6(Of S1)(Nothing)
	    System.Console.WriteLine("---------")

        Test5(Of C1)(New C1())
        Test5(Of C1)(Nothing)
	    System.Console.WriteLine("---------")
        Test6(Of C1)(New C1())
        Test6(Of C1)(Nothing)
	    System.Console.WriteLine("---------")

        Test7(Of S1)(Nothing)
	    System.Console.WriteLine("---------")
        Test8(Of S1)(Nothing)
	    System.Console.WriteLine("---------")

        Test7(Of C1)(New C1())
        Test7(Of C1)(Nothing)
	    System.Console.WriteLine("---------")
        Test8(Of C1)(New C1())
        Test8(Of C1)(Nothing)
	    System.Console.WriteLine("---------")
    End Sub

    Sub Test1(x As S1?)
	    Dim y = x?.P1 'BIND1:"P1"
        System.Console.WriteLine(if(y.HasValue, y.ToString(), "Null"))
    End Sub

    Sub [Do](Of T)(x As T)
        System.Console.WriteLine(if(CObj(x),"Null"))
    End Sub

    Sub Test1_IL_01(x As S1?)
	    [Do](x?.P1)
    End Sub

    Sub Test1_IL_02(ByRef x As S1?)
	    [Do](x?.P1)
    End Sub

    Sub Test1_IL_03(x As S1?)
	    [Do]((x)?.P1)
    End Sub

    Sub Test1_IL_04(ByRef x As S1?)
	    [Do]((x)?.P1)
    End Sub

    Sub Test1_IL_05(x() As S1?)
	    [Do](x(0)?.P1)
    End Sub

    Sub Test2(x As S1?)
	    Dim y = x?.P2
        System.Console.WriteLine(if(y, "Null"))
    End Sub

    Sub Test3(x As C1)
	    Dim y = x?.P1
        System.Console.WriteLine(if(y.HasValue, y.ToString(), "Null"))
    End Sub

    Sub Test4(x As C1)
	    Dim y = x?.P2
        System.Console.WriteLine(if(y, "Null"))
    End Sub

    Sub Test4_IL_01(x As C1)
	    [Do](x?.P2)
    End Sub

    Sub Test4_IL_02(ByRef x As C1)
	    [Do](x?.P2)
    End Sub

    Sub Test4_IL_03(x As C1)
	    [Do]((x)?.P2)
    End Sub

    Sub Test4_IL_04(ByRef x As C1)
	    [Do]((x)?.P2)
    End Sub

    Sub Test4_IL_05(x() As C1)
	    [Do](x(0)?.P2)
    End Sub

    Sub Test5(Of T As I1)(x As T)
	    Dim y = x?.P1
        System.Console.WriteLine(if(y.HasValue, y.ToString(), "Null"))
    End Sub

    Sub Test6(Of T As I1)(x As T)
	    Dim y = x?.P2
        System.Console.WriteLine(if(y, "Null"))
    End Sub

    Sub Test6_IL_01(Of T As I1)(x As T)
	    [Do](x?.P2)
    End Sub

    Sub Test6_IL_02(Of T As I1)(ByRef x As T)
	    [Do](x?.P2)
    End Sub

    Sub Test6_IL_03(Of T As I1)(x As T)
	    [Do]((x)?.P2)
    End Sub

    Sub Test6_IL_04(Of T As I1)(ByRef x As T)
	    [Do]((x)?.P2)
    End Sub

    Sub Test6_IL_05(Of T As I1)(x() As T)
	    [Do](x(0)?.P2)
    End Sub

    Function GetT(Of T)(x As T) As T
        Return x
    End Function

    Sub Test7(Of T As I1)(x As T)
	    [Do](GetT(x)?.P1)
    End Sub

    Sub Test8(Of T As I1)(x As T)
	    [Do](GetT(x)?.P2)
    End Sub
End Module

Interface I1
    ReadOnly Property P1 As Integer
    ReadOnly Property P2 As String
End Interface

Structure S1
    Implements I1

    ReadOnly Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("S1.P1")
            Return 1
        End Get
    End Property

    ReadOnly Property P2 As String Implements I1.P2
        Get
            System.Console.WriteLine("S1.P2")
            Return 2
        End Get
    End Property
End Structure

Class C1
    Implements I1

    ReadOnly Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C1.P1")
            Return 3
        End Get
    End Property

    ReadOnly Property P2 As String Implements I1.P2
        Get
            System.Console.WriteLine("C1.P2")
            Return 4
        End Get
    End Property
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            '            AssertTheseDiagnostics(compilation,
            '<expected>
            '</expected>)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
S1.P1
1
Null
---------
S1.P2
2
Null
---------
C1.P1
3
Null
---------
C1.P2
4
Null
---------
S1.P1
1
---------
S1.P2
2
---------
C1.P1
3
Null
---------
C1.P2
4
Null
---------
S1.P1
1
---------
S1.P2
2
---------
C1.P1
3
Null
---------
C1.P2
4
Null
---------
]]>)

            verifier.VerifyIL("Module1.Test1_IL_01",
            <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  1
  .locals init (Integer? V_0,
                S1 V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function S1?.get_HasValue() As Boolean"
  IL_0007:  brtrue.s   IL_0014
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    "Integer?"
  IL_0011:  ldloc.0
  IL_0012:  br.s       IL_0028
  IL_0014:  ldarga.s   V_0
  IL_0016:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_001b:  stloc.1
  IL_001c:  ldloca.s   V_1
  IL_001e:  call       "Function S1.get_P1() As Integer"
  IL_0023:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0028:  call       "Sub Module1.Do(Of Integer?)(Integer?)"
  IL_002d:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1_IL_02",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  1
  .locals init (S1? V_0,
                Integer? V_1,
                S1 V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      "S1?"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Function S1?.get_HasValue() As Boolean"
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    "Integer?"
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_002f
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_0022:  stloc.2
  IL_0023:  ldloca.s   V_2
  IL_0025:  call       "Function S1.get_P1() As Integer"
  IL_002a:  newobj     "Sub Integer?..ctor(Integer)"
  IL_002f:  call       "Sub Module1.Do(Of Integer?)(Integer?)"
  IL_0034:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1_IL_03",
            <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  1
  .locals init (Integer? V_0,
                S1 V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function S1?.get_HasValue() As Boolean"
  IL_0007:  brtrue.s   IL_0014
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    "Integer?"
  IL_0011:  ldloc.0
  IL_0012:  br.s       IL_0028
  IL_0014:  ldarga.s   V_0
  IL_0016:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_001b:  stloc.1
  IL_001c:  ldloca.s   V_1
  IL_001e:  call       "Function S1.get_P1() As Integer"
  IL_0023:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0028:  call       "Sub Module1.Do(Of Integer?)(Integer?)"
  IL_002d:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1_IL_04",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  1
  .locals init (S1? V_0,
                Integer? V_1,
                S1 V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      "S1?"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Function S1?.get_HasValue() As Boolean"
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    "Integer?"
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_002f
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_0022:  stloc.2
  IL_0023:  ldloca.s   V_2
  IL_0025:  call       "Function S1.get_P1() As Integer"
  IL_002a:  newobj     "Sub Integer?..ctor(Integer)"
  IL_002f:  call       "Sub Module1.Do(Of Integer?)(Integer?)"
  IL_0034:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1_IL_05",
            <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (S1? V_0,
                Integer? V_1,
                S1 V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem     "S1?"
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function S1?.get_HasValue() As Boolean"
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    "Integer?"
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_0030
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_0023:  stloc.2
  IL_0024:  ldloca.s   V_2
  IL_0026:  call       "Function S1.get_P1() As Integer"
  IL_002b:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0030:  call       "Sub Module1.Do(Of Integer?)(Integer?)"
  IL_0035:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_IL_01",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldnull
  IL_0004:  br.s       IL_000c
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.get_P2() As String"
  IL_000c:  call       "Sub Module1.Do(Of String)(String)"
  IL_0011:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_IL_02",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  dup
  IL_0003:  brtrue.s   IL_0009
  IL_0005:  pop
  IL_0006:  ldnull
  IL_0007:  br.s       IL_000e
  IL_0009:  call       "Function C1.get_P2() As String"
  IL_000e:  call       "Sub Module1.Do(Of String)(String)"
  IL_0013:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_IL_03",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldnull
  IL_0004:  br.s       IL_000c
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.get_P2() As String"
  IL_000c:  call       "Sub Module1.Do(Of String)(String)"
  IL_0011:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_IL_04",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  dup
  IL_0003:  brtrue.s   IL_0009
  IL_0005:  pop
  IL_0006:  ldnull
  IL_0007:  br.s       IL_000e
  IL_0009:  call       "Function C1.get_P2() As String"
  IL_000e:  call       "Sub Module1.Do(Of String)(String)"
  IL_0013:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_IL_05",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.ref
  IL_0003:  dup
  IL_0004:  brtrue.s   IL_000a
  IL_0006:  pop
  IL_0007:  ldnull
  IL_0008:  br.s       IL_000f
  IL_000a:  call       "Function C1.get_P2() As String"
  IL_000f:  call       "Sub Module1.Do(Of String)(String)"
  IL_0014:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test6_IL_01",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brtrue.s   IL_000b
  IL_0008:  ldnull
  IL_0009:  br.s       IL_0018
  IL_000b:  ldarga.s   V_0
  IL_000d:  constrained. "T"
  IL_0013:  callvirt   "Function I1.get_P2() As String"
  IL_0018:  call       "Sub Module1.Do(Of String)(String)"
  IL_001d:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test6_IL_02",
            <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    "T"
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0025
  IL_0011:  ldobj      "T"
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        "T"
  IL_001f:  brtrue.s   IL_0025
  IL_0021:  pop
  IL_0022:  ldnull
  IL_0023:  br.s       IL_0030
  IL_0025:  constrained. "T"
  IL_002b:  callvirt   "Function I1.get_P2() As String"
  IL_0030:  call       "Sub Module1.Do(Of String)(String)"
  IL_0035:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test6_IL_03",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brtrue.s   IL_000b
  IL_0008:  ldnull
  IL_0009:  br.s       IL_0018
  IL_000b:  ldarga.s   V_0
  IL_000d:  constrained. "T"
  IL_0013:  callvirt   "Function I1.get_P2() As String"
  IL_0018:  call       "Sub Module1.Do(Of String)(String)"
  IL_001d:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test6_IL_04",
            <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    "T"
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0025
  IL_0011:  ldobj      "T"
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        "T"
  IL_001f:  brtrue.s   IL_0025
  IL_0021:  pop
  IL_0022:  ldnull
  IL_0023:  br.s       IL_0030
  IL_0025:  constrained. "T"
  IL_002b:  callvirt   "Function I1.get_P2() As String"
  IL_0030:  call       "Sub Module1.Do(Of String)(String)"
  IL_0035:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test6_IL_05",
            <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  readonly.
  IL_0004:  ldelema    "T"
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    "T"
  IL_0011:  ldloc.0
  IL_0012:  box        "T"
  IL_0017:  brtrue.s   IL_002d
  IL_0019:  ldobj      "T"
  IL_001e:  stloc.0
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldloc.0
  IL_0022:  box        "T"
  IL_0027:  brtrue.s   IL_002d
  IL_0029:  pop
  IL_002a:  ldnull
  IL_002b:  br.s       IL_0038
  IL_002d:  constrained. "T"
  IL_0033:  callvirt   "Function I1.get_P2() As String"
  IL_0038:  call       "Sub Module1.Do(Of String)(String)"
  IL_003d:  ret
}
]]>)


            verifier.VerifyIL("Module1.Test8",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.GetT(Of T)(T) As T"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0015
  IL_0011:  pop
  IL_0012:  ldnull
  IL_0013:  br.s       IL_0020
  IL_0015:  constrained. "T"
  IL_001b:  callvirt   "Function I1.get_P2() As String"
  IL_0020:  call       "Sub Module1.Do(Of String)(String)"
  IL_0025:  ret
}
]]>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            typeInfo = semanticModel.GetTypeInfo(node1)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString())

            symbolInfo = semanticModel.GetSymbolInfo(node1)

            Assert.Equal("ReadOnly Property S1.P1 As System.Int32", symbolInfo.Symbol.ToTestDisplayString())

            Dim member = DirectCast(node1.Parent, MemberAccessExpressionSyntax)

            Assert.Null(member.Expression)

            typeInfo = semanticModel.GetTypeInfo(member)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString())

            symbolInfo = semanticModel.GetSymbolInfo(member)

            Assert.Equal("ReadOnly Property S1.P1 As System.Int32", symbolInfo.Symbol.ToTestDisplayString())

            Dim conditional = DirectCast(member.Parent, ConditionalAccessExpressionSyntax)

            typeInfo = semanticModel.GetTypeInfo(conditional)
            Assert.Equal("System.Nullable(Of System.Int32)", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

            symbolInfo = semanticModel.GetSymbolInfo(conditional)

            Assert.Null(symbolInfo.Symbol)
            Assert.True(symbolInfo.CandidateSymbols.IsEmpty)

            Dim receiver = DirectCast(conditional.Expression, IdentifierNameSyntax)

            typeInfo = semanticModel.GetTypeInfo(receiver)
            Assert.Equal("System.Nullable(Of S1)", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of S1)", typeInfo.ConvertedType.ToTestDisplayString())

            symbolInfo = semanticModel.GetSymbolInfo(receiver)

            Assert.Equal("x As System.Nullable(Of S1)", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub Simple2()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Test1(New S1())
    	System.Console.WriteLine("---------")
        Test1(Nothing)
    	System.Console.WriteLine("---------")
        Test2(New S1())
    	System.Console.WriteLine("---------")
        Test2(Nothing)
    	System.Console.WriteLine("---------")
        Test3(New S1())
    	System.Console.WriteLine("---------")
        Test3(Nothing)
    	System.Console.WriteLine("---------")
        Test4(New S1())
    	System.Console.WriteLine("---------")
        Test4(Nothing)
    	System.Console.WriteLine("---------")
        Test5(New S1())
    	System.Console.WriteLine("---------")
        Test5(Nothing)
    	System.Console.WriteLine("---------")
    End Sub

    Function GetX(x As S1?) As S1?
    	System.Console.WriteLine("GetX")
        Return x
    End Function

    Sub Test1(x As S1?)
    	System.Console.WriteLine("Test1")
	    Dim y = GetX(x)?.M1()
        System.Console.WriteLine(if(y.HasValue, y.ToString(), "Null"))
    End Sub

    Sub Test2(x As S1?)
    	System.Console.WriteLine("Test2")
	    GetX(x)?.M2()
    End Sub

    Sub Test3(x As S1?)
    	System.Console.WriteLine("Test3")
	    GetX(x)?.M2
    End Sub

    Sub Test4(x As S1?)
    	System.Console.WriteLine("Test4")
	    Call GetX(x)?.M2
    End Sub

    Sub Test5(x As S1?)
    	System.Console.WriteLine("Test5")
	    Call GetX(x)?.M1()
    End Sub
End Module

Structure S1

    Function M1() As Integer
        System.Console.WriteLine("S1.M1")
        Return 1
    End Function

    Sub M2()
        System.Console.WriteLine("S1.M2")
    End Sub
End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1
GetX
S1.M1
1
---------
Test1
GetX
Null
---------
Test2
GetX
S1.M2
---------
Test2
GetX
---------
Test3
GetX
S1.M2
---------
Test3
GetX
---------
Test4
GetX
S1.M2
---------
Test4
GetX
---------
Test5
GetX
S1.M1
---------
Test5
GetX
---------
]]>)
        End Sub

        <Fact()>
        Public Sub Simple3()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test2(x As S1?)
        System.Console.WriteLine(x?.M2())
    End Sub
End Module

Structure S1
    Sub M2()
        System.Console.WriteLine("S1.M2")
    End Sub
End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
        System.Console.WriteLine(x?.M2())
                                   ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CallContext1()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test(x As S1)
        Call x.M1(0)
        x.M1(0)
    End Sub

    Sub Test(x As S1?)
        Call x?.M1(0)
        x?.M1(0)
    End Sub
End Module

Structure S1

    Function M1() As Integer()
        System.Console.WriteLine("S1.M1")
        Return {1}
    End Function

End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Function M1() As Integer()'.
        Call x.M1(0)
                  ~
BC30057: Too many arguments to 'Public Function M1() As Integer()'.
        x.M1(0)
             ~
BC30057: Too many arguments to 'Public Function M1() As Integer()'.
        Call x?.M1(0)
                   ~
BC30057: Too many arguments to 'Public Function M1() As Integer()'.
        x?.M1(0)
              ~
</expected>)
        End Sub

        <Fact()>
        Public Sub CallContext2()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Test1(New S1())
        System.Console.WriteLine("---")
        Test2(New S1())
        System.Console.WriteLine("---")
        Test2(Nothing)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x As S1)
        System.Console.WriteLine(x.M1(0))
    End Sub

    Sub Test2(x As S1?)
        System.Console.WriteLine(x?.M1(0))
    End Sub
End Module

Structure S1

    Function M1() As Integer()
        System.Console.WriteLine("S1.M1")
        Return {1}
    End Function

End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
   S1.M1
1
---
S1.M1
1
---

---
]]>)
        End Sub

        <Fact()>
        Public Sub CallContext3()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Test1(New S1() With {.m_Array = {"1"}})
        System.Console.WriteLine("---")
        Test2(New S1() With {.m_Array = {"2"}})
        System.Console.WriteLine("---")
        Test2(New S1() With {.m_Array = {Nothing}})
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x As S1)
        Call x.M1(0).ToString()
    End Sub

    Sub Test2(x As S1)
        Call x.M1(0)?.ToString()
    End Sub
End Module

Structure S1

    Public m_Array As String()
    Function M1() As String()
        System.Console.WriteLine("S1.M1")
        Return m_Array
    End Function

End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
S1.M1
---
S1.M1
---
S1.M1
---
]]>)
        End Sub

        <Fact()>
        Public Sub CallContext4()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test(x As S1)
        Call x.P1()
        x.P1()
        x.P1
    End Sub

    Sub Test(x As S1?)
        Call x?.P1()
        x?.P1()
        x?.P1
    End Sub
End Module

Structure S1

    Property P1() As Integer

End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30545: Property access must assign to the property or use its value.
        Call x.P1()
             ~~~~~~
BC30545: Property access must assign to the property or use its value.
        x.P1()
        ~~~~~~
BC30545: Property access must assign to the property or use its value.
        x.P1
        ~~~~
BC30545: Property access must assign to the property or use its value.
        Call x?.P1()
               ~~~~~
BC30545: Property access must assign to the property or use its value.
        x?.P1()
          ~~~~~
BC30545: Property access must assign to the property or use its value.
        x?.P1
          ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AssignmentContext()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test(x As S1?)
        x?.P1() = Nothing
        x?.P1 = Nothing
        x?.F1 = Nothing
    End Sub
End Module

Structure S1

    Property P1() As Integer
    Public F1 As Integer
End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x?.P1() = Nothing
        ~~~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x?.P1 = Nothing
        ~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x?.F1 = Nothing
        ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ERR_CannotBeMadeNullable1_1()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test(Of T, U As Class, V As Structure)(x1 As S1(Of T)?, x2 As S1(Of T)?, x3 As S1(Of U)?, x4 As S1(Of U)?, x5 As S1(Of V)?, x6 As S1(Of V)?)
        Dim y1 = x1?.M1()
        x2?.M1()
        Dim y3 = x3?.M1()
        x4?.M1()
        Dim y5 = x5?.M1()
        x5?.M1()
    End Sub

End Module

Structure S1(Of T)

    Function M1() As T
        Return Nothing
    End Function

End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC37238: 'T' cannot be made nullable.
        Dim y1 = x1?.M1()
                    ~~~~~
</expected>)
        End Sub

        <WorkItem(23422, "https://github.com/dotnet/roslyn/issues/23422")>
        <Fact()>
        Public Sub ERR_CannotBeMadeNullable1_2()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()
        Dim o = New C1
        Dim x = o?.F ' this should be an error
    End Sub
End Module

Public Class C1
    Public Function F() As TypedReference
        System.Console.WriteLine("hi")
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC37238: 'TypedReference' cannot be made nullable.
        Dim x = o?.F ' this should be an error
                  ~~
</expected>)
        End Sub

        <WorkItem(23422, "https://github.com/dotnet/roslyn/issues/23422")>
        <Fact()>
        Public Sub ERR_CannotBeMadeNullable1_3()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Sub Main()
        Dim o = New C1
        o?.F() ' this is ok
    End Sub
End Module

Public Class C1
    Public Function F() As TypedReference
        System.Console.WriteLine("hi")
        Return Nothing
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            ' VB seems to allow methods that return TypedReference, likely for compat reasons
            ' that is technically not verifiable, but it is not relevant to this test
            Dim verifier = CompileAndVerify(compilation, verify:=Verification.Fails, expectedOutput:=
            <![CDATA[
hi
]]>)

            verifier.VerifyIL("Module1.Main()",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (C1 V_0) //o
  IL_0000:  newobj     "Sub C1..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brfalse.s  IL_0010
  IL_0009:  ldloc.0
  IL_000a:  call       "Function C1.F() As System.TypedReference"
  IL_000f:  pop
  IL_0010:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub ERR_UnaryOperand2_1()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test(Of T, U As Class, V As Structure)(x1 As T, x2 As U, x3 As V)
        x1?.ToString()
        x2?.ToString()
        x3?.ToString()
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30487: Operator '?' is not defined for type 'V'.
        x3?.ToString()
          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub InvocationOrIndex_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        System.Console.WriteLine(Invoke(Function(x) CStr(x)))
        System.Console.WriteLine(If(Invoke(Nothing), "Null"))
        System.Console.WriteLine(Index({"2"}))
        System.Console.WriteLine(If(Index(Nothing), "Null"))
        System.Console.WriteLine(DefaultProperty(New C1()))
        System.Console.WriteLine(If(DefaultProperty(Nothing), "Null"))
    End Sub


    Function Invoke(x As System.Func(Of Integer, String)) As String
        Return x?(1) 'BIND1:"(1)"
    End Function

    Function Index(x As String()) As String
        Return x?(0) 'BIND2:"(0)"
    End Function

    Function DefaultProperty(x As C1) As String
        Return x?(3) 'BIND3:"(3)"
    End Function

End Module


Class C1
    Default ReadOnly Property P1(i As Integer) As String
        Get
            Return CStr(i)
        End Get
    End Property
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
1
Null
2
Null
3
Null
]]>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim semanticModel = compilation.GetSemanticModel(tree)

            If True Then
                Dim node1 As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 1)

                Assert.Null(node1.Expression)

                typeInfo = semanticModel.GetTypeInfo(node1)
                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(node1)

                Assert.Equal("Function System.Func(Of System.Int32, System.String).Invoke(arg As System.Int32) As System.String", symbolInfo.Symbol.ToTestDisplayString())

                Dim conditional = DirectCast(node1.Parent, ConditionalAccessExpressionSyntax)

                typeInfo = semanticModel.GetTypeInfo(conditional)
                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(conditional)

                Assert.Null(symbolInfo.Symbol)
                Assert.True(symbolInfo.CandidateSymbols.IsEmpty)

                Dim receiver = DirectCast(conditional.Expression, IdentifierNameSyntax)

                typeInfo = semanticModel.GetTypeInfo(receiver)
                Assert.Equal("System.Func(Of System.Int32, System.String)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Func(Of System.Int32, System.String)", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(receiver)
                Assert.Equal("x As System.Func(Of System.Int32, System.String)", symbolInfo.Symbol.ToTestDisplayString())
            End If

            If True Then
                Dim node2 As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 2)

                Assert.Null(node2.Expression)

                typeInfo = semanticModel.GetTypeInfo(node2)
                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(node2)

                Assert.Null(symbolInfo.Symbol)
                Assert.True(symbolInfo.CandidateSymbols.IsEmpty)

                Dim conditional = DirectCast(node2.Parent, ConditionalAccessExpressionSyntax)

                typeInfo = semanticModel.GetTypeInfo(conditional)
                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(conditional)

                Assert.Null(symbolInfo.Symbol)
                Assert.True(symbolInfo.CandidateSymbols.IsEmpty)

                Dim receiver = DirectCast(conditional.Expression, IdentifierNameSyntax)

                typeInfo = semanticModel.GetTypeInfo(receiver)
                Assert.Equal("System.String()", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String()", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(receiver)
                Assert.Equal("x As System.String()", symbolInfo.Symbol.ToTestDisplayString())
            End If

            If True Then
                Dim node3 As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 3)

                Assert.Null(node3.Expression)

                typeInfo = semanticModel.GetTypeInfo(node3)
                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(node3)

                Assert.Equal("ReadOnly Property C1.P1(i As System.Int32) As System.String", symbolInfo.Symbol.ToTestDisplayString())

                Dim conditional = DirectCast(node3.Parent, ConditionalAccessExpressionSyntax)

                typeInfo = semanticModel.GetTypeInfo(conditional)
                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(conditional)

                Assert.Null(symbolInfo.Symbol)
                Assert.True(symbolInfo.CandidateSymbols.IsEmpty)

                Dim receiver = DirectCast(conditional.Expression, IdentifierNameSyntax)

                typeInfo = semanticModel.GetTypeInfo(receiver)
                Assert.Equal("C1", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("C1", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(receiver)
                Assert.Equal("x As C1", symbolInfo.Symbol.ToTestDisplayString())
            End If

        End Sub

        <Fact()>
        Public Sub XmlMember_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq

Module Module1

    Sub Main()
        Dim x = <e0 a1="a1_1">
                    <e1>e1_1</e1>
                    <e3>
                        <e1>e1_2</e1>
                        <e2>e2_1</e2>
                    </e3>
                </e0>

        System.Console.WriteLine(Test1(x))
        System.Console.WriteLine(Test2(x).Single())
        System.Console.WriteLine(Test3(x).Single())
        System.Console.WriteLine(if(CObj(Test1(Nothing)),"Null"))
        System.Console.WriteLine(if(CObj(Test2(Nothing)),"Null"))
        System.Console.WriteLine(if(CObj(Test3(Nothing)),"Null"))
    End Sub

    Function Test1(x As XElement) As String
        Return x?.@a1 'BIND1:"a1"
    End Function

    Function Test2(x As XElement) As IEnumerable(Of XElement)
        Return x?.<e1> 'BIND2:"<e1>"
    End Function

    Function Test3(x As XElement) As IEnumerable(Of XElement)
        Return x?...<e2> 'BIND3:"<e2>"
    End Function

End Module
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, XmlReferences, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
a1_1
<e1>e1_1</e1>
<e2>e2_1</e2>
Null
Null
Null
]]>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim semanticModel = compilation.GetSemanticModel(tree)

            If True Then
                Dim node1 As XmlNameSyntax = CompilationUtils.FindBindingText(Of XmlNameSyntax)(compilation, "a.vb", 1)

                typeInfo = semanticModel.GetTypeInfo(node1)
                Assert.Null(typeInfo.Type)

                symbolInfo = semanticModel.GetSymbolInfo(node1)

                Assert.Null(symbolInfo.Symbol)
                Assert.True(symbolInfo.CandidateSymbols.IsEmpty)

                Dim member = DirectCast(node1.Parent, XmlMemberAccessExpressionSyntax)

                Assert.Null(member.Base)

                typeInfo = semanticModel.GetTypeInfo(member)
                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(member)
                Assert.Equal("Property My.InternalXmlHelper.AttributeValue(source As System.Xml.Linq.XElement, name As System.Xml.Linq.XName) As System.String", symbolInfo.Symbol.ToTestDisplayString())

                Dim conditional = DirectCast(member.Parent, ConditionalAccessExpressionSyntax)

                typeInfo = semanticModel.GetTypeInfo(conditional)
                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(conditional)

                Assert.Null(symbolInfo.Symbol)
                Assert.True(symbolInfo.CandidateSymbols.IsEmpty)

                Dim receiver = DirectCast(conditional.Expression, IdentifierNameSyntax)

                typeInfo = semanticModel.GetTypeInfo(receiver)
                Assert.Equal("System.Xml.Linq.XElement", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Xml.Linq.XElement", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(receiver)
                Assert.Equal("x As System.Xml.Linq.XElement", symbolInfo.Symbol.ToTestDisplayString())
            End If

            If True Then
                Dim node2 As XmlBracketedNameSyntax = CompilationUtils.FindBindingText(Of XmlBracketedNameSyntax)(compilation, "a.vb", 2)

                typeInfo = semanticModel.GetTypeInfo(node2)
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(node2)

                Assert.Equal("Function System.Xml.Linq.XContainer.Elements(name As System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", symbolInfo.Symbol.ToTestDisplayString())

                Dim member = DirectCast(node2.Parent, XmlMemberAccessExpressionSyntax)

                Assert.Null(member.Base)

                typeInfo = semanticModel.GetTypeInfo(member)
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(member)
                Assert.Equal("Function System.Xml.Linq.XContainer.Elements(name As System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", symbolInfo.Symbol.ToTestDisplayString())

                Dim conditional = DirectCast(member.Parent, ConditionalAccessExpressionSyntax)

                typeInfo = semanticModel.GetTypeInfo(conditional)
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(conditional)

                Assert.Null(symbolInfo.Symbol)
                Assert.True(symbolInfo.CandidateSymbols.IsEmpty)

                Dim receiver = DirectCast(conditional.Expression, IdentifierNameSyntax)

                typeInfo = semanticModel.GetTypeInfo(receiver)
                Assert.Equal("System.Xml.Linq.XElement", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Xml.Linq.XElement", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(receiver)
                Assert.Equal("x As System.Xml.Linq.XElement", symbolInfo.Symbol.ToTestDisplayString())
            End If

            If True Then
                Dim node3 As XmlBracketedNameSyntax = CompilationUtils.FindBindingText(Of XmlBracketedNameSyntax)(compilation, "a.vb", 3)

                typeInfo = semanticModel.GetTypeInfo(node3)
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(node3)

                Assert.Equal("Function System.Xml.Linq.XContainer.Descendants(name As System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", symbolInfo.Symbol.ToTestDisplayString())

                Dim member = DirectCast(node3.Parent, XmlMemberAccessExpressionSyntax)

                Assert.Null(member.Base)

                typeInfo = semanticModel.GetTypeInfo(member)
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(member)
                Assert.Equal("Function System.Xml.Linq.XContainer.Descendants(name As System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", symbolInfo.Symbol.ToTestDisplayString())

                Dim conditional = DirectCast(member.Parent, ConditionalAccessExpressionSyntax)

                typeInfo = semanticModel.GetTypeInfo(conditional)
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(conditional)

                Assert.Null(symbolInfo.Symbol)
                Assert.True(symbolInfo.CandidateSymbols.IsEmpty)

                Dim receiver = DirectCast(conditional.Expression, IdentifierNameSyntax)

                typeInfo = semanticModel.GetTypeInfo(receiver)
                Assert.Equal("System.Xml.Linq.XElement", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Xml.Linq.XElement", typeInfo.ConvertedType.ToTestDisplayString())

                symbolInfo = semanticModel.GetSymbolInfo(receiver)
                Assert.Equal("x As System.Xml.Linq.XElement", symbolInfo.Symbol.ToTestDisplayString())
            End If

        End Sub

        <Fact()>
        Public Sub DictionaryAccess_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine(Test1(New C1()))
        System.Console.WriteLine(if(Test1(Nothing), "Null"))
    End Sub

    Function Test1(x As C1) As String
        Return x?!a1
    End Function

End Module


Class C1
    Default ReadOnly Property P1(i As String) As String
        Get
            Return i
        End Get
    End Property
End Class
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
a1
Null
]]>)
        End Sub

        <Fact()>
        Public Sub LateBound_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine(Test1(New C1()))
        System.Console.WriteLine(Test2(New C1()))
        Test3(New C1())
        Test4(New C1())
        System.Console.WriteLine(Test5(New C1()))
        System.Console.WriteLine(Test6(New C1(), "a4"))

        System.Console.WriteLine(If(Test1(Nothing), "Null"))
        System.Console.WriteLine(if(Test2(Nothing), "Null"))
        Test3(Nothing)
        Test4(Nothing)
        System.Console.WriteLine(if(Test5(Nothing), "Null"))
        System.Console.WriteLine(if(Test6(Nothing, "a4"), "Null"))
    End Sub

    Function Test1(x As Object) As String
        Return x?!a1
    End Function

    Function Test2(x As Object) As String
        Return x?.P1("a2")
    End Function

    Sub Test3(x As Object)
        System.Console.WriteLine("Test3")
        x?.P1("a2")
    End Sub

    Sub Test4(x As Object)
        System.Console.WriteLine("Test4")
        Dim saveCulture = System.Threading.Thread.CurrentThread.CurrentCulture
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            x?.P2(0)
        Catch e As System.Exception
            System.Console.WriteLine(e.Message)
        Finally
            System.Threading.Thread.CurrentThread.CurrentCulture = saveCulture
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub

    Function Test5(x As Object) As String
        Return x?.P2(0)
    End Function

    Function Test6(x As C1, y As Object) As String
        Return x?.M1(y)
    End Function
End Module


Class C1
    Default ReadOnly Property P1(i As String) As String
        Get
            Return i
        End Get
    End Property

    ReadOnly Property P2 As String()
        Get
            Return {"a3"}
        End Get
    End Property

    Function M1(x As String) As String
        Return x
    End Function

    Function M1(x As Integer) As Integer
        Return x
    End Function
End Class
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
a1
a2
Test3
Test4
Overload resolution failed because no accessible 'P2' accepts this number of arguments.
a3
a4
Null
Null
Test3
Test4
Null
Null
]]>)
        End Sub

        <Fact()>
        Public Sub WRN_UnobservedAwaitableExpression_1()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Async Function Test6(x As C1) As System.Threading.Tasks.Task(Of Integer)
        Dim y = Await x?.M1()
        x?.M1()
        Return 0
    End Function
End Module


Class C1
    Function M1() As System.Threading.Tasks.Task(Of Integer)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected>
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        x?.M1()
        ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ImplicitLocal_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off

Module Module1

    Sub Main()
        Dim y1 = implicit1?.ToString()
        Dim y2 = implicit2?()
        Dim y3 = implicit3.@x
        Dim y4 = implicit4.<x>
        Dim y5 = implicit5...<x>
        Dim y6 = implicit6!x
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, XmlReferences, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30451: 'implicit1' is not declared. It may be inaccessible due to its protection level.
        Dim y1 = implicit1?.ToString()
                 ~~~~~~~~~
BC30451: 'implicit2' is not declared. It may be inaccessible due to its protection level.
        Dim y2 = implicit2?()
                 ~~~~~~~~~
BC42104: Variable 'implicit3' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim y3 = implicit3.@x
                 ~~~~~~~~~
BC31168: XML axis properties do not support late binding.
        Dim y3 = implicit3.@x
                 ~~~~~~~~~~~~
BC42104: Variable 'implicit4' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim y4 = implicit4.<x>
                 ~~~~~~~~~
BC31168: XML axis properties do not support late binding.
        Dim y4 = implicit4.<x>
                 ~~~~~~~~~~~~~
BC42104: Variable 'implicit5' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim y5 = implicit5...<x>
                 ~~~~~~~~~
BC31168: XML axis properties do not support late binding.
        Dim y5 = implicit5...<x>
                 ~~~~~~~~~~~~~~~
BC42104: Variable 'implicit6' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim y6 = implicit6!x
                 ~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ImplicitLocal_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off

Module Module1

    Sub Main()
        Dim y1 = implicit1?.ToString().@x.<x>...<x>!x?.ToString()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, XmlReferences, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30451: 'implicit1' is not declared. It may be inaccessible due to its protection level.
        Dim y1 = implicit1?.ToString().@x.<x>...<x>!x?.ToString()
                 ~~~~~~~~~
BC36807: XML elements cannot be selected from type 'String'.
        Dim y1 = implicit1?.ToString().@x.<x>...<x>!x?.ToString()
                           ~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ImplicitLocal_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off

Module Module1

    Sub Main()
        Dim y1 = implicit1?().ToString.@x.<x>...<x>!x?.ToString()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, XmlReferences, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30451: 'implicit1' is not declared. It may be inaccessible due to its protection level.
        Dim y1 = implicit1?().ToString.@x.<x>...<x>!x?.ToString()
                 ~~~~~~~~~
BC36807: XML elements cannot be selected from type 'String'.
        Dim y1 = implicit1?().ToString.@x.<x>...<x>!x?.ToString()
                           ~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ImplicitLocal_04()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off

Module Module1

    Sub Main()
        Dim y1 = implicit1?.<x>.ToString()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, XmlReferences, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42104: Variable 'implicit1' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim y1 = implicit1?.<x>.ToString()
                 ~~~~~~~~~
BC31168: XML axis properties do not support late binding.
        Dim y1 = implicit1?.<x>.ToString()
                           ~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub Flow_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x as Object
        CStr(Nothing)?.Test(x)
        x.ToString()
    End Sub

    <Extension>
    Sub Test(this as String, ByRef x as Object)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, XmlReferences, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        x.ToString()
        ~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub Flow_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x as Object
        Call "a"?.Test(x)
        x.ToString()
    End Sub

    <Extension>
    Sub Test(this as String, ByRef x as Object)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, XmlReferences, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42030: Variable 'x' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
        Call "a"?.Test(x)
                       ~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub Flow_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x as Object
        GetString()?.Test(x)
        x.ToString()
    End Sub

    Function GetString() As String
        return "b"
    End Function

    <Extension>
    Sub Test(this as String, ByRef x as Object)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, XmlReferences, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42030: Variable 'x' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
        GetString()?.Test(x)
                          ~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub WithStatement_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1

    Sub Main()

        Dim c1 As New C1()

        With "string"
            c1?.M1(.Length)
            Dim y = c1?(.Length)
        End With

    End Sub

End Module


Class C1
    Sub M1(x As Integer)
        System.Console.WriteLine("M1 - {0}", x)
    End Sub

    Default ReadOnly Property P1(x As Integer) As Integer
        Get
            System.Console.WriteLine("P1 - {0}", x)
            Return x
        End Get
    End Property
End Class
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
M1 - 6
P1 - 6
]]>)
        End Sub

        <Fact()>
        Public Sub WithStatement_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1

    Sub Main()
        Test(New C1())
        Test(Nothing)
    End Sub

    Sub Test(c1 As C1)
        System.Console.WriteLine("Test - {0}", c1)

        With c1
            ?.M1()
            Dim y = ?!str
        End With
    End Sub

End Module


Class C1
    Sub M1()
        System.Console.WriteLine("M1")
    End Sub

    Default ReadOnly Property P1(x As String) As String
        Get
            System.Console.WriteLine("P1 - {0}", x)
            Return x
        End Get
    End Property
End Class
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test - C1
M1
P1 - str
Test -
]]>)
        End Sub

        <Fact()>
        Public Sub WithStatement_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim x1 = New C1() With { ?.P2 = "a" }
        Dim x2 = New C1() With { .P3 = ?.P4 }
        Dim x3 = New C1() With { ?!b = "c" }
        Dim x4 = New C1() With { .P5 = ?!d }

        Dim x5 = New With { ?.P2 = "a" }
        Dim x6 = New With { .P3 = ?.P4 }
        Dim x7 = New With { ?!b = "c" }
        Dim x8 = New With { .P5 = ?!d }
    End Sub

    Sub Test(c1 As C1)
            ?.M1()
            Dim y = ?!str
    End Sub
End Module


Class C1
    Sub M1()
        System.Console.WriteLine("M1")
    End Sub

    Default ReadOnly Property P1(x As String) As String
        Get
            System.Console.WriteLine("P1 - {0}", x)
            Return x
        End Get
    End Property
    Property P2 As String
    Property P3 As String
    Property P4 As String
    Property P5 As String
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30985: Name of field or property being initialized in an object initializer must start with '.'.
        Dim x1 = New C1() With { ?.P2 = "a" }
                                 ~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x1 = New C1() With { ?.P2 = "a" }
                                 ~~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x2 = New C1() With { .P3 = ?.P4 }
                                       ~~~~
BC30985: Name of field or property being initialized in an object initializer must start with '.'.
        Dim x3 = New C1() With { ?!b = "c" }
                                 ~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x3 = New C1() With { ?!b = "c" }
                                 ~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x4 = New C1() With { .P5 = ?!d }
                                       ~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x5 = New With { ?.P2 = "a" }
                            ~~~~
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim x5 = New With { ?.P2 = "a" }
                            ~~~~~~~~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x6 = New With { .P3 = ?.P4 }
                                  ~~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x7 = New With { ?!b = "c" }
                            ~~~
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim x7 = New With { ?!b = "c" }
                            ~~~~~~~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x8 = New With { .P5 = ?!d }
                                  ~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
            ?.M1()
            ~~~~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
            Dim y = ?!str
                    ~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub WithStatement_04()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1

    Sub Main()

        Dim s1 As S1? = New S1()

        With "string"
            s1?.M1(.Length)
            Dim y = s1?(.Length)
        End With

    End Sub

End Module


Structure S1
    Sub M1(x As Integer)
        System.Console.WriteLine("M1 - {0}", x)
    End Sub

    Default ReadOnly Property P1(x As Integer) As Integer
        Get
            System.Console.WriteLine("P1 - {0}", x)
            Return x
        End Get
    End Property
End Structure
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
M1 - 6
P1 - 6
]]>)
        End Sub

        <Fact()>
        Public Sub ExpressionTree_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim x As System.Linq.Expressions.Expression(Of System.Func(Of Object, String)) = Function(y As Object) y?.ToString()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, {SystemCoreRef}, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC37240: A null propagating operator cannot be converted into an expression tree.
        Dim x As System.Linq.Expressions.Expression(Of System.Func(Of Object, String)) = Function(y As Object) y?.ToString()
                                                                                                               ~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub AnonymousTypeMemberName_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System.Linq

Module Module1

    Sub Main()
        Dim c1 As New C1()
        Dim x As System.Func(Of String) = Function() "x"
        Dim y = <a><b></b></a>
        Dim z As System.Func(Of System.Func(Of String)) = Function() Function() "z"

        System.Console.WriteLine(new With {c1?.P2})
        System.Console.WriteLine(new With {c1?.P2()})
        System.Console.WriteLine(New With {x?()})
        System.Console.WriteLine(New With {y?.<b>(0)})
        System.Console.WriteLine(New With {y?...<b>(0)})
        System.Console.WriteLine(New With {y.<b>?(0)})
        System.Console.WriteLine(New With {y...<b>?(0)})
        System.Console.WriteLine(New With {y?.<b>?(0)})
        System.Console.WriteLine(New With {y?...<b>?(0)})
        System.Console.WriteLine(New With {z?()()})
        System.Console.WriteLine(New With {z()?()})
        System.Console.WriteLine(New With {z?()?()})
        System.Console.WriteLine(New With {y?.<b>?.Count})
        System.Console.WriteLine(New With {y?.<b>.Count})
        System.Console.WriteLine(New With {y.<b>?.Count})
    End Sub

End Module

Class C1

    ReadOnly Property P2 As String
        Get
            Return 4
        End Get
    End Property
End Class

]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, XmlReferences, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
{ P2 = 4 }
{ P2 = 4 }
{ x = x }
{ b = <b></b> }
{ b = <b></b> }
{ b = <b></b> }
{ b = <b></b> }
{ b = <b></b> }
{ b = <b></b> }
{ z = z }
{ z = z }
{ z = z }
{ b = 1 }
{ b = 1 }
{ b = 1 }]]>)

        End Sub

        <Fact()>
        Public Sub MeMyBaseMyClass_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
    End Sub

End Module

Class C1
    Sub MeTest()
        Me?.ToString() ' 1
    End Sub

    Sub MyBaseTest()
        MyBase?.ToString() ' 2
    End Sub

    Sub MyClassTest()
        MyClass?.ToString() ' 3
    End Sub
End Class

Structure S1
    Sub MeTest()
        Me?.ToString() ' 4
    End Sub

    Sub MyBaseTest()
        MyBase?.ToString() ' 5
    End Sub

    Sub MyClassTest()
        MyClass?.ToString() ' 6
    End Sub
End Structure
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC32027: 'MyBase' must be followed by '.' and an identifier.
        MyBase?.ToString() ' 2
        ~~~~~~
BC32028: 'MyClass' must be followed by '.' and an identifier.
        MyClass?.ToString() ' 3
        ~~~~~~~
BC30487: Operator '?' is not defined for type 'S1'.
        Me?.ToString() ' 4
          ~
BC30044: 'MyBase' is not valid within a structure.
        MyBase?.ToString() ' 5
        ~~~~~~
BC32027: 'MyBase' must be followed by '.' and an identifier.
        MyBase?.ToString() ' 5
        ~~~~~~
BC32028: 'MyClass' must be followed by '.' and an identifier.
        MyClass?.ToString() ' 6
        ~~~~~~~
BC30487: Operator '?' is not defined for type 'S1'.
        MyClass?.ToString() ' 6
               ~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub MeMyBaseMyClass_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
     Dim c as New C1()

     System.Console.WriteLine(c.MeTest())
    End Sub

End Module

Class C1
    Function MeTest() As String
        return Me?.ToString() 
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
C1
]]>)

            verifier.VerifyIL("C1.MeTest",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldnull
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  callvirt   "Function Object.ToString() As String"
  IL_000b:  ret
}
]]>)

        End Sub

        <Fact(), WorkItem(7388, "https://github.com/dotnet/roslyn/issues/7388")>
        Public Sub ConstrainedToClass()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim v As New A(Of Object)
        System.Console.WriteLine(A(Of Object).test(v))
    End Sub

End Module

Public Class A(Of T As Class)
    Public ReadOnly Property Value As T
        Get
            Return CType(CObj(42), T)
        End Get
    End Property

    Public Shared Function test(val As A(Of T)) As T
        Return val?.Value
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
42
]]>)

            verifier.VerifyIL("A(Of T).test(A(Of T))",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    "T"
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  call       "Function A(Of T).get_Value() As T"
  IL_0013:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub CodeGen_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Test1(Nothing)
        System.Console.WriteLine("--")
        Test2(Nothing)
        System.Console.WriteLine("--")
        Test3(Nothing)
        System.Console.WriteLine("--")
        Test4(Nothing)
        System.Console.WriteLine("--")

        Test1(new S2())
        System.Console.WriteLine("--")
        Test2(new S2())
        System.Console.WriteLine("--")
        Test3(new S2())
        System.Console.WriteLine("--")
        Test4(new S2())
        System.Console.WriteLine("--")
    End Sub

    Sub Test1(x as S2?)
        x?.Ext1()
    End Sub

    Sub Test2(x as S2?)
        x?.Ext2()
    End Sub

    Sub Test3(x as S2?)
        x?.Ext3()
    End Sub

    Sub Test4(x as S2?)
        x?.Ext4()
    End Sub

    <Extension>
    Function Ext1(this as S2) As Object
        System.Console.WriteLine("Ext1")
    End Function 

    <Extension>
    Function Ext2(ByRef this as S2) As Object
        System.Console.WriteLine("Ext2")
    End Function 

    <Extension>
    Function Ext3(this as I2) As Object
        System.Console.WriteLine("Ext3")
    End Function 

    <Extension>
    Function Ext4(ByRef this as I2) As Object
        System.Console.WriteLine("Ext4")
    End Function 
End Module

Interface I2
End Interface

Structure S2
    Implements I2
End Structure

    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, {SystemCoreRef}, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
--
--
--
--
Ext1
--
Ext2
--
Ext3
--
Ext4
--
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function S2?.get_HasValue() As Boolean"
  IL_0007:  brfalse.s  IL_0016
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       "Function S2?.GetValueOrDefault() As S2"
  IL_0010:  call       "Function Module1.Ext1(S2) As Object"
  IL_0015:  pop
  IL_0016:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (S2 V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function S2?.get_HasValue() As Boolean"
  IL_0007:  brfalse.s  IL_0019
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       "Function S2?.GetValueOrDefault() As S2"
  IL_0010:  stloc.0
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       "Function Module1.Ext2(ByRef S2) As Object"
  IL_0018:  pop
  IL_0019:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function S2?.get_HasValue() As Boolean"
  IL_0007:  brfalse.s  IL_0020
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       "Function S2?.GetValueOrDefault() As S2"
  IL_0010:  box        "S2"
  IL_0015:  castclass  "I2"
  IL_001a:  call       "Function Module1.Ext3(I2) As Object"
  IL_001f:  pop
  IL_0020:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  1
  .locals init (I2 V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function S2?.get_HasValue() As Boolean"
  IL_0007:  brfalse.s  IL_0023
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       "Function S2?.GetValueOrDefault() As S2"
  IL_0010:  box        "S2"
  IL_0015:  castclass  "I2"
  IL_001a:  stloc.0
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       "Function Module1.Ext4(ByRef I2) As Object"
  IL_0022:  pop
  IL_0023:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Test1(Nothing)
        System.Console.WriteLine("--")
        Test2(Nothing)
        System.Console.WriteLine("--")
        Test3(Nothing)
        System.Console.WriteLine("--")
        Test4(Nothing)
        System.Console.WriteLine("--")

        Test1(new C1())
        System.Console.WriteLine("--")
        Test2(new C1())
        System.Console.WriteLine("--")
        Test3(new C1())
        System.Console.WriteLine("--")
        Test4(new C1())
        System.Console.WriteLine("--")
    End Sub

    Sub Test1(x as C1)
        x?.Ext1()
    End Sub

    Sub Test2(x as C1)
        x?.Ext2()
    End Sub

    Sub Test3(x as C1)
        x?.Ext3()
    End Sub

    Sub Test4(x as C1)
        x?.Ext4()
    End Sub

    <Extension>
    Function Ext1(this as C1) As Object
        System.Console.WriteLine("Ext1")
        return Nothing
    End Function 

    <Extension>
    Function Ext2(ByRef this as C1) As Object
        System.Console.WriteLine("Ext2")
        return Nothing
    End Function 

    <Extension>
    Function Ext3(this as I1) As Object
        System.Console.WriteLine("Ext3")
        return Nothing
    End Function 

    <Extension>
    Function Ext4(ByRef this as I1) As Object
        System.Console.WriteLine("Ext4")
        return Nothing
    End Function 
End Module

Interface I1
End Interface

Class C1
    Implements I1
End Class

    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, {SystemCoreRef}, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
--
--
--
--
Ext1
--
Ext2
--
Ext3
--
Ext4
--
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_000a
  IL_0003:  ldarg.0
  IL_0004:  call       "Function Module1.Ext1(C1) As Object"
  IL_0009:  pop
  IL_000a:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (C1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_000d
  IL_0003:  ldarg.0
  IL_0004:  stloc.0
  IL_0005:  ldloca.s   V_0
  IL_0007:  call       "Function Module1.Ext2(ByRef C1) As Object"
  IL_000c:  pop
  IL_000d:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_000a
  IL_0003:  ldarg.0
  IL_0004:  call       "Function Module1.Ext3(I1) As Object"
  IL_0009:  pop
  IL_000a:  ret
}]]>)

            verifier.VerifyIL("Module1.Test4",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (I1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_000d
  IL_0003:  ldarg.0
  IL_0004:  stloc.0
  IL_0005:  ldloca.s   V_0
  IL_0007:  call       "Function Module1.Ext4(ByRef I1) As Object"
  IL_000c:  pop
  IL_000d:  ret
}]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x as C1

        x = new C1()
        x.Ext2()
        System.Console.WriteLine(x)
        x = new C1()
        x.Ext4()
        System.Console.WriteLine(x)
        x = new C1()
        x?.Ext2()
        System.Console.WriteLine(x)
        x = new C1()
        x?.Ext4()
        System.Console.WriteLine(x)
    End Sub

    <Extension>
    Function Ext2(ByRef this as C1) As Object
        System.Console.WriteLine("Ext2")
        this = Nothing
        return Nothing
    End Function 

    <Extension>
    Function Ext4(ByRef this as I1) As Object
        System.Console.WriteLine("Ext4")
        this = Nothing
        return Nothing
    End Function 
End Module

Interface I1
End Interface

Class C1
    Implements I1
End Class

    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, {SystemCoreRef}, TestOptions.ReleaseExe, TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Ext2

Ext4

Ext2
C1
Ext4
C1
]]>)

        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CodeGen_04()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim x as new S1

        System.Console.WriteLine(x.F1)
        x.Test()
        System.Console.WriteLine(x.F1)
        Call (x).Test()
        System.Console.WriteLine(x.F1)
        Test1(x)
        System.Console.WriteLine(x.F1)
        Test2(x)
        System.Console.WriteLine(x.F1)

        Dim y = {x}

        Test3(y)
        System.Console.WriteLine(y(0).F1)
        Test4(y)
        System.Console.WriteLine(y(0).F1)

        Dim z As New C1(Of S1) With {.F2 = New S1 With {.F1=101}}
        System.Console.WriteLine(z.F2.F1)
        Test5(z)
        System.Console.WriteLine(z.F2.F1)
    End Sub

    Sub Test1(Of T As I1)(ByRef x as T)
        x?.Test()
    End Sub 

    Sub Test2(Of T As I1)(ByRef x as T)
        Call (x)?.Test()
    End Sub 

    Sub Test3(Of T As I1)(x() as T)
        x(0)?.Test()
    End Sub 

    Sub Test4(Of T As I1)(x() as T)
        Call (x(0))?.Test()
    End Sub 

    Sub Test5(Of T As I1)(x as C1(Of T))'BIND:"Sub Test5(Of T As I1)(x as C1(Of T))"
        With x.F2
            ?.Test()
        End With
    End Sub 
End Module

Interface I1
    Sub Test()
End Interface

Structure S1
    Implements I1

    Public F1 as Integer

    Sub Test() Implements I1.Test
        System.Console.WriteLine("Test")
        F1+=1
    End Sub
End Structure

Class C1(Of T)
    Public F2 as T
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
0
Test
1
Test
2
Test
3
Test
4
Test
5
Test
6
101
Test
102
]]>)

            VerifyOperationTreeForTest(Of MethodBlockSyntax)(compilation, "a.vb", expectedOperationTree:="
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Sub Test5(O ... End Sub')
  IWithOperation (OperationKind.None, Type: null) (Syntax: 'With x.F2 ... End With')
    Value: 
      IFieldReferenceOperation: C1(Of T).F2 As T (OperationKind.FieldReference, Type: T) (Syntax: 'x.F2')
        Instance Receiver: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1(Of T)) (Syntax: 'x')
    Body: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'With x.F2 ... End With')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '?.Test()')
          Expression: 
            IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: System.Void) (Syntax: '?.Test()')
              Operation: 
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsImplicit) (Syntax: 'x.F2')
              WhenNotNull: 
                IInvocationOperation (virtual Sub I1.Test()) (OperationKind.Invocation, Type: System.Void) (Syntax: '.Test()')
                  Instance Receiver: 
                    IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: T, IsImplicit) (Syntax: '?.Test()')
                  Arguments(0)
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null")

        End Sub

        <Fact()>
        Public Sub CodeGen_05()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
	    System.Console.WriteLine("---")
        Test1_1(Of S1)(Nothing)
	    System.Console.WriteLine("---")
        Test1_2(Of S1)(Nothing)
	    System.Console.WriteLine("---")
        Test1_3(Of S1)(Nothing)
	    System.Console.WriteLine("---")
        Test1_4(Of S1)(Nothing)
	    System.Console.WriteLine("---")
        Test1_5(Of S1)({Nothing})
	    System.Console.WriteLine("---")
        Test1_6(Of S1)(Nothing)
	    System.Console.WriteLine("---")

        Test2_1(Of S1)(Nothing)
	    System.Console.WriteLine("---")
        Test2_2(Of S1)(Nothing)
	    System.Console.WriteLine("---")
        Test2_3(Of S1)(Nothing)
	    System.Console.WriteLine("---")
        Test2_4(Of S1)(Nothing)
	    System.Console.WriteLine("---")
        Test2_5(Of S1)({Nothing})
	    System.Console.WriteLine("---")

        Test3_1(Of S1)(Nothing)
	    System.Console.WriteLine("---")
        Test3_2(Of S1)(Nothing)
	    System.Console.WriteLine("---")

        Test4_1(Of S1)(Nothing)
	    System.Console.WriteLine("---")
        Test4_2(Of S1)(Nothing)
	    System.Console.WriteLine("---")
        Test4_3(Of S1)({Nothing})
	    System.Console.WriteLine("---")

	    System.Console.WriteLine("***")

	    System.Console.WriteLine("---")
        Test1_1(Of C1)(Nothing)
	    System.Console.WriteLine("---")
        Test1_2(Of C1)(Nothing)
	    System.Console.WriteLine("---")
        Test1_3(Of C1)(Nothing)
	    System.Console.WriteLine("---")
        Test1_4(Of C1)(Nothing)
	    System.Console.WriteLine("---")
        Test1_5(Of C1)({Nothing})
	    System.Console.WriteLine("---")
        Test1_6(Of C1)(Nothing)
	    System.Console.WriteLine("---")

        Test2_1(Of C1)(Nothing)
	    System.Console.WriteLine("---")
        Test2_2(Of C1)(Nothing)
	    System.Console.WriteLine("---")
        Test2_3(Of C1)(Nothing)
	    System.Console.WriteLine("---")
        Test2_4(Of C1)(Nothing)
	    System.Console.WriteLine("---")
        Test2_5(Of C1)({Nothing})
	    System.Console.WriteLine("---")

        Test3_1(Of C1)(Nothing)
	    System.Console.WriteLine("---")
        Test3_2(Of C1)(Nothing)
	    System.Console.WriteLine("---")

        Test4_1(Of C1)(Nothing)
	    System.Console.WriteLine("---")
        Test4_2(Of C1)(Nothing)
	    System.Console.WriteLine("---")
        Test4_3(Of C1)({Nothing})
	    System.Console.WriteLine("---")

	    System.Console.WriteLine("***")

	    System.Console.WriteLine("---")
        Test1_1(Of C1)(New C1())
	    System.Console.WriteLine("---")
        Test1_2(Of C1)(New C1())
	    System.Console.WriteLine("---")
        Test1_3(Of C1)(New C1())
	    System.Console.WriteLine("---")
        Test1_4(Of C1)(New C1())
	    System.Console.WriteLine("---")
        Test1_5(Of C1)({New C1()})
	    System.Console.WriteLine("---")
        Test1_6(Of C1)(New C1())
	    System.Console.WriteLine("---")

        Test2_1(Of C1)(New C1())
	    System.Console.WriteLine("---")
        Test2_2(Of C1)(New C1())
	    System.Console.WriteLine("---")
        Test2_3(Of C1)(New C1())
	    System.Console.WriteLine("---")
        Test2_4(Of C1)(New C1())
	    System.Console.WriteLine("---")
        Test2_5(Of C1)({New C1()})
	    System.Console.WriteLine("---")

        Test3_1(Of C1)(New C1())
	    System.Console.WriteLine("---")
        Test3_2(Of C1)(New C1())
	    System.Console.WriteLine("---")

        Test4_1(Of C1)(New C1())
	    System.Console.WriteLine("---")
        Test4_2(Of C1)(New C1())
	    System.Console.WriteLine("---")
        Test4_3(Of C1)({New C1()})
	    System.Console.WriteLine("---")
    End Sub

    Function GetT(Of T)(x As T) As T
        Return x
    End Function

    Sub Test1_1(Of T As I1)(x As T)
	    x?.Ext1()
    End Sub

    Sub Test1_2(Of T As I1)(ByRef x As T)
	    x?.Ext1()
    End Sub

    Sub Test1_3(Of T As I1)(x As T)
	    Call (x)?.Ext1()
    End Sub

    Sub Test1_4(Of T As I1)(ByRef x As T)
	    Call (x)?.Ext1()
    End Sub

    Sub Test1_5(Of T As I1)(x() As T)
	    x(0)?.Ext1()
    End Sub

    Sub Test1_6(Of T As I1)(x As T)
	    GetT(x)?.Ext1()
    End Sub

    Sub Test2_1(Of T As I1)(x As T)
	    x?.Ext2()
    End Sub

    Sub Test2_2(Of T As I1)(ByRef x As T)
	    x?.Ext2()
    End Sub

    Sub Test2_3(Of T As I1)(x As T)
	    Call (x)?.Ext2()
    End Sub

    Sub Test2_4(Of T As I1)(ByRef x As T)
	    Call (x)?.Ext2()
    End Sub

    Sub Test2_5(Of T As I1)(x() As T)
	    x(0)?.Ext2()
    End Sub

    Sub Test3_1(Of T As I1)(x As T)
	    x?.Ext3()
    End Sub

    Sub Test3_2(Of T As I1)(ByRef x As T)
	    x?.Ext3()
    End Sub

    Sub Test4_1(Of T As I1)(x As T)
	    x?.Ext4()
    End Sub

    Sub Test4_2(Of T As I1)(ByRef x As T)
	    x?.Ext4()
    End Sub

    Sub Test4_3(Of T As I1)(x() As T)
	    x(0)?.Ext4()
    End Sub

    <Extension>
    Sub Ext1(Of T As I1)(this as T)
	    System.Console.WriteLine("Ext1 {0}", this.GetType())
    End Sub

    <Extension>
    Sub Ext2(Of T As I1)(ByRef this as T)
	    System.Console.WriteLine("Ext2 {0}", this.GetType())
    End Sub

    <Extension>
    Sub Ext3(this as I1)
	    System.Console.WriteLine("Ext3 {0}", this.GetType())
    End Sub

    <Extension>
    Sub Ext4(ByRef this as I1)
	    System.Console.WriteLine("Ext4 {0}", this.GetType())
    End Sub

End Module

Interface I1
End Interface

Structure S1
    Implements I1
End Structure

Class C1
    Implements I1
End Class

    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, {SystemCoreRef},
                                                                                         TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                                                                                         TestOptions.ReleaseExe.ParseOptions)


            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
Ext1 S1
---
Ext1 S1
---
Ext1 S1
---
Ext1 S1
---
Ext1 S1
---
Ext1 S1
---
Ext2 S1
---
Ext2 S1
---
Ext2 S1
---
Ext2 S1
---
Ext2 S1
---
Ext3 S1
---
Ext3 S1
---
Ext4 S1
---
Ext4 S1
---
Ext4 S1
---
***
---
---
---
---
---
---
---
---
---
---
---
---
---
---
---
---
---
***
---
Ext1 C1
---
Ext1 C1
---
Ext1 C1
---
Ext1 C1
---
Ext1 C1
---
Ext1 C1
---
Ext2 C1
---
Ext2 C1
---
Ext2 C1
---
Ext2 C1
---
Ext2 C1
---
Ext3 C1
---
Ext3 C1
---
Ext4 C1
---
Ext4 C1
---
Ext4 C1
---
]]>)

            verifier.VerifyIL("Module1.Test1_1",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_0014
  IL_0008:  ldarga.s   V_0
  IL_000a:  ldobj      "T"
  IL_000f:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_0014:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1_2",
            <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    "T"
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0023
  IL_0011:  ldobj      "T"
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        "T"
  IL_001f:  brtrue.s   IL_0023
  IL_0021:  pop
  IL_0022:  ret
  IL_0023:  ldobj      "T"
  IL_0028:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_002d:  ret
}]]>)

            verifier.VerifyIL("Module1.Test1_3",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_0014
  IL_0008:  ldarga.s   V_0
  IL_000a:  ldobj      "T"
  IL_000f:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_0014:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1_4",
            <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    "T"
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0023
  IL_0011:  ldobj      "T"
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        "T"
  IL_001f:  brtrue.s   IL_0023
  IL_0021:  pop
  IL_0022:  ret
  IL_0023:  ldobj      "T"
  IL_0028:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_002d:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1_5",
            <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  readonly.
  IL_0004:  ldelema    "T"
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    "T"
  IL_0011:  ldloc.0
  IL_0012:  box        "T"
  IL_0017:  brtrue.s   IL_002b
  IL_0019:  ldobj      "T"
  IL_001e:  stloc.0
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldloc.0
  IL_0022:  box        "T"
  IL_0027:  brtrue.s   IL_002b
  IL_0029:  pop
  IL_002a:  ret
  IL_002b:  ldobj      "T"
  IL_0030:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_0035:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1_6",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.GetT(Of T)(T) As T"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0013
  IL_0011:  pop
  IL_0012:  ret
  IL_0013:  ldobj      "T"
  IL_0018:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_001d:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2_1",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_0017
  IL_0008:  ldarga.s   V_0
  IL_000a:  ldobj      "T"
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "Sub Module1.Ext2(Of T)(ByRef T)"
  IL_0017:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2_2",
            <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (T V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    "T"
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0023
  IL_0011:  ldobj      "T"
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        "T"
  IL_001f:  brtrue.s   IL_0023
  IL_0021:  pop
  IL_0022:  ret
  IL_0023:  ldobj      "T"
  IL_0028:  stloc.1
  IL_0029:  ldloca.s   V_1
  IL_002b:  call       "Sub Module1.Ext2(Of T)(ByRef T)"
  IL_0030:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2_3",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_0017
  IL_0008:  ldarga.s   V_0
  IL_000a:  ldobj      "T"
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "Sub Module1.Ext2(Of T)(ByRef T)"
  IL_0017:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2_4",
            <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (T V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    "T"
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0023
  IL_0011:  ldobj      "T"
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        "T"
  IL_001f:  brtrue.s   IL_0023
  IL_0021:  pop
  IL_0022:  ret
  IL_0023:  ldobj      "T"
  IL_0028:  stloc.1
  IL_0029:  ldloca.s   V_1
  IL_002b:  call       "Sub Module1.Ext2(Of T)(ByRef T)"
  IL_0030:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2_5",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (T V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  readonly.
  IL_0004:  ldelema    "T"
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    "T"
  IL_0011:  ldloc.0
  IL_0012:  box        "T"
  IL_0017:  brtrue.s   IL_002b
  IL_0019:  ldobj      "T"
  IL_001e:  stloc.0
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldloc.0
  IL_0022:  box        "T"
  IL_0027:  brtrue.s   IL_002b
  IL_0029:  pop
  IL_002a:  ret
  IL_002b:  ldobj      "T"
  IL_0030:  stloc.1
  IL_0031:  ldloca.s   V_1
  IL_0033:  call       "Sub Module1.Ext2(Of T)(ByRef T)"
  IL_0038:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3_1",
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_001e
  IL_0008:  ldarga.s   V_0
  IL_000a:  ldobj      "T"
  IL_000f:  box        "T"
  IL_0014:  castclass  "I1"
  IL_0019:  call       "Sub Module1.Ext3(I1)"
  IL_001e:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3_2",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    "T"
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0023
  IL_0011:  ldobj      "T"
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        "T"
  IL_001f:  brtrue.s   IL_0023
  IL_0021:  pop
  IL_0022:  ret
  IL_0023:  ldobj      "T"
  IL_0028:  box        "T"
  IL_002d:  castclass  "I1"
  IL_0032:  call       "Sub Module1.Ext3(I1)"
  IL_0037:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_1",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (I1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_0021
  IL_0008:  ldarga.s   V_0
  IL_000a:  ldobj      "T"
  IL_000f:  box        "T"
  IL_0014:  castclass  "I1"
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  call       "Sub Module1.Ext4(ByRef I1)"
  IL_0021:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_2",
            <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (T V_0,
                I1 V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    "T"
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0023
  IL_0011:  ldobj      "T"
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        "T"
  IL_001f:  brtrue.s   IL_0023
  IL_0021:  pop
  IL_0022:  ret
  IL_0023:  ldobj      "T"
  IL_0028:  box        "T"
  IL_002d:  castclass  "I1"
  IL_0032:  stloc.1
  IL_0033:  ldloca.s   V_1
  IL_0035:  call       "Sub Module1.Ext4(ByRef I1)"
  IL_003a:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_3",
            <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  2
  .locals init (T V_0,
                I1 V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  readonly.
  IL_0004:  ldelema    "T"
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    "T"
  IL_0011:  ldloc.0
  IL_0012:  box        "T"
  IL_0017:  brtrue.s   IL_002b
  IL_0019:  ldobj      "T"
  IL_001e:  stloc.0
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldloc.0
  IL_0022:  box        "T"
  IL_0027:  brtrue.s   IL_002b
  IL_0029:  pop
  IL_002a:  ret
  IL_002b:  ldobj      "T"
  IL_0030:  box        "T"
  IL_0035:  castclass  "I1"
  IL_003a:  stloc.1
  IL_003b:  ldloca.s   V_1
  IL_003d:  call       "Sub Module1.Ext4(ByRef I1)"
  IL_0042:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_06()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()

	    System.Console.WriteLine("---------")
        Dim x as S1

        x = new S1 With {.F1=1}
        x.Ext2()
        x.Print()

        x = new S1 With {.F1=2}
        x.Ext4()
        x.Print()

        x = new S1 With {.F1=3}
        call (x).Ext2()
        x.Print()

        x = new S1 With {.F1=4}
        call (x).Ext4()
        x.Print()

	    System.Console.WriteLine("---------")

	    System.Console.WriteLine("---------")
        Dim y as C1

        y = new C1()
        y.Ext2()
        y.Print()

        y = new C1()
        y.Ext4()
        y.Print()

        y = new C1()
        call (y).Ext2()
        y.Print()

        y = new C1()
        Call (y).Ext4()
        y.Print()
	    System.Console.WriteLine("---------")


	    System.Console.WriteLine("---------")
        Test1(new S1 With {.F1=5})
        Test2(new S1 With {.F1=6})
        Test3(new S1 With {.F1=7})
        Test4(new S1 With {.F1=8})
        Test5(new S1 With {.F1=9})
        Test6(new S1 With {.F1=10})
	    System.Console.WriteLine("---------")

	    System.Console.WriteLine("---------")
        Test1(new C1())
        Test2(new C1())
        Test3(new C1())
        Test4(new C1())
        Test5(new C1())
        Test6(new C1())
	    System.Console.WriteLine("---------")
    End Sub

    Sub Test1(Of T As I1)(x As T)
       x.Print()
	   x?.Ext2()
       x.Print()
    End Sub

    Sub Test2(Of T As I1)(x As T)
       x.Print()
	   Call (x)?.Ext2()
       x.Print()
    End Sub

    Sub Test3(Of T As I1)(x As T)
       x.Print()
	   x?.Ext4()
       x.Print()
    End Sub

    Sub Test4(Of T As I1)(x As T)
       x.Print()
	   Call (x)?.Ext4()
       x.Print()
    End Sub

    Sub Test5(Of T As I1)(ByRef x As T)
       x.Print()
	   x?.Ext2()
       x.Print()
    End Sub

    Sub Test6(Of T As I1)(ByRef x As T)
       x.Print()
	   x?.Ext4()
       x.Print()
    End Sub

    <Extension>
    Sub Ext2(Of T As I1)(ByRef this as T)
	    System.Console.WriteLine("Ext2")
        this = Nothing
    End Sub

    <Extension>
    Sub Ext4(ByRef this as I1)
	    System.Console.WriteLine("Ext4")
        this = Nothing
    End Sub

    <Extension>
    Sub Print(this as I1)
        if this Is Nothing
	        System.Console.WriteLine("Null")
        Else
            Dim c1 = TryCast(this, C1)
            if c1 IsNot Nothing
    	        System.Console.WriteLine("C1")
            Else
    	        System.Console.WriteLine(DirectCast(this, S1).F1)
            End If
        End If
    End Sub
End Module

Interface I1
    Sub Test()
End Interface

Structure S1
    Implements I1

    Public F1 as Integer

    Sub Test() Implements I1.Test
        System.Console.WriteLine("Test")
        F1+=1
    End Sub
End Structure

Class C1
    Implements I1

    Sub Test() Implements I1.Test
    End Sub
End Class

    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, {SystemCoreRef},
                                                                                         TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                                                                                         TestOptions.ReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC41999: Implicit conversion from 'I1' to 'S1' in copying the value of 'ByRef' parameter 'this' back to the matching argument.
        x.Ext4()
        ~
BC41999: Implicit conversion from 'I1' to 'C1' in copying the value of 'ByRef' parameter 'this' back to the matching argument.
        y.Ext4()
        ~
]]></expected>)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
 ---------
Ext2
0
Ext4
0
Ext2
3
Ext4
4
---------
---------
Ext2
Null
Ext4
Null
Ext2
C1
Ext4
C1
---------
---------
5
Ext2
5
6
Ext2
6
7
Ext4
7
8
Ext4
8
9
Ext2
9
10
Ext4
10
---------
---------
C1
Ext2
C1
C1
Ext2
C1
C1
Ext4
C1
C1
Ext4
C1
C1
Ext2
C1
C1
Ext4
C1
---------
]]>)
        End Sub


        <Fact()>
        Public Sub CodeGen_07()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks

Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim x = Test(Of S1)(Nothing)
        Task.WaitAll(x)
        System.Console.WriteLine(If(x.Result, "Null"))

        System.Console.WriteLine("---")
        Dim y = Test(Of C1)(Nothing)
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))

        System.Console.WriteLine("---")
        y = Test(Of C1)(New C1())
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))
    End Sub

    Async Function Test(Of T As I1)(x As T) As Task(Of Object)
        Return x?.CallAsync(Await PassAsync())
    End Function

    Async Function PassAsync() As Task(Of Integer)
        Return 1
    End Function
End Module

Interface I1
    Function CallAsync(x As Integer) As Object
End Interface

Structure S1
    Implements I1

    Public Function CallAsync(x As Integer) As Object Implements I1.CallAsync
        System.Console.WriteLine("S1.CallAsync")
        Return 1
    End Function
End Structure

Class C1
    Implements I1

    Public Function CallAsync(x As Integer) As Object Implements I1.CallAsync
        System.Console.WriteLine("C1.CallAsync")
        Return 2
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
S1.CallAsync
1
---
Null
---
C1.CallAsync
2
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_08()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim s1 = {New S1 With {.F1 = 1}}
        System.Console.WriteLine(s1(0).F1)
        Dim x = Test1(Of S1)(s1)
        Task.WaitAll(x)
        System.Console.WriteLine(If(x.Result, "Null"))
        System.Console.WriteLine("{0}", s1(0).F1)

        System.Console.WriteLine("---")
        Dim y = Test1(Of C1)({Nothing})
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))

        System.Console.WriteLine("---")
        y = Test1(Of C1)({New C1()})
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))

        System.Console.WriteLine("---")
        s1(0) = New S1 With {.F1 = 3}
        System.Console.WriteLine(s1(0).F1)
        Dim z = Test2(Of S1)(s1)
        Task.WaitAll(z)
        System.Console.WriteLine(If(z.Result, "Null"))
        System.Console.WriteLine(s1(0).F1)

        System.Console.WriteLine("---")
        z = Test3(Of C1)({Nothing})
        Task.WaitAll(z)
        System.Console.WriteLine(If(z.Result, "Null"))

        System.Console.WriteLine("---")
        Dim c1 = {new C1()}
        System.Console.WriteLine(c1(0))
        z = Test3(Of C1)(c1)
        Task.WaitAll(z)
        System.Console.WriteLine(If(z.Result, "Null"))
        System.Console.WriteLine(c1(0))
    End Sub

    Async Function Test1(Of T As I1)(x() As T) As Task(Of Object)
        Return x(0)?.CallAsync(Await PassAsync())
    End Function

    Async Function Test2(Of T As I1)(x() As T) As Task(Of Integer?)
        Return x(0)?.CallAsyncExt1(Await PassAsync())
    End Function

    Async Function Test3(Of T As I1)(x() As T) As Task(Of Integer?)
        Return x(0)?.CallAsyncExt2(Await PassAsync())
    End Function

    Async Function PassAsync() As Task(Of Integer)
        Return 1
    End Function

    <Extension>    
    Function CallAsyncExt1(Of T)(ByRef x As T, y as Integer) As Integer
        System.Console.WriteLine("CallAsyncExt1")
        x = Nothing
        return 100
    End Function

    <Extension>    
    Function CallAsyncExt2(Of T)(ByRef x As T, y as Integer) As Integer?
        System.Console.WriteLine("CallAsyncExt2")
        x = Nothing
        return 101
    End Function
End Module

Interface I1
    Function CallAsync(x As Integer) As Object
End Interface

Structure S1
    Implements I1

    Public F1 As Integer

    Public Function CallAsync(x As Integer) As Object Implements I1.CallAsync
        System.Console.WriteLine("S1.CallAsync")
        F1+=1
        Return 1
    End Function
End Structure

Class C1
    Implements I1

    Public Function CallAsync(x As Integer) As Object Implements I1.CallAsync
        System.Console.WriteLine("C1.CallAsync")
        Return 2
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
1
S1.CallAsync
1
2
---
Null
---
C1.CallAsync
2
---
3
CallAsyncExt1
100
3
---
Null
---
C1
CallAsyncExt2
101
C1
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_09()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
    End Sub

    Async Function Test1(Of T As I1)(x() As T) As Task(Of Short?)
        Return x(0)?.CallAsync()
    End Function
End Module

Interface I1
    Function CallAsync() As Short
End Interface
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation)

            verifier.VerifyIL("Module1.VB$StateMachine_1_Test1(Of SM$T).MoveNext",
            <![CDATA[
{
  // Code size      143 (0x8f)
  .maxstack  3
  .locals init (Short? V_0,
                Integer V_1,
                SM$T V_2,
                Short? V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Test1(Of SM$T).$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldarg.0
    IL_0008:  ldfld      "Module1.VB$StateMachine_1_Test1(Of SM$T).$VB$Local_x As SM$T()"
    IL_000d:  ldc.i4.0
    IL_000e:  readonly.
    IL_0010:  ldelema    "SM$T"
    IL_0015:  ldloca.s   V_2
    IL_0017:  initobj    "SM$T"
    IL_001d:  ldloc.2
    IL_001e:  box        "SM$T"
    IL_0023:  brtrue.s   IL_0041
    IL_0025:  ldobj      "SM$T"
    IL_002a:  stloc.2
    IL_002b:  ldloca.s   V_2
    IL_002d:  ldloc.2
    IL_002e:  box        "SM$T"
    IL_0033:  brtrue.s   IL_0041
    IL_0035:  pop
    IL_0036:  ldloca.s   V_3
    IL_0038:  initobj    "Short?"
    IL_003e:  ldloc.3
    IL_003f:  br.s       IL_0051
    IL_0041:  constrained. "SM$T"
    IL_0047:  callvirt   "Function I1.CallAsync() As Short"
    IL_004c:  newobj     "Sub Short?..ctor(Short)"
    IL_0051:  stloc.0
    IL_0052:  leave.s    IL_0078
  }
  catch System.Exception
  {
    IL_0054:  dup
    IL_0055:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_005a:  stloc.s    V_4
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.s   -2
    IL_005f:  stfld      "Module1.VB$StateMachine_1_Test1(Of SM$T).$State As Integer"
    IL_0064:  ldarg.0
    IL_0065:  ldflda     "Module1.VB$StateMachine_1_Test1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Short?)"
    IL_006a:  ldloc.s    V_4
    IL_006c:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Short?).SetException(System.Exception)"
    IL_0071:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0076:  leave.s    IL_008e
  }
  IL_0078:  ldarg.0
  IL_0079:  ldc.i4.s   -2
  IL_007b:  dup
  IL_007c:  stloc.1
  IL_007d:  stfld      "Module1.VB$StateMachine_1_Test1(Of SM$T).$State As Integer"
  IL_0082:  ldarg.0
  IL_0083:  ldflda     "Module1.VB$StateMachine_1_Test1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Short?)"
  IL_0088:  ldloc.0
  IL_0089:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Short?).SetResult(Short?)"
  IL_008e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_10()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim x() As Derived = {New Derived()}
        System.Console.WriteLine("---")
        Test3(Of Base)(x)
        System.Console.WriteLine("---")
        x(0) = Nothing
        Test3(Of Base)(x)
        System.Console.WriteLine("---")
    End Sub

    Sub Test3(Of T As I1)(x() As T)
        x(0)?.Test()
    End Sub
End Module

Interface I1
    Sub Test()
End Interface

Class Base
    Implements I1

    Public Sub Test() Implements I1.Test
        System.Console.WriteLine("Test")
    End Sub
End Class

Class Derived
    Inherits Base
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
Test
---
---
]]>)

        End Sub

        <Fact()>
        Public Sub CodeGen_11()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Threading.Tasks

Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim s1 = {New S1()}
        Dim x = Test1(Of S1)(s1)
        Task.WaitAll(x)

        System.Console.WriteLine("---")
        Dim y = Test1(Of C1)({Nothing})
        Task.WaitAll(y)

        System.Console.WriteLine("---")
        y = Test1(Of C1)({New C1()})
        Task.WaitAll(y)

        System.Console.WriteLine("---")
    End Sub

    Async Function Test1(Of T As I1)(x() As T) As Task(Of Object)
        x(0)?.CallAsync(Await PassAsync())
        return Nothing
    End Function

    Async Function PassAsync() As Task(Of Integer)
        Return 1
    End Function
End Module

Interface I1
    Sub CallAsync(x As Integer)
End Interface

Structure S1
    Implements I1

    Sub CallAsync(x As Integer) Implements I1.CallAsync
        System.Console.WriteLine("S1.CallAsync")
    End Sub
End Structure

Class C1
    Implements I1

    Public Sub CallAsync(x As Integer) Implements I1.CallAsync
        System.Console.WriteLine("C1.CallAsync")
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
S1.CallAsync
---
---
C1.CallAsync
---
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_12()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections.Generic

Module Module1

    Sub Main()
        Dim x = {Nothing, New C1(), Nothing, New C1()}
        For Each y In Test(Of C1)(x)
            System.Console.WriteLine(If(CObj(y), "Null"))
        Next
    End Sub

    Function GetT(Of T)(x As T) As T
        Return x
    End Function

    Iterator Function Test(Of T As I1)(x() As T) As IEnumerable(Of Integer?)
        Yield 0
        GetT(x(0))?.Test2()
        Yield 1
        x(1)?.Test2()
        Yield 2
        Yield x(2)?.Test1()
        Yield 3
        Yield GetT(x(3))?.Test1()
        Yield 4
    End Function
End Module

Interface I1
    Function Test1() As Integer
    Sub Test2()
End Interface

Class C1
    Implements I1

    Public Sub Test2() Implements I1.Test2
        System.Console.WriteLine("Test2")
    End Sub

    Public Function Test1() As Integer Implements I1.Test1
        System.Console.WriteLine("Test1")
        Return 123
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
0
1
Test2
2
Null
3
Test1
123
4
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_13()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks

Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim x = Test(Of S1)(New S1(10))
        Task.WaitAll(x)
        System.Console.WriteLine(If(x.Result, "Null"))

        System.Console.WriteLine("---")
        Dim y = Test(Of C1)(Nothing)
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))

        System.Console.WriteLine("---")
        y = Test(Of C1)(New C1(20))
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))
    End Sub

    Async Function Test(Of T As I1)(x As T) As Task(Of Object)
        Dim y = x
        Return y?.CallAsync(Await PassAsync())
    End Function

    Async Function PassAsync() As Task(Of Integer)
        Return 1
    End Function
End Module

Interface I1
    Function CallAsync(x As Integer) As Object
End Interface

Structure S1
    Implements I1

    Private m_F1 As Integer

    Sub New(f1 As Integer)
        m_F1 = f1
    End Sub

    Public Function CallAsync(x As Integer) As Object Implements I1.CallAsync
        System.Console.WriteLine("S1.CallAsync {0}", m_F1)
        Return 1
    End Function
End Structure

Class C1
    Implements I1

    Private m_F1 As Integer

    Sub New(f1 As Integer)
        m_F1 = f1
    End Sub

    Public Function CallAsync(x As Integer) As Object Implements I1.CallAsync
        System.Console.WriteLine("C1.CallAsync {0}", m_F1)
        Return 2
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
S1.CallAsync 10
1
---
Null
---
C1.CallAsync 20
2
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_14()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Test1()
        System.Console.WriteLine("---")
        Test2()
        System.Console.WriteLine("---")
        Test3()
        System.Console.WriteLine("---")
    End Sub

    Sub Test1()
        System.Console.WriteLine(CStr(Nothing)?.ToString().Length)
    End Sub

    Sub Test2()
        System.Console.WriteLine("abc"?.ToString().Length)
    End Sub

    Sub Test3()
        CStr(Nothing)?.ToString()
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---

---
3
---
---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       "Sub System.Console.WriteLine(Object)"
  IL_0006:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  1
  IL_0000:  ldstr      "abc"
  IL_0005:  callvirt   "Function String.ToString() As String"
  IL_000a:  callvirt   "Function String.get_Length() As Integer"
  IL_000f:  box        "Integer"
  IL_0014:  call       "Sub System.Console.WriteLine(Object)"
  IL_0019:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_15()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Test1()
        System.Console.WriteLine("---")
        Test2()
        System.Console.WriteLine("---")
        Test3()
        System.Console.WriteLine("---")
    End Sub

    Sub Test1()
        System.Console.WriteLine(New Integer?()?.ToString())
    End Sub

    Sub Test2()
        System.Console.WriteLine(New Integer?(3)?.ToString())
    End Sub

    Sub Test3()
        Call New Integer?()?.ToString()
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---

---
3
---
---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       "Sub System.Console.WriteLine(String)"
  IL_0006:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       "Function Integer.ToString() As String"
  IL_0009:  call       "Sub System.Console.WriteLine(String)"
  IL_000e:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_16()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks

Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim x = Test(New S1())
        Task.WaitAll(x)
        System.Console.WriteLine(If(x.Result, "Null"))

        System.Console.WriteLine("---")
        Dim y = Test(Nothing)
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))

        System.Console.WriteLine("---")
    End Sub

    Async Function Test(x As S1?) As Task(Of Object)
        Return x?.CallAsync(Await PassAsync())
    End Function

    Async Function PassAsync() As Task(Of Integer)
        Return 1
    End Function
End Module

Structure S1
    Public Function CallAsync(x As Integer) As Object
        System.Console.WriteLine("S1.CallAsync")
        Return 1
    End Function
End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
S1.CallAsync
1
---
Null
---
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_17()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks

Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim x = Test(New C1())
        Task.WaitAll(x)
        System.Console.WriteLine(If(x.Result, "Null"))

        System.Console.WriteLine("---")
        Dim y = Test(Nothing)
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))

        System.Console.WriteLine("---")
    End Sub

    Async Function Test(x As C1) As Task(Of Object)
        Return x?.CallAsync(Await PassAsync())
    End Function

    Async Function PassAsync() As Task(Of Integer)
        Return 1
    End Function
End Module

Class C1
    Public Function CallAsync(x As Integer) As Object
        System.Console.WriteLine("C1.CallAsync")
        Return 1
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
C1.CallAsync
1
---
Null
---
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_18()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks

Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim x = Test(New C1())
        Task.WaitAll(x)
        System.Console.WriteLine(If(x.Result, "Null"))

        System.Console.WriteLine("---")
        Dim y = Test(Nothing)
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))

        System.Console.WriteLine("---")
    End Sub

    Async Function Test(x As C1) As Task(Of Object)
        Return (Await GetAsync(x))?.CallAsync(Await PassAsync())
    End Function

    Async Function PassAsync() As Task(Of Integer)
        Return 1
    End Function

    Async Function GetAsync(x As C1) As Task(Of C1)
        Return x
    End Function
End Module

Class C1
    Public Function CallAsync(x As Integer) As Object
        System.Console.WriteLine("C1.CallAsync")
        Return 1
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
C1.CallAsync
1
---
Null
---
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_19()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks

Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim x = Test(New C1())
        Task.WaitAll(x)
        System.Console.WriteLine(If(x.Result, "Null"))

        System.Console.WriteLine("---")
        Dim y = Test(Nothing)
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))

        System.Console.WriteLine("---")
    End Sub

    Async Function Test(x As C1) As Task(Of Object)
        Return (Await GetAsync(x))?.CallAsync(1)
    End Function

    Async Function GetAsync(x As C1) As Task(Of C1)
        Return x
    End Function
End Module

Class C1
    Public Function CallAsync(x As Integer) As Object
        System.Console.WriteLine("C1.CallAsync")
        Return 1
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
C1.CallAsync
1
---
Null
---
]]>)
        End Sub

        <Fact()>
        Public Sub CodeGen_20()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        
    End Sub

    Sub Test1()
        Dim x = "abc"
        Dim y = Sub()
                    x = Nothing
                End Sub
                    
        x?.ToString()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (Module1._Closure$__1-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub Module1._Closure$__1-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      "abc"
  IL_000c:  stfld      "Module1._Closure$__1-0.$VB$Local_x As String"
  IL_0011:  ldloc.0
  IL_0012:  ldfld      "Module1._Closure$__1-0.$VB$Local_x As String"
  IL_0017:  dup
  IL_0018:  brtrue.s   IL_001c
  IL_001a:  pop
  IL_001b:  ret
  IL_001c:  callvirt   "Function String.ToString() As String"
  IL_0021:  pop
  IL_0022:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub CodeGen_21()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        
    End Sub

    Sub Test1()
        Dim x = "abc"
        Dim z = "abc"
        Dim y = Sub()
                    z = Nothing
                End Sub
                    
        x?.ToString()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  .locals init (String V_0) //x
  IL_0000:  newobj     "Sub Module1._Closure$__1-0..ctor()"
  IL_0005:  ldstr      "abc"
  IL_000a:  stloc.0
  IL_000b:  dup
  IL_000c:  ldstr      "abc"
  IL_0011:  stfld      "Module1._Closure$__1-0.$VB$Local_z As String"
  IL_0016:  pop
  IL_0017:  ldloc.0
  IL_0018:  brfalse.s  IL_0021
  IL_001a:  ldloc.0
  IL_001b:  callvirt   "Function String.ToString() As String"
  IL_0020:  pop
  IL_0021:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub CodeGen_22()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim s1 = {New S1 With {.F1 = 1}}
        System.Console.WriteLine(s1(0).F1)
        Dim x = Test1(Of S1)(s1)
        Task.WaitAll(x)
        System.Console.WriteLine(If(x.Result, "Null"))
        System.Console.WriteLine("{0}", s1(0).F1)

        System.Console.WriteLine("---")
        Dim y = Test1(Of C1)({Nothing})
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))

        System.Console.WriteLine("---")
        y = Test1(Of C1)({New C1()})
        Task.WaitAll(y)
        System.Console.WriteLine(If(y.Result, "Null"))

        System.Console.WriteLine("---")
        s1(0) = New S1 With {.F1 = 3}
        System.Console.WriteLine(s1(0).F1)
        Dim z = Test2(Of S1)(s1)
        Task.WaitAll(z)
        System.Console.WriteLine(If(z.Result, "Null"))
        System.Console.WriteLine(s1(0).F1)

        System.Console.WriteLine("---")
        z = Test3(Of C1)({Nothing})
        Task.WaitAll(z)
        System.Console.WriteLine(If(z.Result, "Null"))

        System.Console.WriteLine("---")
        Dim c1 = {new C1()}
        System.Console.WriteLine(c1(0))
        z = Test3(Of C1)(c1)
        Task.WaitAll(z)
        System.Console.WriteLine(If(z.Result, "Null"))
        System.Console.WriteLine(c1(0))
    End Sub

    Async Function Test1(Of T As I1)(x() As T) As Task(Of Object)
        Return (x(0))?.CallAsync(Await PassAsync())
    End Function

    Async Function Test2(Of T As I1)(x() As T) As Task(Of Integer?)
        Return (x(0))?.CallAsyncExt1(Await PassAsync())
    End Function

    Async Function Test3(Of T As I1)(x() As T) As Task(Of Integer?)
        Return (x(0))?.CallAsyncExt2(Await PassAsync())
    End Function

    Async Function PassAsync() As Task(Of Integer)
        Return 1
    End Function

    <Extension>    
    Function CallAsyncExt1(Of T)(ByRef x As T, y as Integer) As Integer
        System.Console.WriteLine("CallAsyncExt1")
        x = Nothing
        return 100
    End Function

    <Extension>    
    Function CallAsyncExt2(Of T)(ByRef x As T, y as Integer) As Integer?
        System.Console.WriteLine("CallAsyncExt2")
        x = Nothing
        return 101
    End Function
End Module

Interface I1
    Function CallAsync(x As Integer) As Object
End Interface

Structure S1
    Implements I1

    Public F1 As Integer

    Public Function CallAsync(x As Integer) As Object Implements I1.CallAsync
        System.Console.WriteLine("S1.CallAsync")
        F1+=1
        Return 1
    End Function
End Structure

Class C1
    Implements I1

    Public Function CallAsync(x As Integer) As Object Implements I1.CallAsync
        System.Console.WriteLine("C1.CallAsync")
        Return 2
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
1
S1.CallAsync
1
2
---
Null
---
C1.CallAsync
2
---
3
CallAsyncExt1
100
3
---
Null
---
C1
CallAsyncExt2
101
C1
]]>)
        End Sub

        <Fact>
        <WorkItem(3519, "https://github.com/dotnet/roslyn/issues/35319")>
        Public Sub CodeGen_ConditionalAccessUnconstrainedTField()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Class C(Of T)
    Public Sub New(t As T)
        field = t
    End Sub

    Public Sub New()
    End Sub

    Private field As T

    Public Sub Print()
        Console.WriteLine(field?.ToString())
        Console.WriteLine(field)
    End Sub
End Class

Public Structure S
    Private a As Integer

    Public Overrides Function ToString() As String
        Dim result = a.ToString()
        a = a + 1
        Return result
    End Function
End Structure

Module Program
    Sub Main()
        Call New C(Of S)().Print()
        Call New C(Of S?)().Print()
        Call New C(Of S?)(New S()).Print()
        Call New C(Of String)("hello").Print()
        Call New C(Of String)().Print()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="0
1


0
0
hello
hello")

            c.VerifyIL("C(Of T).Print()",
            <![CDATA[
{
  // Code size       75 (0x4b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "C(Of T).field As T"
  IL_0006:  ldloca.s   V_0
  IL_0008:  initobj    "T"
  IL_000e:  ldloc.0
  IL_000f:  box        "T"
  IL_0014:  brtrue.s   IL_002a
  IL_0016:  ldobj      "T"
  IL_001b:  stloc.0
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldloc.0
  IL_001f:  box        "T"
  IL_0024:  brtrue.s   IL_002a
  IL_0026:  pop
  IL_0027:  ldnull
  IL_0028:  br.s       IL_0035
  IL_002a:  constrained. "T"
  IL_0030:  callvirt   "Function Object.ToString() As String"
  IL_0035:  call       "Sub System.Console.WriteLine(String)"
  IL_003a:  ldarg.0
  IL_003b:  ldfld      "C(Of T).field As T"
  IL_0040:  box        "T"
  IL_0045:  call       "Sub System.Console.WriteLine(Object)"
  IL_004a:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(3519, "https://github.com/dotnet/roslyn/issues/35319")>
        Public Sub CodeGen_ConditionalAccessReadonlyUnconstrainedTField()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Class C(Of T)
    Public Sub New(ByVal t As T)
        field = t
    End Sub

    Public Sub New()
    End Sub

    ReadOnly field As T

    Public Sub Print()
        Console.WriteLine(field?.ToString())
        Console.WriteLine(field)
    End Sub
End Class

Public Structure S
    Private a As Integer

    Public Overrides Function ToString() As String
        Return Math.Min(System.Threading.Interlocked.Increment(a), a - 1).ToString()
    End Function
End Structure

Module Program
    Sub Main()
		Call New C(Of S)().Print()
		Call New C(Of S?)().Print()
		Call New C(Of S?)(New S()).Print()
		Call New C(Of String)("hello").Print()
		Call New C(Of String)().Print()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="0
0


0
0
hello
hello")

            c.VerifyIL("C(Of T).Print()",
            <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C(Of T).field As T"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0015
  IL_0011:  pop
  IL_0012:  ldnull
  IL_0013:  br.s       IL_0020
  IL_0015:  constrained. "T"
  IL_001b:  callvirt   "Function Object.ToString() As String"
  IL_0020:  call       "Sub System.Console.WriteLine(String)"
  IL_0025:  ldarg.0
  IL_0026:  ldfld      "C(Of T).field As T"
  IL_002b:  box        "T"
  IL_0030:  call       "Sub System.Console.WriteLine(Object)"
  IL_0035:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(3519, "https://github.com/dotnet/roslyn/issues/35319")>
        Public Sub CodeGen_ConditionalAccessUnconstrainedTLocal()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Class C(Of T)
    Public Sub New(ByVal t As T)
        field = t
    End Sub

    Public Sub New()
    End Sub

    Private field As T

    Public Sub Print()
        Dim temp = field
        Console.WriteLine(temp?.ToString())
        Console.WriteLine(temp)
    End Sub
End Class

Public Structure S
    Private a As Integer

    Public Overrides Function ToString() As String
        Return Math.Min(System.Threading.Interlocked.Increment(a), a - 1).ToString()
    End Function
End Structure

Module Program
	Sub Main()
		Call New C(Of S)().Print()
		Call New C(Of S?)().Print()
		Call New C(Of S?)(New S()).Print()
		Call New C(Of String)("hello").Print()
		Call New C(Of String)().Print()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="0
1


0
1
hello
hello")

            c.VerifyIL("C(Of T).Print()",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  1
  .locals init (T V_0) //temp
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C(Of T).field As T"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  box        "T"
  IL_000d:  brtrue.s   IL_0012
  IL_000f:  ldnull
  IL_0010:  br.s       IL_001f
  IL_0012:  ldloca.s   V_0
  IL_0014:  constrained. "T"
  IL_001a:  callvirt   "Function Object.ToString() As String"
  IL_001f:  call       "Sub System.Console.WriteLine(String)"
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(3519, "https://github.com/dotnet/roslyn/issues/35319")>
        Public Sub CodeGen_ConditionalAccessUnconstrainedTTemp()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Class C(Of T)
    Public Sub New(ByVal t As T)
        field = t
    End Sub

    Public Sub New()
    End Sub

    Private field As T

    Private Function M() As T
        Return field
    End Function

    Public Sub Print()
        Console.WriteLine(M()?.ToString())
    End Sub
End Class

Module Program
    Sub Main()
        Call New C(Of Integer)().Print()
        Call New C(Of Integer?)().Print()
        Call New C(Of Integer?)(0).Print()
        Call New C(Of String)("hello").Print()
        Call New C(Of String)().Print()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="0

0
hello
")

            c.VerifyIL("C(Of T).Print()",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function C(Of T).M() As T"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0015
  IL_0011:  pop
  IL_0012:  ldnull
  IL_0013:  br.s       IL_0020
  IL_0015:  constrained. "T"
  IL_001b:  callvirt   "Function Object.ToString() As String"
  IL_0020:  call       "Sub System.Console.WriteLine(String)"
  IL_0025:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineNullableIsTrue_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim c1 As New C1(True)
        Test1(c1)
        Test2(c1)

        c1 = New C1(False)
        Test1(c1)
        Test2(c1)

        Test1(Nothing)
        Test2(Nothing)
    End Sub

    Sub Test1(x as C1)
        if x?.M1()
            System.Console.WriteLine("Test1.Then")
        Else
            System.Console.WriteLine("Test1.Else")
        End If
    End Sub

    Sub Test2(x as C1)
        if x?.M2()
            System.Console.WriteLine("Test2.Then")
        Else
            System.Console.WriteLine("Test2.Else")
        End If
    End Sub
End Module

Class C1
    Private m_Boolean As Boolean

    Sub New (x as Boolean)
        m_Boolean = x
    End Sub

    Function M1() As Boolean
        return m_Boolean
    End Function

    Function M2() As Boolean?
        return m_Boolean
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1.Then
Test2.Then
Test1.Else
Test2.Else
Test1.Else
Test2.Else
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.0
  IL_0004:  br.s       IL_000c
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.M1() As Boolean"
  IL_000c:  brfalse.s  IL_0019
  IL_000e:  ldstr      "Test1.Then"
  IL_0013:  call       "Sub System.Console.WriteLine(String)"
  IL_0018:  ret
  IL_0019:  ldstr      "Test1.Else"
  IL_001e:  call       "Sub System.Console.WriteLine(String)"
  IL_0023:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  1
  .locals init (Boolean? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.0
  IL_0004:  br.s       IL_0014
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.M2() As Boolean?"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0014:  brfalse.s  IL_0021
  IL_0016:  ldstr      "Test2.Then"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ret
  IL_0021:  ldstr      "Test2.Else"
  IL_0026:  call       "Sub System.Console.WriteLine(String)"
  IL_002b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineNullableIsTrue_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim s1 As New S1(True)
        Test1(s1)

        s1 = New S1(False)
        Test1(s1)

        Test1(Nothing)
    End Sub

    Sub Test1(x as S1?)
        if GetVal(x)?.M1()
            System.Console.WriteLine("Test1.Then")
        Else
            System.Console.WriteLine("Test1.Else")
        End If
    End Sub

    Function GetVal(x As S1?) As S1?
        return x
    End Function
End Module

Structure S1
    Dim _x as Boolean 

    Sub New(x as Boolean)
        _x = x
    End Sub

    Function M1() As Boolean
        System.Console.WriteLine("M1")
        return _x
    End Function
End Structure
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
M1
Test1.Then
M1
Test1.Else
Test1.Else
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  1
  .locals init (S1? V_0,
                S1 V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.GetVal(S1?) As S1?"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Function S1?.get_HasValue() As Boolean"
  IL_000e:  brtrue.s   IL_0013
  IL_0010:  ldc.i4.0
  IL_0011:  br.s       IL_0022
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  call       "Function S1.M1() As Boolean"
  IL_0022:  brfalse.s  IL_002f
  IL_0024:  ldstr      "Test1.Then"
  IL_0029:  call       "Sub System.Console.WriteLine(String)"
  IL_002e:  ret
  IL_002f:  ldstr      "Test1.Else"
  IL_0034:  call       "Sub System.Console.WriteLine(String)"
  IL_0039:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineBinaryConditional_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim c1 As New C1(1)
        Test1(c1)
        Test2(c1)
        Test3(c1)

        c1 = New C1(2)
        Test1(c1)
        Test2(c1)
        Test3(c1)

        Test1(Nothing)
        Test2(Nothing)
        Test3(Nothing)

        c1 = New C1(Nothing)
        Test2(c1)
        Test3(c1)
    End Sub

    Function GetX(x as Integer) As Integer
        return x
    End Function

    Sub Test1(x as C1)
        System.Console.WriteLine(if(x?.M1(), GetX(101)))
    End Sub

    Sub Test2(x as C1)
        System.Console.WriteLine(if(x?.M2(), GetX(201)))
    End Sub

    Sub Test3(x as C1)
        System.Console.WriteLine(if(x?.M2(), 301))
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
1
1
1
2
2
2
101
201
301
201
301
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000c
  IL_0003:  ldc.i4.s   101
  IL_0005:  call       "Function Module1.GetX(Integer) As Integer"
  IL_000a:  br.s       IL_0012
  IL_000c:  ldarg.0
  IL_000d:  call       "Function C1.M1() As Integer"
  IL_0012:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0017:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (Integer? V_0,
                Integer? V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Integer?"
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M2() As Integer?"
  IL_0014:  dup
  IL_0015:  stloc.0
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001e:  brtrue.s   IL_002c
  IL_0020:  ldc.i4     0xc9
  IL_0025:  call       "Function Module1.GetX(Integer) As Integer"
  IL_002a:  br.s       IL_0033
  IL_002c:  ldloca.s   V_0
  IL_002e:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0033:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0038:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (Integer? V_0,
                Integer? V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Integer?"
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M2() As Integer?"
  IL_0014:  dup
  IL_0015:  stloc.0
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001e:  brtrue.s   IL_0027
  IL_0020:  ldc.i4     0x12d
  IL_0025:  br.s       IL_002e
  IL_0027:  ldloca.s   V_0
  IL_0029:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_002e:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0033:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineBinaryConditional_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim c1 As New C1(1)
        Test1(c1)
        Test2(c1)
        Test3(c1)

        c1 = New C1(2)
        Test1(c1)
        Test2(c1)
        Test3(c1)

        Test1(Nothing)
        Test2(Nothing)
        Test3(Nothing)

        c1 = New C1(Nothing)
        Test2(c1)
        Test3(c1)
    End Sub

    Function GetX(x as Integer) As Long
        return x
    End Function

    Sub Test1(x as C1)
        System.Console.WriteLine(if(x?.M1(), GetX(101)))
    End Sub

    Sub Test2(x as C1)
        System.Console.WriteLine(if(x?.M2(), GetX(201)))
    End Sub

    Sub Test3(x as C1)
        System.Console.WriteLine(if(x?.M2(), CLng(301)))
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
1
1
1
2
2
2
101
201
301
201
301
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000c
  IL_0003:  ldc.i4.s   101
  IL_0005:  call       "Function Module1.GetX(Integer) As Long"
  IL_000a:  br.s       IL_0013
  IL_000c:  ldarg.0
  IL_000d:  call       "Function C1.M1() As Integer"
  IL_0012:  conv.i8
  IL_0013:  call       "Sub System.Console.WriteLine(Long)"
  IL_0018:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (Integer? V_0,
                Integer? V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Integer?"
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M2() As Integer?"
  IL_0014:  dup
  IL_0015:  stloc.0
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001e:  brtrue.s   IL_002c
  IL_0020:  ldc.i4     0xc9
  IL_0025:  call       "Function Module1.GetX(Integer) As Long"
  IL_002a:  br.s       IL_0034
  IL_002c:  ldloca.s   V_0
  IL_002e:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0033:  conv.i8
  IL_0034:  call       "Sub System.Console.WriteLine(Long)"
  IL_0039:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (Integer? V_0,
                Integer? V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Integer?"
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M2() As Integer?"
  IL_0014:  dup
  IL_0015:  stloc.0
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001e:  brtrue.s   IL_0028
  IL_0020:  ldc.i4     0x12d
  IL_0025:  conv.i8
  IL_0026:  br.s       IL_0030
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_002f:  conv.i8
  IL_0030:  call       "Sub System.Console.WriteLine(Long)"
  IL_0035:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineBinaryConditional_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim c1 As New C1(1)
        Test1(c1)
        Test2(c1)
        Test3(c1)

        c1 = New C1(2)
        Test1(c1)
        Test2(c1)
        Test3(c1)

        Test1(Nothing)
        Test2(Nothing)
        Test3(Nothing)

        c1 = New C1(Nothing)
        Test2(c1)
        Test3(c1)
    End Sub

    Function GetX(x as Integer) As Long?
        return x
    End Function

    Sub Test1(x as C1)
        System.Console.WriteLine(if(x?.M1(), GetX(101)))
    End Sub

    Sub Test2(x as C1)
        System.Console.WriteLine(if(x?.M2(), GetX(201)))
    End Sub

    Sub Test3(x as C1)
        System.Console.WriteLine(if(x?.M2(), new Long?(301)))
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
1
1
1
2
2
2
101
201
301
201
301
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000c
  IL_0003:  ldc.i4.s   101
  IL_0005:  call       "Function Module1.GetX(Integer) As Long?"
  IL_000a:  br.s       IL_0018
  IL_000c:  ldarg.0
  IL_000d:  call       "Function C1.M1() As Integer"
  IL_0012:  conv.i8
  IL_0013:  newobj     "Sub Long?..ctor(Long)"
  IL_0018:  box        "Long?"
  IL_001d:  call       "Sub System.Console.WriteLine(Object)"
  IL_0022:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       68 (0x44)
  .maxstack  2
  .locals init (Integer? V_0,
                Integer? V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Integer?"
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M2() As Integer?"
  IL_0014:  dup
  IL_0015:  stloc.0
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001e:  brtrue.s   IL_002c
  IL_0020:  ldc.i4     0xc9
  IL_0025:  call       "Function Module1.GetX(Integer) As Long?"
  IL_002a:  br.s       IL_0039
  IL_002c:  ldloca.s   V_0
  IL_002e:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0033:  conv.i8
  IL_0034:  newobj     "Sub Long?..ctor(Long)"
  IL_0039:  box        "Long?"
  IL_003e:  call       "Sub System.Console.WriteLine(Object)"
  IL_0043:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       69 (0x45)
  .maxstack  2
  .locals init (Integer? V_0,
                Integer? V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Integer?"
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M2() As Integer?"
  IL_0014:  dup
  IL_0015:  stloc.0
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001e:  brtrue.s   IL_002d
  IL_0020:  ldc.i4     0x12d
  IL_0025:  conv.i8
  IL_0026:  newobj     "Sub Long?..ctor(Long)"
  IL_002b:  br.s       IL_003a
  IL_002d:  ldloca.s   V_0
  IL_002f:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0034:  conv.i8
  IL_0035:  newobj     "Sub Long?..ctor(Long)"
  IL_003a:  box        "Long?"
  IL_003f:  call       "Sub System.Console.WriteLine(Object)"
  IL_0044:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineBinaryConditional_04()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim c1 As New C1(1)
        Test1(c1)
        Test2(c1)
        Test3(c1)

        c1 = New C1(2)
        Test1(c1)
        Test2(c1)
        Test3(c1)

        Test1(Nothing)
        Test2(Nothing)
        Test3(Nothing)

        c1 = New C1(Nothing)
        Test2(c1)
        Test3(c1)
    End Sub

    Function GetX(x as Integer) As Integer?
        return x
    End Function

    Sub Test1(x as C1)
        System.Console.WriteLine(if(x?.M1(), GetX(101)))
    End Sub

    Sub Test2(x as C1)
        System.Console.WriteLine(if(x?.M2(), GetX(201)))
    End Sub

    Sub Test3(x as C1)
        System.Console.WriteLine(if(x?.M2(), new Integer?(301)))
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
1
1
1
2
2
2
101
201
301
201
301
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000c
  IL_0003:  ldc.i4.s   101
  IL_0005:  call       "Function Module1.GetX(Integer) As Integer?"
  IL_000a:  br.s       IL_0017
  IL_000c:  ldarg.0
  IL_000d:  call       "Function C1.M1() As Integer"
  IL_0012:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0017:  box        "Integer?"
  IL_001c:  call       "Sub System.Console.WriteLine(Object)"
  IL_0021:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (Integer? V_0,
                Integer? V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Integer?"
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M2() As Integer?"
  IL_0014:  dup
  IL_0015:  stloc.0
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001e:  brtrue.s   IL_002c
  IL_0020:  ldc.i4     0xc9
  IL_0025:  call       "Function Module1.GetX(Integer) As Integer?"
  IL_002a:  br.s       IL_002d
  IL_002c:  ldloc.0
  IL_002d:  box        "Integer?"
  IL_0032:  call       "Sub System.Console.WriteLine(Object)"
  IL_0037:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (Integer? V_0,
                Integer? V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Integer?"
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M2() As Integer?"
  IL_0014:  dup
  IL_0015:  stloc.0
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001e:  brtrue.s   IL_002c
  IL_0020:  ldc.i4     0x12d
  IL_0025:  newobj     "Sub Integer?..ctor(Integer)"
  IL_002a:  br.s       IL_002d
  IL_002c:  ldloc.0
  IL_002d:  box        "Integer?"
  IL_0032:  call       "Sub System.Console.WriteLine(Object)"
  IL_0037:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineBinaryConditional_05()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim s1 As New S1()
        Test1(s1)
        System.Console.WriteLine("---")
        Test1(Nothing)
    End Sub

    Sub Test1(x as S1?)
        System.Console.WriteLine(if(GetVal(x)?.M1(), 101))
    End Sub

    Function GetVal(x As S1?) As S1?
        return x
    End Function
End Module

Structure S1
    Function M1() As Integer
        System.Console.WriteLine("M1")
        return 1
    End Function
End Structure
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
M1
1
---
101
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  1
  .locals init (S1? V_0,
                S1 V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.GetVal(S1?) As S1?"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Function S1?.get_HasValue() As Boolean"
  IL_000e:  brtrue.s   IL_0014
  IL_0010:  ldc.i4.s   101
  IL_0012:  br.s       IL_0023
  IL_0014:  ldloca.s   V_0
  IL_0016:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_001b:  stloc.1
  IL_001c:  ldloca.s   V_1
  IL_001e:  call       "Function S1.M1() As Integer"
  IL_0023:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0028:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineBinaryConditional_Default()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Public Module Program
    Public Class C1
        Public Property x As Integer
    End Class

    Public Sub Main()
        Dim c = New C1() With { .x = 42 }
        System.Console.WriteLine(Test(c))
        System.Console.WriteLine(Test(Nothing))
    End Sub

    Public Function Test(c As C1) As Integer
        Return If(c?.x, 0)
    End Function
End Module</file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
42
0
]]>)
            verifier.VerifyIL("Program.Test(Program.C1)",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldc.i4.0
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  call       "Function Program.C1.get_x() As Integer"
  IL_000b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineConversion_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim c1 As New C1(1)
        Test1(c1)
        Test2(c1)

        Test1(Nothing)
        Test2(Nothing)

        c1 = New C1(Nothing)
        Test2(c1)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x as C1)
        System.Console.WriteLine(CType(x?.M1(), Long?))
    End Sub

    Sub Test2(x as C1)
        System.Console.WriteLine(CType(x?.M2(), Long?))
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
1
1



---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  1
  .locals init (Long? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    "Long?"
  IL_000b:  ldloc.0
  IL_000c:  br.s       IL_001a
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M1() As Integer"
  IL_0014:  conv.i8
  IL_0015:  newobj     "Sub Long?..ctor(Long)"
  IL_001a:  box        "Long?"
  IL_001f:  call       "Sub System.Console.WriteLine(Object)"
  IL_0024:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       65 (0x41)
  .maxstack  1
  .locals init (Integer? V_0,
                Integer? V_1,
                Long? V_2)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Integer?"
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M2() As Integer?"
  IL_0014:  stloc.0
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001c:  brtrue.s   IL_0029
  IL_001e:  ldloca.s   V_2
  IL_0020:  initobj    "Long?"
  IL_0026:  ldloc.2
  IL_0027:  br.s       IL_0036
  IL_0029:  ldloca.s   V_0
  IL_002b:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0030:  conv.i8
  IL_0031:  newobj     "Sub Long?..ctor(Long)"
  IL_0036:  box        "Long?"
  IL_003b:  call       "Sub System.Console.WriteLine(Object)"
  IL_0040:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineConversion_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim s1 As New S1()
        Test1(s1)
        System.Console.WriteLine("---")
        Test1(Nothing)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x as S1?)
        System.Console.WriteLine(CType(GetVal(x)?.M1(), Long?))
    End Sub

    Function GetVal(x As S1?) As S1?
        return x
    End Function
End Module

Structure S1
    Function M1() As Integer
        System.Console.WriteLine("M1")
        return 1
    End Function
End Structure
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
M1
1
---

---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (S1? V_0,
                Long? V_1,
                S1 V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.GetVal(S1?) As S1?"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Function S1?.get_HasValue() As Boolean"
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    "Long?"
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0030
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_0022:  stloc.2
  IL_0023:  ldloca.s   V_2
  IL_0025:  call       "Function S1.M1() As Integer"
  IL_002a:  conv.i8
  IL_002b:  newobj     "Sub Long?..ctor(Long)"
  IL_0030:  box        "Long?"
  IL_0035:  call       "Sub System.Console.WriteLine(Object)"
  IL_003a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineIs_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim c1 As New C1(1)
        Test1(c1)
        Test2(c1)

        Test1(Nothing)
        Test2(Nothing)

        c1 = New C1(Nothing)
        Test2(c1)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x as C1)
        System.Console.WriteLine(x?.M1() Is Nothing)
    End Sub

    Sub Test2(x as C1)
        System.Console.WriteLine(x?.M2() Is Nothing)
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        System.Console.WriteLine("M1")
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        System.Console.WriteLine("M2")
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
M1
False
M2
False
True
True
M2
True
---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.1
  IL_0004:  br.s       IL_000e
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.M1() As Integer"
  IL_000c:  pop
  IL_000d:  ldc.i4.0
  IL_000e:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0013:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (Integer? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.1
  IL_0004:  br.s       IL_0017
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.M2() As Integer?"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0014:  ldc.i4.0
  IL_0015:  ceq
  IL_0017:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineIs_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim c1 As New C1(1)
        Test1(c1)
        Test2(c1)

        Test1(Nothing)
        Test2(Nothing)

        c1 = New C1(Nothing)
        Test2(c1)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x as C1)
        System.Console.WriteLine(Nothing Is x?.M1())
    End Sub

    Sub Test2(x as C1)
        System.Console.WriteLine(Nothing Is x?.M2())
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        System.Console.WriteLine("M1")
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        System.Console.WriteLine("M2")
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
M1
False
M2
False
True
True
M2
True
---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.1
  IL_0004:  br.s       IL_000e
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.M1() As Integer"
  IL_000c:  pop
  IL_000d:  ldc.i4.0
  IL_000e:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0013:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (Integer? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.1
  IL_0004:  br.s       IL_0017
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.M2() As Integer?"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0014:  ldc.i4.0
  IL_0015:  ceq
  IL_0017:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineIsNot_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim c1 As New C1(1)
        Test1(c1)
        Test2(c1)

        Test1(Nothing)
        Test2(Nothing)

        c1 = New C1(Nothing)
        Test2(c1)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x as C1)
        System.Console.WriteLine(x?.M1() IsNot Nothing)
    End Sub

    Sub Test2(x as C1)
        System.Console.WriteLine(x?.M2() IsNot Nothing)
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        System.Console.WriteLine("M1")
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        System.Console.WriteLine("M2")
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
M1
True
M2
True
False
False
M2
False
---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.0
  IL_0004:  br.s       IL_000e
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.M1() As Integer"
  IL_000c:  pop
  IL_000d:  ldc.i4.1
  IL_000e:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0013:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (Integer? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.0
  IL_0004:  br.s       IL_0014
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.M2() As Integer?"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0014:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0019:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineIsNot_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim c1 As New C1(1)
        Test1(c1)
        Test2(c1)

        Test1(Nothing)
        Test2(Nothing)

        c1 = New C1(Nothing)
        Test2(c1)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x as C1)
        System.Console.WriteLine(Nothing IsNot x?.M1())
    End Sub

    Sub Test2(x as C1)
        System.Console.WriteLine(Nothing IsNot x?.M2())
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        System.Console.WriteLine("M1")
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        System.Console.WriteLine("M2")
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
M1
True
M2
True
False
False
M2
False
---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.0
  IL_0004:  br.s       IL_000e
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.M1() As Integer"
  IL_000c:  pop
  IL_000d:  ldc.i4.1
  IL_000e:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0013:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (Integer? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.0
  IL_0004:  br.s       IL_0014
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.M2() As Integer?"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0014:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0019:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineIsNot_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim s1 As New S1()
        Test1(s1)

        System.Console.WriteLine("---")

        Test1(Nothing)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x as S1?)
        System.Console.WriteLine(GetVal(x)?.M1() IsNot Nothing)
    End Sub

    Function GetVal(x As S1?) As S1?
        return x
    End Function
End Module

Structure S1
    Function M1() As Integer
        System.Console.WriteLine("M1")
        return 1
    End Function
End Structure
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
M1
True
---
False
---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  1
  .locals init (S1? V_0,
                S1 V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.GetVal(S1?) As S1?"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Function S1?.get_HasValue() As Boolean"
  IL_000e:  brtrue.s   IL_0013
  IL_0010:  ldc.i4.0
  IL_0011:  br.s       IL_0024
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  call       "Function S1.M1() As Integer"
  IL_0022:  pop
  IL_0023:  ldc.i4.1
  IL_0024:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0029:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineBinary_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim c1 As New C1(1)
        Test1(c1)
        Test2(c1)

        Test1(Nothing)
        Test2(Nothing)

        c1 = New C1(Nothing)
        Test2(c1)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x as C1)
        System.Console.WriteLine(x?.M1() = 1)
    End Sub

    Sub Test2(x as C1)
        System.Console.WriteLine(x?.M2() = 1)
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        System.Console.WriteLine("M1")
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        System.Console.WriteLine("M2")
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
M1
True
M2
True


M2

---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (Boolean? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    "Boolean?"
  IL_000b:  ldloc.0
  IL_000c:  br.s       IL_001c
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M1() As Integer"
  IL_0014:  ldc.i4.1
  IL_0015:  ceq
  IL_0017:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_001c:  box        "Boolean?"
  IL_0021:  call       "Sub System.Console.WriteLine(Object)"
  IL_0026:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  2
  .locals init (Integer? V_0,
                Integer? V_1,
                Boolean? V_2)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Integer?"
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M2() As Integer?"
  IL_0014:  stloc.0
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001c:  brtrue.s   IL_0029
  IL_001e:  ldloca.s   V_2
  IL_0020:  initobj    "Boolean?"
  IL_0026:  ldloc.2
  IL_0027:  br.s       IL_0038
  IL_0029:  ldloca.s   V_0
  IL_002b:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0030:  ldc.i4.1
  IL_0031:  ceq
  IL_0033:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0038:  box        "Boolean?"
  IL_003d:  call       "Sub System.Console.WriteLine(Object)"
  IL_0042:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineBinary_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim c1 As New C1(1)
        Test1(c1)
        Test2(c1)

        Test1(Nothing)
        Test2(Nothing)

        c1 = New C1(Nothing)
        Test2(c1)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x as C1)
        Dim val = 1
        System.Console.WriteLine(x?.M1() <> val)
    End Sub

    Sub Test2(x as C1)
        Dim val = 1
        System.Console.WriteLine(x?.M2() <> val)
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        System.Console.WriteLine("M1")
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        System.Console.WriteLine("M2")
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
M1
False
M2
False


M2

---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (Integer V_0, //val
                Boolean? V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  brtrue.s   IL_0010
  IL_0005:  ldloca.s   V_1
  IL_0007:  initobj    "Boolean?"
  IL_000d:  ldloc.1
  IL_000e:  br.s       IL_0021
  IL_0010:  ldarg.0
  IL_0011:  call       "Function C1.M1() As Integer"
  IL_0016:  ldloc.0
  IL_0017:  ceq
  IL_0019:  ldc.i4.0
  IL_001a:  ceq
  IL_001c:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0021:  box        "Boolean?"
  IL_0026:  call       "Sub System.Console.WriteLine(Object)"
  IL_002b:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       72 (0x48)
  .maxstack  2
  .locals init (Integer V_0, //val
                Integer? V_1,
                Integer? V_2,
                Boolean? V_3)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  brtrue.s   IL_0010
  IL_0005:  ldloca.s   V_2
  IL_0007:  initobj    "Integer?"
  IL_000d:  ldloc.2
  IL_000e:  br.s       IL_0016
  IL_0010:  ldarg.0
  IL_0011:  call       "Function C1.M2() As Integer?"
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001e:  brtrue.s   IL_002b
  IL_0020:  ldloca.s   V_3
  IL_0022:  initobj    "Boolean?"
  IL_0028:  ldloc.3
  IL_0029:  br.s       IL_003d
  IL_002b:  ldloca.s   V_1
  IL_002d:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0032:  ldloc.0
  IL_0033:  ceq
  IL_0035:  ldc.i4.0
  IL_0036:  ceq
  IL_0038:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_003d:  box        "Boolean?"
  IL_0042:  call       "Sub System.Console.WriteLine(Object)"
  IL_0047:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineBinary_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim c1 As New C1(1)
        Test1(c1, 2)
        Test2(c1, 2)

        Test1(Nothing, 2)
        Test2(Nothing, 2)

        c1 = New C1(Nothing)
        Test2(c1, 2)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x as C1, val as Integer)
        If x?.M1() < CInt(val)
            System.Console.WriteLine("Then")
        Else
            System.Console.WriteLine("Else")
        End If
    End Sub

    Sub Test2(x as C1, val as Integer)
        If x?.M2() < CInt(val)
            System.Console.WriteLine("Then")
        Else
            System.Console.WriteLine("Else")
        End If
    End Sub
End Module

Class C1
    Private m_Integer As Integer?

    Sub New (x as Integer?)
        m_Integer = x
    End Sub

    Function M1() As Integer
        System.Console.WriteLine("M1")
        return m_Integer.Value
    End Function

    Function M2() As Integer?
        System.Console.WriteLine("M2")
        return m_Integer
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
M1
Then
M2
Then
Else
Else
M2
Else
---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.0
  IL_0004:  br.s       IL_000f
  IL_0006:  ldarg.0
  IL_0007:  call       "Function C1.M1() As Integer"
  IL_000c:  ldarg.1
  IL_000d:  clt
  IL_000f:  brfalse.s  IL_001c
  IL_0011:  ldstr      "Then"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ret
  IL_001c:  ldstr      "Else"
  IL_0021:  call       "Sub System.Console.WriteLine(String)"
  IL_0026:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       88 (0x58)
  .maxstack  2
  .locals init (Integer? V_0,
                Integer? V_1,
                Boolean? V_2)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Integer?"
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0014
  IL_000e:  ldarg.0
  IL_000f:  call       "Function C1.M2() As Integer?"
  IL_0014:  stloc.0
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001c:  brtrue.s   IL_0029
  IL_001e:  ldloca.s   V_2
  IL_0020:  initobj    "Boolean?"
  IL_0026:  ldloc.2
  IL_0027:  br.s       IL_0038
  IL_0029:  ldloca.s   V_0
  IL_002b:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0030:  ldarg.1
  IL_0031:  clt
  IL_0033:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0038:  stloc.2
  IL_0039:  ldloca.s   V_2
  IL_003b:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0040:  brfalse.s  IL_004d
  IL_0042:  ldstr      "Then"
  IL_0047:  call       "Sub System.Console.WriteLine(String)"
  IL_004c:  ret
  IL_004d:  ldstr      "Else"
  IL_0052:  call       "Sub System.Console.WriteLine(String)"
  IL_0057:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InlineBinary_04()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine("---")
        Dim s1 As New S1()
        Test1(s1)
        System.Console.WriteLine("---")
        Test1(Nothing)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x as S1?)
        System.Console.WriteLine(GetVal(x)?.M1() = 1)
    End Sub

    Function GetVal(x As S1?) As S1?
        return x
    End Function
End Module

Structure S1
    Function M1() As Integer
        System.Console.WriteLine("M1")
        return 1
    End Function
End Structure
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
M1
True
---

---
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init (S1? V_0,
                Boolean? V_1,
                S1 V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.GetVal(S1?) As S1?"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Function S1?.get_HasValue() As Boolean"
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    "Boolean?"
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0032
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_0022:  stloc.2
  IL_0023:  ldloca.s   V_2
  IL_0025:  call       "Function S1.M1() As Integer"
  IL_002a:  ldc.i4.1
  IL_002b:  ceq
  IL_002d:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0032:  box        "Boolean?"
  IL_0037:  call       "Sub System.Console.WriteLine(Object)"
  IL_003c:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub Bug1078014_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
        Dim x = New Test()
        x?.M1()?.M1()
    End Sub
End Module

Class Test
    Function M1() As Test
        System.Console.WriteLine("Test.M1")
        return Me
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test.M1
Test.M1
]]>)
        End Sub

        <Fact()>
        Public Sub Bug1078014_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
        Dim x = New Test()
        x?.M1()?.M1()?.M1()?.M1()?.M1()?.M1()?.M1()?.M1()
    End Sub
End Module

Class Test
    Function M1() As Test
        System.Console.WriteLine("Test.M1")
        return Me
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
]]>)
        End Sub

        <Fact()>
        Public Sub Bug1078014_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
        Dim x = New Test()
        Call x?.M1()?.M1()?.M1()?.M1()?.M1()?.M1()?.M1()?.M1()
    End Sub
End Module

Class Test
    Function M1() As Test
        System.Console.WriteLine("Test.M1")
        return Me
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
]]>)
        End Sub

        <Fact()>
        Public Sub Bug1078014_04()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
        Dim x = New Test()
        System.Console.WriteLine(x?.M1()?.M1()?.M1()?.M1()?.M1()?.M1()?.M1()?.M1())
    End Sub
End Module

Class Test
    Function M1() As Test
        System.Console.WriteLine("Test.M1")
        return Me
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
Test.M1
Test
]]>)
        End Sub

        <WorkItem(470, "CodePlex")>
        <Fact>
        Public Sub CodPlexBug470()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
        System.Console.WriteLine(MyMethod(Nothing))
        System.Console.WriteLine(MyMethod(new MyType()))
    End Sub

    Function MyMethod(myObject As MyType) As Decimal
        return If(myObject?.MyField, 0D)
    End Function
End Module

Public Class MyType
    Public MyField As Decimal = 123
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
0
123
]]>)

            verifier.VerifyIL("Program.MyMethod", <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  ldsfld     "Decimal.Zero As Decimal"
  IL_0008:  ret
  IL_0009:  ldarg.0
  IL_000a:  ldfld      "MyType.MyField As Decimal"
  IL_000f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub RaceInAsync_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Interface I1
    Sub Test(val As Object)
End Interface

Module Module1

    Sub Main()
        Test1()
        System.Console.WriteLine()
        Test2()
        System.Console.WriteLine()
        Test3()
    End Sub

    Sub Test1()
        System.Console.WriteLine("--- Test1 ---")

        Dim a1 = {New S1()}
        Test11(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test12(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test13(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test14(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        Test11(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test12(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test12(Of C1)({Nothing})
        System.Console.WriteLine("----")

        Test13(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test14(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test14(Of C1)({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test2()
        System.Console.WriteLine("--- Test2 ---")

        Dim a1 = {New S1()}
        Test21(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test22(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test23(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test24(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        Test21(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test22(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test22(Of C1)({Nothing})
        System.Console.WriteLine("----")

        Test23(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test24(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test24(Of C1)({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test3()
        System.Console.WriteLine("--- Test3 ---")

        Dim a1 = {New S1()}
        Test31(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test32(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test33(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test34(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        Test31(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test32(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test32(Of C1)({Nothing})
        System.Console.WriteLine("----")

        Test33(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test34(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test34(Of C1)({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Function GetVal(Of T)(val As T) As T
        System.Console.WriteLine("GetVal")
        Return val
    End Function

    Sub Test11(Of T As I1)(array As T())
        GetVal(array)(0).Test(DoNothing1())
    End Sub

    Sub Test12(Of T As I1)(array As T())
        GetVal(array)(0)?.Test(Clear1(array))
    End Sub

    Async Function Test13(Of T As I1)(array As T()) As Threading.Tasks.Task
        GetVal(array)(0).Test(Await DoNothing2(array))
    End Function

    Async Function Test14(Of T As I1)(array As T()) As Threading.Tasks.Task
        GetVal(array)(0)?.Test(Await Clear2(array))
    End Function

    Sub Test21(Of T As I1)(array As T())
        GetVal(array)(0).ExtensionByVal(Clear1(array))
    End Sub

    Sub Test22(Of T As I1)(array As T())
        GetVal(array)(0)?.ExtensionByVal(Clear1(array))
    End Sub

    Async Function Test23(Of T As I1)(array As T()) As Threading.Tasks.Task
        GetVal(array)(0).ExtensionByVal(Await DoNothing2(array))
    End Function

    Async Function Test24(Of T As I1)(array As T()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByVal(Await Clear2(array))
    End Function

    Sub Test31(Of T As I1)(array As T())
        GetVal(array)(0).ExtensionByRef(DoNothing1())
    End Sub

    Sub Test32(Of T As I1)(array As T())
        GetVal(array)(0)?.ExtensionByRef(Clear1(array))
    End Sub

    Async Function Test33(Of T As I1)(array As T()) As Threading.Tasks.Task
        GetVal(array)(0).ExtensionByRef(Await DoNothing2(array))
    End Function

    Async Function Test34(Of T As I1)(array As T()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByRef(Await Clear2(array))
    End Function

    Function Clear1(Of T)(array As T()) As Object
        System.Console.WriteLine("Clear")
        array(0) = Nothing
        Return Nothing
    End Function

    Async Function Clear2(Of T)(array As T()) As Threading.Tasks.Task(Of Object)
        System.Console.WriteLine("Clear")
        array(0) = Nothing
        Await Task.Delay(10)
        Return Nothing
    End Function

    Function DoNothing1() As Object
        System.Console.WriteLine("Clear")
        Return Nothing
    End Function

    Async Function DoNothing2(Of T)(array As T()) As Threading.Tasks.Task(Of Object)
        System.Console.WriteLine("Clear")
        Return Nothing
    End Function

    <Runtime.CompilerServices.Extension()>
    Sub ExtensionByVal(Of T As I1)(receiver As T, val As Object)
        receiver.Test(val)
    End Sub

    <Runtime.CompilerServices.Extension()>
    Sub ExtensionByRef(Of T As I1)(ByRef receiver As T, val As Object)
        receiver.Test(val)
    End Sub
End Module

Structure S1
    Implements I1

    Public IsMutated As Boolean

    Public Sub Test(val As Object) Implements I1.Test
        IsMutated = True
        System.Console.WriteLine("S1.Test")
    End Sub
End Structure

Class C1
    Implements I1

    Public Sub Test(val As Object) Implements I1.Test
        System.Console.WriteLine("C1.Test")
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
--- Test1 ---
GetVal
Clear
S1.Test
True
----
GetVal
Clear
S1.Test
True
----
GetVal
Clear
S1.Test
True
----
GetVal
Clear
S1.Test
True
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----

--- Test2 ---
GetVal
Clear
S1.Test
False
----
GetVal
Clear
S1.Test
False
----
GetVal
Clear
S1.Test
False
----
GetVal
Clear
S1.Test
False
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----

--- Test3 ---
GetVal
Clear
S1.Test
True
----
GetVal
Clear
S1.Test
False
----
GetVal
Clear
S1.Test
True
----
GetVal
Clear
S1.Test
False
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----
]]>)
        End Sub

        <Fact()>
        Public Sub RaceInAsync_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Interface I1
    Sub Test(val As Object)
End Interface

Module Module1

    Sub Main()
        Test1()
        System.Console.WriteLine()
        Test2()
        System.Console.WriteLine()
        Test3()
    End Sub

    Sub Test1()
        System.Console.WriteLine("--- Test1 ---")

        Dim a1 = {New S1()}
        Test11(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test12(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test13(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test14(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        Test11(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test12(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test12(Of C1)({Nothing})
        System.Console.WriteLine("----")

        Test13(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test14(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test14(Of C1)({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test2()
        System.Console.WriteLine("--- Test2 ---")

        Dim a1 = {New S1()}
        Test21(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test22(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test23(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test24(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        Test21(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test22(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test22(Of C1)({Nothing})
        System.Console.WriteLine("----")

        Test23(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test24(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test24(Of C1)({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test3()
        System.Console.WriteLine("--- Test3 ---")

        Dim a1 = {New S1()}
        Test31(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test32(a1)
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test33(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        a1 = {New S1()}
        Test34(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        Test31(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test32(Of C1)({New C1()})
        System.Console.WriteLine("----")

        Test32(Of C1)({Nothing})
        System.Console.WriteLine("----")

        Test33(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test34(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test34(Of C1)({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Function GetVal(Of T)(val As T) As T
        System.Console.WriteLine("GetVal")
        Return val
    End Function

    Sub Test11(Of T As I1)(array As T())
        Call (GetVal(array)(0)).Test(DoNothing1())
    End Sub

    Sub Test12(Of T As I1)(array As T())
        Call (GetVal(array)(0))?.Test(Clear1(array))
    End Sub

    Async Function Test13(Of T As I1)(array As T()) As Threading.Tasks.Task
        Call (GetVal(array)(0)).Test(Await DoNothing2(array))
    End Function

    Async Function Test14(Of T As I1)(array As T()) As Threading.Tasks.Task
        Call (GetVal(array)(0))?.Test(Await Clear2(array))
    End Function

    Sub Test21(Of T As I1)(array As T())
        Call (GetVal(array)(0)).ExtensionByVal(Clear1(array))
    End Sub

    Sub Test22(Of T As I1)(array As T())
        Call (GetVal(array)(0))?.ExtensionByVal(Clear1(array))
    End Sub

    Async Function Test23(Of T As I1)(array As T()) As Threading.Tasks.Task
        Call (GetVal(array)(0)).ExtensionByVal(Await DoNothing2(array))
    End Function

    Async Function Test24(Of T As I1)(array As T()) As Threading.Tasks.Task
        Call (GetVal(array)(0))?.ExtensionByVal(Await Clear2(array))
    End Function

    Sub Test31(Of T As I1)(array As T())
        Call (GetVal(array)(0)).ExtensionByRef(DoNothing1())
    End Sub

    Sub Test32(Of T As I1)(array As T())
        Call (GetVal(array)(0))?.ExtensionByRef(Clear1(array))
    End Sub

    Async Function Test33(Of T As I1)(array As T()) As Threading.Tasks.Task
        Call (GetVal(array)(0)).ExtensionByRef(Await DoNothing2(array))
    End Function

    Async Function Test34(Of T As I1)(array As T()) As Threading.Tasks.Task
        Call (GetVal(array)(0))?.ExtensionByRef(Await Clear2(array))
    End Function

    Function Clear1(Of T)(array As T()) As Object
        System.Console.WriteLine("Clear")
        array(0) = Nothing
        Return Nothing
    End Function

    Async Function Clear2(Of T)(array As T()) As Threading.Tasks.Task(Of Object)
        System.Console.WriteLine("Clear")
        array(0) = Nothing
        Await Task.Delay(10)
        Return Nothing
    End Function

    Function DoNothing1() As Object
        System.Console.WriteLine("Clear")
        Return Nothing
    End Function

    Async Function DoNothing2(Of T)(array As T()) As Threading.Tasks.Task(Of Object)
        System.Console.WriteLine("Clear")
        Return Nothing
    End Function

    <Runtime.CompilerServices.Extension()>
    Sub ExtensionByVal(Of T As I1)(receiver As T, val As Object)
        receiver.Test(val)
    End Sub

    <Runtime.CompilerServices.Extension()>
    Sub ExtensionByRef(Of T As I1)(ByRef receiver As T, val As Object)
        receiver.Test(val)
    End Sub
End Module

Structure S1
    Implements I1

    Public IsMutated As Boolean

    Public Sub Test(val As Object) Implements I1.Test
        IsMutated = True
        System.Console.WriteLine("S1.Test")
    End Sub
End Structure

Class C1
    Implements I1

    Public Sub Test(val As Object) Implements I1.Test
        System.Console.WriteLine("C1.Test")
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
--- Test1 ---
GetVal
Clear
S1.Test
True
----
GetVal
Clear
S1.Test
True
----
GetVal
Clear
S1.Test
True
----
GetVal
Clear
S1.Test
True
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----

--- Test2 ---
GetVal
Clear
S1.Test
False
----
GetVal
Clear
S1.Test
False
----
GetVal
Clear
S1.Test
False
----
GetVal
Clear
S1.Test
False
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----

--- Test3 ---
GetVal
Clear
S1.Test
False
----
GetVal
Clear
S1.Test
False
----
GetVal
Clear
S1.Test
False
----
GetVal
Clear
S1.Test
False
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
Clear
C1.Test
----
GetVal
----
]]>)
        End Sub

        <Fact()>
        Public Sub RaceInAsync_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Test1()
        System.Console.WriteLine()
        Test2()
        System.Console.WriteLine()
        Test3()
    End Sub

    Sub Test1()
        System.Console.WriteLine("--- Test1 ---")

        Test12({New S1()})
        System.Console.WriteLine("----")

        Test12({Nothing})
        System.Console.WriteLine("----")

        Test14({New S1()}).Wait()
        System.Console.WriteLine("----")

        Test14({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test2()
        System.Console.WriteLine("--- Test2 ---")

        Test22({New S1()})
        System.Console.WriteLine("----")

        Test22({Nothing})
        System.Console.WriteLine("----")

        Test24({New S1()}).Wait()
        System.Console.WriteLine("----")

        Test24({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test3()
        System.Console.WriteLine("--- Test3 ---")

        Test32({New S1()})
        System.Console.WriteLine("----")

        Test32({Nothing})
        System.Console.WriteLine("----")

        Test34({New S1()}).Wait()
        System.Console.WriteLine("----")

        Test34({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Function GetVal(Of T)(val As T) As T
        System.Console.WriteLine("GetVal")
        Return val
    End Function

    Sub Test12(array As S1?())
        GetVal(array)(0)?.Test(Clear1(array))
    End Sub

    Async Function Test14(array As S1?()) As Threading.Tasks.Task
        GetVal(array)(0)?.Test(Await Clear2(array))
    End Function

    Sub Test22(array As S1?())
        GetVal(array)(0)?.ExtensionByVal(Clear1(array))
    End Sub

    Async Function Test24(array As S1?()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByVal(Await Clear2(array))
    End Function

    Sub Test32(array As S1?())
        GetVal(array)(0)?.ExtensionByRef(Clear1(array))
    End Sub

    Async Function Test34(array As S1?()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByRef(Await Clear2(array))
    End Function

    Function Clear1(array As S1?()) As Object
        System.Console.WriteLine("Clear")
        array(0) = New S1?()
        Return Nothing
    End Function

    Async Function Clear2(array As S1?()) As Threading.Tasks.Task(Of Object)
        System.Console.WriteLine("Clear")
        array(0) = New S1?()
        Await Task.Delay(10)
        Return Nothing
    End Function

    <Runtime.CompilerServices.Extension()>
    Sub ExtensionByVal(receiver As S1, val As Object)
        receiver.Test(val)
    End Sub

    <Runtime.CompilerServices.Extension()>
    Sub ExtensionByRef(ByRef receiver As S1, val As Object)
        receiver.Test(val)
    End Sub
End Module

Structure S1
    Public Sub Test(val As Object)
        System.Console.WriteLine("S1.Test")
    End Sub
End Structure
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
--- Test1 ---
GetVal
Clear
S1.Test
----
GetVal
----
GetVal
Clear
S1.Test
----
GetVal
----

--- Test2 ---
GetVal
Clear
S1.Test
----
GetVal
----
GetVal
Clear
S1.Test
----
GetVal
----

--- Test3 ---
GetVal
Clear
S1.Test
----
GetVal
----
GetVal
Clear
S1.Test
----
GetVal
----
]]>)
        End Sub

        <Fact()>
        Public Sub RaceInAsync_04()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Test1()
        System.Console.WriteLine()
        Test2()
        System.Console.WriteLine()
        Test3()
    End Sub

    Sub Test1()
        System.Console.WriteLine("--- Test1 ---")

        Test12({New C1()})
        System.Console.WriteLine("----")

        Test12({Nothing})
        System.Console.WriteLine("----")

        Test14({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test14({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test2()
        System.Console.WriteLine("--- Test2 ---")

        Test22({New C1()})
        System.Console.WriteLine("----")

        Test22({Nothing})
        System.Console.WriteLine("----")

        Test24({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test24({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test3()
        System.Console.WriteLine("--- Test3 ---")

        Test32({New C1()})
        System.Console.WriteLine("----")

        Test32({Nothing})
        System.Console.WriteLine("----")

        Test34({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test34({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Function GetVal(Of T)(val As T) As T
        System.Console.WriteLine("GetVal")
        Return val
    End Function

    Sub Test12(array As C1())
        GetVal(array)(0)?.Test(Clear1(array))
    End Sub

    Async Function Test14(array As C1()) As Threading.Tasks.Task
        GetVal(array)(0)?.Test(Await Clear2(array))
    End Function

    Sub Test22(array As C1())
        GetVal(array)(0)?.ExtensionByVal(Clear1(array))
    End Sub

    Async Function Test24(array As C1()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByVal(Await Clear2(array))
    End Function

    Sub Test32(array As C1())
        GetVal(array)(0)?.ExtensionByRef(Clear1(array))
    End Sub

    Async Function Test34(array As C1()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByRef(Await Clear2(array))
    End Function

    Function Clear1(array As C1()) As Object
        System.Console.WriteLine("Clear")
        array(0) = Nothing
        Return Nothing
    End Function

    Async Function Clear2(array As C1()) As Threading.Tasks.Task(Of Object)
        System.Console.WriteLine("Clear")
        array(0) = Nothing
        Await Task.Delay(10)
        Return Nothing
    End Function

    <Runtime.CompilerServices.Extension()>
    Sub ExtensionByVal(receiver As C1, val As Object)
        receiver.Test(val)
    End Sub

    <Runtime.CompilerServices.Extension()>
    Sub ExtensionByRef(ByRef receiver As C1, val As Object)
        receiver.Test(val)
    End Sub
End Module

Class C1
    Public Sub Test(val As Object)
        System.Console.WriteLine("C1.Test")
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
--- Test1 ---
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
----

--- Test2 ---
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
----

--- Test3 ---
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
----
]]>)
        End Sub

        <Fact()>
        Public Sub RaceInAsync_05()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Test1()
        System.Console.WriteLine()
        Test2()
        System.Console.WriteLine()
        Test3()
    End Sub

    Sub Test1()
        System.Console.WriteLine("--- Test1 ---")

        Test12({New S1()})
        System.Console.WriteLine("----")

        Test12({Nothing})
        System.Console.WriteLine("----")

        Test14({New S1()}).Wait()
        System.Console.WriteLine("----")

        Test14({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test2()
        System.Console.WriteLine("--- Test2 ---")

        Test22({New S1()})
        System.Console.WriteLine("----")

        Test22({Nothing})
        System.Console.WriteLine("----")

        Test24({New S1()}).Wait()
        System.Console.WriteLine("----")

        Test24({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test3()
        System.Console.WriteLine("--- Test3 ---")

        Test32({New S1()})
        System.Console.WriteLine("----")

        Test32({Nothing})
        System.Console.WriteLine("----")

        Test34({New S1()}).Wait()
        System.Console.WriteLine("----")

        Test34({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Function GetVal(Of T)(val As T) As T
        System.Console.WriteLine("GetVal")
        Return val
    End Function

    Sub Test12(array As S1?())
        GetVal(array)(0)?.Instance(Clear1(array)).Dummy(Nothing)
    End Sub

    Async Function Test14(array As S1?()) As Threading.Tasks.Task
        GetVal(array)(0)?.Instance(Clear1(array)).Dummy(Await DoNothing())
    End Function

    Sub Test22(array As S1?())
        GetVal(array)(0)?.ExtensionByVal(Clear1(array)).Dummy(Nothing)
    End Sub

    Async Function Test24(array As S1?()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByVal(Clear1(array)).Dummy(Await DoNothing())
    End Function

    Sub Test32(array As S1?())
        GetVal(array)(0)?.ExtensionByRef(Clear1(array)).Dummy(Nothing)
    End Sub

    Async Function Test34(array As S1?()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByRef(Clear1(array)).Dummy(Await DoNothing())
    End Function

    Function Clear1(array As S1?()) As Object
        System.Console.WriteLine("Clear")
        array(0) = New S1?()
        Return Nothing
    End Function

    Async Function DoNothing() As Threading.Tasks.Task(Of Object)
        Return Nothing
    End Function

    <Runtime.CompilerServices.Extension()>
    Function ExtensionByVal(receiver As S1, val As Object) As S1
        Return receiver.Instance(val)
    End Function

    <Runtime.CompilerServices.Extension()>
    Function ExtensionByRef(ByRef receiver As S1, val As Object) As S1
        Return receiver.Instance(val)
    End Function
End Module

Structure S1
    Public Function Instance(val As Object) As S1
        System.Console.WriteLine("S1.Test")
        Return Me
    End Function

    Public Sub Dummy(val As Object)
    End Sub
End Structure
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
--- Test1 ---
GetVal
Clear
S1.Test
----
GetVal
----
GetVal
Clear
S1.Test
----
GetVal
----

--- Test2 ---
GetVal
Clear
S1.Test
----
GetVal
----
GetVal
Clear
S1.Test
----
GetVal
----

--- Test3 ---
GetVal
Clear
S1.Test
----
GetVal
----
GetVal
Clear
S1.Test
----
GetVal
----
]]>)
        End Sub

        <Fact()>
        Public Sub RaceInAsync_06()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Test1()
        System.Console.WriteLine()
        Test2()
        System.Console.WriteLine()
        Test3()
    End Sub

    Sub Test1()
        System.Console.WriteLine("--- Test1 ---")

        Test12({New C1()})
        System.Console.WriteLine("----")

        Test12({Nothing})
        System.Console.WriteLine("----")

        Test14({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test14({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test2()
        System.Console.WriteLine("--- Test2 ---")

        Test22({New C1()})
        System.Console.WriteLine("----")

        Test22({Nothing})
        System.Console.WriteLine("----")

        Test24({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test24({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test3()
        System.Console.WriteLine("--- Test3 ---")

        Test32({New C1()})
        System.Console.WriteLine("----")

        Test32({Nothing})
        System.Console.WriteLine("----")

        Test34({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test34({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Function GetVal(Of T)(val As T) As T
        System.Console.WriteLine("GetVal")
        Return val
    End Function

    Sub Test12(array As C1())
        GetVal(array)(0)?.Instance(Clear1(array)).Dummy(Nothing)
    End Sub

    Async Function Test14(array As C1()) As Threading.Tasks.Task
        GetVal(array)(0)?.Instance(Clear1(array)).Dummy(Await DoNothing())
    End Function

    Sub Test22(array As C1())
        GetVal(array)(0)?.ExtensionByVal(Clear1(array)).Dummy(Nothing)
    End Sub

    Async Function Test24(array As C1()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByVal(Clear1(array)).Dummy(Await DoNothing())
    End Function

    Sub Test32(array As C1())
        GetVal(array)(0)?.ExtensionByRef(Clear1(array)).Dummy(Nothing)
    End Sub

    Async Function Test34(array As C1()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByRef(Clear1(array)).Dummy(Await DoNothing())
    End Function

    Function Clear1(array As C1()) As Object
        System.Console.WriteLine("Clear")
        array(0) = Nothing
        Return Nothing
    End Function

    Async Function DoNothing() As Threading.Tasks.Task(Of Object)
        Await Task.Delay(10)
        Return Nothing
    End Function

    <Runtime.CompilerServices.Extension()>
    Function ExtensionByVal(receiver As C1, val As Object) As C1
        Return receiver.Instance(val)
    End Function

    <Runtime.CompilerServices.Extension()>
    Function ExtensionByRef(ByRef receiver As C1, val As Object) As C1
        Return receiver.Instance(val)
    End Function
End Module

Class C1
    Public Function Instance(val As Object) As C1
        System.Console.WriteLine("C1.Test")
        Return Me
    End Function

    Public Sub Dummy(val As Object)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
--- Test1 ---
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
----

--- Test2 ---
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
----

--- Test3 ---
GetVal
Clear
C1.Test
----
GetVal
----
GetVal
Clear
C1.Test
----
GetVal
----
]]>)
        End Sub

        <Fact()>
        Public Sub RaceInAsync_07()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Interface I1
    Function Test(val As Object) As I1
End Interface

Module Module1

    Sub Main()
        Test1()
        System.Console.WriteLine()
        Test2()
        System.Console.WriteLine()
        Test3()
    End Sub

    Sub Test1()
        System.Console.WriteLine("--- Test1 ---")

        Dim a1 = {New S1()}
        Test14(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        Test14(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test14(Of C1)({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test2()
        System.Console.WriteLine("--- Test2 ---")

        Dim a1 = {New S1()}
        Test24(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        Test24(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test24(Of C1)({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Sub Test3()
        System.Console.WriteLine("--- Test3 ---")

        Dim a1 = {New S1()}
        Test34(a1).Wait()
        System.Console.WriteLine(a1(0).IsMutated)
        System.Console.WriteLine("----")

        Test34(Of C1)({New C1()}).Wait()
        System.Console.WriteLine("----")

        Test34(Of C1)({Nothing}).Wait()
        System.Console.WriteLine("----")
    End Sub

    Function GetVal(Of T)(val As T) As T
        System.Console.WriteLine("GetVal")
        Return val
    End Function

    Async Function Test14(Of T As I1)(array As T()) As Threading.Tasks.Task
        GetVal(array)(0)?.Test(Clear1(array)).Dummy(Await DoNothing())
    End Function

    Async Function Test24(Of T As I1)(array As T()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByVal(Clear1(array)).Dummy(Await DoNothing())
    End Function

    Async Function Test34(Of T As I1)(array As T()) As Threading.Tasks.Task
        GetVal(array)(0)?.ExtensionByRef(Clear1(array)).Dummy(Await DoNothing())
    End Function

    Function Clear1(Of T)(array As T()) As Object
        System.Console.WriteLine("Clear")
        array(0) = Nothing
        Return Nothing
    End Function

    Async Function DoNothing() As Threading.Tasks.Task(Of Object)
        Await Task.Delay(10)
        Return Nothing
    End Function

    <Runtime.CompilerServices.Extension()>
    Function ExtensionByVal(Of T As I1)(receiver As T, val As Object) As I1
        Return receiver.Test(val)
    End Function

    <Runtime.CompilerServices.Extension()>
    Function ExtensionByRef(Of T As I1)(ByRef receiver As T, val As Object) As I1
        Return receiver.Test(val)
    End Function

    <Runtime.CompilerServices.Extension()>
    Sub Dummy(this As I1, val As Object)
    End Sub
End Module

Structure S1
    Implements I1

    Public IsMutated As Boolean

    Public Function Test(val As Object) As I1 Implements I1.Test
        IsMutated = True
        System.Console.WriteLine("S1.Test")
        Return Me
    End Function
End Structure

Class C1
    Implements I1

    Public Function Test(val As Object) As I1 Implements I1.Test
        System.Console.WriteLine("C1.Test")
        Return Me
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
--- Test1 ---
GetVal
Clear
S1.Test
True
----
GetVal
Clear
C1.Test
----
GetVal
----

--- Test2 ---
GetVal
Clear
S1.Test
False
----
GetVal
Clear
C1.Test
----
GetVal
----

--- Test3 ---
GetVal
Clear
S1.Test
False
----
GetVal
Clear
C1.Test
----
GetVal
----
]]>)
        End Sub

        <Fact()>
        Public Sub NestedConditionalInAsync()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices


Module Module1

    Sub Main()
        Test1(New C1(), New C1()).Wait()
        System.Console.WriteLine("---")
        Test1(New C1(), Nothing).Wait()
        System.Console.WriteLine("---")
        Test1(Nothing, Nothing).Wait()
        System.Console.WriteLine("---")

        Test2(New C1(), New C1()).Wait()
        System.Console.WriteLine("---")
        Test2(New C1(), Nothing).Wait()
        System.Console.WriteLine("---")
        Test2(Nothing, Nothing).Wait()
        System.Console.WriteLine("---")

        Test3(New C1(), New C1()).Wait()
        System.Console.WriteLine("---")
        Test3(New C1(), Nothing).Wait()
        System.Console.WriteLine("---")
        Test3(Nothing, Nothing).Wait()
        System.Console.WriteLine("---")

        Test4(Nothing, Nothing).Wait()
        System.Console.WriteLine("---")
        Test4(Nothing, New C1()).Wait()
        System.Console.WriteLine("---")
        Test4(New C1(), Nothing).Wait()
        System.Console.WriteLine("---")
        Test4(New C1(), New C1()).Wait()
        System.Console.WriteLine("---")
    End Sub

    Function GetVal(Of T)(val As T) As T
        System.Console.WriteLine("GetVal")
        Return val
    End Function

    Async Function Test1(val1 As C1, val2 As C1) As Threading.Tasks.Task
        GetVal(val1)?.M1(Await GetObject(), val2?.M2())
    End Function

    Async Function Test2(val1 As C1, val2 As C1) As Threading.Tasks.Task
        GetVal(val1)?.M1(Await GetObject(), val2)?.M3(Await GetObject(), Nothing)
    End Function

    Async Function Test3(val1 As C1, val2 As C1) As Threading.Tasks.Task
        GetVal(val1)?.M1(Nothing, val2)?.M3(Await GetObject(), Nothing)
    End Function

    Async Function Test4(val1 As C1, val2 As C1) As Threading.Tasks.Task
        val1?.M1(GetObject().Result, val2?.M2())?.M3(Await GetObject(), Nothing)
    End Function

    Async Function GetObject() As Task(Of Object)
        Return Nothing
    End Function

End Module

Class C1
    Public Function M1(val1 As Object, val2 As C1) As C1
        System.Console.WriteLine("M1")
        Return val2
    End Function

    Public Function M2() As C1
        System.Console.WriteLine("M2")
        Return Me
    End Function

    Public Function M3(val1 As Object, val2 As C1) As C1
        System.Console.WriteLine("M3")
        Return val2
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
GetVal
M2
M1
---
GetVal
M1
---
GetVal
---
GetVal
M1
M3
---
GetVal
M1
---
GetVal
---
GetVal
M1
M3
---
GetVal
M1
---
GetVal
---
---
---
M1
---
M2
M1
M3
---
]]>)
        End Sub

        <Fact(), WorkItem(4028, "https://github.com/dotnet/roslyn/issues/4028")>
        Public Sub ConditionalAccessToEvent_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class TestClass

    Event TestEvent As Action

    Sub Main(receiver As TestClass)
        Console.WriteLine(receiver?.TestEvent)
    End Sub

End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef)

            compilation.AssertTheseDiagnostics(<expected>
BC32022: 'Public Event TestEvent As Action' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.
        Console.WriteLine(receiver?.TestEvent)
                                   ~~~~~~~~~~
                                               </expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim access = tree.GetRoot().DescendantNodes().OfType(Of ConditionalAccessExpressionSyntax)().Single()
            Dim memberBinding = DirectCast(access.WhenNotNull, MemberAccessExpressionSyntax)

            Assert.Equal(".TestEvent", memberBinding.ToString())
            Assert.Equal("receiver?.TestEvent", access.ToString())

            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(memberBinding)
            Assert.Equal(CandidateReason.NotAValue, info.CandidateReason)
            Assert.Equal("Event TestClass.TestEvent As System.Action", info.CandidateSymbols.Single().ToTestDisplayString())

            info = model.GetSymbolInfo(memberBinding.Name)
            Assert.Equal(CandidateReason.NotAValue, info.CandidateReason)
            Assert.Equal("Event TestClass.TestEvent As System.Action", info.CandidateSymbols.Single().ToTestDisplayString())

            info = model.GetSymbolInfo(access)
            Assert.Null(info.Symbol)
            Assert.False(info.CandidateSymbols.Any())
        End Sub

        <Fact(), WorkItem(4028, "https://github.com/dotnet/roslyn/issues/4028")>
        Public Sub ConditionalAccessToEvent_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class TestClass

    Event TestEvent As Action

    Shared Sub Test(receiver As TestClass)
        RaiseEvent receiver?.TestEvent
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef)

            compilation.AssertTheseDiagnostics(<expected>
BC30451: 'receiver' is not declared. It may be inaccessible due to its protection level.
        RaiseEvent receiver?.TestEvent
                   ~~~~~~~~
BC30205: End of statement expected.
        RaiseEvent receiver?.TestEvent
                           ~
                                               </expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Assert.False(tree.GetRoot().DescendantNodes().OfType(Of ConditionalAccessExpressionSyntax)().Any())
        End Sub

        <Fact(), WorkItem(4028, "https://github.com/dotnet/roslyn/issues/4028")>
        Public Sub ConditionalAccessToEvent_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class TestClass

    Event TestEvent As Action

    Shared Sub Test(receiver As TestClass)
        AddHandler receiver?.TestEvent, AddressOf Main
    End Sub

    Shared Sub Main()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef)

            compilation.AssertTheseDiagnostics(<expected>
BC30677: 'AddHandler' or 'RemoveHandler' statement event operand must be a dot-qualified expression or a simple name.
        AddHandler receiver?.TestEvent, AddressOf Main
                   ~~~~~~~~~~~~~~~~~~~
                                               </expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim access = tree.GetRoot().DescendantNodes().OfType(Of ConditionalAccessExpressionSyntax)().Single()
            Dim memberBinding = DirectCast(access.WhenNotNull, MemberAccessExpressionSyntax)

            Assert.Equal(".TestEvent", memberBinding.ToString())
            Assert.Equal("receiver?.TestEvent", access.ToString())

            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(memberBinding)
            Assert.Equal(CandidateReason.NotAValue, info.CandidateReason)
            Assert.Equal("Event TestClass.TestEvent As System.Action", info.CandidateSymbols.Single().ToTestDisplayString())

            info = model.GetSymbolInfo(memberBinding.Name)
            Assert.Equal(CandidateReason.NotAValue, info.CandidateReason)
            Assert.Equal("Event TestClass.TestEvent As System.Action", info.CandidateSymbols.Single().ToTestDisplayString())

            info = model.GetSymbolInfo(access)
            Assert.Null(info.Symbol)
            Assert.False(info.CandidateSymbols.Any())
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact(), WorkItem(4028, "https://github.com/dotnet/roslyn/issues/4028")>
        Public Sub ConditionalAccessToEvent_04()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class TestClass

    Event TestEvent As Action

    Shared Sub Test(receiver As TestClass)'BIND:"Shared Sub Test(receiver As TestClass)"
        receiver?.TestEvent()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef)

            compilation.AssertTheseDiagnostics(<expected>
BC32022: 'Public Event TestEvent As Action' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.
        receiver?.TestEvent()
                 ~~~~~~~~~~
                                               </expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim access = tree.GetRoot().DescendantNodes().OfType(Of ConditionalAccessExpressionSyntax)().Single()
            Dim invocation = DirectCast(access.WhenNotNull, InvocationExpressionSyntax)
            Dim memberBinding = DirectCast(invocation.Expression, MemberAccessExpressionSyntax)

            Assert.Equal(".TestEvent", memberBinding.ToString())
            Assert.Equal(".TestEvent()", invocation.ToString())
            Assert.Equal("receiver?.TestEvent()", access.ToString())

            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(memberBinding)
            Assert.Equal(CandidateReason.NotAValue, info.CandidateReason)
            Assert.Equal("Event TestClass.TestEvent As System.Action", info.CandidateSymbols.Single().ToTestDisplayString())

            info = model.GetSymbolInfo(memberBinding.Name)
            Assert.Equal(CandidateReason.NotAValue, info.CandidateReason)
            Assert.Equal("Event TestClass.TestEvent As System.Action", info.CandidateSymbols.Single().ToTestDisplayString())

            info = model.GetSymbolInfo(invocation)
            Assert.Null(info.Symbol)
            Assert.False(info.CandidateSymbols.Any())

            info = model.GetSymbolInfo(access)
            Assert.Null(info.Symbol)
            Assert.False(info.CandidateSymbols.Any())

            compilation.VerifyOperationTree(access, expectedOperationTree:=<![CDATA[
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: System.Void, IsInvalid) (Syntax: 'receiver?.TestEvent()')
  Operation: 
    IParameterReferenceOperation: receiver (OperationKind.ParameterReference, Type: TestClass) (Syntax: 'receiver')
  WhenNotNull: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.TestEvent()')
      Children(1):
          IEventReferenceOperation: Event TestClass.TestEvent As System.Action (OperationKind.EventReference, Type: System.Action, IsInvalid) (Syntax: '.TestEvent')
            Instance Receiver: 
              IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: TestClass, IsImplicit) (Syntax: 'receiver')
]]>.Value)

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedFlowGraph:=<![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'receiver')
              Value: 
                IParameterReferenceOperation: receiver (OperationKind.ParameterReference, Type: TestClass) (Syntax: 'receiver')

        Jump if True (Regular) to Block[B3]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'receiver')
              Operand: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: TestClass, IsImplicit) (Syntax: 'receiver')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'receiver?.TestEvent()')
              Expression: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.TestEvent()')
                  Children(1):
                      IEventReferenceOperation: Event TestClass.TestEvent As System.Action (OperationKind.EventReference, Type: System.Action, IsInvalid) (Syntax: '.TestEvent')
                        Instance Receiver: 
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: TestClass, IsImplicit) (Syntax: 'receiver')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value)
        End Sub

        <Fact(), WorkItem(4615, "https://github.com/dotnet/roslyn/issues/4615")>
        Public Sub ConditionalAndConditionalMethods()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Class Program
    Shared Sub Main()
        TestClass.Create().Test()
        TestClass.Create().Self().Test()
        System.Console.WriteLine("---")
        TestClass.Create()?.Test()
        TestClass.Create()?.Self().Test()
        TestClass.Create()?.Self()?.Test()
    End Sub
End Class

Class TestClass
    <System.Diagnostics.Conditional("DEBUG")>
    Public Sub Test()
        System.Console.WriteLine("Test")
    End Sub

    Shared Function Create() As TestClass
        System.Console.WriteLine("Create")
        return new TestClass()
    End Function

    Function Self() As TestClass
        System.Console.WriteLine("Self")
        return Me
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef, options:=TestOptions.DebugExe,
                                                                             parseOptions:=VisualBasicParseOptions.Default.WithPreprocessorSymbols({New KeyValuePair(Of String, Object)("DEBUG", True)}))

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Create
Test
Create
Self
Test
---
Create
Test
Create
Self
Test
Create
Self
Test
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef, options:=TestOptions.ReleaseExe)

            verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
]]>)
        End Sub

        <Fact()>
        <WorkItem(23351, "https://github.com/dotnet/roslyn/issues/23351")>
        Public Sub ConditionalAccessOffConstrainedTypeParameter_Property()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim obj1 As New MyObject1 With {.MyDate = New Date(636461511000000000L)}
        Dim obj2 As New MyObject2(Of MyObject1)(obj1)

        System.Console.WriteLine(obj1.MyDate.Ticks)
        System.Console.WriteLine(obj2.CurrentDate.Value.Ticks)
        System.Console.WriteLine(new MyObject2(Of MyObject1)(Nothing).CurrentDate.HasValue)
    End Sub
End Module

Public MustInherit Class MyBaseObject1
    Property MyDate As Date
End Class

Public Class MyObject1
    Inherits MyBaseObject1
End Class

Public Class MyObject2(Of MyObjectType As {MyBaseObject1, New})
    Public Sub New(obj As MyObjectType)
        m_CurrentObject1 = obj
    End Sub

    Private m_CurrentObject1 As MyObjectType = Nothing
    Public ReadOnly Property CurrentObject1 As MyObjectType
        Get
            Return m_CurrentObject1
        End Get
    End Property
    Public ReadOnly Property CurrentDate As Date?
        Get
            Return CurrentObject1?.MyDate
        End Get
    End Property
End Class
    </file>
</compilation>

            Dim expectedOutput =
            <![CDATA[
636461511000000000
636461511000000000
False
]]>
            CompileAndVerify(compilationDef, options:=TestOptions.DebugExe, expectedOutput:=expectedOutput)
            CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe, expectedOutput:=expectedOutput)
        End Sub

        <Fact()>
        <WorkItem(23351, "https://github.com/dotnet/roslyn/issues/23351")>
        Public Sub ConditionalAccessOffConstrainedTypeParameter_Field()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim obj1 As New MyObject1 With {.MyDate = New Date(636461511000000000L)}
        Dim obj2 As New MyObject2(Of MyObject1)(obj1)

        System.Console.WriteLine(obj1.MyDate.Ticks)
        System.Console.WriteLine(obj2.CurrentDate.Value.Ticks)
        System.Console.WriteLine(new MyObject2(Of MyObject1)(Nothing).CurrentDate.HasValue)
    End Sub
End Module

Public MustInherit Class MyBaseObject1
    Public MyDate As Date
End Class

Public Class MyObject1
    Inherits MyBaseObject1
End Class

Public Class MyObject2(Of MyObjectType As {MyBaseObject1, New})
    Public Sub New(obj As MyObjectType)
        m_CurrentObject1 = obj
    End Sub

    Private m_CurrentObject1 As MyObjectType = Nothing
    Public ReadOnly Property CurrentObject1 As MyObjectType
        Get
            Return m_CurrentObject1
        End Get
    End Property
    Public ReadOnly Property CurrentDate As Date?
        Get
            Return CurrentObject1?.MyDate
        End Get
    End Property
End Class
    </file>
</compilation>

            Dim expectedOutput =
            <![CDATA[
636461511000000000
636461511000000000
False
]]>
            CompileAndVerify(compilationDef, options:=TestOptions.DebugExe, expectedOutput:=expectedOutput)
            CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe, expectedOutput:=expectedOutput)
        End Sub

    End Class
End Namespace
