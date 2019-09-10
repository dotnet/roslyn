// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider((IPickMembersService)parameters.fixProviderData);

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
parameters: CSharp6);
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
parameters: CSharp6);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestNullableReferenceIEquatable()
        {
            await TestInRegularAndScript1Async(
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
}", index: 1);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
@"using System.Collections.Generic;

struct ReallyLongName
{
    int i;

    string S { get; }

    public override bool Equals(object obj)
    {
        if (!(obj is ReallyLongName))
        {
            return false;
        }

        var name = (ReallyLongName)obj;
        return i == name.i &&
               S == name.S;
    }
}",
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
        var program = obj as Program;
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
        var program = obj as Program;
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
parameters: CSharp6);
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
        var @class = obj as Class1;
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
        var program = obj as Program;
        return program != null &&
               i == program.i &&
               S == program.S;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = -538000506;
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
@"using System.Collections.Generic;

struct S
{
    int j;

    public override bool Equals(object obj)
    {
        if (!(obj is S))
        {
            return false;
        }

        var s = (S)obj;
        return j == s.j;
    }

    public override int GetHashCode()
    {
        return 1424088837 + j.GetHashCode();
    }
}",
index: 1,
parameters: CSharp6);
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
struct S
{
    int j;

    public override bool Equals(object obj)
    {
        if (!(obj is S))
        {
            return false;
        }

        var s = (S)obj;
        return j == s.j;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(j);
    }
}",
index: 1,
parameters: CSharp6);
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
struct S
{
    int j, k, l, m, n, o, p, q;

    public override bool Equals(object obj)
    {
        if (!(obj is S))
        {
            return false;
        }

        var s = (S)obj;
        return j == s.j &&
               k == s.k &&
               l == s.l &&
               m == s.m &&
               n == s.n &&
               o == s.o &&
               p == s.p &&
               q == s.q;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(j, k, l, m, n, o, p, q);
    }
}",
index: 1,
parameters: CSharp6);
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
struct S
{
    int j, k, l, m, n, o, p, q, r;

    public override bool Equals(object obj)
    {
        if (!(obj is S))
        {
            return false;
        }

        var s = (S)obj;
        return j == s.j &&
               k == s.k &&
               l == s.l &&
               m == s.m &&
               n == s.n &&
               o == s.o &&
               p == s.p &&
               q == s.q &&
               r == s.r;
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
}",
index: 1,
parameters: CSharp6);
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
@"using System.Collections.Generic;

struct Program
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
    }
}
