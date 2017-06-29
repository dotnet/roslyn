// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAutoPropertyToFullProperty
{
    public class ConvertAutoPropertyToFullPropertyTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider();

        private IDictionary<OptionKey, object> PreferExpressionBodiedAccessors =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement));

        private IDictionary<OptionKey, object> DoNotPreferExpressionBodiedAccessors =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement));

        private IDictionary<OptionKey, object> DoNotPreferExpressionBodiedAccessorsAndPropertyOpenBraceOnSameLine =>
             OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement),
                SingleOption(CSharpFormattingOptions.NewLinesForBracesInProperties, false));

        private IDictionary<OptionKey, object> DoNotPreferExpressionBodiedAccessorsAndAccessorOpenBraceOnSameLine =>
             OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement),
                SingleOption(CSharpFormattingOptions.NewLinesForBracesInAccessors, false));

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task SimpleAutoPropertyTest()
        {
            var text = @"
class foo
{
    public int F[||]oo { get; set; }
}
";
            var expected = @"
class foo
{
    private int _foo;

    public int Foo
    {
        get
        {
            return _foo;
        }
        set
        {
            _foo = value;
        }
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task AutoPropertyWithPrivateSetter()
        {
            var text = @"
class foo
{
    public int F[||]oo { get; private set; }
}
";
            var expected = @"
class foo
{
    private int _foo;

    public int Foo
    {
        get
        {
            return _foo;
        }
        private set
        {
            _foo = value;
        }
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task AutoPropertyWithFieldNameAlreadyUsed()
        {
            var text = @"
class foo
{
    private int _foo;

    public int F[||]oo { get; private set; }
}
";
        var expected = @"
class foo
{
    private int _foo;
    private int _foo1;

    public int Foo
    {
        get
        {
            return _foo1;
        }
        private set
        {
            _foo1 = value;
        }
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithComments()
        {
            var text = @"
class foo
{
    // Comments before
    public int F[||]oo { get; private set; } //Comments during
    //Comments after
}
";
        var expected = @"
class foo
{
    private int _foo;

    // Comments before
    public int Foo
    {
        get
        {
            return _foo;
        }
        private set
        {
            _foo = value;
        }
    } //Comments during
    //Comments after
}
";
            await TestInRegularAndScriptAsync(text, expected, options:DoNotPreferExpressionBodiedAccessors, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithExpressionBody()
        {
            var text = @"
class foo
{
    public int F[||]oo { get; set; }
}
";
        var expected = @"
class foo
{
    private int _foo;

    public int Foo { get => _foo; set => _foo = value; }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: PreferExpressionBodiedAccessors, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithExpressionBodyWithTrivia()
        {
            var text = @"
class foo
{
    public int F[||]oo { get /* test */ ; set /* test2 */ ; }
}
";
            var expected = @"
class foo
{
    private int _foo;

    public int Foo { get /* test */ => _foo; set /* test2 */ => _foo = value; }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: PreferExpressionBodiedAccessors, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithPropertyOpenBraceOnSameLine()
        {
            var text = @"
class foo
{
    public int F[||]oo { get; set; }
}
";
            var expected = @"
class foo
{
    private int _foo;

    public int Foo {
        get
        {
            return _foo;
        }
        set
        {
            _foo = value;
        }
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessorsAndPropertyOpenBraceOnSameLine, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task WithAccessorOpenBraceOnSameLine()
        {
            var text = @"
class foo
{
    public int F[||]oo { get; set; }
}
";
            var expected = @"
class foo
{
    private int _foo;

    public int Foo
    {
        get {
            return _foo;
        }
        set {
            _foo = value;
        }
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessorsAndAccessorOpenBraceOnSameLine, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task StaticProperty()
        {
            var text = @"
class foo
{
    public static int F[||]oo { get; set; }
}
";
            var expected = @"
class foo
{
    private static int s_foo;

    public static int Foo
    {
        get
        {
            return s_foo;
        }
        set
        {
            s_foo = value;
        }
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task PropertyWithAttributes()
        {
            var text = @"
class foo
{
    [A]
    public int F[||]oo { get; set; }
}
";
            var expected = @"
class foo
{
    private int _foo;

    [A]
    public int Foo
    {
        get
        {
            return _foo;
        }
        set
        {
            _foo = value;
        }
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task CommentsInAccessors()
        {
            var text = @"
class foo
{
    /// <summary>
    /// test stuff here
    /// </summary>
    public int testf[||]oo { /* test1 */ get /* test2 */; /* test3 */ set /* test4 */; /* test5 */ } /* test6 */
}
";
            var expected = @"
class foo
{
    private int _testfoo;

    /// <summary>
    /// test stuff here
    /// </summary>
    public int testfoo
    { /* test1 */
        get /* test2 */
        {
            return _testfoo;
        } /* test3 */
        set /* test4 */
        {
            _testfoo = value;
        } /* test5 */
    } /* test6 */
}
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors, ignoreTrivia: false);
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
    private string _name;

    public override string Name
    {
        get
        {
            return _name;
        }
        set
        {
            _name = value;
        }
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors, ignoreTrivia: false);
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
    private string _name;

    public sealed string Name
    {
        get
        {
            return _name;
        }
        set
        {
            _name = value;
        }
    }
}
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors, ignoreTrivia: false);
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
    private string _name;

    public virtual string Name
    {
        get
        {
            return _name;
        }
        set
        {
            _name = value;
        }
    }
}

class MyDerivedClass : MyBaseClass
{
    public override string Name {get; set;}
}
";
            await TestInRegularAndScriptAsync(text, expected, options: DoNotPreferExpressionBodiedAccessors, ignoreTrivia: false);
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
        public async Task GetterOnly()
        {
            var text = @"
class foo
{
    public int F[||]oo { get;}
}
";
            await TestMissingAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)]
        public async Task SetterOnly()
        {
            var text = @"
class foo
{
    public int F[||]oo
````{
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
class foo
{
   private int _testfoo;

   public int testf[||]oo {get => _testfoo; set => _testfoo = value; }
}
";
            await TestMissingAsync(text);
        }

    }
}
