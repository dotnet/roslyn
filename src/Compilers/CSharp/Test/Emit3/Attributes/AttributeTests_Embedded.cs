// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_Embedded : CSharpTestBase
    {
        [Fact]
        public void ReferencingEmbeddedAttributesFromTheSameAssemblySucceeds()
        {
            var code = @"
namespace Microsoft.CodeAnalysis
{
    internal sealed class EmbeddedAttribute : System.Attribute { }
}
namespace TestReference
{
    [Microsoft.CodeAnalysis.Embedded]
    internal class TestType1 { }

    [Microsoft.CodeAnalysis.EmbeddedAttribute]
    internal class TestType2 { }

    internal class TestType3 { }
}
class Program
{
    public static void Main()
    {
        var obj1 = new TestReference.TestType1();
        var obj2 = new TestReference.TestType2();
        var obj3 = new TestReference.TestType3();
    }
}";

            CreateCompilation(code, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute()).VerifyEmitDiagnostics();
        }

        [Fact]
        public void ReferencingEmbeddedAttributesFromADifferentAssemblyFails_Internal()
        {
            var reference = CreateCompilation(@"
[assembly:System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Source"")]
namespace Microsoft.CodeAnalysis
{
    internal class EmbeddedAttribute : System.Attribute { }
}
namespace TestReference
{
    [Microsoft.CodeAnalysis.Embedded]
    internal class TestType1 { }

    [Microsoft.CodeAnalysis.EmbeddedAttribute]
    internal class TestType2 { }

    internal class TestType3 { }
}");

            var code = @"
class Program
{
    public static void Main()
    {
        var obj1 = new TestReference.TestType1();
        var obj2 = new TestReference.TestType2();
        var obj3 = new TestReference.TestType3(); // This should be fine
    }
}";

            CreateCompilation(code, references: new[] { reference.ToMetadataReference() }, assemblyName: "Source").VerifyDiagnostics(
                // (6,38): error CS0234: The type or namespace name 'TestType1' does not exist in the namespace 'TestReference' (are you missing an assembly reference?)
                //         var obj1 = new TestReference.TestType1();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "TestType1").WithArguments("TestType1", "TestReference").WithLocation(6, 38),
                // (7,38): error CS0234: The type or namespace name 'TestType2' does not exist in the namespace 'TestReference' (are you missing an assembly reference?)
                //         var obj2 = new TestReference.TestType2();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "TestType2").WithArguments("TestType2", "TestReference").WithLocation(7, 38));
        }

        [Fact]
        public void ReferencingEmbeddedAttributesFromADifferentAssemblyFails_Module()
        {
            var module = CreateCompilation(@"
namespace Microsoft.CodeAnalysis
{
    internal sealed class EmbeddedAttribute : System.Attribute { }
}
namespace TestReference
{
    [Microsoft.CodeAnalysis.Embedded]
    internal class TestType1 { }

    [Microsoft.CodeAnalysis.EmbeddedAttribute]
    internal class TestType2 { }

    internal class TestType3 { }
}", options: TestOptions.ReleaseModule);

            var reference = ModuleMetadata.CreateFromImage(module.EmitToArray()).GetReference();

            var code = @"
class Program
{
    public static void Main()
    {
        var obj1 = new TestReference.TestType1();
        var obj2 = new TestReference.TestType2();
        var obj3 = new TestReference.TestType3(); // This should be fine
    }
}";

            CreateCompilation(code, references: new[] { reference }, assemblyName: "Source").VerifyDiagnostics(
                // (6,38): error CS0234: The type or namespace name 'TestType1' does not exist in the namespace 'TestReference' (are you missing an assembly reference?)
                //         var obj1 = new TestReference.TestType1();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "TestType1").WithArguments("TestType1", "TestReference").WithLocation(6, 38),
                // (7,38): error CS0234: The type or namespace name 'TestType2' does not exist in the namespace 'TestReference' (are you missing an assembly reference?)
                //         var obj2 = new TestReference.TestType2();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "TestType2").WithArguments("TestType2", "TestReference").WithLocation(7, 38));
        }

        [Fact]
        public void ReferencingEmbeddedAttributesFromADifferentAssemblyFails_Public()
        {
            var reference = CreateCompilation(@"
namespace Microsoft.CodeAnalysis
{
    internal class EmbeddedAttribute : System.Attribute { }
}
namespace TestReference
{
    [Microsoft.CodeAnalysis.Embedded]
    public class TestType1 { }

    [Microsoft.CodeAnalysis.EmbeddedAttribute]
    public class TestType2 { }

    public class TestType3 { }
}");

            var code = @"
class Program
{
    public static void Main()
    {
        var obj1 = new TestReference.TestType1();
        var obj2 = new TestReference.TestType2();
        var obj3 = new TestReference.TestType3(); // This should be fine
    }
}";

            CreateCompilation(code, references: new[] { reference.ToMetadataReference() }).VerifyDiagnostics(
                // (6,38): error CS0234: The type or namespace name 'TestType1' does not exist in the namespace 'TestReference' (are you missing an assembly reference?)
                //         var obj1 = new TestReference.TestType1();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "TestType1").WithArguments("TestType1", "TestReference").WithLocation(6, 38),
                // (7,38): error CS0234: The type or namespace name 'TestType2' does not exist in the namespace 'TestReference' (are you missing an assembly reference?)
                //         var obj2 = new TestReference.TestType2();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "TestType2").WithArguments("TestType2", "TestReference").WithLocation(7, 38));
        }

        [Fact]
        public void EmbeddedAttributeInSourceIsAllowedIfCompilerDoesNotNeedToGenerateOne()
        {
            var code = @"
namespace Microsoft.CodeAnalysis
{
    internal sealed class EmbeddedAttribute : System.Attribute { }
}
namespace OtherNamespace
{
    [Microsoft.CodeAnalysis.EmbeddedAttribute]
    public class TestReference
    {
        public static int GetValue() => 3;
    }
}
class Test
{
    public static void Main()
    {
        // This should be fine, as the compiler doesn't need to use an embedded attribute for this compilation
        System.Console.Write(OtherNamespace.TestReference.GetValue());
    }
}";

            CompileAndVerify(code, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), verify: Verification.Passes, expectedOutput: "3");
        }

        [Theory]
        [CombinatorialData]
        public void EmbeddedAttributeFromSourceValidation_Valid(
            [CombinatorialValues("internal", "public")] string ctorAccessModifier,
            [CombinatorialValues(
                "System.AttributeTargets.Class | System.AttributeTargets.Struct | System.AttributeTargets.Interface | System.AttributeTargets.Enum | System.AttributeTargets.Delegate",
                "System.AttributeTargets.All",
                "")]
            string targetsList)
        {
            var targets = targetsList != "" ? $", System.AttributeUsage({targetsList})" : "";

            var code = $$"""
                namespace Microsoft.CodeAnalysis
                {
                    [Embedded{{targets}}]
                    internal sealed class EmbeddedAttribute : System.Attribute
                    {
                        {{ctorAccessModifier}} EmbeddedAttribute() { }
                    }
                }

                [Microsoft.CodeAnalysis.Embedded]
                public class Test
                {
                    public static void Main()
                    {
                        M(1);
                    }

                    public static void M(in int p)
                    {
                        System.Console.WriteLine("M");
                    }
                }
                """;

            var verifier = CompileAndVerify(code, targetFramework: TargetFramework.NetStandard20, expectedOutput: "M", symbolValidator: module =>
            {
                var embeddedAttribute = module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                Assert.NotNull(embeddedAttribute);
                Assert.Equal("Microsoft.CodeAnalysis.EmbeddedAttribute", embeddedAttribute.GetAttributes().Single(a => a.AttributeClass.Name != "AttributeUsageAttribute").AttributeClass.ToTestDisplayString());
            });

            verifier.VerifyDiagnostics();

            var reference = verifier.Compilation.EmitToImageReference();

            var comp2 = CreateCompilation("""
                Test.M(1);
                """, references: [reference], targetFramework: TargetFramework.NetStandard20);

            comp2.VerifyDiagnostics(
                // (1,1): error CS0103: The name 'Test' does not exist in the current context
                // Test.M(1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Test").WithArguments("Test").WithLocation(1, 1));

            comp2 = CreateCompilation("""
                public class Test
                {
                    public static void M(in int p) {}
                }
                """, references: [reference], targetFramework: TargetFramework.NetStandard20);

            CompileAndVerify(comp2, symbolValidator: module =>
            {
                var embeddedAttribute = module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                Assert.NotNull(embeddedAttribute);
                Assert.Equal(["System.Runtime.CompilerServices.CompilerGeneratedAttribute", "Microsoft.CodeAnalysis.EmbeddedAttribute"], embeddedAttribute.GetAttributes().Select(a => a.AttributeClass.ToTestDisplayString()));
            }).VerifyDiagnostics();
        }

        [Fact]
        public void EmbeddedAttributeFromSourceValidation_Public()
        {
            var code = """
                namespace Microsoft.CodeAnalysis
                {
                    [Embedded]
                    public sealed class EmbeddedAttribute : System.Attribute
                    {
                        public EmbeddedAttribute() { }
                    }
                }
                class Test
                {
                    public void M(in int p)
                    {
                        // This would trigger generating another EmbeddedAttribute, if we hadn't already defined one
                    }
                }
                """;

            CreateCompilation(code, assemblyName: "testModule", targetFramework: TargetFramework.NetStandard20).VerifyEmitDiagnostics(
                // (4,25): error CS9271: The type 'Microsoft.CodeAnalysis.EmbeddedAttribute' must be non-generic, internal, non-file, sealed, non-static, have a parameterless constructor, inherit from System.Attribute, and be able to be applied to any type.
                //     public sealed class EmbeddedAttribute : System.Attribute
                Diagnostic(ErrorCode.ERR_EmbeddedAttributeMustFollowPattern, "EmbeddedAttribute").WithLocation(4, 25));
        }

        [Fact]
        public void EmbeddedAttributeFromSourceValidation_NoSealed()
        {
            var code = """
                namespace Microsoft.CodeAnalysis
                {
                    [Embedded]
                    internal class EmbeddedAttribute : System.Attribute
                    {
                        public EmbeddedAttribute() { }
                    }
                }
                class Test
                {
                    public void M(in int p)
                    {
                        // This would trigger generating another EmbeddedAttribute, if we hadn't already defined one
                    }
                }
                """;

            CreateCompilation(code, assemblyName: "testModule", targetFramework: TargetFramework.NetStandard20).VerifyEmitDiagnostics(
                // (4,20): error CS9271: The type 'Microsoft.CodeAnalysis.EmbeddedAttribute' must be non-generic, internal, non-file, sealed, non-static, have a parameterless constructor, inherit from System.Attribute, and be able to be applied to any type.
                //     internal class EmbeddedAttribute : System.Attribute
                Diagnostic(ErrorCode.ERR_EmbeddedAttributeMustFollowPattern, "EmbeddedAttribute").WithLocation(4, 20));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80105")]
        public void EmbeddedAttributeFromSourceValidation_File()
        {
            var code = """
                namespace Microsoft.CodeAnalysis
                {
                    [Embedded]
                    file sealed class EmbeddedAttribute : System.Attribute
                    {
                        public EmbeddedAttribute() { }
                    }
                }
                class Test
                {
                    public void M(in int p)
                    {
                        // This would trigger generating another EmbeddedAttribute, if we hadn't already defined one
                    }
                }
                """;

            CreateCompilation(code, targetFramework: TargetFramework.NetStandard20).VerifyEmitDiagnostics(
                // (4,23): error CS9271: The type 'Microsoft.CodeAnalysis.EmbeddedAttribute' must be non-generic, internal, non-file, sealed, non-static, have a parameterless constructor, inherit from System.Attribute, and be able to be applied to any type.
                //     file sealed class EmbeddedAttribute : System.Attribute
                Diagnostic(ErrorCode.ERR_EmbeddedAttributeMustFollowPattern, "EmbeddedAttribute").WithLocation(4, 23));
        }

        [Theory]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public void EmbeddedAttributeFromSourceValidation_CtorAccessibility(string ctorAccessModifier)
        {
            var code = $$"""
                namespace Microsoft.CodeAnalysis
                {
                    [Embedded]
                    internal sealed class EmbeddedAttribute : System.Attribute
                    {
                        {{ctorAccessModifier}} EmbeddedAttribute() { }
                    }
                }
                class Test
                {
                    public void M(in int p)
                    {
                        // This would trigger generating another EmbeddedAttribute, if we hadn't already defined one
                    }
                }
                """;

            List<DiagnosticDescription> diagnostics = [
                // (4,27): error CS9271: The type 'Microsoft.CodeAnalysis.EmbeddedAttribute' must be non-generic, internal, non-file, sealed, non-static, have a parameterless constructor, inherit from System.Attribute, and be able to be applied to any type.
                //     internal sealed class EmbeddedAttribute : System.Attribute
                Diagnostic(ErrorCode.ERR_EmbeddedAttributeMustFollowPattern, "EmbeddedAttribute").WithLocation(4, 27),
            ];

            if (ctorAccessModifier != "private")
            {
                diagnostics.Add(
                    // (6,27): warning CS0628: 'EmbeddedAttribute.EmbeddedAttribute()': new protected member declared in sealed type
                    //         private protected EmbeddedAttribute() { }
                    Diagnostic(ErrorCode.WRN_ProtectedInSealed, "EmbeddedAttribute").WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute.EmbeddedAttribute()").WithLocation(6, 10 + ctorAccessModifier.Length)
                );
            }

            CreateCompilation(code, assemblyName: "testModule", targetFramework: TargetFramework.NetStandard20)
                .VerifyEmitDiagnostics(diagnostics.ToArray());
        }

        [Theory, CombinatorialData]
        public void EmbeddedAttributeFromSourceValidation_MissingEmbeddedAttributeApplicationGeneratesAttributeApplication(bool hasObsolete)
        {
            var code = $$"""
                namespace Microsoft.CodeAnalysis
                {
                    {{(hasObsolete ? "[System.Obsolete]" : "")}}
                    internal sealed class EmbeddedAttribute : System.Attribute
                    {
                        public EmbeddedAttribute() { }
                    }
                }
                class Test
                {
                    public void M(in int p)
                    {
                        // This would trigger generating EmbeddedAttribute if we hadn't defined one
                    }
                }
                """;

            var comp = CreateCompilation(code, assemblyName: "testModule", targetFramework: TargetFramework.NetStandard20);
            var embeddedAttribute = comp.Assembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
            Assert.True(embeddedAttribute.HasCodeAnalysisEmbeddedAttribute);

            CompileAndVerify(comp, symbolValidator: static module =>
                {
                    var embeddedAttribute = module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                    Assert.NotNull(embeddedAttribute);
                    Assert.Equal(["Microsoft.CodeAnalysis.EmbeddedAttribute"], embeddedAttribute.GetAttributes().Where(a => a.AttributeClass.Name != "ObsoleteAttribute").Select(a => a.AttributeClass.ToTestDisplayString()));
                })
                .VerifyDiagnostics();
        }

        [Fact]
        public void EmbeddedAttributeFromSourceValidation_MissingEmbeddedAttributeApplicationGeneratesAttributeApplication_Partial()
        {
            var code = """
                namespace Microsoft.CodeAnalysis
                {
                    internal sealed partial class EmbeddedAttribute : System.Attribute
                    {
                    }
                }
                """;

            var comp = CreateCompilation([code, code]);
            var sourceDeclaration = comp.SourceAssembly.GetTypeByMetadataName("Microsoft.CodeAnalysis.EmbeddedAttribute");

            Assert.Equal(2, sourceDeclaration.Locations.Length);

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var embeddedAttribute = module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                Assert.NotNull(embeddedAttribute);
                Assert.Equal("Microsoft.CodeAnalysis.EmbeddedAttribute", embeddedAttribute.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            }).VerifyDiagnostics();
        }

        [Fact]
        public void EmbeddedAttributeFromSourceValidation_Generic()
        {
            var code = """
                namespace Microsoft.CodeAnalysis
                {
                    [Embedded<int>]
                    internal sealed class EmbeddedAttribute<T> : System.Attribute
                    {
                        public EmbeddedAttribute() { }
                    }
                }
                class Test
                {
                    public void M(in int p)
                    {
                        // This should trigger generating another EmbeddedAttribute
                    }
                }
                """;

            var comp = CreateCompilation(code, assemblyName: "testModule", targetFramework: TargetFramework.NetStandard20);
            CompileAndVerify(comp, symbolValidator: verifyModule).VerifyDiagnostics();

            static void verifyModule(ModuleSymbol module)
            {
                var nonGenericAttribute = module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                Assert.NotNull(nonGenericAttribute);
                Assert.Equal("Microsoft.CodeAnalysis.EmbeddedAttribute", nonGenericAttribute.ToTestDisplayString());

                var genericAttribute = module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName + "`1");
                Assert.NotNull(genericAttribute);
                Assert.Equal("Microsoft.CodeAnalysis.EmbeddedAttribute<T>", genericAttribute.ToTestDisplayString());

                var isReadonlyAttribute = module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.IsReadOnlyAttribute.FullName);
                Assert.NotNull(isReadonlyAttribute);
                AssertEx.Equal("Microsoft.CodeAnalysis.EmbeddedAttribute", isReadonlyAttribute.GetAttributes().Single(a => a.AttributeClass.Name == "EmbeddedAttribute").AttributeClass.ToTestDisplayString());
            }
        }

        [Fact]
        public void EmbeddedAttributeFromSourceValidation_Generic_BothDefined()
        {
            var code = """
                namespace Microsoft.CodeAnalysis
                {
                    [Embedded<int>]
                    internal sealed class EmbeddedAttribute<T> : System.Attribute
                    {
                        public EmbeddedAttribute() { }
                    }

                    internal sealed class EmbeddedAttribute : System.Attribute {}
                }
                class Test
                {
                    public void M(in int p)
                    {
                        // This would trigger generating another EmbeddedAttribute, if we hadn't already defined one
                    }
                }
                """;

            var comp = CreateCompilation(code, assemblyName: "testModule", targetFramework: TargetFramework.NetStandard20);
            CompileAndVerify(comp, symbolValidator: verifyModule).VerifyDiagnostics();

            static void verifyModule(ModuleSymbol module)
            {
                var nonGenericAttribute = module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                Assert.NotNull(nonGenericAttribute);
                Assert.Equal("Microsoft.CodeAnalysis.EmbeddedAttribute", nonGenericAttribute.ToTestDisplayString());

                var genericAttribute = module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName + "`1");
                Assert.NotNull(genericAttribute);
                Assert.Equal("Microsoft.CodeAnalysis.EmbeddedAttribute<T>", genericAttribute.ToTestDisplayString());

                var isReadonlyAttribute = module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.IsReadOnlyAttribute.FullName);
                Assert.NotNull(isReadonlyAttribute);
                AssertEx.Equal("Microsoft.CodeAnalysis.EmbeddedAttribute", isReadonlyAttribute.GetAttributes().Single(a => a.AttributeClass.Name == "EmbeddedAttribute").AttributeClass.ToTestDisplayString());
            }
        }

        [Fact]
        public void EmbeddedAttributeFromSourceValidation_Static()
        {
            var code = """
                namespace Microsoft.CodeAnalysis
                {
                    [Embedded]
                    internal sealed static class EmbeddedAttribute : System.Attribute
                    {
                    }
                }
                class Test
                {
                    public void M(in int p)
                    {
                        // This would trigger generating another EmbeddedAttribute, if we hadn't already defined one
                    }
                }
                """;

            CreateCompilation(code, assemblyName: "testModule", targetFramework: TargetFramework.NetStandard20).VerifyEmitDiagnostics(
                // (3,6): error CS0616: 'EmbeddedAttribute' is not an attribute class
                //     [Embedded]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "Embedded").WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute").WithLocation(3, 6),
                // (4,34): error CS0441: 'EmbeddedAttribute': a type cannot be both static and sealed
                //     internal sealed static class EmbeddedAttribute : System.Attribute
                Diagnostic(ErrorCode.ERR_SealedStaticClass, "EmbeddedAttribute").WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute").WithLocation(4, 34),
                // (4,34): error CS9271: The type 'Microsoft.CodeAnalysis.EmbeddedAttribute' must be non-generic, internal, non-file, sealed, non-static, have a parameterless constructor, inherit from System.Attribute, and be able to be applied to any type.
                //     internal sealed static class EmbeddedAttribute : System.Attribute
                Diagnostic(ErrorCode.ERR_EmbeddedAttributeMustFollowPattern, "EmbeddedAttribute").WithLocation(4, 34),
                // (4,54): error CS0713: Static class 'EmbeddedAttribute' cannot derive from type 'Attribute'. Static classes must derive from object.
                //     internal sealed static class EmbeddedAttribute : System.Attribute
                Diagnostic(ErrorCode.ERR_StaticDerivedFromNonObject, "System.Attribute").WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute", "System.Attribute").WithLocation(4, 54));
        }

        [Fact]
        public void EmbeddedAttributeFromSourceValidation_WrongBaseType()
        {
            var code = """
                namespace Microsoft.CodeAnalysis
                {
                    [Embedded]
                    internal sealed class EmbeddedAttribute
                    {
                        public EmbeddedAttribute() { }
                    }
                }
                class Test
                {
                    public void M(in int p)
                    {
                        // This would trigger generating another EmbeddedAttribute, if we hadn't already defined one
                    }
                }
                """;

            CreateCompilation(code, assemblyName: "testModule", targetFramework: TargetFramework.NetStandard20).VerifyEmitDiagnostics(
                // (3,6): error CS0616: 'EmbeddedAttribute' is not an attribute class
                //     [Embedded]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "Embedded").WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute").WithLocation(3, 6),
                // (4,27): error CS9271: The type 'Microsoft.CodeAnalysis.EmbeddedAttribute' must be non-generic, internal, non-file, sealed, non-static, have a parameterless constructor, inherit from System.Attribute, and be able to be applied to any type.
                //     internal sealed class EmbeddedAttribute
                Diagnostic(ErrorCode.ERR_EmbeddedAttributeMustFollowPattern, "EmbeddedAttribute").WithLocation(4, 27));
        }

        [Theory]
        [InlineData("AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate")]
        [InlineData("AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate")]
        [InlineData("AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate")]
        [InlineData("AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Delegate")]
        [InlineData("AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum")]
        public void EmbeddedAttributeFromSourceValidation_AttributeUsage(string targets)
        {
            var code = $$"""
                using System;
                namespace Microsoft.CodeAnalysis
                {
                    [Embedded, AttributeUsage({{targets}})]
                    internal sealed class EmbeddedAttribute : System.Attribute
                    {
                        internal EmbeddedAttribute() { }
                    }
                }
                class Test
                {
                    public void M(in int p)
                    {
                        // This would trigger generating EmbeddedAttribute if we hadn't defined one
                    }
                }
                """;

            List<DiagnosticDescription> diagnostics = [];
            if (!targets.Contains("Class"))
            {
                diagnostics.Add(
                    // (4,6): error CS0592: Attribute 'Embedded' is not valid on this declaration type. It is only valid on 'struct, enum, interface, delegate' declarations.
                    //     [Embedded, AttributeUsage(AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate)]
                    Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Embedded").WithArguments("Embedded", "struct, enum, interface, delegate").WithLocation(4, 6)
                );
            }

            diagnostics.Add(
                // (5,27): error CS9271: The type 'Microsoft.CodeAnalysis.EmbeddedAttribute' must be non-generic, internal, non-file, sealed, non-static, have a parameterless constructor, inherit from System.Attribute, and be able to be applied to any type.
                //     internal sealed class EmbeddedAttribute : System.Attribute
                Diagnostic(ErrorCode.ERR_EmbeddedAttributeMustFollowPattern, "EmbeddedAttribute").WithLocation(5, 27)
            );

            CreateCompilation(code, assemblyName: "testModule", targetFramework: TargetFramework.NetStandard20)
                .VerifyEmitDiagnostics(diagnostics.ToArray());
        }

        [Fact]
        public void EmbeddedAttributeForwardedToAnotherAssemblyShouldTriggerAnError()
        {
            var reference = CompileIL("""
                .assembly extern mscorlib { }
                .assembly testRef
                {
                }

                .class public auto ansi beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
                    extends [mscorlib]System.Attribute
                {
                    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Attribute::.ctor()
                        ret
                    }
                }
                """, prependDefaultHeader: false);

            var code = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(Microsoft.CodeAnalysis.EmbeddedAttribute))]
class Test
{
    public void M(in int p)
    {
        // This should trigger generating another EmbeddedAttribute
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyEmitDiagnostics(
                // error CS8006: Forwarded type 'EmbeddedAttribute' conflicts with type declared in primary module of this assembly.
                Diagnostic(ErrorCode.ERR_ForwardedTypeConflictsWithDeclaration).WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute").WithLocation(1, 1));
        }

        [Fact]
        public void CompilerShouldIgnorePublicEmbeddedAttributesInReferencedAssemblies()
        {
            var reference = CompileIL("""
                .assembly extern mscorlib { }
                .assembly testRef
                {
                }

                .class public auto ansi beforefieldinit OtherNamespace.TestReference
                    extends [mscorlib]System.Object
                {
                    // Methods
                    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }

                .class public auto ansi beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
                    extends [mscorlib]System.Attribute
                {
                    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Attribute::.ctor()
                        ret
                    }
                }
                """, prependDefaultHeader: false);

            var code = @"
class Test
{
    // This should trigger generating another EmbeddedAttribute
    public void M(in int p)
    {
        var obj = new OtherNamespace.TestReference(); // This should be fine
    }
}";

            CompileAndVerify(code, verify: Verification.Passes, references: new[] { reference }, symbolValidator: module =>
            {
                var attributeName = AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName;

                var referenceAttribute = module.GetReferencedAssemblySymbols().Single(assembly => assembly.Name == "testRef").GetTypeByMetadataName(attributeName);
                Assert.NotNull(referenceAttribute);

                var generatedAttribute = module.ContainingAssembly.GetTypeByMetadataName(attributeName);
                Assert.NotNull(generatedAttribute);

                Assert.False(referenceAttribute.Equals(generatedAttribute));
            });
        }

        [Fact]
        public void SynthesizingAttributeRequiresSystemAttribute_NonExisting()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
    public class ValueType {}
    public struct Int32 { }
}
public class Test
{
    public void M(in object x) { } // should trigger synthesizing IsReadOnly
}";

            CreateEmptyCompilation(code).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
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
        public void SynthesizingAttributeRequiresSystemAttribute_NoSystemObject()
        {
            var code = @"
namespace System
{
    public class Attribute {}
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public class ValueType { }
    public struct Enum { }
    public enum AttributeTargets { }
    public class Void {}
    public struct Int32 { }
    public struct Boolean { }
}
public class Test
{
    public object M(in object x) { return x; } // should trigger synthesizing IsReadOnly
}";

            CreateEmptyCompilation(code).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // (4,18): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     public class Attribute {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Attribute").WithArguments("System.Object").WithLocation(4, 18),
                // (11,18): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     public class ValueType { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ValueType").WithArguments("System.Object").WithLocation(11, 18),
                // (14,18): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     public class Void {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Void").WithArguments("System.Object").WithLocation(14, 18),
                // (18,14): error CS0518: Predefined type 'System.Object' is not defined or imported
                // public class Test
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Test").WithArguments("System.Object").WithLocation(18, 14),
                // (20,24): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     public object M(in object x) { return x; } // should trigger synthesizing IsReadOnly
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object").WithArguments("System.Object").WithLocation(20, 24),
                // (20,12): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     public object M(in object x) { return x; } // should trigger synthesizing IsReadOnly
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object").WithArguments("System.Object").WithLocation(20, 12),
                // (7,40): error CS0518: Predefined type 'System.Object' is not defined or imported
                //         public AttributeUsageAttribute(AttributeTargets t) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "AttributeTargets").WithArguments("System.Object").WithLocation(7, 40),
                // (14,18): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                //     public class Void {}
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Void").WithArguments("object", "0").WithLocation(14, 18),
                // (4,18): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                //     public class Attribute {}
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Attribute").WithArguments("object", "0").WithLocation(4, 18),
                // (11,18): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                //     public class ValueType { }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "ValueType").WithArguments("object", "0").WithLocation(11, 18),
                // (18,14): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                // public class Test
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test").WithArguments("object", "0").WithLocation(18, 14));
        }

        [Fact]
        public void SynthesizingAttributeRequiresSystemAttribute_NoSystemVoid()
        {
            var code = @"
namespace System
{
    public class Attribute {}
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public class ValueType { }
    public struct Int32 { }
    public struct Boolean { }
    public struct Enum { }
    public enum AttributeTargets { }
    public class Object {}
}
public class Test
{
    public object M(in object x) { return x; } // should trigger synthesizing IsReadOnly
}";

            CreateEmptyCompilation(code).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // (8,42): error CS0518: Predefined type 'System.Void' is not defined or imported
                //         public bool AllowMultiple { get; set; }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "set;").WithArguments("System.Void").WithLocation(8, 42),
                // (9,38): error CS0518: Predefined type 'System.Void' is not defined or imported
                //         public bool Inherited { get; set; }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "set;").WithArguments("System.Void").WithLocation(9, 38),
                // (7,9): error CS0518: Predefined type 'System.Void' is not defined or imported
                //         public AttributeUsageAttribute(AttributeTargets t) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "public AttributeUsageAttribute(AttributeTargets t) { }").WithArguments("System.Void").WithLocation(7, 9),
                // (4,18): error CS0518: Predefined type 'System.Void' is not defined or imported
                //     public class Attribute {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Attribute").WithArguments("System.Void").WithLocation(4, 18),
                // (11,18): error CS0518: Predefined type 'System.Void' is not defined or imported
                //     public class ValueType { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ValueType").WithArguments("System.Void").WithLocation(11, 18),
                // (18,14): error CS0518: Predefined type 'System.Void' is not defined or imported
                // public class Test
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Test").WithArguments("System.Void").WithLocation(18, 14),
                // (7,16): error CS0518: Predefined type 'System.Void' is not defined or imported
                //         public AttributeUsageAttribute(AttributeTargets t) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "AttributeUsageAttribute").WithArguments("System.Void").WithLocation(7, 16),
                // error CS0518: Predefined type 'System.Void' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Void").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Void' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Void").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Void' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Void").WithLocation(1, 1));
        }

        [Fact]
        public void SynthesizingAttributeRequiresSystemAttribute_NoDefaultConstructor()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
    public class ValueType { }
    public struct Int32 { }
    public struct Boolean { }
    public class Attribute
    {
        public Attribute(object p) { }
    }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) : base(null) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
public class Test
{
    public void M(in object x) { } // should trigger synthesizing IsReadOnly
}";

            CreateEmptyCompilation(code).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1));
        }

        [Fact]
        public void EmbeddedTypesInAnAssemblyAreNotExposedExternally()
        {
            var compilation1 = CreateCompilation(parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), source: @"
namespace Microsoft.CodeAnalysis
{
    internal sealed class EmbeddedAttribute : System.Attribute { }
}
[Microsoft.CodeAnalysis.Embedded]
public class TestReference1 { }
public class TestReference2 { }
");

            Assert.NotNull(compilation1.GetTypeByMetadataName("TestReference1"));
            Assert.NotNull(compilation1.GetTypeByMetadataName("TestReference2"));

            var compilation2 = CreateCompilation("", references: new[] { compilation1.EmitToImageReference() });

            Assert.Null(compilation2.GetTypeByMetadataName("TestReference1"));
            Assert.NotNull(compilation2.GetTypeByMetadataName("TestReference2"));
        }
    }
}
