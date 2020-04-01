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
        public async Task DoNotWrapNonCopyableTypeInNullableT()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Runtime.InteropServices;

class C
{
    GCHandle? field = null;
}
",
                // /0/Test0.cs(6,21): warning RS0042: Do not wrap non-copyable type '{0}' in '{1)' operation
                VerifyCS.Diagnostic(DoNotCopyValue.AvoidNullableWrapperRule).WithSpan(6, 21, 6, 27).WithArguments("System.Runtime.InteropServices.GCHandle?", "FieldInitializer"));
        }
    }
}
