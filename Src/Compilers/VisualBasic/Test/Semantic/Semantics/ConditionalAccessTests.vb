' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class ConditionalAccessTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub DisabledIfNotExperimental()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test2(x As S1?)
        System.Console.WriteLine(x?.M2())
        System.Console.WriteLine(?.M2())
        ?.M2()
    End Sub
End Module

Structure S1
    Sub M2()
        System.Console.WriteLine("S1.M2")
    End Sub
End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC36637: The '?' character cannot be used here.
        System.Console.WriteLine(x?.M2())
                                  ~
BC30201: Expression expected.
        System.Console.WriteLine(?.M2())
                                 ~
BC36637: The '?' character cannot be used here.
        System.Console.WriteLine(?.M2())
                                 ~
BC31003: Expression statement is only allowed at the end of an interactive submission.
        ?.M2()
        ~~~~~~
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
        ?.M2()
         ~~~
</expected>)
        End Sub

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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
  IL_0007:  callvirt   "Function C1.get_P2() As String"
  IL_000c:  call       "Sub Module1.Do(Of String)(String)"
  IL_0011:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_IL_02",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (C1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  brtrue.s   IL_0009
  IL_0006:  ldnull
  IL_0007:  br.s       IL_000f
  IL_0009:  ldloc.0
  IL_000a:  callvirt   "Function C1.get_P2() As String"
  IL_000f:  call       "Sub Module1.Do(Of String)(String)"
  IL_0014:  ret
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
  IL_0007:  callvirt   "Function C1.get_P2() As String"
  IL_000c:  call       "Sub Module1.Do(Of String)(String)"
  IL_0011:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_IL_04",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (C1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  brtrue.s   IL_0009
  IL_0006:  ldnull
  IL_0007:  br.s       IL_000f
  IL_0009:  ldloc.0
  IL_000a:  callvirt   "Function C1.get_P2() As String"
  IL_000f:  call       "Sub Module1.Do(Of String)(String)"
  IL_0014:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_IL_05",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (C1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.ref
  IL_0003:  dup
  IL_0004:  stloc.0
  IL_0005:  brtrue.s   IL_000a
  IL_0007:  ldnull
  IL_0008:  br.s       IL_0010
  IL_000a:  ldloc.0
  IL_000b:  callvirt   "Function C1.get_P2() As String"
  IL_0010:  call       "Sub Module1.Do(Of String)(String)"
  IL_0015:  ret
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
  // Code size       57 (0x39)
  .maxstack  1
  .locals init (T V_0,
                T& V_1,
                T V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldloca.s   V_2
  IL_0004:  initobj    "T"
  IL_000a:  ldloc.2
  IL_000b:  box        "T"
  IL_0010:  brtrue.s   IL_0027
  IL_0012:  ldloc.1
  IL_0013:  ldobj      "T"
  IL_0018:  stloc.0
  IL_0019:  ldloca.s   V_0
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  box        "T"
  IL_0022:  brtrue.s   IL_0027
  IL_0024:  ldnull
  IL_0025:  br.s       IL_0033
  IL_0027:  ldloc.1
  IL_0028:  constrained. "T"
  IL_002e:  callvirt   "Function I1.get_P2() As String"
  IL_0033:  call       "Sub Module1.Do(Of String)(String)"
  IL_0038:  ret
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
  // Code size       57 (0x39)
  .maxstack  1
  .locals init (T V_0,
                T& V_1,
                T V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldloca.s   V_2
  IL_0004:  initobj    "T"
  IL_000a:  ldloc.2
  IL_000b:  box        "T"
  IL_0010:  brtrue.s   IL_0027
  IL_0012:  ldloc.1
  IL_0013:  ldobj      "T"
  IL_0018:  stloc.0
  IL_0019:  ldloca.s   V_0
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  box        "T"
  IL_0022:  brtrue.s   IL_0027
  IL_0024:  ldnull
  IL_0025:  br.s       IL_0033
  IL_0027:  ldloc.1
  IL_0028:  constrained. "T"
  IL_002e:  callvirt   "Function I1.get_P2() As String"
  IL_0033:  call       "Sub Module1.Do(Of String)(String)"
  IL_0038:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test6_IL_05",
            <![CDATA[
{
  // Code size       76 (0x4c)
  .maxstack  2
  .locals init (T V_0,
                T V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    "T"
  IL_0008:  ldloc.1
  IL_0009:  box        "T"
  IL_000e:  brtrue.s   IL_0032
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.0
  IL_0012:  ldelem     "T"
  IL_0017:  dup
  IL_0018:  stloc.0
  IL_0019:  box        "T"
  IL_001e:  brtrue.s   IL_0023
  IL_0020:  ldnull
  IL_0021:  br.s       IL_0046
  IL_0023:  ldloca.s   V_0
  IL_0025:  constrained. "T"
  IL_002b:  callvirt   "Function I1.get_P2() As String"
  IL_0030:  br.s       IL_0046
  IL_0032:  ldarg.0
  IL_0033:  ldc.i4.0
  IL_0034:  readonly.
  IL_0036:  ldelema    "T"
  IL_003b:  constrained. "T"
  IL_0041:  callvirt   "Function I1.get_P2() As String"
  IL_0046:  call       "Sub Module1.Do(Of String)(String)"
  IL_004b:  ret
}]]>)


            verifier.VerifyIL("Module1.Test8",
            <![CDATA[
{
  // Code size       65 (0x41)
  .maxstack  1
  .locals init (T V_0,
                T& V_1,
                T V_2,
                T V_3)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.GetT(Of T)(T) As T"
  IL_0006:  stloc.2
  IL_0007:  ldloca.s   V_2
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_3
  IL_000c:  initobj    "T"
  IL_0012:  ldloc.3
  IL_0013:  box        "T"
  IL_0018:  brtrue.s   IL_002f
  IL_001a:  ldloc.1
  IL_001b:  ldobj      "T"
  IL_0020:  stloc.0
  IL_0021:  ldloca.s   V_0
  IL_0023:  stloc.1
  IL_0024:  ldloc.0
  IL_0025:  box        "T"
  IL_002a:  brtrue.s   IL_002f
  IL_002c:  ldnull
  IL_002d:  br.s       IL_003b
  IL_002f:  ldloc.1
  IL_0030:  constrained. "T"
  IL_0036:  callvirt   "Function I1.get_P2() As String"
  IL_003b:  call       "Sub Module1.Do(Of String)(String)"
  IL_0040:  ret
}]]>)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC37238: 'T' cannot be made nullable.
        Dim y1 = x1?.M1()
                    ~~~~~
</expected>)
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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
        Try
            x?.P2(0)
        Catch e As System.Exception
            System.Console.WriteLine(e.Message)
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ExperimentalReleaseExe, parseOptions:=TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, {SystemCoreRef}, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, {SystemCoreRef}, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, {SystemCoreRef}, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, {SystemCoreRef}, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

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

    Sub Test5(Of T As I1)(x as C1(Of T))
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, {SystemCoreRef},
                                                                                         TestOptions.ExperimentalReleaseExe.WithOptionStrict(OptionStrict.Custom),
                                                                                         TestOptions.ExperimentalReleaseExe.ParseOptions)


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
  // Code size       15 (0xf)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_000e
  IL_0008:  ldarg.0
  IL_0009:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_000e:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1_2",
            <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  1
  .locals init (T V_0,
                T& V_1,
                T V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldloca.s   V_2
  IL_0004:  initobj    "T"
  IL_000a:  ldloc.2
  IL_000b:  box        "T"
  IL_0010:  brtrue.s   IL_0024
  IL_0012:  ldloc.1
  IL_0013:  ldobj      "T"
  IL_0018:  stloc.0
  IL_0019:  ldloca.s   V_0
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  box        "T"
  IL_0022:  brfalse.s  IL_002f
  IL_0024:  ldloc.1
  IL_0025:  ldobj      "T"
  IL_002a:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_002f:  ret
}]]>)

            verifier.VerifyIL("Module1.Test1_3",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_000e
  IL_0008:  ldarg.0
  IL_0009:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_000e:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1_4",
            <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  1
  .locals init (T V_0,
                T& V_1,
                T V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldloca.s   V_2
  IL_0004:  initobj    "T"
  IL_000a:  ldloc.2
  IL_000b:  box        "T"
  IL_0010:  brtrue.s   IL_0024
  IL_0012:  ldloc.1
  IL_0013:  ldobj      "T"
  IL_0018:  stloc.0
  IL_0019:  ldloca.s   V_0
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  box        "T"
  IL_0022:  brfalse.s  IL_002f
  IL_0024:  ldloc.1
  IL_0025:  ldobj      "T"
  IL_002a:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_002f:  ret
}]]>)

            verifier.VerifyIL("Module1.Test1_5",
            <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (T V_0,
                T V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    "T"
  IL_0008:  ldloc.1
  IL_0009:  box        "T"
  IL_000e:  brtrue.s   IL_0027
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.0
  IL_0012:  ldelem     "T"
  IL_0017:  dup
  IL_0018:  stloc.0
  IL_0019:  box        "T"
  IL_001e:  brfalse.s  IL_0033
  IL_0020:  ldloc.0
  IL_0021:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_0026:  ret
  IL_0027:  ldarg.0
  IL_0028:  ldc.i4.0
  IL_0029:  ldelem     "T"
  IL_002e:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_0033:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1_6",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  1
  .locals init (T V_0,
                T& V_1,
                T V_2,
                T V_3)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.GetT(Of T)(T) As T"
  IL_0006:  stloc.2
  IL_0007:  ldloca.s   V_2
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_3
  IL_000c:  initobj    "T"
  IL_0012:  ldloc.3
  IL_0013:  box        "T"
  IL_0018:  brtrue.s   IL_002c
  IL_001a:  ldloc.1
  IL_001b:  ldobj      "T"
  IL_0020:  stloc.0
  IL_0021:  ldloca.s   V_0
  IL_0023:  stloc.1
  IL_0024:  ldloc.0
  IL_0025:  box        "T"
  IL_002a:  brfalse.s  IL_0037
  IL_002c:  ldloc.1
  IL_002d:  ldobj      "T"
  IL_0032:  call       "Sub Module1.Ext1(Of T)(T)"
  IL_0037:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2_1",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_0011
  IL_0008:  ldarg.0
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       "Sub Module1.Ext2(Of T)(ByRef T)"
  IL_0011:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2_2",
            <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (T V_0,
                T& V_1,
                T V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldloca.s   V_2
  IL_0004:  initobj    "T"
  IL_000a:  ldloc.2
  IL_000b:  box        "T"
  IL_0010:  brtrue.s   IL_0024
  IL_0012:  ldloc.1
  IL_0013:  ldobj      "T"
  IL_0018:  stloc.0
  IL_0019:  ldloca.s   V_0
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  box        "T"
  IL_0022:  brfalse.s  IL_0032
  IL_0024:  ldloc.1
  IL_0025:  ldobj      "T"
  IL_002a:  stloc.2
  IL_002b:  ldloca.s   V_2
  IL_002d:  call       "Sub Module1.Ext2(Of T)(ByRef T)"
  IL_0032:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2_3",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_0011
  IL_0008:  ldarg.0
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       "Sub Module1.Ext2(Of T)(ByRef T)"
  IL_0011:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2_4",
            <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (T V_0,
                T& V_1,
                T V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldloca.s   V_2
  IL_0004:  initobj    "T"
  IL_000a:  ldloc.2
  IL_000b:  box        "T"
  IL_0010:  brtrue.s   IL_0024
  IL_0012:  ldloc.1
  IL_0013:  ldobj      "T"
  IL_0018:  stloc.0
  IL_0019:  ldloca.s   V_0
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  box        "T"
  IL_0022:  brfalse.s  IL_0032
  IL_0024:  ldloc.1
  IL_0025:  ldobj      "T"
  IL_002a:  stloc.2
  IL_002b:  ldloca.s   V_2
  IL_002d:  call       "Sub Module1.Ext2(Of T)(ByRef T)"
  IL_0032:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2_5",
            <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (T V_0,
                T V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    "T"
  IL_0008:  ldloc.1
  IL_0009:  box        "T"
  IL_000e:  brtrue.s   IL_002a
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.0
  IL_0012:  ldelem     "T"
  IL_0017:  dup
  IL_0018:  stloc.0
  IL_0019:  box        "T"
  IL_001e:  brfalse.s  IL_0039
  IL_0020:  ldloc.0
  IL_0021:  stloc.1
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       "Sub Module1.Ext2(Of T)(ByRef T)"
  IL_0029:  ret
  IL_002a:  ldarg.0
  IL_002b:  ldc.i4.0
  IL_002c:  ldelem     "T"
  IL_0031:  stloc.1
  IL_0032:  ldloca.s   V_1
  IL_0034:  call       "Sub Module1.Ext2(Of T)(ByRef T)"
  IL_0039:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3_1",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_0018
  IL_0008:  ldarg.0
  IL_0009:  box        "T"
  IL_000e:  castclass  "I1"
  IL_0013:  call       "Sub Module1.Ext3(I1)"
  IL_0018:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3_2",
            <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  1
  .locals init (T V_0,
                T& V_1,
                T V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldloca.s   V_2
  IL_0004:  initobj    "T"
  IL_000a:  ldloc.2
  IL_000b:  box        "T"
  IL_0010:  brtrue.s   IL_0024
  IL_0012:  ldloc.1
  IL_0013:  ldobj      "T"
  IL_0018:  stloc.0
  IL_0019:  ldloca.s   V_0
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  box        "T"
  IL_0022:  brfalse.s  IL_0039
  IL_0024:  ldloc.1
  IL_0025:  ldobj      "T"
  IL_002a:  box        "T"
  IL_002f:  castclass  "I1"
  IL_0034:  call       "Sub Module1.Ext3(I1)"
  IL_0039:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_1",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  1
  .locals init (I1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_001b
  IL_0008:  ldarg.0
  IL_0009:  box        "T"
  IL_000e:  castclass  "I1"
  IL_0013:  stloc.0
  IL_0014:  ldloca.s   V_0
  IL_0016:  call       "Sub Module1.Ext4(ByRef I1)"
  IL_001b:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_2",
            <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  1
  .locals init (T V_0,
                T& V_1,
                T V_2,
                I1 V_3)
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldloca.s   V_2
  IL_0004:  initobj    "T"
  IL_000a:  ldloc.2
  IL_000b:  box        "T"
  IL_0010:  brtrue.s   IL_0024
  IL_0012:  ldloc.1
  IL_0013:  ldobj      "T"
  IL_0018:  stloc.0
  IL_0019:  ldloca.s   V_0
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  box        "T"
  IL_0022:  brfalse.s  IL_003c
  IL_0024:  ldloc.1
  IL_0025:  ldobj      "T"
  IL_002a:  box        "T"
  IL_002f:  castclass  "I1"
  IL_0034:  stloc.3
  IL_0035:  ldloca.s   V_3
  IL_0037:  call       "Sub Module1.Ext4(ByRef I1)"
  IL_003c:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_3",
            <![CDATA[
{
  // Code size       78 (0x4e)
  .maxstack  2
  .locals init (T V_0,
                T V_1,
                I1 V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    "T"
  IL_0008:  ldloc.1
  IL_0009:  box        "T"
  IL_000e:  brtrue.s   IL_0034
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.0
  IL_0012:  ldelem     "T"
  IL_0017:  dup
  IL_0018:  stloc.0
  IL_0019:  box        "T"
  IL_001e:  brfalse.s  IL_004d
  IL_0020:  ldloc.0
  IL_0021:  box        "T"
  IL_0026:  castclass  "I1"
  IL_002b:  stloc.2
  IL_002c:  ldloca.s   V_2
  IL_002e:  call       "Sub Module1.Ext4(ByRef I1)"
  IL_0033:  ret
  IL_0034:  ldarg.0
  IL_0035:  ldc.i4.0
  IL_0036:  ldelem     "T"
  IL_003b:  box        "T"
  IL_0040:  castclass  "I1"
  IL_0045:  stloc.2
  IL_0046:  ldloca.s   V_2
  IL_0048:  call       "Sub Module1.Ext4(ByRef I1)"
  IL_004d:  ret
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, {SystemCoreRef},
                                                                                         TestOptions.ExperimentalReleaseExe.WithOptionStrict(OptionStrict.Custom),
                                                                                         TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ExperimentalReleaseExe, parseOptions:=TestOptions.ExperimentalReleaseExe.ParseOptions)

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
        System.Console.WriteLine("{0} <-- This number should be 2 (see Bug #1026678)", s1(0).F1)

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

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ExperimentalReleaseExe, parseOptions:=TestOptions.ExperimentalReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
---
1
S1.CallAsync
1
1 <-- This number should be 2 (see Bug #1026678)
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

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ExperimentalReleaseExe, parseOptions:=TestOptions.ExperimentalReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation)

            verifier.VerifyIL("Module1.VB$StateMachine_0_Test1(Of SM$T).MoveNext",
            <![CDATA[
{
  // Code size      168 (0xa8)
  .maxstack  3
  .locals init (Short? V_0,
                Integer V_1,
                SM$T V_2, //
                SM$T V_3,
                Short? V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Friend $State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloca.s   V_3
    IL_0009:  initobj    "SM$T"
    IL_000f:  ldloc.3
    IL_0010:  box        "SM$T"
    IL_0015:  brtrue.s   IL_004c
    IL_0017:  ldarg.0
    IL_0018:  ldfld      "Friend $VB$Local_x As SM$T()"
    IL_001d:  ldc.i4.0
    IL_001e:  ldelem     "SM$T"
    IL_0023:  dup
    IL_0024:  stloc.2
    IL_0025:  box        "SM$T"
    IL_002a:  brtrue.s   IL_0038
    IL_002c:  ldloca.s   V_4
    IL_002e:  initobj    "Short?"
    IL_0034:  ldloc.s    V_4
    IL_0036:  br.s       IL_006a
    IL_0038:  ldloca.s   V_2
    IL_003a:  constrained. "SM$T"
    IL_0040:  callvirt   "Function I1.CallAsync() As Short"
    IL_0045:  newobj     "Sub Short?..ctor(Short)"
    IL_004a:  br.s       IL_006a
    IL_004c:  ldarg.0
    IL_004d:  ldfld      "Friend $VB$Local_x As SM$T()"
    IL_0052:  ldc.i4.0
    IL_0053:  readonly.
    IL_0055:  ldelema    "SM$T"
    IL_005a:  constrained. "SM$T"
    IL_0060:  callvirt   "Function I1.CallAsync() As Short"
    IL_0065:  newobj     "Sub Short?..ctor(Short)"
    IL_006a:  stloc.0
    IL_006b:  leave.s    IL_0091
  }
  catch System.Exception
  {
    IL_006d:  dup
    IL_006e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0073:  stloc.s    V_5
    IL_0075:  ldarg.0
    IL_0076:  ldc.i4.s   -2
    IL_0078:  stfld      "Friend $State As Integer"
    IL_007d:  ldarg.0
    IL_007e:  ldflda     "Friend $Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Short?)"
    IL_0083:  ldloc.s    V_5
    IL_0085:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Short?).SetException(System.Exception)"
    IL_008a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_008f:  leave.s    IL_00a7
  }
  IL_0091:  ldarg.0
  IL_0092:  ldc.i4.s   -2
  IL_0094:  dup
  IL_0095:  stloc.1
  IL_0096:  stfld      "Friend $State As Integer"
  IL_009b:  ldarg.0
  IL_009c:  ldflda     "Friend $Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Short?)"
  IL_00a1:  ldloc.0
  IL_00a2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Short?).SetResult(Short?)"
  IL_00a7:  ret
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ExperimentalReleaseExe, parseOptions:=TestOptions.ExperimentalReleaseExe.ParseOptions)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

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

    End Class
End Namespace