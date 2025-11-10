// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateEqualsAndGetHashCodeFromMembers;

using VerifyCS = CSharpCodeRefactoringVerifier<
    GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
public sealed class GenerateEqualsAndGetHashCodeFromMembersTests
{
    private sealed class TestWithDialog : VerifyCS.Test
    {
        private static readonly TestComposition s_composition =
            EditorTestCompositions.EditorFeatures.AddParts(typeof(TestPickMembersService));

        public ImmutableArray<string> MemberNames;
        public Action<ImmutableArray<PickMembersOption>> OptionsCallback;

        protected override Task<Workspace> CreateWorkspaceImplAsync()
        {
            // If we're a dialog test, then mixin our mock and initialize its values to the ones the test asked for.
            var workspace = new AdhocWorkspace(s_composition.GetHostServices());

            var service = (TestPickMembersService)workspace.Services.GetService<IPickMembersService>();
            service.MemberNames = MemberNames;
            service.OptionsCallback = OptionsCallback;

            return Task.FromResult<Workspace>(workspace);
        }
    }

    private static OptionsCollection PreferImplicitTypeWithInfo()
        => new(LanguageNames.CSharp)
        {
            { CSharpCodeStyleOptions.VarElsewhere, true, NotificationOption2.Suggestion },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, true, NotificationOption2.Suggestion },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, true, NotificationOption2.Suggestion },
        };

    private static OptionsCollection PreferExplicitTypeWithInfo()
        => new(LanguageNames.CSharp)
        {
            { CSharpCodeStyleOptions.VarElsewhere, false, NotificationOption2.Suggestion },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, false, NotificationOption2.Suggestion },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, false, NotificationOption2.Suggestion },
        };

    internal static void EnableOption(ImmutableArray<PickMembersOption> options, string id)
    {
        var option = options.FirstOrDefault(o => o.Id == id);
        option?.Value = true;
    }

    [Fact]
    public Task TestEqualsSingleField()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                int a;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           a == program.a;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestEqualsSingleField_CSharp7()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                int a;

                public override bool Equals(object obj)
                {
                    return obj is Program program &&
                           a == program.a;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39916")]
    public Task TestEqualsSingleField_PreferExplicitType()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                int a;

                public override bool Equals(object obj)
                {
                    Program program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           a == program.a;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferExplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestReferenceIEquatable()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Collections.Generic;

            class S : {|CS0535:IEquatable<S>|} { }

            class Program
            {
                [|S a;|]
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;

            class S : {|CS0535:IEquatable<S>|} { }

            class Program
            {
                S a;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           EqualityComparer<S>.Default.Equals(a, program.a);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestNullableReferenceIEquatable()
        => new VerifyCS.Test
        {
            TestCode = """
            #nullable enable

            using System;
            using System.Collections.Generic;

            class S : {|CS0535:IEquatable<S>|} { }

            class Program
            {
                [|S? a;|]
            }
            """,
            FixedCode = """
            #nullable enable

            using System;
            using System.Collections.Generic;

            class S : {|CS0535:IEquatable<S>|} { }

            class Program
            {
                S? a;

                public override bool Equals(object? obj)
                {
                    return obj is Program program &&
                           EqualityComparer<S?>.Default.Equals(a, program.a);
                }

                public override int GetHashCode()
                {
                    return -1757793268 + EqualityComparer<S?>.Default.GetHashCode(a);
                }
            }
            """,
            CodeActionIndex = 1,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestValueIEquatable()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Collections.Generic;

            struct S : {|CS0535:IEquatable<S>|} { }

            class Program
            {
                [|S a;|]
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;

            struct S : {|CS0535:IEquatable<S>|} { }

            class Program
            {
                S a;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           a.Equals(program.a);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestEqualsLongName()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class ReallyLongName
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class ReallyLongName
            {
                int a;

                public override bool Equals(object obj)
                {
                    var name = obj as ReallyLongName;
                    return !ReferenceEquals(name, null) &&
                           a == name.a;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestEqualsKeywordName()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class ReallyLongLong
            {
                [|long a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class ReallyLongLong
            {
                long a;

                public override bool Equals(object obj)
                {
                    var @long = obj as ReallyLongLong;
                    return !ReferenceEquals(@long, null) &&
                           a == @long.a;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestEqualsProperty()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class ReallyLongName
            {
                [|int a;

                string B { get; }|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class ReallyLongName
            {
                int a;

                string B { get; }

                public override bool Equals(object obj)
                {
                    var name = obj as ReallyLongName;
                    return !ReferenceEquals(name, null) &&
                           a == name.a &&
                           B == name.B;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestEqualsBaseTypeWithNoEquals()
        => new VerifyCS.Test
        {
            TestCode = """
            class Base
            {
            }

            class Program : Base
            {
                [|int i;|]
            }
            """,
            FixedCode = """
            class Base
            {
            }

            class Program : Base
            {
                int i;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           i == program.i;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestEqualsBaseWithOverriddenEquals()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Base
            {
                public override bool Equals(object o)
                {
                    return false;
                }
            }

            class Program : Base
            {
                [|int i;

                string S { get; }|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Base
            {
                public override bool Equals(object o)
                {
                    return false;
                }
            }

            class Program : Base
            {
                int i;

                string S { get; }

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           base.Equals(obj) &&
                           i == program.i &&
                           S == program.S;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestEqualsOverriddenDeepBase()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Base
            {
                public override bool Equals(object o)
                {
                    return false;
                }
            }

            class Middle : Base
            {
            }

            class Program : Middle
            {
                [|int i;

                string S { get; }|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Base
            {
                public override bool Equals(object o)
                {
                    return false;
                }
            }

            class Middle : Base
            {
            }

            class Program : Middle
            {
                int i;

                string S { get; }

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           base.Equals(obj) &&
                           i == program.i &&
                           S == program.S;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestEqualsStruct()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System.Collections.Generic;

            struct ReallyLongName
            {
                [|int i;

                string S { get; }|]
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            struct ReallyLongName : IEquatable<ReallyLongName>
            {
                int i;

                string S { get; }

                public override bool Equals(object obj)
                {
                    return obj is ReallyLongName name && Equals(name);
                }

                public bool Equals(ReallyLongName other)
                {
                    return i == other.i &&
                           S == other.S;
                }

                public static bool operator ==(ReallyLongName left, ReallyLongName right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(ReallyLongName left, ReallyLongName right)
                {
                    return !(left == right);
                }
            }
            """);

    [Fact]
    public Task TestEqualsStructCSharpLatest()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System.Collections.Generic;

            struct ReallyLongName
            {
                [|int i;

                string S { get; }|]
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            struct ReallyLongName : IEquatable<ReallyLongName>
            {
                int i;

                string S { get; }

                public override bool Equals(object obj)
                {
                    return obj is ReallyLongName name && Equals(name);
                }

                public bool Equals(ReallyLongName other)
                {
                    return i == other.i &&
                           S == other.S;
                }

                public static bool operator ==(ReallyLongName left, ReallyLongName right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(ReallyLongName left, ReallyLongName right)
                {
                    return !(left == right);
                }
            }
            """);

    [Fact]
    public Task TestEqualsStructAlreadyImplementsIEquatable()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System;
            using System.Collections.Generic;

            struct ReallyLongName : {|CS0535:IEquatable<ReallyLongName>|}
            {
                [|int i;

                string S { get; }|]
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            struct ReallyLongName : {|CS0535:IEquatable<ReallyLongName>|}
            {
                int i;

                string S { get; }

                public override bool Equals(object obj)
                {
                    return obj is ReallyLongName name &&
                           i == name.i &&
                           S == name.S;
                }

                public static bool operator ==(ReallyLongName left, ReallyLongName right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(ReallyLongName left, ReallyLongName right)
                {
                    return !(left == right);
                }
            }
            """);

    [Fact]
    public Task TestEqualsStructAlreadyHasOperators()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System;
            using System.Collections.Generic;

            struct ReallyLongName
            {
                [|int i;

                string S { get; }|]

                public static bool operator ==(ReallyLongName left, ReallyLongName right) => false;
                public static bool operator !=(ReallyLongName left, ReallyLongName right) => false;
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            struct ReallyLongName : IEquatable<ReallyLongName>
            {
                int i;

                string S { get; }

                public override bool Equals(object obj)
                {
                    return obj is ReallyLongName name && Equals(name);
                }

                public bool Equals(ReallyLongName other)
                {
                    return i == other.i &&
                           S == other.S;
                }

                public static bool operator ==(ReallyLongName left, ReallyLongName right) => false;
                public static bool operator !=(ReallyLongName left, ReallyLongName right) => false;
            }
            """);

    [Fact]
    public Task TestEqualsStructAlreadyImplementsIEquatableAndHasOperators()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System;
            using System.Collections.Generic;

            struct ReallyLongName : {|CS0535:IEquatable<ReallyLongName>|}
            {
                [|int i;

                string S { get; }|]

                public static bool operator ==(ReallyLongName left, ReallyLongName right) => false;
                public static bool operator !=(ReallyLongName left, ReallyLongName right) => false;
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            struct ReallyLongName : {|CS0535:IEquatable<ReallyLongName>|}
            {
                int i;

                string S { get; }

                public override bool Equals(object obj)
                {
                    return obj is ReallyLongName name &&
                           i == name.i &&
                           S == name.S;
                }

                public static bool operator ==(ReallyLongName left, ReallyLongName right) => false;
                public static bool operator !=(ReallyLongName left, ReallyLongName right) => false;
            }
            """);

    [Fact]
    public Task TestEqualsGenericType()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;
            class Program<T>
            {
                [|int i;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;
            class Program<T>
            {
                int i;

                public override bool Equals(object obj)
                {
                    var program = obj as Program<T>;
                    return !ReferenceEquals(program, null) &&
                           i == program.i;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestEqualsNullableContext()
        => VerifyCS.VerifyRefactoringAsync(
            """
            #nullable enable

            class Program
            {
                [|int a;|]
            }
            """,
            """
            #nullable enable

            class Program
            {
                int a;

                public override bool Equals(object? obj)
                {
                    return obj is Program program &&
                           a == program.a;
                }
            }
            """);

    [Fact]
    public Task TestGetHashCodeSingleField1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                [|int i;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                int i;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           i == program.i;
                }

                public override int GetHashCode()
                {
                    return 165851236 + i.GetHashCode();
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestGetHashCodeSingleField2()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                [|int j;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                int j;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           j == program.j;
                }

                public override int GetHashCode()
                {
                    return 1424088837 + j.GetHashCode();
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestGetHashCodeWithBaseHashCode1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Base {
                public override int GetHashCode() => 0;
            }

            class Program : Base
            {
                [|int j;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Base {
                public override int GetHashCode() => 0;
            }

            class Program : Base
            {
                int j;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           j == program.j;
                }

                public override int GetHashCode()
                {
                    var hashCode = 339610899;
                    hashCode = hashCode * -1521134295 + base.GetHashCode();
                    hashCode = hashCode * -1521134295 + j.GetHashCode();
                    return hashCode;
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestGetHashCodeWithBaseHashCode2()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Base {
                public override int GetHashCode() => 0;
            }

            class Program : Base
            {
                int j;
                [||]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Base {
                public override int GetHashCode() => 0;
            }

            class Program : Base
            {
                int j;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null);
                }

                public override int GetHashCode()
                {
                    return 624022166 + base.GetHashCode();
                }
            }
            """,
            CodeActionIndex = 1,
            MemberNames = [],
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestGetHashCodeSingleField_CodeStyle1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                [|int i;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                int i;

                public override bool Equals(object obj)
                {
                    Program program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           i == program.i;
                }

                public override int GetHashCode() => 165851236 + i.GetHashCode();
            }
            """,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
            },
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
        }.RunAsync();

    [Fact]
    public Task TestGetHashCodeTypeParameter()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program<T>
            {
                [|T i;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program<T>
            {
                T i;

                public override bool Equals(object obj)
                {
                    var program = obj as Program<T>;
                    return !ReferenceEquals(program, null) &&
                           EqualityComparer<T>.Default.Equals(i, program.i);
                }

                public override int GetHashCode()
                {
                    return 165851236 + EqualityComparer<T>.Default.GetHashCode(i);
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestGetHashCodeGenericType()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program<T>
            {
                [|Program<T> i;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program<T>
            {
                Program<T> i;

                public override bool Equals(object obj)
                {
                    var program = obj as Program<T>;
                    return !ReferenceEquals(program, null) &&
                           EqualityComparer<Program<T>>.Default.Equals(i, program.i);
                }

                public override int GetHashCode()
                {
                    return 165851236 + EqualityComparer<Program<T>>.Default.GetHashCode(i);
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestGetHashCodeMultipleMembers()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                [|int i;

                string S { get; }|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                int i;

                string S { get; }

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           i == program.i &&
                           S == program.S;
                }

                public override int GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestSmartTagText1()
        => new VerifyCS.Test
        {
            TestCode = """
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
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                bool b;
                HashSet<string> s;

                public Program(bool b)
                {
                    this.b = b;
                }

                public override bool Equals(object obj)
                {
                    return obj is Program program &&
                           b == program.b &&
                           EqualityComparer<HashSet<string>>.Default.Equals(s, program.s);
                }
            }
            """,
            CodeActionEquivalenceKey = FeaturesResources.Generate_Equals,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Generate_Equals, codeAction.Title),
        }.RunAsync();

    [Fact]
    public Task TestSmartTagText2()
        => new VerifyCS.Test
        {
            TestCode = """
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
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                bool b;
                HashSet<string> s;

                public Program(bool b)
                {
                    this.b = b;
                }

                public override bool Equals(object obj)
                {
                    return obj is Program program &&
                           b == program.b &&
                           EqualityComparer<HashSet<string>>.Default.Equals(s, program.s);
                }

                public override int GetHashCode()
                {
                    int hashCode = -666523601;
                    hashCode = hashCode * -1521134295 + b.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<HashSet<string>>.Default.GetHashCode(s);
                    return hashCode;
                }
            }
            """,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = FeaturesResources.Generate_Equals_and_GetHashCode,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Generate_Equals_and_GetHashCode, codeAction.Title),
        }.RunAsync();

    [Fact]
    public Task TestSmartTagText3()
        => new VerifyCS.Test
        {
            TestCode = """
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
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                bool b;
                HashSet<string> s;

                public Program(bool b)
                {
                    this.b = b;
                }

                public override bool Equals(object obj)
                {
                    return obj is Program program &&
                           b == program.b &&
                           EqualityComparer<HashSet<string>>.Default.Equals(s, program.s);
                }

                public override int GetHashCode()
                {
                    int hashCode = -666523601;
                    hashCode = hashCode * -1521134295 + b.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<HashSet<string>>.Default.GetHashCode(s);
                    return hashCode;
                }
            }
            """,
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = FeaturesResources.Generate_Equals_and_GetHashCode,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Generate_Equals_and_GetHashCode, codeAction.Title),
        }.RunAsync();

    [Fact]
    public Task Tuple_Disabled()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class C
            {
                [|{|CS8059:(int, string)|} a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class C
            {
                {|CS8059:(int, string)|} a;

                public override bool Equals(object obj)
                {
                    var c = obj as C;
                    return !ReferenceEquals(c, null) &&
                           a.Equals(c.a);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task Tuples_Equals()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class C
            {
                [|{|CS8059:(int, string)|} a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class C
            {
                {|CS8059:(int, string)|} a;

                public override bool Equals(object obj)
                {
                    var c = obj as C;
                    return !ReferenceEquals(c, null) &&
                           a.Equals(c.a);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TupleWithNames_Equals()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class C
            {
                [|{|CS8059:(int x, string y)|} a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class C
            {
                {|CS8059:(int x, string y)|} a;

                public override bool Equals(object obj)
                {
                    var c = obj as C;
                    return !ReferenceEquals(c, null) &&
                           a.Equals(c.a);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task Tuple_HashCode()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                [|{|CS8059:(int, string)|} i;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                {|CS8059:(int, string)|} i;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           i.Equals(program.i);
                }

                public override int GetHashCode()
                {
                    return 165851236 + i.GetHashCode();
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TupleWithNames_HashCode()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                [|{|CS8059:(int x, string y)|} i;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                {|CS8059:(int x, string y)|} i;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           i.Equals(program.i);
                }

                public override int GetHashCode()
                {
                    return 165851236 + i.GetHashCode();
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task StructWithoutGetHashCodeOverride_ShouldCallGetHashCodeDirectly()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Foo
            {
                [|Bar bar;|]
            }

            struct Bar
            {
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Foo
            {
                Bar bar;

                public override bool Equals(object obj)
                {
                    var foo = obj as Foo;
                    return !ReferenceEquals(foo, null) &&
                           EqualityComparer<Bar>.Default.Equals(bar, foo.bar);
                }

                public override int GetHashCode()
                {
                    return 999205674 + bar.GetHashCode();
                }
            }

            struct Bar
            {
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task StructWithGetHashCodeOverride_ShouldCallGetHashCodeDirectly()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Foo
            {
                [|Bar bar;|]
            }

            struct Bar
            {
                public override int GetHashCode() => 0;
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Foo
            {
                Bar bar;

                public override bool Equals(object obj)
                {
                    var foo = obj as Foo;
                    return !ReferenceEquals(foo, null) &&
                           EqualityComparer<Bar>.Default.Equals(bar, foo.bar);
                }

                public override int GetHashCode()
                {
                    return 999205674 + bar.GetHashCode();
                }
            }

            struct Bar
            {
                public override int GetHashCode() => 0;
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task NullableStructWithoutGetHashCodeOverride_ShouldCallGetHashCodeDirectly()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Foo
            {
                [|Bar? bar;|]
            }

            struct Bar
            {
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Foo
            {
                Bar? bar;

                public override bool Equals(object obj)
                {
                    var foo = obj as Foo;
                    return !ReferenceEquals(foo, null) &&
                           EqualityComparer<Bar?>.Default.Equals(bar, foo.bar);
                }

                public override int GetHashCode()
                {
                    return 999205674 + bar.GetHashCode();
                }
            }

            struct Bar
            {
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task StructTypeParameter_ShouldCallGetHashCodeDirectly()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Foo<TBar> where TBar : struct
            {
                [|TBar bar;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Foo<TBar> where TBar : struct
            {
                TBar bar;

                public override bool Equals(object obj)
                {
                    var foo = obj as Foo<TBar>;
                    return !ReferenceEquals(foo, null) &&
                           EqualityComparer<TBar>.Default.Equals(bar, foo.bar);
                }

                public override int GetHashCode()
                {
                    return 999205674 + bar.GetHashCode();
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task NullableStructTypeParameter_ShouldCallGetHashCodeDirectly()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Foo<TBar> where TBar : struct
            {
                [|TBar? bar;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Foo<TBar> where TBar : struct
            {
                TBar? bar;

                public override bool Equals(object obj)
                {
                    var foo = obj as Foo<TBar>;
                    return !ReferenceEquals(foo, null) &&
                           EqualityComparer<TBar?>.Default.Equals(bar, foo.bar);
                }

                public override int GetHashCode()
                {
                    return 999205674 + bar.GetHashCode();
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task Enum_ShouldCallGetHashCodeDirectly()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Foo
            {
                [|Bar bar;|]
            }

            enum Bar
            {
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Foo
            {
                Bar bar;

                public override bool Equals(object obj)
                {
                    var foo = obj as Foo;
                    return !ReferenceEquals(foo, null) &&
                           bar == foo.bar;
                }

                public override int GetHashCode()
                {
                    return 999205674 + bar.GetHashCode();
                }
            }

            enum Bar
            {
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task PrimitiveValueType_ShouldCallGetHashCodeDirectly()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Foo
            {
                [|ulong bar;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Foo
            {
                ulong bar;

                public override bool Equals(object obj)
                {
                    var foo = obj as Foo;
                    return !ReferenceEquals(foo, null) &&
                           bar == foo.bar;
                }

                public override int GetHashCode()
                {
                    return 999205674 + bar.GetHashCode();
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestWithDialog1()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                int a;
                string b;
                [||]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                int a;
                string b;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           a == program.a &&
                           b == program.b;
                }
            }
            """,
            MemberNames = ["a", "b"],
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestWithDialog2()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                int a;
                string b;
                bool c;
                [||]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                int a;
                string b;
                bool c;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           c == program.c &&
                           b == program.b;
                }
            }
            """,
            MemberNames = ["c", "b"],
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestWithDialog3()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                int a;
                string b;
                bool c;
                [||]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                int a;
                string b;
                bool c;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null);
                }
            }
            """,
            MemberNames = [],
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17643")]
    public Task TestWithDialogNoBackingField()
        => new TestWithDialog
        {
            TestCode = """
            class Program
            {
                public int F { get; set; }
                [||]
            }
            """,
            FixedCode = """
            class Program
            {
                public int F { get; set; }

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           F == program.F;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25690")]
    public Task TestWithDialogNoIndexer()
        => new TestWithDialog
        {
            TestCode = """
            class Program
            {
                public int P => 0;
                public int this[int index] => 0;
                [||]
            }
            """,
            FixedCode = """
            class Program
            {
                public int P => 0;
                public int this[int index] => 0;

                public override bool Equals(object obj)
                {
                    return obj is Program program &&
                           P == program.P;
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25707")]
    public Task TestWithDialogNoSetterOnlyProperty()
        => new TestWithDialog
        {
            TestCode = """
            class Program
            {
                public int P => 0;
                public int S { set { } }
                [||]
            }
            """,
            FixedCode = """
            class Program
            {
                public int P => 0;
                public int S { set { } }

                public override bool Equals(object obj)
                {
                    return obj is Program program &&
                           P == program.P;
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41958")]
    public Task TestWithDialogInheritedMembers()
        => new TestWithDialog
        {
            TestCode = """
            class Base
            {
                public int C { get; set; }
            }

            class Middle : Base
            {
                public int B { get; set; }
            }

            class Derived : Middle
            {
                public int A { get; set; }
                [||]
            }
            """,
            FixedCode = """
            class Base
            {
                public int C { get; set; }
            }

            class Middle : Base
            {
                public int B { get; set; }
            }

            class Derived : Middle
            {
                public int A { get; set; }

                public override bool Equals(object obj)
                {
                    return obj is Derived derived &&
                           C == derived.C &&
                           B == derived.B &&
                           A == derived.A;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestGenerateOperators1()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                public string s;
                [||]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                public string s;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           s == program.s;
                }

                public static bool operator ==(Program left, Program right)
                {
                    return EqualityComparer<Program>.Default.Equals(left, right);
                }

                public static bool operator !=(Program left, Program right)
                {
                    return !(left == right);
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId),
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestGenerateOperators2()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                public string s;
                [||]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                public string s;

                public override bool Equals(object obj)
                {
                    Program program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           s == program.s;
                }

                public static bool operator ==(Program left, Program right) => EqualityComparer<Program>.Default.Equals(left, right);
                public static bool operator !=(Program left, Program right) => !(left == right);
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId),
            LanguageVersion = LanguageVersion.CSharp6,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
            },
        }.RunAsync();

    [Fact]
    public Task TestGenerateOperators3()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                public string s;
                [||]

                public static bool operator {|CS0216:==|}(Program left, Program right) => true;
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                public string s;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           s == program.s;
                }

                public static bool operator {|CS0216:==|}(Program left, Program right) => true;
            }
            """,
            MemberNames = default,
            OptionsCallback = options => Assert.Null(options.FirstOrDefault(i => i.Id == GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId)),
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestGenerateOperators4()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            struct Program
            {
                public string s;
                [||]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            struct Program
            {
                public string s;

                public override bool Equals(object obj)
                {
                    if (!(obj is Program))
                    {
                        return false;
                    }

                    var program = (Program)obj;
                    return s == program.s;
                }

                public static bool operator ==(Program left, Program right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(Program left, Program right)
                {
                    return !(left == right);
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId),
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestGenerateLiftedOperators()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Collections.Generic;

            class Foo
            {
                [|public bool? BooleanValue { get; }
                public decimal? DecimalValue { get; }
                public Bar? EnumValue { get; }
                public DateTime? DateTimeValue { get; }|]
            }

            enum Bar
            {
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;

            class Foo
            {
                public bool? BooleanValue { get; }
                public decimal? DecimalValue { get; }
                public Bar? EnumValue { get; }
                public DateTime? DateTimeValue { get; }

                public override bool Equals(object obj)
                {
                    var foo = obj as Foo;
                    return !ReferenceEquals(foo, null) &&
                           BooleanValue == foo.BooleanValue &&
                           DecimalValue == foo.DecimalValue &&
                           EnumValue == foo.EnumValue &&
                           DateTimeValue == foo.DateTimeValue;
                }
            }

            enum Bar
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task LiftedOperatorIsNotUsedWhenDirectOperatorWouldNotBeUsed()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Collections.Generic;

            class Foo
            {
                [|public Bar Value { get; }
                public Bar? NullableValue { get; }|]
            }

            struct Bar : IEquatable<Bar>
            {
                private readonly int value;

                public override bool Equals(object obj) => false;

                public bool Equals(Bar other) => value == other.value;

                public override int GetHashCode() => -1584136870 + value.GetHashCode();

                public static bool operator ==(Bar left, Bar right) => left.Equals(right);

                public static bool operator !=(Bar left, Bar right) => !(left == right);
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;

            class Foo
            {
                public Bar Value { get; }
                public Bar? NullableValue { get; }

                public override bool Equals(object obj)
                {
                    var foo = obj as Foo;
                    return !ReferenceEquals(foo, null) &&
                           Value.Equals(foo.Value) &&
                           EqualityComparer<Bar?>.Default.Equals(NullableValue, foo.NullableValue);
                }
            }

            struct Bar : IEquatable<Bar>
            {
                private readonly int value;

                public override bool Equals(object obj) => false;

                public bool Equals(Bar other) => value == other.value;

                public override int GetHashCode() => -1584136870 + value.GetHashCode();

                public static bool operator ==(Bar left, Bar right) => left.Equals(right);

                public static bool operator !=(Bar left, Bar right) => !(left == right);
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestImplementIEquatableOnStruct()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            struct Program
            {
                public string s;
                [||]
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;

            struct Program : IEquatable<Program>
            {
                public string s;

                public override bool Equals(object obj)
                {
                    return obj is Program && Equals((Program)obj);
                }

                public bool Equals(Program other)
                {
                    return s == other.s;
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25708")]
    public Task TestOverrideEqualsOnRefStructReturnsFalse()
        => new TestWithDialog
        {
            TestCode = """
            ref struct Program
            {
                public string s;
                [||]
            }
            """,
            FixedCode = """
            ref struct Program
            {
                public string s;

                public override bool Equals(object obj)
                {
                    return false;
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25708")]
    public Task TestImplementIEquatableOnRefStructSkipsIEquatable()
        => new TestWithDialog
        {
            TestCode = """
            ref struct Program
            {
                public string s;
                [||]
            }
            """,
            FixedCode = """
            ref struct Program
            {
                public string s;

                public override bool Equals(object obj)
                {
                    return false;
                }
            }
            """,
            MemberNames = default,
            // We are forcefully enabling the ImplementIEquatable option, as that is our way
            // to test that the option does nothing. The VS mode will ensure if the option
            // is not available it will not be shown.
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
        }.RunAsync();

    [Fact]
    public Task TestImplementIEquatableOnStructInNullableContextWithUnannotatedMetadata()
        => new TestWithDialog
        {
            TestCode = """
            #nullable enable

            struct Foo
            {
                public int Bar { get; }
                [||]
            }
            """,
            FixedCode = """
            #nullable enable

            using System;

            struct Foo : IEquatable<Foo>
            {
                public int Bar { get; }

                public override bool Equals(object? obj)
                {
                    return obj is Foo foo && Equals(foo);
                }

                public bool Equals(Foo other)
                {
                    return Bar == other.Bar;
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();

    [Fact]
    public Task TestImplementIEquatableOnStructInNullableContextWithAnnotatedMetadata()
        => new TestWithDialog
        {
            TestCode = """
            #nullable enable

            using System;
            using System.Diagnostics.CodeAnalysis;

            struct Foo
            {
                public bool Bar { get; }
                [||]
            }
            """,
            FixedCode = """
            #nullable enable

            using System;
            using System.Diagnostics.CodeAnalysis;

            struct Foo : IEquatable<Foo>
            {
                public bool Bar { get; }

                public override bool Equals(object? obj)
                {
                    return obj is Foo foo && Equals(foo);
                }

                public bool Equals(Foo other)
                {
                    return Bar == other.Bar;
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();

    [Fact]
    public Task TestImplementIEquatableOnClass_CSharp6()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                public string s;
                [||]
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;

            class Program : IEquatable<Program>
            {
                public string s;

                public override bool Equals(object obj)
                {
                    return Equals(obj as Program);
                }

                public bool Equals(Program other)
                {
                    return !ReferenceEquals(other, null) &&
                           s == other.s;
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestImplementIEquatableOnClass_CSharp7()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                public string s;
                [||]
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;

            class Program : IEquatable<Program>
            {
                public string s;

                public override bool Equals(object obj)
                {
                    return Equals(obj as Program);
                }

                public bool Equals(Program other)
                {
                    return !(other is null) &&
                           s == other.s;
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
            LanguageVersion = LanguageVersion.CSharp7,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestImplementIEquatableOnClass_CSharp8()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                public string s;
                [||]
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;

            class Program : IEquatable<Program>
            {
                public string s;

                public override bool Equals(object obj)
                {
                    return Equals(obj as Program);
                }

                public bool Equals(Program other)
                {
                    return !(other is null) &&
                           s == other.s;
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
            LanguageVersion = LanguageVersion.CSharp8,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestImplementIEquatableOnClass_CSharp9()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                public string s;
                [||]
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;

            class Program : IEquatable<Program>
            {
                public string s;

                public override bool Equals(object obj)
                {
                    return Equals(obj as Program);
                }

                public bool Equals(Program other)
                {
                    return other is not null &&
                           s == other.s;
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
            LanguageVersion = LanguageVersion.CSharp9,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestImplementIEquatableOnClassInNullableContextWithUnannotatedMetadata()
        => new TestWithDialog
        {
            TestCode = """
            #nullable enable

            class Foo
            {
                public int Bar { get; }
                [||]
            }
            """,
            FixedCode = """
            #nullable enable

            using System;

            class Foo : IEquatable<Foo?>
            {
                public int Bar { get; }

                public override bool Equals(object? obj)
                {
                    return Equals(obj as Foo);
                }

                public bool Equals(Foo? other)
                {
                    return !(other is null) &&
                           Bar == other.Bar;
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();

    [Fact]
    public Task TestImplementIEquatableOnClassInNullableContextWithAnnotatedMetadata()
        => new TestWithDialog
        {
            TestCode = """
            #nullable enable

            using System;
            using System.Diagnostics.CodeAnalysis;

            class Foo
            {
                public bool Bar { get; }
                [||]
            }
            """,
            FixedCode = """
            #nullable enable

            using System;
            using System.Diagnostics.CodeAnalysis;

            class Foo : IEquatable<Foo?>
            {
                public bool Bar { get; }

                public override bool Equals(object? obj)
                {
                    return Equals(obj as Foo);
                }

                public bool Equals(Foo? other)
                {
                    return !(other is null) &&
                           Bar == other.Bar;
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();

    [Fact]
    public Task TestDoNotOfferIEquatableIfTypeAlreadyImplementsIt()
        => new TestWithDialog
        {
            TestCode = """
            using System.Collections.Generic;

            class Program : {|CS0535:System.IEquatable<Program>|}
            {
                public string s;
                [||]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program : {|CS0535:System.IEquatable<Program>|}
            {
                public string s;

                public override bool Equals(object obj)
                {
                    var program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           s == program.s;
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => Assert.Null(options.FirstOrDefault(i => i.Id == GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId)),
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestMissingReferences1()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp6,
            CodeActionIndex = 1,
            TestState =
            {
                Sources =
                {
                    """
                    public class Class1
                    {
                        [|int i;|]

                        public void F()
                        {
                        }
                    }
                    """,
                },
                ExpectedDiagnostics =
                {
// /0/Test0.cs(1,14): error CS0518: Predefined type 'System.Object' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(1, 14, 1, 20).WithArguments("System.Object"),
// /0/Test0.cs(1,14): error CS1729: 'object' does not contain a constructor that takes 0 arguments
DiagnosticResult.CompilerError("CS1729").WithSpan(1, 14, 1, 20).WithArguments("object", "0"),
// /0/Test0.cs(3,5): error CS0518: Predefined type 'System.Int32' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(3, 5, 3, 8).WithArguments("System.Int32"),
// /0/Test0.cs(5,12): error CS0518: Predefined type 'System.Void' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(5, 12, 5, 16).WithArguments("System.Void"),
                },
            },
            FixedState =
            {
                Sources = {
                    """
                    public class Class1
                    {
                        int i;

                        public override System.Boolean Equals(System.Object obj)
                        {
                            Class1 @class = obj as Class1;
                            return !ReferenceEquals(@class, null) &&
                                   i == @class.i;
                        }

                        public void F()
                        {
                        }

                        public override System.Int32 GetHashCode()
                        {
                            return 165851236 + EqualityComparer<System.Int32>.Default.GetHashCode(i);
                        }
                    }
                    """,
                },
                ExpectedDiagnostics =
                {
// /0/Test0.cs(1,14): error CS0518: Predefined type 'System.Object' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(1, 14, 1, 20).WithArguments("System.Object"),
// /0/Test0.cs(1,14): error CS1729: 'object' does not contain a constructor that takes 0 arguments
DiagnosticResult.CompilerError("CS1729").WithSpan(1, 14, 1, 20).WithArguments("object", "0"),
// /0/Test0.cs(3,5): error CS0518: Predefined type 'System.Int32' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(3, 5, 3, 8).WithArguments("System.Int32"),
// /0/Test0.cs(5,21): error CS0518: Predefined type 'System.Object' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(5, 21, 5, 27).WithArguments("System.Object"),
// /0/Test0.cs(5,28): error CS1069: The type name 'Boolean' could not be found in the namespace 'System'. This type has been forwarded to assembly 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' Consider adding a reference to that assembly.
DiagnosticResult.CompilerError("CS1069").WithSpan(5, 28, 5, 35).WithArguments("Boolean", "System", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
// /0/Test0.cs(5,43): error CS0518: Predefined type 'System.Object' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(5, 43, 5, 49).WithArguments("System.Object"),
// /0/Test0.cs(5,50): error CS1069: The type name 'Object' could not be found in the namespace 'System'. This type has been forwarded to assembly 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' Consider adding a reference to that assembly.
DiagnosticResult.CompilerError("CS1069").WithSpan(5, 50, 5, 56).WithArguments("Object", "System", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
// /0/Test0.cs(7,9): error CS0518: Predefined type 'System.Object' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(7, 9, 7, 15).WithArguments("System.Object"),
// /0/Test0.cs(7,32): error CS0518: Predefined type 'System.Object' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(7, 32, 7, 38).WithArguments("System.Object"),
// /0/Test0.cs(8,17): error CS0103: The name 'ReferenceEquals' does not exist in the current context
DiagnosticResult.CompilerError("CS0103").WithSpan(8, 17, 8, 32).WithArguments("ReferenceEquals"),
// /0/Test0.cs(8,17): error CS0518: Predefined type 'System.Object' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(8, 17, 8, 32).WithArguments("System.Object"),
// /0/Test0.cs(9,16): error CS0518: Predefined type 'System.Boolean' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(9, 16, 9, 29).WithArguments("System.Boolean"),
// /0/Test0.cs(12,12): error CS0518: Predefined type 'System.Void' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(12, 12, 12, 16).WithArguments("System.Void"),
// /0/Test0.cs(16,21): error CS0518: Predefined type 'System.Object' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(16, 21, 16, 27).WithArguments("System.Object"),
// /0/Test0.cs(16,28): error CS1069: The type name 'Int32' could not be found in the namespace 'System'. This type has been forwarded to assembly 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' Consider adding a reference to that assembly.
DiagnosticResult.CompilerError("CS1069").WithSpan(16, 28, 16, 33).WithArguments("Int32", "System", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
// /0/Test0.cs(18,16): error CS0518: Predefined type 'System.Int32' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(18, 16, 18, 25).WithArguments("System.Int32"),
// /0/Test0.cs(18,28): error CS0103: The name 'EqualityComparer' does not exist in the current context
DiagnosticResult.CompilerError("CS0103").WithSpan(18, 28, 18, 58).WithArguments("EqualityComparer"),
// /0/Test0.cs(18,28): error CS0518: Predefined type 'System.Object' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(18, 28, 18, 58).WithArguments("System.Object"),
// /0/Test0.cs(18,45): error CS0518: Predefined type 'System.Object' is not defined or imported
DiagnosticResult.CompilerError("CS0518").WithSpan(18, 45, 18, 51).WithArguments("System.Object"),
// /0/Test0.cs(18,52): error CS1069: The type name 'Int32' could not be found in the namespace 'System'. This type has been forwarded to assembly 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' Consider adding a reference to that assembly.
DiagnosticResult.CompilerError("CS1069").WithSpan(18, 52, 18, 57).WithArguments("Int32", "System", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                },
            },
            ReferenceAssemblies = ReferenceAssemblies.Default.WithAssemblies([]),
        }.RunAsync();

    [Fact]
    public Task TestGetHashCodeInCheckedContext()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                [|int i;

                string S { get; }|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Program
            {
                int i;

                string S { get; }

                public override bool Equals(object obj)
                {
                    Program program = obj as Program;
                    return !ReferenceEquals(program, null) &&
                           i == program.i &&
                           S == program.S;
                }

                public override int GetHashCode()
                {
                    unchecked
                    {
                        int hashCode = -538000506;
                        hashCode = hashCode * -1521134295 + i.GetHashCode();
                        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                        return hashCode;
                    }
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            SolutionTransforms =
            {
                (solution, projectId) =>
                {
                    var compilationOptions = solution.GetRequiredProject(projectId).CompilationOptions;
                    return solution.WithProjectCompilationOptions(projectId, compilationOptions.WithOverflowChecks(true));
                },
            },
        }.RunAsync();

    [Fact]
    public Task TestGetHashCodeStruct()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            struct S
            {
                [|int j;|]
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;

            struct S : IEquatable<S>
            {
                int j;

                public override bool Equals(object obj)
                {
                    return obj is S && Equals((S)obj);
                }

                public bool Equals(S other)
                {
                    return j == other.j;
                }

                public override int GetHashCode()
                {
                    return 1424088837 + j.GetHashCode();
                }

                public static bool operator ==(S left, S right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(S left, S right)
                {
                    return !(left == right);
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public async Task TestGetHashCodeSystemHashCodeOneMember()
    {
        var fixedCode =
            """
            using System;
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            struct S : IEquatable<S>
            {
                int j;

                public override bool Equals(object obj)
                {
                    return obj is S && Equals((S)obj);
                }

                public bool Equals(S other)
                {
                    return j == other.j;
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(j);
                }

                public static bool operator ==(S left, S right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(S left, S right)
                {
                    return !(left == right);
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            struct S
            {
                [|int j;|]
            }
            """,
            FixedState =
            {
                Sources = { fixedCode },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(21,25): error CS0117: 'HashCode' does not contain a definition for 'Combine'
                    DiagnosticResult.CompilerError("CS0117").WithSpan(21, 25, 21, 32).WithArguments("System.HashCode", "Combine"),
                },
            },
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37297")]
    public async Task TestPublicSystemHashCodeOtherProject()
    {
        var code =
            """
            struct S
            {
                [|int j;|]
            }
            """;
        var fixedCode =
            """
            using System;

            struct S : IEquatable<S>
            {
                int j;

                public override bool Equals(object obj)
                {
                    return obj is S && Equals((S)obj);
                }

                public bool Equals(S other)
                {
                    return j == other.j;
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(j);
                }

                public static bool operator ==(S left, S right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(S left, S right)
                {
                    return !(left == right);
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                AdditionalProjects =
                {
                    ["P1"] =
                    {
                        Sources = { ("HashCode.cs", """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }
            """) },
                    },
                },
                Sources = { code },
                AdditionalProjectReferences = { "P1" },
            },
            FixedState =
            {
                Sources = { fixedCode },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(19,25): error CS0117: 'HashCode' does not contain a definition for 'Combine'
                    DiagnosticResult.CompilerError("CS0117").WithSpan(19, 25, 19, 32).WithArguments("System.HashCode", "Combine"),
                },
            },
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37297")]
    public async Task TestInternalSystemHashCode()
    {
        var code =
            """
            struct S
            {
                [|int j;|]
            }
            """;
        await new VerifyCS.Test
        {
            TestState =
            {
                AdditionalProjects =
                {
                    ["P1"] =
                    {
                        Sources = { ("HashCode.cs", """
            using System.Collections.Generic;
            namespace System { internal struct HashCode { } }
            """) },
                    },
                },
                Sources = { code },
                AdditionalProjectReferences = { "P1" },
            },
            FixedCode = """
            using System;

            struct S : IEquatable<S>
            {
                int j;

                public override bool Equals(object obj)
                {
                    return obj is S && Equals((S)obj);
                }

                public bool Equals(S other)
                {
                    return j == other.j;
                }

                public override int GetHashCode()
                {
                    return 1424088837 + j.GetHashCode();
                }

                public static bool operator ==(S left, S right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(S left, S right)
                {
                    return !(left == right);
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();
    }

    [Fact]
    public async Task TestGetHashCodeSystemHashCodeEightMembers()
    {
        var fixedCode =
            """
            using System;
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            struct S : IEquatable<S>
            {
                int j, k, l, m, n, o, p, q;

                public override bool Equals(object obj)
                {
                    return obj is S && Equals((S)obj);
                }

                public bool Equals(S other)
                {
                    return j == other.j &&
                           k == other.k &&
                           l == other.l &&
                           m == other.m &&
                           n == other.n &&
                           o == other.o &&
                           p == other.p &&
                           q == other.q;
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(j, k, l, m, n, o, p, q);
                }

                public static bool operator ==(S left, S right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(S left, S right)
                {
                    return !(left == right);
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            struct S
            {
                [|int j, k, l, m, n, o, p, q;|]
            }
            """,
            FixedState =
            {
                Sources = { fixedCode },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(28,25): error CS0117: 'HashCode' does not contain a definition for 'Combine'
                    DiagnosticResult.CompilerError("CS0117").WithSpan(28, 25, 28, 32).WithArguments("System.HashCode", "Combine"),
                },
            },
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();
    }

    [Fact]
    public Task TestGetHashCodeSystemHashCodeNineMembers()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;
            namespace System { public struct HashCode { public void Add<T>(T value) { } public int ToHashCode() => 0; } }

            struct S
            {
                [|int j, k, l, m, n, o, p, q, r;|]
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;
            namespace System { public struct HashCode { public void Add<T>(T value) { } public int ToHashCode() => 0; } }

            struct S : IEquatable<S>
            {
                int j, k, l, m, n, o, p, q, r;

                public override bool Equals(object obj)
                {
                    return obj is S && Equals((S)obj);
                }

                public bool Equals(S other)
                {
                    return j == other.j &&
                           k == other.k &&
                           l == other.l &&
                           m == other.m &&
                           n == other.n &&
                           o == other.o &&
                           p == other.p &&
                           q == other.q &&
                           r == other.r;
                }

                public override int GetHashCode()
                {
                    var hash = new HashCode();
                    hash.Add(j);
                    hash.Add(k);
                    hash.Add(l);
                    hash.Add(m);
                    hash.Add(n);
                    hash.Add(o);
                    hash.Add(p);
                    hash.Add(q);
                    hash.Add(r);
                    return hash.ToHashCode();
                }

                public static bool operator ==(S left, S right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(S left, S right)
                {
                    return !(left == right);
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39916")]
    public Task TestGetHashCodeSystemHashCodeNineMembers_Explicit()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;
            namespace System { public struct HashCode { public void Add<T>(T value) { } public int ToHashCode() => 0; } }

            struct S
            {
                [|int j, k, l, m, n, o, p, q, r;|]
            }
            """,
            FixedCode = """
            using System;
            using System.Collections.Generic;
            namespace System { public struct HashCode { public void Add<T>(T value) { } public int ToHashCode() => 0; } }

            struct S : IEquatable<S>
            {
                int j, k, l, m, n, o, p, q, r;

                public override bool Equals(object obj)
                {
                    return obj is S && Equals((S)obj);
                }

                public bool Equals(S other)
                {
                    return j == other.j &&
                           k == other.k &&
                           l == other.l &&
                           m == other.m &&
                           n == other.n &&
                           o == other.o &&
                           p == other.p &&
                           q == other.q &&
                           r == other.r;
                }

                public override int GetHashCode()
                {
                    HashCode hash = new HashCode();
                    hash.Add(j);
                    hash.Add(k);
                    hash.Add(l);
                    hash.Add(m);
                    hash.Add(n);
                    hash.Add(o);
                    hash.Add(p);
                    hash.Add(q);
                    hash.Add(r);
                    return hash.ToHashCode();
                }

                public static bool operator ==(S left, S right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(S left, S right)
                {
                    return !(left == right);
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp6,
            Options = { PreferExplicitTypeWithInfo() },
        }.RunAsync();

    [Fact]
    public Task TestEqualsSingleField_Patterns()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                [|int a;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                int a;

                public override bool Equals(object obj)
                {
                    return obj is Program program &&
                           a == program.a;
                }
            }
            """);

    [Fact]
    public Task TestEqualsSingleFieldInStruct_Patterns()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System.Collections.Generic;

            struct Program
            {
                [|int a;|]
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            struct Program : IEquatable<Program>
            {
                int a;

                public override bool Equals(object obj)
                {
                    return obj is Program program && Equals(program);
                }

                public bool Equals(Program other)
                {
                    return a == other.a;
                }

                public static bool operator ==(Program left, Program right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(Program left, Program right)
                {
                    return !(left == right);
                }
            }
            """);

    [Fact]
    public Task TestEqualsBaseWithOverriddenEquals_Patterns()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Base
            {
                public override bool Equals(object o)
                {
                    return false;
                }
            }

            class Program : Base
            {
                [|int i;

                string S { get; }|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            class Base
            {
                public override bool Equals(object o)
                {
                    return false;
                }
            }

            class Program : Base
            {
                int i;

                string S { get; }

                public override bool Equals(object obj)
                {
                    return obj is Program program &&
                           base.Equals(obj) &&
                           i == program.i &&
                           S == program.S;
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestPartialSelection()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            class Program
            {
                int [|a|];
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40053")]
    public async Task TestEqualityOperatorsNullableAnnotationWithReferenceType()
    {
        var fixedCode =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;

            namespace N
            {
                public class C
                {
                    public int X;

                    public override bool Equals(object? obj)
                    {
                        return obj is C c &&
                               X == c.X;
                    }

                    public static bool operator ==(C? left, C? right)
                    {
                        return EqualityComparer<C>.Default.Equals(left, right);
                    }

                    public static bool operator !=(C? left, C? right)
                    {
                        return !(left == right);
                    }
                }
            }
            """;

        await new TestWithDialog
        {
            TestCode = """
            #nullable enable
            using System;

            namespace N
            {
                public class C[||]
                {
                    public int X;
                }
            }
            """,
            FixedState =
            {
                Sources = { fixedCode },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(20,55): error CS8604: Possible null reference argument for parameter 'x' in 'bool EqualityComparer<C>.Equals(C x, C y)'.
                    DiagnosticResult.CompilerError("CS8604").WithSpan(19, 55, 19, 59).WithArguments("x", "bool EqualityComparer<C>.Equals(C x, C y)"),
                    // /0/Test0.cs(20,61): error CS8604: Possible null reference argument for parameter 'y' in 'bool EqualityComparer<C>.Equals(C x, C y)'.
                    DiagnosticResult.CompilerError("CS8604").WithSpan(19, 61, 19, 66).WithArguments("y", "bool EqualityComparer<C>.Equals(C x, C y)"),
                },
            },
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId),
            LanguageVersion = LanguageVersion.Default,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40053")]
    public Task TestEqualityOperatorsNullableAnnotationWithValueType()
        => new TestWithDialog
        {
            TestCode = """
            #nullable enable
            using System;

            namespace N
            {
                public struct C[||]
                {
                    public int X;
                }
            }
            """,
            FixedCode = """
            #nullable enable
            using System;

            namespace N
            {
                public struct C
                {
                    public int X;

                    public override bool Equals(object? obj)
                    {
                        return obj is C c &&
                               X == c.X;
                    }

                    public static bool operator ==(C left, C right)
                    {
                        return left.Equals(right);
                    }

                    public static bool operator !=(C left, C right)
                    {
                        return !(left == right);
                    }
                }
            }
            """,
            MemberNames = default,
            OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId),
            LanguageVersion = LanguageVersion.Default,
            Options = { PreferImplicitTypeWithInfo() },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42574")]
    public Task TestPartialTypes1()
        => new TestWithDialog
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class Goo
                    {
                        int bar;
                        [||]
                    }
                    """,
                    """
                    partial class Goo
                    {


                    }
                    """,
                },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class Goo
                    {
                        int bar;

                        public override bool Equals(object obj)
                        {
                            return obj is Goo goo &&
                                   bar == goo.bar;
                        }

                        public override int GetHashCode()
                        {
                            return 999205674 + bar.GetHashCode();
                        }
                    }
                    """,
                    """
                    partial class Goo
                    {


                    }
                    """,
                },
            },
            MemberNames = ["bar"],
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42574")]
    public Task TestPartialTypes2()
        => new TestWithDialog
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class Goo
                    {
                        int bar;

                    }
                    """,
                    """
                    partial class Goo
                    {

                    [||]
                    }
                    """,
                },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class Goo
                    {
                        int bar;

                    }
                    """,
                    """
                    partial class Goo
                    {
                        public override bool Equals(object obj)
                        {
                            return obj is Goo goo &&
                                   bar == goo.bar;
                        }

                        public override int GetHashCode()
                        {
                            return 999205674 + bar.GetHashCode();
                        }
                    }
                    """,
                },
            },
            MemberNames = ["bar"],
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42574")]
    public Task TestPartialTypes3()
        => new TestWithDialog
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class Goo
                    {

                    [||]
                    }
                    """,
                    """
                    partial class Goo
                    {
                        int bar;

                    }
                    """,
                },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class Goo
                    {
                        public override bool Equals(object obj)
                        {
                            return obj is Goo goo &&
                                   bar == goo.bar;
                        }

                        public override int GetHashCode()
                        {
                            return 999205674 + bar.GetHashCode();
                        }
                    }
                    """,
                    """
                    partial class Goo
                    {
                        int bar;

                    }
                    """,
                },
            },
            MemberNames = ["bar"],
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42574")]
    public Task TestPartialTypes4()
        => new TestWithDialog
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class Goo
                    {


                    }
                    """,
                    """
                    partial class Goo
                    {
                        int bar;
                    [||]
                    }
                    """,
                },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class Goo
                    {


                    }
                    """,
                    """
                    partial class Goo
                    {
                        int bar;

                        public override bool Equals(object obj)
                        {
                            return obj is Goo goo &&
                                   bar == goo.bar;
                        }

                        public override int GetHashCode()
                        {
                            return 999205674 + bar.GetHashCode();
                        }
                    }
                    """,
                },
            },
            MemberNames = ["bar"],
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43290")]
    public async Task TestAbstractBase()
    {
        var fixedCode =
            """
            #nullable enable

            using System;

            namespace System { public struct HashCode { } }

            abstract class Base
            {
                public abstract override bool Equals(object? obj);
                public abstract override int GetHashCode();
            }

            class Derived : Base
            {
                public int P { get; }

                public override bool Equals(object? obj)
                {
                    return obj is Derived derived &&
                           P == derived.P;
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(P);
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            #nullable enable

            namespace System { public struct HashCode { } }

            abstract class Base
            {
                public abstract override bool Equals(object? obj);
                public abstract override int GetHashCode();
            }

            class {|CS0534:{|CS0534:Derived|}|} : Base
            {
                [|public int P { get; }|]
            }
            """,
            FixedState =
            {
                Sources = { fixedCode },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(23,25): error CS0117: 'HashCode' does not contain a definition for 'Combine'
                    DiagnosticResult.CompilerError("CS0117").WithSpan(25, 25, 25, 32).WithArguments("System.HashCode", "Combine"),
                },
            },
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.Default,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76916")]
    public Task TestMissingWithPrimaryConstructorAndNoFields()
        => new VerifyCS.Test
        {
            TestCode = """
                class C(int a)
                {
                    [||]
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestRecord_Equals1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            record Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            record Program
            {
                int a;

                public virtual bool Equals(Program program)
                {
                    return program is not null &&
                           a == program.a;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestRecord_EqualsAndGetHashCode1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            record Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            record Program
            {
                int a;
            
                public virtual bool Equals(Program program)
                {
                    return program is not null &&
                           a == program.a;
                }

                public override int GetHashCode()
                {
                    return -1757793268 + a.GetHashCode();
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestSealedRecord_Equals1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            sealed record Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            sealed record Program
            {
                int a;

                public bool Equals(Program program)
                {
                    return program is not null &&
                           a == program.a;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestSealedRecord_EqualsAndGetHashCode1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            sealed record Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            sealed record Program
            {
                int a;
            
                public bool Equals(Program program)
                {
                    return program is not null &&
                           a == program.a;
                }
            
                public override int GetHashCode()
                {
                    return -1757793268 + a.GetHashCode();
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestRecordStruct_Equals1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            record struct Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            record struct Program
            {
                int a;

                public bool Equals(Program program)
                {
                    return a == program.a;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestRecordStruct_EqualsAndGetHashCode1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            record struct Program
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            record struct Program
            {
                int a;
            
                public bool Equals(Program program)
                {
                    return a == program.a;
                }
            
                public override int GetHashCode()
                {
                    return -1757793268 + a.GetHashCode();
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestDerived_Equals1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;

            record Base { }

            record Program : Base
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;

            record Base { }

            record Program : Base
            {
                int a;

                public virtual bool Equals(Program program)
                {
                    return program is not null &&
                           base.Equals(program) &&
                           a == program.a;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task TestDerivedRecord_EqualsAndGetHashCode1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;
            
            record Base { }

            record Program : Base
            {
                [|int a;|]
            }
            """,
            FixedCode = """
            using System.Collections.Generic;
            
            record Base { }

            record Program : Base
            {
                int a;
            
                public virtual bool Equals(Program program)
                {
                    return program is not null &&
                           base.Equals(program) &&
                           a == program.a;
                }

                public override int GetHashCode()
                {
                    int hashCode = 155057090;
                    hashCode = hashCode * -1521134295 + base.GetHashCode();
                    hashCode = hashCode * -1521134295 + a.GetHashCode();
                    return hashCode;
                }
            }
            """,
            LanguageVersion = LanguageVersion.Preview,
            CodeActionIndex = 1,
        }.RunAsync();
}
