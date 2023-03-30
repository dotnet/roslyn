' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Type = Microsoft.VisualStudio.Debugger.Metadata.Type

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class TypeNameFormatterTests : Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub Primitives()
            Assert.Equal("Object", GetType(Object).GetTypeName())
            Assert.Equal("Boolean", GetType(Boolean).GetTypeName())
            Assert.Equal("Char", GetType(Char).GetTypeName())
            Assert.Equal("SByte", GetType(SByte).GetTypeName())
            Assert.Equal("Byte", GetType(Byte).GetTypeName())
            Assert.Equal("Short", GetType(Short).GetTypeName())
            Assert.Equal("UShort", GetType(UShort).GetTypeName())
            Assert.Equal("Integer", GetType(Integer).GetTypeName())
            Assert.Equal("UInteger", GetType(UInteger).GetTypeName())
            Assert.Equal("Long", GetType(Long).GetTypeName())
            Assert.Equal("ULong", GetType(ULong).GetTypeName())
            Assert.Equal("Single", GetType(Single).GetTypeName())
            Assert.Equal("Double", GetType(Double).GetTypeName())
            Assert.Equal("Decimal", GetType(Decimal).GetTypeName())
            Assert.Equal("String", GetType(String).GetTypeName())
            Assert.Equal("Date", GetType(Date).GetTypeName())
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1016796")>
        Public Sub NestedTypes()
            Dim source = "
Public Class A
    Public Class B
    End Class
End Class

Namespace N
    Public Class A
        public class B
        End Class
    End Class
    Public Class G1(Of T)
        Public Class G2(Of T)
            Public Class G3(Of U)
            End Class
            Class G4(Of U, V)
            End Class
        End Class
    End Class
End Namespace
"
            Dim assembly = GetAssembly(source)

            Assert.Equal("A", assembly.GetType("A").GetTypeName())
            Assert.Equal("A.B", assembly.GetType("A+B").GetTypeName())
            Assert.Equal("N.A", assembly.GetType("N.A").GetTypeName())
            Assert.Equal("N.A.B", assembly.GetType("N.A+B").GetTypeName())
            Assert.Equal("N.G1(Of Integer).G2(Of Single).G3(Of Double)", assembly.GetType("N.G1`1+G2`1+G3`1").MakeGenericType(GetType(Integer), GetType(Single), GetType(Double)).GetTypeName())
            Assert.Equal("N.G1(Of Integer).G2(Of Single).G4(Of Double, UShort)", assembly.GetType("N.G1`1+G2`1+G4`2").MakeGenericType(GetType(Integer), GetType(Single), GetType(Double), GetType(UShort)).GetTypeName())
        End Sub

        <Fact>
        Public Sub GenericTypes()
            Dim source = "
Public Class A
    Public Class B
    End Class
End Class

Namespace N
    Public Class C(Of T, U)
        Public Class D(Of V, W)
        End Class
    End Class
End Namespace
"
            Dim assembly = GetAssembly(source)
            Dim typeA = assembly.GetType("A")
            Dim typeB = typeA.GetNestedType("B")
            Dim typeC = assembly.GetType("N.C`2")
            Dim typeD = typeC.GetNestedType("D`2")
            Dim typeInt = GetType(Integer)
            Dim typeString = GetType(String)
            Dim typeCIntString = typeC.MakeGenericType(typeInt, typeString)

            Assert.Equal("N.C(Of T, U)", typeC.GetTypeName())
            Assert.Equal("N.C(Of Integer, String)", typeCIntString.GetTypeName())
            Assert.Equal("N.C(Of A, A.B)", typeC.MakeGenericType(typeA, typeB).GetTypeName())
            Assert.Equal("N.C(Of Integer, String).D(Of A, A.B)", typeD.MakeGenericType(typeInt, typeString, typeA, typeB).GetTypeName())
            Assert.Equal("N.C(Of A, N.C(Of Integer, String)).D(Of N.C(Of Integer, String), A.B)", typeD.MakeGenericType(typeA, typeCIntString, typeCIntString, typeB).GetTypeName())
        End Sub

        <Fact>
        Public Sub NonGenericInGeneric()
            Dim source = "
Public Class A(Of T)
    Public Class B
    End Class
End Class
"
            Dim assembly = GetAssembly(source)
            Dim typeA = assembly.GetType("A`1")
            Dim typeB = typeA.GetNestedType("B")

            Assert.Equal("A(Of Integer).B", typeB.MakeGenericType(GetType(Integer)).GetTypeName())
        End Sub

        <Fact>
        Public Sub PrimitiveNullableTypes()
            Assert.Equal("Integer?", GetType(Integer?).GetTypeName())
            Assert.Equal("Boolean?", GetType(Boolean?).GetTypeName())
        End Sub

        <Fact>
        Public Sub NullableTypes()
            Dim source = "
Namespace N
    Public Structure A(Of T)
        Public Structure B(Of U)
        End Structure
    End Structure

    Public Structure C
    End Structure
End Namespace
"
            Dim typeNullable = GetType(System.Nullable(Of ))

            Dim assembly = GetAssembly(source)
            Dim typeA = assembly.GetType("N.A`1")
            Dim typeB = typeA.GetNestedType("B`1")
            Dim typeC = assembly.GetType("N.C")

            Assert.Equal("N.C?", typeNullable.MakeGenericType(typeC).GetTypeName())
            Assert.Equal("N.A(Of N.C)?", typeNullable.MakeGenericType(typeA.MakeGenericType(typeC)).GetTypeName())
            Assert.Equal("N.A(Of N.C).B(Of N.C)?", typeNullable.MakeGenericType(typeB.MakeGenericType(typeC, typeC)).GetTypeName())
        End Sub

        <Fact>
        Public Sub PrimitiveArrayTypes()
            Assert.Equal("Integer()", GetType(Integer()).GetTypeName())
            Assert.Equal("Integer(,)", GetType(Integer(,)).GetTypeName())
            Assert.Equal("Integer()(,)", GetType(Integer()(,)).GetTypeName())
            Assert.Equal("Integer(,)()", GetType(Integer(,)()).GetTypeName())
        End Sub

        <Fact>
        Public Sub ArrayTypes()
            Dim source = "
Namespace N
    Public Class A(Of T)
        Public Class B(Of U)
        End Class
    End Class

    Public Class C
    End Class
End Namespace
"
            Dim assembly = GetAssembly(source)
            Dim typeA = assembly.GetType("N.A`1")
            Dim typeB = typeA.GetNestedType("B`1")
            Dim typeC = assembly.GetType("N.C")

            Assert.NotEqual(typeC.MakeArrayType(), typeC.MakeArrayType(1))

            Assert.Equal("N.C()", typeC.MakeArrayType().GetTypeName())
            Assert.Equal("N.C()", typeC.MakeArrayType(1).GetTypeName()) ' NOTE: Multi-dimensional array that happens to exactly one dimension.
            Assert.Equal("N.A(Of N.C)(,)", typeA.MakeGenericType(typeC).MakeArrayType(2).GetTypeName())
            Assert.Equal("N.A(Of N.C()).B(Of N.C)(,,)", typeB.MakeGenericType(typeC.MakeArrayType(), typeC).MakeArrayType(3).GetTypeName())
        End Sub

        <Fact>
        Public Sub CustomBoundsArrayTypes()
            Dim instance = Array.CreateInstance(GetType(Integer), {1, 2, 3}, {4, 5, 6})

            Assert.Equal("Integer(,,)", instance.GetType().GetTypeName())
            Assert.Equal("Integer()(,,)", instance.GetType().MakeArrayType().GetTypeName())
        End Sub

        <Fact>
        Public Sub PrimitivePointerTypes()
            Assert.Equal("Integer*", GetType(Integer).MakePointerType().GetTypeName())
            Assert.Equal("Integer**", GetType(Integer).MakePointerType().MakePointerType().GetTypeName())
            Assert.Equal("Integer*()", GetType(Integer).MakePointerType().MakeArrayType().GetTypeName())
        End Sub

        <Fact>
        Public Sub PointerTypes()
            Dim source = "
Namespace N
    Public Structure A(Of T)
        Public Structure B(Of U)
        End Structure
    End Structure

    Public Structure C
    End Structure
End Namespace
"
            Dim assembly = GetAssembly(source)
            Dim typeA = assembly.GetType("N.A`1")
            Dim typeB = typeA.GetNestedType("B`1")
            Dim typeC = assembly.GetType("N.C")

            Assert.Equal("N.C*", typeC.MakePointerType().GetTypeName())
            Assert.Equal("N.A(Of N.C)*", typeA.MakeGenericType(typeC).MakePointerType().GetTypeName())
            Assert.Equal("N.A(Of N.C).B(Of N.C)*", typeB.MakeGenericType(typeC, typeC).MakePointerType().GetTypeName())
        End Sub

        <Fact>
        Public Sub Void()
            Assert.Equal("System.Void", GetType(Void).GetTypeName())
            Assert.Equal("System.Void*", GetType(Void).MakePointerType().GetTypeName())
        End Sub

        <Fact>
        Public Sub KeywordIdentifiers()
            Dim source = "
Public Class [Object]
    Public Class [True]
    End Class
End Class

Namespace [Return]
    Public Class [From](Of [Async])
        Public Class [Await]
        End Class
    End Class

    Namespace [False]
        Public Class [Nothing]
        End Class
    End Namespace
End Namespace
"

            Dim assembly = GetAssembly(source)
            Dim objectType = assembly.GetType("Object")
            Dim trueType = objectType.GetNestedType("True")
            Dim nothingType = assembly.GetType("Return.False.Nothing")
            Dim fromType = assembly.GetType("Return.From`1")
            Dim constructedYieldType = fromType.MakeGenericType(nothingType)
            Dim awaitType = fromType.GetNestedType("Await")
            Dim constructedAwaitType = awaitType.MakeGenericType(nothingType)

            Assert.Equal("Object", objectType.GetTypeName(escapeKeywordIdentifiers:=False))
            Assert.Equal("Object.True", trueType.GetTypeName(escapeKeywordIdentifiers:=False))
            Assert.Equal("Return.False.Nothing", nothingType.GetTypeName(escapeKeywordIdentifiers:=False))
            Assert.Equal("Return.From(Of Async)", fromType.GetTypeName(escapeKeywordIdentifiers:=False))
            Assert.Equal("Return.From(Of Return.False.Nothing)", constructedYieldType.GetTypeName(escapeKeywordIdentifiers:=False))
            Assert.Equal("Return.From(Of Return.False.Nothing).Await", constructedAwaitType.GetTypeName(escapeKeywordIdentifiers:=False))

            Assert.Equal("[Object]", objectType.GetTypeName(escapeKeywordIdentifiers:=True))
            Assert.Equal("[Object].[True]", trueType.GetTypeName(escapeKeywordIdentifiers:=True))
            Assert.Equal("[Return].[False].[Nothing]", nothingType.GetTypeName(escapeKeywordIdentifiers:=True))
            Assert.Equal("[Return].[From](Of [Async])", fromType.GetTypeName(escapeKeywordIdentifiers:=True))
            Assert.Equal("[Return].[From](Of [Return].[False].[Nothing])", constructedYieldType.GetTypeName(escapeKeywordIdentifiers:=True))
            Assert.Equal("[Return].[From](Of [Return].[False].[Nothing]).[Await]", constructedAwaitType.GetTypeName(escapeKeywordIdentifiers:=True))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087216")>
        Public Sub DynamicAttribute_ValidFlags()
            Assert.Equal("Object", GetType(Object).GetTypeName(MakeCustomTypeInfo(True)))
            Assert.Equal("Object()", GetType(Object()).GetTypeName(MakeCustomTypeInfo(False, True)))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087216")>
        Public Sub DynamicAttribute_OtherGuid()
            Dim typeInfo = DkmClrCustomTypeInfo.Create(Guid.NewGuid(), New ReadOnlyCollection(Of Byte)({1}))
            Assert.Equal("Object", GetType(Object).GetTypeName(typeInfo))
            Assert.Equal("Object()", GetType(Object()).GetTypeName(typeInfo))
        End Sub

    End Class

End Namespace
