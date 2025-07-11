// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpSecurityCodeFixVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpDoNotCopyValue,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicSecurityCodeFixVerifier<
    Roslyn.Diagnostics.VisualBasic.Analyzers.VisualBasicDoNotCopyValue,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class DoNotCopyValueTests
    {
        [Fact]
        public Task TestSliceOfStringAsync()
            => new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9,
                TestCode = """
                using System.Runtime.InteropServices;

                class C
                {
                    void M()
                    {
                        var local = ""[..];
                        local = ""[..];
                    }
                }
                """,
            }.RunAsync();

        [Fact]
        public async Task TestAcquireFromReturnByValueAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                using System.Runtime.InteropServices;

                class C
                {
                    void M()
                    {
                        var local = GCHandle.Alloc(new object());
                    }
                }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports System.Runtime.InteropServices

                Class C
                    Sub M()
                        Dim local = GCHandle.Alloc(New Object())
                    End Sub
                End Class
                """);
        }

        [Theory]
        [InlineData("Func<ValueTask<GCHandle>>", "Func(Of ValueTask(Of GCHandle))")]
        [InlineData("Func<ConfiguredValueTaskAwaitable<GCHandle>>", "Func(Of ConfiguredValueTaskAwaitable(Of GCHandle))")]
        [InlineData("Func<Task<GCHandle>>", "Func(Of Task(Of GCHandle))")]
        [InlineData("Func<ConfiguredTaskAwaitable<GCHandle>>", "Func(Of ConfiguredTaskAwaitable(Of GCHandle))")]
        public async Task TestAcquireFromAwaitInvocationAsync(string csharpInvokeType, string visualBasicInvokeType)
        {
            await VerifyCS.VerifyAnalyzerAsync($$"""
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Threading.Tasks;

                class C
                {
                    async Task M({{csharpInvokeType}} d)
                    {
                        var local = await d();
                        local = await d();
                    }
                }
                """);

            await VerifyVB.VerifyAnalyzerAsync($"""
                Imports System
                Imports System.Runtime.CompilerServices
                Imports System.Runtime.InteropServices
                Imports System.Threading.Tasks

                Class C
                    Async Function M(d as {visualBasicInvokeType}) As Task
                        Dim local = Await d()
                        local = Await d()
                    End Function
                End Class
                """);
        }

        [Theory]
        [InlineData("ValueTask<GCHandle>", "ValueTask(Of GCHandle)")]
        [InlineData("ConfiguredValueTaskAwaitable<GCHandle>", "ConfiguredValueTaskAwaitable(Of GCHandle)")]
        public async Task TestAcquireFromAwaitAsync(string csharpAwaitableType, string visualBasicAwaitableType)
        {
            await VerifyCS.VerifyAnalyzerAsync($$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Threading.Tasks;

                class C
                {
                    async Task M({{csharpAwaitableType}} task)
                    {
                        var local = await task;
                    }
                }
                """);

            await VerifyVB.VerifyAnalyzerAsync($"""
                Imports System.Runtime.CompilerServices
                Imports System.Runtime.InteropServices
                Imports System.Threading.Tasks

                Class C
                    Async Function M(task as {visualBasicAwaitableType}) As Task
                        Dim local = Await task
                    End Function
                End Class
                """);
        }

        [Theory]
        [InlineData("Task<GCHandle>", "Task(Of GCHandle)")]
        [InlineData("ConfiguredTaskAwaitable<GCHandle>", "ConfiguredTaskAwaitable(Of GCHandle)")]
        public async Task TestFailedAcquireFromUnsupportedAwaitAsync(string csharpAwaitableType, string visualBasicAwaitableType)
        {
            await VerifyCS.VerifyAnalyzerAsync($$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Threading.Tasks;

                class C
                {
                    async Task M({{csharpAwaitableType}} task)
                    {
                        var local {|#0:= await task|};
                    }
                }
                """,
                // /0/Test0.cs(10,19): warning RS0042: Cannot assign a value from a reference to non-copyable type 'System.Runtime.InteropServices.GCHandle'
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.NoAssignValueFromReferenceRule).WithLocation(0).WithArguments("System.Runtime.InteropServices.GCHandle"));

            await VerifyVB.VerifyAnalyzerAsync($$"""
                Imports System.Runtime.CompilerServices
                Imports System.Runtime.InteropServices
                Imports System.Threading.Tasks

                Class C
                    Async Function M(task as {{visualBasicAwaitableType}}) As Task
                        Dim local {|#0:= Await task|}
                    End Function
                End Class
                """,
                // /0/Test0.vb(8,19): warning RS0042: Cannot assign a value from a reference to non-copyable type 'System.Runtime.InteropServices.GCHandle'
                VerifyVB.Diagnostic(AbstractDoNotCopyValue.NoAssignValueFromReferenceRule).WithLocation(0).WithArguments("System.Runtime.InteropServices.GCHandle"));
        }

        [Theory]
        [InlineData("field", "field")]
        [InlineData("(field)", null)]
        [InlineData("this.field", "Me.field")]
        [InlineData("(this).field", null)]
        [InlineData("((C)this).field", "DirectCast(Me, C).field")]
        public async Task TestAcquireIntoFieldFromReturnByValueAsync(string csharpFieldReference, string? visualBasicFieldReference)
        {
            await VerifyCS.VerifyAnalyzerAsync($$"""
                using System.Runtime.InteropServices;

                class C
                {
                    GCHandle field;

                    void M(bool condition)
                    {
                        {{csharpFieldReference}} = GCHandle.Alloc(new object());
                        {{csharpFieldReference}} = condition ? GCHandle.Alloc(new object()) : GCHandle.Alloc(new object());
                    }
                }
                """);

            if (visualBasicFieldReference is object)
            {
                await VerifyVB.VerifyAnalyzerAsync($"""
                    Imports System.Runtime.InteropServices

                    Class C
                        Dim field As GCHandle

                        Sub M(condition As Boolean)
                            {visualBasicFieldReference} = GCHandle.Alloc(New Object())
                            {visualBasicFieldReference} = If(condition, GCHandle.Alloc(New Object()), GCHandle.Alloc(New Object()))
                        End Sub
                    End Class
                    """);
            }
        }

        [Theory]
        [InlineData("field", "field")]
        [InlineData("(field)", null)]
        [InlineData("this.field", "Me.field")]
        [InlineData("(this).field", null)]
        [InlineData("((C)this).field", "DirectCast(Me, C).field")]
        public async Task TestAcquireIntoArrayFieldFromReturnByValueAsync(string csharpFieldReference, string? visualBasicFieldReference)
        {
            await VerifyCS.VerifyAnalyzerAsync($$"""
                using System.Runtime.InteropServices;

                class C
                {
                    GCHandle[] field;

                    void M()
                    {
                        {{csharpFieldReference}}[0] = GCHandle.Alloc(new object());
                    }
                }
                """);

            if (visualBasicFieldReference is object)
            {
                await VerifyVB.VerifyAnalyzerAsync($"""
                    Imports System.Runtime.InteropServices

                    Class C
                        Dim field As GCHandle()

                        Sub M()
                            {visualBasicFieldReference}(0) = GCHandle.Alloc(New Object())
                        End Sub
                    End Class
                    """);
            }
        }

        [Fact]
        public Task TestDoNotAcquireFromReturnByReferenceAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
                using System.Runtime.InteropServices;

                class C
                {
                    void M()
                    {
                        var local {|#0:= GetRef()|};
                    }

                    ref GCHandle GetRef() => throw null;
                }
                """,
                // /0/Test0.cs(8,19): warning RS0042: Cannot assign a value from a reference to non-copyable type 'System.Runtime.InteropServices.GCHandle'
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.NoAssignValueFromReferenceRule).WithLocation(0).WithArguments("System.Runtime.InteropServices.GCHandle"));

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/77925")]
        public Task TestPassToInstancePropertyGetterAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
                using System.Runtime.InteropServices;

                class C
                {
                    GCHandle field;
                    readonly GCHandle readonlyfield;

                    void M()
                    {
                        var local = new GCHandle();
                        ref var reflocal = ref local;
                        ref readonly var refreadonlylocal = ref local;

                        // Call each proprety twice to ensure the analyzer didn't silently treat one like a move
                        _ = field.Target;
                        _ = field.Target;
                        _ = local.Target;
                        _ = local.Target;
                        _ = reflocal.Target;
                        _ = reflocal.Target;

                        _ = {|#0:readonlyfield|}.Target;
                        _ = {|#1:refreadonlylocal|}.Target;
                    }
                }
                """,
                // /0/Test0.cs(23,13): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'FieldReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(0).WithArguments("System.Runtime.InteropServices.GCHandle", "FieldReference"),
                // /0/Test0.cs(17,13): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(1).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"));

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/77925")]
        public Task TestPassToInstanceMethodAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
                using System.Runtime.InteropServices;

                class C
                {
                    GCHandle field;
                    readonly GCHandle readonlyfield;

                    void M()
                    {
                        var local = new GCHandle();
                        ref var reflocal = ref local;
                        ref readonly var refreadonlylocal = ref local;

                        // Call each method twice to ensure the analyzer didn't silently treat one like a move
                        _ = field.AddrOfPinnedObject();
                        _ = field.AddrOfPinnedObject();
                        _ = local.AddrOfPinnedObject();
                        _ = local.AddrOfPinnedObject();
                        _ = reflocal.AddrOfPinnedObject();
                        _ = reflocal.AddrOfPinnedObject();

                        _ = {|#0:readonlyfield|}.AddrOfPinnedObject();
                        _ = {|#1:refreadonlylocal|}.AddrOfPinnedObject();
                    }
                }
                """,
                // /0/Test0.cs(23,13): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'FieldReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(0).WithArguments("System.Runtime.InteropServices.GCHandle", "FieldReference"),
                // /0/Test0.cs(17,13): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(1).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"));

        [Fact]
        public Task TestPassToExtensionMethodAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
                using System.Runtime.InteropServices;

                class C
                {
                    GCHandle field;
                    readonly GCHandle readonlyfield;

                    void M()
                    {
                        var local = new GCHandle();
                        ref var reflocal = ref local;
                        ref readonly var refreadonlylocal = ref local;

                        // Success cases. Call each method twice to ensure the analyzer didn't silently treat one like a move.
                        field.XRef();
                        field.XRef();
                        local.XRef();
                        local.XRef();
                        reflocal.XRef();
                        reflocal.XRef();

                        readonlyfield.XIn();
                        readonlyfield.XIn();
                        reflocal.XIn();
                        reflocal.XIn();
                        local.XIn();
                        local.XIn();

                        // Failure cases.
                        {|#0:{|CS0192:readonlyfield|}|}.XRef();
                        {|#1:{|CS1510:refreadonlylocal|}|}.XRef();
                        {|#2:field|}.X();
                        {|#3:readonlyfield|}.X();
                        {|#4:local|}.X();
                        {|#5:reflocal|}.X();
                        {|#6:refreadonlylocal|}.X();
                    }
                }

                static class E
                {
                    public static void X(this GCHandle handle) { }
                    public static void XRef(this ref GCHandle handle) { }
                    public static void XIn(this in GCHandle handle) { }
                }
                """,
                // /0/Test0.cs(31,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'FieldReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(0).WithArguments("System.Runtime.InteropServices.GCHandle", "FieldReference"),
                // /0/Test0.cs(32,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(1).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"),
                // /0/Test0.cs(33,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'FieldReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(2).WithArguments("System.Runtime.InteropServices.GCHandle", "FieldReference"),
                // /0/Test0.cs(34,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'FieldReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(3).WithArguments("System.Runtime.InteropServices.GCHandle", "FieldReference"),
                // /0/Test0.cs(35,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(4).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"),
                // /0/Test0.cs(36,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(5).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"),
                // /0/Test0.cs(37,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(6).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"));

        [Theory]
        [InlineData("throw null")]
        [InlineData("(true ? throw null : default(GCHandle))")]
        [InlineData("(false ? new GCHandle() : throw null)")]
        public Task TestConversionFromThrowNullAsync(string throwExpression)
            => VerifyCS.VerifyAnalyzerAsync($$"""
                using System.Runtime.InteropServices;

                class C
                {
                    GCHandle Get() => {{throwExpression}};
                }
                """);

        [Fact]
        public async Task TestPassByReferenceAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                using System.Runtime.InteropServices;

                class C
                {
                    void Get(ref GCHandle handle) => Get(ref handle);
                }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports System.Runtime.InteropServices

                Class C
                    Sub Method(ByRef handle As GCHandle)
                        Method(handle)
                    End Sub
                End Class
                """);
        }

        [Fact]
        public Task TestPassByReadOnlyReferenceAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
                using System.Runtime.InteropServices;

                class C
                {
                    void Get(in GCHandle handle) => Get(in handle);
                }
                """);

        [Fact]
        public Task TestAssignToMemberAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
                using System.Runtime.InteropServices;

                class C
                {
                    CannotCopy _field;

                    void Method(CannotCopy parameter)
                    {
                        CannotCopy local = new CannotCopy();

                        _field.Field = 0;
                        parameter.Field = 0;
                        local.Field = 0;

                        _field.Property = 0;
                        parameter.Property = 0;
                        local.Property = 0;
                    }
                }

                [NonCopyable]
                struct CannotCopy
                {
                    public int Field;
                    public int Property { get; set; }
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """);

        [Fact]
        public Task ReturnLocalByValueAsync()
            => new VerifyCS.Test
            {
                TestCode = """
                using System.Runtime.InteropServices;

                class C
                {
                    GCHandle Method()
                    {
                        GCHandle handle = default;
                        return handle;
                    }
                }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Fact]
        public Task TestReturnMemberAsync()
            => new VerifyCS.Test
            {
                TestCode = """
                using System.Runtime.InteropServices;

                class C
                {
                    CannotCopy _field;
                    readonly CannotCopy _readonlyField;

                    int ReturnFieldMemberField()
                    {
                        return _field.Field;
                    }

                    int ReturnReadonlyFieldMemberField()
                    {
                        return _readonlyField.Field;
                    }

                    int ReturnParameterMemberField(CannotCopy parameter)
                    {
                        return parameter.Field;
                    }

                    int ReturnArrayParameterMemberField(CannotCopy[] parameter)
                    {
                        return parameter[0].Field;
                    }

                    int ReturnLocalMemberField()
                    {
                        CannotCopy local = new CannotCopy();
                        return local.Field;
                    }

                    int ReturnFieldMemberProperty()
                    {
                        return _field.Property;
                    }

                    int ReturnReadonlyFieldMemberProperty()
                    {
                        return {|#0:_readonlyField|}.Property;
                    }

                    int ReturnParameterMemberProperty(CannotCopy parameter)
                    {
                        return parameter.Property;
                    }

                    int ReturnArrayParameterMemberProperty(CannotCopy[] parameter)
                    {
                        return parameter[0].Property;
                    }

                    int ReturnLocalMemberProperty()
                    {
                        CannotCopy local = new CannotCopy();
                        return local.Property;
                    }

                    int ReturnFieldMemberReadonlyProperty()
                    {
                        return _field.ReadonlyProperty;
                    }

                    int ReturnReadonlyFieldMemberReadonlyProperty()
                    {
                        return _readonlyField.ReadonlyProperty;
                    }

                    int ReturnParameterMemberReadonlyProperty(CannotCopy parameter)
                    {
                        return parameter.ReadonlyProperty;
                    }

                    int ReturnArrayParameterMemberReadonlyProperty(CannotCopy[] parameter)
                    {
                        return parameter[0].ReadonlyProperty;
                    }

                    int ReturnLocalMemberReadonlyProperty()
                    {
                        CannotCopy local = new CannotCopy();
                        return local.ReadonlyProperty;
                    }
                }

                [NonCopyable]
                struct CannotCopy
                {
                    public int Field;
                    public int Property { get { return 0; } }
                    public int ReadonlyProperty { readonly get { return 0; } set { } }
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                ExpectedDiagnostics =
                {
                    // The only reported diagnostic occurs for the invocation of a non-readonly getter of a readonly
                    // non-copyable field.
                    //
                    // /0/Test0.cs(42,16): warning RS0042: Unsupported use of non-copyable type 'CannotCopy' in 'FieldReference' operation
                    VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(0).WithArguments("CannotCopy", "FieldReference"),
                },
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Fact]
        public Task NonReadonlyMemberPropertiesAsync()
            => new VerifyCS.Test
            {
                TestCode = """
                using System.Runtime.InteropServices;

                [NonCopyable]
                struct CannotCopy
                {
                    public int First { get { return 0; } }
                    public int Second { get { return First; } }
                    public int Third => First;
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Fact]
        public Task NonReadonlyMemberMethodsAsync()
            => new VerifyCS.Test
            {
                TestCode = """
                using System.Runtime.InteropServices;

                [NonCopyable]
                struct CannotCopy
                {
                    public int First() { return 0; }
                    public int Second() { return First(); }
                    public int Third() => First();
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Fact]
        public Task AllowObjectInitializerAsync()
            => new VerifyCS.Test
            {
                TestCode = """
                using System.Runtime.InteropServices;

                class C
                {
                    CannotCopy Method()
                    {
                        return new CannotCopy() { First = 0, Second = 1 };
                    }
                }

                [NonCopyable]
                struct CannotCopy
                {
                    public int First { get; set; }
                    public int Second { get; set; }
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Fact]
        public Task AllowCustomForeachEnumeratorAsync()
            => new VerifyCS.Test
            {
                TestCode = """
                using System.Runtime.InteropServices;

                class C
                {
                    void Method()
                    {
                        var cannotCopy = new CannotCopy();
                        foreach (var obj in cannotCopy)
                        {
                        }
                    }
                }

                [NonCopyable]
                struct CannotCopy
                {
                    public Enumerator GetEnumerator() => throw null;

                    public struct Enumerator
                    {
                        public object Current => throw null;
                        public bool MoveNext() => throw null;
                    }
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Theory]
        [CombinatorialData]
        public async Task AllowCustomForeachEnumeratorParameterReferenceAsync(
            [CombinatorialValues("", "ref", "in")] string parameterModifiers,
            [CombinatorialValues("", "readonly")] string getEnumeratorModifiers)
        {
            var expected = (parameterModifiers, getEnumeratorModifiers) switch
            {
                // /0/Test0.cs(8,29): warning RS0042: Unsupported use of non-copyable type 'CannotCopy' in 'ParameterReference' operation
                ("in", "") => [VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(0).WithArguments("CannotCopy", "ParameterReference")],

                _ => DiagnosticResult.EmptyDiagnosticResults,
            };

            var test = new VerifyCS.Test
            {
                TestCode = $$"""
                using System.Runtime.InteropServices;

                class C
                {
                    void Method({{parameterModifiers}} CannotCopy cannotCopy)
                    {
                        foreach (var obj in {|#0:cannotCopy|})
                        {
                        }
                    }
                }

                [NonCopyable]
                struct CannotCopy
                {
                    public {{getEnumeratorModifiers}} Enumerator GetEnumerator() => throw null;

                    public struct Enumerator
                    {
                        public object Current => throw null;
                        public bool MoveNext() => throw null;
                    }
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        [Fact]
        public Task AllowCustomForeachEnumeratorDisposableObject1Async()
            => new VerifyCS.Test
            {
                TestCode = """
                using System;
                using System.Runtime.InteropServices;

                class C
                {
                    void Method()
                    {
                        using var cannotCopy = new CannotCopy();
                        foreach (var obj in cannotCopy)
                        {
                        }
                    }
                }

                [NonCopyable]
                struct CannotCopy : IDisposable
                {
                    public void Dispose() => throw null;
                    public Enumerator GetEnumerator() => throw null;

                    public struct Enumerator
                    {
                        public object Current => throw null;
                        public bool MoveNext() => throw null;
                    }
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Fact]
        public Task AllowCustomForeachEnumeratorDisposableObject2Async()
            => new VerifyCS.Test
            {
                TestCode = """
                using System;
                using System.Runtime.InteropServices;

                class C
                {
                    void Method()
                    {
                        using (var cannotCopy = new CannotCopy())
                        {
                            foreach (var obj in cannotCopy)
                            {
                            }
                        }
                    }
                }

                [NonCopyable]
                struct CannotCopy : IDisposable
                {
                    public void Dispose() => throw null;
                    public Enumerator GetEnumerator() => throw null;

                    public struct Enumerator
                    {
                        public object Current => throw null;
                        public bool MoveNext() => throw null;
                    }
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Theory]
        [InlineData("new CannotCopy()")]
        [InlineData("default(CannotCopy)")]
        [InlineData("CannotCopy.Create()")]
        [InlineData("CannotCopy.Empty")]
        public Task AllowDisposableObjectAsync(string creation)
            => new VerifyCS.Test
            {
                TestCode = $$"""
                using System;
                using System.Runtime.InteropServices;

                class C
                {
                    void UsingStatement()
                    {
                        using ({{creation}})
                        {
                        }
                    }

                    void UsingStatementWithVariable()
                    {
                        using (var cannotCopy = {{creation}})
                        {
                        }
                    }

                    void UsingStatementWithDiscard()
                    {
                        using (_ = {{creation}})
                        {
                        }
                    }

                    void UsingDeclarationStatement()
                    {
                        using var cannotCopy = {{creation}};
                    }
                }

                [NonCopyable]
                struct CannotCopy : IDisposable
                {
                    public static CannotCopy Empty => throw null;
                    public static CannotCopy Create() => throw null;

                    public void Dispose() => throw null;
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Fact]
        public Task AllowCustomForeachReadonlyEnumeratorAsync()
            => new VerifyCS.Test
            {
                TestCode = """
                using System.Runtime.InteropServices;

                class C
                {
                    void Method()
                    {
                        var cannotCopy = new CannotCopy();
                        foreach (var obj in cannotCopy)
                        {
                        }
                    }
                }

                [NonCopyable]
                struct CannotCopy
                {
                    public readonly Enumerator GetEnumerator() => throw null;

                    public struct Enumerator
                    {
                        public object Current => throw null;
                        public bool MoveNext() => throw null;
                    }
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Theory]
        [InlineData("")]
        [InlineData("ref")]
        [InlineData("in")]
        public Task AllowNameOfParameterReferenceAsync(string parameterModifiers)
            => new VerifyCS.Test
            {
                TestCode = $$"""
                using System.Runtime.InteropServices;

                class C
                {
                    void Method({{parameterModifiers}} CannotCopy value)
                    {
                        _ = nameof(CannotCopy);
                        _ = nameof(value);
                        _ = nameof(value.ToString);
                    }
                }

                [NonCopyable]
                struct CannotCopy
                {
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Theory]
        [InlineData("ref")]
        [InlineData("in")]
        public Task AllowUnsafeAsRefParameterReferenceAsync(string parameterModifiers)
            => new VerifyCS.Test
            {
                TestCode = $$"""
                using System.Runtime.InteropServices;

                class C
                {
                    ref CannotCopy Method({{parameterModifiers}} CannotCopy cannotCopy)
                    {
                        return ref AsRef(in cannotCopy);
                    }

                    ref T AsRef<T>(in T value)
                        => throw null;
                }

                [NonCopyable]
                struct CannotCopy
                {
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Theory]
        [CombinatorialData]
        public Task AllowUnsafeAsRefParameterReference2Async(
            [CombinatorialValues("ref", "in", "ref readonly")] string parameterModifiers,
            [CombinatorialValues("in", "scoped in", "ref readonly", "scoped ref readonly")] string asRefParameterModifiers)
            => new VerifyCS.Test
            {
                TestCode = $$"""
                using System.Runtime.InteropServices;

                class C
                {
                    ref CannotCopy Method({{parameterModifiers}} CannotCopy cannotCopy)
                    {
                        return ref AsRef(in cannotCopy);
                    }

                    ref T AsRef<T>({{asRefParameterModifiers}} T value)
                        => throw null;
                }

                [NonCopyable]
                struct CannotCopy
                {
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();

        [Theory]
        [InlineData("ref")]
        [InlineData("in")]
        public async Task StoreUnsafeAsRefParameterReferenceToLocalAsync(string parameterModifiers)
        {
            var localModifiers = parameterModifiers switch
            {
                "in" => "ref readonly",
                _ => parameterModifiers,
            };
            await new VerifyCS.Test
            {
                TestCode = $$"""
                using System.Runtime.InteropServices;

                class C
                {
                    void Method({{parameterModifiers}} CannotCopy cannotCopy)
                    {
                        {{localModifiers}} var local = ref AsRef(in cannotCopy);

                        local = ref AsRef(in cannotCopy);
                    }

                    ref T AsRef<T>(in T value)
                        => throw null;
                }

                [NonCopyable]
                struct CannotCopy
                {
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public Task CannotStoreRefReturnByValueAsync()
            => new VerifyCS.Test
            {
                TestCode = $$"""
                using System.Runtime.InteropServices;

                class C
                {
                    void Method(in CannotCopy cannotCopy)
                    {
                        // Test with initializer
                        var local {|#0:= AsRef(in cannotCopy)|};

                        // Test with assignment to local
                        local = {|#1:AsRef(in cannotCopy)|};

                        // Implicit and explicit discard is acceptable
                        AsRef(in cannotCopy);
                        _ = AsRef(in cannotCopy);
                    }

                    ref T AsRef<T>(in T value)
                        => throw null;
                }

                [NonCopyable]
                struct CannotCopy
                {
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(8,19): warning RS0042: Cannot assign a value from a reference to non-copyable type 'CannotCopy'
                    VerifyCS.Diagnostic(AbstractDoNotCopyValue.NoAssignValueFromReferenceRule).WithLocation(0).WithArguments("CannotCopy"),
                    // /0/Test0.cs(11,17): warning RS0042: Cannot assign a value from a reference to non-copyable type 'CannotCopy'
                    VerifyCS.Diagnostic(AbstractDoNotCopyValue.NoAssignValueFromReferenceRule).WithLocation(1).WithArguments("CannotCopy"),
                },
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Fact]
        public Task CannotReturnRefReturnByValueAsync()
            => new VerifyCS.Test
            {
                TestCode = $$"""
                using System.Runtime.InteropServices;

                class C
                {
                    CannotCopy Method(in CannotCopy cannotCopy)
                    {
                        return {|#0:AsRef(in cannotCopy)|};
                    }

                    ref T AsRef<T>(in T value)
                        => throw null;
                }

                [NonCopyable]
                struct CannotCopy
                {
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(8,16): warning RS0042: Cannot return a value from a reference to non-copyable type 'CannotCopy'
                    VerifyCS.Diagnostic(AbstractDoNotCopyValue.NoReturnValueFromReferenceRule).WithLocation(0).WithArguments("CannotCopy"),
                },
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();

        [Fact]
        public Task TestNonCopyableAttributeAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
                using System.Runtime.InteropServices;

                class C
                {
                    CanCopy _canCopy;
                    CannotCopy _cannotCopy;

                    void Method(object value)
                    {
                        Method(_canCopy);
                        Method({|#0:_cannotCopy|});
                    }
                }

                struct CanCopy
                {
                }

                [NonCopyable]
                struct CannotCopy
                {
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                // /0/Test0.cs(12,16): warning RS0042: Unsupported use of non-copyable type 'CannotCopy' in 'FieldReference' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(0).WithArguments("CannotCopy", "FieldReference"));

        [Fact]
        public Task DoNotWrapNonCopyableTypeInNullableTAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
                using System.Runtime.InteropServices;

                class C
                {
                    GCHandle? field {|#0:= null|};
                }
                """,
                // /0/Test0.cs(6,21): warning RS0042: Do not wrap non-copyable type 'System.Runtime.InteropServices.GCHandle?' in 'FieldInitializer' operation
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.AvoidNullableWrapperRule).WithLocation(0).WithArguments("System.Runtime.InteropServices.GCHandle?", "FieldInitializer"));

        [Fact]
        public async Task DoNotDefineNonCopyableFieldInCopyableTypeAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                class C1
                {
                    CanCopy field1;
                    CannotCopy field2;
                }

                struct C2
                {
                    CanCopy field1;
                    CannotCopy {|#0:field2|};
                }

                [NonCopyable]
                struct C3
                {
                    CanCopy field1;
                    CannotCopy field2;
                }

                struct CanCopy { }
                [NonCopyable] struct CannotCopy { }
                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                // /0/Test0.cs(11,16): warning RS0042: Copyable field 'C2.field2' cannot have non-copyable type 'CannotCopy'
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.NoFieldOfCopyableTypeRule).WithLocation(0).WithArguments("CannotCopy", "C2.field2"));

            await VerifyVB.VerifyAnalyzerAsync("""
                Class C1
                    Private field1 As CanCopy
                    Private field2 As CannotCopy
                End Class

                Structure C2
                    Private field1 As CanCopy
                    Private {|#0:field2|} As CannotCopy
                End Structure

                <NonCopyable>
                Structure C3
                    Private field1 As CanCopy
                    Private field2 As CannotCopy
                End Structure

                Structure CanCopy : End Structure
                <NonCopyable> Structure CannotCopy : End Structure
                Public NotInheritable Class NonCopyableAttribute : Inherits System.Attribute : End Class
                """,
                // /0/Test0.vb(9,13): warning RS0042: Copyable field 'Private field2 As CannotCopy' cannot have non-copyable type 'CannotCopy'
                VerifyVB.Diagnostic(AbstractDoNotCopyValue.NoFieldOfCopyableTypeRule).WithLocation(0).WithArguments("CannotCopy", "Private field2 As CannotCopy"));
        }

        [Fact]
        public async Task DoNotDefineNonCopyableAutoPropertyAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                class C1
                {
                    CanCopy Property1 { get; set; }
                    CanCopy Property2 { get => throw null; set => throw null; }
                    CannotCopy {|#0:Property3|} { get; set; }
                    CannotCopy Property4 { get => throw null; set => throw null; }
                }

                struct C2
                {
                    CanCopy Property1 { get; set; }
                    CanCopy Property2 { get => throw null; set => throw null; }
                    CannotCopy {|#1:Property3|} { get; set; }
                    CannotCopy Property4 { get => throw null; set => throw null; }
                }

                struct CanCopy { }
                [NonCopyable] struct CannotCopy { }
                internal sealed class NonCopyableAttribute : System.Attribute { }
                """,
                // /0/Test0.cs(6,16): warning RS0042: Auto-property 'C1.Property3' cannot have non-copyable type 'CannotCopy'
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.NoAutoPropertyRule).WithLocation(0).WithArguments("CannotCopy", "C1.Property3"),
                // /0/Test0.cs(14,16): warning RS0042: Auto-property 'C2.Property3' cannot have non-copyable type 'CannotCopy'
                VerifyCS.Diagnostic(AbstractDoNotCopyValue.NoAutoPropertyRule).WithLocation(1).WithArguments("CannotCopy", "C2.Property3"));

            await VerifyVB.VerifyAnalyzerAsync("""
                Class C1
                    Private Property Property1 As CanCopy

                    Private Property Property2 As CanCopy
                        Get
                            Throw DirectCast(Nothing, System.Exception)
                        End Get
                        Set
                            Throw DirectCast(Nothing, System.Exception)
                        End Set
                    End Property

                    Private Property {|#0:Property3|} As CannotCopy

                    Private Property Property4 As CannotCopy
                        Get
                            Throw DirectCast(Nothing, System.Exception)
                        End Get
                        Set
                            Throw DirectCast(Nothing, System.Exception)
                        End Set
                    End Property
                End Class

                Structure C2
                    Private Property Property1 As CanCopy

                    Private Property Property2 As CanCopy
                        Get
                            Throw DirectCast(Nothing, System.Exception)
                        End Get
                        Set
                            Throw DirectCast(Nothing, System.Exception)
                        End Set
                    End Property

                    Private Property {|#1:Property3|} As CannotCopy

                    Private Property Property4 As CannotCopy
                        Get
                            Throw DirectCast(Nothing, System.Exception)
                        End Get
                        Set
                            Throw DirectCast(Nothing, System.Exception)
                        End Set
                    End Property
                End Structure

                Structure CanCopy : End Structure
                <NonCopyable> Structure CannotCopy : End Structure
                Public NotInheritable Class NonCopyableAttribute : Inherits System.Attribute : End Class
                """,
                // /0/Test0.vb(14,22): warning RS0042: Auto-property 'Private Property Property3 As CannotCopy' cannot have non-copyable type 'CannotCopy'
                VerifyVB.Diagnostic(AbstractDoNotCopyValue.NoAutoPropertyRule).WithLocation(0).WithArguments("CannotCopy", "Private Property Property3 As CannotCopy"),
                // /0/Test0.vb(38,22): warning RS0042: Auto-property 'Private Property Property3 As CannotCopy' cannot have non-copyable type 'CannotCopy'
                VerifyVB.Diagnostic(AbstractDoNotCopyValue.NoAutoPropertyRule).WithLocation(1).WithArguments("CannotCopy", "Private Property Property3 As CannotCopy"));
        }

        [Fact]
        public Task AllowCopyFromCollectionExpression()
            => new VerifyCS.Test
            {
                TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;

                [NonCopyable]
                [CollectionBuilder(typeof(MyCollection), nameof(Create))]
                partial struct MyCollection : IEnumerable<int>
                {
                    public IEnumerator<int> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;

                    public static MyCollection Create(ReadOnlySpan<int> r) => throw null;
                }

                class C
                {
                    void M()
                    {
                        MyCollection m = [1, 2, 3];
                        m = [];
                    }
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }

                namespace System.Runtime.CompilerServices
                {
                    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
                    internal sealed class CollectionBuilderAttribute : Attribute
                    {
                        public CollectionBuilderAttribute(Type builderType, string methodName)
                        {
                            BuilderType = builderType;
                            MethodName = methodName;
                        }

                        public Type BuilderType { get; }

                        public string MethodName { get; }
                    }
                }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();

        [Fact]
        public Task DoNotAllowCopyInCollectionExpressionElement()
            => new VerifyCS.Test
            {
                TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;

                [NonCopyable]
                struct S
                {
                }

                [CollectionBuilder(typeof(MyCollection), nameof(Create))]
                partial struct MyCollection : IEnumerable<S>
                {
                    public IEnumerator<S> GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;

                    public static MyCollection Create(ReadOnlySpan<S> r) => throw null;
                }

                class C
                {
                    void M()
                    {
                        S s = new();
                        MyCollection m = [{|#0:s|}, new S()];
                    }
                }

                internal sealed class NonCopyableAttribute : System.Attribute { }

                namespace System.Runtime.CompilerServices
                {
                    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
                    internal sealed class CollectionBuilderAttribute : Attribute
                    {
                        public CollectionBuilderAttribute(Type builderType, string methodName)
                        {
                            BuilderType = builderType;
                            MethodName = methodName;
                        }

                        public Type BuilderType { get; }

                        public string MethodName { get; }
                    }
                }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
                ExpectedDiagnostics = {
                    // /0/Test0.cs(25,27): warning RS0042: Unsupported use of non-copyable type 'S' in 'LocalReference' operation
                    VerifyCS.Diagnostic(AbstractDoNotCopyValue.UnsupportedUseRule).WithLocation(0).WithArguments("S", "LocalReference")
                }
            }.RunAsync();
    }
}
