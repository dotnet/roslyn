// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertForToForEach;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertForToForEach
{
    public class ConvertForToForEachTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertForToForEachCodeRefactoringProvider();

        private readonly CodeStyleOption<bool> onWithSilent = new CodeStyleOption<bool>(true, NotificationOption.Silent);

        private IDictionary<OptionKey, object> ImplicitTypeEverywhere() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithSilent),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestArray1()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestWarnIfCrossesFunctionBoundary()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Action a = () =>
            {
                Console.WriteLine(array[i]);
            };
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Action a = () =>
            {
                Console.WriteLine({|Warning:v|});
            };
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestWarnIfCollectionPotentiallyMutated1()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        [||]for (int i = 0; i < list.Count; i++)
        {
            Console.WriteLine(list[i]);
            list.Add(null);
        }
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        foreach (string {|Rename:v|} in list)
        {
            Console.WriteLine(v);
            {|Warning:list|}.Add(null);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestWarnIfCollectionPotentiallyMutated2()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        [||]for (int i = 0; i < list.Count; i++)
        {
            Console.WriteLine(list[i]);
            list = null;
        }
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        foreach (string {|Rename:v|} in list)
        {
            Console.WriteLine(v);
            {|Warning:list|} = null;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestNoWarnIfCollectionPropertyAccess()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        [||]for (int i = 0; i < list.Count; i++)
        {
            Console.WriteLine(list[i]);
            Console.WriteLine(list.Count);
        }
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        foreach (string {|Rename:v|} in list)
        {
            Console.WriteLine(v);
            Console.WriteLine(list.Count);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestNoWarnIfDoesNotCrossFunctionBoundary()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        Action a = () =>
        {
            [||]for (int i = 0; i < array.Length; i++)
            {
                Console.WriteLine(array[i]);
            }
        };
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        Action a = () =>
        {
            foreach (string {|Rename:v|} in array)
            {
                Console.WriteLine(v);
            }
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMultipleReferences()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestEmbeddedStatement()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
            Console.WriteLine(array[i]);
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
            Console.WriteLine(v);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestPostIncrement()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; ++i)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestArrayPlusEqualsIncrementor()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i += 1)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestBeforeKeyword()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
       [||] for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingAfterOpenParen()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        for ( [||]int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestInParentheses()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        for ([||]int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingBeforeCloseParen()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        for (int i = 0; i < array.Length; i++[||] )
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestInParentheses2()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        for (int i = 0; i < array.Length; i++[||])
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestAtEndOfFor()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        for[||] (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestForSelected()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        [|for|] (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestBeforeOpenParen()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        for [||](int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestAfterCloseParen()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        for (int i = 0; i < array.Length; i++)[||]
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithoutIncrementor()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; )
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithoutIncorrectIncrementor1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i += 2)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithoutIncorrectIncrementor2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; j += 2)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithoutCondition()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; ; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithIncorrectCondition1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; j < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithIncorrectCondition2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < GetLength(array); i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestWithoutInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestWithInitializerOfVariableOutsideLoop()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        int i;
        [||]for (i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestWithUninitializedVariable()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestNotStartingAtZero()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 1; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestWithMultipleVariables()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0, j = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestList1()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        [||]for (int i = 0; i < list.Count; i++)
        {
            Console.WriteLine(list[i]);
        }
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        foreach (string {|Rename:v|} in list)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestChooseNameFromDeclarationStatement()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        [||]for (int i = 0; i < list.Count; i++)
        {
            var val = list[i];
            Console.WriteLine(list[i]);
        }
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        foreach (var val in list)
        {
            Console.WriteLine(val);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestIgnoreFormattingForReferences()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        [||]for (int i = 0; i < list.Count; i++)
        {
            var val = list [ i ];
            Console.WriteLine(list [ /*find me*/ i ]);
        }
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        foreach (var val in list)
        {
            Console.WriteLine(val);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestChooseNameFromDeclarationStatement_PreserveComments()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        [||]for (int i = 0; i < list.Count; i++)
        {
            // loop comment

            var val = list[i];
            Console.WriteLine(list[i]);
        }
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        foreach (var val in list)
        {
            // loop comment

            Console.WriteLine(val);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestChooseNameFromDeclarationStatement_PreserveDirectives()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        [||]for (int i = 0; i < list.Count; i++)
        {
#if true

            var val = list[i];
            Console.WriteLine(list[i]);

#endif
        }
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    void Test(IList<string> list)
    {
        foreach (var val in list)
        {
#if true

            Console.WriteLine(val);

#endif
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingIfVariableUsedNotForIndexing()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(i);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingIfVariableUsedForIndexingNonCollection()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(other[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestWarningIfCollectionWrittenTo()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            array[i] = 1;
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            {|Warning:v|} = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task UseVarIfPreferred1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (var {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}", options: ImplicitTypeEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestDifferentIndexerAndEnumeratorType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class MyList
{
  public string this[int i] { get; }

  public Enumerator GetEnumerator() { }

  public struct Enumerator { public object Current { get; } }
}

class C
{
    void Test(MyList list)
    {
        // need to use 'string' here to preserve original index semantics.
        [||]for (int i = 0; i < list.Length; i++)
        {
            Console.WriteLine(list[i]);
        }
    }
}",
@"using System;

class MyList
{
  public string this[int i] { get; }

  public Enumerator GetEnumerator() { }

  public struct Enumerator { public object Current { get; } }
}

class C
{
    void Test(MyList list)
    {
        // need to use 'string' here to preserve original index semantics.
        foreach (string {|Rename:v|} in list)
        {
            Console.WriteLine(v);
        }
    }
}", options: ImplicitTypeEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestSameIndexerAndEnumeratorType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class MyList
{
    public object this[int i] { get => default; }

    public Enumerator GetEnumerator() { return default; }

    public struct Enumerator { public object Current { get; } public bool MoveNext() => true; }
}

class C
{
    void Test(MyList list)
    {
        // can use 'var' here since hte type stayed the same.
        [||]for (int i = 0; i < list.Length; i++)
        {
            Console.WriteLine(list[i]);
        }
    }
}",
@"using System;

class MyList
{
    public object this[int i] { get => default; }

    public Enumerator GetEnumerator() { return default; }

    public struct Enumerator { public object Current { get; } public bool MoveNext() => true; }
}

class C
{
    void Test(MyList list)
    {
        // can use 'var' here since hte type stayed the same.
        foreach (var {|Rename:v|} in list)
        {
            Console.WriteLine(v);
        }
    }
}", options: ImplicitTypeEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestTrivia()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        // trivia 1
        [||]for /*trivia 2*/ ( /*trivia 3*/ int i = 0; i < array.Length; i++) /*trivia 4*/
        // trivia 5
        {
            Console.WriteLine(array[i]);
        } // trivia 6
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        // trivia 1
        foreach /*trivia 2*/ ( /*trivia 3*/ string {|Rename:v|} in array) /*trivia 4*/
        // trivia 5
        {
            Console.WriteLine(v);
        } // trivia 6
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestNotWithDeconstruction()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (var (i, j) = (0, 0); i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMultidimensionalArray1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[,] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i, 0]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMultidimensionalArray2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[,] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i, i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestJaggedArray1()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[][] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[][] array)
    {
        foreach (string[] {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestJaggedArray2()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[][] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i][0]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[][] array)
    {
        foreach (string[] {|Rename:v|} in array)
        {
            Console.WriteLine(v[0]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestJaggedArray3()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[][] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            var subArray = array[i];
            for (int j = 0; j < subArray.Length; j++)
            {
                Console.WriteLine(array[i][j]);
            }
        }
    }
}",
@"using System;

class C
{
    void Test(string[][] array)
    {
        foreach (var subArray in array)
        {
            for (int j = 0; j < subArray.Length; j++)
            {
                Console.WriteLine(subArray[j]);
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestJaggedArray4()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[][] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            for (int j = 0; j < array[i].Length; j++)
            {
                Console.WriteLine(array[i][j]);
            }
        }
    }
}",
@"using System;

class C
{
    void Test(string[][] array)
    {
        foreach (string[] {|Rename:v|} in array)
        {
            for (int j = 0; j < v.Length; j++)
            {
                Console.WriteLine(v[j]);
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestJaggedArray5()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[][] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            [||]for (int j = 0; j < array[i].Length; j++)
            {
                Console.WriteLine(array[i][j]);
            }
        }
    }
}",
@"using System;

class C
{
    void Test(string[][] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            foreach (string {|Rename:v|} in array[i])
            {
                Console.WriteLine(v);
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestJaggedArray6()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[][] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i][i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestDoesNotUseLocalFunctionName()
        {
            await TestInRegularAndScript1Async(
    @"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }

        void v() { }
    }
}",
    @"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v1|} in array)
        {
            Console.WriteLine(v1);
        }

        void v() { }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestUsesLocalFunctionParameterName()
        {
            await TestInRegularAndScript1Async(
    @"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }

        void M(string v)
        {
        }
    }
}",
    @"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }

        void M(string v)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestDoesNotUseLambdaParameterWithCSharpLessThan8()
        {
            await TestInRegularAndScript1Async(
    @"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }

        Action<int> myLambda = v => { };
    }
}",
    @"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v1|} in array)
        {
            Console.WriteLine(v1);
        }

        Action<int> myLambda = v => { };
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestUsesLambdaParameterNameInCSharp8()
        {
            await TestInRegularAndScript1Async(
    @"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }

        Action<int> myLambda = v => { };
    }
}",
    @"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }

        Action<int> myLambda = v => { };
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp8)));
        }
    }
}
