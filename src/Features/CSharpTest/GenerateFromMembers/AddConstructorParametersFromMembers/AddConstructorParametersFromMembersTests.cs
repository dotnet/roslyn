// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddConstructorParametersFromMembers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateFromMembers.AddConstructorParameters;

using VerifyCS = CSharpCodeRefactoringVerifier<AddConstructorParametersFromMembersCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
public class AddConstructorParametersFromMembersTests
{
    private const string FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig = """
        [*.cs]
        dotnet_naming_style.field_camel_case.capitalization         = camel_case
        dotnet_naming_style.field_camel_case.required_prefix        = field_
        dotnet_naming_symbols.fields.applicable_kinds               = field
        dotnet_naming_symbols.fields.applicable_accessibilities     = *
        dotnet_naming_rule.fields_should_be_camel_case.severity     = error
        dotnet_naming_rule.fields_should_be_camel_case.symbols      = fields
        dotnet_naming_rule.fields_should_be_camel_case.style        = field_camel_case
        """;

    private const string FieldNamesCamelCaseWithFieldUnderscorePrefixEndUnderscoreSuffixEditorConfig =
        FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig + """

        dotnet_naming_style.field_camel_case.required_suffix        = _End
        """;

    private const string ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig = """

        [*.cs]
        dotnet_naming_style.p_camel_case.capitalization             = camel_case
        dotnet_naming_style.p_camel_case.required_prefix            = p_
        dotnet_naming_symbols.parameters.applicable_kinds           = parameter
        dotnet_naming_symbols.parameters.applicable_accessibilities = *
        dotnet_naming_rule.parameters_should_be_camel_case.severity = error
        dotnet_naming_rule.parameters_should_be_camel_case.symbols  = parameters
        dotnet_naming_rule.parameters_should_be_camel_case.style    = p_camel_case
        """;

    private const string ParameterNamesCamelCaseWithPUnderscorePrefixEndUnderscoreSuffixEditorConfig =
        ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig + """

        dotnet_naming_style.p_camel_case.required_suffix            = _End
        """;

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
    public async Task TestAdd1()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                [|int i;
                string s;|]

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;

                public Program(int i, string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")]
    public async Task TestProperlyWrapParameters1()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                [|int i;
                string s;|]

                public Program(
                    int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;

                public Program(
                    int i, string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")]
    public async Task TestProperlyWrapParameters2()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                [|int i;
                string s;
                bool b;|]

                public Program(
                    int i,
                    string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;
                bool b;

                public Program(
                    int i,
                    string s,
                    bool b)
                {
                    this.i = i;
                    this.s = s;
                    this.b = b;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i, string s)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")]
    public async Task TestProperlyWrapParameters3()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                [|int i;
                string s;
                bool b;|]

                public Program(int i,
                    string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;
                bool b;

                public Program(int i,
                    string s,
                    bool b)
                {
                    this.i = i;
                    this.s = s;
                    this.b = b;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i, string s)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")]
    public async Task TestProperlyWrapParameters4()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                [|int i;
                string s;
                bool b;|]

                public Program(int i,
                               string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;
                bool b;

                public Program(int i,
                               string s,
                               bool b)
                {
                    this.i = i;
                    this.s = s;
                    this.b = b;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i, string s)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
    public async Task TestAddOptional1()
    {
        await new VerifyCS.Test()
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                [|int i;
                string s;|]

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;

                public Program(int i, string s = null)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionIndex = 1,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_optional_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
    public async Task TestAddToConstructorWithMostMatchingParameters1()
    {
        // behavior change with 33603, now all constructors offered
        await new VerifyCS.Test()
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                [|int i;
                string s;
                bool b;|]

                public Program(int i)
                {
                    this.i = i;
                }

                public Program(int i, string s) : this(i)
                {
                    this.s = s;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;
                bool b;

                public Program(int i)
                {
                    this.i = i;
                }

                public Program(int i, string s, bool b) : this(i)
                {
                    this.s = s;
                    this.b = b;
                }
            }
            """,
            CodeActionIndex = 1,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "Program(int i, string s)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
    public async Task TestAddOptionalToConstructorWithMostMatchingParameters1()
    {
        // Behavior change with #33603, now all constructors are offered
        await new VerifyCS.Test()
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                [|int i;
                string s;
                bool b;|]

                public Program(int i)
                {
                    this.i = i;
                }

                public Program(int i, string s) : this(i)
                {
                    this.s = s;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;
                bool b;

                public Program(int i)
                {
                    this.i = i;
                }

                public Program(int i, string s, bool b = false) : this(i)
                {
                    this.s = s;
                    this.b = b;
                }
            }
            """,
            CodeActionIndex = 3,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "Program(int i, string s)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact]
    public async Task TestSmartTagDisplayText1()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                [|bool b;
                HashSet<string> s;|]

                public Program(bool b)
                {
                    this.b = b;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                bool b;
                HashSet<string> s;

                public Program(bool b, HashSet<string> s)
                {
                    this.b = b;
                    this.s = s;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(bool b)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact]
    public async Task TestSmartTagDisplayText2()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                [|bool b;
                HashSet<string> s;|]

                public Program(bool b)
                {
                    this.b = b;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                bool b;
                HashSet<string> s;

                public Program(bool b, HashSet<string> s = null)
                {
                    this.b = b;
                    this.s = s;
                }
            }
            """,
            CodeActionIndex = 1,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_optional_parameters_to_0, "Program(bool b)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact]
    public async Task TestTuple()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class Program
            {
                [|(int, string) i;
                (string, int) s;|]

                public Program((int, string) i)
                {
                    this.i = i;
                }
            }
            """,
            """
            class Program
            {
                (int, string) i;
                (string, int) s;

                public Program((int, string) i, (string, int) s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """);
    }

    [Fact]
    public async Task TestTupleWithNames()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class Program
            {
                [|(int a, string b) i;
                (string c, int d) s;|]

                public Program((int a, string b) i)
                {
                    this.i = i;
                }
            }
            """,
            """
            class Program
            {
                (int a, string b) i;
                (string c, int d) s;

                public Program((int a, string b) i, (string c, int d) s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """);
    }

    [Fact]
    public async Task TestTupleWithDifferentNames()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class Program
            {
                [|(int a, string b) i;
                (string c, int d) s;|]

                public Program((int e, string f) i)
                {
                    this.i = i;
                }
            }
            """,
            """
            class Program
            {
                [|(int a, string b) i;
                (string c, int d) s;|]

                public Program((int e, string f) i, (string c, int d) s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """);
    }

    [Fact]
    public async Task TestTupleOptionalCSharp7()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            class Program
            {
                [|(int, string) i;
                (string, int) s;|]

                public Program((int, string) i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            class Program
            {
                (int, string) i;
                (string, int) s;

                public Program((int, string) i, (string, int) s = default((string, int)))
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp7
        }.RunAsync();
    }

    [Fact]
    public async Task TestTupleOptional()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            class Program
            {
                [|(int, string) i;
                (string, int) s;|]

                public Program((int, string) i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            class Program
            {
                (int, string) i;
                (string, int) s;

                public Program((int, string) i, (string, int) s = default)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionIndex = 1
        }.RunAsync();
    }

    [Fact]
    public async Task TestTupleOptionalWithNames_CSharp7()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            class Program
            {
                [|(int a, string b) i;
                (string c, int d) s;|]

                public Program((int a, string b) i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            class Program
            {
                (int a, string b) i;
                (string c, int d) s;

                public Program((int a, string b) i, (string c, int d) s = default((string c, int d)))
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7,
            CodeActionIndex = 1
        }.RunAsync();
    }

    [Fact]
    public async Task TestTupleOptionalWithNamesCSharp7()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            class Program
            {
                [|(int a, string b) i;
                (string c, int d) s;|]

                public Program((int a, string b) i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            class Program
            {
                (int a, string b) i;
                (string c, int d) s;

                public Program((int a, string b) i, (string c, int d) s = default((string c, int d)))
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp7
        }.RunAsync();
    }

    [Fact]
    public async Task TestTupleOptionalWithNames()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            class Program
            {
                [|(int a, string b) i;
                (string c, int d) s;|]

                public Program((int a, string b) i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            class Program
            {
                (int a, string b) i;
                (string c, int d) s;

                public Program((int a, string b) i, (string c, int d) s = default)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionIndex = 1
        }.RunAsync();
    }

    [Fact]
    public async Task TestTupleOptionalWithDifferentNames()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            class Program
            {
                [|(int a, string b) i;
                (string c, int d) s;|]

                public Program((int e, string f) i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            class Program
            {
                [|(int a, string b) i;
                (string c, int d) s;|]

                public Program((int e, string f) i, (string c, int d) s = default)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionIndex = 1
        }.RunAsync();
    }

    [Fact]
    public async Task TestTupleWithNullable()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class Program
            {
                [|(int?, bool?) i;
                (byte?, long?) s;|]

                public Program((int?, bool?) i)
                {
                    this.i = i;
                }
            }
            """,
            """
            class Program
            {
                (int?, bool?) i;
                (byte?, long?) s;

                public Program((int?, bool?) i, (byte?, long?) s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """);
    }

    [Fact]
    public async Task TestTupleWithGenericss()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                [|(List<int>, List<bool>) i;
                (List<byte>, List<long>) s;|]

                public Program((List<int>, List<bool>) i)
                {
                    this.i = i;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                (List<int>, List<bool>) i;
                (List<byte>, List<long>) s;

                public Program((List<int>, List<bool>) i, (List<byte>, List<long>) s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28775")]
    public async Task TestAddParamtersToConstructorBySelectOneMember()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            using System.Collections.Generic;

            class C
            {
                int i;
                [|(List<byte>, List<long>) s;|]
                int j;

                public C(int i, int j)
                {
                    this.i = i;
                    this.j = j;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                int i;
                (List<byte>, List<long>) s;
                int j;

                public C(int i, int j, (List<byte>, List<long>) s)
                {
                    this.i = i;
                    this.j = j;
                    this.s = s;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28775")]
    public async Task TestParametersAreStillRightIfMembersAreOutOfOrder()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [|int i;
                int k;
                int j;|]

                public C(int i, int j)
                {
                    this.i = i;
                    this.j = j;
                }
            }
            """,
            """
            class C
            {
                int i;
                int k;
                int j;

                public C(int i, int j, int k)
                {
                    this.i = i;
                    this.j = j;
                    this.k = k;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28775")]
    public async Task TestMissingIfFieldsAlreadyExistingInConstructor()
    {
        var source =
            """
            class C
            {
                [|string _barBar;
                int fooFoo;|]
                public C(string barBar, int fooFoo)
                {
                }
            }
            """;
        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28775")]
    public async Task TestMissingIfPropertyAlreadyExistingInConstructor()
    {
        var source =
            """
            class C
            {
                [|string bar;
                int HelloWorld { get; set; }|]
                public C(string bar, int helloWorld)
                {
                }
            }
            """;
        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28775")]
    public async Task TestNormalProperty()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [|int i;
                int Hello { get; set; }|]
                public C(int i)
                {
                }
            }
            """,
            """
            class C
            {
                int i;
                int Hello { get; set; }
                public C(int i, int hello)
                {
                    Hello = hello;
                }
            }
            """
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33602")]
    public async Task TestConstructorWithNoParameters()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [|int i;
                int Hello { get; set; }|]
                public C()
                {
                }
            }
            """,
            """
            class C
            {
                int i;
                int Hello { get; set; }
                public C(int i, int hello)
                {
                    this.i = i;
                    Hello = hello;
                }
            }
            """
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33602")]
    public async Task TestDefaultConstructor()
    {
        var source =
            """
            class C
            {
                [|int i;|]
                int Hello { get; set; }
            }
            """;
        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public async Task TestPartialSelected()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                int i;
                int [|j|];
                public C(int i)
                {
                }
            }
            """,
            """
            class C
            {
                int i;
                int j;
                public C(int i, int j)
                {
                    this.j = j;
                }
            }
            """
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public async Task TestPartialMultipleSelected()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                int i;
                int [|j;
                int k|];
                public C(int i)
                {
                }
            }
            """,
            """
            class C
            {
                int i;
                int j;
                int k;
                public C(int i, int j, int k)
                {
                    this.j = j;
                    this.k = k;
                }
            }
            """
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public async Task TestPartialMultipleSelected2()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                int i;
                int [|j;
                int |]k;
                public C(int i)
                {
                }
            }
            """,
            """
            class C
            {
                int i;
                int j;
                int k;
                public C(int i, int j)
                {
                    this.j = j;
                }
            }
            """
        );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public async Task TestMultipleConstructors_FirstofThree()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            class C
            {
                int [|l|];
                public C(int i)
                {
                }
                public C(int i, int j)
                {
                }
                public C(int i, int j, int k)
                {
                }
            }
            """,
            FixedCode =
            """
            class C
            {
                int l;
                public C(int i, int l)
                {
                    this.l = l;
                }
                public {|CS0111:C|}(int i, int j)
                {
                }
                public C(int i, int j, int k)
                {
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public async Task TestMultipleConstructors_SecondOfThree()
    {
        var source =
            """
            class C
            {
                int [|l|];
                public C(int i)
                {
                }
                public C(int i, int j)
                {
                }
                public C(int i, int j, int k)
                {
                }
            }
            """;
        var expected =
            """
            class C
            {
                int l;
                public C(int i)
                {
                }
                public C(int i, int j, int l)
                {
                    this.l = l;
                }
                public {|CS0111:C|}(int i, int j, int k)
                {
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            CodeActionIndex = 1,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, int j)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public async Task TestMultipleConstructors_ThirdOfThree()
    {
        var source =
            """
            class C
            {
                int [|l|];
                public C(int i)
                {
                }
                public C(int i, int j)
                {
                }
                public C(int i, int j, int k)
                {
                }
            }
            """;

        var expected =
            """
            class C
            {
                int l;
                public C(int i)
                {
                }
                public C(int i, int j)
                {
                }
                public C(int i, int j, int k, int l)
                {
                    this.l = l;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            CodeActionIndex = 2,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, int j, int k)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public async Task TestMultipleConstructors_FirstOptionalOfThree()
    {
        var source =
            """
            class C
            {
                int [|l|];
                public C(int i)
                {
                }
                public C(int i, int j)
                {
                }
                public C(int i, int j, int k)
                {
                }
            }
            """;
        var expected =
            """
            class C
            {
                int l;
                public C(int i, int l = 0)
                {
                    this.l = l;
                }
                public {|CS0111:C|}(int i, int j)
                {
                }
                public C(int i, int j, int k)
                {
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            CodeActionIndex = 3,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public async Task TestMultipleConstructors_SecondOptionalOfThree()
    {
        var source =
            """
            class C
            {
                int [|l|];
                public C(int i)
                {
                }
                public C(int i, int j)
                {
                }
                public C(int i, int j, int k)
                {
                }
            }
            """;
        var expected =
            """
            class C
            {
                int [|l|];
                public C(int i)
                {
                }
                public C(int i, int j, int l = 0)
                {
                    this.l = l;
                }
                public {|CS0111:C|}(int i, int j, int k)
                {
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            CodeActionIndex = 4,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, int j)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public async Task TestMultipleConstructors_ThirdOptionalOfThree()
    {
        var source =
            """
            class C
            {
                int [|l|];
                public C(int i)
                {
                }
                public C(int i, int j)
                {
                }
                public C(int i, int j, int k)
                {
                }
            }
            """;
        var expected =
            """
            class C
            {
                int [|l|];
                public C(int i)
                {
                }
                public C(int i, int j)
                {
                }
                public C(int i, int j, int k, int l = 0)
                {
                    this.l = l;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            CodeActionIndex = 5,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, int j, int k)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public async Task TestMultipleConstructors_OneMustBeOptional()
    {
        var source =
            """
            class C
            {
                int [|l|];

                // index 0 as required
                // index 2 as optional
                public C(int i)
                {
                }

                // index 3 as optional
                public C(int i, double j = 0)
                {
                }

                // index 1 as required
                // index 4 as optional
                public C(int i, double j, int k)
                {
                }
            }
            """;
        var expected =
            """
            class C
            {
                int [|l|];

                // index 0 as required
                // index 2 as optional
                public C(int i)
                {
                }

                // index 3 as optional
                public C(int i, double j = 0)
                {
                }

                // index 1 as required
                // index 4 as optional
                public C(int i, double j, int k, int l)
                {
                    this.l = l;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            CodeActionIndex = 1,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, double j, int k)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public async Task TestMultipleConstructors_OneMustBeOptional2()
    {
        var source =
            """
            class C
            {
                int [|l|];

                // index 0, and 2 as optional
                public C(int i)
                {
                }

                // index 3 as optional
                public C(int i, double j = 0)
                {
                }

                // index 1, and 4 as optional
                public C(int i, double j, int k)
                {
                }
            }
            """;
        var expected =
            """
            class C
            {
                int [|l|];

                // index 0, and 2 as optional
                public C(int i)
                {
                }

                // index 3 as optional
                public C(int i, double j = 0, int l = 0)
                {
                    this.l = l;
                }

                // index 1, and 4 as optional
                public {|CS0111:C|}(int i, double j, int k)
                {
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            CodeActionIndex = 3,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, double j)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public async Task TestMultipleConstructors_AllMustBeOptional()
    {
        await new VerifyCS.Test
        {
            TestCode =
"""
 class C
 {
     int [|p|];
     public C(int i = 0)
     {
     }
     public C(double j, int k = 0)
     {
     }
     public C(int l, double m, int n = 0)
     {
     }
 }
 """,
            FixedCode =
            """
            class C
            {
                int [|p|];
                public C(int i = 0, int p = 0)
                {
                    this.p = p;
                }
                public C(double j, int k = 0)
                {
                }
                public C(int l, double m, int n = 0)
                {
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public async Task TestMultipleConstructors_AllMustBeOptional2()
    {
        var source =
            """
            class C
            {
                int [|p|];
                public C(int i = 0)
                {
                }
                public C(double j, int k = 0)
                {
                }
                public C(int l, double m, int n = 0)
                {
                }
            }
            """;
        var expected =
            """
            class C
            {
                int [|p|];
                public C(int i = 0)
                {
                }
                public C(double j, int k = 0)
                {
                }
                public C(int l, double m, int n = 0, int p = 0)
                {
                    this.p = p;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            CodeActionIndex = 2,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int l, double m, int n)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33623")]
    public async Task TestDeserializationConstructor()
    {
        var source =
            """
            using System;
            using System.Runtime.Serialization;

            class C : {|CS0535:ISerializable|}
            {
                int [|i|];

                private C(SerializationInfo info, StreamingContext context)
                {
                }
            }
            """;
        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public async Task TestNoFieldNamingStyle_ParameterPrefixAndSuffix()
    {
        var source =
            """
            class C
            {
                private int [|v|];
                public C()
                {
                }
            }
            """;

        var expected =
            """
            class C
            {
                private int v;
                public C(int p_v_End)
                {
                    v = p_v_End;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            EditorConfig = ParameterNamesCamelCaseWithPUnderscorePrefixEndUnderscoreSuffixEditorConfig
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public async Task TestCommonFieldNamingStyle()
    {
        var source =
            """
            class C
            {
                private int [|t_v|];
                public C()
                {
                }
            }
            """;

        var expected =
            """
            class C
            {
                private int t_v;
                public C(int p_v)
                {
                    t_v = p_v;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            EditorConfig = ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public async Task TestSpecifiedFieldNamingStyle()
    {
        var source =
            """
            class C
            {
                private int [|field_v|];
                public C()
                {
                }
            }
            """;

        var expected =
            """
            class C
            {
                private int field_v;
                public C(int p_v)
                {
                    field_v = p_v;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig + ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public async Task TestSpecifiedAndCommonFieldNamingStyle()
    {
        var source =
            """
            class C
            {
                private int [|field_s_v|];
                public C()
                {
                }
            }
            """;

        var expected =
            """
            class C
            {
                private int field_s_v;
                public C(int p_v)
                {
                    field_s_v = p_v;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig + ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public async Task TestSpecifiedAndCommonFieldNamingStyle2()
    {
        var source =
            """
            class C
            {
                private int [|s_field_v|];
                public C()
                {
                }
            }
            """;

        var expected =
            """
            class C
            {
                private int s_field_v;
                public C(int p_v)
                {
                    s_field_v = p_v;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig + ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public async Task TestBaseNameEmpty()
    {
        var source =
            """
            class C
            {
                private int [|field__End|];
                public C()
                {
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEndUnderscoreSuffixEditorConfig
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public async Task TestSomeBaseNamesAreEmpty()
    {
        var source =
            """
            class C
            {
                private int [|field_test_End;
                private int field__End|];
                public C()
                {
                }
            }
            """;

        var expected =
            """
            class C
            {
                private int field_test_End;
                private int field__End;
                public C(int p_test)
                {
                    field_test_End = p_test;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEndUnderscoreSuffixEditorConfig + ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public async Task TestManyCommonPrefixes()
    {
        var source =
            """
            class C
            {
                private int [|______test|];
                public C()
                {
                }
            }
            """;

        var expected =
            """
            class C
            {
                private int ______test;
                public C(int p_test)
                {
                    ______test = p_test;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = expected,
            EditorConfig = ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelection1()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
              [||]  string s;

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;

                public Program(int i, string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelection2()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                [||]string s;

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;

                public Program(int i, string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelection3()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string [||]s;

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;

                public Program(int i, string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelection4()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s[||];

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;

                public Program(int i, string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelection5()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;[||]

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;

                public Program(int i, string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelection6()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s; [||]

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s;

                public Program(int i, string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelectionMultiVar1()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                [||]string s, t;

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s, t;

                public Program(int i, string s, string t)
                {
                    this.i = i;
                    this.s = s;
                    this.t = t;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelectionMultiVar2()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s, t;[||]

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s, t;

                public Program(int i, string s, string t)
                {
                    this.i = i;
                    this.s = s;
                    this.t = t;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelectionMultiVar3()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string [||]s, t;

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s, t;

                public Program(int i, string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelectionMultiVar4()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s[||], t;

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s, t;

                public Program(int i, string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelectionMultiVar5()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s, [||]t;

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s, t;

                public Program(int i, string t)
                {
                    this.i = i;
                    this.t = t;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelectionMultiVar6()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s, t[||];

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s, t;

                public Program(int i, string t)
                {
                    this.i = i;
                    this.t = t;
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "Program(int i)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelectionMissing1()
    {
        var source =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                [||]
                string s, t;

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """;
        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelectionMissing2()
    {
        var source =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                s[||]tring s, t;

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """;
        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelectionMissing3()
    {
        var source =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string[||] s, t;

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """;
        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public async Task TestNonSelectionMissing4()
    {
        var source =
            """
            using System.Collections.Generic;

            class Program
            {
                int i;
                string s,[||] t;

                public Program(int i)
                {
                    this.i = i;
                }
            }
            """;
        await VerifyCS.VerifyRefactoringAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59292")]
    public async Task TestPartialClass1()
    {
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        private int [|_v|];
                    }
                    """,
                    """
                    partial class C
                    {
                        public C()
                        {
                        }
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        private int _v;
                    }
                    """,
                    """
                    partial class C
                    {
                        public C(int v)
                        {
                            _v = v;
                        }
                    }
                    """
                }
            }
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59292")]
    public async Task TestPartialClass2()
    {
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        private int [|_v|];

                        public C()
                        {
                        }
                    }
                    """,
                    """
                    partial class C
                    {
                        public C(object goo)
                        {
                        }
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        private int _v;

                        public C()
                        {
                        }
                    }
                    """,
                    """
                    partial class C
                    {
                        public C(object goo, int v)
                        {
                            _v = v;
                        }
                    }
                    """
                }
            },
            CodeActionIndex = 1
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59292")]
    public async Task TestPartialClass3()
    {
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        private int [|_v|];
                    }
                    """,
                    """
                    partial class C
                    {
                        public C()
                        {
                        }
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        private int _v;
                    }
                    """,
                    """
                    partial class C
                    {
                        public C(int v = 0)
                        {
                            _v = v;
                        }
                    }
                    """
                }
            },
            CodeActionIndex = 1
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60816")]
    public async Task TestAddMultipleParametersWithWrapping()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            namespace M
            {
                public class C
                {
                    public int original { get; }

                    public int original2 { get; }

                    [|public int a1 { get; }

                    public int a2 { get; }|]

                    public C(
                        int original,
                        int original2)
                    {
                        this.original = original;
                        this.original2 = original2;
                    }
                }
            }
            """,
            FixedCode =
            """
            namespace M
            {
                public class C
                {
                    public int original { get; }

                    public int original2 { get; }

                    public int a1 { get; }

                    public int a2 { get; }

                    public C(
                        int original,
                        int original2,
                        int a1,
                        int a2)
                    {
                        this.original = original;
                        this.original2 = original2;
                        this.a1 = a1;
                        this.a2 = a2;
                    }
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(FeaturesResources.Add_parameters_to_0, "C(int original, int original2)"), codeAction.Title)
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49112")]
    public async Task TestAddParameterToExpressionBodiedConstructor()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System.Collections.Generic;

            class C
            {
                int x;
                [|int y;|]

                public C(int x) => this.x = x;
            }
            """,
            FixedCode =
            """
            using System.Collections.Generic;

            class C
            {
                int x;
                int y;

                public C(int x, int y)
                {
                    this.x = x;
                    this.y = y;
                }
            }
            """,
        }.RunAsync();
    }
}
