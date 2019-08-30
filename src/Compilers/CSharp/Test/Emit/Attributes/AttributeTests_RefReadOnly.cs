// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_RefReadOnly : CSharpTestBase
    {
        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_Method()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}
class Test
{
    public ref readonly int M(in int x) { return ref x; }
}
";

            CompileAndVerify(text, verify: Verification.Fails, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);

                Assert.Empty(parameter.GetAttributes());
                Assert.Empty(method.GetReturnTypeAttributes());

                var peModule = (PEModuleSymbol)module;
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)method).Signature.ReturnParam.Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEParameterSymbol)parameter).Handle));

                AssertDeclaresType(peModule, WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute, Accessibility.Public);
            });
        }

        [Fact]
        public void InIsWrittenToMetadata_NeedsToBeGenerated_Method_Parameter()
        {
            var text = @"
class Test
{
    public void M(in int x) { }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M").GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);
                Assert.Empty(parameter.GetAttributes());

                var peModule = (PEModuleSymbol)module;
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEParameterSymbol)parameter).Handle));

                AssertDeclaresType(peModule, WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute, Accessibility.Internal);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_Method_ReturnType()
        {
            var text = @"
class Test
{
    private int x;
    public ref readonly int M() { return ref x; }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                Assert.Empty(method.GetReturnTypeAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_Method()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
class Test
{
    public ref readonly int M(in int x) { return ref x; }
}
";

            CompileAndVerify(codeB, verify: Verification.Fails, references: new[] { referenceA }, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);

                Assert.Empty(parameter.GetAttributes());
                Assert.Empty(method.GetReturnTypeAttributes());

                AssertNoIsReadOnlyAttributeExists(module.ContainingAssembly);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_Operator()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}
struct Test
{
    public static int operator +(in Test x, in Test y) { return 0; }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("op_Addition");
                Assert.Equal(2, method.ParameterCount);

                foreach (var parameter in method.Parameters)
                {
                    Assert.Equal(RefKind.In, parameter.RefKind);
                    Assert.Empty(parameter.GetAttributes());
                }
            });
        }

        [Fact]
        public void InIsWrittenToMetadata_NeedsToBeGenerated_Operator_Parameter()
        {
            var text = @"
struct Test
{
    public static int operator +(in Test x, in Test y) { return 0; }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("op_Addition");
                Assert.Equal(2, method.ParameterCount);

                foreach (var parameter in method.Parameters)
                {
                    Assert.Empty(parameter.GetAttributes());
                }
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_Operator_Method()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
struct Test
{
    public static int operator +(in Test x, in Test y) { return 0; }
}
";

            CompileAndVerify(codeB, references: new[] { referenceA }, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("op_Addition");
                Assert.Equal(2, method.ParameterCount);
                foreach (var parameter in method.Parameters)
                {
                    Assert.Equal(RefKind.In, parameter.RefKind);
                    Assert.Empty(parameter.GetAttributes());
                }

                AssertNoIsReadOnlyAttributeExists(module.ContainingAssembly);

            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_Constructor()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}
class Test
{
    public Test(in int x) { }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod(".ctor").Parameters.Single();

                Assert.Equal(RefKind.In, parameter.RefKind);
                Assert.Empty(parameter.GetAttributes());
            });
        }

        [Fact]
        public void InIsWrittenToMetadata_NeedsToBeGenerated_Constructor_Parameter()
        {
            var text = @"
class Test
{
    public Test(in int x) { }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod(".ctor").Parameters.Single();
                Assert.Empty(parameter.GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_Constructor_Method()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
class Test
{
    public Test(in int x) { }
}
";

            CompileAndVerify(codeB, references: new[] { referenceA }, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod(".ctor").Parameters.Single();

                Assert.Equal(RefKind.In, parameter.RefKind);
                Assert.Empty(parameter.GetAttributes());

                AssertNoIsReadOnlyAttributeExists(module.ContainingAssembly);

            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_Property()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}
class Test
{
    private int x = 0;
    public ref readonly int P1 { get { return ref x; } }
    public ref readonly int P2 => ref x;
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");

                AssertProperty(type.GetProperty("P1"));
                AssertProperty(type.GetProperty("P2"));

                void AssertProperty(PropertySymbol property)
                {
                    Assert.Equal(RefKind.RefReadOnly, property.RefKind);
                    Assert.True(property.ReturnsByRefReadonly);

                    Assert.Empty(property.GetAttributes());
                }
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_Property()
        {
            var text = @"
class Test
{
    private int x = 0;
    public ref readonly int P1 { get { return ref x; } }
    public ref readonly int P2 => ref x;
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");

                AssertProperty(type.GetProperty("P1"));
                AssertProperty(type.GetProperty("P2"));

                void AssertProperty(PropertySymbol property)
                {
                    Assert.Equal(RefKind.RefReadOnly, property.RefKind);
                    Assert.True(property.ReturnsByRefReadonly);

                    Assert.Empty(property.GetAttributes());
                }
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_Property()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
class Test
{
    private int x = 0;
    public ref readonly int P1 { get { return ref x; } }
    public ref readonly int P2 => ref x;
}
";

            CompileAndVerify(codeB, references: new[] { referenceA }, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");

                AssertProperty(type.GetProperty("P1"));
                AssertProperty(type.GetProperty("P2"));

                void AssertProperty(PropertySymbol property)
                {
                    Assert.Equal(RefKind.RefReadOnly, property.RefKind);
                    Assert.True(property.ReturnsByRefReadonly);
                    Assert.Empty(property.GetAttributes());

                    AssertNoIsReadOnlyAttributeExists(module.ContainingAssembly);
                }
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_Indexer()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}
class Test
{
    public ref readonly int this[in int x] { get { return ref x; } }
}
";

            CompileAndVerify(text, verify: Verification.Fails, symbolValidator: module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");
                Assert.Equal(RefKind.RefReadOnly, indexer.RefKind);
                Assert.True(indexer.ReturnsByRefReadonly);

                var parameter = indexer.GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);

                Assert.Empty(parameter.GetAttributes());
                Assert.Empty(indexer.GetAttributes());
            });
        }

        [Fact]
        public void InIsWrittenToMetadata_NeedsToBeGenerated_Indexer_Parameter()
        {
            var text = @"
class Test
{
    public int this[in int x] { get { return x; } }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]").GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);

                Assert.Empty(parameter.GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_Indexer_ReturnType()
        {
            var text = @"
class Test
{
    private int x;
    public ref readonly int this[int p] { get { return ref x; } }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");
                Assert.Equal(RefKind.RefReadOnly, indexer.RefKind);
                Assert.True(indexer.ReturnsByRefReadonly);

                Assert.Empty(indexer.GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_Indexer()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
class Test
{
    public ref readonly int this[in int x] { get { return ref x; } }
}
";

            CompileAndVerify(codeB, verify: Verification.Fails, references: new[] { referenceA }, symbolValidator: module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");
                Assert.Equal(RefKind.RefReadOnly, indexer.RefKind);
                Assert.True(indexer.ReturnsByRefReadonly);

                var parameter = indexer.GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);

                Assert.Empty(parameter.GetAttributes());
                Assert.Empty(indexer.GetAttributes());

                AssertNoIsReadOnlyAttributeExists(module.ContainingAssembly);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_Delegate()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}
public delegate ref readonly int D(in int x);
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod;
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);

                Assert.Empty(parameter.GetAttributes());
                Assert.Empty(method.GetReturnTypeAttributes());
            });
        }

        [Fact]
        public void InIsWrittenToMetadata_NeedsToBeGenerated_Delegate_Parameter()
        {
            var text = @"
public delegate void D(in int x);
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod.GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);
                Assert.Empty(parameter.GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_Delegate_ReturnType()
        {
            var text = @"
public delegate ref readonly int D();
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod;
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);
                Assert.Empty(method.GetReturnTypeAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_Delegate()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
public delegate ref readonly int D(in int x);
";

            CompileAndVerify(codeB, references: new[] { referenceA }, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod;
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);

                Assert.Empty(parameter.GetAttributes());
                Assert.Empty(method.GetReturnTypeAttributes());

                AssertNoIsReadOnlyAttributeExists(module.ContainingAssembly);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_LocalFunctions()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}
public class Test
{
    public void M()
    {
		ref readonly int Inner(in int x)
		{
			return ref x;
		}
    }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(text, verify: Verification.Fails, options: options, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__Inner|0_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);

                Assert.Empty(parameter.GetAttributes());
                Assert.Empty(method.GetReturnTypeAttributes());
            });
        }

        [Fact]
        public void InIsWrittenToMetadata_NeedsToBeGenerated_LocalFunctions_Parameters()
        {
            var text = @"
public class Test
{
    public void M()
    {
		void Inner(in int x) { }
    }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(text, options: options, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__Inner|0_0").GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);
                Assert.Empty(parameter.GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_LocalFunctions_ReturnType()
        {
            var text = @"
public class Test
{
    private int x;
    public void M()
    {
		ref readonly int Inner()
		{
			return ref x;
		}
    }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(text, verify: Verification.Fails, options: options, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__Inner|1_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                Assert.Empty(method.GetReturnTypeAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_LocalFunctions()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
public class Test
{
    public void M()
    {
		ref readonly int Inner(in int x)
		{
			return ref x;
		}
    }
}
";
            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(codeB, verify: Verification.Fails, references: new[] { referenceA }, options: options, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__Inner|0_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);

                Assert.Empty(parameter.GetAttributes());
                Assert.Empty(method.GetReturnTypeAttributes());

                AssertNoIsReadOnlyAttributeExists(module.ContainingAssembly);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_Lambda()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}

delegate ref readonly int D(in int x);

class Test
{
    public void M1()
    {
        M2((in int x) => ref x);
    }

    public void M2(D value) { }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(text, verify: Verification.Fails, options: options, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("Test.<>c.<M1>b__0_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);

                Assert.Empty(parameter.GetAttributes());
                Assert.Empty(method.GetReturnTypeAttributes());
            });
        }

        [Fact]
        public void InIsWrittenToMetadata_NeedsToBeGenerated_Lambda_Parameter()
        {
            var text = @"
delegate void D(in int x);

class Test
{
    public void M1()
    {
        M2((in int x) => {});
    }

    public void M2(D value) { }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(text, options: options, symbolValidator: module =>
            {
                var parameter = module.GlobalNamespace.GetMember<MethodSymbol>("Test.<>c.<M1>b__0_0").GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);
                Assert.Empty(parameter.GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_Lambda_ReturnType()
        {
            var text = @"
delegate ref readonly int D();

class Test
{
    private int x;
    public void M1()
    {
        M2(() => ref x);
    }

    public void M2(D value) { }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(text, options: options, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("Test.<M1>b__1_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);
                Assert.Empty(method.GetReturnTypeAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_Lambda()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
delegate ref readonly int D(in int x);

class Test
{
    public void M1()
    {
        M2((in int x) => ref x);
    }

    public void M2(D value) { }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(codeB, verify: Verification.Fails, options: options, references: new[] { referenceA }, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("Test.<>c.<M1>b__0_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);

                Assert.Empty(parameter.GetAttributes());
                Assert.Empty(method.GetReturnTypeAttributes());

                AssertNoIsReadOnlyAttributeExists(module.ContainingAssembly);
            });
        }

        [Fact]
        public void IsReadOnlyAttributeIsDisallowedEverywhereInSource_Delegates()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

[IsReadOnly]
public delegate ref readonly int D([IsReadOnly]in int x);
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (4,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                // [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(4, 2),
                // (5,37): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                // public delegate ref readonly int D([IsReadOnly]in int x);
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(5, 37));
        }

        [Fact]
        public void IsReadOnlyAttributeIsDisallowedEverywhereInSource_Types()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

[IsReadOnly]
public class Test
{
}
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (4,2): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                // [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(4, 2));
        }

        [Fact]
        public void IsReadOnlyAttributeIsDisallowedEverywhereInSource_Fields()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsReadOnly]
    private int x = 0;

    public int X => x;
}
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(6, 6));
        }

        [Fact]
        public void IsReadOnlyAttributeIsDisallowedEverywhereInSource_Properties()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    private int x = 0;

    [IsReadOnly]
    public ref readonly int Property => ref x;
}
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (8,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(8, 6));
        }

        [Fact]
        public void IsReadOnlyAttributeIsDisallowedEverywhereInSource_Methods()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsReadOnly]
    [return: IsReadOnly]
    public ref readonly int Method([IsReadOnly]in int x)
    {
        return ref x;
    }
}
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(6, 6),
                // (7,14): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     [return: IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(7, 14),
                // (8,37): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     public ref readonly int Method([IsReadOnly]in int x)
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(8, 37));
        }

        [Fact]
        public void IsReadOnlyAttributeIsDisallowedEverywhereInSource_Indexers()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilation(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsReadOnly]
    public ref readonly int this[[IsReadOnly]in int x] { get { return ref x; } }
}
";

            CreateCompilation(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(6, 6),
                // (7,35): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     public ref readonly int this[[IsReadOnly]in int x] { get { return ref x; } }
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(7, 35));
        }

        [Fact]
        public void UserReferencingEmbeddedAttributeShouldResultInAnError()
        {
            var code = @"
[Embedded]
public class Test
{
	public ref readonly int M(in int p) => ref p;
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (2,2): error CS0246: The type or namespace name 'EmbeddedAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [Embedded]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Embedded").WithArguments("EmbeddedAttribute").WithLocation(2, 2),
                // (2,2): error CS0246: The type or namespace name 'Embedded' could not be found (are you missing a using directive or an assembly reference?)
                // [Embedded]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Embedded").WithArguments("Embedded").WithLocation(2, 2));
        }

        [Fact]
        public void UserReferencingIsReadOnlyAttributeShouldResultInAnError()
        {
            var code = @"
[IsReadOnly]
public class Test
{
	public ref readonly int M(in int p) => ref p;
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (2,2): error CS0246: The type or namespace name 'IsReadOnlyAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [IsReadOnly]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IsReadOnly").WithArguments("IsReadOnlyAttribute").WithLocation(2, 2),
                // (2,2): error CS0246: The type or namespace name 'IsReadOnly' could not be found (are you missing a using directive or an assembly reference?)
                // [IsReadOnly]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IsReadOnly").WithArguments("IsReadOnly").WithLocation(2, 2));
        }

        [Fact]
        public void TypeReferencingAnotherTypeThatUsesAPublicAttributeFromAThirdNotReferencedAssemblyShouldGenerateItsOwn()
        {
            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            var code1 = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}");

            var code2 = CreateCompilation(@"
public class Test1
{
	public static ref readonly int M(in int p) => ref p;
}", references: new[] { code1.ToMetadataReference() }, options: options);

            CompileAndVerify(code2, verify: Verification.Fails, symbolValidator: module =>
            {
                // IsReadOnly is not generated in assembly
                var isReadOnlyAttributeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute);
                Assert.Null(module.ContainingAssembly.GetTypeByMetadataName(isReadOnlyAttributeName));
            });

            var code3 = CreateCompilation(@"
public class Test2
{
	public static ref readonly int M(in int p) => ref Test1.M(p);
}", references: new[] { code2.ToMetadataReference() }, options: options);

            CompileAndVerify(code3, symbolValidator: module =>
            {
                // IsReadOnly is generated in assembly
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.IsReadOnlyAttribute.FullName);
            });
        }

        [Fact]
        public void BuildingAModuleRequiresIsReadOnlyAttributeToBeThere_Missing_SourceMethod()
        {
            var code = @"
public class Test
{
    public void M(in int x) { }
}";

            CreateCompilation(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (4,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsReadOnlyAttribute' is not defined or imported
                //     public void M(in int x) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(4, 19));
        }

        [Fact]
        public void BuildingAModuleRequiresIsReadOnlyAttributeToBeThere_Missing_SourceMethod_MultipleLocations()
        {
            var code = @"
public class Test
{
    public void M1(in int x) { }
    public void M2(in int x) { }
}";

            CreateCompilation(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (4,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsReadOnlyAttribute' is not defined or imported
                //     public void M1(in int x) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(4, 20),
                // (5,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsReadOnlyAttribute' is not defined or imported
                //     public void M2(in int x) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(5, 20));
        }

        [Fact]
        public void BuildingAModuleRequiresIsReadOnlyAttributeToBeThere_Missing_LocalFunctions()
        {
            var code = @"
public class Test
{
    public void Parent()
    {
        void child(in int p) { }
        
        int x = 0;
        child(x);
    }
}";

            CreateCompilation(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (6,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsReadOnlyAttribute' is not defined or imported
                //         void child(in int p) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "in int p").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(6, 20));
        }

        [Fact]
        public void BuildingAModuleRequiresIsReadOnlyAttributeToBeThere_InAReference()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}").ToMetadataReference();

            var code = @"
public class Test
{
    public void M(in int x) { }
}";

            CompileAndVerify(code, verify: Verification.Fails, references: new[] { reference }, options: TestOptions.ReleaseModule, symbolValidator: module =>
            {
                AssertNoIsReadOnlyAttributeExists(module.ContainingAssembly);

                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M").GetParameters().Single();
                Assert.Equal(RefKind.In, parameter.RefKind);
                Assert.Empty(parameter.GetAttributes());
            });
        }

        [Fact]
        public void ReferencingAnEmbeddedIsReadOnlyAttributeDoesNotUseIt_InternalsVisible()
        {
            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            var code1 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Assembly2"")]
public class Test1
{
	public static ref readonly int M(in int p) => ref p;
}";

            var comp1 = CompileAndVerify(code1, options: options, verify: Verification.Fails, symbolValidator: module =>
            {
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.IsReadOnlyAttribute.FullName);
            });

            var code2 = @"
public class Test2
{
	public static ref readonly int M(in int p) => ref Test1.M(p);
}";

            CompileAndVerify(code2, options: options.WithModuleName("Assembly2"), references: new[] { comp1.Compilation.ToMetadataReference() }, symbolValidator: module =>
            {
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.IsReadOnlyAttribute.FullName);
            });
        }

        [Fact]
        public void IfIsReadOnlyAttributeIsDefinedThenEmbeddedIsNotGenerated()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}
class Test
{
    public ref readonly int M(in int x) { return ref x; }
}
";

            CompileAndVerify(text, verify: Verification.Fails, symbolValidator: module =>
            {
                Assert.Null(module.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName));
            });
        }

        [Fact]
        public void IsReadOnlyAttributeExistsWithWrongConstructorSignature_NetModule()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute(int p) { }
    }
}
class Test
{
    public void M(in int x) { }
}";

            CreateCompilation(text, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (11,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public void M(in int x) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(11, 19));
        }

        [Fact]
        public void IsReadOnlyAttributeExistsWithWrongConstructorSignature_Assembly()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute(int p) { }
    }
}
class Test
{
    public void M(in int x) { }
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (11,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public void M(in int x) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(11, 19));
        }

        [Fact]
        public void IsReadOnlyAttributeExistsWithWrongConstructorSignature_PrivateConstructor()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
        private IsReadOnlyAttribute() { }
    }
}
class Test
{
    public void M(in int x) { }
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (11,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public void M(in int x) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(11, 19));
        }

        [Fact]
        public void IsReadOnlyAttributesAreNotPortedInNoPia()
        {
            var comAssembly = CreateCompilationWithMscorlib40(@"
using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""test.dll"")]
[assembly: Guid(""9784f9a1-594a-4351-8f69-0fd2d2df03d3"")]
[ComImport()]
[Guid(""9784f9a1-594a-4351-8f69-0fd2d2df03d3"")]
public interface Test
{
    ref readonly int Property { get; }
    ref readonly int Method(in int x);
}");

            CompileAndVerify(comAssembly, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");

                var property = type.GetMember<PEPropertySymbol>("Property");
                Assert.NotNull(property);
                Assert.Empty(property.GetAttributes());

                var method = type.GetMethod("Method");
                Assert.NotNull(method);
                Assert.Empty(method.GetReturnTypeAttributes());

                var parameter = method.Parameters.Single();
                Assert.NotNull(parameter);
                Assert.Empty(parameter.GetAttributes());
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
                AssertNoIsReadOnlyAttributeExists(module.ContainingAssembly);

                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");

                var property = type.GetMember<PEPropertySymbol>("Property");
                Assert.NotNull(property);
                Assert.Empty(property.GetAttributes());

                var method = type.GetMethod("Method");
                Assert.NotNull(method);
                Assert.Empty(method.GetReturnTypeAttributes());

                var parameter = method.Parameters.Single();
                Assert.NotNull(parameter);
                Assert.Empty(parameter.GetAttributes());
            }
        }

        [Fact]
        public void TryingToBindFromSemanticModelDoesNotPolluteCompilation_Lambdas_Parameters()
        {
            var reference = CreateCompilation(@"
public delegate int D (in int x);
").VerifyEmitDiagnostics();

            Assert.True(NeedsGeneratedIsReadOnlyAttribute(reference));

            var compilation = CreateCompilation(@"
public class Test
{
    public void Process(D lambda) { }

    void User()
    {
    }
}", references: new[] { reference.ToMetadataReference() });

            compilation.VerifyEmitDiagnostics();
            Assert.False(NeedsGeneratedIsReadOnlyAttribute(compilation));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

            var userFunction = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "User");
            var position = userFunction.Body.CloseBraceToken.Position;
            var newInvocation = SyntaxFactory.ParseExpression("Process((in int x) => x)");

            var result = model.GetSpeculativeSymbolInfo(position, newInvocation, SpeculativeBindingOption.BindAsExpression);
            Assert.NotNull(result.Symbol);
            Assert.Equal(CandidateReason.None, result.CandidateReason);
            Assert.Empty(result.CandidateSymbols);

            Assert.False(NeedsGeneratedIsReadOnlyAttribute(compilation));
        }

        [Fact]
        public void TryingToBindFromSemanticModelDoesNotPolluteCompilation_Lambdas_ReturnTypes()
        {
            var reference = CreateCompilation(@"
public delegate ref readonly int D ();
").VerifyEmitDiagnostics();

            Assert.True(NeedsGeneratedIsReadOnlyAttribute(reference));

            var compilation = CreateCompilation(@"
public class Test
{
    private int x;

    public void Process(D lambda)
    {
        x = lambda();
    }

    void User()
    {
    }
}", references: new[] { reference.ToMetadataReference() });

            compilation.VerifyEmitDiagnostics();
            Assert.False(NeedsGeneratedIsReadOnlyAttribute(compilation));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

            var userFunction = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "User");
            var position = userFunction.Body.CloseBraceToken.Position;
            var newInvocation = SyntaxFactory.ParseExpression("Process(() => ref x)");

            var result = model.GetSpeculativeSymbolInfo(position, newInvocation, SpeculativeBindingOption.BindAsExpression);
            Assert.NotNull(result.Symbol);
            Assert.Equal(CandidateReason.None, result.CandidateReason);
            Assert.Empty(result.CandidateSymbols);

            Assert.False(NeedsGeneratedIsReadOnlyAttribute(compilation));
        }

        [Fact]
        public void TryingToBindFromSemanticModelDoesNotPolluteCompilation_LocalFunctions_Parameters()
        {
            var compilation = CreateCompilation(@"
public class Test
{
    void User()
    {
    }
}");

            compilation.VerifyEmitDiagnostics();
            Assert.False(NeedsGeneratedIsReadOnlyAttribute(compilation));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

            var userFunction = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "User");
            var position = userFunction.Body.CloseBraceToken.Position;
            var localfunction = SyntaxFactory.ParseStatement("int localFunction(in int x) { return x; }");

            Assert.True(model.TryGetSpeculativeSemanticModel(position, localfunction, out var newModel));
            var localFunctionSymbol = newModel.GetDeclaredSymbol(localfunction);
            Assert.NotNull(localFunctionSymbol);
            Assert.False(NeedsGeneratedIsReadOnlyAttribute(compilation));
        }

        [Fact]
        public void TryingToBindFromSemanticModelDoesNotPolluteCompilation_LocalFunctions_ReturnTypes()
        {
            var compilation = CreateCompilation(@"
public class Test
{
    void User()
    {
    }
}");

            compilation.VerifyEmitDiagnostics();
            Assert.False(NeedsGeneratedIsReadOnlyAttribute(compilation));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

            var userFunction = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "User");
            var position = userFunction.Body.CloseBraceToken.Position;
            var localfunction = SyntaxFactory.ParseStatement("ref readonly int localFunction(int x) { return ref x; }");

            Assert.True(model.TryGetSpeculativeSemanticModel(position, localfunction, out var newModel));
            var localFunctionSymbol = newModel.GetDeclaredSymbol(localfunction);
            Assert.NotNull(localFunctionSymbol);
            Assert.False(NeedsGeneratedIsReadOnlyAttribute(compilation));
        }

        [Fact]
        public void TryingPossibleBindingsForRefReadOnlyDoesNotPolluteCompilationForInvalidOnes()
        {
            var reference = CreateCompilation(@"
public delegate ref readonly int D1 ();
public delegate ref int D2 ();
").VerifyEmitDiagnostics();

            Assert.True(NeedsGeneratedIsReadOnlyAttribute(reference));

            var compilation = CreateCompilation(@"
public class Test
{
    public void Process(D1 lambda, int x) { }
    public void Process(D2 lambda, byte x) { }

    void User()
    {
        byte byteVar = 0;
        Process(() => { throw null; }, byteVar);
    }
}", references: new[] { reference.ToMetadataReference() });

            compilation.VerifyEmitDiagnostics();
            Assert.False(NeedsGeneratedIsReadOnlyAttribute(compilation));
        }

        [Fact]
        public void RefReadOnlyErrorsForLambdasDoNotPolluteCompilationDeclarationsDiagnostics()
        {
            var reference = CreateCompilation(@"
public delegate int D (in int x);
").EmitToImageReference();

            var code = @"
public class Test
{
    public void Process(D lambda) { }

    void User()
    {
        Process((in int p) => p);
    }
}";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseModule, references: new[] { reference });

            compilation.DeclarationDiagnostics.Verify();

            compilation.VerifyDiagnostics(
                // (8,18): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsReadOnlyAttribute' is not defined or imported
                //         Process((in int p) => p);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "in int p").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(8, 18));
        }

        [Fact]
        public void RefReadOnlyErrorsForLocalFunctionsDoNotPolluteCompilationDeclarationsDiagnostics()
        {
            var code = @"
public class Test
{
    private int x = 0;
    void User()
    {
        void local(in int x) { }
        local(x);
    }
}";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseModule);

            compilation.DeclarationDiagnostics.Verify();

            compilation.VerifyDiagnostics(
                // (7,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsReadOnlyAttribute' is not defined or imported
                //         void local(in int x) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(7, 20));
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_Class_NoParent()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute
    {
        private int value;

        public ref readonly int Method(in int x) => ref value;

        public static int operator +(in IsReadOnlyAttribute x, in IsReadOnlyAttribute y) => 0;

        public ref readonly int Property => ref value;

        public ref readonly int this[in int x] => ref value;
    }
}";

            CompileAndVerify(code, verify: Verification.Passes, symbolValidator: module =>
            {
                var isReadOnlyAttributeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute);
                var type = module.ContainingAssembly.GetTypeByMetadataName(isReadOnlyAttributeName);

                var method = type.GetMethod("Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var @operator = type.GetMethod("op_Addition");
                Assert.Empty(@operator.Parameters[0].GetAttributes());
                Assert.Empty(@operator.Parameters[1].GetAttributes());

                var property = type.GetProperty("Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("this[]");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_Class_CorrectParent()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
        private int value;

        public ref readonly int Method(in int x) => ref value;

        public static int operator +(in IsReadOnlyAttribute x, in IsReadOnlyAttribute y) => 0;

        public ref readonly int Property => ref value;

        public ref readonly int this[in int x] => ref value;
    }
}";

            CompileAndVerify(code, verify: Verification.Passes, symbolValidator: module =>
            {
                var isReadOnlyAttributeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute);
                var type = module.ContainingAssembly.GetTypeByMetadataName(isReadOnlyAttributeName);

                var method = type.GetMethod("Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var @operator = type.GetMethod("op_Addition");
                Assert.Empty(@operator.Parameters[0].GetAttributes());
                Assert.Empty(@operator.Parameters[1].GetAttributes());

                var property = type.GetProperty("Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("this[]");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_ClassInherit()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
    }
}
public class Child : System.Runtime.CompilerServices.IsReadOnlyAttribute
{
    private int value;

    public ref readonly int Method(in int x) => ref value;

    public static int operator +(in Child x, in Child y) => 0;

    public ref readonly int Property => ref value;

    public ref readonly int this[in int x] => ref value;
}";

            CompileAndVerify(code, verify: Verification.Passes, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");

                var method = type.GetMethod("Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var @operator = type.GetMethod("op_Addition");
                Assert.Empty(@operator.Parameters[0].GetAttributes());
                Assert.Empty(@operator.Parameters[1].GetAttributes());

                var property = type.GetProperty("Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("this[]");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_ClassOverride_SameAssembly()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public abstract class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute() { }

        public abstract ref readonly int Method(in int x);

        public abstract ref readonly int Property { get; }

        public abstract ref readonly int this[in int x] { get; }
    }
}
public class Child : System.Runtime.CompilerServices.IsReadOnlyAttribute
{
    private int value;

    public override ref readonly int Method(in int x) => ref value;

    public override ref readonly int Property => ref value;

    public override ref readonly int this[in int x] => ref value;
}";

            CompileAndVerify(code, verify: Verification.Passes, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");

                var method = type.GetMethod("Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var property = type.GetProperty("Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("this[]");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_ClassOverride_ExternalAssembly()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public abstract class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute() { }

        public abstract ref readonly int Method(in int x);

        public abstract ref readonly int Property { get; }

        public abstract ref readonly int this[in int x] { get; }
    }
}", assemblyName: "testRef").ToMetadataReference();

            var code = @"
public class Child : System.Runtime.CompilerServices.IsReadOnlyAttribute
{
    private int value;

    public override ref readonly int Method(in int x) => ref value;

    public override ref readonly int Property => ref value;

    public override ref readonly int this[in int x] => ref value;
}";

            CompileAndVerify(code, verify: Verification.Passes, references: new[] { reference }, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");

                var method = type.GetMethod("Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var property = type.GetProperty("Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("this[]");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_ClassOverridden_SameAssembly()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public abstract class Parent : System.Attribute
    {
        public abstract ref readonly int Method(in int x);

        public abstract ref readonly int Property { get; }

        public abstract ref readonly int this[in int x] { get; }
    }
    public class IsReadOnlyAttribute : Parent
    {
        private int value;

        public override ref readonly int Method(in int x) => ref value;

        public override ref readonly int Property => ref value;

        public override ref readonly int this[in int x] => ref value;
    }
}";

            CompileAndVerify(code, verify: Verification.Passes, symbolValidator: module =>
            {
                var typeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute);
                var type = module.ContainingAssembly.GetTypeByMetadataName(typeName);

                var method = type.GetMethod("Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var property = type.GetProperty("Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("this[]");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_ClassOverridden_ExternalAssembly()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public abstract class Parent : System.Attribute
    {
        public abstract ref readonly int Method(in int x);

        public abstract ref readonly int Property { get; }

        public abstract ref readonly int this[in int x] { get; }
    }
}").ToMetadataReference();

            var code = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : Parent
    {
        private int value;

        public override ref readonly int Method(in int x) => ref value;

        public override ref readonly int Property => ref value;

        public override ref readonly int this[in int x] => ref value;
    }
}";

            CompileAndVerify(code, verify: Verification.Passes, references: new[] { reference }, symbolValidator: module =>
            {
                var typeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute);
                var type = module.ContainingAssembly.GetTypeByMetadataName(typeName);

                var method = type.GetMethod("Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var property = type.GetProperty("Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("this[]");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_Class_WrongParent()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public class TestParent { }

    public class IsReadOnlyAttribute : TestParent
    {
        private int value;

        public ref readonly int Method(in int x) => ref value;

        public static int operator +(in IsReadOnlyAttribute x, in IsReadOnlyAttribute y) => 0;

        public ref readonly int Property => ref value;

        public ref readonly int this[in int x] => ref value;
    }
}";

            CompileAndVerify(code, verify: Verification.Passes, symbolValidator: module =>
            {
                var isReadOnlyAttributeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute);
                var type = module.ContainingAssembly.GetTypeByMetadataName(isReadOnlyAttributeName);

                var method = type.GetMethod("Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var @operator = type.GetMethod("op_Addition");
                Assert.Empty(@operator.Parameters[0].GetAttributes());
                Assert.Empty(@operator.Parameters[1].GetAttributes());

                var property = type.GetProperty("Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("this[]");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_Interface()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public interface IsReadOnlyAttribute
    {
        ref readonly int Method(in int x);
    }
}";

            CreateCompilation(code).VerifyEmitDiagnostics(
                // (6,9): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //         ref readonly int Method(in int x);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "ref readonly int").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(6, 9),
                // (6,33): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //         ref readonly int Method(in int x);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(6, 33));
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_ExplicitInterfaceImplementation_SameAssembly()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public interface ITest
    {
        ref readonly int Method(in int x);

        ref readonly int Property { get; }

        ref readonly int this[in int x] { get; }
    }
    public class IsReadOnlyAttribute : ITest
    {
        private int value;

        ref readonly int ITest.Method(in int x) => ref value;

        ref readonly int ITest.Property => ref value;

        ref readonly int ITest.this[in int x] => ref value;
    }
}";

            CompileAndVerify(code, verify: Verification.Passes, symbolValidator: module =>
            {
                var typeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute);
                var type = module.ContainingAssembly.GetTypeByMetadataName(typeName);

                var method = type.GetMethod("System.Runtime.CompilerServices.ITest.Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var property = type.GetProperty("System.Runtime.CompilerServices.ITest.Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("System.Runtime.CompilerServices.ITest.Item");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_ExplicitInterfaceImplementation_ExternalAssembly()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.CompilerServices
{
    public interface ITest
    {
        ref readonly int Method(in int x);

        ref readonly int Property { get; }

        ref readonly int this[in int x] { get; }
    }
}").ToMetadataReference();

            var code = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : ITest
    {
        private int value;

        ref readonly int ITest.Method(in int x) => ref value;

        ref readonly int ITest.Property => ref value;

        ref readonly int ITest.this[in int x] => ref value;
    }
}";

            CompileAndVerify(code, verify: Verification.Passes, references: new[] { reference }, symbolValidator: module =>
            {
                var typeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute);
                var type = module.ContainingAssembly.GetTypeByMetadataName(typeName);

                var method = type.GetMethod("System.Runtime.CompilerServices.ITest.Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var property = type.GetProperty("System.Runtime.CompilerServices.ITest.Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("System.Runtime.CompilerServices.ITest.Item");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void IsReadOnlyAttributeIsGenerated_ExplicitInterfaceImplementation_SameAssembly()
        {
            var code = @"
public interface ITest
{
    ref readonly int Method(in int x);

    ref readonly int Property { get; }

    ref readonly int this[in int x] { get; }
}
public class TestImpl : ITest
{
    private int value;

    ref readonly int ITest.Method(in int x) => ref value;

    ref readonly int ITest.Property => ref value;

    ref readonly int ITest.this[in int x] => ref value;
}";

            CompileAndVerify(code, verify: Verification.Passes, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("TestImpl");

                var method = type.GetMethod("ITest.Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var property = type.GetProperty("ITest.Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("ITest.Item");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void IsReadOnlyAttributeIsGenerated_ExplicitInterfaceImplementation_ExternalAssembly()
        {
            var reference = CreateCompilation(@"
public interface ITest
{
    ref readonly int Method(in int x);

    ref readonly int Property { get; }

    ref readonly int this[in int x] { get; }
}").ToMetadataReference();

            var code = @"
public class TestImpl : ITest
{
    private int value;

    ref readonly int ITest.Method(in int x) => ref value;

    ref readonly int ITest.Property => ref value;

    ref readonly int ITest.this[in int x] => ref value;
}";

            CompileAndVerify(code, verify: Verification.Passes, references: new[] { reference }, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("TestImpl");

                var method = type.GetMethod("ITest.Method");
                Assert.Empty(method.GetReturnTypeAttributes());
                Assert.Empty(method.Parameters.Single().GetAttributes());

                var property = type.GetProperty("ITest.Property");
                Assert.Empty(property.GetAttributes());

                var indexer = type.GetProperty("ITest.Item");
                Assert.Empty(indexer.GetAttributes());
                Assert.Empty(indexer.Parameters.Single().GetAttributes());
            });
        }

        [Fact]
        public void RefReadOnlyDefinitionsInsideUserDefinedIsReadOnlyAttribute_Delegate()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public delegate ref readonly int IsReadOnlyAttribute(in int x);
}";

            CreateCompilation(code).VerifyEmitDiagnostics(
                // (4,21): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public delegate ref readonly int IsReadOnlyAttribute(in int x);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "ref readonly int").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(4, 21),
                // (4,58): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public delegate ref readonly int IsReadOnlyAttribute(in int x);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(4, 58));
        }

        [Fact]
        public void MissingRequiredConstructorWillReportErrorsOnApproriateSyntax_Constructor()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute(int p) { }
    }
}
public class Test
{
    public Test(in int x) { }
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (11,17): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public Test(in int x) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(11, 17));
        }

        [Fact]
        public void MissingRequiredConstructorWillReportErrorsOnApproriateSyntax_Method()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute(int p) { }
    }
}
public class Test
{
    public ref readonly int Method(in int x) => ref x;
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (11,12): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public ref readonly int Method(in int x) => ref x;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "ref readonly int").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(11, 12),
                // (11,36): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public ref readonly int Method(in int x) => ref x;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(11, 36));
        }

        [Fact]
        public void MissingRequiredConstructorWillReportErrorsOnApproriateSyntax_LocalFunction()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute(int p) { }
    }
}
public class Test
{
    public void M()
    {
        int x = 0;

        ref readonly int local(in int p)
        {
            return ref p;
        }

        local(x);
    }
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (15,9): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //         ref readonly int local(in int p)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "ref readonly int").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(15, 9),
                // (15,32): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //         ref readonly int local(in int p)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in int p").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(15, 32));
        }

        [Fact]
        public void MissingRequiredConstructorWillReportErrorsOnApproriateSyntax_Lambda()
        {
            var reference = CreateCompilation(@"
public delegate ref readonly int D(in int x);
").EmitToImageReference();

            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute(int p) { }
    }
}

class Test
{
    public void M1()
    {
        M2((in int x) => ref x);
    }

    public void M2(D value) { }
}";

            CreateCompilation(text, references: new[] { reference }).VerifyEmitDiagnostics(
                // (14,33): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //         M2((in int x) => ref x);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=>").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(14, 23),
                // (14,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //         M2((in int x) => ref x);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(14, 13));
        }

        [Fact]
        public void MissingRequiredConstructorWillReportErrorsOnApproriateSyntax_Property()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute(int p) { }
    }
}
public class Test
{
    private int value;
    public ref readonly int Property => ref value;
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (12,12): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public ref readonly int Property => ref value;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "ref readonly int").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(12, 12));
        }

        [Fact]
        public void MissingRequiredConstructorWillReportErrorsOnApproriateSyntax_Indexer()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute(int p) { }
    }
}
public class Test
{

    public ref readonly int this[in int x] => ref x;
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (12,12): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public ref readonly int this[in int x] => ref x;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "ref readonly int").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(12, 12),
                // (12,34): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public ref readonly int this[in int x] => ref x;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(12, 34));
        }

        [Fact]
        public void MissingRequiredConstructorWillReportErrorsOnApproriateSyntax_Operator()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute(int p) { }
    }
}
public class Test
{
    public static int operator + (in Test x, in Test y) => 0;
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (11,35): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public static int operator + (in Test x, in Test y) => 0;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in Test x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(11, 35),
                // (11,46): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public static int operator + (in Test x, in Test y) => 0;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "in Test y").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(11, 46));
        }

        private void AssertNoIsReadOnlyAttributeExists(AssemblySymbol assembly)
        {
            var isReadOnlyAttributeTypeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute);
            Assert.Null(assembly.GetTypeByMetadataName(isReadOnlyAttributeTypeName));
        }

        private void AssertGeneratedEmbeddedAttribute(AssemblySymbol assembly, string expectedTypeName)
        {
            var typeSymbol = assembly.GetTypeByMetadataName(expectedTypeName);
            Assert.NotNull(typeSymbol);
            Assert.Equal(Accessibility.Internal, typeSymbol.DeclaredAccessibility);

            var attributes = typeSymbol.GetAttributes().OrderBy(attribute => attribute.AttributeClass.Name).ToArray();
            Assert.Equal(2, attributes.Length);

            Assert.Equal(WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute), attributes[0].AttributeClass.ToDisplayString());
            Assert.Equal(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName, attributes[1].AttributeClass.ToDisplayString());
        }

        private static bool NeedsGeneratedIsReadOnlyAttribute(CSharpCompilation compilation)
        {
            return (compilation.GetNeedsGeneratedAttributes() & EmbeddableAttributes.IsReadOnlyAttribute) != 0;
        }
    }
}
