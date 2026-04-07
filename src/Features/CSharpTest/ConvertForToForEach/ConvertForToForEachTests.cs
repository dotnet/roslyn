// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertForToForEach;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertForToForEach;

[Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
public sealed class ConvertForToForEachTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpConvertForToForEachCodeRefactoringProvider();

    private readonly CodeStyleOption2<bool> onWithSilent = new(true, NotificationOption2.Silent);

    private OptionsCollection ImplicitTypeEverywhere()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, onWithSilent },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent },
        };

    [Fact]
    public Task TestArray1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """);

    [Fact]
    public Task TestWarnIfCrossesFunctionBoundary()
        => TestInRegularAndScriptAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """);

    [Fact]
    public Task TestWarnIfCollectionPotentiallyMutated1()
        => TestInRegularAndScriptAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """);

    [Fact]
    public Task TestWarnIfCollectionPotentiallyMutated2()
        => TestInRegularAndScriptAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """);

    [Fact]
    public Task TestNoWarnIfCollectionPropertyAccess()
        => TestInRegularAndScriptAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """);

    [Fact]
    public Task TestNoWarnIfDoesNotCrossFunctionBoundary()
        => TestInRegularAndScriptAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """);

    [Fact]
    public Task TestMultipleReferences()
        => TestInRegularAndScriptAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """);

    [Fact]
    public Task TestEmbeddedStatement()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                        Console.WriteLine(array[i]);
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                        Console.WriteLine(v);
                }
            }
            """);

    [Fact]
    public Task TestPostIncrement()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; ++i)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """);

    [Fact]
    public Task TestArrayPlusEqualsIncrementor()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; i += 1)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestBeforeKeyword()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                   [||] for (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingAfterOpenParen()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    for ( [||]int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestInParentheses()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    for ([||]int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingBeforeCloseParen()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    for (int i = 0; i < array.Length; i++[||] )
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestInParentheses2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    for (int i = 0; i < array.Length; i++[||])
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """);

    [Fact]
    public Task TestAtEndOfFor()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    for[||] (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """);

    [Fact]
    public Task TestForSelected()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [|for|] (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """);

    [Fact]
    public Task TestBeforeOpenParen()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    for [||](int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """);

    [Fact]
    public Task TestAfterCloseParen()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    for (int i = 0; i < array.Length; i++)[||]
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingWithoutIncrementor()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; )
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingWithoutIncorrectIncrementor1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; i += 2)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingWithoutIncorrectIncrementor2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; j += 2)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingWithoutCondition()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; ; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingWithIncorrectCondition1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; j < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingWithIncorrectCondition2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < GetLength(array); i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithoutInitializer()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithInitializerOfVariableOutsideLoop()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

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
            }
            """);

    [Fact]
    public Task TestWithUninitializedVariable()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestNotStartingAtZero()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 1; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithMultipleVariables()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0, j = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestList1()
        => TestInRegularAndScriptAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """);

    [Fact]
    public Task TestChooseNameFromDeclarationStatement()
        => TestInRegularAndScriptAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """);

    [Fact]
    public Task TestIgnoreFormattingForReferences()
        => TestInRegularAndScriptAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """);

    [Fact]
    public Task TestChooseNameFromDeclarationStatement_PreserveComments()
        => TestInRegularAndScriptAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """);

    [Fact]
    public Task TestChooseNameFromDeclarationStatement_PreserveDirectives()
        => TestInRegularAndScriptAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """);

    [Fact]
    public Task TestMissingIfVariableUsedNotForIndexing()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingIfVariableUsedForIndexingNonCollection()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(other[i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestWarningIfCollectionWrittenTo()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = 1;
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (string {|Rename:v|} in array)
                    {
                        {|Warning:v|} = 1;
                    }
                }
            }
            """);

    [Fact]
    public Task UseVarIfPreferred1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    foreach (var {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestDifferentIndexerAndEnumeratorType()
        => TestInRegularAndScriptAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestSameIndexerAndEnumeratorType()
        => TestInRegularAndScriptAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestTrivia()
        => TestInRegularAndScriptAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """);

    [Fact]
    public Task TestNotWithDeconstruction()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (var (i, j) = (0, 0); i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81530")]
    public Task TestNotWithIterationVariableInTupleExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                    {
                        var tuple = (array[i], i);
                        Console.WriteLine(tuple);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMultidimensionalArray1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[,] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i, 0]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMultidimensionalArray2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[,] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i, i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestJaggedArray1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[][] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[][] array)
                {
                    foreach (string[] {|Rename:v|} in array)
                    {
                        Console.WriteLine(v);
                    }
                }
            }
            """);

    [Fact]
    public Task TestJaggedArray2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[][] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i][0]);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Test(string[][] array)
                {
                    foreach (string[] {|Rename:v|} in array)
                    {
                        Console.WriteLine(v[0]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestJaggedArray3()
        => TestInRegularAndScriptAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """);

    [Fact]
    public Task TestJaggedArray4()
        => TestInRegularAndScriptAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """);

    [Fact]
    public Task TestJaggedArray5()
        => TestInRegularAndScriptAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """);

    [Fact]
    public Task TestJaggedArray6()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Test(string[][] array)
                {
                    [||]for (int i = 0; i < array.Length; i++)
                    {
                        Console.WriteLine(array[i][i]);
                    }
                }
            }
            """);

    [Fact]
    public Task TestDoesNotUseLocalFunctionName()
        => TestInRegularAndScriptAsync(
"""
using System;

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
}
""",
"""
using System;

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
}
""");

    [Fact]
    public Task TestUsesLocalFunctionParameterName()
        => TestInRegularAndScriptAsync(
"""
using System;

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
}
""",
"""
using System;

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
}
""");

    [Fact]
    public Task TestDoesNotUseLambdaParameterWithCSharpLessThan8()
        => TestInRegularAndScriptAsync(
"""
using System;

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
}
""",
"""
using System;

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
}
""", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_3)));

    [Fact]
    public Task TestUsesLambdaParameterNameInCSharp8()
        => TestInRegularAndScriptAsync(
"""
using System;

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
}
""",
"""
using System;

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
}
""", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp8)));

    [Fact]
    public Task TestNotWhenIteratingDifferentLists()
        => TestMissingAsync(
            """
            using System;
            using System.Collection.Generic;

            class Item { public string Value; }

            class C
            {
                static void Test()
                {
                    var first = new { list = new List<Item>() };
                    var second = new { list = new List<Item>() };

                    [||]for (var i = 0; i < first.list.Count; i++)
                    {
                        first.list[i].Value = second.list[i].Value;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36305")]
    public Task TestOnElementAt1()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class V
            {
                void M(ICollection<V> collection)
                {
                    [||]for (int i = 0; i < collection.Count; ++i)
                        collection.ElementAt(i).M();
                }

                private void M()
                {
                }
            }
            """,

            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class V
            {
                void M(ICollection<V> collection)
                {
                    foreach (V {|Rename:v|} in collection)
                        v.M();
                }

                private void M()
                {
                }
            }
            """);
}
