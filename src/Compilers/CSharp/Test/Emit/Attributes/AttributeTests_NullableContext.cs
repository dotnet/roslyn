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
        public void EmptyProject()
        {
            var source = @"";
            var comp = CreateCompilation(source);
            var expected =
@"";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void ExplicitAttribute_FromSource()
        {
            var source =
@"#nullable enable
public class Program
{
    public object F(object arg) => arg;
}";
            var comp = CreateCompilation(new[] { NullableContextAttributeDefinition, source });
            var expected =
@"Program
    [NullableContext(1)] System.Object! F(System.Object! arg)
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void ExplicitAttribute_FromMetadata()
        {
            var comp = CreateCompilation(NullableContextAttributeDefinition);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source =
@"#nullable enable
public class Program
{
    public object F(object arg) => arg;
}";
            comp = CreateCompilation(source, references: new[] { ref0 });
            var expected =
@"Program
    [NullableContext(1)] System.Object! F(System.Object! arg)
";
            AssertNullableAttributes(comp, expected);
        }

        [Fact]
        public void ExplicitAttribute_MissingSingleByteConstructor()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullableContextAttribute : Attribute
    {
    }
}";
            var source2 =
@"public class Program
{
    public object F(object arg) => arg;
}";

            // C#7
            var comp = CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.Regular7);
            comp.VerifyEmitDiagnostics();

            // C#8, nullable disabled
            comp = CreateCompilation(new[] { source1, source2 }, options: WithNonNullTypesFalse());
            comp.VerifyEmitDiagnostics();

            // C#8, nullable enabled
            comp = CreateCompilation(new[] { source1, source2 }, options: WithNonNullTypesTrue());
            comp.VerifyEmitDiagnostics(
                // (3,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableContextAttribute..ctor'
                //     public object F(object arg) => arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F").WithArguments("System.Runtime.CompilerServices.NullableContextAttribute", ".ctor").WithLocation(3, 19));
        }

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

        [Fact]
        public void AttributeField()
        {
            var source =
@"#nullable enable
using System;
using System.Linq;
public class A
{
    private object _f1;
    private object _f2;
    private object _f3;
}
public class B
{
    private object? _f1;
    private object? _f2;
    private object? _f3;
}
class Program
{
    static void Main()
    {
        Console.WriteLine(GetAttributeValue(typeof(A)));
        Console.WriteLine(GetAttributeValue(typeof(B)));
    }    
    static byte GetAttributeValue(Type type)
    {
        var attribute = type.GetCustomAttributes(false).Single(a => a.GetType().Name == ""NullableContextAttribute"");
        var field = attribute.GetType().GetField(""Flag"");
        return (byte)field.GetValue(attribute);
    }    
}";
            var expectedOutput =
@"1
2";
            var expectedAttributes =
@"[NullableContext(1)] [Nullable(0)] A
    A()
[NullableContext(2)] [Nullable(0)] B
    B()
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput, symbolValidator: module => AssertNullableAttributes(module, expectedAttributes));
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
