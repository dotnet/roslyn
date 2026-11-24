// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.GenerateToStringFromMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateToStringFromMembers;

using VerifyCS = CSharpCodeRefactoringVerifier<
    GenerateToStringFromMembersCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateToString)]
public sealed class GenerateToStringFromMembersTests
{
    [Fact]
    public Task TestRecord_ToString()
        => new VerifyCS.Test
        {
            TestCode = """
            record Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Text;

            record Program
            {
                int a;

                public override string ToString()
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("Program");
                    builder.Append(" { ");
                    if (PrintMembers(builder))
                    {
                        builder.Append(' ');
                    }
                    builder.Append('}');
                    return builder.ToString();
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionEquivalenceKey = FeaturesResources.Generate_ToString,
        }.RunAsync();

    [Fact]
    public Task TestRecord_ToStringAndPrintMembers()
        => new VerifyCS.Test
        {
            TestCode = """
            record Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Runtime.CompilerServices;
            using System.Text;

            record Program
            {
                int a;

                public override string ToString()
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("Program");
                    builder.Append(" { ");
                    if (PrintMembers(builder))
                    {
                        builder.Append(' ');
                    }
                    builder.Append('}');
                    return builder.ToString();
                }

                protected virtual bool PrintMembers(StringBuilder builder)
                {
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    builder.Append("a = ");
                    builder.Append(a.ToString());
                    return true;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = FeaturesResources.Generate_ToString_and_PrintMembers,
        }.RunAsync();

    [Fact]
    public Task TestRecord_ToStringAndPrintMembers_MultipleMembers()
        => new VerifyCS.Test
        {
            TestCode = """
            record Program
            {
                [|int a;
                string b;|]
            }
            """,
            FixedCode = """
            using System.Runtime.CompilerServices;
            using System.Text;

            record Program
            {
                int a;
                string b;

                public override string ToString()
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("Program");
                    builder.Append(" { ");
                    if (PrintMembers(builder))
                    {
                        builder.Append(' ');
                    }
                    builder.Append('}');
                    return builder.ToString();
                }

                protected virtual bool PrintMembers(StringBuilder builder)
                {
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    builder.Append("a = ");
                    builder.Append(a.ToString());
                    builder.Append(", b = ");
                    builder.Append((object)b);
                    return true;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = FeaturesResources.Generate_ToString_and_PrintMembers,
        }.RunAsync();

    [Fact]
    public Task TestSealedRecord_ToStringAndPrintMembers()
        => new VerifyCS.Test
        {
            TestCode = """
            sealed record Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Runtime.CompilerServices;
            using System.Text;

            sealed record Program
            {
                int a;

                public override string ToString()
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("Program");
                    builder.Append(" { ");
                    if (PrintMembers(builder))
                    {
                        builder.Append(' ');
                    }
                    builder.Append('}');
                    return builder.ToString();
                }

                private bool PrintMembers(StringBuilder builder)
                {
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    builder.Append("a = ");
                    builder.Append(a.ToString());
                    return true;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = FeaturesResources.Generate_ToString_and_PrintMembers,
        }.RunAsync();

    [Fact]
    public Task TestRecordStruct_ToStringAndPrintMembers()
        => new VerifyCS.Test
        {
            TestCode = """
            record struct Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Text;

            record struct Program
            {
                int a;

                public override readonly string ToString()
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("Program");
                    builder.Append(" { ");
                    if (PrintMembers(builder))
                    {
                        builder.Append(' ');
                    }
                    builder.Append('}');
                    return builder.ToString();
                }

                private readonly bool PrintMembers(StringBuilder builder)
                {
                    builder.Append("a = ");
                    builder.Append(a.ToString());
                    return true;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = FeaturesResources.Generate_ToString_and_PrintMembers,
        }.RunAsync();

    [Fact]
    public Task TestDerivedRecord_ToStringAndPrintMembers()
        => new VerifyCS.Test
        {
            TestCode = """
            record Base { }

            record Program : Base
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Text;

            record Base { }

            record Program : Base
            {
                int a;

                public override string ToString()
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("Program");
                    builder.Append(" { ");
                    if (PrintMembers(builder))
                    {
                        builder.Append(' ');
                    }
                    builder.Append('}');
                    return builder.ToString();
                }

                protected override bool PrintMembers(StringBuilder builder)
                {
                    if (base.PrintMembers(builder))
                    {
                        builder.Append(", ");
                    }
                    builder.Append("a = ");
                    builder.Append(a.ToString());
                    return true;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = FeaturesResources.Generate_ToString_and_PrintMembers,
        }.RunAsync();

    [Fact]
    public Task TestRecord_ToString_NoSelection()
        => new VerifyCS.Test
        {
            TestCode = """
            record Program
            {
                int a;
                [||]
            }
            """,
            FixedCode = """
            using System.Runtime.CompilerServices;
            using System.Text;

            record Program
            {
                int a;

                public override string ToString()
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("Program");
                    builder.Append(" { ");
                    if (PrintMembers(builder))
                    {
                        builder.Append(' ');
                    }
                    builder.Append('}');
                    return builder.ToString();
                }

                protected virtual bool PrintMembers(StringBuilder builder)
                {
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    builder.Append("a = ");
                    builder.Append(a.ToString());
                    return true;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = FeaturesResources.Generate_ToString_and_PrintMembers,
        }.RunAsync();

    [Fact]
    public Task TestRecord_NoPrintableMembers()
        => new VerifyCS.Test
        {
            TestCode = """
            record Program
            {
                private int a;
                [||]
            }
            """,
            FixedCode = """
            using System.Text;

            record Program
            {
                private int a;

                public override string ToString()
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("Program");
                    builder.Append(" { ");
                    if (PrintMembers(builder))
                    {
                        builder.Append(' ');
                    }
                    builder.Append('}');
                    return builder.ToString();
                }

                protected virtual bool PrintMembers(StringBuilder builder)
                {
                    return false;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = FeaturesResources.Generate_ToString_and_PrintMembers,
        }.RunAsync();

    [Fact]
    public Task TestRecord_PrintMembersOnly()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Text;

            record Program
            {
                [|int a;|]

                public override string ToString()
                {
                    return "Program";
                }
            }
            """,
            FixedCode = """
            using System.Runtime.CompilerServices;
            using System.Text;

            record Program
            {
                int a;

                public override string ToString()
                {
                    return "Program";
                }

                protected virtual bool PrintMembers(StringBuilder builder)
                {
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    builder.Append("a = ");
                    builder.Append(a.ToString());
                    return true;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionEquivalenceKey = FeaturesResources.Generate_PrintMembers,
        }.RunAsync();

    [Fact]
    public Task TestRecord_ToStringOnly()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Text;

            record Program
            {
                [|int a;|]

                protected virtual bool PrintMembers(StringBuilder builder)
                {
                    return true;
                }
            }
            """,
            FixedCode = """
            using System.Text;

            record Program
            {
                int a;

                protected virtual bool PrintMembers(StringBuilder builder)
                {
                    return true;
                }

                public override string ToString()
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("Program");
                    builder.Append(" { ");
                    if (PrintMembers(builder))
                    {
                        builder.Append(' ');
                    }
                    builder.Append('}');
                    return builder.ToString();
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionEquivalenceKey = FeaturesResources.Generate_ToString,
        }.RunAsync();

    [Fact]
    public Task TestRecord_NoActionWhenBothExist()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Text;

            record Program
            {
                [|int a;|]

                public override string ToString()
                {
                    return "Program";
                }

                protected virtual bool PrintMembers(StringBuilder builder)
                {
                    return true;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestClass_NoAction()
        => new VerifyCS.Test
        {
            TestCode = """
            class Program
            {
                [|int a;|]
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();
}
