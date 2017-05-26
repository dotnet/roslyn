// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_IsByRefLike : CSharpTestBase
    {
        [Fact]
        public void IsByRefLikeIsWrittenToMetadata_SameAssembly()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}
class Test
{
    public ref struct S1 {}
}
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");
                Assert.True(type.IsByRefLikeType);

                AssertReferencedIsByRefLikeAttribute(Accessibility.Public, type.GetAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void IsByRefLikeIsWrittenToMetadata_NeedsToBeGenerated()
        {
            var text = @"
ref struct S1{}
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("S1");
                Assert.True(type.IsByRefLikeType);

                AssertReferencedIsByRefLikeAttribute(Accessibility.Internal, type.GetAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void IsByRefLikeIsWrittenToMetadata_NeedsToBeGeneratedNested()
        {
            var text = @"
class Test
{
    public ref struct S1 {}
}
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");
                Assert.True(type.IsByRefLikeType);

                AssertReferencedIsByRefLikeAttribute(Accessibility.Internal, type.GetAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void IsByRefLikeIsWrittenToMetadata_NeedsToBeGeneratedGeneric()
        {
            var text = @"
class Test
{
    public ref struct S1<T> {}
}
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test+S1`1");
                Assert.True(type.IsByRefLikeType);

                AssertReferencedIsByRefLikeAttribute(Accessibility.Internal, type.GetAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void IsByRefLikeIsWrittenToMetadata_NeedsToBeGeneratedNestedInGeneric()
        {
            var text = @"
class Test<T>
{
    public ref struct S1 {}
}
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test`1").GetTypeMember("S1");
                Assert.True(type.IsByRefLikeType);

                AssertReferencedIsByRefLikeAttribute(Accessibility.Internal, type.GetAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void IsByRefLikeIsWrittenToMetadata_DifferentAssembly()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateStandardCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
class Test
{
     public ref struct S1 {}
}
";

            CompileAndVerify(codeB, verify: false, additionalRefs: new[] { referenceA }, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");
                Assert.True(type.IsByRefLikeType);

                AssertReferencedIsByRefLikeAttribute(Accessibility.Public, type.GetAttributes(), referenceA.Compilation.AssemblyName);
                AssertNoIsByRefLikeAttributeExists(module.ContainingAssembly);
            });
        }

        [Fact]
        public void IsByRefLikeAttributeIsDisallowedEverywhereInSource_Delegates()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateStandardCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

[IsByRefLike]
public delegate ref readonly int D([IsByRefLike]ref readonly int x);
";

            CreateStandardCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (4,2): error CS8412: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                // [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitIsByRefLikeAttr, "IsByRefLike").WithLocation(4, 2),
                // (5,37): error CS8412: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                // public delegate ref readonly int D([IsByRefLike]ref readonly int x);
                Diagnostic(ErrorCode.ERR_ExplicitIsByRefLikeAttr, "IsByRefLike").WithLocation(5, 37));
        }

        [Fact]
        public void IsByRefLikeAttributeIsDisallowedEverywhereInSource_Types()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateStandardCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

[IsByRefLike]
public class Test
{
}
";

            CreateStandardCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (4,2): error CS8412: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                // [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitIsByRefLikeAttr, "IsByRefLike").WithLocation(4, 2));
        }

        [Fact]
        public void IsByRefLikeAttributeIsDisallowedEverywhereInSource_Fields()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateStandardCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsByRefLike]
    private int x = 0;

    public int X => x;
}
";

            CreateStandardCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8412: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitIsByRefLikeAttr, "IsByRefLike").WithLocation(6, 6));
        }

        [Fact]
        public void IsByRefLikeAttributeIsDisallowedEverywhereInSource_Properties()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateStandardCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    private int x = 0;

    [IsByRefLike]
    public ref readonly int Property => ref x;
}
";

            CreateStandardCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (8,6): error CS8412: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitIsByRefLikeAttr, "IsByRefLike").WithLocation(8, 6));
        }

        [Fact]
        public void IsByRefLikeAttributeIsDisallowedEverywhereInSource_Methods()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateStandardCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsByRefLike]
    [return: IsByRefLike]
    public ref readonly int Method([IsByRefLike]ref readonly int x)
    {
        return ref x;
    }
}
";

            CreateStandardCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8412: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitIsByRefLikeAttr, "IsByRefLike").WithLocation(6, 6),
                // (7,14): error CS8412: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     [return: IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitIsByRefLikeAttr, "IsByRefLike").WithLocation(7, 14),
                // (8,37): error CS8412: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     public ref readonly int Method([IsByRefLike]ref readonly int x)
                Diagnostic(ErrorCode.ERR_ExplicitIsByRefLikeAttr, "IsByRefLike").WithLocation(8, 37));
        }

        [Fact]
        public void IsByRefLikeAttributeIsDisallowedEverywhereInSource_Indexers()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateStandardCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsByRefLike]
    public ref readonly int this[[IsByRefLike]ref readonly int x] { get { return ref x; } }
}
";

            CreateStandardCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8412: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitIsByRefLikeAttr, "IsByRefLike").WithLocation(6, 6),
                // (7,35): error CS8412: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     public ref readonly int this[[IsByRefLike]ref readonly int x] { get { return ref x; } }
                Diagnostic(ErrorCode.ERR_ExplicitIsByRefLikeAttr, "IsByRefLike").WithLocation(7, 35));
        }

        [Fact]
        public void UserReferencingIsByRefLikeAttributeShouldResultInAnError()
        {
            var code = @"
[IsByRefLike]
public class Test
{
	ref struct S1{}
}";

            CreateStandardCompilation(code).VerifyDiagnostics(
                // (2,2): error CS0246: The type or namespace name 'IsByRefLikeAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [IsByRefLike]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IsByRefLike").WithArguments("IsByRefLikeAttribute").WithLocation(2, 2),
                // (2,2): error CS0246: The type or namespace name 'IsByRefLike' could not be found (are you missing a using directive or an assembly reference?)
                // [IsByRefLike]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IsByRefLike").WithArguments("IsByRefLike").WithLocation(2, 2)
                );
        }

        [Fact]
        public void TypeReferencingAnotherTypeThatUsesAPublicIsByRefLikeAttributeFromAThirdNotReferencedAssemblyShouldGenerateItsOwn()
        {
            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            var code1 = CreateStandardCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}");

            var code2 = CreateStandardCompilation(@"
public class Test1
{
	public ref struct S1{}
}", references: new[] { code1.ToMetadataReference() }, options: options);

            CompileAndVerify(code2, verify: false, symbolValidator: module =>
            {
                // IsByRefLike is not generated in assembly
                var isByRefLikeAttributeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute);
                Assert.Null(module.ContainingAssembly.GetTypeByMetadataName(isByRefLikeAttributeName));
            });

            var code3 = CreateStandardCompilation(@"
public class Test2
{
	public ref struct S1{}
}", references: new[] { code2.ToMetadataReference() }, options: options);

            CompileAndVerify(code3, symbolValidator: module =>
            {
                // IsByRefLike is generated in assembly
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.IsByRefLikeAttribute.FullName);
            });
        }

        [Fact]
        public void BuildingAModuleRequiresIsByRefLikeAttributeToBeThere_Missing_SourceMethod()
        {
            var code = @"
public ref struct S1{}
";

            CreateStandardCompilation(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (2,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsByRefLikeAttribute' is not defined or imported
                // public ref struct S1{}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "S1").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(2, 19)
                );
        }

        [Fact]
        public void BuildingAModuleRequiresIsByRefLikeAttributeToBeThere_Missing_SourceMethod_MultipleLocations()
        {
            var code = @"
public class Test
{
    public ref struct S1{}
    public ref struct S2{}
}";

            CreateStandardCompilation(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (5,23): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsByRefLikeAttribute' is not defined or imported
                //     public ref struct S2{}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "S2").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(5, 23),
                // (4,23): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsByRefLikeAttribute' is not defined or imported
                //     public ref struct S1{}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "S1").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(4, 23)
                );
        }

        [Fact]
        public void BuildingAModuleRequiresIsByRefLikeAttributeToBeThere_InAReference()
        {
            var reference = CreateStandardCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}").ToMetadataReference();

            var code = @"
public class Test
{
    public ref struct S1{}
}";

            CompileAndVerify(code, verify: false, additionalRefs: new[] { reference }, options: TestOptions.ReleaseModule, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");
                Assert.True(type.IsByRefLikeType);

                AssertReferencedIsByRefLikeAttribute(Accessibility.Public, type.GetAttributes(), reference.Display);
                AssertNoIsByRefLikeAttributeExists(module.ContainingAssembly);
            });
        }

        [Fact]
        public void ReferencingAnEmbeddedIsByRefLikeAttributeDoesNotUseIt_InternalsVisible()
        {
            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            var code1 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Assembly2"")]
public class Test1
{
	public ref struct S1{}
}";

            var comp1 = CompileAndVerify(code1, options: options, verify: false, symbolValidator: module =>
            {
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.IsByRefLikeAttribute.FullName);
            });

            var code2 = @"
public class Test2
{
	public ref struct S1{}
}";

            CompileAndVerify(code2, options: options.WithModuleName("Assembly2"), additionalRefs: new[] { comp1.Compilation.ToMetadataReference() }, symbolValidator: module =>
            {
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.IsByRefLikeAttribute.FullName);
            });
        }

        [Fact]
        public void IfIsByRefLikeAttributeIsDefinedThenEmbeddedIsNotGenerated()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}
class Test
{
    public ref struct S1{}
}
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                Assert.Null(module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName));
            });
        }

        [Fact]
        public void IsByRefLikeAttributeExistsWithWrongConstructorSignature_NetModule()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute
    {
        public IsByRefLikeAttribute(int p) { }
    }
}
class Test
{
    public ref struct S1{}
}";

            CreateStandardCompilation(text, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (11,23): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsByRefLikeAttribute..ctor'
                //     public ref struct S1{}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "S1").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute", ".ctor").WithLocation(11, 23)
                );
        }

        [Fact]
        public void IsByRefLikeAttributeExistsWithWrongConstructorSignature_Assembly()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute
    {
        public IsByRefLikeAttribute(int p) { }
    }
}
class Test
{
   public ref struct S1{}
}";

            CreateStandardCompilation(text).VerifyEmitDiagnostics(
                // (11,22): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsByRefLikeAttribute..ctor'
                //    public ref struct S1{}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "S1").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute", ".ctor").WithLocation(11, 22)
                );
        }

        [Fact]
        public void IsByRefLikeAttributeExistsWithWrongConstructorSignature_PrivateConstructor()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute
    {
        private IsByRefLikeAttribute() { }
    }
}
class Test
{
    public ref struct S1{}
}";

            CreateStandardCompilation(text).VerifyEmitDiagnostics(
                // (11,23): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsByRefLikeAttribute..ctor'
                //     public ref struct S1{}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "S1").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute", ".ctor").WithLocation(11, 23)
                );
        }

        [Fact]
        public void IsByRefLikeAttributesInNoPia()
        {
            var comAssembly = CreateStandardCompilation(@"
using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""test.dll"")]
[assembly: Guid(""32A961ED-A399-4BBA-B09C-99B7BA297A5C"")]
[ComImport()]
[Guid(""32A961ED-A399-4BBA-B09C-99B7BA297A5C"")]
public interface Test
{
    S1 Property { get; }
    S1 Method(S1 x);
}

public ref struct S1{}
");

            CompileAndVerify(comAssembly, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");

                var property = type.GetMember<PEPropertySymbol>("Property");
                Assert.NotNull(property);
                AssertReferencedIsByRefLikeAttribute(Accessibility.Internal, property.Type.GetAttributes(), module.ContainingAssembly.Name);
            });

            var code = @"
class User
{
    public void M(Test p)
    {
        p.Method(p.Property);
    }
}";


            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            var compilation_CompilationReference = CreateStandardCompilation(code, options: options, references: new[] { comAssembly.ToMetadataReference(embedInteropTypes: true) });
            CompileAndVerify(compilation_CompilationReference, symbolValidator: symbolValidator);

            var compilation_BinaryReference = CreateStandardCompilation(code, options: options, references: new[] { comAssembly.EmitToImageReference(embedInteropTypes: true) });
            CompileAndVerify(compilation_BinaryReference, symbolValidator: symbolValidator);

            void symbolValidator(ModuleSymbol module)
            {
                // No attribute is copied
                AssertNoIsByRefLikeAttributeExists(module.ContainingAssembly);

                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");

                var property = type.GetMember<PEPropertySymbol>("Property");
                Assert.NotNull(property);
                AssertNotReferencedIsByRefLikeAttribute(property.Type.GetAttributes());
            }
        }

        [Fact]
        public void MissingRequiredConstructorWillReportErrorsOnApproriateSyntax_IsByRefLike()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute
    {
        public IsByRefLikeAttribute(int p) { }
    }
}
public class Test
{
    public ref struct S1{}
}";

            CreateStandardCompilation(text).VerifyEmitDiagnostics(
                // (11,23): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsByRefLikeAttribute..ctor'
                //     public ref struct S1{}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "S1").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute", ".ctor").WithLocation(11, 23)
                );
        }

        private static void AssertReferencedIsByRefLikeAttribute(Accessibility accessibility, ImmutableArray<CSharpAttributeData> attributes, string assemblyName)
        {
            var attributeType = attributes.Single().AttributeClass;
            Assert.Equal("IsByRefLikeAttribute", attributeType.Name);
            Assert.Equal(assemblyName, attributeType.ContainingAssembly.Name);
            Assert.Equal(accessibility, attributeType.DeclaredAccessibility);
        }

        private static void AssertNotReferencedIsByRefLikeAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            foreach(var attr in attributes)
            {
                Assert.NotEqual("IsByRefLikeAttribute", attr.AttributeClass.Name);
            }
        }

        private static void AssertNoIsByRefLikeAttributeExists(AssemblySymbol assembly)
        {
            var isByRefLikeAttributeTypeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute);
            Assert.Null(assembly.GetTypeByMetadataName(isByRefLikeAttributeTypeName));
        }

        private static void AssertGeneratedEmbeddedAttribute(AssemblySymbol assembly, string expectedTypeName)
        {
            var typeSymbol = assembly.GetTypeByMetadataName(expectedTypeName);
            Assert.NotNull(typeSymbol);
            Assert.Equal(Accessibility.Internal, typeSymbol.DeclaredAccessibility);

            var attributes = typeSymbol.GetAttributes().OrderBy(attribute => attribute.AttributeClass.Name).ToArray();
            Assert.Equal(2, attributes.Length);

            Assert.Equal(WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute), attributes[0].AttributeClass.ToDisplayString());
            Assert.Equal(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName, attributes[1].AttributeClass.ToDisplayString());
        }
    }
}
