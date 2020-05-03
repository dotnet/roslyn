' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenGetTypeOperator
        Inherits BasicTestBase

        <Fact>
        Public Sub CodeGen_GetType_Simple()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
            Console.WriteLine(GetType(C))
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
C
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldtoken    "C"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  call       "Sub System.Console.WriteLine(Object)"
  IL_000f:  ret
}
]]>).Compilation
        End Sub

        <Fact>
        Public Sub CodeGen_GetType_NonGeneric()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Namespace FormSource
    Class SourceClass
    End Class

    Structure SourceStructure
    End Structure
    
    Enum SourceEnum 
    e 
    End Enum
    
    Interface SourceInterface
    End Interface
End Namespace

Module Program
    Public Sub Main()
        ' From source
        Console.WriteLine(GetType(FormSource.SourceClass))
        Console.WriteLine(GetType(FormSource.SourceStructure))
        Console.WriteLine(GetType(FormSource.SourceEnum))
        Console.WriteLine(GetType(FormSource.SourceInterface))

        ' From metadata
        Console.WriteLine(GetType(String))
        Console.WriteLine(GetType(Integer))
        Console.WriteLine(GetType(System.IO.FileMode))
        Console.WriteLine(GetType(System.IFormattable))
        Console.WriteLine(GetType(System.Math)) ' static class, but there is no Shared Class in VB from source.

        ' Special (or not so in VB)
        System.Console.WriteLine(GetType(Void))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
FormSource.SourceClass
FormSource.SourceStructure
FormSource.SourceEnum
FormSource.SourceInterface
System.String
System.Int32
System.IO.FileMode
System.IFormattable
System.Math
System.Void
]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size      151 (0x97)
  .maxstack  1
  IL_0000:  ldtoken    "FormSource.SourceClass"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  call       "Sub System.Console.WriteLine(Object)"
  IL_000f:  ldtoken    "FormSource.SourceStructure"
  IL_0014:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0019:  call       "Sub System.Console.WriteLine(Object)"
  IL_001e:  ldtoken    "FormSource.SourceEnum"
  IL_0023:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0028:  call       "Sub System.Console.WriteLine(Object)"
  IL_002d:  ldtoken    "FormSource.SourceInterface"
  IL_0032:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0037:  call       "Sub System.Console.WriteLine(Object)"
  IL_003c:  ldtoken    "String"
  IL_0041:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0046:  call       "Sub System.Console.WriteLine(Object)"
  IL_004b:  ldtoken    "Integer"
  IL_0050:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0055:  call       "Sub System.Console.WriteLine(Object)"
  IL_005a:  ldtoken    "System.IO.FileMode"
  IL_005f:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0064:  call       "Sub System.Console.WriteLine(Object)"
  IL_0069:  ldtoken    "System.IFormattable"
  IL_006e:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0073:  call       "Sub System.Console.WriteLine(Object)"
  IL_0078:  ldtoken    "System.Math"
  IL_007d:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0082:  call       "Sub System.Console.WriteLine(Object)"
  IL_0087:  ldtoken    "Void"
  IL_008c:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0091:  call       "Sub System.Console.WriteLine(Object)"
  IL_0096:  ret
}
]]>).Compilation
        End Sub

        <Fact>
        Public Sub CodeGen_GetType_Generic()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class Class1(Of T)
End Class
Class Class2(Of T, U)
End Class

Module Program
    Public Sub Main()
        ' From source
        System.Console.WriteLine(GetType(Class1(of Integer)))
        System.Console.WriteLine(GetType(Class1(of Class1(of Integer))))
        System.Console.WriteLine(GetType(Class2(of Integer, long)))
        System.Console.WriteLine(GetType(Class2(of Class1(of Integer), Class1(of long))))

        ' From metadata
        System.Console.WriteLine(GetType(Func(of Integer)))
        System.Console.WriteLine(GetType(Func(of Class1(of Integer))))
        System.Console.WriteLine(GetType(Func(of Class1(of Integer), Class1(of long))))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
Class1`1[System.Int32]
Class1`1[Class1`1[System.Int32]]
Class2`2[System.Int32,System.Int64]
Class2`2[Class1`1[System.Int32],Class1`1[System.Int64]]
System.Func`1[System.Int32]
System.Func`1[Class1`1[System.Int32]]
System.Func`2[Class1`1[System.Int32],Class1`1[System.Int64]]
]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size      106 (0x6a)
  .maxstack  1
  IL_0000:  ldtoken    "Class1(Of Integer)"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  call       "Sub System.Console.WriteLine(Object)"
  IL_000f:  ldtoken    "Class1(Of Class1(Of Integer))"
  IL_0014:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0019:  call       "Sub System.Console.WriteLine(Object)"
  IL_001e:  ldtoken    "Class2(Of Integer, Long)"
  IL_0023:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0028:  call       "Sub System.Console.WriteLine(Object)"
  IL_002d:  ldtoken    "Class2(Of Class1(Of Integer), Class1(Of Long))"
  IL_0032:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0037:  call       "Sub System.Console.WriteLine(Object)"
  IL_003c:  ldtoken    "System.Func(Of Integer)"
  IL_0041:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0046:  call       "Sub System.Console.WriteLine(Object)"
  IL_004b:  ldtoken    "System.Func(Of Class1(Of Integer))"
  IL_0050:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0055:  call       "Sub System.Console.WriteLine(Object)"
  IL_005a:  ldtoken    "System.Func(Of Class1(Of Integer), Class1(Of Long))"
  IL_005f:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0064:  call       "Sub System.Console.WriteLine(Object)"
  IL_0069:  ret
}
]]>).Compilation
        End Sub

        <Fact>
        Public Sub CodeGen_GetType_TypeParameter()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class Class1(Of T)
    Public Shared Sub Print()
        System.Console.WriteLine(GetType(T))
        System.Console.WriteLine(GetType(Class1(Of T)))
    End Sub

    Public Shared Sub Print(Of U)()
        System.Console.WriteLine(GetType(U))
        System.Console.WriteLine(GetType(Class1(Of U)))
    End Sub
End Class

Module Program
    Public Sub Main()
        Class1(Of Integer).Print()
        Class1(Of Class1(Of Integer)).Print()

        Class1(Of Integer).Print(Of Long)()
        Class1(Of Class1(OF Integer)).Print(Of Class1(Of Long))()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
System.Int32
Class1`1[System.Int32]
Class1`1[System.Int32]
Class1`1[Class1`1[System.Int32]]
System.Int64
Class1`1[System.Int64]
Class1`1[System.Int64]
Class1`1[Class1`1[System.Int64]]
]]>)

            compilation1.VerifyIL("Class1(Of T).Print", <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldtoken    "T"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  call       "Sub System.Console.WriteLine(Object)"
  IL_000f:  ldtoken    "Class1(Of T)"
  IL_0014:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0019:  call       "Sub System.Console.WriteLine(Object)"
  IL_001e:  ret
}
            ]]>)

            compilation1.VerifyIL("Class1(Of T).Print(Of U)", <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldtoken    "U"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  call       "Sub System.Console.WriteLine(Object)"
  IL_000f:  ldtoken    "Class1(Of U)"
  IL_0014:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0019:  call       "Sub System.Console.WriteLine(Object)"
  IL_001e:  ret
}
            ]]>)
        End Sub

        <Fact>
        Public Sub CodeGen_GetType_UnboundGeneric()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class Class1(Of T)
End Class
Class Class2(Of T, U)
End Class

Module Program
    Public Sub Main()
        ' From source
        System.Console.WriteLine(GetType(Class1(Of )))
        System.Console.WriteLine(GetType(Class2(Of ,)))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
Class1`1[T]
Class2`2[T,U]
]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldtoken    "Class1(Of T)"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  call       "Sub System.Console.WriteLine(Object)"
  IL_000f:  ldtoken    "Class2(Of T, U)"
  IL_0014:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0019:  call       "Sub System.Console.WriteLine(Object)"
  IL_001e:  ret
}
]]>).Compilation
        End Sub

        <Fact,
         WorkItem(9850, "https://github.com/dotnet/roslyn/issues/9850"),
         WorkItem(542581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542581")>
        Public Sub CodeGen_GetType_InheritedNestedTypeThroughUnboundGeneric()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Class C
    Public Class E
    End Class

    Public Class E(Of V)
    End Class

    Public Shared Sub Main()
        Dim t1 = GetType(D(Of ).E)
        Console.WriteLine(t1)

        Dim t2 = GetType(F(Of ).E)
        Dim t3 = GetType(G(Of ).E)
        Console.WriteLine(t2)
        Console.WriteLine(t3)
        Console.WriteLine(t2.Equals(t3))

        Dim t4 = GetType(D(Of ).E(Of ))
        Console.WriteLine(t4)

        Dim t5 = GetType(F(Of ).E(Of ))
        Dim t6 = GetType(G(Of ).E(Of ))
        Console.WriteLine(t5)
        Console.WriteLine(t6)
        Console.WriteLine(t5.Equals(t6))
    End Sub
End Class

Class C(Of U)
    Public Class E
    End Class

    Public Class E(Of V)
    End Class
End Class

Class D(Of T) 
    Inherits C
End Class

Class F(Of T) 
    Inherits C(Of T)
End Class

Class G(Of T) 
    Inherits C(Of Integer)
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=<![CDATA[
C+E
C`1+E[U]
C`1+E[U]
True
C+E`1[V]
C`1+E`1[U,V]
C`1+E`1[U,V]
True
]]>)
        End Sub

        <Fact>
        Public Sub CodeGen_GetType_Arrays()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class Class1(Of T)
End Class

Module Program
    Public Sub Print(Of U)()
        System.Console.WriteLine(GetType(Integer()))
        System.Console.WriteLine(GetType(Integer(,)))
        System.Console.WriteLine(GetType(Integer()()))
        System.Console.WriteLine(GetType(U()))
        System.Console.WriteLine(GetType(Class1(Of U)()))
        System.Console.WriteLine(GetType(Class1(Of Integer)()))
    End Sub

    Public Sub Main()
        Print(Of Long)()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
System.Int32[]
System.Int32[,]
System.Int32[][]
System.Int64[]
Class1`1[System.Int64][]
Class1`1[System.Int32][]
]]>).VerifyIL("Program.Print(Of U)", <![CDATA[
{
  // Code size       91 (0x5b)
  .maxstack  1
  IL_0000:  ldtoken    "Integer()"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  call       "Sub System.Console.WriteLine(Object)"
  IL_000f:  ldtoken    "Integer(,)"
  IL_0014:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0019:  call       "Sub System.Console.WriteLine(Object)"
  IL_001e:  ldtoken    "Integer()()"
  IL_0023:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0028:  call       "Sub System.Console.WriteLine(Object)"
  IL_002d:  ldtoken    "U()"
  IL_0032:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0037:  call       "Sub System.Console.WriteLine(Object)"
  IL_003c:  ldtoken    "Class1(Of U)()"
  IL_0041:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0046:  call       "Sub System.Console.WriteLine(Object)"
  IL_004b:  ldtoken    "Class1(Of Integer)()"
  IL_0050:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0055:  call       "Sub System.Console.WriteLine(Object)"
  IL_005a:  ret
}
]]>).Compilation
        End Sub

        <Fact>
        Public Sub CodeGen_GetType_Nested()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class Outer(Of T)
    Public Shared Sub Print()
        System.Console.WriteLine(GetType(Inner(Of )))
        System.Console.WriteLine(GetType(Inner(Of T)))
        System.Console.WriteLine(GetType(Inner(Of Integer)))

        System.Console.WriteLine(GetType(Outer(Of ).Inner(Of )))
        'System.Console.WriteLine(GetType(Outer(Of ).Inner(Of T))) ' BC32099: Comma or ')' expected.
        'System.Console.WriteLine(GetType(Outer(Of ).Inner(Of Integer))) ' BC32099: Comma or ')' expected.

        'System.Console.WriteLine(GetType(Outer(Of T).Inner(Of ))) ' BC30182: Type expected.
        System.Console.WriteLine(GetType(Outer(Of T).Inner(Of T)))
        System.Console.WriteLine(GetType(Outer(Of T).Inner(Of Integer)))

        'System.Console.WriteLine(GetType(Outer(Of Integer).Inner(Of ))) ' BC30182: Type expected.
        System.Console.WriteLine(GetType(Outer(Of Integer).Inner(Of T)))
        System.Console.WriteLine(GetType(Outer(Of Integer).Inner(Of Integer)))
    End Sub

    Class Inner(Of U)
    End Class
End Class

Class Class1(Of T)
End Class

Module Program
    Public Sub Main()
        Outer(Of Long).Print()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
Outer`1+Inner`1[T,U]
Outer`1+Inner`1[System.Int64,System.Int64]
Outer`1+Inner`1[System.Int64,System.Int32]
Outer`1+Inner`1[T,U]
Outer`1+Inner`1[System.Int64,System.Int64]
Outer`1+Inner`1[System.Int64,System.Int32]
Outer`1+Inner`1[System.Int32,System.Int64]
Outer`1+Inner`1[System.Int32,System.Int32]
]]>).VerifyIL("Outer(Of T).Print", <![CDATA[
{
  // Code size      121 (0x79)
  .maxstack  1
  IL_0000:  ldtoken    "Outer(Of T).Inner(Of U)"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  call       "Sub System.Console.WriteLine(Object)"
  IL_000f:  ldtoken    "Outer(Of T).Inner(Of T)"
  IL_0014:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0019:  call       "Sub System.Console.WriteLine(Object)"
  IL_001e:  ldtoken    "Outer(Of T).Inner(Of Integer)"
  IL_0023:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0028:  call       "Sub System.Console.WriteLine(Object)"
  IL_002d:  ldtoken    "Outer(Of T).Inner(Of U)"
  IL_0032:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0037:  call       "Sub System.Console.WriteLine(Object)"
  IL_003c:  ldtoken    "Outer(Of T).Inner(Of T)"
  IL_0041:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0046:  call       "Sub System.Console.WriteLine(Object)"
  IL_004b:  ldtoken    "Outer(Of T).Inner(Of Integer)"
  IL_0050:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0055:  call       "Sub System.Console.WriteLine(Object)"
  IL_005a:  ldtoken    "Outer(Of Integer).Inner(Of T)"
  IL_005f:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0064:  call       "Sub System.Console.WriteLine(Object)"
  IL_0069:  ldtoken    "Outer(Of Integer).Inner(Of Integer)"
  IL_006e:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0073:  call       "Sub System.Console.WriteLine(Object)"
  IL_0078:  ret
}
]]>).Compilation
        End Sub

        <Fact>
        Public Sub CodeGen_GetType_InLambda()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Class Outer(Of T)
    public class Inner(Of U)
        public Function Method(Of V)(p as V) as Action
            return Sub()
                Console.WriteLine(p)

                Console.WriteLine(GetType(T))
                Console.WriteLine(GetType(U))
                Console.WriteLine(GetType(V))

                Console.WriteLine(GetType(Outer(Of )))
                Console.WriteLine(GetType(Outer(Of T)))
                Console.WriteLine(GetType(Outer(Of U)))
                Console.WriteLine(GetType(Outer(Of V)))

                Console.WriteLine(GetType(Inner(Of )))
                Console.WriteLine(GetType(Inner(Of T)))
                Console.WriteLine(GetType(Inner(Of U)))
                Console.WriteLine(GetType(Inner(Of V)))
            End Sub
        End Function
    End Class
End Class

Module Program
    Public Sub Main()
        Dim a as Action = new Outer(Of Integer).Inner(Of Char)().Method(Of Byte)(1)
        a()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
System.Int32
System.Char
System.Byte
Outer`1[T]
Outer`1[System.Int32]
Outer`1[System.Char]
Outer`1[System.Byte]
Outer`1+Inner`1[T,U]
Outer`1+Inner`1[System.Int32,System.Int32]
Outer`1+Inner`1[System.Int32,System.Char]
Outer`1+Inner`1[System.Int32,System.Byte]
]]>)
        End Sub

        <Fact>
        Public Sub CodeGen_GetType_AliasForTypeMemberOfGeneric_1()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports MyTestClass = TestClass(Of String)

Public Class TestClass(Of T)
    Public Enum TestEnum
        First = 0   
    End Enum
End Class

Module Program
    Public Sub Main()
        Console.WriteLine(GetType(MyTestClass.TestEnum))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
TestClass`1+TestEnum[System.String]
]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldtoken    "TestClass(Of String).TestEnum"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  call       "Sub System.Console.WriteLine(Object)"
  IL_000f:  ret
}
]]>).Compilation
        End Sub

        <Fact>
        Public Sub CodeGen_GetType_AliasForTypeMemberOfGeneric_2()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Imports OuterOfString = Outer(Of String)
Imports OuterOfInt = Outer(Of Integer)

Public Class Outer(Of T)
    Public Class Inner(Of U)
    End Class
End Class

Module Program
    Public Sub Main()
        System.Console.WriteLine(GetType(OuterOfString.Inner(Of )).Equals(GetType(OuterOfInt.Inner(Of ))))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
True
]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldtoken    "Outer(Of T).Inner(Of U)"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  ldtoken    "Outer(Of T).Inner(Of U)"
  IL_000f:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0014:  callvirt   "Function System.Type.Equals(System.Type) As Boolean"
  IL_0019:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_001e:  ret
}
]]>).Compilation
        End Sub
    End Class
End Namespace
