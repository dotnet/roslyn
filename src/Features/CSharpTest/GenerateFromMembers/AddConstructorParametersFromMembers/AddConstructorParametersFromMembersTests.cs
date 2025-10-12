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
public sealed class AddConstructorParametersFromMembersTests
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
    public Task TestAdd1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")]
    public Task TestProperlyWrapParameters1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")]
    public Task TestProperlyWrapParameters2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")]
    public Task TestProperlyWrapParameters3()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")]
    public Task TestProperlyWrapParameters4()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
    public Task TestAddOptional1()
        => new VerifyCS.Test()
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
    public Task TestAddToConstructorWithMostMatchingParameters1()
        => new VerifyCS.Test()
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
    public Task TestAddOptionalToConstructorWithMostMatchingParameters1()
        => new VerifyCS.Test()
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

    [Fact]
    public Task TestSmartTagDisplayText1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestSmartTagDisplayText2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTuple()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact]
    public Task TestTupleWithNames()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact]
    public Task TestTupleWithDifferentNames()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact]
    public Task TestTupleOptionalCSharp7()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTupleOptional()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTupleOptionalWithNames_CSharp7()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTupleOptionalWithNamesCSharp7()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTupleOptionalWithNames()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTupleOptionalWithDifferentNames()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTupleWithNullable()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact]
    public Task TestTupleWithGenericss()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28775")]
    public Task TestAddParamtersToConstructorBySelectOneMember()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28775")]
    public Task TestParametersAreStillRightIfMembersAreOutOfOrder()
        => VerifyCS.VerifyRefactoringAsync(
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
    public Task TestNormalProperty()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33602")]
    public Task TestConstructorWithNoParameters()
        => VerifyCS.VerifyRefactoringAsync(
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
    public Task TestPartialSelected()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestPartialMultipleSelected()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestPartialMultipleSelected2()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public Task TestMultipleConstructors_FirstofThree()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public Task TestMultipleConstructors_SecondOfThree()
        => new VerifyCS.Test
        {
            TestCode = """
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
            FixedCode = """
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
            """,
            CodeActionIndex = 1,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, int j)"), codeAction.Title)
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public Task TestMultipleConstructors_ThirdOfThree()
        => new VerifyCS.Test
        {
            TestCode = """
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
            FixedCode = """
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
            """,
            CodeActionIndex = 2,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, int j, int k)"), codeAction.Title)
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public Task TestMultipleConstructors_FirstOptionalOfThree()
        => new VerifyCS.Test
        {
            TestCode = """
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
            FixedCode = """
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
            """,
            CodeActionIndex = 3,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i)"), codeAction.Title)
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public Task TestMultipleConstructors_SecondOptionalOfThree()
        => new VerifyCS.Test
        {
            TestCode = """
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
            FixedCode = """
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
            """,
            CodeActionIndex = 4,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, int j)"), codeAction.Title)
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public Task TestMultipleConstructors_ThirdOptionalOfThree()
        => new VerifyCS.Test
        {
            TestCode = """
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
            FixedCode = """
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
            """,
            CodeActionIndex = 5,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, int j, int k)"), codeAction.Title)
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public Task TestMultipleConstructors_OneMustBeOptional()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            FixedCode = """
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
            """,
            CodeActionIndex = 1,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, double j, int k)"), codeAction.Title)
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public Task TestMultipleConstructors_OneMustBeOptional2()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            FixedCode = """
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
            """,
            CodeActionIndex = 3,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int i, double j)"), codeAction.Title)
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public Task TestMultipleConstructors_AllMustBeOptional()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")]
    public Task TestMultipleConstructors_AllMustBeOptional2()
        => new VerifyCS.Test
        {
            TestCode = """
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
            FixedCode = """
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
            """,
            CodeActionIndex = 2,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(string.Format(CodeFixesResources.Add_to_0, "C(int l, double m, int n)"), codeAction.Title)
        }.RunAsync();

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
    public Task TestNoFieldNamingStyle_ParameterPrefixAndSuffix()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                private int [|v|];
                public C()
                {
                }
            }
            """,
            FixedCode = """
            class C
            {
                private int v;
                public C(int p_v_End)
                {
                    v = p_v_End;
                }
            }
            """,
            EditorConfig = ParameterNamesCamelCaseWithPUnderscorePrefixEndUnderscoreSuffixEditorConfig
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public Task TestCommonFieldNamingStyle()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                private int [|t_v|];
                public C()
                {
                }
            }
            """,
            FixedCode = """
            class C
            {
                private int t_v;
                public C(int p_v)
                {
                    t_v = p_v;
                }
            }
            """,
            EditorConfig = ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public Task TestSpecifiedFieldNamingStyle()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                private int [|field_v|];
                public C()
                {
                }
            }
            """,
            FixedCode = """
            class C
            {
                private int field_v;
                public C(int p_v)
                {
                    field_v = p_v;
                }
            }
            """,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig + ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public Task TestSpecifiedAndCommonFieldNamingStyle()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                private int [|field_s_v|];
                public C()
                {
                }
            }
            """,
            FixedCode = """
            class C
            {
                private int field_s_v;
                public C(int p_v)
                {
                    field_s_v = p_v;
                }
            }
            """,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig + ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public Task TestSpecifiedAndCommonFieldNamingStyle2()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                private int [|s_field_v|];
                public C()
                {
                }
            }
            """,
            FixedCode = """
            class C
            {
                private int s_field_v;
                public C(int p_v)
                {
                    s_field_v = p_v;
                }
            }
            """,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig + ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public Task TestBaseNameEmpty()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                private int [|field__End|];
                public C()
                {
                }
            }
            """,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEndUnderscoreSuffixEditorConfig
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public Task TestSomeBaseNamesAreEmpty()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                private int [|field_test_End;
                private int field__End|];
                public C()
                {
                }
            }
            """,
            FixedCode = """
            class C
            {
                private int field_test_End;
                private int field__End;
                public C(int p_test)
                {
                    field_test_End = p_test;
                }
            }
            """,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEndUnderscoreSuffixEditorConfig + ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35775")]
    public Task TestManyCommonPrefixes()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                private int [|______test|];
                public C()
                {
                }
            }
            """,
            FixedCode = """
            class C
            {
                private int ______test;
                public C(int p_test)
                {
                    ______test = p_test;
                }
            }
            """,
            EditorConfig = ParameterNamesCamelCaseWithPUnderscorePrefixEditorConfig
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelection1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelection2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelection3()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelection4()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelection5()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelection6()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelectionMultiVar1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelectionMultiVar2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelectionMultiVar3()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelectionMultiVar4()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelectionMultiVar5()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")]
    public Task TestNonSelectionMultiVar6()
        => new VerifyCS.Test
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
    public Task TestPartialClass1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59292")]
    public Task TestPartialClass2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59292")]
    public Task TestPartialClass3()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60816")]
    public Task TestAddMultipleParametersWithWrapping()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49112")]
    public Task TestAddParameterToExpressionBodiedConstructor()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80564")]
    public Task TestAddParameterToPrimaryConstructor_Property()
        => new VerifyCS.Test
        {
            TestCode =
            """
            public class MyClass(int myProperty)
            {
                public int MyProperty { get; set; } = myProperty;

                [|public int MyProperty1 { get; set; }|]
            }
            """,
            FixedCode =
            """
            public class MyClass(int myProperty, int myProperty1)
            {
                public int MyProperty { get; set; } = myProperty;

                public int MyProperty1 { get; set; } = myProperty1;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80564")]
    public Task TestAddParameterToPrimaryConstructor_Field()
        => new VerifyCS.Test
        {
            TestCode =
            """
            public class MyClass(int myField)
            {
                public int myField = myField;

                [|public int myField1;|]
            }
            """,
            FixedCode =
            """
            public class MyClass(int myField, int myField1)
            {
                public int myField = myField;

                public int myField1 = myField1;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80564")]
    public Task TestAddParameterToPrimaryConstructor_MultipleProperties()
        => new VerifyCS.Test
        {
            TestCode =
            """
            public class MyClass(int a)
            {
                public int A { get; set; } = a;

                [|public int B { get; set; }
                public int C { get; set; }|]
            }
            """,
            FixedCode =
            """
            public class MyClass(int a, int b, int c)
            {
                public int A { get; set; } = a;

                public int B { get; set; } = b;
                public int C { get; set; } = c;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80564")]
    public Task TestAddParameterToPrimaryConstructor_EmptyParameterList()
        => new VerifyCS.Test
        {
            TestCode =
            """
            public class MyClass()
            {
                [|public int MyProperty { get; set; }|]
            }
            """,
            FixedCode =
            """
            public class MyClass(int myProperty)
            {
                public int MyProperty { get; set; } = myProperty;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12
        }.RunAsync();
}
