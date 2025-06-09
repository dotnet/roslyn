' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CustomModifiersTests
        Inherits BasicTestBase

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ModifiedTypeArgument_01()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit Test1
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test1::.ctor

  .method public hidebysig static void  Test(valuetype [mscorlib]System.Nullable`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)> x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      "Test"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Test1::Test
  
} // end of class Test1
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Test1.Test(Nothing)
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim test = compilation.GetTypeByMetadataName("Test1").GetMember(Of MethodSymbol)("Test")
            Dim type = DirectCast(test.Parameters.First().Type, INamedTypeSymbol)
            Assert.Equal("System.Nullable(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong))", type.ToTestDisplayString())
            Assert.Equal("System.Runtime.CompilerServices.IsLong", type.GetTypeArgumentCustomModifiers(0).Single().Modifier.ToTestDisplayString())
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() type.GetTypeArgumentCustomModifiers(1))
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() type.GetTypeArgumentCustomModifiers(-1))

            Dim nullable = type.OriginalDefinition
            Assert.Equal("System.Nullable(Of T)", nullable.ToTestDisplayString())
            Assert.True(nullable.GetTypeArgumentCustomModifiers(0).IsEmpty)
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() nullable.GetTypeArgumentCustomModifiers(1))
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() nullable.GetTypeArgumentCustomModifiers(-1))

            Dim i = DirectCast(type.TypeArguments.First(), INamedTypeSymbol)
            Assert.Equal("System.Int32", i.ToTestDisplayString())
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() i.GetTypeArgumentCustomModifiers(0))

            nullable = nullable.Construct(i)
            Assert.Equal("System.Nullable(Of System.Int32)", nullable.ToTestDisplayString())
            Assert.True(nullable.GetTypeArgumentCustomModifiers(0).IsEmpty)
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() nullable.GetTypeArgumentCustomModifiers(1))
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() nullable.GetTypeArgumentCustomModifiers(-1))

            CompileAndVerify(compilation, expectedOutput:="Test")
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ModifiedTypeArgument_02()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit Test1
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test1::.ctor

  .method public hidebysig static void Test(class [mscorlib] System.Collections.Generic.Dictionary`2<int32, int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsConst)> x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      "Test"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Test1::Test
  
} // end of class Test1
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Test1.Test(Nothing)
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim test = compilation.GetTypeByMetadataName("Test1").GetMember(Of MethodSymbol)("Test")
            Dim type = DirectCast(test.Parameters.First().Type, INamedTypeSymbol)
            Assert.Equal("System.Collections.Generic.Dictionary(Of System.Int32, System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong))",
                         type.ToTestDisplayString())
            Assert.True(type.GetTypeArgumentCustomModifiers(0).IsEmpty)
            Dim modifiers = type.GetTypeArgumentCustomModifiers(1)
            Assert.Equal(2, modifiers.Length)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", modifiers.First().Modifier.ToTestDisplayString())
            Assert.Equal("System.Runtime.CompilerServices.IsLong", modifiers.Last().Modifier.ToTestDisplayString())
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() type.GetTypeArgumentCustomModifiers(2))
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() type.GetTypeArgumentCustomModifiers(-1))

            Dim dictionary = type.OriginalDefinition
            Assert.Equal("System.Collections.Generic.Dictionary(Of TKey, TValue)", dictionary.ToTestDisplayString())
            Assert.True(dictionary.GetTypeArgumentCustomModifiers(0).IsEmpty)
            Assert.True(dictionary.GetTypeArgumentCustomModifiers(1).IsEmpty)
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() dictionary.GetTypeArgumentCustomModifiers(2))
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() dictionary.GetTypeArgumentCustomModifiers(-1))

            Dim i = type.TypeArguments.First()
            dictionary = dictionary.Construct(i, i)
            Assert.Equal("System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)", dictionary.ToTestDisplayString())
            Assert.True(dictionary.GetTypeArgumentCustomModifiers(0).IsEmpty)
            Assert.True(dictionary.GetTypeArgumentCustomModifiers(1).IsEmpty)
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() dictionary.GetTypeArgumentCustomModifiers(2))
            Assert.Throws(Of System.IndexOutOfRangeException)(Sub() dictionary.GetTypeArgumentCustomModifiers(-1))

            CompileAndVerify(compilation, expectedOutput:="Test")
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ConcatModifiers_01()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig instance void  Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t1) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      IL_0000:  ldstr      "Test"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Call New CL2().Test(Nothing)
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Test")
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ConcatModifiers_02()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual 
            instance void  Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL2 = New CL3()

        x.Test(Nothing)
    End Sub
End Class

Class CL3
    Inherits CL2

    Overrides Sub Test(x as Integer)
        System.Console.WriteLine("Overridden")
    End Sub
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub CL3.Test(x As System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong))", test.ToTestDisplayString())

            Dim withModifiers = cl3.BaseType.BaseType
            Dim withoutModifiers = withModifiers.OriginalDefinition.Construct(withModifiers.TypeArguments)
            Assert.True(withModifiers.HasTypeArgumentsCustomModifiers)
            Assert.False(withoutModifiers.HasTypeArgumentsCustomModifiers)
            Assert.True(withoutModifiers.IsSameTypeIgnoringAll(withModifiers))
            Assert.NotEqual(withoutModifiers, withModifiers)

            CompileAndVerify(compilation, expectedOutput:="Overridden")
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ConcatModifiersAndByRef_01()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual 
            instance void  Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)& t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL2 = New CL3()

        x.Test(Nothing)
    End Sub
End Class

Class CL3
    Inherits CL2

    Overrides Sub Test(ByRef x as Integer)
        System.Console.WriteLine("Overridden")
    End Sub
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub CL3.Test(ByRef x As System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong))", test.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Overridden")
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ConcatModifiersAndByRef_02()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual 
            instance void  Test(!T1& modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL2 = New CL3()

        x.Test(Nothing)
    End Sub
End Class

Class CL3
    Inherits CL2

    Overrides Sub Test(ByRef x as Integer)
        System.Console.WriteLine("Overridden")
    End Sub
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub CL3.Test(ByRef modopt(System.Runtime.CompilerServices.IsConst) x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong))", test.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Overridden")
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ConcatModifiersAndByRef_03()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual 
            instance void  Test(!T1& t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL2 = New CL3()

        x.Test(Nothing)
    End Sub
End Class

Class CL3
    Inherits CL2

    Overrides Sub Test(ByRef x as Integer)
        System.Console.WriteLine("Overridden")
    End Sub
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub CL3.Test(ByRef x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong))", test.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Overridden")
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ConcatModifiersAndByRef_04()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual 
            instance void  Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL2 = New CL3()

        x.Test(Nothing)
    End Sub
End Class

Class CL3
    Inherits CL2

    Overrides Sub Test(ByRef x as Integer)
        System.Console.WriteLine("Overridden")
    End Sub
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub CL3.Test(ByRef modopt(System.Runtime.CompilerServices.IsConst) x As System.Int32 modopt(System.Runtime.CompilerServices.IsVolatile) modopt(System.Runtime.CompilerServices.IsLong))", test.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Overridden")
        End Sub

        <Fact(), WorkItem(8948, "https://github.com/dotnet/roslyn/issues/8948")>
        Public Sub ConcatModifiersAndByRefReturn_01()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .field private !T1 f1

    .method public hidebysig newslot virtual
            instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)& Test() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      ldstr      "Test"
      call       void [mscorlib]System.Console::WriteLine(string)
      ldarg.0
      ldflda     !0 class CL1`1<!T1>::f1
      ret
    } // end of method CL1`1::Test

    .method public hidebysig newslot virtual
            instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)& get_P() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      ldstr      "get_P"
      call       void [mscorlib]System.Console::WriteLine(string)
      ldarg.0
      ldflda     !0 class CL1`1<!T1>::f1
      ret
    } 

    .property instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)& P()
    {
      .get instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)& CL1`1::get_P()
    } 

} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL2 = New CL2()

        x.Test() = 2
        x.P = 3
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl2Base = compilation.GetTypeByMetadataName("CL2").BaseType
            Assert.Equal("ByRef Function CL1(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)).Test() As System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong)", cl2Base.GetMember(Of MethodSymbol)("Test").ToTestDisplayString())
            Assert.Equal("ReadOnly ByRef Property CL1(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)).P As System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong)", cl2Base.GetMember(Of PropertySymbol)("P").ToTestDisplayString())

            Dim cl1 = compilation.GetTypeByMetadataName("CL1`1")
            Assert.Equal("ByRef Function CL1(Of T1).Test() As T1 modopt(System.Runtime.CompilerServices.IsConst)", cl1.GetMember(Of MethodSymbol)("Test").ToTestDisplayString())
            Assert.Equal("ReadOnly ByRef Property CL1(Of T1).P As T1 modopt(System.Runtime.CompilerServices.IsConst)", cl1.GetMember(Of PropertySymbol)("P").ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:=
"Test
get_P")
        End Sub

        <Fact(), WorkItem(8948, "https://github.com/dotnet/roslyn/issues/8948")>
        Public Sub ConcatModifiersAndByRefReturn_02()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .field private !T1 f1

    .method public hidebysig newslot virtual
            instance !T1& modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Test() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      ldstr      "Test"
      call       void [mscorlib]System.Console::WriteLine(string)
      ldarg.0
      ldflda     !0 class CL1`1<!T1>::f1
      ret
    } // end of method CL1`1::Test

    .method public hidebysig newslot virtual
            instance !T1& modopt([mscorlib]System.Runtime.CompilerServices.IsConst) get_P() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      ldstr      "get_P"
      call       void [mscorlib]System.Console::WriteLine(string)
      ldarg.0
      ldflda     !0 class CL1`1<!T1>::f1
      ret
    } 

    .property instance !T1& modopt([mscorlib]System.Runtime.CompilerServices.IsConst) P()
    {
      .get instance !T1& modopt([mscorlib]System.Runtime.CompilerServices.IsConst) CL1`1::get_P()
    } 

} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL2 = New CL2()

        x.Test() = 2
        x.P = 3
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl2Base = compilation.GetTypeByMetadataName("CL2").BaseType
            Assert.Equal("ByRef modopt(System.Runtime.CompilerServices.IsConst) Function CL1(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)).Test() As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)", cl2Base.GetMember(Of MethodSymbol)("Test").ToTestDisplayString())
            Assert.Equal("ReadOnly ByRef modopt(System.Runtime.CompilerServices.IsConst) Property CL1(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)).P As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)", cl2Base.GetMember(Of PropertySymbol)("P").ToTestDisplayString())

            Dim cl1 = compilation.GetTypeByMetadataName("CL1`1")
            Assert.Equal("ByRef modopt(System.Runtime.CompilerServices.IsConst) Function CL1(Of T1).Test() As T1", cl1.GetMember(Of MethodSymbol)("Test").ToTestDisplayString())
            Assert.Equal("ReadOnly ByRef modopt(System.Runtime.CompilerServices.IsConst) Property CL1(Of T1).P As T1", cl1.GetMember(Of PropertySymbol)("P").ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:=
"Test
get_P")
        End Sub

        <Fact(), WorkItem(8948, "https://github.com/dotnet/roslyn/issues/8948")>
        Public Sub ConcatModifiersAndByRefReturn_03()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .field private !T1 f1

    .method public hidebysig newslot virtual
            instance !T1& Test() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      ldstr      "Test"
      call       void [mscorlib]System.Console::WriteLine(string)
      ldarg.0
      ldflda     !0 class CL1`1<!T1>::f1
      ret
    } // end of method CL1`1::Test

    .method public hidebysig newslot virtual
            instance !T1& get_P() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      ldstr      "get_P"
      call       void [mscorlib]System.Console::WriteLine(string)
      ldarg.0
      ldflda     !0 class CL1`1<!T1>::f1
      ret
    } 

    .property instance !T1& P()
    {
      .get instance !T1& CL1`1::get_P()
    } 

} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL2 = New CL2()

        x.Test() = 2
        x.P = 3
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl2Base = compilation.GetTypeByMetadataName("CL2").BaseType
            Assert.Equal("ByRef Function CL1(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)).Test() As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)", cl2Base.GetMember(Of MethodSymbol)("Test").ToTestDisplayString())
            Assert.Equal("ReadOnly ByRef Property CL1(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)).P As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)", cl2Base.GetMember(Of PropertySymbol)("P").ToTestDisplayString())

            Dim cl1 = compilation.GetTypeByMetadataName("CL1`1")
            Assert.Equal("ByRef Function CL1(Of T1).Test() As T1", cl1.GetMember(Of MethodSymbol)("Test").ToTestDisplayString())
            Assert.Equal("ReadOnly ByRef Property CL1(Of T1).P As T1", cl1.GetMember(Of PropertySymbol)("P").ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:=
"Test
get_P")
        End Sub

        <Fact(), WorkItem(8948, "https://github.com/dotnet/roslyn/issues/8948")>
        Public Sub ConcatModifiersAndByRefReturn_04()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .field private !T1 f1

    .method public hidebysig newslot virtual
            instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Test() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      ldstr      "Test"
      call       void [mscorlib]System.Console::WriteLine(string)
      ldarg.0
      ldflda     !0 class CL1`1<!T1>::f1
      ret
    } // end of method CL1`1::Test

    .method public hidebysig newslot virtual
            instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) get_P() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      ldstr      "get_P"
      call       void [mscorlib]System.Console::WriteLine(string)
      ldarg.0
      ldflda     !0 class CL1`1<!T1>::f1
      ret
    } 

    .property instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) P()
    {
      .get instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) CL1`1::get_P()
    } 

} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL2 = New CL2()

        x.Test() = 2
        x.P = 3
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl2Base = compilation.GetTypeByMetadataName("CL2").BaseType
            Assert.Equal("ByRef modopt(System.Runtime.CompilerServices.IsConst) Function CL1(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)).Test() As System.Int32 modopt(System.Runtime.CompilerServices.IsVolatile) modopt(System.Runtime.CompilerServices.IsLong)", cl2Base.GetMember(Of MethodSymbol)("Test").ToTestDisplayString())
            Assert.Equal("ReadOnly ByRef modopt(System.Runtime.CompilerServices.IsConst) Property CL1(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)).P As System.Int32 modopt(System.Runtime.CompilerServices.IsVolatile) modopt(System.Runtime.CompilerServices.IsLong)", cl2Base.GetMember(Of PropertySymbol)("P").ToTestDisplayString())

            Dim cl1 = compilation.GetTypeByMetadataName("CL1`1")
            Assert.Equal("ByRef modopt(System.Runtime.CompilerServices.IsConst) Function CL1(Of T1).Test() As T1 modopt(System.Runtime.CompilerServices.IsVolatile)", cl1.GetMember(Of MethodSymbol)("Test").ToTestDisplayString())
            Assert.Equal("ReadOnly ByRef modopt(System.Runtime.CompilerServices.IsConst) Property CL1(Of T1).P As T1 modopt(System.Runtime.CompilerServices.IsVolatile)", cl1.GetMember(Of PropertySymbol)("P").ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:=
"Test
get_P")
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ConcatModifiers_03()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .property instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
            Test()
    {
      .get instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) CL1`1::get_Test()
      .set instance void CL1`1::set_Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst))
    } // end of property CL1`1::Test

    .method public hidebysig newslot specialname virtual 
            instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 
            get_Test() cil managed
    {
      // Code size       2 (0x2)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  throw
    } // end of method CL1`1::get_Test

    .method public hidebysig newslot specialname virtual 
            instance void  set_Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) x) cil managed
    {
      // Code size       3 (0x3)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  throw
      IL_0002:  ret
    } // end of method CL1`1::set_Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL2 = New CL3()

        x.Test = Nothing
        Dim y = x.Test
    End Sub
End Class

Class CL3
    Inherits CL2

    Overrides Property Test As Integer
        Get
            System.Console.WriteLine("Get Overridden")
        End Get
        Set
            System.Console.WriteLine("Set Overridden")
        End Set
    End Property
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of PropertySymbol)("Test")
            Assert.Equal("Property CL3.Test As System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong)", test.ToTestDisplayString())
            Assert.Equal("Function CL3.get_Test() As System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong)", test.GetMethod.ToTestDisplayString())
            Assert.Equal("Sub CL3.set_Test(Value As System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong))", test.SetMethod.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Set Overridden
Get Overridden")
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ConcatModifiers_04()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual 
            instance void  Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[] t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL2 = New CL3()

        x.Test(Nothing)
    End Sub
End Class

Class CL3
    Inherits CL2

    Overrides Sub Test(x as Integer())
        System.Console.WriteLine("Overridden")
    End Sub
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub CL3.Test(x As System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) ())", test.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Overridden")
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ConcatModifiers_05()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .field public static !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Test

    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      // Code size       18 (0x12)
      .maxstack  1
      IL_0000:  ldc.i4.s   123
      IL_0002:  box        [mscorlib]System.Int32
      IL_0007:  unbox.any  !T1
      IL_000c:  stsfld     !0 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) class CL1`1<!T1>::Test
      IL_0011:  ret
    } // end of method CL1`1::.cctor

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        System.Console.WriteLine(CL2.Test)
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl2 = compilation.GetTypeByMetadataName("CL2")
            Assert.Equal("CL1(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)).Test As System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong)", cl2.BaseType.GetMember("Test").ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="123")
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub ConstructedTypesEquality_02()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual 
            instance void  Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

.class public auto ansi beforefieldinit CL3
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

.class public auto ansi beforefieldinit CL4
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim base1 = compilation.GetTypeByMetadataName("CL2").BaseType
            Dim base2 = compilation.GetTypeByMetadataName("CL3").BaseType
            Dim base3 = compilation.GetTypeByMetadataName("CL4").BaseType

            Assert.True(base1.HasTypeArgumentsCustomModifiers)
            Assert.True(base2.HasTypeArgumentsCustomModifiers)
            Assert.True(base1.IsSameTypeIgnoringAll(base2))
            Assert.NotEqual(base1, base2)

            Assert.True(base3.HasTypeArgumentsCustomModifiers)
            Assert.True(base1.IsSameTypeIgnoringAll(base3))
            Assert.Equal(base1, base3)
            Assert.NotSame(base1, base3)
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub RetargetingModifiedTypeArgument_01()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit Test1
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test1::.ctor

  .method public hidebysig newslot virtual 
            instance void  Test(valuetype [mscorlib]System.Nullable`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)> x) cil managed
  {
    .maxstack  1
    IL_000a:  ret
  } // end of method Test1::Test
  
} // end of class Test1
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Inherits Test1

    Overrides Sub Test(x as System.Nullable(Of Integer))
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation1 = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            CompileAndVerify(compilation1)

            Dim test = compilation1.GetTypeByMetadataName("Module1").GetMember(Of MethodSymbol)("Test")

            Assert.Equal("Sub Module1.Test(x As System.Nullable(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)))", test.ToTestDisplayString())

            Assert.Same(compilation1.SourceModule.CorLibrary(), test.Parameters.First.Type.OriginalDefinition.ContainingAssembly)
            Assert.Same(compilation1.SourceModule.CorLibrary(), DirectCast(test.Parameters.First.Type, NamedTypeSymbol).GetTypeArgumentCustomModifiers(0).First.Modifier.ContainingAssembly)

            Dim compilation2 = CreateCompilationWithMscorlib461(source:=Nothing, references:={New VisualBasicCompilationReference(compilation1)})

            test = compilation2.GetTypeByMetadataName("Module1").GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub Module1.Test(x As System.Nullable(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)))", test.ToTestDisplayString())

            Assert.IsType(Of VisualBasic.Symbols.Retargeting.RetargetingAssemblySymbol)(test.ContainingAssembly)
            Assert.Same(compilation2.SourceModule.CorLibrary(), test.Parameters.First.Type.OriginalDefinition.ContainingAssembly)
            Assert.Same(compilation2.SourceModule.CorLibrary(), DirectCast(test.Parameters.First.Type, NamedTypeSymbol).GetTypeArgumentCustomModifiers(0).First.Modifier.ContainingAssembly)

            Assert.NotSame(compilation1.SourceModule.CorLibrary(), compilation2.SourceModule.CorLibrary())
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub TypeUnification_01()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T>
{
} // end of class ITest2`1
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface ITest3(Of T, U)
    Inherits ITest1(Of T), ITest2(Of U)
End Interface

Interface ITest4(Of T, U)
    Inherits ITest2(Of T), ITest1(Of U)
End Interface
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(<expected>
BC32122: Cannot inherit interface 'ITest2(Of U)' because the interface 'ITest0(Of U)' from which it inherits could be identical to interface 'ITest0(Of T)' from which the interface 'ITest1(Of T)' inherits for some type arguments.
    Inherits ITest1(Of T), ITest2(Of U)
                           ~~~~~~~~~~~~
BC32122: Cannot inherit interface 'ITest1(Of U)' because the interface 'ITest0(Of U)' from which it inherits could be identical to interface 'ITest0(Of T)' from which the interface 'ITest2(Of T)' inherits for some type arguments.
    Inherits ITest2(Of T), ITest1(Of U)
                           ~~~~~~~~~~~~
                                               </expected>)
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub TypeUnification_02()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T>
{
} // end of class ITest2`1
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface ITest3(Of T, U)
    Inherits ITest1(Of T), ITest2(Of U)
End Interface

Interface ITest4(Of T, U)
    Inherits ITest2(Of T), ITest1(Of U)
End Interface
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(<expected>
BC32122: Cannot inherit interface 'ITest2(Of U)' because the interface 'ITest0(Of U)' from which it inherits could be identical to interface 'ITest0(Of T)' from which the interface 'ITest1(Of T)' inherits for some type arguments.
    Inherits ITest1(Of T), ITest2(Of U)
                           ~~~~~~~~~~~~
BC32122: Cannot inherit interface 'ITest1(Of U)' because the interface 'ITest0(Of U)' from which it inherits could be identical to interface 'ITest0(Of T)' from which the interface 'ITest2(Of T)' inherits for some type arguments.
    Inherits ITest2(Of T), ITest1(Of U)
                           ~~~~~~~~~~~~
                                               </expected>)
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub TypeUnification_03()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
} // end of class ITest2`1
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface ITest3(Of T, U)
    Inherits ITest1(Of T), ITest2(Of U)
End Interface

Interface ITest4(Of T, U)
    Inherits ITest2(Of T), ITest1(Of U)
End Interface
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(<expected>
                                               </expected>)
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub TypeUnification_04()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest2`1
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface ITest3(Of T, U)
    Inherits ITest1(Of T), ITest2(Of U)
End Interface

Interface ITest4(Of T, U)
    Inherits ITest2(Of T), ITest1(Of U)
End Interface
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(<expected>
BC32122: Cannot inherit interface 'ITest2(Of U)' because the interface 'ITest0(Of U)' from which it inherits could be identical to interface 'ITest0(Of T)' from which the interface 'ITest1(Of T)' inherits for some type arguments.
    Inherits ITest1(Of T), ITest2(Of U)
                           ~~~~~~~~~~~~
BC32122: Cannot inherit interface 'ITest1(Of U)' because the interface 'ITest0(Of U)' from which it inherits could be identical to interface 'ITest0(Of T)' from which the interface 'ITest2(Of T)' inherits for some type arguments.
    Inherits ITest2(Of T), ITest1(Of U)
                           ~~~~~~~~~~~~
                                               </expected>)
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub TypeUnification_05()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest2`1
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface ITest3(Of T, U)
    Inherits ITest1(Of T), ITest2(Of U)
End Interface

Interface ITest4(Of T, U)
    Inherits ITest2(Of T), ITest1(Of U)
End Interface
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            Assert.Equal("ITest0(Of T modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong))", compilation.GetTypeByMetadataName("ITest1`1").Interfaces.First.ToTestDisplayString())
            Assert.Equal("ITest0(Of T modopt(System.Runtime.CompilerServices.IsConst))", compilation.GetTypeByMetadataName("ITest2`1").Interfaces.First.ToTestDisplayString())

            compilation.AssertTheseDiagnostics(<expected>
BC32122: Cannot inherit interface 'ITest2(Of U)' because the interface 'ITest0(Of U)' from which it inherits could be identical to interface 'ITest0(Of T)' from which the interface 'ITest1(Of T)' inherits for some type arguments.
    Inherits ITest1(Of T), ITest2(Of U)
                           ~~~~~~~~~~~~
BC32122: Cannot inherit interface 'ITest1(Of U)' because the interface 'ITest0(Of U)' from which it inherits could be identical to interface 'ITest0(Of T)' from which the interface 'ITest2(Of T)' inherits for some type arguments.
    Inherits ITest2(Of T), ITest1(Of U)
                           ~~~~~~~~~~~~
                                               </expected>)
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub TypeUnification_06()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest2`1
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface ITest3(Of T, U)
    Inherits ITest1(Of T), ITest2(Of U)
End Interface

Interface ITest4(Of T, U)
    Inherits ITest2(Of T), ITest1(Of U)
End Interface
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            Assert.Equal("ITest0(Of T modopt(System.Runtime.CompilerServices.IsLong) modopt(System.Runtime.CompilerServices.IsConst))", compilation.GetTypeByMetadataName("ITest1`1").Interfaces.First.ToTestDisplayString())
            Assert.Equal("ITest0(Of T modopt(System.Runtime.CompilerServices.IsConst))", compilation.GetTypeByMetadataName("ITest2`1").Interfaces.First.ToTestDisplayString())

            compilation.AssertTheseDiagnostics(<expected>
                                               </expected>)
        End Sub

        <Fact(), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")>
        Public Sub Delegates_01()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual 
            instance !T1  Test(class [mscorlib]System.Func`2<!T1,!T1> d,
                               !T1 val) cil managed
    {
      // Code size       10 (0xa)
      .maxstack  2
      .locals init ([0] !T1 V_0)
      IL_0000:  ldarg.1
      IL_0001:  ldarg.2
      IL_0002:  callvirt   instance !1 class [mscorlib]System.Func`2<!T1,!T1>::Invoke(!0)
      IL_0007:  stloc.0
      IL_0008:  ldloc.0
      IL_0009:  ret
    } // end of method CL1`1::Test

} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

.class public auto ansi sealed beforefieldinit MyDelegate
       extends [mscorlib]System.MulticastDelegate
{
    .method public specialname rtspecialname 
            instance void  .ctor(object A_0,
                                 native int A_1) runtime managed forwardref
    {
    } // end of method MyDelegate::.ctor

    .method public newslot virtual final instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x,
                        class [mscorlib]System.AsyncCallback callback,
                        object obj) runtime managed forwardref
    {
    } // end of method MyDelegate::BeginInvoke

    .method public newslot virtual final instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 
            EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed forwardref
    {
    } // end of method MyDelegate::EndInvoke

    .method public newslot virtual final instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 
            Invoke(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) runtime managed forwardref
    {
    } // end of method MyDelegate::Invoke
} // end of class MyDelegate
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x as CL2 = New CL2()
        x.Test(AddressOf Test, 1)
        x.Test(Function(v As Integer) As Integer
                   System.Console.WriteLine("Test {0}", v)
                   Return v
               End Function, 2)

        x = new CL3()
        x.Test(AddressOf Test, 3)
        x.Test(Function(v As Integer) As Integer
                   System.Console.WriteLine("Test {0}", v)
                   Return v
               End Function, 4)

        Test(AddressOf Test, 5)
        Test(Function(v As Integer) As Integer
                 System.Console.WriteLine("Test {0}", v)
                 Return v
             End Function, 6)
    End Sub

    Shared Function Test(v As Integer) As Integer
        System.Console.WriteLine("Test {0}", v)
        Return v
    End Function

    Shared Function Test(d As MyDelegate, v as Integer) As Integer
        System.Console.WriteLine("MyDelegate")
        Return d(v)
    End Function
End Class

Class CL3
    Inherits CL2

    Overrides Function Test(x as System.Func(Of Integer, Integer), y as Integer) As Integer
        System.Console.WriteLine("Overridden")
        return x(y)
    End Function
End Class 

]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Test 1
Test 2
Overridden
Test 3
Overridden
Test 4
MyDelegate
Test 5
MyDelegate
Test 6")
        End Sub

        <Fact, WorkItem(4623, "https://github.com/dotnet/roslyn/issues/4623")>
        Public Sub MultiDimensionalArray_01()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit Test1
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig newslot virtual 
            instance void  Test(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[0...,0...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      IL_0000:  ldstr      "Test"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method Test1::Test
} // end of class Test1
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As Test1 = new Test1()
        x.Test(Nothing)
        x = new Test11()
        x.Test(Nothing)
    End Sub
End Class

class Test11 
    Inherits Test1

    public overrides Sub Test(c As Integer(,))
        System.Console.WriteLine("Overridden")
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Test
Overridden")
        End Sub

        <Fact, WorkItem(5725, "https://github.com/dotnet/roslyn/issues/5725")>
        Public Sub ModifiersWithConstructedType_01()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<valuetype .ctor ([mscorlib]System.ValueType) T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006:
        ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test(!T1 modopt(valuetype [mscorlib]System.Nullable`1<!T1>) t1) cil managed 
    {
      // Code size       1 (0x1)
      .maxstack  1
      IL_0000:  ldstr      "Test"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x = new CL2()
        x.Test(1)
        x = new CL3()
        x.Test(1)
    End Sub
End Class

class CL3 
    Inherits CL2

    public overrides Sub Test(c As Integer)
        System.Console.WriteLine("Overridden")
    end Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Test
Overridden")
        End Sub

        <Fact, WorkItem(5725, "https://github.com/dotnet/roslyn/issues/5725")>
        Public Sub ModifiersWithConstructedType_02()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<valuetype .ctor ([mscorlib]System.ValueType) T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006:
        ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test(!T1 modopt(valuetype [mscorlib]System.Nullable`1) t1) cil managed 
    {
      // Code size       1 (0x1)
      .maxstack  1
      IL_0000:  ldstr      "Test"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x = new CL2()
        x.Test(1)
        x = new CL3()
        x.Test(1)
    End Sub
End Class

class CL3 
    Inherits CL2

    public overrides Sub Test(c As Integer)
        System.Console.WriteLine("Overridden")
    end Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Test
Overridden")
        End Sub

        <Fact, WorkItem(5725, "https://github.com/dotnet/roslyn/issues/5725")>
        Public Sub ModifiersWithConstructedType_03()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<valuetype .ctor ([mscorlib]System.ValueType) T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006:
        ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance int32 modopt(CL2) modopt(valuetype [mscorlib]System.Nullable`1<!T1>) modopt(valuetype [mscorlib]System.Nullable`1<!T1>) modopt(CL2) [] Test(!T1 t1) cil managed 
    {
      // Code size       1 (0x1)
      .maxstack  1
      IL_0000:  ldstr      "Test"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0006:  ldnull
      IL_000a:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x = new CL2()
        x.Test(1)
        x = new CL3()
        x.Test(1)
    End Sub
End Class

class CL3 
    Inherits CL2

    public overrides Function Test(c As Integer) As Integer()
        System.Console.WriteLine("Overridden")
        return Nothing
    end Function
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Test
Overridden")
        End Sub

        <Fact, WorkItem(14453, "https://github.com/dotnet/roslyn/issues/14453")>
        Public Sub ModifiersWithConstructedType_04()
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As CL1 = new CL2()
        x.Test(Of Integer)(1)
    End Sub
End Class

class CL2 
    Inherits CL1

    public overrides Function Test(Of U As Structure)(c As System.ValueType) As System.ValueType
        System.Console.WriteLine("Overridden")
        return c
    end Function
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(vbSource,
                                                                                     {TestReferences.SymbolsTests.CustomModifiers.GenericMethodWithModifiers.dll},
                                                                                     options:=TestOptions.ReleaseExe)

            Dim cl2 = compilation.GetTypeByMetadataName("CL2")
            Dim test = cl2.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Function CL2.Test(Of U)(c As System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(System.Nullable(Of U))) As System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(System.Nullable(Of U))", test.ToTestDisplayString())
            Assert.Equal("Function CL1.Test(Of T)(x As System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(System.Nullable(Of T))) As System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(System.Nullable(Of T))", test.OverriddenMethod.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Overridden")
        End Sub

        <Fact, WorkItem(14453, "https://github.com/dotnet/roslyn/issues/14453")>
        Public Sub ModifiersWithConstructedType_05()
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim x As I1 = new CL2()
        x.Test(Of Integer)(1)

        x = new CL3()
        x.Test(Of Integer)(2)
    End Sub
End Class

class CL2 
    Implements I1

    public Function Test(Of U As Structure)(c As System.ValueType) As System.ValueType Implements I1.Test
        System.Console.WriteLine("CL2.Test")
        return c
    end Function
End Class

class CL3 
    Implements I1

    private Function Test(Of U As Structure)(c As System.ValueType) As System.ValueType Implements I1.Test
        System.Console.WriteLine("CL3.Test")
        return c
    end Function
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(vbSource,
                                                                                     {TestReferences.SymbolsTests.CustomModifiers.GenericMethodWithModifiers.dll},
                                                                                     options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Function CL3.Test(Of U)(c As System.ValueType) As System.ValueType", test.ToTestDisplayString())
            Assert.Equal("Function I1.Test(Of T)(x As System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(System.Nullable(Of T))) As System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(System.Nullable(Of T))", test.ExplicitInterfaceImplementations(0).ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:=
"CL2.Test
CL3.Test")
        End Sub

        <Fact, WorkItem(5993, "https://github.com/dotnet/roslyn/issues/5993")>
        Public Sub ConcatModifiersAndByRef_05()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi beforefieldinit X.I
{
  .method public newslot abstract virtual 
          instance void  A(uint32& modopt([mscorlib]System.Runtime.CompilerServices.IsImplicitlyDereferenced) x) cil managed
  {
  } // end of method I::A

  .method public newslot abstract virtual 
          instance void  B(uint32& x) cil managed
  {
  } // end of method I::B

} // end of class X.I
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Imports X

Class Module1
    Shared Sub Main()
        Dim x As I = new CI()
        Dim y As UInteger = 0
        x.A(y)
        x.B(y)
    End Sub
End Class

class CI 
    Implements I 

    public Sub A(Byref x As UInteger) Implements I.A
        System.Console.WriteLine("Implemented A")
    End Sub

    public Sub B(Byref x As UInteger) Implements I.B
        System.Console.WriteLine("Implemented B")
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Implemented A
Implemented B")
        End Sub

        <Fact, WorkItem(6372, "https://github.com/dotnet/roslyn/issues/6372")>
        Public Sub ModifiedTypeParameterAsTypeArgument_01()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006:
        ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test1(class CL1`1<!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)> t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:
        ret
    } // end of method CL1`1::Test

    .method public hidebysig newslot virtual
            instance void  Test2(class CL1`1<!T1> t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:
        ret
    } // end of method CL1`1::Test
} // end of class CL1`1
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl1 = compilation.GetTypeByMetadataName("CL1`1")
            Dim test1 = cl1.GetMember(Of MethodSymbol)("Test1")
            Assert.Equal("Sub CL1(Of T1).Test1(t1 As CL1(Of T1 modopt(System.Runtime.CompilerServices.IsConst)))", test1.ToTestDisplayString())

            Dim test2 = cl1.GetMember(Of MethodSymbol)("Test2")
            Assert.Equal("Sub CL1(Of T1).Test2(t1 As CL1(Of T1))", test2.ToTestDisplayString())

            Dim t1 = test1.Parameters(0).Type
            Dim t2 = test2.Parameters(0).Type

            Assert.False(t1.Equals(t2))
            Assert.False(t2.Equals(t1))

            Assert.True(t1.IsSameTypeIgnoringAll(t2))
            Assert.True(t2.IsSameTypeIgnoringAll(t1))
        End Sub

        <Fact>
        Public Sub TupleWithCustomModifiersInInterfaceMethod()

            Dim il = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}
.assembly extern System.ValueTuple { .publickeytoken = (CC 7B 13 FF CD 2D DD 51 ) .ver 4:0:1:0 }
.assembly '<<GeneratedFileName>>' { }

.class interface public abstract auto ansi I
{
  .method public hidebysig newslot abstract virtual
          instance class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong), object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
          M(class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong), object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)> x) cil managed
  {
    .param [0]
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = {string[2]('a' 'b')}
    .param [1]
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = {string[2]('c' 'd')}
  } // end of method I::M

} // end of class I
]]>.Value

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I

    Public Function M(x As (c As Object, d As Object)) As (a As Object, b As Object) Implements I.M
        Return x
    End Function
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithCustomILSource(source1, il, appendDefaultHeader:=False, additionalReferences:={ValueTupleRef, SystemRuntimeFacadeRef})
            comp1.AssertTheseDiagnostics()

            Dim interfaceMethod1 = comp1.GlobalNamespace.GetMember(Of MethodSymbol)("I.M")

            Assert.Equal("Function I.M(x As (c As System.Object, d As System.Object)) As (a As System.Object, b As System.Object)",
                         interfaceMethod1.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         interfaceMethod1.ReturnType.TupleUnderlyingType.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         interfaceMethod1.Parameters(0).Type.TupleUnderlyingType.ToTestDisplayString())

            ' Note: no copying of custom modifiers when implementing interface in VB
            Dim classMethod1 = comp1.GlobalNamespace.GetMember(Of MethodSymbol)("C.M")

            Assert.Equal("Function C.M(x As (c As System.Object, d As System.Object)) As (a As System.Object, b As System.Object)",
                         classMethod1.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object, System.Object)", ' modopts not copied
                         classMethod1.ReturnType.TupleUnderlyingType.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object, System.Object)", ' modopts not copied
                         classMethod1.Parameters(0).Type.TupleUnderlyingType.ToTestDisplayString())

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I

    Public Function M(x As (Object, Object)) As (a As Object, b As Object) Implements I.M
        Return x
    End Function
End Class
]]></file>
</compilation>

            Dim comp2 = CreateCompilationWithCustomILSource(source2, il, appendDefaultHeader:=False, additionalReferences:={ValueTupleRef, SystemRuntimeFacadeRef})
            comp2.AssertTheseDiagnostics(
<errors>
BC30402: 'M' cannot implement function 'M' on interface 'I' because the tuple element names in 'Public Function M(x As (Object, Object)) As (a As Object, b As Object)' do not match those in 'Function M(x As (c As Object, d As Object)) As (a As Object, b As Object)'.
    Public Function M(x As (Object, Object)) As (a As Object, b As Object) Implements I.M
                                                                                      ~~~
</errors>)

            Dim classMethod2 = comp2.GlobalNamespace.GetMember(Of MethodSymbol)("C.M")

            Assert.Equal("Function C.M(x As (System.Object, System.Object)) As (a As System.Object, b As System.Object)",
                         classMethod2.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object, System.Object)", ' modopts not copied
                         classMethod2.ReturnType.TupleUnderlyingType.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object, System.Object)", ' modopts not copied
                         classMethod2.Parameters(0).Type.TupleUnderlyingType.ToTestDisplayString())

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I

    Public Function M(x As (c As Object, d As Object)) As (Object, Object) Implements I.M
        Return x
    End Function
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilationWithCustomILSource(source3, il, appendDefaultHeader:=False, additionalReferences:={ValueTupleRef, SystemRuntimeFacadeRef})
            comp3.AssertTheseDiagnostics(
<errors>
BC30402: 'M' cannot implement function 'M' on interface 'I' because the tuple element names in 'Public Function M(x As (c As Object, d As Object)) As (Object, Object)' do not match those in 'Function M(x As (c As Object, d As Object)) As (a As Object, b As Object)'.
    Public Function M(x As (c As Object, d As Object)) As (Object, Object) Implements I.M
                                                                                      ~~~
</errors>)

            Dim classMethod3 = comp3.GlobalNamespace.GetMember(Of MethodSymbol)("C.M")

            Assert.Equal("Function C.M(x As (c As System.Object, d As System.Object)) As (System.Object, System.Object)",
                         classMethod3.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object, System.Object)", ' modopts not copied
                         classMethod3.ReturnType.TupleUnderlyingType.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object, System.Object)", ' modopts not copied
                         classMethod3.Parameters(0).Type.TupleUnderlyingType.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleWithCustomModifiersInInterfaceProperty()

            Dim il = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}
.assembly extern System.ValueTuple { .publickeytoken = (CC 7B 13 FF CD 2D DD 51 ) .ver 4:0:1:0 }
.assembly '<<GeneratedFileName>>' { }

.class interface public abstract auto ansi I
{
  .method public hidebysig newslot specialname abstract virtual
          instance class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong), object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
          get_P() cil managed
  {
    .param [0]
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = {string[2]('a' 'b')}
  } // end of method I::get_P

  .method public hidebysig newslot specialname abstract virtual
          instance void  set_P(class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong), object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)> 'value') cil managed
  {
    .param [1]
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = {string[2]('a' 'b')}
  } // end of method I::set_P

  .property instance class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong), object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
          P()
  {
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = {string[2]('a' 'b')}
    .get instance class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong),object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)> I::get_P()
    .set instance void I::set_P(class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong),object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>)
  } // end of property I::P
} // end of class I
]]>.Value

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I

    Public Property P As (a As Object, b As Object) Implements I.P
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithCustomILSource(source1, il, appendDefaultHeader:=False, additionalReferences:={ValueTupleRef, SystemRuntimeFacadeRef})
            comp1.AssertTheseDiagnostics()

            Dim interfaceProperty1 = comp1.GlobalNamespace.GetMember(Of PropertySymbol)("I.P")

            Assert.Equal("Property I.P As (a As System.Object, b As System.Object)",
                         interfaceProperty1.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         interfaceProperty1.Type.TupleUnderlyingType.ToTestDisplayString())

            ' Note: no copying of custom modifiers when implementing interface in VB
            Dim classProperty1 = comp1.GlobalNamespace.GetMember(Of PropertySymbol)("C.P")

            Assert.Equal("Property C.P As (a As System.Object, b As System.Object)",
                         classProperty1.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object, System.Object)", ' modopts not copied
                         classProperty1.Type.TupleUnderlyingType.ToTestDisplayString())

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I

    Public Property P As (Object, Object) Implements I.P
End Class
]]></file>
</compilation>

            Dim comp2 = CreateCompilationWithCustomILSource(source2, il, appendDefaultHeader:=False, additionalReferences:={ValueTupleRef, SystemRuntimeFacadeRef})
            comp2.AssertTheseDiagnostics()

            Dim classProperty2 = comp2.GlobalNamespace.GetMember(Of PropertySymbol)("C.P")

            Assert.Equal("Property C.P As (System.Object, System.Object)",
                         classProperty2.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object, System.Object)", ' modopts not copied
                         classProperty2.Type.TupleUnderlyingType.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleWithCustomModifiersInOverride()
            ' // This IL is based on this code, but with modopts added
            ' //public class Base
            ' //{
            ' //    public virtual (object a, object b) P { get; set; }
            ' //    public virtual (object a, object b) M((object c, object d) x) { return x; }
            ' //}

            Dim il = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}
.assembly extern System.ValueTuple { .publickeytoken = (CC 7B 13 FF CD 2D DD 51 ) .ver 4:0:1:0 }
.assembly '<<GeneratedFileName>>' { }

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .field private class [System.ValueTuple]System.ValueTuple`2<object,object> '<P>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = {string[2]('a' 'b')}
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 )
  .method public hidebysig newslot specialname virtual
          instance class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong),object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
          get_P() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .param [0]
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])  = {string[2]('a' 'b')}
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [System.ValueTuple]System.ValueTuple`2<object,object> Base::'<P>k__BackingField'
    IL_0006:  ret
  } // end of method Base::get_P

  .method public hidebysig newslot specialname virtual
          instance void  set_P(class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong),object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)> 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .param [1]
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])  = {string[2]('a' 'b')}
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      class [System.ValueTuple]System.ValueTuple`2<object,object> Base::'<P>k__BackingField'
    IL_0007:  ret
  } // end of method Base::set_P

  .method public hidebysig newslot virtual
          instance class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong),object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
          M(class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong),object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)> x) cil managed
  {
    .param [0]
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])  = {string[2]('a' 'b')}
    .param [1]
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])  = {string[2]('c' 'd')}
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (class [System.ValueTuple]System.ValueTuple`2<object,object> V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method Base::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Base::.ctor

  .property instance class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong),object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
          P()
  {
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])  = {string[2]('a' 'b')}
    .get instance class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong),object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)> Base::get_P()
    .set instance void Base::set_P(class [System.ValueTuple]System.ValueTuple`2<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong),object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>)
  } // end of property Base::P
} // end of class Base
]]>.Value

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Inherits Base

    Public Overrides Function M(x As (c As Object, d As Object)) As (a As Object, b As Object)
        Return x
    End Function

    Public Overrides Property P As (a As Object, b As Object)
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithCustomILSource(source1, il, appendDefaultHeader:=False, additionalReferences:={ValueTupleRef, SystemRuntimeFacadeRef})
            comp1.AssertTheseDiagnostics()

            Dim baseMethod1 = comp1.GlobalNamespace.GetMember(Of MethodSymbol)("Base.M")

            Assert.Equal("Function Base.M(x As (c As System.Object, d As System.Object)) As (a As System.Object, b As System.Object)",
                         baseMethod1.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         baseMethod1.ReturnType.TupleUnderlyingType.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         baseMethod1.Parameters(0).Type.TupleUnderlyingType.ToTestDisplayString())

            Dim baseProperty1 = comp1.GlobalNamespace.GetMember(Of PropertySymbol)("Base.P")

            Assert.Equal("Property Base.P As (a As System.Object, b As System.Object)", baseProperty1.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         baseProperty1.Type.TupleUnderlyingType.ToTestDisplayString())

            Dim classMethod1 = comp1.GlobalNamespace.GetMember(Of MethodSymbol)("C.M")

            Assert.Equal("Function C.M(x As (c As System.Object, d As System.Object)) As (a As System.Object, b As System.Object)",
                         classMethod1.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         classMethod1.ReturnType.TupleUnderlyingType.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         classMethod1.Parameters(0).Type.TupleUnderlyingType.ToTestDisplayString())

            Dim classProperty1 = comp1.GlobalNamespace.GetMember(Of PropertySymbol)("C.P")

            Assert.Equal("Property C.P As (a As System.Object, b As System.Object)", classProperty1.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         classProperty1.Type.TupleUnderlyingType.ToTestDisplayString())

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Inherits Base

    Public Overrides Function M(x As (c As Object, d As Object)) As (Object, Object)
        Return x
    End Function

    Public Overrides Property P As (Object, Object)
End Class
]]></file>
</compilation>

            Dim comp2 = CreateCompilationWithCustomILSource(source2, il, appendDefaultHeader:=False, additionalReferences:={ValueTupleRef, SystemRuntimeFacadeRef})
            comp2.AssertTheseDiagnostics(
<errors>
BC40001: 'Public Overrides Function M(x As (c As Object, d As Object)) As (Object, Object)' cannot override 'Public Overridable Overloads Function M(x As (c As Object, d As Object)) As (a As Object, b As Object)' because they differ by their tuple element names.
    Public Overrides Function M(x As (c As Object, d As Object)) As (Object, Object)
                              ~
</errors>)

            Dim classProperty2 = comp2.GlobalNamespace.GetMember(Of PropertySymbol)("C.P")

            Assert.Equal("Property C.P As (System.Object, System.Object)", classProperty2.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object, System.Object)",
                         classProperty2.Type.TupleUnderlyingType.ToTestDisplayString())

            Dim classMethod2 = comp2.GlobalNamespace.GetMember(Of MethodSymbol)("C.M")

            Assert.Equal("Function C.M(x As (c As System.Object, d As System.Object)) As (System.Object, System.Object)",
                         classMethod2.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         classMethod2.ReturnType.TupleUnderlyingType.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         classMethod2.Parameters(0).Type.TupleUnderlyingType.ToTestDisplayString())

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Inherits Base

    Public Overrides Function M(x As (Object, Object)) As (a As Object, b As Object)
        Return x
    End Function

    Public Overrides Property P As (a As Object, b As Object)
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilationWithCustomILSource(source3, il, appendDefaultHeader:=False, additionalReferences:={ValueTupleRef, SystemRuntimeFacadeRef})
            comp3.AssertTheseDiagnostics(
<errors>
BC40001: 'Public Overrides Function M(x As (Object, Object)) As (a As Object, b As Object)' cannot override 'Public Overridable Overloads Function M(x As (c As Object, d As Object)) As (a As Object, b As Object)' because they differ by their tuple element names.
    Public Overrides Function M(x As (Object, Object)) As (a As Object, b As Object)
                              ~
</errors>)

            Dim classMethod3 = comp3.GlobalNamespace.GetMember(Of MethodSymbol)("C.M")

            Assert.Equal("Function C.M(x As (System.Object, System.Object)) As (a As System.Object, b As System.Object)",
                         classMethod3.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         classMethod3.ReturnType.TupleUnderlyingType.ToTestDisplayString())
            Assert.Equal("System.ValueTuple(Of System.Object modopt(System.Runtime.CompilerServices.IsLong), System.Object modopt(System.Runtime.CompilerServices.IsLong))",
                         classMethod3.Parameters(0).Type.TupleUnderlyingType.ToTestDisplayString())

        End Sub

    End Class
End Namespace
