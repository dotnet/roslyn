// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ReassignedVariable;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReassignedVariable
{
    public class CSharpReassignedVariableTests : AbstractReassignedVariableTests
    {
        protected override TestWorkspace CreateWorkspace(string markup)
            => TestWorkspace.CreateCSharp(markup);

        [Fact]
        public async Task TestNoParameterReassignment()
        {
            await TestAsync(
                """
                class C
                {
                    void M(int p)
                    {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestParameterReassignment()
        {
            await TestAsync(
                """
                class C
                {
                    void M(int [|p|])
                    {
                        [|p|] = 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestParameterReassignmentWhenReadAfter()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestParameterReassignmentWhenReadBefore()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestParameterReassignmentWhenReadWithDefaultValue()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestParameterWithExprBodyWithReassignment()
        {
            await TestAsync(
                """
                using System;
                class C
                {
                    void M(int [|p|]) => Console.WriteLine([|p|]++);
                }
                """);
        }

        [Fact]
        public async Task TestLocalFunctionWithExprBodyWithReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestIndexerWithWriteInExprBody()
        {
            await TestAsync(
                """
                using System;
                class C
                {
                    int this[int [|p|]] => [|p|]++;
                }
                """);
        }

        [Fact]
        public async Task TestIndexerWithWriteInGetter1()
        {
            await TestAsync(
                """
                using System;
                class C
                {
                    int this[int [|p|]] { get => [|p|]++; }
                }
                """);
        }

        [Fact]
        public async Task TestIndexerWithWriteInGetter2()
        {
            await TestAsync(
                """
                using System;
                class C
                {
                    int this[int [|p|]] { get { [|p|]++; } }
                }
                """);
        }

        [Fact]
        public async Task TestIndexerWithWriteInSetter1()
        {
            await TestAsync(
                """
                using System;
                class C
                {
                    int this[int [|p|]] { set => [|p|]++; }
                }
                """);
        }

        [Fact]
        public async Task TestIndexerWithWriteInSetter2()
        {
            await TestAsync(
                """
                using System;
                class C
                {
                    int this[int [|p|]] { set { [|p|]++; } }
                }
                """);
        }

        [Fact]
        public async Task TestPropertyWithAssignmentToValue1()
        {
            await TestAsync(
                """
                using System;
                class C
                {
                    int Goo { set => [|value|] = [|value|] + 1; }
                }
                """);
        }

        [Fact]
        public async Task TestPropertyWithAssignmentToValue2()
        {
            await TestAsync(
                """
                using System;
                class C
                {
                    int Goo { set { [|value|] = [|value|] + 1; } }
                }
                """);
        }

        [Fact]
        public async Task TestEventAddWithAssignmentToValue()
        {
            await TestAsync(
                """
                using System;
                class C
                {
                    event Action Goo { add { [|value|] = null; } remove { } }
                }
                """);
        }

        [Fact]
        public async Task TestEventRemoveWithAssignmentToValue()
        {
            await TestAsync(
                """
                using System;
                class C
                {
                    event Action Goo { add { } remove { [|value|] = null; } }
                }
                """);
        }

        [Fact]
        public async Task TestLambdaParameterWithoutReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLambdaParameterWithReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLambdaParameterWithReassignment2()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLocalWithoutInitializerWithoutReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLocalWithoutInitializerWithReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLocalDeclaredByPattern()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLocalDeclaredByPatternButAssignedInFalseBranch()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLocalDeclaredByPositionalPattern()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLocalDeclaredByOutVar()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestOutParameterCausingReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestOutParameterWithoutReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task AssignmentThroughOutParameter()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestOutParameterReassignmentOneWrites()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task AssignmentThroughRefParameter()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestRefParameterReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task AssignmentThroughRefLocal()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task AssignmentThroughScopedRefLocal()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestRefLocalReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task AssignmentThroughPointerIsNotAssignmentOfTheVariableItself()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestPointerVariableReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestRefParameterCausingPossibleReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestVolatileRefReadParameterCausingPossibleReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestRefParameterWithoutReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestRefLocalCausingPossibleReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestReadonlyRefLocalWithNoReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestScopedReadonlyRefLocalWithNoReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestReadonlyRefLocalWithNoReassignment1()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestScopedReadonlyRefLocalWithNoReassignment1()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestPointerCausingPossibleReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestRefExtensionMethodCausingPossibleReassignment()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestMutatingStructMethod()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestReassignmentWhenDeclaredWithDeconstruction()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestReassignmentThroughDeconstruction()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestTopLevelNotReassigned()
        {
            await TestAsync(
                """
                int p;
                p = 0;
                Console.WriteLine(p);
                """);
        }

        [Fact]
        public async Task TestTopLevelReassigned()
        {
            await TestAsync(
                """
                int [|p|] = 1;
                [|p|] = 0;
                Console.WriteLine([|p|]);
                """);
        }

        [Fact]
        public async Task TestTopLevelArgsParameterNotReassigned()
        {
            await TestAsync(
                """
                Console.WriteLine(args);
                """);
        }

        [Fact]
        public async Task TestTopLevelArgsParameterReassigned()
        {
            await TestAsync(
                """

                [|args|] = null
                Console.WriteLine([|args|]);

                """);
        }

        [Fact]
        public async Task TestUsedInThisBase1()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestUsedInThisBase2()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestRecord1()
        {
            await TestAsync(
                """
                record X(int [|x|]) : Y([|x|]++)
                {
                }

                record Y(int x)
                {
                }
                """);
        }

        [Fact]
        public async Task TestRecord2()
        {
            await TestAsync(
                """
                record X(int [|x|])
                {
                    int Y = [|x|]++;
                }
                """);
        }

        [Fact]
        public async Task TestRecord3()
        {
            await TestAsync(
                """
                record struct X(int [|x|])
                {
                    int Y = [|x|]++;
                }
                """);
        }

        [Fact]
        public async Task TestClass1()
        {
            await TestAsync(
                """
                class X(int [|x|]) : Y([|x|]++)
                {
                }

                class Y(int x)
                {
                }
                """);
        }

        [Fact]
        public async Task TestClass2()
        {
            await TestAsync(
                """
                class X(int [|x|])
                {
                    int Y = [|x|]++;
                }
                """);
        }

        [Fact]
        public async Task TestClass3()
        {
            await TestAsync(
                """
                class X(int [|x|])
                {
                    int Y() => [|x|]++;
                }
                """);
        }

        [Fact]
        public async Task TestStruct2()
        {
            await TestAsync(
                """
                struct X(int [|x|])
                {
                    int Y = [|x|]++;
                }
                """);
        }

        [Fact]
        public async Task TestStruct3()
        {
            await TestAsync(
                """
                struct X(int [|x|])
                {
                    int Y() => [|x|]++;
                }
                """);
        }

        [Fact]
        public async Task TestExceptionVariableReassignment()
        {
            // Note: this is a bug.  But the test currently tracks the current behavior.  Fixing this
            // is just not deemed worth it currently.
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLocalReassignedInExceptionFilter()
        {
            // Note: this is a bug.  But the test currently tracks the current behavior.  Fixing this
            // is just not deemed worth it currently.
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLocalReassignedInCaseGuard()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLocalWithMultipleDeclarators()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestForLoop()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestForeach()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestWriteThroughOneBranch()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestDuplicateMethod()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestDuplicateParameter()
        {
            await TestAsync(
                """
                class C
                {
                    void M(int p, int p)
                    {
                        p = 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58161")]
        public async Task TestRefToSuppression1()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestPrimaryConstructor1()
        {
            await TestAsync(
                """
                class C(int [|p|])
                {
                    void M()
                    {
                        [|p|] = 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestPrimaryConstructor2()
        {
            await TestAsync(
                """
                class C(int p)
                {
                    void M()
                    {
                        var v = new C(p: 1);
                    }
                }
                """);
        }

        [Fact]
        public async Task TestPrimaryConstructor3()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestPrimaryConstructor4()
        {
            await TestAsync(
                """
                class B(int p)
                {
                }

                partial class C(int [|p|]) : B([|p|] = 1)
                {
                }
                """);
        }

        [Fact]
        public async Task TestPrimaryConstructor5()
        {
            await TestAsync(
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
    }
}
