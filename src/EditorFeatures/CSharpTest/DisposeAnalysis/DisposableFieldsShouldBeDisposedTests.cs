// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DisposeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using static Roslyn.Test.Utilities.TestHelpers;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DisposeAnalysis
{
    [Trait(Traits.Feature, Traits.Features.DisposeAnalysis)]
    public sealed class DisposableFieldsShouldBeDisposedTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new DisposableFieldsShouldBeDisposedDiagnosticAnalyzer(isEnabledByDefault: true), null);

        private Task TestDiagnosticsAsync(string initialMarkup, params DiagnosticDescription[] expectedDiagnostics)
            => TestDiagnosticsAsync(initialMarkup, parseOptions: null, expectedDiagnostics);
        private Task TestDiagnosticsAsync(string initialMarkup, CSharpParseOptions parseOptions, params DiagnosticDescription[] expectedDiagnostics)
            => TestDiagnosticsAsync(initialMarkup, new TestParameters(parseOptions, retainNonFixableDiagnostics: true), expectedDiagnostics);
        private Task TestDiagnosticMissingAsync(string initialMarkup, CSharpParseOptions parseOptions = null)
            => TestDiagnosticMissingAsync(initialMarkup, new TestParameters(parseOptions, retainNonFixableDiagnostics: true));

        [Fact]
        public async Task DisposableAllocationInConstructor_AssignedDirectly_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private readonly A a;|]
    public B()
    {
        a = new A();
    }

    public void Dispose()
    {
        a.Dispose();
    }
}
");
        }

        [Fact]
        public async Task DisposableAllocationInConstructor_AssignedDirectly_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    private readonly A [|a|];
    public B()
    {
        a = new A();
    }

    public void Dispose()
    {
    }
}
",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId));
        }

        [Fact]
        public async Task DisposableAllocationInMethod_AssignedDirectly_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a;|]
    public void SomeMethod()
    {
        a = new A();
    }

    public void Dispose()
    {
        a.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocationInMethod_AssignedDirectly_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    private A [|a|];
    public void SomeMethod()
    {
        a = new A();
    }

    public void Dispose()
    {
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId));
        }

        [Fact]
        public async Task DisposableAllocationInFieldInitializer_AssignedDirectly_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();
    private readonly A a2 = new A();|]

    public void Dispose()
    {
        a.Dispose();
        a2.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocationInFieldInitializer_AssignedDirectly_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();
    private readonly A a2 = new A();|]

    public void Dispose()
    {
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId, "a").WithLocation(13, 15),
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId, "a2").WithLocation(14, 24));
        }

        [Fact]
        public async Task StaticField_NotDisposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private static A a = new A();
    private static readonly A a2 = new A();|]

    public void Dispose()
    {
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_AssignedThroughLocal_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a;|]
    public void SomeMethod()
    {
        var l = new A();
        a = l;
    }

    public void Dispose()
    {
        a.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_AssignedThroughLocal_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    private A [|a|];
    public void SomeMethod()
    {
        var l = new A();
        a = l;
    }

    public void Dispose()
    {
    }
}",
           Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId));
        }

        [Fact]
        public async Task DisposableAllocation_AssignedThroughParameter_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a;|]
    public B(A p)
    {
        p = new A();
        a = p;
    }

    public void Dispose()
    {
        a.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_AssignedThroughParameter_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    private A [|a|];
    public B(A p)
    {
        p = new A();
        a = p;
    }

    public void Dispose()
    {
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId));
        }

        [Fact]
        public async Task DisposableSymbolWithoutAllocation_AssignedThroughParameter_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a;|]
    public B(A p)
    {
        a = p;
    }

    public void Dispose()
    {
        a.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableSymbolWithoutAllocation_AssignedThroughParameter_NotDisposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a;|]
    public B(A p)
    {
        a = p;
    }

    public void Dispose()
    {
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_AssignedThroughInstanceInvocation_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a;|]
    public B()
    {
        a = GetA();
    }

    private A GetA() => new A();

    public void Dispose()
    {
        a.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_AssignedThroughInstanceInvocation_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    private A [|a|];
    public B()
    {
        a = GetA();
    }

    private A GetA() => new A();

    public void Dispose()
    {
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId));
        }

        [Fact]
        public async Task DisposableAllocation_AssignedThroughStaticCreateInvocation_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a;|]
    public B()
    {
        a = Create();
    }

    private static A Create() => new A();

    public void Dispose()
    {
        a.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_AssignedThroughStaticCreateInvocation_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    private A [|a|];
    public B()
    {
        a = Create();
    }

    private static A Create() => new A();

    public void Dispose()
    {
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId));
        }

        [Fact]
        public async Task DisposableAllocation_AssignedInDifferentType_DisposedInContainingType_NoDiagnostic()
        {
            // We don't track disposable field assignments in different type.
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|public A a;|]
    public void Dispose()
    {
        a.Dispose();
    }
}

class WrapperB
{
    private B b;
    public void Create()
    {
        b.a = new A();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_AssignedInDifferentType_DisposedInDifferentNonDisposableType_NoDiagnostic()
        {
            // We don't track disposable field assignments in different type.
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|public A a;|]
    public void Dispose()
    {
    }
}

class WrapperB
{
    private B b;

    public void Create()
    {
        b.a = new A();
    }

    public void Dispose()
    {
        b.a.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_AssignedInDifferentType_NotDisposed_NoDiagnostic()
        {
            // We don't track disposable field assignments in different type.
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|public A a;|]
    public void Dispose()
    {
    }
}

class Test
{
    public void M(B b)
    {
        b.a = new A();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_DisposedWithConditionalAccess_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();|]

    public void Dispose()
    {
        a?.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_AssignedToLocal_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();|]

    public void Dispose()
    {
        A l = a;
        l.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_AssignedToLocal_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    private A [|a|] = new A();

    public void Dispose()
    {
        A l = a;
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId));
        }

        [Fact]
        public async Task DisposableAllocation_IfElseStatement_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a;
    private A b;|]

    public B(bool flag)
    {
        A l = new A();
        if (flag)
        {
            a = l;
        }
        else
        {
            b = l;
        }
    }

    public void Dispose()
    {
        A l = null;
        if (a != null)
        {
            l = a;
        }
        else if (b != null)
        {
            l = b;
        }

        l.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_IfElseStatement_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a;
    private A b;|]

    public B(bool flag)
    {
        A l = new A();
        if (flag)
        {
            a = l;
        }
        else
        {
            b = l;
        }
    }

    public void Dispose()
    {
        A l = null;
        if (a != null)
        {
            l = a;
        }
        else if (b != null)
        {
            l = b;
        }
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId, "a").WithLocation(13, 15),
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId, "b").WithLocation(14, 15));
        }

        [Fact]
        public async Task DisposableAllocation_EscapedField_NotDisposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();|]

    public void Dispose()
    {
        DisposeA(ref this.a);
    }

    private static void DisposeA(ref A a)
    {
        a.Dispose();
        a = null;
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_OptimisticPointsToAnalysis_NoDiagnostic()
        {
            // Invoking an instance method may likely invalidate all the instance field analysis state, i.e.
            // reference type fields might be re-assigned to point to different objects in the called method.
            // An optimistic points to analysis assumes that the points to values of instance fields don't change on invoking an instance method.
            // A pessimistic points to analysis resets all the instance state and assumes the instance field might point to any object, hence has unknown state.
            // For dispose analysis, we want to perform an optimistic points to analysis as we assume a disposable field is not likely to be re-assigned to a separate object in helper method invocations in Dispose.

            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
    public void PerformSomeCleanup()
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();|]

    public void Dispose()
    {
        a.PerformSomeCleanup();
        ClearMyState();
        a.Dispose();
    }

    private void ClearMyState()
    {
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_OptimisticPointsToAnalysis_WithReturn_NoDiagnostic()
        {
            // Invoking an instance method may likely invalidate all the instance field analysis state, i.e.
            // reference type fields might be re-assigned to point to different objects in the called method.
            // An optimistic points to analysis assumes that the points to values of instance fields don't change on invoking an instance method.
            // A pessimistic points to analysis resets all the instance state and assumes the instance field might point to any object, hence has unknown state.
            // For dispose analysis, we want to perform an optimistic points to analysis as we assume a disposable field is not likely to be re-assigned to a separate object in helper method invocations in Dispose.

            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
    public void PerformSomeCleanup()
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();|]
    public bool Disposed;

    public void Dispose()
    {
        if (Disposed)
        {
            return;
        }

        a.PerformSomeCleanup();
        ClearMyState();
        a.Dispose();
    }

    private void ClearMyState()
    {
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_IfStatementInDispose_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class Test : IDisposable
{
    [|private readonly A a = new A();|]
    private bool cancelled;

    public void Dispose()
    {
        if (cancelled)
        {
            a.GetType();
        }

        a.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_DisposedinDisposeOverride_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

abstract class Base : IDisposable
{
    public virtual void Dispose()
    {
    }
}

class Derived : Base
{
    [|private readonly A a = new A();|]
    public override void Dispose()
    {
        base.Dispose();
        a.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_DisposedWithDisposeBoolInvocation_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }

    public void Dispose(bool disposed)
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();|]

    public void Dispose()
    {
        a.Dispose(true);
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_DisposedInsideDisposeBool_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }

    public void Dispose(bool disposed)
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();|]

    public void Dispose()
    {
        Dispose(true);
    }

    public void Dispose(bool disposed)
    {
        a.Dispose(disposed);
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_DisposedWithDisposeCloseInvocation_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }

    public void Close()
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();|]

    public void Dispose()
    {
        a.Close();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_AllDisposedMethodsMixed_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }

    public void Dispose(bool disposed)
    {
    }

    public void Close()
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();
    private A a2 = new A();
    private A a3 = new A();|]

    public void Dispose()
    {
        a.Close();
    }

    public void Dispose(bool disposed)
    {
        a2.Dispose();
    }

    public void Close()
    {
        a3.Dispose(true);
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_DisposedInsideDisposeClose_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }

    public void Close()
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();|]

    public void Dispose()
    {
        Close();
    }

    public void Close()
    {
        a.Close();
    }
}");
        }

        [Fact]
        public async Task SystemThreadingTask_SpecialCase_NotDisposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Threading.Tasks;

public class A: IDisposable
{
    [|private readonly Task t;|]
    public A()
    {
        t = new Task(null);
    }
    public void Dispose()
    {
    }
}");
        }

        [Fact, WorkItem(1796, "https://github.com/dotnet/roslyn-analyzers/issues/1796")]
        public async Task DisposableAllocation_DisposedWithDisposeAsyncInvocation_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Threading.Tasks;

class A : IDisposable
{
    public void Dispose() => DisposeAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}

class B : IDisposable
{
    [|private A a = new A();|]

    public void Dispose()
    {
        a.DisposeAsync();
    }
}");
        }

        [Fact, WorkItem(1796, "https://github.com/dotnet/roslyn-analyzers/issues/1796")]
        public async Task DisposableAllocation_DisposedInsideDisposeCoreAsync_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Threading.Tasks;

abstract class A : IDisposable
{
    public void Dispose() => DisposeAsync();

    public Task DisposeAsync() => DisposeCoreAsync(true);

    protected abstract Task DisposeCoreAsync(bool initialized);
}

class A2 : A
{
    protected override Task DisposeCoreAsync(bool initialized)
    {
        return Task.CompletedTask;
    }
}

class B : A
{
    [|private A2 a = new A2();|]

    protected override Task DisposeCoreAsync(bool initialized)
    {
        return a.DisposeAsync();
    }
}");
        }

        [Fact, WorkItem(1813, "https://github.com/dotnet/roslyn-analyzers/issues/1813")]
        public async Task DisposableAllocation_DisposedInInvokedMethod_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private A a = new A();|]

    public void Dispose()
    {
        DisposeHelper();
    }

    private void DisposeHelper()
    {
        a.Dispose();
    }
}");
        }

        [Fact, WorkItem(1813, "https://github.com/dotnet/roslyn-analyzers/issues/1813")]
        public async Task DisposableAllocation_NotDisposedInInvokedMethod_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    private A [|a|] = new A();

    public void Dispose()
    {
        DisposeHelper();
    }

    private void DisposeHelper()
    {
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId));
        }

        [Fact, WorkItem(1813, "https://github.com/dotnet/roslyn-analyzers/issues/1813")]
        public async Task DisposableAllocation_DisposedInInvokedMethod_DisposableTypeInMetadata_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.IO;

class B : IDisposable
{
    [|private FileStream a = File.Open("""", FileMode.Create);|]

    public void Dispose()
    {
        DisposeHelper();
    }

    private void DisposeHelper()
    {
        a.Dispose();
    }
}");
        }

        [Fact, WorkItem(1813, "https://github.com/dotnet/roslyn-analyzers/issues/1813")]
        public async Task DisposableAllocation_NotDisposedInInvokedMethod_DisposableTypeInMetadata_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;
using System.IO;

class B : IDisposable
{
    private FileStream [|a|] = File.Open("""", FileMode.Create);

    public void Dispose()
    {
        DisposeHelper();
    }

    private void DisposeHelper()
    {
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId));
        }

        [Fact, WorkItem(1813, "https://github.com/dotnet/roslyn-analyzers/issues/1813")]
        public async Task DisposableAllocation_DisposedInInvokedMethodMultipleLevelsDown_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.IO;

class B : IDisposable
{
    [|private FileStream a = File.Open("""", FileMode.Create);|]

    public void Dispose()
    {
        DisposeHelper();
    }

    private void DisposeHelper()
    {
        Helper.PerformDispose(a);
    }
}

static class Helper
{
    public static void PerformDispose(IDisposable a)
    {
        a.Dispose();
    }
}");
        }

        [Fact, WorkItem(1813, "https://github.com/dotnet/roslyn-analyzers/issues/1813")]
        public async Task DisposableAllocation_NotDisposedInInvokedMethodMultipleLevelsDown_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;
using System.IO;

class B : IDisposable
{
    private FileStream [|a|] = File.Open("""", FileMode.Create);

    public void Dispose()
    {
        DisposeHelper();
    }

    private void DisposeHelper()
    {
        Helper.PerformDispose(a);
    }
}

static class Helper
{
    public static void PerformDispose(IDisposable a)
    {
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId));
        }

        [Fact, WorkItem(2182, "https://github.com/dotnet/roslyn-analyzers/issues/2182")]
        public async Task DisposableAllocation_NonReadOnlyField_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

public sealed class B : IDisposable
{
    public void Dispose()
    {
    }
}

public sealed class A : IDisposable
{
    [|private B _b;|]

    public A()
    {
        _b = new B();
    }

    public void Dispose()
    {
        if (_b == null)
        {
            return;
        }

        _b.Dispose();
        _b = null;
    }
}");
        }

        [Fact, WorkItem(2306, "https://github.com/dotnet/roslyn-analyzers/issues/2306")]
        public async Task DisposableAllocationInConstructor_DisposedInGeneratedCodeFile_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    [|private readonly A a;|]
    public B()
    {
        a = new A();
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    public void Dispose()
    {
        a.Dispose();
    }
}");
        }

        [Fact]
        public async Task DisposableAllocation_FieldDisposedInOverriddenHelper_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : IDisposable
{
    private readonly object _gate = new object();

    public void Dispose()
    {
        lock (_gate)
        {
            DisposeUnderLock();
        }
    }

    protected virtual void DisposeUnderLock()
    {
    }
}

class C : B
{
    // Ensure this field is not flagged
    [|private readonly A _a = new A();|]

    protected override void DisposeUnderLock()
    {
        _a.Dispose();
        base.DisposeUnderLock();
    }
}");
        }
    }
}
