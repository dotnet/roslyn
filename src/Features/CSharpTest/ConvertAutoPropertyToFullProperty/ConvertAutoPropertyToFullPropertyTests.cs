// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAutoPropertyToFullProperty;

[Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
public sealed partial class ConvertAutoPropertyToFullPropertyTests : AbstractCSharpCodeActionTest_NoEditor
{
    private static readonly CSharpParseOptions CSharp14 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersionExtensions.CSharpNext);

    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider();

    [Theory]
    [InlineData("set"), InlineData("init")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/48133")]
    public async Task SimpleAutoPropertyTest(string setter)
    {
        await TestInRegularAndScriptAsync($$"""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task ExtraLineAfterProperty()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task WithInitialValue()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task WithCalculatedInitialValue()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task WithPrivateSetter()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task WithFieldNameAlreadyUsed()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task WithComments()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task WithExpressionBody()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: PreferExpressionBodiedAccessorsWhenPossible);
    }

    [Fact]
    public async Task WithExpressionBodyWhenOnSingleLine()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: PreferExpressionBodiedAccessorsWhenOnSingleLine);
    }

    [Fact]
    public async Task WithExpressionBodyWhenOnSingleLine2()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: PreferExpressionBodiedAccessorsWhenOnSingleLine);
    }

    [Fact]
    public async Task WithExpressionBodyWithTrivia()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: PreferExpressionBodiedAccessorsWhenPossible);
    }

    [Fact]
    public async Task WithPropertyOpenBraceOnSameLine()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessorsAndPropertyOpenBraceOnSameLine);
    }

    [Fact]
    public async Task WithAccessorOpenBraceOnSameLine()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessorsAndAccessorOpenBraceOnSameLine);
    }

    [Fact]
    public async Task StaticProperty()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task ProtectedProperty()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task InternalProperty()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task WithAttributes()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task CommentsInAccessors()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task OverrideProperty()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task SealedProperty()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task VirtualProperty()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task PrivateProperty()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task AbstractProperty()
    {
        await TestMissingAsync("""
            class MyBaseClass
            {
                public abstract string N[||]ame { get; set; }
            }

            class MyDerivedClass : MyBaseClass
            {
                public override string Name {get; set;}
            }
            """);
    }

    [Fact]
    public async Task ExternProperty()
    {
        await TestMissingAsync("""
            class MyBaseClass
            {
                extern string N[||]ame { get; set; }
            }
            """);
    }

    [Fact]
    public async Task GetterOnly()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task GetterOnlyExpressionBodies()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: PreferExpressionBodiesOnAccessorsAndMethods);
    }

    [Fact]
    public async Task GetterOnlyExpressionBodies_Field()
    {
        await TestInRegularAndScriptAsync(
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
            """, options: PreferExpressionBodiesOnAccessorsAndMethods, index: 1, parseOptions: CSharp14);
    }

    [Fact]
    public async Task SetterOnly()
    {
        await TestMissingAsync("""
            class TestClass
            {
                public int G[||]oo
                {
                    set {}
                }
            }
            """);
    }

    [Fact]
    public async Task ExpressionBodiedAccessors()
    {
        await TestMissingAsync("""
            class TestClass
            {
               private int testgoo;

               public int testg[||]oo {get => testgoo; set => testgoo = value; }
            }
            """);
    }

    [Fact]
    public async Task CursorAtBeginning()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task CursorAtEnd()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task CursorOnAccessors()
    {
        await TestMissingAsync("""
            class TestClass
            {
                public int Goo { g[||]et; set; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task CursorInType()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task SelectionWhole()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task SelectionName()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact]
    public async Task MoreThanOneGetter()
    {
        await TestMissingAsync("""
            class TestClass
            {
                public int Goo { g[||]et; get; }
            }
            """);
    }

    [Fact]
    public async Task MoreThanOneSetter()
    {
        await TestMissingAsync("""
            class TestClass
            {
                public int Goo { get; s[||]et; set; }
            }
            """);
    }

    [Fact]
    public async Task CustomFieldName()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: UseCustomFieldName);
    }

    [Fact, WorkItem(28013, "https://github.com/dotnet/roslyn/issues/26992")]
    public async Task UnderscorePrefixedFieldName()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: UseUnderscorePrefixedFieldName);
    }

    [Fact, WorkItem(28013, "https://github.com/dotnet/roslyn/issues/26992")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30208")]
    public async Task PropertyNameEqualsToClassNameExceptFirstCharCasingWhichCausesFieldNameCollisionByDefault()
    {
        await TestInRegularAndScriptAsync("""
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
    }

    [Fact]
    public async Task NonStaticPropertyWithCustomStaticFieldName()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: UseCustomStaticFieldName);
    }

    [Fact]
    public async Task StaticPropertyWithCustomStaticFieldName()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: UseCustomStaticFieldName);
    }

    [Fact]
    public async Task InInterface()
    {
        await TestMissingAsync("""
            interface IGoo
            {
                public int Goo { get; s[||]et; }
            }
            """);
    }

    [Fact]
    public async Task InStruct()
    {
        await TestInRegularAndScriptAsync("""
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
            """, options: DoNotPreferExpressionBodiedAccessors);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22146")]
    public async Task PartialClasses()
    {
        await TestInRegularAndScriptAsync("""
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
    }

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
    public async Task NullBackingField()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29021")]
    public async Task ConstructorInitializerIndentation()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75547")]
    public async Task ConvertField1()
    {
        await TestInRegularAndScriptAsync(
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
            parseOptions: CSharp14);
    }

    [Theory]
    [InlineData("set"), InlineData("init")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/76899")]
    public async Task ProduceFieldBackedProperty(string setter)
    {
        await TestInRegularAndScriptAsync($$"""
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
            """, options: DoNotPreferExpressionBodiedAccessors, index: 1, parseOptions: CSharp14);
    }

    [Theory]
    [InlineData("set"), InlineData("init")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/76992")]
    public async Task ProduceFieldBackedProperty2(string setter)
    {
        await TestInRegularAndScriptAsync($$"""
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
            """, options: DoNotPreferExpressionBodiedAccessors, index: 1, parseOptions: CSharp14);
    }
}
