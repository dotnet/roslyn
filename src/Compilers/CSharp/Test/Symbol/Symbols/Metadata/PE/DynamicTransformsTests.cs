// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class DynamicTransformsTests : CSharpTestBase
    {
        private AssemblySymbol _assembly;
        private NamedTypeSymbol _base0Class, _base1Class, _base2Class, _derivedClass;
        private NamedTypeSymbol _outerClass, _innerClass, _innerInnerClass;
        private NamedTypeSymbol _outer2Class, _inner2Class, _innerInner2Class;
        private NamedTypeSymbol _outer3Class, _inner3Class;
        private NamedTypeSymbol _objectType, _intType;
        private static readonly DynamicTypeSymbol s_dynamicType = DynamicTypeSymbol.Instance;

        private void CommonTestInitialization()
        {
            _assembly = MetadataTestHelpers.GetSymbolsForReferences(
                TestReferences.SymbolsTests.Metadata.DynamicAttributeLib,
                TestReferences.NetFx.v4_0_30319.mscorlib)[0];

            _base0Class = _assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Base0");
            _base1Class = _assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Base1");
            _base2Class = _assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Base2");
            _derivedClass = _assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            _outerClass = _assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Outer");
            _innerClass = _outerClass.GetTypeMember("Inner");
            _innerInnerClass = _innerClass.GetTypeMember("InnerInner");
            _outer2Class = _assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Outer2");
            _inner2Class = _outer2Class.GetTypeMember("Inner2");
            _innerInner2Class = _inner2Class.GetTypeMember("InnerInner2");
            _outer3Class = _assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Outer3");
            _inner3Class = _outer3Class.GetTypeMember("Inner3");

            _objectType = _assembly.CorLibrary.GetSpecialType(SpecialType.System_Object);
            _intType = _assembly.CorLibrary.GetSpecialType(SpecialType.System_Int32);
        }

        [Fact]
        public void TestBaseTypeDynamicTransforms()
        {
            CommonTestInitialization();

            // public class Base0 { }
            Assert.Equal(_objectType, _base0Class.BaseType);
            Assert.False(_base0Class.ContainsDynamic());

            // public class Base1<T> { }
            Assert.Equal(_objectType, _base1Class.BaseType);
            Assert.False(_base1Class.ContainsDynamic());

            // public class Base2<T, U> { }
            Assert.Equal(_objectType, _base2Class.BaseType);
            Assert.False(_base2Class.ContainsDynamic());

            // public class Derived<T> : Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic> where T : Derived<T> { ... }
            Assert.False(_derivedClass.ContainsDynamic());
            Assert.True(_derivedClass.BaseType.ContainsDynamic());

            // Outer<dynamic>
            var outerClassOfDynamic = _outerClass.Construct(s_dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>
            var t = _derivedClass.TypeParameters[0];
            var arrayOfT = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(t));
            var innerClassOfTArrDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(arrayOfT, s_dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[]
            var memberInnerInnerOfInt = innerClassOfTArrDynamic.GetTypeMember("InnerInner").Construct(_intType);
            var arrayOfInnerInnerOfInt = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(memberInnerInnerOfInt));
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>
            var memberComplicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(arrayOfInnerInnerOfInt, s_dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
            var memberInnerInnerOfDynamic = memberComplicatedInner.GetTypeMember("InnerInner").Construct(s_dynamicType);

            Assert.Equal(memberInnerInnerOfDynamic, _derivedClass.BaseType);

            // public class Outer<T> : Base1<dynamic>
            Assert.False(_outerClass.ContainsDynamic());
            Assert.True(_outerClass.BaseType.ContainsDynamic());
            var base1OfDynamic = _base1Class.Construct(s_dynamicType);
            Assert.Equal(base1OfDynamic, _outerClass.BaseType);

            // public class Inner<U, V> : Base2<dynamic, V>
            Assert.False(_innerClass.ContainsDynamic());
            Assert.True(_innerClass.BaseType.ContainsDynamic());
            var base2OfDynamicV = _base2Class.Construct(s_dynamicType, _innerClass.TypeParameters[1]);
            Assert.Equal(base2OfDynamicV, _innerClass.BaseType);

            // public class InnerInner<W> : Base1<dynamic> { }
            Assert.False(_innerInnerClass.ContainsDynamic());
            Assert.True(_innerInnerClass.BaseType.ContainsDynamic());
            Assert.Equal(base1OfDynamic, _innerInnerClass.BaseType);

            // public class Outer2<T> : Base1<dynamic>
            Assert.False(_outer2Class.ContainsDynamic());
            Assert.True(_outer2Class.BaseType.ContainsDynamic());
            Assert.Equal(base1OfDynamic, _outer2Class.BaseType);

            // public class Inner2<U, V> : Base0
            Assert.False(_inner2Class.ContainsDynamic());
            Assert.False(_inner2Class.BaseType.ContainsDynamic());
            Assert.Equal(_base0Class, _inner2Class.BaseType);

            // public class InnerInner2<W> : Base0 { }
            Assert.False(_innerInner2Class.ContainsDynamic());
            Assert.False(_innerInner2Class.BaseType.ContainsDynamic());
            Assert.Equal(_base0Class, _innerInner2Class.BaseType);

            // public class Inner3<U>
            Assert.False(_inner3Class.ContainsDynamic());
        }

        [Fact]
        public void TestFieldDynamicTransforms()
        {
            CommonTestInitialization();

            //public static dynamic field1;
            var field1 = _derivedClass.GetMember<FieldSymbol>("field1");
            Assert.Equal(s_dynamicType, field1.Type.TypeSymbol);

            //public static dynamic[] field2;
            var field2 = _derivedClass.GetMember<FieldSymbol>("field2");
            var arrayOfDynamic = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(s_dynamicType), 1);
            Assert.Equal(arrayOfDynamic, field2.Type.TypeSymbol);

            //public static dynamic[][] field3;
            var field3 = _derivedClass.GetMember<FieldSymbol>("field3");
            var arrayOfArrayOfDynamic = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(arrayOfDynamic), 1);
            Assert.Equal(arrayOfArrayOfDynamic, field3.Type.TypeSymbol);

            //public const dynamic field4 = null;
            var field4 = _derivedClass.GetMember<FieldSymbol>("field4");
            Assert.Equal(s_dynamicType, field4.Type.TypeSymbol);

            //public const dynamic[] field5 = null;
            var field5 = _derivedClass.GetMember<FieldSymbol>("field5");
            Assert.Equal(arrayOfDynamic, field5.Type.TypeSymbol);

            //public const dynamic[][] field6 = null;
            var field6 = _derivedClass.GetMember<FieldSymbol>("field6");
            Assert.Equal(arrayOfArrayOfDynamic, field6.Type.TypeSymbol);

            //public const dynamic[][] field7 = null;
            var field7 = _derivedClass.GetMember<FieldSymbol>("field7");
            Assert.Equal(arrayOfArrayOfDynamic, field7.Type.TypeSymbol);

            //public Outer<T>.Inner<int, T>.InnerInner<Outer<dynamic>> field8 = null;
            var field8 = _derivedClass.GetMember<FieldSymbol>("field8");
            var derivedTypeParam = _derivedClass.TypeParameters[0];
            var outerOfT = _outerClass.Construct(derivedTypeParam);
            var innerOfIntOfTWithOuterT = outerOfT.GetTypeMember("Inner").Construct(_intType, derivedTypeParam);
            // Outer<dynamic>
            var outerClassOfDynamic = _outerClass.Construct(s_dynamicType);
            var complicatedInnerInner = innerOfIntOfTWithOuterT.GetTypeMember("InnerInner").Construct(outerClassOfDynamic);
            Assert.Equal(complicatedInnerInner, field8.Type.TypeSymbol);

            //public Outer<dynamic>.Inner<T, T>.InnerInner<T> field9 = null;
            var field9 = _derivedClass.GetMember<FieldSymbol>("field9");
            var innerOfTTWithOuterOfDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(derivedTypeParam, derivedTypeParam);
            complicatedInnerInner = innerOfTTWithOuterOfDynamic.GetTypeMember("InnerInner").Construct(derivedTypeParam);
            Assert.Equal(complicatedInnerInner, field9.Type.TypeSymbol);

            //public Outer<Outer<dynamic>.Inner<T, dynamic>>.Inner<dynamic, T>.InnerInner<T> field10 = null;
            var field10 = _derivedClass.GetMember<FieldSymbol>("field10");
            // Outer<dynamic>.Inner<T, dynamic>
            var innerOfTDynamicWithOuterOfDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(derivedTypeParam, s_dynamicType);
            // Outer<Outer<dynamic>.Inner<T, dynamic>>
            var complicatedOuter = _outerClass.Construct(innerOfTDynamicWithOuterOfDynamic);
            // Outer<Outer<dynamic>.Inner<T, dynamic>>.Inner<dynamic, T>
            var complicatedInner = complicatedOuter.GetTypeMember("Inner").Construct(s_dynamicType, derivedTypeParam);
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(derivedTypeParam);
            Assert.Equal(complicatedInnerInner, field10.Type.TypeSymbol);

            //public Outer<T>.Inner<dynamic, dynamic>.InnerInner<T> field11 = null;
            var field11 = _derivedClass.GetMember<FieldSymbol>("field11");
            // Outer<T>.Inner<dynamic, dynamic>
            var innerOfDynamicDynamicWithOuterOfT = outerOfT.GetTypeMember("Inner").Construct(s_dynamicType, s_dynamicType);
            complicatedInnerInner = innerOfDynamicDynamicWithOuterOfT.GetTypeMember("InnerInner").Construct(derivedTypeParam);
            Assert.Equal(complicatedInnerInner, field11.Type.TypeSymbol);

            //public Outer<T>.Inner<T, T>.InnerInner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>> field12 = null;
            var field12 = _derivedClass.GetMember<FieldSymbol>("field12");
            // Outer<T>.Inner<T, T>
            var innerOfTTWithOuterOfT = outerOfT.GetTypeMember("Inner").Construct(derivedTypeParam, derivedTypeParam);
            // Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>
            complicatedInnerInner = innerOfTDynamicWithOuterOfDynamic.GetTypeMember("InnerInner").Construct(_intType);
            complicatedInnerInner = innerOfTTWithOuterOfT.GetTypeMember("InnerInner").Construct(complicatedInnerInner);
            Assert.Equal(complicatedInnerInner, field12.Type.TypeSymbol);

            //public Outer<dynamic>.Inner<Outer<T>, T>.InnerInner<dynamic> field13 = null;
            var field13 = _derivedClass.GetMember<FieldSymbol>("field13");
            // Outer<dynamic>.Inner<Outer<T>, T>
            var innerOfOuterOfTTWithOuterDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(outerOfT, derivedTypeParam);
            complicatedInnerInner = innerOfOuterOfTTWithOuterDynamic.GetTypeMember("InnerInner").Construct(s_dynamicType);
            Assert.Equal(complicatedInnerInner, field13.Type.TypeSymbol);

            //public Outer<dynamic>.Inner<dynamic, dynamic>.InnerInner<dynamic> field14 = null;
            var field14 = _derivedClass.GetMember<FieldSymbol>("field14");
            // Outer<dynamic>.Inner<dynamic, dynamic>
            var innerOfDynamicDynamicWithOuterOfDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(s_dynamicType, s_dynamicType);
            complicatedInnerInner = innerOfDynamicDynamicWithOuterOfDynamic.GetTypeMember("InnerInner").Construct(s_dynamicType);
            Assert.Equal(complicatedInnerInner, field14.Type.TypeSymbol);

            //public Outer<dynamic>.Inner<Outer<dynamic>, T>.InnerInner<dynamic>[] field15 = null;
            var field15 = _derivedClass.GetMember<FieldSymbol>("field15");
            // Outer<dynamic>.Inner<Outer<dynamic>, T>
            var innerOfOuterOfDynamicTWithOuterDynamic = outerClassOfDynamic.GetTypeMember("Inner").Construct(outerClassOfDynamic, derivedTypeParam);
            // Outer<dynamic>.Inner<Outer<dynamic>, T>.InnerInner<dynamic>
            complicatedInnerInner = innerOfOuterOfDynamicTWithOuterDynamic.GetTypeMember("InnerInner").Construct(s_dynamicType);
            var complicatedInnerInnerArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInner), 1);
            Assert.Equal(complicatedInnerInnerArray, field15.Type.TypeSymbol);

            //public Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic.InnerInner<int>, dynamic[]>.InnerInner<dynamic>[][] field16 = null;
            var field16 = _derivedClass.GetMember<FieldSymbol>("field16");
            // Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>
            complicatedInnerInner = innerOfTDynamicWithOuterOfDynamic.GetTypeMember("InnerInner").Construct(_intType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>
            complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(complicatedInnerInner, arrayOfDynamic);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>.InnerInner<dynamic>
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(s_dynamicType);
            complicatedInnerInnerArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInner), 1);
            var complicatedInnerInnerArrayOfArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInnerArray), 1);
            Assert.Equal(complicatedInnerInnerArrayOfArray, field16.Type.TypeSymbol);

            //public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] field17 = null;
            var field17 = _derivedClass.GetMember<FieldSymbol>("field17");
            // T[]
            var arrayOfDerivedTypeParam = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(derivedTypeParam), 1);
            // Outer<dynamic>.Inner<T[], dynamic>
            complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(arrayOfDerivedTypeParam, s_dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(_intType);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[]
            complicatedInnerInnerArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInner), 1);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>
            complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(complicatedInnerInnerArray, s_dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(s_dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][]
            complicatedInnerInnerArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInner), 1);
            complicatedInnerInnerArrayOfArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInnerArray), 1);
            Assert.Equal(complicatedInnerInnerArrayOfArray, field17.Type.TypeSymbol);

            //public static Outer3.Inner3<dynamic> field1 = null;
            field1 = _inner3Class.GetMember<FieldSymbol>("field1");
            var inner3OfDynamic = _inner3Class.Construct(s_dynamicType);
            Assert.Equal(inner3OfDynamic, field1.Type.TypeSymbol);
        }

        [Fact]
        public void TestReturnValueParameterAndPropertyTransforms()
        {
            CommonTestInitialization();

            //public static dynamic F1(dynamic x) { return x; }
            var f1 = _derivedClass.GetMember<MethodSymbol>("F1");
            Assert.Equal(s_dynamicType, f1.ReturnType.TypeSymbol);
            Assert.Equal(s_dynamicType, f1.ParameterTypes[0]);

            //public static dynamic F2(ref dynamic x) { return x; }
            var f2 = _derivedClass.GetMember<MethodSymbol>("F2");
            Assert.Equal(s_dynamicType, f2.ReturnType.TypeSymbol);
            Assert.Equal(s_dynamicType, f2.ParameterTypes[0]);
            Assert.Equal(RefKind.Ref, f2.Parameters[0].RefKind);

            //public static dynamic[] F3(dynamic[] x) { return x; }
            var f3 = _derivedClass.GetMember<MethodSymbol>("F3");
            var arrayOfDynamic = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(s_dynamicType));
            Assert.Equal(arrayOfDynamic, f3.ReturnType.TypeSymbol);
            Assert.Equal(arrayOfDynamic, f3.ParameterTypes[0]);
            Assert.Equal(RefKind.None, f3.Parameters[0].RefKind);

            var derivedTypeParam = _derivedClass.TypeParameters[0];
            var arrayOfDerivedTypeParam = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(derivedTypeParam));
            // Outer<dynamic>
            var outerClassOfDynamic = _outerClass.Construct(s_dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>
            var complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(arrayOfDerivedTypeParam, s_dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>
            var complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(_intType);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[]
            var complicatedInnerInnerArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInner));
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>
            complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(complicatedInnerInnerArray, s_dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(s_dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][]
            complicatedInnerInnerArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInner));
            var complicatedInnerInnerArrayOfArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInnerArray));

            //public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
            var f4 = _derivedClass.GetMember<MethodSymbol>("F4");
            Assert.Equal(complicatedInnerInnerArrayOfArray, f4.ReturnType.TypeSymbol);
            Assert.Equal(complicatedInnerInnerArrayOfArray, f4.ParameterTypes[0]);
            Assert.Equal(RefKind.None, f4.Parameters[0].RefKind);

            //public static dynamic Prop1 { get { return field1; } }
            var prop1 = _derivedClass.GetMember<PropertySymbol>("Prop1");
            Assert.Equal(s_dynamicType, prop1.Type.TypeSymbol);
            Assert.Equal(s_dynamicType, prop1.GetMethod.ReturnType.TypeSymbol);

            //public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] Prop2 { get { return field17; } set { field17 = value; } }
            var prop2 = _derivedClass.GetMember<PropertySymbol>("Prop2");
            Assert.Equal(complicatedInnerInnerArrayOfArray, prop2.Type.TypeSymbol);
            Assert.Equal(complicatedInnerInnerArrayOfArray, prop2.GetMethod.ReturnType.TypeSymbol);
            Assert.Equal(SpecialType.System_Void, prop2.SetMethod.ReturnType.SpecialType);
            Assert.Equal(complicatedInnerInnerArrayOfArray, prop2.SetMethod.ParameterTypes[0]);
        }

        [Fact]
        public void TestPointerTypeTransforms()
        {
            CommonTestInitialization();

            // public unsafe class UnsafeClass<T> : Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]> { }
            var unsafeClass = _assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("UnsafeClass");
            Assert.False(unsafeClass.ContainsDynamic());
            Assert.True(unsafeClass.BaseType.ContainsDynamic());

            var unsafeClassTypeParam = unsafeClass.TypeParameters[0];
            // T[]
            var arrayOfDerivedTypeParam = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(unsafeClassTypeParam), 1);
            // Outer<dynamic>
            var outerClassOfDynamic = _outerClass.Construct(s_dynamicType);
            // Outer<dynamic>.Inner<T[], dynamic>
            var complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(arrayOfDerivedTypeParam, s_dynamicType);
            // int*[]
            var pointerToInt = new PointerTypeSymbol(TypeSymbolWithAnnotations.Create(_intType));
            var arrayOfPointerToInt = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(pointerToInt), 1);
            // int*[][]
            var arrayOfArrayOfPointerToInt = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(arrayOfPointerToInt), 1);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>
            var complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(arrayOfArrayOfPointerToInt);
            // Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[]
            var complicatedInnerInnerArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInner), 1);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>
            complicatedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(complicatedInnerInnerArray, s_dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>
            complicatedInnerInner = complicatedInner.GetTypeMember("InnerInner").Construct(s_dynamicType);
            // Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]
            complicatedInnerInnerArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInner), 1);
            var complicatedInnerInnerArrayOfArray = ArrayTypeSymbol.CreateCSharpArray(_assembly, TypeSymbolWithAnnotations.Create(complicatedInnerInnerArray), 1);
            // Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]>
            var baseType = _base2Class.Construct(arrayOfPointerToInt, complicatedInnerInnerArrayOfArray);

            Assert.Equal(baseType, unsafeClass.BaseType);
        }

        [Fact]
        public void TestNullableTypeTransforms()
        {
            CommonTestInitialization();

            var structType = _assembly.Modules[0].GlobalNamespace.GetMember<NamedTypeSymbol>("Struct");
            Assert.False(structType.ContainsDynamic());

            // public static Outer<dynamic>.Inner<dynamic, Struct?> nullableField;
            var field = structType.GetMember<FieldSymbol>("nullableField");
            Assert.True(field.Type.TypeSymbol.ContainsDynamic());

            var nullableStruct = _assembly.CorLibrary.GetSpecialType(SpecialType.System_Nullable_T).Construct(structType);
            // Outer<dynamic>
            var outerClassOfDynamic = _outerClass.Construct(s_dynamicType);
            // Outer<dynamic>.Inner<dynamic, Struct?>
            var constructedInner = outerClassOfDynamic.GetTypeMember("Inner").Construct(s_dynamicType, nullableStruct);

            Assert.Equal(constructedInner, field.Type.TypeSymbol);
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
            Assert.True(f0.Type.TypeSymbol.ContainsDynamic());

            // .field public static class A`1<object> modopt([mscorlib]System.Runtime.CompilerServices.IsConst) F0
            var constructedA = classA.Construct(s_dynamicType);
            Assert.Equal(constructedA, f0.Type.TypeSymbol);

            Assert.Equal(1, f0.Type.CustomModifiers.Length);
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

        [ClrOnlyFact]
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
            CompileAndVerify(compilation, expectedOutput: expectedOutput);

            var classDerived = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var field1 = classDerived.BaseType.GetMember<FieldSymbol>("field1");
            Assert.False(field1.Type.TypeSymbol.ContainsDynamic());

            var classDerived2 = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived2");
            field1 = classDerived.BaseType.GetMember<FieldSymbol>("field1");
            Assert.False(field1.Type.TypeSymbol.ContainsDynamic());

            var classB = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B");

            field1 = classB.GetMember<FieldSymbol>("field1");
            Assert.False(field1.Type.TypeSymbol.ContainsDynamic());
        }
    }
}
