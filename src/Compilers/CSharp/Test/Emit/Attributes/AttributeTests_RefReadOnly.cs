// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using System;

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
    public class ReadOnlyAttribute : System.Attribute { }
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

                AssertReferencedReadOnlyAttribute(parameter.GetAttributes(), module.ContainingAssembly.Name);
                AssertReferencedReadOnlyAttribute(method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_Method()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
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

                AssertReferencedReadOnlyAttribute(parameter.GetAttributes(), referenceA.Compilation.AssemblyName);
                AssertReferencedReadOnlyAttribute(method.GetReturnTypeAttributes(), referenceA.Compilation.AssemblyName);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_Property()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
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

                    AssertReferencedReadOnlyAttribute(property.GetAttributes(), module.ContainingAssembly.Name);
                }
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_Property()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
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

                    AssertReferencedReadOnlyAttribute(property.GetAttributes(), referenceA.Compilation.AssemblyName);
                }
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_Indexer()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
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
                
                AssertReferencedReadOnlyAttribute(parameter.GetAttributes(), module.ContainingAssembly.Name);
                AssertReferencedReadOnlyAttribute(indexer.GetAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_Indexer()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
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

                AssertReferencedReadOnlyAttribute(parameter.GetAttributes(), referenceA.Compilation.AssemblyName);
                AssertReferencedReadOnlyAttribute(indexer.GetAttributes(), referenceA.Compilation.AssemblyName);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_Delegate()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
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

                AssertReferencedReadOnlyAttribute(parameter.GetAttributes(), module.ContainingAssembly.Name);
                AssertReferencedReadOnlyAttribute(method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_Delegate()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
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

                AssertReferencedReadOnlyAttribute(parameter.GetAttributes(), referenceA.Compilation.AssemblyName);
                AssertReferencedReadOnlyAttribute(method.GetReturnTypeAttributes(), referenceA.Compilation.AssemblyName);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_LocalFunctions()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
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

                AssertReferencedReadOnlyAttribute(parameter.GetAttributes(), module.ContainingAssembly.Name);
                AssertReferencedReadOnlyAttribute(method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_LocalFunctions()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
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

                AssertReferencedReadOnlyAttribute(parameter.GetAttributes(), referenceA.Compilation.AssemblyName);
                AssertReferencedReadOnlyAttribute(method.GetReturnTypeAttributes(), referenceA.Compilation.AssemblyName);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_SameAssembly_Lambda()
        {
            var text = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
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

                AssertReferencedReadOnlyAttribute(parameter.GetAttributes(), module.ContainingAssembly.Name);
                AssertReferencedReadOnlyAttribute(method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlyIsWrittenToMetadata_DifferentAssembly_Lambda()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
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

                AssertReferencedReadOnlyAttribute(parameter.GetAttributes(), referenceA.Compilation.AssemblyName);
                AssertReferencedReadOnlyAttribute(method.GetReturnTypeAttributes(), referenceA.Compilation.AssemblyName);
            });
        }
        
        [Fact]
        public void ReadOnlyAttributeIsDisallowedEverywhereInSource_Delegates()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

[ReadOnly]
public delegate ref readonly int D([ReadOnly] ref readonly int x);
";

            CreateCompilationWithMscorlib(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (4,2): error CS8412: Do not use 'System.Runtime.CompilerServices.ReadOnlyAttribute'. This is reserved for compiler usage.
                // [ReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "ReadOnly").WithLocation(4, 2),
                // (5,37): error CS8412: Do not use 'System.Runtime.CompilerServices.ReadOnlyAttribute'. This is reserved for compiler usage.
                // public delegate ref readonly int D([ReadOnly] ref readonly int x);
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "ReadOnly").WithLocation(5, 37));
        }

        [Fact]
        public void ReadOnlyAttributeIsDisallowedEverywhereInSource_Types()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

[ReadOnly]
public class Test
{
}
";

            CreateCompilationWithMscorlib(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (4,2): error CS8412: Do not use 'System.Runtime.CompilerServices.ReadOnlyAttribute'. This is reserved for compiler usage.
                // [ReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "ReadOnly").WithLocation(4, 2));
        }

        [Fact]
        public void ReadOnlyAttributeIsDisallowedEverywhereInSource_Fields()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [ReadOnly]
    private int x = 0;

    public int X => x;
}
";

            CreateCompilationWithMscorlib(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8412: Do not use 'System.Runtime.CompilerServices.ReadOnlyAttribute'. This is reserved for compiler usage.
                //     [ReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "ReadOnly").WithLocation(6, 6));
        }

        [Fact]
        public void ReadOnlyAttributeIsDisallowedEverywhereInSource_Properties()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    private int x = 0;

    [ReadOnly]
    public ref readonly int Property => ref x;
}
";

            CreateCompilationWithMscorlib(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (8,6): error CS8412: Do not use 'System.Runtime.CompilerServices.ReadOnlyAttribute'. This is reserved for compiler usage.
                //     [ReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "ReadOnly").WithLocation(8, 6));
        }

        [Fact]
        public void ReadOnlyAttributeIsDisallowedEverywhereInSource_Methods()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [ReadOnly]
    [return: ReadOnly]
    public ref readonly int Method([ReadOnly] ref readonly int x)
    {
        return ref x;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8412: Do not use 'System.Runtime.CompilerServices.ReadOnlyAttribute'. This is reserved for compiler usage.
                //     [ReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "ReadOnly").WithLocation(6, 6),
                // (7,14): error CS8412: Do not use 'System.Runtime.CompilerServices.ReadOnlyAttribute'. This is reserved for compiler usage.
                //     [return: ReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "ReadOnly").WithLocation(7, 14),
                // (8,37): error CS8412: Do not use 'System.Runtime.CompilerServices.ReadOnlyAttribute'. This is reserved for compiler usage.
                //     public ref readonly int Method([ReadOnly] ref readonly int x)
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "ReadOnly").WithLocation(8, 37));
        }

        [Fact]
        public void ReadOnlyAttributeIsDisallowedEverywhereInSource_Indexers()
        {
            var codeA = @"
namespace System.Runtime.CompilerServices
{
    public class ReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
using System.Runtime.CompilerServices;

public class Test
{
    [ReadOnly]
    public ref readonly int this[[ReadOnly] ref readonly int x] { get { return ref x; } }
}
";

            CreateCompilationWithMscorlib(codeB, references: new[] { referenceA }).VerifyDiagnostics(
                // (6,6): error CS8412: Do not use 'System.Runtime.CompilerServices.ReadOnlyAttribute'. This is reserved for compiler usage.
                //     [ReadOnly]
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "ReadOnly").WithLocation(6, 6),
                // (7,35): error CS8412: Do not use 'System.Runtime.CompilerServices.ReadOnlyAttribute'. This is reserved for compiler usage.
                //     public ref readonly int this[[ReadOnly] ref readonly int x] { get { return ref x; } }
                Diagnostic(ErrorCode.ERR_ExplicitReadOnlyAttr, "ReadOnly").WithLocation(7, 35));
        }

        [Fact]
        public void ReadOnlyAttributeIsGeneratedIfItDoesNotExist()
        {
            var code = @"
public class Test
{
	public ref readonly int M(ref readonly int p) => ref p;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("Test.M");
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);

                var parameter = method.GetParameters().Single();
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);

                AssertReferencedReadOnlyAttribute(parameter.GetAttributes(), module.ContainingAssembly.Name);
                AssertReferencedReadOnlyAttribute(method.GetReturnTypeAttributes(), module.ContainingAssembly.Name);

                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, WellKnownType.Microsoft_CodeAnalysis_EmbeddedAttribute);
                AssertGeneratedEmbeddedAttribute(module.ContainingAssembly, WellKnownType.System_Runtime_CompilerServices_ReadOnlyAttribute);
            });
        }

        private void AssertReferencedReadOnlyAttribute(ImmutableArray<CSharpAttributeData> attributes, string assemblyName)
        {
            var attributeType = attributes.Single().AttributeClass;
            Assert.Equal("ReadOnlyAttribute", attributeType.Name);
            Assert.Equal(assemblyName, attributeType.ContainingAssembly.Name);
        }

        private void AssertGeneratedEmbeddedAttribute(AssemblySymbol assembly, WellKnownType type)
        {
            var expectedTypeName = WellKnownTypes.GetMetadataName(type);
            var typeSymbol = assembly.GlobalNamespace.GetMember(expectedTypeName);
            Assert.NotNull(typeSymbol);

            var attributes = typeSymbol.GetAttributes().Select(attribute => attribute.AttributeClass.Name).ToArray();
            Assert.Equal(2, attributes.Length);

            Array.Sort(attributes);
            Assert.Equal("CompilerGeneratedAttribute", attributes[0]);
            Assert.Equal("EmbeddedAttribute", attributes[1]);
        }
    }
}
