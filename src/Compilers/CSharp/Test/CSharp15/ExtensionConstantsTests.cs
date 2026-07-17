// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public class ExtensionConstantsTests : CompilingTestBase
{
    [Fact]
    public void Declaration_01()
    {
        // LangVer
        var src = """
public static class E
{
    extension(object)
    {
        public const int i = 0;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        verifyGetDeclaredSymbol(comp);

        CreateCompilation(src, parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics();

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyEmitDiagnostics(
            // (5,26): error CS8652: The feature 'extension constants' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         public const int i = 0;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "i").WithArguments("extension constants").WithLocation(5, 26));

        static void verifyGetDeclaredSymbol(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var field = (IFieldSymbol)model.GetDeclaredSymbol(declarator);

            Assert.Equal("i", field.Name);
            Assert.True(field.IsConst);
            Assert.Equal(0, field.ConstantValue);
            AssertEx.Equal("E.extension(object).i", field.ToDisplayString());
        }
    }

    [Theory]
    [InlineData("public")]
    [InlineData("private")]
    [InlineData("internal")]
    [InlineData("new")]
    [InlineData("public new")]
    public void Declaration_02(string modifier)
    {
        var src = $$"""
public static class E
{
    extension(object)
    {
        {{modifier}} const int i = 0;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Theory]
    [InlineData("protected")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public void Declaration_03(string modifier)
    {
        // `protected` modifier
        var src = $$"""
public static class E
{
    extension(object)
    {
        {{modifier}} const int i = 0;
    }
}
""";
        var column = 20 + modifier.Length;

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,29): error CS9302: 'E.extension(object).i': new protected member declared in an extension block
            //         protected const int i = 0;
            Diagnostic(ErrorCode.ERR_ProtectedInExtension, "i").WithArguments("E.extension(object).i").WithLocation(5, column));
    }

    [Theory]
    [InlineData("sealed")]
    [InlineData("readonly")]
    [InlineData("volatile")]
    [InlineData("extern")]
    [InlineData("unsafe")]
    [InlineData("virtual")]
    [InlineData("override")]
    [InlineData("file")]
    public void Declaration_04(string modifier)
    {
        // misc modifiers
        var src = $$"""
public static class E
{
    extension(object)
    {
        {{modifier}} const int i = 0;
    }
}
""";
        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,26): error CS0106: The modifier 'sealed' is not valid for this item
            //         sealed const int i = 0;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "i").WithArguments(modifier));
    }

    [Fact]
    public void Declaration_05()
    {
        // `unsafe const` is rejected as an invalid modifier. It should not require RequiresUnsafeAttribute.
        var src = """
public static class E
{
    extension(object)
    {
        unsafe const int i = 0;
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules());
        comp.MakeMemberMissing(WellKnownMember.System_Diagnostics_CodeAnalysis_RequiresUnsafeAttribute__ctor);
        comp.VerifyEmitDiagnostics(
            // (5,26): error CS0106: The modifier 'unsafe' is not valid for this item
            //         unsafe const int i = 0;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "i").WithArguments("unsafe").WithLocation(5, 26));
    }

    [Fact]
    public void Declaration_06()
    {
        // `abstract` modifier
        var src = """
public static class E
{
    extension(object)
    {
        abstract const int i = 0;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,28): error CS0681: The modifier 'abstract' is not valid on fields. Try using a property instead.
            //         abstract const int i = 0;
            Diagnostic(ErrorCode.ERR_AbstractField, "i").WithLocation(5, 28));
    }

    [Fact]
    public void Declaration_07()
    {
        // `static` modifier
        var src = """
public static class E
{
    extension(object)
    {
        static const int i = 0;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,26): error CS0504: The constant 'i' cannot be marked static
            //         static const int i = 0;
            Diagnostic(ErrorCode.ERR_StaticConstant, "i").WithArguments("i").WithLocation(5, 26));
    }

    [Fact]
    public void Declaration_08()
    {
        // `required` modifier
        var src = """
public static class E
{
    extension(object)
    {
        required const int i = 0;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,5): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RequiredMemberAttribute..ctor'
            //     extension(object)
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "extension").WithArguments("System.Runtime.CompilerServices.RequiredMemberAttribute", ".ctor").WithLocation(3, 5),
            // (3,5): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
            //     extension(object)
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "extension").WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(3, 5),
            // (5,28): error CS0106: The modifier 'required' is not valid for this item
            //         required const int i = 0;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "i").WithArguments("required").WithLocation(5, 28));
    }

    [Fact]
    public void Declaration_09()
    {
        // generic extension block
        var src = """
public static class E
{
    extension<T>(T)
    {
        public const int i = 42;
        public const C<T> c = null;
    }
}

public class C<T>
{
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        verifyGetDeclaredSymbol(comp);

        static void verifyGetDeclaredSymbol(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();

            var field = (IFieldSymbol)model.GetDeclaredSymbol(declarators[0]);
            Assert.Equal("i", field.Name);
            Assert.True(field.IsConst);
            Assert.Equal(42, field.ConstantValue);
            AssertEx.Equal("E.extension<T>(T).i", field.ToDisplayString());

            field = (IFieldSymbol)model.GetDeclaredSymbol(declarators[1]);
            Assert.Equal("c", field.Name);
            Assert.True(field.IsConst);
            Assert.Null(field.ConstantValue);
            AssertEx.Equal("E.extension<T>(T).c", field.ToDisplayString());
            AssertEx.Equal("C<T>", field.Type.ToDisplayString());
        }
    }

    [Fact]
    public void Declaration_10()
    {
        // class-constrained type parameter
        var src = """
public static class E
{
    extension<T>(T) where T : class
    {
        public const T t = null;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,28): error CS1959: 'E.extension<T>(T).t' is of type 'T'. The type specified in a constant declaration must be sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, decimal, bool, string, an enum-type, or a reference-type.
            //         public const T t = null;
            Diagnostic(ErrorCode.ERR_InvalidConstantDeclarationType, "null").WithArguments("E.extension<T>(T).t", "T").WithLocation(5, 28));
    }

    [Fact]
    public void Declaration_11()
    {
        // DateTime constant
        var src = """
public static class E
{
    extension(object)
    {
        public const System.DateTime D = default;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,16): error CS0283: The type 'DateTime' cannot be declared const
            //         public const System.DateTime D = default;
            Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("System.DateTime").WithLocation(5, 16),
            // (5,42): error CS0133: The expression being assigned to 'E.extension(object).D' must be constant
            //         public const System.DateTime D = default;
            Diagnostic(ErrorCode.ERR_NotConstantExpression, "default").WithArguments("E.extension(object).D").WithLocation(5, 42));
    }

    [Fact]
    public void Declaration_12()
    {
        // non-null reference constant
        var src = """
public static class E
{
    extension(object)
    {
        public const object O = "hello";
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,33): error CS0134: 'E.extension(object).O' is of type 'object'. A const field of a reference type other than string can only be initialized with null.
            //         public const object O = "hello";
            Diagnostic(ErrorCode.ERR_NotNullConstRefField, @"""hello""").WithArguments("E.extension(object).O", "object").WithLocation(5, 33));
    }

    [Fact]
    public void Declaration_13()
    {
        // non-constant initializer
        var src = """
public static class E
{
    static int M() => 0;

    extension(object)
    {
        public const int I = M();
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (7,30): error CS0133: The expression being assigned to 'E.extension(object).I' must be constant
            //         public const int I = M();
            Diagnostic(ErrorCode.ERR_NotConstantExpression, "M()").WithArguments("E.extension(object).I").WithLocation(7, 30));
    }

    [Fact]
    public void Declaration_14()
    {
        var src = """
System.Console.Write(object.Second);

public static class E
{
    extension(object)
    {
        public const int First = 41;
        public const int Second = object.First + 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
        var first = (IFieldSymbol)model.GetDeclaredSymbol(declarators.Single(d => d.Identifier.ValueText == "First"));
        var second = (IFieldSymbol)model.GetDeclaredSymbol(declarators.Single(d => d.Identifier.ValueText == "Second"));

        Assert.Equal(41, first.ConstantValue);
        Assert.Equal(42, second.ConstantValue);
    }

    [Fact]
    public void Declaration_15()
    {
        var src = """
public static class E
{
    extension(object)
    {
        public const int First = object.Second;
        public const int Second = object.First;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,26): error CS0110: The evaluation of the constant value for 'E.extension(object).First' involves a circular definition
            //         public const int First = object.Second;
            Diagnostic(ErrorCode.ERR_CircConstValue, "First").WithArguments("E.extension(object).First").WithLocation(5, 26));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
        var first = (IFieldSymbol)model.GetDeclaredSymbol(declarators.Single(d => d.Identifier.ValueText == "First"));
        var second = (IFieldSymbol)model.GetDeclaredSymbol(declarators.Single(d => d.Identifier.ValueText == "Second"));

        Assert.True(first.IsConst);
        Assert.True(second.IsConst);
        Assert.False(first.HasConstantValue);
        Assert.False(second.HasConstantValue);
    }

    [Fact]
    public void Declaration_16()
    {
        // non-const fields
        var src = """
public static class E
{
    extension(object)
    {
        public int F;
        public static int SF;
        public readonly int RF;
        public static readonly int SRF;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (5,20): error CS9282: This member is not allowed in an extension block
            //         public int F;
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "F").WithLocation(5, 20),
            // (6,27): error CS9282: This member is not allowed in an extension block
            //         public static int SF;
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "SF").WithLocation(6, 27),
            // (7,29): error CS9282: This member is not allowed in an extension block
            //         public readonly int RF;
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "RF").WithLocation(7, 29),
            // (8,36): error CS9282: This member is not allowed in an extension block
            //         public static readonly int SRF;
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "SRF").WithLocation(8, 36));
    }

    [Fact]
    public void Declaration_17()
    {
        // Custom attribute on extension const field
        var src = """
using System;

_ = int.Const;

public static class E
{
    extension(int)
    {
        [A]
        public const int Const = 42;
    }
}

[AttributeUsage(AttributeTargets.Field)]
class AAttribute : Attribute { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        verifySymbols(comp.GetMember<NamedTypeSymbol>("E"), fromMetadata: false);

        var comp2 = CreateCompilation("_ = int.Const;", references: [comp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics();
        verifySymbols(comp2.GetMember<NamedTypeSymbol>("E"), fromMetadata: true);

        static void verifySymbols(NamedTypeSymbol e, bool fromMetadata)
        {
            var extension = e.GetTypeMembers("").Single(t => t.IsExtension);
            verifyField(extension.GetMember<FieldSymbol>("Const"), isExtensionBlockMember: true, fromMetadata);
            verifyField(e.GetMember<FieldSymbol>("Const"), isExtensionBlockMember: false, fromMetadata);
        }

        static void verifyField(FieldSymbol field, bool isExtensionBlockMember, bool fromMetadata)
        {
            Assert.Equal(isExtensionBlockMember, field.ContainingType.IsExtension);
            Assert.Equal("Const", field.Name);
            Assert.Equal("AAttribute", field.GetAttributes().Single().AttributeClass.ToTestDisplayString());

            if (fromMetadata)
            {
                Assert.IsType<PEFieldSymbol>(field);
            }
            else
            {
                Assert.Equal(!isExtensionBlockMember, field.IsImplicitlyDeclared);
            }
        }
    }

    [Fact]
    public void Declaration_18()
    {
        // Required, volatile, static, readonly, ref, and unsafe modifiers on extension const fields
        var src = """
static class E
{
    extension(int)
    {
        public required const int Required = 1;
        public volatile const int Volatile = 2;
        public static const int Static = 3;
        public readonly const int ReadOnly = 4;
        public ref const int Ref = 5;
        public unsafe const int Unsafe = 6;
    }
}

namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct | System.AttributeTargets.Field | System.AttributeTargets.Property, Inherited = false)]
    public sealed class RequiredMemberAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public sealed class CompilerFeatureRequiredAttribute : System.Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName) { }
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules());
        comp.VerifyEmitDiagnostics(
            // (5,35): error CS0106: The modifier 'required' is not valid for this item
            //         public required const int Required = 1;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Required").WithArguments("required").WithLocation(5, 35),
            // (6,35): error CS0106: The modifier 'volatile' is not valid for this item
            //         public volatile const int Volatile = 2;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Volatile").WithArguments("volatile").WithLocation(6, 35),
            // (7,33): error CS0504: The constant 'Static' cannot be marked static
            //         public static const int Static = 3;
            Diagnostic(ErrorCode.ERR_StaticConstant, "Static").WithArguments("Static").WithLocation(7, 33),
            // (8,35): error CS0106: The modifier 'readonly' is not valid for this item
            //         public readonly const int ReadOnly = 4;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "ReadOnly").WithArguments("readonly").WithLocation(8, 35),
            // (9,20): error CS1031: Type expected
            //         public ref const int Ref = 5;
            Diagnostic(ErrorCode.ERR_TypeExpected, "const").WithLocation(9, 20),
            // (10,33): error CS0106: The modifier 'unsafe' is not valid for this item
            //         public unsafe const int Unsafe = 6;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Unsafe").WithArguments("unsafe").WithLocation(10, 33));
    }

    [Theory, CombinatorialData]
    public void Usage_01(bool useCompilationReference)
    {
        // LangVer
        var libSrc = """
public static class E
{
    extension(object)
    {
        public const int i = 42;
    }
}
""";
        var libComp = CreateCompilation(libSrc);
        var libRef = AsReference(libComp, useCompilationReference);

        var src = """
System.Console.Write(object.i);
""";

        CompileAndVerify(src, references: [libRef], expectedOutput: "42").VerifyDiagnostics();

        CompileAndVerify(src, references: [libRef], parseOptions: TestOptions.RegularNext, expectedOutput: "42").VerifyDiagnostics();

        CreateCompilation(src, references: [libRef], parseOptions: TestOptions.Regular14).VerifyEmitDiagnostics(
            // (1,22): error CS8652: The feature 'extension constants' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // System.Console.Write(object.i);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "object.i").WithArguments("extension constants").WithLocation(1, 22));

        CreateCompilation([src, libSrc], parseOptions: TestOptions.Regular14).VerifyEmitDiagnostics(
            // (5,26): error CS8652: The feature 'extension constants' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         public const int i = 42;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "i").WithArguments("extension constants").WithLocation(5, 26));
    }

    [Fact]
    public void Usage_02()
    {
        // const vs. property ambiguity
        var src = """
_ = object.Member;

public static class E1
{
    extension(object)
    {
        public const int Member = 42;
    }
}

public static class E2
{
    extension(object)
    {
        public static int Member => 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS9339: The extension resolution is ambiguous between the following members: 'E2.extension(object).Member' and 'E1.extension(object).Member'
            // _ = object.Member;
            Diagnostic(ErrorCode.ERR_AmbigExtension, "object.Member").WithArguments("E2.extension(object).Member", "E1.extension(object).Member").WithLocation(1, 5));
    }

    [Fact]
    public void Usage_03()
    {
        // const vs. method ambiguity for value access and delegate conversion
        var src = """
var x = object.Member;

System.Func<int> y = object.Member;

public static class E1
{
    extension(object)
    {
        public const int Member = 42;
    }
}

public static class E2
{
    extension(object)
    {
        public static int Member() => 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,9): error CS9339: The extension resolution is ambiguous between the following members: 'E2.extension(object).Member()' and 'E1.extension(object).Member'
            // var x = object.Member;
            Diagnostic(ErrorCode.ERR_AmbigExtension, "object.Member").WithArguments("E2.extension(object).Member()", "E1.extension(object).Member").WithLocation(1, 9),
            // (3,22): error CS9339: The extension resolution is ambiguous between the following members: 'E2.extension(object).Member()' and 'E1.extension(object).Member'
            // System.Func<int> y = object.Member;
            Diagnostic(ErrorCode.ERR_AmbigExtension, "object.Member").WithArguments("E2.extension(object).Member()", "E1.extension(object).Member").WithLocation(3, 22));
    }

    [Fact]
    public void Usage_04()
    {
        // non-invocable const vs. method
        var src = """
System.Console.Write(object.Member());

public static class E1
{
    extension(object)
    {
        public const int Member = 42;
    }
}

public static class E2
{
    extension(object)
    {
        public static int Member() => 1;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void Usage_05()
    {
        // delegate-typed const
        var src = """
System.Console.Write(object.Member is null);

public static class E
{
    extension(object)
    {
        public const System.Func<int> Member = null;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "True").VerifyDiagnostics(
            // (1,22): warning CS8520: The given expression always matches the provided constant.
            // System.Console.Write(object.Member is null);
            Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant, "object.Member is null").WithLocation(1, 22));
    }

    [Fact]
    public void Usage_06()
    {
        // delegate-typed const vs. method ambiguity for value access and delegate conversion
        var src = """
var x = object.Member;

System.Func<int> y = object.Member;

public static class E1
{
    extension(object)
    {
        public const System.Func<int> Member = null;
    }
}

public static class E2
{
    extension(object)
    {
        public static int Member() => 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,9): error CS9339: The extension resolution is ambiguous between the following members: 'E2.extension(object).Member()' and 'E1.extension(object).Member'
            // var x = object.Member;
            Diagnostic(ErrorCode.ERR_AmbigExtension, "object.Member").WithArguments("E2.extension(object).Member()", "E1.extension(object).Member").WithLocation(1, 9),
            // (3,22): error CS9339: The extension resolution is ambiguous between the following members: 'E2.extension(object).Member()' and 'E1.extension(object).Member'
            // System.Func<int> y = object.Member;
            Diagnostic(ErrorCode.ERR_AmbigExtension, "object.Member").WithArguments("E2.extension(object).Member()", "E1.extension(object).Member").WithLocation(3, 22));
    }

    [Fact]
    public void Usage_07()
    {
        // delegate-typed const vs. method in invocation
        var src = """
_ = object.Member();

public static class E1
{
    extension(object)
    {
        public const System.Func<int> Member = null;
    }
}

public static class E2
{
    extension(object)
    {
        public static int Member() => 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS9339: The extension resolution is ambiguous between the following members: 'E2.extension(object).Member()' and 'E1.extension(object).Member'
            // _ = object.Member();
            Diagnostic(ErrorCode.ERR_AmbigExtension, "object.Member").WithArguments("E2.extension(object).Member()", "E1.extension(object).Member").WithLocation(1, 5));
    }

    [Fact]
    public void Usage_08()
    {
        // const vs. const ambiguity
        var src = """
_ = object.Member;

public static class E1
{
    extension(object)
    {
        public const int Member = 42;
    }
}

public static class E2
{
    extension(object)
    {
        public const int Member = 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS9339: The extension resolution is ambiguous between the following members: 'E1.extension(object).Member' and 'E2.extension(object).Member'
            // _ = object.Member;
            Diagnostic(ErrorCode.ERR_AmbigExtension, "object.Member").WithArguments("E1.extension(object).Member", "E2.extension(object).Member").WithLocation(1, 5));
    }

    [Fact]
    public void Usage_09()
    {
        // two consts vs. property ambiguity
        var src = """
_ = object.Member;

public static class E1
{
    extension(object)
    {
        public const int Member = 42;
    }
}

public static class E2
{
    extension(object)
    {
        public const int Member = 42;
    }
}

public static class E3
{
    extension(object)
    {
        public static int Member => 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS9339: The extension resolution is ambiguous between the following members: 'E3.extension(object).Member' and 'E1.extension(object).Member'
            // _ = object.Member;
            Diagnostic(ErrorCode.ERR_AmbigExtension, "object.Member").WithArguments("E3.extension(object).Member", "E1.extension(object).Member").WithLocation(1, 5));
    }

    [Fact]
    public void Usage_10()
    {
        // generic extension block
        var src = """
System.Console.Write(string.Member);

public static class E
{
    extension<T>(T)
    {
        public const int Member = 42;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void Usage_11()
    {
        // extension constant for base type
        var src = """
System.Console.Write(string.Member);

public static class E
{
    extension(object)
    {
        public const int Member = 42;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void Usage_12()
    {
        // generic name arity does not match an extension constant
        var src = """
_ = object.Member<int>;

public static class E
{
    extension(object)
    {
        public const int Member = 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,12): error CS0117: 'object' does not contain a definition for 'Member'
            // _ = object.Member<int>;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Member<int>").WithArguments("object", "Member").WithLocation(1, 12));
    }

    [Fact]
    public void Usage_13()
    {
        // generic extension block constraints are not satisfied
        var src = """
_ = string.Member;

public static class E
{
    extension<T>(T) where T : struct
    {
        public const int Member = 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,12): error CS0117: 'string' does not contain a definition for 'Member'
            // _ = string.Member;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Member").WithArguments("string", "Member").WithLocation(1, 12));
    }

    [Fact]
    public void Usage_14()
    {
        // non-generic extension block receiver type is not applicable
        var src = """
_ = int.Member;

public static class E
{
    extension(string)
    {
        public const int Member = 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,9): error CS0117: 'int' does not contain a definition for 'Member'
            // _ = int.Member;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Member").WithArguments("int", "Member").WithLocation(1, 9));
    }

    [Fact]
    public void Usage_15()
    {
        // Color Color from a primary constructor
        var src = """
struct S(Color Color)
{
    public void M()
    {
        System.Console.Write(Color.Member);
    }
}

class Color
{
    public int Member => 2;
}

class Program
{
    static void Main()
    {
        new S(new Color()).M();
    }
}

static class E
{
    extension(Color)
    {
        public const int Member = 1;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "2").VerifyDiagnostics();
    }

    [Fact]
    public void Usage_16()
    {
        // Color Color
        var src = """
class Program
{
    static void Main()
    {
        M(new Color());
    }

    static void M(Color Color)
    {
        System.Console.Write(Color.Member);
    }
}

class Color
{
    public int Member => 2;
}

static class E
{
    extension(Color)
    {
        public const int Member = 1;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "2").VerifyDiagnostics();
    }

    [Fact]
    public void Usage_17()
    {
        // Color Color from primary constructor, no instance member
        var src = """
struct S(Color Color)
{
    public void M()
    {
        System.Console.Write(Color.Member);
    }
}

class Color
{
}

class Program
{
    static void Main()
    {
        new S(new Color()).M();
    }
}

static class E
{
    extension(Color)
    {
        public const int Member = 1;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "1").VerifyDiagnostics(
            // (1,16): warning CS9113: Parameter 'Color' is unread.
            // struct S(Color Color)
            Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(1, 16));
    }

    [Fact]
    public void Usage_18()
    {
        // Color Color, no instance member
        var src = """
class Program
{
    static void Main()
    {
        M(new Color());
    }

    static void M(Color Color)
    {
        System.Console.Write(Color.Member);
    }
}

class Color
{
}

static class E
{
    extension(Color)
    {
        public const int Member = 1;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void Usage_20()
    {
        // const vs. property vs. method ambiguity
        var src = """
_ = object.Member;

public static class E1
{
    extension(object)
    {
        public const int Member = 42;
    }
}

public static class E2
{
    extension(object)
    {
        public static int Member => 42;
    }
}

public static class E3
{
    extension(object)
    {
        public static int Member() => 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS9339: The extension resolution is ambiguous between the following members: 'E3.extension(object).Member()' and 'E2.extension(object).Member'
            // _ = object.Member;
            Diagnostic(ErrorCode.ERR_AmbigExtension, "object.Member").WithArguments("E3.extension(object).Member()", "E2.extension(object).Member").WithLocation(1, 5));
    }

    [Fact]
    public void Usage_21()
    {
        // obsolete extension constant
        var src = """
_ = object.Member;

public static class E
{
    extension(object)
    {
        [System.Obsolete("obsolete extension constant")]
        public const int Member = 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): warning CS0618: 'E.extension(object).Member' is obsolete: 'obsolete extension constant'
            // _ = object.Member;
            Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "object.Member").WithArguments("E.extension(object).Member", "obsolete extension constant").WithLocation(1, 5));
    }

    [Fact]
    public void Usage_22()
    {
        // metadata extension constant with RequiresUnsafeAttribute under normal and updated memory safety rules
        var il = $$"""
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly '<<GeneratedFileName>>' { }
.module '<<GeneratedFileName>>.dll'
.custom instance void System.Runtime.CompilerServices.MemorySafetyRulesAttribute::.ctor(int32) = { int32({{CSharpCompilationOptions.UpdatedMemorySafetyRulesVersion}}) }

.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

    .class nested public auto ansi sealed specialname '<G>$C43E2675C7BBF9284AF22FB8A9BF0280'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

        .class nested public auto ansi abstract sealed specialname '<M>$C43E2675C7BBF9284AF22FB8A9BF0280'
            extends System.Object
        {
            .field public static literal int32 I = int32(1)
            .custom instance void System.Diagnostics.CodeAnalysis.RequiresUnsafeAttribute::.ctor()

            .method public hidebysig specialname static void '<Extension>$' ( object '' ) cil managed
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                .maxstack 8
                IL_0000: ret
            }
        }
    }
}
""" + MemorySafetyRulesAttributeIL + RequiresUnsafeAttributeIL;

        var src = """
unsafe
{
    _ = object.I;
}

_ = object.I;
""";

        CreateCompilationWithIL(src, il, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules(), appendDefaultHeader: false).VerifyEmitDiagnostics(
            // (6,5): error CS9362: 'E.extension(object).I' must be used in an unsafe context because it is marked as 'unsafe'
            // _ = object.I;
            Diagnostic(ErrorCode.ERR_UnsafeMemberOperation, "object.I").WithArguments("E.extension(object).I").WithLocation(6, 5));

        // Normal memory safety rules ignore RequiresUnsafeAttribute.
        CreateCompilationWithIL(src, il, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseExe, appendDefaultHeader: false).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Usage_23()
    {
        // Non-static extension member in inner scope is skipped in favor of static one from outer scope
        var src = """
using N;

System.Console.Write(object.Member);

public static class E
{
    extension(object o)
    {
        public int Member => 1;
    }
}

namespace N
{
    public static class E2
    {
        extension(object)
        {
            public const int Member = 2;
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single(n => n.ToString() == "object.Member");
        AssertEx.Equal("System.Int32 N.E2.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.Member", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void Usage_24()
    {
        // inner scope wins over outer scope
        var src = """
using N;

Marker.M();
System.Console.Write(object.Member);

public static class E
{
    extension(object)
    {
        public const int Member = 1;
    }
}

namespace N
{
    public static class Marker
    {
        public static void M() { }
    }

    public static class E2
    {
        extension(object)
        {
            public const int Member = 2;
        }
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single(n => n.ToString() == "object.Member");
        AssertEx.Equal("System.Int32 E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.Member", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void GetMemberGroup_01()
    {
        var src = """
_ = object.Member();

public static class E
{
    extension(object)
    {
        public const int Member = 42;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,12): error CS0117: 'object' does not contain a definition for 'Member'
            // _ = object.Member();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Member").WithArguments("object", "Member").WithLocation(1, 12));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single(n => n.ToString() == "object.Member");
        var symbolInfo = model.GetSymbolInfo(memberAccess);
        var memberGroup = model.GetMemberGroup(memberAccess);

        Assert.Null(symbolInfo.Symbol);
        Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
        AssertEx.Equal(["System.Int32 E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.Member"], symbolInfo.CandidateSymbols.ToTestDisplayStrings());
        AssertEx.Equal(["System.Int32 E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.Member"], memberGroup.ToTestDisplayStrings());
    }

    [Fact]
    public void Usage_25()
    {
        // instance wins over extension
        var src = """
System.Console.Write(C.Member);

public class C
{
    public const int Member = 2;
}

public static class E
{
    extension(C)
    {
        public const int Member = 1;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "2").VerifyDiagnostics();
    }

    [Fact]
    public void Usage_26()
    {
        // using static
        var src = """
using static N.E;

System.Console.Write(object.Member);

namespace N
{
    public static class E
    {
        extension(object)
        {
            public const int Member = 42;
        }
    }
}
""";

        CompileAndVerify(src, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void Usage_27()
    {
        // extension constant can be accessed on the enclosing static class
        var src = """
System.Console.Write(E.Member);

public static class E
{
    extension(string)
    {
        public const int Member = 42;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void Usage_28()
    {
        // two extension constants with different names in the same enclosing static class,
        // and two extension constants with the same name in different enclosing static classes
        var src = """
System.Console.Write(string.Member1);
System.Console.Write(int.Member2);
System.Console.Write(string.Member);
System.Console.Write(int.Member);

public static class E
{
    extension(string)
    {
        public const int Member1 = 1;
    }

    extension(int)
    {
        public const int Member2 = 2;
    }
}

public static class E1
{
    extension(string)
    {
        public const int Member = 3;
    }
}

public static class E2
{
    extension(int)
    {
        public const int Member = 4;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "1234").VerifyDiagnostics();
    }

    [Fact]
    public void Usage_29()
    {
        // Type-qualified access succeeds, but value access is rejected.
        var src = """
System.Console.Write(object.Member);

var o = new object();
_ = o.Member;

public static class E
{
    extension(object)
    {
        public const int Member = 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (4,7): error CS1061: 'object' does not contain a definition for 'Member' and no accessible extension method 'Member' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            // _ = o.Member;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Member").WithArguments("object", "Member").WithLocation(4, 7));
    }

    [Fact]
    public void Betterness_01()
    {
        // more specific extension constant wins
        var src = """
System.Console.Write(object.Member);
System.Console.Write(string.Member);

public static class E1
{
    extension(object)
    {
        public const int Member = 1;
    }
}

public static class E2
{
    extension(string)
    {
        public const int Member = 2;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void Betterness_02()
    {
        // Fewer custom modifiers wins.
        var il = """
.class public auto ansi abstract sealed beforefieldinit E1
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

    .class nested public auto ansi sealed specialname '<G>$C43E2675C7BBF9284AF22FB8A9BF0280'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

        .class nested public auto ansi abstract sealed specialname '<M>$C43E2675C7BBF9284AF22FB8A9BF0280'
            extends System.Object
        {
            .field public static literal int32 Member = int32(1)

            .method public hidebysig specialname static void '<Extension>$' ( object '' ) cil managed
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                .maxstack 8
                IL_0000: ret
            }
        }
    }
}

.class public auto ansi abstract sealed beforefieldinit E2
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

    .class nested public auto ansi sealed specialname '<G>$C43E2675C7BBF9284AF22FB8A9BF0280'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

        .class nested public auto ansi abstract sealed specialname '<M>$C43E2675C7BBF9284AF22FB8A9BF0280'
            extends System.Object
        {
            .field public static literal int32 modopt([mscorlib]System.String) Member = int32(2)

            .method public hidebysig specialname static void '<Extension>$' ( object '' ) cil managed
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                .maxstack 8
                IL_0000: ret
            }
        }
    }
}
""";

        var src = """
System.Console.Write(object.Member);
""";

        var comp = CreateCompilationWithIL(src, il, parseOptions: TestOptions.RegularPreview);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single(n => n.ToString() == "object.Member");
        AssertEx.Equal("System.Int32 E1.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.Member", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void Betterness_03()
    {
        // Non-generic extension constant wins over a generic extension constant when parameter types are otherwise equivalent.
        var src = """
System.Console.Write(object.Member);

public static class E1
{
    extension(object)
    {
        public const int Member = 1;
    }
}

public static class E2
{
    extension<T>(object)
    {
        public const int Member = 2;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void Betterness_04()
    {
        // Ref kind of the extension parameter is ignored for static extension member betterness.
        var src = """
_ = int.Member;

public static class E1
{
    extension(int)
    {
        public const int Member = 1;
    }
}

public static class E2
{
    extension(in int i)
    {
        public const int Member = 2;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (1,5): error CS9339: The extension resolution is ambiguous between the following members: 'E1.extension(int).Member' and 'E2.extension(in int).Member'
            // _ = int.Member;
            Diagnostic(ErrorCode.ERR_AmbigExtension, "int.Member").WithArguments("E1.extension(int).Member", "E2.extension(in int).Member").WithLocation(1, 5));
    }

    [Fact]
    public void Constant_01()
    {
        // decimal extension constant
        var src = """
_ = object.Member + 0.25m;

public static class E
{
    extension(object)
    {
        public const decimal Member = 1.25m;
    }
}
""";

        CreateCompilation(src).VerifyDiagnostics();
    }

    [Fact]
    public void Constant_02()
    {
        // extension constant in contexts that require constant expressions
        var src = """
[A(object.Member)]
class C
{
    const int Local = object.Member;

    void M(int x = object.Member) { }

    void Test(int value)
    {
        _ = value is object.Member;

        switch (value)
        {
            case object.Member:
                break;
        }
    }
}

class A : System.Attribute
{
    public A(int i) { }
}

public static class E
{
    extension(object)
    {
        public const int Member = 42;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Constant_04()
    {
        // Disambiguation usage
        var src = """
System.Console.Write(E1.Member);
System.Console.Write(E2.Member);

static class E1
{
    extension(int)
    {
        public const int Member = 1;
    }
}

static class E2
{
    extension(int)
    {
        public const int Member = 2;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void Constant_05()
    {
        // Members conflict
        var src = """
static class E
{
    extension(int)
    {
        public const int Member = 1;
    }

    extension(string)
    {
        public const int Member = 2;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (10,26): error CS0102: The type 'E' already contains a definition for 'Member'
            //         public const int Member = 2;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Member").WithArguments("E", "Member").WithLocation(10, 26));
    }

    [Fact]
    public void Constant_06()
    {
        // Members conflict, generic extension block
        var src = """
static class E
{
    extension(int)
    {
        public const int Member = 1;
    }

    extension<T>(T)
    {
        public const int Member = 2;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (10,26): error CS0102: The type 'E' already contains a definition for 'Member'
            //         public const int Member = 2;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Member").WithArguments("E", "Member").WithLocation(10, 26));
    }

    [Theory]
    [InlineData("public static int Member = 0;")]
    [InlineData("public static void Member(int i) { }")]
    [InlineData("public static int Member => 0;")]
    public void Constant_07(string existingMember)
    {
        // Member conflicts with existing enclosing type member
        var src = $$"""
static class E
{
    {{existingMember}}

    extension(int)
    {
        public const int Member = 1;
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (7,26): error CS0102: The type 'E' already contains a definition for 'Member'
            //         public const int Member = 1;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Member").WithArguments("E", "Member").WithLocation(7, 26));
    }

    [Fact]
    public void Constant_08()
    {
        // Disambiguation usage with a generic extension block.
        var src = """
System.Console.Write(E.Member);

static class E
{
    extension<T>(T)
    {
        public const int Member = 42;
    }
}
""";

        CompileAndVerify(src, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void Nullability_01()
    {
        var src = """
#nullable enable

object.MaybeNull.ToString();
object.NotNull.ToString();

public static class E
{
    extension(object)
    {
        public const string? MaybeNull = null;
        public const string NotNull = "";
    }
}
""";

        CreateCompilation(src).VerifyEmitDiagnostics(
            // (3,1): warning CS8602: Dereference of a possibly null reference.
            // object.MaybeNull.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "object.MaybeNull").WithLocation(3, 1));
    }

    [Fact]
    public void LookupSymbols_01()
    {
        // value receiver
        var src = """
class C
{
    void M()
    {
        var o = new object();
        _ = o.Member;
    }
}

public static class E
{
    extension(object)
    {
        public const int Member = 42;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (6,15): error CS1061: 'object' does not contain a definition for 'Member' and no accessible extension method 'Member' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            //         _ = o.Member;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Member").WithArguments("object", "Member").WithLocation(6, 15));

        var (model, receiver, type) = getReceiverInfo(comp);

        var member = model.LookupSymbols(receiver.SpanStart, type, "Member", includeReducedExtensionMethods: true).Single();
        AssertEx.Equal("System.Int32 E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.Member", member.ToTestDisplayString());
        Assert.Contains(member, model.LookupSymbols(receiver.SpanStart, type, includeReducedExtensionMethods: true));
        Assert.Empty(model.LookupSymbols(receiver.SpanStart, type, "Member", includeReducedExtensionMethods: false));

        static (SemanticModel model, ExpressionSyntax receiver, ITypeSymbol type) getReceiverInfo(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var receiver = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single().Expression;
            var type = model.GetTypeInfo(receiver).Type;
            AssertEx.Equal("System.Object", type.ToTestDisplayString());
            return (model, receiver, type);
        }
    }

    [Fact]
    public void LookupSymbols_02()
    {
        // applicable and inapplicable extension constants
        var src = """
class C
{
    void M()
    {
        var s = "";
        _ = s.Member;
    }
}

public static class E1
{
    extension(string)
    {
        public const int Member = 1;
    }
}

public static class E2
{
    extension(int)
    {
        public const int Member = 2;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (6,15): error CS1061: 'string' does not contain a definition for 'Member' and no accessible extension method 'Member' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
            //         _ = s.Member;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Member").WithArguments("string", "Member").WithLocation(6, 15));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var receiver = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single().Expression;
        var type = model.GetTypeInfo(receiver).Type;
        AssertEx.Equal("System.String", type.ToTestDisplayString());

        var member = model.LookupSymbols(receiver.SpanStart, type, "Member", includeReducedExtensionMethods: true).Single();
        AssertEx.Equal("System.Int32 E1.<G>$34505F560D9EACF86A87F3ED1F85E448.Member", member.ToTestDisplayString());
    }

    [Fact]
    public void LookupSymbols_03()
    {
        // generic extension constant with constraints
        var src = """
class C
{
    void M()
    {
        string s = "";
        _ = s.Member;
    }
}

public static class E
{
    extension<T>(T) where T : class
    {
        public const int Member = 42;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (6,15): error CS1061: 'string' does not contain a definition for 'Member' and no accessible extension method 'Member' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
            //         _ = s.Member;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Member").WithArguments("string", "Member").WithLocation(6, 15));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var receiver = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single().Expression;
        var type = model.GetTypeInfo(receiver).Type;

        var member = model.LookupSymbols(receiver.SpanStart, type, "Member", includeReducedExtensionMethods: true).Single();
        AssertEx.Equal("System.Int32 E.<G>$66F77D1E46F965A5B22D4932892FA78B<System.String>.Member", member.ToTestDisplayString());
    }

    [Fact]
    public void LookupSymbols_04()
    {
        // inaccessible extension constant
        var src = """
class C
{
    void M()
    {
        var o = new object();
        _ = o.Member;
    }
}

public static class E
{
    extension(object)
    {
        private const int Member = 42;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (6,15): error CS1061: 'object' does not contain a definition for 'Member' and no accessible extension method 'Member' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            //         _ = o.Member;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Member").WithArguments("object", "Member").WithLocation(6, 15));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var receiver = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single().Expression;
        var type = model.GetTypeInfo(receiver).Type;

        Assert.Empty(model.LookupSymbols(receiver.SpanStart, type, "Member", includeReducedExtensionMethods: true));
    }

    [Fact]
    public void LookupSymbols_05()
    {
        // extension constant with extension type parameter not fully inferred from receiver
        var src = """
class C
{
    void M()
    {
        var o = new object();
        _ = o.Member;
    }
}

public static class E
{
    extension<T>(object)
    {
        public const int Member = 42;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (6,15): error CS1061: 'object' does not contain a definition for 'Member' and no accessible extension method 'Member' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            //         _ = o.Member;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Member").WithArguments("object", "Member").WithLocation(6, 15));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var receiver = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single().Expression;
        var type = model.GetTypeInfo(receiver).Type;
        AssertEx.Equal("System.Object", type.ToTestDisplayString());

        var member = model.LookupSymbols(receiver.SpanStart, type, "Member", includeReducedExtensionMethods: true).Single();
        AssertEx.Equal("System.Int32 E.<G>$F3EC63F55CD2663D3F6B00F6D7E0AC7E<T>.Member", member.ToTestDisplayString());
    }

    [Fact]
    public void Cref_01()
    {
        var src = """
/// <see cref="E.extension(int).Member"/>
/// <see cref="E.Member"/>
static class E
{
    extension(int)
    {
        /// <see cref="Member"/>
        public const int Member = 1;
    }
}
""";

        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="F:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.Member"/>
    <see cref="F:E.Member"/>
</member>

""", e.GetDocumentationCommentXml());

        var extensionConstant = e.GetTypeMembers().Single().GetMember<FieldSymbol>("Member");
        AssertEx.Equal("""
<member name="F:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.Member">
    <see cref="F:E.Member"/>
</member>

""", extensionConstant.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(int).Member, System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.Member)",
            "(E.Member, System.Int32 E.Member)",
            "(Member, System.Int32 E.Member)"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_02()
    {
        var src = """
/// <see cref="E.extension{T}(T).Member"/>
/// <see cref="E.Member"/>
static class E
{
    extension<T>(T)
    {
        public const int Member = 1;
    }
}
""";

        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="F:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB`1.Member"/>
    <see cref="F:E.Member"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension{T}(T).Member, System.Int32 E.<G>$8048A6C8BE30A622530249B904B537EB<T>.Member)",
            "(E.Member, System.Int32 E.Member)"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void XmlDoc_01()
    {
        var src = """
#nullable disable
static class E
{
    /// <summary>Summary for extension block</summary>
    /// <typeparam name="T">Description for T</typeparam>
    extension<T>(T)
    {
#nullable enable
        /// <summary>Summary for constant with reference to <typeparamref name="T"/>.</summary>
        public const string? Member = null;
    }
}
""";
        var comp = CreateCompilation(src, assemblyName: "assembly", parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var expected = """
<?xml version="1.0"?>
<doc>
    <assembly>
        <name>assembly</name>
    </assembly>
    <members>
        <member name="T:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB`1.&lt;M&gt;$01CE3801593377B4E240F33E20D30D50">
            <summary>Summary for extension block</summary>
            <typeparam name="T">Description for T</typeparam>
        </member>
        <member name="F:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB`1.Member">
            <summary>Summary for constant with reference to <typeparamref name="T"/>.</summary>
        </member>
    </members>
</doc>
""";
        AssertEx.Equal(expected, GetDocumentationCommentText(comp));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(T, T)", "(T, T)"], PrintXmlNameSymbols(tree, model));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var implementationField = e.GetMember<FieldSymbol>("Member");
        AssertEx.Equal("System.String? E.Member", implementationField.ToTestDisplayString(includeNonNullable: true));
        Assert.Equal(NullableAnnotation.Annotated, implementationField.TypeWithAnnotations.NullableAnnotation);

        var comp2 = CreateCompilation("", references: [comp.EmitToImageReference()], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
        var implementationFieldFromMetadata = comp2.GetMember<NamedTypeSymbol>("E").GetMember<FieldSymbol>("Member");
        AssertEx.Equal("System.String? E.Member", implementationFieldFromMetadata.ToTestDisplayString(includeNonNullable: true));
        Assert.Equal(NullableAnnotation.Annotated, implementationFieldFromMetadata.TypeWithAnnotations.NullableAnnotation);
    }

    [Theory, CombinatorialData]
    public void Nullability_NullableContext_01(bool useCompilationReference)
    {
        // non-nullable extension constant
        var src = """
#nullable enable

public static class E
{
    extension(object)
    {
        public const string Member = "";
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics();

        var src2 = """
#nullable enable

object.Member.ToString();
E.Member.ToString();
""";
        var comp2 = CreateCompilation(src2, references: [AsReference(comp, useCompilationReference)], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics();
        verifySymbols(comp2, includeNonNullable: true);

        static void verifySymbols(CSharpCompilation comp, bool includeNonNullable)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var objectMember = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single(n => n.ToString() == "object.Member");
            var eMember = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single(n => n.ToString() == "E.Member");

            AssertEx.Equal("System.String! E.extension(System.Object!).Member", model.GetSymbolInfo(objectMember).Symbol.ToTestDisplayString(includeNonNullable: includeNonNullable));
            AssertEx.Equal("System.String! E.Member", model.GetSymbolInfo(eMember).Symbol.ToTestDisplayString(includeNonNullable: includeNonNullable));
        }
    }

    [Theory, CombinatorialData]
    public void Nullability_NullableContext_02(bool useCompilationReference)
    {
        // nullable extension constant
        var src = """
#nullable enable

public static class E
{
    extension(object)
    {
        public const string? Member = null;
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics();

        var src2 = """
#nullable enable

object.Member.ToString();
E.Member.ToString();
""";
        var comp2 = CreateCompilation(src2, references: [AsReference(comp, useCompilationReference)], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(
            // (3,1): warning CS8602: Dereference of a possibly null reference.
            // object.Member.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "object.Member").WithLocation(3, 1),
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // E.Member.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.Member").WithLocation(4, 1));
        verifySymbols(comp2, includeNonNullable: true);

        static void verifySymbols(CSharpCompilation comp, bool includeNonNullable)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var objectMember = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single(n => n.ToString() == "object.Member");
            var eMember = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single(n => n.ToString() == "E.Member");

            AssertEx.Equal("System.String? E.extension(System.Object!).Member", model.GetSymbolInfo(objectMember).Symbol.ToTestDisplayString(includeNonNullable: includeNonNullable));
            AssertEx.Equal("System.String? E.Member", model.GetSymbolInfo(eMember).Symbol.ToTestDisplayString(includeNonNullable: includeNonNullable));
        }
    }

    [Theory, CombinatorialData]
    public void Nullability_NullableContext_03(bool useCompilationReference)
    {
        // nullable extension constant with local nullable context that differs from the enclosing type
        var src = """
#nullable disable

public static class E
{
    extension(object)
    {
#nullable enable
        public const string? Member = null;
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics();

        var src2 = """
#nullable enable

object.Member.ToString();
E.Member.ToString();
""";
        var comp2 = CreateCompilation(src2, references: [AsReference(comp, useCompilationReference)], targetFramework: TargetFramework.Net100);
        comp2.VerifyEmitDiagnostics(
            // (3,1): warning CS8602: Dereference of a possibly null reference.
            // object.Member.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "object.Member").WithLocation(3, 1),
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // E.Member.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.Member").WithLocation(4, 1));
        verifySymbols(comp2, includeNonNullable: true);

        static void verifySymbols(CSharpCompilation comp, bool includeNonNullable)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var objectMember = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single(n => n.ToString() == "object.Member");
            var eMember = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single(n => n.ToString() == "E.Member");

            AssertEx.Equal("System.String? E.extension(System.Object).Member", model.GetSymbolInfo(objectMember).Symbol.ToTestDisplayString(includeNonNullable: includeNonNullable));
            AssertEx.Equal("System.String? E.Member", model.GetSymbolInfo(eMember).Symbol.ToTestDisplayString(includeNonNullable: includeNonNullable));
        }
    }

    [Theory, CombinatorialData]
    public void Metadata_01(bool useCompilationReference)
    {
        var src = """
public static class E
{
    extension(object)
    {
        public const int I = 1;
        private const string S = "hello";
    }
}
""";

        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp);
        var reference = AsReference(comp, useCompilationReference);
        verifySymbols(comp.GetMember<NamedTypeSymbol>("E"), fromMetadata: false);

        var src2 = """
_ = object.I;
""";

        var comp2 = CreateCompilation(src2, references: [reference]);
        comp2.VerifyEmitDiagnostics();
        verifySymbols(comp2.GetMember<NamedTypeSymbol>("E"), fromMetadata: !useCompilationReference);

        verifier.VerifyTypeIL("E", actual => AssertEx.AssertEqualToleratingWhitespaceDifferences("""
.class public auto ansi abstract sealed beforefieldinit E
    extends [netstandard]System.Object
{
    .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$C43E2675C7BBF9284AF22FB8A9BF0280'
        extends [netstandard]System.Object
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$C43E2675C7BBF9284AF22FB8A9BF0280'
            extends [netstandard]System.Object
        {
            // Fields
            .field public static literal int32 I = int32(1)
            .field private static literal string S = "hello"

            // Methods
            .method public hidebysig specialname static
                void '<Extension>$' (
                    object ''
                ) cil managed
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x2067
                // Header size: 1
                // Code size: 1 (0x1)
                .maxstack 8

                IL_0000: ret
            } // end of method '<M>$C43E2675C7BBF9284AF22FB8A9BF0280'::'<Extension>$'

        } // end of class <M>$C43E2675C7BBF9284AF22FB8A9BF0280


    } // end of class <G>$C43E2675C7BBF9284AF22FB8A9BF0280


    // Fields
    .field public static literal int32 I = int32(1)
    .field private static literal string S = "hello"

} // end of class E

""", actual.Replace("[mscorlib]", "[netstandard]")));

        void verifySymbols(NamedTypeSymbol e, bool fromMetadata)
        {
            var extension = e.GetTypeMembers("").Single(t => t.IsExtension);
            Assert.True(extension.IsExtension);

            var i = extension.GetMember<FieldSymbol>("I");
            VerifyExtensionConstant(i, "I", Accessibility.Public, SpecialType.System_Int32, 1, fromMetadata);
            AssertEx.Equal("System.Int32 E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.I", i.ToTestDisplayString());
            AssertEx.Equal("E.extension(object).I", i.ToDisplayString());

            var implementationI = e.GetMember<FieldSymbol>("I");
            VerifyExtensionConstant(implementationI, "I", Accessibility.Public, SpecialType.System_Int32, 1, fromMetadata, isExtensionBlockMember: false);
            AssertEx.Equal("System.Int32 E.I", implementationI.ToTestDisplayString());
            AssertEx.Equal("E.I", implementationI.ToDisplayString());

            var s = extension.GetMember<FieldSymbol>("S");
            var implementationS = e.GetMember<FieldSymbol>("S");
            if (fromMetadata)
            {
                Assert.Null(s);
                Assert.Null(implementationS); // TODo2
            }
            else
            {
                VerifyExtensionConstant(s, "S", Accessibility.Private, SpecialType.System_String, "hello", fromMetadata);
                AssertEx.Equal("System.String E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.S", s.ToTestDisplayString());
                AssertEx.Equal("E.extension(object).S", s.ToDisplayString());
                VerifyExtensionConstant(implementationS, "S", Accessibility.Private, SpecialType.System_String, "hello", fromMetadata, isExtensionBlockMember: false);
                AssertEx.Equal("System.String E.S", implementationS.ToTestDisplayString());
                AssertEx.Equal("E.S", implementationS.ToDisplayString());
            }
        }
    }

    [Theory, CombinatorialData]
    public void Metadata_03(bool useCompilationReference)
    {
        // two consts into a merged marker type
        var src = """
public static class E
{
    extension(object)
    {
        public const int I = 1;
    }

    extension(object)
    {
        public const int J = 2;
    }
}
""";

        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp);
        var reference = AsReference(comp, useCompilationReference);
        verifySymbols(comp.GetMember<NamedTypeSymbol>("E"), fromMetadata: false);

        var src2 = """
System.Console.Write(object.I + object.J);
""";

        CompileAndVerify(src2, references: [reference], expectedOutput: "3").VerifyDiagnostics();
        var comp2 = CreateCompilation(src2, references: [reference]);
        comp2.VerifyEmitDiagnostics();
        verifySymbols(comp2.GetMember<NamedTypeSymbol>("E"), fromMetadata: !useCompilationReference);

        verifier.VerifyTypeIL("E", actual =>
        {
            actual = actual.Replace("[mscorlib]", "[netstandard]");
            Assert.Contains(".class nested public auto ansi sealed specialname '<G>$C43E2675C7BBF9284AF22FB8A9BF0280'", actual);
            Assert.Contains(".class nested public auto ansi abstract sealed specialname '<M>$C43E2675C7BBF9284AF22FB8A9BF0280'", actual);
            Assert.Contains(".field public static literal int32 I = int32(1)", actual);
            Assert.Contains(".field public static literal int32 J = int32(2)", actual);
        });

        void verifySymbols(NamedTypeSymbol e, bool fromMetadata)
        {
            var extensions = e.GetTypeMembers("").Where(t => t.IsExtension).ToArray();
            Assert.Equal(fromMetadata ? 1 : 2, extensions.Length);

            var extension = fromMetadata ? extensions.Single() : extensions.Single(t => t.GetMember<FieldSymbol>("I") is not null);
            Assert.True(extension.IsExtension);
            var i = extension.GetMember<FieldSymbol>("I");
            VerifyExtensionConstant(i, "I", Accessibility.Public, SpecialType.System_Int32, 1, fromMetadata);
            AssertEx.Equal("System.Int32 E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.I", i.ToTestDisplayString());

            extension = fromMetadata ? extension : extensions.Single(t => t.GetMember<FieldSymbol>("J") is not null);
            Assert.True(extension.IsExtension);
            var j = extension.GetMember<FieldSymbol>("J");
            VerifyExtensionConstant(j, "J", Accessibility.Public, SpecialType.System_Int32, 2, fromMetadata);
            AssertEx.Equal("System.Int32 E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.J", j.ToTestDisplayString());
        }
    }

    [Fact]
    public void Metadata_04()
    {
        // ref assembly
        var src = """
public static class E
{
    extension(object)
    {
        public const int I = 1;
        private const string S = "hello";
    }
}
""";

        var comp = CreateCompilation(src);
        var emitOptions = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(true);
        CompileAndVerify(comp, emitOptions: emitOptions).VerifyDiagnostics();

        var reference = comp.EmitToImageReference(emitOptions);
        var comp2 = CreateCompilation("", references: [reference], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

        var e = comp2.GetMember<NamedTypeSymbol>("E");
        var extension = e.GetTypeMembers("").Single(t => t.IsExtension);
        Assert.True(extension.IsExtension);

        var i = extension.GetMember<FieldSymbol>("I");
        VerifyExtensionConstant(i, "I", Accessibility.Public, SpecialType.System_Int32, 1, fromMetadata: true);
        AssertEx.Equal("System.Int32 E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.I", i.ToTestDisplayString());
        AssertEx.Equal("E.extension(object).I", i.ToDisplayString());

        var s = extension.GetMember<FieldSymbol>("S");
        VerifyExtensionConstant(s, "S", Accessibility.Private, SpecialType.System_String, "hello", fromMetadata: true);
        AssertEx.Equal("System.String E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.S", s.ToTestDisplayString());
        AssertEx.Equal("E.extension(object).S", s.ToDisplayString());
    }

    [Theory, CombinatorialData]
    public void Metadata_02(bool useCompilationReference)
    {
        // decimal extension constant
        var src = """
public static class E
{
    extension(object)
    {
        public const decimal D = 1.25m;
    }
}
""";

        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp);
        var reference = AsReference(comp, useCompilationReference);
        verifySymbols(comp.GetMember<NamedTypeSymbol>("E"), fromMetadata: false);

        var src2 = """
System.Console.Write(object.D == 1.25m);

var group = typeof(E).GetNestedType("<G>$C43E2675C7BBF9284AF22FB8A9BF0280", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
var marker = group.GetNestedType("<M>$C43E2675C7BBF9284AF22FB8A9BF0280", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
var field = marker.GetField("D", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
System.Console.Write(field.GetValue(null).Equals(1.25m));
""";

        // Extension marker types are compiler metadata only. There is no .cctor to initialize the
        // emitted decimal marker field.
        CompileAndVerify(src2, references: [reference], expectedOutput: "TrueFalse").VerifyDiagnostics();
        var comp2 = CreateCompilation(src2, references: [reference]);
        comp2.VerifyEmitDiagnostics();
        verifySymbols(comp2.GetMember<NamedTypeSymbol>("E"), fromMetadata: !useCompilationReference);

        verifier.VerifyTypeIL("E", actual => AssertEx.AssertEqualToleratingWhitespaceDifferences("""
.class public auto ansi abstract sealed beforefieldinit E
    extends [netstandard]System.Object
{
    .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$C43E2675C7BBF9284AF22FB8A9BF0280'
        extends [netstandard]System.Object
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$C43E2675C7BBF9284AF22FB8A9BF0280'
            extends [netstandard]System.Object
        {
            // Fields
            .field public static initonly valuetype [netstandard]System.Decimal D
            .custom instance void [netstandard]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = (
                01 00 02 00 00 00 00 00 00 00 00 00 7d 00 00 00
                00 00
            )

            // Methods
            .method public hidebysig specialname static
                void '<Extension>$' (
                    object ''
                ) cil managed
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x2067
                // Header size: 1
                // Code size: 1 (0x1)
                .maxstack 8

                IL_0000: ret
            } // end of method '<M>$C43E2675C7BBF9284AF22FB8A9BF0280'::'<Extension>$'

        } // end of class <M>$C43E2675C7BBF9284AF22FB8A9BF0280


    } // end of class <G>$C43E2675C7BBF9284AF22FB8A9BF0280


    // Fields
    .field public static initonly valuetype [netstandard]System.Decimal D
    .custom instance void [netstandard]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = (
        01 00 02 00 00 00 00 00 00 00 00 00 7d 00 00 00
        00 00
    )

} // end of class E

""", actual.Replace("[mscorlib]", "[netstandard]")));

        void verifySymbols(NamedTypeSymbol e, bool fromMetadata)
        {
            var extension = e.GetTypeMembers("").Single(t => t.IsExtension);
            Assert.True(extension.IsExtension);

            var d = extension.GetMember<FieldSymbol>("D");
            VerifyExtensionConstant(d, "D", Accessibility.Public, SpecialType.System_Decimal, 1.25m, fromMetadata);
            AssertEx.Equal("System.Decimal E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.D", d.ToTestDisplayString());

            var implementationD = e.GetMember<FieldSymbol>("D");
            VerifyExtensionConstant(implementationD, "D", Accessibility.Public, SpecialType.System_Decimal, 1.25m, fromMetadata, isExtensionBlockMember: false);
            AssertEx.Equal("System.Decimal E.D", implementationD.ToTestDisplayString());
            AssertEx.Equal("E.D", implementationD.ToDisplayString());
        }
    }

    [Theory, CombinatorialData]
    public void Metadata_07(bool useCompilationReference)
    {
        // constants requiring synthesized attributes on implementation fields
        var src = """
public static class E
{
    extension(object)
    {
        public const dynamic Dynamic = null;
        public const nint NativeInt = 1;
        public const (int First, int Second)[] Tuple = null;
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
        CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics();
        var reference = AsReference(comp, useCompilationReference);

        var comp2 = CreateCompilation("", references: [reference], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), targetFramework: TargetFramework.Net100);
        verifySymbols(comp2.GetMember<NamedTypeSymbol>("E"));

        static void verifySymbols(NamedTypeSymbol e)
        {
            var extension = e.GetTypeMembers("").Single(t => t.IsExtension);

            verifyField(extension.GetMember<FieldSymbol>("Dynamic"), "Dynamic", Accessibility.Public, SpecialType.None, null, "dynamic E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.Dynamic", isExtension: true);
            verifyField(e.GetMember<FieldSymbol>("Dynamic"), "Dynamic", Accessibility.Public, SpecialType.None, null, "dynamic E.Dynamic", isExtension: false);

            verifyField(extension.GetMember<FieldSymbol>("NativeInt"), "NativeInt", Accessibility.Public, SpecialType.System_IntPtr, 1, "System.IntPtr E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.NativeInt", isExtension: true);
            verifyField(e.GetMember<FieldSymbol>("NativeInt"), "NativeInt", Accessibility.Public, SpecialType.System_IntPtr, 1, "System.IntPtr E.NativeInt", isExtension: false);

            verifyField(extension.GetMember<FieldSymbol>("Tuple"), "Tuple", Accessibility.Public, SpecialType.None, null, "(System.Int32 First, System.Int32 Second)[] E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.Tuple", isExtension: true);
            verifyField(e.GetMember<FieldSymbol>("Tuple"), "Tuple", Accessibility.Public, SpecialType.None, null, "(System.Int32 First, System.Int32 Second)[] E.Tuple", isExtension: false);

            static void verifyField(FieldSymbol field, string name, Accessibility accessibility, SpecialType specialType, object constantValue, string display, bool isExtension)
            {
                VerifyExtensionConstant(field, name, accessibility, specialType, constantValue, fromMetadata: false, isExtension);
                AssertEx.Equal(display, field.ToTestDisplayString());

                switch (name)
                {
                    case "Dynamic":
                        Assert.True(field.TypeWithAnnotations.Type.IsDynamic());
                        break;
                    case "NativeInt":
                        Assert.True(field.TypeWithAnnotations.Type.IsNativeIntegerType);
                        break;
                    case "Tuple":
                        AssertEx.Equal("(System.Int32 First, System.Int32 Second)[]", field.TypeWithAnnotations.Type.ToTestDisplayString());
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(name);
                }
            }
        }
    }

    [Theory, CombinatorialData]
    public void Metadata_05(bool useCompilationReference, [CombinatorialValues("public", "internal", "private", "")] string accessibility)
    {
        // constant accessibility round-trip
        var src = $$"""
public static class E
{
    extension(object)
    {
        {{accessibility}} const int I = 1;
    }
}
""";

        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        var reference = AsReference(comp, useCompilationReference);

        var expectedAccessibility = accessibility switch
        {
            "public" => Accessibility.Public,
            "internal" => Accessibility.Internal,
            _ => Accessibility.Private,
        };

        var comp2 = CreateCompilation("", references: [reference], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
        var field = comp2.GetMember<NamedTypeSymbol>("E").GetTypeMembers("").Single(t => t.IsExtension).GetMember<FieldSymbol>("I");
        VerifyExtensionConstant(field, "I", expectedAccessibility, SpecialType.System_Int32, 1, fromMetadata: !useCompilationReference);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("initonly ", true)]
    public void Metadata_06(string fieldModifiers, bool isReadOnly)
    {
        // non-const field in extension marker type
        var il = $$"""
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

    .class nested public auto ansi sealed specialname '<G>$C43E2675C7BBF9284AF22FB8A9BF0280'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

        .class nested public auto ansi abstract sealed specialname '<M>$C43E2675C7BBF9284AF22FB8A9BF0280'
            extends System.Object
        {
            .field public static {{fieldModifiers}}int32 I

            .method public hidebysig specialname static void '<Extension>$' ( object '' ) cil managed
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                .maxstack 8
                IL_0000: ret
            }
        }
    }
}
""";

        var src = """
_ = object.I;
""";

        var comp = CreateCompilationWithIL(src, il, parseOptions: TestOptions.RegularPreview);
        comp.VerifyEmitDiagnostics(
            // (1,12): error CS0117: 'object' does not contain a definition for 'I'
            // _ = object.I;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "I").WithArguments("object", "I").WithLocation(1, 12));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = e.GetTypeMembers("").Single(t => t.IsExtension);

        var field = extension.GetMember<FieldSymbol>("I");
        Assert.NotNull(field);
        Assert.False(field.IsConst);
        Assert.False(field.HasConstantValue);
        Assert.Equal(isReadOnly, field.IsReadOnly);

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Last();
        var lookup = model.LookupSymbols(memberAccess.SpanStart, name: "I", includeReducedExtensionMethods: true);
        Assert.Empty(lookup);
    }

    private static void VerifyExtensionConstant(FieldSymbol field, string name, Accessibility accessibility, SpecialType specialType, object constantValue, bool fromMetadata, bool isExtensionBlockMember = true)
    {
        Assert.Equal(name, field.Name);
        Assert.True(field.IsConst);
        Assert.True(field.HasConstantValue);
        Assert.Equal(constantValue, field.ConstantValue);
        Assert.Equal(accessibility, field.DeclaredAccessibility);
        Assert.Equal(specialType, field.Type.SpecialType);
        Assert.Equal(isExtensionBlockMember, field.ContainingType.IsExtension);

        if (fromMetadata)
        {
            Assert.IsType<PEFieldSymbol>(field);
        }
    }
}
