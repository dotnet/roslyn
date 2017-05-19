' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.CSharp
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
            Dim mscorlibRef = TestReferences.NetFx.v4_0_21006.mscorlib

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

        <Fact>
        Public Sub NonExtensibleReadOnlySignaturesAreRead()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    public void M(ref readonly int x)
    {
        System.Console.WriteLine(x);
    }
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim x = 5
        Dim obj = New TestRef()
        obj.M(x)
    End Sub
End Class
    </file>
                </compilation>

            CompileAndVerify(source, additionalRefs:={reference}, expectedOutput:="5")
        End Sub

        <Fact>
        Public Sub ExtensibleReadOnlySignaturesAreNotSupported_Methods_ReturnTypes()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    private int value = 0;
    public virtual ref readonly int M()
    {
        return ref value;
    }
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim obj = New TestRef()
        Dim value = obj.M()
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC30657: 'M' has a return type that is not supported or parameter types that are not supported.
        Dim value = obj.M()
                        ~
                                                </expected>)
        End Sub

        <Fact>
        Public Sub ExtensibleReadOnlySignaturesAreNotSupported_Methods_Parameters()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    public virtual void M(ref readonly int x)
    {
        System.Console.WriteLine(x);
    }
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim x = 5
        Dim obj = New TestRef()
        obj.M(x)
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC30657: 'M' has a return type that is not supported or parameter types that are not supported.
        obj.M(x)
            ~
</expected>)
        End Sub

        <Fact>
        Public Sub ExtensibleReadOnlySignaturesAreNotSupported_Properties()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    private int value = 0;
    public virtual ref readonly int P => ref value;
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim obj = New TestRef()
        Dim value = obj.P
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC30643: Property 'P' is of an unsupported type.
        Dim value = obj.P
                        ~
                                                </expected>)
        End Sub

        <Fact>
        Public Sub ExtensibleReadOnlySignaturesAreNotSupported_Indexers_ReturnTypes()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    private int value = 0;
    public virtual ref readonly int this[int p] => ref value;
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim obj = New TestRef()
        Dim value = obj(0)
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC30643: Property 'TestRef.Item(p As )' is of an unsupported type.
        Dim value = obj(0)
                    ~~~
                                                </expected>)
        End Sub

        <Fact>
        Public Sub ExtensibleReadOnlySignaturesAreNotSupported_Indexers_Parameters()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    public virtual int this[ref readonly int p] => 0;
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim p = 0
        Dim obj = New TestRef()
        Dim value = obj(p)
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC30643: Property 'TestRef.Item(p As )' is of an unsupported type.
        Dim value = obj(p)
                    ~~~
                                                </expected>)
        End Sub
    End Class
End Namespace
