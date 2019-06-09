// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_IsUnmanaged : CSharpTestBase
    {
        [Fact]
        public void AttributeUsedIfExists_FromSource_Method()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}
public class Test
{
    public void M<T>() where T : unmanaged { }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                Assert.Null(module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName));
                AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void AttributeUsedIfExists_FromSource_Class()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}
public class Test<T> where T : unmanaged
{
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test`1").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                Assert.Null(module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName));
                AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void AttributeUsedIfExists_FromSource_LocalFunction()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}
public class Test
{
    public void M()
    {
        void N<T>(T arg) where T : unmanaged
        {
        }
    }
}
";

            CompileAndVerify(text, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__N|0_0").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                Assert.Null(module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName));
                AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, module.ContainingAssembly.Name);
            });
        }


        [Fact]
        public void AttributeUsedIfExists_FromSource_Delegate()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}
public delegate void D<T>() where T : unmanaged;
";

            CompileAndVerify(text, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                var typeParameter = module.GlobalNamespace.GetTypeMember("D").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                Assert.Null(module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName));
                AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void AttributeUsedIfExists_FromReference_Method_Reference()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}").EmitToImageReference();

            var text = @"
public class Test
{
    public void M<T>() where T : unmanaged { }
}
";

            CompileAndVerify(text, references: new[] { reference }, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, reference.Display);
                AssertNoIsUnmanagedAttributeExists(module.ContainingAssembly);
            });
        }

        [Fact]
        public void AttributeUsedIfExists_FromReference_Class_Reference()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}").EmitToImageReference();

            var text = @"
public class Test<T> where T : unmanaged
{
}
";

            CompileAndVerify(text, references: new[] { reference }, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test`1").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, reference.Display);
                AssertNoIsUnmanagedAttributeExists(module.ContainingAssembly);
            });
        }

        [Fact]
        public void AttributeUsedIfExists_FromReference_LocalFunction_Reference()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}").EmitToImageReference();

            var text = @"
public class Test
{
    public void M()
    {
        void N<T>() where T : unmanaged
        {
        }
    }
}
";

            CompileAndVerify(
                source: text,
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
                references: new[] { reference },
                symbolValidator: module =>
                {
                    var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__N|0_0").TypeParameters.Single();
                    Assert.True(typeParameter.HasValueTypeConstraint);
                    Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                    AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, reference.Display);
                    AssertNoIsUnmanagedAttributeExists(module.ContainingAssembly);
                });
        }

        [Fact]
        public void AttributeUsedIfExists_FromReference_Delegate_Reference()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}").EmitToImageReference();

            var text = @"
public delegate void D<T>() where T : unmanaged;
";

            CompileAndVerify(
                source: text,
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
                references: new[] { reference },
                symbolValidator: module =>
                {
                    var typeParameter = module.GlobalNamespace.GetTypeMember("D").TypeParameters.Single();
                    Assert.True(typeParameter.HasValueTypeConstraint);
                    Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                    AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, reference.Display);
                    AssertNoIsUnmanagedAttributeExists(module.ContainingAssembly);
                });
        }

        [Fact]
        public void AttributeUsedIfExists_FromReference_Method_Module()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}").EmitToImageReference();

            var text = @"
public class Test
{
    public void M<T>() where T : unmanaged { }
}
";

            CompileAndVerify(text, verify: Verification.Fails, references: new[] { reference }, options: TestOptions.ReleaseModule, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, reference.Display);
                AssertNoIsUnmanagedAttributeExists(module.ContainingAssembly);
            });
        }

        [Fact]
        public void AttributeUsedIfExists_FromReference_Class_Module()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}").EmitToImageReference();

            var text = @"
public class Test<T> where T : unmanaged
{
}
";

            CompileAndVerify(text, verify: Verification.Fails, references: new[] { reference }, options: TestOptions.ReleaseModule, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test`1").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, reference.Display);
                AssertNoIsUnmanagedAttributeExists(module.ContainingAssembly);
            });
        }

        [Fact]
        public void AttributeUsedIfExists_FromReference_LocalFunction_Module()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}").EmitToImageReference();

            var text = @"
public class Test
{
    public void M()
    {
        void N<T>() where T : unmanaged
        {
        }
    }
}
";

            CompileAndVerify(
                source: text,
                verify: Verification.Fails,
                references: new[] { reference },
                options: TestOptions.ReleaseModule.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__N|0_0").TypeParameters.Single();
                    Assert.True(typeParameter.HasValueTypeConstraint);
                    Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                    AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, reference.Display);
                    AssertNoIsUnmanagedAttributeExists(module.ContainingAssembly);
                });
        }

        [Fact]
        public void AttributeUsedIfExists_FromReference_Delegate_Module()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}").EmitToImageReference();

            var text = @"
public delegate void D<T>() where T : unmanaged;
";

            CompileAndVerify(
                source: text,
                verify: Verification.Fails,
                references: new[] { reference },
                options: TestOptions.ReleaseModule.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var typeParameter = module.GlobalNamespace.GetTypeMember("D").TypeParameters.Single();
                    Assert.True(typeParameter.HasValueTypeConstraint);
                    Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                    AssertReferencedIsUnmanagedAttribute(Accessibility.Public, typeParameter, reference.Display);
                    AssertNoIsUnmanagedAttributeExists(module.ContainingAssembly);
                });
        }

        [Fact]
        public void AttributeGeneratedIfNotExists_FromSource_Method()
        {
            var text = @"
public class Test
{
    public void M<T>() where T : unmanaged { }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void AttributeGeneratedIfNotExists_FromSource_Class()
        {
            var text = @"
public class Test<T> where T : unmanaged
{
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test`1").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void AttributeGeneratedIfNotExists_FromSource_LocalFunction()
        {
            var text = @"
public class Test
{
    public void M()
    {
        void N<T>() where T : unmanaged { }
        {
        }
    }
}
";

            CompileAndVerify(
                source: text,
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__N|0_0").TypeParameters.Single();
                    Assert.True(typeParameter.HasValueTypeConstraint);
                    Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                    AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
                });
        }

        [Fact]
        public void AttributeGeneratedIfNotExists_FromSource_Delegate()
        {
            var text = @"
public delegate void D<T>() where T : unmanaged;
";

            CompileAndVerify(
                source: text,
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var typeParameter = module.GlobalNamespace.GetTypeMember("D").TypeParameters.Single();
                    Assert.True(typeParameter.HasValueTypeConstraint);
                    Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                    AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
                });
        }

        [Fact]
        public void IsUnmanagedAttributeIsDisallowedEverywhereInSource_Delegates()
        {
            var code = @"
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}

[IsUnmanaged]
public delegate void D([IsUnmanaged]int x);
";

            CreateCompilation(code).VerifyDiagnostics(
                // (9,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsUnmanagedAttribute'. This is reserved for compiler usage.
                // [IsUnmanaged]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsUnmanaged").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(9, 2),
                // (10,25): error CS8335: Do not use 'System.Runtime.CompilerServices.IsUnmanagedAttribute'. This is reserved for compiler usage.
                // public delegate void D([IsUnmanaged]int x);
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsUnmanaged").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(10, 25));
        }

        [Fact]
        public void IsUnmanagedAttributeIsDisallowedEverywhereInSource_Types()
        {
            var code = @"
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}

[IsUnmanaged]
public class Test
{
}
";

            CreateCompilation(code).VerifyDiagnostics(
                // (9,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsUnmanagedAttribute'. This is reserved for compiler usage.
                // [IsUnmanaged]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsUnmanaged").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(9, 2));
        }

        [Fact]
        public void IsUnmanagedAttributeIsDisallowedEverywhereInSource_Fields()
        {
            var code = @"
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}

public class Test
{
    [IsUnmanaged]
    public int x = 0;
}
";

            CreateCompilation(code).VerifyDiagnostics(
                // (11,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsUnmanagedAttribute'. This is reserved for compiler usage.
                //     [IsUnmanaged]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsUnmanaged").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(11, 6));
        }

        [Fact]
        public void IsUnmanagedAttributeIsDisallowedEverywhereInSource_Properties()
        {
            var code = @"
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}

public class Test
{
    [IsUnmanaged]
    public int Property => 0;
}
";

            CreateCompilation(code).VerifyDiagnostics(
                // (11,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsUnmanagedAttribute'. This is reserved for compiler usage.
                //     [IsUnmanaged]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsUnmanaged").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(11, 6));
        }

        [Fact]
        public void IsUnmanagedAttributeIsDisallowedEverywhereInSource_Methods()
        {
            var code = @"
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}

public class Test
{
    [IsUnmanaged]
    [return: IsUnmanaged]
    public int Method([IsUnmanaged]int x)
    {
        return x;
    }
}
";

            CreateCompilation(code).VerifyDiagnostics(
                // (11,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsUnmanagedAttribute'. This is reserved for compiler usage.
                //     [IsUnmanaged]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsUnmanaged").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(11, 6),
                // (12,14): error CS8335: Do not use 'System.Runtime.CompilerServices.IsUnmanagedAttribute'. This is reserved for compiler usage.
                //     [return: IsUnmanaged]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsUnmanaged").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(12, 14),
                // (13,24): error CS8335: Do not use 'System.Runtime.CompilerServices.IsUnmanagedAttribute'. This is reserved for compiler usage.
                //     public int Method([IsUnmanaged]int x)
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsUnmanaged").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(13, 24));
        }

        [Fact]
        public void IsUnmanagedAttributeIsDisallowedEverywhereInSource_Indexers()
        {
            var code = @"
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}

public class Test
{
    [IsUnmanaged]
    public int this[[IsUnmanaged]int x] => x;
}
";

            CreateCompilation(code).VerifyDiagnostics(
                // (11,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsUnmanagedAttribute'. This is reserved for compiler usage.
                //     [IsUnmanaged]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsUnmanaged").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(11, 6),
                // (12,22): error CS8335: Do not use 'System.Runtime.CompilerServices.IsUnmanagedAttribute'. This is reserved for compiler usage.
                //     public int this[[IsUnmanaged]int x] => x;
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsUnmanaged").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(12, 22));
        }

        [Fact]
        public void UserReferencingIsUnmanagedAttributeShouldResultInAnError()
        {
            var code = @"
[IsUnmanaged]
public class Test
{
}
";

            CreateCompilation(code).VerifyDiagnostics(
                // (2,2): error CS0246: The type or namespace name 'IsUnmanagedAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [IsUnmanaged]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IsUnmanaged").WithArguments("IsUnmanagedAttribute").WithLocation(2, 2),
                // (2,2): error CS0246: The type or namespace name 'IsUnmanaged' could not be found (are you missing a using directive or an assembly reference?)
                // [IsUnmanaged]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IsUnmanaged").WithArguments("IsUnmanaged").WithLocation(2, 2));
        }

        [Fact]
        public void TypeReferencingAnotherTypeThatUsesAPublicAttributeFromAThirdNotReferencedAssemblyShouldGenerateItsOwn()
        {
            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            var code1 = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute { }
}");

            var code2 = CreateCompilation(@"
public class Test1<T> where T : unmanaged { }
", references: new[] { code1.ToMetadataReference() }, options: options);

            CompileAndVerify(code2, symbolValidator: module =>
            {
                AssertNoIsUnmanagedAttributeExists(module.ContainingAssembly);
            });

            var code3 = CreateCompilation(@"
public class Test2<T> : Test1<T> where T : unmanaged { }
", references: new[] { code2.ToMetadataReference() }, options: options);

            CompileAndVerify(code3, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test2`1").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void BuildingAModuleRequiresIsUnmanagedAttributeToBeThere_Missing_Type()
        {
            var code = @"
public class Test<T> where T : unmanaged
{
}";

            CreateCompilation(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (2,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsUnmanagedAttribute' is not defined or imported
                // public class Test<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "T").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(2, 19));
        }

        [Fact]
        public void BuildingAModuleRequiresIsUnmanagedAttributeToBeThere_Missing_Method()
        {
            var code = @"
public class Test
{
    public void M<T>() where T : unmanaged {}
}";

            CreateCompilation(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (4,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsUnmanagedAttribute' is not defined or imported
                //     public void M<T>() where T : unmanaged {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "T").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(4, 19));
        }

        [Fact]
        public void BuildingAModuleRequiresIsUnmanagedAttributeToBeThere_Missing_LocalFunction()
        {
            var code = @"
public class Test
{
    public void M()
    {
        void N<T>() where T : unmanaged
        {
        }

        N<int>();
    }
}";

            CreateCompilation(source: code, options: TestOptions.ReleaseModule.WithMetadataImportOptions(MetadataImportOptions.All)).VerifyDiagnostics(
                // (6,16): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsUnmanagedAttribute' is not defined or imported
                //         void N<T>() where T : unmanaged
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "T").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(6, 16));
        }

        [Fact]
        public void BuildingAModuleRequiresIsUnmanagedAttributeToBeThere_Missing_Delegate()
        {
            var code = "public delegate void D<T>() where T : unmanaged;";

            CreateCompilation(source: code, options: TestOptions.ReleaseModule.WithMetadataImportOptions(MetadataImportOptions.All)).VerifyDiagnostics(
                // (1,24): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsUnmanagedAttribute' is not defined or imported
                // public delegate void D<T>() where T : unmanaged;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "T").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(1, 24));
        }

        [Fact]
        public void ReferencingAnEmbeddedIsUnmanagedAttributeDoesNotUseIt_InternalsVisible()
        {
            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            var code1 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Assembly2"")]
public class Test1<T> where T : unmanaged
{
}";

            var comp1 = CompileAndVerify(code1, options: options, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test1`1").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            var code2 = @"
public class Test2<T> : Test1<T> where T : unmanaged
{
}";

            CompileAndVerify(code2, options: options.WithModuleName("Assembly2"), references: new[] { comp1.Compilation.ToMetadataReference() }, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Test2`1").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });
        }

        [Theory]
        [InlineData(OutputKind.DynamicallyLinkedLibrary)]
        [InlineData(OutputKind.NetModule)]
        public void IsUnmanagedAttributeExistsWithWrongConstructorSignature(OutputKind outputKind)
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute
    {
        public IsUnmanagedAttribute(int p) { }
    }
}
class Test<T> where T : unmanaged
{
}";

            CreateCompilation(text, options: new CSharpCompilationOptions(outputKind)).VerifyDiagnostics(
                // (9,12): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsUnmanagedAttribute..ctor'
                // class Test<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "T").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute", ".ctor").WithLocation(9, 12));
        }

        [Theory]
        [InlineData(OutputKind.DynamicallyLinkedLibrary)]
        [InlineData(OutputKind.NetModule)]
        public void IsUnmanagedAttributeExistsWithPrivateConstructor(OutputKind outputKind)
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsUnmanagedAttribute : System.Attribute
    {
        private IsUnmanagedAttribute() { }
    }
}
class Test<T> where T : unmanaged
{
}";

            CreateCompilation(text, options: new CSharpCompilationOptions(outputKind)).VerifyDiagnostics(
                // (9,12): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsUnmanagedAttribute..ctor'
                // class Test<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "T").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute", ".ctor").WithLocation(9, 12));
        }

        [Theory]
        [InlineData(OutputKind.DynamicallyLinkedLibrary)]
        [InlineData(OutputKind.NetModule)]
        public void IsUnmanagedAttributeExistsAsInterface(OutputKind outputKind)
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public interface IsUnmanagedAttribute { }
}
class Test<T> where T : unmanaged
{
}";

            CreateCompilation(text, options: new CSharpCompilationOptions(outputKind)).VerifyDiagnostics(
                // (6,12): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsUnmanagedAttribute..ctor'
                // class Test<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "T").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute", ".ctor").WithLocation(6, 12));
        }

        internal static void AssertReferencedIsUnmanagedAttribute(Accessibility accessibility, TypeParameterSymbol typeParameter, string assemblyName)
        {
            var attributes = ((PEModuleSymbol)typeParameter.ContainingModule).GetCustomAttributesForToken(((PETypeParameterSymbol)typeParameter).Handle);
            NamedTypeSymbol attributeType = attributes.Single().AttributeClass;

            Assert.Equal("IsUnmanagedAttribute", attributeType.Name);
            Assert.Equal(assemblyName, attributeType.ContainingAssembly.Name);
            Assert.Equal(accessibility, attributeType.DeclaredAccessibility);

            switch (accessibility)
            {
                case Accessibility.Internal:
                    {
                        var isUnmanagedTypeAttributes = attributeType.GetAttributes().OrderBy(attribute => attribute.AttributeClass.Name).ToArray();
                        Assert.Equal(2, isUnmanagedTypeAttributes.Length);

                        Assert.Equal(WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute), isUnmanagedTypeAttributes[0].AttributeClass.ToDisplayString());
                        Assert.Equal(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName, isUnmanagedTypeAttributes[1].AttributeClass.ToDisplayString());
                        break;
                    }

                case Accessibility.Public:
                    {
                        Assert.Null(attributeType.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName));

                        break;
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(accessibility);
            }

        }

        private void AssertNoIsUnmanagedAttributeExists(AssemblySymbol assembly)
        {
            var isUnmanagedAttributeTypeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsUnmanagedAttribute);
            Assert.Null(assembly.GetTypeByMetadataName(isUnmanagedAttributeTypeName));
        }
    }
}
