// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ReassignedVariable;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReassignedVariable;

public sealed class CSharpReassignedVariableTests : AbstractReassignedVariableTests
{
    protected override EditorTestWorkspace CreateWorkspace(string markup)
        => EditorTestWorkspace.CreateCSharp(markup);

    [Fact]
    public Task TestNoParameterReassignment()
        => TestAsync(
            """
            class C
            {
                void M(int p)
                {
                }
            }
            """);

    [Fact]
    public Task TestParameterReassignment()
        => TestAsync(
            """
            class C
            {
                void M(int [|p|])
                {
                    [|p|] = 1;
                }
            }
            """);

    [Fact]
    public Task TestParameterReassignmentWhenReadAfter()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(int [|p|])
                {
                    [|p|] = 1;
                    Console.WriteLine([|p|]);
                }
            }
            """);

    [Fact]
    public Task TestParameterReassignmentWhenReadBefore()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(int [|p|])
                {
                    Console.WriteLine([|p|]);
                    [|p|] = 1;
                }
            }
            """);

    [Fact]
    public Task TestParameterReassignmentWhenReadWithDefaultValue()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(int [|p|] = 1)
                {
                    Console.WriteLine([|p|]);
                    [|p|] = 1;
                }
            }
            """);

    [Fact]
    public Task TestParameterWithExprBodyWithReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(int [|p|]) => Console.WriteLine([|p|]++);
            }
            """);

    [Fact]
    public Task TestLocalFunctionWithExprBodyWithReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    void Local(int [|p|])
                        => Console.WriteLine([|p|]++);
            }
            """);

    [Fact]
    public Task TestIndexerWithWriteInExprBody()
        => TestAsync(
            """
            using System;
            class C
            {
                int this[int [|p|]] => [|p|]++;
            }
            """);

    [Fact]
    public Task TestIndexerWithWriteInGetter1()
        => TestAsync(
            """
            using System;
            class C
            {
                int this[int [|p|]] { get => [|p|]++; }
            }
            """);

    [Fact]
    public Task TestIndexerWithWriteInGetter2()
        => TestAsync(
            """
            using System;
            class C
            {
                int this[int [|p|]] { get { [|p|]++; } }
            }
            """);

    [Fact]
    public Task TestIndexerWithWriteInSetter1()
        => TestAsync(
            """
            using System;
            class C
            {
                int this[int [|p|]] { set => [|p|]++; }
            }
            """);

    [Fact]
    public Task TestIndexerWithWriteInSetter2()
        => TestAsync(
            """
            using System;
            class C
            {
                int this[int [|p|]] { set { [|p|]++; } }
            }
            """);

    [Fact]
    public Task TestPropertyWithAssignmentToValue1()
        => TestAsync(
            """
            using System;
            class C
            {
                int Goo { set => [|value|] = [|value|] + 1; }
            }
            """);

    [Fact]
    public Task TestPropertyWithAssignmentToValue2()
        => TestAsync(
            """
            using System;
            class C
            {
                int Goo { set { [|value|] = [|value|] + 1; } }
            }
            """);

    [Fact]
    public Task TestEventAddWithAssignmentToValue()
        => TestAsync(
            """
            using System;
            class C
            {
                event Action Goo { add { [|value|] = null; } remove { } }
            }
            """);

    [Fact]
    public Task TestEventRemoveWithAssignmentToValue()
        => TestAsync(
            """
            using System;
            class C
            {
                event Action Goo { add { } remove { [|value|] = null; } }
            }
            """);

    [Fact]
    public Task TestLambdaParameterWithoutReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    Action<int> a = x => Console.WriteLine(x);
                }
            }
            """);

    [Fact]
    public Task TestLambdaParameterWithReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    Action<int> a = [|x|] => Console.WriteLine([|x|]++);
                }
            }
            """);

    [Fact]
    public Task TestLambdaParameterWithReassignment2()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    Action<int> a = (int [|x|]) => Console.WriteLine([|x|]++);
                }
            }
            """);

    [Fact]
    public Task TestLocalWithoutInitializerWithoutReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(bool b)
                {
                    int p;
                    if (b)
                        p = 1;
                    else
                        p = 2;

                    Console.WriteLine(p);
                }
            }
            """);

    [Fact]
    public Task TestLocalWithoutInitializerWithReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(bool b)
                {
                    int [|p|];
                    if (b)
                        [|p|] = 1;
                    else
                        [|p|] = 2;

                    [|p|] = 0;
                    Console.WriteLine([|p|]);
                }
            }
            """);

    [Fact]
    public Task TestLocalDeclaredByPattern()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    if (0 is var [|p|]) [|p|] = 0;
                    Console.WriteLine([|p|]);
                }
            }
            """);

    [Fact]
    public Task TestLocalDeclaredByPatternButAssignedInFalseBranch()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    if (0 is var [|p|])
                    {
                    }
                    else
                    {
                         [|p|] = 0;
                    }

                    Console.WriteLine([|p|]);
                }
            }
            """);

    [Fact]
    public Task TestLocalDeclaredByPositionalPattern()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    if ((0, 1) is var ([|p|], _)) [|p|] = 0;
                    Console.WriteLine([|p|]);
                }
            }
            """);

    [Fact]
    public Task TestPatternMatchingReassignedInLocalFunction()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    if (0 is var [|p|])
                    {
                        LocalFunc();
                        Console.WriteLine([|p|]);
                    }
                    
                    void LocalFunc()
                    {
                        [|p|] = 1;
                    }
                }
            }
            """);

    [Fact]
    public Task TestLocalDeclaredByOutVar()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    M2(out var [|p|]);
                    [|p|] = 0;
                    Console.WriteLine([|p|]);
                }

                void M2(out int p) => p = 0;
            }
            """);

    [Fact]
    public Task TestOutVarReassignedInLocalFunction()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    Goo(out var [|v|]);
                    
                    LocalFunc();
                    Console.WriteLine([|v|]);
                    
                    void LocalFunc()
                    {
                        [|v|] = 1;
                    }
                }

                void Goo(out int v) => v = 0;
            }
            """);

    [Fact]
    public Task TestOutParameterCausingReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    int [|p|] = 0;
                    M2(out [|p|]);
                    Console.WriteLine([|p|]);
                }

                void M2(out int p) => p = 0;
            }
            """);

    [Fact]
    public Task TestOutParameterWithoutReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    int p;
                    M2(out p);
                    Console.WriteLine(p);
                }

                void M2(out int p) => p = 0;
            }
            """);

    [Fact]
    public Task AssignmentThroughOutParameter()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(out int [|p|])
                {
                    [|p|] = 0;
                    [|p|] = 1;
                    Console.WriteLine([|p|]);
                }
            }
            """);

    [Fact]
    public Task TestOutParameterReassignmentOneWrites()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(out int p)
                {
                    p = ref p;
                    Console.WriteLine(p);
                }
            }
            """);

    [Fact]
    public Task AssignmentThroughRefParameter()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(ref int [|p|])
                {
                    [|p|] = 0;
                    [|p|] = 1;
                    Console.WriteLine([|p|]);
                }
            }
            """);

    [Fact]
    public Task TestRefParameterReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(ref int [|p|])
                {
                    [|p|] = ref [|p|];
                    Console.WriteLine([|p|]);
                }
            }
            """);

    [Fact]
    public Task AssignmentThroughRefLocal()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(ref int [|p|])
                {
                    ref var [|local|] = ref [|p|];
                    [|local|] = 0;
                    [|local|] = 1;
                    Console.WriteLine([|local|]);
                }
            }
            """);

    [Fact]
    public Task AssignmentThroughScopedRefLocal()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(ref int [|p|])
                {
                    scoped ref var [|local|] = ref [|p|];
                    [|local|] = 0;
                    [|local|] = 1;
                    Console.WriteLine([|local|]);
                }
            }
            """);

    [Fact]
    public Task TestRefLocalReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(ref int [|p|])
                {
                    // p is statically detected as overwritten (even though it is not written at runtime)
                    // due to a limitation in alias analysis.
                    ref var [|local|] = ref [|p|];
                    [|local|] = ref [|p|];
                    Console.WriteLine([|local|]);
                }
            }
            """);

    [Fact]
    public Task AssignmentThroughPointerIsNotAssignmentOfTheVariableItself()
        => TestAsync(
            """
            using System;
            class C
            {
                unsafe void M(int* p)
                {
                    *p = 4;
                    Console.WriteLine((IntPtr)p);
                }
            }
            """);

    [Fact]
    public Task TestPointerVariableReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                unsafe void M(int* [|p|])
                {
                    [|p|] = null;
                    Console.WriteLine((IntPtr)[|p|]);
                }
            }
            """);

    [Fact]
    public Task TestRefParameterCausingPossibleReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    int [|p|] = 0;
                    M2(ref [|p|]);
                    Console.WriteLine([|p|]);
                }

                void M2(ref int p) { }
            }
            """);

    [Fact]
    public Task TestVolatileRefReadParameterCausingPossibleReassignment()
        => TestAsync(
            """
            using System;
            using System.Threading;
            class C
            {
                void M()
                {
                    // p is statically detected as overwritten (even though it is not written at runtime)
                    // due to a limitation in ref analysis.
                    int [|p|] = 0;
                    Volatile.Read(ref [|p|]);
                    Console.WriteLine([|p|]);
                }
            }
            """);

    [Fact]
    public Task TestRefParameterWithoutReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    int p;
                    M2(ref p);
                    Console.WriteLine(p);
                }

                void M2(ref int p) { }
            }
            """);

    [Fact]
    public Task TestRefLocalCausingPossibleReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    int [|p|] = 0;
                    ref int refP = ref [|p|];
                    Console.WriteLine([|p|]);
                }
            }
            """);

    [Fact]
    public Task TestReadonlyRefLocalWithNoReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    int p = 0;
                    ref readonly int refP = ref p;
                    Console.WriteLine(p);
                }
            }
            """);

    [Fact]
    public Task TestScopedReadonlyRefLocalWithNoReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    int p = 0;
                    scoped ref readonly int refP = ref p;
                    Console.WriteLine(p);
                }
            }
            """);

    [Fact]
    public Task TestReadonlyRefLocalWithNoReassignment1()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    int p = 0;
                    ref readonly int refP = ref p!;
                    Console.WriteLine(p);
                }
            }
            """);

    [Fact]
    public Task TestScopedReadonlyRefLocalWithNoReassignment1()
        => TestAsync(
            """
            using System;
            class C
            {
                void M1()
                {
                    int p = 0;
                    scoped ref readonly int refP = ref p!;
                    Console.WriteLine(p);
                }
            }
            """);

    [Fact]
    public Task TestPointerCausingPossibleReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                unsafe void M()
                {
                    int [|p|] = 0;
                    int* pointer = &[|p|];
                    Console.WriteLine([|p|]);
                }
            }
            """);

    [Fact]
    public Task TestRefExtensionMethodCausingPossibleReassignment()
        => TestAsync(
            """
            using System;
            static class C
            {
                void M()
                {
                    int [|p|] = 0;
                    [|p|].M2();
                    Console.WriteLine([|p|]);
                }

                static void M2(this ref int p) { }
            }
            """);

    [Fact]
    public Task TestMutatingStructMethod()
        => TestAsync(
            """
            using System;
            struct S
            {
                int f;

                void M(S p)
                {
                    p.MutatingMethod();
                    Console.WriteLine(p);
                }

                void MutatingMethod() => this = default;
            }
            """);

    [Fact]
    public Task TestReassignmentWhenDeclaredWithDeconstruction()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    var ([|x|], y) = Goo();
                    [|x|] = 0;
                    Console.WriteLine([|x|]);
                }

                (int x, int y) Goo() => default;
            }
            """);

    [Fact]
    public Task TestReassignmentThroughDeconstruction()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    var [|x|] = 0;
                    ([|x|], _) = Goo();
                    Console.WriteLine([|x|]);
                }

                (int x, int y) Goo() => default;
            }
            """);

    [Fact]
    public Task TestDeconstructionReassignedInLocalFunction()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    var ([|b|], [|c|]) = (0, 0);
                    
                    Foo();
                    Console.WriteLine($"{[|b|]} {[|c|]}");
                    
                    void Foo()
                    {
                        [|b|] = 2;
                        if (Environment.TickCount > 12345)
                            [|c|] = 1;
                        else
                            [|c|] = 2;
                    }
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionReassignedInLocalFunction_MixedWithRegular()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    var [|a|] = 0;
                    var ([|b|], [|c|]) = (0, 0);
                    
                    Foo();
                    Console.WriteLine($"{[|a|]} {[|b|]} {[|c|]}");
                    
                    void Foo()
                    {
                        [|a|] = 1;
                        [|b|] = 2;
                        if (Environment.TickCount > 12345)
                            [|c|] = 1;
                        else
                            [|c|] = 2;
                    }
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionReassignedInLocalFunction_ExplicitType()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    (int [|b|], int [|c|]) = (0, 0);
                    
                    Foo();
                    Console.WriteLine($"{[|b|]} {[|c|]}");
                    
                    void Foo()
                    {
                        [|b|] = 2;
                        if (Environment.TickCount > 12345)
                            [|c|] = 1;
                        else
                            [|c|] = 2;
                    }
                }
            }
            """);

    [Fact]
    public Task TestDeconstructionReassignedInLocalFunction_Nested()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    var ([|a|], ([|b|], [|c|])) = (1, (2, 3));
                    
                    Foo();
                    Console.WriteLine($"{[|a|]} {[|b|]} {[|c|]}");
                    
                    void Foo()
                    {
                        [|a|] = 10;
                        [|b|] = 20;
                        [|c|] = 30;
                    }
                }
            }
            """);

    [Fact]
    public Task TestTopLevelNotReassigned()
        => TestAsync(
            """
            int p;
            p = 0;
            Console.WriteLine(p);
            """);

    [Fact]
    public Task TestTopLevelReassigned()
        => TestAsync(
            """
            int [|p|] = 1;
            [|p|] = 0;
            Console.WriteLine([|p|]);
            """);

    [Fact]
    public Task TestTopLevelArgsParameterNotReassigned()
        => TestAsync(
            """
            Console.WriteLine(args);
            """);

    [Fact]
    public Task TestTopLevelArgsParameterReassigned()
        => TestAsync(
            """

            [|args|] = null
            Console.WriteLine([|args|]);

            """);

    [Fact]
    public Task TestUsedInThisBase1()
        => TestAsync(
            """
            class C
            {
                public C(int [|x|])
                    : this([|x|]++, true)
                {
                }

                public C(int x, bool b)
                {
                }
            }
            """);

    [Fact]
    public Task TestUsedInThisBase2()
        => TestAsync(
            """
            class C
            {
                public C(string s)
                    : this(int.TryParse(s, out var [|x|]) ? [|x|]++ : 0, true)
                {
                }

                public C(int x, bool b)
                {
                }
            }
            """);

    [Fact]
    public Task TestRecord1()
        => TestAsync(
            """
            record X(int [|x|]) : Y([|x|]++)
            {
            }

            record Y(int x)
            {
            }
            """);

    [Fact]
    public Task TestRecord2()
        => TestAsync(
            """
            record X(int [|x|])
            {
                int Y = [|x|]++;
            }
            """);

    [Fact]
    public Task TestRecord3()
        => TestAsync(
            """
            record struct X(int [|x|])
            {
                int Y = [|x|]++;
            }
            """);

    [Fact]
    public Task TestClass1()
        => TestAsync(
            """
            class X(int [|x|]) : Y([|x|]++)
            {
            }

            class Y(int x)
            {
            }
            """);

    [Fact]
    public Task TestClass2()
        => TestAsync(
            """
            class X(int [|x|])
            {
                int Y = [|x|]++;
            }
            """);

    [Fact]
    public Task TestClass3()
        => TestAsync(
            """
            class X(int [|x|])
            {
                int Y() => [|x|]++;
            }
            """);

    [Fact]
    public Task TestStruct2()
        => TestAsync(
            """
            struct X(int [|x|])
            {
                int Y = [|x|]++;
            }
            """);

    [Fact]
    public Task TestStruct3()
        => TestAsync(
            """
            struct X(int [|x|])
            {
                int Y() => [|x|]++;
            }
            """);

    [Fact]
    public Task TestExceptionVariableReassignment()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    try { }
                    catch (Exception ex)
                    {
                        [|ex|] = null;
                    }
                }
            }
            """);

    [Fact]
    public Task TestLocalReassignedInExceptionFilter()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    try { }
                    catch (Exception ex) when (([|ex|] = null) == null) { }
                }
            }
            """);

    [Fact]
    public Task TestLocalReassignedInCaseGuard()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    switch (1)
                    {
                        case var [|x|] when [|x|]++ == 2: break;
                    }
                }
            }
            """);

    [Fact]
    public Task TestLocalWithMultipleDeclarators()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    int a, [|b|] = 1, c;
                    [|b|] = 2;
                    Console.WriteLine([|b|]);
                }
            }
            """);

    [Fact]
    public Task TestForLoop()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    for (int [|i|] = 0; [|i|] < 10; [|i|]++)
                        Console.WriteLine([|i|]);
                }
            }
            """);

    [Fact]
    public Task TestForeach()
        => TestAsync(
            """
            using System;
            class C
            {
                void M(string[] args)
                {
                    foreach (var arg in args)
                        Console.WriteLine(arg);
                }
            }
            """);

    [Fact]
    public Task TestWriteThroughOneBranch()
        => TestAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    int p;
                    if (p)
                        p = 1;

                    p = 0;
                    Console.WriteLine(p);
                }
            }
            """);

    [Fact]
    public Task TestDuplicateMethod()
        => TestAsync(
            """
            class C
            {
                void M(int [|p|])
                {
                    [|p|] = 1;
                }

                void M(int [|p|])
                {
                    [|p|] = 1;
                }
            }
            """);

    [Fact]
    public Task TestDuplicateParameter()
        => TestAsync(
            """
            class C
            {
                void M(int p, int p)
                {
                    p = 1;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58161")]
    public Task TestRefToSuppression1()
        => TestAsync(
            """
            #nullable enable

            using System.Diagnostics.CodeAnalysis;
            using System.Threading;

            class C
            {
                public static T EnsureInitialized<T>([NotNull] ref T? [|target|]) where T : class
                    => Volatile.Read(ref [|target|]!);
            }
            """);

    [Fact]
    public Task TestPrimaryConstructor1()
        => TestAsync(
            """
            class C(int [|p|])
            {
                void M()
                {
                    [|p|] = 1;
                }
            }
            """);

    [Fact]
    public Task TestPrimaryConstructor2()
        => TestAsync(
            """
            class C(int p)
            {
                void M()
                {
                    var v = new C(p: 1);
                }
            }
            """);

    [Fact]
    public Task TestPrimaryConstructor3()
        => TestAsync(
            """
            partial class C(int [|p|])
            {
            }

            partial class C
            {
                void M()
                {
                    [|p|] = 1;
                }
            }
            """);

    [Fact]
    public Task TestPrimaryConstructor4()
        => TestAsync(
            """
            class B(int p)
            {
            }

            partial class C(int [|p|]) : B([|p|] = 1)
            {
            }
            """);

    [Fact]
    public Task TestPrimaryConstructor5()
        => TestAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            partial class C(int [|p|])
            {
            }
                    </Document>
                    <Document>
            partial class C
            {
                void M()
                {
                    [|p|] = 1;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);
}
