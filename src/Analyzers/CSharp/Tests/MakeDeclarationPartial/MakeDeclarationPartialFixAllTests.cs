// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.MakeDeclarationPartial;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.CSharp.UnitTests.MakeDeclarationPartial
{
    using VerifyCS = CSharpCodeFixVerifier<
        EmptyDiagnosticAnalyzer,
        CSharpMakeDeclarationPartialCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsMakeDeclarationPartial)]
    public sealed class MakeDeclarationPartialFixAllTests
    {
        public static IEnumerable<object[]> AllValidDeclarationTypes()
        {
            yield return new[] { "class" };
            yield return new[] { "struct" };
            yield return new[] { "interface" };
            yield return new[] { "record" };
            yield return new[] { "record class" };
            yield return new[] { "record struct" };
        }

        [Theory]
        [MemberData(nameof(AllValidDeclarationTypes))]
        public async Task SeveralDeclarationsOfSingleClass(string declarationType)
        {
            await new VerifyCS.Test
            {
                TestCode = $$"""
                partial {{declarationType}} Declaration
                {
                }
                
                {{declarationType}} {|CS0260:Declaration|}
                {
                }

                {{declarationType}} {|CS0260:Declaration|}
                {
                }
                """,
                FixedCode = $$"""
                partial {{declarationType}} Declaration
                {
                }
                
                partial {{declarationType}} Declaration
                {
                }

                partial {{declarationType}} Declaration
                {
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Theory]
        [MemberData(nameof(AllValidDeclarationTypes))]
        public async Task SingleDeclarationOfSeveralClasses(string declarationType)
        {
            await new VerifyCS.Test
            {
                TestCode = $$"""
                partial {{declarationType}} Declaration1
                {
                }
                
                {{declarationType}} {|CS0260:Declaration1|}
                {
                }

                partial {{declarationType}} Declaration2
                {
                }

                {{declarationType}} {|CS0260:Declaration2|}
                {
                }
                """,
                FixedCode = $$"""
                partial {{declarationType}} Declaration1
                {
                }
                
                partial {{declarationType}} Declaration1
                {
                }
                
                partial {{declarationType}} Declaration2
                {
                }
                
                partial {{declarationType}} Declaration2
                {
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Theory]
        [MemberData(nameof(AllValidDeclarationTypes))]
        public async Task SeveralDeclarationsOfSeveralClasses(string declarationType)
        {
            await new VerifyCS.Test
            {
                TestCode = $$"""
                partial {{declarationType}} Declaration1
                {
                }
                
                {{declarationType}} {|CS0260:Declaration1|}
                {
                }

                {{declarationType}} {|CS0260:Declaration1|}
                {
                }

                partial {{declarationType}} Declaration2
                {
                }

                {{declarationType}} {|CS0260:Declaration2|}
                {
                }

                {{declarationType}} {|CS0260:Declaration2|}
                {
                }
                """,
                FixedCode = $$"""
                partial {{declarationType}} Declaration1
                {
                }
                
                partial {{declarationType}} Declaration1
                {
                }

                partial {{declarationType}} Declaration1
                {
                }
                
                partial {{declarationType}} Declaration2
                {
                }
                
                partial {{declarationType}} Declaration2
                {
                }

                partial {{declarationType}} Declaration2
                {
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }
    }
}
