// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to await expressions.
    /// </summary>
    public class AwaitExpressionTests : CompilingTestBase
    {
        [Fact]
        public void TestAwaitInfoExtensionMethod()
        {
            var text =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static class App{
    public static async Task Main(){
        var x = new MyAwaitable();
        x.SetValue(42);

        Console.WriteLine(await x + ""!"");
    }
}

struct MyAwaitable
{
    private ValueTask<int> task;
    private TaskCompletionSource<int> source;

    private TaskCompletionSource<int> Source
    {
        get
        {
            if (source == null)
            {
                source = new TaskCompletionSource<int>();
                task = new ValueTask<int>(source.Task);
            }
            return source;
        }
    }
    internal ValueTask<int> Task
    {
        get
        {
            _ = Source;
            return task;
        }
    }

    public void SetValue(int i)
    {
        Source.SetResult(i);
    }
}

static class MyAwaitableExtension
{
    public static System.Runtime.CompilerServices.ValueTaskAwaiter<int> GetAwaiter(this MyAwaitable a)
    {
        return a.Task.GetAwaiter();
    }
}";

            var csCompilation = CreateCompilation(text, targetFramework: TargetFramework.NetCoreApp);
            var tree = csCompilation.SyntaxTrees.Single();

            var model = csCompilation.GetSemanticModel(tree);
            var awaitExpression = tree.GetRoot().DescendantNodes().OfType<AwaitExpressionSyntax>().First();
            Assert.Equal("await x", awaitExpression.ToString());

            var info = model.GetAwaitExpressionInfo(awaitExpression);
            Assert.Equal(
                "System.Runtime.CompilerServices.ValueTaskAwaiter<System.Int32> MyAwaitableExtension.GetAwaiter(this MyAwaitable a)",
                info.GetAwaiterMethod.ToTestDisplayString()
            );
            Assert.Equal(
                "System.Int32 System.Runtime.CompilerServices.ValueTaskAwaiter<System.Int32>.GetResult()",
                info.GetResultMethod.ToTestDisplayString()
            );
            Assert.Equal(
                "System.Boolean System.Runtime.CompilerServices.ValueTaskAwaiter<System.Int32>.IsCompleted { get; }",
                info.IsCompletedProperty.ToTestDisplayString()
            );
            Assert.Null(info.RuntimeAwaitMethod);
        }

        [Fact]
        [WorkItem(711413, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/711413")]
        public void TestAwaitInfo()
        {
            var text =
@"using System.Threading.Tasks;

class C
{
    async void Goo(Task<int> t)
    {
        int c = 1 + await t;
    }
}";
            var info = GetAwaitExpressionInfo(text);
            Assert.Equal("System.Runtime.CompilerServices.TaskAwaiter<System.Int32> System.Threading.Tasks.Task<System.Int32>.GetAwaiter()", info.GetAwaiterMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Runtime.CompilerServices.TaskAwaiter<System.Int32>.GetResult()", info.GetResultMethod.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Runtime.CompilerServices.TaskAwaiter<System.Int32>.IsCompleted { get; }", info.IsCompletedProperty.ToTestDisplayString());
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/76999")]
        public void TestAwaitHoistedRef()
        {
            var src = """
                using System.Threading.Tasks;

                public sealed class RefHolder<T>
                {
                    private T _t;
                    public ref T Get() => ref _t;
                }

                public static class App
                {
                    public static void Do<T>()
                    {
                        var res = new RefHolder<T>();
                        M().Wait();
                        async Task M()
                        {
                            res.Get() = await Task.FromResult(default(T));
                        }
                    }
                }
                """;

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (17,13): error CS8178: A reference returned by a call to 'RefHolder<T>.Get()' cannot be preserved across 'await' or 'yield' boundary.
                //             res.Get() = await Task.FromResult(default(T));
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "res.Get()").WithArguments("RefHolder<T>.Get()").WithLocation(17, 13)
            );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/76999")]
        public void TestAwaitHoistedRef_InNewExtensionContainer()
        {
            var src = """
                using System.Threading.Tasks;

                public sealed class RefHolder<T>
                {
                    private T _t;
                    public ref T Get() => ref _t;
                }

                public static class App
                {
                    extension(int)
                    {
                        public static void Do<T>()
                        {
                            var res = new RefHolder<T>();
                            M().Wait();
                            async Task M()
                            {
                                res.Get() = await Task.FromResult(default(T));
                            }
                        }
                    }
                }
                """;

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (19,17): error CS8178: A reference returned by a call to 'RefHolder<T>.Get()' cannot be preserved across 'await' or 'yield' boundary.
                //                 res.Get() = await Task.FromResult(default(T));
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "res.Get()").WithArguments("RefHolder<T>.Get()").WithLocation(19, 17)
            );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/76999")]
        public void TestAwaitHoistedRef2()
        {
            var src = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Threading.Tasks;

                public sealed class ValuesHolder<T>
                {
                	private readonly T[] _values = new T[10];

                	public ref T this[int type] => ref _values[type];
                }

                public static class App
                {
                	public static async Task<ValuesHolder<TResult>> Do<TResult>()
                	{
                		var res = new ValuesHolder<TResult>();

                		var taskGroup = new List<KeyValuePair<int, Task<TResult>>>();

                		await Task.WhenAll(taskGroup.Select(async kv =>
                		{
                			res[0] = await kv.Value;
                		}));

                		return res;
                	}
                }
                """;

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (23,4): error CS8178: A reference returned by a call to 'ValuesHolder<TResult>.this[int].get' cannot be preserved across 'await' or 'yield' boundary.
                // 			res[0] = await kv.Value;
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "res[0]").WithArguments("ValuesHolder<TResult>.this[int].get").WithLocation(23, 4)
            );
        }

        [Fact]
        [WorkItem(1084696, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084696")]
        public void TestAwaitInfo2()
        {
            var text =
@"using System;
using System.Threading.Tasks;
public class C {
    public C(Task<int> t) {
        Func<Task> f = async() => await t;
    }
}";
            var info = GetAwaitExpressionInfo(text);
            Assert.Equal("System.Runtime.CompilerServices.TaskAwaiter<System.Int32> System.Threading.Tasks.Task<System.Int32>.GetAwaiter()", info.GetAwaiterMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Runtime.CompilerServices.TaskAwaiter<System.Int32>.GetResult()", info.GetResultMethod.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Runtime.CompilerServices.TaskAwaiter<System.Int32>.IsCompleted { get; }", info.IsCompletedProperty.ToTestDisplayString());
            Assert.Null(info.RuntimeAwaitMethod);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/79818")]
        public void TestAwaitInfo3()
        {
            var text =
                """
                using System;
                using System.Threading.Tasks;
                public class C {
                    public C(Task<int> t) {
                        Func<Task> f = async() => await t;
                    }
                }
                """;
            var comp = CreateRuntimeAsyncCompilation(text);
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var syntaxNode = (AwaitExpressionSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.AwaitExpression).AsNode();
            var treeModel = comp.GetSemanticModel(tree);
            var info = treeModel.GetAwaitExpressionInfo(syntaxNode);
            Assert.Null(info.GetAwaiterMethod);
            Assert.Null(info.GetResultMethod);
            Assert.Null(info.IsCompletedProperty);
            AssertEx.Equal("System.Int32 System.Runtime.CompilerServices.AsyncHelpers.Await<System.Int32>(System.Threading.Tasks.Task<System.Int32> task)", info.RuntimeAwaitMethod.ToTestDisplayString());
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/79818")]
        public void TestAwaitInfo4()
        {
            var text =
                """
                using System;
                using System.Threading.Tasks;
                public class C {
                    public C() {
                        Func<Task> f = async() => await Task.Yield();
                    }
                }
                """;
            var comp = CreateRuntimeAsyncCompilation(text);
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var syntaxNode = (AwaitExpressionSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.AwaitExpression).AsNode();
            var treeModel = comp.GetSemanticModel(tree);
            var info = treeModel.GetAwaitExpressionInfo(syntaxNode);
            AssertEx.Equal("System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()", info.GetAwaiterMethod.ToTestDisplayString());
            AssertEx.Equal("void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()", info.GetResultMethod.ToTestDisplayString());
            AssertEx.Equal("System.Boolean System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted { get; }", info.IsCompletedProperty.ToTestDisplayString());
            AssertEx.Equal(
                "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter awaiter)",
                info.RuntimeAwaitMethod.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(744146, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/744146")]
        public void DefaultAwaitExpressionInfo()
        {
            AwaitExpressionInfo info = default;
            Assert.Null(info.GetAwaiterMethod);
            Assert.Null(info.GetResultMethod);
            Assert.Null(info.IsCompletedProperty);
            Assert.Null(info.RuntimeAwaitMethod);
            Assert.False(info.IsDynamic);
            Assert.Equal(0, info.GetHashCode());
        }

        private AwaitExpressionInfo GetAwaitExpressionInfo(string text, out CSharpCompilation compilation, params DiagnosticDescription[] diagnostics)
        {
            var tree = Parse(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            var comp = CreateCompilationWithMscorlib461(new SyntaxTree[] { tree }, new MetadataReference[] { SystemRef });
            comp.VerifyDiagnostics(diagnostics);
            compilation = comp;
            var syntaxNode = (AwaitExpressionSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.AwaitExpression).AsNode();
            var treeModel = comp.GetSemanticModel(tree);
            return treeModel.GetAwaitExpressionInfo(syntaxNode);
        }

        private AwaitExpressionInfo GetAwaitExpressionInfo(string text, params DiagnosticDescription[] diagnostics)
        {
            CSharpCompilation temp;
            return GetAwaitExpressionInfo(text, out temp, diagnostics);
        }

        [Fact]
        [WorkItem(748533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/748533")]
        public void Bug748533()
        {
            var text =
@"
using System;
using System.Threading;
using System.Threading.Tasks;
class A
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(10);
        return t;
    }
    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        tests++;
        dynamic f = (await GetVal((Func<Task<int>>)(async () => 1)))();
        if (await f == 1)
            Driver.Count++;
        tests++;
        dynamic ff = new Func<Task<int>>((Func<Task<int>>)(async () => 1));
        if (await ff() == 1)
            Driver.Count++;
        Driver.Result = Driver.Count - tests;
        Driver.CompletedSignal.Set();
    }
}
class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static int Main()
    {
        var t = new A();
        t.Run(6);
        CompletedSignal.WaitOne();
        return Driver.Result;
    }
}
";
            var comp = CreateCompilationWithMscorlib461(text, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (17,13): error CS0656: Missing compiler required member 'Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create'
                //         if (await f == 1)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "await f").WithArguments("Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo", "Create"));
        }

        [Fact]
        [WorkItem(576316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576316")]
        public void Bug576316()
        {
            var text =
@"using System;
using System.Threading.Tasks;
 
class C
{
    static async Task Goo()
    {
        Console.WriteLine(new TypedReference().Equals(await Task.FromResult(0)));
    }
}";
            var comp = CreateCompilationWithMscorlib461(text, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (8,27): error CS4007: Instance of type 'System.TypedReference' cannot be preserved across 'await' or 'yield' boundary.
                //         Console.WriteLine(new TypedReference().Equals(await Task.FromResult(0)));
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "new TypedReference()").WithArguments("System.TypedReference").WithLocation(8, 27));
        }

        [Fact]
        [WorkItem(3951, "https://github.com/dotnet/roslyn/issues/3951")]
        public void TestAwaitInNonAsync()
        {
            var text =
@"using System.Threading.Tasks;

class C
{
    void Goo(Task<int> t)
    {
        var v = await t;
    }
}";
            CSharpCompilation compilation;
            var info = GetAwaitExpressionInfo(text, out compilation,
                // (7,21): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         int c = 1 + await t;
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await t").WithLocation(7, 17)
                );
            Assert.Equal("System.Runtime.CompilerServices.TaskAwaiter<System.Int32> System.Threading.Tasks.Task<System.Int32>.GetAwaiter()", info.GetAwaiterMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Runtime.CompilerServices.TaskAwaiter<System.Int32>.GetResult()", info.GetResultMethod.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Runtime.CompilerServices.TaskAwaiter<System.Int32>.IsCompleted { get; }", info.IsCompletedProperty.ToTestDisplayString());
            var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
            var decl = compilation.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().AsSingleton();
            var symbolV = (ILocalSymbol)semanticModel.GetDeclaredSymbol(decl);
            Assert.Equal("System.Int32", symbolV.Type.ToTestDisplayString());
        }

        [Fact]
        public void Dynamic()
        {
            string source =
@"using System.Threading.Tasks;
class Program
{
    static async Task Main()
    {
        dynamic d = Task.CompletedTask;
        await d;
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = (AwaitExpressionSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.AwaitExpression).AsNode();
            var info = model.GetAwaitExpressionInfo(expr);
            Assert.True(info.IsDynamic);
            Assert.Null(info.GetAwaiterMethod);
            Assert.Null(info.IsCompletedProperty);
            Assert.Null(info.GetResultMethod);
        }

        [Fact]
        [WorkItem(52639, "https://github.com/dotnet/roslyn/issues/52639")]
        public void Issue52639_1()
        {
            var text =
@"
using System;
using System.Threading.Tasks;

class Test1
{
    public async Task<ActionResult> Test(MyBaseClass model)
    {
        switch (model)
        {
            case FirstImplementation firstImplementation:
                firstImplementation.MyString1 = await Task.FromResult(""test"");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(model));
        }

        switch (model)
        {
            case FirstImplementation firstImplementation:
                await Task.FromResult(1);
                return PartialView(""View"", firstImplementation);
            default:
                throw new ArgumentOutOfRangeException(nameof(model));
        }
    }

    private ActionResult PartialView(string v, FirstImplementation firstImplementation)
    {
        return new ActionResult { F = firstImplementation };
    }

    static void Main()
    {
        var c = new Test1();
        var f = new FirstImplementation();

        if (c.Test(f).Result.F == f && f.MyString1 == ""test"")
        {
            System.Console.WriteLine(""Passed"");
        }
        else
        {
            System.Console.WriteLine(""Failed"");
        }
    }
}

internal class ActionResult
{
    public FirstImplementation F;
}

public abstract class MyBaseClass
{
    public string MyString { get; set; }
}

public class FirstImplementation : MyBaseClass
{
    public string MyString1 { get; set; }
}

public class SecondImplementation : MyBaseClass
{
    public string MyString2 { get; set; }
}
";
            CompileAndVerify(text, options: TestOptions.ReleaseExe, expectedOutput: "Passed").VerifyDiagnostics();
            CompileAndVerify(text, options: TestOptions.DebugExe, expectedOutput: "Passed").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(52639, "https://github.com/dotnet/roslyn/issues/52639")]
        public void Issue52639_2()
        {
            var text =
@"
using System.Threading.Tasks;

class C
{
    string F;

    async Task<C> Test(C c)
    {
        c.F = await Task.FromResult(""a"");

        switch (c)
        {
            case C c1:
                await Task.FromResult(1);
                return c1;
        }

        return null;
    }

    static void Main()
    {
        var c = new C();
        if (c.Test(c).Result == c && c.F == ""a"")
        {
            System.Console.WriteLine(""Passed"");
        }
        else
        {
            System.Console.WriteLine(""Failed"");
        }
    }
}
";
            CompileAndVerify(text, options: TestOptions.ReleaseExe, expectedOutput: "Passed").VerifyDiagnostics();
            CompileAndVerify(text, options: TestOptions.DebugExe, expectedOutput: "Passed").VerifyDiagnostics();
        }

        [Fact]
        public void TestAwaitUsingDeclarationAwaitInfo()
        {
            var text = @"
using System;
using System.Threading.Tasks;

class C : IAsyncDisposable
{
    async Task M()
    {
        await using var x = new C();
    }

    public ValueTask DisposeAsync() => default;
}";

            var comp = CreateCompilation(text, targetFramework: TargetFramework.NetCoreApp);
            validateComp(comp, isRuntimeAsync: false);

            comp = CreateRuntimeAsyncCompilation(text);
            validateComp(comp, isRuntimeAsync: true);

            static void validateComp(CSharpCompilation comp, bool isRuntimeAsync)
            {
                comp.VerifyDiagnostics();
                var tree = comp.SyntaxTrees.Single();
                var model = comp.GetSemanticModel(tree);
                var awaitUsingDeclaration = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();

                var info = model.GetAwaitExpressionInfo(awaitUsingDeclaration);
                if (isRuntimeAsync)
                {
                    Assert.Null(info.GetAwaiterMethod);
                    Assert.Null(info.IsCompletedProperty);
                    Assert.Null(info.GetResultMethod);
                    AssertEx.Equal("void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask task)", info.RuntimeAwaitMethod.ToTestDisplayString());
                }
                else
                {
                    AssertEx.Equal("System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()", info.GetAwaiterMethod.ToTestDisplayString());
                    Assert.NotNull(info.IsCompletedProperty);
                    Assert.NotNull(info.GetResultMethod);
                    Assert.Null(info.RuntimeAwaitMethod);
                }
                Assert.False(info.IsDynamic);
            }
        }

        [Fact]
        public void TestAwaitUsingStatementAwaitInfo()
        {
            var text = @"
using System;
using System.Threading.Tasks;

class C : IAsyncDisposable
{
    async Task M()
    {
        await using (var x = new C())
        {
        }
    }

    public ValueTask DisposeAsync() => default;
}";

            var comp = CreateCompilation(text, targetFramework: TargetFramework.NetCoreApp);
            validateComp(comp, isRuntimeAsync: false);

            comp = CreateRuntimeAsyncCompilation(text);
            validateComp(comp, isRuntimeAsync: true);

            static void validateComp(CSharpCompilation comp, bool isRuntimeAsync)
            {
                comp.VerifyDiagnostics();
                var tree = comp.SyntaxTrees.Single();
                var model = comp.GetSemanticModel(tree);
                var awaitUsingStatement = tree.GetRoot().DescendantNodes().OfType<UsingStatementSyntax>().Single();

                var info = model.GetAwaitExpressionInfo(awaitUsingStatement);
                if (isRuntimeAsync)
                {
                    Assert.Null(info.GetAwaiterMethod);
                    Assert.Null(info.IsCompletedProperty);
                    Assert.Null(info.GetResultMethod);
                    AssertEx.Equal("void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask task)", info.RuntimeAwaitMethod.ToTestDisplayString());
                }
                else
                {
                    AssertEx.Equal("System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()", info.GetAwaiterMethod.ToTestDisplayString());
                    Assert.NotNull(info.IsCompletedProperty);
                    Assert.NotNull(info.GetResultMethod);
                    Assert.Null(info.RuntimeAwaitMethod);
                }
                Assert.False(info.IsDynamic);
            }
        }

        [Fact]
        public void TestAwaitUsingDeclarationAwaitInfo_ThrowsOnNonAwaitUsing()
        {
            var text = @"
using System;

class C : IDisposable
{
    void M()
    {
        using var x = new C();
    }

    public void Dispose() { }
}";

            var comp = CreateCompilation(text);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var usingDeclaration = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
            Assert.Throws<ArgumentException>("node", () => model.GetAwaitExpressionInfo(usingDeclaration));
        }

        [Fact]
        public void TestAwaitUsingStatementAwaitInfo_ReturnsDefaultOnNonAwaitUsing()
        {
            var text = @"
using System;

class C : IDisposable
{
    void M()
    {
        using (var x = new C())
        {
        }
    }

    public void Dispose() { }
}";

            var comp = CreateCompilation(text);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var usingStatement = tree.GetRoot().DescendantNodes().OfType<UsingStatementSyntax>().Single();
            Assert.Throws<ArgumentException>("node", () => model.GetAwaitExpressionInfo(usingStatement));
        }

        [Fact]
        public void TestAwaitUsingDeclarationWithCustomAwaitable()
        {
            var text = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public struct MyValueTask
{
    public MyAwaiter GetAwaiter() => default;
}

public struct MyAwaiter : ICriticalNotifyCompletion
{
    public bool IsCompleted => true;
    public void GetResult() { }
    public void OnCompleted(Action continuation) { }
    public void UnsafeOnCompleted(Action continuation) { }
}

class C : IAsyncDisposable
{
    async Task M()
    {
        await using var x = new C();
    }

    public MyValueTask DisposeAsync() => default;
}";

            var comp = CreateCompilation(text, targetFramework: TargetFramework.NetCoreApp);
            validate(comp, isRuntimeAsync: false);

            comp = CreateRuntimeAsyncCompilation(text);
            validate(comp, isRuntimeAsync: true);

            static void validate(CSharpCompilation comp, bool isRuntimeAsync)
            {
                var tree = comp.SyntaxTrees.Single();
                var model = comp.GetSemanticModel(tree);

                var awaitUsingDeclaration = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();

                var info = model.GetAwaitExpressionInfo(awaitUsingDeclaration);
                Assert.NotNull(info.GetAwaiterMethod);
                Assert.Equal("MyAwaiter MyValueTask.GetAwaiter()", info.GetAwaiterMethod.ToTestDisplayString());
                Assert.NotNull(info.IsCompletedProperty);
                Assert.Equal("System.Boolean MyAwaiter.IsCompleted { get; }", info.IsCompletedProperty.ToTestDisplayString());
                Assert.NotNull(info.GetResultMethod);
                Assert.Equal("void MyAwaiter.GetResult()", info.GetResultMethod.ToTestDisplayString());
                Assert.False(info.IsDynamic);

                if (isRuntimeAsync)
                {
                    AssertEx.Equal("void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<MyAwaiter>(MyAwaiter awaiter)", info.RuntimeAwaitMethod.ToTestDisplayString());
                }
                else
                {
                    Assert.Null(info.RuntimeAwaitMethod);
                }
            }
        }

        [Fact]
        public void TestAwaitUsingStatementWithCustomAwaitable()
        {
            var text = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public struct MyValueTask
{
    public MyAwaiter GetAwaiter() => default;
}

public struct MyAwaiter : ICriticalNotifyCompletion
{
    public bool IsCompleted => true;
    public void GetResult() { }
    public void OnCompleted(Action continuation) { }
    public void UnsafeOnCompleted(Action continuation) { }
}

class C : IAsyncDisposable
{
    async Task M()
    {
        await using (var x = new C()) { }
    }

    public MyValueTask DisposeAsync() => default;
}";

            var comp = CreateCompilation(text, targetFramework: TargetFramework.NetCoreApp);
            validate(comp, isRuntimeAsync: false);

            comp = CreateRuntimeAsyncCompilation(text);
            validate(comp, isRuntimeAsync: true);

            static void validate(CSharpCompilation comp, bool isRuntimeAsync)
            {
                var tree = comp.SyntaxTrees.Single();
                var model = comp.GetSemanticModel(tree);

                var awaitUsingDeclaration = tree.GetRoot().DescendantNodes().OfType<UsingStatementSyntax>().Single();

                var info = model.GetAwaitExpressionInfo(awaitUsingDeclaration);
                Assert.NotNull(info.GetAwaiterMethod);
                Assert.Equal("MyAwaiter MyValueTask.GetAwaiter()", info.GetAwaiterMethod.ToTestDisplayString());
                Assert.NotNull(info.IsCompletedProperty);
                Assert.Equal("System.Boolean MyAwaiter.IsCompleted { get; }", info.IsCompletedProperty.ToTestDisplayString());
                Assert.NotNull(info.GetResultMethod);
                Assert.Equal("void MyAwaiter.GetResult()", info.GetResultMethod.ToTestDisplayString());
                Assert.False(info.IsDynamic);

                if (isRuntimeAsync)
                {
                    AssertEx.Equal("void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<MyAwaiter>(MyAwaiter awaiter)", info.RuntimeAwaitMethod.ToTestDisplayString());
                }
                else
                {
                    Assert.Null(info.RuntimeAwaitMethod);
                }
            }
        }

        [Fact]
        public void SpeculativeSemanticModel_GetAwaitExpressionInfo_LocalDeclarationStatement()
        {
            var text = """
                using System.Threading.Tasks;
                class C : System.IAsyncDisposable
                {
                    async Task Goo()
                    {
                        await using var x = new C();
                    }
                    public ValueTask DisposeAsync() => default;
                }
                """;
            var comp = CreateCompilation(text, targetFramework: TargetFramework.NetCoreApp);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var actualLocalDecl = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().First();
            var speculativeLocalDecl = SyntaxFactory.ParseStatement("await using var y = new C();");

            var success = model.TryGetSpeculativeSemanticModel(actualLocalDecl.SpanStart, speculativeLocalDecl, out var specModel);
            Assert.True(success);
            Assert.NotNull(specModel);

            var speculativeInfo = specModel.GetAwaitExpressionInfo((LocalDeclarationStatementSyntax)speculativeLocalDecl);
            AssertEx.Equal("System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()", speculativeInfo.GetAwaiterMethod.ToTestDisplayString());
        }

        [Fact]
        public void SpeculativeSemanticModel_GetAwaitExpressionInfo_UsingStatementSyntax()
        {
            var text = """
                using System.Threading.Tasks;
                class C : System.IAsyncDisposable
                {
                    async Task Goo()
                    {
                        await using (var x = new C()) { }
                    }
                    public ValueTask DisposeAsync() => default;
                }
                """;
            var comp = CreateCompilation(text, targetFramework: TargetFramework.NetCoreApp);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var actualUsingStmt = tree.GetRoot().DescendantNodes().OfType<UsingStatementSyntax>().First();
            var speculativeUsingStmt = SyntaxFactory.ParseStatement("await using (var y = new C()) { }");

            var success = model.TryGetSpeculativeSemanticModel(actualUsingStmt.SpanStart, speculativeUsingStmt, out var specModel);
            Assert.True(success);
            Assert.NotNull(specModel);

            var speculativeInfo = specModel.GetAwaitExpressionInfo((UsingStatementSyntax)speculativeUsingStmt);
            AssertEx.Equal("System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()", speculativeInfo.GetAwaiterMethod.ToTestDisplayString());
        }
    }
}
