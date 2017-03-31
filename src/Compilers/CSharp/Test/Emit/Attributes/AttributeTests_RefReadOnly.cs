// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Linq;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_RefReadOnly : CSharpTestBase
    {
        [Fact]
        public void RefReadOnlIsWrittenToMetadata_SameAssembly_Method()
        {
            var text = @"
namespace System.Runtime.InteropServices
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

                var parameterAttribute = parameter.GetAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", parameterAttribute.MetadataName);
                Assert.Equal(module.ContainingAssembly.Name, parameterAttribute.ContainingAssembly.Name);

                var returnTypeAttribute = method.GetReturnTypeAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", returnTypeAttribute.MetadataName);
                Assert.Equal(module.ContainingAssembly.Name, returnTypeAttribute.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlIsWrittenToMetadata_DifferentAssembly_Method()
        {
            var codeA = @"
namespace System.Runtime.InteropServices
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

                var parameterAttribute = parameter.GetAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", parameterAttribute.MetadataName);
                Assert.Equal(referenceA.Compilation.AssemblyName, parameterAttribute.ContainingAssembly.Name);

                var returnTypeAttribute = method.GetReturnTypeAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", returnTypeAttribute.MetadataName);
                Assert.Equal(referenceA.Compilation.AssemblyName, returnTypeAttribute.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlIsWrittenToMetadata_SameAssembly_Property()
        {
            var text = @"
namespace System.Runtime.InteropServices
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

                    var returnTypeAttribute = property.GetAttributes().Single().AttributeClass;
                    Assert.Equal("ReadOnlyAttribute", returnTypeAttribute.MetadataName);
                    Assert.Equal(module.ContainingAssembly.Name, returnTypeAttribute.ContainingAssembly.Name);
                }
            });
        }

        [Fact]
        public void RefReadOnlIsWrittenToMetadata_DifferentAssembly_Property()
        {
            var codeA = @"
namespace System.Runtime.InteropServices
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

                    var returnTypeAttribute = property.GetAttributes().Single().AttributeClass;
                    Assert.Equal("ReadOnlyAttribute", returnTypeAttribute.MetadataName);
                    Assert.Equal(referenceA.Compilation.AssemblyName, returnTypeAttribute.ContainingAssembly.Name);
                }
            });
        }

        [Fact]
        public void RefReadOnlIsWrittenToMetadata_SameAssembly_Indexer()
        {
            var text = @"
namespace System.Runtime.InteropServices
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

                var parameterAttribute = parameter.GetAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", parameterAttribute.MetadataName);
                Assert.Equal(module.ContainingAssembly.Name, parameterAttribute.ContainingAssembly.Name);

                var returnTypeAttribute = indexer.GetAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", returnTypeAttribute.MetadataName);
                Assert.Equal(module.ContainingAssembly.Name, returnTypeAttribute.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlIsWrittenToMetadata_DifferentAssembly_Indexer()
        {
            var codeA = @"
namespace System.Runtime.InteropServices
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

                var parameterAttribute = parameter.GetAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", parameterAttribute.MetadataName);
                Assert.Equal(referenceA.Compilation.AssemblyName, parameterAttribute.ContainingAssembly.Name);

                var returnTypeAttribute = indexer.GetAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", returnTypeAttribute.MetadataName);
                Assert.Equal(referenceA.Compilation.AssemblyName, returnTypeAttribute.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlIsWrittenToMetadata_SameAssembly_Delegate()
        {
            var text = @"
namespace System.Runtime.InteropServices
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

                var parameterAttribute = parameter.GetAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", parameterAttribute.MetadataName);
                Assert.Equal(module.ContainingAssembly.Name, parameterAttribute.ContainingAssembly.Name);

                var returnTypeAttribute = method.GetReturnTypeAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", returnTypeAttribute.MetadataName);
                Assert.Equal(module.ContainingAssembly.Name, returnTypeAttribute.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlIsWrittenToMetadata_DifferentAssembly_Delegate()
        {
            var codeA = @"
namespace System.Runtime.InteropServices
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

                var parameterAttribute = parameter.GetAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", parameterAttribute.MetadataName);
                Assert.Equal(referenceA.Compilation.AssemblyName, parameterAttribute.ContainingAssembly.Name);

                var returnTypeAttribute = method.GetReturnTypeAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", returnTypeAttribute.MetadataName);
                Assert.Equal(referenceA.Compilation.AssemblyName, returnTypeAttribute.ContainingAssembly.Name);
            });
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18357")]
        public void RefReadOnlIsWrittenToMetadata_SameAssembly_LocalFunctions()
        {
            var text = @"
using System.Linq;
namespace System.Runtime.InteropServices
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

            CompileAndVerify(text, verify: false, symbolValidator: module =>
            {
                // PROTOTYPE(readonlyRefs) Assert both return type and parameter of local function once bug is fixed
            });
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18357")]
        public void RefReadOnlIsWrittenToMetadata_DifferentAssembly_LocalFunctions()
        {
            var codeA = @"
namespace System.Runtime.InteropServices
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

            CompileAndVerify(codeB, verify: false, additionalRefs: new[] { referenceA }, symbolValidator: module =>
            {
                // PROTOTYPE(readonlyRefs) Assert both return type and parameter of local function once bug is fixed
            });
        }
        [Fact]
        public void RefReadOnlIsWrittenToMetadata_SameAssembly_Lambda()
        {
            var text = @"
namespace System.Runtime.InteropServices
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

                var parameterAttribute = parameter.GetAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", parameterAttribute.MetadataName);
                Assert.Equal(module.ContainingAssembly.Name, parameterAttribute.ContainingAssembly.Name);

                var returnTypeAttribute = method.GetReturnTypeAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", returnTypeAttribute.MetadataName);
                Assert.Equal(module.ContainingAssembly.Name, returnTypeAttribute.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void RefReadOnlIsWrittenToMetadata_DifferentAssembly_Lambda()
        {
            var codeA = @"
namespace System.Runtime.InteropServices
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

                var parameterAttribute = parameter.GetAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", parameterAttribute.MetadataName);
                Assert.Equal(referenceA.Compilation.AssemblyName, parameterAttribute.ContainingAssembly.Name);

                var returnTypeAttribute = method.GetReturnTypeAttributes().Single().AttributeClass;
                Assert.Equal("ReadOnlyAttribute", returnTypeAttribute.MetadataName);
                Assert.Equal(referenceA.Compilation.AssemblyName, returnTypeAttribute.ContainingAssembly.Name);
            });
        }
    }
}
