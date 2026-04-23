// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.RemoveAsyncModifier;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveAsyncModifier;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveUnnecessaryAsyncModifierInterfaceImplementationOrOverrideDiagnosticAnalyzer,
    CSharpRemoveAsyncModifierCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
public sealed class RemoveAsyncModifierInterfaceImplementationOrOverrideTests
{
    [Fact]
    public Task ImplicitInterfaceImplementation()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            interface I
            {
                Task Goo();
            }

            class C : I
            {
                public {|IDE0391:async|} Task Goo(){}
            }
            """,
            """
            using System.Threading.Tasks;
            
            interface I
            {
                Task Goo();
            }

            class C : I
            {
                public Task Goo()
                {
                    return Task.CompletedTask;
                }
            }
            """);

    [Fact]
    public Task ExplicitInterfaceImplementation()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            interface I
            {
                Task Goo();
            }

            class C : I
            {
                {|IDE0391:async|} Task I.Goo(){}
            }
            """,
            """
            using System.Threading.Tasks;
            
            interface I
            {
                Task Goo();
            }

            class C : I
            {
                Task I.Goo()
                {
                    return Task.CompletedTask;
                }
            }
            """);

    [Fact]
    public Task Override()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            class B
            {
                public virtual Task Goo() => Task.CompletedTask;
            }

            class C : B
            {
                public override {|IDE0391:async|} Task Goo(){}
            }
            """,
            """
            using System.Threading.Tasks;
            
            class B
            {
                public virtual Task Goo() => Task.CompletedTask;
            }

            class C : B
            {
                public override Task Goo()
                {
                    return Task.CompletedTask;
                }
            }
            """);
}
