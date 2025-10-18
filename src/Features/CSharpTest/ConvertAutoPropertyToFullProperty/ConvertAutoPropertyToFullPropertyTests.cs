// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAutoPropertyToFullProperty;

[Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
public sealed partial class ConvertAutoPropertyToFullPropertyTests : AbstractCSharpCodeActionTest_NoEditor
{
    private static readonly CSharpParseOptions CSharp14 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp14);

    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider();

    [Theory]
    [InlineData("set"), InlineData("init")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/48133")]
    public Task SimpleAutoPropertyTest(string setter)
        => TestInRegularAndScriptAsync($$"""
            class TestClass
            {
                public int G[||]oo { get; {{setter}}; }
            }
            """, $$"""
            class TestClass
            {
                private int goo;

                public int Goo
                {
                    get
                    {
                        return goo;
                    }
                    {{setter}}
                    {
                        goo = value;
                    }
                }
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task ExtraLineAfterProperty()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get; set; }

            }
            """, """
            class TestClass
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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task WithInitialValue()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get; set; } = 2
            }
            """, """
            class TestClass
            {
                private int goo = 2;

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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task WithCalculatedInitialValue()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                const int num = 345;
                public int G[||]oo { get; set; } = 2*num
            }
            """, """
            class TestClass
            {
                const int num = 345;
                private int goo = 2 * num;

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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task WithPrivateSetter()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get; private set; }
            }
            """, """
            class TestClass
            {
                private int goo;

                public int Goo
                {
                    get
                    {
                        return goo;
                    }
                    private set
                    {
                        goo = value;
                    }
                }
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task WithFieldNameAlreadyUsed()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                private int goo;

                public int G[||]oo { get; private set; }
            }
            """, """
            class TestClass
            {
                private int goo;
                private int goo1;

                public int Goo
                {
                    get
                    {
                        return goo1;
                    }
                    private set
                    {
                        goo1 = value;
                    }
                }
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task WithComments()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                // Comments before
                public int G[||]oo { get; private set; } //Comments during
                //Comments after
            }
            """, """
            class TestClass
            {
                private int goo;

                // Comments before
                public int Goo
                {
                    get
                    {
                        return goo;
                    }
                    private set
                    {
                        goo = value;
                    }
                } //Comments during
                //Comments after
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task WithExpressionBody()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get; set; }
            }
            """, """
            class TestClass
            {
                private int goo;

                public int Goo { get => goo; set => goo = value; }
            }
            """, new(options: PreferExpressionBodiedAccessorsWhenPossible));

    [Fact]
    public Task WithExpressionBodyWhenOnSingleLine()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get; set; }
            }
            """, """
            class TestClass
            {
                private int goo;

                public int Goo { get => goo; set => goo = value; }
            }
            """, new(options: PreferExpressionBodiedAccessorsWhenOnSingleLine));

    [Fact]
    public Task WithExpressionBodyWhenOnSingleLine2()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo
                {
                    get;
                    set;
                }
            }
            """, """
            class TestClass
            {
                private int goo;

                public int Goo
                {
                    get => goo;
                    set => goo = value;
                }
            }
            """, new(options: PreferExpressionBodiedAccessorsWhenOnSingleLine));

    [Fact]
    public Task WithExpressionBodyWithTrivia()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get /* test */ ; set /* test2 */ ; }
            }
            """, """
            class TestClass
            {
                private int goo;

                public int Goo { get /* test */ => goo; set /* test2 */ => goo = value; }
            }
            """, new(options: PreferExpressionBodiedAccessorsWhenPossible));

    [Fact]
    public Task WithPropertyOpenBraceOnSameLine()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get; set; }
            }
            """, """
            class TestClass
            {
                private int goo;

                public int Goo {
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
            """, new(options: DoNotPreferExpressionBodiedAccessorsAndPropertyOpenBraceOnSameLine));

    [Fact]
    public Task WithAccessorOpenBraceOnSameLine()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get; set; }
            }
            """, """
            class TestClass
            {
                private int goo;

                public int Goo
                {
                    get {
                        return goo;
                    }
                    set {
                        goo = value;
                    }
                }
            }
            """, new(options: DoNotPreferExpressionBodiedAccessorsAndAccessorOpenBraceOnSameLine));

    [Fact]
    public Task StaticProperty()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public static int G[||]oo { get; set; }
            }
            """, """
            class TestClass
            {
                private static int goo;

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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task ProtectedProperty()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                protected int G[||]oo { get; set; }
            }
            """, """
            class TestClass
            {
                private int goo;

                protected int Goo
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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task InternalProperty()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                internal int G[||]oo { get; set; }
            }
            """, """
            class TestClass
            {
                private int goo;

                internal int Goo
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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task WithAttributes()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                [A]
                public int G[||]oo { get; set; }
            }
            """, """
            class TestClass
            {
                private int goo;

                [A]
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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task CommentsInAccessors()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                /// <summary>
                /// test stuff here
                /// </summary>
                public int Testg[||]oo { /* test1 */ get /* test2 */; /* test3 */ set /* test4 */; /* test5 */ } /* test6 */
            }
            """, """
            class TestClass
            {
                private int testgoo;

                /// <summary>
                /// test stuff here
                /// </summary>
                public int Testgoo
                { /* test1 */
                    get /* test2 */
                    {
                        return testgoo;
                    } /* test3 */
                    set /* test4 */
                    {
                        testgoo = value;
                    } /* test5 */
                } /* test6 */
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task OverrideProperty()
        => TestInRegularAndScriptAsync("""
            class MyBaseClass
            {
                public virtual string Name { get; set; }
            }

            class MyDerivedClass : MyBaseClass
            {
                public override string N[||]ame {get; set;}
            }
            """, """
            class MyBaseClass
            {
                public virtual string Name { get; set; }
            }

            class MyDerivedClass : MyBaseClass
            {
                private string name;

                public override string Name
                {
                    get
                    {
                        return name;
                    }
                    set
                    {
                        name = value;
                    }
                }
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task SealedProperty()
        => TestInRegularAndScriptAsync("""
            class MyClass
            {
                public sealed string N[||]ame {get; set;}
            }
            """, """
            class MyClass
            {
                private string name;

                public sealed string Name
                {
                    get
                    {
                        return name;
                    }
                    set
                    {
                        name = value;
                    }
                }
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task VirtualProperty()
        => TestInRegularAndScriptAsync("""
            class MyBaseClass
            {
                public virtual string N[||]ame { get; set; }
            }

            class MyDerivedClass : MyBaseClass
            {
                public override string Name {get; set;}
            }
            """, """
            class MyBaseClass
            {
                private string name;

                public virtual string Name
                {
                    get
                    {
                        return name;
                    }
                    set
                    {
                        name = value;
                    }
                }
            }

            class MyDerivedClass : MyBaseClass
            {
                public override string Name {get; set;}
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task PrivateProperty()
        => TestInRegularAndScriptAsync("""
            class MyClass
            {
                private string N[||]ame { get; set; }
            }
            """, """
            class MyClass
            {
                private string name;

                private string Name
                {
                    get
                    {
                        return name;
                    }
                    set
                    {
                        name = value;
                    }
                }
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task AbstractProperty()
        => TestMissingAsync("""
            class MyBaseClass
            {
                public abstract string N[||]ame { get; set; }
            }

            class MyDerivedClass : MyBaseClass
            {
                public override string Name {get; set;}
            }
            """);

    [Fact]
    public Task ExternProperty()
        => TestMissingAsync("""
            class MyBaseClass
            {
                extern string N[||]ame { get; set; }
            }
            """);

    [Fact]
    public Task GetterOnly()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get;}
            }
            """, """
            class TestClass
            {
                private readonly int goo;

                public int Goo
                {
                    get
                    {
                        return goo;
                    }
                }
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task GetterOnlyExpressionBodies()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get;}
            }
            """, """
            class TestClass
            {
                private readonly int goo;

                public int Goo => goo;
            }
            """, new(options: PreferExpressionBodiesOnAccessorsAndMethods));

    [Fact]
    public Task GetterOnlyExpressionBodies_Field()
        => TestInRegularAndScriptAsync(
            """
            class TestClass
            {
                public int G[||]oo { get;}
            }
            """, """
            class TestClass
            {
                public int Goo => field;
            }
            """, new(options: PreferExpressionBodiesOnAccessorsAndMethods, index: 1, parseOptions: CSharp14));

    [Fact]
    public Task SetterOnly()
        => TestMissingAsync("""
            class TestClass
            {
                public int G[||]oo
                {
                    set {}
                }
            }
            """);

    [Fact]
    public Task ExpressionBodiedAccessors()
        => TestMissingAsync("""
            class TestClass
            {
               private int testgoo;

               public int testg[||]oo {get => testgoo; set => testgoo = value; }
            }
            """);

    [Fact]
    public Task CursorAtBeginning()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                [||]public int Goo { get; set; }
            }
            """, """
            class TestClass
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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task CursorAtEnd()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int Goo[||] { get; set; }
            }
            """, """
            class TestClass
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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task CursorOnAccessors()
        => TestMissingAsync("""
            class TestClass
            {
                public int Goo { g[||]et; set; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task CursorInType()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public in[||]t Goo { get; set; }
            }
            """, """
            class TestClass
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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task SelectionWhole()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                [|public int Goo { get; set; }|]
            }
            """, """
            class TestClass
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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task SelectionName()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int [|Goo|] { get; set; }
            }
            """, """
            class TestClass
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
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact]
    public Task MoreThanOneGetter()
        => TestMissingAsync("""
            class TestClass
            {
                public int Goo { g[||]et; get; }
            }
            """);

    [Fact]
    public Task MoreThanOneSetter()
        => TestMissingAsync("""
            class TestClass
            {
                public int Goo { get; s[||]et; set; }
            }
            """);

    [Fact]
    public Task CustomFieldName()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get; set; }
            }
            """, """
            class TestClass
            {
                private int testingGoo;

                public int Goo
                {
                    get
                    {
                        return testingGoo;
                    }
                    set
                    {
                        testingGoo = value;
                    }
                }
            }
            """, new(options: UseCustomFieldName));

    [Fact, WorkItem(28013, "https://github.com/dotnet/roslyn/issues/26992")]
    public Task UnderscorePrefixedFieldName()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get; set; }
            }
            """, """
            class TestClass
            {
                private int _goo;

                public int Goo
                {
                    get
                    {
                        return _goo;
                    }
                    set
                    {
                        _goo = value;
                    }
                }
            }
            """, new(options: UseUnderscorePrefixedFieldName));

    [Fact, WorkItem(28013, "https://github.com/dotnet/roslyn/issues/26992")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30208")]
    public Task PropertyNameEqualsToClassNameExceptFirstCharCasingWhichCausesFieldNameCollisionByDefault()
        => TestInRegularAndScriptAsync("""
            class stranger
            {
                public int S[||]tranger { get; set; }
            }
            """, """
            class stranger
            {
                private int stranger1;

                public int Stranger { get => stranger1; set => stranger1 = value; }
            }
            """);

    [Fact]
    public Task NonStaticPropertyWithCustomStaticFieldName()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public int G[||]oo { get; set; }
            }
            """, """
            class TestClass
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
            """, new(options: UseCustomStaticFieldName));

    [Fact]
    public Task StaticPropertyWithCustomStaticFieldName()
        => TestInRegularAndScriptAsync("""
            class TestClass
            {
                public static int G[||]oo { get; set; }
            }
            """, """
            class TestClass
            {
                private static int staticfieldtestGoo;

                public static int Goo
                {
                    get
                    {
                        return staticfieldtestGoo;
                    }
                    set
                    {
                        staticfieldtestGoo = value;
                    }
                }
            }
            """, new(options: UseCustomStaticFieldName));

    [Fact]
    public Task InInterface()
        => TestMissingAsync("""
            interface IGoo
            {
                public int Goo { get; s[||]et; }
            }
            """);

    [Fact]
    public Task InStruct()
        => TestInRegularAndScriptAsync("""
            struct goo
            {
                public int G[||]oo { get; set; }
            }
            """, """
            struct goo
            {
                private int goo1;

                public int Goo
                {
                    get
                    {
                        return goo1;
                    }
                    set
                    {
                        goo1 = value;
                    }
                }
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22146")]
    public Task PartialClasses()
        => TestInRegularAndScriptAsync("""
            partial class Program
            {
                int P { get; set; }
            }

            partial class Program
            {
                int [||]Q { get; set; }
            }
            """, """
            partial class Program
            {
                int P { get; set; }
            }

            partial class Program
            {
                private int q;

                int Q { get => q; set => q = value; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22146")]
    public async Task PartialClassInSeparateFiles1()
    {
        var file1 = """
            partial class Program
            {
                int [||]P { get; set; }
            }
            """;
        var file2 = """
            partial class Program
            {
                int Q { get; set; }
            }
            """;
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true">
                    <Document FilePath="file1">{1}</Document>
                    <Document FilePath="file2">{2}</Document>
                </Project>
            </Workspace>
            """, LanguageNames.CSharp, file1, file2);

        using var testWorkspace = TestWorkspace.Create(xmlString);
        // refactor file1 and check
        var (_, action) = await GetCodeActionsAsync(testWorkspace);
        await TestActionAsync(
            testWorkspace,
            """
            partial class Program
            {
                private int p;

                int P { get => p; set => p = value; }
            }
            """,
            action,
            conflictSpans: [],
            renameSpans: [],
            warningSpans: [],
            navigationSpans: [],
            parameters: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22146")]
    public async Task PartialClassInSeparateFiles2()
    {
        var file1 = """
            partial class Program
            {
                int P { get; set; }
            }
            """;
        var file2 = """
            partial class Program
            {
                int Q[||] { get; set; }
            }
            """;
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true">
                    <Document FilePath="file1">{1}</Document>
                    <Document FilePath="file2">{2}</Document>
                </Project>
            </Workspace>
            """, LanguageNames.CSharp, file1, file2);

        using var testWorkspace = TestWorkspace.Create(xmlString);
        // refactor file2 and check
        var (_, action) = await GetCodeActionsAsync(testWorkspace);
        await TestActionAsync(
            testWorkspace,
            """
            partial class Program
            {
                private int q;

                int Q { get => q; set => q = value; }
            }
            """,
            action,
            conflictSpans: [],
            renameSpans: [],
            warningSpans: [],
            navigationSpans: [],
            parameters: null);
    }

    [Fact]
    public async Task InvalidLocation()
    {
        await TestMissingAsync("""
            namespace NS
            {
                public int G[||]oo { get; set; }
            }
            """);

        await TestMissingAsync("public int G[||]oo { get; set; }");
    }

    [Fact]
    public Task NullBackingField()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class Program
            {
                string? Name[||] { get; set; }
            }
            """,
            """
            #nullable enable

            class Program
            {
                private string? name;

                string? Name { get => name; set => name = value; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29021")]
    public Task ConstructorInitializerIndentation()
        => TestInRegularAndScriptAsync(
            """
            internal class EvaluationCommandLineHandler
            {
                public EvaluationCommandLineHandler(UnconfiguredProject project)
                    : base(project)
                {
                }

                public Dictionary<string, IImmutableDictionary<string, string>> [||]Files
                {
                    get;
                }
            }
            """,
            """
            internal class EvaluationCommandLineHandler
            {
                private readonly Dictionary<string, IImmutableDictionary<string, string>> files;

                public EvaluationCommandLineHandler(UnconfiguredProject project)
                    : base(project)
                {
                }

                public Dictionary<string, IImmutableDictionary<string, string>> Files => files;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75547")]
    public Task ConvertField1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                int [||]P
                {
                    get;
                    set
                    {
                        M(field);
                        field = value;
                    }
                }

                void M(int i) { }
            }
            """,
            """
            class Class
            {
                private int p;

                int [||]P
                {
                    get => p;
                    set
                    {
                        M(p);
                        p = value;
                    }
                }
            
                void M(int i) { }
            }
            """,
            new(parseOptions: CSharp14));

    [Theory]
    [InlineData("set"), InlineData("init")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/76899")]
    public Task ProduceFieldBackedProperty(string setter)
        => TestInRegularAndScriptAsync($$"""
            class TestClass
            {
                public int G[||]oo { get; {{setter}}; }
            }
            """, $$"""
            class TestClass
            {
                public int Goo
                {
                    get
                    {
                        return field;
                    }
                    {{setter}}
                    {
                        field = value;
                    }
                }
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors, index: 1, parseOptions: CSharp14));

    [Theory]
    [InlineData("set"), InlineData("init")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/76992")]
    public Task ProduceFieldBackedProperty2(string setter)
        => TestInRegularAndScriptAsync($$"""
            class TestClass
            {
                public int G[||]oo { get; {{setter}}; } = 0;
            }
            """, $$"""
            class TestClass
            {
                public int Goo
                {
                    get
                    {
                        return field;
                    }
                    {{setter}}
                    {
                        field = value;
                    }
                } = 0;
            }
            """, new(options: DoNotPreferExpressionBodiedAccessors, index: 1, parseOptions: CSharp14));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80771")]
    public Task ProduceFieldBackedProperty_WithRecord()
        => TestInRegularAndScriptAsync($$"""
            record C(int P)
            {
                public int [||]P { get; } = P;
            }
            """, $$"""
            record C(int P)
            {
                public int P { get => field; } = P;
            }
            """, new(index: 1, parseOptions: CSharp14));
}
