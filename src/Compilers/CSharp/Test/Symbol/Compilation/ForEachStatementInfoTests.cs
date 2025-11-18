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

namespace Microsoft.CodeAnalysis.CSharp
{
    public class ForEachStatementInfoTests : CSharpTestBase
    {
        [Fact]
        public void Equality()
        {
            var c = (Compilation)CreateCompilation(@"
class E1
{
    public E GetEnumerator() { return null; }
    public bool MoveNext() { return false; }
    public object Current { get; }
    public void Dispose() { }
}

class E2
{
    public E GetEnumerator() { return null; }
    public bool MoveNext() { return false; }
    public object Current { get; }
    public void Dispose() { }
}
");
            var e1 = (ITypeSymbol)c.GlobalNamespace.GetMembers("E1").Single();
            var ge1 = (IMethodSymbol)e1.GetMembers("GetEnumerator").Single();
            var mn1 = (IMethodSymbol)e1.GetMembers("MoveNext").Single();
            var cur1 = (IPropertySymbol)e1.GetMembers("Current").Single();
            var disp1 = (IMethodSymbol)e1.GetMembers("Dispose").Single();
            var conv1 = Conversion.Identity;
            var mnea1 = default(AwaitExpressionInfo);
            var dispa1 = default(AwaitExpressionInfo);

            var e2 = (ITypeSymbol)c.GlobalNamespace.GetMembers("E2").Single();
            var ge2 = (IMethodSymbol)e2.GetMembers("GetEnumerator").Single();
            var mn2 = (IMethodSymbol)e2.GetMembers("MoveNext").Single();
            var cur2 = (IPropertySymbol)e2.GetMembers("Current").Single();
            var disp2 = (IMethodSymbol)e2.GetMembers("Dispose").Single();
            var conv2 = Conversion.NoConversion;
            var mnea2 = new AwaitExpressionInfo(mn2, null, null, null, false);
            var dispa2 = new AwaitExpressionInfo(disp2, null, null, null, false);

            EqualityTesting.AssertEqual(default(ForEachStatementInfo), default(ForEachStatementInfo));
            EqualityTesting.AssertEqual(new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge2, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, mn2, mnea1, cur1, disp1, dispa1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea2, cur1, disp1, dispa1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur2, disp1, dispa1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp2, dispa1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa2, e1, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e2, conv1, conv1), new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv2, conv1), new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv2), new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1));
            EqualityTesting.AssertNotEqual(new ForEachStatementInfo(isAsync: true, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1), new ForEachStatementInfo(isAsync: false, ge1, mn1, mnea1, cur1, disp1, dispa1, e1, conv1, conv1));
        }

        [Fact]
        public void TestAwaitForeachAwaitableInfo()
        {
            var text = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class C
{
    async Task M()
    {
        await foreach (var x in new AsyncEnumerable())
        {
        }
    }
}

class AsyncEnumerable : IAsyncEnumerable<int>
{
    public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new AsyncEnumerator();
}

class AsyncEnumerator : IAsyncEnumerator<int>
{
    public int Current => 42;
    
    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(false);
    
    public ValueTask DisposeAsync() => default;
}";

            var comp = CreateCompilationWithTasksExtensions(new[] { text, s_IAsyncEnumerable });
            validate(comp, isRuntimeAsync: false);

            comp = CreateRuntimeAsyncCompilation(text);
            validate(comp, isRuntimeAsync: true);

            static void validate(CSharpCompilation comp, bool isRuntimeAsync)
            {
                comp.VerifyDiagnostics();
                var tree = comp.SyntaxTrees.First();
                var model = comp.GetSemanticModel(tree);

                var awaitForeachStatement = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

                var info = model.GetForEachStatementInfo(awaitForeachStatement);
                Assert.True(info.IsAsynchronous);

                // Verify MoveNextAwaitableInfo
                var moveNextAwaitInfo = info.MoveNextAwaitableInfo;
                if (isRuntimeAsync)
                {
                    Assert.Null(moveNextAwaitInfo.GetAwaiterMethod);
                    Assert.Null(moveNextAwaitInfo.IsCompletedProperty);
                    Assert.Null(moveNextAwaitInfo.GetResultMethod);
                    AssertEx.Equal("System.Boolean System.Runtime.CompilerServices.AsyncHelpers.Await<System.Boolean>(System.Threading.Tasks.ValueTask<System.Boolean> task)",
                        moveNextAwaitInfo.RuntimeAwaitMethod.ToTestDisplayString());
                }
                else
                {
                    AssertEx.Equal("System.Runtime.CompilerServices.ValueTaskAwaiter<System.Boolean> System.Threading.Tasks.ValueTask<System.Boolean>.GetAwaiter()",
                        moveNextAwaitInfo.GetAwaiterMethod.ToTestDisplayString());
                    Assert.NotNull(moveNextAwaitInfo.IsCompletedProperty);
                    Assert.NotNull(moveNextAwaitInfo.GetResultMethod);
                    Assert.Null(moveNextAwaitInfo.RuntimeAwaitMethod);
                }
                Assert.False(moveNextAwaitInfo.IsDynamic);

                // Verify DisposeAwaitableInfo  
                var disposeAwaitInfo = info.DisposeAwaitableInfo;
                if (isRuntimeAsync)
                {
                    Assert.Null(disposeAwaitInfo.GetAwaiterMethod);
                    Assert.Null(disposeAwaitInfo.IsCompletedProperty);
                    Assert.Null(disposeAwaitInfo.GetResultMethod);
                    AssertEx.Equal("void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask task)",
                        disposeAwaitInfo.RuntimeAwaitMethod.ToTestDisplayString());
                }
                else
                {
                    AssertEx.Equal("System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()",
                        disposeAwaitInfo.GetAwaiterMethod.ToTestDisplayString());
                    Assert.NotNull(disposeAwaitInfo.IsCompletedProperty);
                    Assert.NotNull(disposeAwaitInfo.GetResultMethod);
                    Assert.Null(disposeAwaitInfo.RuntimeAwaitMethod);
                }
                Assert.False(disposeAwaitInfo.IsDynamic);
            }
        }

        [Fact]
        public void TestSynchronousForeachAwaitableInfo()
        {
            var text = @"
using System.Collections.Generic;

class C
{
    void M()
    {
        foreach (var x in new List<int>())
        {
        }
    }
}";

            var comp = CreateCompilation(text);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var foreachStatement = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachStatement);

            Assert.False(info.IsAsynchronous);

            var moveNextAwaitInfo = info.MoveNextAwaitableInfo;
            Assert.Null(moveNextAwaitInfo.GetAwaiterMethod);
            Assert.Null(moveNextAwaitInfo.IsCompletedProperty);
            Assert.Null(moveNextAwaitInfo.GetResultMethod);
            Assert.False(moveNextAwaitInfo.IsDynamic);

            var disposeAwaitInfo = info.DisposeAwaitableInfo;
            Assert.Null(disposeAwaitInfo.GetAwaiterMethod);
            Assert.Null(disposeAwaitInfo.IsCompletedProperty);
            Assert.Null(disposeAwaitInfo.GetResultMethod);
            Assert.False(disposeAwaitInfo.IsDynamic);
        }
    }
}
