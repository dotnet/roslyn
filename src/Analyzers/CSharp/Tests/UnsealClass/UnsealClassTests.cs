// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UnsealClass;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UnsealClass
{
    using VerifyCS = CSharpCodeFixVerifier<
        EmptyDiagnosticAnalyzer,
        CSharpUnsealClassCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUnsealClass)]
    public sealed class UnsealClassTests
    {
        [Fact]
        public async Task RemovedFromSealedClass()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                sealed class C
                {
                }
                class D : {|CS0509:C|}
                {
                }
                """, """
                class C
                {
                }
                class D : C
                {
                }
                """);
        }

        [Fact]
        public async Task RemovedFromSealedClassWithOtherModifiersPreserved()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                public sealed unsafe class C
                {
                }
                class D : {|CS0509:C|}
                {
                }
                """, """
                public unsafe class C
                {
                }
                class D : C
                {
                }
                """);
        }

        [Fact]
        public async Task RemovedFromSealedClassWithConstructedGeneric()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                sealed class C<T>
                {
                }
                class D : {|CS0509:C<int>|}
                {
                }
                """, """
                class C<T>
                {
                }
                class D : C<int>
                {
                }
                """);
        }

        [Fact]
        public async Task NotOfferedForNonSealedClass()
        {
            var code = """
                class C
                {
                }
                class D : C
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task NotOfferedForStaticClass()
        {
            var code = """
                static class C
                {
                }
                class {|CS0709:D|} : C
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task NotOfferedForStruct()
        {
            var code = """
                struct S
                {
                }
                class D : {|CS0509:S|}
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task NotOfferedForDelegate()
        {
            var code = """
                delegate void F();

                class D : {|CS0509:F|}
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task NotOfferedForSealedClassFromMetadata1()
        {
            var code = """
                class D : {|CS0509:string|}
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task NotOfferedForSealedClassFromMetadata2()
        {
            var code = """
                class D : {|CS0509:System.ApplicationId|}
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task RemovedFromAllPartialClassDeclarationsInSameFile()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                public sealed partial class C
                {
                }
                partial class C
                {
                }
                sealed partial class C
                {
                }
                class D : {|CS0509:C|}
                {
                }
                """, """
                public partial class C
                {
                }
                partial class C
                {
                }
                partial class C
                {
                }
                class D : C
                {
                }
                """);
        }

        [Fact]
        public async Task RemovedFromAllPartialClassDeclarationsAcrossFiles()
        {
            var document1 = """
                public sealed partial class C
                {
                }
                """;
            var document2 = """
                partial class C
                {
                }
                sealed partial class C
                {
                }
                """;
            var document3 = """
                class D : {|CS0509:C|}
                {
                }
                """;

            var fixedDocument1 = """
                public partial class C
                {
                }
                """;
            var fixedDocument2 = """
                partial class C
                {
                }
                partial class C
                {
                }
                """;
            var fixedDocument3 = """
                class D : C
                {
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { document1, document2, document3 }
                },
                FixedState =
                {
                    Sources = { fixedDocument1, fixedDocument2, fixedDocument3 }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task RemovedFromClassInVisualBasicProject()
        {
            var csharpDocument = """
                class D : {|CS0509:C|}
                {
                }
                """;
            var vbDocument = """
                public notinheritable class C
                end class
                """;

            var fixedCSharpDocument = """
                class D : C
                {
                }
                """;
            var fixedVBDocument = """
                public class C
                end class
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { csharpDocument },
                    AdditionalProjectReferences = { "Project2" },
                    AdditionalProjects =
                    {
                        ["Project2", LanguageNames.VisualBasic] =
                        {
                            Sources = { vbDocument }
                        }
                    }
                },
                FixedState =
                {
                    Sources = { fixedCSharpDocument },
                    AdditionalProjectReferences = { "Project2" },
                    AdditionalProjects =
                    {
                        ["Project2", LanguageNames.VisualBasic] =
                        {
                            Sources = { fixedVBDocument }
                        }
                    }
                }
            }.RunAsync();
        }
    }
}
