// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpSecurityCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.DoNotCopyValue,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicSecurityCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.DoNotCopyValue,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class DoNotCopyValueTests
    {
        [Fact]
        public async Task TestAcquireFromReturnByValue()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Runtime.InteropServices;

class C
{
    void M()
    {
        var local = GCHandle.Alloc(new object());
    }
}
");

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System.Runtime.InteropServices

Class C
    Sub M()
        Dim local = GCHandle.Alloc(New Object())
    End Sub
End Class");
        }

        [Theory]
        [InlineData("ValueTask<GCHandle>", "ValueTask(Of GCHandle)")]
        [InlineData("ConfiguredValueTaskAwaitable<GCHandle>", "ConfiguredValueTaskAwaitable(Of GCHandle)")]
        public async Task TestAcquireFromAwait(string csharpAwaitableType, string visualBasicAwaitableType)
        {
            await VerifyCS.VerifyAnalyzerAsync($@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

class C
{{
    async Task M({csharpAwaitableType} task)
    {{
        var local = await task;
    }}
}}
");

            await VerifyVB.VerifyAnalyzerAsync($@"
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks

Class C
    Async Function M(task as {visualBasicAwaitableType}) As Task
        Dim local = Await task
    End Function
End Class");
        }

        [Theory]
        [InlineData("Task<GCHandle>", "Task(Of GCHandle)")]
        [InlineData("ConfiguredTaskAwaitable<GCHandle>", "ConfiguredTaskAwaitable(Of GCHandle)")]
        public async Task TestFailedAcquireFromUnsupportedAwait(string csharpAwaitableType, string visualBasicAwaitableType)
        {
            await VerifyCS.VerifyAnalyzerAsync($@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

class C
{{
    async Task M({csharpAwaitableType} task)
    {{
        var local = await task;
    }}
}}
",
                // /0/Test0.cs(10,21): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'Await' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(10, 21, 10, 31).WithArguments("System.Runtime.InteropServices.GCHandle", "Await"));

            await VerifyVB.VerifyAnalyzerAsync($@"
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks

Class C
    Async Function M(task as {visualBasicAwaitableType}) As Task
        Dim local = Await task
    End Function
End Class",
                // /0/Test0.vb(8,21): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'Await' operation
                VerifyVB.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(8, 21, 8, 31).WithArguments("System.Runtime.InteropServices.GCHandle", "Await"));
        }

        [Theory]
        [InlineData("field", "field")]
        [InlineData("(field)", null)]
        [InlineData("this.field", "Me.field")]
        [InlineData("(this).field", null)]
        [InlineData("((C)this).field", "DirectCast(Me, C).field")]
        public async Task TestAcquireIntoFieldFromReturnByValue(string csharpFieldReference, string? visualBasicFieldReference)
        {
            await VerifyCS.VerifyAnalyzerAsync($@"
using System.Runtime.InteropServices;

class C
{{
    GCHandle field;

    void M()
    {{
        {csharpFieldReference} = GCHandle.Alloc(new object());
    }}
}}
");

            if (visualBasicFieldReference is object)
            {
                await VerifyVB.VerifyAnalyzerAsync($@"
Imports System.Runtime.InteropServices

Class C
    Dim field As GCHandle

    Sub M()
        {visualBasicFieldReference} = GCHandle.Alloc(New Object())
    End Sub
End Class");
            }
        }

        [Fact]
        public async Task TestDoNotAcquireFromReturnByReference()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Runtime.InteropServices;

class C
{
    void M()
    {
        var local = GetRef();
    }

    ref GCHandle GetRef() => throw null;
}
",
                // /0/Test0.cs(8,21): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'Invocation' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(8, 21, 8, 29).WithArguments("System.Runtime.InteropServices.GCHandle", "Invocation"));
        }

        [Fact]
        public async Task TestPassToInstancePropertyGetter()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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

        _ = readonlyfield.Target;
        _ = refreadonlylocal.Target;
    }
}
",
                // /0/Test0.cs(23,13): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'FieldReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(23, 13, 23, 26).WithArguments("System.Runtime.InteropServices.GCHandle", "FieldReference"),
                // /0/Test0.cs(17,13): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(24, 13, 24, 29).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"));
        }

        [Fact]
        public async Task TestPassToInstanceMethod()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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

        _ = readonlyfield.AddrOfPinnedObject();
        _ = refreadonlylocal.AddrOfPinnedObject();
    }
}
",
                // /0/Test0.cs(23,13): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'FieldReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(23, 13, 23, 26).WithArguments("System.Runtime.InteropServices.GCHandle", "FieldReference"),
                // /0/Test0.cs(17,13): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(24, 13, 24, 29).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"));
        }

        [Fact]
        public async Task TestPassToExtensionMethod()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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
        {|CS0192:readonlyfield|}.XRef();
        {|CS1510:refreadonlylocal|}.XRef();
        field.X();
        readonlyfield.X();
        local.X();
        reflocal.X();
        refreadonlylocal.X();
    }
}

static class E
{
    public static void X(this GCHandle handle) { }
    public static void XRef(this ref GCHandle handle) { }
    public static void XIn(this in GCHandle handle) { }
}
",
                // /0/Test0.cs(31,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'FieldReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(31, 9, 31, 22).WithArguments("System.Runtime.InteropServices.GCHandle", "FieldReference"),
                // /0/Test0.cs(32,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(32, 9, 32, 25).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"),
                // /0/Test0.cs(33,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'FieldReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(33, 9, 33, 14).WithArguments("System.Runtime.InteropServices.GCHandle", "FieldReference"),
                // /0/Test0.cs(34,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'FieldReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(34, 9, 34, 22).WithArguments("System.Runtime.InteropServices.GCHandle", "FieldReference"),
                // /0/Test0.cs(35,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(35, 9, 35, 14).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"),
                // /0/Test0.cs(36,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(36, 9, 36, 17).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"),
                // /0/Test0.cs(37,9): warning RS0042: Unsupported use of non-copyable type 'System.Runtime.InteropServices.GCHandle' in 'LocalReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(37, 9, 37, 25).WithArguments("System.Runtime.InteropServices.GCHandle", "LocalReference"));
        }

        [Theory]
        [InlineData("throw null")]
        [InlineData("(true ? throw null : default(GCHandle))")]
        [InlineData("(false ? new GCHandle() : throw null)")]
        public async Task TestConversionFromThrowNull(string throwExpression)
        {
            await VerifyCS.VerifyAnalyzerAsync($@"
using System.Runtime.InteropServices;

class C
{{
    GCHandle Get() => {throwExpression};
}}
");
        }

        [Fact]
        public async Task TestPassByReference()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Runtime.InteropServices;

class C
{
    void Get(ref GCHandle handle) => Get(ref handle);
}
");

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System.Runtime.InteropServices

Class C
    Sub Method(ByRef handle As GCHandle)
        Method(handle)
    End Sub
End Class");
        }

        [Fact]
        public async Task TestPassByReadOnlyReference()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Runtime.InteropServices;

class C
{
    void Get(in GCHandle handle) => Get(in handle);
}
");
        }

        [Fact]
        public async Task TestAssignToMember()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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
");
        }

        [Fact]
        public async Task ReturnLocalByValue()
        {
            var source = @"
using System.Runtime.InteropServices;

class C
{
    GCHandle Method()
    {
        GCHandle handle = default;
        return handle;
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task TestReturnMember()
        {
            var source = @"
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
        return _readonlyField.Property;
    }

    int ReturnParameterMemberProperty(CannotCopy parameter)
    {
        return parameter.Property;
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
";

            await new VerifyCS.Test
            {
                TestCode = source,
                ExpectedDiagnostics =
                {
                    // The only reported diagnostic occurs for the invocation of a non-readonly getter of a readonly
                    // non-copyable field.
                    //
                    // /0/Test0.cs(37,16): warning RS0042: Unsupported use of non-copyable type 'CannotCopy' in 'FieldReference' operation
                    VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(37, 16, 37, 30).WithArguments("CannotCopy", "FieldReference"),
                },
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task NonReadonlyMemberProperties()
        {
            // Verify that a non-readonly member of a non-copyable type can reference another non-readonly member of the
            // same type.
            var source = @"
using System.Runtime.InteropServices;

[NonCopyable]
struct CannotCopy
{
    public int First { get { return 0; } }
    public int Second { get { return First; } }
    public int Third => First;
}

internal sealed class NonCopyableAttribute : System.Attribute { }
";

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task NonReadonlyMemberMethods()
        {
            // Verify that a non-readonly member of a non-copyable type can reference another non-readonly member of the
            // same type.
            var source = @"
using System.Runtime.InteropServices;

[NonCopyable]
struct CannotCopy
{
    public int First() { return 0; }
    public int Second() { return First(); }
    public int Third() => First();
}

internal sealed class NonCopyableAttribute : System.Attribute { }
";

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task AllowObjectInitializer()
        {
            var source = @"
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
";

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNonCopyableAttribute()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Runtime.InteropServices;

class C
{
    CanCopy _canCopy;
    CannotCopy _cannotCopy;

    void Method(object value)
    {
        Method(_canCopy);
        Method(_cannotCopy);
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
",
                // /0/Test0.cs(12,16): warning RS0042: Unsupported use of non-copyable type 'CannotCopy' in 'FieldReference' operation
                VerifyCS.Diagnostic(DoNotCopyValue.UnsupportedUseRule).WithSpan(12, 16, 12, 27).WithArguments("CannotCopy", "FieldReference"));
        }

        [Fact]
        public async Task DoNotWrapNonCopyableTypeInNullableT()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Runtime.InteropServices;

class C
{
    GCHandle? field = null;
}
",
                // /0/Test0.cs(6,21): warning RS0042: Do not wrap non-copyable type 'System.Runtime.InteropServices.GCHandle?' in 'FieldInitializer' operation
                VerifyCS.Diagnostic(DoNotCopyValue.AvoidNullableWrapperRule).WithSpan(6, 21, 6, 27).WithArguments("System.Runtime.InteropServices.GCHandle?", "FieldInitializer"));
        }
    }
}
