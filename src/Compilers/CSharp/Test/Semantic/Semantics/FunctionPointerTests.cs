// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.UnitTests.FunctionPointerUtilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FunctionPointerTests : CompilingTestBase
    {
        private static CSharpCompilation CreateCompilationWithFunctionPointers(string source, CSharpCompilationOptions? options = null, CSharpParseOptions? parseOptions = null, TargetFramework? targetFramework = null)
        {
            return CreateCompilation(source, options: options ?? TestOptions.UnsafeReleaseDll, parseOptions: parseOptions, targetFramework: targetFramework ?? TargetFramework.Standard);
        }

        private CompilationVerifier CompileAndVerifyFunctionPointers(CSharpCompilation compilation, string? expectedOutput = null)
        {
            return CompileAndVerify(compilation, verify: Verification.Skipped, expectedOutput: expectedOutput);
        }

        [Fact]
        public void LangVersion()
        {
            var src = """
                #pragma warning disable 169 // Unused field
                unsafe class C
                {
                    delegate*<void> f;
                }
                """;

            var comp = CreateCompilationWithFunctionPointers(src, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,5): error CS8400: Feature 'function pointers' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     delegate*<void> f;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "delegate").WithArguments("function pointers", "9.0").WithLocation(4, 5)
            );

            comp = CreateCompilationWithFunctionPointers(src, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UsingAliasTest()
        {
            var src = @"
using s = delegate*<void>;";

            var comp = CreateCompilationWithFunctionPointers(src, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (2,11): error CS9058: Feature 'using type alias' is not available in C# 11.0. Please use language version 12.0 or greater.
                // using s = delegate*<void>;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "delegate*<void>").WithArguments("using type alias", "12.0").WithLocation(2, 11),
                // (2,7): warning CS8981: The type name 's' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // using s = delegate*<void>;
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "s").WithArguments("s").WithLocation(2, 7),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using s = delegate*<void>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using s = delegate*<void>;").WithLocation(2, 1));

            comp = CreateCompilationWithFunctionPointers(src, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (2,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // using s = delegate*<void>;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(2, 11),
                // (2,7): warning CS8981: The type name 's' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // using s = delegate*<void>;
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "s").WithArguments("s").WithLocation(2, 7),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using s = delegate*<void>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using s = delegate*<void>;").WithLocation(2, 1));
        }

        [Fact]
        public void UsingAliasTest_UnsafeModifier()
        {
            var src = @"
using unsafe S = delegate*<void>;";

            var comp = CreateCompilationWithFunctionPointers(src, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular11);

            comp.VerifyDiagnostics(
                // (2,7): error CS9058: Feature 'using type alias' is not available in C# 11.0. Please use language version 12.0 or greater.
                // using unsafe S = delegate*<void>;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "unsafe").WithArguments("using type alias", "12.0").WithLocation(2, 7),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using unsafe S = delegate*<void>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe S = delegate*<void>;").WithLocation(2, 1));

            comp = CreateCompilationWithFunctionPointers(src, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular12);

            comp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using unsafe S = delegate*<void>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe S = delegate*<void>;").WithLocation(2, 1));

            comp = CreateCompilationWithFunctionPointers(src, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using unsafe S = delegate*<void>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe S = delegate*<void>;").WithLocation(2, 1));
        }

        [Fact]
        public void UsingAliasTest_TypeArgument()
        {
            var src = @"
using S = System.Collections.Generic.List<delegate*<void>[]>;";

            var comp = CreateCompilationWithFunctionPointers(src, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using S = System.Collections.Generic.List<delegate*<void>[]>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using S = System.Collections.Generic.List<delegate*<void>[]>;").WithLocation(2, 1));

            comp = CreateCompilationWithFunctionPointers(src, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using S = System.Collections.Generic.List<delegate*<void>[]>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using S = System.Collections.Generic.List<delegate*<void>[]>;").WithLocation(2, 1),
                // (2,43): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // using S = System.Collections.Generic.List<delegate*<void>[]>;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(2, 43));
        }

        [Fact]
        public void UsingAliasTest_TypeArgument_UnsafeModifier()
        {
            var src = @"
using unsafe S = System.Collections.Generic.List<delegate*<void>[]>;";

            var comp = CreateCompilationWithFunctionPointers(src, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (2,7): error CS9058: Feature 'using type alias' is not available in C# 11.0. Please use language version 12.0 or greater.
                // using unsafe S = System.Collections.Generic.List<delegate*<void>[]>;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "unsafe").WithArguments("using type alias", "12.0").WithLocation(2, 7),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using unsafe S = System.Collections.Generic.List<delegate*<void>[]>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe S = System.Collections.Generic.List<delegate*<void>[]>;").WithLocation(2, 1));

            comp = CreateCompilationWithFunctionPointers(src, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using unsafe S = System.Collections.Generic.List<delegate*<void>[]>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe S = System.Collections.Generic.List<delegate*<void>[]>;").WithLocation(2, 1));
        }

        [Fact]
        public void ImplicitConversionToVoid()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M1(delegate*<void> p1)
    {
        void* v1 = p1;
        delegate*<string> p2 = &M4;
        M2(p2);
        void* v2 = M3();
    }

    void M2(void* v1) {}
    delegate*<string, int> M3() => throw null;
    static string M4() => throw null;
}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("C.M1", expectedIL: @"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (void* V_0, //v1
                delegate*<string> V_1, //p2
                void* V_2) //v2
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldftn      ""string C.M4()""
  IL_0008:  stloc.1
  IL_0009:  ldarg.0
  IL_000a:  ldloc.1
  IL_000b:  call       ""void C.M2(void*)""
  IL_0010:  ldarg.0
  IL_0011:  call       ""delegate*<string, int> C.M3()""
  IL_0016:  stloc.2
  IL_0017:  ret
}
            ");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var initializer1 = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Initializer!.Value;
            assertResult(model, initializer1, comp);
            var parameter = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First().ArgumentList.Arguments.Single();
            assertResult(model, parameter.Expression, comp);
            var initializer2 = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Last().Initializer!.Value;
            assertResult(model, initializer2, comp);

            static void assertResult(SemanticModel model, ExpressionSyntax initializer1, Compilation comp)
            {
                var typeInfo = model.GetTypeInfo(initializer1);
                Assert.True(typeInfo.ConvertedType is IPointerTypeSymbol { PointedAtType: { SpecialType: SpecialType.System_Void } });
                Assert.Equal(TypeKind.FunctionPointer, typeInfo.Type!.TypeKind);
                var conversion = model.GetConversion(initializer1);
                Assert.Equal(ConversionKind.ImplicitPointerToVoid, conversion.Kind);
                Assert.True(conversion.IsImplicit);
                Assert.True(conversion.IsPointer);

                var classifiedConversion = comp.ClassifyConversion(typeInfo.Type, typeInfo.ConvertedType!);
                Assert.Equal(conversion, classifiedConversion);
            }
        }

        [Fact]
        public void ConversionToVoidAndBackRuns()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
unsafe class C
{
    static void Write() => Console.Write(1);
    static void* Get() => (delegate*<void>)&Write;
    static void Main()
    {
        void* ptr = Get();
        ((delegate*<void>)ptr)();
    }
}", options: TestOptions.UnsafeReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: "1", verify: Verification.Skipped);
            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  call       ""void* C.Get()""
  IL_0005:  calli      ""delegate*<void>""
  IL_000a:  ret
}
");
        }

        [Fact]
        public void ImplicitNullToFunctionPointer()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M()
    {
        delegate*<void> ptr1 = null;
        delegate* unmanaged[Cdecl]<void> ptr2 = null;
        delegate*<string> ptr3 = null;
        delegate*<C, int> ptr4 = null;
    }
}");
            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (delegate*<void> V_0, //ptr1
                delegate* unmanaged[Cdecl]<void> V_1, //ptr2
                delegate*<string> V_2, //ptr3
                delegate*<C, int> V_3) //ptr4
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  conv.u
  IL_0005:  stloc.1
  IL_0006:  ldc.i4.0
  IL_0007:  conv.u
  IL_0008:  stloc.2
  IL_0009:  ldc.i4.0
  IL_000a:  conv.u
  IL_000b:  stloc.3
  IL_000c:  ret
}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            foreach (var literal in tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(v => v.Initializer!.Value))
            {
                var conversion = model.GetConversion(literal);
                var typeInfo = model.GetTypeInfo(literal);
                Assert.Null(typeInfo.Type);
                Assert.Equal(TypeKind.FunctionPointer, typeInfo.ConvertedType!.TypeKind);
                Assert.Equal(ConversionKind.ImplicitNullToPointer, conversion.Kind);
                Assert.True(conversion.IsImplicit);
                Assert.True(conversion.IsPointer);

                var classifiedConversion = model.ClassifyConversion(literal, typeInfo.ConvertedType);
                Assert.Equal(conversion, classifiedConversion);
            }
        }

        [Theory]
        [InlineData("sbyte", "conv.i1", "conv.ovf.i1.un")]
        [InlineData("byte", "conv.u1", "conv.ovf.u1.un")]
        [InlineData("short", "conv.i2", "conv.ovf.i2.un")]
        [InlineData("ushort", "conv.u2", "conv.ovf.u2.un")]
        [InlineData("int", "conv.i4", "conv.ovf.i4.un")]
        [InlineData("uint", "conv.u4", "conv.ovf.u4.un")]
        [InlineData("long", "conv.u8", "conv.ovf.i8.un")]
        [InlineData("ulong", "conv.u8", "conv.u8")]
        public void FunctionPointerToNumericConversions(string type, string uncheckedInstruction, string checkedInstruction)
        {
            var comp = CreateCompilationWithFunctionPointers($@"
unsafe class C
{{
    public void M(delegate*<void> p)
    {{
        {type} num = ({type})p;
        {type}? numNullable = ({type}?)p;
        num = checked(({type})p);
    }}
}}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL(@"C.M", expectedIL: $@"
{{
  // Code size       10 (0xa)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  {uncheckedInstruction}
  IL_0002:  pop
  IL_0003:  ldarg.1
  IL_0004:  {uncheckedInstruction}
  IL_0005:  pop
  IL_0006:  ldarg.1
  IL_0007:  {checkedInstruction}
  IL_0008:  pop
  IL_0009:  ret
}}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var conversions = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().ToList();
            Assert.Equal(3, conversions.Count);

            var typeInfoOuter = model.GetTypeInfo(conversions[0]);
            var conversion = model.ClassifyConversion(conversions[0].Expression, typeInfoOuter.Type!);
            Assert.Equal(ConversionKind.ExplicitPointerToInteger, conversion.Kind);
            Assert.False(conversion.IsImplicit);
            Assert.True(conversion.IsPointer);

            typeInfoOuter = model.GetTypeInfo(conversions[1]);
            conversion = model.ClassifyConversion(conversions[1].Expression, typeInfoOuter.Type!);
            Assert.Equal(ConversionKind.ExplicitNullable, conversion.Kind);
            var underlying = conversion.UnderlyingConversions.Single();
            Assert.Equal(ConversionKind.ExplicitPointerToInteger, underlying.Kind);
            Assert.False(underlying.IsImplicit);
            Assert.True(underlying.IsPointer);
        }

        [Theory]
        [InlineData("IntPtr")]
        [InlineData("UIntPtr")]
        public void FunctionPointerToNumericConversions_IntUIntPtr(string type)
        {
            var comp = CreateCompilationWithFunctionPointers($@"
using System;
unsafe class C
{{
    public void M(delegate*<void> p)
    {{
        {type} num = ({type})p;
        delegate*<void> ptr = (delegate*<void>)num;
    }}
}}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL(@"C.M", expectedIL: $@"
{{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (delegate*<void> V_0) //ptr
  IL_0000:  ldarg.1
  IL_0001:  call       ""System.{type} System.{type}.op_Explicit(void*)""
  IL_0006:  call       ""void* System.{type}.op_Explicit(System.{type})""
  IL_000b:  stloc.0
  IL_000c:  ret
}}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var conversions = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().ToList();
            Assert.Equal(2, conversions.Count);

            var typeInfoOuter = model.GetTypeInfo(conversions[0]);
            var conversion = model.ClassifyConversion(conversions[0].Expression, typeInfoOuter.Type!);
            Assert.Equal(ConversionKind.IntPtr, conversion.Kind);

            typeInfoOuter = model.GetTypeInfo(conversions[1]);
            conversion = model.ClassifyConversion(conversions[1].Expression, typeInfoOuter.Type!);
            Assert.Equal(ConversionKind.IntPtr, conversion.Kind);
        }

        [Theory]
        [InlineData("sbyte", "conv.i", "conv.ovf.u")]
        [InlineData("byte", "conv.u", "conv.u")]
        [InlineData("short", "conv.i", "conv.ovf.u")]
        [InlineData("ushort", "conv.u", "conv.u")]
        [InlineData("int", "conv.i", "conv.ovf.u")]
        [InlineData("uint", "conv.u", "conv.u")]
        [InlineData("long", "conv.u", "conv.ovf.u")]
        [InlineData("ulong", "conv.u", "conv.ovf.u.un")]
        public void NumericToFunctionPointerConversions(string type, string convKind, string checkedKind)
        {
            var comp = CreateCompilationWithFunctionPointers($@"
unsafe class C
{{
    public void M({type} num, {type}? numNullable)
    {{
        delegate*<void> ptr = (delegate*<void>)num;
        ptr = checked((delegate*<void>)num);
    }}
}}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("C.M", $@"
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (delegate*<void> V_0) //ptr
  IL_0000:  ldarg.1
  IL_0001:  {convKind}
  IL_0002:  stloc.0
  IL_0003:  ldarg.1
  IL_0004:  {checkedKind}
  IL_0005:  stloc.0
  IL_0006:  ret
}}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var conversions = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().ToList();
            Assert.Equal(2, conversions.Count);

            var typeInfoOuter = model.GetTypeInfo(conversions[0]);
            var conversion = model.ClassifyConversion(conversions[0].Expression, typeInfoOuter.Type!);
            Assert.Equal(ConversionKind.ExplicitIntegerToPointer, conversion.Kind);
            Assert.False(conversion.IsImplicit);
            Assert.True(conversion.IsPointer);
        }

        [Fact]
        public void PointerToPointerConversion()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe struct S
{
    void M(delegate*<void> f, S* s, int* i, void* v)
    {
        f = (delegate*<void>)i;
        f = (delegate*<void>)s;
        f = (delegate*<void>)v;
        i = (int*)f;
        s = (S*)f;
    }
}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("S.M", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.3
  IL_0001:  starg.s    V_1
  IL_0003:  ldarg.2
  IL_0004:  starg.s    V_1
  IL_0006:  ldarg.s    V_4
  IL_0008:  starg.s    V_1
  IL_000a:  ldarg.1
  IL_000b:  starg.s    V_3
  IL_000d:  ldarg.1
  IL_000e:  starg.s    V_2
  IL_0010:  ret
}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var conversions = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().ToList();
            Assert.Equal(5, conversions.Count);

            foreach (var conv in conversions)
            {
                var typeInfoOuter = model.GetTypeInfo(conv);
                var conversion = model.ClassifyConversion(conv.Expression, typeInfoOuter.Type!);
                Assert.Equal(ConversionKind.ExplicitPointerToPointer, conversion.Kind);
                Assert.False(conversion.IsImplicit);
                Assert.True(conversion.IsPointer);
            }
        }

        [Fact]
        public void InvalidConversion()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class A
{
    public static implicit operator delegate*<void>(A a) => throw null;
}

unsafe class C : A
{
    public static implicit operator delegate*<void>(A a) => throw null;
    void M(int i, C c, S s)
    {
        delegate*<void> ptr = i; // Missing explicit cast
        i = ptr;
        ptr = c; // Ambiguous user-defined conversion
        ptr = s; // Conversion does not exist
        s = ptr;
        ptr = undefined; // No type
        undefined = ptr;
        
    }
}
struct S {}");

            comp.VerifyDiagnostics(
                // (9,37): error CS0556: User-defined conversion must convert to or from the enclosing type
                //     public static implicit operator delegate*<void>(A a) => throw null;
                Diagnostic(ErrorCode.ERR_ConversionNotInvolvingContainedType, "delegate*<void>").WithLocation(9, 37),
                // (12,31): error CS0266: Cannot implicitly convert type 'int' to 'delegate*<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<void> ptr = i; // Missing explicit cast
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i").WithArguments("int", "delegate*<void>").WithLocation(12, 31),
                // (13,13): error CS0266: Cannot implicitly convert type 'delegate*<void>' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         i = ptr;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "ptr").WithArguments("delegate*<void>", "int").WithLocation(13, 13),
                // (14,15): error CS0457: Ambiguous user defined conversions 'C.implicit operator delegate*<void>(A)' and 'A.implicit operator delegate*<void>(A)' when converting from 'C' to 'delegate*<void>'
                //         ptr = c; // Ambiguous user-defined conversion
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "c").WithArguments("C.implicit operator delegate*<void>(A)", "A.implicit operator delegate*<void>(A)", "C", "delegate*<void>").WithLocation(14, 15),
                // (15,15): error CS0029: Cannot implicitly convert type 'S' to 'delegate*<void>'
                //         ptr = s; // Conversion does not exist
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("S", "delegate*<void>").WithLocation(15, 15),
                // (16,13): error CS0029: Cannot implicitly convert type 'delegate*<void>' to 'S'
                //         s = ptr;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "ptr").WithArguments("delegate*<void>", "S").WithLocation(16, 13),
                // (17,15): error CS0103: The name 'undefined' does not exist in the current context
                //         ptr = undefined; // No type
                Diagnostic(ErrorCode.ERR_NameNotInContext, "undefined").WithArguments("undefined").WithLocation(17, 15),
                // (18,9): error CS0103: The name 'undefined' does not exist in the current context
                //         undefined = ptr;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "undefined").WithArguments("undefined").WithLocation(18, 9)
            );
        }

        [Fact]
        public void UserDefinedConversion_01()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public static implicit operator delegate*<void>(C c) => throw null;
    public static implicit operator C(delegate*<void> ptr) => throw null;
    public void M()
    {
        delegate*<void> ptr = this;
        C c = ptr;
    }
}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""delegate*<void> C.op_Implicit(C)""
  IL_0006:  call       ""C C.op_Implicit(delegate*<void>)""
  IL_000b:  pop
  IL_000c:  ret
}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(d => d.Initializer!.Value).ToList();
            Assert.Equal(2, decls.Count);

            foreach (var decl in decls)
            {
                var conversion = model.GetConversion(decl);
                Assert.Equal(ConversionKind.ImplicitUserDefined, conversion.Kind);

                var typeInfo = model.GetTypeInfo(decl);
                var classifiedConversion = comp.ClassifyConversion(typeInfo.Type!, typeInfo.ConvertedType!);
                Assert.Equal(conversion.Kind, classifiedConversion.Kind);
                Assert.Equal(conversion.Method, classifiedConversion.Method);
            }
        }

        [Fact, WorkItem(57994, "https://github.com/dotnet/roslyn/issues/57994")]
        public void UserDefinedConversion_02()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe readonly struct S
{
    public static implicit operator delegate*<void*, void>(S _) => null;
    void M()
    {
        // S -> delegate*<void*, void> -> delegate*<int*, void>
        /*<bind>*/_ = (delegate*<int*, void>)new S()/*</bind>*/;
    }
}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("S.M", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloc.0
  IL_0009:  call       ""delegate*<void*, void> S.op_Implicit(S)""
  IL_000e:  pop
  IL_000f:  ret
}
");

            VerifyOperationTreeForTest<AssignmentExpressionSyntax>(comp, @"
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: delegate*<System.Int32*, System.Void>) (Syntax: '_ = (delega ... id>)new S()')
  Left:
    IDiscardOperation (Symbol: delegate*<System.Int32*, System.Void> _) (OperationKind.Discard, Type: delegate*<System.Int32*, System.Void>) (Syntax: '_')
  Right:
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Int32*, System.Void>, IsImplicit) (Syntax: '(delegate*< ... id>)new S()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: delegate*<System.Void*, System.Void> S.op_Implicit(S _)) (OperationKind.Conversion, Type: delegate*<System.Void*, System.Void>, IsImplicit) (Syntax: '(delegate*< ... id>)new S()')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: delegate*<System.Void*, System.Void> S.op_Implicit(S _))
          Operand:
            IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S()')
              Arguments(0)
              Initializer:
                null
");
        }

        [Fact, WorkItem(57994, "https://github.com/dotnet/roslyn/issues/57994")]
        public void UserDefinedConversion_03()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe readonly struct S
{
    public static explicit operator delegate*<void*, void>(S _) => null;
    void M()
    {
        // S -> delegate*<void*, void> -> delegate*<int*, void>
        /*<bind>*/_ = (delegate*<int*, void>)new S()/*</bind>*/;
    }
}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("S.M", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloc.0
  IL_0009:  call       ""delegate*<void*, void> S.op_Explicit(S)""
  IL_000e:  pop
  IL_000f:  ret
}
");

            VerifyOperationTreeForTest<AssignmentExpressionSyntax>(comp, @"
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: delegate*<System.Int32*, System.Void>) (Syntax: '_ = (delega ... id>)new S()')
  Left:
    IDiscardOperation (Symbol: delegate*<System.Int32*, System.Void> _) (OperationKind.Discard, Type: delegate*<System.Int32*, System.Void>) (Syntax: '_')
  Right:
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Int32*, System.Void>, IsImplicit) (Syntax: '(delegate*< ... id>)new S()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: delegate*<System.Void*, System.Void> S.op_Explicit(S _)) (OperationKind.Conversion, Type: delegate*<System.Void*, System.Void>, IsImplicit) (Syntax: '(delegate*< ... id>)new S()')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: delegate*<System.Void*, System.Void> S.op_Explicit(S _))
          Operand:
            IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S()')
              Arguments(0)
              Initializer:
                null
");
        }

        [Fact, WorkItem(57994, "https://github.com/dotnet/roslyn/issues/57994")]
        public void UserDefinedConversion_04()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe readonly struct S
{
    public static implicit operator delegate*<void*, void>(S _) => null;
    void M()
    {
        // S -> delegate*<void*, void> -> delegate*<int*, void>
        /*<bind>*/delegate*<int*, void> _ = new S()/*</bind>*/;
    }
}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("S.M", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (delegate*<int*, void> V_0, //_
                S V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""S""
  IL_0008:  ldloc.1
  IL_0009:  call       ""delegate*<void*, void> S.op_Implicit(S)""
  IL_000e:  stloc.0
  IL_000f:  ret
}
");

            VerifyOperationTreeForTest<VariableDeclarationSyntax>(comp, @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'delegate*<i ... _ = new S()')
  Declarators:
      IVariableDeclaratorOperation (Symbol: delegate*<System.Int32*, System.Void> _) (OperationKind.VariableDeclarator, Type: null) (Syntax: '_ = new S()')
        Initializer:
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new S()')
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Int32*, System.Void>, IsImplicit) (Syntax: 'new S()')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand:
                IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: delegate*<System.Void*, System.Void> S.op_Implicit(S _)) (OperationKind.Conversion, Type: delegate*<System.Void*, System.Void>, IsImplicit) (Syntax: 'new S()')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: delegate*<System.Void*, System.Void> S.op_Implicit(S _))
                  Operand:
                    IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S()')
                      Arguments(0)
                      Initializer:
                        null
  Initializer:
    null
");
        }

        [Fact, WorkItem(57994, "https://github.com/dotnet/roslyn/issues/57994")]
        public void UserDefinedConversion_05()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe readonly struct S
{
    public static explicit operator delegate*<void*, void>(S _) => null;
    void M()
    {
        // S -> delegate*<void*, void> -> delegate*<int*, void>
        /*<bind>*/delegate*<int*, void> _ = new S()/*</bind>*/;
    }
}");

            comp.VerifyDiagnostics(
                // (8,45): error CS0266: Cannot implicitly convert type 'S' to 'delegate*<int*, void>'. An explicit conversion exists (are you missing a cast?)
                //         /*<bind>*/delegate*<int*, void> _ = new S()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new S()").WithArguments("S", "delegate*<int*, void>").WithLocation(8, 45)
            );

            VerifyOperationTreeForTest<VariableDeclarationSyntax>(comp, @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'delegate*<i ... _ = new S()')
  Declarators:
      IVariableDeclaratorOperation (Symbol: delegate*<System.Int32*, System.Void> _) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '_ = new S()')
        Initializer:
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new S()')
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Int32*, System.Void>, IsInvalid, IsImplicit) (Syntax: 'new S()')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand:
                IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: delegate*<System.Void*, System.Void> S.op_Explicit(S _)) (OperationKind.Conversion, Type: delegate*<System.Void*, System.Void>, IsInvalid, IsImplicit) (Syntax: 'new S()')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: delegate*<System.Void*, System.Void> S.op_Explicit(S _))
                  Operand:
                    IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S, IsInvalid) (Syntax: 'new S()')
                      Arguments(0)
                      Initializer:
                        null
  Initializer:
    null
");
        }

        [Fact]
        public void FunctionPointerToFunctionPointerValid_ReferenceVarianceAndIdentity()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<object, ref int, string> param1, delegate*<object, ref int> param2, delegate*<object, void> param3, delegate*<object, out int, string> param4)
    {
        delegate*<string, ref int, object> ptr1 = param1;
        delegate*<string, ref int> ptr2 = param2;
        delegate*<string, void> ptr3 = param3;
        delegate*<string, out int, object> ptr4 = param4;
    }
}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (delegate*<string, ref int, object> V_0, //ptr1
                delegate*<string, ref int> V_1, //ptr2
                delegate*<string, void> V_2, //ptr3
                delegate*<string, out int, object> V_3) //ptr4
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldarg.2
  IL_0003:  stloc.1
  IL_0004:  ldarg.3
  IL_0005:  stloc.2
  IL_0006:  ldarg.s    V_4
  IL_0008:  stloc.3
  IL_0009:  ret
}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(d => d.Initializer!.Value).ToList();
            Assert.Equal(4, decls.Count);

            foreach (var decl in decls)
            {
                var conversion = model.GetConversion(decl);
                Assert.Equal(ConversionKind.ImplicitPointer, conversion.Kind);
                Assert.True(conversion.IsImplicit);
                Assert.True(conversion.IsPointer);

                var typeInfo = model.GetTypeInfo(decl);
                var classifiedConversion = comp.ClassifyConversion(typeInfo.Type!, typeInfo.ConvertedType!);
                Assert.Equal(conversion, classifiedConversion);
            }
        }

        [Fact]
        public void FunctionPointerToFunctionPointerValid_NestedFunctionPointerVariantConversions()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<delegate*<string, void>, delegate*<string>> param1)
    {
        delegate*<delegate*<object, void>, delegate*<object>> ptr1 = param1;
    }
}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (delegate*<delegate*<object, void>, delegate*<object>> V_0) //ptr1
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ret
}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var variableDeclaratorSyntax = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();

            VerifyDeclarationConversion(comp, model, variableDeclaratorSyntax,
                expectedConversionKind: ConversionKind.ImplicitPointer, expectedImplicit: true,
                expectedOriginalType: "delegate*<delegate*<System.String, System.Void>, delegate*<System.String>>",
                expectedConvertedType: "delegate*<delegate*<System.Object, System.Void>, delegate*<System.Object>>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<delegate*<System.Object, System.Void>, delegate*<System.Object>> ptr1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'ptr1 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<delegate*<System.Object, System.Void>, delegate*<System.Object>>, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<delegate*<System.String, System.Void>, delegate*<System.String>>) (Syntax: 'param1')
");
        }

        private static void VerifyDeclarationConversion(CSharpCompilation comp, SemanticModel model, VariableDeclaratorSyntax decl, ConversionKind expectedConversionKind, bool expectedImplicit, string expectedOriginalType, string expectedConvertedType, string expectedOperationTree)
        {
            var initializer = decl.Initializer!.Value;
            var conversion = model.GetConversion(initializer);
            Assert.Equal(expectedImplicit, conversion.IsImplicit);
            Assert.Equal(expectedConversionKind, conversion.Kind);

            var typeInfo = model.GetTypeInfo(initializer);
            AssertEx.Equal(expectedOriginalType, typeInfo.Type!.ToTestDisplayString());
            AssertEx.Equal(expectedConvertedType, typeInfo.ConvertedType!.ToTestDisplayString());
            var classifiedConversion = comp.ClassifyConversion(typeInfo.Type!, typeInfo.ConvertedType!);
            Assert.Equal(conversion, classifiedConversion);

            VerifyOperationTreeForNode(comp, model, decl, expectedOperationTree);
        }

        [Fact]
        public void FunctionPointerToFunctionPointerValid_PointerVariance()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<void*, int*> param1)
    {
        delegate*<delegate*<object, void>, void*> ptr1 = param1;
    }
}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (delegate*<delegate*<object, void>, void*> V_0) //ptr1
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ret
}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var variableDeclaratorSyntax = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();

            VerifyDeclarationConversion(comp, model, variableDeclaratorSyntax,
                expectedConversionKind: ConversionKind.ImplicitPointer, expectedImplicit: true,
                expectedOriginalType: "delegate*<System.Void*, System.Int32*>",
                expectedConvertedType: "delegate*<delegate*<System.Object, System.Void>, System.Void*>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<delegate*<System.Object, System.Void>, System.Void*> ptr1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'ptr1 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<delegate*<System.Object, System.Void>, System.Void*>, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<System.Void*, System.Int32*>) (Syntax: 'param1')
");
        }

        [Fact]
        public void FunctionPointerToFunctionPointerValid_UnmanagedConventionOrder()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate* unmanaged[Stdcall, Thiscall]<void> param1)
    {
        delegate* unmanaged[Stdcall, Thiscall]<void> ptr1 = param1;
        delegate* unmanaged[Thiscall, Stdcall]<void> ptr2 = param1;
        delegate* unmanaged[Stdcall, Stdcall, Thiscall]<void> ptr3 = param1;
    }
}", targetFramework: TargetFramework.NetCoreApp);

            CompileAndVerify(comp, verify: Verification.Skipped);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var variableDeclaratorSyntaxes = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            Assert.Equal(3, variableDeclaratorSyntaxes.Length);

            VerifyDeclarationConversion(comp, model, variableDeclaratorSyntaxes[0],
                expectedConversionKind: ConversionKind.Identity, expectedImplicit: true,
                expectedOriginalType: "delegate* unmanaged[Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)>",
                expectedConvertedType: "delegate* unmanaged[Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate* unmanaged[Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)> ptr1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'ptr1 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= param1')
      IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate* unmanaged[Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)>) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, variableDeclaratorSyntaxes[1],
                expectedConversionKind: ConversionKind.Identity, expectedImplicit: true,
                expectedOriginalType: "delegate* unmanaged[Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)>",
                expectedConvertedType: "delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)> ptr2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'ptr2 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate* unmanaged[Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)>) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, variableDeclaratorSyntaxes[2],
                expectedConversionKind: ConversionKind.Identity, expectedImplicit: true,
                expectedOriginalType: "delegate* unmanaged[Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)>",
                expectedConvertedType: "delegate* unmanaged[Stdcall, Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate* unmanaged[Stdcall, Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)> ptr3) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'ptr3 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate* unmanaged[Stdcall, Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)>, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate* unmanaged[Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)>) (Syntax: 'param1')
");
        }

        [Fact]
        public void FunctionPointerToFunctionPointerInvalid_DifferentParameterCounts()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<string, string, void> param1)
    {
        delegate*<string, void> ptr1 = param1;
        delegate*<string, string, string, void> ptr2 = param1;
    }
}");

            comp.VerifyDiagnostics(
                // (6,40): error CS0266: Cannot implicitly convert type 'delegate*<string, string, void>' to 'delegate*<string, void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<string, void> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<string, string, void>", "delegate*<string, void>").WithLocation(6, 40),
                // (7,56): error CS0266: Cannot implicitly convert type 'delegate*<string, string, void>' to 'delegate*<string, string, string, void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<string, string, string, void> ptr2 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<string, string, void>", "delegate*<string, string, string, void>").WithLocation(7, 56)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            Assert.Equal(2, decls.Length);

            VerifyDeclarationConversion(comp, model, decls[0],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<System.String, System.String, System.Void>",
                expectedConvertedType: "delegate*<System.String, System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.String, System.Void> ptr1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr1 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.String, System.Void>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<System.String, System.String, System.Void>, IsInvalid) (Syntax: 'param1')
");
            VerifyDeclarationConversion(comp, model, decls[1],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<System.String, System.String, System.Void>",
                expectedConvertedType: "delegate*<System.String, System.String, System.String, System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.String, System.String, System.String, System.Void> ptr2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr2 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.String, System.String, System.String, System.Void>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<System.String, System.String, System.Void>, IsInvalid) (Syntax: 'param1')
");
        }

        [Fact]
        public void FunctionPointerToFunctionPointerInvalid_ParameterVariance()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<ref object, void> param1, delegate*<out object, void> param2)
    {
        delegate*<in object, void> ptr1 = param1;
        delegate*<object, void> ptr2 = param1;
        delegate*<ref string, void> ptr3 = param1;
        delegate*<out object, void> ptr4 = param1;
        delegate*<in object, void> ptr5 = param2;
        delegate*<object, void> ptr6 = param2;
        delegate*<ref object, void> ptr7 = param2;
        delegate*<out string, void> ptr8 = param2;
    }
}");

            comp.VerifyDiagnostics(
                // (6,43): error CS0266: Cannot implicitly convert type 'delegate*<ref object, void>' to 'delegate*<in object, void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<in object, void> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<ref object, void>", "delegate*<in object, void>").WithLocation(6, 43),
                // (7,40): error CS0266: Cannot implicitly convert type 'delegate*<ref object, void>' to 'delegate*<object, void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<object, void> ptr2 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<ref object, void>", "delegate*<object, void>").WithLocation(7, 40),
                // (8,44): error CS0266: Cannot implicitly convert type 'delegate*<ref object, void>' to 'delegate*<ref string, void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<ref string, void> ptr3 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<ref object, void>", "delegate*<ref string, void>").WithLocation(8, 44),
                // (9,44): error CS0266: Cannot implicitly convert type 'delegate*<ref object, void>' to 'delegate*<out object, void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<out object, void> ptr4 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<ref object, void>", "delegate*<out object, void>").WithLocation(9, 44),
                // (10,43): error CS0266: Cannot implicitly convert type 'delegate*<out object, void>' to 'delegate*<in object, void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<in object, void> ptr5 = param2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param2").WithArguments("delegate*<out object, void>", "delegate*<in object, void>").WithLocation(10, 43),
                // (11,40): error CS0266: Cannot implicitly convert type 'delegate*<out object, void>' to 'delegate*<object, void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<object, void> ptr6 = param2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param2").WithArguments("delegate*<out object, void>", "delegate*<object, void>").WithLocation(11, 40),
                // (12,44): error CS0266: Cannot implicitly convert type 'delegate*<out object, void>' to 'delegate*<ref object, void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<ref object, void> ptr7 = param2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param2").WithArguments("delegate*<out object, void>", "delegate*<ref object, void>").WithLocation(12, 44),
                // (13,44): error CS0266: Cannot implicitly convert type 'delegate*<out object, void>' to 'delegate*<out string, void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<out string, void> ptr8 = param2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param2").WithArguments("delegate*<out object, void>", "delegate*<out string, void>").WithLocation(13, 44)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            Assert.Equal(8, decls.Length);

            VerifyDeclarationConversion(comp, model, decls[0],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<ref System.Object, System.Void>",
                expectedConvertedType: "delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.Object, System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.Object, System.Void> ptr1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr1 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.Object, System.Void>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<ref System.Object, System.Void>, IsInvalid) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, decls[1],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<ref System.Object, System.Void>",
                expectedConvertedType: "delegate*<System.Object, System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.Object, System.Void> ptr2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr2 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Object, System.Void>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<ref System.Object, System.Void>, IsInvalid) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, decls[2],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<ref System.Object, System.Void>",
                expectedConvertedType: "delegate*<ref System.String, System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<ref System.String, System.Void> ptr3) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr3 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<ref System.String, System.Void>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<ref System.Object, System.Void>, IsInvalid) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, decls[3],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<ref System.Object, System.Void>",
                expectedConvertedType: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.Object, System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.Object, System.Void> ptr4) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr4 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.Object, System.Void>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<ref System.Object, System.Void>, IsInvalid) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, decls[4],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.Object, System.Void>",
                expectedConvertedType: "delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.Object, System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.Object, System.Void> ptr5) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr5 = param2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.Object, System.Void>, IsInvalid, IsImplicit) (Syntax: 'param2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param2 (OperationKind.ParameterReference, Type: delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.Object, System.Void>, IsInvalid) (Syntax: 'param2')
");

            VerifyDeclarationConversion(comp, model, decls[5],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.Object, System.Void>",
                expectedConvertedType: "delegate*<System.Object, System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.Object, System.Void> ptr6) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr6 = param2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Object, System.Void>, IsInvalid, IsImplicit) (Syntax: 'param2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param2 (OperationKind.ParameterReference, Type: delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.Object, System.Void>, IsInvalid) (Syntax: 'param2')
");

            VerifyDeclarationConversion(comp, model, decls[6],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.Object, System.Void>",
                expectedConvertedType: "delegate*<ref System.Object, System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<ref System.Object, System.Void> ptr7) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr7 = param2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<ref System.Object, System.Void>, IsInvalid, IsImplicit) (Syntax: 'param2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param2 (OperationKind.ParameterReference, Type: delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.Object, System.Void>, IsInvalid) (Syntax: 'param2')
");

            VerifyDeclarationConversion(comp, model, decls[7],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.Object, System.Void>",
                expectedConvertedType: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.String, System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.String, System.Void> ptr8) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr8 = param2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.String, System.Void>, IsInvalid, IsImplicit) (Syntax: 'param2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param2 (OperationKind.ParameterReference, Type: delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.Object, System.Void>, IsInvalid) (Syntax: 'param2')
");
        }

        [Fact]
        public void FunctionPointerToFunctionPointerInvalid_ReturnTypeVariance()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<ref string> param1)
    {
        delegate*<ref readonly string> ptr1 = param1;
        delegate*<string> ptr2 = param1;
        delegate*<object> ptr3 = param1;
    }
}");

            comp.VerifyDiagnostics(
                // (6,47): error CS0266: Cannot implicitly convert type 'delegate*<ref string>' to 'delegate*<ref readonly string>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<ref readonly string> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<ref string>", "delegate*<ref readonly string>").WithLocation(6, 47),
                // (7,34): error CS0266: Cannot implicitly convert type 'delegate*<ref string>' to 'delegate*<string>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<string> ptr2 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<ref string>", "delegate*<string>").WithLocation(7, 34),
                // (8,34): error CS0266: Cannot implicitly convert type 'delegate*<ref string>' to 'delegate*<object>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<object> ptr3 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<ref string>", "delegate*<object>").WithLocation(8, 34)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            Assert.Equal(3, decls.Length);

            VerifyDeclarationConversion(comp, model, decls[0],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<ref System.String>",
                expectedConvertedType: "delegate*<ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.String>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.String> ptr1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr1 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.String>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<ref System.String>, IsInvalid) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, decls[1],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<ref System.String>",
                expectedConvertedType: "delegate*<System.String>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.String> ptr2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr2 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.String>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<ref System.String>, IsInvalid) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, decls[2],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<ref System.String>",
                expectedConvertedType: "delegate*<System.Object>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.Object> ptr3) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr3 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Object>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<ref System.String>, IsInvalid) (Syntax: 'param1')
");
        }

        [Fact]
        public void FunctionPointerToFunctionPointerInvalid_CallingConvention()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<void> param1, delegate* unmanaged[Thiscall, Stdcall]<void> param2)
    {
        delegate* unmanaged[Cdecl]<void> ptr1 = param1;
        delegate* unmanaged[Thiscall]<void> ptr2 = param1;
        delegate* unmanaged[Stdcall]<void> ptr3 = param1;
        delegate* unmanaged[Thiscall]<void> ptr4 = param2;
        delegate* unmanaged[Stdcall]<void> ptr5 = param2;
        delegate* unmanaged[Thiscall, Cdecl]<void> ptr6 = param2;
        delegate* unmanaged[Cdecl, Stdcall]<void> ptr7 = param2;
        delegate* unmanaged[Thiscall, Stdcall, Cdecl]<void> ptr8 = param2;
    }
}", targetFramework: TargetFramework.NetCoreApp);

            comp.VerifyDiagnostics(
                // (6,49): error CS0266: Cannot implicitly convert type 'delegate*<void>' to 'delegate* unmanaged[Cdecl]<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate* unmanaged[Cdecl]<void> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<void>", "delegate* unmanaged[Cdecl]<void>").WithLocation(6, 49),
                // (7,52): error CS0266: Cannot implicitly convert type 'delegate*<void>' to 'delegate* unmanaged[Thiscall]<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate* unmanaged[Thiscall]<void> ptr2 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<void>", "delegate* unmanaged[Thiscall]<void>").WithLocation(7, 52),
                // (8,51): error CS0266: Cannot implicitly convert type 'delegate*<void>' to 'delegate* unmanaged[Stdcall]<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate* unmanaged[Stdcall]<void> ptr3 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<void>", "delegate* unmanaged[Stdcall]<void>").WithLocation(8, 51),
                // (9,52): error CS0266: Cannot implicitly convert type 'delegate* unmanaged[Thiscall, Stdcall]<void>' to 'delegate* unmanaged[Thiscall]<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate* unmanaged[Thiscall]<void> ptr4 = param2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param2").WithArguments("delegate* unmanaged[Thiscall, Stdcall]<void>", "delegate* unmanaged[Thiscall]<void>").WithLocation(9, 52),
                // (10,51): error CS0266: Cannot implicitly convert type 'delegate* unmanaged[Thiscall, Stdcall]<void>' to 'delegate* unmanaged[Stdcall]<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate* unmanaged[Stdcall]<void> ptr5 = param2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param2").WithArguments("delegate* unmanaged[Thiscall, Stdcall]<void>", "delegate* unmanaged[Stdcall]<void>").WithLocation(10, 51),
                // (11,59): error CS0266: Cannot implicitly convert type 'delegate* unmanaged[Thiscall, Stdcall]<void>' to 'delegate* unmanaged[Thiscall, Cdecl]<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate* unmanaged[Thiscall, Cdecl]<void> ptr6 = param2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param2").WithArguments("delegate* unmanaged[Thiscall, Stdcall]<void>", "delegate* unmanaged[Thiscall, Cdecl]<void>").WithLocation(11, 59),
                // (12,58): error CS0266: Cannot implicitly convert type 'delegate* unmanaged[Thiscall, Stdcall]<void>' to 'delegate* unmanaged[Cdecl, Stdcall]<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate* unmanaged[Cdecl, Stdcall]<void> ptr7 = param2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param2").WithArguments("delegate* unmanaged[Thiscall, Stdcall]<void>", "delegate* unmanaged[Cdecl, Stdcall]<void>").WithLocation(12, 58),
                // (13,68): error CS0266: Cannot implicitly convert type 'delegate* unmanaged[Thiscall, Stdcall]<void>' to 'delegate* unmanaged[Thiscall, Stdcall, Cdecl]<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate* unmanaged[Thiscall, Stdcall, Cdecl]<void> ptr8 = param2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param2").WithArguments("delegate* unmanaged[Thiscall, Stdcall]<void>", "delegate* unmanaged[Thiscall, Stdcall, Cdecl]<void>").WithLocation(13, 68)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            Assert.Equal(8, decls.Length);

            VerifyDeclarationConversion(comp, model, decls[0],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<System.Void>",
                expectedConvertedType: "delegate* unmanaged[Cdecl]<System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate* unmanaged[Cdecl]<System.Void> ptr1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr1 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate* unmanaged[Cdecl]<System.Void>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, decls[1],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<System.Void>",
                expectedConvertedType: "delegate* unmanaged[Thiscall]<System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate* unmanaged[Thiscall]<System.Void> ptr2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr2 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate* unmanaged[Thiscall]<System.Void>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, decls[2],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<System.Void>",
                expectedConvertedType: "delegate* unmanaged[Stdcall]<System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate* unmanaged[Stdcall]<System.Void> ptr3) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr3 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate* unmanaged[Stdcall]<System.Void>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, decls[3],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>",
                expectedConvertedType: "delegate* unmanaged[Thiscall]<System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate* unmanaged[Thiscall]<System.Void> ptr4) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr4 = param2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate* unmanaged[Thiscall]<System.Void>, IsInvalid, IsImplicit) (Syntax: 'param2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param2 (OperationKind.ParameterReference, Type: delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>, IsInvalid) (Syntax: 'param2')
");

            VerifyDeclarationConversion(comp, model, decls[4],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>",
                expectedConvertedType: "delegate* unmanaged[Stdcall]<System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate* unmanaged[Stdcall]<System.Void> ptr5) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr5 = param2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate* unmanaged[Stdcall]<System.Void>, IsInvalid, IsImplicit) (Syntax: 'param2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param2 (OperationKind.ParameterReference, Type: delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>, IsInvalid) (Syntax: 'param2')
");

            VerifyDeclarationConversion(comp, model, decls[5],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>",
                expectedConvertedType: "delegate* unmanaged[Thiscall, Cdecl]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvCdecl)>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate* unmanaged[Thiscall, Cdecl]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvCdecl)> ptr6) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr6 = param2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate* unmanaged[Thiscall, Cdecl]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvCdecl)>, IsInvalid, IsImplicit) (Syntax: 'param2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param2 (OperationKind.ParameterReference, Type: delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>, IsInvalid) (Syntax: 'param2')
");

            VerifyDeclarationConversion(comp, model, decls[6],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>",
                expectedConvertedType: "delegate* unmanaged[Cdecl, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl) modopt(System.Runtime.CompilerServices.CallConvStdcall)>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate* unmanaged[Cdecl, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl) modopt(System.Runtime.CompilerServices.CallConvStdcall)> ptr7) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr7 = param2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate* unmanaged[Cdecl, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl) modopt(System.Runtime.CompilerServices.CallConvStdcall)>, IsInvalid, IsImplicit) (Syntax: 'param2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param2 (OperationKind.ParameterReference, Type: delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>, IsInvalid) (Syntax: 'param2')
");

            VerifyDeclarationConversion(comp, model, decls[7],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>",
                expectedConvertedType: "delegate* unmanaged[Thiscall, Stdcall, Cdecl]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvCdecl)>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate* unmanaged[Thiscall, Stdcall, Cdecl]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvCdecl)> ptr8) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr8 = param2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate* unmanaged[Thiscall, Stdcall, Cdecl]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvCdecl)>, IsInvalid, IsImplicit) (Syntax: 'param2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param2 (OperationKind.ParameterReference, Type: delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>, IsInvalid) (Syntax: 'param2')
");
        }

        [Fact]
        public void FunctionPointerToFunctionPointerInvalid_IncompatibleTypes()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<int, object> param1)
    {
        delegate*<object, string> ptr1 = param1;
        delegate*<int, string> ptr2 = param1;
    }
}");

            comp.VerifyDiagnostics(
                // (6,42): error CS0266: Cannot implicitly convert type 'delegate*<int, object>' to 'delegate*<object, string>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<object, string> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<int, object>", "delegate*<object, string>").WithLocation(6, 42),
                // (7,39): error CS0266: Cannot implicitly convert type 'delegate*<int, object>' to 'delegate*<int, string>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<int, string> ptr2 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<int, object>", "delegate*<int, string>").WithLocation(7, 39)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            Assert.Equal(2, decls.Length);

            VerifyDeclarationConversion(comp, model, decls[0],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<System.Int32, System.Object>",
                expectedConvertedType: "delegate*<System.Object, System.String>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.Object, System.String> ptr1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr1 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Object, System.String>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<System.Int32, System.Object>, IsInvalid) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, decls[1],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<System.Int32, System.Object>",
                expectedConvertedType: "delegate*<System.Int32, System.String>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.Int32, System.String> ptr2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr2 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Int32, System.String>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<System.Int32, System.Object>, IsInvalid) (Syntax: 'param1')
");
        }

        [Fact]
        public void FunctionPointerToFunctionPointerInvalid_BadNestedVariance()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<delegate*<object, void>, void> param1, delegate*<delegate*<object>> param2)
    {
        delegate*<delegate*<string, void>, void> ptr1 = param1;
        delegate*<delegate*<string>> ptr2 = param2;
    }
}");

            comp.VerifyDiagnostics(
                // (6,57): error CS0266: Cannot implicitly convert type 'delegate*<delegate*<object, void>, void>' to 'delegate*<delegate*<string, void>, void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<delegate*<string, void>, void> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<delegate*<object, void>, void>", "delegate*<delegate*<string, void>, void>").WithLocation(6, 57),
                // (7,45): error CS0266: Cannot implicitly convert type 'delegate*<delegate*<object>>' to 'delegate*<delegate*<string>>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<delegate*<string>> ptr2 = param2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param2").WithArguments("delegate*<delegate*<object>>", "delegate*<delegate*<string>>").WithLocation(7, 45)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            Assert.Equal(2, decls.Length);

            VerifyDeclarationConversion(comp, model, decls[0],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<delegate*<System.Object, System.Void>, System.Void>",
                expectedConvertedType: "delegate*<delegate*<System.String, System.Void>, System.Void>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<delegate*<System.String, System.Void>, System.Void> ptr1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr1 = param1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<delegate*<System.String, System.Void>, System.Void>, IsInvalid, IsImplicit) (Syntax: 'param1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param1 (OperationKind.ParameterReference, Type: delegate*<delegate*<System.Object, System.Void>, System.Void>, IsInvalid) (Syntax: 'param1')
");

            VerifyDeclarationConversion(comp, model, decls[1],
                expectedConversionKind: ConversionKind.ExplicitPointerToPointer, expectedImplicit: false,
                expectedOriginalType: "delegate*<delegate*<System.Object>>",
                expectedConvertedType: "delegate*<delegate*<System.String>>",
                expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<delegate*<System.String>> ptr2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr2 = param2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= param2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<delegate*<System.String>>, IsInvalid, IsImplicit) (Syntax: 'param2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: param2 (OperationKind.ParameterReference, Type: delegate*<delegate*<System.Object>>, IsInvalid) (Syntax: 'param2')
");
        }

        [Fact]
        public void FunctionPointerParameterTypeInference()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M1<T>(delegate*<T, void> param) {}
    public void M2()
    {
        delegate*<string, void> p = null;
        M1(p);
    }
}");

            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var methodSymbol = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;

            Assert.NotSame(methodSymbol, methodSymbol.OriginalDefinition);
            Assert.Equal(SpecialType.System_String, methodSymbol.TypeArguments[0].SpecialType);
            var functionPointer = (IFunctionPointerTypeSymbol)methodSymbol.Parameters[0].Type;
            Assert.Equal(SpecialType.System_String, functionPointer.Signature.Parameters[0].Type.SpecialType);

            VerifyOperationTreeForNode(comp, model, invocation, expectedOperationTree: @"
IInvocationOperation ( void C.M1<System.String>(delegate*<System.String, System.Void> param)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M1(p)')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M1')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: param) (OperationKind.Argument, Type: null) (Syntax: 'p')
        ILocalReferenceOperation: p (OperationKind.LocalReference, Type: delegate*<System.String, System.Void>) (Syntax: 'p')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");
        }

        [Fact]
        public void FunctionPointerGenericSubstitutionVariantParameterSubstitutions()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M1<T>(delegate*<T, void> param1, delegate*<T, void> param2) {}
    public void M2()
    {
        delegate*<string, void> p1 = null;
        delegate*<object, void> p2 = null;
        M1(p1, p2);

        delegate*<int*, void> p3 = null;
        delegate*<void*, void> p4 = null;
        M1(p3, p4);
    }
}");

            comp.VerifyDiagnostics(
                // (13,9): error CS0411: The type arguments for method 'C.M1<T>(delegate*<T, void>, delegate*<T, void>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(p3, p4);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("C.M1<T>(delegate*<T, void>, delegate*<T, void>)").WithLocation(13, 9)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var m1Invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();

            AssertEx.Equal("void C.M1<System.String>(delegate*<System.String, System.Void> param1, delegate*<System.String, System.Void> param2)",
                           model.GetSymbolInfo(m1Invocation).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void FunctionPointerGenericSubstitutionVariantParameterSubstitutions_UpperBounds_Parameter()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M1<T>(delegate*<delegate*<T, void>, void> param1, delegate*<delegate*<T, void>, void> param2) {}
    public void M2()
    {
        delegate*<delegate*<string, void>, void> p1 = null;
        delegate*<delegate*<object, void>, void> p2 = null;
        M1(p1, p2);

        delegate*<delegate*<int*, void>, void> p3 = null;
        delegate*<delegate*<void*, void>, void> p4 = null;
        M1(p3, p4);
    }
}");

            comp.VerifyDiagnostics(
                // (13,9): error CS0411: The type arguments for method 'C.M1<T>(delegate*<delegate*<T, void>, void>, delegate*<delegate*<T, void>, void>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(p3, p4);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("C.M1<T>(delegate*<delegate*<T, void>, void>, delegate*<delegate*<T, void>, void>)").WithLocation(13, 9)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var m1Invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();

            AssertEx.Equal("void C.M1<System.Object>(delegate*<delegate*<System.Object, System.Void>, System.Void> param1, delegate*<delegate*<System.Object, System.Void>, System.Void> param2)",
                           model.GetSymbolInfo(m1Invocation).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void FunctionPointerGenericSubstitutionVariantParameterSubstitutions_UpperBounds_Return()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M1<T>(delegate*<delegate*<T>, void> param1, delegate*<delegate*<T>, void> param2) {}
    public void M2()
    {
        delegate*<delegate*<string>, void> p1 = null;
        delegate*<delegate*<object>, void> p2 = null;
        M1(p1, p2);

        delegate*<delegate*<int*>, void> p3 = null;
        delegate*<delegate*<void*>, void> p4 = null;
        M1(p3, p4);
    }
}");

            comp.VerifyDiagnostics(
                // (13,9): error CS0411: The type arguments for method 'C.M1<T>(delegate*<delegate*<T>, void>, delegate*<delegate*<T>, void>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(p3, p4);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("C.M1<T>(delegate*<delegate*<T>, void>, delegate*<delegate*<T>, void>)").WithLocation(13, 9)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var m1Invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();

            AssertEx.Equal("void C.M1<System.String>(delegate*<delegate*<System.String>, System.Void> param1, delegate*<delegate*<System.String>, System.Void> param2)",
                           model.GetSymbolInfo(m1Invocation).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void FunctionPointerGenericSubstitutionConflictingParameterSubstitutions()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M1<T>(delegate*<T, void> param1, delegate*<T, void> param2) {}
    public void M2()
    {
        delegate*<string, void> p1 = null;
        delegate*<int, void> p2 = null;
        M1(p1, p2);
    }
}");

            comp.VerifyDiagnostics(
                // (9,9): error CS0411: The type arguments for method 'C.M1<T>(delegate*<T, void>, delegate*<T, void>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(p1, p2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("C.M1<T>(delegate*<T, void>, delegate*<T, void>)").WithLocation(9, 9)
            );
        }

        [Fact]
        public void FunctionPointerReturnTypeInference()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M1<T>(delegate*<T> param) {}
    public void M2()
    {
        delegate*<string> p = null;
        M1(p);
    }
}");

            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var methodSymbol = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;

            Assert.NotSame(methodSymbol, methodSymbol.OriginalDefinition);
            Assert.Equal(SpecialType.System_String, methodSymbol.TypeArguments[0].SpecialType);
            var functionPointer = (IFunctionPointerTypeSymbol)methodSymbol.Parameters[0].Type;
            Assert.Equal(SpecialType.System_String, functionPointer.Signature.ReturnType.SpecialType);

            VerifyOperationTreeForNode(comp, model, invocation, expectedOperationTree: @"
IInvocationOperation ( void C.M1<System.String>(delegate*<System.String> param)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M1(p)')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M1')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: param) (OperationKind.Argument, Type: null) (Syntax: 'p')
        ILocalReferenceOperation: p (OperationKind.LocalReference, Type: delegate*<System.String>) (Syntax: 'p')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");
        }

        [Fact]
        public void FunctionPointerGenericSubstitutionVariantReturnSubstitutions()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M1<T>(delegate*<T> param1, delegate*<T> param2) {}
    public void M2()
    {
        delegate*<string> p1 = null;
        delegate*<object> p2 = null;
        M1(p1, p2);

        delegate*<int*> p3 = null;
        delegate*<void*> p4 = null;
        M1(p3, p4);
    }
}");

            comp.VerifyDiagnostics(
                // (13,9): error CS0411: The type arguments for method 'C.M1<T>(delegate*<T>, delegate*<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(p3, p4);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("C.M1<T>(delegate*<T>, delegate*<T>)").WithLocation(13, 9)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var m1Invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();

            AssertEx.Equal("void C.M1<System.Object>(delegate*<System.Object> param1, delegate*<System.Object> param2)",
                           model.GetSymbolInfo(m1Invocation).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void FunctionPointerGenericSubstitutionVariantReturnSubstitutions_LowerBounds_Parameter()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M1<T>(delegate*<delegate*<T, void>> param1, delegate*<delegate*<T, void>> param2) {}
    public void M2()
    {
        delegate*<delegate*<string, void>> p1 = null;
        delegate*<delegate*<object, void>> p2 = null;
        M1(p1, p2);

        delegate*<delegate*<int*, void>> p3 = null;
        delegate*<delegate*<void*, void>> p4 = null;
        M1(p3, p4);
    }
}");

            comp.VerifyDiagnostics(
                // (13,9): error CS0411: The type arguments for method 'C.M1<T>(delegate*<delegate*<T, void>>, delegate*<delegate*<T, void>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(p3, p4);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("C.M1<T>(delegate*<delegate*<T, void>>, delegate*<delegate*<T, void>>)").WithLocation(13, 9)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var m1Invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();

            AssertEx.Equal("void C.M1<System.String>(delegate*<delegate*<System.String, System.Void>> param1, delegate*<delegate*<System.String, System.Void>> param2)",
                           model.GetSymbolInfo(m1Invocation).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void FunctionPointerGenericSubstitutionVariantReturnSubstitutions_LowerBounds_Return()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M1<T>(delegate*<delegate*<T>> param1, delegate*<delegate*<T>> param2) {}
    public void M2()
    {
        delegate*<delegate*<string>> p1 = null;
        delegate*<delegate*<object>> p2 = null;
        M1(p1, p2);

        delegate*<delegate*<int*>> p3 = null;
        delegate*<delegate*<void*>> p4 = null;
        M1(p3, p4);
    }
}");

            comp.VerifyDiagnostics(
                // (13,9): error CS0411: The type arguments for method 'C.M1<T>(delegate*<delegate*<T>>, delegate*<delegate*<T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(p3, p4);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("C.M1<T>(delegate*<delegate*<T>>, delegate*<delegate*<T>>)").WithLocation(13, 9)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var m1Invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();

            AssertEx.Equal("void C.M1<System.Object>(delegate*<delegate*<System.Object>> param1, delegate*<delegate*<System.Object>> param2)",
                           model.GetSymbolInfo(m1Invocation).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void FunctionPointerGenericSubstitutionConflictingReturnSubstitution()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M1<T>(delegate*<T> param1, delegate*<T> param2) {}
    public void M2()
    {
        delegate*<string> p1 = null;
        delegate*<int> p2 = null;
        M1(p1, p2);
    }
}");

            comp.VerifyDiagnostics(
                // (9,9): error CS0411: The type arguments for method 'C.M1<T>(delegate*<T>, delegate*<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(p1, p2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("C.M1<T>(delegate*<T>, delegate*<T>)").WithLocation(9, 9)
            );
        }

        [Fact]
        public void FunctionPointerGenericSubstitutionInference()
        {

            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M1<T>(delegate*<T, T> param) {}
    public void M2<T>()
    {
        delegate*<T, T> p = null;
        M1(p);
    }
}");

            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var m1InvocationSymbol = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;

            Assert.NotSame(m1InvocationSymbol, m1InvocationSymbol.OriginalDefinition);
            Assert.Equal(TypeKind.TypeParameter, m1InvocationSymbol.TypeArguments[0].TypeKind);
            var functionPointer = (IFunctionPointerTypeSymbol)m1InvocationSymbol.Parameters[0].Type;
            Assert.Equal(TypeKind.TypeParameter, functionPointer.Signature.ReturnType.TypeKind);
            Assert.Equal(TypeKind.TypeParameter, functionPointer.Signature.Parameters[0].Type.TypeKind);

            var declaredSymbol = (IMethodSymbol)comp.GetTypeByMetadataName("C").GetMethod("M2").ISymbol;
            Assert.True(declaredSymbol.TypeParameters[0].Equals(functionPointer.Signature.ReturnType, TypeCompareKind.ConsiderEverything));
            Assert.True(declaredSymbol.TypeParameters[0].Equals(functionPointer.Signature.Parameters[0].Type, TypeCompareKind.ConsiderEverything));

            VerifyOperationTreeForNode(comp, model, invocation, expectedOperationTree: @"
IInvocationOperation ( void C.M1<T>(delegate*<T, T> param)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M1(p)')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M1')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: param) (OperationKind.Argument, Type: null) (Syntax: 'p')
        ILocalReferenceOperation: p (OperationKind.LocalReference, Type: delegate*<T, T>) (Syntax: 'p')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");
        }

        [Fact, WorkItem(51037, "https://github.com/dotnet/roslyn/issues/51037")]
        public void FunctionPointerGenericSubstitutionCustomModifiersTypesDefinedOnClass()
        {
            var il = @"
.class public auto ansi beforefieldinit A`1<T>
    extends [mscorlib]System.Object
{
}
.class public auto ansi beforefieldinit C`6<T1, T2, T3, T4, T5, T6>
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        void M1 (
            method class A`1<!T1> modopt(class A`1<!T2>) & modopt(class A`1<!T3>) *(class A`1<!T4> modopt(class A`1<!T5>) & modopt(class A`1<!T6>)) param
        ) cil managed 
    {
        ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        nop
        ret
    }
}
";

            var source = @"
class Derived : C<D1, D2, D3, D4, D5, D6> {}

class D1 {}
class D2 {}
class D3 {}
class D4 {}
class D5 {}
class D6 {}
";

            var comp = CreateCompilationWithIL(source, il, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics();

            var derived = comp.GetTypeByMetadataName("Derived");
            var m1 = derived!.BaseTypeNoUseSiteDiagnostics.GetMethod("M1");

            AssertEx.Equal("void C<D1, D2, D3, D4, D5, D6>.M1(delegate*<ref modopt(A<D6>) A<D4> modopt(A<D5>), ref modopt(A<T3>) A<D1> modopt(A<D2>)> param)",
                           m1.ToTestDisplayString());
        }

        [Fact, WorkItem(51037, "https://github.com/dotnet/roslyn/issues/51037")]
        public void FunctionPointerGenericSubstitutionCustomModifiersTypesDefinedOnMethod()
        {
            var il = @"
.class public auto ansi beforefieldinit A`1<T>
    extends [mscorlib]System.Object
{
}
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        void M1<T1, T2, T3, T4, T5, T6> (
            method class A`1<!!T1> modopt(class A`1<!!T2>) & modopt(class A`1<!!T3>) *(class A`1<!!T4> modopt(class A`1<!!T5>) & modopt(class A`1<!!T6>)) param
        ) cil managed 
    {
        ret
    }
}
";

            var source = @"
unsafe
{
    C.M1<D1, D2, D3, D4, D5, D6>(null);
}

class D1 {}
class D2 {}
class D3 {}
class D4 {}
class D5 {}
class D6 {}
";

            var comp = CreateCompilationWithIL(source, il, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            var symbol = model.GetSymbolInfo(invocation);

            AssertEx.Equal("void C.M1<D1, D2, D3, D4, D5, D6>(delegate*<ref modopt(A<D6>) A<D4> modopt(A<D5>), ref modopt(A<T3>) A<D1> modopt(A<D2>)> param)",
                           symbol.Symbol.ToTestDisplayString());
        }

        [Fact]
        public void FunctionPointerGenericSubstitution_SubstitutionAddsCallConvModopts()
        {
            var il = @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .field public static int32 modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) modopt([mscorlib]System.Runtime.CompilerServices.CallConvStdcall) Field
}
";

            var code = @"
unsafe
{
    delegate* unmanaged<int> ptr = M(C.Field);

    delegate* unmanaged<T> M<T>(T arg) => null;
}
";

            var comp = CreateCompilationWithIL(code, il, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var localSyntax = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            AssertEx.Equal("ptr = M(C.Field)", localSyntax.ToString());
            var local = (ILocalSymbol)model.GetDeclaredSymbol(localSyntax)!;

            AssertEx.Equal("delegate* unmanaged<System.Int32>", local.Type.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(localSyntax.Initializer!.Value);
            AssertEx.Equal("delegate* unmanaged<System.Int32>", typeInfo.Type.ToTestDisplayString());
        }

        [Fact]
        public void FunctionPointerGenericSubstitution_SubstitutionAddsCallConvModopts_InIL()
        {
            var cDefinition = @"
public class C<T>
{
    public unsafe delegate* unmanaged<T> M() => null;
}
";
            var cComp = CreateCompilation(cDefinition, assemblyName: "cLib");

            var il = @"
.assembly extern cLib
{
  .ver 0:0:0:0
}
.class public auto ansi beforefieldinit D
    extends class [cLib]C`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) modopt([mscorlib]System.Runtime.CompilerServices.CallConvStdcall)>
{
}
";

            var comp = CreateCompilationWithIL("", il, references: new[] { cComp.ToMetadataReference() }, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics();

            var d = comp.GetTypeByMetadataName("D");
            var m = d!.BaseTypeNoUseSiteDiagnostics.GetMethod("M");

            var funcPtrType = (FunctionPointerTypeSymbol)m.ReturnType;
            AssertEx.Equal("delegate* unmanaged[Stdcall, Cdecl]<System.Int32 modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvCdecl)>", funcPtrType.ToTestDisplayString());
            AssertEx.SetEqual(new[]
                {
                    "System.Runtime.CompilerServices.CallConvCdecl",
                    "System.Runtime.CompilerServices.CallConvStdcall"
                },
                funcPtrType.Signature.GetCallingConventionModifiers().Select(c => ((CSharpCustomModifier)c).ModifierSymbol.ToTestDisplayString()));
        }

        [Fact]
        public void FunctionPointerAsConstraint()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C<T> where T : delegate*<void> {}");

            comp.VerifyDiagnostics(
                // (2,29): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                // unsafe class C<T> where T : delegate*<void> {}
                Diagnostic(ErrorCode.ERR_BadConstraintType, "delegate*<void>").WithLocation(2, 29)
            );
        }

        [Fact]
        public void FunctionPointerAsTypeArgument()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
class C<T> {}
unsafe class D : C<delegate*<void>>
{
    public static C<TStatic> SubstitutedStatic<TStatic>(TStatic s) => throw null;
    public static C<TStatic> SubstitutedStatic2<TStatic>(TStatic s1, TStatic s2) => throw null;
    public static void M()
    {
        _ = new C<delegate*<void>>();
        SubstitutedStatic<delegate*<void>>(null);
        delegate*<string, void> ptr1 = null;
        SubstitutedStatic(ptr1);
        delegate*<object, void> ptr2 = null;
        delegate*<int, void> ptr3 = null;
        delegate* unmanaged[Cdecl]<string, void> ptr4 = null;
        SubstitutedStatic2(ptr1, ptr1);
        SubstitutedStatic2(ptr1, ptr2);
        SubstitutedStatic2(ptr1, ptr3);
        SubstitutedStatic2(ptr1, ptr4);

        delegate*<object> ptr5 = null;
        delegate*<string> ptr6 = null;
        delegate*<int> ptr7 = null;
        delegate* unmanaged[Cdecl]<object> ptr8 = null;
        SubstitutedStatic2(ptr5, ptr5);
        SubstitutedStatic2(ptr5, ptr6);
        SubstitutedStatic2(ptr5, ptr7);
        SubstitutedStatic2(ptr5, ptr8);
    }
}");

            comp.VerifyDiagnostics(
                // (3,14): error CS0306: The type 'delegate*<void>' may not be used as a type argument
                // unsafe class D : C<delegate*<void>>
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "D").WithArguments("delegate*<void>").WithLocation(3, 14),
                // (9,19): error CS0306: The type 'delegate*<void>' may not be used as a type argument
                //         _ = new C<delegate*<void>>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "delegate*<void>").WithArguments("delegate*<void>").WithLocation(9, 19),
                // (10,9): error CS0306: The type 'delegate*<void>' may not be used as a type argument
                //         SubstitutedStatic<delegate*<void>>(null);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "SubstitutedStatic<delegate*<void>>").WithArguments("delegate*<void>").WithLocation(10, 9),
                // (12,9): error CS0306: The type 'delegate*<string, void>' may not be used as a type argument
                //         SubstitutedStatic(ptr1);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "SubstitutedStatic").WithArguments("delegate*<string, void>").WithLocation(12, 9),
                // (16,9): error CS0306: The type 'delegate*<string, void>' may not be used as a type argument
                //         SubstitutedStatic2(ptr1, ptr1);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "SubstitutedStatic2").WithArguments("delegate*<string, void>").WithLocation(16, 9),
                // (17,9): error CS0306: The type 'delegate*<string, void>' may not be used as a type argument
                //         SubstitutedStatic2(ptr1, ptr2);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "SubstitutedStatic2").WithArguments("delegate*<string, void>").WithLocation(17, 9),
                // (18,9): error CS0411: The type arguments for method 'D.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr1, ptr3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("D.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(18, 9),
                // (19,9): error CS0411: The type arguments for method 'D.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr1, ptr4);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("D.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(19, 9),
                // (25,9): error CS0306: The type 'delegate*<object>' may not be used as a type argument
                //         SubstitutedStatic2(ptr5, ptr5);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "SubstitutedStatic2").WithArguments("delegate*<object>").WithLocation(25, 9),
                // (26,9): error CS0306: The type 'delegate*<object>' may not be used as a type argument
                //         SubstitutedStatic2(ptr5, ptr6);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "SubstitutedStatic2").WithArguments("delegate*<object>").WithLocation(26, 9),
                // (27,9): error CS0411: The type arguments for method 'D.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr5, ptr7);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("D.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(27, 9),
                // (28,9): error CS0411: The type arguments for method 'D.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr5, ptr8);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("D.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(28, 9)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocationTypes = tree.GetRoot()
                                      .DescendantNodes()
                                      .OfType<InvocationExpressionSyntax>()
                                      .Select(s => model.GetSymbolInfo(s).CandidateSymbols.Single())
                                      .Cast<IMethodSymbol>()
                                      .Select(m => m!.TypeArguments.Single().ToTestDisplayString())
                                      .ToList();

            var expectedTypes = new string[] {
                "delegate*<System.Void>",
                "delegate*<System.String, System.Void>",
                "delegate*<System.String, System.Void>",
                "delegate*<System.String, System.Void>",
                "TStatic",
                "TStatic",
                "delegate*<System.Object>",
                "delegate*<System.Object>",
                "TStatic",
                "TStatic"
            };

            AssertEx.Equal(expectedTypes, invocationTypes);
        }

        [Fact]
        public void ArrayOfFunctionPointersAsTypeArguments()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public static void SubstitutedStatic2<TStatic>(TStatic s1, TStatic s2) => throw null;
    public static void M()
    {
        delegate*<string, void>[] ptr1 = null;
        delegate*<object, void>[] ptr2 = null;
        SubstitutedStatic2(ptr1, ptr1);
        SubstitutedStatic2(ptr1, ptr2);

        delegate*<object>[] ptr3 = null;
        delegate*<string>[] ptr4 = null;
        SubstitutedStatic2(ptr3, ptr3);
        SubstitutedStatic2(ptr3, ptr4);
    }
}");

            comp.VerifyDiagnostics(
                // (10,9): error CS0411: The type arguments for method 'C.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr1, ptr2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("C.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(10, 9),
                // (15,9): error CS0411: The type arguments for method 'C.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr3, ptr4);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("C.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(15, 9)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocationTypes = tree.GetRoot()
                                      .DescendantNodes()
                                      .OfType<InvocationExpressionSyntax>()
                                      .Select(s =>
                                      {
                                          var symbolInfo = model.GetSymbolInfo(s);
                                          return symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.Single();
                                      })
                                      .Cast<IMethodSymbol>()
                                      .Select(m => m!.TypeArguments.Single().ToTestDisplayString())
                                      .ToList();

            var expectedTypes = new string[] {
                "delegate*<System.String, System.Void>[]",
                "TStatic",
                "delegate*<System.Object>[]",
                "TStatic"
            };

            AssertEx.Equal(expectedTypes, invocationTypes);
        }

        [Fact]
        public void ArrayOfFunctionPointersAsTypeArguments_NoBestType()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public static void SubstitutedStatic2<TStatic>(TStatic s1, TStatic s2) => throw null;
    public static void M()
    {
        delegate*<string, void>[] ptr1 = null;
        delegate*<ref string, void>[] ptr2 = null;
        delegate*<int, void>[] ptr3 = null;
        delegate* unmanaged[Cdecl]<string, void>[] ptr4 = null;
        SubstitutedStatic2(ptr1, ptr2);
        SubstitutedStatic2(ptr1, ptr3);
        SubstitutedStatic2(ptr1, ptr4);

        delegate*<string>[] ptr5 = null;
        delegate*<ref string>[] ptr6 = null;
        delegate*<int>[] ptr7 = null;
        delegate* unmanaged[Cdecl]<string>[] ptr8 = null;
        SubstitutedStatic2(ptr5, ptr6);
        SubstitutedStatic2(ptr5, ptr7);
        SubstitutedStatic2(ptr5, ptr8);
    }
}");

            comp.VerifyDiagnostics(
                // (11,9): error CS0411: The type arguments for method 'C.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr1, ptr2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("C.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(11, 9),
                // (12,9): error CS0411: The type arguments for method 'C.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr1, ptr3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("C.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(12, 9),
                // (13,9): error CS0411: The type arguments for method 'C.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr1, ptr4);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("C.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(13, 9),
                // (19,9): error CS0411: The type arguments for method 'C.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr5, ptr6);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("C.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(19, 9),
                // (20,9): error CS0411: The type arguments for method 'C.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr5, ptr7);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("C.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(20, 9),
                // (21,9): error CS0411: The type arguments for method 'C.SubstitutedStatic2<TStatic>(TStatic, TStatic)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         SubstitutedStatic2(ptr5, ptr8);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "SubstitutedStatic2").WithArguments("C.SubstitutedStatic2<TStatic>(TStatic, TStatic)").WithLocation(21, 9)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocationTypes = tree.GetRoot()
                                      .DescendantNodes()
                                      .OfType<InvocationExpressionSyntax>()
                                      .Select(s =>
                                      {
                                          var symbolInfo = model.GetSymbolInfo(s);
                                          return symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.Single();
                                      })
                                      .Cast<IMethodSymbol>()
                                      .Select(m => m!.TypeArguments.Single().ToTestDisplayString())
                                      .ToList();

            var expectedTypes = new string[] {
                "TStatic",
                "TStatic",
                "TStatic",
                "TStatic",
                "TStatic",
                "TStatic",
            };

            AssertEx.Equal(expectedTypes, invocationTypes);
        }

        [Fact]
        public void NoBestTypeArrayInitializer()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public static void M()
    {
        delegate*<string, void>[] ptr1 = null;
        delegate*<ref string, void>[] ptr2 = null;
        delegate*<int, void>[] ptr3 = null;
        delegate* unmanaged[Cdecl]<string, void>[] ptr4 = null;
        var arr1 = new[] { ptr1, ptr2 };
        var arr2 = new[] { ptr1, ptr3 };
        var arr3 = new[] { ptr1, ptr4 };

        delegate*<string>[] ptr5 = null;
        delegate*<ref string>[] ptr6 = null;
        delegate*<int>[] ptr7 = null;
        delegate* unmanaged[Cdecl]<string>[] ptr8 = null;
        var arr4 = new[] { ptr5, ptr6 };
        var arr5 = new[] { ptr5, ptr7 };
        var arr6 = new[] { ptr5, ptr8 };
    }
}");

            comp.VerifyDiagnostics(
                // (10,20): error CS0826: No best type found for implicitly-typed array
                //         var arr1 = new[] { ptr1, ptr2 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { ptr1, ptr2 }").WithLocation(10, 20),
                // (11,20): error CS0826: No best type found for implicitly-typed array
                //         var arr2 = new[] { ptr1, ptr3 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { ptr1, ptr3 }").WithLocation(11, 20),
                // (12,20): error CS0826: No best type found for implicitly-typed array
                //         var arr3 = new[] { ptr1, ptr4 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { ptr1, ptr4 }").WithLocation(12, 20),
                // (18,20): error CS0826: No best type found for implicitly-typed array
                //         var arr4 = new[] { ptr5, ptr6 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { ptr5, ptr6 }").WithLocation(18, 20),
                // (19,20): error CS0826: No best type found for implicitly-typed array
                //         var arr5 = new[] { ptr5, ptr7 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { ptr5, ptr7 }").WithLocation(19, 20),
                // (20,20): error CS0826: No best type found for implicitly-typed array
                //         var arr6 = new[] { ptr5, ptr8 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { ptr5, ptr8 }").WithLocation(20, 20)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocationTypes = tree.GetRoot()
                                      .DescendantNodes()
                                      .OfType<ImplicitArrayCreationExpressionSyntax>()
                                      .Select(s => model.GetTypeInfo(s).Type.ToTestDisplayString())
                                      .ToList();

            var expectedTypes = new string[] {
                "?[]",
                "?[]",
                "?[]",
                "?[]",
                "?[]",
                "?[]"
            };

            AssertEx.Equal(expectedTypes, invocationTypes);
        }

        [Fact]
        public void NoBestTypeConditional()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public static void M(bool b)
    {
        delegate*<string, void>[] ptr1 = null;
        delegate*<ref string, void>[] ptr2 = null;
        delegate*<int, void>[] ptr3 = null;
        delegate* unmanaged[Cdecl]<string, void>[] ptr4 = null;
        _ = b ? ptr1 : ptr2;
        _ = b ? ptr1 : ptr3;
        _ = b ? ptr1 : ptr4;

        delegate*<string>[] ptr5 = null;
        delegate*<ref string>[] ptr6 = null;
        delegate*<int>[] ptr7 = null;
        delegate* unmanaged[Cdecl]<string>[] ptr8 = null;
        _ = b ? ptr5 : ptr6;
        _ = b ? ptr5 : ptr7;
        _ = b ? ptr5 : ptr8;
    }
}");

            comp.VerifyDiagnostics(
                // (10,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string, void>[]' and 'delegate*<ref string, void>[]'
                //         _ = b ? ptr1 : ptr2;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr1 : ptr2").WithArguments("delegate*<string, void>[]", "delegate*<ref string, void>[]").WithLocation(10, 13),
                // (11,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string, void>[]' and 'delegate*<int, void>[]'
                //         _ = b ? ptr1 : ptr3;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr1 : ptr3").WithArguments("delegate*<string, void>[]", "delegate*<int, void>[]").WithLocation(11, 13),
                // (12,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string, void>[]' and 'delegate* unmanaged[Cdecl]<string, void>[]'
                //         _ = b ? ptr1 : ptr4;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr1 : ptr4").WithArguments("delegate*<string, void>[]", "delegate* unmanaged[Cdecl]<string, void>[]").WithLocation(12, 13),
                // (18,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string>[]' and 'delegate*<ref string>[]'
                //         _ = b ? ptr5 : ptr6;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr5 : ptr6").WithArguments("delegate*<string>[]", "delegate*<ref string>[]").WithLocation(18, 13),
                // (19,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string>[]' and 'delegate*<int>[]'
                //         _ = b ? ptr5 : ptr7;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr5 : ptr7").WithArguments("delegate*<string>[]", "delegate*<int>[]").WithLocation(19, 13),
                // (20,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string>[]' and 'delegate* unmanaged[Cdecl]<string>[]'
                //         _ = b ? ptr5 : ptr8;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr5 : ptr8").WithArguments("delegate*<string>[]", "delegate* unmanaged[Cdecl]<string>[]").WithLocation(20, 13)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocationTypes = tree.GetRoot()
                                      .DescendantNodes()
                                      .OfType<ConditionalExpressionSyntax>()
                                      .Select(s => model.GetTypeInfo(s).Type.ToTestDisplayString())
                                      .ToList();

            var expectedTypes = new string[] {
                "?",
                "?",
                "?",
                "?",
                "?",
                "?"
            };

            AssertEx.Equal(expectedTypes, invocationTypes);
        }

        [Fact]
        public void BestTypeInferrerInvertingVariance()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
interface I<in T> {}
unsafe class C
{
    static void Print(object o) => Console.Write(o);
    static void Print(string s) => Console.Write(s);
    static void Main()
    {
        I<delegate*<string, void>[]> i1 = null;
        I<delegate*<object, void>[]> i2 = null;
        I<delegate*<object, void>[]>[] iArr = new[] { i1, i2 }; 
    }
}");

            // Array variance is reference-type only, so there is no best time between i1 and i2
            comp.VerifyDiagnostics(
                // (12,47): error CS0826: No best type found for implicitly-typed array
                //         I<delegate*<object, void>[]>[] iArr = new[] { i1, i2 }; 
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { i1, i2 }").WithLocation(12, 47)
            );
        }

        [Fact]
        public void MergeVariantAnnotations_ReturnTypes()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
#nullable enable
unsafe class C
{
    void M(bool b)
    {
        delegate*<string> ptr1 = null;
        delegate*<string?> ptr2 = null;
        delegate*<ref string> ptr3 = null;
        delegate*<ref string?> ptr4 = null;
        _ = b ? ptr1 : ptr2;
        _ = b ? ptr1 : ptr3;
        _ = b ? ptr1 : ptr4;
        _ = b ? ptr3 : ptr4;

        delegate*<string> ptr5 = null;
        delegate*<string?> ptr6 = null;
        delegate*<ref string> ptr7 = null;
        delegate*<ref string?> ptr8 = null;
        _ = b ? ptr5 : ptr6;
        _ = b ? ptr5 : ptr7;
        _ = b ? ptr5 : ptr8;
        _ = b ? ptr7 : ptr8;
    }
}");

            comp.VerifyDiagnostics(
                // (12,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string>' and 'delegate*<ref string>'
                //         _ = b ? ptr1 : ptr3;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr1 : ptr3").WithArguments("delegate*<string>", "delegate*<ref string>").WithLocation(12, 13),
                // (13,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string>' and 'delegate*<ref string?>'
                //         _ = b ? ptr1 : ptr4;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr1 : ptr4").WithArguments("delegate*<string>", "delegate*<ref string?>").WithLocation(13, 13),
                // (14,24): warning CS8619: Nullability of reference types in value of type 'delegate*<ref string?>' doesn't match target type 'delegate*<ref string>'.
                //         _ = b ? ptr3 : ptr4;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "ptr4").WithArguments("delegate*<ref string?>", "delegate*<ref string>").WithLocation(14, 24),
                // (21,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string>' and 'delegate*<ref string>'
                //         _ = b ? ptr5 : ptr7;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr5 : ptr7").WithArguments("delegate*<string>", "delegate*<ref string>").WithLocation(21, 13),
                // (22,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string>' and 'delegate*<ref string?>'
                //         _ = b ? ptr5 : ptr8;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr5 : ptr8").WithArguments("delegate*<string>", "delegate*<ref string?>").WithLocation(22, 13),
                // (23,24): warning CS8619: Nullability of reference types in value of type 'delegate*<ref string?>' doesn't match target type 'delegate*<ref string>'.
                //         _ = b ? ptr7 : ptr8;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "ptr8").WithArguments("delegate*<ref string?>", "delegate*<ref string>").WithLocation(23, 24)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocationTypes = tree.GetRoot()
                                      .DescendantNodes()
                                      .OfType<ConditionalExpressionSyntax>()
                                      .Select(s => model.GetTypeInfo(s).Type.ToTestDisplayString())
                                      .ToList();

            var expectedTypes = new string[] {
               "delegate*<System.String?>",
               "?",
               "?",
               "delegate*<ref System.String>",
               "delegate*<System.String?>",
               "?",
               "?",
               "delegate*<ref System.String>"
            };

            AssertEx.Equal(expectedTypes, invocationTypes);
        }

        [Fact]
        public void MergeVariantAnnotations_ParamTypes()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
#nullable enable
unsafe class C
{
    void M(bool b)
    {
        delegate*<string, void> ptr1 = null;
        delegate*<string?, void> ptr2 = null;
        delegate*<ref string, void> ptr3 = null;
        delegate*<ref string?, void> ptr4 = null;
        _ = b ? ptr1 : ptr2;
        _ = b ? ptr1 : ptr3;
        _ = b ? ptr1 : ptr4;
        _ = b ? ptr3 : ptr4;

        delegate*<string, void> ptr5 = null;
        delegate*<string?, void> ptr6 = null;
        delegate*<ref string, void> ptr7 = null;
        delegate*<ref string?, void> ptr8 = null;
        _ = b ? ptr5 : ptr6;
        _ = b ? ptr5 : ptr7;
        _ = b ? ptr5 : ptr8;
        _ = b ? ptr7 : ptr8;
    }
}");

            comp.VerifyDiagnostics(
                // (12,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string, void>' and 'delegate*<ref string, void>'
                //         _ = b ? ptr1 : ptr3;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr1 : ptr3").WithArguments("delegate*<string, void>", "delegate*<ref string, void>").WithLocation(12, 13),
                // (13,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string, void>' and 'delegate*<ref string?, void>'
                //         _ = b ? ptr1 : ptr4;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr1 : ptr4").WithArguments("delegate*<string, void>", "delegate*<ref string?, void>").WithLocation(13, 13),
                // (14,24): warning CS8619: Nullability of reference types in value of type 'delegate*<ref string?, void>' doesn't match target type 'delegate*<ref string, void>'.
                //         _ = b ? ptr3 : ptr4;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "ptr4").WithArguments("delegate*<ref string?, void>", "delegate*<ref string, void>").WithLocation(14, 24),
                // (21,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string, void>' and 'delegate*<ref string, void>'
                //         _ = b ? ptr5 : ptr7;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr5 : ptr7").WithArguments("delegate*<string, void>", "delegate*<ref string, void>").WithLocation(21, 13),
                // (22,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'delegate*<string, void>' and 'delegate*<ref string?, void>'
                //         _ = b ? ptr5 : ptr8;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? ptr5 : ptr8").WithArguments("delegate*<string, void>", "delegate*<ref string?, void>").WithLocation(22, 13),
                // (23,24): warning CS8619: Nullability of reference types in value of type 'delegate*<ref string?, void>' doesn't match target type 'delegate*<ref string, void>'.
                //         _ = b ? ptr7 : ptr8;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "ptr8").WithArguments("delegate*<ref string?, void>", "delegate*<ref string, void>").WithLocation(23, 24)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocationTypes = tree.GetRoot()
                                      .DescendantNodes()
                                      .OfType<ConditionalExpressionSyntax>()
                                      .Select(s => model.GetTypeInfo(s).Type.ToTestDisplayString())
                                      .ToList();

            var expectedTypes = new string[] {
               "delegate*<System.String, System.Void>",
               "?",
               "?",
               "delegate*<ref System.String, System.Void>",
               "delegate*<System.String, System.Void>",
               "?",
               "?",
               "delegate*<ref System.String, System.Void>"
            };

            AssertEx.Equal(expectedTypes, invocationTypes);
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInferenceInParameter()
        {
            var verifier = CompileAndVerify(@"
unsafe
{
    Test(0, &converter);

    static string converter(int v) => string.Empty;
    static void Test<T1, T2>(T1 t1, delegate*<T1, T2> func) {}
}
", options: TestOptions.UnsafeReleaseExe, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped);

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldftn      ""string Program.<<Main>$>g__converter|0_0(int)""
  IL_0007:  call       ""void Program.<<Main>$>g__Test|0_1<int, string>(int, delegate*<int, string>)""
  IL_000c:  ret
}
");
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInferenceInParameter_ImplicitReferenceConversionOnParameter()
        {
            var verifier = CompileAndVerify(@"
unsafe
{
    Test(string.Empty, &converter);

    static string converter(object o) => string.Empty;
    static void Test<T1, T2>(T1 t1, delegate*<T1, T2> func) {}
}
", options: TestOptions.UnsafeReleaseExe, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped);

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldsfld     ""string string.Empty""
  IL_0005:  ldftn      ""string Program.<<Main>$>g__converter|0_0(object)""
  IL_000b:  call       ""void Program.<<Main>$>g__Test|0_1<string, string>(string, delegate*<string, string>)""
  IL_0010:  ret
}
");
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInferenceInReturn()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe
{
    Test(0, &converter);

    static int converter(string v) => 0;
    static void Test<T1, T2>(T2 t2, delegate*<T1, T2> func) {}
}
", options: TestOptions.UnsafeReleaseExe);

            // It might seem like this should have the same behavior as FunctionPointerInferenceInParameter. However, this is not how method group resolution works.
            // We don't look at return types when resolving a method group, which means that we can't use T2 to narrow down the candidates. We therefore can't
            // find any information about T1, and resolution fails. FunctionPointerInferenceInParameter, on the other hand, has a bound for T1: int. That bound
            // is then used as an input to method group resolution, and we can narrow down to string converter(int v). The return type of that method can then be
            // used as a bound to infer T2.
            comp.VerifyDiagnostics(
                // (4,5): error CS0411: The type arguments for method 'Test<T1, T2>(T2, delegate*<T1, T2>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test(0, &converter);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Test<T1, T2>(T2, delegate*<T1, T2>)").WithLocation(4, 5),
                // (7,17): warning CS8321: The local function 'Test' is declared but never used
                //     static void Test<T1, T2>(T2 t2, delegate*<T1, T2> func) {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Test").WithArguments("Test").WithLocation(7, 17)
            );
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInference_ThroughMethodGroup()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe
{
    Test1(string.Empty, converter);
    Test2(0, converter);
    Test1<string, int>(string.Empty, converter);
    Test2<string, int>(0, converter);

    static int converter(string v) => 0;
    static void Test1<T1, T2>(T1 t1, delegate*<T1, T2> func) {}
    static void Test2<T1, T2>(T2 t2, delegate*<T1, T2> func) {}
}
", options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (4,5): error CS0411: The type arguments for method 'Test1<T1, T2>(T1, delegate*<T1, T2>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test1(string.Empty, converter);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test1").WithArguments("Test1<T1, T2>(T1, delegate*<T1, T2>)").WithLocation(4, 5),
                // (5,5): error CS0411: The type arguments for method 'Test2<T1, T2>(T2, delegate*<T1, T2>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test2(0, converter);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test2").WithArguments("Test2<T1, T2>(T2, delegate*<T1, T2>)").WithLocation(5, 5),
                // (6,38): error CS8787: Cannot convert method group to function pointer (Are you missing a '&'?)
                //     Test1<string, int>(string.Empty, converter);
                Diagnostic(ErrorCode.ERR_MissingAddressOf, "converter").WithLocation(6, 38),
                // (7,27): error CS8787: Cannot convert method group to function pointer (Are you missing a '&'?)
                //     Test2<string, int>(0, converter);
                Diagnostic(ErrorCode.ERR_MissingAddressOf, "converter").WithLocation(7, 27)
            );
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInference_ThroughLambdaExpression()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe
{
    Test1(string.Empty, v => 0);
    Test2(0, v => 0);
    Test1<string, int>(string.Empty, v => 0);
    Test2<string, int>(0, v => 0);

    static void Test1<T1, T2>(T1 t1, delegate*<T1, T2> func) {}
    static void Test2<T1, T2>(T2 t2, delegate*<T1, T2> func) {}
}
", options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (4,5): error CS0411: The type arguments for method 'Test1<T1, T2>(T1, delegate*<T1, T2>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test1(string.Empty, v => 0);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test1").WithArguments("Test1<T1, T2>(T1, delegate*<T1, T2>)").WithLocation(4, 5),
                // (5,5): error CS0411: The type arguments for method 'Test2<T1, T2>(T2, delegate*<T1, T2>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test2(0, v => 0);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test2").WithArguments("Test2<T1, T2>(T2, delegate*<T1, T2>)").WithLocation(5, 5),
                // (6,40): error CS1660: Cannot convert lambda expression to type 'delegate*<string, int>' because it is not a delegate type
                //     Test1<string, int>(string.Empty, v => 0);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "delegate*<string, int>").WithLocation(6, 40),
                // (7,29): error CS1660: Cannot convert lambda expression to type 'delegate*<string, int>' because it is not a delegate type
                //     Test2<string, int>(0, v => 0);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "delegate*<string, int>").WithLocation(7, 29));
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void AddressOfInference_OnDelegateType()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
unsafe
{
    Test1(string.Empty, &converter);
    Test2(0, &converter);
    Test1<string, int>(string.Empty, &converter);
    Test2<string, int>(0, &converter);

    static int converter(string v) => 0;
    static void Test1<T1, T2>(T1 t1, Func<T1, T2> func) {}
    static void Test2<T1, T2>(T2 t2, Func<T1, T2> func) {}
}
", options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (5,5): error CS0411: The type arguments for method 'Test1<T1, T2>(T1, Func<T1, T2>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test1(string.Empty, &converter);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test1").WithArguments("Test1<T1, T2>(T1, System.Func<T1, T2>)").WithLocation(5, 5),
                // (6,5): error CS0411: The type arguments for method 'Test2<T1, T2>(T2, Func<T1, T2>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test2(0, &converter);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test2").WithArguments("Test2<T1, T2>(T2, System.Func<T1, T2>)").WithLocation(6, 5),
                // (7,38): error CS1503: Argument 2: cannot convert from '&method group' to 'Func<string, int>'
                //     Test1<string, int>(string.Empty, &converter);
                Diagnostic(ErrorCode.ERR_BadArgType, "&converter").WithArguments("2", "&method group", "System.Func<string, int>").WithLocation(7, 38),
                // (8,27): error CS1503: Argument 2: cannot convert from '&method group' to 'Func<string, int>'
                //     Test2<string, int>(0, &converter);
                Diagnostic(ErrorCode.ERR_BadArgType, "&converter").WithArguments("2", "&method group", "System.Func<string, int>").WithLocation(8, 27)
            );
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInference_ExactInferenceThroughArray_CallingConventionMismatch()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe
{
    delegate*<string, void>[] ptr1 = null;
    Test1(ptr1);
    Test1<string>(ptr1);
    delegate* unmanaged[Cdecl, Stdcall]<string, void>[] ptr2 = null;
    Test1(ptr2);
    Test1<string>(ptr2);

    static void Test1<T1>(delegate* unmanaged<T1, void>[] func) {}
}
", options: TestOptions.UnsafeReleaseExe, targetFramework: TargetFramework.NetCoreApp);

            comp.VerifyDiagnostics(
                // (5,5): error CS0411: The type arguments for method 'Test1<T1>(delegate* unmanaged<T1, void>[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test1(ptr1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test1").WithArguments("Test1<T1>(delegate* unmanaged<T1, void>[])").WithLocation(5, 5),
                // (6,19): error CS1503: Argument 1: cannot convert from 'delegate*<string, void>[]' to 'delegate* unmanaged<string, void>[]'
                //     Test1<string>(ptr1);
                Diagnostic(ErrorCode.ERR_BadArgType, "ptr1").WithArguments("1", "delegate*<string, void>[]", "delegate* unmanaged<string, void>[]").WithLocation(6, 19),
                // (8,5): error CS0411: The type arguments for method 'Test1<T1>(delegate* unmanaged<T1, void>[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test1(ptr2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test1").WithArguments("Test1<T1>(delegate* unmanaged<T1, void>[])").WithLocation(8, 5),
                // (9,19): error CS1503: Argument 1: cannot convert from 'delegate* unmanaged[Cdecl, Stdcall]<string, void>[]' to 'delegate* unmanaged<string, void>[]'
                //     Test1<string>(ptr2);
                Diagnostic(ErrorCode.ERR_BadArgType, "ptr2").WithArguments("1", "delegate* unmanaged[Cdecl, Stdcall]<string, void>[]", "delegate* unmanaged<string, void>[]").WithLocation(9, 19)
            );
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInference_ExactInferenceThroughArray_RefKindMismatch()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe
{
    delegate*<ref string, string>[] ptr1 = null;
    Test1(ptr1);
    Test1<string, string>(ptr1);
    delegate*<string, ref string>[] ptr2 = null;
    Test1(ptr2);
    Test1<string, string>(ptr2);

    static void Test1<T1, T2>(delegate*<T1, T2>[] func) {}
}
", options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (5,5): error CS0411: The type arguments for method 'Test1<T1, T2>(delegate*<T1, T2>[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test1(ptr1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test1").WithArguments("Test1<T1, T2>(delegate*<T1, T2>[])").WithLocation(5, 5),
                // (6,27): error CS1503: Argument 1: cannot convert from 'delegate*<ref string, string>[]' to 'delegate*<string, string>[]'
                //     Test1<string, string>(ptr1);
                Diagnostic(ErrorCode.ERR_BadArgType, "ptr1").WithArguments("1", "delegate*<ref string, string>[]", "delegate*<string, string>[]").WithLocation(6, 27),
                // (8,5): error CS0411: The type arguments for method 'Test1<T1, T2>(delegate*<T1, T2>[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test1(ptr2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test1").WithArguments("Test1<T1, T2>(delegate*<T1, T2>[])").WithLocation(8, 5),
                // (9,27): error CS1503: Argument 1: cannot convert from 'delegate*<string, ref string>[]' to 'delegate*<string, string>[]'
                //     Test1<string, string>(ptr2);
                Diagnostic(ErrorCode.ERR_BadArgType, "ptr2").WithArguments("1", "delegate*<string, ref string>[]", "delegate*<string, string>[]").WithLocation(9, 27)
            );
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInference_ExactInferenceThroughArray_RefKindMatch()
        {
            // ILVerify: Unexpected type on the stack. ImportCalli not implemented
            var verifier = CompileAndVerify(@"
unsafe
{
    var ptr1 = new delegate*<ref string, string>[] { &Test };
    Test1(ptr1);

    static string Test(ref string s) => s = ""1"";
    static void Test1<T1, T2>(delegate*<ref T1, T2>[] func) {
        T1 t = default;
        System.Console.Write(func[0](ref t));
        System.Console.Write(t);
    }
}
", expectedOutput: "11", options: TestOptions.UnsafeReleaseExe, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.FailsILVerify : Verification.Skipped);

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       21 (0x15)
  .maxstack  4
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""delegate*<ref string, string>""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldftn      ""string Program.<<Main>$>g__Test|0_0(ref string)""
  IL_000e:  stelem.i
  IL_000f:  call       ""void Program.<<Main>$>g__Test1|0_1<string, string>(delegate*<ref string, string>[])""
  IL_0014:  ret
}
");
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInference_LowerBoundInference_CallingConventionMismatch()
        {
            var verifier = CreateCompilationWithFunctionPointers(@"
unsafe
{
    delegate*<int, string> ptr = null;
    Test(ptr);
    Test<int, string>(ptr);

    static void Test<T1, T2>(delegate* unmanaged[Cdecl]<T1, T2> func) {}
}
", options: TestOptions.UnsafeReleaseExe);

            verifier.VerifyDiagnostics(
                // (5,5): error CS0411: The type arguments for method 'Test<T1, T2>(delegate* unmanaged[Cdecl]<T1, T2>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test(ptr);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Test<T1, T2>(delegate* unmanaged[Cdecl]<T1, T2>)").WithLocation(5, 5),
                // (6,23): error CS1503: Argument 1: cannot convert from 'delegate*<int, string>' to 'delegate* unmanaged[Cdecl]<int, string>'
                //     Test<int, string>(ptr);
                Diagnostic(ErrorCode.ERR_BadArgType, "ptr").WithArguments("1", "delegate*<int, string>", "delegate* unmanaged[Cdecl]<int, string>").WithLocation(6, 23)
            );
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInference_LowerBoundInference_RefKindMismatch()
        {
            var verifier = CreateCompilationWithFunctionPointers(@"
unsafe
{
    delegate*<ref string, string> ptr1 = null;
    Test(ptr1);
    Test<string, string>(ptr1);
    delegate*<string, ref string> ptr2 = null;
    Test(ptr2);
    Test<string, string>(ptr2);

    static void Test<T1, T2>(delegate*<T1, T2> func) {}
}
", options: TestOptions.UnsafeReleaseExe);

            verifier.VerifyDiagnostics(
                // (5,5): error CS0411: The type arguments for method 'Test<T1, T2>(delegate*<T1, T2>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test(ptr1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Test<T1, T2>(delegate*<T1, T2>)").WithLocation(5, 5),
                // (6,26): error CS1503: Argument 1: cannot convert from 'delegate*<ref string, string>' to 'delegate*<string, string>'
                //     Test<string, string>(ptr1);
                Diagnostic(ErrorCode.ERR_BadArgType, "ptr1").WithArguments("1", "delegate*<ref string, string>", "delegate*<string, string>").WithLocation(6, 26),
                // (8,5): error CS0411: The type arguments for method 'Test<T1, T2>(delegate*<T1, T2>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test(ptr2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Test<T1, T2>(delegate*<T1, T2>)").WithLocation(8, 5),
                // (9,26): error CS1503: Argument 1: cannot convert from 'delegate*<string, ref string>' to 'delegate*<string, string>'
                //     Test<string, string>(ptr2);
                Diagnostic(ErrorCode.ERR_BadArgType, "ptr2").WithArguments("1", "delegate*<string, ref string>", "delegate*<string, string>").WithLocation(9, 26)
            );
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInference_UpperBoundInference_CallingConventionMismatch()
        {
            var verifier = CreateCompilationWithFunctionPointers(@"
unsafe
{
    delegate*<delegate*<string, string>, void> ptr = null;
    Test(ptr);
    Test<string, string>(ptr);

    static void Test<T1, T2>(delegate*<delegate* unmanaged[Cdecl]<T1, T2>, void> func) {}
}
", options: TestOptions.UnsafeReleaseExe);

            verifier.VerifyDiagnostics(
                // (5,5): error CS0411: The type arguments for method 'Test<T1, T2>(delegate*<delegate* unmanaged[Cdecl]<T1, T2>, void>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test(ptr);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Test<T1, T2>(delegate*<delegate* unmanaged[Cdecl]<T1, T2>, void>)").WithLocation(5, 5),
                // (6,26): error CS1503: Argument 1: cannot convert from 'delegate*<delegate*<string, string>, void>' to 'delegate*<delegate* unmanaged[Cdecl]<string, string>, void>'
                //     Test<string, string>(ptr);
                Diagnostic(ErrorCode.ERR_BadArgType, "ptr").WithArguments("1", "delegate*<delegate*<string, string>, void>", "delegate*<delegate* unmanaged[Cdecl]<string, string>, void>").WithLocation(6, 26)
            );
        }

        [Fact, WorkItem(50096, "https://github.com/dotnet/roslyn/issues/50096")]
        public void FunctionPointerInference_UpperBoundInference_RefKindMismatch()
        {
            var verifier = CreateCompilationWithFunctionPointers(@"
unsafe
{
    delegate*<delegate*<ref string, string>, void> ptr1 = null;
    Test(ptr1);
    Test<string, string>(ptr1);
    delegate*<delegate*<string, ref string>, void> ptr2 = null;
    Test(ptr2);
    Test<string, string>(ptr2);

    static void Test<T1, T2>(delegate*<delegate*<T1, T2>, void> func) {}
}
", options: TestOptions.UnsafeReleaseExe);

            verifier.VerifyDiagnostics(
                // (5,5): error CS0411: The type arguments for method 'Test<T1, T2>(delegate*<delegate*<T1, T2>, void>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test(ptr1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Test<T1, T2>(delegate*<delegate*<T1, T2>, void>)").WithLocation(5, 5),
                // (6,26): error CS1503: Argument 1: cannot convert from 'delegate*<delegate*<ref string, string>, void>' to 'delegate*<delegate*<string, string>, void>'
                //     Test<string, string>(ptr1);
                Diagnostic(ErrorCode.ERR_BadArgType, "ptr1").WithArguments("1", "delegate*<delegate*<ref string, string>, void>", "delegate*<delegate*<string, string>, void>").WithLocation(6, 26),
                // (8,5): error CS0411: The type arguments for method 'Test<T1, T2>(delegate*<delegate*<T1, T2>, void>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     Test(ptr2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Test<T1, T2>(delegate*<delegate*<T1, T2>, void>)").WithLocation(8, 5),
                // (9,26): error CS1503: Argument 1: cannot convert from 'delegate*<delegate*<string, ref string>, void>' to 'delegate*<delegate*<string, string>, void>'
                //     Test<string, string>(ptr2);
                Diagnostic(ErrorCode.ERR_BadArgType, "ptr2").WithArguments("1", "delegate*<delegate*<string, ref string>, void>", "delegate*<delegate*<string, string>, void>").WithLocation(9, 26)
            );
        }

        [Fact]
        public void FunctionPointerTypeCannotBeUsedInDynamicTypeArguments()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(dynamic d)
    {
        d.M<delegate*<void>>();
    }
}");

            comp.VerifyDiagnostics(
                // (6,13): error CS0306: The type 'delegate*<void>' may not be used as a type argument
                //         d.M<delegate*<void>>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "delegate*<void>").WithArguments("delegate*<void>").WithLocation(6, 13)
            );
        }

        [Fact]
        public void FunctionPointerTypeCannotBeUsedInDynamicArgument()
        {

            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(dynamic d, delegate*<void> ptr)
    {
        d.M(ptr);
    }
}");

            comp.VerifyDiagnostics(
                // (6,13): error CS1978: Cannot use an expression of type 'delegate*<void>' as an argument to a dynamically dispatched operation.
                //         d.M(ptr);
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "ptr").WithArguments("delegate*<void>").WithLocation(6, 13)
            );
        }

        [Fact]
        public void FunctionPointerTypeCannotBeConvertedFromDynamic()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<void> ptr)
    {
        dynamic d = ptr;
        d = (dynamic)ptr;
        ptr = d;
        ptr = (delegate*<void>)d;
    }
}");

            comp.VerifyDiagnostics(
                // (6,21): error CS0029: Cannot implicitly convert type 'delegate*<void>' to 'dynamic'
                //         dynamic d = ptr;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "ptr").WithArguments("delegate*<void>", "dynamic").WithLocation(6, 21),
                // (7,13): error CS0030: Cannot convert type 'delegate*<void>' to 'dynamic'
                //         d = (dynamic)ptr;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(dynamic)ptr").WithArguments("delegate*<void>", "dynamic").WithLocation(7, 13),
                // (8,15): error CS0029: Cannot implicitly convert type 'dynamic' to 'delegate*<void>'
                //         ptr = d;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d").WithArguments("dynamic", "delegate*<void>").WithLocation(8, 15),
                // (9,15): error CS0030: Cannot convert type 'dynamic' to 'delegate*<void>'
                //         ptr = (delegate*<void>)d;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(delegate*<void>)d").WithArguments("dynamic", "delegate*<void>").WithLocation(9, 15)
            );
        }

        [Fact]
        public void FunctionPointerTypeCannotBeQuestionDotted()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    delegate*<void> GetPtr() => null;
    void M(delegate*<void> ptr, C c)
    {
        ptr?.ToString();
        ptr = c?.GetPtr();
        (c?.GetPtr())();
    }
}");

            comp.VerifyDiagnostics(
                // (7,12): error CS0023: Operator '?' cannot be applied to operand of type 'delegate*<void>'
                //         ptr?.ToString();
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "delegate*<void>").WithLocation(7, 12));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocations = tree.GetRoot().DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().ToList();
            Assert.Equal(3, invocations.Count);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocations[0],
                expectedSyntax: "ptr?.ToString()",
                expectedType: "?",
                expectedSymbol: null,
                expectedSymbolCandidates: null);

            VerifyOperationTreeForNode(comp, model, invocations[0], expectedOperationTree: @"
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: ?, IsInvalid) (Syntax: 'ptr?.ToString()')
  Operation: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'ptr')
      Children(1):
          IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'ptr')
  WhenNotNull: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.ToString()')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '.ToString')
            Children(1):
                IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: ?, IsInvalid, IsImplicit) (Syntax: 'ptr')
");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocations[1],
                expectedSyntax: "c?.GetPtr()",
                expectedType: "delegate*<System.Void>",
                expectedConvertedType: "delegate*<System.Void>",
                expectedSymbol: null,
                expectedSymbolCandidates: null);

            VerifyOperationTreeForNode(comp, model, invocations[1], expectedOperationTree: @"
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: delegate*<System.Void>) (Syntax: 'c?.GetPtr()')
  Operation:
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  WhenNotNull:
    IInvocationOperation ( delegate*<System.Void> C.GetPtr()) (OperationKind.Invocation, Type: delegate*<System.Void>) (Syntax: '.GetPtr()')
      Instance Receiver:
        IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: C, IsImplicit) (Syntax: 'c')
      Arguments(0)");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocations[2].Parent!.Parent!,
                expectedSyntax: "(c?.GetPtr())()",
                expectedType: "System.Void",
                expectedSymbol: "delegate*<System.Void>",
                expectedSymbolCandidates: null);

            VerifyOperationTreeForNode(comp, model, invocations[2].Parent!.Parent!, expectedOperationTree: @"
IFunctionPointerInvocationOperation (OperationKind.FunctionPointerInvocation, Type: System.Void) (Syntax: '(c?.GetPtr())()')
  Target:
    IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: delegate*<System.Void>) (Syntax: 'c?.GetPtr()')
      Operation:
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
      WhenNotNull:
        IInvocationOperation ( delegate*<System.Void> C.GetPtr()) (OperationKind.Invocation, Type: delegate*<System.Void>) (Syntax: '.GetPtr()')
          Instance Receiver:
            IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: C, IsImplicit) (Syntax: 'c')
          Arguments(0)
  Arguments(0)");

        }

        [Fact]
        public void UnusedFunctionPointerAsResultOfQuestionDot()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
unsafe class C
{
    static string Print()
    {
        Console.WriteLine(""Print"");
        return string.Empty;
    }
    delegate*<string> GetPtr()
    {
        Console.WriteLine(""GetPtr"");
        return &Print;
    }

    static void Main()
    {
        C c = new C();
        c?.GetPtr();
        c = null;
        c?.GetPtr();
    }
}", options: TestOptions.UnsafeReleaseExe);

            var verifier = CompileAndVerifyFunctionPointers(comp, expectedOutput: "GetPtr");
            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       30 (0x1e)
  .maxstack  2
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000b
  IL_0008:  pop
  IL_0009:  br.s       IL_0011
  IL_000b:  call       ""delegate*<string> C.GetPtr()""
  IL_0010:  pop
  IL_0011:  ldnull
  IL_0012:  dup
  IL_0013:  brtrue.s   IL_0017
  IL_0015:  pop
  IL_0016:  ret
  IL_0017:  call       ""delegate*<string> C.GetPtr()""
  IL_001c:  pop
  IL_001d:  ret
}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var invocations = tree.GetRoot().DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().ToList();
            Assert.Equal(2, invocations.Count);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocations[0],
                expectedSyntax: "c?.GetPtr()",
                expectedType: "delegate*<System.String>",
                expectedSymbol: null,
                expectedSymbolCandidates: null);

            VerifyOperationTreeForNode(comp, model, invocations[0], expectedOperationTree: @"
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: delegate*<System.String>) (Syntax: 'c?.GetPtr()')
  Operation:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  WhenNotNull:
    IInvocationOperation ( delegate*<System.String> C.GetPtr()) (OperationKind.Invocation, Type: delegate*<System.String>) (Syntax: '.GetPtr()')
      Instance Receiver:
        IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: C, IsImplicit) (Syntax: 'c')
      Arguments(0)");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocations[1],
                expectedSyntax: "c?.GetPtr()",
                expectedType: "delegate*<System.String>",
                expectedSymbol: null,
                expectedSymbolCandidates: null);

            VerifyOperationTreeForNode(comp, model, invocations[1], expectedOperationTree: @"
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: delegate*<System.String>) (Syntax: 'c?.GetPtr()')
  Operation:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  WhenNotNull:
    IInvocationOperation ( delegate*<System.String> C.GetPtr()) (OperationKind.Invocation, Type: delegate*<System.String>) (Syntax: '.GetPtr()')
      Instance Receiver:
        IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: C, IsImplicit) (Syntax: 'c')
      Arguments(0)");
        }

        [Fact]
        public void FunctionPointerTypePatternIsNull()
        {
            var source = @"
using System;
unsafe class C
{
    static void Main()
    {
        delegate*<void> ptr = null;
        Console.WriteLine(ptr is null);
        Console.WriteLine(ptr is var v);
    }
}";

            var comp = CreateCompilationWithFunctionPointers(source, TestOptions.UnsafeReleaseExe);
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: @"
True
True");
            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (delegate*<void> V_0) //ptr
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  conv.u
  IL_0006:  ceq
  IL_0008:  call       ""void System.Console.WriteLine(bool)""
  IL_000d:  ldc.i4.1
  IL_000e:  call       ""void System.Console.WriteLine(bool)""
  IL_0013:  ret
}
");

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var isPatterns = syntaxTree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().ToArray();
            Assert.Equal(2, isPatterns.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, isPatterns[1].Pattern,
                expectedSyntax: "var v",
                expectedType: "delegate*<System.Void>",
                expectedSymbol: null,
                expectedSymbolCandidates: null);

            VerifyOperationTreeForNode(comp, model, isPatterns[0], expectedOperationTree: @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'ptr is null')
  Value: 
    ILocalReferenceOperation: ptr (OperationKind.LocalReference, Type: delegate*<System.Void>) (Syntax: 'ptr')
  Pattern: 
    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: delegate*<System.Void>, NarrowedType: delegate*<System.Void>)
      Value: 
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
");

            VerifyOperationTreeForNode(comp, model, isPatterns[1], expectedOperationTree: @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'ptr is var v')
  Value: 
    ILocalReferenceOperation: ptr (OperationKind.LocalReference, Type: delegate*<System.Void>) (Syntax: 'ptr')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var v') (InputType: delegate*<System.Void>, NarrowedType: delegate*<System.Void>, DeclaredSymbol: delegate*<System.Void> v, MatchesNull: True)
");

            comp = CreateCompilationWithFunctionPointers(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (7,9): error CS8370: Feature 'function pointers' is not available in C# 7.3. Please use language version 9.0 or greater.
                //         delegate*<void> ptr = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "delegate").WithArguments("function pointers", "9.0").WithLocation(7, 9));
        }

        [Fact]
        public void FunctionPointerTypePatternIsVarParenthesized()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
unsafe class C
{
    static void Main()
    {
        delegate*<void> ptr = null;
        _ = ptr is var (x);
    }
}");

            comp.VerifyDiagnostics(
                // (8,20): error CS8521: Pattern-matching is not permitted for pointer types.
                //         _ = ptr is var (x);
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "var (x)").WithLocation(8, 20)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var isPattern = syntaxTree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();
            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, isPattern.Pattern,
                expectedSyntax: "var (x)",
                expectedType: null,
                expectedConvertedType: null,
                expectedSymbol: null,
                expectedSymbolCandidates: null);

            VerifyOperationTreeForNode(comp, model, isPattern, expectedOperationTree: @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'ptr is var (x)')
  Value: 
    ILocalReferenceOperation: ptr (OperationKind.LocalReference, Type: delegate*<System.Void>) (Syntax: 'ptr')
  Pattern: 
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: '(x)') (InputType: ?, NarrowedType: ?, DeclaredSymbol: null, MatchedType: ?, DeconstructSymbol: null)
      DeconstructionSubpatterns (1):
          IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'x') (InputType: ?, NarrowedType: ?, DeclaredSymbol: ?? x, MatchesNull: True)
      PropertySubpatterns (0)
");
        }

        [Fact]
        public void FunctionPointerTypePatternRecursiveInput()
        {

            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<void> ptr)
    {
        _ = ptr is { } _;
    }
}");

            comp.VerifyDiagnostics(
                // (6,20): error CS8521: Pattern-matching is not permitted for pointer types.
                //         _ = ptr is { } _;
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "{ } _").WithLocation(6, 20)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var isPattern = syntaxTree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, isPattern.Pattern,
                expectedSyntax: "{ } _",
                expectedType: "?",
                expectedSymbol: null,
                expectedSymbolCandidates: null);

            VerifyOperationTreeForNode(comp, model, isPattern, expectedOperationTree: @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'ptr is { } _')
  Value: 
    IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.Void>) (Syntax: 'ptr')
  Pattern: 
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: '{ } _') (InputType: ?, NarrowedType: ?, DeclaredSymbol: null, MatchedType: ?, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (0)
");
        }

        [Fact]
        public void FunctionPointerTypeIsAsOperator()
        {

            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(object o, delegate*<void> ptr)
    {
        _ = o is delegate*<void>;
        _ = o as delegate*<void>;
        _ = ptr as object;
        _ = ptr is object;
    }
}");

            comp.VerifyDiagnostics(
                // (6,13): error CS0244: Neither 'is' nor 'as' is valid on pointer types
                //         _ = o is delegate*<void>;
                Diagnostic(ErrorCode.ERR_PointerInAsOrIs, "o is delegate*<void>").WithLocation(6, 13),
                // (7,13): error CS0244: Neither 'is' nor 'as' is valid on pointer types
                //         _ = o as delegate*<void>;
                Diagnostic(ErrorCode.ERR_PointerInAsOrIs, "o as delegate*<void>").WithLocation(7, 13),
                // (8,13): error CS0244: Neither 'is' nor 'as' is valid on pointer types
                //         _ = ptr as object;
                Diagnostic(ErrorCode.ERR_PointerInAsOrIs, "ptr as object").WithLocation(8, 13),
                // (9,13): error CS0244: Neither 'is' nor 'as' is valid on pointer types
                //         _ = ptr is object;
                Diagnostic(ErrorCode.ERR_PointerInAsOrIs, "ptr is object").WithLocation(9, 13)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var binaryExpressions = syntaxTree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().ToArray();
            Assert.Equal(4, binaryExpressions.Length);

            VerifyOperationTreeForNode(comp, model, binaryExpressions[0], expectedOperationTree: @"
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean, IsInvalid) (Syntax: 'o is delegate*<void>')
  Operand: 
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'o')
  IsType: delegate*<System.Void>
");

            VerifyOperationTreeForNode(comp, model, binaryExpressions[1], expectedOperationTree: @"
IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'o as delegate*<void>')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'o')
");

            VerifyOperationTreeForNode(comp, model, binaryExpressions[2], expectedOperationTree: @"
IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid) (Syntax: 'ptr as object')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'ptr')
");

            VerifyOperationTreeForNode(comp, model, binaryExpressions[3], expectedOperationTree: @"
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean, IsInvalid) (Syntax: 'ptr is object')
  Operand: 
    IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'ptr')
  IsType: System.Object
");
        }

        [Fact]
        public void FunctionPointerTypePatternRecursiveType()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public object O = null;
    void M(C c)
    {
        _ = c is { O: delegate*<void> _ };
    }
}");

            comp.VerifyDiagnostics(
                // (7,23): error CS8521: Pattern-matching is not permitted for pointer types.
                //         _ = c is { O: delegate*<void> _ };
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "delegate*<void>").WithLocation(7, 23)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var isPattern = syntaxTree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();
            var funcPtrTypeSyntax = isPattern.DescendantNodes().OfType<FunctionPointerTypeSyntax>().Single();
            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, funcPtrTypeSyntax,
                expectedSyntax: "delegate*<void>",
                expectedType: "delegate*<System.Void>",
                expectedSymbol: "delegate*<System.Void>");

            VerifyOperationTreeForNode(comp, model, isPattern, expectedOperationTree: @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'c is { O: d ... *<void> _ }')
  Value: 
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Pattern: 
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: '{ O: delegate*<void> _ }') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'O: delegate*<void> _')
            Member: 
              IFieldReferenceOperation: System.Object C.O (OperationKind.FieldReference, Type: System.Object) (Syntax: 'O')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'O')
            Pattern: 
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'delegate*<void> _') (InputType: System.Object, NarrowedType: delegate*<System.Void>, DeclaredSymbol: null, MatchesNull: False)
");
        }

        [Fact]
        public void FunctionPointerTypeNotPermittedInFixedInitializer()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    static void M() {}
    void M(C c)
    {
        fixed (delegate*<void> ptr = &M)
        {
        }
    }
}");

            comp.VerifyDiagnostics(
                // (7,32): error CS8789: The type of a local declared in a fixed statement cannot be a function pointer type.
                //         fixed (delegate*<void> ptr = &M)
                Diagnostic(ErrorCode.ERR_CannotUseFunctionPointerAsFixedLocal, "ptr = &M").WithLocation(7, 32)
            );
        }

        [Fact]
        public void NoUnusedLocalWarning()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C {
    void M() {
        delegate*<void> i = default;
    }
}");

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedInvalidTypes()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
#nullable enable
static class S {}
unsafe class C
{
    void M1(delegate*<C*, S> ptr) {}
    void M2<T>(delegate*<T?> ptr) {}
    void M3<T>(delegate*<D<T>> ptr) {}
    void M4<T>(delegate*<E<T>> ptr) {}
}
class D<T> where T : unmanaged {}
class E<T> where T : struct {}
");

            comp.VerifyDiagnostics(
                // (6,27): error CS0722: 'S': static types cannot be used as return types
                //     void M1(delegate*<C*, S> ptr) {}
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "S").WithArguments("S").WithLocation(6, 27),
                // (6,30): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('C')
                //     void M1(delegate*<C*, S> ptr) {}
                Diagnostic(ErrorCode.WRN_ManagedAddr, "ptr").WithArguments("C").WithLocation(6, 30),
                // (8,32): error CS8377: The type 'T' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     void M3<T>(delegate*<D<T>> ptr) {}
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "ptr").WithArguments("D<T>", "T", "T").WithLocation(8, 32),
                // (9,32): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'E<T>'
                //     void M4<T>(delegate*<E<T>> ptr) {}
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "ptr").WithArguments("E<T>", "T", "T").WithLocation(9, 32)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var paramTypes = syntaxTree.GetRoot().DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Select(m => m.ParameterList.Parameters.Single().Type!)
                .ToArray();

            Assert.Equal(4, paramTypes.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, paramTypes[0],
                expectedSyntax: "delegate*<C*, S>",
                expectedType: "delegate*<C*, S>",
                expectedSymbol: "delegate*<C*, S>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, paramTypes[1],
                expectedSyntax: "delegate*<T?>",
                expectedType: "delegate*<T?>",
                expectedSymbol: "delegate*<T?>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, paramTypes[2],
                expectedSyntax: "delegate*<D<T>>",
                expectedType: "delegate*<D<T>>",
                expectedSymbol: "delegate*<D<T>>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, paramTypes[3],
                expectedSyntax: "delegate*<E<T>>",
                expectedType: "delegate*<E<T>>",
                expectedSymbol: "delegate*<E<T>>");
        }

        [Fact]
        public void NewFunctionPointerType()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M()
    {
        _ = /*<bind>*/new delegate*<void>()/*</bind>*/;
    }
}");

            comp.VerifyDiagnostics(
                // (6,23): error CS1919: Unsafe type 'delegate*<void>' cannot be used in object creation
                //         _ = /*<bind>*/new delegate*<void>()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeTypeInObjectCreation, "new delegate*<void>()").WithArguments("delegate*<void>").WithLocation(6, 23)
            );

            VerifyOperationTreeForTest<ObjectCreationExpressionSyntax>(comp, expectedOperationTree: @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new delegate*<void>()')
  Children(0)
");
        }

        [Fact]
        public void IndexerAccessOnFunctionPointer()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<void> ptr)
    {
        _ = /*<bind>*/ptr[0]/*</bind>*/;
    }
}");

            comp.VerifyDiagnostics(
                // (6,23): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
                //         _ = /*<bind>*/ptr[0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "ptr[0]").WithArguments("delegate*<void>").WithLocation(6, 23)
            );

            VerifyOperationTreeForTest<ElementAccessExpressionSyntax>(comp, expectedOperationTree: @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'ptr[0]')
  Children(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
      IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'ptr')
");
        }

        [Fact]
        public void ClsCompliance()
        {

            var comp = CreateCompilationWithFunctionPointers(@"
using System;
[assembly: CLSCompliant(true)]
[CLSCompliant(true)]
public class C
{
    private unsafe void M1(delegate*<void> m) {}
    internal unsafe void M2(delegate*<void> m) {}
    public unsafe void M3(delegate*<void> m) {}
}");

            comp.VerifyDiagnostics(
                // (9,43): warning CS3001: Argument type 'delegate*<void>' is not CLS-compliant
                //     public unsafe void M3(delegate*<void> m) {}
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "m").WithArguments("delegate*<void>").WithLocation(9, 43)
            );
        }

        [Fact]
        public void CannotMakeFunctionPointerConst()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    const delegate*<void> field = null;
    public static void M()
    {
        const delegate*<void> local = null;
    }
}");

            comp.VerifyDiagnostics(
                // (4,5): error CS0283: The type 'delegate*<void>' cannot be declared const
                //     const delegate*<void> field = null;
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("delegate*<void>").WithLocation(4, 5),
                // (4,35): error CS0133: The expression being assigned to 'C.field' must be constant
                //     const delegate*<void> field = null;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "null").WithArguments("C.field").WithLocation(4, 35),
                // (7,15): error CS0283: The type 'delegate*<void>' cannot be declared const
                //         const delegate*<void> local = null;
                Diagnostic(ErrorCode.ERR_BadConstType, "delegate*<void>").WithArguments("delegate*<void>").WithLocation(7, 15)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            VariableDeclarationSyntax fieldDeclaration = syntaxTree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Single().Declaration;
            var fieldVariable = fieldDeclaration.Variables.Single();
            Assert.Equal("delegate*<System.Void> C.field", model.GetDeclaredSymbol(fieldVariable).ToTestDisplayString());

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, fieldDeclaration.Type,
                expectedSyntax: "delegate*<void>",
                expectedType: "delegate*<System.Void>",
                expectedSymbol: "delegate*<System.Void>");

            var localDeclaration = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Single().DescendantNodes()
                .OfType<VariableDeclarationSyntax>()
                .Single();

            var localVariable = localDeclaration.Variables.Single();

            Assert.Equal("delegate*<System.Void> local", model.GetDeclaredSymbol(localVariable).ToTestDisplayString());

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, localDeclaration.Type,
                expectedSyntax: "delegate*<void>",
                expectedType: "delegate*<System.Void>",
                expectedSymbol: "delegate*<System.Void>");
        }

        [Fact]
        public void FunctionPointerParameterDefaultValue()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
unsafe class C
{
    public static void Main()
    {
        M();
    }
    public static void M(delegate*<void> ptr = null)
    {
        Console.Write(ptr is null);
    }
}", options: TestOptions.UnsafeReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: "True", verify: Verification.Skipped);

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  call       ""void C.M(delegate*<void>)""
  IL_0007:  ret
}
");

            verifier.VerifyIL("C.M", expectedIL: @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  conv.u
  IL_0003:  ceq
  IL_0005:  call       ""void System.Console.Write(bool)""
  IL_000a:  ret
}
");
        }

        [Fact]
        public void MethodCallOnFunctionPointerType()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<void> ptr)
    {
        /*<bind>*/ptr.ToString()/*</bind>*/;
    }
}");

            comp.VerifyDiagnostics(
                // (6,23): error CS1061: 'delegate*<void>' does not contain a definition for 'ToString' and no accessible extension method 'ToString' accepting a first argument of type 'delegate*<void>' could be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/ptr.ToString()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "ToString").WithArguments("delegate*<void>", "ToString").WithLocation(6, 23)
            );

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(comp, expectedOperationTree: @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'ptr.ToString()')
  Children(1):
      IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.Void>) (Syntax: 'ptr')
");
        }

        [Fact]
        public void ArglistCannotBeUsedWithFunctionPointers()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    static void M(delegate*<string, int, void> ptr1, delegate*<__arglist, void> ptr2)
    /*<bind>*/{
        ptr1(__arglist(string.Empty, 1), 1);
        ptr1(null, __arglist(string.Empty, 1));
        ptr1(null, 1, __arglist(string.Empty, 1));
        ptr2(__arglist(1, 2, 3, ptr1));
    }/*</bind>*/
}");

            comp.VerifyDiagnostics(
                // (4,64): error CS1031: Type expected
                //     static void M(delegate*<string, int, void> ptr1, delegate*<__arglist, void> ptr2)
                Diagnostic(ErrorCode.ERR_TypeExpected, "__arglist").WithLocation(4, 64),
                // (4,64): error CS1003: Syntax error, ',' expected
                //     static void M(delegate*<string, int, void> ptr1, delegate*<__arglist, void> ptr2)
                Diagnostic(ErrorCode.ERR_SyntaxError, "__arglist").WithArguments(",").WithLocation(4, 64),
                // (6,14): error CS1503: Argument 1: cannot convert from '__arglist' to 'string'
                //         ptr1(__arglist(string.Empty, 1), 1);
                Diagnostic(ErrorCode.ERR_BadArgType, "__arglist(string.Empty, 1)").WithArguments("1", "__arglist", "string").WithLocation(6, 14),
                // (7,20): error CS1503: Argument 2: cannot convert from '__arglist' to 'int'
                //         ptr1(null, __arglist(string.Empty, 1));
                Diagnostic(ErrorCode.ERR_BadArgType, "__arglist(string.Empty, 1)").WithArguments("2", "__arglist", "int").WithLocation(7, 20),
                // (8,9): error CS8756: Function pointer 'delegate*<string, int, void>' does not take 3 arguments
                //         ptr1(null, 1, __arglist(string.Empty, 1));
                Diagnostic(ErrorCode.ERR_BadFuncPointerArgCount, "ptr1(null, 1, __arglist(string.Empty, 1))").WithArguments("delegate*<string, int, void>", "3").WithLocation(8, 9),
                // (9,14): error CS1503: Argument 1: cannot convert from '__arglist' to '?'
                //         ptr2(__arglist(1, 2, 3, ptr1));
                Diagnostic(ErrorCode.ERR_BadArgType, "__arglist(1, 2, 3, ptr1)").WithArguments("1", "__arglist", "?").WithLocation(9, 14)
            );

            var m = comp.GetTypeByMetadataName("C").GetMethod("M");

            Assert.Equal(2, m.ParameterCount);

            var type = (FunctionPointerTypeSymbol)m.Parameters[1].Type;
            VerifyFunctionPointerSymbol(type, CallingConvention.Default,
                (RefKind.None, IsVoidType()),
                (RefKind.None, IsErrorType()));

            Assert.False(type.Signature.IsVararg);

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var parameterDecls = syntaxTree.GetRoot().DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .SelectMany(m => m.ParameterList.Parameters)
                .Select(p => p.Type!)
                .ToArray();

            Assert.Equal(2, parameterDecls.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[0],
                expectedSyntax: "delegate*<string, int, void>",
                expectedType: "delegate*<System.String, System.Int32, System.Void>",
                expectedSymbol: "delegate*<System.String, System.Int32, System.Void>");

            var semanticInfo = model.GetSemanticInfoSummary(parameterDecls[1]);

            // Calling GetTypeInfo on `__arglist` returns null, so attempting to verify the individual parameters would fail
            Assert.Equal("delegate*<__arglist, void>", parameterDecls[1].ToString());
            Assert.Equal("delegate*<?, System.Void>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(semanticInfo.Type, semanticInfo.ConvertedType, SymbolEqualityComparer.IncludeNullability);
            Assert.Equal("delegate*<?, System.Void>", semanticInfo.Symbol.ToTestDisplayString(includeNonNullable: false));
            Assert.Empty(semanticInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);

            VerifyOperationTreeForTest<BlockSyntax>(comp, expectedOperationTree: @"
IBlockOperation (4 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'ptr1(__argl ... ty, 1), 1);')
    Expression: 
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'ptr1(__argl ... pty, 1), 1)')
        Children(3):
            IParameterReferenceOperation: ptr1 (OperationKind.ParameterReference, Type: delegate*<System.String, System.Int32, System.Void>) (Syntax: 'ptr1')
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '__arglist(s ... g.Empty, 1)')
              Children(2):
                  IFieldReferenceOperation: System.String System.String.Empty (Static) (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'string.Empty')
                    Instance Receiver: 
                      null
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'ptr1(null,  ... Empty, 1));')
    Expression: 
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'ptr1(null,  ... .Empty, 1))')
        Children(3):
            IParameterReferenceOperation: ptr1 (OperationKind.ParameterReference, Type: delegate*<System.String, System.Int32, System.Void>) (Syntax: 'ptr1')
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '__arglist(s ... g.Empty, 1)')
              Children(2):
                  IFieldReferenceOperation: System.String System.String.Empty (Static) (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'string.Empty')
                    Instance Receiver: 
                      null
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'ptr1(null,  ... Empty, 1));')
    Expression: 
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'ptr1(null,  ... .Empty, 1))')
        Children(4):
            IParameterReferenceOperation: ptr1 (OperationKind.ParameterReference, Type: delegate*<System.String, System.Int32, System.Void>, IsInvalid) (Syntax: 'ptr1')
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '__arglist(s ... g.Empty, 1)')
              Children(2):
                  IFieldReferenceOperation: System.String System.String.Empty (Static) (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'string.Empty')
                    Instance Receiver: 
                      null
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'ptr2(__argl ...  3, ptr1));')
    Expression: 
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'ptr2(__argl ... , 3, ptr1))')
        Children(2):
            IParameterReferenceOperation: ptr2 (OperationKind.ParameterReference, Type: delegate*<?, System.Void>) (Syntax: 'ptr2')
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '__arglist(1, 2, 3, ptr1)')
              Children(4):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
                  IParameterReferenceOperation: ptr1 (OperationKind.ParameterReference, Type: delegate*<System.String, System.Int32, System.Void>, IsInvalid) (Syntax: 'ptr1')
");
        }

        [Fact, WorkItem(44953, "https://github.com/dotnet/roslyn/issues/44953")]
        public void StaticClassInFunctionPointer()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe static class C
{
    static delegate*<C, C> Ptr;
}");

            comp.VerifyDiagnostics(
                // (4,22): error CS0721: 'C': static types cannot be used as parameters
                //     static delegate*<C, C> Ptr;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C").WithArguments("C").WithLocation(4, 22),
                // (4,25): error CS0722: 'C': static types cannot be used as return types
                //     static delegate*<C, C> Ptr;
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "C").WithArguments("C").WithLocation(4, 25),
                // (4,28): warning CS0169: The field 'C.Ptr' is never used
                //     static delegate*<C, C> Ptr;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Ptr").WithArguments("C.Ptr").WithLocation(4, 28)
            );
        }

        [ConditionalFact(typeof(CoreClrOnly)), WorkItem(44953, "https://github.com/dotnet/roslyn/issues/44953")]
        public void RestrictedTypeInFunctionPointer()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
unsafe static class C
{
    static delegate*<ArgIterator, ref ArgIterator, ArgIterator> Ptr;
}", targetFramework: TargetFramework.NetCoreApp);

            comp.VerifyDiagnostics(
                // (5,35): error CS1601: Cannot make reference to variable of type 'ArgIterator'
                //     static delegate*<ArgIterator, ref ArgIterator, ArgIterator> Ptr;
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "ref ArgIterator").WithArguments("System.ArgIterator").WithLocation(5, 35),
                // (5,52): error CS1599: The return type of a method, delegate, or function pointer cannot be 'System.ArgIterator'
                //     static delegate*<ArgIterator, ref ArgIterator, ArgIterator> Ptr;
                Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "ArgIterator").WithArguments("System.ArgIterator").WithLocation(5, 52),
                // (5,65): warning CS0169: The field 'C.Ptr' is never used
                //     static delegate*<ArgIterator, ref ArgIterator, ArgIterator> Ptr;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Ptr").WithArguments("C.Ptr").WithLocation(5, 65)
            );
        }

        [Fact, WorkItem(46688, "https://github.com/dotnet/roslyn/issues/46688")]
        public void NewAfterPtrDeclaration()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M1()
    {
        delegate*<void> ptr = new () => {};
    }

    void M2()
    {
        delegate*<void> ptr = new();
    }
}
");
            comp.VerifyDiagnostics(
                // (6,38): error CS1003: Syntax error, ',' expected
                //         delegate*<void> ptr = new () => {};
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(6, 38),
                // (6,41): error CS1002: ; expected
                //         delegate*<void> ptr = new () => {};
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(6, 41),
                // (11,31): error CS1919: Unsafe type 'delegate*<void>' cannot be used in object creation
                //         delegate*<void> ptr = new();
                Diagnostic(ErrorCode.ERR_UnsafeTypeInObjectCreation, "new()").WithArguments("delegate*<void>").WithLocation(11, 31)
            );
        }

        [Fact, WorkItem(48071, "https://github.com/dotnet/roslyn/issues/48071")]
        public void FunctionPointerCalledWithNamedArguments()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
public class C
{
    public unsafe void M(delegate*<string, int, void> ptr)
    {
        ptr(""a"", arg1: 1);
    }
}
");
            comp.VerifyDiagnostics(
                // (6,18): error CS8904: A function pointer cannot be called with named arguments.
                //         ptr("a", arg1: 1);
                Diagnostic(ErrorCode.ERR_FunctionPointersCannotBeCalledWithNamedArguments, "arg1").WithLocation(6, 18)
            );
        }

        [Fact, WorkItem(48071, "https://github.com/dotnet/roslyn/issues/48071")]
        public void FunctionPointerCalledWithNamedArguments2()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
public class C
{
    public unsafe void M(delegate*<string, int, void> ptr)
    {
        ptr(arg0: ""a"", arg1: 1);
    }
}
");
            comp.VerifyDiagnostics(
                // (6,13): error CS8904: A function pointer cannot be called with named arguments.
                //         ptr(arg0: "a", arg1: 1);
                Diagnostic(ErrorCode.ERR_FunctionPointersCannotBeCalledWithNamedArguments, "arg0").WithLocation(6, 13)
            );
        }

        [Fact, WorkItem(53973, "https://github.com/dotnet/roslyn/issues/53973")]
        public void FunctionPointerNullable()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
public class C
{
    
    public unsafe void M(delegate*<void>? f) {
    }
}
");
            comp.VerifyDiagnostics(
                // (5,43): error CS0306: The type 'delegate*<void>' may not be used as a type argument
                //     public unsafe void M(delegate*<void>? f) {
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "f").WithArguments("delegate*<void>").WithLocation(5, 43)
            );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75933")]
        public void BoolLiteralInvocation_01()
        {
            var source = """
                #nullable enable

                class C
                {
                    static unsafe void M()
                    {
                        int i = 0;
                        ((delegate*<int, void>)true)(i);
                    }
                }
                """;

            var comp = CreateCompilationWithFunctionPointers(source);
            comp.VerifyEmitDiagnostics(
                // (8,10): error CS0030: Cannot convert type 'bool' to 'delegate*<int, void>'
                //         ((delegate*<int, void>)true)(i);
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(delegate*<int, void>)true").WithArguments("bool", "delegate*<int, void>").WithLocation(8, 10)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75933")]
        public void BoolLiteralInvocation_02()
        {
            var source = """
                #nullable enable

                using System;

                class C
                {
                    static unsafe void M()
                    {
                        var d = delegate (object? o, IntPtr f) { return ((delegate* managed<object?, long>)false)(o); };
                    }
                }
                """;

            var comp = CreateCompilationWithFunctionPointers(source);
            comp.VerifyEmitDiagnostics(
                // (9,58): error CS0030: Cannot convert type 'bool' to 'delegate*<object?, long>'
                //         var d = delegate (object? o, IntPtr f) { return ((delegate* managed<object?, long>)false)(o); };
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(delegate* managed<object?, long>)false").WithArguments("bool", "delegate*<object?, long>").WithLocation(9, 58)
                );
        }
    }
}
