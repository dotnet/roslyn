// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_NonNullTypes : CSharpTestBase
    {
        [Fact]
        public void EmbeddedAttributeInAddedModule()
        {
            var module = CreateCompilation(@"
namespace Microsoft.CodeAnalysis
{
    public class EmbeddedAttribute : System.Attribute { }
}
", options: TestOptions.ReleaseModule);

            var reference = ModuleMetadata.CreateFromImage(module.EmitToArray()).GetReference();

            var comp = CreateCompilation("", options: WithNonNullTypesTrue(), references: new[] { reference });
            comp.VerifyEmitDiagnostics(
                // error CS0101: The namespace 'Microsoft.CodeAnalysis' already contains a definition for 'EmbeddedAttribute'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("EmbeddedAttribute", "Microsoft.CodeAnalysis").WithLocation(1, 1)
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/29732 Crashing")]
        public void EmbeddedAttributeInAddedModule_Injected()
        {
            var code = "[module: System.Runtime.CompilerServices.NonNullTypes]";

            var module = CreateCompilation(code, options: TestOptions.ReleaseModule);
            var reference = ModuleMetadata.CreateFromImage(module.EmitToArray()).GetReference();

            var comp = CreateCompilation(code, references: new[] { reference });
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void NonNullTypesAttributeNamespace()
        {
            var code = @"
namespace System.Runtime.CompilerServices.NonNullTypesAttribute
{
    class C { }
}

[System.Runtime.CompilerServices.NonNullTypes]
internal class TestType1 { }

[System.Runtime.CompilerServices.NonNullTypesAttribute(false)]
internal class TestType2 { }
";

            var comp = CreateCompilation(code);
            comp.VerifyEmitDiagnostics(
                // (7,34): error CS0616: 'System.Runtime.CompilerServices.NonNullTypesAttribute' is not an attribute class
                // [System.Runtime.CompilerServices.NonNullTypes]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "NonNullTypes").WithArguments("System.Runtime.CompilerServices.NonNullTypesAttribute").WithLocation(7, 34),
                // (10,34): error CS0616: 'System.Runtime.CompilerServices.NonNullTypesAttribute' is not an attribute class
                // [System.Runtime.CompilerServices.NonNullTypesAttribute(false)]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "NonNullTypesAttribute").WithArguments("System.Runtime.CompilerServices.NonNullTypesAttribute").WithLocation(10, 34)
                );
            Assert.Equal(SymbolKind.Namespace, comp.GlobalNamespace.GetMember("System.Runtime.CompilerServices.NonNullTypesAttribute").Kind);
        }

        [Fact]
        public void NonNullTypesAttributeNamespace_WithoutUsage()
        {
            var code = @"
namespace System.Runtime.CompilerServices.NonNullTypesAttribute
{
    class C { }
}
";

            var comp = CreateCompilation(code);
            comp.VerifyEmitDiagnostics();
            Assert.Equal(SymbolKind.Namespace, comp.GlobalNamespace.GetMember("System.Runtime.CompilerServices.NonNullTypesAttribute").Kind);
        }

        [Fact]
        public void ReferencingNonNullTypesAttributesFromTheSameAssemblyAllowed()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    internal class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}

" + NonNullTypesOn() + @"
internal class TestType1 { }

" + NonNullTypesOff() + @"
internal class TestType2 { }
";

            var comp = CreateCompilation(code);
            comp.VerifyEmitDiagnostics();
            Assert.True(comp.GetMember("TestType1").NonNullTypes);
            Assert.False(comp.GetMember("TestType2").NonNullTypes);

            CompileAndVerify(comp, symbolValidator:
                (m) =>
                {
                    Assert.Same(m, m.GlobalNamespace.GetMember("TestType1").GetAttributes().Single().AttributeClass.ContainingModule);
                    Assert.Same(m, m.GlobalNamespace.GetMember("TestType2").GetAttributes().Single().AttributeClass.ContainingModule);
                    Assert.True(m.GlobalNamespace.GetMember("TestType1").NonNullTypes);
                    Assert.False(m.GlobalNamespace.GetMember("TestType2").NonNullTypes);
                }
                );

        }

        [Fact]
        public void CannotReferenceNonNullTypesAttributesFromADifferentAssembly_Internal()
        {
            var reference = CreateCompilation(@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Source"")]
namespace System.Runtime.CompilerServices
{
    internal class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}

" + NonNullTypesOn() + @"
internal class TestType1 { }

" + NonNullTypesOn() + @"
internal class TestType2 { }
");
            Assert.Empty(reference.GetMember("TestType1").GetAttributes());
            Assert.Empty(reference.GetMember("TestType2").GetAttributes());

            CompileAndVerify(reference, symbolValidator:
                (m) =>
                {
                    Assert.Same(m, m.GlobalNamespace.GetMember("TestType1").GetAttributes().Single().AttributeClass.ContainingModule);
                    Assert.Same(m, m.GlobalNamespace.GetMember("TestType2").GetAttributes().Single().AttributeClass.ContainingModule);
                }
                );

            // NonNullTypesAttribute from referenced assembly is ignored, and we use the injected one instead
            var comp = CreateCompilation("", options: WithNonNullTypesTrue(), references: new[] { reference.ToMetadataReference() }, assemblyName: "Source");
            comp.VerifyDiagnostics();
            Assert.Empty(comp.SourceModule.GetAttributes());
            Assert.True(comp.SourceModule.NonNullTypes);

            CompileAndVerify(comp, symbolValidator:
                (m) =>
                {
                    Assert.Same(m, m.GetAttributes().Single().AttributeClass.ContainingModule);
                    Assert.True(m.NonNullTypes);
                }
                );

            var system = (INamespaceSymbol)comp.GlobalNamespace.GetMember("System");
            Assert.Equal(NamespaceKind.Compilation, system.NamespaceKind);
        }

        [Fact]
        public void CannotReferenceNonNullTypesAttributesFromADifferentAssembly_Module()
        {
            var module = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    internal class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}

" + NonNullTypesOn() + @"
internal class TestType1 { }
", options: TestOptions.ReleaseModule);

            var reference = ModuleMetadata.CreateFromImage(module.EmitToArray()).GetReference();

            var system = (INamespaceSymbol)module.GlobalNamespace.GetMember("System");
            Assert.Equal(NamespaceKind.Compilation, system.NamespaceKind);

            // NonNullTypesAttribute from module conflicts with injected symbol
            var comp = CreateCompilation("", references: new[] { reference }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // error CS0101: The namespace 'System.Runtime.CompilerServices' already contains a definition for 'NonNullTypesAttribute'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("NonNullTypesAttribute", "System.Runtime.CompilerServices").WithLocation(1, 1)
                );

            system = (INamespaceSymbol)comp.GlobalNamespace.GetMember("System");
            Assert.Equal(NamespaceKind.Compilation, system.NamespaceKind);
        }

        [Fact]
        public void CannotReferenceNonNullTypesAttributesFromADifferentAssembly_Public()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}

" + NonNullTypesOn() + @"
public class TestType1 { }

" + NonNullTypesOff() + @"
public class TestType2 { }
");
            Assert.Empty(reference.GetMember("TestType1").GetAttributes());
            Assert.Empty(reference.GetMember("TestType2").GetAttributes());

            CompileAndVerify(reference, symbolValidator:
            (m) =>
            {
                Assert.Same(m, m.GlobalNamespace.GetMember("TestType1").GetAttributes().Single().AttributeClass.ContainingModule);
                Assert.Same(m, m.GlobalNamespace.GetMember("TestType2").GetAttributes().Single().AttributeClass.ContainingModule);
            });

            // NonNullTypesAttribute from referenced assembly is ignored, and we use the injected one instead
            var comp = CreateCompilation("", references: new[] { reference.ToMetadataReference() }, options: WithNonNullTypesTrue());

            Assert.Empty(comp.SourceModule.GetAttributes());

            CompileAndVerify(comp, symbolValidator:
            (m) =>
            {
                Assert.Same(m, m.GetAttributes().Single().AttributeClass.ContainingModule);
                Assert.True(m.NonNullTypes);
            });
        }

        [Fact]
        public void CanReferenceNonNullTypesAttributesFromADifferentAssemblyViaExternDeclaration()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}

[System.Runtime.CompilerServices.NonNullTypes]
public class TestType1 { }
");
            Assert.False(reference.GetMember("TestType1").GetAttributes().Single().AttributeClass.IsImplicitlyDeclared);

            var code = @"
extern alias Reference;
[module: Reference::System.Runtime.CompilerServices.NonNullTypes]
class C
{
    string? M() => throw null;
}
";

            var comp = CreateCompilation(code, references: new[] { reference.ToMetadataReference(aliases: ImmutableArray.Create("Reference")) });
            comp.VerifyDiagnostics(
                // (3,10): error CS8635: Explicit application of 'System.Runtime.CompilerServices.NonNullTypesAttribute' is not allowed.
                // [module: Reference::System.Runtime.CompilerServices.NonNullTypes]
                Diagnostic(ErrorCode.ERR_ExplicitNonNullTypesAttribute, "Reference::System.Runtime.CompilerServices.NonNullTypes").WithLocation(3, 10),
                // (6,11): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //     string? M() => throw null;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(6, 11)
                );

            Assert.False(comp.SourceModule.GetAttributes().Single().AttributeClass.IsImplicitlyDeclared);
            Assert.Null(comp.SourceModule.NonNullTypes);
        }

        [Fact]
        public void NonNullTypesAttributeInSourceCanBeUsed()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public class NonNullTypesAttribute : System.Attribute { public NonNullTypesAttribute(bool flag = true) { } }
}";

            var comp = CreateCompilation(code, options: WithNonNullTypesTrue());
            comp.VerifyEmitDiagnostics();
            Assert.True(comp.SourceModule.NonNullTypes);
            Assert.Empty(comp.SourceModule.GetAttributes());
            Assert.True(comp.SourceModule.NonNullTypes);

            CompileAndVerify(comp, symbolValidator:
                (m) =>
                {
                    Assert.Same(m, m.GetAttributes().Single().AttributeClass.ContainingModule);
                    Assert.True(m.NonNullTypes);
                });
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

            var code = " /* NonNullTypesAttribute is injected when not present in source */ ";

            // It is okay to have a duplicate of NonNullTypesAttribute when the other comes from a referenced assembly,
            // but we don't allow such collisions when the other comes from an added module.

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



class Test
{
}";

            CreateCompilation(code, references: new[] { reference },
                options: WithNonNullTypesTrue() // This should trigger generating another NonNullTypesAttribute
                ).VerifyEmitDiagnostics(
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
public class D : C { }";

            CompileAndVerify(code, options: WithNonNullTypesTrue(), references: new[] { reference }, symbolValidator: module =>
            {
                var attributeName = AttributeDescription.NonNullTypesAttribute.FullName;

                var referenceAttribute = module.GetReferencedAssemblySymbols().Single(assembly => assembly.Name == "testRef").GetTypeByMetadataName(attributeName);
                Assert.NotNull(referenceAttribute);

                var generatedAttribute = module.ContainingAssembly.GetTypeByMetadataName(attributeName);
                Assert.NotNull(generatedAttribute);

                Assert.False(referenceAttribute.Equals(generatedAttribute));
            });
        }

        [Fact]
        public void SynthesizingAttributeRequiresSystemAttribute_NoSystemAttribute()
        {
            var code = @"


namespace System
{
    public class Object {}
    public class Void {}
}";

            CreateEmptyCompilation(code,
                options: WithNonNullTypesTrue() // This should trigger generating NonNullTypesAttribute
                ).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // error CS0518: Predefined type 'System.Boolean' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Boolean").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Boolean' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Boolean").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
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
}";

            CreateEmptyCompilation(code,
                options: WithNonNullTypesTrue() // This should trigger generating NonNullTypesAttribute
                ).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
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


namespace System
{
    public class Attribute {}
    public class Object {}
}";

            CreateEmptyCompilation(code,
                options: WithNonNullTypesTrue() // This should trigger generating NonNullTypesAttribute
                ).VerifyEmitDiagnostics(CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"),
                // (6,18): error CS0518: Predefined type 'System.Void' is not defined or imported
                //     public class Attribute {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Attribute").WithArguments("System.Void").WithLocation(6, 18));
        }
    }
}
