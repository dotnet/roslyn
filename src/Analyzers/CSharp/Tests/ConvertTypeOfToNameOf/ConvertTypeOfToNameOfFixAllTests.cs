// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.ConvertTypeOfToNameOf;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertTypeOfToNameOf
{
    using VerifyCS = CSharpCodeFixVerifier<CSharpConvertTypeOfToNameOfDiagnosticAnalyzer,
        CSharpConvertTypeOfToNameOfCodeFixProvider>;

    public partial class ConvertTypeOfToNameOfTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task FixAllDocumentBasic()
        {
            var input = @"class Test
{
    static void Main()
    {
        var typeName1 = [|typeof(Test).Name|];
        var typeName2 = [|typeof(Test).Name|];
        var typeName3 = [|typeof(Test).Name|];
    }
}
";

            var expected = @"class Test
{
    static void Main()
    {
        var typeName1 = nameof(Test);
        var typeName2 = nameof(Test);
        var typeName3 = nameof(Test);
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task FixAllDocumentVariedSingleLine()
        {
            var input = @"class Test
{
    static void Main()
    {
        var typeName1 = [|typeof(Test).Name|]; var typeName2 = [|typeof(int).Name|]; var typeName3 = [|typeof(System.String).Name|];
    }
}
";

            var expected = @"class Test
{
    static void Main()
    {
        var typeName1 = nameof(Test); var typeName2 = nameof(System.Int32); var typeName3 = nameof(System.String);
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task FixAllDocumentVariedWithUsing()
        {
            var input = @"using System;

class Test
{
    static void Main()
    {
        var typeName1 = [|typeof(Test).Name|];
        var typeName2 = [|typeof(int).Name|];
        var typeName3 = [|typeof(String).Name|];
        var typeName4 = [|typeof(System.Double).Name|];
    }
}
";

            var expected = @"using System;

class Test
{
    static void Main()
    {
        var typeName1 = nameof(Test);
        var typeName2 = nameof(Int32);
        var typeName3 = nameof(String);
        var typeName4 = nameof(Double);
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task FixAllProject()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
class Test1
{
    static void Main()
    {
        var typeName1 = [|typeof(Test1).Name|];
        var typeName2 = [|typeof(Test1).Name|];
        var typeName3 = [|typeof(Test1).Name|];
    }
}
",
                        @"
using System;

class Test2
{
    static void Main()
    {
        var typeName1 = [|typeof(Test1).Name|];
        var typeName2 = [|typeof(int).Name|];
        var typeName3 = [|typeof(System.String).Name|];
        var typeName4 = [|typeof(Double).Name|];
    }
}
"
                    }
                },
                FixedState =
                {
                    Sources =
                    {
                        @"
class Test1
{
    static void Main()
    {
        var typeName1 = nameof(Test1);
        var typeName2 = nameof(Test1);
        var typeName3 = nameof(Test1);
    }
}
",
                        @"
using System;

class Test2
{
    static void Main()
    {
        var typeName1 = nameof(Test1);
        var typeName2 = nameof(Int32);
        var typeName3 = nameof(String);
        var typeName4 = nameof(Double);
    }
}
",
                    }
                }
            }.RunAsync();
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task FixAllSolution()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
class Test1
{
    static void Main()
    {
        var typeName1 = [|typeof(Test1).Name|];
        var typeName2 = [|typeof(Test1).Name|];
        var typeName3 = [|typeof(Test1).Name|];
    }
}
",
                        @"
using System;

class Test2
{
    static void Main()
    {
        var typeName1 = [|typeof(Test1).Name|];
        var typeName2 = [|typeof(int).Name|];
        var typeName3 = [|typeof(System.String).Name|];
        var typeName4 = [|typeof(Double).Name|];
    }
}
"
                    },
                    AdditionalProjects =
                    {
                        ["DependencyProject"] =
                        {
                            Sources =
                            {
                                @"
class Test3
{
    static void Main()
    {
        var typeName2 = [|typeof(int).Name|]; var typeName3 = [|typeof(System.String).Name|];
    }
}
"
                            }
                        }
                    }
                },
                FixedState =
                {
                    Sources =
                    {
                        @"
class Test1
{
    static void Main()
    {
        var typeName1 = nameof(Test1);
        var typeName2 = nameof(Test1);
        var typeName3 = nameof(Test1);
    }
}
",
                        @"
using System;

class Test2
{
    static void Main()
    {
        var typeName1 = nameof(Test1);
        var typeName2 = nameof(Int32);
        var typeName3 = nameof(String);
        var typeName4 = nameof(Double);
    }
}
"
                    },
                    AdditionalProjects =
                    {
                        ["DependencyProject"] =
                        {
                            Sources =
                            {
                                @"
class Test3
{
    static void Main()
    {
        var typeName2 = nameof(System.Int32); var typeName3 = nameof(System.String);
    }
}
"
                            }
                        }
                    }
                }
            }.RunAsync();
        }
    }
}
