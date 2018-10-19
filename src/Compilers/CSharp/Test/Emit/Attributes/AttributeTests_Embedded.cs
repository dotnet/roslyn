// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public void EmbeddedAttributeNamespace()
        {
            var code = @"
namespace Microsoft.CodeAnalysis.EmbeddedAttribute
{
    class C { }
}
namespace TestReference
{
    [Microsoft.CodeAnalysis.Embedded]
    internal class TestType1 { }

    [Microsoft.CodeAnalysis.EmbeddedAttribute]
    internal class TestType2 { }

    internal class TestType3 { }
}";

            CreateCompilation(code).VerifyEmitDiagnostics(
                // (11,29): error CS0616: 'Microsoft.CodeAnalysis.EmbeddedAttribute' is not an attribute class
                //     [Microsoft.CodeAnalysis.EmbeddedAttribute]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "EmbeddedAttribute").WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute").WithLocation(11, 29),
                // (8,29): error CS0616: 'Microsoft.CodeAnalysis.EmbeddedAttribute' is not an attribute class
                //     [Microsoft.CodeAnalysis.Embedded]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "Embedded").WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute").WithLocation(8, 29)
                );
        }

        [Fact]
        public void EmbeddedAttributeInMergedNamespaceSymbol()
        {
            var reference_cs = @"
namespace Microsoft.CodeAnalysis
{
    internal class EmbeddedAttribute : System.Attribute { }
}";
            var reference = CreateCompilation(new[] { reference_cs });
            reference.VerifyDiagnostics();

            var comp_cs = @"
namespace Microsoft.CodeAnalysis.EmbeddedAttribute
{
    class C { }
}";
            var comp = CreateCompilation(new[] { comp_cs }, options: WithNonNullTypesTrue(), references: new[] { reference.ToMetadataReference() });
            comp.VerifyEmitDiagnostics(
                // error CS0518: Predefined type 'Microsoft.CodeAnalysis.EmbeddedAttribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute").WithLocation(1, 1)
                );

            var type = comp.GetWellKnownType(WellKnownType.Microsoft_CodeAnalysis_EmbeddedAttribute);
            Assert.True(type.IsErrorType());

            // https://github.com/dotnet/roslyn/issues/29683 CSharpCompilation.AbstractSymbolSearcher needs to inject namespaces and types too
            //Assert.False(comp.ContainsSymbolsWithName("NonNullTypesAttribute", SymbolFilter.Type));
            //Assert.Empty(comp.GetSymbolsWithName("NonNullTypesAttribute", SymbolFilter.Type));
            //Assert.Empty(comp.GetSymbolsWithName(n => n == "NonNullTypesAttribute", SymbolFilter.Type));

            //Assert.True(comp.ContainsSymbolsWithName("EmbeddedAttribute", SymbolFilter.Type));
            //Assert.Equal("Microsoft.CodeAnalysis.EmbeddedAttribute", comp.GetSymbolsWithName("EmbeddedAttribute", SymbolFilter.Type).Single().ToTestDisplayString());
            //Assert.Equal("Microsoft.CodeAnalysis.EmbeddedAttribute", comp.GetSymbolsWithName(n => n == "EmbeddedAttribute", SymbolFilter.Type).Single().ToTestDisplayString());
        }

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

            CreateCompilation(code).VerifyEmitDiagnostics();
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
                // error CS0101: The namespace 'Microsoft.CodeAnalysis' already contains a definition for 'EmbeddedAttribute'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("EmbeddedAttribute", "Microsoft.CodeAnalysis").WithLocation(1, 1),
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

            CompileAndVerify(code, verify: Verification.Passes, expectedOutput: "3");
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
                // error CS0101: The namespace 'Microsoft.CodeAnalysis' already contains a definition for 'EmbeddedAttribute'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("EmbeddedAttribute", "Microsoft.CodeAnalysis").WithLocation(1, 1));
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
                // (2,99): warning CS0436: The type 'EmbeddedAttribute' in 'injected declaration' conflicts with the imported type 'EmbeddedAttribute' in 'reference, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'injected declaration'.
                // [assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(Microsoft.CodeAnalysis.EmbeddedAttribute))]
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "EmbeddedAttribute").WithArguments("injected declaration", "Microsoft.CodeAnalysis.EmbeddedAttribute", "reference, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Microsoft.CodeAnalysis.EmbeddedAttribute").WithLocation(2, 99),
                // (2,12): error CS0729: Type 'EmbeddedAttribute' is defined in this assembly, but a type forwarder is specified for it
                // [assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(Microsoft.CodeAnalysis.EmbeddedAttribute))]
                Diagnostic(ErrorCode.ERR_ForwardedTypeInThisAssembly, "System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(Microsoft.CodeAnalysis.EmbeddedAttribute))").WithArguments("Microsoft.CodeAnalysis.EmbeddedAttribute").WithLocation(2, 12));
        }

        [Fact]
        public void CompilerShouldIgnorePublicEmbeddedAttributesInReferencedAssemblies()
        {
            var reference = CreateCompilation(assemblyName: "testRef", source: @"
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
}
public class Test
{
    public void M(in object x) { } // should trigger synthesizing IsReadOnly
}";

            CreateEmptyCompilation(code).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1));
        }

        [Fact]
        public void SynthesizingAttributeRequiresSystemAttribute_NoSystemObject()
        {
            var code = @"
namespace System
{
    public class Attribute {}
    public class Void {}
}
public class Test
{
    public object M(in object x) { return x; } // should trigger synthesizing IsReadOnly
}";

            CreateEmptyCompilation(code).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // (5,18): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     public class Void {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Void").WithArguments("System.Object").WithLocation(5, 18),
                // (7,14): error CS0518: Predefined type 'System.Object' is not defined or imported
                // public class Test
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Test").WithArguments("System.Object").WithLocation(7, 14),
                // (4,18): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     public class Attribute {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Attribute").WithArguments("System.Object").WithLocation(4, 18),
                // (9,24): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     public object M(in object x) { return x; } // should trigger synthesizing IsReadOnly
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object").WithArguments("System.Object").WithLocation(9, 24),
                // (9,12): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     public object M(in object x) { return x; } // should trigger synthesizing IsReadOnly
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object").WithArguments("System.Object").WithLocation(9, 12),
                // (4,18): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                //     public class Attribute {}
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Attribute").WithArguments("object", "0").WithLocation(4, 18),
                // (5,18): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                //     public class Void {}
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Void").WithArguments("object", "0").WithLocation(5, 18),
                // (7,14): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                // public class Test
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test").WithArguments("object", "0").WithLocation(7, 14));
        }

        [Fact]
        public void SynthesizingAttributeRequiresSystemAttribute_NoSystemVoid()
        {
            var code = @"
namespace System
{
    public class Attribute {}
    public class Object {}
}
public class Test
{
    public object M(in object x) { return x; } // should trigger synthesizing IsReadOnly
}";

            CreateEmptyCompilation(code).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // (4,18): error CS0518: Predefined type 'System.Void' is not defined or imported
                //     public class Attribute {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Attribute").WithArguments("System.Void").WithLocation(4, 18),
                // (7,14): error CS0518: Predefined type 'System.Void' is not defined or imported
                // public class Test
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Test").WithArguments("System.Void").WithLocation(7, 14),
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
    public class Attribute
    {
        public Attribute(object p) { }
    }
}
public class Test
{
    public void M(in object x) { } // should trigger synthesizing IsReadOnly
}";

            CreateEmptyCompilation(code).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1));
        }

        [Fact]
        public void EmbeddedTypesInAnAssemblyAreNotExposedExternally()
        {
            var compilation1 = CreateCompilation(@"
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
