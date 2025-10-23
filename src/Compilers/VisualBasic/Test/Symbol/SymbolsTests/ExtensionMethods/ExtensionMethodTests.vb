' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Basic.Reference.Assemblies
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.ExtensionMethods

    Public Class ExtensionMethodTests : Inherits BasicTestBase

        <Fact>
        Public Sub DetectingExtensionAttributeOnImport1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="DetectingExtensionAttributeOnImport">
        <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

End Module
        </file>
    </compilation>, {Net40.References.SystemCore})

            Dim enumerable As NamedTypeSymbol = compilation1.GetTypeByMetadataName("System.Linq.Enumerable")

            Assert.True(enumerable.ContainingAssembly.MightContainExtensionMethods)
            Assert.True(enumerable.ContainingModule.MightContainExtensionMethods)
            Assert.True(enumerable.MightContainExtensionMethods)

            For Each [select] As MethodSymbol In enumerable.GetMembers("Select")
                Assert.True([select].IsExtensionMethod)
            Next

        End Sub

        <Fact>
        Public Sub DetectingExtensionAttributeOnImport2()

            Dim customIL = <![CDATA[
.assembly extern mscorlib
{
}
.assembly extern System.Core
{
}

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi Module1
       extends [mscorlib]System.Object
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Module1::.ctor

  .method private specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::.cctor

  .method public static void  Test1(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test1

  .method public specialname static int32 
          get_Test2() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Module1::get_Test2

  .method public specialname static int32 
          get_Test3() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Module1::get_Test3

  .method public specialname static void 
          set_Test3(int32 'value') cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::set_Test3

  .method public static void  Test4() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test4

  .method public static void  Test5([opt] int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    .param [1] = int32(0x00000000)
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test5

  .method public static void  Test6(int32[] x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test6

  .method public static void  Test7<(class [mscorlib]System.Collections.Generic.IList`1<!!U[]>) T,U>(!!T x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test7

  .method public static void  Test8<T,U>(!!T x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test8

  .method public instance void  Test9(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test9

  .property int32 Test2()
  {
    .get int32 Module1::get_Test2()
  } // end of property Module1::Test2
  .property int32 Test3()
  {
    .get int32 Module1::get_Test3()
    .set void Module1::set_Test3(int32)
  } // end of property Module1::Test3
} // end of class Module1]]>

            Using reference = IlasmUtilities.CreateTempAssembly(customIL.Value, prependDefaultHeader:=False)

                Dim ILRef = MetadataReference.CreateFromImage(ReadFromFile(reference.Path))

                Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
        <compilation name="DetectingExtensionAttributeOnImport">
            <file name="a.vb">
Module M1
    Sub Main()
    End Sub
End Module
        </file>
        </compilation>, {ILRef})

                Dim module1 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Module1")

                Assert.NotSame(compilation1.Assembly, module1.ContainingAssembly)
                Assert.True(module1.ContainingAssembly.MightContainExtensionMethods)
                Assert.True(module1.ContainingModule.MightContainExtensionMethods)
                Assert.True(module1.MightContainExtensionMethods)

                For Each method As MethodSymbol In module1.GetMembers().OfType(Of MethodSymbol)()
                    Select Case method.Name
                        Case "Test1", "Test8"
                            Assert.True(method.IsExtensionMethod)

                        Case Else
                            Assert.False(method.IsExtensionMethod)
                    End Select
                Next

                Assert.Equal(13, module1.GetMembers().Length)

            End Using

        End Sub

        <Fact>
        Public Sub DetectingExtensionAttributeOnImport3()

            Dim customIL = <![CDATA[
.assembly extern mscorlib
{
}
.assembly extern System.Core
{
}

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi Module2
       extends [mscorlib]System.Object
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

.class auto ansi nested public Module1
        extends [mscorlib]System.Object
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Module1::.ctor

  .method private specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::.cctor

  .method public static void  Test1(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test1

  .method public specialname static int32 
          get_Test2() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Module1::get_Test2

  .method public specialname static int32 
          get_Test3() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Module1::get_Test3

  .method public specialname static void 
          set_Test3(int32 'value') cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::set_Test3

  .method public static void  Test4() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test4

  .method public static void  Test5([opt] int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    .param [1] = int32(0x00000000)
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test5

  .method public static void  Test6(int32[] x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test6

  .method public static void  Test7<(class [mscorlib]System.Collections.Generic.IList`1<!!U[]>) T,U>(!!T x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test7

  .method public static void  Test8<T,U>(!!T x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test8

  .method public instance void  Test9(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test9

  .property int32 Test2()
  {
    .get int32 Module2/Module1::get_Test2()
  } // end of property Module1::Test2
  .property int32 Test3()
  {
    .get int32 Module2/Module1::get_Test3()
    .set void Module2/Module1::set_Test3(int32)
  } // end of property Module1::Test3
} // end of class Module1

} // end of class Module2
]]>

            Using reference = IlasmUtilities.CreateTempAssembly(customIL.Value, prependDefaultHeader:=False)

                Dim ILRef = MetadataReference.CreateFromImage(ReadFromFile(reference.Path))

                Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
        <compilation name="DetectingExtensionAttributeOnImport">
            <file name="a.vb">
Module M1
    Sub Main()
    End Sub
End Module
        </file>
        </compilation>, {ILRef})

                Dim module1 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Module2+Module1")

                Assert.NotSame(compilation1.Assembly, module1.ContainingAssembly)
                Assert.True(module1.ContainingAssembly.MightContainExtensionMethods)
                Assert.True(module1.ContainingModule.MightContainExtensionMethods)
                Assert.True(module1.ContainingType.MightContainExtensionMethods)
                Assert.False(module1.MightContainExtensionMethods)

                For Each method As MethodSymbol In module1.GetMembers().OfType(Of MethodSymbol)()
                    Assert.False(method.IsExtensionMethod)
                Next

                Assert.Equal(13, module1.GetMembers().Length)
            End Using

        End Sub

        <Fact>
        Public Sub DetectingExtensionAttributeOnImport4()

            Dim customIL = <![CDATA[
.assembly extern mscorlib
{
}
.assembly extern System.Core
{
}

.assembly '<<GeneratedFileName>>'
{
}
.module '<<GeneratedFileName>>.dll'

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi Module1
       extends [mscorlib]System.Object
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Module1::.ctor

  .method private specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::.cctor

  .method public static void  Test1(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test1

  .method public specialname static int32 
          get_Test2() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Module1::get_Test2

  .method public specialname static int32 
          get_Test3() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Module1::get_Test3

  .method public specialname static void 
          set_Test3(int32 'value') cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::set_Test3

  .method public static void  Test4() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test4

  .method public static void  Test5([opt] int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    .param [1] = int32(0x00000000)
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test5

  .method public static void  Test6(int32[] x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test6

  .method public static void  Test7<(class [mscorlib]System.Collections.Generic.IList`1<!!U[]>) T,U>(!!T x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test7

  .method public static void  Test8<T,U>(!!T x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test8

  .method public instance void  Test9(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test9

  .property int32 Test2()
  {
    .get int32 Module1::get_Test2()
  } // end of property Module1::Test2
  .property int32 Test3()
  {
    .get int32 Module1::get_Test3()
    .set void Module1::set_Test3(int32)
  } // end of property Module1::Test3
} // end of class Module1]]>

            Using reference = IlasmUtilities.CreateTempAssembly(customIL.Value, prependDefaultHeader:=False)

                Dim ILRef = MetadataReference.CreateFromImage(ReadFromFile(reference.Path))

                Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
        <compilation name="DetectingExtensionAttributeOnImport">
            <file name="a.vb">
Module M1
    Sub Main()
    End Sub
End Module
        </file>
        </compilation>, {ILRef})

                Dim module1 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Module1")

                Assert.NotSame(compilation1.Assembly, module1.ContainingAssembly)
                Assert.False(module1.ContainingAssembly.MightContainExtensionMethods)
                Assert.False(module1.ContainingModule.MightContainExtensionMethods)
                Assert.False(module1.MightContainExtensionMethods)

                For Each method As MethodSymbol In module1.GetMembers().OfType(Of MethodSymbol)()
                    Assert.False(method.IsExtensionMethod)
                Next

                Assert.Equal(13, module1.GetMembers().Length)
            End Using

        End Sub

        <Fact>
        Public Sub DetectingExtensionAttributeOnImport5()

            Dim customIL = <![CDATA[
.assembly extern mscorlib
{
}
.assembly extern System.Core
{
}

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi Module1
       extends [mscorlib]System.Object
{

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Module1::.ctor

  .method private specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::.cctor

  .method public static void  Test1(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test1

  .method public specialname static int32 
          get_Test2() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Module1::get_Test2

  .method public specialname static int32 
          get_Test3() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Module1::get_Test3

  .method public specialname static void 
          set_Test3(int32 'value') cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::set_Test3

  .method public static void  Test4() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test4

  .method public static void  Test5([opt] int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    .param [1] = int32(0x00000000)
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test5

  .method public static void  Test6(int32[] x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test6

  .method public static void  Test7<(class [mscorlib]System.Collections.Generic.IList`1<!!U[]>) T,U>(!!T x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test7

  .method public static void  Test8<T,U>(!!T x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test8

  .method public instance void  Test9(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test9

  .property int32 Test2()
  {
    .get int32 Module1::get_Test2()
  } // end of property Module1::Test2
  .property int32 Test3()
  {
    .get int32 Module1::get_Test3()
    .set void Module1::set_Test3(int32)
  } // end of property Module1::Test3
} // end of class Module1]]>

            Using reference = IlasmUtilities.CreateTempAssembly(customIL.Value, prependDefaultHeader:=False)

                Dim ILRef = MetadataReference.CreateFromImage(ReadFromFile(reference.Path))

                Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
        <compilation name="DetectingExtensionAttributeOnImport">
            <file name="a.vb">
Module M1
    Sub Main()
    End Sub
End Module
        </file>
        </compilation>, {ILRef})

                Dim module1 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Module1")

                Assert.NotSame(compilation1.Assembly, module1.ContainingAssembly)
                Assert.True(module1.ContainingAssembly.MightContainExtensionMethods)
                Assert.True(module1.ContainingModule.MightContainExtensionMethods)
                Assert.False(module1.MightContainExtensionMethods)

                For Each method As MethodSymbol In module1.GetMembers().OfType(Of MethodSymbol)()
                    Assert.False(method.IsExtensionMethod)
                Next

                Assert.Equal(13, module1.GetMembers().Length)
            End Using

        End Sub

        <Fact>
        Public Sub DetectingExtensionAttributeOnImport6()

            Dim customIL = <![CDATA[
.assembly extern mscorlib
{
}
.assembly extern System.Core
{
}

.module '<<GeneratedFileName>>.dll'

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi Module1
       extends [mscorlib]System.Object
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Module1::.ctor

  .method private specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::.cctor

  .method public static void  Test1(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test1

  .method public specialname static int32 
          get_Test2() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Module1::get_Test2

  .method public specialname static int32 
          get_Test3() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Module1::get_Test3

  .method public specialname static void 
          set_Test3(int32 'value') cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::set_Test3

  .method public static void  Test4() cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test4

  .method public static void  Test5([opt] int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    .param [1] = int32(0x00000000)
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test5

  .method public static void  Test6(int32[] x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test6

  .method public static void  Test7<(class [mscorlib]System.Collections.Generic.IList`1<!!U[]>) T,U>(!!T x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test7

  .method public static void  Test8<T,U>(!!T x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test8

  .method public instance void  Test9(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module1::Test9

  .property int32 Test2()
  {
    .get int32 Module1::get_Test2()
  } // end of property Module1::Test2
  .property int32 Test3()
  {
    .get int32 Module1::get_Test3()
    .set void Module1::set_Test3(int32)
  } // end of property Module1::Test3
} // end of class Module1

// =============================================================

.custom ([mscorlib]System.Runtime.CompilerServices.AssemblyAttributesGoHere) instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
]]>

            Using reference = IlasmUtilities.CreateTempAssembly(customIL.Value, prependDefaultHeader:=False)

                Dim ILRef = ModuleMetadata.CreateFromImage(File.ReadAllBytes(reference.Path)).GetReference()

                Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
        <compilation name="DetectingExtensionAttributeOnImport">
            <file name="a.vb">
Module M1
    Sub Main()
    End Sub
End Module
        </file>
        </compilation>, {ILRef})

                Dim module1 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Module1")

                Assert.Same(compilation1.Assembly, module1.ContainingAssembly)
                Assert.True(module1.ContainingAssembly.MightContainExtensionMethods)
                Assert.NotSame(compilation1.Assembly.Modules(0), module1.ContainingModule)
                Assert.False(module1.ContainingModule.MightContainExtensionMethods)
                Assert.False(module1.MightContainExtensionMethods)

                For Each method As MethodSymbol In module1.GetMembers().OfType(Of MethodSymbol)()
                    Assert.False(method.IsExtensionMethod)
                Next

                Assert.Equal(13, module1.GetMembers().Length)

            End Using

        End Sub

        <Fact>
        Public Sub MightContainExtensionMethods_InSource()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MightContainExtensionMethods_InSource">
        <file name="a.vb">
Module Module1

    Module Module2
    End Module

End Module

Class Class1
End Class
        </file>
    </compilation>)

            Dim module1 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Module1")

            Assert.True(module1.ContainingAssembly.MightContainExtensionMethods)
            Assert.True(module1.ContainingModule.MightContainExtensionMethods)
            Assert.False(module1.MightContainExtensionMethods)

            Dim module2 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Module1+Module2")

            Assert.False(module2.MightContainExtensionMethods)
            Assert.Equal(TypeKind.Module, module2.TypeKind)

            Dim class1 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Class1")
            Assert.False(class1.MightContainExtensionMethods)

        End Sub

        <Fact>
        Public Sub DeclaringExtensionMethods1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="DeclaringExtensionMethod1">
        <file name="a.vb">
Module Module1

    &lt;System.Runtime.CompilerServices.Extension()&gt; 'Test1
    Sub Test1(x As Integer)
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test2
    ReadOnly Property Test2 As Integer
        Get
            Return Nothing
        End Get
    End Property

    Property Test3 As Integer
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Get
        Get
            Return Nothing
        End Get
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Set
        Set
        End Set
    End Property

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test4
    Sub Test4()
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test5
    Sub Test5(Optional x As Integer = 0)
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test6
    Sub Test6(ParamArray x As Integer())
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test7
    Sub Test7(Of T As U, U)(x As T)
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test8
    Sub Test8(Of T, U)(x As T)
    End Sub
End Module
        </file>
    </compilation>, {Net40.References.SystemCore})

            Dim module1 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Module1")

            For Each method As MethodSymbol In module1.GetMembers().OfType(Of MethodSymbol)()
                Select Case method.Name
                    Case "Test1", "Test8"
                        Assert.True(method.IsExtensionMethod)

                    Case Else
                        Assert.False(method.IsExtensionMethod)
                End Select
            Next

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30662: Attribute 'ExtensionAttribute' cannot be applied to 'Test2' because the attribute is not valid on this declaration type.
    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test2
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36550: 'Extension' attribute can be applied only to 'Module', 'Sub', or 'Function' declarations.
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Get
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36550: 'Extension' attribute can be applied only to 'Module', 'Sub', or 'Function' declarations.
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Set
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36552: Extension methods must declare at least one parameter. The first parameter specifies which type to extend.
    Sub Test4()
        ~~~~~
BC36553: 'Optional' cannot be applied to the first parameter of an extension method. The first parameter specifies which type to extend.
    Sub Test5(Optional x As Integer = 0)
                       ~
BC36554: 'ParamArray' cannot be applied to the first parameter of an extension method. The first parameter specifies which type to extend.
    Sub Test6(ParamArray x As Integer())
                         ~
BC36561: Extension method 'Test7' has type constraints that can never be satisfied.
    Sub Test7(Of T As U, U)(x As T)
        ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub DeclaringExtensionMethods2()
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="DeclaringExtensionMethod2">
        <file name="a.vb">
Class Module2

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test1
    Sub Test1(x As Integer)
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test2
    ReadOnly Property Test2 As Integer
        Get
            Return Nothing
        End Get
    End Property

    Property Test3 As Integer
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Get
        Get
            Return Nothing
        End Get
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Set
        Set
        End Set
    End Property

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test4
    Sub Test4()
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test5
    Sub Test5(Optional x As Integer = 0)
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test6
    Sub Test6(ParamArray x As Integer())
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test7
    Sub Test7(Of T As U, U)(x As T)
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test8
    Sub Test8(Of T, U)(x As T)
    End Sub
End Class
        </file>
    </compilation>, {Net40.References.SystemCore})

            Dim module2 As NamedTypeSymbol = compilation2.GetTypeByMetadataName("Module2")

            For Each method As MethodSymbol In module2.GetMembers().OfType(Of MethodSymbol)()
                Assert.False(method.IsExtensionMethod)
            Next

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected>
BC36551: Extension methods can be defined only in modules.
    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test1
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30662: Attribute 'ExtensionAttribute' cannot be applied to 'Test2' because the attribute is not valid on this declaration type.
    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test2
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36550: 'Extension' attribute can be applied only to 'Module', 'Sub', or 'Function' declarations.
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Get
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36550: 'Extension' attribute can be applied only to 'Module', 'Sub', or 'Function' declarations.
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Set
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36551: Extension methods can be defined only in modules.
    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test4
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36551: Extension methods can be defined only in modules.
    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test5
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36551: Extension methods can be defined only in modules.
    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test6
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36551: Extension methods can be defined only in modules.
    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test7
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36551: Extension methods can be defined only in modules.
    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test8
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub DeclaringExtensionMethods3()
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="DeclaringExtensionMethod3">
        <file name="a.vb">
&lt;System.Runtime.CompilerServices.Extension()&gt; 'C
Class C
End Class

&lt;System.Runtime.CompilerServices.Extension()&gt; 'S
Structure S
End Structure

&lt;System.Runtime.CompilerServices.Extension()&gt; 'I
Interface I
End Interface

&lt;System.Runtime.CompilerServices.Extension()&gt; 'E
Enum E
    x
End Enum

&lt;System.Runtime.CompilerServices.Extension()&gt; 'M
Module M
End Module

&lt;System.Runtime.CompilerServices.Extension()&gt; 'D
Delegate Sub D()
        </file>
    </compilation>, {Net40.References.SystemCore})

            For Each type As NamedTypeSymbol In compilation2.SourceModule.GlobalNamespace.GetTypeMembers()
                Assert.False(type.MightContainExtensionMethods)
            Next

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected>
BC36550: 'Extension' attribute can be applied only to 'Module', 'Sub', or 'Function' declarations.
Class C
      ~
BC30662: Attribute 'ExtensionAttribute' cannot be applied to 'S' because the attribute is not valid on this declaration type.
&lt;System.Runtime.CompilerServices.Extension()&gt; 'S
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30662: Attribute 'ExtensionAttribute' cannot be applied to 'I' because the attribute is not valid on this declaration type.
&lt;System.Runtime.CompilerServices.Extension()&gt; 'I
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30662: Attribute 'ExtensionAttribute' cannot be applied to 'E' because the attribute is not valid on this declaration type.
&lt;System.Runtime.CompilerServices.Extension()&gt; 'E
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30662: Attribute 'ExtensionAttribute' cannot be applied to 'D' because the attribute is not valid on this declaration type.
&lt;System.Runtime.CompilerServices.Extension()&gt; 'D
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub DeclaringExtensionMethods4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="DeclaringExtensionMethod4">
        <file name="a.vb">
Module Module2

&lt;System.Runtime.CompilerServices.Extension()&gt; 'Module1
Module Module1

    &lt;System.Runtime.CompilerServices.Extension()&gt; 'Test1
    Sub Test1(x As Integer)
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test2
    ReadOnly Property Test2 As Integer
        Get
            Return Nothing
        End Get
    End Property

    Property Test3 As Integer
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Get
        Get
            Return Nothing
        End Get
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Set
        Set
        End Set
    End Property

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test4
    Sub Test4()
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test5
    Sub Test5(Optional x As Integer = 0)
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test6
    Sub Test6(ParamArray x As Integer())
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test7
    Sub Test7(Of T As U, U)(x As T)
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test8
    Sub Test8(Of T, U)(x As T)
    End Sub
End Module
End Module
        </file>
    </compilation>, {Net40.References.SystemCore})

            Dim module1 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Module2+Module1")

            Assert.False(module1.MightContainExtensionMethods)

            For Each method As MethodSymbol In module1.GetMembers().OfType(Of MethodSymbol)()
                Assert.False(method.IsExtensionMethod)
            Next

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30617: 'Module' statements can occur only at file or namespace level.
&lt;System.Runtime.CompilerServices.Extension()&gt; 'Module1
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30662: Attribute 'ExtensionAttribute' cannot be applied to 'Test2' because the attribute is not valid on this declaration type.
    &lt;System.Runtime.CompilerServices.Extension()&gt; ' Test2
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36550: 'Extension' attribute can be applied only to 'Module', 'Sub', or 'Function' declarations.
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Get
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36550: 'Extension' attribute can be applied only to 'Module', 'Sub', or 'Function' declarations.
        &lt;System.Runtime.CompilerServices.Extension()&gt;  ' On Set
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36552: Extension methods must declare at least one parameter. The first parameter specifies which type to extend.
    Sub Test4()
        ~~~~~
BC36553: 'Optional' cannot be applied to the first parameter of an extension method. The first parameter specifies which type to extend.
    Sub Test5(Optional x As Integer = 0)
                       ~
BC36554: 'ParamArray' cannot be applied to the first parameter of an extension method. The first parameter specifies which type to extend.
    Sub Test6(ParamArray x As Integer())
                         ~
BC36561: Extension method 'Test7' has type constraints that can never be satisfied.
    Sub Test7(Of T As U, U)(x As T)
        ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub DetectingAbsenceOfExtensionMethods1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="DeclaringExtensionMethod1">
        <file name="a.vb">
Module Module1
    &lt;System.Runtime.CompilerServices.Extension()&gt; 'Test1
    Sub Test1(x As Integer)
    End Sub
End Module
        </file>
    </compilation>)

            Dim module1 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Module1")

            Assert.True(module1.MightContainExtensionMethods)
            Assert.True(module1.ContainingModule.MightContainExtensionMethods)
            Assert.True(module1.ContainingAssembly.MightContainExtensionMethods)

            DirectCast(module1, SourceNamedTypeSymbol).GenerateDeclarationErrors(Nothing)

            Assert.False(module1.MightContainExtensionMethods)
            Assert.True(module1.ContainingModule.MightContainExtensionMethods)
            Assert.True(module1.ContainingAssembly.MightContainExtensionMethods)

            Dim containsExtensions As Boolean
            DirectCast(module1.ContainingModule, SourceModuleSymbol).GetAllDeclarationErrors(BindingDiagnosticBag.Discarded, Nothing, containsExtensions)

            Assert.False(module1.MightContainExtensionMethods)
            Assert.False(module1.ContainingModule.MightContainExtensionMethods)
            Assert.False(module1.ContainingAssembly.MightContainExtensionMethods)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1,
<expected>
BC30002: Type 'System.Runtime.CompilerServices.Extension' is not defined.
    &lt;System.Runtime.CompilerServices.Extension()&gt; 'Test1
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub DetectingAbsenceOfExtensionMethods2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="DeclaringExtensionMethod1">
        <file name="a.vb">
Module Module1
Module Module2
    &lt;System.Runtime.CompilerServices.Extension()&gt; 'Test1
    Sub Test1(x As Integer)
    End Sub
End Module
End Module
        </file>
    </compilation>, {Net40.References.SystemCore})

            Dim module1 As NamedTypeSymbol = compilation1.GetTypeByMetadataName("Module1")

            Assert.False(module1.MightContainExtensionMethods)
            Assert.True(module1.ContainingModule.MightContainExtensionMethods)
            Assert.True(module1.ContainingAssembly.MightContainExtensionMethods)

            DirectCast(module1, SourceNamedTypeSymbol).GenerateDeclarationErrors(Nothing)

            Assert.False(module1.MightContainExtensionMethods)
            Assert.True(module1.ContainingModule.MightContainExtensionMethods)
            Assert.True(module1.ContainingAssembly.MightContainExtensionMethods)

            Dim containsExtensions As Boolean
            DirectCast(module1.ContainingModule, SourceModuleSymbol).GetAllDeclarationErrors(BindingDiagnosticBag.Discarded, Nothing, containsExtensions)

            Assert.False(module1.MightContainExtensionMethods)
            Assert.False(module1.ContainingModule.MightContainExtensionMethods)
            Assert.False(module1.ContainingAssembly.MightContainExtensionMethods)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30617: 'Module' statements can occur only at file or namespace level.
Module Module2
~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub EmitExtensionAttribute1()
            Dim compilationDef =
    <compilation name="EmitExtensionAttribute1">
        <file name="a.vb">
Module Module1
    &lt;System.Runtime.CompilerServices.Extension()&gt; 
    Sub Test1(x As Integer)
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={Net40.References.SystemCore},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Assert.Equal(1, m.ContainingAssembly.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Dim module1 = m.ContainingAssembly.GetTypeByMetadataName("Module1")
                                                  Assert.Equal(1, module1.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Assert.Equal(1, module1.GetMember("Test1").
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                              End Sub)

        End Sub

        <Fact>
        Public Sub EmitExtensionAttribute2()
            Dim compilationDef =
    <compilation name="EmitExtensionAttribute2">
        <file name="a.vb">
&lt;System.Runtime.CompilerServices.Extension()&gt; 
Module Module1
    &lt;System.Runtime.CompilerServices.Extension()&gt; 
    Sub Test1(x As Integer)
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={Net40.References.SystemCore},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Assert.Equal(1, m.ContainingAssembly.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Dim module1 = m.ContainingAssembly.GetTypeByMetadataName("Module1")
                                                  Assert.Equal(1, module1.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Assert.Equal(1, module1.GetMember("Test1").
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                              End Sub)

        End Sub

        <Fact>
        Public Sub EmitExtensionAttribute3()
            Dim compilationDef =
    <compilation name="EmitExtensionAttribute3">
        <file name="a.vb">
&lt;Assembly:System.Runtime.CompilerServices.Extension()&gt; 

&lt;System.Runtime.CompilerServices.Extension()&gt; 
Module Module1
    &lt;System.Runtime.CompilerServices.Extension()&gt; 
    Sub Test1(x As Integer)
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={Net40.References.SystemCore},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Assert.Equal(1, m.ContainingAssembly.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Dim module1 = m.ContainingAssembly.GetTypeByMetadataName("Module1")
                                                  Assert.Equal(1, module1.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Assert.Equal(1, module1.GetMember("Test1").
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                              End Sub)

        End Sub

        <Fact>
        Public Sub EmitExtensionAttribute4()
            Dim compilationDef =
    <compilation name="EmitExtensionAttribute4">
        <file name="a.vb">
&lt;Assembly:System.Runtime.CompilerServices.Extension()&gt; 

&lt;System.Runtime.CompilerServices.Extension()&gt; 
Module Module1
    Sub Test1(x As Integer)
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={Net40.References.SystemCore},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Assert.Equal(1, m.ContainingAssembly.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Dim module1 = m.ContainingAssembly.GetTypeByMetadataName("Module1")
                                                  Assert.Equal(1, module1.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Assert.Equal(0, module1.GetMember("Test1").
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                              End Sub)

        End Sub

        <Fact>
        Public Sub EmitExtensionAttribute5()
            Dim compilationDef =
    <compilation name="EmitExtensionAttribute5">
        <file name="a.vb">
&lt;System.Runtime.CompilerServices.Extension()&gt; 
Module Module1
    Sub Test1(x As Integer)
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={Net40.References.SystemCore},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Assert.Equal(0, m.ContainingAssembly.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Dim module1 = m.ContainingAssembly.GetTypeByMetadataName("Module1")
                                                  Assert.Equal(1, module1.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Assert.Equal(0, module1.GetMember("Test1").
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                              End Sub)

        End Sub

        <Fact>
        Public Sub EmitExtensionAttribute6()
            Dim compilationDef =
    <compilation name="EmitExtensionAttribute6">
        <file name="a.vb">
Module Module1
    Sub Test1(x As Integer)
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(compilationDef,
                             references:={Net40.References.SystemCore},
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Assert.Equal(0, m.ContainingAssembly.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Dim module1 = m.ContainingAssembly.GetTypeByMetadataName("Module1")
                                                  Assert.Equal(0, module1.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Assert.Equal(0, module1.GetMember("Test1").
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                              End Sub)

        End Sub

        <Fact>
        Public Sub EmitExtensionAttribute7()
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EmitExtensionAttribute7_1">
        <file name="a.vb">
Namespace System.Runtime.CompilerServices
    Class ExtensionAttribute
    End Class
End Namespace
        </file>
    </compilation>)

            CompilationUtils.AssertNoErrors(compilation2)

            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EmitExtensionAttribute7_2">
        <file name="a.vb">
Namespace System.Runtime.CompilerServices
    Class ExtensionAttribute
    End Class
End Namespace
        </file>
    </compilation>)

            CompilationUtils.AssertNoErrors(compilation3)

            Dim compilation1Def =
    <compilation name="EmitExtensionAttribute7_3">
        <file name="a.vb">
Module Module1
    Sub Main()
        Call 345.Test1()
    End Sub

    &lt;System.Runtime.CompilerServices.Extension()&gt; 'Test1
    Sub Test1(x As Integer)
        System.Console.WriteLine(x)
    End Sub

End Module
        </file>
    </compilation>

            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilation1Def,
                                                                                                       {Net40.References.SystemCore,
                                                                                                        New VisualBasicCompilationReference(compilation2),
                                                                                                        New VisualBasicCompilationReference(compilation3)},
                                                                                                       TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, expectedOutput:="345")

            Dim compilation3_1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EmitExtensionAttribute7_3_1">
        <file name="a.vb">
Namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Public Class extensionattribute : Inherits Attribute
    End Class
End Namespace
        </file>
    </compilation>)

            Dim compilation1_1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilation1Def,
                                                                                                        {Net40.References.SystemCore,
                                                                                                         New VisualBasicCompilationReference(compilation3_1)},
                                                                                                         TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1_1,
<expected>
    <![CDATA[
BC30560: 'ExtensionAttribute' is ambiguous in the namespace 'System.Runtime.CompilerServices'.
    <System.Runtime.CompilerServices.Extension()> 'Test1
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation1_1,
<expected>
    <![CDATA[
BC30456: 'Test1' is not a member of 'Integer'.
        Call 345.Test1()
             ~~~~~~~~~
BC30560: 'ExtensionAttribute' is ambiguous in the namespace 'System.Runtime.CompilerServices'.
    <System.Runtime.CompilerServices.Extension()> 'Test1
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
</expected>)

            Dim compilation3_2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EmitExtensionAttribute7_3_2">
        <file name="a.vb">
Namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Friend Class extensionattribute : Inherits Attribute
    End Class
End Namespace
        </file>
    </compilation>)

            Dim compilation1_2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilation1Def,
                                                                                                        {Net40.References.SystemCore,
                                                                                                         New VisualBasicCompilationReference(compilation3_2)},
                                                                                                         TestOptions.ReleaseExe)

            CompileAndVerify(compilation1_2, expectedOutput:="345")

            Dim compilation1_3_Def =
    <compilation name="EmitExtensionAttribute7_3">
        <file name="a.vb">
Module Module1
    Sub Main()
        Call 345.Test1()
    End Sub

    &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt; 'Test1
    Sub Test1(x As Integer)
        System.Console.WriteLine(x)
    End Sub

End Module
        </file>
    </compilation>

            Dim compilation1_3 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilation1_3_Def,
                                                                                                        {Net40.References.SystemCore,
                                                                                                         New VisualBasicCompilationReference(compilation3_1)},
                                                                                                        TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1_3,
<expected>
BC30560: 'ExtensionAttribute' is ambiguous in the namespace 'System.Runtime.CompilerServices'.
    &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt; 'Test1
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation1_3,
<expected>
BC30456: 'Test1' is not a member of 'Integer'.
        Call 345.Test1()
             ~~~~~~~~~
BC30560: 'ExtensionAttribute' is ambiguous in the namespace 'System.Runtime.CompilerServices'.
    &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt; 'Test1
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim compilation1_4_Def =
    <compilation name="EmitExtensionAttribute7_3">
        <file name="a.vb">
Module Module1
    Sub Main()
        Call 345.Test1()
    End Sub

    &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt; 'Test1
    Sub Test1(x As Integer)
        System.Console.WriteLine(x)
    End Sub

End Module

Namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Public Class extensionattribute : Inherits Attribute
    End Class
End Namespace
        </file>
    </compilation>

            Dim compilation1_4 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilation1_4_Def,
                                                                                                        {Net40.References.SystemCore,
                                                                                                         New VisualBasicCompilationReference(compilation3_1)},
                                                                                                        TestOptions.ReleaseExe)

            CompileAndVerify(compilation1_4, expectedOutput:="345")

            Dim compilation3_3 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EmitExtensionAttribute7_3_3">
        <file name="a.vb">
Namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Public Class extensionattribute : Inherits Attribute
        Friend Sub New()
        End Sub
    End Class
End Namespace
        </file>
    </compilation>)

            Dim compilation1_5 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilation1Def,
                                                                                                        {New VisualBasicCompilationReference(compilation3_3)})

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1_5,
<expected>
BC30517: Overload resolution failed because no 'New' is accessible.
    &lt;System.Runtime.CompilerServices.Extension()&gt; 'Test1
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation1_5,
<expected>
BC30456: 'Test1' is not a member of 'Integer'.
        Call 345.Test1()
             ~~~~~~~~~
BC30517: Overload resolution failed because no 'New' is accessible.
    &lt;System.Runtime.CompilerServices.Extension()&gt; 'Test1
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim compilation4 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="EmitExtensionAttribute7_4">
        <file name="a.vb">

&lt;System.Runtime.CompilerServices.Extension()&gt;
Module Module1

    &lt;System.Runtime.CompilerServices.Extension()&gt; 'Test1
    Sub Test1(x As Integer)
    End Sub

End Module
        </file>
    </compilation>, {Net40.References.SystemCore,
                     New VisualBasicCompilationReference(compilation2),
                     New VisualBasicCompilationReference(compilation3)})

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation4,
<expected>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor' is not defined.
Module Module1
       ~~~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation4,
<expected>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor' is not defined.
Module Module1
       ~~~~~~~
</expected>)

            Dim compilation5 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="EmitExtensionAttribute7_5">
        <file name="a.vb">
&lt;Assembly:System.Runtime.CompilerServices.Extension()&gt;            

&lt;System.Runtime.CompilerServices.Extension()&gt;
Module Module1

    &lt;System.Runtime.CompilerServices.Extension()&gt; 'Test1
    Sub Test1(x As Integer)
    End Sub

End Module
        </file>
    </compilation>, {Net40.References.SystemCore,
                     New VisualBasicCompilationReference(compilation2),
                     New VisualBasicCompilationReference(compilation3)})

            CompilationUtils.AssertNoErrors(compilation5)
        End Sub

        <Fact>
        Public Sub EmitExtensionAttribute8()

            ' manually get attributes before emit

            Dim compilationDef =
    <compilation name="EmitExtensionAttribute1">
        <file name="a.vb">
Imports System.Security
Imports System.Security.Permissions
Imports System.Security.Principal

    &lt;assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration:=true)&gt;

Module Module1

    &lt;System.Runtime.CompilerServices.Extension()&gt; 
    Sub Test1(x As Integer)
    End Sub
End Module
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {Net40.References.SystemCore})
            Dim assembly = compilation.SourceModule.ContainingAssembly
            Dim securityAttributes = assembly.GetAttributes()
            Debug.Assert(securityAttributes.Length = 1)

            CompileAndVerify(compilation,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Assert.Equal(1, m.ContainingAssembly.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Dim module1 = m.ContainingAssembly.GetTypeByMetadataName("Module1")
                                                  Assert.Equal(1, module1.
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                                  Assert.Equal(1, module1.GetMember("Test1").
                                                                  GetAttributes("System.Runtime.CompilerServices",
                                                                                "ExtensionAttribute").Count)
                                              End Sub)

        End Sub

        <Fact>
        Public Sub BC36558ERR_ExtensionAttributeInvalid1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ExtensionAttributeInvalid">
        <file name="a.vb">
            Namespace System.Runtime.Compilerservices
                &lt;AttributeUsage(AttributeTargets.Class Or AttributeTargets.Method, AllowMultiple:=False, Inherited:=True)&gt; _
                Class ExtensionAttribute
                    Inherits Attribute
                End Class
            End Namespace

            Module ExtMethods
                &lt;System.Runtime.Compilerservices.Extension()&gt; Function IntegerExtension(ByVal a As Integer) As Integer
                    Return 100
                End Function
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC36558: The custom-designed version of 'System.Runtime.CompilerServices.ExtensionAttribute' found by the compiler is not valid. Its attribute usage flags must be set to allow assemblies, classes, and methods.
                &lt;System.Runtime.Compilerservices.Extension()&gt; Function IntegerExtension(ByVal a As Integer) As Integer
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     </errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC36558ERR_ExtensionAttributeInvalid2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ExtensionAttributeInvalid">
        <file name="a.vb">
            Namespace System.Runtime.CompilerServices
                &lt;AttributeUsage(AttributeTargets.Class Or AttributeTargets.Method, AllowMultiple:=False, Inherited:=True)&gt; _
                Class ExtensionAttribute
                    Inherits Attribute
                End Class
            End Namespace

            Module ExtMethods
                &lt;System.Runtime.Compilerservices.Extension()&gt; 
                Function IntegerExtension(ByVal a As Integer) As Integer
                    Return 100
                End Function
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC36558: The custom-designed version of 'System.Runtime.CompilerServices.ExtensionAttribute' found by the compiler is not valid. Its attribute usage flags must be set to allow assemblies, classes, and methods.
                &lt;System.Runtime.Compilerservices.Extension()&gt; 
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     </errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC36558ERR_ExtensionAttributeInvalid3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ExtensionAttributeInvalid">
        <file name="a.vb">
            Namespace System.Runtime.CompilerServices
                &lt;AttributeUsage(AttributeTargets.Class Or AttributeTargets.Method, AllowMultiple:=False, Inherited:=True)&gt; _
                Class ExtensionAttribute
                    Inherits Attribute
                End Class
            End Namespace

            &lt;System.Runtime.Compilerservices.Extension()&gt; 
            Module ExtMethods
                &lt;System.Runtime.Compilerservices.Extension()&gt; 
                Function IntegerExtension(ByVal a As Integer) As Integer
                    Return 100
                End Function
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC36558: The custom-designed version of 'System.Runtime.CompilerServices.ExtensionAttribute' found by the compiler is not valid. Its attribute usage flags must be set to allow assemblies, classes, and methods.
            &lt;System.Runtime.Compilerservices.Extension()&gt; 
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36558: The custom-designed version of 'System.Runtime.CompilerServices.ExtensionAttribute' found by the compiler is not valid. Its attribute usage flags must be set to allow assemblies, classes, and methods.
                &lt;System.Runtime.Compilerservices.Extension()&gt; 
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     </errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub FlowAnalysis1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="FlowAnalysis1">
        <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices


Module Module1
    Sub Main()
        Dim x As Integer
        x.F1()
    End Sub

    Sub Main2()
        Dim x As Integer
        x.F2()
    End Sub

    Sub Main3()
        Dim x As C1 '3
        x.F3()
    End Sub

    Sub Main4()
        Dim x As C1 '4
        x.F4()
    End Sub

    Sub Main5()
        Dim x As C2 '5
        x.F4()
    End Sub

    Sub Main31()
        Dim x As C1 '31
        Dim d As System.Action = AddressOf x.F3
    End Sub

    Sub Main41()
        Dim x As C1 '41
        Dim d As System.Action = AddressOf x.F4
    End Sub

    Sub Main51()
        Dim x As C2 '51
        Dim d As System.Action = AddressOf x.F4
    End Sub

    &lt;Extension()&gt;
    Sub F1(this As Integer)
    End Sub

    &lt;Extension()&gt;
    Sub F2(ByRef this As Integer)
    End Sub

    &lt;Extension()&gt;
    Sub F3(this As C1)
    End Sub

    &lt;Extension()&gt;
    Sub F4(ByRef this As C1)
    End Sub
End Module

Class C1
End Class

Class C2
    Inherits C1
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
        </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        x.F3()
        ~
BC42030: Variable 'x' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
        x.F4()
        ~
BC42030: Variable 'x' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
        x.F4()
        ~
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim d As System.Action = AddressOf x.F3
                                           ~
BC42030: Variable 'x' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
        Dim d As System.Action = AddressOf x.F4
                                           ~
BC42030: Variable 'x' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
        Dim d As System.Action = AddressOf x.F4
                                           ~
</expected>)

        End Sub

        <Fact()>
        <WorkItem(528983, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528983")>
        Public Sub ExtensionMethodsDeclaredInTypesWithConflictingNamesAreNotVisible()

            'namespace Extensions
            '{
            '    public static class C
            '    {
            '        public static void Goo(this int x) { }
            '    }

            '    public static class D
            '    {
            '        public static void Goo(this int x) { }
            '    }
            '}

            'namespace extensions
            '{
            '    public static class C
            '    {
            '        public static void Goo(this int x) { }
            '    }
            '}

            Dim customIL = <![CDATA[
.assembly extern mscorlib
{
}
.assembly extern System.Core
{
}

.module '<<GeneratedFileName>>.dll'

.class public abstract auto ansi sealed beforefieldinit Extensions.C
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    ret
  }
}

.class public abstract auto ansi sealed beforefieldinit Extensions.D
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    ret
  }
}

.class public abstract auto ansi sealed beforefieldinit extensions.C
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    ret
  }
}
]]>

            Using reference = IlasmUtilities.CreateTempAssembly(customIL.Value, prependDefaultHeader:=False)

                Dim ILRef = ModuleMetadata.CreateFromImage(File.ReadAllBytes(reference.Path)).GetReference()

                Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
        <compilation name="ExtensionMethodsDeclaredInTypesWithConflictingNamesAreNotVisible">
            <file name="a.vb">
Imports Extensions

Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo
    End Sub
End Module
        </file>
        </compilation>, {ILRef})

                compilation1.VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_NameNotMember2, "x.Goo").WithArguments("Goo", "Integer"),
                    Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports Extensions"))
            End Using

        End Sub

        <Fact()>
        Public Sub AttributeErrors_1()
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="DeclaringExtensionMethod3">
        <file name="a.vb"><![CDATA[
<System.Runtime.CompilerServices.Extension()> ' 1
<System.Runtime.CompilerServices.Extension()> ' 2
Class C
        End Class
        ]]></file>
    </compilation>, {Net40.References.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected><![CDATA[
BC30663: Attribute 'ExtensionAttribute' cannot be applied multiple times.
<System.Runtime.CompilerServices.Extension()> ' 2
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36550: 'Extension' attribute can be applied only to 'Module', 'Sub', or 'Function' declarations.
Class C
      ~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub AttributeErrors_2()
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="DeclaringExtensionMethod3">
        <file name="a.vb"><![CDATA[
<System.Runtime.CompilerServices.ExtensionAttribute()> ' 1
<System.Runtime.CompilerServices.ExtensionAttribute()> ' 2
Class C
End Class

Namespace System.Runtime.CompilerServices
    Structure ExtensionAttribute
    End Structure
End Namespace

        ]]></file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected><![CDATA[
BC31503: 'ExtensionAttribute' cannot be used as an attribute because it is not a class.
<System.Runtime.CompilerServices.ExtensionAttribute()> ' 1
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31503: 'ExtensionAttribute' cannot be used as an attribute because it is not a class.
<System.Runtime.CompilerServices.ExtensionAttribute()> ' 2
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub AttributeErrors_3()
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="DeclaringExtensionMethod3">
        <file name="a.vb"><![CDATA[
<System.Runtime.CompilerServices.ExtensionAttribute()> ' 1
<System.Runtime.CompilerServices.ExtensionAttribute()> ' 2
Class C
End Class

Namespace System.Runtime.CompilerServices
    Class ExtensionAttribute
    End Class
End Namespace

        ]]></file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected><![CDATA[
BC31504: 'ExtensionAttribute' cannot be used as an attribute because it does not inherit from 'System.Attribute'.
<System.Runtime.CompilerServices.ExtensionAttribute()> ' 1
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31504: 'ExtensionAttribute' cannot be used as an attribute because it does not inherit from 'System.Attribute'.
<System.Runtime.CompilerServices.ExtensionAttribute()> ' 2
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub AttributeErrors_4()
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="DeclaringExtensionMethod3">
        <file name="a.vb"><![CDATA[
<System.Runtime.CompilerServices.ExtensionAttribute()> ' 1
Class C
End Class

Namespace System.Runtime.CompilerServices
    <System.AttributeUsageAttribute(System.AttributeTargets.Assembly, Inherited := False)> 
    Class ExtensionAttribute
        Inherits System.Attribute
    End Class
End Namespace

        ]]></file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected><![CDATA[
BC30662: Attribute 'ExtensionAttribute' cannot be applied to 'C' because the attribute is not valid on this declaration type.
<System.Runtime.CompilerServices.ExtensionAttribute()> ' 1
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact(), WorkItem(545799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545799")>
        Public Sub SameExtensionMethodSymbol()

            Dim comp = CreateCompilationWithMscorlib40AndReferences(
    <compilation name="SameExtensionMethodSymbol">
        <file name="a.vb"><![CDATA[
Imports System.Collections.Generic

Public Class C
    Public Sub InstanceMethod(Of T)(o As T)
    End Sub
End Class

Public Module Extensions
    <System.Runtime.CompilerServices.Extension()>
    Public Sub ExtensionMethod(Of T)(this As C, o As T)
    End Sub
End Module

Module M

    Sub Main()
        Dim obj = New C()
        obj.InstanceMethod("q")
        obj.ExtensionMethod("c")
    End Sub

End Module
        ]]></file>
    </compilation>, references:={Net40.References.SystemCore})

            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)

            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)()
            ' Invocation
            Dim node = nodes.First()
            Dim node2 = node.Expression
            Assert.Equal("obj.InstanceMethod", node2.ToString())
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, node2.Kind)

            Dim sym = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(sym)
            Assert.Equal("InstanceMethod", sym.Name)
            Dim sym2 = model.GetSymbolInfo(node2).Symbol
            Assert.Equal(sym2, sym)

            node = nodes.Last()
            node2 = node.Expression
            Assert.Equal("obj.ExtensionMethod", node2.ToString())
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, node2.Kind)

            sym = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(sym)
            Assert.Equal("ExtensionMethod", sym.Name)
            sym2 = model.GetSymbolInfo(node2).Symbol
            Assert.Equal(sym2, sym)

        End Sub

        <ConditionalFact(GetType(NoUsedAssembliesValidation))> ' https://github.com/dotnet/roslyn/issues/40680: The test hook is blocked by this issue.
        <WorkItem(40680, "https://github.com/dotnet/roslyn/issues/40680")>
        Public Sub ScriptExtensionMethods()
            Dim source = <![CDATA[
Imports System.Runtime.CompilerServices
<Extension>
Shared Function F(o As Object) As Object
    Return Nothing
End Function
Dim o As New Object()
o.F()]]>
            Dim comp = CreateCompilationWithMscorlib461(
                {VisualBasicSyntaxTree.ParseText(source.Value, TestOptions.Script)})
            comp.VerifyDiagnostics()
            Assert.True(comp.SourceAssembly.MightContainExtensionMethods)
        End Sub

        <Fact>
        Public Sub InteractiveExtensionMethods()
            Dim references = {Net40.References.mscorlib, Net40.References.SystemCore}

            Dim source0 = "
Imports System.Runtime.CompilerServices
<Extension>
Shared Function F(o As Object) As Object
    Return 0
End Function
Dim o As New Object()
? o.F()"

            Dim source1 = "
Imports System.Runtime.CompilerServices
<Extension>
Shared Function G(o As Object) As Object
    Return 1
End Function
Dim o As New Object()
? o.G().F()"

            Dim s0 = VisualBasicCompilation.CreateScriptCompilation(
                "s0.dll",
                syntaxTree:=Parse(source0, TestOptions.Script),
                references:=references)
            s0.VerifyDiagnostics()
            Assert.True(s0.SourceAssembly.MightContainExtensionMethods)

            Dim s1 = VisualBasicCompilation.CreateScriptCompilation(
                "s1.dll",
                syntaxTree:=Parse(source1, TestOptions.Script),
                previousScriptCompilation:=s0,
                references:=references)
            s1.VerifyDiagnostics()
            Assert.True(s1.SourceAssembly.MightContainExtensionMethods)
        End Sub

        <Fact>
        Public Sub ConsumeRefExtensionMethods()
            Dim options = New CSharpParseOptions(CodeAnalysis.CSharp.LanguageVersion.Latest)
            Dim csharp = CreateCSharpCompilation("
public static class Extensions
{
    public static void PrintValue(ref this int p)
    {
        System.Console.Write(p);
    }
}", referencedAssemblies:={Net40.References.mscorlib, Net40.References.SystemCore}, parseOptions:=options).EmitToImageReference()

            Dim vb = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Module Program
    Sub Main()
        Dim value = 5
        value.PrintValue()
    End Sub 
End Module
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseExe, additionalRefs:={csharp})

            CompileAndVerify(vb, expectedOutput:="5")
        End Sub

        <Fact>
        Public Sub ConsumeInExtensionMethods()
            Dim options = New CSharpParseOptions(CodeAnalysis.CSharp.LanguageVersion.Latest)
            Dim csharp = CreateCSharpCompilation("
public static class Extensions
{
    public static void PrintValue(in this int p)
    {
        System.Console.Write(p);
    }
}", referencedAssemblies:={Net40.References.mscorlib, Net40.References.SystemCore}, parseOptions:=options).EmitToImageReference()

            Dim vb = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AssemblyName">
    <file name="a.vb">
        <![CDATA[
Module Program
    Sub Main()
        Dim value = 5
        value.PrintValue()
    End Sub 
End Module
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseExe, additionalRefs:={csharp})

            CompileAndVerify(vb, expectedOutput:="5")
        End Sub

        <Fact>
        <WorkItem(65020, "https://github.com/dotnet/roslyn/issues/65020")>
        Public Sub ReduceExtensionMethodOnReceiverTypeSystemVoid()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Structure C
End Structure
Module E
    <Extension()>
    Friend Sub ExtMethod(o As ValueType)
    End Sub
End Module
]]></file>
</compilation>, {Net40.References.SystemCore})

            Dim extensionMethod = DirectCast(compilation.GetSymbolsWithName("ExtMethod", SymbolFilter.Member).Single(), IMethodSymbol)
            Assert.NotNull(extensionMethod)

            Dim reducedMethodOnC = extensionMethod.ReduceExtensionMethod(compilation.GetTypeByMetadataName("C"))
            Assert.NotNull(reducedMethodOnC)
            Assert.Equal("Sub System.ValueType.ExtMethod()", reducedMethodOnC.ToTestDisplayString())

            Dim reducedMethodOnVoid = extensionMethod.ReduceExtensionMethod(compilation.GetSpecialType(SpecialType.System_Void))
            Assert.Null(reducedMethodOnVoid)
        End Sub

        <Fact>
        Public Sub ReduceExtensionMember_01()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class E
    Public Sub Method()
    End Sub
    Public Property Prop As Integer
End Class
]]></file>
</compilation>)

            compilation.VerifyEmitDiagnostics()

            Dim systemObject As NamedTypeSymbol = compilation.GetSpecialType(SpecialType.System_Object)
            Dim method = DirectCast(compilation.GlobalNamespace.GetTypeMember("E").GetMember(Of MethodSymbol)("Method"), IMethodSymbol)
            Assert.NotNull(method)
            Assert.Null(method.ReduceExtensionMember(systemObject))

            Dim prop = DirectCast(compilation.GlobalNamespace.GetTypeMember("E").GetMember(Of PropertySymbol)("Prop"), IPropertySymbol)
            Assert.Null(prop.ReduceExtensionMember(systemObject))
        End Sub
    End Class

End Namespace

