// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeDeclarationPartial;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.CSharp.UnitTests.MakeDeclarationPartial
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsMakeDeclarationPartial)]
    public sealed class MakeDeclarationPartialTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public MakeDeclarationPartialTests(ITestOutputHelper logger)
            : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpMakeDeclarationPartialCodeFixProvider());

        public static IEnumerable<object[]> AllValidDeclarationTypes()
        {
            yield return new[] { "class" };
            yield return new[] { "struct" };
            yield return new[] { "interface" };
            yield return new[] { "record" };
            yield return new[] { "record struct" };
        }

        [Theory]
        [MemberData(nameof(AllValidDeclarationTypes))]
        public async Task OutsideNamespace(string declarationType)
        {
            await TestInRegularAndScriptAsync($$"""
                partial {{declarationType}} Declaration
                {
                }

                {{declarationType}} [|Declaration|]
                {
                }
                """, $$"""
                partial {{declarationType}} Declaration
                {
                }

                partial {{declarationType}} [|Declaration|]
                {
                }
                """);
        }

        [Theory]
        [MemberData(nameof(AllValidDeclarationTypes))]
        public async Task InsideOneFileScopedNamespace(string declarationType)
        {
            await TestInRegularAndScriptAsync($$"""
                namespace TestNamespace;

                partial {{declarationType}} Declaration
                {
                }

                {{declarationType}} [|Declaration|]
                {
                }
                """, $$"""
                namespace TestNamespace;

                partial {{declarationType}} Declaration
                {
                }

                partial {{declarationType}} [|Declaration|]
                {
                }
                """);
        }

        [Theory]
        [MemberData(nameof(AllValidDeclarationTypes))]
        public async Task InsideOneBlockScopedNamespace(string declarationType)
        {
            await TestInRegularAndScriptAsync($$"""
                namespace TestNamespace
                {
                    partial {{declarationType}} Declaration
                    {
                    }

                    {{declarationType}} [|Declaration|]
                    {
                    }
                }
                """, $$"""
                namespace TestNamespace
                {
                    partial {{declarationType}} Declaration
                    {
                    }

                    partial {{declarationType}} [|Declaration|]
                    {
                    }
                }
                """);
        }

        [Theory]
        [MemberData(nameof(AllValidDeclarationTypes))]
        public async Task InsideTwoEqualBlockScopedNamespaces(string declarationType)
        {
            await TestInRegularAndScriptAsync($$"""
                namespace TestNamespace
                {
                    partial {{declarationType}} Declaration
                    {
                    }
                }

                namespace TestNamespace
                {
                    {{declarationType}} [|Declaration|]
                    {
                    }
                }
                """, $$"""
                namespace TestNamespace
                {
                    partial {{declarationType}} Declaration
                    {
                    }
                }

                namespace TestNamespace
                {
                    partial {{declarationType}} [|Declaration|]
                    {
                    }
                }
                """);
        }

        [Theory]
        [MemberData(nameof(AllValidDeclarationTypes))]
        public async Task InDifferentDocuments(string declarationType)
        {
            await TestInRegularAndScriptAsync($$"""
                <Workspace>
                    <Project Language="C#">
                        <Document>
                partial {{declarationType}} Declaration
                {
                }
                        </Document>
                        <Document>
                {{declarationType}} [|Declaration|]
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """, $$"""
                <Workspace>
                    <Project Language="C#">
                        <Document>
                partial {{declarationType}} Declaration
                {
                }
                        </Document>
                        <Document>
                partial {{declarationType}} [|Declaration|]
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }

        [Theory]
        [MemberData(nameof(AllValidDeclarationTypes))]
        public async Task WithOtherModifiers(string declarationType)
        {
            await TestInRegularAndScriptAsync($$"""
                public partial {{declarationType}} Declaration
                {
                }

                public {{declarationType}} [|Declaration|]
                {
                }
                """, $$"""
                public partial {{declarationType}} Declaration
                {
                }

                public partial {{declarationType}} [|Declaration|]
                {
                }
                """);
        }

        [Theory]
        [MemberData(nameof(AllValidDeclarationTypes))]
        public async Task NotInDifferentNamespaces(string declarationType)
        {
            await TestMissingInRegularAndScriptAsync($$"""
                namespace TestNamespace1
                {
                    partial {{declarationType}} Declaration
                    {
                    }
                }

                namespace TestNamespace2
                {
                    {{declarationType}} [|Declaration|]
                    {
                    }
                }
                """);
        }
    }
}
