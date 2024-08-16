// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenAsyncMethodBuilderOverrideTests : EmitMetadataTestBase
    {
        private const string AsyncMethodBuilderAttribute =
            "namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } } ";

        private static string AsyncBuilderCode(string builderTypeName, string tasklikeTypeName, string? genericTypeParameter = null, bool isStruct = false)
        {
            string ofT = genericTypeParameter == null ? "" : "<" + genericTypeParameter + ">";

            return $@"
public {(isStruct ? "struct" : "class")} {builderTypeName}{ofT}
{{
    public static {builderTypeName}{ofT} Create() => new {builderTypeName}{ofT}(new {tasklikeTypeName}{ofT}());
    private {tasklikeTypeName}{ofT} _task;
    private {builderTypeName}({tasklikeTypeName}{ofT} task) {{ _task = task; }}
    public void SetStateMachine(IAsyncStateMachine stateMachine) {{ }}
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {{ stateMachine.MoveNext(); }}
    public void SetException(System.Exception e) {{ }}
    public void SetResult({(genericTypeParameter == null ? "" : genericTypeParameter + " result")}) {{ {(genericTypeParameter == null ? "" : "_task._result = result;")} }}
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public {tasklikeTypeName}{ofT} Task => _task;
}}
";
        }

        private static string AwaitableTypeCode(string taskLikeName, string? genericTypeParameter = null, bool isStruct = false)
        {
            if (genericTypeParameter == null)
            {
                return $@"
public {(isStruct ? "struct" : "class")} {taskLikeName}
{{
    internal Awaiter GetAwaiter() => new Awaiter();
    internal class Awaiter : INotifyCompletion
    {{
        public void OnCompleted(Action a) {{ }}
        internal bool IsCompleted => true;
        internal void GetResult() {{ }}
    }}
}}";
            }
            else
            {
                string ofT = "<" + genericTypeParameter + ">";
                return $@"
public {(isStruct ? "struct" : "class")} {taskLikeName}{ofT}
{{
    internal {genericTypeParameter} _result;
    public {genericTypeParameter} Result => _result;
    internal Awaiter GetAwaiter() => new Awaiter(this);
    internal class Awaiter : INotifyCompletion
    {{
        private readonly {taskLikeName}{ofT} _task;
        internal Awaiter({taskLikeName}{ofT} task) {{ _task = task; }}
        public void OnCompleted(Action a) {{ }}
        internal bool IsCompleted => true;
        internal {genericTypeParameter} GetResult() => _task.Result;
    }}
}}
";
            }
        }

        [Theory]
        [InlineData("[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]")]
        [InlineData("[AsyncMethodBuilder(typeof(object))]")]
        [InlineData("[AsyncMethodBuilder(null)]")]
        [InlineData("")]
        public void BuilderOnMethod_DummyBuilderOnType(string dummyBuilder)
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await C.M());

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    public static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{dummyBuilder}
{AwaitableTypeCode("MyTask")}

{dummyBuilder}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular9);
            compilation.VerifyDiagnostics(
                // (11,25): error CS8773: Feature 'async method builder override' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "F").WithArguments("async method builder override", "10.0").WithLocation(11, 25),
                // (14,28): error CS8773: Feature 'async method builder override' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "G").WithArguments("async method builder override", "10.0").WithLocation(14, 28),
                // (17,37): error CS8773: Feature 'async method builder override' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "M").WithArguments("async method builder override", "10.0").WithLocation(17, 37)
                );

            compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular10);
            var verifier = CompileAndVerify(compilation, expectedOutput: "M F G 3");
            verifier.VerifyDiagnostics();
            var testData = verifier.TestData;
            var method = (MethodSymbol)testData.GetMethodData("C.F()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsAsyncEffectivelyReturningTask(compilation));
            method = (MethodSymbol)testData.GetMethodData("C.G<T>(T)").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsAsyncEffectivelyReturningGenericTask(compilation));
            verifier.VerifyIL("C.F()", @"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (C.<F>d__0 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""MyTaskMethodBuilder MyTaskMethodBuilder.Create()""
  IL_0007:  stfld      ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0014:  ldloc.0
  IL_0015:  ldfld      ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_001a:  ldloca.s   V_0
  IL_001c:  callvirt   ""void MyTaskMethodBuilder.Start<C.<F>d__0>(ref C.<F>d__0)""
  IL_0021:  ldloc.0
  IL_0022:  ldfld      ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_0027:  callvirt   ""MyTask MyTaskMethodBuilder.Task.get""
  IL_002c:  ret
}
");
            verifier.VerifyIL("C.G<T>(T)", @"
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (C.<G>d__1<T> V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""MyTaskMethodBuilder<T> MyTaskMethodBuilder<T>.Create()""
  IL_0007:  stfld      ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldarg.0
  IL_000f:  stfld      ""T C.<G>d__1<T>.t""
  IL_0014:  ldloca.s   V_0
  IL_0016:  ldc.i4.m1
  IL_0017:  stfld      ""int C.<G>d__1<T>.<>1__state""
  IL_001c:  ldloc.0
  IL_001d:  ldfld      ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
  IL_0022:  ldloca.s   V_0
  IL_0024:  callvirt   ""void MyTaskMethodBuilder<T>.Start<C.<G>d__1<T>>(ref C.<G>d__1<T>)""
  IL_0029:  ldloc.0
  IL_002a:  ldfld      ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
  IL_002f:  callvirt   ""MyTask<T> MyTaskMethodBuilder<T>.Task.get""
  IL_0034:  ret
}
");
        }

        [Fact]
        public void BuilderOnMethod_Nullability()
        {
            var source = $@"
#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t)
    {{
        await Task.Delay(0);
        return default(T); // 1
    }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    public static async MyTask<string> M()
    {{
        return await G((string?)null); // 2
    }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    public static async MyTask<string?> M2() {{ return await G((string?)null); }}
}}

// no attribute
{AwaitableTypeCode("MyTask")}

// no attribute
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular10);
            compilation.VerifyDiagnostics(
                // (14,16): warning CS8603: Possible null reference return.
                //         return default(T); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "default(T)").WithLocation(14, 16),
                // (20,16): warning CS8603: Possible null reference return.
                //         return await G((string?)null); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "await G((string?)null)").WithLocation(20, 16),
                // (44,16): warning CS8618: Non-nullable field '_result' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     internal T _result;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "_result").WithArguments("field", "_result").WithLocation(44, 16)
                );
        }

        [Fact]
        public void BuilderOnMethod_BadReturns()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ await Task.Yield(); return 1; }} // 1

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ await Task.Yield(); return; }} // 2

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ await Task.Yield(); return null; }} // 3

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M2(MyTask<int> mt) {{ await Task.Yield(); return mt; }} // 4

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask M2(bool b) => b ? await Task.Yield() : await Task.Yield(); // 5
}}

// no attribute
{AwaitableTypeCode("MyTask")}

// no attribute
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (9,51): error CS1997: Since 'C.F()' is an async method that returns 'MyTask', a return keyword must not be followed by an object expression
                //     static async MyTask F() { await Task.Yield(); return 1; } // 1
                Diagnostic(ErrorCode.ERR_TaskRetNoObjectRequired, "return").WithArguments("C.F()", "MyTask").WithLocation(9, 51),
                // (12,60): error CS0126: An object of a type convertible to 'T' is required
                //     static async MyTask<T> G<T>(T t) { await Task.Yield(); return; } // 2
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("T").WithLocation(12, 60),
                // (15,63): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //     static async MyTask<int> M() { await Task.Yield(); return null; } // 3
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(15, 63),
                // (18,78): error CS4016: Since this is an async method, the return expression must be of type 'int' rather than 'MyTask<int>'
                //     static async MyTask<int> M2(MyTask<int> mt) { await Task.Yield(); return mt; } // 4
                Diagnostic(ErrorCode.ERR_BadAsyncReturnExpression, "mt").WithArguments("int", "MyTask<int>").WithLocation(18, 78),
                // (21,39): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //     static async MyTask M2(bool b) => b ? await Task.Yield() : await Task.Yield(); // 5
                Diagnostic(ErrorCode.ERR_IllegalStatement, "b ? await Task.Yield() : await Task.Yield()").WithLocation(21, 39)
                );
        }

        [Fact]
        public void BuilderOnMethod_IgnoreBadBuilderOnType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await C.M());

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    public static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(typeof(void))] // void
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(typeof(IgnoredTaskMethodBuilder))] // wrong arity
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}
{AsyncBuilderCode("IgnoredTaskMethodBuilder", "MyTask")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics();
        }

        [Fact]
        public void BuilderOnMethod_IgnoreBadBuilderOnType_CreateReturnsInt()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}
}}

[AsyncMethodBuilder(typeof(IgnoredTaskMethodBuilder))]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(typeof(IgnoredTaskMethodBuilder<>))]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncBuilderCode("IgnoredTaskMethodBuilder", "MyTask")
    .Replace("public static IgnoredTaskMethodBuilder Create() => new IgnoredTaskMethodBuilder(new MyTask());", "public static int Create() => 0;")}

{AsyncBuilderCode("IgnoredTaskMethodBuilder", "MyTask", "T")
    .Replace("public static IgnoredTaskMethodBuilder<T> Create() => new IgnoredTaskMethodBuilder<T>(new MyTask<T>());", "public static int Create() => 0;")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics();
        }

        [Fact]
        public void BuilderOnMethod_IgnoreBadBuilderOnType_TaskPropertyReturnsInt()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}
}}

[AsyncMethodBuilder(typeof(IgnoredTaskMethodBuilder))]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(typeof(IgnoredTaskMethodBuilder<>))]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncBuilderCode("IgnoredTaskMethodBuilder", "MyTask")
    .Replace("public MyTask Task => _task;", "public int Task => 0;")}

{AsyncBuilderCode("IgnoredTaskMethodBuilder", "MyTask", "T")
    .Replace("public MyTask<T> Task => _task;", "public int Task => 0;")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics();
        }

        [Fact]
        public void BuilderOnMethod_IgnoreBadBuilderOnType_SetExceptionIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}
}}

[AsyncMethodBuilder(typeof(IgnoredTaskMethodBuilder))]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(typeof(IgnoredTaskMethodBuilder<>))]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncBuilderCode("IgnoredTaskMethodBuilder", "MyTask")
    .Replace("public void SetException", "internal void SetException")
    .Replace("public void SetResult", "internal void SetResult")}

{AsyncBuilderCode("IgnoredTaskMethodBuilder", "MyTask", "T")
    .Replace("public void SetException", "internal void SetException")
    .Replace("public void SetResult", "internal void SetResult")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics();
        }

        [Fact]
        public void BuilderOnMethod_IgnoreBadBuilderOnType_SetResultIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}
}}

[AsyncMethodBuilder(typeof(IgnoredTaskMethodBuilder))]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(typeof(IgnoredTaskMethodBuilder<>))]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncBuilderCode("IgnoredTaskMethodBuilder", "MyTask")
    .Replace("public void SetResult", "internal void SetResult")}

{AsyncBuilderCode("IgnoredTaskMethodBuilder", "MyTask", "T")
    .Replace("public void SetResult", "internal void SetResult")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics();
        }

        [Theory]
        [InlineData("typeof(MyTaskMethodBuilder)")]
        [InlineData("typeof(object)")]
        [InlineData("null")]
        public void BuilderOnMethod_DummyBuilderOnType_OnLocalFunction(string dummyBuilder)
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await M());

[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}

[AsyncMethodBuilder({dummyBuilder})]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder({dummyBuilder})]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular9);
            compilation.VerifyDiagnostics(
                // (9,21): error CS8773: Feature 'async method builder override' is not available in C# 9.0. Please use language version 10.0 or greater.
                // static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "F").WithArguments("async method builder override", "10.0").WithLocation(9, 21),
                // (12,24): error CS8773: Feature 'async method builder override' is not available in C# 9.0. Please use language version 10.0 or greater.
                // static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "G").WithArguments("async method builder override", "10.0").WithLocation(12, 24),
                // (15,26): error CS8773: Feature 'async method builder override' is not available in C# 9.0. Please use language version 10.0 or greater.
                // static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "M").WithArguments("async method builder override", "10.0").WithLocation(15, 26)
                );

            compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular10);
            var verifier = CompileAndVerify(compilation, expectedOutput: "M F G 3");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void BuilderOnMethod_ErrorType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{{
    [AsyncMethodBuilder(typeof(Error))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(Error<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(Error<>))]
    public static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,32): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                //     [AsyncMethodBuilder(typeof(Error))]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(8, 32),
                // (11,32): error CS0246: The type or namespace name 'Error<>' could not be found (are you missing a using directive or an assembly reference?)
                //     [AsyncMethodBuilder(typeof(Error<>))]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error<>").WithArguments("Error<>").WithLocation(11, 32),
                // (14,32): error CS0246: The type or namespace name 'Error<>' could not be found (are you missing a using directive or an assembly reference?)
                //     [AsyncMethodBuilder(typeof(Error<>))]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error<>").WithArguments("Error<>").WithLocation(14, 32)
                );
        }

        [Fact]
        public void BuilderOnMethod_WrongArity()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    public static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncMethodBuilderAttribute}
";

            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (9,29): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithLocation(9, 29),
                // (12,38): error CS8940: A generic task-like return type was expected, but the type 'MyTaskMethodBuilder' found in 'AsyncMethodBuilder' attribute was not suitable. It must be an unbound generic type of arity one, and its containing type (if any) must be non-generic.
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_WrongArityAsyncReturn, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder").WithLocation(12, 38),
                // (15,41): error CS8940: A generic task-like return type was expected, but the type 'MyTaskMethodBuilder' found in 'AsyncMethodBuilder' attribute was not suitable. It must be an unbound generic type of arity one, and its containing type (if any) must be non-generic.
                //     public static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_WrongArityAsyncReturn, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder").WithLocation(15, 41)
                );
        }

        [Fact]
        public void BuilderOnMethod_IgnoreBuilderTypeAccessibility()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class T1 {{ }}
public class T2 {{ }}
internal class T3 {{ }}
internal class T4 {{ }}

{AsyncBuilderCode("B1", "T1").Replace("public class B1", "public class B1")}
{AsyncBuilderCode("B2", "T2").Replace("public class B2", "internal class B2")}
{AsyncBuilderCode("B3", "T3").Replace("public class B3", "public class B3").Replace("public T3 Task =>", "internal T3 Task =>")}
{AsyncBuilderCode("B4", "T4").Replace("public class B4", "internal class B4")}

class Program
{{
    [AsyncMethodBuilder(typeof(B1))] public async T1 F1() => await Task.Delay(1);
    [AsyncMethodBuilder(typeof(B2))] public async T2 F2() => await Task.Delay(2);
    [AsyncMethodBuilder(typeof(B3))] internal async T3 F3() => await Task.Delay(3);
    [AsyncMethodBuilder(typeof(B4))] internal async T4 F4() => await Task.Delay(4);
}}

{AsyncMethodBuilderAttribute}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(
                // (76,61): error CS0656: Missing compiler required member 'B3.Task'
                //     [AsyncMethodBuilder(typeof(B3))] internal async T3 F3() => await Task.Delay(3);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(3)").WithArguments("B3", "Task").WithLocation(76, 61)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await C.M());

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    public static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{asyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask")}
{asyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T", isStruct: true)}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (11,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilderFactory", "Create").WithLocation(11, 29),
                // (11,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory.Task'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilderFactory", "Task").WithLocation(11, 29),
                // (14,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilderFactory<T>", "Create").WithLocation(14, 38),
                // (14,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<T>.Task'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilderFactory<T>", "Task").WithLocation(14, 38),
                // (17,41): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<int>.Create'
                //     public static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilderFactory<int>", "Create").WithLocation(17, 41),
                // (17,41): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<int>.Task'
                //     public static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilderFactory<int>", "Task").WithLocation(17, 41)
                );

            static string asyncBuilderFactoryCode(string builderTypeName, string tasklikeTypeName, string? genericTypeParameter = null, bool isStruct = false)
            {
                string ofT = genericTypeParameter == null ? "" : "<" + genericTypeParameter + ">";

                return $@"
public {(isStruct ? "struct" : "class")} {builderTypeName}Factory{ofT}
{{
    public static {builderTypeName}{ofT} Create() => new {builderTypeName}{ofT}(new {tasklikeTypeName}{ofT}());
}}

public {(isStruct ? "struct" : "class")} {builderTypeName}{ofT}
{{
    private {tasklikeTypeName}{ofT} _task;
    internal {builderTypeName}({tasklikeTypeName}{ofT} task) {{ _task = task; }}
    public void SetStateMachine(IAsyncStateMachine stateMachine) {{ }}
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {{ stateMachine.MoveNext(); }}
    public void SetException(Exception e) {{ }}
    public void SetResult({(genericTypeParameter == null ? "" : genericTypeParameter + " result")}) {{ {(genericTypeParameter == null ? "" : "_task._result = result;")} }}
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public {tasklikeTypeName}{ofT} Task => _task;
}}
";
            }
        }

        [Fact]
        public void BuilderOnMethod_OnLambda()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async () => {{ System.Console.Write(""F ""); await Task.Delay(0); }};

Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] async () => {{ System.Console.Write(""M ""); await f(); return 3; }};

Console.WriteLine(await m());
return;

{AwaitableTypeCode("MyTask", isStruct: true)}
{AwaitableTypeCode("MyTask", "T", isStruct: true)}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", isStruct: true)}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T", isStruct: true)}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular9);
            compilation.VerifyEmitDiagnostics(
                // (6,18): error CS8773: Feature 'lambda attributes' is not available in C# 9.0. Please use language version 10.0 or greater.
                // Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async () => { System.Console.Write("F "); await Task.Delay(0); };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]").WithArguments("lambda attributes", "10.0").WithLocation(6, 18),
                // (6,84): error CS8935: The AsyncMethodBuilder attribute is disallowed on anonymous methods without an explicit return type.
                // Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async () => { System.Console.Write("F "); await Task.Delay(0); };
                Diagnostic(ErrorCode.ERR_BuilderAttributeDisallowed, "=>").WithLocation(6, 84),
                // (6,84): error CS8773: Feature 'async method builder override' is not available in C# 9.0. Please use language version 10.0 or greater.
                // Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async () => { System.Console.Write("F "); await Task.Delay(0); };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "=>").WithArguments("async method builder override", "10.0").WithLocation(6, 84),
                // (8,23): error CS8773: Feature 'lambda attributes' is not available in C# 9.0. Please use language version 10.0 or greater.
                // Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] async () => { System.Console.Write("M "); await f(); return 3; };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]").WithArguments("lambda attributes", "10.0").WithLocation(8, 23),
                // (8,84): error CS8935: The AsyncMethodBuilder attribute is disallowed on anonymous methods without an explicit return type.
                // Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] async () => { System.Console.Write("M "); await f(); return 3; };
                Diagnostic(ErrorCode.ERR_BuilderAttributeDisallowed, "=>").WithLocation(8, 84),
                // (8,84): error CS8773: Feature 'async method builder override' is not available in C# 9.0. Please use language version 10.0 or greater.
                // Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] async () => { System.Console.Write("M "); await f(); return 3; };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "=>").WithArguments("async method builder override", "10.0").WithLocation(8, 84)
                );
            compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular10);
            compilation.VerifyEmitDiagnostics(
                // (6,84): error CS8933: The AsyncMethodBuilder attribute is disallowed on anonymous methods without an explicit return type.
                // Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async () => { System.Console.Write("F "); await Task.Delay(0); };
                Diagnostic(ErrorCode.ERR_BuilderAttributeDisallowed, "=>").WithLocation(6, 84),
                // (8,84): error CS8933: The AsyncMethodBuilder attribute is disallowed on anonymous methods without an explicit return type.
                // Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] async () => { System.Console.Write("M "); await f(); return 3; };
                Diagnostic(ErrorCode.ERR_BuilderAttributeDisallowed, "=>").WithLocation(8, 84)
                );
        }

        [Fact]
        public void BuilderOnMethod_OnLambda_WithExplicitType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async MyTask () => {{ System.Console.Write(""F ""); await Task.Delay(0); }};

Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] async MyTask<int> () => {{ System.Console.Write(""M ""); await f(); return 3; }};

Console.WriteLine(await m());
return;

{AwaitableTypeCode("MyTask", isStruct: true)}
{AwaitableTypeCode("MyTask", "T", isStruct: true)}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", isStruct: true)}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T", isStruct: true)}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular9);
            compilation.VerifyEmitDiagnostics(
                // (6,18): error CS8773: Feature 'lambda attributes' is not available in C# 9.0. Please use language version 10.0 or greater.
                // Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async MyTask () => { System.Console.Write("F "); await Task.Delay(0); };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]").WithArguments("lambda attributes", "10.0").WithLocation(6, 18),
                // (6,81): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                // Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async MyTask () => { System.Console.Write("F "); await Task.Delay(0); };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "MyTask").WithArguments("lambda return type", "10.0").WithLocation(6, 81),
                // (6,91): error CS8773: Feature 'async method builder override' is not available in C# 9.0. Please use language version 10.0 or greater.
                // Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async MyTask () => { System.Console.Write("F "); await Task.Delay(0); };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "=>").WithArguments("async method builder override", "10.0").WithLocation(6, 91),
                // (8,23): error CS8773: Feature 'lambda attributes' is not available in C# 9.0. Please use language version 10.0 or greater.
                // Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] async MyTask<int> () => { System.Console.Write("M "); await f(); return 3; };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]").WithArguments("lambda attributes", "10.0").WithLocation(8, 23),
                // (8,81): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                // Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] async MyTask<int> () => { System.Console.Write("M "); await f(); return 3; };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "MyTask<int>").WithArguments("lambda return type", "10.0").WithLocation(8, 81),
                // (8,96): error CS8773: Feature 'async method builder override' is not available in C# 9.0. Please use language version 10.0 or greater.
                // Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] async MyTask<int> () => { System.Console.Write("M "); await f(); return 3; };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "=>").WithArguments("async method builder override", "10.0").WithLocation(8, 96)
                );

            compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular10);
            var verifier = CompileAndVerify(compilation, expectedOutput: "M F 3");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void BuilderOnMethod_OnLambda_NotTaskLikeTypes_InferReturnType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

C.F(
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async () => {{ System.Console.Write(""Lambda1 ""); await Task.Delay(0); }} // 1
);

C.F(
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] static async () => {{ System.Console.Write(""Lambda2 ""); await Task.Delay(0); return 3; }} // 2
);

await Task.Delay(0);
return;

public class C
{{
    public static void F(Func<MyTask> f) {{ System.Console.Write(""Overload1 ""); f().GetAwaiter().GetResult(); }}
    public static void F<T>(Func<MyTask<T>> f) {{ System.Console.Write(""Overload2 ""); f().GetAwaiter().GetResult(); }}
    public static void F(Func<MyTask<string>> f) {{ System.Console.Write(""Overload3 ""); f().GetAwaiter().GetResult(); }}
}}

// no attribute
{AwaitableTypeCode("MyTask", isStruct: true)}

// no attribute
{AwaitableTypeCode("MyTask", "T", isStruct: true)}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", isStruct: true)}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T", isStruct: true)}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (7,71): error CS8935: The AsyncMethodBuilder attribute is disallowed on anonymous methods without an explicit return type.
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async () => { System.Console.Write("Lambda1 "); await Task.Delay(0); } // 1
                Diagnostic(ErrorCode.ERR_BuilderAttributeDisallowed, "=>").WithLocation(7, 71),
                // (11,73): error CS8935: The AsyncMethodBuilder attribute is disallowed on anonymous methods without an explicit return type.
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] static async () => { System.Console.Write("Lambda2 "); await Task.Delay(0); return 3; } // 2
                Diagnostic(ErrorCode.ERR_BuilderAttributeDisallowed, "=>").WithLocation(11, 73)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
            var lambdas = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToArray();
            var firstLambda = model.GetTypeInfo(lambdas[0]);
            Assert.Null(firstLambda.Type);
            Assert.Equal("System.Func<MyTask>", firstLambda.ConvertedType.ToTestDisplayString());

            var secondLambda = model.GetTypeInfo(lambdas[1]);
            Assert.Null(secondLambda.Type);
            Assert.Equal("System.Func<MyTask>", secondLambda.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void BuilderOnMethod_OnLambda_NotTaskLikeTypes_ExplicitReturnType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

C.F(
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))] static async MyTask () => {{ System.Console.Write(""Lambda1 ""); await Task.Delay(0); }} // 1
);

C.F(
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))] static async MyTask<int> () => {{ System.Console.Write(""Lambda2 ""); await Task.Delay(0); return 3; }} // 2
);

await Task.Delay(0);
return;

public class C
{{
    public static void F(Func<MyTask> f) {{ System.Console.Write(""Overload1 ""); f().GetAwaiter().GetResult(); }}
    public static void F<T>(Func<MyTask<T>> f) {{ System.Console.Write(""Overload2 ""); f().GetAwaiter().GetResult(); }}
    public static void F(Func<MyTask<string>> f) {{ System.Console.Write(""Overload3 ""); f().GetAwaiter().GetResult(); }}
}}

// no attribute
{AwaitableTypeCode("MyTask", isStruct: true)}

// no attribute
{AwaitableTypeCode("MyTask", "T", isStruct: true)}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", isStruct: true)}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T", isStruct: true)}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(compilation, expectedOutput: "Overload1 Lambda1 Overload2 Lambda2");
            verifier.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
            var lambdas = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToArray();
            var firstLambda = model.GetTypeInfo(lambdas[0]);
            Assert.Null(firstLambda.Type);
            Assert.Equal("System.Func<MyTask>", firstLambda.ConvertedType.ToTestDisplayString());

            var secondLambda = model.GetTypeInfo(lambdas[1]);
            Assert.Null(secondLambda.Type);
            Assert.Equal("System.Func<MyTask<System.Int32>>", secondLambda.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void BuilderOnMethod_TaskPropertyHasObjectType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public MyTask Task", "public object Task")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public MyTask<T> Task", "public object Task")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS8204: For type 'MyTaskMethodBuilder' to be used as an AsyncMethodBuilder for type 'MyTask', its Task property should return type 'MyTask' instead of type 'object'.
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_BadAsyncMethodBuilderTaskProperty, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "MyTask", "object").WithLocation(8, 29),
                // (11,38): error CS8204: For type 'MyTaskMethodBuilder<T>' to be used as an AsyncMethodBuilder for type 'MyTask<T>', its Task property should return type 'MyTask<T>' instead of type 'object'.
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_BadAsyncMethodBuilderTaskProperty, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "MyTask<T>", "object").WithLocation(11, 38),
                // (14,34): error CS8204: For type 'MyTaskMethodBuilder<int>' to be used as an AsyncMethodBuilder for type 'MyTask<int>', its Task property should return type 'MyTask<int>' instead of type 'object'.
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_BadAsyncMethodBuilderTaskProperty, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "MyTask<int>", "object").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_CreateMissing()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")
    .Replace("public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder(new MyTask());", "")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")
    .Replace("public static MyTaskMethodBuilder<T> Create() => new MyTaskMethodBuilder<T>(new MyTask<T>());", "")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Create").WithLocation(14, 34)
                );
        }

        [Theory]
        [InlineData("internal")]
        [InlineData("private")]
        public void BuilderOnMethod_CreateNotPublic(string accessibility)
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public static MyTaskMethodBuilder Create()", accessibility + " static MyTaskMethodBuilder Create()")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public static MyTaskMethodBuilder<T> Create()", accessibility + " static MyTaskMethodBuilder<T> Create()")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_CreateNotStatic()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public static MyTaskMethodBuilder Create()", "public MyTaskMethodBuilder Create()")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public static MyTaskMethodBuilder<T> Create()", "public MyTaskMethodBuilder<T> Create()")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_CreateHasParameter()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public static MyTaskMethodBuilder Create()", "public static MyTaskMethodBuilder Create(int i)")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public static MyTaskMethodBuilder<T> Create()", "public static MyTaskMethodBuilder<T> Create(int i)")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_CreateIsGeneric()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public static MyTaskMethodBuilder Create()", "public static MyTaskMethodBuilder Create<U>()")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public static MyTaskMethodBuilder<T> Create()", "public static MyTaskMethodBuilder<T> Create<U>()")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_CreateHasRefReturn()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")
    .Replace(
        "public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder(new MyTask());",
        "public static ref MyTaskMethodBuilder Create() => throw null;")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")
    .Replace(
        "public static MyTaskMethodBuilder<T> Create() => new MyTaskMethodBuilder<T>(new MyTask<T>());",
        "public static ref MyTaskMethodBuilder<T> Create() => throw null;")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_BuilderIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await C.M());

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    public static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public class MyTaskMethodBuilder", "internal class MyTaskMethodBuilder")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public class MyTaskMethodBuilder<T>", "internal class MyTaskMethodBuilder<T>")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation, expectedOutput: "M F G 3");
        }

        [Fact]
        public void BuilderOnMethod_BuilderIsPrivate()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await C.M());

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    public static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}

    {AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public class MyTaskMethodBuilder", "private class MyTaskMethodBuilder")}
    {AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public class MyTaskMethodBuilder<T>", "private class MyTaskMethodBuilder<T>")}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation, expectedOutput: "M F G 3");
        }

        [Fact]
        public void BuilderOnMethod_CreateIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public static MyTaskMethodBuilder Create()", "internal static MyTaskMethodBuilder Create()")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public static MyTaskMethodBuilder<T> Create()", "internal static MyTaskMethodBuilder<T> Create()")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_TwoMethodLevelAttributes()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await C.M());

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    [AsyncMethodBuilder(null)]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    [AsyncMethodBuilder(null)]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    [AsyncMethodBuilder(null)]
    public static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", isStruct: true)}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T", isStruct: true)}

namespace System.Runtime.CompilerServices
{{
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple=true)]
    class AsyncMethodBuilderAttribute : System.Attribute {{ public AsyncMethodBuilderAttribute(System.Type t) {{ }} }}
}}
";
            // The first attribute is used
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "M F G 3");
        }

        [Fact]
        public void BuilderOnMethod_TwoMethodLevelAttributes_ReverseOrder()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{{
    [AsyncMethodBuilder(null)]
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(null)]
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(null)]
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

namespace System.Runtime.CompilerServices
{{
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple=true)]
    class AsyncMethodBuilderAttribute : System.Attribute {{ public AsyncMethodBuilderAttribute(System.Type t) {{ }} }}
}}
";
            // The first attribute is used
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (10,29): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithLocation(10, 29),
                // (14,38): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithLocation(14, 38),
                // (18,34): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithLocation(18, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_BoundGeneric_TypeParameter()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C<U>
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<U>))]
    static async MyTask<U> G<T>(T t) {{ await Task.Delay(0); throw null; }}
}}

{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,25): error CS0416: 'MyTaskMethodBuilder<U>': an attribute argument cannot use type parameters
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<U>))]
                Diagnostic(ErrorCode.ERR_AttrArgWithTypeVars, "typeof(MyTaskMethodBuilder<U>)").WithArguments("MyTaskMethodBuilder<U>").WithLocation(8, 25)
                );
        }

        [Fact]
        public void BuilderOnMethod_BoundGeneric_SpecificType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C<U>
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<int>))]
    static async MyTask<int> M() {{ await Task.Delay(0); throw null; }}
}}

{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyEmitDiagnostics(
                // (9,34): error CS8940: A generic task-like return type was expected, but the type 'MyTaskMethodBuilder<int>' found in 'AsyncMethodBuilder' attribute was not suitable. It must be an unbound generic type of arity one, and its containing type (if any) must be non-generic.
                //     static async MyTask<int> M() { await Task.Delay(0); throw null; }
                Diagnostic(ErrorCode.ERR_WrongArityAsyncReturn, "{ await Task.Delay(0); throw null; }").WithArguments("MyTaskMethodBuilder<int>").WithLocation(9, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_TaskPropertyIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public MyTask Task =>", "internal MyTask Task =>")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public MyTask<T> Task =>", "internal MyTask<T> Task =>")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Task'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Task").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Task'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Task").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Task'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Task").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_TaskPropertyIsStatic()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public MyTask Task => _task;", "public static MyTask Task => throw null;")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public MyTask<T> Task => _task;", "public static MyTask<T> Task => throw null;")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Task'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Task").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Task'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Task").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Task'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Task").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_TaskPropertyIsField()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public MyTask Task => _task;", "public static MyTask Task = null;")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public MyTask<T> Task => _task;", "public MyTask<T> Task = null;")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Task'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Task").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Task'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Task").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Task'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Task").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_SetExceptionIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public void SetException", "internal void SetException")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void SetException", "internal void SetException")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.SetException'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "SetException").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.SetException'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "SetException").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.SetException'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "SetException").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_SetExceptionReturnsObject()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask")
    .Replace("public void SetException(System.Exception e) { }", "public object SetException(System.Exception e) => null;")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T")
    .Replace("public void SetException(System.Exception e) { }", "public object SetException(System.Exception e) => null;")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.SetException'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "SetException").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.SetException'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "SetException").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.SetException'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "SetException").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_SetExceptionLacksParameter()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public void SetException(System.Exception e)", "public void SetException()")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void SetException(System.Exception e)", "public void SetException()")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.SetException'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "SetException").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.SetException'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "SetException").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.SetException'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "SetException").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_SetResultIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public void SetResult", "internal void SetResult")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void SetResult", "internal void SetResult")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.SetResult'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "SetResult").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.SetResult'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "SetResult").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.SetResult'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "SetResult").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_AwaitOnCompletedIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public void AwaitOnCompleted", "internal void AwaitOnCompleted")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void AwaitOnCompleted", "internal void AwaitOnCompleted")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.AwaitOnCompleted'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "AwaitOnCompleted").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.AwaitOnCompleted'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "AwaitOnCompleted").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.AwaitOnCompleted'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "AwaitOnCompleted").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_AwaitUnsafeOnCompletedIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public void AwaitUnsafeOnCompleted", "internal void AwaitUnsafeOnCompleted")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void AwaitUnsafeOnCompleted", "internal void AwaitUnsafeOnCompleted")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.AwaitUnsafeOnCompleted'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "AwaitUnsafeOnCompleted").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.AwaitUnsafeOnCompleted'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "AwaitUnsafeOnCompleted").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.AwaitUnsafeOnCompleted'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "AwaitUnsafeOnCompleted").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_StartIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public void Start", "internal void Start")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void Start", "internal void Start")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Start'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Start").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Start'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Start").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Start'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Start").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_SetStateMachineIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

{AwaitableTypeCode("MyTask")}
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask").Replace("public void SetStateMachine", "internal void SetStateMachine")}
{AsyncBuilderCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void SetStateMachine", "internal void SetStateMachine")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.SetStateMachine'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "SetStateMachine").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.SetStateMachine'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "SetStateMachine").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.SetStateMachine'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "SetStateMachine").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderOnMethod_AsyncMethodReturnsTask()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await C.M());

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async Task F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async Task<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    public static async Task<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

public class MyTaskMethodBuilder
{{
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder();
    internal MyTaskMethodBuilder() {{ }}
    public void SetStateMachine(IAsyncStateMachine stateMachine) {{ }}
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {{ stateMachine.MoveNext(); }}
    public void SetException(Exception e) {{ }}
    public void SetResult() {{  }}
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public Task Task => System.Threading.Tasks.Task.CompletedTask;
}}

public class MyTaskMethodBuilder<T>
{{
    public static MyTaskMethodBuilder<T> Create() => new MyTaskMethodBuilder<T>();
    private TaskCompletionSource<T> _taskCompletionSource = new TaskCompletionSource<T>();
    internal MyTaskMethodBuilder() {{ }}
    public void SetStateMachine(IAsyncStateMachine stateMachine) {{ }}
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {{ stateMachine.MoveNext(); }}
    public void SetException(Exception e) {{ }}
    public void SetResult(T result) {{ _taskCompletionSource.SetResult(result); }}
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public Task<T> Task => _taskCompletionSource.Task;
}}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "M F G 3");
        }

        [Fact]
        public void BuilderOnMethod_AsyncMethodReturnsValueTask()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await C.M());

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    static async ValueTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    static async ValueTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    public static async ValueTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

public class MyTaskMethodBuilder
{{
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder();
    internal MyTaskMethodBuilder() {{ }}
    public void SetStateMachine(IAsyncStateMachine stateMachine) {{ }}
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {{ stateMachine.MoveNext(); }}
    public void SetException(Exception e) {{ }}
    public void SetResult() {{  }}
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public ValueTask Task => new ValueTask(System.Threading.Tasks.Task.CompletedTask);
}}

public class MyTaskMethodBuilder<T>
{{
    public static MyTaskMethodBuilder<T> Create() => new MyTaskMethodBuilder<T>();
    private TaskCompletionSource<T> _taskCompletionSource = new TaskCompletionSource<T>();
    internal MyTaskMethodBuilder() {{ }}
    public void SetStateMachine(IAsyncStateMachine stateMachine) {{ }}
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {{ stateMachine.MoveNext(); }}
    public void SetException(Exception e) {{ }}
    public void SetResult(T result) {{ _taskCompletionSource.SetResult(result); }}
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public ValueTask<T> Task => new ValueTask<T>(_taskCompletionSource.Task);
}}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (10,6): warning CS0436: The type 'AsyncMethodBuilderAttribute' in '' conflicts with the imported type 'AsyncMethodBuilderAttribute' in 'System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'. Using the type defined in ''.
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "AsyncMethodBuilder").WithArguments("", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute", "System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute").WithLocation(10, 6),
                // (13,6): warning CS0436: The type 'AsyncMethodBuilderAttribute' in '' conflicts with the imported type 'AsyncMethodBuilderAttribute' in 'System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'. Using the type defined in ''.
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "AsyncMethodBuilder").WithArguments("", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute", "System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute").WithLocation(13, 6),
                // (16,6): warning CS0436: The type 'AsyncMethodBuilderAttribute' in '' conflicts with the imported type 'AsyncMethodBuilderAttribute' in 'System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'. Using the type defined in ''.
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "AsyncMethodBuilder").WithArguments("", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute", "System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute").WithLocation(16, 6)
                );
            CompileAndVerify(compilation, expectedOutput: "M F G 3");
        }

        [Fact]
        public void BuilderOnMethod_InternalReturnType()
        {
            var source = $@"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[AsyncMethodBuilder(null)] internal class MyTaskType {{ }}

// Make the builder factory and the builder internal as well
{AsyncBuilderCode("MyTaskTypeBuilder", "MyTaskType").Replace("public class MyTaskType", "internal class MyTaskType")}

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskTypeBuilder))]
    async MyTaskType M() => await Task.Delay(4);
}}

{AsyncMethodBuilderAttribute}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void BuilderOnMethod_IntReturnType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

System.Console.Write(new C().M());

[AsyncMethodBuilder(typeof(IgnoredTaskMethodBuilder))] public class MyTaskType {{ }}

public class C
{{
    [AsyncMethodBuilder(typeof(MyTaskTypeBuilder))]
    public async int M() => await Task.Delay(4);
}}

public class MyTaskTypeBuilder
{{
    public static MyTaskTypeBuilder Create() => new MyTaskTypeBuilder();
    public void SetStateMachine(IAsyncStateMachine stateMachine) {{ }}
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {{ stateMachine.MoveNext(); }}
    public void SetException(Exception e) {{ }}
    public void SetResult() {{  }}
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public int Task => 42;
}}

{AsyncBuilderCode("IgnoredTaskMethodBuilder", "MyTaskType")}
{AsyncMethodBuilderAttribute}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyDiagnostics();
        }
    }
}
