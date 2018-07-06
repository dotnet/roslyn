// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertTupleToStruct
{
    public class ConvertTupleToStructTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertTupleToStructCodeRefactoringProvider();

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleType()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestNonLiteralNames()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: Foo(), b: Bar());
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertSingleTupleTypeWithInferredName()
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = [||](a: 1, b);
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertMultipleInstancesInSameMethod()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertMultipleInstancesAcrossMethods()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }

    void Method2()
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task OnlyConvertMatchingTypesInSameMethod()
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = [||](a: 1, b: 2);
        var t2 = (a: 3, b);
        var t3 = (a: 4, b: 5, c: 6);
        var t4 = (b: 5, a: 6);
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestFixAllMatchesInSingleMethod()
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = [||](a: 1, b: 2);
        var t2 = (a: 3, b);
        var t3 = (a: 4, b: 5, c: 6);
        var t4 = (b: 5, a: 6);
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestFixNotAcrossMethods()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }

    void Method2()
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestTrivia()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = /*1*/ [||]( /*2*/ a /*3*/ : /*4*/ 1 /*5*/ , /*6*/ b /*7*/ = /*8*/ 2 /*9*/ ) /*10*/ ;
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task NotIfReferencesAnonymousTypeInternally()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: new { c = 1, d = 2 });
    }
}
";

            await TestMissingInRegularAndScriptAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task ConvertMultipleNestedInstancesInSameMethod()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: (object)(a: 1, b: default(object)));
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task RenameAnnotationOnStartingPoint()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
        var t2 = [||](a: 3, b: 4);
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task CapturedTypeParameters()
        {
            var text = @"
class Test<X> where X : struct
{
    void Method<Y>(List<X> x, Y[] y) where Y : class, new()
    {
        var t1 = [||](a: x, b: y);
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task NewTypeNameCollision()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
    }
}

class NewStruct
{
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestDuplicatedName()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, a: 2);
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLambda1()
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
        Action a = () =>
        {
            var t2 = (a: 3, b: 4);
        };
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLambda2()
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
        Action a = () =>
        {
            var t2 = [||](a: 3, b: 4);
        };
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLocalFunction1()
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2);
        void Goo()
        {
            var t2 = (a: 3, b: 4);
        }
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
        public async Task TestInLocalFunction2()
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
        void Goo()
        {
            var t2 = [||](a: 3, b: 4);
        }
    }
}
";
            var expected = @"";
            await TestInRegularAndScriptAsync(text, expected);
        }
    }
}
