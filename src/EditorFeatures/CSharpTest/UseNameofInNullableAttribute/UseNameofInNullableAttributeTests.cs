// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.UseNameofInNullableAttribute;
using Microsoft.CodeAnalysis.CSharp.UseNameofInNullableAttribute;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNameofInNullableAttribute
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUseNameofInNullableAttributeDiagnosticAnalyzer,
        CSharpUseNameofInNullableAttributeCodeFixProvider>;

    public class UseNameofInNullableAttributeTests
    {
        [Fact]
        public async Task TestOnMethod()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        [return: NotNullIfNotNull([|"input"|])]
                        string? M(string? input) => input;
                    }
                    """,
                FixedCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        [return: NotNullIfNotNull(nameof(input))]
                        string? M(string? input) => input;
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTrivia()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        [return: NotNullIfNotNull(/*before*/[|"input"|])/*after*/]
                        string? M(string? input) => input;
                    }
                    """,
                FixedCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        [return: NotNullIfNotNull(/*before*/nameof(input)/*after*/)]
                        string? M(string? input) => input;
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFullAttributeName()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        [return: NotNullIfNotNullAttribute([|"input"|])]
                        string? M(string? input) => input;
                    }
                    """,
                FixedCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        [return: NotNullIfNotNullAttribute(nameof(input))]
                        string? M(string? input) => input;
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNamedArg()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        [return: NotNullIfNotNullAttribute(parameterName: [|"input"|])]
                        string? M(string? input) => input;
                    }
                    """,
                FixedCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        [return: NotNullIfNotNullAttribute(parameterName: nameof(input))]
                        string? M(string? input) => input;
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact]
        public async Task NotBeforeCSharp11()
        {
            var code = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    [return: NotNullIfNotNull("input")]
                    string? M(string? input) => input;
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOnParameter()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        void M([NotNullIfNotNull([|"input"|])] string? input) { }
                    }
                    """,
                FixedCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        void M([NotNullIfNotNull(nameof(input))] string? input) { }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact]
        public async Task TestForProperty()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        string? Prop { get; set; }

                        [MemberNotNullWhen(true, [|"Prop"|])]
                        bool IsInitialized
                        {
                            get
                            {
                                Prop = "";
                                return true;
                            }
                        }
                    }
                    """,
                FixedCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        string? Prop { get; set; }
                    
                        [MemberNotNullWhen(true, nameof(Prop))]
                        bool IsInitialized
                        {
                            get
                            {
                                Prop = "";
                                return true;
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact]
        public async Task TestMultipleArguments()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        string? Prop1 { get; set; }
                        string? Prop2 { get; set; }

                        [MemberNotNull([|"Prop1"|], [|"Prop2"|])]
                        bool IsInitialized
                        {
                            get
                            {
                                Prop1 = "";
                                Prop2 = "";
                                return true;
                            }
                        }
                    }
                    """,
                FixedCode = """
                    using System.Diagnostics.CodeAnalysis;
                    #nullable enable
                    class C
                    {
                        string? Prop1 { get; set; }
                        string? Prop2 { get; set; }
                    
                        [MemberNotNull(nameof(Prop1), nameof(Prop2))]
                        bool IsInitialized
                        {
                            get
                            {
                                Prop1 = "";
                                Prop2 = "";
                                return true;
                            }
                        }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }
    }
}
