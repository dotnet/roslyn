// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
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
            var comp = CreateEmptyCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
                );
        }

        [Fact]
        public void ExplicitAttributeFromSource()
        {
            var source =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullableAttribute : Attribute
    {
        public NullableAttribute(byte a) { }
        public NullableAttribute(byte[] b) { }
    }
}
class C
{
    static void F(object? x, object?[] y) { }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
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
        public NullableAttribute(byte a) { }
        public NullableAttribute(byte[] b) { }
    }
}";
            var comp0 = CreateCompilation(source0, parseOptions: TestOptions.Regular7);
            var ref0 = comp0.EmitToImageReference();

            var source =
@"class C
{
    static void F(object? x, object?[] y) { }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), references: new[] { ref0 }, parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ExplicitAttribute_MissingSingleByteConstructor()
        {
            var source =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullableAttribute : Attribute
    {
        public NullableAttribute(byte[] b) { }
    }
}
class C
{
    static void F(object? x, object?[] y) { }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // (5,34): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //         public NullableAttribute(byte[] b) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "byte[] b").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(5, 34),
                // (10,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     static void F(object? x, object?[] y) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object? x").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 19),
                // (10,30): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     static void F(object? x, object?[] y) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?[] y").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 30));
        }

        [Fact]
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
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // (10,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     static void F(object? x, object?[] y) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object? x").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 19),
                // (10,30): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     static void F(object? x, object?[] y) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?[] y").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 30));
        }

        [Fact]
        public void ExplicitAttribute_MissingBothNeededConstructors()
        {
            var source =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullableAttribute : Attribute
    {
        public NullableAttribute(string[] b) { }
    }
}
class C
{
    static void F(object? x, object?[] y) { }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // (5,34): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //         public NullableAttribute(string[] b) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "string[] b").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(5, 34),
                // (10,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     static void F(object? x, object?[] y) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object? x").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 19),
                // (10,30): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     static void F(object? x, object?[] y) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?[] y").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 30));
        }

        [Fact]
        public void AttributeFromInternalsVisibleTo_01()
        {
            var sourceA =
@"using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""B"")]
#nullable enable
class A
{
    object? F = null;
}";
            var options = TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All);
            var comp = CreateCompilation(sourceA, assemblyName: "A", options: options);
            CompileAndVerify(comp, symbolValidator: m => CheckAttribute(m.GlobalNamespace.GetMember("A.F").GetAttributes().Single(), "A"));
            var refA = comp.EmitToImageReference();

            var sourceB =
@"#nullable enable
class B
{
    object? G = new A();
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, assemblyName: "B", options: options);
            CompileAndVerify(comp, symbolValidator: m => CheckAttribute(m.GlobalNamespace.GetMember("B.G").GetAttributes().Single(), "B"));
        }

        [Fact]
        public void AttributeFromInternalsVisibleTo_02()
        {
            var sourceAttribute =
@"namespace System.Runtime.CompilerServices
{
    internal sealed class NullableAttribute : Attribute
    {
        public NullableAttribute(byte b) { }
        public NullableAttribute(byte[] b) { }
    }
}";
            var sourceA =
@"using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""B"")]
#nullable enable
class A
{
    object? F = null;
}";
            var options = TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All);
            var comp = CreateCompilation(new[] { sourceAttribute, sourceA }, assemblyName: "A", options: options);
            CompileAndVerify(comp, symbolValidator: m => CheckAttribute(m.GlobalNamespace.GetMember("A.F").GetAttributes().Single(), "A"));
            var refA = comp.EmitToImageReference();

            var sourceB =
@"#nullable enable
class B
{
    object? G = new A();
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, assemblyName: "B", options: options);
            CompileAndVerify(comp, symbolValidator: m => CheckAttribute(m.GlobalNamespace.GetMember("B.G").GetAttributes().Single(), "A"));
        }

        private static void CheckAttribute(CSharpAttributeData attribute, string assemblyName)
        {
            var attributeType = attribute.AttributeClass;
            Assert.Equal("System.Runtime.CompilerServices", attributeType.ContainingNamespace.QualifiedName);
            Assert.Equal("NullableAttribute", attributeType.Name);
            Assert.Equal(assemblyName, attributeType.ContainingAssembly.Name);
        }

        [Fact]
        public void NullableAttribute_MissingByte()
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
            var comp0 = CreateEmptyCompilation(source0, parseOptions: TestOptions.Regular7);
            var ref0 = comp0.EmitToImageReference();

            var source =
@"class C
{
    object? F() => null;
}";
            var comp = CreateEmptyCompilation(
                source,
                references: new[] { ref0 },
                parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // (3,11): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     object? F() => null;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(3, 11),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Byte' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Byte").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Byte' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Byte").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Int32' is not defined or imported
                // 
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "").WithArguments("System.Int32").WithLocation(1, 1));
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
    public struct Byte { }
}";
            var comp0 = CreateEmptyCompilation(source0, parseOptions: TestOptions.Regular7);
            var ref0 = comp0.EmitToImageReference();

            var source =
@"class C
{
    object? F() => null;
}";
            var comp = CreateEmptyCompilation(
                source,
                references: new[] { ref0 },
                parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // (3,11): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     object? F() => null;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(3, 11),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1));
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
    public struct Byte { }
    public class Attribute
    {
        static Attribute() { }
        public Attribute(object o) { }
    }
}";
            var comp0 = CreateEmptyCompilation(source0, parseOptions: TestOptions.Regular7);
            var ref0 = comp0.EmitToImageReference();

            var source =
@"class C
{
    object? F() => null;
}";
            var comp = CreateEmptyCompilation(
                source,
                references: new[] { ref0 },
                parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics(
                // (3,11): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     object? F() => null;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(3, 11),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1));
        }

        [Fact]
        public void MissingAttributeUsageAttribute()
        {
            var source =
@"#nullable enable
class Program
{
    object? F() => null;
}";

            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_AttributeUsageAttribute);
            comp.VerifyEmitDiagnostics(
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_AttributeUsageAttribute__ctor);
            comp.VerifyEmitDiagnostics(
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_AttributeUsageAttribute__AllowMultiple);
            comp.VerifyEmitDiagnostics(
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_AttributeUsageAttribute__Inherited);
            comp.VerifyEmitDiagnostics(
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1));
        }

        [Fact]
        public void EmitAttribute_NoNullable()
        {
            var source =
@"public class C
{
    public object F = new object();
}";
            // C# 7.0: No NullableAttribute.
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
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
            // C# 8.0: NullableAttribute not included if no ? annotation.
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
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

        [Fact]
        public void EmitAttribute_LocalFunctionConstraints()
        {
            var source = @"
class C
{
#nullable enable
    void M1()
    {
        local(new C());
        void local<T>(T t) where T : C?
        {
        }
    }
}";
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var assembly = module.ContainingAssembly;
                Assert.NotNull(assembly.GetTypeByMetadataName("System.Runtime.CompilerServices.NullableAttribute"));
            });
        }

        [Fact]
        public void EmitAttribute_OnlyAnnotationsEnabled_LocalFunctionConstraints()
        {
            var source = @"
class C
{
#nullable enable annotations
    void M1()
    {
        local(new C());
        void local<T>(T t) where T : C?
        {
        }
    }
}";
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var assembly = module.ContainingAssembly;
                Assert.NotNull(assembly.GetTypeByMetadataName("System.Runtime.CompilerServices.NullableAttribute"));
            });
        }

        [Fact]
        public void EmitAttribute_NullableEnabledInProject_LocalFunctionConstraints()
        {
            var source = @"
class C
{
    void M1()
    {
        local(new C());
        void local<T>(T t) where T : C?
        {
        }
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypes(NullableContextOptions.Enable), parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var assembly = module.ContainingAssembly;
                Assert.NotNull(assembly.GetTypeByMetadataName("System.Runtime.CompilerServices.NullableAttribute"));
            });
        }

        [Fact]
        public void EmitAttribute_OnlyAnnotationsEnabledInProject_LocalFunctionConstraints()
        {
            var source = @"
class C
{
    void M1()
    {
        local(new C());
        void local<T>(T t) where T : C?
        {
        }
    }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypes(NullableContextOptions.Annotations), parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var assembly = module.ContainingAssembly;
                Assert.NotNull(assembly.GetTypeByMetadataName("System.Runtime.CompilerServices.NullableAttribute"));
            });
        }

        [Fact]
        public void EmitAttribute_LocalFunctionConstraints_Nested()
        {
            var source = @"
interface I<T> { }
class C
{
#nullable enable
    void M1()
    {
        void local<T>(T t) where T : I<C?>
        {
        }
    }
}";
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var assembly = module.ContainingAssembly;
                Assert.NotNull(assembly.GetTypeByMetadataName("System.Runtime.CompilerServices.NullableAttribute"));
            });
        }

        [Fact]
        public void EmitAttribute_LocalFunctionConstraints_NoAnnotation()
        {
            var source = @"
class C
{
#nullable enable
    void M1()
    {
        local(new C());
        void local<T>(T t) where T : C
        {
        }
    }
}";
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var assembly = module.ContainingAssembly;
                Assert.NotNull(assembly.GetTypeByMetadataName("System.Runtime.CompilerServices.NullableAttribute"));
            });
        }

        [Fact]
        public void EmitAttribute_Module()
        {
            var source =
@"public class C
{
    public object? F = new object();
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var assembly = module.ContainingAssembly;
                var type = assembly.GetTypeByMetadataName("C");
                var field = (FieldSymbol)type.GetMembers("F").Single();
                AssertNullableAttribute(field.GetAttributes());
                AssertNoNullableAttribute(module.GetAttributes());
                AssertAttributes(assembly.GetAttributes(),
                    "System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                    "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
                    "System.Diagnostics.DebuggableAttribute");
            });
        }

        [Fact]
        public void EmitAttribute_NetModule()
        {
            var source =
@"public class C
{
    public object? F = new object();
}";
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
            comp.VerifyEmitDiagnostics(
                // (3,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     public object? F = new object();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "F").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 20));
        }

        [Fact]
        public void EmitAttribute_NetModuleNoDeclarations()
        {
            var source = "";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            CompileAndVerify(comp, verify: Verification.Skipped, symbolValidator: module =>
            {
                AssertAttributes(module.GetAttributes());
            });
        }

        [Fact]
        public void EmitAttribute_01()
        {
            var source =
@"public class Program
{
    public object? F;
    public object?[]? G;
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            var expected =
@"[NullableContext(2)] [Nullable(0)] Program
    System.Object? F
    System.Object?[]? G
    Program()
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_02()
        {
            var source =
@"public class Program
{
    public object? F(object?[]? args) => null;
    public object G(object[] args) => null!;
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            var expected =
@"Program
    [NullableContext(2)] System.Object? F(System.Object?[]? args)
        System.Object?[]? args
    [NullableContext(1)] System.Object! G(System.Object![]! args)
        System.Object![]! args
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_03()
        {
            var source =
@"public class Program
{
    public static void F(string x, string y, string z) { }
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            var expected =
@"Program
    [NullableContext(1)] void F(System.String! x, System.String! y, System.String! z)
        System.String! x
        System.String! y
        System.String! z
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_BaseClass()
        {
            var source =
@"public class A<T>
{
}
public class B1 : A<object>
{
}
public class B2 : A<object?>
{
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            var expected =
@"A<T>
    [Nullable(2)] T
[Nullable({ 0, 1 })] B1
[Nullable({ 0, 2 })] B2
";
            AssertNullableAttributes(comp, expected);

            var source2 =
@"class C
{
    static void F(A<object> x, A<object?> y)
    {
    }
    static void G(B1 x, B2 y)
    {
        F(x, x);
        F(y, y);
    }
}";
            var comp2 = CreateCompilation(new[] { source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics(
                // (8,14): warning CS8620: Argument of type 'B1' cannot be used as an input of type 'A<object?>' for parameter 'y' in 'void C.F(A<object> x, A<object?> y)' due to differences in the nullability of reference types.
                //         F(x, x);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("B1", "A<object?>", "y", "void C.F(A<object> x, A<object?> y)").WithLocation(8, 14),
                // (9,11): warning CS8620: Argument of type 'B2' cannot be used as an input of type 'A<object>' for parameter 'x' in 'void C.F(A<object> x, A<object?> y)' due to differences in the nullability of reference types.
                //         F(y, y);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("B2", "A<object>", "x", "void C.F(A<object> x, A<object?> y)").WithLocation(9, 11));
        }

        [Fact]
        public void EmitAttribute_Interface_01()
        {
            var source =
@"public interface I<T>
{
}
public class A : I<object>
{
}
#nullable disable
public class AOblivious : I<object> { }
#nullable enable
public class B : I<object?>
{
}
";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, validator: assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var typeDef = GetTypeDefinitionByName(reader, "A");
                var interfaceImpl = reader.GetInterfaceImplementation(typeDef.GetInterfaceImplementations().Single());
                AssertAttributes(reader, interfaceImpl.GetCustomAttributes(), "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])");
                typeDef = GetTypeDefinitionByName(reader, "B");
                interfaceImpl = reader.GetInterfaceImplementation(typeDef.GetInterfaceImplementations().Single());
                AssertAttributes(reader, interfaceImpl.GetCustomAttributes(), "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])");
            });
            var source2 =
@"class C
{
    static void F(I<object> x, I<object?> y) { }
#nullable disable
    static void FOblivious(I<object> x) { }
#nullable enable
    static void G(A x, B y, AOblivious z)
    {
        F(x, x);
        F(y, y);
        F(z, z);
        FOblivious(x);
        FOblivious(y);
        FOblivious(z);
    }
}";
            var comp2 = CreateCompilation(new[] { source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics(
                // (9,14): warning CS8620: Argument of type 'A' cannot be used as an input of type 'I<object?>' for parameter 'y' in 'void C.F(I<object> x, I<object?> y)' due to differences in the nullability of reference types.
                //         F(x, x);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("A", "I<object?>", "y", "void C.F(I<object> x, I<object?> y)").WithLocation(9, 14),
                // (10,11): warning CS8620: Argument of type 'B' cannot be used as an input of type 'I<object>' for parameter 'x' in 'void C.F(I<object> x, I<object?> y)' due to differences in the nullability of reference types.
                //         F(y, y);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("B", "I<object>", "x", "void C.F(I<object> x, I<object?> y)").WithLocation(10, 11));
        }

        [Fact]
        public void EmitAttribute_Interface_02()
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, validator: assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var typeDef = GetTypeDefinitionByName(reader, "A");
                var interfaceImpl = reader.GetInterfaceImplementation(typeDef.GetInterfaceImplementations().Single());
                AssertAttributes(reader, interfaceImpl.GetCustomAttributes(),
                    "MemberReference:Void System.Runtime.CompilerServices.TupleElementNamesAttribute..ctor(String[])");
                typeDef = GetTypeDefinitionByName(reader, "B");
                interfaceImpl = reader.GetInterfaceImplementation(typeDef.GetInterfaceImplementations().Single());
                AssertAttributes(reader, interfaceImpl.GetCustomAttributes(),
                    "MemberReference:Void System.Runtime.CompilerServices.TupleElementNamesAttribute..ctor(String[])",
                    "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])");
            });

            var source2 =
@"class C
{
    static void F(I<(object, object)> a, I<(object, object?)> b)
    {
    }
    static void G(A a, B b)
    {
        F(a, a);
        F(b, b);
    }
}";
            var comp2 = CreateCompilation(new[] { source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics(
                // (9,11): warning CS8620: Argument of type 'B' cannot be used as an input of type 'I<(object, object)>' for parameter 'a' in 'void C.F(I<(object, object)> a, I<(object, object?)> b)' due to differences in the nullability of reference types.
                //         F(b, b);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "b").WithArguments("B", "I<(object, object)>", "a", "void C.F(I<(object, object)> a, I<(object, object?)> b)").WithLocation(9, 11));

            var type = comp2.GetMember<NamedTypeSymbol>("A");
            Assert.Equal("I<(System.Object X, System.Object Y)>", type.Interfaces()[0].ToTestDisplayString());
            type = comp2.GetMember<NamedTypeSymbol>("B");
            Assert.Equal("I<(System.Object X, System.Object? Y)>", type.Interfaces()[0].ToTestDisplayString());
        }

        [Fact]
        public void EmitAttribute_ImplementedInterfaces_01()
        {
            var source =
@"#nullable enable
public interface I<T> { }
public class A :
    I<object>,
    I<string?>
{
    public object FA1;
    public object FA2;
}
public class B :
#nullable disable
    I<object>,
#nullable enable
    I<int>
{
    public object FB1;
    public object FB2;
}";
            var comp = CreateCompilation(source);
            var expected =
@"[NullableContext(2)] I<T>
    T
[NullableContext(1)] [Nullable(0)] A
    System.Object! FA1
    System.Object! FA2
    A()
B
    [Nullable(1)] System.Object! FB1
    [Nullable(1)] System.Object! FB2
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_ImplementedInterfaces_02()
        {
            var source =
@"#nullable enable
public interface IA { }
public interface IB<T> : IA { }
public interface IC<T> : IB<
#nullable disable
    object
#nullable enable
    >
{
}
public class C : IC<
#nullable disable
    string
#nullable enable
    >
{
    public object F1;
    public object F2;
    public object F3;
}";
            var comp = CreateCompilation(source);
            var expected =
@"IB<T>
    [Nullable(2)] T
IC<T>
    [Nullable(2)] T
C
    [Nullable(1)] System.Object! F1
    [Nullable(1)] System.Object! F2
    [Nullable(1)] System.Object! F3
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_TypeParameters()
        {
            var source =
@"#nullable enable
public interface I<T, U, V>
    where U : class
    where V : struct
{
    T F1();
    U F2();
    U? F3();
    V F4();
    V? F5();
#nullable disable
    T F6();
    U F7();
    U? F8();
    V F9();
    V? F10();
}";
            var comp = CreateCompilation(source);
            var expected =
@"I<T, U, V> where U : class! where V : struct
    [Nullable(2)] T
    [Nullable(1)] U
    [NullableContext(1)] T F1()
    [NullableContext(1)] U! F2()
    [NullableContext(2)] U? F3()
    [NullableContext(2)] U? F8()
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_Constraint_Nullable()
        {
            var source =
@"public class A
{
}
public class C<T> where T : A?
{
}
public class D<T> where T : A
{
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, validator: assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var typeDef = GetTypeDefinitionByName(reader, "C`1");
                var typeParameter = reader.GetGenericParameter(typeDef.GetGenericParameters()[0]);
                var constraint = reader.GetGenericParameterConstraint(typeParameter.GetConstraints()[0]);
                AssertAttributes(reader, constraint.GetCustomAttributes(), "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte)");
                typeDef = GetTypeDefinitionByName(reader, "D`1");
                typeParameter = reader.GetGenericParameter(typeDef.GetGenericParameters()[0]);
                constraint = reader.GetGenericParameterConstraint(typeParameter.GetConstraints()[0]);
                AssertAttributes(reader, constraint.GetCustomAttributes(), "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte)");
            });

            var source2 =
@"class B : A { }
class Program
{
    static void Main()
    {
        new C<A?>();
        new C<A>();
        new C<B?>();
        new C<B>();
        new D<A?>(); // warning
        new D<A>();
        new D<B?>(); // warning
        new D<B>();
    }
}";
            var comp2 = CreateCompilation(new[] { source, source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp2.VerifyEmitDiagnostics(
                // (10,15): warning CS8627: The type 'A?' cannot be used as type parameter 'T' in the generic type or method 'D<T>'. Nullability of type argument 'A?' doesn't match constraint type 'A'.
                //         new D<A?>(); // warning
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint, "A?").WithArguments("D<T>", "A", "T", "A?").WithLocation(10, 15),
                // (12,15): warning CS8627: The type 'B?' cannot be used as type parameter 'T' in the generic type or method 'D<T>'. Nullability of type argument 'B?' doesn't match constraint type 'A'.
                //         new D<B?>(); // warning
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint, "B?").WithArguments("D<T>", "A", "T", "B?").WithLocation(12, 15));

            comp2 = CreateCompilation(new[] { source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyEmitDiagnostics(
                // (10,15): warning CS8627: The type 'A?' cannot be used as type parameter 'T' in the generic type or method 'D<T>'. Nullability of type argument 'A?' doesn't match constraint type 'A'.
                //         new D<A?>(); // warning
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint, "A?").WithArguments("D<T>", "A", "T", "A?").WithLocation(10, 15),
                // (12,15): warning CS8627: The type 'B?' cannot be used as type parameter 'T' in the generic type or method 'D<T>'. Nullability of type argument 'B?' doesn't match constraint type 'A'.
                //         new D<B?>(); // warning
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint, "B?").WithArguments("D<T>", "A", "T", "B?").WithLocation(12, 15));

            var type = comp2.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("A?", type.TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0].ToTestDisplayString(true));
            type = comp2.GetMember<NamedTypeSymbol>("D");
            Assert.Equal("A!", type.TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0].ToTestDisplayString(true));
        }

        // https://github.com/dotnet/roslyn/issues/29976: Test with [NonNullTypes].
        [Fact]
        public void EmitAttribute_Constraint_Oblivious()
        {
            var source =
@"public class A<T>
{
}
public class C<T> where T : A<object>
{
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            CompileAndVerify(comp, validator: assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var typeDef = GetTypeDefinitionByName(reader, "C`1");
                var typeParameter = reader.GetGenericParameter(typeDef.GetGenericParameters()[0]);
                var constraint = reader.GetGenericParameterConstraint(typeParameter.GetConstraints()[0]);
                AssertAttributes(reader, constraint.GetCustomAttributes());
            });

            var source2 =
@"class B1 : A<object?> { }
class B2 : A<object> { }
class Program
{
    static void Main()
    {
        new C<A<object?>>();
        new C<A<object>>();
        new C<A<object?>?>();
        new C<A<object>?>();
        new C<B1>();
        new C<B2>();
        new C<B1?>();
        new C<B2?>();
    }
}";
            var comp2 = CreateCompilation(new[] { source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics();

            var type = comp2.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("A<System.Object>", type.TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0].ToTestDisplayString(true));
        }

        [WorkItem(27742, "https://github.com/dotnet/roslyn/issues/27742")]
        [Fact]
        public void EmitAttribute_Constraint_Nested()
        {
            var source =
@"public class A<T>
{
}
public class B<T> where T : A<object?>
{
}
public class C<T> where T : A<object>
{
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, validator: assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var typeDef = GetTypeDefinitionByName(reader, "B`1");
                var typeParameter = reader.GetGenericParameter(typeDef.GetGenericParameters()[0]);
                var constraint = reader.GetGenericParameterConstraint(typeParameter.GetConstraints()[0]);
                AssertAttributes(reader, constraint.GetCustomAttributes(), "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])");
                typeDef = GetTypeDefinitionByName(reader, "C`1");
                typeParameter = reader.GetGenericParameter(typeDef.GetGenericParameters()[0]);
                constraint = reader.GetGenericParameterConstraint(typeParameter.GetConstraints()[0]);
                AssertAttributes(reader, constraint.GetCustomAttributes(), "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte)");
            });

            var source2 =
@"class Program
{
    static void Main()
    {
        new B<A<object?>>();
        new B<A<object>>(); // warning
        new C<A<object?>>(); // warning
        new C<A<object>>();
    }
}";
            var comp2 = CreateCompilation(new[] { source, source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            comp2.VerifyEmitDiagnostics(
                // (6,15): warning CS8627: The type 'A<object>' cannot be used as type parameter 'T' in the generic type or method 'B<T>'. Nullability of type argument 'A<object>' doesn't match constraint type 'A<object?>'.
                //         new B<A<object>>(); // warning
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint, "A<object>").WithArguments("B<T>", "A<object?>", "T", "A<object>").WithLocation(6, 15),
                // (7,15): warning CS8627: The type 'A<object?>' cannot be used as type parameter 'T' in the generic type or method 'C<T>'. Nullability of type argument 'A<object?>' doesn't match constraint type 'A<object>'.
                //         new C<A<object?>>(); // warning
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint, "A<object?>").WithArguments("C<T>", "A<object>", "T", "A<object?>").WithLocation(7, 15));

            comp2 = CreateCompilation(new[] { source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics(
                // (6,15): warning CS8627: The type 'A<object>' cannot be used as type parameter 'T' in the generic type or method 'B<T>'. Nullability of type argument 'A<object>' doesn't match constraint type 'A<object?>'.
                //         new B<A<object>>(); // warning
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint, "A<object>").WithArguments("B<T>", "A<object?>", "T", "A<object>").WithLocation(6, 15),
                // (7,15): warning CS8627: The type 'A<object?>' cannot be used as type parameter 'T' in the generic type or method 'C<T>'. Nullability of type argument 'A<object?>' doesn't match constraint type 'A<object>'.
                //         new C<A<object?>>(); // warning
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint, "A<object?>").WithArguments("C<T>", "A<object>", "T", "A<object?>").WithLocation(7, 15));

            var type = comp2.GetMember<NamedTypeSymbol>("B");
            Assert.Equal("A<System.Object?>!", type.TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0].ToTestDisplayString(true));
            type = comp2.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("A<System.Object!>!", type.TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0].ToTestDisplayString(true));
        }

        [Fact]
        public void EmitAttribute_Constraint_TypeParameter()
        {
            var source =
@"public class C<T, U>
    where T : class
    where U : T?
{
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, validator: assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var typeDef = GetTypeDefinitionByName(reader, "C`2");
                var typeParameter = reader.GetGenericParameter(typeDef.GetGenericParameters()[1]);
                var constraint = reader.GetGenericParameterConstraint(typeParameter.GetConstraints()[0]);
                AssertAttributes(reader, constraint.GetCustomAttributes(), "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte)");
            });

            var source2 =
@"class Program
{
    static void Main()
    {
        new C<object?, string?>();
        new C<object?, string>();
        new C<object, string?>();
        new C<object, string>();
    }
}";
            var comp2 = CreateCompilation(new[] { source, source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            var expected = new[] {
                // (5,15): warning CS8634: The type 'object?' cannot be used as type parameter 'T' in the generic type or method 'C<T, U>'. Nullability of type argument 'object?' doesn't match 'class' constraint.
                //         new C<object?, string?>();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "object?").WithArguments("C<T, U>", "T", "object?").WithLocation(5, 15),
                // (6,15): warning CS8634: The type 'object?' cannot be used as type parameter 'T' in the generic type or method 'C<T, U>'. Nullability of type argument 'object?' doesn't match 'class' constraint.
                //         new C<object?, string>();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "object?").WithArguments("C<T, U>", "T", "object?").WithLocation(6, 15)
            };

            comp2.VerifyEmitDiagnostics(expected);

            comp2 = CreateCompilation(new[] { source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyEmitDiagnostics(expected);

            var type = comp2.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("T?", type.TypeParameters[1].ConstraintTypesNoUseSiteDiagnostics[0].ToTestDisplayString(true));
        }

        [Fact]
        public void EmitAttribute_Constraints()
        {
            var source =
@"#nullable enable
public abstract class Program
{
#nullable disable
    public abstract void M0<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
#nullable enable
        where T4 : class?;
    public abstract void M1<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
#nullable disable
        where T4 : class;
#nullable enable
    public abstract void M2<T1, T2, T3, T4>()
        where T1 : class?
        where T2 : class?
        where T3 : class?
        where T4 : class;
    private object _f1;
    private object _f2;
    private object _f3;
}";
            var comp = CreateCompilation(source);
            var expected =
@"[NullableContext(1)] [Nullable(0)] Program
    [NullableContext(0)] void M0<T1, T2, T3, T4>() where T1 : class where T2 : class where T3 : class where T4 : class?
        T1
        T2
        T3
        [Nullable(2)] T4
    void M1<T1, T2, T3, T4>() where T1 : class! where T2 : class! where T3 : class! where T4 : class
        T1
        T2
        T3
        [Nullable(0)] T4
    [NullableContext(2)] void M2<T1, T2, T3, T4>() where T1 : class? where T2 : class? where T3 : class? where T4 : class!
        T1
        T2
        T3
        [Nullable(1)] T4
    Program()
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_ClassConstraint_SameAsContext()
        {
            var source =
@"#nullable enable
public class Program
{
    public class C0<T0>
#nullable disable
        where T0 : class
#nullable enable
    {
#nullable disable
        public object F01;
        public object F02;
#nullable enable
    }
    public class C1<T1>
        where T1 : class
    {
        public object F11;
        public object F12;
    }
    public class C2<T2>
        where T2 : class?
    {
        public object? F21;
        public object? F22;
    }
    public object F31;
    public object F32;
    public object F33;
}";
            var comp = CreateCompilation(source);
            var expected =
@"[NullableContext(1)] [Nullable(0)] Program
    System.Object! F31
    System.Object! F32
    System.Object! F33
    Program()
    [NullableContext(0)] Program.C0<T0> where T0 : class
        T0
        System.Object F01
        System.Object F02
        C0()
    [Nullable(0)] Program.C1<T1> where T1 : class!
        T1
        System.Object! F11
        System.Object! F12
        C1()
    [NullableContext(2)] [Nullable(0)] Program.C2<T2> where T2 : class?
        T2
        System.Object? F21
        System.Object? F22
        C2()
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                AssertNullableAttributes(module, expected);
                verifyTypeParameterConstraint("Program.C0", null);
                verifyTypeParameterConstraint("Program.C1", false);
                verifyTypeParameterConstraint("Program.C2", true);

                void verifyTypeParameterConstraint(string typeName, bool? expectedConstraintIsNullable)
                {
                    var typeParameter = module.GlobalNamespace.GetMember<NamedTypeSymbol>(typeName).TypeParameters.Single();
                    Assert.True(typeParameter.HasReferenceTypeConstraint);
                    Assert.Equal(expectedConstraintIsNullable, typeParameter.ReferenceTypeConstraintIsNullable);
                }
            });
        }

        [Fact]
        public void EmitAttribute_ClassConstraint_DifferentFromContext()
        {
            var source =
@"#nullable enable
public class Program
{
#nullable enable
    public class C0<T0>
#nullable disable
        where T0 : class
#nullable enable
    {
        public object F01;
        public object F02;
    }
    public class C1<T1>
        where T1 : class
    {
        public object? F11;
        public object? F12;
    }
    public class C2<T2>
        where T2 : class?
    {
        public object F21;
        public object F22;
    }
    public object F31;
    public object F32;
    public object F33;
}";
            var comp = CreateCompilation(source);
            var expected =
@"[NullableContext(1)] [Nullable(0)] Program
    System.Object! F31
    System.Object! F32
    System.Object! F33
    Program()
    [NullableContext(0)] Program.C0<T0> where T0 : class
        T0
        [Nullable(1)] System.Object! F01
        [Nullable(1)] System.Object! F02
        C0()
    [NullableContext(2)] [Nullable(0)] Program.C1<T1> where T1 : class!
        [Nullable(1)] T1
        System.Object? F11
        System.Object? F12
        C1()
    [Nullable(0)] Program.C2<T2> where T2 : class?
        [Nullable(2)] T2
        System.Object! F21
        System.Object! F22
        C2()
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                AssertNullableAttributes(module, expected);
                verifyTypeParameterConstraint("Program.C0", null);
                verifyTypeParameterConstraint("Program.C1", false);
                verifyTypeParameterConstraint("Program.C2", true);

                void verifyTypeParameterConstraint(string typeName, bool? expectedConstraintIsNullable)
                {
                    var typeParameter = module.GlobalNamespace.GetMember<NamedTypeSymbol>(typeName).TypeParameters.Single();
                    Assert.True(typeParameter.HasReferenceTypeConstraint);
                    Assert.Equal(expectedConstraintIsNullable, typeParameter.ReferenceTypeConstraintIsNullable);
                }
            });
        }

        [Fact]
        public void EmitAttribute_NotNullConstraint()
        {
            var source =
@"#nullable enable
public class C0<T0>
    where T0 : notnull
{
#nullable disable
    public object F01;
    public object F02;
#nullable enable
}
public class C1<T1>
    where T1 : notnull
{
    public object F11;
    public object F12;
}
public class C2<T2>
    where T2 : notnull
{
    public object? F21;
    public object? F22;
}";
            var comp = CreateCompilation(source);
            var expected =
@"C0<T0> where T0 : notnull
    [Nullable(1)] T0
[NullableContext(1)] [Nullable(0)] C1<T1> where T1 : notnull
    T1
    System.Object! F11
    System.Object! F12
    C1()
[NullableContext(2)] [Nullable(0)] C2<T2> where T2 : notnull
    [Nullable(1)] T2
    System.Object? F21
    System.Object? F22
    C2()
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                AssertNullableAttributes(module, expected);
                verifyTypeParameterConstraint("C0");
                verifyTypeParameterConstraint("C1");
                verifyTypeParameterConstraint("C2");

                void verifyTypeParameterConstraint(string typeName)
                {
                    var typeParameter = module.GlobalNamespace.GetMember<NamedTypeSymbol>(typeName).TypeParameters.Single();
                    Assert.True(typeParameter.HasNotNullConstraint);
                }
            });
        }

        [Fact]
        public void EmitAttribute_ConstraintTypes_01()
        {
            var source =
@"#nullable enable
public interface IA { }
public interface IB<T> { }
public interface I0<T>
#nullable disable
    where T : IA, IB<int>
#nullable enable
{
    object F01();
    object F02();
}
public interface I1<T>
    where T : IA, IB<int>
{
    object? F11();
    object? F12();
}
public interface I2<T>
    where T : IA?, IB<object?>?
{
    object F21();
    object F22();
}";
            var comp = CreateCompilation(source);
            var expected =
@"[NullableContext(2)] IB<T>
    T
I0<T> where T : IA, IB<System.Int32>
    [NullableContext(1)] System.Object! F01()
    [NullableContext(1)] System.Object! F02()
[NullableContext(1)] I1<T> where T : IA!, IB<System.Int32>!
    [Nullable(0)] T
    [NullableContext(2)] System.Object? F11()
    [NullableContext(2)] System.Object? F12()
[NullableContext(1)] I2<T> where T : IA?, IB<System.Object?>?
    [Nullable(0)] T
    System.Object! F21()
    System.Object! F22()
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_ConstraintTypes_02()
        {
            var source =
@"#nullable enable
public interface IA { }
public interface IB<T> { }
public class Program
{
    public static void M0<T>(object x, object y)
#nullable disable
        where T : IA, IB<int>
#nullable enable
    {
    }
    public static void M1<T>(object? x, object? y)
        where T : IA, IB<int>
    {
    }
    public static void M2<T>(object x, object y)
        where T : IA?, IB<object?>?
    {
    }
}";
            var comp = CreateCompilation(source);
            var expected =
@"[NullableContext(2)] IB<T>
    T
Program
    void M0<T>(System.Object! x, System.Object! y) where T : IA, IB<System.Int32>
        [Nullable(1)] System.Object! x
        [Nullable(1)] System.Object! y
    [NullableContext(1)] void M1<T>(System.Object? x, System.Object? y) where T : IA!, IB<System.Int32>!
        [Nullable(0)] T
        [Nullable(2)] System.Object? x
        [Nullable(2)] System.Object? y
    [NullableContext(1)] void M2<T>(System.Object! x, System.Object! y) where T : IA?, IB<System.Object?>?
        [Nullable(0)] T
        System.Object! x
        System.Object! y
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_MethodReturnType()
        {
            var source =
@"public class C
{
    public object? F() => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var expected =
@"C
    [NullableContext(2)] System.Object? F()
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_MethodParameters()
        {
            var source =
@"public class A
{
    public void F(object?[] c) { }
}
#nullable enable
public class B
{
    public void F(object x, object y) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var expected =
@"A
    void F(System.Object?[] c)
        [Nullable({ 0, 2 })] System.Object?[] c
B
    [NullableContext(1)] void F(System.Object! x, System.Object! y)
        System.Object! x
        System.Object! y
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_ConstructorParameters()
        {
            var source =
@"public class C
{
    public C(object?[] c) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var expected =
@"C
    C(System.Object?[] c)
        [Nullable({ 0, 2 })] System.Object?[] c
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_PropertyType()
        {
            var source =
@"public class C
{
    public object? P => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var expected =
@"[NullableContext(2)] [Nullable(0)] C
    C()
    System.Object? P { get; }
        System.Object? P.get
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_PropertyParameters()
        {
            var source =
@"public class A
{
    public object this[object x, object? y] => throw new System.NotImplementedException();
}
#nullable enable
public class B
{
    public object this[object x, object y] => throw new System.NotImplementedException();
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var expected =
@"A
    System.Object this[System.Object x, System.Object? y] { get; }
        [Nullable(2)] System.Object? y
        System.Object this[System.Object x, System.Object? y].get
            [Nullable(2)] System.Object? y
[NullableContext(1)] [Nullable(0)] B
    B()
    System.Object! this[System.Object! x, System.Object! y] { get; }
        System.Object! x
        System.Object! y
        System.Object! this[System.Object! x, System.Object! y].get
            System.Object! x
            System.Object! y
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_Indexers()
        {
            var source =
@"#nullable enable
public class Program
{
    public object this[object? x, object? y] => throw new System.NotImplementedException();
    public object this[object? z] { set { } }
}";
            var comp = CreateCompilation(source);
            var expected =
@"[NullableContext(1)] [Nullable(0)] Program
    Program()
    System.Object! this[System.Object! x, System.Object! y] { get; }
        System.Object! x
        System.Object! y
        [NullableContext(2)] [Nullable(1)] System.Object! this[System.Object! x, System.Object! y].get
            System.Object? x
            System.Object? y
    System.Object! this[System.Object? z] { set; }
        [Nullable(2)] System.Object? z
        void this[System.Object? z].set
            [Nullable(2)] System.Object? z
            System.Object! value
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_OperatorReturnType()
        {
            var source =
@"public class C
{
    public static object? operator+(C a, C b) => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var expected =
@"C
    [Nullable(2)] System.Object? operator +(C a, C b)
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_OperatorParameters()
        {
            var source =
@"public class C
{
    public static object operator+(C a, object?[] b) => a;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var expected =
@"C
    System.Object operator +(C a, System.Object?[] b)
        [Nullable({ 0, 2 })] System.Object?[] b
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_DelegateReturnType()
        {
            var source =
@"public delegate object? D();";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var expected =
@"D
    [NullableContext(2)] System.Object? Invoke()
    [Nullable(2)] System.Object? EndInvoke(System.IAsyncResult result)
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_DelegateParameters()
        {
            var source =
@"public delegate void D(object?[] o);";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var expected =
@"D
    void Invoke(System.Object?[] o)
        [Nullable({ 0, 2 })] System.Object?[] o
    System.IAsyncResult BeginInvoke(System.Object?[] o, System.AsyncCallback callback, System.Object @object)
        [Nullable({ 0, 2 })] System.Object?[] o
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_NestedEnum()
        {
            var source =
@"#nullable enable
public class Program
{
    public enum E
    {
        A,
        B
    }
    public object F1;
    public object F2;
    public object F3;
}";
            var comp = CreateCompilation(source);
            var expected =
@"[NullableContext(1)] [Nullable(0)] Program
    System.Object! F1
    System.Object! F2
    System.Object! F3
    Program()
    [NullableContext(0)] Program.E
        A
        B
        E()
";
            AssertNullableAttributes(comp, expected);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            AssertNoNullableAttributes(comp);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            AssertNoNullableAttributes(comp);
        }

        // See https://github.com/dotnet/roslyn/issues/28862.
        [Fact]
        public void EmitAttribute_QueryClauseParameters()
        {
            var source0 =
@"public class A
{
    public static object?[] F(object[] x) => x;
}";
            var comp0 = CreateCompilation(source0, parseOptions: TestOptions.Regular8);
            var ref0 = comp0.EmitToImageReference();

            var source =
@"using System.Linq;
class B
{
    static void M(object[] c)
    {
        var z = from x in A.F(c)
            let y = x
            where y != null
            select y;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8, references: new[] { ref0 });
            AssertNoNullableAttributes(comp);
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
            var comp0 = CreateCompilation(source0, parseOptions: TestOptions.Regular8);
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
                references: new[] { ref0 },
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
        [WorkItem(30010, "https://github.com/dotnet/roslyn/issues/30010")]
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
                    AssertNullableAttribute(method.GetReturnTypeAttributes());
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
        public void EmitAttribute_Byte0()
        {
            var source =
@"#nullable enable
public class Program
{
#nullable disable
    public object
#nullable enable
        F1(object x, object y) => null;
#nullable disable
    public object
#nullable enable
        F2(object? x, object? y) => null;
}";
            var comp = CreateCompilation(source);
            var expected =
@"Program
    [NullableContext(1)] [Nullable(0)] System.Object F1(System.Object! x, System.Object! y)
        System.Object! x
        System.Object! y
    [NullableContext(2)] [Nullable(0)] System.Object F2(System.Object? x, System.Object? y)
        System.Object? x
        System.Object? y
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void EmitPrivateMetadata_BaseTypes()
        {
            var source =
@"public class Base<T, U> { }
namespace Namespace
{
    public class Public : Base<object, string?> { }
    internal class Internal : Base<object, string?> { }
}
public class PublicTypes
{
    public class Public : Base<object, string?> { }
    internal class Internal : Base<object, string?> { }
    protected class Protected : Base<object, string?> { }
    protected internal class ProtectedInternal : Base<object, string?> { }
    private protected class PrivateProtected : Base<object, string?> { }
    private class Private : Base<object, string?> { }
}
internal class InternalTypes
{
    public class Public : Base<object, string?> { }
    internal class Internal : Base<object, string?> { }
    protected class Protected : Base<object, string?> { }
    protected internal class ProtectedInternal : Base<object, string?> { }
    private protected class PrivateProtected : Base<object, string?> { }
    private class Private : Base<object, string?> { }
}";
            var expectedPublicOnly = @"
[NullableContext(2)] [Nullable(0)] Base<T, U>
    T
    U
    Base()
PublicTypes
    [Nullable({ 0, 1, 2 })] PublicTypes.Public
    [Nullable({ 0, 1, 2 })] PublicTypes.Protected
    [Nullable({ 0, 1, 2 })] PublicTypes.ProtectedInternal
[Nullable({ 0, 1, 2 })] Namespace.Public
";
            var expectedPublicAndInternal = @"
[NullableContext(2)] [Nullable(0)] Base<T, U>
    T
    U
    Base()
PublicTypes
    [Nullable({ 0, 1, 2 })] PublicTypes.Public
    [Nullable({ 0, 1, 2 })] PublicTypes.Internal
    [Nullable({ 0, 1, 2 })] PublicTypes.Protected
    [Nullable({ 0, 1, 2 })] PublicTypes.ProtectedInternal
    [Nullable({ 0, 1, 2 })] PublicTypes.PrivateProtected
InternalTypes
    [Nullable({ 0, 1, 2 })] InternalTypes.Public
    [Nullable({ 0, 1, 2 })] InternalTypes.Internal
    [Nullable({ 0, 1, 2 })] InternalTypes.Protected
    [Nullable({ 0, 1, 2 })] InternalTypes.ProtectedInternal
    [Nullable({ 0, 1, 2 })] InternalTypes.PrivateProtected
[Nullable({ 0, 1, 2 })] Namespace.Public
[Nullable({ 0, 1, 2 })] Namespace.Internal
";
            var expectedAll = @"
[NullableContext(2)] [Nullable(0)] Base<T, U>
    T
    U
    Base()
PublicTypes
    [Nullable({ 0, 1, 2 })] PublicTypes.Public
    [Nullable({ 0, 1, 2 })] PublicTypes.Internal
    [Nullable({ 0, 1, 2 })] PublicTypes.Protected
    [Nullable({ 0, 1, 2 })] PublicTypes.ProtectedInternal
    [Nullable({ 0, 1, 2 })] PublicTypes.PrivateProtected
    [Nullable({ 0, 1, 2 })] PublicTypes.Private
InternalTypes
    [Nullable({ 0, 1, 2 })] InternalTypes.Public
    [Nullable({ 0, 1, 2 })] InternalTypes.Internal
    [Nullable({ 0, 1, 2 })] InternalTypes.Protected
    [Nullable({ 0, 1, 2 })] InternalTypes.ProtectedInternal
    [Nullable({ 0, 1, 2 })] InternalTypes.PrivateProtected
    [Nullable({ 0, 1, 2 })] InternalTypes.Private
[Nullable({ 0, 1, 2 })] Namespace.Public
[Nullable({ 0, 1, 2 })] Namespace.Internal
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        public void EmitPrivateMetadata_Delegates()
        {
            var source =
@"public class Program
{
    protected delegate object ProtectedDelegate(object? arg);
    internal delegate object InternalDelegate(object? arg);
    private delegate object PrivateDelegate(object? arg);
}";
            var expectedPublicOnly = @"
Program
    Program.ProtectedDelegate
        [NullableContext(1)] System.Object! Invoke(System.Object? arg)
            [Nullable(2)] System.Object? arg
        System.IAsyncResult BeginInvoke(System.Object? arg, System.AsyncCallback callback, System.Object @object)
            [Nullable(2)] System.Object? arg
        [Nullable(1)] System.Object! EndInvoke(System.IAsyncResult result)
";
            var expectedPublicAndInternal = @"
Program
    Program.ProtectedDelegate
        [NullableContext(1)] System.Object! Invoke(System.Object? arg)
            [Nullable(2)] System.Object? arg
        System.IAsyncResult BeginInvoke(System.Object? arg, System.AsyncCallback callback, System.Object @object)
            [Nullable(2)] System.Object? arg
        [Nullable(1)] System.Object! EndInvoke(System.IAsyncResult result)
    Program.InternalDelegate
        [NullableContext(1)] System.Object! Invoke(System.Object? arg)
            [Nullable(2)] System.Object? arg
        System.IAsyncResult BeginInvoke(System.Object? arg, System.AsyncCallback callback, System.Object @object)
            [Nullable(2)] System.Object? arg
        [Nullable(1)] System.Object! EndInvoke(System.IAsyncResult result)
";
            var expectedAll = @"
Program
    Program.ProtectedDelegate
        [NullableContext(1)] System.Object! Invoke(System.Object? arg)
            [Nullable(2)] System.Object? arg
        System.IAsyncResult BeginInvoke(System.Object? arg, System.AsyncCallback callback, System.Object @object)
            [Nullable(2)] System.Object? arg
        [Nullable(1)] System.Object! EndInvoke(System.IAsyncResult result)
    Program.InternalDelegate
        [NullableContext(1)] System.Object! Invoke(System.Object? arg)
            [Nullable(2)] System.Object? arg
        System.IAsyncResult BeginInvoke(System.Object? arg, System.AsyncCallback callback, System.Object @object)
            [Nullable(2)] System.Object? arg
        [Nullable(1)] System.Object! EndInvoke(System.IAsyncResult result)
    Program.PrivateDelegate
        [NullableContext(1)] System.Object! Invoke(System.Object? arg)
            [Nullable(2)] System.Object? arg
        System.IAsyncResult BeginInvoke(System.Object? arg, System.AsyncCallback callback, System.Object @object)
            [Nullable(2)] System.Object? arg
        [Nullable(1)] System.Object! EndInvoke(System.IAsyncResult result)
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        public void EmitPrivateMetadata_Events()
        {
            var source =
@"#nullable disable
public delegate void D<T>(T t);
#nullable enable
public class Program
{
    public event D<object?>? PublicEvent { add { } remove { } }
    internal event D<object> InternalEvent { add { } remove { } }
    protected event D<object?> ProtectedEvent { add { } remove { } }
    protected internal event D<object?> ProtectedInternalEvent { add { } remove { } }
    private protected event D<object>? PrivateProtectedEvent { add { } remove { } }
    private event D<object?>? PrivateEvent { add { } remove { } }
}";
            var expectedPublicOnly = @"
[NullableContext(2)] [Nullable(0)] Program
    Program()
    event D<System.Object?>? PublicEvent
        void PublicEvent.add
            D<System.Object?>? value
        void PublicEvent.remove
            D<System.Object?>? value
    [Nullable(1)] event D<System.Object!>! InternalEvent
    [Nullable({ 1, 2 })] event D<System.Object?>! ProtectedEvent
        void ProtectedEvent.add
            [Nullable({ 1, 2 })] D<System.Object?>! value
        void ProtectedEvent.remove
            [Nullable({ 1, 2 })] D<System.Object?>! value
    [Nullable({ 1, 2 })] event D<System.Object?>! ProtectedInternalEvent
        void ProtectedInternalEvent.add
            [Nullable({ 1, 2 })] D<System.Object?>! value
        void ProtectedInternalEvent.remove
            [Nullable({ 1, 2 })] D<System.Object?>! value
    [Nullable({ 2, 1 })] event D<System.Object!>? PrivateProtectedEvent
";
            var expectedPublicAndInternal = @"
[NullableContext(2)] [Nullable(0)] Program
    Program()
    event D<System.Object?>? PublicEvent
        void PublicEvent.add
            D<System.Object?>? value
        void PublicEvent.remove
            D<System.Object?>? value
    [Nullable(1)] event D<System.Object!>! InternalEvent
        [NullableContext(1)] void InternalEvent.add
            D<System.Object!>! value
        [NullableContext(1)] void InternalEvent.remove
            D<System.Object!>! value
    [Nullable({ 1, 2 })] event D<System.Object?>! ProtectedEvent
        void ProtectedEvent.add
            [Nullable({ 1, 2 })] D<System.Object?>! value
        void ProtectedEvent.remove
            [Nullable({ 1, 2 })] D<System.Object?>! value
    [Nullable({ 1, 2 })] event D<System.Object?>! ProtectedInternalEvent
        void ProtectedInternalEvent.add
            [Nullable({ 1, 2 })] D<System.Object?>! value
        void ProtectedInternalEvent.remove
            [Nullable({ 1, 2 })] D<System.Object?>! value
    [Nullable({ 2, 1 })] event D<System.Object!>? PrivateProtectedEvent
        void PrivateProtectedEvent.add
            [Nullable({ 2, 1 })] D<System.Object!>? value
        void PrivateProtectedEvent.remove
            [Nullable({ 2, 1 })] D<System.Object!>? value
";
            var expectedAll = @"
[NullableContext(2)] [Nullable(0)] Program
    Program()
    event D<System.Object?>? PublicEvent
        void PublicEvent.add
            D<System.Object?>? value
        void PublicEvent.remove
            D<System.Object?>? value
    [Nullable(1)] event D<System.Object!>! InternalEvent
        [NullableContext(1)] void InternalEvent.add
            D<System.Object!>! value
        [NullableContext(1)] void InternalEvent.remove
            D<System.Object!>! value
    [Nullable({ 1, 2 })] event D<System.Object?>! ProtectedEvent
        void ProtectedEvent.add
            [Nullable({ 1, 2 })] D<System.Object?>! value
        void ProtectedEvent.remove
            [Nullable({ 1, 2 })] D<System.Object?>! value
    [Nullable({ 1, 2 })] event D<System.Object?>! ProtectedInternalEvent
        void ProtectedInternalEvent.add
            [Nullable({ 1, 2 })] D<System.Object?>! value
        void ProtectedInternalEvent.remove
            [Nullable({ 1, 2 })] D<System.Object?>! value
    [Nullable({ 2, 1 })] event D<System.Object!>? PrivateProtectedEvent
        void PrivateProtectedEvent.add
            [Nullable({ 2, 1 })] D<System.Object!>? value
        void PrivateProtectedEvent.remove
            [Nullable({ 2, 1 })] D<System.Object!>? value
    event D<System.Object?>? PrivateEvent
        void PrivateEvent.add
            D<System.Object?>? value
        void PrivateEvent.remove
            D<System.Object?>? value
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        public void EmitPrivateMetadata_Fields()
        {
            var source =
@"public class Program
{
    public object PublicField;
    internal object? InternalField;
    protected object ProtectedField;
    protected internal object? ProtectedInternalField;
    private protected object? PrivateProtectedField;
    private object? PrivateField;
}";
            var expectedPublicOnly = @"
[NullableContext(1)] [Nullable(0)] Program
    System.Object! PublicField
    System.Object! ProtectedField
    [Nullable(2)] System.Object? ProtectedInternalField
    Program()
";
            var expectedPublicAndInternal = @"
[NullableContext(2)] [Nullable(0)] Program
    [Nullable(1)] System.Object! PublicField
    System.Object? InternalField
    [Nullable(1)] System.Object! ProtectedField
    System.Object? ProtectedInternalField
    System.Object? PrivateProtectedField
    Program()
";
            var expectedAll = @"
[NullableContext(2)] [Nullable(0)] Program
    [Nullable(1)] System.Object! PublicField
    System.Object? InternalField
    [Nullable(1)] System.Object! ProtectedField
    System.Object? ProtectedInternalField
    System.Object? PrivateProtectedField
    System.Object? PrivateField
    Program()
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        public void EmitPrivateMetadata_Methods()
        {
            var source =
@"public class Program
{
    public void PublicMethod(object arg) { }
    internal object? InternalMethod(object? arg) => null;
    protected object ProtectedMethod(object? arg) => null;
    protected internal object? ProtectedInternalMethod(object? arg) => null;
    private protected void PrivateProtectedMethod(object? arg) { }
    private object? PrivateMethod(object? arg) => null;
}";
            var expectedPublicOnly = @"
[NullableContext(1)] [Nullable(0)] Program
    void PublicMethod(System.Object! arg)
        System.Object! arg
    System.Object! ProtectedMethod(System.Object? arg)
        [Nullable(2)] System.Object? arg
    [NullableContext(2)] System.Object? ProtectedInternalMethod(System.Object? arg)
        System.Object? arg
    Program()
";
            var expectedPublicAndInternal = @"
[NullableContext(2)] [Nullable(0)] Program
    [NullableContext(1)] void PublicMethod(System.Object! arg)
        System.Object! arg
    System.Object? InternalMethod(System.Object? arg)
        System.Object? arg
    [NullableContext(1)] System.Object! ProtectedMethod(System.Object? arg)
        [Nullable(2)] System.Object? arg
    System.Object? ProtectedInternalMethod(System.Object? arg)
        System.Object? arg
    void PrivateProtectedMethod(System.Object? arg)
        System.Object? arg
    Program()
";
            var expectedAll = @"
[NullableContext(2)] [Nullable(0)] Program
    [NullableContext(1)] void PublicMethod(System.Object! arg)
        System.Object! arg
    System.Object? InternalMethod(System.Object? arg)
        System.Object? arg
    [NullableContext(1)] System.Object! ProtectedMethod(System.Object? arg)
        [Nullable(2)] System.Object? arg
    System.Object? ProtectedInternalMethod(System.Object? arg)
        System.Object? arg
    void PrivateProtectedMethod(System.Object? arg)
        System.Object? arg
    System.Object? PrivateMethod(System.Object? arg)
        System.Object? arg
    Program()
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        public void EmitPrivateMetadata_Properties()
        {
            var source =
@"public class Program
{
    public object PublicProperty => null;
    internal object? InternalProperty => null;
    protected object ProtectedProperty => null;
    protected internal object? ProtectedInternalProperty => null;
    private protected object? PrivateProtectedProperty => null;
    private object? PrivateProperty => null;
}";
            var expectedPublicOnly = @"
[NullableContext(2)] [Nullable(0)] Program
    Program()
    [Nullable(1)] System.Object! PublicProperty { get; }
        [NullableContext(1)] System.Object! PublicProperty.get
    [Nullable(1)] System.Object! ProtectedProperty { get; }
        [NullableContext(1)] System.Object! ProtectedProperty.get
    System.Object? ProtectedInternalProperty { get; }
        System.Object? ProtectedInternalProperty.get
";
            var expectedPublicAndInternal = @"
[NullableContext(2)] [Nullable(0)] Program
    Program()
    [Nullable(1)] System.Object! PublicProperty { get; }
        [NullableContext(1)] System.Object! PublicProperty.get
    System.Object? InternalProperty { get; }
        System.Object? InternalProperty.get
    [Nullable(1)] System.Object! ProtectedProperty { get; }
        [NullableContext(1)] System.Object! ProtectedProperty.get
    System.Object? ProtectedInternalProperty { get; }
        System.Object? ProtectedInternalProperty.get
    System.Object? PrivateProtectedProperty { get; }
        System.Object? PrivateProtectedProperty.get
";
            var expectedAll = @"
[NullableContext(2)] [Nullable(0)] Program
    Program()
    [Nullable(1)] System.Object! PublicProperty { get; }
        [NullableContext(1)] System.Object! PublicProperty.get
    System.Object? InternalProperty { get; }
        System.Object? InternalProperty.get
    [Nullable(1)] System.Object! ProtectedProperty { get; }
        [NullableContext(1)] System.Object! ProtectedProperty.get
    System.Object? ProtectedInternalProperty { get; }
        System.Object? ProtectedInternalProperty.get
    System.Object? PrivateProtectedProperty { get; }
        System.Object? PrivateProtectedProperty.get
    System.Object? PrivateProperty { get; }
        System.Object? PrivateProperty.get
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        public void EmitPrivateMetadata_Indexers()
        {
            var source =
@"public class Program
{
    public class PublicType
    {
        public object? this[object? x, object y] => null;
    }
    internal class InternalType
    {
        public object this[object x, object y] { get => null; set { } }
    }
    protected class ProtectedType
    {
        public object? this[object x, object? y] { get => null; set { } }
    }
    protected internal class ProtectedInternalType
    {
        public object this[object x, object y]  { set { } }
    }
    private protected class PrivateProtectedType
    {
        public object this[object x, object y] => null;
    }
    private class PrivateType
    {
        public object this[object x, object y] => null;
    }
}";
            var expectedPublicOnly = @"
[NullableContext(2)] [Nullable(0)] Program
    Program()
    [Nullable(0)] Program.PublicType
        PublicType()
        System.Object? this[System.Object? x, System.Object! y] { get; }
            System.Object? x
            [Nullable(1)] System.Object! y
            System.Object? this[System.Object? x, System.Object! y].get
                System.Object? x
                [Nullable(1)] System.Object! y
    [Nullable(0)] Program.ProtectedType
        ProtectedType()
        System.Object? this[System.Object! x, System.Object? y] { get; set; }
            [Nullable(1)] System.Object! x
            System.Object? y
            System.Object? this[System.Object! x, System.Object? y].get
                [Nullable(1)] System.Object! x
                System.Object? y
            void this[System.Object! x, System.Object? y].set
                [Nullable(1)] System.Object! x
                System.Object? y
                System.Object? value
    [NullableContext(1)] [Nullable(0)] Program.ProtectedInternalType
        ProtectedInternalType()
        System.Object! this[System.Object! x, System.Object! y] { set; }
            System.Object! x
            System.Object! y
            void this[System.Object! x, System.Object! y].set
                System.Object! x
                System.Object! y
                System.Object! value
";
            var expectedPublicAndInternal = @"
[NullableContext(1)] [Nullable(0)] Program
    Program()
    [NullableContext(2)] [Nullable(0)] Program.PublicType
        PublicType()
        System.Object? this[System.Object? x, System.Object! y] { get; }
            System.Object? x
            [Nullable(1)] System.Object! y
            System.Object? this[System.Object? x, System.Object! y].get
                System.Object? x
                [Nullable(1)] System.Object! y
    [Nullable(0)] Program.InternalType
        InternalType()
        System.Object! this[System.Object! x, System.Object! y] { get; set; }
            System.Object! x
            System.Object! y
            System.Object! this[System.Object! x, System.Object! y].get
                System.Object! x
                System.Object! y
            void this[System.Object! x, System.Object! y].set
                System.Object! x
                System.Object! y
                System.Object! value
    [NullableContext(2)] [Nullable(0)] Program.ProtectedType
        ProtectedType()
        System.Object? this[System.Object! x, System.Object? y] { get; set; }
            [Nullable(1)] System.Object! x
            System.Object? y
            System.Object? this[System.Object! x, System.Object? y].get
                [Nullable(1)] System.Object! x
                System.Object? y
            void this[System.Object! x, System.Object? y].set
                [Nullable(1)] System.Object! x
                System.Object? y
                System.Object? value
    [Nullable(0)] Program.ProtectedInternalType
        ProtectedInternalType()
        System.Object! this[System.Object! x, System.Object! y] { set; }
            System.Object! x
            System.Object! y
            void this[System.Object! x, System.Object! y].set
                System.Object! x
                System.Object! y
                System.Object! value
    [Nullable(0)] Program.PrivateProtectedType
        PrivateProtectedType()
        System.Object! this[System.Object! x, System.Object! y] { get; }
            System.Object! x
            System.Object! y
            System.Object! this[System.Object! x, System.Object! y].get
                System.Object! x
                System.Object! y
";
            var expectedAll = @"
[NullableContext(1)] [Nullable(0)] Program
    Program()
    [NullableContext(2)] [Nullable(0)] Program.PublicType
        PublicType()
        System.Object? this[System.Object? x, System.Object! y] { get; }
            System.Object? x
            [Nullable(1)] System.Object! y
            System.Object? this[System.Object? x, System.Object! y].get
                System.Object? x
                [Nullable(1)] System.Object! y
    [Nullable(0)] Program.InternalType
        InternalType()
        System.Object! this[System.Object! x, System.Object! y] { get; set; }
            System.Object! x
            System.Object! y
            System.Object! this[System.Object! x, System.Object! y].get
                System.Object! x
                System.Object! y
            void this[System.Object! x, System.Object! y].set
                System.Object! x
                System.Object! y
                System.Object! value
    [NullableContext(2)] [Nullable(0)] Program.ProtectedType
        ProtectedType()
        System.Object? this[System.Object! x, System.Object? y] { get; set; }
            [Nullable(1)] System.Object! x
            System.Object? y
            System.Object? this[System.Object! x, System.Object? y].get
                [Nullable(1)] System.Object! x
                System.Object? y
            void this[System.Object! x, System.Object? y].set
                [Nullable(1)] System.Object! x
                System.Object? y
                System.Object? value
    [Nullable(0)] Program.ProtectedInternalType
        ProtectedInternalType()
        System.Object! this[System.Object! x, System.Object! y] { set; }
            System.Object! x
            System.Object! y
            void this[System.Object! x, System.Object! y].set
                System.Object! x
                System.Object! y
                System.Object! value
    [Nullable(0)] Program.PrivateProtectedType
        PrivateProtectedType()
        System.Object! this[System.Object! x, System.Object! y] { get; }
            System.Object! x
            System.Object! y
            System.Object! this[System.Object! x, System.Object! y].get
                System.Object! x
                System.Object! y
    [Nullable(0)] Program.PrivateType
        PrivateType()
        System.Object! this[System.Object! x, System.Object! y] { get; }
            System.Object! x
            System.Object! y
            System.Object! this[System.Object! x, System.Object! y].get
                System.Object! x
                System.Object! y
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        public void EmitPrivateMetadata_TypeParameters()
        {
            var source =
@"public class Base { }
public class Program
{
    protected static void ProtectedMethod<T, U>()
        where T : notnull
        where U : class
    {
    }
    internal static void InternalMethod<T, U>()
        where T : notnull
        where U : class
    {
    }
    private static void PrivateMethod<T, U>()
        where T : notnull
        where U : class
    {
    }
}";
            var expectedPublicOnly = @"
Program
    [NullableContext(1)] void ProtectedMethod<T, U>() where T : notnull where U : class!
        T
        U
";
            var expectedPublicAndInternal = @"
[NullableContext(1)] [Nullable(0)] Program
    void ProtectedMethod<T, U>() where T : notnull where U : class!
        T
        U
    void InternalMethod<T, U>() where T : notnull where U : class!
        T
        U
    Program()
";
            var expectedAll = @"
[NullableContext(1)] [Nullable(0)] Program
    void ProtectedMethod<T, U>() where T : notnull where U : class!
        T
        U
    void InternalMethod<T, U>() where T : notnull where U : class!
        T
        U
    void PrivateMethod<T, U>() where T : notnull where U : class!
        T
        U
    Program()
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        [WorkItem(37161, "https://github.com/dotnet/roslyn/issues/37161")]
        public void EmitPrivateMetadata_ExplicitImplementation()
        {
            var source =
@"public interface I<T>
{
    T M(T[] args);
    T P { get; set; }
    T[] this[T index] { get; }
}
public class C : I<object?>
{
    object? I<object?>.M(object?[] args) => throw null!;
    object? I<object?>.P { get; set; }
    object?[] I<object?>.this[object? index] => throw null!;
}";
            // Attributes emitted for explicitly-implemented property and indexer, but not for accessors.
            var expectedPublicOnly = @"
[NullableContext(1)] I<T>
    [Nullable(2)] T
    T M(T[]! args)
        T[]! args
    T P { get; set; }
        T P.get
        void P.set
            T value
    T[]! this[T index] { get; }
        T index
        T[]! this[T index].get
            T index
C
    [Nullable(2)] System.Object? I<System.Object>.P { get; set; }
    [Nullable({ 1, 2 })] System.Object?[]! I<System.Object>.Item[System.Object index] { get; }
";
            // Attributes emitted for explicitly-implemented property and indexer, but not for accessors.
            var expectedPublicAndInternal = @"
[NullableContext(1)] I<T>
    [Nullable(2)] T
    T M(T[]! args)
        T[]! args
    T P { get; set; }
        T P.get
        void P.set
            T value
    T[]! this[T index] { get; }
        T index
        T[]! this[T index].get
            T index
C
    [Nullable(2)] System.Object? I<System.Object>.P { get; set; }
    [Nullable({ 1, 2 })] System.Object?[]! I<System.Object>.Item[System.Object index] { get; }
";
            var expectedAll = @"
[NullableContext(1)] I<T>
    [Nullable(2)] T
    T M(T[]! args)
        T[]! args
    T P { get; set; }
        T P.get
        void P.set
            T value
    T[]! this[T index] { get; }
        T index
        T[]! this[T index].get
            T index
[NullableContext(2)] [Nullable(0)] C
    System.Object? <I<System.Object>.P>k__BackingField
    System.Object? I<System.Object>.M(System.Object?[]! args)
        [Nullable({ 1, 2 })] System.Object?[]! args
    [Nullable({ 1, 2 })] System.Object?[]! I<System.Object>.get_Item(System.Object? index)
        System.Object? index
    C()
    System.Object? I<System.Object>.P { get; set; }
        System.Object? I<System.Object>.P.get
        void I<System.Object>.P.set
            System.Object? value
    [Nullable({ 1, 2 })] System.Object?[]! I<System.Object>.Item[System.Object? index] { get; }
        System.Object? index
    [Nullable({ 1, 2 })] System.Object?[]! I<System.Object>.get_Item(System.Object? index)
        System.Object? index
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        public void EmitPrivateMetadata_SynthesizedFields()
        {
            var source =
@"public struct S<T> { }
public class Public
{
    public static void PublicMethod()
    {
        S<object?> s;
        System.Action a = () => { s.ToString(); };
    }
}";
            var expectedPublicOnly = @"
S<T>
    [Nullable(2)] T
";
            var expectedPublicAndInternal = @"
S<T>
    [Nullable(2)] T
";
            var expectedAll = @"
S<T>
    [Nullable(2)] T
Public
    Public.<>c__DisplayClass0_0
        [Nullable({ 0, 2 })] S<System.Object?> s
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        public void EmitPrivateMetadata_SynthesizedParameters()
        {
            var source =
@"public class Public
{
    private static void PrivateMethod(string x)
    {
        _ = new System.Action<string?>((string y) => { });
    }
}";
            var expectedPublicOnly = @"";
            var expectedPublicAndInternal = @"";
            var expectedAll = @"
Public
    [NullableContext(1)] void PrivateMethod(System.String! x)
        System.String! x
    Public.<>c
        [Nullable({ 0, 2 })] System.Action<System.String?> <>9__0_0
        [NullableContext(1)] void <PrivateMethod>b__0_0(System.String! y)
            System.String! y
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        public void EmitPrivateMetadata_AnonymousType()
        {
            var source =
@"public class Program
{
    public static void Main()
    {
        _ = new { A = new object(), B = (string?)null };
    }
}";
            var expectedPublicOnly = @"";
            var expectedPublicAndInternal = @"";
            var expectedAll = @"";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        [Fact]
        public void EmitPrivateMetadata_Iterator()
        {
            var source =
@"using System.Collections.Generic;
public class Program
{
    public static IEnumerable<object?> F()
    {
        yield break;
    }
}";
            var expectedPublicOnly = @"
Program
    [Nullable({ 1, 2 })] System.Collections.Generic.IEnumerable<System.Object?>! F()
";
            var expectedPublicAndInternal = @"
Program
    [Nullable({ 1, 2 })] System.Collections.Generic.IEnumerable<System.Object?>! F()
";
            var expectedAll = @"
Program
    [Nullable({ 1, 2 })] System.Collections.Generic.IEnumerable<System.Object?>! F()
    Program.<F>d__0
        [Nullable(2)] System.Object? <>2__current
        System.Object System.Collections.Generic.IEnumerator<System.Object>.Current { get; }
            [Nullable(2)] System.Object? System.Collections.Generic.IEnumerator<System.Object>.Current.get
";
            EmitPrivateMetadata(source, expectedPublicOnly, expectedPublicAndInternal, expectedAll);
        }

        private void EmitPrivateMetadata(string source, string expectedPublicOnly, string expectedPublicAndInternal, string expectedAll)
        {
            var sourceIVTs =
@"using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""Other"")]";

            var options = WithNonNullTypesTrue().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;
            AssertNullableAttributes(CreateCompilation(source, options: options, parseOptions: parseOptions), expectedAll);
            AssertNullableAttributes(CreateCompilation(source, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly")), expectedPublicOnly);
            AssertNullableAttributes(CreateCompilation(new[] { source, sourceIVTs }, options: options, parseOptions: parseOptions), expectedAll);
            AssertNullableAttributes(CreateCompilation(new[] { source, sourceIVTs }, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly")), expectedPublicAndInternal);
        }

        /// <summary>
        /// Should only require NullableAttribute constructor if nullable annotations are emitted.
        /// </summary>
        [Fact]
        public void EmitPrivateMetadata_MissingAttributeConstructor()
        {
            var sourceAttribute =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullableAttribute : Attribute { }
}";
            var source =
@"#pragma warning disable 0067
#pragma warning disable 0169
#pragma warning disable 8321
public class A
{
    private object? F;
    private static object? M(object arg) => null;
    private object? P => null;
    private object? this[object x, object? y] => null;
    private event D<object?> E;
    public static void M()
    {
        object? f(object arg) => arg;
        object? l(object arg) { return arg; }
        D<object> d = () => new object();
    }
}
internal delegate T D<T>();
internal interface I<T> { }
internal class B : I<object>
{
    public static object operator!(B b) => b;
    public event D<object?> E;
    private (object, object?) F;
}";
            var options = WithNonNullTypesTrue();
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(new[] { sourceAttribute, source }, options: options, parseOptions: parseOptions);
            comp.VerifyEmitDiagnostics(
                // (6,21): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? F;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(6, 21),
                // (7,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private static object? M(object arg) => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(7, 20),
                // (7,30): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private static object? M(object arg) => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object arg").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(7, 30),
                // (8,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? P => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(8, 13),
                // (9,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? this[object x, object? y] => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(9, 13),
                // (9,26): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? this[object x, object? y] => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object x").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(9, 26),
                // (9,36): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? this[object x, object? y] => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object? y").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(9, 36),
                // (10,30): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private event D<object?> E;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 30),
                // (10,30): warning CS8618: Non-nullable event 'E' is uninitialized. Consider declaring the event as nullable.
                //     private event D<object?> E;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "E").WithArguments("event", "E").WithLocation(10, 30),
                // (13,9): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //         object? f(object arg) => arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(13, 9),
                // (13,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //         object? f(object arg) => arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object arg").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(13, 19),
                // (14,9): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //         object? l(object arg) { return arg; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(14, 9),
                // (14,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //         object? l(object arg) { return arg; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object arg").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(14, 19),
                // (15,26): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //         D<object> d = () => new object();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=>").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(15, 26),
                // (18,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                // internal delegate T D<T>();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "T").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(18, 19),
                // (18,23): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                // internal delegate T D<T>();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "T").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(18, 23),
                // (19,22): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                // internal interface I<T> { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "T").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(19, 22),
                // (20,16): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                // internal class B : I<object>
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "B").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(20, 16),
                // (22,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     public static object operator!(B b) => b;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(22, 19),
                // (22,36): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     public static object operator!(B b) => b;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "B b").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(22, 36),
                // (23,29): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     public event D<object?> E;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(23, 29),
                // (23,29): warning CS8618: Non-nullable event 'E' is uninitialized. Consider declaring the event as nullable.
                //     public event D<object?> E;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "E").WithArguments("event", "E").WithLocation(23, 29),
                // (24,31): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private (object, object?) F;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(24, 31));

            comp = CreateCompilation(new[] { sourceAttribute, source }, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly"));
            comp.VerifyEmitDiagnostics(
                // (8,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? P => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(8, 13),
                // (9,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? this[object x, object? y] => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(9, 13),
                // (9,26): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? this[object x, object? y] => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object x").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(9, 26),
                // (9,36): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? this[object x, object? y] => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object? y").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(9, 36),
                // (10,30): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private event D<object?> E;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(10, 30),
                // (10,30): warning CS8618: Non-nullable event 'E' is uninitialized. Consider declaring the event as nullable.
                //     private event D<object?> E;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "E").WithArguments("event", "E").WithLocation(10, 30),
                // (23,29): warning CS8618: Non-nullable event 'E' is uninitialized. Consider declaring the event as nullable.
                //     public event D<object?> E;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "E").WithArguments("event", "E").WithLocation(23, 29)
                );
        }

        [Fact]
        public void EmitPrivateMetadata_MissingAttributeConstructor_NullableDisabled()
        {
            var sourceAttribute =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullableAttribute : Attribute { }
}";
            var source =
@"#pragma warning disable 414
public class Program
{
    private object? F = null;
    private object? P => null;
}";
            var options = TestOptions.ReleaseDll;
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(new[] { sourceAttribute, source }, options: options, parseOptions: parseOptions);
            comp.VerifyEmitDiagnostics(
                // (4,19): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private object? F = null;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(4, 19),
                // (4,21): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? F = null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(4, 21),
                // (5,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? P => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(5, 13),
                // (5,19): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private object? P => null;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(5, 19));

            comp = CreateCompilation(new[] { sourceAttribute, source }, options: options, parseOptions: parseOptions.WithFeature("nullablePublicOnly"));
            comp.VerifyEmitDiagnostics(
                // (4,19): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private object? F = null;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(4, 19),
                // (5,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'
                //     private object? P => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute", ".ctor").WithLocation(5, 13),
                // (5,19): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     private object? P => null;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(5, 19));
        }

        [Fact]
        public void EmitAttribute_ValueTypes_01()
        {
            var source =
@"#nullable enable
struct S1<T> { }
struct S2<T, U> { }
class C1<T> { }
class C2<T, U> { }
class Program
{
    static void F() { }
    int F11;
    int? F12;
#nullable disable
    object F21;
#nullable enable
    object F22;
    S1<int> F31;
    S1<int?>? F32;
    S1<
#nullable disable
        object
#nullable enable
        > F33;
    S1<object?> F34;
    S2<int, int> F41;
    S2<int,
#nullable disable
        object
#nullable enable
        > F42;
    S2<
#nullable disable
        object,
#nullable enable
        int> F43;
    S2<
#nullable disable
        object, object
#nullable enable
        > F44;
    S2<int, object> F45;
    S2<object?, int> F46;
    S2<
#nullable disable
        object,
#nullable enable
        object> F47;
    S2<object?,
#nullable disable
        object
#nullable enable
        > F48;
    S2<object, object?> F49;
    C1<int
#nullable disable
        > F51;
#nullable enable
    C1<int?
#nullable disable
        > F52;
#nullable enable
    C1<int> F53;
    C1<int?> F54;
    C1<
#nullable disable
        object> F55;
#nullable enable
    C1<object
#nullable disable
        > F56;
#nullable enable
    C1<
#nullable disable
        object
#nullable enable
        >? F57;
    C1<object>? F58;
    C2<int,
#nullable disable
        object> F60;
#nullable enable
    C2<int, object
#nullable disable
        > F61;
#nullable enable
    C2<object?, int
#nullable disable
        > F62;
#nullable enable
    C2<int, object> F63;
    C2<object?, int>? F64;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate);

            static void validate(ModuleSymbol module)
            {
                var globalNamespace = module.GlobalNamespace;
                VerifyBytes(globalNamespace.GetMember<MethodSymbol>("Program.F").ReturnTypeWithAnnotations, new byte[] { 0 }, new byte[] { }, "void");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F11").TypeWithAnnotations, new byte[] { 0 }, new byte[] { }, "int");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F12").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { }, "int?");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F21").TypeWithAnnotations, new byte[] { 0 }, new byte[] { 0 }, "object");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F22").TypeWithAnnotations, new byte[] { 1 }, new byte[] { 1 }, "object!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F31").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0 }, "S1<int>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F32").TypeWithAnnotations, new byte[] { 0, 0, 0, 0 }, new byte[] { 0 }, "S1<int?>?");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F33").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0, 0 }, "S1<object>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F34").TypeWithAnnotations, new byte[] { 0, 2 }, new byte[] { 0, 2 }, "S1<object?>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F41").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0 }, "S2<int, int>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F42").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0, 0 }, "S2<int, object>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F43").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0, 0 }, "S2<object, int>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F44").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0, 0, 0 }, "S2<object, object>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F45").TypeWithAnnotations, new byte[] { 0, 0, 1 }, new byte[] { 0, 1 }, "S2<int, object!>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F46").TypeWithAnnotations, new byte[] { 0, 2, 0 }, new byte[] { 0, 2 }, "S2<object?, int>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F47").TypeWithAnnotations, new byte[] { 0, 0, 1 }, new byte[] { 0, 0, 1 }, "S2<object, object!>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F48").TypeWithAnnotations, new byte[] { 0, 2, 0 }, new byte[] { 0, 2, 0 }, "S2<object?, object>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F49").TypeWithAnnotations, new byte[] { 0, 1, 2 }, new byte[] { 0, 1, 2 }, "S2<object!, object?>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F51").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0 }, "C1<int>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F52").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0 }, "C1<int?>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F53").TypeWithAnnotations, new byte[] { 1, 0 }, new byte[] { 1 }, "C1<int>!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F54").TypeWithAnnotations, new byte[] { 1, 0, 0 }, new byte[] { 1 }, "C1<int?>!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F55").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0, 0 }, "C1<object>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F56").TypeWithAnnotations, new byte[] { 0, 1 }, new byte[] { 0, 1 }, "C1<object!>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F57").TypeWithAnnotations, new byte[] { 2, 0 }, new byte[] { 2, 0 }, "C1<object>?");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F58").TypeWithAnnotations, new byte[] { 2, 1 }, new byte[] { 2, 1 }, "C1<object!>?");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F60").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0, 0 }, "C2<int, object>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F61").TypeWithAnnotations, new byte[] { 0, 0, 1 }, new byte[] { 0, 1 }, "C2<int, object!>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F62").TypeWithAnnotations, new byte[] { 0, 2, 0 }, new byte[] { 0, 2 }, "C2<object?, int>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F63").TypeWithAnnotations, new byte[] { 1, 0, 1 }, new byte[] { 1, 1 }, "C2<int, object!>!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F64").TypeWithAnnotations, new byte[] { 2, 2, 0 }, new byte[] { 2, 2 }, "C2<object?, int>?");
            }
        }

        [Fact]
        public void EmitAttribute_ValueTypes_02()
        {
            var source =
@"#nullable enable
struct S<T> { }
class Program
{
    int
#nullable disable
        [] F1;
#nullable enable
    int[] F2;
    int?[]? F3;
    int
#nullable disable
        []
#nullable enable
        [] F4;
    int?[]
#nullable disable
        [] F5;
#nullable enable
    S<int
#nullable disable
        []
#nullable enable
        > F6;
    S<int?[]?>? F7;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate);

            static void validate(ModuleSymbol module)
            {
                var globalNamespace = module.GlobalNamespace;
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F1").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0 }, "int[]");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F2").TypeWithAnnotations, new byte[] { 1, 0 }, new byte[] { 1 }, "int[]!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F3").TypeWithAnnotations, new byte[] { 2, 0, 0 }, new byte[] { 2 }, "int?[]?");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F4").TypeWithAnnotations, new byte[] { 0, 1, 0 }, new byte[] { 0, 1 }, "int[]![]");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F5").TypeWithAnnotations, new byte[] { 1, 0, 0, 0 }, new byte[] { 1, 0 }, "int?[][]!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F6").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0, 0 }, "S<int[]>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F7").TypeWithAnnotations, new byte[] { 0, 0, 2, 0, 0 }, new byte[] { 0, 2 }, "S<int?[]?>?");
            }
        }

        [Fact]
        public void EmitAttribute_ValueTypes_03()
        {
            var source =
@"#nullable enable
class Program
{
    System.ValueTuple F0;
    (int, int) F1;
    (int?, int?)? F2;
#nullable disable
    (int, object) F3;
    (object, int) F4;
#nullable enable
    (int, object?) F5;
    (object, int) F6;
    ((int, int), ((int, int), int)) F7;
    ((int, int), ((int, object), int)) F8;
#nullable disable
    (int _1, int _2, int _3, int _4, int _5, int _6, int _7, object _8) F9;
#nullable enable
    (int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, int _9) F10;
    (int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, object _9) F11;
    (int _1, int _2, int _3, int _4, int _5, int _6, int _7, object _8, int _9) F12;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate);

            static void validate(ModuleSymbol module)
            {
                var globalNamespace = module.GlobalNamespace;
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F0").TypeWithAnnotations, new byte[] { 0 }, new byte[] { }, "System.ValueTuple");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F1").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0 }, "(int, int)");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F2").TypeWithAnnotations, new byte[] { 0, 0, 0, 0, 0, 0 }, new byte[] { 0 }, "(int?, int?)?");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F3").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0, 0 }, "(int, object)");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F4").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0, 0 }, "(object, int)");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F5").TypeWithAnnotations, new byte[] { 0, 0, 2 }, new byte[] { 0, 2 }, "(int, object?)");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F6").TypeWithAnnotations, new byte[] { 0, 1, 0 }, new byte[] { 0, 1 }, "(object!, int)");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F7").TypeWithAnnotations, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new byte[] { 0, 0, 0, 0 }, "((int, int), ((int, int), int))");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F8").TypeWithAnnotations, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1, 0 }, new byte[] { 0, 0, 0, 0, 1 }, "((int, int), ((int, object!), int))");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F9").TypeWithAnnotations, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new byte[] { 0, 0, 0 }, "(int _1, int _2, int _3, int _4, int _5, int _6, int _7, object _8)");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F10").TypeWithAnnotations, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new byte[] { 0, 0 }, "(int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, int _9)");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F11").TypeWithAnnotations, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, new byte[] { 0, 0, 1 }, "(int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, object! _9)");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F12").TypeWithAnnotations, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 }, new byte[] { 0, 0, 1 }, "(int _1, int _2, int _3, int _4, int _5, int _6, int _7, object! _8, int _9)");
            }
        }

        [Fact]
        public void EmitAttribute_ValueTypes_04()
        {
            var source =
@"#nullable enable
struct S0
{
    internal struct S { }
    internal class C { }
}
struct S1<T>
{
    internal struct S { }
    internal class C { }
}
class C0
{
    internal struct S { }
    internal class C { }
}
class C1<T>
{
    internal struct S { }
    internal class C { }
}
class Program
{
    S0.S F11;
#nullable disable
    S0.C F12;
#nullable enable
    S0.C F13;
    S1<int>.S F21;
#nullable disable
    S1<int>.C F22;
#nullable enable
    S1<int>.C F23;
#nullable disable
    S1<object>.S F24;
#nullable enable
    S1<object>.S F25;
    S1<
#nullable disable
        object
#nullable enable
        >.C F26;
    S1<object
#nullable disable
        >.C F27;
#nullable enable
    S1<int>.S[] F28;
    S1<C1<object>.S> F29;
    C0.S F31;
#nullable disable
    C0.C F32;
#nullable enable
    C0.C F33;
    C1<int>.S F41;
#nullable disable
    C1<int>.C F42;
#nullable enable
    C1<int>.C F43;
#nullable disable
    C1<object>.S F44;
#nullable enable
    C1<object>.S F45;
    C1<
#nullable disable
        object
#nullable enable
        >.C F46;
    C1<object
#nullable disable
        >.C F47;
#nullable enable
    C1<int>.S[] F48;
    C1<S1<object>.S> F49;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate);

            static void validate(ModuleSymbol module)
            {
                var globalNamespace = module.GlobalNamespace;
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F11").TypeWithAnnotations, new byte[] { 0 }, new byte[] { }, "S0.S");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F12").TypeWithAnnotations, new byte[] { 0 }, new byte[] { 0 }, "S0.C");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F13").TypeWithAnnotations, new byte[] { 1 }, new byte[] { 1 }, "S0.C!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F21").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0 }, "S1<int>.S");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F22").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0 }, "S1<int>.C");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F23").TypeWithAnnotations, new byte[] { 1, 0 }, new byte[] { 1 }, "S1<int>.C!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F24").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0, 0 }, "S1<object>.S");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F25").TypeWithAnnotations, new byte[] { 0, 1 }, new byte[] { 0, 1 }, "S1<object!>.S");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F26").TypeWithAnnotations, new byte[] { 1, 0 }, new byte[] { 1, 0 }, "S1<object>.C!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F27").TypeWithAnnotations, new byte[] { 0, 1 }, new byte[] { 0, 1 }, "S1<object!>.C");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F28").TypeWithAnnotations, new byte[] { 1, 0, 0 }, new byte[] { 1, 0 }, "S1<int>.S[]!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F29").TypeWithAnnotations, new byte[] { 0, 0, 1 }, new byte[] { 0, 0, 1 }, "S1<C1<object!>.S>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F31").TypeWithAnnotations, new byte[] { 0 }, new byte[] { }, "C0.S");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F32").TypeWithAnnotations, new byte[] { 0 }, new byte[] { 0 }, "C0.C");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F33").TypeWithAnnotations, new byte[] { 1 }, new byte[] { 1 }, "C0.C!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F41").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0 }, "C1<int>.S");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F42").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0 }, "C1<int>.C");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F43").TypeWithAnnotations, new byte[] { 1, 0 }, new byte[] { 1 }, "C1<int>.C!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F44").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0, 0 }, "C1<object>.S");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F45").TypeWithAnnotations, new byte[] { 0, 1 }, new byte[] { 0, 1 }, "C1<object!>.S");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F46").TypeWithAnnotations, new byte[] { 1, 0 }, new byte[] { 1, 0 }, "C1<object>.C!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F47").TypeWithAnnotations, new byte[] { 0, 1 }, new byte[] { 0, 1 }, "C1<object!>.C");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F48").TypeWithAnnotations, new byte[] { 1, 0, 0 }, new byte[] { 1, 0 }, "C1<int>.S[]!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F49").TypeWithAnnotations, new byte[] { 1, 0, 1 }, new byte[] { 1, 0, 1 }, "C1<S1<object!>.S>!");
            }
        }

        [Fact]
        public void EmitAttribute_ValueTypes_05()
        {
            var source =
@"#nullable enable
interface I0
{
    internal delegate void D();
    internal enum E { }
    internal interface I { }
}
interface I1<T>
{
    internal delegate void D();
    internal enum E { }
    internal interface I { }
}
class Program
{
    I0.D F1;
    I0.E F2;
    I0.I F3;
    I1<int>.D F4;
    I1<int>.E F5;
    I1<int>.I F6;
#nullable disable
    I1<object>.D F7;
    I1<object>.E F8;
    I1<object>.I F9;
#nullable enable
    I1<object>.E F10;
    I1<int>.E[] F11;
    I1<I0.E> F12;
    I1<I1<object>.E>.E F13;
    I1<I1<int>.D>.I F14;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate);

            static void validate(ModuleSymbol module)
            {
                var globalNamespace = module.GlobalNamespace;
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F1").TypeWithAnnotations, new byte[] { 1 }, new byte[] { 1 }, "I0.D!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F2").TypeWithAnnotations, new byte[] { 0 }, new byte[] { }, "I0.E");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F3").TypeWithAnnotations, new byte[] { 1 }, new byte[] { 1 }, "I0.I!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F4").TypeWithAnnotations, new byte[] { 1, 0 }, new byte[] { 1 }, "I1<int>.D!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F5").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0 }, "I1<int>.E");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F6").TypeWithAnnotations, new byte[] { 1, 0 }, new byte[] { 1 }, "I1<int>.I!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F7").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0, 0 }, "I1<object>.D");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F8").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0, 0 }, "I1<object>.E");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F9").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0, 0 }, "I1<object>.I");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F10").TypeWithAnnotations, new byte[] { 0, 1 }, new byte[] { 0, 1 }, "I1<object!>.E");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F11").TypeWithAnnotations, new byte[] { 1, 0, 0 }, new byte[] { 1, 0 }, "I1<int>.E[]!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F12").TypeWithAnnotations, new byte[] { 1, 0 }, new byte[] { 1 }, "I1<I0.E>!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F13").TypeWithAnnotations, new byte[] { 0, 0, 1 }, new byte[] { 0, 0, 1 }, "I1<I1<object!>.E>.E");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F14").TypeWithAnnotations, new byte[] { 1, 1, 0 }, new byte[] { 1, 1 }, "I1<I1<int>.D!>.I!");
            }
        }

        [Fact]
        public void EmitAttribute_ValueTypes_06()
        {
            var source =
@"#nullable enable
struct S<T> { }
class C<T> { }
unsafe class Program
{
    int* F1;
    int?* F2;
    S<int*> F3;
    S<int>* F4;
#nullable disable
    C<int*> F5;
#nullable enable
    C<int*> F6;
}";
            var comp = CreateCompilation(source);
            var globalNamespace = comp.GlobalNamespace;
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F1").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0 }, "int*");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F2").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0 }, "int?*");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F3").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0, 0 }, "S<int*>");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F4").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0, 0 }, "S<int>*");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F5").TypeWithAnnotations, new byte[] { 0, 0, 0 }, new byte[] { 0, 0 }, "C<int*>");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F6").TypeWithAnnotations, new byte[] { 1, 0, 0 }, new byte[] { 1, 0 }, "C<int*>!");
        }

        [Fact]
        public void EmitAttribute_ValueTypes_07()
        {
            var source =
@"#nullable enable
class C<T> { }
struct S<T> { }
class Program<T, U, V>
    where U : class
    where V : struct
{
    T F11;
    T[] F12;
    C<T> F13;
    S<T> F14;
#nullable disable
    U F21;
#nullable enable
    U? F22;
    U[] F23;
    C<U> F24;
    S<U> F25;
    V F31;
    V? F32;
    V[] F33;
    C<V> F34;
    S<V> F35;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate);

            static void validate(ModuleSymbol module)
            {
                var globalNamespace = module.GlobalNamespace;
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F11").TypeWithAnnotations, new byte[] { 1 }, new byte[] { 1 }, "T");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F12").TypeWithAnnotations, new byte[] { 1, 1 }, new byte[] { 1, 1 }, "T[]!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F13").TypeWithAnnotations, new byte[] { 1, 1 }, new byte[] { 1, 1 }, "C<T>!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F14").TypeWithAnnotations, new byte[] { 0, 1 }, new byte[] { 0, 1 }, "S<T>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F21").TypeWithAnnotations, new byte[] { 0 }, new byte[] { 0 }, "U");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F22").TypeWithAnnotations, new byte[] { 2 }, new byte[] { 2 }, "U?");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F23").TypeWithAnnotations, new byte[] { 1, 1 }, new byte[] { 1, 1 }, "U![]!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F24").TypeWithAnnotations, new byte[] { 1, 1 }, new byte[] { 1, 1 }, "C<U!>!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F25").TypeWithAnnotations, new byte[] { 0, 1 }, new byte[] { 0, 1 }, "S<U!>");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F31").TypeWithAnnotations, new byte[] { 0 }, new byte[] { 0 }, "V");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F32").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0 }, "V?");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F33").TypeWithAnnotations, new byte[] { 1, 0 }, new byte[] { 1, 0 }, "V[]!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F34").TypeWithAnnotations, new byte[] { 1, 0 }, new byte[] { 1, 0 }, "C<V>!");
                VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F35").TypeWithAnnotations, new byte[] { 0, 0 }, new byte[] { 0, 0 }, "S<V>");
            }
        }

        [Fact]
        public void EmitAttribute_ValueTypes_08()
        {
            var source0 =
@"public struct S0 { }
public struct S2<T, U> { }";
            var comp = CreateCompilation(source0);
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"#nullable enable
public class C2<T, U> { }
public class Program
{
    public C2<S0, object?> F1;
    public C2<object, S0>? F2;
    public S2<S0, object> F3;
    public S2<object?, S0> F4;
    public (S0, object) F5;
    public (object?, S0) F6;
}";

            // With reference assembly.
            comp = CreateCompilation(source1, references: new[] { ref0 });
            var ref1 = comp.EmitToImageReference();

            var globalNamespace = comp.GlobalNamespace;
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F1").TypeWithAnnotations, new byte[] { 1, 0, 2 }, new byte[] { 1, 2 }, "C2<S0, object?>!");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F2").TypeWithAnnotations, new byte[] { 2, 1, 0 }, new byte[] { 2, 1 }, "C2<object!, S0>?");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F3").TypeWithAnnotations, new byte[] { 0, 0, 1 }, new byte[] { 0, 1 }, "S2<S0, object!>");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F4").TypeWithAnnotations, new byte[] { 0, 2, 0 }, new byte[] { 0, 2 }, "S2<object?, S0>");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F5").TypeWithAnnotations, new byte[] { 0, 0, 1 }, new byte[] { 0, 1 }, "(S0, object!)");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F6").TypeWithAnnotations, new byte[] { 0, 2, 0 }, new byte[] { 0, 2 }, "(object?, S0)");

            // Without reference assembly.
            comp = CreateCompilation(source1);
            globalNamespace = comp.GlobalNamespace;
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F1").TypeWithAnnotations, new byte[] { 1, 0, 2 }, new byte[] { 1, 1, 2 }, "C2<S0!, object?>!");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F2").TypeWithAnnotations, new byte[] { 2, 1, 0 }, new byte[] { 2, 1, 1 }, "C2<object!, S0!>?");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F3").TypeWithAnnotations, new byte[] { 0, 0, 1 }, new byte[] { 1, 1, 1 }, "S2<S0!, object!>!");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F4").TypeWithAnnotations, new byte[] { 0, 2, 0 }, new byte[] { 1, 2, 1 }, "S2<object?, S0!>!");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F5").TypeWithAnnotations, new byte[] { 0, 0, 1 }, new byte[] { 0, 1, 1 }, "(S0!, object!)");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F6").TypeWithAnnotations, new byte[] { 0, 2, 0 }, new byte[] { 0, 2, 1 }, "(object?, S0!)");

            var source2 =
@"";

            // Without reference assembly.
            comp = CreateCompilation(source2, references: new[] { ref1 });
            globalNamespace = comp.GlobalNamespace;
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F1").TypeWithAnnotations, new byte[] { 1, 0, 2 }, new byte[] { 0, 0, 0 }, "C2<S0, object>");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F2").TypeWithAnnotations, new byte[] { 2, 1, 0 }, new byte[] { 0, 0, 0 }, "C2<object, S0>");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F3").TypeWithAnnotations, new byte[] { 0, 0, 1 }, new byte[] { 0, 0, 0 }, "S2<S0, object>");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F4").TypeWithAnnotations, new byte[] { 0, 2, 0 }, new byte[] { 0, 0, 0 }, "S2<object, S0>");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F5").TypeWithAnnotations, new byte[] { 0, 0, 1 }, new byte[] { 0, 0, 0 }, "(S0, object)");
            VerifyBytes(globalNamespace.GetMember<FieldSymbol>("Program.F6").TypeWithAnnotations, new byte[] { 0, 2, 0 }, new byte[] { 0, 0, 0 }, "(object, S0)");
        }

        private static readonly SymbolDisplayFormat _displayFormat = SymbolDisplayFormat.TestFormat.
            WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes).
            WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier);

        private static void VerifyBytes(TypeWithAnnotations type, byte[] expectedPreviously, byte[] expectedNow, string expectedDisplay)
        {
            var builder = ArrayBuilder<byte>.GetInstance();
            type.AddNullableTransforms(builder);
            var actualBytes = builder.ToImmutableAndFree();

            Assert.Equal(expectedNow, actualBytes);
            Assert.Equal(expectedDisplay, type.ToDisplayString(_displayFormat));

            var underlyingType = type.SetUnknownNullabilityForReferenceTypes();

            // Verify re-applying the same bytes gives the same result.
            TypeWithAnnotations updated;
            int position = 0;
            Assert.True(underlyingType.ApplyNullableTransforms(0, actualBytes, ref position, out updated));
            Assert.True(updated.Equals(type, TypeCompareKind.ConsiderEverything));

            // If the expected byte[] is shorter than earlier builds, verify that
            // applying the previous byte[] does not consume all bytes.
            if (!expectedPreviously.SequenceEqual(expectedNow))
            {
                position = 0;
                underlyingType.ApplyNullableTransforms(0, ImmutableArray.Create(expectedPreviously), ref position, out _);
                Assert.Equal(position, expectedNow.Length);
            }
        }

        [Fact]
        public void EmitAttribute_ValueTypes_09()
        {
            var source1 =
@"#nullable enable
public interface I
{
    void M1(int x);
    void M2(int[]? x);
    void M3(int x, object? y);
}";
            var comp = CreateCompilation(source1);
            var expected1 =
@"[NullableContext(2)] I
    void M1(System.Int32 x)
        System.Int32 x
    void M2(System.Int32[]? x)
        System.Int32[]? x
    void M3(System.Int32 x, System.Object? y)
        System.Int32 x
        System.Object? y
";
            AssertNullableAttributes(comp, expected1);
            var ref0 = comp.EmitToImageReference();

            var source2 =
@"#nullable enable
class C : I
{
    public void M1(int x) { }
    public void M2(int[]? x) { }
    public void M3(int x, object? y) { }
}";
            comp = CreateCompilation(source2, references: new[] { ref0 });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void EmitAttribute_ValueTypes_10()
        {
            var source1 =
@"#nullable enable
public class C<T> { }
public interface I1
{
    void M(int x, object? y, object? z);
}
public interface I2
{
    void M(C<int>? x, object? y, object? z);
}";
            var comp = CreateCompilation(source1);
            var expected1 =
@"C<T>
    [Nullable(2)] T
[NullableContext(2)] I1
    void M(System.Int32 x, System.Object? y, System.Object? z)
        System.Int32 x
        System.Object? y
        System.Object? z
[NullableContext(2)] I2
    void M(C<System.Int32>? x, System.Object? y, System.Object? z)
        C<System.Int32>? x
        System.Object? y
        System.Object? z
";
            AssertNullableAttributes(comp, expected1);
            var ref0 = comp.EmitToImageReference();

            var source2 =
@"#nullable enable
class C1 : I1
{
    public void M(int x, object? y, object? z) { }
}
class C2 : I2
{
    public void M(C<int>? x, object? y, object? z) { }
}";
            comp = CreateCompilation(source2, references: new[] { ref0 });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void EmitAttribute_ValueTypes_11()
        {
            var source1 =
@"#nullable enable
public interface I1<T>
{
    void M(T x, object? y, object? z);
}
public interface I2<T> where T : class
{
    void M(T? x, object? y, object? z);
}
public interface I3<T> where T : struct
{
    void M(T x, object? y, object? z);
}";
            var comp = CreateCompilation(source1);
            var expected1 =
@"[NullableContext(2)] I1<T>
    T
    void M(T x, System.Object? y, System.Object? z)
        [Nullable(1)] T x
        System.Object? y
        System.Object? z
[NullableContext(1)] I2<T> where T : class!
    T
    [NullableContext(2)] void M(T? x, System.Object? y, System.Object? z)
        T? x
        System.Object? y
        System.Object? z
I3<T> where T : struct
    [NullableContext(2)] void M(T x, System.Object? y, System.Object? z)
        [Nullable(0)] T x
        System.Object? y
        System.Object? z
";
            AssertNullableAttributes(comp, expected1);
            var ref0 = comp.EmitToImageReference();

            var source2 =
@"#nullable enable
class C1A<T> : I1<T> where T : struct
{
    public void M(T x, object? y, object? z) { }
}
class C1B : I1<int>
{
    public void M(int x, object? y, object? z) { }
}
class C2A<T> : I2<T> where T : class
{
    public void M(T? x, object? y, object? z) { }
}
class C2B : I2<string>
{
    public void M(string? x, object? y, object? z) { }
}
class C3A<T> : I3<T> where T : struct
{
    public void M(T x, object? y, object? z) { }
}
class C3B : I3<int>
{
    public void M(int x, object? y, object? z) { }
}";
            comp = CreateCompilation(source2, references: new[] { ref0 });
            comp.VerifyDiagnostics();
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
            var comp0 = CreateEmptyCompilation(source0);
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
            var comp = CreateEmptyCompilation(
                source,
                references: new[] { ref0 },
                parseOptions: TestOptions.Regular8);
            comp.VerifyEmitDiagnostics();
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
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
            comp.VerifyEmitDiagnostics(
                // (1,9): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                // class A<T>
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "T").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(1, 9),
                // (4,7): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                // class B : A<object?>
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(4, 7));
        }

        [Fact]
        public void ModuleMissingAttribute_Interface()
        {
            var source =
@"interface I<T>
{
}
class C : I<(object X, object? Y)>
{
}";
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
            comp.VerifyEmitDiagnostics(
                // (1,11): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableContextAttribute' is not defined or imported
                // interface I<T>
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "I").WithArguments("System.Runtime.CompilerServices.NullableContextAttribute").WithLocation(1, 11),
                // (1,13): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                // interface I<T>
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "T").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(1, 13),
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
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     object? F() => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 5),
                // (3,13): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableContextAttribute' is not defined or imported
                //     object? F() => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "F").WithArguments("System.Runtime.CompilerServices.NullableContextAttribute").WithLocation(3, 13));
        }

        [Fact]
        public void ModuleMissingAttribute_MethodParameters()
        {
            var source =
@"class C
{
    void F(object?[] c) { }
}";
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
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
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
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
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableContextAttribute' is not defined or imported
                // class C
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C").WithArguments("System.Runtime.CompilerServices.NullableContextAttribute").WithLocation(1, 7),
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
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableContextAttribute' is not defined or imported
                // class C
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C").WithArguments("System.Runtime.CompilerServices.NullableContextAttribute").WithLocation(1, 7),
                // (3,5): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     object this[object x, object? y] => throw new System.NotImplementedException();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 5),
                // (3,17): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     object this[object x, object? y] => throw new System.NotImplementedException();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object x").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 17),
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
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
            comp.VerifyEmitDiagnostics(
                // (3,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     public static object? operator+(C a, C b) => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 19),
                // (3,37): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     public static object? operator+(C a, C b) => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C a").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 37),
                // (3,42): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     public static object? operator+(C a, C b) => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C b").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 42)
                );
        }

        [Fact]
        public void ModuleMissingAttribute_OperatorParameters()
        {
            var source =
@"class C
{
    public static object operator+(C a, object?[] b) => a;
}";
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
            comp.VerifyEmitDiagnostics(
                // (3,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     public static object operator+(C a, object?[] b) => a;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 19),
                // (3,36): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     public static object operator+(C a, object?[] b) => a;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C a").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 36),
                // (3,41): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     public static object operator+(C a, object?[] b) => a;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?[] b").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(3, 41));
        }

        [Fact]
        public void ModuleMissingAttribute_DelegateReturnType()
        {
            var source =
@"delegate object? D();";
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
            comp.VerifyEmitDiagnostics(
                // (1,10): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                // delegate object? D();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object?").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(1, 10),
                // (1,18): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableContextAttribute' is not defined or imported
                // delegate object? D();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "D").WithArguments("System.Runtime.CompilerServices.NullableContextAttribute").WithLocation(1, 18));
        }

        [Fact]
        public void ModuleMissingAttribute_DelegateParameters()
        {
            var source =
@"delegate void D(object?[] o);";
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.ReleaseModule);
            // The lambda signature is emitted without a [Nullable] attribute because
            // the return type is inferred from flow analysis, not from initial binding.
            // As a result, there is no missing attribute warning.
            comp.VerifyEmitDiagnostics();
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
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
            comp.VerifyEmitDiagnostics(
                // (1,15): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableContextAttribute' is not defined or imported
                // delegate void D<T>(T t);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "D").WithArguments("System.Runtime.CompilerServices.NullableContextAttribute").WithLocation(1, 15),
                // (1,17): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                // delegate void D<T>(T t);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "T").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(1, 17),
                // (1,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                // delegate void D<T>(T t);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "T t").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(1, 20),
                // (4,17): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableContextAttribute' is not defined or imported
                //     static void F<T>(D<T> d)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "F").WithArguments("System.Runtime.CompilerServices.NullableContextAttribute").WithLocation(4, 17),
                // (4,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     static void F<T>(D<T> d)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "T").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(4, 19),
                // (4,22): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //     static void F<T>(D<T> d)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "D<T> d").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(4, 22),
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
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
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
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNonNullTypesTrue(TestOptions.ReleaseModule));
            comp.VerifyEmitDiagnostics(
                // (5,16): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //         void L(object? x, object y) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object? x").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(5, 16),
                // (5,27): error CS0518: Predefined type 'System.Runtime.CompilerServices.NullableAttribute' is not defined or imported
                //         void L(object? x, object y) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object y").WithArguments("System.Runtime.CompilerServices.NullableAttribute").WithLocation(5, 27));
        }

        [Fact]
        public void Tuples()
        {
            var source =
@"public class A
{
    public static ((object?, object) _1, object? _2, object _3, ((object?[], object), object?) _4) Nested;
    public static (object? _1, object _2, object? _3, object _4, object? _5, object _6, object? _7, object _8, object? _9) Long;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, validator: assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var typeDef = GetTypeDefinitionByName(reader, "A");
                var fieldDefs = typeDef.GetFields().Select(f => reader.GetFieldDefinition(f)).ToArray();

                // Nested tuple
                var field = fieldDefs.Single(f => reader.StringComparer.Equals(f.Name, "Nested"));
                var customAttributes = field.GetCustomAttributes();
                AssertAttributes(reader, customAttributes,
                    "MemberReference:Void System.Runtime.CompilerServices.TupleElementNamesAttribute..ctor(String[])",
                    "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])");
                var customAttribute = GetAttributeByConstructorName(reader, customAttributes, "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])");
                AssertEx.Equal(ImmutableArray.Create<byte>(0, 0, 2, 0, 2, 0, 0, 0, 0, 2, 0, 2), reader.ReadByteArray(customAttribute.Value));

                // Long tuple
                field = fieldDefs.Single(f => reader.StringComparer.Equals(f.Name, "Long"));
                customAttributes = field.GetCustomAttributes();
                AssertAttributes(reader, customAttributes,
                    "MemberReference:Void System.Runtime.CompilerServices.TupleElementNamesAttribute..ctor(String[])",
                    "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])");
                customAttribute = GetAttributeByConstructorName(reader, customAttributes, "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])");
                AssertEx.Equal(ImmutableArray.Create<byte>(0, 2, 0, 2, 0, 2, 0, 2, 0, 0, 2), reader.ReadByteArray(customAttribute.Value));
            });

            var source2 =
@"class B
{
    static void Main()
    {
        A.Nested._1.Item1.ToString(); // 1
        A.Nested._1.Item2.ToString();
        A.Nested._2.ToString(); // 2
        A.Nested._3.ToString();
        A.Nested._4.Item1.Item1.ToString();
        A.Nested._4.Item1.Item1[0].ToString(); // 3
        A.Nested._4.Item1.Item2.ToString();
        A.Nested._4.Item2.ToString(); // 4
        A.Long._1.ToString(); // 5
        A.Long._2.ToString();
        A.Long._3.ToString(); // 6
        A.Long._4.ToString();
        A.Long._5.ToString(); // 7
        A.Long._6.ToString();
        A.Long._7.ToString(); // 8
        A.Long._8.ToString();
        A.Long._9.ToString(); // 9
    }
}";
            var comp2 = CreateCompilation(new[] { source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics(
                // (5,9): warning CS8602: Dereference of a possibly null reference.
                //         A.Nested._1.Item1.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "A.Nested._1.Item1").WithLocation(5, 9),
                // (7,9): warning CS8602: Dereference of a possibly null reference.
                //         A.Nested._2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "A.Nested._2").WithLocation(7, 9),
                // (10,9): warning CS8602: Dereference of a possibly null reference.
                //         A.Nested._4.Item1.Item1[0].ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "A.Nested._4.Item1.Item1[0]").WithLocation(10, 9),
                // (12,9): warning CS8602: Dereference of a possibly null reference.
                //         A.Nested._4.Item2.ToString(); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "A.Nested._4.Item2").WithLocation(12, 9),
                // (13,9): warning CS8602: Dereference of a possibly null reference.
                //         A.Long._1.ToString(); // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "A.Long._1").WithLocation(13, 9),
                // (15,9): warning CS8602: Dereference of a possibly null reference.
                //         A.Long._3.ToString(); // 6
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "A.Long._3").WithLocation(15, 9),
                // (17,9): warning CS8602: Dereference of a possibly null reference.
                //         A.Long._5.ToString(); // 7
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "A.Long._5").WithLocation(17, 9),
                // (19,9): warning CS8602: Dereference of a possibly null reference.
                //         A.Long._7.ToString(); // 8
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "A.Long._7").WithLocation(19, 9),
                // (21,9): warning CS8602: Dereference of a possibly null reference.
                //         A.Long._9.ToString(); // 9
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "A.Long._9").WithLocation(21, 9));

            var type = comp2.GetMember<NamedTypeSymbol>("A");
            Assert.Equal(
                "((System.Object?, System.Object) _1, System.Object? _2, System.Object _3, ((System.Object?[], System.Object), System.Object?) _4)",
                type.GetMember<FieldSymbol>("Nested").TypeWithAnnotations.ToTestDisplayString());
            Assert.Equal(
                "(System.Object? _1, System.Object _2, System.Object? _3, System.Object _4, System.Object? _5, System.Object _6, System.Object? _7, System.Object _8, System.Object? _9)",
                type.GetMember<FieldSymbol>("Long").TypeWithAnnotations.ToTestDisplayString());
        }

        // DynamicAttribute and NullableAttribute formats should be aligned.
        [Fact]
        public void TuplesDynamic()
        {
            var source =
@"#pragma warning disable 0067
using System;
public interface I<T> { }
public class A<T> { }
public class B<T> :
    A<(object? _1, (object _2, object? _3), object _4, object? _5, object _6, object? _7, object _8, object? _9)>,
    I<(object? _1, (object _2, object? _3), object _4, object? _5, object _6, object? _7, object _8, object? _9)>
    where T : A<(object? _1, (object _2, object? _3), object _4, object? _5, object _6, object? _7, object _8, object? _9)>
{
    public (dynamic? _1, (object _2, dynamic? _3), object _4, dynamic? _5, object _6, dynamic? _7, object _8, dynamic? _9) Field;
    public event EventHandler<(dynamic? _1, (object _2, dynamic? _3), object _4, dynamic? _5, object _6, dynamic? _7, object _8, dynamic? _9)> Event;
    public (dynamic? _1, (object _2, dynamic? _3), object _4, dynamic? _5, object _6, dynamic? _7, object _8, dynamic? _9) Method(
        (dynamic? _1, (object _2, dynamic? _3), object _4, dynamic? _5, object _6, dynamic? _7, object _8, dynamic? _9) arg) => arg;
    public (dynamic? _1, (object _2, dynamic? _3), object _4, dynamic? _5, object _6, dynamic? _7, object _8, dynamic? _9) Property { get; set; }
}";
            var comp = CreateCompilation(new[] { source }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, validator: assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var typeDef = GetTypeDefinitionByName(reader, "B`1");
                // Base type
                checkAttributesNoDynamic(typeDef.GetCustomAttributes(), addOne: 0); // add one for A<T>
                // Interface implementation
                var interfaceImpl = reader.GetInterfaceImplementation(typeDef.GetInterfaceImplementations().Single());
                checkAttributesNoDynamic(interfaceImpl.GetCustomAttributes(), addOne: 0); // add one for I<T>
                // Type parameter constraint type
                var typeParameter = reader.GetGenericParameter(typeDef.GetGenericParameters()[0]);
                var constraint = reader.GetGenericParameterConstraint(typeParameter.GetConstraints()[0]);
                checkAttributesNoDynamic(constraint.GetCustomAttributes(), addOne: 1); // add one for A<T>
                // Field type
                var field = typeDef.GetFields().Select(f => reader.GetFieldDefinition(f)).Single(f => reader.StringComparer.Equals(f.Name, "Field"));
                checkAttributes(field.GetCustomAttributes());
                // Event type
                var @event = typeDef.GetEvents().Select(e => reader.GetEventDefinition(e)).Single(e => reader.StringComparer.Equals(e.Name, "Event"));
                checkAttributes(@event.GetCustomAttributes(), addOne: 1); // add one for EventHandler<T>
                // Method return type and parameter type
                var method = typeDef.GetMethods().Select(m => reader.GetMethodDefinition(m)).Single(m => reader.StringComparer.Equals(m.Name, "Method"));
                var parameters = method.GetParameters().Select(p => reader.GetParameter(p)).ToArray();
                checkAttributes(parameters[0].GetCustomAttributes()); // return type
                checkAttributes(parameters[1].GetCustomAttributes()); // parameter
                // Property type
                var property = typeDef.GetProperties().Select(p => reader.GetPropertyDefinition(p)).Single(p => reader.StringComparer.Equals(p.Name, "Property"));
                checkAttributes(property.GetCustomAttributes());

                void checkAttributes(CustomAttributeHandleCollection customAttributes, byte? addOne = null)
                {
                    AssertAttributes(reader, customAttributes,
                        "MemberReference:Void System.Runtime.CompilerServices.DynamicAttribute..ctor(Boolean[])",
                        "MemberReference:Void System.Runtime.CompilerServices.TupleElementNamesAttribute..ctor(String[])",
                        "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])");
                    checkNullableAttribute(customAttributes, addOne);
                }

                void checkAttributesNoDynamic(CustomAttributeHandleCollection customAttributes, byte? addOne = null)
                {
                    AssertAttributes(reader, customAttributes,
                        "MemberReference:Void System.Runtime.CompilerServices.TupleElementNamesAttribute..ctor(String[])",
                        "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])");
                    checkNullableAttribute(customAttributes, addOne);
                }

                void checkNullableAttribute(CustomAttributeHandleCollection customAttributes, byte? addOne)
                {
                    var customAttribute = GetAttributeByConstructorName(reader, customAttributes, "MethodDefinition:Void System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])");
                    var expectedBits = ImmutableArray.Create<byte>(0, 2, 0, 1, 2, 1, 2, 1, 2, 1, 0, 2);
                    if (addOne.HasValue)
                    {
                        expectedBits = ImmutableArray.Create(addOne.GetValueOrDefault()).Concat(expectedBits);
                    }
                    AssertEx.Equal(expectedBits, reader.ReadByteArray(customAttribute.Value));
                }
            });

            var source2 =
@"class C
{
    static void F(B<A<(object?, (object, object?), object, object?, object, object?, object, object?)>> b)
    {
        b.Field._8.ToString();
        b.Field._9.ToString(); // 1
        b.Method(default)._8.ToString();
        b.Method(default)._9.ToString(); // 2
        b.Property._8.ToString();
        b.Property._9.ToString(); // 3
    }
}";
            var comp2 = CreateCompilation(new[] { source2 }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics(
                // (6,9): warning CS8602: Dereference of a possibly null reference.
                //         b.Field._9.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Field._9").WithLocation(6, 9),
                // (8,9): warning CS8602: Dereference of a possibly null reference.
                //         b.Method(default)._9.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Method(default)._9").WithLocation(8, 9),
                // (10,9): warning CS8602: Dereference of a possibly null reference.
                //         b.Property._9.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Property._9").WithLocation(10, 9));

            var type = comp2.GetMember<NamedTypeSymbol>("B");
            Assert.Equal(
                "A<(System.Object? _1, (System.Object _2, System.Object? _3), System.Object _4, System.Object? _5, System.Object _6, System.Object? _7, System.Object _8, System.Object? _9)>",
                type.BaseTypeNoUseSiteDiagnostics.ToTestDisplayString());
            Assert.Equal(
                "I<(System.Object? _1, (System.Object _2, System.Object? _3), System.Object _4, System.Object? _5, System.Object _6, System.Object? _7, System.Object _8, System.Object? _9)>",
                type.Interfaces()[0].ToTestDisplayString());
            Assert.Equal(
                "A<(System.Object? _1, (System.Object _2, System.Object? _3), System.Object _4, System.Object? _5, System.Object _6, System.Object? _7, System.Object _8, System.Object? _9)>",
                type.TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0].ToTestDisplayString());
            Assert.Equal(
                "(dynamic? _1, (System.Object _2, dynamic? _3), System.Object _4, dynamic? _5, System.Object _6, dynamic? _7, System.Object _8, dynamic? _9)",
                type.GetMember<FieldSymbol>("Field").TypeWithAnnotations.ToTestDisplayString());
            Assert.Equal(
                "System.EventHandler<(dynamic? _1, (System.Object _2, dynamic? _3), System.Object _4, dynamic? _5, System.Object _6, dynamic? _7, System.Object _8, dynamic? _9)>",
                type.GetMember<EventSymbol>("Event").TypeWithAnnotations.ToTestDisplayString());
            Assert.Equal(
                "(dynamic? _1, (System.Object _2, dynamic? _3), System.Object _4, dynamic? _5, System.Object _6, dynamic? _7, System.Object _8, dynamic? _9) B<T>.Method((dynamic? _1, (System.Object _2, dynamic? _3), System.Object _4, dynamic? _5, System.Object _6, dynamic? _7, System.Object _8, dynamic? _9) arg)",
                type.GetMember<MethodSymbol>("Method").ToTestDisplayString());
            Assert.Equal(
                "(dynamic? _1, (System.Object _2, dynamic? _3), System.Object _4, dynamic? _5, System.Object _6, dynamic? _7, System.Object _8, dynamic? _9) B<T>.Property { get; set; }",
                type.GetMember<PropertySymbol>("Property").ToTestDisplayString());
        }

        [Fact]
        [WorkItem(36934, "https://github.com/dotnet/roslyn/issues/36934")]
        public void AttributeUsage()
        {
            var source =
@"#nullable enable
public class Program
{
    public object? F;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var attributeType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.NullableAttribute");
                AttributeUsageInfo attributeUsage = attributeType.GetAttributeUsageInfo();
                Assert.False(attributeUsage.Inherited);
                Assert.False(attributeUsage.AllowMultiple);
                Assert.True(attributeUsage.HasValidAttributeTargets);
                var expectedTargets = AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.GenericParameter | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue;
                Assert.Equal(expectedTargets, attributeUsage.ValidTargets);
            });
        }

        [Fact]
        public void NullableFlags_Field_Exists()
        {
            var source =
@"public class C
{
    public void F(object? x, object y, object z) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                var method = (MethodSymbol)type.GetMembers("F").Single();
                var attributes = method.Parameters[0].GetAttributes();
                AssertNullableAttribute(attributes);

                var nullable = GetNullableAttribute(attributes);

                var field = nullable.AttributeClass.GetField("NullableFlags");
                Assert.NotNull(field);
                Assert.Equal("System.Byte[]", field.TypeWithAnnotations.ToTestDisplayString());
            });
        }

        [Fact]
        public void NullableFlags_Field_Contains_ConstructorArguments_SingleByteConstructor()
        {
            var source =
@"
#nullable enable
using System;
using System.Linq;
public class C
{
    public void F(object? x, object y, object z) { }

    public static void Main()
    {
        var attribute = typeof(C).GetMethod(""F"").GetParameters()[0].GetCustomAttributes(true).Single(a => a.GetType().Name == ""NullableAttribute"");
        var field = attribute.GetType().GetField(""NullableFlags"");
        byte[] flags = (byte[])field.GetValue(attribute);

        Console.Write($""{{ {string.Join("","", flags)} }}"");
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "{ 2 }", symbolValidator: module =>
            {
                var expected =
@"C
    [NullableContext(1)] void F(System.Object? x, System.Object! y, System.Object! z)
        [Nullable(2)] System.Object? x
        System.Object! y
        System.Object! z
";
                AssertNullableAttributes(module, expected);
            });
        }

        [Fact]
        public void NullableFlags_Field_Contains_ConstructorArguments_ByteArrayConstructor()
        {
            var source =
@"
#nullable enable
using System;
using System.Linq;
public class C
{
    public void F(Action<object?, Action<object, object?>?> c) { }

    public static void Main()
    {
        var attribute = typeof(C).GetMethod(""F"").GetParameters()[0].GetCustomAttributes(true).Single(a => a.GetType().Name == ""NullableAttribute"");
        var field = attribute.GetType().GetField(""NullableFlags"");
        byte[] flags = (byte[])field.GetValue(attribute);

        System.Console.Write($""{{ {string.Join("","", flags)} }}"");
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "{ 1,2,2,1,2 }", symbolValidator: module =>
            {
                var expected =
@"C
    void F(System.Action<System.Object?, System.Action<System.Object!, System.Object?>?>! c)
        [Nullable({ 1, 2, 2, 1, 2 })] System.Action<System.Object?, System.Action<System.Object!, System.Object?>?>! c
";
                AssertNullableAttributes(module, expected);
            });
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

        private static void AssertNoNullableAttributes(CSharpCompilation comp)
        {
            var image = comp.EmitToArray();
            using (var reader = new PEReader(image))
            {
                var metadataReader = reader.GetMetadataReader();
                var attributes = metadataReader.GetCustomAttributeRows().Select(metadataReader.GetCustomAttributeName).ToArray();
                Assert.False(attributes.Contains("NullableContextAttribute"));
                Assert.False(attributes.Contains("NullableAttribute"));
            }
        }

        private static CSharpAttributeData GetNullableAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            return attributes.Single(a => a.AttributeClass.ToTestDisplayString() == "System.Runtime.CompilerServices.NullableAttribute");
        }

        private static TypeDefinition GetTypeDefinitionByName(MetadataReader reader, string name)
        {
            return reader.GetTypeDefinition(reader.TypeDefinitions.Single(h => reader.StringComparer.Equals(reader.GetTypeDefinition(h).Name, name)));
        }

        private static string GetAttributeConstructorName(MetadataReader reader, CustomAttributeHandle handle)
        {
            return reader.Dump(reader.GetCustomAttribute(handle).Constructor);
        }

        private static CustomAttribute GetAttributeByConstructorName(MetadataReader reader, CustomAttributeHandleCollection handles, string name)
        {
            return reader.GetCustomAttribute(handles.FirstOrDefault(h => GetAttributeConstructorName(reader, h) == name));
        }

        private static void AssertAttributes(MetadataReader reader, CustomAttributeHandleCollection handles, params string[] expectedNames)
        {
            var actualNames = handles.Select(h => GetAttributeConstructorName(reader, h)).ToArray();
            AssertEx.SetEqual(actualNames, expectedNames);
        }

        private void AssertNullableAttributes(CSharpCompilation comp, string expected)
        {
            CompileAndVerify(comp, symbolValidator: module => AssertNullableAttributes(module, expected));
        }

        private static void AssertNullableAttributes(ModuleSymbol module, string expected)
        {
            var actual = NullableAttributesVisitor.GetString((PEModuleSymbol)module);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual);
        }
    }
}
