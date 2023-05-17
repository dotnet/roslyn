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
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeRefactoringVerifier<
    Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers.GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateEqualsAndGetHashCodeFromMembers
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
    public class GenerateEqualsAndGetHashCodeFromMembersTests
    {
        private class TestWithDialog : VerifyCS.Test
        {
            private static readonly TestComposition s_composition =
                EditorTestCompositions.EditorFeatures.AddParts(typeof(TestPickMembersService));

            public ImmutableArray<string> MemberNames;
            public Action<ImmutableArray<PickMembersOption>> OptionsCallback;

            protected override Workspace CreateWorkspaceImpl()
            {
                // If we're a dialog test, then mixin our mock and initialize its values to the ones the test asked for.
                var workspace = new AdhocWorkspace(s_composition.GetHostServices());

                var service = (TestPickMembersService)workspace.Services.GetService<IPickMembersService>();
                service.MemberNames = MemberNames;
                service.OptionsCallback = OptionsCallback;

                return workspace;
            }
        }

        private static OptionsCollection PreferImplicitTypeWithInfo()
            => new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.VarElsewhere, true, NotificationOption2.Suggestion },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, true, NotificationOption2.Suggestion },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, true, NotificationOption2.Suggestion },
            };

        private static OptionsCollection PreferExplicitTypeWithInfo()
            => new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.VarElsewhere, false, NotificationOption2.Suggestion },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, false, NotificationOption2.Suggestion },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, false, NotificationOption2.Suggestion },
            };

        internal static void EnableOption(ImmutableArray<PickMembersOption> options, string id)
        {
            var option = options.FirstOrDefault(o => o.Id == id);
            if (option != null)
            {
                option.Value = true;
            }
        }

        [Fact]
        public async Task TestEqualsSingleField()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    [|int a;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestEqualsSingleField_CSharp7()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    [|int a;|]
                }
                """;
            var fixedCode =
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp7,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39916")]
        public async Task TestEqualsSingleField_PreferExplicitType()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    [|int a;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferExplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestReferenceIEquatable()
        {
            var code =
                """
                using System;
                using System.Collections.Generic;

                class S : {|CS0535:IEquatable<S>|} { }

                class Program
                {
                    [|S a;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestNullableReferenceIEquatable()
        {
            var code =
                """
                #nullable enable

                using System;
                using System.Collections.Generic;

                class S : {|CS0535:IEquatable<S>|} { }

                class Program
                {
                    [|S? a;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestValueIEquatable()
        {
            var code =
                """
                using System;
                using System.Collections.Generic;

                struct S : {|CS0535:IEquatable<S>|} { }

                class Program
                {
                    [|S a;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestEqualsLongName()
        {
            var code =
                """
                using System.Collections.Generic;

                class ReallyLongName
                {
                    [|int a;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestEqualsKeywordName()
        {
            var code =
                """
                using System.Collections.Generic;

                class ReallyLongLong
                {
                    [|long a;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestEqualsProperty()
        {
            var code =
                """
                using System.Collections.Generic;

                class ReallyLongName
                {
                    [|int a;

                    string B { get; }|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestEqualsBaseTypeWithNoEquals()
        {
            var code =
                """
                class Base
                {
                }

                class Program : Base
                {
                    [|int i;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestEqualsBaseWithOverriddenEquals()
        {
            var code =
                """
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
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 0,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestEqualsOverriddenDeepBase()
        {
            var code =
                """
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
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestEqualsStruct()
        {
            await VerifyCS.VerifyRefactoringAsync(
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
        }

        [Fact]
        public async Task TestEqualsStructCSharpLatest()
        {
            await VerifyCS.VerifyRefactoringAsync(
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
        }

        [Fact]
        public async Task TestEqualsStructAlreadyImplementsIEquatable()
        {
            await VerifyCS.VerifyRefactoringAsync(
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
        }

        [Fact]
        public async Task TestEqualsStructAlreadyHasOperators()
        {
            await VerifyCS.VerifyRefactoringAsync(
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
        }

        [Fact]
        public async Task TestEqualsStructAlreadyImplementsIEquatableAndHasOperators()
        {
            await VerifyCS.VerifyRefactoringAsync(
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
        }

        [Fact]
        public async Task TestEqualsGenericType()
        {
            var code = """
                using System.Collections.Generic;
                class Program<T>
                {
                    [|int i;|]
                }
                """;

            var expected = """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = expected,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestEqualsNullableContext()
        {
            await VerifyCS.VerifyRefactoringAsync(
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
        }

        [Fact]
        public async Task TestGetHashCodeSingleField1()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    [|int i;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGetHashCodeSingleField2()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    [|int j;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGetHashCodeWithBaseHashCode1()
        {
            var code =
                """
                using System.Collections.Generic;

                class Base {
                    public override int GetHashCode() => 0;
                }

                class Program : Base
                {
                    [|int j;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGetHashCodeWithBaseHashCode2()
        {
            var code =
                """
                using System.Collections.Generic;

                class Base {
                    public override int GetHashCode() => 0;
                }

                class Program : Base
                {
                    int j;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                MemberNames = ImmutableArray<string>.Empty,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGetHashCodeSingleField_CodeStyle1()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    [|int i;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options =
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
                },
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
            }.RunAsync();
        }

        [Fact]
        public async Task TestGetHashCodeTypeParameter()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program<T>
                {
                    [|T i;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGetHashCodeGenericType()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program<T>
                {
                    [|Program<T> i;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGetHashCodeMultipleMembers()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    [|int i;

                    string S { get; }|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestSmartTagText1()
        {
            var code =
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
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 0,
                CodeActionEquivalenceKey = FeaturesResources.Generate_Equals_object,
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Generate_Equals_object, codeAction.Title),
            }.RunAsync();
        }

        [Fact]
        public async Task TestSmartTagText2()
        {
            var code =
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
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = FeaturesResources.Generate_Equals_and_GetHashCode,
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Generate_Equals_and_GetHashCode, codeAction.Title),
            }.RunAsync();
        }

        [Fact]
        public async Task TestSmartTagText3()
        {
            var code =
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
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = FeaturesResources.Generate_Equals_and_GetHashCode,
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Generate_Equals_and_GetHashCode, codeAction.Title),
            }.RunAsync();
        }

        [Fact]
        public async Task Tuple_Disabled()
        {
            var code =
                """
                using System.Collections.Generic;

                class C
                {
                    [|{|CS8059:(int, string)|} a;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 0,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task Tuples_Equals()
        {
            var code =
                """
                using System.Collections.Generic;

                class C
                {
                    [|{|CS8059:(int, string)|} a;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TupleWithNames_Equals()
        {
            var code =
                """
                using System.Collections.Generic;

                class C
                {
                    [|{|CS8059:(int x, string y)|} a;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task Tuple_HashCode()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    [|{|CS8059:(int, string)|} i;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TupleWithNames_HashCode()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    [|{|CS8059:(int x, string y)|} i;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task StructWithoutGetHashCodeOverride_ShouldCallGetHashCodeDirectly()
        {
            var code =
                """
                using System.Collections.Generic;

                class Foo
                {
                    [|Bar bar;|]
                }

                struct Bar
                {
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task StructWithGetHashCodeOverride_ShouldCallGetHashCodeDirectly()
        {
            var code =
                """
                using System.Collections.Generic;

                class Foo
                {
                    [|Bar bar;|]
                }

                struct Bar
                {
                    public override int GetHashCode() => 0;
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task NullableStructWithoutGetHashCodeOverride_ShouldCallGetHashCodeDirectly()
        {
            var code =
                """
                using System.Collections.Generic;

                class Foo
                {
                    [|Bar? bar;|]
                }

                struct Bar
                {
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task StructTypeParameter_ShouldCallGetHashCodeDirectly()
        {
            var code =
                """
                using System.Collections.Generic;

                class Foo<TBar> where TBar : struct
                {
                    [|TBar bar;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task NullableStructTypeParameter_ShouldCallGetHashCodeDirectly()
        {
            var code =
                """
                using System.Collections.Generic;

                class Foo<TBar> where TBar : struct
                {
                    [|TBar? bar;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task Enum_ShouldCallGetHashCodeDirectly()
        {
            var code =
                """
                using System.Collections.Generic;

                class Foo
                {
                    [|Bar bar;|]
                }

                enum Bar
                {
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task PrimitiveValueType_ShouldCallGetHashCodeDirectly()
        {
            var code =
                """
                using System.Collections.Generic;

                class Foo
                {
                    [|ulong bar;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithDialog1()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    int a;
                    string b;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = ImmutableArray.Create("a", "b"),
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithDialog2()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    int a;
                    string b;
                    bool c;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = ImmutableArray.Create("c", "b"),
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithDialog3()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    int a;
                    string b;
                    bool c;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = ImmutableArray<string>.Empty,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17643")]
        public async Task TestWithDialogNoBackingField()
        {
            var code =
                """
                class Program
                {
                    public int F { get; set; }
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25690")]
        public async Task TestWithDialogNoIndexer()
        {
            var code =
                """
                class Program
                {
                    public int P => 0;
                    public int this[int index] => 0;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25707")]
        public async Task TestWithDialogNoSetterOnlyProperty()
        {
            var code =
                """
                class Program
                {
                    public int P => 0;
                    public int S { set { } }
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41958")]
        public async Task TestWithDialogInheritedMembers()
        {
            var code =
                """
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
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestGenerateOperators1()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    public string s;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId),
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGenerateOperators2()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    public string s;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId),
                LanguageVersion = LanguageVersion.CSharp6,
                Options =
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGenerateOperators3()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    public string s;
                    [||]

                    public static bool operator {|CS0216:==|}(Program left, Program right) => true;
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => Assert.Null(options.FirstOrDefault(i => i.Id == GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId)),
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGenerateOperators4()
        {
            var code =
                """
                using System.Collections.Generic;

                struct Program
                {
                    public string s;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId),
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGenerateLiftedOperators()
        {
            var code =
                """
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
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 0,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task LiftedOperatorIsNotUsedWhenDirectOperatorWouldNotBeUsed()
        {
            var code =
                """
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
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 0,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestImplementIEquatableOnStruct()
        {
            var code =
                """
                using System.Collections.Generic;

                struct Program
                {
                    public string s;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25708")]
        public async Task TestOverrideEqualsOnRefStructReturnsFalse()
        {
            var code =
                """
                ref struct Program
                {
                    public string s;
                    [||]
                }
                """;
            var fixedCode =
                """
                ref struct Program
                {
                    public string s;

                    public override bool Equals(object obj)
                    {
                        return false;
                    }
                }
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25708")]
        public async Task TestImplementIEquatableOnRefStructSkipsIEquatable()
        {
            var code =
                """
                ref struct Program
                {
                    public string s;
                    [||]
                }
                """;
            var fixedCode =
                """
                ref struct Program
                {
                    public string s;

                    public override bool Equals(object obj)
                    {
                        return false;
                    }
                }
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                // We are forcefully enabling the ImplementIEquatable option, as that is our way
                // to test that the option does nothing. The VS mode will ensure if the option
                // is not available it will not be shown.
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
            }.RunAsync();
        }

        [Fact]
        public async Task TestImplementIEquatableOnStructInNullableContextWithUnannotatedMetadata()
        {
            var code =
                """
                #nullable enable

                struct Foo
                {
                    public int Bar { get; }
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task TestImplementIEquatableOnStructInNullableContextWithAnnotatedMetadata()
        {
            var code =
                """
                #nullable enable

                using System;
                using System.Diagnostics.CodeAnalysis;

                struct Foo
                {
                    public bool Bar { get; }
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task TestImplementIEquatableOnClass_CSharp6()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    public string s;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestImplementIEquatableOnClass_CSharp7()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    public string s;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
                LanguageVersion = LanguageVersion.CSharp7,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestImplementIEquatableOnClass_CSharp8()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    public string s;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
                LanguageVersion = LanguageVersion.CSharp8,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestImplementIEquatableOnClass_CSharp9()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    public string s;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
                LanguageVersion = LanguageVersion.CSharp9,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestImplementIEquatableOnClassInNullableContextWithUnannotatedMetadata()
        {
            var code =
                """
                #nullable enable

                class Foo
                {
                    public int Bar { get; }
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task TestImplementIEquatableOnClassInNullableContextWithAnnotatedMetadata()
        {
            var code =
                """
                #nullable enable

                using System;
                using System.Diagnostics.CodeAnalysis;

                class Foo
                {
                    public bool Bar { get; }
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId),
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task TestDoNotOfferIEquatableIfTypeAlreadyImplementsIt()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program : {|CS0535:System.IEquatable<Program>|}
                {
                    public string s;
                    [||]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => Assert.Null(options.FirstOrDefault(i => i.Id == GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.ImplementIEquatableId)),
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestMissingReferences1()
        {
            await new VerifyCS.Test
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
                ReferenceAssemblies = ReferenceAssemblies.Default.WithAssemblies(ImmutableArray<string>.Empty),
            }.RunAsync();
        }

        [Fact]
        public async Task TestGetHashCodeInCheckedContext()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    [|int i;

                    string S { get; }|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
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
        }

        [Fact]
        public async Task TestGetHashCodeStruct()
        {
            var code =
                """
                using System.Collections.Generic;

                struct S
                {
                    [|int j;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGetHashCodeSystemHashCodeOneMember()
        {
            var code =
                """
                using System.Collections.Generic;
                namespace System { public struct HashCode { } }

                struct S
                {
                    [|int j;|]
                }
                """;
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
                TestCode = code,
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
            var publicHashCode =
                """
                using System.Collections.Generic;
                namespace System { public struct HashCode { } }
                """;
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
                            Sources = { ("HashCode.cs", publicHashCode) },
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
            var internalHashCode =
                """
                using System.Collections.Generic;
                namespace System { internal struct HashCode { } }
                """;
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
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    AdditionalProjects =
                    {
                        ["P1"] =
                        {
                            Sources = { ("HashCode.cs", internalHashCode) },
                        },
                    },
                    Sources = { code },
                    AdditionalProjectReferences = { "P1" },
                },
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGetHashCodeSystemHashCodeEightMembers()
        {
            var code =
                """
                using System.Collections.Generic;
                namespace System { public struct HashCode { } }

                struct S
                {
                    [|int j, k, l, m, n, o, p, q;|]
                }
                """;
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
                TestCode = code,
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
        public async Task TestGetHashCodeSystemHashCodeNineMembers()
        {
            var code =
                """
                using System.Collections.Generic;
                namespace System { public struct HashCode { public void Add<T>(T value) { } public int ToHashCode() => 0; } }

                struct S
                {
                    [|int j, k, l, m, n, o, p, q, r;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39916")]
        public async Task TestGetHashCodeSystemHashCodeNineMembers_Explicit()
        {
            var code =
                """
                using System.Collections.Generic;
                namespace System { public struct HashCode { public void Add<T>(T value) { } public int ToHashCode() => 0; } }

                struct S
                {
                    [|int j, k, l, m, n, o, p, q, r;|]
                }
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp6,
                Options = { PreferExplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact]
        public async Task TestEqualsSingleField_Patterns()
        {
            await VerifyCS.VerifyRefactoringAsync(
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
        }

        [Fact]
        public async Task TestEqualsSingleFieldInStruct_Patterns()
        {
            await VerifyCS.VerifyRefactoringAsync(
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
        }

        [Fact]
        public async Task TestEqualsBaseWithOverriddenEquals_Patterns()
        {
            var code =
                """
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
                """;
            var fixedCode =
                """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionIndex = 0,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestPartialSelection()
        {
            var code =
                """
                using System.Collections.Generic;

                class Program
                {
                    int [|a|];
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40053")]
        public async Task TestEqualityOperatorsNullableAnnotationWithReferenceType()
        {
            var code =
                """
                #nullable enable
                using System;

                namespace N
                {
                    public class C[||]
                    {
                        public int X;
                    }
                }
                """;
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
                TestCode = code,
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
        public async Task TestEqualityOperatorsNullableAnnotationWithValueType()
        {
            var code =
                """
                #nullable enable
                using System;

                namespace N
                {
                    public struct C[||]
                    {
                        public int X;
                    }
                }
                """;
            var fixedCode =
                """
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
                """;

            await new TestWithDialog
            {
                TestCode = code,
                FixedCode = fixedCode,
                MemberNames = default,
                OptionsCallback = options => EnableOption(options, GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider.GenerateOperatorsId),
                LanguageVersion = LanguageVersion.Default,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42574")]
        public async Task TestPartialTypes1()
        {
            await new TestWithDialog
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
                MemberNames = ImmutableArray.Create("bar"),
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42574")]
        public async Task TestPartialTypes2()
        {
            await new TestWithDialog
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
                MemberNames = ImmutableArray.Create("bar"),
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42574")]
        public async Task TestPartialTypes3()
        {
            await new TestWithDialog
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
                MemberNames = ImmutableArray.Create("bar"),
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42574")]
        public async Task TestPartialTypes4()
        {
            await new TestWithDialog
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
                MemberNames = ImmutableArray.Create("bar"),
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43290")]
        public async Task TestAbstractBase()
        {
            var code =
                """
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
                """;
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
                TestCode = code,
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
    }
}
