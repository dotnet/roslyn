// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UsePatternMatching;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseNotPatternDiagnosticAnalyzer,
    CSharpUseNotPatternCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseNotPattern)]
public partial class CSharpUseNotPatternTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50690")]
    public async Task BinaryIsExpression()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object x)
                    {
                        if (!(x [|is|] string))
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object x)
                    {
                        if (x is not string)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task BinaryIsExpression2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object x)
                    {
                        if (!(x [|is|] string /*trailing*/))
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object x)
                    {
                        if (x is not string /*trailing*/)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50690")]
    public async Task ConstantPattern()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object x)
                    {
                        if (!(x [|is|] null))
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object x)
                    {
                        if (x is not null)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64292")]
    public async Task BooleanValueConstantPattern()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(bool x)
                    {
                        if (!(x [|is|] true))
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool x)
                    {
                        if (x is false)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64292")]
    public async Task NonBooleanValueConstantPattern()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object x)
                    {
                        if (!(x [|is|] true))
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object x)
                    {
                        if (x is not true)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46699")]
    public async Task UseNotPattern()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object x)
                    {
                        if (!(x [|is|] string s))
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object x)
                    {
                        if (x is not string s)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72370")]
    public async Task UseNotPattern2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                       public class Program
                       {
                           public class C
                           {
                           }
                           public static void Main()
                           {
                               C C = new();
                               object O = C;
                               
                               if (!(O [|is|] C))
                               {
                               }
                           }
                       }
                       """,
            FixedCode = """
                        public class Program
                        {
                            public class C
                            {
                            }
                            public static void Main()
                            {
                                C C = new();
                                object O = C;
                                
                                if (O is not Program.C)
                                {
                                }
                            }
                        }
                        """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72370")]
    public async Task UseNotPattern3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                       public class Program
                       {
                           public class C
                           {
                           }
                           public static void Main(object O)
                           {
                               if (!(O [|is|] C))
                               {
                               }
                           }
                       }
                       """,
            FixedCode = """
                        public class Program
                        {
                            public class C
                            {
                            }
                            public static void Main(object O)
                            {
                                if (O is not C)
                                {
                                }
                            }
                        }
                        """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task UnavailableInCSharp8()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object x)
                    {
                        if (!(x is string s))
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50690")]
    public async Task BinaryIsObject()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object x)
                    {
                        if (!(x [|is|] object))
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object x)
                    {
                        if (x is null)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50690")]
    public async Task BinaryIsObject2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object x)
                    {
                        if (!(x [|is|] object /*trailing*/))
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object x)
                    {
                        if (x is null /*trailing*/)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task BinaryIsObject3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(object x)
                    {
                        if (!(x [|is|] Object))
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(object x)
                    {
                        if (x is null)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68784")]
    public async Task NotInExpressionTree()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    IQueryable<object> M(IQueryable<object> query)
                    {
                        return query.Where(x => !(x is string));
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70964")]
    public async Task TestMissingOnNullable1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        object o = null;
                        if (!(o is bool?))
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70964")]
    public async Task TestMissingOnNullable2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        object o = null;
                        if (!(o is Nullable<bool>))
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }
}
