// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Linq;
using Xunit;
using static Xunit.Assert;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_NonNullTypes : CSharpTestBase
    {
        [Fact]
        public void ReferencingNonNullTypesAttributesFromTheSameAssemblyAllowed()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    internal class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}

[System.Runtime.CompilerServices.NonNullTypes]
internal class TestType1 { }

[System.Runtime.CompilerServices.NonNullTypesAttribute(false)]
internal class TestType2 { }
";

            var comp = CreateCompilation(code);
            comp.VerifyEmitDiagnostics();
            True(comp.GetMember("TestType1").NonNullTypes);
            False(comp.GetMember("TestType2").NonNullTypes);
        }

        [Fact]
        public void CannotReferenceNonNullTypesAttributesFromADifferentAssembly_Internal()
        {
            var reference = CreateCompilation(@"
[assembly:System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Source"")]
namespace System.Runtime.CompilerServices
{
    internal class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}

[System.Runtime.CompilerServices.NonNullTypes]
internal class TestType1 { }

[System.Runtime.CompilerServices.NonNullTypesAttribute]
internal class TestType2 { }
");
            False(reference.GetMember("TestType1").GetAttributes().Single().AttributeClass.IsImplicitlyDeclared);
            False(reference.GetMember("TestType2").GetAttributes().Single().AttributeClass.IsImplicitlyDeclared);

            var code = "[module: System.Runtime.CompilerServices.NonNullTypes]";

            // NonNullTypesAttribute from referenced assembly is ignored, and we use the injected on instead
            var comp = CreateCompilation(code, references: new[] { reference.ToMetadataReference() });
            comp.VerifyDiagnostics();
            True(comp.SourceModule.GetAttributes().Single().AttributeClass.IsImplicitlyDeclared);

            var system = (INamespaceSymbol)comp.GlobalNamespace.GetMember("System");
            Equal(NamespaceKind.Compilation, system.NamespaceKind);
        }

        [Fact]
        public void CannotReferenceNonNullTypesAttributesFromADifferentAssembly_Module()
        {
            var module = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    internal class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}

[System.Runtime.CompilerServices.NonNullTypes]
internal class TestType1 { }
", options: TestOptions.ReleaseModule);

            var reference = ModuleMetadata.CreateFromImage(module.EmitToArray()).GetReference();

            var system = (INamespaceSymbol)module.GlobalNamespace.GetMember("System");
            Equal(NamespaceKind.Compilation, system.NamespaceKind);

            var code = "[module: System.Runtime.CompilerServices.NonNullTypes]";

            // NonNullTypesAttribute from module conflicts with injected symbol
            var comp = CreateCompilation(code, references: new[] { reference }, assemblyName: "Source");
            comp.VerifyDiagnostics(
                // error CS0101: The namespace 'System.Runtime.CompilerServices' already contains a definition for 'NonNullTypesAttribute'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("NonNullTypesAttribute", "System.Runtime.CompilerServices").WithLocation(1, 1)
                );

            system = (INamespaceSymbol)comp.GlobalNamespace.GetMember("System");
            Equal(NamespaceKind.Compilation, system.NamespaceKind);
        }

        [Fact]
        public void CannotReferenceNonNullTypesAttributesFromADifferentAssembly_Public()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    internal class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}

[System.Runtime.CompilerServices.NonNullTypes]
public class TestType1 { }

[System.Runtime.CompilerServices.NonNullTypesAttribute(false)]
public class TestType2 { }
");
            False(reference.GetMember("TestType1").GetAttributes().Single().AttributeClass.IsImplicitlyDeclared);
            False(reference.GetMember("TestType2").GetAttributes().Single().AttributeClass.IsImplicitlyDeclared);

            var code = "[module: System.Runtime.CompilerServices.NonNullTypes]";

            var comp = CreateCompilation(code, references: new[] { reference.ToMetadataReference() });
            comp.VerifyDiagnostics();

            True(comp.SourceModule.GetAttributes().Single().AttributeClass.IsImplicitlyDeclared);
        }

        [Fact]
        public void NonNullTypesAttributeInSourceCanBeUsed()
        {
            var code = @"
// This should trigger generating another NonNullTypesAttribute
[module: System.Runtime.CompilerServices.NonNullTypes]

namespace System.Runtime.CompilerServices
{
    public class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}";

            var comp = CreateCompilation(code);
            comp.VerifyEmitDiagnostics();
            True(comp.SourceModule.NonNullTypes);
            False(comp.SourceModule.GetAttributes().Single().AttributeClass.IsImplicitlyDeclared);
        }

        [Fact]
        public void NonNullTypesAttributeInReferencedModuleCausesAmbiguity()
        {
            var module = CreateCompilation(options: TestOptions.ReleaseModule, assemblyName: "testModule", source: @"
namespace System.Runtime.CompilerServices
{
    public class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}");

            var moduleRef = ModuleMetadata.CreateFromImage(module.EmitToArray()).GetReference();

            var code = @"
class Test
{
    public void M(in int p)
    {
        // This should trigger generating another NonNullTypesAttribute
    }
}";

            CreateCompilation(code, references: new[] { moduleRef }).VerifyEmitDiagnostics(
                // error CS0101: The namespace 'System.Runtime.CompilerServices' already contains a definition for 'NonNullTypesAttribute'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("NonNullTypesAttribute", "System.Runtime.CompilerServices").WithLocation(1, 1));
        }

        [Fact]
        public void NonNullTypesAttributeForwardedToAnotherAssemblyShouldTriggerAnError()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}", assemblyName: "reference").ToMetadataReference();

            var code = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(System.Runtime.CompilerServices.NonNullTypesAttribute))]

// This should trigger generating another NonNullTypesAttribute
[module: System.Runtime.CompilerServices.NonNullTypes]
class Test
{
}";

            CreateCompilation(code, references: new[] { reference }).VerifyEmitDiagnostics(
                // (2,12): error CS0729: Type 'NonNullTypesAttribute' is defined in this assembly, but a type forwarder is specified for it
                // [assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(System.Runtime.CompilerServices.NonNullTypesAttribute))]
                Diagnostic(ErrorCode.ERR_ForwardedTypeInThisAssembly, "System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(System.Runtime.CompilerServices.NonNullTypesAttribute))").WithArguments("System.Runtime.CompilerServices.NonNullTypesAttribute").WithLocation(2, 12));
        }

        [Fact]
        public void CompilerShouldIgnorePublicNonNullTypesAttributesInReferencedAssemblies()
        {
            var reference = CreateCompilation(assemblyName: "testRef", source: @"
namespace System.Runtime.CompilerServices
{
    public class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}
public class C { }
").ToMetadataReference();

            var code = @"
[module: System.Runtime.CompilerServices.NonNullTypes]
public class D : C { }";

            CompileAndVerify(code, references: new[] { reference }, symbolValidator: module =>
            {
                var attributeName = AttributeDescription.NonNullTypesAttribute.FullName;

                var referenceAttribute = module.GetReferencedAssemblySymbols().Single(assembly => assembly.Name == "testRef").GetTypeByMetadataName(attributeName);
                NotNull(referenceAttribute);

                var generatedAttribute = module.ContainingAssembly.GetTypeByMetadataName(attributeName);
                NotNull(generatedAttribute);

                False(referenceAttribute.Equals(generatedAttribute));
            });
        }

        [Fact]
        public void SynthesizingAttributeRequiresSystemAttribute_NoSystemAttribute()
        {
            var code = @"
// This should trigger generating NonNullTypesAttribute
[module: System.Runtime.CompilerServices.NonNullTypes]
namespace System
{
    public class Object {}
    public class Void {}
}";

            CreateEmptyCompilation(code).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // error CS0518: Predefined type 'System.Boolean' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Boolean").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Boolean' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Boolean").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS1729: 'Attribute' does not contain a constructor that takes 0 arguments
                Diagnostic(ErrorCode.ERR_BadCtorArgCount).WithArguments("System.Attribute", "0").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1));
        }

        [Fact]
        public void SynthesizingAttributeRequiresSystemAttribute_NoSystemObject()
        {
            var code = @"
// This should trigger generating NonNullTypesAttribute
[module: System.Runtime.CompilerServices.NonNullTypes]
namespace System
{
    public class Attribute {}
    public class Void {}
}";

            CreateEmptyCompilation(code).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // (6,18): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     public class Attribute {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Attribute").WithArguments("System.Object").WithLocation(6, 18),
                // (7,18): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     public class Void {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Void").WithArguments("System.Object").WithLocation(7, 18),
                // (6,18): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                //     public class Attribute {}
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Attribute").WithArguments("object", "0").WithLocation(6, 18),
                // (7,18): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                //     public class Void {}
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Void").WithArguments("object", "0").WithLocation(7, 18));
        }

        [Fact]
        public void SynthesizingAttributeRequiresSystemAttribute_NoSystemVoid()
        {
            var code = @"
// This should trigger generating NonNullTypesAttribute
[module: System.Runtime.CompilerServices.NonNullTypes]
namespace System
{
    public class Attribute {}
    public class Object {}
}";

            CreateEmptyCompilation(code).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // (3,10): error CS0518: Predefined type 'System.Void' is not defined or imported
                // [module: System.Runtime.CompilerServices.NonNullTypes]
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "System.Runtime.CompilerServices.NonNullTypes").WithArguments("System.Void").WithLocation(3, 10),
                // (6,18): error CS0518: Predefined type 'System.Void' is not defined or imported
                //     public class Attribute {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Attribute").WithArguments("System.Void").WithLocation(6, 18));
        }
    }
}
