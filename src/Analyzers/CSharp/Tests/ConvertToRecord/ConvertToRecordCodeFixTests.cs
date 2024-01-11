// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ConvertToRecord;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToRecord
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsConvertToRecord)]
    public class ConvertToRecordCodeFixTests
    {
        [Fact]
        public async Task TestMovePropertySimpleRecordInheritance_CodeFix()
        {
            var initialMarkup = """
                namespace N
                {
                    public record B
                    {
                        public int Foo { get; init; }
                    }

                    public class C : [|B|]
                    {
                        public int P { get; init; }
                    }
                }
                """;
            var changedMarkup = """
                namespace N
                {
                    public record B
                    {
                        public int Foo { get; init; }
                    }

                    public record C(int P) : B;
                }
                """;
            await TestCodeFixAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertyPositionalParameterRecordInheritance_CodeFix()
        {
            var initialMarkup = """
                namespace N
                {
                    public record B(int Foo, int Bar);

                    public class {|CS1729:C|} : [|B|]
                    {
                        public int P { get; init; }
                    }
                }
                """;
            var changedMarkup = """
                namespace N
                {
                    public record B(int Foo, int Bar);

                    public record C(int Foo, int Bar, int P) : B(Foo, Bar);
                }
                """;
            await TestCodeFixAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertyPositionalParameterRecordInheritanceWithComments_CodeFix()
        {
            var initialMarkup = """
                namespace N
                {
                    /// <summary> B </summary>
                    /// <param name="Foo"> Foo is an int </param>
                    /// <param name="Bar"> Bar is an int as well </param>
                    public record B(int Foo, int Bar);

                    /// <summary> C inherits from B </summary>
                    public class {|CS1729:C|} : [|B|]
                    {
                        /// <summary> P can be initialized </summary>
                        public int P { get; init; }
                    }
                }
                """;
            var changedMarkup = """
                namespace N
                {
                    /// <summary> B </summary>
                    /// <param name="Foo"> Foo is an int </param>
                    /// <param name="Bar"> Bar is an int as well </param>
                    public record B(int Foo, int Bar);

                    /// <summary> C inherits from B </summary>
                    /// <param name="Foo"><inheritdoc/></param>
                    /// <param name="Bar"><inheritdoc/></param>
                    /// <param name="P"> P can be initialized </param>
                    public record C(int Foo, int Bar, int P) : B(Foo, Bar);
                }
                """;
            await TestCodeFixAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertyAndReorderWithPositionalParameterRecordInheritance_CodeFix()
        {
            var initialMarkup = """
                namespace N
                {
                    public record B(int Foo, int Bar);

                    public class C : [|B|]
                    {
                        public int P { get; init; }

                        public {|CS1729:C|}(int p, int bar, int foo)
                        {
                            P = p;
                            Bar = bar;
                            Foo = foo;
                        }
                    }
                }
                """;
            var changedMarkup = """
                namespace N
                {
                    public record B(int Foo, int Bar);

                    public record C(int P, int Bar, int Foo) : B(Foo, Bar);
                }
                """;
            await TestCodeFixAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        private class CodeFixTest : CSharpCodeFixVerifier<TestAnalyzer, CSharpConvertToRecordCodeFixProvider>.Test
        {
        }

        private static async Task TestCodeFixAsync(string initialMarkup, string fixedMarkup)
        {
            var test = new CodeFixTest()
            {
                TestCode = initialMarkup,
                FixedCode = fixedMarkup,
                LanguageVersion = LanguageVersion.CSharp10,
                ReferenceAssemblies = Testing.ReferenceAssemblies.Net.Net60,
            };
            await test.RunAsync().ConfigureAwait(false);
        }

        private class TestAnalyzer : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
#pragma warning disable RS0030 // Do not used banned APIs
                => ImmutableArray.Create(new DiagnosticDescriptor(
                    "CS8865",
                    "Only records may inherit from records.",
                    "Only records may inherit from records.",
                    "Compiler error",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true));
#pragma warning restore RS0030 // Do not used banned APIs

            public override void Initialize(AnalysisContext context)
            {
            }
        }
    }
}
