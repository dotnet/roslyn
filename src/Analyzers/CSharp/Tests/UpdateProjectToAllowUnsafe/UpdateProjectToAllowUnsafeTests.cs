// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UpdateProjectToAllowUnsafe;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UpdateProjectToAllowUnsafe;

[Trait(Traits.Feature, Traits.Features.CodeActionsUpdateProjectToAllowUnsafe)]
public sealed class UpdateProjectToAllowUnsafeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public UpdateProjectToAllowUnsafeTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpUpdateProjectToAllowUnsafeCodeFixProvider());

    private async Task TestAllowUnsafeEnabledIfDisabledAsync(string initialMarkup)
    {
        var parameters = TestParameters.Default;
        using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
        {
            var (_, action) = await GetCodeActionsAsync(workspace, parameters);
            var operations = await VerifyActionAndGetOperationsAsync(workspace, action);

            var (oldSolution, newSolution) = await ApplyOperationsAndGetSolutionAsync(workspace, operations);
            Assert.True(((CSharpCompilationOptions)newSolution.Projects.Single().CompilationOptions!).AllowUnsafe);
        }

        // no action offered if unsafe was already enabled
        await TestMissingAsync(initialMarkup, new TestParameters(compilationOptions:
            new CSharpCompilationOptions(outputKind: default, allowUnsafe: true)));
    }

    [Fact]
    public Task OnUnsafeClass()
        => TestAllowUnsafeEnabledIfDisabledAsync(
            """
            unsafe class [|C|] // The compiler reports this on the name, not the 'unsafe' keyword.
            {
            }
            """);

    [Fact]
    public Task OnUnsafeMethod()
        => TestAllowUnsafeEnabledIfDisabledAsync(
            """
            class C
            {
                unsafe void [|M|]()
                {
                }
            }
            """);

    [Fact]
    public Task OnUnsafeLocalFunction()
        => TestAllowUnsafeEnabledIfDisabledAsync(
            """
            class C
            {
                void M()
                {
                    unsafe void [|F|]()
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task OnUnsafeBlock()
        => TestAllowUnsafeEnabledIfDisabledAsync(
            """
            class C
            {
                void M()
                {
                    [|unsafe|]
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotInsideUnsafeBlock()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    unsafe
                    {
                        [|int * p;|]
                    }
                }
            }
            """);
}
