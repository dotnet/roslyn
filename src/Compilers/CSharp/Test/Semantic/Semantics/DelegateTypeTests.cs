// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
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

        private static readonly string s_expressionOfTDelegate0ArgTypeName = ExecutionConditionUtil.IsDesktop ?
            "System.Linq.Expressions.Expression`1" :
            "System.Linq.Expressions.Expression0`1";
        private static readonly string s_expressionOfTDelegate1ArgTypeName = ExecutionConditionUtil.IsDesktop ?
            "System.Linq.Expressions.Expression`1" :
            "System.Linq.Expressions.Expression1`1";
        private static readonly string s_libPrefix = ExecutionConditionUtil.IsDesktop ? "mscorlib" : "netstandard";
        private static readonly string s_corePrefix = ExecutionConditionUtil.IsDesktop ? "System.Core" : "netstandard";

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
                // (7,16): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         d = () => { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(7, 16),
                // (8,13): error CS1660: Cannot convert anonymous method to type 'Delegate' because it is not a delegate type
                //         d = delegate () { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "System.Delegate").WithLocation(8, 13),
                // (9,51): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         System.Linq.Expressions.Expression e = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(9, 51));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MethodGroupConversions_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        object o = Main;
        ICloneable c = Main;
        Delegate d = Main;
        MulticastDelegate m = Main;
        Report(o);
        Report(c);
        Report(d);
        Report(m);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,20): error CS0428: Cannot convert method group 'Main' to non-delegate type 'object'. Did you intend to invoke the method?
                //         object o = Main;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "object").WithLocation(6, 20),
                // (7,24): error CS0428: Cannot convert method group 'Main' to non-delegate type 'ICloneable'. Did you intend to invoke the method?
                //         ICloneable c = Main;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "System.ICloneable").WithLocation(7, 24),
                // (8,22): error CS0428: Cannot convert method group 'Main' to non-delegate type 'Delegate'. Did you intend to invoke the method?
                //         Delegate d = Main;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "System.Delegate").WithLocation(8, 22),
                // (9,31): error CS0428: Cannot convert method group 'Main' to non-delegate type 'MulticastDelegate'. Did you intend to invoke the method?
                //         MulticastDelegate m = Main;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "System.MulticastDelegate").WithLocation(9, 31));

            comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,20): warning CS8974: Converting method group 'Main' to non-delegate type 'object'. Did you intend to invoke the method?
                //         object o = Main;
                Diagnostic(ErrorCode.WRN_MethGrpToNonDel, "Main").WithArguments("Main", "object").WithLocation(6, 20));

            CompileAndVerify(comp, expectedOutput:
@"System.Action
System.Action
System.Action
System.Action
");
        }

        [Fact]
        public void MethodGroupConversions_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var o = (object)Main;
        var c = (ICloneable)Main;
        var d = (Delegate)Main;
        var m = (MulticastDelegate)Main;
        Report(o);
        Report(c);
        Report(d);
        Report(m);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,17): error CS0030: Cannot convert type 'method' to 'object'
                //         var o = (object)Main;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object)Main").WithArguments("method", "object").WithLocation(6, 17),
                // (7,17): error CS0030: Cannot convert type 'method' to 'ICloneable'
                //         var c = (ICloneable)Main;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ICloneable)Main").WithArguments("method", "System.ICloneable").WithLocation(7, 17),
                // (8,17): error CS0030: Cannot convert type 'method' to 'Delegate'
                //         var d = (Delegate)Main;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Delegate)Main").WithArguments("method", "System.Delegate").WithLocation(8, 17),
                // (9,17): error CS0030: Cannot convert type 'method' to 'MulticastDelegate'
                //         var m = (MulticastDelegate)Main;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(MulticastDelegate)Main").WithArguments("method", "System.MulticastDelegate").WithLocation(9, 17));

            comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"System.Action
System.Action
System.Action
System.Action
");
        }

        [Fact]
        public void MethodGroupConversions_03()
        {
            var source =
@"class Program
{
    static void Main()
    {
        System.Linq.Expressions.Expression e = F;
        e = (System.Linq.Expressions.Expression)F;
        System.Linq.Expressions.LambdaExpression l = F;
        l = (System.Linq.Expressions.LambdaExpression)F;
    }
    static int F() => 1;
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,48): error CS0428: Cannot convert method group 'F' to non-delegate type 'Expression'. Did you intend to invoke the method?
                //         System.Linq.Expressions.Expression e = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "System.Linq.Expressions.Expression").WithLocation(5, 48),
                // (6,13): error CS0030: Cannot convert type 'method' to 'Expression'
                //         e = (System.Linq.Expressions.Expression)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.Linq.Expressions.Expression)F").WithArguments("method", "System.Linq.Expressions.Expression").WithLocation(6, 13),
                // (7,54): error CS0428: Cannot convert method group 'F' to non-delegate type 'LambdaExpression'. Did you intend to invoke the method?
                //         System.Linq.Expressions.LambdaExpression l = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "System.Linq.Expressions.LambdaExpression").WithLocation(7, 54),
                // (8,13): error CS0030: Cannot convert type 'method' to 'LambdaExpression'
                //         l = (System.Linq.Expressions.LambdaExpression)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.Linq.Expressions.LambdaExpression)F").WithArguments("method", "System.Linq.Expressions.LambdaExpression").WithLocation(8, 13));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,48): error CS0428: Cannot convert method group 'F' to non-delegate type 'Expression'. Did you intend to invoke the method?
                //         System.Linq.Expressions.Expression e = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "System.Linq.Expressions.Expression").WithLocation(5, 48),
                // (6,13): error CS0428: Cannot convert method group 'F' to non-delegate type 'Expression'. Did you intend to invoke the method?
                //         e = (System.Linq.Expressions.Expression)F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "(System.Linq.Expressions.Expression)F").WithArguments("F", "System.Linq.Expressions.Expression").WithLocation(6, 13),
                // (7,54): error CS0428: Cannot convert method group 'F' to non-delegate type 'LambdaExpression'. Did you intend to invoke the method?
                //         System.Linq.Expressions.LambdaExpression l = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "System.Linq.Expressions.LambdaExpression").WithLocation(7, 54),
                // (8,13): error CS0428: Cannot convert method group 'F' to non-delegate type 'LambdaExpression'. Did you intend to invoke the method?
                //         l = (System.Linq.Expressions.LambdaExpression)F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "(System.Linq.Expressions.LambdaExpression)F").WithArguments("F", "System.Linq.Expressions.LambdaExpression").WithLocation(8, 13));
        }

        [Fact]
        public void MethodGroupConversions_04()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void F() { }
    static void F(object o) { }
    static void Main()
    {
        object o = F;
        ICloneable c = F;
        Delegate d = F;
        MulticastDelegate m = F;
        Expression e = F;
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,20): error CS8917: The delegate type could not be inferred.
                //         object o = F;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F").WithLocation(9, 20),
                // (10,24): error CS8917: The delegate type could not be inferred.
                //         ICloneable c = F;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F").WithLocation(10, 24),
                // (11,22): error CS8917: The delegate type could not be inferred.
                //         Delegate d = F;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F").WithLocation(11, 22),
                // (12,31): error CS8917: The delegate type could not be inferred.
                //         MulticastDelegate m = F;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F").WithLocation(12, 31),
                // (13,24): error CS0428: Cannot convert method group 'F' to non-delegate type 'Expression'. Did you intend to invoke the method?
                //         Expression e = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "System.Linq.Expressions.Expression").WithLocation(13, 24));
        }

        [Fact]
        public void LambdaConversions_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        object o = () => { };
        ICloneable c = () => { };
        Delegate d = () => { };
        MulticastDelegate m = () => { };
        Report(o);
        Report(c);
        Report(d);
        Report(m);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,23): error CS1660: Cannot convert lambda expression to type 'object' because it is not a delegate type
                //         object o = () => { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "object").WithLocation(6, 23),
                // (7,27): error CS1660: Cannot convert lambda expression to type 'ICloneable' because it is not a delegate type
                //         ICloneable c = () => { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.ICloneable").WithLocation(7, 27),
                // (8,25): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         Delegate d = () => { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(8, 25),
                // (9,34): error CS1660: Cannot convert lambda expression to type 'MulticastDelegate' because it is not a delegate type
                //         MulticastDelegate m = () => { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.MulticastDelegate").WithLocation(9, 34));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Action
System.Action
System.Action
System.Action
");
        }

        [Fact]
        public void LambdaConversions_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var o = (object)(() => { });
        var c = (ICloneable)(() => { });
        var d = (Delegate)(() => { });
        var m = (MulticastDelegate)(() => { });
        Report(o);
        Report(c);
        Report(d);
        Report(m);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,29): error CS1660: Cannot convert lambda expression to type 'object' because it is not a delegate type
                //         var o = (object)(() => { });
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "object").WithLocation(6, 29),
                // (7,33): error CS1660: Cannot convert lambda expression to type 'ICloneable' because it is not a delegate type
                //         var c = (ICloneable)(() => { });
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.ICloneable").WithLocation(7, 33),
                // (8,31): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         var d = (Delegate)(() => { });
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(8, 31),
                // (9,40): error CS1660: Cannot convert lambda expression to type 'MulticastDelegate' because it is not a delegate type
                //         var m = (MulticastDelegate)(() => { });
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.MulticastDelegate").WithLocation(9, 40));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Action
System.Action
System.Action
System.Action
");
        }

        [Fact]
        public void LambdaConversions_03()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        Expression e = () => 1;
        Report(e);
        e = (Expression)(() => 2);
        Report(e);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,27): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         Expression e = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(7, 27),
                // (9,29): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         e = (Expression)(() => 2);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(9, 29));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
$@"{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
");
        }

        [Fact]
        public void LambdaConversions_04()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        LambdaExpression e = () => 1;
        Report(e);
        e = (LambdaExpression)(() => 2);
        Report(e);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,33): error CS1660: Cannot convert lambda expression to type 'LambdaExpression' because it is not a delegate type
                //         LambdaExpression e = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.LambdaExpression").WithLocation(7, 33),
                // (9,35): error CS1660: Cannot convert lambda expression to type 'LambdaExpression' because it is not a delegate type
                //         e = (LambdaExpression)(() => 2);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.LambdaExpression").WithLocation(9, 35));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
$@"{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
");
        }

        [Fact]
        public void LambdaConversions_05()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        Delegate d = x => x;
        object o = (object)(x => x);
        Expression e = x => x;
        e = (Expression)(x => x);
        LambdaExpression l = x => x;
        l = (LambdaExpression)(x => x);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,24): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         Delegate d = x => x;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(7, 24),
                // (8,31): error CS1660: Cannot convert lambda expression to type 'object' because it is not a delegate type
                //         object o = (object)(x => x);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "object").WithLocation(8, 31),
                // (9,26): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         Expression e = x => x;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(9, 26),
                // (10,28): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         e = (Expression)(x => x);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(10, 28),
                // (11,32): error CS1660: Cannot convert lambda expression to type 'LambdaExpression' because it is not a delegate type
                //         LambdaExpression l = x => x;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.LambdaExpression").WithLocation(11, 32),
                // (12,34): error CS1660: Cannot convert lambda expression to type 'LambdaExpression' because it is not a delegate type
                //         l = (LambdaExpression)(x => x);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.LambdaExpression").WithLocation(12, 34));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,24): error CS8917: The delegate type could not be inferred.
                //         Delegate d = x => x;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(7, 24),
                // (8,31): error CS8917: The delegate type could not be inferred.
                //         object o = (object)(x => x);
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(8, 31),
                // (9,26): error CS8917: The delegate type could not be inferred.
                //         Expression e = x => x;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(9, 26),
                // (10,28): error CS8917: The delegate type could not be inferred.
                //         e = (Expression)(x => x);
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(10, 28),
                // (11,32): error CS8917: The delegate type could not be inferred.
                //         LambdaExpression l = x => x;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(11, 32),
                // (12,34): error CS8917: The delegate type could not be inferred.
                //         l = (LambdaExpression)(x => x);
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(12, 34));
        }

        [Fact]
        public void LambdaConversions_06()
        {
            var sourceA =
@"namespace System.Linq.Expressions
{
    public class LambdaExpression<T>
    {
    }
}";
            var sourceB =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        LambdaExpression<Func<int>> l = () => 1;
        l = (LambdaExpression<Func<int>>)(() => 2);
    }
}";

            var expectedDiagnostics = new[]
            {
                    // 1.cs(7,44): error CS1660: Cannot convert lambda expression to type 'LambdaExpression<Func<int>>' because it is not a delegate type
                    //         LambdaExpression<Func<int>> l = () => 1;
                    Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.LambdaExpression<System.Func<int>>").WithLocation(7, 44),
                    // 1.cs(8,46): error CS1660: Cannot convert lambda expression to type 'LambdaExpression<Func<int>>' because it is not a delegate type
                    //         l = (LambdaExpression<Func<int>>)(() => 2);
                    Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.LambdaExpression<System.Func<int>>").WithLocation(8, 46)
            };

            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(new[] { sourceA, sourceB });
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void LambdaConversions_07()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        System.Delegate d = () => Main;
        System.Linq.Expressions.Expression e = () => Main;
        Report(d);
        Report(e);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
$@"System.Func`1[System.Action]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Action]]
");
        }

        [Fact]
        public void AnonymousMethod_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        object o = delegate () { };
        ICloneable c = delegate () { };
        Delegate d = delegate () { };
        MulticastDelegate m = delegate () { };
        Report(o);
        Report(c);
        Report(d);
        Report(m);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,20): error CS1660: Cannot convert anonymous method to type 'object' because it is not a delegate type
                //         object o = delegate () { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "object").WithLocation(6, 20),
                // (7,24): error CS1660: Cannot convert anonymous method to type 'ICloneable' because it is not a delegate type
                //         ICloneable c = delegate () { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "System.ICloneable").WithLocation(7, 24),
                // (8,22): error CS1660: Cannot convert anonymous method to type 'Delegate' because it is not a delegate type
                //         Delegate d = delegate () { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "System.Delegate").WithLocation(8, 22),
                // (9,31): error CS1660: Cannot convert anonymous method to type 'MulticastDelegate' because it is not a delegate type
                //         MulticastDelegate m = delegate () { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "System.MulticastDelegate").WithLocation(9, 31));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Action
System.Action
System.Action
System.Action
");
        }

        [Fact]
        public void AnonymousMethod_02()
        {
            var source =
@"using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        System.Linq.Expressions.Expression e = delegate () { return 1; };
        e = (Expression)delegate () { return 2; };
        LambdaExpression l = delegate () { return 3; };
        l = (LambdaExpression)delegate () { return 4; };
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,48): error CS1660: Cannot convert anonymous method to type 'Expression' because it is not a delegate type
                //         System.Linq.Expressions.Expression e = delegate () { return 1; };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "System.Linq.Expressions.Expression").WithLocation(6, 48),
                // (7,25): error CS1660: Cannot convert anonymous method to type 'Expression' because it is not a delegate type
                //         e = (Expression)delegate () { return 2; };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "System.Linq.Expressions.Expression").WithLocation(7, 25),
                // (8,30): error CS1660: Cannot convert anonymous method to type 'LambdaExpression' because it is not a delegate type
                //         LambdaExpression l = delegate () { return 3; };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "System.Linq.Expressions.LambdaExpression").WithLocation(8, 30),
                // (9,31): error CS1660: Cannot convert anonymous method to type 'LambdaExpression' because it is not a delegate type
                //         l = (LambdaExpression)delegate () { return 4; };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "System.Linq.Expressions.LambdaExpression").WithLocation(9, 31));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,48): error CS1946: An anonymous method expression cannot be converted to an expression tree
                //         System.Linq.Expressions.Expression e = delegate () { return 1; };
                Diagnostic(ErrorCode.ERR_AnonymousMethodToExpressionTree, "delegate").WithLocation(6, 48),
                // (7,13): error CS1946: An anonymous method expression cannot be converted to an expression tree
                //         e = (Expression)delegate () { return 2; };
                Diagnostic(ErrorCode.ERR_AnonymousMethodToExpressionTree, "(Expression)delegate () { return 2; }").WithLocation(7, 13),
                // (8,30): error CS1946: An anonymous method expression cannot be converted to an expression tree
                //         LambdaExpression l = delegate () { return 3; };
                Diagnostic(ErrorCode.ERR_AnonymousMethodToExpressionTree, "delegate").WithLocation(8, 30),
                // (9,13): error CS1946: An anonymous method expression cannot be converted to an expression tree
                //         l = (LambdaExpression)delegate () { return 4; };
                Diagnostic(ErrorCode.ERR_AnonymousMethodToExpressionTree, "(LambdaExpression)delegate () { return 4; }").WithLocation(9, 13));
        }

        [Fact]
        public void DynamicConversion()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        dynamic d;
        d = Main;
        d = () => 1;
    }
    static void Report(dynamic d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,13): error CS0428: Cannot convert method group 'Main' to non-delegate type 'dynamic'. Did you intend to invoke the method?
                //         d = Main;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "dynamic").WithLocation(7, 13),
                // (8,16): error CS1660: Cannot convert lambda expression to type 'dynamic' because it is not a delegate type
                //         d = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "dynamic").WithLocation(8, 16));
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
            yield return getData("static void F(int x, ref int y) { }", "F", "F", "<>A{00000008}<System.Int32, System.Int32>");
            yield return getData("static void F(int x, in int y) { }", "F", "F", "<>A{00000018}<System.Int32, System.Int32>");
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
            yield return getData("() => ref args[0]", "<>F{00000001}<System.String>", "<anonymous delegate>");
            yield return getData("() => { }", "System.Action");
            yield return getData("(int x, int y) => { }", "System.Action<System.Int32, System.Int32>");
            yield return getData("(out int x, int y) => { x = 0; }", "<>A{00000002}<System.Int32, System.Int32>", "<anonymous delegate>");
            yield return getData("(int x, ref int y) => { x = 0; }", "<>A{00000008}<System.Int32, System.Int32>", "<anonymous delegate>");
            yield return getData("(int x, in int y) => { x = 0; }", "<>A{00000018}<System.Int32, System.Int32>", "<anonymous delegate>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) => { }", "System.Action<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) => { }", "<>A<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32>", "<anonymous delegate>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) => _1", "System.Func<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) => _1", "<>F<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Int32>", "<anonymous delegate>");
            yield return getData("static () => 1", "System.Func<System.Int32>");
            yield return getData("async () => { await System.Threading.Tasks.Task.Delay(0); }", "System.Func<System.Threading.Tasks.Task>");
            yield return getData("static async () => { await System.Threading.Tasks.Task.Delay(0); return 0; }", "System.Func<System.Threading.Tasks.Task<System.Int32>>");
            yield return getData("() => Main", "System.Func<System.Action<System.String[]>>");
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

            // Distinct names for distinct signatures with > 16 parameters: https://github.com/dotnet/roslyn/issues/55570
            yield return getData("(int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, int _9, int _10, int _11, int _12, int _13, int _14, int _15, int _16, ref int _17) => { }", "<>A{1000000000000}<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32>", "<anonymous delegate>");
            yield return getData("(int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, int _9, int _10, int _11, int _12, int _13, int _14, int _15, int _16, in int _17)  => { }", "<>A{3000000000000}<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32>", "<anonymous delegate>");

            static object?[] getData(string expr, string? expectedType, string? expectedDisplayString = null) =>
                new object?[] { expr, expectedType, expectedDisplayString ?? expectedType };
        }

        public static IEnumerable<object?[]> GetAnonymousMethodData()
        {
            yield return getData("delegate { }", null);
            yield return getData("delegate () { return 1; }", "System.Func<System.Int32>");
            yield return getData("delegate () { return ref args[0]; }", "<>F{00000001}<System.String>", "<anonymous delegate>");
            yield return getData("delegate () { }", "System.Action");
            yield return getData("delegate (int x, int y) { }", "System.Action<System.Int32, System.Int32>");
            yield return getData("delegate (out int x, int y) { x = 0; }", "<>A{00000002}<System.Int32, System.Int32>", "<anonymous delegate>");
            yield return getData("delegate (int x, ref int y) { x = 0; }", "<>A{00000008}<System.Int32, System.Int32>", "<anonymous delegate>");
            yield return getData("delegate (int x, in int y) { x = 0; }", "<>A{00000018}<System.Int32, System.Int32>", "<anonymous delegate>");
            yield return getData("delegate (int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) { }", "System.Action<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object>");
            yield return getData("delegate (int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) { }", "<>A<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32>", "<anonymous delegate>");
            yield return getData("delegate (int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) { return _1; }", "System.Func<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32>");
            yield return getData("delegate (int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) { return _1; }", "<>F<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Int32>", "<anonymous delegate>");

            static object?[] getData(string expr, string? expectedType, string? expectedDisplayString = null) =>
                new object?[] { expr, expectedType, expectedDisplayString ?? expectedType };
        }

        [Theory]
        [MemberData(nameof(GetLambdaData))]
        [MemberData(nameof(GetAnonymousMethodData))]
        public void AnonymousFunction_ImplicitConversion(string anonymousFunction, string? expectedType, string? expectedDisplayString)
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
                    // 0.cs(5,32): error CS8917: The delegate type could not be inferred.
                    //         System.Delegate d = () => throw null;
                    Diagnostic(ErrorCode.ERR_CannotInferDelegateType, anonymousFunction.StartsWith("delegate") ? "delegate" : "=>"));
            }
            else
            {
                CompileAndVerify(comp, expectedOutput: expectedType);
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<AnonymousFunctionExpressionSyntax>().Single();
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Equal(expectedDisplayString, typeInfo.Type?.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);

            var symbolInfo = model.GetSymbolInfo(expr);
            var method = (IMethodSymbol)symbolInfo.Symbol!;
            Assert.Equal(MethodKind.LambdaMethod, method.MethodKind);
            if (typeInfo.Type is { })
            {
                Assert.True(HaveMatchingSignatures(((INamedTypeSymbol)typeInfo.Type!).DelegateInvokeMethod!, method));
            }
        }

        [Theory]
        [MemberData(nameof(GetLambdaData))]
        [MemberData(nameof(GetAnonymousMethodData))]
        public void AnonymousFunction_ExplicitConversion(string anonymousFunction, string? expectedType, string? expectedDisplayString)
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
                    // 0.cs(5,41): error CS8917: The delegate type could not be inferred.
                    //         object o = (System.Delegate)(() => throw null);
                    Diagnostic(ErrorCode.ERR_CannotInferDelegateType, anonymousFunction.StartsWith("delegate") ? "delegate" : "=>"));
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
            Assert.Equal(expectedDisplayString, typeInfo.ConvertedType?.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(expr);
            var method = (IMethodSymbol)symbolInfo.Symbol!;
            Assert.Equal(MethodKind.LambdaMethod, method.MethodKind);
            if (typeInfo.Type is { })
            {
                Assert.True(HaveMatchingSignatures(((INamedTypeSymbol)typeInfo.Type!).DelegateInvokeMethod!, method));
            }
        }

        private static bool HaveMatchingSignatures(IMethodSymbol methodA, IMethodSymbol methodB)
        {
            return MemberSignatureComparer.CSharp10MethodGroupSignatureComparer.Equals(methodA.GetSymbol<MethodSymbol>(), methodB.GetSymbol<MethodSymbol>());
        }

        public static IEnumerable<object?[]> GetExpressionData()
        {
            yield return getData("x => x", null);
            yield return getData("() => 1", "System.Func<System.Int32>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16) => _1", "System.Func<System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32, System.Object, System.Int32>");
            yield return getData("(int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) => _1", "<anonymous delegate>");
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
                    // (5,50): error CS8917: The delegate type could not be inferred.
                    //         System.Linq.Expressions.Expression e = x => x;
                    Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(5, 50));
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
                    // (5,59): error CS8917: The delegate type could not be inferred.
                    //         object o = (System.Linq.Expressions.Expression)(x => x);
                    Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(5, 59));
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
                // (6,26): error CS8917: The delegate type could not be inferred.
                //         Delegate d0 = x0 => { _ = x0.Length; object y0 = 0; _ = y0.Length; };
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(6, 26),
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
            yield return getData("internal void F() { }", "internal void F<T>() { }", "F<int>", "F<int>", null, "System.Action"); // different arity
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

        [Fact]
        public void MethodGroup_BaseAndDerivedTypes_1()
        {
            var source = """
new B().M();

partial class B
{
    public void M()
    {
        System.Delegate d = F;
        d.DynamicInvoke();
        System.Console.Write(d.GetDelegateTypeName());
    }
}
abstract class A
{
    internal void F() { System.Console.Write("RAN "); }
}
partial class B : A
{
    internal void F<T>() => throw null;
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "RAN System.Action");
            verifier.VerifyDiagnostics();

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
            yield return getData("internal static void F(this object x, object y) { }", "internal static void F<T>(this object x, T y) { }", "this.F<int>", "F<int>", null, "B.F", "System.Action<System.Int32>"); // different arity
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

        [Fact]
        public void MethodGroup_ExtensionMethodsSameScope_1()
        {
            var source = """
new Program().M();

partial class Program
{
    public void M()
    {
        System.Delegate d = this.F;
        d.DynamicInvoke(42);
        System.Console.Write("{0}: {1}", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }
}
static class A
{
    internal static void F(this object x, object y) { System.Console.Write($"RAN({y}) "); }
}
static class B
{
    internal static void F<T>(this object x, T y) => throw null;
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "RAN(42) A.F: System.Action<System.Object>");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer!.Value;
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);

            var symbolInfo = model.GetSymbolInfo(expr);
            // https://github.com/dotnet/roslyn/issues/52870: GetSymbolInfo() should return resolved method from method group.
            Assert.Null(symbolInfo.Symbol);

            Assert.Equal(["void System.Object.F(System.Object y)", "void System.Object.F<T>(T y)"],
                model.GetMemberGroup(expr).ToTestDisplayStrings());
        }

        [Fact]
        public void MethodGroup_ExtensionMethodsSameScope_2()
        {
            var source = """
new Program().M();

partial class Program
{
    public void M()
    {
        System.Delegate d = this.F;
        d.DynamicInvoke();
        System.Console.Write("{0}: {1}", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }
}
static class A
{
    internal static void F<T>(this object x) => throw null;
}
static class B
{
    internal static void F(this object x) { System.Console.Write("RAN "); }
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "RAN B.F: System.Action");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer!.Value;
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);

            var symbolInfo = model.GetSymbolInfo(expr);
            // https://github.com/dotnet/roslyn/issues/52870: GetSymbolInfo() should return resolved method from method group.
            Assert.Null(symbolInfo.Symbol);

            Assert.Equal(["void System.Object.F<T>()", "void System.Object.F()"],
                model.GetMemberGroup(expr).ToTestDisplayStrings());
        }

        [Fact]
        public void MethodGroup_ExtensionMethodsSameScope_3()
        {
            var source = """
new Program().M();

partial class Program
{
    public void M()
    {
        System.Delegate d = this.F<int>;
    }
}
static class A
{
    internal static void F<T>(this T t) where T : class { }
}
static class B
{
    internal static void F<T>(this T t) where T : struct { }
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // 0.cs(7,34): error CS8917: The delegate type could not be inferred.
                //         System.Delegate d = this.F<int>;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F<int>").WithLocation(7, 34)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer!.Value;
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);

            var symbolInfo = model.GetSymbolInfo(expr);
            // https://github.com/dotnet/roslyn/issues/52870: GetSymbolInfo() should return resolved method from method group.
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(model.GetMemberGroup(expr));
        }

        public static IEnumerable<object?[]> GetExtensionMethodsDifferentScopeData_CSharp10()
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
            yield return getData("internal static void F<T>(this T t) where T : class { }", "internal static void F<T>(this T t) where T : struct { }", "this.F<int>", "F<int>",
                new[]
                {
                    // (6,34): error CS0123: No overload for 'F' matches delegate 'Action'
                    //         System.Delegate d = this.F<int>;
                    Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "F<int>").WithArguments("F", "System.Action").WithLocation(6, 34)
                 }); // different type parameter constraints

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
        [MemberData(nameof(GetExtensionMethodsDifferentScopeData_CSharp10))]
        public void MethodGroup_ExtensionMethodsDifferentScope_CSharp10(string methodA, string methodB, string methodGroupExpression, DiagnosticDescription[]? expectedDiagnostics, string? expectedMethod, string? expectedType)
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
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseExe);
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

        public static IEnumerable<object?[]> GetExtensionMethodsDifferentScopeData_CSharp13()
        {
            yield return getData("internal static void F(this object x) { }", "internal static void F(this object x) { }", "this.F", "F", null, "A.F", "System.Action"); // hiding
            yield return getData("internal static void F(this object x) { }", "internal static void F(this object y) { }", "this.F", "F", null, "A.F", "System.Action"); // different parameter name
            yield return getData("internal static void F(this object x) { }", "internal static void F(this string x) { }", "string.Empty.F", "F", null, "A.F", "System.Action"); // different parameter type
            yield return getData("internal static void F(this object x) { }", "internal static void F(this string x) { }", "this.F", "F", null, "A.F", "System.Action"); // different parameter type
            yield return getData("internal static void F(this object x, System.IntPtr y) { }", "internal static void F(this object x, nint y) { }", "this.F", "F", null, "A.F", "System.Action<System.IntPtr>"); // System.IntPtr/nint
            yield return getData("internal static nint F(this object x) => throw null;", "internal static System.IntPtr F(this object x) => throw null;", "this.F", "F", null, "A.F", "System.Func<System.IntPtr>"); // System.IntPtr/nint
            yield return getData("internal static void F(this object x, object y) { }", "internal static void F<T>(this object x, T y) { }", "this.F<int>", "F<int>", null, "N.B.F", "System.Action<System.Int32>"); // different arity
            yield return getData("internal static void F<T>(this object x) { }", "internal static void F(this object x) { }", "this.F<int>", "F<int>", null, "A.F", "System.Action"); // different arity
            yield return getData("internal static void F<T>(this T t) where T : class { }", "internal static void F<T>(this T t) { }", "this.F<object>", "F<object>", null, "A.F", "System.Action"); // different type parameter constraints
            yield return getData("internal static void F<T>(this T t) { }", "internal static void F<T>(this T t) where T : class { }", "this.F<object>", "F<object>", null, "A.F", "System.Action"); // different type parameter constraints

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
        [MemberData(nameof(GetExtensionMethodsDifferentScopeData_CSharp13))]
        public void MethodGroup_ExtensionMethodsDifferentScope_CSharp13(string methodA, string methodB, string methodGroupExpression, DiagnosticDescription[]? expectedDiagnostics, string? expectedMethod, string? expectedType)
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
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.Regular13, options: TestOptions.ReleaseExe);
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

        [Theory, CombinatorialData]
        public void MethodGroup_ExtensionMethodsDifferentScope_CSharp13_2(bool useCSharp13)
        {
            var source = """
using N;

new Program().M();

partial class Program
{
    public void M()
    {
        System.Delegate d = this.F;
        System.Console.Write("{0}: {1}", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }
}

static class A
{
    internal static void F(this object x) { }
}
namespace N
{
    static class B
    {
        internal static void F(this object x, object y) { }
    }
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // 0.cs(1,1): hidden CS8019: Unnecessary using directive.
                // using N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1)
                );

            CompileAndVerify(comp, expectedOutput: "A.F: System.Action");

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

        [Theory, CombinatorialData]
        public void MethodGroup_ExtensionMethodsDifferentScope_CSharp13_3(bool useCSharp13)
        {
            var source = """
using N;

new Program().M();

partial class Program
{
    public void M()
    {
        System.Delegate d = this.F;
        System.Console.Write("{0}: {1}", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }
}

static class A
{
    internal static void F(this object x, object y) { }
}
namespace N
{
    static class B
    {
        internal static void F(this object x, ref object y) { }
    }
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // 0.cs(1,1): hidden CS8019: Unnecessary using directive.
                // using N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1)
                );

            CompileAndVerify(comp, expectedOutput: "A.F: System.Action<System.Object>");

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

        [Theory, CombinatorialData]
        public void MethodGroup_ExtensionMethodsDifferentScope_CSharp13_4(bool useCSharp13)
        {
            var source = """
using N;

new Program().M();

partial class Program
{
    public void M()
    {
        System.Delegate d = this.F;
        System.Console.Write("{0}: {1}", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }
}

static class A
{
    internal static void F(this object x, ref object y) { }
}
namespace N
{
    static class B
    {
        internal static void F(this object x, object y) { }
    }
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // 0.cs(1,1): hidden CS8019: Unnecessary using directive.
                // using N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1)
                );

            CompileAndVerify(comp, expectedOutput: "A.F: <>A{00000001}<System.Object>");

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

        [Theory, CombinatorialData]
        public void MethodGroup_ExtensionMethodsDifferentScope_CSharp13_5(bool useCSharp13)
        {
            var source = """
using N;

new Program().M();

partial class Program
{
    public void M()
    {
        System.Delegate d = this.F;
        System.Console.Write("{0}: {1}", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }
}

static class A
{
    internal static object F(this object x) => throw null;
}
namespace N
{
    static class B
    {
        internal static ref object F(this object x) => throw null;
    }
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // 0.cs(1,1): hidden CS8019: Unnecessary using directive.
                // using N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1)
                );

            CompileAndVerify(comp, expectedOutput: "A.F: System.Func<System.Object>");

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

        [Theory, CombinatorialData]
        public void MethodGroup_ExtensionMethodsDifferentScope_CSharp13_6(bool useCSharp13)
        {
            var source = """
using N;

new Program().M();

partial class Program
{
    public void M()
    {
        System.Delegate d = this.F;
        System.Console.Write("{0}: {1}", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }
}

static class A
{
    internal static ref object F(this object x) => throw null;
}
namespace N
{
    static class B
    {
        internal static object F(this object x) => throw null;
    }
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // 0.cs(1,1): hidden CS8019: Unnecessary using directive.
                // using N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1)
                );

            CompileAndVerify(comp, expectedOutput: "A.F: <>F{00000001}<System.Object>");

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

        [Theory, CombinatorialData]
        public void MethodGroup_ExtensionMethodsDifferentScope_CSharp13_7(bool useCSharp13)
        {
            var source = """
using N;

new Program().M();

partial class Program
{
    public void M()
    {
        System.Delegate d = this.F;
        System.Console.Write("{0}: {1}", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }
}

static class A
{
    internal static void F(this object x, object y) { }
}
namespace N
{
    static class B
    {
        internal static void F<T>(this object x, T y) { }
    }
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // 0.cs(1,1): hidden CS8019: Unnecessary using directive.
                // using N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1)
                );

            CompileAndVerify(comp, expectedOutput: "A.F: System.Action<System.Object>");

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

        [Theory, CombinatorialData]
        public void MethodGroup_ExtensionMethodsDifferentScope_CSharp13_8(bool useCSharp13)
        {
            var source = """
using N;

new Program().M();

partial class Program
{
    public void M()
    {
        System.Delegate d = this.F;
        d.DynamicInvoke();
        System.Console.Write("{0}: {1}", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }
}

static class A
{
    internal static void F<T>(this object x) => throw null;
}
namespace N
{
    static class B
    {
        internal static void F(this object x) { System.Console.Write("RAN "); }
    }
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "RAN N.B.F: System.Action");
            verifier.VerifyDiagnostics();

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

        [Theory, CombinatorialData]
        public void MethodGroup_ExtensionMethodsDifferentScope_CSharp13_9(bool useCSharp13)
        {
            var source = """
using N;

new Program().M();

partial class Program
{
    public void M()
    {
        System.Delegate d = this.F<int>;
        System.Console.Write("{0}: {1}", d.GetDelegateMethodName(), d.GetDelegateTypeName());
    }
}

static class A
{
    internal static void F<T>(this T t) where T : class { }
}
namespace N
{
    static class B
    {
        internal static void F<T>(this T t) where T : struct { }
    }
}
""";
            var comp = CreateCompilation(new[] { source, s_utils }, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // 0.cs(9,34): error CS8917: The delegate type could not be inferred.
                //         System.Delegate d = this.F<int>;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F<int>").WithLocation(9, 34)
                );

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

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_InstanceBeforeExtensions(bool useCSharp13)
        {
            // Instance method takes priority over extensions for method group natural type in C# 13
            var source = """
System.Action x = new C().M;
x();

System.Action<object> y = new C().M;
y(42);

var z = new C().M;
z();

public class C
{
    public void M()
    {
        System.Console.Write("C.M ");
    }
}

public static class E
{
    public static void M(this C c, object o)
    {
        System.Console.Write("E.M ");
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (7,9): error CS8917: The delegate type could not be inferred.
                // var z = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(7, 9)
                );

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "C.M E.M C.M");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Action", typeInfo.ConvertedType!.ToTestDisplayString());
            Assert.Equal("void C.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
            Assert.Equal(["void C.M()", "void C.M(System.Object o)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_AmbiguityWithScope_SameSignature(bool useCSharp13)
        {
            // All extensions in a given scope are considered together for method group natural type
            // In C# 13, multiple extension methods in inner scope having the same signature means
            // we can pick a natural type for the method group
            var source = """
using N;

System.Action x = new C().M;
var z = new C().M;

public class C { }

public static class E1
{
    public static void M(this C c) { }
}

public static class E2
{
    public static void M(this C c) { }
}

namespace N
{
    public static class E3
    {
        public static void M(this C c, object o) { } // ignored
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (3,19): error CS0121: The call is ambiguous between the following methods or properties: 'E1.M(C)' and 'E2.M(C)'
                // System.Action x = new C().M;
                Diagnostic(ErrorCode.ERR_AmbigCall, "new C().M").WithArguments("E1.M(C)", "E2.M(C)").WithLocation(3, 19),
                // (4,9): error CS8917: The delegate type could not be inferred.
                // var z = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(4, 9)
                );

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1),
                // (3,19): error CS0121: The call is ambiguous between the following methods or properties: 'E1.M(C)' and 'E2.M(C)'
                // System.Action x = new C().M;
                Diagnostic(ErrorCode.ERR_AmbigCall, "new C().M").WithArguments("E1.M(C)", "E2.M(C)").WithLocation(3, 19),
                // (4,9): error CS0121: The call is ambiguous between the following methods or properties: 'E1.M(C)' and 'E2.M(C)'
                // var z = new C().M;
                Diagnostic(ErrorCode.ERR_AmbigCall, "new C().M").WithArguments("E1.M(C)", "E2.M(C)").WithLocation(4, 9)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Action", typeInfo.ConvertedType!.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
            Assert.Equal(["void C.M()", "void C.M()", "void C.M(System.Object o)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_AmbiguityWithScope_DifferentSignature(bool useCSharp13)
        {
            // All extensions in a given scope are considered together for method group natural type
            // Two inner scope extension with different signatures means
            // we can't determine the natural type of the method group
            var source = """
using N;

System.Action x = new C().M;
var z = new C().M;

public class C { }

public static class E1
{
    public static void M(this C c) { }
}

public static class E2
{
    public static void M(this C c, object o) { }
}

namespace N
{
    public static class E3
    {
        public static void M(this C c, object o) { } // ignored
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1),
                // (4,9): error CS8917: The delegate type could not be inferred.
                // var z = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(4, 9)
                );

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1),
                // (4,9): error CS8917: The delegate type could not be inferred.
                // var z = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(4, 9)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.True(typeInfo.ConvertedType!.IsErrorType());
            Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);

            Assert.Equal(["void C.M()", "void C.M(System.Object o)", "void C.M(System.Object o)"],
                model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_InnerScopeBeforeOuterScope(bool useCSharp13)
        {
            // In C# 13, extensions in inner scopes take precedence over those in outer scopes
            var source = """
using N;

System.Action x = new C().M;
x();

var z = new C().M;
z();

public class C { }

public static class E1
{
    public static void M(this C c)
    {
        System.Console.Write("E1.M ");
    }
}

namespace N
{
    public static class E2
    {
        public static void M(this C c, object o) { } // ignored
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (6,9): error CS8917: The delegate type could not be inferred.
                // var z = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(6, 9)
                );

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1)
                );

            CompileAndVerify(comp, expectedOutput: "E1.M E1.M");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Action", typeInfo.ConvertedType!.ToTestDisplayString());
            Assert.Equal("void C.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
            Assert.Equal(["void C.M()", "void C.M(System.Object o)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_InaccessibleInInnerScope(bool useCSharp13)
        {
            // Inaccessible extension method in inner scope is ignored
            var source = """
using N;

System.Action x = new C().M;
x();

var z = new C().M;
z();

public class C { }

public static class E1
{
    private static void M(this C c) { } // ignored
}

namespace N
{
    public static class E2
    {
        public static void M(this C c)
        {
            System.Console.Write("E2.M ");
        }
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "E2.M E2.M");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Action", typeInfo.ConvertedType!.ToTestDisplayString());
            Assert.Equal("void C.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
            Assert.Equal(["void C.M()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_InaccessibleInstance(bool useCSharp13)
        {
            // Inaccessible instance method is ignored
            var source = """
var z = new C().M;
z();

public class C
{
    protected static void M(object o) { } // ignored
}

public static class E
{
    public static void M(this C c)
    {
        System.Console.Write("E.M ");
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "E.M");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Action", typeInfo.ConvertedType!.ToTestDisplayString());
            // https://github.com/dotnet/roslyn/issues/52870: GetSymbolInfo() should return resolved method from method group.
            Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
            Assert.Equal(["void C.M()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_InstanceReceiver(bool useCSharp13)
        {
            // Static method is ignored on instance receiver
            var source = """
System.Action x = new C().M;
x();

var z = new C().M;
z();

public class C
{
    static void M() { } // ignored
}

public static class E
{
    public static void M(this C c)
    {
        System.Console.Write("E.M ");
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "E.M E.M");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Action", typeInfo.ConvertedType!.ToTestDisplayString());
            // https://github.com/dotnet/roslyn/issues/52870: GetSymbolInfo() should return resolved method from method group.
            Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
            Assert.Equal(["void C.M()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_TypeReceiver(bool useCSharp13)
        {
            // Instance method and extension methods are ignored on type receiver
            var source = """
System.Action x = C.M;
x();

var z = C.M;
z();

public class C
{
    public void M(C c) { } // ignored
}

public static class E
{
    public static void M(this C c) { }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (1,21): error CS0123: No overload for 'M' matches delegate 'Action'
                // System.Action x = C.M;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M").WithArguments("M", "System.Action").WithLocation(1, 21),
                // (4,9): error CS8917: The delegate type could not be inferred.
                // var z = C.M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "C.M").WithLocation(4, 9)
                );

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,21): error CS0123: No overload for 'M' matches delegate 'Action'
                // System.Action x = C.M;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M").WithArguments("M", "System.Action").WithLocation(1, 21),
                // (4,9): error CS8917: The delegate type could not be inferred.
                // var z = C.M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "C.M").WithLocation(4, 9)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "C.M").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.True(typeInfo.ConvertedType!.IsErrorType());
            Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
            Assert.Equal(["void C.M(C c)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_NoTypeArguments_ExtensionMethodHasZeroArity(bool useCSharp13)
        {
            var source = """
var z = new C().M;
z();

public class C
{
    public void M<T>() { }
}

public static class E
{
    public static void M(this C c)
    {
        System.Console.Write("E.M");
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (1,9): error CS8917: The delegate type could not be inferred.
                // var z = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(1, 9)
                );

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "E.M");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Action", typeInfo.ConvertedType!.ToTestDisplayString());
            Assert.Equal("void C.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
            Assert.Equal(["void C.M<T>()", "void C.M()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_NoTypeArguments_OuterExtensionMethodHasZeroArity(bool useCSharp13)
        {
            var source = """
using N;

System.Action x = new C().M;
x();

var z = new C().M;
z();

public class C { }

public static class E1
{
    public static void M<T>(this C c) => throw null;
}

namespace N
{
    public static class E2
    {
        public static void M(this C c) { System.Console.Write("E2.M "); }
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (6,9): error CS8917: The delegate type could not be inferred.
                // var z = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(6, 9)
                );

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "E2.M E2.M");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Action", typeInfo.ConvertedType!.ToTestDisplayString());
            Assert.Equal("void C.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
            Assert.Equal(["void C.M<T>()", "void C.M()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_NoTypeArguments_InnerExtensionMethodHasArityOne(bool useCSharp13)
        {
            var source = """
using N;

System.Action x = new C().M;
x();

var z = new C().M;
z();

public class C { }

public static class E1
{
    public static void M<T>(this T c) { System.Console.Write("E1.M "); }
}

namespace N
{
    public static class E2
    {
        public static void M(this C c) => throw null;
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (6,9): error CS8917: The delegate type could not be inferred.
                // var z = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(6, 9)
                );

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "E1.M E1.M");
            verifier.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Action", typeInfo.ConvertedType!.ToTestDisplayString());
            Assert.Equal("void C.M<C>()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
            Assert.Equal(["void C.M<C>()", "void C.M()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_TypeArgumentsDoNotMatchInstanceMethod(bool useCSharp13)
        {
            // An instance method differing from requested non-zero arity is ignored
            var source = """
System.Action x = new C().M<int, int>;
x();

var z = new C().M<int, int>;
z();

public class C
{
    public void M<T>(C c) { }
}

public static class E
{
    public static void M<T, U>(this C c)
    {
        System.Console.Write("E.M<T, U> ");
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (4,9): error CS8917: The delegate type could not be inferred.
                // var z = new C().M<int, int>;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M<int, int>").WithLocation(4, 9)
                );

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "E.M<T, U> E.M<T, U>");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M<int, int>").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Action", typeInfo.ConvertedType!.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
            Assert.Equal(["void C.M<System.Int32, System.Int32>()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_TypeArgumentsDoNotMatchInnerScopeExtensionMethod(bool useCSharp13)
        {
            // An extension method in inner scope differing from requested non-zero arity is ignored
            var source = """
using N;

System.Action x = new C().M<int, int>;
x();

var z = new C().M<int, int>;
z();

public class C { }

public static class E1
{
    public static void M<T>(this C c) => throw null;
}

namespace N
{
    public static class E2
    {
        public static void M<T, U>(this C c)
        {
            System.Console.Write("E2.M<T, U> ");
        }
    }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "E2.M<T, U> E2.M<T, U>");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M<int, int>").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Action", typeInfo.ConvertedType!.ToTestDisplayString());
            Assert.Equal("void C.M<System.Int32, System.Int32>()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
            Assert.Equal(["void C.M<System.Int32, System.Int32>()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_NoTypeArguments(bool useCSharp13)
        {
            var source = """
System.Action x = new C().M;
var z = new C().M;

public class C
{
    public void M<T>() => throw null;
}

public static class E
{
    public static void M<T, U>(this C c) => throw null;
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (1,19): error CS0411: The type arguments for method 'C.M<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                // System.Action x = new C().M;
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "new C().M").WithArguments("C.M<T>()").WithLocation(1, 19),
                // (2,9): error CS8917: The delegate type could not be inferred.
                // var z = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(2, 9)
                );

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,19): error CS0411: The type arguments for method 'C.M<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                // System.Action x = new C().M;
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "new C().M").WithArguments("C.M<T>()").WithLocation(1, 19),
                // (2,9): error CS8917: The delegate type could not be inferred.
                // var z = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(2, 9)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").Last();
            var typeInfo = model.GetTypeInfo(memberAccess);
            Assert.Null(typeInfo.Type);
            Assert.True(typeInfo.ConvertedType!.IsErrorType());
            Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
            Assert.Equal(["void C.M<T>()", "void C.M<T, U>()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_BreakingChange(bool useCSharp13)
        {
            var source = """
var c = new C();
var d = new D();
d.Del(c.M); // used to bind to DExt.Del, now binds to D.Del

public class C
{
    public void M() { }
}

public static class CExt
{
    public static void M(this C c, object o) { }
}

public class D
{
    public void Del(System.Delegate d) { System.Console.Write("ran12"); }
}

public static class DExt
{
    public static void Del(this D d, System.Action<object> action) { System.Console.Write("ran11"); }
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ran11");

            comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ran12");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "c.M");
            // https://github.com/dotnet/roslyn/issues/52870: GetSymbolInfo() should return resolved method from method group.
            Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
            Assert.Equal(["void C.M()", "void C.M(System.Object o)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Fact, WorkItem("https://github.com/dotnet/csharplang/issues/7364")]
        public void MethodGroup_ScopeByScope_SameSignatureDifferentArities()
        {
            var source = """
var x = new C().M;
x();

public class C
{
    public void M() { System.Console.Write("ran"); }
    public void M<T>() => throw null;
}
""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (1,9): error CS8917: The delegate type could not be inferred.
                // var x = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(1, 9));

            comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "ran");
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69222")]
        public void MethodGroup_GenericExtensionMethod()
        {
            var source = """
var d = new object().M;
d();

static class E
{
    public static void M<T>(this T t)
    {
        System.Console.Write("ran");
    }
}
""";
            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "ran");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
            Assert.Equal("void System.Object.M<System.Object>()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
            Assert.Equal(["void System.Object.M<System.Object>()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69222")]
        public void MethodGroup_GenericExtensionMethod_Nested()
        {
            var source = """
var d = new C<int, long>().M;
d();

class C<T, U> { }

static class E
{
    public static void M<T1, T2>(this C<T1, T2> t)
    {
        System.Console.Write("ran");
    }
}
""";
            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "ran");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C<int, long>().M");
            Assert.Equal("void C<System.Int32, System.Int64>.M<System.Int32, System.Int64>()",
                model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

            Assert.Equal(["void C<System.Int32, System.Int64>.M<System.Int32, System.Int64>()"],
                 model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69222")]
        public void MethodGroup_GenericExtensionMethod_UnsubstitutedTypeParameter()
        {
            var source = """
var d = new C().M;
d();

class C { }

static class E
{
    public static void M<T>(this C c) { }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,9): error CS8917: The delegate type could not be inferred.
                // var d = new C().M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M").WithLocation(1, 9)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
            Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
            Assert.Equal(["void C.M<T>()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69222")]
        public void MethodGroup_GenericInstanceMethod_Constraint()
        {
            var source = """
var x = new C().M<int>;
x();

public class C
{
    public void M<T>()
    {
        System.Console.Write("ran");
    }

    public void M<T>(object o) where T : class { }
}
""";
            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "ran");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M<int>");
            Assert.Equal("void C.M<System.Int32>()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

            Assert.Equal(["void C.M<System.Int32>()", "void C.M<System.Int32>(System.Object o)"],
                model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69222")]
        public void MethodGroup_GenericInstanceMethod_Constraint_Nullability_Ambiguity()
        {
            var source = """
#nullable enable
var x = new C().M<object?>;
x();

public class C
{
    public void M<T>() { }

    public void M<T>(object o) where T : class { }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,9): error CS8917: The delegate type could not be inferred.
                // var x = new C().M<object?>;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().M<object?>").WithLocation(2, 9)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M<object?>");
            Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);

            Assert.Equal(["void C.M<System.Object?>()", "void C.M<System.Object?>(System.Object o)"],
                model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69222")]
        public void MethodGroup_GenericInstanceMethod_Constraint_Nullability_Warn()
        {
            var source = """
#nullable enable
var x = new C().M<object?>;
x();

public class C
{
    public void M<T>() where T : class
    {
        System.Console.Write("ran");
    }
}
""";
            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "ran");
            verifier.VerifyDiagnostics(
                // (2,9): warning CS8634: The type 'object?' cannot be used as type parameter 'T' in the generic type or method 'C.M<T>()'. Nullability of type argument 'object?' doesn't match 'class' constraint.
                // var x = new C().M<object?>;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "new C().M<object?>").WithArguments("C.M<T>()", "T", "object?").WithLocation(2, 9)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M<object?>");
            Assert.Equal("void C.M<System.Object?>()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
            Assert.Equal(["void C.M<System.Object?>()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69222")]
        public void MethodGroup_GenericExtensionMethod_Constraint_ImplicitTypeArguments()
        {
            var source = """
var x = new object().M;
x();

static class E1
{
    public static void M<T>(this T t)
    {
        System.Console.Write("ran");
    }
}

static class E2
{
    public static void M<T>(this T t, object ignored) where T : struct { }
}
""";
            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "ran");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
            Assert.Equal("void System.Object.M<System.Object>()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
            Assert.Equal(["void System.Object.M<System.Object>()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69222")]
        public void MethodGroup_GenericExtensionMethod_Constraint_ExplicitTypeArguments()
        {
            var source = """
var x = new object().M<object>;
x();

static class E1
{
    public static void M<T>(this T t)
    {
        System.Console.Write("ran");
    }
}

static class E2
{
    public static void M<T>(this T t, object ignored) where T : struct { }
}
""";
            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "ran");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M<object>");
            Assert.Equal("void System.Object.M<System.Object>()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

            Assert.Equal(["void System.Object.M<System.Object>()", "void System.Object.M<System.Object>(System.Object ignored)"],
                model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Theory, CombinatorialData]
        public void GenericExtensionMethod_ArityIgnoredInSignature(bool useCSharp13)
        {
            var source = """
var x = new object().F;
x();

static class B
{
    internal static void F<T>(this T x) => throw null;
}

static class A
{
    internal static void F(this object x) { System.Console.Write("A.F"); }
}
""";
            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (1,9): error CS8917: The delegate type could not be inferred.
                // var x = new object().F;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new object().F").WithLocation(1, 9));

            var comp = CreateCompilation(source, parseOptions: useCSharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "A.F");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().F");
            Assert.Equal("void System.Object.F()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

            Assert.Equal(["void System.Object.F<System.Object>()", "void System.Object.F()"],
                model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
        }

        [Fact]
        public void GenericExtensionMethod_Constraint()
        {
            // In C# 13, a method group that cannot be successfully substituted (ie. respecting constraints)
            // does not contribute to the natural type determination
            var source = """
var x = new C().F<object>;

class C { }

static class E
{
    public static void F<T>(this C c) where T : struct { }
}
""";
            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (1,9): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'E.F<T>(C)'
                // var x = new C().F<object>;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "new C().F<object>").WithArguments("E.F<T>(C)", "T", "object").WithLocation(1, 9));

            CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
                // (1,9): error CS8917: The delegate type could not be inferred.
                // var x = new C().F<object>;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().F<object>").WithLocation(1, 9));

            CreateCompilation(source).VerifyDiagnostics(
                // (1,9): error CS8917: The delegate type could not be inferred.
                // var x = new C().F<object>;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().F<object>").WithLocation(1, 9));
        }

        [Fact]
        public void GenericInstanceMethod_Constraint()
        {
            // In C# 13, a method that cannot be successfully substituted (ie. respecting constraints)
            // does not contribute to the natural type determination
            var source = """
var x = new C().F<object>;

class C
{
    public void F<T>() where T : struct { }
}
""";
            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (1,9): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'C.F<T>()'
                // var x = new C().F<object>;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "new C().F<object>").WithArguments("C.F<T>()", "T", "object").WithLocation(1, 9));

            CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
                // (1,9): error CS8917: The delegate type could not be inferred.
                // var x = new C().F<object>;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().F<object>").WithLocation(1, 9));

            CreateCompilation(source).VerifyDiagnostics(
                // (1,9): error CS8917: The delegate type could not be inferred.
                // var x = new C().F<object>;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "new C().F<object>").WithLocation(1, 9));
        }

        [Fact, WorkItem("https://github.com/dotnet/csharplang/discussions/129")]
        public void BoundInferenceFromMethodGroup()
        {
            var source = """
int result1 = Test(IsEven); // 1
bool result2 = Test2(IsEven);
int result3 = Test3(IsEven); // 2
bool result4 = Test4(IsEven);
System.Func<int, bool> result5 = Test5(IsEven);

delegate bool Predicate<T>(T t);
delegate T Predicate2<T>(int i);

partial class Program
{
    public static bool IsEven(int x) => x % 2 == 0;

    public static T Test<T>(System.Func<T, bool> predicate) => throw null;
    public static T Test2<T>(System.Func<int, T> predicate) => throw null;
    public static T Test3<T>(Predicate<T> predicate) => throw null;
    public static T Test4<T>(Predicate2<T> predicate) => throw null;
    public static T Test5<T>(T predicate) => throw null;
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,15): error CS0411: The type arguments for method 'Program.Test<T>(Func<T, bool>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                // int result1 = Test(IsEven); // 1
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Program.Test<T>(System.Func<T, bool>)").WithLocation(1, 15),
                // (3,15): error CS0411: The type arguments for method 'Program.Test3<T>(Predicate<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                // int result3 = Test3(IsEven); // 2
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test3").WithArguments("Program.Test3<T>(Predicate<T>)").WithLocation(3, 15));
        }

        [Fact]
        public void Discard()
        {
            var source =
@"class Program
{
    static void F() { }
    static void F(object o) { }
    static void Main()
    {
        _ = Main;
        _ = F;
        _ = () => { };
        _ = x => x;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (7,9): error CS8183: Cannot infer the type of implicitly-typed discard.
                //         _ = Main;
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "_").WithLocation(7, 9),
                // (8,9): error CS8183: Cannot infer the type of implicitly-typed discard.
                //         _ = F;
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "_").WithLocation(8, 9),
                // (9,9): error CS8183: Cannot infer the type of implicitly-typed discard.
                //         _ = () => { };
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "_").WithLocation(9, 9),
                // (10,9): error CS8183: Cannot infer the type of implicitly-typed discard.
                //         _ = x => x;
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "_").WithLocation(10, 9)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [WorkItem(55923, "https://github.com/dotnet/roslyn/issues/55923")]
        [Fact]
        public void ConvertMethodGroupToObject_01()
        {
            var source =
@"class Program
{
    static object GetValue() => 0;
    static void Main()
    {
        object x = GetValue;
        x = GetValue;
        x = (object)GetValue;
#pragma warning disable 8974
        object y = GetValue;
        y = GetValue;
        y = (object)GetValue;
#pragma warning restore 8974
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,20): error CS0428: Cannot convert method group 'GetValue' to non-delegate type 'object'. Did you intend to invoke the method?
                //         object x = GetValue;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "GetValue").WithArguments("GetValue", "object").WithLocation(6, 20),
                // (7,13): error CS0428: Cannot convert method group 'GetValue' to non-delegate type 'object'. Did you intend to invoke the method?
                //         x = GetValue;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "GetValue").WithArguments("GetValue", "object").WithLocation(7, 13),
                // (8,13): error CS0030: Cannot convert type 'method' to 'object'
                //         x = (object)GetValue;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object)GetValue").WithArguments("method", "object").WithLocation(8, 13),
                // (10,20): error CS0428: Cannot convert method group 'GetValue' to non-delegate type 'object'. Did you intend to invoke the method?
                //         object y = GetValue;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "GetValue").WithArguments("GetValue", "object").WithLocation(10, 20),
                // (11,13): error CS0428: Cannot convert method group 'GetValue' to non-delegate type 'object'. Did you intend to invoke the method?
                //         y = GetValue;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "GetValue").WithArguments("GetValue", "object").WithLocation(11, 13),
                // (12,13): error CS0030: Cannot convert type 'method' to 'object'
                //         y = (object)GetValue;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object)GetValue").WithArguments("method", "object").WithLocation(12, 13));

            var expectedDiagnostics = new[]
            {
                // (6,20): warning CS8974: Converting method group 'GetValue' to non-delegate type 'object'. Did you intend to invoke the method?
                //         object x = GetValue;
                Diagnostic(ErrorCode.WRN_MethGrpToNonDel, "GetValue").WithArguments("GetValue", "object").WithLocation(6, 20),
                // (7,13): warning CS8974: Converting method group 'GetValue' to non-delegate type 'object'. Did you intend to invoke the method?
                //         x = GetValue;
                Diagnostic(ErrorCode.WRN_MethGrpToNonDel, "GetValue").WithArguments("GetValue", "object").WithLocation(7, 13)
            };

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [WorkItem(55923, "https://github.com/dotnet/roslyn/issues/55923")]
        [Fact]
        public void ConvertMethodGroupToObject_02()
        {
            var source =
@"class Program
{
    static int F() => 0;
    static object F1() => F;
    static object F2() => (object)F;
    static object F3() { return F; }
    static object F4() { return (object)F; }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,27): error CS0428: Cannot convert method group 'F' to non-delegate type 'object'. Did you intend to invoke the method?
                //     static object F1() => F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "object").WithLocation(4, 27),
                // (5,27): error CS0030: Cannot convert type 'method' to 'object'
                //     static object F2() => (object)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object)F").WithArguments("method", "object").WithLocation(5, 27),
                // (6,33): error CS0428: Cannot convert method group 'F' to non-delegate type 'object'. Did you intend to invoke the method?
                //     static object F3() { return F; }
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "object").WithLocation(6, 33),
                // (7,33): error CS0030: Cannot convert type 'method' to 'object'
                //     static object F4() { return (object)F; }
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object)F").WithArguments("method", "object").WithLocation(7, 33));

            var expectedDiagnostics = new[]
            {
                // (4,27): warning CS8974: Converting method group 'F' to non-delegate type 'object'. Did you intend to invoke the method?
                //     static object F1() => F;
                Diagnostic(ErrorCode.WRN_MethGrpToNonDel, "F").WithArguments("F", "object").WithLocation(4, 27),
                // (6,33): warning CS8974: Converting method group 'F' to non-delegate type 'object'. Did you intend to invoke the method?
                //     static object F3() { return F; }
                Diagnostic(ErrorCode.WRN_MethGrpToNonDel, "F").WithArguments("F", "object").WithLocation(6, 33)
            };

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [WorkItem(55923, "https://github.com/dotnet/roslyn/issues/55923")]
        [Fact]
        public void ConvertMethodGroupToObject_03()
        {
            var source =
@"class Program
{
    static int F() => 0;
    static void Main()
    {
        object[] a = new[] { F, (object)F, F };
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,30): error CS0428: Cannot convert method group 'F' to non-delegate type 'object'. Did you intend to invoke the method?
                //         object[] a = new[] { F, (object)F, F };
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "object").WithLocation(6, 30),
                // (6,33): error CS0030: Cannot convert type 'method' to 'object'
                //         object[] a = new[] { F, (object)F, F };
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object)F").WithArguments("method", "object").WithLocation(6, 33),
                // (6,44): error CS0428: Cannot convert method group 'F' to non-delegate type 'object'. Did you intend to invoke the method?
                //         object[] a = new[] { F, (object)F, F };
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "object").WithLocation(6, 44));

            var expectedDiagnostics = new[]
            {
                // (6,30): warning CS8974: Converting method group 'F' to non-delegate type 'object'. Did you intend to invoke the method?
                //         object[] a = new[] { F, (object)F, F };
                Diagnostic(ErrorCode.WRN_MethGrpToNonDel, "F").WithArguments("F", "object").WithLocation(6, 30),
                // (6,44): warning CS8974: Converting method group 'F' to non-delegate type 'object'. Did you intend to invoke the method?
                //         object[] a = new[] { F, (object)F, F };
                Diagnostic(ErrorCode.WRN_MethGrpToNonDel, "F").WithArguments("F", "object").WithLocation(6, 44)
            };

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (14,15): error CS8917: The delegate type could not be inferred.
                //         d = p.F2;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F2").WithLocation(14, 15));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular13);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var f1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "p.F1");
            var typeInfo1 = model.GetTypeInfo(f1);
            Assert.Null(typeInfo1.Type);
            Assert.Equal("System.Delegate", typeInfo1.ConvertedType!.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(f1).Symbol);
            Assert.Equal(new[] { "void System.Object.F1(System.Int32 y)" }, model.GetMemberGroup(f1).ToTestDisplayStrings());

            var f2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "p.F2");
            var typeInfo2 = model.GetTypeInfo(f2);
            Assert.Null(typeInfo2.Type);
            Assert.Equal("System.Delegate", typeInfo2.ConvertedType!.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(f2).Symbol);

            Assert.Equal(new[] { "void Program.F2(System.Int32 x)", "void System.Object.F2()" },
                model.GetMemberGroup(f2).ToTestDisplayStrings());
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
        d = p.F1; // 1
        d = p.F2; // 2
        d = p.F3;
        d = E1.F1;
        d = E2.F1;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (22,15): error CS8917: The delegate type could not be inferred.
                //         d = p.F1; // 1
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F1").WithLocation(22, 15),
                // (23,15): error CS8917: The delegate type could not be inferred.
                //         d = p.F2; // 2
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F2").WithLocation(23, 15));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular13);
            comp.VerifyDiagnostics(
                // (23,15): error CS8917: The delegate type could not be inferred.
                //         d = p.F2; // 2
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
        d = t.F1; // 1
        d = t.F2; // 2
        d = t.F1<int>;
        d = t.F1<T>;
        d = t.F2<T, object>;
        d = t.F2<object, T>;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (11,15): error CS8917: The delegate type could not be inferred.
                //         d = t.F1; // 1
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "F1").WithLocation(11, 15),
                // (12,15): error CS8917: The delegate type could not be inferred.
                //         d = t.F2; // 2
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
@"using System;
unsafe class Program
{
    static int* F() { Console.WriteLine(nameof(F)); return (int*)0; }
    static void Main()
    {
        var d1 = F;
        var d2 = (int x, int* y) => { Console.WriteLine((x, (int)y)); };
        d1.Invoke();
        d2.Invoke(1, (int*)2);
        Report(d1);
        Report(d2);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"F
(1, 2)
<>f__AnonymousDelegate0
<>f__AnonymousDelegate1
");
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
        Report(F1<string>());
        Report(F2<string>());
    }
    static Delegate F1<T>()
    {
        return (T t, ref int i) => { };
    }
    static Delegate F2<T>()
    {
        return (ref T () => throw null);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";
            CompileAndVerify(source, expectedOutput:
@"<>A{00000008}`2[System.String,System.Int32]
<>F{00000001}`1[System.String]
");
        }

        [Fact]
        public void Member_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Console.WriteLine((() => { }).GetType());
    }
}";

            var expectedDiagnostics = new[]
            {
                // (6,27): error CS0023: Operator '.' cannot be applied to operand of type 'lambda expression'
                //         Console.WriteLine((() => { }).GetType());
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "(() => { }).GetType").WithArguments(".", "lambda expression").WithLocation(6, 27)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Member_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Console.WriteLine(Main.GetType());
    }
}";

            var expectedDiagnostics = new[]
            {
                // (6,27): error CS0119: 'Program.Main()' is a method, which is not valid in the given context
                //         Console.WriteLine(Main.GetType());
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Main").WithArguments("Program.Main()", "method").WithLocation(6, 27)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(expectedDiagnostics);
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
                // (10,16): error CS0570: 'A.F1(object)' is not supported by the language
                //         Report(A.F1);
                Diagnostic(ErrorCode.ERR_BindToBogus, "A.F1").WithArguments("A.F1(object)").WithLocation(10, 16),
                // (10,16): error CS0648: '' is a type not supported by the language
                //         Report(A.F1);
                Diagnostic(ErrorCode.ERR_BogusType, "A.F1").WithArguments("").WithLocation(10, 16),
                // (11,16): error CS0570: 'A.F2()' is not supported by the language
                //         Report(A.F2);
                Diagnostic(ErrorCode.ERR_BindToBogus, "A.F2").WithArguments("A.F2()").WithLocation(11, 16),
                // (11,16): error CS0648: '' is a type not supported by the language
                //         Report(A.F2);
                Diagnostic(ErrorCode.ERR_BogusType, "A.F2").WithArguments("").WithLocation(11, 16));
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
}
.class public abstract System.MulticastDelegate extends System.Delegate
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
                // (6,13): error CS8917: The delegate type could not be inferred.
                //         d = Main;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "Main").WithLocation(6, 13),
                // (6,13): error CS0518: Predefined type 'System.Action' is not defined or imported
                //         d = Main;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Main").WithArguments("System.Action").WithLocation(6, 13),
                // (7,13): error CS0518: Predefined type 'System.Func`1' is not defined or imported
                //         d = () => 1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "() => 1").WithArguments("System.Func`1").WithLocation(7, 13),
                // (7,16): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         d = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(7, 16));
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
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "() => 1").WithArguments("System.Linq.Expressions.Expression`1").WithLocation(5, 48),
                // (5,51): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         System.Linq.Expressions.Expression e = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(5, 51));
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

        // Expression<T> not derived from Expression.
        private static MetadataReference GetCorlibWithExpressionOfTNotDerivedType()
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
}
.class public abstract System.Linq.Expressions.LambdaExpression extends System.Linq.Expressions.Expression
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed System.Linq.Expressions.Expression`1<T> extends System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}";
            return CompileIL(sourceA, prependDefaultHeader: false, autoInherit: false);
        }

        [Fact]
        public void SystemLinqExpressionsExpression_NotDerivedType_01()
        {
            var refA = GetCorlibWithExpressionOfTNotDerivedType();

            var sourceB =
@"class Program
{
    static void Main()
    {
        System.Linq.Expressions.Expression e = () => 1;
    }
}";

            var comp = CreateEmptyCompilation(sourceB, new[] { refA });
            comp.VerifyDiagnostics(
                // (5,51): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         System.Linq.Expressions.Expression e = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(5, 51));
        }

        [Fact]
        public void SystemLinqExpressionsExpression_NotDerivedType_02()
        {
            var refA = GetCorlibWithExpressionOfTNotDerivedType();

            var sourceB =
@"class Program
{
    static T F<T>(T t) where T : System.Linq.Expressions.Expression => t;
    static void Main()
    {
        var e = F(() => 1);
    }
}";

            var comp = CreateEmptyCompilation(sourceB, new[] { refA });
            comp.VerifyDiagnostics(
                // (6,17): error CS0311: The type 'System.Linq.Expressions.Expression<System.Func<int>>' cannot be used as type parameter 'T' in the generic type or method 'Program.F<T>(T)'. There is no implicit reference conversion from 'System.Linq.Expressions.Expression<System.Func<int>>' to 'System.Linq.Expressions.Expression'.
                //         var e = F(() => 1);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "F").WithArguments("Program.F<T>(T)", "System.Linq.Expressions.Expression", "T", "System.Linq.Expressions.Expression<System.Func<int>>").WithLocation(6, 17));
        }

        /// <summary>
        /// System.Linq.Expressions as a type rather than a namespace.
        /// </summary>
        [Fact]
        public void SystemLinqExpressions_IsType()
        {
            var sourceA =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public class String { }
    public class Type { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct IntPtr { }
    public abstract class Delegate { }
    public abstract class MulticastDelegate : Delegate { }
    public delegate T Func<T>();
}
namespace System.Linq
{
    public class Expressions
    {
        public abstract class Expression { }
        public abstract class LambdaExpression : Expression { }
        public sealed class Expression<T> : LambdaExpression { }
    }
}";
            var sourceB =
@"class Program
{
    static void Main()
    {
        System.Linq.Expressions.Expression e1 = () => 1;
        System.Linq.Expressions.LambdaExpression e2 = () => 2;
        System.Linq.Expressions.Expression<System.Func<int>> e3 = () => 3;
    }
}";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB });
            comp.VerifyDiagnostics(
                // 1.cs(5,52): error CS1660: Cannot convert lambda expression to type 'Expressions.Expression' because it is not a delegate type
                //         System.Linq.Expressions.Expression e1 = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(5, 52),
                // 1.cs(6,58): error CS1660: Cannot convert lambda expression to type 'Expressions.LambdaExpression' because it is not a delegate type
                //         System.Linq.Expressions.LambdaExpression e2 = () => 2;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.LambdaExpression").WithLocation(6, 58),
                // 1.cs(7,70): error CS1660: Cannot convert lambda expression to type 'Expressions.Expression<Func<int>>' because it is not a delegate type
                //         System.Linq.Expressions.Expression<System.Func<int>> e3 = () => 3;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression<System.Func<int>>").WithLocation(7, 70));
        }

        /// <summary>
        /// System.Linq.Expressions as a nested namespace.
        /// </summary>
        [Fact]
        public void SystemLinqExpressions_IsNestedNamespace()
        {
            var sourceA =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public class String { }
    public class Type { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct IntPtr { }
    public abstract class Delegate { }
    public abstract class MulticastDelegate : Delegate { }
    public delegate T Func<T>();
}
namespace Root.System.Linq.Expressions
{
    public abstract class Expression { }
    public abstract class LambdaExpression : Expression { }
    public sealed class Expression<T> : LambdaExpression { }
}";
            var sourceB =
@"using System;
using Root.System.Linq.Expressions;
class Program
{
    static void Main()
    {
        Expression e1 = () => 1;
        LambdaExpression e2 = () => 2;
        Expression<Func<int>> e3 = () => 3;
    }
}";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB });
            comp.VerifyDiagnostics(
                // 1.cs(7,28): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         Expression e1 = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "Root.System.Linq.Expressions.Expression").WithLocation(7, 28),
                // 1.cs(8,34): error CS1660: Cannot convert lambda expression to type 'LambdaExpression' because it is not a delegate type
                //         LambdaExpression e2 = () => 2;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "Root.System.Linq.Expressions.LambdaExpression").WithLocation(8, 34),
                // 1.cs(9,39): error CS1660: Cannot convert lambda expression to type 'Expression<Func<int>>' because it is not a delegate type
                //         Expression<Func<int>> e3 = () => 3;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "Root.System.Linq.Expressions.Expression<System.Func<int>>").WithLocation(9, 39));
        }

        [Fact]
        public void SystemIntPtr_Missing_01()
        {
            var sourceA =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public class String { }
    public class Type { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public abstract class Delegate { }
    public abstract class MulticastDelegate : Delegate { }
}";
            var sourceB =
@"class Program
{
    static void Main()
    {
        System.Delegate d;
        d = (ref int i) => i;
    }
}";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // error CS0518: Predefined type 'System.IntPtr' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.IntPtr").WithLocation(1, 1),
                // (6,13): error CS0518: Predefined type 'System.IntPtr' is not defined or imported
                //         d = (ref int i) => i;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(ref int i) => i").WithArguments("System.IntPtr").WithLocation(6, 13));
        }

        [Fact]
        public void SystemIntPtr_Missing_02()
        {
            var sourceA =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public class String { }
    public class Type { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public abstract class Delegate { }
    public abstract class MulticastDelegate : Delegate { }
}";
            var sourceB =
@"class Program
{
    static unsafe void Main()
    {
        System.Delegate d;
        d = (int* p) => p;
    }
}";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.UnsafeReleaseExe);
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // error CS0518: Predefined type 'System.IntPtr' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.IntPtr").WithLocation(1, 1),
                // (6,13): error CS0518: Predefined type 'System.IntPtr' is not defined or imported
                //         d = (int* p) => p;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(int* p) => p").WithArguments("System.IntPtr").WithLocation(6, 13));
        }

        [Fact]
        public void SystemDelegate_Missing()
        {
            var sourceA =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public class String { }
    public class Type { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct IntPtr { }
}";
            var sourceB =
@"class Program
{
    static void F(ref object o) { }
    static void Main()
    {
        var d1 = F;
        var d2 = (ref int i) => i;
    }
}";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // error CS0518: Predefined type 'System.MulticastDelegate' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.MulticastDelegate").WithLocation(1, 1));
        }

        [Fact]
        public void SystemMulticastDelegate_Missing()
        {
            var sourceA =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public class String { }
    public class Type { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct IntPtr { }
    public abstract class Delegate { }
}";
            var sourceB =
@"class Program
{
    static void Main()
    {
        System.Delegate d;
        d = (ref int i) => i;
    }
}";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // 1.cs(6,25): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         d = (ref int i) => i;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(6, 25));
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

            var expectedOutput = "M(Action<string> a)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
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

            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput:
@"E.M(object x, Action y)
E.M(object x, Action y)
");

            // Breaking change from C#9 which binds to E.M(object x, Action y).
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput:
@"C.M(object y)
C.M(object y)
");
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,11): error CS1660: Cannot convert anonymous method to type 'Expression' because it is not a delegate type
                //         F(delegate () { return 0; });
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "System.Linq.Expressions.Expression").WithLocation(9, 11),
                // (10,11): error CS1660: Cannot convert anonymous method to type 'Expression' because it is not a delegate type
                //         F(delegate () { return string.Empty; });
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "System.Linq.Expressions.Expression").WithLocation(10, 11));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (9,11): error CS1946: An anonymous method expression cannot be converted to an expression tree
                //         F(delegate () { return 0; });
                Diagnostic(ErrorCode.ERR_AnonymousMethodToExpressionTree, "delegate").WithLocation(9, 11),
                // (10,11): error CS1946: An anonymous method expression cannot be converted to an expression tree
                //         F(delegate () { return string.Empty; });
                Diagnostic(ErrorCode.ERR_AnonymousMethodToExpressionTree, "delegate").WithLocation(10, 11));
        }

        [WorkItem(55319, "https://github.com/dotnet/roslyn/issues/55319")]
        [Fact]
        public void OverloadResolution_08()
        {
            var source =
@"using System;
using static System.Console;
class C
{
    static void Main()
    {
        var c = new C();
        c.F(x => x);
        c.F((int x) => x);
    }
    void F(Delegate d) => Write(""instance, "");
}
static class Extensions
{
    public static void F(this C c, Func<int, int> f) => Write(""extension, "");
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "extension, extension, ");
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: "extension, instance, ");
            CompileAndVerify(source, expectedOutput: "extension, instance, ");
        }

        [WorkItem(55319, "https://github.com/dotnet/roslyn/issues/55319")]
        [Fact]
        public void OverloadResolution_09()
        {
            var source =
@"using System;
using System.Linq.Expressions;
using static System.Console;
class C
{
    static void Main()
    {
        var c = new C();
        c.F(x => x);
        c.F((int x) => x);
    }
    void F(Expression e) => Write(""instance, "");
}
static class Extensions
{
    public static void F(this C c, Expression<Func<int, int>> e) => Write(""extension, "");
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "extension, extension, ");
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: "extension, instance, ");
            CompileAndVerify(source, expectedOutput: "extension, instance, ");
        }

        [WorkItem(55319, "https://github.com/dotnet/roslyn/issues/55319")]
        [Fact]
        public void OverloadResolution_10()
        {
            var source =
@"using System;
using static System.Console;
class C
{
    static object M1(object o) => o;
    static int M1(int i) => i;
    static int M2(int i) => i;
    static void Main()
    {
        var c = new C();
        c.F(M1);
        c.F(M2);
    }
    void F(Delegate d) => Write(""instance, "");
}
static class Extensions
{
    public static void F(this C c, Func<int, int> f) => Write(""extension, "");
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "extension, extension, ");
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: "extension, instance, ");
            CompileAndVerify(source, expectedOutput: "extension, instance, ");
        }

        [Fact]
        public void OverloadResolution_11()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C
{
    static object M1(object o) => o;
    static int M1(int i) => i;
    static void Main()
    {
        F1(x => x);
        F1(M1);
        F2(x => x);
    }
    static void F1(Delegate d) { }
    static void F2(Expression e) { }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,14): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         F1(x => x);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(9, 14),
                // (10,12): error CS1503: Argument 1: cannot convert from 'method group' to 'System.Delegate'
                //         F1(M1);
                Diagnostic(ErrorCode.ERR_BadArgType, "M1").WithArguments("1", "method group", "System.Delegate").WithLocation(10, 12),
                // (11,14): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         F2(x => x);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(11, 14));

            var expectedDiagnostics10AndLater = new[]
            {
                // (9,14): error CS8917: The delegate type could not be inferred.
                //         F1(x => x);
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(9, 14),
                // (10,12): error CS1503: Argument 1: cannot convert from 'method group' to 'System.Delegate'
                //         F1(M1);
                Diagnostic(ErrorCode.ERR_BadArgType, "M1").WithArguments("1", "method group", "System.Delegate").WithLocation(10, 12),
                // (11,14): error CS8917: The delegate type could not be inferred.
                //         F2(x => x);
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(11, 14)
            };

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics10AndLater);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics10AndLater);
        }

        [WorkItem(55691, "https://github.com/dotnet/roslyn/issues/55691")]
        [Fact]
        public void OverloadResolution_12()
        {
            var source =
@"using System;
#nullable enable
var app = new WebApp();
app.Map(""/sub1"", builder =>
{
    builder.UseAuth();
});
app.Map(""/sub2"", (IAppBuilder builder) =>
{
    builder.UseAuth();
});
class WebApp : IAppBuilder, IRouteBuilder
{
    public void UseAuth() { }
}
interface IAppBuilder
{
    void UseAuth();
}
interface IRouteBuilder
{
}
static class AppBuilderExtensions
{
    public static IAppBuilder Map(this IAppBuilder app, PathString path, Action<IAppBuilder> callback)
    {
        Console.WriteLine(""AppBuilderExtensions.Map(this IAppBuilder app, PathString path, Action<IAppBuilder> callback)"");
        return app;
    }
}
static class RouteBuilderExtensions
{
    public static IRouteBuilder Map(this IRouteBuilder routes, string path, Delegate callback)
    {
        Console.WriteLine(""RouteBuilderExtensions.Map(this IRouteBuilder routes, string path, Delegate callback)"");
        return routes;
    }
}
struct PathString
{
    public PathString(string? path)
    {
        Path = path;
    }
    public string? Path { get; }
    public static implicit operator PathString(string? s) => new PathString(s);
    public static implicit operator string?(PathString path) => path.Path;
}";

            var expectedOutput =
@"AppBuilderExtensions.Map(this IAppBuilder app, PathString path, Action<IAppBuilder> callback)
AppBuilderExtensions.Map(this IAppBuilder app, PathString path, Action<IAppBuilder> callback)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(55691, "https://github.com/dotnet/roslyn/issues/55691")]
        [Fact]
        public void OverloadResolution_13()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        F(1, () => { });
        F(2, Main);
    }
    static void F(object obj, Action a) { Console.WriteLine(""F(object obj, Action a)""); }
    static void F(int i, Delegate d) { Console.WriteLine(""F(int i, Delegate d)""); }
}";

            var expectedOutput =
@"F(object obj, Action a)
F(object obj, Action a)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(55691, "https://github.com/dotnet/roslyn/issues/55691")]
        [Fact]
        public void OverloadResolution_14()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        F(() => 1, 2);
    }
    static void F(Expression<Func<object>> f, object obj) { Console.WriteLine(""F(Expression<Func<object>> f, object obj)""); }
    static void F(Expression e, int i) { Console.WriteLine(""F(Expression e, int i)""); }
}";

            var expectedOutput = @"F(Expression<Func<object>> f, object obj)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(4674, "https://github.com/dotnet/csharplang/issues/4674")]
        [Fact]
        public void OverloadResolution_15()
        {
            var source =
@"using System;
delegate void StringAction(string arg);
class Program
{
    static void F<T>(T t) { Console.WriteLine(typeof(T).Name); }
    static void F(StringAction a) { Console.WriteLine(""StringAction""); }
    static void M(string arg) { }
    static void Main()
    {
        F((string s) => { }); // C#9: F(StringAction)
        F(M); // C#9: F(StringAction)
    }
}";

            var expectedOutput =
@"StringAction
StringAction
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(56623, "https://github.com/dotnet/roslyn/issues/56623")]
        [Fact]
        public void OverloadResolution_16()
        {
            var source =
@"using System;
class Program
{
    static void F(Func<Func<object>> f, int i) => Report(f);
    static void F(Func<Func<int>> f, object o) => Report(f);
    static void Main()
    {
        M(false);
    }
    static void M(bool b)
    {
        F(() => () => 1, 2);
        F(() => () => { if (b) return 0; return 1; }, 2);
        F(() => { if (b) return () => 0; return () => 1; }, 2);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput:
@"System.Func`1[System.Func`1[System.Object]]
System.Func`1[System.Func`1[System.Object]]
System.Func`1[System.Func`1[System.Object]]
");

            // Breaking change from C#9 which binds calls to F(Func<Func<object>>, int).
            //
            // The calls such as F(() => () => 1, 2) should be considered ambiguous in C#9 as per the C# spec.
            // But for compatibility with the legacy compiler, the implementation of "better conversion
            // from expression" ignores delegate types for certain cases when the corresponding
            // argument is a lambda expression, such as when the inferred return type of the lambda
            // expression is null (see OverloadResolution.CanDowngradeConversionFromLambdaToNeither()).
            // With the code example above, in C#10, the inferred return type of lambdas such as () => () => 1
            // is Func<int> rather than null so the compatibility exception no longer applies for this example.
            //
            // We've decided to take the breaking change and match the C# spec rather than the
            // legacy compiler in this particular case.
            var expectedDiagnostics = new[]
            {
                // (12,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F(Func<Func<object>>, int)' and 'Program.F(Func<Func<int>>, object)'
                //         F(() => () => 1, 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F(System.Func<System.Func<object>>, int)", "Program.F(System.Func<System.Func<int>>, object)").WithLocation(12, 9),
                // (13,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F(Func<Func<object>>, int)' and 'Program.F(Func<Func<int>>, object)'
                //         F(() => () => { if (b) return 0; return 1; }, 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F(System.Func<System.Func<object>>, int)", "Program.F(System.Func<System.Func<int>>, object)").WithLocation(13, 9),
                // (14,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F(Func<Func<object>>, int)' and 'Program.F(Func<Func<int>>, object)'
                //         F(() => { if (b) return () => 0; return () => 1; }, 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F(System.Func<System.Func<object>>, int)", "Program.F(System.Func<System.Func<int>>, object)").WithLocation(14, 9)
            };
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OverloadResolution_17()
        {
            var source =
@"delegate void StringAction(string arg);
class Program
{
    static void F<T>(System.Action<T> a) { }
    static void F(StringAction a) { }
    static void Main()
    {
        F((string s) => { });
    }
}";

            var expectedDiagnostics = new[]
            {
                // (8,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F<T>(Action<T>)' and 'Program.F(StringAction)'
                //         F((string s) => { });
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F<T>(System.Action<T>)", "Program.F(StringAction)").WithLocation(8, 9)
            };
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OverloadResolution_18()
        {
            var source =
@"delegate void StringAction(string arg);
class Program
{
    static void F0<T>(System.Action<T> a) { }
    static void F1<T>(System.Action<T> a) { }
    static void F1(StringAction a) { }
    static void M(string arg) { }
    static void Main()
    {
        F0(M);
        F1(M);
    }
}";

            var expectedDiagnostics = new[]
            {
                // (10,9): error CS0411: The type arguments for method 'Program.F0<T>(Action<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F0(M);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F0").WithArguments("Program.F0<T>(System.Action<T>)").WithLocation(10, 9)
            };
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OverloadResolution_19()
        {
            var source =
@"delegate void MyAction<T>(T arg);
class Program
{
    static void F<T>(System.Action<T> a) { }
    static void F<T>(MyAction<T> a) { }
    static void M(string arg) { }
    static void Main()
    {
        F((string s) => { });
        F(M);
    }
}";

            var expectedDiagnostics = new[]
            {
                // (9,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F<T>(Action<T>)' and 'Program.F<T>(MyAction<T>)'
                //         F((string s) => { });
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F<T>(System.Action<T>)", "Program.F<T>(MyAction<T>)").WithLocation(9, 9),
                // (10,9): error CS0411: The type arguments for method 'Program.F<T>(Action<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(M);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(System.Action<T>)").WithLocation(10, 9)
            };
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OverloadResolution_20()
        {
            var source =
@"using System;
delegate void StringAction(string s);
class Program
{
    static void F(Action<string> a) { }
    static void F(StringAction a) { }
    static void M(string s) { }
    static void Main()
    {
        F(M);
        F((string s) => { });
    }
}";

            var expectedDiagnostics = new[]
            {
                // (10,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F(Action<string>)' and 'Program.F(StringAction)'
                //         F(M);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F(System.Action<string>)", "Program.F(StringAction)").WithLocation(10, 9),
                // (11,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F(Action<string>)' and 'Program.F(StringAction)'
                //         F((string s) => { });
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F(System.Action<string>)", "Program.F(StringAction)").WithLocation(11, 9)
            };
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OverloadResolution_21()
        {
            var source =
@"using System;
class C<T>
{
    public void F(Delegate d) => Report(""F(Delegate d)"", d);
    public void F(T t) => Report(""F(T t)"", t);
    public void F(Func<T> f) => Report(""F(Func<T> f)"", f);
    static void Report(string method, object arg) => Console.WriteLine(""{0}, {1}"", method, arg.GetType());
}
class Program
{
    static void Main()
    {
        var c = new C<Delegate>();
        c.F(() => (Action)null);
    }
}";

            string expectedOutput = "F(Func<T> f), System.Func`1[System.Delegate]";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(1361172, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1361172")]
        [Fact]
        public void OverloadResolution_22()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C<T>
{
    public void F(Delegate d) => Report(""F(Delegate d)"", d);
    public void F(T t) => Report(""F(T t)"", t);
    public void F(Func<T> f) => Report(""F(Func<T> f)"", f);
    static void Report(string method, object arg) => Console.WriteLine(""{0}, {1}"", method, arg.GetType());
}
class Program
{
    static void Main()
    {
        var c = new C<Expression>();
        c.F(() => Expression.Constant(1));
    }
}";

            string expectedOutput = "F(Func<T> f), System.Func`1[System.Linq.Expressions.Expression]";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_23()
        {
            var source =
@"using System;
class Program
{
    static void F(Delegate d) => Console.WriteLine(""F(Delegate d)"");
    static void F(Func<object> f) => Console.WriteLine(""F(Func<int> f)"");
    static void Main()
    {
        F(() => 1);
    }
}";

            string expectedOutput = "F(Func<int> f)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(1361172, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1361172")]
        [Fact]
        public void OverloadResolution_24()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void F(Expression e) => Console.WriteLine(""F(Expression e)"");
    static void F(Func<Expression> f) => Console.WriteLine(""F(Func<Expression> f)"");
    static void Main()
    {
        F(() => Expression.Constant(1));
    }
}";

            string expectedOutput = "F(Func<Expression> f)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(56167, "https://github.com/dotnet/roslyn/issues/56167")]
        [Fact]
        public void OverloadResolution_25()
        {
            var source =
@"using static System.Console;
delegate void D();
class Program
{
    static void F(D d) => WriteLine(""D"");
    static void F<T>(T t) => WriteLine(typeof(T).Name);
    static void Main()
    {
        F(() => { });
    }
}";

            string expectedOutput = "D";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(56167, "https://github.com/dotnet/roslyn/issues/56167")]
        [Fact]
        public void OverloadResolution_26()
        {
            var source =
@"using System;
class Program
{
    static void F(Action action) => Console.WriteLine(""Action"");
    static void F<T>(T t) => Console.WriteLine(typeof(T).Name);
    static void Main()
    {
        int i = 0;
        F(() => i++);
    }
}";

            string expectedOutput = "Action";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(56167, "https://github.com/dotnet/roslyn/issues/56167")]
        [Fact]
        public void OverloadResolution_27()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void F(Action action) => Console.WriteLine(""Action"");
    static void F(Expression expression) => Console.WriteLine(""Expression"");
    static int GetValue() => 0;
    static void Main()
    {
        F(() => GetValue());
    }
}";

            string expectedOutput = "Action";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(56319, "https://github.com/dotnet/roslyn/issues/56319")]
        [Fact]
        public void OverloadResolution_28()
        {
            var source =
@"using System;

var source = new C<int>();
source.Aggregate(() => 0, (i, j) => i, (i, j) => i, i => i);

class C<T> { }

static class Extensions
{
    public static TResult Aggregate<TSource, TAccumulate, TResult>(
        this C<TSource> source,
        Func<TAccumulate> seedFactory,
        Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc,
        Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc,
        Func<TAccumulate, TResult> resultSelector)
    {
        Console.WriteLine((typeof(TSource).FullName, typeof(TAccumulate).FullName, typeof(TResult).FullName));
        return default;
    }

    public static TResult Aggregate<TSource, TAccumulate, TResult>(
        this C<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc,
        Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc,
        Func<TAccumulate, TResult> resultSelector)
    {
        Console.WriteLine((typeof(TSource).FullName, typeof(TAccumulate).FullName, typeof(TResult).FullName));
        return default;
    }
}";

            string expectedOutput = "(System.Int32, System.Int32, System.Int32)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_29()
        {
            var source =
@"using System;
class A { }
class B : A { }
class Program
{
    static void M<T>(T x, T y) { Console.WriteLine(""M<T>(T x, T y)""); }
    static void M(Func<object> x, Func<object> y) { Console.WriteLine(""M(Func<object> x, Func<object> y)""); }
    static void Main()
    {
        Func<object> fo = () => new A();
        Func<A> fa = () => new A();
        M(() => new A(), () => new B());
        M(fo, () => new B());
        M(fa, () => new B());
    }
}";

            var expectedOutput =
@"M(Func<object> x, Func<object> y)
M(Func<object> x, Func<object> y)
M<T>(T x, T y)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_30()
        {
            var source =
@"using System;
class Program
{
    static void M<T>(T t, Func<object> f) { Console.WriteLine(""M<T>(T t, Func<object> f)""); }
    static void M<T>(Func<object> f, T t) { Console.WriteLine(""M<T>(Func<object> f, T t)""); }
    static object F() => null;
    static void Main()
    {
        M(F, F);
        M(() => 1, () => 2);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,9): error CS0411: The type arguments for method 'Program.M<T>(T, Func<object>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(F, F);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T>(T, System.Func<object>)").WithLocation(9, 9),
                // (10,9): error CS0411: The type arguments for method 'Program.M<T>(T, Func<object>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(() => 1, () => 2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T>(T, System.Func<object>)").WithLocation(10, 9));

            var expectedDiagnostics = new[]
            {
                // (9,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M<T>(T, Func<object>)' and 'Program.M<T>(Func<object>, T)'
                //         M(F, F);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M<T>(T, System.Func<object>)", "Program.M<T>(System.Func<object>, T)").WithLocation(9, 9),
                // (10,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M<T>(T, Func<object>)' and 'Program.M<T>(Func<object>, T)'
                //         M(() => 1, () => 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M<T>(T, System.Func<object>)", "Program.M<T>(System.Func<object>, T)").WithLocation(10, 9)
            };
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OverloadResolution_31()
        {
            var source =
@"using System;
 using System.Linq.Expressions;
class Program
{
    static void M<T>(T t) { Console.WriteLine(""M<T>(T t)""); }
    static void M(Expression<Func<object>> e) { Console.WriteLine(""M(Expression<Func<object>> e)""); }
    static void Main()
    {
        M(() => string.Empty);
    }
}";

            var expectedOutput = "M(Expression<Func<object>> e)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_32()
        {
            var source =
@"using System;
 using System.Linq.Expressions;
class A { }
class B : A { }
class Program
{
    static void M<T>(T x, T y) { Console.WriteLine(""M<T>(T x, T y)""); }
    static void M(Expression<Func<object>> x, Expression<Func<object>> y) { Console.WriteLine(""M(Expression<Func<object>> x, Expression<Func<object>> y)""); }
    static void Main()
    {
        Expression<Func<object>> fo = () => new A();
        Expression<Func<A>> fa = () => new A();
        M(() => new A(), () => new B());
        M(fo, () => new B());
        M(fa, () => new B());
    }
}";

            var expectedOutput =
@"M(Expression<Func<object>> x, Expression<Func<object>> y)
M(Expression<Func<object>> x, Expression<Func<object>> y)
M<T>(T x, T y)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_33()
        {
            var source =
@"using System;
class Program
{
    static void M<T>(object x, T y) { Console.WriteLine(""M<T>(object x, T y)""); }
    static void M<T, U>(T x, U y) { Console.WriteLine(""M<T, U>(T x, U y)""); }
    static void Main()
    {
        Func<int> f = () => 0;
        M(() => 1, () => 2);
        M(() => 1, f);
        M(f, () => 2);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,9): error CS0411: The type arguments for method 'Program.M<T>(object, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(() => 1, () => 2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T>(object, T)").WithLocation(9, 9),
                // (10,14): error CS1660: Cannot convert lambda expression to type 'object' because it is not a delegate type
                //         M(() => 1, f);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "object").WithLocation(10, 14),
                // (11,9): error CS0411: The type arguments for method 'Program.M<T>(object, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(f, () => 2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T>(object, T)").WithLocation(11, 9));

            var expectedOutput =
@"M<T, U>(T x, U y)
M<T, U>(T x, U y)
M<T, U>(T x, U y)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_34()
        {
            var source =
@"using System;
class Program
{
    static void M<T, U>(Func<T> x, U y) { Console.WriteLine(""M<T, U>(Func<T> x, U y)""); }
    static void M<T, U>(T x, U y) { Console.WriteLine(""M<T, U>(T x, U y)""); }
    static void Main()
    {
        Func<int> f = () => 0;
        M(() => 1, () => 2);
        M(() => 1, f);
        M(f, () => 2);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,9): error CS0411: The type arguments for method 'Program.M<T, U>(Func<T>, U)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(() => 1, () => 2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T, U>(System.Func<T>, U)").WithLocation(9, 9),
                // (11,9): error CS0411: The type arguments for method 'Program.M<T, U>(Func<T>, U)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(f, () => 2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T, U>(System.Func<T>, U)").WithLocation(11, 9));

            var expectedOutput =
@"M<T, U>(Func<T> x, U y)
M<T, U>(Func<T> x, U y)
M<T, U>(Func<T> x, U y)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_35()
        {
            var source =
@"using System;
class Program
{
    static void M(Delegate x, Func<int> y) { Console.WriteLine(""M(Delegate x, Func<int> y)""); }
    static void M<T, U>(T x, U y) { Console.WriteLine(""M<T, U>(T x, U y)""); }
    static void Main()
    {
        Func<int> f = () => 0;
        M(() => 1, () => 2);
        M(() => 1, f);
        M(f, () => 2);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,14): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         M(() => 1, () => 2);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(9, 14),
                // (10,14): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         M(() => 1, f);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(10, 14));

            var expectedOutput =
@"M<T, U>(T x, U y)
M<T, U>(T x, U y)
M(Delegate x, Func<int> y)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_36()
        {
            var source =
@"using System;
class Program
{
    static void F<T>(T t) { Console.WriteLine(""F<{0}>({0} t)"", typeof(T).Name); }
    static void F(Delegate d) { Console.WriteLine(""F(Delegate d)""); }
    static void Main()
    {
        F(Main);
        F(() => 1);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,11): error CS1503: Argument 1: cannot convert from 'method group' to 'System.Delegate'
                //         F(Main);
                Diagnostic(ErrorCode.ERR_BadArgType, "Main").WithArguments("1", "method group", "System.Delegate").WithLocation(8, 11),
                // (9,14): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         F(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(9, 14));

            var expectedOutput =
@"F<Action>(Action t)
F<Func`1>(Func`1 t)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_37()
        {
            var source =
@"using System;
class Program
{
    static void F(object o) { Console.WriteLine(""F(object o)""); }
    static void F(Delegate d) { Console.WriteLine(""F(Delegate d)""); }
    static void Main()
    {
        F(Main);
        F(() => 1);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,11): error CS1503: Argument 1: cannot convert from 'method group' to 'object'
                //         F(Main);
                Diagnostic(ErrorCode.ERR_BadArgType, "Main").WithArguments("1", "method group", "object").WithLocation(8, 11),
                // (9,14): error CS1660: Cannot convert lambda expression to type 'object' because it is not a delegate type
                //         F(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "object").WithLocation(9, 14));

            var expectedOutput =
@"F(Delegate d)
F(Delegate d)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_38()
        {
            var source =
@"using System;
class MyString
{
    public static implicit operator MyString(string s) => new MyString();
}
class Program
{
    static void F(Delegate d1, Delegate d2, string s) { Console.WriteLine(""F(Delegate d1, Delegate d2, string s)""); }
    static void F(Func<int> f, Delegate d, MyString s) { Console.WriteLine(""F(Func<int> f, Delegate d, MyString s)""); }
    static void Main()
    {
        F(() => 1, () => 2, string.Empty);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (12,14): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         F(() => 1, () => 2, string.Empty);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(12, 14),
                // (12,23): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         F(() => 1, () => 2, string.Empty);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(12, 23));

            var expectedDiagnostics = new[]
            {
                // (12,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F(Delegate, Delegate, string)' and 'Program.F(Func<int>, Delegate, MyString)'
                //         F(() => 1, () => 2, string.Empty);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F(System.Delegate, System.Delegate, string)", "Program.F(System.Func<int>, System.Delegate, MyString)").WithLocation(12, 9)
            };
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OverloadResolution_39()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C
{
    static void M(Expression e) { Console.WriteLine(""M(Expression e)""); }
    static void M(object o) { Console.WriteLine(""M(object o)""); }
    static int F() => 0;
    static void Main()
    {
        M(F);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (10,11): error CS1503: Argument 1: cannot convert from 'method group' to 'Expression'
                //         M(F);
                Diagnostic(ErrorCode.ERR_BadArgType, "F").WithArguments("1", "method group", "System.Linq.Expressions.Expression").WithLocation(10, 11));

            var expectedDiagnostics = new[]
            {
                // (10,11): error CS0428: Cannot convert method group 'F' to non-delegate type 'Expression'. Did you intend to invoke the method?
                //         M(F);
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "System.Linq.Expressions.Expression").WithLocation(10, 11)
            };
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OverloadResolution_40()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C
{
    static void M(Expression e) { Console.WriteLine(""M(Expression e)""); }
    static void M(object o) { Console.WriteLine(""M(object o)""); }
    static void Main()
    {
        M(() => 1);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,14): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         M(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(9, 14));

            var expectedOutput = @"M(Expression e)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_41()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C
{
    static void M(Expression e) { Console.WriteLine(""M(Expression e)""); }
    static void M(Delegate d) { Console.WriteLine(""M(Delegate d)""); }
    static int F() => 0;
    static void Main()
    {
        M(F);
        M(() => 1);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (10,11): error CS1503: Argument 1: cannot convert from 'method group' to 'System.Linq.Expressions.Expression'
                //         M(F);
                Diagnostic(ErrorCode.ERR_BadArgType, "F").WithArguments("1", "method group", "System.Linq.Expressions.Expression").WithLocation(10, 11),
                // (11,14): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         M(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(11, 14));

            var expectedDiagnostics = new[]
            {
                // (10,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(Expression)' and 'C.M(Delegate)'
                //         M(F);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(System.Linq.Expressions.Expression)", "C.M(System.Delegate)").WithLocation(10, 9),
                // (11,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(Expression)' and 'C.M(Delegate)'
                //         M(() => 1);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(System.Linq.Expressions.Expression)", "C.M(System.Delegate)").WithLocation(11, 9)
            };
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OverloadResolution_42()
        {
            var source =
@"using System;
using System.Runtime.InteropServices;
[ComImport]
[Guid(""96A2DE64-6D44-4DA5-BBA4-25F5F07E0E6B"")]
interface I
{
    void F(Delegate d, short s);
    void F(Action a, ref int i);
}
class C : I
{
    void I.F(Delegate d, short s) => Console.WriteLine(""I.F(Delegate d, short s)"");
    void I.F(Action a, ref int i) => Console.WriteLine(""I.F(Action a, ref int i)"");
}
class Program
{
    static void M(I i)
    {
        i.F(() => { }, 1);
    }
    static void Main()
    {
        M(new C());
    }
}";

            var expectedOutput = @"I.F(Action a, ref int i)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(4674, "https://github.com/dotnet/csharplang/issues/4674")]
        [Fact]
        public void OverloadResolution_43()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static int F() => 0;
    static void Main()
    {
        var c = new C();
        c.M(F);
        c.M(delegate () { return 1; });
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

            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput:
@"E.M
E.M");

            var expectedDiagnostics = new[]
            {
                // (9,13): error CS0428: Cannot convert method group 'F' to non-delegate type 'Expression'. Did you intend to invoke the method?
                //         c.M(F);
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "System.Linq.Expressions.Expression").WithLocation(9, 13),
                // (10,13): error CS1946: An anonymous method expression cannot be converted to an expression tree
                //         c.M(delegate () { return 1; });
                Diagnostic(ErrorCode.ERR_AnonymousMethodToExpressionTree, "delegate").WithLocation(10, 13)
            };

            // Breaking change from C#9 which binds to E.M in each case.
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OverloadResolution_44()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class A
{
    public static void F1(Func<int> f) { Console.WriteLine(""A.F1(Func<int> f)""); }
    public void F2(Func<int> f) { Console.WriteLine(""A.F2(Func<int> f)""); }
}
class B : A
{
    public static void F1(Delegate d) { Console.WriteLine(""B.F1(Delegate d)""); }
    public void F2(Expression e) { Console.WriteLine(""B.F2(Expression e)""); }
}
class Program
{
    static void Main()
    {
        B.F1(() => 1);
        var b = new B();
        b.F2(() => 2);
    }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput:
@"A.F1(Func<int> f)
A.F2(Func<int> f)");

            // Breaking change from C#9 which binds to methods from A.
            var expectedOutput =
@"B.F1(Delegate d)
B.F2(Expression e)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_45()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class A
{
    public object this[Func<int> f] => Report(""A.this[Func<int> f]"");
    public static object Report(string message) { Console.WriteLine(message); return null; }
}
class B1 : A
{
    public object this[Delegate d] => Report(""B1.this[Delegate d]"");
}
class B2 : A
{
    public object this[Expression e] => Report(""B2.this[Expression e]"");
}
class Program
{
    static void Main()
    {
        var b1 = new B1();
        _ = b1[() => 1];
        var b2 = new B2();
        _ = b2[() => 2];
    }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput:
@"A.this[Func<int> f]
A.this[Func<int> f]");

            // Breaking change from C#9 which binds to methods from A.
            var expectedOutput =
@"B1.this[Delegate d]
B2.this[Expression e]";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_46()
        {
            var source =
@"using System;
class Program
{
    static void F(Func<Func<object>> f, int i) => Report(f);
    static void F(Func<Func<int>> f, object o) => Report(f);
    static void Main()
    {
        M(false);
    }
    static void M(bool b)
    {
        F(() => b ? () => 0 : () => 1, 2);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var expectedDiagnostics = new[]
            {
                // (12,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F(Func<Func<object>>, int)' and 'Program.F(Func<Func<int>>, object)'
                //         F(() => b ? () => 0 : () => 1, 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F(System.Func<System.Func<object>>, int)", "Program.F(System.Func<System.Func<int>>, object)").WithLocation(12, 9)
            };
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OverloadResolution_47()
        {
            var source =
@"using System;
class Program
{
    static void F(int i, Func<Func<object>> f) => Report(f);
    static void F(object o, Func<Func<int>> f) => Report(f);
    static void Main()
    {
        F(2, () => new[] { () => 0, () => 1 }[0]);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,20): error CS0826: No best type found for implicitly-typed array
                //         F(2, () => new[] { () => 0, () => 1 }[0]);
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { () => 0, () => 1 }").WithLocation(8, 20));

            var expectedOutput = @"System.Func`1[System.Func`1[System.Int32]]";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(57627, "https://github.com/dotnet/roslyn/issues/57627")]
        [Fact]
        public void OverloadResolution_48()
        {
            var source =
@"using System;
using System.Threading.Tasks;

delegate void MyAction();
delegate T MyFunc<T>();

class A
{
    public static void F(object o) { Console.WriteLine(""F(object o)""); }
    public static void F(object o, string format, params object[] args) { Console.WriteLine(""F(object o, string format, params object[] args)""); }
    
    public static void F<T>(T t) { Console.WriteLine(""F<T>(T t)""); }
    public static void F<T>(T t, string format, params object[] args) { Console.WriteLine(""F<T>(T t, string format, params object[] args)""); }
    
    public static void F(MyAction a) { Console.WriteLine(""F(MyAction a)""); }
    public static void F(MyAction a, string format, params object[] args) { Console.WriteLine(""F(MyAction a, string format, params object[] args)""); }
    
    public static void F<T>(MyFunc<T> f) { Console.WriteLine(""F<T>(MyFunc<T> f)""); }
    public static void F<T>(MyFunc<T> f, string format, params object[] args) { Console.WriteLine(""F<T>(MyFunc<T> f, string format, params object[] args)""); }
}

class B
{
    static async Task Main()
    {
        A.F(() => { });
        A.F(() => { }, """");
        A.F(() => { }, ""{0}"", 1);
        A.F(async () => await Task.FromResult<object>(null));
        A.F(async () => await Task.FromResult<object>(null), """");
        A.F(async () => await Task.FromResult<object>(null), ""{0}"", 1);
    }
}";

            string expectedOutput =
@"F(MyAction a)
F(MyAction a, string format, params object[] args)
F(MyAction a, string format, params object[] args)
F<T>(MyFunc<T> f)
F<T>(MyFunc<T> f, string format, params object[] args)
F<T>(MyFunc<T> f, string format, params object[] args)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OverloadResolution_49()
        {
            var source = """
class Program
{
    delegate void D1(int i = 1);
    delegate void D2(int i = 2);
    static int F(D1 d) => 1;
    static object F(D2 d) => 2;
    static void Main()
    {
        int y = F((int x = 2) => { });
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F(Program.D1)' and 'Program.F(Program.D2)'
                //         int y = F((int x = 2) => { });
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F(Program.D1)", "Program.F(Program.D2)").WithLocation(9, 17));
        }

        [Fact]
        public void OverloadResolution_50()
        {

            var source = """
class Program
{
    delegate void D1(int i = 1);
    static int F(D1 d) => 1;
    static void Main()
    {
        int y = F((int x = 2) => { });
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,24): warning CS9099: Parameter 1 has default value '2' in lambda but '1' in the target delegate type.
                //         int y = F((int x = 2) => { });
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "2", "1").WithLocation(7, 24));
        }

        [Fact]
        public void OverloadResolution_51()
        {
            var source = """
class Program
{
    delegate void D1(int i = 1);
    delegate void D2(int i = 2);
    static int F(D1 d) => 1;
    static object F(D2 d) => 2;
    static void M(int i = 2) { }

    static void Main()
    {
        int y = F(M);
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F(Program.D1)' and 'Program.F(Program.D2)'
                //         int y = F(M);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("Program.F(Program.D1)", "Program.F(Program.D2)").WithLocation(11, 17));
        }

        [Fact]
        public void OverloadResolution_52()
        {
            var source = """
using System;

class Program
{
    delegate void D1(int i = 1);
    delegate int D2(int j = 1);

    static void F(D1 d) { }
    static int F(D2 d) => d();
    static int M(int i = 2) => i;

    static void Main()
    {
        int y = F(M);
        Console.WriteLine(y);
    }
}
""";
            CompileAndVerify(source, expectedOutput: "1").VerifyDiagnostics();
        }

        [Fact]
        public void BestCommonType_01()
        {
            var source =
@"using System;
delegate int StringIntDelegate(string s);
class Program
{
    static int M(string s) => s.Length;
    static void Main()
    {
        StringIntDelegate d = M;
        var a1 = new[] { d, (string s) => int.Parse(s) };
        var a2 = new[] { (string s) => int.Parse(s), d };
        Report(a1[1]);
        Report(a2[0]);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            string expectedOutput =
@"StringIntDelegate
StringIntDelegate";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
        }

        [Fact]
        public void BestCommonType_02()
        {
            var source =
@"using System;
delegate int StringIntDelegate(string s);
class Program
{
    static int M(string s) => s.Length;
    static void F(bool  b)
    {
        StringIntDelegate d = M;
        var c1 = b ? d : ((string s) => int.Parse(s));
        var c2 = b ? ((string s) => int.Parse(s)) : d;
        Report(c1);
        Report(c2);
    }
    static void Main()
    {
        F(false);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            string expectedOutput =
@"StringIntDelegate
StringIntDelegate";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
        }

        [Fact]
        public void BestCommonType_03()
        {
            var source =
@"using System;
delegate int StringIntDelegate(string s);
class Program
{
    static int M(string s) => s.Length;
    static void Main()
    {
        var f1 = (bool b) => { if (b) return (StringIntDelegate)M; return ((string s) => int.Parse(s)); };
        var f2 = (bool b) => { if (b) return ((string s) => int.Parse(s)); return (StringIntDelegate)M; };
        Report(f1(true));
        Report(f2(true));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"StringIntDelegate
StringIntDelegate");
        }

        [Fact]
        public void BestCommonType_04()
        {
            var source =
@"using System;
delegate int StringIntDelegate(string s);
class Program
{
    static int M(string s) => s.Length;
    static void Main()
    {
        var f1 = (bool b) => { if (b) return M; return ((string s) => int.Parse(s)); };
        var f2 = (bool b) => { if (b) return ((string s) => int.Parse(s)); return M; };
        Report(f1(true));
        Report(f2(true));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Func`2[System.String,System.Int32]
System.Func`2[System.String,System.Int32]");
        }

        [Fact]
        public void BestCommonType_05()
        {
            var source =
@"using System;
class Program
{
    static int M1(string s) => s.Length;
    static int M2(string s) => int.Parse(s);
    static void Main()
    {
        var a1 = new[] { M1, (string s) => int.Parse(s) };
        var a2 = new[] { (string s) => s.Length, M2 };
        Report(a1[1]);
        Report(a2[1]);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { M1, (string s) => int.Parse(s) };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { M1, (string s) => int.Parse(s) }").WithLocation(8, 18),
                // (9,18): error CS0826: No best type found for implicitly-typed array
                //         var a2 = new[] { (string s) => s.Length, M2 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { (string s) => s.Length, M2 }").WithLocation(9, 18));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Func`2[System.String,System.Int32]
System.Func`2[System.String,System.Int32]");
        }

        [Fact]
        public void BestCommonType_06()
        {
            var source =
@"using System;
class Program
{
    static void F1<T>(T t) { }
    static T F2<T>() => default;
    static void Main()
    {
        var a1 = new[] { F1<object>, F1<string> };
        var a2 = new[] { F2<object>, F2<string> };
        Report(a1[0]);
        Report(a2[0]);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { F1<object>, F1<string> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F1<object>, F1<string> }").WithLocation(8, 18),
                // (9,18): error CS0826: No best type found for implicitly-typed array
                //         var a2 = new[] { F2<object>, F2<string> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F2<object>, F2<string> }").WithLocation(9, 18));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Action`1[System.String]
System.Func`1[System.Object]");
        }

        [Fact]
        public void BestCommonType_07()
        {
            var source =
@"class Program
{
    static void F1<T>(T t) { }
    static T F2<T>() => default;
    static T F3<T>(T t) => t;
    static void Main()
    {
        var a1 = new[] { F1<int>, F1<object> };
        var a2 = new[] { F2<nint>, F2<System.IntPtr> };
        var a3 = new[] { F3<string>, F3<object> };
    }
}";

            var expectedDiagnostics = new[]
            {
                // (8,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { F1<int>, F1<object> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F1<int>, F1<object> }").WithLocation(8, 18),
                // (9,18): error CS0826: No best type found for implicitly-typed array
                //         var a2 = new[] { F2<nint>, F2<System.IntPtr> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F2<nint>, F2<System.IntPtr> }").WithLocation(9, 18),
                // (10,18): error CS0826: No best type found for implicitly-typed array
                //         var a3 = new[] { F3<string>, F3<object> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F3<string>, F3<object> }").WithLocation(10, 18)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void BestCommonType_08()
        {
            var source =
@"#nullable enable
using System;
class Program
{
    static void F<T>(T t) { }
    static void Main()
    {
        var a1 = new[] { F<string?>, F<string> };
        var a2 = new[] { F<(int X, object Y)>, F<(int, dynamic)> };
        Report(a1[0]);
        Report(a2[0]);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { F<string?>, F<string> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F<string?>, F<string> }").WithLocation(8, 18),
                // (9,18): error CS0826: No best type found for implicitly-typed array
                //         var a2 = new[] { F<(int X, object Y)>, F<(int, dynamic)> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F<(int X, object Y)>, F<(int, dynamic)> }").WithLocation(9, 18));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Action`1[System.String]
System.Action`1[System.ValueTuple`2[System.Int32,System.Object]]");
        }

        [Fact]
        public void BestCommonType_09()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var a1 = new[] { (object o) => { }, (string s) => { } };
        var a2 = new[] { () => (object)null, () => (string)null };
        Report(a1[0]);
        Report(a2[0]);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { (object o) => { }, (string s) => { } };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { (object o) => { }, (string s) => { } }").WithLocation(6, 18),
                // (7,18): error CS0826: No best type found for implicitly-typed array
                //         var a2 = new[] { () => (object)null, () => (string)null };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { () => (object)null, () => (string)null }").WithLocation(7, 18));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,34): error CS1678: Parameter 1 is declared as type 'object' but should be 'string'
                //         var a1 = new[] { (object o) => { }, (string s) => { } };
                Diagnostic(ErrorCode.ERR_BadParamType, "o").WithArguments("1", "", "object", "", "string").WithLocation(6, 34),
                // (6,37): error CS1661: Cannot convert lambda expression to type 'Action<string>' because the parameter types do not match the delegate parameter types
                //         var a1 = new[] { (object o) => { }, (string s) => { } };
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "System.Action<string>").WithLocation(6, 37));
        }

        [Fact]
        public void BestCommonType_10()
        {
            var source =
@"using System;
class Program
{
    static void F1<T>(T t, ref object o) { }
    static void F2<T, U>(ref T t, U u) { }
    static void Main()
    {
        var a1 = new[] { F1<string>, F1<string> };
        var a2 = new[] { F2<object, string>, F2<object, string> };
        Report(a1[0]);
        Report(a2[0]);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { F1<string>, F1<string> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F1<string>, F1<string> }").WithLocation(8, 18),
                // (9,18): error CS0826: No best type found for implicitly-typed array
                //         var a2 = new[] { F2<object, string>, F2<object, string> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F2<object, string>, F2<object, string> }").WithLocation(9, 18));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"<>A{00000008}`2[System.String,System.Object]
<>A{00000001}`2[System.Object,System.String]");
        }

        [Fact]
        [WorkItem(55909, "https://github.com/dotnet/roslyn/issues/55909")]
        public void BestCommonType_11()
        {
            var source =
@"using System;
class Program
{
    static void F1<T>(T t, ref object o) { }
    static void F2<T, U>(ref T t, U u) { }
    static void Main()
    {
        var a1 = new[] { F1<object>, F1<string> };
        var a2 = new[] { F2<object, string>, F2<object, object> };
        Report(a1[0]);
        Report(a2[0]);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var expectedDiagnostics = new[]
            {
                // (8,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { F1<object>, F1<string> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F1<object>, F1<string> }").WithLocation(8, 18),
                // (9,18): error CS0826: No best type found for implicitly-typed array
                //         var a2 = new[] { F2<object, string>, F2<object, object> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F2<object, string>, F2<object, object> }").WithLocation(9, 18)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            // https://github.com/dotnet/roslyn/issues/55909: ConversionsBase.HasImplicitSignatureConversion()
            // relies on the variance of FunctionTypeSymbol.GetInternalDelegateType() which fails for synthesized
            // delegate types where the type parameters are invariant.
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void BestCommonType_12()
        {
            var source =
@"class Program
{
    static void F<T>(ref T t) { }
    static void Main()
    {
        var a1 = new[] { F<object>, F<string> };
        var a2 = new[] { (object x, ref object y) => { }, (string x, ref object y) => { } };
        var a3 = new[] { (object x, ref object y) => { }, (object x, ref string y) => { } };
    }
}";

            var expectedDiagnostics = new[]
            {
                // (6,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { F<object>, F<string> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F<object>, F<string> }").WithLocation(6, 18),
                // (7,18): error CS0826: No best type found for implicitly-typed array
                //         var a2 = new[] { (object x, ref object y) => { }, (string x, ref object y) => { } };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { (object x, ref object y) => { }, (string x, ref object y) => { } }").WithLocation(7, 18),
                // (8,18): error CS0826: No best type found for implicitly-typed array
                //         var a3 = new[] { (object x, ref object y) => { }, (object x, ref string y) => { } };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { (object x, ref object y) => { }, (object x, ref string y) => { } }").WithLocation(8, 18)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void BestCommonType_13()
        {
            var source =
@"using System;
class Program
{
    static void F<T>(ref T t) { }
    static void Main()
    {
        var a1 = new[] { F<object>, null };
        var a2 = new[] { default, F<string> };
        var a3 = new[] { null, default, (object x, ref string y) => { } };
        Report(a1[0]);
        Report(a2[1]);
        Report(a3[2]);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { F<object>, null };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F<object>, null }").WithLocation(7, 18),
                // (8,18): error CS0826: No best type found for implicitly-typed array
                //         var a2 = new[] { default, F<string> };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { default, F<string> }").WithLocation(8, 18),
                // (9,18): error CS0826: No best type found for implicitly-typed array
                //         var a3 = new[] { null, default, (object x, ref string y) => { } };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { null, default, (object x, ref string y) => { } }").WithLocation(9, 18));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"<>A{00000001}`1[System.Object]
<>A{00000001}`1[System.String]
<>A{00000008}`2[System.Object,System.String]
");
        }

        /// <summary>
        /// Best common type inference with delegate signatures that cannot be inferred.
        /// </summary>
        [Fact]
        public void BestCommonType_NoInferredSignature()
        {
            var source =
@"class Program
{
    static void F1() { }
    static int F1(int i) => i;
    static void F2() { }
    static void Main()
    {
        var a1 = new[] { F1 };
        var a2 = new[] { F1, F2 };
        var a3 = new[] { F2, F1 };
        var a4 = new[] { x => x };
        var a5 = new[] { x => x, (int y) => y };
        var a6 = new[] { (int y) => y, static x => x };
        var a7 = new[] { x => x, F1 };
        var a8 = new[] { F1, (int y) => y };
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { F1 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F1 }").WithLocation(8, 18),
                // (9,18): error CS0826: No best type found for implicitly-typed array
                //         var a2 = new[] { F1, F2 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F1, F2 }").WithLocation(9, 18),
                // (10,18): error CS0826: No best type found for implicitly-typed array
                //         var a3 = new[] { F2, F1 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F2, F1 }").WithLocation(10, 18),
                // (11,18): error CS0826: No best type found for implicitly-typed array
                //         var a4 = new[] { x => x };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { x => x }").WithLocation(11, 18),
                // (12,18): error CS0826: No best type found for implicitly-typed array
                //         var a5 = new[] { x => x, (int y) => y };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { x => x, (int y) => y }").WithLocation(12, 18),
                // (13,18): error CS0826: No best type found for implicitly-typed array
                //         var a6 = new[] { (int y) => y, static x => x };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { (int y) => y, static x => x }").WithLocation(13, 18),
                // (14,18): error CS0826: No best type found for implicitly-typed array
                //         var a7 = new[] { x => x, F1 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { x => x, F1 }").WithLocation(14, 18),
                // (15,18): error CS0826: No best type found for implicitly-typed array
                //         var a8 = new[] { F1, (int y) => y };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F1, (int y) => y }").WithLocation(15, 18));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { F1 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { F1 }").WithLocation(8, 18),
                // (11,18): error CS0826: No best type found for implicitly-typed array
                //         var a4 = new[] { x => x };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { x => x }").WithLocation(11, 18),
                // (14,18): error CS0826: No best type found for implicitly-typed array
                //         var a7 = new[] { x => x, F1 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { x => x, F1 }").WithLocation(14, 18));
        }

        [Fact]
        public void ArrayInitializer_01()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        var a1 = new Func<int>[] { () => 1 };
        var a2 = new Expression<Func<int>>[] { () => 2 };
        Report(a1[0]);
        Report(a2[0]);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            string expectedOutput =
$@"System.Func`1[System.Int32]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ArrayInitializer_02()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        var a1 = new Delegate[] { () => 1 };
        var a2 = new Expression[] { () => 2 };
        Report(a1[0]);
        Report(a2[0]);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,38): error CS1660: Cannot convert lambda expression to type 'Delegate' because it is not a delegate type
                //         var a1 = new Delegate[] { () => 1 };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Delegate").WithLocation(7, 38),
                // (8,40): error CS1660: Cannot convert lambda expression to type 'Expression' because it is not a delegate type
                //         var a2 = new Expression[] { () => 2 };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression").WithLocation(8, 40));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
$@"System.Func`1[System.Int32]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]");
        }

        [Fact]
        public void ArrayInitializer_03()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var a1 = new[] { () => 1 };
        Report(a1[0]);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,18): error CS0826: No best type found for implicitly-typed array
                //         var a1 = new[] { () => 1 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { () => 1 }").WithLocation(6, 18));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Func`1[System.Int32]");
        }

        [Fact]
        public void NullCoalescingOperator_01()
        {
            var source =
@"class Program
{
    static void F<T>(T t) { }
    static void Main()
    {
        var c1 = F<object> ?? F<string>;
        var c2 = ((object o) => { }) ?? ((string s) => { });
        var c3 = F<string> ?? ((object o) => { });
    }
}";

            var expectedDiagnostics = new[]
            {
                // (6,18): error CS0019: Operator '??' cannot be applied to operands of type 'method group' and 'method group'
                //         var c1 = F<object> ?? F<string>;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "F<object> ?? F<string>").WithArguments("??", "method group", "method group").WithLocation(6, 18),
                // (7,18): error CS0019: Operator '??' cannot be applied to operands of type 'lambda expression' and 'lambda expression'
                //         var c2 = ((object o) => { }) ?? ((string s) => { });
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "((object o) => { }) ?? ((string s) => { })").WithArguments("??", "lambda expression", "lambda expression").WithLocation(7, 18),
                // (8,18): error CS0019: Operator '??' cannot be applied to operands of type 'method group' and 'lambda expression'
                //         var c3 = F<string> ?? ((object o) => { });
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "F<string> ?? ((object o) => { })").WithArguments("??", "method group", "lambda expression").WithLocation(8, 18)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void LambdaReturn_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var a1 = () => () => 1;
        var a2 = () => Main;
        Report(a1());
        Report(a2());
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Func`1[System.Int32]
System.Action");
        }

        [Fact]
        public void InferredType_MethodGroup()
        {
            var source =
@"class Program
{
    static void Main()
    {
        System.Delegate d = Main;
        System.Console.Write(d.GetDelegateTypeName());
    }
}";
            var comp = CreateCompilation(new[] { source, s_utils }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "System.Action");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer!.Value;
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);
        }

        [Fact]
        public void InferredType_LambdaExpression()
        {
            var source =
@"class Program
{
    static void Main()
    {
        System.Delegate d = () => { };
        System.Console.Write(d.GetDelegateTypeName());
    }
}";
            var comp = CreateCompilation(new[] { source, s_utils }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "System.Action");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<AnonymousFunctionExpressionSyntax>().Single();
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Equal("System.Action", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Delegate, typeInfo.ConvertedType!.SpecialType);

            var symbolInfo = model.GetSymbolInfo(expr);
            var method = (IMethodSymbol)symbolInfo.Symbol!;
            Assert.Equal(MethodKind.LambdaMethod, method.MethodKind);
            Assert.True(HaveMatchingSignatures(((INamedTypeSymbol)typeInfo.Type!).DelegateInvokeMethod!, method));
        }

        [WorkItem(55320, "https://github.com/dotnet/roslyn/issues/55320")]
        [Fact]
        public void InferredReturnType_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(() => { return; });
        Report((bool b) => { if (b) return; });
        Report((bool b) => { if (b) return; else return; });
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Action
System.Action`1[System.Boolean]
System.Action`1[System.Boolean]
");
        }

        [Fact]
        public void InferredReturnType_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(async () => { return; });
        Report(async (bool b) => { if (b) return; });
        Report(async (bool b) => { if (b) return; else return; });
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Func`1[System.Threading.Tasks.Task]
System.Func`2[System.Boolean,System.Threading.Tasks.Task]
System.Func`2[System.Boolean,System.Threading.Tasks.Task]
");
        }

        [WorkItem(55320, "https://github.com/dotnet/roslyn/issues/55320")]
        [Fact]
        public void InferredReturnType_03()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report((bool b) => { if (b) return null; });
        Report((bool b) => { if (b) return; else return null; });
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,25): error CS8917: The delegate type could not be inferred.
                //         Report((bool b) => { if (b) return null; });
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(6, 25),
                // (7,25): error CS8917: The delegate type could not be inferred.
                //         Report((bool b) => { if (b) return; else return null; });
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(7, 25),
                // (7,50): error CS8030: Anonymous function converted to a void returning delegate cannot return a value
                //         Report((bool b) => { if (b) return; else return null; });
                Diagnostic(ErrorCode.ERR_RetNoObjectRequiredLambda, "return").WithLocation(7, 50));
        }

        [WorkItem(55320, "https://github.com/dotnet/roslyn/issues/55320")]
        [Fact]
        public void InferredReturnType_04()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report((bool b) => { if (b) return default; });
        Report((bool b) => { if (b) return; else return default; });
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,25): error CS8917: The delegate type could not be inferred.
                //         Report((bool b) => { if (b) return default; });
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(6, 25),
                // (7,25): error CS8917: The delegate type could not be inferred.
                //         Report((bool b) => { if (b) return; else return default; });
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(7, 25),
                // (7,50): error CS8030: Anonymous function converted to a void returning delegate cannot return a value
                //         Report((bool b) => { if (b) return; else return default; });
                Diagnostic(ErrorCode.ERR_RetNoObjectRequiredLambda, "return").WithLocation(7, 50));
        }

        [Fact]
        public void ExplicitReturnType_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(object () => { return; });
        Report(object (bool b) => { if (b) return null; });
        Report(object (bool b) => { if (b) return; else return default; });
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,31): error CS0126: An object of a type convertible to 'object' is required
                //         Report(object () => { return; });
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("object").WithLocation(6, 31),
                // (7,32): error CS1643: Not all code paths return a value in lambda expression of type 'Func<bool, object>'
                //         Report(object (bool b) => { if (b) return null; });
                Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "=>").WithArguments("lambda expression", "System.Func<bool, object>").WithLocation(7, 32),
                // (8,44): error CS0126: An object of a type convertible to 'object' is required
                //         Report(object (bool b) => { if (b) return; else return default; });
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("object").WithLocation(8, 44));
        }

        [Fact]
        public void TypeInference_Constraints_01()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static T F1<T>(T t) where T : Delegate => t;
    static T F2<T>(T t) where T : Expression => t;
    static void Main()
    {
        Report(F1((int i) => { }));
        Report(F2(() => 1));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,16): error CS0411: The type arguments for method 'Program.F1<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F1((int i) => { }));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(T)").WithLocation(9, 16),
                // (10,16): error CS0411: The type arguments for method 'Program.F2<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F2(() => 1));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(T)").WithLocation(10, 16));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
$@"System.Action`1[System.Int32]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
");
        }

        [Fact]
        public void TypeInference_Constraints_02()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class A<T>
{
    public static U F<U>(U u) where U : T => u;
}
class B
{
    static void Main()
    {
        Report(A<object>.F(() => 1));
        Report(A<ICloneable>.F(() => 1));
        Report(A<Delegate>.F(() => 1));
        Report(A<MulticastDelegate>.F(() => 1));
        Report(A<Func<int>>.F(() => 1));
        Report(A<Expression>.F(() => 1));
        Report(A<LambdaExpression>.F(() => 1));
        Report(A<Expression<Func<int>>>.F(() => 1));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
$@"System.Func`1[System.Int32]
System.Func`1[System.Int32]
System.Func`1[System.Int32]
System.Func`1[System.Int32]
System.Func`1[System.Int32]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
");
        }

        [Fact]
        public void TypeInference_Constraints_03()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class A<T, U> where U : T
{
    public static V F<V>(V v) where V : U => v;
}
class B
{
    static void Main()
    {
        Report(A<object, object>.F(() => 1));
        Report(A<object, Delegate>.F(() => 1));
        Report(A<object, Func<int>>.F(() => 1));
        Report(A<Delegate, Func<int>>.F(() => 1));
        Report(A<object, Expression>.F(() => 1));
        Report(A<object, Expression<Func<int>>>.F(() => 1));
        Report(A<Expression, LambdaExpression>.F(() => 1));
        Report(A<Expression, Expression<Func<int>>>.F(() => 1));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
$@"System.Func`1[System.Int32]
System.Func`1[System.Int32]
System.Func`1[System.Int32]
System.Func`1[System.Int32]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
");
        }

        [Fact]
        public void TypeInference_MatchingSignatures()
        {
            var source =
@"using System;
class Program
{
    static T F<T>(T x, T y) => x;
    static int F1(string s) => s.Length;
    static void F2(string s) { }
    static void Main()
    {
        Report(F(F1, (string s) => int.Parse(s)));
        Report(F((string s) => { }, F2));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F1, (string s) => int.Parse(s)));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(9, 16),
                // (10,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F((string s) => { }, F2));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(10, 16));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Func`2[System.String,System.Int32]
System.Action`1[System.String]
");
        }

        [Fact]
        public void TypeInference_DistinctSignatures()
        {
            var source =
@"using System;
class Program
{
    static T F<T>(T x, T y) => x;
    static int F1(object o) => o.GetHashCode();
    static void F2(object o) { }
    static void Main()
    {
        Report(F(F1, (string s) => int.Parse(s)));
        Report(F((string s) => { }, F2));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F1, (string s) => int.Parse(s)));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(9, 16),
                // (10,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F((string s) => { }, F2));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(10, 16));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Func`2[System.String,System.Int32]
System.Action`1[System.String]
");
        }

        [Fact]
        public void TypeInference_01()
        {
            var source =
@"using System;
class Program
{
    static T M<T>(T x, T y) => x;
    static int F1(int i) => i;
    static void F1() { }
    static T F2<T>(T t) => t;
    static void Main()
    {
        var f1 = M(x => x, (int y) => y);
        var f2 = M(F1, F2<int>);
        var f3 = M(F2<object>, z => z);
        Report(f1);
        Report(f2);
        Report(f3);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (10,18): error CS0411: The type arguments for method 'Program.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var f1 = M(x => x, (int y) => y);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T>(T, T)").WithLocation(10, 18),
                // (11,18): error CS0411: The type arguments for method 'Program.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var f2 = M(F1, F2<int>);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T>(T, T)").WithLocation(11, 18),
                // (12,18): error CS0411: The type arguments for method 'Program.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var f3 = M(F2<object>, z => z);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T>(T, T)").WithLocation(12, 18));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Func`2[System.Int32,System.Int32]
System.Func`2[System.Int32,System.Int32]
System.Func`2[System.Object,System.Object]
");
        }

        [Fact]
        public void TypeInference_02()
        {
            var source =
@"using System;
class Program
{
    static T M<T>(T x, T y) where T : class => x ?? y;
    static T F<T>() => default;
    static void Main()
    {
        var f1 = M(F<object>, null);
        var f2 = M(default, F<string>);
        var f3 = M((object x, ref string y) => { }, default);
        var f4 = M(null, (ref object x, string y) => { });
        Report(f1);
        Report(f2);
        Report(f3);
        Report(f4);
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,18): error CS0411: The type arguments for method 'Program.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var f1 = M(F<object>, null);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T>(T, T)").WithLocation(8, 18),
                // (9,18): error CS0411: The type arguments for method 'Program.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var f2 = M(default, F<string>);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T>(T, T)").WithLocation(9, 18),
                // (10,18): error CS0411: The type arguments for method 'Program.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var f3 = M((object x, ref string y) => { }, default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T>(T, T)").WithLocation(10, 18),
                // (11,18): error CS0411: The type arguments for method 'Program.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var f4 = M(null, (ref object x, string y) => { });
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("Program.M<T>(T, T)").WithLocation(11, 18));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Func`1[System.Object]
System.Func`1[System.String]
<>A{00000008}`2[System.Object,System.String]
<>A{00000001}`2[System.Object,System.String]
");
        }

        [Fact]
        public void TypeInference_LowerBoundsMatchingSignature()
        {
            var source =
@"using System;
delegate void D1<T>(T t);
delegate T D2<T>();
class Program
{
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        D1<string> d1 = (string s) => { };
        D2<int> d2 = () => 1;
        Report(F(d1, (string s) => { }));
        Report(F(() => 2, d2));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            var expectedOutput =
@"D1`1[System.String]
D2`1[System.Int32]
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TypeInference_LowerBoundsDistinctSignature_01()
        {
            var source =
@"using System;
delegate void D1<T>(T t);
delegate T D2<T>();
class Program
{
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        D1<string> d1 = (string s) => { };
        D2<int> d2 = () => 1;
        Report(F(d1, (object o) => { }));
        Report(F(() => 1.0, d2));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var expectedDiagnostics = new[]
            {
                // (11,30): error CS1678: Parameter 1 is declared as type 'object' but should be 'string'
                //         Report(F(d1, (object o) => { }));
                Diagnostic(ErrorCode.ERR_BadParamType, "o").WithArguments("1", "", "object", "", "string").WithLocation(11, 30),
                // (11,33): error CS1661: Cannot convert lambda expression to type 'D1<string>' because the parameter types do not match the delegate parameter types
                //         Report(F(d1, (object o) => { }));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "D1<string>").WithLocation(11, 33),
                // (12,24): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         Report(F(() => 1.0, d2));
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1.0").WithArguments("double", "int").WithLocation(12, 24),
                // (12,24): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Report(F(() => 1.0, d2));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "1.0").WithArguments("lambda expression").WithLocation(12, 24)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void TypeInference_LowerBoundsDistinctSignature_02()
        {
            var source =
@"using System;
class Program
{
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        Report(F((string s) => { }, (object o) => { }));
        Report(F(() => string.Empty, () => new object()));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F((string s) => { }, (object o) => { }));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(7, 16),
                // (8,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(() => string.Empty, () => new object()));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(8, 16));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,45): error CS1678: Parameter 1 is declared as type 'object' but should be 'string'
                //         Report(F((string s) => { }, (object o) => { }));
                Diagnostic(ErrorCode.ERR_BadParamType, "o").WithArguments("1", "", "object", "", "string").WithLocation(7, 45),
                // (7,48): error CS1661: Cannot convert lambda expression to type 'Action<string>' because the parameter types do not match the delegate parameter types
                //         Report(F((string s) => { }, (object o) => { }));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "System.Action<string>").WithLocation(7, 48));
        }

        [Fact]
        public void TypeInference_UpperAndLowerBoundsMatchingSignature()
        {
            var source =
@"using System;
delegate void D1<T>(T t);
delegate T D2<T>();
class Program
{
    static T F1<T>(Action<T> x, T y) => y;
    static T F2<T>(T x, Action<T> y) => x;
    static void Main()
    {
        Action<D1<string>> a1 = null;
        Action<D2<int>> a2 = null;
        Report(F1(a1, (string s) => { }));
        Report(F2(() => 2, a2));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            var expectedOutput =
@"D1`1[System.String]
D2`1[System.Int32]
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TypeInference_UpperAndLowerBoundsDistinctSignature_01()
        {
            var source =
@"using System;
delegate void D1<T>(T t);
delegate T D2<T>();
class Program
{
    static T F1<T>(Action<T> x, T y) => y;
    static T F2<T>(T x, Action<T> y) => x;
    static void Main()
    {
        Action<D1<string>> a1 = null;
        Action<D2<object>> a2 = null;
        Report(F1(a1, (object o) => { }));
        Report(F2(() => string.Empty, a2));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var expectedDiagnostics = new[]
            {
                // (12,31): error CS1678: Parameter 1 is declared as type 'object' but should be 'string'
                //         Report(F1(a1, (object o) => { }));
                Diagnostic(ErrorCode.ERR_BadParamType, "o").WithArguments("1", "", "object", "", "string").WithLocation(12, 31),
                // (12,34): error CS1661: Cannot convert lambda expression to type 'D1<string>' because the parameter types do not match the delegate parameter types
                //         Report(F1(a1, (object o) => { }));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "D1<string>").WithLocation(12, 34)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void TypeInference_UpperAndLowerBoundsDistinctSignature_02()
        {
            var source =
@"using System;
delegate void D1<T>(T t);
delegate T D2<T>();
class Program
{
    static T F1<T>(Action<T> x, T y) => y;
    static T F2<T>(T x, Action<T> y) => x;
    static void Main()
    {
        Report(F1((D1<string> d) => { }, (object o) => { }));
        Report(F2(() => string.Empty, (D2<object>  d) => { }));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var expectedDiagnostics = new[]
            {
                // (10,50): error CS1678: Parameter 1 is declared as type 'object' but should be 'string'
                //         Report(F1((D1<string> d) => { }, (object o) => { }));
                Diagnostic(ErrorCode.ERR_BadParamType, "o").WithArguments("1", "", "object", "", "string").WithLocation(10, 50),
                // (10,53): error CS1661: Cannot convert lambda expression to type 'D1<string>' because the parameter types do not match the delegate parameter types
                //         Report(F1((D1<string> d) => { }, (object o) => { }));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "D1<string>").WithLocation(10, 53)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void TypeInference_ExactAndLowerBoundsMatchingSignature()
        {
            var source =
@"using System;
delegate void D1<T>(T t);
delegate T D2<T>();
class Program
{
    static T F1<T>(ref T x, T y) => y;
    static T F2<T>(T x, ref T y) => y;
    static void Main()
    {
        D1<string> d1 = (string s) => { };
        D2<int> d2 = () => 1;
        Report(F1(ref d1, (string s) => { }));
        Report(F2(() => 2, ref d2));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";
            var expectedOutput =
@"D1`1[System.String]
D2`1[System.Int32]
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TypeInference_ExactAndLowerBoundsDistinctSignature_01()
        {
            var source =
@"using System;
delegate void D1<T>(T t);
delegate T D2<T>();
class Program
{
    static T F1<T>(ref T x, T y) => y;
    static T F2<T>(T x, ref T y) => y;
    static void Main()
    {
        D1<string> d1 = (string s) => { };
        D2<object> d2 = () => new object();
        Report(F1(ref d1, (object o) => { }));
        Report(F2(() => string.Empty, ref d2));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var expectedDiagnostics = new[]
            {
                // (12,35): error CS1678: Parameter 1 is declared as type 'object' but should be 'string'
                //         Report(F1(ref d1, (object o) => { }));
                Diagnostic(ErrorCode.ERR_BadParamType, "o").WithArguments("1", "", "object", "", "string").WithLocation(12, 35),
                // (12,38): error CS1661: Cannot convert lambda expression to type 'D1<string>' because the parameter types do not match the delegate parameter types
                //         Report(F1(ref d1, (object o) => { }));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "D1<string>").WithLocation(12, 38)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void TypeInference_ExactAndLowerBoundsDistinctSignature_02()
        {
            var source =
@"using System;
delegate void D1<T>(T t);
delegate T D2<T>();
class Program
{
    static T F1<T>(in T x, T y) => y;
    static T F2<T>(T x, in T y) => y;
    static void Main()
    {
        Report(F1((D1<string> d) => { }, (object o) => { }));
        Report(F2(() => string.Empty, (D2<object> d) => { }));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var expectedDiagnostics = new[]
            {
                // (10,16): error CS0411: The type arguments for method 'Program.F1<T>(in T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F1((D1<string> d) => { }, (object o) => { }));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(in T, T)").WithLocation(10, 16),
                // (11,16): error CS0411: The type arguments for method 'Program.F2<T>(T, in T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F2(() => 1.0, (D2<int> d) => { }));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(T, in T)").WithLocation(11, 16)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,50): error CS1678: Parameter 1 is declared as type 'object' but should be 'D1<string>'
                //         Report(F1((D1<string> d) => { }, (object o) => { }));
                Diagnostic(ErrorCode.ERR_BadParamType, "o").WithArguments("1", "", "object", "", "D1<string>").WithLocation(10, 50),
                // (10,53): error CS1661: Cannot convert lambda expression to type 'Action<D1<string>>' because the parameter types do not match the delegate parameter types
                //         Report(F1((D1<string> d) => { }, (object o) => { }));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "System.Action<D1<string>>").WithLocation(10, 53),
                // (11,16): error CS0411: The type arguments for method 'Program.F2<T>(T, in T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F2(() => string.Empty, (D2<object> d) => { }));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(T, in T)").WithLocation(11, 16));
        }

        [Fact]
        public void TypeInference_Variance_01()
        {
            var source =
@"using System;
class Program
{
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        Report(F(() => string.Empty, () => new object()));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(() => string.Empty, () => new object()));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(7, 16));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Func`1[System.Object]
");
        }

        [Fact]
        public void TypeInference_Variance_02()
        {
            var source =
@"using System;
class Program
{
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        Report(F((string s) => { }, (object o) => { }));
        Report(F(string () => default, object () => default));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F((string s) => { }, (object o) => { }));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(7, 16),
                // (8,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(string () => default, object () => default));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(8, 16),
                // (8,18): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         Report(F(string () => default, object () => default));
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "string").WithArguments("lambda return type", "10.0").WithLocation(8, 18),
                // (8,40): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         Report(F(string () => default, object () => default));
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "object").WithArguments("lambda return type", "10.0").WithLocation(8, 40));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,45): error CS1678: Parameter 1 is declared as type 'object' but should be 'string'
                //         Report(F((string s) => { }, (object o) => { }));
                Diagnostic(ErrorCode.ERR_BadParamType, "o").WithArguments("1", "", "object", "", "string").WithLocation(7, 45),
                // (7,48): error CS1661: Cannot convert lambda expression to type 'Action<string>' because the parameter types do not match the delegate parameter types
                //         Report(F((string s) => { }, (object o) => { }));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "System.Action<string>").WithLocation(7, 48),
                // (8,28): error CS8934: Cannot convert lambda expression to type 'Func<object>' because the return type does not match the delegate return type
                //         Report(F(string () => default, object () => default));
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "System.Func<object>").WithLocation(8, 28));
        }

        [Fact]
        public void TypeInference_Variance_03()
        {
            var source =
@"using System;
class Program
{
    static void F1<T>(T t) { }
    static T F2<T>() => default;
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        Report(F(F1<string>, F1<object>));
        Report(F(F2<string>, F2<object>));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F1<string>, F1<object>));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(9, 16),
                // (10,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F2<string>, F2<object>));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(10, 16));

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"System.Action`1[System.String]
System.Func`1[System.Object]
");
        }

        [Fact]
        [WorkItem(55909, "https://github.com/dotnet/roslyn/issues/55909")]
        public void TypeInference_Variance_04()
        {
            var source =
@"using System;
class Program
{
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        Report(F((out string s) => { s = default; }, (out object o) => { o = default; }));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F((out string s) => { s = default; }, (out object o) => { o = default; });
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(7, 16));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F((out string s) => { s = default; }, (out object o) => { o = default; });
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(7, 16));
        }

        [Fact]
        [WorkItem(55909, "https://github.com/dotnet/roslyn/issues/55909")]
        public void TypeInference_Variance_05()
        {
            var source =
@"using System;
class Program
{
    static void F1<T>(out T t) { t = default; }
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        Report(F(F1<string>, F1<object>));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F1<string>, F1<object>));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(8, 16));

            // Compile and execute after fixing https://github.com/dotnet/roslyn/issues/55909.
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F1<string>, F1<object>));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(8, 16));
        }

        [Fact]
        public void TypeInference_Variance_06()
        {
            var source =
@"using System;
class Program
{
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        Report(F((ref string x) => { }, (ref object y) => { }));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F((out string s) => { s = default; }, (out object o) => { o = default; });
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(7, 16));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F((out string s) => { s = default; }, (out object o) => { o = default; });
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(7, 16));
        }

        [Fact]
        public void TypeInference_Variance_07()
        {
            var source =
@"using System;
class Program
{
    static void F1<T>(ref T t) { }
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        Report(F(F1<string>, F1<object>));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F1<string>, F1<object>));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(8, 16));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F1<string>, F1<object>));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(8, 16));
        }

        [Fact]
        [WorkItem(55909, "https://github.com/dotnet/roslyn/issues/55909")]
        public void TypeInference_Variance_08()
        {
            var source =
@"using System;
class Program
{
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        Report(F((ref int i, string s) => { }, (ref int i, object o) => { }));
        Report(F(string (ref int i) => default, object (ref int i) => default));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F((ref int i, string s) => { }, (ref int i, object o) => { }));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(7, 16),
                // (8,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(string (ref int i) => default, object (ref int i) => default));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(8, 16),
                // (8,18): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         Report(F(string (ref int i) => default, object (ref int i) => default));
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "string").WithArguments("lambda return type", "10.0").WithLocation(8, 18),
                // (8,49): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         Report(F(string (ref int i) => default, object (ref int i) => default));
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "object").WithArguments("lambda return type", "10.0").WithLocation(8, 49));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F((ref int i, string s) => { }, (ref int i, object o) => { }));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(7, 16),
                // (8,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(string (ref int i) => default, object (ref int i) => default));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(8, 16));
        }

        [Fact]
        [WorkItem(55909, "https://github.com/dotnet/roslyn/issues/55909")]
        public void TypeInference_Variance_09()
        {
            var source =
@"using System;
class Program
{
    static void F1<T>(ref int i, T t) { }
    static T F2<T>(ref int i) => default;
    static T F<T>(T x, T y) => y;
    static void Main()
    {
        Report(F(F1<string>, F1<object>));
        Report(F(F2<string>, F2<object>));
    }
    static void Report(object obj) => Console.WriteLine(obj.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F1<string>, F1<object>));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(9, 16),
                // (10,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F2<string>, F2<object>));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(10, 16));

            // Compile and execute after fixing https://github.com/dotnet/roslyn/issues/55909.
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F1<string>, F1<object>));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(9, 16),
                // (10,16): error CS0411: The type arguments for method 'Program.F<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Report(F(F2<string>, F2<object>));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T, T)").WithLocation(10, 16));
        }

        [Fact]
        public void TypeInference_Nested_01()
        {
            var source =
@"delegate void D<T>(T t);
class Program
{
    static T F1<T>(T t) => t;
    static D<T> F2<T>(D<T> d) => d;
    static void Main()
    {
        F2(F1((string s) => { }));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,12): error CS0411: The type arguments for method 'Program.F1<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F2(F1((string s) => { }));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(T)").WithLocation(8, 12));

            // Reports error on F1() in C#9, and reports error on F2() in C#10.
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,9): error CS0411: The type arguments for method 'Program.F2<T>(D<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F2(F1((string s) => { }));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(D<T>)").WithLocation(8, 9));
        }

        [Fact]
        public void TypeInference_Nested_02()
        {
            var source =
@"using System.Linq.Expressions;
class Program
{
    static T F1<T>(T x) => throw null;
    static Expression<T> F2<T>(Expression<T> e) => e;
    static void Main()
    {
        F2(F1((object x1) => 1));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,12): error CS0411: The type arguments for method 'Program.F1<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F2(F1((string s) => { }));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(T)").WithLocation(8, 12));

            // Reports error on F1() in C#9, and reports error on F2() in C#10.
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,9): error CS0411: The type arguments for method 'Program.F2<T>(Expression<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F2(F1((object x1) => 1));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(System.Linq.Expressions.Expression<T>)").WithLocation(8, 9));
        }

        /// <summary>
        /// Method type inference with delegate signatures that cannot be inferred.
        /// </summary>
        [Fact]
        public void TypeInference_NoInferredSignature()
        {
            var source =
@"class Program
{
    static void F1() { }
    static void F1(int i) { }
    static void F2() { }
    static T M1<T>(T t) => t;
    static T M2<T>(T x, T y) => x;
    static void Main()
    {
        var a1 = M1(F1);
        var a2 = M2(F1, F2);
        var a3 = M2(F2, F1);
        var a4 = M1(x => x);
        var a5 = M2(x => x, (int y) => y);
        var a6 = M2((int y) => y, x => x);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (10,18): error CS0411: The type arguments for method 'Program.M1<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var a1 = M1(F1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("Program.M1<T>(T)").WithLocation(10, 18),
                // (11,18): error CS0411: The type arguments for method 'Program.M2<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var a2 = M2(F1, F2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("Program.M2<T>(T, T)").WithLocation(11, 18),
                // (12,18): error CS0411: The type arguments for method 'Program.M2<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var a3 = M2(F2, F1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("Program.M2<T>(T, T)").WithLocation(12, 18),
                // (13,18): error CS0411: The type arguments for method 'Program.M1<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var a4 = M1(x => x);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("Program.M1<T>(T)").WithLocation(13, 18),
                // (14,18): error CS0411: The type arguments for method 'Program.M2<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var a5 = M2(x => x, (int y) => y);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("Program.M2<T>(T, T)").WithLocation(14, 18),
                // (15,18): error CS0411: The type arguments for method 'Program.M2<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var a6 = M2((int y) => y, x => x);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("Program.M2<T>(T, T)").WithLocation(15, 18));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,18): error CS0411: The type arguments for method 'Program.M1<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var a1 = M1(F1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("Program.M1<T>(T)").WithLocation(10, 18),
                // (13,18): error CS0411: The type arguments for method 'Program.M1<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var a4 = M1(x => x);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("Program.M1<T>(T)").WithLocation(13, 18));
        }

        [Fact]
        public void TypeInference_ExplicitReturnType_01()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void F1<T>(Func<T> f) { Console.WriteLine(f.GetType()); }
    static void F2<T>(Expression<Func<T>> e) { Console.WriteLine(e.GetType()); }
    static void Main()
    {
        F1(int () => throw new Exception());
        F2(int () => default);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F1(int () => throw new Exception());
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "int").WithArguments("lambda return type", "10.0").WithLocation(9, 12),
                // (10,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F2(int () => default);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "int").WithArguments("lambda return type", "10.0").WithLocation(10, 12));

            var expectedOutput =
$@"System.Func`1[System.Int32]
{s_expressionOfTDelegate0ArgTypeName}[System.Func`1[System.Int32]]
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_ExplicitReturnType_02()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void F1<T>(Func<T, T> f) { Console.WriteLine(f.GetType()); }
    static void F2<T>(Expression<Func<T, T>> e) { Console.WriteLine(e.GetType()); }
    static void Main()
    {
        F1(int (i) => i);
        F2(string (s) => s);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F1(int (i) => i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "int").WithArguments("lambda return type", "10.0").WithLocation(9, 12),
                // (10,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F2(string (s) => s);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "string").WithArguments("lambda return type", "10.0").WithLocation(10, 12));

            var expectedOutput =
$@"System.Func`2[System.Int32,System.Int32]
{s_expressionOfTDelegate1ArgTypeName}[System.Func`2[System.String,System.String]]
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_ExplicitReturnType_03()
        {
            var source =
@"using System;
delegate ref T D1<T>(T t);
delegate ref readonly T D2<T>(T t);
class Program
{
    static void F1<T>(D1<T> d) { Console.WriteLine(d.GetType()); }
    static void F2<T>(D2<T> d) { Console.WriteLine(d.GetType()); }
    static void Main()
    {
        F1((ref int (i) => ref i));
        F2((ref readonly string (s) => ref s));
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (10,13): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F1((ref int (i) => ref i));
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "ref int").WithArguments("lambda return type", "10.0").WithLocation(10, 13),
                // (10,32): error CS8166: Cannot return a parameter by reference 'i' because it is not a ref parameter
                //         F1((ref int (i) => ref i));
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(10, 32),
                // (11,13): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F2((ref readonly string (s) => ref s));
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "ref readonly string").WithArguments("lambda return type", "10.0").WithLocation(11, 13),
                // (11,44): error CS8166: Cannot return a parameter by reference 's' because it is not a ref parameter
                //         F2((ref readonly string (s) => ref s));
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "s").WithArguments("s").WithLocation(11, 44));

            var expectedDiagnostics = new[]
            {
                // (10,32): error CS8166: Cannot return a parameter by reference 'i' because it is not a ref parameter
                //         F1((ref int (i) => ref i));
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(10, 32),
                // (11,44): error CS8166: Cannot return a parameter by reference 's' because it is not a ref parameter
                //         F2((ref readonly string (s) => ref s));
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "s").WithArguments("s").WithLocation(11, 44)
            };

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_ExplicitReturnType_04()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void F1<T>(Func<T, T> x, T y) { Console.WriteLine(x.GetType()); }
    static void F2<T>(Expression<Func<T, T>> x, T y) { Console.WriteLine(x.GetType()); }
    static void Main()
    {
        F1(object (o) => o, 1);
        F1(int (i) => i, 2);
        F2(object (o) => o, string.Empty);
        F2(string (s) => s, string.Empty);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F1(object (o) => o, 1);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "object").WithArguments("lambda return type", "10.0").WithLocation(9, 12),
                // (10,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F1(int (i) => i, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "int").WithArguments("lambda return type", "10.0").WithLocation(10, 12),
                // (11,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F2(object (o) => o, string.Empty);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "object").WithArguments("lambda return type", "10.0").WithLocation(11, 12),
                // (12,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F2(string (s) => s, string.Empty);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "string").WithArguments("lambda return type", "10.0").WithLocation(12, 12));

            var expectedOutput =
$@"System.Func`2[System.Object,System.Object]
System.Func`2[System.Int32,System.Int32]
{s_expressionOfTDelegate1ArgTypeName}[System.Func`2[System.Object,System.Object]]
{s_expressionOfTDelegate1ArgTypeName}[System.Func`2[System.String,System.String]]
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_ExplicitReturnType_05()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void F1<T>(Func<T, T> x, T y) { Console.WriteLine(x.GetType()); }
    static void F2<T>(Expression<Func<T, T>> x, T y) { Console.WriteLine(x.GetType()); }
    static void Main()
    {
        F1(int (i) => i, (object)1);
        F2(string (s) => s, (object)2);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,9): error CS0411: The type arguments for method 'Program.F1<T>(Func<T, T>, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F1(int (i) => i, (object)1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(System.Func<T, T>, T)").WithLocation(9, 9),
                // (9,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F1(int (i) => i, (object)1);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "int").WithArguments("lambda return type", "10.0").WithLocation(9, 12),
                // (10,9): error CS0411: The type arguments for method 'Program.F2<T>(Expression<Func<T, T>>, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F2(string (s) => s, (object)2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(System.Linq.Expressions.Expression<System.Func<T, T>>, T)").WithLocation(10, 9),
                // (10,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F2(string (s) => s, (object)2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "string").WithArguments("lambda return type", "10.0").WithLocation(10, 12));

            var expectedDiagnostics = new[]
            {
                // (9,9): error CS0411: The type arguments for method 'Program.F1<T>(Func<T, T>, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F1(int (i) => i, (object)1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(System.Func<T, T>, T)").WithLocation(9, 9),
                // (10,9): error CS0411: The type arguments for method 'Program.F2<T>(Expression<Func<T, T>>, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F2(string (s) => s, (object)2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(System.Linq.Expressions.Expression<System.Func<T, T>>, T)").WithLocation(10, 9)
            };

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_ExplicitReturnType_06()
        {
            var source =
@"using System;
using System.Linq.Expressions;
interface I<T> { }
class Program
{
    static void F1<T>(Func<T, T> f) { }
    static void F2<T>(Func<T, I<T>> f) { }
    static void F3<T>(Func<I<T>, T> f) { }
    static void F4<T>(Func<I<T>, I<T>> f) { }
    static void F5<T>(Expression<Func<T, T>> e) { }
    static void F6<T>(Expression<Func<T, I<T>>> e) { }
    static void F7<T>(Expression<Func<I<T>, T>> e) { }
    static void F8<T>(Expression<Func<I<T>, I<T>>> e) { }
    static void Main()
    {
        F1(int (int i) => default);
        F2(I<int> (int i) => default);
        F3(int (I<int> i) => default);
        F4(I<int> (I<int> i) => default);
        F5(int (int i) => default);
        F6(I<int> (int i) => default);
        F7(int (I<int> i) => default);
        F8(I<int> (I<int> i) => default);
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_ExplicitReturnType_07()
        {
            var source =
@"using System;
using System.Linq.Expressions;
interface I<T> { }
class Program
{
    static void F1<T>(Func<T, T> f) { }
    static void F2<T>(Func<T, I<T>> f) { }
    static void F3<T>(Func<I<T>, T> f) { }
    static void F4<T>(Func<I<T>, I<T>> f) { }
    static void F5<T>(Expression<Func<T, T>> e) { }
    static void F6<T>(Expression<Func<T, I<T>>> e) { }
    static void F7<T>(Expression<Func<I<T>, T>> e) { }
    static void F8<T>(Expression<Func<I<T>, I<T>>> e) { }
    static void Main()
    {
        F1(int (object i) => default);
        F2(I<int> (object i) => default);
        F3(object (I<int> i) => default);
        F4(I<object> (I<int> i) => default);
        F5(object (int i) => default);
        F6(I<object> (int i) => default);
        F7(int (I<object> i) => default);
        F8(I<int> (I<object> i) => default);
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16,9): error CS0411: The type arguments for method 'Program.F1<T>(Func<T, T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F1(int (object i) => default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(System.Func<T, T>)").WithLocation(16, 9),
                // (17,9): error CS0411: The type arguments for method 'Program.F2<T>(Func<T, I<T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F2(I<int> (object i) => default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(System.Func<T, I<T>>)").WithLocation(17, 9),
                // (18,9): error CS0411: The type arguments for method 'Program.F3<T>(Func<I<T>, T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F3(object (I<int> i) => default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F3").WithArguments("Program.F3<T>(System.Func<I<T>, T>)").WithLocation(18, 9),
                // (19,9): error CS0411: The type arguments for method 'Program.F4<T>(Func<I<T>, I<T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F4(I<object> (I<int> i) => default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F4").WithArguments("Program.F4<T>(System.Func<I<T>, I<T>>)").WithLocation(19, 9),
                // (20,9): error CS0411: The type arguments for method 'Program.F5<T>(Expression<Func<T, T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F5(object (int i) => default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F5").WithArguments("Program.F5<T>(System.Linq.Expressions.Expression<System.Func<T, T>>)").WithLocation(20, 9),
                // (21,9): error CS0411: The type arguments for method 'Program.F6<T>(Expression<Func<T, I<T>>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F6(I<object> (int i) => default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F6").WithArguments("Program.F6<T>(System.Linq.Expressions.Expression<System.Func<T, I<T>>>)").WithLocation(21, 9),
                // (22,9): error CS0411: The type arguments for method 'Program.F7<T>(Expression<Func<I<T>, T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F7(int (I<object> i) => default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F7").WithArguments("Program.F7<T>(System.Linq.Expressions.Expression<System.Func<I<T>, T>>)").WithLocation(22, 9),
                // (23,9): error CS0411: The type arguments for method 'Program.F8<T>(Expression<Func<I<T>, I<T>>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F8(I<int> (I<object> i) => default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F8").WithArguments("Program.F8<T>(System.Linq.Expressions.Expression<System.Func<I<T>, I<T>>>)").WithLocation(23, 9));
        }

        // Variance in inference from explicit return type is disallowed
        // (see https://github.com/dotnet/csharplang/blob/main/meetings/2021/LDM-2021-06-21.md).
        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_ExplicitReturnType_08()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void F1<T>(Func<T, T> x, Func<T, T> y) { Console.WriteLine(x.GetType()); }
    static void F2<T>(Expression<Func<T, T>> x, Expression<Func<T, T>> y) { Console.WriteLine(x.GetType()); }
    static void Main()
    {
        F1(int (x) => x, int (y) => y);
        F1(object (x) => x, int (y) => y);
        F2(string (x) => x, string (y) => y);
        F2(string (x) => x, object (y) => y);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F1(int (x) => x, int (y) => y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "int").WithArguments("lambda return type", "10.0").WithLocation(9, 12),
                // (9,26): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F1(int (x) => x, int (y) => y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "int").WithArguments("lambda return type", "10.0").WithLocation(9, 26),
                // (10,9): error CS0411: The type arguments for method 'Program.F1<T>(Func<T, T>, Func<T, T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F1(object (x) => x, int (y) => y);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(System.Func<T, T>, System.Func<T, T>)").WithLocation(10, 9),
                // (10,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F1(object (x) => x, int (y) => y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "object").WithArguments("lambda return type", "10.0").WithLocation(10, 12),
                // (10,29): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F1(object (x) => x, int (y) => y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "int").WithArguments("lambda return type", "10.0").WithLocation(10, 29),
                // (11,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F2(string (x) => x, string (y) => y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "string").WithArguments("lambda return type", "10.0").WithLocation(11, 12),
                // (11,29): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F2(string (x) => x, string (y) => y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "string").WithArguments("lambda return type", "10.0").WithLocation(11, 29),
                // (12,9): error CS0411: The type arguments for method 'Program.F2<T>(Expression<Func<T, T>>, Expression<Func<T, T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F2(string (x) => x, object (y) => y);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(System.Linq.Expressions.Expression<System.Func<T, T>>, System.Linq.Expressions.Expression<System.Func<T, T>>)").WithLocation(12, 9),
                // (12,12): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F2(string (x) => x, object (y) => y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "string").WithArguments("lambda return type", "10.0").WithLocation(12, 12),
                // (12,29): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F2(string (x) => x, object (y) => y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "object").WithArguments("lambda return type", "10.0").WithLocation(12, 29));

            var expectedDiagnostics = new[]
            {
                // (10,9): error CS0411: The type arguments for method 'Program.F1<T>(Func<T, T>, Func<T, T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F1(object (x) => x, int (y) => y);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Program.F1<T>(System.Func<T, T>, System.Func<T, T>)").WithLocation(10, 9),
                // (12,9): error CS0411: The type arguments for method 'Program.F2<T>(Expression<Func<T, T>>, Expression<Func<T, T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F2(string (x) => x, object (y) => y);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F2").WithArguments("Program.F2<T>(System.Linq.Expressions.Expression<System.Func<T, T>>, System.Linq.Expressions.Expression<System.Func<T, T>>)").WithLocation(12, 9)
            };

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_ExplicitReturnType_09()
        {
            var source =
@"#nullable enable
using System;
using System.Linq.Expressions;
class Program
{
    static T F1<T>(Func<T, T> f) => default!;
    static T F2<T>(Expression<Func<T, T>> e) => default!;
    static void Main()
    {
        F1(object (x1) => x1).ToString();
        F2(object (x2) => x2).ToString();
        F1(object? (y1) => y1).ToString();
        F2(object? (y2) => y2).ToString();
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,9): warning CS8602: Dereference of a possibly null reference.
                //         F1(object? (y1) => y1).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F1(object? (y1) => y1)").WithLocation(12, 9),
                // (13,9): warning CS8602: Dereference of a possibly null reference.
                //         F2(object? (y2) => y2).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F2(object? (y2) => y2)").WithLocation(13, 9));
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_ExplicitReturnType_10()
        {
            var source =
@"#nullable enable
using System;
using System.Linq.Expressions;
class Program
{
    static T F1<T>(Func<T, T> f) => default!;
    static T F2<T>(Expression<Func<T, T>> e) => default!;
    static void Main()
    {
        F1(
#nullable disable
            object (x1) =>
#nullable enable
                x1).ToString();
        F2(
#nullable disable
            object (x2) =>
#nullable enable
                x2).ToString();
        F1(
#nullable disable
            object
#nullable enable
                (y1) => y1).ToString();
        F2(
#nullable disable
            object
#nullable enable
                (y2) => y2).ToString();
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_ExplicitReturnType_11()
        {
            var source =
@"#nullable enable
using System;
using System.Linq.Expressions;
class Program
{
    static T F1<T>(Func<T, T> f) => default!;
    static T F2<T>(Expression<Func<T, T>> e) => default!;
    static void Main()
    {
        var x1 = F1(
#nullable enable
            object?
#nullable disable
                (x1) =>
#nullable enable
                x1);
        var x2 = F2(
#nullable enable
            object?
#nullable disable
                (x2) =>
#nullable enable
                x2);
        var y1 = F1(
#nullable enable
            object
#nullable disable
                (y1) =>
#nullable enable
                y1);
        var y2 = F2(
#nullable enable
            object
#nullable disable
                (y2) =>
#nullable enable
                y2);
        x1.ToString();
        x2.ToString();
        y1.ToString();
        y2.ToString();
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (38,9): warning CS8602: Dereference of a possibly null reference.
                //         x1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1").WithLocation(38, 9),
                // (39,9): warning CS8602: Dereference of a possibly null reference.
                //         x2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(39, 9));
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_ExplicitReturnType_12()
        {
            var source =
@"#nullable enable
using System;
using System.Linq.Expressions;
class Program
{
    static T F1<T>(Func<T, T> f) => default!;
    static T F2<T>(Expression<Func<T, T>> e) => default!;
    static void Main()
    {
        var x1 = F1(object (object x1) => x1);
        var x2 = F1(object (object? x2) => x2);
        var x3 = F1(object? (object x3) => x3);
        var x4 = F1(object? (object? x4) => x4);
        var y1 = F2(object (object y1) => y1);
        var y2 = F2(object (object? y2) => y2);
        var y3 = F2(object? (object y3) => y3);
        var y4 = F2(object? (object? y4) => y4);
        x1.ToString();
        x2.ToString();
        x3.ToString();
        x4.ToString();
        y1.ToString();
        y2.ToString();
        y3.ToString();
        y4.ToString();
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,21): warning CS8622: Nullability of reference types in type of parameter 'x2' of 'lambda expression' doesn't match the target delegate 'Func<object, object>' (possibly because of nullability attributes).
                //         var x2 = F1(object (object? x2) => x2);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "object (object? x2) =>").WithArguments("x2", "lambda expression", "System.Func<object, object>").WithLocation(11, 21),
                // (11,44): warning CS8603: Possible null reference return.
                //         var x2 = F1(object (object? x2) => x2);
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "x2").WithLocation(11, 44),
                // (12,21): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<object, object>' (possibly because of nullability attributes).
                //         var x3 = F1(object? (object x3) => x3);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "object? (object x3) =>").WithArguments("lambda expression", "System.Func<object, object>").WithLocation(12, 21),
                // (15,21): warning CS8622: Nullability of reference types in type of parameter 'y2' of 'lambda expression' doesn't match the target delegate 'Func<object, object>' (possibly because of nullability attributes).
                //         var y2 = F2(object (object? y2) => y2);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "object (object? y2) =>").WithArguments("y2", "lambda expression", "System.Func<object, object>").WithLocation(15, 21),
                // (15,44): warning CS8603: Possible null reference return.
                //         var y2 = F2(object (object? y2) => y2);
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "y2").WithLocation(15, 44),
                // (16,21): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<object, object>' (possibly because of nullability attributes).
                //         var y3 = F2(object? (object y3) => y3);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "object? (object y3) =>").WithArguments("lambda expression", "System.Func<object, object>").WithLocation(16, 21),
                // (21,9): warning CS8602: Dereference of a possibly null reference.
                //         x4.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x4").WithLocation(21, 9),
                // (25,9): warning CS8602: Dereference of a possibly null reference.
                //         y4.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y4").WithLocation(25, 9));
        }

        [WorkItem(57517, "https://github.com/dotnet/roslyn/issues/57517")]
        [Fact]
        public void TypeInference_03()
        {
            var source =
@"using System;
using System.Linq.Expressions;

void Test1<T>(Func<T> exp) {}
void Test2<T>(Expression<Func<T>> exp) {}
void Test3<T>(Func<Func<T>> exp) {}
void Test4<T>(Func<Expression<Func<T>>> exp) {}

Test1(() => 1);
Test2(() => 2);
Test3(() => () => 3);
Test4(() => () => 4);
";

            var expectedDiagnostics = new[]
            {
                // (6,6): warning CS8321: The local function 'Test3' is declared but never used
                // void Test3<T>(Func<Func<T>> exp) {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Test3").WithArguments("Test3").WithLocation(6, 6),
                // (7,6): warning CS8321: The local function 'Test4' is declared but never used
                // void Test4<T>(Func<Expression<Func<T>>> exp) {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Test4").WithArguments("Test4").WithLocation(7, 6),
                // (11,1): error CS0411: The type arguments for method 'Test3<T>(Func<Func<T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                // Test3(() => () => 3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test3").WithArguments("Test3<T>(System.Func<System.Func<T>>)").WithLocation(11, 1),
                // (12,1): error CS0411: The type arguments for method 'Test4<T>(Func<Expression<Func<T>>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                // Test4(() => () => 4);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test4").WithArguments("Test4<T>(System.Func<System.Linq.Expressions.Expression<System.Func<T>>>)").WithLocation(12, 1)
            };
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [WorkItem(57517, "https://github.com/dotnet/roslyn/issues/57517")]
        [Fact]
        public void TypeInference_04()
        {
            var source =
@"#nullable enable
using System;
using System.Linq.Expressions;
delegate int D();
class Program
{
    static int F() => 0;
    static void M1<T>(T t, Func<T> f) { }
    static void M2<T>(T t, Expression<Func<T>> e) { }
    static void Main()
    {
        D d = null;
        M1(d, () => F);
        M2(d, () => F);
        M1(d, () => () => 1);
        M2(d, () => () => 2);
    }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10);
            CompileAndVerify(source);
        }

        [WorkItem(57517, "https://github.com/dotnet/roslyn/issues/57517")]
        [Fact]
        public void TypeInference_05()
        {
            var source =
@"#nullable enable
using System;
using System.Linq.Expressions;
delegate int D();
class Program
{
    static int F() => 0;
    static void M1<T>(ref T t, Func<T> f) { }
    static void M2<T>(ref T t, Expression<Func<T>> e) { }
    static void Main()
    {
        D d = null;
        M1(ref d, () => F);
        M2(ref d, () => F);
        M1(ref d, () => () => 1);
        M2(ref d, () => () => 2);
    }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10);
            CompileAndVerify(source);
        }

        [WorkItem(57517, "https://github.com/dotnet/roslyn/issues/57517")]
        [Fact]
        public void TypeInference_06()
        {
            var source =
@"#nullable enable
using System;
using System.Linq.Expressions;
class Program
{
    static int F() => 0;
    static void M1<T>(Func<T> f) { }
    static void M2<T>(Expression<Func<T>> e) { }
    static void Main()
    {
        M1(() => F);
        M2(() => F);
        M1(() => () => 1);
        M2(() => () => 2);
    }
}";

            var expectedDiagnostics = new[]
            {
                // (11,9): error CS0411: The type arguments for method 'Program.M1<T>(Func<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(() => F);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("Program.M1<T>(System.Func<T>)").WithLocation(11, 9),
                // (12,9): error CS0411: The type arguments for method 'Program.M2<T>(Expression<Func<T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M2(() => F);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("Program.M2<T>(System.Linq.Expressions.Expression<System.Func<T>>)").WithLocation(12, 9),
                // (13,9): error CS0411: The type arguments for method 'Program.M1<T>(Func<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(() => () => 1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("Program.M1<T>(System.Func<T>)").WithLocation(13, 9),
                // (14,9): error CS0411: The type arguments for method 'Program.M2<T>(Expression<Func<T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M2(() => () => 2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("Program.M2<T>(System.Linq.Expressions.Expression<System.Func<T>>)").WithLocation(14, 9)
            };
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [WorkItem(57517, "https://github.com/dotnet/roslyn/issues/57517")]
        [Fact]
        public void TypeInference_07()
        {
            var source =
@"#nullable enable
using System;
using System.Linq.Expressions;
class Program
{
    static int F() => 0;
    static void M1<T>(Func<Func<T>> f) { }
    static void M2<T>(Func<Expression<Func<T>>> e) { }
    static void M3<T>(Expression<Func<Func<T>>> e) { }
    static void Main()
    {
        M1(() => () => F);
        M2(() => () => F);
        M3(() => () => F);
        M1(() => () => () => 1);
        M2(() => () => () => 2);
        M3(() => () => () => 3);
    }
}";

            var expectedDiagnostics = new[]
            {
                // (12,9): error CS0411: The type arguments for method 'Program.M1<T>(Func<Func<T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(() => () => F);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("Program.M1<T>(System.Func<System.Func<T>>)").WithLocation(12, 9),
                // (13,9): error CS0411: The type arguments for method 'Program.M2<T>(Func<Expression<Func<T>>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M2(() => () => F);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("Program.M2<T>(System.Func<System.Linq.Expressions.Expression<System.Func<T>>>)").WithLocation(13, 9),
                // (14,9): error CS0411: The type arguments for method 'Program.M3<T>(Expression<Func<Func<T>>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M3(() => () => F);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M3").WithArguments("Program.M3<T>(System.Linq.Expressions.Expression<System.Func<System.Func<T>>>)").WithLocation(14, 9),
                // (15,9): error CS0411: The type arguments for method 'Program.M1<T>(Func<Func<T>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(() => () => () => 1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("Program.M1<T>(System.Func<System.Func<T>>)").WithLocation(15, 9),
                // (16,9): error CS0411: The type arguments for method 'Program.M2<T>(Func<Expression<Func<T>>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M2(() => () => () => 2);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("Program.M2<T>(System.Func<System.Linq.Expressions.Expression<System.Func<T>>>)").WithLocation(16, 9),
                // (17,9): error CS0411: The type arguments for method 'Program.M3<T>(Expression<Func<Func<T>>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M3(() => () => () => 3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M3").WithArguments("Program.M3<T>(System.Linq.Expressions.Expression<System.Func<System.Func<T>>>)").WithLocation(17, 9)
            };
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [WorkItem(57517, "https://github.com/dotnet/roslyn/issues/57517")]
        [Fact]
        public void TypeInference_08()
        {
            var source =
@"#nullable enable
using System;
using System.Linq.Expressions;
delegate int D();
class Program
{
    static int F() => 0;
    static void M1<T>(T t, Func<Func<T>> f) { }
    static void M2<T>(T t, Func<Expression<Func<T>>> e) { }
    static void M3<T>(T t, Expression<Func<Func<T>>> e) { }
    static void Main()
    {
        D d = null;
        M1(d, () => () => F);
        M2(d, () => () => F);
        M3(d, () => () => F);
        M1(d, () => () => () => 1);
        M2(d, () => () => () => 2);
        M3(d, () => () => () => 3);
    }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10);
            CompileAndVerify(source);
        }

        [WorkItem(57517, "https://github.com/dotnet/roslyn/issues/57517")]
        [Fact]
        public void TypeInference_09()
        {
            var source =
@"#nullable enable
using System;
delegate void D<T>(object x, T y);
class Program
{
    static void F(object x, int y) { }
    static void M<T>(T t, Func<T> f) { }
    static void Main()
    {
        D<int> d = null;
        M(d, () => F);
        M(d, () => (object x, int y) => { });
    }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10);
            CompileAndVerify(source);
        }

        [WorkItem(57517, "https://github.com/dotnet/roslyn/issues/57517")]
        [Fact]
        public void TypeInference_10()
        {
            var source =
@"#nullable enable
using System;
delegate void D1<T>(object x, T y);
delegate T D2<T>();
class Program
{
    static void F(object x, int y) { }
    static void M<T>(T t, D2<T> d) { }
    static void Main()
    {
        D1<int> d = null;
        M(d, () => F);
        M(d, () => (object x, int y) => { });
    }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10);
            CompileAndVerify(source);
        }

        [WorkItem(57517, "https://github.com/dotnet/roslyn/issues/57517")]
        [Fact]
        public void TypeInference_11()
        {
            var source =
@"#nullable enable
using System;
delegate T D1<T>();
delegate T D2<T>(ref object o);
class Program
{
    static int F() => 0;
    static void M<T>(T t, D2<T> d) { }
    static void Main()
    {
        D1<int> d = null;
        M(d, (ref object o) => F);
        M(d, (ref object o) => () => 1);
    }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10);
            CompileAndVerify(source);
        }

        [WorkItem(57517, "https://github.com/dotnet/roslyn/issues/57517")]
        [Fact]
        public void TypeInference_12()
        {
            var source =
@"#nullable enable
using System;
delegate int D();
class Program
{
    static void M<T>(T t, Func<bool, T> f) { }
    static void Main()
    {
        D d = null;
        M(d, (bool b) => { if (b) return () => 1; return () => 2; });
    }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10);
            CompileAndVerify(source);
        }

        [WorkItem(57630, "https://github.com/dotnet/roslyn/issues/57630")]
        [Fact]
        public void TypeInference_13()
        {
            var source =
@"#nullable enable
using System;
delegate void D();
class C<T> { }
static class E
{
    public static void F<T>(this C<T> c, Func<T> f) { }
}
class Program
{
    static void Main()
    {
        var c = new C<D>();
        c.F(() => () => { });
    }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10);
            CompileAndVerify(source);
        }

        [Fact]
        public void Variance_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Action<string> a1 = s => { };
        Action<string> a2 = (string s) => { };
        Action<string> a3 = (object o) => { };
        Action<string> a4 = (Action<object>)((object o) => { });
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,37): error CS1678: Parameter 1 is declared as type 'object' but should be 'string'
                //         Action<string> a3 = (object o) => { };
                Diagnostic(ErrorCode.ERR_BadParamType, "o").WithArguments("1", "", "object", "", "string").WithLocation(8, 37),
                // (8,40): error CS1661: Cannot convert lambda expression to type 'Action<string>' because the parameter types do not match the delegate parameter types
                //         Action<string> a3 = (object o) => { };
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "System.Action<string>").WithLocation(8, 40));
        }

        [Fact]
        public void Variance_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Func<object> f1 = () => string.Empty;
        Func<object> f2 = string () => string.Empty;
        Func<object> f3 = (Func<string>)(() => string.Empty);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,37): error CS8934: Cannot convert lambda expression to type 'Func<object>' because the return type does not match the delegate return type
                //         Func<object> f2 = string () => string.Empty;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "System.Func<object>").WithLocation(7, 37));
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

            comp = CreateCompilation(new[] { source, s_utils }, parseOptions: TestOptions.Regular10, options: TestOptions.DebugExe);
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
        public void ImplicitlyTypedVariables_15()
        {
            var source =
@"class Program
{
    static string F1() => string.Empty;
    static void F2(object o) { }
    static void M(bool b)
    {
        var d1 = b ? () => string.Empty : () => string.Empty;
        var d2 = b ? F1 : () => string.Empty;
        var d3 = b ? (object o) => { } : F2;
        var d4 = b ? F2 : F2;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (7,18): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'lambda expression' and 'lambda expression'
                //         var d1 = b ? () => string.Empty : () => string.Empty;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? () => string.Empty : () => string.Empty").WithArguments("lambda expression", "lambda expression").WithLocation(7, 18),
                // (8,18): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'method group' and 'lambda expression'
                //         var d2 = b ? F1 : () => string.Empty;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? F1 : () => string.Empty").WithArguments("method group", "lambda expression").WithLocation(8, 18),
                // (9,18): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'lambda expression' and 'method group'
                //         var d3 = b ? (object o) => { } : F2;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? (object o) => { } : F2").WithArguments("lambda expression", "method group").WithLocation(9, 18),
                // (10,18): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'method group' and 'method group'
                //         var d4 = b ? F2 : F2;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? F2 : F2").WithArguments("method group", "method group").WithLocation(10, 18)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
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

        [Fact]
        public void BinaryOperator_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var b1 = (() => { }) == null;
        var b2 = null == Main;
        var b3 = Main == (() => { });
        Console.WriteLine((b1, b2, b3));
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,18): error CS0019: Operator '==' cannot be applied to operands of type 'lambda expression' and '<null>'
                //         var b1 = (() => { }) == null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(() => { }) == null").WithArguments("==", "lambda expression", "<null>").WithLocation(6, 18),
                // (7,18): error CS0019: Operator '==' cannot be applied to operands of type '<null>' and 'method group'
                //         var b2 = null == Main;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "null == Main").WithArguments("==", "<null>", "method group").WithLocation(7, 18),
                // (8,18): error CS0019: Operator '==' cannot be applied to operands of type 'method group' and 'lambda expression'
                //         var b3 = Main == (() => { });
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "Main == (() => { })").WithArguments("==", "method group", "lambda expression").WithLocation(8, 18));

            var expectedOutput = @"(False, False, False)";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void BinaryOperator_02()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C
{
    public static C operator+(C c, Delegate d) { Console.WriteLine(""operator+(C c, Delegate d)""); return c; }
    public static C operator+(C c, Expression e) { Console.WriteLine(""operator=(C c, Expression e)""); return c; }
    static void Main()
    {
        var c = new C();
        _ = c + Main;
        _ = c + (() => 1);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (10,13): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'method group'
                //         _ = c + Main;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c + Main").WithArguments("+", "C", "method group").WithLocation(10, 13),
                // (11,13): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'lambda expression'
                //         _ = c + (() => 1);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c + (() => 1)").WithArguments("+", "C", "lambda expression").WithLocation(11, 13));

            var expectedDiagnostics = new[]
            {
                // (10,13): error CS0034: Operator '+' is ambiguous on operands of type 'C' and 'method group'
                //         _ = c + Main;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "c + Main").WithArguments("+", "C", "method group").WithLocation(10, 13),
                // (11,13): error CS0034: Operator '+' is ambiguous on operands of type 'C' and 'lambda expression'
                //         _ = c + (() => 1);
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "c + (() => 1)").WithArguments("+", "C", "lambda expression").WithLocation(11, 13)
            };
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void BinaryOperator_03()
        {
            var source =
@"using System;
class C
{
    public static C operator+(C c, Delegate d) { Console.WriteLine(""operator+(C c, Delegate d)""); return c; }
    public static C operator+(C c, object o) { Console.WriteLine(""operator+(C c, object o)""); return c; }
    static int F() => 0;
    static void Main()
    {
        var c = new C();
        _ = c + F;
        _ = c + (() => 1);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (10,13): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'method group'
                //         _ = c + F;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c + F").WithArguments("+", "C", "method group").WithLocation(10, 13),
                // (11,13): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'lambda expression'
                //         _ = c + (() => 1);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c + (() => 1)").WithArguments("+", "C", "lambda expression").WithLocation(11, 13));

            var expectedOutput =
@"operator+(C c, Delegate d)
operator+(C c, Delegate d)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void BinaryOperator_04()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C
{
    public static C operator+(C c, Expression e) { Console.WriteLine(""operator+(C c, Expression e)""); return c; }
    public static C operator+(C c, object o) { Console.WriteLine(""operator+(C c, object o)""); return c; }
    static int F() => 0;
    static void Main()
    {
        var c = new C();
        _ = c + F;
        _ = c + (() => 1);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (11,13): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'method group'
                //         _ = c + F;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c + F").WithArguments("+", "C", "method group").WithLocation(11, 13),
                // (12,13): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'lambda expression'
                //         _ = c + (() => 1);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c + (() => 1)").WithArguments("+", "C", "lambda expression").WithLocation(12, 13));

            var expectedDiagnostics = new[]
            {
                // (11,17): error CS0428: Cannot convert method group 'F' to non-delegate type 'Expression'. Did you intend to invoke the method?
                //         _ = c + F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "System.Linq.Expressions.Expression").WithLocation(11, 17)
            };
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void BinaryOperator_05()
        {
            var source =
@"using System;
class C
{
    public static C operator+(C c, Delegate d) { Console.WriteLine(""operator+(C c, Delegate d)""); return c; }
    public static C operator+(C c, Func<object> f) { Console.WriteLine(""operator+(C c, Func<object> f)""); return c; }
    static int F() => 0;
    static void Main()
    {
        var c = new C();
        _ = c + F;
        _ = c + (() => 1);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (10,13): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'method group'
                //         _ = c + F;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c + F").WithArguments("+", "C", "method group").WithLocation(10, 13));

            var expectedOutput =
@"operator+(C c, Delegate d)
operator+(C c, Func<object> f)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void BinaryOperator_06()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C
{
    public static C operator+(C c, Expression e) { Console.WriteLine(""operator+(C c, Expression e)""); return c; }
    public static C operator+(C c, Func<object> f) { Console.WriteLine(""operator+(C c, Func<object> f)""); return c; }
    static void Main()
    {
        var c = new C();
        _ = c + (() => new object());
        _ = c + (() => 1);
    }
}";

            var expectedOutput =
@"operator+(C c, Func<object> f)
operator+(C c, Func<object> f)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void BinaryOperator_07()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C
{
    public static C operator+(C c, Expression e) { Console.WriteLine(""operator+(C c, Expression e)""); return c; }
    public static C operator+(C c, Func<object> f) { Console.WriteLine(""operator+(C c, Func<object> f)""); return c; }
    static int F() => 0;
    static void Main()
    {
        var c = new C();
        _ = c + F;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (11,13): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'method group'
                //         _ = c + F;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c + F").WithArguments("+", "C", "method group").WithLocation(11, 13));

            var expectedDiagnostics = new[]
            {
                // (11,17): error CS0428: Cannot convert method group 'F' to non-delegate type 'Expression'. Did you intend to invoke the method?
                //         _ = c + F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "System.Linq.Expressions.Expression").WithLocation(11, 17)
            };
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void BinaryOperator_08()
        {
            var source =
@"using System;
class A
{
    public static A operator+(A a, Func<int> f) { Console.WriteLine(""operator+(A a, Func<int> f)""); return a; }
}
class B : A
{
    public static B operator+(B b, Delegate d) { Console.WriteLine(""operator+(B b, Delegate d)""); return b; }
    static int F() => 1;
    static void Main()
    {
        var b = new B();
        _ = b + F;
        _ = b + (() => 2);
    }
}";

            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput:
@"operator+(A a, Func<int> f)
operator+(A a, Func<int> f)
");

            // Breaking change from C#9.
            string expectedOutput =
@"operator+(B b, Delegate d)
operator+(B b, Delegate d)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: expectedOutput);
            CompileAndVerify(source, expectedOutput: expectedOutput);
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
  IL_0000:  ldsfld     ""<anonymous delegate> Program.<>c__0<T>.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c__0<T> Program.<>c__0<T>.<>9""
  IL_000e:  ldftn      ""T Program.<>c__0<T>.<M1>b__0_0(ref T)""
  IL_0014:  newobj     ""<>F{00000001}<T, T>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""<anonymous delegate> Program.<>c__0<T>.<>9__0_0""
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
  IL_0000:  ldsfld     ""<anonymous delegate> Program.<>c__1<U>.<>9__1_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c__1<U> Program.<>c__1<U>.<>9""
  IL_000e:  ldftn      ""U Program.<>c__1<U>.<M2>b__1_0(ref U)""
  IL_0014:  newobj     ""<>F{00000001}<U, U>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""<anonymous delegate> Program.<>c__1<U>.<>9__1_0""
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
  IL_0000:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__2_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_000e:  ldftn      ""double Program.<>c.<M3>b__2_0(ref double)""
  IL_0014:  newobj     ""<>F{00000001}<double, double>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""<anonymous delegate> Program.<>c.<>9__2_0""
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
            VerifyLocalDelegateType(model, variables[0], "T <anonymous delegate>.Invoke(ref T arg)");
            VerifyLocalDelegateType(model, variables[1], "U <anonymous delegate>.Invoke(ref U arg)");
            VerifyLocalDelegateType(model, variables[2], "System.Double <anonymous delegate>.Invoke(ref System.Double arg)");

            var identifiers = nodes.OfType<InvocationExpressionSyntax>().Where(i => i.Expression is IdentifierNameSyntax id && id.Identifier.Text == "Report").Select(i => i.ArgumentList.Arguments[0].Expression).ToArray();
            Assert.Equal(3, identifiers.Length);
            VerifyExpressionType(model, identifiers[0], "<anonymous delegate> d", "T <anonymous delegate>.Invoke(ref T arg)");
            VerifyExpressionType(model, identifiers[1], "<anonymous delegate> d", "U <anonymous delegate>.Invoke(ref U arg)");
            VerifyExpressionType(model, identifiers[2], "<anonymous delegate> d", "System.Double <anonymous delegate>.Invoke(ref System.Double arg)");
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
            VerifyLocalDelegateType(model, variables[0], "ref System.Int32 <anonymous delegate>.Invoke()");
            VerifyLocalDelegateType(model, variables[1], "ref System.Single <anonymous delegate>.Invoke()");
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
<>A{00000008}`2[System.Int32,System.Int32]
<>A{00000001}`2[System.Single,System.Int32]
<>A{00000008}`2[System.Single,System.Int32]
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      145 (0x91)
  .maxstack  2
  IL_0000:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_000e:  ldftn      ""void Program.<>c.<Main>b__0_0(ref int, int)""
  IL_0014:  newobj     ""<>A{00000001}<int, int>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""<anonymous delegate> Program.<>c.<>9__0_0""
  IL_001f:  call       ""void Program.Report(System.Delegate)""
  IL_0024:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__0_1""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0032:  ldftn      ""void Program.<>c.<Main>b__0_1(int, ref int)""
  IL_0038:  newobj     ""<>A{00000008}<int, int>..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""<anonymous delegate> Program.<>c.<>9__0_1""
  IL_0043:  call       ""void Program.Report(System.Delegate)""
  IL_0048:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__0_2""
  IL_004d:  dup
  IL_004e:  brtrue.s   IL_0067
  IL_0050:  pop
  IL_0051:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0056:  ldftn      ""void Program.<>c.<Main>b__0_2(ref float, int)""
  IL_005c:  newobj     ""<>A{00000001}<float, int>..ctor(object, System.IntPtr)""
  IL_0061:  dup
  IL_0062:  stsfld     ""<anonymous delegate> Program.<>c.<>9__0_2""
  IL_0067:  call       ""void Program.Report(System.Delegate)""
  IL_006c:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__0_3""
  IL_0071:  dup
  IL_0072:  brtrue.s   IL_008b
  IL_0074:  pop
  IL_0075:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_007a:  ldftn      ""void Program.<>c.<Main>b__0_3(float, ref int)""
  IL_0080:  newobj     ""<>A{00000008}<float, int>..ctor(object, System.IntPtr)""
  IL_0085:  dup
  IL_0086:  stsfld     ""<anonymous delegate> Program.<>c.<>9__0_3""
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
  IL_0024:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__1_1""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0032:  ldftn      ""ref int Program.<>c.<Main>b__1_1()""
  IL_0038:  newobj     ""<>F{00000001}<int>..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""<anonymous delegate> Program.<>c.<>9__1_1""
  IL_0043:  call       ""void Program.Report(System.Delegate)""
  IL_0048:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__1_2""
  IL_004d:  dup
  IL_004e:  brtrue.s   IL_0067
  IL_0050:  pop
  IL_0051:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0056:  ldftn      ""ref readonly int Program.<>c.<Main>b__1_2()""
  IL_005c:  newobj     ""<>F{00000003}<int>..ctor(object, System.IntPtr)""
  IL_0061:  dup
  IL_0062:  stsfld     ""<anonymous delegate> Program.<>c.<>9__1_2""
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10, options: TestOptions.ReleaseExe);
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
<>F{00000009}`2[System.Int32,System.Int32]
<>F{00000019}`2[System.Int32,System.Int32]
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      109 (0x6d)
  .maxstack  2
  IL_0000:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_000e:  ldftn      ""int Program.<>c.<Main>b__0_0(ref int)""
  IL_0014:  newobj     ""<>F{00000001}<int, int>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""<anonymous delegate> Program.<>c.<>9__0_0""
  IL_001f:  call       ""void Program.Report(System.Delegate)""
  IL_0024:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__0_1""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0032:  ldftn      ""ref int Program.<>c.<Main>b__0_1(ref int)""
  IL_0038:  newobj     ""<>F{00000009}<int, int>..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""<anonymous delegate> Program.<>c.<>9__0_1""
  IL_0043:  call       ""void Program.Report(System.Delegate)""
  IL_0048:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__0_2""
  IL_004d:  dup
  IL_004e:  brtrue.s   IL_0067
  IL_0050:  pop
  IL_0051:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0056:  ldftn      ""ref readonly int Program.<>c.<Main>b__0_2(ref int)""
  IL_005c:  newobj     ""<>F{00000019}<int, int>..ctor(object, System.IntPtr)""
  IL_0061:  dup
  IL_0062:  stsfld     ""<anonymous delegate> Program.<>c.<>9__0_2""
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
  IL_0024:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__1_1""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0032:  ldftn      ""void Program.<>c.<Main>b__1_1(out int)""
  IL_0038:  newobj     ""<>A{00000002}<int>..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""<anonymous delegate> Program.<>c.<>9__1_1""
  IL_0043:  call       ""void Program.Report(System.Delegate)""
  IL_0048:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__1_2""
  IL_004d:  dup
  IL_004e:  brtrue.s   IL_0067
  IL_0050:  pop
  IL_0051:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0056:  ldftn      ""void Program.<>c.<Main>b__1_2(ref int)""
  IL_005c:  newobj     ""<>A{00000001}<int>..ctor(object, System.IntPtr)""
  IL_0061:  dup
  IL_0062:  stsfld     ""<anonymous delegate> Program.<>c.<>9__1_2""
  IL_0067:  call       ""void Program.Report(System.Delegate)""
  IL_006c:  ldsfld     ""<anonymous delegate> Program.<>c.<>9__1_3""
  IL_0071:  dup
  IL_0072:  brtrue.s   IL_008b
  IL_0074:  pop
  IL_0075:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_007a:  ldftn      ""void Program.<>c.<Main>b__1_3(in int)""
  IL_0080:  newobj     ""<>A{00000003}<int>..ctor(object, System.IntPtr)""
  IL_0085:  dup
  IL_0086:  stsfld     ""<anonymous delegate> Program.<>c.<>9__1_3""
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10, options: TestOptions.ReleaseExe);
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
@"using System;
class Program
{
    unsafe static void Main()
    {
        var d1 = int* () => { Console.WriteLine(1); return (int*)42; };
        var d2 = (int* p) => { Console.WriteLine((int)p); };
        var d3 = delegate*<void> () => { Console.WriteLine(3); return default; };
        var d4 = (delegate*<void> d) => { Console.WriteLine((int)d); };
        d1.Invoke();
        d2.Invoke((int*)2);
        d3.Invoke();
        d4.Invoke((delegate*<void>)4);
        Report(d1);
        Report(d2);
        Report(d3);
        Report(d4);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"1
2
3
4
<>f__AnonymousDelegate0
<>f__AnonymousDelegate1
<>f__AnonymousDelegate2
<>f__AnonymousDelegate3
");
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
        Report(d1);
        Report(d2);
        Report(d3);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"<>f__AnonymousDelegate0
<>f__AnonymousDelegate1
<>f__AnonymousDelegate2
");
        }

        [WorkItem(55217, "https://github.com/dotnet/roslyn/issues/55217")]
        [Fact]
        public void SynthesizedDelegateTypes_13()
        {
            var source =
@"using System;
ref struct S<T> { }
class Program
{
    static void F1(int x, S<int> y) { Console.WriteLine(x); }
    static S<T> F2<T>() { Console.WriteLine(typeof(T)); return default; }
    static void Main()
    {
        var d1 = F1;
        var d2 = F2<object>;
        d1.Invoke(0, default);
        d2.Invoke();
        Report(d1);
        Report(d2);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput:
@"0
System.Object
<>f__AnonymousDelegate0
<>f__AnonymousDelegate1
", verify: Verification.FailsILVerify with { ILVerifyMessage = "[F2]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x18 }" });

            verifier.VerifyIL("Program.F2<T>()", """
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (S<T> V_0)
  IL_0000:  ldtoken    "T"
  IL_0005:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
  IL_000a:  call       "void System.Console.WriteLine(object)"
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    "S<T>"
  IL_0017:  ldloc.0
  IL_0018:  ret
}
""");
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/68208")]
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

            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsCoreClr
            ? """
              System.Func`1[System.Int32&(System.Int32)[]]
              System.Action`1[System.Void(System.Int32, System.Int32&)[]]
              <>F{00000001}`2[System.Int32,System.Int32&(System.Int32)[]]
              <>A{00000001}`1[System.Void(System.Int32, System.Int32&)[]]
              System.Func`1[System.Int32&()[]]
              System.Action`1[System.Void(System.Int32&)[]]
              <>F{00000001}`2[System.Int32,System.Int32&()[]]
              <>A{00000001}`1[System.Void(System.Int32&)[]]
              """
            : """
              System.Func`1[(fnptr)[]]
              System.Action`1[(fnptr)[]]
              <>F{00000001}`2[System.Int32,(fnptr)[]]
              <>A{00000001}`1[(fnptr)[]]
              System.Func`1[(fnptr)[]]
              System.Action`1[(fnptr)[]]
              <>F{00000001}`2[System.Int32,(fnptr)[]]
              <>A{00000001}`1[(fnptr)[]]
              """);
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
<>A{00000008}`2[System.IntPtr,System.IntPtr]
<>A{00000001}`1[System.ValueTuple`2[System.Int32,System.Int32]]
<>A{00000008}`2[System.Object,System.Object[]]
");
        }

        [Fact]
        [WorkItem(55570, "https://github.com/dotnet/roslyn/issues/55570")]
        public void SynthesizedDelegateTypes_18()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report((int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17) => { return 1; });
        Report((int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, out int _17) => { _17 = 0; return 2; });
        Report((int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, ref int _17) => { return 3; });
        Report((int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, in int _17) => { return 4; });
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"<>F`18[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Int32]
<>F{2000000000000}`18[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Int32]
<>F{1000000000000}`18[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Int32]
<>F{3000000000000}`18[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Int32]
");
        }

        [Fact]
        [WorkItem(55570, "https://github.com/dotnet/roslyn/issues/55570")]
        public void SynthesizedDelegateTypes_19()
        {
            var source =
@"using System;
class Program
{
    static void F1(ref int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17, object _18) { }
    static void F2(ref int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17, out object _18) { _18 = null; }
    static void F3(ref int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17, ref object _18) { }
    static void F4(ref int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16, int _17, in object _18) { }
    static void Main()
    {
        Report(F1);
        Report(F2);
        Report(F3);
        Report(F4);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"<>A{00000001}`18[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object]
<>A{10000000000001}`18[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object]
<>A{8000000000001}`18[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object]
<>A{18000000000001}`18[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object]
");
        }

        [Fact]
        [WorkItem(55570, "https://github.com/dotnet/roslyn/issues/55570")]
        public void SynthesizedDelegateTypes_20()
        {
            var source =
@"using System;
class Program
{
    static void F1(
        int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16,
        int _17, object _18, int _19, object _20, int _21, object _22, int _23, object _24, int _25, object _26, int _27, object _28, int _29, object _30, int _31, ref object _32, int _33) { }
    static void F2(
        int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16,
        int _17, object _18, int _19, object _20, int _21, object _22, int _23, object _24, int _25, object _26, int _27, object _28, int _29, object _30, int _31, ref object _32, out int _33) { _33 = 0; }
    static void F3(
        int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16,
        int _17, object _18, int _19, object _20, int _21, object _22, int _23, object _24, int _25, object _26, int _27, object _28, int _29, object _30, int _31, ref object _32, ref int _33) { }
    static void F4(
        int _1, object _2, int _3, object _4, int _5, object _6, int _7, object _8, int _9, object _10, int _11, object _12, int _13, object _14, int _15, object _16,
        int _17, object _18, int _19, object _20, int _21, object _22, int _23, object _24, int _25, object _26, int _27, object _28, int _29, object _30, int _31, ref object _32, in int _33) { }
    static void Main()
    {
        Report(F1);
        Report(F2);
        Report(F3);
        Report(F4);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"<>A{00000000\,20000000}`33[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32]
<>A{00000000\,220000000}`33[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32]
<>A{00000000\,120000000}`33[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32]
<>A{00000000\,320000000}`33[System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32,System.Object,System.Int32]
");
        }

        /// <summary>
        /// Synthesized delegate types should only be emitted if referenced in the assembly.
        /// </summary>
        [Fact]
        [WorkItem(55896, "https://github.com/dotnet/roslyn/issues/55896")]
        public void SynthesizedDelegateTypes_21()
        {
            var source =
@"using System;
delegate ref object D();
class Program
{
    static void M(Delegate d) { Report(d); }
    static void M(D d) { Report(d); }
    static ref object F() => throw null;
    static void Main()
    {
        M(F);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, validator: validator, expectedOutput: "D");

            static void validator(PEAssembly assembly)
            {
                var reader = assembly.GetMetadataReader();
                var actualTypes = reader.GetTypeDefNames().Select(h => reader.GetString(h)).ToArray();

                string[] expectedTypes = new[] { "<Module>", "D", "Program", };
                AssertEx.Equal(expectedTypes, actualTypes);
            }
        }

        /// <summary>
        /// Synthesized delegate types should only be emitted if referenced in the assembly.
        /// </summary>
        [Fact]
        [WorkItem(55896, "https://github.com/dotnet/roslyn/issues/55896")]
        public void SynthesizedDelegateTypes_22()
        {
            var source =
@"using System;
delegate void D2(object x, ref object y);
delegate void D4(out object x, ref object y);
class Program
{
    static void M(Delegate d) { Report(d); }
    static void M(D2 d) { Report(d); }
    static void M(D4 d) { Report(d); }
    static void F1(ref object x, object y) { }
    static void F2(object x, ref object y) { }
    static void Main()
    {
        M(F1);
        M(F2);
        M((ref object x, out object y) => { y = null; });
        M((out object x, ref object y) => { x = null; });
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, validator: validator, expectedOutput:
@"<>A{00000001}`2[System.Object,System.Object]
D2
<>A{00000011}`2[System.Object,System.Object]
D4");

            static void validator(PEAssembly assembly)
            {
                var reader = assembly.GetMetadataReader();
                var actualTypes = reader.GetTypeDefNames().Select(h => reader.GetString(h)).ToArray();

                string[] expectedTypes = new[] { "<Module>", "<>A{00000001}`2", "<>A{00000011}`2", "D2", "D4", "Program", "<>c", };
                AssertEx.Equal(expectedTypes, actualTypes);
            }
        }

        /// <summary>
        /// Synthesized delegate types should only be emitted if referenced in the assembly.
        /// </summary>
        [Fact]
        [WorkItem(55896, "https://github.com/dotnet/roslyn/issues/55896")]
        public void SynthesizedDelegateTypes_23()
        {
            var source =
@"using System;
class Program
{
    static T M<T>(T t) => t;
    static int F(ref object x) => 1;
    static void Main()
    {
        M(() => { });
    }
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var syntaxTree = comp.SyntaxTrees[0];

            var model = comp.GetSemanticModel(syntaxTree);
            var syntax = syntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            int position = syntax.SpanStart;
            speculate(model, position, "M(F);", "<anonymous delegate>");
            speculate(model, position, "M((out object y) => { y = null; return 2; });", "<anonymous delegate>");

            var verifier = CompileAndVerify(comp, validator: validator);

            static void speculate(SemanticModel? model, int position, string text, string expectedDelegateType)
            {
                var stmt = SyntaxFactory.ParseStatement(text);
                Assert.True(model.TryGetSpeculativeSemanticModel(position, stmt, out model));
                var expr = ((ExpressionStatementSyntax)stmt).Expression;
                var type = model!.GetTypeInfo(expr).Type;
                Assert.Equal(expectedDelegateType, type.ToTestDisplayString());
            }

            static void validator(PEAssembly assembly)
            {
                var reader = assembly.GetMetadataReader();
                var actualTypes = reader.GetTypeDefNames().Select(h => reader.GetString(h)).ToArray();

                string[] expectedTypes = new[] { "<Module>", "EmbeddedAttribute", "RefSafetyRulesAttribute", "Program", "<>c", };
                AssertEx.Equal(expectedTypes, actualTypes);
            }
        }

        [Fact]
        public void SynthesizedDelegateTypes_24()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(A.F1());
        Report(A.F2<int>());
        Report(B<string>.F3());
        Report(A.F2<string>());
        Report(B<int>.F3());
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
class A
{
    internal unsafe static Delegate F1() => ref int () => throw null;
    internal unsafe static Delegate F2<T>() => ref int () => throw null;
}
class B<T>
{
    internal unsafe static Delegate F3() => ref int () => throw null;
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"<>F{00000001}`1[System.Int32]
<>F{00000001}`1[System.Int32]
<>F{00000001}`1[System.Int32]
<>F{00000001}`1[System.Int32]
<>F{00000001}`1[System.Int32]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_25()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Delegate d1 = A.F1();
        Delegate d2 = A.F2<int>();
        Delegate d3 = B<string>.F3();
        Delegate d4 = A.F2<string>();
        Delegate d5 = B<int>.F3();
        d1.DynamicInvoke();
        d2.DynamicInvoke();
        d3.DynamicInvoke();
        d4.DynamicInvoke();
        d5.DynamicInvoke();
        Report(d1);
        Report(d2);
        Report(d3);
        Report(d4);
        Report(d5);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
class A
{
    internal unsafe static Delegate F1() => int* () => { Console.WriteLine(nameof(F1)); return (int*)1; };
    internal unsafe static Delegate F2<T>() => int* () => { Console.WriteLine((nameof(F2), typeof(T))); return (int*)2; };
}
class B<T>
{
    internal unsafe static Delegate F3() => int* () => { Console.WriteLine((nameof(F3), typeof(T))); return (int*)3; };
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"F1
(F2, System.Int32)
(F3, System.String)
(F2, System.String)
(F3, System.Int32)
<>f__AnonymousDelegate0
<>f__AnonymousDelegate0
<>f__AnonymousDelegate0
<>f__AnonymousDelegate0
<>f__AnonymousDelegate0
");
        }

        [WorkItem(55217, "https://github.com/dotnet/roslyn/issues/55217")]
        [Fact]
        public void SynthesizedDelegateTypes_26()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Span<int> s = stackalloc int[] { 1, 2, 3 };
        var d = (Span<int> s) => { Console.WriteLine(s.Length); };
        d.Invoke(s);
        Console.WriteLine(d.GetType());
    }
}";
            var comp = CreateCompilationWithSpan(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"3
<>f__AnonymousDelegate0
");
        }

        [WorkItem(55217, "https://github.com/dotnet/roslyn/issues/55217")]
        [Fact]
        public void SynthesizedDelegateTypes_27()
        {
            var source =
@"#nullable enable
class Program
{
    unsafe static void Main()
    {
        object o = null; // 1
        var d = (int* p) => o;
        d((int*)42).ToString(); // 2
    }
}";
            // Should report warning 2.
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics(
                // (6,20): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         object o = null; // 1
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(6, 20));
        }

        [WorkItem(55217, "https://github.com/dotnet/roslyn/issues/55217")]
        [Fact]
        public void SynthesizedDelegateTypes_28()
        {
            var sourceA =
@"using System;
class A
{
    static void Main()
    {
        B.M();
        C.M();
    }
    internal static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";
            var sourceB =
@"using System;
class B
{
    internal unsafe static void M()
    {
        int* x = (int*)42;
        var d1 = (int y) => { Console.WriteLine((1, y)); return x; };
        var d2 = (ref int y) => { Console.WriteLine((2, y)); return x; };
        var d3 = (out int y) => { Console.WriteLine(3); y = 0; return x; };
        var d4 = (in int y) => { Console.WriteLine((4, y)); return x; };
        int i = 42;
        d1.Invoke(i);
        d2.Invoke(ref i);
        d3.Invoke(out i);
        d4.Invoke(in i);
        A.Report(d1);
        A.Report(d2);
        A.Report(d3);
        A.Report(d4);
    }
}";
            var sourceC =
@"using System;
class C
{
    internal unsafe static void M()
    {
        int* p = (int*)42;
        var d1 = (in int i) => { Console.WriteLine((1, i)); return p; };
        var d2 = (out int i) => { Console.WriteLine(2); i = 0; return p; };
        var d3 = (ref int i) => { Console.WriteLine((3, i)); return p; };
        var d4 = (int i) => { Console.WriteLine((4, i)); return p; };
        int i = 42;
        d1.Invoke(in i);
        d2.Invoke(out i);
        d3.Invoke(ref i);
        d4.Invoke(i);
        A.Report(d1);
        A.Report(d2);
        A.Report(d3);
        A.Report(d4);
    }
}";
            CompileAndVerify(new[] { sourceA, sourceB, sourceC }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"(1, 42)
(2, 42)
3
(4, 0)
<>f__AnonymousDelegate0
<>f__AnonymousDelegate1
<>f__AnonymousDelegate2
<>f__AnonymousDelegate3
(1, 42)
2
(3, 0)
(4, 0)
<>f__AnonymousDelegate3
<>f__AnonymousDelegate2
<>f__AnonymousDelegate1
<>f__AnonymousDelegate0
");
        }

        [WorkItem(55217, "https://github.com/dotnet/roslyn/issues/55217")]
        [Fact]
        public void SynthesizedDelegateTypes_29()
        {
            var sourceA =
@"using System;
class A
{
    static void Main()
    {
        B.M();
        C.M();
    }
    internal static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";
            var sourceB =
@"class B
{
    internal unsafe static void M()
    {
        A.Report((int* x, int y) => { });
        A.Report((short* x, int y) => { });
        A.Report((int* x, object y) => { });
    }
}";
            var sourceC =
@"class C
{
    internal unsafe static void M()
    {
        A.Report((int* x, string y) => { });
        A.Report((short* x, int y) => { });
        A.Report((char* x, char y) => { });
        A.Report((int* x, int y) => { });
        A.Report((int* x, object y) => { });
    }
}";
            CompileAndVerify(new[] { sourceA, sourceB, sourceC }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"<>f__AnonymousDelegate0
<>f__AnonymousDelegate1
<>f__AnonymousDelegate2
<>f__AnonymousDelegate3
<>f__AnonymousDelegate1
<>f__AnonymousDelegate4
<>f__AnonymousDelegate0
<>f__AnonymousDelegate2
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_30()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Delegate d = F<string>();
        Console.WriteLine(d.GetType());
    }
    unsafe static Delegate F<T>()
    {
        return Local<int>();
        static Delegate Local<U>() where U : unmanaged
        {
            var d = (T t, U* u) => { Console.WriteLine((t, (int)u)); };
            d.Invoke(default, (U*)42);
            return d;
        }
    }
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"(, 42)
<>f__AnonymousDelegate0`2[System.String,System.Int32]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_31()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(C<string>.F());
        Report(C<float>.F());
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
unsafe class C<T>
{
    internal static Func<Delegate> F = () =>
    {
        return Local1<int>();
        static Delegate Local1<U>() where U : unmanaged
        {
            return Local2<double>();
            static Delegate Local2<V>() where V : struct
            {
                var d = U* (T t) => { Console.WriteLine((typeof(T), typeof(U))); return (U*)42; };
                d.Invoke(default);
                return d;
            }
        }
    };
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@" (System.String, System.Int32)
<>f__AnonymousDelegate0`2[System.String,System.Int32]
(System.Single, System.Int32)
<>f__AnonymousDelegate0`2[System.Single,System.Int32]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_32()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(S<int>.F);
        Report(S<char>.P);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
unsafe struct S<T> where T : unmanaged
{
    internal static Delegate F = T* () => (T*)1;
    internal static Delegate P => S<T>* () => (S<T>*)2;
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"<>f__AnonymousDelegate0`1[System.Int32]
<>f__AnonymousDelegate1`1[System.Char]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_33()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(A.F1<int, string>());
        Report(A.F1<string, int>());
        Report(A.B<int, string>.F2());
        Report(A.B<string, int>.F2());
        Report(S<int>.C.F3<string>());
        Report(S<string>.C.F3<int>());
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
class A
{
    internal unsafe static Delegate F1<T, U>()
    {
        var d = int (int* x, T[] y, S<U> z) => { Console.WriteLine(((int)x, typeof(T), typeof(U))); return 0; };
        d.Invoke((int*)1, null, default);
        return d;
    }
    internal struct B<T, U>
    {
        internal unsafe static Delegate F2()
        {
            var d = int (int* x, T[] y, S<U> z) => { Console.WriteLine(((int)x, typeof(T), typeof(U))); return 0; };
            d.Invoke((int*)2, null, default);
            return d;
        }
    }
}
struct S<U>
{
    internal class C
    {
        internal unsafe static Delegate F3<T>()
        {
            var d = int (int* x, T[] y, S<U> z) => { Console.WriteLine(((int)x, typeof(T), typeof(U))); return 0; };
            d.Invoke((int*)3, null, default);
            return d;
        }
    }
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"(1, System.Int32, System.String)
<>f__AnonymousDelegate0`2[System.Int32,System.String]
(1, System.String, System.Int32)
<>f__AnonymousDelegate0`2[System.String,System.Int32]
(2, System.Int32, System.String)
<>f__AnonymousDelegate0`2[System.Int32,System.String]
(2, System.String, System.Int32)
<>f__AnonymousDelegate0`2[System.String,System.Int32]
(3, System.String, System.Int32)
<>f__AnonymousDelegate1`2[System.Int32,System.String]
(3, System.Int32, System.String)
<>f__AnonymousDelegate1`2[System.String,System.Int32]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_34()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(A<int>.B<string>.M1<object>());
        Report(A<int>.M2());
        Report(A<int>.M3<string>());
        Report(A<int>.M4<string, object>());
        Report(A<string>.B<object>.M1<int>());
        Report(A<string>.M2());
        Report(A<string>.M3<object>());
        Report(A<string>.M4<object, int>());
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
struct A<T>
{
    internal class B<U>
    {
        internal unsafe static Delegate M1<V>()
        {
            var d = void (int* x) => { Console.WriteLine(((int)x, typeof(T), typeof(U), typeof(V))); };
            d.Invoke((int*)1);
            return d;
        }
    }
    internal unsafe static Delegate M2()
    {
        var d = void (int* x) => { Console.WriteLine(((int)x, typeof(T))); };
        d.Invoke((int*)2);
        return d;
    }
    internal unsafe static Delegate M3<U>()
    {
        var d = void (int* x) => { Console.WriteLine(((int)x, typeof(T), typeof(U))); };
        d.Invoke((int*)3);
        return d;
    }
    internal unsafe static Delegate M4<U, V>()
    {
        var d = void (int* x) => { Console.WriteLine(((int)x, typeof(T), typeof(U), typeof(V))); };
        d.Invoke((int*)4);
        return d;
    }
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"(1, System.Int32, System.String, System.Object)
<>f__AnonymousDelegate0
(2, System.Int32)
<>f__AnonymousDelegate0
(3, System.Int32, System.String)
<>f__AnonymousDelegate0
(4, System.Int32, System.String, System.Object)
<>f__AnonymousDelegate0
(1, System.String, System.Object, System.Int32)
<>f__AnonymousDelegate0
(2, System.String)
<>f__AnonymousDelegate0
(3, System.String, System.Object)
<>f__AnonymousDelegate0
(4, System.String, System.Object, System.Int32)
<>f__AnonymousDelegate0
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_35()
        {
            var sourceA =
@".class public A`1<T>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .method public static void F1(int32* modopt(class A`1<!T>) i) { ret }
  .method public static int32* F2<U>(!T modopt(class A`1<!!U>) t) { ldnull throw }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using System;
class B1<T> : A<T>
{
    unsafe public static Delegate F()
    {
        return F1;
    }
}
class B2 : A<string>
{
    unsafe public static Delegate F<T>()
    {
        return F2<T>;
    }
}
class Program
{
    unsafe static void Main()
    {
        var d1 = B1<double>.F();
        Report(d1);
        var d2 = B2.F<double>();
        Report(d2);
    }
    static void Report(Delegate d)
    {
        var t = d.GetType();
        Console.WriteLine(t);
    }
}";
            CompileAndVerify(sourceB, references: new[] { refA }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"<>f__AnonymousDelegate0`1[System.Double]
<>f__AnonymousDelegate1`1[System.Double]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_36()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(S<int>.F1<string>());
        Report(S<int>.F2<string>());
        Report(S<int>.F3<string>());
        Report(S<int>.F4<string>());
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
struct S<T>
{
    internal unsafe static Delegate F1<U>()
    {
        var d1 = (ref int x) => (ref int y) => { Console.WriteLine((y, typeof(T), typeof(U))); return default(T); };
        int x = 1;
        int y = 2;
        d1.Invoke(ref x).Invoke(ref y);
        return d1;
    }
    internal unsafe static Delegate F2<U>()
    {
        var d1 = (ref int x) => (int* y) => { Console.WriteLine(((int)y, typeof(T), typeof(U))); return default(T); };
        int x = 3;
        int* y = (int*)4;
        d1.Invoke(ref x).Invoke(y);
        return d1;
    }
    internal unsafe static Delegate F3<U>()
    {
        var d1 = (int* x) => (ref int y) => { Console.WriteLine(((int)x, y, typeof(T), typeof(U))); return default(U); };
        int* x = (int*)5;
        int y = 6;
        d1.Invoke(x).Invoke(ref y);
        return d1;
    }
    internal unsafe static Delegate F4<U>()
    {
        var d1 = (int* x) => (int* y) => { Console.WriteLine(((int)x, (int)y, typeof(T), typeof(U))); return default(U); };
        int* x = (int*)7;
        int* y = (int*)8;
        d1.Invoke(x).Invoke(y);
        return d1;
    }
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"(2, System.Int32, System.String)
<>F{00000001}`2[System.Int32,<>F{00000001}`2[System.Int32,System.Int32]]
(4, System.Int32, System.String)
<>F{00000001}`2[System.Int32,<>f__AnonymousDelegate0`1[System.Int32]]
(5, 6, System.Int32, System.String)
<>f__AnonymousDelegate1`1[System.String]
(7, 8, System.Int32, System.String)
<>f__AnonymousDelegate2`1[System.String]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_37()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(S<int>.F1<string>());
        Report(S<int>.F2<string>());
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
struct S<T>
{
    internal unsafe static Delegate F1<U>()
    {
        var d = (int* x) => { Console.WriteLine(((int)x, typeof(T), typeof(U))); return new { A = default(T) }; };
        d.Invoke((int*)1);
        return d;
    }
    internal unsafe static Delegate F2<U>()
    {
        var d = (int* x) => { Console.WriteLine(((int)x, typeof(T), typeof(U))); return new { B = default(U) }; };
        d.Invoke((int*)2);
        return d;
    }
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"(1, System.Int32, System.String)
<>f__AnonymousDelegate0`1[System.Int32]
(2, System.Int32, System.String)
<>f__AnonymousDelegate1`1[System.String]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_38()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        F1();
        F2();
    }
    static void F1()
    {
        int i;
        var d1 = (ref int i) => { Console.WriteLine(i); };
        i = 1;
        d1(ref i);
        Report(d1);
        var d2 = (scoped ref int i) => { Console.WriteLine(i); };
        i = 2;
        d2(ref i);
        Report(d2);
        var d3 = (in int i) => { Console.WriteLine(i); };
        i = 3;
        d3(i);
        Report(d3);
        var d4 = (scoped in int i) => { Console.WriteLine(i); };
        i = 4;
        d4(i);
        Report(d4);
        var d5 = (out int i) => { i = 5; Console.WriteLine(i); };
        d5(out i);
        Report(d5);
        var d6 = (scoped out int i) => { i = 6; Console.WriteLine(i); };
        d6(out i);
        Report(d6);
    }
    static void F2()
    {
        byte b;
        var d7 = (ref byte b) => { Console.WriteLine(b); };
        b = 7;
        d7(ref b);
        Report(d7);
        var d8 = (scoped ref byte b) => { Console.WriteLine(b); };
        b = 8;
        d8(ref b);
        Report(d8);
        var d9 = (in byte b) => { Console.WriteLine(b); };
        b = 9;
        d9(b);
        Report(d9);
        var d10 = (scoped in byte b) => { Console.WriteLine(b); };
        b = 10;
        d10(b);
        Report(d10);
        var d11 = (out byte b) => { b = 11; Console.WriteLine(b); };
        d11(out b);
        Report(d11);
        var d12 = (scoped out byte b) => { b = 12; Console.WriteLine(b); };
        d12(out b);
        Report(d12);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";
            CompileAndVerify(source, verify: Verification.Skipped, expectedOutput:
@"1
<>A{00000001}`1[System.Int32]
2
<>f__AnonymousDelegate0
3
<>A{00000003}`1[System.Int32]
4
<>f__AnonymousDelegate1
5
<>A{00000002}`1[System.Int32]
6
<>A{00000002}`1[System.Int32]
7
<>A{00000001}`1[System.Byte]
8
<>f__AnonymousDelegate2
9
<>A{00000003}`1[System.Byte]
10
<>f__AnonymousDelegate3
11
<>A{00000002}`1[System.Byte]
12
<>A{00000002}`1[System.Byte]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_39()
        {
            var source =
@"using System;
ref struct R1
{
    public int _i;
    public R1(int i) { _i = i; }
}
ref struct R2
{
    public byte _b;
    public R2(byte b) { _b = b; }
}
class Program
{
    static void Main()
    {
        var d1 = (R1 r) => { Console.WriteLine(r._i); return new R1(); };
        d1(new R1(1));
        Report(d1);
        var d2 = (scoped R1 r) => { Console.WriteLine(r._i); return new R1(); };
        d2(new R1(2));
        Report(d2);
        var d3 = (R2 r) => { Console.WriteLine(r._b); return new R2(); };
        d3(new R2(3));
        Report(d3);
        var d4 = (scoped R1 r) => { Console.WriteLine(r._i); return new R1(); };
        d4(new R1(4));
        Report(d4);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";
            CompileAndVerify(source, verify: Verification.Skipped, expectedOutput:
@"1
<>f__AnonymousDelegate0
2
<>f__AnonymousDelegate1
3
<>f__AnonymousDelegate2
4
<>f__AnonymousDelegate1
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_40()
        {
            var source =
@"using System;
ref struct R
{
    public int _i;
    public R(int i) { _i = i; }
}
class Program
{
    static R F1(ref R r) { Console.WriteLine(r._i); return new R(); }
    static R F2(scoped ref R r) { Console.WriteLine(r._i); return new R(); }
    static R F3(in R r) { Console.WriteLine(r._i); return new R(); }
    static R F4(scoped in R r) { Console.WriteLine(r._i); return new R(); }
    static R F5(out R r) { r = new R(-5); Console.WriteLine(r._i); return new R(); }
    static R F6(scoped out R r) { r = new R(-6); Console.WriteLine(r._i); return new R(); }
    static void Main()
    {
        var d1 = F1;
        var r1 = new R(1);
        d1(ref r1);
        Report(d1);
        var d2 = F2;
        var r2 = new R(2);
        d2(ref r2);
        Report(d2);
        var d3 = F3;
        var r3 = new R(3);
        d3(r3);
        Report(d3);
        var d4 = F4;
        var r4 = new R(4);
        d4(r4);
        Report(d4);
        var d5 = F5;
        var r5 = new R(5);
        d5(out r5);
        Report(d5);
        var d6 = F6;
        var r6 = new R(6);
        d6(out r6);
        Report(d6);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}";
            CompileAndVerify(source, verify: Verification.Skipped, expectedOutput:
@"1
<>f__AnonymousDelegate0
2
<>f__AnonymousDelegate1
3
<>f__AnonymousDelegate2
4
<>f__AnonymousDelegate3
-5
<>f__AnonymousDelegate4
-6
<>f__AnonymousDelegate4
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_Constraints_01()
        {
            var source =
@"using System;
class A
{
    internal void F<T>(T? t) where T : struct
    {
        Console.WriteLine((typeof(T), t.HasValue ? t.Value.ToString() : ""<null>""));
    }
}
struct S
{
}
class Program
{
    static void Main()
    {
        var a = new A();
        Report(M(a, 1));
        Report(M(a, (short)2));
        Report(M(a, new S()));
    }
    static void Report(Delegate d)
    {
        Console.WriteLine(d.GetType());
    }
    unsafe static Delegate M<T, U>(T t, U u)
        where T : A
        where U : struct
    {
        var d = void (int* p, T t, U? u) =>
        {
            t.F(u);
        };
        d.Invoke((int*)0, t, u);
        d.Invoke((int*)1, t, default(U?));
        return d;
    }
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"(System.Int32, 1)
(System.Int32, <null>)
<>f__AnonymousDelegate0`2[A,System.Int32]
(System.Int16, 2)
(System.Int16, <null>)
<>f__AnonymousDelegate0`2[A,System.Int16]
(S, S)
(S, <null>)
<>f__AnonymousDelegate0`2[A,S]
");
        }

        [Fact]
        public void SynthesizedDelegateTypes_Constraints_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Report(M1(new object(), string.Empty));
        Report(M1(new object(), 1));
        Report(M2(2, new object()));
        Report(M2((short)3, (long)4));
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
    unsafe static Delegate M1<T, U>(T t, U u)
        where T : class
        where U : T
    {
        var d = void (int* x, T y, U z) => { Console.WriteLine(((int)x, y.GetType(), z.GetType())); };
        d.Invoke((int*)1, t, u);
        return d;
    }
    unsafe static Delegate M2<T, U>(T t, U u)
        where T : struct
    {
        var d = void (int* x, T y, U z) => { Console.WriteLine(((int)x, y.GetType(), z.GetType())); };
        d.Invoke((int*)2, t, u);
        return d;
    }
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput:
@"(1, System.Object, System.String)
<>f__AnonymousDelegate0`2[System.Object,System.String]
(1, System.Object, System.Int32)
<>f__AnonymousDelegate0`2[System.Object,System.Int32]
(2, System.Int32, System.Object)
<>f__AnonymousDelegate0`2[System.Int32,System.Object]
(2, System.Int16, System.Int64)
<>f__AnonymousDelegate0`2[System.Int16,System.Int64]
");
        }

        [Fact, WorkItem(64436, "https://github.com/dotnet/roslyn/issues/64436")]
        public void SynthesizedDelegateTypes_NamedArguments_Ref()
        {
            var source = """
                var lam1 = (ref int x) => { };
                void m1(ref int x) { }
                var inferred1 = m1;
                var lam2 = (int x, ref int y) => { };
                void m2(int x, ref int y) { }
                var inferred2 = m2;
                var lam3 = (in int x, int y) => { };
                void m3(in int x, int y) { }
                var inferred3 = m3;
                var lam4 = (out int x) => { x = 5; };
                void m4(out int x) { x = 5; }
                var inferred4 = m4;

                int i = 1;
                lam1(arg: ref i);
                inferred1(arg: ref i);
                lam2(arg1: 10, arg2: ref i);
                inferred2(arg1: 10, arg2: ref i);
                lam3(arg1: in i, arg2: 10);
                inferred3(arg1: in i, arg2: 10);
                lam4(arg: out i);
                inferred4(arg: out i);

                // Error cases:
                lam1();
                inferred1();
                lam2(10);
                inferred2(10);
                lam3(arg2: 100);
                inferred3(arg2: 100);
                lam4(arg: i);
                inferred4(arg: i);
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (25,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of '<anonymous delegate>'
                // lam1();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "lam1").WithArguments("arg", "<anonymous delegate>").WithLocation(25, 1),
                // (26,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of '<anonymous delegate>'
                // inferred1();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "inferred1").WithArguments("arg", "<anonymous delegate>").WithLocation(26, 1),
                // (27,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg2' of '<anonymous delegate>'
                // lam2(10);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "lam2").WithArguments("arg2", "<anonymous delegate>").WithLocation(27, 1),
                // (28,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg2' of '<anonymous delegate>'
                // inferred2(10);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "inferred2").WithArguments("arg2", "<anonymous delegate>").WithLocation(28, 1),
                // (29,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg1' of '<anonymous delegate>'
                // lam3(arg2: 100);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "lam3").WithArguments("arg1", "<anonymous delegate>").WithLocation(29, 1),
                // (30,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg1' of '<anonymous delegate>'
                // inferred3(arg2: 100);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "inferred3").WithArguments("arg1", "<anonymous delegate>").WithLocation(30, 1),
                // (31,11): error CS1620: Argument 1 must be passed with the 'out' keyword
                // lam4(arg: i);
                Diagnostic(ErrorCode.ERR_BadArgRef, "i").WithArguments("1", "out").WithLocation(31, 11),
                // (32,16): error CS1620: Argument 1 must be passed with the 'out' keyword
                // inferred4(arg: i);
                Diagnostic(ErrorCode.ERR_BadArgRef, "i").WithArguments("1", "out").WithLocation(32, 16));
        }

        [Fact, WorkItem(64436, "https://github.com/dotnet/roslyn/issues/64436")]
        public void SynthesizedDelegateTypes_NamedArguments_Pointer()
        {
            var source = """
                unsafe
                {
                    var lam1 = (int* x) => { };
                    void m1(int* x) { }
                    var inferred1 = m1;
                    var lam2 = (int x, int* y, int z) => { };
                    void m2(int x, int* y, int z) { }
                    var inferred2 = m2;

                    lam1(arg: null);
                    inferred1(arg: null);
                    lam2(arg1: 10, arg2: null, arg3: 100);
                    inferred2(arg1: 10, arg2: null, arg3: 100);

                    // Error cases:
                    lam1();
                    inferred1();
                    lam2(10);
                    inferred2(10);
                    lam2(10, arg3: 100);
                    inferred2(10, arg3: 100);
                }
                """;
            CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
                // (16,5): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of '<anonymous delegate>'
                //     lam1();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "lam1").WithArguments("arg", "<anonymous delegate>").WithLocation(16, 5),
                // (17,5): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of '<anonymous delegate>'
                //     inferred1();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "inferred1").WithArguments("arg", "<anonymous delegate>").WithLocation(17, 5),
                // (18,5): error CS7036: There is no argument given that corresponds to the required parameter 'arg2' of '<anonymous delegate>'
                //     lam2(10);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "lam2").WithArguments("arg2", "<anonymous delegate>").WithLocation(18, 5),
                // (19,5): error CS7036: There is no argument given that corresponds to the required parameter 'arg2' of '<anonymous delegate>'
                //     inferred2(10);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "inferred2").WithArguments("arg2", "<anonymous delegate>").WithLocation(19, 5),
                // (20,5): error CS7036: There is no argument given that corresponds to the required parameter 'arg2' of '<anonymous delegate>'
                //     lam2(10, arg3: 100);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "lam2").WithArguments("arg2", "<anonymous delegate>").WithLocation(20, 5),
                // (21,5): error CS7036: There is no argument given that corresponds to the required parameter 'arg2' of '<anonymous delegate>'
                //     inferred2(10, arg3: 100);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "inferred2").WithArguments("arg2", "<anonymous delegate>").WithLocation(21, 5));
        }

        [Fact, WorkItem(64436, "https://github.com/dotnet/roslyn/issues/64436")]
        public void SynthesizedDelegateTypes_NamedArguments_MoreThan16Parameters()
        {
            var range = Enumerable.Range(1, 17);
            var manyParams = string.Join(", ", range.Select(i => $"int p{i}"));
            var manyArgs = string.Join(", ", range.Select(i => $"arg{i}: {i * 10}"));
            var manyTypes = string.Join(",", range.Select(_ => "System.Int32"));
            var source = $$"""
                using System;

                var lam = ({{manyParams}}) => { };
                void method({{manyParams}}) { }
                var inferred = method;
                lam({{manyArgs}});
                inferred({{manyArgs}});
                Report(lam);
                Report(inferred);
                
                static void Report(Delegate d) => Console.WriteLine(d.GetType());
                """;
            CompileAndVerify(source, expectedOutput: $"""
                <>A`17[{manyTypes}]
                <>A`17[{manyTypes}]
                """).VerifyDiagnostics();

            var fewArgs = string.Join(", ", range.Skip(1).Select(i => $"arg{i}: {i * 10}"));
            source = $$"""
                var lam = ({{manyParams}}) => { };
                void method({{manyParams}}) { }
                var inferred = method;
                lam({{fewArgs}});
                inferred({{fewArgs}});
                lam();
                inferred();
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (4,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg1' of '<anonymous delegate>'
                // lam(arg2: 20, arg3: 30, arg4: 40, arg5: 50, arg6: 60, arg7: 70, arg8: 80, arg9: 90, arg10: 100, arg11: 110, arg12: 120, arg13: 130, arg14: 140, arg15: 150, arg16: 160, arg17: 170);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "lam").WithArguments("arg1", "<anonymous delegate>").WithLocation(4, 1),
                // (5,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg1' of '<anonymous delegate>'
                // inferred(arg2: 20, arg3: 30, arg4: 40, arg5: 50, arg6: 60, arg7: 70, arg8: 80, arg9: 90, arg10: 100, arg11: 110, arg12: 120, arg13: 130, arg14: 140, arg15: 150, arg16: 160, arg17: 170);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "inferred").WithArguments("arg1", "<anonymous delegate>").WithLocation(5, 1),
                // (6,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg1' of '<anonymous delegate>'
                // lam();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "lam").WithArguments("arg1", "<anonymous delegate>").WithLocation(6, 1),
                // (7,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg1' of '<anonymous delegate>'
                // inferred();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "inferred").WithArguments("arg1", "<anonymous delegate>").WithLocation(7, 1));
        }

        [Fact]
        public void SynthesizedDelegateTypes_MoreThan16Parameters_DefaultParameterValue()
        {
            var range = Enumerable.Range(1, 17);
            var manyParams = string.Join(", ", range.Select(i => $"int p{i}"));
            var manyTypes = string.Join(",", range.Select(_ => "System.Int32"));
            var source = $$"""
                using System;
                static void Report(Delegate d) => Console.WriteLine(d.GetType());
                var lam1 = ({{manyParams}} = 1) => { };
                Report(lam1);
                var lam2 = ({{manyParams}} = 1) => { };
                Report(lam2);
                var lam3 = ({{manyParams}} = 2) => { };
                Report(lam3);
                """;
            CompileAndVerify(source, expectedOutput: $"""
                <>f__AnonymousDelegate0`17[{manyTypes}]
                <>f__AnonymousDelegate0`17[{manyTypes}]
                <>f__AnonymousDelegate1`17[{manyTypes}]
                """).VerifyDiagnostics();
        }

        [Fact]
        public void SynthesizedDelegateTypes_MoreThan16Parameters_ParamsArray()
        {
            var range = Enumerable.Range(1, 16);
            var manyParams = string.Join(", ", range.Select(i => $"int p{i}"));
            var manyTypes = string.Join(",", range.Select(_ => "System.Int32"));
            var source = $$"""
                using System;
                static void Report(Delegate d) => Console.WriteLine(d.GetType());
                var lam1 = ({{manyParams}}, params int[] xs) => { };
                Report(lam1);
                var lam2 = ({{manyParams}}, params int[] xs) => { };
                Report(lam2);
                var lam3 = ({{manyParams}}, int[] xs) => { };
                Report(lam3);
                """;
            CompileAndVerify(source, expectedOutput: $"""
                <>f__AnonymousDelegate0`17[{manyTypes},System.Int32]
                <>f__AnonymousDelegate0`17[{manyTypes},System.Int32]
                <>A`17[{manyTypes},System.Int32[]]
                """).VerifyDiagnostics();
        }

        [Fact]
        public void SynthesizedDelegateTypes_Pointer_DefaultParameterValue()
        {
            var source = """
                using System;
                static void Report(Delegate d) => Console.WriteLine(d.GetType());
                unsafe
                {
                    var lam1 = (byte* a, int b = 1) => { };
                    Report(lam1);
                    var lam2 = (byte* x, int y = 1) => { };
                    Report(lam2);
                    var lam3 = (byte* a, int b = 2) => { };
                    Report(lam3);
                }
                """;
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, expectedOutput: $"""
                <>f__AnonymousDelegate0
                <>f__AnonymousDelegate0
                <>f__AnonymousDelegate1
                """).VerifyDiagnostics();
        }

        [Fact]
        public void SynthesizedDelegateTypes_Pointer_ParamsArray()
        {
            var source = """
                using System;
                static void Report(Delegate d) => Console.WriteLine(d.GetType());
                unsafe
                {
                    var lam1 = (byte* a, params int[] bs) => { };
                    Report(lam1);
                    var lam2 = (byte* x, params int[] ys) => { };
                    Report(lam2);
                    var lam3 = (byte* a, int[] bs) => { };
                    Report(lam3);
                }
                """;
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, expectedOutput: $"""
                <>f__AnonymousDelegate0
                <>f__AnonymousDelegate0
                <>f__AnonymousDelegate1
                """).VerifyDiagnostics();
        }

        [Fact]
        public void SynthesizedDelegateTypes_DefaultParameterValues_DefaultUnification()
        {
            var source = """
                using System;
                static void Report(Delegate d) => Console.WriteLine(d.GetType());
                var lam1 = (object o = null) => { };
                Report(lam1);
                var lam2 = (string s = null) => { };
                Report(lam2);
                var lam3 = (int? i = default) => { };
                Report(lam3);
                var lam4 = (S1 s1 = default) => { };
                Report(lam4);
                var lam5 = (S2 s2 = default) => { };
                Report(lam5);
                var lam6 = (C c = default) => { };
                Report(lam6);
                var lam7 = (I i = default) => { };
                Report(lam7);
                var lam8 = (int i = default) => { };
                Report(lam8);

                public struct S1 { public int X; }
                public struct S2 { public int Y; }
                public class C { }
                public interface I { }
                """;
            CompileAndVerify(source, expectedOutput: $"""
                <>f__AnonymousDelegate0`1[System.Object]
                <>f__AnonymousDelegate0`1[System.String]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.Int32]]
                <>f__AnonymousDelegate0`1[S1]
                <>f__AnonymousDelegate0`1[S2]
                <>f__AnonymousDelegate0`1[C]
                <>f__AnonymousDelegate0`1[I]
                <>f__AnonymousDelegate1`1[System.Int32]
                """).VerifyDiagnostics();
        }

        [Fact]
        public void SynthesizedDelegateTypes_DefaultParameterValues_IntNullableUnification()
        {
            var source = """
                using System;
                static void Report(Delegate d) => Console.WriteLine(d.GetType());
                var lam1 = (int a = 1) => { };
                Report(lam1);
                var lam2 = (int? b = 1) => { };
                Report(lam2);
                """;
            CompileAndVerify(source, expectedOutput: $"""
                <>f__AnonymousDelegate0`1[System.Int32]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.Int32]]
                """).VerifyDiagnostics();
        }

        [Fact]
        public void SynthesizedDelegateTypes_DefaultParameterValues_StringUnification()
        {
            var source = """
                using System;
                using System.Runtime.InteropServices;
                static void Report(Delegate d) => Console.WriteLine(d.GetType());
                var lam1 = (string a = "") => { };
                Report(lam1);
                var lam2 = (string b = null) => { };
                Report(lam2);
                var lam3 = (string c = "abc") => { };
                Report(lam3);
                var lam4 = (string d = "a" + "bc") => { };
                Report(lam4);
                var lam5 = ([Optional, DefaultParameterValue("abc")] object o) => { };
                Report(lam5);
                """;
            CompileAndVerify(source, expectedOutput: $"""
                <>f__AnonymousDelegate0`1[System.String]
                <>f__AnonymousDelegate1`1[System.String]
                <>f__AnonymousDelegate2`1[System.String]
                <>f__AnonymousDelegate2`1[System.String]
                <>f__AnonymousDelegate2`1[System.Object]
                """).VerifyDiagnostics();
        }

        [Fact]
        public void SynthesizedDelegateTypes_ParamsArray_DifferentElementTypes()
        {
            var source = """
                using System;
                static void Report(Delegate d) => Console.WriteLine(d.GetType());
                var lam1 = (params int[] xs) => { };
                Report(lam1);
                var lam2 = (params string[] ys) => { };
                Report(lam2);
                """;
            CompileAndVerify(source, expectedOutput: $"""
                <>f__AnonymousDelegate0`1[System.Int32]
                <>f__AnonymousDelegate0`1[System.String]
                """).VerifyDiagnostics();
        }

        [Fact]
        public void SynthesizedDelegateTypes_ParamsArray_NonArrayTypeInMetadata()
        {
            /*
                public static class C
                {
                    public static void M(params int x) { }
                }
             */
            var ilSource = """
                .class public auto ansi abstract sealed beforefieldinit C
                    extends [mscorlib]System.Object
                {
                    // Methods
                    .method public hidebysig static 
                        void M (
                            int32 x
                        ) cil managed 
                    {
                        .param [1]
                            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                                01 00 00 00
                            )
                        // Method begins at RVA 0x20a2
                        // Code size 2 (0x2)
                        .maxstack 8

                        IL_0000: nop
                        IL_0001: ret
                    } // end of method C::M

                } // end of class C
                """;
            var source = """
                var m = C.M;
                System.Console.WriteLine(m.GetType());
                """;
            var comp = CreateCompilationWithIL(source, ilSource).VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "System.Action`1[System.Int32]");
        }

        [Fact]
        public void SynthesizedDelegateTypes_ParamsArray_NotLastInMetadata()
        {
            /*
                public static class C
                {
                    public static void M(params int[] x, int y) { }
                }
             */
            var ilSource = """
                .class public auto ansi abstract sealed beforefieldinit C
                    extends [mscorlib]System.Object
                {
                    // Methods
                    .method public hidebysig static 
                        void M (
                            int32[] x,
                            int32 y
                        ) cil managed 
                    {
                        .param [1]
                            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                                01 00 00 00
                            )
                        // Method begins at RVA 0x20a2
                        // Code size 2 (0x2)
                        .maxstack 8

                        IL_0000: nop
                        IL_0001: ret
                    } // end of method C::M

                } // end of class C
                """;
            var source = """
                var m = C.M;
                System.Console.WriteLine(m.GetType());
                """;
            var comp = CreateCompilationWithIL(source, ilSource).VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "System.Action`2[System.Int32[],System.Int32]");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem(63565, "https://github.com/dotnet/roslyn/issues/63565")]
        public void SynthesizedDelegateTypes_UnscopedRefAttribute_01()
        {
            string source = """
                using System;
                using System.Diagnostics.CodeAnalysis;
                class Program
                {
                    static ref int F(ref int x, [UnscopedRef] ref int y) => ref x;
                    static void Main()
                    {
                        var d = F;
                        Report(d);
                    }
                    static void Report(Delegate d) => Console.WriteLine(d.GetType());
                }
                """;
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net70, verify: Verification.Skipped, expectedOutput:
                """
                <>f__AnonymousDelegate0
                """);
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0",
                """
                .class private auto ansi sealed '<>f__AnonymousDelegate0'
                    extends [System.Runtime]System.MulticastDelegate
                {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    // Methods
                    .method public hidebysig specialname rtspecialname 
                        instance void .ctor (
                            object 'object',
                            native int 'method'
                        ) runtime managed 
                    {
                    } // end of method '<>f__AnonymousDelegate0'::.ctor
                    .method public hidebysig newslot virtual 
                        instance int32& Invoke (
                            int32& arg1,
                            int32& arg2
                        ) runtime managed 
                    {
                        .param [2]
                            .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = (
                                01 00 00 00
                            )
                    } // end of method '<>f__AnonymousDelegate0'::Invoke
                } // end of class <>f__AnonymousDelegate0
                """);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem(63565, "https://github.com/dotnet/roslyn/issues/63565")]
        public void SynthesizedDelegateTypes_UnscopedRefAttribute_02()
        {
            string source = """
                using System;
                using System.Diagnostics.CodeAnalysis;
                class Program
                {
                    static void Main()
                    {
                        var d = ([UnscopedRef] ref int x, ref int y) => ref x;
                        Report(d);
                    }
                    static void Report(Delegate d) => Console.WriteLine(d.GetType());
                }
                """;
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net70, verify: Verification.Skipped, expectedOutput:
                """
                <>f__AnonymousDelegate0
                """);
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0",
                """
                .class private auto ansi sealed '<>f__AnonymousDelegate0'
                    extends [System.Runtime]System.MulticastDelegate
                {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                        01 00 00 00
                    )
                    // Methods
                    .method public hidebysig specialname rtspecialname 
                        instance void .ctor (
                            object 'object',
                            native int 'method'
                        ) runtime managed 
                    {
                    } // end of method '<>f__AnonymousDelegate0'::.ctor
                    .method public hidebysig newslot virtual 
                        instance int32& Invoke (
                            int32& arg1,
                            int32& arg2
                        ) runtime managed 
                    {
                        .param [1]
                            .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = (
                                01 00 00 00
                            )
                    } // end of method '<>f__AnonymousDelegate0'::Invoke
                } // end of class <>f__AnonymousDelegate0
                """);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem(63565, "https://github.com/dotnet/roslyn/issues/63565")]
        public void SynthesizedDelegateTypes_UnscopedRefAttribute_03()
        {
            string source = """
                using System;
                using System.Diagnostics.CodeAnalysis;
                class Program
                {
                    static ref int F1(ref int x, ref int y) => ref x;
                    static ref int F2(ref int x, [UnscopedRef] ref int y) => ref x;
                    static ref int F3([UnscopedRef] ref int x, ref int y) => ref x;
                    static void Main()
                    {
                        var d1 = F1;
                        var d2 = F2;
                        var d3 = F3;
                        var d4 = ([UnscopedRef] ref int x, ref int y) => ref x;
                        var d5 = (ref int x, [UnscopedRef] ref int y) => ref x;
                        var d6 = (ref int x, ref int y) => ref x;
                        Report(d1);
                        Report(d2);
                        Report(d3);
                        Report(d4);
                        Report(d5);
                        Report(d6);
                    }
                    static void Report(Delegate d) => Console.WriteLine(d.GetType());
                }
                """;
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net70, verify: Verification.Skipped, expectedOutput:
                """
                <>F{00000049}`3[System.Int32,System.Int32,System.Int32]
                <>f__AnonymousDelegate0
                <>f__AnonymousDelegate1
                <>f__AnonymousDelegate1
                <>f__AnonymousDelegate0
                <>F{00000049}`3[System.Int32,System.Int32,System.Int32]
                """);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem(63565, "https://github.com/dotnet/roslyn/issues/63565")]
        public void SynthesizedDelegateTypes_UnscopedRefAttribute_MissingType()
        {
            var attributeAssemblyName = GetUniqueName();
            var comp = CreateCompilation(UnscopedRefAttributeDefinition, targetFramework: TargetFramework.Net60, assemblyName: attributeAssemblyName);
            var refAttribute = comp.EmitToImageReference();

            string sourceA = """
                using System.Diagnostics.CodeAnalysis;
                public class A
                {
                    public static ref int F([UnscopedRef] ref int i) => ref i;
                }
                """;

            comp = CreateCompilation(sourceA, references: new[] { refAttribute }, targetFramework: TargetFramework.Net60);
            var refA = comp.EmitToImageReference();

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        int i = 0;
                        var d = A.F;
                        d(ref i);
                    }
                }
                """;

            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net60);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS0656: Missing compiler required member 'System.Diagnostics.CodeAnalysis.UnscopedRefAttribute..ctor'
                //         var d = A.F;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "A.F").WithArguments("System.Diagnostics.CodeAnalysis.UnscopedRefAttribute", ".ctor").WithLocation(6, 17));

            comp = CreateCompilation(sourceB, references: new[] { refA, refAttribute }, targetFramework: TargetFramework.Net60);
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0",
                $$"""
                    .class private auto ansi sealed '<>f__AnonymousDelegate0'
                        extends [System.Runtime]System.MulticastDelegate
                    {
                        .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                            01 00 00 00
                        )
                        // Methods
                        .method public hidebysig specialname rtspecialname
                            instance void .ctor (
                                object 'object',
                                native int 'method'
                            ) runtime managed
                        {
                        } // end of method '<>f__AnonymousDelegate0'::.ctor
                        .method public hidebysig newslot virtual
                            instance int32& Invoke (
                                int32& arg
                            ) runtime managed
                        {
                            .param [1]
                                .custom instance void ['{{attributeAssemblyName}}']System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = (
                                    01 00 00 00
                                )
                        } // end of method '<>f__AnonymousDelegate0'::Invoke
                    } // end of class <>f__AnonymousDelegate0
                    """);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem(63565, "https://github.com/dotnet/roslyn/issues/63565")]
        public void SynthesizedDelegateTypes_UnscopedRefAttribute_MissingConstructor_01()
        {
            string sourceA = """
                using System.Diagnostics.CodeAnalysis;
                public class A
                {
                    public static ref int F(int x, [UnscopedRef] ref int y) => ref y;
                }
                """;
            var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net70);
            var refA = comp.EmitToImageReference();

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        int i = 0;
                        var d = A.F;
                        d(0, ref i);
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net70);
            comp.MakeMemberMissing(WellKnownMember.System_Diagnostics_CodeAnalysis_UnscopedRefAttribute__ctor);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS0656: Missing compiler required member 'System.Diagnostics.CodeAnalysis.UnscopedRefAttribute..ctor'
                //         var d = A.F;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "A.F").WithArguments("System.Diagnostics.CodeAnalysis.UnscopedRefAttribute", ".ctor").WithLocation(6, 17));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem(63565, "https://github.com/dotnet/roslyn/issues/63565")]
        public void SynthesizedDelegateTypes_UnscopedRefAttribute_MissingConstructor_02()
        {
            string source = """
                using System;
                using System.Diagnostics.CodeAnalysis;
                class Program
                {
                    static void Main()
                    {
                        var d = (ref int x, [UnscopedRef] ref int y) => ref y;
                        Console.WriteLine(d);
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.MakeMemberMissing(WellKnownMember.System_Diagnostics_CodeAnalysis_UnscopedRefAttribute__ctor);
            comp.VerifyEmitDiagnostics(
                // (7,51): error CS0656: Missing compiler required member 'System.Diagnostics.CodeAnalysis.UnscopedRefAttribute..ctor'
                //         var d = (ref int x, [UnscopedRef] ref int y) => ref y;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "y").WithArguments("System.Diagnostics.CodeAnalysis.UnscopedRefAttribute", ".ctor").WithLocation(7, 51));
        }

        private static void VerifyLocalDelegateType(SemanticModel model, VariableDeclaratorSyntax variable, string expectedInvokeMethod)
        {
            var expectedBaseType = ((CSharpCompilation)model.Compilation).GetSpecialType(SpecialType.System_MulticastDelegate);

            var local = (ILocalSymbol)model.GetDeclaredSymbol(variable)!;
            var delegateType = (INamedTypeSymbol)local.Type;
            Assert.Equal(Accessibility.Internal, delegateType.DeclaredAccessibility);
            Assert.Equal(expectedInvokeMethod, delegateType.DelegateInvokeMethod.ToTestDisplayString());
            Assert.True(delegateType.IsImplicitlyDeclared);
            Assert.Equal(expectedBaseType.GetPublicSymbol(), delegateType.BaseType);

            var underlyingType = delegateType.GetSymbol<NamedTypeSymbol>();
            Assert.True(underlyingType.IsImplicitlyDeclared);
            Assert.Empty(underlyingType.DeclaringSyntaxReferences);
            Assert.Equal(expectedBaseType, underlyingType.BaseTypeNoUseSiteDiagnostics);
            Assert.Equal(expectedBaseType, underlyingType.GetDeclaredBaseType(null));
        }

        private static void VerifyExpressionType(SemanticModel model, ExpressionSyntax variable, string expectedSymbol, string expectedInvokeMethod)
        {
            var symbol = model.GetSymbolInfo(variable).Symbol;
            Assert.Equal(expectedSymbol, symbol.ToTestDisplayString());
            var type = (INamedTypeSymbol)model.GetTypeInfo(variable).Type!;
            Assert.Equal(expectedInvokeMethod, type.DelegateInvokeMethod.ToTestDisplayString());
        }

        [Fact]
        public void Invoke_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var d = (ref object obj) => obj;
        object obj = 1;
        var value = d.Invoke(ref obj);
        Console.WriteLine(value);
    }
}";
            CompileAndVerify(source, expectedOutput: @"1");
        }

        [Fact]
        public void Invoke_02()
        {
            var source =
@"#nullable enable
using System;
class Program
{
    static void Main()
    {
        var d1 = (ref int x) => x;
        var d2 = d1.Invoke;
        Console.WriteLine(d2.GetType());
    }
}";
            CompileAndVerify(source, expectedOutput: @"<>F{00000001}`2[System.Int32,System.Int32]");
        }

        [WorkItem(56808, "https://github.com/dotnet/roslyn/issues/56808")]
        [Fact]
        public void Invoke_03()
        {
            var source =
@"class Program
{
    static void Main()
    {
        M1();
        M2();
    }
    static void M1()
    {
        var d1 = (ref object obj) => { };
        object obj = null;
        var result = d1.BeginInvoke(ref obj, null, null);
        d1.EndInvoke(result);
    }
    unsafe static void M2()
    {
        var d2 = (int* p) => p;
        int* p = (int*)0;
        var result = d2.BeginInvoke(p, null, null);
        d2.EndInvoke(result);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            // https://github.com/dotnet/roslyn/issues/56808: Synthesized delegates should include BeginInvoke() and EndInvoke().
            comp.VerifyDiagnostics(
                // (12,25): error CS1061: '<anonymous delegate>' does not contain a definition for 'BeginInvoke' and no accessible extension method 'BeginInvoke' accepting a first argument of type '<anonymous delegate>' could be found (are you missing a using directive or an assembly reference?)
                //         var result = d1.BeginInvoke(ref obj, null, null);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "BeginInvoke").WithArguments("<anonymous delegate>", "BeginInvoke").WithLocation(12, 25),
                // (13,12): error CS1061: '<anonymous delegate>' does not contain a definition for 'EndInvoke' and no accessible extension method 'EndInvoke' accepting a first argument of type '<anonymous delegate>' could be found (are you missing a using directive or an assembly reference?)
                //         d1.EndInvoke(result);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "EndInvoke").WithArguments("<anonymous delegate>", "EndInvoke").WithLocation(13, 12),
                // (19,25): error CS1061: '<anonymous delegate>' does not contain a definition for 'BeginInvoke' and no accessible extension method 'BeginInvoke' accepting a first argument of type '<anonymous delegate>' could be found (are you missing a using directive or an assembly reference?)
                //         var result = d2.BeginInvoke(p, null, null);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "BeginInvoke").WithArguments("<anonymous delegate>", "BeginInvoke").WithLocation(19, 25),
                // (20,12): error CS1061: '<anonymous delegate>' does not contain a definition for 'EndInvoke' and no accessible extension method 'EndInvoke' accepting a first argument of type '<anonymous delegate>' could be found (are you missing a using directive or an assembly reference?)
                //         d2.EndInvoke(result);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "EndInvoke").WithArguments("<anonymous delegate>", "EndInvoke").WithLocation(20, 12));
        }

        [Fact]
        public void With()
        {
            var source =
@"class Program
{
    static void Main()
    {
        var d1 = (int x) => x;
        d1 = d1 with { };
        var d2 = (ref int x) => x;
        d2 = d2 with { };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,14): error CS8858: The receiver type 'Func<int, int>' is not a valid record type and is not a struct type.
                //         d1 = d1 with { };
                Diagnostic(ErrorCode.ERR_CannotClone, "d1").WithArguments("System.Func<int, int>").WithLocation(6, 14),
                // (8,14): error CS8858: The receiver type '<anonymous delegate>' is not a valid record type and is not a struct type.
                //         d2 = d2 with { };
                Diagnostic(ErrorCode.ERR_CannotClone, "d2").WithArguments("<anonymous delegate>").WithLocation(8, 14));
        }

        [Fact]
        public void Extension_GetAwaiter_01()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class Program
{
    static async Task Main()
    {
        Type type;
        type = await Main;
        Console.WriteLine(type);
        type = await ((ref int x) => x);
        Console.WriteLine(type);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
class Awaiter : INotifyCompletion
{
    private Delegate _d;
    public Awaiter(Delegate d) { _d = d; }
    public void OnCompleted(Action a) { }
    public Type GetResult() => _d.GetType();
    public bool IsCompleted => true;
}
static class Extensions
{
    public static Awaiter GetAwaiter(this Delegate d) => new Awaiter(d);
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,16): error CS4001: Cannot await 'method group'
                //         type = await Main;
                Diagnostic(ErrorCode.ERR_BadAwaitArgIntrinsic, "await Main").WithArguments("method group").WithLocation(9, 16),
                // (11,16): error CS4001: Cannot await 'lambda expression'
                //         type = await ((ref int x) => x);
                Diagnostic(ErrorCode.ERR_BadAwaitArgIntrinsic, "await ((ref int x) => x)").WithArguments("lambda expression").WithLocation(11, 16));
        }

        [Fact]
        public void Extension_GetAwaiter_02()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class Program
{
    static async Task Main()
    {
        Type type;
        type = await (Delegate)Main;
        Console.WriteLine(type);
        type = await (Delegate)((ref int x) => x);
        Console.WriteLine(type);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
class Awaiter : INotifyCompletion
{
    private Delegate _d;
    public Awaiter(Delegate d) { _d = d; }
    public void OnCompleted(Action a) { }
    public Type GetResult() => _d.GetType();
    public bool IsCompleted => true;
}
static class Extensions
{
    public static Awaiter GetAwaiter(this Delegate d) => new Awaiter(d);
}";
            CompileAndVerify(source, expectedOutput:
@"System.Func`1[System.Threading.Tasks.Task]
<>F{00000001}`2[System.Int32,System.Int32]
");
        }

        [Fact]
        public void Extension_GetEnumerator_01()
        {
            var source =
@"using System;
using System.Collections.Generic;
class Program
{
    static void Main()
    {
        foreach(var d in Main) Report(d);
        foreach(var d in (ref int x) => x) Report(d);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
static class Extensions
{
    public static IEnumerator<Delegate> GetEnumerator(this Delegate d)
    {
        yield return d;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,26): error CS0446: Foreach cannot operate on a 'method group'. Did you intend to invoke the 'method group'?
                //         foreach(var d in Main) Report(d);
                Diagnostic(ErrorCode.ERR_AnonMethGrpInForEach, "Main").WithArguments("method group").WithLocation(7, 26),
                // (8,26): error CS0446: Foreach cannot operate on a 'lambda expression'. Did you intend to invoke the 'lambda expression'?
                //         foreach(var d in (ref int x) => x) Report(d);
                Diagnostic(ErrorCode.ERR_AnonMethGrpInForEach, "(ref int x) => x").WithArguments("lambda expression").WithLocation(8, 26));
        }

        [Fact]
        public void Extension_GetEnumerator_02()
        {
            var source =
@"using System;
using System.Collections.Generic;
class Program
{
    static void Main()
    {
        foreach (var d in (Delegate)Main) Report(d);
        foreach (var d in (Delegate)((ref int x) => x)) Report(d);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
static class Extensions
{
    public static IEnumerator<Delegate> GetEnumerator(this Delegate d)
    {
        yield return d;
    }
}";
            CompileAndVerify(source, expectedOutput:
@"System.Action
<>F{00000001}`2[System.Int32,System.Int32]
");
        }

        [Fact]
        public void Extension_Deconstruct_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Type type;
        string name;
        (type, name) = Main;
        Console.WriteLine(type);
        (type, name) = (ref int x) => x;
        Console.WriteLine(type);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
static class Extensions
{
    public static void Deconstruct(this Delegate d, out Type type, out string name)
    {
        type = d.GetType();
        name = d.Method.Name;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,24): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         (type, name) = Main;
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "Main").WithLocation(8, 24),
                // (10,24): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         (type, name) = (ref int x) => x;
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "(ref int x) => x").WithLocation(10, 24));
        }

        [Fact]
        public void Extension_Deconstruct_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Type type;
        string name;
        (type, name) = (Delegate)Main;
        Console.WriteLine(type);
        (type, name) = (Delegate)((ref int x) => x);
        Console.WriteLine(type);
    }
    static void Report(Delegate d) => Console.WriteLine(d.GetType());
}
static class Extensions
{
    public static void Deconstruct(this Delegate d, out Type type, out string name)
    {
        type = d.GetType();
        name = d.Method.Name;
    }
}";
            CompileAndVerify(source, expectedOutput:
@"System.Action
<>F{00000001}`2[System.Int32,System.Int32]
");
        }

        [Fact]
        public void IOperation()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Delegate d = (int x) => x.ToString();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var operation = (IVariableDeclaratorOperation)model.GetOperation(syntax)!;

            var actualText = OperationTreeVerifier.GetOperationTree(comp, operation);
            OperationTreeVerifier.Verify(
@"IVariableDeclaratorOperation (Symbol: System.Delegate d) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd = (int x) ... .ToString()')
  Initializer:
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (int x) = ... .ToString()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Delegate, IsImplicit) (Syntax: '(int x) => x.ToString()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand:
          IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32, System.String>, IsImplicit) (Syntax: '(int x) => x.ToString()')
            Target:
              IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '(int x) => x.ToString()')
                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                    ReturnedValue:
                      IInvocationOperation (virtual System.String System.Int32.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'x.ToString()')
                        Instance Receiver:
                          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                        Arguments(0)
",
            actualText);

            var value = ((IConversionOperation)operation.Initializer!.Value).Operand;
            Assert.Equal("System.Func<System.Int32, System.String>", value.Type.ToTestDisplayString());
        }

        [Fact]
        public void ClassifyConversionFromExpression()
        {
            var source =
@"class Program
{
    static void Main()
    {
        object o = () => 1;
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var funcOfT = comp.GetWellKnownType(WellKnownType.System_Func_T);
            var tree = comp.SyntaxTrees[0];
            var expr = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var model = comp.GetSemanticModel(tree);
            model = ((CSharpSemanticModel)model).GetMemberModel(expr);

            verifyConversions(model, expr, comp.GetSpecialType(SpecialType.System_MulticastDelegate).GetPublicSymbol(), ConversionKind.FunctionType, ConversionKind.FunctionType);
            verifyConversions(model, expr, comp.GetWellKnownType(WellKnownType.System_Linq_Expressions_Expression).GetPublicSymbol(), ConversionKind.FunctionType, ConversionKind.FunctionType);
            verifyConversions(model, expr, getFunctionType(funcOfT.Construct(comp.GetSpecialType(SpecialType.System_Int32))), ConversionKind.FunctionType, ConversionKind.FunctionType);
            verifyConversions(model, expr, getFunctionType(funcOfT.Construct(comp.GetSpecialType(SpecialType.System_Object))), ConversionKind.NoConversion, ConversionKind.NoConversion);

            static ITypeSymbol getFunctionType(NamedTypeSymbol delegateType)
            {
                return new FunctionTypeSymbol_PublicModel(new FunctionTypeSymbol(delegateType));
            }

            static void verifyConversions(SemanticModel model, ExpressionSyntax expr, ITypeSymbol destination, ConversionKind expectedImplicitKind, ConversionKind expectedExplicitKind)
            {
                Assert.Equal(expectedImplicitKind, model.ClassifyConversion(expr, destination, isExplicitInSource: false).Kind);
                Assert.Equal(expectedExplicitKind, model.ClassifyConversion(expr, destination, isExplicitInSource: true).Kind);
            }
        }

        private sealed class FunctionTypeSymbol_PublicModel : Symbols.PublicModel.TypeSymbol
        {
            private readonly FunctionTypeSymbol _underlying;

            internal FunctionTypeSymbol_PublicModel(FunctionTypeSymbol underlying) :
                base(nullableAnnotation: default)
            {
                _underlying = underlying;
            }

            internal override TypeSymbol UnderlyingTypeSymbol => _underlying;
            internal override NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol => _underlying;
            internal override Symbol UnderlyingSymbol => _underlying;

            protected override void Accept(SymbolVisitor visitor) => throw new NotImplementedException();
            protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new NotImplementedException();
            protected override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument) => throw new NotImplementedException();
            protected override ITypeSymbol WithNullableAnnotation(CodeAnalysis.NullableAnnotation nullableAnnotation) => this;
        }

        [WorkItem(56407, "https://github.com/dotnet/roslyn/issues/56407")]
        [Fact]
        public void UserDefinedConversions_01()
        {
            var source =
@"using System.Linq.Expressions;

public class Program
{
    public static void Main()
    {
        SomeMethod((Employee e) => e.Name);
    }

    public static void SomeMethod(Field field) { }

    public class Employee
    {
        public string Name {  get; set; }
    }

    public class Field
    {
        public static implicit operator Field(Expression expression) => null;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (7,33): error CS1660: Cannot convert lambda expression to type 'Program.Field' because it is not a delegate type
                //         SomeMethod((Employee e) => e.Name);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "Program.Field").WithLocation(7, 33)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void UserDefinedConversions_Implicit_01()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C1
{
    public static implicit operator C1(Func<int> f) { Console.WriteLine(""operator C1(Func<int> f)""); return new C1(); }
}
class C2
{
    public static implicit operator C2(Expression<Func<int>> e) { Console.WriteLine(""operator C2(Expression<Func<int>> e)""); return new C2(); }
}
class Program
{
    static int F() => 0;
    static void Main()
    {
        C1 c1 = () => 1;
        C2 c2 = () => 2;
        c1 = F;
        _ = (C1)(() => 1);
        _ = (C2)(() => 2);
        _ = (C1)F;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (16,20): error CS1660: Cannot convert lambda expression to type 'C1' because it is not a delegate type
                //         C1 c1 = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C1").WithLocation(16, 20),
                // (17,20): error CS1660: Cannot convert lambda expression to type 'C2' because it is not a delegate type
                //         C2 c2 = () => 2;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C2").WithLocation(17, 20),
                // (18,14): error CS0428: Cannot convert method group 'F' to non-delegate type 'C1'. Did you intend to invoke the method?
                //         c1 = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "C1").WithLocation(18, 14)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void UserDefinedConversions_Implicit_02()
        {
            var source =
@"using System;
class C1
{
    public static implicit operator C1(object o) { Console.WriteLine(""operator C1(object o)""); return new C1(); }
}
class C2
{
    public static implicit operator C2(ICloneable c) { Console.WriteLine(""operator C2(ICloneable c)""); return new C2(); }
}
class Program
{
    static int F() => 0;
    static void Main()
    {
        C1 c1 = () => 1;
        C2 c2 = () => 2;
        c1 = F;
        c2 = F;
        _ = (C1)(() => 1);
        _ = (C2)(() => 2);
        _ = (C1)F;
        _ = (C2)F;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (4,37): error CS0553: 'C1.implicit operator C1(object)': user-defined conversions to or from a base type are not allowed
                //     public static implicit operator C1(object o) { Console.WriteLine("operator C1(object o)"); return new C1(); }
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "C1").WithArguments("C1.implicit operator C1(object)").WithLocation(4, 37),
                // (8,37): error CS0552: 'C2.implicit operator C2(ICloneable)': user-defined conversions to or from an interface are not allowed
                //     public static implicit operator C2(ICloneable c) { Console.WriteLine("operator C2(ICloneable c)"); return new C2(); }
                Diagnostic(ErrorCode.ERR_ConversionWithInterface, "C2").WithArguments("C2.implicit operator C2(System.ICloneable)").WithLocation(8, 37),
                // (15,20): error CS1660: Cannot convert lambda expression to type 'C1' because it is not a delegate type
                //         C1 c1 = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C1").WithLocation(15, 20),
                // (16,20): error CS1660: Cannot convert lambda expression to type 'C2' because it is not a delegate type
                //         C2 c2 = () => 2;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C2").WithLocation(16, 20),
                // (17,14): error CS0428: Cannot convert method group 'F' to non-delegate type 'C1'. Did you intend to invoke the method?
                //         c1 = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "C1").WithLocation(17, 14),
                // (18,14): error CS0428: Cannot convert method group 'F' to non-delegate type 'C2'. Did you intend to invoke the method?
                //         c2 = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "C2").WithLocation(18, 14),
                // (19,21): error CS1660: Cannot convert lambda expression to type 'C1' because it is not a delegate type
                //         _ = (C1)(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C1").WithLocation(19, 21),
                // (20,21): error CS1660: Cannot convert lambda expression to type 'C2' because it is not a delegate type
                //         _ = (C2)(() => 2);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C2").WithLocation(20, 21),
                // (21,13): error CS0030: Cannot convert type 'method' to 'C1'
                //         _ = (C1)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C1)F").WithArguments("method", "C1").WithLocation(21, 13),
                // (22,13): error CS0030: Cannot convert type 'method' to 'C2'
                //         _ = (C2)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C2)F").WithArguments("method", "C2").WithLocation(22, 13)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void UserDefinedConversions_Implicit_03()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C1
{
    public static implicit operator C1(Delegate d) { Console.WriteLine(""operator C1(Delegate d)""); return new C1(); }
}
class C2
{
    public static implicit operator C2(MulticastDelegate d) { Console.WriteLine(""operator C2(MulticastDelegate d)""); return new C2(); }
}
class C3
{
    public static implicit operator C3(Expression e) { Console.WriteLine(""operator C3(Expression e)""); return new C3(); }
}
class C4
{
    public static implicit operator C4(LambdaExpression e) { Console.WriteLine(""operator C4(LambdaExpression e)""); return new C4(); }
}
class Program
{
    static int F() => 0;
    static void Main()
    {
        C1 c1 = () => 1;
        C2 c2 = () => 2;
        C3 c3 = () => 3;
        C4 c4 = () => 4;
        c1 = F;
        c2 = F;
        _ = (C1)(() => 1);
        _ = (C2)(() => 2);
        _ = (C3)(() => 3);
        _ = (C4)(() => 4);
        _ = (C1)F;
        _ = (C2)F;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (24,20): error CS1660: Cannot convert lambda expression to type 'C1' because it is not a delegate type
                //         C1 c1 = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C1").WithLocation(24, 20),
                // (25,20): error CS1660: Cannot convert lambda expression to type 'C2' because it is not a delegate type
                //         C2 c2 = () => 2;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C2").WithLocation(25, 20),
                // (26,20): error CS1660: Cannot convert lambda expression to type 'C3' because it is not a delegate type
                //         C3 c3 = () => 3;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C3").WithLocation(26, 20),
                // (27,20): error CS1660: Cannot convert lambda expression to type 'C4' because it is not a delegate type
                //         C4 c4 = () => 4;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C4").WithLocation(27, 20),
                // (28,14): error CS0428: Cannot convert method group 'F' to non-delegate type 'C1'. Did you intend to invoke the method?
                //         c1 = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "C1").WithLocation(28, 14),
                // (29,14): error CS0428: Cannot convert method group 'F' to non-delegate type 'C2'. Did you intend to invoke the method?
                //         c2 = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "C2").WithLocation(29, 14),
                // (30,21): error CS1660: Cannot convert lambda expression to type 'C1' because it is not a delegate type
                //         _ = (C1)(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C1").WithLocation(30, 21),
                // (31,21): error CS1660: Cannot convert lambda expression to type 'C2' because it is not a delegate type
                //         _ = (C2)(() => 2);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C2").WithLocation(31, 21),
                // (32,21): error CS1660: Cannot convert lambda expression to type 'C3' because it is not a delegate type
                //         _ = (C3)(() => 3);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C3").WithLocation(32, 21),
                // (33,21): error CS1660: Cannot convert lambda expression to type 'C4' because it is not a delegate type
                //         _ = (C4)(() => 4);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C4").WithLocation(33, 21),
                // (34,13): error CS0030: Cannot convert type 'method' to 'C1'
                //         _ = (C1)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C1)F").WithArguments("method", "C1").WithLocation(34, 13),
                // (35,13): error CS0030: Cannot convert type 'method' to 'C2'
                //         _ = (C2)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C2)F").WithArguments("method", "C2").WithLocation(35, 13)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void UserDefinedConversions_Implicit_04()
        {
            var source =
@"using System;
class C<T>
{
    public static implicit operator C<T>(T t) { Console.WriteLine(""operator C<{0}>({0} t)"", typeof(T).FullName); return new C<T>(); }
}
class Program
{
    static int F() => 0;
    static void Main()
    {
        C<object> c1 = () => 1;
        C<ICloneable> c2 = () => 2;
        c1 = F;
        c2 = F;
        _ = (C<object>)(() => 1);
        _ = (C<ICloneable>)(() => 2);
        _ = (C<object>)F;
        _ = (C<ICloneable>)F;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (11,27): error CS1660: Cannot convert lambda expression to type 'C<object>' because it is not a delegate type
                //         C<object> c1 = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<object>").WithLocation(11, 27),
                // (12,31): error CS1660: Cannot convert lambda expression to type 'C<ICloneable>' because it is not a delegate type
                //         C<ICloneable> c2 = () => 2;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.ICloneable>").WithLocation(12, 31),
                // (13,14): error CS0428: Cannot convert method group 'F' to non-delegate type 'C<object>'. Did you intend to invoke the method?
                //         c1 = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "C<object>").WithLocation(13, 14),
                // (14,14): error CS0428: Cannot convert method group 'F' to non-delegate type 'C<ICloneable>'. Did you intend to invoke the method?
                //         c2 = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "C<System.ICloneable>").WithLocation(14, 14),
                // (15,28): error CS1660: Cannot convert lambda expression to type 'C<object>' because it is not a delegate type
                //         _ = (C<object>)(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<object>").WithLocation(15, 28),
                // (16,32): error CS1660: Cannot convert lambda expression to type 'C<ICloneable>' because it is not a delegate type
                //         _ = (C<ICloneable>)(() => 2);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.ICloneable>").WithLocation(16, 32),
                // (17,13): error CS0030: Cannot convert type 'method' to 'C<object>'
                //         _ = (C<object>)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C<object>)F").WithArguments("method", "C<object>").WithLocation(17, 13),
                // (18,13): error CS0030: Cannot convert type 'method' to 'C<ICloneable>'
                //         _ = (C<ICloneable>)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C<ICloneable>)F").WithArguments("method", "C<System.ICloneable>").WithLocation(18, 13)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void UserDefinedConversions_Implicit_05()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C<T>
{
    public static implicit operator C<T>(T t) { Console.WriteLine(""operator C<{0}>({0} t)"", typeof(T).FullName); return new C<T>(); }
}
class Program
{
    static int F() => 0;
    static void Main()
    {
        C<Delegate> c1 = () => 1;
        C<MulticastDelegate> c2 = () => 2;
        C<Expression> c3 = () => 3;
        C<LambdaExpression> c4 = () => 4;
        c1 = F;
        c2 = F;
        _ = (C<Delegate>)(() => 1);
        _ = (C<MulticastDelegate>)(() => 2);
        _ = (C<Expression>)(() => 3);
        _ = (C<LambdaExpression>)(() => 4);
        _ = (C<Delegate>)F;
        _ = (C<MulticastDelegate>)F;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (12,29): error CS1660: Cannot convert lambda expression to type 'C<Delegate>' because it is not a delegate type
                //         C<Delegate> c1 = () => 1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.Delegate>").WithLocation(12, 29),
                // (13,38): error CS1660: Cannot convert lambda expression to type 'C<MulticastDelegate>' because it is not a delegate type
                //         C<MulticastDelegate> c2 = () => 2;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.MulticastDelegate>").WithLocation(13, 38),
                // (14,31): error CS1660: Cannot convert lambda expression to type 'C<Expression>' because it is not a delegate type
                //         C<Expression> c3 = () => 3;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.Linq.Expressions.Expression>").WithLocation(14, 31),
                // (15,37): error CS1660: Cannot convert lambda expression to type 'C<LambdaExpression>' because it is not a delegate type
                //         C<LambdaExpression> c4 = () => 4;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.Linq.Expressions.LambdaExpression>").WithLocation(15, 37),
                // (16,14): error CS0428: Cannot convert method group 'F' to non-delegate type 'C<Delegate>'. Did you intend to invoke the method?
                //         c1 = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "C<System.Delegate>").WithLocation(16, 14),
                // (17,14): error CS0428: Cannot convert method group 'F' to non-delegate type 'C<MulticastDelegate>'. Did you intend to invoke the method?
                //         c2 = F;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "C<System.MulticastDelegate>").WithLocation(17, 14),
                // (18,30): error CS1660: Cannot convert lambda expression to type 'C<Delegate>' because it is not a delegate type
                //         _ = (C<Delegate>)(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.Delegate>").WithLocation(18, 30),
                // (19,39): error CS1660: Cannot convert lambda expression to type 'C<MulticastDelegate>' because it is not a delegate type
                //         _ = (C<MulticastDelegate>)(() => 2);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.MulticastDelegate>").WithLocation(19, 39),
                // (20,32): error CS1660: Cannot convert lambda expression to type 'C<Expression>' because it is not a delegate type
                //         _ = (C<Expression>)(() => 3);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.Linq.Expressions.Expression>").WithLocation(20, 32),
                // (21,38): error CS1660: Cannot convert lambda expression to type 'C<LambdaExpression>' because it is not a delegate type
                //         _ = (C<LambdaExpression>)(() => 4);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.Linq.Expressions.LambdaExpression>").WithLocation(21, 38),
                // (22,13): error CS0030: Cannot convert type 'method' to 'C<Delegate>'
                //         _ = (C<Delegate>)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C<Delegate>)F").WithArguments("method", "C<System.Delegate>").WithLocation(22, 13),
                // (23,13): error CS0030: Cannot convert type 'method' to 'C<MulticastDelegate>'
                //         _ = (C<MulticastDelegate>)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C<MulticastDelegate>)F").WithArguments("method", "C<System.MulticastDelegate>").WithLocation(23, 13)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void UserDefinedConversions_Explicit_01()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C1
{
    public static explicit operator C1(Func<int> f) { Console.WriteLine(""operator C1(Func<int> f)""); return new C1(); }
}
class C2
{
    public static explicit operator C2(Expression<Func<int>> e) { Console.WriteLine(""operator C2(Expression<Func<int>> e)""); return new C2(); }
}
class Program
{
    static int F() => 0;
    static void Main()
    {
        _ = (C1)(() => 1);
        _ = (C2)(() => 2);
        _ = (C1)F;
    }
}";

            string expectedOutput =
@"operator C1(Func<int> f)
operator C2(Expression<Func<int>> e)
operator C1(Func<int> f)
";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
        }

        [Fact]
        public void UserDefinedConversions_Explicit_02()
        {
            var source =
@"using System;
class C1
{
    public static explicit operator C1(object o) { Console.WriteLine(""operator C1(object o)""); return new C1(); }
}
class C2
{
    public static explicit operator C2(ICloneable c) { Console.WriteLine(""operator C2(ICloneable c)""); return new C2(); }
}
class Program
{
    static int F() => 0;
    static void Main()
    {
        _ = (C1)(() => 1);
        _ = (C2)(() => 2);
        _ = (C1)F;
        _ = (C2)F;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (4,37): error CS0553: 'C1.explicit operator C1(object)': user-defined conversions to or from a base type are not allowed
                //     public static explicit operator C1(object o) { Console.WriteLine("operator C1(object o)"); return new C1(); }
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "C1").WithArguments("C1.explicit operator C1(object)").WithLocation(4, 37),
                // (8,37): error CS0552: 'C2.explicit operator C2(ICloneable)': user-defined conversions to or from an interface are not allowed
                //     public static explicit operator C2(ICloneable c) { Console.WriteLine("operator C2(ICloneable c)"); return new C2(); }
                Diagnostic(ErrorCode.ERR_ConversionWithInterface, "C2").WithArguments("C2.explicit operator C2(System.ICloneable)").WithLocation(8, 37),
                // (15,21): error CS1660: Cannot convert lambda expression to type 'C1' because it is not a delegate type
                //         _ = (C1)(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C1").WithLocation(15, 21),
                // (16,21): error CS1660: Cannot convert lambda expression to type 'C2' because it is not a delegate type
                //         _ = (C2)(() => 2);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C2").WithLocation(16, 21),
                // (17,13): error CS0030: Cannot convert type 'method' to 'C1'
                //         _ = (C1)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C1)F").WithArguments("method", "C1").WithLocation(17, 13),
                // (18,13): error CS0030: Cannot convert type 'method' to 'C2'
                //         _ = (C2)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C2)F").WithArguments("method", "C2").WithLocation(18, 13)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void UserDefinedConversions_Explicit_03()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C1
{
    public static explicit operator C1(Delegate d) { Console.WriteLine(""operator C1(Delegate d)""); return new C1(); }
}
class C2
{
    public static explicit operator C2(MulticastDelegate d) { Console.WriteLine(""operator C2(MulticastDelegate d)""); return new C2(); }
}
class C3
{
    public static explicit operator C3(Expression e) { Console.WriteLine(""operator C3(Expression e)""); return new C3(); }
}
class C4
{
    public static explicit operator C4(LambdaExpression e) { Console.WriteLine(""operator C4(LambdaExpression e)""); return new C4(); }
}
class Program
{
    static int F() => 0;
    static void Main()
    {
        _ = (C1)(() => 1);
        _ = (C2)(() => 2);
        _ = (C3)(() => 3);
        _ = (C4)(() => 4);
        _ = (C1)F;
        _ = (C2)F;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (24,21): error CS1660: Cannot convert lambda expression to type 'C1' because it is not a delegate type
                //         _ = (C1)(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C1").WithLocation(24, 21),
                // (25,21): error CS1660: Cannot convert lambda expression to type 'C2' because it is not a delegate type
                //         _ = (C2)(() => 2);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C2").WithLocation(25, 21),
                // (26,21): error CS1660: Cannot convert lambda expression to type 'C3' because it is not a delegate type
                //         _ = (C3)(() => 3);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C3").WithLocation(26, 21),
                // (27,21): error CS1660: Cannot convert lambda expression to type 'C4' because it is not a delegate type
                //         _ = (C4)(() => 4);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C4").WithLocation(27, 21),
                // (28,13): error CS0030: Cannot convert type 'method' to 'C1'
                //         _ = (C1)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C1)F").WithArguments("method", "C1").WithLocation(28, 13),
                // (29,13): error CS0030: Cannot convert type 'method' to 'C2'
                //         _ = (C2)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C2)F").WithArguments("method", "C2").WithLocation(29, 13)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void UserDefinedConversions_Explicit_04()
        {
            var source =
@"using System;
class C<T>
{
    public static explicit operator C<T>(T t) { Console.WriteLine(""operator C<{0}>({0} t)"", typeof(T).FullName); return new C<T>(); }
}
class Program
{
    static int F() => 0;
    static void Main()
    {
        _ = (C<object>)(() => 1);
        _ = (C<ICloneable>)(() => 2);
        _ = (C<object>)F;
        _ = (C<ICloneable>)F;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (11,28): error CS1660: Cannot convert lambda expression to type 'C<object>' because it is not a delegate type
                //         _ = (C<object>)(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<object>").WithLocation(11, 28),
                // (12,32): error CS1660: Cannot convert lambda expression to type 'C<ICloneable>' because it is not a delegate type
                //         _ = (C<ICloneable>)(() => 2);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.ICloneable>").WithLocation(12, 32),
                // (13,13): error CS0030: Cannot convert type 'method' to 'C<object>'
                //         _ = (C<object>)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C<object>)F").WithArguments("method", "C<object>").WithLocation(13, 13),
                // (14,13): error CS0030: Cannot convert type 'method' to 'C<ICloneable>'
                //         _ = (C<ICloneable>)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C<ICloneable>)F").WithArguments("method", "C<System.ICloneable>").WithLocation(14, 13)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void UserDefinedConversions_Explicit_05()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C<T>
{
    public static explicit operator C<T>(T t) { Console.WriteLine(""operator C<{0}>({0} t)"", typeof(T).FullName); return new C<T>(); }
}
class Program
{
    static int F() => 0;
    static void Main()
    {
        _ = (C<Delegate>)(() => 1);
        _ = (C<MulticastDelegate>)(() => 2);
        _ = (C<Expression>)(() => 3);
        _ = (C<LambdaExpression>)(() => 4);
        _ = (C<Delegate>)F;
        _ = (C<MulticastDelegate>)F;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (12,30): error CS1660: Cannot convert lambda expression to type 'C<Delegate>' because it is not a delegate type
                //         _ = (C<Delegate>)(() => 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.Delegate>").WithLocation(12, 30),
                // (13,39): error CS1660: Cannot convert lambda expression to type 'C<MulticastDelegate>' because it is not a delegate type
                //         _ = (C<MulticastDelegate>)(() => 2);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.MulticastDelegate>").WithLocation(13, 39),
                // (14,32): error CS1660: Cannot convert lambda expression to type 'C<Expression>' because it is not a delegate type
                //         _ = (C<Expression>)(() => 3);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.Linq.Expressions.Expression>").WithLocation(14, 32),
                // (15,38): error CS1660: Cannot convert lambda expression to type 'C<LambdaExpression>' because it is not a delegate type
                //         _ = (C<LambdaExpression>)(() => 4);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "C<System.Linq.Expressions.LambdaExpression>").WithLocation(15, 38),
                // (16,13): error CS0030: Cannot convert type 'method' to 'C<Delegate>'
                //         _ = (C<Delegate>)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C<Delegate>)F").WithArguments("method", "C<System.Delegate>").WithLocation(16, 13),
                // (17,13): error CS0030: Cannot convert type 'method' to 'C<MulticastDelegate>'
                //         _ = (C<MulticastDelegate>)F;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C<MulticastDelegate>)F").WithArguments("method", "C<System.MulticastDelegate>").WithLocation(17, 13)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        /// <summary>
        /// Overload resolution and method type inference should not need to infer delegate
        /// types for lambdas and method groups when the overloads have specific delegate types.
        /// It is important to avoid inferring delegate types unnecessarily in these cases because
        /// that would add overhead (particularly for overload resolution with nested lambdas)
        /// while adding explicit parameter types for lambda expressions should ideally improve
        /// overload resolution performance in those cases because fewer overloads may be applicable.
        /// </summary>
        [Fact]
        [WorkItem(1153265, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1153265")]
        [WorkItem(58106, "https://github.com/dotnet/roslyn/issues/58106")]
        public void InferDelegateType_01()
        {
            var source =
@"using System;
class Program
{
    static void M(int i) { }
    static void Main()
    {
        int x = 0;
        F(x, (int y) => { });
        F(x, M);
    }
    static void F(int x, Action<int> y) { }
    static void F(int x, Action<int, int> y) { }
    static void F<T>(int x, Func<T> y, int z) { }
}";
            var comp = CreateCompilation(source);
            var data = new InferredDelegateTypeData();
            comp.TestOnlyCompilationData = data;
            comp.VerifyDiagnostics();
            Assert.Equal(0, data.InferredDelegateCount);
        }

        /// <summary>
        /// Similar to test above but with errors in the overloaded calls which means overload resolution
        /// will consider more overloads when binding for error recovery.
        /// </summary>
        [Fact]
        [WorkItem(1153265, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1153265")]
        [WorkItem(58106, "https://github.com/dotnet/roslyn/issues/58106")]
        public void InferDelegateType_02()
        {
            var source =
@"using System;
class Program
{
    static void M(int i) { }
    static void Main()
    {
        F(x, (int y) => { });
        F(x, M);
    }
    static void F(int x, Action<int> y) { }
    static void F(int x, Action<int, int> y) { }
    static void F<T>(int x, Func<T> y, int z) { }
}";
            var comp = CreateCompilation(source);
            var data = new InferredDelegateTypeData();
            comp.TestOnlyCompilationData = data;
            comp.VerifyDiagnostics(
                // (7,11): error CS0103: The name 'x' does not exist in the current context
                //         F(x, (int y) => { });
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(7, 11),
                // (8,11): error CS0103: The name 'x' does not exist in the current context
                //         F(x, M);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(8, 11));
            Assert.Equal(0, data.InferredDelegateCount);
        }

        [ConditionalFact(typeof(NoUsedAssembliesValidation), Reason = "GetEmitDiagnostics affects result")]
        public void InferDelegateType_03()
        {
            var source =
@"using System;
class Program
{
    static void M(int i) { }
    static void Main()
    {
        int x = 0;
        F(x, (int y) => { });
        F(x, M);
    }
    static void F(int x, Action<int> y) { }
    static void F(int x, Delegate y) { }
}";
            var comp = CreateCompilation(source);
            var data = new InferredDelegateTypeData();
            comp.TestOnlyCompilationData = data;
            comp.VerifyDiagnostics();
            Assert.Equal(2, data.InferredDelegateCount);
        }

        [ConditionalFact(typeof(NoUsedAssembliesValidation), Reason = "GetEmitDiagnostics affects result")]
        public void InferDelegateType_04()
        {
            var source =
@"using System;
class Program
{
    static void M(int i) { }
    static void Main()
    {
        F(x, (int y) => { });
        F(x, M);
    }
    static void F(int x, Action<int> y) { }
    static void F<T>(int x, T y, int z) { }
}";
            var comp = CreateCompilation(source);
            var data = new InferredDelegateTypeData();
            comp.TestOnlyCompilationData = data;
            comp.VerifyDiagnostics(
                // (7,11): error CS0103: The name 'x' does not exist in the current context
                //         F(x, (int y) => { });
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(7, 11),
                // (8,11): error CS0103: The name 'x' does not exist in the current context
                //         F(x, M);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(8, 11));
            Assert.Equal(2, data.InferredDelegateCount);
        }

        [Fact]
        public void FunctionTypeSymbolOperations()
        {
            var source =
@"class Program
{
    static void Main()
    {
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var objectType = comp.GetSpecialType(SpecialType.System_Object);
            var stringType = comp.GetSpecialType(SpecialType.System_String);
            var funcOfT = comp.GetWellKnownType(WellKnownType.System_Func_T);
            var funcOfObjectNullable = funcOfT.Construct(ImmutableArray.Create(TypeWithAnnotations.Create(objectType, NullableAnnotation.Annotated)));
            var funcOfStringNullable = funcOfT.Construct(ImmutableArray.Create(TypeWithAnnotations.Create(stringType, NullableAnnotation.Annotated)));
            var funcOfStringNotNullable = funcOfT.Construct(ImmutableArray.Create(TypeWithAnnotations.Create(stringType, NullableAnnotation.NotAnnotated)));

            var functionTypeObjectNullable = new FunctionTypeSymbol(funcOfObjectNullable);
            var functionTypeStringNullable = new FunctionTypeSymbol(funcOfStringNullable);
            var functionTypeStringNotNullable = new FunctionTypeSymbol(funcOfStringNotNullable);
            var functionTypeNullA = new FunctionTypeSymbol(null!);
            var functionTypeNullB = new FunctionTypeSymbol(null!);

            // MergeEquivalentTypes
            Assert.Equal(functionTypeStringNullable, functionTypeStringNullable.MergeEquivalentTypes(functionTypeStringNullable, VarianceKind.Out));
            Assert.Equal(functionTypeStringNullable, functionTypeStringNullable.MergeEquivalentTypes(functionTypeStringNotNullable, VarianceKind.Out));
            Assert.Equal(functionTypeStringNullable, functionTypeStringNotNullable.MergeEquivalentTypes(functionTypeStringNullable, VarianceKind.Out));
            Assert.Equal(functionTypeStringNotNullable, functionTypeStringNotNullable.MergeEquivalentTypes(functionTypeStringNotNullable, VarianceKind.Out));
            Assert.Equal(functionTypeNullA, functionTypeNullA.MergeEquivalentTypes(functionTypeNullA, VarianceKind.Out));

            // SetNullabilityForReferenceTypes
            var setNotNullable = (TypeWithAnnotations type) => TypeWithAnnotations.Create(type.Type, NullableAnnotation.NotAnnotated);
            Assert.Equal(functionTypeStringNotNullable, functionTypeStringNullable.SetNullabilityForReferenceTypes(setNotNullable));
            Assert.Equal(functionTypeNullA, functionTypeNullA.SetNullabilityForReferenceTypes(setNotNullable));

            // Equals
            Assert.True(functionTypeStringNotNullable.Equals(functionTypeStringNullable, TypeCompareKind.AllIgnoreOptions));
            Assert.False(functionTypeStringNotNullable.Equals(functionTypeStringNullable, TypeCompareKind.ConsiderEverything));
            Assert.False(functionTypeNullA.Equals(functionTypeStringNullable, TypeCompareKind.AllIgnoreOptions));
            Assert.False(functionTypeStringNullable.Equals(functionTypeNullA, TypeCompareKind.AllIgnoreOptions));
            Assert.True(functionTypeNullA.Equals(functionTypeNullA, TypeCompareKind.ConsiderEverything));
            Assert.False(functionTypeNullA.Equals(functionTypeNullB, TypeCompareKind.ConsiderEverything));

            // GetHashCode
            Assert.Equal(functionTypeStringNullable.GetHashCode(), functionTypeStringNotNullable.GetHashCode());
            Assert.Equal(functionTypeNullA.GetHashCode(), functionTypeNullB.GetHashCode());

            // ConversionsBase.ClassifyImplicitConversionFromTypeWhenNeitherOrBothFunctionTypes
            var conversions = new TypeConversions(comp.SourceAssembly.CorLibrary);
            var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            Assert.Equal(ConversionKind.FunctionType, conversions.ClassifyImplicitConversionFromTypeWhenNeitherOrBothFunctionTypes(functionTypeStringNullable, functionTypeStringNotNullable, ref useSiteInfo).Kind);
            Assert.Equal(ConversionKind.FunctionType, conversions.ClassifyImplicitConversionFromTypeWhenNeitherOrBothFunctionTypes(functionTypeStringNullable, functionTypeObjectNullable, ref useSiteInfo).Kind);
            Assert.Equal(ConversionKind.NoConversion, conversions.ClassifyImplicitConversionFromTypeWhenNeitherOrBothFunctionTypes(functionTypeStringNullable, functionTypeNullA, ref useSiteInfo).Kind);
            Assert.Equal(ConversionKind.NoConversion, conversions.ClassifyImplicitConversionFromTypeWhenNeitherOrBothFunctionTypes(functionTypeNullA, functionTypeStringNullable, ref useSiteInfo).Kind);
            Assert.Equal(ConversionKind.NoConversion, conversions.ClassifyImplicitConversionFromTypeWhenNeitherOrBothFunctionTypes(functionTypeNullA, functionTypeNullA, ref useSiteInfo).Kind);
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

        [Fact, WorkItem(64656, "https://github.com/dotnet/roslyn/issues/64656")]
        public void UsingStatic_DelegateInference()
        {
            var source = """
                using static A;
                var f = M;
                f();
                static class A
                {
                    public static void M() => System.Console.WriteLine("A.M()");
                }
                """;
            CompileAndVerify(source, expectedOutput: "A.M()").VerifyDiagnostics();
        }

        [Fact]
        public void LambdaWithDefaultParameter()
        {
            var source = """
using System;

class Program
{
    static void Report(object d) => Console.WriteLine(d.GetType());
    public static void Main()
    {   
        var lam = (int x = 30) => x;
        Console.WriteLine(lam() + " " + lam(10));
        Report(lam);
    } 
}
""";

            var expectAnonymousDelegateIL =
$@"
.class private auto ansi sealed '<>f__AnonymousDelegate0`2'<T1, TResult>
	extends [{s_libPrefix}]System.MulticastDelegate
{{
	.custom instance void [{s_libPrefix}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			object 'object',
			native int 'method'
		) runtime managed 
	{{
	}} // end of method '<>f__AnonymousDelegate0`2'::.ctor
	.method public hidebysig newslot virtual 
		instance !TResult Invoke (
			[opt] !T1 arg
		) runtime managed 
	{{
		.param [1] = int32(30)
	}} // end of method '<>f__AnonymousDelegate0`2'::Invoke
}} // end of class <>f__AnonymousDelegate0`2
";

            var expectLoweredClosureContainerIL =
$@"
    .class nested private auto ansi sealed serializable beforefieldinit '<>c'
    extends [{s_libPrefix}]System.Object
{{
    .custom instance void [{s_libPrefix}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    // Fields
    .field public static initonly class Program/'<>c' '<>9'
    .field public static class '<>f__AnonymousDelegate0`2'<int32, int32> '<>9__1_0'
    // Methods
    .method private hidebysig specialname rtspecialname static 
        void .cctor () cil managed 
    {{
        // Method begins at RVA 0x20de
        // Code size 11 (0xb)
        .maxstack 8
        IL_0000: newobj instance void Program/'<>c'::.ctor()
        IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
        IL_000a: ret
    }} // end of method '<>c'::.cctor
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {{
        // Method begins at RVA 0x20d6
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [{s_libPrefix}]System.Object::.ctor()
        IL_0006: ret
    }} // end of method '<>c'::.ctor
    .method assembly hidebysig 
        instance int32 '<Main>b__1_0' (
            [opt] int32 x
        ) cil managed 
    {{
        .param [1] = int32(30)
        // Method begins at RVA 0x20ea
        // Code size 2 (0x2)
        .maxstack 8
        IL_0000: ldarg.1
        IL_0001: ret
    }} // end of method '<>c'::'<Main>b__1_0'
}} // end of class <>c
";

            var verifier = CompileAndVerify(source, expectedOutput:
@"30 10
<>f__AnonymousDelegate0`2[System.Int32,System.Int32]");
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`2", expectAnonymousDelegateIL);
            verifier.VerifyTypeIL("<>c", expectLoweredClosureContainerIL);
        }

        [Fact]
        public void LambdaWithMultipleDefaultParameters()
        {
            var source = """
using System;

class Program
{
    public static string Report(object obj) => obj.GetType().ToString();
    public static void Main()
    {
        var lam = (int a = 1, int b = 2, int c = 3) => a + b + c;
        Console.WriteLine(lam(2) + " " + Report(lam));
    }
}
""";
            var verifier = CompileAndVerify(source, expectedOutput: "7 <>f__AnonymousDelegate0`4[System.Int32,System.Int32,System.Int32,System.Int32]");
        }

        [Fact]
        public void LambdaWithOptionalAndDefaultParameters()
        {
            var source = """
using System;

class Program
{
    public static string Report(object obj) => obj.GetType().ToString();
    public static void Main()
    {
        var lam = (string s1, string s2 = "b", string s3 = "c") => s1 + s2 + s3;
        Console.WriteLine(lam("a") + " " + Report(lam));
    }
}
""";
            var expectAnonymousDelegateIL =
$@"
.class private auto ansi sealed '<>f__AnonymousDelegate0`4'<T1, T2, T3, TResult>
	extends [{s_libPrefix}]System.MulticastDelegate
{{
	.custom instance void [{s_libPrefix}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			object 'object',
			native int 'method'
		) runtime managed 
	{{
	}} // end of method '<>f__AnonymousDelegate0`4'::.ctor
	.method public hidebysig newslot virtual 
		instance !TResult Invoke (
			!T1 arg1,
			[opt] !T2 arg2,
			[opt] !T3 arg3
		) runtime managed 
	{{
		.param [2] = ""b""
		.param [3] = ""c""
	}} // end of method '<>f__AnonymousDelegate0`4'::Invoke
}} // end of class <>f__AnonymousDelegate0`4
";

            var expectLoweredClosureContainerIL =
$@"
    .class nested private auto ansi sealed serializable beforefieldinit '<>c'
	extends [{s_libPrefix}]System.Object
{{
	.custom instance void [{s_libPrefix}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Fields
	.field public static initonly class Program/'<>c' '<>9'
	.field public static class '<>f__AnonymousDelegate0`4'<string, string, string, string> '<>9__1_0'
	// Methods
	.method private hidebysig specialname rtspecialname static 
		void .cctor () cil managed 
	{{
		// Method begins at RVA 0x20d3
		// Code size 11 (0xb)
		.maxstack 8
		IL_0000: newobj instance void Program/'<>c'::.ctor()
		IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
		IL_000a: ret
	}} // end of method '<>c'::.cctor
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{{
		// Method begins at RVA 0x20cb
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [{s_libPrefix}]System.Object::.ctor()
		IL_0006: ret
	}} // end of method '<>c'::.ctor
	.method assembly hidebysig 
		instance string '<Main>b__1_0' (
			string s1,
			[opt] string s2,
			[opt] string s3
		) cil managed 
	{{
		.param [2] = ""b""
		.param [3] = ""c""
		// Method begins at RVA 0x20df
		// Code size 9 (0x9)
		.maxstack 8
		IL_0000: ldarg.1
		IL_0001: ldarg.2
		IL_0002: ldarg.3
		IL_0003: call string [{s_libPrefix}]System.String::Concat(string, string, string)
		IL_0008: ret
	}} // end of method '<>c'::'<Main>b__1_0'
}} // end of class <>c
";

            var verifier = CompileAndVerify(source, expectedOutput: "abc <>f__AnonymousDelegate0`4[System.String,System.String,System.String,System.String]");
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`4", expectAnonymousDelegateIL);
            verifier.VerifyTypeIL("<>c", expectLoweredClosureContainerIL);
        }

        [Fact]
        public void LambdaWithIdenticalSignatureDifferentDefaultValue()
        {
            var source = """
using System;

class Program
{
    public static void Report(object obj) => Console.WriteLine(obj.GetType()); 
    public static void Main()
    {
        var lam1 = (int x = 10) => x + x;
        var lam2 = (int x = 20) => x + x;
        Report(lam1);
        Report(lam2);
    }
}
""";
            CompileAndVerify(source, expectedOutput:
@"<>f__AnonymousDelegate0`2[System.Int32,System.Int32]
<>f__AnonymousDelegate1`2[System.Int32,System.Int32]
");
        }

        [Fact]
        public void LambdaWithIdenticalSignatureIdenticalDefaultValue()
        {
            var source = """
using System;

class Program
{
    public static void Report(object obj) => Console.WriteLine(obj.GetType()); 
    public static void Main()
    {
        var lam1 = (int x = 10) => x + x;
        var lam2 = (int x = 10) => x + 1;
        Report(lam1);
        Report(lam2);
    }
}
""";
            CompileAndVerify(source, expectedOutput:
 @"<>f__AnonymousDelegate0`2[System.Int32,System.Int32]
<>f__AnonymousDelegate0`2[System.Int32,System.Int32]");
        }

        [Fact]
        public void LambdaWithIdenticalSignatureOptionalMismatch()
        {
            var source = """
using System;

class Program
{
    public static void Report(object obj) => Console.WriteLine(obj.GetType()); 
    public static void Main()
    {
        var lam1 = (int x = 10) => x + x;
        var lam2 = (int x) => x + 1;
        Report(lam1);
        Report(lam2);
    }
}
""";
            CompileAndVerify(source, expectedOutput:
@"<>f__AnonymousDelegate0`2[System.Int32,System.Int32]
System.Func`2[System.Int32,System.Int32]
");

        }

        [Fact]
        public void LambdaIdenticalArityIdenticalDefaultParamDifferentRequiredParams()
        {
            var source = """
using System;

class Program
{
    public static void Report(object obj) => Console.WriteLine(obj.GetType()); 
    public static void Main()
    {
        var lam1 = (double d, int x = 10) => { };
        var lam2 = (string s, int x = 10) => { };
        Report(lam1);
        Report(lam2);
    }
}
""";
            CompileAndVerify(source, expectedOutput:
@"<>f__AnonymousDelegate0`2[System.Double,System.Int32]
<>f__AnonymousDelegate0`2[System.String,System.Int32]");
        }

        [Fact]
        public void LambdaConversionDefaultParameterValueMismatch()
        {
            var source = """
using System;

class Program
{
    public static void Report(object obj) => Console.WriteLine(obj.GetType()); 
    public static void Main()
    {
        var lam1 = (int x = 10) => x + x;
        var lam2 = (int x = 20) => x + 1;
        lam1 = lam2;
        lam1();
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,16): error CS0029: Cannot implicitly convert type '<anonymous delegate>' to '<anonymous delegate>'
                //         lam1 = lam2;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "lam2").WithArguments("<anonymous delegate>", "<anonymous delegate>").WithLocation(10, 16));
        }

        [Fact]
        public void LambdaDefaultParameterNameMismatch()
        {
            var source = """
using System;

class Program
{
    public static void Report(object obj) => Console.WriteLine(obj.GetType()); 
    public static void Main()
    {
        var lam1 = (int x = 10) => x + x;
        var lam2 = (int a = 10) => a + a;
        lam1 = lam2;
        Console.WriteLine(lam1());
    }
}
""";
            CompileAndVerify(source, expectedOutput: "20");
        }

        [Fact]
        public void LambdaWithDefaultNamedDelegateConversion_DefaultValueMatch()
        {
            var source = """
using System;

class Program
{
    delegate int D(int x = 1);
    public static void Main()
    {
        D d = (int x = 1) => x + x;
        Console.WriteLine(d());
    }
}
""";
            CompileAndVerify(source, expectedOutput: "2");
        }

        [Fact]
        public void LambdaWithDefaultNamedDelegateConversion_DefaultValueMismatch()
        {
            var source = """
using System;

class Program
{
    delegate int D(int x = 1);
    public static void Main()
    {
        D d = (int x = 1000) => x + x;
        Console.WriteLine(d());
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,20): warning CS9099: Parameter 1 has default value '1000' in lambda but '1' in the target delegate type.
                //         D d = (int x = 1000) => x + x;
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "1000", "1").WithLocation(8, 20));
        }

        [Fact]
        public void LambdaWithDefaultNamedDelegateConversion_DefaultValueMismatch_WithParameterError()
        {

            var source = """
using System;

class Program
{
    delegate int D(int x = 1);
    static int f(int x) => 2 * x;

    public static void Main()
    {
        D d = (int x = f(1)) => x + x;
        Console.WriteLine(d());
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,24): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //         D d = (int x = f(1)) => x + x;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "f(1)").WithArguments("x").WithLocation(10, 24));
        }

        [Fact]
        public void LambdaWithDefaultNamedDelegateConversion_TargetMissingOptional()
        {
            var source = """
class Program
{
    // Named delegate has required parameter x
    delegate int D(int x);
    public static void Main()
    {
        // lambda has optional parameter x
        D d = (int x = 1000) => x + x;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,20): warning CS9099: Parameter 1 has default value '1000' in lambda but '<missing>' in the target delegate type.
                //         D d = (int x = 1000) => x + x;
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "1000", "<missing>").WithLocation(8, 20));
        }

        [Fact]
        public void LambdWithDefaultNamedDelegateConversion_LambdaMissingOptional()
        {
            var source = """
class Program
{
    delegate int D(int x = 3);
    public static void Main()
    {
        D d = (int x) => x;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void LambdaWithDefaultNamedDelegateConversion_TargetDelegateMissingOptionalParameter_WithParameterError()
        {
            var source = """
class Program
{
    // Named delegate has required parameter x
    delegate int D(int x);
    public static int f(int x) => 2 * x;
    public static void Main()
    {
        // lambda has optional parameter x
        D d = (int x = f(1000)) => x + x;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,24): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //         D d = (int x = f(1000)) => x + x;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "f(1000)").WithArguments("x").WithLocation(9, 24));
        }

        [Fact]
        public void LambdaOptionalBeforeRequiredBadConversion()
        {

            var source = """
class Program
{
    public static void Main()
    {
        // lambda has optional parameter y
        var lam1 = (int x, int y = 10, int z) => x * x + y * y + z * z;
        var lam2 = (int x, int y, int z) => x * x + y * y + z * z;

        lam2 = lam1;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,45): error CS1737: Optional parameters must appear after all required parameters
                //         var lam1 = (int x, int y = 10, int z) => x * x + y * y + z * z;
                Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ")").WithLocation(6, 45),
                // (9,16): error CS0029: Cannot implicitly convert type '<anonymous delegate>' to 'System.Func<int, int, int, int>'
                //         lam2 = lam1;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "lam1").WithArguments("<anonymous delegate>", "System.Func<int, int, int, int>").WithLocation(9, 16));
        }

        [Fact]
        public void LambdaRequiredBetweenOptionalsParameters()
        {
            var source = """
    class Program
    {
        public static void Main()
        {
            var lam = (string s1 = null, string s2, string s3, string s4, string s5 = "") => s5;
        }
    }
    """;
            CreateCompilation(source).VerifyDiagnostics(
                // (5,47): error CS1737: Optional parameters must appear after all required parameters
                //         var lam = (string s1 = null, string s2, string s3, string s4, string s5 = "") => s5;
                Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ",").WithLocation(5, 47),
                // (5,58): error CS1737: Optional parameters must appear after all required parameters
                //         var lam = (string s1 = null, string s2, string s3, string s4, string s5 = "") => s5;
                Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ",").WithLocation(5, 58),
                // (5,69): error CS1737: Optional parameters must appear after all required parameters
                //         var lam = (string s1 = null, string s2, string s3, string s4, string s5 = "") => s5;
                Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ",").WithLocation(5, 69));
        }

        [Fact]
        public void LambdaWithDefaultInvalidTargetTypeConversion_01()
        {
            var source = """
    class Program
    {
        delegate double D(int x, int d = 3);
        public static void Main()
        {
            D d = (int x, double d = 3.0) => x + d;
        }
    }
    """;
            CreateCompilation(source).VerifyDiagnostics(
                // (6,30): error CS1678: Parameter 2 is declared as type 'double' but should be 'int'
                //         D d = (int x, double d = 3.0) => x + d;
                Diagnostic(ErrorCode.ERR_BadParamType, "d").WithArguments("2", "", "double", "", "int").WithLocation(6, 30),
                // (6,39): error CS1661: Cannot convert lambda expression to type 'Program.D' because the parameter types do not match the delegate parameter types
                //         D d = (int x, double d = 3.0) => x + d;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "Program.D").WithLocation(6, 39));
        }

        [Fact]
        public void LambdaWithInvalidDefaultValidTargetTypeConversion_02()
        {
            var source = """
    class A
    { }

    class B : A
    { }

    class Program
    {
        delegate double D(int x, B b = null);
        public static void Main()
        {
            D d = (int x, A a = null) => { };
        }
    }
    """;
            CreateCompilation(source).VerifyDiagnostics(
                // (12,25): error CS1678: Parameter 2 is declared as type 'A' but should be 'B'
                //         D d = (int x, A a = null) => { };
                Diagnostic(ErrorCode.ERR_BadParamType, "a").WithArguments("2", "", "A", "", "B").WithLocation(12, 25),
                // (12,35): error CS1661: Cannot convert lambda expression to type 'Program.D' because the parameter types do not match the delegate parameter types
                //         D d = (int x, A a = null) => { };
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "Program.D").WithLocation(12, 35));
        }

        [Fact]
        public void LambdaWithDefaultsAndRefParameters()
        {

            var source = """
    using System;

    class Program
    {
        static void Report(object d) => Console.WriteLine(d.GetType());
        public static void Main()
        {
            int x = 9;
            var lam = (ref int x, out int y, int z = 3) => { y = x + z; };
            lam(ref x, out var y);
            lam(ref x, out var w, 20);

            Console.WriteLine(y);
            Console.WriteLine(w);
            Report(lam);
        }
    }
    """;
            var expectAnonymousDelegateIL =
$@"
    .class private auto ansi sealed '<>f__AnonymousDelegate0`3'<T1, T2, T3>
	extends [{s_libPrefix}]System.MulticastDelegate
{{
	.custom instance void [{s_libPrefix}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			object 'object',
			native int 'method'
		) runtime managed 
	{{
	}} // end of method '<>f__AnonymousDelegate0`3'::.ctor
	.method public hidebysig newslot virtual 
		instance void Invoke (
			!T1& arg1,
			[out] !T2& arg2,
			[opt] !T3 arg3
		) runtime managed 
	{{
		.param [3] = int32(3)
	}} // end of method '<>f__AnonymousDelegate0`3'::Invoke
}} // end of class <>f__AnonymousDelegate0`3
";

            var expectLoweredClosureContainerIL =
$@"
.class nested private auto ansi sealed serializable beforefieldinit '<>c'
	extends [{s_libPrefix}]System.Object
{{
	.custom instance void [{s_libPrefix}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Fields
	.field public static initonly class Program/'<>c' '<>9'
	.field public static class '<>f__AnonymousDelegate0`3'<int32, int32, int32> '<>9__1_0'
	// Methods
	.method private hidebysig specialname rtspecialname static 
		void .cctor () cil managed 
	{{
		// Method begins at RVA 0x20d3
		// Code size 11 (0xb)
		.maxstack 8
		IL_0000: newobj instance void Program/'<>c'::.ctor()
		IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
		IL_000a: ret
	}} // end of method '<>c'::.cctor
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{{
		// Method begins at RVA 0x20cb
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [{s_libPrefix}]System.Object::.ctor()
		IL_0006: ret
	}} // end of method '<>c'::.ctor
	.method assembly hidebysig 
		instance void '<Main>b__1_0' (
			int32& x,
			[out] int32& y,
			[opt] int32 z
		) cil managed 
	{{
		.param [3] = int32(3)
		// Method begins at RVA 0x20df
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.2
		IL_0001: ldarg.1
		IL_0002: ldind.i4
		IL_0003: ldarg.3
		IL_0004: add
		IL_0005: stind.i4
		IL_0006: ret
	}} // end of method '<>c'::'<Main>b__1_0'
}} // end of class <>c
";

            var verifier = CompileAndVerify(source, expectedOutput:
 @"12
29
<>f__AnonymousDelegate0`3[System.Int32,System.Int32,System.Int32]");
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`3", expectAnonymousDelegateIL);
            verifier.VerifyTypeIL("<>c", expectLoweredClosureContainerIL);
        }

        [Fact]
        public void LambdaOutOfOrderParameterInvocation_AllParametersSpecified()
        {
            var source = """
using System;

class Program
{
    public static void Main()
    {
        var lam = (string a, string b, string c = "c") => $"{a}{b}{c}";
        Console.WriteLine(lam(b: "b", c: "a", a: "c"));
    }
 }
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,31): error CS1746: The delegate '<anonymous delegate>' does not have a parameter named 'b'
                //         Console.WriteLine(lam(b: "b", c: "a", a: "c"));
                Diagnostic(ErrorCode.ERR_BadNamedArgumentForDelegateInvoke, "b").WithArguments("<anonymous delegate>", "b").WithLocation(8, 31));
        }

        [Fact]
        public void LambdaOutOfOrderParameterInvocation_MissingOptionalParameter()
        {
            var source = """
using System;

class Program
{
    public static void Main()
    {
        var lam = (string a, string b, string c = "c") => $"{a}{b}{c}";
        Console.WriteLine(lam(b: "a", a: "b"));
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,31): error CS1746: The delegate '<anonymous delegate>' does not have a parameter named 'b'
                //         Console.WriteLine(lam(b: "a", a: "b"));
                Diagnostic(ErrorCode.ERR_BadNamedArgumentForDelegateInvoke, "b").WithArguments("<anonymous delegate>", "b").WithLocation(8, 31));
        }

        [Fact]
        public void LambdaOptionalParameterDecimalExpression()
        {
            var source = """
using System;
using System.Globalization;

class Program
{
    public static void Main()
    {
        var lam = (decimal dec =  Decimal.One / (decimal) 3) => dec; 
        Console.WriteLine(lam().ToString(CultureInfo.InvariantCulture));
    }

}
""";
            CompileAndVerify(source, expectedOutput: "0.3333333333333333333333333333");
        }

        [Fact]
        public void CallerAttributesOnLambdaWithDefaultParam()
        {
            var source = """
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        var lam = ([CallerMemberName] string member = "member", [CallerFilePath] string filePath = "file", [CallerLineNumber] int lineNumber = 0) => Console.WriteLine($"{filePath}::{member}:{lineNumber}");
        lam();
    }
}
""";
            var verifier = CompileAndVerify(source, expectedOutput: "file::member:0");
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`3", $$"""
                .class private auto ansi sealed '<>f__AnonymousDelegate0`3'<T1, T2, T3>
                	extends [{{s_libPrefix}}]System.MulticastDelegate
                {
                	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	// Methods
                	.method public hidebysig specialname rtspecialname 
                		instance void .ctor (
                			object 'object',
                			native int 'method'
                		) runtime managed 
                	{
                	} // end of method '<>f__AnonymousDelegate0`3'::.ctor
                	.method public hidebysig newslot virtual 
                		instance void Invoke (
                			[opt] !T1 arg1,
                			[opt] !T2 arg2,
                			[opt] !T3 arg3
                		) runtime managed 
                	{
                		.param [1] = "member"
                		.param [2] = "file"
                		.param [3] = int32(0)
                	} // end of method '<>f__AnonymousDelegate0`3'::Invoke
                } // end of class <>f__AnonymousDelegate0`3
                """);
        }

        [Fact]
        public void CallerArgumentExpressionAttributeOnLambdaWithDefaultParam()
        {
            var source = """
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main()
    {
        var lam = (int arg, [CallerArgumentExpression("arg")] string argExpression = "callerArgExpression") => Console.WriteLine($"{argExpression}");
        lam(3);
    }
}
""";
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net60,
                verify: ExecutionConditionUtil.IsCoreClr ? Verification.Passes : Verification.Skipped,
                expectedOutput: ExecutionConditionUtil.IsCoreClr ? "callerArgExpression" : null);
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`2", """
                .class private auto ansi sealed '<>f__AnonymousDelegate0`2'<T1, T2>
                	extends [System.Runtime]System.MulticastDelegate
                {
                	.custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	// Methods
                	.method public hidebysig specialname rtspecialname 
                		instance void .ctor (
                			object 'object',
                			native int 'method'
                		) runtime managed 
                	{
                	} // end of method '<>f__AnonymousDelegate0`2'::.ctor
                	.method public hidebysig newslot virtual 
                		instance void Invoke (
                			!T1 arg1,
                			[opt] !T2 arg2
                		) runtime managed 
                	{
                		.param [2] = "callerArgExpression"
                	} // end of method '<>f__AnonymousDelegate0`2'::Invoke
                } // end of class <>f__AnonymousDelegate0`2
                """);
        }

        [Fact]
        public void CallerInfoAttributes_Lambda_NoDefaultValue()
        {
            var source = """
                using System.Runtime.CompilerServices;
                var lam1 = ([CallerMemberName] string member, [CallerFilePath] string filePath, [CallerLineNumber] int lineNumber) => { };
                var lam2 = (int arg, [CallerArgumentExpression("arg")] string argExpression) => { };
                """;
            CreateCompilation(source, targetFramework: TargetFramework.Net60).VerifyDiagnostics(
                // (2,14): error CS4022: The CallerMemberNameAttribute may only be applied to parameters with default values
                // var lam1 = ([CallerMemberName] string member, [CallerFilePath] string filePath, [CallerLineNumber] int lineNumber) => { };
                Diagnostic(ErrorCode.ERR_BadCallerMemberNameParamWithoutDefaultValue, "CallerMemberName").WithLocation(2, 14),
                // (2,48): error CS4021: The CallerFilePathAttribute may only be applied to parameters with default values
                // var lam1 = ([CallerMemberName] string member, [CallerFilePath] string filePath, [CallerLineNumber] int lineNumber) => { };
                Diagnostic(ErrorCode.ERR_BadCallerFilePathParamWithoutDefaultValue, "CallerFilePath").WithLocation(2, 48),
                // (2,82): error CS4020: The CallerLineNumberAttribute may only be applied to parameters with default values
                // var lam1 = ([CallerMemberName] string member, [CallerFilePath] string filePath, [CallerLineNumber] int lineNumber) => { };
                Diagnostic(ErrorCode.ERR_BadCallerLineNumberParamWithoutDefaultValue, "CallerLineNumber").WithLocation(2, 82),
                // (3,23): error CS8964: The CallerArgumentExpressionAttribute may only be applied to parameters with default values
                // var lam2 = (int arg, [CallerArgumentExpression("arg")] string argExpression) => { };
                Diagnostic(ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue, "CallerArgumentExpression").WithLocation(3, 23));
        }

        [Fact]
        public void LambdaDefaultParameterMatchesDelegateAfterBinding()
        {
            var source = """
using System;

class Program
{
    delegate int D(int x = 7);
    const int num = 4;

    public static void Main()
    {
        D d = (int x = num + 3) => x;
        Console.WriteLine(d());
    }
}
""";
            CompileAndVerify(source, expectedOutput: "7");
        }

        [Fact]
        public void ImplicitLambdaDefaultParameter_NamedDelegateConversion()
        {
            var source = """
using System;

class Program
{
    delegate int D(int x = 3);
    public static void Main()
    {
        D d = (x = 3) => x;
        Console.WriteLine(d());
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,16): error CS9098: Implicitly typed lambda parameter 'x' cannot have a default value.
                //         D d = (x = 3) => x;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedDefaultParameter, "x").WithArguments("x").WithLocation(8, 16));
        }

        [Fact]
        public void SimpleMethodGroupInference_DefaultParameter()
        {
            var source = """
using System;

class Program
{
    public static void Report(object d) => Console.WriteLine(d.GetType());

    public static int M(int arg = 1)
    {
        return arg;
    }

    public static void Main()
    {
        var f = M;
        int x = f();
        Console.WriteLine(x);
        Report(f);
    }
}
""";

            CompileAndVerify(source, expectedOutput:
@"1
<>f__AnonymousDelegate0`2[System.Int32,System.Int32]");
        }

        [Fact]
        public void InstanceMethodGroupInference_DefaultParameter()
        {
            var source = """
using System;

class C
{
    public int Z;
    public void SetZ(int x = 10)
    {
        this.Z = x;
    }
}

class Program
{
    
    public static void Main()
    {
        C c = new C();
        var setZ = c.SetZ;
        setZ();
        Console.WriteLine(c.Z);
        setZ(7);
        Console.WriteLine(c.Z);
    }
}
""";

            CompileAndVerify(source, expectedOutput:
@"10
7");
        }

        [Fact]
        public void MethodGroupInferenceMatchingSignaturesAndDefaultParameterValues()
        {
            var source = """
using System;

class Program
{
    public static void Report(object d) => Console.WriteLine(d.GetType());

    public static int M(int x = 3)
    {
        return x;
    }

    public static int N(int y = 3)
    {
        return y * 100;
    }

    public static void Main()
    {
        var m = M;
        var n = N;

        Report(m);
        Report(n);
    }
}
""";
            CompileAndVerify(source, expectedOutput:
@"<>f__AnonymousDelegate0`2[System.Int32,System.Int32]
<>f__AnonymousDelegate0`2[System.Int32,System.Int32]");
        }

        [Fact]
        public void MethodGroupInference_SignaturesMatchDefaultParameterMismatch()
        {
            var source = """
using System;

class Program
{
    public static void Report(object d) => Console.WriteLine(d.GetType());

    public static int M(int x = 3)
    {
        return x;
    }

    public static int N(int y = 4)
    {
        return y * 100;
    }

    public static void Main()
    {
        var m = M;
        var n = N;

        Report(m);
        Report(n);
    }
}
""";
            CompileAndVerify(source, expectedOutput:
@"<>f__AnonymousDelegate0`2[System.Int32,System.Int32]
<>f__AnonymousDelegate1`2[System.Int32,System.Int32]");
        }

        [Fact]
        public void MethodGroupTargetConversion_DefaultValueMatch()
        {
            var source = """
using System;

class Program
{
    delegate string D(string s = "defaultstring");

    public static string M(string s = "defaultstring")
    {
        return s;
    }

    public static void Main()
    {
        D d = M;
        Console.WriteLine(d());
    }
}
""";
            CompileAndVerify(source, expectedOutput: "defaultstring");
        }

        [Fact]
        public void MethodGroupTargetConversion_DefaultValueMismatch()
        {
            var source = """
using System;

class Program
{
    delegate string D(string s = "string1");

    public static string M(string s = "string2")
    {
        return s;
    }

    public static void Main()
    {
        D d = M;
        Console.WriteLine(d());
    }
}
""";
            CompileAndVerify(source, expectedOutput: "string1").VerifyDiagnostics();
        }

        [Fact]
        public void MethodGroupTargetConversion_ParameterOptionalInMethodGroupOnly()
        {
            var source = """
using System;

class Program
{
    delegate string D(string s);

    public static string M(string s = "a string")
    {
        return s;
    }

    public static void Main()
    {
        D d = M;
        Console.WriteLine(d("my string"));
    }
}
""";
            CompileAndVerify(source, expectedOutput: "my string").VerifyDiagnostics();
        }

        [Fact]
        public void MethodGroupTargetConversion_ParameterOptionalInDelegateOnly()
        {
            var source = """
using System;

class Program
{
    delegate string D(string s = "string1");

    public static string M(string s) => s;

    public static void Main()
    {
        D d = M;
        Console.WriteLine(d());
    }
}
""";
            CompileAndVerify(source, expectedOutput: "string1").VerifyDiagnostics();
        }

        [Fact]
        public void MethodGroup_NamedDelegateConversion_MultipleValueMismatches()
        {
            var source = """
class Program
{
    delegate int Del(int x, string s = "a", long l = 0L);
    static int M(int x = 40, string s = "b", long l = 1) => x;

    public static void Main()
    {
        Del del = M;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionMethodGroup_DefaultParameters_TargetTypeConversion()
        {
            var source = """
using System;
using ProgramExtensions;

namespace ProgramExtensions {
    public static class ProgramExtensions
    {
        public static string M(this Program p, string s = "b", long l = 1L) => $"{p.field} {s} {l}";
    }
}

public class Program
{
    public int field = 10;
    delegate string Del(string s = "a", long l = 0L);
    public string M1(string s = "c", long l = 2L) => $"{this.field} {s} {l}";

    public static void Main()
    {
        Program prog = new Program();
        Del del = prog.M;
        Console.WriteLine(del());
    }
}
""";

            var verifier = CompileAndVerify(source);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Main",
@"
 {
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  ldftn      ""string ProgramExtensions.ProgramExtensions.M(Program, string, long)""
  IL_000b:  newobj     ""Program.Del..ctor(object, System.IntPtr)""
  IL_0010:  ldstr      ""a""
  IL_0015:  ldc.i4.0
  IL_0016:  conv.i8
  IL_0017:  callvirt   ""string Program.Del.Invoke(string, long)""
  IL_001c:  call       ""void System.Console.WriteLine(string)""
  IL_0021:  ret
}");
        }

        [Fact]
        public void ExtensionMethodGroup_DefaultParameters_TargetTypeConversion_02()
        {
            var source = """
namespace ProgramExtensions {
    public static class PExt
    {
        public static string M(this Program p, string s = "a", long l = 0L) => $"{p.field} {s} {l}";
    }
}

public class Program
{
    public int field = 10;
    delegate string Del(string s = "a", long l = 0L);

    public static void Main()
    {
        Del del = ProgramExtensions.PExt.M;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,42): error CS0123: No overload for 'M' matches delegate 'Program.Del'
                //         Del del = ProgramExtensions.PExt.M;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M").WithArguments("M", "Program.Del").WithLocation(15, 42));
        }

        [Fact]
        public void ExtensionMethodGroup_DefaultParameters_TargetTypeConversion_03()
        {
            var source = """
using System;

namespace ProgramExtensions {
    public static class PExt
    {
        public static string M(this Program p, string s = "b", long l = 1) => $"{p.Field} {s} {l}";
    }
}

public class Program
{
    public int Field = 10;
    delegate string Del(Program p, string s = "a", long l = 0L);

    public static void Main()
    {
        Del del = ProgramExtensions.PExt.M;
        Console.WriteLine(del(new Program() { Field = -1 }));
    }
}
""";
            CompileAndVerify(source, expectedOutput: "-1 a 0").VerifyDiagnostics();
        }

        [Fact]
        public void InstanceMethodGroup_DefaultParameters_TargetTypeConversion()
        {
            var source = """
using System;

public class Program
{
    public int field = 10;
    delegate string Del(string s = "a", long l = 0L);
    public string M1(string s = "c", long l = 2L) => $"{this.field} {s} {l}";

    public static void Main()
    {
        Program prog = new Program();
        Del del = prog.M1;
        Console.WriteLine(del());
    }
}
""";
            var verifier = CompileAndVerify(source, expectedOutput: @"10 a 0");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionMethodGroup_DefaultParameters_InferredType()
        {
            var source = """
using System;
using ProgramExtensions;

namespace ProgramExtensions {
    public static class PExt
    {
        public static string M(this Program p, string s = "b", long l = 1) => $"{p.Field} {s} {l}";
    }
}

public class Program
{
    public int Field = 10;
    public static void Report(object obj) => Console.WriteLine(obj.GetType());

    public static void Main()
    {
        var m = ProgramExtensions.PExt.M;
        var n = (new Program()).M;
        Report(m);
        Report(n);
        Console.WriteLine(m(new Program() { Field = 20 }));
        Console.WriteLine(n());
    }
}
""";

            CompileAndVerify(source, expectedOutput:
@"<>f__AnonymousDelegate0`4[Program,System.String,System.Int64,System.String]
<>f__AnonymousDelegate1`3[System.String,System.Int64,System.String]
20 b 1
10 b 1");
        }

        [Fact]
        public void MethodGroupInferenceCompatBreak()
        {
            var source = """
using System;

class Program
{
    public static int Fun(int arg = 10) => arg + 1;

    public static void PrintFunResult<T>(Func<T, T> f, T input)
    {
        Console.WriteLine(f(input));
    }

    public static void Main()
    {
        var f = Fun;
        PrintFunResult(f, 3);
    }

}
""";
            CreateCompilation(source).VerifyDiagnostics(
                    // (15,24): error CS1503: Argument 1: cannot convert from '<anonymous delegate>' to 'System.Func<int, int>'
                    //         PrintFunResult(f, 3);
                    Diagnostic(ErrorCode.ERR_BadArgType, "f").WithArguments("1", "<anonymous delegate>", "System.Func<int, int>").WithLocation(15, 24));
        }

        [Fact]
        public void LambdaDefaultDiscardParameter_DelegateConversion_OptionalRequiredMismatch()
        {
            var source = """
using System;

class Program
{
    delegate int D(int x, int y);
    public static void Main()
    {
        D d = (int _, int _ = 3) => 10;
        Console.WriteLine(d(4));
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,27): warning CS9099: Parameter 2 has default value '3' in lambda but '<missing>' in the target delegate type.
                //         D d = (int _, int _ = 3) => 10;
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "_").WithArguments("2", "3", "<missing>").WithLocation(8, 27),
                // (9,27): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'Program.D'
                //         Console.WriteLine(d(4));
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "d").WithArguments("y", "Program.D").WithLocation(9, 27));
        }

        [Fact]
        public void LambdaDefaultDiscardParameter_DelegateConversion_DefaultValueMismatch()
        {
            var source = """
using System;

class Program
{
    delegate int D(int x, int y = 7);
    public static void Main()
    {
        D d = (int _, int _ = 3) => 10;
        Console.WriteLine(d(4));
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,27): warning CS9099: Parameter 2 has default value '3' in lambda but '7' in the target delegate type.
                //         D d = (int _, int _ = 3) => 10;
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "_").WithArguments("2", "3", "7").WithLocation(8, 27));
        }

        [Fact]
        public void LambdaDefaultParameter_TargetTypeConversionWarning_ErrorInLambdaBody()
        {
            var source = """
using System;

class Program
{
    delegate void D(int x, int y);
    public static void Main()
    {
        D d = (int x, int y = 4) => {
            string s = 5;
        };

        Console.WriteLine(d(4));
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,24): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //             string s = 5;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "5").WithArguments("int", "string").WithLocation(9, 24),
                // (12,27): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'Program.D'
                //         Console.WriteLine(d(4));
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "d").WithArguments("y", "Program.D").WithLocation(12, 27));
        }

        [Fact]
        public void Lambda_DiscardParameters()
        {
            var source = """
                var lam1 = (string _, int _ = 1) => { };
                lam1("s", 2);
                var lam2 = (string _, params int[] _) => { };
                lam2("s", 3, 4, 5);
                """;
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void MethodGroup_LambdaAssignment_DefaultParameterMismatch_01()
        {
            var source = """
using System;

class Program
{
    public static int M(int i = 3) => i;
    public static void Main()
    {
        var m = M;
        m = (int i = 4) => i;
        Console.WriteLine(m());
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,18): warning CS9099: Parameter 1 has default value '4' in lambda but '3' in the target delegate type.
                //         m = (int i = 4) => i;
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "i").WithArguments("1", "4", "3").WithLocation(9, 18));
        }

        [Fact]
        public void MethodGroup_LambdaAssignment_DefaultParameterMismatch_02()
        {
            var source = """
using System;

class Program
{
    public static int M(int i) => i;
    public static void Main()
    {
        var m = M;
        m = (int i = 4) => i;
        Console.WriteLine(m());
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,18): warning CS9099: Parameter 1 has default value '4' in lambda but '<missing>' in the target delegate type.
                //         m = (int i = 4) => i;
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "i").WithArguments("1", "4", "<missing>").WithLocation(9, 18),
                // (10,27): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of 'Func<int, int>'
                //         Console.WriteLine(m());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "m").WithArguments("arg", "System.Func<int, int>").WithLocation(10, 27));
        }

        [Fact]
        public void MethodGroup_LambdaAssignment_DefaultParameterValueMismatch_03()
        {
            var source = """
using System;

class Program
{
    public static int M(int i = 4) => i;
    public static void Main()
    {
        var m = M;
        m = (int x) => x; 
        Console.WriteLine(m());
    }
}
""";
            CompileAndVerify(source, expectedOutput: "4").VerifyDiagnostics();
        }

        [Fact]
        public void MethodGroup_LambdaAssignment_DefaultParameterValueMatch()
        {
            var source = """
using System;

class Program
{
    public static int M(int i = 3) => i;
    public static void Main()
    {
        var m = M;
        m = (int x = 3) => x; 
        Console.WriteLine(m());
    }
}
""";
            CompileAndVerify(source, expectedOutput: "3").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("sbyte")]
        [InlineData("byte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("long")]
        [InlineData("ulong")]
        [InlineData("nint")]
        [InlineData("nuint")]
        [InlineData("float")]
        [InlineData("double")]
        [InlineData("decimal")]
        [InlineData("E", "E.FIELD", "FIELD")]
        [InlineData("bool", "true", "True")]
        [InlineData("char", "'a'", "a")]
        [InlineData("string", @"""a string""", "a string")]
        [InlineData("C", "null", "")]
        [InlineData("C", "default(C)", "")]
        [InlineData("C", "default", "")]
        [InlineData("S", "new S()", "Program+S")]
        [InlineData("S", "default(S)", "Program+S")]
        [InlineData("S", "default", "Program+S")]
        public void LambdaDefaultParameter_AllConstantValueTypes(string parameterType, string defaultValue = "0", string expectedOutput = "0")
        {
            var source = $$"""
using System;
public class Program
{
    public enum E 
    {
        FIELD
    }
    
    class C {}

    struct S {}

    public static void Main()
    {
        var lam = ({{parameterType}} p = {{defaultValue}}) => p;
        Console.WriteLine(lam());
    }
}
""";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void LambdaDefaultParameter_TargetTypedValidLiteralConversion()
        {
            var source = """
                var lam = (short s = 1) => { };
                """;
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultParameter_TargetTypeInvalidLiteralConversion()
        {
            var source = """
                var lam = (short s = 32768) => { };
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,18): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type 'short'
                // var lam = (short s = 32768) => { };
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "s").WithArguments("int", "short").WithLocation(1, 18));
        }

        [Fact]
        public void LambdaDefaultParameter_TargetTypedValidNonLiteralConversion()
        {
            var source = """
                const float floatConst = 1f;
                var lam = (double d = floatConst) => { };
                """;
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultParameter_InterpolatedStringHandler()
        {
            var source = """
using System;

public class Program
{
     public static void Main()
     {
        int i = 0;
        var lam = (CustomHandler h = $"i: {i}") =>
        {
            Console.WriteLine(h.ToString());
        };
        lam();
     }
}
""";
            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "struct", useBoolReturns: false);

            CreateCompilation(new[] { source, handler }).VerifyDiagnostics(
                // (8,38): error CS1736: Default parameter value for 'h' must be a compile-time constant
                //         var lam = (CustomHandler h = $"i: {i}") =>
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @"$""i: {i}""").WithArguments("h").WithLocation(8, 38));
        }

        [Fact]
        public void LambdaWithDefault_InvalidConstantConversion()
        {
            var source = """
                var lam = (string s = 1) => { };
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,19): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type 'string'
                // var lam = (string s = 1) => { };
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "s").WithArguments("int", "string").WithLocation(1, 19));
        }

        [Fact]
        public void LambdaWithDefault_NonConstantNonLiteral()
        {
            var source = """
class Program
{
    // Named delegate has required parameter x
    public static int f(int x) => 2 * x;
    public static void Main()
    {
        // lambda has optional parameter x
        var lam = (int x = f(1000)) => { };
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,28): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //         var lam = (int x = f(1000)) => { };
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "f(1000)").WithArguments("x").WithLocation(8, 28));
        }

        [Fact]
        public void LambdaWithDefault_NonConstantLiteral_InterpolatedString()
        {
            var source = """
class Program
{
    public static void Main()
    {
        int n = 42;
        // lambda has optional parameter x
        var lam = (string s = $"n: {n}") => { };
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,31): error CS1736: Default parameter value for 's' must be a compile-time constant
                //         var lam = (string s = $"n: {n}") => { };
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @"$""n: {n}""").WithArguments("s").WithLocation(7, 31));
        }

        [Fact]
        public void LambdaWithDefault_NonConstantLiteral_U8String()
        {
            var source = """
                var lam = (ReadOnlySpan<byte> s = "u8 string"u8) => { };
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,12): error CS0246: The type or namespace name 'ReadOnlySpan<>' could not be found (are you missing a using directive or an assembly reference?)
                // var lam = (ReadOnlySpan<byte> s = "u8 string"u8) => { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ReadOnlySpan<byte>").WithArguments("ReadOnlySpan<>").WithLocation(1, 12));
        }

        [Fact]
        public void LambdaWithDefault_EmbeddedType_Propagated()
        {
            var source1 = """
                using System.Runtime.InteropServices;
                [assembly: PrimaryInteropAssembly(0, 0)]
                [assembly: Guid("863D5BC0-46A1-49AC-97AA-A5F0D441A9DA")]
                [ComImport, Guid("863D5BC0-46A1-49AD-97AA-A5F0D441A9DA")]
                public interface MyEmbeddedType { }
                """;
            var comp1 = CreateCompilation(source1);
            var ref1 = comp1.EmitToImageReference(embedInteropTypes: true);

            var source2 = """
                var l = (MyEmbeddedType t = null) => {};
                """;
            var comp2 = CreateCompilation(source2, new[] { ref1 });
            CompileAndVerify(comp2, symbolValidator: static module =>
            {
                Assert.Contains("MyEmbeddedType", module.TypeNames);
            });
        }

        [Fact]
        public void LambdaWithDefault_EmbeddedType_NotPropagated_Default()
        {
            var source1 = """
                using System.Runtime.InteropServices;
                [assembly: PrimaryInteropAssembly(0, 0)]
                [assembly: Guid("863D5BC0-46A1-49AC-97AA-A5F0D441A9DA")]
                [ComImport, Guid("863D5BC0-46A1-49AD-97AA-A5F0D441A9DA")]
                public interface MyEmbeddedType { }
                """;
            var comp1 = CreateCompilation(source1);
            var ref1 = comp1.EmitToImageReference(embedInteropTypes: true);

            var source2 = """
                var l = (object o = default(MyEmbeddedType)) => {};
                """;
            var comp2 = CreateCompilation(source2, new[] { ref1 });
            CompileAndVerify(comp2, symbolValidator: static module =>
            {
                Assert.DoesNotContain("MyEmbeddedType", module.TypeNames);
            });
        }

        [Fact]
        public void LambdaWithParameterDefaultValueAttribute()
        {
            var source = """
using System;
using System.Runtime.InteropServices;

class Program
{
    static void Report(object obj) => Console.WriteLine(obj.GetType());
    public static void Main()
    {
        var lam = ([Optional, DefaultParameterValue(3)] int x) => x;
        int Method([Optional, DefaultParameterValue(3)] int x) => x;
        var inferred = Method;
        Console.WriteLine(lam());
        Console.WriteLine(Method());
        Console.WriteLine(inferred());
        Report(lam);
        Report(inferred);
    }
}
""";
            var verifier = CompileAndVerify(source, expectedOutput:
@" 3
3
3
<>f__AnonymousDelegate0`2[System.Int32,System.Int32]
<>f__AnonymousDelegate0`2[System.Int32,System.Int32]").VerifyDiagnostics();
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`2", $$"""
                .class private auto ansi sealed '<>f__AnonymousDelegate0`2'<T1, TResult>
                	extends [{{s_libPrefix}}]System.MulticastDelegate
                {
                	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	// Methods
                	.method public hidebysig specialname rtspecialname 
                		instance void .ctor (
                			object 'object',
                			native int 'method'
                		) runtime managed 
                	{
                	} // end of method '<>f__AnonymousDelegate0`2'::.ctor
                	.method public hidebysig newslot virtual 
                		instance !TResult Invoke (
                			[opt] !T1 arg
                		) runtime managed 
                	{
                		.param [1] = int32(3)
                	} // end of method '<>f__AnonymousDelegate0`2'::Invoke
                } // end of class <>f__AnonymousDelegate0`2
                """);
        }

        [Fact]
        public void LambdaWithParameterDefaultValueAttribute_NoOptional()
        {
            var source = """
                using System;
                using System.Runtime.InteropServices;

                var lam = ([DefaultParameterValue(3)] int x) => x;
                int Method([DefaultParameterValue(3)] int x) => x;
                var inferred = Method;
                AcceptFunc(lam);
                AcceptFunc(Method);
                AcceptFunc(inferred);
                lam();
                Method();
                inferred();

                void AcceptFunc(Func<int, int> f) { }
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (10,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of 'Func<int, int>'
                // lam();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "lam").WithArguments("arg", "System.Func<int, int>").WithLocation(10, 1),
                // (11,1): error CS7036: There is no argument given that corresponds to the required parameter 'x' of 'Method(int)'
                // Method();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Method").WithArguments("x", "Method(int)").WithLocation(11, 1),
                // (12,1): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of 'Func<int, int>'
                // inferred();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "inferred").WithArguments("arg", "System.Func<int, int>").WithLocation(12, 1));
        }

        [Fact]
        public void LambdaWithDefaultParameterValueAttribute_SynthesizedDelegateTypeMatch()
        {
            var source = """
                using System.Runtime.InteropServices;
                var lam1 = (int a = 1) => a;
                var lam2 = ([Optional] int b) => b;
                var lam3 = ([DefaultParameterValue(1)] int c) => c;
                var lam4 = ([Optional, DefaultParameterValue(1)] int d) => d;
                Report(lam1);
                Report(lam2);
                Report(lam3);
                Report(lam4);
                static void Report(object obj) => System.Console.WriteLine(obj.GetType());
                """;
            CompileAndVerify(source, expectedOutput: """
                <>f__AnonymousDelegate0`2[System.Int32,System.Int32]
                System.Func`2[System.Int32,System.Int32]
                System.Func`2[System.Int32,System.Int32]
                <>f__AnonymousDelegate0`2[System.Int32,System.Int32]
                """).VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultParameter_UnsafeNull()
        {
            var source = """
using System;

class Program
{
    public static unsafe void Main()
    {
        var lam = (int *ptr = null) => ptr;
        Console.WriteLine(lam() == (int*) null);
    }
}
""";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput: "True").VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultParameter_UnsafeSizeof()
        {
            var source = """
                using System;
                unsafe
                {
                    var lam = (int sz = sizeof(int)) => sz;
                    Console.WriteLine(lam());
                }
                """;
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, expectedOutput: "4").VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultParameter_Dynamic()
        {
            var source = """
using System;
class Program
{
    public static void Main()
    {
        var lam = (dynamic d = null) => { };
    }
}
""";
            var verifier = CompileAndVerify(source, expectedOutput: "");
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`1",
$$"""
.class private auto ansi sealed '<>f__AnonymousDelegate0`1'<T1>
	extends [{{s_libPrefix}}]System.MulticastDelegate
{
	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			object 'object',
			native int 'method'
		) runtime managed 
	{
	} // end of method '<>f__AnonymousDelegate0`1'::.ctor
	.method public hidebysig newslot virtual 
		instance void Invoke (
			[opt] !T1 arg
		) runtime managed 
	{
		.param [1] = nullref
	} // end of method '<>f__AnonymousDelegate0`1'::Invoke
} // end of class <>f__AnonymousDelegate0`1
""");
            verifier.VerifyTypeIL("<>c", $$"""
                .class nested private auto ansi sealed serializable beforefieldinit '<>c'
                	extends [{{s_libPrefix}}]System.Object
                {
                	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	// Fields
                	.field public static initonly class Program/'<>c' '<>9'
                	.field public static class '<>f__AnonymousDelegate0`1'<object> '<>9__0_0'
                	// Methods
                	.method private hidebysig specialname rtspecialname static 
                		void .cctor () cil managed 
                	{
                		// Method begins at RVA 0x208d
                		// Code size 11 (0xb)
                		.maxstack 8
                		IL_0000: newobj instance void Program/'<>c'::.ctor()
                		IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
                		IL_000a: ret
                	} // end of method '<>c'::.cctor
                	.method public hidebysig specialname rtspecialname 
                		instance void .ctor () cil managed 
                	{
                		// Method begins at RVA 0x2085
                		// Code size 7 (0x7)
                		.maxstack 8
                		IL_0000: ldarg.0
                		IL_0001: call instance void [{{s_libPrefix}}]System.Object::.ctor()
                		IL_0006: ret
                	} // end of method '<>c'::.ctor
                	.method assembly hidebysig 
                		instance void '<Main>b__0_0' (
                			[opt] object d
                		) cil managed 
                	{
                		.param [1] = nullref
                			.custom instance void [{{s_corePrefix}}]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = (
                				01 00 00 00
                			)
                		// Method begins at RVA 0x2099
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method '<>c'::'<Main>b__0_0'
                } // end of class <>c
                """);
        }

        [Fact]
        public void LambdaRefParameterWithDynamicParameter()
        {
            var source = """
using System;
class Program
{
    static void Report(object obj) => Console.WriteLine(obj.GetType());
    public static void Main()
    {
        var lam = (ref int i, dynamic d) => i;
        Report(lam);
    }
}
""";
            var verifier = CompileAndVerify(source);
            verifier.VerifyTypeIL(
                "<>F{00000001}`3",
$$"""
.class private auto ansi sealed '<>F{00000001}`3'<T1, T2, TResult>
	extends [{{s_libPrefix}}]System.MulticastDelegate
{
	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			object 'object',
			native int 'method'
		) runtime managed 
	{

	} // end of method '<>F{00000001}`3'::.ctor
	.method public hidebysig newslot virtual 
		instance !TResult Invoke (
			!T1& arg1,
			!T2 arg2
		) runtime managed 
	{
	} // end of method '<>F{00000001}`3'::Invoke
} // end of class <>F{00000001}`3
""");
            verifier.VerifyTypeIL("<>c",
$$"""
.class nested private auto ansi sealed serializable beforefieldinit '<>c'
	extends [{{s_libPrefix}}]System.Object
{
	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Fields
	.field public static initonly class Program/'<>c' '<>9'
	.field public static class '<>F{00000001}`3'<int32, object, int32> '<>9__1_0'
	// Methods
	.method private hidebysig specialname rtspecialname static 
		void .cctor () cil managed 
	{
		// Method begins at RVA 0x20a2
		// Code size 11 (0xb)
		.maxstack 8
		IL_0000: newobj instance void Program/'<>c'::.ctor()
		IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
		IL_000a: ret
	} // end of method '<>c'::.cctor
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x209a
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [{{s_libPrefix}}]System.Object::.ctor()
		IL_0006: ret
	} // end of method '<>c'::.ctor
	.method assembly hidebysig 
		instance int32 '<Main>b__1_0' (
			int32& i,
			object d
		) cil managed 
	{
		.param [2]
			.custom instance void [{{s_corePrefix}}]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = (
				01 00 00 00
			)
		// Method begins at RVA 0x20ae
		// Code size 3 (0x3)
		.maxstack 8
		IL_0000: ldarg.1
		IL_0001: ldind.i4
		IL_0002: ret
	} // end of method '<>c'::'<Main>b__1_0'
} // end of class <>c
""");
        }

        [Fact]
        public void LambdaDefaultParameter_TypeArgumentDefaultNull()
        {
            var source = """
using System;
class C<T> where T : class
{
    static void Report(object obj) => Console.WriteLine(obj.GetType());
    public void Test()
    {
        var lam1 = (int a, T b = default) => b;
        var lam2 = (int a, T b = null) => b;
        Report(lam1);
        Report(lam2);
    }
}
class Program
{
    public static void Main()
    {
        new C<string>().Test();
    }
}
""";
            CompileAndVerify(source, expectedOutput:
@"<>f__AnonymousDelegate0`3[System.Int32,System.String,System.String]
<>f__AnonymousDelegate0`3[System.Int32,System.String,System.String]").VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultParameter_GenericDelegateDefaultNull()
        {
            var source = """
delegate void D<T>(T t = default);
class Program
{
    static void M<T>(D<T> p) { }
    public static void Main()
    {
        M((object o = null) => {});
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultParameter_OptionalAndCustomConstantAttributes()
        {
            var source = """
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
class Program
{
    static void Report(object obj) => Console.WriteLine(obj.GetType());
    public static void Main()
    {
        var lam1 = ([Optional, DecimalConstant(0, 0, 0, 0, 100)] decimal d) => d;
        var lam2 = (decimal d = 100m) => d;
        Report(lam1);
        Report(lam2);
        Console.WriteLine(lam1());
        Console.WriteLine(lam2());
        Console.WriteLine(lam1(5));
        Console.WriteLine(lam2(5));
    }
}
""";
            CompileAndVerify(source, expectedOutput:
@"<>f__AnonymousDelegate0`2[System.Decimal,System.Decimal]
<>f__AnonymousDelegate0`2[System.Decimal,System.Decimal]
100
100
5
5").VerifyDiagnostics();
        }

        // delegate void <>f__AnonymousDelegate0<T1>([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] T1 d)
        // (the decimal constant is equivalent to 1.1m)
        private static readonly string s_anonymousDelegateWithDecimalConstant = $$"""
            .class private auto ansi sealed '<>f__AnonymousDelegate0`1'<T1>
                extends [{{s_libPrefix}}]System.MulticastDelegate
            {
                .custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                	01 00 00 00
                )
                // Methods
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor (
                		object 'object',
                		native int 'method'
                	) runtime managed 
                {
                } // end of method '<>f__AnonymousDelegate0`1'::.ctor
                .method public hidebysig newslot virtual 
                	instance void Invoke (
                		[opt] !T1 arg
                	) runtime managed 
                {
                	.param [1]
                		.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = (
                			01 00 01 00 00 00 00 00 00 00 00 00 0b 00 00 00
                			00 00
                		)
                } // end of method '<>f__AnonymousDelegate0`1'::Invoke
            } // end of class <>f__AnonymousDelegate0`1
            """;

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void DefaultParameterValue_Decimal_Lambda_DelegateIL()
        {
            var source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                static void Report(object obj) => System.Console.WriteLine(obj.GetType());

                var lam1 = (decimal d = 1.1m) => {};
                Report(lam1);
                var lam2 = ([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] decimal d) => {};
                Report(lam2);
                var lam3 = ([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal d = 1.1m) => {};
                Report(lam3);
                var lam4 = ([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal d) => {};
                Report(lam4);
                var lam5 = (decimal? d = 1.1m) => {};
                Report(lam5);
                var lam6 = ([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] decimal? d) => {};
                Report(lam6);
                var lam7 = ([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal? d = 1.1m) => {};
                Report(lam7);
                var lam8 = ([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal? d) => {};
                Report(lam8);
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                <>f__AnonymousDelegate0`1[System.Decimal]
                <>f__AnonymousDelegate0`1[System.Decimal]
                <>f__AnonymousDelegate0`1[System.Decimal]
                System.Action`1[System.Decimal]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.Decimal]]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.Decimal]]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.Decimal]]
                System.Action`1[System.Nullable`1[System.Decimal]]
                """).VerifyDiagnostics();
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`1", s_anonymousDelegateWithDecimalConstant);
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void DefaultParameterValue_Decimal_Lambda_ClosureIL()
        {
            var source = """
                var lam1 = (decimal d = 1.1m) => {};
                var lam2 = (decimal? d = 1.1m) => {};
                """;
            var verifier = CompileAndVerify(source).VerifyDiagnostics();
            verifier.VerifyTypeIL("<>c", $$"""
                .class nested private auto ansi sealed serializable beforefieldinit '<>c'
                	extends [{{s_libPrefix}}]System.Object
                {
                	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	// Fields
                	.field public static initonly class Program/'<>c' '<>9'
                	.field public static class '<>f__AnonymousDelegate0`1'<valuetype [{{s_libPrefix}}]System.Decimal> '<>9__0_0'
                	.field public static class '<>f__AnonymousDelegate0`1'<valuetype [{{s_libPrefix}}]System.Nullable`1<valuetype [{{s_libPrefix}}]System.Decimal>> '<>9__0_1'
                	// Methods
                	.method private hidebysig specialname rtspecialname static 
                		void .cctor () cil managed 
                	{
                		// Method begins at RVA 0x20a9
                		// Code size 11 (0xb)
                		.maxstack 8
                		IL_0000: newobj instance void Program/'<>c'::.ctor()
                		IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
                		IL_000a: ret
                	} // end of method '<>c'::.cctor
                	.method public hidebysig specialname rtspecialname 
                		instance void .ctor () cil managed 
                	{
                		// Method begins at RVA 0x20a1
                		// Code size 7 (0x7)
                		.maxstack 8
                		IL_0000: ldarg.0
                		IL_0001: call instance void [{{s_libPrefix}}]System.Object::.ctor()
                		IL_0006: ret
                	} // end of method '<>c'::.ctor
                	.method assembly hidebysig 
                		instance void '<<Main>$>b__0_0' (
                			[opt] valuetype [{{s_libPrefix}}]System.Decimal d
                		) cil managed 
                	{
                		.param [1]
                			.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = (
                				01 00 01 00 00 00 00 00 00 00 00 00 0b 00 00 00
                				00 00
                			)
                		// Method begins at RVA 0x20b5
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method '<>c'::'<<Main>$>b__0_0'
                	.method assembly hidebysig 
                		instance void '<<Main>$>b__0_1' (
                			[opt] valuetype [{{s_libPrefix}}]System.Nullable`1<valuetype [{{s_libPrefix}}]System.Decimal> d
                		) cil managed 
                	{
                		.param [1]
                			.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = (
                				01 00 01 00 00 00 00 00 00 00 00 00 0b 00 00 00
                				00 00
                			)
                		// Method begins at RVA 0x20b5
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method '<>c'::'<<Main>$>b__0_1'
                } // end of class <>c
                """);
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void DefaultParameterValue_Decimal_LocalFunction_DelegateIL()
        {
            var source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                static void Report(System.Delegate obj) => System.Console.WriteLine(obj.GetType());

                void local1(decimal d = 1.1m) {}
                Report(local1);
                void local2([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] decimal d) {}
                Report(local2);
                void local3([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal d = 1.1m) {}
                Report(local3);
                void local4([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal d) {}
                Report(local4);
                void local5(decimal? d = 1.1m) {}
                Report(local5);
                void local6([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] decimal? d) {}
                Report(local6);
                void local7([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal? d = 1.1m) {}
                Report(local7);
                void local8([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal? d) {}
                Report(local8);
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                <>f__AnonymousDelegate0`1[System.Decimal]
                <>f__AnonymousDelegate0`1[System.Decimal]
                <>f__AnonymousDelegate0`1[System.Decimal]
                System.Action`1[System.Decimal]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.Decimal]]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.Decimal]]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.Decimal]]
                System.Action`1[System.Nullable`1[System.Decimal]]
                """).VerifyDiagnostics();
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`1", s_anonymousDelegateWithDecimalConstant);
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void DefaultParameterValue_Decimal_LocalFunction_ClosureIL()
        {
            var source = """
                #pragma warning disable CS8321 // The local function is declared but never used

                void local1(decimal d = 1.1m) {}
                void local2(decimal? d = 1.1m) {}
                """;
            var verifier = CompileAndVerify(source).VerifyDiagnostics();
            verifier.VerifyTypeIL("Program", $$"""
                .class private auto ansi beforefieldinit Program
                	extends [{{s_libPrefix}}]System.Object
                {
                	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	// Methods
                	.method private hidebysig static 
                		void '<Main>$' (
                			string[] args
                		) cil managed 
                	{
                		// Method begins at RVA 0x2067
                		// Code size 1 (0x1)
                		.maxstack 8
                		.entrypoint
                		IL_0000: ret
                	} // end of method Program::'<Main>$'
                	.method public hidebysig specialname rtspecialname 
                		instance void .ctor () cil managed 
                	{
                		// Method begins at RVA 0x2069
                		// Code size 7 (0x7)
                		.maxstack 8
                		IL_0000: ldarg.0
                		IL_0001: call instance void [{{s_libPrefix}}]System.Object::.ctor()
                		IL_0006: ret
                	} // end of method Program::.ctor
                	.method assembly hidebysig static 
                		void '<<Main>$>g__local1|0_0' (
                			[opt] valuetype [{{s_libPrefix}}]System.Decimal d
                		) cil managed 
                	{
                		.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                			01 00 00 00
                		)
                		.param [1]
                			.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = (
                				01 00 01 00 00 00 00 00 00 00 00 00 0b 00 00 00
                				00 00
                			)
                		// Method begins at RVA 0x2067
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method Program::'<<Main>$>g__local1|0_0'
                	.method assembly hidebysig static 
                		void '<<Main>$>g__local2|0_1' (
                			[opt] valuetype [{{s_libPrefix}}]System.Nullable`1<valuetype [{{s_libPrefix}}]System.Decimal> d
                		) cil managed 
                	{
                		.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                			01 00 00 00
                		)
                		.param [1]
                			.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = (
                				01 00 01 00 00 00 00 00 00 00 00 00 0b 00 00 00
                				00 00
                			)
                		// Method begins at RVA 0x2067
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method Program::'<<Main>$>g__local2|0_1'
                } // end of class Program
                """);
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void DefaultParameterValue_Decimal_MethodGroup()
        {
            var source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                static void Report(object obj) => System.Console.WriteLine(obj.GetType());

                var m1 = C.M1;
                Report(m1);
                var m2 = C.M2;
                Report(m2);
                var m3 = C.M3;
                Report(m3);
                var m4 = C.M4;
                Report(m4);
                var m5 = C.M5;
                Report(m5);
                var m6 = C.M6;
                Report(m6);
                var m7 = C.M7;
                Report(m7);
                var m8 = C.M8;
                Report(m8);

                class C
                {
                    public static void M1(decimal d = 1.1m) {}
                    public static void M2([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] decimal d) {}
                    public static void M3([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal d = 1.1m) {}
                    public static void M4([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal d) {}
                    public static void M5(decimal? d = 1.1m) {}
                    public static void M6([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] decimal? d) {}
                    public static void M7([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal? d = 1.1m) {}
                    public static void M8([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal? d) {}
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                <>f__AnonymousDelegate0`1[System.Decimal]
                <>f__AnonymousDelegate0`1[System.Decimal]
                <>f__AnonymousDelegate0`1[System.Decimal]
                System.Action`1[System.Decimal]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.Decimal]]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.Decimal]]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.Decimal]]
                System.Action`1[System.Nullable`1[System.Decimal]]
                """).VerifyDiagnostics();
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`1", s_anonymousDelegateWithDecimalConstant);
        }

        // delegate void <>f__AnonymousDelegate0<T1>([Optional, DateTimeConstant(100L)] T1 d)
        private static readonly string s_anonymousDelegateWithDateTimeConstant = $$"""
            .class private auto ansi sealed '<>f__AnonymousDelegate0`1'<T1>
                extends [{{s_libPrefix}}]System.MulticastDelegate
            {
                .custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                	01 00 00 00
                )
                // Methods
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor (
                		object 'object',
                		native int 'method'
                	) runtime managed 
                {
                } // end of method '<>f__AnonymousDelegate0`1'::.ctor
                .method public hidebysig newslot virtual 
                	instance void Invoke (
                		[opt] !T1 arg
                	) runtime managed 
                {
                	.param [1]
                		.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = (
                			01 00 64 00 00 00 00 00 00 00 00 00
                		)
                } // end of method '<>f__AnonymousDelegate0`1'::Invoke
            } // end of class <>f__AnonymousDelegate0`1
            """;

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void DefaultParameterValue_DateTime_Lambda_DelegateIL()
        {
            var source = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                static void Report(object obj) => System.Console.WriteLine(obj.GetType());

                var lam1 = ([Optional, DateTimeConstant(100L)] DateTime d) => {};
                Report(lam1);
                var lam2 = ([Optional, DateTimeConstant(100L)] DateTime? d) => {};
                Report(lam2);
                var lam3 = ([DateTimeConstant(100L)] DateTime d) => {};
                Report(lam3);
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                <>f__AnonymousDelegate0`1[System.DateTime]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.DateTime]]
                System.Action`1[System.DateTime]
                """).VerifyDiagnostics();
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`1", s_anonymousDelegateWithDateTimeConstant);
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void DefaultParameterValue_DateTime_Lambda_ClosureIL()
        {
            var source = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                var lam1 = ([Optional, DateTimeConstant(100L)] DateTime d) => {};
                var lam2 = ([Optional, DateTimeConstant(100L)] DateTime? d) => {};
                """;
            var verifier = CompileAndVerify(source).VerifyDiagnostics();
            verifier.VerifyTypeIL("<>c", $$"""
                .class nested private auto ansi sealed serializable beforefieldinit '<>c'
                	extends [{{s_libPrefix}}]System.Object
                {
                	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	// Fields
                	.field public static initonly class Program/'<>c' '<>9'
                	.field public static class '<>f__AnonymousDelegate0`1'<valuetype [{{s_libPrefix}}]System.DateTime> '<>9__0_0'
                	.field public static class '<>f__AnonymousDelegate0`1'<valuetype [{{s_libPrefix}}]System.Nullable`1<valuetype [{{s_libPrefix}}]System.DateTime>> '<>9__0_1'
                	// Methods
                	.method private hidebysig specialname rtspecialname static 
                		void .cctor () cil managed 
                	{
                		// Method begins at RVA 0x20a9
                		// Code size 11 (0xb)
                		.maxstack 8
                		IL_0000: newobj instance void Program/'<>c'::.ctor()
                		IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
                		IL_000a: ret
                	} // end of method '<>c'::.cctor
                	.method public hidebysig specialname rtspecialname 
                		instance void .ctor () cil managed 
                	{
                		// Method begins at RVA 0x20a1
                		// Code size 7 (0x7)
                		.maxstack 8
                		IL_0000: ldarg.0
                		IL_0001: call instance void [{{s_libPrefix}}]System.Object::.ctor()
                		IL_0006: ret
                	} // end of method '<>c'::.ctor
                	.method assembly hidebysig 
                		instance void '<<Main>$>b__0_0' (
                			[opt] valuetype [{{s_libPrefix}}]System.DateTime d
                		) cil managed 
                	{
                		.param [1]
                			.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = (
                				01 00 64 00 00 00 00 00 00 00 00 00
                			)
                		// Method begins at RVA 0x20b5
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method '<>c'::'<<Main>$>b__0_0'
                	.method assembly hidebysig 
                		instance void '<<Main>$>b__0_1' (
                			[opt] valuetype [{{s_libPrefix}}]System.Nullable`1<valuetype [{{s_libPrefix}}]System.DateTime> d
                		) cil managed 
                	{
                		.param [1]
                			.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = (
                				01 00 64 00 00 00 00 00 00 00 00 00
                			)
                		// Method begins at RVA 0x20b5
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method '<>c'::'<<Main>$>b__0_1'
                } // end of class <>c
                """);
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void DefaultParameterValue_DateTime_LocalFunction_DelegateIL()
        {
            var source = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                static void Report(Delegate obj) => System.Console.WriteLine(obj.GetType());

                void local1([Optional, DateTimeConstant(100L)] DateTime d) {}
                Report(local1);
                void local2([Optional, DateTimeConstant(100L)] DateTime? d) {}
                Report(local2);
                void local3([DateTimeConstant(100L)] DateTime d) {}
                Report(local3);
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                <>f__AnonymousDelegate0`1[System.DateTime]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.DateTime]]
                System.Action`1[System.DateTime]
                """).VerifyDiagnostics();
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`1", s_anonymousDelegateWithDateTimeConstant);
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void DefaultParameterValue_DateTime_LocalFunction_ClosureIL()
        {
            var source = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                #pragma warning disable CS8321 // The local function is declared but never used
                
                void local1([Optional, DateTimeConstant(100L)] DateTime d) {}
                void local2([Optional, DateTimeConstant(100L)] DateTime? d) {}
                """;
            var verifier = CompileAndVerify(source).VerifyDiagnostics();
            verifier.VerifyTypeIL("Program", $$"""
                .class private auto ansi beforefieldinit Program
                	extends [{{s_libPrefix}}]System.Object
                {
                	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	// Methods
                	.method private hidebysig static 
                		void '<Main>$' (
                			string[] args
                		) cil managed 
                	{
                		// Method begins at RVA 0x2067
                		// Code size 1 (0x1)
                		.maxstack 8
                		.entrypoint
                		IL_0000: ret
                	} // end of method Program::'<Main>$'
                	.method public hidebysig specialname rtspecialname 
                		instance void .ctor () cil managed 
                	{
                		// Method begins at RVA 0x2069
                		// Code size 7 (0x7)
                		.maxstack 8
                		IL_0000: ldarg.0
                		IL_0001: call instance void [{{s_libPrefix}}]System.Object::.ctor()
                		IL_0006: ret
                	} // end of method Program::.ctor
                	.method assembly hidebysig static 
                		void '<<Main>$>g__local1|0_0' (
                			[opt] valuetype [{{s_libPrefix}}]System.DateTime d
                		) cil managed 
                	{
                		.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                			01 00 00 00
                		)
                 		.param [1]
                 			.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = (
                 				01 00 64 00 00 00 00 00 00 00 00 00
                 			)
                		// Method begins at RVA 0x2067
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method Program::'<<Main>$>g__local1|0_0'
                	.method assembly hidebysig static 
                		void '<<Main>$>g__local2|0_1' (
                			[opt] valuetype [{{s_libPrefix}}]System.Nullable`1<valuetype [{{s_libPrefix}}]System.DateTime> d
                		) cil managed 
                	{
                		.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                			01 00 00 00
                		)
                 		.param [1]
                 			.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = (
                 				01 00 64 00 00 00 00 00 00 00 00 00
                 			)
                		// Method begins at RVA 0x2067
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method Program::'<<Main>$>g__local2|0_1'
                } // end of class Program
                """);
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void DefaultParameterValue_DateTime_MethodGroup()
        {
            var source = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                static void Report(object obj) => System.Console.WriteLine(obj.GetType());

                var m1 = C.M1;
                Report(m1);
                var m2 = C.M2;
                Report(m2);
                var m3 = C.M3;
                Report(m3);

                public class C
                {
                    public static void M1([Optional, DateTimeConstant(100L)] DateTime d) {}
                    public static void M2([Optional, DateTimeConstant(100L)] DateTime? d) {}
                    public static void M3([DateTimeConstant(100L)] DateTime d) {}
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                <>f__AnonymousDelegate0`1[System.DateTime]
                <>f__AnonymousDelegate0`1[System.Nullable`1[System.DateTime]]
                System.Action`1[System.DateTime]
                """).VerifyDiagnostics();
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`1", s_anonymousDelegateWithDateTimeConstant);
        }

        [Fact]
        public void LambdaDefaultParameter_ArrayCommonType_DefaultValueMismatch()
        {
            var source = """
                var arr = new[] { (int i = 1) => { }, (int i = 2) => { } };
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,11): error CS0826: No best type found for implicitly-typed array
                // var arr = new[] { (int i = 1) => { }, (int i = 2) => { } };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { (int i = 1) => { }, (int i = 2) => { } }").WithLocation(1, 11));
        }

        [Fact]
        public void LambdaDefaultParameter_ArrayCommonType_DefaultValueMatch()
        {
            var source = """
using System;
class Program
{
    static void Report(object obj) => Console.WriteLine(obj.GetType());
    public static void Main()
    {
        var arr = new[] { (int i = 1) => { }, (int i = 1) => { } };
        Report(arr);
    }
}
""";
            CompileAndVerify(source, expectedOutput:
@"<>f__AnonymousDelegate0`1[System.Int32][]").VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultParameter_InsideExpressionTree()
        {
            var source = """
using System;
using System.Linq.Expressions;
class Program
{
    static void Report(object obj) => Console.WriteLine(obj.GetType());
    public static void Main()
    {
        Expression e1 = (int x = 1) => x;
        Expression e2 = (int x) => (int y = 1) => y;
        Report(e1);
        Report(e2);
    }   
}
""";
            CompileAndVerify(source, expectedOutput:
$@"{s_expressionOfTDelegate1ArgTypeName}[<>f__AnonymousDelegate0`2[System.Int32,System.Int32]]
{s_expressionOfTDelegate1ArgTypeName}[System.Func`2[System.Int32,<>f__AnonymousDelegate0`2[System.Int32,System.Int32]]]").VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultParameter_MissingConstantValueType()
        {
            var source = """
                var lam1 = (object f = 1.0) => f;
                var lam2 = (object d = 2m) => d;
                namespace System
                {
                    public class Object { }
                    public class String { }
                    public abstract class ValueType { }
                    public struct Void { }
                    public abstract class Delegate { }
                    public abstract class MulticastDelegate : Delegate { }
                }
                """;
            CreateEmptyCompilation(source).VerifyDiagnostics(
                // (1,20): error CS1763: 'f' is of type 'object'. A default parameter value of a reference type other than string can only be initialized with null
                // var lam1 = (object f = 1.0) => f;
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "f").WithArguments("f", "object").WithLocation(1, 20),
                // (1,24): error CS0518: Predefined type 'System.Double' is not defined or imported
                // var lam1 = (object f = 1.0) => f;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1.0").WithArguments("System.Double").WithLocation(1, 24),
                // (2,20): error CS1763: 'd' is of type 'object'. A default parameter value of a reference type other than string can only be initialized with null
                // var lam2 = (object d = 2m) => d;
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "d").WithArguments("d", "object").WithLocation(2, 20),
                // (2,24): error CS0518: Predefined type 'System.Decimal' is not defined or imported
                // var lam2 = (object d = 2m) => d;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "2m").WithArguments("System.Decimal").WithLocation(2, 24));
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Lambda(string type)
        {
            var source = $$"""
                var lam = ({{type}} d = 1.1m) => { };
                var lam2 = lam;
                """;

            var diagnostics = new[]
            {
                // (1,25): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                // var lam = (decimal  d = 1.1m) => { };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1.1m").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(1, 25)
            };

            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyDiagnostics(diagnostics);

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor);
            comp.VerifyDiagnostics(diagnostics);

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctorByteByteInt32Int32Int32);
            comp.VerifyEmitDiagnostics();
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Lambda_AsArgument(string type)
        {
            var source = $$"""
                TakeDelegate(({{type}} d = 1.1m) => { });

                static void TakeDelegate(System.Delegate d) { }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyDiagnostics(
                // (1,28): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                // TakeDelegate((decimal  d = 1.1m) => { });
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1.1m").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(1, 28));
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Lambda_AsGenericArgument(string type)
        {
            var source = $$"""
                TakeDelegate(({{type}} d = 1.1m) => { });

                static void TakeDelegate<T>(T d) where T : System.Delegate { }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyDiagnostics(
                // (1,28): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                // TakeDelegate((decimal  d = 1.1m) => { });
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1.1m").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(1, 28));
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Lambda_CustomType(string type)
        {
            var source = $$"""
                Del lam = ({{type}} d = 1.1m) => { };

                delegate void Del({{type}} d = 1.1m);
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyDiagnostics(
                // (1,25): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                // Del lam = (decimal  d = 1.1m) => { };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1.1m").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(1, 25),
                // (3,32): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                // delegate void Del(decimal  d = 1.1m);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1.1m").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(3, 32));
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal", "System.Decimal")]
        [InlineData("decimal?", "System.Nullable`1[System.Decimal]")]
        public void MissingDecimalConstantAttribute_Lambda_ExplicitAttribute_Alone(string type, string typeFullName)
        {
            var source = $$"""
                using System.Runtime.CompilerServices;

                var lam = ([DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d) => { };
                System.Console.WriteLine(lam.GetType());
                var lam2 = lam;
                System.Console.WriteLine(lam2.GetType());
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            CompileAndVerify(comp, expectedOutput: $"""
                System.Action`1[{typeFullName}]
                System.Action`1[{typeFullName}]
                """).VerifyDiagnostics();
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Lambda_ExplicitAttribute_Alone_CustomType(string type)
        {
            var source = $$"""
                using System.Runtime.CompilerServices;

                Del lam = ([DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d) => { };

                delegate void Del([DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d);
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyEmitDiagnostics();
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Lambda_ExplicitAttribute_WithOptional(string type)
        {
            var source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                var lam = ([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d) => { };
                var lam2 = lam;
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyDiagnostics(
                // (4,68): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                // var lam = ([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] decimal  d) => { };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "d").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(4, 68));
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Lambda_ExplicitAttribute_WithOptional_CustomType(string type)
        {
            var source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                Del lam = ([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d) => { };
                
                delegate void Del([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d);
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyEmitDiagnostics();
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Lambda_ExplicitAttribute_WithDefault(string type)
        {
            var source = $$"""
                using System.Runtime.CompilerServices;

                var lam = ([DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d = 1.1m) => { };
                var lam2 = lam;
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyDiagnostics(
                // (3,58): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                // var lam = ([DecimalConstant(1, 0, 0u, 0u, 11u)] decimal  d = 1.1m) => { };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "d").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(3, 58));
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Lambda_ExplicitAttribute_WithDefault_CustomType(string type)
        {
            var source = $$"""
                using System.Runtime.CompilerServices;

                Del lam = ([DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d = 1.1m) => { };

                delegate void Del([DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d = 1.1m);
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyEmitDiagnostics();
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Method(string type)
        {
            var source = $$"""
                var m = C.M;

                class C
                {
                    public static void M({{type}} d = 1.1m) { }
                }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyDiagnostics(
                // (1,9): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                // var m = C.M;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C.M").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(1, 9),
                // (5,39): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                //     public static void M(decimal  d = 1.1m) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1.1m").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(5, 39));
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Method_ExplicitAttribute_WithOptional(string type)
        {
            var source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                var m = C.M;
                
                class C
                {
                    public static void M([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d) { }
                }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyDiagnostics(
                // (4,9): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                // var m = C.M;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C.M").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(4, 9));
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Method_ExplicitAttribute_WithOptional_CustomType(string type)
        {
            var source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                Del m = C.M;
                
                class C
                {
                    public static void M([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d) { }
                }

                delegate void Del([Optional, DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d);
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyEmitDiagnostics();
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Method_ExplicitAttribute_WithDefault(string type)
        {
            var source = $$"""
                using System.Runtime.CompilerServices;

                var m = C.M;
                
                class C
                {
                    public static void M([DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d = 1.1m) { }
                }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyDiagnostics(
                // (3,9): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                // var m = C.M;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C.M").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(3, 9));
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal ")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_Method_ExplicitAttribute_WithDefault_CustomType(string type)
        {
            var source = $$"""
                using System.Runtime.CompilerServices;

                Del m = C.M;
                
                class C
                {
                    public static void M([DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d = 1.1m) { }
                }

                delegate void Del([DecimalConstant(1, 0, 0u, 0u, 11u)] {{type}} d = 1.1m);
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyEmitDiagnostics();
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_ExternalMethodGroup(string type)
        {
            var source1 = $$"""
                public class C
                {
                    public static void M({{type}} d = 1.1m) { }
                }
                """;
            var comp1 = CreateCompilation(source1).VerifyDiagnostics();

            var source2 = """
                var m = C.M;
                """;
            var comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp2.VerifyDiagnostics(
                // (1,9): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                // var m = C.M;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C.M").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(1, 9));
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("decimal")]
        [InlineData("decimal?")]
        public void MissingDecimalConstantAttribute_ExternalMethodGroup_CustomType(string type)
        {
            var source1 = $$"""
                public delegate void Del({{type}} d = 1.1m);

                public class C
                {
                    public static void M({{type}} d = 1.1m) { }
                }
                """;
            var comp1 = CreateCompilation(source1).VerifyDiagnostics();

            var source2 = """
                Del m = C.M;
                """;
            var comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp2.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void MissingDecimalConstantAttribute_Field()
        {
            var source = """
                class C
                {
                    public const decimal D = 1.1m;
                }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyDiagnostics(
                // (3,26): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor'
                //     public const decimal D = 1.1m;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "D = 1.1m").WithArguments("System.Runtime.CompilerServices.DecimalConstantAttribute", ".ctor").WithLocation(3, 26));
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void MissingDecimalConstantAttribute_Field_ExplicitAttribute_Alone()
        {
            var source = """
                public static class C
                {
                    [System.Runtime.CompilerServices.DecimalConstant(1, 0, 0u, 0u, 11u)]
                    public static readonly decimal D;
                }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyDiagnostics();

            // Even though we use `static readonly` above, it's actually equivalent to `const` as demonstrated below.
            var source2 = """
                const decimal d = C.D;
                """;
            CreateCompilation(source2, new[] { comp.EmitToImageReference() }).VerifyEmitDiagnostics(
                // (1,15): warning CS0219: The variable 'd' is assigned but its value is never used
                // const decimal d = C.D;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "d").WithArguments("d").WithLocation(1, 15));
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void MissingDecimalConstantAttribute_Field_ExplicitAttribute_WithDefault()
        {
            var source = """
                class C
                {
                    [System.Runtime.CompilerServices.DecimalConstant(1, 0, 0u, 0u, 11u)]
                    public const decimal D = 1.1m;
                }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void MissingDecimalConstantAttribute_OverloadResolution()
        {
            var source1 = """
                public delegate void Del(decimal d = 1.1m);

                public class C
                {
                    public void M(Del d) { }
                    public void M(System.Action<decimal> d) { }
                }
                """;
            var comp1 = CreateCompilation(source1).VerifyDiagnostics();

            var source2 = """
                new C().M((decimal d = 1.1m) => { });
                """;

            var diagnostics = new[]
            {
                // (1,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(Del)' and 'C.M(Action<decimal>)'
                // new C().M((decimal d = 1.1m) => { });
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(Del)", "C.M(System.Action<decimal>)").WithLocation(1, 9)
            };

            var comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
            comp2.VerifyDiagnostics(diagnostics);

            comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor);
            comp2.VerifyDiagnostics(diagnostics);

            comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctorByteByteInt32Int32Int32);
            comp2.VerifyDiagnostics(diagnostics);

            comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.VerifyDiagnostics(diagnostics);
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("DateTime", "System.DateTime")]
        [InlineData("DateTime?", "System.Nullable`1[System.DateTime]")]
        public void MissingDateTimeConstantAttribute_Lambda_Alone(string type, string typeFullName)
        {
            var source = $$"""
                using System;
                using System.Runtime.CompilerServices;

                var lam = ([DateTimeConstant(100L)] {{type}} d) => { };
                System.Console.WriteLine(lam.GetType());
                var lam2 = lam;
                System.Console.WriteLine(lam2.GetType());
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute);
            CompileAndVerify(comp, expectedOutput: $"""
                System.Action`1[{typeFullName}]
                System.Action`1[{typeFullName}]
                """).VerifyDiagnostics();
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("DateTime")]
        [InlineData("DateTime?")]
        public void MissingDateTimeConstantAttribute_Lambda_Alone_CustomType(string type)
        {
            var source = $$"""
                using System;
                using System.Runtime.CompilerServices;

                Del lam = ([DateTimeConstant(100L)] {{type}} d) => { };

                delegate void Del([DateTimeConstant(100L)] {{type}} d);
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute);
            comp.VerifyEmitDiagnostics();
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("DateTime ")]
        [InlineData("DateTime?")]
        public void MissingDateTimeConstantAttribute_Lambda_WithOptional(string type)
        {
            var source = $$"""
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                var lam = ([Optional, DateTimeConstant(100L)] {{type}} d) => { };
                var lam2 = lam;
                """;

            var diagnostics = new[]
            {
                // (5,57): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DateTimeConstantAttribute..ctor'
                // var lam = ([Optional, DateTimeConstant(100L)] DateTime  d) => { };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "d").WithArguments("System.Runtime.CompilerServices.DateTimeConstantAttribute", ".ctor").WithLocation(5, 57)
            };

            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute);
            comp.VerifyDiagnostics(diagnostics);

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor);
            comp.VerifyDiagnostics(diagnostics);
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("DateTime ")]
        [InlineData("DateTime?")]
        public void MissingDateTimeConstantAttribute_Lambda_WithOptional_CustomType(string type)
        {
            var source = $$"""
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                Del lam = ([Optional, DateTimeConstant(100L)] {{type}} d) => { };

                delegate void Del([Optional, DateTimeConstant(100L)] {{type}} d);
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute);
            comp.VerifyEmitDiagnostics();
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("DateTime ")]
        [InlineData("DateTime?")]
        public void MissingDateTimeConstantAttribute_ExternalMethodGroup(string type)
        {
            var source1 = $$"""
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                
                public class C
                {
                    public static void M([Optional, DateTimeConstant(100L)] {{type}} d) { }
                }
                """;
            var comp1 = CreateCompilation(source1).VerifyDiagnostics();

            var source2 = """
                var m = C.M;
                """;
            var comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute);
            comp2.VerifyDiagnostics(
                // (1,9): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DateTimeConstantAttribute..ctor'
                // var m = C.M;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C.M").WithArguments("System.Runtime.CompilerServices.DateTimeConstantAttribute", ".ctor").WithLocation(1, 9));
        }

        [Theory, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        [InlineData("DateTime ")]
        [InlineData("DateTime?")]
        public void MissingDateTimeConstantAttribute_ExternalMethodGroup_CustomType(string type)
        {
            var source1 = $$"""
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                public delegate void Del([Optional, DateTimeConstant(100L)] {{type}} d);
                
                public class C
                {
                    public static void M([Optional, DateTimeConstant(100L)] {{type}} d) { }
                }
                """;
            var comp1 = CreateCompilation(source1).VerifyDiagnostics();

            var source2 = """
                Del m = C.M;
                """;
            var comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute);
            comp2.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem(65728, "https://github.com/dotnet/roslyn/issues/65728")]
        public void MissingDateTimeConstantAttribute_OverloadResolution()
        {
            var source1 = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                public delegate void Del([Optional, DateTimeConstant(100L)] DateTime d);

                public class C
                {
                    public void M(Del d) { }
                    public void M(Action<DateTime> d) { }
                }
                """;
            var comp1 = CreateCompilation(source1).VerifyDiagnostics();

            var source2 = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                new C().M(([Optional, DateTimeConstant(100L)] DateTime d) => { });
                """;

            var diagnostics = new[]
            {
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(Del)' and 'C.M(Action<DateTime>)'
                // new C().M(([Optional, DateTimeConstant(100L)] DateTime d) => { });
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(Del)", "C.M(System.Action<System.DateTime>)").WithLocation(5, 9)
            };

            var comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute);
            comp2.VerifyDiagnostics(diagnostics);

            comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor);
            comp2.VerifyDiagnostics(diagnostics);

            comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.VerifyDiagnostics(diagnostics);
        }

        [Fact]
        public void ParamsArray_MissingParamArrayAttribute_Lambda()
        {
            var source = """
                var lam = (params int[] xs) => xs.Length;
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_ParamArrayAttribute);
            comp.VerifyDiagnostics(
                // (1,12): error CS0656: Missing compiler required member 'System.ParamArrayAttribute..ctor'
                // var lam = (params int[] xs) => xs.Length;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "params").WithArguments("System.ParamArrayAttribute", ".ctor").WithLocation(1, 12));
        }

        [Fact]
        public void ParamsArray_MissingParamArrayAttribute_Lambda_ExplicitDelegateType()
        {
            var source = """
                System.Func<int[], int> lam = (params int[] xs) => xs.Length;
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_ParamArrayAttribute);
            comp.VerifyDiagnostics(
                // (1,32): error CS0656: Missing compiler required member 'System.ParamArrayAttribute..ctor'
                // System.Func<int[], int> lam = (params int[] xs) => xs.Length;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "params").WithArguments("System.ParamArrayAttribute", ".ctor").WithLocation(1, 32),
                // (1,45): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                // System.Func<int[], int> lam = (params int[] xs) => xs.Length;
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "xs").WithArguments("1").WithLocation(1, 45));
        }

        [Fact]
        public void ParamsArray_MissingParamArrayAttribute_LocalFunction()
        {
            var source = """
                int local(params int[] xs) => xs.Length;
                local();
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_ParamArrayAttribute);
            comp.VerifyDiagnostics(
                // (1,11): error CS0656: Missing compiler required member 'System.ParamArrayAttribute..ctor'
                // int local(params int[] xs) => xs.Length;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "params int[] xs").WithArguments("System.ParamArrayAttribute", ".ctor").WithLocation(1, 11));
        }

        [Fact]
        public void ParamsArray_MissingParamArrayAttribute_ExternalMethodGroup()
        {
            var source1 = """
                public class C
                {
                    public static void M(params int[] xs) { }
                }
                """;
            var comp1 = CreateCompilation(source1).VerifyDiagnostics();

            var source2 = """
                var m = C.M;
                """;
            var comp2 = CreateCompilation(source2, new[] { comp1.ToMetadataReference() });
            comp2.MakeTypeMissing(WellKnownType.System_ParamArrayAttribute);
            comp2.VerifyDiagnostics(
                // (1,9): error CS0656: Missing compiler required member 'System.ParamArrayAttribute..ctor'
                // var m = C.M;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "C.M").WithArguments("System.ParamArrayAttribute", ".ctor").WithLocation(1, 9));
        }

        [Fact]
        public void ParamsArray_MissingParamArrayAttribute_Method()
        {
            var source = """
                class C
                {
                    static void M(params int[] xs) { }
                }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_ParamArrayAttribute);
            comp.VerifyDiagnostics(
                // (3,19): error CS0656: Missing compiler required member 'System.ParamArrayAttribute..ctor'
                //     static void M(params int[] xs) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "params int[] xs").WithArguments("System.ParamArrayAttribute", ".ctor").WithLocation(3, 19));
        }

        [Fact]
        public void ParamsArray_MissingParamArrayAttribute_Property()
        {
            var source = """
                class C
                {
                    int this[params int[] xs] { get => throw null; set => throw null; }
                }
                """;
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_ParamArrayAttribute);
            comp.VerifyDiagnostics(
                // (3,14): error CS0656: Missing compiler required member 'System.ParamArrayAttribute..ctor'
                //     int this[params int[] xs] { get => throw null; set => throw null; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "params int[] xs").WithArguments("System.ParamArrayAttribute", ".ctor").WithLocation(3, 14));
        }

        [Fact]
        public void ParamsArray_SynthesizedTypesMatch()
        {
            var source = """
                static void Report(object obj1, object obj2) => System.Console.WriteLine($"{obj1.GetType() == obj2.GetType()}, {obj1.GetType()}");

                var lam1 = (params int[] xs) => xs.Length;
                int Method1(params int[] xs) => xs.Length;
                var del1 = Method1;
                Report(lam1, del1);

                var lam2 = (params int[] ys) => ys.Length;
                int Method2(params int[] ys) => ys.Length;
                var del2 = Method2;
                Report(lam2, del2);
                Report(lam1, lam2);

                var lam3 = (int[] xs) => xs.Length;
                int Method3(int[] xs) => xs.Length;
                var del3 = Method3;
                Report(lam3, del3);

                var lam4 = (ref int a, int b, int[] xs) => { };
                void Method4(ref int a, int b, int[] xs) { }
                var del4 = Method4;
                Report(lam4, del4);

                var lam5 = (ref int a, int b, params int[] xs) => { };
                void Method5(ref int a, int b, params int[] xs) { }
                var del5 = Method5;
                Report(lam5, del5);

                var lam6 = (int a, System.TypedReference b, params int[] xs) => { };
                void Method6(int a, System.TypedReference b, params int[] xs) { }
                var del6 = Method6;
                Report(lam6, del6);

                var lam7 = (int x, System.TypedReference y, params int[] ys) => { };
                void Method7(int x, System.TypedReference y, params int[] ys) { }
                var del7 = Method7;
                Report(lam7, del7);
                """;
            CompileAndVerify(source, expectedOutput: """
                True, <>f__AnonymousDelegate0`2[System.Int32,System.Int32]
                True, <>f__AnonymousDelegate0`2[System.Int32,System.Int32]
                True, <>f__AnonymousDelegate0`2[System.Int32,System.Int32]
                True, System.Func`2[System.Int32[],System.Int32]
                True, <>A{00000001}`3[System.Int32,System.Int32,System.Int32[]]
                True, <>f__AnonymousDelegate1`3[System.Int32,System.Int32,System.Int32]
                True, <>f__AnonymousDelegate2
                True, <>f__AnonymousDelegate2
                """).VerifyDiagnostics();
        }

        [Theory]
        [InlineData("(int x, params int[] ys) => { }", "")]
        [InlineData("M", "static void M(int x, params int[] ys) { }")]
        public void ParamsArray_SynthesizedDelegateIL(string variable, string method)
        {
            var source = $$"""
                class C
                {
                    static void Report(object obj) => System.Console.WriteLine(obj.GetType());
                    static void Main()
                    {
                        var m = {{variable}};
                        Report(m);
                    }
                    {{method}}
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "<>f__AnonymousDelegate0`2[System.Int32,System.Int32]").VerifyDiagnostics();
            verifier.VerifyTypeIL("<>f__AnonymousDelegate0`2", $$"""
                .class private auto ansi sealed '<>f__AnonymousDelegate0`2'<T1, T2>
                	extends [{{s_libPrefix}}]System.MulticastDelegate
                {
                	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	// Methods
                	.method public hidebysig specialname rtspecialname 
                		instance void .ctor (
                			object 'object',
                			native int 'method'
                		) runtime managed 
                	{
                	} // end of method '<>f__AnonymousDelegate0`2'::.ctor
                	.method public hidebysig newslot virtual 
                		instance void Invoke (
                			!T1 arg1,
                			!T2[] arg2
                		) runtime managed 
                	{
                		.param [2]
                			.custom instance void [{{s_libPrefix}}]System.ParamArrayAttribute::.ctor() = (
                				01 00 00 00
                			)
                	} // end of method '<>f__AnonymousDelegate0`2'::Invoke
                } // end of class <>f__AnonymousDelegate0`2
                """);
        }

        [Fact]
        public void ParamsArray_SynthesizedMethodIL_Lambda()
        {
            var source = """
                var lam = (int x, params int[] ys) => { };
                System.Console.WriteLine(lam.Method.DeclaringType!.Name);
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "<>c").VerifyDiagnostics();
            verifier.VerifyTypeIL("<>c", $$"""
                .class nested private auto ansi sealed serializable beforefieldinit '<>c'
                	extends [{{s_libPrefix}}]System.Object
                {
                	.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	// Fields
                	.field public static initonly class Program/'<>c' '<>9'
                	.field public static class '<>f__AnonymousDelegate0`2'<int32, int32> '<>9__0_0'
                	// Methods
                	.method private hidebysig specialname rtspecialname static 
                		void .cctor () cil managed 
                	{
                		// Method begins at RVA 0x20a4
                		// Code size 11 (0xb)
                		.maxstack 8
                		IL_0000: newobj instance void Program/'<>c'::.ctor()
                		IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
                		IL_000a: ret
                	} // end of method '<>c'::.cctor
                	.method public hidebysig specialname rtspecialname 
                		instance void .ctor () cil managed 
                	{
                		// Method begins at RVA 0x209c
                		// Code size 7 (0x7)
                		.maxstack 8
                		IL_0000: ldarg.0
                		IL_0001: call instance void [{{s_libPrefix}}]System.Object::.ctor()
                		IL_0006: ret
                	} // end of method '<>c'::.ctor
                	.method assembly hidebysig 
                		instance void '<<Main>$>b__0_0' (
                			int32 x,
                			int32[] ys
                		) cil managed 
                	{
                		// Method begins at RVA 0x20b0
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method '<>c'::'<<Main>$>b__0_0'
                } // end of class <>c
                """);
        }

        [Fact]
        public void ParamsArray_SynthesizedMethodIL_LocalFunction()
        {
            var source = """
                class C
                {
                    public static void M()
                    {
                        void local(int x, params int[] ys) { }
                    }
                }
                """;
            var verifier = CompileAndVerify(source).VerifyDiagnostics(
                // (5,14): warning CS8321: The local function 'local' is declared but never used
                //         void local(int x, params int[] ys) { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(5, 14));
            verifier.VerifyTypeIL("C", $$"""
                .class private auto ansi beforefieldinit C
                	extends [{{s_libPrefix}}]System.Object
                {
                	// Methods
                	.method public hidebysig static 
                		void M () cil managed 
                	{
                		// Method begins at RVA 0x2067
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method C::M
                	.method public hidebysig specialname rtspecialname 
                		instance void .ctor () cil managed 
                	{
                		// Method begins at RVA 0x2069
                		// Code size 7 (0x7)
                		.maxstack 8
                		IL_0000: ldarg.0
                		IL_0001: call instance void [{{s_libPrefix}}]System.Object::.ctor()
                		IL_0006: ret
                	} // end of method C::.ctor
                	.method assembly hidebysig static 
                		void '<M>g__local|0_0' (
                			int32 x,
                			int32[] ys
                		) cil managed 
                	{
                		.custom instance void [{{s_libPrefix}}]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                			01 00 00 00
                		)
                		// Method begins at RVA 0x2067
                		// Code size 1 (0x1)
                		.maxstack 8
                		IL_0000: ret
                	} // end of method C::'<M>g__local|0_0'
                } // end of class C
                """);
        }

        [Fact]
        public void ParamsArray_DelegateConversions_Lambdas()
        {
            var source = """
                var noParams = (int[] xs) => xs.Length;
                var withParams = (params int[] xs) => xs.Length;
                noParams = withParams; // 1
                withParams = noParams; // 2
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (3,12): error CS0029: Cannot implicitly convert type '<anonymous delegate>' to 'System.Func<int[], int>'
                // noParams = withParams; // 1
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "withParams").WithArguments("<anonymous delegate>", "System.Func<int[], int>").WithLocation(3, 12),
                // (4,14): error CS0029: Cannot implicitly convert type 'System.Func<int[], int>' to '<anonymous delegate>'
                // withParams = noParams; // 2
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "noParams").WithArguments("System.Func<int[], int>", "<anonymous delegate>").WithLocation(4, 14));
        }

        [Fact]
        public void ParamsArray_DelegateConversions_MethodGroups()
        {
            var source = """
                int MethodNoParams(int[] xs) => xs.Length;
                int MethodWithParams(params int[] xs) => xs.Length;
                var noParams = MethodNoParams;
                var withParams = MethodWithParams;
                noParams = withParams; // 1
                withParams = noParams; // 2
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (5,12): error CS0029: Cannot implicitly convert type '<anonymous delegate>' to 'System.Func<int[], int>'
                // noParams = withParams; // 1
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "withParams").WithArguments("<anonymous delegate>", "System.Func<int[], int>").WithLocation(5, 12),
                // (6,14): error CS0029: Cannot implicitly convert type 'System.Func<int[], int>' to '<anonymous delegate>'
                // withParams = noParams; // 2
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "noParams").WithArguments("System.Func<int[], int>", "<anonymous delegate>").WithLocation(6, 14));
        }

        [Fact]
        public void ParamsArray_LambdaConversions()
        {
            var source = """
                int MethodNoParams(int[] xs) => xs.Length;
                int MethodWithParams(params int[] xs) => xs.Length;
                var noParams = MethodNoParams;
                var withParams = MethodWithParams;
                noParams = (params int[] xs) => xs.Length; // 1
                noParams = (int[] xs) => xs.Length;
                withParams = (params int[] xs) => xs.Length;
                withParams = (int[] xs) => xs.Length;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (5,26): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                // noParams = (params int[] xs) => xs.Length; // 1
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "xs").WithArguments("1").WithLocation(5, 26));
        }

        [Fact]
        public void ParamsArray_MethodGroupConversions()
        {
            var source = """
                int MethodNoParams(int[] xs) => xs.Length;
                int MethodWithParams(params int[] xs) => xs.Length;
                var noParams = (int[] xs) => xs.Length;
                var withParams = (params int[] xs) => xs.Length;
                noParams = MethodWithParams;
                withParams = MethodWithParams;
                noParams = MethodNoParams;
                withParams = MethodNoParams;
                """;
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ParamsArray_ConversionsToNamedDelegates()
        {
            var source = """
                int MethodNoParams(int[] xs) => xs.Length;
                int MethodWithParams(params int[] xs) => xs.Length;
                DelegateNoParams dNoParams;
                DelegateWithParams dWithParams;
                dNoParams = (params int[] xs) => xs.Length; // 1
                dNoParams = (int[] xs) => xs.Length;
                dNoParams = MethodNoParams;
                dNoParams = MethodWithParams;
                dWithParams = (params int[] xs) => xs.Length;
                dWithParams = (int[] xs) => xs.Length;
                dWithParams = MethodNoParams;
                dWithParams = MethodWithParams;
                delegate int DelegateNoParams(int[] xs);
                delegate int DelegateWithParams(params int[] xs);
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (5,27): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                // dNoParams = (params int[] xs) => xs.Length; // 1
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "xs").WithArguments("1").WithLocation(5, 27));
        }

        [Fact]
        public void ParamsArray_CommonType()
        {
            var source = """
                int MethodNoParams(int[] xs) => xs.Length;
                int MethodWithParams(params int[] xs) => xs.Length;
                var inferredNoParams = MethodNoParams;
                var inferredWithParams = MethodWithParams;
                var lambdaNoParams = (int[] xs) => xs.Length;
                var lambdaWithParams = (params int[] xs) => xs.Length;
                var a1 = new[] { MethodNoParams, MethodWithParams }; // 1
                var a2 = new[] { inferredNoParams, inferredWithParams }; // 2
                var a3 = new[] { lambdaNoParams, lambdaWithParams }; // 3
                var a4 = new[] { (int[] xs) => xs.Length, (params int[] xs) => xs.Length }; // 4
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (7,10): error CS0826: No best type found for implicitly-typed array
                // var a1 = new[] { MethodNoParams, MethodWithParams }; // 1
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { MethodNoParams, MethodWithParams }").WithLocation(7, 10),
                // (8,10): error CS0826: No best type found for implicitly-typed array
                // var a2 = new[] { inferredNoParams, inferredWithParams }; // 2
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { inferredNoParams, inferredWithParams }").WithLocation(8, 10),
                // (9,10): error CS0826: No best type found for implicitly-typed array
                // var a3 = new[] { lambdaNoParams, lambdaWithParams }; // 3
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { lambdaNoParams, lambdaWithParams }").WithLocation(9, 10),
                // (10,10): error CS0826: No best type found for implicitly-typed array
                // var a4 = new[] { (int[] xs) => xs.Length, (params int[] xs) => xs.Length }; // 4
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { (int[] xs) => xs.Length, (params int[] xs) => xs.Length }").WithLocation(10, 10));
        }

        [Fact]
        public void DefaultsParamsConversion_Spec()
        {
            var source = """
                int MethodNoDefault(int x) => x;
                int MethodWithDefault(int x = 2) => x;
                DelegateNoDefault d1 = MethodWithDefault;
                DelegateWithDefault d2 = MethodWithDefault;
                DelegateWithDefault d3 = MethodNoDefault;
                DelegateNoDefault d4 = (int x = 1) => x; // 1
                DelegateWithDefault d5 = (int x = 1) => x;
                DelegateWithDefault d6 = (int x = 2) => x; // 2
                DelegateWithDefault d7 = (int x) => x;
                DelegateNoDefault d8 = (int x) => x;

                int MethodNoParams(int[] xs) => xs.Length;
                int MethodWithParams(params int[] xs) => xs.Length;
                DelegateNoParams p1 = MethodWithParams;
                DelegateWithParams p2 = MethodNoParams;
                DelegateNoParams p3 = (params int[] xs) => xs.Length; // 3
                DelegateWithParams p4 = (params int[] xs) => xs.Length;
                DelegateWithParams p5 = (int[] xs) => xs.Length;
                DelegateNoParams p6 = (int[] xs) => xs.Length;

                delegate int DelegateNoDefault(int x);
                delegate int DelegateWithDefault(int x = 1);
                delegate int DelegateNoParams(int[] xs);
                delegate int DelegateWithParams(params int[] xs);
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (6,29): warning CS9099: Parameter 1 has default value '1' in lambda but '<missing>' in the target delegate type.
                // DelegateNoDefault d4 = (int x = 1) => x; // 1
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "1", "<missing>").WithLocation(6, 29),
                // (8,31): warning CS9099: Parameter 1 has default value '2' in lambda but '1' in the target delegate type.
                // DelegateWithDefault d6 = (int x = 2) => x; // 2
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "2", "1").WithLocation(8, 31),
                // (16,37): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                // DelegateNoParams p3 = (params int[] xs) => xs.Length; // 3
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "xs").WithArguments("1").WithLocation(16, 37));
        }

        [Fact]
        public void DefaultsParamsConversion_DelegateCreation()
        {
            var source = """
                int MethodNoDefault(int x) => x;
                int MethodWithDefault(int x = 2) => x;
                var d1 = new DelegateNoDefault(MethodWithDefault);
                var d2 = new DelegateWithDefault(MethodWithDefault);
                var d3 = new DelegateWithDefault(MethodNoDefault);
                var d4 = new DelegateNoDefault((int x = 1) => x); // 1
                var d5 = new DelegateWithDefault((int x = 1) => x);
                var d6 = new DelegateWithDefault((int x = 2) => x); // 2
                var d7 = new DelegateWithDefault((int x) => x);
                var d8 = new DelegateNoDefault((int x) => x);
                
                int MethodNoParams(int[] xs) => xs.Length;
                int MethodWithParams(params int[] xs) => xs.Length;
                var p1 = new DelegateNoParams(MethodWithParams);
                var p2 = new DelegateWithParams(MethodNoParams);
                var p3 = new DelegateNoParams((params int[] xs) => xs.Length); // 3
                var p4 = new DelegateWithParams((params int[] xs) => xs.Length);
                var p5 = new DelegateWithParams((int[] xs) => xs.Length);
                var p6 = new DelegateNoParams((int[] xs) => xs.Length);
                
                delegate int DelegateNoDefault(int x);
                delegate int DelegateWithDefault(int x = 1);
                delegate int DelegateNoParams(int[] xs);
                delegate int DelegateWithParams(params int[] xs);
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (6,37): warning CS9099: Parameter 1 has default value '1' in lambda but '<missing>' in the target delegate type.
                // var d4 = new DelegateNoDefault((int x = 1) => x); // 1
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "1", "<missing>").WithLocation(6, 37),
                // (8,39): warning CS9099: Parameter 1 has default value '2' in lambda but '1' in the target delegate type.
                // var d6 = new DelegateWithDefault((int x = 2) => x); // 2
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "2", "1").WithLocation(8, 39),
                // (16,45): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                // var p3 = new DelegateNoParams((params int[] xs) => xs.Length); // 3
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "xs").WithArguments("1").WithLocation(16, 45));
        }

        [Fact]
        public void DefaultsParamsConversion_ExplicitConversion()
        {
            var source = """
                int MethodNoDefault(int x) => x;
                int MethodWithDefault(int x = 2) => x;
                var d1 = (DelegateNoDefault)MethodWithDefault;
                var d2 = (DelegateWithDefault)MethodWithDefault;
                var d3 = (DelegateWithDefault)MethodNoDefault;
                var d4 = (DelegateNoDefault)((int x = 1) => x); // 1
                var d5 = (DelegateWithDefault)((int x = 1) => x);
                var d6 = (DelegateWithDefault)((int x = 2) => x); // 2
                var d7 = (DelegateWithDefault)((int x) => x);
                var d8 = (DelegateNoDefault)((int x) => x);
                
                int MethodNoParams(int[] xs) => xs.Length;
                int MethodWithParams(params int[] xs) => xs.Length;
                var p1 = (DelegateNoParams)MethodWithParams;
                var p2 = (DelegateWithParams)MethodNoParams;
                var p3 = (DelegateNoParams)((params int[] xs) => xs.Length); // 3
                var p4 = (DelegateWithParams)((params int[] xs) => xs.Length);
                var p5 = (DelegateWithParams)((int[] xs) => xs.Length);
                var p6 = (DelegateNoParams)((int[] xs) => xs.Length);
                
                delegate int DelegateNoDefault(int x);
                delegate int DelegateWithDefault(int x = 1);
                delegate int DelegateNoParams(int[] xs);
                delegate int DelegateWithParams(params int[] xs);
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (6,35): warning CS9099: Parameter 1 has default value '1' in lambda but '<missing>' in the target delegate type.
                // var d4 = (DelegateNoDefault)((int x = 1) => x); // 1
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "1", "<missing>").WithLocation(6, 35),
                // (8,37): warning CS9099: Parameter 1 has default value '2' in lambda but '1' in the target delegate type.
                // var d6 = (DelegateWithDefault)((int x = 2) => x); // 2
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "2", "1").WithLocation(8, 37),
                // (16,43): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                // var p3 = (DelegateNoParams)((params int[] xs) => xs.Length); // 3
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "xs").WithArguments("1").WithLocation(16, 43));
        }

        [Fact]
        public void DefaultsParamsConversion_Invocation()
        {
            var source = """
                NoDefault((int x = 1) => x); // 1
                WithDefault((int x = 1) => x);
                WithDefault((int x = 2) => x); // 2
                WithDefault((int x) => x);
                NoDefault((int x) => x);

                NoParams((params int[] xs) => xs.Length); // 3
                WithParams((params int[] xs) => xs.Length);
                WithParams((int[] xs) => xs.Length);
                NoParams((int[] xs) => xs.Length);

                static void NoDefault(DelegateNoDefault d) { }
                static void WithDefault(DelegateWithDefault d) { }
                static void NoParams(DelegateNoParams d) { }
                static void WithParams(DelegateWithParams d) { }
                
                delegate int DelegateNoDefault(int x);
                delegate int DelegateWithDefault(int x = 1);
                delegate int DelegateNoParams(int[] xs);
                delegate int DelegateWithParams(params int[] xs);
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,16): warning CS9099: Parameter 1 has default value '1' in lambda but '<missing>' in the target delegate type.
                // NoDefault((int x = 1) => x); // 1
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "1", "<missing>").WithLocation(1, 16),
                // (3,18): warning CS9099: Parameter 1 has default value '2' in lambda but '1' in the target delegate type.
                // WithDefault((int x = 2) => x); // 2
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "2", "1").WithLocation(3, 18),
                // (7,24): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                // NoParams((params int[] xs) => xs.Length); // 3
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "xs").WithArguments("1").WithLocation(7, 24));
        }

        [Fact]
        public void DefaultsParamsConversion_Mix()
        {
            var source = """
                D d1 = (int a, int b = 2, params int[] c) => { }; // 1, 2
                void M(int a, int b = 2, params int[] c) { }
                D d2 = M;
                delegate void D(int x, int y, int[] z);
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,20): warning CS9099: Parameter 2 has default value '2' in lambda but '<missing>' in the target delegate type.
                // D d1 = (int a, int b = 2, params int[] c) => { }; // 1, 2
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "b").WithArguments("2", "2", "<missing>").WithLocation(1, 20),
                // (1,40): warning CS9100: Parameter 3 has params modifier in lambda but not in target delegate type.
                // D d1 = (int a, int b = 2, params int[] c) => { }; // 1, 2
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "c").WithArguments("3").WithLocation(1, 40));
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71002")]
        public void ArrayInitializer_04()
        {
            var source =
@"class Program
{
    static void Main()
    {
        var tests = new[]
        {
        () =>
        };
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,14): error CS1525: Invalid expression term '}'
                //         () =>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(7, 14)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();

            Assert.Equal("System.Func<?>[] tests", model.GetDeclaredSymbol(declarator).ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(declarator.Initializer!.Value);
            Assert.Equal("System.Func<?>[]", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Func<?>[]", typeInfo.ConvertedType.ToTestDisplayString());

            typeInfo = model.GetTypeInfo(declarator.Initializer!.Value.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single());
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Func<?>", typeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71002")]
        public void ArrayInitializer_05()
        {
            var source =
@"class Program
{
    static void Main()
    {
        var tests = new[]
        {
        () => throw null
        };
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,21): error CS0826: No best type found for implicitly-typed array
                //         var tests = new[]
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, @"new[]
        {
        () => throw null
        }").WithLocation(5, 21)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();

            Assert.Equal("?[] tests", model.GetDeclaredSymbol(declarator).ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(declarator.Initializer!.Value);
            Assert.Equal("?[]", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("?[]", typeInfo.ConvertedType.ToTestDisplayString());

            typeInfo = model.GetTypeInfo(declarator.Initializer!.Value.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single());
            Assert.Null(typeInfo.Type);
            Assert.Equal("?", typeInfo.ConvertedType.ToTestDisplayString());
        }
    }
}
