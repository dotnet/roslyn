// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.IntroduceVariable
{
    public class IntroduceVariableTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new IntroduceVariableCodeRefactoringProvider();

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => GetNestedActions(actions);

        private readonly CodeStyleOption<bool> onWithInfo = new CodeStyleOption<bool>(true, NotificationOption.Suggestion);

        // specify all options explicitly to override defaults.
        private IDictionary<OptionKey, object> ImplicitTypingEverywhere() =>
            OptionsSet(
                SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithInfo),
                SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo),
                SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo));

        internal IDictionary<OptionKey, object> OptionSet(OptionKey option, object value)
        {
            var options = new Dictionary<OptionKey, object>();
            options.Add(option, value);
            return options;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestEmptySpan1()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class C
{
    void M(Action action)
    {
        M(() [||]=> { });
    }
}",
@"using System;
class C
{
    void M(Action action)
    {
        Action {|Rename:action1|} = () => { };
        M(action1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestEmptySpan2()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class C
{
    void M(int a, int b)
    {
        var x = a [||]+ b + 3;
    }
}",
@"using System;
class C
{
    void M(int a, int b)
    {
        int {|Rename:v|} = a + b;
        var x = v + 3;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestEmptySpan3()
        {
            await TestMissingAsync(
@"using System;
class C
{
    void M(int a)
    {
        var x = [||]a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestEmptySpan4()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class C
{
    void M(Action action)
    {
        M(() => { var x [||]= y; });
    }
}",
@"using System;
class C
{
    void M(Action action)
    {
        Action {|Rename:p|} = () => { var x[||] = y; };
        M(p);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFix1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        Bar([|1 + 1|]);
        Bar(1 + 1);
    }
}",
@"class C
{
    void Goo()
    {
        const int {|Rename:V|} = 1 + 1;
        Bar(V);
        Bar(1 + 1);
    }
}",
                index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFix2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        Bar([|1 + 1|]);
        Bar(1 + 1);
    }
}",
@"class C
{
    void Goo()
    {
        const int {|Rename:V|} = 1 + 1;
        Bar(V);
        Bar(V);
    }
}",
                index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFix3()
        {
            var code =
@"class C
{
    void Goo()
    {
        Bar(([|1 + 1|]));
        Bar((1 + 1));
    }
}";

            var expected =
@"class C
{
    void Goo()
    {
        const int {|Rename:V|} = 1 + 1;
        Bar(V);
        Bar((1 + 1));
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFix4()
        {
            var code =
@"class C
{
    void Goo()
    {
        Bar(([|1 + 1|]));
        Bar((1 + 1));
    }
}";

            var expected =
@"class C
{
    void Goo()
    {
        const int {|Rename:V|} = 1 + 1;
        Bar(V);
        Bar(V);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestThrowExpression()
        {
            var code =
@"class C
{
    void M(bool b)
    {
        var x = b ? 1 : [|throw null|];
    }
}";
            await TestActionCountAsync(code, count: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestThrowExpression2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool b)
    {
        var x = [|b ? 1 : throw null|];
    }
}",
@"class C
{
    void M(bool b)
    {
        int {|Rename:v|} = b ? 1 : throw null;
        var x = v;
    }
}",
                index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestThrowStatement()
        {
            var code =
@"class C
{
    void M()
    {
        [|throw null|];
    }
}";
            await TestActionCountAsync(code, count: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFieldFix1()
        {
            var code =
@"class C
{
    int i = ([|1 + 1|]) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V|} = 1 + 1;
    int i = V + (1 + 1);
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFieldFix2()
        {
            var code =
@"class C
{
    int i = ([|1 + 1|]) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V|} = 1 + 1;
    int i = V + V;
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(21747, "https://github.com/dotnet/roslyn/issues/21747")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestTriviaFieldFix1()
        {
            var code =
@"class C
{
    int i = (/* CommentLeading */ [|1 + 1|] /* CommentTrailing */) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V|} = 1 + 1;
    int i = /* CommentLeading */ V /* CommentTrailing */ + V;
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(21747, "https://github.com/dotnet/roslyn/issues/21747")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestTriviaFieldFix2()
        {
            var code =
@"class C
{
    int i = (/* CommentLeading */ [|1 + /*CommentEmbedded*/ 1|] /* CommentTrailing */) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V|} = 1 + /*CommentEmbedded*/ 1;
    int i = /* CommentLeading */ V /* CommentTrailing */ + V;
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstFieldFix1()
        {
            var code =
@"class C
{
    const int i = ([|1 + 1|]) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V|} = 1 + 1;
    const int i = V + (1 + 1);
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstFieldFix2()
        {
            var code =
@"class C
{
    const int i = ([|1 + 1|]) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V|} = 1 + 1;
    const int i = V + V;
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstructorFix1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C() : this([|1 + 1|], 1 + 1)
    {
    }
}",
@"class C
{
    private const int {|Rename:V|} = 1 + 1;

    public C() : this(V, 1 + 1)
    {
    }
}",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstructorFix2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C() : this([|1 + 1|], 1 + 1)
    {
    }
}",
@"class C
{
    private const int {|Rename:V|} = 1 + 1;

    public C() : this(V, V)
    {
    }
}",
                index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestParameterFix1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Bar(int i = [|1 + 1|], int j = 1 + 1)
    {
    }
}",
@"class C
{
    private const int {|Rename:V|} = 1 + 1;

    void Bar(int i = V, int j = 1 + 1)
    {
    }
}",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestParameterFix2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Bar(int i = [|1 + 1|], int j = 1 + 1)
    {
    }
}",
@"class C
{
    private const int {|Rename:V|} = 1 + 1;

    void Bar(int i = V, int j = V)
    {
    }
}",
                index: 1);
        }

        [Fact]
        public async Task TestAttributeFix1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    [Goo([|1 + 1|], 1 + 1)]
    void Bar()
    {
    }
}",
@"class C
{
    private const int {|Rename:V|} = 1 + 1;

    [Goo(V, 1 + 1)]
    void Bar()
    {
    }
}",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestAttributeFix2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    [Goo([|1 + 1|], 1 + 1)]
    void Bar()
    {
    }
}",
@"class C
{
    private const int {|Rename:V|} = 1 + 1;

    [Goo(V, V)]
    void Bar()
    {
    }
}",
                index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFixExistingName1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        int V = 0;
        Bar([|1 + 1|]);
        Bar(1 + 1);
    }
}",
@"class C
{
    void Goo()
    {
        int V = 0;
        const int {|Rename:V1|} = 1 + 1;
        Bar(V1);
        Bar(1 + 1);
    }
}",
                index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFieldExistingName1()
        {
            var code =
@"class C
{
    int V;
    int V1;
    int i = ([|1 + 1|]) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V2|} = 1 + 1;
    int V;
    int V1;
    int i = V2 + (1 + 1);
}";

            await TestInRegularAndScriptAsync(
                code,
                expected,
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFixComplexName1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static int Baz;

    void Goo()
    {
        Bar([|C.Baz|]);
        Bar(1 + 1);
    }
}",
@"class C
{
    static int Baz;

    void Goo()
    {
        var {|Rename:baz|} = C.Baz;
        Bar(baz);
        Bar(1 + 1);
    }
}",
                index: 0,
                options: ImplicitTypingEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFixComplexName1NotVar()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static int Baz;

    void Goo()
    {
        Bar([|C.Baz|]);
        Bar(1 + 1);
    }
}",
@"class C
{
    static int Baz;

    void Goo()
    {
        int {|Rename:baz|} = C.Baz;
        Bar(baz);
        Bar(1 + 1);
    }
}",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameConflict1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C(int V) : this([|1 + 1|])
    {
    }
}",
@"class C
{
    private const int {|Rename:V|} = 1 + 1;

    public C(int V) : this(C.V)
    {
    }
}",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameConflict2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    private static int v = 5;

    static void Main(string[] args)
    {
        Func<int, int> d = (x) => {
            return [|x * v|];
        };
        d.Invoke(v);
    }
}",
@"using System;

class Program
{
    private static int v = 5;

    static void Main(string[] args)
    {
        Func<int, int> d = (x) => {
            var {|Rename:v1|} = x * v;
            return v1;
        };
        d.Invoke(v);
    }
}",
options: ImplicitTypingEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameConflict2NotVar()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    private static int v = 5;

    static void Main(string[] args)
    {
        Func<int, int> d = (x) => {
            return [|x * v|];
        };
        d.Invoke(v);
    }
}",
@"using System;

class Program
{
    private static int v = 5;

    static void Main(string[] args)
    {
        Func<int, int> d = (x) => {
            int {|Rename:v1|} = x * v;
            return v1;
        };
        d.Invoke(v);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameVerbatimIdentifier1()
        {
            await TestInRegularAndScriptAsync(
@"static class G<T>
{
    public class @class
    {
    }

    public static void Add(object @class)
    {
    }
}

class Program
{
    static void Main()
    {
        G<int>.Add([|new G<int>.@class()|]);
    }
}",
@"static class G<T>
{
    public class @class
    {
    }

    public static void Add(object @class)
    {
    }
}

class Program
{
    static void Main()
    {
        var {|Rename:@class|} = new G<int>.@class();
        G<int>.Add(@class);
    }
}",
options: ImplicitTypingEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameVerbatimIdentifier1NoVar()
        {
            await TestInRegularAndScriptAsync(
@"static class G<T>
{
    public class @class
    {
    }

    public static void Add(object @class)
    {
    }
}

class Program
{
    static void Main()
    {
        G<int>.Add([|new G<int>.@class()|]);
    }
}",
@"static class G<T>
{
    public class @class
    {
    }

    public static void Add(object @class)
    {
    }
}

class Program
{
    static void Main()
    {
        G<int>.@class {|Rename:@class|} = new G<int>.@class();
        G<int>.Add(@class);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameVerbatimIdentifier2()
        {
            await TestInRegularAndScriptAsync(
@"static class G<T>
{
    public class @class
    {
    }

    public static void Add(object @class)
    {
    }

    static void Main()
    {
        G<int>.Add([|new G<int>.@class()|]);
    }
}",
@"static class G<T>
{
    public class @class
    {
    }

    public static void Add(object @class)
    {
    }

    static void Main()
    {
        var {|Rename:@class|} = new G<int>.@class();
        G<int>.Add(@class);
    }
}",
options: ImplicitTypingEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameVerbatimIdentifier2NoVar()
        {
            await TestInRegularAndScriptAsync(
@"static class G<T>
{
    public class @class
    {
    }

    public static void Add(object @class)
    {
    }

    static void Main()
    {
        G<int>.Add([|new G<int>.@class()|]);
    }
}",
@"static class G<T>
{
    public class @class
    {
    }

    public static void Add(object @class)
    {
    }

    static void Main()
    {
        G<int>.@class {|Rename:@class|} = new G<int>.@class();
        G<int>.Add(@class);
    }
}");
        }

        [WorkItem(540078, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540078")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstantField1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int[] array = new int[[|10|]];
}",
@"class C
{
    private const int {|Rename:V|} = 10;
    int[] array = new int[V];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(540079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540079")]
        public async Task TestFormattingOfReplacedExpression1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int i = [|1 + 2|] + 3;
    }
}",
@"class C
{
    void M()
    {
        const int {|Rename:V|} = 1 + 2;
        int i = V + 3;
    }
}",
index: 2);
        }

        [WorkItem(540468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestCantExtractMethodTypeParameterToField()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main<T>(string[] args)
    {
        Goo([|(T)2.ToString()|]);
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main<T>(string[] args)
    {
        var {|Rename:t|} = (T)2.ToString();
        Goo(t);
    }
}",
options: ImplicitTypingEverywhere());
        }

        [WorkItem(540468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestCantExtractMethodTypeParameterToFieldCount()
        {
            await TestActionCountAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main<T>(string[] args)
    {
        Goo([|(T)2.ToString()|]);
    }
}",
count: 2);
        }

        [WorkItem(552389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552389")]
        [WorkItem(540482, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540482")]
        [WpfFact(Skip = "552389"), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstantForFixedBufferInitializer()
        {
            await TestInRegularAndScriptAsync(
@"unsafe struct S
{
    fixed int buffer[[|10|]];
}",
@"unsafe struct S
{
    private const int p = 10;
    fixed int buffer[p];
}");
        }

        [WorkItem(540486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540486")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFormattingOfIntroduceLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int i = [|1 + 2|] + 3;
    }
}",
@"class C
{
    void M()
    {
        const int {|Rename:V|} = 1 + 2;
        int i = V + 3;
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLocalConstant()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        const int i = [|1|] + 1;
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        const int {|Rename:V|} = 1;
        const int i = V + 1;
    }
}",
index: 2);
        }

        [WorkItem(542699, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542699")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFieldConstant()
        {
            await TestInRegularAndScriptAsync(
@"[Goo(2 + 3 + 4)]
class Program
{
    int x = [|2 + 3|] + 4;
}

internal class GooAttribute : System.Attribute
{
    public GooAttribute(int x)
    {
    }
}",
@"[Goo(V + 4)]
class Program
{
    private const int {|Rename:V|} = 2 + 3;
    int x = V + 4;
}

internal class GooAttribute : System.Attribute
{
    public GooAttribute(int x)
    {
    }
}",
index: 1);
        }

        [WorkItem(542781, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542781")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnExpressionStatement()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        int i;
        [|i = 2|];
        i = 3;
    }
}");
        }

        [WorkItem(542780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542780")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSingleQueryClause()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    select [|i + j|];
    }
}",
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    let {|Rename:v|} = i + j
                    select v;
    }
}");
        }

        [WorkItem(542780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542780")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSingleQuerySelectOrGroupByClause()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    where [|i + j|] > 5
                    select i + j;
    }
}",
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    let {|Rename:v|} = i + j
                    where v > 5
                    select i + j;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLinqQuery()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    where [|i + j|] > 5
                    let x = j + i
                    select i + j;
    }
}",
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    let {|Rename:v|} = i + j
                    where v > 5
                    let x = j + i
                    select v;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSingleQueryReplaceAll()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    where i + j > 5
                    let x = j + i
                    select [|i + j|];
    }
}",
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    let {|Rename:v|} = i + j
                    where v > 5
                    let x = j + i
                    select v;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNestedQueryReplaceOne1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    where i > (from k in new int[] { 3 }

                               select [|i + j|]).Max()
                    where j > (from m in new int[] { 4 }

                               select i + j).Max()
                    let x = j + i
                    select i + j;
    }
}",
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    where i > (from k in new int[] { 3 }

                               let {|Rename:v|} = i + j
                               select v).Max()
                    where j > (from m in new int[] { 4 }

                               select i + j).Max()
                    let x = j + i
                    select i + j;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNestedQueryReplaceAll1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    where i > (from k in new int[] { 3 }

                               select [|i + j|]).Max()
                    where j > (from m in new int[] { 4 }

                               select i + j).Max()
                    let x = j + i
                    select i + j;
    }
}",
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    let {|Rename:v|} = i + j
                    where i > (from k in new int[] { 3 }

                               select v).Max()
                    where j > (from m in new int[] { 4 }

                               select v).Max()
                    let x = j + i
                    select v;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNestedQueryReplaceOne2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    where i > (from k in new int[] { 3 }

                               select i + j).Max()
                    where j > (from m in new int[] { 4 }

                               select [|i + j|]).Max()
                    let x = j + i
                    select i + j;
    }
}",
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    where i > (from k in new int[] { 3 }

                               select i + j).Max()
                    where j > (from m in new int[] { 4 }

                               let {|Rename:v|} = i + j
                               select v).Max()
                    let x = j + i
                    select i + j;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNestedQueryReplaceAll2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    where i > (from k in new int[] { 3 }

                               select i + j).Max()
                    where j > (from m in new int[] { 4 }

                               select [|i + j|]).Max()
                    let x = j + i
                    select i + j;
    }
}",
@"using System.Linq;

class Program
{
    void Main()
    {
        var query = from i in new int[] { 1 }

                    from j in new int[] { 2 }

                    let {|Rename:v|} = i + j
                    where i > (from k in new int[] { 3 }

                               select v).Max()
                    where j > (from m in new int[] { 4 }

                               select v).Max()
                    let x = j + i
                    select v;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10742, "DevDiv_Projects/Roslyn")]
        public async Task TestAnonymousTypeMemberAssignment()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var a = new { [|A = 0|] };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10743, "DevDiv_Projects/Roslyn")]
        public async Task TestAnonymousTypeBody()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        var a = new [|{ A = 0 }|];
    }
}",
@"class Program
{
    void Main()
    {
        var {|Rename:p|} = new { A = 0 };
        var a = p;
    }
}");
        }

        [WorkItem(543477, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543477")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestImplicitlyTypedArraysUsedInCheckedExpression()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        int[] a = null;
        int[] temp = checked([|a = new[] { 1, 2, 3 }|]);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        int[] a = null;
        var {|Rename:v|} = a = new[] { 1, 2, 3 };
        int[] temp = checked(v);
    }
}",
options: ImplicitTypingEverywhere());
        }

        [WorkItem(543832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543832")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnGenericTypeParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        F<[|int?|], int?>(3);
    }

    R F<T, R>(T arg1)
    {
        return default(R);
    }
}");
        }

        [WorkItem(543941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543941")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestAnonymousType1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        WriteLine([|new { X = 1 }|]);
    }
}",
@"class Program
{
    void Main()
    {
        var {|Rename:p|} = new { X = 1 };
        WriteLine(p);
    }
}");
        }

        [WorkItem(544099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544099")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnAttributeNameEquals()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Runtime.InteropServices;

class M
{
    [DllImport(""user32.dll"", [|CharSet|] = CharSet.Auto)]
    public static extern IntPtr FindWindow(string className, string windowTitle);
}");
        }

        [WorkItem(544162, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544162")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnRightOfDot()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Runtime.InteropServices;

class M
{
    [DllImport(""user32.dll"", CharSet = CharSet.[|Auto|])]
    public static extern IntPtr FindWindow(string className, string windowTitle);
}");
        }

        [WorkItem(544209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544209")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnAttributeNamedParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class TestAttribute : Attribute
{
    public TestAttribute(int a = 42)
    {
    }
}

[Test([|a|]: 1)]
class Goo
{
}");
        }

        [WorkItem(544264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544264")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnVariableWrite()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        var x = new int[3];
        [|x[1]|] = 2;
    }
}");
        }

        [WorkItem(544577, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544577")]
        [WorkItem(909152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/909152")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestExpressionTLambda()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Linq.Expressions;

class Program
{
    static Expression<Func<int?, char?>> e1 = c => [|null|];
}");
        }

        [WorkItem(544915, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544915")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnTypeSyntax()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Main()
    {
        int[,] array2Da = new [|int[1, 2]|] { { 1, 2 } };
    }
}");
        }

        [WorkItem(544610, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544610")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task ParenthesizeIfParseChanges()
        {
            var code = @"
class C
{
    static void M()
    {
        int x = 2;
        Bar(x < [|1|], x > (2 + 3));
    }
}";

            var expected = @"
class C
{
    static void M()
    {
        int x = 2;
        const int {|Rename:V|} = 1;
        Bar(x < V, (x > (2 + 3)));
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInPartiallyHiddenMethod()
        {
            await TestMissingAsync(
@"class Program
{
#line hidden
    void Main()
    {
#line default
        Goo([|1 + 1|]);
    }
}", new TestParameters(Options.Regular));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInVisibleMethod()
        {
            await TestInRegularAndScriptAsync(
@"#line hidden
class Program
{
#line default
    void Main()
    {
        Goo([|1 + 1|]);
    }
#line hidden
}
#line default",
@"#line hidden
class Program
{
#line default
    void Main()
    {
        const int {|Rename:V|} = 1 + 1;
        Goo(V);
    }
#line hidden
}
#line default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInFieldInPartiallyHiddenType()
        {
            await TestMissingAsync(
@"class Program
{
    int i = [|1 + 1|];

#line hidden
}
#line default", new TestParameters(Options.Regular));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInAttributeInPartiallyHiddenType()
        {
            await TestMissingAsync(
@"[Goo([|1 + 1|])]
class Program
{
#line hidden
}
#line default", new TestParameters(Options.Regular));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInConstructorInitializerInPartiallyHiddenType()
        {
            await TestMissingAsync(
@"class Program
{
    public Program() : this([|1 + 1|])
    {
    }

#line hidden
}
#line default", new TestParameters(Options.Regular));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInParameterInPartiallyHiddenType()
        {
            await TestMissingAsync(
@"class Program
{
    public Program(int i = [|1 + 1|])
    {
    }

#line hidden
}
#line default", new TestParameters(Options.Regular));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInQueryInPartiallyHiddenType()
        {
            await TestMissingAsync(
@"using System.Linq;

class Program
{
    public Program(string[] args)
    {
        var q = from x in args
#line hidden
                let z = 1
#line default
                select [|x + x|];
    }
}", new TestParameters(Options.Regular));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInVisibleQueryInHiddenType()
        {
            await TestAsync(
@"#line hidden
using System.Linq;

class Program
{
    public Program(string[] args)
    {
        var q =
#line default
            from x in args
            let z = 1
            select [|x + x|];
#line hidden
    }
}
#line default",
@"#line hidden
using System.Linq;

class Program
{
    public Program(string[] args)
    {
        var q =
#line default
            from x in args
            let z = 1
            let {|Rename:v|} = x + x
            select v;
#line hidden
    }
}
#line default",
parseOptions: Options.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnNamespace()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|System|].Console.WriteLine(4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|System.Console|].WriteLine(4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnBase()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|base|].ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestVenusGeneration1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
#line 1 ""goo""
        Console.WriteLine([|5|]);
#line default
#line hidden
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestVenusGeneration2()
        {
            var code =
@"
class Program
{
    void Main ( )
    {
#line 1 ""goo""
        if (true)
        {
            Console.WriteLine([|5|]);
        }
#line default
#line hidden
    }
}";

            await TestExactActionSetOfferedAsync(code, new[] { string.Format(FeaturesResources.Introduce_local_constant_for_0, "5") });

            await TestInRegularAndScriptAsync(code,
@"
class Program
{
    void Main ( )
    {
#line 1 ""goo""
        if (true)
        {
            const int {|Rename:V|} = 5;
            Console.WriteLine(V);
        }
#line default
#line hidden
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestVenusGeneration3()
        {
            var code =
@"
class Program
{
#line 1 ""goo""
    void Main ( )
    {
        if (true)
        {
            Console.WriteLine([|5|]);
        }
    }
#line default
#line hidden
}";

            await TestExactActionSetOfferedAsync(code,
                new[] { string.Format(FeaturesResources.Introduce_local_constant_for_0, "5"), string.Format(FeaturesResources.Introduce_local_constant_for_all_occurrences_of_0, "5") });
        }

        [WorkItem(529795, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529795")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnNegatedLiteral()
        {
            await TestMissingInRegularAndScriptAsync(
@"class A
{
    void Main()
    {
        long x = -[|9223372036854775808|];
    }
}");
        }

        [WorkItem(546091, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546091")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNotOnInterfaceAttribute()
        {
            await TestMissingInRegularAndScriptAsync(
@"[GuidAttribute([|""1A585C4D-3371-48dc-AF8A-AFFECC1B0967""|])]
public interface I
{
}");
        }

        [WorkItem(546095, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546095")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNotOnTypeOfInAttribute()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Runtime.InteropServices;

[ComSourceInterfaces([|typeof(GuidAttribute)|])]
public class Button
{
}");
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPreferGenerateConstantField1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void goo(string s = ""Hello"")
    {
        var s2 = [|""Hello""|] + ""World"";
    }
}",
@"class C
{
    private const string {|Rename:V|} = ""Hello"";

    void goo(string s = ""Hello"")
    {
        var s2 = V + ""World"";
    }
}");
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPreferGenerateConstantField2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void goo(string s = ""Hello"")
    {
        var s2 = [|""Hello""|] + ""World"";
    }
}",
@"class C
{
    private const string {|Rename:V|} = ""Hello"";

    void goo(string s = V)
    {
        var s2 = V + ""World"";
    }
}",
index: 1);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPreferGenerateConstantField3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void goo(string s = ""Hello"")
    {
        var s2 = [|""Hello""|] + ""World"";
    }
}",
@"class C
{
    void goo(string s = ""Hello"")
    {
        const string {|Rename:V|} = ""Hello"";
        var s2 = V + ""World"";
    }
}",
index: 2);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPreferGenerateConstantField4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void goo(string s = ""Hello"")
    {
        var s2 = [|""Hello""|] + ""World"";
    }
}",
@"class C
{
    void goo(string s = ""Hello"")
    {
        const string {|Rename:V|} = ""Hello"";
        var s2 = V + ""World"";
    }
}",
index: 3);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfAccessingLocal1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void goo(string s = ""Hello"")
    {
        const string s1 = ""World"";
        var s2 = [|""Hello"" + s1|];
    }
}",
@"class C
{
    void goo(string s = ""Hello"")
    {
        const string s1 = ""World"";
        const string {|Rename:V|} = ""Hello"" + s1;
        var s2 = V;
    }
}");
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfAccessingLocal2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void goo(string s = ""Hello"")
    {
        const string s1 = ""World"";
        var s2 = [|""Hello"" + s1|];
    }
}",
@"class C
{
    void goo(string s = ""Hello"")
    {
        const string s1 = ""World"";
        const string {|Rename:V|} = ""Hello"" + s1;
        var s2 = V;
    }
}",
index: 1);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfNotAccessingLocal1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    const string s1 = ""World"";

    void goo(string s = ""Hello"")
    {
        var s2 = [|""Hello"" + s1|];
    }
}",
@"class C
{
    const string s1 = ""World"";
    private const string {|Rename:V|} = ""Hello"" + s1;

    void goo(string s = ""Hello"")
    {
        var s2 = V;
    }
}");
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfNotAccessingLocal2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    const string s1 = ""World"";

    void goo(string s = ""Hello"")
    {
        var s2 = [|""Hello"" + s1|];
    }
}",
@"class C
{
    const string s1 = ""World"";
    private const string {|Rename:V|} = ""Hello"" + s1;

    void goo(string s = ""Hello"")
    {
        var s2 = V;
    }
}",
index: 1);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfNotAccessingLocal3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    const string s1 = ""World"";

    void goo(string s = ""Hello"")
    {
        var s2 = [|""Hello"" + s1|];
    }
}",
@"class C
{
    const string s1 = ""World"";

    void goo(string s = ""Hello"")
    {
        const string {|Rename:V|} = ""Hello"" + s1;
        var s2 = V;
    }
}",
index: 2);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfNotAccessingLocal4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    const string s1 = ""World"";

    void goo(string s = ""Hello"")
    {
        var s2 = [|""Hello"" + s1|];
    }
}",
@"class C
{
    const string s1 = ""World"";

    void goo(string s = ""Hello"")
    {
        const string {|Rename:V|} = ""Hello"" + s1;
        var s2 = V;
    }
}",
index: 3);
        }

        [WorkItem(606347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606347")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InsertNeededCast1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

static class C
{
    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

    static T Goo<T>(T x) { return x; }

    static void Main()
    {
        Outer(y => Inner(x => { [|Goo(x)|].ToString(); }, y), null);
    }
}",

@"using System;

static class C
{
    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

    static T Goo<T>(T x) { return x; }

    static void Main()
    {
        Outer(y => Inner(x => { var {|Rename:v|} = Goo(x); v.ToString(); }, y), null);
    }
}",

options: ImplicitTypingEverywhere());
        }

        [WorkItem(606347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606347")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InsertNeededCast1NotVar()
        {
            await TestInRegularAndScriptAsync(
@"using System;

static class C
{
    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

    static T Goo<T>(T x) { return x; }

    static void Main()
    {
        Outer(y => Inner(x => { [|Goo(x)|].ToString(); }, y), null);
    }
}",

@"using System;

static class C
{
    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

    static T Goo<T>(T x) { return x; }

    static void Main()
    {
        Outer(y => Inner(x => { string {|Rename:v|} = Goo(x); v.ToString(); }, y), (object)null);
    }
}");
        }

        [WorkItem(606347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606347"), WorkItem(714632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/714632")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InsertNeededCast2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Goo([|x => 0|], y => 0, z, z);
    }

    static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}",

@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Func<byte, byte> {|Rename:p|} = x => 0;
        Goo(p, y => (byte)0, z, z);
    }

    static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}");
        }

        [WorkItem(546512, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546512")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInSwitchSection()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class Program
{
    int Main(int i)
    {
        switch (1)
        {
            case 0:
                var f = Main([|1 + 1|]);
                Console.WriteLine(f);
        }
    }
}",
@"using System;
class Program
{
    int Main(int i)
    {
        switch (1)
        {
            case 0:
                const int {|Rename:I|} = 1 + 1;
                var f = Main(I);
                Console.WriteLine(f);
        }
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInSwitchSection_AllOccurencesMultiStatement()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class Program
{
    int Main(int i)
    {
        switch (1)
        {
            case 0:
                var f = Main([|1 + 1|]);
                var g = Main(1 + 1);
            case 1:
                Console.WriteLine(1 + 0);
        }
    }
}",
@"using System;
class Program
{
    int Main(int i)
    {
        switch (1)
        {
            case 0:
                const int {|Rename:I|} = 1 + 1;
                var f = Main(I);
                var g = Main(I);
            case 1:
                Console.WriteLine(1 + 0);
        }
    }
}",
index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInSwitchSection_AllOccurencesDifferentSections()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class Program
{
    int Main(int i)
    {
        switch (1)
        {
            case 0:
                var f = Main([|1 + 1|]);
                var g = Main(1 + 1);
            case 1:
                Console.WriteLine(1 + 1);
        }
    }
}",
@"using System;
class Program
{
    int Main(int i)
    {
        const int {|Rename:I|} = 1 + 1;
        switch (1)
        {
            case 0:
                var f = Main(I);
                var g = Main(I);
            case 1:
                Console.WriteLine(I);
        }
    }
}",
index: 3);
        }

        [WorkItem(530480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLambdaParameter1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, int> f = x => [|x + 1|];
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, int> f = x =>
        {
            var {|Rename:v|} = x + 1;
            return v;
        };
    }
}",
options: ImplicitTypingEverywhere());
        }

        [WorkItem(530480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLambdaParameter2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = x => y => [|x + 1|];
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = x => y =>
        {
            var {|Rename:v|} = x + 1;
            return v;
        };
    }
}",
options: ImplicitTypingEverywhere());
        }

        [WorkItem(530480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLambdaParameter3()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = x => y => [|y + 1|];
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = x => y =>
        {
            var {|Rename:v|} = y + 1;
            return v;
        };
    }
}",
options: ImplicitTypingEverywhere());
        }

        [WorkItem(530480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLambdaParameter4()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = x => [|y => y + 1|];
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = x =>
        {
            Func<int, int> {|Rename:p|} = y => y + 1;
            return p;
        };
    }
}");
        }

        [WorkItem(530480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLambdaParameter5()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = x => [|y => x + 1|];
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = x =>
        {
            Func<int, int> {|Rename:p|} = y => x + 1;
            return p;
        };
    }
}");
        }

        [WorkItem(530721, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530721")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroVarInAction1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    void M()
    {
        Action<int> goo = x => [|x.ToString()|];
    }
}",
@"using System;

class Program
{
    void M()
    {
        Action<int> goo = x =>
        {
            string {|Rename:v|} = x.ToString();
        };
    }
}");
        }

        [WorkItem(530919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530919")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNullableOfPointerType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        [|new Nullable<int*>()|].GetValueOrDefault();
    }
}",
@"using System;

class Program
{
    static void Main()
    {
        var {|Rename:v|} = new Nullable<int*>();
        v.GetValueOrDefault();
    }
}",
options: ImplicitTypingEverywhere());
        }

        [WorkItem(530919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530919")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNullableOfPointerTypeNotVar()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        [|new Nullable<int*>()|].GetValueOrDefault();
    }
}",
@"using System;

class Program
{
    static void Main()
    {
        Nullable<int*> {|Rename:v|} = new Nullable<int*>();
        v.GetValueOrDefault();
    }
}");
        }

        [WorkItem(830885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/830885")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalRemovesUnnecessaryCast()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    static void Main(string[] args)
    {
        var set = new HashSet<string>();
        set.Add([|set.ToString()|]);
    }
}",
@"using System.Collections.Generic;

class C
{
    static void Main(string[] args)
    {
        var set = new HashSet<string>();
        var {|Rename:item|} = set.ToString();
        set.Add(item);
    }
}",
options: ImplicitTypingEverywhere());
        }

        [WorkItem(655498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/655498")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task HandleParenthesizedExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        ([|(C.Bar)|].Invoke)();
    }

    static Action Bar;
}",

@"using System;

class C
{
    void Goo()
    {
        Action {|Rename:bar|} = (C.Bar);
        bar.Invoke();
    }

    static Action Bar;
}");
        }

        [WorkItem(682683, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682683")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task DontRemoveParenthesesIfOperatorPrecedenceWouldBeBroken()
        {
            await TestInRegularAndScriptAsync(
@"using System;
 
class Program
{
    static void Main()
    {
        Console.WriteLine(5 - ([|1|] + 2));
    }
}",

@"using System;
 
class Program
{
    static void Main()
    {
        const int {|Rename:V|} = 1;
        Console.WriteLine(5 - (V + 2));
    }
}",
index: 2);
        }

        [WorkItem(828108, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828108")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task UseNewSemanticModelForSimplification()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var d = new Dictionary<string, Exception>();
        d.Add(""a"", [|new Exception()|]);
    }
}",

@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var d = new Dictionary<string, Exception>();
        var {|Rename:value|} = new Exception();
        d.Add(""a"", value);
    }
}",
options: ImplicitTypingEverywhere());
        }

        [WorkItem(884961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/884961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInCollectionInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var l = new List<int>() { [|Environment.TickCount|] };
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var {|Rename:tickCount|} = Environment.TickCount;
        var l = new List<int>() { tickCount };
    }
}",
options: ImplicitTypingEverywhere());
        }

        [WorkItem(884961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/884961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInCollectionInitializerNoVar()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var l = new List<int>() { [|Environment.TickCount|] };
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        int {|Rename:tickCount|} = Environment.TickCount;
        var l = new List<int>() { tickCount };
    }
}");
        }

        [WorkItem(854662, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854662")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInNestedCollectionInitializers()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
class C
{
    public Dictionary<int, int> A { get; private set; }
    static int Main(string[] args)
    {
        int a = 0;
        return new Program { A = { { [|a + 2|], 0 } } }.A.Count;
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    public Dictionary<int, int> A { get; private set; }
    static int Main(string[] args)
    {
        int a = 0;
        var {|Rename:v|} = a + 2;
        return new Program { A = { { v, 0 } } }.A.Count;
    }
}",
options: ImplicitTypingEverywhere());
        }

        [WorkItem(884961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/884961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInArrayInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var a = new int[] { [|Environment.TickCount|] };
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var {|Rename:tickCount|} = Environment.TickCount;
        var a = new int[] { tickCount };
    }
}",
options: ImplicitTypingEverywhere());
        }

        [WorkItem(884961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/884961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInArrayInitializerWithoutVar()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var a = new int[] { [|Environment.TickCount|] };
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        int {|Rename:tickCount|} = Environment.TickCount;
        var a = new int[] { tickCount };
    }
}");
        }

        [WorkItem(1022447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1022447")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFormattingOfIntroduceLocal2()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class C
{
    void M()
    {
        var s = ""Text"";
        var x = 42;
        if ([|s.Length|].CompareTo(x) > 0 &&
            s.Length.CompareTo(x) > 0)
        {
        }
    }
}",
@"using System;
class C
{
    void M()
    {
        var s = ""Text"";
        var x = 42;
        var {|Rename:length|} = s.Length;
        if (length.CompareTo(x) > 0 &&
            length.CompareTo(x) > 0)
        {
        }
    }
}",
index: 1,
options: ImplicitTypingEverywhere());
        }

        [WorkItem(939259, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939259")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalWithTriviaInMultiLineStatements()
        {
            var code =
    @"class C
{
    void Goo()
    {
        var d = [|true|] // TODO: comment
            ? 1
            : 2;
    }
}";

            var expected =
    @"class C
{
    void Goo()
    {
        const bool {|Rename:V|} = true;
        var d = V // TODO: comment
            ? 1
            : 2;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 3);
        }

        [WorkItem(939259, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939259")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalWithTriviaInMultiLineStatements2()
        {
            var code =
    @"class C
{
    void Goo()
    {
        var d = true
            ? 1
            : [|2|]; // TODO: comment
    }
}";

            var expected =
    @"class C
{
    void Goo()
    {
        const int {|Rename:V|} = 2;
        var d = true
            ? 1
            : V; // TODO: comment
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 3);
        }

        [WorkItem(1064803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064803")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInStringInterpolation()
        {
            var code =
    @"class C
{
    void Goo()
    {
        var s = $""Alpha Beta { [|int.Parse(""12345"")|] } Gamma"";
    }
}";

            var expected =
    @"class C
{
    void Goo()
    {
        var {|Rename:v|} = int.Parse(""12345"");
        var s = $""Alpha Beta { v } Gamma"";
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: ImplicitTypingEverywhere());
        }

        [WorkItem(1037057, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1037057")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalWithBlankLine()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    void M()
    {
        int x = 5;

        // comment
        int y = [|(x + 5)|] * (x + 5);
    }
}
", @"
class C
{
    void M()
    {
        int x = 5;

        // comment
        var {|Rename:v|} = (x + 5);
        int y = v * (x + 5);
    }
}
", options: ImplicitTypingEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalWithBlankLine_AllOccurencesMultiStatement()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    void M()
    {
        int x = 5;

        // comment
        int y = [|(x + 5)|] * (x + 5);
        int z = (x + 5);
    }
}
", @"
class C
{
    void M()
    {
        int x = 5;

        // comment
        var {|Rename:v|} = (x + 5);
        int y = v * v;
        int z = v;
    }
}
", options: ImplicitTypingEverywhere(), index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public Task TestIntroduceLocal_NullableType_FlowStateNonNull()
        => TestInRegularAndScriptAsync(@"
#nullable enable

class C
{
    void M()
    {
        string? s = string.Empty;
        M2([|s|]);
    }

    void M2(string? s)
    {
    }
}", @"
#nullable enable

class C
{
    void M()
    {
        string? s = string.Empty;
        string {|Rename:s1|} = s;
        M2(s1);
    }

    void M2(string? s)
    {
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public Task TestIntroduceLocal_NullableType_FlowStateNull()
        => TestInRegularAndScriptAsync(@"
#nullable enable

class C
{
    void M()
    {
        string? s = null;
        M2([|s|]);
    }

    void M2(string? s)
    {
    }
}", @"
#nullable enable

class C
{
    void M()
    {
        string? s = null;
        string? {|Rename:s1|} = s;
        M2(s1);
    }

    void M2(string? s)
    {
    }
}");

        [WorkItem(1065661, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065661")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceVariableTextDoesntSpanLines1()
        {
            await TestSmartTagTextAsync(
@"class C
{
    void M()
    {
        var s = [|@""a

b
c""|];
    }
}",
string.Format(FeaturesResources.Introduce_local_constant_for_0, @"@""a b c"""),
index: 2);
        }

        [WorkItem(1065661, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065661")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceVariableTextDoesntSpanLines2()
        {
            await TestSmartTagTextAsync(
@"class C
{
    void M()
    {
        var s = [|$@""a

b
c""|];
    }
}",
string.Format(FeaturesResources.Introduce_local_for_0, @"$@""a b c"""));
        }

        [WorkItem(1097147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSmartNameForNullablesInConditionalAccessExpressionContext()
        {
            var code =
    @"using System;
class C
{
    static void Goo(string s)
    {
        var l = [|s?.Length|] ?? 0;
    }
}";

            var expected =
    @"using System;
class C
{
    static void Goo(string s)
    {
        var {|Rename:length|} = s?.Length;
        var l = length ?? 0;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: ImplicitTypingEverywhere());
        }

        [WorkItem(1097147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSmartNameForNullablesInConditionalAccessExpressionContext2()
        {
            var code =
    @"using System;
class C
{
    static void Goo(string s)
    {
        var l = [|s?.ToLower()|] ?? string.Empty;
    }
}";

            var expected =
    @"using System;
class C
{
    static void Goo(string s)
    {
        var {|Rename:v|} = s?.ToLower();
        var l = v ?? string.Empty;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: ImplicitTypingEverywhere());
        }

        [WorkItem(1097147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSmartNameForNullablesInConditionalAccessExpressionContext3()
        {
            var code =
    @"using System;
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
        var l = [|a?.Prop?.Length|] ?? 0;
    }
}
class A
{
    public B Prop { get; set; }
}
class B
{
    public int Length { get; set; }
}";

            var expected =
    @"using System;
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
        var {|Rename:length|} = a?.Prop?.Length;
        var l = length ?? 0;
    }
}
class A
{
    public B Prop { get; set; }
}
class B
{
    public int Length { get; set; }
}";

            await TestInRegularAndScriptAsync(code, expected, options: ImplicitTypingEverywhere());
        }

        [WorkItem(1097147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSmartNameForNullablesInConditionalAccessExpressionContext4()
        {
            var code =
    @"using System;
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
        var age = [|a?.Prop?.GetAge()|] ?? 0;
    }
}
class A
{
    public B Prop { get; set; }
}
class B
{
    int age;
    public int GetAge() { return age; }
}";

            var expected =
    @"using System;
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
        var {|Rename:v|} = a?.Prop?.GetAge();
        var age = v ?? 0;
    }
}
class A
{
    public B Prop { get; set; }
}
class B
{
    int age;
    public int GetAge() { return age; }
}";

            await TestInRegularAndScriptAsync(code, expected, options: ImplicitTypingEverywhere());
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedMethod()
        {
            var code =
    @"using System;
class T
{
    int m;
    int M1() => [|1|] + 2 + 3 + m;
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;
    int m;
    int M1() => V + 2 + 3 + m;
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedNonVoidMethod()
        {
            var code =
    @"using System;
class T
{
    int m;
    int M1() => [|1|] + 2 + 3 + m;
}";

            var expected =
    @"using System;
class T
{
    int m;
    int M1()
    {
        const int {|Rename:V|} = 1;
        return V + 2 + 3 + m;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(31012, "https://github.com/dotnet/roslyn/issues/31012")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInArgumentList()
        {
            var code =
                @"using System;
public interface IResolver { int Resolve(); }
public class Test
{
    private void register(Func<IResolver, object> a) { }
    private void test(Func<int, object> factory)
        => register(x => factory(
            [|x.Resolve()|]
        ));
}";

            var expected =
                @"using System;
public interface IResolver { int Resolve(); }
public class Test
{
    private void register(Func<IResolver, object> a) { }
    private void test(Func<int, object> factory)
        => register(x =>
        {
            int {|Rename:arg|} = x.Resolve();
            return factory(
     arg
 );
        });
}";

            await TestInRegularAndScriptAsync(code, expected, index: 0);
        }

        [WorkItem(24807, "https://github.com/dotnet/roslyn/issues/24807")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedVoidMethod()
        {
            var code =
    @"using System;
class T
{
    int m;
    void M1() => Console.WriteLine([|1|] + 2 + 3 + m);
}";

            var expected =
    @"using System;
class T
{
    int m;
    void M1()
    {
        const int {|Rename:V|} = 1;
        Console.WriteLine(V + 2 + 3 + m);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedConstructor()
        {
            var code =
    @"using System;
class T
{
    int m;
    T() => Console.WriteLine([|1|] + 2 + 3 + m);
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;
    int m;
    T() => Console.WriteLine(V + 2 + 3 + m);
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(24807, "https://github.com/dotnet/roslyn/issues/24807")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedConstructor()
        {
            var code =
    @"using System;
class T
{
    int m;
    T() => Console.WriteLine([|1|] + 2 + 3 + m);
}";

            var expected =
    @"using System;
class T
{
    int m;
    T()
    {
        const int {|Rename:V|} = 1;
        Console.WriteLine(V + 2 + 3 + m);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedDestructor()
        {
            var code =
    @"using System;
class T
{
    int m;
    ~T() => Console.WriteLine([|1|] + 2 + 3 + m);
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;
    int m;
    ~T() => Console.WriteLine(V + 2 + 3 + m);
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(24807, "https://github.com/dotnet/roslyn/issues/24807")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedDestructor()
        {
            var code =
    @"using System;
class T
{
    int m;
    ~T() => Console.WriteLine([|1|] + 2 + 3 + m);
}";

            var expected =
    @"using System;
class T
{
    int m;
    ~T()
    {
        const int {|Rename:V|} = 1;
        Console.WriteLine(V + 2 + 3 + m);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedOperator()
        {
            var code =
    @"using System;
class Complex
{
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b) => a.Add(b.real + [|1|]);

    private Complex Add(int b)
    {
        throw new NotImplementedException();
    }
}";

            var expected =
    @"using System;
class Complex
{
    private const int {|Rename:V|} = 1;
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b) => a.Add(b.real + V);

    private Complex Add(int b)
    {
        throw new NotImplementedException();
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedOperator()
        {
            var code =
    @"using System;
class Complex
{
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b) => a.Add([|b.real + 1|]);

    private Complex Add(int b)
    {
        throw new NotImplementedException();
    }
}";

            var expected =
    @"using System;
class Complex
{
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b)
    {
        var {|Rename:b1|} = b.real + 1;
        return a.Add(b1);
    }

    private Complex Add(int b)
    {
        throw new NotImplementedException();
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: ImplicitTypingEverywhere());
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedConversionOperator()
        {
            var code =
    @"using System;
public struct DBBool
{
    public static readonly DBBool dbFalse = new DBBool(-1);
    int value;

    DBBool(int value)
    {
        this.value = value;
    }

    public static implicit operator DBBool(bool x) => x ? new DBBool([|1|]) : dbFalse;
}";

            var expected =
    @"using System;
public struct DBBool
{
    private const int {|Rename:Value|} = 1;
    public static readonly DBBool dbFalse = new DBBool(-1);
    int value;

    DBBool(int value)
    {
        this.value = value;
    }

    public static implicit operator DBBool(bool x) => x ? new DBBool(Value) : dbFalse;
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedConversionOperator()
        {
            var code =
    @"using System;
public struct DBBool
{
    public static readonly DBBool dbFalse = new DBBool(-1);
    int value;

    DBBool(int value)
    {
        this.value = value;
    }

    public static implicit operator DBBool(bool x) => x ? new DBBool([|1|]) : dbFalse;
}";

            var expected =
    @"using System;
public struct DBBool
{
    public static readonly DBBool dbFalse = new DBBool(-1);
    int value;

    DBBool(int value)
    {
        this.value = value;
    }

    public static implicit operator DBBool(bool x)
    {
        const int {|Rename:Value|} = 1;
        return x ? new DBBool(Value) : dbFalse;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedProperty()
        {
            var code =
    @"using System;
class T
{
    int M1 => [|1|] + 2;
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;

    int M1 => V + 2;
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedProperty()
        {
            var code =
    @"using System;
class T
{
    int M1 => [|1|] + 2;
}";

            var expected =
    @"using System;
class T
{
    int M1
    {
        get
        {
            const int {|Rename:V|} = 1;
            return V + 2;
        }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedIndexer()
        {
            var code =
    @"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i] => i > [|0|] ? arr[i + 1] : arr[i + 2];
}";

            var expected =
    @"using System;
class SampleCollection<T>
{
    private const int {|Rename:V|} = 0;
    private T[] arr = new T[100];
    public T this[int i] => i > V ? arr[i + 1] : arr[i + 2];
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedIndexer()
        {
            var code =
    @"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i] => i > 0 ? arr[[|i + 1|]] : arr[i + 2];
}";

            var expected =
    @"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get
        {
            var {|Rename:v|} = i + 1;
            return i > 0 ? arr[v] : arr[i + 2];
        }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: ImplicitTypingEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedPropertyGetter()
        {
            var code =
    @"using System;
class T
{
    int M1
    {
        get => [|1|] + 2;
    }
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;

    int M1
    {
        get => V + 2;
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedPropertyGetter()
        {
            var code =
    @"using System;
class T
{
    int M1
    {
        get => [|1|] + 2;
    }
}";

            var expected =
    @"using System;
class T
{
    int M1
    {
        get
        {
            const int {|Rename:V|} = 1;
            return V + 2;
        }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedPropertySetter()
        {
            var code =
    @"using System;
class T
{
    int M1
    {
        set => Console.WriteLine([|1|] + 2);
    }
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;

    int M1
    {
        set => Console.WriteLine(V + 2);
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedPropertySetter()
        {
            var code =
    @"using System;
class T
{
    int M1
    {
        set => Console.WriteLine([|1|] + 2);
    }
}";

            var expected =
    @"using System;
class T
{
    int M1
    {
        set
        {
            const int {|Rename:V|} = 1;
            Console.WriteLine(V + 2);
        }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedIndexerGetter()
        {
            var code =
    @"using System;
class T
{
    int this[int i]
    {
        get => [|1|] + 2;
    }
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;

    int this[int i]
    {
        get => V + 2;
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedIndexerGetter()
        {
            var code =
    @"using System;
class T
{
    int this[int i]
    {
        get => [|1|] + 2;
    }
}";

            var expected =
    @"using System;
class T
{
    int this[int i]
    {
        get
        {
            const int {|Rename:V|} = 1;
            return V + 2;
        }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedIndexerSetter()
        {
            var code =
    @"using System;
class T
{
    int this[int i]
    {
        set => Console.WriteLine([|1|] + 2);
    }
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;

    int this[int i]
    {
        set => Console.WriteLine(V + 2);
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedIndexerSetter()
        {
            var code =
    @"using System;
class T
{
    int this[int i]
    {
        set => Console.WriteLine([|1|] + 2);
    }
}";

            var expected =
    @"using System;
class T
{
    int this[int i]
    {
        set
        {
            const int {|Rename:V|} = 1;
            Console.WriteLine(V + 2);
        }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedEventAdder()
        {
            var code =
    @"using System;
class T
{
    event EventHandler E
    {
        add => Console.WriteLine([|1|] + 2);
        remove { }
    }
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;

    event EventHandler E
    {
        add => Console.WriteLine(V + 2);
        remove { }
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedEventAdder()
        {
            var code =
    @"using System;
class T
{
    event EventHandler E
    {
        add => Console.WriteLine([|1|] + 2);
        remove { }
    }
}";

            var expected =
    @"using System;
class T
{
    event EventHandler E
    {
        add
        {
            const int {|Rename:V|} = 1;
            Console.WriteLine(V + 2);
        }
        remove { }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedEventRemover()
        {
            var code =
    @"using System;
class T
{
    event EventHandler E
    {
        add { }
        remove => Console.WriteLine([|1|] + 2);
    }
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;

    event EventHandler E
    {
        add { }
        remove => Console.WriteLine(V + 2);
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedEventRemover()
        {
            var code =
    @"using System;
class T
{
    event EventHandler E
    {
        add { }
        remove => Console.WriteLine([|1|] + 2);
    }
}";

            var expected =
    @"using System;
class T
{
    event EventHandler E
    {
        add { }
        remove
        {
            const int {|Rename:V|} = 1;
            Console.WriteLine(V + 2);
        }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedLocalFunction()
        {
            var code =
    @"using System;
class T
{
    void M()
    {
        int F() => [|1|] + 2;
    }
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;

    void M()
    {
        int F() => V + 2;
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedNonVoidLocalFunction()
        {
            var code =
    @"using System;
class T
{
    void M()
    {
        int F() => [|1|] + 2;
    }
}";

            var expected =
    @"using System;
class T
{
    void M()
    {
        int F()
        {
            const int {|Rename:V|} = 1;
            return V + 2;
        }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedVoidLocalFunction()
        {
            var code =
    @"using System;
class T
{
    void M()
    {
        void F() => Console.WriteLine([|1|] + 2);
    }
}";

            var expected =
    @"using System;
class T
{
    void M()
    {
        void F()
        {
            const int {|Rename:V|} = 1;
            Console.WriteLine(V + 2);
        }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestTrailingTriviaOnExpressionBodiedMethodRewrites()
        {
            var code =
    @"using System;
class T
{
    int M1() => 1 + 2 + [|3|] /*not moved*/; /*moved to end of block*/

    // rewrite should preserve newline above this.
    void Cat() { }
}";

            var expected =
    @"using System;
class T
{
    int M1()
    {
        const int {|Rename:V|} = 3;
        return 1 + 2 + V /*not moved*/;
    } /*moved to end of block*/

    // rewrite should preserve newline above this.
    void Cat() { }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLeadingTriviaOnExpressionBodiedMethodRewrites()
        {
            var code =
    @"using System;
class T
{
    /*not moved*/
    int M1() => 1 + 2 + /*not moved*/ [|3|];
}";

            var expected =
    @"using System;
class T
{
    /*not moved*/
    int M1()
    {
        const int {|Rename:V|} = 3;
        return 1 + 2 + /*not moved*/ V;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestTriviaAroundArrowTokenInExpressionBodiedMemberSyntax()
        {
            var code =
    @"using System;
class T
{
    // comment
    int M1() /*c1*/ => /*c2*/ 1 + 2 + /*c3*/ [|3|];
}";

            var expected =
    @"using System;
class T
{
    // comment
    int M1() /*c1*/  /*c2*/
    {
        const int {|Rename:V|} = 3;
        return 1 + 2 + /*c3*/ V;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x)
    {
        return [|9|];
    };
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x)
    {
        const int {|Rename:V|} = 9;
        return V;
    };
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithSingleLineBlockBodiedAnonymousMethodExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x) { return [|9|]; };
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x) { const int {|Rename:V|} = 9; return V; };
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedSimpleLambdaExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => f =>
    {
        return f * [|9|];
    };
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y() => f =>
    {
        const int {|Rename:V|} = 9;
        return f * V;
    };
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithExpressionBodiedSimpleLambdaExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => f => f * [|9|];
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y() => f =>
    {
        const int {|Rename:V|} = 9;
        return f * V;
    };
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedParenthesizedLambdaExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => (f) =>
    {
        return f * [|9|];
    };
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y() => (f) =>
    {
        const int {|Rename:V|} = 9;
        return f * V;
    };
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithExpressionBodiedParenthesizedLambdaExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => (f) => f * [|9|];
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y() => (f) =>
    {
        const int {|Rename:V|} = 9;
        return f * V;
    };
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpressionInMethodArgs()
        {
            var code =
    @"using System;
class TestClass
{
    public int Prop => Method1(delegate()
    {
        return [|8|];
    });
}";

            var expected =
    @"using System;
class TestClass
{
    public int Prop => Method1(delegate()
    {
        const int {|Rename:V|} = 8;
        return V;
    });
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoConstantForInterpolatedStrings1()
        {
            var code =
    @"using System;
class TestClass
{
    static void Test(string[] args)
    {
        Console.WriteLine([|$""{DateTime.Now.ToString()}Text{args[0]}""|]);
    }
}";

            var expected =
    @"using System;
class TestClass
{
    static void Test(string[] args)
    {
        var {|Rename:value|} = $""{DateTime.Now.ToString()}Text{args[0]}"";
        Console.WriteLine(value);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: ImplicitTypingEverywhere());
        }

        [WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoConstantForInterpolatedStrings2()
        {
            var code =
    @"using System;
class TestClass
{
    static void Test(string[] args)
    {
        Console.WriteLine([|$""Text{{s}}""|]);
        Console.WriteLine($""Text{{s}}"");
    }
}";

            var expected =
    @"using System;
class TestClass
{
    static void Test(string[] args)
    {
        var {|Rename:value|} = $""Text{{s}}"";
        Console.WriteLine(value);
        Console.WriteLine(value);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1, options: ImplicitTypingEverywhere());
        }

        [WorkItem(909152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/909152")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnNullLiteral()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C1
{
}

class C2
{
}

class Test
{
    void M()
    {
        C1 c1 = [|null|];
        C2 c2 = null;
    }
}");
        }

        [WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InParentConditionalAccessExpressions()
        {
            var code =
    @"using System;
class C
{
    public T F<T>(T x)
    {
        var y = [|F(new C())|]?.F(new C())?.F(new C());
        return x;
    }
}";

            var expected =
    @"using System;
class C
{
    public T F<T>(T x)
    {
        var {|Rename:c|} = F(new C());
        var y = c?.F(new C())?.F(new C());
        return x;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: ImplicitTypingEverywhere());
        }

        [WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InParentConditionalAccessExpression2()
        {
            var code =
    @"using System;
class C
{
    public T F<T>(T x)
    {
        var y = [|F(new C()).F(new C())|]?.F(new C());
        return x;
    }
}";

            var expected =
    @"using System;
class C
{
    public T F<T>(T x)
    {
        var {|Rename:c|} = F(new C()).F(new C());
        var y = c?.F(new C());
        return x;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, options: ImplicitTypingEverywhere());
        }

        [WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")]
        [WorkItem(3110, "https://github.com/dotnet/roslyn/issues/3110")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task MissingAcrossMultipleParentConditionalAccessExpressions()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    public T F<T>(T x)
    {
        var y = [|F(new C())?.F(new C())|]?.F(new C());
        return x;
    }
}");
        }

        [WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task MissingOnInvocationExpressionInParentConditionalAccessExpressions()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    public T F<T>(T x)
    {
        var y = F(new C())?.[|F(new C())|]?.F(new C());
        return x;
    }
}");
        }

        [WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task MissingOnMemberBindingExpressionInParentConditionalAccessExpressions()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static void Test(string s)
    {
        var l = s?.[|Length|] ?? 0;
    }
}");
        }

        [WorkItem(3147, "https://github.com/dotnet/roslyn/issues/3147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task HandleFormattableStringTargetTyping1()
        {
            const string code = CodeSnippets.FormattableStringType + @"
namespace N
{
    using System;

    class C
    {
        public async Task M()
        {
            var f = FormattableString.Invariant([|$""""|]);
        }
    }
}";

            const string expected = CodeSnippets.FormattableStringType + @"
namespace N
{
    using System;

    class C
    {
        public async Task M()
        {
            FormattableString {|Rename:formattable|} = $"""";
            var f = FormattableString.Invariant(formattable);
        }
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InAutoPropertyInitializer()
        {
            var code =
@"using System;
class C
{
    int Prop1 { get; } = [|1 + 2|];
}";
            var expected =
@"using System;
class C
{
    private const int {|Rename:V|} = 1 + 2;

    int Prop1 { get; } = V;
}";
            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InAutoPropertyInitializer2()
        {
            var code =
@"using System;
class C
{
    public DateTime TimeStamp { get; } = [|DateTime.UtcNow|];
}";
            var expected =
@"using System;
class C
{
    private static readonly DateTime {|Rename:utcNow|} = DateTime.UtcNow;

    public DateTime TimeStamp { get; } = utcNow;
}";
            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task BlockContextPreferredOverAutoPropertyInitializerContext()
        {
            var code =
@"using System;
class C
{
    Func<int, int> X { get; } = a => { return [|7|]; };
}";
            var expected =
@"using System;
class C
{
    Func<int, int> X { get; } = a => { const int {|Rename:V|} = 7; return V; };
}";
            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task Tuple_TuplesDisabled()
        {
            var code =
@"class C
{
    var i = [|(1, ""hello"")|].ToString();
}";

            var expected =
            @"class C
{
    private static readonly (int, string) {|Rename:p|} = (1, ""hello"");
    var i = p.ToString();
}";

            await TestAsync(code, expected, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task ElementOfTuple()
        {
            var code =
@"class C
{
    var i = (1, [|""hello""|]).ToString();
}";

            var expected =
            @"class C
{
    private const string {|Rename:V|} = ""hello"";
    var i = (1, V).ToString();
}";

            await TestInRegularAndScriptAsync(
                code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task Tuple_IntroduceConstant()
        {
            var code =
@"class C
{
    var i = [|(1, ""hello"")|].ToString();
}";

            var expected =
@"class C
{
    private static readonly (int, string) {|Rename:p|} = (1, ""hello"");
    var i = p.ToString();
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TupleWithNames_IntroduceConstant()
        {
            var code =
@"class C
{
    var i = [|(a: 1, b: ""hello"")|].ToString();
}";

            var expected =
@"class C
{
    private static readonly (int a, string b) {|Rename:p|} = (a: 1, b: ""hello"");
    var i = p.ToString();
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task Tuple_IntroduceConstantForAllOccurrences()
        {
            var code =
@"class C
{
    var i = [|(1, ""hello"")|].ToString() + (1, ""hello"").ToString();
}";

            var expected =
@"class C
{
    private static readonly (int, string) {|Rename:p|} = (1, ""hello"");
    var i = p.ToString() + p.ToString();
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TupleWithNames_IntroduceConstantForAllOccurrences()
        {
            var code =
@"class C
{
    var i = [|(a: 1, b: ""hello"")|].ToString() + (a: 1, b: ""hello"").ToString();
}";

            var expected =
@"class C
{
    private static readonly (int a, string b) {|Rename:p|} = (a: 1, b: ""hello"");
    var i = p.ToString() + p.ToString();
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TupleWithDifferentNames_IntroduceConstantForAllOccurrences()
        {
            var code =
@"class C
{
    var i = [|(a: 1, b: ""hello"")|].ToString() + (c: 1, d: ""hello"").ToString();
}";

            var expected =
@"class C
{
    private static readonly (int a, string b) {|Rename:p|} = (a: 1, b: ""hello"");
    var i = p.ToString() + (c: 1, d: ""hello"").ToString();
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TupleWithOneName_IntroduceConstantForAllOccurrences()
        {
            var code =
@"class C
{
    var i = [|(a: 1, ""hello"")|].ToString() + (a: 1, ""hello"").ToString();
}";

            var expected =
@"class C
{
    private static readonly (int a, string) {|Rename:p|} = (a: 1, ""hello"");
    var i = p.ToString() + p.ToString();
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);

            // no third action available
            await TestActionCountAsync(code, count: 2, parameters: new TestParameters(TestOptions.Regular));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task Tuple_IntroduceLocalForAllOccurrences()
        {
            // Cannot refactor tuple as local constant
            await TestActionCountAsync(
@"class C
{
    void Goo()
    {
        Bar([|(1, ""hello"")|]);
        Bar((1, ""hello"");
    }
}", count: 2);
        }

        [WorkItem(11777, "https://github.com/dotnet/roslyn/issues/11777")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestGenerateLocalConflictingName1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    class MySpan { public int Start { get; } public int End { get; } }
    void Method(MySpan span)
    {
        int pos = span.Start;
        while (pos < [|span.End|])
        {
            int spanEnd = span.End;
            int end = pos;
        }
    }
}",
@"class Program
{
    class MySpan { public int Start { get; } public int End { get; } }
    void Method(MySpan span)
    {
        int pos = span.Start;
        int {|Rename:end1|} = span.End;
        while (pos < end1)
        {
            int spanEnd = span.End;
            int end = pos;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TupleWithInferredName_LeaveExplicitName()
        {
            var code =
@"class C
{
    static int y = 2;
    void M()
    {
        int a = 1;
        var t = (a, x: [|C.y|]);
    }
}";

            var expected =
            @"class C
{
    static int y = 2;
    void M()
    {
        int a = 1;
        int {|Rename:y1|} = C.y;
        var t = (a, x: y1);
    }
}";

            await TestAsync(code, expected, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TupleWithInferredName_InferredNameBecomesExplicit()
        {
            var code =
@"class C
{
    static int y = 2;
    void M()
    {
        int x = 1;
        var t = (x, [|C.y|]);
    }
}";

            var expected =
            @"class C
{
    static int y = 2;
    void M()
    {
        int x = 1;
        int {|Rename:y1|} = C.y;
        var t = (x, y: y1);
    }
}";

            await TestAsync(code, expected, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TupleWithInferredName_AllOccurrences()
        {
            var code =
@"class C
{
    static int y = 2;
    void M()
    {
        int x = 1;
        var t = (x, [|C.y|]);
        var t2 = (C.y, x);
    }
}";

            var expected =
            @"class C
{
    static int y = 2;
    void M()
    {
        int x = 1;
        int {|Rename:y1|} = C.y;
        var t = (x, y: y1);
        var t2 = (y: y1, x);
    }
}";
            await TestAsync(code, expected, index: 1, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TupleWithInferredName_NoDuplicateNames()
        {
            var code =
@"class C
{
    static int y = 2;
    void M()
    {
        int x = 1;
        var t = (C.y, [|C.y|]);
    }
}";

            var expected =
            @"class C
{
    static int y = 2;
    void M()
    {
        int x = 1;
        int {|Rename:y1|} = C.y;
        var t = (y1, y1);
    }
}";
            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task AnonymousTypeWithInferredName_LeaveExplicitName()
        {
            var code =
@"class C
{
    static int y = 2;
    void M()
    {
        int a = 1;
        var t = new { a, x= [|C.y|] };
    }
}";

            var expected =
            @"class C
{
    static int y = 2;
    void M()
    {
        int a = 1;
        int {|Rename:y1|} = C.y;
        var t = new { a, x= y1 };
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task AnonymousTypeWithInferredName_InferredNameBecomesExplicit()
        {
            var code =
@"class C
{
    static int y = 2;
    void M()
    {
        int x = 1;
        var t = new { x, [|C.y|] };
    }
}";

            var expected =
            @"class C
{
    static int y = 2;
    void M()
    {
        int x = 1;
        int {|Rename:y1|} = C.y;
        var t = new { x, y = y1 };
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task AnonymousTypeWithInferredName_NoDuplicatesAllowed()
        {
            var code =
@"class C
{
    static int y = 2;
    void M()
    {
        int x = 1;
        var t = new { C.y, [|C.y|] }; // this is an error already
    }
}";

            var expected =
            @"class C
{
    static int y = 2;
    void M()
    {
        int x = 1;
        int {|Rename:y1|} = C.y;
        var t = new { y= y1, y= y1 }; // this is an error already
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(31795, "https://github.com/dotnet/roslyn/issues/31795")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInAnonymousObjectMemberDeclaratorWithInferredType()
        {
            var code =
@"using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    void Test(Dictionary<int, List<Guid>> d)
    {
        _ = new
        {
            a = [|d.Values|].Where(l => l.Count == 1)
        };
    }
}";

            var expected =
@"using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    void Test(Dictionary<int, List<Guid>> d)
    {
        Dictionary<int, List<Guid>>.ValueCollection {|Rename:values|} = d.Values;
        _ = new
        {
            a = values.Where(l => l.Count == 1)
        };
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(2423, "https://github.com/dotnet/roslyn/issues/2423")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPickNameBasedOnArgument1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C(string a, string b)
    {
        new TextSpan([|int.Parse(a)|], int.Parse(b));
    }
}

struct TextSpan
{
    public TextSpan(int start, int length)
    {

    }
}",
@"class C
{
    public C(string a, string b)
    {
        int {|Rename:start|} = int.Parse(a);
        new TextSpan(start, int.Parse(b));
    }
}

struct TextSpan
{
    public TextSpan(int start, int length)
    {

    }
}");
        }

        [WorkItem(2423, "https://github.com/dotnet/roslyn/issues/2423")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPickNameBasedOnArgument2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C(string a, string b)
    {
        new TextSpan(int.Parse(a), [|int.Parse(b)|]);
    }
}

struct TextSpan
{
    public TextSpan(int start, int length)
    {

    }
}",
@"class C
{
    public C(string a, string b)
    {
        int {|Rename:length|} = int.Parse(b);
        new TextSpan(int.Parse(a), length);
    }
}

struct TextSpan
{
    public TextSpan(int start, int length)
    {

    }
}");
        }

        [WorkItem(21665, "https://github.com/dotnet/roslyn/issues/21665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPickNameBasedOnValueTupleFieldName1()
        {
            await TestAsync(
@"using System;

class C
{
    public C(string a, string b)
    {
        var tuple = (id: 1, date: [|DateTime.Now.ToString()|]);
    }
}",
@"using System;

class C
{
    public C(string a, string b)
    {
        string {|Rename:date|} = DateTime.Now.ToString();
        var tuple = (id: 1, date: date);
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
        }

        [WorkItem(21665, "https://github.com/dotnet/roslyn/issues/21665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPickNameBasedOnValueTupleFieldName2()
        {
            await TestAsync(
@"using System;

class C
{
    public C()
    {
        var tuple = (key: 1, value: [|1 + 1|]);
    }
}",
@"using System;

class C
{
    private const int {|Rename:Value|} = 1 + 1;

    public C()
    {
        var tuple = (key: 1, value: Value);
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), index: 0);
        }

        [WorkItem(21665, "https://github.com/dotnet/roslyn/issues/21665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPickNameBasedOnValueTupleFieldName3()
        {
            await TestAsync(
@"using System;

class C
{
    public C()
    {
        var tuple = (key: 1, value: [|1 + 1|]);
    }
}",
@"using System;

class C
{
    public C()
    {
        const int {|Rename:Value|} = 1 + 1;
        var tuple = (key: 1, value: Value);
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), index: 2);
        }

        [WorkItem(21373, "https://github.com/dotnet/roslyn/issues/21373")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInAttribute()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    public string Goo { get; set; }

    [Example([|2+2|])]
    public string Bar { get; set; }
}",
@"public class C
{
    private const int {|Rename:V|} = 2 + 2;

    public string Goo { get; set; }

    [Example(V)]
    public string Bar { get; set; }
}");
        }

        [WorkItem(21687, "https://github.com/dotnet/roslyn/issues/21687")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIfClassHasSameNameAsNamespace()
        {
            await TestInRegularAndScriptAsync(
@"namespace C
{
    class C
    {
        void M()
        {
            var t = new { foo = [|1 + 1|] };
        }
    }
}",
@"namespace C
{
    class C
    {
        private const int {|Rename:V|} = 1 + 1;

        void M()
        {
            var t = new { foo = V };
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10123, "https://github.com/dotnet/roslyn/issues/10123")]
        public async Task TestSimpleParameterName()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M(int a)
    {
        System.Console.Write([|a|]);
    }
}",
@"
class C
{
    void M(int a)
    {
        int {|Rename:a1|} = a;
        System.Console.Write(a1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10123, "https://github.com/dotnet/roslyn/issues/10123")]
        public async Task TestSimpleParamterName_EmptySelection()
        {
            await TestMissingAsync(
@"class C
{
    void M(int a)
    {
        System.Console.Write([||]a);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10123, "https://github.com/dotnet/roslyn/issues/10123")]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestSimpleParamterName_SmallSelection()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int parameter)
    {
        System.Console.Write([|par|]ameter);
    }
}",
@"class C
{
    void M(int parameter)
    {
        int {|Rename:parameter1|} = parameter;
        System.Console.Write(parameter1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10123, "https://github.com/dotnet/roslyn/issues/10123")]
        public async Task TestFieldName_QualifiedWithThis()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    int a;
    void M()
    {
        System.Console.Write([|this.a|]);
    }
}",
@"
class C
{
    int a;
    void M()
    {
        int {|Rename:a1|} = this.a;
        System.Console.Write(a1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10123, "https://github.com/dotnet/roslyn/issues/10123")]
        public async Task TestFieldName_QualifiedWithType()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    static int a;
    void M()
    {
        System.Console.Write([|C.a|]);
    }
}",
@"
class C
{
    static int a;
    void M()
    {
        int {|Rename:a1|} = C.a;
        System.Console.Write(a1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10123, "https://github.com/dotnet/roslyn/issues/10123")]
        public async Task TestFieldName_QualifiedWithType_TinySelection1()
        {
            await TestMissingAsync(
@"
class C
{
    static int a;
    void M()
    {
        System.Console.Write(C[|.|]a);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10123, "https://github.com/dotnet/roslyn/issues/10123")]
        public async Task TestFieldName_QualifiedWithType_TinySelection2()
        {
            await TestMissingAsync(
@"
class C
{
    static int a;
    void M()
    {
        System.Console.Write([|C.|]a);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10123, "https://github.com/dotnet/roslyn/issues/10123")]
        public async Task TestFieldName_QualifiedWithType_TinySelection3()
        {
            await TestMissingAsync(
@"
class C
{
    static int a;
    void M()
    {
        System.Console.Write(C.[|a|]);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10123, "https://github.com/dotnet/roslyn/issues/10123")]
        public async Task TestFieldName_QualifiedWithType_EmptySelection()
        {
            await TestMissingAsync(
@"class C
{
    static int a;
    void M()
    {
        System.Console.Write(C.[||]a);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(25990, "https://github.com/dotnet/roslyn/issues/25990")]
        public async Task TestWithLineBreak()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x = [|
            5 * 2 |]
            ;
    }
}",
@"class C
{
    private const int {|Rename:V|} = 5 * 2;

    void M()
    {
        int x =
            V;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(25990, "https://github.com/dotnet/roslyn/issues/25990")]
        public async Task TestWithLineBreak_AfterExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x =
[|            5 * 2
|]            ;
    }
}",
@"class C
{
    private const int {|Rename:V|} = 5 * 2;

    void M()
    {
        int x =
            V;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(25990, "https://github.com/dotnet/roslyn/issues/25990")]
        public async Task TestWithLineBreak_WithMultiLineComment()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = // start [| comment
            5 * 2 |]
            ;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(25990, "https://github.com/dotnet/roslyn/issues/25990")]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestWithLineBreak_WithSingleLineComments()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x = /*comment1*/ [|
            5 * 2 |]
            ;
    }
}",
@"class C
{
    private const int {|Rename:V|} = 5 * 2;

    void M()
    {
        int x = /*comment1*/
            V;
    }
}"
);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInCallRefExpression()
        {
            // This test indicates that ref-expressions are l-values and
            // introduce local still introduces a local, not a ref-local.
            await TestInRegularAndScriptAsync(@"
class C
{
    void M(int x)
    {
        ref int y = ref x;
        M2([|(y = ref x)|]);
    }
    void M2(int p) { }
}", @"
class C
{
    void M(int x)
    {
        ref int y = ref x;
        int {|Rename:p|} = (y = ref x);
        M2(p);
    }
    void M2(int p) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInRefCallRefExpression()
        {
            // Cannot extract expressions passed by ref
            await TestMissingInRegularAndScriptAsync(@"
class C
{
    void M(int x)
    {
        ref int y = ref x;
        M2(ref [|(y = ref x)|]);
    }
    void M2(ref int p) { }
}");
        }

        [WorkItem(28266, "https://github.com/dotnet/roslyn/issues/28266")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestCaretAtEndOfExpression1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        Bar(1[||], 2);
    }
}",
@"class C
{
    private const int {|Rename:V|} = 1;

    void Goo()
    {
        Bar(V, 2);
    }
}");
        }

        [WorkItem(28266, "https://github.com/dotnet/roslyn/issues/28266")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestCaretAtEndOfExpression2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        Bar(1, 2[||]);
    }
}",
@"class C
{
    private const int {|Rename:V|} = 2;

    void Goo()
    {
        Bar(1, V);
    }
}");
        }

        [WorkItem(28266, "https://github.com/dotnet/roslyn/issues/28266")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestCaretAtEndOfExpression3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        Bar(1, (2[||]));
    }
}",
@"class C
{
    private const int {|Rename:V|} = (2);

    void Goo()
    {
        Bar(1, V);
    }
}");
        }

        [WorkItem(28266, "https://github.com/dotnet/roslyn/issues/28266")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestCaretAtEndOfExpression4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        Bar(1, Bar(2[||]));
    }
}",
@"class C
{
    private const int {|Rename:V|} = 2;

    void Goo()
    {
        Bar(1, Bar(V));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(27949, "https://github.com/dotnet/roslyn/issues/27949")]
        public async Task TestWhitespaceSpanInAssignment()
        {
            await TestMissingAsync(@"
class C
{
    int x = [| |] 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(28665, "https://github.com/dotnet/roslyn/issues/28665")]
        public async Task TestWhitespaceSpanInAttribute()
        {
            await TestMissingAsync(@"
class C
{
    [Example( [| |] )]
    public void Goo()
    {
    }
}");
        }

        [WorkItem(28941, "https://github.com/dotnet/roslyn/issues/28941")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestElementAccessExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class C
{
    byte[] getArray() => null;
    void test()
    {
        var goo = [|getArray()|][0];
    }
}",
@"using System;
class C
{
    byte[] getArray() => null;
    void test()
    {
        byte[] {|Rename:v|} = getArray();
        var goo = v[0];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIndexExpression()
        {
            var code = TestSources.Index + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|^1|]);
    }
}";

            var expected = TestSources.Index + @"
class Program
{
    static void Main(string[] args)
    {
        System.Index {|Rename:value|} = ^1;
        System.Console.WriteLine(value);
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestRangeExpression_None()
        {
            var code = TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|..|]);
    }
}";

            var expected = TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Range {|Rename:value|} = ..;
        System.Console.WriteLine(value);
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestRangeExpression_Right()
        {
            var code = TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|..1|]);
    }
}";

            var expected = TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Range {|Rename:value|} = ..1;
        System.Console.WriteLine(value);
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestRangeExpression_Left()
        {
            var code = TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|1..|]);
    }
}";

            var expected = TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Range {|Rename:value|} = 1..;
        System.Console.WriteLine(value);
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestRangeExpression_Both()
        {
            var code = TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|1..2|]);
    }
}";

            var expected = TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Range {|Rename:value|} = 1..2;
        System.Console.WriteLine(value);
    }
}";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(30207, "http://github.com/dotnet/roslyn/issues/30207")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestImplicitRecursiveInstanceMemberAccess_ForAllOccurrences()
        {
            var code =
@"class C
{
    C c;
    void Test()
    {
        var x = [|c|].c.c;
    }
}";

            var expected =
@"class C
{
    C c;
    void Test()
    {
        C {|Rename:c1|} = c;
        var x = c1.c.c;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(30207, "http://github.com/dotnet/roslyn/issues/30207")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestExplicitRecursiveInstanceMemberAccess_ForAllOccurrences()
        {
            var code =
@"class C
{
    C c;
    void Test()
    {
        var x = [|this.c|].c.c;
    }
}";

            var expected =
@"class C
{
    C c;
    void Test()
    {
        C {|Rename:c1|} = this.c;
        var x = c1.c.c;
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(30207, "http://github.com/dotnet/roslyn/issues/30207")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestExplicitInstanceMemberAccess_ForAllOccurrences()
        {
            var code =
@"class C
{
    C c;
    void Test(C arg)
    {
        var x = [|this.c|].Test(this.c);
    }
}";

            var expected =
@"class C
{
    C c;
    void Test(C arg)
    {
        C {|Rename:c1|} = this.c;
        var x = c1.Test(c1);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(30207, "http://github.com/dotnet/roslyn/issues/30207")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestImplicitInstanceMemberAccess_ForAllOccurrences()
        {
            var code =
@"class C
{
    C c;
    void Test(C arg)
    {
        var x = [|c|].Test(c);
    }
}";

            var expected =
@"class C
{
    C c;
    void Test(C arg)
    {
        C {|Rename:c1|} = c;
        var x = c1.Test(c1);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(30207, "http://github.com/dotnet/roslyn/issues/30207")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestExpressionOfUndeclaredType()
        {
            var code =
@"class C
{
    void Test()
    {
        A[] array = [|A|].Foo();
        foreach (A a in array)
        {
        }
    }
}";
            await TestMissingAsync(code);
        }
    }
}
