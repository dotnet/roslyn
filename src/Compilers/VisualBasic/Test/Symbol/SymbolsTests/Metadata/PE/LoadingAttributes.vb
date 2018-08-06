' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class LoadingCustomAttributes
        Inherits BasicTestBase

        <Fact>
        Public Sub TestDecodeCustomAttributeType()
            Dim text = <compilation>
                           <file name="a.vb">
                               <![CDATA[
Imports System.Runtime.InteropServices

<CoClass(GetType(Integer))>
Public Interface IT
    Sub M()
End Interface
]]>
                           </file>
                       </compilation>
            Dim comp1 = CreateEmptyCompilationWithReferences(text, references:={MscorlibRef_v20})
            Dim ref1 = comp1.EmitToImageReference()
            Dim text2 =
<compilation>
    <file name="a.vb">
        <![CDATA[
Public Class C
    Implements IT
    Sub M() Implements IT.M
    End Sub
End Class]]>
    </file>
</compilation>

            Dim comp2 = CreateEmptyCompilationWithReferences(text2, references:={MscorlibRef_v4_0_30316_17626, ref1})

            Dim it = comp2.SourceModule.GlobalNamespace.GetTypeMember("C").Interfaces.Single()
            Assert.False(it.CoClassType.IsErrorType())

            ' Test retargeting symbols by using the compilation itself as a reference
            Dim comp3 = CreateEmptyCompilationWithReferences(text2, references:={MscorlibRef_v4_0_30316_17626, comp1.ToMetadataReference()})
            Dim it2 = comp3.SourceModule.GlobalNamespace.GetTypeMember("C").Interfaces.Single()
            Assert.Same(comp3.SourceModule.GetReferencedAssemblySymbols()(0), it2.CoClassType.ContainingAssembly)
            Assert.False(it2.CoClassType.IsErrorType())
        End Sub

        <Fact>
        Public Sub TestAssemblyAttributes()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {
                                        TestResources.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                                        TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib,
                                        TestResources.NetFX.v4_0_21006.mscorlib
                                    })

            Dim assembly0 = assemblies(0)
            Dim assembly1 = assemblies(1)

            '<Assembly:ABoolean(True)> 
            '<Assembly:AByte(1)> 
            '<Assembly:AChar("a"c)>
            '<Assembly:ADouble(3.1415926)> 
            '<Assembly:AInt16(16)> 
            '<Assembly:AInt32(32)> 
            '<Assembly:AInt64(64)>
            '<Assembly:AObject("object")> 
            '<Assembly:ASingle(3.14159)> 
            '<Assembly:AString("assembly")> 
            '<Assembly:AType(GetType(String))>

            Dim aBoolClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("ABooleanAttribute"), NamedTypeSymbol)
            Dim aByteClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AByteAttribute"), NamedTypeSymbol)
            Dim aCharClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("ACharAttribute"), NamedTypeSymbol)
            Dim aSingleClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("ASingleAttribute"), NamedTypeSymbol)
            Dim aDoubleClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("ADoubleAttribute"), NamedTypeSymbol)
            Dim aInt16Class = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AInt16Attribute"), NamedTypeSymbol)
            Dim aInt32Class = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AInt32Attribute"), NamedTypeSymbol)
            Dim aInt64Class = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AInt64Attribute"), NamedTypeSymbol)
            Dim aObjectClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AObjectAttribute"), NamedTypeSymbol)
            Dim aStringClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AStringAttribute"), NamedTypeSymbol)
            Dim aTypeClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("ATypeAttribute"), NamedTypeSymbol)

            ' Check attributes on assembly
            Dim aBoolInst = assembly0.GetAttribute(aBoolClass)
            Assert.Equal(True, aBoolInst.CommonConstructorArguments.Single().Value)

            Dim aByteInst = assembly0.GetAttribute(aByteClass)
            Assert.Equal(CByte(1), aByteInst.CommonConstructorArguments.Single().Value)

            Dim aCharInst = assembly0.GetAttribute(aCharClass)
            Assert.Equal("a"c, aCharInst.CommonConstructorArguments.Single().Value)

            Dim aSingleInst = assembly0.GetAttribute(aSingleClass)
            Assert.Equal(3.14159F, aSingleInst.CommonConstructorArguments.Single().Value)

            Dim aDoubleInst = assembly0.GetAttribute(aDoubleClass)
            Assert.Equal(3.1415926, aDoubleInst.CommonConstructorArguments.Single().Value)

            Dim aInt16Inst = assembly0.GetAttribute(aInt16Class)
            Assert.Equal(16S, aInt16Inst.CommonConstructorArguments.Single().Value)

            Dim aInt32Inst = assembly0.GetAttribute(aInt32Class)
            Assert.Equal(32, aInt32Inst.CommonConstructorArguments.Single().Value)

            Dim aInt64Inst = assembly0.GetAttribute(aInt64Class)
            Assert.Equal(64L, aInt64Inst.CommonConstructorArguments.Single().Value)

            Dim aObjectInst = assembly0.GetAttribute(aObjectClass)
            Assert.Equal("object", aObjectInst.CommonConstructorArguments.Single().Value)

            Dim aStringInst = assembly0.GetAttribute(aStringClass)
            Assert.Equal("assembly", aStringInst.CommonConstructorArguments.Single().Value)

            Dim aTypeInst = assembly0.GetAttribute(aTypeClass)
            Assert.Equal("System.String", CType(aTypeInst.CommonConstructorArguments.Single().Value, Symbol).ToDisplayString(SymbolDisplayFormat.TestFormat))
        End Sub

        <Fact>
        Public Sub TestModuleAttributes()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                {
                    TestResources.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                    TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib,
                    TestResources.NetFX.v4_0_21006.mscorlib
                })

            Dim assembly1 = assemblies(1)
            Dim module0 = assemblies(0).Modules(0)

            '<Module:AString("module")>
            '<Module:ABoolean(True)>
            '<Module:AByte(1)>
            '<Module:AChar("a"c)>
            '<Module:ADouble(3.1415926)>
            '<Module:AInt16(16)>
            '<Module:AInt32(32)>
            '<Module:AInt64(64)>
            '<Module:AObject("object")>
            '<Module:ASingle(3.14159)>
            '<Module:AType(GetType(String))>

            Dim aBoolClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("ABooleanAttribute"), NamedTypeSymbol)
            Dim aByteClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AByteAttribute"), NamedTypeSymbol)
            Dim aCharClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("ACharAttribute"), NamedTypeSymbol)
            Dim aSingleClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("ASingleAttribute"), NamedTypeSymbol)
            Dim aDoubleClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("ADoubleAttribute"), NamedTypeSymbol)
            Dim aInt16Class = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AInt16Attribute"), NamedTypeSymbol)
            Dim aInt32Class = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AInt32Attribute"), NamedTypeSymbol)
            Dim aInt64Class = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AInt64Attribute"), NamedTypeSymbol)
            Dim aObjectClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AObjectAttribute"), NamedTypeSymbol)
            Dim aStringClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("AStringAttribute"), NamedTypeSymbol)
            Dim aTypeClass = TryCast(assembly1.Modules(0).GlobalNamespace.GetMember("ATypeAttribute"), NamedTypeSymbol)

            ' Check attributes on module
            Dim aBoolInst = module0.GetAttribute(aBoolClass)
            Assert.Equal(True, aBoolInst.CommonConstructorArguments.Single().Value)

            Dim aByteInst = module0.GetAttribute(aByteClass)
            Assert.Equal(CByte(1), aByteInst.CommonConstructorArguments.Single().Value)

            Dim aCharInst = module0.GetAttribute(aCharClass)
            Assert.Equal("a"c, aCharInst.CommonConstructorArguments.Single().Value)

            Dim aSingleInst = module0.GetAttribute(aSingleClass)
            Assert.Equal(3.14159F, aSingleInst.CommonConstructorArguments.Single().Value)

            Dim aDoubleInst = module0.GetAttribute(aDoubleClass)
            Assert.Equal(3.1415926, aDoubleInst.CommonConstructorArguments.Single().Value)

            Dim aInt16Inst = module0.GetAttribute(aInt16Class)
            Assert.Equal(16S, aInt16Inst.CommonConstructorArguments.Single().Value)

            Dim aInt32Inst = module0.GetAttribute(aInt32Class)
            Assert.Equal(32, aInt32Inst.CommonConstructorArguments.Single().Value)

            Dim aInt64Inst = module0.GetAttribute(aInt64Class)
            Assert.Equal(64L, aInt64Inst.CommonConstructorArguments.Single().Value)

            Dim aObjectInst = module0.GetAttribute(aObjectClass)
            Assert.Equal("object", aObjectInst.CommonConstructorArguments.Single().Value)

            Dim aStringInst = module0.GetAttribute(aStringClass)
            Assert.Equal("module", aStringInst.CommonConstructorArguments.Single().Value)

            Dim aTypeInst = module0.GetAttribute(aTypeClass)
            Assert.Equal("System.String", CType(aTypeInst.CommonConstructorArguments.Single().Value, Symbol).ToDisplayString(SymbolDisplayFormat.TestFormat))

        End Sub

        <Fact>
        Public Sub TestAttributesOnClassAndMembers()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {
                                        TestResources.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                                        TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib,
                                        TestResources.NetFX.v4_0_21006.mscorlib
                                    })

            '<AString("C1")>
            'Public Class C1
            '    <AString("InnerC1")>
            '    Public Class InnerC1(of t1)
            '        <AString("InnerC2")>
            '        Public class InnerC2(of s1, s2)
            '        End class
            '    End Class

            '    <AString("field1")>
            '    Public field1 As integer

            '    <AString("Property1")>
            '    Public Property Property1 As Integer

            '    <AString("Sub1")>
            '    Public Sub Sub1(<AString("p1")> p1 As Integer)
            '    End Sub

            '    <AString("Function1")>
            '    Public Function Function1(<AString("p1")> p1 As Integer) As <AString("Integer")> Integer
            '        Return 0
            '    End Function
            'End Class

            Dim c1 = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMember("C1"), NamedTypeSymbol)
            Dim topLevel = DirectCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("TopLevelClass"), NamedTypeSymbol)
            Dim aNestedAttribute = DirectCast(topLevel.GetMember("ANestedAttribute"), NamedTypeSymbol)

            Assert.Equal("C1", c1.GetAttributes.First().CommonConstructorArguments.Single().Value)

            Dim innerC1 = c1.GetTypeMembers("InnerC1").Single()
            Assert.Equal("InnerC1", innerC1.GetAttributes.First().CommonConstructorArguments.Single().Value)

            '<TopLevelClass.ANested(True)> 
            'Public Class InnerC1(of t1)
            Assert.Equal(aNestedAttribute, DirectCast(innerC1.GetAttributes(aNestedAttribute).Single(), VisualBasicAttributeData).AttributeClass)

            Dim innerC2 = innerC1.GetTypeMembers("InnerC2").Single()
            Assert.Equal("InnerC2", innerC2.GetAttributes.First().CommonConstructorArguments.Single().Value)

            Dim field1 = DirectCast(c1.GetMember("field1"), FieldSymbol)
            Assert.Equal("field1", field1.GetAttributes.First().CommonConstructorArguments.Single().Value)

            Dim property1 = DirectCast(c1.GetMember("Property1"), PropertySymbol)
            Assert.Equal("Property1", property1.GetAttributes.First().CommonConstructorArguments.Single().Value)

            Dim sub1 = DirectCast(c1.GetMember("Sub1"), MethodSymbol)
            Assert.Equal("Sub1", sub1.GetAttributes.First().CommonConstructorArguments.Single().Value)

            Dim sub1P1 = sub1.Parameters().Where(Function(p) p.Name = "p1").Single()
            Assert.Equal("p1", sub1P1.GetAttributes.First().CommonConstructorArguments.Single().Value)

            Dim function1 = DirectCast(c1.GetMember("Function1"), MethodSymbol)
            Assert.Equal("Function1", function1.GetAttributes.First().CommonConstructorArguments.Single().Value)

            Dim function1P1 = function1.Parameters().Where(Function(p) p.Name = "p1").Single()
            Assert.Equal("p1", function1P1.GetAttributes.First().CommonConstructorArguments.Single().Value)

        End Sub

        <Fact>
        Public Sub TestNamedAttributes()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {
                                        TestResources.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                                        TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib,
                                        TestResources.NetFX.v4_0_21006.mscorlib
                                    })

            Dim aBoolClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("ABooleanAttribute"), NamedTypeSymbol)
            Dim aByteClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AByteAttribute"), NamedTypeSymbol)
            Dim aCharClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("ACharAttribute"), NamedTypeSymbol)
            Dim aEnumClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AEnumAttribute"), NamedTypeSymbol)
            Dim aSingleClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("ASingleAttribute"), NamedTypeSymbol)
            Dim aDoubleClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("ADoubleAttribute"), NamedTypeSymbol)
            Dim aInt16Class = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AInt16Attribute"), NamedTypeSymbol)
            Dim aInt32Class = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AInt32Attribute"), NamedTypeSymbol)
            Dim aInt64Class = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AInt64Attribute"), NamedTypeSymbol)
            Dim aObjectClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AObjectAttribute"), NamedTypeSymbol)
            Dim aStringClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AStringAttribute"), NamedTypeSymbol)
            Dim aTypeClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("ATypeAttribute"), NamedTypeSymbol)

            Dim c3 = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMember("C3"), NamedTypeSymbol)

            '<ABoolean(False, B:=True)> <AByte(0, B:=1)> 
            '<AChar("a"c, C:="b"c)>
            '<AEnum(TestAttributeEnum.No, e := TestAttributeEnum.Yes) 
            '<AInt16(16, I:=16)> <AInt32(32, I:=32)>
            '<AInt64(64, I:=64)> <ASingle(3.1459, S:=3.14159)> 
            '<ADouble(3.1415926, D:=3.1415926)> 
            '<AString("hello", S:="world")> 
            '<AType(GetType(C1), T:=GetType(C3))>

            ' Check named value on attributes on c3
            Dim a = c3.GetAttribute(aBoolClass)
            Dim kv = a.CommonNamedArguments.Single()
            Assert.Equal("B", kv.Key)
            Assert.Equal(True, kv.Value.Value)

            a = c3.GetAttribute(aByteClass)
            kv = a.CommonNamedArguments.Single()
            Assert.Equal("B", kv.Key)
            Assert.Equal(CByte(1), kv.Value.Value)

            a = c3.GetAttribute(aCharClass)
            kv = a.CommonNamedArguments.Single()
            Assert.Equal("C", kv.Key)
            Assert.Equal("b"c, kv.Value.Value)

            a = c3.GetAttribute(aEnumClass)
            kv = a.CommonNamedArguments.Single()
            Assert.Equal("E", kv.Key)
            Assert.Equal(0, CType(kv.Value.Value, Integer))

            a = c3.GetAttribute(aSingleClass)
            kv = a.CommonNamedArguments.Single()
            Assert.Equal("S", kv.Key)
            Assert.Equal(3.14159F, kv.Value.Value)

            a = c3.GetAttribute(aDoubleClass)
            kv = a.CommonNamedArguments.Single()
            Assert.Equal("D", kv.Key)
            Assert.Equal(3.1415926, kv.Value.Value)

            a = c3.GetAttribute(aInt16Class)
            kv = a.CommonNamedArguments.Single()
            Assert.Equal("I", kv.Key)
            Assert.Equal(16S, kv.Value.Value)

            a = c3.GetAttribute(aInt32Class)
            kv = a.CommonNamedArguments.Single()
            Assert.Equal("I", kv.Key)
            Assert.Equal(32, kv.Value.Value)

            a = c3.GetAttribute(aInt64Class)
            kv = a.CommonNamedArguments.Single()
            Assert.Equal("I", kv.Key)
            Assert.Equal(64L, kv.Value.Value)

            a = c3.GetAttribute(aTypeClass)
            kv = a.CommonNamedArguments.Single()
            Assert.Equal("T", kv.Key)
            Assert.Equal(c3, kv.Value.Value)
        End Sub

        <Fact>
        Public Sub TestNamedAttributesWithArrays()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
            {
                TestResources.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib,
                TestResources.NetFX.v4_0_21006.mscorlib
            })

            Dim aBoolClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("ABooleanAttribute"), NamedTypeSymbol)
            Dim aByteClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AByteAttribute"), NamedTypeSymbol)
            Dim aCharClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("ACharAttribute"), NamedTypeSymbol)
            Dim aEnumClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AEnumAttribute"), NamedTypeSymbol)
            Dim aSingleClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("ASingleAttribute"), NamedTypeSymbol)
            Dim aDoubleClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("ADoubleAttribute"), NamedTypeSymbol)
            Dim aInt16Class = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AInt16Attribute"), NamedTypeSymbol)
            Dim aInt32Class = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AInt32Attribute"), NamedTypeSymbol)
            Dim aInt64Class = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AInt64Attribute"), NamedTypeSymbol)
            Dim aObjectClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AObjectAttribute"), NamedTypeSymbol)
            Dim aStringClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("AStringAttribute"), NamedTypeSymbol)
            Dim aTypeClass = TryCast(assemblies(1).Modules(0).GlobalNamespace.GetMember("ATypeAttribute"), NamedTypeSymbol)

            Dim c4 = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMember("C4"), NamedTypeSymbol)

            ' Check named value on attributes on c4

            '<AInt32(0, IA := {1,2})>
            Dim a = c4.GetAttribute(aInt32Class)
            a.VerifyValue(0, "IA", TypedConstantKind.Array, {1, 2})

            '<AEnum(TestAttributeEnum.No, ea:={TestAttributeEnum.Yes, TestAttributeEnum.No})>
            a = c4.GetAttribute(aEnumClass)
            a.VerifyValue(0, "EA", TypedConstantKind.Array, {0, 1})

            '<AString("No", sa:={"Yes", "No"})>
            a = c4.GetAttribute(aStringClass)
            a.VerifyValue(0, "SA", TypedConstantKind.Array, {"Yes", "No"})

            '<AObject("No", oa :={CType("Yes", Object), CType("No", Object)})>
            a = c4.GetAttribute(aObjectClass)
            a.VerifyValue(0, "OA", TypedConstantKind.Array, {"Yes", "No"})

            '<AType(GetType(C1), ta:={GetType(C1), GetType(C3)})>
            Dim c1 = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMember("C1"), TypeSymbol)
            Dim c3 = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMember("C3"), TypeSymbol)
            a = c4.GetAttribute(aTypeClass)
            a.VerifyValue(0, "TA", TypedConstantKind.Array, {c1, c3})
        End Sub

        <Fact()>
        Public Sub TestAttributesOnReturnTypes()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {
                                        TestResources.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                                        TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib,
                                        TestResources.NetFX.v4_0_21006.mscorlib
                                    })

            '<AString("C1")>
            'Public Class C1
            '    <AString("InnerC1")>
            '    Public Class InnerC1(of t1)
            '        <AString("InnerC2")>
            '        Public class InnerC2(of s1, s2)
            '        End class
            '    End Class

            '    <AString("field1")>
            '    Public field1 As integer

            '    <AString("Property1")>
            '    Public Property Property1 As Integer

            '    <AString("Sub1")>
            '    Public Sub Sub1(<AString("p1")> p1 As Integer)
            '    End Sub

            '    <AString("Function1")>
            '    Public Function Function1(<AString("p1")> p1 As Integer) As <AString("Integer")> Integer
            '        Return 0
            '    End Function
            'End Class

            Dim c1 = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMembers("C1").Single, NamedTypeSymbol)
            Assert.Equal("C1", c1.GetAttributes.First().CommonConstructorArguments.Single().Value)

            Dim property1 = DirectCast(c1.GetMember("Property1"), PropertySymbol)

            Dim returnAttributes = property1.GetMethod.GetReturnTypeAttributes()
            ' parameter.
            Assert.Equal("Integer", returnAttributes.First().CommonConstructorArguments.Single().Value)

            Dim function1 = DirectCast(c1.GetMember("Function1"), MethodSymbol)

            returnAttributes = function1.GetReturnTypeAttributes()
            ' parameter.
            Assert.Equal("Integer", returnAttributes.First().CommonConstructorArguments.Single().Value)
        End Sub

        <Fact>
        Public Sub TestAttributesWithTypesAndArrays()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {
                                        TestResources.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                                        TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib,
                                        TestResources.NetFX.v4_0_21006.mscorlib
                                    })

            'Public Class C2(Of T1)
            '     Custom attributes with generics

            '    <AType(GetType(List(Of )))>
            '    Public L1 As List(Of T1)

            '    <AType(GetType(List(Of C1)))>
            '    Public L2 As List(Of C1)

            '    <AType(GetType(List(Of String)))>
            '    Public L3 As List(Of String)

            '    <AType(GetType(List(Of KeyValuePair(Of C1, string))))>
            '    Public L4 As List(Of KeyValuePair(Of C1, string))

            '    <AType(GetType(List(Of KeyValuePair(Of String, C1.InnerC1(of integer).InnerC2(of string, string)))))>
            '    Public L5 As List(Of KeyValuePair(Of String, C1.InnerC1(of integer).InnerC2(of string, string)))


            Dim c2 = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMembers("C2").Single, NamedTypeSymbol)

            Dim l = DirectCast(c2.GetMember("L1"), FieldSymbol)
            Assert.Equal("System.Collections.Generic.List(Of )", DirectCast(l.GetAttributes.First().CommonConstructorArguments.Single().Value, Symbol).ToDisplayString(SymbolDisplayFormat.TestFormat))

            l = DirectCast(c2.GetMember("L2"), FieldSymbol)
            Assert.Equal("System.Collections.Generic.List(Of C1)", DirectCast(l.GetAttributes.First().CommonConstructorArguments.Single().Value, Symbol).ToDisplayString(SymbolDisplayFormat.TestFormat))

            l = DirectCast(c2.GetMember("L3"), FieldSymbol)
            Assert.Equal("System.Collections.Generic.List(Of System.String)", DirectCast(l.GetAttributes.First().CommonConstructorArguments.Single().Value, Symbol).ToDisplayString(SymbolDisplayFormat.TestFormat))

            l = DirectCast(c2.GetMember("L4"), FieldSymbol)
            Assert.Equal("System.Collections.Generic.List(Of System.Collections.Generic.KeyValuePair(Of C1, System.String))", DirectCast(l.GetAttributes.First().CommonConstructorArguments.Single().Value, Symbol).ToDisplayString(SymbolDisplayFormat.TestFormat))

            l = DirectCast(c2.GetMember("L5"), FieldSymbol)
            Assert.Equal("System.Collections.Generic.List(Of System.Collections.Generic.KeyValuePair(Of System.String, C1.InnerC1(Of System.Int32).InnerC2(Of System.String, System.String)))", DirectCast(l.GetAttributes.First().CommonConstructorArguments.Single().Value, Symbol).ToDisplayString(SymbolDisplayFormat.TestFormat))

            '    Arrays

            '<AInt32(New Integer() {1, 2})>
            'Public A1 As Type()

            '<AType(new Type(){GetType(string)})>
            'Public A2 As Object()

            '<AObject(new Type(){GetType(string)})>
            'Public A3 As Object()

            '<AObject(new Object(){GetType(string)})>
            'Public A4 As Object()

            '<AObject(new Object(){new Object() {GetType(string)}})>
            'Public A5 As Object()

            '<AObject({1, "two", GetType(string), 3.1415926})>
            'Public A6 As Object()

            '<AObject({1, new Object(){2,3,4}, 5})>
            'Public A7 As Object()

            '<AObject(new Integer(){1,2,3})>
            'Public A8 As Object()   

            Dim stringType = GetType(String) ' DirectCast(assemblies(0).Modules(0), PEModuleSymbol).GetCorLibType(SpecialType.System_string)

            Dim field = c2.GetMember(Of FieldSymbol)("A1")
            Dim arg = field.GetAttributes.Single()
            arg.VerifyValue(0, TypedConstantKind.Array, {1, 2})

            field = c2.GetMember(Of FieldSymbol)("A2")
            arg = field.GetAttributes.Single()
            arg.VerifyValue(0, TypedConstantKind.Array, {stringType})

            field = c2.GetMember(Of FieldSymbol)("A3")
            arg = field.GetAttributes.Single()
            arg.VerifyValue(0, TypedConstantKind.Array, New Object() {stringType})

            field = c2.GetMember(Of FieldSymbol)("A4")
            arg = field.GetAttributes.Single()
            arg.VerifyValue(0, TypedConstantKind.Array, {stringType})

            field = c2.GetMember(Of FieldSymbol)("A5")
            arg = field.GetAttributes.Single
            arg.VerifyValue(0, TypedConstantKind.Array, New Object() {New Object() {stringType}})

            field = c2.GetMember(Of FieldSymbol)("A6")
            Dim t = field.GetAttributes.First().CommonConstructorArguments.Single().Type
            Assert.Equal("Object()", t.ToDisplayString())
            arg = field.GetAttributes.Single
            arg.VerifyValue(0, TypedConstantKind.Array, New Object() {1, "two", stringType, 3.1415926})

            field = c2.GetMember(Of FieldSymbol)("A7")
            arg = field.GetAttributes.Single
            t = arg.CommonConstructorArguments.Single().Type
            Assert.Equal("Object()", t.ToDisplayString())
            VerifyValue(arg, 0, TypedConstantKind.Array, New Object() {1, New Object() {2, 3, 4}, 5})

            field = c2.GetMember(Of FieldSymbol)("A8")
            arg = field.GetAttributes.Single
            t = arg.CommonConstructorArguments.Single().Type
            Assert.Equal("Integer()", t.ToDisplayString())
            VerifyValue(arg, 0, TypedConstantKind.Array, New Integer() {1, 2, 3})
        End Sub

#Region "NotImpl"
#If False Then
        Structure AttributeArgs
            Public Pos As String()
            Public Named As KeyValuePair(Of String, String)()

            Public Sub New(p As String(), n As KeyValuePair(Of String, String)())
                Me.Pos = p
                Me.Named = n
            End Sub
        End Structure

        <Fact>
        Public Sub TestDumpAllAttributesTesLib()
                        Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {
                                        SymbolsTests.Metadata.MDTestAttributeDefLib,
                                        NetFx.v4_0_21006.mscorlib
                                    })

            Dim assemblyArgs = New AttributeArgs() {
                    New AttributeArgs(nothing, { new KeyValuePair(Of String, String)("Public WrapNonExceptionThrows As Boolean", "True") }),
                    New AttributeArgs({"8"}, nothing)
                }

            CheckAttributes(assemblies(0), assemblyArgs)

            DumpAttributes(assemblies(0).Modules(0))

        End Sub

        Private Sub DumpAttributes(s As Symbol)
            Dim i As Integer = 0
            For Each sa In s.GetAttributes()
                Dim j As Integer = 0
                For Each pa In sa.ConstructorArguments()
                    Console.WriteLine("{0} {1} {2}", pa.ToString())
                    j += 1
                Next

                j = 0
                For Each na In sa.NamedArguments()
                    Console.WriteLine("{0} {1} {2} = {3}", na.Key, na.Value.ToString())
                    j += 1
                Next
                i += 1
            Next
        End Sub

        Private Sub CheckAttributes(s As Symbol, expected As AttributeArgs())
            Dim i As Integer = 0
            For Each sa In s.GetAttributes()
                Dim j As Integer = 0
                For Each pa In sa.ConstructorArguments()
                    CheckPositionalArg(expected(i).Pos(j), pa)
                    j += 1
                Next

                j = 0
                For Each na In sa.NamedArguments()
                    CheckNamedArg(expected(i).Named(j), na)
                    j += 1
                Next
                i += 1
            Next
        End Sub

        Private Shared sub CheckPositionalArg(expected as String, actual As Object)
            Assert.Equal(expected, actual.ToString())
        end sub

        Private Shared Sub CheckNamedArg(expected As KeyvaluePair(of String, String), actual As KeyValuePair(of Symbol, Object))
            Assert.Equal(expected.Key, actual.Key.ToString)
            Assert.Equal(expected.Value, actual.Value.ToString())
        End Sub
#End If
#End Region

        <Fact>
        Public Sub TestInteropAttributesAssembly()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {
                                        TestResources.SymbolsTests.Metadata.AttributeInterop01,
                                        TestResources.NetFX.v4_0_21006.mscorlib
                                    })

            '[assembly: ImportedFromTypeLib("InteropAttributes")]
            '[assembly: PrimaryInteropAssembly(1, 2)]
            '[assembly: Guid("1234C65D-1234-447A-B786-64682CBEF136")]
            '[assembly: BestFitMapping(false, ThrowOnUnmappableChar = true)]

            '[assembly: AutomationProxy(false)]
            '[assembly: ClassInterface(ClassInterfaceType.AutoDual)]
            '[assembly: ComCompatibleVersion(1, 2, 3, 4)]
            '[assembly: ComConversionLoss()] 
            '[assembly: ComVisible(true)]
            '[assembly: TypeLibVersion(1, 0)]

            Dim asm = DirectCast(assemblies(0), AssemblySymbol)

            Dim attrs = asm.GetAttributes()
            ' 10 + 2 compiler inserted
            Assert.Equal(12, attrs.Length)
            For Each a In attrs
                Select Case a.AttributeClass.Name
                    Case "ImportedFromTypeLibAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                        Assert.Equal("String", a.CommonConstructorArguments(0).Type.ToDisplayString)
                        Assert.Equal("InteropAttributes", a.CommonConstructorArguments(0).Value)
                    Case "PrimaryInteropAssemblyAttribute"
                        Assert.Equal(2, a.CommonConstructorArguments.Length)
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                        Assert.Equal(2, a.CommonConstructorArguments(1).Value)
                    Case "GuidAttribute"
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal("String", a.CommonConstructorArguments(0).Type.ToDisplayString)
                        Assert.Equal("1234C65D-1234-447A-B786-64682CBEF136", a.CommonConstructorArguments(0).Value)
                    Case "BestFitMappingAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(False, a.CommonConstructorArguments(0).Value)
                        Assert.Equal(1, a.CommonNamedArguments.Length)
                        Assert.Equal("Boolean", a.CommonNamedArguments(0).Value.Type.ToDisplayString)
                        Assert.Equal("ThrowOnUnmappableChar", a.CommonNamedArguments(0).Key)
                        Assert.Equal(True, a.CommonNamedArguments(0).Value.Value)
                    Case "AutomationProxyAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                        Assert.Equal(False, a.CommonConstructorArguments(0).Value)
                    Case "ClassInterfaceAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                        ' enum is stored as its underneath type
                        Assert.Equal("System.Runtime.InteropServices.ClassInterfaceType", a.CommonConstructorArguments(0).Type.ToDisplayString)
                        ' ClassInterfaceType.AutoDual
                        Assert.Equal(2, a.CommonConstructorArguments(0).Value)
                    Case "ComCompatibleVersionAttribute"
                        Assert.Equal(4, a.CommonConstructorArguments.Length)
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                        Assert.Equal(3, a.CommonConstructorArguments(2).Value)
                    Case "ComConversionLossAttribute"
                        Assert.Equal(0, a.CommonConstructorArguments.Length)
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                    Case "ComVisibleAttribute"
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(True, a.CommonConstructorArguments(0).Value)
                    Case "TypeLibVersionAttribute"
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                        Assert.Equal(2, a.CommonConstructorArguments.Length)
                    Case "CompilationRelaxationsAttribute"
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(8, a.CommonConstructorArguments(0).Value)
                    Case "RuntimeCompatibilityAttribute"
                        Assert.Equal(0, a.CommonConstructorArguments.Length)
                        Assert.Equal(1, a.CommonNamedArguments.Length)
                        Assert.Equal("WrapNonExceptionThrows", a.CommonNamedArguments(0).Key)
                        Assert.Equal(True, a.CommonNamedArguments(0).Value.Value)
                    Case Else
                        Assert.Equal("Unexpected Attr", a.AttributeClass.Name)
                End Select
            Next

        End Sub

        ''' Did not Skip the test - will remove the explicit cast (from IMethodSymbol to MethodSymbol) once this bug is fixed
        <WorkItem(528029, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528029")>
        <Fact>
        Public Sub TestInteropAttributesInterface()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                   {
                                       TestResources.SymbolsTests.Metadata.AttributeInterop01,
                                       TestResources.NetFX.v4_0_21006.mscorlib
                                   })

            '[ComImport, Guid("ABCDEF5D-2448-447A-B786-64682CBEF123")]
            '[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
            '[TypeLibImportClass(typeof(object)), TypeLibType(TypeLibTypeFlags.FAggregatable)]
            'public interface IFoo
            '{
            '    [AllowReversePInvokeCalls()]
            '    void DoSomething();
            '    [ComRegisterFunction()]
            '    void Register(object o);
            '    [ComUnregisterFunction()]
            '    void UnRegister();
            '    [TypeLibFunc(TypeLibFuncFlags.FDefaultBind)]
            '    void LibFunc();
            '}

            Dim sysNS = DirectCast(assemblies(1).GlobalNamespace.GetMember("System"), NamespaceSymbol)
            Dim runtimeNS = DirectCast(sysNS.GetMember("Runtime"), NamespaceSymbol)
            Dim interopNS = DirectCast(runtimeNS.GetMember("InteropServices"), NamespaceSymbol)

            Dim appNS = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMember("Interop"), NamespaceSymbol)
            Dim igoo = DirectCast(appNS.GetMember("IFoo"), NamedTypeSymbol)
            ' ComImport is Pseudo attr
            Assert.Equal(4, igoo.GetAttributes().Length)

            ' get attr by NamedTypeSymbol
            Dim attrObj = DirectCast(interopNS.GetTypeMembers("GuidAttribute").Single(), NamedTypeSymbol)
            Dim attrSym = igoo.GetAttribute(attrObj)
            'Assert.Null(attrSym.NamedArguments)
            Assert.Equal(GetType(String), attrSym.CommonConstructorArguments(0).Value.GetType())
            Assert.Equal("ABCDEF5D-2448-447A-B786-64682CBEF123", attrSym.CommonConstructorArguments(0).Value)

            attrObj = DirectCast(interopNS.GetTypeMembers("InterfaceTypeAttribute").Single(), NamedTypeSymbol)
            ' use first ctor
            Dim ctor = attrObj.InstanceConstructors.First()
            attrSym = igoo.GetAttributes(ctor).First
            ' param in ctor is Int16, but Int32 in MD
            Assert.Equal(GetType(Int32), attrSym.CommonConstructorArguments(0).Value.GetType())
            Assert.Equal(1, attrSym.CommonConstructorArguments(0).Value)

            attrObj = DirectCast(interopNS.GetTypeMembers("TypeLibImportClassAttribute").Single(), NamedTypeSymbol)
            Dim msym = attrObj.InstanceConstructors.First()
            attrSym = igoo.GetAttributes(msym).First
            Assert.Equal("Object", CType(attrSym.CommonConstructorArguments(0).Value, Symbol).ToString())

            ' =============================
            Dim mem = DirectCast(igoo.GetMember("DoSomething"), MethodSymbol)
            Assert.Equal(1, mem.GetAttributes().Length)
            mem = DirectCast(igoo.GetMember("Register"), MethodSymbol)
            Assert.Equal(1, mem.GetAttributes().Length)
            mem = DirectCast(igoo.GetMember("UnRegister"), MethodSymbol)
            Assert.Equal(1, mem.GetAttributes().Length)
            mem = DirectCast(igoo.GetMember("LibFunc"), MethodSymbol)
            attrSym = mem.GetAttributes().First()
            Assert.Equal(1, attrSym.CommonConstructorArguments.Length)
            Assert.Equal(32, attrSym.CommonConstructorArguments(0).Value)
        End Sub

        <WorkItem(539942, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539942")>
        <Fact>
        Public Sub TestInteropAttributesDelegate()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                   {
                                       TestResources.SymbolsTests.Metadata.AttributeInterop01,
                                       TestResources.NetFX.v4_0_21006.mscorlib
                                   })

            ' [Serializable, ComVisible(false)]
            ' [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall, BestFitMapping = true, CharSet = CharSet.Ansi, SetLastError = true, ThrowOnUnmappableChar = true)]
            ' public delegate void DFoo(char p1, sbyte p2);

            Dim sysNS = DirectCast(assemblies(1).GlobalNamespace.GetMember("System"), NamespaceSymbol)
            Dim runtimeNS = DirectCast(sysNS.GetMember("Runtime"), NamespaceSymbol)
            Dim interopNS = DirectCast(runtimeNS.GetMember("InteropServices"), NamespaceSymbol)

            Dim appNS = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMember("Interop"), NamespaceSymbol)
            Dim dfoo = DirectCast(appNS.GetMember("DFoo"), NamedTypeSymbol)
            ' Pseudo - Serializable
            Assert.Equal(2, dfoo.GetAttributes().Length)

            ' get attr by NamedTypeSymbol
            Dim attrObj = DirectCast(interopNS.GetTypeMembers("ComVisibleAttribute").Single(), NamedTypeSymbol)
            Dim attrSym = dfoo.GetAttribute(attrObj)
            Assert.Equal(False, attrSym.CommonConstructorArguments(0).Value)

            attrObj = DirectCast(interopNS.GetTypeMembers("UnmanagedFunctionPointerAttribute").Single(), NamedTypeSymbol)
            attrSym = dfoo.GetAttribute(attrObj)
            'Assert.Equal(1, attrSym.ConstructorArguments.Count)
            Assert.Equal(3, attrSym.CommonConstructorArguments(0).Value)

            Assert.Equal(4, attrSym.CommonNamedArguments.Length)
            Assert.Equal("BestFitMapping", attrSym.CommonNamedArguments(0).Key)
            Assert.Equal(True, attrSym.CommonNamedArguments(0).Value.Value)
            Assert.Equal("CharSet", attrSym.CommonNamedArguments(1).Key)
            Assert.Equal(2, attrSym.CommonNamedArguments(1).Value.Value)
            Assert.Equal("SetLastError", attrSym.CommonNamedArguments(2).Key)
            Assert.Equal(True, attrSym.CommonNamedArguments(2).Value.Value)
            Assert.Equal("ThrowOnUnmappableChar", attrSym.CommonNamedArguments(3).Key)
            Assert.Equal(True, attrSym.CommonNamedArguments(3).Value.Value)
        End Sub

        <Fact>
        Public Sub TestInteropAttributesEnum()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                   {
                                       TestResources.SymbolsTests.Metadata.AttributeInterop02,
                                       TestResources.NetFX.v4_0_21006.mscorlib
                                   })

            ' [Guid("31230DD5-2448-447A-B786-64682CBEFEEE"), Flags]
            ' public enum MyEnum : sbyte  { 
            '    [NonSerialized]zero = 0, one = 1, two = 2, [Obsolete("message", false)]three = 4 
            ' }

            Dim sysNS = DirectCast(assemblies(1).GlobalNamespace.GetMember("System"), NamespaceSymbol)
            Dim runtimeNS = DirectCast(sysNS.GetMember("Runtime"), NamespaceSymbol)
            Dim interopNS = DirectCast(runtimeNS.GetMember("InteropServices"), NamespaceSymbol)

            Dim modattr = assemblies(0).Modules(0).GetAttributes().First()
            Assert.Equal("UnverifiableCodeAttribute", modattr.AttributeClass.Name)

            Dim appNS = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMember("EventNS"), NamespaceSymbol)
            Dim myEnum = DirectCast(appNS.GetMember("MyEnum"), NamedTypeSymbol)
            ' 
            Assert.Equal(2, myEnum.GetAttributes().Length)

            Dim field = DirectCast(myEnum.GetMember("zero"), FieldSymbol)
            ' Pseudo: NonSerialized
            Assert.Equal(0, field.GetAttributes().Length)

            field = DirectCast(myEnum.GetMember("three"), FieldSymbol)
            Assert.Equal(1, field.GetAttributes().Length)
            Dim attrSym = field.GetAttributes().First()
            Assert.Equal("ObsoleteAttribute", attrSym.AttributeClass.Name)
            Assert.Equal(2, attrSym.CommonConstructorArguments.Length)
            Assert.Equal("message", attrSym.CommonConstructorArguments(0).Value)
            Assert.Equal(False, attrSym.CommonConstructorArguments(1).Value)

        End Sub

        <Fact>
        Public Sub TestInteropAttributesMembers()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                   {
                                       TestResources.SymbolsTests.Metadata.AttributeInterop01,
                                       TestResources.NetFX.v4_0_21006.mscorlib
                                   })

            '[ComImport, TypeLibType(TypeLibTypeFlags.FAggregatable)]
            '[Guid("A88A175D-2448-447A-B786-CCC82CBEF156"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
            '[CoClass(typeof(CBar))]
            'public interface IBar             '{
            '    [DispId(10)]
            '    long MarshalAsGetProperty { [return: MarshalAs(UnmanagedType.I8)] get; }

            '    [DispId(20), IndexerNameAttribute("MyIndex")]
            '    int this[int idx] { get; set; }

            '    [DispId(30), PreserveSig]
            '    int MixedAttrMethod1([In] [MarshalAs(UnmanagedType.U4)] uint v1, [In, Out][MarshalAs(UnmanagedType.I4)] ref int v2);

            '    [DispId(40), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            '    void IDispatchParameters([MarshalAs(UnmanagedType.IDispatch)] object v1, [Out] [MarshalAs(UnmanagedType.IUnknown)] out object v2);

            '    [DispId(50), TypeLibFunc(TypeLibFuncFlags.FBindable)]
            '    void SCodeParameter([MarshalAs(UnmanagedType.Error)] int v1);

            '    [DispId(60)]
            '    [return: MarshalAs(UnmanagedType.BStr)]
            '    string VariantParameters([MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = "YumYum", MarshalType = "IUnknown")] object v1, [In][Out] ref object v2);

            '    [LCIDConversion(1)]
            '    void DecimalStringParameter([In] decimal v1, [MarshalAs(UnmanagedType.LPStr)] string v2, [MarshalAs(UnmanagedType.LPWStr)] string v3);
            '    void CurrencyParameter([In, MarshalAs(UnmanagedType.Currency)] decimal v1);
            '    // int MixedAttrMethod([In] [ComAliasName(stdole.OLE_COLOR)]uint v1, [In][Out][MarshalAs(UnmanagedType.I4)] ref int v2);
            '}

            Dim sysNS = DirectCast(assemblies(1).GlobalNamespace.GetMember("System"), NamespaceSymbol)
            Dim runtimeNS = DirectCast(sysNS.GetMember("Runtime"), NamespaceSymbol)
            Dim interopNS = DirectCast(runtimeNS.GetMember("InteropServices"), NamespaceSymbol)
            Dim reflectNS = DirectCast(sysNS.GetMember("Reflection"), NamespaceSymbol)

            Dim appNS = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMember("Interop"), NamespaceSymbol)
            ' 
            Dim ibar = DirectCast(appNS.GetMember("IBar"), NamedTypeSymbol)
            ' Pseudo - ComImport ( 4 + 1 -> DefaultMember)
            Assert.Equal(5, ibar.GetAttributes().Length)
            Dim atts = ibar.GetAttributes()
            ' get attr by NamedTypeSymbol
            Dim attrObj = DirectCast(interopNS.GetTypeMembers("CoClassAttribute").Single(), NamedTypeSymbol)
            Dim attrSym = ibar.GetAttribute(attrObj)
            Dim cbar = DirectCast(appNS.GetMember("CBar"), NamedTypeSymbol)
            Assert.Equal(cbar, attrSym.CommonConstructorArguments(0).Value)

            attrObj = DirectCast(reflectNS.GetTypeMembers("DefaultMemberAttribute").Single(), NamedTypeSymbol)
            attrSym = ibar.GetAttribute(attrObj)
            Assert.Equal("MyIndex", attrSym.CommonConstructorArguments(0).Value)

            '===================
            ' Members
            Dim mem = DirectCast(ibar.GetMember("MarshalAsGetProperty"), PropertySymbol)
            Assert.Equal(1, mem.GetAttributes().Length)
            Assert.Equal(10, mem.GetAttributes().First().CommonConstructorArguments(0).Value)
            ' attribute nor work on return type
            'attrSym = mem.Type.GetAttribute(attrObj)
            ' TODO: index

            Dim mem2 = DirectCast(ibar.GetMember("MixedAttrMethod1"), MethodSymbol)
            ' Pseudo: PreserveSig
            Assert.Equal(1, mem2.GetAttributes().Length)

            mem2 = DirectCast(ibar.GetMember("IDispatchParameters"), MethodSymbol)
            ' Pseudo: MethodImpl
            Assert.Equal(1, mem2.GetAttributes().Length)
            ' ? Pseudo: Out & MarshalAs
            'Assert.Equal(2, mem2.Parameters.Count)
            'Assert.Equal(2, mem2.Parameters(1).GetAttributes().Count)

            'attrSym = mem2.Parameters(1).GetAttributes().First()
            'Dim attrSym2 = mem2.Parameters(1).GetAttributes().Last()
            '' swap
            'If (attrSym2.AttributeClass.Name = "OutAttribute") Then
            '    Dim tmp = attrSym
            '    attrSym = attrSym2
            '    attrSym2 = tmp
            'End If

            ''attrObj = DirectCast(interopNS.GetTypeMembers("MarshalAsAttribute").Single(), NamedTypeSymbol)
            'Assert.Equal("MarshalAsAttribute", attrSym2.AttributeClass.Name) Assert.Equal(1,
            'attrSym2.ConstructorArguments(0).Value)

            mem2 = DirectCast(ibar.GetMember("DecimalStringParameter"), MethodSymbol)
            Assert.Equal(1, mem2.GetAttributes().Length)
            attrSym = mem2.GetAttributes().First()
            Assert.Equal("LCIDConversionAttribute", attrSym.AttributeClass.Name)
            Assert.Equal(1, attrSym.CommonConstructorArguments(0).Value)

        End Sub

        <Fact>
        Public Sub TestAttributesNames()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                   {
                                       TestResources.SymbolsTests.Metadata.AttributeTestLib01,
                                       TestResources.SymbolsTests.Metadata.AttributeTestDef01,
                                       TestResources.NetFX.v4_0_21006.mscorlib
                                   })

            Dim caNS = DirectCast(assemblies(1).GlobalNamespace.GetMember("CustomAttribute"), NamespaceSymbol)
            ' 
            Dim attrObj1 = DirectCast(caNS.GetTypeMembers("AttrName").Single(), NamedTypeSymbol)
            Dim attrObj2 = DirectCast(caNS.GetTypeMembers("AttrNameAttribute").Single(), NamedTypeSymbol)
            '
            '[assembly: @AttrName()]
            '[assembly: @AttrName(UShortField = 321)]
            '[module: AttrNameAttribute(TypeField = typeof(Dictionary<string, int>))]

            ' 2 + 2 compiler inserted
            Assert.Equal(4, assemblies(0).GetAttributes().Length)

            Dim attrSym = assemblies(0).GetAttribute(attrObj1)
            Assert.Equal("AttrName", attrSym.AttributeClass.Name)

            attrSym = assemblies(0).GetAttributes(attrObj1).Last()
            Assert.Equal("AttrName", attrSym.AttributeClass.Name)
            Assert.Equal(1, attrSym.CommonNamedArguments.Length)
            Assert.Equal("UShortField", attrSym.CommonNamedArguments(0).Key)
            Assert.Equal(CUShort(321), attrSym.CommonNamedArguments(0).Value.Value)

            attrSym = assemblies(0).Modules(0).GetAttributes().First()
            Assert.Equal("AttrNameAttribute", attrSym.AttributeClass.Name)
            Assert.Equal(1, attrSym.CommonNamedArguments.Length)
            Assert.Equal("TypeField", attrSym.CommonNamedArguments(0).Key)
            Assert.Equal("System.Collections.Generic.Dictionary(Of String, Integer)", TryCast(attrSym.CommonNamedArguments(0).Value.Value, TypeSymbol).ToString())
            Assert.Equal(2, TryCast(attrSym.CommonNamedArguments(0).Value.Value, NamedTypeSymbol).Arity)

        End Sub

        <WorkItem(539965, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539965")>
        <Fact>
        Public Sub TestAttributesOnTypeParameters()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                   {
                                       TestResources.SymbolsTests.Metadata.AttributeTestLib01,
                                       TestResources.SymbolsTests.Metadata.AttributeTestDef01,
                                       TestResources.NetFX.v4_0_21006.mscorlib
                                   })

            Dim caNS = DirectCast(assemblies(1).GlobalNamespace.GetMember("CustomAttribute"), NamespaceSymbol)

            Dim attrObj1 = DirectCast(caNS.GetTypeMembers("AllInheritMultipleAttribute").Single(), NamedTypeSymbol)
            Dim attrObj2 = DirectCast(caNS.GetTypeMembers("DerivedAttribute").Single(), NamedTypeSymbol)

            Dim appNS = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMember("AttributeUse"), NamespaceSymbol)

            'public interface IFoo<[typevar: AllInheritMultiple(3.1415926)] T, [AllInheritMultiple('q', 2)] V>
            '{
            '    // default: method
            '    [AllInheritMultiple(p3:1.234f, p2: 1056, p1: "555")]
            '    // attribute on return, param
            '    [return: AllInheritMultiple("obj", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static)]
            '    V Method([param: DerivedAttribute(new sbyte[] {-1, 0, 1}, ObjectField = typeof(IList<>))]T t);
            '}
            ' 
            Dim igoo = DirectCast(appNS.GetMember("IFoo"), NamedTypeSymbol)
            ' attribute on type parameter of interface
            Dim tp = igoo.TypeParameters(0)
            Dim attrSym = tp.GetAttributes().First()
            Assert.Equal("AllInheritMultipleAttribute", attrSym.AttributeClass.Name)
            ' p2 is optional
            Assert.Equal(2, attrSym.CommonConstructorArguments.Length)
            Assert.Equal(3.1415926, attrSym.CommonConstructorArguments(0).Value) 'object
            ' NYI: default optional
            ' Assert.Equal(CByte(1), attrSym.ConstructorArguments(1).Value) 'enum

            tp = igoo.TypeParameters(1)
            attrSym = tp.GetAttribute(attrObj1)
            Assert.Equal(3, attrSym.CommonConstructorArguments.Length)
            Assert.Equal("q"c, attrSym.CommonConstructorArguments(0).Value)
            Assert.Equal(CByte(2), attrSym.CommonConstructorArguments(1).Value)
            ' NYI: optional
            'Assert.Equal(CSByte(-1), attrSym.ConstructorArguments(2).Value)

            ' attribute on method
            ' [AllInheritMultiple(p3:1.234f, p2: 1056, p1: "555")]
            Dim mtd = DirectCast(igoo.GetMember("Method"), MethodSymbol)
            Assert.Equal(1, mtd.GetAttributes().Length)
            attrSym = mtd.GetAttributes().First()
            Assert.Equal(4, attrSym.CommonConstructorArguments.Length) ' p4 is default optional
            Assert.Equal("555", attrSym.CommonConstructorArguments(0).Value) ' object

            ' NotImpl [return: AllInheritMultiple("obj", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static)] attrSym =
            'mtd.ReturnType.GetAttributes().First() Assert.Equal(2, attrSym.ConstructorArguments.Count)
            'Assert.Equal("obj", attrSym.ConstructorArguments(0).Value) Assert.Equal(CByte(3),
            'attrSym.ConstructorArguments(1))

            ' [param: DerivedAttribute(new sbyte[] {-1, 0, 1}, ObjectField = typeof(IList<>))]
            attrSym = mtd.Parameters(0).GetAttribute(attrObj2)
            Assert.Equal(1, attrSym.CommonConstructorArguments.Length)
            Assert.Equal(1, attrSym.CommonNamedArguments.Length)
            Assert.Equal("SByte()", attrSym.CommonConstructorArguments(0).Type.ToDisplayString())
            attrSym.VerifyValue(0, TypedConstantKind.Array, New SByte() {-1, 0, 1})

            Assert.Equal("ObjectField", attrSym.CommonNamedArguments(0).Key)
            Assert.Equal("System.Collections.Generic.IList(Of )", TryCast(attrSym.CommonNamedArguments(0).Value.Value, NamedTypeSymbol).ToString())
        End Sub

        '[AllInheritMultiple(new char[] { '1', '2' }, UIntField = 112233)]
        '[type: AllInheritMultiple(new char[] { 'a', '\0', '\t' }, AryField = new ulong[] { 0, 1, ulong.MaxValue })]
        '[AllInheritMultiple(null, "", null, "1234", AryProp = new object[2] { new ushort[] { 1 }, new ushort[] { 2, 3, 4 } })]
        'public class Foo<[typevar: AllInheritMultiple(null), AllInheritMultiple()] T> : IFoo<T, ushort>
        '{
        '    // named parameters
        '    [field: AllInheritMultiple(p2: System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public, p1: -123)]
        '    [AllInheritMultiple(p1: 111, p2: System.Reflection.BindingFlags.NonPublic)]
        '    public int ClassField;

        '    [property: BaseAttribute(-1)]
        '    public Foo<char> Prop
        '    {
        '        // return:
        '        [AllInheritMultiple(1, 2, 3), AllInheritMultiple(4, 5, 1.1f)]
        '        get;
        '        [param: DerivedAttribute(-3)]
        '        set;
        '    }

        '    [AllInheritMultiple(+007, 256)]
        '    [AllInheritMultiple(-008, 255)]
        '    [method: DerivedAttribute(typeof(IFoo<short, ushort>), ObjectField = 1)]
        '    public ushort Method(T t) { return 0; }
        '    // Explicit NotImpl
        '    // ushort IFoo<T, ushort>.Method(T t) { return 0; }
        '}
        <WorkItem(539965, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539965")>
        <Fact>
        Public Sub TestAttributesMultiples()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                   {
                                       TestResources.SymbolsTests.Metadata.AttributeTestLib01,
                                       TestResources.SymbolsTests.Metadata.AttributeTestDef01,
                                       TestResources.NetFX.v4_0_21006.mscorlib
                                   })

            Dim caNS = DirectCast(assemblies(1).GlobalNamespace.GetMember("CustomAttribute"), NamespaceSymbol)

            Dim attrObj1 = DirectCast(caNS.GetTypeMembers("AllInheritMultipleAttribute").Single(), NamedTypeSymbol)
            Dim mctors = attrObj1.Constructors '.ToList()
            Assert.Equal(5, mctors.Length)

            Dim attrObj2 = DirectCast(caNS.GetTypeMembers("DerivedAttribute").Single(), NamedTypeSymbol)

            Dim appNS = DirectCast(assemblies(0).Modules(0).GlobalNamespace.GetMember("AttributeUse"), NamespaceSymbol)
            Dim foo = DirectCast(appNS.GetMember("Foo"), NamedTypeSymbol)
            ' Attribute on class Foo
            Dim attrs = foo.GetAttributes(DirectCast(mctors(4), MethodSymbol))

            Assert.Equal(foo.GetAttributes().Length, attrs.Count)
            Dim count = 0
            For Each a In attrs
                Dim pos0 = a.CommonConstructorArguments(0).Values
                Assert.Equal("Char()", a.CommonConstructorArguments(0).Type.ToDisplayString())
                ' [AllInheritMultiple(null, "", null, "1234", AryProp = new object[2] { new ushort[] { 1 }, new ushort[] { 2, 3, 4 } })]
                If pos0.IsDefaultOrEmpty Then
                    count += 1
                    Assert.Equal("String()", a.CommonConstructorArguments(1).Type.ToDisplayString())
                    Assert.Equal(3, a.CommonConstructorArguments(1).Values.Length)
                    Dim na0 = a.CommonNamedArguments(0).Value.Values
                    Assert.Equal(2, na0.Length)
                    ' jagged array
                    Assert.Equal("UShort()", na0(1).Type.ToDisplayString())
                    Dim elem = na0(1).Values
                    Assert.Equal("UShort", elem(1).Type.ToDisplayString())
                    ' [AllInheritMultiple(new char[] { '1', '2' }, UIntField = 112233)]
                ElseIf pos0.Length = 2 Then
                    count += 2
                    Assert.Equal(1, a.CommonNamedArguments.Length)
                    ' [type: AllInheritMultiple(new char[] { 'a', '\0', '\t' }, AryField = new ulong[] { 0, 1, ulong.MaxValue })]
                ElseIf pos0.Length = 3 Then
                    count += 4
                    Assert.Equal("AryField", a.CommonNamedArguments(0).Key)
                    Assert.Equal("ULong()", a.CommonNamedArguments(0).Value.Type.ToDisplayString)
                    Dim na1 = a.CommonNamedArguments(0).Value.Values
                    Assert.Equal("AryField", a.CommonNamedArguments(0).Key)
                Else
                    count += 99 ' should not be here
                End If
            Next
            ' hit 3 attr once each
            Assert.Equal(7, count)

            ' attribute on type parameter of class Foo
            Dim tp = foo.TypeParameters(0)
            Assert.Equal(2, tp.GetAttributes().Length)

            ' field
            Dim fld = DirectCast(foo.GetMember("ClassField"), FieldSymbol)
            Assert.Equal(2, fld.GetAttributes().Length)
            Assert.Equal(0, fld.GetAttributes().First().CommonNamedArguments.Length)

            ' property
            Dim prop = DirectCast(foo.GetMember("Prop"), PropertySymbol)
            Assert.Equal(1, prop.GetAttributes().Length)
            Assert.Equal(-1, prop.GetAttributes().First().CommonConstructorArguments(0).Value)
            ' get, set
            Assert.Equal(3, prop.GetMethod.GetAttributes().Length)
            Assert.Equal(1, prop.SetMethod.GetAttributes().Length)

            Dim attrSym = tp.GetAttribute(attrObj1)

            ' method
            Dim mtd = DirectCast(foo.GetMember("Method"), MethodSymbol)
            Assert.Equal(3, mtd.GetAttributes().Length)
            attrs = mtd.GetAttributes(DirectCast(mctors(2), MethodSymbol))
            Assert.Equal(1, attrs.Count)
            ' [AllInheritMultiple(-008, 255)] ' p3 is optional
            attrSym = attrs.First()
            Assert.Equal(3, attrSym.CommonConstructorArguments.Length)
            Assert.Equal(-8, attrSym.CommonConstructorArguments(0).Value) ' object
            Assert.Equal(CByte(255), attrSym.CommonConstructorArguments(1).Value)

            attrs = mtd.GetAttributes(DirectCast(mctors(3), MethodSymbol))
            Assert.Equal(1, attrs.Count)
            ' [AllInheritMultiple(+007, 256)] ' p3, p4 optional
            attrSym = attrs.First()
            Assert.Equal(4, attrSym.CommonConstructorArguments.Length) ' p4 is default optional
            Assert.Equal(7, attrSym.CommonConstructorArguments(0).Value) ' object
            Assert.Equal(256&, attrSym.CommonConstructorArguments(1).Value)
            ' default
            Assert.Equal(0.123!, attrSym.CommonConstructorArguments(2).Value)
            Assert.Equal(CShort(-2), attrSym.CommonConstructorArguments(3).Value)

            ' [method: DerivedAttribute(typeof(IFoo<short, ushort>), ObjectField = 1)]
            attrs = mtd.GetAttributes(attrObj2)
            Assert.Equal(1, attrs.Count)
            attrSym = attrs.First()
            Assert.Equal(1, attrSym.CommonConstructorArguments.Length)
            Assert.Equal(1, attrSym.CommonNamedArguments.Length)
            Assert.Equal("AttributeUse.IFoo(Of System.Int16, System.UInt16)", TryCast(attrSym.CommonConstructorArguments(0).Value, NamedTypeSymbol).ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal(1, attrSym.CommonNamedArguments(0).Value.Value)
        End Sub

#Region "Regression"

        <WorkItem(539995, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539995")>
        <Fact>
        Public Sub TestAttributesAssemblyVersionValue()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                   {
                                       TestResources.NetFX.v4_0_30319.System_Core,
                                       TestResources.NetFX.v4_0_30319.System,
                                       TestResources.NetFX.v4_0_21006.mscorlib
                                   })

            Dim sysNS = DirectCast(assemblies(2).GlobalNamespace.GetMember("System"), NamespaceSymbol)
            Dim refNS = DirectCast(sysNS.GetMember("Reflection"), NamespaceSymbol)
            Dim rtNS = DirectCast(sysNS.GetMember("Runtime"), NamespaceSymbol)

            Dim asmFileAttr = DirectCast(refNS.GetTypeMembers("AssemblyFileVersionAttribute").Single(), NamedTypeSymbol)
            Dim attr1 = assemblies(0).GetAttribute(asmFileAttr)
            Assert.Equal("4.0.30319.1", attr1.CommonConstructorArguments(0).Value)

            Dim asmInfoAttr = DirectCast(refNS.GetTypeMembers("AssemblyInformationalVersionAttribute").Single(), NamedTypeSymbol)
            attr1 = assemblies(0).GetAttribute(asmInfoAttr)
            Assert.Equal("4.0.30319.1", attr1.CommonConstructorArguments(0).Value)

            Dim asmTgtAttr = DirectCast(rtNS.GetTypeMembers("AssemblyTargetedPatchBandAttribute").Single(), NamedTypeSymbol)
            attr1 = assemblies(0).GetAttribute(asmTgtAttr)
            Assert.Equal("1.0.21-0", attr1.CommonConstructorArguments(0).Value)

        End Sub

        <WorkItem(539996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539996")>
        <Fact>
        Public Sub TestAttributesWithTypeOfInternalClass()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                   {
                                       TestResources.NetFX.v4_0_30319.System_Core,
                                       TestResources.NetFX.v4_0_30319.System,
                                       TestResources.NetFX.v4_0_21006.mscorlib
                                   })

            Dim corsysNS = TryCast(assemblies(2).GlobalNamespace.GetMembers("System").Single, NamespaceSymbol)
            Dim diagNS = TryCast(corsysNS.GetMembers("Diagnostics").Single, NamespaceSymbol)

            Dim sysNS = DirectCast(assemblies(0).GlobalNamespace.GetMember("System"), NamespaceSymbol)
            Dim linqNS = DirectCast(sysNS.GetMember("Linq"), NamespaceSymbol)
            Dim exprNS = DirectCast(linqNS.GetMember("Expressions"), NamespaceSymbol)

            Dim dbgProxyAttr = DirectCast(diagNS.GetTypeMembers("DebuggerTypeProxyAttribute").Single(), NamedTypeSymbol)

            ' [DebuggerTypeProxy(typeof(Expression.BinaryExpressionProxy))] - internal class as argument to typeof()
            ' public class BinaryExpression : Expression {... }
            Dim attr1 = exprNS.GetTypeMembers("BinaryExpression").First().GetAttribute(dbgProxyAttr)
            Assert.Equal("System.Linq.Expressions.Expression.BinaryExpressionProxy", CType(attr1.CommonConstructorArguments(0).Value, TypeSymbol).ToDisplayString(SymbolDisplayFormat.TestFormat))

            ' [DebuggerTypeProxy(typeof(Expression.TypeBinaryExpressionProxy))]
            ' public sealed class TypeBinaryExpression : Expression
            attr1 = exprNS.GetTypeMembers("TypeBinaryExpression").First().GetAttribute(dbgProxyAttr)
            Assert.Equal("System.Linq.Expressions.Expression.TypeBinaryExpressionProxy", CType(attr1.CommonConstructorArguments(0).Value, TypeSymbol).ToDisplayString(SymbolDisplayFormat.TestFormat))

        End Sub

        <WorkItem(539999, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539999")>
        <Fact>
        Public Sub TestAttributesStaticInstanceCtors()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                {
                    TestResources.NetFX.v4_0_30319.System,
                    TestResources.NetFX.v4_0_21006.mscorlib,
                    TestResources.NetFX.v4_0_30319.System_Configuration
                })

            Dim sysNS = DirectCast(assemblies(0).GlobalNamespace.GetMember("System"), NamespaceSymbol)
            Dim secondNS = DirectCast(sysNS.GetMember("Configuration"), NamespaceSymbol)
            Dim type01 = DirectCast(secondNS.GetTypeMembers("SchemeSettingElement").Single(), NamedTypeSymbol)

            Dim mems = type01.GetMembers("GenericUriParserOptions")
            Dim prop = TryCast(mems.First(), PropertySymbol)
            If (prop Is Nothing) Then
                prop = TryCast(mems.Last(), PropertySymbol)
            End If

            '  [ConfigurationProperty("genericUriParserOptions", DefaultValue=0, IsRequired=true)]
            Dim attr = prop.GetAttributes().First()
            Assert.Equal("ConfigurationPropertyAttribute", attr.AttributeClass.Name)
            Assert.Equal("genericUriParserOptions", attr.CommonConstructorArguments(0).Value)
            Assert.Equal(2, attr.CommonNamedArguments().Length)
            Assert.Equal("DefaultValue", attr.CommonNamedArguments(0).Key)
            Assert.Equal(0, attr.CommonNamedArguments(0).Value.Value)
            Assert.Equal("IsRequired", attr.CommonNamedArguments(1).Key)
            Assert.Equal(True, attr.CommonNamedArguments(1).Value.Value)

        End Sub

        <WorkItem(540000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540000")>
        <Fact>
        Public Sub TestAttributesOverloadedCtors()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                 {
                                     TestResources.NetFX.v4_0_30319.System_Data,
                                     TestResources.NetFX.v4_0_30319.System_Core,
                                     TestResources.NetFX.v4_0_30319.System,
                                     TestResources.NetFX.v4_0_30319.mscorlib
                                 })

            Dim sysNS = DirectCast(assemblies(0).GlobalNamespace.GetMember("System"), NamespaceSymbol)
            Dim secondNS = DirectCast(sysNS.GetMember("Data"), NamespaceSymbol)
            Dim thirdNS = DirectCast(secondNS.GetMember("Common"), NamespaceSymbol)

            Dim resCatAttr = DirectCast(secondNS.GetTypeMembers("ResCategoryAttribute").Single(), NamedTypeSymbol)
            Dim resDesAttr = DirectCast(secondNS.GetTypeMembers("ResDescriptionAttribute").Single(), NamedTypeSymbol)
            Dim level01NS = DirectCast(assemblies(2).GlobalNamespace.GetMember("System"), NamespaceSymbol)
            Dim level02NS = DirectCast(level01NS.GetMember("ComponentModel"), NamespaceSymbol)
            Dim defValAttr = DirectCast(level02NS.GetTypeMembers("DefaultValueAttribute").Single(), NamedTypeSymbol)

            Dim type01 = DirectCast(thirdNS.GetTypeMembers("DataAdapter").Single(), NamedTypeSymbol)
            Dim prop = TryCast(type01.GetMember("MissingMappingAction"), PropertySymbol)

            ' [DefaultValue(1), ResCategory("DataCategory_Mapping"), ResDescription("DataAdapter_MissingMappingAction")]
            ' public MissingMappingAction MissingMappingAction { get; set; }
            Dim attr = prop.GetAttributes(resCatAttr).Single()
            Assert.Equal(1, attr.CommonConstructorArguments().Length)
            Assert.Equal("DataCategory_Mapping", attr.CommonConstructorArguments(0).Value)

            attr = prop.GetAttributes(resDesAttr).Single()
            Assert.Equal(1, attr.CommonConstructorArguments().Length)
            Assert.Equal("DataAdapter_MissingMappingAction", attr.CommonConstructorArguments(0).Value)

            attr = prop.GetAttributes(defValAttr).Single()
            Assert.Equal(1, attr.CommonConstructorArguments().Length)
            Assert.Equal(1, attr.CommonConstructorArguments(0).Value)
        End Sub

#End Region

        <WorkItem(530209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530209")>
        <Fact>
        Public Sub Bug530209_DecimalConstant()
            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Public Class Class1
    Public Const d1 as Decimal = -7

    Public Const d2 as Date = #1/1/2013#

    Public Sub M1(Optional d1 as Decimal = -7, 
                  Optional d2 as Date = #1/1/2013#)
    End Sub
End Class
]]>
                    </file>
                </compilation>)

            CompileAndVerify(c1, symbolValidator:=Sub(m As ModuleSymbol)
                                                      Dim peModule = DirectCast(m, PEModuleSymbol)
                                                      Dim class1 = peModule.ContainingAssembly.GetTypeByMetadataName("Class1")
                                                      Dim d1 = class1.GetMember(Of PEFieldSymbol)("d1")
                                                      Dim d2 = class1.GetMember(Of PEFieldSymbol)("d2")
                                                      Dim m1Parameters = class1.GetMethod("M1").Parameters.Cast(Of PEParameterSymbol)

                                                      Assert.Empty(d1.GetAttributes())
                                                      Assert.Equal("System.Runtime.CompilerServices.DecimalConstantAttribute(0, 128, 0, 0, 7)", peModule.GetCustomAttributesForToken(d1.Handle).Single().ToString())
                                                      Assert.Equal(d1.ConstantValue, CDec(-7))
                                                      Assert.Empty(d2.GetAttributes())
                                                      Assert.Equal("System.Runtime.CompilerServices.DateTimeConstantAttribute(634925952000000000)", peModule.GetCustomAttributesForToken(d2.Handle).Single().ToString())
                                                      Assert.Equal(d2.ConstantValue, #1/1/2013#)

                                                      Assert.Empty(m1Parameters(0).GetAttributes())
                                                      Assert.Equal("System.Runtime.CompilerServices.DecimalConstantAttribute(0, 128, 0, 0, 7)", peModule.GetCustomAttributesForToken(m1Parameters(0).Handle).Single().ToString())
                                                      Assert.Equal(m1Parameters(0).ExplicitDefaultValue, CDec(-7))

                                                      Assert.Empty(m1Parameters(1).GetAttributes())
                                                      Assert.Equal("System.Runtime.CompilerServices.DateTimeConstantAttribute(634925952000000000)", peModule.GetCustomAttributesForToken(m1Parameters(1).Handle).Single().ToString())
                                                      Assert.Equal(m1Parameters(1).ExplicitDefaultValue, #1/1/2013#)
                                                  End Sub)
        End Sub

        <WorkItem(530209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530209")>
        <Fact>
        Public Sub Bug530209_DecimalConstant_FromIL()

            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit Class1
       extends [mscorlib]System.Object
{
  .field public static initonly valuetype [mscorlib]System.Decimal d1
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                  uint8,
                                                                                                  uint32,
                                                                                                  uint32,
                                                                                                  uint32)
           = {uint8(0)
              uint8(128)
              uint32(0)
              uint32(0)
              uint32(7)}
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                  uint8,
                                                                                                  int32,
                                                                                                  int32,
                                                                                                  int32)
           = {uint8(0)
              uint8(128)
              int32(0)
              int32(0)
              int32(8)}
  .field public static initonly valuetype [mscorlib]System.DateTime d2
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64)
           = {int64(634925952000000000)}
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64)
           = {int64(634925952000000001)}
  .method private specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldc.i4.s   -7
    IL_0002:  conv.i8
    IL_0003:  newobj     instance void [mscorlib]System.Decimal::.ctor(int64)
    IL_0008:  stsfld     valuetype [mscorlib]System.Decimal Class1::d1
    IL_000d:  ldc.i8     0x8cfb5ca13a30000
    IL_0016:  newobj     instance void [mscorlib]System.DateTime::.ctor(int64)
    IL_001b:  stsfld     valuetype [mscorlib]System.DateTime Class1::d2
    IL_0020:  ret
  } // end of method Class1::.cctor

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Class1::.ctor

  .method public instance void  M1([opt] valuetype [mscorlib]System.Decimal d1,
                                   [opt] valuetype [mscorlib]System.DateTime d2) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                    uint8,
                                                                                                    uint32,
                                                                                                    uint32,
                                                                                                    uint32)
             = {uint8(0)
                uint8(128)
                uint32(0)
                uint32(0)
                uint32(7)}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                    uint8,
                                                                                                    int32,
                                                                                                    int32,
                                                                                                    int32)
             = {uint8(0)
                uint8(128)
                int32(0)
                int32(0)
                int32(8)}
    .param [2]
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64)
             = {int64(634925952000000001)}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64)
             = {int64(634925952000000000)}
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Class1::M1

} // end of class Class1
]]>.Value

            Dim c1 = CompilationUtils.CreateCompilationWithCustomILSource(
                <compilation>
                    <file name="a.vb">
Class Class2
    Inherits Class1
End Class
                    </file>
                </compilation>, ilSource)


            CompileAndVerify(c1, symbolValidator:=Sub(m As ModuleSymbol)
                                                      Dim peModule = DirectCast(m, PEModuleSymbol)
                                                      Dim class1 = peModule.ContainingAssembly.GetTypeByMetadataName("Class2").BaseType()
                                                      Dim d1 = class1.GetMember(Of PEFieldSymbol)("d1")
                                                      Dim d2 = class1.GetMember(Of PEFieldSymbol)("d2")
                                                      Dim m1Parameters = class1.GetMethod("M1").Parameters.Cast(Of PEParameterSymbol)

                                                      Assert.Empty(d1.GetAttributes())
                                                      Assert.Equal(d1.ConstantValue, CDec(-7))

                                                      Assert.Empty(d2.GetAttributes())
                                                      Assert.Equal(d2.ConstantValue, #1/1/2013#)

                                                      Assert.Empty(m1Parameters(0).GetAttributes())
                                                      Assert.Equal(m1Parameters(0).ExplicitDefaultValue, CDec(-7))

                                                      Assert.Empty(m1Parameters(1).GetAttributes())
                                                      Assert.Equal(m1Parameters(1).ExplicitDefaultValue, #1/1/2013#)
                                                  End Sub)

            Dim c2 = CompilationUtils.CreateCompilationWithCustomILSource(
                <compilation>
                    <file name="a.vb">
Class Class2
    Inherits Class1
End Class
                    </file>
                </compilation>, ilSource)

            ' Switch order of API calls

            CompileAndVerify(c2, symbolValidator:=Sub(m As ModuleSymbol)
                                                      Dim peModule = DirectCast(m, PEModuleSymbol)
                                                      Dim class1 = peModule.ContainingAssembly.GetTypeByMetadataName("Class2").BaseType()
                                                      Dim d1 = class1.GetMember(Of PEFieldSymbol)("d1")
                                                      Dim d2 = class1.GetMember(Of PEFieldSymbol)("d2")
                                                      Dim m1Parameters = class1.GetMethod("M1").Parameters.Cast(Of PEParameterSymbol)

                                                      Assert.Equal(d1.ConstantValue, CDec(-7))
                                                      Assert.Empty(d1.GetAttributes())

                                                      Assert.Equal(d2.ConstantValue, #1/1/2013#)
                                                      Assert.Empty(d2.GetAttributes())

                                                      Assert.Equal(m1Parameters(0).ExplicitDefaultValue, CDec(-7))
                                                      Assert.Empty(m1Parameters(0).GetAttributes())

                                                      Assert.Equal(m1Parameters(1).ExplicitDefaultValue, #1/1/2013#)
                                                      Assert.Empty(m1Parameters(1).GetAttributes())
                                                  End Sub)
        End Sub

        <Fact>
        <WorkItem(18092, "https://github.com/dotnet/roslyn/issues/18092")>
        Public Sub ForwardedSystemType()

            Dim ilSource = <![CDATA[
.class extern forwarder System.Type
{
  .assembly extern mscorlib
}

.class public auto ansi beforefieldinit MyAttribute
       extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class [mscorlib]System.Type val) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ret
  } // end of method MyAttribute::.ctor

} // end of class MyAttribute
]]>.Value


            Dim c = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation>
    <file name="a.vb"><![CDATA[
<MyAttribute(GetType(MyAttribute))>
Class Test
End Class
    ]]></file>
</compilation>, ilSource)

            Const expected = "MyAttribute(GetType(MyAttribute))"
            Assert.Equal(expected, c.GetTypeByMetadataName("Test").GetAttributes().Single().ToString())

            CompileAndVerify(c, symbolValidator:=Sub(m)
                                                     Assert.Equal(expected, m.GlobalNamespace.GetTypeMember("Test").GetAttributes().Single().ToString())
                                                 End Sub)
        End Sub

    End Class

End Namespace
