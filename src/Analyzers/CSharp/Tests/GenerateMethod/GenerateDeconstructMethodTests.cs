// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateDeconstructMethod;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateDeconstructMethod;

[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
public sealed class GenerateDeconstructMethodTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new GenerateDeconstructMethodCodeFixProvider());

    [Fact]
    public Task TestDeconstructionDeclaration_Simple()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    (int x, int y) = [|this|];
                }
            }
            """,
            """
            using System;

            class Class
            {
                private void Deconstruct(out int x, out int y)
                {
                    throw new NotImplementedException();
                }

                void Method()
                {
                    (int x, int y) = this;
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionDeclaration_Simple_Record()
        => TestInRegularAndScriptAsync(
            """
            record R
            {
                void Method()
                {
                    (int x, int y) = [|this|];
                }
            }
            """,
            """
            using System;

            record R
            {
                private void Deconstruct(out int x, out int y)
                {
                    throw new NotImplementedException();
                }

                void Method()
                {
                    (int x, int y) = this;
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionDeclaration_TypeParameters()
        => TestInRegularAndScriptAsync(
            """
            class Class<T>
            {
                void Method<U>()
                {
                    (T x, U y) = [|this|];
                }
            }
            """,
            """
            using System;

            class Class<T>
            {
                private void Deconstruct(out T x, out object y)
                {
                    throw new NotImplementedException();
                }

                void Method<U>()
                {
                    (T x, U y) = this;
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionDeclaration_OtherDeconstructMethods()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    (int x, int y) = [|this|];
                }
                void Deconstruct(out int x) => throw null;
                void Deconstruct(out int x, out int y, out int z) => throw null;
            }
            """,
            """
            using System;

            class Class
            {
                void Method()
                {
                    (int x, int y) = this;
                }
                void Deconstruct(out int x) => throw null;
                void Deconstruct(out int x, out int y, out int z) => throw null;

                private void Deconstruct(out int x, out int y)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionDeclaration_AlreadySuccessfull()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    (int x, int y) = [|this|];
                }
                void Deconstruct(out int x, out int y) => throw null;
            }
            """);

    [Fact]
    public Task TestDeconstructionDeclaration_UndeterminedType()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    (var x, var y) = [|this|];
                }
            }
            """,
            """
            using System;

            class Class
            {
                private void Deconstruct(out object x, out object y)
                {
                    throw new NotImplementedException();
                }

                void Method()
                {
                    (var x, var y) = this;
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionDeclaration_UndeterminedType2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    var (x, y) = [|this|];
                }
            }
            """,
            """
            using System;

            class Class
            {
                private void Deconstruct(out object x, out object y)
                {
                    throw new NotImplementedException();
                }

                void Method()
                {
                    var (x, y) = this;
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionDeclaration_BuiltinType()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    (int x, int y) = [|1|];
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionAssignment()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    int x, y;
                    (x, y) = [|this|];
                }
            }
            """,
            """
            using System;

            class Class
            {
                private void Deconstruct(out int x, out int y)
                {
                    throw new NotImplementedException();
                }

                void Method()
                {
                    int x, y;
                    (x, y) = this;
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionAssignment_Nested()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    int x, y, z;
                    ((x, y), z) = ([|this|], 0);
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionAssignment_Array()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    int x, y;
                    (x, y) = [|new[] { this }|];
                }
            }
            """);

    [Fact]
    public Task TestSimpleDeconstructionForeach()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    foreach ((int x, int y) in new[] { [|this|] }) { }
                }
            }
            """,
            """
            using System;

            class Class
            {
                private void Deconstruct(out int x, out int y)
                {
                    throw new NotImplementedException();
                }

                void Method()
                {
                    foreach ((int x, int y) in new[] { this }) { }
                }
            }
            """);

    [Fact]
    public Task TestSimpleDeconstructionForeach_AnotherType()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(D d)
                {
                    foreach ((int x, int y) in new[] { [|d|] }) { }
                }
            }
            class D
            {
            }
            """,
            """
            using System;

            class Class
            {
                void Method(D d)
                {
                    foreach ((int x, int y) in new[] { d }) { }
                }
            }
            class D
            {
                internal void Deconstruct(out int x, out int y)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionPositionalPattern()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Method()
                {
                    if(this is C(""[||])) { }
                }
            }
            """,
            """
            using System;

            class C
            {
                private void Deconstruct(out string v)
                {
                    throw new NotImplementedException();
                }

                void Method()
                {
                    if(this is C(""[||])) { }
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionPositionalPattern_NullExpression()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Method()
                {
                    if(this is C(""[||], ref 0)) { }
                }
            }
            """, """
            using System;

            class C
            {
                private void Deconstruct(out string v, out object value)
                {
                    throw new NotImplementedException();
                }

                void Method()
                {
                    if(this is C(""[||], ref 0)) { }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32510")]
    public Task TestDeconstructionAssignment_InvalidDeclaration()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void Method()
                {
                    var stuff = new Dictionary<string, string>();
                    foreach ((key, value) in [|stuff|]) // Invalid variable declarator syntax
                    {
                    }
                }
            }
            """);
}
