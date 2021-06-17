// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    // Broad scenarios covered:
    // - AsyncMethodBuilderAttribute can be placed on an async method, which overrides the builder used
    // - The type used in an override needs only have a static Create() method which gives us the actual builder type
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

        private static string AsyncBuilderFactoryCode(string builderTypeName, string tasklikeTypeName, string? genericTypeParameter = null, bool isStruct = false)
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
        [InlineData("typeof(MyTaskMethodBuilder)")]
        [InlineData("typeof(object)")]
        [InlineData("null")]
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
                // (11,25): error CS8652: The feature 'async method builder override' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "F").WithArguments("async method builder override").WithLocation(11, 25),
                // (14,28): error CS8652: The feature 'async method builder override' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "G").WithArguments("async method builder override").WithLocation(14, 28),
                // (17,37): error CS8652: The feature 'async method builder override' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("async method builder override").WithLocation(17, 37)
                );

            compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(compilation, expectedOutput: "M F G 3");
            verifier.VerifyDiagnostics();
            var testData = verifier.TestData;
            var method = (MethodSymbol)testData.GetMethodData("C.F()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsAsyncReturningTask(compilation));
            method = (MethodSymbol)testData.GetMethodData("C.G<T>(T)").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsAsyncReturningGenericTask(compilation));
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
        public void BuilderOnMethod_NoBuilderOnType()
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
                // (11,25): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "F").WithLocation(11, 25),
                // (11,25): error CS0161: 'C.F()': not all code paths return a value
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "F").WithArguments("C.F()").WithLocation(11, 25),
                // (14,28): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "G").WithLocation(14, 28),
                // (17,37): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     public static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "M").WithLocation(17, 37)
                );
        }

        [Fact]
        public void BuilderOnMethod_NoBuilderOnType_Nullability()
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
            // Async methods must return task-like types
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics(
                // (11,28): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask<T> G<T>(T t)
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "G").WithLocation(11, 28),
                // (18,40): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     public static async MyTask<string> M()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "M").WithLocation(18, 40),
                // (24,41): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     public static async MyTask<string?> M2() { return await G((string?)null); }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "M2").WithLocation(24, 41),
                // (44,16): warning CS8618: Non-nullable field '_result' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     internal T _result;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "_result").WithArguments("field", "_result").WithLocation(44, 16)
                );
        }

        [Fact]
        public void BuilderOnMethod_NoBuilderOnType_BadReturns()
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
                // (9,25): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask F() { await Task.Yield(); return 1; } // 1
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "F").WithLocation(9, 25),
                // (12,28): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask<T> G<T>(T t) { await Task.Yield(); return; } // 2
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "G").WithLocation(12, 28),
                // (12,60): error CS0126: An object of a type convertible to 'MyTask<T>' is required
                //     static async MyTask<T> G<T>(T t) { await Task.Yield(); return; } // 2
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("MyTask<T>").WithLocation(12, 60),
                // (15,30): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask<int> M() { await Task.Yield(); return null; } // 3
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "M").WithLocation(15, 30),
                // (18,30): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask<int> M2(MyTask<int> mt) { await Task.Yield(); return mt; } // 4
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "M2").WithLocation(18, 30),
                // (21,25): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask M2(bool b) => b ? await Task.Yield() : await Task.Yield(); // 5
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "M2").WithLocation(21, 25)
                );
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
            source = source.Replace("DUMMY_BUILDER", dummyBuilder);
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular9);
            compilation.VerifyDiagnostics(
                // (9,21): error CS8652: The feature 'async method builder override' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "F").WithArguments("async method builder override").WithLocation(9, 21),
                // (12,24): error CS8652: The feature 'async method builder override' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "G").WithArguments("async method builder override").WithLocation(12, 24),
                // (15,26): error CS8652: The feature 'async method builder override' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("async method builder override").WithLocation(15, 26)
                );

            compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
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
                // (12,38): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithLocation(12, 38),
                // (15,41): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     public static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithLocation(15, 41)
                );
        }

        [Fact]
        public void BuilderOnMethod_WrongAccessibility()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[AsyncMethodBuilder(typeof(B1))] public class T1 {{ }}
[AsyncMethodBuilder(typeof(B2))] public class T2 {{ }}
[AsyncMethodBuilder(typeof(B3))] internal class T3 {{ }}
[AsyncMethodBuilder(typeof(B4))] internal class T4 {{ }}

{AsyncBuilderCode("B1", "T1").Replace("public class B1", "public class B1")}
{AsyncBuilderCode("B2", "T2").Replace("public class B2", "internal class B2")}
{AsyncBuilderCode("B3", "T3").Replace("public class B3", "public class B3").Replace("public T3 Task =>", "internal T3 Task =>")}
{AsyncBuilderCode("B4", "T4").Replace("public class B4", "internal class B4")}

class Program
{{
    async T1 f1() => await Task.Delay(1);
    async T2 f2() => await Task.Delay(2);
    async T3 f3() => await Task.Delay(3);
    async T4 f4() => await Task.Delay(4);
}}

{AsyncMethodBuilderAttribute}
";

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (75,19): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     async T2 f2() => await Task.Delay(2);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.Delay(2)").WithLocation(75, 19),
                // (76,19): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     async T3 f3() => await Task.Delay(3);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.Delay(3)").WithLocation(76, 19)
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

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(compilation, expectedOutput: "M F G 3", symbolValidator: verifyMembers);
            verifier.VerifyDiagnostics();
            var testData = verifier.TestData;
            var method = (MethodSymbol)testData.GetMethodData("C.F()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsAsyncReturningTask(compilation));
            method = (MethodSymbol)testData.GetMethodData("C.G<T>(T)").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsAsyncReturningGenericTask(compilation));

            // The initial builder type is used for Create() invocation
            verifier.VerifyIL("C.F()", @"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (C.<F>d__0 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""MyTaskMethodBuilder MyTaskMethodBuilderFactory.Create()""
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
}");
            // The final builder type is used for the rest
            verifier.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      153 (0x99)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldstr      ""F ""
    IL_000f:  call       ""void System.Console.Write(string)""
    IL_0014:  ldc.i4.0
    IL_0015:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_001f:  stloc.1
    IL_0020:  ldloca.s   V_1
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.1
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldfld      ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_003f:  ldloca.s   V_1
    IL_0041:  ldarg.0
    IL_0042:  callvirt   ""void MyTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__0)""
    IL_0047:  leave.s    IL_0098
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_004f:  stloc.1
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0065:  ldloca.s   V_1
    IL_0067:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_006c:  leave.s    IL_0085
  }
  catch System.Exception
  {
    IL_006e:  stloc.2
    IL_006f:  ldarg.0
    IL_0070:  ldc.i4.s   -2
    IL_0072:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0077:  ldarg.0
    IL_0078:  ldfld      ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_007d:  ldloc.2
    IL_007e:  callvirt   ""void MyTaskMethodBuilder.SetException(System.Exception)""
    IL_0083:  leave.s    IL_0098
  }
  IL_0085:  ldarg.0
  IL_0086:  ldc.i4.s   -2
  IL_0088:  stfld      ""int C.<F>d__0.<>1__state""
  IL_008d:  ldarg.0
  IL_008e:  ldfld      ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_0093:  callvirt   ""void MyTaskMethodBuilder.SetResult()""
  IL_0098:  ret
}
");

            // The initial builder type is used for Create() invocation
            verifier.VerifyIL("C.G<T>(T)", @"
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (C.<G>d__1<T> V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""MyTaskMethodBuilder<T> MyTaskMethodBuilderFactory<T>.Create()""
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
}");

            // The final builder type is used for the rest
            verifier.VerifyIL("C.<G>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      161 (0xa1)
  .maxstack  3
  .locals init (int V_0,
                T V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<G>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldstr      ""G ""
    IL_000f:  call       ""void System.Console.Write(string)""
    IL_0014:  ldc.i4.0
    IL_0015:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int C.<G>d__1<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<G>d__1<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldfld      ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  callvirt   ""void MyTaskMethodBuilder<T>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<G>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<G>d__1<T>)""
    IL_0047:  leave.s    IL_00a0
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<G>d__1<T>.<>u__1""
    IL_004f:  stloc.2
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<G>d__1<T>.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int C.<G>d__1<T>.<>1__state""
    IL_0065:  ldloca.s   V_2
    IL_0067:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_006c:  ldarg.0
    IL_006d:  ldfld      ""T C.<G>d__1<T>.t""
    IL_0072:  stloc.1
    IL_0073:  leave.s    IL_008c
  }
  catch System.Exception
  {
    IL_0075:  stloc.3
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.s   -2
    IL_0079:  stfld      ""int C.<G>d__1<T>.<>1__state""
    IL_007e:  ldarg.0
    IL_007f:  ldfld      ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
    IL_0084:  ldloc.3
    IL_0085:  callvirt   ""void MyTaskMethodBuilder<T>.SetException(System.Exception)""
    IL_008a:  leave.s    IL_00a0
  }
  IL_008c:  ldarg.0
  IL_008d:  ldc.i4.s   -2
  IL_008f:  stfld      ""int C.<G>d__1<T>.<>1__state""
  IL_0094:  ldarg.0
  IL_0095:  ldfld      ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
  IL_009a:  ldloc.1
  IL_009b:  callvirt   ""void MyTaskMethodBuilder<T>.SetResult(T)""
  IL_00a0:  ret
}
");

            void verifyMembers(ModuleSymbol module)
            {
                var fType = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C.<F>d__0");
                AssertEx.SetEqual(new[] {
                    "MyTaskMethodBuilder C.<F>d__0.<>t__builder",
                    "C.<F>d__0..ctor()",
                    "void C.<F>d__0.MoveNext()",
                    "void C.<F>d__0.SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)",
                    "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1",
                    "System.Int32 C.<F>d__0.<>1__state" },
                    fType.GetMembersUnordered().ToTestDisplayStrings());

                var gType = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C.<G>d__1");
                AssertEx.SetEqual(new[] {
                    "void C.<G>d__1<T>.SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)",
                    "MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder",
                    "T C.<G>d__1<T>.t",
                    "C.<G>d__1<T>..ctor()",
                    "void C.<G>d__1<T>.MoveNext()",
                    "System.Runtime.CompilerServices.TaskAwaiter C.<G>d__1<T>.<>u__1",
                    "System.Int32 C.<G>d__1<T>.<>1__state"},
                    gType.GetMembersUnordered().ToTestDisplayStrings());
            }
        }

        [Fact]
        public void BuilderFactoryOnMethod_OnLambda()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))] static async () => {{ System.Console.Write(""F ""); await Task.Delay(0); }};

Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))] async () => {{ System.Console.Write(""M ""); await f(); return 3; }};

Console.WriteLine(await m());
return;

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", isStruct: true)}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T", isStruct: true)}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", isStruct: true)}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T", isStruct: true)}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(compilation, expectedOutput: "M F 3");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void BuilderFactoryOnMethod_OnLambda_WithExplicitType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

var f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))] static async MyTask () => {{ System.Console.Write(""F ""); await Task.Delay(0); }};

var m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))] async MyTask<int> () => {{ System.Console.Write(""M ""); await f(); return 3; }};

Console.WriteLine(await m());
return;

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", isStruct: true)}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T", isStruct: true)}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", isStruct: true)}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T", isStruct: true)}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            // This test will be revisited once explicit lambda return types are allowed
            Assert.Equal(16, compilation.GetDiagnostics().Length);
            //var verifier = CompileAndVerify(compilation, expectedOutput: "M F 3");
            //verifier.VerifyDiagnostics();
        }

        [Fact]
        public void BuilderFactoryOnMethod_OnLambda_NotTaskLikeTypes()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))] static async () => {{ System.Console.Write(""F ""); await Task.Delay(0); }};

Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))] async () => {{ System.Console.Write(""M ""); await f(); return 3; }};

Console.WriteLine(await m());
return;

// no attribute
{AwaitableTypeCode("MyTask", isStruct: true)}

// no attribute
{AwaitableTypeCode("MyTask", "T", isStruct: true)}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", isStruct: true)}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T", isStruct: true)}
{AsyncMethodBuilderAttribute}
";
            // Async methods must return task-like types
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (6,91): error CS1643: Not all code paths return a value in lambda expression of type 'Func<MyTask>'
                // Func<MyTask> f = [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))] static async () => { System.Console.Write("F "); await Task.Delay(0); };
                Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "=>").WithArguments("lambda expression", "System.Func<MyTask>").WithLocation(6, 91),
                // (8,91): error CS4010: Cannot convert async lambda expression to delegate type 'Func<MyTask<int>>'. An async lambda expression may return void, Task or Task<T>, none of which are convertible to 'Func<MyTask<int>>'.
                // Func<MyTask<int>> m = [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))] async () => { System.Console.Write("M "); await f(); return 3; };
                Diagnostic(ErrorCode.ERR_CantConvAsyncAnonFuncReturns, "=>").WithArguments("lambda expression", "System.Func<MyTask<int>>").WithLocation(8, 91)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_OnLambda_NotTaskLikeTypes_InferReturnType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

C.F(
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))] static async () => {{ System.Console.Write(""Lambda1 ""); await Task.Delay(0); }}
);

C.F(
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))] static async () => {{ System.Console.Write(""Lambda2 ""); await Task.Delay(0); return 3; }}
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

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", isStruct: true)}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T", isStruct: true)}
{AsyncMethodBuilderAttribute}
";
            // Even if we allowed async lambdas to return non-task-like types, there would be an issue to be worked out
            // with type inference
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (7,78): error CS1643: Not all code paths return a value in lambda expression of type 'Func<MyTask>'
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))] static async () => { System.Console.Write("Lambda1 "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "=>").WithArguments("lambda expression", "System.Func<MyTask>").WithLocation(7, 78),
                // (11,80): error CS4010: Cannot convert async lambda expression to delegate type 'Func<MyTask>'. An async lambda expression may return void, Task or Task<T>, none of which are convertible to 'Func<MyTask>'.
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))] static async () => { System.Console.Write("Lambda2 "); await Task.Delay(0); return 3; }
                Diagnostic(ErrorCode.ERR_CantConvAsyncAnonFuncReturns, "=>").WithArguments("lambda expression", "System.Func<MyTask>").WithLocation(11, 80)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_TaskPropertyHasObjectType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public MyTask Task", "public object Task")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public MyTask<T> Task", "public object Task")}
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
        public void BuilderFactoryOnMethod_CreateMissing()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask")
    .Replace("public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder(new MyTask());", "")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T")
    .Replace("public static MyTaskMethodBuilder<T> Create() => new MyTaskMethodBuilder<T>(new MyTask<T>());", "")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilderFactory", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilderFactory<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilderFactory<int>", "Create").WithLocation(14, 34)
                );
        }

        [Theory]
        [InlineData("internal")]
        [InlineData("private")]
        public void BuilderFactoryOnMethod_CreateNotPublic(string accessibility)
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public static MyTaskMethodBuilder Create()", accessibility + " static MyTaskMethodBuilder Create()")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public static MyTaskMethodBuilder<T> Create()", accessibility + " static MyTaskMethodBuilder<T> Create()")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilderFactory", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilderFactory<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilderFactory<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_CreateNotStatic()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public static MyTaskMethodBuilder Create()", "public MyTaskMethodBuilder Create()")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public static MyTaskMethodBuilder<T> Create()", "public MyTaskMethodBuilder<T> Create()")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilderFactory", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilderFactory<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilderFactory<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_CreateHasParameter()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public static MyTaskMethodBuilder Create()", "public static MyTaskMethodBuilder Create(int i)")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public static MyTaskMethodBuilder<T> Create()", "public static MyTaskMethodBuilder<T> Create(int i)")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilderFactory", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilderFactory<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilderFactory<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_CreateIsGeneric()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public static MyTaskMethodBuilder Create()", "public static MyTaskMethodBuilder Create<U>()")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public static MyTaskMethodBuilder<T> Create()", "public static MyTaskMethodBuilder<T> Create<U>()")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilderFactory", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilderFactory<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilderFactory<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_CreateHasRefReturn()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask")
    .Replace(
        "public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder(new MyTask());",
        "public static ref MyTaskMethodBuilder Create() => throw null;")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T")
    .Replace(
        "public static MyTaskMethodBuilder<T> Create() => new MyTaskMethodBuilder<T>(new MyTask<T>());",
        "public static ref MyTaskMethodBuilder<T> Create() => throw null;")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilderFactory", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilderFactory<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilderFactory<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_BuilderFactoryIsInternal()
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

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public class MyTaskMethodBuilderFactory", "internal class MyTaskMethodBuilderFactory")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public class MyTaskMethodBuilderFactory<T>", "internal class MyTaskMethodBuilderFactory<T>")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation, expectedOutput: "M F G 3");
        }

        [Fact]
        public void BuilderFactoryOnMethod_BuilderFactoryIsPrivate()
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

    {AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public class MyTaskMethodBuilderFactory", "private class MyTaskMethodBuilderFactory")}
    {AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public class MyTaskMethodBuilderFactory<T>", "private class MyTaskMethodBuilderFactory<T>")}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation, expectedOutput: "M F G 3");
        }

        [Fact]
        public void BuilderFactoryOnMethod_CreateIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public static MyTaskMethodBuilder Create()", "internal static MyTaskMethodBuilder Create()")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public static MyTaskMethodBuilder<T> Create()", "internal static MyTaskMethodBuilder<T> Create()")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory.Create'
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithArguments("MyTaskMethodBuilderFactory", "Create").WithLocation(8, 29),
                // (11,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<T>.Create'
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilderFactory<T>", "Create").WithLocation(11, 38),
                // (14,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<int>.Create'
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithArguments("MyTaskMethodBuilderFactory<int>", "Create").WithLocation(14, 34)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_TwoMethodLevelAttributes()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await C.M());

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    [AsyncMethodBuilder(null)]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    [AsyncMethodBuilder(null)]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    [AsyncMethodBuilder(null)]
    public static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", isStruct: true)}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T", isStruct: true)}

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
        public void BuilderFactoryOnMethod_TwoMethodLevelAttributes_ReverseOrder()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{{
    [AsyncMethodBuilder(null)]
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(null)]
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(null)]
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T")}

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
        public void BuilderFactoryOnMethod_WrongArity()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (9,29): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask F() { System.Console.Write("F "); await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, @"{ System.Console.Write(""F ""); await Task.Delay(0); }").WithLocation(9, 29),
                // (12,38): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask<T> G<T>(T t) { System.Console.Write("G "); await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, @"{ System.Console.Write(""G ""); await Task.Delay(0); return t; }").WithLocation(12, 38),
                // (15,34): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask<int> M() { System.Console.Write("M "); await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, @"{ System.Console.Write(""M ""); await F(); return await G(3); }").WithLocation(15, 34)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_BoundGeneric_TypeParameter()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C<U>
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<U>))]
    static async MyTask<U> G<T>(T t) {{ await Task.Delay(0); throw null; }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (8,25): error CS0416: 'MyTaskMethodBuilderFactory<U>': an attribute argument cannot use type parameters
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<U>))]
                Diagnostic(ErrorCode.ERR_AttrArgWithTypeVars, "typeof(MyTaskMethodBuilderFactory<U>)").WithArguments("MyTaskMethodBuilderFactory<U>").WithLocation(8, 25)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_BoundGeneric_SpecificType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C<U>
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<int>))]
    static async MyTask<int> M() {{ await Task.Delay(0); throw null; }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T")}

{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics(
                // (9,34): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async MyTask<int> M() { await Task.Delay(0); throw null; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "{ await Task.Delay(0); throw null; }").WithLocation(9, 34)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_FinalBuilderTypeIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public static class MyTaskMethodBuilder", "internal static class MyTaskMethodBuilder")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public static class MyTaskMethodBuilder<T>", "internal static class MyTaskMethodBuilder<T>")}
{AsyncMethodBuilderAttribute}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyEmitDiagnostics();
        }

        [Fact]
        public void BuilderFactoryOnMethod_TaskPropertyIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public MyTask Task =>", "internal MyTask Task =>")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public MyTask<T> Task =>", "internal MyTask<T> Task =>")}
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
        public void BuilderFactoryOnMethod_TaskPropertyIsStatic()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public MyTask Task => _task;", "public static MyTask Task => throw null;")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public MyTask<T> Task => _task;", "public static MyTask<T> Task => throw null;")}
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
        public void BuilderFactoryOnMethod_TaskPropertyIsField()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public MyTask Task => _task;", "public static MyTask Task = null;")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public MyTask<T> Task => _task;", "public MyTask<T> Task = null;")}
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
        public void BuilderFactoryOnMethod_SetExceptionIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public void SetException", "internal void SetException")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void SetException", "internal void SetException")}
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
        public void BuilderFactoryOnMethod_SetExceptionReturnsObject()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask")
    .Replace("public void SetException(Exception e) { }", "public object SetException(Exception e) => null;")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T")
    .Replace("public void SetException(Exception e) { }", "public object SetException(Exception e) => null;")}

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
        public void BuilderFactoryOnMethod_SetExceptionLacksParameter()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public void SetException(Exception e)", "public void SetException()")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void SetException(Exception e)", "public void SetException()")}
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
        public void BuilderFactoryOnMethod_SetResultIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public void SetResult", "internal void SetResult")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void SetResult", "internal void SetResult")}
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
        public void BuilderFactoryOnMethod_AwaitOnCompletedIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public void AwaitOnCompleted", "internal void AwaitOnCompleted")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void AwaitOnCompleted", "internal void AwaitOnCompleted")}
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
        public void BuilderFactoryOnMethod_AwaitUnsafeOnCompletedIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public void AwaitUnsafeOnCompleted", "internal void AwaitUnsafeOnCompleted")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void AwaitUnsafeOnCompleted", "internal void AwaitUnsafeOnCompleted")}
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
        public void BuilderFactoryOnMethod_StartIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public void Start", "internal void Start")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void Start", "internal void Start")}
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
        public void BuilderFactoryOnMethod_SetStateMachineIsInternal()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async MyTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async MyTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask")}

[AsyncMethodBuilder(null)]
{AwaitableTypeCode("MyTask", "T")}

{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask").Replace("public void SetStateMachine", "internal void SetStateMachine")}
{AsyncBuilderFactoryCode("MyTaskMethodBuilder", "MyTask", "T").Replace("public void SetStateMachine", "internal void SetStateMachine")}
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
        public void BuilderFactoryOnMethod_AsyncMethodReturnsTask()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await C.M());

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async Task F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async Task<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    public static async Task<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

public class MyTaskMethodBuilderFactory
{{
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder();
}}

public class MyTaskMethodBuilder
{{
    internal MyTaskMethodBuilder() {{ }}
    public void SetStateMachine(IAsyncStateMachine stateMachine) {{ }}
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {{ stateMachine.MoveNext(); }}
    public void SetException(Exception e) {{ }}
    public void SetResult() {{  }}
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public Task Task => System.Threading.Tasks.Task.CompletedTask;
}}

public class MyTaskMethodBuilderFactory<T>
{{
    public static MyTaskMethodBuilder<T> Create() => new MyTaskMethodBuilder<T>();
}}

public class MyTaskMethodBuilder<T>
{{
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
        public void BuilderFactoryOnMethod_AsyncMethodReturnsValueTask()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.WriteLine(await C.M());

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
    static async ValueTask F() {{ System.Console.Write(""F ""); await Task.Delay(0); }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    static async ValueTask<T> G<T>(T t) {{ System.Console.Write(""G ""); await Task.Delay(0); return t; }}

    [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
    public static async ValueTask<int> M() {{ System.Console.Write(""M ""); await F(); return await G(3); }}
}}

public class MyTaskMethodBuilderFactory
{{
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder();
}}

public class MyTaskMethodBuilder
{{
    internal MyTaskMethodBuilder() {{ }}
    public void SetStateMachine(IAsyncStateMachine stateMachine) {{ }}
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {{ stateMachine.MoveNext(); }}
    public void SetException(Exception e) {{ }}
    public void SetResult() {{  }}
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public ValueTask Task => new ValueTask(System.Threading.Tasks.Task.CompletedTask);
}}

public class MyTaskMethodBuilderFactory<T>
{{
    public static MyTaskMethodBuilder<T> Create() => new MyTaskMethodBuilder<T>();
}}

public class MyTaskMethodBuilder<T>
{{
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
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "AsyncMethodBuilder").WithArguments("", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute", "System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute").WithLocation(10, 6),
                // (13,6): warning CS0436: The type 'AsyncMethodBuilderAttribute' in '' conflicts with the imported type 'AsyncMethodBuilderAttribute' in 'System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'. Using the type defined in ''.
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "AsyncMethodBuilder").WithArguments("", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute", "System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute").WithLocation(13, 6),
                // (16,6): warning CS0436: The type 'AsyncMethodBuilderAttribute' in '' conflicts with the imported type 'AsyncMethodBuilderAttribute' in 'System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'. Using the type defined in ''.
                //     [AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "AsyncMethodBuilder").WithArguments("", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute", "System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute").WithLocation(16, 6)
                );
            CompileAndVerify(compilation, expectedOutput: "M F G 3");
        }

        [Fact]
        public void BuilderFactoryOnMethod_WrongAccessibilityForFinalBuilderMembers()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[AsyncMethodBuilder(null)] public class T1 {{ }}
[AsyncMethodBuilder(null)] public class T2 {{ }}
[AsyncMethodBuilder(null)] internal class T3 {{ }}
[AsyncMethodBuilder(null)] internal class T4 {{ }}

{AsyncBuilderFactoryCode("B1", "T1")}

{AsyncBuilderFactoryCode("B2", "T2")
    .Replace("public class B2", "internal class B2")
    .Replace("internal class B2Factory", "public class B2Factory")
    .Replace("public static B2 Create()", "internal static B2 Create()")}

{AsyncBuilderFactoryCode("B3", "T3")
    .Replace("public T3 Task =>", "internal T3 Task =>")}

{AsyncBuilderFactoryCode("B4", "T4")
    .Replace("public class B4", "internal class B4")
    .Replace("internal class B4Factory", "public class B4Factory")
    .Replace("public static B4 Create()", "internal static B4 Create()")}

class Program
{{
    [AsyncMethodBuilder(typeof(B1Factory))]
    async T1 f1() => await Task.Delay(1);

    [AsyncMethodBuilder(typeof(B2Factory))]
    async T2 f2() => await Task.Delay(2);

    [AsyncMethodBuilder(typeof(B3Factory))]
    async T3 f3() => await Task.Delay(3);

    [AsyncMethodBuilder(typeof(B4Factory))]
    async T4 f4() => await Task.Delay(4);
}}

{AsyncMethodBuilderAttribute}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(
                // (97,19): error CS0656: Missing compiler required member 'B2Factory.Create'
                //     async T2 f2() => await Task.Delay(2);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(2)").WithArguments("B2Factory", "Create").WithLocation(97, 19),
                // (100,19): error CS0656: Missing compiler required member 'B3.Task'
                //     async T3 f3() => await Task.Delay(3);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(3)").WithArguments("B3", "Task").WithLocation(100, 19),
                // (103,19): error CS0656: Missing compiler required member 'B4Factory.Create'
                //     async T4 f4() => await Task.Delay(4);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(4)").WithArguments("B4Factory", "Create").WithLocation(103, 19)
                );
        }

        [Fact]
        public void BuilderFactoryOnMethod_InternalReturnType()
        {
            var source = $@"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[AsyncMethodBuilder(null)] internal class MyTaskType {{ }}

// Make the builder factory and the builder internal as well
{AsyncBuilderFactoryCode("MyTaskTypeBuilder", "MyTaskType").Replace("public class MyTaskType", "internal class MyTaskType") }

class C
{{
    [AsyncMethodBuilder(typeof(MyTaskTypeBuilderFactory))]
    async MyTaskType M() => await Task.Delay(4);
}}

{AsyncMethodBuilderAttribute}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();
        }
    }
}
