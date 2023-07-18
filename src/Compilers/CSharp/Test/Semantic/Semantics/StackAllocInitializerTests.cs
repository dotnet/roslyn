// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.StackAllocInitializer)]
    public class StackAllocInitializerTests : CompilingTestBase
    {
        [Fact, WorkItem(33945, "https://github.com/dotnet/roslyn/issues/33945")]
        public void RestrictedTypesAllowedInStackalloc()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
public ref struct RefS { }
public ref struct RefG<T> { public T field; }

class C
{
    unsafe void M()
    {
        var x1 = stackalloc RefS[10];
        var x2 = stackalloc RefG<string>[10];
        var x3 = stackalloc RefG<int>[10];
        var x4 = stackalloc System.TypedReference[10];
        var x5 = stackalloc System.ArgIterator[10];
        var x6 = stackalloc System.RuntimeArgumentHandle[10];

        var y1 = new RefS[10];
        var y2 = new RefG<string>[10];
        var y3 = new RefG<int>[10];
        var y4 = new System.TypedReference[10];
        var y5 = new System.ArgIterator[10];
        var y6 = new System.RuntimeArgumentHandle[10];

        RefS[] z1 = null;
        RefG<string>[] z2 = null;
        RefG<int>[] z3 = null;
        System.TypedReference[] z4 = null;
        System.ArgIterator[] z5 = null;
        System.RuntimeArgumentHandle[] z6 = null;
        _ = z1;
        _ = z2;
        _ = z3;
        _ = z4;
        _ = z5;
        _ = z6;
    }
}
", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (10,29): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('RefG<string>')
                //         var x2 = stackalloc RefG<string>[10];
                Diagnostic(ErrorCode.ERR_ManagedAddr, "RefG<string>").WithArguments("RefG<string>").WithLocation(10, 29),
                // (12,29): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('TypedReference')
                //         var x4 = stackalloc System.TypedReference[10];
                Diagnostic(ErrorCode.ERR_ManagedAddr, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(12, 29),
                // (16,22): error CS0611: Array elements cannot be of type 'RefS'
                //         var y1 = new RefS[10];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "RefS").WithArguments("RefS").WithLocation(16, 22),
                // (17,22): error CS0611: Array elements cannot be of type 'RefG<string>'
                //         var y2 = new RefG<string>[10];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "RefG<string>").WithArguments("RefG<string>").WithLocation(17, 22),
                // (18,22): error CS0611: Array elements cannot be of type 'RefG<int>'
                //         var y3 = new RefG<int>[10];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "RefG<int>").WithArguments("RefG<int>").WithLocation(18, 22),
                // (19,22): error CS0611: Array elements cannot be of type 'TypedReference'
                //         var y4 = new System.TypedReference[10];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(19, 22),
                // (20,22): error CS0611: Array elements cannot be of type 'ArgIterator'
                //         var y5 = new System.ArgIterator[10];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(20, 22),
                // (21,22): error CS0611: Array elements cannot be of type 'RuntimeArgumentHandle'
                //         var y6 = new System.RuntimeArgumentHandle[10];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "System.RuntimeArgumentHandle").WithArguments("System.RuntimeArgumentHandle").WithLocation(21, 22),
                // (23,9): error CS0611: Array elements cannot be of type 'RefS'
                //         RefS[] z1 = null;
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "RefS").WithArguments("RefS").WithLocation(23, 9),
                // (24,9): error CS0611: Array elements cannot be of type 'RefG<string>'
                //         RefG<string>[] z2 = null;
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "RefG<string>").WithArguments("RefG<string>").WithLocation(24, 9),
                // (25,9): error CS0611: Array elements cannot be of type 'RefG<int>'
                //         RefG<int>[] z3 = null;
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "RefG<int>").WithArguments("RefG<int>").WithLocation(25, 9),
                // (26,9): error CS0611: Array elements cannot be of type 'TypedReference'
                //         System.TypedReference[] z4 = null;
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(26, 9),
                // (27,9): error CS0611: Array elements cannot be of type 'ArgIterator'
                //         System.ArgIterator[] z5 = null;
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(27, 9),
                // (28,9): error CS0611: Array elements cannot be of type 'RuntimeArgumentHandle'
                //         System.RuntimeArgumentHandle[] z6 = null;
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "System.RuntimeArgumentHandle").WithArguments("System.RuntimeArgumentHandle").WithLocation(28, 9)
                );
        }

        [Fact]
        public void NoBestType_Pointer()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    struct A {}
    struct B {}

    void Method(dynamic d, RefStruct r)
    {
        var p0 = stackalloc[] { new A(), new B() };
        var p1 = stackalloc[] { };
        var p2 = stackalloc[] { VoidMethod() };
        var p3 = stackalloc[] { null };
        var p4 = stackalloc[] { (1, null) };
        var p5 = stackalloc[] { () => { } };
        var p6 = stackalloc[] { new {} , new { i = 0 } };
        var p7 = stackalloc[] { d };
        var p8 = stackalloc[] { _ };
    }

    public void VoidMethod() {}
}
namespace System {
    public struct ValueTuple<T1, T2> {
        public ValueTuple(T1 a, T2 b) => throw null;
    }
}
", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (7,28): error CS0246: The type or namespace name 'RefStruct' could not be found (are you missing a using directive or an assembly reference?)
                //     void Method(dynamic d, RefStruct r)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "RefStruct").WithArguments("RefStruct").WithLocation(7, 28),
                // (9,18): error CS0826: No best type found for implicitly-typed array
                //         var p0 = stackalloc[] { new A(), new B() };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { new A(), new B() }").WithLocation(9, 18),
                // (10,18): error CS0826: No best type found for implicitly-typed array
                //         var p1 = stackalloc[] { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { }").WithLocation(10, 18),
                // (11,18): error CS0826: No best type found for implicitly-typed array
                //         var p2 = stackalloc[] { VoidMethod() };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { VoidMethod() }").WithLocation(11, 18),
                // (12,18): error CS0826: No best type found for implicitly-typed array
                //         var p3 = stackalloc[] { null };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { null }").WithLocation(12, 18),
                // (13,18): error CS0826: No best type found for implicitly-typed array
                //         var p4 = stackalloc[] { (1, null) };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { (1, null) }").WithLocation(13, 18),
                // (14,18): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('Action')
                //         var p5 = stackalloc[] { () => { } };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "stackalloc[] { () => { } }").WithArguments("System.Action").WithLocation(14, 18),
                // (15,18): error CS0826: No best type found for implicitly-typed array
                //         var p6 = stackalloc[] { new {} , new { i = 0 } };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { new {} , new { i = 0 } }").WithLocation(15, 18),
                // (16,18): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('dynamic')
                //         var p7 = stackalloc[] { d };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "stackalloc[] { d }").WithArguments("dynamic").WithLocation(16, 18),
                // (17,33): error CS0103: The name '_' does not exist in the current context
                //         var p8 = stackalloc[] { _ };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(17, 33)
                );
        }

        [Fact]
        public void NoBestType_Span()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    struct A {}
    struct B {}

    void Method(dynamic d, bool c)
    {
        var p0 = c ? default : stackalloc[] { new A(), new B() };
        var p1 = c ? default : stackalloc[] { };
        var p2 = c ? default : stackalloc[] { VoidMethod() };
        var p3 = c ? default : stackalloc[] { null };
        var p4 = c ? default : stackalloc[] { (1, null) };
        var p5 = c ? default : stackalloc[] { () => { } };
        var p6 = c ? default : stackalloc[] { new {} , new { i = 0 } };
        var p7 = c ? default : stackalloc[] { d };
        var p8 = c ? default : stackalloc[] { _ };
    }

    public void VoidMethod() {}
}
namespace System {
    public struct ValueTuple<T1, T2> {
        public ValueTuple(T1 a, T2 b) => throw null;
    }
}
", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (9,32): error CS0826: No best type found for implicitly-typed array
                //         var p0 = c ? default : stackalloc[] { new A(), new B() };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { new A(), new B() }").WithLocation(9, 32),
                // (10,32): error CS0826: No best type found for implicitly-typed array
                //         var p1 = c ? default : stackalloc[] { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { }").WithLocation(10, 32),
                // (11,32): error CS0826: No best type found for implicitly-typed array
                //         var p2 = c ? default : stackalloc[] { VoidMethod() };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { VoidMethod() }").WithLocation(11, 32),
                // (12,32): error CS0826: No best type found for implicitly-typed array
                //         var p3 = c ? default : stackalloc[] { null };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { null }").WithLocation(12, 32),
                // (13,32): error CS0826: No best type found for implicitly-typed array
                //         var p4 = c ? default : stackalloc[] { (1, null) };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { (1, null) }").WithLocation(13, 32),
                // (14,32): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('Action')
                //         var p5 = c ? default : stackalloc[] { () => { } };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "stackalloc[] { () => { } }").WithArguments("System.Action").WithLocation(14, 32),
                // (15,32): error CS0826: No best type found for implicitly-typed array
                //         var p6 = c ? default : stackalloc[] { new {} , new { i = 0 } };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[] { new {} , new { i = 0 } }").WithLocation(15, 32),
                // (16,32): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('dynamic')
                //         var p7 = c ? default : stackalloc[] { d };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "stackalloc[] { d }").WithArguments("dynamic").WithLocation(16, 32),
                // (17,47): error CS0103: The name '_' does not exist in the current context
                //         var p8 = c ? default : stackalloc[] { _ };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(17, 47)
                );
        }

        [Fact]
        public void InitializeWithSelf_Pointer()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    void Method1()
    {
        var obj1 = stackalloc int[1] { obj1 };
        var obj2 = stackalloc int[ ] { obj2 };
        var obj3 = stackalloc    [ ] { obj3 };
    }

    void Method2()
    {
        var obj1 = stackalloc int[2] { obj1[0] , obj1[1] };
        var obj2 = stackalloc int[ ] { obj2[0] , obj2[1] };
        var obj3 = stackalloc    [ ] { obj3[0] , obj3[1] };
    }
}
", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (6,40): error CS0841: Cannot use local variable 'obj1' before it is declared
                //         var obj1 = stackalloc int[1] { obj1 };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj1").WithArguments("obj1").WithLocation(6, 40),
                // (7,40): error CS0841: Cannot use local variable 'obj2' before it is declared
                //         var obj2 = stackalloc int[ ] { obj2 };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj2").WithArguments("obj2").WithLocation(7, 40),
                // (8,40): error CS0841: Cannot use local variable 'obj3' before it is declared
                //         var obj3 = stackalloc    [ ] { obj3 };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj3").WithArguments("obj3").WithLocation(8, 40),
                // (13,40): error CS0841: Cannot use local variable 'obj1' before it is declared
                //         var obj1 = stackalloc int[2] { obj1[0] , obj1[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj1").WithArguments("obj1").WithLocation(13, 40),
                // (13,50): error CS0841: Cannot use local variable 'obj1' before it is declared
                //         var obj1 = stackalloc int[2] { obj1[0] , obj1[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj1").WithArguments("obj1").WithLocation(13, 50),
                // (14,40): error CS0841: Cannot use local variable 'obj2' before it is declared
                //         var obj2 = stackalloc int[ ] { obj2[0] , obj2[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj2").WithArguments("obj2").WithLocation(14, 40),
                // (14,50): error CS0841: Cannot use local variable 'obj2' before it is declared
                //         var obj2 = stackalloc int[ ] { obj2[0] , obj2[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj2").WithArguments("obj2").WithLocation(14, 50),
                // (15,40): error CS0841: Cannot use local variable 'obj3' before it is declared
                //         var obj3 = stackalloc    [ ] { obj3[0] , obj3[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj3").WithArguments("obj3").WithLocation(15, 40),
                // (15,50): error CS0841: Cannot use local variable 'obj3' before it is declared
                //         var obj3 = stackalloc    [ ] { obj3[0] , obj3[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj3").WithArguments("obj3").WithLocation(15, 50)
                );
        }

        [Fact]
        public void InitializeWithSelf_Span()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    void Method1(bool c)
    {
        var obj1 = c ? default : stackalloc int[1] { obj1 };
        var obj2 = c ? default : stackalloc int[ ] { obj2 };
        var obj3 = c ? default : stackalloc    [ ] { obj3 };
    }

    void Method2(bool c)
    {
        var obj1 = c ? default : stackalloc int[2] { obj1[0] , obj1[1] };
        var obj2 = c ? default : stackalloc int[ ] { obj2[0] , obj2[1] };
        var obj3 = c ? default : stackalloc    [ ] { obj3[0] , obj3[1] };
    }
}
", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (6,54): error CS0841: Cannot use local variable 'obj1' before it is declared
                //         var obj1 = c ? default : stackalloc int[1] { obj1 };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj1").WithArguments("obj1").WithLocation(6, 54),
                // (7,54): error CS0841: Cannot use local variable 'obj2' before it is declared
                //         var obj2 = c ? default : stackalloc int[ ] { obj2 };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj2").WithArguments("obj2").WithLocation(7, 54),
                // (8,54): error CS0841: Cannot use local variable 'obj3' before it is declared
                //         var obj3 = c ? default : stackalloc    [ ] { obj3 };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj3").WithArguments("obj3").WithLocation(8, 54),
                // (13,54): error CS0841: Cannot use local variable 'obj1' before it is declared
                //         var obj1 = c ? default : stackalloc int[2] { obj1[0] , obj1[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj1").WithArguments("obj1").WithLocation(13, 54),
                // (13,64): error CS0841: Cannot use local variable 'obj1' before it is declared
                //         var obj1 = c ? default : stackalloc int[2] { obj1[0] , obj1[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj1").WithArguments("obj1").WithLocation(13, 64),
                // (14,54): error CS0841: Cannot use local variable 'obj2' before it is declared
                //         var obj2 = c ? default : stackalloc int[ ] { obj2[0] , obj2[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj2").WithArguments("obj2").WithLocation(14, 54),
                // (14,64): error CS0841: Cannot use local variable 'obj2' before it is declared
                //         var obj2 = c ? default : stackalloc int[ ] { obj2[0] , obj2[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj2").WithArguments("obj2").WithLocation(14, 64),
                // (15,54): error CS0841: Cannot use local variable 'obj3' before it is declared
                //         var obj3 = c ? default : stackalloc    [ ] { obj3[0] , obj3[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj3").WithArguments("obj3").WithLocation(15, 54),
                // (15,64): error CS0841: Cannot use local variable 'obj3' before it is declared
                //         var obj3 = c ? default : stackalloc    [ ] { obj3[0] , obj3[1] };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "obj3").WithArguments("obj3").WithLocation(15, 64)
                );
        }

        [Fact]
        public void BadBestType_Pointer()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    ref struct S {}
    void Method1(S s)
    {
        var obj1 = stackalloc[] { """" };
        var obj2 = stackalloc[] { new {} };
        var obj3 = stackalloc[] { s }; // OK
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (7,20): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('string')
                //         var obj1 = stackalloc[] { "" };
                Diagnostic(ErrorCode.ERR_ManagedAddr, @"stackalloc[] { """" }").WithArguments("string").WithLocation(7, 20),
                // (8,20): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('<empty anonymous type>')
                //         var obj2 = stackalloc[] { new {} };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "stackalloc[] { new {} }").WithArguments("<empty anonymous type>").WithLocation(8, 20)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var expressions = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitStackAllocArrayCreationExpressionSyntax>().ToArray();
            Assert.Equal(3, expressions.Length);

            var @stackalloc = expressions[0];
            var stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.String*", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.String*", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            var element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Null(element0Info.Symbol);
            Assert.Equal("System.String", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.String", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element0Info.ImplicitConversion);

            @stackalloc = expressions[1];
            stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("<empty anonymous type>*", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("<empty anonymous type>*", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Equal("<empty anonymous type>..ctor()", element0Info.Symbol.ToTestDisplayString());
            Assert.Equal("<empty anonymous type>", element0Info.Type.ToTestDisplayString());
            Assert.Equal("<empty anonymous type>", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element0Info.ImplicitConversion);

            @stackalloc = expressions[2];
            stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("Test.S*", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("Test.S*", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Equal("Test.S s", element0Info.Symbol.ToTestDisplayString());
            Assert.Equal("Test.S", element0Info.Type.ToTestDisplayString());
            Assert.Equal("Test.S", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element0Info.ImplicitConversion);
        }

        [Fact]
        public void BadBestType_Span()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    ref struct S {}
    void Method1(S s, bool c)
    {
        var obj1 = c ? default : stackalloc[] { """" };
        var obj2 = c ? default : stackalloc[] { new {} };
        var obj3 = c ? default : stackalloc[] { s };
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (7,34): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('string')
                //         var obj1 = c ? default : stackalloc[] { "" };
                Diagnostic(ErrorCode.ERR_ManagedAddr, @"stackalloc[] { """" }").WithArguments("string").WithLocation(7, 34),
                // (8,34): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('<empty anonymous type>')
                //         var obj2 = c ? default : stackalloc[] { new {} };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "stackalloc[] { new {} }").WithArguments("<empty anonymous type>").WithLocation(8, 34),
                // (9,34): error CS0306: The type 'Test.S' may not be used as a type argument
                //         var obj3 = c ? default : stackalloc[] { s };
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "stackalloc[] { s }").WithArguments("Test.S").WithLocation(9, 34)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var expressions = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitStackAllocArrayCreationExpressionSyntax>().ToArray();
            Assert.Equal(3, expressions.Length);

            var @stackalloc = expressions[0];
            var stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Span<System.String>", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.String>", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            var element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Null(element0Info.Symbol);
            Assert.Equal("System.String", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.String", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element0Info.ImplicitConversion);

            @stackalloc = expressions[1];
            stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Span<<empty anonymous type>>", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<<empty anonymous type>>", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Equal("<empty anonymous type>..ctor()", element0Info.Symbol.ToTestDisplayString());
            Assert.Equal("<empty anonymous type>", element0Info.Type.ToTestDisplayString());
            Assert.Equal("<empty anonymous type>", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element0Info.ImplicitConversion);

            @stackalloc = expressions[2];
            stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Span<Test.S>", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<Test.S>", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Equal("Test.S s", element0Info.Symbol.ToTestDisplayString());
            Assert.Equal("Test.S", element0Info.Type.ToTestDisplayString());
            Assert.Equal("Test.S", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element0Info.ImplicitConversion);
        }

        [Fact]
        public void TestFor_Pointer()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    static void Method1()
    {
        int i = 0;
        for (var p = stackalloc int[3] { 1, 2, 3 }; i < 3; i++)
            Console.Write(p[i]);
    }

    static void Method2()
    {
        int i = 0;
        for (var p = stackalloc int[ ] { 1, 2, 3 }; i < 3; i++)
            Console.Write(p[i]);
    }

    static void Method3()
    {
        int i = 0;
        for (var p = stackalloc    [ ] { 1, 2, 3 }; i < 3; i++)
            Console.Write(p[i]);
    }
    
    public static void Main()
    {
        Method1();
        Method2();
        Method3();
    }
}", TestOptions.UnsafeReleaseExe);

            CompileAndVerify(comp, expectedOutput: "123123123", verify: Verification.Fails);
        }

        [Fact]
        public void TestFor_Span()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    static void Method1()
    {
        int i = 0;
        for (Span<int> p = stackalloc int[3] { 1, 2, 3 }; i < 3; i++)
            Console.Write(p[i]);
    }

    static void Method2()
    {
        int i = 0;
        for (Span<int> p = stackalloc int[ ] { 1, 2, 3 }; i < 3; i++)
            Console.Write(p[i]);
    }

    static void Method3()
    {
        int i = 0;
        for (Span<int> p = stackalloc    [ ] { 1, 2, 3 }; i < 3; i++)
            Console.Write(p[i]);
    }
    
    public static void Main()
    {
        Method1();
        Method2();
        Method3();
    }
}", TestOptions.DebugExe);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestForTernary()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
class Test
{
    static void Method1(bool b)
    {
        for (var p = b ? stackalloc int[3] { 1, 2, 3 } : default; false;) {}
        for (var p = b ? stackalloc int[ ] { 1, 2, 3 } : default; false;) {}
        for (var p = b ? stackalloc    [ ] { 1, 2, 3 } : default; false;) {}
    }
}", TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestLock()
        {
            var source = @"
class Test
{
    static void Method1()
    {
        lock (stackalloc int[3] { 1, 2, 3 }) {}
        lock (stackalloc int[ ] { 1, 2, 3 }) {}
        lock (stackalloc    [ ] { 1, 2, 3 }) {} 
    }
}";
            CreateCompilationWithMscorlibAndSpan(source, TestOptions.ReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (6,15): error CS8370: Feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         lock (stackalloc int[3] { 1, 2, 3 }) {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(6, 15),
                // (6,15): error CS0185: 'int*' is not a reference type as required by the lock statement
                //         lock (stackalloc int[3] { 1, 2, 3 }) {}
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "stackalloc int[3] { 1, 2, 3 }").WithArguments("int*").WithLocation(6, 15),
                // (7,15): error CS8370: Feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         lock (stackalloc int[ ] { 1, 2, 3 }) {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 15),
                // (7,15): error CS0185: 'int*' is not a reference type as required by the lock statement
                //         lock (stackalloc int[ ] { 1, 2, 3 }) {}
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "stackalloc int[ ] { 1, 2, 3 }").WithArguments("int*").WithLocation(7, 15),
                // (8,15): error CS8370: Feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         lock (stackalloc    [ ] { 1, 2, 3 }) {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(8, 15),
                // (8,15): error CS0185: 'int*' is not a reference type as required by the lock statement
                //         lock (stackalloc    [ ] { 1, 2, 3 }) {} 
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "stackalloc    [ ] { 1, 2, 3 }").WithArguments("int*").WithLocation(8, 15)
                );
            CreateCompilationWithMscorlibAndSpan(source, TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8)
                .VerifyDiagnostics(
                // (6,15): error CS0185: 'Span<int>' is not a reference type as required by the lock statement
                //         lock (stackalloc int[3] { 1, 2, 3 }) {}
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "stackalloc int[3] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(6, 15),
                // (7,15): error CS0185: 'Span<int>' is not a reference type as required by the lock statement
                //         lock (stackalloc int[ ] { 1, 2, 3 }) {}
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "stackalloc int[ ] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(7, 15),
                // (8,15): error CS0185: 'Span<int>' is not a reference type as required by the lock statement
                //         lock (stackalloc    [ ] { 1, 2, 3 }) {} 
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "stackalloc    [ ] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(8, 15)
                );
        }

        [Fact]
        public void TestSelect()
        {
            var source = @"
using System.Linq;
class Test
{
    static void Method1(int[] array)
    {
        var q1 = from item in array select stackalloc int[3] { 1, 2, 3 };
        var q2 = from item in array select stackalloc int[ ] { 1, 2, 3 };
        var q3 = from item in array select stackalloc    [ ] { 1, 2, 3 };
    }
}";
            CreateCompilationWithMscorlibAndSpan(source, TestOptions.ReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (7,44): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var q1 = from item in array select stackalloc int[3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 44),
                // (8,44): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var q2 = from item in array select stackalloc int[ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(8, 44),
                // (9,44): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var q3 = from item in array select stackalloc    [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(9, 44)
                );
            CreateCompilationWithMscorlibAndSpan(source, TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8)
                .VerifyDiagnostics(
                // (7,37): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         var q1 = from item in array select stackalloc int[3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "select stackalloc int[3] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(7, 37),
                // (7,44): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         var q1 = from item in array select stackalloc int[3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[3] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(7, 44),
                // (8,37): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         var q2 = from item in array select stackalloc int[ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "select stackalloc int[ ] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(8, 37),
                // (8,44): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         var q2 = from item in array select stackalloc int[ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[ ] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(8, 44),
                // (9,37): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         var q3 = from item in array select stackalloc    [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "select stackalloc    [ ] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(9, 37),
                // (9,44): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         var q3 = from item in array select stackalloc    [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc    [ ] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(9, 44)
                );
        }

        [Fact]
        public void TestLet()
        {
            var source = @"
using System.Linq;
class Test
{
    static void Method1(int[] array)
    {
        var q1 = from item in array let v = stackalloc int[3] { 1, 2, 3 } select v;
        var q2 = from item in array let v = stackalloc int[ ] { 1, 2, 3 } select v;
        var q3 = from item in array let v = stackalloc    [ ] { 1, 2, 3 } select v;
    }
}";
            CreateCompilationWithMscorlibAndSpan(source, TestOptions.ReleaseDll, parseOptions: TestOptions.Regular7_3)
                .VerifyDiagnostics(
                // (7,45): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var q1 = from item in array let v = stackalloc int[3] { 1, 2, 3 } select v;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 45),
                // (8,45): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var q2 = from item in array let v = stackalloc int[ ] { 1, 2, 3 } select v;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(8, 45),
                // (9,45): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var q3 = from item in array let v = stackalloc    [ ] { 1, 2, 3 } select v;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(9, 45)
                );
            CreateCompilationWithMscorlibAndSpan(source, TestOptions.ReleaseDll, parseOptions: TestOptions.Regular8)
                .VerifyDiagnostics(
                // (7,75): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         var q1 = from item in array let v = stackalloc int[3] { 1, 2, 3 } select v;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "select v").WithArguments("System.Span<int>").WithLocation(7, 75),
                // (8,75): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         var q2 = from item in array let v = stackalloc int[ ] { 1, 2, 3 } select v;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "select v").WithArguments("System.Span<int>").WithLocation(8, 75),
                // (9,75): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         var q3 = from item in array let v = stackalloc    [ ] { 1, 2, 3 } select v;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "select v").WithArguments("System.Span<int>").WithLocation(9, 75)
                );
        }

        [Fact]
        public void TestAwait_Pointer()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System.Threading.Tasks;
unsafe class Test
{
    async void M()
    {
        var p = stackalloc int[await Task.FromResult(1)] { await Task.FromResult(2) };
    }
}", TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (7,32): error CS4004: Cannot await in an unsafe context
                //         var p = stackalloc int[await Task.FromResult(1)] { await Task.FromResult(2) };
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.FromResult(1)").WithLocation(7, 32),
                // (7,60): error CS4004: Cannot await in an unsafe context
                //         var p = stackalloc int[await Task.FromResult(1)] { await Task.FromResult(2) };
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.FromResult(2)").WithLocation(7, 60)
                );
        }

        [Fact]
        public void TestAwait_Span()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
using System.Threading.Tasks;
class Test
{
    async void M()
    {
        Span<int> p = stackalloc int[await Task.FromResult(1)] { await Task.FromResult(2) };
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (8,38): error CS0150: A constant value is expected
                //         Span<int> p = stackalloc int[await Task.FromResult(1)] { await Task.FromResult(2) };
                Diagnostic(ErrorCode.ERR_ConstantExpected, "await Task.FromResult(1)").WithLocation(8, 38),
                // (8,9): error CS4012: Parameters or locals of type 'Span<int>' cannot be declared in async methods or async lambda expressions.
                //         Span<int> p = stackalloc int[await Task.FromResult(1)] { await Task.FromResult(2) };
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "Span<int>").WithArguments("System.Span<int>").WithLocation(8, 9)
                );
        }

        [Fact]
        public void TestSelfInSize()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    void M()
    {
        var x = stackalloc int[x] { };
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (6,32): error CS0841: Cannot use local variable 'x' before it is declared
                //         var x = stackalloc int[x] { };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(6, 32)
                );
        }

        [Fact]
        public void WrongLength()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    public void Method1()
    {
        var obj1 = stackalloc int[10] { };
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (6,20): error CS0847: An array initializer of length '10' is expected
                //         var obj1 = stackalloc int[10] { };
                Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[10] { }").WithArguments("10").WithLocation(6, 20)
                );
        }

        [Fact]
        public void NoInit()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    public void Method1()
    {
        var obj1 = stackalloc int[];
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (6,34): error CS1586: Array creation must have array size or array initializer
                //         var obj1 = stackalloc int[];
                Diagnostic(ErrorCode.ERR_MissingArraySize, "[]").WithLocation(6, 34)
                );
        }

        [Fact]
        public void NestedInit()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    public void Method1()
    {
        var obj1 = stackalloc int[1] { { 42 } };
        var obj2 = stackalloc int[ ] { { 42 } };
        var obj3 = stackalloc    [ ] { { 42 } };
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (6,40): error CS0623: Array initializers can only be used in a variable or field initializer. Try using a new expression instead.
                //         var obj1 = stackalloc int[1] { { 42 } };
                Diagnostic(ErrorCode.ERR_ArrayInitInBadPlace, "{ 42 }").WithLocation(6, 40),
                // (7,40): error CS0623: Array initializers can only be used in a variable or field initializer. Try using a new expression instead.
                //         var obj2 = stackalloc int[ ] { { 42 } };
                Diagnostic(ErrorCode.ERR_ArrayInitInBadPlace, "{ 42 }").WithLocation(7, 40),
                // (8,40): error CS0623: Array initializers can only be used in a variable or field initializer. Try using a new expression instead.
                //         var obj3 = stackalloc    [ ] { { 42 } };
                Diagnostic(ErrorCode.ERR_ArrayInitInBadPlace, "{ 42 }").WithLocation(8, 40)
                );
        }

        [Fact]
        public void AsStatement()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    public void Method1()
    {
        stackalloc[] {1};
        stackalloc int[] {1};
        stackalloc int[1] {1};
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         stackalloc[] {1};
                Diagnostic(ErrorCode.ERR_IllegalStatement, "stackalloc[] {1}").WithLocation(6, 9),
                // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         stackalloc int[] {1};
                Diagnostic(ErrorCode.ERR_IllegalStatement, "stackalloc int[] {1}").WithLocation(7, 9),
                // (8,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         stackalloc int[1] {1};
                Diagnostic(ErrorCode.ERR_IllegalStatement, "stackalloc int[1] {1}").WithLocation(8, 9)
                );
        }

        [Fact]
        public void BadRank()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    public void Method1()
    {
        var obj1 = stackalloc int[][] { 1 };
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (6,31): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('int[]')
                //         var obj1 = stackalloc int[][] { 1 };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "int").WithArguments("int[]").WithLocation(6, 31),
                // (6,31): error CS1575: A stackalloc expression requires [] after type
                //         var obj1 = stackalloc int[][] { 1 };
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int[][]").WithLocation(6, 31)
                );
        }

        [Fact]
        public void BadDimension()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    public void Method1()
    {
        var obj1 = stackalloc int[,] { 1 };
        var obj2 = stackalloc    [,] { 1 };
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (7,35): error CS8381: "Invalid rank specifier: expected ']'
                //         var obj2 = stackalloc    [,] { 1 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(7, 35),
                // (6,31): error CS1575: A stackalloc expression requires [] after type
                //         var obj1 = stackalloc int[,] { 1 };
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int[,]").WithLocation(6, 31)
                );
        }

        [Fact]
        public void TestFlowPass1()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    public static void Main()
    {
        int i, j, k;
        var obj1 = stackalloc int [1] { i = 1 };
        var obj2 = stackalloc int [ ] { j = 2 };
        var obj3 = stackalloc     [ ] { k = 3 };

        Console.Write(i);
        Console.Write(j);
        Console.Write(k);
    }
}", TestOptions.UnsafeReleaseExe);

            CompileAndVerify(comp, expectedOutput: "123");
        }

        [Fact]
        public void TestFlowPass2()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
unsafe class Test
{
    public static void Main()
    {
        int i, j, k;
        var obj1 = stackalloc int [1] { i };
        var obj2 = stackalloc int [ ] { j };
        var obj3 = stackalloc     [ ] { k };
    }
}", TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (7,41): error CS0165: Use of unassigned local variable 'i'
                //         var obj1 = stackalloc int [1] { i };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i").WithLocation(7, 41),
                // (8,41): error CS0165: Use of unassigned local variable 'j'
                //         var obj2 = stackalloc int [ ] { j };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "j").WithArguments("j").WithLocation(8, 41),
                // (9,41): error CS0165: Use of unassigned local variable 'k'
                //         var obj3 = stackalloc     [ ] { k };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "k").WithArguments("k").WithLocation(9, 41)
                );
        }

        [Fact]
        public void ConversionFromPointerStackAlloc_UserDefined_Implicit()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    public void Method1()
    {
        Test obj1 = stackalloc int[3] { 1, 2, 3 };
        var obj2 = stackalloc int[3] { 1, 2, 3 };
        Span<int> obj3 = stackalloc int[3] { 1, 2, 3 };
        int* obj4 = stackalloc int[3] { 1, 2, 3 };
        double* obj5 = stackalloc int[3] { 1, 2, 3 };
    }
    
    public void Method2()
    {
        Test obj1 = stackalloc int[] { 1, 2, 3 };
        var obj2 = stackalloc int[] { 1, 2, 3 };
        Span<int> obj3 = stackalloc int[] { 1, 2, 3 };
        int* obj4 = stackalloc int[] { 1, 2, 3 };
        double* obj5 = stackalloc int[] { 1, 2, 3 };
    }

    public void Method3()
    {
        Test obj1 = stackalloc[] { 1, 2, 3 };
        var obj2 = stackalloc[] { 1, 2, 3 };
        Span<int> obj3 = stackalloc[] { 1, 2, 3 };
        int* obj4 = stackalloc[] { 1, 2, 3 };
        double* obj5 = stackalloc[] { 1, 2, 3 };
    }

    public static implicit operator Test(int* value) 
    {
        return default(Test);
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (11,24): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double*' is not possible.
                //         double* obj5 = stackalloc int[3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[3] { 1, 2, 3 }").WithArguments("int", "double*").WithLocation(11, 24),
                // (20,24): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double*' is not possible.
                //         double* obj5 = stackalloc int[] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[] { 1, 2, 3 }").WithArguments("int", "double*").WithLocation(20, 24),
                // (29,24): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double*' is not possible.
                //         double* obj5 = stackalloc[] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc[] { 1, 2, 3 }").WithArguments("int", "double*").WithLocation(29, 24)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var variables = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>();
            Assert.Equal(15, variables.Count());

            for (int i = 0; i < 15; i += 5)
            {
                var obj1 = variables.ElementAt(i);
                Assert.Equal("obj1", obj1.Identifier.Text);

                var obj1Value = model.GetSemanticInfoSummary(obj1.Initializer.Value);
                Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj1Value.Type).PointedAtType.SpecialType);
                Assert.Equal("Test", obj1Value.ConvertedType.Name);
                Assert.Equal(ConversionKind.ImplicitUserDefined, obj1Value.ImplicitConversion.Kind);

                var obj2 = variables.ElementAt(i + 1);
                Assert.Equal("obj2", obj2.Identifier.Text);

                var obj2Value = model.GetSemanticInfoSummary(obj2.Initializer.Value);
                Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj2Value.Type).PointedAtType.SpecialType);
                Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj2Value.ConvertedType).PointedAtType.SpecialType);
                Assert.Equal(ConversionKind.Identity, obj2Value.ImplicitConversion.Kind);

                var obj3 = variables.ElementAt(i + 2);
                Assert.Equal("obj3", obj3.Identifier.Text);

                var obj3Value = model.GetSemanticInfoSummary(obj3.Initializer.Value);
                Assert.Equal("Span", obj3Value.Type.Name);
                Assert.Equal("Span", obj3Value.ConvertedType.Name);
                Assert.Equal(ConversionKind.Identity, obj3Value.ImplicitConversion.Kind);

                var obj4 = variables.ElementAt(i + 3);
                Assert.Equal("obj4", obj4.Identifier.Text);

                var obj4Value = model.GetSemanticInfoSummary(obj4.Initializer.Value);
                Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj4Value.Type).PointedAtType.SpecialType);
                Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj4Value.ConvertedType).PointedAtType.SpecialType);
                Assert.Equal(ConversionKind.Identity, obj4Value.ImplicitConversion.Kind);

                var obj5 = variables.ElementAt(i + 4);
                Assert.Equal("obj5", obj5.Identifier.Text);

                var obj5Value = model.GetSemanticInfoSummary(obj5.Initializer.Value);
                Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj5Value.Type).PointedAtType.SpecialType);
                Assert.Equal(SpecialType.System_Double, ((IPointerTypeSymbol)obj5Value.ConvertedType).PointedAtType.SpecialType);
                Assert.Equal(ConversionKind.NoConversion, obj5Value.ImplicitConversion.Kind);
            }
        }

        [Fact]
        public void ConversionFromPointerStackAlloc_UserDefined_Explicit()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    public void Method1()
    {
        Test obj1 = (Test)stackalloc int[3]  { 1, 2, 3 };
        var obj2 = stackalloc int[3] { 1, 2, 3 };
        Span<int> obj3 = stackalloc int[3] { 1, 2, 3 };
        int* obj4 = stackalloc int[3] { 1, 2, 3 };
        double* obj5 = stackalloc int[3] { 1, 2, 3 };
    }
    
    public void Method2()
    {
        Test obj1 = (Test)stackalloc int[]  { 1, 2, 3 };
        var obj2 = stackalloc int[] { 1, 2, 3 };
        Span<int> obj3 = stackalloc int[] { 1, 2, 3 };
        int* obj4 = stackalloc int[] { 1, 2, 3 };
        double* obj5 = stackalloc int[] { 1, 2, 3 };
    }

    public void Method3()
    {
        Test obj1 = (Test)stackalloc []  { 1, 2, 3 };
        var obj2 = stackalloc[] { 1, 2, 3 };
        Span<int> obj3 = stackalloc [] { 1, 2, 3 };
        int* obj4 = stackalloc[] { 1, 2, 3 };
        double* obj5 = stackalloc[] { 1, 2, 3 };
    }

    public static explicit operator Test(Span<int> value) 
    {
        return default(Test);
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (11,24): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double*' is not possible.
                //         double* obj5 = stackalloc int[3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[3] { 1, 2, 3 }").WithArguments("int", "double*").WithLocation(11, 24),
                // (20,24): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double*' is not possible.
                //         double* obj5 = stackalloc int[] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[] { 1, 2, 3 }").WithArguments("int", "double*").WithLocation(20, 24),
                // (29,24): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double*' is not possible.
                //         double* obj5 = stackalloc[] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc[] { 1, 2, 3 }").WithArguments("int", "double*").WithLocation(29, 24)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var variables = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>();
            Assert.Equal(15, variables.Count());

            for (int i = 0; i < 15; i += 5)
            {
                var obj1 = variables.ElementAt(i);
                Assert.Equal("obj1", obj1.Identifier.Text);
                Assert.Equal(SyntaxKind.CastExpression, obj1.Initializer.Value.Kind());

                var obj1Value = model.GetSemanticInfoSummary(((CastExpressionSyntax)obj1.Initializer.Value).Expression);
                Assert.Equal("Span", obj1Value.Type.Name);
                Assert.Equal("Span", obj1Value.ConvertedType.Name);
                Assert.Equal(ConversionKind.Identity, obj1Value.ImplicitConversion.Kind);

                var obj2 = variables.ElementAt(i + 1);
                Assert.Equal("obj2", obj2.Identifier.Text);

                var obj2Value = model.GetSemanticInfoSummary(obj2.Initializer.Value);
                Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj2Value.Type).PointedAtType.SpecialType);
                Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj2Value.ConvertedType).PointedAtType.SpecialType);
                Assert.Equal(ConversionKind.Identity, obj2Value.ImplicitConversion.Kind);

                var obj3 = variables.ElementAt(i + 2);
                Assert.Equal("obj3", obj3.Identifier.Text);

                var obj3Value = model.GetSemanticInfoSummary(obj3.Initializer.Value);
                Assert.Equal("Span", obj3Value.Type.Name);
                Assert.Equal("Span", obj3Value.ConvertedType.Name);
                Assert.Equal(ConversionKind.Identity, obj3Value.ImplicitConversion.Kind);

                var obj4 = variables.ElementAt(i + 3);
                Assert.Equal("obj4", obj4.Identifier.Text);

                var obj4Value = model.GetSemanticInfoSummary(obj4.Initializer.Value);
                Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj4Value.Type).PointedAtType.SpecialType);
                Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj4Value.ConvertedType).PointedAtType.SpecialType);
                Assert.Equal(ConversionKind.Identity, obj4Value.ImplicitConversion.Kind);

                var obj5 = variables.ElementAt(i + 4);
                Assert.Equal("obj5", obj5.Identifier.Text);

                var obj5Value = model.GetSemanticInfoSummary(obj5.Initializer.Value);
                Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj5Value.Type).PointedAtType.SpecialType);
                Assert.Equal(SpecialType.System_Double, ((IPointerTypeSymbol)obj5Value.ConvertedType).PointedAtType.SpecialType);
                Assert.Equal(ConversionKind.NoConversion, obj5Value.ImplicitConversion.Kind);
            }
        }

        [Fact]
        public void ConversionError()
        {
            CreateCompilationWithMscorlibAndSpan(@"
class Test
{
    void Method1()
    {
        double x = stackalloc int[3] { 1, 2, 3 };        // implicit
        short y = (short)stackalloc int[3] { 1, 2, 3 };  // explicit
    }

    void Method2()
    {
        double x = stackalloc int[] { 1, 2, 3 };          // implicit
        short y = (short)stackalloc int[] { 1, 2, 3 };    // explicit
    }

    void Method3()
    {
        double x = stackalloc[] { 1, 2, 3 };              // implicit
        short y = (short)stackalloc[] { 1, 2, 3 };        // explicit
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics(

                // (6,20): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double' is not possible.
                //         double x = stackalloc int[3] { 1, 2, 3 };        // implicit
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[3] { 1, 2, 3 }").WithArguments("int", "double").WithLocation(6, 20),
                // (7,19): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'short' is not possible.
                //         short y = (short)stackalloc int[3] { 1, 2, 3 };  // explicit
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "(short)stackalloc int[3] { 1, 2, 3 }").WithArguments("int", "short").WithLocation(7, 19),
                // (12,20): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double' is not possible.
                //         double x = stackalloc int[] { 1, 2, 3 };          // implicit
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[] { 1, 2, 3 }").WithArguments("int", "double").WithLocation(12, 20),
                // (13,19): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'short' is not possible.
                //         short y = (short)stackalloc int[] { 1, 2, 3 };    // explicit
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "(short)stackalloc int[] { 1, 2, 3 }").WithArguments("int", "short").WithLocation(13, 19),
                // (18,20): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double' is not possible.
                //         double x = stackalloc[] { 1, 2, 3 };          // implicit
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc[] { 1, 2, 3 }").WithArguments("int", "double").WithLocation(18, 20),
                // (19,19): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'short' is not possible.
                //         short y = (short)stackalloc[] { 1, 2, 3 };    // explicit
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "(short)stackalloc[] { 1, 2, 3 }").WithArguments("int", "short").WithLocation(19, 19)
                );
        }

        [Fact]
        public void MissingSpanType()
        {
            CreateCompilation(@"
class Test
{
    void M()
    {
        Span<int> a1 = stackalloc int [3] { 1, 2, 3 };
        Span<int> a2 = stackalloc int [ ] { 1, 2, 3 };
        Span<int> a3 = stackalloc     [ ] { 1, 2, 3 };
    }
}").VerifyDiagnostics(
                // (6,9): error CS0246: The type or namespace name 'Span<>' could not be found (are you missing a using directive or an assembly reference?)
                //         Span<int> a1 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Span<int>").WithArguments("Span<>").WithLocation(6, 9),
                // (6,24): error CS0518: Predefined type 'System.Span`1' is not defined or imported
                //         Span<int> a1 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "stackalloc int [3] { 1, 2, 3 }").WithArguments("System.Span`1").WithLocation(6, 24),
                // (7,9): error CS0246: The type or namespace name 'Span<>' could not be found (are you missing a using directive or an assembly reference?)
                //         Span<int> a2 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Span<int>").WithArguments("Span<>").WithLocation(7, 9),
                // (7,24): error CS0518: Predefined type 'System.Span`1' is not defined or imported
                //         Span<int> a2 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "stackalloc int [ ] { 1, 2, 3 }").WithArguments("System.Span`1").WithLocation(7, 24),
                // (8,9): error CS0246: The type or namespace name 'Span<>' could not be found (are you missing a using directive or an assembly reference?)
                //         Span<int> a3 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Span<int>").WithArguments("Span<>").WithLocation(8, 9),
                // (8,24): error CS0518: Predefined type 'System.Span`1' is not defined or imported
                //         Span<int> a3 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "stackalloc     [ ] { 1, 2, 3 }").WithArguments("System.Span`1").WithLocation(8, 24)
                );
        }

        [Fact]
        public void MissingSpanConstructor()
        {
            CreateCompilation(@"
namespace System
{
    ref struct Span<T>
    {
    }
    class Test
    {
        void M()
        {
            Span<int> a1 = stackalloc int [3] { 1, 2, 3 };
            Span<int> a2 = stackalloc int [ ] { 1, 2, 3 };
            Span<int> a3 = stackalloc     [ ] { 1, 2, 3 };
        }
    }
}").VerifyEmitDiagnostics(
                // (11,28): error CS0656: Missing compiler required member 'System.Span`1..ctor'
                //             Span<int> a1 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "stackalloc int [3] { 1, 2, 3 }").WithArguments("System.Span`1", ".ctor").WithLocation(11, 28),
                // (12,28): error CS0656: Missing compiler required member 'System.Span`1..ctor'
                //             Span<int> a2 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "stackalloc int [ ] { 1, 2, 3 }").WithArguments("System.Span`1", ".ctor").WithLocation(12, 28),
                // (13,28): error CS0656: Missing compiler required member 'System.Span`1..ctor'
                //             Span<int> a3 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "stackalloc     [ ] { 1, 2, 3 }").WithArguments("System.Span`1", ".ctor").WithLocation(13, 28)
                );
        }

        [Fact]
        public void ConditionalExpressionOnSpan_BothStackallocSpans()
        {
            CreateCompilationWithMscorlibAndSpan(@"
class Test
{
    void M()
    {
        var x1 = true ? stackalloc int [3] { 1, 2, 3 } : stackalloc int [3] { 1, 2, 3 };
        var x2 = true ? stackalloc int [ ] { 1, 2, 3 } : stackalloc int [ ] { 1, 2, 3 };
        var x3 = true ? stackalloc     [ ] { 1, 2, 3 } : stackalloc     [ ] { 1, 2, 3 };
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void ConditionalExpressionOnSpan_Convertible()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    void M()
    {
        var x1 = true ? stackalloc int [3] { 1, 2, 3 } : (Span<int>)stackalloc int [3] { 1, 2, 3 };
        var x2 = true ? stackalloc int [ ] { 1, 2, 3 } : (Span<int>)stackalloc int [ ] { 1, 2, 3 };
        var x3 = true ? stackalloc     [ ] { 1, 2, 3 } : (Span<int>)stackalloc     [ ] { 1, 2, 3 };
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void ConditionalExpressionOnSpan_NoCast()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    void M()
    {
        var x1 = true ? stackalloc int [3] { 1, 2, 3, } : (Span<int>)stackalloc short [3] { (short)1, (short)2, (short)3 };
        var x2 = true ? stackalloc int [ ] { 1, 2, 3, } : (Span<int>)stackalloc short [ ] { (short)1, (short)2, (short)3 };
        var x3 = true ? stackalloc     [ ] { 1, 2, 3, } : (Span<int>)stackalloc       [ ] { (short)1, (short)2, (short)3 };
    } 
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,59): error CS8346: Conversion of a stackalloc expression of type 'short' to type 'Span<int>' is not possible.
                //         var x1 = true ? stackalloc int [3] { 1, 2, 3, } : (Span<int>)stackalloc short [3] { (short)1, (short)2, (short)3 };
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "(Span<int>)stackalloc short [3] { (short)1, (short)2, (short)3 }").WithArguments("short", "System.Span<int>").WithLocation(7, 59),
                // (8,59): error CS8346: Conversion of a stackalloc expression of type 'short' to type 'Span<int>' is not possible.
                //         var x2 = true ? stackalloc int [ ] { 1, 2, 3, } : (Span<int>)stackalloc short [ ] { (short)1, (short)2, (short)3 };
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "(Span<int>)stackalloc short [ ] { (short)1, (short)2, (short)3 }").WithArguments("short", "System.Span<int>").WithLocation(8, 59),
                // (9,59): error CS8346: Conversion of a stackalloc expression of type 'short' to type 'Span<int>' is not possible.
                //         var x3 = true ? stackalloc     [ ] { 1, 2, 3, } : (Span<int>)stackalloc       [ ] { (short)1, (short)2, (short)3 };
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "(Span<int>)stackalloc       [ ] { (short)1, (short)2, (short)3 }").WithArguments("short", "System.Span<int>").WithLocation(9, 59)
                );
        }

        [Fact]
        public void ConditionalExpressionOnSpan_CompatibleTypes()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    void M()
    {
        Span<int> a1 = stackalloc int [3] { 1, 2, 3 };
        Span<int> a2 = stackalloc int [ ] { 1, 2, 3 };
        Span<int> a3 = stackalloc     [ ] { 1, 2, 3 };

        var x1 = true ? stackalloc int [3] { 1, 2, 3 } : a1;
        var x2 = true ? stackalloc int [ ] { 1, 2, 3 } : a2;
        var x3 = true ? stackalloc     [ ] { 1, 2, 3 } : a3;
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void ConditionalExpressionOnSpan_IncompatibleTypes()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    void M()
    {
        Span<short> a = stackalloc short [10];
        var x1 = true ? stackalloc int [3] { 1, 2, 3 } : a;
        var x2 = true ? stackalloc int [ ] { 1, 2, 3 } : a;
        var x3 = true ? stackalloc     [ ] { 1, 2, 3 } : a;
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,18): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'System.Span<int>' and 'System.Span<short>'
                //         var x1 = true ? stackalloc int [3] { 1, 2, 3 } : a;
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? stackalloc int [3] { 1, 2, 3 } : a").WithArguments("System.Span<int>", "System.Span<short>").WithLocation(8, 18),
                // (9,18): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'System.Span<int>' and 'System.Span<short>'
                //         var x2 = true ? stackalloc int [ ] { 1, 2, 3 } : a;
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? stackalloc int [ ] { 1, 2, 3 } : a").WithArguments("System.Span<int>", "System.Span<short>").WithLocation(9, 18),
                // (10,18): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'System.Span<int>' and 'System.Span<short>'
                //         var x3 = true ? stackalloc     [ ] { 1, 2, 3 } : a;
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? stackalloc     [ ] { 1, 2, 3 } : a").WithArguments("System.Span<int>", "System.Span<short>").WithLocation(10, 18)
                );
        }

        [Fact]
        public void ConditionalExpressionOnSpan_Nested()
        {
            CreateCompilationWithMscorlibAndSpan(@"
class Test
{
    bool N() => true;

    void M()
    {
        var x = N()
            ? N()
                ? stackalloc int[1] { 42 }
                : stackalloc int[ ] { 42 }
            : N()
                ? stackalloc[] { 42 }
                : N()
                    ? stackalloc int[2]
                    : stackalloc int[3];
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void BooleanOperatorOnSpan_NoTargetTyping()
        {
            CreateCompilationWithMscorlibAndSpan(@"
class Test
{
    void M()
    {
        if (stackalloc int[3] { 1, 2, 3 } == stackalloc int[3] { 1, 2, 3 }) { }
        if (stackalloc int[ ] { 1, 2, 3 } == stackalloc int[ ] { 1, 2, 3 }) { }
        if (stackalloc    [ ] { 1, 2, 3 } == stackalloc    [ ] { 1, 2, 3 }) { }
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,13): error CS0019: Operator '==' cannot be applied to operands of type 'Span<int>' and 'Span<int>'
                //         if (stackalloc int[3] { 1, 2, 3 } == stackalloc int[3] { 1, 2, 3 }) { }
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "stackalloc int[3] { 1, 2, 3 } == stackalloc int[3] { 1, 2, 3 }").WithArguments("==", "System.Span<int>", "System.Span<int>").WithLocation(6, 13),
                // (7,13): error CS0019: Operator '==' cannot be applied to operands of type 'Span<int>' and 'Span<int>'
                //         if (stackalloc int[ ] { 1, 2, 3 } == stackalloc int[ ] { 1, 2, 3 }) { }
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "stackalloc int[ ] { 1, 2, 3 } == stackalloc int[ ] { 1, 2, 3 }").WithArguments("==", "System.Span<int>", "System.Span<int>").WithLocation(7, 13),
                // (8,13): error CS0019: Operator '==' cannot be applied to operands of type 'Span<int>' and 'Span<int>'
                //         if (stackalloc    [ ] { 1, 2, 3 } == stackalloc    [ ] { 1, 2, 3 }) { }
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "stackalloc    [ ] { 1, 2, 3 } == stackalloc    [ ] { 1, 2, 3 }").WithArguments("==", "System.Span<int>", "System.Span<int>").WithLocation(8, 13)
            );
        }

        [Fact]
        public void StackAllocInitializerSyntaxProducesErrorsOnEarlierVersions()
        {
            var parseOptions = new CSharpParseOptions().WithLanguageVersion(LanguageVersion.CSharp7);

            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    void M()
    {
        Span<int> x1 = stackalloc int [3] { 1, 2, 3 };
        Span<int> x2 = stackalloc int [ ] { 1, 2, 3 };
        Span<int> x3 = stackalloc     [ ] { 1, 2, 3 };
    }
}", options: TestOptions.UnsafeReleaseDll, parseOptions: parseOptions).VerifyDiagnostics(
            // (7,24): error CS8107: Feature 'stackalloc initializer' is not available in C# 7.0. Please use language version 7.3 or greater.
            //         Span<int> x1 = stackalloc int [3] { 1, 2, 3 };
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc").WithArguments("stackalloc initializer", "7.3").WithLocation(7, 24),
            // (7,24): error CS8107: Feature 'ref structs' is not available in C# 7.0. Please use language version 7.2 or greater.
            //         Span<int> x1 = stackalloc int [3] { 1, 2, 3 };
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc int [3] { 1, 2, 3 }").WithArguments("ref structs", "7.2").WithLocation(7, 24),
            // (8,24): error CS8107: Feature 'stackalloc initializer' is not available in C# 7.0. Please use language version 7.3 or greater.
            //         Span<int> x2 = stackalloc int [ ] { 1, 2, 3 };
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc").WithArguments("stackalloc initializer", "7.3").WithLocation(8, 24),
            // (8,24): error CS8107: Feature 'ref structs' is not available in C# 7.0. Please use language version 7.2 or greater.
            //         Span<int> x2 = stackalloc int [ ] { 1, 2, 3 };
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc int [ ] { 1, 2, 3 }").WithArguments("ref structs", "7.2").WithLocation(8, 24),
            // (9,24): error CS8107: Feature 'stackalloc initializer' is not available in C# 7.0. Please use language version 7.3 or greater.
            //         Span<int> x3 = stackalloc     [ ] { 1, 2, 3 };
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc").WithArguments("stackalloc initializer", "7.3").WithLocation(9, 24),
            // (9,24): error CS8107: Feature 'ref structs' is not available in C# 7.0. Please use language version 7.2 or greater.
            //         Span<int> x3 = stackalloc     [ ] { 1, 2, 3 };
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc     [ ] { 1, 2, 3 }").WithArguments("ref structs", "7.2").WithLocation(9, 24));
        }

        [Fact]
        public void StackAllocSyntaxProducesUnsafeErrorInSafeCode()
        {
            CreateCompilation(@"
class Test
{
    void M()
    {
        var x1 = stackalloc int [3] { 1, 2, 3 };
        var x2 = stackalloc int [ ] { 1, 2, 3 };
        var x3 = stackalloc     [ ] { 1, 2, 3 };
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var x1 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int [3] { 1, 2, 3 }").WithLocation(6, 18),
                // (7,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var x2 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(7, 18),
                // (8,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var x3 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(8, 18)
                );
        }

        [Fact]
        public void StackAllocInUsing1()
        {
            var test = @"
public class Test
{
    unsafe public static void Main()
    {
        using (var v1 = stackalloc int [3] { 1, 2, 3 })
        using (var v2 = stackalloc int [ ] { 1, 2, 3 })
        using (var v3 = stackalloc     [ ] { 1, 2, 3 })
        {
        }
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (6,16): error CS1674: 'Span<int>': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (var v1 = stackalloc int [3] { 1, 2, 3 })
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var v1 = stackalloc int [3] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(6, 16),
                // (7,16): error CS1674: 'Span<int>': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (var v2 = stackalloc int [ ] { 1, 2, 3 })
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var v2 = stackalloc int [ ] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(7, 16),
                // (8,16): error CS1674: 'Span<int>': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (var v3 = stackalloc     [ ] { 1, 2, 3 })
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var v3 = stackalloc     [ ] { 1, 2, 3 }").WithArguments("System.Span<int>").WithLocation(8, 16)
                );
        }

        [Fact]
        public void StackAllocInUsing2()
        {
            var test = @"
public class Test
{
    unsafe public static void Main()
    {
        using (System.IDisposable v1 = stackalloc int [3] { 1, 2, 3 })
        using (System.IDisposable v2 = stackalloc int [ ] { 1, 2, 3 })
        using (System.IDisposable v3 = stackalloc     [ ] { 1, 2, 3 })
        {
        }
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (6,40): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'IDisposable' is not possible.
                //         using (System.IDisposable v1 = stackalloc int [3] { 1, 2, 3 })
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int [3] { 1, 2, 3 }").WithArguments("int", "System.IDisposable").WithLocation(6, 40),
                // (7,40): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'IDisposable' is not possible.
                //         using (System.IDisposable v2 = stackalloc int [ ] { 1, 2, 3 })
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int [ ] { 1, 2, 3 }").WithArguments("int", "System.IDisposable").WithLocation(7, 40),
                // (8,40): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'IDisposable' is not possible.
                //         using (System.IDisposable v3 = stackalloc     [ ] { 1, 2, 3 })
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc     [ ] { 1, 2, 3 }").WithArguments("int", "System.IDisposable").WithLocation(8, 40)
                );
        }

        [Fact]
        public void StackAllocInFixed()
        {
            var test = @"
public class Test
{
    unsafe public static void Main()
    {
        fixed (int* v1 = stackalloc int [3] { 1, 2, 3 })
        fixed (int* v2 = stackalloc int [ ] { 1, 2, 3 })
        fixed (int* v3 = stackalloc     [ ] { 1, 2, 3 })
        {
        }
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (6,26): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* v1 = stackalloc int [3] { 1, 2, 3 })
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "stackalloc int [3] { 1, 2, 3 }").WithLocation(6, 26),
                // (7,26): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* v2 = stackalloc int [ ] { 1, 2, 3 })
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(7, 26),
                // (8,26): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* v3 = stackalloc     [ ] { 1, 2, 3 })
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(8, 26)
                );
        }

        [Fact]
        public void ConstStackAllocExpression()
        {
            var test = @"
unsafe public class Test
{
    void M()
    {
        const int* p1 = stackalloc int [3] { 1, 2, 3 };
        const int* p2 = stackalloc int [ ] { 1, 2, 3 };
        const int* p3 = stackalloc     [ ] { 1, 2, 3 };
    }
}
";
            CreateCompilation(test, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (6,15): error CS0283: The type 'int*' cannot be declared const
                //         const int* p1 = stackalloc int[1] { 1 };
                Diagnostic(ErrorCode.ERR_BadConstType, "int*").WithArguments("int*").WithLocation(6, 15),
                // (7,15): error CS0283: The type 'int*' cannot be declared const
                //         const int* p2 = stackalloc int[] { 1 };
                Diagnostic(ErrorCode.ERR_BadConstType, "int*").WithArguments("int*").WithLocation(7, 15),
                // (8,15): error CS0283: The type 'int*' cannot be declared const
                //         const int* p3 = stackalloc [] { 1 };
                Diagnostic(ErrorCode.ERR_BadConstType, "int*").WithArguments("int*").WithLocation(8, 15)
                );
        }

        [Fact]
        public void RefStackAllocAssignment_ValueToRef()
        {
            var test = @"
using System;
public class Test
{
    void M()
    {
        ref Span<int> p1 = stackalloc int [3] { 1, 2, 3 };
        ref Span<int> p2 = stackalloc int [ ] { 1, 2, 3 };
        ref Span<int> p3 = stackalloc     [ ] { 1, 2, 3 };
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (7,23): error CS8172: Cannot initialize a by-reference variable with a value
                //         ref Span<int> p1 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "p1 = stackalloc int [3] { 1, 2, 3 }").WithLocation(7, 23),
                // (7,28): error CS1510: A ref or out value must be an assignable variable
                //         ref Span<int> p1 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "stackalloc int [3] { 1, 2, 3 }").WithLocation(7, 28),
                // (8,23): error CS8172: Cannot initialize a by-reference variable with a value
                //         ref Span<int> p2 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "p2 = stackalloc int [ ] { 1, 2, 3 }").WithLocation(8, 23),
                // (8,28): error CS1510: A ref or out value must be an assignable variable
                //         ref Span<int> p2 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(8, 28),
                // (9,23): error CS8172: Cannot initialize a by-reference variable with a value
                //         ref Span<int> p3 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "p3 = stackalloc     [ ] { 1, 2, 3 }").WithLocation(9, 23),
                // (9,28): error CS1510: A ref or out value must be an assignable variable
                //         ref Span<int> p3 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(9, 28)
                );
        }

        [Fact]
        public void RefStackAllocAssignment_RefToRef()
        {
            var test = @"
using System;
public class Test
{
    void M()
    {
        ref Span<int> p1 = ref stackalloc int [3] { 1, 2, 3 };
        ref Span<int> p2 = ref stackalloc int [ ] { 1, 2, 3 };
        ref Span<int> p3 = ref stackalloc     [ ] { 1, 2, 3 };
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (7,32): error CS1510: A ref or out value must be an assignable variable
                //         ref Span<int> p1 = ref stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "stackalloc int [3] { 1, 2, 3 }").WithLocation(7, 32),
                // (8,32): error CS1510: A ref or out value must be an assignable variable
                //         ref Span<int> p2 = ref stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(8, 32),
                // (9,32): error CS1510: A ref or out value must be an assignable variable
                //         ref Span<int> p3 = ref stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(9, 32)
                );
        }

        [Fact]
        public void InvalidPositionForStackAllocSpan()
        {
            var test = @"
using System;
public class Test
{
    void M()
    {
        N(stackalloc int [3] { 1, 2, 3 });
        N(stackalloc int [ ] { 1, 2, 3 });
        N(stackalloc     [ ] { 1, 2, 3 });
    }
    void N(Span<int> span)
    {
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (7,11): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         N(stackalloc int [3] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 11),
                // (8,11): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         N(stackalloc int [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(8, 11),
                // (9,11): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         N(stackalloc     [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(9, 11)
                );
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CannotDotIntoStackAllocExpression()
        {
            var test = @"
public class Test
{
    void M()
    {
        int length1 = (stackalloc int [3] { 1, 2, 3 }).Length;
        int length2 = (stackalloc int [ ] { 1, 2, 3 }).Length;
        int length3 = (stackalloc     [ ] { 1, 2, 3 }).Length;

        int length4 = stackalloc int [3] { 1, 2, 3 }.Length;
        int length5 = stackalloc int [ ] { 1, 2, 3 }.Length;
        int length6 = stackalloc     [ ] { 1, 2, 3 }.Length;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, TestOptions.ReleaseDll, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (6,24): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         int length1 = (stackalloc int [3] { 1, 2, 3 }).Length;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(6, 24),
                // (7,24): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         int length2 = (stackalloc int [ ] { 1, 2, 3 }).Length;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 24),
                // (8,24): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         int length3 = (stackalloc     [ ] { 1, 2, 3 }).Length;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(8, 24),
                // (10,23): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         int length4 = stackalloc int [3] { 1, 2, 3 }.Length;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(10, 23),
                // (11,23): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         int length5 = stackalloc int [ ] { 1, 2, 3 }.Length;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(11, 23),
                // (12,23): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         int length6 = stackalloc     [ ] { 1, 2, 3 }.Length;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(12, 23)
                );
            CreateCompilationWithMscorlibAndSpan(test, TestOptions.ReleaseDll).VerifyDiagnostics(
                );
        }

        [Fact]
        public void OverloadResolution_Fail()
        {
            var test = @"
using System;
unsafe public class Test
{
    static void Main()
    {
        Invoke(stackalloc int [3] { 1, 2, 3 });
        Invoke(stackalloc int [ ] { 1, 2, 3 });
        Invoke(stackalloc     [ ] { 1, 2, 3 });
    }

    static void Invoke(Span<short> shortSpan) => Console.WriteLine(""shortSpan"");
    static void Invoke(Span<bool> boolSpan) => Console.WriteLine(""boolSpan"");
    static void Invoke(int* intPointer) => Console.WriteLine(""intPointer"");
    static void Invoke(void* voidPointer) => Console.WriteLine(""voidPointer"");
}
";
            CreateCompilationWithMscorlibAndSpan(test, TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (7,16): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         Invoke(stackalloc int [3] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 16),
                // (8,16): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         Invoke(stackalloc int [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(8, 16),
                // (9,16): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         Invoke(stackalloc     [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(9, 16)
                );
            CreateCompilationWithMscorlibAndSpan(test, TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
                // (7,16): error CS1503: Argument 1: cannot convert from 'System.Span<int>' to 'System.Span<short>'
                //         Invoke(stackalloc int [3] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_BadArgType, "stackalloc int [3] { 1, 2, 3 }").WithArguments("1", "System.Span<int>", "System.Span<short>").WithLocation(7, 16),
                // (8,16): error CS1503: Argument 1: cannot convert from 'System.Span<int>' to 'System.Span<short>'
                //         Invoke(stackalloc int [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_BadArgType, "stackalloc int [ ] { 1, 2, 3 }").WithArguments("1", "System.Span<int>", "System.Span<short>").WithLocation(8, 16),
                // (9,16): error CS1503: Argument 1: cannot convert from 'System.Span<int>' to 'System.Span<short>'
                //         Invoke(stackalloc     [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_BadArgType, "stackalloc     [ ] { 1, 2, 3 }").WithArguments("1", "System.Span<int>", "System.Span<short>").WithLocation(9, 16)
            );
        }

        [Fact]
        public void StackAllocWithDynamic()
        {
            CreateCompilation(@"
class Program
{
    static void Main()
    {
        dynamic d = 1;
        var d1 = stackalloc dynamic [3] { d };
        var d2 = stackalloc dynamic [ ] { d };
        var d3 = stackalloc         [ ] { d };
    }
}").VerifyDiagnostics(
                // (7,29): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('dynamic')
                //         var d1 = stackalloc dynamic [3] { d };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "dynamic").WithArguments("dynamic").WithLocation(7, 29),
                // (7,18): error CS0847: An array initializer of length '3' is expected
                //         var d1 = stackalloc dynamic [3] { d };
                Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc dynamic [3] { d }").WithArguments("3").WithLocation(7, 18),
                // (8,29): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('dynamic')
                //         var d2 = stackalloc dynamic [ ] { d };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "dynamic").WithArguments("dynamic").WithLocation(8, 29),
                // (9,18): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('dynamic')
                //         var d3 = stackalloc         [ ] { d };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "stackalloc         [ ] { d }").WithArguments("dynamic").WithLocation(9, 18),
                // (9,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var d3 = stackalloc         [ ] { d };
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc         [ ] { d }").WithLocation(9, 18)
                );
        }

        [Fact]
        public void StackAllocWithDynamicSpan()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Program
{
    static void Main()
    {
        dynamic d = 1;
        Span<dynamic> d1 = stackalloc dynamic [3] { d };
        Span<dynamic> d2 = stackalloc dynamic [ ] { d };
        Span<dynamic> d3 = stackalloc         [ ] { d };
    }
}").VerifyDiagnostics(
                // (8,39): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('dynamic')
                //         Span<dynamic> d1 = stackalloc dynamic [3] { d };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "dynamic").WithArguments("dynamic").WithLocation(8, 39),
                // (8,28): error CS0847: An array initializer of length '3' is expected
                //         Span<dynamic> d1 = stackalloc dynamic [3] { d };
                Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc dynamic [3] { d }").WithArguments("3").WithLocation(8, 28),
                // (9,39): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('dynamic')
                //         Span<dynamic> d2 = stackalloc dynamic [ ] { d };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "dynamic").WithArguments("dynamic").WithLocation(9, 39),
                // (10,28): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('dynamic')
                //         Span<dynamic> d3 = stackalloc         [ ] { d };
                Diagnostic(ErrorCode.ERR_ManagedAddr, "stackalloc         [ ] { d }").WithArguments("dynamic").WithLocation(10, 28)
                );
        }

        [Fact]
        public void StackAllocAsArgument()
        {
            var source = @"
class Program
{
    static void N(object p) { }

    static void Main()
    {
        N(stackalloc int [3] { 1, 2, 3 });
        N(stackalloc int [ ] { 1, 2, 3 });
        N(stackalloc     [ ] { 1, 2, 3 });
    }
}";
            CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (8,11): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         N(stackalloc int [3] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(8, 11),
                // (9,11): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         N(stackalloc int [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(9, 11),
                // (10,11): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         N(stackalloc     [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(10, 11)
                );
            CreateCompilationWithMscorlibAndSpan(source).VerifyDiagnostics(
                // (8,11): error CS1503: Argument 1: cannot convert from 'System.Span<int>' to 'object'
                //         N(stackalloc int [3] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_BadArgType, "stackalloc int [3] { 1, 2, 3 }").WithArguments("1", "System.Span<int>", "object").WithLocation(8, 11),
                // (9,11): error CS1503: Argument 1: cannot convert from 'System.Span<int>' to 'object'
                //         N(stackalloc int [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_BadArgType, "stackalloc int [ ] { 1, 2, 3 }").WithArguments("1", "System.Span<int>", "object").WithLocation(9, 11),
                // (10,11): error CS1503: Argument 1: cannot convert from 'System.Span<int>' to 'object'
                //         N(stackalloc     [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_BadArgType, "stackalloc     [ ] { 1, 2, 3 }").WithArguments("1", "System.Span<int>", "object").WithLocation(10, 11)
                );
        }

        [Fact]
        public void StackAllocInParenthesis()
        {
            var source = @"
class Program
{
    static void Main()
    {
        var x1 = (stackalloc int [3] { 1, 2, 3 });
        var x2 = (stackalloc int [ ] { 1, 2, 3 });
        var x3 = (stackalloc     [ ] { 1, 2, 3 });
    }
}";
            CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (6,19): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x1 = (stackalloc int [3] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(6, 19),
                // (7,19): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x2 = (stackalloc int [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 19),
                // (8,19): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x3 = (stackalloc     [ ] { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(8, 19)
                );
            CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                );
        }

        [Fact]
        public void StackAllocInNullConditionalOperator()
        {
            var source = @"
class Program
{
    static void Main()
    {
        var x1 = stackalloc int [3] { 1, 2, 3 } ?? stackalloc int [3] { 1, 2, 3 };
        var x2 = stackalloc int [ ] { 1, 2, 3 } ?? stackalloc int [ ] { 1, 2, 3 };
        var x3 = stackalloc     [ ] { 1, 2, 3 } ?? stackalloc     [ ] { 1, 2, 3 };
    }
}";
            CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (6,18): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x1 = stackalloc int [3] { 1, 2, 3 } ?? stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(6, 18),
                // (6,52): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x1 = stackalloc int [3] { 1, 2, 3 } ?? stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(6, 52),
                // (7,18): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x2 = stackalloc int [ ] { 1, 2, 3 } ?? stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 18),
                // (7,52): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x2 = stackalloc int [ ] { 1, 2, 3 } ?? stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 52),
                // (8,18): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x3 = stackalloc     [ ] { 1, 2, 3 } ?? stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(8, 18),
                // (8,52): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x3 = stackalloc     [ ] { 1, 2, 3 } ?? stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(8, 52)
                );
            CreateCompilationWithMscorlibAndSpan(source).VerifyDiagnostics(
                // (6,18): error CS0019: Operator '??' cannot be applied to operands of type 'Span<int>' and 'Span<int>'
                //         var x1 = stackalloc int [3] { 1, 2, 3 } ?? stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "stackalloc int [3] { 1, 2, 3 } ?? stackalloc int [3] { 1, 2, 3 }").WithArguments("??", "System.Span<int>", "System.Span<int>").WithLocation(6, 18),
                // (7,18): error CS0019: Operator '??' cannot be applied to operands of type 'Span<int>' and 'Span<int>'
                //         var x2 = stackalloc int [ ] { 1, 2, 3 } ?? stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "stackalloc int [ ] { 1, 2, 3 } ?? stackalloc int [ ] { 1, 2, 3 }").WithArguments("??", "System.Span<int>", "System.Span<int>").WithLocation(7, 18),
                // (8,18): error CS0019: Operator '??' cannot be applied to operands of type 'Span<int>' and 'Span<int>'
                //         var x3 = stackalloc     [ ] { 1, 2, 3 } ?? stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "stackalloc     [ ] { 1, 2, 3 } ?? stackalloc     [ ] { 1, 2, 3 }").WithArguments("??", "System.Span<int>", "System.Span<int>").WithLocation(8, 18)
                );
        }

        [Fact]
        public void StackAllocInCastAndConditionalOperator()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public void Method()
    {
        Test value1 = true ? new Test() : (Test)stackalloc int [3] { 1, 2, 3 };
        Test value2 = true ? new Test() : (Test)stackalloc int [ ] { 1, 2, 3 };
        Test value3 = true ? new Test() : (Test)stackalloc     [ ] { 1, 2, 3 };
    }
    
    public static explicit operator Test(Span<int> value) 
    {
        return new Test();
    }
}", TestOptions.ReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void ERR_StackallocInCatchFinally_Catch()
        {
            var text = @"
unsafe class C
{
    int x = M(() =>
    {
        try
        {
            // fine
            int* p1 = stackalloc int [3] { 1, 2, 3 };
            int* p2 = stackalloc int [ ] { 1, 2, 3 };
            int* p3 = stackalloc     [ ] { 1, 2, 3 };
            System.Action a = () =>
            {
                try
                {
                    // fine
                    int* q1 = stackalloc int [3] { 1, 2, 3 };
                    int* q2 = stackalloc int [ ] { 1, 2, 3 };
                    int* q3 = stackalloc     [ ] { 1, 2, 3 };
                }
                catch
                {
                    int* err11 = stackalloc int [3] { 1, 2, 3 };
                    int* err12 = stackalloc int [ ] { 1, 2, 3 };
                    int* err13 = stackalloc     [ ] { 1, 2, 3 };
                }
            };
        }
        catch
        {
            int* err21 = stackalloc int [3] { 1, 2, 3 };
            int* err22 = stackalloc int [ ] { 1, 2, 3 };
            int* err23 = stackalloc     [ ] { 1, 2, 3 };
            System.Action a = () =>
            {
                try
                {
                    // fine
                    int* p1 = stackalloc int [3] { 1, 2, 3 };
                    int* p2 = stackalloc int [ ] { 1, 2, 3 };
                    int* p3 = stackalloc     [ ] { 1, 2, 3 };
                }
                catch
                {
                    int* err31 = stackalloc int [3] { 1, 2, 3 };
                    int* err32 = stackalloc int [ ] { 1, 2, 3 };
                    int* err33 = stackalloc     [ ] { 1, 2, 3 };
                }
            };
        }
    });

    static int M(System.Action action)
    {
        try
        {
            // fine
            int* p1 = stackalloc int [3] { 1, 2, 3 };
            int* p2 = stackalloc int [ ] { 1, 2, 3 };
            int* p3 = stackalloc     [ ] { 1, 2, 3 };
            System.Action a = () =>
            {
                try
                {
                    // fine
                    int* q1 = stackalloc int [3] { 1, 2, 3 };
                    int* q2 = stackalloc int [ ] { 1, 2, 3 };
                    int* q3 = stackalloc     [ ] { 1, 2, 3 };
                }
                catch
                {
                    int* err41 = stackalloc int [3] { 1, 2, 3 };
                    int* err42 = stackalloc int [ ] { 1, 2, 3 };
                    int* err43 = stackalloc     [ ] { 1, 2, 3 };
                }
            };
        }
        catch
        {
            int* err51 = stackalloc int [3] { 1, 2, 3 };
            int* err52 = stackalloc int [ ] { 1, 2, 3 };
            int* err53 = stackalloc     [ ] { 1, 2, 3 };
            System.Action a = () =>
            {
                try
                {
                    // fine
                    int* p1 = stackalloc int [3] { 1, 2, 3 };
                    int* p2 = stackalloc int [ ] { 1, 2, 3 };
                    int* p3 = stackalloc     [ ] { 1, 2, 3 };
                }
                catch
                {
                    int* err61 = stackalloc int [3] { 1, 2, 3 };
                    int* err62 = stackalloc int [ ] { 1, 2, 3 };
                    int* err63 = stackalloc     [ ] { 1, 2, 3 };
                }
            };
        }
        return 0;
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (23,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err11 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(23, 34),
                // (24,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err12 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(24, 34),
                // (25,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err13 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(25, 34),
                // (31,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err21 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(31, 26),
                // (32,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err22 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(32, 26),
                // (33,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err23 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(33, 26),
                // (45,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err31 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(45, 34),
                // (46,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err32 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(46, 34),
                // (47,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err33 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(47, 34),
                // (72,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err41 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(72, 34),
                // (73,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err42 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(73, 34),
                // (74,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err43 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(74, 34),
                // (80,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err51 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(80, 26),
                // (81,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err52 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(81, 26),
                // (82,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err53 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(82, 26),
                // (94,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err61 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(94, 34),
                // (95,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err62 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(95, 34),
                // (96,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err63 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(96, 34)
                );
        }

        [Fact]
        public void ERR_StackallocInCatchFinally_Finally()
        {
            var text = @"
unsafe class C
{
    int x = M(() =>
    {
        try
        {
            // fine
            int* p1 = stackalloc int [3] { 1, 2, 3 };
            int* p2 = stackalloc int [ ] { 1, 2, 3 };
            int* p3 = stackalloc     [ ] { 1, 2, 3 };
            System.Action a = () =>
            {
                try
                {
                    // fine
                    int* q1 = stackalloc int [3] { 1, 2, 3 };
                    int* q2 = stackalloc int [ ] { 1, 2, 3 };
                    int* q3 = stackalloc     [ ] { 1, 2, 3 };
                }
                finally
                {
                    int* err11 = stackalloc int [3] { 1, 2, 3 };
                    int* err12 = stackalloc int [ ] { 1, 2, 3 };
                    int* err13 = stackalloc     [ ] { 1, 2, 3 };
                }
            };
        }
        finally
        {
            int* err21 = stackalloc int [3] { 1, 2, 3 };
            int* err22 = stackalloc int [ ] { 1, 2, 3 };
            int* err23 = stackalloc     [ ] { 1, 2, 3 };
            System.Action a = () =>
            {
                try
                {
                    // fine
                    int* p1 = stackalloc int [3] { 1, 2, 3 };
                    int* p2 = stackalloc int [ ] { 1, 2, 3 };
                    int* p3 = stackalloc     [ ] { 1, 2, 3 };
                }
                finally
                {
                    int* err31 = stackalloc int [3] { 1, 2, 3 };
                    int* err32 = stackalloc int [ ] { 1, 2, 3 };
                    int* err33 = stackalloc     [ ] { 1, 2, 3 };
                }
            };
        }
    });

    static int M(System.Action action)
    {
        try
        {
            // fine
            int* p1 = stackalloc int [3] { 1, 2, 3 };
            int* p2 = stackalloc int [ ] { 1, 2, 3 };
            int* p3 = stackalloc     [ ] { 1, 2, 3 };
            System.Action a = () =>
            {
                try
                {
                    // fine
                    int* q1 = stackalloc int [3] { 1, 2, 3 };
                    int* q2 = stackalloc int [ ] { 1, 2, 3 };
                    int* q3 = stackalloc     [ ] { 1, 2, 3 };
                }
                finally
                {
                    int* err41 = stackalloc int [3] { 1, 2, 3 };
                    int* err42 = stackalloc int [ ] { 1, 2, 3 };
                    int* err43 = stackalloc     [ ] { 1, 2, 3 };
                }
            };
        }
        finally
        {
            int* err51 = stackalloc int [3] { 1, 2, 3 };
            int* err52 = stackalloc int [ ] { 1, 2, 3 };
            int* err53 = stackalloc     [ ] { 1, 2, 3 };
            System.Action a = () =>
            {
                try
                {
                    // fine
                    int* p1 = stackalloc int [3] { 1, 2, 3 };
                    int* p2 = stackalloc int [ ] { 1, 2, 3 };
                    int* p3 = stackalloc     [ ] { 1, 2, 3 };
                }
                finally
                {
                    int* err61 = stackalloc int [3] { 1, 2, 3 };
                    int* err62 = stackalloc int [ ] { 1, 2, 3 };
                    int* err63 = stackalloc     [ ] { 1, 2, 3 };
                }
            };
        }
        return 0;
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (23,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err11 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(23, 34),
                // (24,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err12 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(24, 34),
                // (25,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err13 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(25, 34),
                // (31,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err21 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(31, 26),
                // (32,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err22 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(32, 26),
                // (33,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err23 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(33, 26),
                // (45,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err31 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(45, 34),
                // (46,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err32 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(46, 34),
                // (47,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err33 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(47, 34),
                // (72,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err41 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(72, 34),
                // (73,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err42 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(73, 34),
                // (74,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err43 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(74, 34),
                // (80,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err51 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(80, 26),
                // (81,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err52 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(81, 26),
                // (82,26): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* err53 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(82, 26),
                // (94,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err61 = stackalloc int [3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [3] { 1, 2, 3 }").WithLocation(94, 34),
                // (95,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err62 = stackalloc int [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int [ ] { 1, 2, 3 }").WithLocation(95, 34),
                // (96,34): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* err63 = stackalloc     [ ] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc     [ ] { 1, 2, 3 }").WithLocation(96, 34)
                );
        }

        [Fact]
        public void StackAllocArrayCreationExpression_Symbols()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    public void Method1()
    {
        var obj1 = stackalloc double[2] { 1, 1.2 };
        Span<double> obj2 = stackalloc double[2] { 1, 1.2 };
        _ = stackalloc double[2] { 1, 1.2 };
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var expressions = tree.GetCompilationUnitRoot().DescendantNodes().OfType<StackAllocArrayCreationExpressionSyntax>().ToArray();
            Assert.Equal(3, expressions.Length);

            var @stackalloc = expressions[0];
            var stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Double*", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Double*", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            var element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Null(element0Info.Symbol);
            Assert.Equal("System.Int32", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, element0Info.ImplicitConversion);

            var element1Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[1]);
            Assert.Null(element1Info.Symbol);
            Assert.Equal("System.Double", element1Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element1Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element1Info.ImplicitConversion);

            var sizeInfo = model.GetSemanticInfoSummary(((ArrayTypeSyntax)@stackalloc.Type).RankSpecifiers[0].Sizes[0]);
            Assert.Null(sizeInfo.Symbol);
            Assert.Equal("System.Int32", sizeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", sizeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, sizeInfo.ImplicitConversion);

            Assert.Null(model.GetDeclaredSymbol(@stackalloc));

            @stackalloc = expressions[1];
            stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Span<System.Double>", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.Double>", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Null(element0Info.Symbol);
            Assert.Equal("System.Int32", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, element0Info.ImplicitConversion);

            element1Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[1]);
            Assert.Null(element1Info.Symbol);
            Assert.Equal("System.Double", element1Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element1Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element1Info.ImplicitConversion);

            sizeInfo = model.GetSemanticInfoSummary(((ArrayTypeSyntax)@stackalloc.Type).RankSpecifiers[0].Sizes[0]);
            Assert.Null(sizeInfo.Symbol);
            Assert.Equal("System.Int32", sizeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", sizeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, sizeInfo.ImplicitConversion);

            Assert.Null(model.GetDeclaredSymbol(@stackalloc));

            @stackalloc = expressions[2];
            stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Span<System.Double>", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.Double>", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Null(element0Info.Symbol);
            Assert.Equal("System.Int32", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, element0Info.ImplicitConversion);

            element1Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[1]);
            Assert.Null(element1Info.Symbol);
            Assert.Equal("System.Double", element1Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element1Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element1Info.ImplicitConversion);

            sizeInfo = model.GetSemanticInfoSummary(((ArrayTypeSyntax)@stackalloc.Type).RankSpecifiers[0].Sizes[0]);
            Assert.Null(sizeInfo.Symbol);
            Assert.Equal("System.Int32", sizeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", sizeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, sizeInfo.ImplicitConversion);

            Assert.Null(model.GetDeclaredSymbol(@stackalloc));
        }

        [Fact]
        public void ImplicitStackAllocArrayCreationExpression_Symbols()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    public void Method1()
    {
        var obj1 = stackalloc[] { 1, 1.2 };
        Span<double> obj2 = stackalloc[] { 1, 1.2 };
        _ = stackalloc[] { 1, 1.2 };
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var expressions = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitStackAllocArrayCreationExpressionSyntax>().ToArray();
            Assert.Equal(3, expressions.Length);

            var @stackalloc = expressions[0];
            var stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Double*", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Double*", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            var element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Null(element0Info.Symbol);
            Assert.Equal("System.Int32", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, element0Info.ImplicitConversion);

            var element1Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[1]);
            Assert.Null(element1Info.Symbol);
            Assert.Equal("System.Double", element1Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element1Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element1Info.ImplicitConversion);

            Assert.Null(model.GetDeclaredSymbol(@stackalloc));

            @stackalloc = expressions[1];
            stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Span<System.Double>", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.Double>", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Null(element0Info.Symbol);
            Assert.Equal("System.Int32", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, element0Info.ImplicitConversion);

            element1Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[1]);
            Assert.Null(element1Info.Symbol);
            Assert.Equal("System.Double", element1Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element1Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element1Info.ImplicitConversion);

            Assert.Null(model.GetDeclaredSymbol(@stackalloc));

            @stackalloc = expressions[2];
            stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Span<System.Double>", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.Double>", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Null(element0Info.Symbol);
            Assert.Equal("System.Int32", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, element0Info.ImplicitConversion);

            element1Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[1]);
            Assert.Null(element1Info.Symbol);
            Assert.Equal("System.Double", element1Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element1Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element1Info.ImplicitConversion);

            Assert.Null(model.GetDeclaredSymbol(@stackalloc));
        }

        [Fact]
        public void StackAllocArrayCreationExpression_Symbols_ErrorCase()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    public void Method1()
    {
        short* obj1 = stackalloc double[*obj1] { obj1[0], *obj1 };
        Span<short> obj2 = stackalloc double[obj2.Length] { obj2[0], obj2.Length };
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,41): error CS0150: A constant value is expected
                //         short* obj1 = stackalloc double[*obj1] { obj1[0], *obj1 };
                Diagnostic(ErrorCode.ERR_ConstantExpected, "*obj1").WithLocation(7, 41),
                // (8,46): error CS0150: A constant value is expected
                //         Span<short> obj2 = stackalloc double[obj2.Length] { obj2[0], obj2.Length };
                Diagnostic(ErrorCode.ERR_ConstantExpected, "obj2.Length").WithLocation(8, 46),
                // (7,42): error CS0165: Use of unassigned local variable 'obj1'
                //         short* obj1 = stackalloc double[*obj1] { obj1[0], *obj1 };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "obj1").WithArguments("obj1").WithLocation(7, 42),
                // (8,46): error CS0165: Use of unassigned local variable 'obj2'
                //         Span<short> obj2 = stackalloc double[obj2.Length] { obj2[0], obj2.Length };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "obj2").WithArguments("obj2").WithLocation(8, 46)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var expressions = tree.GetCompilationUnitRoot().DescendantNodes().OfType<StackAllocArrayCreationExpressionSyntax>().ToArray();
            Assert.Equal(2, expressions.Length);

            var @stackalloc = expressions[0];
            var stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Double*", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int16*", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.NoConversion, stackallocInfo.ImplicitConversion);

            var element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Null(element0Info.Symbol);
            Assert.Equal("System.Int16", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, element0Info.ImplicitConversion);

            var element1Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[1]);
            Assert.Null(element1Info.Symbol);
            Assert.Equal("System.Int16", element1Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element1Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, element1Info.ImplicitConversion);

            var sizeInfo = model.GetSemanticInfoSummary(((ArrayTypeSyntax)@stackalloc.Type).RankSpecifiers[0].Sizes[0]);
            Assert.Null(sizeInfo.Symbol);
            Assert.Equal("System.Int16", sizeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", sizeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, sizeInfo.ImplicitConversion);

            Assert.Null(model.GetDeclaredSymbol(@stackalloc));

            @stackalloc = expressions[1];
            stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Double*", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.Int16>", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.NoConversion, stackallocInfo.ImplicitConversion);

            element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Equal("ref System.Int16 System.Span<System.Int16>.this[System.Int32 i] { get; }", element0Info.Symbol.ToTestDisplayString());
            Assert.Equal("System.Int16", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, element0Info.ImplicitConversion);

            element1Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[1]);
            Assert.Equal("System.Int32 System.Span<System.Int16>.Length { get; }", element1Info.Symbol.ToTestDisplayString());
            Assert.Equal("System.Int32", element1Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element1Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, element1Info.ImplicitConversion);

            sizeInfo = model.GetSemanticInfoSummary(((ArrayTypeSyntax)@stackalloc.Type).RankSpecifiers[0].Sizes[0]);
            Assert.Equal("System.Int32 System.Span<System.Int16>.Length { get; }", sizeInfo.Symbol.ToTestDisplayString());
            Assert.Equal("System.Int32", sizeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", sizeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, sizeInfo.ImplicitConversion);

            Assert.Null(model.GetDeclaredSymbol(@stackalloc));
        }

        [Fact]
        public void ImplicitStackAllocArrayCreationExpression_Symbols_ErrorCase()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    public void Method1()
    {
        double* obj1 = stackalloc[] { obj1[0], *obj1 };
        Span<double> obj2 = stackalloc[] { obj2[0], obj2.Length };
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,39): error CS0165: Use of unassigned local variable 'obj1'
                //         double* obj1 = stackalloc[] { obj1[0], *obj1 };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "obj1").WithArguments("obj1").WithLocation(7, 39),
                // (8,44): error CS0165: Use of unassigned local variable 'obj2'
                //         Span<double> obj2 = stackalloc[] { obj2[0], obj2.Length };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "obj2").WithArguments("obj2").WithLocation(8, 44)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var expressions = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitStackAllocArrayCreationExpressionSyntax>().ToArray();
            Assert.Equal(2, expressions.Length);

            var @stackalloc = expressions[0];
            var stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Double*", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Double*", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            var element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Null(element0Info.Symbol);
            Assert.Equal("System.Double", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element0Info.ImplicitConversion);

            var element1Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[1]);
            Assert.Null(element1Info.Symbol);
            Assert.Equal("System.Double", element1Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element1Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element1Info.ImplicitConversion);

            Assert.Null(model.GetDeclaredSymbol(@stackalloc));

            @stackalloc = expressions[1];
            stackallocInfo = model.GetSemanticInfoSummary(@stackalloc);

            Assert.Null(stackallocInfo.Symbol);
            Assert.Equal("System.Span<System.Double>", stackallocInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.Double>", stackallocInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, stackallocInfo.ImplicitConversion);

            element0Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[0]);
            Assert.Equal("ref System.Double System.Span<System.Double>.this[System.Int32 i] { get; }", element0Info.Symbol.ToTestDisplayString());
            Assert.Equal("System.Double", element0Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element0Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, element0Info.ImplicitConversion);

            element1Info = model.GetSemanticInfoSummary(@stackalloc.Initializer.Expressions[1]);
            Assert.Equal("System.Int32 System.Span<System.Double>.Length { get; }", element1Info.Symbol.ToTestDisplayString());
            Assert.Equal("System.Int32", element1Info.Type.ToTestDisplayString());
            Assert.Equal("System.Double", element1Info.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNumeric, element1Info.ImplicitConversion);

            Assert.Null(model.GetDeclaredSymbol(@stackalloc));
        }
    }
}
