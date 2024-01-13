// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_NullablePublicOnly : CSharpTestBase
    {
        [Fact]
        public void ExplicitAttribute_FromSource()
        {
            var source =
@"public class A<T>
{
}
public class B : A<object?>
{
}";
            var options = WithNullableEnable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;
            CSharpTestSource sources = new[] { NullablePublicOnlyAttributeDefinition, source };

            var comp = CreateCompilation(sources, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: m => AssertNullablePublicOnlyAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, publicDefinition: true));

            comp = CreateCompilation(sources, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: m => AssertNullablePublicOnlyAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, publicDefinition: true));
        }

        [Fact]
        public void ExplicitAttribute_FromMetadata()
        {
            var source =
@"public class A<T>
{
}
public class B : A<object?>
{
}";
            var comp = CreateCompilation(NullablePublicOnlyAttributeDefinition, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics();
            var ref1 = comp.EmitToImageReference();

            var options = WithNullableEnable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            comp = CreateCompilation(source, references: new[] { ref1 }, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: m => AssertNullablePublicOnlyAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: true));

            comp = CreateCompilation(source, references: new[] { ref1 }, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: m => AssertNullablePublicOnlyAttribute(m, includesAttributeDefinition: false, includesAttributeUse: true, publicDefinition: true));
        }

        [Fact]
        public void ExplicitAttribute_MissingSingleValueConstructor()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullablePublicOnlyAttribute : Attribute
    {
    }
}";
            var source2 =
@"public class A<T>
{
}
public class B : A<object?>
{
}";
            var options = WithNullableEnable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(new[] { source1, source2 }, options: options, parseOptions: parseOptions);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(new[] { source1, source2 }, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            comp.VerifyEmitDiagnostics(
                // error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullablePublicOnlyAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Runtime.CompilerServices.NullablePublicOnlyAttribute", ".ctor").WithLocation(1, 1));
        }

        [Fact]
        public void EmptyProject()
        {
            var source = @"";
            var options = WithNullableEnable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);
        }

        [Fact]
        public void CSharp7_NoAttribute()
        {
            var source =
@"public class A<T>
{
}
public class B : A<object>
{
}";
            var options = TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular7;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);
        }

        [Fact]
        public void NullableEnabled()
        {
            var source =
@"public class A<T>
{
}
public class B : A<object?>
{
}";
            var options = WithNullableEnable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: AssertNullablePublicOnlyAttribute);
        }

        [Fact]
        public void NullableEnabled_NoPublicMembers()
        {
            var source =
@"class A<T>
{
}
class B : A<object?>
{
}";
            var options = WithNullableEnable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);
        }

        [Fact]
        public void NullableDisabled()
        {
            var source =
@"public class A<T>
{
}
public class B : A<object>
{
}";
            var options = WithNullableDisable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);
        }

        [Fact]
        public void NullableDisabled_Annotation()
        {
            var source =
@"public class A<T>
{
}
public class B : A<object?>
{
}";
            var options = WithNullableDisable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: AssertNullablePublicOnlyAttribute);
        }

        [Fact]
        public void NullableDisabled_OtherNullableAttributeDefinitions()
        {
            var source =
@"public class Program
{
}";
            var options = WithNullableDisable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;
            CSharpTestSource sources = new[] { NullableAttributeDefinition, NullableContextAttributeDefinition, source };

            var comp = CreateCompilation(sources, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(sources, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);
        }

        [Fact]
        public void NullableEnabled_MethodBodyOnly()
        {
            var source0 =
@"#nullable enable
public class A
{
    public static object? F;
    public static void M(object o) { }
}";
            var comp = CreateCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"public class B
{
    static void Main()
    {
#nullable enable
        A.M(A.F);
    }
}";
            comp = CreateCompilation(
                source1,
                references: new[] { ref0 },
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
                parseOptions: TestOptions.Regular8.WithNullablePublicOnly());
            comp.VerifyDiagnostics(
                // (6,13): warning CS8604: Possible null reference argument for parameter 'o' in 'void A.M(object o)'.
                //         A.M(A.F);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "A.F").WithArguments("o", "void A.M(object o)").WithLocation(6, 13));
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);
        }

        [Fact]
        public void NullableEnabled_AllNullableAttributeDefinitions_01()
        {
            var source =
@"#nullable enable
public class Program
{
}";
            var options = WithNullableDisable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;
            CSharpTestSource sources = new[] { NullableAttributeDefinition, NullableContextAttributeDefinition, NullablePublicOnlyAttributeDefinition, source };

            var comp = CreateCompilation(sources, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: m => AssertNullablePublicOnlyAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, publicDefinition: true));

            comp = CreateCompilation(sources, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: m => AssertNullablePublicOnlyAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, publicDefinition: true));
        }

        [Fact]
        public void NullableEnabled_AllNullableAttributeDefinitions_02()
        {
            var source =
@"#nullable enable
public class Program
{
    public object F() => null!;
}";
            var options = WithNullableDisable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;
            CSharpTestSource sources = new[] { NullableAttributeDefinition, NullableContextAttributeDefinition, NullablePublicOnlyAttributeDefinition, source };

            var comp = CreateCompilation(sources, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: m => AssertNullablePublicOnlyAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, publicDefinition: true));

            comp = CreateCompilation(sources, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: m => AssertNullablePublicOnlyAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, publicDefinition: true));
        }

        [Fact]
        public void NullableEnabled_OtherNullableAttributeDefinitions_01()
        {
            var source =
@"#nullable enable
public class Program
{
}";
            var options = WithNullableDisable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;
            CSharpTestSource sources = new[] { NullableAttributeDefinition, NullableContextAttributeDefinition, source };

            var comp = CreateCompilation(sources, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(sources, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);
        }

        [Fact]
        public void NullableEnabled_OtherNullableAttributeDefinitions_02()
        {
            var source =
@"#nullable enable
public class Program
{
    public object F() => null!;
}";
            var options = WithNullableDisable().WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;
            CSharpTestSource sources = new[] { NullableAttributeDefinition, NullableContextAttributeDefinition, source };

            var comp = CreateCompilation(sources, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(sources, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: AssertNullablePublicOnlyAttribute);
        }

        [Fact]
        public void LocalFunctionReturnType_SynthesizedAttributeDefinitions()
        {
            var source =
@"public class Program
{
    static void Main()
    {
#nullable enable
        object? L() => null;
        L();
    }
}";
            var options = TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;

            var comp = CreateCompilation(source, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);

            comp = CreateCompilation(source, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: AssertNoNullablePublicOnlyAttribute);
        }

        [Fact]
        public void LocalFunctionReturnType_ExplicitAttributeDefinitions()
        {
            var source =
@"public class Program
{
    static void Main()
    {
#nullable enable
        object? L() => null;
        L();
    }
}";
            var options = TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All);
            var parseOptions = TestOptions.Regular8;
            CSharpTestSource sources = new[] { NullableAttributeDefinition, NullableContextAttributeDefinition, NullablePublicOnlyAttributeDefinition, source };

            var comp = CreateCompilation(sources, options: options, parseOptions: parseOptions);
            CompileAndVerify(comp, symbolValidator: m => AssertNullablePublicOnlyAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, publicDefinition: true));

            comp = CreateCompilation(sources, options: options, parseOptions: parseOptions.WithNullablePublicOnly());
            CompileAndVerify(comp, symbolValidator: m => AssertNullablePublicOnlyAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, publicDefinition: true));
        }

        [Fact]
        [WorkItem(36457, "https://github.com/dotnet/roslyn/issues/36457")]
        public void ExplicitAttribute_ReferencedInSource_Assembly()
        {
            var sourceAttribute =
@"namespace System.Runtime.CompilerServices
{
    internal class NullablePublicOnlyAttribute : System.Attribute
    {
        internal NullablePublicOnlyAttribute(bool b) { }
    }
}";
            var source =
@"using System.Runtime.CompilerServices;
[assembly: NullablePublicOnly(false)]
[module: NullablePublicOnly(false)]
";

            // C#7
            var comp = CreateCompilation(new[] { sourceAttribute, source }, parseOptions: TestOptions.Regular7);
            verifyDiagnostics(comp);

            // C#8
            comp = CreateCompilation(new[] { sourceAttribute, source });
            verifyDiagnostics(comp);

            static void verifyDiagnostics(CSharpCompilation comp)
            {
                comp.VerifyDiagnostics(
                // (3,10): error CS8335: Do not use 'System.Runtime.CompilerServices.NullablePublicOnlyAttribute'. This is reserved for compiler usage.
                // [module: NullablePublicOnly(false)]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NullablePublicOnly(false)").WithArguments("System.Runtime.CompilerServices.NullablePublicOnlyAttribute").WithLocation(3, 10));
            }
        }

        [Fact]
        [WorkItem(36457, "https://github.com/dotnet/roslyn/issues/36457")]
        public void ExplicitAttribute_ReferencedInSource_Other()
        {
            var sourceAttribute =
@"namespace System.Runtime.CompilerServices
{
    internal class NullablePublicOnlyAttribute : System.Attribute
    {
        internal NullablePublicOnlyAttribute(bool b) { }
    }
}";
            var source =
@"using System.Runtime.CompilerServices;
[NullablePublicOnly(false)]
class Program
{
}";
            var comp = CreateCompilation(new[] { sourceAttribute, source });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ExplicitAttribute_WithNullableAttribute()
        {
            var sourceAttribute =
@"#nullable enable
namespace System.Runtime.CompilerServices
{
    public class NullablePublicOnlyAttribute : System.Attribute
    {
        public NullablePublicOnlyAttribute(bool b) { }
        public NullablePublicOnlyAttribute(
            object x,
            object? y,
#nullable disable
            object z)
        {
        }
    }
}";
            var comp = CreateCompilation(sourceAttribute);
            var ref0 = comp.EmitToImageReference();
            var expected =
@"System.Runtime.CompilerServices.NullablePublicOnlyAttribute
    NullablePublicOnlyAttribute(System.Object! x, System.Object? y, System.Object z)
        [Nullable(1)] System.Object! x
        [Nullable(2)] System.Object? y
";
            AssertNullableAttributes(comp, expected);

            var source =
@"#nullable enable
public class Program
{
    public object _f1;
    public object? _f2;
#nullable disable
    public object _f3;
}";
            comp = CreateCompilation(source, references: new[] { ref0 });
            expected =
@"Program
    [Nullable(1)] System.Object! _f1
    [Nullable(2)] System.Object? _f2
";
            AssertNullableAttributes(comp, expected);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithNullablePublicOnly(), options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var attributeType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.NullablePublicOnlyAttribute");
                AttributeUsageInfo attributeUsage = attributeType.GetAttributeUsageInfo();
                Assert.False(attributeUsage.Inherited);
                Assert.False(attributeUsage.AllowMultiple);
                Assert.True(attributeUsage.HasValidAttributeTargets);
                Assert.Equal(AttributeTargets.Module, attributeUsage.ValidTargets);
            });
        }

        [Fact]
        public void MissingAttributeUsageAttribute()
        {
            var source =
@"#nullable enable
public class Program
{
    public object? F;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithNullablePublicOnly().WithNoRefSafetyRulesAttribute());
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
        }

        [Fact]
        public void AttributeField()
        {
            var source =
@"#nullable enable
using System;
using System.Linq;
using System.Reflection;
public class Program
{
    public static void Main(string[] args)
    {
        var value = GetAttributeValue(typeof(Program).Assembly.Modules.First());
        Console.WriteLine(value == null ? ""<null>"" : value.ToString());
    }
    static bool? GetAttributeValue(Module module)
    {
        var attribute = module.GetCustomAttributes(false).SingleOrDefault(a => a.GetType().Name == ""NullablePublicOnlyAttribute"");
        if (attribute == null) return null;
        var field = attribute.GetType().GetField(""IncludesInternals"");
        return (bool)field.GetValue(attribute);
    }
}";
            var sourceIVTs =
@"using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""Other"")]";

            var parseOptions = TestOptions.Regular8;
            CompileAndVerify(source, parseOptions: parseOptions, expectedOutput: "<null>");
            CompileAndVerify(source, parseOptions: parseOptions.WithNullablePublicOnly(), expectedOutput: "False");
            CompileAndVerify(new[] { source, sourceIVTs }, parseOptions: parseOptions, expectedOutput: "<null>");
            CompileAndVerify(new[] { source, sourceIVTs }, parseOptions: parseOptions.WithNullablePublicOnly(), expectedOutput: "True");
        }

        private static void AssertNoNullablePublicOnlyAttribute(ModuleSymbol module)
        {
            AssertNullablePublicOnlyAttribute(module, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false);
        }

        private static void AssertNullablePublicOnlyAttribute(ModuleSymbol module)
        {
            AssertNullablePublicOnlyAttribute(module, includesAttributeDefinition: true, includesAttributeUse: true, publicDefinition: false);
        }

        private static void AssertNullablePublicOnlyAttribute(ModuleSymbol module, bool includesAttributeDefinition, bool includesAttributeUse, bool publicDefinition)
        {
            const string attributeName = "System.Runtime.CompilerServices.NullablePublicOnlyAttribute";
            var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember(attributeName);
            var attribute = module.GetAttributes().SingleOrDefault();
            if (includesAttributeDefinition)
            {
                Assert.NotNull(type);
            }
            else
            {
                Assert.Null(type);
                if (includesAttributeUse)
                {
                    type = attribute.AttributeClass;
                }
            }
            if (type is object)
            {
                Assert.Equal(publicDefinition ? Accessibility.Public : Accessibility.Internal, type.DeclaredAccessibility);
            }
            if (includesAttributeUse)
            {
                Assert.Equal(type, attribute.AttributeClass);
            }
            else
            {
                Assert.Null(attribute);
            }
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
