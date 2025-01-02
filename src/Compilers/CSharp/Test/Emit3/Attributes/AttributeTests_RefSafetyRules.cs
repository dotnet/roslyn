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

        [WorkItem(63692, "https://github.com/dotnet/roslyn/issues/63692")]
        [Theory]
        [InlineData("", false)]
        [InlineData("[assembly: System.Reflection.AssemblyDescriptionAttribute(null)] [assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(string))]", false)]
        [InlineData("using System;", false)]
        [InlineData("using S = System.String;", false)]
        [InlineData("namespace N { namespace M { } }", false)]
        [InlineData("interface I { }", true)]
        [InlineData("namespace N { namespace M { interface I { } } }", true)]
        [InlineData("interface I { T F<T>(); }", true)]
        [InlineData("interface I { ref T F<T>(); }", true)]
        [InlineData("interface I { void F<T>(T t); }", true)]
        [InlineData("interface I { void F<T>(ref T t); }", true)]
        [InlineData("interface I { void F<T>(in T t); }", true)]
        [InlineData("interface I { void F<T>(out T t); }", true)]
        [InlineData("interface I { ref int P { get; } }", true)]
        [InlineData("class C { }", true)]
        [InlineData("struct S { }", true)]
        [InlineData("ref struct R { }", true)]
        [InlineData("delegate void D();", true)]
        [InlineData("enum E { }", true)]
        public void EmitAttribute_01(string source, bool expectedIncludesAttributeUse)
        {
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            CompileAndVerify(comp, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false));

            comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: expectedIncludesAttributeUse, includesAttributeUse: expectedIncludesAttributeUse, publicDefinition: false));
        }

        [WorkItem(63692, "https://github.com/dotnet/roslyn/issues/63692")]
        [Theory]
        [InlineData("class B { I F() => default; }")]
        [InlineData("class B { A F() => default; }")]
        [InlineData("class B { S F() => default; }")]
        [InlineData("class B { R F() => default; }")]
        [InlineData("class B { void F(I i) { } }")]
        [InlineData("class B { void F(A a) { } }")]
        [InlineData("class B { void F(S s) { } }")]
        [InlineData("class B { void F(R r) { } }")]
        [InlineData("class B { R P => default; }")]
        [InlineData("class B { R P { set { } } }")]
        public void EmitAttribute_02(string source)
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
            CompileAndVerify(comp, verify: Verification.Skipped, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: true, includesAttributeUse: true, publicDefinition: false));
        }

        [Theory]
        [CombinatorialData]
        public void EmitAttribute_TypeForwardedTo(
            [CombinatorialValues(LanguageVersion.CSharp10, LanguageVersion.CSharp11)] LanguageVersion languageVersionA,
            [CombinatorialValues(LanguageVersion.CSharp10, LanguageVersion.CSharp11)] LanguageVersion languageVersionB,
            bool useCompilationReference)
        {
            var sourceA =
@"public class A { }
";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersionA));
            var refA = AsReference(comp, useCompilationReference);
            bool useUpdatedEscapeRulesA = languageVersionA == LanguageVersion.CSharp11;
            Assert.Equal(useUpdatedEscapeRulesA, comp.SourceModule.UseUpdatedEscapeRules);
            CompileAndVerify(comp, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: useUpdatedEscapeRulesA, includesAttributeUse: useUpdatedEscapeRulesA, publicDefinition: false));

            var sourceB =
@"using System.Runtime.CompilerServices;
[assembly: TypeForwardedTo(typeof(A))]
";
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersionB));
            Assert.Equal(languageVersionB == LanguageVersion.CSharp11, comp.SourceModule.UseUpdatedEscapeRules);
            CompileAndVerify(comp, symbolValidator: m => AssertRefSafetyRulesAttribute(m, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false));
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
