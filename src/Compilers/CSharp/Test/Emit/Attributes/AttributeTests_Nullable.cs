// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
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
            comp0.VerifyDiagnostics();
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
            comp0.VerifyDiagnostics();
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
            comp0.VerifyDiagnostics();
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
                AssertNoNullableAttribute(type.BaseType.GetAttributes());
                AssertNoNullableAttribute(type.GetAttributes());
                type = module.ContainingAssembly.GetTypeByMetadataName("B2");
                AssertNoNullableAttribute(type.BaseType.GetAttributes());
                AssertNullableAttribute(type.GetAttributes());
            });
        }

        // PROTOTYPE(NullableReferenceTypes: Synthesize [Nullable] for interface
        // implementations. See TypeSymbolExtensions.GetTypeRefWithAttributes.
        [Fact(Skip = "TODO")]
        public void EmitAttribute_Interface()
        {
            var source =
@"public interface I<T>
{
}
public class A : I<object>
{
}
public class B : I<object?>
{
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("I`1");
                AssertNoNullableAttribute(type.GetAttributes());
                type = module.ContainingAssembly.GetTypeByMetadataName("A");
                AssertNoNullableAttribute(type.Interfaces.Single().GetAttributes());
                AssertNoNullableAttribute(type.GetAttributes());
                type = module.ContainingAssembly.GetTypeByMetadataName("B");
                AssertNoNullableAttribute(type.Interfaces.Single().GetAttributes());
                // No [Nullable] attribute on 'B' but there should be a [Nullable]
                // attribute on the interfaceimpl.
                AssertNoNullableAttribute(type.GetAttributes());
            });
            var image = comp.EmitToArray();
            using (var reader = new PEReader(image))
            {
                var metadataReader = reader.GetMetadataReader();
                var attributes = metadataReader.GetCustomAttributeRows().Select(metadataReader.GetCustomAttributeName).ToArray();
                Assert.True(attributes.Contains("NullableAttribute"));
            }
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
            // PROTOTYPE(NullableReferenceTypes: Should report warnings.
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
        public void EmitAttribute_OperatorType()
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
                AssertNullableAttribute(method.Parameters[1].GetAttributes());
            });
        }

        [Fact]
        public void EmitAttribute_PropertyParameters()
        {
            var source =
@"public class C
{
    public object this[object x, object? y] => null;
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                var property = (PropertySymbol)type.GetMembers("this[]").Single();
                AssertNullableAttribute(property.Parameters[1].GetAttributes());
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

        private static void AssertNoNullableAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            Assert.Equal(0, attributes.Length);
        }

        private static void AssertNullableAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            var attributeType = attributes.Single().AttributeClass;
            Assert.Equal("System.Runtime.CompilerServices.NullableAttribute", attributeType.ToTestDisplayString());
        }
    }
}
