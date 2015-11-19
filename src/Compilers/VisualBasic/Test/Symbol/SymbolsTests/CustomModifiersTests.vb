' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        System.Console.WriteLine("Overriden")
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
            Assert.True(withoutModifiers.IsSameTypeIgnoringCustomModifiers(withModifiers))
            Assert.NotEqual(withoutModifiers, withModifiers)

            CompileAndVerify(compilation, expectedOutput:="Overriden")
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
        System.Console.WriteLine("Overriden")
    End Sub
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub CL3.Test(ByRef x As System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong))", test.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Overriden")
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
        System.Console.WriteLine("Overriden")
    End Sub
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub CL3.Test(ByRef modopt(System.Runtime.CompilerServices.IsConst) x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong))", test.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Overriden")
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
        System.Console.WriteLine("Overriden")
    End Sub
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub CL3.Test(ByRef x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong))", test.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Overriden")
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
        System.Console.WriteLine("Overriden")
    End Sub
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub CL3.Test(ByRef modopt(System.Runtime.CompilerServices.IsConst) x As System.Int32 modopt(System.Runtime.CompilerServices.IsVolatile) modopt(System.Runtime.CompilerServices.IsLong))", test.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Overriden")
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
            System.Console.WriteLine("Get Overriden")
        End Get
        Set
            System.Console.WriteLine("Set Overriden")
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

            CompileAndVerify(compilation, expectedOutput:="Set Overriden
Get Overriden")
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
        System.Console.WriteLine("Overriden")
    End Sub
End Class 
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            Dim cl3 = compilation.GetTypeByMetadataName("CL3")
            Dim test = cl3.GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub CL3.Test(x As System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) ())", test.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="Overriden")
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
            Assert.True(base1.IsSameTypeIgnoringCustomModifiers(base2))
            Assert.NotEqual(base1, base2)

            Assert.True(base3.HasTypeArgumentsCustomModifiers)
            Assert.True(base1.IsSameTypeIgnoringCustomModifiers(base3))
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
            Assert.Same(compilation1.SourceModule.CorLibrary(), DirectCast(test.Parameters.First.Type, NamedTypeSymbol).TypeArgumentsCustomModifiers.First.First.Modifier.ContainingAssembly)

            Dim compilation2 = CreateCompilationWithMscorlib45({}, references:={New VisualBasicCompilationReference(compilation1)})

            test = compilation2.GetTypeByMetadataName("Module1").GetMember(Of MethodSymbol)("Test")
            Assert.Equal("Sub Module1.Test(x As System.Nullable(Of System.Int32 modopt(System.Runtime.CompilerServices.IsLong)))", test.ToTestDisplayString())

            Assert.IsType(Of VisualBasic.Symbols.Retargeting.RetargetingAssemblySymbol)(test.ContainingAssembly)
            Assert.Same(compilation2.SourceModule.CorLibrary(), test.Parameters.First.Type.OriginalDefinition.ContainingAssembly)
            Assert.Same(compilation2.SourceModule.CorLibrary(), DirectCast(test.Parameters.First.Type, NamedTypeSymbol).TypeArgumentsCustomModifiers.First.First.Modifier.ContainingAssembly)

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
        System.Console.WriteLine("Overriden")
        return x(y)
    End Function
End Class 

]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Test 1
Test 2
Overriden
Test 3
Overriden
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
        System.Console.WriteLine("Overriden")
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Test
Overriden")
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
        System.Console.WriteLine("Overriden")
    end Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Test
Overriden")
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
        System.Console.WriteLine("Overriden")
    end Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Test
Overriden")
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
        System.Console.WriteLine("Overriden")
        return Nothing
    end Function
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="Test
Overriden")
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

    End Class
End Namespace
