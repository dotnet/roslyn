// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_Nullable : CSharpTestBase
    {
        // An empty project should not require System.Attribute.
        [Fact]
        public void EmptyProject_MissingAttribute()
        {
            var source = "";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1));
        }

        [Fact]
        public void ExplicitAttributeFromSource()
        {
            var source =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullableAttribute : Attribute
    {
        public NullableAttribute() { }
        public NullableAttribute(bool[] b) { }
    }
}
class C
{
    static void F(object? x, object?[] y) { }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ExplicitAttributeFromMetadata()
        {
            var source0 =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullableAttribute : Attribute
    {
        public NullableAttribute() { }
        public NullableAttribute(bool[] b) { }
    }
}";
            var comp0 = CreateStandardCompilation(source0, parseOptions: TestOptions.Regular7);
            var ref0 = comp0.EmitToImageReference();

            var source =
@"class C
{
    static void F(object? x, object?[] y) { }
}";
            var comp = CreateStandardCompilation(source, references: new[] { ref0 }, parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ExplicitAttribute_MissingParameterlessConstructor()
        {
            var source =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullableAttribute : Attribute
    {
        public NullableAttribute(bool[] b) { }
    }
}
class C
{
    static void F(object? x, object?[] y) { }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // (10,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     static void F(object? x, object?[] y) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object? x").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 19),
                // (10,30): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     static void F(object? x, object?[] y) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?[] y").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 30));
        }

        // PROTOTYPE(NullableReferenceTypes): Handle missing constructor.
        [Fact(Skip = "Handle missing constructor")]
        public void ExplicitAttribute_MissingConstructor()
        {
            var source =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullableAttribute : Attribute
    {
        public NullableAttribute() { }
    }
}
class C
{
    static void F(object? x, object?[] y) { }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // (10,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     static void F(object? x, object?[] y) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object? x").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 19),
                // (10,30): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     static void F(object? x, object?[] y) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?[] y").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 30));
        }

        [Fact]
        public void NullableAttribute_MissingBoolean()
        {
            var source0 =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public class Attribute
    {
    }
}";
            var comp0 = CreateCompilation(source0, parseOptions: TestOptions.Regular7);
            var ref0 = comp0.EmitToImageReference();

            var source =
@"class C
{
    object? F() => null;
}";
            var comp = CreateCompilation(
                source,
                references: new[] { ref0 },
                parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // error CS0518: Predefined type 'System.Boolean' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Boolean").WithLocation(1, 1));
        }

        [Fact]
        public void NullableAttribute_MissingAttribute()
        {
            var source0 =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
}";
            var comp0 = CreateCompilation(source0, parseOptions: TestOptions.Regular7);
            var ref0 = comp0.EmitToImageReference();

            var source =
@"class C
{
    object? F() => null;
}";
            var comp = CreateCompilation(
                source,
                references: new[] { ref0 },
                parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1));
        }

        [Fact]
        public void NullableAttribute_StaticAttributeConstructorOnly()
        {
            var source0 =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public class Attribute
    {
        static Attribute() { }
        public Attribute(object o) { }
    }
}";
            var comp0 = CreateCompilation(source0, parseOptions: TestOptions.Regular7);
            var ref0 = comp0.EmitToImageReference();

            var source =
@"class C
{
    object? F() => null;
}";
            var comp = CreateCompilation(
                source,
                references: new[] { ref0 },
                parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1));
        }

        [Fact]
        public void EmitAttribute_NoNullable()
        {
            var source =
@"public class C
{
    public object F = new object();
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var assembly = module.ContainingAssembly;
                var type = assembly.GetTypeByMetadataName("C");
                var field = (FieldSymbol)type.GetMembers("F").Single();
                AssertNoNullableAttribute(field.GetAttributes());
                AssertNoNullableAttribute(module.GetAttributes());
                AssertAttributes(assembly.GetAttributes(),
                    "System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                    "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
                    "System.Diagnostics.DebuggableAttribute");
            });
        }

        // PROTOTYPE(NullableReferenceTypes): Should generate [module: Nullable].
        [Fact(Skip = "TODO")]
        public void EmitAttribute_Module()
        {
            var source =
@"public class C
{
    public object? F = new object();
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var assembly = module.ContainingAssembly;
                var type = assembly.GetTypeByMetadataName("C");
                var field = (FieldSymbol)type.GetMembers("F").Single();
                AssertNullableAttribute(field.GetAttributes());
                AssertNullableAttribute(module.GetAttributes());
                AssertAttributes(assembly.GetAttributes(),
                    "System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                    "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
                    "System.Diagnostics.DebuggableAttribute");
            });
        }

        [Fact]
        public void EmitAttribute_BaseClass()
        {
            var source =
@"class A<T>
{
}
class B1 : A<object>
{
}
class B2 : A<object?>
{
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("A`1");
                AssertNoNullableAttribute(type.GetAttributes());
                type = module.ContainingAssembly.GetTypeByMetadataName("B1");
                AssertNoNullableAttribute(type.BaseType().GetAttributes());
                AssertNoNullableAttribute(type.GetAttributes());
                type = module.ContainingAssembly.GetTypeByMetadataName("B2");
                AssertNoNullableAttribute(type.BaseType().GetAttributes());
                AssertNullableAttribute(type.GetAttributes());
            });
        }

        // PROTOTYPE(NullableReferenceTypes): Synthesize [Nullable] for interface
        // implementations. See TypeSymbolExtensions.GetTypeRefWithAttributes.
        [Fact(Skip = "TODO")]
        public void EmitAttribute_Interface()
        {
            var source =
@"public interface I<T>
{
}
public class A : I<(object X, object Y)>
{
}
public class B : I<(object X, object? Y)>
{
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, references: new[] { SystemRuntimeFacadeRef, ValueTupleRef });
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("I`1");
                AssertNoNullableAttribute(type.GetAttributes());
                type = module.ContainingAssembly.GetTypeByMetadataName("A");
                AssertNoNullableAttribute(type.Interfaces().Single().GetAttributes());
                AssertNoNullableAttribute(type.GetAttributes());
                type = module.ContainingAssembly.GetTypeByMetadataName("B");
                AssertNoNullableAttribute(type.Interfaces().Single().GetAttributes());
                // No [Nullable] or [TupleElementNames] attributes on 'B' but
                // there should be attributes on the interfaceimpl.
                AssertNoNullableAttribute(type.GetAttributes());
            });
            AssertAttribute(comp, "NullableAttribute");
            AssertAttribute(comp, "TupleElementNamesAttribute");
            var source2 =
@"class C
{
    static void F(I<object> x, I<object?> y)
    {
    }
    static void G(A a, B b)
    {
        F(a, a);
        F(b, b);
    }
}";
            var comp2 = CreateStandardCompilation(source2, parseOptions: TestOptions.Regular8, references: new[] { comp.EmitToImageReference() });
            // PROTOTYPE(NullableReferenceTypes): Should report warnings.
            comp2.VerifyEmitDiagnostics();
        }

        [Fact]
        public void EmitAttribute_MethodReturnType()
        {
            var source =
@"public class C
{
    public object? F() => null;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                var method = (MethodSymbol)type.GetMembers("F").Single();
                AssertNullableAttribute(method.GetReturnTypeAttributes());
            });
        }

        [Fact]
        public void EmitAttribute_MethodParameters()
        {
            var source =
@"public class C
{
    public void F(object?[] c) { }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                var method = (MethodSymbol)type.GetMembers("F").Single();
                AssertNullableAttribute(method.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void EmitAttribute_ConstructorParameters()
        {
            var source =
@"public class C
{
    public C(object?[] c) { }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                var method = type.Constructors.Single();
                AssertNullableAttribute(method.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void EmitAttribute_PropertyType()
        {
            var source =
@"public class C
{
    public object? P => null;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                var property = (PropertySymbol)type.GetMembers("P").Single();
                AssertNullableAttribute(property.GetAttributes());
            });
        }

        [Fact]
        public void EmitAttribute_PropertyParameters()
        {
            var source =
@"public class C
{
    public object this[object x, object? y] => throw new System.NotImplementedException();
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                var property = (PropertySymbol)type.GetMembers("this[]").Single();
                AssertNoNullableAttribute(property.Parameters[0].GetAttributes());
                AssertNullableAttribute(property.Parameters[1].GetAttributes());
            });
        }

        [Fact]
        public void EmitAttribute_OperatorReturnType()
        {
            var source =
@"public class C
{
    public static object? operator+(C a, C b) => null;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                var method = (MethodSymbol)type.GetMembers("op_Addition").Single();
                AssertNullableAttribute(method.GetReturnTypeAttributes());
                AssertNoNullableAttribute(method.GetAttributes());
            });
        }

        [Fact]
        public void EmitAttribute_OperatorParameters()
        {
            var source =
@"public class C
{
    public static object operator+(C a, object?[] b) => a;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                var method = (MethodSymbol)type.GetMembers("op_Addition").Single();
                AssertNoNullableAttribute(method.Parameters[0].GetAttributes());
                AssertNullableAttribute(method.Parameters[1].GetAttributes());
            });
        }

        [Fact]
        public void EmitAttribute_DelegateReturnType()
        {
            var source =
@"public delegate object? D();";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod;
                AssertNullableAttribute(method.GetReturnTypeAttributes());
                AssertNoNullableAttribute(method.GetAttributes());
            });
        }

        [Fact]
        public void EmitAttribute_DelegateParameters()
        {
            var source =
@"public delegate void D(object?[] o);";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod;
                AssertNullableAttribute(method.Parameters[0].GetAttributes());
            });
        }

        [Fact]
        public void EmitAttribute_LambdaReturnType()
        {
            var source =
@"delegate T D<T>();
class C
{
    static void F<T>(D<T> d)
    {
    }
    static void G(object o)
    {
        F(() =>
        {
            if (o != new object()) return o;
            return null;
        });
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Use AssertNullableAttribute(method.GetReturnTypeAttributes()). 
            AssertAttribute(comp);
        }

        [Fact]
        public void EmitAttribute_LambdaParameters()
        {
            var source =
@"delegate void D<T>(T t);
class C
{
    static void F<T>(D<T> d)
    {
    }
    static void G()
    {
        F((object? o) => { });
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Use AssertNullableAttribute(method.Parameters[0].GetAttributes()). 
            AssertAttribute(comp);
        }

        [Fact]
        public void EmitAttribute_LocalFunctionReturnType()
        {
            var source =
@"class C
{
    static void M()
    {
        object?[] L() => throw new System.NotImplementedException();
        L();
    }
}";
            CompileAndVerify(
                source,
                parseOptions: TestOptions.Regular8,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var method = module.ContainingAssembly.GetTypeByMetadataName("C").GetMethod("<M>g__L|0_0");
                    AssertNullableAttribute(method.GetReturnTypeAttributes());
                    AssertAttributes(method.GetAttributes(), "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
                });
        }

        [Fact]
        public void EmitAttribute_LocalFunctionParameters()
        {
            var source =
@"class C
{
    static void M()
    {
        void L(object? x, object y) { }
        L(null, 2);
    }
}";
            CompileAndVerify(
                source,
                parseOptions: TestOptions.Regular8,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var method = module.ContainingAssembly.GetTypeByMetadataName("C").GetMethod("<M>g__L|0_0");
                    AssertNullableAttribute(method.Parameters[0].GetAttributes());
                    AssertNoNullableAttribute(method.Parameters[1].GetAttributes());
                });
        }

        [Fact]
        public void EmitAttribute_ExplicitImplementationForwardingMethod()
        {
            var source0 =
@"public class A
{
    public object? F() => null;
}";
            var comp0 = CreateStandardCompilation(source0, parseOptions: TestOptions.Regular8);
            var ref0 = comp0.EmitToImageReference();
            var source =
@"interface I
{
    object? F();
}
class B : A, I
{
}";
            CompileAndVerify(
                source,
                additionalRefs: new[] { ref0 },
                parseOptions: TestOptions.Regular8,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var method = module.ContainingAssembly.GetTypeByMetadataName("B").GetMethod("I.F");
                    AssertNullableAttribute(method.GetReturnTypeAttributes());
                    AssertNoNullableAttribute(method.GetAttributes());
                });
        }

        [Fact]
        public void EmitAttribute_Iterator_01()
        {
            var source =
@"using System.Collections.Generic;
class C
{
    static IEnumerable<object?> F()
    {
        yield break;
    }
}";
            CompileAndVerify(
                source,
                parseOptions: TestOptions.Regular8,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var property = module.ContainingAssembly.GetTypeByMetadataName("C").GetTypeMember("<F>d__0").GetProperty("System.Collections.Generic.IEnumerator<System.Object>.Current");
                    AssertNoNullableAttribute(property.GetAttributes());
                    var method = property.GetMethod;
                    // PROTOTYPE(NullableReferenceTypes): No synthesized attributes for this
                    // case which is inconsisten with IEnumerable<object?[]> in test below.
                    AssertNoNullableAttribute(method.GetReturnTypeAttributes());
                    AssertAttributes(method.GetAttributes(), "System.Diagnostics.DebuggerHiddenAttribute");
                });
        }

        [Fact]
        public void EmitAttribute_Iterator_02()
        {
            var source =
@"using System.Collections.Generic;
class C
{
    static IEnumerable<object?[]> F()
    {
        yield break;
    }
}";
            CompileAndVerify(
                source,
                parseOptions: TestOptions.Regular8,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var property = module.ContainingAssembly.GetTypeByMetadataName("C").GetTypeMember("<F>d__0").GetProperty("System.Collections.Generic.IEnumerator<System.Object[]>.Current");
                    AssertNoNullableAttribute(property.GetAttributes());
                    var method = property.GetMethod;
                    AssertNullableAttribute(method.GetReturnTypeAttributes());
                    AssertAttributes(method.GetAttributes(), "System.Diagnostics.DebuggerHiddenAttribute");
                });
        }

        [Fact]
        public void UseSiteError_LambdaReturnType()
        {
            var source0 =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct IntPtr { }
    public class MulticastDelegate { }
}";
            var comp0 = CreateCompilation(source0);
            var ref0 = comp0.EmitToImageReference();

            var source =
@"delegate T D<T>();
class C
{
    static void F<T>(D<T> d)
    {
    }
    static void G(object o)
    {
        F(() =>
        {
            if (o != new object()) return o;
            return null;
        });
    }
}";
            var comp = CreateCompilation(
                source,
                references: new[] { ref0 },
                parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1));
        }

        [Fact]
        public void ModuleMissingAttribute_BaseClass()
        {
            var source =
@"class A<T>
{
}
class B : A<object?>
{
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (4,7): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                // class B : A<object?>
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(4, 7));
        }

        // PROTOTYPE(NullableReferenceTypes): Synthesize [Nullable] for interface
        // implementations. See TypeSymbolExtensions.GetTypeRefWithAttributes.
        [Fact(Skip = "TODO")]
        public void ModuleMissingAttribute_Interface()
        {
            var source =
@"interface I<T>
{
}
class C : I<(object X, object? Y)>
{
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule, references: new[] { SystemRuntimeFacadeRef, ValueTupleRef });
            comp.VerifyEmitDiagnostics(
                // (4,7): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                // class C : I<(object X, object? Y)>
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(4, 7));
        }

        [Fact]
        public void ModuleMissingAttribute_MethodReturnType()
        {
            var source =
@"class C
{
    object? F() => null;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     object? F() => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 5));
        }

        [Fact]
        public void ModuleMissingAttribute_MethodParameters()
        {
            var source =
@"class C
{
    void F(object?[] c) { }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     void F(object?[] c) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?[] c").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 12));
        }

        [Fact]
        public void ModuleMissingAttribute_ConstructorParameters()
        {
            var source =
@"class C
{
    C(object?[] c) { }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (3,7): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     C(object?[] c) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?[] c").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 7));
        }

        [Fact]
        public void ModuleMissingAttribute_PropertyType()
        {
            var source =
@"class C
{
    object? P => null;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     object? P => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 5));
        }

        [Fact]
        public void ModuleMissingAttribute_PropertyParameters()
        {
            var source =
@"class C
{
    object this[object x, object? y] => throw new System.NotImplementedException();
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (3,27): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     object this[object x, object? y] => throw new System.NotImplementedException();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object? y").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 27));
        }

        [Fact]
        public void ModuleMissingAttribute_OperatorReturnType()
        {
            var source =
@"class C
{
    public static object? operator+(C a, C b) => null;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (3,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     public static object? operator+(C a, C b) => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 19));
        }

        [Fact]
        public void ModuleMissingAttribute_OperatorParameters()
        {
            var source =
@"class C
{
    public static object operator+(C a, object?[] b) => a;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (3,41): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     public static object operator+(C a, object?[] b) => a;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?[] b").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 41));
        }

        [Fact]
        public void ModuleMissingAttribute_DelegateReturnType()
        {
            var source =
@"delegate object? D();";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (1,10): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                // delegate object? D();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(1, 10));
        }

        [Fact]
        public void ModuleMissingAttribute_DelegateParameters()
        {
            var source =
@"delegate void D(object?[] o);";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (1,17): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                // delegate void D(object?[] o);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?[] o").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(1, 17));
        }

        [Fact]
        public void ModuleMissingAttribute_LambdaReturnType()
        {
            var source =
@"delegate T D<T>();
class C
{
    static void F<T>(D<T> d)
    {
    }
    static void G(object o)
    {
        F(() =>
        {
            if (o != new object()) return o;
            return null;
        });
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (9,14): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //         F(() =>
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "=>").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(9, 14));
        }

        [Fact]
        public void ModuleMissingAttribute_LambdaParameters()
        {
            var source =
@"delegate void D<T>(T t);
class C
{
    static void F<T>(D<T> d)
    {
    }
    static void G()
    {
        F((object? o) => { });
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (9,12): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //         F((object? o) => { });
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object? o").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(9, 12));
        }

        [Fact]
        public void ModuleMissingAttribute_LocalFunctionReturnType()
        {
            var source =
@"class C
{
    static void M()
    {
        object?[] L() => throw new System.NotImplementedException();
        L();
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (5,9): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //         object?[] L() => throw new System.NotImplementedException();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?[]").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(5, 9));
        }

        [Fact]
        public void ModuleMissingAttribute_LocalFunctionParameters()
        {
            var source =
@"class C
{
    static void M()
    {
        void L(object? x, object y) { }
        L(null, 2);
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            comp.VerifyEmitDiagnostics(
                // (5,16): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //         void L(object? x, object y) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object? x").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(5, 16));
        }

        private static void AssertNoNullableAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            AssertAttributes(attributes);
        }

        private static void AssertNullableAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            AssertAttributes(attributes, "System.Runtime.CompilerServices.NullableAttribute");
        }

        private static void AssertAttributes(ImmutableArray<CSharpAttributeData> attributes, params string[] expectedNames)
        {
            var actualNames = attributes.Select(a => a.AttributeClass.ToTestDisplayString()).ToArray();
            AssertEx.SetEqual(actualNames, expectedNames);
        }

        private static void AssertAttribute(CSharpCompilation comp, string attributeName = "NullableAttribute")
        {
            var image = comp.EmitToArray();
            using (var reader = new PEReader(image))
            {
                var metadataReader = reader.GetMetadataReader();
                var attributes = metadataReader.GetCustomAttributeRows().Select(metadataReader.GetCustomAttributeName).ToArray();
                Assert.True(attributes.Contains(attributeName));
            }
        }
    }
}
