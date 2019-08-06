// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAutoPropertyToFullProperty
{
    public partial class ConvertAutoPropertyToFullPropertyTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task SimpleAutoPropertyTest()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task ExtraLineAfterProperty()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get; set; }

}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithInitialValue()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get; set; } = 2
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithCalculatedInitialValue()
        {
            var text = @"
class TestClass
{
    const int num = 345;
    public int G[||]oo { get; set; } = 2*num
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithPrivateSetter()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get; private set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithFieldNameAlreadyUsed()
        {
            var text = @"
class TestClass
{
    private int goo;

    public int G[||]oo { get; private set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithComments()
        {
            var text = @"
class TestClass
{
    // Comments before
    public int G[||]oo { get; private set; } //Comments during
    //Comments after
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithExpressionBody()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get; set; }
}
";
            var expected = @"
class TestClass
{
    private int goo;

    public int Goo { get => goo; set => goo = value; }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: PreferExpressionBodiedAccessorsWhenPossible);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithExpressionBodyWhenOnSingleLine()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get; set; }
}
";
            var expected = @"
class TestClass
{
    private int goo;

    public int Goo { get => goo; set => goo = value; }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: PreferExpressionBodiedAccessorsWhenOnSingleLine);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithExpressionBodyWhenOnSingleLine2()
        {
            var text = @"
class TestClass
{
    public int G[||]oo
    {
        get;
        set;
    }
}
";
            var expected = @"
class TestClass
{
    private int goo;

    public int Goo
    {
        get => goo;
        set => goo = value;
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: PreferExpressionBodiedAccessorsWhenOnSingleLine);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithExpressionBodyWithTrivia()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get /* test */ ; set /* test2 */ ; }
}
";
            var expected = @"
class TestClass
{
    private int goo;

    public int Goo { get /* test */ => goo; set /* test2 */ => goo = value; }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: PreferExpressionBodiedAccessorsWhenPossible);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithPropertyOpenBraceOnSameLine()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessorsAndPropertyOpenBraceOnSameLine);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithAccessorOpenBraceOnSameLine()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessorsAndAccessorOpenBraceOnSameLine);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task StaticProperty()
        {
            var text = @"
class TestClass
{
    public static int G[||]oo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task ProtectedProperty()
        {
            var text = @"
class TestClass
{
    protected int G[||]oo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task InternalProperty()
        {
            var text = @"
class TestClass
{
    internal int G[||]oo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithAttributes()
        {
            var text = @"
class TestClass
{
    [A]
    public int G[||]oo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task CommentsInAccessors()
        {
            var text = @"
class TestClass
{
    /// <summary>
    /// test stuff here
    /// </summary>
    public int Testg[||]oo { /* test1 */ get /* test2 */; /* test3 */ set /* test4 */; /* test5 */ } /* test6 */
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task OverrideProperty()
        {
            var text = @"
class MyBaseClass
{
    public virtual string Name { get; set; }
}

class MyDerivedClass : MyBaseClass
{
    public override string N[||]ame {get; set;}
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task SealedProperty()
        {
            var text = @"
class MyClass
{
    public sealed string N[||]ame {get; set;}
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task VirtualProperty()
        {
            var text = @"
class MyBaseClass
{
    public virtual string N[||]ame { get; set; }
}

class MyDerivedClass : MyBaseClass
{
    public override string Name {get; set;}
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task PrivateProperty()
        {
            var text = @"
class MyClass
{
    private string N[||]ame { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task AbstractProperty()
        {
            var text = @"
class MyBaseClass
{
    public abstract string N[||]ame { get; set; }
}

class MyDerivedClass : MyBaseClass
{
    public override string Name {get; set;}
}
";
            await TestMissingAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task ExternProperty()
        {
            var text = @"
class MyBaseClass
{
    extern string N[||]ame { get; set; }
}
";
            await TestMissingAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task GetterOnly()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get;}
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task GetterOnlyExpressionBodies()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get;}
}
";
            var expected = @"
class TestClass
{
    private readonly int goo;

    public int Goo => goo;
}
";
            await TestInRegularAndScriptAsync(text, expected, options: PreferExpressionBodiesOnAccessorsAndMethods);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task SetterOnly()
        {
            var text = @"
class TestClass
{
    public int G[||]oo
    {
        set {}
    }
}
";
            await TestMissingAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task ExpressionBodiedAccessors()
        {
            var text = @"
class TestClass
{
   private int testgoo;

   public int testg[||]oo {get => testgoo; set => testgoo = value; }
}
";
            await TestMissingAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task CursorAtBeginning()
        {
            var text = @"
class TestClass
{
    [||]public int Goo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task CursorAtEnd()
        {
            var text = @"
class TestClass
{
    public int Goo[||] { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task CursorOnAccessors()
        {
            var text = @"
class TestClass
{
    public int Goo { g[||]et; set; }
}
";
            await TestMissingAsync(text);
        }

        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task CursorInType()
        {
            var text = @"
class TestClass
{
    public in[||]t Goo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task SelectionWhole()
        {
            var text = @"
class TestClass
{
    [|public int Goo { get; set; }|]
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task SelectionName()
        {
            var text = @"
class TestClass
{
    public int [|Goo|] { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task MoreThanOneGetter()
        {
            var text = @"
class TestClass
{
    public int Goo { g[||]et; get; }
}
";
            await TestMissingAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task MoreThanOneSetter()
        {
            var text = @"
class TestClass
{
    public int Goo { get; s[||]et; set; }
}
";
            await TestMissingAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task CustomFieldName()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: UseCustomFieldName);
        }

        [WorkItem(28013, "https://github.com/dotnet/roslyn/issues/26992")]
        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task UnderscorePrefixedFieldName()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: UseUnderscorePrefixedFieldName);
        }

        [WorkItem(28013, "https://github.com/dotnet/roslyn/issues/26992")]
        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task PropertyNameEqualsToClassNameExceptFirstCharCasingWhichCausesFieldNameCollisionByDefault()
        {
            var text = @"
class stranger
{
    public int S[||]tranger { get; set; }
}
";
            var expected = @"
class stranger
{
    private int stranger;

    public int Stranger { get => stranger; set => stranger = value; }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task NonStaticPropertyWithCustomStaticFieldName()
        {
            var text = @"
class TestClass
{
    public int G[||]oo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: UseCustomStaticFieldName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task StaticPropertyWithCustomStaticFieldName()
        {
            var text = @"
class TestClass
{
    public static int G[||]oo { get; set; }
}
";
            var expected = @"
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
";
            await TestInRegularAndScriptAsync(text, expected, options: UseCustomStaticFieldName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task InInterface()
        {
            var text = @"
interface IGoo
{
    public int Goo { get; s[||]et; }
}
";
            await TestMissingAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task InStruct()
        {
            var text = @"
struct goo
{
    public int G[||]oo { get; set; }
}
";
            var expected = @"
struct goo
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
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors);
        }

        [WorkItem(22146, "https://github.com/dotnet/roslyn/issues/22146")]
        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task PartialClasses()
        {
            var text = @"
partial class Program
{
    int P { get; set; }
}

partial class Program
{
    int [||]Q { get; set; }
}
";
            var expected = @"
partial class Program
{
    int P { get; set; }
}

partial class Program
{
    private int q;

    int Q { get => q; set => q = value; }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [WorkItem(22146, "https://github.com/dotnet/roslyn/issues/22146")]
        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task PartialClassInSeparateFiles1()
        {
            var file1 = @"
partial class Program
{
    int [||]P { get; set; }
}";
            var file2 = @"
partial class Program
{
    int Q { get; set; }
}";
            var file1AfterRefactor = @"
partial class Program
{
    private int p;

    int P { get => p; set => p = value; }
}";

            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""file1"">{1}</Document>
        <Document FilePath=""file2"">{2}</Document>
    </Project>
</Workspace>", LanguageNames.CSharp, file1, file2);

            using var testWorkspace = TestWorkspace.Create(xmlString);
            // refactor file1 and check
            var (_, action) = await GetCodeActionsAsync(testWorkspace, parameters: default);
            await TestActionAsync(
                testWorkspace,
                file1AfterRefactor,
                action,
                conflictSpans: ImmutableArray<TextSpan>.Empty,
                renameSpans: ImmutableArray<TextSpan>.Empty,
                warningSpans: ImmutableArray<TextSpan>.Empty,
                navigationSpans: ImmutableArray<TextSpan>.Empty,
                parameters: default);
        }

        [WorkItem(22146, "https://github.com/dotnet/roslyn/issues/22146")]
        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task PartialClassInSeparateFiles2()
        {
            var file1 = @"
partial class Program
{
    int P { get; set; }
}";
            var file2 = @"
partial class Program
{
    int Q[||] { get; set; }
}";
            var file2AfterRefactor = @"
partial class Program
{
    private int q;

    int Q { get => q; set => q = value; }
}";

            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""file1"">{1}</Document>
        <Document FilePath=""file2"">{2}</Document>
    </Project>
</Workspace>", LanguageNames.CSharp, file1, file2);

            using var testWorkspace = TestWorkspace.Create(xmlString);
            // refactor file2 and check
            var (_, action) = await GetCodeActionsAsync(testWorkspace, parameters: default);
            await TestActionAsync(
                testWorkspace,
                file2AfterRefactor,
                action,
                conflictSpans: ImmutableArray<TextSpan>.Empty,
                renameSpans: ImmutableArray<TextSpan>.Empty,
                warningSpans: ImmutableArray<TextSpan>.Empty,
                navigationSpans: ImmutableArray<TextSpan>.Empty,
                parameters: default);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task InvalidLocation()
        {
            await TestMissingAsync(@"namespace NS
{
    public int G[||]oo { get; set; }
}");

            await TestMissingAsync("public int G[||]oo { get; set; }");
        }
    }
}
