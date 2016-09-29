' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class LoadingOperators
        Inherits BasicTestBase

        <Fact()>
        Public Sub Import1()
            Dim customIL =
            <![CDATA[
.class public auto ansi beforefieldinit A1
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A1  op_Addition(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Addition

  .method public hidebysig specialname static 
          class A1  op_BitwiseAnd(class A1 x,
                                  class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_BitwiseAnd

  .method public hidebysig specialname static 
          class A1  op_LogicalAnd(class A1 x,
                                  class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalAnd

  .method public hidebysig specialname static 
          class A1  op_BitwiseOr(class A1 x,
                                 class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_BitwiseOr

  .method public hidebysig specialname static 
          class A1  op_LogicalOr(class A1 x,
                                 class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalOr

  .method public hidebysig specialname static 
          class A1  op_Concatenate(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Concatenate

  .method public hidebysig specialname static 
          class A1  op_Division(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Division

  .method public hidebysig specialname static 
          class A1  op_Equality(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Equality

  .method public hidebysig specialname static 
          class A1  op_ExclusiveOr(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_ExclusiveOr

  .method public hidebysig specialname static 
          uint8  op_Explicit(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Explicit

  .method public hidebysig specialname static 
          class A1  op_Exponent(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Exponent

  .method public hidebysig specialname static 
          bool  op_False(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] bool CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_False

  .method public hidebysig specialname static 
          class A1  op_GreaterThan(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_GreaterThan

  .method public hidebysig specialname static 
          class A1  op_GreaterThanOrEqual(class A1 x,
                                          class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_GreaterThanOrEqual

  .method public hidebysig specialname static 
          int32  op_Implicit(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] int32 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Implicit

  .method public hidebysig specialname static 
          class A1  op_Inequality(class A1 x,
                                  class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Inequality

  .method public hidebysig specialname static 
          class A1  op_IntegerDivision(class A1 x,
                                       class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_IntegerDivision

  .method public hidebysig specialname static 
          class A1  op_LeftShift(class A1 x,
                                 int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LeftShift

  .method public hidebysig specialname static 
          class A1  op_UnsignedLeftShift(class A1 x,
                                         int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnsignedLeftShift

  .method public hidebysig specialname static 
          class A1  op_LessThan(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LessThan

  .method public hidebysig specialname static 
          class A1  op_LessThanOrEqual(class A1 x,
                                       class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LessThanOrEqual

  .method public hidebysig specialname static 
          class A1  op_Like(class A1 x,
                            class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Like

  .method public hidebysig specialname static 
          class A1  op_Modulus(class A1 x,
                               class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Modulus

  .method public hidebysig specialname static 
          class A1  op_Multiply(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Multiply

  .method public hidebysig specialname static 
          class A1  op_OnesComplement(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_OnesComplement

  .method public hidebysig specialname static 
          class A1  op_LogicalNot(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalNot

  .method public hidebysig specialname static 
          class A1  op_RightShift(class A1 x,
                                  int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_RightShift

  .method public hidebysig specialname static 
          class A1  op_UnsignedRightShift(class A1 x,
                                          int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnsignedRightShift

  .method public hidebysig specialname static 
          class A1  op_Subtraction(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Subtraction

  .method public hidebysig specialname static 
          bool  op_True(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] bool CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_True

  .method public hidebysig specialname static 
          class A1  op_UnaryNegation(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnaryNegation

  .method public hidebysig specialname static 
          class A1  op_UnaryPlus(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnaryPlus

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A1::.ctor

} // end of class A1
]]>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim baseLine As BaseLine() = {
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator +(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator And(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function op_LogicalAnd(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Or(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function op_LogicalOr(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator &(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator /(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator =(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Xor(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Conversion, "Public Shared Overloads Narrowing Operator CType(x As A1) As Byte"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator ^(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator IsFalse(x As A1) As Boolean"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator >(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator >=(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Conversion, "Public Shared Overloads Widening Operator CType(x As A1) As Integer"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <>(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator \(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <<(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function op_UnsignedLeftShift(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <=(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Like(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Mod(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator *(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Not(x As A1) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function op_LogicalNot(x As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator >>(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function op_UnsignedRightShift(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator -(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator IsTrue(x As A1) As Boolean"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator -(x As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator +(x As A1) As A1")}


            Dim a1 = compilation.GetTypeByMetadataName("A1")
            Dim members As ImmutableArray(Of Symbol) = a1.GetMembers()

            For i = 0 To members.Length - 2
                Dim method = DirectCast(members(i), MethodSymbol)

                Assert.Equal(baseLine(i).Kind, method.MethodKind)

                Dim display As String = method.ToDisplayString()
                Assert.Equal(baseLine(i).Display, display)
                Assert.Equal("Function A1." & method.Name &
                             display.Substring(display.IndexOf("("c)).
                                Replace("Boolean", "System.Boolean").
                                Replace("Integer", "System.Int32").
                                Replace("Byte", "System.Byte"), method.ToTestDisplayString())
            Next
        End Sub

        Private Structure BaseLine
            Public ReadOnly Kind As MethodKind
            Public ReadOnly Display As String

            Public Sub New(kind As MethodKind, display As String)
                Me.Kind = kind
                Me.Display = display
            End Sub
        End Structure

        <Fact()>
        Public Sub Import2()
            Dim customIL =
            <![CDATA[
.class public auto ansi beforefieldinit A1
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A1  OP_ADDITION(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Addition

  .method public hidebysig specialname static 
          class A1  OP_BITWISEAND(class A1 x,
                                  class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_BitwiseAnd

  .method public hidebysig specialname static 
          class A1  OP_LOGICALAND(class A1 x,
                                  class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalAnd

  .method public hidebysig specialname static 
          class A1  OP_BITWISEOR(class A1 x,
                                 class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_BitwiseOr

  .method public hidebysig specialname static 
          class A1  OP_LOGICALOR(class A1 x,
                                 class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalOr

  .method public hidebysig specialname static 
          class A1  OP_CONCATENATE(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Concatenate

  .method public hidebysig specialname static 
          class A1  OP_DIVISION(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Division

  .method public hidebysig specialname static 
          class A1  OP_EQUALITY(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Equality

  .method public hidebysig specialname static 
          class A1  OP_EXCLUSIVEOR(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_ExclusiveOr

  .method public hidebysig specialname static 
          uint8  OP_EXPLICIT(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Explicit

  .method public hidebysig specialname static 
          class A1  OP_EXPONENT(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Exponent

  .method public hidebysig specialname static 
          bool  OP_FALSE(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] bool CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_False

  .method public hidebysig specialname static 
          class A1  OP_GREATERTHAN(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_GreaterThan

  .method public hidebysig specialname static 
          class A1  OP_GREATERTHANOREQUAL(class A1 x,
                                          class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_GreaterThanOrEqual

  .method public hidebysig specialname static 
          int32  OP_IMPLICIT(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] int32 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Implicit

  .method public hidebysig specialname static 
          class A1  OP_INEQUALITY(class A1 x,
                                  class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Inequality

  .method public hidebysig specialname static 
          class A1  OP_INTEGERDIVISION(class A1 x,
                                       class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_IntegerDivision

  .method public hidebysig specialname static 
          class A1  OP_LEFTSHIFT(class A1 x,
                                 int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LeftShift

  .method public hidebysig specialname static 
          class A1  OP_UNSIGNEDLEFTSHIFT(class A1 x,
                                         int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnsignedLeftShift

  .method public hidebysig specialname static 
          class A1  OP_LESSTHAN(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LessThan

  .method public hidebysig specialname static 
          class A1  OP_LESSTHANOREQUAL(class A1 x,
                                       class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LessThanOrEqual

  .method public hidebysig specialname static 
          class A1  OP_LIKE(class A1 x,
                            class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Like

  .method public hidebysig specialname static 
          class A1  OP_MODULUS(class A1 x,
                               class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Modulus

  .method public hidebysig specialname static 
          class A1  OP_MULTIPLY(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Multiply

  .method public hidebysig specialname static 
          class A1  OP_ONESCOMPLEMENT(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_OnesComplement

  .method public hidebysig specialname static 
          class A1  OP_LOGICALNOT(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalNot

  .method public hidebysig specialname static 
          class A1  OP_RIGHTSHIFT(class A1 x,
                                  int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_RightShift

  .method public hidebysig specialname static 
          class A1  OP_UNSIGNEDRIGHTSHIFT(class A1 x,
                                          int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnsignedRightShift

  .method public hidebysig specialname static 
          class A1  OP_SUBTRACTION(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Subtraction

  .method public hidebysig specialname static 
          bool  OP_TRUE(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] bool CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_True

  .method public hidebysig specialname static 
          class A1  OP_UNARYNEGATION(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnaryNegation

  .method public hidebysig specialname static 
          class A1  OP_UNARYPLUS(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnaryPlus

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A1::.ctor

} // end of class A1
]]>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim baseLine As BaseLine() = {
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator +(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator And(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function OP_LOGICALAND(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Or(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function OP_LOGICALOR(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator &(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator /(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator =(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Xor(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Conversion, "Public Shared Overloads Narrowing Operator CType(x As A1) As Byte"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator ^(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator IsFalse(x As A1) As Boolean"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator >(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator >=(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Conversion, "Public Shared Overloads Widening Operator CType(x As A1) As Integer"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <>(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator \(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <<(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function OP_UNSIGNEDLEFTSHIFT(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <=(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Like(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Mod(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator *(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Not(x As A1) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function OP_LOGICALNOT(x As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator >>(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function OP_UNSIGNEDRIGHTSHIFT(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator -(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator IsTrue(x As A1) As Boolean"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator -(x As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator +(x As A1) As A1")}


            Dim a1 = compilation.GetTypeByMetadataName("A1")
            Dim members As ImmutableArray(Of Symbol) = a1.GetMembers()

            For i = 0 To members.Length - 2
                Dim method = DirectCast(members(i), MethodSymbol)

                Assert.Equal(baseLine(i).Kind, method.MethodKind)

                Dim display As String = method.ToDisplayString()
                Assert.Equal(baseLine(i).Display, display)
                Assert.Equal("Function A1." & method.Name &
                             display.Substring(display.IndexOf("("c)).
                                Replace("Boolean", "System.Boolean").
                                Replace("Integer", "System.Int32").
                                Replace("Byte", "System.Byte"), method.ToTestDisplayString())
            Next
        End Sub

        <Fact()>
        Public Sub Import3()
            Dim customIL =
            <![CDATA[
.class public auto ansi beforefieldinit A1
       extends [mscorlib]System.Object
{
  .method family hidebysig specialname static 
          class A1  op_Addition(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Addition

  .method public hidebysig specialname instance class A1 
          op_BitwiseAnd(class A1 x,
                        class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_BitwiseAnd

  .method public hidebysig static class A1 
          op_BitwiseOr(class A1 x,
                       class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_BitwiseOr

  .method public hidebysig specialname static 
          void  op_Concatenate(class A1 x,
                               class A1 y) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method A1::op_Concatenate

  .method public hidebysig specialname static 
          class A1  op_Division<T>(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Division

  .method public hidebysig specialname static 
          class A1  op_Equality(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Equality

  .method public hidebysig specialname static 
          class A1  op_ExclusiveOr(class A1 x,
                                   class A1 y,
                                   class A1 z) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_ExclusiveOr

  .method public hidebysig specialname static 
          uint8  op_Explicit(class A1 x,
                             class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Explicit

  .method public hidebysig specialname static 
          bool  op_False() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] bool CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_False

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A1::.ctor

} // end of class A1
]]>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)


            Dim a1 = compilation.GetTypeByMetadataName("A1")
            Dim members As ImmutableArray(Of Symbol) = a1.GetMembers()

            For i = 0 To members.Length - 2
                Dim method = DirectCast(members(i), MethodSymbol)
                Assert.Equal(MethodKind.Ordinary, method.MethodKind)
            Next
        End Sub

        <Fact()>
        Public Sub Import4()
            Dim customIL =
            <![CDATA[
.class public auto ansi beforefieldinit sealed A1
       extends [mscorlib]System.ValueType
{
  .method public hidebysig specialname static 
          class A1  op_Addition(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Addition

  .method public hidebysig specialname static 
          class A1  op_BitwiseAnd(class A1 x,
                                  class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_BitwiseAnd

  .method public hidebysig specialname static 
          class A1  op_LogicalAnd(class A1 x,
                                  class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalAnd

  .method public hidebysig specialname static 
          class A1  op_BitwiseOr(class A1 x,
                                 class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_BitwiseOr

  .method public hidebysig specialname static 
          class A1  op_LogicalOr(class A1 x,
                                 class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalOr

  .method public hidebysig specialname static 
          class A1  op_Concatenate(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Concatenate

  .method public hidebysig specialname static 
          class A1  op_Division(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Division

  .method public hidebysig specialname static 
          class A1  op_Equality(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Equality

  .method public hidebysig specialname static 
          class A1  op_ExclusiveOr(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_ExclusiveOr

  .method public hidebysig specialname static 
          uint8  op_Explicit(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Explicit

  .method public hidebysig specialname static 
          class A1  op_Exponent(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Exponent

  .method public hidebysig specialname static 
          bool  op_False(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] bool CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_False

  .method public hidebysig specialname static 
          class A1  op_GreaterThan(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_GreaterThan

  .method public hidebysig specialname static 
          class A1  op_GreaterThanOrEqual(class A1 x,
                                          class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_GreaterThanOrEqual

  .method public hidebysig specialname static 
          int32  op_Implicit(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] int32 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Implicit

  .method public hidebysig specialname static 
          class A1  op_Inequality(class A1 x,
                                  class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Inequality

  .method public hidebysig specialname static 
          class A1  op_IntegerDivision(class A1 x,
                                       class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_IntegerDivision

  .method public hidebysig specialname static 
          class A1  op_LeftShift(class A1 x,
                                 int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LeftShift

  .method public hidebysig specialname static 
          class A1  op_UnsignedLeftShift(class A1 x,
                                         int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnsignedLeftShift

  .method public hidebysig specialname static 
          class A1  op_LessThan(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LessThan

  .method public hidebysig specialname static 
          class A1  op_LessThanOrEqual(class A1 x,
                                       class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LessThanOrEqual

  .method public hidebysig specialname static 
          class A1  op_Like(class A1 x,
                            class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Like

  .method public hidebysig specialname static 
          class A1  op_Modulus(class A1 x,
                               class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Modulus

  .method public hidebysig specialname static 
          class A1  op_Multiply(class A1 x,
                                class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Multiply

  .method public hidebysig specialname static 
          class A1  op_OnesComplement(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_OnesComplement

  .method public hidebysig specialname static 
          class A1  op_LogicalNot(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalNot

  .method public hidebysig specialname static 
          class A1  op_RightShift(class A1 x,
                                  int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_RightShift

  .method public hidebysig specialname static 
          class A1  op_UnsignedRightShift(class A1 x,
                                          int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnsignedRightShift

  .method public hidebysig specialname static 
          class A1  op_Subtraction(class A1 x,
                                   class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_Subtraction

  .method public hidebysig specialname static 
          bool  op_True(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] bool CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_True

  .method public hidebysig specialname static 
          class A1  op_UnaryNegation(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnaryNegation

  .method public hidebysig specialname static 
          class A1  op_UnaryPlus(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnaryPlus

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A1::.ctor

} // end of class A1
]]>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim baseLine As BaseLine() = {
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator +(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator And(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function op_LogicalAnd(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Or(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function op_LogicalOr(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator &(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator /(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator =(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Xor(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Conversion, "Public Shared Overloads Narrowing Operator CType(x As A1) As Byte"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator ^(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator IsFalse(x As A1) As Boolean"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator >(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator >=(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.Conversion, "Public Shared Overloads Widening Operator CType(x As A1) As Integer"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <>(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator \(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <<(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function op_UnsignedLeftShift(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <=(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Like(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Mod(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator *(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Not(x As A1) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function op_LogicalNot(x As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator >>(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.Ordinary, "Public Shared Overloads Function op_UnsignedRightShift(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator -(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator IsTrue(x As A1) As Boolean"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator -(x As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator +(x As A1) As A1")}


            Dim a1 = compilation.GetTypeByMetadataName("A1")
            Dim members As ImmutableArray(Of Symbol) = a1.GetMembers()

            For i = 0 To members.Length - 2
                Dim method = DirectCast(members(i), MethodSymbol)

                Assert.Equal(baseLine(i).Kind, method.MethodKind)

                Dim display As String = method.ToDisplayString()
                Assert.Equal(baseLine(i).Display, display)
                Assert.Equal("Function A1." & method.Name &
                             display.Substring(display.IndexOf("("c)).
                                Replace("Boolean", "System.Boolean").
                                Replace("Integer", "System.Int32").
                                Replace("Byte", "System.Byte"), method.ToTestDisplayString())
            Next
        End Sub

        <Fact()>
        Public Sub Import5()
            Dim customIL =
            <![CDATA[
.class public auto ansi beforefieldinit A1
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A1  op_LogicalAnd(class A1 x,
                                  class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalAnd

  .method public hidebysig specialname static 
          class A1  op_LogicalOr(class A1 x,
                                 class A1 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalOr

  .method public hidebysig specialname static 
          class A1  op_UnsignedLeftShift(class A1 x,
                                         int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnsignedLeftShift

  .method public hidebysig specialname static 
          class A1  op_LogicalNot(class A1 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_LogicalNot

  .method public hidebysig specialname static 
          class A1  op_UnsignedRightShift(class A1 x,
                                          int32 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A1 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A1::op_UnsignedRightShift

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A1::.ctor

} // end of class A1
]]>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim baseLine As BaseLine() = {
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator And(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Or(x As A1, y As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator <<(x As A1, y As Integer) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator Not(x As A1) As A1"),
                     New BaseLine(MethodKind.UserDefinedOperator, "Public Shared Overloads Operator >>(x As A1, y As Integer) As A1")}


            Dim a1 = compilation.GetTypeByMetadataName("A1")
            Dim members As ImmutableArray(Of Symbol) = a1.GetMembers()

            For i = 0 To members.Length - 2
                Dim method = DirectCast(members(i), MethodSymbol)

                Assert.Equal(baseLine(i).Kind, method.MethodKind)

                Dim display As String = method.ToDisplayString()
                Assert.Equal(baseLine(i).Display, display)
                Assert.Equal("Function A1." & method.Name &
                             display.Substring(display.IndexOf("("c)).
                                Replace("Boolean", "System.Boolean").
                                Replace("Integer", "System.Int32").
                                Replace("Byte", "System.Byte"), method.ToTestDisplayString())
            Next
        End Sub

        <Fact>
        Public Sub Not1()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A11
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A11  op_OnesComplement(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_OnesComplement"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_OnesComplement

  .method public hidebysig specialname static 
          class A11  OP_ONESCOMPLEMENT(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "OP_ONESCOMPLEMENT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::OP_ONESCOMPLEMENT

  .method public hidebysig specialname static 
          class A11  op_LogicalNot(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalNot"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_LogicalNot

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A11::.ctor

} // end of class A11
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim a11 = compilation.GetTypeByMetadataName("A11")

            Assert.Equal(MethodKind.Ordinary, DirectCast(a11.GetMembers("op_LogicalNot").Single(), MethodSymbol).MethodKind)

            For Each m In a11.GetMembers("op_OnesComplement")
                Assert.Equal(MethodKind.Ordinary, DirectCast(m, MethodSymbol).MethodKind)
            Next
        End Sub

        <Fact>
        Public Sub Not2()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A11
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A11  op_OnesComplement(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_OnesComplement"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_OnesComplement

  .method public hidebysig specialname static 
          class A11  OP_ONESCOMPLEMENT(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "OP_ONESCOMPLEMENT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::OP_ONESCOMPLEMENT

  .method public hidebysig specialname static 
          class A11  op_LogicalNot(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalNot"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_LogicalNot

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A11::.ctor

} // end of class A11
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim a11 = compilation.GetTypeByMetadataName("A11")

            Assert.Equal(MethodKind.Ordinary, DirectCast(a11.GetMembers().Where(Function(m) m.Name.Equals("op_OnesComplement")).Single(), MethodSymbol).MethodKind)
            Assert.Equal(MethodKind.Ordinary, DirectCast(a11.GetMembers().Where(Function(m) m.Name.Equals("OP_ONESCOMPLEMENT")).Single(), MethodSymbol).MethodKind)
            Assert.Equal(MethodKind.Ordinary, DirectCast(a11.GetMembers("op_LogicalNot").Single(), MethodSymbol).MethodKind)
        End Sub

        <Fact>
        Public Sub Not3()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A11
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A11  op_OnesComplement(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_OnesComplement"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_OnesComplement

  .method public hidebysig specialname static 
          class A11  OP_ONESCOMPLEMENT(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "OP_ONESCOMPLEMENT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::OP_ONESCOMPLEMENT

  .method public hidebysig specialname static 
          class A11  op_LogicalNot(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalNot"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_LogicalNot

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A11::.ctor

} // end of class A11
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim a11 = compilation.GetTypeByMetadataName("A11")

            Assert.Equal(MethodKind.Ordinary, DirectCast(a11.GetMembers().Where(Function(m) m.Name.Equals("OP_ONESCOMPLEMENT")).Single(), MethodSymbol).MethodKind)
            Assert.Equal(MethodKind.Ordinary, DirectCast(a11.GetMembers().Where(Function(m) m.Name.Equals("op_OnesComplement")).Single(), MethodSymbol).MethodKind)
            Assert.Equal(MethodKind.Ordinary, DirectCast(a11.GetMembers("op_LogicalNot").Single(), MethodSymbol).MethodKind)
        End Sub

        <Fact>
        Public Sub Not4()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A11
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A11  OP_ONESCOMPLEMENT(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "OP_ONESCOMPLEMENT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::OP_ONESCOMPLEMENT

  .method public hidebysig specialname static 
          class A11  op_LogicalNot(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalNot"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_LogicalNot

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A11::.ctor

} // end of class A11
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim a11 = compilation.GetTypeByMetadataName("A11")

            Assert.Equal(MethodKind.Ordinary, DirectCast(a11.GetMembers("op_LogicalNot").Single(), MethodSymbol).MethodKind)
            Assert.Equal(MethodKind.UserDefinedOperator, DirectCast(a11.GetMembers("op_OnesComplement").Single(), MethodSymbol).MethodKind)
        End Sub

        <Fact>
        Public Sub Not5()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A11
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A11  OP_ONESCOMPLEMENT(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "OP_ONESCOMPLEMENT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::OP_ONESCOMPLEMENT

  .method public hidebysig specialname static 
          class A11  op_LogicalNot(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalNot"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_LogicalNot

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A11::.ctor

} // end of class A11
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim a11 = compilation.GetTypeByMetadataName("A11")

            Assert.Equal(MethodKind.UserDefinedOperator, DirectCast(a11.GetMembers("op_OnesComplement").Single(), MethodSymbol).MethodKind)
            Assert.Equal(MethodKind.Ordinary, DirectCast(a11.GetMembers("op_LogicalNot").Single(), MethodSymbol).MethodKind)
        End Sub

        <Fact>
        Public Sub Not6()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A11
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A11  op_OnesComplement(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_OnesComplement"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_OnesComplement

  .method public hidebysig static 
          class A11  OP_ONESCOMPLEMENT(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "OP_ONESCOMPLEMENT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::OP_ONESCOMPLEMENT

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A11::.ctor

} // end of class A11
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim a11 = compilation.GetTypeByMetadataName("A11")

            Assert.Equal(MethodKind.UserDefinedOperator, DirectCast(a11.GetMembers().Where(Function(m) m.Name.Equals("op_OnesComplement")).Single(), MethodSymbol).MethodKind)
            Assert.Equal(MethodKind.Ordinary, DirectCast(a11.GetMembers().Where(Function(m) m.Name.Equals("OP_ONESCOMPLEMENT")).Single(), MethodSymbol).MethodKind)
        End Sub

        <Fact>
        Public Sub Not7()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A11
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A11  op_OnesComplement(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_OnesComplement"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_OnesComplement


  .field public class A11 OP_ONESCOMPLEMENT

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A11::.ctor

} // end of class A11
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim a11 = compilation.GetTypeByMetadataName("A11")

            Assert.Equal(MethodKind.UserDefinedOperator, DirectCast(a11.GetMembers().Where(Function(m) m.Name.Equals("op_OnesComplement")).Single(), MethodSymbol).MethodKind)
        End Sub

        <Fact>
        Public Sub Addition1()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A14
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A14  op_Addition(class A14 x,
                                class A15 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A14 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A14::op_Addition

  .method public hidebysig specialname static 
          class A14  op_Addition(class A15 x,
                                class A14 y) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] class A14 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A14::op_Addition

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A14::.ctor

} // end of class A14

.class public auto ansi beforefieldinit A15
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A15::.ctor

} // end of class A15
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim a14 = compilation.GetTypeByMetadataName("A14")

            For Each m In a14.GetMembers("op_Addition")
                Assert.Equal(MethodKind.UserDefinedOperator, DirectCast(m, MethodSymbol).MethodKind)
            Next
        End Sub

        <WorkItem(546315, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546315")>
        <Fact>
        Public Sub Bug15563()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="C">
    <file name="a.vb">
        
Module Module1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim nullable = compilation.GetSpecialType(SpecialType.System_Nullable_T)
            Dim op_Implicit = DirectCast(nullable.GetMembers("op_Implicit").Single, MethodSymbol)
            Dim op_Explicit = DirectCast(nullable.GetMembers("op_Explicit").Single, MethodSymbol)

            Assert.Equal(MethodKind.Conversion, op_Implicit.MethodKind)
            Assert.Equal(MethodKind.Conversion, op_Explicit.MethodKind)
        End Sub

    End Class

End Namespace
