﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateEqualsAndGetHashCodeFromMembers
{
    using static GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider;

    public class GenerateEqualsAndGetHashCodeFromMembersTests : AbstractCSharpCodeActionTest
    {
        private static readonly TestParameters CSharp6 =
            new TestParameters(parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));

        private static readonly TestParameters CSharpLatest =
            new TestParameters(parseOptions: TestOptions.Regular);

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider((IPickMembersService)parameters.fixProviderData);

        private TestParameters CSharp6Implicit => CSharp6.WithOptions(this.PreferImplicitTypeWithInfo());
        private TestParameters CSharp6Explicit => CSharp6.WithOptions(this.PreferExplicitTypeWithInfo());
        private TestParameters CSharpLatestImplicit => CSharpLatest.WithOptions(this.PreferImplicitTypeWithInfo());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsSingleField()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program
{
    [|int a;|]
}",
@"using System.Collections.Generic;

class Program
{
    int a;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               a == program.a;
    }
}",
parameters: CSharp6Implicit);
        }

        [WorkItem(39916, "https://github.com/dotnet/roslyn/issues/39916")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsSingleField_PreferExplicitType()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program
{
    [|int a;|]
}",
@"using System.Collections.Generic;

class Program
{
    int a;

    public override bool Equals(object obj)
    {
        Program program = obj as Program;
        return program != null &&
               a == program.a;
    }
}",
parameters: CSharp6Explicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestReferenceIEquatable()
        {
            await TestInRegularAndScript1Async(
@"
using System;
using System.Collections.Generic;

class S : IEquatable<S> { }

class Program
{
    [|S a;|]
}",
@"
using System;
using System.Collections.Generic;

class S : IEquatable<S> { }

class Program
{
    S a;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               EqualityComparer<S>.Default.Equals(a, program.a);
    }
}",
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestNullableReferenceIEquatable()
        {
            await TestInRegularAndScriptAsync(
@"#nullable enable

using System;
using System.Collections.Generic;

class S : IEquatable<S> { }

class Program
{
    [|S? a;|]
}",
@"#nullable enable

using System;
using System.Collections.Generic;

class S : IEquatable<S> { }

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
}", index: 1, options: this.PreferImplicitTypeWithInfo());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestValueIEquatable()
        {
            await TestInRegularAndScript1Async(
@"
using System;
using System.Collections.Generic;

struct S : IEquatable<S> { }

class Program
{
    [|S a;|]
}",
@"
using System;
using System.Collections.Generic;

struct S : IEquatable<S> { }

class Program
{
    S a;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               a.Equals(program.a);
    }
}",
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsLongName()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class ReallyLongName
{
    [|int a;|]
}",
@"using System.Collections.Generic;

class ReallyLongName
{
    int a;

    public override bool Equals(object obj)
    {
        var name = obj as ReallyLongName;
        return name != null &&
               a == name.a;
    }
}",
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsKeywordName()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class ReallyLongLong
{
    [|long a;|]
}",
@"using System.Collections.Generic;

class ReallyLongLong
{
    long a;

    public override bool Equals(object obj)
    {
        var @long = obj as ReallyLongLong;
        return @long != null &&
               a == @long.a;
    }
}",
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsProperty()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class ReallyLongName
{
    [|int a;

    string B { get; }|]
}",
@"using System.Collections.Generic;

class ReallyLongName
{
    int a;

    string B { get; }

    public override bool Equals(object obj)
    {
        var name = obj as ReallyLongName;
        return name != null &&
               a == name.a &&
               B == name.B;
    }
}",
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsBaseTypeWithNoEquals()
        {
            await TestInRegularAndScript1Async(
@"class Base
{
}

class Program : Base
{
    [|int i;|]
}",
@"class Base
{
}

class Program : Base
{
    int i;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               i == program.i;
    }
}",
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsBaseWithOverriddenEquals()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Base
{
    public override bool Equals(object o)
    {
    }
}

class Program : Base
{
    [|int i;

    string S { get; }|]
}",
@"using System.Collections.Generic;

class Base
{
    public override bool Equals(object o)
    {
    }
}

class Program : Base
{
    int i;

    string S { get; }

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               base.Equals(obj) &&
               i == program.i &&
               S == program.S;
    }
}",
index: 0,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsOverriddenDeepBase()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Base
{
    public override bool Equals(object o)
    {
    }
}

class Middle : Base
{
}

class Program : Middle
{
    [|int i;

    string S { get; }|]
}",
@"using System.Collections.Generic;

class Base
{
    public override bool Equals(object o)
    {
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
        return program != null &&
               base.Equals(obj) &&
               i == program.i &&
               S == program.S;
    }
}",
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsStruct()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

struct ReallyLongName
{
    [|int i;

    string S { get; }|]
}",
@"using System;
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsStructCSharpLatest()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

struct ReallyLongName
{
    [|int i;

    string S { get; }|]
}",
@"using System;
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsStructAlreadyImplementsIEquatable()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

struct ReallyLongName : IEquatable<ReallyLongName>
{
    [|int i;

    string S { get; }|]
}",
@"using System;
using System.Collections.Generic;

struct ReallyLongName : IEquatable<ReallyLongName>
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsStructAlreadyHasOperators()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

struct ReallyLongName
{
    [|int i;

    string S { get; }|]

    public static bool operator ==(ReallyLongName left, ReallyLongName right) => false;
    public static bool operator !=(ReallyLongName left, ReallyLongName right) => false;
}",
@"using System;
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsStructAlreadyImplementsIEquatableAndHasOperators()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

struct ReallyLongName : IEquatable<ReallyLongName>
{
    [|int i;

    string S { get; }|]

    public static bool operator ==(ReallyLongName left, ReallyLongName right) => false;
    public static bool operator !=(ReallyLongName left, ReallyLongName right) => false;
}",
@"using System;
using System.Collections.Generic;

struct ReallyLongName : IEquatable<ReallyLongName>
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsGenericType()
        {
            var code = @"
using System.Collections.Generic;
class Program<T>
{
    [|int i;|]
}
";

            var expected = @"
using System.Collections.Generic;
class Program<T>
{
    int i;

    public override bool Equals(object obj)
    {
        var program = obj as Program<T>;
        return program != null &&
               i == program.i;
    }
}
";

            await TestInRegularAndScript1Async(code, expected,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsNullableContext()
        {
            await TestInRegularAndScript1Async(
@"#nullable enable

class Program
{
    [|int a;|]
}",
@"#nullable enable

class Program
{
    int a;

    public override bool Equals(object? obj)
    {
        return obj is Program program &&
               a == program.a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeSingleField1()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program
{
    [|int i;|]
}",
@"using System.Collections.Generic;

class Program
{
    int i;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               i == program.i;
    }

    public override int GetHashCode()
    {
        return 165851236 + i.GetHashCode();
    }
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeSingleField2()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program
{
    [|int j;|]
}",
@"using System.Collections.Generic;

class Program
{
    int j;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               j == program.j;
    }

    public override int GetHashCode()
    {
        return 1424088837 + j.GetHashCode();
    }
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeWithBaseHashCode1()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Base {
    public override int GetHashCode() => 0;
}

class Program : Base
{
    [|int j;|]
}",
@"using System.Collections.Generic;

class Base {
    public override int GetHashCode() => 0;
}

class Program : Base
{
    int j;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               j == program.j;
    }

    public override int GetHashCode()
    {
        var hashCode = 339610899;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        hashCode = hashCode * -1521134295 + j.GetHashCode();
        return hashCode;
    }
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeWithBaseHashCode2()
        {
            await TestWithPickMembersDialogAsync(
@"using System.Collections.Generic;

class Base {
    public override int GetHashCode() => 0;
}

class Program : Base
{
    int j;
    [||]
}",
@"using System.Collections.Generic;

class Base {
    public override int GetHashCode() => 0;
}

class Program : Base
{
    int j;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null;
    }

    public override int GetHashCode()
    {
        return 624022166 + base.GetHashCode();
    }
}",
chosenSymbols: new string[] { },
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeSingleField_CodeStyle1()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program
{
    [|int i;|]
}",
@"using System.Collections.Generic;

class Program
{
    int i;

    public override bool Equals(object obj)
    {
        Program program = obj as Program;
        return program != null &&
               i == program.i;
    }

    public override int GetHashCode() => 165851236 + i.GetHashCode();
}",
index: 1,
parameters: new TestParameters(
    options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement),
    parseOptions: CSharp6.parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeTypeParameter()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program<T>
{
    [|T i;|]
}",
@"using System.Collections.Generic;

class Program<T>
{
    T i;

    public override bool Equals(object obj)
    {
        var program = obj as Program<T>;
        return program != null &&
               EqualityComparer<T>.Default.Equals(i, program.i);
    }

    public override int GetHashCode()
    {
        return 165851236 + EqualityComparer<T>.Default.GetHashCode(i);
    }
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeGenericType()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program<T>
{
    [|Program<T> i;|]
}",
@"using System.Collections.Generic;

class Program<T>
{
    Program<T> i;

    public override bool Equals(object obj)
    {
        var program = obj as Program<T>;
        return program != null &&
               EqualityComparer<Program<T>>.Default.Equals(i, program.i);
    }

    public override int GetHashCode()
    {
        return 165851236 + EqualityComparer<Program<T>>.Default.GetHashCode(i);
    }
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeMultipleMembers()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program
{
    [|int i;

    string S { get; }|]
}",
@"using System.Collections.Generic;

class Program
{
    int i;

    string S { get; }

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
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
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestSmartTagText1()
        {
            await TestSmartTagTextAsync(
@"using System.Collections.Generic;

class Program
{
    [|bool b;
    HashSet<string> s;|]

    public Program(bool b)
    {
        this.b = b;
    }
}",
FeaturesResources.Generate_Equals_object);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestSmartTagText2()
        {
            await TestSmartTagTextAsync(
@"using System.Collections.Generic;

class Program
{
    [|bool b;
    HashSet<string> s;|]

    public Program(bool b)
    {
        this.b = b;
    }
}",
FeaturesResources.Generate_Equals_and_GetHashCode,
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestSmartTagText3()
        {
            await TestSmartTagTextAsync(
@"using System.Collections.Generic;

class Program
{
    [|bool b;
    HashSet<string> s;|]

    public Program(bool b)
    {
        this.b = b;
    }
}",
FeaturesResources.Generate_Equals_and_GetHashCode,
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task Tuple_Disabled()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class C
{
    [|(int, string) a;|]
}",
@"using System.Collections.Generic;

class C
{
    (int, string) a;

    public override bool Equals(object obj)
    {
        var c = obj as C;
        return c != null &&
               a.Equals(c.a);
    }
}",
index: 0,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task Tuples_Equals()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class C
{
    [|(int, string) a;|]
}",
@"using System.Collections.Generic;

class C
{
    (int, string) a;

    public override bool Equals(object obj)
    {
        var c = obj as C;
        return c != null &&
               a.Equals(c.a);
    }
}",
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TupleWithNames_Equals()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class C
{
    [|(int x, string y) a;|]
}",
@"using System.Collections.Generic;

class C
{
    (int x, string y) a;

    public override bool Equals(object obj)
    {
        var c = obj as C;
        return c != null &&
               a.Equals(c.a);
    }
}",
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task Tuple_HashCode()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program
{
    [|(int, string) i;|]
}",
@"using System.Collections.Generic;

class Program
{
    (int, string) i;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               i.Equals(program.i);
    }

    public override int GetHashCode()
    {
        return 165851236 + i.GetHashCode();
    }
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TupleWithNames_HashCode()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program
{
    [|(int x, string y) i;|]
}",
@"using System.Collections.Generic;

class Program
{
    (int x, string y) i;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               i.Equals(program.i);
    }

    public override int GetHashCode()
    {
        return 165851236 + i.GetHashCode();
    }
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task StructWithoutGetHashCodeOverride_ShouldCallGetHashCodeDirectly()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Foo
{
    [|Bar bar;|]
}

struct Bar
{
}",
@"using System.Collections.Generic;

class Foo
{
    Bar bar;

    public override bool Equals(object obj)
    {
        var foo = obj as Foo;
        return foo != null &&
               EqualityComparer<Bar>.Default.Equals(bar, foo.bar);
    }

    public override int GetHashCode()
    {
        return 999205674 + bar.GetHashCode();
    }
}

struct Bar
{
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task StructWithGetHashCodeOverride_ShouldCallGetHashCodeDirectly()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Foo
{
    [|Bar bar;|]
}

struct Bar
{
    public override int GetHashCode() => 0;
}",
@"using System.Collections.Generic;

class Foo
{
    Bar bar;

    public override bool Equals(object obj)
    {
        var foo = obj as Foo;
        return foo != null &&
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
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task NullableStructWithoutGetHashCodeOverride_ShouldCallGetHashCodeDirectly()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Foo
{
    [|Bar? bar;|]
}

struct Bar
{
}",
@"using System.Collections.Generic;

class Foo
{
    Bar? bar;

    public override bool Equals(object obj)
    {
        var foo = obj as Foo;
        return foo != null &&
               EqualityComparer<Bar?>.Default.Equals(bar, foo.bar);
    }

    public override int GetHashCode()
    {
        return 999205674 + bar.GetHashCode();
    }
}

struct Bar
{
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task StructTypeParameter_ShouldCallGetHashCodeDirectly()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Foo<TBar> where TBar : struct
{
    [|TBar bar;|]
}",
@"using System.Collections.Generic;

class Foo<TBar> where TBar : struct
{
    TBar bar;

    public override bool Equals(object obj)
    {
        var foo = obj as Foo<TBar>;
        return foo != null &&
               EqualityComparer<TBar>.Default.Equals(bar, foo.bar);
    }

    public override int GetHashCode()
    {
        return 999205674 + bar.GetHashCode();
    }
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task NullableStructTypeParameter_ShouldCallGetHashCodeDirectly()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Foo<TBar> where TBar : struct
{
    [|TBar? bar;|]
}",
@"using System.Collections.Generic;

class Foo<TBar> where TBar : struct
{
    TBar? bar;

    public override bool Equals(object obj)
    {
        var foo = obj as Foo<TBar>;
        return foo != null &&
               EqualityComparer<TBar?>.Default.Equals(bar, foo.bar);
    }

    public override int GetHashCode()
    {
        return 999205674 + bar.GetHashCode();
    }
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task Enum_ShouldCallGetHashCodeDirectly()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Foo
{
    [|Bar bar;|]
}

enum Bar
{
}",
@"using System.Collections.Generic;

class Foo
{
    Bar bar;

    public override bool Equals(object obj)
    {
        var foo = obj as Foo;
        return foo != null &&
               bar == foo.bar;
    }

    public override int GetHashCode()
    {
        return 999205674 + bar.GetHashCode();
    }
}

enum Bar
{
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task PrimitiveValueType_ShouldCallGetHashCodeDirectly()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Foo
{
    [|ulong bar;|]
}",
@"using System.Collections.Generic;

class Foo
{
    ulong bar;

    public override bool Equals(object obj)
    {
        var foo = obj as Foo;
        return foo != null &&
               bar == foo.bar;
    }

    public override int GetHashCode()
    {
        return 999205674 + bar.GetHashCode();
    }
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestWithDialog1()
        {
            await TestWithPickMembersDialogAsync(
@"using System.Collections.Generic;

class Program
{
    int a;
    string b;
    [||]
}",
@"using System.Collections.Generic;

class Program
{
    int a;
    string b;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               a == program.a &&
               b == program.b;
    }
}",
chosenSymbols: new[] { "a", "b" },
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestWithDialog2()
        {
            await TestWithPickMembersDialogAsync(
@"using System.Collections.Generic;

class Program
{
    int a;
    string b;
    bool c;
    [||]
}",
@"using System.Collections.Generic;

class Program
{
    int a;
    string b;
    bool c;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               c == program.c &&
               b == program.b;
    }
}",
chosenSymbols: new[] { "c", "b" },
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestWithDialog3()
        {
            await TestWithPickMembersDialogAsync(
@"using System.Collections.Generic;

class Program
{
    int a;
    string b;
    bool c;
    [||]
}",
@"using System.Collections.Generic;

class Program
{
    int a;
    string b;
    bool c;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null;
    }
}",
chosenSymbols: new string[] { },
parameters: CSharp6Implicit);
        }

        [WorkItem(17643, "https://github.com/dotnet/roslyn/issues/17643")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestWithDialogNoBackingField()
        {
            await TestWithPickMembersDialogAsync(
@"
class Program
{
    public int F { get; set; }
    [||]
}",
@"
class Program
{
    public int F { get; set; }

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               F == program.F;
    }
}",
chosenSymbols: null,
parameters: CSharp6Implicit);
        }

        [WorkItem(25690, "https://github.com/dotnet/roslyn/issues/25690")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestWithDialogNoIndexer()
        {
            await TestWithPickMembersDialogAsync(
@"
class Program
{
    public int P => 0;
    public int this[int index] => 0;
    [||]
}",
@"
class Program
{
    public int P => 0;
    public int this[int index] => 0;

    public override bool Equals(object obj)
    {
        return obj is Program program &&
               P == program.P;
    }
}",
chosenSymbols: null);
        }

        [WorkItem(25707, "https://github.com/dotnet/roslyn/issues/25707")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestWithDialogNoSetterOnlyProperty()
        {
            await TestWithPickMembersDialogAsync(
@"
class Program
{
    public int P => 0;
    public int S { set { } }
    [||]
}",
@"
class Program
{
    public int P => 0;
    public int S { set { } }

    public override bool Equals(object obj)
    {
        return obj is Program program &&
               P == program.P;
    }
}",
chosenSymbols: null);
        }

        [WorkItem(41958, "https://github.com/dotnet/roslyn/issues/41958")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestWithDialogInheritedMembers()
        {
            await TestWithPickMembersDialogAsync(
@"
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
}",
@"
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
}",
chosenSymbols: null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGenerateOperators1()
        {
            await TestWithPickMembersDialogAsync(
@"
using System.Collections.Generic;

class Program
{
    public string s;
    [||]
}",
@"
using System.Collections.Generic;

class Program
{
    public string s;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
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
}",
chosenSymbols: null,
optionsCallback: options => EnableOption(options, GenerateOperatorsId),
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGenerateOperators2()
        {
            await TestWithPickMembersDialogAsync(
@"
using System.Collections.Generic;

class Program
{
    public string s;
    [||]
}",
@"
using System.Collections.Generic;

class Program
{
    public string s;

    public override bool Equals(object obj)
    {
        Program program = obj as Program;
        return program != null &&
               s == program.s;
    }

    public static bool operator ==(Program left, Program right) => EqualityComparer<Program>.Default.Equals(left, right);
    public static bool operator !=(Program left, Program right) => !(left == right);
}",
chosenSymbols: null,
optionsCallback: options => EnableOption(options, GenerateOperatorsId),
parameters: new TestParameters(
    options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement),
    parseOptions: CSharp6.parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGenerateOperators3()
        {
            await TestWithPickMembersDialogAsync(
@"
using System.Collections.Generic;

class Program
{
    public string s;
    [||]

    public static bool operator ==(Program left, Program right) => true;
}",
@"
using System.Collections.Generic;

class Program
{
    public string s;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               s == program.s;
    }

    public static bool operator ==(Program left, Program right) => true;
}",
chosenSymbols: null,
optionsCallback: options => Assert.Null(options.FirstOrDefault(i => i.Id == GenerateOperatorsId)),
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGenerateOperators4()
        {
            await TestWithPickMembersDialogAsync(
@"
using System.Collections.Generic;

struct Program
{
    public string s;
    [||]
}",
@"
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
}",
chosenSymbols: null,
optionsCallback: options => EnableOption(options, GenerateOperatorsId),
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGenerateLiftedOperators()
        {
            await TestInRegularAndScript1Async(
@"
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
}",
@"
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
        return foo != null &&
               BooleanValue == foo.BooleanValue &&
               DecimalValue == foo.DecimalValue &&
               EnumValue == foo.EnumValue &&
               DateTimeValue == foo.DateTimeValue;
    }
}

enum Bar
{
}",
index: 0,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task LiftedOperatorIsNotUsedWhenDirectOperatorWouldNotBeUsed()
        {
            await TestInRegularAndScript1Async(
@"
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

    public override bool Equals(object obj) => obj is Bar bar && Equals(bar);

    public bool Equals(Bar other) => value == other.value;

    public override int GetHashCode() => -1584136870 + value.GetHashCode();

    public static bool operator ==(Bar left, Bar right) => left.Equals(right);

    public static bool operator !=(Bar left, Bar right) => !(left == right);
}",
@"
using System;
using System.Collections.Generic;

class Foo
{
    public Bar Value { get; }
    public Bar? NullableValue { get; }

    public override bool Equals(object obj)
    {
        var foo = obj as Foo;
        return foo != null &&
               Value.Equals(foo.Value) &&
               EqualityComparer<Bar?>.Default.Equals(NullableValue, foo.NullableValue);
    }
}

struct Bar : IEquatable<Bar>
{
    private readonly int value;

    public override bool Equals(object obj) => obj is Bar bar && Equals(bar);

    public bool Equals(Bar other) => value == other.value;

    public override int GetHashCode() => -1584136870 + value.GetHashCode();

    public static bool operator ==(Bar left, Bar right) => left.Equals(right);

    public static bool operator !=(Bar left, Bar right) => !(left == right);
}",
index: 0,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestImplementIEquatableOnStruct()
        {
            await TestWithPickMembersDialogAsync(
@"
using System.Collections.Generic;

struct Program
{
    public string s;
    [||]
}",
@"
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
}",
chosenSymbols: null,
optionsCallback: options => EnableOption(options, ImplementIEquatableId),
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        [WorkItem(25708, "https://github.com/dotnet/roslyn/issues/25708")]
        public async Task TestOverrideEqualsOnRefStructReturnsFalse()
        {
            await TestWithPickMembersDialogAsync(
@"
ref struct Program
{
    public string s;
    [||]
}",
@"
ref struct Program
{
    public string s;

    public override bool Equals(object obj)
    {
        return false;
    }
}",
chosenSymbols: null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        [WorkItem(25708, "https://github.com/dotnet/roslyn/issues/25708")]
        public async Task TestImplementIEquatableOnRefStructSkipsIEquatable()
        {
            await TestWithPickMembersDialogAsync(
@"
ref struct Program
{
    public string s;
    [||]
}",
@"
ref struct Program
{
    public string s;

    public override bool Equals(object obj)
    {
        return false;
    }
}",
chosenSymbols: null,
// We are forcefully enabling the ImplementIEquatable option, as that is our way
// to test that the option does nothing. The VS mode will ensure if the option
// is not available it will not be shown.
optionsCallback: options => EnableOption(options, ImplementIEquatableId));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestImplementIEquatableOnStructInNullableContextWithUnannotatedMetadata()
        {
            await TestWithPickMembersDialogAsync(
@"#nullable enable

struct Foo
{
    public int Bar { get; }
    [||]
}",
@"#nullable enable

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
}",
chosenSymbols: null,
optionsCallback: options => EnableOption(options, ImplementIEquatableId),
parameters: new TestParameters(TestOptions.Regular8));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestImplementIEquatableOnStructInNullableContextWithAnnotatedMetadata()
        {
            await TestWithPickMembersDialogAsync(
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""false"">
        <Document><![CDATA[
#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

struct Foo
{
    public bool Bar { get; }
    [||]
}

namespace System
{
    public class Object { }
    public struct Boolean { }

    public interface IEquatable<T>
    {
        bool Equals([AllowNull] T other);
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    public sealed class AllowNullAttribute : Attribute { }
}
]]>
        </Document>
    </Project>
</Workspace>",
@"
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

namespace System
{
    public class Object { }
    public struct Boolean { }

    public interface IEquatable<T>
    {
        bool Equals([AllowNull] T other);
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    public sealed class AllowNullAttribute : Attribute { }
}
",
chosenSymbols: null,
optionsCallback: options => EnableOption(options, ImplementIEquatableId),
parameters: new TestParameters(TestOptions.Regular8, retainNonFixableDiagnostics: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestImplementIEquatableOnClass()
        {
            await TestWithPickMembersDialogAsync(
@"
using System.Collections.Generic;

class Program
{
    public string s;
    [||]
}",
@"
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
        return other != null &&
               s == other.s;
    }
}",
chosenSymbols: null,
optionsCallback: options => EnableOption(options, ImplementIEquatableId),
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestImplementIEquatableOnClassInNullableContextWithUnannotatedMetadata()
        {
            await TestWithPickMembersDialogAsync(
@"#nullable enable

class Foo
{
    public int Bar { get; }
    [||]
}",
@"#nullable enable

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
        return other != null &&
               Bar == other.Bar;
    }
}",
chosenSymbols: null,
optionsCallback: options => EnableOption(options, ImplementIEquatableId),
parameters: new TestParameters(TestOptions.Regular8));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestImplementIEquatableOnClassInNullableContextWithAnnotatedMetadata()
        {
            await TestWithPickMembersDialogAsync(
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""false"">
        <Document><![CDATA[
#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

class Foo
{
    public bool Bar { get; }
    [||]
}

namespace System
{
    public class Object { }
    public struct Boolean { }

    public interface IEquatable<T>
    {
        bool Equals([AllowNull] T other);
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    public sealed class AllowNullAttribute : Attribute { }
}
]]>
        </Document>
    </Project>
</Workspace>",
@"
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
        return other != null &&
               Bar == other.Bar;
    }
}

namespace System
{
    public class Object { }
    public struct Boolean { }

    public interface IEquatable<T>
    {
        bool Equals([AllowNull] T other);
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    public sealed class AllowNullAttribute : Attribute { }
}
",
chosenSymbols: null,
optionsCallback: options => EnableOption(options, ImplementIEquatableId),
parameters: new TestParameters(TestOptions.Regular8, retainNonFixableDiagnostics: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestDoNotOfferIEquatableIfTypeAlreadyImplementsIt()
        {
            await TestWithPickMembersDialogAsync(
@"
using System.Collections.Generic;

class Program : System.IEquatable<Program>
{
    public string s;
    [||]
}",
@"
using System.Collections.Generic;

class Program : System.IEquatable<Program>
{
    public string s;

    public override bool Equals(object obj)
    {
        var program = obj as Program;
        return program != null &&
               s == program.s;
    }
}",
chosenSymbols: null,
optionsCallback: options => Assert.Null(options.FirstOrDefault(i => i.Id == ImplementIEquatableId)),
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestMissingReferences()
        {
            await TestWithPickMembersDialogAsync(
@"
<Workspace>
    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='false' LanguageVersion='CSharp6'>
        <Document FilePath='Test1.cs'>
public class Class1
{
    int i;
    [||]

    public void F()
    {
    }
}
        </Document>
    </Project>
</Workspace>",
@"
public class Class1
{
    int i;

    public override global::System.Boolean Equals(global::System.Object obj)
    {
        Class1 @class = obj as Class1;
        return @class != null;
    }

    public void F()
    {
    }

    public override global::System.Int32 GetHashCode()
    {
        return 0;
    }
}
        ",
chosenSymbols: new string[] { },
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeInCheckedContext()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program
{
    [|int i;

    string S { get; }|]
}",
@"using System.Collections.Generic;

class Program
{
    int i;

    string S { get; }

    public override bool Equals(object obj)
    {
        Program program = obj as Program;
        return program != null &&
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
}",
index: 1,
parameters: new TestParameters(
    parseOptions: CSharp6.parseOptions,
    compilationOptions: new CSharpCompilationOptions(
        OutputKind.DynamicallyLinkedLibrary, checkOverflow: true)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeStruct()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

struct S
{
    [|int j;|]
}",
@"using System;
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
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeSystemHashCodeOneMember()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

struct S
{
    [|int j;|]
}",
@"using System;
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
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [WorkItem(37297, "https://github.com/dotnet/roslyn/issues/37297")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestPublicSystemHashCodeOtherProject()
        {
            await TestInRegularAndScript1Async(
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" Name=""P1"">
        <Document>
using System.Collections.Generic;
namespace System { public struct HashCode { } }
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" Name=""P2"">
        <ProjectReference>P1</ProjectReference>
        <Document>
struct S
{
    [|int j;|]
}
        </Document>
    </Project>
</Workspace>",
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" Name=""P1"">
        <Document>
using System.Collections.Generic;
namespace System { public struct HashCode { } }
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" Name=""P2"">
        <ProjectReference>P1</ProjectReference>
        <Document>
using System;

struct S : IEquatable&lt;S&gt;
{
    int j;

    public override bool Equals(object obj)
    {
        return obj is S s &amp;&amp; Equals(s);
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
        </Document>
    </Project>
</Workspace>",
index: 1,
parameters: CSharp6Implicit);
        }

        [WorkItem(37297, "https://github.com/dotnet/roslyn/issues/37297")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestInternalSystemHashCode()
        {
            await TestInRegularAndScript1Async(
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" Name=""P1"">
        <Document>
using System.Collections.Generic;
namespace System { internal struct HashCode { } }
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" Name=""P2"">
        <ProjectReference>P1</ProjectReference>
        <Document>
struct S
{
    [|int j;|]
}
        </Document>
    </Project>
</Workspace>",

@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" Name=""P1"">
        <Document>
using System.Collections.Generic;
namespace System { internal struct HashCode { } }
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" Name=""P2"">
        <ProjectReference>P1</ProjectReference>
        <Document>
using System;

struct S : IEquatable&lt;S&gt;
{
    int j;

    public override bool Equals(object obj)
    {
        return obj is S s &amp;&amp; Equals(s);
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
        </Document>
    </Project>
</Workspace>",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeSystemHashCodeEightMembers()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

struct S
{
    [|int j, k, l, m, n, o, p, q;|]
}",
@"using System;
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
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeSystemHashCodeNineMembers()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

struct S
{
    [|int j, k, l, m, n, o, p, q, r;|]
}",
@"using System;
using System.Collections.Generic;
namespace System { public struct HashCode { } }

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
}",
index: 1,
parameters: CSharp6Implicit);
        }

        [WorkItem(39916, "https://github.com/dotnet/roslyn/issues/39916")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestGetHashCodeSystemHashCodeNineMembers_Explicit()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

struct S
{
    [|int j, k, l, m, n, o, p, q, r;|]
}",
@"using System;
using System.Collections.Generic;
namespace System { public struct HashCode { } }

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
}",
index: 1,
parameters: CSharp6Explicit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsSingleField_Patterns()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Program
{
    [|int a;|]
}",
@"using System.Collections.Generic;

class Program
{
    int a;

    public override bool Equals(object obj)
    {
        return obj is Program program &&
               a == program.a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsSingleFieldInStruct_Patterns()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

struct Program
{
    [|int a;|]
}",
@"using System;
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualsBaseWithOverriddenEquals_Patterns()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;

class Base
{
    public override bool Equals(object o)
    {
    }
}

class Program : Base
{
    [|int i;

    string S { get; }|]
}",
@"using System.Collections.Generic;

class Base
{
    public override bool Equals(object o)
    {
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
}",
index: 0);
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestPartialSelection()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;

class Program
{
    int [|a|];
}");
        }

        [WorkItem(40053, "https://github.com/dotnet/roslyn/issues/40053")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualityOperatorsNullableAnnotationWithReferenceType()
        {
            await TestWithPickMembersDialogAsync(
@"
#nullable enable
using System;

namespace N
{
    public class C[||]
    {
        public int X;
    }
}",
@"
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
}",
chosenSymbols: null,
optionsCallback: options => EnableOption(options, GenerateOperatorsId),
parameters: CSharpLatestImplicit);
        }

        [WorkItem(40053, "https://github.com/dotnet/roslyn/issues/40053")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestEqualityOperatorsNullableAnnotationWithValueType()
        {
            await TestWithPickMembersDialogAsync(
@"
#nullable enable
using System;

namespace N
{
    public struct C[||]
    {
        public int X;
    }
}",
@"
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
}",
chosenSymbols: null,
optionsCallback: options => EnableOption(options, GenerateOperatorsId),
parameters: CSharpLatestImplicit);
        }

        [WorkItem(42574, "https://github.com/dotnet/roslyn/issues/42574")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestPartialTypes1()
        {
            await TestWithPickMembersDialogAsync(
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document>
partial class Goo
{
    int bar;
    [||]
}
        </Document>
        <Document>
partial class Goo
{


}
        </Document>
    </Project>
</Workspace>",
@"
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
        ",
chosenSymbols: new[] { "bar" },
index: 1);
        }

        [WorkItem(42574, "https://github.com/dotnet/roslyn/issues/42574")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestPartialTypes2()
        {
            await TestWithPickMembersDialogAsync(
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document>
partial class Goo
{
    int bar;

}
        </Document>
        <Document>
partial class Goo
{

[||]
}
        </Document>
    </Project>
</Workspace>",
@"
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
        ",
chosenSymbols: new[] { "bar" },
index: 1);
        }

        [WorkItem(42574, "https://github.com/dotnet/roslyn/issues/42574")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestPartialTypes3()
        {
            await TestWithPickMembersDialogAsync(
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document>
partial class Goo
{

[||]
}
        </Document>
        <Document>
partial class Goo
{
    int bar;

}
        </Document>
    </Project>
</Workspace>",
@"
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
        ",
chosenSymbols: new[] { "bar" },
index: 1);
        }

        [WorkItem(42574, "https://github.com/dotnet/roslyn/issues/42574")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestPartialTypes4()
        {
            await TestWithPickMembersDialogAsync(
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document>
partial class Goo
{


}
        </Document>
        <Document>
partial class Goo
{
    int bar;
[||]
}
        </Document>
    </Project>
</Workspace>",
@"
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
        ",
chosenSymbols: new[] { "bar" },
index: 1);
        }

        [WorkItem(43290, "https://github.com/dotnet/roslyn/issues/43290")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestAbstractBase()
        {
            await TestInRegularAndScript1Async(
@"namespace System { public struct HashCode { } }

abstract class Base
{
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();
}

class Derived : Base
{
    [|public int P { get; }|]
}",
@"using System;

namespace System { public struct HashCode { } }

abstract class Base
{
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();
}

class Derived : Base
{
    public int P { get; }

    public override bool Equals(object obj)
    {
        return obj is Derived derived &&
               P == derived.P;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(P);
    }
}",
index: 1,
parameters: CSharpLatest);
        }
    }
}
