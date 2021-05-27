' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

    Public Class EmitCustomModifiers
        Inherits BasicTestBase

        <Fact>
        Public Sub Test1()
            Dim mscorlibRef = TestMetadata.Net40.mscorlib

            Dim source As String = <text> 
Public Class A

    Public Shared Sub Main()
    
        Modifiers.F1(1)
        Modifiers.F2(1)
        Modifiers.F3(1)

        System.Console.WriteLine(Modifiers.F7())
        Modifiers.F8()
        Modifiers.F9()

        C4.M4()
    End Sub
End Class
</text>.Value

            Dim c1 = VisualBasicCompilation.Create("VB_EmitCustomModifiers",
                                        {Parse(source)},
                                        {mscorlibRef, TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll})

            CompileAndVerify(c1, expectedOutput:=<![CDATA[
F1
F2
F3
F7
F8
F9
M4
]]>)
        End Sub

        <Fact>
        <WorkItem(737971, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/737971")>
        Public Sub ByRefBeforeCustomModifiers()
            Dim il = <![CDATA[
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  // Increments argument
  .method public hidebysig static void Incr(uint32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) & a) cil managed
  {
    ldarg.0
    dup
    ldind.u4
    ldc.i4.1
    add
    stind.i4
    ret
  } // end of method Test::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class D
]]>

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim u As UInteger = 1
        C.Incr(u)
        System.Console.WriteLine(u)
    End Sub
End Class
    </file>
                </compilation>

            Dim comp = CreateCompilationWithCustomILSource(source, il.Value, TestOptions.ReleaseExe)

            Dim [type] = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Dim method = type.GetMember(Of MethodSymbol)("Incr")
            Dim parameter = method.Parameters.Single()

            Assert.True(parameter.IsByRef)
            Assert.False(parameter.CustomModifiers.IsEmpty)
            Assert.True(parameter.RefCustomModifiers.IsEmpty)

            CompileAndVerify(comp, expectedOutput:=<![CDATA[2]]>)
        End Sub
    End Class
End Namespace
