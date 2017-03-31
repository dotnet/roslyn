// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_RefReadOnly : CSharpTestBase
    {
        [Fact]
        public void RefReadOnlyParameterIsWrittenToMetadata_IfItExistsInSameAssembly()
        {
            var text = @"
namespace System.Runtime.InteropServices
{
    public class RefReadOnlyAttribute : System.Attribute { }
}
class Test
{
    public void M(ref readonly int x) { }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M").GetParameters().Single();
                var attributeClass = parameter.GetAttributes().Single().AttributeClass;

                Assert.Equal("RefReadOnlyAttribute", attributeClass.MetadataName);
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);
            });
        }

        [Fact]
        public void RefReadOnlyParameterIsWrittenToMetadata_IfItExistsInADifferentAssembly()
        {
            var codeA = @"
namespace System.Runtime.InteropServices
{
    public class RefReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
class Test
{
    public void M(ref readonly int x) { }
}
";

            CompileAndVerify(codeB, additionalRefs: new[] { referenceA }, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M").GetParameters().Single();
                var attributeClass = parameter.GetAttributes().Single().AttributeClass;

                Assert.Equal("RefReadOnlyAttribute", attributeClass.MetadataName);
                Assert.Equal(RefKind.RefReadOnly, parameter.RefKind);
            });
        }

        [Fact]
        public void RefReadOnlyReturnIsWrittenToMetadata_IfItExistsInSameAssembly()
        {
            var text = @"
namespace System.Runtime.InteropServices
{
    public class RefReadOnlyAttribute : System.Attribute { }
}
class Test
{
    private int x = 0;
    public ref readonly int M() { return ref x; }
}
";

            CompileAndVerify(text, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M");
                var attributeClass = method.GetReturnTypeAttributes().Single().AttributeClass;

                Assert.Equal("RefReadOnlyAttribute", attributeClass.MetadataName);
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);
            });
        }

        [Fact]
        public void RefReadOnlyReturnIsWrittenToMetadata_IfItExistsInADifferentAssembly()
        {
            var codeA = @"
namespace System.Runtime.InteropServices
{
    public class RefReadOnlyAttribute : System.Attribute { }
}";

            var referenceA = CreateCompilationWithMscorlib(codeA).VerifyDiagnostics().ToMetadataReference();

            var codeB = @"
class Test
{
    private int x = 0;
    public ref readonly int M() { return ref x; }
}
";

            CompileAndVerify(codeB, additionalRefs: new[] { referenceA }, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("M");
                var attributeClass = method.GetReturnTypeAttributes().Single().AttributeClass;

                Assert.Equal("RefReadOnlyAttribute", attributeClass.MetadataName);
                Assert.Equal(RefKind.RefReadOnly, method.RefKind);
                Assert.True(method.ReturnsByRefReadonly);
            });
        }
    }
}
