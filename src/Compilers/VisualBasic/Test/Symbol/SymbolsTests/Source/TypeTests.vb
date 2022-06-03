' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class TypeTests
        Inherits BasicTestBase

        <Fact>
        Public Sub AlphaRenaming()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Public Class A(Of T)
    Public Class B(Of U)
        Public X As A(Of A(Of U))
    End Class
End Class

Public Class A1
    Inherits A(Of Integer)
End Class

Public Class A2
    Inherits A(Of Integer)
End Class
    </file>
</compilation>)
            Dim aint1 = compilation.GlobalNamespace.GetTypeMembers("A1")(0).BaseType  ' A<int>
            Dim aint2 = compilation.GlobalNamespace.GetTypeMembers("A2")(0).BaseType  ' A<int>
            Dim b1 = aint1.GetTypeMembers("B", 1).Single()                            ' A<int>.B<U>
            Dim b2 = aint2.GetTypeMembers("B", 1).Single()                            ' A<int>.B<U>
            Assert.NotSame(b1.TypeParameters(0), b2.TypeParameters(0))                ' they've been alpha renamed independently
            Assert.Equal(b1.TypeParameters(0), b2.TypeParameters(0))                  ' but happen to be the same type
            Dim xtype1 = DirectCast(b1.GetMembers("X")(0), FieldSymbol).Type          ' Types using them are the same too
            Dim xtype2 = DirectCast(b2.GetMembers("X")(0), FieldSymbol).Type
            Assert.Equal(xtype1, xtype2)
        End Sub

        <Fact>
        Public Sub SourceTypeSymbols1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="C">
    <file name="a.vb">
Friend Interface A
End Interface
Namespace n
    Partial Public MustInherit Class B
    End Class
    Structure I
    End Structure
End Namespace
    </file>
    <file name="b.vb">
Namespace N
    Partial Public Class b
    End Class
    Friend Enum E
       A
    End Enum
    Friend Delegate Function B(Of T)() As String
    Module M
    End Module
End Namespace
    
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace

            Assert.Equal("", globalNS.Name)
            Assert.Equal(SymbolKind.Namespace, globalNS.Kind)
            Assert.Equal(2, globalNS.GetMembers().Length())

            Dim membersNamedA = globalNS.GetMembers("a")
            Assert.Equal(1, membersNamedA.Length)
            Dim membersNamedN = globalNS.GetMembers("n")
            Assert.Equal(1, membersNamedN.Length)

            Dim ifaceA = DirectCast(membersNamedA(0), NamedTypeSymbol)
            Assert.Equal(globalNS, ifaceA.ContainingSymbol)
            Assert.Equal("A", ifaceA.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, ifaceA.Kind)
            Assert.Equal(TypeKind.Interface, ifaceA.TypeKind)
            Assert.Equal(Accessibility.Friend, ifaceA.DeclaredAccessibility)
            Assert.False(ifaceA.IsNotInheritable)
            Assert.True(ifaceA.IsMustInherit)
            Assert.True(ifaceA.IsReferenceType)
            Assert.False(ifaceA.IsValueType)
            Assert.Equal(0, ifaceA.Arity)
            Assert.Null(ifaceA.BaseType)

            Dim nsN = DirectCast(membersNamedN(0), NamespaceSymbol)
            Dim membersOfN = nsN.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ThenBy(Function(s) DirectCast(s, NamedTypeSymbol).Arity).ToArray()
            Assert.Equal(5, membersOfN.Length)

            Dim classB = DirectCast(membersOfN(0), NamedTypeSymbol)
            Assert.Equal(nsN.GetTypeMembers("B", 0).First(), classB)
            Assert.Equal(nsN, classB.ContainingSymbol)
            Assert.Equal("B", classB.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, classB.Kind)
            Assert.Equal(TypeKind.Class, classB.TypeKind)
            Assert.Equal(Accessibility.Public, classB.DeclaredAccessibility)
            Assert.False(classB.IsNotInheritable)
            Assert.True(classB.IsMustInherit)
            Assert.True(classB.IsReferenceType)
            Assert.False(classB.IsValueType)
            Assert.Equal(0, classB.Arity)
            Assert.Equal(0, classB.TypeParameters.Length)
            Assert.Equal(2, classB.Locations.Length())
            Assert.Equal(1, classB.InstanceConstructors.Length())
            Assert.Equal("System.Object", classB.BaseType.ToTestDisplayString())

            Dim delegateB = DirectCast(membersOfN(1), NamedTypeSymbol)
            Assert.Equal(nsN.GetTypeMembers("B", 1).First(), delegateB)
            Assert.Equal(nsN, delegateB.ContainingSymbol)
            Assert.Equal("B", delegateB.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, delegateB.Kind)
            Assert.Equal(TypeKind.Delegate, delegateB.TypeKind)
            Assert.Equal(Accessibility.Friend, delegateB.DeclaredAccessibility)
            Assert.True(delegateB.IsNotInheritable)
            Assert.False(delegateB.IsMustInherit)
            Assert.True(delegateB.IsReferenceType)
            Assert.False(delegateB.IsValueType)
            Assert.Equal(1, delegateB.Arity)
            Assert.Equal(1, delegateB.TypeParameters.Length)
            Assert.Equal(0, delegateB.TypeParameters(0).Ordinal)
            Assert.Same(delegateB, delegateB.TypeParameters(0).ContainingSymbol)
            Assert.Equal(1, delegateB.Locations.Length())
            Assert.Equal("System.MulticastDelegate", delegateB.BaseType.ToTestDisplayString())

#If Not DISABLE_GOOD_HASH_TESTS Then
            Assert.NotEqual(IdentifierComparison.GetHashCode("A"), IdentifierComparison.GetHashCode("B"))
#End If
            Dim enumE = DirectCast(membersOfN(2), NamedTypeSymbol)
            Assert.Equal(nsN.GetTypeMembers("E", 0).First(), enumE)
            Assert.Equal(nsN, enumE.ContainingSymbol)
            Assert.Equal("E", enumE.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, enumE.Kind)
            Assert.Equal(TypeKind.Enum, enumE.TypeKind)
            Assert.Equal(Accessibility.Friend, enumE.DeclaredAccessibility)
            Assert.True(enumE.IsNotInheritable)
            Assert.False(enumE.IsMustInherit)
            Assert.False(enumE.IsReferenceType)
            Assert.True(enumE.IsValueType)
            Assert.Equal(0, enumE.Arity)
            Assert.Equal(1, enumE.Locations.Length())
            Assert.Equal("System.Enum", enumE.BaseType.ToTestDisplayString())

            Dim structI = DirectCast(membersOfN(3), NamedTypeSymbol)
            Assert.Equal(nsN.GetTypeMembers("i", 0).First(), structI)
            Assert.Equal(nsN, structI.ContainingSymbol)
            Assert.Equal("I", structI.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, structI.Kind)
            Assert.Equal(TypeKind.Structure, structI.TypeKind)
            Assert.Equal(Accessibility.Friend, structI.DeclaredAccessibility)
            Assert.True(structI.IsNotInheritable)
            Assert.False(structI.IsMustInherit)
            Assert.False(structI.IsReferenceType)
            Assert.True(structI.IsValueType)
            Assert.Equal(0, structI.Arity)
            Assert.Equal(1, structI.Locations.Length())
            Assert.Equal("System.ValueType", structI.BaseType.ToTestDisplayString())

            Dim moduleM = DirectCast(membersOfN(4), NamedTypeSymbol)
            Assert.Equal(nsN.GetTypeMembers("m", 0).First(), moduleM)
            Assert.Equal(nsN, moduleM.ContainingSymbol)
            Assert.Equal("M", moduleM.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, moduleM.Kind)
            Assert.Equal(TypeKind.Module, moduleM.TypeKind)
            Assert.Equal(Accessibility.Friend, moduleM.DeclaredAccessibility)
            Assert.True(moduleM.IsNotInheritable)
            Assert.False(moduleM.IsMustInherit)
            Assert.True(moduleM.IsReferenceType)
            Assert.False(moduleM.IsValueType)
            Assert.Equal(0, moduleM.Arity)
            Assert.Equal(1, moduleM.Locations.Length())
            Assert.Equal("System.Object", moduleM.BaseType.ToTestDisplayString())

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<expected>
BC40055: Casing of namespace name 'n' does not match casing of namespace name 'N' in 'b.vb'.
Namespace n
          ~
</expected>)
        End Sub

        <Fact>
        Public Sub NestedSourceTypeSymbols()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Public Partial Class Outer(Of K)
    Private Partial Class I1
        Protected Partial Structure I2(Of T, U)
        End Structure
    End Class 

    Private Partial Class I1
        Protected Partial Structure I2(Of W)
        End Structure
    End Class 

    Enum I4
       X
    End Enum       
End Class

Public Partial Class Outer(Of K)
    Protected Friend Interface I3
    End Interface
End Class
    </file>
    <file name="b.vb">
Public Class outer(Of K)
    Private Partial Class i1
        Protected Partial Structure i2(Of T, U)
        End Structure
    End Class 
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim globalNSmembers = globalNS.GetMembers()
            Assert.Equal(1, globalNSmembers.Length)
            Dim outerClass = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Assert.Equal(globalNS, outerClass.ContainingSymbol)
            Assert.Equal("Outer", outerClass.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, outerClass.Kind)
            Assert.Equal(TypeKind.Class, outerClass.TypeKind)
            Assert.Equal(Accessibility.Public, outerClass.DeclaredAccessibility)
            Assert.Equal(1, outerClass.Arity)
            Assert.Equal(1, outerClass.TypeParameters.Length)
            Dim outerTypeParam = outerClass.TypeParameters(0)
            Assert.Equal(0, outerTypeParam.Ordinal)
            Assert.Same(outerClass, outerTypeParam.ContainingSymbol)

            Dim membersOfOuter = outerClass.GetTypeMembers().AsEnumerable().OrderBy(Function(s) s.Name).ThenBy(Function(s) DirectCast(s, NamedTypeSymbol).Arity).ToArray()
            Assert.Equal(3, membersOfOuter.Length)

            Dim i1Class = membersOfOuter(0)
            Assert.Equal(1, outerClass.GetTypeMembers("i1").Length())
            Assert.Same(i1Class, outerClass.GetTypeMembers("i1").First())
            Assert.Equal("I1", i1Class.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, i1Class.Kind)
            Assert.Equal(TypeKind.Class, i1Class.TypeKind)
            Assert.Equal(Accessibility.Private, i1Class.DeclaredAccessibility)
            Assert.Equal(0, i1Class.Arity)
            Assert.Equal(3, i1Class.Locations.Length())

            Dim i3Interface = membersOfOuter(1)
            Assert.Equal(1, outerClass.GetTypeMembers("i3").Length())
            Assert.Same(i3Interface, outerClass.GetTypeMembers("i3").First())
            Assert.Equal("I3", i3Interface.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, i3Interface.Kind)
            Assert.Equal(TypeKind.Interface, i3Interface.TypeKind)
            Assert.Equal(Accessibility.ProtectedOrFriend, i3Interface.DeclaredAccessibility)
            Assert.Equal(0, i3Interface.Arity)
            Assert.Equal(1, i3Interface.Locations.Length())

            Dim i4Enum = membersOfOuter(2)
            Assert.Equal(1, outerClass.GetTypeMembers("i4").Length())
            Assert.Same(i4Enum, outerClass.GetTypeMembers("i4").First())
            Assert.Equal("I4", i4Enum.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, i4Enum.Kind)
            Assert.Equal(TypeKind.Enum, i4Enum.TypeKind)
            Assert.Equal(Accessibility.Public, i4Enum.DeclaredAccessibility)
            Assert.Equal(0, i4Enum.Arity)
            Assert.Equal(1, i4Enum.Locations.Length())

            Dim membersOfI1 = i1Class.GetTypeMembers().AsEnumerable().OrderBy(Function(s) s.Name).ThenBy(Function(s) DirectCast(s, NamedTypeSymbol).Arity).ToArray()
            Assert.Equal(2, membersOfI1.Length)
            Assert.Equal(2, i1Class.GetTypeMembers("I2").Length())

            Dim i2Arity1 = membersOfI1(0)
            Assert.Equal(1, i1Class.GetTypeMembers("i2", 1).Length())
            Assert.Same(i2Arity1, i1Class.GetTypeMembers("i2", 1).First())
            Assert.Equal("I2", i2Arity1.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, i2Arity1.Kind)
            Assert.Equal(TypeKind.Structure, i2Arity1.TypeKind)
            Assert.Equal(Accessibility.Protected, i2Arity1.DeclaredAccessibility)
            Assert.Equal(1, i2Arity1.Arity)
            Assert.Equal(1, i2Arity1.TypeParameters.Length)
            Dim i2Arity1TypeParam = i2Arity1.TypeParameters(0)
            Assert.Equal(0, i2Arity1TypeParam.Ordinal)
            Assert.Same(i2Arity1, i2Arity1TypeParam.ContainingSymbol)
            Assert.Equal(1, i2Arity1.Locations.Length())

            Dim i2Arity2 = membersOfI1(1)
            Assert.Equal(1, i1Class.GetTypeMembers("i2", 2).Length())
            Assert.Same(i2Arity2, i1Class.GetTypeMembers("i2", 2).First())
            Assert.Equal("I2", i2Arity2.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, i2Arity2.Kind)
            Assert.Equal(TypeKind.Structure, i2Arity2.TypeKind)
            Assert.Equal(Accessibility.Protected, i2Arity2.DeclaredAccessibility)
            Assert.Equal(2, i2Arity2.Arity)
            Assert.Equal(2, i2Arity2.TypeParameters.Length)
            Dim i2Arity2TypeParam0 = i2Arity2.TypeParameters(0)
            Assert.Equal(0, i2Arity2TypeParam0.Ordinal)
            Assert.Same(i2Arity2, i2Arity2TypeParam0.ContainingSymbol)
            Dim i2Arity2TypeParam1 = i2Arity2.TypeParameters(1)
            Assert.Equal(1, i2Arity2TypeParam1.Ordinal)
            Assert.Same(i2Arity2, i2Arity2TypeParam1.ContainingSymbol)
            Assert.Equal(2, i2Arity2.Locations.Length())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <WorkItem(2200, "DevDiv_Projects/Roslyn")>
        <Fact>
        Public Sub ArrayTypes()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
  <compilation name="ArrayTypes">
      <file name="a.vb">
    Public Class A
        Public Shared AryField()(,) as Object
        Friend fAry01(9) as String
        Dim fAry02(9) as String

        Function M(ByVal ary1() As Byte, ByRef ary2()() as Single, ParamArray ary3(,) As Long) As String()
          Return Nothing
        End Function      
    End Class
    </file>
  </compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace

            Dim classTest = DirectCast(globalNS.GetTypeMembers("A").Single(), NamedTypeSymbol)
            Dim members = classTest.GetMembers()
            Dim field1 = DirectCast(members(1), FieldSymbol)
            Assert.Equal(SymbolKind.ArrayType, field1.Type.Kind)
            Dim sym1 = field1.Type
            ' Object
            Assert.Equal(SymbolKind.ArrayType, sym1.Kind)
            Assert.Equal(Accessibility.NotApplicable, sym1.DeclaredAccessibility)
            Assert.False(sym1.IsShared)
            Assert.Null(sym1.ContainingAssembly)
            ' bug 2200
            field1 = DirectCast(members(2), FieldSymbol)
            Assert.Equal(SymbolKind.ArrayType, field1.Type.Kind)
            field1 = DirectCast(members(3), FieldSymbol)
            Assert.Equal(SymbolKind.ArrayType, field1.Type.Kind)

            Dim mem1 = DirectCast(members(4), MethodSymbol)
            Assert.Equal(3, mem1.Parameters.Length())

            Dim sym2 = mem1.Parameters(0)
            Assert.Equal("ary1", sym2.Name)
            Assert.Equal(SymbolKind.ArrayType, sym2.Type.Kind)
            Assert.Equal("Array", sym2.Type.BaseType.Name)

            Dim sym3 = mem1.Parameters(1)
            Assert.Equal("ary2", sym3.Name)
            Assert.True(sym3.IsByRef)
            Assert.Equal(SymbolKind.ArrayType, sym3.Type.Kind)

            Dim sym4 = mem1.Parameters(2)
            Assert.Equal("ary3", sym4.Name)
            Assert.Equal(SymbolKind.ArrayType, sym4.Type.Kind)
            Assert.Equal(0, sym4.Type.GetTypeMembers().Length())
            Assert.Equal(0, sym4.Type.GetTypeMembers(String.Empty).Length())
            Assert.Equal(0, sym4.Type.GetTypeMembers(String.Empty, 0).Length())

            Dim sym5 = mem1.ReturnType
            Assert.Equal(SymbolKind.ArrayType, sym5.Kind)
            Assert.Equal(0, sym5.GetAttributes().Length())

        End Sub

        <WorkItem(537281, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537281")>
        <WorkItem(537300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537300")>
        <WorkItem(932303, "DevDiv/Personal")>
        <Fact>
        Public Sub ArrayTypeInterfaces()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="ArrayTypes">
    <file name="a.vb">
    Public Class A
        Public AryField() As Object
        Shared AryField2()() As Integer
        Private AryField3(,,) As Byte

        Public Sub New()
        End Sub
    End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim classTest = DirectCast(globalNS.GetTypeMembers("A").Single(), NamedTypeSymbol)

            Dim sym1 = DirectCast(classTest.GetMembers().First(), FieldSymbol).Type
            Assert.Equal(SymbolKind.ArrayType, sym1.Kind)
            ' IList(Of T)
            Assert.Equal(1, sym1.Interfaces.Length)
            Dim itype1 = sym1.Interfaces(0)
            Assert.Equal("System.Collections.Generic.IList(Of System.Object)", itype1.ToTestDisplayString())

            ' Jagged array's rank is 1
            Dim sym2 = DirectCast(classTest.GetMembers("AryField2").First(), FieldSymbol).Type
            Assert.Equal(SymbolKind.ArrayType, sym2.Kind)
            Assert.Equal(1, sym2.Interfaces.Length)
            Dim itype2 = sym2.Interfaces(0)
            Assert.Equal("System.Collections.Generic.IList(Of System.Int32())", itype2.ToTestDisplayString())

            Dim sym3 = DirectCast(classTest.GetMembers("AryField3").First(), FieldSymbol).Type
            Assert.Equal(SymbolKind.ArrayType, sym3.Kind)
            Assert.Equal(0, sym3.Interfaces.Length)

            Assert.Throws(Of ArgumentNullException)(Sub() compilation.CreateArrayTypeSymbol(Nothing))
        End Sub

        <WorkItem(537420, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537420")>
        <WorkItem(537515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537515")>
        <Fact>
        Public Sub ArrayTypeGetFullNameAndHashCode()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="ArrayTypes">
    <file name="a.vb">
    Public Class A
        Public Shared AryField1() As Integer
        Private AryField2(,,) As String
        Protected AryField3()(,) As Byte
        Shared AryField4 As ULong(,)()
        Friend AryField5(9) As Long
        Dim AryField6(2,4) As Object
        Public Abc(), Bbc(,,,), Cbc()()

        Public Sub New()
        End Sub
    End Class
    </file>
</compilation>)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim classTest = DirectCast(globalNS.GetTypeMembers("A").Single(), NamedTypeSymbol)
            Dim mems = classTest.GetMembers()
            Dim sym1 = DirectCast(classTest.GetMembers().First(), FieldSymbol).Type
            Assert.Equal(SymbolKind.ArrayType, sym1.Kind)
            Assert.Equal("System.Int32()", sym1.ToTestDisplayString())
            Dim v1 = sym1.GetHashCode()
            Dim v2 = sym1.GetHashCode()
            Assert.Equal(v1, v2)

            Dim sym21 = DirectCast(classTest.GetMembers("AryField2").First(), FieldSymbol)
            Assert.Equal(1, sym21.Locations.Length)
            Dim span = DirectCast(sym21.Locations(0), Location).GetLineSpan()
            Assert.Equal(span.StartLinePosition.Line, span.EndLinePosition.Line)
            Assert.Equal(16, span.StartLinePosition.Character)
            Assert.Equal(25, span.EndLinePosition.Character)
            Assert.Equal("AryField2", sym21.Name)
            Assert.Equal("A.AryField2 As System.String(,,)", sym21.ToTestDisplayString())

            Dim sym22 = sym21.Type
            Assert.Equal(SymbolKind.ArrayType, sym22.Kind)
            Assert.Equal("System.String(,,)", sym22.ToTestDisplayString())
            v1 = sym22.GetHashCode()
            v2 = sym22.GetHashCode()
            Assert.Equal(v1, v2)

            Dim sym3 = DirectCast(classTest.GetMembers("AryField3").First(), FieldSymbol).Type
            Assert.Equal(SymbolKind.ArrayType, sym3.Kind)
            Assert.Equal("System.Byte()(,)", sym3.ToTestDisplayString())
            v1 = sym3.GetHashCode()
            v2 = sym3.GetHashCode()
            Assert.Equal(v1, v2)

            Dim sym4 = DirectCast(mems(3), FieldSymbol).Type
            Assert.Equal(SymbolKind.ArrayType, sym4.Kind)
            Assert.Equal("System.UInt64(,)()", sym4.ToTestDisplayString())
            v1 = sym4.GetHashCode()
            v2 = sym4.GetHashCode()
            Assert.Equal(v1, v2)

            Dim sym5 = DirectCast(mems(4), FieldSymbol).Type
            Assert.Equal(SymbolKind.ArrayType, sym5.Kind)
            Assert.Equal("System.Int64()", sym5.ToTestDisplayString())
            v1 = sym5.GetHashCode()
            v2 = sym5.GetHashCode()
            Assert.Equal(v1, v2)

            Dim sym61 = DirectCast(mems(5), FieldSymbol)
            span = DirectCast(sym61.Locations(0), Location).GetLineSpan()
            Assert.Equal(span.StartLinePosition.Line, span.EndLinePosition.Line)
            Assert.Equal(12, span.StartLinePosition.Character)
            Assert.Equal(21, span.EndLinePosition.Character)

            Dim sym62 = sym61.Type
            Assert.Equal(SymbolKind.ArrayType, sym62.Kind)
            Assert.Equal("System.Object(,)", sym62.ToTestDisplayString())
            v1 = sym62.GetHashCode()
            v2 = sym62.GetHashCode()
            Assert.Equal(v1, v2)

            Dim sym71 = DirectCast(classTest.GetMembers("Abc").First(), FieldSymbol)
            Assert.Equal("system.object()", sym71.Type.ToTestDisplayString().ToLower())
            span = DirectCast(sym71.Locations(0), Location).GetLineSpan()
            Assert.Equal(span.StartLinePosition.Line, span.EndLinePosition.Line)
            Assert.Equal(15, span.StartLinePosition.Character)
            Assert.Equal(18, span.EndLinePosition.Character)

            Dim sym72 = DirectCast(classTest.GetMembers("bBc").First(), FieldSymbol)
            Assert.Equal("system.object(,,,)", sym72.Type.ToTestDisplayString().ToLower())
            span = DirectCast(sym72.Locations(0), Location).GetLineSpan()
            Assert.Equal(span.StartLinePosition.Line, span.EndLinePosition.Line)
            Assert.Equal(22, span.StartLinePosition.Character)
            Assert.Equal(25, span.EndLinePosition.Character)

            Dim sym73 = DirectCast(classTest.GetMembers("cbC").First(), FieldSymbol)
            Assert.Equal("system.object()()", sym73.Type.ToTestDisplayString().ToLower())
            span = DirectCast(sym73.Locations(0), Location).GetLineSpan()
            Assert.Equal(span.StartLinePosition.Line, span.EndLinePosition.Line)
            Assert.Equal(32, span.StartLinePosition.Character)
            Assert.Equal(35, span.EndLinePosition.Character)
            Assert.Equal("A.Cbc As System.Object()()", sym73.ToTestDisplayString())
        End Sub

        <Fact(), WorkItem(537187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537187"), WorkItem(529941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529941")>
        Public Sub EnumFields()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="EnumFields">
    <file name="a.vb">
    Public Enum E
       One
       Two = 2
       Three
    End Enum   
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace

            Assert.Equal("", globalNS.Name)
            Assert.Equal(SymbolKind.Namespace, globalNS.Kind)
            Assert.Equal(1, globalNS.GetMembers().Length())

            Dim enumE = globalNS.GetTypeMembers("E", 0).First()
            Assert.Equal("E", enumE.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.NamedType, enumE.Kind)
            Assert.Equal(TypeKind.Enum, enumE.TypeKind)
            Assert.Equal(Accessibility.Public, enumE.DeclaredAccessibility)
            Assert.True(enumE.IsNotInheritable)
            Assert.False(enumE.IsMustInherit)
            Assert.False(enumE.IsReferenceType)
            Assert.True(enumE.IsValueType)
            Assert.Equal(0, enumE.Arity)
            Assert.Equal("System.Enum", enumE.BaseType.ToTestDisplayString())

            Dim enumMembers = enumE.GetMembers()
            Assert.Equal(5, enumMembers.Length)
            Dim ctor = enumMembers.Where(Function(s) s.Kind = SymbolKind.Method)
            Assert.Equal(1, ctor.Count)
            Assert.Equal(SymbolKind.Method, ctor(0).Kind)

            Dim _val = enumMembers.Where(Function(s) Not s.IsShared AndAlso s.Kind = SymbolKind.Field)
            Assert.Equal(1, _val.Count)

            Dim emem = enumMembers.Where(Function(s) s.IsShared)
            Assert.Equal(3, emem.Count)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub SimpleGenericType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Generic">
    <file name="g.vb">
Namespace NS
    Public Interface IGoo(Of T)
    End Interface

    Friend Class A(Of V)
    End Class

    Public Structure S(Of X, Y)
    End Structure
End Namespace
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim namespaceNS = globalNS.GetMembers("NS")

            Dim nsNS = DirectCast(namespaceNS(0), NamespaceSymbol)
            Dim membersOfNS = nsNS.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ThenBy(Function(s) DirectCast(s, NamedTypeSymbol).Arity).ToArray()
            Assert.Equal(3, membersOfNS.Length)

            Dim classA = DirectCast(membersOfNS(0), NamedTypeSymbol)
            Assert.Equal(nsNS.GetTypeMembers("A").First(), classA)
            Assert.Equal(nsNS, classA.ContainingSymbol)
            Assert.Equal(SymbolKind.NamedType, classA.Kind)
            Assert.Equal(TypeKind.Class, classA.TypeKind)
            Assert.Equal(Accessibility.Friend, classA.DeclaredAccessibility)
            Assert.Equal(1, classA.TypeParameters.Length)
            Assert.Equal("V", classA.TypeParameters(0).Name)
            Assert.Equal(1, classA.TypeArguments.Length)

            Dim igoo = DirectCast(membersOfNS(1), NamedTypeSymbol)
            Assert.Equal(nsNS.GetTypeMembers("IGoo").First(), igoo)
            Assert.Equal(nsNS, igoo.ContainingSymbol)
            Assert.Equal(SymbolKind.NamedType, igoo.Kind)
            Assert.Equal(TypeKind.Interface, igoo.TypeKind)
            Assert.Equal(Accessibility.Public, igoo.DeclaredAccessibility)
            Assert.Equal(1, igoo.TypeParameters.Length)
            Assert.Equal("T", igoo.TypeParameters(0).Name)
            Assert.Equal(1, igoo.TypeArguments.Length)

            Dim structS = DirectCast(membersOfNS(2), NamedTypeSymbol)
            Assert.Equal(nsNS.GetTypeMembers("S").First(), structS)
            Assert.Equal(nsNS, structS.ContainingSymbol)
            Assert.Equal(SymbolKind.NamedType, structS.Kind)
            Assert.Equal(TypeKind.Structure, structS.TypeKind)
            Assert.Equal(Accessibility.Public, structS.DeclaredAccessibility)
            Assert.Equal(2, structS.TypeParameters.Length)
            Assert.Equal("X", structS.TypeParameters(0).Name)
            Assert.Equal("Y", structS.TypeParameters(1).Name)
            Assert.Equal(2, structS.TypeArguments.Length)

        End Sub

        ' Check that type parameters work correctly.
        <Fact>
        Public Sub TypeParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="C">
        <file name="a.vb">
            Interface Z(Of T, In U, Out V)
            End Interface
        </file>
    </compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers()
            Assert.Equal(1, globalNSmembers.Length)
            Dim interfaceZ = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Dim typeParams = interfaceZ.TypeParameters
            Assert.Equal(3, typeParams.Length)

            Assert.Equal("T", typeParams(0).Name)
            Assert.Equal(VarianceKind.None, typeParams(0).Variance)
            Assert.Equal(0, typeParams(0).Ordinal)
            Assert.Equal(Accessibility.NotApplicable, typeParams(0).DeclaredAccessibility)

            Assert.Equal("U", typeParams(1).Name)
            Assert.Equal(VarianceKind.In, typeParams(1).Variance)
            Assert.Equal(1, typeParams(1).Ordinal)
            Assert.Equal(Accessibility.NotApplicable, typeParams(1).DeclaredAccessibility)

            Assert.Equal("V", typeParams(2).Name)
            Assert.Equal(VarianceKind.Out, typeParams(2).Variance)
            Assert.Equal(2, typeParams(2).Ordinal)
            Assert.Equal(Accessibility.NotApplicable, typeParams(2).DeclaredAccessibility)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <WorkItem(537199, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537199")>
        <Fact>
        Public Sub UseTypeInNetModule()
            Dim mscorlibRef = TestMetadata.Net40.mscorlib
            Dim module1Ref = TestReferences.SymbolsTests.netModule.netModule1
            Dim text = <literal>
Class Test
    Dim a As Class1 = Nothing

    Public Sub New()
    End Sub
End Class
</literal>.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.From(text), VisualBasicParseOptions.Default, "")
            Dim comp As VisualBasicCompilation = VisualBasicCompilation.Create("Test", {tree}, {mscorlibRef, module1Ref})

            Dim globalNS = comp.SourceModule.GlobalNamespace
            Dim classTest = DirectCast(globalNS.GetTypeMembers("Test").First(), NamedTypeSymbol)
            Dim members = classTest.GetMembers() ' has to have mscorlib
            Dim varA = DirectCast(members(0), FieldSymbol)
            Assert.Equal(SymbolKind.Field, varA.Kind)
            Assert.Equal(TypeKind.Class, varA.Type.TypeKind)
            Assert.Equal(SymbolKind.NamedType, varA.Type.Kind)
        End Sub

        ' Date: IEEE 64bits (8 bytes) values
        <Fact>
        Public Sub PredefinedType01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Generic">
    <file name="pd.vb">
Namespace NS
  Friend Module MyMod
     Dim dateField As Date = #8/13/2002 12:14 PM#

     Function DateFunc() As Date()
            Dim Obj As Object = #10/10/2001#
            Return Obj
     End Function

    Sub DateSub(ByVal Ary(,,) As Date)
#If compErrorTest Then
            'COMPILEERROR: BC30414, "New Date() {}"
            Ary = New Date() {}
#End If
    End Sub

    Shared Sub New()
    End Sub
  End Module
End Namespace
    </file>
</compilation>)

            Dim nsNS = DirectCast(compilation.Assembly.GlobalNamespace.GetMembers("NS").Single(), NamespaceSymbol)
            Dim modOfNS = DirectCast(nsNS.GetMembers("MyMod").Single(), NamedTypeSymbol)
            Dim members = modOfNS.GetMembers()
            Assert.Equal(4, members.Length) ' 3 members + implicit shared constructor

            Dim mem1 = DirectCast(members(0), FieldSymbol)
            Assert.Equal(modOfNS, mem1.ContainingSymbol)
            Assert.Equal(Accessibility.Private, mem1.DeclaredAccessibility)
            Assert.Equal(SymbolKind.Field, mem1.Kind)
            Assert.Equal(TypeKind.Structure, mem1.Type.TypeKind)
            Assert.Equal("Date", mem1.Type.ToDisplayString())
            Assert.Equal("System.DateTime", mem1.Type.ToTestDisplayString())

            Dim mem2 = DirectCast(members(1), MethodSymbol)
            Assert.Equal(modOfNS, mem2.ContainingSymbol)
            Assert.Equal(Accessibility.Public, mem2.DeclaredAccessibility)
            Assert.Equal(SymbolKind.Method, mem2.Kind)
            Assert.Equal(TypeKind.Array, mem2.ReturnType.TypeKind)
            Dim ary = DirectCast(mem2.ReturnType, ArrayTypeSymbol)
            Assert.Equal(1, ary.Rank)
            Assert.Equal("Date", ary.ElementType.ToDisplayString())
            Assert.Equal("System.DateTime", ary.ElementType.ToTestDisplayString())

            Dim mem3 = DirectCast(modOfNS.GetMembers("DateSub").Single(), MethodSymbol)
            Assert.Equal(modOfNS, mem3.ContainingSymbol)
            Assert.Equal(Accessibility.Public, mem3.DeclaredAccessibility)
            Assert.Equal(SymbolKind.Method, mem3.Kind)
            Assert.True(mem3.IsSub)
            Dim param = DirectCast(mem3.Parameters(0), ParameterSymbol)
            Assert.Equal(TypeKind.Array, param.Type.TypeKind)
            ary = DirectCast(param.Type, ArrayTypeSymbol)
            Assert.Equal(3, ary.Rank)
            Assert.Equal("Date", ary.ElementType.ToDisplayString())
            Assert.Equal("System.DateTime", ary.ElementType.ToTestDisplayString())
        End Sub

        <WorkItem(537461, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537461")>
        <Fact>
        Public Sub SourceTypeUndefinedBaseType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="SourceTypeUndefinedBaseType">
    <file name="undefinedbasetype.vb">
Class Class1 : Inherits Goo
End Class
    </file>
</compilation>)
            Dim baseType = compilation.GlobalNamespace.GetTypeMembers("Class1").Single().BaseType
            Assert.Equal("Goo", baseType.ToTestDisplayString())
            Assert.Equal(SymbolKind.ErrorType, baseType.Kind)
        End Sub

        <WorkItem(537467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537467")>
        <Fact>
        Public Sub TopLevelPrivateTypes()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation name="C">
                    <file name="a.vb">
Option Strict Off 
Option Explicit Off
Imports VB6 = Microsoft.VisualBasic

Namespace InterfaceErr005
    'COMPILEERROR: BC31089, "PrivateUDT"
    Private Structure PrivateUDT
        Public x As Short
    End Structure

    'COMPILEERROR: BC31089, "PrivateIntf"
    Private Interface PrivateIntf
        Function goo()
    End Interface
   'COMPILEERROR: BC31047
    Protected Class ProtectedClass

    End Class
  
End Namespace
                    </file>
                </compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim ns = DirectCast(globalNS.GetMembers("InterfaceErr005").First(), NamespaceSymbol)
            Dim type1 = DirectCast(ns.GetTypeMembers("PrivateUDT").Single(), NamedTypeSymbol)
            Assert.Equal(Accessibility.Friend, type1.DeclaredAccessibility)  ' NOTE: for erroneous symbols we return 'best guess'
            type1 = DirectCast(ns.GetTypeMembers("PrivateIntf").Single(), NamedTypeSymbol)
            Assert.Equal(Accessibility.Friend, type1.DeclaredAccessibility)  ' NOTE: for erroneous symbols we return 'best guess'
            type1 = DirectCast(ns.GetTypeMembers("ProtectedClass").Single(), NamedTypeSymbol)
            Assert.Equal(Accessibility.Friend, type1.DeclaredAccessibility)  ' NOTE: for erroneous symbols we return 'best guess'

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31089: Types declared 'Private' must be inside another type.
    Private Structure PrivateUDT
                      ~~~~~~~~~~
BC31089: Types declared 'Private' must be inside another type.
    Private Interface PrivateIntf
                      ~~~~~~~~~~~
BC31047: Protected types can only be declared inside of a class.
    Protected Class ProtectedClass
                    ~~~~~~~~~~~~~~
</expected>)

        End Sub

        <WorkItem(527185, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527185")>
        <Fact>
        Public Sub InheritTypeFromMetadata01()

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Test2">
    <file name="b.vb">
Public Module m1

    Public Class C1_1
        Public Class goo
        End Class
    End Class

End Module
    </file>
</compilation>)

            Dim compRef1 = New VisualBasicCompilationReference(comp1)
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="Test1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Namespace ShadowsGen203
    ' from mscorlib
    Public Class MyAttribute
        Inherits Attribute
    End Class

    ' from another module
    Public Module M1
        Public Class C1_3
            Inherits C1_1
            Private Shadows Class goo
            End Class
        End Class

    End Module
End Namespace
    </file>
</compilation>, {compRef1})
            '  VisualBasicCompilation.Create("Test", CompilationOptions.Default, {SyntaxTree.ParseCompilationUnit(text1)}, {compRef1})

            Dim ns = DirectCast(comp.GlobalNamespace.GetMembers("ShadowsGen203").Single(), NamespaceSymbol)
            Dim mod1 = DirectCast(ns.GetMembers("m1").Single(), NamedTypeSymbol)
            Dim type1 = DirectCast(mod1.GetTypeMembers("C1_3").Single(), NamedTypeSymbol)
            Assert.Equal("C1_1", type1.BaseType.Name)

            Dim type2 = DirectCast(ns.GetTypeMembers("MyAttribute").Single(), NamedTypeSymbol)
            Assert.Equal("Attribute", type2.BaseType.Name)

        End Sub

        <WorkItem(537753, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537753")>
        <Fact>
        Public Sub ImplementTypeCrossComps()

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Test2">
        <file name="comp.vb">
Imports System.Collections.Generic

Namespace MT
    Public Interface IGoo(Of T)
        Sub M(ByVal t As T)
    End Interface
End Namespace
    </file>
    </compilation>)

            Dim compRef1 = New VisualBasicCompilationReference(comp1)

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="Test2">
    <file name="comp2.vb">
    Imports System.Collections.Generic
    Imports MT

    Namespace SS
        Public Class Goo
            Implements IGoo(Of String)
            Sub N(ByVal s As String) Implements IGoo(Of String).M
            End Sub
        End Class
    End Namespace
    </file>
</compilation>, {compRef1})


            Dim ns = DirectCast(comp.SourceModule.GlobalNamespace.GetMembers("SS").Single(), NamespaceSymbol)
            Dim type1 = DirectCast(ns.GetTypeMembers("Goo", 0).Single(), NamedTypeSymbol)
            ' Not impl ex
            Assert.Equal(1, type1.Interfaces.Length)
            Dim type2 = DirectCast(type1.Interfaces(0), NamedTypeSymbol)
            Assert.Equal(TypeKind.Interface, type2.TypeKind)
            Assert.Equal(1, type2.Arity)
            Assert.Equal(1, type2.TypeParameters.Length)
            Assert.Equal("MT.IGoo(Of System.String)", type2.ToTestDisplayString())

        End Sub

        <WorkItem(537492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537492")>
        <Fact>
        Public Sub PartialClassImplInterface()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="C">
                    <file name="a.vb">
Option Strict Off 
Option Explicit On

Imports System.Collections.Generic

Public Interface vbInt2(Of T)
    Sub Sub1(ByVal b1 As Byte, ByRef t2 As T)
    Function Fun1(Of Z)(ByVal p1 As T) As Z
End Interface
                    </file>
                    <file name="b.vb">
Module Module1
Public Class vbPartialCls200a(Of P, Q)    ' for test GenPartialCls200
    Implements vbInt2(Of P)

    Public Function Fun1(Of X)(ByVal a As P) As X Implements vbInt2(Of P).Fun1
        Return Nothing
    End Function

End Class

Partial Public Class vbPartialCls200a(Of P, Q)
    Implements vbInt2(Of P)
    Public Sub Sub1(ByVal p1 As Byte, ByRef p2 As P) Implements vbInt2(Of P).Sub1
    End Sub

End Class

Sub Main()
End Sub
End Module
               </file>
                </compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim myMod = DirectCast(globalNS.GetMembers("Module1").First(), NamedTypeSymbol)
            Dim type1 = DirectCast(myMod.GetTypeMembers("vbPartialCls200a").Single(), NamedTypeSymbol)
            Assert.Equal(1, type1.Interfaces.Length)
        End Sub

        <Fact>
        Public Sub CyclesInStructureDeclarations()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
        <compilation name="C">
            <file name="a.vb">
Module Module1
    Structure stdUDT
    End Structure
    Structure nestedUDT
        Public xstdUDT As stdUDT
        Public xstdUDTarr() As stdUDT
    End Structure
    Public m_nx1var As nestedUDT
    Public m_nx2var As nestedUDT
    Sub Scen1()
        Dim x2var As stdUDT
    End Sub
End Module
    </file>
        </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42024: Unused local variable: 'x2var'.
        Dim x2var As stdUDT
            ~~~~~
</errors>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="C">
        <file name="a.vb">
Module Module1
    Structure OuterStruct
        Structure InnerStruct
            Public t As OuterStruct
        End Structure
        Public one As InnerStruct
        Public two As InnerStruct
        Sub Scen1()
            Dim three As OuterStruct
        End Sub
    End Structure
End Module
    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'OuterStruct' cannot contain an instance of itself: 
    'Module1.OuterStruct' contains 'Module1.OuterStruct.InnerStruct' (variable 'one').
    'Module1.OuterStruct.InnerStruct' contains 'Module1.OuterStruct' (variable 't').
        Public one As InnerStruct
               ~~~
BC42024: Unused local variable: 'three'.
            Dim three As OuterStruct
                ~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub CyclesInStructureDeclarations2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CyclesInStructureDeclarations2">
        <file name="a.vb">
Structure st1(Of T)
    Dim x As T
End Structure

Structure st2
    Dim x As st1(Of st2)
End Structure
    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'st2' cannot contain an instance of itself: 
    'st2' contains 'st1(Of st2)' (variable 'x').
    'st1(Of st2)' contains 'st2' (variable 'x').
    Dim x As st1(Of st2)
        ~
</errors>)
        End Sub

        <Fact>
        Public Sub CyclesInStructureDeclarations2_()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CyclesInStructureDeclarations2_">
        <file name="a.vb">
Structure st2
    Dim x As st1(Of st2)
End Structure

Structure st1(Of T)
    Dim x As T
End Structure
    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'st2' cannot contain an instance of itself: 
    'st2' contains 'st1(Of st2)' (variable 'x').
    'st1(Of st2)' contains 'st2' (variable 'x').
    Dim x As st1(Of st2)
        ~
</errors>)
        End Sub

        <Fact>
        Public Sub CyclesInStructureDeclarations3()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
        <compilation name="CyclesInStructureDeclarations3">
            <file name="a.vb">
Structure st1(Of T)
    Dim x As st2
End Structure

Structure st2
    Dim x As st1(Of st2)
End Structure
    </file>
        </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'st1' cannot contain an instance of itself: 
    'st1(Of T)' contains 'st2' (variable 'x').
    'st2' contains 'st1(Of st2)' (variable 'x').
    Dim x As st2
        ~
</errors>)
        End Sub

        <Fact>
        Public Sub CyclesInStructureDeclarations3_()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
        <compilation name="CyclesInStructureDeclarations3_">
            <file name="a.vb">
Structure st2
    Dim x As st1(Of st2)
End Structure

Structure st1(Of T)
    Dim x As st2
End Structure
    </file>
        </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'st2' cannot contain an instance of itself: 
    'st2' contains 'st1(Of st2)' (variable 'x').
    'st1(Of T)' contains 'st2' (variable 'x').
    Dim x As st1(Of st2)
        ~
</errors>)
        End Sub

        <Fact>
        Public Sub CyclesInStructureDeclarations4()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
        <compilation name="CyclesInStructureDeclarations4">
            <file name="a.vb">
Structure E
End Structure

Structure X(Of T)
    Public _t As T
End Structure

Structure Y
    Public xz As X(Of Z)
End Structure

Structure Z
    Public xe As X(Of E)
    Public xy As X(Of Y)
End Structure
    </file>
        </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'Y' cannot contain an instance of itself: 
    'Y' contains 'X(Of Z)' (variable 'xz').
    'X(Of Z)' contains 'Z' (variable '_t').
    'Z' contains 'X(Of Y)' (variable 'xy').
    'X(Of Y)' contains 'Y' (variable '_t').
    Public xz As X(Of Z)
           ~~
</errors>)
        End Sub

        <Fact>
        Public Sub PortedFromCSharp_StructLayoutCycle01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
        <compilation name="PortedFromCSharp_StructLayoutCycle01">
            <file name="a.vb">
Module Module1
    Structure A
        Public F As A  ' BC30294
        Public F_ As A ' no additional error
    End Structure
    Structure B
        Public F As C ' BC30294
        Public G As C ' no additional error, cycle is reported for B.F
    End Structure
    Structure C
        Public G As B ' no additional error, cycle is reported for B.F
    End Structure
    Structure D(Of T)
        Public F As D(Of D(Of Object)) ' BC30294
    End Structure
    Structure E
        Public F As F(Of E) ' no error
    End Structure
    Class F(Of T)
        Public G As E ' no error
    End Class
    Structure G
        Public F As H(Of G) ' BC30294
    End Structure
    Structure H(Of T)
        Public G As G ' no additional error, cycle is reported for B.F
    End Structure
    Structure J
        Public Shared j As J ' no error
    End Structure
    Structure K
        Public Shared l As L ' no error
    End Structure
    Structure L
        Public l As K ' no error
    End Structure
End Module
    </file>
        </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'A' cannot contain an instance of itself: 
    'Module1.A' contains 'Module1.A' (variable 'F').
        Public F As A  ' BC30294
               ~
BC30294: Structure 'B' cannot contain an instance of itself: 
    'Module1.B' contains 'Module1.C' (variable 'F').
    'Module1.C' contains 'Module1.B' (variable 'G').
        Public F As C ' BC30294
               ~
BC30294: Structure 'D' cannot contain an instance of itself: 
    'Module1.D(Of T)' contains 'Module1.D(Of Module1.D(Of Object))' (variable 'F').
        Public F As D(Of D(Of Object)) ' BC30294
               ~
BC30294: Structure 'G' cannot contain an instance of itself: 
    'Module1.G' contains 'Module1.H(Of Module1.G)' (variable 'F').
    'Module1.H(Of T)' contains 'Module1.G' (variable 'G').
        Public F As H(Of G) ' BC30294
               ~
</errors>)
        End Sub

        <Fact>
        Public Sub PortedFromCSharp_StructLayoutCycle02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
        <compilation name="PortedFromCSharp_StructLayoutCycle01">
            <file name="a.vb">
Module Module1
    Structure A
        Public Property P1 As A ' BC30294
        Public Property P2 As A ' no additional error
    End Structure
    Structure B
        Public Property C1 As C ' BC30294
        Public Property C2 As C ' no additional error
    End Structure
    Structure C
        Public Property B1 As B ' no error, cycle is already reported
        Public Property B2 As B ' no additional error
    End Structure
    Structure D(Of T)
        Public Property P1 As D(Of D(Of Object)) ' BC30294
        Public Property P2 As D(Of D(Of Object)) ' no additional error
    End Structure
    Structure E
        Public Property F1 As F(Of E) ' no error
        Public Property F2 As F(Of E) ' no error
    End Structure
    Class F(Of T)
        Public Property P1 As E ' no error
        Public Property P2 As E ' no error
    End Class
    Structure G
        Public Property H1 As H(Of G) ' BC30294
        Public Property H2 As H(Of G) ' no additional error
        Public Property G1 As G ' BC30294
    End Structure
    Structure H(Of T)
        Public Property G1 As G ' no error
        Public Property G2 As G ' no error
    End Structure
    Structure J
        Public Shared Property j As J ' no error
    End Structure
    Structure K
        Public Shared Property l As L ' no error
    End Structure
    Structure L
        Public Property l As K ' no error
    End Structure
    Structure M
        Public Property N1 As N ' no error
        Public Property N2 As N ' no error
    End Structure
    Structure N
        Public Property M1 As M ' no error
            Get
                Return Nothing
            End Get
            Set(value As M)
            End Set
        End Property
        Public Property M2 As M ' no error
            Get
                Return Nothing
            End Get
            Set(value As M)
            End Set
        End Property
    End Structure
End Module
    </file>
        </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'A' cannot contain an instance of itself: 
    'Module1.A' contains 'Module1.A' (variable '_P1').
        Public Property P1 As A ' BC30294
                        ~~
BC30294: Structure 'B' cannot contain an instance of itself: 
    'Module1.B' contains 'Module1.C' (variable '_C1').
    'Module1.C' contains 'Module1.B' (variable '_B1').
        Public Property C1 As C ' BC30294
                        ~~
BC30294: Structure 'D' cannot contain an instance of itself: 
    'Module1.D(Of T)' contains 'Module1.D(Of Module1.D(Of Object))' (variable '_P1').
        Public Property P1 As D(Of D(Of Object)) ' BC30294
                        ~~
BC30294: Structure 'G' cannot contain an instance of itself: 
    'Module1.G' contains 'Module1.H(Of Module1.G)' (variable '_H1').
    'Module1.H(Of T)' contains 'Module1.G' (variable '_G1').
        Public Property H1 As H(Of G) ' BC30294
                        ~~
BC30294: Structure 'G' cannot contain an instance of itself: 
    'Module1.G' contains 'Module1.G' (variable '_G1').
        Public Property G1 As G ' BC30294
                        ~~
</errors>)
        End Sub

        <Fact>
        Public Sub MultiplyCyclesInStructure01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
        <compilation name="PortedFromCSharp_StructLayoutCycle01">
            <file name="a.vb">
Structure S1
    Dim s2 As S2  ' ERROR
    Dim s2_ As S2 ' NO ERROR 
    Dim s3 As S3  ' ERROR
End Structure

Structure S2
    Dim s1 As S1
End Structure

Structure S3
    Dim s1 As S1
End Structure
    </file>
        </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'S1' cannot contain an instance of itself: 
    'S1' contains 'S2' (variable 's2').
    'S2' contains 'S1' (variable 's1').
    Dim s2 As S2  ' ERROR
        ~~
BC30294: Structure 'S1' cannot contain an instance of itself: 
    'S1' contains 'S3' (variable 's3').
    'S3' contains 'S1' (variable 's1').
    Dim s3 As S3  ' ERROR
        ~~
</errors>)
        End Sub

        <Fact>
        Public Sub MultiplyCyclesInStructure02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
        <compilation name="PortedFromCSharp_StructLayoutCycle01">
            <file name="a.vb">
Structure S1
    Dim s2 As S2  ' ERROR
    Dim s2_ As S2 ' NO ERROR 
    Dim s3 As S3  ' NO ERROR 
End Structure

Structure S2
    Dim s1 As S1
End Structure

Structure S3
    Dim s2 As S2
End Structure
    </file>
        </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'S1' cannot contain an instance of itself: 
    'S1' contains 'S2' (variable 's2').
    'S2' contains 'S1' (variable 's1').
    Dim s2 As S2  ' ERROR
        ~~
</errors>)
        End Sub

        <Fact>
        Public Sub MultiplyCyclesInStructure03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
        <compilation name="PortedFromCSharp_StructLayoutCycle01">
            <file name="a.vb">
Structure S1
    Dim s2 As S2 ' two errors
    Dim s2_ As S2 ' no errors
End Structure

Structure S2
    Dim s1 As S1
    Dim s3 As S3
End Structure

Structure S3
    Dim s1 As S1
End Structure
    </file>
        </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'S1' cannot contain an instance of itself: 
    'S1' contains 'S2' (variable 's2').
    'S2' contains 'S1' (variable 's1').
    Dim s2 As S2 ' two errors
        ~~
BC30294: Structure 'S1' cannot contain an instance of itself: 
    'S1' contains 'S2' (variable 's2').
    'S2' contains 'S3' (variable 's3').
    'S3' contains 'S1' (variable 's1').
    Dim s2 As S2 ' two errors
        ~~
</errors>)
        End Sub

        <Fact>
        Public Sub MultiplyCyclesInStructure04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
        <compilation name="PortedFromCSharp_StructLayoutCycle01">
            <file name="a.vb">
Structure S1
    Dim s2 As S2 ' two errors
    Dim s2_ As S2 ' no errors
End Structure

Structure S2
    Dim s3 As S3
    Dim s1 As S1
    Dim s1_ As S1
End Structure

Structure S3
    Dim s1 As S1
End Structure
    </file>
        </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30294: Structure 'S1' cannot contain an instance of itself: 
    'S1' contains 'S2' (variable 's2').
    'S2' contains 'S1' (variable 's1').
    Dim s2 As S2 ' two errors
        ~~
BC30294: Structure 'S1' cannot contain an instance of itself: 
    'S1' contains 'S2' (variable 's2').
    'S2' contains 'S3' (variable 's3').
    'S3' contains 'S1' (variable 's1').
    Dim s2 As S2 ' two errors
        ~~
</errors>)
        End Sub

        <Fact>
        Public Sub MultiplyCyclesInStructure05()

            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
        <compilation name="MultiplyCyclesInStructure05_I">
            <file name="a.vb">
Public Structure SI_1
End Structure

Public Structure SI_2
    Public s1 As SI_1
End Structure
    </file>
        </compilation>)

            CompilationUtils.AssertNoErrors(compilation1)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
        <compilation name="MultiplyCyclesInStructure05_II">
            <file name="a.vb">
Public Structure SII_3
    Public s2 As SI_2
End Structure

Public Structure SII_4
    Public s3 As SII_3
End Structure
    </file>
        </compilation>, {New VisualBasicCompilationReference(compilation1)})
            CompilationUtils.AssertNoErrors(compilation2)


            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
        <compilation name="MultiplyCyclesInStructure05_I">
            <file name="a.vb">
Public Structure SI_1
    Public s4 As SII_4
End Structure

Public Structure SI_2
    Public s1 As SI_1
End Structure
    </file>
        </compilation>, {New VisualBasicCompilationReference(compilation2)})
            CompilationUtils.AssertTheseDiagnostics(compilation3,
<errors>
BC30294: Structure 'SI_1' cannot contain an instance of itself: 
    'SI_1' contains 'SII_4' (variable 's4').
    'SII_4' contains 'SII_3' (variable 's3').
    'SII_3' contains 'SI_2' (variable 's2').
    'SI_2' contains 'SI_1' (variable 's1').
    Public s4 As SII_4
           ~~
</errors>)
        End Sub

        <Fact>
        Public Sub SynthesizedConstructorLocation()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
               <compilation name="C">
                   <file name="a.vb">
Class Goo
End Class
                    </file>
               </compilation>)

            Dim typeGoo = compilation.SourceModule.GlobalNamespace.GetTypeMembers("Goo").Single()
            Dim instanceConstructor = typeGoo.InstanceConstructors.Single()

            AssertEx.Equal(typeGoo.Locations, instanceConstructor.Locations)
        End Sub

        <Fact>
        Public Sub UsingProtectedInStructureMethods()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
               <compilation name="UsingProtectedInStructureMethods">
                   <file name="a.vb">
Structure Goo
    Protected Overrides Sub Finalize()
    End Sub
    Protected Sub OtherMethod()
    End Sub
End Structure
                    </file>
               </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31067: Method in a structure cannot be declared 'Protected', 'Protected Friend', or 'Private Protected'.
    Protected Sub OtherMethod()
    ~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub UsingMustOverrideInStructureMethods()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
               <compilation name="UsingProtectedInStructureMethods">
                   <file name="a.vb">
Module Module1
Sub Main()
End Sub
End Module
 
Structure S2
Public MustOverride Function Goo() As String
End Function
End Structure

                    </file>
               </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30435: Members in a Structure cannot be declared 'MustOverride'.
Public MustOverride Function Goo() As String
       ~~~~~~~~~~~~
BC30430: 'End Function' must be preceded by a matching 'Function'.
End Function
~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub Bug4135()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
               <compilation name="Bug4135">
                   <file name="a.vb">
Interface I1
    Protected Interface I2
    End Interface
    Protected Friend Interface I3
    End Interface

    Protected delegate Sub D1()
    Protected Friend delegate Sub D2()

    Protected Class C1
    End Class
    Protected Friend Class C2
    End Class

    Protected Structure S1
    End Structure

    Protected Friend Structure S2
    End Structure

    Protected Enum E1
        val
    End Enum
    Protected Friend Enum E2
        val
    End Enum

    'Protected F1 As Integer
    'Protected Friend F2 As Integer

    Protected Sub Sub1()
    Protected Friend Sub Sub2()
End Interface

Structure S3
    Protected Interface I4
    End Interface
    Protected Friend Interface I5
    End Interface

    Protected delegate Sub D3()
    Protected Friend delegate Sub D4()

    Protected Class C3
    End Class
    Protected Friend Class C4
    End Class

    Protected Structure S4
    End Structure

    Protected Friend Structure S5
    End Structure

    Protected Enum E3
        val
    End Enum
    Protected Friend Enum E4
        val
    End Enum

    Protected F3 As Integer
    Protected Friend F4 As Integer

    Protected Sub Sub3()
    End Sub

    Protected Friend Sub Sub4()
    End Sub
End Structure

Module M1
    Protected Interface I8
    End Interface
    Protected Friend Interface I9
    End Interface

    Protected delegate Sub D7()
    Protected Friend delegate Sub D8()

    Protected Class C7
    End Class
    Protected Friend Class C8
    End Class

    Protected Structure S8
    End Structure

    Protected Friend Structure S9
    End Structure

    Protected Enum E5
        val
    End Enum
    Protected Friend Enum E6
        val
    End Enum

    Protected F5 As Integer
    Protected Friend F6 As Integer

    Protected Sub Sub7()
    End Sub
    Protected Friend Sub Sub8()
    End Sub

End Module

Protected Interface I11
End Interface

Protected Structure S11
End Structure

Protected Class C11
End Class

Protected Enum E11
    val
End Enum

Protected Module M11
End Module

Protected Friend Interface I12
End Interface

Protected Friend Structure S12
End Structure

Protected Friend Class C12
End Class

Protected Friend Enum E12
    val
End Enum

Protected Friend Module M12
End Module

Protected delegate Sub D11()
Protected Friend delegate Sub D12()

Class C4
    Protected Interface I6
    End Interface
    Protected Friend Interface I7
    End Interface

    Protected delegate Sub D5()
    Protected Friend delegate Sub D6()

    Protected Class C5
    End Class
    Protected Friend Class C6
    End Class

    Protected Structure S6
    End Structure

    Protected Friend Structure S7
    End Structure

    Protected Enum E7
        val
    End Enum
    Protected Friend Enum E8
        val
    End Enum

    Protected F7 As Integer
    Protected Friend F8 As Integer

    Protected Sub Sub5()
    End Sub
    Protected Friend Sub Sub6()
    End Sub

End Class
                    </file>
               </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31209: Interface in an interface cannot be declared 'Protected'.
    Protected Interface I2
    ~~~~~~~~~
BC31209: Interface in an interface cannot be declared 'Protected Friend'.
    Protected Friend Interface I3
    ~~~~~~~~~~~~~~~~
BC31068: Delegate in an interface cannot be declared 'Protected'.
    Protected delegate Sub D1()
    ~~~~~~~~~
BC31068: Delegate in an interface cannot be declared 'Protected Friend'.
    Protected Friend delegate Sub D2()
    ~~~~~~~~~~~~~~~~
BC31070: Class in an interface cannot be declared 'Protected'.
    Protected Class C1
    ~~~~~~~~~
BC31070: Class in an interface cannot be declared 'Protected Friend'.
    Protected Friend Class C2
    ~~~~~~~~~~~~~~~~
BC31071: Structure in an interface cannot be declared 'Protected'.
    Protected Structure S1
    ~~~~~~~~~
BC31071: Structure in an interface cannot be declared 'Protected Friend'.
    Protected Friend Structure S2
    ~~~~~~~~~~~~~~~~
BC31069: Enum in an interface cannot be declared 'Protected'.
    Protected Enum E1
    ~~~~~~~~~
BC31069: Enum in an interface cannot be declared 'Protected Friend'.
    Protected Friend Enum E2
    ~~~~~~~~~~~~~~~~
BC30270: 'Protected' is not valid on an interface method declaration.
    Protected Sub Sub1()
    ~~~~~~~~~
BC30270: 'Protected Friend' is not valid on an interface method declaration.
    Protected Friend Sub Sub2()
    ~~~~~~~~~~~~~~~~
BC31047: Protected types can only be declared inside of a class.
    Protected Interface I4
                        ~~
BC31047: Protected types can only be declared inside of a class.
    Protected Friend Interface I5
                               ~~
BC31047: Protected types can only be declared inside of a class.
    Protected delegate Sub D3()
                           ~~
BC31047: Protected types can only be declared inside of a class.
    Protected Friend delegate Sub D4()
                                  ~~
BC31047: Protected types can only be declared inside of a class.
    Protected Class C3
                    ~~
BC31047: Protected types can only be declared inside of a class.
    Protected Friend Class C4
                           ~~
BC31047: Protected types can only be declared inside of a class.
    Protected Structure S4
                        ~~
BC31047: Protected types can only be declared inside of a class.
    Protected Friend Structure S5
                               ~~
BC31047: Protected types can only be declared inside of a class.
    Protected Enum E3
                   ~~
BC31047: Protected types can only be declared inside of a class.
    Protected Friend Enum E4
                          ~~
BC30435: Members in a Structure cannot be declared 'Protected'.
    Protected F3 As Integer
    ~~~~~~~~~
BC30435: Members in a Structure cannot be declared 'Protected Friend'.
    Protected Friend F4 As Integer
    ~~~~~~~~~~~~~~~~
BC31067: Method in a structure cannot be declared 'Protected', 'Protected Friend', or 'Private Protected'.
    Protected Sub Sub3()
    ~~~~~~~~~
BC31067: Method in a structure cannot be declared 'Protected', 'Protected Friend', or 'Private Protected'.
    Protected Friend Sub Sub4()
    ~~~~~~~~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected'.
    Protected Interface I8
    ~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected Friend'.
    Protected Friend Interface I9
    ~~~~~~~~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected'.
    Protected delegate Sub D7()
    ~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected Friend'.
    Protected Friend delegate Sub D8()
    ~~~~~~~~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected'.
    Protected Class C7
    ~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected Friend'.
    Protected Friend Class C8
    ~~~~~~~~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected'.
    Protected Structure S8
    ~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected Friend'.
    Protected Friend Structure S9
    ~~~~~~~~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected'.
    Protected Enum E5
    ~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected Friend'.
    Protected Friend Enum E6
    ~~~~~~~~~~~~~~~~
BC30593: Variables in Modules cannot be declared 'Protected'.
    Protected F5 As Integer
    ~~~~~~~~~
BC30593: Variables in Modules cannot be declared 'Protected Friend'.
    Protected Friend F6 As Integer
    ~~~~~~~~~~~~~~~~
BC30433: Methods in a Module cannot be declared 'Protected'.
    Protected Sub Sub7()
    ~~~~~~~~~
BC30433: Methods in a Module cannot be declared 'Protected Friend'.
    Protected Friend Sub Sub8()
    ~~~~~~~~~~~~~~~~
BC31047: Protected types can only be declared inside of a class.
Protected Interface I11
                    ~~~
BC31047: Protected types can only be declared inside of a class.
Protected Structure S11
                    ~~~
BC31047: Protected types can only be declared inside of a class.
Protected Class C11
                ~~~
BC31047: Protected types can only be declared inside of a class.
Protected Enum E11
               ~~~
BC31047: Protected types can only be declared inside of a class.
Protected Module M11
                 ~~~
BC31047: Protected types can only be declared inside of a class.
Protected Friend Interface I12
                           ~~~
BC31047: Protected types can only be declared inside of a class.
Protected Friend Structure S12
                           ~~~
BC31047: Protected types can only be declared inside of a class.
Protected Friend Class C12
                       ~~~
BC31047: Protected types can only be declared inside of a class.
Protected Friend Enum E12
                      ~~~
BC31047: Protected types can only be declared inside of a class.
Protected Friend Module M12
                        ~~~
BC31047: Protected types can only be declared inside of a class.
Protected delegate Sub D11()
                       ~~~
BC31047: Protected types can only be declared inside of a class.
Protected Friend delegate Sub D12()
                              ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Bug4136()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
               <compilation name="Bug4136">
                   <file name="a.vb">
Interface I1
    Private Interface I2
    End Interface

    Private delegate Sub D1()

    Private Class C1
    End Class

    Private Structure S1
    End Structure


    Private Enum E1
        val
    End Enum

    'Private F1 As Integer

    Private Sub Sub1()
End Interface

Private Interface I11
End Interface

Private Structure S11
End Structure

Private Class C11
End Class

Private Enum E11
    val
End Enum

Private Module M11
End Module

Private delegate Sub D11()

Structure S3
    Private Interface I4
    End Interface

    Private delegate Sub D3()

    Private Class C3
    End Class

    Private Structure S4
    End Structure

    Private Enum E3
        val
    End Enum

    Private F3 As Integer

    Private Sub Sub3()
    End Sub
End Structure

Module M1
    Private Interface I8
    End Interface

    Private delegate Sub D7()

    Private Class C7
    End Class

    Private Structure S8
    End Structure

    Private Enum E5
        val
    End Enum

    Private F5 As Integer

    Private Sub Sub7()
    End Sub

End Module

Class C4
    Private Interface I6
    End Interface

    Private delegate Sub D5()

    Private Class C5
    End Class

    Private Structure S6
    End Structure

    Private Enum E7
        val
    End Enum

    Private F7 As Integer

    Private Sub Sub5()
    End Sub
End Class
                    </file>
               </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31209: Interface in an interface cannot be declared 'Private'.
    Private Interface I2
    ~~~~~~~
BC31068: Delegate in an interface cannot be declared 'Private'.
    Private delegate Sub D1()
    ~~~~~~~
BC31070: Class in an interface cannot be declared 'Private'.
    Private Class C1
    ~~~~~~~
BC31071: Structure in an interface cannot be declared 'Private'.
    Private Structure S1
    ~~~~~~~
BC31069: Enum in an interface cannot be declared 'Private'.
    Private Enum E1
    ~~~~~~~
BC30270: 'Private' is not valid on an interface method declaration.
    Private Sub Sub1()
    ~~~~~~~
BC31089: Types declared 'Private' must be inside another type.
Private Interface I11
                  ~~~
BC31089: Types declared 'Private' must be inside another type.
Private Structure S11
                  ~~~
BC31089: Types declared 'Private' must be inside another type.
Private Class C11
              ~~~
BC31089: Types declared 'Private' must be inside another type.
Private Enum E11
             ~~~
BC31089: Types declared 'Private' must be inside another type.
Private Module M11
               ~~~
BC31089: Types declared 'Private' must be inside another type.
Private delegate Sub D11()
                     ~~~
</expected>)


            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
               <compilation name="Bug4136">
                   <file name="a.vb">
Interface I1
    Friend Interface I2
    End Interface

    Friend delegate Sub D1()

    Friend Class C1
    End Class

    Friend Structure S1
    End Structure


    Friend Enum E1
        val
    End Enum

    'Friend F1 As Integer

    Friend Sub Sub1()
End Interface

Friend Interface I11
End Interface

Friend Structure S11
End Structure

Friend Class C11
End Class

Friend Enum E11
    val
End Enum

Friend Module M11
End Module

Friend delegate Sub D11()

Structure S3
    Friend Interface I4
    End Interface

    Friend delegate Sub D3()

    Friend Class C3
    End Class

    Friend Structure S4
    End Structure

    Friend Enum E3
        val
    End Enum

    Friend F3 As Integer

    Friend Sub Sub3()
    End Sub
End Structure

Module M1
    Friend Interface I8
    End Interface

    Friend delegate Sub D7()

    Friend Class C7
    End Class

    Friend Structure S8
    End Structure

    Friend Enum E5
        val
    End Enum

    Friend F5 As Integer

    Friend Sub Sub7()
    End Sub

End Module

Class C4
    Friend Interface I6
    End Interface

    Friend delegate Sub D5()

    Friend Class C5
    End Class

    Friend Structure S6
    End Structure

    Friend Enum E7
        val
    End Enum

    Friend F7 As Integer

    Friend Sub Sub5()
    End Sub
End Class
                   </file>
               </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31068: Delegate in an interface cannot be declared 'Friend'.
    Friend delegate Sub D1()
    ~~~~~~
BC31070: Class in an interface cannot be declared 'Friend'.
    Friend Class C1
    ~~~~~~
BC31071: Structure in an interface cannot be declared 'Friend'.
    Friend Structure S1
    ~~~~~~
BC31069: Enum in an interface cannot be declared 'Friend'.
    Friend Enum E1
    ~~~~~~
BC30270: 'Friend' is not valid on an interface method declaration.
    Friend Sub Sub1()
    ~~~~~~
</expected>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
               <compilation name="Bug4136">
                   <file name="a.vb">
Interface I1
    Public Interface I2
    End Interface

    Public delegate Sub D1()

    Public Class C1
    End Class

    Public Structure S1
    End Structure


    Public Enum E1
        val
    End Enum

    'Public F1 As Integer

    Public Sub Sub1()
End Interface

Public Interface I11
End Interface

Public Structure S11
End Structure

Public Class C11
End Class

Public Enum E11
    val
End Enum

Public Module M11
End Module

Public delegate Sub D11()

Structure S3
    Public Interface I4
    End Interface

    Public delegate Sub D3()

    Public Class C3
    End Class

    Public Structure S4
    End Structure

    Public Enum E3
        val
    End Enum

    Public F3 As Integer

    Public Sub Sub3()
    End Sub
End Structure

Module M1
    Public Interface I8
    End Interface

    Public delegate Sub D7()

    Public Class C7
    End Class

    Public Structure S8
    End Structure

    Public Enum E5
        val
    End Enum

    Public F5 As Integer

    Public Sub Sub7()
    End Sub

End Module

Class C4
    Public Interface I6
    End Interface

    Public delegate Sub D5()

    Public Class C5
    End Class

    Public Structure S6
    End Structure

    Public Enum E7
        val
    End Enum

    Public F7 As Integer

    Public Sub Sub5()
    End Sub
End Class
                   </file>
               </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31068: Delegate in an interface cannot be declared 'Public'.
    Public delegate Sub D1()
    ~~~~~~
BC31070: Class in an interface cannot be declared 'Public'.
    Public Class C1
    ~~~~~~
BC31071: Structure in an interface cannot be declared 'Public'.
    Public Structure S1
    ~~~~~~
BC31069: Enum in an interface cannot be declared 'Public'.
    Public Enum E1
    ~~~~~~
BC30270: 'Public' is not valid on an interface method declaration.
    Public Sub Sub1()
    ~~~~~~
</expected>)

        End Sub

        ' Constructor initializers don't bind yet
        <WorkItem(7926, "DevDiv_Projects/Roslyn")>
        <WorkItem(541123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541123")>
        <Fact>
        Public Sub StructDefaultConstructorInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="StructDefaultConstructorInitializer">
    <file name="StructDefaultConstructorInitializer.vb">
Structure S
    Public _x As Integer
    Public Sub New(x As Integer)
        Me.New() ' Note: not allowed in Dev10
    End Sub
End Structure
    </file>
</compilation>)

            compilation.VerifyDiagnostics()

            Dim structType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("S")
            Dim constructors = structType.GetMembers(WellKnownMemberNames.InstanceConstructorName)
            Assert.Equal(2, constructors.Length)

            Dim sourceConstructor = CType(constructors.First(Function(c) Not c.IsImplicitlyDeclared), MethodSymbol)
            Dim synthesizedConstructor = CType(constructors.First(Function(c) c.IsImplicitlyDeclared), MethodSymbol)
            Assert.NotEqual(sourceConstructor, synthesizedConstructor)

            Assert.Equal(1, sourceConstructor.Parameters.Length)
            Assert.Equal(0, synthesizedConstructor.Parameters.Length)

        End Sub

        <Fact>
        Public Sub MetadataNameOfGenericTypes()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="MetadataName">
    <file name="a.vb">
Class Gen1(Of T, U, V)
End Class
Class NonGen
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.GlobalNamespace

            Dim gen1Class = DirectCast(globalNS.GetMembers("Gen1").First(), NamedTypeSymbol)
            Assert.Equal("Gen1", gen1Class.Name)
            Assert.Equal("Gen1`3", gen1Class.MetadataName)

            Dim nonGenClass = DirectCast(globalNS.GetMembers("NonGen").First(), NamedTypeSymbol)
            Assert.Equal("NonGen", nonGenClass.Name)
            Assert.Equal("NonGen", nonGenClass.MetadataName)

            Dim system = DirectCast(globalNS.GetMembers("System").First(), NamespaceSymbol)
            Dim equatable = DirectCast(system.GetMembers("IEquatable").First(), NamedTypeSymbol)
            Assert.Equal("IEquatable", equatable.Name)
            Assert.Equal("IEquatable`1", equatable.MetadataName)
        End Sub

        <Fact()>
        Public Sub TypeNameSpelling1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Public Class Aa
End Class

Public Partial Class AA
End Class
    </file>
    <file name="b.vb">
Public Partial Class aa
End Class
    </file>
</compilation>)
            Assert.Equal("Aa", compilation.GlobalNamespace.GetTypeMembers("aa")(0).Name)
            Assert.Equal("Aa", compilation.GlobalNamespace.GetTypeMembers("Aa")(0).Name)
            Assert.Equal("Aa", compilation.GlobalNamespace.GetTypeMembers("AA")(0).Name)
            Assert.Equal("Aa", compilation.GlobalNamespace.GetTypeMembers("aA")(0).Name)
        End Sub

        <Fact()>
        Public Sub TypeNameSpelling2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="b.vb">
Public Partial Class aa
End Class
    </file>
    <file name="a.vb">
Public Class Aa
End Class

Public Partial Class AA
End Class
    </file>
</compilation>)
            Assert.Equal("aa", compilation.GlobalNamespace.GetTypeMembers("aa")(0).Name)
            Assert.Equal("aa", compilation.GlobalNamespace.GetTypeMembers("Aa")(0).Name)
            Assert.Equal("aa", compilation.GlobalNamespace.GetTypeMembers("AA")(0).Name)
            Assert.Equal("aa", compilation.GlobalNamespace.GetTypeMembers("aA")(0).Name)
        End Sub

        <Fact()>
        Public Sub StructureInstanceConstructors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="C">
        <file name="b.vb">
Structure S1
    Sub new ()
    end sub
End Structure

Structure S2
    Sub new (optional i as integer = 1)
    end sub
End Structure

    </file>
    </compilation>)

            Dim s1 = compilation.GlobalNamespace.GetTypeMembers("s1")(0)
            Assert.Equal(1, s1.InstanceConstructors.Length)

            Dim s2 = compilation.GlobalNamespace.GetTypeMembers("s2")(0)
            Assert.Equal(2, s2.InstanceConstructors.Length)

            compilation.VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_NewInStruct, "new").WithLocation(2, 9)
)
        End Sub

        <Fact, WorkItem(530171, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530171")>
        Public Sub ErrorTypeTest01()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation name="Err">
        <file name="b.vb">
    Sub TopLevelMethod()
    End Sub
    </file>
    </compilation>)

            Dim symbol = DirectCast(compilation.SourceModule.GlobalNamespace.GetMembers().LastOrDefault(), NamedTypeSymbol)
            Assert.Equal("<invalid-global-code>", symbol.Name)
            Assert.False(symbol.IsErrorType(), "ErrorType")
            Assert.True(symbol.IsImplicitClass, "ImplicitClass")
        End Sub

        <Fact>
        Public Sub NameCollisionWithAddedModule_01()

            Dim source1 =
<compilation name="module">
    <file name="a.vb">
Namespace NS1
    Friend Class C1
    End Class

    Friend Class c2
    End Class
End Namespace

Namespace ns1
    Friend Class C3
    End Class

    Friend Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim modComp = CreateCompilationWithMscorlib40(source1, OutputKind.NetModule)
            Dim modRef = modComp.EmitToImageReference(expectedWarnings:=
            {
                Diagnostic(ERRID.WRN_NamespaceCaseMismatch3, "ns1").WithArguments("ns1", "NS1", "a.vb")
            })

            Dim source2 =
<compilation>
    <file name="a.vb">
Namespace NS1
    Friend Class C1
    End Class

    Friend Class c2
    End Class
End Namespace

Namespace ns1
    Friend Class C3
    End Class

    Friend Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source2, {modRef}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC40055: Casing of namespace name 'ns1' does not match casing of namespace name 'NS1' in 'a.vb'.
Namespace ns1
          ~~~
</expected>)

            CompileAndVerify(compilation)
        End Sub

        <Fact>
        Public Sub NameCollisionWithAddedModule_02()

            Dim source1 =
<compilation name="module">
    <file name="a.vb">
Namespace NS1
    Public Class C1
    End Class

    Public Class c2
    End Class
End Namespace

Namespace ns1
    Public Class C3
    End Class

    Public Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim modComp = CreateCompilationWithMscorlib40(source1, OutputKind.NetModule)
            Dim modRef = modComp.EmitToImageReference(expectedWarnings:=
            {
                Diagnostic(ERRID.WRN_NamespaceCaseMismatch3, "ns1").WithArguments("ns1", "NS1", "a.vb")
            })

            Dim source2 =
<compilation>
    <file name="a.vb">
Namespace NS1
    Friend Class C1
    End Class

    Friend Class c2
    End Class
End Namespace

Namespace ns1
    Friend Class C3
    End Class

    Friend Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source2, {modRef}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC37210: Type 'C1' conflicts with public type defined in added module 'module.netmodule'.
    Friend Class C1
                 ~~
BC37210: Type 'c2' conflicts with public type defined in added module 'module.netmodule'.
    Friend Class c2
                 ~~
BC40055: Casing of namespace name 'ns1' does not match casing of namespace name 'NS1' in 'a.vb'.
Namespace ns1
          ~~~
BC37210: Type 'C3' conflicts with public type defined in added module 'module.netmodule'.
    Friend Class C3
                 ~~
BC37210: Type 'c4' conflicts with public type defined in added module 'module.netmodule'.
    Friend Class c4
                 ~~
</expected>)
        End Sub

        <Fact>
        Public Sub NameCollisionWithAddedModule_03()

            Dim source1 =
<compilation name="module">
    <file name="a.vb">
    Public Class C1
    End Class

    Public Class c2
    End Class

Namespace ns2
    Public Class C3
    End Class

    Public Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim modComp = CreateCompilationWithMscorlib40(source1, OutputKind.NetModule)
            Dim modRef = modComp.EmitToImageReference()

            Dim source2 =
<compilation>
    <file name="a.vb">
Namespace NS1
    Friend Class C1
    End Class

    Friend Class c2
    End Class
End Namespace

Namespace ns1
    Friend Class C3
    End Class

    Friend Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source2, {modRef}, TestOptions.ReleaseDll)

            CompileAndVerify(compilation)
        End Sub

        <Fact>
        Public Sub NameCollisionWithAddedModule_04()

            Dim source1 =
<compilation name="module">
    <file name="a.vb">
Namespace NS1
    Public Class c1
    End Class
End Namespace

Namespace ns1
    Public Class c2
    End Class

    Public Class C3(Of T)
    End Class

    Public Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim modComp = CreateCompilationWithMscorlib40(source1, OutputKind.NetModule)
            Dim modRef = modComp.EmitToImageReference(expectedWarnings:=
            {
                Diagnostic(ERRID.WRN_NamespaceCaseMismatch3, "ns1").WithArguments("ns1", "NS1", "a.vb")
            })

            Dim source2 =
<compilation>
    <file name="a.vb">
Namespace NS1
    Friend Class C1
    End Class

    Friend Class c2
    End Class
End Namespace

Namespace ns1
    Friend Class C3
    End Class

    Friend Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source2, {modRef}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC40055: Casing of namespace name 'ns1' does not match casing of namespace name 'NS1' in 'a.vb'.
Namespace ns1
          ~~~
BC37210: Type 'c4' conflicts with public type defined in added module 'module.netmodule'.
    Friend Class c4
                 ~~
</expected>)
        End Sub

        <Fact>
        Public Sub NameCollisionWithAddedModule_05()

            Dim source1 =
<compilation name="module">
    <file name="a.vb">
Public Class C1
End Class

Public Class c2
End Class

Namespace ns1
    Public Class C3
    End Class

    Public Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim modComp = CreateCompilationWithMscorlib40(source1, OutputKind.NetModule)
            Dim modRef = modComp.EmitToImageReference()

            Dim source2 =
<compilation>
    <file name="a.vb">
Friend Class C1
End Class

Friend Class c2
End Class

Namespace ns1
    Friend Class C3
    End Class

    Friend Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source2, {modRef}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC37210: Type 'C1' conflicts with public type defined in added module 'module.netmodule'.
Friend Class C1
             ~~
BC37210: Type 'c2' conflicts with public type defined in added module 'module.netmodule'.
Friend Class c2
             ~~
BC37210: Type 'C3' conflicts with public type defined in added module 'module.netmodule'.
    Friend Class C3
                 ~~
BC37210: Type 'c4' conflicts with public type defined in added module 'module.netmodule'.
    Friend Class c4
                 ~~
</expected>)
        End Sub

        <Fact>
        Public Sub NameCollisionWithAddedModule_06()

            Dim source1 =
<compilation name="module">
    <file name="a.vb">
Public Class C1
End Class

Public Class c2
End Class

Namespace NS1
    Public Class C3
    End Class

    Public Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim modComp = CreateCompilationWithMscorlib40(source1, OutputKind.NetModule)
            Dim modRef = modComp.EmitToImageReference()

            Dim source2 =
<compilation>
    <file name="a.vb">
Friend Class C1
End Class

Friend Class c2
End Class

Namespace ns1
    Friend Class C3
    End Class

    Friend Class c4
    End Class
End Namespace
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source2, {modRef}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC37210: Type 'C1' conflicts with public type defined in added module 'module.netmodule'.
Friend Class C1
             ~~
BC37210: Type 'c2' conflicts with public type defined in added module 'module.netmodule'.
Friend Class c2
             ~~
</expected>)
        End Sub

        <Fact>
        Public Sub NameCollisionWithAddedModule_07()

            Dim ilSource =
            <![CDATA[
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.module ITest20Mod.netmodule
// MVID: {53AFCDC2-985A-43AE-928E-89B4A4017344}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00EC0000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi ITest20<T>
{
} // end of class ITest20
]]>

            Dim ilBytes As ImmutableArray(Of Byte) = Nothing
            Using reference = IlasmUtilities.CreateTempAssembly(ilSource.Value, prependDefaultHeader:=False)
                ilBytes = ReadFromFile(reference.Path)
            End Using

            Dim moduleRef = ModuleMetadata.CreateFromImage(ilBytes).GetReference()

            Dim source =
<compilation>
    <file name="a.vb">
Interface ITest20
End Interface
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source, {moduleRef}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
BC37211: Type 'ITest20(Of T)' exported from module 'ITest20Mod.netmodule' conflicts with type declared in primary module of this assembly.
</expected>)
        End Sub

        <Fact>
        Public Sub NameCollisionWithAddedModule_08()

            Dim ilSource =
            <![CDATA[
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.module mod_1_1.netmodule
// MVID: {98479031-F5D1-443D-AF73-CF21159C1BCF}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00D30000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi ns.c1<T>
{
} 

.class interface public abstract auto ansi c2<T>
{
} 

.class interface public abstract auto ansi ns.C3<T>
{
} 

.class interface public abstract auto ansi C4<T>
{
} 

.class interface public abstract auto ansi NS1.c5<T>
{
} 
]]>

            Dim ilBytes As ImmutableArray(Of Byte) = Nothing
            Using reference = IlasmUtilities.CreateTempAssembly(ilSource.Value, prependDefaultHeader:=False)
                ilBytes = ReadFromFile(reference.Path)
            End Using

            Dim moduleRef1 = ModuleMetadata.CreateFromImage(ilBytes).GetReference()

            Dim mod2 =
<compilation name="mod_1_2">
    <file name="a.vb">
namespace ns
    public interface c1
    end interface

    public interface c3
    end interface
end namespace

public interface c2
end interface

public interface c4
end interface

namespace ns1
    public interface c5
    end interface
end namespace
    </file>
</compilation>

            Dim source =
<compilation>
    <file name="a.vb">
    </file>
</compilation>

            Dim moduleRef2 = CreateCompilationWithMscorlib40(mod2, options:=TestOptions.ReleaseModule).EmitToImageReference()

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source, {moduleRef1, moduleRef2}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
BC37212: Type 'c2' exported from module 'mod_1_2.netmodule' conflicts with type 'c2(Of T)' exported from module 'mod_1_1.netmodule'.
BC37212: Type 'ns.c1' exported from module 'mod_1_2.netmodule' conflicts with type 'ns.c1(Of T)' exported from module 'mod_1_1.netmodule'.
</expected>)
        End Sub

        <Fact>
        Public Sub NameCollisionWithAddedModule_09()
            Dim forwardedTypesSource =
<compilation name="">
    <file name="a.vb">
Public Class CF1
End Class

namespace ns
    Public Class CF2
    End Class
End Namespace

public class CF3(Of T)
End Class
    </file>
</compilation>

            forwardedTypesSource.@name = "ForwardedTypes1"
            Dim forwardedTypes1 = CreateCompilationWithMscorlib40(forwardedTypesSource, options:=TestOptions.ReleaseDll)
            Dim forwardedTypes1Ref = New VisualBasicCompilationReference(forwardedTypes1)

            forwardedTypesSource.@name = "ForwardedTypes2"
            Dim forwardedTypes2 = CreateCompilationWithMscorlib40(forwardedTypesSource, options:=TestOptions.ReleaseDll)
            Dim forwardedTypes2Ref = New VisualBasicCompilationReference(forwardedTypes2)

            forwardedTypesSource.@name = "forwardedTypesMod"
            Dim forwardedTypesModRef = CreateCompilationWithMscorlib40(forwardedTypesSource, options:=TestOptions.ReleaseModule).EmitToImageReference()

            Dim modSource =
            <![CDATA[
.assembly extern <<TypesForWardedToAssembly>>
{
  .ver 0:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.class extern forwarder CF1
{
  .assembly extern <<TypesForWardedToAssembly>>
}
.class extern forwarder ns.CF2
{
  .assembly extern <<TypesForWardedToAssembly>>
}
.module <<ModuleName>>.netmodule
// MVID: {987C2448-14BC-48E8-BE36-D24E14D49864}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00D90000
]]>.Value

            Dim ilBytes As ImmutableArray(Of Byte) = Nothing
            Using reference = IlasmUtilities.CreateTempAssembly(modSource.Replace("<<ModuleName>>", "module1_FT1").Replace("<<TypesForWardedToAssembly>>", "ForwardedTypes1"),
                                                                       prependDefaultHeader:=False)
                ilBytes = ReadFromFile(reference.Path)
            End Using

            Dim module1_FT1_Ref = ModuleMetadata.CreateFromImage(ilBytes).GetReference()

            Using reference = IlasmUtilities.CreateTempAssembly(modSource.Replace("<<ModuleName>>", "module2_FT1").Replace("<<TypesForWardedToAssembly>>", "ForwardedTypes1"),
                                                                       prependDefaultHeader:=False)
                ilBytes = ReadFromFile(reference.Path)
            End Using

            Dim module2_FT1_Ref = ModuleMetadata.CreateFromImage(ilBytes).GetReference()

            Using reference = IlasmUtilities.CreateTempAssembly(modSource.Replace("<<ModuleName>>", "module3_FT2").Replace("<<TypesForWardedToAssembly>>", "ForwardedTypes2"),
                                                                       prependDefaultHeader:=False)
                ilBytes = ReadFromFile(reference.Path)
            End Using

            Dim module3_FT2_Ref = ModuleMetadata.CreateFromImage(ilBytes).GetReference()

            Dim module4_FT1_source =
            <![CDATA[
.assembly extern ForwardedTypes1
{
  .ver 0:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.class extern forwarder CF3`1
{
  .assembly extern ForwardedTypes1
}
.module module4_FT1.netmodule
// MVID: {5C652C9E-35F2-4D1D-B2A4-68683237D8F1}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01100000
]]>.Value

            Using reference = IlasmUtilities.CreateTempAssembly(module4_FT1_source, prependDefaultHeader:=False)
                ilBytes = ReadFromFile(reference.Path)
            End Using

            Dim module4_Ref = ModuleMetadata.CreateFromImage(ilBytes).GetReference()

            forwardedTypesSource.@name = "consumer"

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(forwardedTypesSource,
                {
                    module1_FT1_Ref,
                    forwardedTypes1Ref
                }, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
BC37217: Forwarded type 'CF1' conflicts with type declared in primary module of this assembly.
BC37217: Forwarded type 'ns.CF2' conflicts with type declared in primary module of this assembly.
</expected>)

            Dim emptySource =
<compilation>
    <file name="a.vb">
    </file>
</compilation>

            compilation = CreateCompilationWithMscorlib40AndReferences(emptySource,
                {
                    forwardedTypesModRef,
                    module1_FT1_Ref,
                    forwardedTypes1Ref
                }, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
BC37218: Type 'CF1' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'CF1' exported from module 'forwardedTypesMod.netmodule'.
BC37218: Type 'ns.CF2' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'ns.CF2' exported from module 'forwardedTypesMod.netmodule'.
</expected>)

            compilation = CreateCompilationWithMscorlib40AndReferences(emptySource,
                {
                    module1_FT1_Ref,
                    forwardedTypesModRef,
                    forwardedTypes1Ref
                }, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
BC37218: Type 'CF1' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'CF1' exported from module 'forwardedTypesMod.netmodule'.
BC37218: Type 'ns.CF2' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'ns.CF2' exported from module 'forwardedTypesMod.netmodule'.
</expected>)

            compilation = CreateCompilationWithMscorlib40AndReferences(emptySource,
                {
                    module1_FT1_Ref,
                    module2_FT1_Ref,
                    forwardedTypes1Ref
                }, TestOptions.ReleaseDll)

            ' Exported types in .NET modules cause PEVerify to fail.
            CompileAndVerify(compilation, verify:=Verification.FailsPEVerify).VerifyDiagnostics()

            compilation = CreateCompilationWithMscorlib40AndReferences(emptySource,
                {
                    module1_FT1_Ref,
                    module3_FT2_Ref,
                    forwardedTypes1Ref,
                    forwardedTypes2Ref
                }, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
BC37219: Type 'CF1' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'CF1' forwarded to assembly 'ForwardedTypes2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
BC37219: Type 'ns.CF2' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'ns.CF2' forwarded to assembly 'ForwardedTypes2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
</expected>)
        End Sub

        <Fact>
        Public Sub PartialModules()
            Dim verifier = CompileAndVerify(
<compilation name="PartialModules">
    <file name="Program.vb">
Imports System.Console

Module Program
    Sub Main()
        Write(Syntax.SyntaxFacts.IsExpressionKind(0))
        Write(Syntax.SyntaxFacts.IsStatementKind(0))
        Write(GetType(Syntax.SyntaxFacts).GetCustomAttributes(GetType(MyTestAttribute1), False).Length)
        Write(GetType(Syntax.SyntaxFacts).GetCustomAttributes(GetType(MyTestAttribute2), False).Length)
        Write(GetType(Syntax.SyntaxFacts).GetCustomAttributes(False).Length)
    End Sub
End Module        

Public Class MyTestAttribute1
    Inherits System.Attribute
End Class
Public Class MyTestAttribute2
    Inherits System.Attribute
End Class
    </file>
    <file name="SyntaxFacts.vb"><![CDATA[
Namespace Syntax
    <MyTestAttribute1>
    Module SyntaxFacts
        Public Function IsExpressionKind(kind As Integer) As Boolean
            Return False
        End Function
    End Module
End Namespace        
    ]]></file>
    <file name="GeneratedSyntaxFacts.vb"><![CDATA[
Namespace Syntax
    <MyTestAttribute2>
    Partial Module SyntaxFacts
        Public Function IsStatementKind(kind As Integer) As Boolean
            Return True
        End Function
    End Module
End Namespace
    ]]></file>
</compilation>,
expectedOutput:="FalseTrue113")

        End Sub

        <Fact>
        Public Sub PartialInterfaces()
            Dim verifier = CompileAndVerify(
<compilation name="PartialModules">
    <file name="Program.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim customer As ICustomer = New Customer() With { .Name = "Unnamed" }
        ValidateCustomer(customer)
    End Sub

    Sub ValidateCustomer(customer As ICustomer)
        Write(customer.Id = Guid.Empty)
        Write(customer.NameHash = customer.Name.GetHashCode())
        Write(GetType(ICustomer).GetCustomAttributes(GetType(MyTestAttribute1), False).Length)
        Write(GetType(ICustomer).GetCustomAttributes(GetType(MyTestAttribute2), False).Length)
        Write(GetType(ICustomer).GetCustomAttributes(False).Length)
    End Sub
End Module      
    
Public Class MyTestAttribute1
    Inherits System.Attribute
End Class
Public Class MyTestAttribute2
    Inherits System.Attribute
End Class
    </file>
    <file name="ViewModels.vb"><![CDATA[
Imports System

' We only need this property for Silverlight. We omit this file when building for WPF.
<MyTestAttribute1>
Partial Interface ICustomer
    ReadOnly Property NameHash As Integer
End Interface

Partial Class Customer

    Private ReadOnly Property NameHash As Integer Implements ICustomer.NameHash
        Get
            Return Name.GetHashCode()
        End Get
    End Property

End Class
    ]]></file>
    <file name="GeneratedViewModels.vb"><![CDATA[
Imports System

<MyTestAttribute2>
Partial Interface ICustomer
    ReadOnly Property Id As Guid
    Property Name As String
End Interface

Partial Class Customer
    Implements ICustomer

    Private ReadOnly _Id As Guid
    Public ReadOnly Property Id As Guid Implements ICustomer.Id
        Get
            return _Id
        End Get
    End Property

    Public Property Name As String Implements ICustomer.Name

    Public Sub New()
        Me.New(Guid.NewGuid())
    End Sub

    Public Sub New(id As Guid)
        _Id = id
    End Sub

End Class
    ]]></file>
</compilation>,
expectedOutput:="FalseTrue112")

        End Sub

        <Fact, WorkItem(8400, "https://github.com/dotnet/roslyn/issues/8400")>
        Public Sub WrongModifier()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="C">
    <file name="a.vb"><![CDATA[
    public class AAA : IBBB
    {
        public static AAA MMM(Stream xamlStream)
        {
            // Note: create custom module catalog 
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(
<expected>
BC30481: 'Class' statement must end with a matching 'End Class'.
public class AAA : IBBB
~~~~~~~~~~~~~~~~
BC30188: Declaration expected.
public class AAA : IBBB
                   ~~~~
BC30035: Syntax error.
    {
    ~
BC30235: 'static' is not valid on a member variable declaration.
        public static AAA MMM(Stream xamlStream)
               ~~~~~~
BC30205: End of statement expected.
        public static AAA MMM(Stream xamlStream)
                          ~~~
BC30035: Syntax error.
        {
        ~
BC30035: Syntax error.
            // Note: create custom module catalog
            ~
BC30188: Declaration expected.
            // Note: create custom module catalog
                     ~~~~~~
BC31140: 'Custom' modifier can only be used immediately before an 'Event' declaration.
            // Note: create custom module catalog
                            ~~~~~~
BC30617: 'Module' statements can occur only at file or namespace level.
            // Note: create custom module catalog
                            ~~~~~~~~~~~~~~~~~~~~~
BC30625: 'Module' statement must end with a matching 'End Module'.
            // Note: create custom module catalog
                            ~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_01()
            Dim sources =
<compilation>
    <file><![CDATA[
Imports System.Runtime.InteropServices

<typeidentifier>
Public Interface I1
End Interface
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(sources)
            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40(sources)
            i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")
            i1.GetAttributes()
            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports System.Runtime.InteropServices

<TypeIdentifierAttribute>
Public Interface I1
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_03()
            Dim sources =
<compilation>
    <file><![CDATA[
Imports alias1 = System.Runtime.InteropServices.TypeIdentifier

<alias1>
Public Interface I1
End Interface
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(sources)
            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.False(i1.IsExplicitDefinitionOfNoPiaLocalType)

            compilation.AssertTheseDeclarationDiagnostics(
<expected><![CDATA[
BC40056: Namespace or type specified in the Imports 'System.Runtime.InteropServices.TypeIdentifier' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports alias1 = System.Runtime.InteropServices.TypeIdentifier
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30182: Type expected.
<alias1>
 ~~~~~~
]]></expected>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40(sources)
            i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")
            i1.GetAttributes()
            Assert.False(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports alias1 = System.Runtime.InteropServices.TypeIdentifier

<alias1Attribute>
Public Interface I1
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.False(i1.IsExplicitDefinitionOfNoPiaLocalType)

            compilation.AssertTheseDeclarationDiagnostics(
<expected><![CDATA[
BC40056: Namespace or type specified in the Imports 'System.Runtime.InteropServices.TypeIdentifier' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports alias1 = System.Runtime.InteropServices.TypeIdentifier
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'alias1Attribute' is not defined.
<alias1Attribute>
 ~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_05()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports alias1 = System.Runtime.InteropServices.typeIdentifierattribute

<alias1>
Public Interface I1
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_06()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports alias1attribute = System.Runtime.InteropServices.typeIdentifierattribute

<Alias1>
Public Interface I1
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_07()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports alias1attribute = System.Runtime.InteropServices.typeIdentifierattribute

<Alias1Attribute>
Public Interface I1
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_08()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports alias1attributeAttribute = System.Runtime.InteropServices.typeIdentifierattribute

<Alias1Attribute>
Public Interface I1
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_09()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports alias2 = alias1

<alias2>
Public Interface I1
End Interface
    ]]></file>
</compilation>, options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("alias1=System.Runtime.InteropServices.typeIdentifierattribute")))

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.False(i1.IsExplicitDefinitionOfNoPiaLocalType)

            compilation.AssertTheseDeclarationDiagnostics(
<expected><![CDATA[
BC40056: Namespace or type specified in the Imports 'alias1' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports alias2 = alias1
                 ~~~~~~
BC30182: Type expected.
<alias2>
 ~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_10()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
<alias1>
Public Interface I1
End Interface
    ]]></file>
</compilation>, options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("alias1=System.Runtime.InteropServices.typeIdentifierattribute")))

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_11()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports alias2 = I1

<alias1>
Public Interface I1
End Interface
    ]]></file>
</compilation>, options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("alias1=System.Runtime.InteropServices.typeIdentifierattribute")))

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_12()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports alias1 = System.Runtime.InteropServices.TypeIdentifierAttribute
Imports System.Runtime.CompilerServices

<alias1>
Public Partial Interface I1
End Interface

<CompilerGenerated>
Public Partial Interface I2
End Interface
    ]]></file>
    <file><![CDATA[
Imports alias1 = System.Runtime.InteropServices.ComImportAttribute

<alias1>
Public Partial Interface I1
End Interface

<alias1>
Public Partial Interface I2
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")
            Dim i2 = compilation.SourceAssembly.GetTypeByMetadataName("I2")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
            Assert.False(i2.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_13()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports alias1 = System.Runtime.InteropServices.ComImportAttribute

<alias1>
Public Partial Interface I1
End Interface

<alias1>
Public Partial Interface I2
End Interface
    ]]></file>
    <file><![CDATA[
Imports alias1 = System.Runtime.InteropServices.TypeIdentifierAttribute
Imports System.Runtime.CompilerServices

<alias1>
Public Partial Interface I1
End Interface

<CompilerGenerated>
Public Partial Interface I2
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")
            Dim i2 = compilation.SourceAssembly.GetTypeByMetadataName("I2")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
            Assert.False(i2.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_14()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports alias1 = System.Runtime.InteropServices

<alias1>
Public Interface I1
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.False(i1.IsExplicitDefinitionOfNoPiaLocalType)

            compilation.AssertTheseDeclarationDiagnostics(
<expected><![CDATA[
BC30182: Type expected.
<alias1>
 ~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_15()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
<System.Runtime.InteropServices.TypeIdentifier>
Public Interface I1
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_16()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
<System.Runtime.InteropServices.TypeIdentifierAttribute>
Public Interface I1
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Fact>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType_17()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file><![CDATA[
Imports alias1 = System.Runtime.InteropServices.TypeIdentifierAttribute

<alias1>
Public Interface I1
End Interface
    ]]></file>
</compilation>)

            Dim i1 = compilation.SourceAssembly.GetTypeByMetadataName("I1")

            Assert.True(i1.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <WorkItem(30673, "https://github.com/dotnet/roslyn/issues/30673")>
        <Fact>
        Public Sub TypeSymbolGetHashCode_ContainingType_GenericNestedType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="TypeSymbolGetHashCode_ContainingType_GenericNestedType">
    <file name="a.vb">
Public Class C(Of T)
    Public Interface I(Of U)
    End Interface
End Class
    </file>
</compilation>)

            AssertNoDeclarationDiagnostics(compilation)
            Dim modifiers = ImmutableArray.Create(VisualBasicCustomModifier.CreateOptional(compilation.GetSpecialType(SpecialType.System_Object)))

            Dim iDefinition = compilation.GetMember(Of NamedTypeSymbol)("C.I")
            Assert.Equal("C(Of T).I(Of U)", iDefinition.ToTestDisplayString())
            Assert.True(iDefinition.IsDefinition)

            ' Construct from iDefinition with modified U from iDefinition
            Dim modifiedU = ImmutableArray.Create(New TypeWithModifiers(iDefinition.TypeParameters.Single(), modifiers))
            Dim i1 = iDefinition.Construct(TypeSubstitution.Create(iDefinition, iDefinition.TypeParameters, modifiedU))
            Assert.Equal("C(Of T).I(Of U modopt(System.Object))", i1.ToTestDisplayString())
            AssertHashCodesMatch(iDefinition, i1)

            Dim cDefinition = iDefinition.ContainingType
            Assert.Equal("C(Of T)", cDefinition.ToTestDisplayString())
            Assert.True(cDefinition.IsDefinition)

            ' Construct from cDefinition with modified T from cDefinition
            Dim modifiedT = ImmutableArray.Create(New TypeWithModifiers(cDefinition.TypeParameters.Single(), modifiers))
            Dim c2 = cDefinition.Construct(TypeSubstitution.Create(cDefinition, cDefinition.TypeParameters, modifiedT))
            Dim i2 = c2.GetTypeMember("I")
            Assert.Equal("C(Of T modopt(System.Object)).I(Of U)", i2.ToTestDisplayString())
            Assert.Same(i2.OriginalDefinition, iDefinition)
            AssertHashCodesMatch(iDefinition, i2)

            ' Construct from i2 with U from iDefinition
            Dim i2a = i2.Construct(iDefinition.TypeParameters.Single())
            Assert.Equal("C(Of T modopt(System.Object)).I(Of U)", i2a.ToTestDisplayString())
            AssertHashCodesMatch(iDefinition, i2a)

            ' Construct from i2 (reconstructed) with modified U from iDefinition
            Dim i2b = iDefinition.Construct(TypeSubstitution.Create(iDefinition,
                                                                    ImmutableArray.Create(cDefinition.TypeParameters.Single(), iDefinition.TypeParameters.Single()),
                                                                    ImmutableArray.Create(modifiedT.Single(), modifiedU.Single())))
            Assert.Equal("C(Of T modopt(System.Object)).I(Of U modopt(System.Object))", i2b.ToTestDisplayString())
            AssertHashCodesMatch(iDefinition, i2b)

            ' Construct from cDefinition with modified T from cDefinition
            Dim c4 = cDefinition.Construct(TypeSubstitution.Create(cDefinition, cDefinition.TypeParameters, modifiedT))
            Assert.Equal("C(Of T modopt(System.Object))", c4.ToTestDisplayString())
            Assert.False(c4.IsDefinition)
            AssertHashCodesMatch(cDefinition, c4)

            Dim i4 = c4.GetTypeMember("I")
            Assert.Equal("C(Of T modopt(System.Object)).I(Of U)", i4.ToTestDisplayString())
            Assert.Same(i4.OriginalDefinition, iDefinition)
            AssertHashCodesMatch(iDefinition, i4)
        End Sub

        <WorkItem(30673, "https://github.com/dotnet/roslyn/issues/30673")>
        <Fact>
        Public Sub TypeSymbolGetHashCode_ContainingType_GenericNestedType_Nested()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="TypeSymbolGetHashCode_ContainingType_GenericNestedType">
    <file name="a.vb">
Public Class C(Of T)
    Public Class C2(Of U)
        Public Interface I(Of V)
        End Interface
    End Class
End Class
    </file>
</compilation>)

            AssertNoDeclarationDiagnostics(compilation)
            Dim modifiers = ImmutableArray.Create(VisualBasicCustomModifier.CreateOptional(compilation.GetSpecialType(SpecialType.System_Object)))

            Dim iDefinition = compilation.GetMember(Of NamedTypeSymbol)("C.C2.I")
            Assert.Equal("C(Of T).C2(Of U).I(Of V)", iDefinition.ToTestDisplayString())
            Assert.True(iDefinition.IsDefinition)

            Dim c2Definition = iDefinition.ContainingType
            Dim cDefinition = c2Definition.ContainingType
            Dim modifiedT = New TypeWithModifiers(cDefinition.TypeParameters.Single(), modifiers)
            Dim modifiedU = New TypeWithModifiers(c2Definition.TypeParameters.Single(), modifiers)
            Dim modifiedV = New TypeWithModifiers(iDefinition.TypeParameters.Single(), modifiers)

            Dim i = iDefinition.Construct(TypeSubstitution.Create(iDefinition,
                                                                    ImmutableArray.Create(cDefinition.TypeParameters.Single(), c2Definition.TypeParameters.Single(), iDefinition.TypeParameters.Single()),
                                                                    ImmutableArray.Create(modifiedT, modifiedU, modifiedV)))
            Assert.Equal("C(Of T modopt(System.Object)).C2(Of U modopt(System.Object)).I(Of V modopt(System.Object))", i.ToTestDisplayString())
            AssertHashCodesMatch(iDefinition, i)
        End Sub

        <WorkItem(30673, "https://github.com/dotnet/roslyn/issues/30673")>
        <Fact>
        Public Sub TypeSymbolGetHashCode_SubstitutedErrorType()
            Dim missing = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="TypeSymbolGetHashCode_SubstitutedErrorType">
    <file name="a.vb">
Public Class C(Of T)
    Public Class D(Of U)
    End Class
End Class
    </file>
</compilation>)
            AssertNoDeclarationDiagnostics(missing)

            Dim reference = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="TypeSymbolGetHashCode_SubstitutedErrorType">
    <file name="a.vb">
Public Class Reference(Of T, U)
    Inherits C(Of T).D(Of U)
End Class
    </file>
</compilation>, references:={missing.EmitToImageReference()})
            AssertNoDeclarationDiagnostics(reference)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="TypeSymbolGetHashCode_SubstitutedErrorType">
    <file name="a.vb">
Public Class Program(Of V, W)
    Inherits Reference(Of V, W)
End Class
    </file>
</compilation>, references:={reference.EmitToImageReference()})

            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31091: Import of type 'C(Of ).D(Of )' from assembly or module 'TypeSymbolGetHashCode_SubstitutedErrorType.dll' failed.
Public Class Program(Of V, W)
             ~~~~~~~
BC31091: Import of type 'C(Of ).D(Of )' from assembly or module 'TypeSymbolGetHashCode_SubstitutedErrorType.dll' failed.
    Inherits Reference(Of V, W)
             ~~~~~~~~~~~~~~~~~~
            ]]></errors>)

            Dim modifiers = ImmutableArray.Create(VisualBasicCustomModifier.CreateOptional(compilation.GetSpecialType(SpecialType.System_Object)))

            Dim programType = compilation.GlobalNamespace.GetTypeMember("Program")
            Dim errorType = programType.BaseType.BaseType

            Dim definition = errorType.OriginalDefinition
            Assert.Equal("Microsoft.CodeAnalysis.VisualBasic.Symbols.SubstitutedErrorType", errorType.GetType().ToString())
            Assert.Equal("C(Of )[missing].D(Of )[missing]", definition.ToTestDisplayString())
            Assert.True(definition.IsDefinition)

            ' Construct from definition with modified U from definition
            Dim modifiedU = ImmutableArray.Create(New TypeWithModifiers(definition.TypeParameters.Single(), modifiers))
            Dim t1 = definition.Construct(TypeSubstitution.Create(definition, definition.TypeParameters, modifiedU))
            Assert.Equal("C(Of )[missing].D(Of  modopt(System.Object))[missing]", t1.ToTestDisplayString())
            AssertHashCodesMatch(definition, t1)
        End Sub

        Private Shared Sub AssertHashCodesMatch(c As NamedTypeSymbol, c2 As NamedTypeSymbol)
            Assert.False(c.IsSameType(c2, TypeCompareKind.ConsiderEverything))
            Assert.True(c.IsSameType(c2, (TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds Or TypeCompareKind.IgnoreTupleNames)))
            Assert.False(c2.IsSameType(c, TypeCompareKind.ConsiderEverything))
            Assert.True(c2.IsSameType(c, (TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds Or TypeCompareKind.IgnoreTupleNames)))

            Assert.Equal(c2.GetHashCode(), c.GetHashCode())

            If c.Arity <> 0 Then
                Dim ctp = c.TypeParameters(0)
                Dim ctp2 = c2.TypeParameters(0)

                If ctp IsNot ctp2 Then
                    Assert.False(ctp.IsSameType(ctp2, TypeCompareKind.ConsiderEverything))
                    Assert.True(ctp.IsSameType(ctp2, (TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds Or TypeCompareKind.IgnoreTupleNames)))

                    Assert.False(ctp2.IsSameType(ctp, TypeCompareKind.ConsiderEverything))
                    Assert.True(ctp2.IsSameType(ctp, (TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds Or TypeCompareKind.IgnoreTupleNames)))

                    Assert.Equal(ctp2.GetHashCode(), ctp.GetHashCode())
                End If
            End If
        End Sub
    End Class

End Namespace
