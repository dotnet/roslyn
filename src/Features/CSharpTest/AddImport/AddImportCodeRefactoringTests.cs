// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddImport;

using VerifyCS = CSharpCodeRefactoringVerifier<CSharpAddImportCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
public sealed class AddImportCodeRefactoringTests
{
    [Fact]
    public Task TestSimpleQualifiedTypeName()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [||]System.Threading.Tasks.Task M() => null;
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                Task M() => null;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InReturnType()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                System.Threading.Tasks.[||]Task M() => null;
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                Task M() => null;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_GenericType()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [||]System.Collections.Generic.List<int> M() => null;
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                List<int> M() => null;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InParameter()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M([||]System.Threading.Tasks.Task t) { }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                void M(Task t) { }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InField()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                private [||]System.Threading.Tasks.Task _task;
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                private Task _task;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InLocalVariable()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    [||]System.Threading.Tasks.Task task = null;
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Task task = null;
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InNewExpression()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    var list = new [||]System.Collections.Generic.List<int>();
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = new List<int>();
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InTypeof()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    var type = typeof([||]System.Threading.Tasks.Task);
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    var type = typeof(Task);
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InBaseType()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C : [||]System.Exception
            {
            }
            """,
            """
            using System;

            class C : Exception
            {
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InInterface()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C : [||]System.IDisposable
            {
                public void Dispose() { }
            }
            """,
            """
            using System;

            class C : IDisposable
            {
                public void Dispose() { }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InGenericConstraint()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C<T> where T : [||]System.IDisposable
            {
            }
            """,
            """
            using System;

            class C<T> where T : IDisposable
            {
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InCastExpression()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(object o)
                {
                    var e = ([||]System.Exception)o;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var e = (Exception)o;
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InIsExpression()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(object o)
                {
                    var b = o is [||]System.Exception;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var b = o is Exception;
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InAsExpression()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(object o)
                {
                    var e = o as [||]System.Exception;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var e = o as Exception;
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_NestedNamespace()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [||]System.Threading.Tasks.Task<int> M() => null;
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                Task<int> M() => null;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InAttribute()
        => VerifyCS.VerifyRefactoringAsync(
            """
            [[||]System.Obsolete]
            class C
            {
            }
            """,
            """
            using System;

            [Obsolete]
            class C
            {
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_NotOfferedWhenUsingAlreadyExists()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                [||]System.Threading.Tasks.Task M() => null;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_NotOfferedOnNamespace()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    var x = [||]System.Threading;
                }
            }
            """);

    [Fact]
    public Task TestStaticMemberAccess()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    [||]System.Console.WriteLine();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task TestStaticMemberAccess_InMiddle()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    System.[||]Console.WriteLine();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task TestStaticMemberAccess_WithExistingUsing()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [||]System.Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task TestGlobalQualifiedName()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [||]global::System.Threading.Tasks.Task M() => null;
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                Task M() => null;
            }
            """);

    [Fact]
    public Task TestAmbiguity_LocalTypeWithSameName()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class Task { }

            class C
            {
                [||]System.Threading.Tasks.Task M() => null;
            }
            """,
            """
            using System.Threading.Tasks;

            class Task { }

            class C
            {
                Task M() => null;
            }
            """);
}
