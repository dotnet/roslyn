﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.EncapsulateField
{
    public class EncapsulateFieldTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new EncapsulateFieldRefactoringProvider();

        private IDictionary<OptionKey, object> AllOptionsOff =>
            OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithNoneEnforcement));

        internal Task TestAllOptionsOffAsync(
            string initialMarkup, string expectedMarkup,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            int index = 0, bool ignoreTrivia = true,
            IDictionary<OptionKey, object> options = null)
        {
            options = options ?? new Dictionary<OptionKey, object>();
            foreach (var kvp in AllOptionsOff)
            {
                options.Add(kvp);
            }

            return TestAsync(initialMarkup, expectedMarkup,
                parseOptions, compilationOptions, index, ignoreTrivia, options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task PrivateFieldToPropertyIgnoringReferences()
        {
            var text = @"
class foo
{
    private int b[|a|]r;

    void baz()
    {
        var q = bar;
    }
}
";

            var expected = @"
class foo
{
    private int bar;

    public int Bar
    {
        get
        {
            return bar;
        }

        set
        {
            bar = value;
        }
    }

    void baz()
    {
        var q = bar;
    }
}
";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task PrivateFieldToPropertyUpdatingReferences()
        {
            var text = @"
class foo
{
    private int b[|a|]r;

    void baz()
    {
        var q = Bar;
    }
}
";

            var expected = @"
class foo
{
    private int bar;

    public int Bar
    {
        get
        {
            return bar;
        }

        set
        {
            bar = value;
        }
    }

    void baz()
    {
        var q = Bar;
    }
}
";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task TestCodeStyle1()
        {
            var text = @"
class foo
{
    private int b[|a|]r;

    void baz()
    {
        var q = Bar;
    }
}
";

            var expected = @"
class foo
{
    private int bar;

    public int Bar
    {
        get
        {
            return bar;
        }

        set
        {
            bar = value;
        }
    }

    void baz()
    {
        var q = Bar;
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, 
                options: OptionsSet(
                    SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible, NotificationOption.None),
                    SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption.None)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task TestCodeStyle2()
        {
            var text = @"
class foo
{
    private int b[|a|]r;

    void baz()
    {
        var q = Bar;
    }
}
";

            var expected = @"
class foo
{
    private int bar;

    public int Bar
    {
        get => bar;

        set => bar = value;
    }

    void baz()
    {
        var q = Bar;
    }
}
";
            await TestInRegularAndScriptAsync(text, expected,
                options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task PublicFieldIntoPublicPropertyIgnoringReferences()
        {
            var text = @"
class foo
{
    public int b[|a|]r;

    void baz()
    {
        var q = bar;
    }
}
";

            var expected = @"
class foo
{
    private int bar;

    public int Bar
    {
        get
        {
            return bar;
        }

        set
        {
            bar = value;
        }
    }

    void baz()
    {
        var q = bar;
    }
}
";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task PublicFieldIntoPublicPropertyUpdatingReferences()
        {
            var text = @"
class foo
{
    public int b[|a|]r;

    void baz()
    {
        var q = bar;
    }
}
";

            var expected = @"
class foo
{
    private int bar;

    public int Bar
    {
        get
        {
            return bar;
        }

        set
        {
            bar = value;
        }
    }

    void baz()
    {
        var q = Bar;
    }
}
";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task StaticPreserved()
        {
            var text = @"class Program
{
    static int [|foo|];
}";

            var expected = @"class Program
{
    static int foo;

    public static int Foo
    {
        get
        {
            return foo;
        }

        set
        {
            foo = value;
        }
    }
}";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task UniqueNameGenerated()
        {
            var text = @"
class Program
{
    [|int foo|];
    string Foo;
}";

            var expected = @"
class Program
{
    int foo;
    string Foo;

    public int Foo1
    {
        get
        {
            return foo;
        }

        set
        {
            foo = value;
        }
    }
}";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task GenericField()
        {
            var text = @"
class C<T>
{
    private [|T foo|];
}";

            var expected = @"
class C<T>
{
    private T foo;

    public T Foo
    {
        get
        {
            return foo;
        }

        set
        {
            foo = value;
        }
    }
}";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task NewFieldNameIsUnique()
        {
            var text = @"
class foo
{
    public [|int X|];
    private string x;
}";

            var expected = @"
class foo
{
    private int x1;
    private string x;

    public int X
    {
        get
        {
            return x1;
        }

        set
        {
            x1 = value;
        }
    }
}";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task RespectReadonly()
        {
            var text = @"
class foo
{
    private readonly [|int x|];
}";

            var expected = @"
class foo
{
    private readonly int x;

    public int X
    {
        get
        {
            return x;
        }
    }
}";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task PreserveNewAndConsiderBaseMemberNames()
        {
            var text = @"
class c
{
    protected int foo;

    protected int Foo { get; set; }
}

class d : c
{
    protected new int [|foo|];
}";

            var expected = @"
class c
{
    protected int foo;

    protected int Foo { get; set; }
}

class d : c
{
    private new int foo;

    protected int Foo1
    {
        get
        {
            return foo;
        }

        set
        {
            foo = value;
        }
    }
}";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateMultiplePrivateFields()
        {
            var text = @"
class foo
{
    private int [|x, y|];

    void bar()
    {
        x = 1;
        y = 2;
    }
}";

            var expected = @"
class foo
{
    private int x, y;

    public int X
    {
        get
        {
            return x;
        }

        set
        {
            x = value;
        }
    }

    public int Y
    {
        get
        {
            return y;
        }

        set
        {
            y = value;
        }
    }

    void bar()
    {
        X = 1;
        Y = 2;
    }
}";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateMultiplePrivateFields2()
        {
            var text = @"
class foo
{
    [|private int x;
    private int y|];

    void bar()
    {
        x = 1;
        y = 2;
    }
}";

            var expected = @"
class foo
{
    private int x;
    private int y;

    public int X
    {
        get
        {
            return x;
        }

        set
        {
            x = value;
        }
    }

    public int Y
    {
        get
        {
            return y;
        }

        set
        {
            y = value;
        }
    }

    void bar()
    {
        X = 1;
        Y = 2;
    }
}";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateSinglePublicFieldInMultipleVariableDeclarationAndUpdateReferences()
        {
            var text = @"
class foo
{
    public int [|x|], y;

    void bar()
    {
        x = 1;
        y = 2;
    }
}";

            var expected = @"
class foo
{
    public int y;
    private int x;

    public int X
    {
        get
        {
            return x;
        }

        set
        {
            x = value;
        }
    }

    void bar()
    {
        X = 1;
        y = 2;
    }
}";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [WorkItem(694057, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/694057")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task ConstFieldNoGetter()
        {
            var text = @"
class Program
{
    private const int [|bar|] = 3;
}
";

            var expected = @"
class Program
{
    private const int bar = 3;

    public static int Bar
    {
        get
        {
            return bar;
        }
    }
}
";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [WorkItem(694276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/694276")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateFieldNamedValue()
        {
            var text = @"
class Program
{
    private const int [|bar|] = 3;
}
";

            var expected = @"
class Program
{
    private const int bar = 3;

    public static int Bar
    {
        get
        {
            return bar;
        }
    }
}
";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [WorkItem(694276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/694276")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task PublicFieldNamed__()
        {
            var text = @"
class Program
{
    public int [|__|];
}
";

            var expected = @"
class Program
{
    private int __1;

    public int __
    {
        get
        {
            return __1;
        }

        set
        {
            __1 = value;
        }
    }
}
";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [WorkItem(695046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/695046")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task AvailableNotJustOnVariableName()
        {
            var text = @"
class Program
{
    pri[||]vate int x;
}
";

            await TestActionCountAsync(text, 2);
        }

        [WorkItem(705898, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705898")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task CopyFieldAccessibility()
        {
            var text = @"
class Program
{
    protected const int [|bar|] = 3;
}
";

            var expected = @"
class Program
{
    private const int bar = 3;

    protected static int Bar
    {
        get
        {
            return bar;
        }
    }
}
";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task UpdateReferencesCrossProject()
        {
            var text = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
public class C
{
    public int [|foo|];
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
public class D
{
    void bar()
    {
        var c = new C;
        c.foo = 3;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
public class C
{
    private int foo;

    public int Foo
    {
        get
        {
            return foo;
        }

        set
        {
            foo = value;
        }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
public class D
{
    void bar()
    {
        var c = new C;
        c.Foo = 3;
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestAllOptionsOffAsync(text, expected, new CodeAnalysis.CSharp.CSharpParseOptions(), TestOptions.ReleaseExe, ignoreTrivia: false);
        }

        [WorkItem(713269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713269")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task PreserveUnsafe()
        {
            var text = @"
class C
{
    unsafe int* [|foo|];
}
";

            var expected = @"
class C
{
    unsafe int* foo;

    public unsafe int* Foo
    {
        get
        {
            return foo;
        }

        set
        {
            foo = value;
        }
    }
}
";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [WorkItem(713240, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713240")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task ConsiderReturnTypeAccessibility()
        {
            var text = @"
public class Program
{
    private State [|state|];
}
 
internal enum State
{
    WA
}
";

            var expected = @"
public class Program
{
    private State state;

    internal State State
    {
        get
        {
            return state;
        }

        set
        {
            state = value;
        }
    }
}
 
internal enum State
{
    WA
}
";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [WorkItem(713191, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713191")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task DoNotReferToReadOnlyPropertyInConstructor()
        {
            var text = @"
class Program
{
    public readonly int [|Field|];
 
    public Program(int f)
    {
        this.Field = f;
    }
}";

            var expected = @"
class Program
{
    private readonly int field;

    public Program(int f)
    {
        this.field = f;
    }

    public int Field
    {
        get
        {
            return field;
        }
    }
}";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [WorkItem(713191, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713191")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task DoNotReferToStaticReadOnlyPropertyInConstructor()
        {
            var text = @"
class Program
{
    public static readonly int [|Field|];
 
    static Program()
    {
        Field = 3;
    }
}";

            var expected = @"
class Program
{
    private static readonly int field;

    static Program()
    {
        field = 3;
    }

    public static int Field
    {
        get
        {
            return field;
        }
    }
}";
            await TestAllOptionsOffAsync(text, expected, ignoreTrivia: false, index: 0);
        }

        [WorkItem(765959, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/765959")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task GenerateInTheCorrectPart()
        {
            var text = @"
partial class Program {}

partial class Program {
   private int [|x|];
}
";
            var expected = @"
partial class Program {}

partial class Program {
    private int x;

    public int X
    {
        get
        {
            return x;
        }
        set
        {
            x = value;
        }
    }
}
";
            await TestAllOptionsOffAsync(text, expected);
        }

        [WorkItem(829178, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829178")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task ErrorTolerance()
        {
            var text = @"class Program 
{
    a b c [|b|]
}";

            await TestActionCountAsync(text, count: 2);
        }

        [WorkItem(834072, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/834072")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task DuplicateFieldErrorTolerance()
        {
            var text = @"
class Program
{
    public const string [|s|] = ""S"";
    public const string s = ""S"";
}
";

            await TestActionCountAsync(text, count: 2);
        }

        [WorkItem(862517, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862517")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task Trivia()
        {
            var text = @"
namespace ConsoleApplication1
{
    class Program
    {
        // Some random comment
        public int myI[||]nt;
    }
}
";

            var expected = @"
namespace ConsoleApplication1
{
    class Program
    {
        // Some random comment
        private int myInt;

        public int MyInt
        {
            get
            {
                return myInt;
            }

            set
            {
                myInt = value;
            }
        }
    }
}";

            await TestAllOptionsOffAsync(text, expected);
        }

        [WorkItem(1096007, "https://github.com/dotnet/roslyn/issues/282")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task DoNotEncapsulateOutsideTypeDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"var [|x|] = 1;");

            await TestMissingInRegularAndScriptAsync(
@"namespace N
{
    var [|x|] = 1;
}");

            await TestMissingInRegularAndScriptAsync(
@"enum E
{
    [|x|] = 1;
}");
        }

        [WorkItem(5524, "https://github.com/dotnet/roslyn/issues/5524")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_TurkishDottedI()
        {
            using (new CultureContext("tr-TR"))
            {
                await TestAllOptionsOffAsync(
@"class C
{
    int [|iyi|];
}", 
@"class C
{
    int iyi;

    public int Iyi
    {
        get
        {
            return iyi;
        }

        set
        {
            iyi = value;
        }
    }
}");
            }
        }

        [WorkItem(5524, "https://github.com/dotnet/roslyn/issues/5524")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_TurkishUndottedI()
        {
            using (new CultureContext("tr-TR"))
            {
                await TestAllOptionsOffAsync(
@"class C
{
    int [|ırak|];
}", 
@"class C
{
    int ırak;

    public int Irak
    {
        get
        {
            return ırak;
        }

        set
        {
            ırak = value;
        }
    }
}");
            }
        }

        [WorkItem(5524, "https://github.com/dotnet/roslyn/issues/5524")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_Arabic()
        {
            using (new CultureContext("ar-EG"))
            {
                await TestAllOptionsOffAsync(
@"class C
{
    int [|بيت|];
}", 
@"class C
{
    int بيت;

    public int بيت1
    {
        get
        {
            return بيت;
        }

        set
        {
            بيت = value;
        }
    }
}");
            }
        }

        [WorkItem(5524, "https://github.com/dotnet/roslyn/issues/5524")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_Spanish()
        {
            using (new CultureContext("es-ES"))
            {
                await TestAllOptionsOffAsync(
@"class C
{
    int [|árbol|];
}", 
@"class C
{
    int árbol;

    public int Árbol
    {
        get
        {
            return árbol;
        }

        set
        {
            árbol = value;
        }
    }
}");
            }
        }

        [WorkItem(5524, "https://github.com/dotnet/roslyn/issues/5524")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_Greek()
        {
            using (new CultureContext("el-GR"))
            {
                await TestAllOptionsOffAsync(
@"class C
{
    int [|σκύλος|];
}", 
@"class C
{
    int σκύλος;

    public int Σκύλος
    {
        get
        {
            return σκύλος;
        }

        set
        {
            σκύλος = value;
        }
    }
}");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task TestEncapsulateEscapedIdentifier()
        {
            await TestAllOptionsOffAsync(@"
class C
{
    int [|@class|];
}
", @"
class C
{
    int @class;

    public int Class
    {
        get
        {
            return @class;
        }

        set
        {
            @class = value;
        }
    }
}
", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task TestEncapsulateEscapedIdentifierAndQualifiedAccess()
        {
            await TestAllOptionsOffAsync(@"
class C
{
    int [|@class|];
}
", @"
class C
{
    int @class;

    public int Class
    {
        get
        {
            return this.@class;
        }

        set
        {
            this.@class = value;
        }
    }
}
", ignoreTrivia: false, options: Option(CodeStyleOptions.QualifyFieldAccess, true, NotificationOption.Error));
        }

        [WorkItem(7090, "https://github.com/dotnet/roslyn/issues/7090")]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task ApplyCurrentThisPrefixStyle()
        {
            await TestAllOptionsOffAsync(
@"class C
{
    int [|i|];
}", 
@"class C
{
    int i;

    public int I
    {
        get
        {
            return this.i;
        }

        set
        {
            this.i = value;
        }
    }
}", options: Option(CodeStyleOptions.QualifyFieldAccess, true, NotificationOption.Error));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        public async Task TestTuple()
        {
            var text = @"
class C
{
    private (int, string) b[|o|]b;

    void M()
    {
        var q = bob;
    }
}
";

            var expected = @"
class C
{
    private (int, string) bob;

    public (int, string) Bob
    {
        get
        {
            return bob;
        }

        set
        {
            bob = value;
        }
    }

    void M()
    {
        var q = bob;
    }
}
";
            await TestAllOptionsOffAsync(
                text, expected, ignoreTrivia: false, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        public async Task TupleWithNames()
        {
            var text = @"
class C
{
    private (int a, string b) b[|o|]b;

    void M()
    {
        var q = bob.b;
    }
}
";

            var expected = @"
class C
{
    private (int a, string b) bob;

    public (int a, string b) Bob
    {
        get
        {
            return bob;
        }

        set
        {
            bob = value;
        }
    }

    void M()
    {
        var q = bob.b;
    }
}
";
            await TestAllOptionsOffAsync(
                text, expected, ignoreTrivia: false, index: 1);
        }
    }
}
