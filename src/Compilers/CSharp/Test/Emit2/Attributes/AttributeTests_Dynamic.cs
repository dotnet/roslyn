// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_Dynamic : WellKnownAttributesTestBase
    {
        private static readonly string s_dynamicTestSource = @"
public class Base0 { }
public class Base1<T> { }
public class Base2<T, U> { }

public class Outer<T> : Base1<dynamic>
{
    public class Inner<U, V> : Base2<dynamic, V>
    {
        public class InnerInner<W> : Base1<dynamic> { }
    }
}

public class Outer2<T> : Base1<dynamic>
{
    public class Inner2<U, V> : Base0
    {
        public class InnerInner2<W> : Base0 { }
    }
}

public class Outer3
{
    public class Inner3<U>
    {
        public static Outer3.Inner3<dynamic> field1 = null;
    }
}

public class Derived<T> : Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
where T : Derived<T>
{
    public static dynamic field1;
    public static dynamic[] field2;
    public static dynamic[][] field3;

    public const dynamic field4 = null;
    public const dynamic[] field5 = null;
    public const dynamic[][] field6 = null;
    public const dynamic[][] field7 = null;

    public Outer<T>.Inner<int, T>.InnerInner<Outer<dynamic>> field8 = null;
    public Outer<dynamic>.Inner<T, T>.InnerInner<T> field9 = null;
    public Outer<Outer<dynamic>.Inner<T, dynamic>>.Inner<dynamic, T>.InnerInner<T> field10 = null;
    public Outer<T>.Inner<dynamic, dynamic>.InnerInner<T> field11 = null;
    public Outer<T>.Inner<T, T>.InnerInner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>> field12 = null;
    public Outer<dynamic>.Inner<Outer<T>, T>.InnerInner<dynamic> field13 = null;
    public Outer<dynamic>.Inner<dynamic, dynamic>.InnerInner<dynamic> field14 = null;

    public Outer<dynamic>.Inner<Outer<dynamic>, T>.InnerInner<dynamic>[] field15 = null;
    public Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>.InnerInner<dynamic>[][] field16 = null;
    public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] field17 = null;

    public static dynamic F1(dynamic x) { return x; }
    public static dynamic F2(ref dynamic x) { return x; }
    public static dynamic[] F3(dynamic[] x) { return x; }
    public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }

    public static dynamic Prop1 { get { return field1; } }
    public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] Prop2 { get { return field17; } set { field17 = value; } }

    public dynamic this[dynamic param]
    {
        get { return null; }
        set {}
    }

    public static (dynamic, object, dynamic) F5((dynamic, object, dynamic) x) { return x; }
}

public unsafe class UnsafeClass<T> : Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]> { }

public struct Struct
{
    public static Outer<dynamic>.Inner<dynamic, Struct?> nullableField;
}

public delegate dynamic[] MyDelegate(dynamic[] x);
";

        [Fact]
        public void TestCompileDynamicAttributes()
        {
            var comp = CreateCompilationWithMscorlib40(s_dynamicTestSource, options: TestOptions.UnsafeReleaseDll, references: new[] { SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef });

            CompileAndVerify(comp, verify: Verification.Passes, symbolValidator: module =>
            {
                DynamicAttributeValidator.ValidateDynamicAttributes(module);
            });
        }

        internal struct DynamicAttributeValidator
        {
            private readonly ModuleSymbol _module;
            private readonly NamedTypeSymbol _base0Class, _base1Class, _base2Class, _derivedClass;
            private readonly NamedTypeSymbol _outerClass, _innerClass, _innerInnerClass;
            private readonly NamedTypeSymbol _outer2Class, _inner2Class, _innerInner2Class;
            private readonly NamedTypeSymbol _outer3Class, _inner3Class;
            private readonly NamedTypeSymbol _unsafeClass;
            private readonly NamedTypeSymbol _structType;
            private readonly NamedTypeSymbol _synthesizedMyDelegateType;
            private bool[] _expectedTransformFlags;

            private DynamicAttributeValidator(ModuleSymbol module)
            {
                _module = module;
                var globalNamespace = module.GlobalNamespace;

                _base0Class = globalNamespace.GetMember<NamedTypeSymbol>("Base0");
                _base1Class = globalNamespace.GetMember<NamedTypeSymbol>("Base1");
                _base2Class = globalNamespace.GetMember<NamedTypeSymbol>("Base2");
                _derivedClass = globalNamespace.GetMember<NamedTypeSymbol>("Derived");
                _outerClass = globalNamespace.GetMember<NamedTypeSymbol>("Outer");
                _innerClass = _outerClass.GetTypeMember("Inner");
                _innerInnerClass = _innerClass.GetTypeMember("InnerInner");
                _outer2Class = globalNamespace.GetMember<NamedTypeSymbol>("Outer2");
                _inner2Class = _outer2Class.GetTypeMember("Inner2");
                _innerInner2Class = _inner2Class.GetTypeMember("InnerInner2");
                _outer3Class = globalNamespace.GetMember<NamedTypeSymbol>("Outer3");
                _inner3Class = _outer3Class.GetTypeMember("Inner3");
                _unsafeClass = globalNamespace.GetMember<NamedTypeSymbol>("UnsafeClass");
                _structType = globalNamespace.GetMember<NamedTypeSymbol>("Struct");
                _synthesizedMyDelegateType = globalNamespace.GetMember<NamedTypeSymbol>("MyDelegate");

                _expectedTransformFlags = null;
            }

            internal static void ValidateDynamicAttributes(ModuleSymbol module)
            {
                var validator = new DynamicAttributeValidator(module);

                validator.ValidateAttributesOnNamedTypes();
                validator.ValidateAttributesOnFields();
                validator.ValidateAttributesOnMethodReturnValueAndParameters();
                validator.ValidateAttributesOnProperty();
                validator.ValidateAttributesOnIndexer();
                validator.ValidateAttributesForPointerType();
                validator.ValidateAttributesForNullableType();
                validator.ValidateAttributesForSynthesizedDelegateMembers();
            }

            private void ValidateAttributesOnNamedTypes()
            {
                // public class Base0 { }
                ValidateDynamicAttribute(_base0Class.GetAttributes(), expectedDynamicAttribute: false);

                // public class Base1<T> { }
                ValidateDynamicAttribute(_base1Class.GetAttributes(), expectedDynamicAttribute: false);

                // public class Base2<T, U> { }
                ValidateDynamicAttribute(_base2Class.GetAttributes(), expectedDynamicAttribute: false);

                // public class Derived<T> : Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
                Assert.True(_derivedClass.BaseType().ContainsDynamic());
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 0B 00 00 00 * 00 01 00 00 01 00 00 01 00 01 01 * 00 00 )
                _expectedTransformFlags = new bool[] { false, true, false, false, true, false, false, true, false, true, true };
                ValidateDynamicAttribute(_derivedClass.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);

                // public class Outer<T> : Base1<dynamic>
                Assert.True(_outerClass.BaseType().ContainsDynamic());
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                _expectedTransformFlags = new bool[] { false, true };
                ValidateDynamicAttribute(_outerClass.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);

                // public class Inner<U, V> : Base2<dynamic, V>
                Assert.True(_innerClass.BaseType().ContainsDynamic());
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 * 00 01 00 * 00 00 ) 
                _expectedTransformFlags = new bool[] { false, true, false };
                ValidateDynamicAttribute(_innerClass.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);

                // public class InnerInner<W> : Base1<dynamic> { }
                Assert.True(_innerInnerClass.BaseType().ContainsDynamic());
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                _expectedTransformFlags = new bool[] { false, true };
                ValidateDynamicAttribute(_innerInnerClass.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);

                // public class Outer2<T> : Base1<dynamic>
                Assert.True(_outer2Class.BaseType().ContainsDynamic());
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                _expectedTransformFlags = new bool[] { false, true };
                ValidateDynamicAttribute(_outer2Class.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);

                // public class Inner2<U, V> : Base0
                Assert.False(_inner2Class.BaseType().ContainsDynamic());
                ValidateDynamicAttribute(_inner2Class.GetAttributes(), expectedDynamicAttribute: false);

                // public class InnerInner2<W> : Base0 { }
                Assert.False(_innerInner2Class.BaseType().ContainsDynamic());
                ValidateDynamicAttribute(_innerInner2Class.GetAttributes(), expectedDynamicAttribute: false);

                // public class Inner3<U>
                ValidateDynamicAttribute(_inner3Class.GetAttributes(), expectedDynamicAttribute: false);
            }

            private void ValidateAttributesOnFields()
            {
                bool[] expectedTransformFlags;

                //public static dynamic field1;
                var field1 = _derivedClass.GetMember<FieldSymbol>("field1");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 ) 
                ValidateDynamicAttribute(field1.GetAttributes(), expectedDynamicAttribute: true);

                //public static dynamic[] field2;
                var field2 = _derivedClass.GetMember<FieldSymbol>("field2");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, true };
                ValidateDynamicAttribute(field2.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public static dynamic[][] field3;
                var field3 = _derivedClass.GetMember<FieldSymbol>("field3");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 * 00 00 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, false, true };
                ValidateDynamicAttribute(field3.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public const dynamic field4 = null;
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 ) 
                var field4 = _derivedClass.GetMember<FieldSymbol>("field4");
                ValidateDynamicAttribute(field4.GetAttributes(), expectedDynamicAttribute: true);

                //public const dynamic[] field5 = null;
                var field5 = _derivedClass.GetMember<FieldSymbol>("field5");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, true };
                ValidateDynamicAttribute(field5.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public const dynamic[][] field6 = null;
                var field6 = _derivedClass.GetMember<FieldSymbol>("field6");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 * 00 00 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, false, true };
                ValidateDynamicAttribute(field6.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public const dynamic[][] field7 = null;
                var field7 = _derivedClass.GetMember<FieldSymbol>("field7");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 * 00 00 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, false, true };
                ValidateDynamicAttribute(field7.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public Outer<T>.Inner<int, T>.InnerInner<Outer<dynamic>> field8 = null;
                var field8 = _derivedClass.GetMember<FieldSymbol>("field8");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 06 00 00 00 * 00 00 00 00 00 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, false, false, false, false, true };
                ValidateDynamicAttribute(field8.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public Outer<dynamic>.Inner<T, T>.InnerInner<T> field9 = null;
                var field9 = _derivedClass.GetMember<FieldSymbol>("field9");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 05 00 00 00 * 00 01 00 00 00 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, true, false, false, false };
                ValidateDynamicAttribute(field9.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public Outer<Outer<dynamic>.Inner<T, dynamic>>.Inner<dynamic, T>.InnerInner<T> field10 = null;
                var field10 = _derivedClass.GetMember<FieldSymbol>("field10");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 08 00 00 00 * 00 00 01 00 01 01 00 00 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, false, true, false, true, true, false, false };
                ValidateDynamicAttribute(field10.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public Outer<T>.Inner<dynamic, dynamic>.InnerInner<T> field11 = null;
                var field11 = _derivedClass.GetMember<FieldSymbol>("field11");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 05 00 00 00 * 00 00 01 01 00 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, false, true, true, false };
                ValidateDynamicAttribute(field11.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public Outer<T>.Inner<T, T>.InnerInner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>> field12 = null;
                var field12 = _derivedClass.GetMember<FieldSymbol>("field12");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 09 00 00 00 * 00 00 00 00 00 01 00 01 00 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, false, false, false, false, true, false, true, false };
                ValidateDynamicAttribute(field12.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public Outer<dynamic>.Inner<Outer<T>, T>.InnerInner<dynamic> field13 = null;
                var field13 = _derivedClass.GetMember<FieldSymbol>("field13");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 06 00 00 00 * 00 01 00 00 00 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, true, false, false, false, true };
                ValidateDynamicAttribute(field13.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public Outer<dynamic>.Inner<dynamic, dynamic>.InnerInner<dynamic> field14 = null;
                var field14 = _derivedClass.GetMember<FieldSymbol>("field14");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 05 00 00 00 * 00 01 01 01 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, true, true, true, true };
                ValidateDynamicAttribute(field14.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public Outer<dynamic>.Inner<Outer<dynamic>, T>.InnerInner<dynamic>[] field15 = null;
                var field15 = _derivedClass.GetMember<FieldSymbol>("field15");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 07 00 00 00 * 00 00 01 00 01 00 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, false, true, false, true, false, true };
                ValidateDynamicAttribute(field15.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>.InnerInner<dynamic>[][] field16 = null;
                var field16 = _derivedClass.GetMember<FieldSymbol>("field16");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 0C 00 00 00 * 00 00 00 01 00 01 00 01 00 00 01 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, false, false, true, false, true, false, true, false, false, true, true };
                ValidateDynamicAttribute(field16.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] field17 = null;
                var field17 = _derivedClass.GetMember<FieldSymbol>("field17");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 0D 00 00 00 * 00 00 00 01 00 00 01 00 00 01 00 01 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, false, false, true, false, false, true, false, false, true, false, true, true };
                ValidateDynamicAttribute(field17.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                //public static Outer3.Inner3<dynamic> field1 = null;
                field1 = _inner3Class.GetMember<FieldSymbol>("field1");
                //   .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                expectedTransformFlags = new bool[] { false, true };
                ValidateDynamicAttribute(field1.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);
            }

            private void ValidateAttributesOnMethodReturnValueAndParameters()
            {
                //public static dynamic F1(dynamic x) { return x; }
                var f1 = _derivedClass.GetMember<MethodSymbol>("F1");
                ValidateDynamicAttribute(f1.GetAttributes(), expectedDynamicAttribute: false);
                //.param [0]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 ) 
                //.param [1]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 ) 
                ValidateDynamicAttribute(f1.GetReturnTypeAttributes(), expectedDynamicAttribute: true);
                ValidateDynamicAttribute(f1.Parameters[0].GetAttributes(), expectedDynamicAttribute: true);

                //public static dynamic F2(ref dynamic x) { return x; }
                var f2 = _derivedClass.GetMember<MethodSymbol>("F2");
                ValidateDynamicAttribute(f2.GetAttributes(), expectedDynamicAttribute: false);
                //.param [0]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 ) 
                //.param [1]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 )
                ValidateDynamicAttribute(f2.GetReturnTypeAttributes(), expectedDynamicAttribute: true);
                _expectedTransformFlags = new bool[] { false, true };
                ValidateDynamicAttribute(f2.Parameters[0].GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);

                //public static dynamic[] F3(dynamic[] x) { return x; }
                var f3 = _derivedClass.GetMember<MethodSymbol>("F3");
                ValidateDynamicAttribute(f3.GetAttributes(), expectedDynamicAttribute: false);
                //.param [0]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                //.param [1]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                ValidateDynamicAttribute(f3.GetReturnTypeAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);
                ValidateDynamicAttribute(f3.Parameters[0].GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);

                //public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
                var f4 = _derivedClass.GetMember<MethodSymbol>("F4");
                ValidateDynamicAttribute(f4.GetAttributes(), expectedDynamicAttribute: false);
                //.param [0]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 0D 00 00 00 * 00 00 00 01 00 00 01 00 00 01 00 01 01 * 00 00 ) 
                //.param [1]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 0D 00 00 00 * 00 00 00 01 00 00 01 00 00 01 00 01 01 * 00 00 ) 
                _expectedTransformFlags = new bool[] { false, false, false, true, false, false, true, false, false, true, false, true, true };
                ValidateDynamicAttribute(f4.GetReturnTypeAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);
                ValidateDynamicAttribute(f4.Parameters[0].GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);

                //public static (dynamic, object, dynamic) F5((dynamic, object, dynamic) x) { return x; }
                var f5 = _derivedClass.GetMember<MethodSymbol>("F5");
                ValidateDynamicAttribute(f5.GetAttributes(), expectedDynamicAttribute: false);
                //.param [0]
                //.custom instance void[System.Core] System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 00 01 01 00 00 )
                //.param[1]
                //.custom instance void[System.Core] System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 00 01 01 00 00 )
                _expectedTransformFlags = new bool[] { false, true, false, true };
                ValidateDynamicAttribute(f5.GetReturnTypeAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);
                ValidateDynamicAttribute(f5.Parameters[0].GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);
            }

            private void ValidateAttributesOnProperty()
            {
                //public static dynamic Prop1 { get { return field1; } }
                var prop1 = _derivedClass.GetMember<PropertySymbol>("Prop1");
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 ) 
                ValidateDynamicAttribute(prop1.GetAttributes(), expectedDynamicAttribute: true);

                // GetMethod
                //.param [0]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 ) 
                ValidateDynamicAttribute(prop1.GetMethod.GetReturnTypeAttributes(), expectedDynamicAttribute: true);

                //public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] Prop2 { get { return field17; } set { field17 = value; } }
                var prop2 = _derivedClass.GetMember<PropertySymbol>("Prop2");
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 0D 00 00 00 * 00 00 00 01 00 00 01 00 00 01 00 01 01 * 00 00 ) 
                _expectedTransformFlags = new bool[] { false, false, false, true, false, false, true, false, false, true, false, true, true };
                ValidateDynamicAttribute(prop2.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);

                // GetMethod
                //.param [0]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 0D 00 00 00 * 00 00 00 01 00 00 01 00 00 01 00 01 01 * 00 00 ) 
                ValidateDynamicAttribute(prop2.GetMethod.GetReturnTypeAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);

                // SetMethod
                //.param [1]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 0D 00 00 00 * 00 00 00 01 00 00 01 00 00 01 00 01 01 * 00 00 ) 
                ValidateDynamicAttribute(prop2.SetMethod.Parameters[0].GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);
            }

            private void ValidateAttributesOnIndexer()
            {
                // public dynamic this[dynamic param]
                var indexer = _derivedClass.GetIndexer<PropertySymbol>("Item");
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 ) 
                ValidateDynamicAttribute(indexer.GetAttributes(), expectedDynamicAttribute: true);

                // GetMethod
                //.param [0]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 ) 
                //.param [1]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 ) 
                ValidateDynamicAttribute(indexer.GetMethod.GetReturnTypeAttributes(), expectedDynamicAttribute: true);
                ValidateDynamicAttribute(indexer.GetMethod.Parameters[0].GetAttributes(), expectedDynamicAttribute: true);

                // SetMethod
                //.param [1]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 ) 
                //.param [2]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = ( 01 00 00 00 )
                ValidateDynamicAttribute(indexer.SetMethod.GetReturnTypeAttributes(), expectedDynamicAttribute: false);
                ValidateDynamicAttribute(indexer.SetMethod.Parameters[0].GetAttributes(), expectedDynamicAttribute: true);
                ValidateDynamicAttribute(indexer.SetMethod.Parameters[1].GetAttributes(), expectedDynamicAttribute: true);
            }

            private void ValidateAttributesForPointerType()
            {
                // public unsafe class UnsafeClass<T> : Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]> { }
                // .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 14 00 00 00 * 00 00 00 00 00 00 00 01 00 00 01 00 00 01 00 00 00 00 01 01 * 00 00 ) 
                _expectedTransformFlags = new bool[] { false, false, false, false, false, false, false, true, false, false, true, false, false, true, false, false, false, false, true, true };
                Assert.False(_unsafeClass.ContainsDynamic());
                Assert.True(_unsafeClass.BaseType().ContainsDynamic());
                ValidateDynamicAttribute(_unsafeClass.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);
            }

            private void ValidateAttributesForNullableType()
            {
                // public static Outer<dynamic>.Inner<dynamic, Struct?> nullableField;
                var nullableField = _structType.GetMember<FieldSymbol>("nullableField");
                // .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 05 00 00 00 * 00 01 01 00 00 * 00 00 ) 
                _expectedTransformFlags = new bool[] { false, true, true, false, false };
                ValidateDynamicAttribute(nullableField.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: _expectedTransformFlags);
            }

            private void ValidateAttributesForSynthesizedDelegateMembers()
            {
                // public delegate dynamic[] MyDelegate(dynamic[] x);

                // .class public auto ansi sealed MyDelegate
                //      extends [mscorlib]System.MulticastDelegate
                ValidateDynamicAttribute(_synthesizedMyDelegateType.GetAttributes(), expectedDynamicAttribute: false);

                var expectedTransformFlags = new bool[] { false, true };

                // MyDelegate::.ctor
                //
                // .method public hidebysig specialname rtspecialname 
                //  instance void  .ctor(object 'object',
                //                    native int 'method') runtime managed
                var ctor = _synthesizedMyDelegateType.InstanceConstructors[0];
                ValidateDynamicAttribute(ctor.GetAttributes(), expectedDynamicAttribute: false);
                ValidateDynamicAttribute(ctor.GetReturnTypeAttributes(), expectedDynamicAttribute: false);
                foreach (var param in ctor.Parameters)
                {
                    ValidateDynamicAttribute(param.GetAttributes(), expectedDynamicAttribute: false);
                }

                // Invoke method
                // 
                //  .method public hidebysig newslot virtual 
                //      instance object[]  Invoke(object[] x) runtime managed
                var invokeMethod = _synthesizedMyDelegateType.GetMember<MethodSymbol>("Invoke");
                ValidateDynamicAttribute(invokeMethod.GetAttributes(), expectedDynamicAttribute: false);
                //.param [0]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                //.param [1]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                ValidateDynamicAttribute(invokeMethod.GetReturnTypeAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);
                ValidateDynamicAttribute(invokeMethod.Parameters[0].GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);

                // BeginInvoke method
                //
                // .method public hidebysig newslot virtual 
                // instance class [mscorlib]System.IAsyncResult 
                //  BeginInvoke(object[] x,
                //      class [mscorlib]System.AsyncCallback callback,
                //      object 'object') runtime managed
                var beginInvokeMethod = _synthesizedMyDelegateType.GetMember<MethodSymbol>("BeginInvoke");
                ValidateDynamicAttribute(beginInvokeMethod.GetAttributes(), expectedDynamicAttribute: false);
                ValidateDynamicAttribute(beginInvokeMethod.GetReturnTypeAttributes(), expectedDynamicAttribute: false);
                //.param [1]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                var parameters = beginInvokeMethod.Parameters;
                ValidateDynamicAttribute(parameters[0].GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);
                ValidateDynamicAttribute(parameters[1].GetAttributes(), expectedDynamicAttribute: false);
                ValidateDynamicAttribute(parameters[2].GetAttributes(), expectedDynamicAttribute: false);

                // EndInvoke method
                //
                // .method public hidebysig newslot virtual 
                // instance object[]  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
                var endInvokeMethod = _synthesizedMyDelegateType.GetMember<MethodSymbol>("EndInvoke");
                ValidateDynamicAttribute(endInvokeMethod.GetAttributes(), expectedDynamicAttribute: false);
                //.param [0]
                //.custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 * 00 01 * 00 00 ) 
                ValidateDynamicAttribute(endInvokeMethod.GetReturnTypeAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformFlags);
                ValidateDynamicAttribute(endInvokeMethod.Parameters[0].GetAttributes(), expectedDynamicAttribute: false);
            }

            public static void ValidateDynamicAttribute(ImmutableArray<CSharpAttributeData> attributes, bool expectedDynamicAttribute, bool[] expectedTransformFlags = null)
            {
                var synthesizedDynamicAttributes = attributes.Where((attr) => string.Equals(attr.AttributeClass.Name, "DynamicAttribute", StringComparison.Ordinal));

                if (!expectedDynamicAttribute)
                {
                    Assert.Empty(synthesizedDynamicAttributes);
                }
                else
                {
                    var dynamicAttribute = synthesizedDynamicAttributes.Single();

                    if (expectedTransformFlags == null)
                    {
                        Assert.Empty(dynamicAttribute.AttributeConstructor.Parameters);
                        Assert.Empty(dynamicAttribute.CommonConstructorArguments);
                    }
                    else
                    {
                        Assert.Equal("System.Boolean[]", dynamicAttribute.AttributeConstructor.Parameters.Single().TypeWithAnnotations.ToTestDisplayString());

                        TypedConstant argument = dynamicAttribute.CommonConstructorArguments.Single();
                        Assert.Equal(TypedConstantKind.Array, argument.Kind);

                        ImmutableArray<TypedConstant> actualTransformFlags = argument.Values;
                        Assert.Equal(expectedTransformFlags.Length, actualTransformFlags.Length);

                        for (int i = 0; i < actualTransformFlags.Length; i++)
                        {
                            TypedConstant actualTransformFlag = actualTransformFlags[i];

                            Assert.Equal(TypedConstantKind.Primitive, actualTransformFlag.Kind);
                            Assert.Equal("System.Boolean", actualTransformFlag.Type.ToTestDisplayString());
                            Assert.Equal(expectedTransformFlags[i], (bool)actualTransformFlag.Value);
                        }
                    }
                }
            }
        }

        [Fact]
        public void CS1980ERR_DynamicAttributeMissing()
        {
            var comp = CreateCompilationWithMscorlib46(s_dynamicTestSource, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (6,31): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public class Outer<T> : Base1<dynamic>
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(6, 31),
                // (14,32): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public class Outer2<T> : Base1<dynamic>
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(14, 32),
                // (8,38): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public class Inner<U, V> : Base2<dynamic, V>
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(8, 38),
                // (10,44): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //         public class InnerInner<W> : Base1<dynamic> { }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(10, 44),
                // (26,37): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //         public static Outer3.Inner3<dynamic> field1 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(26, 37),
                // (78,17): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public delegate dynamic[] MyDelegate(dynamic[] x);
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(78, 17),
                // (78,38): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public delegate dynamic[] MyDelegate(dynamic[] x);
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(78, 38),
                // (30,33): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public class Derived<T> : Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(30, 33),
                // (30,54): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public class Derived<T> : Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(30, 54),
                // (30,74): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public class Derived<T> : Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(30, 74),
                // (30,102): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public class Derived<T> : Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(30, 102),
                // (30,122): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public class Derived<T> : Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(30, 122),
                // (71,58): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public unsafe class UnsafeClass<T> : Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]> { }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(71, 58),
                // (71,79): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public unsafe class UnsafeClass<T> : Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]> { }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(71, 79),
                // (71,99): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public unsafe class UnsafeClass<T> : Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]> { }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(71, 99),
                // (71,132): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public unsafe class UnsafeClass<T> : Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]> { }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(71, 132),
                // (71,152): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public unsafe class UnsafeClass<T> : Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]> { }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(71, 152),
                // (75,25): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<dynamic, Struct?> nullableField;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(75, 25),
                // (75,40): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<dynamic, Struct?> nullableField;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(75, 40),
                // (37,18): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public const dynamic field4 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(37, 18),
                // (38,18): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public const dynamic[] field5 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(38, 18),
                // (39,18): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public const dynamic[][] field6 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(39, 18),
                // (40,18): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public const dynamic[][] field7 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(40, 18),
                // (54,30): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static dynamic F1(dynamic x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(54, 30),
                // (54,19): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static dynamic F1(dynamic x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(54, 19),
                // (55,34): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static dynamic F2(ref dynamic x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(55, 34),
                // (55,19): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static dynamic F2(ref dynamic x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(55, 19),
                // (56,32): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static dynamic[] F3(dynamic[] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(56, 32),
                // (56,19): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static dynamic[] F3(dynamic[] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(56, 19),
                // (57,136): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(57, 136),
                // (57,157): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(57, 157),
                // (57,177): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(57, 177),
                // (57,205): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(57, 205),
                // (57,225): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(57, 225),
                // (57,25): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(57, 25),
                // (57,46): error CS198 0: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(57, 46),
                // (57,66): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(57, 66),
                // (57,94): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(57, 94),
                // (57,114): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(57, 114),
                // (59,19): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static dynamic Prop1 { get { return field1; } }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(59, 19),
                // (60,25): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] Prop2 { get { return field17; } set { field17 = value; } }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(60, 25),
                // (60,46): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] Prop2 { get { return field17; } set { field17 = value; } }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(60, 46),
                // (60,66): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] Prop2 { get { return field17; } set { field17 = value; } }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(60, 66),
                // (60,94): error CS1980: C annot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] Prop2 { get { return field17; } set { field17 = value; } }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(60, 94),
                // (60,114): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] Prop2 { get { return field17; } set { field17 = value; } }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(60, 114),
                // (62,25): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public dynamic this[dynamic param]
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(62, 25),
                // (62,12): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public dynamic this[dynamic param]
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(62, 12),
                // (68,50): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static (dynamic, object, dynamic) F5((dynamic, object, dynamic) x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(68, 50),
                // (68,67): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static (dynamic, object, dynamic) F5((dynamic, object, dynamic) x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(68, 67),
                // (68,20): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static (dynamic, object, dynamic) F5((dynamic, object, dynamic) x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(68, 20),
                // (68,37): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static (dynamic, object, dynamic) F5((dynamic, object, dynamic) x) { return x; }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(68, 37),
                // (34,19): error CS1980: Cannot  define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static dynamic[] field2;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(34, 19),
                // (35,19): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static dynamic[][] field3;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(35, 19),
                // (42,52): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<T>.Inner<int, T>.InnerInner<Outer<dynamic>> field8 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(42, 52),
                // (43,18): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<T, T>.InnerInner<T> field9 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(43, 18),
                // (44,24): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<Outer<dynamic>.Inner<T, dynamic>>.Inner<dynamic, T>.InnerInner<T> field10 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(44, 24),
                // (44,42): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<Outer<dynamic>.Inner<T, dynamic>>.Inner<dynamic, T>.InnerInner<T> field10 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(44, 42),
                // (44,58): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<Outer<dynamic>.Inner<T, dynamic>>.Inner<dynamic, T>.InnerInner<T> field10 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(44, 58),
                // (45,27): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<T>.Inner<dynamic, dynamic>.InnerInner<T> field11 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(45, 27),
                // (45,36): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<T>.Inner<dynamic, dynamic>.InnerInner<T> field11 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(45, 36),
                // (46,50): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<T>.Inner<T, T>.InnerInner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>> field12 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(46, 50),
                // (46,68): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<T>.Inner<T, T>.InnerInner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>> field12 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(46, 68),
                // (47,18): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<Outer<T>, T>.InnerInner<dynamic> field13 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(47, 18),
                // (47,57): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<Outer<T>, T>.InnerInner<dynamic> field13 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(47, 57),
                // (48,18): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<dynamic, dynamic>.InnerInner<dynamic> field14 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(48, 18),
                // (48,33): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<dynamic, dynamic>.InnerInner<dynamic> field14 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(48, 33),
                // (48,42): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<dynamic, dynamic>.InnerInner<dynamic> field14 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(48, 42),
                // (48,62): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<dynamic, dynamic>.InnerInner<dynamic> field14 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(48, 62),
                // (50,18): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<Outer<dynamic>, T>.InnerInner<dynamic>[] field15 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(50, 18),
                // (50,39): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<Outer<dynamic>, T>.InnerInner<dynamic>[] field15 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(50, 39),
                // (50,63): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<Outer<dynamic>, T>.InnerInner<dynamic>[] field15 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(50, 63),
                // (51,18): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>.InnerInner<dynamic>[][] field16 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(51, 18),
                // (51,39): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>.InnerInner<dynamic>[][] field16 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(51, 39),
                // (51,57): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>.InnerInner<dynamic>[][] field16 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(51, 57),
                // (51,83): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>.InnerInner<dynamic>[][] field16 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(51, 83),
                // (51,105): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>.InnerInner<dynamic>[][] field16 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(51, 105),
                // (52,25): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] field17 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(52, 25),
                // (52,46): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] field17 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(52, 46),
                // (52,66): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] field17 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(52, 66),
                // (52,94): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] field17 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(52, 94),
                // (52,114): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] field17 = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(52, 114),
                // (33,19): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public static dynamic field1;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(33, 19)
                );
        }

        [Fact]
        public void TestDynamicAttributeForScript_Field()
        {
            string source = @"
dynamic x = 0;
";
            var comp = CreateCompilationWithMscorlib45(
                source: source,
                parseOptions: TestOptions.Script,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                references: new[] { SystemCoreRef, CSharpDesktopRef });

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var implicitField = module.GlobalNamespace.GetTypeMember("Script").GetMember<FieldSymbol>("x");
                DynamicAttributeValidator.ValidateDynamicAttribute(implicitField.GetAttributes(), expectedDynamicAttribute: true);
            });
        }

        [Fact]
        public void TestDynamicAttributeForScript_NoCore()
        {
            string source = @"
dynamic x = 0;
";

            CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (2,1): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // dynamic x = 0;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute"));
        }

        [Fact]
        public void TestDynamicAttributeForScript_DynamicTypeArgument()
        {
            var source = GetNoCS1980String(typeName: @"Gen<dynamic>") + @"
Gen<dynamic> x = null;";

            var comp = CreateCompilationWithMscorlib45(
                source: source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                parseOptions: TestOptions.Script,
                references: new[] { SystemCoreRef });

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var implicitField = module.GlobalNamespace.GetTypeMember("Script").GetMember<FieldSymbol>("x");
                var expectedTransformsFlags = new bool[] { false, true };
                DynamicAttributeValidator.ValidateDynamicAttribute(implicitField.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformsFlags);
            });
        }

        [Fact]
        public void TestDynamicAttributeForScript_DynamicTypeArgument_NoCore()
        {
            var source = GetNoCS1980String(typeName: "Gen<dynamic>") + @"
Gen<dynamic> x = null;";

            var comp = CreateCompilationWithMscorlib45(source: source, parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (21,5): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // Gen<dynamic> x = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(21, 5)
                );
        }

        [Fact]
        public void TestDynamicAttributeForScript_DynamicTypeInAliasTarget()
        {
            var source =
                "using X = Gen<dynamic>;"
                + GetNoCS1980String(typeName: @"Gen<dynamic>")
                + "X x = null;";

            var comp = CreateCompilationWithMscorlib45(
                source: source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                parseOptions: TestOptions.Script,
                references: new[] { SystemCoreRef });

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var implicitField = module.GlobalNamespace.GetTypeMember("Script").GetMember<FieldSymbol>("x");
                var expectedTransformsFlags = new bool[] { false, true };
                DynamicAttributeValidator.ValidateDynamicAttribute(implicitField.GetAttributes(), expectedDynamicAttribute: true, expectedTransformFlags: expectedTransformsFlags);
            });
        }

        [Fact]
        public void TestDynamicAttributeForScript_DynamicTypeInAliasTarget_NoCore()
        {
            var source =
                "using X = Gen<dynamic>;"
                + GetNoCS1980String(typeName: @"Gen<dynamic>")
                + "X x = null;";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (20,1): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // X x = null;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "X").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(20, 1));
        }

        [Fact, WorkItem(531108, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531108")]
        public void DynamicAttributeCtorCS1980BreakingChange()
        {
            var customDynamicAttrSource = @"
namespace System
{
    namespace Runtime
    {
        namespace CompilerServices
        {
            public class DynamicAttribute : Attribute
            {
                public DynamicAttribute() {}
            }
        }
    }
}";
            var customRef = CreateCompilationWithMscorlib46(customDynamicAttrSource).ToMetadataReference();

            var source = @"
public class C<T>
{
    public C<dynamic> field2;   // Uses missing ctor ""DynamicAttribute(bool[] transformFlags)"", generates CS1980
}";
            var comp = CreateCompilationWithMscorlib46(source, references: new[] { customRef });
            comp.VerifyDiagnostics(
                // (4,14): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public C<dynamic> field2;   // Uses missing ctor "DynamicAttribute(bool[] transformFlags)", generates CS1980
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute"));

            source = @"
public class C<T>
{
    public dynamic field1;      // Uses available ctor ""DynamicAttribute()"", No CS1980 in native compiler.
}";
            // Bug 531108-Won't Fix
            comp = CreateCompilationWithMscorlib46(source, references: new[] { customRef });
            comp.VerifyDiagnostics(
                // (4,12): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public dynamic field1;      // Uses available ctor "DynamicAttribute()", No CS1980 in native compiler.
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute")
             );
        }

        [Fact]
        public void ExplicitDynamicAttribute()
        {
            var text = @"
using System.Runtime.CompilerServices;

[Dynamic(new[] { true })]
public class C
{
    [Dynamic(new[] { true })]
    public object F = null;

    [Dynamic(new[] { true })]
    public object P { get; set; }
    
    [return: Dynamic(new[] { true })]
    public void M([Dynamic(new[] { true })]object a) 
    {
    }
}

[Dynamic(new bool[] { true })]
public struct S { }
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (4,2): error CS1970: Do not use 'System.Runtime.CompilerServices.DynamicAttribute'. Use the 'dynamic' keyword instead.
                Diagnostic(ErrorCode.ERR_ExplicitDynamicAttr, "Dynamic(new[] { true })"),
                // (19,2): error CS1970: Do not use 'System.Runtime.CompilerServices.DynamicAttribute'. Use the 'dynamic' keyword instead.
                Diagnostic(ErrorCode.ERR_ExplicitDynamicAttr, "Dynamic(new bool[] { true })"),
                // (10,6): error CS1970: Do not use 'System.Runtime.CompilerServices.DynamicAttribute'. Use the 'dynamic' keyword instead.
                Diagnostic(ErrorCode.ERR_ExplicitDynamicAttr, "Dynamic(new[] { true })"),
                // (13,14): error CS1970: Do not use 'System.Runtime.CompilerServices.DynamicAttribute'. Use the 'dynamic' keyword instead.
                Diagnostic(ErrorCode.ERR_ExplicitDynamicAttr, "Dynamic(new[] { true })"),
                // (14,20): error CS1970: Do not use 'System.Runtime.CompilerServices.DynamicAttribute'. Use the 'dynamic' keyword instead.
                Diagnostic(ErrorCode.ERR_ExplicitDynamicAttr, "Dynamic(new[] { true })"),
                // (7,6): error CS1970: Do not use 'System.Runtime.CompilerServices.DynamicAttribute'. Use the 'dynamic' keyword instead.
                Diagnostic(ErrorCode.ERR_ExplicitDynamicAttr, "Dynamic(new[] { true })"));
        }

        [Fact]
        public void DynamicAttributeType()
        {
            var text = @"
[dynamic]
public class C
{
    [return: dynamic]
    [dynamic]
    public void dynamic([dynamic]dynamic dynamic) { }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (2,2): error CS0246: The type or namespace name 'dynamicAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [dynamic]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "dynamic").WithArguments("dynamicAttribute").WithLocation(2, 2),
                // (2,2): error CS0246: The type or namespace name 'dynamic' could not be found (are you missing a using directive or an assembly reference?)
                // [dynamic]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "dynamic").WithArguments("dynamic").WithLocation(2, 2),
                // (6,6): error CS0246: The type or namespace name 'dynamicAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //     [dynamic]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "dynamic").WithArguments("dynamicAttribute").WithLocation(6, 6),
                // (6,6): error CS0246: The type or namespace name 'dynamic' could not be found (are you missing a using directive or an assembly reference?)
                //     [dynamic]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "dynamic").WithArguments("dynamic").WithLocation(6, 6),
                // (5,14): error CS0246: The type or namespace name 'dynamicAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //     [return: dynamic]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "dynamic").WithArguments("dynamicAttribute").WithLocation(5, 14),
                // (5,14): error CS0246: The type or namespace name 'dynamic' could not be found (are you missing a using directive or an assembly reference?)
                //     [return: dynamic]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "dynamic").WithArguments("dynamic").WithLocation(5, 14),
                // (7,26): error CS0246: The type or namespace name 'dynamicAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //     public void dynamic([dynamic]dynamic dynamic) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "dynamic").WithArguments("dynamicAttribute").WithLocation(7, 26),
                // (7,26): error CS0246: The type or namespace name 'dynamic' could not be found (are you missing a using directive or an assembly reference?)
                //     public void dynamic([dynamic]dynamic dynamic) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "dynamic").WithArguments("dynamic").WithLocation(7, 26));
        }

        [Fact]
        [WorkItem(552843, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552843")]
        public void IteratorYieldingDynamic()
        {
            string source = @"
using System.Collections.Generic;
 
class C
{
    static IEnumerable<dynamic> Goo()
    {
        yield break;
    }
}
";
            CompileAndVerify(source, references: new[] { CSharpRef }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var iterator = c.GetMember<NamedTypeSymbol>("<Goo>d__0");
                var getEnumerator = iterator.GetMethod("System.Collections.Generic.IEnumerable<dynamic>.GetEnumerator");
                var attrs = getEnumerator.GetAttributes();

                foreach (var attr in attrs)
                {
                    switch (attr.AttributeClass.Name)
                    {
                        case "DebuggerHiddenAttribute":
                            break;

                        case "DynamicAttribute":
                            var values = attr.ConstructorArguments.Single().Values.ToArray();
                            Assert.Equal(2, values.Length);
                            Assert.Equal(false, values[0].Value);
                            Assert.Equal(true, values[1].Value);
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(attr.AttributeClass.Name);
                    }
                }
            });
        }

        [Fact]
        public void DynamicLambdaParameterChecksDynamic()
        {
            var source =
@"using System;

class C
{
    static void Main()
    {
        Func<dynamic, dynamic[], object> f = (x, y) => x;
        f(null, null);
    }
}";

            // Make sure we emit without errors when dynamic attributes are not present. 
            CompileAndVerifyWithMscorlib40(source, new[] { CSharpRef }, expectedSignatures: new[]
            {
                Signature(
                    "C+<>c",
                    "<Main>b__0_0",
                    ".method assembly hidebysig instance System.Object <Main>b__0_0(System.Object x, System.Object[] y) cil managed")
            });
        }

        [Fact]
        [WorkItem(4160, "https://github.com/dotnet/roslyn/issues/4160")]
        public void DynamicLambdaParametersEmitAsDynamic()
        {
            var source =
@"using System;

class C
{
    static void Main()
    {
        Func<dynamic, dynamic[], object> f = (x, y) => x;
        f(null, null);
    }
}";

            CompileAndVerify(source, references: new[] { CSharpRef }, expectedSignatures: new[]
            {
                Signature(
                    "C+<>c",
                    "<Main>b__0_0",
                    ".method assembly hidebysig instance System.Object <Main>b__0_0([System.Runtime.CompilerServices.DynamicAttribute()] System.Object x, [System.Runtime.CompilerServices.DynamicAttribute(System.Collections.ObjectModel.ReadOnlyCollection`1[System.Reflection.CustomAttributeTypedArgument])] System.Object[] y) cil managed")
            });
        }

        [Fact]
        [WorkItem(6126, "https://github.com/dotnet/roslyn/issues/6126")]
        public void DynamicLambdaParametersMissingBoolean()
        {
            var source0 =
@"namespace System
{
    public class Object { }
    public class ValueType { }
    public struct Void { }
    public struct IntPtr { }
    public class MulticastDelegate { }
}";
            var source1 =
@"delegate void D<T>(T t);
class C
{
    static void Main()
    {
        D<dynamic[]> d = o => { };
        d(null);
    }
}";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();
            comp = CreateEmptyCompilation(source1, references: new[] { ref0, SystemCoreRef });
            comp.VerifyDiagnostics();
            // Make sure we emit without errors when System.Boolean is missing.
            // PEVerify: Type load failed.
            // ILVerify: Failed to load type 'System.String' from assembly ...
            CompileAndVerify(comp, verify: Verification.Fails);
        }

        [Fact]
        [WorkItem(7840, "https://github.com/dotnet/roslyn/issues/7840")]
        public void DynamicDisplayClassFieldMissingBoolean()
        {
            var source0 =
@"namespace System
{
    public class Object { }
    public struct Int32 { }
    public class ValueType { }
    public class Attribute { }
    public struct Void { }
    public struct IntPtr { }
    public class MulticastDelegate { }
}";
            var source1 =
@"delegate void D();
class C
{
    static void Main()
    {
        dynamic x = 1;
        D d = () => { dynamic y = x; };
    }
}";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();
            comp = CreateEmptyCompilation(source1, references: new[] { ref0, SystemCoreRef });
            comp.VerifyDiagnostics();
            // Make sure we emit without errors when System.Boolean is missing.
            // PEVerify: Type load failed.
            // ILVerify: Internal.TypeSystem.TypeSystemException+TypeLoadException : Failed to load type 'System.String' from assembly ...
            CompileAndVerify(comp, verify: Verification.Fails);
        }

        [Fact]
        public void BackingField()
        {
            var source =
@"class C
{
    static dynamic[] P { get; set; }
}";
            CompileAndVerify(source, references: new[] { CSharpRef }, expectedSignatures: new[]
            {
                Signature(
                    "C",
                    "<P>k__BackingField",
                    ".field [System.Runtime.CompilerServices.DynamicAttribute(System.Collections.ObjectModel.ReadOnlyCollection`1[System.Reflection.CustomAttributeTypedArgument])] [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] private static System.Object[] <P>k__BackingField")
            });
        }

        [Fact]
        [WorkItem(1095613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1095613")]
        public void DisplayClassField()
        {
            var source =
@"using System;
class C
{
    static void F(dynamic[] a)
    {
        H(() => G(a));
        dynamic d = a;
        H(() => G(d));
    }
    static void G(object a)
    {
    }
    static void H(Action a)
    {
    }
    static void Main()
    {
        F(new object[0]);
    }
}";
            CompileAndVerify(source, references: new[] { CSharpRef }, expectedSignatures: new[]
            {
                Signature(
                    "C+<>c__DisplayClass0_0",
                    "a",
                    ".field [System.Runtime.CompilerServices.DynamicAttribute(System.Collections.ObjectModel.ReadOnlyCollection`1[System.Reflection.CustomAttributeTypedArgument])] public instance System.Object[] a"),
                Signature(
                    "C+<>c__DisplayClass0_0",
                    "d",
                    ".field [System.Runtime.CompilerServices.DynamicAttribute()] public instance System.Object d")
            });
        }

        [Theory]
        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        public void TestNoCS1980WhenNotInContextWhichNeedsDynamicAttribute(SourceCodeKind sourceCodeKind)
        {
            CompileAndVerify(CreateCompilationWithMscorlib45(
                source: GetNoCS1980String(typeName: @"Gen<dynamic>"),
                parseOptions: new CSharpParseOptions(kind: sourceCodeKind, languageVersion: LanguageVersion.CSharp7_2)));
        }

        [Theory]
        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        public void TestNoCS1980WhenNotInContextWhichNeedsDynamicAttribute_Errors(SourceCodeKind sourceCodeKind)
        {
            var comp = CreateCompilationWithMscorlib45(
                source: GetNoCS1980String(typeName: @"dynamic"),
                parseOptions: new CSharpParseOptions(kind: sourceCodeKind, languageVersion: LanguageVersion.CSharp7_2));

            comp.VerifyDiagnostics(
                // (2,7): error CS1962: The typeof operator cannot be used on the dynamic type
                // [Attr(typeof(dynamic))]            // No CS1980
                Diagnostic(ErrorCode.ERR_BadDynamicTypeof, "typeof(dynamic)").WithLocation(2, 7),
                // (5,21): error CS1962: The typeof operator cannot be used on the dynamic type
                //   public object f = typeof(dynamic);  // No CS1980
                Diagnostic(ErrorCode.ERR_BadDynamicTypeof, "typeof(dynamic)").WithLocation(5, 21),
                // (12,16): error CS1962: The typeof operator cannot be used on the dynamic type
                //     object y = typeof(dynamic);   // No CS1980
                Diagnostic(ErrorCode.ERR_BadDynamicTypeof, "typeof(dynamic)").WithLocation(12, 16));
        }

        [Fact]
        public void TestDynamicAttributeForSubmissionGlobalStatement()
        {
            string source =
                "using X = Gen<dynamic>;     // No CS1980"
                + GetNoCS1980String(typeName: @"Gen<dynamic>")
                + "System.Console.WriteLine(typeof(dynamic));"
                + "System.Console.WriteLine(typeof(Gen<dynamic>));"
                + "System.Console.WriteLine(typeof(X));";

            CreateCompilationWithMscorlib45(source: source, parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (20,26): error CS1962: The typeof operator cannot be used on the dynamic type
                // System.Console.WriteLine(typeof(dynamic));System.Console.WriteLine(typeof(Gen<dynamic>));System.Console.WriteLine(typeof(X));
                Diagnostic(ErrorCode.ERR_BadDynamicTypeof, "typeof(dynamic)").WithLocation(20, 26)
            );
        }

        [Theory]
        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        public void TestDynamicAttributeInAliasContext(SourceCodeKind sourceCodeKind)
        {
            string source =
                "using X = Gen<dynamic>;     // No CS1980"
                + GetNoCS1980String(typeName: "X");

            CompileAndVerify(CreateCompilationWithMscorlib45(
                source: source,
                parseOptions: new CSharpParseOptions(kind: sourceCodeKind, languageVersion: LanguageVersion.CSharp7_2)));
        }

        [Theory]
        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        public void TestDynamicAttributeInAliasContext_Errors(SourceCodeKind sourceCodeKind)
        {
            string source =
                "using X = Gen<dynamic>;     // No CS1980"
                + GetNoCS1980String(typeName: "X") + @"
public class Gen2<T> : X    // CS1980
{
  public X field = null;   // CS1980

  private X Method(X param) // CS1980, CS1980
  {
     return param;
  }

  private X Prop { get; set; } // CS1980

  private X this[X param]   // CS1980, CS1980
  {
    get { return null; }
    set {}
  }
}";

            CreateCompilationWithMscorlib45(source: source, parseOptions: new CSharpParseOptions(kind: sourceCodeKind, languageVersion: LanguageVersion.CSharp7_2)).VerifyDiagnostics(
                // (21,24): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // public class Gen2<T> : X    // CS1980
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "X").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(21, 24),
                // (25,20): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //   private X Method(X param) // CS1980, CS1980
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "X").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(25, 20),
                // (25,11): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //   private X Method(X param) // CS1980, CS1980
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "X").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(25, 11),
                // (30,11): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //   private X Prop { get; set; } // CS1980
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "X").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(30, 11),
                // (32,18): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //   private X this[X param]   // CS1980, CS1980
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "X").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(32, 18),
                // (32,11): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //   private X this[X param]   // CS1980, CS1980
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "X").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(32, 11),
                // (23,10): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //   public X field = null;   // CS1980
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "X").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(23, 10)
                );
        }

        private static string GetNoCS1980String(string typeName)
        {
            const string noCS1980String = @"
[Attr(typeof(%TYPENAME%))]            // No CS1980
public class Gen<T>
{
  public object f = typeof(%TYPENAME%);  // No CS1980
  public const object Const = null;

  private void M([Attr(Gen<dynamic>.Const)]object param = Gen<dynamic>.Const)     // No CS1980
  {
    %TYPENAME% x = null;             // No CS1980
    System.Console.WriteLine(x);
    object y = typeof(%TYPENAME%);   // No CS1980
  }
}

class Attr: System.Attribute
{
  public Attr(object x) {}
}
";
            return noCS1980String.Replace("%TYPENAME%", typeName);
        }
    }
}
