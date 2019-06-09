// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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

            void validate(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");
                AssertReferencedIsByRefLike(type);

                var peModule = (PEModuleSymbol)module;
                Assert.True(peModule.Module.HasIsByRefLikeAttribute(((PENamedTypeSymbol)type).Handle));
                AssertDeclaresType(peModule, WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute, Accessibility.Public);
            }

            CompileAndVerify(text, verify: Verification.Passes, symbolValidator: validate);
        }

        [Fact]
        public void IsByRefLikeIsWrittenToMetadata_NeedsToBeGenerated()
        {
            var text = @"
ref struct S1{}
";

            CompileAndVerify(text, verify: Verification.Passes, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("S1");
                AssertReferencedIsByRefLike(type);
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

            CompileAndVerify(text, verify: Verification.Passes, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");
                AssertReferencedIsByRefLike(type);
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

            void validate(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test+S1`1");
                AssertReferencedIsByRefLike(type);

                var peModule = (PEModuleSymbol)module;
                Assert.True(peModule.Module.HasIsByRefLikeAttribute(((PENamedTypeSymbol)type).Handle));
                AssertDeclaresType(peModule, WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute, Accessibility.Internal);
            }

            CompileAndVerify(text, symbolValidator: validate);
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

            CompileAndVerify(text, verify: Verification.Passes, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test`1").GetTypeMember("S1");
                AssertReferencedIsByRefLike(type);
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

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
class Test
{
     public ref struct S1 {}
}
";

            CompileAndVerify(codeB, verify: Verification.Passes, references: new[] { referenceA }, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");

                AssertReferencedIsByRefLike(type);
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

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

[IsByRefLike]
public delegate ref readonly int D([IsByRefLike]in int x);
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (4,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                // [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsByRefLike").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(4, 2),
                // (5,37): error CS8335: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                // public delegate ref readonly int D([IsByRefLike]in int x);
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsByRefLike").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(5, 37));
        }

        [Fact]
        public void IsByRefLikeAttributeIsDisallowedEverywhereInSource_Types()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

[IsByRefLike]
public class Test
{
}
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (4,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                // [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsByRefLike").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(4, 2));
        }

        [Fact]
        public void IsByRefLikeAttributeIsDisallowedEverywhereInSource_Fields()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsByRefLike]
    private int x = 0;

    public int X => x;
}
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsByRefLike").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(6, 6));
        }

        [Fact]
        public void IsByRefLikeAttributeIsDisallowedEverywhereInSource_Properties()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    private int x = 0;

    [IsByRefLike]
    public ref readonly int Property => ref x;
}
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (8,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsByRefLike").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(8, 6));
        }

        [Fact]
        public void IsByRefLikeAttributeIsDisallowedEverywhereInSource_Methods()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsByRefLike]
    [return: IsByRefLike]
    public ref readonly int Method([IsByRefLike]in int x)
    {
        return ref x;
    }
}
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsByRefLike").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(6, 6),
                // (7,14): error CS8335: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     [return: IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsByRefLike").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(7, 14),
                // (8,37): error CS8335: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     public ref readonly int Method([IsByRefLike]in int x)
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsByRefLike").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(8, 37));
        }

        [Fact]
        public void IsByRefLikeAttributeIsDisallowedEverywhereInSource_Indexers()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsByRefLike]
    public ref readonly int this[[IsByRefLike]in int x] { get { return ref x; } }
}
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     [IsByRefLike]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsByRefLike").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(6, 6),
                // (7,35): error CS8335: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //     public ref readonly int this[[IsByRefLike]in int x] { get { return ref x; } }
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsByRefLike").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(7, 35));
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

            CreateCompilation(code).VerifyDiagnostics(
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

            var code1 = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}");

            var code2 = CreateCompilation(@"
public class Test1
{
	public ref struct S1{}
}", references: new[] { code1.ToMetadataReference() }, options: options);

            CompileAndVerify(code2, verify: Verification.Passes, symbolValidator: module =>
            {
                // IsByRefLike is not generated in assembly
                var isByRefLikeAttributeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute);
                Assert.Null(module.ContainingAssembly.GetTypeByMetadataName(isByRefLikeAttributeName));
            });

            var code3 = CreateCompilation(@"
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

            CreateCompilation(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
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

            CreateCompilation(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
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
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}").ToMetadataReference();

            var code = @"
public class Test
{
    public ref struct S1{}
}";

            CompileAndVerify(code, verify: Verification.Fails, references: new[] { reference }, options: TestOptions.ReleaseModule, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");

                AssertReferencedIsByRefLike(type);
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

            var comp1 = CompileAndVerify(code1, options: options, verify: Verification.Passes, symbolValidator: module =>
            {
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.IsByRefLikeAttribute.FullName);
            });

            var code2 = @"
public class Test2
{
	public ref struct S1{}
}";

            CompileAndVerify(code2, options: options.WithModuleName("Assembly2"), references: new[] { comp1.Compilation.ToMetadataReference() }, symbolValidator: module =>
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

            CompileAndVerify(text, verify: Verification.Passes, symbolValidator: module =>
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

            CreateCompilation(text, options: TestOptions.ReleaseModule).VerifyDiagnostics(
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

            CreateCompilation(text).VerifyEmitDiagnostics(
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

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (11,23): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsByRefLikeAttribute..ctor'
                //     public ref struct S1{}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "S1").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute", ".ctor").WithLocation(11, 23)
                );
        }

        [Fact]
        public void IsByRefLikeAttributesInNoPia()
        {
            var comAssembly = CreateCompilationWithMscorlib40(@"
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
                AssertReferencedIsByRefLike(property.Type);
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

            var compilation_CompilationReference = CreateCompilationWithMscorlib40(code, options: options, references: new[] { comAssembly.ToMetadataReference(embedInteropTypes: true) });
            CompileAndVerify(compilation_CompilationReference, symbolValidator: symbolValidator);

            var compilation_BinaryReference = CreateCompilationWithMscorlib40(code, options: options, references: new[] { comAssembly.EmitToImageReference(embedInteropTypes: true) });
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

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (11,23): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsByRefLikeAttribute..ctor'
                //     public ref struct S1{}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "S1").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute", ".ctor").WithLocation(11, 23)
                );
        }

        [Fact]
        public void IsByRefLikeObsolete()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}

class Test
{
    [System.Obsolete(""hello"", true)]
    public ref struct S1 {}
}
";

            void validate(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");
                Assert.True(type.IsRefLikeType);

                var assemblyName = module.ContainingAssembly.Name;

                var attribute = type.GetAttributes().Single();
                Assert.Equal("System.ObsoleteAttribute", attribute.AttributeClass.ToDisplayString());
                Assert.Equal("hello", attribute.ConstructorArguments.ElementAt(0).Value);
                Assert.Equal(true, attribute.ConstructorArguments.ElementAt(1).Value);

                if (module is PEModuleSymbol peModule)
                {
                    Assert.True(peModule.Module.HasIsByRefLikeAttribute(((PENamedTypeSymbol)type).Handle));
                    AssertDeclaresType(peModule, WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute, Accessibility.Public);
                }
            };

            CompileAndVerify(text, verify: Verification.Passes, symbolValidator: validate, sourceSymbolValidator: validate);
        }

        [Fact]
        public void IsByRefLikeObsoleteMissing()
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

namespace System
{
    public class ObsoleteAttribute{}
}
";

            CompileAndVerify(text, verify: Verification.Passes, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");
                AssertReferencedIsByRefLike(type, hasObsolete: false);
            });
        }

        [Fact]
        public void IsByRefLikeDeprecated()
        {
            var text = @"
using System;
using Windows.Foundation.Metadata;

namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}

namespace Windows.Foundation.Metadata
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = true)]
    public sealed class DeprecatedAttribute : Attribute
    {
        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version)
        {
        }
    }
    public enum DeprecationType
    {
        Deprecate = 0,
        Remove = 1
    }
}

class Test
{
    [Deprecated(""hello"", DeprecationType.Deprecate, 42)]
    public ref struct S1 {}
}
";

            CompileAndVerify(text, verify: Verification.Passes, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");
                Assert.True(type.IsRefLikeType);

                var attribute = type.GetAttributes().Single();
                Assert.Equal("Windows.Foundation.Metadata.DeprecatedAttribute", attribute.AttributeClass.ToDisplayString());
                Assert.Equal(42u, attribute.ConstructorArguments.ElementAt(2).Value);
            });
        }

        [Fact]
        public void IsByRefLikeDeprecatedAndObsolete()
        {
            var text = @"
using System;
using Windows.Foundation.Metadata;

namespace System.Runtime.CompilerServices
{
    public class IsByRefLikeAttribute : System.Attribute { }
}

namespace Windows.Foundation.Metadata
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = true)]
    public sealed class DeprecatedAttribute : Attribute
    {
        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version)
        {
        }
    }
    public enum DeprecationType
    {
        Deprecate = 0,
        Remove = 1
    }
}

class Test
{
    [Obsolete]
    [Deprecated(""hello"", DeprecationType.Deprecate, 42)]
    public ref struct S1 {}
}
";

            CompileAndVerify(text, verify: Verification.Passes, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test").GetTypeMember("S1");
                Assert.True(type.IsRefLikeType);

                var attributes = type.GetAttributes();

                Assert.Equal(2, attributes.Length);
                Assert.Equal("Windows.Foundation.Metadata.DeprecatedAttribute", attributes[1].AttributeClass.ToDisplayString());

                var attribute = attributes[0];
                Assert.Equal("System.ObsoleteAttribute", attribute.AttributeClass.ToDisplayString());
                Assert.Equal(0, attribute.ConstructorArguments.Count());
            });
        }

        [Fact]
        public void ObsoleteInSource()
        {
            var text = @"

class C1
{
    void Method()
    {
        Test.S1 v1 = default;
        Test.S2 v2 = default;
    }
}

class Test
{
    [System.Obsolete(""Types with embedded references are not supported in this version of your compiler."", true)]
    public struct S1 {}

    [System.Obsolete(""Types with embedded references are not supported in this version of your compiler."", true)]
    public ref struct S2 {}
}
";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (7,9): error CS0619: 'Test.S1' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'
                //         Test.S1 v1 = default;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Test.S1").WithArguments("Test.S1", "Types with embedded references are not supported in this version of your compiler.").WithLocation(7, 9),
                // (8,9): error CS0619: 'Test.S2' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'
                //         Test.S2 v2 = default;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Test.S2").WithArguments("Test.S2", "Types with embedded references are not supported in this version of your compiler.").WithLocation(8, 9),
                // (7,17): warning CS0219: The variable 'v1' is assigned but its value is never used
                //         Test.S1 v1 = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "v1").WithArguments("v1").WithLocation(7, 17),
                // (8,17): warning CS0219: The variable 'v2' is assigned but its value is never used
                //         Test.S2 v2 = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "v2").WithArguments("v2").WithLocation(8, 17)
                );
        }

        [Fact]
        public void ObsoleteHasErrorEqualsTrue()
        {
            var text = @"public ref struct S {}";

            CompileAndVerify(text, verify: Verification.Passes, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("S");
                AssertReferencedIsByRefLike(type);
            });
        }

        [Fact]
        public void ObsoleteInWrongPlaces()
        {

            var libSrc = @"
public class Test
{
    [System.Obsolete(""Types with embedded references are not supported in this version of your compiler."", true)]
    public ref struct S1 {}

    [System.Obsolete(""Types with embedded references are not supported in this version of your compiler."", true)]
    public struct S2 {}

    [System.Obsolete(""Types with embedded references are not supported in this version of your compiler."", true)]
    public static int field;
}
";

            var libComp = CreateCompilation(libSrc);

            var text = @"
class C1
{
    void Method()
    {
        //ok
        Test.S1 v1 = default;
    
        //error not a ref struct
        Test.S2 v2 = default;

        //error not a ref struct
        var x = Test.field;
    }
}
";

            CreateCompilation(text, new[] { libComp.EmitToImageReference() }).VerifyEmitDiagnostics(
                // (10,9): error CS0619: 'Test.S2' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'
                //         Test.S2 v2 = default;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Test.S2").WithArguments("Test.S2", "Types with embedded references are not supported in this version of your compiler.").WithLocation(10, 9),
                // (13,17): error CS0619: 'Test.field' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'
                //         var x = Test.field;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Test.field").WithArguments("Test.field", "Types with embedded references are not supported in this version of your compiler.").WithLocation(13, 17),
                // (7,17): warning CS0219: The variable 'v1' is assigned but its value is never used
                //         Test.S1 v1 = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "v1").WithArguments("v1").WithLocation(7, 17),
                // (10,17): warning CS0219: The variable 'v2' is assigned but its value is never used
                //         Test.S2 v2 = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "v2").WithArguments("v2").WithLocation(10, 17)
            );

            CreateCompilation(text, new[] { libComp.ToMetadataReference() }).VerifyEmitDiagnostics(
                // (7,9): error CS0619: 'Test.S1' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'
                //         Test.S1 v1 = default;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Test.S1").WithArguments("Test.S1", "Types with embedded references are not supported in this version of your compiler.").WithLocation(7, 9),
                // (10,9): error CS0619: 'Test.S2' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'
                //         Test.S2 v2 = default;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Test.S2").WithArguments("Test.S2", "Types with embedded references are not supported in this version of your compiler.").WithLocation(10, 9),
                // (13,17): error CS0619: 'Test.field' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'
                //         var x = Test.field;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Test.field").WithArguments("Test.field", "Types with embedded references are not supported in this version of your compiler.").WithLocation(13, 17),
                // (7,17): warning CS0219: The variable 'v1' is assigned but its value is never used
                //         Test.S1 v1 = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "v1").WithArguments("v1").WithLocation(7, 17),
                // (10,17): warning CS0219: The variable 'v2' is assigned but its value is never used
                //         Test.S2 v2 = default;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "v2").WithArguments("v2").WithLocation(10, 17)
            );
        }

        [WorkItem(22198, "https://github.com/dotnet/roslyn/issues/22198")]
        [Fact]
        public void SpecialTypes_CorLib()
        {
            var source1 =
@"
namespace System
{
    public class Object { }
    public class String { }
    public struct Void { }
    public class ValueType { }
    public struct Int32 { }
    public struct Boolean { }
    public struct Decimal { }
    public class Attribute{ }
    public class ObsoleteAttribute: Attribute
    {
        public ObsoleteAttribute(string message, bool error){}
    }

    public ref struct TypedReference { }
    public ref struct ArgIterator { }
    public ref struct RuntimeArgumentHandle { }

    public ref struct NotTypedReference { }
}";
            var compilation1 = CreateEmptyCompilation(source1, assemblyName: GetUniqueName());

            CompileAndVerify(compilation1, verify: Verification.Fails, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("System.TypedReference");
                AssertReferencedIsByRefLike(type, hasObsolete: false);

                type = module.ContainingAssembly.GetTypeByMetadataName("System.ArgIterator");
                AssertReferencedIsByRefLike(type, hasObsolete: false);

                type = module.ContainingAssembly.GetTypeByMetadataName("System.RuntimeArgumentHandle");
                AssertReferencedIsByRefLike(type, hasObsolete: false);

                // control case. Not a special type.
                type = module.ContainingAssembly.GetTypeByMetadataName("System.NotTypedReference");
                AssertReferencedIsByRefLike(type, hasObsolete: true);
            });
        }

        [Fact]
        public void SpecialTypes_NotCorLib()
        {
            var text = @"
namespace System
{
    public ref struct TypedReference { }
}
";

            CompileAndVerify(text, verify: Verification.Passes, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("System.TypedReference");

                AssertReferencedIsByRefLike(type);
            });
        }

        private static void AssertReferencedIsByRefLike(TypeSymbol type, bool hasObsolete = true)
        {
            var peType = (PENamedTypeSymbol)type;
            Assert.True(peType.IsRefLikeType);

            // there is no [Obsolete] or [IsByRef] attribute returned
            Assert.Empty(peType.GetAttributes());

            var peModule = (PEModuleSymbol)peType.ContainingModule;
            var obsoleteAttribute = peModule.Module.TryGetDeprecatedOrExperimentalOrObsoleteAttribute(peType.Handle, ignoreByRefLikeMarker: false);

            if (hasObsolete)
            {
                Assert.NotNull(obsoleteAttribute);
                Assert.Equal("Types with embedded references are not supported in this version of your compiler.", obsoleteAttribute.Message);
                Assert.Equal(true, obsoleteAttribute.IsError);
            }
            else
            {
                Assert.Null(obsoleteAttribute);
            }
        }

        private static void AssertNotReferencedIsByRefLikeAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            foreach (var attr in attributes)
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
