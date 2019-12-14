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
    public sealed class DisposeObjectsBeforeLosingScopeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new DisposeObjectsBeforeLosingScopeDiagnosticAnalyzer(isEnabledByDefault: true), null);

        private Task TestDiagnosticsAsync(string initialMarkup, params DiagnosticDescription[] expectedDiagnostics)
            => TestDiagnosticsAsync(initialMarkup, parseOptions: null, expectedDiagnostics);
        private Task TestDiagnosticsAsync(string initialMarkup, CSharpParseOptions parseOptions, params DiagnosticDescription[] expectedDiagnostics)
            => TestDiagnosticsAsync(initialMarkup, new TestParameters(parseOptions, retainNonFixableDiagnostics: true), expectedDiagnostics);
        private Task TestDiagnosticMissingAsync(string initialMarkup, CSharpParseOptions parseOptions = null)
            => TestDiagnosticMissingAsync(initialMarkup, new TestParameters(parseOptions, retainNonFixableDiagnostics: true));

        [Fact]
        public async Task LocalWithDisposableInitializer_DisposeCall_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        [|var a = new A()|];
        a.Dispose();
    }
}
");
        }

        [Fact]
        public async Task LocalWithDisposableInitializer_NoDisposeCall_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        var a = [|new A()|];
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId));
        }

        [Fact]
        public async Task LocalWithDisposableAssignment_DisposeCall_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        [|A a;
        a = new A();
        a.Dispose();

        A b = new A();
        a = b;
        a.Dispose();|]
    }
}");
        }

        [Fact]
        public async Task LocalWithDisposableAssignment_NoDisposeCall_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        A a;
        a = [|new A()|];
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId));
        }

        [Fact]
        public async Task ParameterWithDisposableAssignment_DisposeCall_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(A a)
    {
        [|a = new A();
        a.Dispose();|]
    }
}");
        }

        [Fact]
        public async Task ParameterWithDisposableAssignment_NoDisposeCall_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(A a)
    {
        a = [|new A()|];
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId));
        }

        [Fact]
        public async Task OutAndRefParametersWithDisposableAssignment_NoDisposeCall_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(ref A a1, out A a2)
    {
        [|a1 = new A();
        a2 = new A();|]
    }
}");
        }

        [Fact]
        public async Task OutDisposableArgument_NoDisposeCall_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }
}

class Test
{
    void M1(out A param)
    {
        param = new A();
    }

    void M2(out A param2)
    {
        M3(out param2);
    }

    void M3(out A param3)
    {
        param3 = new A();
    }

    void Method()
    {
        [|A a;
        M1(out a);
        A local = a;
        M1(out a);

        M1(out var a2);

        A a3;
        M2(out a3)|];
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out a").WithLocation(32, 12),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out a").WithLocation(34, 12),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out var a2").WithLocation(36, 12),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out a3").WithLocation(39, 12));
        }

        [Fact]
        public async Task OutDisposableArgument_DisposeCall_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|
    void M1(out A param)
    {
        param = new A();
    }

    void M2(out A param2)
    {
        M3(out param2);
    }

    void M3(out A param3)
    {
        param3 = new A();
    }

    void Method()
    {
        A a;
        M1(out a);
        A local = a;
        M1(out a);

        M1(out var a2);

        A a3;
        M2(out a3);

        local.Dispose();
        a.Dispose();
        a2.Dispose();
        a3.Dispose();
    }|]
}");
        }

        [Fact]
        public async Task TryGetSpecialCase_OutDisposableArgument_NoDisposeCall_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class MyCollection
{
    private readonly Dictionary<int, A> _map;
    public MyCollection(Dictionary<int, A> map)
    {
        _map = map;
    }

    public bool ValueExists(int i)
    {
        return [|_map.TryGetValue(i, out var value);|]
    }
}");
        }

        [Fact, WorkItem(2245, "https://github.com/dotnet/roslyn-analyzers/issues/2245")]
        public async Task OutDisposableArgument_StoredIntoField_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    private A _a;
    void M(out A param)
    {
        param = new A();
    }

    void Method()
    {
        [|M(out _a);|]  // This is considered as an escape of interprocedural disposable creation.
    }
}");
        }

        [Fact, WorkItem(2245, "https://github.com/dotnet/roslyn-analyzers/issues/2245")]
        public async Task OutDisposableArgument_WithinTryXXXInvocation_DisposedOnSuccessPath_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Collections.Concurrent;

public class C
{
    private readonly ConcurrentDictionary<object, IDisposable> _dictionary;
    public C(ConcurrentDictionary<object, IDisposable> dictionary)
    {
        _dictionary = dictionary;
    }

    [|public void Remove1(object key)
    {
        if (_dictionary.TryRemove(key, out IDisposable value))
        {
            value.Dispose();
        }
    }

    public void Remove2(object key)
    {
        if (!_dictionary.TryRemove(key, out IDisposable value))
        {
            return;
        }

        value.Dispose();
    }|]
}");
        }

        [Fact, WorkItem(2245, "https://github.com/dotnet/roslyn-analyzers/issues/2245")]
        public async Task OutDisposableArgument_WithinTryXXXInvocation_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;
using System.Collections.Concurrent;

public class C
{
    private readonly ConcurrentDictionary<object, IDisposable> _dictionary;
    public C(ConcurrentDictionary<object, IDisposable> dictionary)
    {
        _dictionary = dictionary;
    }

    public void Remove(object key)
    {
        if (_dictionary.TryRemove(key, [|out IDisposable value|]))
        {
            // value is not disposed.
        }
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId));
        }

        [Fact]
        public async Task LocalWithMultipleDisposableAssignment_DisposeCallOnSome_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        A a;
        [|a = new A(1);
        a = new A(2);
        a.Dispose();
        a = new A(3);|]
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(1)").WithLocation(17, 13),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(3)").WithLocation(20, 13));
        }

        [Fact]
        public async Task FieldWithDisposableAssignment_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    public A a;
    void M1(Test p)
    {
        [|p.a = new A();

        Test l = new Test();
        l.a = new A();

        this.a = new A();|]
    }
}");
        }

        [Fact]
        public async Task PropertyWithDisposableAssignment_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    public A a { get; set; }
    void M1(Test p)
    {
        [|p.a = new A();

        Test l = new Test();
        l.a = new A();

        this.a = new A();|]
    }
}");
        }

        [Fact]
        public async Task Interprocedural_DisposedInHelper_MethodInvocation_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    public A a;
    void M1(Test2 t2)
    {
        [|DisposeHelper(new A());
        t2.DisposeHelper_MethodOnDifferentType(new A());
        DisposeHelper_MultiLevelDown(new A());|]
    }

    void DisposeHelper(A a)
    {
        a.Dispose();
    }

    void DisposeHelper_MultiLevelDown(A a)
    {
        DisposeHelper(a);
    }
}

class Test2
{
    public A a;
    public void DisposeHelper_MethodOnDifferentType(A a)
    {
        a.Dispose();
    }
}");
        }

        [Fact]
        public async Task Interprocedural_DisposeOwnershipTransfer_MethodInvocation_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    public A a;
    void M1()
    {
        [|DisposeOwnershipTransfer(new A());
        var t2 = new Test2();
        t2.DisposeOwnershipTransfer_MethodOnDifferentType(new A());
        DisposeOwnershipTransfer_MultiLevelDown(new A());|]
    }

    void DisposeOwnershipTransfer(A a)
    {
        this.a = a;
    }

    void DisposeOwnershipTransfer_MultiLevelDown(A a)
    {
        DisposeOwnershipTransfer(a);
    }
}

class Test2
{
    public A a;
    public void DisposeOwnershipTransfer_MethodOnDifferentType(A a)
    {
        this.a = a;
    }
}");
        }

        [Fact]
        public async Task Interprocedural_NoDisposeOwnershipTransfer_MethodInvocation_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    public A a;
    void M1(Test2 t2)
    {
        [|NoDisposeOwnershipTransfer(new A(1));
        t2.NoDisposeOwnershipTransfer_MethodOnDifferentType(new A(2));
        NoDisposeOwnershipTransfer_MultiLevelDown(new A(3));|]
    }

    void NoDisposeOwnershipTransfer(A a)
    {
        var str = a.ToString();
        var b = a;
    }

    void NoDisposeOwnershipTransfer_MultiLevelDown(A a)
    {
        NoDisposeOwnershipTransfer(a);
    }
}

class Test2
{
    public A a;
    public void NoDisposeOwnershipTransfer_MethodOnDifferentType(A a)
    {
        var str = a.ToString();
        var b = a;
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(1)").WithLocation(17, 36),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(2)").WithLocation(18, 61),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(3)").WithLocation(19, 51));
        }

        [Fact, WorkItem(2136, "https://github.com/dotnet/roslyn-analyzers/issues/2136")]
        public async Task Interprocedural_DisposedInHelper_ConstructorInvocation_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        [|new DisposeHelperType(new A());
        DisposeHelper_MultiLevelDown(new A());|]
    }

    void DisposeHelper(A a)
    {
        new DisposeHelperType(a);
    }

    void DisposeHelper_MultiLevelDown(A a)
    {
        DisposeHelper(a);
    }
}

class DisposeHelperType
{
    public DisposeHelperType(A a)
    {
        a.Dispose();
    }
}");
        }

        [Fact, WorkItem(2136, "https://github.com/dotnet/roslyn-analyzers/issues/2136")]
        public async Task Interprocedural_DisposeOwnershipTransfer_ConstructorInvocation_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        [|new DisposableOwnerType(new A());
        DisposeOwnershipTransfer_MultiLevelDown(new A());|]
    }

    void DisposeOwnershipTransfer(A a)
    {
        new DisposableOwnerType(a);
    }

    void DisposeOwnershipTransfer_MultiLevelDown(A a)
    {
        DisposeOwnershipTransfer(a);
    }
}

class DisposableOwnerType
{
    public A a;
    public DisposableOwnerType(A a)
    {
        this.a = a;
    }
}");
        }

        [Fact, WorkItem(2136, "https://github.com/dotnet/roslyn-analyzers/issues/2136")]
        public async Task Interprocedural_NoDisposeOwnershipTransfer_ConstructorInvocation_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        [|new NotDisposableOwnerType(new A(1));
        NoDisposeOwnershipTransfer_MultiLevelDown(new A(2));|]
    }

    void NoDisposeOwnershipTransfer(A a)
    {
        new NotDisposableOwnerType(a);
    }

    void NoDisposeOwnershipTransfer_MultiLevelDown(A a)
    {
        NoDisposeOwnershipTransfer(a);
    }
}

class NotDisposableOwnerType
{
    public A a;
    public NotDisposableOwnerType(A a)
    {
        var str = a.ToString();
        var b = a;
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(1)").WithLocation(16, 36),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(2)").WithLocation(17, 51));
        }

        [Fact]
        public async Task DisposeOwnershipTransfer_AtConstructorInvocation_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""A1"" CommonReferences=""true"">
        <ProjectReference>A2</ProjectReference>
        <Document>
using System;

class Test
{
    DisposableOwnerType M1()
    {
        [|return new DisposableOwnerType(new A());|]
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""A2"" CommonReferences=""true"">
        <Document>
using System;

public class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class DisposableOwnerType
{
    public DisposableOwnerType(A a)
    {
    }
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact]
        public async Task LocalWithDisposableAssignment_DisposeBoolCall_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {

    }

    public void Dispose(bool b)
    {
    }
}

class Test
{
    void M1()
    {
        [|A a;
        a = new A();
        a.Dispose(true);

        A b = new A();
        a = b;
        a.Dispose(true);|]
    }
}");
        }

        [Fact]
        public async Task LocalWithDisposableAssignment_CloseCall_NoDiagnostic()
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

class Test
{
    void M1()
    {
        [|A a;
        a = new A();
        a.Close();

        A b = new A();
        a = b;
        a.Close();|]
    }
}");
        }

        [Fact, WorkItem(1796, "https://github.com/dotnet/roslyn-analyzers/issues/1796")]
        public async Task LocalWithDisposableAssignment_DisposeAsyncCall_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Threading.Tasks;

class A : IDisposable
{
    public void Dispose() => DisposeAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}

class Test
{
    async Task M1()
    {
        [|A a;
        a = new A();
        await a.DisposeAsync();

        A b = new A();
        a = b;
        await a.DisposeAsync();|]
    }
}");
        }

        [Fact]
        public async Task ArrayElementWithDisposableAssignment_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(A[] a)
    {
        [|a[0] = new A();|]     // TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    }
}");
        }

        [Fact]
        public async Task ArrayElementWithDisposableAssignment_ConstantIndex_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(A[] a)
    {
        [|a[0] = new A();
        a[0].Dispose();|]
    }
}");
        }

        [Fact]
        public async Task ArrayElementWithDisposableAssignment_NonConstantIndex_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(A[] a, int i)
    {
        [|a[i] = new A();
        a[i].Dispose();|]
    }
}");
        }

        [Fact]
        public async Task ArrayElementWithDisposableAssignment_NonConstantIndex_02_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(A[] a, int i, int j)
    {
        [|a[i] = new A();
        i = j;                // Value of i is now unknown
        a[i].Dispose();|]     // We don't know the points to value of a[i], so don't flag 'new A()'
    }
}");
        }

        [Fact]
        public async Task ArrayInitializer_ElementWithDisposableAssignment_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
       [| A[] a = new A[] { new A() };|]   // TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    }
}");
        }

        [Fact]
        public async Task ArrayInitializer_ElementWithDisposableAssignment_ConstantIndex_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        [|A[] a = new A[] { new A() };
        a[0].Dispose();|]
    }
}");
        }

        [Fact]
        public async Task ArrayInitializer_ElementWithDisposableAssignment_NonConstantIndex_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(int i)
    {
        [|A[] a = new A[] { new A() };
        a[i].Dispose();|]
    }
}");
        }

        [Fact]
        internal async Task CollectionInitializer_ElementWithDisposableAssignment_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        [|List<A> a = new List<A>() { new A() };|]   // TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    }
}");
        }

        [Fact]
        internal async Task CollectionInitializer_ElementWithDisposableAssignment_ConstantIndex_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        [|List<A> a = new List<A>() { new A() };
        a[0].Dispose();|]
    }
}");
        }

        [Fact]
        internal async Task CollectionInitializer_ElementWithDisposableAssignment_NonConstantIndex_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(int i)
    {
        [|List<A> a = new List<A>() { new A() };
        a[i].Dispose();|]
    }
}");
        }

        [Fact]
        internal async Task CollectionAdd_SpecialCases_ElementWithDisposableAssignment_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Collections;
using System.Collections.Generic;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class NonGenericList : ICollection
{
    public void Add(A item)
    {
    }

    public int Count => throw new NotImplementedException();

    public object SyncRoot => throw new NotImplementedException();

    public bool IsSynchronized => throw new NotImplementedException();

    public void CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

class Test
{
    void M1()
    {
        [|List<A> a = new List<A>();
        a.Add(new A(1));

        A b = new A(2);
        a.Add(b);

        NonGenericList l = new NonGenericList();
        l.Add(new A(3));

        b = new A(4);
        l.Add(b);|]
    }
}");
        }

        [Fact]
        internal async Task CollectionAdd_IReadOnlyCollection_SpecialCases_ElementWithDisposableAssignment_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class MyReadOnlyCollection : IReadOnlyCollection<A>
{
    public void Add(A item)
    {
    }

    public int Count => throw new NotImplementedException();

    public IEnumerator<A> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

class Test
{
    void M1()
    {
        [|var myReadOnlyCollection = new MyReadOnlyCollection();
        myReadOnlyCollection.Add(new A(1));
        A a = new A(2);
        myReadOnlyCollection.Add(a);

        var bag = new ConcurrentBag<A>();
        bag.Add(new A(3));
        A a2 = new A(4);
        bag.Add(a2);|]
    }
}");
        }

        [Fact]
        public async Task MemberInitializerWithDisposableAssignment_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public int X;
    public void Dispose()
    {
    }
}

class Test
{
    public A a;
    void M1()
    {
        [|var a = new Test { a = { X = 0 } };|]
    }
}");
        }

        [Fact]
        public async Task StructImplementingIDisposable_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

struct A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        [|var a = new A();|]
    }
}");
        }

        [Fact]
        public async Task NonUserDefinedConversions_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : A
{
}

class Test
{
    void M1()
    {
        [|object obj = new A();   // Implicit conversion from A to object
        ((A)obj).Dispose();     // Explicit conversion from object to A

        A a = new B();          // Implicit conversion from B to A     
        a.Dispose();|]
    }
}");
        }

        [Fact]
        public async Task NonUserDefinedConversions_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : A
{
}

class Test
{
    void M1()
    {
        [|object obj = new A();   // Implicit conversion from A to object
        A a = (A)new B();|]       // Explicit conversion from B to A
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(19, 22),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new B()").WithLocation(20, 18));
        }

        [Fact]
        public async Task UserDefinedConversions_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }

    public static implicit operator A(B value)
    {
        value.Dispose();
        return null;
    }

    public static explicit operator B(A value)
    {
        value.Dispose();
        return null;
    }
}

class B : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    Test(string s)
    {
    }

    void M1()
    {
        [|A a = new B();      // Implicit user defined conversion
        B b = (B)new A();|]   // Explicit user defined conversion
    }
}");
        }

        [Fact]
        public async Task LocalWithDisposableAssignment_ByRef_DisposedInCallee_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a = new A();
        M2(ref a);
    }

    void M2(ref A a)
    {
        a.Dispose();
        a = null;
    }|]
}");
        }

        [Fact]
        public async Task LocalWithDisposableAssignment_ByRefEscape_AbstractVirtualMethod_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

public class A : IDisposable
{
    public void Dispose()
    {
    }
}

public abstract class Test
{
    void M1()
    {
        [|A a = new A();
        M2(ref a);

        a = new A();
        M3(ref a);|]
    }

    public virtual void M2(ref A a)
    {
    }

    public abstract void M3(ref A a);
}");
        }

        [Fact]
        public async Task LocalWithDisposableAssignment_OutRefKind_NotDisposed_Diagnostic()
        {
            // Local/parameter passed as out is not considered escaped.
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a = new A();
        M2(out a);
    }

    void M2(out A a)
    {
        a = new A();
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(15, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out a").WithLocation(16, 12));
        }

        [Fact]
        public async Task LocalWithDefaultOfDisposableAssignment_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1()
    {
        [|A a = default(A);|]
    }
}");
        }

        [Fact]
        public async Task NullCoalesce_NoDiagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(A a)
    {
        [|A b = a ?? new A();
        b.Dispose();

        A c = new A();
        A d = c ?? a;
        d.Dispose();

        a = new A();
        A e = a ?? new A();
        e.Dispose();|]
    }
}");
        }

        [Fact]
        public async Task NullCoalesce_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(A a)
    {
        A b = a ?? [|new A()|];
        a.Dispose();
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(15, 20));
        }

        [Fact]
        public async Task WhileLoop_DisposeOnBackEdge_NoDiagnostic()
        {
            // Need precise CFG to avoid false reports.
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    void M1(bool flag)
    {
        [|A a = new A();
        while (true)
        {
            a.Dispose();
            if (flag)
            {
                break;  // All 'A' instances have been disposed on this path, so no diagnostic should be reported.
            }
            a = new A();
        }|]
    }
}");
        }

        [Fact, WorkItem(1648, "https://github.com/dotnet/roslyn-analyzers/issues/1648")]
        public async Task WhileLoop_MissingDisposeOnExit_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a = new A(1);  // Allocated outside the loop and disposed inside a loop is not a recommended pattern and is flagged.
        while (true)
        {
            a.Dispose();
            a = new A(2);   // This instance will not be disposed on loop exit.
        }
    }|]
}",
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(1)").WithLocation(16, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(2)").WithLocation(20, 17));
        }

        [Fact]
        public async Task WhileLoop_MissingDisposeOnEntry_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    public bool Flag;
    [|void M1()
    {
        A a;      
        while ((a = new A(1)) != null)   // This instance will never be disposed, but is not flagged as there is no feasible loop exit.
        {
            a = new A(2);
            a.Dispose();
        }
    }

    void M2(bool flag)
    {
        A a;      
        while ((a = new A(3)) != null)   // This instance will never be disposed on loop exit.
        {
            if (Flag)
            {
                break;
            }
            a = new A(4);
            a.Dispose();
        }
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(3)").WithLocation(28, 21));
        }

        [Fact]
        public async Task DoWhileLoop_DisposeOnBackEdge_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1(bool flag)
    {
        A a = new A();
        do
        {
            a.Dispose();
            if (flag)
            {
                break;  // All 'A' instances have been disposed on this path, so no diagnostic should be reported.
            }
            a = new A();
        } while (true);
    }|]
}");
        }

        [Fact]
        public async Task DoWhileLoop_MissingDisposeOnExit_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a = new A(1);
        do
        {
            a.Dispose();
            a = new A(2);   // This instance will not be disposed on loop exit.
        } while (true);
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(2)").WithLocation(20, 17));
        }

        [Fact]
        public async Task DoWhileLoop_MissingDisposeOnEntry_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a;      
        do
        {
            a = new A(1);
            a.Dispose();
        } while ((a = new A(2)) != null);   // This instance will never be disposed, but it is not flagged as there is no feasible loop exit.
    }

    void M2()
    {
        A a = null;      
        do
        {
            if (a != null)
            {
                break;
            }
            a = new A(3);
            a.Dispose();
        } while ((a = new A(4)) != null);   // This instance will never be disposed.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(4)").WithLocation(35, 23));
        }

        [Fact]
        public async Task ForLoop_DisposeOnBackEdge_MayBeNotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1(bool flag)
    {
        A a = new A(1);      // Allocation outside a loop, dispose inside a loop is not a recommended pattern and should fire diagnostic.
        for (int i = 0; i < 10; i++)
        {
            a.Dispose();
            if (flag)
            {
                break;  // All 'A' instances have been disposed on this path.
            }

            a = new A(2); // This can leak on loop exit, and is flagged as a maybe disposed violation.
        }
    }|]
}",
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(1)").WithLocation(16, 15),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(2)").WithLocation(25, 17));
        }

        [Fact]
        public async Task ForLoop_MissingDisposeOnExit_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a = new A(1);  // Allocation outside a loop, dispose inside a loop is not a recommended pattern and should fire diagnostic.
        for (int i = 0; i < 10; i++)
        {
            a.Dispose();
            a = new A(2);   // This instance will not be disposed on loop exit.
        }
    }|]
}",
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(1)").WithLocation(16, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(2)").WithLocation(20, 17));
        }

        [Fact]
        public async Task ForLoop_MissingDisposeOnEntry_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a;
        int i;
        for (i = 0, a = new A(1); i < 10; i++)   // This 'A' instance will never be disposed.
        {
            a = new A(2);
            a.Dispose();
        }
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(1)").WithLocation(18, 25));
        }

        [Fact]
        public async Task IfStatement_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : A
{
}

class Test
{
    [|void M1(A a, string param)
    {
        A a1 = new A();
        B a2 = new B();
        A b;
        if (param != null)
        {
            a = a1;
            b = new B();
        }
        else 
        {
            a = a2;
            b = new A();
        }

        a.Dispose();         // a points to either a1 or a2.
        b.Dispose();         // b points to either instance created in if or else.
    }|]
}");
        }

        [Fact]
        public async Task IfStatement_02_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : A
{
}

class Test
{
    [|void M1(A a, string param, string param2)
    {
        A a1 = new A();
        B a2 = new B();
        A b;
        if (param != null)
        {
            a = a1;
            b = new B();

            if (param == """")
            {
                a = new B();
            }
            else
            {
                if (param2 != null)
                {
                    b = new A();
                }
                else
                {
                    b = new B();
                }
            }
        }
        else 
        {
            a = a2;
            b = new A();
        }

        a.Dispose();         // a points to either a1 or a2 or instance created in 'if(param == """")'.
        b.Dispose();         // b points to either instance created in outer if or outer else or innermost if or innermost else.
    }|]
}");
        }

        [Fact]
        public async Task IfStatement_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public A() { }
    public void Dispose()
    {
    }
}

class B : A
{
}

class C : B
{
}

class D : C
{
}

class E : D
{
}

class Test
{
    [|void M1(A a, string param, string param2)
    {
        A a1 = new A(1);     // Maybe disposed.
        B a2 = new B();     // Never disposed.
        A b;
        if (param != null)
        {
            a = a1;
            b = new C();     // Never disposed.
        }
        else
        {
            a = a2;
            b = new D();     // Never disposed.
        }

        // a points to either a1 or a2.
        // b points to either instance created in if or else.

        if (param != null)
        {
            A c = new A(2);
            a = c;
            b = a1;
        }
        else 
        {
            C d = new E();
            b = d;
            a = b;
        }

        a.Dispose();         // a points to either c or d.
        b.Dispose();         // b points to either a1 or d.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new B()").WithLocation(34, 16),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new C()").WithLocation(39, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new D()").WithLocation(44, 17));
        }

        [Fact]
        public async Task IfStatement_02_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public A() { }
    public void Dispose()
    {
    }
}

class B : A
{
}

class C : B
{
}

class D : C
{
}

class E : D
{
}

class Test
{
    [|void M1(A a, string param, string param2)
    {
        A a1 = new B();     // Never disposed
        B a2 = new C();     // Never disposed
        A b;
        if (param != null)
        {
            a = a1;
            b = new A(1);     // Maybe disposed

            if (param == """")
            {
                a = new D();     // Never disposed
            }
            else
            {
                if (param2 != null)
                {
                    b = new A(2);    // Maybe disposed
                }
                else
                {
                    b = new A(3);    // Maybe disposed
                    if (param == """")
                    {
                        b = new A(4);    // Maybe disposed
                    }
                }

                if (param2 == """")
                {
                    b.Dispose();    // b points to one of the three instances of A created above.
                    b = new A(5);    // Always disposed
                }
            }
        }
        else 
        {
            a = a2;
            b = new A(6);        // Maybe disposed
            if (param2 != null)
            {
                a = new A(7);    // Always disposed
            }
            else
            {
                a = new A(8);    // Always disposed
                b = new A(9);    // Always disposed
            }

            a.Dispose();
        }

        b.Dispose();         
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new B()").WithLocation(33, 16),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new C()").WithLocation(34, 16),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new D()").WithLocation(43, 21));
        }

        [Fact]
        public async Task UsingStatement_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        using (var a = new A())
        {
        }

        A b;
        using (b = new A())
        {
        }

        using (A c = new A(), d = new A())
        {
        }

        A e = new A();
        using (e)
        {
        }

        using (A f = null)
        {
        }
    }|]
}");
        }

        [Fact, WorkItem(2201, "https://github.com/dotnet/roslyn-analyzers/issues/2201")]
        public async Task UsingStatementInTryCatch_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System.IO;

class Test
{
    void M1()
    {
        try
        {
            [|using (var ms = new MemoryStream())|]
            {
            }
        }
        catch
        {
        }
    }
}");
        }

        [Fact, WorkItem(2201, "https://github.com/dotnet/roslyn-analyzers/issues/2201")]
        public async Task NestedTryFinallyInTryCatch_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System.IO;

class Test
{
    void M1()
    {
        try
        {
            [|var ms = new MemoryStream();|]
            try
            {
            }
            finally
            {
                ms?.Dispose();
            }
        }
        catch
        {
        }
    }
}");
        }

        [Fact]
        public async Task ReturnStatement_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Collections.Generic;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|A M1()
    {
        return new A();
    }

    A M2(A a)
    {
        a = new A();
        return a;
    }

    A M3(A a)
    {
        a = new A();
        A b = a;
        return b;
    }

    A M4(A a) => new A();

    IEnumerable<A> M5()
    {
        yield return new A();
    }|]
}");
        }

        [Fact]
        public async Task ReturnStatement_02_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : I, IDisposable
{
    public void Dispose()
    {
    }
}

interface I
{
}

class Test
{
    [|I M1()
    {
        return new A();
    }

    I M2()
    {
        return new A() as I;
    }|]
}");
        }

        [Fact]
        public async Task LocalFunctionInvocation_EmptyBody_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a;
        a = new A();

        void MyLocalFunction()
        {
        };

        MyLocalFunction();    // This should not change state of 'a'.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(16, 13));

            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunctionInvocation_DisposesCapturedValue_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a = new A();

        void MyLocalFunction()
        {
            a.Dispose();
        };

        MyLocalFunction();    // This should change state of 'a' to be Disposed.
    }|]
}");

            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunctionInvocation_CapturedValueAssignedNewDisposable_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a;

        void MyLocalFunction()
        {
            a = new A();
        };

        MyLocalFunction();    // This should change state of 'a' to be NotDisposed and fire a diagnostic.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(19, 17));

            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunctionInvocation_ChangesCapturedValueContextSensitive_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a;

        void MyLocalFunction(A b)
        {
            a = b;
        };

        MyLocalFunction(new A());    // This should change state of 'a' to be NotDisposed and fire a diagnostic.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(22, 25));

            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunction_DisposableCreationNotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        void MyLocalFunction()
        {
            A a = new A();  // This should fire a diagnostic.
        };

        MyLocalFunction();
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(17, 19));

            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunction_DisposableCreation_InvokedMultipleTimes_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        void MyLocalFunction(int i)
        {
            A a = new A();  // This should fire a single diagnostic.
        };

        MyLocalFunction(1);
        MyLocalFunction(2);
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(17, 19));

            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunction_DisposableCreationReturned_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A MyLocalFunction(int i)
        {
            return new A();
        };

        var a = MyLocalFunction(1);  // This should fire a diagnostic.
        var b = MyLocalFunction(2);  // This should fire a diagnostic.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "MyLocalFunction(1)").WithLocation(20, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "MyLocalFunction(2)").WithLocation(21, 17));

            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunction_DisposableCreationReturned_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A MyLocalFunction()
        {
            return new A();
        };

        var a = MyLocalFunction();
        a.Dispose();
    }|]
}");
            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunction_DisposableCreationAssignedToRefOutParameter_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a1 = null, a2;
        MyLocalFunction(ref a1, out a2);  // This should fire two diagnostics.
        return;

        void MyLocalFunction(ref A param1, out A param2)
        {
            param1 = new A();
            param2 = new A();
        };
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "ref a1").WithLocation(16, 25),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out a2").WithLocation(16, 33));

            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunction_DisposableCreationAssignedToRefOutParameter_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a1 = null, a2;
        MyLocalFunction(ref a1, out a2);
        a1.Dispose();
        a2.Dispose();
        return;

        void MyLocalFunction(ref A param1, out A param2)
        {
            param1 = new A();
            param2 = new A();
        };
    }|]
}");

            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunction_DisposableCreationAssignedToRefOutParameter_MultipleCalls_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a1 = null, a2;
        MyLocalFunction(ref /*1*/a1, out /*2*/a2);    // This should fire two diagnostics.
        MyLocalFunction(ref /*3*/a1, out /*4*/a2);    // No diagnostics.
        a1.Dispose();
        a2.Dispose();
        return;

        void MyLocalFunction(ref A param1, out A param2)
        {
            param1 = new A();
            param2 = new A();
        };
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "ref /*1*/a1").WithLocation(16, 25),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out /*2*/a2").WithLocation(16, 38));

            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunction_DisposableCreation_MultipleLevelsBelow_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a1 = null, a2;
        MyLocalFunction1(ref /*1*/a1, out /*2*/a2);    // This should fire two diagnostics.
        return;

        void MyLocalFunction1(ref A param1, out A param2)
        {
            MyLocalFunction2(ref /*3*/param1, out /*4*/param2);
        };

        void MyLocalFunction2(ref A param3, out A param4)
        {
            param3 = new A();
            param4 = new A();
        };
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "ref /*1*/a1").WithLocation(16, 26),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out /*2*/a2").WithLocation(16, 39));

            // VB has no local functions.
        }

        [Fact]
        public async Task LocalFunction_DisposableCreation_MultipleLevelsBelow_Nested_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a1 = null, a2;
        MyLocalFunction1(ref /*1*/a1, out /*2*/a2);    // This should fire two diagnostics.
        return;

        void MyLocalFunction1(ref A param1, out A param2)
        {
            MyLocalFunction2(ref /*3*/param1, out /*4*/param2);

            void MyLocalFunction2(ref A param3, out A param4)
            {
                param3 = new A();
                param4 = new A();
            };
        };
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "ref /*1*/a1").WithLocation(16, 26),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out /*2*/a2").WithLocation(16, 39));

            // VB has no local functions.
        }

        [Fact]
        public async Task LambdaInvocation_EmptyBody_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a;
        a = new A();

        System.Action myLambda = () =>
        {
        };

        myLambda();    // This should not change state of 'a'.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(16, 13));
        }

        [Fact]
        public async Task LambdaInvocation_DisposesCapturedValue_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a = new A();

        System.Action myLambda = () =>
        {
            a.Dispose();
        };

        myLambda();    // This should change state of 'a' to be Disposed.
    }|]
}");
        }

        [Fact]
        public async Task LambdaInvocation_CapturedValueAssignedNewDisposable_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a;

        System.Action myLambda = () =>
        {
            a = new A();
        };

        myLambda();    // This should change state of 'a' to be NotDisposed and fire a diagnostic.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(19, 17));
        }

        [Fact]
        public async Task LambdaInvocation_ChangesCapturedValueContextSensitive_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a;

        System.Action<A> myLambda = b =>
        {
            a = b;
        };

        myLambda(new A());    // This should change state of 'a' to be NotDisposed and fire a diagnostic.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(22, 18));
        }

        [Fact]
        public async Task Lambda_DisposableCreationNotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        System.Action myLambda = () =>
        {
            A a = new A();  // This should fire a diagnostic.
        };

        myLambda();
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(17, 19));
        }

        [Fact]
        public async Task Lambda_DisposableCreation_InvokedMultipleTimes_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        System.Action myLambda = () =>
        {
            A a = new A();  // This should fire a single diagnostic.
        };

        myLambda();
        myLambda();
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(17, 19));
        }

        [Fact]
        public async Task Lambda_DisposableCreationReturned_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        System.Func<A> myLambda = () =>
        {
            return new A();
        };

        var a = myLambda(/*1*/);  // This should fire a diagnostic.
        var b = myLambda(/*2*/);  // This should fire a diagnostic.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "myLambda(/*1*/)").WithLocation(20, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "myLambda(/*2*/)").WithLocation(21, 17));
        }

        [Fact]
        public async Task Lambda_DisposableCreationReturned_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        System.Func<A> myLambda = () =>
        {
            return new A();
        };

        var a = myLambda();
        a.Dispose();
    }|]
}");
        }

        [Fact]
        public async Task Lambda_DisposableCreationAssignedToRefOutParameter_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    delegate void MyDelegate(ref A a1, out A a2);
    [|void M1()
    {
        MyDelegate myDelegate = (ref A param1, out A param2) =>
        {
            param1 = new A();
            param2 = new A();
        };

        A a1 = null, a2;
        myDelegate(ref a1, out a2);  // This should fire two diagnostics.
        return;
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "ref a1").WithLocation(23, 20),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out a2").WithLocation(23, 28));
        }

        [Fact]
        public async Task Lambda_DisposableCreationAssignedToRefOutParameter_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    delegate void MyDelegate(ref A a1, out A a2);
    [|void M1()
    {
        MyDelegate myDelegate = (ref A param1, out A param2) =>
        {
            param1 = new A();
            param2 = new A();
        };

        A a1 = null, a2;
        myDelegate(ref a1, out a2);
        a1.Dispose();
        a2.Dispose();
        return;
    }|]
}");
        }

        [Fact]
        public async Task Lambda_DisposableCreationAssignedToRefOutParameter_MultipleCalls_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    delegate void MyDelegate(ref A a1, out A a2);
    [|void M1()
    {
        MyDelegate myDelegate = (ref A param1, out A param2) =>
        {
            param1 = new A();
            param2 = new A();
        };

        A a1 = null, a2;
        myDelegate(ref /*1*/a1, out /*2*/a2);    // This should fire two diagnostics.
        myDelegate(ref /*3*/a1, out /*4*/a2);    // No diagnostics.
        a1.Dispose();
        a2.Dispose();
        return;
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "ref /*1*/a1").WithLocation(23, 20),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out /*2*/a2").WithLocation(23, 33));
        }

        [Fact]
        public async Task Lambda_DisposableCreation_MultipleLevelsBelow_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    delegate void MyDelegate(ref A a1, out A a2);
    [|void M1()
    {
        MyDelegate myDelegate2 = (ref A param3, out A param4) =>
        {
            param3 = new A();
            param4 = new A();
        };

        MyDelegate myDelegate1 = (ref A param1, out A param2) =>
        {
            myDelegate2(ref /*3*/param1, out /*4*/param2);
        };

        A a1 = null, a2;
        myDelegate1(ref /*1*/a1, out /*2*/a2);    // This should fire two diagnostics.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "ref /*1*/a1").WithLocation(28, 21),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out /*2*/a2").WithLocation(28, 34));
        }

        [Fact]
        public async Task Lambda_DisposableCreation_MultipleLevelsBelow_Nested_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    delegate void MyDelegate(ref A a1, out A a2);
    [|void M1()
    {
        MyDelegate myDelegate1 = (ref A param1, out A param2) =>
        {
            MyDelegate myDelegate2 = (ref A param3, out A param4) =>
            {
                param3 = new A();
                param4 = new A();
            };

            myDelegate2(ref /*3*/param1, out /*4*/param2);
        };

        A a1 = null, a2;
        myDelegate1(ref /*1*/a1, out /*2*/a2);    // This should fire two diagnostics.
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "ref /*1*/a1").WithLocation(28, 21),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "out /*2*/a2").WithLocation(28, 34));
        }

        [Fact]
        public async Task Lambda_InvokedFromInterprocedural_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a1 = new A();
        M2(() => a1.Dispose());
    }

    void M2(Action disposeCallback) => disposeCallback();|]
}");
        }

        [Fact]
        internal async Task Lambda_MayBeInvokedFromInterprocedural_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    public bool Flag;
    [|void M1()
    {
        A a1 = new A(1);
        M2(() => a1.Dispose());

        A a2 = new A(2);
        if (Flag)
            M3(() => a2.Dispose());
    }

    void M2(Action disposeCallback)
    {
        if (Flag)
            disposeCallback();
    }

    void M3(Action disposeCallback) => disposeCallback();|]
}",
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(1)").WithLocation(17, 16),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(2)").WithLocation(20, 16));
        }

        [Fact]
        public async Task DelegateInvocation_EmptyBody_NoArguments_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a;
        a = new A();

        System.Action myDelegate = M2;
        myDelegate();    // This should not change state of 'a' as it is not passed as argument.
    }|]

    void M2() { }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(16, 13));
        }

        [Fact]
        public async Task DelegateInvocation_PassedAsArgumentButNotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a;
        a = new A();

        System.Action<A> myDelegate = M2;
        myDelegate(a);    // This should not change state of 'a'.
    }|]

    void M2(A a) { }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(16, 13));
        }

        [Fact, WorkItem(1813, "https://github.com/dotnet/roslyn-analyzers/issues/1813")]
        public async Task DelegateInvocation_DisposesCapturedValue_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        A a;
        a = new A();

        System.Action<A> myDelegate = M2;
        myDelegate(a);    // This should change state of 'a' to be disposed as we perform interprocedural analysis.
    }|]

    void M2(A a) => a.Dispose();
}");
        }

        [Fact]
        public async Task DisposableCreationNotAssignedToAVariable_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public int X;
    public A(int i) { }
    
    public void Dispose()
    {
    }

    public void M()
    {
    }
}

class Test
{
    [|void M1()
    {
        new A(1);
        new A(2).M();
        var x = new A(3).X;
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(1)").WithLocation(22, 9),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(2)").WithLocation(23, 9),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(3)").WithLocation(24, 17));
        }

        [Fact]
        public async Task DisposableCreationPassedToDisposableConstructor_NoDiagnostic()
        {
            // Dispose ownership transfer
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
    private readonly A _a;
    public B(A a)
    {
        _a = a;
    }

    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        var b = new B(new A());
        b.Dispose();

        var a = new A();
        B b2 = null;
        try
        {
            b2 = new B(a);
        }
        finally
        {
            if (b2 != null)
            {
                b2.Dispose();
            }
        }

        var a2 = new A();
        B b3 = null;
        try
        {
            b3 = new B(a2);
        }
        finally
        {
            if (b3 != null)
            {
                b3.Dispose();
            }
        }
    }|]
}");
        }

        [Fact]
        public async Task DisposableObjectOnlyDisposedOnExceptionPath_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        var a = new A(1);
        try
        {
            ThrowException();
        }
        catch (Exception)
        {
            a.Dispose();
        }
    }

    void M2()
    {
        var a = new A(2);
        try
        {
            ThrowException();
        }
        catch (System.IO.IOException)
        {
            a.Dispose();
        }
    }

    void M3()
    {
        var a = new A(3);
        try
        {
            ThrowException();
        }
        catch (System.IO.IOException)
        {
            a.Dispose();
        }
        catch (Exception)
        {
            a.Dispose();
        }
    }

    void M4(bool flag)
    {
        var a = new A(4);
        try
        {
            ThrowException();
        }
        catch (System.IO.IOException)
        {
            if (flag)
            {
                a.Dispose();
            }
        }
    }|]

    void ThrowException()
    {
        throw new NotImplementedException();
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(1)").WithLocation(16, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(2)").WithLocation(29, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(3)").WithLocation(42, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(4)").WithLocation(59, 17));
        }

        [Fact]
        public async Task DisposableObjectDisposed_FinallyPath_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        var a = new A();
        try
        {
            ThrowException();
        }
        finally
        {
            a.Dispose();
        }
    }

    void M2()
    {
        var a = new A();
        try
        {
            ThrowException();
        }
        catch (Exception)
        {
        }
        finally
        {
            a.Dispose();
        }
    }

    void M3()
    {
        var a = new A();
        try
        {
            ThrowException();   
            a.Dispose();
            a = null;
        }
        catch (System.IO.IOException)
        {
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }
    }|]

    void ThrowException()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact]
        public async Task DelegateCreation_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class Test
{
    [|void M1()
    {
        Func<A> createA = M2;
        A a = createA();
        a.Dispose();
    }

    A M2()
    {
        return new A();
    }|]
}");
        }

        [Fact]
        public async Task MultipleReturnStatements_AllInstancesReturned_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class Test
{
    [|A M1(bool flag)
    {
        A a;
        if (flag)
        {
            A a2 = new A();
            a = a2;
            return a;
        }

        A a3 = new A();
        a = a3;
        return a;
    }|]
}");
        }

        [Fact]
        public async Task MultipleReturnStatements_AllInstancesEscapedWithOutParameter_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class Test
{
    [|void M1(bool flag, out A a)
    {
        if (flag)
        {
            A a2 = new A();
            a = a2;
            return;
        }

        A a3 = new A();
        a = a3;
        return;
    }|]
}");
        }

        [Fact]
        public async Task MultipleReturnStatements_AllButOneInstanceReturned_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

public class Test
{
    [|A M1(int flag, bool flag2, bool flag3)
    {
        A a = null;
        if (flag == 0)
        {
            A a2 = new A(1);        // Escaped with return inside below nested 'if', not disposed on other paths.
            a = a2;

            if (!flag2)
            {
                if (flag3)
                {
                    return a;
                }
            }
        }
        else
        {
            a = new A(2);        // Escaped with return inside below nested 'else', not disposed on other paths.
            if (flag == 1)
            {
                a = new A(3);    // Never disposed.
            }
            else
            {
                if (flag3)
                {
                    a = new A(4);    // Escaped with return inside below 'else', not disposed on other paths.
                }

                if (flag2)
                {
                }
                else
                {
                    return a;
                }
            }
        }

        A a3 = new A(5);     // Always escaped with below return, ensure no diagnostic.
        a = a3;
        return a;
    }|]
}",
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(1)").WithLocation(19, 20),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(2)").WithLocation(32, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(3)").WithLocation(35, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(4)").WithLocation(41, 25));
        }

        [Fact]
        public async Task MultipleReturnStatements_AllButOneInstanceEscapedWithOutParameter_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

class B : A
{
}

public class Test
{
    [|void M1(int flag, bool flag2, bool flag3, out A a)
    {
        a = null;
        if (flag == 0)
        {
            A a2 = new A();        // Escaped with return inside below nested 'if'.
            a = a2;

            if (!flag2)
            {
                if (flag3)
                {
                    return;
                }
            }
        }
        else
        {
            a = new A();        // Escaped with return inside below nested 'else'.
            if (flag == 1)
            {
                a = new B();    // Never disposed.
            }
            else
            {
                if (flag3)
                {
                    a = new A();    // Escaped with return inside below 'else'.
                }

                if (flag2)
                {
                }
                else
                {
                    return;
                }
            }
        }

        A a3 = new A();     // Escaped with below return.
        a = a3;
        return;
    }|]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new B()").WithLocation(38, 21));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public async Task DisposableAllocation_AssignedToTuple_Escaped_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class Test
{
    [|(A, int) M1()
    {
        A a = new A();
        return (a, 0);
    }

    (A, int) M2()
    {
        A a = new A();
        (A, int) b = (a, 0);
        return b;
    }|]
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        internal async Task DisposableAllocation_AssignedToTuple_Escaped_SpecialCases_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class Test
{
    [|
    // Nested tuple
    ((A, int), int) M1()
    {
        A a = new A();
        ((A, int), int) b = ((a, 0), 1);
        return b;
    }

    // Declaration expression target
    A M2()
    {
        A a = new A();
        var ((a2, x), y) = ((a, 0), 1);
        return a2;
    }

    // Declaration expression target with discards
    A M3()
    {
        A a = new A();
        var ((a2, _), _) = ((a, 0), 1);
        return a2;
    }

    // Declaration expressions in target
    A M4()
    {
        A a = new A();
        ((var a2, var x), var y) = ((a, 0), 1);
        return a2;
    }

    // Discards in target
    A M5()
    {
        A a = new A();
        ((var a2, _), _) = ((a, 0), 1);
        return a2;
    }

    // Tuple with multiple disposable escape
    (A, A) M6()
    {
        A a = new A();
        A a2 = new A();
        var b = (a, a2);
        return b;
    }
    |]
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public async Task DisposableAllocation_AssignedToTuple_NotDisposed_SpecialCases_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }

    public void Dispose()
    {
    }
}

public class Test
{
    [|
    // Nested tuple
    ((A, int), (A, int)) M1()
    {
        A a = new A(1);     // Should be flagged.
        A a2 = new A(2);
        ((A, int), (A, int)) b = ((a2, 0), (a2, 0));
        return b;
    }

    // Declaration expression target
    A M2()
    {
        A a = new A(3);     // Should be flagged.
        var ((a2, x), y) = ((a, 0), 1);
        return null;
    }

    // Declaration expression target with discards
    A M3()
    {
        A a = new A(4);     // Should be flagged.
        var ((a2, _), _) = ((a, 0), 1);
        return null;
    }

    // Declaration expressions in target
    A M4()
    {
        A a = new A(5);     // Should be flagged.
        ((var a2, var x), var y) = ((a, 0), 1);
        return null;
    }

    // Discards in target
    A M5()
    {
        A a = new A(6);     // Should be flagged.
        ((var a2, _), _) = ((a, 0), 1);
        return null;
    }

    // Tuple with multiple disposable escape
    (A, A) M6()
    {
        A a = new A(7);     // Should be flagged.
        A a2 = new A(8);
        var b = (a2, a2);
        return b;
    }
    |]
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(1)").WithLocation(19, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(3)").WithLocation(28, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(4)").WithLocation(36, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(5)").WithLocation(44, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(6)").WithLocation(52, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(7)").WithLocation(60, 15));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public async Task DisposableAllocation_EscapedTupleLiteral_SpecialCases_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class Test
{
    [|
    // Tuple literal escaped cases.
    ((A, int), int) M1()
    {
        A a = new A();
        return ((a, 0), 1);
    }

    (A, A) M2()
    {
        A a = new A();
        A a2 = new A();
        return (a, a2);
    }

    void M3(out (A, A) arg)
    {
        A a = new A();
        A a2 = new A();
        arg = (a, a2);
    }

    void M4(out (A, A) arg)
    {
        A a = new A();
        A a2 = new A();
        var a3 = (a, a2);
        arg = a3;
    }

    void M5(ref (A, A) arg)
    {
        A a = new A();
        A a2 = new A();
        var a3 = (a, a2);
        arg = a3;
    }
    |]
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public async Task DisposableAllocation_AddedToTupleLiteral_SpecialCases_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

public class Test
{
    [|
    // Tuple literal assignment cases.
    void M1()
    {
        A a = new A(1);
        var x = ((a, 0), 1);
    }

    void M2()
    {
        A a = new A(2);
        A a2 = new A(3);
        var x = (a, a2);
    }

    void M3(out (A, A) arg)
    {
        A a = new A(4);
        A a2 = new A(5);
        arg = (a, a2);
        arg = default((A, A));
    }

    void M4(out (A, A) arg)
    {
        A a = new A(6);
        A a2 = new A(7);
        var a3 = (a, a2);
        arg = a3;
        arg = default((A, A));
    }
    |]
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(1)").WithLocation(18, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(2)").WithLocation(24, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(3)").WithLocation(25, 16),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(4)").WithLocation(31, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(5)").WithLocation(32, 16),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(6)").WithLocation(39, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(7)").WithLocation(40, 16));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public async Task DisposableAllocation_AssignedToTuple_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class Test
{
    [|void M1()
    {
        A a = new A();
        var b = (a, 0);
    }

    void M2()
    {
        A a = new A();
        (A, int) b = (a, 0);
    }|]
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(15, 15),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A()").WithLocation(21, 15));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public async Task DisposableAllocation_AssignedToTuple_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class Test
{
    [|void M1()
    {
        A a = new A();
        var b = (a, 0);
        b.a.Dispose();
    }

    void M2()
    {
        A a = new A();
        (A, int) b = (a, 0);
        a.Dispose();
    }|]
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public async Task DisposableAllocation_AssignedToTuple_Item1_Disposed_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class Test
{
    [|void M1()
    {
        A a = new A();
        var b = (a, 0);
        b.Item1.Dispose();
    }|]
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3));
        }

        [Fact, WorkItem(1571, "https://github.com/dotnet/roslyn-analyzers/issues/1571")]
        public async Task DisposableAllocation_DeconstructionAssignmentToTuple_DeconstructMethod_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;
using System.Collections.Generic;

internal static class KeyValuePairExtensions
{
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}

class A : IDisposable
{
    public A(int i) { }
    public int X { get; }
    public void Dispose()
    {
    }

    public int M() => 0;
}

public class Test
{
    [|void M1(IDictionary<A, int> map)
    {
        foreach ((A a, _) in map)
        {
            var x = new A(1);
            var y = a.M();
        }
    }

    void M2(IDictionary<A, int> map)
    {
        foreach (var (a, _) in map)
        {
            var x = new A(2);
            var y = a.M();
        }
    }

    void M3(KeyValuePair<A, int> pair, int y)
    {
        A a;
        (a, y) = pair;
        var x = new A(3);
    }|]
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(1)").WithLocation(31, 21),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(2)").WithLocation(40, 21),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(3)").WithLocation(49, 17));
        }

        [Fact]
        public async Task DifferentDisposePatternsInFinally_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    [|
    void M1()
    {
        // Allocated before try, disposed in finally with conditional access.
        A a = new A(1);
        try
        {
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M2()
    {
        // Allocated in try, disposed in finally with conditional access.
        A a = null;
        try
        {
            a = new A(2);
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M3()
    {
        // Allocated before try, disposed in finally with null check.
        A a = new A(3);
        try
        {
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }
    }

    void M4()
    {
        // Allocated in try, disposed in finally with null check.
        A a = null;
        try
        {
            a = new A(4);
        }
        finally
        {
            if (a != null)
            {
                a.Dispose();
            }
        }
    }

    void M5()
    {
        // Allocated before try, disposed in finally with helper method.
        A a = new A(5);
        try
        {
        }
        finally
        {
            DisposeHelper(a);
        }
    }

    void M6()
    {
        // Allocated in try, disposed in finally with helper method.
        A a = null;
        try
        {
            a = new A(6);
        }
        finally
        {
            DisposeHelper(a);
        }
    }

    void DisposeHelper(IDisposable a)
    {
        if (a != null)
        {
            a.Dispose();
        }
    }

    void M7(bool flag)
    {
        // Allocated before try, disposed in try and assigned to null, disposed in finally with conditional access.
        A a = new A(7);
        try
        {
            if (flag)
            {
                a.Dispose();
                a = null;
            }
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M8(bool flag1, bool flag2)
    {
        // Conditionally allocated in try, disposed in try and assigned to null, disposed in finally with conditional access.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(8);
            }

            if (flag2)
            {
                a.Dispose();
                a = null;
            }
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M9(bool flag)
    {
        // Allocated before try, disposed in catch and all exit points from try, but not in finally.
        A a = new A(9);
        try
        {
            if (flag)
            {
                a.Dispose();
                a = null;
                return;
            }

            a.Dispose();
        }
        catch (Exception ex)
        {
            a?.Dispose();
        }
        finally
        {
        }
    }

    void M10(bool flag1, bool flag2)
    {
        // Conditionally allocated in try, disposed in catch and all exit points from try, but not in finally.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(10);
            }

            if (flag2)
            {
                a?.Dispose();
                return;
            }

            if (a != null)
            {
                a.Dispose();
            }
        }
        catch (Exception ex)
        {
            a?.Dispose();
        }
        finally
        {
        }
    }

    private IDisposable A;
    void M11(bool flag)
    {
        // Allocated before try, escaped or disposed at all exit points from try, and disposed with conditional access in finally.
        A a = new A(9);
        try
        {
            if (flag)
            {
                a.Dispose();
                a = null;
                return;
            }

            this.A = a;     // Escaped.
            a = null;
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M12(bool flag1, bool flag2)
    {
        // Conditionally allocated in try, escaped or disposed at all exit points from try, and disposed with conditional access in finally.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(10);
            }

            if (flag2)
            {
                this.A = a;     // Escaped.
                a = null;
                return;
            }

            if (a != null)
            {
                a.Dispose();
                a = null;
            }
        }
        finally
        {
            a?.Dispose();
        }
    }
    |]
}");
        }

        [Fact]
        public async Task DifferentDisposePatternsInFinally_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;

class A : IDisposable
{
    public A(int i) { }
    public void Dispose()
    {
    }
}

class Test
{
    [|
    void M1(bool flag)
    {
        // Allocated before try, disposed only on some paths in finally with conditional access.
        A a = new A(1);
        try
        {
        }
        finally
        {
            if (flag)
            {
                a?.Dispose();
            }
        }
    }

    void M2(bool flag)
    {
        // Allocated in try, disposed only on some paths in finally with conditional access.
        A a = null;
        try
        {
            a = new A(2);
        }
        finally
        {
            if (flag)
            {
                a?.Dispose();
            }
        }
    }

    void M3(bool flag)
    {
        // Allocated before try, disposed in finally with null checks on different variable.
        // It is not recommended to have dispose logic of a variable depend on multiple variables/flags, as the
        // lifetime of allocations might change when code within the try is refactored.
        A a = null;
        A b = null;
        try
        {
            if (flag)
            {
                a = new A(3);
                b = new A(31);
            }
        }
        finally
        {
            if (b != null)
            {
                a.Dispose();
                b.Dispose();
            }
        }
    }

    void M4(bool flag)
    {
        // Allocated in try, disposed in finally with null checks on multiple variables.
        // It is not recommended to have dispose logic of a variable depend on another variable, as the
        // lifetime of allocations might change when code within the try is refactored.
        A a = null;
        A b = null;
        try
        {
            if (flag)
            {
                a = new A(4);
                b = new A(41);
            }
        }
        finally
        {
            if (a != null && b != null)
            {
                a.Dispose();
                b.Dispose();
            }
        }
    }

    void M5(bool flag)
    {
        // Allocated before try, disposed on some paths in finally with helper method.
        A a = new A(5);
        try
        {
        }
        finally
        {
            DisposeHelper(a, flag);
        }
    }

    void M6(bool flag)
    {
        // Allocated in try, disposed in finally with helper method depending on a bool check.
        // It is not recommended to have dispose logic of a variable depend on another flag, as the
        // lifetime of allocation and flag value might change when code within the try is refactored.
        A a = null;
        try
        {
            if (flag)
            {
                a = new A(6);
            }
        }
        finally
        {
            DisposeHelper(a, flag);
        }
    }

    void DisposeHelper(IDisposable a, bool flag)
    {
        if (flag)
        {
            a?.Dispose();
        }
    }

    void M7(bool flag)
    {
        // Allocated before try, leaked on some paths in try, disposed in finally with conditional access.
        A a = new A(7);
        try
        {
            if (flag)
            {
                a = null;   // Leaked here, but need path sensitive analysis to flag this.
            }
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M8(bool flag1, bool flag2)
    {
        // Conditionally allocated in try, leaked on some paths in try, disposed in finally with conditional access.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(8);
            }

            if (flag2)
            {
                a.Dispose();
                a = null;
            }
            else
            {
                a = null;   // Leaked here, needs path sensitive analysis.
            }
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M9(bool flag)
    {
        // Allocated before try, disposed in catch and but leaked from some exit points in try.
        A a = new A(9);
        try
        {
            if (flag)
            {
                a = null;   // Leaked here.
                return;
            }

            a.Dispose();
        }
        catch (Exception ex)
        {
            a?.Dispose();
        }
        finally
        {
        }
    }

    void M10(bool flag1, bool flag2)
    {
        // Conditionally allocated in try, leaked from some exit points in catch.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(10);
            }

            if (flag2)
            {
                a?.Dispose();
                return;
            }

            if (a != null)
            {
                a.Dispose();
            }
        }
        catch (Exception ex)
        {
            if (flag1)
            {
                a?.Dispose();   // Leaked here, but need enhanced exceptional path dispose analysis to flag this.
            }
        }
        finally
        {
        }
    }

    private IDisposable A;
    void M11(bool flag)
    {
        // Allocated before try, leaked before escaped at some points in try.
        A a = new A(11);
        try
        {
            if (flag)
            {
                a.Dispose();
                a = null;
                return;
            }

            a = null;       // Leaked here.
            this.A = a;     // Escaped has no effect as it is already leaked.
        }
        finally
        {
            a?.Dispose();
        }
    }

    void M12(bool flag1, bool flag2, bool flag3)
    {
        // Conditionally allocated in try, escaped and leaked on separate exit points from try, and disposed with conditional access in finally.
        A a = null;
        try
        {
            if (flag1)
            {
                a = new A(12);
            }

            if (flag2)
            {
                this.A = a;     // Escaped.
                a = null;
                return;
            }
            else if (flag3)
            {
                a = new A(121);   // Previous allocation potentially leaked here, but need path sensitive analysis to flag here.
            }
        }
        finally
        {
            a?.Dispose();
        }
    }
    |]
}",
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(1)").WithLocation(18, 15),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(2)").WithLocation(37, 17),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(3)").WithLocation(59, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(4)").WithLocation(84, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(41)").WithLocation(85, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(5)").WithLocation(101, 15),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(6)").WithLocation(121, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(8)").WithLocation(163, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(9)").WithLocation(185, 15),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "new A(11)").WithLocation(243, 15));
        }

        [Fact, WorkItem(2212, "https://github.com/dotnet/roslyn-analyzers/issues/2212")]
        public async Task ReturnDisposableObjectWrappenInTask_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Threading.Tasks;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public Task<C> M1_Task()
    {
        [|return Task.FromResult(new C());|]
    }
}");
        }

        [Fact, WorkItem(2212, "https://github.com/dotnet/roslyn-analyzers/issues/2212")]
        public async Task AwaitedButNotDisposed_NoDiagnostic()
        {
            // We are conservative when disposable object gets wrapped in a task and consider it as escaped.
            await TestDiagnosticMissingAsync(@"
using System;
using System.Threading.Tasks;

class C : IDisposable
{
    public void Dispose()
    {
    }

    [|
    public Task<C> M1_Task()
    {
        return Task.FromResult(new C());
    }

    public async Task M2_Task()
    {
        var c = await M1_Task().ConfigureAwait(false);
    }
    |]
}");
        }

        [Fact, WorkItem(2212, "https://github.com/dotnet/roslyn-analyzers/issues/2212")]
        public async Task AwaitedButNotDisposed_TaskWrappingField_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Threading.Tasks;

class C : IDisposable
{
    private C _c;
    public void Dispose()
    {
    }

    [|
    public Task<C> M1_Task()
    {
        return Task.FromResult(_c);
    }

    public async Task M2_Task()
    {
        var c = await M1_Task().ConfigureAwait(false);
    }
    |]
}");
        }

        [Fact, WorkItem(2347, "https://github.com/dotnet/roslyn-analyzers/issues/2347")]
        public async Task ReturnDisposableObjectInAsyncMethod_DisposedInCaller_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Threading.Tasks;

class C : IDisposable
{
    public void Dispose()
    {
    }

    [|
    public async Task<C> M1_Task(object context)
    {
        await Task.Yield();
        return new C();
    }

    public async Task M2_Task()
    {
        var c = await M1_Task(null).ConfigureAwait(false);
        c.Dispose();
    }
    |]
}");
        }

        [Fact, WorkItem(2347, "https://github.com/dotnet/roslyn-analyzers/issues/2347")]
        public async Task ReturnDisposableObjectInAsyncMethod_NotDisposedInCaller_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System;
using System.Threading.Tasks;

class C : IDisposable
{
    public void Dispose()
    {
    }

    [|
    public async Task<C> M1_Task(object context)
    {
        await Task.Yield();
        return new C();
    }

    public async Task M2_Task()
    {
        var c = await M1_Task(null).ConfigureAwait(false);
    }
    |]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "M1_Task(null)").WithLocation(20, 23));
        }

        [Fact, WorkItem(2361, "https://github.com/dotnet/roslyn-analyzers/issues/2361")]
        public async Task ExpressionBodiedMethod_ReturnsDisposableObject_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync(@"
using System.IO;

class C
{
    [|Stream M() => File.OpenRead(""C:/somewhere/"");|]
}");
        }

        [Fact, WorkItem(2361, "https://github.com/dotnet/roslyn-analyzers/issues/2361")]
        public async Task ReturnsDisposableObject_NotDisposed_Diagnostic()
        {
            await TestDiagnosticsAsync(@"
using System.IO;

class C
{
    [|
    Stream GetStream() => File.OpenRead(""C:/somewhere/"");

    void M2()
    {
        var stream = GetStream();
    }
    |]
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "GetStream()").WithLocation(11, 22));
        }

        [Fact, WorkItem(2497, "https://github.com/dotnet/roslyn-analyzers/issues/2497")]
        public async Task UsingStatementInCatch()
        {
            await TestDiagnosticsAsync(@"
using System;
class C : IDisposable
{
    public void Dispose() { }
    void M1()
    {
        try
        {
        }
        catch (Exception)
        {
            [|using (var c = new C())|]
            {
            }
        }
    }
}");
        }

        [Fact, WorkItem(2497, "https://github.com/dotnet/roslyn-analyzers/issues/2497")]
        public async Task TryFinallyStatementInCatch()
        {
            await TestDiagnosticsAsync(@"
using System;
class C : IDisposable
{
    public void Dispose() { }
    void M1()
    {
        try
        {
        }
        catch (Exception)
        {
            C c = null;
            try
            {
                [|c = new C();|]
            }
            finally
            {
                c.Dispose();
            }
        }
    }
}");
        }

        [Fact, WorkItem(2497, "https://github.com/dotnet/roslyn-analyzers/issues/2497")]
        public async Task UsingStatementInFinally()
        {
            await TestDiagnosticsAsync(@"
using System;
class C : IDisposable
{
    public void Dispose() { }
    void M1()
    {
        try
        {
        }
        finally
        {
            [|using (var c = new C())|]
            {
            }
        }
    }
}");
        }

        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public async Task UsingDeclaration()
        {
            await TestDiagnosticsAsync(@"
using System;
class C : IDisposable
{
    public void Dispose() { }
    void M1()
    {
        [|using var c = new C()|];
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        }

        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public async Task UsingDeclarationWithInitializer()
        {
            await TestDiagnosticsAsync(@"
using System;
class C : IDisposable
{
    public int P { get; set; }
    public void Dispose() { }
    void M1()
    {
        [|using var c = new C() { P = 1 }|];
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        }

        [Fact]
        public async Task MissingDisposeInMethodWithAttributes()
        {
            await TestDiagnosticsAsync(@"
using System;
class C : IDisposable
{
    public void Dispose() { }

    [Obsolete()]
    void M1()
    {
        var c = [|new C()|];
    }
}",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId));
        }

        [Fact, WorkItem(36498, "https://github.com/dotnet/roslyn/issues/36498")]
        public async Task DisposableObjectPushedToStackIsNotFlagged()
        {
            await TestDiagnosticMissingAsync(@"
using System;
using System.Collections.Generic;

class C : IDisposable
{
    public void Dispose() { }

    public void M1(Stack<object> stack)
    {
        var c = [|new C()|];
        stack.Push(c);
    }
}");
        }
    }
}
