// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public void PrivateFieldToPropertyIgnoringReferences()
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
            Test(text, expected, compareTokens: false, index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void PrivateFieldToPropertyUpdatingReferences()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void PublicFieldIntoPublicPropertyIgnoringReferences()
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
            Test(text, expected, compareTokens: false, index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void PublicFieldIntoPublicPropertyUpdatingReferences()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void StaticPreserved()
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
            Test(text, expected, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void UniqueNameGenerated()
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
            Test(text, expected, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void GenericField()
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
            Test(text, expected, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void NewFieldNameIsUnique()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void RespectReadonly()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void PreserveNewAndConsiderBaseMemberNames()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateMultiplePrivateFields()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateMultiplePrivateFields2()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateSinglePublicFieldInMultipleVariableDeclarationAndUpdateReferences()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(694057)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void ConstFieldNoGetter()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(694276)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateFieldNamedValue()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(694276)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void PublicFieldNamed__()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(695046)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void AvailableNotJustOnVariableName()
        {
            var text = @"
class Program
{
    pri[||]vate int x;
}
";

            TestActionCount(text, 2);
        }

        [WorkItem(705898)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void CopyFieldAccessibility()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void UpdateReferencesCrossProject()
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
            Test(text, expected, new CodeAnalysis.CSharp.CSharpParseOptions(), TestOptions.ReleaseExe, compareTokens: false);
        }

        [WorkItem(713269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void PreserveUnsafe()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(713240)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void ConsiderReturnTypeAccessibility()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(713191)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void DoNotReferToReadOnlyPropertyInConstructor()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(713191)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void DoNotReferToStaticReadOnlyPropertyInConstructor()
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
            Test(text, expected, compareTokens: false, index: 0);
        }

        [WorkItem(765959)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void GenerateInTheCorrectPart()
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
            Test(text, expected);
        }

        [WorkItem(829178)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void ErrorTolerance()
        {
            var text = @"class Program 
{
    a b c [|b|]
}";

            using (var workspace = CreateWorkspaceFromFile(text, null, null))
            {
                var result = GetCodeRefactoring(workspace);
                Assert.NotNull(result);
            }
        }

        [WorkItem(834072)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void DuplicateFieldErrorTolerance()
        {
            var text = @"
class Program
{
    public const string [|s|] = ""S"";
    public const string s = ""S"";
    }
";

            using (var workspace = CreateWorkspaceFromFile(text, null, null))
            {
                var result = GetCodeRefactoring(workspace);
                Assert.NotNull(result);
            }
        }

        [WorkItem(862517)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void Trivia()
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

            Test(text, expected);
        }

        [WorkItem(1096007, "https://github.com/dotnet/roslyn/issues/282")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void DoNotEncapsulateOutsideTypeDeclaration()
        {
            TestMissing(@"
var [|x|] = 1;");

            TestMissing(@"
namespace N
{
    var [|x|] = 1;
}");

            TestMissing(@"
enum E
{
    [|x|] = 1;
}");
        }

        [WorkItem(5524, "https://github.com/dotnet/roslyn/issues/5524")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void AlwaysUseEnglishUSCultureWhenFixingVariableNames_TurkishDottedI()
        {
            using (new CultureContext("tr-TR"))
            {
                Test(@"
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
        public void AlwaysUseEnglishUSCultureWhenFixingVariableNames_TurkishUndottedI()
        {
            using (new CultureContext("tr-TR"))
            {
                Test(@"
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
        public void AlwaysUseEnglishUSCultureWhenFixingVariableNames_Arabic()
        {
            using (new CultureContext("ar-EG"))
            {
                Test(@"
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
        public void AlwaysUseEnglishUSCultureWhenFixingVariableNames_Spanish()
        {
            using (new CultureContext("es-ES"))
            {
                Test(@"
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
        public void AlwaysUseEnglishUSCultureWhenFixingVariableNames_Greek()
        {
            using (new CultureContext("el-GR"))
            {
                Test(@"
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
