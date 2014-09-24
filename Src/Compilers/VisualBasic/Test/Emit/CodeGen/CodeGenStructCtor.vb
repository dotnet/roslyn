' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenStructCtor
        Inherits BasicTestBase

        <Fact()>
        Public Sub ParameterlessCtor001()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        

Structure S1
    Public x as integer
    Public y as integer
    
    public Sub New()
        x = 42
    end sub

    public Sub New(dummy as integer)
    end sub

end structure 

Module M1
    Sub Main()
        dim s as new S1()
        Console.WriteLine(s.x)

        s.y = 333
        s = new S1()
        Console.WriteLine(s.y)

        s = new S1(3)
        Console.WriteLine(s.x)
        Console.WriteLine(s.y)

    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[
42
0
0
0
]]>).VerifyIL("S1..ctor()",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    "S1"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   42
  IL_000a:  stfld      "S1.x As Integer"
  IL_000f:  ret
}
]]>).VerifyIL("S1..ctor(Integer)",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    "S1"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ParameterlessCtor002()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        

Structure S1
    Public x as integer
    Public y as integer
    
    public Sub New()
        x = 42
    end sub

    public Sub New(dummy as integer)
        Me.New
    end sub

end structure 

Module M1
    Sub Main()
        dim s as new S1()
        Console.WriteLine(s.x)

        s.y = 333
        s = new S1()
        Console.WriteLine(s.y)

        s = new S1(3)
        Console.WriteLine(s.x)
        Console.WriteLine(s.y)

    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[
42
0
42
0
]]>).VerifyIL("S1..ctor()",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    "S1"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   42
  IL_000a:  stfld      "S1.x As Integer"
  IL_000f:  ret
}
]]>).VerifyIL("S1..ctor(Integer)",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub S1..ctor()"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ParameterlessCtor003()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        

Structure S1
    Public x as integer
    Public y as integer
    
    public Sub New(x as integer)
        Me.New       
        me.x = x
        me.y = me.x + 1
    end sub

end structure 

Module M1
    Sub Main()
        dim s as new S1()
        Console.WriteLine(s.x)

        s.y = 333
        s = new S1()
        Console.WriteLine(s.y)

        s = new S1(3)
        Console.WriteLine(s.x)
        Console.WriteLine(s.y)

    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[
0
0
3
4
]]>).VerifyIL("S1..ctor(Integer)",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  initobj    "S1"
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      "S1.x As Integer"
  IL_000e:  ldarg.0
  IL_000f:  ldarg.0
  IL_0010:  ldfld      "S1.x As Integer"
  IL_0015:  ldc.i4.1
  IL_0016:  add.ovf
  IL_0017:  stfld      "S1.y As Integer"
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ParameterlessCtor004()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System        
Imports System.Linq.Expressions

Structure S1
    Public x as integer
    Public y as integer
    
    public Sub New()
        x = 42
    end sub
end structure 

Module M1
    Sub Main()
        Dim testExpr as Expression(of Func(of S1)) = Function()new S1()
        System.Console.Write(testExpr.Compile()().x)
    End Sub
End Module

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, <![CDATA[42]]>)

        End Sub

    End Class
End Namespace
