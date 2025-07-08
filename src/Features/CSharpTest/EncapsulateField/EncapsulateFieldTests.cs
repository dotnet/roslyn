// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.EncapsulateField;

[Trait(Traits.Feature, Traits.Features.EncapsulateField)]
public sealed class EncapsulateFieldTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new EncapsulateFieldRefactoringProvider();

    private OptionsCollection AllOptionsOff
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    internal Task TestAllOptionsOffAsync(
        TestHost host,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup,
        ParseOptions? parseOptions = null,
        CompilationOptions? compilationOptions = null,
        int index = 0,
        OptionsCollection? options = null)
    {
        options ??= new OptionsCollection(GetLanguage());
        options.AddRange(AllOptionsOff);

        return TestAsync(initialMarkup, expectedMarkup, parseOptions, compilationOptions, index, options, testHost: host);
    }

    [Theory, CombinatorialData]
    public async Task PrivateFieldToPropertyIgnoringReferences(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class goo
            {
                private int b[|a|]r;

                void baz()
                {
                    var q = bar;
                }
            }
            """, """
            class goo
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
            """, index: 1);
    }

    [Theory, CombinatorialData]
    public async Task PrivateNullableFieldToPropertyIgnoringReferences(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            #nullable enable
            class goo
            {
                private string? b[|a|]r;

                void baz()
                {
                    var q = bar;
                }
            }
            """, """
            #nullable enable
            class goo
            {
                private string? bar;

                public string? Bar
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
            """, index: 1);
    }

    [Theory, CombinatorialData]
    public async Task PrivateFieldToPropertyUpdatingReferences(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class goo
            {
                private int b[|a|]r;

                void baz()
                {
                    var q = Bar;
                }
            }
            """, """
            class goo
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
            """, index: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestCodeStyle1(TestHost host)
    {
        await TestInRegularAndScriptAsync("""
            class goo
            {
                private int b[|a|]r;

                void baz()
                {
                    var q = Bar;
                }
            }
            """, """
            class goo
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
            """,
            options: new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.Silent }
            },
            testHost: host);
    }

    [Theory, CombinatorialData]
    public async Task TestCodeStyle2(TestHost host)
    {
        await TestInRegularAndScriptAsync("""
            class goo
            {
                private int b[|a|]r;

                void baz()
                {
                    var q = Bar;
                }
            }
            """, """
            class goo
            {
                private int bar;

                public int Bar { get => bar; set => bar = value; }

                void baz()
                {
                    var q = Bar;
                }
            }
            """,
            options: new OptionsCollection(GetLanguage())
            {
                {  CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
            },
            testHost: host);
    }

    [Theory, CombinatorialData]
    public async Task PublicFieldIntoPublicPropertyIgnoringReferences(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class goo
            {
                public int b[|a|]r;

                void baz()
                {
                    var q = bar;
                }
            }
            """, """
            class goo
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
            """, index: 1);
    }

    [Theory, CombinatorialData]
    public async Task PublicFieldIntoPublicPropertyUpdatingReferences(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class goo
            {
                public int b[|a|]r;

                void baz()
                {
                    var q = bar;
                }
            }
            """, """
            class goo
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
            """, index: 0);
    }

    [Theory, CombinatorialData]
    public async Task StaticPreserved(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class Program
            {
                static int [|goo|];
            }
            """, """
            class Program
            {
                static int goo;

                public static int Goo
                {
                    get
                    {
                        return goo;
                    }

                    set
                    {
                        goo = value;
                    }
                }
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task UniqueNameGenerated(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class Program
            {
                [|int goo|];
                string Goo;
            }
            """, """
            class Program
            {
                int goo;
                string Goo;

                public int Goo1
                {
                    get
                    {
                        return goo;
                    }

                    set
                    {
                        goo = value;
                    }
                }
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task GenericField(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class C<T>
            {
                private [|T goo|];
            }
            """, """
            class C<T>
            {
                private T goo;

                public T Goo
                {
                    get
                    {
                        return goo;
                    }

                    set
                    {
                        goo = value;
                    }
                }
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task NewFieldNameIsUnique(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class goo
            {
                public [|int X|];
                private string x;
            }
            """, """
            class goo
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
            }
            """, index: 0);
    }

    [Theory, CombinatorialData]
    public async Task RespectReadonly(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class goo
            {
                private readonly [|int x|];
            }
            """, """
            class goo
            {
                private readonly int x;

                public int X
                {
                    get
                    {
                        return x;
                    }
                }
            }
            """, index: 0);
    }

    [Theory, CombinatorialData]
    public async Task PreserveNewAndConsiderBaseMemberNames(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class c
            {
                protected int goo;

                protected int Goo { get; set; }
            }

            class d : c
            {
                protected new int [|goo|];
            }
            """, """
            class c
            {
                protected int goo;

                protected int Goo { get; set; }
            }

            class d : c
            {
                private new int goo;

                protected int Goo1
                {
                    get
                    {
                        return goo;
                    }

                    set
                    {
                        goo = value;
                    }
                }
            }
            """, index: 0);
    }

    [Theory, CombinatorialData]
    public async Task EncapsulateMultiplePrivateFields(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class goo
            {
                private int [|x, y|];

                void bar()
                {
                    x = 1;
                    y = 2;
                }
            }
            """, """
            class goo
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
            }
            """, index: 0);
    }

    [Theory, CombinatorialData]
    public async Task EncapsulateMultiplePrivateFields2(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class goo
            {
                [|private int x;
                private int y|];

                void bar()
                {
                    x = 1;
                    y = 2;
                }
            }
            """, """
            class goo
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
            }
            """, index: 0);
    }

    [Theory, CombinatorialData]
    public async Task EncapsulateSinglePublicFieldInMultipleVariableDeclarationAndUpdateReferences(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class goo
            {
                public int [|x|], y;

                void bar()
                {
                    x = 1;
                    y = 2;
                }
            }
            """, """
            class goo
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
            }
            """, index: 0);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/694057"), CombinatorialData]
    public async Task ConstFieldNoGetter(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class Program
            {
                private const int [|bar|] = 3;
            }
            """, """
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
            """, index: 0);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/694276"), CombinatorialData]
    public async Task EncapsulateFieldNamedValue(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class Program
            {
                private const int [|bar|] = 3;
            }
            """, """
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
            """, index: 0);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/694276"), CombinatorialData]
    public async Task PublicFieldNamed__(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class Program
            {
                public int [|__|];
            }
            """, """
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
            """, index: 0);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/695046"), CombinatorialData]
    public async Task AvailableNotJustOnVariableName(TestHost host)
    {
        await TestActionCountAsync("""
            class Program
            {
                pri[||]vate int x;
            }
            """, 2, new TestParameters(testHost: host));
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705898"), CombinatorialData]
    public async Task CopyFieldAccessibility(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class Program
            {
                protected const int [|bar|] = 3;
            }
            """, """
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
            """, index: 0);
    }

    [Theory, CombinatorialData]
    public async Task UpdateReferencesCrossProject(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public class C
            {
                public int [|goo|];
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <ProjectReference>Assembly1</ProjectReference>
                    <Document>
            public class D
            {
                void bar()
                {
                    var c = new C;
                    c.goo = 3;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public class C
            {
                private int goo;

                public int Goo
                {
                    get
                    {
                        return goo;
                    }

                    set
                    {
                        goo = value;
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <ProjectReference>Assembly1</ProjectReference>
                    <Document>
            public class D
            {
                void bar()
                {
                    var c = new C;
                    c.Goo = 3;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, new CodeAnalysis.CSharp.CSharpParseOptions(), TestOptions.ReleaseExe);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713269"), CombinatorialData]
    public async Task PreserveUnsafe(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class C
            {
                unsafe int* [|goo|];
            }
            """, """
            class C
            {
                unsafe int* goo;

                public unsafe int* Goo
                {
                    get
                    {
                        return goo;
                    }

                    set
                    {
                        goo = value;
                    }
                }
            }
            """, index: 0);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713240"), CombinatorialData]
    public async Task ConsiderReturnTypeAccessibility(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            public class Program
            {
                private State [|state|];
            }

            internal enum State
            {
                WA
            }
            """, """
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
            """, index: 0);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713191"), CombinatorialData]
    public async Task DoNotReferToReadOnlyPropertyInConstructor(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class Program
            {
                public readonly int [|Field|];

                public Program(int f)
                {
                    this.Field = f;
                }
            }
            """, """
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
            }
            """, index: 0);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713191"), CombinatorialData]
    public async Task DoNotReferToStaticReadOnlyPropertyInConstructor(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class Program
            {
                public static readonly int [|Field|];

                static Program()
                {
                    Field = 3;
                }
            }
            """, """
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
            }
            """, index: 0);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/765959"), CombinatorialData]
    public async Task GenerateInTheCorrectPart(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            partial class Program {}

            partial class Program {
               private int [|x|];
            }
            """, """
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
            """);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829178"), CombinatorialData]
    public async Task ErrorTolerance(TestHost host)
    {
        await TestActionCountAsync("""
            class Program 
            {
                a b c [|b|]
            }
            """, count: 2, new TestParameters(testHost: host));
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/834072"), CombinatorialData]
    public async Task DuplicateFieldErrorTolerance(TestHost host)
    {
        await TestActionCountAsync("""
            class Program
            {
                public const string [|s|] = "S";
                public const string s = "S";
            }
            """, count: 2, new TestParameters(testHost: host));
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862517"), CombinatorialData]
    public async Task Trivia(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            namespace ConsoleApplication1
            {
                class Program
                {
                    // Some random comment
                    public int myI[||]nt;
                }
            }
            """, """
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
            }
            """);
    }

    [Theory, WorkItem(1096007, "https://github.com/dotnet/roslyn/issues/282"), CombinatorialData]
    public async Task DoNotEncapsulateOutsideTypeDeclaration(TestHost host)
    {
        await TestMissingInRegularAndScriptAsync(
@"var [|x|] = 1;", new TestParameters(testHost: host));

        await TestMissingInRegularAndScriptAsync(
            """
            namespace N
            {
                var [|x|] = 1;
            }
            """, new TestParameters(testHost: host));

        await TestMissingInRegularAndScriptAsync(
            """
            enum E
            {
                [|x|] = 1;
            }
            """, new TestParameters(testHost: host));
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/5524"), CombinatorialData]
    public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_TurkishDottedI(TestHost host)
    {
        using (new CultureContext(new CultureInfo("tr-TR", useUserOverride: false)))
        {
            await TestAllOptionsOffAsync(host,
                """
                class C
                {
                    int [|iyi|];
                }
                """,
                """
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
                """);
        }
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/5524"), CombinatorialData]
    public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_TurkishUndottedI(TestHost host)
    {
        using (new CultureContext(new CultureInfo("tr-TR", useUserOverride: false)))
        {
            await TestAllOptionsOffAsync(host,
                """
                class C
                {
                    int [|ırak|];
                }
                """,
                """
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
                """);
        }
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/5524"), CombinatorialData]
    public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_Arabic(TestHost host)
    {
        using (new CultureContext(new CultureInfo("ar-EG", useUserOverride: false)))
        {
            await TestAllOptionsOffAsync(host,
                """
                class C
                {
                    int [|بيت|];
                }
                """,
                """
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
                """);
        }
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/5524"), CombinatorialData]
    public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_Spanish(TestHost host)
    {
        using (new CultureContext(new CultureInfo("es-ES", useUserOverride: false)))
        {
            await TestAllOptionsOffAsync(host,
                """
                class C
                {
                    int [|árbol|];
                }
                """,
                """
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
                """);
        }
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/5524"), CombinatorialData]
    public async Task AlwaysUseEnglishUSCultureWhenFixingVariableNames_Greek(TestHost host)
    {
        using (new CultureContext(new CultureInfo("el-GR", useUserOverride: false)))
        {
            await TestAllOptionsOffAsync(host,
                """
                class C
                {
                    int [|σκύλος|];
                }
                """,
                """
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
                """);
        }
    }

    [Theory, CombinatorialData]
    public async Task TestEncapsulateEscapedIdentifier(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class C
            {
                int [|@class|];
            }
            """, """
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
            """);
    }

    [Theory, CombinatorialData]
    public async Task TestEncapsulateEscapedIdentifierAndQualifiedAccess(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class C
            {
                int [|@class|];
            }
            """, """
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
            """, options: Option(CodeStyleOptions2.QualifyFieldAccess, true, NotificationOption2.Error));
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/7090"), CombinatorialData]
    public async Task ApplyCurrentThisPrefixStyle(TestHost host)
    {
        await TestAllOptionsOffAsync(host,
            """
            class C
            {
                int [|i|];
            }
            """,
            """
            class C
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
            }
            """, options: Option(CodeStyleOptions2.QualifyFieldAccess, true, NotificationOption2.Error));
    }

    [Theory, CombinatorialData, CompilerTrait(CompilerFeature.Tuples)]
    public async Task TestTuple(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class C
            {
                private (int, string) b[|o|]b;

                void M()
                {
                    var q = bob;
                }
            }
            """, """
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
            """, index: 1);
    }

    [Theory, CombinatorialData, CompilerTrait(CompilerFeature.Tuples)]
    public async Task TupleWithNames(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            class C
            {
                private (int a, string b) b[|o|]b;

                void M()
                {
                    var q = bob.b;
                }
            }
            """, """
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
            """, index: 1);
    }

    [Theory, CombinatorialData, CompilerTrait(CompilerFeature.FunctionPointers)]
    public async Task FunctionPointer(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            unsafe class C
            {
                private delegate*<int, string> f[|i|]eld;

                void M()
                {
                    var q = field;
                }
            }
            """, """
            unsafe class C
            {
                private delegate*<int, string> f[|i|]eld;

                public unsafe delegate*<int, string> Field
                {
                    get
                    {
                        return field;
                    }

                    set
                    {
                        field = value;
                    }
                }

                void M()
                {
                    var q = field;
                }
            }
            """, index: 1);
    }

    [Theory, CombinatorialData, CompilerTrait(CompilerFeature.FunctionPointers)]
    public async Task FunctionPointerWithPrivateTypeParameter(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            unsafe class C
            {
                private struct S { }
                private delegate*<S, string> f[|i|]eld;

                void M()
                {
                    var q = field;
                }
            }
            """, """
            unsafe class C
            {
                private struct S { }
                private delegate*<S, string> f[|i|]eld;

                private unsafe delegate*<S, string> Field
                {
                    get
                    {
                        return field;
                    }

                    set
                    {
                        field = value;
                    }
                }

                void M()
                {
                    var q = field;
                }
            }
            """, index: 1);
    }

    [Theory, CombinatorialData, CompilerTrait(CompilerFeature.FunctionPointers)]
    public async Task FunctionPointerWithPrivateTypeReturnValue(TestHost host)
    {
        await TestAllOptionsOffAsync(host, """
            unsafe class C
            {
                private struct S { }
                private delegate*<string, S> f[|i|]eld;

                void M()
                {
                    var q = field;
                }
            }
            """, """
            unsafe class C
            {
                private struct S { }
                private delegate*<string, S> f[|i|]eld;

                private unsafe delegate*<string, S> Field
                {
                    get
                    {
                        return field;
                    }

                    set
                    {
                        field = value;
                    }
                }

                void M()
                {
                    var q = field;
                }
            }
            """, index: 1);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/75210")]
    public async Task PrimaryConstructor(TestHost host, bool andUseProperty)
    {
        await TestAllOptionsOffAsync(host, """
            public class C(object o)
            {
                public readonly int [|A|] = 1;
            }
            """, """
            public class C(object o)
            {
                private readonly int a = 1;

                public int A
                {
                    get
                    {
                        return a;
                    }
                }
            }
            """,
            index: andUseProperty ? 0 : 1);
    }
}
