// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_RefSafetyRules : CSharpTestBase
    {
        [Fact]
        public void ExplicitAttribute_FromSource()
        {
            var source =
@"public class A
{
    public static ref T F<T>(out T t) => throw null;
}";

            var comp = CreateCompilation(new[] { source, RefSafetyRulesAttributeDefinition }, parseOptions: TestOptions.Regular10);
            CompileAndVerify(comp, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, publicDefinition: true));

            comp = CreateCompilation(new[] { source, RefSafetyRulesAttributeDefinition });
            CompileAndVerify(comp, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, publicDefinition: true));
        }

        [Theory]
        [CombinatorialData]
        public void ExplicitAttribute_FromMetadata(bool useCompilationReference)
        {
            var comp = CreateCompilation(RefSafetyRulesAttributeDefinition, parseOptions: TestOptions.Regular10);
            CompileAndVerify(comp, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: false, publicDefinition: true));
            var ref1 = AsReference(comp, useCompilationReference);

            var source =
@"public class A
{
    public static ref T F<T>(out T t) => throw null;
}";

            comp = CreateCompilation(source, references: new[] { ref1 }, parseOptions: TestOptions.Regular10);
            CompileAndVerify(comp, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: true));

            comp = CreateCompilation(source, references: new[] { ref1 });
            CompileAndVerify(comp, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: true, publicDefinition: true));
        }

        [Fact]
        public void ExplicitAttribute_MissingConstructor()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public sealed class RefSafetyRulesAttribute : Attribute { }
}";
            var source2 =
@"public class A
{
    public static ref T F<T>(out T t) => throw null;
}";

            var comp = CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(new[] { source1, source2 });
            comp.VerifyEmitDiagnostics(
                // error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Runtime.CompilerServices.RefSafetyRulesAttribute", ".ctor").WithLocation(1, 1));
        }

        [Theory]
        [CombinatorialData]
        public void ExplicitAttribute_ReferencedInSource(
            [CombinatorialValues(LanguageVersion.CSharp10, LanguageVersion.CSharp11)] LanguageVersion languageVersion,
            bool useCompilationReference)
        {
            var comp = CreateCompilation(RefSafetyRulesAttributeDefinition);
            comp.VerifyDiagnostics();
            var ref1 = AsReference(comp, useCompilationReference);

            var source =
@"using System.Runtime.CompilerServices;
[assembly: RefSafetyRules(11)]
[module: RefSafetyRules(11)]
";

            comp = CreateCompilation(source, references: new[] { ref1 }, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (3,10): error CS8335: Do not use 'System.Runtime.CompilerServices.RefSafetyRulesAttribute'. This is reserved for compiler usage.
                // [module: RefSafetyRules(11)]
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "RefSafetyRules(11)").WithArguments("System.Runtime.CompilerServices.RefSafetyRulesAttribute").WithLocation(3, 10));
        }

        [Theory]
        [InlineData("interface I { T F<T>(); }", false)]
        [InlineData("interface I { ref T F<T>(); }", true)]
        [InlineData("interface I { void F<T>(T t); }", false)]
        [InlineData("interface I { void F<T>(ref T t); }", true)]
        [InlineData("interface I { void F<T>(in T t); }", true)]
        [InlineData("interface I { void F<T>(out T t); }", true)]
        [InlineData("interface I { ref int P { get; } }", true)]
        [InlineData("interface I { }", false)]
        [InlineData("class C { }", false)]
        [InlineData("struct S { }", false)]
        [InlineData("ref struct R { }", true)]
        public void EmitAttribute_01(string source, bool requiresAttribute)
        {
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            CompileAndVerify(comp, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false));

            comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: requiresAttribute, includesAttributeUse: requiresAttribute, publicDefinition: false));
        }

        [Theory]
        [InlineData("class B { I F() => default; }", false)]
        [InlineData("class B { A F() => default; }", false)]
        [InlineData("class B { S F() => default; }", false)]
        [InlineData("class B { R F() => default; }", true)]
        [InlineData("class B { void F(I i) { } }", false)]
        [InlineData("class B { void F(A a) { } }", false)]
        [InlineData("class B { void F(S s) { } }", false)]
        [InlineData("class B { void F(R r) { } }", true)]
        [InlineData("class B { R P => default; }", true)]
        [InlineData("class B { R P { set { } } }", true)]
        public void EmitAttribute_02(string source, bool requiresAttribute)
        {
            var sourceA =
@"public interface I { }
public class A { }
public struct S { }
public ref struct R { }
";
            var refA = CreateCompilation(sourceA, parseOptions: TestOptions.Regular10).EmitToImageReference();

            var comp = CreateCompilation(source, references: new[] { refA }, parseOptions: TestOptions.Regular10);
            CompileAndVerify(comp, verify: Verification.Skipped, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false));

            comp = CreateCompilation(source, references: new[] { refA });
            CompileAndVerify(comp, verify: Verification.Skipped, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: requiresAttribute, includesAttributeUse: requiresAttribute, publicDefinition: false));
        }

        [Fact]
        public void AttributeField()
        {
            var sourceA =
@"using System;
using System.Linq;
using System.Reflection;
public class A
{
    public static void GetAttributeValue(out int value)
    {
        var module = typeof(A).Assembly.Modules.First();
        var attribute = module.GetCustomAttributes(false).Single(a => a.GetType().Name == ""RefSafetyRulesAttribute"");
        var field = attribute.GetType().GetField(""Version"");
        value = (int)field.GetValue(attribute);
    }
}";
            var refA = CreateCompilation(sourceA).EmitToImageReference();

            var sourceB =
@"using System;
class B : A
{
    static void Main()
    {
        GetAttributeValue(out int value);
        Console.WriteLine(value);
    }
}";
            CompileAndVerify(sourceB, references: new[] { refA }, expectedOutput: "11");
        }

        private static void AssertRefSafetyRulesAttribute(ModuleSymbol module, bool includesAttributeDefinition, bool includesAttributeUse, bool publicDefinition)
        {
            const string attributeName = "System.Runtime.CompilerServices.RefSafetyRulesAttribute";
            var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember(attributeName);
            var attribute = module.GetAttributes().SingleOrDefault();
            if (includesAttributeDefinition)
            {
                Assert.NotNull(type);
            }
            else
            {
                Assert.Null(type);
                if (includesAttributeUse)
                {
                    type = attribute.AttributeClass;
                }
            }
            if (type is { })
            {
                Assert.Equal(publicDefinition ? Accessibility.Public : Accessibility.Internal, type.DeclaredAccessibility);
            }
            if (includesAttributeUse)
            {
                Assert.Equal(type, attribute.AttributeClass);
            }
            else
            {
                Assert.Null(attribute);
            }
        }
    }
}
