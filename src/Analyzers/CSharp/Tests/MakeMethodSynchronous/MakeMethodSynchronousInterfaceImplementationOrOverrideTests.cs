// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeMethodSynchronous;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveUnnecessaryAsyncModifierInterfaceImplementationOrOverrideDiagnosticAnalyzer,
    CSharpMakeMethodSynchronousCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
public sealed class MakeMethodSynchronousInterfaceImplementationOrOverrideTests
{
    [Fact]
    public Task ImplicitInterface()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            interface I
            {
                Task Goo();
            }

            class C : I
            {
                public {|IDE0391:async|} Task Goo()
                {
                }
            }
            """,
            """
            using System.Threading.Tasks;
            
            interface I
            {
                Task Goo();
            }

            class C : {|CS0738:I|}
            {
                public void Goo()
                {
                }
            }
            """);

    [Fact]
    public Task ExplicitImplicitInterface()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            interface I
            {
                Task Goo();
            }

            class C : I
            {
                {|IDE0391:async|} Task I.Goo()
                {
                }
            }
            """,
            """
            using System.Threading.Tasks;
            
            interface I
            {
                Task Goo();
            }

            class C : {|CS0535:I|}
            {
                void I.{|CS9334:Goo|}()
                {
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
                public override {|IDE0391:async|} Task Goo()
                {
                }
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
                public override void {|CS0508:Goo|}()
                {
                }
            }
            """);
}
