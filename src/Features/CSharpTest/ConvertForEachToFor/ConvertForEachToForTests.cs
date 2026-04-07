// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertForEachToFor;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertForEachToFor;

[Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToFor)]
public sealed partial class ConvertForEachToForTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpConvertForEachToForCodeRefactoringProvider();

    private readonly CodeStyleOption2<bool> onWithSilent = new(true, NotificationOption2.Silent);

    private OptionsCollection ImplicitTypeEverywhere
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, onWithSilent },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent },
        };

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")]
    public Task EmptyBlockBody()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach[||](var a in array)
                    {
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                    }
                }
            }
            """);

    [Fact]
    public Task EmptyBody()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach[||](var a in array) ;
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++) ;
                }
            }
            """);

    [Fact]
    public Task Body()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach[||](var a in array) Console.WriteLine(a);
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task BlockBody()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach[||](var a in array)
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")]
    public Task Comment()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    /* comment */
                    foreach[||](var a in array) /* comment */
                    {
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    /* comment */
                    for (int {|Rename:i|} = 0; i < array.Length; i++) /* comment */
                    {
                        int a = array[i];
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")]
    public Task Comment2()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach[||](var a in array)
                    /* comment */
                    {
                    }/* comment */
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    /* comment */
                    {
                        int a = array[i];
                    }/* comment */
                }
            }
            """);

    [Fact]
    public Task Comment3()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach[||](var a in array) /* comment */;
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++) /* comment */;
                }
            }
            """);

    [Fact]
    public Task Comment4()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach[||](var a in array) Console.WriteLine(a); // test
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                        Console.WriteLine(a); // test
                    }
                }
            }
            """);

    [Fact]
    public Task Comment5()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach [||] (var a in array) /* test */ Console.WriteLine(a); 
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++) /* test */
                    {
                        int a = array[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task Comment6()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach [||] (var a in array) 
                        /* test */ Console.WriteLine(a); 
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                        /* test */
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")]
    public Task Comment7()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    // test
                    foreach[||](var a in new int[] { 1, 3, 4 })
                    {
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    // test
                    int[] {|Rename:array|} = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                    }
                }
            }
            """);

    [Fact]
    public Task TestCommentsInTheMiddleOfParentheses()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach [||] (var a /* test */ in array) ;
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++) ;
                }
            }
            """);

    [Fact]
    public Task TestCommentsAtBeginningOfParentheses()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach [||] (/* test */ var a in array) ;
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++) ;
                }
            }
            """);

    [Fact]
    public Task TestCommentsAtTheEndOfParentheses()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach [||] (var a in array /* test */) ;
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++) ;
                }
            }
            """);

    [Fact]
    public Task CollectionStatement()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    foreach[||](var a in new int[] { 1, 3, 4 })
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    int[] {|Rename:array|} = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task CollectionConflict()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = 1;

                    foreach[||](var a in new int[] { 1, 3, 4 })
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = 1;

                    int[] {|Rename:array1|} = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array1.Length; i++)
                    {
                        int a = array1[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task VariableWritten()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new[] { 1 };
                    foreach [||] (var a in array)
                    {
                        a = 1;
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new[] { 1 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        {|Warning:int a = array[i];|}
                        a = 1;
                    }
                }
            }
            """);

    [Fact]
    public Task IndexConflict()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach [||] (var a in array)
                    {
                        int i = 0;
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i1|} = 0; i1 < array.Length; i1++)
                    {
                        int a = array[i1];
                        int i = 0;
                    }
                }
            }
            """);

    [Fact]
    public Task StructPropertyReadFromAndDiscarded()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                struct Struct
                {
                    public string Property { get; }
                }

                void Method()
                {
                    var array = new[] { new Struct() };
                    foreach [||] (var a in array)
                    {
                        _ = a.Property;
                    }
                }
            }
            """, """
            class Test
            {
                struct Struct
                {
                    public string Property { get; }
                }

                void Method()
                {
                    var array = new[] { new Struct() };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        Struct a = array[i];
                        _ = a.Property;
                    }
                }
            }
            """);

    [Fact]
    public Task StructPropertyReadFromAndAssignedToLocal()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                struct Struct
                {
                    public string Property { get; }
                }

                void Method()
                {
                    var array = new[] { new Struct() };
                    foreach [||] (var a in array)
                    {
                        var b = a.Property;
                    }
                }
            }
            """, """
            class Test
            {
                struct Struct
                {
                    public string Property { get; }
                }

                void Method()
                {
                    var array = new[] { new Struct() };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        Struct a = array[i];
                        var b = a.Property;
                    }
                }
            }
            """);

    [Fact]
    public Task WrongCaretPosition()
        => TestMissingInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach (var a in array)
                    {
                        [||] 
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/31621")]
    public Task TestCaretBefore()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    [||] foreach(var a in array)
                    {
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/31621")]
    public Task TestCaretAfter()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach(var a in array) [||]
                    {
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++) 
                    {
                        int a = array[i];
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/31621")]
    public Task TestSelection()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    [|foreach(var a in array)
                    {
                    }|]
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")]
    public Task Field()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                int[] array = new int[] { 1, 3, 4 };

                void Method()
                {
                    foreach [||] (var a in array)
                    {
                    }
                }
            }
            """, """
            class Test
            {
                int[] array = new int[] { 1, 3, 4 };

                void Method()
                {
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                    }
                }
            }
            """);

    [Fact]
    public Task ArrayElement()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[][] { new int[] { 1, 3, 4 } };
                    foreach [||] (var a in array[0])
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[][] { new int[] { 1, 3, 4 } };
                    for (int {|Rename:i|} = 0; i < array[0].Length; i++)
                    {
                        int a = array[0][i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")]
    public Task Parameter()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method(int[] array)
                {
                    foreach [||] (var a in array)
                    {
                    }
                }
            }
            """, """
            class Test
            {
                void Method(int[] array)
                {
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")]
    public Task Property()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                int [] Prop { get; } = new int[] { 1, 2, 3 };

                void Method()
                {
                    foreach [||] (var a in Prop)
                    {
                    }
                }
            }
            """, """
            class Test
            {
                int [] Prop { get; } = new int[] { 1, 2, 3 };

                void Method()
                {
                    for (int {|Rename:i|} = 0; i < Prop.Length; i++)
                    {
                        int a = Prop[i];
                    }
                }
            }
            """);

    [Fact]
    public Task Interface()
        => TestInRegularAndScriptAsync("""
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var array = (IList<int>)(new int[] { 1, 3, 4 });
                    foreach[||] (var a in array)
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var array = (IList<int>)(new int[] { 1, 3, 4 });
                    for (int {|Rename:i|} = 0; i < array.Count; i++)
                    {
                        int a = array[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task IListOfT()
        => TestInRegularAndScriptAsync("""
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new List<int>();
                    foreach [||](var a in list)
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new List<int>();
                    for (int {|Rename:i|} = 0; i < list.Count; i++)
                    {
                        int a = list[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task IReadOnlyListOfT()
        => TestInRegularAndScriptAsync("""
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new ReadOnly<int>();
                    foreach [||](var a in list)
                    {
                        Console.WriteLine(a);
                    }

                }
            }

            class ReadOnly<T> : IReadOnlyList<T>
            {
                public T this[int index] => throw new System.NotImplementedException();
                public int Count => throw new System.NotImplementedException();
                public IEnumerator<T> GetEnumerator() => throw new System.NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """, """
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new ReadOnly<int>();
                    for (int {|Rename:i|} = 0; i < list.Count; i++)
                    {
                        int a = list[i];
                        Console.WriteLine(a);
                    }

                }
            }

            class ReadOnly<T> : IReadOnlyList<T>
            {
                public T this[int index] => throw new System.NotImplementedException();
                public int Count => throw new System.NotImplementedException();
                public IEnumerator<T> GetEnumerator() => throw new System.NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """);

    [Fact]
    public Task IList()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Collections;

            class Test
            {
                void Method()
                {
                    var list = new List();
                    foreach [||](var a in list)
                    {
                        Console.WriteLine(a);
                    }

                }
            }

            class List : IList
            {
                public object this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
                public bool IsReadOnly => throw new NotImplementedException();
                public bool IsFixedSize => throw new NotImplementedException();
                public int Count => throw new NotImplementedException();
                public object SyncRoot => throw new NotImplementedException();
                public bool IsSynchronized => throw new NotImplementedException();
                public int Add(object value) => throw new NotImplementedException();
                public void Clear() => throw new NotImplementedException();
                public bool Contains(object value) => throw new NotImplementedException();
                public void CopyTo(Array array, int index) => throw new NotImplementedException();
                public IEnumerator GetEnumerator() => throw new NotImplementedException();
                public int IndexOf(object value) => throw new NotImplementedException();
                public void Insert(int index, object value) => throw new NotImplementedException();
                public void Remove(object value) => throw new NotImplementedException();
                public void RemoveAt(int index) => throw new NotImplementedException();
            }
            """, """
            using System;
            using System.Collections;

            class Test
            {
                void Method()
                {
                    var list = new List();
                    for (int {|Rename:i|} = 0; i < list.Count; i++)
                    {
                        object a = list[i];
                        Console.WriteLine(a);
                    }

                }
            }

            class List : IList
            {
                public object this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
                public bool IsReadOnly => throw new NotImplementedException();
                public bool IsFixedSize => throw new NotImplementedException();
                public int Count => throw new NotImplementedException();
                public object SyncRoot => throw new NotImplementedException();
                public bool IsSynchronized => throw new NotImplementedException();
                public int Add(object value) => throw new NotImplementedException();
                public void Clear() => throw new NotImplementedException();
                public bool Contains(object value) => throw new NotImplementedException();
                public void CopyTo(Array array, int index) => throw new NotImplementedException();
                public IEnumerator GetEnumerator() => throw new NotImplementedException();
                public int IndexOf(object value) => throw new NotImplementedException();
                public void Insert(int index, object value) => throw new NotImplementedException();
                public void Remove(object value) => throw new NotImplementedException();
                public void RemoveAt(int index) => throw new NotImplementedException();
            }
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/29740")]
    public async Task ImmutableArray()
    {
        var text = """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                <MetadataReference>
            """ + typeof(ImmutableArray<>).Assembly.Location + """
            </MetadataReference>
                    <Document>
            using System;
            using System.Collections.Immutable;

            class Test
            {
                void Method()
                {
                    var list = ImmutableArray.Create(1);
                    foreach [||](var a in list)
                    {
                        Console.WriteLine(a);
                    }
                }
            }</Document>
                </Project>
            </Workspace>
            """;
        await TestInRegularAndScriptAsync(text, """
            using System;
            using System.Collections.Immutable;

            class Test
            {
                void Method()
                {
                    var list = ImmutableArray.Create(1);
                    for (int {|Rename:i|} = 0; i < list.Length; i++)
                    {
                        int a = list[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);
    }

    [Fact]
    public Task ExplicitInterface()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new Explicit();
                    foreach [||] (var a in list)
                    {
                        Console.WriteLine(a);
                    }
                }
            }

            class Explicit : IReadOnlyList<int>
            {
                int IReadOnlyList<int>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<int>.Count => throw new NotImplementedException();
                IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
            }
            """, """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new Explicit();
                    IReadOnlyList<int> {|Rename:list1|} = list;
                    for (int {|Rename:i|} = 0; i < list1.Count; i++)
                    {
                        int a = list1[i];
                        Console.WriteLine(a);
                    }
                }
            }

            class Explicit : IReadOnlyList<int>
            {
                int IReadOnlyList<int>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<int>.Count => throw new NotImplementedException();
                IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task DoubleExplicitInterface()
        => TestMissingInRegularAndScriptAsync("""
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new Explicit();
                    foreach [||] (var a in list)
                    {
                        Console.WriteLine(a);
                    }
                }
            }

            class Explicit : IReadOnlyList<int>, IReadOnlyList<string>
            {
                int IReadOnlyList<int>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<int>.Count => throw new NotImplementedException();
                IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

                string IReadOnlyList<string>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<string>.Count => throw new NotImplementedException();
                IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task DoubleExplicitInterfaceWithExplicitType()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new Explicit();
                    foreach [||] (int a in list)
                    {
                        Console.WriteLine(a);
                    }
                }
            }

            class Explicit : IReadOnlyList<int>, IReadOnlyList<string>
            {
                int IReadOnlyList<int>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<int>.Count => throw new NotImplementedException();
                IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

                string IReadOnlyList<string>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<string>.Count => throw new NotImplementedException();
                IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw new NotImplementedException();
            }
            """, """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new Explicit();
                    IReadOnlyList<int> {|Rename:list1|} = list;
                    for (int {|Rename:i|} = 0; i < list1.Count; i++)
                    {
                        int a = list1[i];
                        Console.WriteLine(a);
                    }
                }
            }

            class Explicit : IReadOnlyList<int>, IReadOnlyList<string>
            {
                int IReadOnlyList<int>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<int>.Count => throw new NotImplementedException();
                IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

                string IReadOnlyList<string>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<string>.Count => throw new NotImplementedException();
                IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task MixedInterfaceImplementation()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new Mixed();
                    foreach [||] (var a in list)
                    {
                        Console.WriteLine(a);
                    }
                }
            }

            class Mixed : IReadOnlyList<int>, IReadOnlyList<string>
            {
                public int this[int index] => throw new NotImplementedException();
                public int Count => throw new NotImplementedException();
                public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

                string IReadOnlyList<string>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<string>.Count => throw new NotImplementedException();
                IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw new NotImplementedException();
            }
            """, """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new Mixed();
                    for (int {|Rename:i|} = 0; i < list.Count; i++)
                    {
                        int a = list[i];
                        Console.WriteLine(a);
                    }
                }
            }

            class Mixed : IReadOnlyList<int>, IReadOnlyList<string>
            {
                public int this[int index] => throw new NotImplementedException();
                public int Count => throw new NotImplementedException();
                public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

                string IReadOnlyList<string>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<string>.Count => throw new NotImplementedException();
                IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task MixedInterfaceImplementationWithExplicitType()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new Mixed();
                    foreach [||] (string a in list)
                    {
                        Console.WriteLine(a);
                    }
                }
            }

            class Mixed : IReadOnlyList<int>, IReadOnlyList<string>
            {
                public int this[int index] => throw new NotImplementedException();
                public int Count => throw new NotImplementedException();
                public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

                string IReadOnlyList<string>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<string>.Count => throw new NotImplementedException();
                IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw new NotImplementedException();
            }
            """, """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new Mixed();
                    IReadOnlyList<string> {|Rename:list1|} = list;
                    for (int {|Rename:i|} = 0; i < list1.Count; i++)
                    {
                        string a = list1[i];
                        Console.WriteLine(a);
                    }
                }
            }

            class Mixed : IReadOnlyList<int>, IReadOnlyList<string>
            {
                public int this[int index] => throw new NotImplementedException();
                public int Count => throw new NotImplementedException();
                public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

                string IReadOnlyList<string>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<string>.Count => throw new NotImplementedException();
                IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task PreserveUserExpression()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Collections;
            using System.Collections.Generic;

            namespace NS
            {
                class Test
                {
                    void Method()
                    {
                        foreach [||] (string a in new NS.Mixed())
                        {
                            Console.WriteLine(a);
                        }
                    }
                }

                class Mixed : IReadOnlyList<int>, IReadOnlyList<string>
                {
                    public int this[int index] => throw new NotImplementedException();
                    public int Count => throw new NotImplementedException();
                    public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
                    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

                    string IReadOnlyList<string>.this[int index] => throw new NotImplementedException();
                    int IReadOnlyCollection<string>.Count => throw new NotImplementedException();
                    IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw new NotImplementedException();
                }
            }
            """, """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            namespace NS
            {
                class Test
                {
                    void Method()
                    {
                        IReadOnlyList<string> {|Rename:list|} = new NS.Mixed();
                        for (int {|Rename:i|} = 0; i < list.Count; i++)
                        {
                            string a = list[i];
                            Console.WriteLine(a);
                        }
                    }
                }

                class Mixed : IReadOnlyList<int>, IReadOnlyList<string>
                {
                    public int this[int index] => throw new NotImplementedException();
                    public int Count => throw new NotImplementedException();
                    public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
                    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

                    string IReadOnlyList<string>.this[int index] => throw new NotImplementedException();
                    int IReadOnlyCollection<string>.Count => throw new NotImplementedException();
                    IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task EmbededStatement()
        => TestMissingInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    if (true)
                        foreach [||] (var a in new int[] {});
                }
            }
            """);

    [Fact]
    public Task EmbededStatement2()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    if (true)
                        foreach [||] (var a in array) Console.WriteLine(a);
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    if (true)
                        for (int {|Rename:i|} = 0; i < array.Length; i++)
                        {
                            int a = array[i];
                            Console.WriteLine(a);
                        }
                }
            }
            """);

    [Fact]
    public Task IndexConflict2()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach [||] (var i in array)
                    {
                        Console.WriteLine(i);
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i1|} = 0; i1 < array.Length; i1++)
                    {
                        int i = array[i1];
                        Console.WriteLine(i);
                    }
                }
            }
            """);

    [Fact]
    public Task UseTypeAsUsedInForeach()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    foreach [||] (int a in array)
                    {
                        Console.WriteLine(i);
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 3, 4 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        int a = array[i];
                        Console.WriteLine(i);
                    }
                }
            }
            """);

    [Fact]
    public Task String()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    foreach [||] (var a in "test")
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    string {|Rename:str|} = "test";
                    for (int {|Rename:i|} = 0; i < str.Length; i++)
                    {
                        char a = str[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task StringLocalConst()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    const string test = "test";
                    foreach [||] (var a in test)
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    const string test = "test";
                    for (int {|Rename:i|} = 0; i < test.Length; i++)
                    {
                        char a = test[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task StringConst()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                const string test = "test";

                void Method()
                {
                    foreach [||] (var a in test)
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            class Test
            {
                const string test = "test";

                void Method()
                {
                    for (int {|Rename:i|} = 0; i < test.Length; i++)
                    {
                        char a = test[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task ElementExplicitCast()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new object[] { 1, 2, 3 };
                    foreach [||] (string a in array)
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var array = new object[] { 1, 2, 3 };
                    for (int {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        string a = (string)array[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50469")]
    public Task PreventExplicitCastToVar()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var items = new[] { new { x = 1 } };

                    foreach [||] (var item in items)
                    {
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var items = new[] { new { x = 1 } };

                    for (int {|Rename:i|} = 0; i < items.Length; i++)
                    {
                        var item = items[i];
                    }
                }
            }
            """);

    [Fact]
    public Task NotAssignable()
        => TestMissingInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 2, 3 };
                    foreach [||] (string a in array)
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task ElementMissing()
        => TestMissingInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var array = new int[] { 1, 2, 3 };
                    foreach [||] (in array)
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task ElementMissing2()
        => TestMissingInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    foreach [||] (string a in )
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task StringExplicitType()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    foreach [||] (int a in "test")
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    string {|Rename:str|} = "test";
                    for (int {|Rename:i|} = 0; i < str.Length; i++)
                    {
                        int a = str[i];
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task Var()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new Explicit();
                    foreach [||] (var a in list)
                    {
                        Console.WriteLine(a);
                    }
                }
            }

            class Explicit : IReadOnlyList<int>
            {
                int IReadOnlyList<int>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<int>.Count => throw new NotImplementedException();
                IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
            }
            """, """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Test
            {
                void Method()
                {
                    var list = new Explicit();
                    var {|Rename:list1|} = (IReadOnlyList<int>)list;
                    for (var {|Rename:i|} = 0; i < list1.Count; i++)
                    {
                        var a = list1[i];
                        Console.WriteLine(a);
                    }
                }
            }

            class Explicit : IReadOnlyList<int>
            {
                int IReadOnlyList<int>.this[int index] => throw new NotImplementedException();
                int IReadOnlyCollection<int>.Count => throw new NotImplementedException();
                IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
            }
            """, new(options: ImplicitTypeEverywhere));

    [Fact]
    public Task ArrayRank2()
        => TestMissingAsync("""
            class Test
            {
                void Method()
                {
                    foreach [||] (int a in new int[,] { {1, 2} })
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48950")]
    public Task NullableReferenceVar()
        => TestInRegularAndScriptAsync("""
            #nullable enable
            class Test
            {
                void Method()
                {
                    foreach [||] (var s in new string[10])
                    {
                        Console.WriteLine(s);
                    }
                }
            }
            """, """
            #nullable enable
            class Test
            {
                void Method()
                {
                    var {|Rename:array|} = new string[10];
                    for (var {|Rename:i|} = 0; i < array.Length; i++)
                    {
                        var s = array[i];
                        Console.WriteLine(s);
                    }
                }
            }
            """, new(options: ImplicitTypeEverywhere));
}
