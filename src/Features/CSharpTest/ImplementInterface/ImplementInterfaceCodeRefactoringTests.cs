// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.ImplementInterface;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.ImplementInterface;

using VerifyCS = CSharpCodeRefactoringVerifier<
    ImplementInterfaceCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
public sealed class ImplementInterfaceCodeRefactoringTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78294")]
    public Task TestInBody()
        => VerifyCS.VerifyRefactoringAsync("""
            interface IGoo
            {
                void Goo();
            }

            class C : {|CS0535:IGoo|}
            {
                $$
            }
            """, """
            interface IGoo
            {
                void Goo();
            }
            
            class C : IGoo
            {
                public void Goo()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78294")]
    public Task TestNotOnInterfaceInBody()
        => VerifyCS.VerifyRefactoringAsync("""
            interface IGoo
            {
                void Goo();
            }

            interface IBar : IGoo
            {
                $$
            }
            """);
}
