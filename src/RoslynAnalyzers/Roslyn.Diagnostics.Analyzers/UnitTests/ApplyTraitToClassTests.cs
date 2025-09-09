// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeRefactoringVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpApplyTraitToClass>;
using VerifyVB = Test.Utilities.VisualBasicCodeRefactoringVerifier<
    Roslyn.Diagnostics.VisualBasic.Analyzers.VisualBasicApplyTraitToClass>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class ApplyTraitToClassTests
    {
        [Theory]
        [InlineData("A", "")]
        [InlineData("", "A")]
        [InlineData("A", "A")]
        public Task MoveTraitToType_MovesSecond_CSharpAsync(string name, string value)
            => new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithXUnit,
                TestCode = $$"""
                using Xunit;

                class C
                {
                    [$$Trait("{{name}}", "{{value}}")]
                    public void Method() { }

                    [Fact, Trait("{{name}}", "{{value}}")]
                    public void Method2() { }
                }
                """,
                FixedCode = $$"""
                using Xunit;

                [Trait("{{name}}", "{{value}}")]
                class C
                {
                    public void Method() { }

                    [Fact]
                    public void Method2() { }
                }
                """,
            }.RunAsync();

        [Theory]
        [InlineData("A", "")]
        [InlineData("", "A")]
        [InlineData("A", "A")]
        public Task MoveTraitToType_MovesSecond_VisualBasicAsync(string name, string value)
            => new VerifyVB.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithXUnit,
                TestCode = $"""
                Imports Xunit

                Class C
                    <$$Trait("{name}", "{value}")>
                    Public Sub Method()
                    End Sub

                    <Fact, Trait("{name}", "{value}")>
                    Public Sub Method2()
                    End Sub
                End Class
                """,
                FixedCode = $"""
                Imports Xunit

                <Trait("{name}", "{value}")>
                Class C
                    Public Sub Method()
                    End Sub

                    <Fact>
                    Public Sub Method2()
                    End Sub
                End Class
                """,
            }.RunAsync();

        [Theory]
        [InlineData("A", "")]
        [InlineData("", "A")]
        [InlineData("A", "A")]
        public Task MoveTraitToType_MovesOnlyFirst_CSharpAsync(string name, string value)
            => new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithXUnit,
                TestCode = $$"""
                using Xunit;

                class C
                {
                    [$$Trait("", "")]
                    public void Method() { }

                    [Fact, Trait("{{name}}", "{{value}}")]
                    public void Method2() { }
                }
                """,
                FixedCode = $$"""
                using Xunit;

                [Trait("", "")]
                class C
                {
                    public void Method() { }

                    [Fact, Trait("{{name}}", "{{value}}")]
                    public void Method2() { }
                }
                """,
            }.RunAsync();

        [Theory]
        [InlineData("A", "")]
        [InlineData("", "A")]
        [InlineData("A", "A")]
        public Task MoveTraitToType_MovesOnlyFirst_VisualBasicAsync(string name, string value)
            => new VerifyVB.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithXUnit,
                TestCode = $"""
                Imports Xunit

                Class C
                    <$$Trait("", "")>
                    Public Sub Method()
                    End Sub

                    <Fact, Trait("{name}", "{value}")>
                    Public Sub Method2()
                    End Sub
                End Class
                """,
                FixedCode = $"""
                Imports Xunit

                <Trait("", "")>
                Class C
                    Public Sub Method()
                    End Sub

                    <Fact, Trait("{name}", "{value}")>
                    Public Sub Method2()
                    End Sub
                End Class
                """,
            }.RunAsync();
    }
}
