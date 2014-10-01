// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class DynamicTransformsTests : CSharpTestBase
    {
        private AssemblySymbol assembly;
        private NamedTypeSymbol base0Class, base1Class, base2Class, derivedClass;
        private NamedTypeSymbol outerClass, innerClass, innerInnerClass;
        private NamedTypeSymbol outer2Class, inner2Class, innerInner2Class;
        private NamedTypeSymbol outer3Class, inner3Class;
        private NamedTypeSymbol objectType, intType;
        private static DynamicTypeSymbol dynamicType = DynamicTypeSymbol.Instance;
            
        private void CommonTestInitialization()
        {
            assembly = MetadataTestHelpers.GetSymbolsForReferences(
                TestReferences.SymbolsTests.Metadata.DynamicAttributeLib, 
                TestReferences.NetFx.v4_0_30319.mscorlib)[0];

            base0Class = assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Base0");
            base1Class = assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Base1");
            base2Class = assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Base2");
            derivedClass = assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            outerClass = assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Outer");
            innerClass = outerClass.GetTypeMember("Inner");
            innerInnerClass = innerClass.GetTypeMember("InnerInner");
            outer2Class = assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Outer2");
            inner2Class = outer2Class.GetTypeMember("Inner2");
            innerInner2Class = inner2Class.GetTypeMember("InnerInner2");
            outer3Class = assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Outer3");
            inner3Class = outer3Class.GetTypeMember("Inner3");
            
            objectType = assembly.CorLibrary.GetSpecialType(SpecialType.System_Object);
            intType = assembly.CorLibrary.GetSpecialType(SpecialType.System_Int32);
        }

        [Fact]
        public void TestBaseTypeDynamicTransforms()
        {
            CommonTestInitialization();

            // public class Base0 { }
            Assert.Equal(objectType, base0Class.BaseType);
            Assert.False(base0Class.ContainsDynamic());

            // public class Base1<T> { }
            Assert.Equal(objectType, base1Class.BaseType);
            Assert.False(base1Class.ContainsDynamic());

            // public class Base2<T, U> { }
            Assert.Equal(objectType, base2Class.BaseType);
            Assert.False(base2Class.ContainsDynamic());

            // public class Derived<T> : Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic> where T : Derived<T> { ... }
            Assert.False(derivedClass.ContainsDynamic());
            Assert.True(derivedClass.BaseType.ContainsDynamic());

            // Outer<dynamic>
            var outerClassOfDynamic = outerClass.Construct(dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>
            var t = derivedClass.TypeParameters[0];
            var arrayOfT = new ArrayTypeSymbol(assembly, t);
            var innerClassOfTArrDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(arrayOfT, dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[]
            var memberInnerInnerOfInt = innerClassOfTArrDynamic.GetTypeMember("InnerInner").Construct(intType);
            var arrayOfInnerInnerOfInt = new ArrayTypeSymbol(assembly, memberInnerInnerOfInt);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>
            var memberComplicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(arrayOfInnerInnerOfInt, dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
            var memberInnerInnerOfDynamic = memberComplicatedInner.GetTypeMember("InnerInner").Construct(dynamicType);

            Assert.Equal(memberInnerInnerOfDynamic, derivedClass.BaseType);

            // public class Outer<T> : Base1<dynamic>
            Assert.False(outerClass.ContainsDynamic());
            Assert.True(outerClass.BaseType.ContainsDynamic());
            var base1OfDynamic = base1Class.Construct(dynamicType);
            Assert.Equal(base1OfDynamic, outerClass.BaseType);

            // public class Inner<U, V> : Base2<dynamic, V>
            Assert.False(innerClass.ContainsDynamic());
            Assert.True(innerClass.BaseType.ContainsDynamic());
            var base2OfDynamicV = base2Class.Construct(dynamicType, innerClass.TypeParameters[1]);
            Assert.Equal(base2OfDynamicV, innerClass.BaseType);

            // public class InnerInner<W> : Base1<dynamic> { }
            Assert.False(innerInnerClass.ContainsDynamic());
            Assert.True(innerInnerClass.BaseType.ContainsDynamic());
            Assert.Equal(base1OfDynamic, innerInnerClass.BaseType);

            // public class Outer2<T> : Base1<dynamic>
            Assert.False(outer2Class.ContainsDynamic());
            Assert.True(outer2Class.BaseType.ContainsDynamic());
            Assert.Equal(base1OfDynamic, outer2Class.BaseType);

            // public class Inner2<U, V> : Base0
            Assert.False(inner2Class.ContainsDynamic());
            Assert.False(inner2Class.BaseType.ContainsDynamic());
            Assert.Equal(base0Class, inner2Class.BaseType);

            // public class InnerInner2<W> : Base0 { }
            Assert.False(innerInner2Class.ContainsDynamic());
            Assert.False(innerInner2Class.BaseType.ContainsDynamic());
            Assert.Equal(base0Class, innerInner2Class.BaseType);

            // public class Inner3<U>
            Assert.False(inner3Class.ContainsDynamic());
        }

        [Fact]
        public void TestFieldDynamicTransforms()
        {
            CommonTestInitialization();

            //public static dynamic field1;
            var field1 = derivedClass.GetMember<FieldSymbol>("field1");
            Assert.Equal(dynamicType, field1.Type);

            //public static dynamic[] field2;
            var field2 = derivedClass.GetMember<FieldSymbol>("field2");
            var arrayOfDynamic = new ArrayTypeSymbol(assembly, dynamicType, ImmutableArray.Create<CustomModifier>(), 1);
            Assert.Equal(arrayOfDynamic, field2.Type);

            //public static dynamic[][] field3;
            var field3 = derivedClass.GetMember<FieldSymbol>("field3");
            var arrayOfArrayOfDynamic = new ArrayTypeSymbol(assembly, arrayOfDynamic, ImmutableArray.Create<CustomModifier>(), 1);
            Assert.Equal(arrayOfArrayOfDynamic, field3.Type);

            //public const dynamic field4 = null;
            var field4 = derivedClass.GetMember<FieldSymbol>("field4");
            Assert.Equal(dynamicType, field4.Type);
            
            //public const dynamic[] field5 = null;
            var field5 = derivedClass.GetMember<FieldSymbol>("field5");
            Assert.Equal(arrayOfDynamic, field5.Type);

            //public const dynamic[][] field6 = null;
            var field6 = derivedClass.GetMember<FieldSymbol>("field6");
            Assert.Equal(arrayOfArrayOfDynamic, field6.Type);

            //public const dynamic[][] field7 = null;
            var field7 = derivedClass.GetMember<FieldSymbol>("field7");
            Assert.Equal(arrayOfArrayOfDynamic, field7.Type);

            //public Outer<T>.Inner<int, T>.InnerInner<Outer<dynamic>> field8 = null;
            var field8 = derivedClass.GetMember<FieldSymbol>("field8");
            var derivedTypeParam = derivedClass.TypeParameters[0];
            var outerOfT = outerClass.Construct(derivedTypeParam);
            var innerOfIntOfTWithOuterT = outerOfT.GetTypeMember("Inner").Construct(intType, derivedTypeParam);
            // Outer<dynamic>
            var outerClassOfDynamic = outerClass.Construct(dynamicType);
            var complicatedInnerInner = innerOfIntOfTWithOuterT.GetTypeMember("InnerInner").Construct(outerClassOfDynamic);
            Assert.Equal(complicatedInnerInner, field8.Type);

            //public Outer<dynamic>.Inner<T, T>.InnerInner<T> field9 = null;
            var field9 = derivedClass.GetMember<FieldSymbol>("field9");
            var innerOfTTWithOuterOfDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(derivedTypeParam, derivedTypeParam);
            complicatedInnerInner = innerOfTTWithOuterOfDynamic.GetTypeMember("InnerInner").Construct(derivedTypeParam);
            Assert.Equal(complicatedInnerInner, field9.Type);

            //public Outer<Outer<dynamic>.Inner<T, dynamic>>.Inner<dynamic, T>.InnerInner<T> field10 = null;
            var field10 = derivedClass.GetMember<FieldSymbol>("field10");
            // Outer<dynamic>.Inner<T, dynamic>
            var innerOfTDynamicWithOuterOfDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(derivedTypeParam, dynamicType);
            // Outer<Outer<dynamic>.Inner<T, dynamic>>
            var complicatedOuter = outerClass.Construct(innerOfTDynamicWithOuterOfDynamic);
            // Outer<Outer<dynamic>.Inner<T, dynamic>>.Inner<dynamic, T>
            var complicatedInner = complicatedOuter.GetTypeMember("Inner").Construct(dynamicType, derivedTypeParam);
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(derivedTypeParam);
            Assert.Equal(complicatedInnerInner, field10.Type);
            
            //public Outer<T>.Inner<dynamic, dynamic>.InnerInner<T> field11 = null;
            var field11 = derivedClass.GetMember<FieldSymbol>("field11");
            // Outer<T>.Inner<dynamic, dynamic>
            var innerOfDynamicDynamicWithOuterOfT = outerOfT.GetTypeMember("Inner").Construct(dynamicType, dynamicType);
            complicatedInnerInner = innerOfDynamicDynamicWithOuterOfT.GetTypeMember("InnerInner").Construct(derivedTypeParam);
            Assert.Equal(complicatedInnerInner, field11.Type);

            //public Outer<T>.Inner<T, T>.InnerInner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>> field12 = null;
            var field12 = derivedClass.GetMember<FieldSymbol>("field12");
            // Outer<T>.Inner<T, T>
            var innerOfTTWithOuterOfT = outerOfT.GetTypeMember("Inner").Construct(derivedTypeParam, derivedTypeParam);
            // Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>
            complicatedInnerInner = innerOfTDynamicWithOuterOfDynamic.GetTypeMember("InnerInner").Construct(intType);
            complicatedInnerInner = innerOfTTWithOuterOfT.GetTypeMember("InnerInner").Construct(complicatedInnerInner);
            Assert.Equal(complicatedInnerInner, field12.Type);
            
            //public Outer<dynamic>.Inner<Outer<T>, T>.InnerInner<dynamic> field13 = null;
            var field13 = derivedClass.GetMember<FieldSymbol>("field13");
            // Outer<dynamic>.Inner<Outer<T>, T>
            var innerOfOuterOfTTWithOuterDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(outerOfT, derivedTypeParam);
            complicatedInnerInner = innerOfOuterOfTTWithOuterDynamic.GetTypeMember("InnerInner").Construct(dynamicType);
            Assert.Equal(complicatedInnerInner, field13.Type);

            //public Outer<dynamic>.Inner<dynamic, dynamic>.InnerInner<dynamic> field14 = null;
            var field14 = derivedClass.GetMember<FieldSymbol>("field14");
            // Outer<dynamic>.Inner<dynamic, dynamic>
            var innerOfDynamicDymanicWithOuterOfDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(dynamicType, dynamicType);
            complicatedInnerInner = innerOfDynamicDymanicWithOuterOfDynamic.GetTypeMember("InnerInner").Construct(dynamicType);
            Assert.Equal(complicatedInnerInner, field14.Type);

            //public Outer<dynamic>.Inner<Outer<dynamic>, T>.InnerInner<dynamic>[] field15 = null;
            var field15 = derivedClass.GetMember<FieldSymbol>("field15");
            // Outer<dynamic>.Inner<Outer<dynamic>, T>
            var innerOfOuterOfDynamicTWithOuterDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(outerClassOfDynamic, derivedTypeParam);
            // Outer<dynamic>.Inner<Outer<dynamic>, T>.InnerInner<dynamic>
            complicatedInnerInner = innerOfOuterOfDynamicTWithOuterDynamic.GetTypeMember("InnerInner").Construct(dynamicType);
            var complicatedInnerInnerArray = new ArrayTypeSymbol(assembly, complicatedInnerInner, ImmutableArray.Create<CustomModifier>(), 1);
            Assert.Equal(complicatedInnerInnerArray, field15.Type);
            
            //public Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic.InnerInner<int>, dynamic[]>.InnerInner<dynamic>[][] field16 = null;
            var field16 = derivedClass.GetMember<FieldSymbol>("field16");
            // Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>
            complicatedInnerInner = innerOfTDynamicWithOuterOfDynamic.GetTypeMember("InnerInner").Construct(intType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>
            complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(complicatedInnerInner, arrayOfDynamic);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>.InnerInner<dynamic>
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(dynamicType);
            complicatedInnerInnerArray = new ArrayTypeSymbol(assembly, complicatedInnerInner, ImmutableArray.Create<CustomModifier>(), 1);
            var complicatedInnerInnerArrayOfArray = new ArrayTypeSymbol(assembly, complicatedInnerInnerArray, ImmutableArray.Create<CustomModifier>(), 1);
            Assert.Equal(complicatedInnerInnerArrayOfArray, field16.Type);

            //public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] field17 = null;
            var field17 = derivedClass.GetMember<FieldSymbol>("field17");
            // T[]
            var arrayOfDerivedTypeParam = new ArrayTypeSymbol(assembly, derivedTypeParam, ImmutableArray.Create<CustomModifier>(), 1);
            // Outer<dynamic>.Inner<T[], dynamic>
            complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(arrayOfDerivedTypeParam, dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(intType);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[]
            complicatedInnerInnerArray = new ArrayTypeSymbol(assembly, complicatedInnerInner, ImmutableArray.Create<CustomModifier>(), 1);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>
            complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(complicatedInnerInnerArray, dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][]
            complicatedInnerInnerArray = new ArrayTypeSymbol(assembly, complicatedInnerInner, ImmutableArray.Create<CustomModifier>(), 1);
            complicatedInnerInnerArrayOfArray = new ArrayTypeSymbol(assembly, complicatedInnerInnerArray, ImmutableArray.Create<CustomModifier>(), 1);
            Assert.Equal(complicatedInnerInnerArrayOfArray, field17.Type);

            //public static Outer3.Inner3<dynamic> field1 = null;
            field1 = inner3Class.GetMember<FieldSymbol>("field1");
            var inner3OfDynamic = inner3Class.Construct(dynamicType);
            Assert.Equal(inner3OfDynamic, field1.Type);
        }

        [Fact]
        public void TestReturnValueParameterAndPropertyTransforms()
        {
            CommonTestInitialization();

            //public static dynamic F1(dynamic x) { return x; }
            var f1 = derivedClass.GetMember<MethodSymbol>("F1");
            Assert.Equal(dynamicType, f1.ReturnType);
            Assert.Equal(dynamicType, f1.ParameterTypes[0]);
            
            //public static dynamic F2(ref dynamic x) { return x; }
            var f2 = derivedClass.GetMember<MethodSymbol>("F2");
            Assert.Equal(dynamicType, f2.ReturnType);
            Assert.Equal(dynamicType, f2.ParameterTypes[0]);
            Assert.Equal(RefKind.Ref, f2.Parameters[0].RefKind);
            
            //public static dynamic[] F3(dynamic[] x) { return x; }
            var f3 = derivedClass.GetMember<MethodSymbol>("F3");
            var arrayOfDynamic = new ArrayTypeSymbol(assembly, dynamicType);
            Assert.Equal(arrayOfDynamic, f3.ReturnType);
            Assert.Equal(arrayOfDynamic, f3.ParameterTypes[0]);
            Assert.Equal(RefKind.None, f3.Parameters[0].RefKind);

            var derivedTypeParam = derivedClass.TypeParameters[0];
            var arrayOfDerivedTypeParam = new ArrayTypeSymbol(assembly, derivedTypeParam);
            // Outer<dynamic>
            var outerClassOfDynamic = outerClass.Construct(dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>
            var complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(arrayOfDerivedTypeParam, dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>
            var complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(intType);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[]
            var complicatedInnerInnerArray = new ArrayTypeSymbol(assembly, complicatedInnerInner);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>
            complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(complicatedInnerInnerArray, dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][]
            complicatedInnerInnerArray = new ArrayTypeSymbol(assembly, complicatedInnerInner);
            var complicatedInnerInnerArrayOfArray = new ArrayTypeSymbol(assembly, complicatedInnerInnerArray);
            
            //public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
            var f4 = derivedClass.GetMember<MethodSymbol>("F4");
            Assert.Equal(complicatedInnerInnerArrayOfArray, f4.ReturnType);
            Assert.Equal(complicatedInnerInnerArrayOfArray, f4.ParameterTypes[0]);
            Assert.Equal(RefKind.None, f4.Parameters[0].RefKind);

            //public static dynamic Prop1 { get { return field1; } }
            var prop1 = derivedClass.GetMember<PropertySymbol>("Prop1");
            Assert.Equal(dynamicType, prop1.Type);
            Assert.Equal(dynamicType, prop1.GetMethod.ReturnType);
            
            //public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] Prop2 { get { return field17; } set { field17 = value; } }
            var prop2 = derivedClass.GetMember<PropertySymbol>("Prop2");
            Assert.Equal(complicatedInnerInnerArrayOfArray, prop2.Type);
            Assert.Equal(complicatedInnerInnerArrayOfArray, prop2.GetMethod.ReturnType);
            Assert.Equal(SpecialType.System_Void, prop2.SetMethod.ReturnType.SpecialType);
            Assert.Equal(complicatedInnerInnerArrayOfArray, prop2.SetMethod.ParameterTypes[0]);
        }

        [Fact]
        public void TestPointerTypeTransforms()
        {
            CommonTestInitialization();
            
            // public unsafe class UnsafeClass<T> : Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]> { }
            var unsafeClass = assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("UnsafeClass");
            Assert.False(unsafeClass.ContainsDynamic());
            Assert.True(unsafeClass.BaseType.ContainsDynamic());

            var unsafeClassTypeParam = unsafeClass.TypeParameters[0];
            // T[]
            var arrayOfDerivedTypeParam = new ArrayTypeSymbol(assembly, unsafeClassTypeParam, ImmutableArray.Create<CustomModifier>(), 1);
            // Outer<dynamic>
            var outerClassOfDynamic = outerClass.Construct(dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>
            var complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(arrayOfDerivedTypeParam, dynamicType);
            // int*[]
            var pointerToInt = new PointerTypeSymbol(intType, ImmutableArray.Create<CustomModifier>());
            var arrayOfPointerToInt = new ArrayTypeSymbol(assembly, pointerToInt, ImmutableArray.Create<CustomModifier>(), 1);
            // int*[][]
            var arrayOfArrayOfPointerToInt = new ArrayTypeSymbol(assembly, arrayOfPointerToInt, ImmutableArray.Create<CustomModifier>(), 1);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>
            var complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(arrayOfArrayOfPointerToInt);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[]
            var complicatedInnerInnerArray = new ArrayTypeSymbol(assembly, complicatedInnerInner, ImmutableArray.Create<CustomModifier>(), 1);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>
            complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(complicatedInnerInnerArray, dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]
            complicatedInnerInnerArray = new ArrayTypeSymbol(assembly, complicatedInnerInner, ImmutableArray.Create<CustomModifier>(), 1);
            var complicatedInnerInnerArrayOfArray = new ArrayTypeSymbol(assembly, complicatedInnerInnerArray, ImmutableArray.Create<CustomModifier>(), 1);
            // Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]>
            var baseType = base2Class.Construct(arrayOfPointerToInt, complicatedInnerInnerArrayOfArray);

            Assert.Equal(baseType, unsafeClass.BaseType);
        }

        [Fact]
        public void TestNullableTypeTransforms()
        {
            CommonTestInitialization();

            var structType = assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Struct");
            Assert.False(structType.ContainsDynamic());
            
            // public static Outer<dynamic>.Inner<dynamic, Struct?> nullableField;
            var field = structType.GetMember<FieldSymbol>("nullableField");
            Assert.True(field.Type.ContainsDynamic());

            var nullableStruct = assembly.CorLibrary.GetSpecialType(SpecialType.System_Nullable_T).Construct(structType);
            // Outer<dynamic>
            var outerClassOfDynamic = outerClass.Construct(dynamicType);
            // Outer<dynamic>.Inner<dynamic, Struct?>
            var constructedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(dynamicType, nullableStruct);

            Assert.Equal(constructedInner, field.Type);
        }

        [Fact]
        public void TestCustomModifierTransforms()
        {
            var il = @"
.assembly extern System.Core
{
}
.class public abstract auto ansi sealed beforefieldinit A`1<T>
       extends [mscorlib]System.Object
{
}

.class public abstract auto ansi sealed beforefieldinit B
       extends [mscorlib]System.Object
{
  .field public static class A`1<object> modopt([mscorlib]System.Runtime.CompilerServices.IsConst) F0
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 00 00 01 00 00 )
}
";

            var compilation = CreateCompilationWithCustomILSource(String.Empty, il, references: new[] { MscorlibRef, SystemCoreRef });
            var classA = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var classB = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B");
            
            var f0 = classB.GetMember<FieldSymbol>("F0");
            Assert.True(f0.Type.ContainsDynamic());

            // .field public static class A`1<object> modopt([mscorlib]System.Runtime.CompilerServices.IsConst) F0
            var constructedA = classA.Construct(dynamicType);
            Assert.Equal(constructedA, f0.Type);

            Assert.Equal(1, f0.CustomModifiers.Length);
        }

        [Fact]
        public void InvalidAttributeArgs1()
        {
            string csSource = @"
class D
{
    void M(C c) 
    {
        var f1 = c.F1;
        var f2 = c.F2;
        var f3 = c.F3;
        var f4 = c.F4;
        var f5 = c.F5;
        var f6 = c.F6;
        var m1 = c.M1();
        var m2 = c.M2();
        var p1 = c.P1;
        var p2 = c.P2;
    }
}
";
            var dll = MetadataReference.CreateFromImage(TestResources.MetadataTests.Invalid.InvalidDynamicAttributeArgs.AsImmutableOrNull());

            var c = CreateCompilationWithMscorlib(csSource, new[] { dll });
                
            c.VerifyDiagnostics(
                // (7,20): error CS0570: 'C.F1' is not supported by the language
                Diagnostic(ErrorCode.ERR_BindToBogus, "F1").WithArguments("C.F1"),
                // (7,20): error CS0570: 'C.F2' is not supported by the language
                Diagnostic(ErrorCode.ERR_BindToBogus, "F2").WithArguments("C.F2"),
                // (8,18): error CS0570: 'C.M1()' is not supported by the language
                Diagnostic(ErrorCode.ERR_BindToBogus, "M1").WithArguments("C.M1()"),
                // (9,18): error CS0570: 'C.M2()' is not supported by the language
                Diagnostic(ErrorCode.ERR_BindToBogus, "M2").WithArguments("C.M2()"),
                // (10,20): error CS0570: 'C.P1' is not supported by the language
                Diagnostic(ErrorCode.ERR_BindToBogus, "P1").WithArguments("C.P1"),
                // (11,20): error CS0570: 'C.P2' is not supported by the language
                Diagnostic(ErrorCode.ERR_BindToBogus, "P2").WithArguments("C.P2"));
        }

        [Fact]
        public void TestDynamicTransformsBadMetadata()
        {
            var il = @"
.assembly extern System.Core
{
}
.class public abstract auto ansi beforefieldinit Derived`1<T>
       extends class Base`1<object>
{
  // DynamicTransforms array has no 'true' values
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 00 00 00 ) 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base0::.ctor
}

.class public abstract auto ansi beforefieldinit Derived2`1<T>
       extends class Base`1<object>
{
  // DynamicTransforms array has 'true' values for non-object types
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 01 01 00 00 ) 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base0::.ctor
}

.class public auto ansi beforefieldinit Base`1<T>
       extends [mscorlib]System.Object
{
  .field public !T field1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base0::.ctor
}

.class public abstract auto ansi beforefieldinit A`1<T>
       extends [mscorlib]System.Object
{
}
.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  // DynamicTransforms array has 'true' values for custom-modifier associated bits
  .field public static class A`1<object> modopt([mscorlib]System.Runtime.CompilerServices.IsConst) field1
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 01 00 01 00 00 )
}
";
            var source = @"
class Y: Derived<int>
{
  public static void Main()
  {
    var y = new Y();
    y.field1 = 3;
    System.Console.WriteLine(y.field1);
    y.field1 = ""str"";
    System.Console.WriteLine(y.field1);

    var y2 = new Y2();
    y2.field1 = 3;
    System.Console.WriteLine(y2.field1);
    y2.field1 = ""str"";
    System.Console.WriteLine(y2.field1);

    A<object> a = B.field1;
  }
}

class Y2: Derived2<int> {}
";
            var expectedOutput = @"3
str
3
str";

            var compilation = CreateCompilationWithCustomILSource(source, il, references: new[] { MscorlibRef, SystemCoreRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expectedOutput, emitOptions: EmitOptions.RefEmitBug);

            var classDerived = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var field1 = classDerived.BaseType.GetMember<FieldSymbol>("field1");
            Assert.False(field1.Type.ContainsDynamic());

            var classDerived2 = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived2");
            field1 = classDerived.BaseType.GetMember<FieldSymbol>("field1");
            Assert.False(field1.Type.ContainsDynamic());

            var classB = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B");

            field1 = classB.GetMember<FieldSymbol>("field1");
            Assert.False(field1.Type.ContainsDynamic());
        }
    }
}