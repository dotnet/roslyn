// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

using System.Linq;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FunctionPointerTypeSymbolTests : CSharpTestBase
    {
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
            FunctionPointerUtilities.CommonVerifyFunctionPointer(pointerType);
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
        [InlineData("cdecl", CallingConvention.CDecl)]
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
            FunctionPointerUtilities.CommonVerifyFunctionPointer(pointerType);
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
            FunctionPointerUtilities.CommonVerifyFunctionPointer(pointerType);
            Assert.Equal(CallingConvention.Default, pointerType.Signature.CallingConvention);
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
                FunctionPointerUtilities.CommonVerifyFunctionPointer(type);
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
                FunctionPointerUtilities.CommonVerifyFunctionPointer(type);
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
        delegate*<out in string, void> p5,
        delegate*<in ref string, void> p6,
        delegate*<ref in string, void> p7,
        delegate*<out ref string, void> p8,
        delegate*<ref out string, void> p9) {}
        
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
                    // (9,23): error CS8328:  The parameter modifier 'in' cannot be used with 'out'
                    //         delegate*<out in string, void> p5,
                    Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "out").WithLocation(9, 23),
                    // (10,22): error CS8328:  The parameter modifier 'ref' cannot be used with 'in'
                    //         delegate*<in ref string, void> p6,
                    Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "in").WithLocation(10, 22),
                    // (11,23): error CS8328:  The parameter modifier 'in' cannot be used with 'ref'
                    //         delegate*<ref in string, void> p7,
                    Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(11, 23),
                    // (12,23): error CS8328:  The parameter modifier 'ref' cannot be used with 'out'
                    //         delegate*<out ref string, void> p8,
                    Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "out").WithLocation(12, 23),
                    // (13,23): error CS8328:  The parameter modifier 'out' cannot be used with 'ref'
                    //         delegate*<ref out string, void> p9) {}
                    Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(13, 23));

            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var parameterTypes = m.Parameters;

            var firstParam = getParam(0);
            Assert.Equal(RefKind.None, firstParam.Parameters.Single().RefKind);

            var secondParam = getParam(1);
            Assert.Equal(RefKind.None, secondParam.Parameters.Single().RefKind);

            var thirdParam = getParam(2);
            Assert.Equal(RefKind.Ref, thirdParam.Parameters.Single().RefKind);

            var fourthParam = getParam(3);
            Assert.Equal(RefKind.In, fourthParam.Parameters.Single().RefKind);

            var fifthParam = getParam(4);
            Assert.Equal(RefKind.Out, fifthParam.Parameters.Single().RefKind);

            var sixthParam = getParam(5);
            Assert.Equal(RefKind.In, sixthParam.Parameters.Single().RefKind);

            var seventhParam = getParam(6);
            Assert.Equal(RefKind.Ref, seventhParam.Parameters.Single().RefKind);

            var eightParam = getParam(7);
            Assert.Equal(RefKind.Out, eightParam.Parameters.Single().RefKind);

            var ninthParam = getParam(8);
            Assert.Equal(RefKind.Ref, ninthParam.Parameters.Single().RefKind);

            MethodSymbol getParam(int index)
            {
                var type = ((FunctionPointerTypeSymbol)parameterTypes[index].Type);
                FunctionPointerUtilities.CommonVerifyFunctionPointer(type);
                return type.Signature;
            }
        }

        [Fact]
        public void VoidAsParameterType()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<void, void> p1) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics(
                    // (4,22): error CS1536: Invalid parameter type 'void'
                    //     void M(delegate*<void, void> p1) {}
                    Diagnostic(ErrorCode.ERR_NoVoidParameter, "void").WithLocation(4, 22));

            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var signature = ((FunctionPointerTypeSymbol)m.Parameters.Single().Type).Signature;
            Assert.True(signature.Parameters.Single().Type.IsVoidType());
        }

        [Fact]
        public void Equality_ReturnVoid()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<void> p1,
           delegate*<void> p2) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type, returnEquality: Equality.Equal, callingConventionEquality: Equality.Equal);
        }

        [Fact]
        public void EqualityDifferingNullability()
        {
            var comp = CreateCompilation(@"
#nullable enable
class C
{
    void M(delegate*<string, string, string?> p1,
           delegate*<string, string?, string> p2) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type,
                returnEquality: Equality.DifferingNullability, callingConventionEquality: Equality.Equal,
                Equality.Equal, Equality.DifferingNullability);

        }

        [Fact]
        public void EqualityMultipleParameters()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<string, object, C, int, void> p1,
           delegate*<string, object, C, int, void> p2) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type,
                returnEquality: Equality.Equal, callingConventionEquality: Equality.Equal,
                Equality.Equal, Equality.Equal, Equality.Equal, Equality.Equal);
        }

        [Fact]
        public void EqualityNestedFunctionPointers()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<delegate*<string, object>, delegate*<C, int>> p1,
           delegate*<delegate*<string, object>, delegate*<C, int>> p2) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type,
                returnEquality: Equality.Equal, callingConventionEquality: Equality.Equal,
                Equality.Equal);
        }

        [Fact]
        public void EqualityNestedFunctionPointersDifferingNullability()
        {
            var comp = CreateCompilation(@"
#nullable enable
class C
{
    void M(delegate*<delegate*<string, object?>, delegate*<C?, int>> p1,
           delegate*<delegate*<string?, object>, delegate*<C, int>> p2) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type,
                returnEquality: Equality.DifferingNullability, callingConventionEquality: Equality.Equal,
                Equality.DifferingNullability);
        }

        [Fact]
        public void Equality_ReturnNotEqual()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<string, object, C, int, string> p1,
           delegate*<string, object, C, int, void> p2) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type,
                returnEquality: Equality.NotEqual, callingConventionEquality: Equality.Equal,
                Equality.Equal, Equality.Equal, Equality.Equal, Equality.Equal);
        }

        [Fact]
        public void Equality_ParameterTypeNotEqual()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<string, object, C, object, void> p1,
           delegate*<string, object, C, int, void> p2) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type,
                returnEquality: Equality.Equal, callingConventionEquality: Equality.Equal,
                Equality.Equal, Equality.Equal, Equality.Equal, Equality.NotEqual);
        }

        [Fact]
        public void Equality_CallingConvetionNotEqual()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate* cdecl<string, object, C, object, void> p1,
           delegate* thiscall<string, object, C, object, void> p2) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type,
                returnEquality: Equality.Equal, callingConventionEquality: Equality.NotEqual,
                Equality.Equal, Equality.Equal, Equality.Equal, Equality.Equal);
        }

        [Fact]
        public void Equality_ParameterModifiersDifferent()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<ref string, object, C, object, void> p1,
           delegate*<string, in object, out C, object, void> p2) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type,
                returnEquality: Equality.Equal, callingConventionEquality: Equality.Equal,
                Equality.NotEqual, Equality.NotEqual, Equality.NotEqual, Equality.Equal);
        }

        [Fact]
        public void Equality_ReturnTypeDifferent()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<string, object, C, object, ref string> p1,
           delegate*<string, object, C, object, ref readonly string> p2) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type,
                returnEquality: Equality.NotEqual, callingConventionEquality: Equality.Equal,
                Equality.Equal, Equality.Equal, Equality.Equal, Equality.Equal);
        }

        [Fact]
        public void Equality_InParameter()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<in string, void> p1,
           delegate*<in string, void> p2) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type,
                returnEquality: Equality.Equal, callingConventionEquality: Equality.Equal,
                Equality.Equal);
        }

        enum Equality
        {
            Equal = 0,
            DifferingNullability = 0b01,
            NotEqual = 0b10
        }

        private void AssertEqualityAndHashCode(FunctionPointerTypeSymbol p1, FunctionPointerTypeSymbol p2, Equality returnEquality, Equality callingConventionEquality, params Equality[] parameterEqualities)
        {
            var overallEquality = returnEquality | callingConventionEquality | (parameterEqualities.Length > 0 ? parameterEqualities.Aggregate((acc, cur) => acc | cur) : 0);

            assertSymbolEquality(p1, p2, overallEquality);
            assertSymbolEquality(p1.Signature, p2.Signature, overallEquality);

            var ret1 = p1.Signature.ReturnTypeWithAnnotations;
            var ret2 = p2.Signature.ReturnTypeWithAnnotations;
            if (hasFlag(returnEquality, Equality.NotEqual))
            {
                if (p1.Signature.RefKind == p2.Signature.RefKind)
                {
                    Assert.False(ret1.Equals(ret2, TypeCompareKind.ConsiderEverything));
                    Assert.False(ret1.Equals(ret2, TypeCompareKind.AllNullableIgnoreOptions));
                }
            }
            else
            {
                Assert.Equal(p1.Signature.RefKind, p2.Signature.RefKind);
                Assert.True(ret1.Equals(ret2, TypeCompareKind.AllNullableIgnoreOptions));
                Assert.Equal(ret1.GetHashCode(), ret2.GetHashCode());
                Assert.Equal(returnEquality == Equality.Equal, ret1.Equals(ret2, TypeCompareKind.ConsiderEverything));
            }

            for (int i = 0; i < p1.Signature.ParameterCount; i++)
            {
                ParameterSymbol param1 = p1.Signature.Parameters[i];
                ParameterSymbol param2 = p2.Signature.Parameters[i];
                assertSymbolEquality(param1, param2, overallEquality);
                if (parameterEqualities[i] == Equality.Equal)
                {
                    Assert.True(((FunctionPointerParameterSymbol)param1).MethodEqualityChecks((FunctionPointerParameterSymbol)param2,
                                                                                              TypeCompareKind.ConsiderEverything,
                                                                                              isValueTypeOverride: null));
                }

                for (int j = 0; j < p1.Signature.ParameterCount; j++)
                {
                    if (j == i) continue;

                    assertSymbolEquality(param1, p2.Signature.Parameters[j], Equality.NotEqual);
                    assertSymbolEquality(param2, p1.Signature.Parameters[j], Equality.NotEqual);
                }
            }

            static bool hasFlag(Equality eq, Equality flag) => (eq & flag) == flag;
            static void assertSymbolEquality(Symbol s1, Symbol s2, Equality eq)
            {
                if (hasFlag(eq, Equality.NotEqual))
                {
                    Assert.False(s1.Equals(s2, TypeCompareKind.ConsiderEverything));
                    Assert.False(s1.Equals(s2, TypeCompareKind.AllNullableIgnoreOptions));
                }
                else
                {
                    Assert.True(s1.Equals(s2, TypeCompareKind.AllNullableIgnoreOptions));
                    Assert.Equal(s1.GetHashCode(), s2.GetHashCode());
                    Assert.Equal(eq == Equality.Equal, s1.Equals(s2, TypeCompareKind.ConsiderEverything));
                }
            }
        }

        [Fact]
        public void NoInAttribute_NoInParameter()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<string, void> p1) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.MakeTypeMissing(WellKnownType.System_Runtime_InteropServices_InAttribute);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NoInAttribute_InParameter()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(delegate*<in string, void> p1) {}
}", parseOptions: TestOptions.RegularPreview);

            comp.MakeTypeMissing(WellKnownType.System_Runtime_InteropServices_InAttribute);
            comp.VerifyDiagnostics(
                    // (4,22): error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported
                    //     void M(delegate*<in string, void> p1) {}
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "in string").WithArguments("System.Runtime.InteropServices.InAttribute").WithLocation(4, 22));
        }

        [Fact]
        public void DifferringModOpts()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Test1
       extends[mscorlib] System.Object
{
    .method public hidebysig instance void  M(method bool& *(int32&) noModopts,
                                              method bool modopt([mscorlib]System.Object)& *(int32&) modoptOnReturn,
                                              method bool& *(int32 modopt([mscorlib]System.Object)&) modoptOnParam,
                                              method bool& modopt([mscorlib]System.Object) *(int32&) modoptOnReturnRef,
                                              method bool& *(int32& modopt([mscorlib]System.Object)) modoptOnParamRef) cil managed
    {
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ret
    } // end of method Test::M
}
";

            var comp = CreateCompilationWithIL("", ilSource, parseOptions: TestOptions.RegularPreview);
            var testClass = comp.GetTypeByMetadataName("Test1")!;
            var m = testClass.GetMethod("M");

            foreach (var param1 in m.Parameters)
            {
                foreach (var param2 in m.Parameters)
                {
                    if (!ReferenceEquals(param1, param2))
                    {
                        Assert.False(param1.Type.Equals(param2.Type, TypeCompareKind.ConsiderEverything));
                    }
                    else
                    {
                        Assert.True(param1.Type.Equals(param2.Type, TypeCompareKind.ConsiderEverything));
                    }

                    Assert.True(param1.Type.Equals(param2.Type, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                }
            }
        }
    }
}
