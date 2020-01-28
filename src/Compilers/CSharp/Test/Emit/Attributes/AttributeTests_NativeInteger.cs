// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_NativeInteger : CSharpTestBase
    {
        private static readonly SymbolDisplayFormat FormatWithSpecialTypes = SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        [Fact]
        public void EmptyProject()
        {
            var source = @"";
            var comp = CreateCompilation(source);
            var expected =
@"";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void ExplicitAttribute_FromSource()
        {
            var source =
@"public class Program
{
    public nint F1;
    public nuint[] F2;
}";
            var comp = CreateCompilation(new[] { NativeIntegerAttributeDefinition, source }, parseOptions: TestOptions.RegularPreview);
            var expected =
@"Program
    [NativeInteger] System.IntPtr F1
    [NativeInteger({ False, True })] System.UIntPtr[] F2
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var attributeType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.NativeIntegerAttribute");
                Assert.NotNull(attributeType);
                AssertNativeIntegerAttributes(module, expected);
            });
        }

        [Fact]
        public void ExplicitAttribute_FromMetadata()
        {
            var comp = CreateCompilation(NativeIntegerAttributeDefinition);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source =
@"public class Program
{
    public nint F1;
    public nuint[] F2;
}";
            comp = CreateCompilation(source, references: new[] { ref0 }, parseOptions: TestOptions.RegularPreview);
            var expected =
@"Program
    [NativeInteger] System.IntPtr F1
    [NativeInteger({ False, True })] System.UIntPtr[] F2
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var attributeType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.NativeIntegerAttribute");
                Assert.Null(attributeType);
                AssertNativeIntegerAttributes(module, expected);
            });
        }

        [Fact]
        public void ExplicitAttribute_MissingEmptyConstructor()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NativeIntegerAttribute : Attribute
    {
        public NativeIntegerAttribute(bool[] flags) { }
    }
}";
            var source2 =
@"public class Program
{
    public nint F1;
    public nuint[] F2;
}";
            var comp = CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //     public nint F1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F1").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(3, 17),
                // (4,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //     public nuint[] F2;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F2").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(4, 20));
        }

        [Fact]
        public void ExplicitAttribute_MissingConstructor()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NativeIntegerAttribute : Attribute
    {
    }
}";
            var source2 =
@"public class Program
{
    public nint F1;
    public nuint[] F2;
}";
            var comp = CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //     public nint F1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F1").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(3, 17),
                // (4,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //     public nuint[] F2;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F2").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(4, 20));
        }

        [Fact]
        public void ExplicitAttribute_ReferencedInSource()
        {
            var sourceAttribute =
@"namespace System.Runtime.CompilerServices
{
    internal class NativeIntegerAttribute : Attribute
    {
        internal NativeIntegerAttribute() { }
        internal NativeIntegerAttribute(bool[] flags) { }
    }
}";
            var source =
@"#pragma warning disable 67
#pragma warning disable 169
using System;
using System.Runtime.CompilerServices;
[NativeInteger] class Program
{
    [NativeInteger] IntPtr F;
    [NativeInteger] event EventHandler E;
    [NativeInteger] object P { get; }
    [NativeInteger(new[] { false, true })] static UIntPtr[] M1() => throw null;
    [return: NativeInteger(new[] { false, true })] static UIntPtr[] M2() => throw null;
    static void M3([NativeInteger]object arg) { }
}";

            var comp = CreateCompilation(new[] { sourceAttribute, source }, parseOptions: TestOptions.Regular7);
            verifyDiagnostics(comp);

            comp = CreateCompilation(new[] { sourceAttribute, source }, parseOptions: TestOptions.RegularPreview);
            verifyDiagnostics(comp);

            static void verifyDiagnostics(CSharpCompilation comp)
            {
                comp.VerifyDiagnostics(
                    // (5,2): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    // [NativeInteger] class Program
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(5, 2),
                    // (7,6): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    //     [NativeInteger] IntPtr F;
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(7, 6),
                    // (8,6): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    //     [NativeInteger] event EventHandler E;
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(8, 6),
                    // (9,6): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    //     [NativeInteger] object P { get; }
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(9, 6),
                    // (11,14): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    //     [return: NativeInteger(new[] { false, true })] static UIntPtr[] M2() => throw null;
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger(new[] { false, true })").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(11, 14),
                    // (12,21): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    //     static void M3([NativeInteger]object arg) { }
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(12, 21));
            }
        }

        [Fact]
        public void MissingAttributeUsageAttribute()
        {
            var source =
@"public class Program
{
    public nint F1;
    public nuint[] F2;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.MakeTypeMissing(WellKnownType.System_AttributeUsageAttribute);
            comp.VerifyEmitDiagnostics(
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1));
        }

        [Fact]
        public void EmitAttribute_BaseClass()
        {
            var source =
@"public class A<T, U>
{
}
public class B : A<nint, nuint[]>
{
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expected =
@"[NativeInteger({ False, True, False, True })] B
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_Interface()
        {
            var source =
@"public interface I<T>
{
}
public class A : I<(nint, nuint[])>
{
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, validator: assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var typeDef = GetTypeDefinitionByName(reader, "A");
                var interfaceImpl = reader.GetInterfaceImplementation(typeDef.GetInterfaceImplementations().Single());
                AssertAttributes(reader, interfaceImpl.GetCustomAttributes(), "MethodDefinition:Void System.Runtime.CompilerServices.NativeIntegerAttribute..ctor(Boolean[])");
            });
        }

        [Fact]
        public void EmitAttribute_Fields()
        {
            var source =
@"public class Program
{
    public nint F1;
    public nuint[] F2;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expected =
@"Program
    [NativeInteger] System.IntPtr F1
    [NativeInteger({ False, True })] System.UIntPtr[] F2
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_MethodReturnType()
        {
            var source =
@"public class Program
{
    public nuint[] F() => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expected =
@"Program
    [NativeInteger({ False, True })] System.UIntPtr[] F()
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_MethodParameters()
        {
            var source =
@"public class Program
{
    public void F(nint x, nuint y) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expected =
@"Program
    void F(System.IntPtr x, System.UIntPtr y)
        [NativeInteger] System.IntPtr x
        [NativeInteger] System.UIntPtr y
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_PropertyType()
        {
            var source =
@"public class Program
{
    public nuint[] P => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expected =
@"Program
    [NativeInteger({ False, True })] System.UIntPtr[] P { get; }
        [NativeInteger({ False, True })] System.UIntPtr[] P.get
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_PropertyParameters()
        {
            var source =
@"public class Program
{
    public object this[nint x, nuint[] y] => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expected =
@"Program
    System.Object this[System.IntPtr x, System.UIntPtr[] y] { get; }
        [NativeInteger] System.IntPtr x
        [NativeInteger({ False, True })] System.UIntPtr[] y
        System.Object this[System.IntPtr x, System.UIntPtr[] y].get
            [NativeInteger] System.IntPtr x
            [NativeInteger({ False, True })] System.UIntPtr[] y
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_EventType()
        {
            var source =
@"using System;
public class Program
{
    public event EventHandler<nuint[]> E;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expected =
@"Program
    [NativeInteger({ False, False, True })] event System.EventHandler<System.UIntPtr[]> E
        void E.add
            [NativeInteger({ False, False, True })] System.EventHandler<System.UIntPtr[]> value
        void E.remove
            [NativeInteger({ False, False, True })] System.EventHandler<System.UIntPtr[]> value
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_OperatorReturnType()
        {
            var source =
@"public class C
{
    public static nint operator+(C a, C b) => 0;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expected =
@"C
    [NativeInteger] System.IntPtr operator +(C a, C b)
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_OperatorParameters()
        {
            var source =
@"public class C
{
    public static C operator+(C a, nuint[] b) => a;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expected =
@"C
    C operator +(C a, System.UIntPtr[] b)
        [NativeInteger({ False, True })] System.UIntPtr[] b
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_DelegateReturnType()
        {
            var source =
@"public delegate nint D();";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expected =
@"D
    [NativeInteger] System.IntPtr Invoke()
    [NativeInteger] System.IntPtr EndInvoke(System.IAsyncResult result)
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_DelegateParameters()
        {
            var source =
@"public delegate void D(nint x, nuint[] y);";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expected =
@"D
    void Invoke(System.IntPtr x, System.UIntPtr[] y)
        [NativeInteger] System.IntPtr x
        [NativeInteger({ False, True })] System.UIntPtr[] y
    System.IAsyncResult BeginInvoke(System.IntPtr x, System.UIntPtr[] y, System.AsyncCallback callback, System.Object @object)
        [NativeInteger] System.IntPtr x
        [NativeInteger({ False, True })] System.UIntPtr[] y
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_Constraint()
        {
            var source =
@"public class A<T>
{
}
public class B<T> where T : A<nint>
{
}
public class C<T> where T : A<nuint[]>
{
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var type = comp.GetMember<NamedTypeSymbol>("B");
            Assert.Equal("A<nint>", getConstraintType(type).ToDisplayString(FormatWithSpecialTypes));
            type = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("A<nuint[]>", getConstraintType(type).ToDisplayString(FormatWithSpecialTypes));

            static TypeWithAnnotations getConstraintType(NamedTypeSymbol type) => type.TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0];
        }

        [Fact]
        public void EmitAttribute_LambdaReturnType()
        {
            var source =
@"using System;
class Program
{
    static object M()
    {
        Func<nint> f = () => (nint)2;
        return f();
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            AssertNoNativeIntegerAttributes(comp);
        }

        [Fact]
        public void EmitAttribute_LambdaParameters()
        {
            var source =
@"using System;
class Program
{
    static void M()
    {
        Action<nuint[]> a = (nuint[] n) => { };
        a(null);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            AssertNoNativeIntegerAttributes(comp);
        }

        [Fact]
        public void EmitAttribute_LocalFunctionReturnType()
        {
            var source =
@"using System;
class Program
{
    static object M()
    {
        nint L() => (nint)2;
        return L();
    }
}";
            CompileAndVerify(
                source,
                parseOptions: TestOptions.RegularPreview,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var method = module.ContainingAssembly.GetTypeByMetadataName("Program").GetMethod("<M>g__L|0_0");
                    AssertNativeIntegerAttribute(method.GetReturnTypeAttributes());
                    AssertAttributes(method.GetAttributes(), "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
                });
        }

        [Fact]
        public void EmitAttribute_LocalFunctionParameters()
        {
            var source =
@"using System;
class Program
{
    static void M()
    {
        void L(nuint[] n) { }
        L(null);
    }
}";
            CompileAndVerify(
                source,
                parseOptions: TestOptions.RegularPreview,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var method = module.ContainingAssembly.GetTypeByMetadataName("Program").GetMethod("<M>g__L|0_0");
                    AssertNativeIntegerAttribute(method.Parameters[0].GetAttributes());
                });
        }

        [Fact]
        public void EmitAttribute_Nested()
        {
            var source =
@"public class A<T>
{
    public class B<U> { }
}
unsafe public class Program
{
    public nint F1;
    public nuint[] F2;
    public nint* F3;
    public A<nint>.B<nuint> F4;
    public (nint, nuint) F5;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseDll);
            var expected =
@"Program
    [NativeInteger] System.IntPtr F1
    [NativeInteger({ False, True })] System.UIntPtr[] F2
    [NativeInteger({ False, True })] System.IntPtr* F3
    [NativeInteger({ False, True, True })] A<System.IntPtr>.B<System.UIntPtr> F4
    [NativeInteger({ False, True, True })] (System.IntPtr, System.UIntPtr) F5
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void AttributeUsage()
        {
            var source =
@"public class Program
{
    public nint F;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var attributeType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.NativeIntegerAttribute");
                AttributeUsageInfo attributeUsage = attributeType.GetAttributeUsageInfo();
                Assert.False(attributeUsage.Inherited);
                Assert.False(attributeUsage.AllowMultiple);
                Assert.True(attributeUsage.HasValidAttributeTargets);
                var expectedTargets =
                    AttributeTargets.Class |
                    AttributeTargets.Event |
                    AttributeTargets.Field |
                    AttributeTargets.GenericParameter |
                    AttributeTargets.Parameter |
                    AttributeTargets.Property |
                    AttributeTargets.ReturnValue;
                Assert.Equal(expectedTargets, attributeUsage.ValidTargets);
            });
        }

        [Fact]
        public void AttributeFieldExists()
        {
            var source =
@"public class Program
{
    public nint F;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Program");
                var member = type.GetMembers("F").Single();
                var attributes = member.GetAttributes();
                AssertNativeIntegerAttribute(attributes);
                var attribute = GetNativeIntegerAttribute(attributes);
                var field = attribute.AttributeClass.GetField("TransformFlags");
                Assert.Equal("System.Boolean[]", field.TypeWithAnnotations.ToTestDisplayString());
            });
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

        private static void AssertNoNativeIntegerAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            AssertAttributes(attributes);
        }

        private static void AssertNativeIntegerAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            AssertAttributes(attributes, "System.Runtime.CompilerServices.NativeIntegerAttribute");
        }

        private static void AssertAttributes(ImmutableArray<CSharpAttributeData> attributes, params string[] expectedNames)
        {
            var actualNames = attributes.Select(a => a.AttributeClass.ToTestDisplayString()).ToArray();
            AssertEx.SetEqual(actualNames, expectedNames);
        }

        private static void AssertNoNativeIntegerAttributes(CSharpCompilation comp)
        {
            var image = comp.EmitToArray();
            using (var reader = new PEReader(image))
            {
                var metadataReader = reader.GetMetadataReader();
                var attributes = metadataReader.GetCustomAttributeRows().Select(metadataReader.GetCustomAttributeName).ToArray();
                Assert.False(attributes.Contains("NativeIntegerAttribute"));
            }
        }

        private void AssertNativeIntegerAttributes(CSharpCompilation comp, string expected)
        {
            CompileAndVerify(comp, symbolValidator: module => AssertNativeIntegerAttributes(module, expected));
        }

        private static void AssertNativeIntegerAttributes(ModuleSymbol module, string expected)
        {
            var actual = NativeIntegerAttributesVisitor.GetString((PEModuleSymbol)module);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual);
        }

        private static CSharpAttributeData GetNativeIntegerAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            return attributes.Single(a => a.AttributeClass.ToTestDisplayString() == "System.Runtime.CompilerServices.NativeIntegerAttribute");
        }
    }
}
