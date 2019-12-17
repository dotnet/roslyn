// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FunctionPointerTypeSymbolTests : CSharpTestBase
    {
        private void CommonVerifyFunctionPointer(FunctionPointerTypeSymbol symbol)
        {
            Assert.Equal(SymbolKind.FunctionPointer, symbol.Kind);
            Assert.Equal(TypeKind.FunctionPointer, symbol.TypeKind);
            Assert.NotNull(symbol.Signature);
            Assert.Equal(MethodKind.FunctionPointerSignature, symbol.Signature.MethodKind);
        }

        [InlineData("", RefKind.None)]
        [InlineData("ref", RefKind.Ref)]
        [InlineData("ref readonly", RefKind.RefReadOnly)]
        [Theory]
        public void ValidReturnModifiers(string modifier, RefKind expectedKind)
        {
            var comp = CreateCompilation($@"
class C
{{
    void M(delegate*<{modifier} object> p) {{}}

}}", parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var pointerType = (FunctionPointerTypeSymbol)m.Parameters.Single().Type;
            CommonVerifyFunctionPointer(pointerType);
            Assert.Equal(expectedKind, pointerType.Signature.RefKind);
            Assert.Equal(SpecialType.System_Object, pointerType.Signature.ReturnType.SpecialType);
            Assert.Empty(pointerType.Signature.Parameters);
        }

        [Fact]
        public void InvalidReturnModifiers()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(
        delegate*<readonly string> p1,
        delegate*<readonly ref string> p2,
        delegate*<ref ref readonly string> p3,
        delegate*<ref readonly readonly string> p4,
        delegate*<this string> p5,
        delegate*<params string> p6,
        delegate*<ref ref string> p7)
    {}
}
", parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                    // (5,19): error CS8753: 'readonly' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<readonly string> p1,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "readonly").WithArguments("readonly").WithLocation(5, 19),
                    // (6,19): error CS8753: 'readonly' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<readonly ref string> p2,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "readonly").WithArguments("readonly").WithLocation(6, 19),
                    // (7,23): error CS8754: A return type can only have one 'ref' modifier.
                    //         delegate*<ref ref readonly string> p3,
                    Diagnostic(ErrorCode.ERR_DupReturnTypeMod, "ref").WithArguments("ref").WithLocation(7, 23),
                    // (7,27): error CS8753: 'readonly' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<ref ref readonly string> p3,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "readonly").WithArguments("readonly").WithLocation(7, 27),
                    // (8,32): error CS8753: 'readonly' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<ref readonly readonly string> p4,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "readonly").WithArguments("readonly").WithLocation(8, 32),
                    // (9,19): error CS8753: 'this' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<this string> p5,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "this").WithArguments("this").WithLocation(9, 19),
                    // (10,19): error CS8753: 'params' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<params string> p6,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "params").WithArguments("params").WithLocation(10, 19),
                    // (11,23): error CS8754: A return type can only have one 'ref' modifier.
                    //         delegate*<ref ref string> p7)
                    Diagnostic(ErrorCode.ERR_DupReturnTypeMod, "ref").WithArguments("ref").WithLocation(11, 23));
        }

        [Fact]
        public void InvalidModifiersOnVoidReturnType()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(
        delegate*<ref void> p1,
        delegate*<ref readonly void> p2) {}
}", parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                    // (5,19): error CS1547: Keyword 'void' cannot be used in this context
                    //         delegate*<ref void> p1,
                    Diagnostic(ErrorCode.ERR_NoVoidHere, "ref void").WithLocation(5, 19),
                    // (6,19): error CS1547: Keyword 'void' cannot be used in this context
                    //         delegate*<ref readonly void> p2) {}
                    Diagnostic(ErrorCode.ERR_NoVoidHere, "ref readonly void").WithLocation(6, 19));

            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var firstSignature = ((FunctionPointerTypeSymbol)m.Parameters[0].Type).Signature;
            var secondSignature = ((FunctionPointerTypeSymbol)m.Parameters[1].Type).Signature;

            Assert.Equal(RefKind.Ref, firstSignature.RefKind);
            Assert.Equal(RefKind.RefReadOnly, secondSignature.RefKind);
        }

        [InlineData("", CallingConvention.Default)]
        [InlineData("managed", CallingConvention.Default)]
        [InlineData("cdecl", CallingConvention.C)]
        [InlineData("stdcall", CallingConvention.Standard)]
        [InlineData("thiscall", CallingConvention.ThisCall)]
        // PROTOTYPE(func-ptr): unmanaged
        [Theory]
        internal void ValidCallingConventions(string convention, CallingConvention expectedConvention)
        {
            var comp = CreateCompilation($@"
class C
{{
    public void M(delegate* {convention}<string> p) {{}}
}}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var pointerType = (FunctionPointerTypeSymbol)m.Parameters.Single().Type;
            CommonVerifyFunctionPointer(pointerType);
            Assert.Equal(expectedConvention, pointerType.Signature.CallingConvention);
            Assert.Equal(SpecialType.System_String, pointerType.Signature.ReturnType.SpecialType);
        }

        [Fact]
        public void InvalidCallingConventions()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M(delegate* invalid<void> p) {}
}", parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                    // (4,29): error CS8752: 'invalid' is not a valid calling convention for a function pointer. Valid conventions are 'cdecl', 'managed', 'thiscall', and 'stdcall'.
                    //     public void M(delegate* invalid<void> p) {}
                    Diagnostic(ErrorCode.ERR_InvalidFunctionPointerCallingConvention, "invalid").WithArguments("invalid").WithLocation(4, 29));

            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var pointerType = (FunctionPointerTypeSymbol)m.Parameters.Single().Type;
            CommonVerifyFunctionPointer(pointerType);
            Assert.Equal(CallingConvention.Invalid, pointerType.Signature.CallingConvention);
        }

        [Fact]
        public void Parameters()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M<T>(
        delegate*<int, void> p1,
        delegate*<object, void> p2,
        delegate*<C, void> p3,
        delegate*<object, object, void> p4,
        delegate*<T, object, void> p5,
        delegate*<delegate*<T>, void> p6) {}
}", parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var t = m.TypeParameters.Single();
            var parameterTypes = m.Parameters;

            var firstParam = getParam(0);
            Assert.Equal(SpecialType.System_Int32, firstParam.Parameters.Single().Type.SpecialType);

            var secondParam = getParam(1);
            Assert.Equal(SpecialType.System_Object, secondParam.Parameters.Single().Type.SpecialType);

            var thirdParam = getParam(2);
            Assert.Equal(c, thirdParam.Parameters.Single().Type);

            var fourthParam = getParam(3);
            Assert.Equal(2, fourthParam.ParameterCount);
            Assert.Equal(SpecialType.System_Object, fourthParam.Parameters[0].Type.SpecialType);
            Assert.Equal(SpecialType.System_Object, fourthParam.Parameters[1].Type.SpecialType);

            var fifthParam = getParam(4);
            Assert.Equal(2, fifthParam.ParameterCount);
            Assert.Equal(t, fifthParam.Parameters[0].Type);
            Assert.Equal(SpecialType.System_Object, fifthParam.Parameters[1].Type.SpecialType);

            var sixthParam = getParam(5);
            var sixthParamParam = ((FunctionPointerTypeSymbol)sixthParam.Parameters.Single().Type).Signature;
            Assert.Equal(t, sixthParamParam.ReturnType);
            Assert.Empty(sixthParamParam.Parameters);

            MethodSymbol getParam(int index)
            {
                var type = ((FunctionPointerTypeSymbol)parameterTypes[index].Type);
                CommonVerifyFunctionPointer(type);
                Assert.True(type.Signature.ReturnsVoid);
                return type.Signature;
            }
        }

        [Fact]
        public void ValidParameterModifiers()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M(
        delegate*<ref string, void> p1,
        delegate*<in string, void> p2,
        delegate*<out string, void> p3,
        delegate*<string, void> p4) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();


            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var parameterTypes = m.Parameters;

            var firstParam = getParam(0);
            Assert.Equal(RefKind.Ref, firstParam.Parameters.Single().RefKind);

            var secondParam = getParam(1);
            Assert.Equal(RefKind.In, secondParam.Parameters.Single().RefKind);

            var thirdParam = getParam(2);
            Assert.Equal(RefKind.Out, thirdParam.Parameters.Single().RefKind);

            var fourthParam = getParam(3);
            Assert.Equal(RefKind.None, fourthParam.Parameters.Single().RefKind);

            MethodSymbol getParam(int index)
            {
                var type = ((FunctionPointerTypeSymbol)parameterTypes[index].Type);
                CommonVerifyFunctionPointer(type);
                return type.Signature;
            }
        }

        [Fact]
        public void InvalidParameterModifiers()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M(
        delegate*<params string[], void> p1,
        delegate*<this string, void> p2,
        delegate*<readonly ref string, void> p3,
        delegate*<in out string, void> p4,
        delegate*<in ref string, void> p5,
        delegate*<out ref string, void> p6) {}
        
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics(
                    // (5,19): error CS8755: 'params' cannot be used as a modifier on a function pointer parameter.
                    //         delegate*<params string[], void> p1,
                    Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "params").WithArguments("params").WithLocation(5, 19),
                    // (6,19): error CS0027: Keyword 'this' is not available in the current context
                    //         delegate*<this string, void> p2,
                    Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(6, 19),
                    // (7,19): error CS8755: 'readonly' cannot be used as a modifier on a function pointer parameter.
                    //         delegate*<readonly ref string, void> p3,
                    Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "readonly").WithArguments("readonly").WithLocation(7, 19),
                    // (8,22): error CS8328:  The parameter modifier 'out' cannot be used with 'in'
                    //         delegate*<in out string, void> p4,
                    Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "in").WithLocation(8, 22),
                    // (9,22): error CS8328:  The parameter modifier 'ref' cannot be used with 'in'
                    //         delegate*<in ref string, void> p5,
                    Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "in").WithLocation(9, 22),
                    // (10,23): error CS8328:  The parameter modifier 'ref' cannot be used with 'out'
                    //         delegate*<out ref string, void> p6) {}
                    Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "out").WithLocation(10, 23));
        }

        [Fact]
        public void VoidInAsParameterType()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<void, void> p1) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var signature = ((FunctionPointerTypeSymbol)m.Parameters.Single().Type).Signature;
            Assert.True(signature.Parameters.Single().Type.IsVoidType());
        }
    }
}
