// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeRefactoringVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpRunIterations>;
using VerifyVB = Test.Utilities.VisualBasicCodeRefactoringVerifier<
    Roslyn.Diagnostics.VisualBasic.Analyzers.VisualBasicRunIterations>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class RunIterationsTests
    {
        private static readonly ReferenceAssemblies xunitWithCombinatorial =
            AdditionalMetadataReferences.DefaultWithXUnit.AddPackages(
                ImmutableArray.Create(new PackageIdentity("Xunit.Combinatorial", "1.4.1")));

        [Theory]
        [InlineData("Fact")]
        [InlineData("FactAttribute")]
        [InlineData("CustomFact")]
        [InlineData("CustomFactAttribute")]
        public async Task RunIterationsOfFact_CSharp(string attributeName)
        {
            var updatedName = attributeName switch
            {
                "Fact" => "Theory",
                "FactAttribute" => "TheoryAttribute",
                "CustomFact" => "CustomTheory",
                "CustomFactAttribute" => "CustomTheoryAttribute",
                _ => throw new ArgumentException("Unexpected argument", nameof(attributeName)),
            };

            await new VerifyCS.Test
            {
                ReferenceAssemblies = xunitWithCombinatorial,
                TestCode = $$"""
                using Xunit;

                class TestClass
                {
                    [{{attributeName}}]
                    public void $$Method()
                    {
                    }
                }

                class CustomFactAttribute : FactAttribute { }
                class CustomTheoryAttribute : TheoryAttribute { }
                """,
                FixedCode = $$"""
                using Xunit;

                class TestClass
                {
                    [{{updatedName}}]
                    [CombinatorialData]
                    public void $$Method([CombinatorialRange(0, 10)] int iteration)
                    {
                        _ = iteration;
                    }
                }

                class CustomFactAttribute : FactAttribute { }
                class CustomTheoryAttribute : TheoryAttribute { }
                """,
            }.RunAsync();
        }

        [Theory]
        [InlineData("Fact")]
        [InlineData("FactAttribute")]
        [InlineData("CustomFact")]
        [InlineData("CustomFactAttribute")]
        public async Task RunIterationsOfFact_VisualBasic(string attributeName)
        {
            // Visual Basic syntax generator, formatter, and/or simplifier drops the Attribute suffix automatically
            var updatedName = attributeName switch
            {
                "Fact" => "Theory",
                "FactAttribute" => "Theory",
                "CustomFact" => "CustomTheory",
                "CustomFactAttribute" => "CustomTheory",
                _ => throw new ArgumentException("Unexpected argument", nameof(attributeName)),
            };

            await new VerifyVB.Test
            {
                ReferenceAssemblies = xunitWithCombinatorial,
                TestCode = $"""
                Imports Xunit

                Class TestClass
                    <{attributeName}>
                    Public Sub $$Method()
                    End Sub
                End Class

                Class CustomFactAttribute : Inherits FactAttribute : End Class
                Class CustomTheoryAttribute : Inherits TheoryAttribute : End Class
                """,
                FixedCode = $"""
                Imports Xunit

                Class TestClass
                    <{updatedName}>
                    <CombinatorialData>
                    Public Sub $$Method(<CombinatorialRange(0, 10)> iteration As Integer)
                    End Sub
                End Class

                Class CustomFactAttribute : Inherits FactAttribute : End Class
                Class CustomTheoryAttribute : Inherits TheoryAttribute : End Class
                """,
            }.RunAsync();
        }

        [Theory]
        [InlineData("Fact")]
        [InlineData("FactAttribute")]
        [InlineData("CustomFact")]
        [InlineData("CustomFactAttribute")]
        public async Task RunIterationsOfFactWithTrait_CSharp(string attributeName)
        {
            var updatedName = attributeName switch
            {
                "Fact" => "Theory",
                "FactAttribute" => "TheoryAttribute",
                "CustomFact" => "CustomTheory",
                "CustomFactAttribute" => "CustomTheoryAttribute",
                _ => throw new ArgumentException("Unexpected argument", nameof(attributeName)),
            };

            await new VerifyCS.Test
            {
                ReferenceAssemblies = xunitWithCombinatorial,
                TestCode = $$"""
                using Xunit;

                class TestClass
                {
                    [{{attributeName}}, Trait("Key", "Value")]
                    public void $$Method()
                    {
                    }
                }

                class CustomFactAttribute : FactAttribute { }
                class CustomTheoryAttribute : TheoryAttribute { }
                """,
                FixedCode = $$"""
                using Xunit;

                class TestClass
                {
                    [{{updatedName}}, Trait("Key", "Value")]
                    [CombinatorialData]
                    public void $$Method([CombinatorialRange(0, 10)] int iteration)
                    {
                        _ = iteration;
                    }
                }

                class CustomFactAttribute : FactAttribute { }
                class CustomTheoryAttribute : TheoryAttribute { }
                """,
            }.RunAsync();
        }

        [Theory]
        [InlineData("Fact")]
        [InlineData("FactAttribute")]
        [InlineData("CustomFact")]
        [InlineData("CustomFactAttribute")]
        public async Task RunIterationsOfFactWithTrait_VisualBasic(string attributeName)
        {
            // Visual Basic syntax generator, formatter, and/or simplifier drops the Attribute suffix automatically
            var updatedName = attributeName switch
            {
                "Fact" => "Theory",
                "FactAttribute" => "Theory",
                "CustomFact" => "CustomTheory",
                "CustomFactAttribute" => "CustomTheory",
                _ => throw new ArgumentException("Unexpected argument", nameof(attributeName)),
            };

            await new VerifyVB.Test
            {
                ReferenceAssemblies = xunitWithCombinatorial,
                TestCode = $"""
                Imports Xunit

                Class TestClass
                    <{attributeName}, Trait("Key", "Value")>
                    Public Sub $$Method()
                    End Sub
                End Class

                Class CustomFactAttribute : Inherits FactAttribute : End Class
                Class CustomTheoryAttribute : Inherits TheoryAttribute : End Class
                """,
                FixedCode = $"""
                Imports Xunit

                Class TestClass
                    <{updatedName}, Trait("Key", "Value")>
                    <CombinatorialData>
                    Public Sub $$Method(<CombinatorialRange(0, 10)> iteration As Integer)
                    End Sub
                End Class

                Class CustomFactAttribute : Inherits FactAttribute : End Class
                Class CustomTheoryAttribute : Inherits TheoryAttribute : End Class
                """,
            }.RunAsync();
        }

        [Theory]
        [InlineData("Theory")]
        [InlineData("TheoryAttribute")]
        [InlineData("CustomTheory")]
        [InlineData("CustomTheoryAttribute")]
        public async Task NoIterationsForTheoryWithInlineData_CSharp(string attributeName)
        {
            var testCode = $$"""
                using Xunit;

                class TestClass
                {
                    [{{attributeName}}]
                    [InlineData(true)]
                    public void $$Method(bool arg)
                    {
                        _ = arg;
                    }
                }

                class CustomFactAttribute : FactAttribute { }
                class CustomTheoryAttribute : TheoryAttribute { }
                """;

            await new VerifyCS.Test
            {
                ReferenceAssemblies = xunitWithCombinatorial,
                TestCode = testCode,
                FixedCode = testCode,
            }.RunAsync();
        }

        [Theory]
        [InlineData("Theory")]
        [InlineData("TheoryAttribute")]
        [InlineData("CustomTheory")]
        [InlineData("CustomTheoryAttribute")]
        public async Task NoIterationsForTheoryWithInlineData_VisualBasic(string attributeName)
        {
            var testCode = $"""
                Imports Xunit

                Class TestClass
                    <{attributeName}>
                    <InlineData(True)>
                    Public Sub $$Method(arg As Boolean)
                    End Sub
                End Class

                Class CustomFactAttribute : Inherits FactAttribute : End Class
                Class CustomTheoryAttribute : Inherits TheoryAttribute : End Class
                """;

            await new VerifyVB.Test
            {
                ReferenceAssemblies = xunitWithCombinatorial,
                TestCode = testCode,
                FixedCode = testCode,
            }.RunAsync();
        }

        [Theory]
        [InlineData("Theory")]
        [InlineData("TheoryAttribute")]
        [InlineData("CustomTheory")]
        [InlineData("CustomTheoryAttribute")]
        public async Task NoIterationsForTheoryWithIterations_CSharp(string attributeName)
        {
            var testCode = $$"""
                using Xunit;

                class TestClass
                {
                    [{{attributeName}}]
                    [CombinatorialData]
                    public void $$Method([CombinatorialRange(0, 10)] int iteration)
                    {
                        _ = iteration;
                    }
                }

                class CustomFactAttribute : FactAttribute { }
                class CustomTheoryAttribute : TheoryAttribute { }
                """;

            await new VerifyCS.Test
            {
                ReferenceAssemblies = xunitWithCombinatorial,
                TestCode = testCode,
                FixedCode = testCode,
            }.RunAsync();
        }

        [Theory]
        [InlineData("Theory")]
        [InlineData("TheoryAttribute")]
        [InlineData("CustomTheory")]
        [InlineData("CustomTheoryAttribute")]
        public async Task NoIterationsForTheoryWithIterations_VisualBasic(string attributeName)
        {
            var testCode = $"""
                Imports Xunit

                Class TestClass
                    <{attributeName}>
                    <CombinatorialData>
                    Public Sub $$Method(<CombinatorialRange(0, 10)> iteration As Integer)
                    End Sub
                End Class

                Class CustomFactAttribute : Inherits FactAttribute : End Class
                Class CustomTheoryAttribute : Inherits TheoryAttribute : End Class
                """;

            await new VerifyVB.Test
            {
                ReferenceAssemblies = xunitWithCombinatorial,
                TestCode = testCode,
                FixedCode = testCode,
            }.RunAsync();
        }

        [Theory]
        [InlineData("Theory")]
        [InlineData("TheoryAttribute")]
        [InlineData("CustomTheory")]
        [InlineData("CustomTheoryAttribute")]
        public Task RunIterationsOfTheory_CSharp(string attributeName)
            => new VerifyCS.Test
            {
                ReferenceAssemblies = xunitWithCombinatorial,
                TestCode = $$"""
                using Xunit;

                class TestClass
                {
                    [{{attributeName}}]
                    [CombinatorialData]
                    public void $$Method(bool arg)
                    {
                        _ = arg;
                    }
                }

                class CustomFactAttribute : FactAttribute { }
                class CustomTheoryAttribute : TheoryAttribute { }
                """,
                FixedCode = $$"""
                using Xunit;

                class TestClass
                {
                    [{{attributeName}}]
                    [CombinatorialData]
                    public void $$Method(bool arg, [CombinatorialRange(0, 10)] int iteration)
                    {
                        _ = iteration;
                        _ = arg;
                    }
                }

                class CustomFactAttribute : FactAttribute { }
                class CustomTheoryAttribute : TheoryAttribute { }
                """,
            }.RunAsync();

        [Theory]
        [InlineData("Theory")]
        [InlineData("TheoryAttribute")]
        [InlineData("CustomTheory")]
        [InlineData("CustomTheoryAttribute")]
        public Task RunIterationsOfTheory_VisualBasic(string attributeName)
            => new VerifyVB.Test
            {
                ReferenceAssemblies = xunitWithCombinatorial,
                TestCode = $"""
                Imports Xunit

                Class TestClass
                    <{attributeName}>
                    <CombinatorialData>
                    Public Sub $$Method(arg As Boolean)
                    End Sub
                End Class

                Class CustomFactAttribute : Inherits FactAttribute : End Class
                Class CustomTheoryAttribute : Inherits TheoryAttribute : End Class
                """,
                FixedCode = $"""
                Imports Xunit

                Class TestClass
                    <{attributeName}>
                    <CombinatorialData>
                    Public Sub $$Method(arg As Boolean, <CombinatorialRange(0, 10)> iteration As Integer)
                    End Sub
                End Class

                Class CustomFactAttribute : Inherits FactAttribute : End Class
                Class CustomTheoryAttribute : Inherits TheoryAttribute : End Class
                """,
            }.RunAsync();
    }
}
