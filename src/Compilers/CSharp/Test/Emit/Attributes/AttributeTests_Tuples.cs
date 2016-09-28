using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_Tuples : WellKnownAttributesTestBase
    {
        private static readonly string s_tuplesTestSource = @"
using System;

public class Base0 { }
public class Base1<T> { }
public class Base2<T, U> { }

public class Outer<T> : Base1<(int key, int val)>
{
    public class Inner<U, V> : Base2<(int key2, int val2), V>
    {
        public class InnerInner<W> : Base1<(int key3, int val3)> { }
    }
}

public class Derived<T> : Outer<(int e1, (int e2, int e3) e4)>.Inner<
    Outer<(int e5, int e6)>.Inner<(int e7, int e8)[], (int e9, int e10)>.InnerInner<int>[],
    ((int e11, int e12) e13, int e14)>
    .InnerInner<((int e15, int e16) e17, (int e18, Base1<(int e19, int e20)> e21) e22)>
{
    public static (int e1, int e2) Field1;
    public static (int e1, int e2) Field2;
    public static Base1<(int e1, (int e2, int e3) e4)> Field3;

    public static ValueTuple<Base1<(int e1, (int, (dynamic, dynamic)) e2)>, int> Field4;

    public static Outer<(object e1, dynamic e2)>
        .Inner<(dynamic e3, object e4),
                ValueTuple<dynamic, dynamic>> Field5;

    // No names
    public static Base1<(int, ValueTuple<int, ValueTuple>)> Field6;
    public static ValueTuple Field7;

    // Long tuples
    public static (int e1, int e2, int e3, int e4, int e5,
                   int e6, int e7, int e8, int e9) Field8;
    public static Base1<(int e1, int e2, int e3, int e4, int e5,
                         int e6, int e7, int e8, int e9)> Field9;

    public static (int e1, int e2) Method1() => (0, 0);
    public static void Method2((int e1, int e2) x) { }
    public static (int e1, int e2) Method3((int e3, int e4) x) => (0, 0);
    public static (int e1, int e2) Method4(ref (int e3, int e4) x) => x;

    public static ((int,
                    (object, (dynamic, object)),
                    object,
                    int),
                   ValueTuple) Method5(ref (object,dynamic) x) =>
        ((0, (null, (null, null)), null, 0), default(ValueTuple));

    public static (int e1, int e2, int e3, int e4, int e5,
                   int e6, int e7, int e8, int e9) Method6() => (0, 0, 0, 0,
                                                                 0, 0, 0, 0, 0);

    public static (int e1, int e2) Prop1 => (0, 0);
    public static (int e1, int e2) Prop2 { get; set; }

    public (int e1, int e2) this[(int e3, int e4) param]
    {
        get { return param; }
        set {}
    }

    public delegate void Delegate1<V>(object sender,
        ValueTuple<V,
            ((dynamic e1, dynamic e2, object e3) e4, dynamic e5),
            (dynamic, object)> args);

    public static event Delegate1<(dynamic e1,
                                   ValueTuple<(dynamic e2, dynamic e3)> e4)> Event1
    {
        add { }
        remove { }
    }
}";

        private static MetadataReference[] s_attributeRefs =
        {
            ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef, CSharpRef
        };

        [Fact]
        public void TestCompile()
        {
            CompileAndVerify(s_tuplesTestSource,
                options: TestOptions.ReleaseDll,
                additionalRefs: s_attributeRefs);
        }

        [Fact]
        public void TestTupleAttributes()
        {
            var comp = CreateCompilationWithMscorlib(s_tuplesTestSource,
                options: TestOptions.UnsafeReleaseDll,
                references: s_attributeRefs);
            TupleAttributeValidator.ValidateTupleAttributes(comp);
        }

        [Fact]
        public void TupleAttributeWithOnlyOneConstructor()
        {
            var comp = CreateCompilationWithMscorlib(
                s_tuplesTestSource + TestResources.NetFX.ValueTuple.tuplelib_cs + @"
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that the use of <see cref=""System.ValueTuple""/> on a member is meant to be treated as a tuple with element names.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Class | AttributeTargets.Struct )]
    public sealed class TupleElementNamesAttribute : Attribute
    {
        public TupleElementNamesAttribute(string[] transformNames) { }
    }
}",
                references: new[] { SystemCoreRef },
                options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
            TupleAttributeValidator.ValidateTupleAttributes(comp);
        }

        [Fact]
        public void TupleLambdaParametersMissingString()
        {
            var source0 =
@"namespace System
{
    public class Object { }
    public class ValueType { }
    public struct Void { }
    public struct IntPtr { }
    public struct Int32 { }
    public class MulticastDelegate { }
}";
            var source1 =
@"delegate void D<T>(T t);
class C
{
    static void Main()
    {
        D<(int x, int y)> d = o => { };
        d((0, 0));
    }
}";
            var comp = CreateCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();
            comp = CreateCompilation(source1,
                references: s_attributeRefs.Concat(new[] { ref0 }));
            comp.VerifyDiagnostics(
                // (6,11): error CS0518: Predefined type 'System.String' is not defined or imported
                //         D<(int x, int y)> d = o => { };
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(int x, int y)").WithArguments("System.String").WithLocation(6, 11)
                );
        }

        [Fact]
        public void TupleInDeclarationFailureWithoutString()
        {
            var source0 =
@"namespace System
{
    public class Object { }
    public class ValueType { }
    public struct Void { }
    public struct IntPtr { }
    public struct Int32 { }
}";
            var source1 =
@"
class C
{
    static (int x, int y) M() => (0, 0);
}";
            var comp = CreateCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();
            comp = CreateCompilation(source1,
                references: new[] { ref0, ValueTupleRef });
            comp.VerifyDiagnostics(
                // (4,12): error CS0518: Predefined type 'System.String' is not defined or imported
                //     static (int x, int y) M() => (0, 0);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(int x, int y)").WithArguments("System.String").WithLocation(4, 12));
        }

        [Fact]
        public void RoundTrip()
        {
            ModuleSymbol sourceModule = null;
            ModuleSymbol peModule = null;
            CompileAndVerify(s_tuplesTestSource,
                options: TestOptions.UnsafeReleaseDll,
                additionalRefs: s_attributeRefs,
                sourceSymbolValidator: m => sourceModule = m,
                symbolValidator: m => peModule = m);

            var srcTypes = sourceModule.GlobalNamespace.GetTypeMembers();
            var peTypes = peModule.GlobalNamespace.GetTypeMembers()
                .Where(t => t.Name != "<Module>").ToList();

            Assert.Equal(srcTypes.Length, peTypes.Count);

            for (int i = 0; i < srcTypes.Length; i++)
            {
                var srcType = srcTypes[i];
                var peType = peTypes[i];

                Assert.Equal(ToTestString(srcType.BaseType), ToTestString(peType.BaseType));

                var srcMembers = srcType.GetMembers()
                    .Where(m => !m.Name.Contains("k__BackingField"))
                    .Select(ToTestString)
                    .ToList();
                var peMembers = peType.GetMembers()
                    .Select(ToTestString)
                    .ToList();

                Assert.Equal(srcMembers.Count, peMembers.Count);

                srcMembers.Sort();
                peMembers.Sort();

                for (int j = 0; j < srcMembers.Count; j++)
                {
                    var srcMember = srcMembers[j];
                    var peMember = peMembers[j];

                    Assert.Equal(srcMember, peMember);
                }
            }
        }

        private static string ToTestString(Symbol symbol)
        {
            var typeSymbols = new List<TypeSymbol>();
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    var methodSymbol = (MethodSymbol)symbol;
                    typeSymbols.Add(methodSymbol.ReturnType);
                    typeSymbols.AddRange(methodSymbol.ParameterTypes);
                    break;
                case SymbolKind.NamedType:
                    var namedType = (NamedTypeSymbol)symbol;
                    typeSymbols.Add(namedType.BaseType ?? namedType);
                    break;
                case SymbolKind.Field:
                    typeSymbols.Add(((FieldSymbol)symbol).Type);
                    break;
                case SymbolKind.Property:
                    typeSymbols.Add(((PropertySymbol)symbol).Type);
                    break;
                case SymbolKind.Event:
                    typeSymbols.Add(((EventSymbol)symbol).Type);
                    break;
            }
            var symbolString = string.Join(" | ", typeSymbols
                .Select(s => s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            return $"{symbol.Name}: {symbolString}";
        }

        private struct TupleAttributeValidator
        {
            private readonly MethodSymbol _tupleAttrTransformNames;
            private readonly ModuleSymbol _srcModule;
            private readonly CSharpCompilation _comp;
            private readonly NamedTypeSymbol _base0Class, _base1Class,
                _base2Class, _outerClass, _derivedClass;

            private TupleAttributeValidator(CSharpCompilation compilation)
            {
                _tupleAttrTransformNames = (MethodSymbol)compilation.GetWellKnownTypeMember(
                    WellKnownMember.System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames);

                _comp = compilation;
                _srcModule = compilation.SourceModule;
                var globalNs = _srcModule.GlobalNamespace;

                _base0Class = globalNs.GetTypeMember("Base0");
                _base1Class = globalNs.GetTypeMember("Base1");
                _base2Class = globalNs.GetTypeMember("Base2");
                _outerClass = globalNs.GetTypeMember("Outer");
                _derivedClass = globalNs.GetTypeMember("Derived");
            }

            internal static void ValidateTupleAttributes(CSharpCompilation comp)
            {
                var validator = new TupleAttributeValidator(comp);

                validator.ValidateAttributesOnNamedTypes();
                validator.ValidateAttributesOnFields();
                validator.ValidateAttributesOnMethods();
                validator.ValidateAttributesOnProperties();
                validator.ValidateAttributesOnEvents();
                validator.ValidateAttributesOnDelegates();
            }

            private void ValidateAttributesOnDelegates()
            {
                // public delegate void Delegate1<T>(object sender,
                //     ValueTuple<T,
                //         ((dynamic e1, dynamic e2, object e3) e4, dynamic e5),
                //         (dynamic, object)> args);
                var delegate1 = _derivedClass.GetMember<NamedTypeSymbol>("Delegate1");
                Assert.NotNull(delegate1);
                Assert.True(delegate1.IsDelegateType());

                var invokeMethod = delegate1.DelegateInvokeMethod;
                Assert.NotNull(invokeMethod);
                ValidateTupleNameAttribute(invokeMethod, expectedTupleNamesAttribute: false);

                Assert.Equal(2, invokeMethod.ParameterCount);
                var sender = invokeMethod.Parameters[0];
                Assert.Equal("sender", sender.Name);
                Assert.Equal(SpecialType.System_Object, sender.Type.SpecialType);
                ValidateTupleNameAttribute(sender, expectedTupleNamesAttribute: false);

                var args = invokeMethod.Parameters[1];
                Assert.Equal("args", args.Name);

                var expectedElementNames = new[]
                {
                    null, null, null, "e4", "e5", "e1", "e2", "e3", null, null
                };
                ValidateTupleNameAttribute(args,
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames);
                AttributeTests_Dynamic.DynamicAttributeValidator.ValidateDynamicAttribute(
                    args, _comp,
                    expectedDynamicAttribute: true,
                    expectedTransformFlags: new[]
                    {
                        false, false, false, false,
                        true, true, false, true,
                        false, true, false
                    });
            }

            private void ValidateAttributesOnEvents()
            {
                // public static event Delegate1<(dynamic e1,
                //                                ValueTuple<(dynamic e2, dynamic e3)> e4)> Event1;
                var event1Type = _derivedClass.GetMember<EventSymbol>("Event1");
                Assert.NotNull(event1Type);

                ValidateTupleNameAttribute(event1Type,
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: new[]
                    {
                        "e1", "e4", null, "e2", "e3"
                    });
                AttributeTests_Dynamic.DynamicAttributeValidator.ValidateDynamicAttribute(
                    event1Type, _comp,
                    expectedDynamicAttribute: true,
                    expectedTransformFlags: new[]
                    {
                        false, false, false, true,
                        false, false, true, true
                    });
            }

            private void ValidateAttributesOnNamedTypes()
            {
                // public class Base0 { }
                ValidateTupleNameAttribute(_base0Class, expectedTupleNamesAttribute: false);

                // public class Base1<T> { }
                ValidateTupleNameAttribute(_base1Class, expectedTupleNamesAttribute: false);

                // public class Base2<T, U> { }
                ValidateTupleNameAttribute(_base2Class, expectedTupleNamesAttribute: false);

                // public class Outer<T> : Base1<(int key, int val)>
                Assert.True(_outerClass.BaseType.ContainsTuple());
                var expectedElementNames = new[] { "key", "val" };
                ValidateTupleNameAttribute(_outerClass,
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames);

                // public class Derived<T> : Outer<(int e1, (int e2, int e3) e4)>.Inner<
                //     Outer<(int e5, int e6)>.Inner<(int e7, int e8)[], (int e9, int e10)>.InnerInner<int>[],
                //     ((int e11, int e12) e13, int e14)>
                //     .InnerInner<((int e15, int e16) e17, (int e18, Base1<(int e19, int e20)> e21) e22)>
                expectedElementNames = new[] {
                    "e1", "e4", "e2", "e3", "e5", "e6", "e7", "e8", "e9",
                    "e10", "e13", "e14", "e11", "e12", "e17", "e22", "e15",
                    "e16", "e18", "e21", "e19", "e20"
                };
                ValidateTupleNameAttribute(_derivedClass,
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames);
            }

            private void ValidateAttributesOnFields()
            {
                // public static (int e1, int e2) Field1;
                var field1 = _derivedClass.GetMember<FieldSymbol>("Field1");
                var expectedElementNames = new[] { "e1", "e2" };
                ValidateTupleNameAttribute(field1, expectedTupleNamesAttribute: true, expectedElementNames: expectedElementNames);

                // public static (int e1, int e2) Field2;
                var field2 = _derivedClass.GetMember<FieldSymbol>("Field2");
                expectedElementNames = new[] { "e1", "e2" };
                ValidateTupleNameAttribute(field2, expectedTupleNamesAttribute: true, expectedElementNames: expectedElementNames);

                // public static Base1<(int e1, (int e2, int e3) e4)> Field3;
                var field3 = _derivedClass.GetMember<FieldSymbol>("Field3");
                expectedElementNames = new[] { "e1", "e4", "e2", "e3" };
                ValidateTupleNameAttribute(field3, expectedTupleNamesAttribute: true, expectedElementNames: expectedElementNames);

                // public static ValueTuple<Base1<(int e1, (int, (dynamic, dynamic)) e2)>, int> Field4;
                var field4 = _derivedClass.GetMember<FieldSymbol>("Field4");
                expectedElementNames = new[] { null, null, "e1", "e2", null, null, null, null };
                ValidateTupleNameAttribute(field4, expectedTupleNamesAttribute: true, expectedElementNames: expectedElementNames);
                AttributeTests_Dynamic.DynamicAttributeValidator.ValidateDynamicAttribute(
                    field4, _comp,
                    expectedDynamicAttribute: true,
                    expectedTransformFlags: new[] {
                        false, false, false, false,
                        false, false, false, true,
                        true, false
                    });

                // public static Outer<(object e1, dynamic e2)>
                //     .Inner<(dynamic e3, object e4),
                //             ValueTuple<dynamic, dynamic>> Field5;
                var field5 = _derivedClass.GetMember<FieldSymbol>("Field5");
                expectedElementNames = new[] { "e1", "e2", "e3", "e4", null, null };
                ValidateTupleNameAttribute(field5,
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames);
                AttributeTests_Dynamic.DynamicAttributeValidator.ValidateDynamicAttribute(
                    field5, _comp,
                    expectedDynamicAttribute: true,
                    expectedTransformFlags: new[]
                    {
                        false, false, false, true,
                        false, true, false, false,
                        true, true 
                    });


                // public static Base1<(int, ValueTuple<int, ValueTuple>)> Field6;
                var field6 = _derivedClass.GetMember<FieldSymbol>("Field6");
                ValidateTupleNameAttribute(field6, expectedTupleNamesAttribute: false);
                var field6Type = Assert.IsType<ConstructedNamedTypeSymbol>(field6.Type);
                Assert.Equal("Base1", field6Type.Name);
                Assert.Equal(1, field6Type.TypeParameters.Length);
                var firstTuple = field6Type.TypeArguments.Single();
                Assert.True(firstTuple.IsTupleType);
                Assert.True(firstTuple.TupleElementNames.IsDefault);
                Assert.Equal(2, firstTuple.TupleElementTypes.Length);
                var secondTuple = firstTuple.TupleElementTypes[1];
                Assert.True(secondTuple.IsTupleType);
                Assert.True(secondTuple.TupleElementNames.IsDefault);
                Assert.Equal(2, secondTuple.TupleElementTypes.Length);

                // public static ValueTuple Field7;
                var field7 = _derivedClass.GetMember<FieldSymbol>("Field7");
                ValidateTupleNameAttribute(field7, expectedTupleNamesAttribute: false);
                Assert.False(field7.Type.IsTupleType);

                // public static (int e1, int e2, int e3, int e4, int e5, int e6, int e7, int e8, int e9) Field8;
                var field8 = _derivedClass.GetMember<FieldSymbol>("Field8");
                expectedElementNames = new[]
                {
                    "e1", "e2", "e3", "e4", "e5",
                    "e6", "e7", "e8", "e9", null, null
                };
                ValidateTupleNameAttribute(field8, expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames);

                // public static Base<(int e1, int e2, int e3, int e4, int e5, int e6, int e7, int e8, int e9)> Field9;
                var field9 = _derivedClass.GetMember<FieldSymbol>("Field9");
                expectedElementNames = new[]
                {
                    "e1", "e2", "e3", "e4", "e5",
                    "e6", "e7", "e8", "e9", null, null
                };
                ValidateTupleNameAttribute(field9, expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames);
            }

            private void ValidateAttributesOnMethods()
            {
                // public static (int e1, int e2) Method1() => (0, 0);
                var method1 = _derivedClass.GetMember<MethodSymbol>("Method1");
                var expectedElementNames = new[] { "e1", "e2" };
                ValidateTupleNameAttribute(method1,
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames,
                    forReturnType: true);

                // public static void Method2((int e1, int e2) x) { }
                var method2 = _derivedClass.GetMember<MethodSymbol>("Method2");
                expectedElementNames = new[] { "e1", "e2" };
                ValidateTupleNameAttribute(method2.Parameters.Single(),
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames);

                // public static (int e1, int e2) Method3((int e3, int e4) x) => (0, 0);
                var method3 = _derivedClass.GetMember<MethodSymbol>("Method3");
                expectedElementNames = new[] { "e1", "e2" };
                ValidateTupleNameAttribute(method3,
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames,
                    forReturnType: true);
                expectedElementNames = new[] { "e3", "e4" };
                ValidateTupleNameAttribute(method3.Parameters.Single(),
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames);

                // public static (int e1, int e2) Method4(ref (int e3, int e4) x) => x;
                var method4 = _derivedClass.GetMember<MethodSymbol>("Method4");
                expectedElementNames = new[] { "e1", "e2" };
                ValidateTupleNameAttribute(method4,
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames,
                    forReturnType: true);
                expectedElementNames = new[] { "e3", "e4" };
                ValidateTupleNameAttribute(method4.Parameters.Single(),
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames);

                // public static ((int,
                //                 (object, (dynamic, object)),
                //                 object,
                //                 int),
                //                ValueTuple) Method5(ref (object,dynamic) x) =>
                //     ((0, (null, (null, null)), null, 0), default(ValueTuple));
                var method5 = _derivedClass.GetMember<MethodSymbol>("Method5");
                ValidateTupleNameAttribute(method5,
                    expectedTupleNamesAttribute: false,
                    forReturnType: true);

                ValidateTupleNameAttribute(method5.Parameters.Single(),
                    expectedTupleNamesAttribute: false,
                    expectedElementNames: expectedElementNames);


                // public static (int e1, int e2, int e3, int e4, int e5,
                //                int e6, int e7, int e8, int e9) Method6() => (0, 0, 0, 0,
                //                                                              0, 0, 0, 0, 0);
                var field9 = _derivedClass.GetMember<MethodSymbol>("Method6");
                expectedElementNames = new[]
                {
                    "e1", "e2", "e3", "e4", "e5",
                    "e6", "e7", "e8", "e9", null, null
                };
                ValidateTupleNameAttribute(field9, expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedElementNames,
                    forReturnType: true);
            }

            private void ValidateAttributesOnProperties()
            {
                // public static (int e1, int e2) Prop1 => (0, 0);
                var prop1 = _derivedClass.GetMember("Prop1");
                var expectedTupleNames = new[] { "e1", "e2" };
                ValidateTupleNameAttribute(prop1,
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedTupleNames);

                // public static (int e1, int e2) Prop2 { get; set; }
                var prop2 = _derivedClass.GetMember("Prop2");
                expectedTupleNames = new[] { "e1", "e2" };
                ValidateTupleNameAttribute(prop2,
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedTupleNames);

                // public (int e1, int e2) this[(int e3, int e4) param]
                var indexer = (SourcePropertySymbol)_derivedClass.GetMember("this[]");
                expectedTupleNames = new[] { "e1", "e2" };
                ValidateTupleNameAttribute(indexer,
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedTupleNames);
                expectedTupleNames = new[] { "e3", "e4" };
                ValidateTupleNameAttribute(indexer.Parameters.Single(),
                    expectedTupleNamesAttribute: true,
                    expectedElementNames: expectedTupleNames);
            }

            private void ValidateTupleNameAttribute(
                Symbol symbol,
                bool expectedTupleNamesAttribute,
                string[] expectedElementNames = null,
                bool forReturnType = false)
            {
                var synthesizedTupleElementNamesAttr = symbol.GetSynthesizedAttributes(forReturnType)
                    .Where(attr => string.Equals(attr.AttributeClass.Name,
                                                 "TupleElementNamesAttribute",
                                                 StringComparison.Ordinal))
                    .ToList();
                if (!expectedTupleNamesAttribute)
                {
                    Assert.Empty(synthesizedTupleElementNamesAttr);
                }
                else
                {
                    var tupleAttr = synthesizedTupleElementNamesAttr.Single();
                    var expectedCtor = _tupleAttrTransformNames;
                    Assert.NotNull(expectedCtor);
                    Assert.Equal(expectedCtor, tupleAttr.AttributeConstructor);

                    if (expectedElementNames == null)
                    {
                        Assert.Empty(tupleAttr.CommonConstructorArguments);
                    }
                    else
                    {
                        Assert.Equal(1, tupleAttr.CommonConstructorArguments.Length);

                        var arg = tupleAttr.CommonConstructorArguments[0];
                        Assert.Equal(TypedConstantKind.Array, arg.Kind);

                        var actualElementNames = arg.Values;
                        Assert.Equal(expectedElementNames.Length, actualElementNames.Length);
                        var stringType = _comp.GetSpecialType(SpecialType.System_String);

                        for (int i =  0; i < actualElementNames.Length; i++)
                        {
                            string expectedName = expectedElementNames[i];
                            TypedConstant actualName = actualElementNames[i];

                            Assert.Equal(TypedConstantKind.Primitive, actualName.Kind);
                            Assert.Equal(stringType, actualName.Type);
                            Assert.Equal(expectedName, (string)actualName.Value);
                        }
                    }
                }
            }
        }

        [Fact]
        public void TupleAttributeMissing()
        {
            var comp = CreateCompilationWithMscorlib(
                s_tuplesTestSource + TestResources.NetFX.ValueTuple.tuplelib_cs,
                references: new[] { SystemCoreRef },
                options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (8,31): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                // public class Outer<T> : Base1<(int key, int val)>
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int key, int val)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(8, 31),
                // (16,42): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                // public class Derived<T> : Outer<(int e1, (int e2, int e3) e4)>.Inner<
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e2, int e3)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(16, 42),
                // (16,33): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                // public class Derived<T> : Outer<(int e1, (int e2, int e3) e4)>.Inner<
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, (int e2, int e3) e4)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(16, 33),
                // (17,11): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     Outer<(int e5, int e6)>.Inner<(int e7, int e8)[], (int e9, int e10)>.InnerInner<int>[],
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e5, int e6)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(17, 11),
                // (17,35): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     Outer<(int e5, int e6)>.Inner<(int e7, int e8)[], (int e9, int e10)>.InnerInner<int>[],
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e7, int e8)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(17, 35),
                // (17,55): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     Outer<(int e5, int e6)>.Inner<(int e7, int e8)[], (int e9, int e10)>.InnerInner<int>[],
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e9, int e10)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(17, 55),
                // (18,6): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     ((int e11, int e12) e13, int e14)>
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e11, int e12)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(18, 6),
                // (18,5): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     ((int e11, int e12) e13, int e14)>
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "((int e11, int e12) e13, int e14)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(18, 5),
                // (19,18): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     .InnerInner<((int e15, int e16) e17, (int e18, Base1<(int e19, int e20)> e21) e22)>
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e15, int e16)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(19, 18),
                // (19,58): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     .InnerInner<((int e15, int e16) e17, (int e18, Base1<(int e19, int e20)> e21) e22)>
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e19, int e20)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(19, 58),
                // (19,42): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     .InnerInner<((int e15, int e16) e17, (int e18, Base1<(int e19, int e20)> e21) e22)>
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e18, Base1<(int e19, int e20)> e21)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(19, 42),
                // (19,17): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     .InnerInner<((int e15, int e16) e17, (int e18, Base1<(int e19, int e20)> e21) e22)>
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "((int e15, int e16) e17, (int e18, Base1<(int e19, int e20)> e21) e22)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(19, 17),
                // (10,38): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public class Inner<U, V> : Base2<(int key2, int val2), V>
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int key2, int val2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(10, 38),
                // (12,44): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //         public class InnerInner<W> : Base1<(int key3, int val3)> { }
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int key3, int val3)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(12, 44),
                // (72,47): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //                                    ValueTuple<(dynamic e2, dynamic e3)> e4)> Event1
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(dynamic e2, dynamic e3)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(72, 47),
                // (71,35): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static event Delegate1<(dynamic e1,
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, @"(dynamic e1,
                                   ValueTuple<(dynamic e2, dynamic e3)> e4)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(71, 35),
                // (41,19): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static (int e1, int e2) Method1() => (0, 0);
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, int e2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(41, 19),
                // (42,32): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static void Method2((int e1, int e2) x) { }
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, int e2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(42, 32),
                // (43,44): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static (int e1, int e2) Method3((int e3, int e4) x) => (0, 0);
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e3, int e4)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(43, 44),
                // (43,19): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static (int e1, int e2) Method3((int e3, int e4) x) => (0, 0);
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, int e2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(43, 19),
                // (44,48): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static (int e1, int e2) Method4(ref (int e3, int e4) x) => x;
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e3, int e4)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(44, 48),
                // (44,19): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static (int e1, int e2) Method4(ref (int e3, int e4) x) => x;
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, int e2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(44, 19),
                // (53,19): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static (int e1, int e2, int e3, int e4, int e5,
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, @"(int e1, int e2, int e3, int e4, int e5,
                   int e6, int e7, int e8, int e9)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(53, 19),
                // (57,19): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static (int e1, int e2) Prop1 => (0, 0);
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, int e2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(57, 19),
                // (58,19): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static (int e1, int e2) Prop2 { get; set; }
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, int e2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(58, 19),
                // (60,34): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public (int e1, int e2) this[(int e3, int e4) param]
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e3, int e4)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(60, 34),
                // (60,12): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public (int e1, int e2) this[(int e3, int e4) param]
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, int e2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(60, 12),
                // (68,14): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //             ((dynamic e1, dynamic e2, object e3) e4, dynamic e5),
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(dynamic e1, dynamic e2, object e3)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(68, 14),
                // (68,13): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //             ((dynamic e1, dynamic e2, object e3) e4, dynamic e5),
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "((dynamic e1, dynamic e2, object e3) e4, dynamic e5)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(68, 13),
                // (22,19): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static (int e1, int e2) Field2;
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, int e2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(22, 19),
                // (23,34): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static Base1<(int e1, (int e2, int e3) e4)> Field3;
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e2, int e3)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(23, 34),
                // (23,25): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static Base1<(int e1, (int e2, int e3) e4)> Field3;
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, (int e2, int e3) e4)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(23, 25),
                // (25,36): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static ValueTuple<Base1<(int e1, (int, (dynamic, dynamic)) e2)>, int> Field4;
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, (int, (dynamic, dynamic)) e2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(25, 36),
                // (27,25): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static Outer<(object e1, dynamic e2)>
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(object e1, dynamic e2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(27, 25),
                // (28,16): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //         .Inner<(dynamic e3, object e4),
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(dynamic e3, object e4)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(28, 16),
                // (36,19): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static (int e1, int e2, int e3, int e4, int e5,
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, @"(int e1, int e2, int e3, int e4, int e5,
                   int e6, int e7, int e8, int e9)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(36, 19),
                // (38,25): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static Base1<(int e1, int e2, int e3, int e4, int e5,
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, @"(int e1, int e2, int e3, int e4, int e5,
                         int e6, int e7, int e8, int e9)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(38, 25),
                // (21,19): error CS8137: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     public static (int e1, int e2) Field1;
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int e1, int e2)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(21, 19));
        }

        [Fact]
        public void ExplicitTupleNamesAttribute()
        {
            var text = @"
using System;
using System.Runtime.CompilerServices;

[TupleElementNames(new[] { ""a"", ""b"" })]
public class C
{
    [TupleElementNames(new string[] { null, null })]
    public ValueTuple<int, int> Field1;

    [TupleElementNames(new[] { ""x"", ""y"" })]
    public ValueTuple<int, int> Prop1;

    [return: TupleElementNames(new string[] { null, null })]
    public ValueTuple<int, int> M([TupleElementNames(new string[] { null})] ValueTuple x) => (0, 0);

    public delegate void Delegate1<T>(object sender,
        [TupleElementNames(new[] { ""x"" })]ValueTuple<T> args);

    [TupleElementNames(new[] { ""y"" })]
    public event Delegate1<ValueTuple<int>> Event1
    {
        add { }
        remove { }
    }

    [TupleElementNames(new[] { ""a"", ""b"" })]
    public (int x, int y) this[[TupleElementNames](int a, int b) t] => t;
}

[TupleElementNames(new[] { ""a"", ""b"" })]
public struct S
{
}";
            var comp = CreateCompilationWithMscorlib(text, references: s_attributeRefs);
            comp.VerifyDiagnostics(
                // (31,2): error CS8208: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
                // [TupleElementNames(new[] { "a", "b" })]
                Diagnostic(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, @"TupleElementNames(new[] { ""a"", ""b"" })").WithLocation(31, 2),
                // (5,2): error CS8208: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
                // [TupleElementNames(new[] { "a", "b" })]
                Diagnostic(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, @"TupleElementNames(new[] { ""a"", ""b"" })").WithLocation(5, 2),
                // (18,10): error CS8208: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
                //         [TupleElementNames(new[] { "x" })]ValueTuple<T> args);
                Diagnostic(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, @"TupleElementNames(new[] { ""x"" })").WithLocation(18, 10),
                // (11,6): error CS8208: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
                //     [TupleElementNames(new[] { "x", "y" })]
                Diagnostic(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, @"TupleElementNames(new[] { ""x"", ""y"" })").WithLocation(11, 6),
                // (14,14): error CS8208: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
                //     [return: TupleElementNames(new string[] { null, null })]
                Diagnostic(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, "TupleElementNames(new string[] { null, null })").WithLocation(14, 14),
                // (15,36): error CS8208: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
                //     public ValueTuple<int, int> M([TupleElementNames(new string[] { null})] ValueTuple x) => (0, 0);
                Diagnostic(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, "TupleElementNames(new string[] { null})").WithLocation(15, 36),
                // (20,6): error CS0592: Attribute 'TupleElementNames' is not valid on this declaration type. It is only valid on 'class, struct, property, indexer, field, parameter, return' declarations.
                //     [TupleElementNames(new[] { "y" })]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "TupleElementNames").WithArguments("TupleElementNames", "class, struct, property, indexer, field, parameter, return").WithLocation(20, 6),
                // (27,6): error CS8208: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
                //     [TupleElementNames(new[] { "a", "b" })]
                Diagnostic(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, @"TupleElementNames(new[] { ""a"", ""b"" })").WithLocation(27, 6),
                // (28,33): error CS8208: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
                //     public (int x, int y) this[[TupleElementNames](int a, int b) t] => t;
                Diagnostic(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, "TupleElementNames").WithLocation(28, 33),
                // (8,6): error CS8208: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
                //     [TupleElementNames(new string[] { null, null })]
                Diagnostic(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, "TupleElementNames(new string[] { null, null })").WithLocation(8, 6));
        }
    }
}
