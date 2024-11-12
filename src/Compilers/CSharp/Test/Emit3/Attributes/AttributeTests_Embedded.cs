// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
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
    internal class EmbeddedAttribute : System.Attribute { }
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
    internal class EmbeddedAttribute : System.Attribute { }
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
    public class EmbeddedAttribute : System.Attribute { }
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

        [Fact]
        public void EmbeddedAttributeInSourceShouldTriggerAnErrorIfCompilerNeedsToGenerateOne()
        {
            var code = @"
namespace Microsoft.CodeAnalysis
{
    public class EmbeddedAttribute : System.Attribute { }
}
class Test
{
    public void M(in int p)
    {
        // This should trigger generating another EmbeddedAttribute
    }
}";

            CreateCompilation(code, assemblyName: "testModule").VerifyEmitDiagnostics(
                // (4,18): error CS8336: The type name 'Microsoft.CodeAnalysis.EmbeddedAttribute' is reserved to be used by the compiler.
                //     public class EmbeddedAttribute : System.Attribute { }
                Diagnostic(ErrorCode.ERR_TypeReserved, "EmbeddedAttribute").WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute").WithLocation(4, 18));
        }

        [Fact]
        public void EmbeddedAttributeInReferencedModuleShouldTriggerAnErrorIfCompilerNeedsToGenerateOne()
        {
            var module = CreateCompilation(options: TestOptions.ReleaseModule, assemblyName: "testModule", source: @"
namespace Microsoft.CodeAnalysis
{
    public class EmbeddedAttribute : System.Attribute { }
}");

            var moduleRef = ModuleMetadata.CreateFromImage(module.EmitToArray()).GetReference();

            var code = @"
class Test
{
    public void M(in int p)
    {
        // This should trigger generating another EmbeddedAttribute
    }
}";

            CreateCompilation(code, references: new[] { moduleRef }).VerifyEmitDiagnostics(
                // error CS8004: Type 'EmbeddedAttribute' exported from module 'testModule.netmodule' conflicts with type declared in primary module of this assembly.
                Diagnostic(ErrorCode.ERR_ExportedTypeConflictsWithDeclaration).WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute", "testModule.netmodule").WithLocation(1, 1));
        }

        [Fact]
        public void EmbeddedAttributeForwardedToAnotherAssemblyShouldTriggerAnError()
        {
            var reference = CreateCompilation(@"
namespace Microsoft.CodeAnalysis
{
    public class EmbeddedAttribute : System.Attribute { }
}", assemblyName: "reference").ToMetadataReference();

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
            var reference = CreateCompilation(assemblyName: "testRef", parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), source: @"
namespace Microsoft.CodeAnalysis
{
    public class EmbeddedAttribute : System.Attribute { }
}
namespace OtherNamespace
{
    public class TestReference { }
}").ToMetadataReference();

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
    public class EmbeddedAttribute : System.Attribute { }
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
