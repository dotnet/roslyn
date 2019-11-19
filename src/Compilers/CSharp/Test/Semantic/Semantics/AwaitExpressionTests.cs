// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) await expressions.
    /// </summary>
    public class AwaitExpressionTests : CompilingTestBase
    {
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
        }

        [Fact]
        [WorkItem(744146, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/744146")]
        public void DefaultAwaitExpressionInfo()
        {
            AwaitExpressionInfo info = default;
            Assert.Null(info.GetAwaiterMethod);
            Assert.Null(info.GetResultMethod);
            Assert.Null(info.IsCompletedProperty);
            Assert.False(info.IsDynamic);
            Assert.Equal(0, info.GetHashCode());
        }

        private AwaitExpressionInfo GetAwaitExpressionInfo(string text, out CSharpCompilation compilation, params DiagnosticDescription[] diagnostics)
        {
            var tree = Parse(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            var comp = CreateCompilationWithMscorlib45(new SyntaxTree[] { tree }, new MetadataReference[] { SystemRef });
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
            var comp = CreateCompilationWithMscorlib45(text, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (16,62): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         dynamic f = (await GetVal((Func<Task<int>>)(async () => 1)))();
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(16, 62),
                // (20,69): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         dynamic ff = new Func<Task<int>>((Func<Task<int>>)(async () => 1));
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(20, 69),
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
            var comp = CreateCompilationWithMscorlib45(text, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (8,27): error CS4007: 'await' cannot be used in an expression containing the type 'System.TypedReference'
                //         Console.WriteLine(new TypedReference().Equals(await Task.FromResult(0)));
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "await Task.FromResult(0)").WithArguments("System.TypedReference").WithLocation(8, 55));
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
    }
}
