' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class FieldTests
        Inherits BasicTestBase

        <Fact>
        Public Sub SimpleFields()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="AAA">
    <file name="a.vb">
Public Structure C
    Shared ch1, ch2 as Char
End Structure
    </file>
</compilation>)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim structC = DirectCast(globalNS.GetMembers().Single(), NamedTypeSymbol)
            Dim field1 = DirectCast(structC.GetMembers()(1), FieldSymbol)
            Dim field2 = DirectCast(structC.GetMembers()(2), FieldSymbol)

            Assert.Same(structC, field1.ContainingSymbol)
            Assert.Same(structC, field2.ContainingType)

            Assert.Equal("ch1", field1.Name)
            Assert.Equal("C.ch2 As System.Char", field2.ToTestDisplayString())
            Assert.False(field1.IsMustOverride)
            Assert.False(field1.IsNotOverridable)
            Assert.False(field2.IsOverrides)
            Assert.False(field2.IsOverridable)
            Assert.Equal(0, field2.CustomModifiers.Length)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub


        <Fact>
        Public Sub Fields1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="C">
    <file name="a.vb">
Public Partial Class C
    Public Shared p?, q as Char, t%
    Protected Friend u@()
    Friend Shared v(,)() as Object
End Class
    </file>
    <file name="b.vb">
Public Partial Class C
    Protected s As Long

    ReadOnly r 

    Private Class D
        Shared Friend l As UInteger = 5
    End Class
End Class
    </file>
</compilation>)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers()
            Assert.Equal(1, globalNSmembers.Length)
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Dim membersOfC = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Assert.Equal(9, membersOfC.Length)

            Dim classD = DirectCast(membersOfC(1), NamedTypeSymbol)
            Assert.Equal("D", classD.Name)
            Assert.Equal(TypeKind.Class, classD.TypeKind)

            Dim fieldP = DirectCast(membersOfC(2), FieldSymbol)
            Assert.Same(classC, fieldP.ContainingSymbol)
            Assert.Same(classC, fieldP.ContainingType)
            Assert.Equal("p", fieldP.Name)
            Assert.Equal(Accessibility.Public, fieldP.DeclaredAccessibility)
            Assert.True(fieldP.IsShared)
            Assert.False(fieldP.IsReadOnly)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Nullable_T), fieldP.Type.OriginalDefinition)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Char), DirectCast(fieldP.Type, NamedTypeSymbol).TypeArguments(0))

            Dim fieldQ = DirectCast(membersOfC(3), FieldSymbol)
            Assert.Equal("q", fieldQ.Name)
            Assert.Equal(Accessibility.Public, fieldQ.DeclaredAccessibility)
            Assert.True(fieldQ.IsShared)
            Assert.False(fieldQ.IsReadOnly)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Char), fieldQ.Type)

            Dim fieldR = DirectCast(membersOfC(4), FieldSymbol)
            Assert.Equal("r", fieldR.Name)
            Assert.Equal(Accessibility.Private, fieldR.DeclaredAccessibility)
            Assert.False(fieldR.IsShared)
            Assert.True(fieldR.IsReadOnly)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Object), fieldR.Type)

            Dim fieldS = DirectCast(membersOfC(5), FieldSymbol)
            Assert.Equal("s", fieldS.Name)
            Assert.Equal(Accessibility.Protected, fieldS.DeclaredAccessibility)
            Assert.False(fieldS.IsShared)
            Assert.False(fieldS.IsReadOnly)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Int64), fieldS.Type)

            Dim fieldT = DirectCast(membersOfC(6), FieldSymbol)
            Assert.Equal("t", fieldT.Name)
            Assert.Equal(Accessibility.Public, fieldT.DeclaredAccessibility)
            Assert.True(fieldT.IsShared)
            Assert.False(fieldT.IsReadOnly)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Int32), fieldT.Type)

            Dim fieldU = DirectCast(membersOfC(7), FieldSymbol)
            Assert.Equal("u", fieldU.Name)
            Assert.Equal(Accessibility.ProtectedOrFriend, fieldU.DeclaredAccessibility)
            Assert.False(fieldU.IsShared)
            Assert.False(fieldU.IsReadOnly)
            Assert.Equal(TypeKind.Array, fieldU.Type.TypeKind)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Decimal), DirectCast(fieldU.Type, ArrayTypeSymbol).ElementType)
            Assert.Equal(1, DirectCast(fieldU.Type, ArrayTypeSymbol).Rank)

            Dim fieldV = DirectCast(membersOfC(8), FieldSymbol)
            Assert.Equal("v", fieldV.Name)
            Assert.Equal(Accessibility.Friend, fieldV.DeclaredAccessibility)
            Assert.True(fieldV.IsShared)
            Assert.False(fieldV.IsReadOnly)
            Assert.Equal(TypeKind.Array, fieldV.Type.TypeKind)  ' v is a 2d array of a 1d array.
            Assert.Equal(2, DirectCast(fieldV.Type, ArrayTypeSymbol).Rank)
            Assert.Equal(1, DirectCast(DirectCast(fieldV.Type, ArrayTypeSymbol).ElementType, ArrayTypeSymbol).Rank)

            Dim membersOfD = classD.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Assert.Equal(3, membersOfD.Length)

            Dim fieldL = DirectCast(membersOfD(2), FieldSymbol)
            Assert.Same(classD, fieldL.ContainingSymbol)
            Assert.Same(classD, fieldL.ContainingType)
            Assert.Equal("l", fieldL.Name)
            Assert.Equal(Accessibility.Friend, fieldL.DeclaredAccessibility)
            Assert.True(fieldL.IsShared)
            Assert.False(fieldL.IsReadOnly)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_UInt32), fieldL.Type)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <WorkItem(537491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537491")>
        <Fact>
        Public Sub ImplicitTypedFields01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Field">
    <file name="a.vb">
'Imports statements should go here
Imports System

Namespace ConstInit02
    Class C
        Const C1 = 2, C2 = 4, C3 = -1

        Public Const ImplChar = "c"c
        Private Const ImplString = "Microsoft"
        Protected Const ImplShort As Short = 32767S
        Friend Const ImplInteger = 123%
        Const ImplLong = 12345678910&amp;
        Friend Protected Const ImplDouble = 1234.1234#
        Public Const ImplSingle = 1234.1234!
        Public Const ImplDecimal = 1234.456@
    End Class
End namespace
    </file>
</compilation>)
            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim ns = DirectCast(globalNS.GetMembers("ConstInit02").Single(), NamespaceSymbol)
            Dim type1 = DirectCast(ns.GetTypeMembers("C").Single(), NamedTypeSymbol)
            ' 11 field + ctor (cctor for decimal is not part of members list)
            Dim m = type1.GetMembers()
            Assert.Equal(12, type1.GetMembers().Length)

            Dim fieldC = DirectCast(type1.GetMembers("C2").Single(), FieldSymbol)
            Assert.Same(type1, fieldC.ContainingSymbol)
            Assert.Same(type1, fieldC.ContainingType)
            Assert.Equal("C2", fieldC.Name)
            Assert.Equal(Accessibility.Private, fieldC.DeclaredAccessibility)
            Assert.Equal("Int32", fieldC.Type.Name)

            Dim field1 = DirectCast(type1.GetMembers("ImplChar").Single(), FieldSymbol)
            Assert.Equal("char", field1.Type.Name.ToLowerInvariant())
            Dim field2 = DirectCast(type1.GetMembers("ImplString").Single(), FieldSymbol)
            Assert.Equal("string", field2.Type.Name.ToLowerInvariant())
            Dim field3 = DirectCast(type1.GetMembers("ImplShort").Single(), FieldSymbol)
            Assert.Equal("int16", field3.Type.Name.ToLowerInvariant())
            Dim field4 = DirectCast(type1.GetMembers("ImplInteger").Single(), FieldSymbol)
            Assert.Equal("int32", field4.Type.Name.ToLowerInvariant())
            Dim field5 = DirectCast(type1.GetMembers("ImplLong").Single(), FieldSymbol)
            Assert.Equal("int64", field5.Type.Name.ToLowerInvariant())
            Dim field6 = DirectCast(type1.GetMembers("ImplDouble").Single(), FieldSymbol)
            Assert.Equal("double", field6.Type.Name.ToLowerInvariant())
            Dim field7 = DirectCast(type1.GetMembers("ImplSingle").Single(), FieldSymbol)
            Assert.Equal("single", field7.Type.Name.ToLowerInvariant())
            Dim field8 = DirectCast(type1.GetMembers("ImplDecimal").Single(), FieldSymbol)
            Assert.Equal("decimal", field8.Type.Name.ToLowerInvariant())

        End Sub

        <Fact>
        Public Sub Bug4993()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="AAA">
    <file name="a.vb">
Option Infer Off
Option Strict On
Public Class Class1
   Private Const LOCAL_SIZE = 1

    Sub Test()
        Const thisIsAConst = 1
        Dim y As Object = thisIsAConst
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
   Private Const LOCAL_SIZE = 1
                 ~~~~~~~~~~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Const thisIsAConst = 1
              ~~~~~~~~~~~~ 
</expected>)
        End Sub

        <Fact>
        Public Sub Bug4993_related_StrictOn()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="AAA">
    <file name="a.vb">
Option Strict On
Public Class Class1
   Private Const LOCAL_SIZE = 1
   Private Const LOCAL_SIZE_2 as object = 1

    Sub Test()
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub Bug4993_related_StrictOff()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="AAA">
    <file name="a.vb">
Option Strict Off
Public Class Class1
   Private Const LOCAL_SIZE = 1
   Private Const LOCAL_SIZE_2 as object = 1

    Sub Test()
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub ConstFieldWithoutValueErr()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
 <compilation name="ConstFieldWithoutValueErr">
     <file name="a.vb">
Public Class C
   Const x As Integer
End Class
    </file>
 </compilation>)

            Dim type1 = DirectCast(compilation.SourceModule.GlobalNamespace.GetMembers("C").Single(), NamedTypeSymbol)
            Dim mem = DirectCast(type1.GetMembers("x").Single(), FieldSymbol)
            Assert.Equal("x", mem.Name)
            Assert.True(mem.IsConst)
            Assert.False(mem.HasConstantValue)
            Assert.Equal(Nothing, mem.ConstantValue)
        End Sub

        <Fact>
        Public Sub Bug9902_NoValuesForConstField()

            Dim expectedErrors() As XElement = {
<errors>
BC30438: Constants must have a value.
    Private Const Field1 As Integer
                  ~~~~~~
BC30438: Constants must have a value.
    Private Const Field2
                  ~~~~~~
</errors>,
<errors>
BC30438: Constants must have a value.
    Private Const Field1 As Integer
                  ~~~~~~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
    Private Const Field2
                  ~~~~~~
BC30438: Constants must have a value.
    Private Const Field2
                  ~~~~~~
</errors>,
<errors>
BC30438: Constants must have a value.
    Private Const Field1 As Integer
                  ~~~~~~
BC30438: Constants must have a value.
    Private Const Field2
                  ~~~~~~
</errors>,
<errors>
BC30438: Constants must have a value.
    Private Const Field1 As Integer
                  ~~~~~~
BC30438: Constants must have a value.
    Private Const Field2
                  ~~~~~~
</errors>
            }

            Dim index = 0
            For Each optionStrict In {"On", "Off"}
                For Each infer In {"On", "Off"}
                    Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
        <compilation name="AAA">
            <file name="a.vb">
Option Strict <%= optionStrict %>
Option Infer <%= infer %>

Public Class Class1
    Private Const Field1 As Integer
    Private Const Field2

    Public Sub Main()
    End Sub    
End Class
    </file>
        </compilation>)

                    CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors(index)
        )
                    index += 1
                Next
            Next
        End Sub

        <Fact>
        Public Sub Bug9902_ValuesForConstField()

            Dim expectedErrors() As XElement = {
<errors>
</errors>,
<errors>
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
    Private Const Field2 = 42
                  ~~~~~~
</errors>,
<errors>
</errors>,
<errors>
</errors>
            }

            Dim index = 0
            For Each optionStrict In {"On", "Off"}
                For Each infer In {"On", "Off"}
                    Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
        <compilation name="AAA">
            <file name="a.vb">
Option Strict <%= optionStrict %>
Option Infer <%= infer %>

Public Class Class1
    Private Const Field1 As Object = 23
    Private Const Field2 = 42

    Public Sub Main()
    End Sub    
End Class
    </file>
        </compilation>)

                    CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors(index)
        )
                    index += 1
                Next
            Next
        End Sub

        <WorkItem(543689, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543689")>
        <Fact()>
        Public Sub TestReadonlyFieldAccessWithoutQualifyingInstance()
            Dim vbCompilation = CreateVisualBasicCompilation("TestReadonlyFieldAccessWithoutQualifyingInstance",
            <![CDATA[
Class Outer
    Public ReadOnly field As Integer
    Class Inner
        Sub New(ByVal value As Integer)
            value = field
        End Sub
    End Class
End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            vbCompilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_ObjectReferenceNotSupplied, "field"))
        End Sub

        ''' <summary>
        ''' Fields named "value__" should be marked rtspecialname.
        ''' </summary>
        <WorkItem(546185, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546185")>
        <WorkItem(6190, "https://github.com/dotnet/roslyn/issues/6190")>
        <Fact>
        Public Sub RTSpecialName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Private value__ As Object = Nothing
End Class
Class B
    Private VALUE__ As Object = Nothing
End Class
Class C
    Sub value__()
    End Sub
End Class
Class D
    Property value__ As Object
End Class
Class E
    Event value__ As System.Action
End Class
Class F
    Interface value__
    End Interface
End Class
Class G
    Class value__
    End Class
End Class
Module M
    Function F() As System.Action(Of Object)
        Dim value__ As Object = Nothing
        Return Function(v) value__ = v
    End Function
End Module
   ]]></file>
</compilation>)
            compilation.AssertNoErrors()

            ' PEVerify should not report "Field value__ ... is not marked RTSpecialName".
            Dim verifier = New CompilationVerifier(Me, compilation)
            verifier.EmitAndVerify(
                "Error: Field name value__ is reserved for Enums only.")
        End Sub

        <Fact>
        Public Sub MultipleFieldsWithBadType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="AAA">
    <file name="a.vb">
Public Class C
    Public x, y, z as abcDef
End Class
    </file>
</compilation>)


            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30002: Type 'abcDef' is not defined.
    Public x, y, z as abcDef
                      ~~~~~~
                                                            </expected>)
        End Sub

        <Fact>
        Public Sub AssociatedSymbolOfSubstitutedField()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="AAA">
    <file name="a.vb">
Public Class C(Of T)
    Public Property P As Integer
End Class
    </file>
</compilation>)

            Dim type = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Dim [property] = type.GetMember(Of PropertySymbol)("P")
            Dim field = [property].AssociatedField
            Assert.Equal([property], field.AssociatedSymbol)

            Dim substitutedType = type.Construct(compilation.GetSpecialType(SpecialType.System_Int32))
            Dim substitutedProperty = substitutedType.GetMember(Of PropertySymbol)("P")
            Dim substitutedField = substitutedProperty.AssociatedField
            Assert.IsType(Of SubstitutedFieldSymbol)(substitutedField)
            Assert.Equal(substitutedProperty, substitutedField.AssociatedSymbol)
        End Sub


    End Class
End Namespace
