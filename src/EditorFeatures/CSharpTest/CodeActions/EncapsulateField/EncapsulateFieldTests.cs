// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EncapsulateField;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.EncapsulateField
{
    public class EncapsulateFieldTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new EncapsulateFieldRefactoringProvider();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(694057)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(694276)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(694276)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(695046)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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

        [WorkItem(705898)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, new CodeAnalysis.CSharp.CSharpParseOptions(), TestOptions.ReleaseExe, compareTokens: false);
        }

        [WorkItem(713269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(713240)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(713191)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(713191)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(765959)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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
            await TestAsync(text, expected);
        }

        [WorkItem(829178)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task ErrorTolerance()
        {
            var text = @"class Program 
{
    a b c [|b|]
}";

            using (var workspace = await CreateWorkspaceFromFileAsync(text, null, null))
            {
                var result = await GetCodeRefactoringAsync(workspace);
                Assert.NotNull(result);
            }
        }

        [WorkItem(834072)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task DuplicateFieldErrorTolerance()
        {
            var text = @"
class Program
{
    public const string [|s|] = ""S"";
    public const string s = ""S"";
    }
";

            using (var workspace = await CreateWorkspaceFromFileAsync(text, null, null))
            {
                var result = await GetCodeRefactoringAsync(workspace);
                Assert.NotNull(result);
            }
        }

        [WorkItem(862517)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
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

            await TestAsync(text, expected);
        }

        [WorkItem(1096007, "https://github.com/dotnet/roslyn/issues/282")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task DoNotEncapsulateOutsideTypeDeclaration()
        {
            await TestMissingAsync(@"
var [|x|] = 1;");

            await TestMissingAsync(@"
namespace N
{
    var [|x|] = 1;
}");

            await TestMissingAsync(@"
enum E
{
    [|x|] = 1;
}");
        }

        [WorkItem(5524, "https://github.com/dotnet/roslyn/issues/5524")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_TurkishDottedI()
        {
            using (new CultureContext("tr-TR"))
            {
                await TestAsync(@"
class C
{
    int [|iyi|];
}
", @"
class C
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
}
");
            }
        }

        [WorkItem(5524, "https://github.com/dotnet/roslyn/issues/5524")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_TurkishUndottedI()
        {
            using (new CultureContext("tr-TR"))
            {
                await TestAsync(@"
class C
{
    int [|ırak|];
}
", @"
class C
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
}
");
            }
        }

        [WorkItem(5524, "https://github.com/dotnet/roslyn/issues/5524")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_Arabic()
        {
            using (new CultureContext("ar-EG"))
            {
                await TestAsync(@"
class C
{
    int [|بيت|];
}
", @"
class C
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
}
");
            }
        }

        [WorkItem(5524, "https://github.com/dotnet/roslyn/issues/5524")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_Spanish()
        {
            using (new CultureContext("es-ES"))
            {
                await TestAsync(@"
class C
{
    int [|árbol|];
}
", @"
class C
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
}
");
            }
        }

        [WorkItem(5524, "https://github.com/dotnet/roslyn/issues/5524")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_Greek()
        {
            using (new CultureContext("el-GR"))
            {
                await TestAsync(@"
class C
{
    int [|σκύλος|];
}
", @"
class C
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
}
");
            }
        }
    }
}
