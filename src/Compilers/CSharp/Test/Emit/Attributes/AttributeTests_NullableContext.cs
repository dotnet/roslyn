// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_NullableContext : CSharpTestBase
    {
        [Fact]
        public void ExplicitAttribute_WithNullableContext()
        {
            var sourceAttribute =
@"#nullable enable
namespace System.Runtime.CompilerServices
{
    public sealed class NullableContextAttribute : Attribute
    {
        private object _f1;
        private object _f2;
        private object _f3;
        public NullableContextAttribute(byte b)
        {
        }
    }
}";
            var comp = CreateCompilation(sourceAttribute);
            var ref0 = comp.EmitToImageReference();
            var expected =
@"[NullableContext(1)] [Nullable(0)] System.Runtime.CompilerServices.NullableContextAttribute
    NullableContextAttribute(System.Byte b)
        System.Byte b
";
            AssertNullableAttributes(comp, expected);

            var source =
@"#nullable enable
public class Program
{
    private object _f1;
    private object _f2;
    private object _f3;
}";
            comp = CreateCompilation(source, references: new[] { ref0 });
            expected =
@"[NullableContext(1)] [Nullable(0)] Program
    Program()
";
            AssertNullableAttributes(comp, expected);
        }

        private void AssertNullableAttributes(CSharpCompilation comp, string expected)
        {
            CompileAndVerify(comp, symbolValidator: module => AssertNullableAttributes(module, expected));
        }

        private static void AssertNullableAttributes(ModuleSymbol module, string expected)
        {
            var actual = NullableAttributesVisitor.GetString((PEModuleSymbol)module);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual);
        }
    }
}
