// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FunctionPointerTypeSymbolTests : CSharpTestBase
    {
        private static CSharpCompilation CreateFunctionPointerCompilation(string source, TargetFramework targetFramework = TargetFramework.Standard)
        {
            return CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll, targetFramework: targetFramework);
        }

        [InlineData("", RefKind.None, "delegate*<System.Object>")]
        [InlineData("ref", RefKind.Ref, "delegate*<ref System.Object>")]
        [InlineData("ref readonly", RefKind.RefReadOnly,
                    "delegate*<ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Object>")]
        [Theory]
        public void ValidReturnModifiers(string modifier, RefKind expectedKind, string expectedPublicType)
        {
            var comp = CreateFunctionPointerCompilation($@"
class C
{{
    unsafe void M(delegate*<{modifier} object> p) {{}}

}}");
            comp.VerifyDiagnostics();

            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var pointerType = (FunctionPointerTypeSymbol)m.Parameters.Single().Type;
            FunctionPointerUtilities.CommonVerifyFunctionPointer(pointerType);
            Assert.Equal(expectedKind, pointerType.Signature.RefKind);
            Assert.Equal(SpecialType.System_Object, pointerType.Signature.ReturnType.SpecialType);
            Assert.Empty(pointerType.Signature.Parameters);

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var paramType = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Single().ParameterList.Parameters
                .Single().Type;

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, paramType!,
                expectedSyntax: $"delegate*<{modifier} object>",
                expectedType: expectedPublicType,
                expectedSymbol: expectedPublicType);
        }

        [Fact]
        public void InvalidReturnModifiers()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(
        delegate*<readonly string> p1,
        delegate*<readonly ref string> p2,
        delegate*<ref ref readonly string> p3,
        delegate*<ref readonly readonly string> p4,
        delegate*<this string> p5,
        delegate*<params string> p6,
        delegate*<ref ref string> p7,
        delegate*<out string> p8)
    {}
}
");
            comp.VerifyDiagnostics(
                    // (5,19): error CS8808: 'readonly' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<readonly string> p1,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "readonly").WithArguments("readonly").WithLocation(5, 19),
                    // (6,19): error CS8808: 'readonly' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<readonly ref string> p2,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "readonly").WithArguments("readonly").WithLocation(6, 19),
                    // (7,23): error CS8809: A return type can only have one 'ref' modifier.
                    //         delegate*<ref ref readonly string> p3,
                    Diagnostic(ErrorCode.ERR_DupReturnTypeMod, "ref").WithArguments("ref").WithLocation(7, 23),
                    // (7,27): error CS8808: 'readonly' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<ref ref readonly string> p3,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "readonly").WithArguments("readonly").WithLocation(7, 27),
                    // (8,32): error CS8808: 'readonly' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<ref readonly readonly string> p4,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "readonly").WithArguments("readonly").WithLocation(8, 32),
                    // (9,19): error CS8808: 'this' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<this string> p5,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "this").WithArguments("this").WithLocation(9, 19),
                    // (10,19): error CS8808: 'params' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<params string> p6,
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "params").WithArguments("params").WithLocation(10, 19),
                    // (11,23): error CS8809: A return type can only have one 'ref' modifier.
                    //         delegate*<ref ref string> p7)
                    Diagnostic(ErrorCode.ERR_DupReturnTypeMod, "ref").WithArguments("ref").WithLocation(11, 23),
                    // (12,19): error CS8808: 'out' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                    //         delegate*<out string> p8)
                    Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "out").WithArguments("out").WithLocation(12, 19));

            var mParams = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters;
            Assert.Equal(8, mParams.Length);

            verifyRefKind(RefKind.None, mParams[0]);
            verifyRefKind(RefKind.Ref, mParams[1]);
            verifyRefKind(RefKind.Ref, mParams[2]);
            verifyRefKind(RefKind.RefReadOnly, mParams[3]);
            verifyRefKind(RefKind.None, mParams[4]);
            verifyRefKind(RefKind.None, mParams[5]);
            verifyRefKind(RefKind.Ref, mParams[6]);
            verifyRefKind(RefKind.None, mParams[7]);

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var parameterDecls = syntaxTree.GetRoot().DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .SelectMany(m => m.ParameterList.Parameters)
                .Select(p => p.Type!)
                .ToArray();

            Assert.Equal(8, parameterDecls.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[0],
                expectedSyntax: "delegate*<readonly string>",
                expectedType: "delegate*<System.String>",
                expectedSymbol: "delegate*<System.String>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[1],
                expectedSyntax: "delegate*<readonly ref string>",
                expectedType: "delegate*<ref System.String>",
                expectedSymbol: "delegate*<ref System.String>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[2],
                expectedSyntax: "delegate*<ref ref readonly string>",
                expectedType: "delegate*<ref System.String>",
                expectedSymbol: "delegate*<ref System.String>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[3],
                expectedSyntax: "delegate*<ref readonly readonly string>",
                expectedType: "delegate*<ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.String>",
                expectedSymbol: "delegate*<ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.String>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[4],
                expectedSyntax: "delegate*<this string>",
                expectedType: "delegate*<System.String>",
                expectedSymbol: "delegate*<System.String>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[5],
                expectedSyntax: "delegate*<params string>",
                expectedType: "delegate*<System.String>",
                expectedSymbol: "delegate*<System.String>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[6],
                expectedSyntax: "delegate*<ref ref string>",
                expectedType: "delegate*<ref System.String>",
                expectedSymbol: "delegate*<ref System.String>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[7],
                expectedSyntax: "delegate*<out string>",
                expectedType: "delegate*<System.String>",
                expectedSymbol: "delegate*<System.String>");

            static void verifyRefKind(RefKind expected, ParameterSymbol actual)
                => Assert.Equal(expected, ((FunctionPointerTypeSymbol)actual.Type).Signature.RefKind);
        }

        [Fact]
        public void InvalidModifiersOnVoidReturnType()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(
        delegate*<ref void> p1,
        delegate*<ref readonly void> p2) {}
}");
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

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var parameterDecls = syntaxTree.GetRoot().DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .SelectMany(m => m.ParameterList.Parameters)
                .Select(p => p.Type!)
                .ToArray();

            Assert.Equal(2, parameterDecls.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[0],
                expectedSyntax: "delegate*<ref void>",
                expectedType: "delegate*<ref System.Void>",
                expectedSymbol: "delegate*<ref System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[1],
                expectedSyntax: "delegate*<ref readonly void>",
                expectedType: "delegate*<ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Void>",
                expectedSymbol: "delegate*<ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Void>");
        }

        [InlineData("", CallingConvention.Default)]
        [InlineData("managed", CallingConvention.Default)]
        [InlineData("unmanaged[Cdecl]", CallingConvention.CDecl)]
        [InlineData("unmanaged[Stdcall]", CallingConvention.Standard)]
        [InlineData("unmanaged[Thiscall]", CallingConvention.ThisCall)]
        [InlineData("unmanaged[Fastcall]", CallingConvention.FastCall)]
        [InlineData("unmanaged", CallingConvention.Unmanaged)]
        [Theory]
        internal void ValidCallingConventions(string convention, CallingConvention expectedConvention)
        {
            string source = $@"
class C
{{
    public unsafe void M(delegate* {convention}<string> p) {{}}
}}";

            verify(CreateFunctionPointerCompilation(source));

            var compWithMissingMembers = CreateFunctionPointerCompilation(source, targetFramework: TargetFramework.Minimal);
            Assert.Null(compWithMissingMembers.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvCdecl"));
            Assert.Null(compWithMissingMembers.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvThiscall"));
            Assert.Null(compWithMissingMembers.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvFastcall"));
            Assert.Null(compWithMissingMembers.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvStdcall"));
            verify(compWithMissingMembers);

            void verify(CSharpCompilation comp)
            {
                if (expectedConvention == CallingConvention.Unmanaged)
                {
                    comp.VerifyDiagnostics(
                        // (4,36): error CS8889: The target runtime doesn't support extensible or runtime-environment default calling conventions.
                        //     public unsafe void M(delegate* unmanaged<string> p) {}
                        Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportUnmanagedDefaultCallConv, "unmanaged").WithLocation(4, 36)
                    );
                }
                else
                {
                    comp.VerifyDiagnostics();
                }
                var c = comp.GetTypeByMetadataName("C");
                var m = c.GetMethod("M");
                var pointerType = (FunctionPointerTypeSymbol)m.Parameters.Single().Type;
                FunctionPointerUtilities.CommonVerifyFunctionPointer(pointerType);
                Assert.Equal(expectedConvention, pointerType.Signature.CallingConvention);
                Assert.Equal(SpecialType.System_String, pointerType.Signature.ReturnType.SpecialType);

                var syntaxTree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(syntaxTree);

                string expectedType;
                switch (convention)
                {
                    case "":
                    case "managed":
                        expectedType = "delegate*<System.String>";
                        break;
                    default:
                        expectedType = $"delegate* {convention}<System.String>";
                        break;
                }

                FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model,
                    syntaxTree.GetRoot().DescendantNodes().OfType<FunctionPointerTypeSyntax>().Single(),
                    expectedSyntax: $"delegate* {convention}<string>",
                    expectedType: expectedType,
                    expectedSymbol: expectedType);
            }
        }

        [Fact]
        public void InvalidCallingConventions()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    public unsafe void M1(delegate* unmanaged[invalid]<void> p) {}
    public unsafe void M2(delegate* unmanaged[invalid, Stdcall]<void> p) {}
    public unsafe void M3(delegate* unmanaged[]<void> p) {}
}");

            comp.VerifyDiagnostics(
                // (4,47): error CS8889: The target runtime doesn't support extensible or runtime-environment default calling conventions.
                //     public unsafe void M1(delegate* unmanaged[invalid]<void> p) {}
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportUnmanagedDefaultCallConv, "invalid").WithLocation(4, 47),
                // (4,47): error CS8890: Type 'CallConvinvalid' is not defined.
                //     public unsafe void M1(delegate* unmanaged[invalid]<void> p) {}
                Diagnostic(ErrorCode.ERR_TypeNotFound, "invalid").WithArguments("CallConvinvalid").WithLocation(4, 47),
                // (5,37): error CS8889: The target runtime doesn't support extensible or runtime-environment default calling conventions.
                //     public unsafe void M2(delegate* unmanaged[invalid, Stdcall]<void> p) {}
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportUnmanagedDefaultCallConv, "unmanaged").WithLocation(5, 37),
                // (5,47): error CS8890: Type 'CallConvinvalid' is not defined.
                //     public unsafe void M2(delegate* unmanaged[invalid, Stdcall]<void> p) {}
                Diagnostic(ErrorCode.ERR_TypeNotFound, "invalid").WithArguments("CallConvinvalid").WithLocation(5, 47),
                // (6,47): error CS1001: Identifier expected
                //     public unsafe void M3(delegate* unmanaged[]<void> p) {}
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "]").WithLocation(6, 47),
                // (6,47): error CS8889: The target runtime doesn't support extensible or runtime-environment default calling conventions.
                //     public unsafe void M3(delegate* unmanaged[]<void> p) {}
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportUnmanagedDefaultCallConv, "").WithLocation(6, 47)
            );

            var c = comp.GetTypeByMetadataName("C");
            var m1 = c.GetMethod("M1");
            var m1PointerType = (FunctionPointerTypeSymbol)m1.Parameters.Single().Type;
            Assert.Equal(CallingConvention.Unmanaged, m1PointerType.Signature.CallingConvention);

            var m2 = c.GetMethod("M2");
            var m2PointerType = (FunctionPointerTypeSymbol)m2.Parameters.Single().Type;
            Assert.Equal(CallingConvention.Unmanaged, m2PointerType.Signature.CallingConvention);

            var m3 = c.GetMethod("M3");
            var m3PointerType = (FunctionPointerTypeSymbol)m3.Parameters.Single().Type;
            Assert.Equal(CallingConvention.Unmanaged, m3PointerType.Signature.CallingConvention);

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var functionPointers = syntaxTree.GetRoot().DescendantNodes().OfType<FunctionPointerTypeSyntax>().ToArray();
            Assert.Equal(3, functionPointers.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, functionPointers[0],
                expectedSyntax: "delegate* unmanaged[invalid]<void>",
                expectedType: "delegate* unmanaged[invalid]<System.Void modopt(System.Runtime.CompilerServices.CallConvinvalid[missing])>",
                expectedSymbol: "delegate* unmanaged[invalid]<System.Void modopt(System.Runtime.CompilerServices.CallConvinvalid[missing])>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, functionPointers[1],
                expectedSyntax: "delegate* unmanaged[invalid, Stdcall]<void>",
                expectedType: "delegate* unmanaged[invalid, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvinvalid[missing]) modopt(System.Runtime.CompilerServices.CallConvStdcall)>",
                expectedSymbol: "delegate* unmanaged[invalid, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvinvalid[missing]) modopt(System.Runtime.CompilerServices.CallConvStdcall)>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, functionPointers[2],
                expectedSyntax: "delegate* unmanaged[]<void>",
                expectedType: "delegate* unmanaged<System.Void>",
                expectedSymbol: "delegate* unmanaged<System.Void>");
        }

        [Fact]
        public void Parameters()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    public unsafe void M<T>(
        delegate*<int, void> p1,
        delegate*<object, void> p2,
        delegate*<C, void> p3,
        delegate*<object, object, void> p4,
        delegate*<T, object, void> p5,
        delegate*<delegate*<T>, void> p6) {}
}");
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

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var parameterDecls = syntaxTree.GetRoot().DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .SelectMany(m => m.ParameterList.Parameters)
                .Select(p => p.Type!)
                .ToArray();

            Assert.Equal(6, parameterDecls.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[0],
                expectedSyntax: "delegate*<int, void>",
                expectedType: "delegate*<System.Int32, System.Void>",
                expectedSymbol: "delegate*<System.Int32, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[1],
                expectedSyntax: "delegate*<object, void>",
                expectedType: "delegate*<System.Object, System.Void>",
                expectedSymbol: "delegate*<System.Object, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[2],
                expectedSyntax: "delegate*<C, void>",
                expectedType: "delegate*<C, System.Void>",
                expectedSymbol: "delegate*<C, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[3],
                expectedSyntax: "delegate*<object, object, void>",
                expectedType: "delegate*<System.Object, System.Object, System.Void>",
                expectedSymbol: "delegate*<System.Object, System.Object, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[4],
                expectedSyntax: "delegate*<T, object, void>",
                expectedType: "delegate*<T, System.Object, System.Void>",
                expectedSymbol: "delegate*<T, System.Object, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[5],
                expectedSyntax: "delegate*<delegate*<T>, void>",
                expectedType: "delegate*<delegate*<T>, System.Void>",
                expectedSymbol: "delegate*<delegate*<T>, System.Void>");

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
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    public unsafe void M(
        delegate*<ref string, void> p1,
        delegate*<in string, void> p2,
        delegate*<out string, void> p3,
        delegate*<string, void> p4) {}
}");

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

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var parameterDecls = syntaxTree.GetRoot().DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .SelectMany(m => m.ParameterList.Parameters)
                .Select(p => p.Type!)
                .ToArray();

            Assert.Equal(4, parameterDecls.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[0],
                expectedSyntax: "delegate*<ref string, void>",
                expectedType: "delegate*<ref System.String, System.Void>",
                expectedSymbol: "delegate*<ref System.String, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[1],
                expectedSyntax: "delegate*<in string, void>",
                expectedType: "delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.String, System.Void>",
                expectedSymbol: "delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.String, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[2],
                expectedSyntax: "delegate*<out string, void>",
                expectedType: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.String, System.Void>",
                expectedSymbol: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.String, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[3],
                expectedSyntax: "delegate*<string, void>",
                expectedType: "delegate*<System.String, System.Void>",
                expectedSymbol: "delegate*<System.String, System.Void>");

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
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    public unsafe void M(
        delegate*<params string[], void> p1,
        delegate*<this string, void> p2,
        delegate*<readonly ref string, void> p3,
        delegate*<in out string, void> p4,
        delegate*<out in string, void> p5,
        delegate*<in ref string, void> p6,
        delegate*<ref in string, void> p7,
        delegate*<out ref string, void> p8,
        delegate*<ref out string, void> p9) {}
        
}");

            comp.VerifyDiagnostics(
                // (5,19): error CS8755: 'params' cannot be used as a modifier on a function pointer parameter.
                //         delegate*<params string[], void> p1,
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "params").WithArguments("params").WithLocation(5, 19),
                // (6,19): error CS0027: Keyword 'this' is not available in the current context
                //         delegate*<this string, void> p2,
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(6, 19),
                // (7,19): error CS9501: 'readonly' modifier must be specified after 'ref'.
                //         delegate*<readonly ref string, void> p3,
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(7, 19),
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

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var parameterDecls = syntaxTree.GetRoot().DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .SelectMany(m => m.ParameterList.Parameters)
                .Select(p => p.Type!)
                .ToArray();

            Assert.Equal(9, parameterDecls.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[0],
                expectedSyntax: "delegate*<params string[], void>",
                expectedType: "delegate*<System.String[], System.Void>",
                expectedSymbol: "delegate*<System.String[], System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[1],
                expectedSyntax: "delegate*<this string, void>",
                expectedType: "delegate*<System.String, System.Void>",
                expectedSymbol: "delegate*<System.String, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[2],
                expectedSyntax: "delegate*<readonly ref string, void>",
                expectedType: "delegate*<ref System.String, System.Void>",
                expectedSymbol: "delegate*<ref System.String, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[3],
                expectedSyntax: "delegate*<in out string, void>",
                expectedType: "delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.String, System.Void>",
                expectedSymbol: "delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.String, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[4],
                expectedSyntax: "delegate*<out in string, void>",
                expectedType: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.String, System.Void>",
                expectedSymbol: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.String, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[5],
                expectedSyntax: "delegate*<in ref string, void>",
                expectedType: "delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.String, System.Void>",
                expectedSymbol: "delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.String, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[6],
                expectedSyntax: "delegate*<ref in string, void>",
                expectedType: "delegate*<ref System.String, System.Void>",
                expectedSymbol: "delegate*<ref System.String, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[7],
                expectedSyntax: "delegate*<out ref string, void>",
                expectedType: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.String, System.Void>",
                expectedSymbol: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.String, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, parameterDecls[8],
                expectedSyntax: "delegate*<ref out string, void>",
                expectedType: "delegate*<ref System.String, System.Void>",
                expectedSymbol: "delegate*<ref System.String, System.Void>");

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
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate*<void, void> p1) {}
}");

            comp.VerifyDiagnostics(
                    // (4,29): error CS1536: Invalid parameter type 'void'
                    //     void M(delegate*<void, void> p1) {}
                    Diagnostic(ErrorCode.ERR_NoVoidParameter, "void").WithLocation(4, 29));

            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var signature = ((FunctionPointerTypeSymbol)m.Parameters.Single().Type).Signature;
            Assert.True(signature.Parameters.Single().Type.IsVoidType());

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var paramType = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Single().ParameterList.Parameters
                .Single().Type;

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, paramType!,
                expectedSyntax: "delegate*<void, void>",
                expectedType: "delegate*<System.Void, System.Void>",
                expectedSymbol: "delegate*<System.Void, System.Void>");
        }

        [Fact]
        public void Equality_ReturnVoid()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate*<void> p1,
           delegate*<void> p2) {}
}");

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type, returnEquality: Equality.Equal, callingConventionEquality: Equality.Equal);
        }

        [Fact]
        public void EqualityDifferingNullability()
        {
            var comp = CreateFunctionPointerCompilation(@"
#nullable enable
class C
{
    unsafe void M(delegate*<string, string, string?> p1,
                  delegate*<string, string?, string> p2) {}
}");

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
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate*<string, object, C, int, void> p1,
                  delegate*<string, object, C, int, void> p2) {}
}");

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
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate*<delegate*<string, object>, delegate*<C, int>> p1,
                  delegate*<delegate*<string, object>, delegate*<C, int>> p2) {}
}");

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
            var comp = CreateFunctionPointerCompilation(@"
#nullable enable
class C
{
    unsafe void M(delegate*<delegate*<string, object?>, delegate*<C?, int>> p1,
                  delegate*<delegate*<string?, object>, delegate*<C, int>> p2) {}
}");

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
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate*<string, object, C, int, string> p1,
                  delegate*<string, object, C, int, void> p2) {}
}");

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
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate*<string, object, C, object, void> p1,
                  delegate*<string, object, C, int, void> p2) {}
}");

            comp.VerifyDiagnostics();
            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            AssertEqualityAndHashCode((FunctionPointerTypeSymbol)m.Parameters[0].Type, (FunctionPointerTypeSymbol)m.Parameters[1].Type,
                returnEquality: Equality.Equal, callingConventionEquality: Equality.Equal,
                Equality.Equal, Equality.Equal, Equality.Equal, Equality.NotEqual);
        }

        [Fact]
        public void Equality_CallingConventionNotEqual()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate* unmanaged[Cdecl]<string, object, C, object, void> p1,
                  delegate* unmanaged[Thiscall]<string, object, C, object, void> p2) {}
}");

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
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate*<ref string, object, C, object, void> p1,
           delegate*<string, in object, out C, object, void> p2) {}
}");

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
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate*<string, object, C, object, ref string> p1,
                  delegate*<string, object, C, object, ref readonly string> p2) {}
}");

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
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate*<in string, void> p1,
                  delegate*<in string, void> p2) {}
}");

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
                                                                                              TypeCompareKind.ConsiderEverything));
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
        public void Equality_DifferingRefKinds()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    delegate*<ref object> ptr1Ref;
    delegate*<ref readonly object> ptr1RefReadonly;
    delegate*<ref object, void> ptr2Ref;
    delegate*<in object, void> ptr2In;
    delegate*<out object, void> ptr2Out;
}");

            var ptr1Ref = comp.GetMember<FieldSymbol>("C.ptr1Ref").Type;
            var ptr1RefReadonly = comp.GetMember<FieldSymbol>("C.ptr1RefReadonly").Type;
            var ptr2Ref = comp.GetMember<FieldSymbol>("C.ptr2Ref").Type;
            var ptr2In = comp.GetMember<FieldSymbol>("C.ptr2In").Type;
            var ptr2Out = comp.GetMember<FieldSymbol>("C.ptr2Out").Type;

            var symbolEqualityComparer = new SymbolEqualityComparer(
                TypeCompareKind.ConsiderEverything | TypeCompareKind.FunctionPointerRefMatchesOutInRefReadonly | TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds);
            Assert.Equal(ptr1Ref.GetPublicSymbol(), ptr1RefReadonly.GetPublicSymbol(), symbolEqualityComparer);
            Assert.Equal(ptr2Ref.GetPublicSymbol(), ptr2In.GetPublicSymbol(), symbolEqualityComparer);
            Assert.Equal(ptr2Ref.GetPublicSymbol(), ptr2Out.GetPublicSymbol(), symbolEqualityComparer);
            Assert.Equal(ptr2In.GetPublicSymbol(), ptr2Out.GetPublicSymbol(), symbolEqualityComparer);

            Assert.Equal(ptr1Ref.GetHashCode(), ptr1RefReadonly.GetHashCode());
            Assert.Equal(ptr2Ref.GetHashCode(), ptr2In.GetHashCode());
            Assert.Equal(ptr2Ref.GetHashCode(), ptr2Out.GetHashCode());
            Assert.Equal(symbolEqualityComparer.GetHashCode(ptr1Ref.GetPublicSymbol()), symbolEqualityComparer.GetHashCode(ptr1RefReadonly.GetPublicSymbol()));
            Assert.Equal(symbolEqualityComparer.GetHashCode(ptr2Ref.GetPublicSymbol()), symbolEqualityComparer.GetHashCode(ptr2In.GetPublicSymbol()));
            Assert.Equal(symbolEqualityComparer.GetHashCode(ptr2Ref.GetPublicSymbol()), symbolEqualityComparer.GetHashCode(ptr2Out.GetPublicSymbol()));
        }

        [Fact]
        public void NoInOutAttribute_NoInOutParameter()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate*<string, void> p1) {}
}");

            comp.MakeTypeMissing(WellKnownType.System_Runtime_InteropServices_InAttribute);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_InteropServices_OutAttribute);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NoInOutAttribute_InOutParameter()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M(delegate*<in string, out string, void> p1) {}
}");

            comp.MakeTypeMissing(WellKnownType.System_Runtime_InteropServices_InAttribute);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_InteropServices_OutAttribute);
            comp.VerifyDiagnostics(
                // (4,29): error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported
                //     unsafe void M(delegate*<in string, out string, void> p1) {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "in string").WithArguments("System.Runtime.InteropServices.InAttribute").WithLocation(4, 29),
                // (4,40): error CS0518: Predefined type 'System.Runtime.InteropServices.OutAttribute' is not defined or imported
                //     unsafe void M(delegate*<in string, out string, void> p1) {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "out string").WithArguments("System.Runtime.InteropServices.OutAttribute").WithLocation(4, 40));
        }

        [Fact]
        public void DifferingModOpts()
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

            var comp = CreateCompilationWithIL("", ilSource, parseOptions: TestOptions.Regular9);
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

        [Fact]
        public void RequiresUnsafeInSignature()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    delegate*<void> _field;
    delegate*<void> Property { get; set; }
    delegate*<void> M(delegate*<void> param)
    {
        delegate*<void> local1;
        /**/delegate/**/*/**/<void> local2;
        throw null;
    }
}");

            comp.VerifyDiagnostics(
                    // (4,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    //     delegate*<void> _field;
                    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(4, 5),
                    // (4,21): warning CS0169: The field 'C._field' is never used
                    //     delegate*<void> _field;
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "_field").WithArguments("C._field").WithLocation(4, 21),
                    // (5,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    //     delegate*<void> Property { get; set; }
                    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(5, 5),
                    // (6,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    //     delegate*<void> M(delegate*<void> param)
                    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(6, 5),
                    // (6,23): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    //     delegate*<void> M(delegate*<void> param)
                    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(6, 23),
                    // (8,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    //         delegate*<void> local1;
                    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(8, 9),
                    // (8,25): warning CS0168: The variable 'local1' is declared but never used
                    //         delegate*<void> local1;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "local1").WithArguments("local1").WithLocation(8, 25),
                    // (9,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    //         /**/delegate/**/*/**/<void> local2;
                    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate/**/*").WithLocation(9, 13),
                    // (9,37): warning CS0168: The variable 'local1' is declared but never used
                    //         /**/delegate/**/*/**/<void> local2;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "local2").WithArguments("local2").WithLocation(9, 37));
        }

        [Fact]
        public void MisdeclaredArraysWithLocalDeclarationsAreHandled()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    unsafe void M()
    {
        int a = 1;
        delegate*<int[a]> local;
    }
}");

            comp.VerifyDiagnostics(
                    // (6,13): warning CS0219: The variable 'a' is assigned but its value is never used
                    //         int a = 1;
                    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "a").WithArguments("a").WithLocation(6, 13),
                    // (7,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                    //         delegate*<int[a]> local;
                    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[a]").WithLocation(7, 22),
                    // (7,27): warning CS0168: The variable 'local' is declared but never used
                    //         delegate*<int[a]> local;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "local").WithArguments("local").WithLocation(7, 27));

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var functionPointerTypeSyntax = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<FunctionPointerTypeSyntax>()
                .Single();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, functionPointerTypeSyntax,
                expectedSyntax: "delegate*<int[a]>",
                expectedType: "delegate*<System.Int32[]>",
                expectedSymbol: "delegate*<System.Int32[]>");

            var misplacedDeclaration =
                ((ArrayTypeSyntax)functionPointerTypeSyntax
                    .ParameterList.Parameters.Single().Type!)
                    .RankSpecifiers.Single()
                    .Sizes.Single();

            var a = (ILocalSymbol)model.GetSymbolInfo(misplacedDeclaration).Symbol!;
            Assert.NotNull(a);
            Assert.Equal("System.Int32 a", a.ToTestDisplayString());

            VerifyOperationTreeForNode(comp, model, functionPointerTypeSyntax.Parent, expectedOperationTree: @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'delegate*<int[a]> local')
  Ignored Dimensions(1):
      ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'a')
  Declarators:
      IVariableDeclaratorOperation (Symbol: delegate*<System.Int32[]> local) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'local')
        Initializer: 
          null
  Initializer: 
    null
");
        }

        [Fact]
        public void IncorrectArguments()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    void M(delegate*<void> p1,
           delegate*<string, void> p2,
           delegate*<string> p3,
           delegate*<ref string, void> p4,
           delegate*<in string, void> p5)
    {
        p1(""No arguments allowed"");
        p2(""Too"", ""many"", ""arguments"");
        p2(); // Not enough arguments
        p2(1); // Invalid argument type
        ref string foo = ref p3();
        string s = null;
        p4(s);
        p4(in s);
        p5(ref s);
    }
}");

            comp.VerifyDiagnostics(
                // (10,9): error CS8756: Function pointer 'delegate*<void>' does not take 1 arguments
                //         p1("No arguments allowed");
                Diagnostic(ErrorCode.ERR_BadFuncPointerArgCount, @"p1(""No arguments allowed"")").WithArguments("delegate*<void>", "1").WithLocation(10, 9),
                // (11,9): error CS8756: Function pointer 'delegate*<string, void>' does not take 3 arguments
                //         p2("Too", "many", "arguments");
                Diagnostic(ErrorCode.ERR_BadFuncPointerArgCount, @"p2(""Too"", ""many"", ""arguments"")").WithArguments("delegate*<string, void>", "3").WithLocation(11, 9),
                // (12,9): error CS8756: Function pointer 'delegate*<string, void>' does not take 0 arguments
                //         p2(); // Not enough arguments
                Diagnostic(ErrorCode.ERR_BadFuncPointerArgCount, "p2()").WithArguments("delegate*<string, void>", "0").WithLocation(12, 9),
                // (13,12): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //         p2(1); // Invalid argument type
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "string").WithLocation(13, 12),
                // (14,30): error CS1510: A ref or out value must be an assignable variable
                //         ref string foo = ref p3();
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "p3()").WithLocation(14, 30),
                // (16,12): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         p4(s);
                Diagnostic(ErrorCode.ERR_BadArgRef, "s").WithArguments("1", "ref").WithLocation(16, 12),
                // (17,15): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         p4(in s);
                Diagnostic(ErrorCode.ERR_BadArgRef, "s").WithArguments("1", "ref").WithLocation(17, 15),
                // (18,16): error CS1615: Argument 1 may not be passed with the 'ref' keyword
                //         p5(ref s);
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "s").WithArguments("1", "ref").WithLocation(18, 16));
        }

        [Fact]
        public void InaccessibleNestedTypes()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    private class D {}
}
class E
{
    unsafe void M()
    {
        delegate*<C.D> d;
    }
}");

            comp.VerifyDiagnostics(
                    // (10,21): error CS0122: 'C.D' is inaccessible due to its protection level
                    //         delegate*<C.D> d;
                    Diagnostic(ErrorCode.ERR_BadAccess, "D").WithArguments("C.D").WithLocation(10, 21),
                    // (10,24): warning CS0168: The variable 'd' is declared but never used
                    //         delegate*<C.D> d;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "d").WithArguments("d").WithLocation(10, 24));

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var functionPointerTypeSyntax = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<FunctionPointerTypeSyntax>()
                .Single();

            Assert.Equal("delegate*<C.D>", functionPointerTypeSyntax.ToString());

            var typeInfo = model.GetTypeInfo(functionPointerTypeSyntax);
            Assert.Equal("delegate*<C.D>", typeInfo.Type.ToTestDisplayString());
            Assert.True(((IFunctionPointerTypeSymbol)typeInfo.Type!).Signature.ReturnType.IsErrorType());

            var nestedTypeInfo = model.GetTypeInfo(functionPointerTypeSyntax.ParameterList.Parameters.Single().Type!);
            Assert.Equal("C.D", nestedTypeInfo.Type!.ToTestDisplayString());
            Assert.False(nestedTypeInfo.Type!.IsErrorType());

            VerifyOperationTreeForNode(comp, model, functionPointerTypeSyntax.Parent, expectedOperationTree: @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'delegate*<C.D> d')
  Declarators:
      IVariableDeclaratorOperation (Symbol: delegate*<C.D> d) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd')
        Initializer: 
          null
  Initializer: 
    null
");
        }

        [Fact]
        public void FunctionPointerConstraintIntroducedBySubstitution()
        {
            string source = @"
class R1<T1>
{
    public virtual void f<T2>() where T2 : T1 { }
}
class R2 : R1<delegate*<void>>
{
    public override void f<T2>() { }
}
class Program
{
    static void Main(string[] args)
    {
        R2 r = new R2();
        r.f<int>();
    }
}";

            var compilation = CreateFunctionPointerCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,7): error CS0306: The type 'delegate*<void>' may not be used as a type argument
                // class R2 : R1<delegate*<void>>
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "R2").WithArguments("delegate*<void>").WithLocation(6, 7)
            );

            var syntaxTree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(syntaxTree);

            var baseNameSyntax = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<SimpleBaseTypeSyntax>()
                .Single()
                .Type;

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, baseNameSyntax,
                expectedSyntax: "R1<delegate*<void>>",
                expectedType: "R1<delegate*<System.Void>>",
                expectedSymbol: "R1<delegate*<System.Void>>");
        }

        [Fact]
        public void FunctionPointerTypeAsThisOfExtensionMethod()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe static class C
{
    static void M1(this delegate*<void> ptr) {}
    static void M2(delegate*<void> ptr)
    {
        ptr.M1();
    }
}");

            comp.VerifyDiagnostics(
                // (4,25): error CS1103: The first parameter of an extension method cannot be of type 'delegate*<void>'
                //     static void M1(this delegate*<void> ptr) {}
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "delegate*<void>").WithArguments("delegate*<void>").WithLocation(4, 25)
            );
        }

        [Fact]
        public void FunctionPointerTypeAsThisOfExtensionMethod_DefinedInIl()
        {
            const string ilSource = @"
.class public auto ansi abstract sealed beforefieldinit CHelper
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Methods
    .method public hidebysig static 
        void M (
            method void*() i
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x205c
        // Code size 9 (0x9)
        .maxstack 8

        IL_0001: ldc.i4.1
        IL_0002: call void [mscorlib]System.Console::WriteLine(int32)
        IL_0008: ret
    } // end of method CHelper::M

} // end of class CHelper
";
            const string source = @"
static class C
{
    static unsafe void Main()
    {
        delegate*<void> ptr = null;
        ptr.M();
    }
}";
            var comp = CreateCompilationWithIL(source, ilSource, options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular9);

            var verifier = CompileAndVerify(comp, expectedOutput: "1", verify: Verification.Skipped);
            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (delegate*<void> V_0) //ptr
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""void CHelper.M(delegate*<void>)""
  IL_0009:  ret
}
");
        }

        [Fact]
        public void FunctionPointerTypeInAnonymousType()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe static class C
{
    static void M(delegate*<void> ptr)
    {
        var a = new { Ptr = ptr };
        var b = new { Ptrs = new[] { ptr } };
    }
}");

            comp.VerifyDiagnostics(
                // (6,23): error CS0828: Cannot assign 'delegate*<void>' to anonymous type property
                //         var a = new { Ptr = ptr };
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "Ptr = ptr").WithArguments("delegate*<void>").WithLocation(6, 23),
                // (7,23): error CS0828: Cannot assign 'delegate*<void>[]' to anonymous type property
                //         var b = new { Ptrs = new[] { ptr } };
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "Ptrs = new[] { ptr }").WithArguments("delegate*<void>[]").WithLocation(7, 23)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var anonymousObjectCreations = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<AnonymousObjectCreationExpressionSyntax>()
                .ToArray();

            Assert.Equal(2, anonymousObjectCreations.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, anonymousObjectCreations[0],
                expectedSyntax: "new { Ptr = ptr }",
                expectedType: "<anonymous type: delegate*<System.Void> Ptr>",
                expectedSymbol: "<anonymous type: delegate*<System.Void> Ptr>..ctor(delegate*<System.Void> Ptr)");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, anonymousObjectCreations[1],
                expectedSyntax: "new { Ptrs = new[] { ptr } }",
                expectedType: "<anonymous type: delegate*<System.Void>[] Ptrs>",
                expectedSymbol: "<anonymous type: delegate*<System.Void>[] Ptrs>..ctor(delegate*<System.Void>[] Ptrs)");

            VerifyOperationTreeForNode(comp, model, anonymousObjectCreations[0], expectedOperationTree: @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: delegate*<System.Void> Ptr>, IsInvalid) (Syntax: 'new { Ptr = ptr }')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'Ptr = ptr')
        Left: 
          IPropertyReferenceOperation: delegate*<System.Void> <anonymous type: delegate*<System.Void> Ptr>.Ptr { get; } (OperationKind.PropertyReference, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'Ptr')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: delegate*<System.Void> Ptr>, IsInvalid, IsImplicit) (Syntax: 'new { Ptr = ptr }')
        Right: 
          IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'ptr')
");

            VerifyOperationTreeForNode(comp, model, anonymousObjectCreations[1], expectedOperationTree: @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: delegate*<System.Void>[] Ptrs>, IsInvalid) (Syntax: 'new { Ptrs  ... ] { ptr } }')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: delegate*<System.Void>[], IsInvalid) (Syntax: 'Ptrs = new[] { ptr }')
        Left: 
          IPropertyReferenceOperation: delegate*<System.Void>[] <anonymous type: delegate*<System.Void>[] Ptrs>.Ptrs { get; } (OperationKind.PropertyReference, Type: delegate*<System.Void>[], IsInvalid) (Syntax: 'Ptrs')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: delegate*<System.Void>[] Ptrs>, IsInvalid, IsImplicit) (Syntax: 'new { Ptrs  ... ] { ptr } }')
        Right: 
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: delegate*<System.Void>[], IsInvalid) (Syntax: 'new[] { ptr }')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'new[] { ptr }')
            Initializer: 
              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{ ptr }')
                Element Values(1):
                    IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'ptr')
");
        }

        [Fact]
        public void FunctionPointerTypeAsArgToIterator()
        {
            var comp = CreateFunctionPointerCompilation(@"
using System.Collections.Generic;
unsafe class C
{
    IEnumerable<int> Iterator1(delegate*<void> i)
    {
        yield return 1;
    }

    IEnumerable<int> Iterator2(delegate*<void>[] i)
    {
        yield return 1;
    }
}");

            comp.VerifyDiagnostics(
                // (5,48): error CS1637: Iterators cannot have unsafe parameters or yield types
                //     IEnumerable<int> Iterator1(delegate*<void> i)
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "i").WithLocation(5, 48),
                // (10,50): error CS1637: Iterators cannot have unsafe parameters or yield types
                //     IEnumerable<int> Iterator2(delegate*<void>[] i)
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "i").WithLocation(10, 50)
            );
        }

        [Fact]
        public void FormattingReturnTypeOptions()
        {
            var il = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
    .field public method int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) *() 'Field1'
    .field public method int32 modopt([mscorlib]System.Object) & *() 'Field2'
}
";

            var comp = CreateCompilation("", references: new[] { CompileIL(il) });
            comp.VerifyDiagnostics();

            var c = comp.GetTypeByMetadataName("C");
            var f1 = c.GetField("Field1").Type;
            var f2 = c.GetField("Field2").Type;

            Assert.Equal("delegate*<ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Int32>", f1.ToTestDisplayString());
            Assert.Equal("delegate*<ref readonly int>", f1.ToDisplayString());
            Assert.Equal("delegate*<ref System.Int32 modopt(System.Object)>", f2.ToTestDisplayString());
            Assert.Equal("delegate*<ref int>", f2.ToDisplayString());
        }

        [Fact]
        public void PublicApi_CreateInvalidInputs()
        {
            var comp = (Compilation)CreateCompilation("");
            var @string = comp.GetSpecialType(SpecialType.System_String);
            var cdeclType = comp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvCdecl");
            Assert.NotNull(cdeclType);
            Assert.Throws<ArgumentNullException>("returnType", () => comp.CreateFunctionPointerTypeSymbol(returnType: null!, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty));
            Assert.Throws<ArgumentNullException>("parameterTypes", () => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: default, parameterRefKinds: ImmutableArray<RefKind>.Empty));
            Assert.Throws<ArgumentNullException>("parameterTypes[0]", () => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray.Create((ITypeSymbol?)null)!, parameterRefKinds: ImmutableArray.Create(RefKind.None)));
            Assert.Throws<ArgumentNullException>("parameterRefKinds", () => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: default));
            Assert.Throws<ArgumentNullException>("callingConventionTypes[0]", () => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, callingConvention: SignatureCallingConvention.Unmanaged, ImmutableArray.Create((INamedTypeSymbol)null!)));
            Assert.Throws<ArgumentException>(() => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray.Create(RefKind.None)));
            Assert.Throws<ArgumentException>(() => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.Out, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, callingConvention: (SignatureCallingConvention)10));
            Assert.Throws<ArgumentException>(() => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, callingConvention: SignatureCallingConvention.Default, callingConventionTypes: ImmutableArray.Create(cdeclType)!));
            Assert.Throws<ArgumentException>(() => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, callingConvention: SignatureCallingConvention.StdCall, callingConventionTypes: ImmutableArray.Create(cdeclType)!));
            Assert.Throws<ArgumentException>(() => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, callingConvention: SignatureCallingConvention.FastCall, callingConventionTypes: ImmutableArray.Create(cdeclType)!));
            Assert.Throws<ArgumentException>(() => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, callingConvention: SignatureCallingConvention.CDecl, callingConventionTypes: ImmutableArray.Create(cdeclType)!));
            Assert.Throws<ArgumentException>(() => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, callingConvention: SignatureCallingConvention.ThisCall, callingConventionTypes: ImmutableArray.Create(cdeclType)!));
            Assert.Throws<ArgumentException>(() => comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, callingConvention: SignatureCallingConvention.Unmanaged, callingConventionTypes: ImmutableArray.Create(@string)!));
        }

        [Fact]
        public void PublicApi_VarargsHasUseSiteDiagnostic()
        {
            var comp = (Compilation)CreateCompilation("");
            var @string = comp.GetSpecialType(SpecialType.System_String);
            var ptr = comp.CreateFunctionPointerTypeSymbol(returnType: @string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, callingConvention: SignatureCallingConvention.VarArgs);

            Assert.Equal(SignatureCallingConvention.VarArgs, ptr.Signature.CallingConvention);
            var expectedMessage = "error CS8806: " + string.Format(CSharpResources.ERR_UnsupportedCallingConvention, "delegate* unmanaged[]<string>");
            AssertEx.Equal(expectedMessage, ptr.EnsureCSharpSymbolOrNull(nameof(ptr)).GetUseSiteDiagnostic().ToString());
        }

        [Fact]
        public void PublicApi_CreateTypeSymbolNoRefKinds()
        {
            var comp = (Compilation)CreateCompilation(@"class C {}");

            var c = comp.GetTypeByMetadataName("C")!;

            var ptr = comp.CreateFunctionPointerTypeSymbol(
                c.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None),
                RefKind.None,
                ImmutableArray.Create(c.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.NotAnnotated), c.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated)),
                ImmutableArray.Create(RefKind.None, RefKind.None));

            Assert.Equal("delegate*<C!, C?, C>", ptr.ToTestDisplayString(includeNonNullable: true));
        }

        [Fact]
        public void PublicApi_CreateInRefReadonlyTypeSymbol()
        {
            var comp = (Compilation)CreateCompilation("");
            var @string = comp.GetSpecialType(SpecialType.System_String);

            var ptr = comp.CreateFunctionPointerTypeSymbol(
                @string,
                RefKind.RefReadOnly,
                ImmutableArray.Create((ITypeSymbol)@string),
                ImmutableArray.Create(RefKind.In));

            Assert.Equal("delegate*<in modreq(System.Runtime.InteropServices.InAttribute) System.String, ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.String>",
                         ptr.ToTestDisplayString());
        }

        [Fact]
        public void PublicApi_CreateOutTypeSymbol()
        {
            var comp = (Compilation)CreateCompilation("");
            var @string = comp.GetSpecialType(SpecialType.System_String);
            var @void = comp.GetSpecialType(SpecialType.System_Void);

            var ptr = comp.CreateFunctionPointerTypeSymbol(
                @void,
                RefKind.None,
                ImmutableArray.Create((ITypeSymbol)@string),
                ImmutableArray.Create(RefKind.Out));

            Assert.Equal("delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.String, System.Void>",
                         ptr.ToTestDisplayString());
        }

        [Fact]
        public void PublicApi_InOutAttributeMissing()
        {
            var comp = (Compilation)CreateCompilation("");
            comp.MakeTypeMissing(WellKnownType.System_Runtime_InteropServices_InAttribute);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_InteropServices_OutAttribute);
            var @string = comp.GetSpecialType(SpecialType.System_String);

            var ptr = comp.CreateFunctionPointerTypeSymbol(
                @string,
                RefKind.RefReadOnly,
                ImmutableArray.Create((ITypeSymbol)@string),
                ImmutableArray.Create(RefKind.Out));

            Assert.Equal("System.Runtime.InteropServices.InAttribute[missing]", ptr.Signature.RefCustomModifiers.Single().Modifier.ToTestDisplayString());
            Assert.Equal("System.Runtime.InteropServices.OutAttribute[missing]", ptr.Signature.Parameters.Single().RefCustomModifiers.Single().Modifier.ToTestDisplayString());
        }

        [Theory]
        [InlineData("[Cdecl]", SignatureCallingConvention.CDecl)]
        [InlineData("[Thiscall]", SignatureCallingConvention.ThisCall)]
        [InlineData("[Stdcall]", SignatureCallingConvention.StdCall)]
        [InlineData("[Fastcall]", SignatureCallingConvention.FastCall)]
        [InlineData("", SignatureCallingConvention.Unmanaged)]
        public void PublicApi_CallingConventions_NoModopts(string expectedText, SignatureCallingConvention convention)
        {
            var comp = (Compilation)CreateCompilation("");
            var @string = comp.GetSpecialType(SpecialType.System_String);

            var ptr = comp.CreateFunctionPointerTypeSymbol(@string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, convention);
            AssertEx.Equal($"delegate* unmanaged{expectedText}<System.String>", ptr.ToTestDisplayString());
            ptr = comp.CreateFunctionPointerTypeSymbol(@string, returnRefKind: RefKind.RefReadOnly, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, convention);
            AssertEx.Equal($"delegate* unmanaged{expectedText}<ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.String>", ptr.ToTestDisplayString());
        }

        [Fact]
        public void PublicApi_CallingConventions_Modopts()
        {
            var comp = (Compilation)CreateCompilation("");
            var @string = comp.GetSpecialType(SpecialType.System_String);
            var cdeclType = comp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvCdecl");
            var stdcallType = comp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvStdcall");
            Assert.NotNull(cdeclType);

            var ptr = comp.CreateFunctionPointerTypeSymbol(@string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, SignatureCallingConvention.Unmanaged, ImmutableArray.Create(cdeclType, stdcallType)!);
            AssertEx.Equal("delegate* unmanaged[Cdecl, Stdcall]<System.String modopt(System.Runtime.CompilerServices.CallConvCdecl) modopt(System.Runtime.CompilerServices.CallConvStdcall)>", ptr.ToTestDisplayString());
            ptr = comp.CreateFunctionPointerTypeSymbol(@string, returnRefKind: RefKind.RefReadOnly, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, SignatureCallingConvention.Unmanaged, ImmutableArray.Create(cdeclType, stdcallType)!);
            AssertEx.Equal("delegate* unmanaged[Cdecl, Stdcall]<ref readonly modopt(System.Runtime.CompilerServices.CallConvCdecl) modopt(System.Runtime.CompilerServices.CallConvStdcall) modreq(System.Runtime.InteropServices.InAttribute) System.String>", ptr.ToTestDisplayString());

            ptr = comp.CreateFunctionPointerTypeSymbol(@string, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<ITypeSymbol>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty, SignatureCallingConvention.Unmanaged, ImmutableArray.Create(cdeclType)!);
            AssertEx.Equal("delegate* unmanaged[Cdecl]<System.String modopt(System.Runtime.CompilerServices.CallConvCdecl)>", ptr.ToTestDisplayString());
            Assert.Equal(SignatureCallingConvention.Unmanaged, ptr.Signature.CallingConvention);
        }

        [Fact]
        public void PublicApi_SemanticInfo01()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    public static string M1(C c) => null;
    public static void M2(string s, int i) {}
    public delegate*<string, int, void> M(delegate*<C, string> ptr)
    {
        delegate*<string, int, void> ptr2 = &M2;
        ptr = &M1;
        ptr(null);
        return &M2;
    }
}");

            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var mDeclSyntax = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Skip(2).Single();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, mDeclSyntax.ReturnType,
                expectedSyntax: "delegate*<string, int, void>",
                expectedType: "delegate*<System.String, System.Int32, System.Void>",
                expectedSymbol: "delegate*<System.String, System.Int32, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, mDeclSyntax.ParameterList.Parameters[0].Type!,
                expectedSyntax: "delegate*<C, string>",
                expectedType: "delegate*<C, System.String>",
                expectedSymbol: "delegate*<C, System.String>");

            var varDecl = mDeclSyntax.DescendantNodes().OfType<VariableDeclarationSyntax>().Single();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, varDecl.Type,
                expectedSyntax: "delegate*<string, int, void>",
                expectedType: "delegate*<System.String, System.Int32, System.Void>",
                expectedSymbol: "delegate*<System.String, System.Int32, System.Void>");

            var varInitializer = varDecl.Variables.Single().Initializer!.Value;
            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, varInitializer,
                expectedSyntax: "&M2",
                expectedType: null,
                expectedConvertedType: "delegate*<System.String, System.Int32, System.Void>",
                expectedSymbol: "void C.M2(System.String s, System.Int32 i)");

            var assignment = mDeclSyntax.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, assignment,
                expectedSyntax: "ptr = &M1",
                expectedType: "delegate*<C, System.String>",
                expectedSymbol: null,
                expectedSymbolCandidates: null);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, assignment.Left,
                expectedSyntax: "ptr",
                expectedType: "delegate*<C, System.String>",
                expectedSymbol: "delegate*<C, System.String> ptr");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, assignment.Right,
                expectedSyntax: "&M1",
                expectedType: null,
                expectedConvertedType: "delegate*<C, System.String>",
                expectedSymbol: "System.String C.M1(C c)");

            InvocationExpressionSyntax invocationExpressionSyntax = mDeclSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocationExpressionSyntax,
                expectedSyntax: "ptr(null)",
                expectedType: "System.String",
                expectedSymbol: "delegate*<C, System.String>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocationExpressionSyntax.Expression,
                expectedSyntax: "ptr",
                expectedType: "delegate*<C, System.String>",
                expectedSymbol: "delegate*<C, System.String> ptr");

            var typeInfo = model.GetTypeInfo(invocationExpressionSyntax);
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString());

            var returnExpression = mDeclSyntax.DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression!;
            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model,
                returnExpression,
                expectedSyntax: "&M2",
                expectedType: null,
                expectedConvertedType: "delegate*<System.String, System.Int32, System.Void>",
                expectedSymbol: "void C.M2(System.String s, System.Int32 i)");
        }

        [Fact]
        public void PublicApi_SemanticInfo02()
        {
            var comp = CreateFunctionPointerCompilation(@"
class C
{
    public static string M1(int i) => null;
    public unsafe void M2()
    {
        delegate*<int, string> ptr = &M1;
        delegate*<int, void> ptr2 = &M1;
        _ = ptr(1);
        _ = ptr();
    }

    public void M3()
    {
        delegate*<int, string> ptr = &M1;
        _ = ptr(1);
    }
}
");

            comp.VerifyDiagnostics(
                // (8,38): error CS0407: 'string C.M1(int)' has the wrong return type
                //         delegate*<int, void> ptr2 = &M1;
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("C.M1(int)", "string").WithLocation(8, 38),
                // (10,13): error CS8756: Function pointer 'delegate*<int, string>' does not take 0 arguments
                //         _ = ptr();
                Diagnostic(ErrorCode.ERR_BadFuncPointerArgCount, "ptr()").WithArguments("delegate*<int, string>", "0").WithLocation(10, 13),
                // (15,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         delegate*<int, string> ptr = &M1;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(15, 9),
                // (16,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         _ = ptr(1);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "ptr(1)").WithLocation(16, 13)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var methodDecls = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();

            var ptrTypes = methodDecls
                .SelectMany(m => m.DescendantNodes().OfType<FunctionPointerTypeSyntax>())
                .ToArray();

            Assert.Equal(3, ptrTypes.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, ptrTypes[0],
                expectedSyntax: "delegate*<int, string>",
                expectedType: "delegate*<System.Int32, System.String>",
                expectedSymbol: "delegate*<System.Int32, System.String>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, ptrTypes[1],
                expectedSyntax: "delegate*<int, void>",
                expectedType: "delegate*<System.Int32, System.Void>",
                expectedSymbol: "delegate*<System.Int32, System.Void>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, ptrTypes[2],
                expectedSyntax: "delegate*<int, string>",
                expectedType: "delegate*<System.Int32, System.String>",
                expectedSymbol: "delegate*<System.Int32, System.String>");

            var m2DeclSyntax = methodDecls[1];
            var decls = m2DeclSyntax.DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();

            Assert.Equal("ptr = &M1", decls[0].ToString());
            var addressOfSyntax = (PrefixUnaryExpressionSyntax)decls[0].Initializer!.Value;

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, addressOfSyntax,
                expectedSyntax: "&M1",
                expectedType: null,
                expectedConvertedType: "delegate*<System.Int32, System.String>",
                expectedSymbol: "System.String C.M1(System.Int32 i)");

            Assert.Equal("ptr2 = &M1", decls[1].ToString());
            addressOfSyntax = (PrefixUnaryExpressionSyntax)decls[1].Initializer!.Value;

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, addressOfSyntax,
                expectedSyntax: "&M1",
                expectedType: null,
                expectedConvertedType: "delegate*<System.Int32, System.Void>",
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "System.String C.M1(System.Int32 i)" });

            var invocations = m2DeclSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(2, invocations.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocations[0],
                expectedSyntax: "ptr(1)",
                expectedType: "System.String",
                expectedSymbol: "delegate*<System.Int32, System.String>");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocations[0].Expression,
                expectedSyntax: "ptr",
                expectedType: "delegate*<System.Int32, System.String>",
                expectedSymbol: "delegate*<System.Int32, System.String> ptr");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocations[1],
                expectedSyntax: "ptr()",
                expectedType: "System.String",
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "delegate*<System.Int32, System.String>" });

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocations[1].Expression,
                expectedSyntax: "ptr",
                expectedType: "delegate*<System.Int32, System.String>",
                expectedSymbol: "delegate*<System.Int32, System.String> ptr");

            var m3DeclSyntax = methodDecls[2];

            var variableDeclaratorSyntax = m3DeclSyntax.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            Assert.Equal("ptr = &M1", variableDeclaratorSyntax.ToString());

            var initializerValue = variableDeclaratorSyntax.Initializer!.Value;
            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, initializerValue,
                expectedSyntax: "&M1",
                expectedType: null,
                expectedConvertedType: "delegate*<System.Int32, System.String>",
                expectedSymbol: "System.String C.M1(System.Int32 i)");

            var invocationExpr = m3DeclSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, invocationExpr,
                expectedSyntax: "ptr(1)",
                expectedType: "System.String",
                expectedSymbol: "delegate*<System.Int32, System.String>");
        }

        [Fact]
        public void PublicApi_DeclaredSymbol_BadSymbols()
        {
            var comp = CreateFunctionPointerCompilation(@"
#pragma warning disable CS0168 // Unused local
unsafe class C
{
    void M()
    {
        delegate*<out int> ptr1;
        delegate*<in int> ptr2;
        delegate*<ref readonly int, void> ptr3;
        delegate*<void, void> ptr4;
        delegate*<ref void> ptr5;
    }
}
");

            comp.VerifyDiagnostics(
                // (7,19): error CS8808: 'out' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                //         delegate*<out int> ptr1;
                Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "out").WithArguments("out").WithLocation(7, 19),
                // (8,19): error CS8808: 'in' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                //         delegate*<in int> ptr2;
                Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "in").WithArguments("in").WithLocation(8, 19),
                // (9,23): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         delegate*<ref readonly int, void> ptr3;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(9, 23),
                // (10,19): error CS1536: Invalid parameter type 'void'
                //         delegate*<void, void> ptr4;
                Diagnostic(ErrorCode.ERR_NoVoidParameter, "void").WithLocation(10, 19),
                // (11,19): error CS1547: Keyword 'void' cannot be used in this context
                //         delegate*<ref void> ptr5;
                Diagnostic(ErrorCode.ERR_NoVoidHere, "ref void").WithLocation(11, 19));

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var decls = syntaxTree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();

            Assert.Equal(5, decls.Length);

            Assert.Equal("delegate*<System.Int32> ptr1", model.GetDeclaredSymbol(decls[0]).ToTestDisplayString());
            Assert.Equal("delegate*<System.Int32> ptr2", model.GetDeclaredSymbol(decls[1]).ToTestDisplayString());
            Assert.Equal("delegate*<ref System.Int32, System.Void> ptr3", model.GetDeclaredSymbol(decls[2]).ToTestDisplayString());
            Assert.Equal("delegate*<System.Void, System.Void> ptr4", model.GetDeclaredSymbol(decls[3]).ToTestDisplayString());
            Assert.Equal("delegate*<ref System.Void> ptr5", model.GetDeclaredSymbol(decls[4]).ToTestDisplayString());
        }

        [Fact]
        public void PublicApi_NonApplicationCorLibrary()
        {
            var otherCorLib = CreateEmptyCompilation(@"
namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public class String { }
    namespace Runtime.CompilerServices
    {
        internal class CallConvTest {}
        public static class RuntimeFeature
        {
            public const string UnmanagedSignatureCallingConvention = nameof(UnmanagedSignatureCallingConvention);
        }
    }
}
", options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9);

            var mainComp = CreateCompilation("");
            var returnType = mainComp.GetSpecialType(SpecialType.System_String).GetPublicSymbol();
            var testConvention = otherCorLib.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvTest");
            Assert.NotNull(testConvention);
            Assert.NotSame(testConvention!.ContainingAssembly.CorLibrary, mainComp.Assembly.CorLibrary);
            Assert.True(FunctionPointerTypeSymbol.IsCallingConventionModifier(testConvention));

            Assert.Throws<ArgumentException>(() => mainComp.CreateFunctionPointerTypeSymbol(
                returnType!,
                returnRefKind: RefKind.None,
                parameterTypes: ImmutableArray<ITypeSymbol>.Empty,
                parameterRefKinds: ImmutableArray<RefKind>.Empty,
                callingConvention: SignatureCallingConvention.Unmanaged,
                callingConventionTypes: ImmutableArray.Create(testConvention.GetPublicSymbol()!)));
        }

        [Fact]
        public void Equality_UnmanagedExtensionModifiers()
        {
            var comp = CreateFunctionPointerCompilation("");

            var returnType = comp.GetSpecialType(SpecialType.System_String);

            var objectMod = CSharpCustomModifier.CreateOptional(comp.GetSpecialType(SpecialType.System_Object));
            var thiscallMod = CSharpCustomModifier.CreateOptional(comp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvThiscall"));
            var stdcallMod = CSharpCustomModifier.CreateOptional(comp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvStdcall"));

            var funcPtrPlatformDefault = createTypeSymbol(customModifiers: default);
            var funcPtrConventionThisCall = createTypeSymbol(customModifiers: default, CallingConvention.ThisCall);
            var funcPtrConventionThisCallWithThiscallMod = createTypeSymbol(customModifiers: ImmutableArray.Create(thiscallMod), CallingConvention.ThisCall);

            var funcPtrThiscall = createTypeSymbol(customModifiers: ImmutableArray.Create(thiscallMod));
            var funcPtrThiscallObject = createTypeSymbol(customModifiers: ImmutableArray.Create(thiscallMod, objectMod));
            var funcPtrObjectThiscall = createTypeSymbol(customModifiers: ImmutableArray.Create(objectMod, thiscallMod));
            var funcPtrObjectThiscallObject = createTypeSymbol(customModifiers: ImmutableArray.Create(objectMod, thiscallMod, objectMod));

            var funcPtrThiscallStdcall = createTypeSymbol(customModifiers: ImmutableArray.Create(thiscallMod, stdcallMod));
            var funcPtrStdcallThiscall = createTypeSymbol(customModifiers: ImmutableArray.Create(stdcallMod, thiscallMod));
            var funcPtrThiscallThiscallStdcall = createTypeSymbol(customModifiers: ImmutableArray.Create(thiscallMod, thiscallMod, stdcallMod));
            var funcPtrThiscallObjectStdcall = createTypeSymbol(customModifiers: ImmutableArray.Create(thiscallMod, objectMod, stdcallMod));

            verifyEquality(funcPtrPlatformDefault, funcPtrThiscall, expectedConventionEquality: false, expectedFullEquality: false);
            verifyEquality(funcPtrPlatformDefault, funcPtrConventionThisCall, expectedConventionEquality: false, expectedFullEquality: false, skipGetCallingConventionModifiersCheck: true);
            verifyEquality(funcPtrConventionThisCallWithThiscallMod, funcPtrConventionThisCall, expectedConventionEquality: true, expectedFullEquality: false, skipGetCallingConventionModifiersCheck: true);

            // Single calling convention modopt
            verifyEquality(funcPtrThiscall, funcPtrThiscallObject, expectedConventionEquality: true, expectedFullEquality: false);
            verifyEquality(funcPtrThiscall, funcPtrObjectThiscall, expectedConventionEquality: true, expectedFullEquality: false);
            verifyEquality(funcPtrThiscall, funcPtrObjectThiscallObject, expectedConventionEquality: true, expectedFullEquality: false);

            verifyEquality(funcPtrThiscallObject, funcPtrObjectThiscall, expectedConventionEquality: true, expectedFullEquality: false);
            verifyEquality(funcPtrThiscallObject, funcPtrObjectThiscallObject, expectedConventionEquality: true, expectedFullEquality: false);

            verifyEquality(funcPtrObjectThiscall, funcPtrObjectThiscallObject, expectedConventionEquality: true, expectedFullEquality: false);

            // Multiple calling convention modopts
            verifyEquality(funcPtrThiscallStdcall, funcPtrStdcallThiscall, expectedConventionEquality: true, expectedFullEquality: false);
            verifyEquality(funcPtrThiscallStdcall, funcPtrThiscallThiscallStdcall, expectedConventionEquality: true, expectedFullEquality: false);
            verifyEquality(funcPtrThiscallStdcall, funcPtrThiscallObjectStdcall, expectedConventionEquality: true, expectedFullEquality: false);

            verifyEquality(funcPtrStdcallThiscall, funcPtrThiscallThiscallStdcall, expectedConventionEquality: true, expectedFullEquality: false);
            verifyEquality(funcPtrStdcallThiscall, funcPtrThiscallObjectStdcall, expectedConventionEquality: true, expectedFullEquality: false);

            verifyEquality(funcPtrThiscallThiscallStdcall, funcPtrThiscallObjectStdcall, expectedConventionEquality: true, expectedFullEquality: false);

            static void verifyEquality((FunctionPointerTypeSymbol NoRef, FunctionPointerTypeSymbol ByRef) ptr1, (FunctionPointerTypeSymbol NoRef, FunctionPointerTypeSymbol ByRef) ptr2, bool expectedConventionEquality, bool expectedFullEquality, bool skipGetCallingConventionModifiersCheck = false)
            {
                // No equality between pointers with differing refkinds
                Assert.False(ptr1.NoRef.Equals(ptr2.ByRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                Assert.False(ptr1.NoRef.Equals(ptr2.ByRef, TypeCompareKind.ConsiderEverything));
                Assert.False(ptr1.ByRef.Equals(ptr2.NoRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                Assert.False(ptr1.ByRef.Equals(ptr2.NoRef, TypeCompareKind.ConsiderEverything));

                if (!skipGetCallingConventionModifiersCheck)
                {
                    Assert.Equal(expectedConventionEquality, ptr1.NoRef.Signature.GetCallingConventionModifiers().SetEquals(ptr2.NoRef.Signature.GetCallingConventionModifiers()));
                    Assert.Equal(expectedConventionEquality, ptr1.ByRef.Signature.GetCallingConventionModifiers().SetEquals(ptr2.ByRef.Signature.GetCallingConventionModifiers()));
                }
                Assert.Equal(expectedConventionEquality, ptr1.NoRef.Equals(ptr2.NoRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                Assert.Equal(expectedConventionEquality, ptr1.ByRef.Equals(ptr2.ByRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                Assert.Equal(expectedFullEquality, ptr1.NoRef.Equals(ptr2.NoRef, TypeCompareKind.ConsiderEverything));
                Assert.Equal(expectedFullEquality, ptr1.ByRef.Equals(ptr2.ByRef, TypeCompareKind.ConsiderEverything));
            }

            (FunctionPointerTypeSymbol NoRef, FunctionPointerTypeSymbol ByRef) createTypeSymbol(ImmutableArray<CustomModifier> customModifiers, CallingConvention callingConvention = CallingConvention.Unmanaged)
                => (FunctionPointerTypeSymbol.CreateFromPartsForTests(
                        callingConvention,
                        TypeWithAnnotations.Create(returnType, customModifiers: customModifiers),
                        refCustomModifiers: default,
                        returnRefKind: RefKind.None,
                        parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty,
                        parameterRefCustomModifiers: default,
                        parameterRefKinds: ImmutableArray<RefKind>.Empty,
                        compilation: comp),
                    FunctionPointerTypeSymbol.CreateFromPartsForTests(
                        callingConvention,
                        TypeWithAnnotations.Create(returnType),
                        customModifiers,
                        RefKind.Ref,
                        parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty,
                        parameterRefCustomModifiers: default,
                        parameterRefKinds: ImmutableArray<RefKind>.Empty,
                        compilation: comp));
        }

        [Fact]
        public void CallingConventionNamedCallConv()
        {
            var comp = CreateEmptyCompilation(@"
namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public class String { }
    namespace Runtime.CompilerServices
    {
        internal class CallConv {}
        public static class RuntimeFeature
        {
            public const string UnmanagedSignatureCallingConvention = nameof(UnmanagedSignatureCallingConvention);
        }
    }
}
", options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9);

            var returnType = comp.GetSpecialType(SpecialType.System_String);

            var callConvMod = CSharpCustomModifier.CreateOptional(comp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConv"));

            var funcPtr = createTypeSymbol(customModifiers: default);
            var funcPtrCallConv = createTypeSymbol(customModifiers: ImmutableArray.Create(callConvMod));

            verifyEquality(funcPtr, funcPtrCallConv, expectedConventionEquality: true, expectedFullEquality: false);

            static void verifyEquality((FunctionPointerTypeSymbol NoRef, FunctionPointerTypeSymbol ByRef) ptr1, (FunctionPointerTypeSymbol NoRef, FunctionPointerTypeSymbol ByRef) ptr2, bool expectedConventionEquality, bool expectedFullEquality)
            {
                // No equality between pointers with differing refkinds
                Assert.False(ptr1.NoRef.Equals(ptr2.ByRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                Assert.False(ptr1.NoRef.Equals(ptr2.ByRef, TypeCompareKind.ConsiderEverything));
                Assert.False(ptr1.ByRef.Equals(ptr2.NoRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                Assert.False(ptr1.ByRef.Equals(ptr2.NoRef, TypeCompareKind.ConsiderEverything));

                Assert.Equal(expectedConventionEquality, ptr1.NoRef.Signature.GetCallingConventionModifiers().SetEquals(ptr2.NoRef.Signature.GetCallingConventionModifiers()));
                Assert.Equal(expectedConventionEquality, ptr1.ByRef.Signature.GetCallingConventionModifiers().SetEquals(ptr2.ByRef.Signature.GetCallingConventionModifiers()));
                Assert.Equal(expectedConventionEquality, ptr1.NoRef.Equals(ptr2.NoRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                Assert.Equal(expectedConventionEquality, ptr1.ByRef.Equals(ptr2.ByRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                Assert.Equal(expectedFullEquality, ptr1.NoRef.Equals(ptr2.NoRef, TypeCompareKind.ConsiderEverything));
                Assert.Equal(expectedFullEquality, ptr1.ByRef.Equals(ptr2.ByRef, TypeCompareKind.ConsiderEverything));
            }

            (FunctionPointerTypeSymbol NoRef, FunctionPointerTypeSymbol ByRef) createTypeSymbol(ImmutableArray<CustomModifier> customModifiers, CallingConvention callingConvention = CallingConvention.Unmanaged)
                => (FunctionPointerTypeSymbol.CreateFromPartsForTests(
                        callingConvention,
                        TypeWithAnnotations.Create(returnType, customModifiers: customModifiers),
                        refCustomModifiers: default,
                        returnRefKind: RefKind.None,
                        parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty,
                        parameterRefCustomModifiers: default,
                        parameterRefKinds: ImmutableArray<RefKind>.Empty,
                        compilation: comp),
                    FunctionPointerTypeSymbol.CreateFromPartsForTests(
                        callingConvention,
                        TypeWithAnnotations.Create(returnType),
                        customModifiers,
                        RefKind.Ref,
                        parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty,
                        parameterRefCustomModifiers: default,
                        parameterRefKinds: ImmutableArray<RefKind>.Empty,
                        compilation: comp));
        }

        [Fact]
        public void Equality_DifferingRefAndTypeCustomModifiers()
        {
            var comp = CreateFunctionPointerCompilation("");

            var returnType = comp.GetSpecialType(SpecialType.System_String);

            var objectMod = CSharpCustomModifier.CreateOptional(comp.GetSpecialType(SpecialType.System_Object));
            var thiscallMod = CSharpCustomModifier.CreateOptional(comp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvThiscall"));
            var stdcallMod = CSharpCustomModifier.CreateOptional(comp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvStdcall"));

            var funcPtrThiscallOnTypeThiscallOnRef = createTypeSymbol(typeCustomModifiers: ImmutableArray.Create(thiscallMod), refCustomModifiers: ImmutableArray.Create(thiscallMod));
            var funcPtrThiscallOnTypeStdcallOnRef = createTypeSymbol(typeCustomModifiers: ImmutableArray.Create(thiscallMod), refCustomModifiers: ImmutableArray.Create(stdcallMod));
            var funcPtrStdcallOnTypeThiscallOnRef = createTypeSymbol(typeCustomModifiers: ImmutableArray.Create(stdcallMod), refCustomModifiers: ImmutableArray.Create(thiscallMod));

            verifyEquality(funcPtrThiscallOnTypeThiscallOnRef, funcPtrThiscallOnTypeStdcallOnRef, expectedTypeConventionEquality: true, expectedRefConventionEquality: false);
            verifyEquality(funcPtrThiscallOnTypeThiscallOnRef, funcPtrStdcallOnTypeThiscallOnRef, expectedTypeConventionEquality: false, expectedRefConventionEquality: true);
            verifyEquality(funcPtrThiscallOnTypeStdcallOnRef, funcPtrStdcallOnTypeThiscallOnRef, expectedTypeConventionEquality: false, expectedRefConventionEquality: false);

            static void verifyEquality((FunctionPointerTypeSymbol NoRef, FunctionPointerTypeSymbol ByRef) ptr1, (FunctionPointerTypeSymbol NoRef, FunctionPointerTypeSymbol ByRef) ptr2, bool expectedTypeConventionEquality, bool expectedRefConventionEquality)
            {
                // No equality between pointers with differing refkinds
                Assert.False(ptr1.NoRef.Equals(ptr2.ByRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                Assert.False(ptr1.NoRef.Equals(ptr2.ByRef, TypeCompareKind.ConsiderEverything));
                Assert.False(ptr1.ByRef.Equals(ptr2.NoRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                Assert.False(ptr1.ByRef.Equals(ptr2.NoRef, TypeCompareKind.ConsiderEverything));

                Assert.Equal(expectedTypeConventionEquality, ptr1.NoRef.Signature.GetCallingConventionModifiers().SetEquals(ptr2.NoRef.Signature.GetCallingConventionModifiers()));
                Assert.Equal(expectedRefConventionEquality, ptr1.ByRef.Signature.GetCallingConventionModifiers().SetEquals(ptr2.ByRef.Signature.GetCallingConventionModifiers()));

                Assert.Equal(expectedTypeConventionEquality, ptr1.NoRef.Equals(ptr2.NoRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                Assert.Equal(expectedRefConventionEquality, ptr1.ByRef.Equals(ptr2.ByRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
                // If we weren't expected the ref version to be equal, but we were expecting the type version to be equal, then that means
                // the type version will be identical because it will have no ref modifiers
                Assert.Equal(expectedTypeConventionEquality && !expectedRefConventionEquality, ptr1.NoRef.Equals(ptr2.NoRef, TypeCompareKind.ConsiderEverything));
                Assert.False(ptr1.ByRef.Equals(ptr2.ByRef, TypeCompareKind.ConsiderEverything));
            }

            (FunctionPointerTypeSymbol NoRef, FunctionPointerTypeSymbol ByRef) createTypeSymbol(ImmutableArray<CustomModifier> typeCustomModifiers, ImmutableArray<CustomModifier> refCustomModifiers, CallingConvention callingConvention = CallingConvention.Unmanaged)
                => (FunctionPointerTypeSymbol.CreateFromPartsForTests(
                        callingConvention,
                        TypeWithAnnotations.Create(returnType, customModifiers: typeCustomModifiers),
                        refCustomModifiers: default,
                        returnRefKind: RefKind.None,
                        parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty,
                        parameterRefCustomModifiers: default,
                        parameterRefKinds: ImmutableArray<RefKind>.Empty,
                        compilation: comp),
                    FunctionPointerTypeSymbol.CreateFromPartsForTests(
                        callingConvention,
                        TypeWithAnnotations.Create(returnType, customModifiers: typeCustomModifiers),
                        refCustomModifiers,
                        RefKind.Ref,
                        parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty,
                        parameterRefCustomModifiers: default,
                        parameterRefKinds: ImmutableArray<RefKind>.Empty,
                        compilation: comp));
        }
    }
}
