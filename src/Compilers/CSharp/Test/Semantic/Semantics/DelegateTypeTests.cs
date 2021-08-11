// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DelegateTypeTests : CSharpTestBase
    {
        private const string s_utils =
@"using System;
using System.Linq;
static class Utils
{
    internal static string GetDelegateMethodName(this Delegate d)
    {
        var method = d.Method;
        return Concat(GetTypeName(method.DeclaringType), method.Name);
    }
    internal static string GetDelegateTypeName(this Delegate d)
    {
        return d.GetType().GetTypeName();
    }
    internal static string GetTypeName(this Type type)
    {
        if (type.IsArray)
        {
            return GetTypeName(type.GetElementType()) + ""[]"";
        }
        string typeName = type.Name;
        int index = typeName.LastIndexOf('`');
        if (index >= 0)
        {
            typeName = typeName.Substring(0, index);
        }
        typeName = Concat(type.Namespace, typeName);
        if (!type.IsGenericType)
        {
            return typeName;
        }
        return $""{typeName}<{string.Join("", "", type.GetGenericArguments().Select(GetTypeName))}>"";
    }
    private static string Concat(string container, string name)
    {
        return string.IsNullOrEmpty(container) ? name : container + ""."" + name;
    }
}";

        [Fact]
        public void LanguageVersion()
        {
            var source =
@"class Program
{
    static void Main()
    {
        System.Delegate d;
        d = Main;
        d = () => { };
        d = delegate () { };
        System.Linq.Expressions.Expression e = () => 1;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,13): error CS0428: Cannot convert method group 'Main' to non-delegate type 'Delegate'. Did you intend to invoke the method?
                //         d = Main;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "System.Delegate").WithLocation(6, 13),
                // (7,13): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         d = () => { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "() => { }").WithArguments("lambda expression", "System.Delegate").WithLocation(7, 13),
                // (8,13): error CS1660: Cannot convert anonymous method to type 'Delegate' because it is not a delegate type
                //         d = delegate () { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate () { }").WithArguments("anonymous method", "System.Delegate").WithLocation(8, 13),
                // (9,48): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         System.Linq.Expressions.Expression e = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "() => 1").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(9, 48));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MethodGroupConversions()
        {
            var source =
@"class Program
{
    static void Main()
    {
        object o = Main;
        System.ICloneable c = Main;
        System.Delegate d = Main;
        System.MulticastDelegate m = Main;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,20): error CS0428: Cannot convert method group 'Main' to non-delegate type 'object'. Did you intend to invoke the method?
                //         object o = Main;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "object").WithLocation(5, 20),
                // (6,31): error CS0428: Cannot convert method group 'Main' to non-delegate type 'ICloneable'. Did you intend to invoke the method?
                //         System.ICloneable c = Main;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "System.ICloneable").WithLocation(6, 31),
                // (8,38): error CS0428: Cannot convert method group 'Main' to non-delegate type 'MulticastDelegate'. Did you intend to invoke the method?
                //         System.MulticastDelegate m = Main;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "System.MulticastDelegate").WithLocation(8, 38));
        }

        [Fact]
        public void LambdaConversions()
        {
            var source =
@"class Program
{
    static void Main()
    {
        object o = () => { };
        System.ICloneable c = () => { };
        System.Delegate d = () => { };
        System.MulticastDelegate m = () => { };
        d = x => x;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,20): error CS1660: Cannot convert lambda expression to type 'object' because it is not a delegate type
                //         object o = () => { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "() => { }").WithArguments("lambda expression", "object").WithLocation(5, 20),
                // (6,31): error CS1660: Cannot convert lambda expression to type 'ICloneable' because it is not a delegate type
                //         System.ICloneable c = () => { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "() => { }").WithArguments("lambda expression", "System.ICloneable").WithLocation(6, 31),
                // (8,38): error CS1660: Cannot convert lambda expression to type 'MulticastDelegate' because it is not a delegate type
                //         System.MulticastDelegate m = () => { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "() => { }").WithArguments("lambda expression", "System.MulticastDelegate").WithLocation(8, 38),
                // (9,13): error CS8917: The delegate type could not be inferred.
                //         d = x => x;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "x => x").WithLocation(9, 13));
        }

        private static IEnumerable<object?[]> GetMethodGroupData(Func<string, string, DiagnosticDescription[]> getExpectedDiagnostics)
        {
            yield return getData("static int F() => 0;", "Program.F", "F", "System.Func<System.Int32>");
            yield return getData("static int F() => 0;", "F", "F", "System.Func<System.Int32>");
            yield return getData("int F() => 0;", "(new Program()).F", "F", "System.Func<System.Int32>");
            yield return getData("static T F<T>() => default;", "Program.F<int>", "F", "System.Func<System.Int32>");
            yield return getData("static void F<T>() where T : class { }", "F<object>", "F", "System.Action");
            yield return getData("static void F<T>() where T : struct { }", "F<int>", "F", "System.Action");
            yield return getData("T F<T>() => default;", "(new Program()).F<int>", "F", "System.Func<System.Int32>");
            yield return getData("T F<T>() => default;", "(new Program()).F", "F", null);
            yield return getData("void F<T>(T t) { }", "(new Program()).F<string>", "F", "System.Action<System.String>");
            yield return getData("void F<T>(T t) { }", "(new Program()).F", "F", null);
            yield return getData("static ref int F() => throw null;", "F", "F", "<>F{00000001}<System.Int32>");
            yield return getData("static ref readonly int F() => throw null;", "F", "F", "<>F{00000003}<System.Int32>");
            yield return getData("static void F() { }", "F", "F", "System.Action");
            yield return getData("static void F(int x, int y) { }", "F", "F", "System.Action<System.Int32, System.Int32>");
            yield return getData("static void F(out int x, int y) { x = 0; }", "F", "F", "<>A{00000002}<System.Int32, System.Int32>");
            yield return getData("static void F(int x, ref int y) { }", "F", "F", "<>A{00000004}<System.Int32, System.Int32>");
            yield return getData("static void F(int x, in int y) { }", "F", "F", "<>A{0000000c}<System.Int32, System.Int32>");
            yield return getData("static void F(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) { }", "F", "F", "System.Action<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object>");
            yield return getData("static void F(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) { }", "F", "F", "<>A<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32>");
            yield return getData("static object F(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) => null;", "F", "F", "System.Func<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Object>");
            yield return getData("static object F(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) => null;", "F", "F", "<>F<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object>");

            object?[] getData(string methodDeclaration, string methodGroupExpression, string methodGroupOnly, string? expectedType) =>
                new object?[] { methodDeclaration, methodGroupExpression, expectedType is null ? getExpectedDiagnostics(methodGroupExpression, methodGroupOnly) : null, expectedType };
        }

        public static IEnumerable<object?[]> GetMethodGroupImplicitConversionData()
        {
            return GetMethodGroupData((methodGroupExpression, methodGroupOnly) =>
                {
                    int offset = methodGroupExpression.Length - methodGroupOnly.Length;
                    return new[]
                        {
                            // (6,29): error CS8917: The delegate type could not be inferred.
                            //         System.Delegate d = F;
                            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, methodGroupOnly).WithLocation(6, 29 + offset)
                        };
                });
        }

        [Theory]
        [MemberData(nameof(GetMethodGroupImplicitConversionData))]
        public void MethodGroup_ImplicitConversion(string methodDeclaration, string methodGroupExpression, DiagnosticDescription[]? expectedDiagnostics, string? expectedType)
        {
            var source =
$@"class Program
{{
    {methodDeclaration}
    static void Main()
    {{
        System.Delegate d = {methodGroupExpression};
        System.Console.Write(d.GetDelegateTypeName());
    }}
}}";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            if (expectedDiagnostics is null)
            {
                CompileAndVerify(comp, expectedOutput: expectedType);
            }
            else
            {
                comp.VerifyDiagnostics(expectedDiagnostics);
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer!.Value;
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);
        }

        public static IEnumerable<object?[]> GetMethodGroupExplicitConversionData()
        {
            return GetMethodGroupData((methodGroupExpression, methodGroupOnly) =>
                {
                    int offset = methodGroupExpression.Length - methodGroupOnly.Length;
                    return new[]
                        {
                            // (6,20): error CS0030: Cannot convert type 'method' to 'Delegate'
                            //         object o = (System.Delegate)F;
                            Diagnostic(ErrorCode.ERR_NoExplicitConv, $"(System.Delegate){methodGroupExpression}").WithArguments("method", "System.Delegate").WithLocation(6, 20)
                        };
                });
        }

        [Theory]
        [MemberData(nameof(GetMethodGroupExplicitConversionData))]
        public void MethodGroup_ExplicitConversion(string methodDeclaration, string methodGroupExpression, DiagnosticDescription[]? expectedDiagnostics, string? expectedType)
        {
            var source =
$@"class Program
{{
    {methodDeclaration}
    static void Main()
    {{
        object o = (System.Delegate){methodGroupExpression};
        System.Console.Write(o.GetType().GetTypeName());
    }}
}}";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            if (expectedDiagnostics is null)
            {
                CompileAndVerify(comp, expectedOutput: expectedType);
            }
            else
            {
                comp.VerifyDiagnostics(expectedDiagnostics);
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = ((CastExpressionSyntax)tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer!.Value).Expression;
            var typeInfo = model.GetTypeInfo(expr);
            // https://github.com/dotnet/roslyn/issues/52874: GetTypeInfo() for method group should return inferred delegate type.
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
        }

        public static IEnumerable<object?[]> GetLambdaData()
        {
            yield return getData("x => x", null);
            yield return getData("x => { return x; }", null);
            yield return getData("x => ref args[0]", null);
            yield return getData("(x, y) => { }", null);
            yield return getData("() => 1", "System.Func<System.Int32>");
            yield return getData("() => ref args[0]", "<>F{00000001}<System.String>");
            yield return getData("() => { }", "System.Action");
            yield return getData("(int x, int y) => { }", "System.Action<System.Int32, System.Int32>");
            yield return getData("(out int x, int y) => { x = 0; }", "<>A{00000002}<System.Int32, System.Int32>");
            yield return getData("(int x, ref int y) => { x = 0; }", "<>A{00000004}<System.Int32, System.Int32>");
            yield return getData("(int x, in int y) => { x = 0; }", "<>A{0000000c}<System.Int32, System.Int32>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) => { }", "System.Action<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) => { }", "<>A<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) => _1", "System.Func<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) => _1", "<>F<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Int32>");
            yield return getData("static () => 1", "System.Func<System.Int32>");
            yield return getData("async () => { await System.Threading.Tasks.Task.Delay(0); }", "System.Func<System.Threading.Tasks.Task>");
            yield return getData("static async () => { await System.Threading.Tasks.Task.Delay(0); return 0; }", "System.Func<System.Threading.Tasks.Task<System.Int32>>");
            yield return getData("() => Main", null);
            yield return getData("(int x) => x switch { _ => null }", null);
            yield return getData("_ => { }", null);
            yield return getData("_ => _", null);
            yield return getData("() => throw null", null);
            yield return getData("x => throw null", null);
            yield return getData("(int x) => throw null", null);
            yield return getData("() => { throw null; }", "System.Action");
            yield return getData("(int x) => { throw null; }", "System.Action<System.Int32>");
            yield return getData("(string s) => { if (s.Length > 0) return s; return null; }", "System.Func<System.String, System.String>");
            yield return getData("(string s) => { if (s.Length > 0) return default; return s; }", "System.Func<System.String, System.String>");
            yield return getData("(int i) => { if (i > 0) return i; return default; }", "System.Func<System.Int32, System.Int32>");
            yield return getData("(int x, short y) => { if (x > 0) return x; return y; }", "System.Func<System.Int32, System.Int16, System.Int32>");
            yield return getData("(int x, short y) => { if (x > 0) return y; return x; }", "System.Func<System.Int32, System.Int16, System.Int32>");
            yield return getData("object () => default", "System.Func<System.Object>");
            yield return getData("void () => { }", "System.Action");

            static object?[] getData(string expr, string? expectedType) =>
                new object?[] { expr, expectedType };
        }

        public static IEnumerable<object?[]> GetAnonymousMethodData()
        {
            yield return getData("delegate { }", null);
            yield return getData("delegate () { return 1; }", "System.Func<System.Int32>");
            yield return getData("delegate () { return ref args[0]; }", "<>F{00000001}<System.String>");
            yield return getData("delegate () { }", "System.Action");
            yield return getData("delegate (int x, int y) { }", "System.Action<System.Int32, System.Int32>");
            yield return getData("delegate (out int x, int y) { x = 0; }", "<>A{00000002}<System.Int32, System.Int32>");
            yield return getData("delegate (int x, ref int y) { x = 0; }", "<>A{00000004}<System.Int32, System.Int32>");
            yield return getData("delegate (int x, in int y) { x = 0; }", "<>A{0000000c}<System.Int32, System.Int32>");
            yield return getData("delegate (int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) { }", "System.Action<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object>");
            yield return getData("delegate (int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) { }", "<>A<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32>");
            yield return getData("delegate (int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) { return _1; }", "System.Func<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32>");
            yield return getData("delegate (int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) { return _1; }", "<>F<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Int32>");

            static object?[] getData(string expr, string? expectedType) =>
                new object?[] { expr, expectedType };
        }

        [Theory]
        [MemberData(nameof(GetLambdaData))]
        [MemberData(nameof(GetAnonymousMethodData))]
        public void AnonymousFunction_ImplicitConversion(string anonymousFunction, string? expectedType)
        {
            var source =
$@"class Program
{{
    static void Main(string[] args)
    {{
        System.Delegate d = {anonymousFunction};
        System.Console.Write(d.GetDelegateTypeName());
    }}
}}";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            if (expectedType is null)
            {
                comp.VerifyDiagnostics(
                    // (5,29): error CS8917: The delegate type could not be inferred.
                    //         System.Delegate d = x => x;
                    Diagnostic(ErrorCode.ERR_CannotInferDelegateType, anonymousFunction).WithLocation(5, 29));
            }
            else
            {
                CompileAndVerify(comp, expectedOutput: expectedType);
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<AnonymousFunctionExpressionSyntax>().Single();
            var typeInfo = model.GetTypeInfo(expr);
            if (expectedType == null)
            {
                Assert.Null(typeInfo.Type);
            }
            else
            {
                Assert.Equal(expectedType, typeInfo.Type.ToTestDisplayString());
            }
            Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);
        }

        [Theory]
        [MemberData(nameof(GetLambdaData))]
        [MemberData(nameof(GetAnonymousMethodData))]
        public void AnonymousFunction_ExplicitConversion(string anonymousFunction, string? expectedType)
        {
            var source =
$@"class Program
{{
    static void Main(string[] args)
    {{
        object o = (System.Delegate)({anonymousFunction});
        System.Console.Write(o.GetType().GetTypeName());
    }}
}}";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            if (expectedType is null)
            {
                comp.VerifyDiagnostics(
                    // (5,20): error CS8917: The delegate type could not be inferred.
                    //         object o = (System.Delegate)(x => x);
                    Diagnostic(ErrorCode.ERR_CannotInferDelegateType, $"(System.Delegate)({anonymousFunction})").WithLocation(5, 20));
            }
            else
            {
                CompileAndVerify(comp, expectedOutput: expectedType);
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = ((CastExpressionSyntax)tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer!.Value).Expression;
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Null(typeInfo.Type);
            Assert.Equal(expectedType, typeInfo.ConvertedType?.ToTestDisplayString());
        }

        public static IEnumerable<object?[]> GetExpressionData()
        {
            yield return getData("x => x", null);
            yield return getData("() => 1", "System.Func<System.Int32>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) => _1", "System.Func<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) => _1", "<>F<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Int32>");
            yield return getData("static () => 1", "System.Func<System.Int32>");

            static object?[] getData(string expr, string? expectedType) =>
                new object?[] { expr, expectedType };
        }

        [Theory]
        [MemberData(nameof(GetExpressionData))]
        public void Expression_ImplicitConversion(string anonymousFunction, string? expectedType)
        {
            var source =
$@"class Program
{{
    static void Main(string[] args)
    {{
        System.Linq.Expressions.Expression e = {anonymousFunction};
    }}
}}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            if (expectedType is null)
            {
                comp.VerifyDiagnostics(
                    // (5,48): error CS8917: The delegate type could not be inferred.
                    //         System.Linq.Expressions.Expression e = x => x;
                    Diagnostic(ErrorCode.ERR_CannotInferDelegateType, anonymousFunction).WithLocation(5, 48));
            }
            else
            {
                comp.VerifyDiagnostics();
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<AnonymousFunctionExpressionSyntax>().Single();
            var typeInfo = model.GetTypeInfo(expr);
            if (expectedType == null)
            {
                Assert.Null(typeInfo.Type);
            }
            else
            {
                Assert.Equal($"System.Linq.Expressions.Expression<{expectedType}>", typeInfo.Type.ToTestDisplayString());
            }
            Assert.Equal("System.Linq.Expressions.Expression", typeInfo.ConvertedType!.ToTestDisplayString());
        }

        [Theory]
        [MemberData(nameof(GetExpressionData))]
        public void Expression_ExplicitConversion(string anonymousFunction, string? expectedType)
        {
            var source =
$@"class Program
{{
    static void Main(string[] args)
    {{
        object o = (System.Linq.Expressions.Expression)({anonymousFunction});
    }}
}}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            if (expectedType is null)
            {
                comp.VerifyDiagnostics(
                    // (5,20): error CS8917: The delegate type could not be inferred.
                    //         object o = (System.Linq.Expressions.Expression)(x => x);
                    Diagnostic(ErrorCode.ERR_CannotInferDelegateType, $"(System.Linq.Expressions.Expression)({anonymousFunction})").WithLocation(5, 20));
            }
            else
            {
                comp.VerifyDiagnostics();
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = ((CastExpressionSyntax)tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer!.Value).Expression;
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Null(typeInfo.Type);
            if (expectedType is null)
            {
                Assert.Null(typeInfo.ConvertedType);
            }
            else
            {
                Assert.Equal($"System.Linq.Expressions.Expression<{expectedType}>", typeInfo.ConvertedType.ToTestDisplayString());
            }
        }

        /// <summary>
        /// Should bind and report diagnostics from anonymous method body
        /// regardless of whether the delegate type can be inferred.
        /// </summary>
        [Fact]
        public void AnonymousMethodBodyErrors()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Delegate d0 = x0 => { _ = x0.Length; object y0 = 0; _ = y0.Length; };
        Delegate d1 = (object x1) => { _ = x1.Length; };
        Delegate d2 = (ref object x2) => { _ = x2.Length; };
        Delegate d3 = delegate (object x3) { _ = x3.Length; };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,23): error CS8917: The delegate type could not be inferred.
                //         Delegate d0 = x0 => { _ = x0.Length; object y0 = 0; _ = y0.Length; };
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "x0 => { _ = x0.Length; object y0 = 0; _ = y0.Length; }").WithLocation(6, 23),
                // (6,68): error CS1061: 'object' does not contain a definition for 'Length' and no accessible extension method 'Length' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Delegate d0 = x0 => { _ = x0.Length; object y0 = 0; _ = y0.Length; };
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Length").WithArguments("object", "Length").WithLocation(6, 68),
                // (7,47): error CS1061: 'object' does not contain a definition for 'Length' and no accessible extension method 'Length' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Delegate d1 = (object x1) => { _ = x1.Length; };
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Length").WithArguments("object", "Length").WithLocation(7, 47),
                // (8,51): error CS1061: 'object' does not contain a definition for 'Length' and no accessible extension method 'Length' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Delegate d2 = (ref object x2) => { _ = x2.Length; };
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Length").WithArguments("object", "Length").WithLocation(8, 51),
                // (9,53): error CS1061: 'object' does not contain a definition for 'Length' and no accessible extension method 'Length' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Delegate d3 = delegate (object x3) { _ = x3.Length; };
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Length").WithArguments("object", "Length").WithLocation(9, 53));
        }

        public static IEnumerable<object?[]> GetBaseAndDerivedTypesData()
        {
            yield return getData("internal void F(object x) { }", "internal static new void F(object x) { }", "F", "F", null, "System.Action<System.Object>"); // instance and static
            // https://github.com/dotnet/roslyn/issues/52701: Assert failure: Unexpected value 'LessDerived' of type 'Microsoft.CodeAnalysis.CSharp.MemberResolutionKind'
#if !DEBUG
            yield return getData("internal void F(object x) { }", "internal static new void F(object x) { }", "this.F", "F",
                new[]
                {
                    // (5,29): error CS0176: Member 'B.F(object)' cannot be accessed with an instance reference; qualify it with a type name instead
                    //         System.Delegate d = this.F;
                    Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.F").WithArguments("B.F(object)").WithLocation(5, 29)
                }); // instance and static
#endif
            yield return getData("internal void F(object x) { }", "internal static new void F(object x) { }", "base.F", "F", null, "System.Action<System.Object>"); // instance and static
            yield return getData("internal static void F(object x) { }", "internal new void F(object x) { }", "F", "F", null, "System.Action<System.Object>"); // static and instance
            yield return getData("internal static void F(object x) { }", "internal new void F(object x) { }", "this.F", "F", null, "System.Action<System.Object>"); // static and instance
            yield return getData("internal static void F(object x) { }", "internal new void F(object x) { }", "base.F", "F"); // static and instance
            yield return getData("internal void F(object x) { }", "internal static void F() { }", "F", "F"); // instance and static, different number of parameters
            yield return getData("internal void F(object x) { }", "internal static void F() { }", "B.F", "F", null, "System.Action"); // instance and static, different number of parameters
            yield return getData("internal void F(object x) { }", "internal static void F() { }", "this.F", "F", null, "System.Action<System.Object>"); // instance and static, different number of parameters
            yield return getData("internal void F(object x) { }", "internal static void F() { }", "base.F", "F", null, "System.Action<System.Object>"); // instance and static, different number of parameters
            yield return getData("internal static void F() { }", "internal void F(object x) { }", "F", "F"); // static and instance, different number of parameters
            yield return getData("internal static void F() { }", "internal void F(object x) { }", "B.F", "F", null, "System.Action"); // static and instance, different number of parameters
            yield return getData("internal static void F() { }", "internal void F(object x) { }", "this.F", "F", null, "System.Action<System.Object>"); // static and instance, different number of parameters
            yield return getData("internal static void F() { }", "internal void F(object x) { }", "base.F", "F"); // static and instance, different number of parameters
            yield return getData("internal static void F(object x) { }", "private static void F() { }", "F", "F"); // internal and private
            yield return getData("private static void F(object x) { }", "internal static void F() { }", "F", "F", null, "System.Action"); // internal and private
            yield return getData("internal abstract void F(object x);", "internal override void F(object x) { }", "F", "F", null, "System.Action<System.Object>"); // override
            yield return getData("internal virtual void F(object x) { }", "internal override void F(object x) { }", "F", "F", null, "System.Action<System.Object>"); // override
            yield return getData("internal void F(object x) { }", "internal void F(object x) { }", "F", "F", null, "System.Action<System.Object>"); // hiding
            yield return getData("internal void F(object x) { }", "internal new void F(object x) { }", "F", "F", null, "System.Action<System.Object>"); // hiding
            yield return getData("internal void F(object x) { }", "internal new void F(object y) { }", "F", "F", null, "System.Action<System.Object>"); // different parameter name
            yield return getData("internal void F(object x) { }", "internal void F(string x) { }", "F", "F"); // different parameter type
            yield return getData("internal void F(object x) { }", "internal void F(object x, object y) { }", "F", "F"); // different number of parameters
            yield return getData("internal void F(object x) { }", "internal void F(ref object x) { }", "F", "F"); // different parameter ref kind
            yield return getData("internal void F(ref object x) { }", "internal void F(object x) { }", "F", "F"); // different parameter ref kind
            yield return getData("internal abstract object F();", "internal override object F() => throw null;", "F", "F", null, "System.Func<System.Object>"); // override
            yield return getData("internal virtual object F() => throw null;", "internal override object F() => throw null;", "F", "F", null, "System.Func<System.Object>"); // override
            yield return getData("internal object F() => throw null;", "internal object F() => throw null;", "F", "F", null, "System.Func<System.Object>"); // hiding
            yield return getData("internal object F() => throw null;", "internal new object F() => throw null;", "F", "F", null, "System.Func<System.Object>"); // hiding
            yield return getData("internal string F() => throw null;", "internal new object F() => throw null;", "F", "F"); // different return type
            yield return getData("internal object F() => throw null;", "internal new ref object F() => throw null;", "F", "F"); // different return ref kind
            yield return getData("internal ref object F() => throw null;", "internal new object F() => throw null;", "F", "F"); // different return ref kind
            yield return getData("internal void F(object x) { }", "internal new void F(dynamic x) { }", "F", "F", null, "System.Action<System.Object>"); // object/dynamic
            yield return getData("internal dynamic F() => throw null;", "internal new object F() => throw null;", "F", "F", null, "System.Func<System.Object>"); // object/dynamic
            yield return getData("internal void F((object, int) x) { }", "internal new void F((object a, int b) x) { }", "F", "F", null, "System.Action<System.ValueTuple<System.Object, System.Int32>>"); // tuple names
            yield return getData("internal (object a, int b) F() => throw null;", "internal new (object, int) F() => throw null;", "F", "F", null, "System.Func<System.ValueTuple<System.Object, System.Int32>>"); // tuple names
            yield return getData("internal void F(System.IntPtr x) { }", "internal new void F(nint x) { }", "F", "F", null, "System.Action<System.IntPtr>"); // System.IntPtr/nint
            yield return getData("internal nint F() => throw null;", "internal new System.IntPtr F() => throw null;", "F", "F", null, "System.Func<System.IntPtr>"); // System.IntPtr/nint
            yield return getData("internal void F(object x) { }",
@"#nullable enable
internal new void F(object? x) { }
#nullable disable", "F", "F", null, "System.Action<System.Object>"); // different nullability
            yield return getData(
    @"#nullable enable
internal object? F() => throw null!;
#nullable disable", "internal new object F() => throw null;", "F", "F", null, "System.Func<System.Object>"); // different nullability
            yield return getData("internal void F() { }", "internal void F<T>() { }", "F", "F"); // different arity
            yield return getData("internal void F() { }", "internal void F<T>() { }", "F<int>", "F<int>", null, "System.Action"); // different arity
            yield return getData("internal void F<T>() { }", "internal void F() { }", "F", "F"); // different arity
            yield return getData("internal void F<T>() { }", "internal void F() { }", "F<int>", "F<int>", null, "System.Action"); // different arity
            yield return getData("internal void F<T>() { }", "internal void F<T, U>() { }", "F<int>", "F<int>", null, "System.Action"); // different arity
            yield return getData("internal void F<T>() { }", "internal void F<T, U>() { }", "F<int, object>", "F<int, object>", null, "System.Action"); // different arity
            yield return getData("internal void F<T>(T t) { }", "internal new void F<U>(U u) { }", "F<int>", "F<int>", null, "System.Action<System.Int32>"); // different type parameter names
            yield return getData("internal void F<T>(T t) where T : class { }", "internal new void F<T>(T t) { }", "F<object>", "F<object>", null, "System.Action<System.Object>"); // different type parameter constraints
            yield return getData("internal void F<T>(T t) { }", "internal new void F<T>(T t) where T : class { }", "F<object>", "F<object>", null, "System.Action<System.Object>"); // different type parameter constraints
            yield return getData("internal void F<T>(T t) { }", "internal new void F<T>(T t) where T : class { }", "base.F<object>", "F<object>", null, "System.Action<System.Object>"); // different type parameter constraints
            yield return getData("internal void F<T>(T t) where T : class { }", "internal new void F<T>(T t) where T : struct { }", "F<int>", "F<int>", null, "System.Action<System.Int32>"); // different type parameter constraints
            // https://github.com/dotnet/roslyn/issues/52701: Assert failure: Unexpected value 'LessDerived' of type 'Microsoft.CodeAnalysis.CSharp.MemberResolutionKind'
#if !DEBUG
            yield return getData("internal void F<T>(T t) where T : class { }", "internal new void F<T>(T t) where T : struct { }", "F<object>", "F<object>",
                new[]
                {
                    // (5,29): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'B.F<T>(T)'
                    //         System.Delegate d = F<object>;
                    Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<object>").WithArguments("B.F<T>(T)", "T", "object").WithLocation(5, 29)
                }); // different type parameter constraints
#endif

            static object?[] getData(string methodA, string methodB, string methodGroupExpression, string methodGroupOnly, DiagnosticDescription[]? expectedDiagnostics = null, string? expectedType = null)
            {
                if (expectedDiagnostics is null && expectedType is null)
                {
                    int offset = methodGroupExpression.Length - methodGroupOnly.Length;
                    expectedDiagnostics = new[]
                    {
                        // (5,29): error CS8917: The delegate type could not be inferred.
                        //         System.Delegate d = F;
                        Diagnostic(ErrorCode.ERR_CannotInferDelegateType, methodGroupOnly).WithLocation(5, 29 + offset)
                    };
                }
                return new object?[] { methodA, methodB, methodGroupExpression, expectedDiagnostics, expectedType };
            }
        }

        [Theory]
        [MemberData(nameof(GetBaseAndDerivedTypesData))]
        public void MethodGroup_BaseAndDerivedTypes(string methodA, string methodB, string methodGroupExpression, DiagnosticDescription[]? expectedDiagnostics, string? expectedType)
        {
            var source =
$@"partial class B
{{
    void M()
    {{
        System.Delegate d = {methodGroupExpression};
        System.Console.Write(d.GetDelegateTypeName());
    }}
    static void Main()
    {{
        new B().M();
    }}
}}
abstract class A
{{
    {methodA}
}}
partial class B : A
{{
    {methodB}
}}";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            if (expectedDiagnostics is null)
            {
                CompileAndVerify(comp, expectedOutput: expectedType);
            }
            else
            {
                comp.VerifyDiagnostics(expectedDiagnostics);
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer!.Value;
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);
        }

        public static IEnumerable<object?[]> GetExtensionMethodsSameScopeData()
        {
            yield return getData("internal static void F(this object x) { }", "internal static void F(this string x) { }", "string.Empty.F", "F", null, "B.F", "System.Action"); // different parameter type
            yield return getData("internal static void F(this object x) { }", "internal static void F(this string x) { }", "this.F", "F", null, "A.F", "System.Action"); // different parameter type
            yield return getData("internal static void F(this object x) { }", "internal static void F(this object x, object y) { }", "this.F", "F"); // different number of parameters
            yield return getData("internal static void F(this object x, object y) { }", "internal static void F(this object x, ref object y) { }", "this.F", "F"); // different parameter ref kind
            yield return getData("internal static void F(this object x, ref object y) { }", "internal static void F(this object x, object y) { }", "this.F", "F"); // different parameter ref kind
            yield return getData("internal static object F(this object x) => throw null;", "internal static ref object F(this object x) => throw null;", "this.F", "F"); // different return ref kind
            yield return getData("internal static ref object F(this object x) => throw null;", "internal static object F(this object x) => throw null;", "this.F", "F"); // different return ref kind
            yield return getData("internal static void F(this object x, object y) { }", "internal static void F<T>(this object x, T y) { }", "this.F", "F"); // different arity
            yield return getData("internal static void F(this object x, object y) { }", "internal static void F<T>(this object x, T y) { }", "this.F<int>", "F<int>", null, "B.F", "System.Action<System.Int32>"); // different arity
            yield return getData("internal static void F<T>(this object x) { }", "internal static void F(this object x) { }", "this.F", "F"); // different arity
            yield return getData("internal static void F<T>(this object x) { }", "internal static void F(this object x) { }", "this.F<int>", "F<int>", null, "A.F", "System.Action"); // different arity
            yield return getData("internal static void F<T>(this T t) where T : class { }", "internal static void F<T>(this T t) { }", "this.F<object>", "F<object>",
                new[]
                {
                    // (5,29): error CS0121: The call is ambiguous between the following methods or properties: 'A.F<T>(T)' and 'B.F<T>(T)'
                    //         System.Delegate d = this.F<object>;
                    Diagnostic(ErrorCode.ERR_AmbigCall, "this.F<object>").WithArguments("A.F<T>(T)", "B.F<T>(T)").WithLocation(5, 29)
                }); // different type parameter constraints
            yield return getData("internal static void F<T>(this T t) { }", "internal static void F<T>(this T t) where T : class { }", "this.F<object>", "F<object>",
                new[]
                {
                    // (5,29): error CS0121: The call is ambiguous between the following methods or properties: 'A.F<T>(T)' and 'B.F<T>(T)'
                    //         System.Delegate d = this.F<object>;
                    Diagnostic(ErrorCode.ERR_AmbigCall, "this.F<object>").WithArguments("A.F<T>(T)", "B.F<T>(T)").WithLocation(5, 29)
                }); // different type parameter constraints
            yield return getData("internal static void F<T>(this T t) where T : class { }", "internal static void F<T>(this T t) where T : struct { }", "this.F<int>", "F<int>"); // different type parameter constraints

            static object?[] getData(string methodA, string methodB, string methodGroupExpression, string methodGroupOnly, DiagnosticDescription[]? expectedDiagnostics = null, string? expectedMethod = null, string? expectedType = null)
            {
                if (expectedDiagnostics is null && expectedType is null)
                {
                    int offset = methodGroupExpression.Length - methodGroupOnly.Length;
                    expectedDiagnostics = new[]
                    {
                        // (5,29): error CS8917: The delegate type could not be inferred.
                        //         System.Delegate d = F;
                        Diagnostic(ErrorCode.ERR_CannotInferDelegateType, methodGroupOnly).WithLocation(5, 29 + offset)
                    };
                }
                return new object?[] { methodA, methodB, methodGroupExpression, expectedDiagnostics, expectedMethod, expectedType };
            }
        }

        [Theory]
        [MemberData(nameof(GetExtensionMethodsSameScopeData))]
        public void MethodGroup_ExtensionMethodsSameScope(string methodA, string methodB, string methodGroupExpression, DiagnosticDescription[]? expectedDiagnostics, string? expectedMethod, string? expectedType)
        {
            var source =
$@"class Program
{{
    void M()
    {{
        System.Delegate d = {methodGroupExpression};
        System.Console.Write(""{{0}}: {{1}}"", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }}
    static void Main()
    {{
        new Program().M();
    }}
}}
static class A
{{
    {methodA}
}}
static class B
{{
    {methodB}
}}";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            if (expectedDiagnostics is null)
            {
                CompileAndVerify(comp, expectedOutput: $"{expectedMethod}: {expectedType}");
            }
            else
            {
                comp.VerifyDiagnostics(expectedDiagnostics);
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer!.Value;
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);

            var symbolInfo = model.GetSymbolInfo(expr);
            // https://github.com/dotnet/roslyn/issues/52870: GetSymbolInfo() should return resolved method from method group.
            Assert.Null(symbolInfo.Symbol);
        }

        public static IEnumerable<object?[]> GetExtensionMethodsDifferentScopeData()
        {
            yield return getData("internal static void F(this object x) { }", "internal static void F(this object x) { }", "this.F", "F", null, "A.F", "System.Action"); // hiding
            yield return getData("internal static void F(this object x) { }", "internal static void F(this object y) { }", "this.F", "F", null, "A.F", "System.Action"); // different parameter name
            yield return getData("internal static void F(this object x) { }", "internal static void F(this string x) { }", "string.Empty.F", "F", null, "A.F", "System.Action"); // different parameter type
            yield return getData("internal static void F(this object x) { }", "internal static void F(this string x) { }", "this.F", "F", null, "A.F", "System.Action"); // different parameter type
            yield return getData("internal static void F(this object x) { }", "internal static void F(this object x, object y) { }", "this.F", "F"); // different number of parameters
            yield return getData("internal static void F(this object x, object y) { }", "internal static void F(this object x, ref object y) { }", "this.F", "F"); // different parameter ref kind
            yield return getData("internal static void F(this object x, ref object y) { }", "internal static void F(this object x, object y) { }", "this.F", "F"); // different parameter ref kind
            yield return getData("internal static object F(this object x) => throw null;", "internal static ref object F(this object x) => throw null;", "this.F", "F"); // different return ref kind
            yield return getData("internal static ref object F(this object x) => throw null;", "internal static object F(this object x) => throw null;", "this.F", "F"); // different return ref kind
            yield return getData("internal static void F(this object x, System.IntPtr y) { }", "internal static void F(this object x, nint y) { }", "this.F", "F", null, "A.F", "System.Action<System.IntPtr>"); // System.IntPtr/nint
            yield return getData("internal static nint F(this object x) => throw null;", "internal static System.IntPtr F(this object x) => throw null;", "this.F", "F", null, "A.F", "System.Func<System.IntPtr>"); // System.IntPtr/nint
            yield return getData("internal static void F(this object x, object y) { }", "internal static void F<T>(this object x, T y) { }", "this.F", "F"); // different arity
            yield return getData("internal static void F(this object x, object y) { }", "internal static void F<T>(this object x, T y) { }", "this.F<int>", "F<int>", null, "N.B.F", "System.Action<System.Int32>"); // different arity
            yield return getData("internal static void F<T>(this object x) { }", "internal static void F(this object x) { }", "this.F", "F"); // different arity
            yield return getData("internal static void F<T>(this object x) { }", "internal static void F(this object x) { }", "this.F<int>", "F<int>", null, "A.F", "System.Action"); // different arity
            yield return getData("internal static void F<T>(this T t) where T : class { }", "internal static void F<T>(this T t) { }", "this.F<object>", "F<object>", null, "A.F", "System.Action"); // different type parameter constraints
            yield return getData("internal static void F<T>(this T t) { }", "internal static void F<T>(this T t) where T : class { }", "this.F<object>", "F<object>", null, "A.F", "System.Action"); // different type parameter constraints
            yield return getData("internal static void F<T>(this T t) where T : class { }", "internal static void F<T>(this T t) where T : struct { }", "this.F<int>", "F<int>"); // different type parameter constraints

            static object?[] getData(string methodA, string methodB, string methodGroupExpression, string methodGroupOnly, DiagnosticDescription[]? expectedDiagnostics = null, string? expectedMethod = null, string? expectedType = null)
            {
                if (expectedDiagnostics is null && expectedType is null)
                {
                    int offset = methodGroupExpression.Length - methodGroupOnly.Length;
                    expectedDiagnostics = new[]
                    {
                        // (6,29): error CS8917: The delegate type could not be inferred.
                        //         System.Delegate d = F;
                        Diagnostic(ErrorCode.ERR_CannotInferDelegateType, methodGroupOnly).WithLocation(6, 29 + offset)
                    };
                }
                return new object?[] { methodA, methodB, methodGroupExpression, expectedDiagnostics, expectedMethod, expectedType };
            }
        }

        [Theory]
        [MemberData(nameof(GetExtensionMethodsDifferentScopeData))]
        public void MethodGroup_ExtensionMethodsDifferentScope(string methodA, string methodB, string methodGroupExpression, DiagnosticDescription[]? expectedDiagnostics, string? expectedMethod, string? expectedType)
        {
            var source =
$@"using N;
class Program
{{
    void M()
    {{
        System.Delegate d = {methodGroupExpression};
        System.Console.Write(""{{0}}: {{1}}"", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }}
    static void Main()
    {{
        new Program().M();
    }}
}}
static class A
{{
    {methodA}
}}
namespace N
{{
    static class B
    {{
        {methodB}
    }}
}}";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            if (expectedDiagnostics is null)
            {
                CompileAndVerify(comp, expectedOutput: $"{expectedMethod}: {expectedType}");
            }
            else
            {
                comp.VerifyDiagnostics(expectedDiagnostics);
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer!.Value;
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);

            var symbolInfo = model.GetSymbolInfo(expr);
            // https://github.com/dotnet/roslyn/issues/52870: GetSymbolInfo() should return resolved method from method group.
            Assert.Null(symbolInfo.Symbol);
        }

        [Fact]
        public void InstanceMethods_01()
        {
            var source =
@"using System;
class Program
{
    object F1() => null;
    void F2(object x, int y) { }
    void F()
    {
        Delegate d1 = F1;
        Delegate d2 = this.F2;
        Console.WriteLine(""{0}, {1}"", d1.GetDelegateTypeName(), d2.GetDelegateTypeName());
    }
    static void Main()
    {
        new Program().F();
    }
}";
            CompileAndVerify(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, expectedOutput: "System.Func<System.Object>, System.Action<System.Object, System.Int32>");
        }

        [Fact]
        public void InstanceMethods_02()
        {
            var source =
@"using System;
class A
{
    protected virtual void F() { Console.WriteLine(nameof(A)); }
}
class B : A
{
    protected override void F() { Console.WriteLine(nameof(B)); }
    static void Invoke(Delegate d) { d.DynamicInvoke(); }
    void M()
    {
        Invoke(F);
        Invoke(this.F);
        Invoke(base.F);
    }
    static void Main()
    {
        new B().M();
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput:
@"B
B
A");
        }

        [Fact]
        public void InstanceMethods_03()
        {
            var source =
@"using System;
class A
{
    protected void F() { Console.WriteLine(nameof(A)); }
}
class B : A
{
    protected new void F() { Console.WriteLine(nameof(B)); }
    static void Invoke(Delegate d) { d.DynamicInvoke(); }
    void M()
    {
        Invoke(F);
        Invoke(this.F);
        Invoke(base.F);
    }
    static void Main()
    {
        new B().M();
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput:
@"B
B
A");
        }

        [Fact]
        public void InstanceMethods_04()
        {
            var source =
@"class Program
{
    T F<T>() => default;
    static void Main()
    {
        var p = new Program();
        System.Delegate d = p.F;
        object o = (System.Delegate)p.F;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,31): error CS8917: The delegate type could not be inferred.
                //         System.Delegate d = p.F;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F").WithLocation(7, 31),
                // (8,20): error CS0030: Cannot convert type 'method' to 'Delegate'
                //         object o = (System.Delegate)p.F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.Delegate)p.F").WithArguments("method", "System.Delegate").WithLocation(8, 20));
        }

        [Fact]
        public void MethodGroup_Inaccessible()
        {
            var source =
@"using System;
class A
{
    private static void F() { }
    internal static void F(object o) { }
}
class B
{
    static void Main()
    {
        Delegate d = A.F;
        Console.WriteLine(d.GetDelegateTypeName());
    }
}";
            CompileAndVerify(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, expectedOutput: "System.Action<System.Object>");
        }

        [Fact]
        public void MethodGroup_IncorrectArity()
        {
            var source =
@"class Program
{
    static void F0(object o) { }
    static void F0<T>(object o) { }
    static void F1(object o) { }
    static void F1<T, U>(object o) { }
    static void F2<T>(object o) { }
    static void F2<T, U>(object o) { }
    static void Main()
    {
        System.Delegate d;
        d = F0<int, object>;
        d = F1<int>;
        d = F2;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (12,13): error CS0308: The non-generic method 'Program.F0(object)' cannot be used with type arguments
                //         d = F0<int, object>;
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "F0<int, object>").WithArguments("Program.F0(object)", "method").WithLocation(12, 13),
                // (13,13): error CS0308: The non-generic method 'Program.F1(object)' cannot be used with type arguments
                //         d = F1<int>;
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "F1<int>").WithArguments("Program.F1(object)", "method").WithLocation(13, 13),
                // (14,13): error CS8917: The delegate type could not be inferred.
                //         d = F2;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F2").WithLocation(14, 13));
        }

        [Fact]
        public void ExtensionMethods_01()
        {
            var source =
@"static class E
{
    internal static void F1(this object x, int y) { }
    internal static void F2(this object x) { }
}
class Program
{
    void F2(int x) { }
    static void Main()
    {
        System.Delegate d;
        var p = new Program();
        d = p.F1;
        d = p.F2;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (14,15): error CS8917: The delegate type could not be inferred.
                //         d = p.F2;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F2").WithLocation(14, 15));
        }

        [Fact]
        public void ExtensionMethods_02()
        {
            var source =
@"using System;
static class E
{
    internal static void F(this System.Type x, int y) { }
    internal static void F(this string x) { }
}
class Program
{
    static void Main()
    {
        Delegate d1 = typeof(Program).F;
        Delegate d2 = """".F;
        Console.WriteLine(""{0}, {1}"", d1.GetDelegateTypeName(), d2.GetDelegateTypeName());
    }
}";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "System.Action<System.Int32>, System.Action");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(d => d.Initializer!.Value).ToArray();
            Assert.Equal(2, exprs.Length);

            foreach (var expr in exprs)
            {
                var typeInfo = model.GetTypeInfo(expr);
                Assert.Null(typeInfo.Type);
                Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);
            }
        }

        [Fact]
        public void ExtensionMethods_03()
        {
            var source =
@"using N;
namespace N
{
    static class E1
    {
        internal static void F1(this object x, int y) { }
        internal static void F2(this object x, int y) { }
        internal static void F2(this object x) { }
        internal static void F3(this object x) { }
    }
}
static class E2
{
    internal static void F1(this object x) { }
}
class Program
{
    static void Main()
    {
        System.Delegate d;
        var p = new Program();
        d = p.F1;
        d = p.F2;
        d = p.F3;
        d = E1.F1;
        d = E2.F1;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (22,15): error CS8917: The delegate type could not be inferred.
                //         d = p.F1;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F1").WithLocation(22, 15),
                // (23,15): error CS8917: The delegate type could not be inferred.
                //         d = p.F2;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F2").WithLocation(23, 15));
        }

        [Fact]
        public void ExtensionMethods_04()
        {
            var source =
@"static class E
{
    internal static void F1(this object x, int y) { }
}
static class Program
{
    static void F2(this object x) { }
    static void Main()
    {
        System.Delegate d;
        d = E.F1;
        d = F2;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionMethods_05()
        {
            var source =
@"using System;
static class E
{
    internal static void F(this A a) { }
}
class A
{
}
class B : A
{
    static void Invoke(Delegate d) { }
    void M()
    {
        Invoke(F);
        Invoke(this.F);
        Invoke(base.F);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (14,16): error CS0103: The name 'F' does not exist in the current context
                //         Invoke(F);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "F").WithArguments("F").WithLocation(14, 16),
                // (16,21): error CS0117: 'A' does not contain a definition for 'F'
                //         Invoke(base.F);
                Diagnostic(ErrorCode.ERR_NoSuchMember, "F").WithArguments("A", "F").WithLocation(16, 21));
        }

        [Fact]
        public void ExtensionMethods_06()
        {
            var source =
@"static class E
{
    internal static void F1<T>(this object x, T y) { }
    internal static void F2<T, U>(this T t) { }
}
class Program
{
    static void F<T>(T t) where T : class
    {
        System.Delegate d;
        d = t.F1;
        d = t.F2;
        d = t.F1<int>;
        d = t.F1<T>;
        d = t.F2<T, object>;
        d = t.F2<object, T>;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (11,15): error CS8917: The delegate type could not be inferred.
                //         d = t.F1;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F1").WithLocation(11, 15),
                // (12,15): error CS8917: The delegate type could not be inferred.
                //         d = t.F2;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F2").WithLocation(12, 15));
        }

        /// <summary>
        /// Method group with dynamic receiver does not use method group conversion.
        /// </summary>
        [Fact]
        public void DynamicReceiver()
        {
            var source =
@"using System;
class Program
{
    void F() { }
    static void Main()
    {
        dynamic d = new Program();
        object obj;
        try
        {
            obj = d.F;
        }
        catch (Exception e)
        {
            obj = e;
        }
        Console.WriteLine(obj.GetType().FullName);
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, references: new[] { CSharpRef }, expectedOutput: "Microsoft.CSharp.RuntimeBinder.RuntimeBinderException");
        }

        // System.Func<> and System.Action<> cannot be used as the delegate type
        // when the parameters or return type are not valid type arguments.
        [WorkItem(55217, "https://github.com/dotnet/roslyn/issues/55217")]
        [Fact]
        public void InvalidTypeArguments()
        {
            var source =
@"unsafe class Program
{
    static int* F() => throw null;
    static void Main()
    {
        System.Delegate d;
        d = F;
        d = (int x, int* y) => { };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics(
                // (7,13): error CS8917: The delegate type could not be inferred.
                //         d = F;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F").WithLocation(7, 13),
                // (8,13): error CS8917: The delegate type could not be inferred.
                //         d = (int x, int* y) => { };
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "(int x, int* y) => { }").WithLocation(8, 13));
        }

        [Fact]
        public void GenericDelegateType()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Delegate d = F<int>();
        Console.WriteLine(d.GetDelegateTypeName());
    }
    unsafe static Delegate F<T>()
    {
        return (T t, int* p) => { };
    }
}";
            // When we synthesize delegate types with parameter types (such as int*) that cannot
            // be used as type arguments, run the program to report the actual delegate type.
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics(
                // (11,16): error CS8917: The delegate type could not be inferred.
                //         return (T t, int* p) => { };
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "(T t, int* p) => { }").WithLocation(11, 16));
        }

        /// <summary>
        /// Custom modifiers should not affect delegate signature.
        /// </summary>
        [Fact]
        public void CustomModifiers_01()
        {
            var sourceA =
@".class public A
{
  .method public static void F1(object modopt(int32) x) { ldnull throw }
  .method public static object modopt(int32) F2() { ldnull throw }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using System;
class B
{
    static void Report(Delegate d)
    {
        Console.WriteLine(d.GetDelegateTypeName());
    }
    static void Main()
    {
        Report(A.F1);
        Report(A.F2);
    }
}";
            var comp = CreateCompilation(new[] { sourceB, s_utils }, new[] { refA }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"System.Action<System.Object>
System.Func<System.Object>");
        }

        /// <summary>
        /// Custom modifiers should not affect delegate signature.
        /// </summary>
        [Fact]
        public void CustomModifiers_02()
        {
            var sourceA =
@".class public A
{
  .method public static void F1(object modreq(int32) x) { ldnull throw }
  .method public static object modreq(int32) F2() { ldnull throw }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using System;
class B
{
    static void Report(Delegate d)
    {
        Console.WriteLine(d.GetDelegateTypeName());
    }
    static void Main()
    {
        Report(A.F1);
        Report(A.F2);
    }
}";
            var comp = CreateCompilation(new[] { sourceB, s_utils }, new[] { refA }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (10,16): error CS1503: Argument 1: cannot convert from 'method group' to 'Delegate'
                //         Report(A.F1);
                Diagnostic(ErrorCode.ERR_BadArgType, "A.F1").WithArguments("1", "method group", "System.Delegate").WithLocation(10, 16),
                // (11,16): error CS1503: Argument 1: cannot convert from 'method group' to 'Delegate'
                //         Report(A.F2);
                Diagnostic(ErrorCode.ERR_BadArgType, "A.F2").WithArguments("1", "method group", "System.Delegate").WithLocation(11, 16));
        }

        [Fact]
        public void UnmanagedCallersOnlyAttribute_01()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
class Program
{
    static void Main()
    {
        Delegate d = F;
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    static void F() { }
}";
            var comp = CreateCompilation(new[] { source, UnmanagedCallersOnlyAttributeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,22): error CS8902: 'Program.F()' is attributed with 'UnmanagedCallersOnly' and cannot be converted to a delegate type. Obtain a function pointer to this method.
                //         Delegate d = F;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeConvertedToDelegate, "F").WithArguments("Program.F()").WithLocation(8, 22));
        }

        [Fact]
        public void UnmanagedCallersOnlyAttribute_02()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
class Program
{
    static void Main()
    {
        Delegate d = new S().F;
    }
}
struct S
{
}
static class E1
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void F(this S s) { }
}
static class E2
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    public static void F(this S s) { }
}";
            var comp = CreateCompilation(new[] { source, UnmanagedCallersOnlyAttributeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,22): error CS0121: The call is ambiguous between the following methods or properties: 'E1.F(S)' and 'E2.F(S)'
                //         Delegate d = new S().F;
                Diagnostic(ErrorCode.ERR_AmbigCall, "new S().F").WithArguments("E1.F(S)", "E2.F(S)").WithLocation(8, 22));
        }

        [Fact]
        public void SystemActionAndFunc_Missing()
        {
            var sourceA =
@".assembly mscorlib
{
  .ver 0:0:0:0
}
.class public System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.ValueType extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public System.String extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Void extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Boolean extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Int32 extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.Delegate extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}";
            var refA = CompileIL(sourceA, prependDefaultHeader: false, autoInherit: false);

            var sourceB =
@"class Program
{
    static void Main()
    {
        System.Delegate d;
        d = Main;
        d = () => 1;
    }
}";

            var comp = CreateEmptyCompilation(sourceB, new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,13): error CS0518: Predefined type 'System.Action' is not defined or imported
                //         d = Main;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Main").WithArguments("System.Action").WithLocation(6, 13),
                // (6,13): error CS0518: Predefined type 'System.Action' is not defined or imported
                //         d = Main;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Main").WithArguments("System.Action").WithLocation(6, 13),
                // (6,13): error CS8917: The delegate type could not be inferred.
                //         d = Main;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "Main").WithLocation(6, 13),
                // (7,13): error CS0518: Predefined type 'System.Func`1' is not defined or imported
                //         d = () => 1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "() => 1").WithArguments("System.Func`1").WithLocation(7, 13));
        }

        private static MetadataReference GetCorlibWithInvalidActionAndFuncOfT()
        {
            var sourceA =
@".assembly mscorlib
{
  .ver 0:0:0:0
}
.class public System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.ValueType extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public System.String extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public System.Type extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Void extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Boolean extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Int32 extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.Delegate extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.Attribute extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Runtime.CompilerServices.RequiredAttributeAttribute extends System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor(class System.Type t) cil managed { ret }
}
.class public abstract System.MulticastDelegate extends System.Delegate
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Action`1<T> extends System.MulticastDelegate
{
  .custom instance void System.Runtime.CompilerServices.RequiredAttributeAttribute::.ctor(class System.Type) = ( 01 00 FF 00 00 ) 
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .method public hidebysig instance void Invoke(!T t) { ret }
}
.class public sealed System.Func`1<T> extends System.MulticastDelegate
{
  .custom instance void System.Runtime.CompilerServices.RequiredAttributeAttribute::.ctor(class System.Type) = ( 01 00 FF 00 00 ) 
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .method public hidebysig instance !T Invoke() { ldnull throw }
}";
            return CompileIL(sourceA, prependDefaultHeader: false, autoInherit: false);
        }

        [Fact]
        public void SystemActionAndFunc_UseSiteErrors()
        {
            var refA = GetCorlibWithInvalidActionAndFuncOfT();

            var sourceB =
@"class Program
{
    static void F(object o)
    {
    }
    static void Main()
    {
        System.Delegate d;
        d = F;
        d = () => 1;
    }
}";

            var comp = CreateEmptyCompilation(sourceB, new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,13): error CS0648: 'Action<T>' is a type not supported by the language
                //         d = F;
                Diagnostic(ErrorCode.ERR_BogusType, "F").WithArguments("System.Action<T>").WithLocation(9, 13),
                // (9,13): error CS0648: 'Action<T>' is a type not supported by the language
                //         d = F;
                Diagnostic(ErrorCode.ERR_BogusType, "F").WithArguments("System.Action<T>").WithLocation(9, 13),
                // (10,13): error CS0648: 'Func<T>' is a type not supported by the language
                //         d = () => 1;
                Diagnostic(ErrorCode.ERR_BogusType, "() => 1").WithArguments("System.Func<T>").WithLocation(10, 13));
        }

        [Fact]
        public void SystemLinqExpressionsExpression_Missing()
        {
            var sourceA =
@".assembly mscorlib
{
  .ver 0:0:0:0
}
.class public System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.ValueType extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public System.String extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public System.Type extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Void extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Boolean extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Int32 extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.Delegate extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.MulticastDelegate extends System.Delegate
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Func`1<T> extends System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .method public hidebysig instance !T Invoke() { ldnull throw }
}
.class public abstract System.Linq.Expressions.Expression extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}";
            var refA = CompileIL(sourceA, prependDefaultHeader: false, autoInherit: false);

            var sourceB =
@"class Program
{
    static void Main()
    {
        System.Linq.Expressions.Expression e = () => 1;
    }
}";

            var comp = CreateEmptyCompilation(sourceB, new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,48): error CS0518: Predefined type 'System.Linq.Expressions.Expression`1' is not defined or imported
                //         System.Linq.Expressions.Expression e = () => 1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "() => 1").WithArguments("System.Linq.Expressions.Expression`1").WithLocation(5, 48));
        }

        [Fact]
        public void SystemLinqExpressionsExpression_UseSiteErrors()
        {
            var sourceA =
@".assembly mscorlib
{
  .ver 0:0:0:0
}
.class public System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.ValueType extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public System.String extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public System.Type extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Void extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Boolean extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Int32 extends System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.Delegate extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.Attribute extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Runtime.CompilerServices.RequiredAttributeAttribute extends System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor(class System.Type t) cil managed { ret }
}
.class public abstract System.MulticastDelegate extends System.Delegate
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Func`1<T> extends System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .method public hidebysig instance !T Invoke() { ldnull throw }
}
.class public abstract System.Linq.Expressions.Expression extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public abstract System.Linq.Expressions.LambdaExpression extends System.Linq.Expressions.Expression
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Linq.Expressions.Expression`1<T> extends System.Linq.Expressions.LambdaExpression
{
  .custom instance void System.Runtime.CompilerServices.RequiredAttributeAttribute::.ctor(class System.Type) = ( 01 00 FF 00 00 ) 
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}";
            var refA = CompileIL(sourceA, prependDefaultHeader: false, autoInherit: false);

            var sourceB =
@"class Program
{
    static void Main()
    {
        System.Linq.Expressions.Expression e = () => 1;
    }
}";

            var comp = CreateEmptyCompilation(sourceB, new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,48): error CS0648: 'Expression<T>' is a type not supported by the language
                //         System.Linq.Expressions.Expression e = () => 1;
                Diagnostic(ErrorCode.ERR_BogusType, "() => 1").WithArguments("System.Linq.Expressions.Expression<T>").WithLocation(5, 48));
        }

        [WorkItem(4674, "https://github.com/dotnet/csharplang/issues/4674")]
        [Fact]
        public void OverloadResolution_01()
        {
            var source =
@"using System;
 
class Program
{
    static void M<T>(T t) { Console.WriteLine(""M<T>(T t)""); }
    static void M(Action<string> a) { Console.WriteLine(""M(Action<string> a)""); }
    
    static void F(object o) { }
    
    static void Main()
    {
        M(F); // C#9: M(Action<string>)
    }
}";

            var expectedOutput = @"M(Action<string> a)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: expectedOutput);
        }

        [WorkItem(4674, "https://github.com/dotnet/csharplang/issues/4674")]
        [Fact]
        public void OverloadResolution_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var c = new C();
        c.M(Main);      // C#9: E.M(object x, Action y)
        c.M(() => { }); // C#9: E.M(object x, Action y)
    }
}
class C
{
    public void M(object y) { Console.WriteLine(""C.M(object y)""); }
}
static class E
{
    public static void M(this object x, Action y) { Console.WriteLine(""E.M(object x, Action y)""); }
}";

            var expectedOutput =
@"E.M(object x, Action y)
E.M(object x, Action y)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: expectedOutput);
        }

        [WorkItem(4674, "https://github.com/dotnet/csharplang/issues/4674")]
        [Fact]
        public void OverloadResolution_03()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var c = new C();
        c.M(Main);      // C#9: E.M(object x, Action y)
        c.M(() => { }); // C#9: E.M(object x, Action y)
    }
}
class C
{
    public void M(Delegate d) { Console.WriteLine(""C.M""); }
}
static class E
{
    public static void M(this object o, Action a) { Console.WriteLine(""E.M""); }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput:
@"E.M
E.M
");

            // Breaking change from C#9 which binds to E.M.
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput:
@"C.M
C.M
");
        }

        [WorkItem(4674, "https://github.com/dotnet/csharplang/issues/4674")]
        [Fact]
        public void OverloadResolution_04()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        var c = new C();
        c.M(() => 1);
    }
}
class C
{
    public void M(Expression e) { Console.WriteLine(""C.M""); }
}
static class E
{
    public static void M(this object o, Func<int> a) { Console.WriteLine(""E.M""); }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: @"E.M");

            // Breaking change from C#9 which binds to E.M.
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: @"C.M");
        }

        [Fact]
        public void OverloadResolution_05()
        {
            var source =
@"using System;
class Program
{
    static void Report(string name) { Console.WriteLine(name); }
    static void FA(Delegate d) { Report(""FA(Delegate)""); }
    static void FA(Action d) { Report(""FA(Action)""); }
    static void FB(Delegate d) { Report(""FB(Delegate)""); }
    static void FB(Func<int> d) { Report(""FB(Func<int>)""); }
    static void F1() { }
    static int F2() => 0;
    static void Main()
    {
        FA(F1);
        FA(F2);
        FB(F1);
        FB(F2);
        FA(() => { });
        FA(() => 0);
        FB(() => { });
        FB(() => 0);
        FA(delegate () { });
        FA(delegate () { return 0; });
        FB(delegate () { });
        FB(delegate () { return 0; });
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (14,12): error CS1503: Argument 1: cannot convert from 'method group' to 'Delegate'
                //         FA(F2);
                Diagnostic(ErrorCode.ERR_BadArgType, "F2").WithArguments("1", "method group", "System.Delegate").WithLocation(14, 12),
                // (15,12): error CS1503: Argument 1: cannot convert from 'method group' to 'Delegate'
                //         FB(F1);
                Diagnostic(ErrorCode.ERR_BadArgType, "F1").WithArguments("1", "method group", "System.Delegate").WithLocation(15, 12),
                // (18,18): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         FA(() => 0);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "0").WithLocation(18, 18),
                // (19,15): error CS1643: Not all code paths return a value in lambda expression of type 'Func<int>'
                //         FB(() => { });
                Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "=>").WithArguments("lambda expression", "System.Func<int>").WithLocation(19, 15),
                // (22,26): error CS8030: Anonymous function converted to a void returning delegate cannot return a value
                //         FA(delegate () { return 0; });
                Diagnostic(ErrorCode.ERR_RetNoObjectRequiredLambda, "return").WithLocation(22, 26),
                // (23,12): error CS1643: Not all code paths return a value in anonymous method of type 'Func<int>'
                //         FB(delegate () { });
                Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "delegate").WithArguments("anonymous method", "System.Func<int>").WithLocation(23, 12));

            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput:
@"FA(Action)
FA(Delegate)
FB(Delegate)
FB(Func<int>)
FA(Action)
FA(Delegate)
FB(Delegate)
FB(Func<int>)
FA(Action)
FA(Delegate)
FB(Delegate)
FB(Func<int>)
");
        }

        [Fact]
        public void OverloadResolution_06()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Report(string name, Expression e) { Console.WriteLine(""{0}: {1}"", name, e); }
    static void F(Expression e) { Report(""F(Expression)"", e); }
    static void F(Expression<Func<int>> e) { Report(""F(Expression<Func<int>>)"", e); }
    static void Main()
    {
        F(() => 0);
        F(() => string.Empty);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (11,17): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         F(() => string.Empty);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "string.Empty").WithArguments("string", "int").WithLocation(11, 17),
                // (11,17): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         F(() => string.Empty);
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "string.Empty").WithArguments("lambda expression").WithLocation(11, 17));

            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput:
@"F(Expression<Func<int>>): () => 0
F(Expression): () => String.Empty
");
        }

        [Fact]
        public void OverloadResolution_07()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void F(Expression e) { }
    static void F(Expression<Func<int>> e) { }
    static void Main()
    {
        F(delegate () { return 0; });
        F(delegate () { return string.Empty; });
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,11): error CS1946: An anonymous method expression cannot be converted to an expression tree
                //         F(delegate () { return 0; });
                Diagnostic(ErrorCode.ERR_AnonymousMethodToExpressionTree, "delegate () { return 0; }").WithLocation(9, 11),
                // (10,11): error CS1946: An anonymous method expression cannot be converted to an expression tree
                //         F(delegate () { return string.Empty; });
                Diagnostic(ErrorCode.ERR_AnonymousMethodToExpressionTree, "delegate () { return string.Empty; }").WithLocation(10, 11));
        }

        [Fact]
        public void ImplicitlyTypedVariables_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var d1 = Main;
        Report(d1);
        var d2 = () => { };
        Report(d2);
        var d3 = delegate () { };
        Report(d3);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetDelegateTypeName());
}";

            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,18): error CS8773: Feature 'inferred delegate type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         var d1 = Main;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Main").WithArguments("inferred delegate type", "10.0").WithLocation(6, 18),
                // (8,18): error CS8773: Feature 'inferred delegate type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         var d2 = () => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "() => { }").WithArguments("inferred delegate type", "10.0").WithLocation(8, 18),
                // (10,18): error CS8773: Feature 'inferred delegate type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         var d3 = delegate () { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "delegate () { }").WithArguments("inferred delegate type", "10.0").WithLocation(10, 18));

            comp = CreateCompilation(new[] { source, s_utils }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput:
@"System.Action
System.Action
System.Action");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      100 (0x64)
  .maxstack  2
  .locals init (System.Action V_0, //d1
                System.Action V_1, //d2
                System.Action V_2) //d3
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  ldftn      ""void Program.Main()""
  IL_0008:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  call       ""void Program.Report(System.Delegate)""
  IL_0014:  nop
  IL_0015:  ldsfld     ""System.Action Program.<>c.<>9__0_0""
  IL_001a:  dup
  IL_001b:  brtrue.s   IL_0034
  IL_001d:  pop
  IL_001e:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0023:  ldftn      ""void Program.<>c.<Main>b__0_0()""
  IL_0029:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002e:  dup
  IL_002f:  stsfld     ""System.Action Program.<>c.<>9__0_0""
  IL_0034:  stloc.1
  IL_0035:  ldloc.1
  IL_0036:  call       ""void Program.Report(System.Delegate)""
  IL_003b:  nop
  IL_003c:  ldsfld     ""System.Action Program.<>c.<>9__0_1""
  IL_0041:  dup
  IL_0042:  brtrue.s   IL_005b
  IL_0044:  pop
  IL_0045:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_004a:  ldftn      ""void Program.<>c.<Main>b__0_1()""
  IL_0050:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0055:  dup
  IL_0056:  stsfld     ""System.Action Program.<>c.<>9__0_1""
  IL_005b:  stloc.2
  IL_005c:  ldloc.2
  IL_005d:  call       ""void Program.Report(System.Delegate)""
  IL_0062:  nop
  IL_0063:  ret
}");
        }

        [Fact]
        public void ImplicitlyTypedVariables_02()
        {
            var source =
@"var d1 = object.ReferenceEquals;
var d2 = () => { };
var d3 = delegate () { };
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9.WithKind(SourceCodeKind.Script));
            comp.VerifyDiagnostics(
                // (1,10): error CS8773: Feature 'inferred delegate type' is not available in C# 9.0. Please use language version 10.0 or greater.
                // var d1 = object.ReferenceEquals;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "object.ReferenceEquals").WithArguments("inferred delegate type", "10.0").WithLocation(1, 10),
                // (2,10): error CS8773: Feature 'inferred delegate type' is not available in C# 9.0. Please use language version 10.0 or greater.
                // var d2 = () => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "() => { }").WithArguments("inferred delegate type", "10.0").WithLocation(2, 10),
                // (3,10): error CS8773: Feature 'inferred delegate type' is not available in C# 9.0. Please use language version 10.0 or greater.
                // var d3 = delegate () { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "delegate () { }").WithArguments("inferred delegate type", "10.0").WithLocation(3, 10));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10.WithKind(SourceCodeKind.Script));
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitlyTypedVariables_03()
        {
            var source =
@"class Program
{
    static void Main()
    {
        ref var d1 = Main;
        ref var d2 = () => { };
        ref var d3 = delegate () { };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,17): error CS8172: Cannot initialize a by-reference variable with a value
                //         ref var d1 = Main;
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "d1 = Main").WithLocation(5, 17),
                // (5,22): error CS1657: Cannot use 'Main' as a ref or out value because it is a 'method group'
                //         ref var d1 = Main;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "Main").WithArguments("Main", "method group").WithLocation(5, 22),
                // (6,17): error CS8172: Cannot initialize a by-reference variable with a value
                //         ref var d2 = () => { };
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "d2 = () => { }").WithLocation(6, 17),
                // (6,22): error CS1510: A ref or out value must be an assignable variable
                //         ref var d2 = () => { };
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "() => { }").WithLocation(6, 22),
                // (7,17): error CS8172: Cannot initialize a by-reference variable with a value
                //         ref var d3 = delegate () { };
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "d3 = delegate () { }").WithLocation(7, 17),
                // (7,22): error CS1510: A ref or out value must be an assignable variable
                //         ref var d3 = delegate () { };
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "delegate () { }").WithLocation(7, 22));
        }

        [Fact]
        public void ImplicitlyTypedVariables_04()
        {
            var source =
@"class Program
{
    static void Main()
    {
        using var d1 = Main;
        using var d2 = () => { };
        using var d3 = delegate () { };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,9): error CS1674: 'Action': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //         using var d1 = Main;
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "using var d1 = Main;").WithArguments("System.Action").WithLocation(5, 9),
                // (6,9): error CS1674: 'Action': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //         using var d2 = () => { };
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "using var d2 = () => { };").WithArguments("System.Action").WithLocation(6, 9),
                // (7,9): error CS1674: 'Action': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //         using var d3 = delegate () { };
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "using var d3 = delegate () { };").WithArguments("System.Action").WithLocation(7, 9));
        }

        [Fact]
        public void ImplicitlyTypedVariables_05()
        {
            var source =
@"class Program
{
    static void Main()
    {
        foreach (var d1 in Main) { }
        foreach (var d2 in () => { }) { }
        foreach (var d3 in delegate () { }) { }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,28): error CS0446: Foreach cannot operate on a 'method group'. Did you intend to invoke the 'method group'?
                //         foreach (var d1 in Main) { }
                Diagnostic(ErrorCode.ERR_AnonMethGrpInForEach, "Main").WithArguments("method group").WithLocation(5, 28),
                // (6,28): error CS0446: Foreach cannot operate on a 'lambda expression'. Did you intend to invoke the 'lambda expression'?
                //         foreach (var d2 in () => { }) { }
                Diagnostic(ErrorCode.ERR_AnonMethGrpInForEach, "() => { }").WithArguments("lambda expression").WithLocation(6, 28),
                // (7,28): error CS0446: Foreach cannot operate on a 'anonymous method'. Did you intend to invoke the 'anonymous method'?
                //         foreach (var d3 in delegate () { }) { }
                Diagnostic(ErrorCode.ERR_AnonMethGrpInForEach, "delegate () { }").WithArguments("anonymous method").WithLocation(7, 28));
        }

        [Fact]
        public void ImplicitlyTypedVariables_06()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Func<int> f;
        var d1 = Main;
        f = d1;
        var d2 = object (int x) => x;
        f = d2;
        var d3 = delegate () { return string.Empty; };
        f = d3;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,13): error CS0029: Cannot implicitly convert type 'System.Action' to 'System.Func<int>'
                //         f = d1;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d1").WithArguments("System.Action", "System.Func<int>").WithLocation(8, 13),
                // (10,13): error CS0029: Cannot implicitly convert type 'System.Func<int, object>' to 'System.Func<int>'
                //         f = d2;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d2").WithArguments("System.Func<int, object>", "System.Func<int>").WithLocation(10, 13),
                // (12,13): error CS0029: Cannot implicitly convert type 'System.Func<string>' to 'System.Func<int>'
                //         f = d3;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d3").WithArguments("System.Func<string>", "System.Func<int>").WithLocation(12, 13));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var variables = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v => v.Initializer != null);
            var expectedInfo = new (string?, string?, string?)[]
            {
                ("System.Action d1", null, "System.Action"),
                ("System.Func<System.Int32, System.Object> d2", null, "System.Func<System.Int32, System.Object>"),
                ("System.Func<System.String> d3", null, "System.Func<System.String>"),
            };
            AssertEx.Equal(expectedInfo, variables.Select(v => getVariableInfo(model, v)));

            static (string?, string?, string?) getVariableInfo(SemanticModel model, VariableDeclaratorSyntax variable)
            {
                var symbol = model.GetDeclaredSymbol(variable);
                var typeInfo = model.GetTypeInfo(variable.Initializer!.Value);
                return (symbol?.ToTestDisplayString(), typeInfo.Type?.ToTestDisplayString(), typeInfo.ConvertedType?.ToTestDisplayString());
            }
        }

        [Fact]
        public void ImplicitlyTypedVariables_07()
        {
            var source =
@"class Program
{
    static void Main()
    {
        var t = (Main, () => { });
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,13): error CS0815: Cannot assign (method group, lambda expression) to an implicitly-typed variable
                //         var t = (Main, () => { });
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "t = (Main, () => { })").WithArguments("(method group, lambda expression)").WithLocation(5, 13));
        }

        [Fact]
        public void ImplicitlyTypedVariables_08()
        {
            var source =
@"class Program
{
    static void Main()
    {
        (var x1, var y1) = Main;
        var (x2, y2) = () => { };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,14): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x1'.
                //         (var x1, var y1) = Main;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x1").WithArguments("x1").WithLocation(5, 14),
                // (5,22): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y1'.
                //         (var x1, var y1) = Main;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y1").WithArguments("y1").WithLocation(5, 22),
                // (5,28): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         (var x1, var y1) = Main;
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "Main").WithLocation(5, 28),
                // (6,14): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         var (x2, y2) = () => { };
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 14),
                // (6,18): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y2'.
                //         var (x2, y2) = () => { };
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y2").WithArguments("y2").WithLocation(6, 18),
                // (6,24): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         var (x2, y2) = () => { };
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "() => { }").WithLocation(6, 24));
        }

        [Fact]
        public void ImplicitlyTypedVariables_09()
        {
            var source =
@"class Program
{
    static void Main()
    {
        var (x, y) = (Main, () => { });
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,14): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x'.
                //         var (x, y) = (Main, () => { });
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x").WithArguments("x").WithLocation(5, 14),
                // (5,17): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y'.
                //         var (x, y) = (Main, () => { });
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y").WithArguments("y").WithLocation(5, 17));
        }

        [Fact]
        public void ImplicitlyTypedVariables_10()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        (var x1, Action y1) = (Main, null);
        (Action x2, var y2) = (null, () => { });
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,14): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x1'.
                //         (var x1, Action y1) = (Main, null);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x1").WithArguments("x1").WithLocation(6, 14),
                // (7,25): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y2'.
                //         (Action x2, var y2) = (null, () => { });
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y2").WithArguments("y2").WithLocation(7, 25));
        }

        [Fact]
        public void ImplicitlyTypedVariables_11()
        {
            var source =
@"class Program
{
    static void F(object o) { }
    static void F(int i) { }
    static void Main()
    {
        var d1 = F;
        var d2 = x => x;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,18): error CS8917: The delegate type could not be inferred.
                //         var d1 = F;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F").WithLocation(7, 18),
                // (8,18): error CS8917: The delegate type could not be inferred.
                //         var d2 = x => x;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "x => x").WithLocation(8, 18));
        }

        [Fact]
        public void ImplicitlyTypedVariables_12()
        {
            var source =
@"class Program
{
    static void F(ref int i) { }
    static void Main()
    {
        var d1 = F;
        var d2 = (ref int x) => x;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitlyTypedVariables_13()
        {
            var source =
@"using System;
class Program
{
    static int F() => 0;
    static void Main()
    {
        var d1 = (F);
        Report(d1);
        var d2 = (object (int x) => x);
        Report(d2);
        var d3 = (delegate () { return string.Empty; });
        Report(d3);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetDelegateTypeName());
}";
            CompileAndVerify(new[] { source, s_utils }, options: TestOptions.DebugExe, expectedOutput:
@"System.Func<System.Int32>
System.Func<System.Int32, System.Object>
System.Func<System.String>");
        }

        [Fact]
        public void ImplicitlyTypedVariables_14()
        {
            var source =
@"delegate void D(string s);
class Program
{
    static void Main()
    {
        (D x, var y) = (() => string.Empty, () => string.Empty);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,19): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y'.
                //         (D x, var y) = (() => string.Empty, () => string.Empty);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y").WithArguments("y").WithLocation(6, 19));
        }

        [Fact]
        public void ImplicitlyTypedVariables_UseSiteErrors()
        {
            var source =
@"class Program
{
    static void F(object o) { }
    static void Main()
    {
        var d1 = F;
        var d2 = () => 1;
    }
}";
            var comp = CreateEmptyCompilation(source, new[] { GetCorlibWithInvalidActionAndFuncOfT() });
            comp.VerifyDiagnostics(
                // (6,18): error CS0648: 'Action<T>' is a type not supported by the language
                //         var d1 = F;
                Diagnostic(ErrorCode.ERR_BogusType, "F").WithArguments("System.Action<T>").WithLocation(6, 18),
                // (7,18): error CS0648: 'Func<T>' is a type not supported by the language
                //         var d2 = () => 1;
                Diagnostic(ErrorCode.ERR_BogusType, "() => 1").WithArguments("System.Func<T>").WithLocation(7, 18));
        }

        /// <summary>
        /// Ensure the conversion group containing the implicit
        /// conversion is handled correctly in NullableWalker.
        /// </summary>
        [Fact]
        public void NullableAnalysis_01()
        {
            var source =
@"#nullable enable
class Program
{
    static void Main()
    {
        System.Delegate d;
        d = Main;
        d = () => { };
        d = delegate () { };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        /// <summary>
        /// Ensure the conversion group containing the explicit
        /// conversion is handled correctly in NullableWalker.
        /// </summary>
        [Fact]
        public void NullableAnalysis_02()
        {
            var source =
@"#nullable enable
class Program
{
    static void Main()
    {
        object o;
        o = (System.Delegate)Main;
        o = (System.Delegate)(() => { });
        o = (System.Delegate)(delegate () { });
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SynthesizedDelegateTypes_01()
        {
            var source =
@"using System;
class Program
{
    static void M1<T>(T t)
    {
        var d = (ref T t) => t;
        Report(d);
        Console.WriteLine(d(ref t));
    }
    static void M2<U>(U u) where U : struct
    {
        var d = (ref U u) => u;
        Report(d);
        Console.WriteLine(d(ref u));
    }
    static void M3(double value)
    {
        var d = (ref double d) => d;
        Report(d);
        Console.WriteLine(d(ref value));
    }
    static void Main()
    {
        M1(41);
        M2(42f);
        M2(43d);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput:
@"<>F{00000001}`2[System.Int32,System.Int32]
41
<>F{00000001}`2[System.Single,System.Single]
42
<>F{00000001}`2[System.Double,System.Double]
43
");
            verifier.VerifyIL("Program.M1<T>",
@"{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldsfld     ""<>F{00000001}<T, T> Program.<>c__0<T>.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c__0<T> Program.<>c__0<T>.<>9""
  IL_000e:  ldftn      ""T Program.<>c__0<T>.<M1>b__0_0(ref T)""
  IL_0014:  newobj     ""<>F{00000001}<T, T>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""<>F{00000001}<T, T> Program.<>c__0<T>.<>9__0_0""
  IL_001f:  dup
  IL_0020:  call       ""void Program.Report(System.Delegate)""
  IL_0025:  ldarga.s   V_0
  IL_0027:  callvirt   ""T <>F{00000001}<T, T>.Invoke(ref T)""
  IL_002c:  box        ""T""
  IL_0031:  call       ""void System.Console.WriteLine(object)""
  IL_0036:  ret
}");
            verifier.VerifyIL("Program.M2<U>",
@"{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldsfld     ""<>F{00000001}<U, U> Program.<>c__1<U>.<>9__1_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c__1<U> Program.<>c__1<U>.<>9""
  IL_000e:  ldftn      ""U Program.<>c__1<U>.<M2>b__1_0(ref U)""
  IL_0014:  newobj     ""<>F{00000001}<U, U>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""<>F{00000001}<U, U> Program.<>c__1<U>.<>9__1_0""
  IL_001f:  dup
  IL_0020:  call       ""void Program.Report(System.Delegate)""
  IL_0025:  ldarga.s   V_0
  IL_0027:  callvirt   ""U <>F{00000001}<U, U>.Invoke(ref U)""
  IL_002c:  box        ""U""
  IL_0031:  call       ""void System.Console.WriteLine(object)""
  IL_0036:  ret
}");
            verifier.VerifyIL("Program.M3",
@"{
  // Code size       50 (0x32)
  .maxstack  2
  IL_0000:  ldsfld     ""<>F{00000001}<double, double> Program.<>c.<>9__2_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_000e:  ldftn      ""double Program.<>c.<M3>b__2_0(ref double)""
  IL_0014:  newobj     ""<>F{00000001}<double, double>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""<>F{00000001}<double, double> Program.<>c.<>9__2_0""
  IL_001f:  dup
  IL_0020:  call       ""void Program.Report(System.Delegate)""
  IL_0025:  ldarga.s   V_0
  IL_0027:  callvirt   ""double <>F{00000001}<double, double>.Invoke(ref double)""
  IL_002c:  call       ""void System.Console.WriteLine(double)""
  IL_0031:  ret
}");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetRoot().DescendantNodes();

            var variables = nodes.OfType<VariableDeclaratorSyntax>().Where(v => v.Identifier.Text == "d").ToArray();
            Assert.Equal(3, variables.Length);
            VerifyLocalDelegateType(model, variables[0], "<>F{00000001}<T, T> d", "T <>F{00000001}<T, T>.Invoke(ref T)");
            VerifyLocalDelegateType(model, variables[1], "<>F{00000001}<U, U> d", "U <>F{00000001}<U, U>.Invoke(ref U)");
            VerifyLocalDelegateType(model, variables[2], "<>F{00000001}<System.Double, System.Double> d", "System.Double <>F{00000001}<System.Double, System.Double>.Invoke(ref System.Double)");

            var identifiers = nodes.OfType<InvocationExpressionSyntax>().Where(i => i.Expression is IdentifierNameSyntax id && id.Identifier.Text == "Report").Select(i => i.ArgumentList.Arguments[0].Expression).ToArray();
            Assert.Equal(3, identifiers.Length);
            VerifyExpressionType(model, identifiers[0], "<>F{00000001}<T, T> d", "<>F{00000001}<T, T>");
            VerifyExpressionType(model, identifiers[1], "<>F{00000001}<U, U> d", "<>F{00000001}<U, U>");
            VerifyExpressionType(model, identifiers[2], "<>F{00000001}<System.Double, System.Double> d", "<>F{00000001}<System.Double, System.Double>");
        }

        [Fact]
        public void SynthesizedDelegateTypes_02()
        {
            var source =
@"using System;
class Program
{
    static void M1(A a, int value)
    {
        var d = a.F1;
        d() = value;
    }
    static void M2(B b, float value)
    {
        var d = b.F2;
        d() = value;
    }
    static void Main()
    {
        var a = new A();
        M1(a, 41);
        var b = new B();
        M2(b, 42f);
        Console.WriteLine((a._f, b._f));
    }
}
class A
{
    public int _f;
    public ref int F1() => ref _f;
}
class B
{
    public float _f;
}
static class E
{
    public static ref float F2(this B b) => ref b._f;
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput: @"(41, 42)");
            verifier.VerifyIL("Program.M1",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""ref int A.F1()""
  IL_0007:  newobj     ""<>F{00000001}<int>..ctor(object, System.IntPtr)""
  IL_000c:  callvirt   ""ref int <>F{00000001}<int>.Invoke()""
  IL_0011:  ldarg.1
  IL_0012:  stind.i4
  IL_0013:  ret
}");
            verifier.VerifyIL("Program.M2",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""ref float E.F2(B)""
  IL_0007:  newobj     ""<>F{00000001}<float>..ctor(object, System.IntPtr)""
  IL_000c:  callvirt   ""ref float <>F{00000001}<float>.Invoke()""
  IL_0011:  ldarg.1
  IL_0012:  stind.r4
  IL_0013:  ret
}");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var variables = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v => v.Identifier.Text == "d").ToArray();
            Assert.Equal(2, variables.Length);
            VerifyLocalDelegateType(model, variables[0], "<>F{00000001}<System.Int32> d", "ref System.Int32 <>F{00000001}<System.Int32>.Invoke()");
            VerifyLocalDelegateType(model, variables[1], "<>F{00000001}<System.Single> d", "ref System.Single <>F{00000001}<System.Single>.Invoke()");
        }

        [Fact]
        public void SynthesizedDelegateTypes_03()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report((ref int x, int y) => { });
        Report((int x, ref int y) => { });
        Report((ref float x, int y) => { });
        Report((float x, ref int y) => { });
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput:
@"<>A{00000001}`2[System.Int32,System.Int32]
<>A{00000004}`2[System.Int32,System.Int32]
<>A{00000001}`2[System.Single,System.Int32]
<>A{00000004}`2[System.Single,System.Int32]
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      145 (0x91)
  .maxstack  2
  IL_0000:  ldsfld     ""<>A{00000001}<int, int> Program.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_000e:  ldftn      ""void Program.<>c.<Main>b__0_0(ref int, int)""
  IL_0014:  newobj     ""<>A{00000001}<int, int>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""<>A{00000001}<int, int> Program.<>c.<>9__0_0""
  IL_001f:  call       ""void Program.Report(System.Delegate)""
  IL_0024:  ldsfld     ""<>A{00000004}<int, int> Program.<>c.<>9__0_1""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0032:  ldftn      ""void Program.<>c.<Main>b__0_1(int, ref int)""
  IL_0038:  newobj     ""<>A{00000004}<int, int>..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""<>A{00000004}<int, int> Program.<>c.<>9__0_1""
  IL_0043:  call       ""void Program.Report(System.Delegate)""
  IL_0048:  ldsfld     ""<>A{00000001}<float, int> Program.<>c.<>9__0_2""
  IL_004d:  dup
  IL_004e:  brtrue.s   IL_0067
  IL_0050:  pop
  IL_0051:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0056:  ldftn      ""void Program.<>c.<Main>b__0_2(ref float, int)""
  IL_005c:  newobj     ""<>A{00000001}<float, int>..ctor(object, System.IntPtr)""
  IL_0061:  dup
  IL_0062:  stsfld     ""<>A{00000001}<float, int> Program.<>c.<>9__0_2""
  IL_0067:  call       ""void Program.Report(System.Delegate)""
  IL_006c:  ldsfld     ""<>A{00000004}<float, int> Program.<>c.<>9__0_3""
  IL_0071:  dup
  IL_0072:  brtrue.s   IL_008b
  IL_0074:  pop
  IL_0075:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_007a:  ldftn      ""void Program.<>c.<Main>b__0_3(float, ref int)""
  IL_0080:  newobj     ""<>A{00000004}<float, int>..ctor(object, System.IntPtr)""
  IL_0085:  dup
  IL_0086:  stsfld     ""<>A{00000004}<float, int> Program.<>c.<>9__0_3""
  IL_008b:  call       ""void Program.Report(System.Delegate)""
  IL_0090:  ret
}");
        }

        [Fact]
        public void SynthesizedDelegateTypes_04()
        {
            var source =
@"using System;
class Program
{
    static int i = 0;
    static void Main()
    {
        Report(int () => i);
        Report((ref int () => ref i));
        Report((ref readonly int () => ref i));
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput:
@"System.Func`1[System.Int32]
<>F{00000001}`1[System.Int32]
<>F{00000003}`1[System.Int32]
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      109 (0x6d)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int> Program.<>c.<>9__1_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_000e:  ldftn      ""int Program.<>c.<Main>b__1_0()""
  IL_0014:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""System.Func<int> Program.<>c.<>9__1_0""
  IL_001f:  call       ""void Program.Report(System.Delegate)""
  IL_0024:  ldsfld     ""<>F{00000001}<int> Program.<>c.<>9__1_1""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0032:  ldftn      ""ref int Program.<>c.<Main>b__1_1()""
  IL_0038:  newobj     ""<>F{00000001}<int>..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""<>F{00000001}<int> Program.<>c.<>9__1_1""
  IL_0043:  call       ""void Program.Report(System.Delegate)""
  IL_0048:  ldsfld     ""<>F{00000003}<int> Program.<>c.<>9__1_2""
  IL_004d:  dup
  IL_004e:  brtrue.s   IL_0067
  IL_0050:  pop
  IL_0051:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0056:  ldftn      ""ref readonly int Program.<>c.<Main>b__1_2()""
  IL_005c:  newobj     ""<>F{00000003}<int>..ctor(object, System.IntPtr)""
  IL_0061:  dup
  IL_0062:  stsfld     ""<>F{00000003}<int> Program.<>c.<>9__1_2""
  IL_0067:  call       ""void Program.Report(System.Delegate)""
  IL_006c:  ret
}");
        }

        [Fact]
        public void SynthesizedDelegateTypes_05()
        {
            var source =
@"using System;
class Program
{
    static int i = 0;
    static int F1() => i;
    static ref int F2() => ref i;
    static ref readonly int F3() => ref i;
    static void Main()
    {
        Report(F1);
        Report(F2);
        Report(F3);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput:
@"System.Func`1[System.Int32]
<>F{00000001}`1[System.Int32]
<>F{00000003}`1[System.Int32]
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       52 (0x34)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""int Program.F1()""
  IL_0007:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_000c:  call       ""void Program.Report(System.Delegate)""
  IL_0011:  ldnull
  IL_0012:  ldftn      ""ref int Program.F2()""
  IL_0018:  newobj     ""<>F{00000001}<int>..ctor(object, System.IntPtr)""
  IL_001d:  call       ""void Program.Report(System.Delegate)""
  IL_0022:  ldnull
  IL_0023:  ldftn      ""ref readonly int Program.F3()""
  IL_0029:  newobj     ""<>F{00000003}<int>..ctor(object, System.IntPtr)""
  IL_002e:  call       ""void Program.Report(System.Delegate)""
  IL_0033:  ret
}");
        }

        [Fact]
        public void SynthesizedDelegateTypes_06()
        {
            var source =
@"using System;
class Program
{
    static int i = 0;
    static int F1() => i;
    static ref int F2() => ref i;
    static ref readonly int F3() => ref i;
    static void Main()
    {
        var d1 = F1;
        var d2 = F2;
        var d3 = F3;
        Report(d1);
        Report(d2);
        Report(d3);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"System.Func`1[System.Int32]
<>F{00000001}`1[System.Int32]
<>F{00000003}`1[System.Int32]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_07()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(int (ref int i) => i);
        Report((ref int (ref int i) => ref i));
        Report((ref readonly int (ref int i) => ref i));
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"<>F{00000001}`2[System.Int32,System.Int32]
<>F{00000005}`2[System.Int32,System.Int32]
<>F{0000000d}`2[System.Int32,System.Int32]
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      109 (0x6d)
  .maxstack  2
  IL_0000:  ldsfld     ""<>F{00000001}<int, int> Program.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_000e:  ldftn      ""int Program.<>c.<Main>b__0_0(ref int)""
  IL_0014:  newobj     ""<>F{00000001}<int, int>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""<>F{00000001}<int, int> Program.<>c.<>9__0_0""
  IL_001f:  call       ""void Program.Report(System.Delegate)""
  IL_0024:  ldsfld     ""<>F{00000005}<int, int> Program.<>c.<>9__0_1""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0032:  ldftn      ""ref int Program.<>c.<Main>b__0_1(ref int)""
  IL_0038:  newobj     ""<>F{00000005}<int, int>..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""<>F{00000005}<int, int> Program.<>c.<>9__0_1""
  IL_0043:  call       ""void Program.Report(System.Delegate)""
  IL_0048:  ldsfld     ""<>F{0000000d}<int, int> Program.<>c.<>9__0_2""
  IL_004d:  dup
  IL_004e:  brtrue.s   IL_0067
  IL_0050:  pop
  IL_0051:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0056:  ldftn      ""ref readonly int Program.<>c.<Main>b__0_2(ref int)""
  IL_005c:  newobj     ""<>F{0000000d}<int, int>..ctor(object, System.IntPtr)""
  IL_0061:  dup
  IL_0062:  stsfld     ""<>F{0000000d}<int, int> Program.<>c.<>9__0_2""
  IL_0067:  call       ""void Program.Report(System.Delegate)""
  IL_006c:  ret
}");
        }

        [Fact]
        public void SynthesizedDelegateTypes_08()
        {
            var source =
@"#pragma warning disable 414
using System;
class Program
{
    static int i = 0;
    static void Main()
    {
        Report((int i) => { });
        Report((out int i) => { i = 0; });
        Report((ref int i) => { });
        Report((in int i) => { });
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput:
@"System.Action`1[System.Int32]
<>A{00000002}`1[System.Int32]
<>A{00000001}`1[System.Int32]
<>A{00000003}`1[System.Int32]
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      145 (0x91)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> Program.<>c.<>9__1_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_000e:  ldftn      ""void Program.<>c.<Main>b__1_0(int)""
  IL_0014:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""System.Action<int> Program.<>c.<>9__1_0""
  IL_001f:  call       ""void Program.Report(System.Delegate)""
  IL_0024:  ldsfld     ""<>A{00000002}<int> Program.<>c.<>9__1_1""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0032:  ldftn      ""void Program.<>c.<Main>b__1_1(out int)""
  IL_0038:  newobj     ""<>A{00000002}<int>..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""<>A{00000002}<int> Program.<>c.<>9__1_1""
  IL_0043:  call       ""void Program.Report(System.Delegate)""
  IL_0048:  ldsfld     ""<>A{00000001}<int> Program.<>c.<>9__1_2""
  IL_004d:  dup
  IL_004e:  brtrue.s   IL_0067
  IL_0050:  pop
  IL_0051:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0056:  ldftn      ""void Program.<>c.<Main>b__1_2(ref int)""
  IL_005c:  newobj     ""<>A{00000001}<int>..ctor(object, System.IntPtr)""
  IL_0061:  dup
  IL_0062:  stsfld     ""<>A{00000001}<int> Program.<>c.<>9__1_2""
  IL_0067:  call       ""void Program.Report(System.Delegate)""
  IL_006c:  ldsfld     ""<>A{00000003}<int> Program.<>c.<>9__1_3""
  IL_0071:  dup
  IL_0072:  brtrue.s   IL_008b
  IL_0074:  pop
  IL_0075:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_007a:  ldftn      ""void Program.<>c.<Main>b__1_3(in int)""
  IL_0080:  newobj     ""<>A{00000003}<int>..ctor(object, System.IntPtr)""
  IL_0085:  dup
  IL_0086:  stsfld     ""<>A{00000003}<int> Program.<>c.<>9__1_3""
  IL_008b:  call       ""void Program.Report(System.Delegate)""
  IL_0090:  ret
}");
        }

        [Fact]
        public void SynthesizedDelegateTypes_09()
        {
            var source =
@"#pragma warning disable 414
using System;
class Program
{
    static void M1(int i) { }
    static void M2(out int i) { i = 0; }
    static void M3(ref int i) { }
    static void M4(in int i) { }
    static void Main()
    {
        Report(M1);
        Report(M2);
        Report(M3);
        Report(M4);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput:
@"System.Action`1[System.Int32]
<>A{00000002}`1[System.Int32]
<>A{00000001}`1[System.Int32]
<>A{00000003}`1[System.Int32]
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       69 (0x45)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void Program.M1(int)""
  IL_0007:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_000c:  call       ""void Program.Report(System.Delegate)""
  IL_0011:  ldnull
  IL_0012:  ldftn      ""void Program.M2(out int)""
  IL_0018:  newobj     ""<>A{00000002}<int>..ctor(object, System.IntPtr)""
  IL_001d:  call       ""void Program.Report(System.Delegate)""
  IL_0022:  ldnull
  IL_0023:  ldftn      ""void Program.M3(ref int)""
  IL_0029:  newobj     ""<>A{00000001}<int>..ctor(object, System.IntPtr)""
  IL_002e:  call       ""void Program.Report(System.Delegate)""
  IL_0033:  ldnull
  IL_0034:  ldftn      ""void Program.M4(in int)""
  IL_003a:  newobj     ""<>A{00000003}<int>..ctor(object, System.IntPtr)""
  IL_003f:  call       ""void Program.Report(System.Delegate)""
  IL_0044:  ret
}");
        }

        [Fact]
        public void SynthesizedDelegateTypes_10()
        {
            var source =
@"#pragma warning disable 414
using System;
class Program
{
    static void M1(int i) { }
    static void M2(out int i) { i = 0; }
    static void M3(ref int i) { }
    static void M4(in int i) { }
    static void Main()
    {
        var d1 = M1;
        var d2 = M2;
        var d3 = M3;
        var d4 = M4;
        Report(d1);
        Report(d2);
        Report(d3);
        Report(d4);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"System.Action`1[System.Int32]
<>A{00000002}`1[System.Int32]
<>A{00000001}`1[System.Int32]
<>A{00000003}`1[System.Int32]
");
        }

        [WorkItem(55217, "https://github.com/dotnet/roslyn/issues/55217")]
        [Fact]
        public void SynthesizedDelegateTypes_11()
        {
            var source =
@"class Program
{
    unsafe static void Main()
    {
        var d1 = int* () => (int*)42;
        var d2 = (int* p) => { };
        var d3 = delegate*<void> () => default;
        var d4 = (delegate*<void> d) => { };
    }
}";

            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics(
                // (5,18): error CS8917: The delegate type could not be inferred.
                //         var d1 = int* () => (int*)42;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "int* () => (int*)42").WithLocation(5, 18),
                // (6,18): error CS8917: The delegate type could not be inferred.
                //         var d2 = (int* p) => { };
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "(int* p) => { }").WithLocation(6, 18),
                // (7,18): error CS8917: The delegate type could not be inferred.
                //         var d3 = delegate*<void> () => default;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "delegate*<void> () => default").WithLocation(7, 18),
                // (8,18): error CS8917: The delegate type could not be inferred.
                //         var d4 = (delegate*<void> d) => { };
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "(delegate*<void> d) => { }").WithLocation(8, 18));
        }

        [WorkItem(55217, "https://github.com/dotnet/roslyn/issues/55217")]
        [ConditionalFact(typeof(DesktopOnly))]
        public void SynthesizedDelegateTypes_12()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var d1 = (TypedReference x) => { };
        var d2 = (int x, RuntimeArgumentHandle y) => { };
        var d3 = (ArgIterator x) => { };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,18): error CS8917: The delegate type could not be inferred.
                //         var d1 = (TypedReference x) => { };
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "(TypedReference x) => { }").WithLocation(6, 18),
                // (7,18): error CS8917: The delegate type could not be inferred.
                //         var d2 = (int x, RuntimeArgumentHandle y) => { };
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "(int x, RuntimeArgumentHandle y) => { }").WithLocation(7, 18),
                // (8,18): error CS8917: The delegate type could not be inferred.
                //         var d3 = (ArgIterator x) => { };
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "(ArgIterator x) => { }").WithLocation(8, 18));
        }

        [WorkItem(55217, "https://github.com/dotnet/roslyn/issues/55217")]
        [Fact]
        public void SynthesizedDelegateTypes_13()
        {
            var source =
@"ref struct S<T> { }
class Program
{
    static void F1(int x, S<int> y) { }
    static S<T> F2<T>() => throw null;
    static void Main()
    {
        var d1 = F1;
        var d2 = F2<object>;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,18): error CS8917: The delegate type could not be inferred.
                //         var d1 = F1;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F1").WithLocation(8, 18),
                // (9,18): error CS8917: The delegate type could not be inferred.
                //         var d2 = F2<object>;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F2<object>").WithLocation(9, 18));
        }

        [Fact]
        public void SynthesizedDelegateTypes_14()
        {
            var source =
@"class Program
{
    static ref void F() { }
    static void Main()
    {
        var d1 = F;
        var d2 = (ref void () => { });
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (3,16): error CS1547: Keyword 'void' cannot be used in this context
                //     static ref void F() { }
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(3, 16),
                // (6,18): error CS8917: The delegate type could not be inferred.
                //         var d1 = F;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F").WithLocation(6, 18),
                // (7,19): error CS8917: The delegate type could not be inferred.
                //         var d2 = (ref void () => { });
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "ref void () => { }").WithLocation(7, 19),
                // (7,23): error CS1547: Keyword 'void' cannot be used in this context
                //         var d2 = (ref void () => { });
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(7, 23));
        }

        [Fact]
        public void SynthesizedDelegateTypes_15()
        {
            var source =
@"using System;
unsafe class Program
{
    static byte*[] F1() => null;
    static void F2(byte*[] a) { }
    static byte*[] F3(ref int i) => null;
    static void F4(ref byte*[] a) { }
    static void Main()
    {
        Report(int*[] () => null);
        Report((int*[] a) => { });
        Report(int*[] (ref int i) => null);
        Report((ref int*[] a) => { });
        Report(F1);
        Report(F2);
        Report(F3);
        Report(F4);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"System.Func`1[System.Int32*[]]
System.Action`1[System.Int32*[]]
<>F{00000001}`2[System.Int32,System.Int32*[]]
<>A{00000001}`1[System.Int32*[]]
System.Func`1[System.Byte*[]]
System.Action`1[System.Byte*[]]
<>F{00000001}`2[System.Int32,System.Byte*[]]
<>A{00000001}`1[System.Byte*[]]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_16()
        {
            var source =
@"using System;
unsafe class Program
{
    static delegate*<ref int>[] F1() => null;
    static void F2(delegate*<ref int, void>[] a) { }
    static delegate*<ref int>[] F3(ref int i) => null;
    static void F4(ref delegate*<ref int, void>[] a) { }
    static void Main()
    {
        Report(delegate*<int, ref int>[] () => null);
        Report((delegate*<int, ref int, void>[] a) => { });
        Report(delegate*<int, ref int>[] (ref int i) => null);
        Report((ref delegate*<int, ref int, void>[] a) => { });
        Report(F1);
        Report(F2);
        Report(F3);
        Report(F4);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"System.Func`1[(fnptr)[]]
System.Action`1[(fnptr)[]]
<>F{00000001}`2[System.Int32,(fnptr)[]]
<>A{00000001}`1[(fnptr)[]]
System.Func`1[(fnptr)[]]
System.Action`1[(fnptr)[]]
<>F{00000001}`2[System.Int32,(fnptr)[]]
<>A{00000001}`1[(fnptr)[]]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_17()
        {
            var source =
@"#nullable enable
using System;
class Program
{
    static void F1(object x, dynamic y) { }
    static void F2(IntPtr x, nint y) { }
    static void F3((int x, int y) t) { }
    static void F4(object? x, object?[] y) { }
    static void F5(ref object x, dynamic y) { }
    static void F6(IntPtr x, ref nint y) { }
    static void F7(ref (int x, int y) t) { }
    static void F8(object? x, ref object?[] y) { }
    static void Main()
    {
        Report(F1);
        Report(F2);
        Report(F3);
        Report(F4);
        Report(F5);
        Report(F6);
        Report(F7);
        Report(F8);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"System.Action`2[System.Object,System.Object]
System.Action`2[System.IntPtr,System.IntPtr]
System.Action`1[System.ValueTuple`2[System.Int32,System.Int32]]
System.Action`2[System.Object,System.Object[]]
<>A{00000001}`2[System.Object,System.Object]
<>A{00000004}`2[System.IntPtr,System.IntPtr]
<>A{00000001}`1[System.ValueTuple`2[System.Int32,System.Int32]]
<>A{00000004}`2[System.Object,System.Object[]]
");
        }

        private static void VerifyLocalDelegateType(SemanticModel model, VariableDeclaratorSyntax variable, string expectedLocal, string expectedInvokeMethod)
        {
            var local = (ILocalSymbol)model.GetDeclaredSymbol(variable)!;
            Assert.Equal(expectedLocal, local.ToTestDisplayString());
            var delegateType = ((INamedTypeSymbol)local.Type);
            Assert.Equal(Accessibility.Internal, delegateType.DeclaredAccessibility);
            Assert.Equal(expectedInvokeMethod, delegateType.DelegateInvokeMethod.ToTestDisplayString());
        }

        private static void VerifyExpressionType(SemanticModel model, ExpressionSyntax variable, string expectedSymbol, string expectedType)
        {
            var symbol = model.GetSymbolInfo(variable).Symbol;
            Assert.Equal(expectedSymbol, symbol.ToTestDisplayString());
            var type = model.GetTypeInfo(variable).Type;
            Assert.Equal(expectedType, type.ToTestDisplayString());
        }

        [Fact]
        public void TaskRunArgument()
        {
            var source =
@"using System.Threading.Tasks;
class Program
{
    static async Task F()
    {	
        await Task.Run(() => { });
    }
}";
            var verifier = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            var method = (MethodSymbol)verifier.TestData.GetMethodsByName()["Program.<>c.<F>b__0_0()"].Method;
            Assert.Equal("void Program.<>c.<F>b__0_0()", method.ToTestDisplayString());
            verifier.VerifyIL("Program.<>c.<F>b__0_0()",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }
    }
}
