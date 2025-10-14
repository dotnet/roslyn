// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.FixIncorrectConstraint;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.FixIncorrectConstraint;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpFixIncorrectConstraintCodeFixProvider>;

public sealed class FixIncorrectConstraintTests
{
    [Fact]
    public Task TestEnumConstraint()
        => new VerifyCS.Test
        {
            TestCode = """
            class C<T> where T : {|CS9010:enum|}
            {
            }
            """,
            FixedCode = """
            class C<T> where T : struct, System.Enum
            {
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestEnumConstraintWithUsing()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C<T> where T : {|CS9010:enum|}
            {
            }
            """,
            FixedCode = """
            using System;

            class C<T> where T : struct, Enum
            {
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestDelegateConstraint()
        => new VerifyCS.Test
        {
            TestCode = """
            class C<T> where T : {|CS9011:delegate|}
            {
            }
            """,
            FixedCode = """
            class C<T> where T : System.Delegate
            {
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestDelegateConstraintWithUsing()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C<T> where T : {|CS9011:delegate|}
            {
            }
            """,
            FixedCode = """
            using System;

            class C<T> where T : Delegate
            {
            }
            """,
        }.RunAsync();
}
