// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class LoadingAttributes : CSharpTestBase
    {
        [Fact]
        public void TestAssemblyAttributes()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                TestReferences.SymbolsTests.Metadata.MDTestAttributeDefLib,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var assembly0 = assemblies[0];
            var assembly1 = assemblies[1];

            //<Assembly:ABoolean(True)> 
            //<Assembly:AByte(1)> 
            //<Assembly:AChar("a"c)>
            //<Assembly:ADouble(3.1415926)> 
            //<Assembly:AInt16(16)> 
            //<Assembly:AInt32(32)> 
            //<Assembly:AInt64(64)>
            //<Assembly:AObject("object")> 
            //<Assembly:ASingle(3.14159)> 
            //<Assembly:AString("assembly")> 
            //<Assembly:AType(GetType(String))>

            var aBoolClass = assembly1.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("ABooleanAttribute") as NamedTypeSymbol;
            var aByteClass = assembly1.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("AByteAttribute") as NamedTypeSymbol;
            var aCharClass = assembly1.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("ACharAttribute") as NamedTypeSymbol;
            var aSingleClass = assembly1.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("ASingleAttribute") as NamedTypeSymbol;
            var aDoubleClass = assembly1.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("ADoubleAttribute") as NamedTypeSymbol;
            var aInt16Class = assembly1.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("AInt16Attribute") as NamedTypeSymbol;
            var aInt32Class = assembly1.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("AInt32Attribute") as NamedTypeSymbol;
            var aInt64Class = assembly1.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("AInt64Attribute") as NamedTypeSymbol;
            var aObjectClass = assembly1.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("AObjectAttribute") as NamedTypeSymbol;
            var aStringClass = assembly1.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("AStringAttribute") as NamedTypeSymbol;
            var aTypeClass = assembly1.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("ATypeAttribute") as NamedTypeSymbol;

            // Check attributes on assembly
            var aBoolInst = assembly0.GetAttribute(aBoolClass);
            aBoolInst.VerifyValue(0, TypedConstantKind.Primitive, true);

            var aByteInst = assembly0.GetAttribute(aByteClass);
            aByteInst.VerifyValue(0, TypedConstantKind.Primitive, Convert.ToByte(1));

            var aCharInst = assembly0.GetAttribute(aCharClass);
            aCharInst.VerifyValue(0, TypedConstantKind.Primitive, 'a');

            var aSingleInst = assembly0.GetAttribute(aSingleClass);
            aSingleInst.VerifyValue(0, TypedConstantKind.Primitive, 3.14159f);

            var aDoubleInst = assembly0.GetAttribute(aDoubleClass);
            aDoubleInst.VerifyValue(0, TypedConstantKind.Primitive, 3.1415926);

            var aInt16Inst = assembly0.GetAttribute(aInt16Class);
            aInt16Inst.VerifyValue(0, TypedConstantKind.Primitive, (Int16)16);

            var aInt32Inst = assembly0.GetAttribute(aInt32Class);
            aInt32Inst.VerifyValue(0, TypedConstantKind.Primitive, 32);

            var aInt64Inst = assembly0.GetAttribute(aInt64Class);
            aInt64Inst.VerifyValue(0, TypedConstantKind.Primitive, 64L);

            var aObjectInst = assembly0.GetAttribute(aObjectClass);
            aObjectInst.VerifyValue(0, TypedConstantKind.Primitive, "object");

            var aStringInst = assembly0.GetAttribute(aStringClass);
            aStringInst.VerifyValue(0, TypedConstantKind.Primitive, "assembly");

            var aTypeInst = assembly0.GetAttribute(aTypeClass);
            aTypeInst.VerifyValue(0, TypedConstantKind.Type, typeof(string));
        }

        [Fact]
        public void TestModuleAttributes()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                TestReferences.SymbolsTests.Metadata.MDTestAttributeDefLib,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });


            var assembly1 = assemblies[1];
            var module0 = assemblies[0].Modules[0];

            //<Module:AString("module")>
            //<Module:ABoolean(True)>
            //<Module:AByte(1)>
            //<Module:AChar("a"c)>
            //<Module:ADouble(3.1415926)>
            //<Module:AInt16(16)>
            //<Module:AInt32(32)>
            //<Module:AInt64(64)>
            //<Module:AObject("object")>
            //<Module:ASingle(3.14159)>
            //<Module:AType(GetType(String))>

            var aBoolClass = assembly1.Modules[0].GlobalNamespace.GetMember("ABooleanAttribute") as NamedTypeSymbol;
            var aByteClass = assembly1.Modules[0].GlobalNamespace.GetMember("AByteAttribute") as NamedTypeSymbol;
            var aCharClass = assembly1.Modules[0].GlobalNamespace.GetMember("ACharAttribute") as NamedTypeSymbol;
            var aSingleClass = assembly1.Modules[0].GlobalNamespace.GetMember("ASingleAttribute") as NamedTypeSymbol;
            var aDoubleClass = assembly1.Modules[0].GlobalNamespace.GetMember("ADoubleAttribute") as NamedTypeSymbol;
            var aInt16Class = assembly1.Modules[0].GlobalNamespace.GetMember("AInt16Attribute") as NamedTypeSymbol;
            var aInt32Class = assembly1.Modules[0].GlobalNamespace.GetMember("AInt32Attribute") as NamedTypeSymbol;
            var aInt64Class = assembly1.Modules[0].GlobalNamespace.GetMember("AInt64Attribute") as NamedTypeSymbol;
            var aObjectClass = assembly1.Modules[0].GlobalNamespace.GetMember("AObjectAttribute") as NamedTypeSymbol;
            var aStringClass = assembly1.Modules[0].GlobalNamespace.GetMember("AStringAttribute") as NamedTypeSymbol;
            var aTypeClass = assembly1.Modules[0].GlobalNamespace.GetMember("ATypeAttribute") as NamedTypeSymbol;

            // Check attributes on module
            var aBoolInst = module0.GetAttribute(aBoolClass);
            aBoolInst.VerifyValue(0, TypedConstantKind.Primitive, true);

            var aByteInst = module0.GetAttribute(aByteClass);
            aByteInst.VerifyValue(0, TypedConstantKind.Primitive, Convert.ToByte(1));

            var aCharInst = module0.GetAttribute(aCharClass);
            aCharInst.VerifyValue(0, TypedConstantKind.Primitive, 'a');

            var aSingleInst = module0.GetAttribute(aSingleClass);
            aSingleInst.VerifyValue(0, TypedConstantKind.Primitive, 3.14159f);

            var aDoubleInst = module0.GetAttribute(aDoubleClass);
            aDoubleInst.VerifyValue(0, TypedConstantKind.Primitive, 3.1415926);

            var aInt16Inst = module0.GetAttribute(aInt16Class);
            aInt16Inst.VerifyValue(0, TypedConstantKind.Primitive, (Int16)16);

            var aInt32Inst = module0.GetAttribute(aInt32Class);
            aInt32Inst.VerifyValue(0, TypedConstantKind.Primitive, 32);

            var aInt64Inst = module0.GetAttribute(aInt64Class);
            aInt64Inst.VerifyValue(0, TypedConstantKind.Primitive, 64L);

            var aObjectInst = module0.GetAttribute(aObjectClass);
            aObjectInst.VerifyValue(0, TypedConstantKind.Primitive, "object");

            var aStringInst = module0.GetAttribute(aStringClass);
            aStringInst.VerifyValue(0, TypedConstantKind.Primitive, "module");

            var aTypeInst = module0.GetAttribute(aTypeClass);
            aTypeInst.VerifyValue(0, TypedConstantKind.Type, typeof(string));
        }

        [Fact]
        public void TestAttributesOnClassAndMembers()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                TestReferences.SymbolsTests.Metadata.MDTestAttributeDefLib,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            //<AString("C1")>
            //Public Class C1
            //    <AString("InnerC1")>
            //    Public Class InnerC1(of t1)
            //        <AString("InnerC2")>
            //        Public class InnerC2(of s1, s2)
            //        End class
            //    End Class

            //    <AString("field1")>
            //    Public field1 As integer

            //    <AString("Property1")>
            //    Public Property Property1 As Integer

            //    <AString("Sub1")>
            //    Public Sub Sub1(<AString("p1")> p1 As Integer)
            //    End Sub

            //    <AString("Function1")>
            //    Public Function Function1(<AString("p1")> p1 As Integer) As <AString("Integer")> Integer
            //        Return 0
            //    End Function
            //End Class

            var c1 = (NamedTypeSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMember("C1");
            var topLevel = (NamedTypeSymbol)assemblies[1].Modules[0].GlobalNamespace.GetMember("TopLevelClass");
            var aNestedAttribute = (NamedTypeSymbol)topLevel.GetMember("ANestedAttribute");

            c1.GetAttributes().First().VerifyValue(0, TypedConstantKind.Primitive, "C1");

            var innerC1 = c1.GetTypeMembers("InnerC1").Single();
            innerC1.GetAttributes().First().VerifyValue(0, TypedConstantKind.Primitive, "InnerC1");

            //<TopLevelClass.ANested(True)> 
            //Public Class InnerC1(of t1)
            Assert.Equal(aNestedAttribute, ((CSharpAttributeData)innerC1.GetAttributes(aNestedAttribute).Single()).AttributeClass);

            var innerC2 = innerC1.GetTypeMembers("InnerC2").Single();
            innerC2.GetAttributes().First().VerifyValue(0, TypedConstantKind.Primitive, "InnerC2");

            var field1 = (FieldSymbol)c1.GetMember("field1");
            field1.GetAttributes().First().VerifyValue(0, TypedConstantKind.Primitive, "field1");

            var property1 = (PropertySymbol)c1.GetMember("Property1");
            property1.GetAttributes().First().VerifyValue(0, TypedConstantKind.Primitive, "Property1");

            var sub1 = (MethodSymbol)c1.GetMember("Sub1");
            sub1.GetAttributes().First().VerifyValue(0, TypedConstantKind.Primitive, "Sub1");

            var sub1P1 = sub1.Parameters.Single(p => p.Name == "p1");
            sub1P1.GetAttributes().First().VerifyValue(0, TypedConstantKind.Primitive, "p1");

            var function1 = (MethodSymbol)c1.GetMember("Function1");
            function1.GetAttributes().First().VerifyValue(0, TypedConstantKind.Primitive, "Function1");
        }

        [Fact]
        public void TestNamedAttributes()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                TestReferences.SymbolsTests.Metadata.MDTestAttributeDefLib,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var aBoolClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("ABooleanAttribute") as NamedTypeSymbol;
            var aByteClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("AByteAttribute") as NamedTypeSymbol;
            var aCharClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("ACharAttribute") as NamedTypeSymbol;
            var aEnumClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("AEnumAttribute") as NamedTypeSymbol;
            var aSingleClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("ASingleAttribute") as NamedTypeSymbol;
            var aDoubleClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("ADoubleAttribute") as NamedTypeSymbol;
            var aInt16Class = assemblies[1].Modules[0].GlobalNamespace.GetMember("AInt16Attribute") as NamedTypeSymbol;
            var aInt32Class = assemblies[1].Modules[0].GlobalNamespace.GetMember("AInt32Attribute") as NamedTypeSymbol;
            var aInt64Class = assemblies[1].Modules[0].GlobalNamespace.GetMember("AInt64Attribute") as NamedTypeSymbol;
            var aObjectClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("AObjectAttribute") as NamedTypeSymbol;
            var aStringClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("AStringAttribute") as NamedTypeSymbol;
            var aTypeClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("ATypeAttribute") as NamedTypeSymbol;

            var c3 = (NamedTypeSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMember("C3");

            //<ABoolean(False, B:=True)> <AByte(0, B:=1)> 
            //<AChar("a"c, C:="b"c)>
            //<AEnum(TestAttributeEnum.No, e := TestAttributeEnum.Yes) 
            //<AInt16(16, I:=16)> <AInt32(32, I:=32)>
            //<AInt64(64, I:=64)> <ASingle(3.1459, S:=3.14159)> 
            //<ADouble(3.1415926, D:=3.1415926)> 
            //<AString("hello", S:="world")> 
            //<AType(GetType(C1), T:=GetType(C3))>

            // Check named value on attributes on c3
            var a = c3.GetAttribute(aBoolClass);
            a.VerifyNamedArgumentValue(0, "B", TypedConstantKind.Primitive, true);

            a = c3.GetAttribute(aByteClass);
            a.VerifyNamedArgumentValue(0, "B", TypedConstantKind.Primitive, Convert.ToByte(1));

            a = c3.GetAttribute(aCharClass);
            a.VerifyNamedArgumentValue(0, "C", TypedConstantKind.Primitive, 'b');

            a = c3.GetAttribute(aEnumClass);
            a.VerifyNamedArgumentValue(0, "E", TypedConstantKind.Enum, 0);

            a = c3.GetAttribute(aSingleClass);
            a.VerifyNamedArgumentValue(0, "S", TypedConstantKind.Primitive, 3.14159f);

            a = c3.GetAttribute(aDoubleClass);
            a.VerifyNamedArgumentValue(0, "D", TypedConstantKind.Primitive, 3.1415926);

            a = c3.GetAttribute(aInt16Class);
            a.VerifyNamedArgumentValue(0, "I", TypedConstantKind.Primitive, (Int16)16);

            a = c3.GetAttribute(aInt32Class);
            a.VerifyNamedArgumentValue(0, "I", TypedConstantKind.Primitive, 32);

            a = c3.GetAttribute(aInt64Class);
            a.VerifyNamedArgumentValue(0, "I", TypedConstantKind.Primitive, 64L);

            a = c3.GetAttribute(aTypeClass);
            a.VerifyNamedArgumentValue(0, "T", TypedConstantKind.Type, c3);
        }

        [Fact]
        public void TestNamedAttributesWithArrays()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                TestReferences.SymbolsTests.Metadata.MDTestAttributeDefLib,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var aBoolClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("ABooleanAttribute") as NamedTypeSymbol;
            var aByteClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("AByteAttribute") as NamedTypeSymbol;
            var aCharClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("ACharAttribute") as NamedTypeSymbol;
            var aEnumClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("AEnumAttribute") as NamedTypeSymbol;
            var aSingleClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("ASingleAttribute") as NamedTypeSymbol;
            var aDoubleClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("ADoubleAttribute") as NamedTypeSymbol;
            var aInt16Class = assemblies[1].Modules[0].GlobalNamespace.GetMember("AInt16Attribute") as NamedTypeSymbol;
            var aInt32Class = assemblies[1].Modules[0].GlobalNamespace.GetMember("AInt32Attribute") as NamedTypeSymbol;
            var aInt64Class = assemblies[1].Modules[0].GlobalNamespace.GetMember("AInt64Attribute") as NamedTypeSymbol;
            var aObjectClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("AObjectAttribute") as NamedTypeSymbol;
            var aStringClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("AStringAttribute") as NamedTypeSymbol;
            var aTypeClass = assemblies[1].Modules[0].GlobalNamespace.GetMember("ATypeAttribute") as NamedTypeSymbol;

            var c4 = (NamedTypeSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMember("C4");

            // Check named value on attributes on c4

            //<AInt32(0, IA := {1,2})>
            var a = c4.GetAttribute(aInt32Class);
            a.VerifyNamedArgumentValue(0, "IA", TypedConstantKind.Array, new int[] { 1, 2 });

            //<AEnum(TestAttributeEnum.No, ea:={TestAttributeEnum.Yes, TestAttributeEnum.No})>
            a = c4.GetAttribute(aEnumClass);
            a.VerifyNamedArgumentValue(0, "EA", TypedConstantKind.Array, new int[] { 0, 1 });

            //<AString("No", sa:={"Yes", "No"})>
            a = c4.GetAttribute(aStringClass);
            a.VerifyNamedArgumentValue(0, "SA", TypedConstantKind.Array, new string[] { "Yes", "No" });

            //<AObject("No", oa :={CType("Yes", Object), CType("No", Object)})>
            a = c4.GetAttribute(aObjectClass);
            a.VerifyNamedArgumentValue(0, "OA", TypedConstantKind.Array, new string[] { "Yes", "No" });

            //<AType(GetType(C1), ta:={GetType(C1), GetType(C3)})>
            var c1 = (TypeSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMember("C1");
            var c3 = (TypeSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMember("C3");
            a = c4.GetAttribute(aTypeClass);
            a.VerifyNamedArgumentValue(0, "TA", TypedConstantKind.Array, new TypeSymbol[] { c1, c3 });
        }

        [Fact]
        public void TestAttributesOnReturnTypes()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                TestReferences.SymbolsTests.Metadata.MDTestAttributeDefLib,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            //<AString("C1")>
            //Public Class C1
            //    <AString("InnerC1")>
            //    Public Class InnerC1(of t1)
            //        <AString("InnerC2")>
            //        Public class InnerC2(of s1, s2)
            //        End class
            //    End Class

            //    <AString("field1")>
            //    Public field1 As integer

            //    <AString("Property1")>
            //    Public Property Property1 As <AString("Integer")> Integer

            //    <AString("Sub1")>
            //    Public Sub Sub1(<AString("p1")> p1 As Integer)
            //    End Sub

            //    <AString("Function1")>
            //    Public Function Function1(<AString("p1")> p1 As Integer) As <AString("Integer")> Integer
            //        Return 0
            //    End Function
            //End Class

            var c1 = (NamedTypeSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMembers("C1").Single();
            c1.GetAttributes().First().VerifyValue(0, TypedConstantKind.Primitive, "C1");

            var property1 = (PropertySymbol)c1.GetMember("Property1");
            var attr = property1.GetMethod.GetReturnTypeAttributes().First();
            Assert.Equal("Integer", attr.CommonConstructorArguments.Single().Value);

            Assert.Equal(0, property1.SetMethod.GetReturnTypeAttributes().Length);

            var function1 = (MethodSymbol)c1.GetMember("Function1");
            attr = function1.GetReturnTypeAttributes().First();
            Assert.Equal("Integer", attr.CommonConstructorArguments.Single().Value);

            var sub1 = (MethodSymbol)c1.GetMember("Sub1");
            Assert.Equal(0, sub1.GetReturnTypeAttributes().Length);
        }

        [Fact]
        public void TestAttributesWithTypesAndArrays()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.MDTestAttributeApplicationLib,
                TestReferences.SymbolsTests.Metadata.MDTestAttributeDefLib,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            //Public Class C2(Of T1)
            //     Custom attributes with generics

            //    <AType(GetType(List(Of )))>
            //    Public L1 As List(Of T1)

            //    <AType(GetType(List(Of C1)))>
            //    Public L2 As List(Of C1)

            //    <AType(GetType(List(Of String)))>
            //    Public L3 As List(Of String)

            //    <AType(GetType(List(Of KeyValuePair(Of C1, string))))>
            //    Public L4 As List(Of KeyValuePair(Of C1, string))

            //    <AType(GetType(List(Of KeyValuePair(Of String, C1.InnerC1(of integer).InnerC2(of string, string)))))>
            //    Public L5 As List(Of KeyValuePair(Of String, C1.InnerC1(of integer).InnerC2(of string, string)))

            var c2 = (NamedTypeSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMembers("C2").Single();

            var l = (FieldSymbol)c2.GetMember("L1");
            l.GetAttributes().First().VerifyValue(0, TypedConstantKind.Type, "System.Collections.Generic.List<>");

            l = (FieldSymbol)c2.GetMember("L2");
            l.GetAttributes().First().VerifyValue(0, TypedConstantKind.Type, "System.Collections.Generic.List<C1>");

            l = (FieldSymbol)c2.GetMember("L3");
            l.GetAttributes().First().VerifyValue(0, TypedConstantKind.Type, "System.Collections.Generic.List<System.String>");

            l = (FieldSymbol)c2.GetMember("L4");
            l.GetAttributes().First().VerifyValue(0, TypedConstantKind.Type, "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<C1, System.String>>");

            l = (FieldSymbol)c2.GetMember("L5");
            l.GetAttributes().First().VerifyValue(0, TypedConstantKind.Type, "System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.String, C1.InnerC1<System.Int32>.InnerC2<System.String, System.String>>>");

            //    Arrays

            //<AInt32(New Integer() {1, 2})>
            //Public A1 As Type()

            //<AType(new Type(){GetType(string)})>
            //Public A2 As Object()

            //<AObject(new Type(){GetType(string)})>
            //Public A3 As Object()

            //<AObject(new Object(){GetType(string)})>
            //Public A4 As Object()

            //<AObject(new Object(){new Object() {GetType(string)}})>
            //Public A5 As Object()

            //<AObject({1, "two", GetType(string), 3.1415926})>
            //Public A6 As Object()

            //<AObject({1, new Object(){2,3,4}, 5})>
            //Public A7 As Object()

            //<AObject(new Integer(){1,2,3})>
            //Public A8 As Object()   

            var stringType = typeof(string);
            // DirectCast(assemblies(0).Modules(0), PEModuleSymbol).GetCorLibType(SpecialType.System_string)

            var field = c2.GetMember<FieldSymbol>("A1");
            var arg = field.GetAttributes().Single();
            arg.VerifyValue(0, TypedConstantKind.Array, new int[] { 1, 2 });

            field = c2.GetMember<FieldSymbol>("A2");
            arg = field.GetAttributes().Single();
            arg.VerifyValue(0, TypedConstantKind.Array, new object[] { stringType });

            field = c2.GetMember<FieldSymbol>("A3");
            arg = field.GetAttributes().Single();
            arg.VerifyValue(0, TypedConstantKind.Array, new object[] { stringType });

            field = c2.GetMember<FieldSymbol>("A4");
            arg = field.GetAttributes().Single();
            arg.VerifyValue(0, TypedConstantKind.Array, new object[] { stringType });

            field = c2.GetMember<FieldSymbol>("A5");
            arg = field.GetAttributes().Single();
            arg.VerifyValue(0, TypedConstantKind.Array, new object[] { new object[] { stringType } });

            field = c2.GetMember<FieldSymbol>("A6");
            var t = field.GetAttributes().First().CommonConstructorArguments.Single().Type;
            Assert.Equal("object[]", t.ToDisplayString());
            arg = field.GetAttributes().Single();
            arg.VerifyValue(0, TypedConstantKind.Array, new object[] { 1, "two", stringType, 3.1415926 });

            field = c2.GetMember<FieldSymbol>("A7");
            arg = field.GetAttributes().Single();
            t = arg.CommonConstructorArguments.Single().Type;
            Assert.Equal("object[]", t.ToDisplayString());
            arg.VerifyValue(0, TypedConstantKind.Array, new object[] { 1, new object[] { 2, 3, 4 }, 5 });

            field = c2.GetMember<FieldSymbol>("A8");
            arg = field.GetAttributes().Single();
            t = arg.CommonConstructorArguments.Single().Type;
            Assert.Equal("int[]", t.ToDisplayString());
            arg.VerifyValue(0, TypedConstantKind.Array, new int[] { 1, 2, 3 });
        }

        public struct AttributeArgs
        {
            public string[] Pos;

            public KeyValuePair<string, string>[] Named;
            public AttributeArgs(string[] p, KeyValuePair<string, string>[] n)
            {
                this.Pos = p;
                this.Named = n;
            }
        }

        [Fact]
        public void TestDumpAllAttributesTesLib()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.MDTestAttributeDefLib ,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var assemblyArgs = new AttributeArgs[] {
                new AttributeArgs(new string[]{ "8"} , null),
                new AttributeArgs(null, new KeyValuePair<string, string>[]{  new KeyValuePair<string, string>("WrapNonExceptionThrows", "True")} )
            };

            CheckAttributes(assemblies[0], assemblyArgs);
        }

        private void CheckAttributes(Symbol s, AttributeArgs[] expected)
        {
            int i = 0;
            foreach (var sa in s.GetAttributes())
            {
                int j = 0;
                foreach (var pa in sa.CommonConstructorArguments)
                {
                    CheckConstructorArg(expected[i].Pos[j], pa.Value.ToString());
                    j += 1;
                }

                j = 0;
                foreach (var na in sa.CommonNamedArguments)
                {
                    CheckNamedArg(expected[i].Named[j], na);
                    j += 1;
                }
                i += 1;
            }
        }

        private static void CheckConstructorArg(string expected, object actual)
        {
            Assert.Equal(expected, actual.ToString());
        }

        private static void CheckNamedArg(KeyValuePair<string, string> expected, KeyValuePair<string, TypedConstant> actual)
        {
            Assert.Equal(expected.Key, actual.Key.ToString());
            Assert.Equal(expected.Value, actual.Value.Value.ToString());
        }

        [Fact]
        public void TestInteropAttributesAssembly()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.AttributeInterop01,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            //[assembly: ImportedFromTypeLib("InteropAttributes")]
            //[assembly: PrimaryInteropAssembly(1, 2)]
            //[assembly: Guid("1234C65D-1234-447A-B786-64682CBEF136")]
            //[assembly: BestFitMapping(false, ThrowOnUnmappableChar = true)]

            //[assembly: AutomationProxy(false)]
            //[assembly: ClassInterface(ClassInterfaceType.AutoDual)]
            //[assembly: ComCompatibleVersion(1, 2, 3, 4)]
            //[assembly: ComConversionLoss()] 
            //[assembly: ComVisible(true)]
            //[assembly: TypeLibVersion(1, 0)]
            var asm = (AssemblySymbol)assemblies[0];

            var attrs = asm.GetAttributes();
            // 10 + 2 compiler inserted
            Assert.Equal(12, attrs.Length);
            foreach (var a in attrs)
            {
                switch (a.AttributeClass.Name)
                {
                    case "ImportedFromTypeLibAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, "InteropAttributes");
                        break;
                    case "PrimaryInteropAssemblyAttribute":
                        a.VerifyValue(1, TypedConstantKind.Primitive, 2);
                        break;
                    case "GuidAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, "1234C65D-1234-447A-B786-64682CBEF136");
                        break;
                    case "BestFitMappingAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, false);
                        a.VerifyNamedArgumentValue(0, "ThrowOnUnmappableChar", TypedConstantKind.Primitive, true);
                        break;
                    case "AutomationProxyAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, false);
                        break;
                    case "ClassInterfaceAttribute":
                        Assert.Equal(1, a.CommonConstructorArguments.Length);
                        Assert.Equal(0, a.CommonNamedArguments.Length);
                        // enum is stored as its underneath type
                        Assert.Equal("System.Runtime.InteropServices.ClassInterfaceType", a.CommonConstructorArguments[0].Type.ToDisplayString());
                        // ClassInterfaceType.AutoDual
                        Assert.Equal(2, a.CommonConstructorArguments[0].Value);
                        break;
                    case "ComCompatibleVersionAttribute":
                        a.VerifyValue(2, TypedConstantKind.Primitive, 3);
                        break;
                    case "ComConversionLossAttribute":
                        Assert.Equal(0, a.CommonConstructorArguments.Length);
                        Assert.Equal(0, a.CommonNamedArguments.Length);
                        break;
                    case "ComVisibleAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, true);
                        break;
                    case "TypeLibVersionAttribute":
                        Assert.Equal(0, a.CommonNamedArguments.Length);
                        Assert.Equal(2, a.CommonConstructorArguments.Length);
                        break;
                    case "CompilationRelaxationsAttribute":
                        a.VerifyValue(0, TypedConstantKind.Primitive, 8);
                        break;
                    case "RuntimeCompatibilityAttribute":
                        a.VerifyNamedArgumentValue(0, "WrapNonExceptionThrows", TypedConstantKind.Primitive, true);
                        break;
                    default:
                        Assert.Equal("Unexpected Attr", a.AttributeClass.Name);
                        break;
                }
            }
        }

        /// Did not Skip the test - will remove the explicit cast (from IMethodSymbol to MethodSymbol) once this bug is fixed
        [Fact]
        public void TestInteropAttributesInterface()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.AttributeInterop01,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            //[ComImport, Guid("ABCDEF5D-2448-447A-B786-64682CBEF123")]
            //[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
            //[TypeLibImportClass(typeof(object)), TypeLibType(TypeLibTypeFlags.FAggregatable)]
            //public interface IFoo
            //{
            //    [AllowReversePInvokeCalls()]
            //    void DoSomething();
            //    [ComRegisterFunction()]
            //    void Register(object o);
            //    [ComUnregisterFunction()]
            //    void UnRegister();
            //    [TypeLibFunc(TypeLibFuncFlags.FDefaultBind)]
            //    void LibFunc();
            //}

            var sysNS = (NamespaceSymbol)assemblies[1].GlobalNamespace.GetMember("System");
            var runtimeNS = (NamespaceSymbol)sysNS.GetMember("Runtime");
            var interopNS = (NamespaceSymbol)runtimeNS.GetMember("InteropServices");

            var appNS = (NamespaceSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMember("Interop");
            var ifoo = (NamedTypeSymbol)appNS.GetMember("IFoo");
            // ComImport is Pseudo attr
            Assert.Equal(4, ifoo.GetAttributes().Length);

            // get attr by NamedTypeSymbol
            var attrObj = (NamedTypeSymbol)interopNS.GetTypeMembers("GuidAttribute").Single();
            var attrSym = ifoo.GetAttribute(attrObj);
            //Assert.Null(attrSym.NamedArguments)
            attrSym.VerifyValue(0, TypedConstantKind.Primitive, "ABCDEF5D-2448-447A-B786-64682CBEF123");

            attrObj = (NamedTypeSymbol)interopNS.GetTypeMembers("InterfaceTypeAttribute").Single();
            // use first ctor
            var ctor = attrObj.InstanceConstructors.First();
            attrSym = ifoo.GetAttribute(ctor);
            // param in ctor is Int16, but Int32 in MD
            Assert.Equal(typeof(Int32), attrSym.CommonConstructorArguments[0].Value.GetType());
            Assert.Equal(1, attrSym.CommonConstructorArguments[0].Value);

            attrObj = (NamedTypeSymbol)interopNS.GetTypeMembers("TypeLibImportClassAttribute").Single();
            var msym = attrObj.InstanceConstructors.First();
            attrSym = ifoo.GetAttribute(msym);
            Assert.Equal("object", ((Symbol)attrSym.CommonConstructorArguments[0].Value).ToString());

            // =============================
            var mem = (MethodSymbol)ifoo.GetMember("DoSomething");
            Assert.Equal(1, mem.GetAttributes().Length);
            mem = (MethodSymbol)ifoo.GetMember("Register");
            Assert.Equal(1, mem.GetAttributes().Length);
            mem = (MethodSymbol)ifoo.GetMember("UnRegister");
            Assert.Equal(1, mem.GetAttributes().Length);
            mem = (MethodSymbol)ifoo.GetMember("LibFunc");
            attrSym = mem.GetAttributes().First();
            Assert.Equal(1, attrSym.CommonConstructorArguments.Length);
            Assert.Equal(32, attrSym.CommonConstructorArguments[0].Value);
        }

        [Fact]
        public void TestInteropAttributesDelegate()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.AttributeInterop01,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            // [Serializable, ComVisible(false)]
            // [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall, BestFitMapping = true, CharSet = CharSet.Ansi, SetLastError = true, ThrowOnUnmappableChar = true)]
            // public delegate void DFoo(char p1, sbyte p2);

            var sysNS = (NamespaceSymbol)assemblies[1].GlobalNamespace.GetMember("System");
            var runtimeNS = (NamespaceSymbol)sysNS.GetMember("Runtime");
            var interopNS = (NamespaceSymbol)runtimeNS.GetMember("InteropServices");

            var appNS = (NamespaceSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMember("Interop");
            var dfoo = (NamedTypeSymbol)appNS.GetMember("DFoo");
            // Pseudo - Serializable
            Assert.Equal(2, dfoo.GetAttributes().Length);

            // get attr by NamedTypeSymbol
            var attrObj = (NamedTypeSymbol)interopNS.GetTypeMembers("ComVisibleAttribute").Single();
            var attrSym = dfoo.GetAttribute(attrObj);
            attrSym.VerifyValue(0, TypedConstantKind.Primitive, false);

            attrObj = (NamedTypeSymbol)interopNS.GetTypeMembers("UnmanagedFunctionPointerAttribute").Single();
            attrSym = dfoo.GetAttribute(attrObj);
            //Assert.Equal(1, attrSym.ConstructorArguments.Count)
            Assert.Equal(3, attrSym.CommonConstructorArguments[0].Value);

            Assert.Equal(4, attrSym.CommonNamedArguments.Length);
            Assert.Equal("BestFitMapping", attrSym.CommonNamedArguments[0].Key);
            Assert.Equal(true, attrSym.CommonNamedArguments[0].Value.Value);
            attrSym.VerifyNamedArgumentValue(0, "BestFitMapping", TypedConstantKind.Primitive, true);
            attrSym.VerifyNamedArgumentValue(1, "CharSet", TypedConstantKind.Enum, (int)CharSet.Ansi);
            attrSym.VerifyNamedArgumentValue(2, "SetLastError", TypedConstantKind.Primitive, true);
            attrSym.VerifyNamedArgumentValue(3, "ThrowOnUnmappableChar", TypedConstantKind.Primitive, true);
        }

        [Fact]
        public void TestInteropAttributesEnum()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.AttributeInterop02,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            // [Guid("31230DD5-2448-447A-B786-64682CBEFEEE"), Flags]
            // public enum MyEnum : sbyte  { 
            //    [NonSerialized]zero = 0, one = 1, two = 2, [Obsolete("message", false)]three = 4 
            // }

            var sysNS = (NamespaceSymbol)assemblies[1].GlobalNamespace.GetMember("System");
            var runtimeNS = (NamespaceSymbol)sysNS.GetMember("Runtime");
            var interopNS = (NamespaceSymbol)runtimeNS.GetMember("InteropServices");

            var modattr = assemblies[0].Modules[0].GetAttributes().First();
            Assert.Equal("UnverifiableCodeAttribute", modattr.AttributeClass.Name);

            var appNS = (NamespaceSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMember("EventNS");
            var myEnum = (NamedTypeSymbol)appNS.GetMember("MyEnum");
            // 
            Assert.Equal(2, myEnum.GetAttributes().Length);

            var field = (FieldSymbol)myEnum.GetMember("zero");
            // Pseudo: NonSerialized
            Assert.Equal(0, field.GetAttributes().Length);

            field = (FieldSymbol)myEnum.GetMember("three");
            Assert.Equal(1, field.GetAttributes().Length);
            var attrSym = field.GetAttributes().First();
            Assert.Equal("ObsoleteAttribute", attrSym.AttributeClass.Name);
            attrSym.VerifyValue(0, TypedConstantKind.Primitive, "message");
            attrSym.VerifyValue(1, TypedConstantKind.Primitive, false);
        }

        [Fact]
        public void TestInteropAttributesMembers()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.AttributeInterop01,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            //[ComImport, TypeLibType(TypeLibTypeFlags.FAggregatable)]
            //[Guid("A88A175D-2448-447A-B786-CCC82CBEF156"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
            //[CoClass(typeof(CBar))]
            //public interface IBar             '{
            //    [DispId(10)]
            //    long MarshalAsGetProperty { [return: MarshalAs(UnmanagedType.I8)] get; }

            //    [DispId(20), IndexerNameAttribute("MyIndex")]
            //    int this[int idx] { get; set; }

            //    [DispId(30), PreserveSig]
            //    int MixedAttrMethod1([In] [MarshalAs(UnmanagedType.U4)] uint v1, [In, Out][MarshalAs(UnmanagedType.I4)] ref int v2);

            //    [DispId(40), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            //    void IDispatchParameters([MarshalAs(UnmanagedType.IDispatch)] object v1, [Out] [MarshalAs(UnmanagedType.IUnknown)] out object v2);

            //    [DispId(50), TypeLibFunc(TypeLibFuncFlags.FBindable)]
            //    void SCodeParameter([MarshalAs(UnmanagedType.Error)] int v1);

            //    [DispId(60)]
            //    [return: MarshalAs(UnmanagedType.BStr)]
            //    string VariantParameters([MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = "YumYum", MarshalType = "IUnknown")] object v1, [In][Out] ref object v2);

            //    [LCIDConversion(1)]
            //    void DecimalStringParameter([In] decimal v1, [MarshalAs(UnmanagedType.LPStr)] string v2, [MarshalAs(UnmanagedType.LPWStr)] string v3);
            //    void CurrencyParameter([In, MarshalAs(UnmanagedType.Currency)] decimal v1);
            //    // int MixedAttrMethod([In] [ComAliasName(stdole.OLE_COLOR)]uint v1, [In][Out][MarshalAs(UnmanagedType.I4)] ref int v2);
            //}

            var sysNS = (NamespaceSymbol)assemblies[1].GlobalNamespace.GetMember("System");
            var runtimeNS = (NamespaceSymbol)sysNS.GetMember("Runtime");
            var interopNS = (NamespaceSymbol)runtimeNS.GetMember("InteropServices");
            var reflectNS = (NamespaceSymbol)sysNS.GetMember("Reflection");

            var appNS = (NamespaceSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMember("Interop");
            // 
            var ibar = (NamedTypeSymbol)appNS.GetMember("IBar");
            // Pseudo - ComImport ( 4 + 1 -> DefaultMember)
            Assert.Equal(5, ibar.GetAttributes().Length);
            var atts = ibar.GetAttributes();
            // get attr by NamedTypeSymbol
            var attrObj = (NamedTypeSymbol)interopNS.GetTypeMembers("CoClassAttribute").Single();
            var attrSym = ibar.GetAttribute(attrObj);
            var cbar = (NamedTypeSymbol)appNS.GetMember("CBar");
            attrSym.VerifyValue(0, TypedConstantKind.Type, cbar);

            attrObj = (NamedTypeSymbol)reflectNS.GetTypeMembers("DefaultMemberAttribute").Single();
            attrSym = ibar.GetAttribute(attrObj);
            attrSym.VerifyValue(0, TypedConstantKind.Primitive, "MyIndex");

            //===================
            // Members
            var mem = (PropertySymbol)ibar.GetMember("MarshalAsGetProperty");
            mem.GetAttributes().First().VerifyValue(0, TypedConstantKind.Primitive, 10);

            // attribute nor work on return type
            //attrSym = mem.Type.GetAttribute(attrObj)
            // TODO: index

            var mem2 = (MethodSymbol)ibar.GetMember("MixedAttrMethod1");
            // Pseudo: PreserveSig
            Assert.Equal(1, mem2.GetAttributes().Length);

            mem2 = (MethodSymbol)ibar.GetMember("IDispatchParameters");
            // Pseudo: MethodImpl
            Assert.Equal(1, mem2.GetAttributes().Length);
            // ? Pseudo: Out & MarshalAs
            //Assert.Equal(2, mem2.Parameters.Count)
            //Assert.Equal(2, mem2.Parameters(1).GetAttributes().Count)

            //attrSym = mem2.Parameters(1).GetAttributes().First()
            //Dim attrSym2 = mem2.Parameters(1).GetAttributes().Last()
            //' swap
            //If (attrSym2.AttributeClass.Name = "OutAttribute") Then
            //    Dim tmp = attrSym
            //    attrSym = attrSym2
            //    attrSym2 = tmp
            //End If

            //'attrObj = DirectCast(interopNS.GetTypeMembers("MarshalAsAttribute").Single(), NamedTypeSymbol)
            //Assert.Equal("MarshalAsAttribute", attrSym2.AttributeClass.Name) Assert.Equal(1,
            //attrSym2.ConstructorArguments(0).Value)

            mem2 = (MethodSymbol)ibar.GetMember("DecimalStringParameter");
            Assert.Equal(1, mem2.GetAttributes().Length);
            attrSym = mem2.GetAttributes().First();
            Assert.Equal("LCIDConversionAttribute", attrSym.AttributeClass.Name);
            attrSym.VerifyValue(0, TypedConstantKind.Primitive, 1);
        }

        [Fact]
        public void TestAttributesNames()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.AttributeTestLib01,
                TestReferences.SymbolsTests.Metadata.AttributeTestDef01,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var caNS = (NamespaceSymbol)assemblies[1].GlobalNamespace.GetMember("CustomAttribute");
            // 
            var attrObj1 = (NamedTypeSymbol)caNS.GetTypeMembers("AttrName").Single();
            var attrObj2 = (NamedTypeSymbol)caNS.GetTypeMembers("AttrNameAttribute").Single();
            //
            //[assembly: @AttrName()]
            //[assembly: @AttrName(UShortField = 321)]
            //[module: AttrNameAttribute(TypeField = typeof(Dictionary<string, int>))]

            // 2 + 2 compiler inserted
            Assert.Equal(4, assemblies[0].GetAttributes().Length);

            var attrSym = assemblies[0].GetAttribute(attrObj1);
            Assert.Equal("AttrName", attrSym.AttributeClass.Name);

            attrSym = assemblies[0].GetAttributes(attrObj1).Last();
            Assert.Equal("AttrName", attrSym.AttributeClass.Name);
            attrSym.VerifyNamedArgumentValue(0, "UShortField", TypedConstantKind.Primitive, Convert.ToUInt16(321));

            attrSym = assemblies[0].Modules[0].GetAttributes().First();
            Assert.Equal("AttrNameAttribute", attrSym.AttributeClass.Name);
            attrSym.VerifyNamedArgumentValue(0, "TypeField", TypedConstantKind.Type, typeof(Dictionary<string, int>));
            Assert.Equal(2, (attrSym.CommonNamedArguments[0].Value.Value as NamedTypeSymbol).Arity);
        }

        [Fact]
        public void TestAttributesOnTypeParameters()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.Metadata.AttributeTestLib01 ,
                TestReferences.SymbolsTests.Metadata.AttributeTestDef01 ,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var caNS = (NamespaceSymbol)assemblies[1].GlobalNamespace.GetMember("CustomAttribute");

            var attrObj1 = (NamedTypeSymbol)caNS.GetTypeMembers("AllInheritMultipleAttribute").Single();
            var attrObj2 = (NamedTypeSymbol)caNS.GetTypeMembers("DerivedAttribute").Single();

            var appNS = (NamespaceSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMember("AttributeUse");

            //public interface IFoo<[typevar: AllInheritMultiple(3.1415926)] T, [AllInheritMultiple('q', 2)] V>
            //{
            //    // default: method
            //    [AllInheritMultiple(p3:1.234f, p2: 1056, p1: "555")]
            //    // attribute on return, param
            //    [return: AllInheritMultiple("obj", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static)]
            //    V Method([param: DerivedAttribute(new sbyte[] {-1, 0, 1}, ObjectField = typeof(IList<>))]T t);
            //}
            // 
            var ifoo = (NamedTypeSymbol)appNS.GetMember("IFoo");
            // attribute on type parameter of interface
            var tp = ifoo.TypeParameters[0];
            var attrSym = tp.GetAttributes().First();
            Assert.Equal("AllInheritMultipleAttribute", attrSym.AttributeClass.Name);
            // p2 is optional
            Assert.Equal(2, attrSym.CommonConstructorArguments.Length);
            attrSym.VerifyValue(0, TypedConstantKind.Primitive, 3.1415926);

            //object
            // NYI: default optional
            // Assert.Equal(CByte(1), attrSym.ConstructorArguments[1].Value) 'enum

            tp = ifoo.TypeParameters[1];
            attrSym = tp.GetAttribute(attrObj1);
            Assert.Equal(3, attrSym.CommonConstructorArguments.Length);
            attrSym.VerifyValue(0, TypedConstantKind.Primitive, 'q');
            attrSym.VerifyValue(1, TypedConstantKind.Primitive, Convert.ToByte(2));
            // NYI: optional
            //Assert.Equal(CSByte(-1), attrSym.ConstructorArguments(2).Value)

            // attribute on method
            // [AllInheritMultiple(p3:1.234f, p2: 1056, p1: "555")]
            var mtd = (MethodSymbol)ifoo.GetMember("Method");
            Assert.Equal(1, mtd.GetAttributes().Length);
            attrSym = mtd.GetAttributes().First();
            Assert.Equal(4, attrSym.CommonConstructorArguments.Length);
            // p4 is default optional
            Assert.Equal("555", attrSym.CommonConstructorArguments[0].Value);
            attrSym.VerifyValue(0, TypedConstantKind.Primitive, "555");

            // object

            // [return: AllInheritMultiple("obj", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static)]
            attrSym = mtd.GetReturnTypeAttributes().First();
            Assert.Equal(2, attrSym.CommonConstructorArguments.Length);
            Assert.Equal("obj", attrSym.CommonConstructorArguments[0].Value);
            Assert.Equal(12, attrSym.CommonConstructorArguments[1].Value);

            // [param: DerivedAttribute(new sbyte[] {-1, 0, 1}, ObjectField = typeof(IList<>))]
            attrSym = mtd.Parameters[0].GetAttribute(attrObj2);
            Assert.Equal(1, attrSym.CommonConstructorArguments.Length);
            Assert.Equal(1, attrSym.CommonNamedArguments.Length);
            Assert.Equal("sbyte[]", attrSym.CommonConstructorArguments[0].Type.ToDisplayString());
            attrSym.VerifyValue(0, TypedConstantKind.Array, new sbyte[] { -1, 0, 1 });

            Assert.Equal("ObjectField", attrSym.CommonNamedArguments[0].Key);
            Assert.Equal("System.Collections.Generic.IList<>", (attrSym.CommonNamedArguments[0].Value.Value as NamedTypeSymbol).ToString());
        }

        //[AllInheritMultiple(new char[] { '1', '2' }, UIntField = 112233)]
        //[type: AllInheritMultiple(new char[] { 'a', '\0', '\t' }, AryField = new ulong[] { 0, 1, ulong.MaxValue })]
        //[AllInheritMultiple(null, "", null, "1234", AryProp = new object[2] { new ushort[] { 1 }, new ushort[] { 2, 3, 4 } })]
        //public class Foo<[typevar: AllInheritMultiple(null), AllInheritMultiple()] T> : IFoo<T, ushort>
        //{
        //    // named parameters
        //    [field: AllInheritMultiple(p2: System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public, p1: -123)]
        //    [AllInheritMultiple(p1: 111, p2: System.Reflection.BindingFlags.NonPublic)]
        //    public int ClassField;

        //    [property: BaseAttribute(-1)]
        //    public Foo<char> Prop
        //    {
        //        // return:
        //        [AllInheritMultiple(1, 2, 3), AllInheritMultiple(4, 5, 1.1f)]
        //        get;
        //        [param: DerivedAttribute(-3)]
        //        set;
        //    }

        //    [AllInheritMultiple(+007, 256)]
        //    [AllInheritMultiple(-008, 255)]
        //    [method: DerivedAttribute(typeof(IFoo<short, ushort>), ObjectField = 1)]
        //    public ushort Method(T t) { return 0; }
        //    // Explicit NotImpl
        //    // ushort IFoo<T, ushort>.Method(T t) { return 0; }
        //}
        [Fact]
        public void TestAttributesMultiples()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]{
                TestReferences.SymbolsTests.Metadata.AttributeTestLib01,
                TestReferences.SymbolsTests.Metadata.AttributeTestDef01,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var caNS = (NamespaceSymbol)assemblies[1].GlobalNamespace.GetMember("CustomAttribute");

            var attrObj1 = (NamedTypeSymbol)caNS.GetTypeMembers("AllInheritMultipleAttribute").Single();
            var mctors = attrObj1.Constructors;
            //.ToList()
            Assert.Equal(5, mctors.Length);

            var attrObj2 = (NamedTypeSymbol)caNS.GetTypeMembers("DerivedAttribute").Single();

            var appNS = (NamespaceSymbol)assemblies[0].Modules[0].GlobalNamespace.GetMember("AttributeUse");
            var foo = (NamedTypeSymbol)appNS.GetMember("Foo");
            // Attribute on class Foo

            var attrs = (from a in foo.GetAttributes()
                         where a.AttributeConstructor.Equals((MethodSymbol)mctors[4])
                         select a).ToList();

            Assert.Equal(foo.GetAttributes().Length, attrs.Count());
            var count = 0;
            foreach (var a in attrs)
            {
                var pos0 = a.CommonConstructorArguments[0].Values;
                Assert.Equal("char[]", a.CommonConstructorArguments[0].Type.ToDisplayString());
                // [AllInheritMultiple(null, "", null, "1234", AryProp = new object[2] { new ushort[] { 1 }, new ushort[] { 2, 3, 4 } })]
                if (pos0.IsDefaultOrEmpty)
                {
                    count += 1;
                    Assert.Equal("string[]", a.CommonConstructorArguments[1].Type.ToDisplayString());
                    Assert.Equal(3, a.CommonConstructorArguments[1].Values.Length);
                    var na0 = a.CommonNamedArguments[0].Value.Values;
                    Assert.Equal(2, na0.Length);
                    // jagged array
                    Assert.Equal("ushort[]", na0[1].Type.ToDisplayString());
                    var elem = na0[1].Values;
                    Assert.Equal("ushort", elem[1].Type.ToDisplayString());
                    // [AllInheritMultiple(new char[] { '1', '2' }, UIntField = 112233)]
                }
                else if (pos0.Length == 2)
                {
                    count += 2;
                    Assert.Equal(1, a.CommonNamedArguments.Length);
                    // [type: AllInheritMultiple(new char[] { 'a', '\0', '\t' }, AryField = new ulong[] { 0, 1, ulong.MaxValue })]
                }
                else if (pos0.Length == 3)
                {
                    count += 4;
                    Assert.Equal("AryField", a.CommonNamedArguments[0].Key);
                    Assert.Equal("ulong[]", a.CommonNamedArguments[0].Value.Type.ToDisplayString());
                    var na1 = a.CommonNamedArguments[0].Value.Values;
                    Assert.Equal("AryField", a.CommonNamedArguments[0].Key);
                }
                else
                {
                    count += 99;
                    // should not be here
                }
            }
            // hit 3 attr once each
            Assert.Equal(7, count);

            // attribute on type parameter of class Foo
            var tp = foo.TypeParameters[0];
            Assert.Equal(2, tp.GetAttributes().Length);

            // field
            var fld = (FieldSymbol)foo.GetMember("ClassField");
            Assert.Equal(2, fld.GetAttributes().Length);
            Assert.Equal(0, fld.GetAttributes().First().CommonNamedArguments.Length);

            // property
            var prop = (PropertySymbol)foo.GetMember("Prop");
            Assert.Equal(1, prop.GetAttributes().Length);
            Assert.Equal(-1, prop.GetAttributes().First().CommonConstructorArguments[0].Value);
            // get, set
            Assert.Equal(3, prop.GetMethod.GetAttributes().Length);
            Assert.Equal(1, prop.SetMethod.GetAttributes().Length);

            var attrSym = tp.GetAttribute(attrObj1);

            // method
            var mtd = (MethodSymbol)foo.GetMember("Method");
            Assert.Equal(3, mtd.GetAttributes().Length);

            attrs = (from a in mtd.GetAttributes()
                     where a.AttributeConstructor.Equals((MethodSymbol)mctors[2])
                     select a).ToList(); ;
            Assert.Equal(1, attrs.Count);
            // [AllInheritMultiple(-008, 255)] ' p3 is optional
            attrSym = attrs.First();
            Assert.Equal(3, attrSym.CommonConstructorArguments.Length);
            Assert.Equal(-8, attrSym.CommonConstructorArguments[0].Value);
            // object
            Assert.Equal(Convert.ToByte(255), attrSym.CommonConstructorArguments[1].Value);

            attrs = (from a in mtd.GetAttributes()
                     where a.AttributeConstructor.Equals((MethodSymbol)mctors[3])
                     select a).ToList(); ;
            Assert.Equal(1, attrs.Count);
            // [AllInheritMultiple(+007, 256)] ' p3, p4 optional
            attrSym = attrs.First();
            Assert.Equal(4, attrSym.CommonConstructorArguments.Length);
            // p4 is default optional
            Assert.Equal(7, attrSym.CommonConstructorArguments[0].Value);
            // object
            Assert.Equal(256L, attrSym.CommonConstructorArguments[1].Value);
            // default
            Assert.Equal(0.123f, attrSym.CommonConstructorArguments[2].Value);
            Assert.Equal(Convert.ToInt16(-2), attrSym.CommonConstructorArguments[3].Value);

            // [method: DerivedAttribute(typeof(IFoo<short, ushort>), ObjectField = 1)]
            attrs = mtd.GetAttributes(attrObj2).ToList();
            Assert.Equal(1, attrs.Count);
            attrSym = attrs.First();
            Assert.Equal(1, attrSym.CommonConstructorArguments.Length);
            Assert.Equal(1, attrSym.CommonNamedArguments.Length);
            Assert.Equal("AttributeUse.IFoo<System.Int16, System.UInt16>", (attrSym.CommonConstructorArguments[0].Value as NamedTypeSymbol).ToDisplayString(SymbolDisplayFormat.TestFormat));
            Assert.Equal(1, attrSym.CommonNamedArguments[0].Value.Value);
        }

        #region "Regression"

        [Fact]
        public void TestAttributesAssemblyVersionValue()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[] {
                TestReferences.NetFx.v4_0_30319.System_Core,
                TestReferences.NetFx.v4_0_30319.System,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var sysNS = (NamespaceSymbol)assemblies[2].GlobalNamespace.GetMember("System");
            var refNS = (NamespaceSymbol)sysNS.GetMember("Reflection");
            var rtNS = (NamespaceSymbol)sysNS.GetMember("Runtime");

            var asmFileAttr = (NamedTypeSymbol)refNS.GetTypeMembers("AssemblyFileVersionAttribute").Single();
            var attr1 = assemblies[0].GetAttribute(asmFileAttr);
            attr1.VerifyValue(0, TypedConstantKind.Primitive, "4.0.30319.1");

            var asmInfoAttr = (NamedTypeSymbol)refNS.GetTypeMembers("AssemblyInformationalVersionAttribute").Single();
            attr1 = assemblies[0].GetAttribute(asmInfoAttr);
            attr1.VerifyValue(0, TypedConstantKind.Primitive, "4.0.30319.1");

            var asmTgtAttr = (NamedTypeSymbol)rtNS.GetTypeMembers("AssemblyTargetedPatchBandAttribute").Single();
            attr1 = assemblies[0].GetAttribute(asmTgtAttr);
            attr1.VerifyValue(0, TypedConstantKind.Primitive, "1.0.21-0");
        }

        [Fact]
        public void TestAttributesWithTypeOfInternalClass()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]{
                TestReferences.NetFx.v4_0_30319.System_Core,
                TestReferences.NetFx.v4_0_30319.System,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var corsysNS = assemblies[2].GlobalNamespace.GetMembers("System").Single() as NamespaceSymbol;
            var diagNS = corsysNS.GetMembers("Diagnostics").Single() as NamespaceSymbol;

            var sysNS = (NamespaceSymbol)assemblies[0].GlobalNamespace.GetMember("System");
            var linqNS = (NamespaceSymbol)sysNS.GetMember("Linq");
            var exprNS = (NamespaceSymbol)linqNS.GetMember("Expressions");

            var dbgProxyAttr = (NamedTypeSymbol)diagNS.GetTypeMembers("DebuggerTypeProxyAttribute").Single();

            // [DebuggerTypeProxy(typeof(Expression.BinaryExpressionProxy))] - internal class as argument to typeof()
            // public class BinaryExpression : Expression {... }
            var attr1 = exprNS.GetTypeMembers("BinaryExpression").First().GetAttribute(dbgProxyAttr);
            Assert.Equal("System.Linq.Expressions.Expression.BinaryExpressionProxy", ((TypeSymbol)attr1.CommonConstructorArguments[0].Value).ToDisplayString(SymbolDisplayFormat.TestFormat));

            // [DebuggerTypeProxy(typeof(Expression.TypeBinaryExpressionProxy))]
            // public sealed class TypeBinaryExpression : Expression
            attr1 = exprNS.GetTypeMembers("TypeBinaryExpression").First().GetAttribute(dbgProxyAttr);
            Assert.Equal("System.Linq.Expressions.Expression.TypeBinaryExpressionProxy", ((TypeSymbol)attr1.CommonConstructorArguments[0].Value).ToDisplayString(SymbolDisplayFormat.TestFormat));
        }

        [Fact]
        public void TestAttributesStaticInstanceCtors()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.NetFx.v4_0_30319.System_Configuration,
                TestReferences.NetFx.v4_0_30319.System,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var sysNS = (NamespaceSymbol)assemblies[1].GlobalNamespace.GetMember("System");
            var secondNS = (NamespaceSymbol)sysNS.GetMember("Configuration");
            var type01 = (NamedTypeSymbol)secondNS.GetTypeMembers("SchemeSettingElement").Single();

            var mems = type01.GetMembers("GenericUriParserOptions");
            var prop = mems.First() as PropertySymbol;
            if (prop == null)
            {
                prop = mems.Last() as PropertySymbol;
            }

            //  [ConfigurationProperty("genericUriParserOptions", DefaultValue=0, IsRequired=true)]
            var attr = prop.GetAttributes().First();
            Assert.Equal("ConfigurationPropertyAttribute", attr.AttributeClass.Name);
            attr.VerifyValue(0, TypedConstantKind.Primitive, "genericUriParserOptions");
            attr.VerifyNamedArgumentValue(1, "IsRequired", TypedConstantKind.Primitive, true);
            Assert.Equal("DefaultValue", attr.CommonNamedArguments[0].Key);
            Assert.Equal(0, attr.CommonNamedArguments[0].Value.Value);
        }

        [Fact]
        public void TestAttributesOverloadedCtors()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]
            {
                TestReferences.NetFx.v4_0_30319.System_Data,
                TestReferences.NetFx.v4_0_30319.System_Core,
                TestReferences.NetFx.v4_0_30319.System,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var sysNS = (NamespaceSymbol)assemblies[0].GlobalNamespace.GetMember("System");
            var secondNS = (NamespaceSymbol)sysNS.GetMember("Data");
            var thirdNS = (NamespaceSymbol)secondNS.GetMember("Common");

            var resCatAttr = (NamedTypeSymbol)secondNS.GetTypeMembers("ResCategoryAttribute").Single();
            var resDesAttr = (NamedTypeSymbol)secondNS.GetTypeMembers("ResDescriptionAttribute").Single();
            var level01NS = (NamespaceSymbol)assemblies[2].GlobalNamespace.GetMember("System");
            var level02NS = (NamespaceSymbol)level01NS.GetMember("ComponentModel");
            var defValAttr = (NamedTypeSymbol)level02NS.GetTypeMembers("DefaultValueAttribute").Single();

            var type01 = (NamedTypeSymbol)thirdNS.GetTypeMembers("DataAdapter").Single();
            var prop = type01.GetMember("MissingMappingAction") as PropertySymbol;

            // [DefaultValue(1), ResCategory("DataCategory_Mapping"), ResDescription("DataAdapter_MissingMappingAction")]
            // public MissingMappingAction MissingMappingAction { get; set; }
            var attr = prop.GetAttributes(resCatAttr).Single();
            attr.VerifyValue(0, TypedConstantKind.Primitive, "DataCategory_Mapping");

            attr = prop.GetAttributes(resDesAttr).Single();
            attr.VerifyValue(0, TypedConstantKind.Primitive, "DataAdapter_MissingMappingAction");

            attr = prop.GetAttributes(defValAttr).Single();
            Assert.Equal(1, attr.CommonConstructorArguments.Length);
            Assert.Equal(1, attr.CommonConstructorArguments[0].Value);
        }

        #endregion

        [WorkItem(530209, "DevDiv")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void Bug530209()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Class1
       extends[mscorlib] System.Object
        {
  .field public static initonly valuetype[mscorlib]System.Decimal d1
  .custom instance void[mscorlib]
        System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                  uint8,
                                                                                                  uint32,
                                                                                                  uint32,
                                                                                                  uint32)
           = {uint8(0)
              uint8(128)
              uint32(0)
              uint32(0)
              uint32(7)}
  .custom instance void[mscorlib]
    System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                  uint8,
                                                                                                  int32,
                                                                                                  int32,
                                                                                                  int32)
           = {uint8(0)
              uint8(128)
              int32(0)
              int32(0)
              int32(8)}
  .field public static initonly valuetype[mscorlib]System.DateTime d2
 .custom instance void[mscorlib] System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64)
           = {int64(634925952000000000)}
  .custom instance void[mscorlib]
System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64)
           = {int64(634925952000000001)}
  .method private specialname rtspecialname static
          void  .cctor() cil managed
{
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldc.i4.s   -7
    IL_0002:  conv.i8
    IL_0003:  newobj instance void [mscorlib]System.Decimal::.ctor(int64)
    IL_0008:  stsfld valuetype[mscorlib]System.Decimal Class1::d1
    IL_000d:  ldc.i8     0x8cfb5ca13a30000
    IL_0016:  newobj instance void [mscorlib]System.DateTime::.ctor(int64)
    IL_001b:  stsfld valuetype[mscorlib]System.DateTime Class1::d2
    IL_0020:  ret
} // end of method Class1::.cctor

  .method public specialname rtspecialname
          instance void  .ctor() cil managed
{
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
} // end of method Class1::.ctor

  .method public instance void M1([opt] valuetype[mscorlib] System.Decimal d1,
                                   [opt] valuetype[mscorlib] System.DateTime d2) cil managed
{
    .param[1]
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                    uint8,
                                                                                                    uint32,
                                                                                                    uint32,
                                                                                                    uint32)
             = {
        uint8(0)
                uint8(128)
                uint32(0)
                uint32(0)
                uint32(7)}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                    uint8,
                                                                                                    int32,
                                                                                                    int32,
                                                                                                    int32)
             = {
        uint8(0)
                uint8(128)
                int32(0)
                int32(0)
                int32(8)}
    .param[2]
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64)
             = { int64(634925952000000001)}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64)
             = { int64(634925952000000000)}
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
} // end of method Class1::M1

} // end of class Class1
";

            var c1 = CreateCompilationWithMscorlib(
@"
public class Class1
{
    public const decimal d1 = -7;

    public void M1(decimal d1 = -7)
    {}
}");


            var class1 = c1.GetTypeByMetadataName("Class1");
            var d1 = class1.GetMember<FieldSymbol>("d1");
            var m1 = class1.GetMember<MethodSymbol>("M1");

            var state = new ModuleCompilationState();

            Assert.Empty(d1.GetAttributes());
            Assert.Equal("System.Runtime.CompilerServices.DecimalConstantAttribute(0, 128, 0, 0, 7)", d1.GetCustomAttributesToEmit(state).Single().ToString());
            Assert.Equal(d1.ConstantValue, -7m);
            Assert.Empty(m1.Parameters[0].GetAttributes());
            Assert.Equal("System.Runtime.CompilerServices.DecimalConstantAttribute(0, 128, 0, 0, 7)", m1.Parameters[0].GetCustomAttributesToEmit(state).Single().ToString());
            Assert.Equal(m1.Parameters[0].ExplicitDefaultValue, -7m);

            var c2 = CreateCompilationWithCustomILSource("", ilSource);

            class1 = c2.GetTypeByMetadataName("Class1");
            d1 = class1.GetMember<FieldSymbol>("d1");
            var d2 = class1.GetMember<FieldSymbol>("d2");
            m1 = class1.GetMember<MethodSymbol>("M1");

            Assert.Empty(d1.GetAttributes());
            Assert.Equal("System.Runtime.CompilerServices.DecimalConstantAttribute(0, 128, 0, 0, 7)", d1.GetCustomAttributesToEmit(state).Single().ToString());
            Assert.Equal(d1.ConstantValue, -7m);
            Assert.Equal(2, d2.GetAttributes().Length);
            Assert.Equal(2, d2.GetCustomAttributesToEmit(state).Count());
            Assert.Null(d2.ConstantValue);
            Assert.Empty(m1.Parameters[0].GetAttributes());
            Assert.Equal("System.Runtime.CompilerServices.DecimalConstantAttribute(0, 128, 0, 0, 7)", m1.Parameters[0].GetCustomAttributesToEmit(state).Single().ToString());
            Assert.Equal(m1.Parameters[0].ExplicitDefaultValue, -7m);
            Assert.Empty(m1.Parameters[1].GetAttributes());
            Assert.Equal("System.Runtime.CompilerServices.DateTimeConstantAttribute(634925952000000000)", m1.Parameters[1].GetCustomAttributesToEmit(state).Single().ToString());
            Assert.Equal(m1.Parameters[1].ExplicitDefaultValue, new DateTime(2013, 1, 1));

            var c3 = CreateCompilationWithCustomILSource("", ilSource);

            class1 = c3.GetTypeByMetadataName("Class1");
            d1 = class1.GetMember<FieldSymbol>("d1");
            d2 = class1.GetMember<FieldSymbol>("d2");
            m1 = class1.GetMember<MethodSymbol>("M1");

            Assert.Equal(d1.ConstantValue, -7m);
            Assert.Empty(d1.GetAttributes());
            Assert.Equal("System.Runtime.CompilerServices.DecimalConstantAttribute(0, 128, 0, 0, 7)", d1.GetCustomAttributesToEmit(state).Single().ToString());
            Assert.Null(d2.ConstantValue);
            Assert.Equal(2, d2.GetAttributes().Length);
            Assert.Equal(2, d2.GetCustomAttributesToEmit(state).Count());
            Assert.Equal(m1.Parameters[0].ExplicitDefaultValue, -7m);
            Assert.Empty(m1.Parameters[0].GetAttributes());
            Assert.Equal("System.Runtime.CompilerServices.DecimalConstantAttribute(0, 128, 0, 0, 7)", m1.Parameters[0].GetCustomAttributesToEmit(state).Single().ToString());
            Assert.Equal(m1.Parameters[1].ExplicitDefaultValue, new DateTime(2013, 1, 1));
            Assert.Empty(m1.Parameters[1].GetAttributes());
            Assert.Equal("System.Runtime.CompilerServices.DateTimeConstantAttribute(634925952000000000)", m1.Parameters[1].GetCustomAttributesToEmit(state).Single().ToString());
        }
    }
}
