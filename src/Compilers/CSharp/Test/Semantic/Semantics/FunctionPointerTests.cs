
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FunctionPointerTests : CompilingTestBase
    {
        private CSharpCompilation CreateCompilationWithFunctionPointers(string source)
        {
            return CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
        }

        private CompilationVerifier CompileAndVerifyFunctionPointers(CSharpCompilation compilation)
        {
            return CompileAndVerify(compilation, verify: Verification.Skipped);
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
  IL_0011:  call       ""delegate*<string,int> C.M3()""
  IL_0016:  stloc.2
  IL_0017:  ret
}
            ");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var initializer1 = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Initializer!.Value;
            assertResult(model, initializer1);
            var parameter = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First().ArgumentList.Arguments.Single();
            assertResult(model, parameter.Expression);
            var initializer2 = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Last().Initializer!.Value;
            assertResult(model, initializer2);

            static void assertResult(SemanticModel model, ExpressionSyntax initializer1)
            {
                var typeInfo = model.GetTypeInfo(initializer1);
                Assert.True(typeInfo.ConvertedType is IPointerTypeSymbol { PointedAtType: { SpecialType: SpecialType.System_Void } });
                Assert.Equal(TypeKind.FunctionPointer, typeInfo.Type!.TypeKind);
                var conversion = model.GetConversion(initializer1);
                Assert.Equal(ConversionKind.PointerToVoid, conversion.Kind);
            }
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
        delegate* cdecl<void> ptr2 = null;
        delegate*<string> ptr3 = null;
        delegate*<C, int> ptr4 = null;
    }
}");
            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  pop
  IL_0003:  ldc.i4.0
  IL_0004:  conv.u
  IL_0005:  pop
  IL_0006:  ldc.i4.0
  IL_0007:  conv.u
  IL_0008:  pop
  IL_0009:  ldc.i4.0
  IL_000a:  conv.u
  IL_000b:  pop
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
                Assert.Equal(ConversionKind.NullToPointer, conversion.Kind);
            }
        }

        [Theory]
        [InlineData("sbyte", "conv.i1", "conv.ovf.i1")]
        [InlineData("byte", "conv.u1", "conv.ovf.u1")]
        [InlineData("short", "conv.i2", "conv.ovf.i2")]
        [InlineData("ushort", "conv.u2", "conv.ovf.u2")]
        [InlineData("int", "conv.i4", "conv.ovf.i4")]
        [InlineData("uint", "conv.u4", "conv.ovf.u4")]
        [InlineData("long", "conv.u8", "conv.ovf.i8.un")]
        [InlineData("ulong", "conv.u8", "conv.u8")]
        public void FunctionPointerToNumericConversions(string type, string convKind, string checkedKind)
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
  IL_0001:  {convKind}
  IL_0002:  pop
  IL_0003:  ldarg.1
  IL_0004:  {convKind}
  IL_0005:  pop
  IL_0006:  ldarg.1
  IL_0007:  {checkedKind}
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
            Assert.Equal(ConversionKind.PointerToInteger, conversion.Kind);

            typeInfoOuter = model.GetTypeInfo(conversions[1]);
            conversion = model.ClassifyConversion(conversions[1].Expression, typeInfoOuter.Type!);
            Assert.Equal(ConversionKind.ExplicitNullable, conversion.Kind);
            Assert.Equal(ConversionKind.PointerToInteger, conversion.UnderlyingConversions.Single().Kind);
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
  IL_0000:  ldarg.1
  IL_0001:  call       ""System.{type} System.{type}.op_Explicit(void*)""
  IL_0006:  call       ""void* System.{type}.op_Explicit(System.{type})""
  IL_000b:  pop
  IL_000c:  ret
}}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var conversions = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().ToList();
            Assert.Equal(2, conversions.Count);

            var typeInfoOuter = model.GetTypeInfo(conversions[0]);
            var conversion = model.ClassifyConversion(conversions[0].Expression, typeInfoOuter.Type!);
            Assert.Equal(ConversionKind.ExplicitUserDefined, conversion.Kind);
            Assert.Equal(ConversionKind.PointerToVoid, conversion.UserDefinedFromConversion.Kind);

            typeInfoOuter = model.GetTypeInfo(conversions[1]);
            conversion = model.ClassifyConversion(conversions[1].Expression, typeInfoOuter.Type!);
            Assert.Equal(ConversionKind.ExplicitUserDefined, conversion.Kind);
            Assert.Equal(ConversionKind.PointerToPointer, conversion.UserDefinedToConversion.Kind);
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
  IL_0000:  ldarg.1
  IL_0001:  {convKind}
  IL_0002:  pop
  IL_0003:  ldarg.1
  IL_0004:  {checkedKind}
  IL_0005:  pop
  IL_0006:  ret
}}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var conversions = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().ToList();
            Assert.Equal(2, conversions.Count);

            var typeInfoOuter = model.GetTypeInfo(conversions[0]);
            var conversion = model.ClassifyConversion(conversions[0].Expression, typeInfoOuter.Type!);
            Assert.Equal(ConversionKind.IntegerToPointer, conversion.Kind);
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
                Assert.Equal(ConversionKind.PointerToPointer, conversion.Kind);
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
        public void UserDefinedConversion()
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
            }
        }

        [Fact]
        public void FunctionPointerToFunctionPointerValid_ReferenceVarianceAndIdentity()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<object, ref int, string> param1, delegate*<object, ref int> param2, delegate*<object, void> param3)
    {
        delegate*<string, ref int, object> ptr1 = param1;
        delegate*<string, ref int> ptr2 = param2;
        delegate*<string, void> ptr3 = param3;
    }
}");

            var verifier = CompileAndVerifyFunctionPointers(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  pop
  IL_0002:  ldarg.2
  IL_0003:  pop
  IL_0004:  ldarg.3
  IL_0005:  pop
  IL_0006:  ret
}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(d => d.Initializer!.Value).ToList();
            Assert.Equal(3, decls.Count);

            foreach (var decl in decls)
            {
                var conversion = model.GetConversion(decl);
                Assert.Equal(ConversionKind.ImplicitPointer, conversion.Kind);
                Assert.True(conversion.IsImplicit);
                Assert.True(conversion.IsPointer);
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
  IL_0000:  ldarg.1
  IL_0001:  pop
  IL_0002:  ret
}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decl = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(d => d.Initializer!.Value).Single();
            var conversion = model.GetConversion(decl);
            Assert.Equal(ConversionKind.ImplicitPointer, conversion.Kind);
            Assert.True(conversion.IsImplicit);
            Assert.True(conversion.IsPointer);
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
  IL_0000:  ldarg.1
  IL_0001:  pop
  IL_0002:  ret
}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decl = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(d => d.Initializer!.Value).Single();
            var conversion = model.GetConversion(decl);
            Assert.Equal(ConversionKind.ImplicitPointer, conversion.Kind);
            Assert.True(conversion.IsImplicit);
            Assert.True(conversion.IsPointer);
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
                // (6,40): error CS0266: Cannot implicitly convert type 'delegate*<string,string,void>' to 'delegate*<string,void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<string, void> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<string,string,void>", "delegate*<string,void>").WithLocation(6, 40),
                // (7,56): error CS0266: Cannot implicitly convert type 'delegate*<string,string,void>' to 'delegate*<string,string,string,void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<string, string, string, void> ptr2 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<string,string,void>", "delegate*<string,string,string,void>").WithLocation(7, 56)
            );
        }

        [Fact]
        public void FunctionPointerToFunctionPointerInvalid_ParameterVariance()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<ref object, void> param1)
    {
        delegate*<in object, void> ptr1 = param1;
        delegate*<object, void> ptr2 = param1;
        delegate*<ref string, void> ptr3 = param1;
    }
}");

            comp.VerifyDiagnostics(
                // (6,43): error CS0266: Cannot implicitly convert type 'delegate*<ref object,void>' to 'delegate*<in object,void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<in object, void> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<ref object,void>", "delegate*<in object,void>").WithLocation(6, 43),
                // (7,40): error CS0266: Cannot implicitly convert type 'delegate*<ref object,void>' to 'delegate*<object,void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<object, void> ptr2 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<ref object,void>", "delegate*<object,void>").WithLocation(7, 40),
                // (8,44): error CS0266: Cannot implicitly convert type 'delegate*<ref object,void>' to 'delegate*<ref string,void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<ref string, void> ptr3 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<ref object,void>", "delegate*<ref string,void>").WithLocation(8, 44)
            );
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
                // (6,47): error CS0266: Cannot implicitly convert type 'delegate*<string>' to 'delegate*<string>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<ref readonly string> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<string>", "delegate*<string>").WithLocation(6, 47),
                // (7,34): error CS0266: Cannot implicitly convert type 'delegate*<string>' to 'delegate*<string>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<string> ptr2 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<string>", "delegate*<string>").WithLocation(7, 34),
                // (8,34): error CS0266: Cannot implicitly convert type 'delegate*<string>' to 'delegate*<object>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<object> ptr3 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<string>", "delegate*<object>").WithLocation(8, 34)
            );
        }

        [Fact]
        public void FunctionPointerToFunctionPointerInvalid_CallingConvention()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M(delegate*<void> param1)
    {
        delegate* cdecl<void> ptr1 = param1;
        delegate* thiscall<void> ptr2 = param1;
        delegate* stdcall<void> ptr3 = param1;
    }
}");

            comp.VerifyDiagnostics(
                // (6,38): error CS0266: Cannot implicitly convert type 'delegate*<void>' to 'delegate*<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate* cdecl<void> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<void>", "delegate*<void>").WithLocation(6, 38),
                // (7,41): error CS0266: Cannot implicitly convert type 'delegate*<void>' to 'delegate*<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate* thiscall<void> ptr2 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<void>", "delegate*<void>").WithLocation(7, 41),
                // (8,40): error CS0266: Cannot implicitly convert type 'delegate*<void>' to 'delegate*<void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate* stdcall<void> ptr3 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<void>", "delegate*<void>").WithLocation(8, 40)
            );
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
                // (6,42): error CS0266: Cannot implicitly convert type 'delegate*<int,object>' to 'delegate*<object,string>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<object, string> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<int,object>", "delegate*<object,string>").WithLocation(6, 42),
                // (7,39): error CS0266: Cannot implicitly convert type 'delegate*<int,object>' to 'delegate*<int,string>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<int, string> ptr2 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<int,object>", "delegate*<int,string>").WithLocation(7, 39)
            );
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
                // (6,57): error CS0266: Cannot implicitly convert type 'delegate*<delegate*<object,void>,void>' to 'delegate*<delegate*<string,void>,void>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<delegate*<string, void>, void> ptr1 = param1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param1").WithArguments("delegate*<delegate*<object,void>,void>", "delegate*<delegate*<string,void>,void>").WithLocation(6, 57),
                // (7,45): error CS0266: Cannot implicitly convert type 'delegate*<delegate*<object>>' to 'delegate*<delegate*<string>>'. An explicit conversion exists (are you missing a cast?)
                //         delegate*<delegate*<string>> ptr2 = param2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "param2").WithArguments("delegate*<delegate*<object>>", "delegate*<delegate*<string>>").WithLocation(7, 45)
            );
        }
    }
}
