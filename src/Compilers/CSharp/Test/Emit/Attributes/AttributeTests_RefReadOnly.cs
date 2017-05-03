// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Immutable;
using System.Linq;
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
    public ref readonly int M(ref readonly int x) { return ref x; }
}
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, parameter.GetAttributes(), module.ContainingAssembly.Name);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_Method_Parameter()
        {
            var text = @"
class Test
{
    public void M(ref readonly int x) { }
}
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M").GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, parameter.GetAttributes(), module.ContainingAssembly.Name);
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

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
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

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
class Test
{
    public ref readonly int M(ref readonly int x) { return ref x; }
}
";

            CompileAndVerify(codeB, verify: false, additionalRefs: new[] { referenceA }, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, parameter.GetAttributes(), referenceA.Compilation.AssemblyName);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, method.GetReturnTypeAttributes(), referenceA.Compilation.AssemblyName);

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

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");

                AssertProperty(type.GetProperty("P1"));
                AssertProperty(type.GetProperty("P2"));

                void AssertProperty(PropertySymbol property)
                {
                    Assert.Equal(RefKind.RefReadOnly, property.RefKind);
                    Assert.True(property.ReturnsByRefReadonly);

                    AssertReferencedIsReadOnlyAttribute(Accessibility.Public, property.GetAttributes(), module.ContainingAssembly.Name);
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

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");

                AssertProperty(type.GetProperty("P1"));
                AssertProperty(type.GetProperty("P2"));

                void AssertProperty(PropertySymbol property)
                {
                    Assert.Equal(RefKind.RefReadOnly, property.RefKind);
                    Assert.True(property.ReturnsByRefReadonly);

                    AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, property.GetAttributes(), module.ContainingAssembly.Name);
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

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
class Test
{
    private int x = 0;
    public ref readonly int P1 { get { return ref x; } }
    public ref readonly int P2 => ref x;
}
";

            CompileAndVerify(codeB, verify: false, additionalRefs: new[] { referenceA }, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");

                AssertProperty(type.GetProperty("P1"));
                AssertProperty(type.GetProperty("P2"));

                void AssertProperty(PropertySymbol property)
                {
                    Assert.Equal(RefKind.RefReadOnly, property.RefKind);
                    Assert.True(property.ReturnsByRefReadonly);

                    AssertReferencedIsReadOnlyAttribute(Accessibility.Public, property.GetAttributes(), referenceA.Compilation.AssemblyName);

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
    public ref readonly int this[ref readonly int x] { get { return ref x; } }
}
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");
                Assert.Equal(RefKind.RefReadOnly, indexer.RefKind);
                Assert.True(indexer.ReturnsByRefReadonly);

                var parameter = indexer.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, parameter.GetAttributes(), module.ContainingAssembly.Name);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, indexer.GetAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_Indexer_Parameter()
        {
            var text = @"
class Test
{
    public int this[ref readonly int x] { get { return x; } }
}
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]").GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, parameter.GetAttributes(), module.ContainingAssembly.Name);
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

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");
                Assert.Equal(RefKind.RefReadOnly, indexer.RefKind);
                Assert.True(indexer.ReturnsByRefReadonly);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, indexer.GetAttributes(), module.ContainingAssembly.Name);
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

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
class Test
{
    public ref readonly int this[ref readonly int x] { get { return ref x; } }
}
";

            CompileAndVerify(codeB, verify: false, additionalRefs: new[] { referenceA }, symbolValidator: module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");
                Assert.Equal(RefKind.RefReadOnly, indexer.RefKind);
                Assert.True(indexer.ReturnsByRefReadonly);

                var parameter = indexer.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, parameter.GetAttributes(), referenceA.Compilation.AssemblyName);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, indexer.GetAttributes(), referenceA.Compilation.AssemblyName);

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
public delegate ref readonly int D(ref readonly int x);
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod;
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, parameter.GetAttributes(), module.ContainingAssembly.Name);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_Delegate_Parameter()
        {
            var text = @"
public delegate void D(ref readonly int x);
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, parameter.GetAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_Delegate_ReturnType()
        {
            var text = @"
public delegate ref readonly int D();
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod;
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
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

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
public delegate ref readonly int D(ref readonly int x);
";

            CompileAndVerify(codeB, verify: false, additionalRefs: new[] { referenceA }, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod;
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, parameter.GetAttributes(), referenceA.Compilation.AssemblyName);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, method.GetReturnTypeAttributes(), referenceA.Compilation.AssemblyName);

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
		ref readonly int Inner(ref readonly int x)
		{
			return ref x;
		}
    }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(text, verify: false, options: options, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__Inner0_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, parameter.GetAttributes(), module.ContainingAssembly.Name);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_LocalFunctions_Parameters()
        {
            var text = @"
public class Test
{
    public void M()
    {
		void Inner(ref readonly int x) { }
    }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(text, verify: false, options: options, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__Inner0_0").GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, parameter.GetAttributes(), module.ContainingAssembly.Name);
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
            CompileAndVerify(text, verify: false, options: options, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__Inner1_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
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

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
public class Test
{
    public void M()
    {
		ref readonly int Inner(ref readonly int x)
		{
			return ref x;
		}
    }
}
";
            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(codeB, verify: false, additionalRefs: new[] { referenceA }, options: options, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("<M>g__Inner0_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, parameter.GetAttributes(), referenceA.Compilation.AssemblyName);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, method.GetReturnTypeAttributes(), referenceA.Compilation.AssemblyName);

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

delegate ref readonly int D(ref readonly int x);

class Test
{
    public void M1()
    {
        M2((ref readonly int x) => ref x);
    }

    public void M2(D value) { }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(text, verify: false, options: options, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("Test.<>c.<M1>b__0_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, parameter.GetAttributes(), module.ContainingAssembly.Name);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_NeedsToBeGenerated_Lambda_Parameter()
        {
            var text = @"
delegate void D(ref readonly int x);

class Test
{
    public void M1()
    {
        M2((ref readonly int x) => {});
    }

    public void M2(D value) { }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(text, verify: false, options: options, symbolValidator: module =>
            {
                var parameter = module.GlobalNamespace.GetMember<MethodSymbol>("Test.<>c.<M1>b__0_0").GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, parameter.GetAttributes(), module.ContainingAssembly.Name);
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
            CompileAndVerify(text, verify: false, options: options, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("Test.<M1>b__1_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
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

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
delegate ref readonly int D(ref readonly int x);

class Test
{
    public void M1()
    {
        M2((ref readonly int x) => ref x);
    }

    public void M2(D value) { }
}
";

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            CompileAndVerify(codeB, verify: false, options: options, additionalRefs: new[] { referenceA }, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("Test.<>c.<M1>b__0_0");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, parameter.GetAttributes(), referenceA.Compilation.AssemblyName);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, method.GetReturnTypeAttributes(), referenceA.Compilation.AssemblyName);

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

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

[IsReadOnly]
public delegate ref readonly int D([IsReadOnly]ref readonly int x);
";

            CreateCompilationWithMscorlib(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (4,2): error CS8412: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                // [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "IsReadOnly").WithLocation(4, 2),
                // (5,37): error CS8412: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                // public delegate ref readonly int D([IsReadOnly]ref readonly int x);
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "IsReadOnly").WithLocation(5, 37));
        }

        [Fact]
        public void IsReadOnlyAttributeIsDisallowedEverywhereInSource_Types()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

[IsReadOnly]
public class Test
{
}
";

            CreateCompilationWithMscorlib(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (4,2): error CS8412: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                // [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "IsReadOnly").WithLocation(4, 2));
        }

        [Fact]
        public void IsReadOnlyAttributeIsDisallowedEverywhereInSource_Fields()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsReadOnly]
    private int x = 0;

    public int X => x;
}
";

            CreateCompilationWithMscorlib(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8412: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "IsReadOnly").WithLocation(6, 6));
        }

        [Fact]
        public void IsReadOnlyAttributeIsDisallowedEverywhereInSource_Properties()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    private int x = 0;

    [IsReadOnly]
    public ref readonly int Property => ref x;
}
";

            CreateCompilationWithMscorlib(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (8,6): error CS8412: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "IsReadOnly").WithLocation(8, 6));
        }

        [Fact]
        public void IsReadOnlyAttributeIsDisallowedEverywhereInSource_Methods()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsReadOnly]
    [return: IsReadOnly]
    public ref readonly int Method([IsReadOnly]ref readonly int x)
    {
        return ref x;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8412: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "IsReadOnly").WithLocation(6, 6),
                // (7,14): error CS8412: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     [return: IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "IsReadOnly").WithLocation(7, 14),
                // (8,37): error CS8412: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     public ref readonly int Method([IsReadOnly]ref readonly int x)
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "IsReadOnly").WithLocation(8, 37));
        }

        [Fact]
        public void IsReadOnlyAttributeIsDisallowedEverywhereInSource_Indexers()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [IsReadOnly]
    public ref readonly int this[[IsReadOnly]ref readonly int x] { get { return ref x; } }
}
";

            CreateCompilationWithMscorlib(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8412: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     [IsReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "IsReadOnly").WithLocation(6, 6),
                // (7,35): error CS8412: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //     public ref readonly int this[[IsReadOnly]ref readonly int x] { get { return ref x; } }
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "IsReadOnly").WithLocation(7, 35));
        }
        
        [Fact]
        public void UserReferencingEmbeddedAttributeShouldResultInAnError()
        {
            var code = @"
[Embedded]
public class Test
{
	public ref readonly int M(ref readonly int p) => ref p;
}";

            CreateCompilationWithMscorlib(code).VerifyDiagnostics(
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
	public ref readonly int M(ref readonly int p) => ref p;
}";

            CreateCompilationWithMscorlib(code).VerifyDiagnostics(
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

            var code1 = CreateCompilationWithMscorlib(@"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}");

            var code2 = CreateCompilationWithMscorlib(@"
public class Test1
{
	public static ref readonly int M(ref readonly int p) => ref p;
}", references: new[] { code1.ToMetadataReference() }, options: options);

            CompileAndVerify(code2, verify: false, symbolValidator: module =>
            {
                // IsReadOnly is not generated in assembly
                var isReadOnlyAttributeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute);
                Assert.Null(module.ContainingAssembly.GetTypeByMetadataName(isReadOnlyAttributeName));
            });

            var code3 = CreateCompilationWithMscorlib(@"
public class Test2
{
	public static ref readonly int M(ref readonly int p) => ref Test1.M(p);
}", references: new[] { code2.ToMetadataReference() }, options: options);

            CompileAndVerify(code3, verify: false, symbolValidator: module =>
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
    public void M(ref readonly int x) { }
}";

            CreateCompilationWithMscorlib(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (4,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsReadOnlyAttribute' is not defined or imported
                //     public void M(ref readonly int x) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(4, 19));
        }

        [Fact]
        public void BuildingAModuleRequiresIsReadOnlyAttributeToBeThere_Missing_SourceMethod_MultipleLocations()
        {
            var code = @"
public class Test
{
    public void M1(ref readonly int x) { }
    public void M2(ref readonly int x) { }
}";

            CreateCompilationWithMscorlib(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (5,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsReadOnlyAttribute' is not defined or imported
                //     public void M2(ref readonly int x) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(5, 20),
                // (4,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsReadOnlyAttribute' is not defined or imported
                //     public void M1(ref readonly int x) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(4, 20));
        }

        [Fact]
        public void BuildingAModuleRequiresIsReadOnlyAttributeToBeThere_Missing_LocalFunctions()
        {
            var code = @"
public class Test
{
    public void Parent()
    {
        void child(ref readonly int p) { }
        
        int x = 0;
        child(x);
    }
}";

            CreateCompilationWithMscorlib(code, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (6,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsReadOnlyAttribute' is not defined or imported
                //         void child(ref readonly int p) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int p").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(6, 20));
        }

        [Fact]
        public void BuildingAModuleRequiresIsReadOnlyAttributeToBeThere_InAReference()
        {
            var reference = CreateCompilationWithMscorlib(@"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : System.Attribute { }
}").ToMetadataReference();

            var code = @"
public class Test
{
    public void M(ref readonly int x) { }
}";

            CompileAndVerify(code, verify: false, additionalRefs: new[] { reference }, options: TestOptions.ReleaseModule, symbolValidator: module =>
            {
                AssertNoIsReadOnlyAttributeExists(module.ContainingAssembly);

                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M").GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Public, parameter.GetAttributes(), reference.Display);
            });
        }

        [Fact]
        public void ReferencingAnEmbeddedAttributeDoesNotUseIt_InternalsVisible()
        {
            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            var code1 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleToAttribute(""Assembly2"")]
public class Test1
{
	public static ref readonly int M(ref readonly int p) => ref p;
}";

            var comp1 = CompileAndVerify(code1, options: options, verify: false, symbolValidator: module =>
            {
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, AttributeDescription.IsReadOnlyAttribute.FullName);
            });

            var code2 = @"
public class Test2
{
	public static ref readonly int M(ref readonly int p) => ref Test1.M(p);
}";

            CompileAndVerify(code2, options: options.WithModuleName("Assembly2"), additionalRefs: new[] { comp1.Compilation.ToMetadataReference() }, verify: false, symbolValidator: module =>
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
    public ref readonly int M(ref readonly int x) { return ref x; }
}
";

            CompileAndVerify(text, verify: false, symbolValidator: module =>
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
    public void M(ref readonly int x) { }
}";

            CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseModule).VerifyDiagnostics(
                // (11,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public void M(ref readonly int x) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "ref readonly int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(11, 19));
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
    public void M(ref readonly int x) { }
}";

            CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseDll).VerifyEmitDiagnostics(
                // (11,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public void M(ref readonly int x) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "ref readonly int x").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(11, 19));
        }

        [Fact]
        public void IsReadOnlyAttributesAreNotPortedInNoPia()
        {
            var comAssembly = CreateCompilationWithMscorlib(@"
using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""test.dll"")]
[assembly: Guid(""9784f9a1-594a-4351-8f69-0fd2d2df03d3"")]
[ComImport()]
[Guid(""9784f9a1-594a-4351-8f69-0fd2d2df03d3"")]
public interface Test
{
    ref readonly int Property { get; }
    ref readonly int Method(ref readonly int x);
}");

            CompileAndVerify(comAssembly, verify: false, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Test");

                var property = type.GetMember<PEPropertySymbol>("Property");
                Assert.NotNull(property);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, property.GetAttributes(), module.ContainingAssembly.Name);

                var method = type.GetMethod("Method");
                Assert.NotNull(method);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);

                var paramater = method.Parameters.Single();
                Assert.NotNull(paramater);
                AssertReferencedIsReadOnlyAttribute(Accessibility.Internal, paramater.GetAttributes(), module.ContainingAssembly.Name);
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

            var compilation_CompilationReference = CreateCompilationWithMscorlib(code, options: options, references: new[] { comAssembly.ToMetadataReference(embedInteropTypes: true) });
            CompileAndVerify(compilation_CompilationReference, verify: false, symbolValidator: symbolValidator);

            var compilation_BinaryReference = CreateCompilationWithMscorlib(code, options: options, references: new[] { comAssembly.EmitToImageReference(embedInteropTypes: true) });
            CompileAndVerify(compilation_BinaryReference, verify: false, symbolValidator: symbolValidator);

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

                var paramater = method.Parameters.Single();
                Assert.NotNull(paramater);
                Assert.Empty(paramater.GetAttributes());
            }
        }

        [Fact]
        public void TryingToBindFromSemanticModelDoesNotPolluteCompilation_Lambdas_Parameters()
        {
            var reference = CreateCompilationWithMscorlib(@"
public delegate int D (ref readonly int x);
").VerifyEmitDiagnostics();

            Assert.True(reference.NeedsGeneratedIsReadOnlyAttribute);

            var compilation = CreateCompilationWithMscorlibAndSystemCore(@"
public class Test
{
    public void Process(D lambda) { }

    void User()
    {
    }
}", references: new[] { reference.ToMetadataReference() });

            compilation.VerifyEmitDiagnostics();
            Assert.False(compilation.NeedsGeneratedIsReadOnlyAttribute);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

            var userFunction = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "User");
            var position = userFunction.Body.CloseBraceToken.Position;
            var newInvocation = SyntaxFactory.ParseExpression("Process((ref readonly int x) => x)");

            var result = model.GetSpeculativeSymbolInfo(position, newInvocation, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(CandidateReason.None, result.CandidateReason);
            Assert.NotNull(result.Symbol);

            Assert.False(compilation.NeedsGeneratedIsReadOnlyAttribute);
        }

        [Fact]
        public void TryingToBindFromSemanticModelDoesNotPolluteCompilation_Lambdas_ReturnTypes()
        {
            var reference = CreateCompilationWithMscorlib(@"
public delegate ref readonly int D (int x);
").VerifyEmitDiagnostics();

            Assert.True(reference.NeedsGeneratedIsReadOnlyAttribute);

            var compilation = CreateCompilationWithMscorlibAndSystemCore(@"
public class Test
{
    public void Process(D lambda) { }

    void User()
    {
    }
}", references: new[] { reference.ToMetadataReference() });

            compilation.VerifyEmitDiagnostics();
            Assert.False(compilation.NeedsGeneratedIsReadOnlyAttribute);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

            var userFunction = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "User");
            var position = userFunction.Body.CloseBraceToken.Position;
            var newInvocation = SyntaxFactory.ParseExpression("Process((int x) => ref x)");

            var result = model.GetSpeculativeSymbolInfo(position, newInvocation, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(CandidateReason.None, result.CandidateReason);
            Assert.NotNull(result.Symbol);

            Assert.False(compilation.NeedsGeneratedIsReadOnlyAttribute);
        }

        [Fact]
        public void TryingToBindFromSemanticModelDoesNotPolluteCompilation_LocalFunctions_Parameters()
        {
            var compilation = CreateCompilationWithMscorlib(@"
public class Test
{
    void User()
    {
    }
}");

            compilation.VerifyEmitDiagnostics();
            Assert.False(compilation.NeedsGeneratedIsReadOnlyAttribute);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

            var userFunction = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "User");
            var position = userFunction.Body.CloseBraceToken.Position;
            var localfunction = SyntaxFactory.ParseStatement("int localFunction(ref readonly int x) { return x; }");

            Assert.True(model.TryGetSpeculativeSemanticModel(position, localfunction, out var newModel));
            var localFunctionSymbol = newModel.GetDeclaredSymbol(localfunction);
            Assert.NotNull(localFunctionSymbol);
            Assert.False(compilation.NeedsGeneratedIsReadOnlyAttribute);
        }

        [Fact]
        public void TryingToBindFromSemanticModelDoesNotPolluteCompilation_LocalFunctions_ReturnTypes()
        {
            var compilation = CreateCompilationWithMscorlib(@"
public class Test
{
    void User()
    {
    }
}");

            compilation.VerifyEmitDiagnostics();
            Assert.False(compilation.NeedsGeneratedIsReadOnlyAttribute);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

            var userFunction = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "User");
            var position = userFunction.Body.CloseBraceToken.Position;
            var localfunction = SyntaxFactory.ParseStatement("ref readonly int localFunction(int x) { return ref x; }");

            Assert.True(model.TryGetSpeculativeSemanticModel(position, localfunction, out var newModel));
            var localFunctionSymbol = newModel.GetDeclaredSymbol(localfunction);
            Assert.NotNull(localFunctionSymbol);
            Assert.False(compilation.NeedsGeneratedIsReadOnlyAttribute);
        }

        [Fact]
        public void TryingPossibleBindingsForRefReadOnlyDoesNotPolluteCompilationForInvalidOnes()
        {
            var reference = CreateCompilationWithMscorlib(@"
public delegate ref readonly int D1 ();
public delegate ref int D2 ();
").VerifyEmitDiagnostics();

            Assert.True(reference.NeedsGeneratedIsReadOnlyAttribute);

            var compilation = CreateCompilationWithMscorlibAndSystemCore(@"
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
            Assert.False(compilation.NeedsGeneratedIsReadOnlyAttribute);
        }

        private void AssertReferencedIsReadOnlyAttribute(Accessibility accessibility, ImmutableArray<CSharpAttributeData> attributes, string assemblyName)
        {
            var attributeType = attributes.Single().AttributeClass;
            Assert.Equal("IsReadOnlyAttribute", attributeType.Name);
            Assert.Equal(assemblyName, attributeType.ContainingAssembly.Name);
            Assert.Equal(accessibility, attributeType.DeclaredAccessibility);
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
    }
}
